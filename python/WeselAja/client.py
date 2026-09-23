import sys
import json
import os
import uuid
import base64
import hashlib
import hmac
from datetime import datetime, timezone
from urllib import request, error


PAYMENT_URI = "/v1/payins"


def generate_signature(
    method: str,
    uri: str,
    timestamp: str,
    body: str,
    secret_key: str
) -> str:
    payload = (
        f"{method}\n"
        f"{uri}\n"
        f"{timestamp}\n"
        f"{body}"
    )

    digest = hmac.new(
        secret_key.encode("utf-8"),
        payload.encode("utf-8"),
        hashlib.sha256
    ).digest()

    return base64.b64encode(digest).decode("utf-8")


def get_config():
    enabled = (
        os.getenv("XENITH_ENABLED", "true")
        .strip()
        .lower()
        == "true"
    )

    api_key = os.getenv(
        "XENITH_API_KEY",
        ""
    ).strip()

    secret_key = os.getenv(
        "XENITH_SECRET_KEY",
        ""
    ).strip()

    api_url = (
        os.getenv(
            "WESELAJA_API_URL",
            "https://sandbox.checkout.weselaja.id"
        )
        .strip()
        .rstrip("/")
    )

    return enabled, api_key, secret_key, api_url


def error_response(message):
    return {
        "success": False,
        "paymentId": None,
        "initiatedAmount": None,
        "paymentAmount": None,
        "feeAmount": None,
        "currency": None,
        "paymentMethod": None,
        "paymentChannel": None,
        "paymentCode": None,
        "paymentCodeType": None,
        "referenceCode": None,
        "customerReference": None,
        "customerName": None,
        "status": None,
        "createdTime": None,
        "updatedTime": None,
        "expirationTime": None,
        "description": None,
        "callbackUrl": None,
        "redirectUrl": None,
        "payerAccountName": None,
        "payerAccountNumber": None,
        "payerPaymentChannel": None,
        "metadata": {},
        "error": message
    }


def create_payment(arguments):
    if not isinstance(arguments, dict):
        return error_response(
            "arguments must be an object."
        )

    enabled, api_key, secret_key, api_url = get_config()

    if not enabled:
        return error_response(
            "WeselAja is disabled."
        )

    if not api_key:
        return error_response(
            "WeselAja API key is not configured."
        )

    if not secret_key:
        return error_response(
            "WeselAja secret key is not configured."
        )

    amount = arguments.get("amount")
    currency = arguments.get("currency")
    payment_method = arguments.get("paymentMethod")
    payment_channel = arguments.get("paymentChannel")
    reference_code = arguments.get("referenceCode")
    customer_reference = arguments.get("customerReference")
    customer_name = arguments.get("customerName")
    customer_phone_number = arguments.get("customerPhoneNumber")
    description = arguments.get("description")
    callback_url = arguments.get("callbackUrl")
    redirect_url = arguments.get("redirectUrl")

    if amount is None:
        return error_response(
            "amount is required."
        )

    try:
        amount = int(amount)
    except (TypeError, ValueError):
        return error_response(
            "amount must be a number."
        )

    if amount <= 0:
        return error_response(
            "Payment amount must be greater than zero."
        )

    required_fields = {
        "currency": currency,
        "paymentMethod": payment_method,
        "paymentChannel": payment_channel,
        "referenceCode": reference_code,
        "customerReference": customer_reference,
        "customerName": customer_name,
        "callbackUrl": callback_url,
        "redirectUrl": redirect_url
    }

    for field, value in required_fields.items():
        if value is None:
            return error_response(
                f"{field} is required."
            )

        if isinstance(value, str) and not value.strip():
            return error_response(
                f"{field} is required."
            )

    body_data = {
        "initiatedAmount": amount,
        "currency": str(currency),
        "paymentMethod": str(payment_method),
        "paymentChannel": str(payment_channel),
        "referenceCode": str(reference_code),
        "customerReference": customer_reference,
        "customerName": str(customer_name),
        "callbackUrl": str(callback_url),
        "redirectUrl": str(redirect_url)
    }

    if (
        customer_phone_number is not None
        and str(customer_phone_number).strip()
    ):
        body_data["customerPhoneNumber"] = str(
            customer_phone_number
        )

    if (
        description is not None
        and str(description).strip()
    ):
        body_data["description"] = str(description)

    body = json.dumps(
        body_data,
        separators=(",", ":"),
        ensure_ascii=False
    )

    timestamp = (
        datetime.now(timezone.utc)
        .isoformat(timespec="milliseconds")
        .replace("+00:00", "Z")
    )

    signature = generate_signature(
        "POST",
        PAYMENT_URI,
        timestamp,
        body,
        secret_key
    )

    idempotency_key = (
        "python-" + uuid.uuid4().hex
    )

    url = api_url + PAYMENT_URI

    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json",
        "Xenith-Api-Key": api_key,
        "Xenith-Request-Timestamp": timestamp,
        "Xenith-Request-Signature": signature,
        "X-Idempotency-Key": idempotency_key
    }

    req = request.Request(
        url=url,
        data=body.encode("utf-8"),
        headers=headers,
        method="POST"
    )

    try:
        with request.urlopen(
            req,
            timeout=30
        ) as response:

            response_body = (
                response.read()
                .decode("utf-8")
            )

            status_code = response.status

    except error.HTTPError as exc:

        try:
            response_body = (
                exc.read()
                .decode("utf-8")
            )
        except Exception:
            response_body = ""

        try:
            error_data = json.loads(
                response_body
            )
        except json.JSONDecodeError:
            error_data = None

        if error_data is not None:
            return error_response(
                json.dumps(
                    error_data,
                    ensure_ascii=False
                )
            )

        return error_response(
            response_body
        )

    except error.URLError as exc:
        return error_response(
            f"Unable to connect to WeselAja: {exc.reason}"
        )

    except TimeoutError:
        return error_response(
            "Connection to WeselAja timed out."
        )

    except Exception as exc:
        return error_response(
            str(exc)
        )

    if not response_body.strip():
        return error_response(
            f"Empty response from WeselAja. HTTP {status_code}"
        )

    try:
        weselaja_response = json.loads(
            response_body
        )
    except json.JSONDecodeError:
        return error_response(
            "Invalid JSON response from WeselAja. "
            f"HTTP {status_code}: {response_body}"
        )

    if not isinstance(
        weselaja_response,
        dict
    ):
        return error_response(
            "Unexpected WeselAja response format."
        )

    if status_code < 200 or status_code >= 300:
        api_error = (
            weselaja_response.get("message")
            or weselaja_response.get("error")
            or weselaja_response.get("code")
            or response_body
        )

        if isinstance(api_error, dict):
            api_error = json.dumps(
                api_error,
                ensure_ascii=False
            )

        return error_response(
            str(api_error)
        )

    return {
        "success": True,
        "paymentId": weselaja_response.get("id"),
        "initiatedAmount":
            weselaja_response.get(
                "initiatedAmount"
            ),
        "paymentAmount":
            weselaja_response.get(
                "paymentAmount"
            ),
        "feeAmount":
            weselaja_response.get(
                "feeAmount"
            ),
        "currency":
            weselaja_response.get(
                "currency"
            ),
        "paymentMethod":
            weselaja_response.get(
                "paymentMethod"
            ),
        "paymentChannel":
            weselaja_response.get(
                "paymentChannel"
            ),
        "paymentCode":
            weselaja_response.get(
                "paymentCode"
            ),
        "paymentCodeType":
            weselaja_response.get(
                "paymentCodeType"
            ),
        "referenceCode":
            weselaja_response.get(
                "referenceCode"
            ),
        "customerReference":
            weselaja_response.get(
                "customerReference"
            ),
        "customerName":
            weselaja_response.get(
                "customerName"
            ),
        "status":
            weselaja_response.get(
                "status"
            ),
        "createdTime":
            weselaja_response.get(
                "createdTime"
            ),
        "updatedTime":
            weselaja_response.get(
                "updatedTime"
            ),
        "expirationTime":
            weselaja_response.get(
                "expirationTime"
            ),
        "description":
            weselaja_response.get(
                "description"
            ),
        "callbackUrl":
            weselaja_response.get(
                "callbackUrl"
            ),
        "redirectUrl":
            weselaja_response.get(
                "redirectUrl"
            ),
        "payerAccountName":
            weselaja_response.get(
                "payerAccountName"
            ),
        "payerAccountNumber":
            weselaja_response.get(
                "payerAccountNumber"
            ),
        "payerPaymentChannel":
            weselaja_response.get(
                "payerPaymentChannel"
            ),
        "metadata": (
            weselaja_response.get("metadata")
            if isinstance(
                weselaja_response.get("metadata"),
                dict
            )
            else {}
        ),
        "error": None
    }


