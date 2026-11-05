using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ModelContextProtocol.Server;

using WeselAjaMcp.Models;
using WeselAjaMcp.Services;

namespace WeselAjaMcpServer.Tools;

[McpServerToolType]
public static class WeselAjaTools
{
    private const string CustomerReference =
        "LAMPUNGDEV123456789";

    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    [McpServerTool(Name = "is_weselaja_active")]
    [Description(
        "Checks whether the WeselAja payment integration is active and enabled. " +
        "Use this tool for availability or activation checks.")]
    public static string IsActive(
        WeselAjaBlocksService service)
    {
        return JsonSerializer.Serialize(
            service.IsActive(),
            SerializerOptions
        );
    }

    [McpServerTool(Name = "get_weselaja_payment_method")]
    [Description(
        "Returns the configured WeselAja payment method and supported virtual account banks. " +
        "Supported banks: BRI, Mandiri, BNI, Permata, CIMB, and BCA.")]
    public static string GetPaymentMethod(
        WeselAjaBlocksService service)
    {
        var info = service.GetPaymentMethodInfo();

        var result = new
        {
            success = true,
            title = info.Title,
            description = info.Description,
            icon = info.Icon,
            supports = info.Supports,

            paymentMethod = GetEnvironmentValue(
                "WESELAJA_PAYMENT_METHOD",
                "VIRTUAL_ACCOUNT"
            ),

            paymentChannels = new[]
            {
                new { bank = "BRI", channel = "BRI.VA" },
                new { bank = "Mandiri", channel = "MDR.VA" },
                new { bank = "BNI", channel = "BNI.VA" },
                new { bank = "Permata", channel = "PTB.VA" },
                new { bank = "CIMB", channel = "CIMB.VA" },
                new { bank = "BCA", channel = "BCA.VA" }
            }
        };

        return JsonSerializer.Serialize(
            result,
            SerializerOptions
        );
    }

    [McpServerTool(Name = "create_weselaja_payment")]
    [Description(
        "Creates a real payment transaction through the configured WeselAja API. " +
        "Required inputs are amount, customer name, and bank. " +
        "Supported banks: BRI, Mandiri, BNI, Permata, CIMB, and BCA. " +
        "Currency, payment method, customer reference, callback URL, and redirect URL " +
        "are configured server-side. Ask for the bank if it has not been specified.")]
    public static async Task<string> CreatePayment(
        [Description(
            "Payment amount in the smallest currency unit. For example, 10000 means IDR 10,000.")]
        int amount,

        [Description(
            "Customer name or the person/company the payment is for.")]
        string customerName,

        [Description(
            "Bank to use. Supported values include BRI, Mandiri, BNI, Permata, CIMB, and BCA.")]
        string bank,

        WeselAjaClient client,

        [Description("Optional customer phone number.")]
        string customerPhoneNumber = "",

        [Description("Optional payment description.")]
        string description = "",

        [Description("Optional merchant reference code. Generated automatically if omitted.")]
        string referenceCode = "",

        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return Error("Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            return Error("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(bank))
        {
            return Error(
                "Bank is required. Supported banks: BRI, Mandiri, BNI, Permata, CIMB, and BCA."
            );
        }

        var paymentChannel = ResolvePaymentChannel(bank);

        if (paymentChannel is null)
        {
            return Error(
                $"Unsupported bank '{bank}'. " +
                "Supported banks are BRI, Mandiri, BNI, Permata, CIMB, and BCA."
            );
        }

        var callbackUrl = Environment
            .GetEnvironmentVariable("WESELAJA_CALLBACK_URL")
            ?.Trim();

        var redirectUrl = Environment
            .GetEnvironmentVariable("WESELAJA_REDIRECT_URL")
            ?.Trim();

        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            return Error("WESELAJA_CALLBACK_URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return Error("WESELAJA_REDIRECT_URL is not configured.");
        }

        if (!IsHttpUrl(callbackUrl))
        {
            return Error(
                "WESELAJA_CALLBACK_URL must be a valid HTTP or HTTPS URL."
            );
        }

        if (!IsHttpUrl(redirectUrl))
        {
            return Error(
                "WESELAJA_REDIRECT_URL must be a valid HTTP or HTTPS URL."
            );
        }

        var generatedReference = string.IsNullOrWhiteSpace(referenceCode)
            ? GenerateReferenceCode(customerName)
            : referenceCode.Trim();

        var request = new CreatePaymentRequest
        {
            InitiatedAmount = amount,

            Currency = GetEnvironmentValue(
                "WESELAJA_CURRENCY",
                "IDR"
            ),

            PaymentMethod = GetEnvironmentValue(
                "WESELAJA_PAYMENT_METHOD",
                "VIRTUAL_ACCOUNT"
            ),

            PaymentChannel = paymentChannel,
            ReferenceCode = generatedReference,
            CustomerReference = CustomerReference,
            CustomerName = customerName.Trim(),

            CustomerPhoneNumber =
                string.IsNullOrWhiteSpace(customerPhoneNumber)
                    ? null
                    : customerPhoneNumber.Trim(),

            Description =
                string.IsNullOrWhiteSpace(description)
                    ? null
                    : description.Trim(),

            CallbackUrl = callbackUrl,
            RedirectUrl = redirectUrl
        };

        try
        {
            var response = await client.CreatePaymentAsync(
                request,
                cancellationToken
            );

            Console.Error.WriteLine("WeselAja payment request");
            Console.Error.WriteLine($"Bank         : {NormalizeBankDisplayName(bank)}");
            Console.Error.WriteLine($"Payment Chan.: {paymentChannel}");
            Console.Error.WriteLine($"Callback URL : {callbackUrl}");
            Console.Error.WriteLine($"Redirect URL : {redirectUrl}");
            Console.Error.WriteLine($"Reference    : {generatedReference}");
            Console.Error.WriteLine($"Customer Ref : {CustomerReference}");

            return JsonSerializer.Serialize(
                response,
                SerializerOptions
            );
        }
        catch (OperationCanceledException)
        {
            return Error("WeselAja payment request was cancelled.");
        }
        catch (Exception exception)
        {
            return Error(
                $"WeselAja payment failed: {exception.Message}"
            );
        }
    }

