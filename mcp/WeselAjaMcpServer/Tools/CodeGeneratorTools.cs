
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;

using ModelContextProtocol.Server;
using WeselAjaMcpServer.Generators;

namespace WeselAjaMcpServer.Tools;

[McpServerToolType]
public static class CodeGeneratorTools
{
    private static readonly string[] SupportedTargets =
    [
        "python",
        "csharp",
        "php",
        "go",
        "javascript",
        "typescript",
        "java",
        "android-kotlin",
        "shopify",
        "odoo",
        "woocommerce",
    ];

    [McpServerTool(Name = "generate_weselaja_integration")]
    [Description(
        "Generate a ready-to-download WeselAja integration project only for a supported " +
        "target: Python, C#, PHP, Go, JavaScript, TypeScript, Java, Android Kotlin, " +
        "Shopify, Odoo 18.0, or WooCommerce. Use this tool when the user asks to create " +
        "a WeselAja integration for one of those targets. Do not create an integration for " +
        "an unsupported platform. Each generation is isolated on the server, so the same " +
        "project name can be used safely by different users. On success, return the download " +
        "URL and tell the user the project is ready to download."
    )]
    public static string GenerateWeselAjaIntegration(
        [Description(
            "Target platform. Supported values: python, csharp, go, php, javascript, " +
            "typescript, java, android-kotlin, shopify, odoo, woocommerce. " +
            "Android Kotlin also accepts android and kotlin. C# also accepts csharp, c#, " +
            "and dotnet. The Shopify target also accepts shopify-app.")]
        string target,

        [Description(
            "Name of the generated project or module directory.")]
        string projectName = "weselaja-integration",

        [Description(
            "Whether to include an example when supported.")]
        bool includeExample = true,

        [Description(
            "Whether to include tests or a manual test checklist when supported.")]
        bool includeTests = true,

        [Description(
            "Target Odoo version. Only 18.0 is supported; ignored for other targets.")]
        string odooVersion = "18.0")
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return SerializeError("Target is required.", SupportedTargets);
        }

        try
        {
            using var generationWorkspace =
                GeneratorOutputScope.BeginIsolatedWorkspace();

            var normalizedTarget = target.Trim();

            if (IsTarget(normalizedTarget, "python"))
            {
                var result = new PythonWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "python",
                    "WeselAja Python integration project generated successfully."
                );
            }

            if (IsTarget(normalizedTarget, "csharp", "c#", "dotnet"))
            {
                var result = new CSharpWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "csharp",
                    "WeselAja C# integration project generated successfully."
                );
            }

            if (IsTarget(normalizedTarget, "php"))
            {
                var result = new PhpWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "php",
                    "WeselAja PHP integration project generated successfully."
                );
            }

            if (IsTarget(normalizedTarget, "go", "golang"))
            {
                var result = new GoWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "go",
                    "WeselAja Go integration project generated successfully."
                );
            }

            if (IsTarget(normalizedTarget, "javascript", "js"))
            {
                var result = new JavaScriptWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "javascript",
                    "WeselAja JavaScript integration project generated successfully."
                );
            }

            if (IsTarget(normalizedTarget, "typescript", "ts"))
            {
                var result = new TypeScriptWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "typescript",
                    "WeselAja TypeScript integration project generated successfully."
                );
            }

            if (IsTarget(normalizedTarget, "java"))
            {
                var result = new JavaWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "java",
                    "WeselAja Java integration project generated successfully."
                );
            }

            if (IsTarget(
                normalizedTarget,
                "android-kotlin",
                "android",
                "kotlin",
                "androidkotlin"))
            {
                var result = new AndroidKotlinWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "android-kotlin",
                    "WeselAja Android Kotlin Direct Payin integration generated successfully. " +
                    "The generated Android app calls the WeselAja middleware directly using " +
                    "Retrofit and OkHttp, includes a payment form and optional checkout WebView, " +
                    "and does not require an application backend. Configure sandbox credentials " +
                    "and merchant settings before testing. Do not distribute an APK containing " +
                    "production API secrets."
                );
            }

            if (IsTarget(normalizedTarget, "shopify", "shopify-app"))
            {
                var result = new ShopifyWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "shopify",
                    $"WeselAja Shopify application generated successfully. " +
                    $"Generator version: {ShopifyWeselAjaGenerator.GeneratorVersion}. " +
                    "Link the app using Shopify CLI, configure sandbox credentials, " +
                    "initialize Prisma, and run the generated tests before testing payments."
                );
            }

            if (IsTarget(normalizedTarget, "odoo"))
            {
                if (!string.Equals(
                    odooVersion?.Trim(),
                    "18.0",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return SerializeError(
                        $"Unsupported Odoo version '{odooVersion}'. " +
                        "Only Odoo 18.0 is supported.",
                        SupportedTargets
                    );
                }

                var result = new OdooWeselAjaGenerator().Generate(
                    projectName,
                    "18.0",
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "odoo",
                    "WeselAja Odoo 18.0 payment provider module generated successfully."
                );
            }

            if (IsTarget(
                normalizedTarget,
                "woocommerce",
                "woo-commerce",
                "wc"))
            {
                var result = new WooCommerceWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite: false
                );

                return SerializeSuccess(
                    result,
                    "woocommerce",
                    "WeselAja WooCommerce payment plugin generated successfully."
                );
            }

            return SerializeError(
                $"Unsupported target '{target}'.",
                SupportedTargets
            );
        }
        catch (Exception exception)
        {
            return JsonSerializer.Serialize(
                new
                {
                    success = false,
                    error = exception.Message
                },
                JsonOptions()
            );
        }
    }

    private static bool IsTarget(
        string target,
        params string[] aliases)
    {
        return aliases.Any(alias =>
            string.Equals(
                target,
                alias,
                StringComparison.OrdinalIgnoreCase
            ));
    }

    private static string SerializeSuccess(
        GenerationResult result,
        string target,
        string message)
    {
        var downloadInfo = BuildDownloadInfo(result);

        return JsonSerializer.Serialize(
            new
            {
                success = true,
                target,
                projectName = result.ProjectName,
                files = result.Files,

                downloadUrl = downloadInfo.Url,
                downloadFileName = downloadInfo.FileName,

                message = downloadInfo.Url is null
                    ? message +
                      " Project berhasil dibuat. Download belum tersedia; " +
                      "hubungi pengelola MCP."
                    : message +
                      " Project siap diunduh sebagai ZIP melalui downloadUrl."
            },
            JsonOptions()
        );
    }

    private static (
        string? Url,
        string? FileName
    ) BuildDownloadInfo(GenerationResult result)
    {
        var publicBaseUrl = Environment
            .GetEnvironmentVariable("MCP_PUBLIC_BASE_URL")
            ?.Trim()
            .TrimEnd('/');

        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return (null, null);
        }

        if (!Uri.TryCreate(
            publicBaseUrl,
            UriKind.Absolute,
            out var baseUri))
        {
            return (null, null);
        }

        var isSecureScheme =
            baseUri.Scheme == Uri.UriSchemeHttps;

        var isLocalHttp =
            baseUri.Scheme == Uri.UriSchemeHttp
            && baseUri.IsLoopback;

        if (!isSecureScheme && !isLocalHttp)
        {
            return (null, null);
        }

        if (!string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment) ||
            !string.IsNullOrEmpty(baseUri.UserInfo))
        {
            return (null, null);
        }

        try
        {
            var generatedRoot =
                GeneratorOutputScope.GetDefaultRoot();

            var outputDirectory = Path.GetFullPath(
                result.OutputDirectory
            );

            var relativeDirectory = Path.GetRelativePath(
                generatedRoot,
                outputDirectory
            );

            if (Path.IsPathRooted(relativeDirectory) ||
                relativeDirectory == ".." ||
                relativeDirectory.StartsWith(
                    ".." + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal
                ) ||
                relativeDirectory.StartsWith(
                    ".." + Path.AltDirectorySeparatorChar,
                    StringComparison.Ordinal
                ))
            {
                return (null, null);
            }

            var segments = relativeDirectory.Split(
                new[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (segments.Length != 4 ||
                !string.Equals(
                    segments[0],
                    "requests",
                    StringComparison.OrdinalIgnoreCase))
            {
                return (null, null);
            }

            var requestId = segments[1];
            var targetDirectory = segments[2];
            var projectDirectory = segments[3];

            var downloadUrl =
                $"{baseUri.AbsoluteUri.TrimEnd('/')}/downloads/" +
                $"{Uri.EscapeDataString(requestId)}/" +
                $"{Uri.EscapeDataString(targetDirectory)}/" +
                $"{Uri.EscapeDataString(projectDirectory)}";

            return (
                downloadUrl,
                $"{result.ProjectName}.zip"
            );
        }
        catch
        {
            return (null, null);
        }
    }

    private static string SerializeError(
        string error,
        string[]? supportedTargets = null)
    {
        return JsonSerializer.Serialize(
            new
            {
                success = false,
                error,
                supportedTargets
            },
            JsonOptions()
        );
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
}
