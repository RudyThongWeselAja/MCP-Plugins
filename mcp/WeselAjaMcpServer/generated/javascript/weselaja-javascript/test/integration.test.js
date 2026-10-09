'use strict';

const assert = require('node:assert/strict');
const { createServer } = require('node:http');
const { once } = require('node:events');
const { after, test } = require('node:test');
const {
  WeselAjaApiError,
  WeselAjaClient,
  WeselAjaConfig,
  generateSignature,
  resolveBankChannel,
} = require('../src');

const server = createServer();
let baseUrl;

after(() => server.close());

async function listen(handler) {
  server.removeAllListeners('request');
  server.on('request', handler);
  if (!server.listening) {
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    baseUrl = `http://127.0.0.1:${server.address().port}`;
  }
}

function config(apiUrl = 'https://example.test') {
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

test('validates configuration', () => {
  assert.throws(() => config('ftp://invalid.example').validate(), /HTTP or HTTPS URL/);
  assert.throws(
    () => new WeselAjaConfig({ apiUrl: 'https://example.test' }).validate(),
    /Missing configuration/,
  );
});

test('creates a signed payment request and parses the response', async () => {
  await listen((req, res) => {
    assert.equal(req.method, 'POST');
    assert.equal(req.url, '/v1/payins');
    assert.equal(req.headers['xenith-api-key'], 'test-api-key');
    assert.equal(req.headers['xenith-request-timestamp'], '2026-10-05T02:03:04.567Z');
    assert.match(req.headers['x-idempotency-key'], /^js-[\da-f-]{36}$/);

    let body = '';
    req.setEncoding('utf8');
    req.on('data', (chunk) => {
      body += chunk;
    });
    req.on('end', () => {
      const request = JSON.parse(body);
      assert.equal(request.initiatedAmount, 10000);
      assert.equal(request.paymentChannel, 'BCA.VA');
      assert.equal(request.customerName, 'Andi');
      assert.equal(
        req.headers['xenith-request-signature'],
        generateSignature('POST', '/v1/payins', req.headers['xenith-request-timestamp'], body, 'test-secret'),
      );
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ paymentId: 'pay_123', status: 'PENDING', paymentCode: '123456' }));
    });
  });

  const client = new WeselAjaClient(config(baseUrl), {
    now: () => new Date('2026-10-05T02:03:04.567Z'),
  });
  const payment = await client.createPayment({
    amount: 10000,
    customerName: ' Andi ',
    bank: 'BCA',
    referenceCode: 'ORDER_1',
  });
  assert.equal(payment.success, true);
  assert.equal(payment.paymentId, 'pay_123');
  assert.equal(payment.status, 'PENDING');
});

test('returns an explicit API error for non-success status', async () => {
  await listen((_req, res) => {
    res.writeHead(400, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ message: 'invalid payment' }));
  });
  const client = new WeselAjaClient(config(baseUrl));
  await assert.rejects(
    client.createPayment({ amount: 10000, customerName: 'Andi', bank: 'BRI' }),
    (error) =>
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
    client.createPayment({ amount: 10000, customerName: 'Andi', bank: 'BCA', referenceCode: 'BAD REF' }),
    /Reference values/,
  );
});
