using System.ComponentModel;
using System.Text;
using System.Text.Json;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using WeselAjaMcpServer.Clients;

namespace WeselAjaMcpServer.Tools;

[McpServerToolType]
public static class WeselAjaTools
{
    [McpServerTool(
        Name = "is_weselaja_active")]
    [Description(
        "Checks whether the WeselAja payment integration is currently active and available. " +
        "Use this tool only when the user asks whether WeselAja is active, enabled, available, or ready to use. " +
        "Do not use this tool to list payment methods or create a payment.")]
    public static async Task<string> IsActive(
        CSharpMcpClient csharpClient,
        CancellationToken cancellationToken = default)
    {
        var result =
            await csharpClient.Client.CallToolAsync(
                "is_weselaja_active",
                cancellationToken:
                    cancellationToken);

        return ExtractCSharpResult(result);
    }

    [McpServerTool(
        Name = "get_weselaja_payment_method")]
    [Description(
        "Returns the payment methods and payment channels available for WeselAja. " +
        "Use this tool when the user asks which banks, payment methods, or payment channels are available. " +
        "Currently supported virtual account banks are BRI, Mandiri, BNI, Permata, CIMB, and BCA. " +
        "Do not use this tool to create a payment.")]
    public static async Task<string> GetPaymentMethod(
        CSharpMcpClient csharpClient,
        CancellationToken cancellationToken = default)
    {
        var result =
            await csharpClient.Client.CallToolAsync(
                "get_weselaja_payment_method",
                cancellationToken:
                    cancellationToken);

        return ExtractCSharpResult(result);
    }

