{
    "name": "WeselAja Payment Provider",
    "version": "1.0.0",
    "category": "Accounting/Payment Providers",
    "summary": "WeselAja implementation for Odoo 18",
    "description": """
WeselAja implementation for Odoo 18.

Provides WeselAja payment provider configuration
and an HTTP interface for WeselAja MCP integration.
""",
    "author": "WeselAja",
    "license": "LGPL-3",

    "depends": [
        "payment",
    ],

    "data": [
        "data/payment_provider_data.xml",
        "views/payment_provider_views.xml",
    ],

    "installable": True,
    "application": False,
}