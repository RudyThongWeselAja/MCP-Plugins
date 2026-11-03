# Android Direct Payin manual test checklist

- [ ] Add Retrofit, Gson converter, OkHttp, and lifecycle runtime KTX dependencies.
- [ ] Merge Internet permission and the two activity declarations into AndroidManifest.xml.
- [ ] Configure sandbox API key, API secret, customer reference, fee bearer, and any required settlement JSON.
- [ ] Confirm no production credentials are committed to source control.
- [ ] Build the Android project.
- [ ] Open `WeselAjaDemoActivity` and submit a valid IDR amount, customer name, phone (optional), and VA channel.
- [ ] Confirm the request goes directly to `POST /v2/payins` on the WeselAja middleware.
- [ ] Confirm request headers are `X-API-Key`, `X-Timestamp`, and `X-Signature`.
- [ ] Confirm the response displays Payin ID, reference, payment channel, status, and payment code when returned.
- [ ] Use Check Payment Status and confirm it calls `GET /v2/payins/{id}`.
- [ ] If a payment URL is returned, open it in the generated WebView activity.
- [ ] If no payment URL is returned, confirm the Virtual Account payment code is displayed instead.
- [ ] Confirm the app never treats a redirect/deep-link status parameter as payment proof.
- [ ] Confirm callback handling is not expected in the Android app; a public server endpoint is required for webhooks.
- [ ] Confirm no API secret is included in a public/distributed production APK.