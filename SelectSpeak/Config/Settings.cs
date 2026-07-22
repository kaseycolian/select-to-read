using System.Text.Json;
using SelectSpeak.Theming;

namespace SelectSpeak.Config;

/// <summary>
/// User preferences, persisted as JSON in %AppData%\SelectSpeak\settings.json.
/// Loading never throws: a missing/corrupt file or missing field falls back to
/// defaults so the app always starts.
/// </summary>
public sealed class Settings
{
    public string? VoiceId { get; set; }
    public int Rate { get; set; }            // -10..10, 0 = normal
    public int Volume { get; set; } = 100;   // 0..100
    public string SpeakHotkey { get; set; } = "Ctrl+Alt+S";
    public string StopHotkey { get; set; } = "Ctrl+Alt+X";
    public bool AutoSpeak { get; set; }
    public string ActiveTheme { get; set; } = BuiltInThemes.DefaultThemeName;
    public List<Theme> CustomThemes { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static string DirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SelectSpeak");

    public static string FilePath => Path.Combine(DirectoryPath, "settings.json");

    public static Settings Load() => LoadFrom(FilePath);

    public static Settings LoadFrom(string path)
    {
        try
        {
            if (File.Exists(path))
                return FromJson(File.ReadAllText(path));
        }
        catch
        {
            // Corrupt file, permission error, etc. — fall through to defaults.
        }
        return new Settings();
    }

    public static Settings FromJson(string json)
    {
        try
        {
            var loaded = JsonSerializer.Deserialize<Settings>(json, JsonOptions);
            if (loaded is null) return new Settings();
            loaded.Normalize();
            return loaded;
        }
        catch
        {
            return new Settings();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public void Save() => SaveTo(FilePath);

    public void SaveTo(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, ToJson());
    }

    /// <summary>Clamp ranges and repair nulls after deserialization.</summary>
    public void Normalize()
    {
        Rate = Math.Clamp(Rate, -10, 10);
        Volume = Math.Clamp(Volume, 0, 100);
        CustomThemes ??= new List<Theme>();
        if (string.IsNullOrWhiteSpace(SpeakHotkey)) SpeakHotkey = "Ctrl+Alt+S";
        if (string.IsNullOrWhiteSpace(StopHotkey)) StopHotkey = "Ctrl+Alt+X";
        if (string.IsNullOrWhiteSpace(ActiveTheme)) ActiveTheme = BuiltInThemes.DefaultThemeName;
        foreach (var t in CustomThemes)
            t.IsBuiltIn = false;
    }
}
