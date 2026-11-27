using WeselAjaMcpServer.Generators;

namespace WeselAjaMcpServer.Services;

/// <summary>
/// Removes expired, request-isolated generator artifacts from local storage.
/// Database audit records are intentionally retained.
/// </summary>
public sealed class GeneratorArtifactCleanupService(
    ILogger<GeneratorArtifactCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DeleteExpiredArtifacts();
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Generator artifact cleanup failed.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private void DeleteExpiredArtifacts()
    {
        var requestsDirectory = Path.Combine(
            GeneratorOutputScope.GetDefaultRoot(),
            "requests");

        if (!Directory.Exists(requestsDirectory))
        {
            return;
        }

        var cutoffUtc = DateTime.UtcNow - GetRetentionPeriod();
        var deletedCount = 0;

        foreach (var directory in Directory.EnumerateDirectories(requestsDirectory))
        {
            var name = Path.GetFileName(directory);

            if (!Guid.TryParseExact(name, "N", out _) ||
                Directory.GetLastWriteTimeUtc(directory) > cutoffUtc)
            {
                continue;
            }

            Directory.Delete(directory, recursive: true);
            deletedCount++;
        }

        if (deletedCount > 0)
        {
            logger.LogInformation(
                "Deleted {DeletedCount} expired generator artifact directories.",
                deletedCount);
        }
    }

    private static TimeSpan GetRetentionPeriod()
    {
        const int defaultHours = 26;

        var configured = Environment.GetEnvironmentVariable(
            "WESELAJA_GENERATOR_RETENTION_HOURS");

        return int.TryParse(configured, out var hours)
            ? TimeSpan.FromHours(Math.Clamp(hours, 24, 168))
            : TimeSpan.FromHours(defaultHours);
    }
}
