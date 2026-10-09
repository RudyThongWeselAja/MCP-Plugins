using System.Text;
namespace WeselAjaMcpServer.Generators;

public sealed class PythonWeselAjaGenerator
{
    public GenerationResult Generate(
        string projectName,
        bool includeExample,
        bool includeTests,
        bool overwrite)
    {
        var safeProjectName =
            SanitizeProjectName(projectName);
        var configuredOutputRoot =
            Environment.GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");
        var outputRoot =
            string.IsNullOrWhiteSpace(
                configuredOutputRoot)
                ? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "generated")
                : configuredOutputRoot.Trim();
        outputRoot =
            Path.GetFullPath(
                outputRoot);
        var pythonRoot =
            Path.GetFullPath(
                Path.Combine(
                    outputRoot,
                    "python"));
        var projectDirectory =
            Path.GetFullPath(
                Path.Combine(
                    pythonRoot,
                    safeProjectName));
        EnsureInsideDirectory(
            projectDirectory,
            pythonRoot);
        if (Directory.Exists(projectDirectory))
        {
            if (!overwrite)
            {
                throw new InvalidOperationException(
                    $"Generated project already exists: {projectDirectory}. " +
                    "Set overwrite=true to replace it."
                );
            }
            Directory.Delete(
                projectDirectory,
                recursive: true);
        }
        Directory.CreateDirectory(
            projectDirectory);
        var files =
            BuildFiles(
                includeExample,
                includeTests,
                safeProjectName);
        var generatedFiles =
            new List<GeneratedFile>();
        foreach (var file in files)
        {
            var filePath =
                Path.Combine(
                    projectDirectory,
                    file.Key);
            var directory =
                Path.GetDirectoryName(
                    filePath);
            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }
            File.WriteAllText(
                filePath,
                file.Value,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier:
                        false));
            generatedFiles.Add(
                new GeneratedFile
                {
                    Path =
                        Path.GetRelativePath(
                            projectDirectory,
                            filePath),
                    Bytes =
                        new FileInfo(
                            filePath).Length
                });
        }
        return new GenerationResult(
            safeProjectName,
            projectDirectory,
            generatedFiles);
    }
    private static Dictionary<string, string> BuildFiles(
        bool includeExample,
        bool includeTests,
        string projectName)
    {
        var files =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        files["weselaja_client.py"] =
            PythonClientTemplate;
        files["models.py"] =
            ModelsTemplate;
        files["exceptions.py"] =
            ExceptionsTemplate;
        files["__init__.py"] =
            InitTemplate;
        files[".env.example"] =
            EnvExampleTemplate;
        files[".gitignore"] =
            GitIgnoreTemplate;
        files["requirements.txt"] =
            RequirementsTemplate;
        files["pyproject.toml"] =
            BuildPyProjectTemplate(
                projectName);
        files["README.md"] =
            BuildReadmeTemplate(
                projectName);
        files["callback_server.py"] =
            CallbackServerTemplate;
        files["redirect_server.py"] =
            RedirectServerTemplate;
        if (includeExample)
        {
            files["example.py"] =
                ExampleTemplate;
        }
        if (includeTests)
        {
            files[
                Path.Combine(
                    "tests",
                    "test_models.py")] =
                TestsTemplate;
        }
        return files;
    }
    private static string SanitizeProjectName(
        string projectName)
    {
        if (string.IsNullOrWhiteSpace(
                projectName))
        {
            return "weselaja-python";
        }
        var builder =
            new StringBuilder();
        var previousWasSeparator =
            false;
        foreach (
            var character
            in projectName.Trim())
        {
            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    char.ToLowerInvariant(
                        character));
                previousWasSeparator =
                    false;
            }
            else
            {
                if (!previousWasSeparator)
                {
                    builder.Append(
                        '-');
                    previousWasSeparator =
                        true;
                }
            }
        }
        var result =
            builder
                .ToString()
                .Trim('-');
        if (string.IsNullOrWhiteSpace(
                result))
        {
            return "weselaja-python";
        }
        if (result.Length > 64)
        {
            result =
                result[..64]
                    .Trim('-');
        }
        return result;
    }
    private static void EnsureInsideDirectory(
        string path,
        string root)
    {
        var fullPath =
            Path.GetFullPath(
                path);
        var fullRoot =
            Path.GetFullPath(
                root);
        var relative =
            Path.GetRelativePath(
                fullRoot,
                fullPath);
        if (
            relative == ".." ||
            relative.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Generated project path is outside the configured generator directory."
            );
        }
        if (Path.IsPathRooted(
                relative))
        {
            throw new InvalidOperationException(
                "Invalid generated project path."
            );
        }
    }
    private static string BuildPyProjectTemplate(
        string projectName)
    {
        return $$"""
        [build-system]
        requires = [
            "setuptools>=68",
            "wheel"
        ]
        build-backend = "setuptools.build_meta"
        [project]
        name = "{{projectName}}"
        version = "0.1.0"
        description = "WeselAja Python integration"
        requires-python = ">=3.11"
        dependencies = [
            "python-dotenv>=1.0,<2.0"
        ]
        [tool.setuptools]
        py-modules = [
            "weselaja_client",
            "models",
            "exceptions"
        ]
        """;
    }
    private static readonly string
        CallbackServerTemplate =
        """
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
        """;
    private static string BuildReadmeTemplate(
        string projectName)
    {
        return $$"""
        # {{projectName}}
        Python integration client for WeselAja.
        ## Features
        - Create WeselAja virtual-account payments
        - Bank-to-channel mapping
        - HMAC-SHA256 request signature
        - Idempotency key generation
        - Structured payment response
        - Automatic `.env` loading
        - Python package metadata
        - Unit tests
        - Standalone redirect handler
        - Standalone callback handler
        - GET payin history
        - No credentials embedded in source code
        ## Supported Banks
        | Bank | WeselAja Channel |
        |---|---|
        | BRI | `BRI.VA` |
        | Mandiri | `MDR.VA` |
        | BNI | `BNI.VA` |
        | Permata | `PTB.VA` |
        | CIMB | `CIMB.VA` |
        | BCA | `BCA.VA` |
        ## Requirements
        - Python 3.11+
        - pip
        ## Installation
        It is recommended to create a virtual environment:
        ```bash
        python -m venv .venv
        ```
        Windows:
        ```powershell
        .venv\Scripts\activate
        ```
        Linux/macOS:
        ```bash
        source .venv/bin/activate
        ```
        Install the project:
        ```bash
        pip install -e .
        ```
        Or install dependencies directly:
        ```bash
        pip install -r requirements.txt
        ```
        ## Environment configuration
        Copy:
        ```text
        .env.example
        ```
        to:
        ```text
        .env
        ```
        Then fill in:
        ```env
        WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
        WESELAJA_API_KEY=
        WESELAJA_SECRET_KEY=
        WESELAJA_CUSTOMER_REFERENCE=
        WESELAJA_CALLBACK_URL=
        WESELAJA_REDIRECT_URL=
        WESELAJA_CURRENCY=IDR
        WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT
        WESELAJA_CALLBACK_PORT=8081
        WEB_SIGNATURE_SECRET=
        WESELAJA_REDIRECT_PORT=8080
        ```
        The generated client automatically loads `.env`.
        Never commit `.env`.
        ## Create payment
        ```python
        from weselaja_client import WeselAjaClient
        client = WeselAjaClient.from_env()
        payment = client.create_payment(
            amount=10_000,
            customer_name="Andi",
            bank="BCA",
        )
        print(payment.status)
        print(payment.payment_code)
        print(payment.reference_code)
        print(payment.payment_channel)
        ```
        ## Bank mapping
        The client accepts human-readable names:
        ```text
        BRI
        Mandiri
        BNI
        Permata
        CIMB
        BCA
        ```
        And maps them internally to the WeselAja channels:
        ```text
        BRI       -> BRI.VA
        Mandiri   -> MDR.VA
        BNI       -> BNI.VA
        Permata   -> PTB.VA
        CIMB      -> CIMB.VA
        BCA       -> BCA.VA
        ```
        ## Run example
        After configuring `.env`:
        ```bash
        python example.py
        ```
        This creates a sandbox payment.
        ## Get payins
        Retrieve payment sessions from the merchant account.
        Optional filters:
        - `status`: `PENDING`, `SUCCESS`, `EXPIRED`, or `FAILED`
        - `limit`: maximum number of records
        ```python
        payins = client.get_payins(
            status="SUCCESS",
            limit=50,
        )
        print(payins)
        ```
        This uses `GET /v1/payins` with the same HMAC-SHA256 signing flow.
        ## Run redirect handler
        The generated redirect handler listens on:
        ```text
        /payment/weselaja/return
        ```
        Start it with:
        ```bash
        python redirect_server.py
        ```
        Set `WESELAJA_REDIRECT_PORT` when a different local port is required.
        The handler returns the received query parameters as JSON. Do not use
        the browser redirect itself as payment confirmation; use the payment
        status API or the callback handler for server-side confirmation.
        ## Run callback handler
        The generated callback handler listens on:
        ```text
        /payment/weselaja/callback
        ```
        Start it with:
        ```bash
        python callback_server.py
        ```
        Set `WESELAJA_CALLBACK_PORT` when a different local port is required.
        Configure `WEB_SIGNATURE_SECRET` with the webhook signature secret.
        The handler verifies `x-middleware-timestamp` and
        `x-middleware-signature` before parsing the JSON body.
        Valid callbacks return HTTP 200. Invalid signatures return HTTP 401.
        ## Run tests
        ```bash
        python -m unittest discover -s tests
        ```
        ## API
        The generated client calls:
        ```text
        POST /v1/payins
        GET /v1/payins
        POST /payment/weselaja/callback
        ```
        The request uses the HMAC-SHA256 signing mechanism used by the
        validated WeselAja integration.
        Generated source code does not contain API credentials.
        """;
    }
    private static readonly string
        PythonClientTemplate =
        """
        from __future__ import annotations
        import base64
        import hashlib
        import hmac
        import json
        import os
        import uuid
        from dataclasses import dataclass
        from datetime import datetime, timezone
        from urllib import error, request
        from urllib.parse import urlencode
        from dotenv import load_dotenv
        from exceptions import (
            WeselAjaApiError,
            WeselAjaConfigurationError,
        )
        from models import (
            CreatePaymentResponse,
            bank_to_channel,
        )
        PAYMENT_URI = "/v1/payins"
        @dataclass(slots=True)
        class WeselAjaConfig:
            api_url: str
            api_key: str
            secret_key: str
            customer_reference: str
            callback_url: str
            redirect_url: str
            currency: str = "IDR"
            payment_method: str = "VIRTUAL_ACCOUNT"
            @classmethod
            def from_env(
                cls,
            ) -> "WeselAjaConfig":
                load_dotenv()
                config = cls(
                    api_url=(
                        os.getenv(
                            "WESELAJA_API_URL",
                            "https://sandbox.checkout.weselaja.id",
                        )
                        .strip()
                        .rstrip("/")
                    ),
                    api_key=(
                        os.getenv(
                            "WESELAJA_API_KEY",
                            "",
                        )
                        .strip()
                    ),
                    secret_key=(
                        os.getenv(
                            "WESELAJA_SECRET_KEY",
                            "",
                        )
                        .strip()
                    ),
                    customer_reference=(
                        os.getenv(
                            "WESELAJA_CUSTOMER_REFERENCE",
                            "",
                        )
                        .strip()
                    ),
                    callback_url=(
                        os.getenv(
                            "WESELAJA_CALLBACK_URL",
                            "",
                        )
                        .strip()
                    ),
                    redirect_url=(
                        os.getenv(
                            "WESELAJA_REDIRECT_URL",
                            "",
                        )
                        .strip()
                    ),
                    currency=(
                        os.getenv(
                            "WESELAJA_CURRENCY",
                            "IDR",
                        )
                        .strip()
                    ),
                    payment_method=(
                        os.getenv(
                            "WESELAJA_PAYMENT_METHOD",
                            "VIRTUAL_ACCOUNT",
                        )
                        .strip()
                    ),
                )
                config.validate()
                return config
            def validate(self) -> None:
                required = {
                    "WESELAJA_API_URL":
                        self.api_url,
                    "WESELAJA_API_KEY":
                        self.api_key,
                    "WESELAJA_SECRET_KEY":
                        self.secret_key,
                    "WESELAJA_CUSTOMER_REFERENCE":
                        self.customer_reference,
                    "WESELAJA_CALLBACK_URL":
                        self.callback_url,
                    "WESELAJA_REDIRECT_URL":
                        self.redirect_url,
                    "WESELAJA_CURRENCY":
                        self.currency,
                    "WESELAJA_PAYMENT_METHOD":
                        self.payment_method,
                }
                missing = [
                    key
                    for key, value
                    in required.items()
                    if not value
                ]
                if missing:
                    raise WeselAjaConfigurationError(
                        "Missing configuration: "
                        + ", ".join(missing)
                    )
        class WeselAjaClient:
            def __init__(
                self,
                config: WeselAjaConfig,
                timeout: float = 30.0,
            ) -> None:
                config.validate()
                self.config = config
                self.timeout = timeout
            @classmethod
            def from_env(
                cls,
                timeout: float = 30.0,
            ) -> "WeselAjaClient":
                return cls(
                    WeselAjaConfig.from_env(),
                    timeout=timeout,
                )
            def create_payment(
                self,
                *,
                amount: int,
                customer_name: str,
                bank: str,
                customer_phone_number:
                    str | None = None,
                description:
                    str | None = None,
                reference_code:
                    str | None = None,
            ) -> CreatePaymentResponse:
                if amount <= 0:
                    raise ValueError(
                        "Payment amount must be greater than zero."
                    )
                if not customer_name.strip():
                    raise ValueError(
                        "customer_name is required."
                    )
                payment_channel = (
                    bank_to_channel(
                        bank
                    )
                )
                reference = (
                    reference_code
                    or self._generate_reference(
                        customer_name
                    )
                )
                body_data = {
                    "initiatedAmount":
                        amount,
                    "currency":
                        self.config.currency,
                    "paymentMethod":
                        self.config.payment_method,
                    "paymentChannel":
                        payment_channel,
                    "referenceCode":
                        reference,
                    "customerReference":
                        self.config.customer_reference,
                    "customerName":
                        customer_name.strip(),
                    "callbackUrl":
                        self.config.callback_url,
                    "redirectUrl":
                        self.config.redirect_url,
                }
                if (
                    customer_phone_number
                    and customer_phone_number.strip()
                ):
                    body_data[
                        "customerPhoneNumber"
                    ] = (
                        customer_phone_number.strip()
                    )
                if (
                    description
                    and description.strip()
                ):
                    body_data[
                        "description"
                    ] = (
                        description.strip()
                    )
                body = json.dumps(
                    body_data,
                    separators=(
                        ",",
                        ":"
                    ),
                    ensure_ascii=False,
                )
                timestamp = (
                    datetime.now(
                        timezone.utc
                    )
                    .isoformat(
                        timespec="milliseconds"
                    )
                    .replace(
                        "+00:00",
                        "Z",
                    )
                )
                signature = (
                    self._generate_signature(
                        "POST",
                        PAYMENT_URI,
                        timestamp,
                        body,
                    )
                )
                idempotency_key = (
                    "python-"
                    + uuid.uuid4().hex
                )
                request_url = (
                    self.config.api_url
                    + PAYMENT_URI
                )
                headers = {
                    "Content-Type":
                        "application/json",
                    "Accept":
                        "application/json",
                    "Xenith-Api-Key":
                        self.config.api_key,
                    "Xenith-Request-Timestamp":
                        timestamp,
                    "Xenith-Request-Signature":
                        signature,
                    "X-Idempotency-Key":
                        idempotency_key,
                }
                http_request = request.Request(
                    url=request_url,
                    data=body.encode(
                        "utf-8"
                    ),
                    headers=headers,
                    method="POST",
                )
                try:
                    with request.urlopen(
                        http_request,
                        timeout=self.timeout,
                    ) as response:
                        status_code = (
                            response.status
                        )
                        response_body = (
                            response
                            .read()
                            .decode(
                                "utf-8"
                            )
                        )
                except error.HTTPError as exc:
                    try:
                        response_body = (
                            exc
                            .read()
                            .decode(
                                "utf-8"
                            )
                        )
                    except Exception:
                        response_body = ""
                    raise (
                        self._api_error_from_response(
                            exc.code,
                            response_body,
                        )
                    ) from exc
                except error.URLError as exc:
                    raise WeselAjaApiError(
                        "Unable to connect to WeselAja: "
                        + str(exc.reason)
                    ) from exc
                except TimeoutError as exc:
                    raise WeselAjaApiError(
                        "Connection to WeselAja timed out."
                    ) from exc
                if not response_body.strip():
                    raise WeselAjaApiError(
                        "Empty response from WeselAja. "
                        f"HTTP {status_code}"
                    )
                try:
                    payload = json.loads(
                        response_body
                    )
                except json.JSONDecodeError as exc:
                    raise WeselAjaApiError(
                        "Invalid JSON response from "
                        "WeselAja. "
                        f"HTTP {status_code}: "
                        f"{response_body}"
                    ) from exc
                if not (
                    200 <= status_code < 300
                ):
                    raise (
                        self._api_error_from_payload(
                            status_code,
                            payload,
                        )
                    )
                if not isinstance(
                    payload,
                    dict,
                ):
                    raise WeselAjaApiError(
                        "Unexpected response format from WeselAja."
                    )
                return (
                    CreatePaymentResponse.from_dict(
                        payload
                    )
                )
            def get_payins(
                self,
                *,
                status: str | None = None,
                limit: int | None = None,
            ) -> dict:
                params = {}
                if status is not None and status.strip():
                    normalized_status = status.strip().upper()
                    if normalized_status not in {
                        "PENDING",
                        "SUCCESS",
                        "EXPIRED",
                        "FAILED",
                    }:
                        raise ValueError(
                            "status must be one of: "
                            "PENDING, SUCCESS, EXPIRED, FAILED."
                        )
                    params["status"] = normalized_status
                if limit is not None:
                    if isinstance(limit, bool) or not isinstance(limit, int) or limit <= 0:
                        raise ValueError(
                            "limit must be a positive integer."
                        )
                    params["limit"] = limit
                query = urlencode(params)
                request_url = (
                    self.config.api_url
                    + PAYMENT_URI
                    + ("?" + query if query else "")
                )
                timestamp = (
                    datetime.now(
                        timezone.utc
                    )
                    .isoformat(
                        timespec="milliseconds"
                    )
                    .replace(
                        "+00:00",
                        "Z",
                    )
                )
                signature = (
                    self._generate_signature(
                        "GET",
                        PAYMENT_URI,
                        timestamp,
                        "",
                    )
                )
                headers = {
                    "Accept":
                        "application/json",
                    "Xenith-Api-Key":
                        self.config.api_key,
                    "Xenith-Request-Timestamp":
                        timestamp,
                    "Xenith-Request-Signature":
                        signature,
                }
                http_request = request.Request(
                    url=request_url,
                    headers=headers,
                    method="GET",
                )
                try:
                    with request.urlopen(
                        http_request,
                        timeout=self.timeout,
                    ) as response:
                        status_code = response.status
                        response_body = response.read().decode("utf-8")
                except error.HTTPError as exc:
                    try:
                        response_body = exc.read().decode("utf-8")
                    except Exception:
                        response_body = ""
                    raise self._api_error_from_response(
                        exc.code,
                        response_body,
                    ) from exc
                except error.URLError as exc:
                    raise WeselAjaApiError(
                        "Unable to connect to WeselAja: "
                        + str(exc.reason)
                    ) from exc
                except TimeoutError as exc:
                    raise WeselAjaApiError(
                        "Connection to WeselAja timed out."
                    ) from exc
                if not response_body.strip():
                    raise WeselAjaApiError(
                        "Empty response from WeselAja. "
                        f"HTTP {status_code}"
                    )
                try:
                    payload = json.loads(response_body)
                except json.JSONDecodeError as exc:
                    raise WeselAjaApiError(
                        "Invalid JSON response from WeselAja. "
                        f"HTTP {status_code}: {response_body}"
                    ) from exc
                if not (200 <= status_code < 300):
                    raise self._api_error_from_payload(
                        status_code,
                        payload,
                    )
                if not isinstance(payload, dict):
                    raise WeselAjaApiError(
                        "Unexpected response format from WeselAja."
                    )
                return payload

            def _generate_signature(
                self,
                method: str,
                uri: str,
                timestamp: str,
                body: str,
            ) -> str:
                payload = (
                    f"{method}\n"
                    f"{uri}\n"
                    f"{timestamp}\n"
                    f"{body}"
                )
                digest = hmac.new(
                    self.config.secret_key.encode(
                        "utf-8"
                    ),
                    payload.encode(
                        "utf-8"
                    ),
                    hashlib.sha256,
                ).digest()
                return base64.b64encode(
                    digest
                ).decode(
                    "utf-8"
                )
            @staticmethod
            def _generate_reference(
                customer_name: str,
            ) -> str:
                normalized = "".join(
                    character
                    for character
                    in customer_name.upper()
                    if (
                        character.isalnum()
                        or character in "-_"
                    )
                )
                normalized = (
                    normalized[:20]
                    or "CUSTOMER"
                )
                timestamp = (
                    datetime.now(
                        timezone.utc
                    ).strftime(
                        "%Y%m%d%H%M%S"
                    )
                )
                return (
                    f"MCP-{normalized}-{timestamp}"
                )
            @staticmethod
            def _api_error_from_response(
                status_code: int,
                response_body: str,
            ) -> WeselAjaApiError:
                try:
                    payload = json.loads(
                        response_body
                    )
                except json.JSONDecodeError:
                    payload = response_body
                return (
                    WeselAjaClient
                    ._api_error_from_payload(
                        status_code,
                        payload,
                    )
                )
            @staticmethod
            def _api_error_from_payload(
                status_code: int,
                payload: object,
            ) -> WeselAjaApiError:
                message: object | None = None
                if isinstance(
                    payload,
                    dict,
                ):
                    message = (
                        payload.get("message")
                        or payload.get("error")
                        or payload.get("code")
                    )
                if message is None:
                    message = (
                        payload
                        or f"HTTP {status_code}"
                    )
                if isinstance(
                    message,
                    dict,
                ):
                    message = json.dumps(
                        message,
                        ensure_ascii=False,
                    )
                return WeselAjaApiError(
                    str(message),
                    status_code=status_code,
                    details=payload,
                )
        """;
    private static readonly string
        ModelsTemplate =
        """
        from __future__ import annotations
        from dataclasses import dataclass, field
        from typing import Any
        BANK_TO_CHANNEL = {
            "BRI": "BRI.VA",
            "MANDIRI": "MDR.VA",
            "BNI": "BNI.VA",
            "PERMATA": "PTB.VA",
            "CIMB": "CIMB.VA",
            "BCA": "BCA.VA",
        }
        def normalize_bank(
            bank: str,
        ) -> str:
            value = (
                bank
                .strip()
                .upper()
                .replace(" ", "")
                .replace("-", "")
                .replace("_", "")
            )
            aliases = {
                "BANKBRI":
                    "BRI",
                "BANKMANDIRI":
                    "MANDIRI",
                "MDR":
                    "MANDIRI",
                "BANKBNI":
                    "BNI",
                "BANKPERMATA":
                    "PERMATA",
                "PTB":
                    "PERMATA",
                "CIMBNIAGA":
                    "CIMB",
                "BANKCIMB":
                    "CIMB",
                "BANKBCA":
                    "BCA",
            }
            return aliases.get(
                value,
                value,
            )
        def bank_to_channel(
            bank: str,
        ) -> str:
            normalized = (
                normalize_bank(
                    bank
                )
            )
            try:
                return BANK_TO_CHANNEL[
                    normalized
                ]
            except KeyError as exc:
                supported = (
                    ", ".join(
                        BANK_TO_CHANNEL.keys()
                    )
                )
                raise ValueError(
                    f"Unsupported bank '{bank}'. "
                    f"Supported banks: {supported}."
                ) from exc
        @dataclass(slots=True)
        class CreatePaymentResponse:
            id: str | None = None
            initiated_amount: int | None = None
            payment_amount: int | None = None
            fee_amount: int | None = None
            currency: str | None = None
            payment_method: str | None = None
            payment_channel: str | None = None
            payment_code: str | None = None
            payment_code_type: str | None = None
            reference_code: str | None = None
            customer_reference: str | None = None
            customer_name: str | None = None
            status: str | None = None
            created_time: str | None = None
            updated_time: str | None = None
            expiration_time: str | None = None
            description: str | None = None
            callback_url: str | None = None
            redirect_url: str | None = None
            payer_account_name: str | None = None
            payer_account_number: str | None = None
            payer_payment_channel: str | None = None
            metadata: dict[str, Any] = field(
                default_factory=dict
            )
            @classmethod
            def from_dict(
                cls,
                data: dict[str, Any],
            ) -> "CreatePaymentResponse":
                return cls(
                    id=data.get(
                        "id"
                    ),
                    initiated_amount=data.get(
                        "initiatedAmount"
                    ),
                    payment_amount=data.get(
                        "paymentAmount"
                    ),
                    fee_amount=data.get(
                        "feeAmount"
                    ),
                    currency=data.get(
                        "currency"
                    ),
                    payment_method=data.get(
                        "paymentMethod"
                    ),
                    payment_channel=data.get(
                        "paymentChannel"
                    ),
                    payment_code=data.get(
                        "paymentCode"
                    ),
                    payment_code_type=data.get(
                        "paymentCodeType"
                    ),
                    reference_code=data.get(
                        "referenceCode"
                    ),
                    customer_reference=data.get(
                        "customerReference"
                    ),
                    customer_name=data.get(
                        "customerName"
                    ),
                    status=data.get(
                        "status"
                    ),
                    created_time=data.get(
                        "createdTime"
                    ),
                    updated_time=data.get(
                        "updatedTime"
                    ),
                    expiration_time=data.get(
                        "expirationTime"
                    ),
                    description=data.get(
                        "description"
                    ),
                    callback_url=data.get(
                        "callbackUrl"
                    ),
                    redirect_url=data.get(
                        "redirectUrl"
                    ),
                    payer_account_name=data.get(
                        "payerAccountName"
                    ),
                    payer_account_number=data.get(
                        "payerAccountNumber"
                    ),
                    payer_payment_channel=data.get(
                        "payerPaymentChannel"
                    ),
                    metadata=(
                        data.get(
                            "metadata"
                        )
                        if isinstance(
                            data.get(
                                "metadata"
                            ),
                            dict,
                        )
                        else {}
                    ),
                )
        """;
    private static readonly string
        ExceptionsTemplate =
        """
        class WeselAjaError(Exception):
            pass
        class WeselAjaConfigurationError(
            WeselAjaError
        ):
            pass
        class WeselAjaApiError(
            WeselAjaError
        ):
            def __init__(
                self,
                message: str,
                *,
                status_code:
                    int | None = None,
                details:
                    object | None = None,
            ) -> None:
                super().__init__(
                    message
                )
                self.status_code = status_code
                self.details = details
        """;
    private static readonly string
        InitTemplate =
        """
        from .weselaja_client import (
            WeselAjaClient,
            WeselAjaConfig,
        )
        from .models import (
            CreatePaymentResponse,
            bank_to_channel,
        )
        from .exceptions import (
            WeselAjaError,
            WeselAjaApiError,
            WeselAjaConfigurationError,
        )
        __all__ = [
            "WeselAjaClient",
            "WeselAjaConfig",
            "CreatePaymentResponse",
            "bank_to_channel",
            "WeselAjaError",
            "WeselAjaApiError",
            "WeselAjaConfigurationError",
        ]
        """;
    private static readonly string
        EnvExampleTemplate =
        """
        # WeselAja Sandbox
        WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
        # Merchant credentials
        WESELAJA_API_KEY=
        WESELAJA_SECRET_KEY=
        # Merchant/customer reference
        WESELAJA_CUSTOMER_REFERENCE=
        # Application URLs
        WESELAJA_CALLBACK_URL=
        WESELAJA_REDIRECT_URL=
        # Callback configuration
        WESELAJA_CALLBACK_PORT=8081
        WEB_SIGNATURE_SECRET=
        # Payment configuration
        WESELAJA_CURRENCY=IDR
        WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT
        """;
    private static readonly string
        GitIgnoreTemplate =
        """
        .env
        .venv/
        __pycache__/
        *.py[cod]
        .pytest_cache/
        build/
        dist/
        *.egg-info/
        """;
    private static readonly string
        RequirementsTemplate =
        """
        python-dotenv>=1.0,<2.0
        """;
    private static readonly string
        ExampleTemplate =
        """
        from weselaja_client import (
            WeselAjaClient,
        )
        def main() -> None:
            client = (
                WeselAjaClient.from_env()
            )
            payment = (
                client.create_payment(
                    amount=10_000,
                    customer_name="Andi",
                    bank="BRI",
                    description=(
                        "Sandbox payment example"
                    ),
                )
            )
            print(
                "Status:",
                payment.status,
            )
            print(
                "Payment code:",
                payment.payment_code,
            )
            print(
                "Reference:",
                payment.reference_code,
            )
            print(
                "Channel:",
                payment.payment_channel,
            )
        if __name__ == "__main__":
            main()
        """;
    private static readonly string
        RedirectServerTemplate =
        """
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
        """;
    private static readonly string
        TestsTemplate =
        """
        import unittest
        from models import (
            bank_to_channel,
        )
        class BankMappingTests(
            unittest.TestCase,
        ):
            def test_bri(self):
                self.assertEqual(
                    bank_to_channel(
                        "BRI"
                    ),
                    "BRI.VA",
                )
            def test_bank_bri(self):
                self.assertEqual(
                    bank_to_channel(
                        "Bank BRI"
                    ),
                    "BRI.VA",
                )
            def test_mandiri(self):
                self.assertEqual(
                    bank_to_channel(
                        "Mandiri"
                    ),
                    "MDR.VA",
                )
            def test_bank_mandiri(self):
                self.assertEqual(
                    bank_to_channel(
                        "Bank Mandiri"
                    ),
                    "MDR.VA",
                )
            def test_bni(self):
                self.assertEqual(
                    bank_to_channel(
                        "BNI"
                    ),
                    "BNI.VA",
                )
            def test_permata(self):
                self.assertEqual(
                    bank_to_channel(
                        "Permata"
                    ),
                    "PTB.VA",
                )
            def test_cimb(self):
                self.assertEqual(
                    bank_to_channel(
                        "CIMB"
                    ),
                    "CIMB.VA",
                )
            def test_cimb_niaga(self):
                self.assertEqual(
                    bank_to_channel(
                        "CIMB Niaga"
                    ),
                    "CIMB.VA",
                )
            def test_bca(self):
                self.assertEqual(
                    bank_to_channel(
                        "BCA"
                    ),
                    "BCA.VA",
                )
            def test_bank_bca(self):
                self.assertEqual(
                    bank_to_channel(
                        "Bank BCA"
                    ),
                    "BCA.VA",
                )
            def test_invalid_bank(self):
                with self.assertRaises(
                    ValueError
                ):
                    bank_to_channel(
                        "Bank Tidak Ada"
                    )
        if __name__ == "__main__":
            unittest.main()
        """;
}
public sealed class GenerationResult
{
    public GenerationResult(
        string projectName,
        string outputDirectory,
        List<GeneratedFile> files)
    {
        ProjectName =
            projectName;
        OutputDirectory =
            outputDirectory;
        Files =
            files;
    }
    public string ProjectName { get; }
    public string OutputDirectory { get; }
    public List<GeneratedFile> Files { get; }
}
public sealed class GeneratedFile
{
    public string Path { get; init; } =
        string.Empty;
    public long Bytes { get; init; }
}
