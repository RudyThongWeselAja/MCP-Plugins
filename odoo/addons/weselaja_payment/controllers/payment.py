import base64
import hashlib
import hmac
import json
import logging
from datetime import datetime, timezone

from odoo import http
from odoo.http import request


_logger = logging.getLogger(__name__)


class WeselAjaPaymentController(http.Controller):

    WEBHOOK_PATH = "/payment/weselaja/notification"
    MAX_TIMESTAMP_AGE = 300

    @staticmethod
    def _parse_timestamp(timestamp):
        if not timestamp:
            return None

        try:
            value = timestamp.strip()

            if value.endswith("Z"):
                value = value[:-1] + "+00:00"

            return datetime.fromisoformat(
                value
            ).astimezone(
                timezone.utc
            )

        except ValueError:
            return None

    @classmethod
    def _is_timestamp_valid(cls, timestamp):
        parsed = cls._parse_timestamp(
            timestamp
        )

        if parsed is None:
            return False

        now = datetime.now(
            timezone.utc
        )

        age = abs(
            (
                now - parsed
            ).total_seconds()
        )

        return age <= cls.MAX_TIMESTAMP_AGE

    @classmethod
    def _build_signature(
        cls,
        method,
        path,
        raw_body,
        timestamp,
        secret,
    ):
        signed_payload = (
            f"{method}"
            "\\n"
            f"{path}"
            "\\n"
            f"{raw_body}"
            "\\n"
            f"{timestamp}"
        )

        digest = hmac.new(
            secret.encode("utf-8"),
            signed_payload.encode("utf-8"),
            hashlib.sha256,
        ).digest()

        return base64.b64encode(
            digest
        ).decode("utf-8")

    @classmethod
    def _verify_signature(
        cls,
        raw_body,
        timestamp,
        received_signature,
        secret,
    ):
        if not timestamp:
            return False

        if not received_signature:
            return False

        if not secret:
            return False

        if not cls._is_timestamp_valid(
            timestamp
        ):
            return False

        expected_signature = (
            cls._build_signature(
                method="POST",
                path=cls.WEBHOOK_PATH,
                raw_body=raw_body,
                timestamp=timestamp,
                secret=secret,
            )
        )

        return hmac.compare_digest(
            expected_signature,
            received_signature.strip(),
        )

    @http.route(
        WEBHOOK_PATH,
        type="http",
        auth="public",
        methods=["POST"],
        csrf=False,
        save_session=False,
    )
    def weselaja_notification(
        self,
        **kwargs,
    ):
        raw_body = (
            request.httprequest.get_data(
                cache=False,
                as_text=True,
            )
        )

        timestamp = (
            request.httprequest.headers.get(
                "X-Xenith-Timestamp"
            )
        )

        received_signature = (
            request.httprequest.headers.get(
                "X-Xenith-Signature"
            )
        )

        provider = (
            request.env[
                "payment.provider"
            ]
            .sudo()
            .search(
                [
                    (
                        "code",
                        "=",
                        "weselaja",
                    ),
                    (
                        "company_id",
                        "=",
                        request.env.company.id,
                    ),
                ],
                limit=1,
            )
        )

        if not provider:
            return request.make_json_response(
                {
                    "success": False,
                    "error":
                        "WeselAja provider not found.",
                },
                status=500,
            )

        is_valid = (
            self._verify_signature(
                raw_body=raw_body,
                timestamp=timestamp,
                received_signature=received_signature,
                secret=provider.weselaja_webhook_secret,
            )
        )

        if not is_valid:
            _logger.warning(
                "Invalid WeselAja webhook signature."
            )

            return request.make_json_response(
                {
                    "success": False,
                    "error":
                        "Invalid webhook signature.",
                },
                status=401,
            )

        try:
            payload = json.loads(
                raw_body or "{}"
            )

        except json.JSONDecodeError:
            return request.make_json_response(
                {
                    "success": False,
                    "error":
                        "Invalid JSON payload.",
                },
                status=400,
            )

        if not isinstance(
            payload,
            dict,
        ):
            return request.make_json_response(
                {
                    "success": False,
                    "error":
                        "Payload must be an object.",
                },
                status=400,
            )

        try:
            tx = (
                request.env[
                    "payment.transaction"
                ]
                .sudo()
                ._handle_notification_data(
                    "weselaja",
                    payload,
                )
            )

            return request.make_json_response(
                {
                    "success": True,
                    "transactionId":
                        tx.id,
                    "reference":
                        tx.reference,
                    "state":
                        tx.state,
                }
            )

        except Exception as exc:
            _logger.exception(
                "WeselAja callback processing failed."
            )

            return request.make_json_response(
                {
                    "success": False,
                    "error":
                        str(exc),
                },
                status=400,
            )

    @http.route(
        "/payment/weselaja/return",
        type="http",
        auth="public",
        methods=["GET"],
        csrf=False,
    )
    def weselaja_return(
        self,
        **kwargs,
    ):
        return request.redirect(
            "/payment/status"
        )