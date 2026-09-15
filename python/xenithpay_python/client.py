import json
import os
import uuid
from datetime import datetime, timezone
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from signature import SignatureGenerator


class XenithPayClient:
    PAYMENT_URI = "/v1/payins"

    def __init__(self):
        self.api_url = (
            os.getenv(
                "XENITH_API_URL"
            )
            or "https://openapi.sandbox.xenithpay.com"
        )

        self.api_key = (
            os.getenv("XENITH_API_KEY")
            or ""
        )

        self.secret_key = (
            os.getenv("XENITH_SECRET_KEY")
            or ""
        )

        self.signature_generator = (
            SignatureGenerator()
        )

    def create_payment(
        self,
        arguments: dict,
    ) -> dict:
        self._validate_configuration()
        self._validate_arguments(arguments)

        payload = {
            "initiatedAmount": arguments["amount"],
            "currency": arguments["currency"],
            "paymentMethod": arguments["paymentMethod"],
            "paymentChannel": arguments["paymentChannel"],
            "referenceCode": arguments["referenceCode"],
            "customerReference": arguments[
                "customerReference"
            ],
            "customerName": arguments[
                "customerName"
            ],
            "callbackUrl": arguments[
                "callbackUrl"
            ],
            "redirectUrl": arguments[
                "redirectUrl"
            ],
        }

        customer_phone = arguments.get(
            "customerPhoneNumber"
        )

        if customer_phone:
            payload["customerPhoneNumber"] = (
                customer_phone
            )

        description = arguments.get(
            "description"
        )

        if description:
            payload["description"] = description

        metadata = arguments.get(
            "metadata"
        )

        if metadata:
            payload["metadata"] = metadata

        body = json.dumps(
            payload,
            ensure_ascii=False,
            separators=(",", ":"),
        )

        timestamp = (
            datetime.now(timezone.utc)
            .isoformat(timespec="milliseconds")
            .replace("+00:00", "Z")
        )

        signature = (
            self.signature_generator.generate(
                "POST",
                self.PAYMENT_URI,
                timestamp,
                body,
                self.secret_key,
            )
        )

        idempotency_key = str(
            uuid.uuid4()
        )

        request_url = (
            f"{self.api_url.rstrip('/')}"
            f"{self.PAYMENT_URI}"
        )

        request = Request(
            request_url,
            data=body.encode("utf-8"),
            method="POST",
            headers={
                "Content-Type": "application/json",
                "Accept": "application/json",
                "Xenith-Api-Key": self.api_key,
                "Xenith-Request-Timestamp": timestamp,
                "Xenith-Request-Signature": signature,
                "X-Idempotency-Key": idempotency_key,
            },
        )

        print(
            "========== PYTHON → XENITHPAY ==========",
            file=__import__("sys").stderr,
        )

        print(
            f"URL         : {request_url}",
            file=__import__("sys").stderr,
        )

        print(
            f"Timestamp   : {timestamp}",
            file=__import__("sys").stderr,
        )

        print(
            f"Body        : {body}",
            file=__import__("sys").stderr,
        )

        print(
            f"Idempotency : {idempotency_key}",
            file=__import__("sys").stderr,
        )

        print(
            "=========================================",
            file=__import__("sys").stderr,
        )

        try:
            with urlopen(
                request,
                timeout=30,
            ) as response:
                response_body = (
                    response.read()
                    .decode("utf-8")
                )

                status_code = response.status

        except HTTPError as error:
            response_body = (
                error.read()
                .decode("utf-8")
            )

            status_code = error.code

        except URLError as error:
            raise RuntimeError(
                f"Unable to connect to XenithPay: "
                f"{error.reason}"
            ) from error

        print(
            file=__import__("sys").stderr
        )

        print(
            "========== XENITHPAY RESPONSE ==========",
            file=__import__("sys").stderr,
        )

        print(
            f"HTTP Status : {status_code}",
            file=__import__("sys").stderr,
        )

        print(
            f"Body        : {response_body}",
            file=__import__("sys").stderr,
        )

        print(
            "=========================================",
            file=__import__("sys").stderr,
        )

        if not response_body.strip():
            return {
                "success": False,
                "error": (
                    "Empty response from "
                    f"XenithPay. HTTP {status_code}"
                ),
            }

        try:
            data = json.loads(
                response_body
            )
        except json.JSONDecodeError:
            return {
                "success": False,
                "error": (
                    "Invalid JSON response from "
                    f"XenithPay. HTTP {status_code}: "
                    f"{response_body}"
                ),
            }

        if status_code < 200 or status_code >= 300:
            return {
                "success": False,
                "error": self._extract_error(
                    data,
                    response_body,
                ),
            }

        return self._normalize_payment_response(
            data
        )

    def _validate_configuration(self):
        if not self.api_key:
            raise RuntimeError(
                "XenithPay API key is not configured."
            )

        if not self.secret_key:
            raise RuntimeError(
                "XenithPay secret key is not configured."
            )

        if not self.api_url:
            raise RuntimeError(
                "XenithPay API URL is not configured."
            )

    def _validate_arguments(
        self,
        arguments: dict,
    ):
        required = [
            "amount",
            "currency",
            "paymentMethod",
            "paymentChannel",
            "referenceCode",
            "customerReference",
            "customerName",
            "callbackUrl",
            "redirectUrl",
        ]

        for field in required:
            value = arguments.get(field)

            if value is None:
                raise ValueError(
                    f"{field} is required."
                )

            if isinstance(value, str):
                if not value.strip():
                    raise ValueError(
                        f"{field} is required."
                    )

        if arguments["amount"] <= 0:
            raise ValueError(
                "Payment amount must be greater than zero."
            )

    def _extract_error(
        self,
        data: dict,
        raw_body: str,
    ) -> str:
        if isinstance(data, dict):
            message = data.get("message")

            if message:
                return str(message)

            error = data.get("error")

            if error:
                if isinstance(error, str):
                    return error

                return json.dumps(
                    error,
                    ensure_ascii=False,
                )

        return raw_body

    def _normalize_payment_response(
        self,
        data: dict,
    ) -> dict:
        return {
            "success": True,
            "paymentId": data.get("id"),
            "status": data.get("status"),
            "paymentUrl": data.get("paymentCode"),
            "paymentCodeType": data.get(
                "paymentCodeType"
            ),
            "amount": data.get(
                "initiatedAmount"
            ),
            "paymentAmount": data.get(
                "paymentAmount"
            ),
            "feeAmount": data.get(
                "feeAmount"
            ),
            "currency": data.get(
                "currency"
            ),
            "paymentMethod": data.get(
                "paymentMethod"
            ),
            "paymentChannel": data.get(
                "paymentChannel"
            ),
            "referenceCode": data.get(
                "referenceCode"
            ),
            "customerReference": data.get(
                "customerReference"
            ),
            "customerName": data.get(
                "customerName"
            ),
            "redirectUrl": data.get(
                "redirectUrl"
            ),
            "error": None,
        }