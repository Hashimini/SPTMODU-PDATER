using System;
namespace MODUPDATER.Config;

public class AppConfig
{
    public string ServerIp { get; set; } = "http://127.0.0.1:8080/versions.json";
    public string SptPath { get; set; } = @"C:\SPT";

    public string GetHost()
    {
        var uri = new Uri(ServerIp);
        return uri.Host;
    }

    public int GetPort()
    {
        var uri = new Uri(ServerIp);
        return uri.Port;
    }
}
