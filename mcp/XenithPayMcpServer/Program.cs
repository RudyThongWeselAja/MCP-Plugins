using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using XenithPayMcpServer.Client;
using XenithPayMcpServer.Clients;

Env.Load(
    Path.Combine(
        Directory.GetCurrentDirectory(),
        ".env"
    )
);

var transportMode =
    (
        Environment.GetEnvironmentVariable(
            "MCP_TRANSPORT"
        )
        ?? "stdio"
    )
    .Trim()
    .ToLowerInvariant();

if (transportMode == "http")
{
    await RunHttpServer(args);
}
else
{
    await RunStdioServer(args);
}

static async Task RunStdioServer(
    string[] args)
{
    var builder =
        Host.CreateApplicationBuilder(args);

    var csharpProjectPath =
        GetCSharpProjectPath(
            builder.Environment.ContentRootPath
        );

    await RegisterCSharpClient(
        builder.Services,
        csharpProjectPath
    );

    RegisterOtherClients(
        builder.Services
    );

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "XenithPay MCP Server");
    Console.Error.WriteLine(
        "====================");
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "Transport : STDIO");
    Console.Error.WriteLine(
        "Status    : Ready");
    Console.Error.WriteLine();

    await builder
        .Build()
        .RunAsync();
}

static async Task RunHttpServer(
    string[] args)
{
    var builder =
        WebApplication.CreateBuilder(args);

    var csharpProjectPath =
        GetCSharpProjectPath(
            builder.Environment.ContentRootPath
        );

    await RegisterCSharpClient(
        builder.Services,
        csharpProjectPath
    );

    RegisterOtherClients(
        builder.Services
    );

    builder.Services
        .AddMcpServer()
        .WithHttpTransport(options =>
        {
            options.SessionMode =
                HttpServerSessionMode.Stateless;
        })
        .WithToolsFromAssembly();

    var app =
        builder.Build();

    app.MapMcp("/mcp");

    app.MapGet(
        "/health",
        () =>
            Results.Ok(
                new
                {
                    success = true,
                    service = "XenithPay MCP",
                    transport = "Streamable HTTP"
                }
            )
    );

    var httpUrl =
        (
            Environment.GetEnvironmentVariable(
                "MCP_HTTP_URL"
            )
            ?? "http://0.0.0.0:3001"
        )
        .Trim();

    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "XenithPay MCP Server");
    Console.Error.WriteLine(
        "====================");
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "Transport : Streamable HTTP");
    Console.Error.WriteLine(
        $"Endpoint  : {httpUrl.TrimEnd('/')}/mcp");
    Console.Error.WriteLine(
        $"Health    : {httpUrl.TrimEnd('/')}/health");
    Console.Error.WriteLine(
        "Status    : Ready");
    Console.Error.WriteLine();

    await app.RunAsync(
        httpUrl
    );
}

static string GetCSharpProjectPath(
    string contentRootPath)
{
    var configuredPath =
        Environment.GetEnvironmentVariable(
            "XENITH_CSHARP_MCP_PROJECT"
        );

    if (!string.IsNullOrWhiteSpace(
            configuredPath))
    {
        if (!File.Exists(
                configuredPath))
        {
            throw new FileNotFoundException(
                "C# MCP Server project was not found.",
                configuredPath
            );
        }

        return configuredPath;
    }

    var defaultPath =
        Path.GetFullPath(
            Path.Combine(
                contentRootPath,
                "..",
                "..",
                "csharp",
                "XenithPayMcp",
                "XenithPayMcp.csproj"
            )
        );

    if (!File.Exists(
            defaultPath))
    {
        throw new FileNotFoundException(
            "C# MCP Server project was not found.",
            defaultPath
        );
    }

    return defaultPath;
}

static async Task RegisterCSharpClient(
    IServiceCollection services,
    string csharpProjectPath)
{
    var csharpTransport =
        new StdioClientTransport(
            new StdioClientTransportOptions
            {
                Name =
                    "XenithPay-CSharp",

                Command =
                    "dotnet",

                Arguments =
                [
                    "run",
                    "--project",
                    csharpProjectPath,
                    "--no-launch-profile"
                ],

                WorkingDirectory =
                    Path.GetDirectoryName(
                        csharpProjectPath
                    )!,

                StandardErrorLines =
                    line =>
                    {
                        Console.Error.WriteLine(
                            line
                        );
                    }
            }
        );

    var csharpClient =
        await McpClient.CreateAsync(
            csharpTransport
        );

    var csharpMcpClient =
        new CSharpMcpClient(
            csharpClient
        );

    services.AddSingleton(
        csharpMcpClient
    );
}

static void RegisterOtherClients(
    IServiceCollection services)
{
    var phpClient =
        new PhpXenithPayClient();

    services.AddSingleton(
        phpClient
    );

    var pythonClient =
        new PythonXenithPayClient();

    services.AddSingleton(
        pythonClient
    );

    var odooHttpClient =
        new HttpClient();

    var odooClient =
        new OdooXenithPayClient(
            odooHttpClient
        );

    services.AddSingleton(
        odooClient
    );
}