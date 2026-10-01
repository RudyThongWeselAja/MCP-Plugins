import logging

from odoo import _, models
from odoo.exceptions import ValidationError

from ..services.weselaja_client import WeselAjaClient


_logger = logging.getLogger(__name__)


class PaymentTransaction(models.Model):
    _inherit = "payment.transaction"

    def _get_specific_processing_values(
        self,
        processing_values,
    ):
        processing_values = (
            super()._get_specific_processing_values(
                processing_values
            )
        )

        if self.provider_code != "weselaja":
            return processing_values

        self.ensure_one()

        provider = self.provider_id

        client = WeselAjaClient(
            provider
        )

        base_url = (
            self.env[
                "ir.config_parameter"
            ]
            .sudo()
            .get_param(
                "web.base.url"
            )
        )

        if not base_url:
            raise ValidationError(
                _(
                    "Odoo web.base.url is not configured."
                )
            )

        base_url = base_url.rstrip("/")

        callback_url = (
            f"{base_url}"
            "/payment/weselaja/notification"
        )

        return_url = (
            f"{base_url}"
            "/payment/weselaja/return"
        )

        customer_reference = (
            self.partner_id.ref
            or f"ODOO-{self.partner_id.id}"
        )

        customer_phone = (
            self.partner_id.phone
            or self.partner_id.mobile
            or ""
        )

        customer_name = (
            self.partner_id.name
            or "Odoo Customer"
        )

        if len(customer_name.strip()) < 5:
            customer_name = (
                f"{customer_name.strip()} Customer"
            ).strip()

        arguments = {
            "amount": int(
                round(
                    self.amount
                )
            ),

            "currency":
                self.currency_id.name,

            "paymentMethod":
                provider.weselaja_payment_method,

            "paymentChannel":
                provider.weselaja_payment_channel,

            "referenceCode":
                self.reference,

            "customerReference":
                customer_reference,

            "customerName":
                customer_name,

            "customerPhoneNumber":
                customer_phone,

            "description":
                self.reference,

            "callbackUrl":
                callback_url,

            "redirectUrl":
                return_url,
        }

        result = client.create_payment(
            arguments
        )

        if not result.get("success"):
            raise ValidationError(
                _(
                    "WeselAja payment creation failed: %s"
                )
                % (
                    result.get("error")
                    or "Unknown error"
                )
            )

        payment_id = result.get(
            "paymentId"
        )

        payment_url = (
            result.get("paymentUrl")
            or result.get("checkoutUrl")
            or result.get("paymentPageUrl")
            or result.get("paymentLink")
            or result.get("paymentCode")
        )

        if not payment_url:
            raise ValidationError(
                _(
                    "Payment was created, "
                    "but WeselAja did not return "
                    "a hosted payment URL."
                )
            )

        self.provider_reference = (
            payment_id
            or self.reference
        )

        processing_values.update(
            {
                "weselaja_payment_id":
                    payment_id,

                "weselaja_payment_url":
                    payment_url,

                "weselaja_status":
                    result.get(
                        "status"
                    ),
            }
        )

        return processing_values

    def _get_specific_rendering_values(
        self,
        processing_values,
    ):
        rendering_values = (
            super()._get_specific_rendering_values(
                processing_values
            )
        )

        if self.provider_code != "weselaja":
            return rendering_values

        rendering_values.update(
            {
                "payment_url":
                    processing_values.get(
                        "weselaja_payment_url"
                    ),
            }
        )

        return rendering_values

    def _get_tx_from_notification_data(
        self,
        provider_code,
        notification_data,
    ):
        if provider_code != "weselaja":
            return super()._get_tx_from_notification_data(
                provider_code,
                notification_data,
            )

        data = notification_data.get(
            "data"
        )

        if not isinstance(
            data,
            dict,
        ):
            raise ValidationError(
                _(
                    "Invalid WeselAja webhook data."
                )
            )

        reference = (
            data.get(
                "referenceCode"
            )
            or data.get(
                "reference"
            )
            or data.get(
                "merchantReference"
            )
        )

        payment_id = (
            data.get(
                "id"
            )
            or data.get(
                "paymentId"
            )
            or data.get(
                "session"
            )
        )

        tx = self.env[
            "payment.transaction"
        ]

        if reference:
            tx = self.search(
                [
                    (
                        "reference",
                        "=",
                        reference,
                    ),
                    (
                        "provider_code",
                        "=",
                        "weselaja",
                    ),
                ],
                limit=1,
            )

        if (
            not tx
            and payment_id
        ):
            tx = self.search(
                [
                    (
                        "provider_reference",
                        "=",
                        payment_id,
                    ),
                    (
                        "provider_code",
                        "=",
                        "weselaja",
                    ),
                ],
                limit=1,
            )

        if not tx:
            raise ValidationError(
                _(
                    "WeselAja transaction "
                    "could not be found. "
                    "Reference: %s, Payment ID: %s"
                )
                % (
                    reference or "-",
                    payment_id or "-",
                )
            )

        return tx

    def _process_notification_data(
        self,
        notification_data,
    ):
        super()._process_notification_data(
            notification_data
        )

        if self.provider_code != "weselaja":
            return

        self.ensure_one()

        data = notification_data.get(
            "data"
        )

        if not isinstance(
            data,
            dict,
        ):
            raise ValidationError(
                _(
                    "Invalid WeselAja webhook data."
                )
            )

        payment_id = (
            data.get(
                "id"
            )
            or data.get(
                "paymentId"
            )
            or data.get(
                "session"
            )
        )

        reference = data.get(
            "referenceCode"
        )

        if payment_id:
            self.provider_reference = (
                payment_id
            )
        elif reference:
            self.provider_reference = (
                reference
            )

        status = str(
            data.get(
                "status"
            )
            or ""
        ).strip().upper()

        state_message = (
            data.get("errorMessage")
            or data.get("description")
            or ""
        )

        if status in {
            "SUCCESS",
            "SUCCEEDED",
            "COMPLETED",
            "DONE",
        }:
            self._set_done(
                state_message=state_message
                or _(
                    "WeselAja payment successful."
                )
            )

        elif status in {
            "PENDING",
            "PROCESSING",
            "ACTIVE",
            "WAITING",
            "CREATED",
        }:
            self._set_pending(
                state_message=state_message
                or _(
                    "WeselAja payment is pending."
                )
            )

        elif status in {
            "EXPIRED",
            "CANCELLED",
            "CANCELED",
        }:
            self._set_canceled(
                state_message=state_message
                or _(
                    "WeselAja payment expired "
                    "or was canceled."
                )
            )

        elif status in {
            "FAILED",
            "FAILURE",
            "ERROR",
            "REVERSED",
            "REFUNDED",
        }:
            self._set_error(
                state_message
                or _(
                    "WeselAja payment status: %s"
                )
                % status
            )

        else:
            _logger.warning(
                "Unknown WeselAja webhook status "
                "%s for transaction %s",
                status,
                self.reference,
            )

            self._set_pending(
                state_message=_(
                    "Unknown WeselAja payment status: %s"
                )
                % status
            )