using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Text.Json;

namespace MODUPDATER.Lang;

public static class LangService
{
    private static Dictionary<string, string> _strings = [];

    public static void Load()
    {
        string culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        string file = culture switch
        { "pt" => "Langs/pt-BR.json", _ => "Langs/en.json" };

        if (!File.Exists(file))
        { file = "Langs/en.json"; }

        string json = File.ReadAllText(file);

        _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? [];
    }

    public static string Get(string key)
    {
        return _strings.TryGetValue(key, out string? value)
            ? value
            : $"[{key}]";
    }
}
