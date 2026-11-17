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
    string downloadFileName) : IResult
{
    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.ContentType = "application/zip";
        context.Response.Headers.ContentDisposition =
            $"attachment; filename=\"{downloadFileName}\"";

        using var archive = new ZipArchive(
            context.Response.Body,
            ZipArchiveMode.Create,
            leaveOpen: true);

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
