using System.ComponentModel;
using ModelContextProtocol.Server;
using WeselAjaMcp.Models;
using WeselAjaMcp.Services;

namespace WeselAjaMcp.Tools;

[McpServerToolType]
public static class WeselAjaTools
{
    [McpServerTool(
        Name = "is_weselaja_active",
        UseStructuredContent = true)]
    [Description(
        "Checks whether the WeselAja payment integration is enabled and active. " +
        "Use this only for service availability or activation checks.")]
    public static bool IsActive(
        WeselAjaBlocksService service)
    {
        return service.IsActive();
    }

    [McpServerTool(
        Name = "get_weselaja_payment_method",
        UseStructuredContent = true)]
    [Description(
        "Returns the WeselAja payment method configuration, including the methods/channels currently available to the integration. " +
        "Use this for payment-method availability questions.")]
    public static PaymentMethodInfo GetPaymentMethodInfo(
        WeselAjaBlocksService service)
    {
        return service.GetPaymentMethodInfo();
    }

    [McpServerTool(
        Name = "create_weselaja_payment",
        UseStructuredContent = true)]
    [Description(
        "Creates a new payment transaction through the WeselAja API using the supplied amount, customer, method, channel, and callback details.")]
    public static async Task<CreatePaymentResponse> CreatePayment(
        [Description("Payment amount")]
        int amount,

        [Description("Currency code, for example IDR")]
        string currency,

        [Description(
            "Payment method, for example VIRTUAL_ACCOUNT")]
        string paymentMethod,

        [Description(
            "Payment channel, for example BCA.VA")]
        string paymentChannel,

        [Description("Unique merchant reference code")]
        string referenceCode,

        [Description("Customer reference identifier")]
        string customerReference,

        [Description("Customer full name")]
        string customerName,

        [Description(
            "URL called by WeselAja after payment updates")]
        string callbackUrl,

        [Description(
            "URL where the customer is redirected after payment")]
        string redirectUrl,

        WeselAjaClient client,

        [Description("Customer phone number")]
        string customerPhoneNumber = "",

        [Description("Payment description")]
        string description = "",

        CancellationToken cancellationToken = default)
    {
        var request = new CreatePaymentRequest
        {
            InitiatedAmount = amount,
            Currency = currency,
            PaymentMethod = paymentMethod,
            PaymentChannel = paymentChannel,

            CustomerPhoneNumber =
                string.IsNullOrWhiteSpace(customerPhoneNumber)
                    ? null
                    : customerPhoneNumber,

            ReferenceCode = referenceCode,
            CustomerReference = customerReference,
            CustomerName = customerName,

            Description =
                string.IsNullOrWhiteSpace(description)
                    ? null
                    : description,

            CallbackUrl = callbackUrl,
            RedirectUrl = redirectUrl
        };

        return await client.CreatePaymentAsync(
            request,
            cancellationToken);
    }
}