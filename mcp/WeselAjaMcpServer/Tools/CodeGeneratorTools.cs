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
        "Generates a standalone WeselAja integration project from a user's request. " +
        "This tool is for code generation only and does not create or execute payments. " +
        "The currently supported target is Python. " +
        "Use this tool when the user asks to build, generate, scaffold, or create " +
        "WeselAja integration code. " +
        "Examples: 'Buatkan integrasi WeselAja dalam Python', " +
        "'Generate a Python client for WeselAja', or " +
        "'Buatkan project Python untuk WeselAja'.")]
    public static string GenerateWeselAjaIntegration(
        [Description(
            "Target programming language or platform. " +
            "Currently supported: python.")]
        string target,

        [Description(
            "Name of the generated project directory. " +
            "Example: weselaja-python.")]
        string projectName = "weselaja-python",

        [Description(
            "Whether to include a runnable example.py file.")]
        bool includeExample = true,

        [Description(
            "Whether to include unit tests using Python's built-in unittest.")]
        bool includeTests = true,

        [Description(
            "Whether an existing generated project with the same name may be overwritten.")]
        bool overwrite = false)
    {
        if (!string.Equals(
                target?.Trim(),
                "python",
                StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(
                new
                {
                    success = false,
                    error =
                        "The Python generator is currently the only supported target.",
                    supportedTargets = new[]
                    {
                        "python"
                    }
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }

        try
        {
            var generator =
                new PythonWeselAjaGenerator();

            var result =
                generator.Generate(
                    projectName,
                    includeExample,
                    includeTests,
                    overwrite);

            return JsonSerializer.Serialize(
                new
                {
                    success = true,

                    target = "python",

                    projectName =
                        result.ProjectName,

                    outputDirectory =
                        result.OutputDirectory,

                    files =
                        result.Files,

                    message =
                        "WeselAja Python integration project generated successfully."
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }
        catch (Exception exception)
        {
            return JsonSerializer.Serialize(
                new
                {
                    success = false,
                    error = exception.Message
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }
    }
}