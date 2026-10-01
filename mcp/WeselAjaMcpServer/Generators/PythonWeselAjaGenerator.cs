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
            SanitizeProjectName(
                projectName);

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

        if (Directory.Exists(
                projectDirectory))
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
                includeTests);

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
        bool includeTests)
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

        files[".env.example"] =
            EnvExampleTemplate;

        files[".gitignore"] =
            GitIgnoreTemplate;

        files["requirements.txt"] =
            RequirementsTemplate;

        files["README.md"] =
            ReadmeTemplate;

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
                    "WESELAJA_API_URL": self.api_url,
                    "WESELAJA_API_KEY": self.api_key,
                    "WESELAJA_SECRET_KEY": self.secret_key,
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
                customer_phone_number: str | None = None,
                description: str | None = None,
                reference_code: str | None = None,
            ) -> CreatePaymentResponse:

                if amount <= 0:
                    raise ValueError(
                        "Payment amount must be greater than zero."
                    )

                if not customer_name.strip():
                    raise ValueError(
                        "customer_name is required."
                    )

                payment_channel =
                    bank_to_channel(
                        bank
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
                        ":",
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

                signature =
                    self._generate_signature(
                        "POST",
                        PAYMENT_URI,
                        timestamp,
                        body,
                    )

                idempotency_key =
                    "python-" + uuid.uuid4().hex

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

                http_request =
                    request.Request(
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

                        status_code =
                            response.status

                        response_body =
                            response.read().decode(
                                "utf-8"
                            )

                except error.HTTPError as exc:
                    try:
                        response_body =
                            exc.read().decode(
                                "utf-8"
                            )
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
                    payload =
                        json.loads(
                            response_body
                        )
                except json.JSONDecodeError as exc:
                    raise WeselAjaApiError(
                        "Invalid JSON response from WeselAja. "
                        f"HTTP {status_code}: "
                        f"{response_body}"
                    ) from exc

                if not (
                    200 <= status_code < 300
                ):
                    raise self._api_error_from_payload(
                        status_code,
                        payload,
                    )

                if not isinstance(
                    payload,
                    dict,
                ):
                    raise WeselAjaApiError(
                        "Unexpected response format from WeselAja."
                    )

                return CreatePaymentResponse.from_dict(
                    payload
                )

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
                    payload =
                        json.loads(
                            response_body
                        )
                except json.JSONDecodeError:
                    payload =
                        response_body

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
                    message =
                        payload or (
                            f"HTTP {status_code}"
                        )

                if isinstance(
                    message,
                    dict,
                ):
                    message =
                        json.dumps(
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

            normalized =
                normalize_bank(
                    bank
                )

            try:
                return BANK_TO_CHANNEL[
                    normalized
                ]

            except KeyError as exc:
                supported = ", ".join(
                    BANK_TO_CHANNEL.keys()
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
                    id=data.get("id"),

                    initiated_amount=
                        data.get(
                            "initiatedAmount"
                        ),

                    payment_amount=
                        data.get(
                            "paymentAmount"
                        ),

                    fee_amount=
                        data.get(
                            "feeAmount"
                        ),

                    currency=data.get(
                        "currency"
                    ),

                    payment_method=
                        data.get(
                            "paymentMethod"
                        ),

                    payment_channel=
                        data.get(
                            "paymentChannel"
                        ),

                    payment_code=
                        data.get(
                            "paymentCode"
                        ),

                    payment_code_type=
                        data.get(
                            "paymentCodeType"
                        ),

                    reference_code=
                        data.get(
                            "referenceCode"
                        ),

                    customer_reference=
                        data.get(
                            "customerReference"
                        ),

                    customer_name=
                        data.get(
                            "customerName"
                        ),

                    status=data.get(
                        "status"
                    ),

                    created_time=
                        data.get(
                            "createdTime"
                        ),

                    updated_time=
                        data.get(
                            "updatedTime"
                        ),

                    expiration_time=
                        data.get(
                            "expirationTime"
                        ),

                    description=
                        data.get(
                            "description"
                        ),

                    callback_url=
                        data.get(
                            "callbackUrl"
                        ),

                    redirect_url=
                        data.get(
                            "redirectUrl"
                        ),

                    payer_account_name=
                        data.get(
                            "payerAccountName"
                        ),

                    payer_account_number=
                        data.get(
                            "payerAccountNumber"
                        ),

                    payer_payment_channel=
                        data.get(
                            "payerPaymentChannel"
                        ),

                    metadata=(
                        data.get(
                            "metadata"
                        )
                        if isinstance(
                            data.get("metadata"),
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
                status_code: int | None = None,
                details: object | None = None,
            ) -> None:

                super().__init__(
                    message
                )

                self.status_code =
                    status_code

                self.details =
                    details
        """;

    private static readonly string
        EnvExampleTemplate =
        """
        WESELAJA_API_URL=https://sandbox.checkout.weselaja.id

        WESELAJA_API_KEY=

        WESELAJA_SECRET_KEY=

        WESELAJA_CUSTOMER_REFERENCE=

        WESELAJA_CALLBACK_URL=

        WESELAJA_REDIRECT_URL=

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
        """;

    private static readonly string
        RequirementsTemplate =
        """
        # WeselAja generated integration
        #
        # This project uses Python standard-library modules only.
        """;

    private static readonly string
        ExampleTemplate =
        """
        from weselaja_client import (
            WeselAjaClient,
        )


        def main() -> None:

            client =
                WeselAjaClient.from_env()

            payment =
                client.create_payment(
                    amount=10_000,
                    customer_name="Andi",
                    bank="BCA",
                    description=
                        "Sandbox payment example",
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
                    bank_to_channel("BRI"),
                    "BRI.VA",
                )

            def test_mandiri(self):
                self.assertEqual(
                    bank_to_channel("Mandiri"),
                    "MDR.VA",
                )

            def test_bni(self):
                self.assertEqual(
                    bank_to_channel("BNI"),
                    "BNI.VA",
                )

            def test_permata(self):
                self.assertEqual(
                    bank_to_channel("Permata"),
                    "PTB.VA",
                )

            def test_cimb(self):
                self.assertEqual(
                    bank_to_channel("CIMB"),
                    "CIMB.VA",
                )

            def test_bca(self):
                self.assertEqual(
                    bank_to_channel("BCA"),
                    "BCA.VA",
                )


        if __name__ == "__main__":
            unittest.main()
        """;

    private static readonly string
        ReadmeTemplate =
        """
        # WeselAja Python Integration

        This project was generated by the WeselAja MCP Code Generator.

        ## Features

        - Create WeselAja virtual-account payments
        - Bank-to-channel mapping
        - HMAC-SHA256 request signature
        - Idempotency key generation
        - Structured payment response
        - Sandbox configuration through environment variables
        - No third-party dependency required

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
        - Standard library only

        ## Configuration

        Copy `.env.example` as a reference for your environment.

        The generated client reads:

        ```text
        WESELAJA_API_URL
        WESELAJA_API_KEY
        WESELAJA_SECRET_KEY
        WESELAJA_CUSTOMER_REFERENCE
        WESELAJA_CALLBACK_URL
        WESELAJA_REDIRECT_URL
        WESELAJA_CURRENCY
        WESELAJA_PAYMENT_METHOD
        ```

        Do not commit credentials to Git.

        ## Example

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
        ```

        ## Bank mapping

        The developer can use human-readable bank names:

        ```text
        BRI
        Mandiri
        BNI
        Permata
        CIMB
        BCA
        ```

        The client maps these values to the corresponding
        WeselAja payment channel.

        ## Run

        ```bash
        python example.py
        ```

        ## Tests

        ```bash
        python -m unittest discover -s tests
        ```

        ## API

        The generated client sends:

        ```text
        POST /v1/payins
        ```

        Request signing uses HMAC-SHA256 with:

        ```text
        METHOD
        URI
        TIMESTAMP
        BODY
        ```

        separated according to the validated WeselAja integration.

        Generated source code does not contain API credentials.
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
    public string Path { get; init; } = string.Empty;

    public long Bytes { get; init; }
}