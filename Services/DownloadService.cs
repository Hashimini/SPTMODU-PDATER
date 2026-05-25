using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MODUPDATER.Services;

public class DownloadService
{
    private readonly HttpClient _client = new();
    private const int MAX_CONCURRENCY = 4;

    public async Task DownloadFile(string url, string outputPath, IProgress<double>? progress = null)
    {
        using var responseMessage = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
        responseMessage.EnsureSuccessStatusCode();
        var totalBytes = responseMessage.Content.Headers.ContentLength ?? throw new Exception("Não foi possível determinar o tamanho do patch.");

        using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        { fs.SetLength(totalBytes); }

        long chunkSize = totalBytes / MAX_CONCURRENCY;
        var tasks = new List<Task>();
        var progressTracker = new long[MAX_CONCURRENCY];

        for (int i = 0; i < MAX_CONCURRENCY; i++)
        {
            int index = i;
            long start = index * chunkSize;
            long end = (index == MAX_CONCURRENCY - 1) ? totalBytes - 1 : start + chunkSize - 1;

            tasks.Add(Task.Run(async () =>
            {
                using var chunkClient = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

                using var response = await chunkClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync();

                using var fileStream = new FileStream(outputPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 131072, true);
                fileStream.Position = start;

                var buffer = new byte[131072];
                int readBytes;

                while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, readBytes);

                    lock (progressTracker)
                    {
                        progressTracker[index] += readBytes;
                        long totalReadBytes = progressTracker.Sum();
                        var percentage = (double)totalReadBytes / totalBytes * 100.0;
                        progress?.Report(percentage);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);
        progress?.Report(100.0);
    }
}
