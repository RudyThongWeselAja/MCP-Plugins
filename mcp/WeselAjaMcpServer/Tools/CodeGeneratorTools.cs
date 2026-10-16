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
        "odoo",
        "woocommerce"
    ];

    [McpServerTool(Name = "generate_weselaja_integration")]
    [Description(
        "Generates a standalone WeselAja integration project, an Odoo " +
        "payment provider module, or a WooCommerce plugin. This tool " +
        "generates code only and never creates a payment itself. " +
        "Supported targets are Python, C#, PHP, Go, JavaScript, " +
        "TypeScript, Java, Odoo, and WooCommerce. The CMS integrations " +
        "include configuration, Virtual Account payment instructions, " +
        "Pay In creation, callbacks, browser redirects, and status checks.")]
    public static string GenerateWeselAjaIntegration(
        [Description(
            "Target platform: python, csharp, php, go, javascript, " +
            "typescript, java, odoo, or woocommerce.")]
        string target,

        [Description(
            "Name of the generated project or module directory.")]
        string projectName = "weselaja-integration",

        [Description(
            "Whether to include an example when supported.")]
        bool includeExample = true,

        [Description(
            "Whether to include tests when supported.")]
        bool includeTests = true,

        [Description(
            "Whether an existing generated project may be overwritten.")]
        bool overwrite = false,

        [Description(
            "Target Odoo version. Supported value is 18.0. " +
            "This parameter is used only when target is odoo.")]
        string odooVersion = "18.0")
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return JsonSerializer.Serialize(
                new
                {
                    success = false,
                    error = "Target is required.",
                    supportedTargets = SupportedTargets
                },
                JsonOptions());
        }

        try
        {
            var normalizedTarget = target.Trim();

            if (IsTarget(normalizedTarget, "python"))
            {
                var result = new PythonWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "python",
                    "WeselAja Python integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "csharp", "c#", "dotnet"))
            {
                var result = new CSharpWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "csharp",
                    "WeselAja C# integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "php"))
            {
                var result = new PhpWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "php",
                    "WeselAja PHP integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "go", "golang"))
            {
                var result = new GoWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "go",
                    "WeselAja Go integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "javascript", "js"))
            {
                var result = new JavaScriptWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "javascript",
                    "WeselAja JavaScript integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "typescript", "ts"))
            {
                var result = new TypeScriptWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "typescript",
                    "WeselAja TypeScript integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "java"))
            {
                var result = new JavaWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "java",
                    "WeselAja Java integration project generated successfully.");
            }

            if (IsTarget(normalizedTarget, "odoo"))
            {
                var result = new OdooWeselAjaGenerator().Generate(
                    projectName,
                    odooVersion,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "odoo",
                    $"WeselAja Odoo {odooVersion} module generated. " +
                    "Validate installation and payment flows before production use.");
            }

            if (IsTarget(
                normalizedTarget,
                "woocommerce",
                "woo-commerce",
                "wordpress",
                "wc"))
            {
                var result = new WooCommerceWeselAjaGenerator().Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

                return SerializeSuccess(
                    result,
                    "woocommerce",
                    "WeselAja WooCommerce plugin generated. Configure " +
                    "the sandbox or production API URL and credentials " +
                    "in WooCommerce settings, then test payment creation, " +
                    "callbacks, redirects, and status checks.");
            }

            return JsonSerializer.Serialize(
                new
                {
                    success = false,
                    error = $"Unsupported target '{target}'.",
                    supportedTargets = SupportedTargets
                },
                JsonOptions());
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

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
}