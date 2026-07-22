using System.Drawing;
using System.Globalization;

namespace SelectSpeak.Theming;

/// <summary>Parse/format helpers between "#RRGGBB" hex strings and <see cref="Color"/>.</summary>
public static class ColorUtil
{
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new FormatException("Hex color is empty.");

        var s = hex.Trim();
        if (s.StartsWith('#')) s = s[1..];

        if (s.Length == 6 &&
            byte.TryParse(s.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
            byte.TryParse(s.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
            byte.TryParse(s.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return Color.FromArgb(255, r, g, b);
        }

        throw new FormatException($"Invalid hex color: '{hex}'. Expected '#RRGGBB'.");
    }

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static bool IsValidHex(string? hex) => hex is not null && TryFromHex(hex, out _);

    public static bool TryFromHex(string hex, out Color color)
    {
        try { color = FromHex(hex); return true; }
        catch { color = Color.Black; return false; }
    }
}
