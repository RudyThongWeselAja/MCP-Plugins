from __future__ import annotations
import json
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse
REDIRECT_PATH = "/payment/weselaja/return"
class RedirectHandler(BaseHTTPRequestHandler):
    def do_GET(self) -> None:
        parsed = urlparse(self.path)
        if parsed.path != REDIRECT_PATH:
            self.send_error(
                404,
                "Not Found",
            )
            return
        parameters = {
            key: values[-1] if values else ""
            for key, values in parse_qs(
                parsed.query,
                keep_blank_values=True,
            ).items()
        }
        body = json.dumps(
            {
                "success": True,
                "path": parsed.path,
                "parameters": parameters,
            },
            ensure_ascii=False,
        ).encode("utf-8")
        self.send_response(200)
        self.send_header(
            "Content-Type",
            "application/json; charset=utf-8",
        )
        self.send_header(
            "Content-Length",
            str(len(body)),
        )
        self.end_headers()
        self.wfile.write(body)
    def log_message(
        self,
        format: str,
        *args: object,
    ) -> None:
        return
def main() -> None:
    port = int(
        os.getenv(
            "WESELAJA_REDIRECT_PORT",
            "8080",
        )
    )
    server = ThreadingHTTPServer(
        ("0.0.0.0", port),
        RedirectHandler,
    )
    print(
        "WeselAja redirect handler listening on "
        f"http://0.0.0.0:{port}{REDIRECT_PATH}"
    )
    server.serve_forever()
if __name__ == "__main__":
    main()