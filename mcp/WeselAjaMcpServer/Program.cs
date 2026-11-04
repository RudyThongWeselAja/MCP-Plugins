
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using DotNetEnv;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

using WeselAjaMcp.Configuration;
using WeselAjaMcp.Security;
using WeselAjaMcp.Services;

var envPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    ".env"
);

if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var transportMode = (
    Environment.GetEnvironmentVariable("MCP_TRANSPORT")
    ?? "stdio"
).Trim().ToLowerInvariant();

if (transportMode == "http")
{
    await RunHttpServer(args);
}
else
{
    await RunStdioServer(args);
}

static async Task RunStdioServer(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);

    ConfigureWeselAjaServices(builder.Services);

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    Console.Error.WriteLine("WeselAja MCP Server");
    Console.Error.WriteLine("Transport : STDIO");
    Console.Error.WriteLine("Status    : Ready");

    await builder.Build().RunAsync();
}

static async Task RunHttpServer(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureWeselAjaServices(builder.Services);

    builder.Services
        .AddMcpServer()
        .WithHttpTransport(options =>
        {
            options.SessionMode =
                HttpServerSessionMode.Stateless;
        })
        .WithToolsFromAssembly();

    var app = builder.Build();

    var accessToken = Environment
        .GetEnvironmentVariable("MCP_ACCESS_TOKEN")
        ?.Trim();

    if (string.IsNullOrWhiteSpace(accessToken))
    {
        throw new InvalidOperationException(
            "MCP_ACCESS_TOKEN environment variable is required."
        );
    }

    var expectedTokenBytes =
        Encoding.UTF8.GetBytes(accessToken);

    app.Use(async (context, next) =>
    {
        var isMcpEndpoint =
            context.Request.Path.StartsWithSegments(
                "/mcp",
                StringComparison.OrdinalIgnoreCase
            );

        if (!isMcpEndpoint)
        {
            await next();
            return;
        }

        const string bearerPrefix = "Bearer ";

        var authorization = context.Request
            .Headers["Authorization"]
            .ToString();

        if (!authorization.StartsWith(
            bearerPrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            context.Response.Headers["WWW-Authenticate"] =
                "Bearer";

            await context.Response.WriteAsync(
                "Unauthorized"
            );

            return;
        }

        var providedToken = authorization[
            bearerPrefix.Length..].Trim();

        var providedTokenBytes =
            Encoding.UTF8.GetBytes(providedToken);

        var isValid =
            providedTokenBytes.Length ==
                expectedTokenBytes.Length
            &&
            CryptographicOperations.FixedTimeEquals(
                providedTokenBytes,
                expectedTokenBytes
            );

        if (!isValid)
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            context.Response.Headers["WWW-Authenticate"] =
                "Bearer";

            await context.Response.WriteAsync(
                "Unauthorized"
            );

            return;
        }

        await next();
    });

    app.MapMcp("/mcp");

    app.MapGet(
        "/health",
        () => Results.Ok(new
        {
            success = true,
            service = "WeselAja MCP",
            transport = "Streamable HTTP"
        })
    );

    var httpUrl = (
        Environment.GetEnvironmentVariable("MCP_HTTP_URL")
        ?? "http://0.0.0.0:3001"
    ).Trim();

    Console.Error.WriteLine("WeselAja MCP Server");
    Console.Error.WriteLine("Transport : Streamable HTTP");
    Console.Error.WriteLine(
        $"Endpoint  : {httpUrl.TrimEnd('/')}/mcp"
    );
    Console.Error.WriteLine(
        $"Health    : {httpUrl.TrimEnd('/')}/health"
    );

    await app.RunAsync(httpUrl);
}

static void ConfigureWeselAjaServices(
    IServiceCollection services)
{
    var enabledValue =
        Environment.GetEnvironmentVariable("XENITH_ENABLED");

    var options = new WeselAjaOptions
    {
        Enabled = !string.Equals(
            enabledValue?.Trim(),
            "false",
            StringComparison.OrdinalIgnoreCase
        ),

        Title = (
            Environment.GetEnvironmentVariable("XENITH_TITLE")
            ?? "WeselAja"
        ).Trim(),

        Description = (
            Environment.GetEnvironmentVariable("XENITH_DESCRIPTION")
            ?? "Pay securely using WeselAja payment gateway."
        ).Trim(),

        Icon = "",

        ApiKey = (
            Environment.GetEnvironmentVariable("XENITH_API_KEY")
            ?? string.Empty
        ).Trim(),

        SecretKey = (
            Environment.GetEnvironmentVariable("XENITH_SECRET_KEY")
            ?? string.Empty
        ).Trim(),

        ApiUrl = (
            Environment.GetEnvironmentVariable("WESELAJA_API_URL")
            ?? "https://sandbox.checkout.weselaja.id"
        ).Trim()
    };

    services.AddSingleton(options);
    services.AddSingleton<SignatureGenerator>();

    services
        .AddHttpClient<WeselAjaClient>()
        .ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                UseProxy = false,
                AutomaticDecompression =
                    System.Net.DecompressionMethods.None,
                MaxConnectionsPerServer = 1
            });

    services.AddSingleton<WeselAjaBlocksService>();
}
