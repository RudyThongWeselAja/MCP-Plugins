from odoo import fields, models


class PaymentProvider(models.Model):
    _inherit = "payment.provider"

    code = fields.Selection(
        selection_add=[
            ("weselaja", "WeselAja"),
        ],
        ondelete={
            "weselaja": "set default",
        },
    )

    weselaja_api_key = fields.Char(
        string="API Key",
        required_if_provider="weselaja",
        copy=False,
        help="WeselAja API key.",
    )

    weselaja_secret_key = fields.Char(
        string="Secret Key",
        required_if_provider="weselaja",
        copy=False,
        help="WeselAja API secret key.",
    )

    weselaja_webhook_secret = fields.Char(
        string="Webhook Secret",
        required_if_provider="weselaja",
        copy=False,
        help="WeselAja webhook signature secret.",
    )

    weselaja_api_url = fields.Char(
        string="API URL",
        required_if_provider="weselaja",
        copy=False,
        default="https://openapi.sandbox.xenithpay.com",
        help="WeselAja API base URL.",
    )

    weselaja_payment_method = fields.Selection(
        selection=[
            ("VIRTUAL_ACCOUNT", "VIRTUAL_ACCOUNT"),
        ],
        string="Payment Method",
        required_if_provider="weselaja",
        default="VIRTUAL_ACCOUNT",
        copy=False,
        help="WeselAja payment method.",
    )

    weselaja_payment_channel = fields.Selection(
        selection=[
            ("BRI.VA", "BRI.VA"),
        ],
        string="Payment Channel",
        required_if_provider="weselaja",
        default="BRI.VA",
        copy=False,
        help="WeselAja payment channel.",
    )

    def _get_redirect_form_view(
        self,
        is_validation=False,
    ):
        self.ensure_one()

        if self.code == "weselaja":
            return self.env.ref(
                "weselaja_payment.weselaja_redirect_form"
            )

        return super()._get_redirect_form_view(
            is_validation=is_validation
        )