using System;
namespace MODUPDATER.Config;

public class AppConfig
{
    //The names and values present on the config.json
    public string ServerIp { get; set; } = "http://127.0.0.1:8080/versions.json";
    public string SptPath { get; set; } = @"C:\SPT";
    public string InstalledVersion { get; set; } = "0.0.0";

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
