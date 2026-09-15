using System.ComponentModel;
using ModelContextProtocol.Server;
using XenithPayMcp.Models;
using XenithPayMcp.Services;

namespace XenithPayMcp.Tools;

[McpServerToolType]
public static class XenithPayTools
{
    [McpServerTool(
        Name = "is_xenithpay_active",
        UseStructuredContent = true)]
    [Description(
        "Checks whether the XenithPay payment method is active.")]
    public static bool IsActive(
        XenithPayBlocksService service)
    {
        return service.IsActive();
    }

    [McpServerTool(
        Name = "get_xenithpay_payment_method",
        UseStructuredContent = true)]
    [Description(
        "Returns XenithPay payment method information.")]
    public static PaymentMethodInfo GetPaymentMethodInfo(
        XenithPayBlocksService service)
    {
        return service.GetPaymentMethodInfo();
    }

    [McpServerTool(
        Name = "create_xenithpay_payment",
        UseStructuredContent = true)]
    [Description(
        "Creates a new payment through the XenithPay API.")]
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
            "URL called by XenithPay after payment updates")]
        string callbackUrl,

        [Description(
            "URL where the customer is redirected after payment")]
        string redirectUrl,

        XenithPayClient client,

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
                string.IsNullOrWhiteSpace(
                    customerPhoneNumber)
                    ? null
                    : customerPhoneNumber,

            ReferenceCode = referenceCode,

            CustomerReference =
                customerReference,

            CustomerName =
                customerName,

            Description =
                string.IsNullOrWhiteSpace(
                    description)
                    ? null
                    : description,

            CallbackUrl =
                callbackUrl,

            RedirectUrl =
                redirectUrl
        };

        return await client.CreatePaymentAsync(
            request,
            cancellationToken);
    }
}