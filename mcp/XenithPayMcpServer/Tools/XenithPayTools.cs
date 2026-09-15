using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using XenithPayMcpServer.Client;
using XenithPayMcpServer.Clients;
using XenithPayMcpServer.Models;

namespace XenithPayMcpServer.Tools;

[McpServerToolType]
public static class XenithPayTools
{
    [McpServerTool(
        Name = "is_xenithpay_active")]
    [Description(
        "Checks whether XenithPay is active using the selected implementation.")]
    public static async Task<string> IsActive(
        [Description(
            "Implementation to use: CSharp, PHP, Python, or Odoo")]
        XenithPayImplementation implementation,

        CSharpMcpClient csharpClient,
        PhpXenithPayClient phpClient,
        PythonXenithPayClient pythonClient,
        OdooXenithPayClient odooClient,

        CancellationToken cancellationToken = default)
    {
        var result =
            await CallImplementation(
                implementation,
                "is_xenithpay_active",
                null,
                csharpClient,
                phpClient,
                pythonClient,
                odooClient,
                cancellationToken);

        return NormalizeActiveResult(result);
    }

    [McpServerTool(
        Name = "get_xenithpay_payment_method")]
    [Description(
        "Returns XenithPay payment method information using the selected implementation.")]
    public static async Task<string> GetPaymentMethod(
        [Description(
            "Implementation to use: CSharp, PHP, Python, or Odoo")]
        XenithPayImplementation implementation,

        CSharpMcpClient csharpClient,
        PhpXenithPayClient phpClient,
        PythonXenithPayClient pythonClient,
        OdooXenithPayClient odooClient,

        CancellationToken cancellationToken = default)
    {
        var result =
            await CallImplementation(
                implementation,
                "get_xenithpay_payment_method",
                null,
                csharpClient,
                phpClient,
                pythonClient,
                odooClient,
                cancellationToken);

        return NormalizePaymentMethodResult(result);
    }

    [McpServerTool(
        Name = "create_xenithpay_payment")]
    [Description(
        "Creates a XenithPay payment using the selected implementation.")]
    public static async Task<string> CreatePayment(
        [Description(
            "Implementation to use: CSharp, PHP, Python, or Odoo")]
        XenithPayImplementation implementation,

        [Description(
            "Payment amount")]
        int amount,

        [Description(
            "Currency code, for example IDR")]
        string currency,

        [Description(
            "Payment method, for example VIRTUAL_ACCOUNT")]
        string paymentMethod,

        [Description(
            "Payment channel, for example BRI.VA")]
        string paymentChannel,

        [Description(
            "Unique merchant reference code")]
        string referenceCode,

        [Description(
            "Customer reference identifier")]
        string customerReference,

        [Description(
            "Customer full name")]
        string customerName,

        [Description(
            "Callback URL provided by the calling application")]
        string callbackUrl,

        [Description(
            "Redirect URL provided by the calling application")]
        string redirectUrl,

        CSharpMcpClient csharpClient,
        PhpXenithPayClient phpClient,
        PythonXenithPayClient pythonClient,
        OdooXenithPayClient odooClient,

        [Description(
            "Customer phone number. Leave empty when not required.")]
        string customerPhoneNumber = "",

        [Description(
            "Payment description. Leave empty when not required.")]
        string description = "",

        CancellationToken cancellationToken = default)
    {
        var arguments =
            new Dictionary<string, object?>
            {
                ["amount"] =
                    amount,

                ["currency"] =
                    currency,

                ["paymentMethod"] =
                    paymentMethod,

                ["paymentChannel"] =
                    paymentChannel,

                ["referenceCode"] =
                    referenceCode,

                ["customerReference"] =
                    customerReference,

                ["customerName"] =
                    customerName,

                ["customerPhoneNumber"] =
                    string.IsNullOrWhiteSpace(
                        customerPhoneNumber)
                        ? null
                        : customerPhoneNumber,

                ["description"] =
                    string.IsNullOrWhiteSpace(
                        description)
                        ? null
                        : description,

                ["callbackUrl"] =
                    callbackUrl,

                ["redirectUrl"] =
                    redirectUrl
            };

        var result =
            await CallImplementation(
                implementation,
                "create_xenithpay_payment",
                arguments,
                csharpClient,
                phpClient,
                pythonClient,
                odooClient,
                cancellationToken);

        return NormalizePaymentResult(result);
    }

