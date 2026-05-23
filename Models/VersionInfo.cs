using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MODUPDATER.Models;

public class VersionInfo
{
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("Download")]
    public string DownloadUrl { get; set; } = string.Empty;

    public string Changelog { get; set; } = string.Empty;

    [JsonPropertyName("files_to_delete")]
    public List<string> FilesToDelete { get; set; } = new();
}
