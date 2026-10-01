from __future__ import annotations

import base64
import hashlib
import hmac
import json
import os
import uuid
from dataclasses import dataclass
from datetime import datetime, timezone
from urllib import error, request

from exceptions import (
    WeselAjaApiError,
    WeselAjaConfigurationError,
)
from models import (
    CreatePaymentResponse,
    bank_to_channel,
)


PAYMENT_URI = "/v1/payins"


@dataclass(slots=True)
class WeselAjaConfig:
    api_url: str
    api_key: str
    secret_key: str
    customer_reference: str
    callback_url: str
    redirect_url: str
    currency: str = "IDR"
    payment_method: str = "VIRTUAL_ACCOUNT"

    @classmethod
    def from_env(
        cls,
    ) -> "WeselAjaConfig":
        config = cls(
            api_url=(
                os.getenv(
                    "WESELAJA_API_URL",
                    "https://sandbox.checkout.weselaja.id",
                )
                .strip()
                .rstrip("/")
            ),

            api_key=(
                os.getenv(
                    "WESELAJA_API_KEY",
                    "",
                )
                .strip()
            ),

            secret_key=(
                os.getenv(
                    "WESELAJA_SECRET_KEY",
                    "",
                )
                .strip()
            ),

            customer_reference=(
                os.getenv(
                    "WESELAJA_CUSTOMER_REFERENCE",
                    "",
                )
                .strip()
            ),

            callback_url=(
                os.getenv(
                    "WESELAJA_CALLBACK_URL",
                    "",
                )
                .strip()
            ),

            redirect_url=(
                os.getenv(
                    "WESELAJA_REDIRECT_URL",
                    "",
                )
                .strip()
            ),

            currency=(
                os.getenv(
                    "WESELAJA_CURRENCY",
                    "IDR",
                )
                .strip()
            ),

            payment_method=(
                os.getenv(
                    "WESELAJA_PAYMENT_METHOD",
                    "VIRTUAL_ACCOUNT",
                )
                .strip()
            ),
        )

        config.validate()

        return config

    def validate(self) -> None:
        required = {
            "WESELAJA_API_URL": self.api_url,
            "WESELAJA_API_KEY": self.api_key,
            "WESELAJA_SECRET_KEY": self.secret_key,
            "WESELAJA_CUSTOMER_REFERENCE":
                self.customer_reference,
            "WESELAJA_CALLBACK_URL":
                self.callback_url,
            "WESELAJA_REDIRECT_URL":
                self.redirect_url,
            "WESELAJA_CURRENCY":
                self.currency,
            "WESELAJA_PAYMENT_METHOD":
                self.payment_method,
        }

        missing = [
            key
            for key, value
            in required.items()
            if not value
        ]

        if missing:
            raise WeselAjaConfigurationError(
                "Missing configuration: "
                + ", ".join(missing)
            )