    private static async Task<string> CallImplementation(
        XenithPayImplementation implementation,
        string tool,
        IReadOnlyDictionary<string, object?>? arguments,
        CSharpMcpClient csharpClient,
        PhpXenithPayClient phpClient,
        PythonXenithPayClient pythonClient,
        OdooXenithPayClient odooClient,
        CancellationToken cancellationToken)
    {
        Console.Error.WriteLine(
            $"[CENTRAL] Implementation: {implementation}");

        Console.Error.WriteLine(
            $"[CENTRAL] Tool: {tool}");

        switch (implementation)
        {
            case XenithPayImplementation.CSharp:
                {
                    CallToolResult result;

                    if (arguments is null)
                    {
                        result =
                            await csharpClient.Client.CallToolAsync(
                                tool,
                                cancellationToken:
                                    cancellationToken);
                    }
                    else
                    {
                        result =
                            await csharpClient.Client.CallToolAsync(
                                tool,
                                arguments,
                                cancellationToken:
                                    cancellationToken);
                    }

                    return ExtractCSharpResult(result);
                }

            case XenithPayImplementation.PHP:
                {
                    var result =
                        await phpClient.CallAsync(
                            tool,
                            arguments);

                    return result.GetRawText();
                }

            case XenithPayImplementation.Python:
                {
                    var result =
                        await pythonClient.CallAsync(
                            tool,
                            arguments,
                            cancellationToken);

                    return result.GetRawText();
                }

            case XenithPayImplementation.Odoo:
                {
                    var result =
                        await odooClient.CallAsync(
                            tool,
                            arguments,
                            cancellationToken);

                    return result.GetRawText();
                }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(implementation),
                    implementation,
                    "Unsupported XenithPay implementation.");
        }
    }

    private static string ExtractCSharpResult(
        CallToolResult result)
    {
        if (result.IsError is true)
        {
            var error =
                result.Content
                    .OfType<TextContentBlock>()
                    .FirstOrDefault()
                    ?.Text
                ?? "C# MCP tool failed.";

            throw new InvalidOperationException(
                error);
        }

        if (result.StructuredContent is JsonElement structured)
        {
            return structured.GetRawText();
        }

        var text =
            result.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault()
                ?.Text;

        return text ?? "null";
    }

    private static string NormalizeActiveResult(
        string raw)
    {
        using var document =
            JsonDocument.Parse(raw);

        var root =
            document.RootElement;

        var active = false;

        if (root.ValueKind ==
                JsonValueKind.True ||
            root.ValueKind ==
                JsonValueKind.False)
        {
            active =
                root.GetBoolean();

            return JsonSerializer.Serialize(
                new
                {
                    success = true,
                    active,
                    error = (string?)null
                });
        }

        if (root.TryGetProperty(
                "active",
                out var activeProperty) &&
            (activeProperty.ValueKind ==
                JsonValueKind.True ||
             activeProperty.ValueKind ==
                JsonValueKind.False))
        {
            active =
                activeProperty.GetBoolean();
        }

        var success = true;

        if (root.TryGetProperty(
                "success",
                out var successProperty) &&
            (successProperty.ValueKind ==
                JsonValueKind.True ||
             successProperty.ValueKind ==
                JsonValueKind.False))
        {
            success =
                successProperty.GetBoolean();
        }

        string? error = null;

        if (root.TryGetProperty(
                "error",
                out var errorProperty) &&
            errorProperty.ValueKind !=
                JsonValueKind.Null)
        {
            error =
                errorProperty.ValueKind ==
                    JsonValueKind.String
                    ? errorProperty.GetString()
                    : errorProperty.GetRawText();
        }

        return JsonSerializer.Serialize(
            new
            {
                success,
                active,
                error
            });
    }

    private static string NormalizePaymentMethodResult(
        string raw)
    {
        using var document =
            JsonDocument.Parse(raw);

        var root =
            document.RootElement;

        var success = true;

        if (root.TryGetProperty(
                "success",
                out var successProperty) &&
            (successProperty.ValueKind ==
                JsonValueKind.True ||
             successProperty.ValueKind ==
                JsonValueKind.False))
        {
            success =
                successProperty.GetBoolean();
        }

        var id =
            GetStringOrNull(
                root,
                "id");

        var title =
            GetStringOrNull(
                root,
                "title");

        var description =
            GetStringOrNull(
                root,
                "description");

        var icon =
            GetStringOrNull(
                root,
                "icon");

        var supports =
            GetPropertyOrNull(
                root,
                "supports")
            ?? JsonSerializer.SerializeToElement(
                Array.Empty<string>());

        string? error = null;

        if (root.TryGetProperty(
                "error",
                out var errorProperty) &&
            errorProperty.ValueKind !=
                JsonValueKind.Null)
        {
            error =
                errorProperty.ValueKind ==
                    JsonValueKind.String
                    ? errorProperty.GetString()
                    : errorProperty.GetRawText();
        }

        return JsonSerializer.Serialize(
            new
            {
                success,
                id,
                title,
                description,
                icon,
                supports,
                error
            });
    }

    private static string NormalizePaymentResult(
        string raw)
    {
        using var document =
            JsonDocument.Parse(raw);

        var root =
            document.RootElement;

        var success = false;

        if (root.TryGetProperty(
                "success",
                out var successProperty) &&
            (successProperty.ValueKind ==
                JsonValueKind.True ||
             successProperty.ValueKind ==
                JsonValueKind.False))
        {
            success =
                successProperty.GetBoolean();
        }
        else if (root.TryGetProperty(
                     "isSuccess",
                     out var isSuccessProperty) &&
                 (isSuccessProperty.ValueKind ==
                     JsonValueKind.True ||
                  isSuccessProperty.ValueKind ==
                     JsonValueKind.False))
        {
            success =
                isSuccessProperty.GetBoolean();
        }
        else if (
            root.TryGetProperty(
                "paymentId",
                out _) ||
            root.TryGetProperty(
                "id",
                out _))
        {
            success = true;
        }

        string? error = null;

        if (root.TryGetProperty(
                "error",
                out var errorProperty) &&
            errorProperty.ValueKind !=
                JsonValueKind.Null)
        {
            error =
                errorProperty.ValueKind ==
                    JsonValueKind.String
                    ? errorProperty.GetString()
                    : errorProperty.GetRawText();
        }

        return JsonSerializer.Serialize(
            new
            {
                success,

                paymentId =
                    GetStringOrNull(
                        root,
                        "paymentId",
                        "id"),

                initiatedAmount =
                    GetPropertyOrNull(
                        root,
                        "initiatedAmount",
                        "amount"),

                paymentAmount =
                    GetPropertyOrNull(
                        root,
                        "paymentAmount"),

                feeAmount =
                    GetPropertyOrNull(
                        root,
                        "feeAmount"),

                currency =
                    GetStringOrNull(
                        root,
                        "currency"),

                paymentMethod =
                    GetStringOrNull(
                        root,
                        "paymentMethod"),

                paymentChannel =
                    GetStringOrNull(
                        root,
                        "paymentChannel"),

                paymentCode =
                    GetPropertyOrNull(
                        root,
                        "paymentCode",
                        "paymentUrl"),

                paymentCodeType =
                    GetStringOrNull(
                        root,
                        "paymentCodeType"),

                referenceCode =
                    GetStringOrNull(
                        root,
                        "referenceCode"),

                customerReference =
                    GetPropertyOrNull(
                        root,
                        "customerReference"),

                customerName =
                    GetStringOrNull(
                        root,
                        "customerName"),

                status =
                    GetStringOrNull(
                        root,
                        "status"),

                createdTime =
                    GetStringOrNull(
                        root,
                        "createdTime"),

                updatedTime =
                    GetStringOrNull(
                        root,
                        "updatedTime"),

                expirationTime =
                    GetStringOrNull(
                        root,
                        "expirationTime"),

                description =
                    GetStringOrNull(
                        root,
                        "description"),

                callbackUrl =
                    GetStringOrNull(
                        root,
                        "callbackUrl"),

                redirectUrl =
                    GetStringOrNull(
                        root,
                        "redirectUrl",
                        "redirect_url"),

                payerAccountName =
                    GetStringOrNull(
                        root,
                        "payerAccountName"),

                payerAccountNumber =
                    GetStringOrNull(
                        root,
                        "payerAccountNumber"),

                payerPaymentChannel =
                    GetStringOrNull(
                        root,
                        "payerPaymentChannel"),

                metadata =
                    GetPropertyOrNull(
                        root,
                        "metadata")
                    ?? JsonSerializer.SerializeToElement(
                        new Dictionary<string, object?>()),

                error
            });
    }

    private static string? GetStringOrNull(
        JsonElement root,
        params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(
                    propertyName,
                    out var property))
            {
                continue;
            }

            switch (property.ValueKind)
            {
                case JsonValueKind.String:
                    return property.GetString();

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return property.ToString();

                case JsonValueKind.Null:
                    return null;
            }
        }

        return null;
    }

    private static JsonElement? GetPropertyOrNull(
        JsonElement root,
        params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (root.TryGetProperty(
                    propertyName,
                    out var property))
            {
                return property.Clone();
            }
        }

        return null;
    }
}