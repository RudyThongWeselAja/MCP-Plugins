using System.IO.Compression;

using Microsoft.AspNetCore.Http;

namespace WeselAjaMcpServer.Services;

/// <summary>
/// Delivers a generated project as a completed ZIP archive. The archive is
/// staged in a temporary file so proxies receive a response with a known
/// Content-Length rather than a chunked archive that is still being written.
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
        var temporaryArchivePath = Path.Combine(
            Path.GetTempPath(),
            $"weselaja-download-{Guid.NewGuid():N}.zip");

        try
        {
            await using (var archiveFile = new FileStream(
                temporaryArchivePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                using (var archive = new ZipArchive(
                    archiveFile,
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
            }

            var archiveLength = new FileInfo(temporaryArchivePath).Length;

            context.Response.ContentType = "application/zip";
            context.Response.ContentLength = archiveLength;
            context.Response.Headers.ContentDisposition =
                $"attachment; filename=\"{downloadFileName}\"";

            await using var archiveInput = new FileStream(
                temporaryArchivePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            await archiveInput.CopyToAsync(
                context.Response.Body,
                context.RequestAborted);

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
        finally
        {
            try
            {
                File.Delete(temporaryArchivePath);
            }
            catch
            {
                // The OS will clean up a locked temporary file later.
            }
        }
    }
}
