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

    // Tudo aqui sao funcoes assincronas para permitir o funcionamento do launcher enquanto elas estao sendo processadas , fiquei com preguica de colocar em EN
    public async Task<bool> PingServer(string url)
    {
        try
        {
            var response = await _client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        { return false; }
    }

    public async Task<List<VersionInfo>?> GetVersionManifest(string url)
    {
        try
        {
            var json = await _client.GetStringAsync(url);

            var options = new JsonSerializerOptions
            { PropertyNameCaseInsensitive = true };

            return JsonSerializer.Deserialize<List<VersionInfo>>(json, options);
        }
        catch
        { return null; }
    }

    // Usada tanto pra pingar o web server quanto o spt server
    public async Task<bool> IsPortOpen(string host, int port, int timeout = 2000)
    {
        using var client = new TcpClient();
        try
        {
            var connectTask = client.ConnectAsync(host, port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(timeout));

            return completedTask == connectTask && client.Connected;
        }
        catch
        { return false; }
    }
}
