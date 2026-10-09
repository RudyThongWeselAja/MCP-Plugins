# weselaja-javascript

Standalone JavaScript integration for the WeselAja payment provider. It uses Node.js built-ins only; no third-party packages are required.

## Scope

- Create virtual-account payments
- HMAC-SHA256 request signing
- Bank-to-channel mapping
- Response parsing, validation, and API error handling
- Environment configuration and runnable example

Callback handling, redirect handling, and payment lookup are not included.

## Requirements

- Node.js 18+

## Configuration

Copy `.env.example` to `.env` and add the merchant credentials and application URLs:

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

Values already present in the process environment take precedence over `.env`. The API URL defaults to the WeselAja sandbox; currency and payment method default to `IDR` and `VIRTUAL_ACCOUNT`. Never commit `.env`.

## Example

After providing valid credentials and URLs, run:

```sh
node example.js
```

This example initiates a real payment.

```js
const { WeselAjaClient } = require('./src');

async function main() {
  const client = WeselAjaClient.fromEnvironment();
  const payment = await client.createPayment({
    amount: 10000,
    customerName: 'Andi',
    bank: 'BCA',
  });
  console.log(payment.status);
}

main().catch(console.error);
```

`createPayment` accepts `amount` (positive safe integer in the smallest currency unit), `customerName`, and `bank`. Optional values are `customerPhoneNumber`, `description`, and `referenceCode`.

## Supported banks

| Bank | WeselAja channel |
|---|---|
| BRI | `BRI.VA` |
| Mandiri | `MDR.VA` |
| BNI | `BNI.VA` |
| Permata | `PTB.VA` |
| CIMB | `CIMB.VA` |
| BCA | `BCA.VA` |

Common aliases such as `Bank Mandiri`, `CIMB Niaga`, and `Bank BCA` are accepted.

## Authentication

Signature input is the newline-separated sequence `METHOD`, `URI`, `TIMESTAMP`, and the exact request body. The signature is HMAC-SHA256 encoded as standard Base64. Requests include the `Xenith-Api-Key`, `Xenith-Request-Timestamp`, `Xenith-Request-Signature`, and `X-Idempotency-Key` headers.

## Tests

```sh
npm test
```
