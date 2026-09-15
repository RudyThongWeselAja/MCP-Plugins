using System.Text.Json;
using System.Text.Json.Serialization;

namespace XenithPayMcp.Models;

public class CreatePaymentResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("initiatedAmount")]
    public decimal? InitiatedAmount { get; set; }

    [JsonPropertyName("paymentAmount")]
    public decimal? PaymentAmount { get; set; }

    [JsonPropertyName("feeAmount")]
    public decimal? FeeAmount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("paymentChannel")]
    public string? PaymentChannel { get; set; }

    [JsonPropertyName("paymentCode")]
    public string? PaymentCode { get; set; }

    [JsonPropertyName("paymentCodeType")]
    public string? PaymentCodeType { get; set; }

    [JsonPropertyName("referenceCode")]
    public string? ReferenceCode { get; set; }

    [JsonPropertyName("customerReference")]
    public string? CustomerReference { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("createdTime")]
    public string? CreatedTime { get; set; }

    [JsonPropertyName("updatedTime")]
    public string? UpdatedTime { get; set; }

    [JsonPropertyName("expirationTime")]
    public string? ExpirationTime { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("callbackUrl")]
    public string? CallbackUrl { get; set; }

    [JsonPropertyName("redirectUrl")]
    public string? RedirectUrl { get; set; }

    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    public bool IsSuccess =>
        !string.IsNullOrWhiteSpace(Id) &&
        !string.Equals(
            Status,
            "FAILED",
            StringComparison.OrdinalIgnoreCase);
}