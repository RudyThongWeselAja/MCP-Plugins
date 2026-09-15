using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using XenithPayMcp.Configuration;
using XenithPayMcp.Security;
using XenithPayMcp.Services;

var builder =
    Host.CreateApplicationBuilder(args);

var enabledValue =
    Environment.GetEnvironmentVariable(
        "XENITH_ENABLED");

var apiKeyValue =
    Environment.GetEnvironmentVariable(
        "XENITH_API_KEY");

var secretKeyValue =
    Environment.GetEnvironmentVariable(
        "XENITH_SECRET_KEY");

var weselAjaUrlValue =
    Environment.GetEnvironmentVariable(
        "WESELAJA_API_URL");

var enabled =
    !string.Equals(
        enabledValue,
        "false",
        StringComparison.OrdinalIgnoreCase);

var options =
    new XenithPayOptions
    {
        Enabled =
            enabled,

        Title =
            Environment.GetEnvironmentVariable(
                "XENITH_TITLE")
            ?? "XenithPay",

        Description =
            Environment.GetEnvironmentVariable(
                "XENITH_DESCRIPTION")
            ?? "Pay securely using XenithPay payment gateway.",

        Icon =
            "",

        ApiKey =
            apiKeyValue
            ?? string.Empty,

        SecretKey =
            secretKeyValue
            ?? string.Empty,

        ApiUrl =
            weselAjaUrlValue
            ?? "https://sandbox.checkout.weselaja.id"
    };

builder.Services.AddSingleton(
    options);

builder.Services.AddSingleton<
    SignatureGenerator>();

builder.Services
    .AddHttpClient<XenithPayClient>();

builder.Services.AddSingleton<
    XenithPayBlocksService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder
    .Build()
    .RunAsync();