# SelectSpeak

A lightweight, system-wide "select-and-speak" reader for Windows 11. Highlight text in
**any** application and have it read aloud with your installed Microsoft voices — no
DOM/element navigation, no browser extension. Lives in the system tray with a 90s
skating-rink neon theme (and a matching light theme, plus custom themes you can create).

> **New here? See [SETUP.md](SETUP.md)** for a full step-by-step setup and usage guide.

## Quick start (double-click, recommended)

1. **Double-click `setup.cmd`** in this folder.
   It builds a standalone `SelectSpeak.exe` and puts a **SelectSpeak** shortcut on your Desktop.
   (First build takes a minute. Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.)
2. **Double-click the SelectSpeak icon on your Desktop** to launch it. A neon speaker
   icon appears in your system tray.
3. Select text in any app and press **Ctrl+Alt+S** to hear it. Right-click the tray icon for Settings.

Want it to launch automatically every time you sign in? Run it once with the startup flag:

```powershell
# double-click won't pass the flag, so run this line once in a terminal here:
.\setup.cmd /startup
```

That's the whole install. The rest of this file is reference detail.

## Requirements

- Windows 10 1809+ / Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (LTS) to build. The published
  single-file build is self-contained and needs no .NET installed to run.

## Build & run

```powershell
# from the repo root
dotnet run --project SelectSpeak/SelectSpeak.csproj -c Release
```

A neon speaker icon appears in the system tray. Right-click it for the menu.

## Publish a standalone .exe

```powershell
dotnet publish SelectSpeak/SelectSpeak.csproj -c Release `
  -p:PublishSingleFile=true --self-contained true -r win-x64
```

The exe lands in
`SelectSpeak/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/SelectSpeak.exe`.
Double-click to run; pin it to Startup (Win+R → `shell:startup`) to launch at login.

## How to use

- **Hotkey mode (default):** Select text anywhere, then press **Ctrl+Alt+S** to read it.
  Press **Ctrl+Alt+X** (or the hotkey again) to stop.
- **Auto-speak mode:** Toggle *Auto-speak on selection* in the tray menu (or Settings).
  Then just releasing a mouse selection reads it — no keypress.
- Both hotkeys are customizable in **Settings** and persist until you change them.

Under the hood it copies your selection with a simulated Ctrl+C, reads the clipboard,
then restores your previous clipboard contents.

## Settings

Right-click the tray icon → **Settings…**:

- **Voice** — every installed voice (classic and natural) in a dropdown; **Test voice** previews it.
- **Speed / Volume** — sliders.
- **Speak / Stop hotkeys** — click the field and press the combo (needs a modifier).
  If a combo is already taken by another app you'll be told to pick another.
- **Auto-speak** — the on/off toggle.
- **Theme** — pick a theme, or **New / Edit…** to build and save your own neon palette.

Settings and custom themes are stored in `%AppData%\SelectSpeak\settings.json`.

## Manual verification checklist

Some behavior needs a live desktop session to confirm:

1. Select text in Notepad, a browser, and a PDF viewer → Ctrl+Alt+S reads it in the
   chosen voice; your clipboard is unchanged afterward.
2. Toggle auto-speak on → selecting text reads it with no keypress; off → it doesn't.
3. Ctrl+Alt+X stops mid-sentence; a new selection interrupts the current one.
4. Rebind the Speak hotkey in Settings → new combo works immediately and after restart.
5. Switch Dark ↔ Light → Settings window, tray menu, and reading overlay all restyle.
6. Create + save a custom theme → it persists in `settings.json` and reloads after restart.

## Project layout

- `SelectSpeak/Speech` — WinRT speech (voice enumeration, synthesis, playback).
- `SelectSpeak/Input` — clipboard selection capture, global hotkeys, auto-speak mouse hook.
- `SelectSpeak/Config` — settings persistence and hotkey parsing.
- `SelectSpeak/Theming` — data-driven palettes, built-in themes, WinForms theming.
- `SelectSpeak/UI` — tray host, settings window, theme editor, reading overlay.
- `SelectSpeak.Tests` — unit tests for the pure-logic layer.
