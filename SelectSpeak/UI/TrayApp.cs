using SelectSpeak.Config;
using SelectSpeak.Input;
using SelectSpeak.Speech;
using SelectSpeak.Theming;

namespace SelectSpeak.UI;

/// <summary>
/// The application host: owns the tray icon/menu and wires selection capture, speech,
/// hotkeys, auto-speak, theming, and the reading overlay together.
/// </summary>
public sealed class TrayApp : ApplicationContext
{
    private const int HotkeySpeak = 1;
    private const int HotkeyStop = 2;

    private readonly Settings _settings;
    private readonly SpeechService _speech;
    private readonly SelectionCapture _capture;
    private readonly ThemeService _themes;
    private readonly HotkeyManager _hotkeys;
    private readonly AutoSpeakWatcher _autoWatcher;
    private readonly ReadingOverlay _overlay;
    private readonly ContextMenuStrip _menu = new();
    private readonly NotifyIcon _tray = new() { Visible = true };
    private readonly SynchronizationContext _ui;

    private ToolStripMenuItem _autoItem = null!;
    private Icon? _trayIcon;
    private SettingsForm? _settingsForm;
    private int _capturing; // 0/1 guard so only one selection capture runs at a time

    public TrayApp()
    {
        _ui = SynchronizationContext.Current ?? new SynchronizationContext();
        _settings = Settings.Load();
        _speech = new SpeechService();
        _capture = new SelectionCapture();
        _themes = new ThemeService(_settings, _settings.Save);
        _hotkeys = new HotkeyManager();
        _autoWatcher = new AutoSpeakWatcher();
        _overlay = new ReadingOverlay();
        _ = _overlay.Handle; // realize the window now so the first read doesn't pay creation cost

        _speech.SpeakStarted += (_, _) => _ui.Post(_ => _overlay.BeginReading(), null);
        _speech.SpeakEnded += (_, _) => _ui.Post(_ => _overlay.EndReading(), null);

        _hotkeys.Pressed += OnHotkey;
        _autoWatcher.SelectionFinished += () => _ = SpeakSelectionAsync(interrupt: false);
        _themes.ThemeChanged += (_, _) => ApplyTheme();

        BuildMenu();
        ApplyTheme();
        ApplyRuntime();

        _ = _speech.WarmUpAsync(); // prime the audio pipeline so the first read has no start-up lag
    }

    private void BuildMenu()
    {
        var speak = new ToolStripMenuItem("Speak selection", null, (_, _) => _ = SpeakSelectionAsync(interrupt: true));
        var stop = new ToolStripMenuItem("Stop", null, (_, _) => StopSpeaking());
        _autoItem = new ToolStripMenuItem("Auto-speak on selection", null, (_, _) => ToggleAutoSpeak())
        {
            Checked = _settings.AutoSpeak,
        };
        var settings = new ToolStripMenuItem("Settings…", null, (_, _) => OpenSettings());
        var exit = new ToolStripMenuItem("Exit", null, (_, _) => ExitApp());

        _menu.Items.AddRange(new ToolStripItem[]
        {
            speak, stop, _autoItem, new ToolStripSeparator(), settings, new ToolStripSeparator(), exit,
        });

        _tray.ContextMenuStrip = _menu;
        _tray.DoubleClick += (_, _) => OpenSettings();
    }

    private void OnHotkey(int id)
    {
        if (id == HotkeySpeak) _ = SpeakSelectionAsync(interrupt: true);
        else if (id == HotkeyStop) StopSpeaking();
    }

    /// <param name="interrupt">
    /// Explicit triggers (Speak hotkey / menu) stop current reading immediately. Auto-speak
    /// passes false so that deselecting text (to read along) or clicking elsewhere does NOT
    /// cut off what's currently being read — only a genuinely new selection interrupts it.
    /// </param>
    private async Task SpeakSelectionAsync(bool interrupt)
    {
        // Only one capture at a time. Overlapping captures (easy to trigger in auto-speak mode)
        // race on the clipboard and cause wrong/stale text to be read.
        if (Interlocked.Exchange(ref _capturing, 1) == 1)
        {
            Diagnostics.DebugLog.Write("trigger: skipped (capture already in progress)");
            return;
        }

        try
        {
            Diagnostics.DebugLog.Write($"trigger: begin (interrupt={interrupt})");

            if (interrupt)
            {
                _speech.Cancel();
                _overlay.EndReading();
            }

            var text = await _capture.CaptureSelectedTextAsync();
            if (text is null)
            {
                // No new selection (e.g. the user deselected to read along) — leave current reading playing.
                Diagnostics.DebugLog.Write("trigger: no new selection — leaving current reading alone");
                return;
            }

            // A new selection: SpeakAsync stops whatever is playing and reads the new text.
            await _speech.SpeakAsync(text, _settings.VoiceId, _settings.Rate, _settings.Volume);
        }
        catch (Exception ex)
        {
            // Ignore transient capture/synthesis failures — the tool should never crash the desktop.
            Diagnostics.DebugLog.Write($"trigger: EXCEPTION {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _capturing, 0);
        }
    }

    private void StopSpeaking()
    {
        _speech.Cancel();
        _overlay.EndReading();
    }

    private void ToggleAutoSpeak()
    {
        _settings.AutoSpeak = !_settings.AutoSpeak;
        _settings.Save();
        ApplyRuntime();
    }

    /// <summary>Re-register hotkeys and sync auto-speak from current settings, everywhere it's shown.</summary>
    private void ApplyRuntime()
    {
        _autoItem.Checked = _settings.AutoSpeak;
        _autoWatcher.Enabled = _settings.AutoSpeak;
        _overlay.SetPersistent(_settings.AutoSpeak);
        _settingsForm?.SyncAutoSpeak(_settings.AutoSpeak); // keep an open Settings dialog in sync

        RegisterHotkey(HotkeySpeak, _settings.SpeakHotkey);
        RegisterHotkey(HotkeyStop, _settings.StopHotkey);
    }

    private void RegisterHotkey(int id, string spec)
    {
        _hotkeys.Unregister(id);
        if (HotkeyCombo.TryParse(spec, out var combo) && combo is not null && !_hotkeys.TryRegister(id, combo))
            ShowBalloon("Hotkey unavailable", $"'{combo}' is in use by another app. Change it in Settings.");
    }

    private void ApplyTheme()
    {
        var palette = _themes.Palette;
        _menu.Renderer = new NeonMenuRenderer(palette);
        _overlay.ApplyPalette(palette);

        var newIcon = IconFactory.CreateTrayIcon(palette);
        _tray.Icon = newIcon;
        _trayIcon?.Dispose();
        _trayIcon = newIcon;
        _tray.Text = "SelectSpeak";
    }

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }
        _settingsForm = new SettingsForm(_settings, _speech, _themes, _hotkeys.CanRegister, ApplyRuntime);
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void ShowBalloon(string title, string text) =>
        _tray.ShowBalloonTip(4000, title, text, ToolTipIcon.Warning);

    private void ExitApp()
    {
        _tray.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeys.Dispose();
            _autoWatcher.Dispose();
            _speech.Dispose();
            _overlay.Dispose();
            _menu.Dispose();
            _tray.Dispose();
            _trayIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
