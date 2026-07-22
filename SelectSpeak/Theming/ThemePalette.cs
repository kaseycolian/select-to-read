using System.Drawing;
using System.Text.Json.Serialization;

namespace SelectSpeak.Theming;

/// <summary>
/// A data-driven set of color roles. Colors are stored as "#RRGGBB" strings so a
/// palette round-trips cleanly through JSON; <c>*Color</c> getters project them to
/// <see cref="Color"/> for WinForms. UI code references roles, never literal colors.
/// </summary>
public sealed class ThemePalette
{
    public string Background { get; set; } = "#0D0B1A";
    public string Surface { get; set; } = "#171331";
    public string SurfaceAlt { get; set; } = "#221A46";
    public string TextPrimary { get; set; } = "#F5F3FF";
    public string TextMuted { get; set; } = "#A79FD6";
    public string Border { get; set; } = "#3A2E6E";
    public string AccentPink { get; set; } = "#FF3DBB";
    public string AccentGreen { get; set; } = "#39FF14";
    public string AccentPurple { get; set; } = "#B026FF";
    public string AccentBlue { get; set; } = "#20E7FF";
    public string Focus { get; set; } = "#FF3DBB";
    public string Disabled { get; set; } = "#5A5580";

    [JsonIgnore] public Color BackgroundColor => ColorUtil.FromHex(Background);
    [JsonIgnore] public Color SurfaceColor => ColorUtil.FromHex(Surface);
    [JsonIgnore] public Color SurfaceAltColor => ColorUtil.FromHex(SurfaceAlt);
    [JsonIgnore] public Color TextPrimaryColor => ColorUtil.FromHex(TextPrimary);
    [JsonIgnore] public Color TextMutedColor => ColorUtil.FromHex(TextMuted);
    [JsonIgnore] public Color BorderColor => ColorUtil.FromHex(Border);
    [JsonIgnore] public Color AccentPinkColor => ColorUtil.FromHex(AccentPink);
    [JsonIgnore] public Color AccentGreenColor => ColorUtil.FromHex(AccentGreen);
    [JsonIgnore] public Color AccentPurpleColor => ColorUtil.FromHex(AccentPurple);
    [JsonIgnore] public Color AccentBlueColor => ColorUtil.FromHex(AccentBlue);
    [JsonIgnore] public Color FocusColor => ColorUtil.FromHex(Focus);
    [JsonIgnore] public Color DisabledColor => ColorUtil.FromHex(Disabled);

    /// <summary>The four neon accents, in a stable order (used by the reading overlay).</summary>
    [JsonIgnore]
    public Color[] Accents => new[] { AccentPinkColor, AccentGreenColor, AccentPurpleColor, AccentBlueColor };

    public ThemePalette Clone() => (ThemePalette)MemberwiseClone();

    /// <summary>Every editable role, exposed generically so the editor and validation stay DRY.</summary>
    [JsonIgnore]
    public static IReadOnlyList<PaletteRole> Roles { get; } = new PaletteRole[]
    {
        new("Background",  "Background",   p => p.Background,  (p, v) => p.Background = v),
        new("Surface",     "Surface",      p => p.Surface,     (p, v) => p.Surface = v),
        new("SurfaceAlt",  "Surface (alt)",p => p.SurfaceAlt,  (p, v) => p.SurfaceAlt = v),
        new("TextPrimary", "Text",         p => p.TextPrimary, (p, v) => p.TextPrimary = v),
        new("TextMuted",   "Text (muted)", p => p.TextMuted,   (p, v) => p.TextMuted = v),
        new("Border",      "Border",       p => p.Border,      (p, v) => p.Border = v),
        new("AccentPink",  "Neon pink",    p => p.AccentPink,  (p, v) => p.AccentPink = v),
        new("AccentGreen", "Neon green",   p => p.AccentGreen, (p, v) => p.AccentGreen = v),
        new("AccentPurple","Neon purple",  p => p.AccentPurple,(p, v) => p.AccentPurple = v),
        new("AccentBlue",  "Neon blue",    p => p.AccentBlue,  (p, v) => p.AccentBlue = v),
        new("Focus",       "Focus/select", p => p.Focus,       (p, v) => p.Focus = v),
        new("Disabled",    "Disabled",     p => p.Disabled,    (p, v) => p.Disabled = v),
    };

    /// <summary>True when every role holds a valid hex string.</summary>
    public bool IsValid(out string? error)
    {
        foreach (var role in Roles)
        {
            if (!ColorUtil.IsValidHex(role.Get(this)))
            {
                error = $"{role.DisplayName} has an invalid color: '{role.Get(this)}'.";
                return false;
            }
        }
        error = null;
        return true;
    }
}

/// <summary>Descriptor letting UI/validation enumerate palette roles without hardcoding each one.</summary>
public sealed record PaletteRole(
    string Key,
    string DisplayName,
    Func<ThemePalette, string> Get,
    Action<ThemePalette, string> Set);
