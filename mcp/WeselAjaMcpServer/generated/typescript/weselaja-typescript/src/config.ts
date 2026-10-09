import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';

export class ConfigurationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ConfigurationError';
  }
}

export interface ConfigOptions {
  apiUrl: string;
  apiKey: string;
  secretKey: string;
  customerReference: string;
  callbackUrl: string;
  redirectUrl: string;
  currency?: string;
  paymentMethod?: string;
}

export class WeselAjaConfig {
  readonly apiUrl: string;
  readonly apiKey: string;
  readonly secretKey: string;
  readonly customerReference: string;
  readonly callbackUrl: string;
  readonly redirectUrl: string;
  readonly currency: string;
  readonly paymentMethod: string;

  constructor(options: ConfigOptions) {
    this.apiUrl = options.apiUrl;
    this.apiKey = options.apiKey;
    this.secretKey = options.secretKey;
    this.customerReference = options.customerReference;
    this.callbackUrl = options.callbackUrl;
    this.redirectUrl = options.redirectUrl;
    this.currency = options.currency ?? 'IDR';
    this.paymentMethod = options.paymentMethod ?? 'VIRTUAL_ACCOUNT';
  }

  static async fromEnvironment(envFile = resolve(process.cwd(), '.env')): Promise<WeselAjaConfig> {
    await loadEnv(envFile);
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

  validate(): void {
    const required: ReadonlyArray<readonly [string, string]> = [
      ['WESELAJA_API_URL', this.apiUrl],
      ['WESELAJA_API_KEY', this.apiKey],
      ['WESELAJA_SECRET_KEY', this.secretKey],
      ['WESELAJA_CUSTOMER_REFERENCE', this.customerReference],
      ['WESELAJA_CALLBACK_URL', this.callbackUrl],
      ['WESELAJA_REDIRECT_URL', this.redirectUrl],
      ['WESELAJA_CURRENCY', this.currency],
      ['WESELAJA_PAYMENT_METHOD', this.paymentMethod],
    ];
    const missing = required
      .filter(([, value]) => value.trim() === '')
      .map(([name]) => name);
    if (missing.length > 0) {
      throw new ConfigurationError(`Missing configuration: ${missing.join(', ')}`);
    }

    for (const [name, value] of [
      ['WESELAJA_API_URL', this.apiUrl],
      ['WESELAJA_CALLBACK_URL', this.callbackUrl],
      ['WESELAJA_REDIRECT_URL', this.redirectUrl],
    ] as const) {
      let parsed: URL;
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

export async function loadEnv(envFile = resolve(process.cwd(), '.env')): Promise<void> {
  let contents: string;
  try {
    contents = await readFile(envFile, 'utf8');
  } catch (error) {
    if (isNodeError(error) && error.code === 'ENOENT') return;
    const detail = error instanceof Error ? error.message : String(error);
    throw new ConfigurationError(`Unable to read environment file '${envFile}': ${detail}`);
  }

  for (const [index, sourceLine] of contents.split(/\r?\n/).entries()) {
    const line = sourceLine.trim();
    if (line === '' || line.startsWith('#')) continue;

    const separator = line.indexOf('=');
    if (separator <= 0) {
      throw new ConfigurationError(`Invalid environment entry at ${envFile}:${index + 1}.`);
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

function environmentValue(name: string, fallback = ''): string {
  const value = process.env[name];
  return value && value.trim() !== '' ? value.trim() : fallback;
}

function isNodeError(error: unknown): error is NodeJS.ErrnoException {
  return error instanceof Error && 'code' in error;
}
