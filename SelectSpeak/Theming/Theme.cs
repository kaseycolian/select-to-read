using System.Text.Json.Serialization;

namespace SelectSpeak.Theming;

/// <summary>A named palette. Built-in themes are read-only; custom themes are user-saved.</summary>
public sealed class Theme
{
    public string Name { get; set; } = "";

    public ThemePalette Palette { get; set; } = new();

    /// <summary>Runtime-only flag; not persisted (custom themes are the only ones stored).</summary>
    [JsonIgnore]
    public bool IsBuiltIn { get; set; }
}
