using System.ComponentModel;
using System.Text.Json;

using ModelContextProtocol.Server;

using WeselAjaMcpServer.Generators;

namespace WeselAjaMcpServer.Tools;

[McpServerToolType]
public static class CodeGeneratorTools
{
    [McpServerTool(
        Name = "generate_weselaja_integration")]
    [Description(
        "Generates a standalone WeselAja developer integration project. " +
        "This tool is for CODE GENERATION only and never creates a payment itself. " +
        "Supported targets are Python, C#, PHP, Go, JavaScript, TypeScript, and Java. " +
        "The generated scope currently includes creating payments, retrieving payin sessions, and handling browser redirects. " +
        "Callback handling is not generated yet. " +
        "Use this tool when the user asks to build, generate, scaffold, or create " +
        "WeselAja integration code.")]
    public static string GenerateWeselAjaIntegration(
        [Description(
            "Target programming language or platform. " +
            "Supported targets: python, csharp, go, golang, php, javascript, js, " +
            "typescript, ts, java.")]
        string target,

        [Description(
            "Name of the generated project directory. " +
            "Examples: weselaja-python, weselaja-csharp, weselaja-php, " +
            "weselaja-go, weselaja-javascript, weselaja-typescript, weselaja-java.")]
        string projectName = "weselaja-integration",

        [Description(
            "Whether to include a runnable example application.")]
        bool includeExample = true,

        [Description(
            "Whether to include self-tests.")]
        bool includeTests = true,

        [Description(
            "Whether an existing generated project may be overwritten.")]
        bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return JsonSerializer.Serialize(
                new
                {
                    success = false,
                    error = "Target is required.",
                    supportedTargets = new[]
                    {
                        "python",
                        "csharp",
                        "php",
                        "go",
                        "javascript",
                        "typescript",
                        "java"
                    }
                },
                JsonOptions());
        }

        try
        {
            var normalizedTarget =
                target.Trim();

            if (
                string.Equals(
                    normalizedTarget,
                    "python",
                    StringComparison.OrdinalIgnoreCase))
            {
                var generator =
                    new PythonWeselAjaGenerator();

                var result =
                    generator.Generate(
                        projectName,
                        includeExample,
                        includeTests,
                        overwrite);

                return SerializeSuccess(
                    result,
                    "python",
                    "WeselAja Python integration project generated successfully.");
            }

            if (
                string.Equals(
                    normalizedTarget,
                    "csharp",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    normalizedTarget,
                    "c#",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    normalizedTarget,
                    "dotnet",
                    StringComparison.OrdinalIgnoreCase))
            {
                var generator =
                    new CSharpWeselAjaGenerator();

                var result =
                    generator.Generate(
                        projectName,
                        includeExample,
                        includeTests,
                        overwrite);

                return SerializeSuccess(
                    result,
                    "csharp",
                    "WeselAja C# integration project generated successfully.");
            }

            if (
                string.Equals(
                    normalizedTarget,
                    "php",
                    StringComparison.OrdinalIgnoreCase))
            {
                var generator =
                    new PhpWeselAjaGenerator();

                var result =
                    generator.Generate(
                        projectName,
                        includeExample,
                        includeTests,
                        overwrite);

                return SerializeSuccess(
                    result,
                    "php",
                    "WeselAja PHP integration project generated successfully.");
            }

            if (
                string.Equals(
                    normalizedTarget,
                    "go",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    normalizedTarget,
                    "golang",
                    StringComparison.OrdinalIgnoreCase))
            {
                var generator =
                    new GoWeselAjaGenerator();

                var result =
                    generator.Generate(
                        projectName,
                        includeExample,
                        includeTests,
                        overwrite);

                return SerializeSuccess(
                    result,
                    "go",
                    "WeselAja Go integration project generated successfully.");
            }

            if (
                string.Equals(
                    normalizedTarget,
                    "javascript",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    normalizedTarget,
                    "js",
                    StringComparison.OrdinalIgnoreCase))
            {
                var generator =
                    new JavaScriptWeselAjaGenerator();

                var result =
                    generator.Generate(
                        projectName,
                        includeExample,
                        includeTests,
                        overwrite);

                return SerializeSuccess(
                    result,
                    "javascript",
                    "WeselAja JavaScript integration project generated successfully.");
            }

            if (
                string.Equals(
                    normalizedTarget,
                    "typescript",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    normalizedTarget,
                    "ts",
                    StringComparison.OrdinalIgnoreCase))
            {
                var generator =
                    new TypeScriptWeselAjaGenerator();

                var result =
                    generator.Generate(
                        projectName,
                        includeExample,
                        includeTests,
                        overwrite);

                return SerializeSuccess(
                    result,
                    "typescript",
                    "WeselAja TypeScript integration project generated successfully.");
            }

            if (
                string.Equals(
                    normalizedTarget,
                    "java",
                    StringComparison.OrdinalIgnoreCase))
            {
                var generator =
                    new JavaWeselAjaGenerator();

                var result =
                    generator.Generate(
                        projectName,
                        includeExample,
                        includeTests,
                        overwrite);

                return SerializeSuccess(
                    result,
                    "java",
                    "WeselAja Java integration project generated successfully.");
            }

            return JsonSerializer.Serialize(
                new
                {
                    success = false,
                    error =
                        $"Unsupported target '{target}'.",
                    supportedTargets = new[]
                    {
                        "python",
                        "csharp",
                        "php",
                        "go",
                        "javascript",
                        "typescript",
                        "java"
                    }
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
                projectName =
                    result.ProjectName,
                outputDirectory =
                    result.OutputDirectory,
                files =
                    result.Files,
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
