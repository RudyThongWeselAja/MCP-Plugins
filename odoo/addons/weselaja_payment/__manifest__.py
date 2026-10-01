{
    "name": "WeselAja Payment Provider",
    "version": "1.1.0",
    "category": "Accounting/Payment Providers",
    "summary": "WeselAja payment provider for Odoo 18",
    "description": """
WeselAja payment provider for Odoo 18.

Provides:
- WeselAja payment provider configuration.
- Native Odoo checkout integration.
- WeselAja payment creation.
- Redirect to WeselAja payment page.
- WeselAja callback handling.
- MCP integration.
""",
    "author": "WeselAja",
    "license": "LGPL-3",

    "depends": [
        "payment",
        "website_payment",
        "website_sale",
    ],

    "data": [
    "data/payment_provider_data.xml",
    "views/payment_provider_views.xml",
    "views/payment_weselaja_templates.xml",
],

    "external_dependencies": {
        "python": [
            "requests",
        ],
    },

    "installable": True,
    "application": False,
}