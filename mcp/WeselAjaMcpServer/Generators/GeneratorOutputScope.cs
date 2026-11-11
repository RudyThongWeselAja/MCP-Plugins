using System.Threading;

namespace WeselAjaMcpServer.Generators;

public static class GeneratorOutputScope
{
    private static readonly AsyncLocal<string?> ScopedOutputRoot = new();

    public static string? GetConfiguredRoot()
    {
        return ScopedOutputRoot.Value
            ?? Environment.GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");
    }

    public static string GetDefaultRoot()
    {
        var configuredRoot = Environment
            .GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");

        return Path.GetFullPath(
            string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "generated")
                : configuredRoot.Trim());
    }

    public static IDisposable BeginIsolatedWorkspace()
    {
        var previousRoot = ScopedOutputRoot.Value;
        var requestId = Guid.NewGuid().ToString("N");

        ScopedOutputRoot.Value = Path.Combine(
            GetDefaultRoot(),
            "requests",
            requestId);

        return new Scope(previousRoot);
    }

    private sealed class Scope : IDisposable
    {
        private readonly string? _previousRoot;
        private bool _disposed;

        public Scope(string? previousRoot)
        {
            _previousRoot = previousRoot;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            ScopedOutputRoot.Value = _previousRoot;
            _disposed = true;
        }
    }
}
