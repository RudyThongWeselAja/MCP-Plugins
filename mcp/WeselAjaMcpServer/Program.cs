
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

using Npgsql;

using WeselAjaMcp.Configuration;
using WeselAjaMcp.Security;
using WeselAjaMcp.Services;

using WeselAjaMcpServer.Data;
using WeselAjaMcpServer.Endpoints;
using WeselAjaMcpServer.Generators;

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

    builder.Services.AddSingleton<
        IGeneratorRequestRepository,
        InMemoryGeneratorRequestRepository>();

    builder.Services.AddSingleton<DownloadTicketService>();

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

    var databaseUrl = Environment
        .GetEnvironmentVariable("DATABASE_URL")
        ?.Trim();

    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        throw new InvalidOperationException(
            "DATABASE_URL environment variable is required.");
    }

    var adminToken = Environment
        .GetEnvironmentVariable("MCP_ADMIN_TOKEN")
        ?.Trim();

    if (string.IsNullOrWhiteSpace(adminToken) ||
        adminToken.Length < 32)
    {
        throw new InvalidOperationException(
            "MCP_ADMIN_TOKEN must contain at least 32 characters.");
    }

    var connectionString =
        BuildNpgsqlConnectionString(databaseUrl);

    builder.Services.AddSingleton<NpgsqlDataSource>(
        _ => NpgsqlDataSource.Create(connectionString));

    builder.Services.AddSingleton<McpDatabase>();
    builder.Services.AddSingleton<AccessTokenRepository>();
    builder.Services.AddSingleton<AccessTokenService>();

    builder.Services.AddSingleton<
        IGeneratorRequestRepository,
        PostgresGeneratorRequestRepository>();

    builder.Services.AddSingleton<DownloadTicketService>();

    builder.Services
        .AddMcpServer()
        .WithHttpTransport(options =>
        {
            options.SessionMode =
                HttpServerSessionMode.Stateless;
        })
        .WithToolsFromAssembly();

    var app = builder.Build();

    await app.Services
        .GetRequiredService<McpDatabase>()
        .InitializeAsync();

    var tokenService = app.Services
        .GetRequiredService<AccessTokenService>();

    app.Use(async (context, next) =>
    {
        var isAdminEndpoint =
            context.Request.Path.StartsWithSegments(
                "/admin",
                StringComparison.OrdinalIgnoreCase);

        var isMcpEndpoint =
            context.Request.Path.StartsWithSegments(
                "/mcp",
                StringComparison.OrdinalIgnoreCase);

        var isDownloadEndpoint =
            context.Request.Path.StartsWithSegments(
                "/downloads",
                StringComparison.OrdinalIgnoreCase);

        if (!isAdminEndpoint &&
            !isMcpEndpoint &&
            !isDownloadEndpoint)
        {
            await next();
            return;
        }

        if (isDownloadEndpoint)
        {
            await next();
            return;
        }

        var providedToken = GetBearerToken(context);

        if (string.IsNullOrWhiteSpace(providedToken))
        {
            await WriteUnauthorized(context);
            return;
        }

        if (isAdminEndpoint)
        {
            if (!FixedTimeSecretEquals(
                providedToken,
                adminToken))
            {
                await WriteUnauthorized(context);
                return;
            }

            await next();
            return;
        }

        if (!isMcpEndpoint)
        {
            await WriteUnauthorized(context);
            return;
        }

        var authenticatedUser = await tokenService.ValidateAsync(
            providedToken,
            context.RequestAborted);

        if (authenticatedUser is null)
        {
            await WriteUnauthorized(context);
            return;
        }

        using (AuthenticatedUserScope.Begin(authenticatedUser))
        {
            await next();
        }
    });

    app.MapMcp("/mcp");

    app.MapTokenManagementEndpoints();

    app.MapGet(
        "/health",
        () => Results.Ok(new
        {
            success = true,
            service = "WeselAja MCP",
            transport = "Streamable HTTP"
        }));

    app.MapGet(
        "/downloads/{requestId}/{target}/{projectName}",
        DownloadGeneratedProject);

    var railwayPort = Environment.GetEnvironmentVariable("PORT");

    var httpUrl = int.TryParse(railwayPort, out var port) && port > 0
        ? $"http://0.0.0.0:{port}"
        : (
            Environment.GetEnvironmentVariable("MCP_HTTP_URL")
            ?? "http://0.0.0.0:3001"
        ).Trim();

    Console.Error.WriteLine("WeselAja MCP Server");
    Console.Error.WriteLine("Transport : Streamable HTTP");

    Console.Error.WriteLine(
        $"Endpoint  : {httpUrl.TrimEnd('/')}/mcp");

    Console.Error.WriteLine(
        $"Health    : {httpUrl.TrimEnd('/')}/health");

    Console.Error.WriteLine(
        $"Downloads : {httpUrl.TrimEnd('/')}/downloads");

    Console.Error.WriteLine(
        $"Output    : {GeneratorOutputScope.GetDefaultRoot()}");

    await app.RunAsync(httpUrl);
}

