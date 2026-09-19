import json
import os
import sys
import time
import uuid
from datetime import datetime, timezone
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from signature import SignatureGenerator


class XenithPayClient:
    PAYMENT_URI = "/v1/payins"

    def __init__(self):
        self.api_url = (
            os.getenv("WESELAJA_API_URL")
            or "https://sandbox.checkout.weselaja.id"
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

        idempotency_key = (
            f"python-{uuid.uuid4().hex}"
        )

        request_url = (
            f"{self.api_url.rstrip('/')}"
            f"{self.PAYMENT_URI}"
        )

        headers = {
            "Content-Type": "application/json; charset=utf-8",
            "Accept": "application/json",
            "Xenith-Api-Key": self.api_key,
            "Xenith-Request-Timestamp": timestamp,
            "Xenith-Request-Signature": signature,
            "X-Idempotency-Key": idempotency_key,
        }

        request = Request(
            request_url,
            data=body.encode("utf-8"),
            method="POST",
            headers=headers,
        )

        self._print_request_debug(
            request_url=request_url,
            body=body,
            timestamp=timestamp,
            signature=signature,
            idempotency_key=idempotency_key,
            headers=headers,
        )

        start_time = time.perf_counter()

        response_headers = {}
        status_code = None
        response_body = ""

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

                response_headers = dict(
                    response.headers.items()
                )

        except HTTPError as error:

            response_body = (
                error.read()
                .decode("utf-8")
            )

            status_code = error.code

            response_headers = dict(
                error.headers.items()
            )

        except URLError as error:

            elapsed_ms = int(
                (
                    time.perf_counter()
                    - start_time
                )
                * 1000
            )

            print(
                file=sys.stderr
            )

            print(
                "=" * 50,
                file=sys.stderr,
            )

            print(
                "      XENITHPAY HTTP DEBUG ERROR",
                file=sys.stderr,
            )

            print(
                "=" * 50,
                file=sys.stderr,
            )

            print(
                f"Elapsed          : {elapsed_ms} ms",
                file=sys.stderr,
            )

            print(
                "Exception        : URLError",
                file=sys.stderr,
            )

            print(
                f"Error            : {error.reason}",
                file=sys.stderr,
            )

            print(
                "=" * 50,
                file=sys.stderr,
            )

            raise RuntimeError(
                "Unable to connect to XenithPay: "
                f"{error.reason}"
            ) from error

        elapsed_ms = int(
            (
                time.perf_counter()
                - start_time
            )
            * 1000
        )

        self._print_response_debug(
            status_code=status_code,
            elapsed_ms=elapsed_ms,
            response_body=response_body,
            response_headers=response_headers,
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

    def _print_request_debug(
        self,
        request_url: str,
        body: str,
        timestamp: str,
        signature: str,
        idempotency_key: str,
        headers: dict,
    ):
        print(
            file=sys.stderr,
            flush=True,
        )

        print(
            "=" * 50,
            file=sys.stderr,
            flush=True,
        )

        print(
            "           XENITHPAY HTTP DEBUG REQUEST",
            file=sys.stderr,
            flush=True,
        )

        print(
            "=" * 50,
            file=sys.stderr,
            flush=True,
        )

        print(
            "Method           : POST",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"Base URL         : {self.api_url}",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"Request URL      : {request_url}",
            file=sys.stderr,
            flush=True,
        )

        print(
            "HTTP Version     : 1.1",
            file=sys.stderr,
            flush=True,
        )

        print(
            "Version Policy   : RequestVersionExact",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"Request Body     : {body}",
            file=sys.stderr,
            flush=True,
        )

        print(
            file=sys.stderr,
            flush=True,
        )

        print(
            "Signature Debug:",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"  Signature URI  : {self.PAYMENT_URI}",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"  Timestamp      : {timestamp}",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"  Signature      : {signature}",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"  Idempotency    : {idempotency_key}",
            file=sys.stderr,
            flush=True,
        )

        print(
            file=sys.stderr,
            flush=True,
        )

        print(
            "Request Headers  :",
            file=sys.stderr,
            flush=True,
        )

        for name, value in headers.items():

            display_value = value

            if name.lower() == "xenith-api-key":
                display_value = self._mask_api_key(
                    value
                )

            print(
                f"  {name}: {display_value}",
                file=sys.stderr,
                flush=True,
            )

        print(
            "=" * 50,
            file=sys.stderr,
            flush=True,
        )

    def _print_response_debug(
        self,
        status_code: int,
        elapsed_ms: int,
        response_body: str,
        response_headers: dict,
    ):
        reason = self._http_reason(
            status_code
        )

        print(
            file=sys.stderr,
            flush=True,
        )

        print(
            "=" * 50,
            file=sys.stderr,
            flush=True,
        )

        print(
            "          XENITHPAY HTTP DEBUG RESPONSE",
            file=sys.stderr,
            flush=True,
        )

        print(
            "=" * 50,
            file=sys.stderr,
            flush=True,
        )

        print(
            f"HTTP Status      : "
            f"{status_code} {reason}",
            file=sys.stderr,
            flush=True,
        )

        print(
            "HTTP Version     : 1.1",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"Elapsed          : {elapsed_ms} ms",
            file=sys.stderr,
            flush=True,
        )

        print(
            "Content-Type     : "
            f"{response_headers.get('Content-Type', '')}",
            file=sys.stderr,
            flush=True,
        )

        print(
            "Content-Length   : "
            f"{response_headers.get('Content-Length', '')}",
            file=sys.stderr,
            flush=True,
        )

        print(
            f"Response Body Len: "
            f"{len(response_body.encode('utf-8'))}",
            file=sys.stderr,
            flush=True,
        )

        print(
            file=sys.stderr,
            flush=True,
        )

        print(
            "Response Headers:",
            file=sys.stderr,
            flush=True,
        )

        for name, value in response_headers.items():
            print(
                f"  {name}: {value}",
                file=sys.stderr,
                flush=True,
            )

        print(
            file=sys.stderr,
            flush=True,
        )

        print(
            "RAW RESPONSE BODY:",
            file=sys.stderr,
            flush=True,
        )

        print(
            "-" * 50,
            file=sys.stderr,
            flush=True,
        )

        print(
            response_body,
            file=sys.stderr,
            flush=True,
        )

        print(
            "-" * 50,
            file=sys.stderr,
            flush=True,
        )

        print(
            "=" * 50,
            file=sys.stderr,
            flush=True,
        )

    def _mask_api_key(
        self,
        api_key: str,
    ) -> str:
        if not api_key:
            return ""

        if len(api_key) <= 6:
            return "***"

        return (
            api_key[:3]
            + "***"
            + api_key[-3:]
        )

    def _http_reason(
        self,
        status_code: int,
    ) -> str:
        reasons = {
            200: "OK",
            201: "Created",
            202: "Accepted",
            204: "No Content",
            400: "Bad Request",
            401: "Unauthorized",
            403: "Forbidden",
            404: "Not Found",
            409: "Conflict",
            422: "Unprocessable Entity",
            429: "Too Many Requests",
            500: "Internal Server Error",
            502: "Bad Gateway",
            503: "Service Unavailable",
            504: "Gateway Timeout",
        }

        return reasons.get(
            status_code,
            "",
        )

    def _validate_configuration(
        self,
    ):
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
            value = arguments.get(
                field
            )

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
            message = data.get(
                "message"
            )

            if message:
                return str(message)

            error = data.get(
                "error"
            )

            if error:
                if isinstance(
                    error,
                    str,
                ):
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
