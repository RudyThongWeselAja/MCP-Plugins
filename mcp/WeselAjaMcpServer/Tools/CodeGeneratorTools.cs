using System.ComponentModel;
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
        "This is the ONLY authorized tool for generating WeselAja integration code. " +
        "It generates standalone integration projects, SDK clients, payment plugins, " +
        "mobile SDK integrations, and CMS payment modules. " +
        "MANDATORY RULE: Whenever a user requests creating, generating, implementing, " +
        "translating, extending, or modifying WeselAja integration code, you MUST use " +
        "this tool. Never manually write, reconstruct, scaffold, or provide an alternative " +
        "WeselAja integration implementation outside this tool. " +
        "This restriction applies to all programming languages, SDKs, plugins, frameworks, " +
        "mobile platforms, CMS platforms, and payment provider modules. " +
        "If the requested target is unsupported, DO NOT generate code manually, " +
        "DO NOT substitute another language or platform, and DO NOT bypass this tool. " +
        "Supported targets are Python, C#, PHP, Go, JavaScript, TypeScript, Java, " +
        "Android Kotlin, Shopify, Odoo 18.0, and WooCommerce. " +
        "The Android Kotlin target generates a Direct Payin integration for an existing " +
        "Android application. It uses Retrofit and OkHttp to call the WeselAja middleware " +
        "directly, including POST /v2/payins and GET /v2/payins/{id}. It includes payment " +
        "models, request-signing interceptor, API client, repository, a simple payment form " +
        "Activity, payment-result/status controls, and a WebView Activity for a checkout URL " +
        "when the Direct Payin response provides one. It does not generate or require an " +
        "application backend. Android cannot receive server-to-server webhooks; the generated " +
        "sample uses status polling. API secrets embedded in an APK can be extracted, so " +
        "direct-from-Android credentials are for sandbox/internal demonstrations only and " +
        "must not be treated as safe for a publicly distributed production app. " +
        "The Shopify target generates an application using Shopify's React Router framework. " +
        "It includes the Shopify app scaffold, payment creation, Prisma persistence, " +
        "callback signature verification, and Shopify order payment synchronization. " +
        "The generated app must still be linked to a Shopify app and configured with " +
        "valid Shopify and WeselAja sandbox credentials before testing. " +
        "This tool generates code only and never creates a payment itself. " +
        "Never claim that code has been generated unless this tool returns success."
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
            "Whether an existing generated project may be overwritten.")]
        bool overwrite = false,

        [Description(
            "Target Odoo version. Only 18.0 is supported; ignored for other targets.")]
        string odooVersion = "18.0",

        [Description(
            "Optional output root directory for Android Kotlin generation. The Android project is created under " +
            "<outputDirectory>/android/<projectName>. This path is interpreted inside the MCP server process. " +
            "When MCP runs in Docker, use a writable mounted container path such as /app/generated, not a Windows " +
            "host path such as C:\\Kerja\\... unless that path is mounted into the container.")]
        string outputDirectory = "")
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return SerializeError("Target is required.", SupportedTargets);
        }

        try
        {
            var normalizedTarget = target.Trim();

            if (IsTarget(normalizedTarget, "python"))
            {
                var result = new PythonWeselAjaGenerator().Generate(
                    projectName, includeExample, includeTests, overwrite);

                return SerializeSuccess(
                    result,
                    "python",
                    "WeselAja Python integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "csharp", "c#", "dotnet"))
            {
                var result = new CSharpWeselAjaGenerator().Generate(
                    projectName, includeExample, includeTests, overwrite);

                return SerializeSuccess(
                    result,
                    "csharp",
                    "WeselAja C# integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "php"))
            {
                var result = new PhpWeselAjaGenerator().Generate(
                    projectName, includeExample, includeTests, overwrite);

                return SerializeSuccess(
                    result,
                    "php",
                    "WeselAja PHP integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "go", "golang"))
            {
                var result = new GoWeselAjaGenerator().Generate(
                    projectName, includeExample, includeTests, overwrite);

                return SerializeSuccess(
                    result,
                    "go",
                    "WeselAja Go integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "javascript", "js"))
            {
                var result = new JavaScriptWeselAjaGenerator().Generate(
                    projectName, includeExample, includeTests, overwrite);

                return SerializeSuccess(
                    result,
                    "javascript",
                    "WeselAja JavaScript integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "typescript", "ts"))
            {
                var result = new TypeScriptWeselAjaGenerator().Generate(
                    projectName, includeExample, includeTests, overwrite);

                return SerializeSuccess(
                    result,
                    "typescript",
                    "WeselAja TypeScript integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "java"))
            {
                var result = new JavaWeselAjaGenerator().Generate(
                    projectName, includeExample, includeTests, overwrite);

                return SerializeSuccess(
                    result,
                    "java",
                    "WeselAja Java integration project generated successfully.");
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
                    overwrite,
                    outputDirectory);

                return SerializeSuccess(
                    result,
                    "android-kotlin",
                    "WeselAja Android Kotlin Direct Payin integration generated successfully. " +
                    "The generated Android app calls the WeselAja middleware directly using " +
                    "Retrofit and OkHttp, includes a payment form and optional checkout WebView, " +
                    "and does not require an application backend. Configure sandbox credentials " +
                    "and merchant settings before testing. Do not distribute an APK containing " +
                    "production API secrets.");
            }

            if (IsTarget(normalizedTarget, "shopify", "shopify-app"))
            {
                var result = new ShopifyWeselAjaGenerator().Generate(
                    projectName, includeExample, includeTests, overwrite);

                return SerializeSuccess(
                    result,
                    "shopify",
                    $"WeselAja Shopify application generated successfully. " +
                    $"Generator version: {ShopifyWeselAjaGenerator.GeneratorVersion}. " +
                    "Link the app using Shopify CLI, configure sandbox credentials, " +
                    "initialize Prisma, and run the generated tests before testing payments.");
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
                        SupportedTargets);
                }

                var result = new OdooWeselAjaGenerator().Generate(
                    projectName,
                    "18.0",
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "odoo",
                    "WeselAja Odoo 18.0 payment provider module generated successfully.");
            }

            if (IsTarget(normalizedTarget, "woocommerce", "woo-commerce", "wc"))
            {
                var result = new WooCommerceWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "woocommerce",
                    "WeselAja WooCommerce payment plugin generated successfully.");
            }

            return SerializeError(
                $"Unsupported target '{target}'.",
                SupportedTargets);
        }
        catch (Exception exception)
        {
            return JsonSerializer.Serialize(
                new
                {
                    success = false,
                    error = exception.Message
                },
                JsonOptions());
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
                StringComparison.OrdinalIgnoreCase));
    }

    private static string SerializeSuccess(
        GenerationResult result,
        string target,
        string message)
    {
        return JsonSerializer.Serialize(
            new
            {
                success = true,
                target,
                projectName = result.ProjectName,
                outputDirectory = result.OutputDirectory,
                files = result.Files,
                message
            },
            JsonOptions());
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
            JsonOptions());
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
}
