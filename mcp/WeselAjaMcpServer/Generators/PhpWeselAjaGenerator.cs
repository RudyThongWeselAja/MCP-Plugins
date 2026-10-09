using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class PhpWeselAjaGenerator
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

        var outputRoot =
            Environment.GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");

        outputRoot =
            string.IsNullOrWhiteSpace(
                outputRoot)
                ? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "generated")
                : outputRoot.Trim();

        outputRoot =
            Path.GetFullPath(
                outputRoot);

        var phpRoot =
            Path.GetFullPath(
                Path.Combine(
                    outputRoot,
                    "php"));

        var projectDirectory =
            Path.GetFullPath(
                Path.Combine(
                    phpRoot,
                    safeProjectName));

        EnsureInsideDirectory(
            projectDirectory,
            phpRoot);

        if (
            Directory.Exists(
                projectDirectory))
        {
            if (!overwrite)
            {
                throw new InvalidOperationException(
                    $"Generated project already exists: {projectDirectory}. " +
                    "Set overwrite=true to replace it.");
            }

            Directory.Delete(
                projectDirectory,
                recursive: true);
        }

        Directory.CreateDirectory(
            projectDirectory);

        var files =
            BuildFiles(
                safeProjectName,
                includeExample,
                includeTests);

        var generatedFiles =
            new List<GeneratedFile>();

        foreach (
            var file
            in files)
        {
            var filePath =
                Path.Combine(
                    projectDirectory,
                    file.Key);

            var directory =
                Path.GetDirectoryName(
                    filePath);

            if (
                !string.IsNullOrWhiteSpace(
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
        string projectName,
        bool includeExample,
        bool includeTests)
    {
        var files =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        files["src/Exceptions/WeselAjaException.php"] =
            ExceptionTemplate;

        files["src/Models/CreatePaymentRequest.php"] =
            CreatePaymentRequestTemplate;

        files["src/Models/CreatePaymentResponse.php"] =
            CreatePaymentResponseTemplate;

        files["src/BankChannels.php"] =
            BankChannelsTemplate;

        files["src/EnvLoader.php"] =
            EnvLoaderTemplate;

        files["src/WeselAjaConfig.php"] =
            ConfigTemplate;

        files["src/WeselAjaSignature.php"] =
            SignatureTemplate;

        files["src/WeselAjaClient.php"] =
            ClientTemplate;

        files[".env.example"] =
            EnvExampleTemplate;

        files[".gitignore"] =
            GitIgnoreTemplate;

        files["README.md"] =
            BuildReadmeTemplate(
                projectName);

        files["redirect.php"] =
            RedirectTemplate;

        files["callback.php"] =
            CallbackTemplate;

        if (includeExample)
        {
            files["example.php"] =
                ExampleTemplate;
        }

        if (includeTests)
        {
            files["tests/self-test.php"] =
                SelfTestTemplate;
        }

        return files;
    }

    private static string SanitizeProjectName(
        string projectName)
    {
        if (
            string.IsNullOrWhiteSpace(
                projectName))
        {
            return "weselaja-php";
        }

        var builder =
            new StringBuilder();

        var previousWasSeparator =
            false;

        foreach (
            var character
            in projectName.Trim())
        {
            if (
                char.IsLetterOrDigit(
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

        if (
            string.IsNullOrWhiteSpace(
                result))
        {
            return "weselaja-php";
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
            relative == ".."
            ||
            relative.StartsWith(
                ".." +
                Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Generated project path is outside the configured generator directory.");
        }

        if (
            Path.IsPathRooted(
                relative))
        {
            throw new InvalidOperationException(
                "Invalid generated project path.");
        }
    }

    private static string BuildReadmeTemplate(
        string projectName)
    {
        return $$"""
        # {{projectName}}

        Standalone PHP integration for the WeselAja payment provider.

        This project was generated by the WeselAja Code Generator.

        ## Scope

        This generated integration supports:

        - create payment
        - get payins
        - HMAC-SHA256 request signing
        - bank/channel mapping
        - response parsing
        - configuration
        - validation
        - error handling
        - callback handling
        - callback signature verification
        - redirect handling

        ## Requirements

        - PHP 8.2+
        - cURL extension enabled

        Check:

        ```bash
        php -m
        ```

        and make sure:

        ```text
        curl
        ```

        is present.

        ## Configuration

        Copy:

        ```text
        .env.example
        ```

        to:

        ```text
        .env
        ```

        Example:

        ```env
        WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
        WESELAJA_API_KEY=
        WESELAJA_SECRET_KEY=
        WESELAJA_CUSTOMER_REFERENCE=
        WESELAJA_CALLBACK_URL=
        WESELAJA_REDIRECT_URL=
        WESELAJA_CURRENCY=IDR
        WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT
        WESELAJA_CAINFO=
        WESELAJA_CALLBACK_PORT=8081
        WESELAJA_REDIRECT_PORT=8080
        WEB_SIGNATURE_SECRET=
        ```

        `WESELAJA_CAINFO` is optional. SSL verification remains enabled.

        `WEB_SIGNATURE_SECRET` is used to verify callback signatures.

        Never commit `.env`.

        ## Example

        ```php
        require_once __DIR__ . '/src/Exceptions/WeselAjaException.php';
        require_once __DIR__ . '/src/Models/CreatePaymentRequest.php';
        require_once __DIR__ . '/src/Models/CreatePaymentResponse.php';
        require_once __DIR__ . '/src/BankChannels.php';
        require_once __DIR__ . '/src/EnvLoader.php';
        require_once __DIR__ . '/src/WeselAjaConfig.php';
        require_once __DIR__ . '/src/WeselAjaSignature.php';
        require_once __DIR__ . '/src/WeselAjaClient.php';

        use WeselAja\WeselAjaClient;
        use WeselAja\Models\CreatePaymentRequest;

        $client =
            WeselAjaClient::fromEnvironment();

        $payment =
            $client->createPayment(
                new CreatePaymentRequest(
                    amount: 10000,
                    customerName: 'Andi',
                    bank: 'BRI',
                    description:
                        'Sandbox payment'
                )
            );

        echo $payment->status . PHP_EOL;
        echo $payment->paymentCode . PHP_EOL;
        ```

        ## Supported banks

        ```text
        BRI       -> BRI.VA
        Mandiri   -> MDR.VA
        BNI       -> BNI.VA
        Permata   -> PTB.VA
        CIMB      -> CIMB.VA
        BCA       -> BCA.VA
        ```

        ## Get payins

        ```php
        $payins =
            $client->getPayins(
                status: 'SUCCESS',
                limit: 50
            );

        print_r($payins);
        ```

        This uses:

        ```text
        GET /v1/payins
        ```

        Optional status values:

        ```text
        PENDING
        SUCCESS
        EXPIRED
        FAILED
        ```

        ## Callback handler

        The generated callback handler listens on:

        ```text
        /payment/weselaja/callback
        ```

        Start it with:

        ```bash
        php -S 0.0.0.0:8081 callback.php
        ```

        Set another port with:

        ```env
        WESELAJA_CALLBACK_PORT=8081
        ```

        The callback handler verifies:

        ```text
        x-middleware-timestamp
        x-middleware-signature
        ```

        using:

        ```text
        POST
        /payment/weselaja/callback
        <raw body>
        <timestamp>
        ```

        with HMAC-SHA256 and Base64.

        ## Redirect handler

        The generated handler listens on:

        ```text
        /payment/weselaja/return
        ```

        Start it with:

        ```bash
        php -S 0.0.0.0:8080 redirect.php
        ```

        Set another port with:

        ```env
        WESELAJA_REDIRECT_PORT=8080
        ```

        ## Run example

        ```bash
        php example.php
        ```

        ## Run self-test

        ```bash
        php tests/self-test.php
        ```

        ## Authentication

        The generated client uses:

        ```text
        METHOD
        URI
        TIMESTAMP
        BODY
        ```

        Headers:

        ```text
        Xenith-Api-Key
        Xenith-Request-Timestamp
        Xenith-Request-Signature
        X-Idempotency-Key
        ```

        ## Security

        API credentials are loaded from `.env`
        and are never embedded in the source code.
        """;
    }

    private const string
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

        WESELAJA_CAINFO=

        WESELAJA_CALLBACK_PORT=8081
        WESELAJA_REDIRECT_PORT=8080

        WEB_SIGNATURE_SECRET=
        """;

    private const string
        GitIgnoreTemplate =
        """
        .env
        vendor/
        """;

    private const string
        EnvLoaderTemplate =
        """
        <?php
        declare(strict_types=1);

        namespace WeselAja;

        final class EnvLoader
        {
            public static function load(
                string $file = '.env'
            ): void {
                $path =
                    self::findFile(
                        $file
                    );

                if ($path === null) {
                    return;
                }

                $lines =
                    file(
                        $path,
                        FILE_IGNORE_NEW_LINES
                        | FILE_SKIP_EMPTY_LINES
                    );

                if ($lines === false) {
                    return;
                }

                foreach ($lines as $line) {
                    $line =
                        trim(
                            $line
                        );

                    if (
                        $line === ''
                        ||
                        str_starts_with(
                            $line,
                            '#'
                        )
                    ) {
                        continue;
                    }

                    $separator =
                        strpos(
                            $line,
                            '='
                        );

                    if (
                        $separator === false
                        ||
                        $separator === 0
                    ) {
                        continue;
                    }

                    $key =
                        trim(
                            substr(
                                $line,
                                0,
                                $separator
                            )
                        );

                    $value =
                        trim(
                            substr(
                                $line,
                                $separator + 1
                            )
                        );

                    $value =
                        self::removeQuotes(
                            $value
                        );

                    if (
                        getenv($key) === false
                        ||
                        trim(
                            (string) getenv(
                                $key
                            )
                        ) === ''
                    ) {
                        putenv(
                            $key . '=' . $value
                        );
                    }
                }
            }

            private static function findFile(
                string $file
            ): ?string {
                $directory =
                    getcwd();

                while (
                    is_string(
                        $directory
                    )
                    &&
                    $directory !== ''
                ) {
                    $candidate =
                        $directory .
                        DIRECTORY_SEPARATOR .
                        $file;

                    if (
                        is_file(
                            $candidate
                        )
                    ) {
                        return $candidate;
                    }

                    $parent =
                        dirname(
                            $directory
                        );

                    if (
                        $parent ===
                        $directory
                    ) {
                        break;
                    }

                    $directory =
                        $parent;
                }

                return null;
            }

            private static function removeQuotes(
                string $value
            ): string {
                if (
                    strlen($value) >= 2
                    &&
                    (
                        (
                            str_starts_with(
                                $value,
                                '"'
                            )
                            &&
                            str_ends_with(
                                $value,
                                '"'
                            )
                        )
                        ||
                        (
                            str_starts_with(
                                $value,
                                "'"
                            )
                            &&
                            str_ends_with(
                                $value,
                                "'"
                            )
                        )
                    )
                ) {
                    return substr(
                        $value,
                        1,
                        -1
                    );
                }

                return $value;
            }
        }
        """;

    private const string
        ConfigTemplate =
        """
        <?php
        declare(strict_types=1);

        namespace WeselAja;

        use WeselAja\Exceptions\WeselAjaConfigurationException;

        final class WeselAjaConfig
        {
            public function __construct(
                public readonly string $apiUrl,
                public readonly string $apiKey,
                public readonly string $secretKey,
                public readonly string $customerReference,
                public readonly string $callbackUrl,
                public readonly string $redirectUrl,
                public readonly string $currency = 'IDR',
                public readonly string $paymentMethod = 'VIRTUAL_ACCOUNT',
                public readonly ?string $caInfo = null
            ) {
            }

            public static function fromEnvironment(): self
            {
                EnvLoader::load();

                $config =
                    new self(
                        apiUrl:
                            self::env(
                                'WESELAJA_API_URL',
                                'https://sandbox.checkout.weselaja.id'
                            ),

                        apiKey:
                            self::env(
                                'WESELAJA_API_KEY'
                            ),

                        secretKey:
                            self::env(
                                'WESELAJA_SECRET_KEY'
                            ),

                        customerReference:
                            self::env(
                                'WESELAJA_CUSTOMER_REFERENCE'
                            ),

                        callbackUrl:
                            self::env(
                                'WESELAJA_CALLBACK_URL'
                            ),

                        redirectUrl:
                            self::env(
                                'WESELAJA_REDIRECT_URL'
                            ),

                        currency:
                            self::env(
                                'WESELAJA_CURRENCY',
                                'IDR'
                            ),

                        paymentMethod:
                            self::env(
                                'WESELAJA_PAYMENT_METHOD',
                                'VIRTUAL_ACCOUNT'
                            ),

                        caInfo:
                            self::nullableEnv(
                                'WESELAJA_CAINFO'
                            )
                    );

                $config->validate();

                return $config;
            }

            public function validate(): void
            {
                $required = [
                    'WESELAJA_API_URL' =>
                        $this->apiUrl,

                    'WESELAJA_API_KEY' =>
                        $this->apiKey,

                    'WESELAJA_SECRET_KEY' =>
                        $this->secretKey,

                    'WESELAJA_CUSTOMER_REFERENCE' =>
                        $this->customerReference,

                    'WESELAJA_CALLBACK_URL' =>
                        $this->callbackUrl,

                    'WESELAJA_REDIRECT_URL' =>
                        $this->redirectUrl,

                    'WESELAJA_CURRENCY' =>
                        $this->currency,

                    'WESELAJA_PAYMENT_METHOD' =>
                        $this->paymentMethod,
                ];

                $missing = [];

                foreach (
                    $required as $key => $value
                ) {
                    if (
                        trim($value) === ''
                    ) {
                        $missing[] = $key;
                    }
                }

                if ($missing !== []) {
                    throw new WeselAjaConfigurationException(
                        'Missing configuration: ' .
                        implode(
                            ', ',
                            $missing
                        )
                    );
                }

                self::validateUrl(
                    $this->apiUrl,
                    'WESELAJA_API_URL'
                );

                self::validateUrl(
                    $this->callbackUrl,
                    'WESELAJA_CALLBACK_URL'
                );

                self::validateUrl(
                    $this->redirectUrl,
                    'WESELAJA_REDIRECT_URL'
                );

                if (
                    $this->caInfo !== null
                    &&
                    !is_file(
                        $this->caInfo
                    )
                ) {
                    throw new WeselAjaConfigurationException(
                        'WESELAJA_CAINFO must point to a valid CA file.'
                    );
                }
            }

            private static function env(
                string $key,
                string $fallback = ''
            ): string {
                $value =
                    getenv($key);

                if (
                    $value === false
                    ||
                    trim($value) === ''
                ) {
                    return $fallback;
                }

                return trim($value);
            }

            private static function nullableEnv(
                string $key
            ): ?string {
                $value =
                    self::env(
                        $key
                    );

                return $value === ''
                    ? null
                    : $value;
            }

            private static function validateUrl(
                string $value,
                string $key
            ): void {
                $parts =
                    parse_url(
                        $value
                    );

                if (
                    $parts === false
                    ||
                    !isset(
                        $parts['scheme']
                    )
                    ||
                    !in_array(
                        strtolower(
                            $parts['scheme']
                        ),
                        [
                            'http',
                            'https'
                        ],
                        true
                    )
                ) {
                    throw new WeselAjaConfigurationException(
                        $key .
                        ' must be a valid HTTP or HTTPS URL.'
                    );
                }
            }
        }
        """;

    private const string
        BankChannelsTemplate =
        """
        <?php
        declare(strict_types=1);

        namespace WeselAja;

        final class BankChannels
        {
            private const CHANNELS = [
                'BRI' =>
                    'BRI.VA',

                'BANKBRI' =>
                    'BRI.VA',

                'MANDIRI' =>
                    'MDR.VA',

                'BANKMANDIRI' =>
                    'MDR.VA',

                'MDR' =>
                    'MDR.VA',

                'BNI' =>
                    'BNI.VA',

                'BANKBNI' =>
                    'BNI.VA',

                'PERMATA' =>
                    'PTB.VA',

                'BANKPERMATA' =>
                    'PTB.VA',

                'PTB' =>
                    'PTB.VA',

                'CIMB' =>
                    'CIMB.VA',

                'CIMBNIAGA' =>
                    'CIMB.VA',

                'BANKCIMB' =>
                    'CIMB.VA',

                'BCA' =>
                    'BCA.VA',

                'BANKBCA' =>
                    'BCA.VA',
            ];

            public static function resolve(
                string $bank
            ): string {
                if (
                    trim($bank) === ''
                ) {
                    throw new \InvalidArgumentException(
                        'Bank is required.'
                    );
                }

                $normalized =
                    self::normalize(
                        $bank
                    );

                if (
                    isset(
                        self::CHANNELS[
                            $normalized
                        ]
                    )
                ) {
                    return self::CHANNELS[
                        $normalized
                    ];
                }

                throw new \InvalidArgumentException(
                    "Unsupported bank '{$bank}'. " .
                    'Supported banks: ' .
                    'BRI, Mandiri, BNI, Permata, CIMB, BCA.'
                );
            }

            public static function normalize(
                string $bank
            ): string {
                return str_replace(
                    [
                        ' ',
                        '-',
                        '_'
                    ],
                    '',
                    strtoupper(
                        trim($bank)
                    )
                );
            }

            public static function supported(): array
            {
                return [
                    'BRI' =>
                        'BRI.VA',

                    'Mandiri' =>
                        'MDR.VA',

                    'BNI' =>
                        'BNI.VA',

                    'Permata' =>
                        'PTB.VA',

                    'CIMB' =>
                        'CIMB.VA',

                    'BCA' =>
                        'BCA.VA',
                ];
            }
        }
        """;

    private const string
        SignatureTemplate =
        """
        <?php
        declare(strict_types=1);

        namespace WeselAja;

        final class WeselAjaSignature
        {
            public static function generate(
                string $method,
                string $uri,
                string $timestamp,
                string $body,
                string $secret
            ): string {
                $payload =
                    $method . "\n" .
                    $uri . "\n" .
                    $timestamp . "\n" .
                    $body;

                return base64_encode(
                    hash_hmac(
                        'sha256',
                        $payload,
                        $secret,
                        true
                    )
                );
            }

            public static function generateCallback(
                string $method,
                string $uri,
                string $rawBody,
                string $timestamp,
                string $secret
            ): string {
                $payload =
                    $method . "\n" .
                    $uri . "\n" .
                    $rawBody . "\n" .
                    $timestamp;

                return base64_encode(
                    hash_hmac(
                        'sha256',
                        $payload,
                        $secret,
                        true
                    )
                );
            }

            public static function verifyCallback(
                string $method,
                string $uri,
                string $rawBody,
                string $timestamp,
                string $signature,
                string $secret
            ): bool {
                if (
                    trim(
                        $signature
                    ) === ''
                ) {
                    return false;
                }

                $expected =
                    self::generateCallback(
                        $method,
                        $uri,
                        $rawBody,
                        $timestamp,
                        $secret
                    );

                return hash_equals(
                    $expected,
                    trim(
                        $signature
                    )
                );
            }
        }
        """;

    private const string
        ExceptionTemplate =
        """
        <?php
        declare(strict_types=1);

        namespace WeselAja\Exceptions;

        class WeselAjaException
            extends \RuntimeException
        {
        }

        final class
            WeselAjaConfigurationException
            extends WeselAjaException
        {
        }

        final class
            WeselAjaApiException
            extends WeselAjaException
        {
            public function __construct(
                string $message,
                public readonly ?int $statusCode = null,
                public readonly ?string $responseBody = null
            ) {
                parent::__construct(
                    $message
                );
            }
        }
        """;

    private const string
        CreatePaymentRequestTemplate =
        """
        <?php
        declare(strict_types=1);

        namespace WeselAja\Models;

        final class CreatePaymentRequest
        {
            public function __construct(
                public readonly int $amount,
                public readonly string $customerName,
                public readonly string $bank,
                public readonly ?string $customerPhoneNumber = null,
                public readonly ?string $description = null,
                public readonly ?string $referenceCode = null
            ) {
            }

            public function validate(): void
            {
                if ($this->amount <= 0) {
                    throw new \InvalidArgumentException(
                        'Payment amount must be greater than zero.'
                    );
                }

                if (
                    trim($this->customerName) === ''
                ) {
                    throw new \InvalidArgumentException(
                        'Customer name is required.'
                    );
                }

                if (
                    trim($this->bank) === ''
                ) {
                    throw new \InvalidArgumentException(
                        'Bank is required.'
                    );
                }
            }
        }
        """;

    private const string
        CreatePaymentResponseTemplate =
        """
        <?php
        declare(strict_types=1);

        namespace WeselAja\Models;

        final class CreatePaymentResponse
        {
            public function __construct(
                public readonly bool $success,
                public readonly ?string $paymentId,
                public readonly ?int $initiatedAmount,
                public readonly mixed $paymentAmount,
                public readonly mixed $feeAmount,
                public readonly ?string $currency,
                public readonly ?string $paymentMethod,
                public readonly ?string $paymentChannel,
                public readonly ?string $paymentCode,
                public readonly ?string $paymentCodeType,
                public readonly ?string $referenceCode,
                public readonly ?string $customerReference,
                public readonly ?string $customerName,
                public readonly ?string $status,
                public readonly ?string $createdTime,
                public readonly ?string $updatedTime,
                public readonly ?string $expirationTime,
                public readonly ?string $description,
                public readonly ?string $callbackUrl,
                public readonly ?string $redirectUrl,
                public readonly ?string $payerAccountName,
                public readonly ?string $payerAccountNumber,
                public readonly ?string $payerPaymentChannel,
                public readonly array $metadata,
                public readonly ?string $error = null
            ) {
            }

            public static function fromArray(
                array $response
            ): self {
                return new self(
                    success:
                        (bool) (
                            $response['success']
                            ?? false
                        ),

                    paymentId:
                        self::stringValue(
                            $response,
                            'paymentId',
                            'id'
                        ),

                    initiatedAmount:
                        self::intValue(
                            $response,
                            'initiatedAmount'
                        ),

                    paymentAmount:
                        $response[
                            'paymentAmount'
                        ]
                        ?? null,

                    feeAmount:
                        $response[
                            'feeAmount'
                        ]
                        ?? null,

                    currency:
                        self::stringValue(
                            $response,
                            'currency'
                        ),

                    paymentMethod:
                        self::stringValue(
                            $response,
                            'paymentMethod'
                        ),

                    paymentChannel:
                        self::stringValue(
                            $response,
                            'paymentChannel'
                        ),

                    paymentCode:
                        self::stringValue(
                            $response,
                            'paymentCode'
                        ),

                    paymentCodeType:
                        self::stringValue(
                            $response,
                            'paymentCodeType'
                        ),

                    referenceCode:
                        self::stringValue(
                            $response,
                            'referenceCode'
                        ),

                    customerReference:
                        self::stringValue(
                            $response,
                            'customerReference'
                        ),

                    customerName:
                        self::stringValue(
                            $response,
                            'customerName'
                        ),

                    status:
                        self::stringValue(
                            $response,
                            'status'
                        ),

                    createdTime:
                        self::stringValue(
                            $response,
                            'createdTime'
                        ),

                    updatedTime:
                        self::stringValue(
                            $response,
                            'updatedTime'
                        ),

                    expirationTime:
                        self::stringValue(
                            $response,
                            'expirationTime'
                        ),

                    description:
                        self::stringValue(
                            $response,
                            'description'
                        ),

                    callbackUrl:
                        self::stringValue(
                            $response,
                            'callbackUrl'
                        ),

                    redirectUrl:
                        self::stringValue(
                            $response,
                            'redirectUrl'
                        ),

                    payerAccountName:
                        self::stringValue(
                            $response,
                            'payerAccountName'
                        ),

                    payerAccountNumber:
                        self::stringValue(
                            $response,
                            'payerAccountNumber'
                        ),

                    payerPaymentChannel:
                        self::stringValue(
                            $response,
                            'payerPaymentChannel'
                        ),

                    metadata:
                        isset(
                            $response['metadata']
                        )
                        &&
                        is_array(
                            $response['metadata']
                        )
                            ? $response['metadata']
                            : [],

                    error:
                        self::stringValue(
                            $response,
                            'error'
                        )
                );
            }

            private static function stringValue(
                array $array,
                string ...$keys
            ): ?string {
                foreach ($keys as $key) {
                    if (
                        isset($array[$key])
                        &&
                        is_string(
                            $array[$key]
                        )
                    ) {
                        return $array[$key];
                    }
                }

                return null;
            }

            private static function intValue(
                array $array,
                string $key
            ): ?int {
                if (
                    !isset(
                        $array[$key]
                    )
                ) {
                    return null;
                }

                return (int) $array[$key];
            }
        }
        """;

    private const string
        ClientTemplate =
        """
        <?php
        declare(strict_types=1);

        namespace WeselAja;

        use WeselAja\Exceptions\WeselAjaApiException;
        use WeselAja\Models\CreatePaymentRequest;
        use WeselAja\Models\CreatePaymentResponse;

        final class WeselAjaClient
        {
            private const PAYMENT_URI =
                '/v1/payins';

            public function __construct(
                private readonly WeselAjaConfig $config
            ) {
                $this->config->validate();
            }

            public static function fromEnvironment(): self
            {
                return new self(
                    WeselAjaConfig::fromEnvironment()
                );
            }

            public function createPayment(
                CreatePaymentRequest $request
            ): CreatePaymentResponse {
                $request->validate();

                $paymentChannel =
                    BankChannels::resolve(
                        $request->bank
                    );

                $referenceCode =
                    $request->referenceCode;

                if (
                    $referenceCode === null
                    ||
                    trim($referenceCode) === ''
                ) {
                    $referenceCode =
                        $this->generateReference(
                            $request->customerName
                        );
                }

                $this->validateReference(
                    $referenceCode
                );

                $this->validateReference(
                    $this->config->customerReference
                );

                $payload = [
                    'initiatedAmount' =>
                        $request->amount,

                    'currency' =>
                        $this->config->currency,

                    'paymentMethod' =>
                        $this->config->paymentMethod,

                    'paymentChannel' =>
                        $paymentChannel,

                    'referenceCode' =>
                        $referenceCode,

                    'customerReference' =>
                        $this->config->customerReference,

                    'customerName' =>
                        trim(
                            $request->customerName
                        ),

                    'callbackUrl' =>
                        $this->config->callbackUrl,

                    'redirectUrl' =>
                        $this->config->redirectUrl,
                ];

                if (
                    $request->customerPhoneNumber !== null
                    &&
                    trim(
                        $request->customerPhoneNumber
                    ) !== ''
                ) {
                    $payload[
                        'customerPhoneNumber'
                    ] =
                        trim(
                            $request->customerPhoneNumber
                        );
                }

                if (
                    $request->description !== null
                    &&
                    trim(
                        $request->description
                    ) !== ''
                ) {
                    $payload[
                        'description'
                    ] =
                        trim(
                            $request->description
                        );
                }

                $body =
                    json_encode(
                        $payload,
                        JSON_UNESCAPED_SLASHES |
                        JSON_UNESCAPED_UNICODE |
                        JSON_THROW_ON_ERROR
                    );

                $timestamp =
                    (
                        new \DateTimeImmutable(
                            'now',
                            new \DateTimeZone(
                                'UTC'
                            )
                        )
                    )->format(
                        'Y-m-d\TH:i:s.v\Z'
                    );

                $signature =
                    WeselAjaSignature::generate(
                        'POST',
                        self::PAYMENT_URI,
                        $timestamp,
                        $body,
                        $this->config->secretKey
                    );

                $idempotencyKey =
                    'php-' .
                    bin2hex(
                        random_bytes(16)
                    );

                $url =
                    $this->config->apiUrl .
                    self::PAYMENT_URI;

                $curl =
                    curl_init();

                if ($curl === false) {
                    throw new WeselAjaApiException(
                        'Unable to initialize PHP cURL.'
                    );
                }

                $headers = [
                    'Content-Type: application/json',
                    'Accept: application/json',
                    'Xenith-Api-Key: ' .
                        $this->config->apiKey,
                    'Xenith-Request-Timestamp: ' .
                        $timestamp,
                    'Xenith-Request-Signature: ' .
                        $signature,
                    'X-Idempotency-Key: ' .
                        $idempotencyKey,
                ];

                $curlOptions = [
                    CURLOPT_URL =>
                        $url,

                    CURLOPT_POST =>
                        true,

                    CURLOPT_POSTFIELDS =>
                        $body,

                    CURLOPT_HTTPHEADER =>
                        $headers,

                    CURLOPT_RETURNTRANSFER =>
                        true,

                    CURLOPT_HEADER =>
                        false,

                    CURLOPT_CONNECTTIMEOUT =>
                        10,

                    CURLOPT_TIMEOUT =>
                        30,

                    CURLOPT_FOLLOWLOCATION =>
                        false,

                    CURLOPT_HTTP_VERSION =>
                        CURL_HTTP_VERSION_1_1,

                    CURLOPT_FRESH_CONNECT =>
                        true,

                    CURLOPT_FORBID_REUSE =>
                        true,

                    CURLOPT_SSL_VERIFYPEER =>
                        true,

                    CURLOPT_SSL_VERIFYHOST =>
                        2,
                ];

                if (
                    $this->config->caInfo !== null
                    &&
                    trim(
                        $this->config->caInfo
                    ) !== ''
                ) {
                    $curlOptions[
                        CURLOPT_CAINFO
                    ] =
                        $this->config->caInfo;
                }

                curl_setopt_array(
                    $curl,
                    $curlOptions
                );

                $responseBody =
                    curl_exec(
                        $curl
                    );

                $statusCode =
                    (int) curl_getinfo(
                        $curl,
                        CURLINFO_HTTP_CODE
                    );

                if (
                    $responseBody === false
                ) {
                    $error =
                        curl_error(
                            $curl
                        );

                    throw new WeselAjaApiException(
                        'Unable to connect to WeselAja: ' .
                        $error
                    );
                }

                if (
                    !is_string(
                        $responseBody
                    )
                    ||
                    trim(
                        $responseBody
                    ) === ''
                ) {
                    throw new WeselAjaApiException(
                        'Empty response from WeselAja. ' .
                        'HTTP ' .
                        $statusCode,
                        $statusCode
                    );
                }

                try {
                    $decoded =
                        json_decode(
                            $responseBody,
                            true,
                            512,
                            JSON_THROW_ON_ERROR
                        );
                } catch (
                    \JsonException $exception
                ) {
                    throw new WeselAjaApiException(
                        'Invalid JSON response from WeselAja. ' .
                        'HTTP ' .
                        $statusCode .
                        ': ' .
                        $exception->getMessage(),
                        $statusCode,
                        $responseBody
                    );
                }

                if (
                    !is_array(
                        $decoded
                    )
                ) {
                    throw new WeselAjaApiException(
                        'Unexpected WeselAja response format.',
                        $statusCode,
                        $responseBody
                    );
                }

                if (
                    $statusCode < 200
                    ||
                    $statusCode >= 300
                ) {
                    throw new WeselAjaApiException(
                        $this->extractError(
                            $decoded,
                            $responseBody
                        ),
                        $statusCode,
                        $responseBody
                    );
                }

                $decoded['success'] =
                    true;

                return
                    CreatePaymentResponse::fromArray(
                        $decoded
                    );
            }

            public function getPayins(
                ?string $status = null,
                ?int $limit = null
            ): array {
                if (
                    $status !== null
                    &&
                    trim(
                        $status
                    ) !== ''
                ) {
                    $status =
                        strtoupper(
                            trim(
                                $status
                            )
                        );

                    if (
                        !in_array(
                            $status,
                            [
                                'PENDING',
                                'SUCCESS',
                                'EXPIRED',
                                'FAILED',
                            ],
                            true
                        )
                    ) {
                        throw new \InvalidArgumentException(
                            'status must be one of: PENDING, SUCCESS, EXPIRED, FAILED.'
                        );
                    }
                } else {
                    $status =
                        null;
                }

                if (
                    $limit !== null
                    &&
                    $limit <= 0
                ) {
                    throw new \InvalidArgumentException(
                        'limit must be a positive integer.'
                    );
                }

                $query = [];

                if ($status !== null) {
                    $query['status'] =
                        $status;
                }

                if ($limit !== null) {
                    $query['limit'] =
                        $limit;
                }

                $url =
                    $this->config->apiUrl .
                    self::PAYMENT_URI;

                if ($query !== []) {
                    $url .=
                        '?' .
                        http_build_query(
                            $query,
                            '',
                            '&',
                            PHP_QUERY_RFC3986
                        );
                }

                $timestamp =
                    (
                        new \DateTimeImmutable(
                            'now',
                            new \DateTimeZone(
                                'UTC'
                            )
                        )
                    )->format(
                        'Y-m-d\TH:i:s.v\Z'
                    );

                $signature =
                    WeselAjaSignature::generate(
                        'GET',
                        self::PAYMENT_URI,
                        $timestamp,
                        '',
                        $this->config->secretKey
                    );

                $curl =
                    curl_init();

                if ($curl === false) {
                    throw new WeselAjaApiException(
                        'Unable to initialize PHP cURL.'
                    );
                }

                $headers = [
                    'Accept: application/json',
                    'Xenith-Api-Key: ' .
                        $this->config->apiKey,
                    'Xenith-Request-Timestamp: ' .
                        $timestamp,
                    'Xenith-Request-Signature: ' .
                        $signature,
                ];

                $curlOptions = [
                    CURLOPT_URL =>
                        $url,

                    CURLOPT_HTTPGET =>
                        true,

                    CURLOPT_HTTPHEADER =>
                        $headers,

                    CURLOPT_RETURNTRANSFER =>
                        true,

                    CURLOPT_HEADER =>
                        false,

                    CURLOPT_CONNECTTIMEOUT =>
                        10,

                    CURLOPT_TIMEOUT =>
                        30,

                    CURLOPT_FOLLOWLOCATION =>
                        false,

                    CURLOPT_HTTP_VERSION =>
                        CURL_HTTP_VERSION_1_1,

                    CURLOPT_FRESH_CONNECT =>
                        true,

                    CURLOPT_FORBID_REUSE =>
                        true,

                    CURLOPT_SSL_VERIFYPEER =>
                        true,

                    CURLOPT_SSL_VERIFYHOST =>
                        2,
                ];

                if (
                    $this->config->caInfo !== null
                    &&
                    trim(
                        $this->config->caInfo
                    ) !== ''
                ) {
                    $curlOptions[
                        CURLOPT_CAINFO
                    ] =
                        $this->config->caInfo;
                }

                curl_setopt_array(
                    $curl,
                    $curlOptions
                );

                $responseBody =
                    curl_exec(
                        $curl
                    );

                $statusCode =
                    (int) curl_getinfo(
                        $curl,
                        CURLINFO_HTTP_CODE
                    );

                if (
                    $responseBody === false
                ) {
                    throw new WeselAjaApiException(
                        'Unable to connect to WeselAja: ' .
                        curl_error(
                            $curl
                        )
                    );
                }

                if (
                    !is_string(
                        $responseBody
                    )
                    ||
                    trim(
                        $responseBody
                    ) === ''
                ) {
                    throw new WeselAjaApiException(
                        'Empty response from WeselAja. HTTP ' .
                        $statusCode,
                        $statusCode
                    );
                }

                try {
                    $decoded =
                        json_decode(
                            $responseBody,
                            true,
                            512,
                            JSON_THROW_ON_ERROR
                        );
                } catch (
                    \JsonException $exception
                ) {
                    throw new WeselAjaApiException(
                        'Invalid JSON response from WeselAja. ' .
                        'HTTP ' .
                        $statusCode .
                        ': ' .
                        $exception->getMessage(),
                        $statusCode,
                        $responseBody
                    );
                }

                if (
                    !is_array(
                        $decoded
                    )
                ) {
                    throw new WeselAjaApiException(
                        'Unexpected WeselAja response format.',
                        $statusCode,
                        $responseBody
                    );
                }

                if (
                    $statusCode < 200
                    ||
                    $statusCode >= 300
                ) {
                    throw new WeselAjaApiException(
                        $this->extractError(
                            $decoded,
                            $responseBody
                        ),
                        $statusCode,
                        $responseBody
                    );
                }

                return $decoded;
            }

            private function generateReference(
                string $customerName
            ): string {
                $normalized =
                    preg_replace(
                        '/[^A-Z0-9_-]/',
                        '',
                        strtoupper(
                            $customerName
                        )
                    );

                if (
                    !is_string(
                        $normalized
                    )
                    ||
                    trim(
                        $normalized
                    ) === ''
                ) {
                    $normalized =
                        'CUSTOMER';
                }

                $normalized =
                    substr(
                        $normalized,
                        0,
                        20
                    );

                return
                    'WES-' .
                    $normalized .
                    '-' .
                    gmdate(
                        'YmdHis'
                    );
            }

            private function validateReference(
                string $value
            ): void {
                if (
                    trim(
                        $value
                    ) === ''
                ) {
                    throw new \InvalidArgumentException(
                        'Reference value is required.'
                    );
                }

                if (
                    preg_match(
                        '/^[A-Za-z0-9_-]+$/',
                        $value
                    ) !== 1
                ) {
                    throw new \InvalidArgumentException(
                        'Reference values may only contain ' .
                        'letters, numbers, "-" and "_".'
                    );
                }
            }

            private function extractError(
                array $response,
                string $rawBody
            ): string {
                foreach (
                    [
                        'message',
                        'error',
                        'code',
                    ] as $key
                ) {
                    if (
                        isset(
                            $response[$key]
                        )
                        &&
                        is_string(
                            $response[$key]
                        )
                        &&
                        trim(
                            $response[$key]
                        ) !== ''
                    ) {
                        return
                            $response[$key];
                    }
                }

                return $rawBody;
            }
        }
        """;

    private const string
        RedirectTemplate =
        """
        <?php
        declare(strict_types=1);

        $path =
            parse_url(
                $_SERVER['REQUEST_URI'] ?? '/',
                PHP_URL_PATH
            );

        if (
            $path !==
            '/payment/weselaja/return'
        ) {
            http_response_code(404);

            header(
                'Content-Type: application/json'
            );

            echo json_encode(
                [
                    'success' => false,
                    'error' => 'Not found'
                ],
                JSON_PRETTY_PRINT
            );

            exit;
        }

        header(
            'Content-Type: application/json'
        );

        echo json_encode(
            [
                'success' => true,
                'path' => $path,
                'parameters' => $_GET
            ],
            JSON_PRETTY_PRINT
        );
        """;

    private const string
        CallbackTemplate =
        """
        <?php
        declare(strict_types=1);

        require_once __DIR__ .
            '/src/EnvLoader.php';

        require_once __DIR__ .
            '/src/WeselAjaSignature.php';

        \WeselAja\EnvLoader::load();

        $path =
            parse_url(
                $_SERVER['REQUEST_URI'] ?? '/',
                PHP_URL_PATH
            );

        if (
            $path !==
            '/payment/weselaja/callback'
        ) {
            http_response_code(404);

            header(
                'Content-Type: application/json'
            );

            echo json_encode(
                [
                    'success' => false,
                    'error' => 'Not found'
                ],
                JSON_PRETTY_PRINT
            );

            exit;
        }

        if (
            strtoupper(
                $_SERVER['REQUEST_METHOD'] ?? 'GET'
            ) !== 'POST'
        ) {
            http_response_code(405);

            header(
                'Content-Type: application/json'
            );

            echo json_encode(
                [
                    'success' => false,
                    'error' => 'Method not allowed'
                ],
                JSON_PRETTY_PRINT
            );

            exit;
        }

        $secret =
            getenv(
                'WEB_SIGNATURE_SECRET'
            );

        if (
            $secret === false
            ||
            trim(
                $secret
            ) === ''
        ) {
            http_response_code(500);

            header(
                'Content-Type: application/json'
            );

            echo json_encode(
                [
                    'success' => false,
                    'error' =>
                        'WEB_SIGNATURE_SECRET is required'
                ],
                JSON_PRETTY_PRINT
            );

            exit;
        }

        $timestamp =
            $_SERVER[
                'HTTP_X_MIDDLEWARE_TIMESTAMP'
            ]
            ??
            '';

        $signature =
            $_SERVER[
                'HTTP_X_MIDDLEWARE_SIGNATURE'
            ]
            ??
            '';

        if (
            trim(
                $timestamp
            ) === ''
        ) {
            http_response_code(400);

            header(
                'Content-Type: application/json'
            );

            echo json_encode(
                [
                    'success' => false,
                    'error' =>
                        'Missing x-middleware-timestamp'
                ],
                JSON_PRETTY_PRINT
            );

            exit;
        }

        if (
            trim(
                $signature
            ) === ''
        ) {
            http_response_code(400);

            header(
                'Content-Type: application/json'
            );

            echo json_encode(
                [
                    'success' => false,
                    'error' =>
                        'Missing x-middleware-signature'
                ],
                JSON_PRETTY_PRINT
            );

            exit;
        }

        $rawBody =
            file_get_contents(
                'php://input'
            );

        if (
            !is_string(
                $rawBody
            )
        ) {
            $rawBody =
                '';
        }

        $valid =
            \WeselAja\WeselAjaSignature::verifyCallback(
                'POST',
                $path,
                $rawBody,
                $timestamp,
                $signature,
                $secret
            );

        if (!$valid) {
            http_response_code(401);

            header(
                'Content-Type: application/json'
            );

            echo json_encode(
                [
                    'success' => false,
                    'verified' => false,
                    'error' =>
                        'Invalid callback signature'
                ],
                JSON_PRETTY_PRINT
            );

            exit;
        }

        $decoded =
            json_decode(
                $rawBody,
                true
            );

        header(
            'Content-Type: application/json'
        );

        echo json_encode(
            [
                'success' => true,
                'verified' => true,
                'body' =>
                    is_array(
                        $decoded
                    )
                        ? $decoded
                        : $rawBody
            ],
            JSON_PRETTY_PRINT |
            JSON_UNESCAPED_SLASHES |
            JSON_UNESCAPED_UNICODE
        );
        """;

    private const string
        ExampleTemplate =
        """
        <?php
        declare(strict_types=1);

        require_once __DIR__ .
            '/src/Exceptions/WeselAjaException.php';

        require_once __DIR__ .
            '/src/Models/CreatePaymentRequest.php';

        require_once __DIR__ .
            '/src/Models/CreatePaymentResponse.php';

        require_once __DIR__ .
            '/src/BankChannels.php';

        require_once __DIR__ .
            '/src/EnvLoader.php';

        require_once __DIR__ .
            '/src/WeselAjaConfig.php';

        require_once __DIR__ .
            '/src/WeselAjaSignature.php';

        require_once __DIR__ .
            '/src/WeselAjaClient.php';

        use WeselAja\WeselAjaClient;
        use WeselAja\Models\CreatePaymentRequest;

        $client =
            WeselAjaClient::fromEnvironment();

        $payment =
            $client->createPayment(
                new CreatePaymentRequest(
                    amount: 10000,
                    customerName: 'Andi',
                    bank: 'BRI',
                    description:
                        'WeselAja sandbox example'
                )
            );

        echo
            'Payment created successfully.'
            . PHP_EOL;

        echo
            'Status   : ' .
            ($payment->status ?? '-')
            . PHP_EOL;

        echo
            'Reference: ' .
            ($payment->referenceCode ?? '-')
            . PHP_EOL;

        echo
            'Channel  : ' .
            ($payment->paymentChannel ?? '-')
            . PHP_EOL;

        echo
            'Payment  : ' .
            ($payment->paymentCode ?? '-')
            . PHP_EOL;
        """;

    private const string
        SelfTestTemplate =
        """
        <?php
        declare(strict_types=1);

        require_once __DIR__ .
            '/../src/BankChannels.php';

        require_once __DIR__ .
            '/../src/Exceptions/WeselAjaException.php';

        require_once __DIR__ .
            '/../src/WeselAjaSignature.php';

        use WeselAja\BankChannels;
        use WeselAja\WeselAjaSignature;

        function assertTest(
            bool $condition,
            string $message
        ): void {
            if (!$condition) {
                throw new RuntimeException(
                    $message
                );
            }
        }

        assertTest(
            BankChannels::resolve('BRI') ===
                'BRI.VA',
            'BRI mapping failed.'
        );

        assertTest(
            BankChannels::resolve(
                'Bank BRI'
            ) ===
                'BRI.VA',
            'Bank BRI mapping failed.'
        );

        assertTest(
            BankChannels::resolve(
                'Mandiri'
            ) ===
                'MDR.VA',
            'Mandiri mapping failed.'
        );

        assertTest(
            BankChannels::resolve(
                'Bank Mandiri'
            ) ===
                'MDR.VA',
            'Bank Mandiri mapping failed.'
        );

        assertTest(
            BankChannels::resolve(
                'BNI'
            ) ===
                'BNI.VA',
            'BNI mapping failed.'
        );

        assertTest(
            BankChannels::resolve(
                'Permata'
            ) ===
                'PTB.VA',
            'Permata mapping failed.'
        );

        assertTest(
            BankChannels::resolve(
                'CIMB'
            ) ===
                'CIMB.VA',
            'CIMB mapping failed.'
        );

        assertTest(
            BankChannels::resolve(
                'CIMB Niaga'
            ) ===
                'CIMB.VA',
            'CIMB Niaga mapping failed.'
        );

        assertTest(
            BankChannels::resolve(
                'BCA'
            ) ===
                'BCA.VA',
            'BCA mapping failed.'
        );

        $unsupportedRejected =
            false;

        try {
            BankChannels::resolve(
                'Bank Tidak Ada'
            );
        } catch (
            InvalidArgumentException
        ) {
            $unsupportedRejected =
                true;
        }

        assertTest(
            $unsupportedRejected,
            'Unsupported bank must be rejected.'
        );

        $method =
            'POST';

        $uri =
            '/payment/weselaja/callback';

        $body =
            '{"hello":"world"}';

        $timestamp =
            '2026-01-01T00:00:00.000Z';

        $secret =
            'test-secret';

        $signature =
            WeselAjaSignature::generateCallback(
                $method,
                $uri,
                $body,
                $timestamp,
                $secret
            );

        assertTest(
            WeselAjaSignature::verifyCallback(
                $method,
                $uri,
                $body,
                $timestamp,
                $signature,
                $secret
            ),
            'Callback signature verification failed.'
        );

        assertTest(
            !WeselAjaSignature::verifyCallback(
                $method,
                $uri,
                $body,
                $timestamp,
                $signature . 'x',
                $secret
            ),
            'Invalid callback signature must be rejected.'
        );

        echo
            'All PHP WeselAja self-tests passed.'
            . PHP_EOL;
        """;
}