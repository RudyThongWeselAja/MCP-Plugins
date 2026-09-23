using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeselAjaMcp.Configuration;
using WeselAjaMcp.Security;
using WeselAjaMcp.Services;

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
        enabledValue?.Trim(),
        "false",
        StringComparison.OrdinalIgnoreCase);

var options =
    new WeselAjaOptions
    {
        Enabled =
            enabled,

        Title =
            (
                Environment.GetEnvironmentVariable(
                    "XENITH_TITLE")
                ?? "XenithPay"
            ).Trim(),

        Description =
            (
                Environment.GetEnvironmentVariable(
                    "XENITH_DESCRIPTION")
                ?? "Pay securely using XenithPay payment gateway."
            ).Trim(),

        Icon =
            "",

        ApiKey =
            apiKeyValue?.Trim()
            ?? string.Empty,

        SecretKey =
            secretKeyValue?.Trim()
            ?? string.Empty,

        ApiUrl =
            (
                weselAjaUrlValue?.Trim()
                ?? "https://sandbox.checkout.weselaja.id"
            ).Trim()
    };

builder.Services.AddSingleton(
    options);

builder.Services.AddSingleton<
    SignatureGenerator>();

builder.Services
    .AddHttpClient<WeselAjaClient>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            UseProxy =
                false,

            AutomaticDecompression =
                System.Net.DecompressionMethods.None,

            MaxConnectionsPerServer =
                1
        });

builder.Services.AddSingleton<
    WeselAjaBlocksService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder
    .Build()
    .RunAsync();
