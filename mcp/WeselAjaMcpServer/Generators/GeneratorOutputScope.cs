using System.Threading;

namespace WeselAjaMcpServer.Generators;

public static class GeneratorOutputScope
{
    private static readonly AsyncLocal<string?> ScopedOutputRoot = new();
    private static readonly AsyncLocal<string?> ScopedRequestId = new();

    public static string? GetConfiguredRoot()
    {
        return ScopedOutputRoot.Value
            ?? Environment.GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");
    }

    public static string? GetCurrentRequestId()
    {
        return ScopedRequestId.Value;
    }

    public static string GetDefaultRoot()
    {
        var configuredRoot = Environment.GetEnvironmentVariable(
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
        var previousRequestId = ScopedRequestId.Value;

        var requestId = Guid.NewGuid().ToString("N");

        ScopedRequestId.Value = requestId;

        ScopedOutputRoot.Value = Path.Combine(
            GetDefaultRoot(),
            "requests",
            requestId);

        return new Scope(previousRoot, previousRequestId);
    }

    private sealed class Scope(
        string? previousRoot,
        string? previousRequestId) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            ScopedOutputRoot.Value = previousRoot;
            ScopedRequestId.Value = previousRequestId;

            _disposed = true;
        }
    }
}