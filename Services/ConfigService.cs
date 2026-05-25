using System.IO;
using System.Text.Json;
using MODUPDATER.Config;

namespace MODUPDATER.Services;

public class ConfigService
{
    private const string CONFIG_PATH = "config.json";

    public AppConfig Load()
    {
        // Create a config.json based on the AppConfig.cs
        if (!File.Exists(CONFIG_PATH))
        {
            var defaultConfig = new AppConfig();
            Save(defaultConfig);

            return defaultConfig;
        }

        string json = File.ReadAllText(CONFIG_PATH);

        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        string json = JsonSerializer.Serialize(
            config,
            new JsonSerializerOptions { WriteIndented = true }
        );

        File.WriteAllText(CONFIG_PATH, json);
    }
}
