import json

from odoo import http
from odoo.http import request

from ..services.xenithpay_client import XenithPayClient


class XenithPayMcpController(http.Controller):

    @http.route(
        "/xenithpay/mcp",
        type="http",
        auth="bearer",
        methods=["POST"],
        csrf=False,
    )
    def xenithpay_mcp(self, **kwargs):
        if not request.env.user.has_group(
            "base.group_system"
        ):
            return request.make_json_response(
                {
                    "success": False,
                    "error": (
                        "Only an Odoo system user "
                        "can use the XenithPay MCP implementation."
                    ),
                },
                status=403,
            )

        raw_body = (
            request.httprequest.data
            or b""
        )

        if not raw_body:
            return request.make_json_response(
                {
                    "success": False,
                    "error": "Empty request body.",
                },
                status=400,
            )

        try:
            payload = json.loads(
                raw_body.decode(
                    "utf-8-sig"
                )
            )
        except (
            UnicodeDecodeError,
            json.JSONDecodeError,
        ) as exc:
            return request.make_json_response(
                {
                    "success": False,
                    "error": (
                        "Invalid JSON input: "
                        f"{exc}"
                    ),
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
                    "error": (
                        "Request must be a JSON object."
                    ),
                },
                status=400,
            )

        tool = payload.get(
            "tool"
        )

        arguments = payload.get(
            "arguments",
            {},
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
                        "xenithpay",
                    ),
                ],
                limit=1,
            )
        )

        if not provider:
            return request.make_json_response(
                {
                    "success": False,
                    "error": (
                        "XenithPay provider "
                        "is not installed in Odoo."
                    ),
                },
                status=404,
            )

        if tool == "is_xenithpay_active":

            active = (
                provider.state
                in (
                    "test",
                    "enabled",
                )
            )

            return request.make_json_response(
                {
                    "success": True,
                    "active": active,
                    "error": None,
                }
            )

        if tool == "get_xenithpay_payment_method":

            return request.make_json_response(
                {
                    "success": True,

                    "id":
                        "xenithpay",

                    "title":
                        provider.name
                        or "XenithPay",

                    "description":
                        "Pay securely using "
                        "XenithPay payment gateway.",

                    "icon":
                        "",

                    "supports": [
                        "products"
                    ],

                    "error": None,
                }
            )

        if tool == "create_xenithpay_payment":

            if not isinstance(
                arguments,
                dict,
            ):
                return request.make_json_response(
                    {
                        "success": False,
                        "error": (
                            "arguments "
                            "must be an object."
                        ),
                    },
                    status=400,
                )

            if provider.state not in (
                "test",
                "enabled",
            ):
                return request.make_json_response(
                    {
                        "success": False,
                        "error": (
                            "XenithPay provider "
                            "is disabled in Odoo."
                        ),
                    }
                )

            client = XenithPayClient(
                provider
            )

            result = client.create_payment(
                arguments
            )

            return request.make_json_response(
                result
            )

        return request.make_json_response(
            {
                "success": False,
                "error":
                    f"Unknown tool: {tool}",
            },
            status=400,
        )