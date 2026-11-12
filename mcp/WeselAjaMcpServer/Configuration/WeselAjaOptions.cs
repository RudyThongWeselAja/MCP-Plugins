namespace WeselAjaMcp.Configuration;

public class WeselAjaOptions
{
    public bool Enabled { get; set; }

    public string Title { get; set; } = "WeselAja";

    public string Description { get; set; } =
        "Pay securely using WeselAja payment gateway.";

    public string Icon { get; set; } = "";

    public string ApiKey { get; set; } = "";

    public string SecretKey { get; set; } = "";

    public string ApiUrl { get; set; } =
        "https://sandbox.checkout.weselaja.id";
}