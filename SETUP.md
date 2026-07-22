# SelectSpeak — Setup Guide

How to build SelectSpeak, put a launcher icon on your Desktop, and start using it.
This is a one-time setup; after that you just double-click the Desktop icon.

---

## Before you start

You need the **.NET 10 SDK** installed (to build the app once).

- Check: open a terminal and run `dotnet --version`. If it prints `10.x.x`, you're set.
- If not, install it from <https://dotnet.microsoft.com/download> and reopen your terminal.

> The finished `SelectSpeak.exe` is self-contained — once built, it runs on any Windows
> 11 PC even without .NET installed. You only need the SDK to build it.

---

## Step 1 — Build it and create the Desktop shortcut

**Double-click `setup.cmd`** in this folder.

That's it. The script will:

1. Build a standalone `SelectSpeak.exe` (the first build takes about a minute).
2. Create a **SelectSpeak** shortcut on your Desktop that points to it.

When it finishes you'll see:

```
Created a 'SelectSpeak' shortcut on your Desktop.
Done! Double-click the Desktop icon to start SelectSpeak.
```

Leave the window open until it says *Done*, then close it.

> **If double-clicking `setup.cmd` flashes and closes**, open a terminal in this folder and run:
> ```powershell
> .\setup.cmd
> ```
> so you can read any error message (most often: the .NET SDK isn't installed).

---

## Step 2 — Launch SelectSpeak

**Double-click the SelectSpeak icon on your Desktop.**

A neon speaker icon appears in your system tray (bottom-right, near the clock — you may
need to click the `^` to show hidden icons). SelectSpeak is now running in the background.

There is no main window — it lives in the tray. **Right-click the tray icon** any time for
the menu (Speak, Stop, Auto-speak, Settings, Exit).

---

## Step 3 — Use it

- **Read a selection:** highlight text in any app, then press **Ctrl + Alt + S**.
- **Stop reading:** press **Ctrl + Alt + X** (or trigger a new read — it interrupts).
- **Hands-free mode:** right-click the tray icon → **Auto-speak on selection**. Now just
  highlighting text reads it automatically, no keypress needed. Toggle it off the same way.

Open **Settings…** from the tray menu to:

- pick a **voice** (all your installed Windows voices) and preview it with **Test voice**,
- adjust **speed** and **volume**,
- change the **Speak/Stop hotkeys** (click the box and press your combo — needs a modifier),
- switch **themes** or build your own with **New / Edit…**.

Your settings and custom themes are saved automatically to
`%AppData%\SelectSpeak\settings.json` and persist between launches.

---

## Optional — Start automatically when you sign in

If you want SelectSpeak running every time you log in, open a terminal in this folder and run:

```powershell
.\setup.cmd /startup
```

This rebuilds (if needed) and adds a shortcut to your Startup folder as well. To undo it
later, delete the `SelectSpeak` shortcut from the Startup folder
(press **Win + R**, type `shell:startup`, press Enter, and delete it there).

---

## Updating after code changes

Whenever the app's source changes, re-run **`setup.cmd`** to rebuild the `.exe`.

> If the rebuild fails with *"Access to the path … SelectSpeak.exe is denied"*, SelectSpeak
> is still running. Exit it first (right-click the tray icon → **Exit**), then re-run `setup.cmd`.

---

## Quitting

Right-click the tray icon → **Exit**. (Closing nothing else is needed — there's no window.)