def is_active():
    enabled, _, _, _ = get_config()

    return {
        "success": True,
        "active": enabled,
        "error": None
    }


def get_payment_method():
    enabled, _, _, _ = get_config()

    if not enabled:
        return {
            "success": False,
            "id": "weselaja",
            "title": "WeselAja",
            "description": (
                "Pay securely using WeselAja payment gateway."
            ),
            "icon": "",
            "supports": ["products"],
            "error": "WeselAja is disabled."
        }

    return {
        "success": True,
        "id": "weselaja",
        "title": "WeselAja",
        "description": (
            "Pay securely using WeselAja payment gateway."
        ),
        "icon": "",
        "supports": ["products"],
        "error": None
    }


def handle_request(payload):
    if not isinstance(payload, dict):
        return error_response(
            "Request must be a JSON object."
        )

    tool = payload.get("tool")
    arguments = payload.get("arguments", {})

    if tool == "is_weselaja_active":
        return is_active()

    if tool == "get_weselaja_payment_method":
        return get_payment_method()

    if tool == "create_weselaja_payment":
        return create_payment(arguments)

    return error_response(
        f"Unknown tool: {tool}"
    )


def main():
    try:
        raw_bytes = sys.stdin.buffer.read()

        if not raw_bytes:
            print(
                json.dumps(
                    error_response(
                        "No JSON input received."
                    ),
                    ensure_ascii=False
                ),
                flush=True
            )
            return

        try:
            raw_input = (
                raw_bytes
                .decode("utf-8-sig")
                .strip()
            )
        except UnicodeDecodeError:
            print(
                json.dumps(
                    error_response(
                        "Unable to decode input as UTF-8."
                    ),
                    ensure_ascii=False
                ),
                flush=True
            )
            return

        if not raw_input:
            print(
                json.dumps(
                    error_response(
                        "Empty JSON input."
                    ),
                    ensure_ascii=False
                ),
                flush=True
            )
            return

        try:
            payload = json.loads(
                raw_input
            )
        except json.JSONDecodeError as exc:
            print(
                json.dumps(
                    error_response(
                        f"Invalid JSON input: {exc}"
                    ),
                    ensure_ascii=False
                ),
                flush=True
            )
            return

        result = handle_request(
            payload
        )

        print(
            json.dumps(
                result,
                ensure_ascii=False
            ),
            flush=True
        )

    except Exception as exc:
        print(
            json.dumps(
                error_response(
                    f"WeselAja Python runtime error: {exc}"
                ),
                ensure_ascii=False
            ),
            flush=True
        )


if __name__ == "__main__":
    main()