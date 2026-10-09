# weselaja-go

Standalone Go integration for the WeselAja payment provider. The project uses only the Go standard library.

## Scope

- Create virtual-account payments
- HMAC-SHA256 request signing
- Bank-to-channel mapping
- Response parsing, validation, and API error handling
- Environment configuration and a runnable example

Callback handling, redirect handling, and payment lookup are not included.

## Requirements

- Go 1.22+

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

Values already present in the process environment take precedence over `.env`. The API URL defaults to the WeselAja sandbox; currency and payment method default to `IDR` and `VIRTUAL_ACCOUNT`. Never commit `.env`.

## Example

The runnable example creates a real payment when configured with valid credentials:

```sh
go run ./cmd/example
```

```go
config, err := weselaja.ConfigFromEnvironment()
if err != nil {
    return err
}
client, err := weselaja.NewClient(config)
if err != nil {
    return err
}

payment, err := client.CreatePayment(weselaja.CreatePaymentRequest{
    Amount:       10000,
    CustomerName: "Andi",
    Bank:         "BCA",
})
```

## Supported banks

| Bank | WeselAja channel |
|---|---|
| BRI | `BRI.VA` |
| Mandiri | `MDR.VA` |
| BNI | `BNI.VA` |
| Permata | `PTB.VA` |
| CIMB | `CIMB.VA` |
| BCA | `BCA.VA` |

Accepted names include common forms such as `Bank Mandiri`, `CIMB Niaga`, and `Bank BCA`.

## Authentication

Signature input is the newline-separated sequence `METHOD`, `URI`, `TIMESTAMP`, and exact request body. The signature is HMAC-SHA256, encoded using standard Base64. Requests use `Xenith-Api-Key`, `Xenith-Request-Timestamp`, `Xenith-Request-Signature`, and `X-Idempotency-Key`.

## Tests

```sh
go test ./...
```
