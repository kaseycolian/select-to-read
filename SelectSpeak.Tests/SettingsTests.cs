using SelectSpeak.Config;
using SelectSpeak.Theming;

namespace SelectSpeak.Tests;

public class SettingsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        var s = new Settings();
        Assert.Equal("Ctrl+Alt+S", s.SpeakHotkey);
        Assert.Equal("Ctrl+Alt+X", s.StopHotkey);
        Assert.False(s.AutoSpeak);
        Assert.Equal(0, s.Rate);
        Assert.Equal(100, s.Volume);
        Assert.Equal(BuiltInThemes.DefaultThemeName, s.ActiveTheme);
        Assert.Empty(s.CustomThemes);
    }

    [Fact]
    public void ToJson_FromJson_RoundTrips()
    {
        var original = new Settings
        {
            VoiceId = "voice-123",
            Rate = 3,
            Volume = 80,
            SpeakHotkey = "Ctrl+Shift+R",
            StopHotkey = "Ctrl+Shift+Q",
            AutoSpeak = true,
            ActiveTheme = "My Neon",
            CustomThemes = { new Theme { Name = "My Neon", Palette = new ThemePalette { Background = "#101010" } } },
        };

        var restored = Settings.FromJson(original.ToJson());

        Assert.Equal("voice-123", restored.VoiceId);
        Assert.Equal(3, restored.Rate);
        Assert.Equal(80, restored.Volume);
        Assert.Equal("Ctrl+Shift+R", restored.SpeakHotkey);
        Assert.True(restored.AutoSpeak);
        Assert.Equal("My Neon", restored.ActiveTheme);
        Assert.Single(restored.CustomThemes);
        Assert.Equal("#101010", restored.CustomThemes[0].Palette.Background);
    }

    [Fact]
    public void FromJson_MissingFields_FallBackToDefaults()
    {
        var restored = Settings.FromJson("""{ "volume": 55 }""");
        Assert.Equal(55, restored.Volume);
        Assert.Equal("Ctrl+Alt+S", restored.SpeakHotkey);       // default preserved
        Assert.Equal(BuiltInThemes.DefaultThemeName, restored.ActiveTheme);
        Assert.NotNull(restored.CustomThemes);
    }

    [Fact]
    public void FromJson_Malformed_ReturnsDefaults()
    {
        var restored = Settings.FromJson("{ not valid json ");
        Assert.Equal(100, restored.Volume);
        Assert.Equal("Ctrl+Alt+S", restored.SpeakHotkey);
    }

    [Fact]
    public void Normalize_ClampsRangesAndRepairsNulls()
    {
        var s = new Settings { Rate = 99, Volume = -20, SpeakHotkey = "  ", ActiveTheme = "" };
        s.Normalize();
        Assert.Equal(10, s.Rate);
        Assert.Equal(0, s.Volume);
        Assert.Equal("Ctrl+Alt+S", s.SpeakHotkey);
        Assert.Equal(BuiltInThemes.DefaultThemeName, s.ActiveTheme);
    }

    [Fact]
    public void SaveTo_LoadFrom_RoundTripsThroughDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"selectspeak-test-{Guid.NewGuid():N}.json");
        try
        {
            new Settings { Volume = 42, AutoSpeak = true }.SaveTo(path);
            var loaded = Settings.LoadFrom(path);
            Assert.Equal(42, loaded.Volume);
            Assert.True(loaded.AutoSpeak);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LoadFrom_MissingFile_ReturnsDefaults()
    {
        var loaded = Settings.LoadFrom(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json"));
        Assert.Equal(100, loaded.Volume);
    }
}
