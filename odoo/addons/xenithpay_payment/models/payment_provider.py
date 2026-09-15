from odoo import fields, models


class PaymentProvider(models.Model):
    _inherit = "payment.provider"

    code = fields.Selection(
        selection_add=[
            ("xenithpay", "XenithPay"),
        ],
        ondelete={
            "xenithpay": "set default",
        },
    )

    xenithpay_api_key = fields.Char(
        string="API Key",
        required_if_provider="xenithpay",
        copy=False,
        help="XenithPay API key.",
    )

    xenithpay_secret_key = fields.Char(
        string="Secret Key",
        required_if_provider="xenithpay",
        copy=False,
        help="XenithPay secret key.",
    )

    xenithpay_api_url = fields.Char(
        string="API URL",
        required_if_provider="xenithpay",
        copy=False,
        default="https://openapi.sandbox.xenithpay.com",
        help="XenithPay API base URL.",
    )