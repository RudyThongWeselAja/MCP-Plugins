using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace WeselAjaMcpServer.Client;

public class PythonWeselAjaClient
{
    private readonly string _pythonPath;
    private readonly string _scriptPath;

    public PythonWeselAjaClient()
    {
        _pythonPath =
            Environment.GetEnvironmentVariable(
                "XENITH_PYTHON_EXECUTABLE")
            ?? "python";

        _scriptPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../python/WeselAja/WeselAja.py"
                )
            );

        if (!File.Exists(_scriptPath))
        {
            throw new FileNotFoundException(
                "Python WeselAja script was not found.",
                _scriptPath
            );
        }
    }

    public async Task<JsonElement> CallAsync(
        string tool,
        object? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            tool,
            arguments
        };

        var json =
            JsonSerializer.Serialize(payload);

        var startInfo =
            new ProcessStartInfo
            {
                FileName = _pythonPath,

                WorkingDirectory =
                    Path.GetDirectoryName(
                        _scriptPath
                    )!,

                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,

                UseShellExecute = false,
                CreateNoWindow = true,

                StandardOutputEncoding =
                    Encoding.UTF8,

                StandardErrorEncoding =
                    Encoding.UTF8
            };

        startInfo.ArgumentList.Add(
            _scriptPath
        );

        using var process =
            new Process
            {
                StartInfo = startInfo
            };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Unable to start WeselAja Python process."
            );
        }

        var inputBytes =
            Encoding.UTF8.GetBytes(
                json + "\n"
            );

        await process.StandardInput.BaseStream.WriteAsync(
            inputBytes,
            cancellationToken
        );

        await process.StandardInput.BaseStream.FlushAsync(
            cancellationToken
        );

        process.StandardInput.Close();

        var stdoutTask =
            process.StandardOutput.ReadToEndAsync(
                cancellationToken
            );

        var stderrTask =
            process.StandardError.ReadToEndAsync(
                cancellationToken
            );

        await process.WaitForExitAsync(
            cancellationToken
        );

        var stdout =
            await stdoutTask;

        var stderr =
            await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "WeselAja Python process failed. " +
                $"Exit code: {process.ExitCode}. " +
                $"Error: {stderr}"
            );
        }

        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException(
                "WeselAja Python process returned empty output."
            );
        }

        try
        {
            using var document =
                JsonDocument.Parse(stdout);

            return document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "WeselAja Python returned invalid JSON. " +
                $"Output: {stdout}. " +
                $"STDERR: {stderr}",
                exception
            );
        }
    }
}