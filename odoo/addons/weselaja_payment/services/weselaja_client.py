import base64
import hashlib
import hmac
import json
import uuid
from datetime import datetime, timezone

import requests


PAYMENT_URI = "/v1/payins"


class WeselAjaClient:

    def __init__(self, provider):
        self.provider = provider

        self.api_url = (
            provider.weselaja_api_url or ""
        ).rstrip("/")

        self.api_key = (
            provider.weselaja_api_key or ""
        )

        self.secret_key = (
            provider.weselaja_secret_key or ""
        )

    def is_configured(self):
        return bool(
            self.api_url
            and self.api_key
            and self.secret_key
        )

    @staticmethod
    def generate_signature(
        method,
        uri,
        timestamp,
        body,
        secret_key,
    ):
        payload = (
            f"{method}\n"
            f"{uri}\n"
            f"{timestamp}\n"
            f"{body}"
        )

        digest = hmac.new(
            secret_key.encode("utf-8"),
            payload.encode("utf-8"),
            hashlib.sha256,
        ).digest()

        return base64.b64encode(
            digest
        ).decode("utf-8")

    def create_payment(self, arguments):

        if not self.is_configured():
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
                "error":
                    "WeselAja provider is not configured.",
            }

        if not isinstance(arguments, dict):
            return {
                "success": False,
                "error":
                    "arguments must be an object.",
            }

        required_fields = [
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

        for field in required_fields:

            value = arguments.get(field)

            if value is None:
                return {
                    "success": False,
                    "error":
                        f"{field} is required.",
                }

            if (
                isinstance(value, str)
                and not value.strip()
            ):
                return {
                    "success": False,
                    "error":
                        f"{field} is required.",
                }

        try:
            amount = int(arguments["amount"])
        except (
            TypeError,
            ValueError,
        ):
            return {
                "success": False,
                "error":
                    "amount must be a number.",
            }

        if amount <= 0:
            return {
                "success": False,
                "error":
                    "Payment amount must be greater than zero.",
            }

        body_data = {
            "initiatedAmount": amount,
            "currency":
                str(arguments["currency"]),
            "paymentMethod":
                str(arguments["paymentMethod"]),
            "paymentChannel":
                str(arguments["paymentChannel"]),
            "referenceCode":
                str(arguments["referenceCode"]),
            "customerReference":
                arguments["customerReference"],
            "customerName":
                str(arguments["customerName"]),
            "callbackUrl":
                str(arguments["callbackUrl"]),
            "redirectUrl":
                str(arguments["redirectUrl"]),
        }

        customer_phone_number = arguments.get(
                "customerPhoneNumber"
            )

        if (
            customer_phone_number is not None
            and str(
                customer_phone_number
            ).strip()
        ):
            body_data[
                "customerPhoneNumber"
            ] = str(
                customer_phone_number
            )

        description = arguments.get("description")

        if (
            description is not None
            and str(description).strip()
        ):
            body_data[
                "description"
            ] = str(description)

        body = json.dumps(
                body_data,
                separators=(",", ":"),
                ensure_ascii=False,
            )

        timestamp = (
                datetime.now(
                    timezone.utc
                )
                .isoformat(
                    timespec="milliseconds"
                )
                .replace(
                    "+00:00",
                    "Z",
                )
            )

        signature = self.generate_signature(
                "POST",
                PAYMENT_URI,
                timestamp,
                body,
                self.secret_key,
            )

        idempotency_key ="odoo-" + uuid.uuid4().hex

        url = self.api_url + PAYMENT_URI

        headers = {
            "Content-Type":
                "application/json",

            "Accept":
                "application/json",

            "Xenith-Api-Key":
                self.api_key,

            "Xenith-Request-Timestamp":
                timestamp,

            "Xenith-Request-Signature":
                signature,

            "X-Idempotency-Key":
                idempotency_key,
        }

        try:
            response = requests.post(
                    url,
                    data=body.encode("utf-8"),
                    headers=headers,
                    timeout=30,
                )

        except requests.RequestException as exc:

            return {
                "success": False,
                "error":
                    "Unable to connect to WeselAja: "
                    f"{exc}",
            }

        try:
            response_data = response.json()

        except ValueError:

            return {
                "success": False,
                "error":
                    "Invalid JSON response from WeselAja. "
                    f"HTTP {response.status_code}: "
                    f"{response.text}",
            }

        if not isinstance(
            response_data,
            dict,
        ):
            return {
                "success": False,
                "error":
                    "Unexpected WeselAja response format.",
            }

        if (
            response.status_code < 200
            or response.status_code >= 300
        ):
            api_error = (
                response_data.get("message")
                or response_data.get("error")
                or response_data.get("code")
                or response.text
            )

            if isinstance(
                api_error,
                dict,
            ):
                api_error = json.dumps(
                        api_error,
                        ensure_ascii=False,
                    )

            return {
                "success": False,
                "error":
                    str(api_error),
            }

        return {
            "success": True,

            "paymentId":
                response_data.get("id"),

            "initiatedAmount":
                response_data.get(
                    "initiatedAmount"
                ),

            "paymentAmount":
                response_data.get(
                    "paymentAmount"
                ),

            "feeAmount":
                response_data.get(
                    "feeAmount"
                ),

            "currency":
                response_data.get(
                    "currency"
                ),

            "paymentMethod":
                response_data.get(
                    "paymentMethod"
                ),

            "paymentChannel":
                response_data.get(
                    "paymentChannel"
                ),

            "paymentCode":
                response_data.get(
                    "paymentCode"
                ) or response_data.get(
                    "paymentUrl"
                ),

            "paymentCodeType":
                response_data.get(
                    "paymentCodeType"
                ),

            "referenceCode":
                response_data.get(
                    "referenceCode"
                ),

            "customerReference":
                response_data.get(
                    "customerReference"
                ),

            "customerName":
                response_data.get(
                    "customerName"
                ),

            "status":
                response_data.get(
                    "status"
                ),

            "createdTime":
                response_data.get(
                    "createdTime"
                ),

            "updatedTime":
                response_data.get(
                    "updatedTime"
                ),

            "expirationTime":
                response_data.get(
                    "expirationTime"
                ),

            "description":
                response_data.get(
                    "description"
                ),

            "callbackUrl":
                response_data.get(
                    "callbackUrl"
                ),

            "redirectUrl":
                response_data.get(
                    "redirectUrl"
                ),

            "payerAccountName":
                response_data.get(
                    "payerAccountName"
                ),

            "payerAccountNumber":
                response_data.get(
                    "payerAccountNumber"
                ),

            "payerPaymentChannel":
                response_data.get(
                    "payerPaymentChannel"
                ),

            "metadata":
                response_data.get(
                    "metadata"
                )
                or {},

            "error":
                None,
        }