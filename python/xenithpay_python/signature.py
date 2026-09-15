import base64
import hashlib
import hmac


class SignatureGenerator:
    def generate(
        self,
        method: str,
        uri: str,
        timestamp: str,
        body: str,
        secret_key: str,
    ) -> str:
        payload = (
            f"{method}\n"
            f"{uri}\n"
            f"{timestamp}\n"
            f"{body}"
        )

        digest = hmac.new(
            secret_key.encode("utf-8"),
            payload.encode("utf-8"),
            hashlib.sha256,
        ).digest()

        return base64.b64encode(digest).decode("utf-8")