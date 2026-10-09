from dotenv import load_dotenv
import base64
import hashlib
import hmac
import json
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
load_dotenv()
CALLBACK_PATH = "/payment/weselaja/callback"
class CallbackHandler(BaseHTTPRequestHandler):
    def _send_json(
        self,
        status_code: int,
        payload: dict
    ) -> None:
        body = json.dumps(
            payload,
            indent=2
        ).encode("utf-8")
        self.send_response(status_code)
        self.send_header(
            "Content-Type",
            "application/json"
        )
        self.send_header(
            "Content-Length",
            str(len(body))
        )
        self.end_headers()
        self.wfile.write(body)
    def do_POST(self) -> None:
        if self.path.split("?", 1)[0] != CALLBACK_PATH:
            self._send_json(
                404,
                {
                    "success": False,
                    "error": "Not found"
                }
            )
            return
        content_length = int(
            self.headers.get(
                "Content-Length",
                "0"
            )
        )
        raw_body = self.rfile.read(
            content_length
        )
        timestamp = self.headers.get(
            "x-middleware-timestamp",
            ""
        )
        signature = self.headers.get(
            "x-middleware-signature",
            ""
        )
        secret = os.getenv(
            "WEB_SIGNATURE_SECRET",
            ""
        )
        if not secret:
            self._send_json(
                500,
                {
                    "success": False,
                    "error": "WEB_SIGNATURE_SECRET is required."
                }
            )
            return
        if not timestamp or not signature:
            self._send_json(
                401,
                {
                    "success": False,
                    "error": "Missing webhook signature headers."
                }
            )
            return
        signing_payload = (
            "POST\n"
            + CALLBACK_PATH
            + "\n"
            + raw_body.decode("utf-8")
            + "\n"
            + timestamp
        )
        expected = base64.b64encode(
            hmac.new(
                secret.encode("utf-8"),
                signing_payload.encode("utf-8"),
                hashlib.sha256
            ).digest()
        ).decode("ascii")
        if not hmac.compare_digest(
            expected,
            signature
        ):
            self._send_json(
                401,
                {
                    "success": False,
                    "error": "Invalid webhook signature."
                }
            )
            return
        try:
            payload = json.loads(
                raw_body.decode("utf-8")
            )
        except json.JSONDecodeError:
            self._send_json(
                400,
                {
                    "success": False,
                    "error": "Invalid JSON payload."
                }
            )
            return
        self._send_json(
            200,
            {
                "success": True,
                "verified": True,
                "body": payload
            }
        )
    def do_GET(self) -> None:
        if self.path.split("?", 1)[0] != CALLBACK_PATH:
            self._send_json(
                404,
                {
                    "success": False,
                    "error": "Not found"
                }
            )
            return
        self._send_json(
            405,
            {
                "success": False,
                "error": "Method not allowed"
            }
        )
    def log_message(
        self,
        format_string: str,
        *args: object
    ) -> None:
        return
def main() -> None:
    port = int(
        os.getenv(
            "WESELAJA_CALLBACK_PORT",
            "8081"
        )
    )
    server = ThreadingHTTPServer(
        ("0.0.0.0", port),
        CallbackHandler
    )
    print(
        "WeselAja callback handler listening on "
        f"http://0.0.0.0:{port}{CALLBACK_PATH}"
    )
    server.serve_forever()
if __name__ == "__main__":
    main()