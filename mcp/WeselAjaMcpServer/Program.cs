
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

using WeselAjaMcp.Configuration;
using WeselAjaMcpServer.Generators;
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
        var isProtectedEndpoint =
            context.Request.Path.StartsWithSegments(
                "/mcp",
                StringComparison.OrdinalIgnoreCase
            )
            ||
            context.Request.Path.StartsWithSegments(
                "/downloads",
                StringComparison.OrdinalIgnoreCase
            );

        if (!isProtectedEndpoint)
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

    app.MapGet(
        "/downloads/{requestId}/{target}/{projectName}",
        DownloadGeneratedProject
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
    Console.Error.WriteLine(
        $"Downloads : {httpUrl.TrimEnd('/')}/downloads"
    );

    await app.RunAsync(httpUrl);
}

static async Task<IResult> DownloadGeneratedProject(
    HttpContext context,
    string requestId,
    string target,
    string projectName,
    CancellationToken cancellationToken)
{
    static bool IsSafeSegment(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value != "."
            && value != ".."
            && value.All(c =>
                char.IsAsciiLetterOrDigit(c)
                || c == '-'
                || c == '_'
                || c == '.'
            );
    }

    if (!IsSafeSegment(requestId) ||
        !IsSafeSegment(target) ||
        !IsSafeSegment(projectName))
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Invalid download request."
        });
    }

    var generatedRoot =
        GeneratorOutputScope.GetDefaultRoot();

    if (!Directory.Exists(generatedRoot))
    {
        return Results.NotFound(new
        {
            success = false,
            message = "Generated directory not found."
        });
    }

    var requestDirectory = Path.GetFullPath(
        Path.Combine(
            generatedRoot,
            "requests",
            requestId)
    );

    var targetDirectory = Path.GetFullPath(
        Path.Combine(requestDirectory, target)
    );

    var projectDirectory = Path.GetFullPath(
        Path.Combine(targetDirectory, projectName)
    );

    var comparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    var requestPrefix =
        requestDirectory.TrimEnd(Path.DirectorySeparatorChar)
        + Path.DirectorySeparatorChar;

    var targetPrefix =
        targetDirectory.TrimEnd(Path.DirectorySeparatorChar)
        + Path.DirectorySeparatorChar;

    if (!targetDirectory.StartsWith(
            requestPrefix,
            comparison)
        ||
        !projectDirectory.StartsWith(
            targetPrefix,
            comparison))
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Project path is outside the generated directory."
        });
    }

    if (!Directory.Exists(requestDirectory) ||
        !Directory.Exists(targetDirectory) ||
        !Directory.Exists(projectDirectory))
    {
        return Results.NotFound(new
        {
            success = false,
            message = "Generated project not found."
        });
    }

    var requestInfo = new DirectoryInfo(requestDirectory);
    var targetInfo = new DirectoryInfo(targetDirectory);
    var projectInfo = new DirectoryInfo(projectDirectory);

    if ((requestInfo.Attributes & FileAttributes.ReparsePoint) != 0 ||
        (targetInfo.Attributes & FileAttributes.ReparsePoint) != 0 ||
        (projectInfo.Attributes & FileAttributes.ReparsePoint) != 0)
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Symbolic link directories are not supported."
        });
    }

    var enumerationOptions = new EnumerationOptions
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = false,
        ReturnSpecialDirectories = false
    };

    var files = Directory
        .EnumerateFiles(
            projectDirectory,
            "*",
            enumerationOptions
        )
        .Where(file =>
        {
            var name = Path.GetFileName(file);

            if (string.Equals(
                name,
                ".env",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (name.StartsWith(
                    ".env.",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !string.Equals(
                    name,
                    ".env.example",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var relativePath = Path.GetRelativePath(
                projectDirectory,
                file
            );

            var segments = relativePath.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );

            if (segments.Any(segment =>
                string.Equals(
                    segment,
                    ".git",
                    StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        })
        .ToList();

    if (files.Count == 0)
    {
        return Results.NotFound(new
        {
            success = false,
            message = "Project contains no downloadable files."
        });
    }

    const int maxFiles = 2000;
    const long maxTotalBytes = 100 * 1024 * 1024;

    if (files.Count > maxFiles)
    {
        return Results.StatusCode(
            StatusCodes.Status413PayloadTooLarge
        );
    }

    long totalBytes = 0;

    foreach (var file in files)
    {
        var fileSize = new FileInfo(file).Length;

        if (fileSize > maxTotalBytes - totalBytes)
        {
            return Results.StatusCode(
                StatusCodes.Status413PayloadTooLarge
            );
        }

        totalBytes += fileSize;
    }

    var memoryStream = new MemoryStream();

    try
    {
        using (var archive = new ZipArchive(
            memoryStream,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath = Path
                    .GetRelativePath(projectDirectory, file)
                    .Replace('\\', '/');

                if (Path.IsPathRooted(relativePath) ||
                    relativePath.Split('/').Any(segment =>
                        segment == ".."))
                {
                    continue;
                }

                var entry = archive.CreateEntry(
                    relativePath,
                    CompressionLevel.Fastest
                );

                await using var input = new FileStream(
                    file,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    useAsync: true
                );

                await using var output = entry.Open();

                await input.CopyToAsync(
                    output,
                    cancellationToken
                );
            }
        }

        memoryStream.Position = 0;

        context.Response.Headers.CacheControl = "no-store";

        return Results.File(
            memoryStream,
            "application/zip",
            $"{projectName}.zip"
        );
    }
    catch
    {
        await memoryStream.DisposeAsync();
        throw;
    }
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
