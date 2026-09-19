using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using XenithPayMcp.Configuration;
using XenithPayMcp.Models;
using XenithPayMcp.Security;

namespace XenithPayMcp.Services;

public class XenithPayClient : IXenithPayClient
{
    private readonly HttpClient _httpClient;
    private readonly XenithPayOptions _options;
    private readonly SignatureGenerator _signatureGenerator;

    private const string PaymentUri = "/v1/payins";
    private const string UserAgent = "Python-urllib/3.13";

    public XenithPayClient(
        HttpClient httpClient,
        XenithPayOptions options,
        SignatureGenerator signatureGenerator)
    {
        _httpClient = httpClient;
        _options = options;
        _signatureGenerator = signatureGenerator;
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return ErrorResponse(
                "XenithPay is disabled.");
        }

        var apiUrl =
            _options.ApiUrl?.Trim()
            ?? string.Empty;

        var apiKey =
            _options.ApiKey?.Trim()
            ?? string.Empty;

        var secretKey =
            _options.SecretKey?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            return ErrorResponse(
                "WeselAja API URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ErrorResponse(
                "XenithPay API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return ErrorResponse(
                "XenithPay secret key is not configured.");
        }

        if (request.InitiatedAmount <= 0)
        {
            return ErrorResponse(
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            return ErrorResponse(
                "Currency is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            return ErrorResponse(
                "Payment method is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentChannel))
        {
            return ErrorResponse(
                "Payment channel is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ReferenceCode))
        {
            return ErrorResponse(
                "Reference code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerReference))
        {
            return ErrorResponse(
                "Customer reference is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return ErrorResponse(
                "Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            return ErrorResponse(
                "Callback URL is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUrl))
        {
            return ErrorResponse(
                "Redirect URL is required.");
        }

        if (!IsValidReference(request.ReferenceCode))
        {
            return ErrorResponse(
                "Reference code may only contain letters, numbers, '-' and '_'.");
        }

        if (!IsValidReference(request.CustomerReference))
        {
            return ErrorResponse(
                "Customer reference may only contain letters, numbers, '-' and '_'.");
        }

        var payload =
            new Dictionary<string, object?>
            {
                ["initiatedAmount"] =
                    request.InitiatedAmount,

                ["currency"] =
                    request.Currency,

                ["paymentMethod"] =
                    request.PaymentMethod,

                ["paymentChannel"] =
                    request.PaymentChannel,

                ["referenceCode"] =
                    request.ReferenceCode,

                ["customerReference"] =
                    request.CustomerReference,

                ["customerName"] =
                    request.CustomerName,

                ["callbackUrl"] =
                    request.CallbackUrl,

                ["redirectUrl"] =
                    request.RedirectUrl
            };

        if (
            !string.IsNullOrWhiteSpace(
                request.CustomerPhoneNumber))
        {
            payload[
                "customerPhoneNumber"
            ] =
                request.CustomerPhoneNumber;
        }

        if (
            !string.IsNullOrWhiteSpace(
                request.Description))
        {
            payload[
                "description"
            ] =
                request.Description;
        }

        var jsonOptions =
            new JsonSerializerOptions
            {
                WriteIndented = false,

                Encoder =
                    JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

                DefaultIgnoreCondition =
                    System.Text.Json.Serialization
                        .JsonIgnoreCondition
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
            _signatureGenerator.Generate(
                "POST",
                PaymentUri,
                timestamp,
                body,
                secretKey);

        var idempotencyKey =
            "csharp-" +
            Guid.NewGuid().ToString("N");

        var requestUrl =
            apiUrl.TrimEnd('/') +
            PaymentUri;

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                requestUrl)
            {
                Version =
                    HttpVersion.Version11,

                VersionPolicy =
                    HttpVersionPolicy.RequestVersionExact
            };

        var bodyBytes =
            Encoding.UTF8.GetBytes(
                body);

        var content =
            new ByteArrayContent(
                bodyBytes);

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
            apiKey);

        httpRequest.Headers.Add(
            "Xenith-Request-Timestamp",
            timestamp);

        httpRequest.Headers.Add(
            "Xenith-Request-Signature",
            signature);

        httpRequest.Headers.Add(
            "X-Idempotency-Key",
            idempotencyKey);

        var signaturePayload =
            "POST" +
            "\n" +
            PaymentUri +
            "\n" +
            timestamp +
            "\n" +
            body;

        var stopwatch =
            Stopwatch.StartNew();

        Console.Error.WriteLine();

        Console.Error.WriteLine(
            "==================================================");

        Console.Error.WriteLine(
            "           XENITHPAY HTTP DEBUG REQUEST");

        Console.Error.WriteLine(
            "==================================================");

        Console.Error.WriteLine(
            $"API URL          : {apiUrl}");

        Console.Error.WriteLine(
            $"Request URI      : {PaymentUri}");

        Console.Error.WriteLine(
            $"Method           : {httpRequest.Method}");

        Console.Error.WriteLine(
            $"URL              : {httpRequest.RequestUri}");

        Console.Error.WriteLine(
            $"HTTP Version     : {httpRequest.Version}");

        Console.Error.WriteLine(
            $"Version Policy   : {httpRequest.VersionPolicy}");

        Console.Error.WriteLine(
            $"Timestamp        : {timestamp}");

        Console.Error.WriteLine(
            $"API Key          : {MaskSecretHeader(apiKey)}");

        Console.Error.WriteLine(
            $"API Key SHA256   : {Sha256(apiKey)}");

        Console.Error.WriteLine(
            $"Secret SHA256    : {Sha256(secretKey)}");

        Console.Error.WriteLine(
            $"Signature        : {signature}");

        Console.Error.WriteLine(
            $"Idempotency      : {idempotencyKey}");

        Console.Error.WriteLine(
            $"Body Length      : {bodyBytes.Length}");

        Console.Error.WriteLine(
            $"Body SHA256      : {Sha256(bodyBytes)}");

        Console.Error.WriteLine(
            $"Signature Payload SHA256: {Sha256(signaturePayload)}");

        Console.Error.WriteLine(
            $"Request Body     : {body}");

        Console.Error.WriteLine();

        Console.Error.WriteLine(
            "Request Headers  :");

        foreach (
            var header in httpRequest.Headers)
        {
            var value =
                header.Key.Equals(
                    "Xenith-Api-Key",
                    StringComparison.OrdinalIgnoreCase)
                    ? MaskSecretHeader(
                        string.Join(
                            ", ",
                            header.Value))
                    : string.Join(
                        ", ",
                        header.Value);

            Console.Error.WriteLine(
                $"  {header.Key}: {value}");
        }

        if (
            httpRequest.Content != null)
        {
            foreach (
                var header in
                httpRequest.Content.Headers)
            {
                Console.Error.WriteLine(
                    $"  {header.Key}: {string.Join(", ", header.Value)}");
            }
        }

        Console.Error.WriteLine(
            "==================================================");

        try
        {
            using var response =
                await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken);

            stopwatch.Stop();

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            Console.Error.WriteLine();

            Console.Error.WriteLine(
                "==================================================");

            Console.Error.WriteLine(
                "          XENITHPAY HTTP DEBUG RESPONSE");

            Console.Error.WriteLine(
                "==================================================");

            Console.Error.WriteLine(
                $"HTTP Status      : {(int)response.StatusCode} {response.StatusCode}");

            Console.Error.WriteLine(
                $"HTTP Version     : {response.Version}");

            Console.Error.WriteLine(
                $"Elapsed          : {stopwatch.ElapsedMilliseconds} ms");

            Console.Error.WriteLine(
                $"Effective URL    : {requestUrl}");

            Console.Error.WriteLine(
                $"Content-Type     : {response.Content.Headers.ContentType}");

            Console.Error.WriteLine(
                $"Content-Length   : {response.Content.Headers.ContentLength}");

            Console.Error.WriteLine(
                $"Response Body Len: {Encoding.UTF8.GetByteCount(responseBody)}");

            Console.Error.WriteLine();

            Console.Error.WriteLine(
                "Response Headers:");

            foreach (
                var header in response.Headers)
            {
                Console.Error.WriteLine(
                    $"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            foreach (
                var header in
                response.Content.Headers)
            {
                Console.Error.WriteLine(
                    $"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            Console.Error.WriteLine();

            Console.Error.WriteLine(
                "RAW RESPONSE BODY:");

            Console.Error.WriteLine(
                "--------------------------------------------------");

            Console.Error.WriteLine(
                string.IsNullOrEmpty(
                    responseBody)
                    ? "<EMPTY>"
                    : responseBody);

            Console.Error.WriteLine(
                "--------------------------------------------------");

            Console.Error.WriteLine(
                "==================================================");

            if (
                string.IsNullOrWhiteSpace(
                    responseBody))
            {
                return ErrorResponse(
                    $"Empty response from WeselAja. " +
                    $"HTTP {(int)response.StatusCode} " +
                    $"{response.StatusCode}");
            }

            if (
                !response.IsSuccessStatusCode)
            {
                return ErrorResponse(
                    ExtractErrorFromResponse(
                        responseBody,
                        response.StatusCode));
            }

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
                    return ErrorResponse(
                        "WeselAja returned an invalid payment response.");
                }

                if (
                    root.TryGetProperty(
                        "success",
                        out var successProperty)
                    &&
                    successProperty.ValueKind ==
                    JsonValueKind.False)
                {
                    return ErrorResponse(
                        ExtractErrorFromJson(
                            root,
                            responseBody));
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
                    return ErrorResponse(
                        ExtractErrorFromJson(
                            root,
                            responseBody));
                }

                return MapResponse(
                    root,
                    request);
            }
            catch (
                JsonException exception)
            {
                return ErrorResponse(
                    $"Invalid JSON response from WeselAja. " +
                    $"HTTP {(int)response.StatusCode}: " +
                    $"{exception.Message}");
            }
        }
        catch (
            HttpRequestException exception)
        {
            stopwatch.Stop();

            Console.Error.WriteLine();

            Console.Error.WriteLine(
                "==================================================");

            Console.Error.WriteLine(
                "           XENITHPAY HTTP EXCEPTION");

            Console.Error.WriteLine(
                "==================================================");

            Console.Error.WriteLine(
                $"Elapsed      : {stopwatch.ElapsedMilliseconds} ms");

            Console.Error.WriteLine(
                $"Message      : {exception.Message}");

            Console.Error.WriteLine(
                $"Status Code  : {exception.StatusCode}");

            Console.Error.WriteLine(
                $"Inner        : {exception.InnerException?.Message}");

            Console.Error.WriteLine(
                $"Stack Trace  : {exception}");

            Console.Error.WriteLine(
                "==================================================");

            return ErrorResponse(
                $"HTTP request to WeselAja failed: {exception.Message}");
        }
        catch (
            TaskCanceledException exception)
        {
            stopwatch.Stop();

            Console.Error.WriteLine();

            Console.Error.WriteLine(
                "==================================================");

            Console.Error.WriteLine(
                "         XENITHPAY HTTP TIMEOUT/CANCEL");

            Console.Error.WriteLine(
                "==================================================");

            Console.Error.WriteLine(
                $"Elapsed      : {stopwatch.ElapsedMilliseconds} ms");

            Console.Error.WriteLine(
                $"Message      : {exception.Message}");

            Console.Error.WriteLine(
                $"Inner        : {exception.InnerException?.Message}");

            Console.Error.WriteLine(
                "==================================================");

            return ErrorResponse(
                "WeselAja HTTP request timed out or was cancelled.");
        }
    }

    private static string Sha256(
        string value)
    {
        return Sha256(
            Encoding.UTF8.GetBytes(
                value));
    }

    private static string Sha256(
        byte[] value)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                value))
            .ToLowerInvariant();
    }

    private static string MaskSecretHeader(
        string value)
    {
        if (
            string.IsNullOrWhiteSpace(
                value))
        {
            return "***";
        }

        if (
            value.Length <= 8)
        {
            return "***";
        }

        return
            value[..4] +
            "***" +
            value[^4..];
    }

    private static CreatePaymentResponse MapResponse(
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
                    "currency")
                ??
                request.Currency,

            PaymentMethod =
                GetString(
                    data,
                    "paymentMethod")
                ??
                request.PaymentMethod,

            PaymentChannel =
                GetString(
                    data,
                    "paymentChannel")
                ??
                request.PaymentChannel,

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
                    "customerReference")
                ??
                request.CustomerReference,

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
                    "callbackUrl")
                ??
                request.CallbackUrl,

            RedirectUrl =
                GetString(
                    data,
                    "redirectUrl",
                    "redirect_url")
                ??
                request.RedirectUrl,

            Error =
                null
        };
    }

    private static bool IsValidReference(
        string value)
    {
        foreach (
            var character in value)
        {
            if (
                char.IsLetterOrDigit(
                    character)
                ||
                character == '-'
                ||
                character == '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static string ExtractErrorFromResponse(
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
        catch (
            JsonException)
        {
            return
                $"WeselAja returned HTTP " +
                $"{(int)statusCode} {statusCode}: " +
                responseBody;
        }
    }

    private static string ExtractErrorFromJson(
        JsonElement root,
        string rawResponse)
    {
        if (
            root.TryGetProperty(
                "message",
                out var messageProperty)
            &&
            messageProperty.ValueKind ==
            JsonValueKind.String)
        {
            return
                messageProperty.GetString()
                ??
                rawResponse;
        }

        if (
            root.TryGetProperty(
                "error",
                out var errorProperty)
            &&
            errorProperty.ValueKind ==
            JsonValueKind.String)
        {
            return
                errorProperty.GetString()
                ??
                rawResponse;
        }

        if (
            root.TryGetProperty(
                "errors",
                out var errorsProperty))
        {
            return
                errorsProperty.ToString();
        }

        if (
            root.TryGetProperty(
                "code",
                out var codeProperty)
            &&
            codeProperty.ValueKind ==
            JsonValueKind.String)
        {
            return
                codeProperty.GetString()
                ??
                rawResponse;
        }

        return rawResponse;
    }

    private static string? GetString(
        JsonElement element,
        params string[] propertyNames)
    {
        foreach (
            var propertyName in propertyNames)
        {
            if (
                !element.TryGetProperty(
                    propertyName,
                    out var property))
            {
                continue;
            }

            return property.ValueKind switch
            {
                JsonValueKind.String =>
                    property.GetString(),

                JsonValueKind.Number =>
                    property.ToString(),

                JsonValueKind.True =>
                    "true",

                JsonValueKind.False =>
                    "false",

                JsonValueKind.Null =>
                    null,

                _ =>
                    property.ToString()
            };
        }

        return null;
    }

    private static decimal? GetDecimal(
        JsonElement element,
        params string[] propertyNames)
    {
        foreach (
            var propertyName in propertyNames)
        {
            if (
                !element.TryGetProperty(
                    propertyName,
                    out var property))
            {
                continue;
            }

            if (
                property.ValueKind ==
                JsonValueKind.Number
                &&
                property.TryGetDecimal(
                    out var number))
            {
                return number;
            }

            if (
                property.ValueKind ==
                JsonValueKind.String
                &&
                decimal.TryParse(
                    property.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static CreatePaymentResponse ErrorResponse(
        string message)
    {
        return new CreatePaymentResponse
        {
            Error = message
        };
    }
}
