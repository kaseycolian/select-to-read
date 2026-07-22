using System.Drawing;
using SelectSpeak.Config;
using SelectSpeak.Theming;

namespace SelectSpeak.Tests;

public class ThemeTests
{
    [Theory]
    [InlineData("#FF3DBB", 255, 61, 187)]
    [InlineData("39FF14", 57, 255, 20)]
    [InlineData("#000000", 0, 0, 0)]
    public void ColorUtil_FromHex_ParsesRgb(string hex, int r, int g, int b)
    {
        var c = ColorUtil.FromHex(hex);
        Assert.Equal(Color.FromArgb(255, r, g, b).ToArgb(), c.ToArgb());
    }

    [Fact]
    public void ColorUtil_ToHex_FromHex_RoundTrips()
    {
        var c = Color.FromArgb(255, 176, 38, 255);
        Assert.Equal("#B026FF", ColorUtil.ToHex(c));
        Assert.Equal(c.ToArgb(), ColorUtil.FromHex(ColorUtil.ToHex(c)).ToArgb());
    }

    [Theory]
    [InlineData("not-a-color")]
    [InlineData("#12345")]
    [InlineData("")]
    public void ColorUtil_IsValidHex_RejectsBadInput(string hex)
    {
        Assert.False(ColorUtil.IsValidHex(hex));
    }

    [Fact]
    public void Palette_IsValid_TrueForBuiltIns_FalseForBadHex()
    {
        Assert.True(BuiltInThemes.Dark.Palette.IsValid(out _));
        Assert.True(BuiltInThemes.Light.Palette.IsValid(out _));

        var bad = BuiltInThemes.Dark.Palette.Clone();
        bad.AccentPink = "nope";
        Assert.False(bad.IsValid(out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Palette_SerializesRolesButNotComputedColors()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(BuiltInThemes.Dark.Palette);
        Assert.Contains("Background", json);
        Assert.Contains("#0D0B1A", json);
        Assert.DoesNotContain("BackgroundColor", json); // [JsonIgnore] getters excluded
    }

    [Fact]
    public void ThemeService_AllThemes_IncludesBuiltInsAndCustom()
    {
        var settings = new Settings();
        var svc = new ThemeService(settings, () => { });
        Assert.Equal(2, svc.AllThemes().Count);

        Assert.True(svc.AddOrUpdateCustom("Cosmic", new ThemePalette(), out _));
        Assert.Equal(3, svc.AllThemes().Count);
        Assert.Contains(svc.AllThemes(), t => t.Name == "Cosmic");
    }

    [Fact]
    public void ThemeService_AddCustom_RejectsBuiltInNameBlankAndBadPalette()
    {
        var svc = new ThemeService(new Settings(), () => { });

        Assert.False(svc.AddOrUpdateCustom(BuiltInThemes.DarkName, new ThemePalette(), out var e1));
        Assert.NotNull(e1);

        Assert.False(svc.AddOrUpdateCustom("   ", new ThemePalette(), out var e2));
        Assert.NotNull(e2);

        var bad = new ThemePalette { Background = "xyz" };
        Assert.False(svc.AddOrUpdateCustom("Broken", bad, out var e3));
        Assert.NotNull(e3);
    }

    [Fact]
    public void ThemeService_AddCustom_SameName_OverwritesNotDuplicates()
    {
        var svc = new ThemeService(new Settings(), () => { });
        svc.AddOrUpdateCustom("Neon", new ThemePalette { Background = "#111111" }, out _);
        svc.AddOrUpdateCustom("Neon", new ThemePalette { Background = "#222222" }, out _);

        var matches = svc.AllThemes().Where(t => t.Name == "Neon").ToList();
        Assert.Single(matches);
        Assert.Equal("#222222", matches[0].Palette.Background);
    }

    [Fact]
    public void ThemeService_SetActive_ResolvesActivePalette()
    {
        var settings = new Settings();
        var svc = new ThemeService(settings, () => { });

        svc.SetActive(BuiltInThemes.LightName);
        Assert.Equal(BuiltInThemes.LightName, svc.Active.Name);
        Assert.Equal(BuiltInThemes.Light.Palette.Background, svc.Palette.Background);

        // Unknown name is ignored (stays put).
        svc.SetActive("does-not-exist");
        Assert.Equal(BuiltInThemes.LightName, svc.Active.Name);
    }

    [Fact]
    public void ThemeService_RemoveCustom_ResetsActiveToDefault()
    {
        var settings = new Settings();
        var svc = new ThemeService(settings, () => { });
        svc.AddOrUpdateCustom("Temp", new ThemePalette(), out _);
        svc.SetActive("Temp");

        Assert.True(svc.RemoveCustom("Temp"));
        Assert.Equal(BuiltInThemes.DefaultThemeName, svc.Active.Name);
    }
}
