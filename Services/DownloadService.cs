using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

namespace MODUPDATER.Services;

public class DownloadService
{
    private readonly HttpClient _client = new();

    public async Task DownloadFile(string url, string outputPath, IProgress<double>? progress = null)
    {
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        var totalReadBytes = 0L;
        int readBytes;

        while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, readBytes);
            totalReadBytes += readBytes;

            if (totalBytes != -1L && progress != null)
            {
                var percentage = (double)totalReadBytes / totalBytes * 100.0;
                progress.Report(percentage);
            }
        }

        progress?.Report(100.0);
    }
}
