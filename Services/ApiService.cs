using System.Net.Http;
using System.Text.Json;
using MODUPDATER.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MODUPDATER.Services;

public class ApiService
{
    private readonly HttpClient _client = new();

    public async Task<bool> PingServer(string url)
    {
        try
        {
            var response = await _client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<VersionInfo>?> GetVersionManifest(string url)
    {
        try
        {
            var json = await _client.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<VersionInfo>>(json, options);
        }
        catch
        {
            return null;
        }
    }
    public async Task<bool> IsPortOpen(string host, int port, int timeoutMilliseconds = 2000)
    {
        using var client = new TcpClient();
        try
        {
            var connectTask = client.ConnectAsync(host, port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(timeoutMilliseconds));

            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
