namespace XenithPayMcp.Configuration;

public class XenithPayOptions
{
    public bool Enabled { get; set; }

    public string Title { get; set; } = "XenithPay";

    public string Description { get; set; } =
        "Pay securely using XenithPay payment gateway.";

    public string Icon { get; set; } = "";

    public string ApiKey { get; set; } = "";

    public string SecretKey { get; set; } = "";

    public string ApiUrl { get; set; } =
        "https://sandbox.checkout.weselaja.id";
}