    private static string? ResolvePaymentChannel(string bank)
    {
        return NormalizeBankName(bank) switch
        {
            "BRI" => "BRI.VA",
            "MANDIRI" => "MDR.VA",
            "BNI" => "BNI.VA",
            "PERMATA" => "PTB.VA",
            "CIMB" => "CIMB.VA",
            "BCA" => "BCA.VA",
            _ => null
        };
    }

    private static string NormalizeBankName(string bank)
    {
        var value = bank
            .Trim()
            .ToUpperInvariant()
            .Replace("BANK ", "")
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        return value switch
        {
            "BRI" or "BANKBRI" => "BRI",
            "MANDIRI" or "BANKMANDIRI" or "MDR" => "MANDIRI",
            "BNI" or "BANKBNI" => "BNI",
            "PERMATA" or "BANKPERMATA" or "PTB" => "PERMATA",
            "CIMB" or "CIMBNIAGA" or "BANKCIMB" => "CIMB",
            "BCA" or "BANKBCA" => "BCA",
            _ => value
        };
    }

    private static string NormalizeBankDisplayName(string bank)
    {
        return NormalizeBankName(bank) switch
        {
            "BRI" => "BRI",
            "MANDIRI" => "Mandiri",
            "BNI" => "BNI",
            "PERMATA" => "Permata",
            "CIMB" => "CIMB",
            "BCA" => "BCA",
            _ => bank.Trim()
        };
    }

    private static string GenerateReferenceCode(string customerName)
    {
        var normalized = RemoveInvalidReferenceCharacters(
            customerName
        );

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "CUSTOMER";
        }

        if (normalized.Length > 20)
        {
            normalized = normalized[..20];
        }

        return $"MCP-{normalized}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static string RemoveInvalidReferenceCharacters(string value)
    {
        var builder = new StringBuilder();

        foreach (var character in value.ToUpperInvariant())
        {
            if (
                char.IsLetterOrDigit(character)
                || character == '-'
                || character == '_')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri)
            && (
                uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps
            );
    }

    private static string GetEnvironmentValue(
        string key,
        string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();
    }

    private static string Error(string message)
    {
        return JsonSerializer.Serialize(
            new
            {
                success = false,
                error = message
            },
            SerializerOptions
        );
    }
}