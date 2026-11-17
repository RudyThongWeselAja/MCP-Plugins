using System.Text.Json.Serialization;

namespace WeselAjaMcp.Models;

public class CreatePaymentRequest
{
    [JsonPropertyName("initiatedAmount")]
    public int InitiatedAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [JsonPropertyName("paymentChannel")]
    public string PaymentChannel { get; set; } = string.Empty;

    [JsonPropertyName("customerPhoneNumber")]
    public string? CustomerPhoneNumber { get; set; }

    [JsonPropertyName("referenceCode")]
    public string ReferenceCode { get; set; } = string.Empty;

    [JsonPropertyName("customerReference")]
    public string CustomerReference { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("callbackUrl")]
    public string CallbackUrl { get; set; } = string.Empty;

    [JsonPropertyName("redirectUrl")]
    public string RedirectUrl { get; set; } = string.Empty;
}