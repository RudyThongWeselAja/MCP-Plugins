# WeselAja Android Kotlin Direct Payin

Android Kotlin integration sample that calls the WeselAja middleware directly with Retrofit. It does not call or require an application backend.

## What is generated

- Retrofit API for `POST /v2/payins` and `GET /v2/payins/{id}`.
- OkHttp interceptor that signs the exact HTTP method, encoded path, UTC timestamp, and raw request body.
- Direct Payin request models for Virtual Account payments.
- A simple native Android form for amount, customer name, phone number, and bank.
- A result panel for status, reference, payment ID, and Virtual Account payment code.
- A WebView activity. It opens a checkout URL only when the Direct Payin response actually contains a supported URL field.
- Manual testing checklist.

## Important security warning

This direct-from-Android mode requires the WeselAja API key and API secret to be available inside the Android process so the app can sign requests. Values compiled into an APK can be extracted by a determined user, even when placed in BuildConfig, resources, native libraries, or obfuscated code.

Use this implementation for sandbox testing, internal demonstrations, or a controlled environment only. Do not distribute an APK containing a production API secret to the public. For production, use a trusted backend or a provider-supported client-safe credential/token flow.

The Android app cannot receive a server-to-server webhook because a device is not a publicly addressable webhook endpoint. This sample checks payment status by calling `GET /v2/payins/{id}` from the app. A verified webhook still requires a public server endpoint.

## Requirements

- An existing Android project using Kotlin.
- Android SDK with WebView support.
- Internet permission.
- Retrofit, Gson converter, OkHttp, and AndroidX lifecycle runtime KTX.

## Dependencies

Add compatible versions to the app module's `build.gradle.kts`:

```kotlin
dependencies {
    implementation("com.squareup.retrofit2:retrofit:2.11.0")
    implementation("com.squareup.retrofit2:converter-gson:2.11.0")
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.7")
}
```

Use versions compatible with the Android project if it already manages dependencies through a version catalog.

## Configure the middleware

Edit `WeselAjaConfig.kt`:

- `BASE_URL`: sandbox `https://sandbox.checkout.weselaja.id/` or the production base URL supplied for the merchant.
- `API_KEY`: WeselAja API key.
- `API_SECRET`: WeselAja API secret used to calculate `X-Signature`.
- `CUSTOMER_REFERENCE`: merchant customer reference.
- `CALLBACK_URL`: optional public callback URL if the merchant/API requires one. It must point to a real public server endpoint; an Android activity cannot receive a webhook.
- `REDIRECT_URL`: optional return URL configured for the merchant. Set it only to a URL accepted by the middleware configuration.
- `SETTLEMENTS_JSON`: optional JSON array for merchants that must explicitly specify settlement accounts. Use the exact structure and account identifiers supplied by WeselAja; do not guess these values.
- `PAYIN_FEE_BEARER`: use the value configured for the merchant (sample default is `MERCHANT`).

Do not commit real credentials. Remember that moving credentials to a local Kotlin file only avoids committing them; it does not protect them from extraction from a distributed APK.

## Add files to your Android app

1. Copy the generated `android/src/main/java/com/weselaja/integration` Kotlin files into the app module's source set.
2. Keep the package `com.weselaja.integration`, or update all package declarations and file locations together.
3. Add the dependencies above.
4. Merge the entries from `AndroidManifest.snippet.xml` into the existing manifest. Do not create a duplicate activity declaration. Add the Internet permission outside the `<application>` element.
5. Open `WeselAjaDemoActivity` from your app's existing navigation or set it as the launcher activity for a standalone demo.
6. Configure the sandbox credentials and merchant settings in `WeselAjaConfig.kt`.
7. Build and run on a device or emulator with network access.

## Direct Payin flow

1. The user fills the form and chooses a bank channel.
2. `WeselAjaPaymentRepository` creates a reference and idempotency key.
3. Retrofit sends `POST /v2/payins` directly to the WeselAja middleware.
4. `WeselAjaSigningInterceptor` signs the exact outgoing request using HMAC-SHA256 and sends `X-API-Key`, `X-Timestamp`, and `X-Signature`.
5. The app displays the payment code and current status.
6. The user can tap Check Status to call `GET /v2/payins/{id}`.
7. If the response includes a checkout URL (`paymentLinkUrl`, `paymentUrl`, or `checkoutUrl`), the app can open it in `WeselAjaWebViewActivity`. A Direct Virtual Account response may contain only a payment code; in that case there is no hosted checkout page to show, so the app displays the VA code instead.

## Callback and status

No callback server is generated. Direct Android-to-middleware integration uses status polling from the app. If the merchant requires callback notifications, configure a public HTTPS webhook endpoint outside the Android application and verify the webhook signature there. Never trust a status passed back through a deep link or WebView URL as proof of payment.

## Documentation

- [Create Payin Direct](https://docs.checkout.weselaja.id/payment#create-payin-direct)
- [WeselAja Middleware Documentation](https://docs.checkout.weselaja.id/)

## Troubleshooting

- HTTP 401/403: verify sandbox/production credentials, timestamp format, API secret, and request-signature format.
- HTTP 400: inspect the middleware's validation message. Merchants with multiple settlement accounts may need the exact `settlements` array provided by WeselAja.
- Network failure: confirm the emulator/device has Internet access and that the base URL is correct.
- No WebView button: the Direct Payin response did not include a hosted checkout URL. Use the returned Virtual Account payment code and check status through the middleware.
