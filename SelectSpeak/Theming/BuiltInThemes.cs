namespace SelectSpeak.Theming;

/// <summary>The two shipped skate-rink themes. Fresh instances each call so callers can't mutate the originals.</summary>
public static class BuiltInThemes
{
    public const string DarkName = "Skate Rink Dark";
    public const string LightName = "Skate Rink Light";
    public const string DefaultThemeName = DarkName;

    /// <summary>90s skating-rink neon on near-black. The default.</summary>
    public static Theme Dark => new()
    {
        Name = DarkName,
        IsBuiltIn = true,
        Palette = new ThemePalette
        {
            Background = "#0D0B1A",
            Surface = "#171331",
            SurfaceAlt = "#221A46",
            TextPrimary = "#F5F3FF",
            TextMuted = "#A79FD6",
            Border = "#3A2E6E",
            AccentPink = "#FF3DBB",
            AccentGreen = "#39FF14",
            AccentPurple = "#B026FF",
            AccentBlue = "#20E7FF",
            Focus = "#FF3DBB",
            Disabled = "#5A5580",
        },
    };

    /// <summary>Same neon tones (deepened for contrast) on a light lavender ground.</summary>
    public static Theme Light => new()
    {
        Name = LightName,
        IsBuiltIn = true,
        Palette = new ThemePalette
        {
            Background = "#F4F1FF",
            Surface = "#FFFFFF",
            SurfaceAlt = "#ECE6FF",
            TextPrimary = "#1A1533",
            TextMuted = "#6B6392",
            Border = "#C9BEEF",
            AccentPink = "#E5008E",
            AccentGreen = "#12A800",
            AccentPurple = "#7A00E6",
            AccentBlue = "#0091B3",
            Focus = "#E5008E",
            Disabled = "#B4ACD6",
        },
    };

    public static IReadOnlyList<Theme> All => new[] { Dark, Light };

    public static bool IsBuiltInName(string? name) =>
        string.Equals(name, DarkName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, LightName, StringComparison.OrdinalIgnoreCase);
}
