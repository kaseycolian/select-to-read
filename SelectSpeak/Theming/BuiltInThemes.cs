namespace SelectSpeak.Theming;

/// <summary>
/// The shipped brand themes, ported from the theme-service (v0.1.1, families: Rink Classic,
/// Midnight Arcade, Hot Neon, Synthwave Sunset, Acid Arcade — each dark + light). Token→role
/// mapping: --bg→Background, --bg-panel→Surface, --bg-elevated→SurfaceAlt, --text→TextPrimary,
/// --text-muted→TextMuted, --border→Border, --focus-ring→Focus, --accent-*→Accent*,
/// --border-strong→Disabled. Values are copied verbatim (AA-validated); see THEME-SERVICE.md.
/// </summary>
public static class BuiltInThemes
{
    public const string DarkName = "Rink Classic Dark";
    public const string LightName = "Rink Classic Light";
    public const string DefaultThemeName = DarkName;

    private static Theme Make(
        string name,
        string bg, string surface, string surfaceAlt, string text, string muted, string border,
        string pink, string green, string purple, string blue, string focus, string disabled) => new()
    {
        Name = name,
        IsBuiltIn = true,
        Palette = new ThemePalette
        {
            Background = "#" + bg,
            Surface = "#" + surface,
            SurfaceAlt = "#" + surfaceAlt,
            TextPrimary = "#" + text,
            TextMuted = "#" + muted,
            Border = "#" + border,
            AccentPink = "#" + pink,
            AccentGreen = "#" + green,
            AccentPurple = "#" + purple,
            AccentBlue = "#" + blue,
            Focus = "#" + focus,
            Disabled = "#" + disabled,
        },
    };

    // name                     bg      surface elevated text    muted   border  pink    green   purple  blue    focus   disabled(border-strong)
    public static IReadOnlyList<Theme> All { get; } = new[]
    {
        Make("Rink Classic Dark",     "070110","110620","1b0c30","f3ecff","bcadde","34205a","ff2ec4","5bff3a","b57fff","3ceaff","3ceaff","8064c0"),
        Make("Midnight Arcade Dark",  "06081a","0e132e","171d40","eaf0ff","a9b6e6","25305e","f060c4","54ffc4","a888f5","5cc8ff","5cc8ff","6072c4"),
        Make("Hot Neon Dark",         "04000a","14041f","1e0a30","ffeffb","e0a6cf","351042","ff3ec8","6bff45","cf7bff","22e0ff","39ff14","b45ab0"),
        Make("Synthwave Sunset Dark", "160821","241033","331847","ffeede","e3b39f","4a2a55","ff5d8f","ffb03a","c17bff","4ad8ff","ffb03a","aa6494"),
        Make("Acid Arcade Dark",      "0d0f12","171b20","222831","f2fff4","a8c0a8","2c3830","ff4de0","c6ff2e","b98cff","38f0ff","c6ff2e","5f7d62"),

        Make("Rink Classic Light",    "f6f1fc","ffffff","ffffff","1c0f2e","5f5080","e0d3f0","b60f86","1f7d2f","6d28d9","0a6a9e","7b2ff0","9070c0"),
        Make("Midnight Arcade Light", "eef2fb","ffffff","ffffff","0e1430","4d5880","d3dcf0","b81e7f","0f7a63","5b3ad0","1257c4","2563eb","7082bc"),
        Make("Hot Neon Light",        "fdf0fa","ffffff","ffffff","2a0722","7a4a6d","f2d4ea","c8127f","1e7714","8b1fd0","0a72a8","c8127f","bd5ea2"),
        Make("Synthwave Sunset Light","fdf1ea","ffffff","ffffff","2e1220","875363","f2dccb","c81e5c","9c5000","7d2fc8","0a6f9e","c23d78","bd7860"),
        Make("Acid Arcade Light",     "f4f7ee","ffffff","ffffff","111a10","556b52","dde8d3","c01e9c","5a7a00","7028d0","0a7099","5a7a00","6f9066"),
    };

    /// <summary>The default dark theme (Rink Classic Dark) — used as the fallback palette.</summary>
    public static Theme Dark => All.First(t => t.Name == DarkName);

    /// <summary>The default light theme (Rink Classic Light).</summary>
    public static Theme Light => All.First(t => t.Name == LightName);

    public static bool IsBuiltInName(string? name) =>
        All.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
