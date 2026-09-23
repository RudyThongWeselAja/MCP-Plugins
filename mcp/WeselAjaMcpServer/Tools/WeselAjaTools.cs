using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using WeselAjaMcpServer.Client;
using WeselAjaMcpServer.Clients;
using WeselAjaMcpServer.Models;

namespace WeselAjaMcpServer.Tools;

[McpServerToolType]
public static class WeselAjaTools
{
    [McpServerTool(
        Name = "is_weselaja_active")]
    [Description(
        "Checks whether WeselAja is active using the selected implementation.")]
    public static async Task<string> IsActive(
        [Description(
            "Implementation to use: CSharp, PHP, WooCommerce, Python, or Odoo")]
        WeselAjaImplementation implementation,

        CSharpMcpClient csharpClient,
        PhpWeselAjaClient phpClient,
        WooCommerceWeselAjaClient wooCommerceClient,
        PythonWeselAjaClient pythonClient,
        OdooWeselAjaClient odooClient,

        CancellationToken cancellationToken = default)
    {
        var result =
            await CallImplementation(
                implementation,
                "is_weselaja_active",
                null,
                csharpClient,
                phpClient,
                wooCommerceClient,
                pythonClient,
                odooClient,
                cancellationToken);

        return NormalizeActiveResult(result);
    }

    [McpServerTool(
        Name = "get_weselaja_payment_method")]
    [Description(
        "Returns WeselAja payment method information using the selected implementation.")]
    public static async Task<string> GetPaymentMethod(
        [Description(
            "Implementation to use: CSharp, PHP, WooCommerce, Python, or Odoo")]
        WeselAjaImplementation implementation,

        CSharpMcpClient csharpClient,
        PhpWeselAjaClient phpClient,
        WooCommerceWeselAjaClient wooCommerceClient,
        PythonWeselAjaClient pythonClient,
        OdooWeselAjaClient odooClient,

        CancellationToken cancellationToken = default)
    {
        var result =
            await CallImplementation(
                implementation,
                "get_weselaja_payment_method",
                null,
                csharpClient,
                phpClient,
                wooCommerceClient,
                pythonClient,
                odooClient,
                cancellationToken);

        return NormalizePaymentMethodResult(result);
    }

    [McpServerTool(
        Name = "create_weselaja_payment")]
    [Description(
        "Creates a WeselAja payment using the selected implementation.")]
    public static async Task<string> CreatePayment(
        [Description(
            "Implementation to use: CSharp, PHP, WooCommerce, Python, or Odoo")]
        WeselAjaImplementation implementation,

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
        PhpWeselAjaClient phpClient,
        WooCommerceWeselAjaClient wooCommerceClient,
        PythonWeselAjaClient pythonClient,
        OdooWeselAjaClient odooClient,

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
                "create_weselaja_payment",
                arguments,
                csharpClient,
                phpClient,
                wooCommerceClient,
                pythonClient,
                odooClient,
                cancellationToken);

        return NormalizePaymentResult(result);
    }

    private static async Task<string> CallImplementation(
        WeselAjaImplementation implementation,
        string tool,
        IReadOnlyDictionary<string, object?>? arguments,
        CSharpMcpClient csharpClient,
        PhpWeselAjaClient phpClient,
        WooCommerceWeselAjaClient wooCommerceClient,
        PythonWeselAjaClient pythonClient,
        OdooWeselAjaClient odooClient,
        CancellationToken cancellationToken)
    {
        var internalTool =
            GetInternalToolName(tool);

        switch (implementation)
        {
            case WeselAjaImplementation.CSharp:
                {
                    CallToolResult result;

                    if (arguments is null)
                    {
                        result =
                            await csharpClient.Client.CallToolAsync(
                                internalTool,
                                cancellationToken:
                                    cancellationToken);
                    }
                    else
                    {
                        result =
                            await csharpClient.Client.CallToolAsync(
                                internalTool,
                                arguments,
                                cancellationToken:
                                    cancellationToken);
                    }

                    return ExtractCSharpResult(result);
                }

            case WeselAjaImplementation.PHP:
                {
                    var result =
                        await phpClient.CallAsync(
                            internalTool,
                            arguments,
                            cancellationToken);

                    return result.GetRawText();
                }

            case WeselAjaImplementation.WooCommerce:
                {
                    var result =
                        await wooCommerceClient.CallAsync(
                            internalTool,
                            arguments,
                            cancellationToken);

                    return result.GetRawText();
                }

            case WeselAjaImplementation.Python:
                {
                    var result =
                        await pythonClient.CallAsync(
                            internalTool,
                            arguments,
                            cancellationToken);

                    return result.GetRawText();
                }

            case WeselAjaImplementation.Odoo:
                {
                    var result =
                        await odooClient.CallAsync(
                            internalTool,
                            arguments,
                            cancellationToken);

                    return result.GetRawText();
                }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(implementation),
                    implementation,
                    "Unsupported payment implementation.");
        }
    }

    private static string GetInternalToolName(
        string tool)
    {
        return tool;
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

        if (
            result.StructuredContent
            is JsonElement structured)
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

        if (
            root.ValueKind ==
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

        if (
            root.TryGetProperty(
                "active",
                out var activeProperty) &&
            (
                activeProperty.ValueKind ==
                    JsonValueKind.True ||
                activeProperty.ValueKind ==
                    JsonValueKind.False
            ))
        {
            active =
                activeProperty.GetBoolean();
        }

        var success = true;

        if (
            root.TryGetProperty(
                "success",
                out var successProperty) &&
            (
                successProperty.ValueKind ==
                    JsonValueKind.True ||
                successProperty.ValueKind ==
                    JsonValueKind.False
            ))
        {
            success =
                successProperty.GetBoolean();
        }

        string? error = null;

        if (
            root.TryGetProperty(
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

        if (
            root.TryGetProperty(
                "success",
                out var successProperty) &&
            (
                successProperty.ValueKind ==
                    JsonValueKind.True ||
                successProperty.ValueKind ==
                    JsonValueKind.False
            ))
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

        if (
            root.TryGetProperty(
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

        if (
            root.TryGetProperty(
                "success",
                out var successProperty) &&
            (
                successProperty.ValueKind ==
                    JsonValueKind.True ||
                successProperty.ValueKind ==
                    JsonValueKind.False
            ))
        {
            success =
                successProperty.GetBoolean();
        }
        else if (
            root.TryGetProperty(
                "isSuccess",
                out var isSuccessProperty) &&
            (
                isSuccessProperty.ValueKind ==
                    JsonValueKind.True ||
                isSuccessProperty.ValueKind ==
                    JsonValueKind.False
            ))
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

        if (
            root.TryGetProperty(
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
            if (
                !root.TryGetProperty(
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
            if (
                root.TryGetProperty(
                    propertyName,
                    out var property))
            {
                return property.Clone();
            }
        }

        return null;
    }
}