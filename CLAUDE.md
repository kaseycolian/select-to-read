# CLAUDE.md — SelectSpeak

Guidance for working in this repo. Keep it current when architecture or key decisions change.

## What this is

A system-wide "select-and-speak" reader for Windows 11. It lives in the system tray.
The user selects text in **any** app; a global hotkey (or an auto-speak-on-selection mode)
copies the selection via a simulated Ctrl+C, reads the clipboard, and speaks it with an
installed Windows voice. Not a browser extension, no DOM/element navigation. Has a themeable
UI (default 90s neon "Skate Rink Dark", plus a light theme and user-created custom themes).

## Stack / build

- **.NET 10** (current LTS). Main project TFM: `net10.0-windows10.0.19041.0` (the windows10
  TFM is required — it auto-references the WinRT projections used for speech).
- WinForms tray app, `OutputType=WinExe`. No external NuGet deps in the app project.
- Solution file is **`SelectSpeak.slnx`** (new XML format) — use that name with `dotnet`, not `.sln`.

```bash
dotnet build SelectSpeak.slnx -c Debug          # build all
dotnet test  SelectSpeak.slnx                    # 39 unit tests (pure-logic layer)
dotnet run   --project SelectSpeak/SelectSpeak.csproj -c Release   # launch tray app
dotnet publish SelectSpeak/SelectSpeak.csproj -c Release -p:PublishSingleFile=true --self-contained true -r win-x64
```

`setup.cmd` (double-click) = publish + create Desktop shortcut; `setup.cmd /startup` also adds
a Startup-folder shortcut. `setup.ps1` does the real work.

## Architecture (one job per component)

