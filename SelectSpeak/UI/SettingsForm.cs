using SelectSpeak.Config;
using SelectSpeak.Speech;
using SelectSpeak.Theming;

namespace SelectSpeak.UI;

/// <summary>
/// The settings window: voice, speed, volume, hotkeys, auto-speak, and theme selection/editing.
/// Changes are saved immediately. <paramref name="validateHotkey"/> tests whether a new combo is
/// free; <paramref name="onRuntimeChanged"/> re-applies hotkeys/auto-speak in the host.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly Settings _settings;
    private readonly SpeechService _speech;
    private readonly ThemeService _themes;
    private readonly Func<HotkeyCombo, bool> _validateHotkey;
    private readonly Action _onRuntimeChanged;

    private readonly ComboBox _voiceBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TrackBar _rate = new() { Minimum = -10, Maximum = 10, TickFrequency = 5 };
    private readonly TrackBar _volume = new() { Minimum = 0, Maximum = 100, TickFrequency = 10 };
    private readonly Label _rateValue = new() { AutoSize = true };
    private readonly Label _volumeValue = new() { AutoSize = true };
    private readonly TextBox _speakHotkey = new() { ReadOnly = true };
    private readonly TextBox _stopHotkey = new() { ReadOnly = true };
    private readonly CheckBox _autoSpeak = new() { Text = "Auto-speak whenever I select text" };
    private readonly ComboBox _themeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _status = new() { AutoSize = false };
    private bool _suppressAutoEvent;

    public SettingsForm(
        Settings settings,
        SpeechService speech,
        ThemeService themes,
        Func<HotkeyCombo, bool> validateHotkey,
        Action onRuntimeChanged)
    {
        _settings = settings;
        _speech = speech;
        _themes = themes;
        _validateHotkey = validateHotkey;
        _onRuntimeChanged = onRuntimeChanged;

        Text = "SelectSpeak — Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 430);

        BuildLayout();
        LoadValues();

        _themes.ThemeChanged += OnThemeChanged;
        FormClosed += (_, _) => _themes.ThemeChanged -= OnThemeChanged;

        ApplyTheme();
    }

    /// <summary>Reflect an auto-speak change made elsewhere (e.g. the tray menu) without re-firing the toggle logic.</summary>
    public void SyncAutoSpeak(bool value)
    {
        if (InvokeRequired) { BeginInvoke(() => SyncAutoSpeak(value)); return; }
        _suppressAutoEvent = true;
        _autoSpeak.Checked = value;
        _suppressAutoEvent = false;
    }

    private void BuildLayout()
    {
        int y = 16;

        AddLabel("Voice", 16, y);
        _voiceBox.SetBounds(120, y - 3, 300, 24);
        _voiceBox.SelectedIndexChanged += (_, _) => SaveVoice();
        Controls.Add(_voiceBox);
        y += 40;

        AddLabel("Speed", 16, y);
        _rate.SetBounds(115, y - 6, 240, 40);
        _rate.ValueChanged += (_, _) => { _rateValue.Text = _rate.Value.ToString(); Save(); };
        _rateValue.Location = new Point(365, y);
        Controls.Add(_rate);
        Controls.Add(_rateValue);
        y += 48;

        AddLabel("Volume", 16, y);
        _volume.SetBounds(115, y - 6, 240, 40);
        _volume.ValueChanged += (_, _) => { _volumeValue.Text = _volume.Value.ToString(); Save(); };
        _volumeValue.Location = new Point(365, y);
        Controls.Add(_volume);
        Controls.Add(_volumeValue);
        y += 52;

        AddLabel("Speak hotkey", 16, y);
        _speakHotkey.SetBounds(120, y - 3, 200, 24);
        _speakHotkey.KeyDown += (s, e) => CaptureHotkey(e, isSpeak: true);
        Controls.Add(_speakHotkey);
        y += 36;

        AddLabel("Stop hotkey", 16, y);
        _stopHotkey.SetBounds(120, y - 3, 200, 24);
        _stopHotkey.KeyDown += (s, e) => CaptureHotkey(e, isSpeak: false);
        Controls.Add(_stopHotkey);
        y += 40;

        _autoSpeak.SetBounds(120, y, 300, 24);
        _autoSpeak.CheckedChanged += (_, _) =>
        {
            if (_suppressAutoEvent) return;
            _settings.AutoSpeak = _autoSpeak.Checked;
            Save();
            _onRuntimeChanged();
        };
        Controls.Add(_autoSpeak);
        y += 40;

        AddLabel("Theme", 16, y);
        _themeBox.SetBounds(120, y - 3, 200, 24);
        _themeBox.SelectedIndexChanged += (_, _) => SaveTheme();
        Controls.Add(_themeBox);

        var editTheme = new Button { Text = "New / Edit…" };
        editTheme.SetBounds(328, y - 4, 92, 26);
        editTheme.Click += (_, _) => OpenThemeEditor();
        Controls.Add(editTheme);
        y += 44;

        _status.SetBounds(16, y, 404, 36);
        Controls.Add(_status);
        y += 40;

        var test = new Button { Text = "Test voice" };
        test.SetBounds(120, y, 120, 30);
        test.Click += (_, _) => _ = _speech.SpeakAsync(
            "The quick brown fox jumps over the lazy dog.", _settings.VoiceId, _settings.Rate, _settings.Volume);
        Controls.Add(test);

        var close = new Button { Text = "Close", DialogResult = DialogResult.OK };
        close.SetBounds(300, y, 120, 30);
        Controls.Add(close);
    }

    private void AddLabel(string text, int x, int y) =>
        Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y) });

    private void LoadValues()
    {
        _voiceBox.Items.Clear();
        var voices = _speech.GetVoices();
        foreach (var v in voices)
            _voiceBox.Items.Add(new VoiceItem(v));

        if (voices.Count == 0)
        {
            _voiceBox.Enabled = false;
            SetStatus("No speech voices are installed. Add voices in Windows Settings → Time & language → Speech.", error: true);
        }
        else
        {
            int idx = voices.ToList().FindIndex(v => v.Id == _settings.VoiceId);
            _voiceBox.SelectedIndex = idx >= 0 ? idx : 0;
        }

        _rate.Value = Math.Clamp(_settings.Rate, _rate.Minimum, _rate.Maximum);
        _rateValue.Text = _rate.Value.ToString();
        _volume.Value = Math.Clamp(_settings.Volume, _volume.Minimum, _volume.Maximum);
        _volumeValue.Text = _volume.Value.ToString();

        _speakHotkey.Text = _settings.SpeakHotkey;
        _stopHotkey.Text = _settings.StopHotkey;
        _autoSpeak.Checked = _settings.AutoSpeak;

        ReloadThemeList();
    }

    private void ReloadThemeList()
    {
        _themeBox.SelectedIndexChanged -= ThemeBoxGuard;
        _themeBox.Items.Clear();
        foreach (var t in _themes.AllThemes())
            _themeBox.Items.Add(t.Name);
        _themeBox.SelectedItem = _settings.ActiveTheme;
        if (_themeBox.SelectedIndex < 0 && _themeBox.Items.Count > 0)
            _themeBox.SelectedIndex = 0;
        _themeBox.SelectedIndexChanged += ThemeBoxGuard;
    }

    // Kept so add/remove of the handler in ReloadThemeList is symmetric.
    private void ThemeBoxGuard(object? sender, EventArgs e) => SaveTheme();

    private void SaveVoice()
    {
        if (_voiceBox.SelectedItem is VoiceItem item)
        {
            _settings.VoiceId = item.Voice.Id;
            Save();
        }
    }

    private void SaveTheme()
    {
        if (_themeBox.SelectedItem is string name && !string.Equals(name, _settings.ActiveTheme, StringComparison.Ordinal))
            _themes.SetActive(name); // fires ThemeChanged → OnThemeChanged re-themes this form
    }

    private void CaptureHotkey(KeyEventArgs e, bool isSpeak)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin)
            return; // wait for the non-modifier key

        var mods = HotModifiers.None;
        if (e.Control) mods |= HotModifiers.Control;
        if (e.Alt) mods |= HotModifiers.Alt;
        if (e.Shift) mods |= HotModifiers.Shift;

        if (mods == HotModifiers.None)
        {
            SetStatus("Include at least one modifier (Ctrl, Alt, or Shift).", error: true);
            return;
        }

        HotkeyCombo combo;
        try { combo = new HotkeyCombo(mods, e.KeyCode); }
        catch { return; }

        string current = isSpeak ? _settings.SpeakHotkey : _settings.StopHotkey;
        bool unchanged = string.Equals(combo.ToString(), current, StringComparison.OrdinalIgnoreCase);
        if (!unchanged && !_validateHotkey(combo))
        {
            SetStatus($"{combo} is already in use by another app. Pick another.", error: true);
            return;
        }

        if (isSpeak) { _settings.SpeakHotkey = combo.ToString(); _speakHotkey.Text = combo.ToString(); }
        else { _settings.StopHotkey = combo.ToString(); _stopHotkey.Text = combo.ToString(); }

        Save();
        _onRuntimeChanged();
        SetStatus($"{(isSpeak ? "Speak" : "Stop")} hotkey set to {combo}.", error: false);
    }

    private void OpenThemeEditor()
    {
        using var editor = new ThemeEditorForm(_themes, _settings.ActiveTheme);
        if (editor.ShowDialog(this) == DialogResult.OK && editor.SavedThemeName is { } saved)
        {
            ReloadThemeList();
            _themeBox.SelectedItem = saved;
            _themes.SetActive(saved);
            SetStatus($"Saved and applied theme '{saved}'.", error: false);
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ReloadThemeList();
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        _themes.Apply(this);
        Invalidate(true);
    }

    private void SetStatus(string message, bool error)
    {
        _status.Text = message;
        _status.ForeColor = error ? Color.FromArgb(255, 90, 90) : _themes.Palette.AccentGreenColor;
    }

    private void Save() => _settings.Save();

    private sealed class VoiceItem
    {
        public VoiceInfo Voice { get; }
        public VoiceItem(VoiceInfo voice) => Voice = voice;
        public override string ToString() => $"{Voice.DisplayName} ({Voice.Language})";
    }
}
