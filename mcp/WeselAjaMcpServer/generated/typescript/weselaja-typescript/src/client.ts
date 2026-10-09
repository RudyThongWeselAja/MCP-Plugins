import { randomUUID } from 'node:crypto';
import { resolveBankChannel } from './bank-channels.js';
import { WeselAjaConfig } from './config.js';
import type { ClientOptions, CreatePaymentRequest, CreatePaymentResponse } from './models.js';
import { generateSignature } from './signature.js';

const PAYMENT_URI = '/v1/payins';
const MAX_RESPONSE_BYTES = 1_048_576;
const REFERENCE_PATTERN = /^[A-Za-z0-9_-]+$/;

export class WeselAjaApiError extends Error {
  constructor(
    readonly statusCode: number,
    readonly body: string,
    readonly messageFromApi: string,
  ) {
    super(`WeselAja API returned HTTP ${statusCode}: ${messageFromApi}`);
    this.name = 'WeselAjaApiError';
  }
}

export function validateReference(value: string): void {
  if (value.trim() === '') throw new TypeError('Reference value is required.');
  if (!REFERENCE_PATTERN.test(value)) {
    throw new TypeError('Reference values may only contain letters, numbers, "-" and "_".');
  }
}

export function generateReference(customerName: string, now: Date): string {
  let normalized = customerName.toUpperCase().replace(/[^A-Z0-9_-]/g, '').slice(0, 20);
  if (normalized === '') normalized = 'CUSTOMER';
  const timestamp = now.toISOString().replace(/[-:TZ.]/g, '').slice(0, 14);
  return `WES-${normalized}-${timestamp}`;
}

export class WeselAjaClient {
  private readonly fetchImpl: typeof fetch;
  private readonly now: () => Date;

  constructor(
    private readonly config: WeselAjaConfig,
    options: ClientOptions = {},
  ) {
    config.validate();
    this.fetchImpl = options.fetchImpl ?? globalThis.fetch;
    if (typeof this.fetchImpl !== 'function') {
      throw new TypeError('A Fetch API implementation is required.');
    }
    this.now = options.now ?? (() => new Date());
  }

  static async fromEnvironment(options?: ClientOptions): Promise<WeselAjaClient> {
    return new WeselAjaClient(await WeselAjaConfig.fromEnvironment(), options);
  }

  async createPayment(request: CreatePaymentRequest): Promise<CreatePaymentResponse> {
    if (!Number.isSafeInteger(request.amount) || request.amount <= 0) {
      throw new TypeError('Payment amount must be a positive safe integer.');
    }
    if (request.customerName.trim() === '') throw new TypeError('Customer name is required.');
    if (request.bank.trim() === '') throw new TypeError('Bank is required.');

    const paymentChannel = resolveBankChannel(request.bank);
    const referenceCode = request.referenceCode?.trim()
      ? request.referenceCode.trim()
      : generateReference(request.customerName, this.now());
    validateReference(referenceCode);
    validateReference(this.config.customerReference);

    const payload: Record<string, string | number> = {
      initiatedAmount: request.amount,
      currency: this.config.currency,
      paymentMethod: this.config.paymentMethod,
      paymentChannel,
      referenceCode,
      customerReference: this.config.customerReference,
      customerName: request.customerName.trim(),
      callbackUrl: this.config.callbackUrl,
      redirectUrl: this.config.redirectUrl,
    };
    if (request.customerPhoneNumber?.trim()) {
      payload.customerPhoneNumber = request.customerPhoneNumber.trim();
    }
    if (request.description?.trim()) payload.description = request.description.trim();

    const body = JSON.stringify(payload);
    const timestamp = this.now().toISOString();
    const signature = generateSignature('POST', PAYMENT_URI, timestamp, body, this.config.secretKey);
    let response: Response;
    try {
      response = await this.fetchImpl(
        `${this.config.apiUrl.replace(/\/+$/, '')}${PAYMENT_URI}`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
            'Xenith-Api-Key': this.config.apiKey,
            'Xenith-Request-Timestamp': timestamp,
            'Xenith-Request-Signature': signature,
            'X-Idempotency-Key': `ts-${randomUUID()}`,
          },
          body,
          signal: AbortSignal.timeout(30_000),
          redirect: 'manual',
        },
      );
    } catch (error) {
      const detail = error instanceof Error ? error.message : String(error);
      throw new Error(`Unable to connect to WeselAja: ${detail}`, { cause: error });
    }

    const responseBody = await readResponseBody(response);
    if (!responseBody.trim()) {
      throw new Error(`Empty response from WeselAja. HTTP ${response.status}`);
    }
    if (!response.ok) {
      throw new WeselAjaApiError(
        response.status,
        responseBody,
        apiErrorMessage(responseBody),
      );
    }

    let decoded: unknown;
    try {
      decoded = JSON.parse(responseBody);
    } catch (error) {
      const detail = error instanceof Error ? error.message : String(error);
      throw new Error(`Invalid JSON response from WeselAja. HTTP ${response.status}: ${detail}`, {
        cause: error,
      });
    }
    if (!decoded || typeof decoded !== 'object' || Array.isArray(decoded)) {
      throw new Error('Unexpected WeselAja response format.');
    }
    return { ...decoded, success: true } as CreatePaymentResponse;
  }
}

async function readResponseBody(response: Response): Promise<string> {
  if (!response.body) return '';
  const reader = response.body.getReader();
  const chunks: Uint8Array[] = [];
  let totalBytes = 0;
  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      totalBytes += value.byteLength;
      if (totalBytes > MAX_RESPONSE_BYTES) {
        await reader.cancel();
        throw new Error('WeselAja response exceeded the 1 MiB size limit.');
      }
      chunks.push(value);
    }
  } catch (error) {
    if (error instanceof Error && error.message.includes('1 MiB size limit')) throw error;
    const detail = error instanceof Error ? error.message : String(error);
    throw new Error(`Unable to read WeselAja response: ${detail}`, { cause: error });
  } finally {
    reader.releaseLock();
  }
  const bytes = new Uint8Array(totalBytes);
  let offset = 0;
  for (const chunk of chunks) {
    bytes.set(chunk, offset);
    offset += chunk.byteLength;
  }
  return new TextDecoder().decode(bytes);
}

function apiErrorMessage(body: string): string {
  try {
    const decoded: unknown = JSON.parse(body);
    if (decoded && typeof decoded === 'object' && !Array.isArray(decoded)) {
      const response = decoded as Record<string, unknown>;
      for (const key of ['message', 'error', 'code']) {
        const value = response[key];
        if (typeof value === 'string' && value.trim() !== '') return value;
      }
    }
  } catch {
    return body.trim();
  }
  return body.trim();
}
