namespace WeselAja.Integration.Models;

public sealed class CreatePaymentRequest
{
    public int InitiatedAmount { get; set; }

    public string CustomerName { get; set; } =
        string.Empty;

    public string Bank { get; set; } =
        string.Empty;

    public string? CustomerPhoneNumber { get; set; }

    public string? Description { get; set; }

    public string? ReferenceCode { get; set; }
}