# weselaja-java

Standalone Java 17 integration for the WeselAja payment provider. It has no third-party runtime dependencies.

## Scope

- Create virtual-account payments
- HMAC-SHA256 request signing
- Bank-to-channel mapping
- Response parsing, validation, and API error handling
- Environment configuration and runnable example

Callback handling, redirect handling, and payment lookup are not included.

## Requirements

- JDK 17+
- Maven (optional; the sources also compile directly with `javac`)

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

Process environment variables take precedence over `.env`. The API URL defaults to the WeselAja sandbox; currency and payment method default to `IDR` and `VIRTUAL_ACCOUNT`. Never commit `.env`.

## Build and example

With Maven:

```sh
mvn package
java -cp target/classes id.weselaja.Example
```

The example creates a real payment when configured with valid credentials.

## Usage

```java
WeselAjaClient client = WeselAjaClient.fromEnvironment();
CreatePaymentResponse payment = client.createPayment(
    new CreatePaymentRequest(10_000, "Andi", "BCA"));
System.out.println(payment.paymentCode());
```

The request also supports optional customer phone number, description, and reference code:

```java
new CreatePaymentRequest(10_000, "Andi", "BCA", "+628123456789", "Order description", "ORDER_1");
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

Common names such as `Bank Mandiri`, `CIMB Niaga`, and `Bank BCA` are accepted.

## Authentication

Signature input is the newline-separated sequence `METHOD`, `URI`, `TIMESTAMP`, and exact request body. The signature is HMAC-SHA256 encoded using standard Base64. Requests include `Xenith-Api-Key`, `Xenith-Request-Timestamp`, `Xenith-Request-Signature`, and `X-Idempotency-Key` headers.

## Self-test

Compile and run the dependency-free tests from the project root:

```sh
javac -d out (Get-ChildItem -Recurse src -Filter *.java | ForEach-Object FullName)
java -cp out id.weselaja.SelfTest
```
