namespace XenithPayMcp.Models;

public class PaymentMethodInfo
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string[] Supports { get; set; } = [];
}