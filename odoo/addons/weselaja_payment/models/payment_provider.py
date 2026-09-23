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
        help="WeselAja secret key.",
    )

    weselaja_api_url = fields.Char(
        string="API URL",
        required_if_provider="weselaja",
        copy=False,
        default="https://sandbox.checkout.weselaja.id",
        help="WeselAja API base URL.",
    )