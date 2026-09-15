using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace XenithPayMcpServer.Client;

public class PhpXenithPayClient
{
    private readonly string _phpPath;
    private readonly string _scriptPath;

    public PhpXenithPayClient()
    {
        _phpPath =
            Environment.GetEnvironmentVariable(
                "XENITH_PHP_EXECUTABLE")
            ?? "php";

        _scriptPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "../../../../../php/WooCommerceWeselAja/mcp/xenithpay.php"
            )
        );

        if (!File.Exists(_scriptPath))
        {
            throw new FileNotFoundException(
                "PHP XenithPay script was not found.",
                _scriptPath);
        }
    }

    public async Task<JsonElement> CallAsync(
        string tool,
        object? arguments = null)
    {
        var request = new
        {
            tool,
            arguments
        };

        var json =
            JsonSerializer.Serialize(request);

        var startInfo =
            new ProcessStartInfo
            {
                FileName = _phpPath,

                Arguments =
                    $"\"{_scriptPath}\"",

                WorkingDirectory =
                    Path.GetDirectoryName(
                        _scriptPath)!,

                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,

                UseShellExecute = false,
                CreateNoWindow = true,

                StandardInputEncoding =
                    new UTF8Encoding(false),

                StandardOutputEncoding =
                    new UTF8Encoding(false),

                StandardErrorEncoding =
                    new UTF8Encoding(false)
            };

        using var process =
            new Process
            {
                StartInfo = startInfo
            };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Unable to start PHP process.");
        }

        await process.StandardInput.WriteAsync(
            json);

        await process.StandardInput.FlushAsync();

        process.StandardInput.Close();

        var stdoutTask =
            process.StandardOutput.ReadToEndAsync();

        var stderrTask =
            process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout =
            await stdoutTask;

        var stderr =
            await stderrTask;

        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException(
                "PHP process returned empty output. " +
                $"Exit code: {process.ExitCode}. " +
                $"Error: {stderr}");
        }

        JsonElement result;

        try
        {
            using var document =
                JsonDocument.Parse(stdout);

            result =
                document.RootElement.Clone();
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException(
                "PHP returned invalid JSON. " +
                $"Output: {stdout}. " +
                $"STDERR: {stderr}",
                error);
        }

        if (process.ExitCode != 0)
        {
            var errorMessage =
                result.TryGetProperty(
                    "error",
                    out var errorProperty)
                && errorProperty.ValueKind !=
                    JsonValueKind.Null
                    ? errorProperty.GetString()
                    : stderr;

            throw new InvalidOperationException(
                "PHP process failed. " +
                $"Exit code: {process.ExitCode}. " +
                $"Error: {errorMessage}");
        }

        return result;
    }
}