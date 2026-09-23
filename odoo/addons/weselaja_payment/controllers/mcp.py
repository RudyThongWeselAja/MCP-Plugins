import json

from odoo import http
from odoo.http import request

from ..services.weselaja_client import WeselAjaClient


class WeselAjaMcpController(http.Controller):

    @http.route(
        "/weselaja/mcp",
        type="http",
        auth="bearer",
        methods=["POST"],
        csrf=False,
    )
    def weselaja_mcp(self, **kwargs):
        if not request.env.user.has_group(
            "base.group_system"
        ):
            return request.make_json_response(
                {
                    "success": False,
                    "error": (
                        "Only an Odoo system user "
                        "can use the WeselAja MCP implementation."
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
                raw_body.decode("utf-8-sig")
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

        if not isinstance(payload, dict):
            return request.make_json_response(
                {
                    "success": False,
                    "error": (
                        "Request must be a JSON object."
                    ),
                },
                status=400,
            )

        tool = payload.get("tool")
        arguments = payload.get(
            "arguments",
            {},
        )

        provider = (
            request.env["payment.provider"]
            .sudo()
            .search(
                [("code", "=", "weselaja")],
                limit=1,
            )
        )

        if not provider:
            return request.make_json_response(
                {
                    "success": False,
                    "error": (
                        "WeselAja provider is not "
                        "installed in Odoo."
                    ),
                },
                status=404,
            )

        if tool == "is_weselaja_active":
            active = provider.state in (
                "test",
                "enabled",
            )

            return request.make_json_response(
                {
                    "success": True,
                    "active": active,
                    "error": None,
                }
            )

        if tool == "get_weselaja_payment_method":
            return request.make_json_response(
                {
                    "success": True,
                    "id": "weselaja",
                    "title": (
                        provider.name
                        or "WeselAja"
                    ),
                    "description": (
                        "Pay securely using "
                        "WeselAja payment gateway."
                    ),
                    "icon": "",
                    "supports": ["products"],
                    "error": None,
                }
            )

        if tool == "create_weselaja_payment":

            if not isinstance(
                arguments,
                dict,
            ):
                return request.make_json_response(
                    {
                        "success": False,
                        "error": (
                            "arguments must be "
                            "an object."
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
                            "WeselAja provider "
                            "is disabled in Odoo."
                        ),
                    }
                )

            client = WeselAjaClient(
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
                "error": f"Unknown tool: {tool}",
            },
            status=400,
        )