class WeselAjaClient:
    def __init__(
        self,
        config: WeselAjaConfig,
        timeout: float = 30.0,
    ) -> None:
        config.validate()

        self.config = config
        self.timeout = timeout

    @classmethod
    def from_env(
        cls,
        timeout: float = 30.0,
    ) -> "WeselAjaClient":
        return cls(
            WeselAjaConfig.from_env(),
            timeout=timeout,
        )

    def create_payment(
        self,
        *,
        amount: int,
        customer_name: str,
        bank: str,
        customer_phone_number: str | None = None,
        description: str | None = None,
        reference_code: str | None = None,
    ) -> CreatePaymentResponse:

        if amount <= 0:
            raise ValueError(
                "Payment amount must be greater than zero."
            )

        if not customer_name.strip():
            raise ValueError(
                "customer_name is required."
            )

        payment_channel = bank_to_channel(
                bank
            )

        reference = (
            reference_code
            or self._generate_reference(
                customer_name
            )
        )

        body_data = {
            "initiatedAmount":
                amount,

            "currency":
                self.config.currency,

            "paymentMethod":
                self.config.payment_method,

            "paymentChannel":
                payment_channel,

            "referenceCode":
                reference,

            "customerReference":
                self.config.customer_reference,

            "customerName":
                customer_name.strip(),

            "callbackUrl":
                self.config.callback_url,

            "redirectUrl":
                self.config.redirect_url,
        }

        if (
            customer_phone_number
            and customer_phone_number.strip()
        ):
            body_data[
                "customerPhoneNumber"
            ] = (
                customer_phone_number.strip()
            )

        if (
            description
            and description.strip()
        ):
            body_data[
                "description"
            ] = (
                description.strip()
            )

        body = json.dumps(
            body_data,
            separators=(
                ",",
                ":",
            ),
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

        signature = self._generate_signature(
                "POST",
                PAYMENT_URI,
                timestamp,
                body,
            )

        idempotency_key = "python-" + uuid.uuid4().hex

        request_url = (
            self.config.api_url
            + PAYMENT_URI
        )

        headers = {
            "Content-Type":
                "application/json",

            "Accept":
                "application/json",

            "Xenith-Api-Key":
                self.config.api_key,

            "Xenith-Request-Timestamp":
                timestamp,

            "Xenith-Request-Signature":
                signature,

            "X-Idempotency-Key":
                idempotency_key,
        }

        http_request = request.Request(
                url=request_url,
                data=body.encode(
                    "utf-8"
                ),
                headers=headers,
                method="POST",
            )

        try:
            with request.urlopen(
                http_request,
                timeout=self.timeout,
            ) as response:

                status_code = response.status

                response_body = response.read().decode(
                        "utf-8"
                    )

        except error.HTTPError as exc:
            try:
                response_body = exc.read().decode(
                        "utf-8"
                    )
            except Exception:
                response_body = ""

            raise self._api_error_from_response(
                exc.code,
                response_body,
            ) from exc

        except error.URLError as exc:
            raise WeselAjaApiError(
                "Unable to connect to WeselAja: "
                + str(exc.reason)
            ) from exc

        except TimeoutError as exc:
            raise WeselAjaApiError(
                "Connection to WeselAja timed out."
            ) from exc

        if not response_body.strip():
            raise WeselAjaApiError(
                "Empty response from WeselAja. "
                f"HTTP {status_code}"
            )

        try:
            payload = json.loads(
                    response_body
                )
        except json.JSONDecodeError as exc:
            raise WeselAjaApiError(
                "Invalid JSON response from WeselAja. "
                f"HTTP {status_code}: "
                f"{response_body}"
            ) from exc

        if not (
            200 <= status_code < 300
        ):
            raise self._api_error_from_payload(
                status_code,
                payload,
            )

        if not isinstance(
            payload,
            dict,
        ):
            raise WeselAjaApiError(
                "Unexpected response format from WeselAja."
            )

        return CreatePaymentResponse.from_dict(
            payload
        )

    def _generate_signature(
        self,
        method: str,
        uri: str,
        timestamp: str,
        body: str,
    ) -> str:

        payload = (
            f"{method}\n"
            f"{uri}\n"
            f"{timestamp}\n"
            f"{body}"
        )

        digest = hmac.new(
            self.config.secret_key.encode(
                "utf-8"
            ),
            payload.encode(
                "utf-8"
            ),
            hashlib.sha256,
        ).digest()

        return base64.b64encode(
            digest
        ).decode(
            "utf-8"
        )

    @staticmethod
    def _generate_reference(
        customer_name: str,
    ) -> str:

        normalized = "".join(
            character
            for character
            in customer_name.upper()
            if (
                character.isalnum()
                or character in "-_"
            )
        )

        normalized = (
            normalized[:20]
            or "CUSTOMER"
        )

        timestamp = (
            datetime.now(
                timezone.utc
            ).strftime(
                "%Y%m%d%H%M%S"
            )
        )

        return (
            f"MCP-{normalized}-{timestamp}"
        )

    @staticmethod
    def _api_error_from_response(
        status_code: int,
        response_body: str,
    ) -> WeselAjaApiError:

        try:
            payload = json.loads(
                    response_body
                )
        except json.JSONDecodeError:
            payload = response_body

        return (
            WeselAjaClient
            ._api_error_from_payload(
                status_code,
                payload,
            )
        )

    @staticmethod
    def _api_error_from_payload(
        status_code: int,
        payload: object,
    ) -> WeselAjaApiError:

        message: object | None = None

        if isinstance(
            payload,
            dict,
        ):
            message = (
                payload.get("message")
                or payload.get("error")
                or payload.get("code")
            )

        if message is None:
            message = payload or (
                    f"HTTP {status_code}"
                )

        if isinstance(
            message,
            dict,
        ):
            message = json.dumps(
                    message,
                    ensure_ascii=False,
                )

        return WeselAjaApiError(
            str(message),
            status_code=status_code,
            details=payload,
        )