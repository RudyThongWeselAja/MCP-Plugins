# weselaja-python
Python integration client for WeselAja.
## Features
- Create WeselAja virtual-account payments
- Bank-to-channel mapping
- HMAC-SHA256 request signature
- Idempotency key generation
- Structured payment response
- Automatic `.env` loading
- Python package metadata
- Unit tests
- Standalone redirect handler
- Standalone callback handler
- GET payin history
- No credentials embedded in source code
## Supported Banks
| Bank | WeselAja Channel |
|---|---|
| BRI | `BRI.VA` |
| Mandiri | `MDR.VA` |
| BNI | `BNI.VA` |
| Permata | `PTB.VA` |
| CIMB | `CIMB.VA` |
| BCA | `BCA.VA` |
## Requirements
- Python 3.11+
- pip
## Installation
It is recommended to create a virtual environment:
```bash
python -m venv .venv
```
Windows:
```powershell
.venv\Scripts\activate
```
Linux/macOS:
```bash
source .venv/bin/activate
```
Install the project:
```bash
pip install -e .
```
Or install dependencies directly:
```bash
pip install -r requirements.txt
```
## Environment configuration
Copy:
```text
.env.example
```
to:
```text
.env
```
Then fill in:
```env
WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
WESELAJA_API_KEY=
WESELAJA_SECRET_KEY=
WESELAJA_CUSTOMER_REFERENCE=
WESELAJA_CALLBACK_URL=
WESELAJA_REDIRECT_URL=
WESELAJA_CURRENCY=IDR
WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT
WESELAJA_CALLBACK_PORT=8081
WEB_SIGNATURE_SECRET=
WESELAJA_REDIRECT_PORT=8080
```
The generated client automatically loads `.env`.
Never commit `.env`.
## Create payment
```python
from weselaja_client import WeselAjaClient
client = WeselAjaClient.from_env()
payment = client.create_payment(
    amount=10_000,
    customer_name="Andi",
    bank="BCA",
)
print(payment.status)
print(payment.payment_code)
print(payment.reference_code)
print(payment.payment_channel)
```
## Bank mapping
The client accepts human-readable names:
```text
BRI
Mandiri
BNI
Permata
CIMB
BCA
```
And maps them internally to the WeselAja channels:
```text
BRI       -> BRI.VA
Mandiri   -> MDR.VA
BNI       -> BNI.VA
Permata   -> PTB.VA
CIMB      -> CIMB.VA
BCA       -> BCA.VA
```
## Run example
After configuring `.env`:
```bash
python example.py
```
This creates a sandbox payment.
## Get payins
Retrieve payment sessions from the merchant account.
Optional filters:
- `status`: `PENDING`, `SUCCESS`, `EXPIRED`, or `FAILED`
- `limit`: maximum number of records
```python
payins = client.get_payins(
    status="SUCCESS",
    limit=50,
)
print(payins)
```
This uses `GET /v1/payins` with the same HMAC-SHA256 signing flow.
## Run redirect handler
The generated redirect handler listens on:
```text
/payment/weselaja/return
```
Start it with:
```bash
python redirect_server.py
```
Set `WESELAJA_REDIRECT_PORT` when a different local port is required.
The handler returns the received query parameters as JSON. Do not use
the browser redirect itself as payment confirmation; use the payment
status API or the callback handler for server-side confirmation.
## Run callback handler
The generated callback handler listens on:
```text
/payment/weselaja/callback
```
Start it with:
```bash
python callback_server.py
```
Set `WESELAJA_CALLBACK_PORT` when a different local port is required.
Configure `WEB_SIGNATURE_SECRET` with the webhook signature secret.
The handler verifies `x-middleware-timestamp` and
`x-middleware-signature` before parsing the JSON body.
Valid callbacks return HTTP 200. Invalid signatures return HTTP 401.
## Run tests
```bash
python -m unittest discover -s tests
```
## API
The generated client calls:
```text
POST /v1/payins
GET /v1/payins
POST /payment/weselaja/callback
```
The request uses the HMAC-SHA256 signing mechanism used by the
validated WeselAja integration.
Generated source code does not contain API credentials.