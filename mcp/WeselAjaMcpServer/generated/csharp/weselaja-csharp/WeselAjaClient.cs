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
        "WeselAja-CSharp/1.0";

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