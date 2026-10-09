'use strict';

const { randomUUID } = require('node:crypto');
const { resolveBankChannel } = require('./bank-channels');
const { generateSignature } = require('./signature');

const PAYMENT_URI = '/v1/payins';
const REFERENCE_PATTERN = /^[A-Za-z0-9_-]+$/;

class WeselAjaApiError extends Error {
  constructor(statusCode, body, message) {
    super(`WeselAja API returned HTTP ${statusCode}: ${message}`);
    this.name = 'WeselAjaApiError';
    this.statusCode = statusCode;
    this.body = body;
    this.messageFromApi = message;
  }
}

function validateReference(value) {
  if (typeof value !== 'string' || value.trim() === '') {
    throw new TypeError('Reference value is required.');
  }
  if (!REFERENCE_PATTERN.test(value)) {
    throw new TypeError('Reference values may only contain letters, numbers, "-" and "_".');
  }
}

function generateReference(customerName, now) {
  let normalized = customerName.toUpperCase().replace(/[^A-Z0-9_-]/g, '').slice(0, 20);
  if (!normalized) normalized = 'CUSTOMER';
  const timestamp = now.toISOString().replace(/[-:TZ.]/g, '').slice(0, 14);
  return `WES-${normalized}-${timestamp}`;
}

function apiErrorMessage(body) {
  try {
    const decoded = JSON.parse(body);
    for (const key of ['message', 'error', 'code']) {
      if (typeof decoded[key] === 'string' && decoded[key].trim()) return decoded[key];
    }
  } catch {
    // Keep the raw body as the error message when the response isn't JSON.
  }
  return body.trim();
}

class WeselAjaClient {
  constructor(config, { fetchImpl = globalThis.fetch, now = () => new Date() } = {}) {
    config.validate();
    if (typeof fetchImpl !== 'function') {
      throw new TypeError('A Fetch API implementation is required.');
    }
    this.config = config;
    this.fetch = fetchImpl;
    this.now = now;
  }

  static fromEnvironment(options) {
    const { WeselAjaConfig } = require('./config');
    return new WeselAjaClient(WeselAjaConfig.fromEnvironment(), options);
  }

  async createPayment(request) {
    if (!request || !Number.isSafeInteger(request.amount) || request.amount <= 0) {
      throw new TypeError('Payment amount must be a positive safe integer.');
    }
    if (typeof request.customerName !== 'string' || request.customerName.trim() === '') {
      throw new TypeError('Customer name is required.');
    }
    if (typeof request.bank !== 'string' || request.bank.trim() === '') {
      throw new TypeError('Bank is required.');
    }
    const paymentChannel = resolveBankChannel(request.bank);
    const referenceCode =
      typeof request.referenceCode === 'string' && request.referenceCode.trim()
        ? request.referenceCode.trim()
        : generateReference(request.customerName, this.now());
    validateReference(referenceCode);
    validateReference(this.config.customerReference);

    const payload = {
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
    if (typeof request.customerPhoneNumber === 'string' && request.customerPhoneNumber.trim()) {
      payload.customerPhoneNumber = request.customerPhoneNumber.trim();
    }
    if (typeof request.description === 'string' && request.description.trim()) {
      payload.description = request.description.trim();
    }

    const body = JSON.stringify(payload);
    const timestamp = this.now().toISOString();
    const signature = generateSignature('POST', PAYMENT_URI, timestamp, body, this.config.secretKey);
    let response;
    try {
      response = await this.fetch(`${this.config.apiUrl.replace(/\/+$/, '')}${PAYMENT_URI}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
          'Xenith-Api-Key': this.config.apiKey,
          'Xenith-Request-Timestamp': timestamp,
          'Xenith-Request-Signature': signature,
          'X-Idempotency-Key': `js-${randomUUID()}`,
        },
        body,
        signal: AbortSignal.timeout(30_000),
        redirect: 'manual',
      });
    } catch (error) {
      throw new Error(`Unable to connect to WeselAja: ${error.message}`, { cause: error });
    }

    let responseBody;
    try {
      responseBody = await response.text();
    } catch (error) {
      throw new Error(`Unable to read WeselAja response: ${error.message}`, { cause: error });
    }
    if (!responseBody.trim()) {
      throw new Error(`Empty response from WeselAja. HTTP ${response.status}`);
    }
    if (responseBody.length > 1_048_576) {
      throw new Error('WeselAja response exceeded the 1 MiB size limit.');
    }
    if (!response.ok) {
      throw new WeselAjaApiError(response.status, responseBody, apiErrorMessage(responseBody));
    }

    let decoded;
    try {
      decoded = JSON.parse(responseBody);
    } catch (error) {
      throw new Error(`Invalid JSON response from WeselAja. HTTP ${response.status}: ${error.message}`, {
        cause: error,
      });
    }
    if (!decoded || typeof decoded !== 'object' || Array.isArray(decoded)) {
      throw new Error('Unexpected WeselAja response format.');
    }
    return { ...decoded, success: true };
  }
}

module.exports = { WeselAjaApiError, WeselAjaClient, generateReference, validateReference };
