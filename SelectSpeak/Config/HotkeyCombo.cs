using System.Text;

namespace SelectSpeak.Config;

/// <summary>Win32 hotkey modifier flags (match MOD_* in RegisterHotKey).</summary>
[Flags]
public enum HotModifiers : uint
{
    None = 0x0000,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
}

/// <summary>
/// A global hotkey as modifiers + a virtual key, with round-trip parsing to/from
/// display strings like "Ctrl+Alt+S". Keys is <see cref="System.Windows.Forms.Keys"/>.
/// </summary>
public sealed class HotkeyCombo : IEquatable<HotkeyCombo>
{
    public HotModifiers Modifiers { get; }
    public Keys Key { get; }

    public HotkeyCombo(HotModifiers modifiers, Keys key)
    {
        if (key == Keys.None)
            throw new ArgumentException("A hotkey must include a non-modifier key.", nameof(key));
        Modifiers = modifiers;
        Key = key;
    }

    /// <summary>Modifier bits for RegisterHotKey. MOD_NOREPEAT (0x4000) suppresses auto-repeat.</summary>
    public uint Win32Modifiers => (uint)Modifiers | 0x4000;

    public uint VirtualKey => (uint)Key;

    public static bool TryParse(string? text, out HotkeyCombo? combo)
    {
        combo = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var mods = HotModifiers.None;
        Keys key = Keys.None;

        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    mods |= HotModifiers.Control; break;
                case "alt":
                    mods |= HotModifiers.Alt; break;
                case "shift":
                    mods |= HotModifiers.Shift; break;
                case "win":
                case "windows":
                case "meta":
                    mods |= HotModifiers.Win; break;
                default:
                    if (!TryParseKey(raw, out key)) return false;
                    break;
            }
        }

        if (key == Keys.None) return false;
        combo = new HotkeyCombo(mods, key);
        return true;
    }

    public static HotkeyCombo Parse(string text) =>
        TryParse(text, out var c) && c is not null
            ? c
            : throw new FormatException($"Invalid hotkey: '{text}'.");

    private static bool TryParseKey(string token, out Keys key)
    {
        key = Keys.None;
        if (token.Length == 1)
        {
            char ch = char.ToUpperInvariant(token[0]);
            if (ch is >= 'A' and <= 'Z') { key = (Keys)ch; return true; }
            if (ch is >= '0' and <= '9') { key = Keys.D0 + (ch - '0'); return true; }
        }
        // F1..F24, named keys (Space, Enter, ...), or "D1"/"NumPad0" style tokens.
        return Enum.TryParse(token, ignoreCase: true, out key) && key != Keys.None;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Modifiers.HasFlag(HotModifiers.Control)) sb.Append("Ctrl+");
        if (Modifiers.HasFlag(HotModifiers.Alt)) sb.Append("Alt+");
        if (Modifiers.HasFlag(HotModifiers.Shift)) sb.Append("Shift+");
        if (Modifiers.HasFlag(HotModifiers.Win)) sb.Append("Win+");
        sb.Append(KeyName(Key));
        return sb.ToString();
    }

    private static string KeyName(Keys key)
    {
        if (key is >= Keys.A and <= Keys.Z) return ((char)key).ToString();
        if (key is >= Keys.D0 and <= Keys.D9) return ((char)('0' + (key - Keys.D0))).ToString();
        return key.ToString();
    }

    public bool Equals(HotkeyCombo? other) =>
        other is not null && Modifiers == other.Modifiers && Key == other.Key;

    public override bool Equals(object? obj) => Equals(obj as HotkeyCombo);
    public override int GetHashCode() => HashCode.Combine(Modifiers, Key);
}