static string? GetBearerToken(HttpContext context)
{
    const string prefix = "Bearer ";

    var authorization = context.Request
        .Headers["Authorization"]
        .ToString();

    if (!authorization.StartsWith(
        prefix,
        StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    return authorization[prefix.Length..].Trim();
}

static bool FixedTimeSecretEquals(
    string provided,
    string expected)
{
    var providedHash = SHA256.HashData(
        Encoding.UTF8.GetBytes(provided));

    var expectedHash = SHA256.HashData(
        Encoding.UTF8.GetBytes(expected));

    return CryptographicOperations.FixedTimeEquals(
        providedHash,
        expectedHash);
}

static async Task WriteUnauthorized(
    HttpContext context)
{
    context.Response.StatusCode =
        StatusCodes.Status401Unauthorized;

    context.Response.Headers["WWW-Authenticate"] =
        "Bearer";

    context.Response.Headers["Cache-Control"] =
        "no-store";

    await context.Response.WriteAsync("Unauthorized");
}

static string BuildNpgsqlConnectionString(string value)
{
    value = value.Trim();

    if (!Uri.TryCreate(
        value,
        UriKind.Absolute,
        out var uri))
    {
        return new NpgsqlConnectionStringBuilder(value)
            .ConnectionString;
    }

    if (uri.Scheme != "postgres" &&
        uri.Scheme != "postgresql")
    {
        throw new InvalidOperationException(
            "DATABASE_URL must use PostgreSQL.");
    }

    var userInfo = Uri.UnescapeDataString(uri.UserInfo);

    var separator = userInfo.IndexOf(':');

    if (separator <= 0)
    {
        throw new InvalidOperationException(
            "Invalid PostgreSQL credentials in DATABASE_URL.");
    }

    var username = userInfo[..separator];
    var password = userInfo[(separator + 1)..];

    var database = Uri.UnescapeDataString(
        uri.AbsolutePath.TrimStart('/'));

    if (string.IsNullOrWhiteSpace(database))
    {
        throw new InvalidOperationException(
            "PostgreSQL database name is missing.");
    }

    var connection = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,

        Port = uri.Port > 0
            ? uri.Port
            : 5432,

        Database = database,
        Username = username,
        Password = password,

        SslMode = SslMode.Prefer,

        Timeout = 15,
        CommandTimeout = 30
    };

    var query = uri.Query.TrimStart('?');

    foreach (var item in query.Split(
        '&',
        StringSplitOptions.RemoveEmptyEntries))
    {
        var parts = item.Split('=', 2);

        var key = Uri.UnescapeDataString(parts[0]);

        var queryValue = parts.Length > 1
            ? Uri.UnescapeDataString(parts[1])
            : string.Empty;

        if (key.Equals(
            "sslmode",
            StringComparison.OrdinalIgnoreCase))
        {
            var normalized = queryValue.Replace(
                "-",
                "",
                StringComparison.Ordinal);

            if (Enum.TryParse<SslMode>(
                normalized,
                true,
                out var sslMode))
            {
                connection.SslMode = sslMode;
            }
        }
    }

    return connection.ConnectionString;
}

static async Task<IResult> DownloadGeneratedProject(
    HttpContext context,
    string requestId,
    string target,
    string projectName,
    IGeneratorRequestRepository requestRepository,
    AccessTokenService accessTokenService,
    DownloadTicketService downloadTicketService,
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
                || c == '.');
    }

    context.Response.Headers["Cache-Control"] = "no-store";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";

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

    if (!Guid.TryParseExact(requestId, "N", out var requestGuid))
    {
        return Results.NotFound(new
        {
            success = false,
            message = "Generated project not found."
        });
    }

    var ticket = context.Request.Query["ticket"].ToString();

    var isTicketDownload =
        !string.IsNullOrWhiteSpace(ticket);

    var authorized = false;

    if (isTicketDownload)
    {
        authorized = await downloadTicketService.ValidateAsync(
            ticket,
            requestGuid,
            target,
            projectName,
            cancellationToken);
    }
    else
    {
        var bearerToken = GetBearerToken(context);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            var user = await accessTokenService.ValidateAsync(
                bearerToken,
                cancellationToken);

            if (user is not null)
            {
                authorized = await requestRepository.IsOwnedAsync(
                    requestGuid,
                    user.UserId,
                    cancellationToken);
            }
        }
    }

    if (!authorized)
    {
        return Results.NotFound(new
        {
            success = false,
            message = "Generated project not found."
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
            requestId));

    var targetDirectory = Path.GetFullPath(
        Path.Combine(
            requestDirectory,
            target));

    var projectDirectory = Path.GetFullPath(
        Path.Combine(
            targetDirectory,
            projectName));

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
            enumerationOptions)
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
                file);

            var segments = relativePath.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

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
            StatusCodes.Status413PayloadTooLarge);
    }

    long totalBytes = 0;

    foreach (var file in files)
    {
        var fileSize = new FileInfo(file).Length;

        if (fileSize > maxTotalBytes - totalBytes)
        {
            return Results.StatusCode(
                StatusCodes.Status413PayloadTooLarge);
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
                    CompressionLevel.Fastest);

                await using var input = new FileStream(
                    file,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    useAsync: true);

                await using var output = entry.Open();

                await input.CopyToAsync(
                    output,
                    cancellationToken);
            }
        }

        if (isTicketDownload)
        {
            var consumed = await downloadTicketService.ConsumeAsync(
                ticket,
                requestGuid,
                target,
                projectName,
                cancellationToken);

            if (!consumed)
            {
                await memoryStream.DisposeAsync();

                return Results.NotFound(new
                {
                    success = false,
                    message = "Download ticket is invalid, expired, or already used."
                });
            }
        }

        memoryStream.Position = 0;

        return Results.File(
            memoryStream,
            "application/zip",
            $"{projectName}.zip");
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
            StringComparison.OrdinalIgnoreCase),

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
