using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class WooCommerceWeselAjaGenerator
{
    private static readonly string[] RequiredPluginFiles =
    [
        "weselaja-woocommerce.php",
        "includes/class-wc-weselaja-api.php",
        "includes/class-wc-gateway-weselaja.php",
        "includes/class-wc-weselaja-webhook.php",
        "includes/class-wc-weselaja-blocks-support.php",
        "assets/js/weselaja-blocks.js",
        "README.md"
    ];

    public GenerationResult Generate(
        string projectName,
        bool includeExample,
        bool includeTests,
        bool overwrite)
    {
        var safeProjectName = SanitizeProjectName(projectName);
        var templateRoot = FindTemplateRoot();

        var configuredOutputRoot = Environment.GetEnvironmentVariable(
            "WESELAJA_GENERATOR_OUTPUT_DIR");

        var outputRoot = string.IsNullOrWhiteSpace(configuredOutputRoot)
            ? Path.Combine(Directory.GetCurrentDirectory(), "generated")
            : configuredOutputRoot.Trim();

        outputRoot = Path.GetFullPath(outputRoot);

        var wooCommerceRoot = Path.GetFullPath(
            Path.Combine(outputRoot, "woocommerce"));

        var outputDirectory = Path.GetFullPath(
            Path.Combine(wooCommerceRoot, safeProjectName));

        EnsureInsideDirectory(outputDirectory, wooCommerceRoot);

        if (Directory.Exists(outputDirectory))
        {
            if (!overwrite)
            {
                throw new InvalidOperationException(
                    $"Generated project already exists: {outputDirectory}. " +
                    "Set overwrite=true to replace it.");
            }

            Directory.Delete(outputDirectory, recursive: true);
        }

        Directory.CreateDirectory(outputDirectory);

        var filesToCopy = new List<string>(RequiredPluginFiles);

        if (includeExample)
        {
            filesToCopy.Add("examples/payin_payload.json");
        }

        if (includeTests)
        {
            filesToCopy.Add("tests/test-weselaja.php");
        }

        var generatedFiles = new List<GeneratedFile>();

        foreach (var relativePath in filesToCopy)
        {
            var sourcePath = Path.GetFullPath(
                Path.Combine(templateRoot, relativePath));

            EnsureInsideDirectory(sourcePath, templateRoot);

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    $"WooCommerce generator template is missing: {relativePath}",
                    sourcePath);
            }

            var outputPath = Path.GetFullPath(
                Path.Combine(outputDirectory, relativePath));

            EnsureInsideDirectory(outputPath, outputDirectory);

            var outputFileDirectory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(outputFileDirectory))
            {
                Directory.CreateDirectory(outputFileDirectory);
            }

            File.Copy(sourcePath, outputPath, overwrite: false);

            generatedFiles.Add(new GeneratedFile
            {
                Path = Path.GetRelativePath(outputDirectory, outputPath),
                Bytes = new FileInfo(outputPath).Length
            });
        }

        return new GenerationResult(
            safeProjectName,
            outputDirectory,
            generatedFiles);
    }

    private static string FindTemplateRoot()
    {
        var starts = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var start in starts)
        {
            DirectoryInfo? current =
                new DirectoryInfo(Path.GetFullPath(start));

            while (current is not null)
            {
                var candidate = Path.Combine(
                    current.FullName,
                    "php",
                    "WooCommerceWeselAja");

                if (Directory.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate php/WooCommerceWeselAja. " +
            "Run the MCP server from the XenithPay repository " +
            "or keep the PHP template folder alongside it.");
    }

    private static string SanitizeProjectName(string? projectName)
    {
        var value = string.IsNullOrWhiteSpace(projectName)
            ? "weselaja-woocommerce"
            : projectName.Trim().ToLowerInvariant();

        var builder = new StringBuilder();
        var dashPending = false;

        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (dashPending && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                dashPending = false;
            }
            else if (builder.Length > 0)
            {
                dashPending = true;
            }
        }

        var result = builder.ToString().Trim('-');

        if (string.IsNullOrWhiteSpace(result))
        {
            result = "weselaja-woocommerce";
        }

        if (result.Length > 64)
        {
            result = result[..64].Trim('-');
        }

        return result;
    }

    private static void EnsureInsideDirectory(
        string path,
        string parentDirectory)
    {
        var fullPath = Path.GetFullPath(path);

        var fullParent = Path.GetFullPath(parentDirectory)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
            fullParent,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A generated path resolved outside its expected directory.");
        }
    }
}