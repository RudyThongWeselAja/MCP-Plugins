using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class GoWeselAjaGenerator
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

        var goRoot =
            Path.GetFullPath(
                Path.Combine(
                    outputRoot,
                    "go"));

        var projectDirectory =
            Path.GetFullPath(
                Path.Combine(
                    goRoot,
                    safeProjectName));

        EnsureInsideDirectory(
            projectDirectory,
            goRoot);

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

        files["go.mod"] =
            BuildGoMod(
                projectName);

        files["weselaja.go"] =
            WeselAjaClientTemplate;

        files["models.go"] =
            ModelsTemplate;

        files["bank_channels.go"] =
            BankChannelsTemplate;

        files["signature.go"] =
            SignatureTemplate;

        files["config.go"] =
            ConfigTemplate;

        files["env.go"] =
            EnvTemplate;

        files["errors.go"] =
            ErrorsTemplate;

        files[".env.example"] =
            EnvExampleTemplate;

        files[".gitignore"] =
            GitIgnoreTemplate;

        files["README.md"] =
            BuildReadmeTemplate(
                projectName);

        if (includeExample)
        {
            files["main.go"] =
                MainTemplate;
        }

        files["cmd/redirect-server/main.go"] =
            RedirectServerTemplate;

        files["cmd/callback-server/main.go"] =
            CallbackServerTemplate;

        if (includeTests)
        {
            files["weselaja_test.go"] =
                SelfTestTemplate;
        }

        return files;
    }

    private static string BuildGoMod(
        string projectName)
    {
        return $"""
        module example.com/{projectName}

        go 1.23

        """;
    }

    private static string SanitizeProjectName(
        string projectName)
    {
        if (
            string.IsNullOrWhiteSpace(
                projectName))
        {
            return "weselaja-go";
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
            return "weselaja-go";
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
            Path.GetFullPath(path);

        var fullRoot =
            Path.GetFullPath(root);

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

        Standalone Go integration for the WeselAja payment provider.

        This project was generated by the WeselAja Code Generator.

        ## Scope

        The generated integration supports:

        - create payment
        - get payins
        - HMAC-SHA256 request signing
        - bank/channel mapping
        - structured payment response
        - configuration through `.env`
        - validation
        - error handling
        - idempotency key generation
        - callback handling
        - callback signature verification
        - redirect handling

        ## Requirements

        - Go 1.23+

        The project uses Go standard-library packages only.

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
        WESELAJA_CALLBACK_PORT=8081
        WESELAJA_REDIRECT_PORT=8080
        WEB_SIGNATURE_SECRET=
        ```

        `WEB_SIGNATURE_SECRET` is used for callback signature verification.

        Never commit `.env`.

        ## Install

        ```bash
        go mod tidy
        ```

        ## Create payment

        ```bash
        go run .
        ```

        ## Get payins

        ```bash
        go run . payins
        ```

        With filters:

        ```bash
        go run . payins SUCCESS 50
        ```

        Supported status values:

        ```text
        PENDING
        SUCCESS
        EXPIRED
        FAILED
        ```

        The client calls:

        ```text
        GET /v1/payins
        ```

        ## Callback server

        Start:

        ```bash
        go run ./cmd/callback-server
        ```

        Default endpoint:

        ```text
        http://0.0.0.0:8081/payment/weselaja/callback
        ```

        The callback handler validates:

        ```text
        x-middleware-timestamp
        x-middleware-signature
        ```

        The signature payload is:

        ```text
        POST
        /payment/weselaja/callback
        <raw body>
        <timestamp>
        ```

        HMAC-SHA256 is used and the signature is encoded using Base64.

        ## Redirect server

        Start:

        ```bash
        go run ./cmd/redirect-server
        ```

        Default endpoint:

        ```text
        http://0.0.0.0:8080/payment/weselaja/return
        ```

        ## Tests

        ```bash
        go test ./...
        ```

        Tests validate:

        - bank mappings
        - unsupported bank rejection
        - callback signature generation
        - callback signature verification

        ## Example

        ```go
        client, err :=
            NewClientFromEnvironment()

        if err != nil {
            log.Fatal(err)
        }

        payment, err :=
            client.CreatePayment(
                context.Background(),
                CreatePaymentRequest{
                    Amount:
                        10000,
                    CustomerName:
                        "Andi",
                    Bank:
                        "BRI",
                    Description:
                        "Sandbox payment",
                },
            )

        if err != nil {
            log.Fatal(err)
        }

        fmt.Println(
            payment.Status)

        fmt.Println(
            payment.PaymentCode)
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

        ## Request signing

        Signature input:

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

        """;
    }

    private const string
        ConfigTemplate =
        """

        package main

        import (
            "fmt"
            "net/url"
            "os"
            "strings"
        )

        type Config struct {
            APIURL string

            APIKey string

            SecretKey string

            CustomerReference string

            CallbackURL string

            RedirectURL string

            Currency string

            PaymentMethod string
        }

        func LoadConfig() (
            Config,
            error,
        ) {
            LoadDotEnv(".env")

            config :=
                Config{
                    APIURL:
                        getEnv(
                            "WESELAJA_API_URL",
                            "https://sandbox.checkout.weselaja.id",
                        ),

                    APIKey:
                        getEnv(
                            "WESELAJA_API_KEY",
                            "",
                        ),

                    SecretKey:
                        getEnv(
                            "WESELAJA_SECRET_KEY",
                            "",
                        ),

                    CustomerReference:
                        getEnv(
                            "WESELAJA_CUSTOMER_REFERENCE",
                            "",
                        ),

                    CallbackURL:
                        getEnv(
                            "WESELAJA_CALLBACK_URL",
                            "",
                        ),

                    RedirectURL:
                        getEnv(
                            "WESELAJA_REDIRECT_URL",
                            "",
                        ),

                    Currency:
                        getEnv(
                            "WESELAJA_CURRENCY",
                            "IDR",
                        ),

                    PaymentMethod:
                        getEnv(
                            "WESELAJA_PAYMENT_METHOD",
                            "VIRTUAL_ACCOUNT",
                        ),
                }

            if err :=
                config.Validate();
                err != nil {
                return Config{}, err
            }

            return config, nil
        }

        func (c Config) Validate() error {
            required :=
                map[string]string{
                    "WESELAJA_API_URL":
                        c.APIURL,

                    "WESELAJA_API_KEY":
                        c.APIKey,

                    "WESELAJA_SECRET_KEY":
                        c.SecretKey,

                    "WESELAJA_CUSTOMER_REFERENCE":
                        c.CustomerReference,

                    "WESELAJA_CALLBACK_URL":
                        c.CallbackURL,

                    "WESELAJA_REDIRECT_URL":
                        c.RedirectURL,

                    "WESELAJA_CURRENCY":
                        c.Currency,

                    "WESELAJA_PAYMENT_METHOD":
                        c.PaymentMethod,
                }

            missing :=
                make([]string, 0)

            for key, value :=
                range required {
                if strings.TrimSpace(value) == "" {
                    missing =
                        append(
                            missing,
                            key,
                        )
                }
            }

            if len(missing) > 0 {
                return fmt.Errorf(
                    "missing configuration: %s",
                    strings.Join(
                        missing,
                        ", ",
                    ),
                )
            }

            for _, item :=
                range ([]struct {
                    name  string
                    value string
                }{
                    {
                        "WESELAJA_API_URL",
                        c.APIURL,
                    },
                    {
                        "WESELAJA_CALLBACK_URL",
                        c.CallbackURL,
                    },
                    {
                        "WESELAJA_REDIRECT_URL",
                        c.RedirectURL,
                    },
                }) {
                parsed, err :=
                    url.Parse(
                        item.value,
                    )

                if err != nil ||
                    (parsed.Scheme != "http" &&
                        parsed.Scheme != "https") {
                    return fmt.Errorf(
                        "%s must be a valid HTTP or HTTPS URL",
                        item.name,
                    )
                }
            }

            return nil
        }

        func getEnv(
            key string,
            fallback string,
        ) string {
            value :=
                strings.TrimSpace(
                    os.Getenv(key),
                )

            if value == "" {
                return fallback
            }

            return value
        }

        """;

    private const string
        EnvTemplate =
        """

        package main

        import (
            "bufio"
            "os"
            "strings"
        )

        func LoadDotEnv(
            fileName string,
        ) {
            file, err :=
                os.Open(
                    fileName,
                )

            if err != nil {
                return
            }

            defer file.Close()

            scanner :=
                bufio.NewScanner(
                    file,
                )

            for scanner.Scan() {
                line :=
                    strings.TrimSpace(
                        strings.TrimPrefix(
                            scanner.Text(),
                            "\uFEFF",
                        ),
                    )

                if line == "" ||
                    strings.HasPrefix(
                        line,
                        "#",
                    ) {
                    continue
                }

                separator :=
                    strings.Index(
                        line,
                        "=",
                    )

                if separator <= 0 {
                    continue
                }

                key :=
                    strings.TrimSpace(
                        line[:separator],
                    )

                value :=
                    strings.TrimSpace(
                        line[separator+1:],
                    )

                value =
                    removeQuotes(
                        value,
                    )

                if os.Getenv(key) == "" {
                    _ =
                        os.Setenv(
                            key,
                            value,
                        )
                }
            }
        }

        func removeQuotes(
            value string,
        ) string {
            if len(value) >= 2 {
                first :=
                    value[0]

                last :=
                    value[len(value)-1]

                if (first == '"' &&
                    last == '"') ||
                    (first == '\'' &&
                        last == '\'') {
                    return value[1 : len(value)-1]
                }
            }

            return value
        }

        """;

    private const string
        ErrorsTemplate =
        """

        package main

        import "fmt"

        type WeselAjaError struct {
            Message string
        }

        func (
            e *WeselAjaError,
        ) Error() string {
            return e.Message
        }

        type ConfigurationError struct {
            Message string
        }

        func (
            e *ConfigurationError,
        ) Error() string {
            return e.Message
        }

        type APIError struct {
            StatusCode int

            Message string

            ResponseBody string
        }

        func (
            e *APIError,
        ) Error() string {
            if e.StatusCode > 0 {
                return fmt.Sprintf(
                    "WeselAja API error (%d): %s",
                    e.StatusCode,
                    e.Message,
                )
            }

            return e.Message
        }

        """;

    private const string
        ModelsTemplate =
        """

        package main

        import (
            "encoding/json"
            "strconv"
        )

        type FlexibleInt64 int64

        func (
            v *FlexibleInt64,
        ) UnmarshalJSON(
            data []byte,
        ) error {
            if string(data) == "null" {
                *v = 0
                return nil
            }

            var number int64

            if err :=
                json.Unmarshal(
                    data,
                    &number,
                ); err == nil {
                *v =
                    FlexibleInt64(
                        number,
                    )

                return nil
            }

            var value string

            if err :=
                json.Unmarshal(
                    data,
                    &value,
                ); err != nil {
                return err
            }

            number, err :=
                strconv.ParseInt(
                    value,
                    10,
                    64,
                )

            if err != nil {
                return err
            }

            *v =
                FlexibleInt64(
                    number,
                )

            return nil
        }

        type CreatePaymentRequest struct {
            Amount int

            CustomerName string

            Bank string

            CustomerPhoneNumber string

            Description string

            ReferenceCode string
        }

        type CreatePaymentResponse struct {
            Success bool `json:"success"`

            ID string `json:"id"`

            InitiatedAmount FlexibleInt64 `json:"initiatedAmount"`

            PaymentAmount any `json:"paymentAmount"`

            FeeAmount any `json:"feeAmount"`

            Currency string `json:"currency"`

            PaymentMethod string `json:"paymentMethod"`

            PaymentChannel string `json:"paymentChannel"`

            PaymentCode string `json:"paymentCode"`

            PaymentCodeType string `json:"paymentCodeType"`

            ReferenceCode string `json:"referenceCode"`

            CustomerReference string `json:"customerReference"`

            CustomerName string `json:"customerName"`

            Status string `json:"status"`

            CreatedTime string `json:"createdTime"`

            UpdatedTime string `json:"updatedTime"`

            ExpirationTime string `json:"expirationTime"`

            Description string `json:"description"`

            CallbackURL string `json:"callbackUrl"`

            RedirectURL string `json:"redirectUrl"`

            Error string `json:"error"`

            Metadata map[string]any `json:"metadata"`
        }

        """;

    private const string
        BankChannelsTemplate =
        """

        package main

        import (
            "fmt"
            "strings"
        )

        var bankChannels =
            map[string]string{
                "BRI":
                    "BRI.VA",

                "BANKBRI":
                    "BRI.VA",

                "MANDIRI":
                    "MDR.VA",

                "BANKMANDIRI":
                    "MDR.VA",

                "MDR":
                    "MDR.VA",

                "BNI":
                    "BNI.VA",

                "BANKBNI":
                    "BNI.VA",

                "PERMATA":
                    "PTB.VA",

                "BANKPERMATA":
                    "PTB.VA",

                "PTB":
                    "PTB.VA",

                "CIMB":
                    "CIMB.VA",

                "CIMBNIAGA":
                    "CIMB.VA",

                "BANKCIMB":
                    "CIMB.VA",

                "BCA":
                    "BCA.VA",

                "BANKBCA":
                    "BCA.VA",
            }

        func normalizeBank(
            bank string,
        ) string {
            value :=
                strings.ToUpper(
                    strings.TrimSpace(
                        bank,
                    ),
                )

            replacer :=
                strings.NewReplacer(
                    " ",
                    "",
                    "-",
                    "",
                    "_",
                    "",
                )

            return replacer.Replace(
                value,
            )
        }

        func resolveBankChannel(
            bank string,
        ) (string, error) {
            if strings.TrimSpace(bank) == "" {
                return "",
                    fmt.Errorf(
                        "bank is required",
                    )
            }

            normalized :=
                normalizeBank(
                    bank,
                )

            channel,
                ok :=
                bankChannels[
                    normalized,
                ]

            if !ok {
                return "",
                    fmt.Errorf(
                        "unsupported bank %q; supported banks: BRI, Mandiri, BNI, Permata, CIMB, BCA",
                        bank,
                    )
            }

            return channel, nil
        }

        func supportedBanks() map[string]string {
            return map[string]string{
                "BRI":
                    "BRI.VA",

                "Mandiri":
                    "MDR.VA",

                "BNI":
                    "BNI.VA",

                "Permata":
                    "PTB.VA",

                "CIMB":
                    "CIMB.VA",

                "BCA":
                    "BCA.VA",
            }
        }

        """;

    private const string
        SignatureTemplate =
        """

        package main

        import (
            "crypto/hmac"
            "crypto/sha256"
            "encoding/base64"
            "strings"
        )

        func GenerateSignature(
            method string,
            uri string,
            timestamp string,
            body string,
            secret string,
        ) string {
            payload :=
                method +
                "\n" +
                uri +
                "\n" +
                timestamp +
                "\n" +
                body

            mac :=
                hmac.New(
                    sha256.New,
                    []byte(secret),
                )

            _,
                _ =
                mac.Write(
                    []byte(payload),
                )

            return base64.StdEncoding.EncodeToString(
                    mac.Sum(nil),
                )
        }

        func GenerateCallbackSignature(
            method string,
            uri string,
            rawBody string,
            timestamp string,
            secret string,
        ) string {
            payload :=
                method +
                "\n" +
                uri +
                "\n" +
                rawBody +
                "\n" +
                timestamp

            mac :=
                hmac.New(
                    sha256.New,
                    []byte(secret),
                )

            _,
                _ =
                mac.Write(
                    []byte(payload),
                )

            return base64.StdEncoding.EncodeToString(
                    mac.Sum(nil),
                )
        }

        func VerifyCallbackSignature(
            method string,
            uri string,
            rawBody string,
            timestamp string,
            signature string,
            secret string,
        ) bool {
            if signature == "" {
                return false
            }

            expected :=
                GenerateCallbackSignature(
                    method,
                    uri,
                    rawBody,
                    timestamp,
                    secret,
                )

            return hmac.Equal(
                    []byte(expected),
                    []byte(
                        strings.TrimSpace(
                            signature,
                        ),
                    ),
                )
        }

        """;

    private const string
        WeselAjaClientTemplate =
        """

        package main

        import (
            "bytes"
            "context"
            "crypto/rand"
            "encoding/hex"
            "encoding/json"
            "fmt"
            "io"
            "net/http"
            "net/url"
            "strings"
            "time"
        )

        const paymentURI =
            "/v1/payins"

        type WeselAjaClient struct {
            config Config

            httpClient *http.Client
        }

        func NewClient(
            config Config,
        ) (*WeselAjaClient, error) {
            if err :=
                config.Validate();
                err != nil {
                return nil, err
            }

            return &WeselAjaClient{
                config:
                    config,

                httpClient:
                    &http.Client{
                        Timeout:
                            30 *
                            time.Second,

                        CheckRedirect:
                            func(
                                req *http.Request,
                                via []*http.Request,
                            ) error {
                                return http.ErrUseLastResponse
                            },
                    },
            }, nil
        }

        func NewClientFromEnvironment() (
            *WeselAjaClient,
            error,
        ) {
            config, err :=
                LoadConfig()

            if err != nil {
                return nil, err
            }

            return NewClient(
                config,
            )
        }

        func (
            client *WeselAjaClient,
        ) CreatePayment(
            ctx context.Context,
            payment CreatePaymentRequest,
        ) (
            CreatePaymentResponse,
            error,
        ) {
            if err :=
                validatePaymentRequest(
                    payment,
                ); err != nil {
                return CreatePaymentResponse{}, err
            }

            paymentChannel,
                err :=
                resolveBankChannel(
                    payment.Bank,
                )

            if err != nil {
                return CreatePaymentResponse{}, err
            }

            referenceCode :=
                strings.TrimSpace(
                    payment.ReferenceCode,
                )

            if referenceCode == "" {
                referenceCode =
                    generateReference(
                        payment.CustomerName,
                    )
            }

            if err :=
                validateReference(
                    referenceCode,
                ); err != nil {
                return CreatePaymentResponse{}, err
            }

            if err :=
                validateReference(
                    client.config.CustomerReference,
                ); err != nil {
                return CreatePaymentResponse{}, err
            }

            payload :=
                map[string]any{
                    "initiatedAmount":
                        payment.Amount,

                    "currency":
                        client.config.Currency,

                    "paymentMethod":
                        client.config.PaymentMethod,

                    "paymentChannel":
                        paymentChannel,

                    "referenceCode":
                        referenceCode,

                    "customerReference":
                        client.config.CustomerReference,

                    "customerName":
                        strings.TrimSpace(
                            payment.CustomerName,
                        ),

                    "callbackUrl":
                        client.config.CallbackURL,

                    "redirectUrl":
                        client.config.RedirectURL,
                }

            if strings.TrimSpace(
                payment.CustomerPhoneNumber,
            ) != "" {
                payload[
                    "customerPhoneNumber",
                ] =
                    strings.TrimSpace(
                        payment.CustomerPhoneNumber,
                    )
            }

            if strings.TrimSpace(
                payment.Description,
            ) != "" {
                payload[
                    "description",
                ] =
                    strings.TrimSpace(
                        payment.Description,
                    )
            }

            bodyBytes,
                err :=
                json.Marshal(
                    payload,
                )

            if err != nil {
                return CreatePaymentResponse{}, fmt.Errorf(
                        "failed to encode payment request: %w",
                        err,
                    )
            }

            body :=
                string(
                    bodyBytes,
                )

            timestamp :=
                time.Now().
                    UTC().
                    Format(
                        "2006-01-02T15:04:05.000Z",
                    )

            signature :=
                GenerateSignature(
                    "POST",
                    paymentURI,
                    timestamp,
                    body,
                    client.config.SecretKey,
                )

            idempotencyKey,
                err :=
                generateIdempotencyKey()

            if err != nil {
                return CreatePaymentResponse{}, fmt.Errorf(
                        "failed to generate idempotency key: %w",
                        err,
                    )
            }

            requestURL :=
                strings.TrimRight(
                    client.config.APIURL,
                    "/",
                ) +
                paymentURI

            httpRequest,
                err :=
                http.NewRequestWithContext(
                    ctx,
                    http.MethodPost,
                    requestURL,
                    bytes.NewReader(
                        bodyBytes,
                    ),
                )

            if err != nil {
                return CreatePaymentResponse{}, fmt.Errorf(
                        "failed to create HTTP request: %w",
                        err,
                    )
            }

            httpRequest.Header.Set(
                "Content-Type",
                "application/json",
            )

            httpRequest.Header.Set(
                "Accept",
                "application/json",
            )

            httpRequest.Header.Set(
                "User-Agent",
                "WeselAja-Go/1.0",
            )

            httpRequest.Header.Set(
                "Accept-Encoding",
                "identity",
            )

            httpRequest.Header.Set(
                "Connection",
                "close",
            )

            httpRequest.Header.Set(
                "Xenith-Api-Key",
                client.config.APIKey,
            )

            httpRequest.Header.Set(
                "Xenith-Request-Timestamp",
                timestamp,
            )

            httpRequest.Header.Set(
                "Xenith-Request-Signature",
                signature,
            )

            httpRequest.Header.Set(
                "X-Idempotency-Key",
                idempotencyKey,
            )

            response,
                err :=
                client.httpClient.Do(
                    httpRequest,
                )

            if err != nil {
                return CreatePaymentResponse{}, fmt.Errorf(
                        "unable to connect to WeselAja: %w",
                        err,
                    )
            }

            defer response.Body.Close()

            responseBody,
                err :=
                io.ReadAll(
                    response.Body,
                )

            if err != nil {
                return CreatePaymentResponse{}, fmt.Errorf(
                        "failed to read WeselAja response: %w",
                        err,
                    )
            }

            if len(
                bytes.TrimSpace(
                    responseBody,
                ),
            ) == 0 {
                return CreatePaymentResponse{}, &APIError{
                        StatusCode:
                            response.StatusCode,

                        Message:
                            "empty response from WeselAja",

                        ResponseBody:
                            "",
                    }
            }

            var decoded any

            if err :=
                json.Unmarshal(
                    responseBody,
                    &decoded,
                ); err != nil {
                return CreatePaymentResponse{}, &APIError{
                        StatusCode:
                            response.StatusCode,

                        Message:
                            fmt.Sprintf(
                                "invalid JSON response from WeselAja: %v",
                                err,
                            ),

                        ResponseBody:
                            string(
                                responseBody,
                            ),
                    }
            }

            if response.StatusCode < 200 ||
                response.StatusCode >= 300 {
                return CreatePaymentResponse{}, &APIError{
                        StatusCode:
                            response.StatusCode,

                        Message:
                            extractAPIError(
                                decoded,
                                string(
                                    responseBody,
                                ),
                            ),

                        ResponseBody:
                            string(
                                responseBody,
                            ),
                    }
            }

            var result CreatePaymentResponse

            if err :=
                json.Unmarshal(
                    responseBody,
                    &result,
                ); err != nil {
                return CreatePaymentResponse{}, fmt.Errorf(
                        "failed to parse WeselAja response: %w",
                        err,
                    )
            }

            result.Success =
                true

            return result, nil
        }

        func (
            client *WeselAjaClient,
        ) GetPayins(
            ctx context.Context,
            status string,
            limit *int,
        ) (map[string]any, error) {
            normalizedStatus :=
                strings.TrimSpace(
                    strings.ToUpper(
                        status,
                    ),
                )

            if normalizedStatus != "" {
                switch normalizedStatus {
                case
                    "PENDING",
                    "SUCCESS",
                    "EXPIRED",
                    "FAILED":
                default:
                    return nil,
                        fmt.Errorf(
                            "status must be one of: PENDING, SUCCESS, EXPIRED, FAILED",
                        )
                }
            }

            if limit != nil &&
                *limit <= 0 {
                return nil,
                    fmt.Errorf(
                        "limit must be a positive integer",
                    )
            }

            query :=
                url.Values{}

            if normalizedStatus != "" {
                query.Set(
                    "status",
                    normalizedStatus,
                )
            }

            if limit != nil {
                query.Set(
                    "limit",
                    fmt.Sprintf(
                        "%d",
                        *limit,
                    ),
                )
            }

            requestURL :=
                strings.TrimRight(
                    client.config.APIURL,
                    "/",
                ) +
                paymentURI

            if encoded :=
                query.Encode();
                encoded != "" {
                requestURL +=
                    "?" +
                    encoded
            }

            timestamp :=
                time.Now().
                    UTC().
                    Format(
                        "2006-01-02T15:04:05.000Z",
                    )

            signature :=
                GenerateSignature(
                    "GET",
                    paymentURI,
                    timestamp,
                    "",
                    client.config.SecretKey,
                )

            httpRequest,
                err :=
                http.NewRequestWithContext(
                    ctx,
                    http.MethodGet,
                    requestURL,
                    nil,
                )

            if err != nil {
                return nil,
                    fmt.Errorf(
                        "failed to create HTTP request: %w",
                        err,
                    )
            }

            httpRequest.Header.Set(
                "Accept",
                "application/json",
            )

            httpRequest.Header.Set(
                "User-Agent",
                "WeselAja-Go/1.0",
            )

            httpRequest.Header.Set(
                "Accept-Encoding",
                "identity",
            )

            httpRequest.Header.Set(
                "Connection",
                "close",
            )

            httpRequest.Header.Set(
                "Xenith-Api-Key",
                client.config.APIKey,
            )

            httpRequest.Header.Set(
                "Xenith-Request-Timestamp",
                timestamp,
            )

            httpRequest.Header.Set(
                "Xenith-Request-Signature",
                signature,
            )

            response,
                err :=
                client.httpClient.Do(
                    httpRequest,
                )

            if err != nil {
                return nil,
                    fmt.Errorf(
                        "unable to connect to WeselAja: %w",
                        err,
                    )
            }

            defer response.Body.Close()

            responseBody,
                err :=
                io.ReadAll(
                    response.Body,
                )

            if err != nil {
                return nil,
                    fmt.Errorf(
                        "failed to read WeselAja response: %w",
                        err,
                    )
            }

            if len(
                bytes.TrimSpace(
                    responseBody,
                ),
            ) == 0 {
                return nil,
                    &APIError{
                        StatusCode:
                            response.StatusCode,

                        Message:
                            "empty response from WeselAja",

                        ResponseBody:
                            "",
                    }
            }

            var decoded any

            if err :=
                json.Unmarshal(
                    responseBody,
                    &decoded,
                ); err != nil {
                return nil,
                    &APIError{
                        StatusCode:
                            response.StatusCode,

                        Message:
                            fmt.Sprintf(
                                "invalid JSON response from WeselAja: %v",
                                err,
                            ),

                        ResponseBody:
                            string(
                                responseBody,
                            ),
                    }
            }

            if response.StatusCode < 200 ||
                response.StatusCode >= 300 {
                return nil,
                    &APIError{
                        StatusCode:
                            response.StatusCode,

                        Message:
                            extractAPIError(
                                decoded,
                                string(
                                    responseBody,
                                ),
                            ),

                        ResponseBody:
                            string(
                                responseBody,
                            ),
                    }
            }

            result,
                ok :=
                decoded.(map[string]any)

            if !ok {
                return nil,
                    fmt.Errorf(
                        "unexpected response format from WeselAja",
                    )
            }

            return result, nil
        }

        func validatePaymentRequest(
            payment CreatePaymentRequest,
        ) error {
            if payment.Amount <= 0 {
                return fmt.Errorf(
                    "payment amount must be greater than zero",
                )
            }

            if strings.TrimSpace(
                payment.CustomerName,
            ) == "" {
                return fmt.Errorf(
                    "customer name is required",
                )
            }

            if strings.TrimSpace(
                payment.Bank,
            ) == "" {
                return fmt.Errorf(
                    "bank is required; supported banks: BRI, Mandiri, BNI, Permata, CIMB, BCA",
                )
            }

            return nil
        }

        func validateReference(
            value string,
        ) error {
            value =
                strings.TrimSpace(
                    value,
                )

            if value == "" {
                return fmt.Errorf(
                    "reference value is required",
                )
            }

            for _, character :=
                range value {
                if (character >= 'a' &&
                    character <= 'z') ||
                    (character >= 'A' &&
                        character <= 'Z') ||
                    (character >= '0' &&
                        character <= '9') ||
                    character == '-' ||
                    character == '_' {
                    continue
                }

                return fmt.Errorf(
                    "reference values may only contain letters, numbers, '-' and '_'",
                )
            }

            return nil
        }

        func generateReference(
            customerName string,
        ) string {
            var builder strings.Builder

            for _, character :=
                range strings.ToUpper(
                    customerName,
                ) {
                if (character >= 'A' &&
                    character <= 'Z') ||
                    (character >= '0' &&
                        character <= '9') ||
                    character == '-' ||
                    character == '_' {
                    builder.WriteRune(
                        character,
                    )
                }
            }

            normalized :=
                builder.String()

            if normalized == "" {
                normalized =
                    "CUSTOMER"
            }

            if len(normalized) > 20 {
                normalized =
                    normalized[:20]
            }

            return fmt.Sprintf(
                "WES-%s-%s",
                normalized,
                time.Now().
                    UTC().
                    Format(
                        "20060102150405",
                    ),
            )
        }

        func generateIdempotencyKey() (
            string,
            error,
        ) {
            bytesBuffer :=
                make(
                    []byte,
                    16,
                )

            if _, err :=
                rand.Read(
                    bytesBuffer,
                ); err != nil {
                return "",
                    err
            }

            return "go-" +
                hex.EncodeToString(
                    bytesBuffer,
                ),
                nil
        }

        func extractAPIError(
            payload any,
            fallback string,
        ) string {
            object,
                ok :=
                payload.(map[string]any)

            if !ok {
                return fallback
            }

            for _, key :=
                range []string{
                    "message",
                    "error",
                    "detail",
                    "code",
                } {
                value,
                    exists :=
                    object[key]

                if !exists {
                    continue
                }

                message,
                    isString :=
                    value.(string)

                if isString &&
                    strings.TrimSpace(
                        message,
                    ) != "" {
                    return message
                }
            }

            return fallback
        }

        """;

    private const string
        MainTemplate =
        """

        package main

        import (
            "context"
            "encoding/json"
            "fmt"
            "log"
            "os"
            "strconv"
        )

        func main() {
            client,
                err :=
                NewClientFromEnvironment()

            if err != nil {
                log.Fatal(err)
            }

            if len(os.Args) > 1 &&
                os.Args[1] == "payins" {
                status :=
                    ""

                if len(os.Args) > 2 {
                    status =
                        os.Args[2]
                }

                var limit *int

                if len(os.Args) > 3 {
                    parsed,
                        parseErr :=
                        strconv.Atoi(
                            os.Args[3],
                        )

                    if parseErr != nil {
                        log.Fatal(parseErr)
                    }

                    limit =
                        &parsed
                }

                result,
                    err :=
                    client.GetPayins(
                        context.Background(),
                        status,
                        limit,
                    )

                if err != nil {
                    log.Fatal(err)
                }

                encoded,
                    err :=
                    json.MarshalIndent(
                        result,
                        "",
                        "  ",
                    )

                if err != nil {
                    log.Fatal(err)
                }

                fmt.Println(
                    string(
                        encoded,
                    ),
                )

                return
            }

            payment,
                err :=
                client.CreatePayment(
                    context.Background(),
                    CreatePaymentRequest{
                        Amount:
                            10000,

                        CustomerName:
                            "Andi",

                        Bank:
                            "BRI",

                        Description:
                            "WeselAja sandbox example",
                    },
                )

            if err != nil {
                log.Fatal(err)
            }

            fmt.Println(
                "Payment created successfully.",
            )

            fmt.Println(
                "Status   :",
                payment.Status,
            )

            fmt.Println(
                "Reference:",
                payment.ReferenceCode,
            )

            fmt.Println(
                "Channel  :",
                payment.PaymentChannel,
            )

            fmt.Println(
                "Payment  :",
                payment.PaymentCode,
            )
        }

        """;

    private const string
        RedirectServerTemplate =
        """

        package main

        import (
            "bufio"
            "encoding/json"
            "log"
            "net/http"
            "os"
            "strconv"
            "strings"
        )

        func LoadDotEnv(fileName string) {
            file, err := os.Open(fileName)
            if err != nil {
                return
            }
            defer file.Close()

            scanner := bufio.NewScanner(file)
            for scanner.Scan() {
                line := strings.TrimSpace(scanner.Text())
                if line == "" || strings.HasPrefix(line, "#") {
                    continue
                }

                key, value, ok := strings.Cut(line, "=")
                if !ok || strings.TrimSpace(key) == "" {
                    continue
                }
                key = strings.TrimSpace(key)
                value = strings.Trim(strings.TrimSpace(value), "\"'")
                if os.Getenv(key) == "" {
                    _ = os.Setenv(key, value)
                }
            }
        }

        func main() {
            LoadDotEnv(".env")

            port :=
                8080

            if value :=
                os.Getenv(
                    "WESELAJA_REDIRECT_PORT",
                );
                value != "" {
                if parsed,
                    err :=
                    strconv.Atoi(
                        value,
                    );
                    err == nil &&
                    parsed > 0 {
                    port =
                        parsed
                }
            }

            mux :=
                http.NewServeMux()

            mux.HandleFunc(
                "/payment/weselaja/return",
                func(
                    writer http.ResponseWriter,
                    request *http.Request,
                ) {
                    if request.Method !=
                        http.MethodGet {
                        writer.Header().Set(
                            "Content-Type",
                            "application/json",
                        )

                        writer.WriteHeader(
                            http.StatusMethodNotAllowed,
                        )

                        _ =
                            json.NewEncoder(
                                writer,
                            ).Encode(
                                map[string]any{
                                    "success":
                                        false,

                                    "error":
                                        "Method not allowed",
                                },
                            )

                        return
                    }

                    parameters :=
                        map[string]string{}

                    for key,
                        values :=
                        range request.URL.Query() {
                        if len(values) > 0 {
                            parameters[key] =
                                values[0]
                        }
                    }

                    response :=
                        map[string]any{
                            "success":
                                true,

                            "path":
                                request.URL.Path,

                            "parameters":
                                parameters,
                        }

                    writer.Header().Set(
                        "Content-Type",
                        "application/json",
                    )

                    _ =
                        json.NewEncoder(
                            writer,
                        ).Encode(
                            response,
                        )
                },
            )

            log.Printf(
                "WeselAja redirect handler listening on http://0.0.0.0:%d/payment/weselaja/return",
                port,
            )

            log.Fatal(
                http.ListenAndServe(
                    ":"+
                        strconv.Itoa(
                            port,
                        ),
                    mux,
                ),
            )
        }

        """;

    private const string
        CallbackServerTemplate =
        """

        package main

        import (
            "bufio"
            "crypto/hmac"
            "crypto/sha256"
            "encoding/base64"
            "encoding/json"
            "io"
            "log"
            "net/http"
            "os"
            "strconv"
            "strings"
        )

        func LoadDotEnv(fileName string) {
            file, err := os.Open(fileName)
            if err != nil {
                return
            }
            defer file.Close()

            scanner := bufio.NewScanner(file)
            for scanner.Scan() {
                line := strings.TrimSpace(scanner.Text())
                if line == "" || strings.HasPrefix(line, "#") {
                    continue
                }

                key, value, ok := strings.Cut(line, "=")
                if !ok || strings.TrimSpace(key) == "" {
                    continue
                }
                key = strings.TrimSpace(key)
                value = strings.Trim(strings.TrimSpace(value), "\"'")
                if os.Getenv(key) == "" {
                    _ = os.Setenv(key, value)
                }
            }
        }

        func VerifyCallbackSignature(
            method string,
            uri string,
            rawBody string,
            timestamp string,
            signature string,
            secret string,
        ) bool {
            if signature == "" {
                return false
            }
            payload := method + "\n" + uri + "\n" + rawBody + "\n" + timestamp
            mac := hmac.New(sha256.New, []byte(secret))
            _, _ = mac.Write([]byte(payload))
            expected := base64.StdEncoding.EncodeToString(mac.Sum(nil))
            return hmac.Equal(
                []byte(expected),
                []byte(strings.TrimSpace(signature)),
            )
        }

        func main() {
            LoadDotEnv(".env")

            secret :=
                os.Getenv(
                    "WEB_SIGNATURE_SECRET",
                )

            if secret == "" {
                log.Fatal(
                    "WEB_SIGNATURE_SECRET is required",
                )
            }

            port :=
                8081

            if value :=
                os.Getenv(
                    "WESELAJA_CALLBACK_PORT",
                );
                value != "" {
                if parsed,
                    err :=
                    strconv.Atoi(
                        value,
                    );
                    err == nil &&
                    parsed > 0 {
                    port =
                        parsed
                }
            }

            mux :=
                http.NewServeMux()

            mux.HandleFunc(
                "/payment/weselaja/callback",
                func(
                    writer http.ResponseWriter,
                    request *http.Request,
                ) {
                    writer.Header().Set(
                        "Content-Type",
                        "application/json",
                    )

                    if request.Method !=
                        http.MethodPost {
                        writer.WriteHeader(
                            http.StatusMethodNotAllowed,
                        )

                        _ =
                            json.NewEncoder(
                                writer,
                            ).Encode(
                                map[string]any{
                                    "success":
                                        false,

                                    "error":
                                        "Method not allowed",
                                },
                            )

                        return
                    }

                    timestamp :=
                        request.Header.Get(
                            "x-middleware-timestamp",
                        )

                    signature :=
                        request.Header.Get(
                            "x-middleware-signature",
                        )

                    if timestamp == "" {
                        writer.WriteHeader(
                            http.StatusBadRequest,
                        )

                        _ =
                            json.NewEncoder(
                                writer,
                            ).Encode(
                                map[string]any{
                                    "success":
                                        false,

                                    "error":
                                        "Missing x-middleware-timestamp",
                                },
                            )

                        return
                    }

                    if signature == "" {
                        writer.WriteHeader(
                            http.StatusBadRequest,
                        )

                        _ =
                            json.NewEncoder(
                                writer,
                            ).Encode(
                                map[string]any{
                                    "success":
                                        false,

                                    "error":
                                        "Missing x-middleware-signature",
                                },
                            )

                        return
                    }

                    rawBodyBytes,
                        err :=
                        io.ReadAll(
                            request.Body,
                        )

                    if err != nil {
                        writer.WriteHeader(
                            http.StatusBadRequest,
                        )

                        _ =
                            json.NewEncoder(
                                writer,
                            ).Encode(
                                map[string]any{
                                    "success":
                                        false,

                                    "error":
                                        "Unable to read request body",
                                },
                            )

                        return
                    }

                    rawBody :=
                        string(
                            rawBodyBytes,
                        )

                    uri :=
                        request.URL.Path

                    if request.URL.RawQuery != "" {
                        uri +=
                            "?" +
                            request.URL.RawQuery
                    }

                    valid :=
                        VerifyCallbackSignature(
                            http.MethodPost,
                            uri,
                            rawBody,
                            timestamp,
                            signature,
                            secret,
                        )

                    if !valid {
                        writer.WriteHeader(
                            http.StatusUnauthorized,
                        )

                        _ =
                            json.NewEncoder(
                                writer,
                            ).Encode(
                                map[string]any{
                                    "success":
                                        false,

                                    "verified":
                                        false,

                                    "error":
                                        "Invalid callback signature",
                                },
                            )

                        return
                    }

                    var body any

                    if err :=
                        json.Unmarshal(
                            rawBodyBytes,
                            &body,
                        ); err != nil {
                        body =
                            rawBody
                    }

                    _ =
                        json.NewEncoder(
                            writer,
                        ).Encode(
                            map[string]any{
                                "success":
                                    true,

                                "verified":
                                    true,

                                "body":
                                    body,
                            },
                        )
                },
            )

            log.Printf(
                "WeselAja callback handler listening on http://0.0.0.0:%d/payment/weselaja/callback",
                port,
            )

            log.Fatal(
                http.ListenAndServe(
                    ":"+
                        strconv.Itoa(
                            port,
                        ),
                    mux,
                ),
            )
        }

        """;

    private const string
        SelfTestTemplate =
        """

        package main

        import (
            "testing"
        )

        func TestBankMappings(
            t *testing.T,
        ) {
            tests :=
                map[string]string{
                    "BRI":
                        "BRI.VA",

                    "Bank BRI":
                        "BRI.VA",

                    "Mandiri":
                        "MDR.VA",

                    "Bank Mandiri":
                        "MDR.VA",

                    "BNI":
                        "BNI.VA",

                    "Permata":
                        "PTB.VA",

                    "CIMB":
                        "CIMB.VA",

                    "CIMB Niaga":
                        "CIMB.VA",

                    "BCA":
                        "BCA.VA",

                    "Bank BCA":
                        "BCA.VA",
                }

            for bank,
                expected :=
                range tests {
                actual,
                    err :=
                    resolveBankChannel(
                        bank,
                    )

                if err != nil {
                    t.Fatalf(
                        "bank %q returned error: %v",
                        bank,
                        err,
                    )
                }

                if actual != expected {
                    t.Fatalf(
                        "bank %q: expected %q, got %q",
                        bank,
                        expected,
                        actual,
                    )
                }
            }
        }

        func TestUnsupportedBank(
            t *testing.T,
        ) {
            if _, err :=
                resolveBankChannel(
                    "Bank Tidak Ada",
                ); err == nil {
                t.Fatal(
                    "unsupported bank should return an error",
                )
            }
        }

        func TestCallbackSignature(
            t *testing.T,
        ) {
            method :=
                "POST"

            uri :=
                "/payment/weselaja/callback"

            body :=
                "{\"hello\":\"world\"}"

            timestamp :=
                "2026-01-01T00:00:00.000Z"

            secret :=
                "test-secret"

            signature :=
                GenerateCallbackSignature(
                    method,
                    uri,
                    body,
                    timestamp,
                    secret,
                )

            if !VerifyCallbackSignature(
                method,
                uri,
                body,
                timestamp,
                signature,
                secret,
            ) {
                t.Fatal(
                    "callback signature verification failed",
                )
            }

            if VerifyCallbackSignature(
                method,
                uri,
                body,
                timestamp,
                signature+"x",
                secret,
            ) {
                t.Fatal(
                    "invalid callback signature should be rejected",
                )
            }
        }

        """;

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

        WESELAJA_CALLBACK_PORT=8081
        WESELAJA_REDIRECT_PORT=8080

        WEB_SIGNATURE_SECRET=

        """;

    private const string
        GitIgnoreTemplate =
        """

        .env

        """;
}