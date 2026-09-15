using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        if (string.IsNullOrWhiteSpace(_options.ApiUrl))
        {
            return ErrorResponse(
                "WeselAja API URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return ErrorResponse(
                "XenithPay API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
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

        var requestBody = new
        {
            initiatedAmount =
                request.InitiatedAmount,

            currency =
                request.Currency,

            paymentChannel =
                request.PaymentChannel,

            customerPhoneNumber =
                string.IsNullOrWhiteSpace(
                    request.CustomerPhoneNumber)
                    ? null
                    : request.CustomerPhoneNumber,

            referenceCode =
                request.ReferenceCode,

            customerReference =
                request.CustomerReference,

            customerName =
                request.CustomerName,

            description =
                string.IsNullOrWhiteSpace(
                    request.Description)
                    ? null
                    : request.Description,

            callbackUrl =
                request.CallbackUrl,

            redirectUrl =
                request.RedirectUrl
        };

        var jsonOptions =
            new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull
            };

        var body =
            JsonSerializer.Serialize(
                requestBody,
                jsonOptions)
            + "\n";

        var timestamp =
            DateTimeOffset.UtcNow.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

        var signature =
            _signatureGenerator.Generate(
                "POST",
                PaymentUri,
                timestamp,
                body,
                _options.SecretKey);

        var requestUrl =
            $"{_options.ApiUrl.TrimEnd('/')}{PaymentUri}";

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

        httpRequest.Content =
            new StringContent(
                body,
                Encoding.UTF8,
                "application/json");

        httpRequest.Headers.Add(
            "Xenith-Api-Key",
            _options.ApiKey);

        httpRequest.Headers.Add(
            "Xenith-Request-Timestamp",
            timestamp);

        httpRequest.Headers.Add(
            "Xenith-Request-Signature",
            signature);

        httpRequest.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        using var response =
            await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return ErrorResponse(
                $"Empty response from WeselAja. " +
                $"HTTP {(int)response.StatusCode} " +
                $"{response.StatusCode}");
        }

        if (!response.IsSuccessStatusCode)
        {
            return ErrorResponse(
                ExtractErrorFromResponse(
                    responseBody,
                    response.StatusCode));
        }

        JsonDocument document;

        try
        {
            document =
                JsonDocument.Parse(
                    responseBody);
        }
        catch (JsonException exception)
        {
            return ErrorResponse(
                $"Invalid JSON response from WeselAja. " +
                $"HTTP {(int)response.StatusCode}: " +
                $"{exception.Message}");
        }

        using (document)
        {
            var root =
                document.RootElement;

            if (root.ValueKind !=
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

            if (string.IsNullOrWhiteSpace(
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

            Metadata =
                data.TryGetProperty(
                    "metadata",
                    out var metadata)
                    ? metadata.Clone()
                    : null,

            Error =
                null
        };
    }

    private static bool IsValidReference(
        string value)
    {
        foreach (var character in value)
        {
            if (
                char.IsLetterOrDigit(character) ||
                character == '-' ||
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
        catch (JsonException)
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

        return rawResponse;
    }

    private static string? GetString(
        JsonElement element,
        params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(
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
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(
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