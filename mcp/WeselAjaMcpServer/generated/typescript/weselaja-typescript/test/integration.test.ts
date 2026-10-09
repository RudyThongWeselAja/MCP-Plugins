import assert from 'node:assert/strict';
import { createServer, type IncomingMessage, type ServerResponse } from 'node:http';
import { once } from 'node:events';
import { after, test } from 'node:test';
import {
  ConfigurationError,
  WeselAjaApiError,
  WeselAjaClient,
  WeselAjaConfig,
  generateSignature,
  resolveBankChannel,
} from '../src/index.js';

const server = createServer();
let baseUrl = '';

after(() => {
  if (server.listening) server.close();
});

async function listen(handler: (request: IncomingMessage, response: ServerResponse) => void): Promise<void> {
  server.removeAllListeners('request');
  server.on('request', handler);
  if (!server.listening) {
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    const address = server.address();
    if (!address || typeof address === 'string') throw new Error('Test server did not bind to a TCP port.');
    baseUrl = `http://127.0.0.1:${address.port}`;
  }
}

function firstHeader(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

function config(apiUrl = 'https://example.test'): WeselAjaConfig {
  return new WeselAjaConfig({
    apiUrl,
    apiKey: 'test-api-key',
    secretKey: 'test-secret',
    customerReference: 'CUSTOMER_123',
    callbackUrl: 'https://merchant.example/callback',
    redirectUrl: 'https://merchant.example/return',
  });
}

test('resolves supported bank aliases and rejects unknown banks', () => {
  assert.equal(resolveBankChannel('Bank BRI'), 'BRI.VA');
  assert.equal(resolveBankChannel('Bank-Mandiri'), 'MDR.VA');
  assert.equal(resolveBankChannel('BNI'), 'BNI.VA');
  assert.equal(resolveBankChannel('Permata'), 'PTB.VA');
  assert.equal(resolveBankChannel('CIMB Niaga'), 'CIMB.VA');
  assert.equal(resolveBankChannel('BCA'), 'BCA.VA');
  assert.throws(() => resolveBankChannel('Unknown'), /Unsupported bank/);
});

test('generates HMAC-SHA256 signatures as Base64', () => {
  const signature = generateSignature('POST', '/v1/payins', 'timestamp', 'body', 'secret');
  assert.match(signature, /^[A-Za-z0-9+/]{43}=$/);
});

test('validates configuration URLs and required values', () => {
  assert.throws(
    () => config('ftp://invalid.example').validate(),
    (error: unknown) => error instanceof ConfigurationError && /HTTP or HTTPS URL/.test(error.message),
  );
  assert.throws(
    () =>
      new WeselAjaConfig({
        apiUrl: 'https://example.test',
        apiKey: '',
        secretKey: '',
        customerReference: '',
        callbackUrl: '',
        redirectUrl: '',
      }).validate(),
    /Missing configuration/,
  );
});

test('creates a signed payment request and parses the response', async () => {
  await listen((request, response) => {
    assert.equal(request.method, 'POST');
    assert.equal(request.url, '/v1/payins');
    assert.equal(firstHeader(request.headers['xenith-api-key']), 'test-api-key');
    assert.equal(firstHeader(request.headers['xenith-request-timestamp']), '2026-10-05T02:03:04.567Z');
    assert.match(firstHeader(request.headers['x-idempotency-key']) ?? '', /^ts-[\da-f-]{36}$/);

    let body = '';
    request.setEncoding('utf8');
    request.on('data', (chunk: string) => {
      body += chunk;
    });
    request.on('end', () => {
      const payload: unknown = JSON.parse(body);
      assert.ok(payload && typeof payload === 'object');
      assert.equal((payload as Record<string, unknown>).initiatedAmount, 10_000);
      assert.equal((payload as Record<string, unknown>).paymentChannel, 'BCA.VA');
      assert.equal((payload as Record<string, unknown>).customerName, 'Andi');
      assert.equal(
        firstHeader(request.headers['xenith-request-signature']),
        generateSignature(
          'POST',
          '/v1/payins',
          firstHeader(request.headers['xenith-request-timestamp']) ?? '',
          body,
          'test-secret',
        ),
      );
      response.writeHead(200, { 'Content-Type': 'application/json' });
      response.end(JSON.stringify({ paymentId: 'pay_123', status: 'PENDING', paymentCode: '123456' }));
    });
  });

  const client = new WeselAjaClient(config(baseUrl), {
    now: () => new Date('2026-10-05T02:03:04.567Z'),
  });
  const payment = await client.createPayment({
    amount: 10_000,
    customerName: ' Andi ',
    bank: 'BCA',
    referenceCode: 'ORDER_1',
  });
  assert.equal(payment.success, true);
  assert.equal(payment.paymentId, 'pay_123');
  assert.equal(payment.status, 'PENDING');
});

test('returns an explicit API error for non-success status', async () => {
  await listen((_request, response) => {
    response.writeHead(400, { 'Content-Type': 'application/json' });
    response.end(JSON.stringify({ message: 'invalid payment' }));
  });
  const client = new WeselAjaClient(config(baseUrl));
  await assert.rejects(
    client.createPayment({ amount: 10_000, customerName: 'Andi', bank: 'BRI' }),
    (error: unknown) =>
      error instanceof WeselAjaApiError &&
      error.statusCode === 400 &&
      error.messageFromApi === 'invalid payment',
  );
});

test('rejects invalid payment input without making an API request', async () => {
  const client = new WeselAjaClient(config());
  await assert.rejects(
    client.createPayment({ amount: 0, customerName: 'Andi', bank: 'BCA' }),
    /positive safe integer/,
  );
  await assert.rejects(
    client.createPayment({
      amount: 10_000,
      customerName: 'Andi',
      bank: 'BCA',
      referenceCode: 'BAD REF',
    }),
    /Reference values/,
  );
});
