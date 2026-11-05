using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class CSharpWeselAjaGenerator
{
    public GenerationResult Generate(
        string projectName,
        bool includeExample,
        bool includeTests,
        bool overwrite)
    {
        var safeProjectName =
            SanitizeProjectName(projectName);

        var outputRoot =
            Environment.GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");

        outputRoot =
            string.IsNullOrWhiteSpace(outputRoot)
                ? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "generated")
                : outputRoot.Trim();

        outputRoot =
            Path.GetFullPath(outputRoot);

        var csharpRoot =
            Path.GetFullPath(
                Path.Combine(
                    outputRoot,
                    "csharp"));

        var projectDirectory =
            Path.GetFullPath(
                Path.Combine(
                    csharpRoot,
                    safeProjectName));

        EnsureInsideDirectory(
            projectDirectory,
            csharpRoot);

        if (Directory.Exists(projectDirectory))
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

        foreach (var file in files)
        {
            var filePath =
                Path.Combine(
                    projectDirectory,
                    file.Key);

            var directory =
                Path.GetDirectoryName(
                    filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                filePath,
                file.Value,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

            generatedFiles.Add(
                new GeneratedFile
                {
                    Path =
                        Path.GetRelativePath(
                            projectDirectory,
                            filePath),

                    Bytes =
                        new FileInfo(filePath).Length
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

        files[$"{projectName}.csproj"] =
            CsProjTemplate;

        files["Configuration/WeselAjaConfig.cs"] =
            ConfigTemplate;

        files["Configuration/EnvLoader.cs"] =
            EnvLoaderTemplate;

        files["Models/CreatePaymentRequest.cs"] =
            CreatePaymentRequestTemplate;

        files["Models/CreatePaymentResponse.cs"] =
            CreatePaymentResponseTemplate;

        files["Models/BankChannels.cs"] =
            BankChannelsTemplate;

        files["Security/SignatureGenerator.cs"] =
            SignatureTemplate;

        files["Exceptions/WeselAjaExceptions.cs"] =
            ExceptionsTemplate;

        files["WeselAjaClient.cs"] =
            ClientTemplate;

        files["CallbackServer.cs"] =
            CallbackServerTemplate;

        files["RedirectServer.cs"] =
            RedirectServerTemplate;

        files[".env.example"] =
            EnvExampleTemplate;

        files[".gitignore"] =
            GitIgnoreTemplate;

        files["README.md"] =
            BuildReadmeTemplate(projectName);

        if (includeExample)
        {
            files["Program.cs"] =
                BuildProgramTemplate(includeTests);
        }

        if (includeTests)
        {
            files["SelfTest.cs"] =
                SelfTestTemplate;
        }

        return files;
    }

    private static string SanitizeProjectName(
        string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return "weselaja-csharp";
        }

        var builder =
            new StringBuilder();

        var previousWasSeparator =
            false;

        foreach (
            var character
            in projectName.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(
                    char.ToLowerInvariant(character));

                previousWasSeparator =
                    false;
            }
            else
            {
                if (!previousWasSeparator)
                {
                    builder.Append('-');

                    previousWasSeparator =
                        true;
                }
            }
        }

        var result =
            builder
                .ToString()
                .Trim('-');

        if (string.IsNullOrWhiteSpace(result))
        {
            return "weselaja-csharp";
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
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Generated project path is outside the configured generator directory.");
        }

        if (Path.IsPathRooted(relative))
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

        Standalone C# integration for the WeselAja payment provider.

        This project was generated by the WeselAja Code Generator.

        ## Features

        - Create virtual-account payments
        - Human-readable bank selection
        - Bank-to-channel mapping
        - HMAC-SHA256 request signature
        - Idempotency key
        - Structured response model
        - Create payment
        - Get payins
        - Callback handling
        - Callback signature verification
        - Redirect handling
        - Environment-based configuration
        - No MCP dependency
        - No credentials embedded in source code

        ## Supported banks

        | Bank | WeselAja channel |
        |---|---|
        | BRI | `BRI.VA` |
        | Mandiri | `MDR.VA` |
        | BNI | `BNI.VA` |
        | Permata | `PTB.VA` |
        | CIMB | `CIMB.VA` |
        | BCA | `BCA.VA` |

        ## Requirements

        - .NET 10 SDK

        ## Configuration

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
        WESELAJA_REDIRECT_PORT=8080
        WEB_SIGNATURE_SECRET=
        ```

        `WEB_SIGNATURE_SECRET` is used to verify callback signatures.

        Transaction values such as amount, customer name, bank,
        description, and reference code are supplied by application
        code and are not stored in `.env`.

        ## Run

        ```bash
        dotnet restore
        dotnet run
        ```

        The default example creates a sandbox payment.

        ## Get payins

        ```bash
        dotnet run -- payins
        ```

        Optional status and limit:

        ```bash
        dotnet run -- payins SUCCESS 50
        ```

        Supported statuses:

        ```text
        PENDING
        SUCCESS
        EXPIRED
        FAILED
        ```

        This uses:

        ```text
        GET /v1/payins
        ```

        with the same HMAC-SHA256 request signing mechanism.

        ## Callback server

        Start:

        ```bash
        dotnet run -- callback
        ```

        Default endpoint:

        ```text
        http://0.0.0.0:8081/payment/weselaja/callback
        ```

        The callback server verifies:

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

        HMAC-SHA256 is calculated using `WEB_SIGNATURE_SECRET`
        and encoded as Base64.

        Set `WESELAJA_CALLBACK_PORT` when a different local port is required.

        ## Redirect server

        Start:

        ```bash
        dotnet run -- redirect
        ```

        Default endpoint:

        ```text
        http://0.0.0.0:8080/payment/weselaja/return
        ```

        Set `WESELAJA_REDIRECT_PORT` when a different local port is required.

        ## Built-in tests

        ```bash
        dotnet run -- test
        ```

        These tests validate:

        - bank mappings
        - invalid bank rejection
        - callback signature generation
        - callback signature verification

        ## Usage

        ```csharp
        using WeselAja.Integration.Configuration;
        using WeselAja.Integration.Models;

        var config =
            WeselAjaConfig.FromEnvironment();

        using var client =
            new WeselAjaClient(config);

        var response =
            await client.CreatePaymentAsync(
                new CreatePaymentRequest
                {
                    InitiatedAmount = 100000,
                    CustomerName = "Andi",
                    Bank = "BCA",
                    Description = "Order payment"
                });

        Console.WriteLine(response.Status);
        Console.WriteLine(response.PaymentCode);
        ```

        ## Bank mapping

        ```text
        BRI       -> BRI.VA
        Mandiri   -> MDR.VA
        BNI       -> BNI.VA
        Permata   -> PTB.VA
        CIMB      -> CIMB.VA
        BCA       -> BCA.VA
        ```

        ## Authentication

        Payment requests use:

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

        ## Scope

        This generated project supports:

        - payment creation
        - payin lookup
        - callback receiving
        - callback signature verification
        - redirect handling

        """;
    }

    private static string BuildProgramTemplate(
        bool includeTests)
    {
        var testBlock =
            includeTests
                ? """
                  if (
                      args.Length > 0
                      &&
                      string.Equals(
                          args[0],
                          "test",
                          StringComparison.OrdinalIgnoreCase))
                  {
                      SelfTest.Run();
                      return;
                  }
                  """
                : string.Empty;

        return $$"""
        using WeselAja.Integration.Configuration;
        using WeselAja.Integration.Models;

        namespace WeselAja.Integration;

        internal static class Program
        {
            private static async Task Main(
                string[] args)
            {
        {{testBlock.Replace("\n", "\n        ")}}
                if (
                    args.Length > 0
                    &&
                    string.Equals(
                        args[0],
                        "callback",
                        StringComparison.OrdinalIgnoreCase))
                {
                    await CallbackServer.RunAsync();
                    return;
                }

                if (
                    args.Length > 0
                    &&
                    string.Equals(
                        args[0],
                        "redirect",
                        StringComparison.OrdinalIgnoreCase))
                {
                    await RedirectServer.RunAsync();
                    return;
                }

                var config =
                    WeselAjaConfig.FromEnvironment();

                using var client =
                    new WeselAjaClient(
                        config);

                if (
                    args.Length > 0
                    &&
                    string.Equals(
                        args[0],
                        "payins",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string? status =
                        args.Length > 1
                            ? args[1]
                            : null;

                    int? limit = null;

                    if (
                        args.Length > 2
                        &&
                        int.TryParse(
                            args[2],
                            out var parsedLimit))
                    {
                        limit = parsedLimit;
                    }

                    var result =
                        await client.GetPayinsAsync(
                            status,
                            limit);

                    Console.WriteLine(
                        result.GetRawText());

                    return;
                }

                var response =
                    await client.CreatePaymentAsync(
                        new CreatePaymentRequest
                        {
                            InitiatedAmount =
                                10_000,

                            CustomerName =
                                "Andi",

                            Bank =
                                "BCA",

                            Description =
                                "WeselAja sandbox example"
                        });

                Console.WriteLine(
                    "Payment created successfully.");

                Console.WriteLine(
                    $"Status   : {response.Status}");

                Console.WriteLine(
                    $"Reference: {response.ReferenceCode}");

                Console.WriteLine(
                    $"Channel  : {response.PaymentChannel}");

                Console.WriteLine(
                    $"Payment  : {response.PaymentCode}");
            }
        }
        """;
    }

    private const string
        CsProjTemplate =
        """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        </Project>
        """;

    private const string
        ConfigTemplate =
        """
        using WeselAja.Integration.Exceptions;

        namespace WeselAja.Integration.Configuration;

        public sealed class WeselAjaConfig
        {
            public string ApiUrl { get; init; } =
                "https://sandbox.checkout.weselaja.id";

            public string ApiKey { get; init; } =
                string.Empty;

            public string SecretKey { get; init; } =
                string.Empty;

            public string CustomerReference { get; init; } =
                string.Empty;

            public string CallbackUrl { get; init; } =
                string.Empty;

            public string RedirectUrl { get; init; } =
                string.Empty;

            public string Currency { get; init; } =
                "IDR";

            public string PaymentMethod { get; init; } =
                "VIRTUAL_ACCOUNT";

            public static WeselAjaConfig FromEnvironment()
            {
                EnvLoader.Load();

                var config =
                    new WeselAjaConfig
                    {
                        ApiUrl =
                            Get(
                                "WESELAJA_API_URL",
                                "https://sandbox.checkout.weselaja.id"),

                        ApiKey =
                            Get(
                                "WESELAJA_API_KEY"),

                        SecretKey =
                            Get(
                                "WESELAJA_SECRET_KEY"),

                        CustomerReference =
                            Get(
                                "WESELAJA_CUSTOMER_REFERENCE"),

                        CallbackUrl =
                            Get(
                                "WESELAJA_CALLBACK_URL"),

                        RedirectUrl =
                            Get(
                                "WESELAJA_REDIRECT_URL"),

                        Currency =
                            Get(
                                "WESELAJA_CURRENCY",
                                "IDR"),

                        PaymentMethod =
                            Get(
                                "WESELAJA_PAYMENT_METHOD",
                                "VIRTUAL_ACCOUNT")
                    };

                config.Validate();

                return config;
            }

            public void Validate()
            {
                var required =
                    new Dictionary<
                        string,
                        string>
                    {
                        ["WESELAJA_API_URL"] =
                            ApiUrl,

                        ["WESELAJA_API_KEY"] =
                            ApiKey,

                        ["WESELAJA_SECRET_KEY"] =
                            SecretKey,

                        ["WESELAJA_CUSTOMER_REFERENCE"] =
                            CustomerReference,

                        ["WESELAJA_CALLBACK_URL"] =
                            CallbackUrl,

                        ["WESELAJA_REDIRECT_URL"] =
                            RedirectUrl,

                        ["WESELAJA_CURRENCY"] =
                            Currency,

                        ["WESELAJA_PAYMENT_METHOD"] =
                            PaymentMethod
                    };

                var missing =
                    required
                        .Where(
                            pair =>
                                string.IsNullOrWhiteSpace(
                                    pair.Value))
                        .Select(
                            pair =>
                                pair.Key)
                        .ToArray();

                if (missing.Length > 0)
                {
                    throw new WeselAjaConfigurationException(
                        "Missing configuration: "
                        + string.Join(
                            ", ",
                            missing));
                }

                if (!IsHttpUrl(ApiUrl))
                {
                    throw new WeselAjaConfigurationException(
                        "WESELAJA_API_URL must be a valid HTTP or HTTPS URL.");
                }

                if (!IsHttpUrl(CallbackUrl))
                {
                    throw new WeselAjaConfigurationException(
                        "WESELAJA_CALLBACK_URL must be a valid HTTP or HTTPS URL.");
                }

                if (!IsHttpUrl(RedirectUrl))
                {
                    throw new WeselAjaConfigurationException(
                        "WESELAJA_REDIRECT_URL must be a valid HTTP or HTTPS URL.");
                }
            }

            private static string Get(
                string key,
                string fallback = "")
            {
                var value =
                    Environment.GetEnvironmentVariable(
                        key);

                return string.IsNullOrWhiteSpace(value)
                    ? fallback
                    : value.Trim();
            }

            private static bool IsHttpUrl(
                string value)
            {
                return
                    Uri.TryCreate(
                        value,
                        UriKind.Absolute,
                        out var uri)
                    &&
                    (
                        uri.Scheme ==
                            Uri.UriSchemeHttp
                        ||
                        uri.Scheme ==
                            Uri.UriSchemeHttps
                    );
            }
        }
        """;

    private const string
        EnvLoaderTemplate =
        """
        namespace WeselAja.Integration.Configuration;

        public static class EnvLoader
        {
            public static void Load(
                string fileName = ".env")
            {
                var path =
                    FindFile(fileName);

                if (path is null)
                {
                    return;
                }

                foreach (
                    var rawLine
                    in File.ReadAllLines(path))
                {
                    var line =
                        rawLine
                            .Trim()
                            .TrimStart('\uFEFF');

                    if (
                        string.IsNullOrWhiteSpace(line)
                        ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    var separator =
                        line.IndexOf('=');

                    if (separator <= 0)
                    {
                        continue;
                    }

                    var key =
                        line[..separator]
                            .Trim();

                    var value =
                        line[(separator + 1)..]
                            .Trim();

                    value =
                        RemoveQuotes(value);

                    if (
                        string.IsNullOrWhiteSpace(
                            Environment.GetEnvironmentVariable(
                                key)))
                    {
                        Environment.SetEnvironmentVariable(
                            key,
                            value);
                    }
                }
            }

            private static string? FindFile(
                string fileName)
            {
                var current =
                    new DirectoryInfo(
                        Directory.GetCurrentDirectory());

                while (current is not null)
                {
                    var candidate =
                        Path.Combine(
                            current.FullName,
                            fileName);

                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }

                    current =
                        current.Parent;
                }

                return null;
            }

            private static string RemoveQuotes(
                string value)
            {
                if (
                    value.Length >= 2
                    &&
                    (
                        (
                            value.StartsWith("\"")
                            &&
                            value.EndsWith("\"")
                        )
                        ||
                        (
                            value.StartsWith("'")
                            &&
                            value.EndsWith("'")
                        )
                    ))
                {
                    return value[1..^1];
                }

                return value;
            }
        }
        """;

    private const string
        CreatePaymentRequestTemplate =
        """
        namespace WeselAja.Integration.Models;

        public sealed class CreatePaymentRequest
        {
            public int InitiatedAmount { get; set; }

            public string CustomerName { get; set; } =
                string.Empty;

            public string Bank { get; set; } =
                string.Empty;

            public string? CustomerPhoneNumber { get; set; }

            public string? Description { get; set; }

            public string? ReferenceCode { get; set; }
        }
        """;

    private const string
        CreatePaymentResponseTemplate =
        """
        using System.Text.Json.Serialization;

        namespace WeselAja.Integration.Models;

        public sealed class CreatePaymentResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("initiatedAmount")]
            public decimal? InitiatedAmount { get; set; }

            [JsonPropertyName("paymentAmount")]
            public decimal? PaymentAmount { get; set; }

            [JsonPropertyName("feeAmount")]
            public decimal? FeeAmount { get; set; }

            [JsonPropertyName("currency")]
            public string? Currency { get; set; }

            [JsonPropertyName("paymentMethod")]
            public string? PaymentMethod { get; set; }

            [JsonPropertyName("paymentChannel")]
            public string? PaymentChannel { get; set; }

            [JsonPropertyName("paymentCode")]
            public string? PaymentCode { get; set; }

            [JsonPropertyName("paymentCodeType")]
            public string? PaymentCodeType { get; set; }

            [JsonPropertyName("referenceCode")]
            public string? ReferenceCode { get; set; }

            [JsonPropertyName("customerReference")]
            public string? CustomerReference { get; set; }

            [JsonPropertyName("customerName")]
            public string? CustomerName { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("createdTime")]
            public string? CreatedTime { get; set; }

            [JsonPropertyName("updatedTime")]
            public string? UpdatedTime { get; set; }

            [JsonPropertyName("expirationTime")]
            public string? ExpirationTime { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("callbackUrl")]
            public string? CallbackUrl { get; set; }

            [JsonPropertyName("redirectUrl")]
            public string? RedirectUrl { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            public bool IsSuccess =>
                !string.IsNullOrWhiteSpace(Id)
                &&
                !string.Equals(
                    Status,
                    "FAILED",
                    StringComparison.OrdinalIgnoreCase);
        }
        """;

    private const string
        BankChannelsTemplate =
        """
        namespace WeselAja.Integration.Models;

        public static class BankChannels
        {
            private static readonly Dictionary<
                string,
                string>
                Channels =
                    new(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["BRI"] =
                            "BRI.VA",

                        ["BANKBRI"] =
                            "BRI.VA",

                        ["MANDIRI"] =
                            "MDR.VA",

                        ["BANKMANDIRI"] =
                            "MDR.VA",

                        ["MDR"] =
                            "MDR.VA",

                        ["BNI"] =
                            "BNI.VA",

                        ["BANKBNI"] =
                            "BNI.VA",

                        ["PERMATA"] =
                            "PTB.VA",

                        ["BANKPERMATA"] =
                            "PTB.VA",

                        ["PTB"] =
                            "PTB.VA",

                        ["CIMB"] =
                            "CIMB.VA",

                        ["CIMBNIAGA"] =
                            "CIMB.VA",

                        ["BANKCIMB"] =
                            "CIMB.VA",

                        ["BCA"] =
                            "BCA.VA",

                        ["BANKBCA"] =
                            "BCA.VA"
                    };

            public static string Resolve(
                string bank)
            {
                if (
                    string.IsNullOrWhiteSpace(
                        bank))
                {
                    throw new ArgumentException(
                        "Bank is required.");
                }

                var key =
                    Normalize(bank);

                if (
                    Channels.TryGetValue(
                        key,
                        out var channel))
                {
                    return channel;
                }

                throw new ArgumentException(
                    $"Unsupported bank '{bank}'. " +
                    "Supported banks: " +
                    "BRI, Mandiri, BNI, Permata, CIMB, BCA.");
            }

            public static string Normalize(
                string bank)
            {
                return bank
                    .Trim()
                    .ToUpperInvariant()
                    .Replace(
                        " ",
                        string.Empty)
                    .Replace(
                        "-",
                        string.Empty)
                    .Replace(
                        "_",
                        string.Empty);
            }

            public static IReadOnlyDictionary<
                string,
                string>
                GetSupportedBanks()
            {
                return new Dictionary<
                    string,
                    string>
                {
                    ["BRI"] =
                        "BRI.VA",

                    ["Mandiri"] =
                        "MDR.VA",

                    ["BNI"] =
                        "BNI.VA",

                    ["Permata"] =
                        "PTB.VA",

                    ["CIMB"] =
                        "CIMB.VA",

                    ["BCA"] =
                        "BCA.VA"
                };
            }
        }
        """;

    private const string
        SignatureTemplate =
        """
        using System.Security.Cryptography;
        using System.Text;

        namespace WeselAja.Integration.Security;

        public static class SignatureGenerator
        {
            public static string Generate(
                string method,
                string uri,
                string timestamp,
                string body,
                string secretKey)
            {
                var payload =
                    $"{method}\n" +
                    $"{uri}\n" +
                    $"{timestamp}\n" +
                    body;

                using var hmac =
                    new HMACSHA256(
                        Encoding.UTF8.GetBytes(
                            secretKey));

                var hash =
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            payload));

                return Convert.ToBase64String(
                    hash);
            }

            public static string GenerateCallbackSignature(
                string method,
                string uri,
                string rawBody,
                string timestamp,
                string secretKey)
            {
                var payload =
                    $"{method}\n" +
                    $"{uri}\n" +
                    $"{rawBody}\n" +
                    $"{timestamp}";

                using var hmac =
                    new HMACSHA256(
                        Encoding.UTF8.GetBytes(
                            secretKey));

                var hash =
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            payload));

                return Convert.ToBase64String(
                    hash);
            }

            public static bool VerifyCallbackSignature(
                string method,
                string uri,
                string rawBody,
                string timestamp,
                string signature,
                string secretKey)
            {
                if (
                    string.IsNullOrWhiteSpace(
                        signature))
                {
                    return false;
                }

                var expected =
                    GenerateCallbackSignature(
                        method,
                        uri,
                        rawBody,
                        timestamp,
                        secretKey);

                var expectedBytes =
                    Encoding.UTF8.GetBytes(
                        expected);

                var providedBytes =
                    Encoding.UTF8.GetBytes(
                        signature.Trim());

                return
                    CryptographicOperations
                        .FixedTimeEquals(
                            expectedBytes,
                            providedBytes);
            }
        }
        """;

    private const string
        ExceptionsTemplate =
        """
        namespace WeselAja.Integration.Exceptions;

        public class WeselAjaException :
            Exception
        {
            public WeselAjaException(
                string message)
                : base(message)
            {
            }

            public WeselAjaException(
                string message,
                Exception innerException)
                : base(
                    message,
                    innerException)
            {
            }
        }

        public sealed class
            WeselAjaConfigurationException :
            WeselAjaException
        {
            public WeselAjaConfigurationException(
                string message)
                : base(message)
            {
            }
        }

        public sealed class
            WeselAjaApiException :
            WeselAjaException
        {
            public int? StatusCode { get; }

            public string? ResponseBody { get; }

            public WeselAjaApiException(
                string message,
                int? statusCode = null,
                string? responseBody = null)
                : base(message)
            {
                StatusCode =
                    statusCode;

                ResponseBody =
                    responseBody;
            }

            public WeselAjaApiException(
                string message,
                Exception innerException)
                : base(
                    message,
                    innerException)
            {
            }
        }
        """;

    private const string
        ClientTemplate =
        """
        using System.Globalization;
        using System.Net;
        using System.Net.Http.Headers;
        using System.Text;
        using System.Text.Encodings.Web;
        using System.Text.Json;
        using System.Text.Json.Serialization;
        using WeselAja.Integration.Configuration;
        using WeselAja.Integration.Exceptions;
        using WeselAja.Integration.Models;
        using WeselAja.Integration.Security;

        namespace WeselAja.Integration;

        public sealed class WeselAjaClient :
            IDisposable
        {
            private const string PaymentUri =
                "/v1/payins";

            private const string UserAgent =
                "Python-urllib/3.13";

            private readonly HttpClient
                _httpClient;

            private readonly WeselAjaConfig
                _config;

            public WeselAjaClient(
                WeselAjaConfig config)
            {
                config.Validate();

                _config =
                    config;

                var handler =
                    new HttpClientHandler
                    {
                        UseProxy = false,

                        AutomaticDecompression =
                            DecompressionMethods.None,

                        MaxConnectionsPerServer =
                            1
                    };

                _httpClient =
                    new HttpClient(handler);
            }

            public static WeselAjaClient
                FromEnvironment()
            {
                return new WeselAjaClient(
                    WeselAjaConfig
                        .FromEnvironment());
            }

            public async Task<
                CreatePaymentResponse>
                CreatePaymentAsync(
                    CreatePaymentRequest request,
                    CancellationToken
                        cancellationToken =
                            default)
            {
                ValidateRequest(
                    request);

                var paymentChannel =
                    BankChannels.Resolve(
                        request.Bank);

                var referenceCode =
                    string.IsNullOrWhiteSpace(
                        request.ReferenceCode)
                        ? GenerateReference(
                            request.CustomerName)
                        : request.ReferenceCode.Trim();

                ValidateReference(
                    referenceCode);

                ValidateReference(
                    _config.CustomerReference);

                var payload =
                    new Dictionary<
                        string,
                        object?>
                    {
                        ["initiatedAmount"] =
                            request.InitiatedAmount,

                        ["currency"] =
                            _config.Currency,

                        ["paymentMethod"] =
                            _config.PaymentMethod,

                        ["paymentChannel"] =
                            paymentChannel,

                        ["referenceCode"] =
                            referenceCode,

                        ["customerReference"] =
                            _config.CustomerReference,

                        ["customerName"] =
                            request.CustomerName.Trim(),

                        ["callbackUrl"] =
                            _config.CallbackUrl,

                        ["redirectUrl"] =
                            _config.RedirectUrl
                    };

                if (
                    !string.IsNullOrWhiteSpace(
                        request.CustomerPhoneNumber))
                {
                    payload[
                        "customerPhoneNumber"] =
                        request.CustomerPhoneNumber.Trim();
                }

                if (
                    !string.IsNullOrWhiteSpace(
                        request.Description))
                {
                    payload[
                        "description"] =
                        request.Description.Trim();
                }

                var jsonOptions =
                    new JsonSerializerOptions
                    {
                        WriteIndented = false,

                        Encoder =
                            JavaScriptEncoder
                                .UnsafeRelaxedJsonEscaping,

                        DefaultIgnoreCondition =
                            JsonIgnoreCondition
                                .WhenWritingNull
                    };

                var body =
                    JsonSerializer.Serialize(
                        payload,
                        jsonOptions);

                var timestamp =
                    DateTimeOffset.UtcNow.ToString(
                        "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                        CultureInfo.InvariantCulture);

                var signature =
                    SignatureGenerator.Generate(
                        "POST",
                        PaymentUri,
                        timestamp,
                        body,
                        _config.SecretKey);

                var idempotencyKey =
                    "csharp-" +
                    Guid.NewGuid().ToString("N");

                var requestUrl =
                    _config.ApiUrl.TrimEnd('/') +
                    PaymentUri;

                using var httpRequest =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        requestUrl)
                    {
                        Version =
                            HttpVersion.Version11,

                        VersionPolicy =
                            HttpVersionPolicy
                                .RequestVersionExact
                    };

                var content =
                    new ByteArrayContent(
                        Encoding.UTF8.GetBytes(
                            body));

                content.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        "application/json");

                httpRequest.Content =
                    content;

                httpRequest.Headers.Accept.Clear();

                httpRequest.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"));

                httpRequest.Headers.UserAgent.Clear();

                httpRequest.Headers.UserAgent.ParseAdd(
                    UserAgent);

                httpRequest.Headers.AcceptEncoding.Clear();

                httpRequest.Headers.AcceptEncoding.Add(
                    new StringWithQualityHeaderValue(
                        "identity"));

                httpRequest.Headers.ConnectionClose =
                    true;

                httpRequest.Headers.ExpectContinue =
                    false;

                httpRequest.Headers.Add(
                    "Xenith-Api-Key",
                    _config.ApiKey);

                httpRequest.Headers.Add(
                    "Xenith-Request-Timestamp",
                    timestamp);

                httpRequest.Headers.Add(
                    "Xenith-Request-Signature",
                    signature);

                httpRequest.Headers.Add(
                    "X-Idempotency-Key",
                    idempotencyKey);

                using var response =
                    await _httpClient.SendAsync(
                        httpRequest,
                        HttpCompletionOption
                            .ResponseContentRead,
                        cancellationToken);

                var responseBody =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                if (
                    string.IsNullOrWhiteSpace(
                        responseBody))
                {
                    throw new WeselAjaApiException(
                        "Empty response from WeselAja.",
                        (int)response.StatusCode,
                        responseBody);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new WeselAjaApiException(
                        ExtractErrorMessage(
                            responseBody,
                            response.StatusCode),
                        (int)response.StatusCode,
                        responseBody);
                }

                return ParseResponse(
                    responseBody,
                    request);
            }

            public async Task<JsonElement>
                GetPayinsAsync(
                    string? status = null,
                    int? limit = null,
                    CancellationToken
                        cancellationToken = default)
            {
                ValidatePayinFilters(
                    ref status,
                    limit);

                var queryParts =
                    new List<string>();

                if (
                    !string.IsNullOrWhiteSpace(
                        status))
                {
                    queryParts.Add(
                        "status=" +
                        Uri.EscapeDataString(
                            status.Trim()
                                .ToUpperInvariant()));
                }

                if (limit.HasValue)
                {
                    queryParts.Add(
                        "limit=" +
                        limit.Value.ToString(
                            CultureInfo.InvariantCulture));
                }

                var query =
                    queryParts.Count == 0
                        ? string.Empty
                        : "?" +
                          string.Join(
                              "&",
                              queryParts);

                var requestUrl =
                    _config.ApiUrl.TrimEnd('/') +
                    PaymentUri +
                    query;

                var timestamp =
                    DateTimeOffset.UtcNow.ToString(
                        "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                        CultureInfo.InvariantCulture);

                var signature =
                    SignatureGenerator.Generate(
                        "GET",
                        PaymentUri,
                        timestamp,
                        string.Empty,
                        _config.SecretKey);

                using var requestMessage =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        requestUrl);

                requestMessage.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"));

                requestMessage.Headers.TryAddWithoutValidation(
                    "Xenith-Api-Key",
                    _config.ApiKey);

                requestMessage.Headers.TryAddWithoutValidation(
                    "Xenith-Request-Timestamp",
                    timestamp);

                requestMessage.Headers.TryAddWithoutValidation(
                    "Xenith-Request-Signature",
                    signature);

                HttpResponseMessage response;

                try
                {
                    response =
                        await _httpClient.SendAsync(
                            requestMessage,
                            cancellationToken);
                }
                catch (HttpRequestException exception)
                {
                    throw new WeselAjaApiException(
                        "Failed to connect to WeselAja: " +
                        exception.Message,
                        exception);
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (
                    string.IsNullOrWhiteSpace(
                        responseBody))
                {
                    throw new WeselAjaApiException(
                        "Empty response from WeselAja. " +
                        $"HTTP {(int)response.StatusCode}",
                        (int)response.StatusCode,
                        responseBody);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new WeselAjaApiException(
                        ExtractErrorFromBody(
                            responseBody,
                            response.StatusCode),
                        (int)response.StatusCode,
                        responseBody);
                }

                try
                {
                    using var document =
                        JsonDocument.Parse(
                            responseBody);

                    return document.RootElement.Clone();
                }
                catch (JsonException exception)
                {
                    throw new WeselAjaApiException(
                        "Invalid JSON response from WeselAja: " +
                        exception.Message,
                        (int)response.StatusCode,
                        responseBody);
                }
            }

            private static CreatePaymentResponse
                ParseResponse(
                    string responseBody,
                    CreatePaymentRequest request)
            {
                try
                {
                    using var document =
                        JsonDocument.Parse(
                            responseBody);

                    var root =
                        document.RootElement;

                    if (
                        root.ValueKind !=
                        JsonValueKind.Object)
                    {
                        throw new WeselAjaApiException(
                            "WeselAja returned an invalid response.",
                            null,
                            responseBody);
                    }

                    if (
                        root.TryGetProperty(
                            "success",
                            out var successProperty)
                        &&
                        successProperty.ValueKind ==
                            JsonValueKind.False)
                    {
                        throw new WeselAjaApiException(
                            ExtractErrorFromJson(
                                root,
                                responseBody),
                            null,
                            responseBody);
                    }

                    if (
                        root.TryGetProperty(
                            "data",
                            out var dataProperty)
                        &&
                        dataProperty.ValueKind ==
                            JsonValueKind.Object)
                    {
                        return MapResponse(
                            dataProperty,
                            request);
                    }

                    var paymentId =
                        GetString(
                            root,
                            "id",
                            "session");

                    if (
                        string.IsNullOrWhiteSpace(
                            paymentId))
                    {
                        throw new WeselAjaApiException(
                            ExtractErrorFromJson(
                                root,
                                responseBody),
                            null,
                            responseBody);
                    }

                    return MapResponse(
                        root,
                        request);
                }
                catch (JsonException exception)
                {
                    throw new WeselAjaApiException(
                        "Invalid JSON response from WeselAja: "
                        + exception.Message,
                        null,
                        responseBody);
                }
            }

            private static CreatePaymentResponse
                MapResponse(
                    JsonElement data,
                    CreatePaymentRequest request)
            {
                return new CreatePaymentResponse
                {
                    Id =
                        GetString(
                            data,
                            "id",
                            "session"),

                    InitiatedAmount =
                        GetDecimal(
                            data,
                            "initiatedAmount",
                            "amount")
                        ??
                        request.InitiatedAmount,

                    PaymentAmount =
                        GetDecimal(
                            data,
                            "paymentAmount"),

                    FeeAmount =
                        GetDecimal(
                            data,
                            "feeAmount"),

                    Currency =
                        GetString(
                            data,
                            "currency"),

                    PaymentMethod =
                        GetString(
                            data,
                            "paymentMethod"),

                    PaymentChannel =
                        GetString(
                            data,
                            "paymentChannel"),

                    PaymentCode =
                        GetString(
                            data,
                            "paymentCode",
                            "paymentUrl"),

                    PaymentCodeType =
                        GetString(
                            data,
                            "paymentCodeType"),

                    ReferenceCode =
                        GetString(
                            data,
                            "referenceCode")
                        ??
                        request.ReferenceCode,

                    CustomerReference =
                        GetString(
                            data,
                            "customerReference"),

                    CustomerName =
                        GetString(
                            data,
                            "customerName")
                        ??
                        request.CustomerName,

                    Status =
                        GetString(
                            data,
                            "status"),

                    CreatedTime =
                        GetString(
                            data,
                            "createdTime",
                            "createdAt"),

                    UpdatedTime =
                        GetString(
                            data,
                            "updatedTime",
                            "updatedAt"),

                    ExpirationTime =
                        GetString(
                            data,
                            "expirationTime",
                            "expiresAt"),

                    Description =
                        GetString(
                            data,
                            "description")
                        ??
                        request.Description,

                    CallbackUrl =
                        GetString(
                            data,
                            "callbackUrl"),

                    RedirectUrl =
                        GetString(
                            data,
                            "redirectUrl",
                            "redirect_url")
                };
            }

            private static string? GetString(
                JsonElement element,
                params string[] names)
            {
                foreach (
                    var name in names)
                {
                    if (
                        !element.TryGetProperty(
                            name,
                            out var property))
                    {
                        continue;
                    }

                    if (
                        property.ValueKind ==
                        JsonValueKind.String)
                    {
                        return property.GetString();
                    }
                }

                return null;
            }

            private static decimal? GetDecimal(
                JsonElement element,
                params string[] names)
            {
                foreach (
                    var name in names)
                {
                    if (
                        !element.TryGetProperty(
                            name,
                            out var property))
                    {
                        continue;
                    }

                    if (
                        property.ValueKind ==
                        JsonValueKind.Number
                        &&
                        property.TryGetDecimal(
                            out var decimalValue))
                    {
                        return decimalValue;
                    }

                    if (
                        property.ValueKind ==
                        JsonValueKind.String
                        &&
                        decimal.TryParse(
                            property.GetString(),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out var parsedValue))
                    {
                        return parsedValue;
                    }
                }

                return null;
            }

            private static string
                ExtractErrorMessage(
                    string responseBody,
                    HttpStatusCode statusCode)
            {
                try
                {
                    using var document =
                        JsonDocument.Parse(
                            responseBody);

                    return ExtractErrorFromJson(
                        document.RootElement,
                        responseBody);
                }
                catch
                {
                    return
                        $"WeselAja returned HTTP " +
                        $"{(int)statusCode} " +
                        $"{statusCode}.";
                }
            }

            private static string
                ExtractErrorFromJson(
                    JsonElement root,
                    string fallback)
            {
                foreach (
                    var propertyName
                    in new[]
                    {
                        "message",
                        "error",
                        "detail",
                        "code"
                    })
                {
                    if (
                        root.TryGetProperty(
                            propertyName,
                            out var property))
                    {
                        if (
                            property.ValueKind ==
                            JsonValueKind.String)
                        {
                            var value =
                                property.GetString();

                            if (
                                !string.IsNullOrWhiteSpace(
                                    value))
                            {
                                return value;
                            }
                        }
                    }
                }

                return
                    "WeselAja rejected the payment request. " +
                    fallback;
            }

            private static string
                ExtractErrorFromBody(
                    string responseBody,
                    HttpStatusCode statusCode)
            {
                try
                {
                    using var document =
                        JsonDocument.Parse(
                            responseBody);

                    return ExtractErrorFromJson(
                        document.RootElement,
                        responseBody);
                }
                catch
                {
                    return
                        $"WeselAja returned HTTP " +
                        $"{(int)statusCode} " +
                        $"{statusCode}.";
                }
            }

            private static void
                ValidatePayinFilters(
                    ref string? status,
                    int? limit)
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        status))
                {
                    status =
                        status.Trim()
                            .ToUpperInvariant();

                    if (
                        status != "PENDING"
                        &&
                        status != "SUCCESS"
                        &&
                        status != "EXPIRED"
                        &&
                        status != "FAILED")
                    {
                        throw new ArgumentException(
                            "status must be one of: " +
                            "PENDING, SUCCESS, EXPIRED, FAILED.");
                    }
                }

                if (
                    limit.HasValue
                    &&
                    limit.Value <= 0)
                {
                    throw new ArgumentException(
                        "limit must be a positive integer.");
                }
            }

            private static void
                ValidateRequest(
                    CreatePaymentRequest request)
            {
                if (
                    request.InitiatedAmount <= 0)
                {
                    throw new ArgumentException(
                        "Payment amount must be greater than zero.");
                }

                if (
                    string.IsNullOrWhiteSpace(
                        request.CustomerName))
                {
                    throw new ArgumentException(
                        "Customer name is required.");
                }

                if (
                    string.IsNullOrWhiteSpace(
                        request.Bank))
                {
                    throw new ArgumentException(
                        "Bank is required. " +
                        "Supported banks: " +
                        "BRI, Mandiri, BNI, Permata, CIMB, BCA.");
                }

                if (
                    !string.IsNullOrWhiteSpace(
                        request.ReferenceCode))
                {
                    ValidateReference(
                        request.ReferenceCode);
                }
            }

            private static void
                ValidateReference(
                    string value)
            {
                foreach (
                    var character in value)
                {
                    if (
                        char.IsLetterOrDigit(character)
                        ||
                        character == '-'
                        ||
                        character == '_')
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        "Reference values may only contain " +
                        "letters, numbers, '-' and '_'.");
                }
            }

            private static string
                GenerateReference(
                    string customerName)
            {
                var normalized =
                    new string(
                        customerName
                            .ToUpperInvariant()
                            .Where(
                                character =>
                                    char.IsLetterOrDigit(
                                        character)
                                    ||
                                    character == '-'
                                    ||
                                    character == '_')
                            .ToArray());

                if (
                    string.IsNullOrWhiteSpace(
                        normalized))
                {
                    normalized =
                        "CUSTOMER";
                }

                if (normalized.Length > 20)
                {
                    normalized =
                        normalized[..20];
                }

                var timestamp =
                    DateTime.UtcNow.ToString(
                        "yyyyMMddHHmmss",
                        CultureInfo.InvariantCulture);

                return
                    $"WES-{normalized}-{timestamp}";
            }

            public void Dispose()
            {
                _httpClient.Dispose();
            }
        }
        """;

    private const string
        CallbackServerTemplate =
        """
        using System.Text;
        using System.Text.Json;
        using WeselAja.Integration.Configuration;
        using WeselAja.Integration.Security;

        namespace WeselAja.Integration;

        internal static class CallbackServer
        {
            public static async Task RunAsync()
            {
                EnvLoader.Load();

                var secret =
                    Environment.GetEnvironmentVariable(
                        "WEB_SIGNATURE_SECRET")?.Trim();

                if (
                    string.IsNullOrWhiteSpace(
                        secret))
                {
                    throw new InvalidOperationException(
                        "WEB_SIGNATURE_SECRET is required.");
                }

                var portValue =
                    Environment.GetEnvironmentVariable(
                        "WESELAJA_CALLBACK_PORT");

                var port =
                    int.TryParse(
                        portValue,
                        out var configuredPort)
                        &&
                        configuredPort > 0
                        ? configuredPort
                        : 8081;

                var builder =
                    WebApplication.CreateBuilder();

                builder.WebHost.UseUrls(
                    $"http://0.0.0.0:{port}");

                var app =
                    builder.Build();

                app.MapPost(
                    "/payment/weselaja/callback",
                    async (HttpRequest request) =>
                    {
                        var timestamp =
                            request.Headers[
                                "x-middleware-timestamp"]
                                .ToString();

                        var signature =
                            request.Headers[
                                "x-middleware-signature"]
                                .ToString();

                        using var reader =
                            new StreamReader(
                                request.Body,
                                Encoding.UTF8);

                        var rawBody =
                            await reader.ReadToEndAsync();

                        if (
                            string.IsNullOrWhiteSpace(
                                timestamp))
                        {
                            return Results.Json(
                                new
                                {
                                    success = false,
                                    error =
                                        "Missing x-middleware-timestamp"
                                },
                                statusCode:
                                    StatusCodes
                                        .Status400BadRequest);
                        }

                        if (
                            string.IsNullOrWhiteSpace(
                                signature))
                        {
                            return Results.Json(
                                new
                                {
                                    success = false,
                                    error =
                                        "Missing x-middleware-signature"
                                },
                                statusCode:
                                    StatusCodes
                                        .Status400BadRequest);
                        }

                        var uri =
                            request.Path.Value
                            ??
                            "/payment/weselaja/callback";

                        if (
                            request.QueryString.HasValue)
                        {
                            uri +=
                                request.QueryString.Value;
                        }

                        var valid =
                            SignatureGenerator
                                .VerifyCallbackSignature(
                                    "POST",
                                    uri,
                                    rawBody,
                                    timestamp,
                                    signature,
                                    secret);

                        if (!valid)
                        {
                            return Results.Json(
                                new
                                {
                                    success = false,
                                    verified = false,
                                    error =
                                        "Invalid callback signature"
                                },
                                statusCode:
                                    StatusCodes
                                        .Status401Unauthorized);
                        }

                        object body;

                        try
                        {
                            using var document =
                                JsonDocument.Parse(
                                    rawBody);

                            body =
                                document.RootElement
                                    .Clone();
                        }
                        catch
                        {
                            body =
                                rawBody;
                        }

                        return Results.Json(
                            new
                            {
                                success = true,
                                verified = true,
                                body
                            });
                    });

                Console.WriteLine(
                    "WeselAja callback handler listening on " +
                    $"http://0.0.0.0:{port}" +
                    "/payment/weselaja/callback");

                await app.RunAsync();
            }
        }
        """;

    private const string
        RedirectServerTemplate =
        """
        using System.Text.Json;

        namespace WeselAja.Integration;

        internal static class RedirectServer
        {
            public static async Task RunAsync()
            {
                var portValue =
                    Environment.GetEnvironmentVariable(
                        "WESELAJA_REDIRECT_PORT");

                var port =
                    int.TryParse(
                        portValue,
                        out var configuredPort)
                        &&
                        configuredPort > 0
                        ? configuredPort
                        : 8080;

                var builder =
                    WebApplication.CreateBuilder();

                builder.WebHost.UseUrls(
                    $"http://0.0.0.0:{port}");

                var app =
                    builder.Build();

                app.MapGet(
                    "/payment/weselaja/return",
                    (HttpRequest request) =>
                    {
                        var parameters =
                            request.Query.ToDictionary(
                                pair =>
                                    pair.Key,
                                pair =>
                                    pair.Value.ToString());

                        return Results.Json(
                            new
                            {
                                success = true,

                                path =
                                    request.Path.Value,

                                parameters
                            },
                            new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                    });

                Console.WriteLine(
                    "WeselAja redirect handler listening on " +
                    $"http://0.0.0.0:{port}" +
                    "/payment/weselaja/return");

                await app.RunAsync();
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
        bin/
        obj/
        .vs/
        *.user
        *.suo
        """;

    private const string
        SelfTestTemplate =
        """
        using WeselAja.Integration.Models;
        using WeselAja.Integration.Security;

        namespace WeselAja.Integration;

        internal static class SelfTest
        {
            public static void Run()
            {
                Assert(
                    BankChannels.Resolve("BRI") ==
                        "BRI.VA",
                    "BRI mapping failed.");

                Assert(
                    BankChannels.Resolve("Bank BRI") ==
                        "BRI.VA",
                    "Bank BRI mapping failed.");

                Assert(
                    BankChannels.Resolve("Mandiri") ==
                        "MDR.VA",
                    "Mandiri mapping failed.");

                Assert(
                    BankChannels.Resolve("Bank Mandiri") ==
                        "MDR.VA",
                    "Bank Mandiri mapping failed.");

                Assert(
                    BankChannels.Resolve("BNI") ==
                        "BNI.VA",
                    "BNI mapping failed.");

                Assert(
                    BankChannels.Resolve("Permata") ==
                        "PTB.VA",
                    "Permata mapping failed.");

                Assert(
                    BankChannels.Resolve("CIMB") ==
                        "CIMB.VA",
                    "CIMB mapping failed.");

                Assert(
                    BankChannels.Resolve("CIMB Niaga") ==
                        "CIMB.VA",
                    "CIMB Niaga mapping failed.");

                Assert(
                    BankChannels.Resolve("BCA") ==
                        "BCA.VA",
                    "BCA mapping failed.");

                var unsupportedRejected =
                    false;

                try
                {
                    BankChannels.Resolve(
                        "Bank Tidak Ada");
                }
                catch (ArgumentException)
                {
                    unsupportedRejected =
                        true;
                }

                Assert(
                    unsupportedRejected,
                    "Unsupported bank should be rejected.");

                const string method =
                    "POST";

                const string uri =
                    "/payment/weselaja/callback";

                const string body =
                    "{\"hello\":\"world\"}";

                const string timestamp =
                    "2026-01-01T00:00:00.000Z";

                const string secret =
                    "test-secret";

                var signature =
                    SignatureGenerator
                        .GenerateCallbackSignature(
                            method,
                            uri,
                            body,
                            timestamp,
                            secret);

                Assert(
                    SignatureGenerator
                        .VerifyCallbackSignature(
                            method,
                            uri,
                            body,
                            timestamp,
                            signature,
                            secret),
                    "Callback signature verification failed.");

                Assert(
                    !SignatureGenerator
                        .VerifyCallbackSignature(
                            method,
                            uri,
                            body,
                            timestamp,
                            signature + "x",
                            secret),
                    "Invalid callback signature should be rejected.");

                Console.WriteLine(
                    "All C# WeselAja self-tests passed.");
            }

            private static void Assert(
                bool condition,
                string message)
            {
                if (!condition)
                {
                    throw new InvalidOperationException(
                        message);
                }
            }
        }
        """;
}