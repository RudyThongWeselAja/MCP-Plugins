'use strict';

const fs = require('node:fs');
const path = require('node:path');

class ConfigurationError extends Error {
  constructor(message) {
    super(message);
    this.name = 'ConfigurationError';
  }
}

function loadEnv(filePath = path.resolve(process.cwd(), '.env')) {
  let contents;
  try {
    contents = fs.readFileSync(filePath, 'utf8');
  } catch (error) {
    if (error.code === 'ENOENT') return;
    throw new ConfigurationError(`Unable to read environment file '${filePath}': ${error.message}`);
  }

  for (const [index, sourceLine] of contents.split(/\r?\n/).entries()) {
    const line = sourceLine.trim();
    if (!line || line.startsWith('#')) continue;

    const separator = line.indexOf('=');
    if (separator <= 0) {
      throw new ConfigurationError(`Invalid environment entry at ${filePath}:${index + 1}.`);
    }
    const key = line.slice(0, separator).trim();
    let value = line.slice(separator + 1).trim();
    if (
      value.length >= 2 &&
      ((value.startsWith('"') && value.endsWith('"')) ||
        (value.startsWith("'") && value.endsWith("'")))
    ) {
      value = value.slice(1, -1);
    }
    if (process.env[key] === undefined) process.env[key] = value;
  }
}

function environmentValue(name, fallback = '') {
  const value = process.env[name];
  return typeof value === 'string' && value.trim() !== '' ? value.trim() : fallback;
}

class WeselAjaConfig {
  constructor({
    apiUrl,
    apiKey,
    secretKey,
    customerReference,
    callbackUrl,
    redirectUrl,
    currency = 'IDR',
    paymentMethod = 'VIRTUAL_ACCOUNT',
  }) {
    this.apiUrl = apiUrl;
    this.apiKey = apiKey;
    this.secretKey = secretKey;
    this.customerReference = customerReference;
    this.callbackUrl = callbackUrl;
    this.redirectUrl = redirectUrl;
    this.currency = currency;
    this.paymentMethod = paymentMethod;
  }

  static fromEnvironment(envFile = path.resolve(process.cwd(), '.env')) {
    loadEnv(envFile);
    const config = new WeselAjaConfig({
      apiUrl: environmentValue('WESELAJA_API_URL', 'https://sandbox.checkout.weselaja.id'),
      apiKey: environmentValue('WESELAJA_API_KEY'),
      secretKey: environmentValue('WESELAJA_SECRET_KEY'),
      customerReference: environmentValue('WESELAJA_CUSTOMER_REFERENCE'),
      callbackUrl: environmentValue('WESELAJA_CALLBACK_URL'),
      redirectUrl: environmentValue('WESELAJA_REDIRECT_URL'),
      currency: environmentValue('WESELAJA_CURRENCY', 'IDR'),
      paymentMethod: environmentValue('WESELAJA_PAYMENT_METHOD', 'VIRTUAL_ACCOUNT'),
    });
    config.validate();
    return config;
  }

  validate() {
    const required = {
      WESELAJA_API_URL: this.apiUrl,
      WESELAJA_API_KEY: this.apiKey,
      WESELAJA_SECRET_KEY: this.secretKey,
      WESELAJA_CUSTOMER_REFERENCE: this.customerReference,
      WESELAJA_CALLBACK_URL: this.callbackUrl,
      WESELAJA_REDIRECT_URL: this.redirectUrl,
      WESELAJA_CURRENCY: this.currency,
      WESELAJA_PAYMENT_METHOD: this.paymentMethod,
    };
    const missing = Object.entries(required)
      .filter(([, value]) => typeof value !== 'string' || value.trim() === '')
      .map(([name]) => name);
    if (missing.length) {
      throw new ConfigurationError(`Missing configuration: ${missing.join(', ')}`);
    }

    for (const [name, value] of [
      ['WESELAJA_API_URL', this.apiUrl],
      ['WESELAJA_CALLBACK_URL', this.callbackUrl],
      ['WESELAJA_REDIRECT_URL', this.redirectUrl],
    ]) {
      let parsed;
      try {
        parsed = new URL(value);
      } catch {
        throw new ConfigurationError(`${name} must be a valid HTTP or HTTPS URL.`);
      }
      if (!['http:', 'https:'].includes(parsed.protocol) || !parsed.hostname) {
        throw new ConfigurationError(`${name} must be a valid HTTP or HTTPS URL.`);
      }
    }
  }
}

module.exports = { ConfigurationError, WeselAjaConfig, loadEnv };
