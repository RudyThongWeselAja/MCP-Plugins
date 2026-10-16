# WeselAja for WooCommerce

WooCommerce payment gateway for WeselAja Virtual Account Pay Ins.

## Requirements

- WordPress 6.2+
- WooCommerce 8.0+
- PHP 8.0+
- A WeselAja API key and secret

## Installation

1. Upload and activate the `weselaja-woocommerce` directory as a WordPress plugin.
2. In **WooCommerce → Settings → Payments**, enable **WeselAja**.
3. Configure the API URL, API key, secret key, bank channel, and webhook secret.
4. Set the WeselAja webhook URL to the URL shown in gateway settings (`/wp-json/weselaja/v1/webhook`).
5. Place a test order and confirm the payment instructions and webhook status handling.

The default API URL is the WeselAja sandbox. Amounts are sent as integer IDR rupiah values, consistent with the WeselAja API convention.

## Payment lifecycle

The plugin creates a Pay In at `POST /v1/payins` using HMAC-SHA256 request signing and an idempotency key. Orders remain on hold until a signed webhook changes the status. The webhook expects `referenceCode` and `status` in the JSON body and verifies the `Xenith-Request-Timestamp` and `Xenith-Request-Signature` headers against the raw body and configured webhook secret.

Supported Virtual Account channels: `BRI.VA`, `MDR.VA`, `BNI.VA`, `PTB.VA`, `CIMB.VA`, and `BCA.VA`. One channel is selected in gateway settings for checkout.

## Development

`examples/payin_payload.json` shows the API payload shape. Run the basic checks with:

```sh
php -l weselaja-woocommerce.php
php tests/test-weselaja.php
```
