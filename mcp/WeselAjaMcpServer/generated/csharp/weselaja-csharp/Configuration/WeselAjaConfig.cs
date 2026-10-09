
using System;
using System.Collections.Generic;
using System.Linq;

namespace WeselAja.Integration.Configuration;

public sealed class WeselAjaConfig
{
    public string ApiUrl { get; init; } =
        "https://sandbox.checkout.weselaja.id";

    public string ApiKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string CustomerReference { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;
    public string RedirectUrl { get; init; } = string.Empty;

    public string Currency { get; init; } = "IDR";

    public string PaymentMethod { get; init; } =
        "VIRTUAL_ACCOUNT";

    public static WeselAjaConfig FromEnvironment()
    {
        EnvLoader.Load();

        var config = new WeselAjaConfig
        {
            ApiUrl = Get(
                "WESELAJA_API_URL",
                "https://sandbox.checkout.weselaja.id"),

            ApiKey = Get("WESELAJA_API_KEY"),
            SecretKey = Get("WESELAJA_SECRET_KEY"),

            CustomerReference = Get(
                "WESELAJA_CUSTOMER_REFERENCE"),

            CallbackUrl = Get("WESELAJA_CALLBACK_URL"),
            RedirectUrl = Get("WESELAJA_REDIRECT_URL"),

            Currency = Get("WESELAJA_CURRENCY", "IDR"),

            PaymentMethod = Get(
                "WESELAJA_PAYMENT_METHOD",
                "VIRTUAL_ACCOUNT")
        };

        config.Validate();

        return config;
    }

    public void Validate()
    {
        var required = new Dictionary<string, string>
        {
            ["WESELAJA_API_URL"] = ApiUrl,
            ["WESELAJA_API_KEY"] = ApiKey,
            ["WESELAJA_SECRET_KEY"] = SecretKey,
            ["WESELAJA_CUSTOMER_REFERENCE"] = CustomerReference,
            ["WESELAJA_CALLBACK_URL"] = CallbackUrl,
            ["WESELAJA_REDIRECT_URL"] = RedirectUrl,
            ["WESELAJA_CURRENCY"] = Currency,
            ["WESELAJA_PAYMENT_METHOD"] = PaymentMethod
        };

        var missing = required
            .Where(pair => string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => pair.Key)
            .ToArray();

        if (missing.Length > 0)
        {
            throw new WeselAjaConfigurationException(
                "Missing configuration: " +
                string.Join(", ", missing));
        }

        if (!IsHttpUrl(ApiUrl))
        {
            throw new WeselAjaConfigurationException(
                "WESELAJA_API_URL must be a valid HTTP or HTTPS URL.");
        }

        if (!IsHttpUrl(CallbackUrl))
        {
            throw new WeselAjaConfigurationException(
                "WESELAJA_CALLBACK_URL must be a valid HTTP or HTTPS URL.");
        }

        if (!IsHttpUrl(RedirectUrl))
        {
            throw new WeselAjaConfigurationException(
                "WESELAJA_REDIRECT_URL must be a valid HTTP or HTTPS URL.");
        }
    }

    private static string Get(
        string key,
        string fallback = "")
    {
        var value = Environment.GetEnvironmentVariable(key);

        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(
                   value,
                   UriKind.Absolute,
                   out var uri)
               &&
               (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps);
    }
}
