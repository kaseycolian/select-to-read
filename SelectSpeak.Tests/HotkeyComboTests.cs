using SelectSpeak.Config;

namespace SelectSpeak.Tests;

public class HotkeyComboTests
{
    [Theory]
    [InlineData("Ctrl+Alt+S", HotModifiers.Control | HotModifiers.Alt, Keys.S)]
    [InlineData("ctrl+alt+x", HotModifiers.Control | HotModifiers.Alt, Keys.X)]
    [InlineData("Ctrl+Shift+F5", HotModifiers.Control | HotModifiers.Shift, Keys.F5)]
    [InlineData("Alt+1", HotModifiers.Alt, Keys.D1)]
    [InlineData("Win+Space", HotModifiers.Win, Keys.Space)]
    public void Parse_ProducesExpectedModifiersAndKey(string text, HotModifiers mods, Keys key)
    {
        Assert.True(HotkeyCombo.TryParse(text, out var combo));
        Assert.NotNull(combo);
        Assert.Equal(mods, combo!.Modifiers);
        Assert.Equal(key, combo.Key);
    }

    [Theory]
    [InlineData(HotModifiers.Control | HotModifiers.Alt, Keys.S, "Ctrl+Alt+S")]
    [InlineData(HotModifiers.Alt, Keys.D1, "Alt+1")]
    [InlineData(HotModifiers.Control | HotModifiers.Shift, Keys.F5, "Ctrl+Shift+F5")]
    [InlineData(HotModifiers.Win, Keys.Space, "Win+Space")]
    public void ToString_FormatsCanonically(HotModifiers mods, Keys key, string expected)
    {
        Assert.Equal(expected, new HotkeyCombo(mods, key).ToString());
    }

    [Theory]
    [InlineData("Ctrl+Alt+S")]
    [InlineData("Ctrl+Shift+F12")]
    [InlineData("Alt+7")]
    public void ParseThenToString_RoundTrips(string text)
    {
        var combo = HotkeyCombo.Parse(text);
        Assert.Equal(text, combo.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+Alt")]     // no non-modifier key
    [InlineData("Ctrl+Alt+??")]  // unknown key token
    public void TryParse_ReturnsFalseForInvalidInput(string text)
    {
        Assert.False(HotkeyCombo.TryParse(text, out var combo));
        Assert.Null(combo);
    }

    [Fact]
    public void Win32Modifiers_IncludeNoRepeatFlag()
    {
        var combo = new HotkeyCombo(HotModifiers.Control, Keys.S);
        Assert.Equal(0x4000u, combo.Win32Modifiers & 0x4000u);
        Assert.Equal((uint)HotModifiers.Control, combo.Win32Modifiers & 0x000Fu);
    }

    [Fact]
    public void Constructor_RejectsKeyNone()
    {
        Assert.Throws<ArgumentException>(() => new HotkeyCombo(HotModifiers.Control, Keys.None));
    }
}