| File | Role |
|------|------|
| `SelectSpeak/Speech/SpeechService.cs` | WinRT `SpeechSynthesizer`: `AllVoices`, synth to stream, playback via headless `MediaPlayer`. A **generation counter** makes `Cancel()`/new calls supersede in-flight synthesis (stop-first, no stale playback). `SpeakEnded` fires on a bg thread. |
| `SelectSpeak/Input/SelectionCapture.cs` | **Waits for held modifiers to release** (clean Ctrl+C; injects key-ups only as fallback) → snapshot clipboard → SendInput Ctrl+C → poll `GetClipboardSequenceNumber` → read text → restore clipboard. UI/STA thread only. **Callers must serialize** (TrayApp `_capturing` guard) — overlapping captures race on the clipboard. |
| `SelectSpeak/Input/HotkeyManager.cs` | `RegisterHotKey` against a hidden `NativeWindow`; `Pressed` event (UI thread); `CanRegister` probes if a combo is free. |
| `SelectSpeak/Input/AutoSpeakWatcher.cs` | `WH_MOUSE_LL` hook **on its own dedicated thread** (with a GetMessage loop) so UI-thread work never lags system-wide mouse input. On `WM_LBUTTONUP` while `Enabled`, posts `SelectionFinished` to the UI ctx. |
| `SelectSpeak/Diagnostics/DebugLog.cs` | Opt-in file log (`SELECTSPEAK_DEBUG=1` → `%AppData%\SelectSpeak\debug.log`); no-op otherwise. |
| `SelectSpeak/Input/NativeMethods.cs` | P/Invoke. Uses **`DllImport`, not `LibraryImport`** (source-gen can't marshal the hook delegate). |
| `SelectSpeak/Config/Settings.cs` | JSON at `%AppData%\SelectSpeak\settings.json`. `Load`/`Save`, plus `FromJson`/`ToJson`/`LoadFrom`/`SaveTo`/`Normalize` for testing. Never throws on load. |
| `SelectSpeak/Config/HotkeyCombo.cs` | Parse/format "Ctrl+Alt+S" ↔ modifiers+`Keys`. `Win32Modifiers` adds MOD_NOREPEAT (0x4000). |
| `SelectSpeak/Theming/ThemePalette.cs` | Color roles as hex strings + `*Color` getters; static `Roles` list drives editor/validation generically. |
| `SelectSpeak/Theming/BuiltInThemes.cs` | `Skate Rink Dark` (default) + `Skate Rink Light`. |
| `SelectSpeak/Theming/ThemeService.cs` | Active-theme resolution, add/remove custom themes, `Apply(Control)` recursive WinForms theming. Ctor takes injectable `save` (tests pass no-op to avoid touching real AppData). |
| `SelectSpeak/Theming/NeonMenuRenderer.cs` | Themes the tray `ContextMenuStrip`. |
| `SelectSpeak/UI/TrayApp.cs` | `ApplicationContext` host. Wires everything; owns tray icon/menu, hotkey (de)registration, overlay show/hide. |
| `SelectSpeak/UI/SettingsForm.cs` | Voice/speed/volume/hotkeys/auto-speak/theme UI. Saves on change. |
| `SelectSpeak/UI/ThemeEditorForm.cs` | Edit a palette role-by-role, live preview, save as named custom theme. |
| `SelectSpeak/UI/ReadingOverlay.cs` | Non-activating topmost neon equalizer. Created once + GDI resources cached; timer runs only while reading. `SetPersistent(true)` (auto-speak) keeps it visible/idle between reads. `BeginReading`/`EndReading`/`ApplyPalette`. |
| `SelectSpeak/UI/IconFactory.cs` | Draws the tray icon from the palette (no shipped .ico). |

Data flow: hotkey/mouse-up → `SelectionCapture` → `SpeechService.SpeakAsync` → audio + overlay.
Stop hotkey or a new selection → `Cancel()`.

## Key decisions / gotchas

- **Voices via WinRT** (`Windows.Media.SpeechSynthesis`), not `System.Speech` — so `AllVoices`
  includes natural/neural voices, not just classic SAPI. Playback goes through `MediaPlayer` + `MediaSource.CreateFromStream`.
- **Threading:** `SpeakEnded`/MediaPlayer callbacks and the mouse hook run off the UI thread —
  marshal with `SynchronizationContext.Post` (TrayApp/AutoSpeakWatcher already do). The mouse
  hook MUST stay on its own thread (see AutoSpeakWatcher) or system-wide mouse lag returns.
- **Serialize captures:** `SpeakSelectionAsync(bool interrupt)` is guarded by `_capturing` (Interlocked).
  Removing it reintroduces clipboard races (reads wrong/stale text in auto-speak mode).
- **Interrupt semantics:** hotkey/menu call with `interrupt:true` (stop current immediately);
  auto-speak (mouse-up) calls `interrupt:false` so deselecting to read along, or a click with no
  selection, does NOT stop the current reading — only a genuinely new selection interrupts it.
- **`SendInput` INPUT struct:** the interop union MUST include `MOUSEINPUT` so `sizeof == 40` (x64);
  if it's smaller, SendInput silently no-ops (returns 0) and nothing ever copies. Guarded by
  `NativeInteropTests.InputStruct_MatchesWin32Size`.
- **Perf:** overlay is create-once + cached brushes/font; don't reintroduce per-frame GDI allocs
  or per-read Show/Hide of a fresh window.
- **Hotkey copy collision:** a hotkey like Ctrl+Alt+S leaves Ctrl/Alt physically down, so a
  raw synthetic Ctrl+C becomes Ctrl+Alt+C. `SelectionCapture` releases held modifiers
  (`GetAsyncKeyState` + key-up SendInput) before copying — don't remove this or hotkey reads break.
- **Hotkey capture** (SettingsForm) requires a modifier and does **not** bind the Win key
  (`KeyEventArgs` can't report it reliably). Parsing still accepts `Win+...` from config.
- **Auto-speak** sends a Ctrl+C probe on every left-button-up; no selection ⇒ clipboard
  unchanged ⇒ nothing read. Acceptable, but the place to look if clicks feel laggy.
- **Rate mapping:** app scale −10..10 → engine `SpeakingRate` 0.5..3.0 (0 = 1.0). See `SpeechService.MapRate`.
- **Defaults:** Speak `Ctrl+Alt+S`, Stop `Ctrl+Alt+X`, auto-speak off, interrupt-on-new-request,
  no "nothing selected" beep.
- Tests cover the pure-logic layer only (Settings, HotkeyCombo, palettes, ThemeService). Speech,
  clipboard, hooks, and UI are verified manually (checklist in README).

## Design spec

Original approved plan: `C:\Users\kasey.colian\.claude\plans\can-we-add-style-cuddly-rocket.md`.