    [McpServerTool(
        Name = "create_weselaja_payment")]
    [Description(
        "Creates a real payment transaction through the WeselAja payment gateway. " +
        "Use this tool when the user explicitly asks to create, initiate, start, or make a payment. " +
        "Required user inputs are the payment amount, customer name, and bank. " +
        "The supported banks are BRI, Mandiri, BNI, Permata, CIMB, and BCA. " +
        "The MCP server automatically maps the selected bank to the correct WeselAja payment channel: " +
        "BRI -> BRI.VA, Mandiri -> MDR.VA, BNI -> BNI.VA, Permata -> PTB.VA, CIMB -> CIMB.VA, BCA -> BCA.VA. " +
        "The customer reference is configured internally and does not need to be provided by the user. " +
        "Currency, payment method, callback URL, and redirect URL are configured server-side. " +
        "If the user does not specify a bank, ask which supported bank they want to use before creating the payment. " +
        "Do not use this tool for status checks or for listing payment methods.")]
    public static async Task<string> CreatePayment(
        [Description(
            "Payment amount in the smallest currency unit. " +
            "For example, 10000 means IDR 10,000.")]
        int amount,

        [Description(
            "Customer name or the person/company the payment is for.")]
        string customerName,

        [Description(
            "Bank to use for the virtual account payment. " +
            "Supported banks: BRI, Mandiri, BNI, Permata, CIMB, BCA. " +
            "The user may say the bank name naturally, such as 'Bank Mandiri' or 'CIMB Niaga'.")]
        string bank,

        CSharpMcpClient csharpClient,

        [Description(
            "Optional customer phone number.")]
        string customerPhoneNumber = "",

        [Description(
            "Optional description of the payment.")]
        string description = "",

        [Description(
            "Optional merchant reference code. " +
            "When omitted, one is generated automatically.")]
        string referenceCode = "",

        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return Error(
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            return Error(
                "Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(bank))
        {
            return Error(
                "Bank is required. Supported banks: BRI, Mandiri, BNI, Permata, CIMB, and BCA.");
        }

        var paymentChannel =
            ResolvePaymentChannel(bank);

        if (paymentChannel is null)
        {
            return Error(
                $"Unsupported bank '{bank}'. " +
                "Supported banks are BRI, Mandiri, BNI, Permata, CIMB, and BCA.");
        }

        var currency =
            GetEnvironmentValue(
                "WESELAJA_CURRENCY",
                "IDR");

        var paymentMethod =
            GetEnvironmentValue(
                "WESELAJA_PAYMENT_METHOD",
                "VIRTUAL_ACCOUNT");

        var callbackUrl =
            Environment.GetEnvironmentVariable(
                "WESELAJA_CALLBACK_URL")
            ?.Trim();

        var redirectUrl =
            Environment.GetEnvironmentVariable(
                "WESELAJA_REDIRECT_URL")
            ?.Trim();

        if (string.IsNullOrWhiteSpace(
                callbackUrl))
        {
            return Error(
                "WESELAJA_CALLBACK_URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(
                redirectUrl))
        {
            return Error(
                "WESELAJA_REDIRECT_URL is not configured.");
        }

        if (!Uri.TryCreate(
                callbackUrl,
                UriKind.Absolute,
                out var callbackUri) ||
            callbackUri.Scheme != Uri.UriSchemeHttp &&
            callbackUri.Scheme != Uri.UriSchemeHttps)
        {
            return Error(
                "WESELAJA_CALLBACK_URL must be a valid HTTP or HTTPS URL.");
        }

        if (!Uri.TryCreate(
                redirectUrl,
                UriKind.Absolute,
                out var redirectUri) ||
            redirectUri.Scheme != Uri.UriSchemeHttp &&
            redirectUri.Scheme != Uri.UriSchemeHttps)
        {
            return Error(
                "WESELAJA_REDIRECT_URL must be a valid HTTP or HTTPS URL.");
        }

        var generatedReference =
            string.IsNullOrWhiteSpace(referenceCode)
                ? GenerateReferenceCode(
                    customerName)
                : referenceCode.Trim();

        const string customerReference =
            "LAMPUNGDEV123456789";

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
                    generatedReference,

                ["customerReference"] =
                    customerReference,

                ["customerName"] =
                    customerName.Trim(),

                ["customerPhoneNumber"] =
                    string.IsNullOrWhiteSpace(
                        customerPhoneNumber)
                        ? null
                        : customerPhoneNumber.Trim(),

                ["description"] =
                    string.IsNullOrWhiteSpace(
                        description)
                        ? null
                        : description.Trim(),

                ["callbackUrl"] =
                    callbackUrl,

                ["redirectUrl"] =
                    redirectUrl
            };

        try
        {
            var result =
                await csharpClient.Client.CallToolAsync(
                    "create_weselaja_payment",
                    arguments,
                    cancellationToken:
                        cancellationToken);

            Console.Error.WriteLine();

            Console.Error.WriteLine(
                "========== WESELAJA MCP PAYMENT DEBUG ==========");

            Console.Error.WriteLine(
                $"Bank         : {NormalizeBankDisplayName(bank)}");

            Console.Error.WriteLine(
                $"Payment Chan.: {paymentChannel}");

            Console.Error.WriteLine(
                $"Callback URL : {callbackUrl}");

            Console.Error.WriteLine(
                $"Redirect URL : {redirectUrl}");

            Console.Error.WriteLine(
                $"Reference    : {generatedReference}");

            Console.Error.WriteLine(
                $"Customer Ref : {customerReference}");

            Console.Error.WriteLine(
                "================================================");

            Console.Error.WriteLine();

            return ExtractCSharpResult(
                result);
        }
        catch (OperationCanceledException)
        {
            return Error(
                "WeselAja payment request was cancelled.");
        }
        catch (Exception exception)
        {
            return Error(
                $"WeselAja payment failed: {exception.Message}");
        }
    }

    private static string? ResolvePaymentChannel(
        string bank)
    {
        var normalized =
            NormalizeBankName(bank);

        return normalized switch
        {
            "BRI" =>
                "BRI.VA",

            "MANDIRI" =>
                "MDR.VA",

            "BNI" =>
                "BNI.VA",

            "PERMATA" =>
                "PTB.VA",

            "CIMB" =>
                "CIMB.VA",

            "BCA" =>
                "BCA.VA",

            _ =>
                null
        };
    }

    private static string NormalizeBankName(
        string bank)
    {
        var value =
            bank
                .Trim()
                .ToUpperInvariant();

        value =
            value
                .Replace("BANK ", "")
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");

        return value switch
        {
            "BRI" =>
                "BRI",

            "BANKBRI" =>
                "BRI",

            "MANDIRI" =>
                "MANDIRI",

            "BANKMANDIRI" =>
                "MANDIRI",

            "MDR" =>
                "MANDIRI",

            "BNI" =>
                "BNI",

            "BANKBNI" =>
                "BNI",

            "PERMATA" =>
                "PERMATA",

            "BANKPERMATA" =>
                "PERMATA",

            "PTB" =>
                "PERMATA",

            "CIMB" =>
                "CIMB",

            "CIMBNIAGA" =>
                "CIMB",

            "BANKCIMB" =>
                "CIMB",

            "BCA" =>
                "BCA",

            "BANKBCA" =>
                "BCA",

            _ =>
                value
        };
    }

    private static string NormalizeBankDisplayName(
        string bank)
    {
        return NormalizeBankName(bank) switch
        {
            "BRI" =>
                "BRI",

            "MANDIRI" =>
                "Mandiri",

            "BNI" =>
                "BNI",

            "PERMATA" =>
                "Permata",

            "CIMB" =>
                "CIMB",

            "BCA" =>
                "BCA",

            _ =>
                bank.Trim()
        };
    }

    private static string GenerateReferenceCode(
        string customerName)
    {
        var normalized =
            RemoveInvalidReferenceCharacters(
                customerName);

        if (string.IsNullOrWhiteSpace(
                normalized))
        {
            normalized = "CUSTOMER";
        }

        if (normalized.Length > 20)
        {
            normalized =
                normalized[..20];
        }

        var timestamp =
            DateTime.UtcNow.ToString(
                "yyyyMMddHHmmss");

        return
            $"MCP-{normalized}-{timestamp}";
    }

    private static string
        RemoveInvalidReferenceCharacters(
            string value)
    {
        var builder =
            new StringBuilder();

        foreach (
            var character in
            value.ToUpperInvariant())
        {
            if (
                char.IsLetterOrDigit(
                    character) ||
                character == '-' ||
                character == '_')
            {
                builder.Append(
                    character);
            }
        }

        return builder.ToString();
    }

    private static string GetEnvironmentValue(
        string key,
        string defaultValue)
    {
        var value =
            Environment.GetEnvironmentVariable(
                key);

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();
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

    private static string Error(
        string message)
    {
        return JsonSerializer.Serialize(
            new
            {
                success = false,
                error = message
            },
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }
}