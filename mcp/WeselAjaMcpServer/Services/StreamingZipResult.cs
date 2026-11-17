using System.IO.Compression;

using Microsoft.AspNetCore.Http;

namespace WeselAjaMcpServer.Services;

/// <summary>
/// Streams a generated project as a ZIP archive without buffering the whole
/// archive in server memory.
/// </summary>
public sealed class StreamingZipResult(
    string projectDirectory,
    IReadOnlyList<string> files,
    string downloadFileName,
    Func<Task<bool>>? completeTicket = null,
    Func<Task>? releaseTicket = null) : IResult
{
    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.ContentType = "application/zip";
        context.Response.Headers.ContentDisposition =
            $"attachment; filename=\"{downloadFileName}\"";

        try
        {
            using (var archive = new ZipArchive(
                context.Response.Body,
                ZipArchiveMode.Create,
                leaveOpen: true))
            {
                foreach (var file in files)
                {
                    context.RequestAborted.ThrowIfCancellationRequested();

                    var relativePath = Path
                        .GetRelativePath(projectDirectory, file)
                        .Replace('\\', '/');

                    if (Path.IsPathRooted(relativePath) ||
                        relativePath.Split('/').Any(segment => segment == ".."))
                    {
                        continue;
                    }

                    var entry = archive.CreateEntry(
                        relativePath,
                        CompressionLevel.Fastest);

                    await using var input = new FileStream(
                        file,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 81920,
                        useAsync: true);

                    await using var output = entry.Open();

                    await input.CopyToAsync(
                        output,
                        context.RequestAborted);
                }
            }

            if (completeTicket is not null && !await completeTicket())
            {
                throw new InvalidOperationException(
                    "Download ticket could not be completed.");
            }
        }
        catch
        {
            if (releaseTicket is not null)
            {
                await releaseTicket();
            }

            throw;
        }
    }
}
