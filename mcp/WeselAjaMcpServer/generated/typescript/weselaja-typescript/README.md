# weselaja-typescript

Standalone TypeScript integration for the WeselAja payment provider.

## Scope

- Create virtual-account payments
- HMAC-SHA256 request signing
- Bank-to-channel mapping
- Response parsing, validation, and API error handling
- Environment configuration and runnable example

Callback handling, redirect handling, and payment lookup are not included.

## Requirements

- Node.js 20+
- npm

## Install and build

```sh
npm install
npm run build
```

The SDK uses Node.js built-ins at runtime. TypeScript and Node.js type declarations are development dependencies.

## Configuration

Copy `.env.example` to `.env` and provide the merchant credentials and application URLs:

```env
WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
WESELAJA_API_KEY=
WESELAJA_SECRET_KEY=
WESELAJA_CUSTOMER_REFERENCE=
WESELAJA_CALLBACK_URL=
WESELAJA_REDIRECT_URL=
WESELAJA_CURRENCY=IDR
WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT
```

Existing process environment variables take precedence over `.env`. The API URL defaults to the WeselAja sandbox; currency and payment method default to `IDR` and `VIRTUAL_ACCOUNT`. Never commit `.env`.

## Example

```sh
npm run example
```

This example initiates a real payment when configured with valid credentials.

```ts
import { WeselAjaClient } from './src/index.js';

const client = await WeselAjaClient.fromEnvironment();
const payment = await client.createPayment({
  amount: 10_000,
  customerName: 'Andi',
  bank: 'BCA',
});
```

`amount` is a positive safe integer in the smallest currency unit. `customerPhoneNumber`, `description`, and `referenceCode` are optional.

## Supported banks

| Bank | WeselAja channel |
|---|---|
| BRI | `BRI.VA` |
| Mandiri | `MDR.VA` |
| BNI | `BNI.VA` |
| Permata | `PTB.VA` |
| CIMB | `CIMB.VA` |
| BCA | `BCA.VA` |

Common forms such as `Bank Mandiri`, `CIMB Niaga`, and `Bank BCA` are accepted.

## Authentication

Signature input is the newline-separated sequence `METHOD`, `URI`, `TIMESTAMP`, and exact request body. The signature is HMAC-SHA256 encoded using standard Base64. Requests include `Xenith-Api-Key`, `Xenith-Request-Timestamp`, `Xenith-Request-Signature`, and `X-Idempotency-Key` headers.

## Tests

```sh
npm test
```
