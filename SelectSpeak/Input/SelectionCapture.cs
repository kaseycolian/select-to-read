using System.Runtime.InteropServices;
using SelectSpeak.Diagnostics;

namespace SelectSpeak.Input;

/// <summary>
/// Grabs the current selection from whatever app has focus by snapshotting the clipboard,
/// sending Ctrl+C, reading the new clipboard text, then restoring the original clipboard.
/// Must be called on the UI (STA) thread, and callers must not run two captures at once
/// (overlapping captures race on the clipboard). Returns null when nothing usable was selected.
/// </summary>
public sealed class SelectionCapture
{
    private const int ReleaseWaitMaxMs = 400;
    private const int PollAttempts = 30;    // ~450 ms
    private const int PollIntervalMs = 15;

    public async Task<string?> CaptureSelectedTextAsync()
    {
        // A global hotkey (e.g. Ctrl+Alt+S) leaves Ctrl/Alt physically down; injecting Ctrl+C
        // then yields Ctrl+Alt+C, not a copy. Wait for the user to let go of the modifiers so
        // the copy is clean. In auto-speak mode no modifiers are held, so this returns instantly.
        bool released = await WaitForModifiersReleasedAsync();
        if (!released)
            ForceReleaseModifiers(); // user is holding the keys down — inject key-ups as a fallback

        IDataObject? saved = SnapshotClipboard();
        uint before = NativeMethods.GetClipboardSequenceNumber();

        uint sent = SendCtrlC();

        string? text = null;
        int changedAt = -1;
        for (int i = 0; i < PollAttempts; i++)
        {
            await Task.Delay(PollIntervalMs);
            if (NativeMethods.GetClipboardSequenceNumber() != before)
            {
                changedAt = i;
                text = TryGetText();
                if (text is not null) break;
            }
        }

        RestoreClipboard(saved);

        var result = string.IsNullOrWhiteSpace(text) ? null : text;
        DebugLog.Write($"capture: inputStructSize={Marshal.SizeOf<NativeMethods.INPUT>()} " +
                       $"modifiersReleased={released} sendInputEvents={sent} seqBefore={before} " +
                       $"seqAfter={NativeMethods.GetClipboardSequenceNumber()} changedAtPoll={changedAt} " +
                       $"hadText={text is not null} len={result?.Length ?? 0} text='{Preview(result)}'");
        return result;
    }

    private static async Task<bool> WaitForModifiersReleasedAsync()
    {
        int waited = 0;
        while (AnyModifierDown())
        {
            if (waited >= ReleaseWaitMaxMs) return false;
            await Task.Delay(PollIntervalMs);
            waited += PollIntervalMs;
        }
        return true;
    }

    private static bool AnyModifierDown()
    {
        foreach (var vk in NativeMethods.ModifierKeys)
            if ((NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0)
                return true;
        return false;
    }

    private static void ForceReleaseModifiers()
    {
        var ups = new List<NativeMethods.INPUT>();
        foreach (var vk in NativeMethods.ModifierKeys)
            if ((NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0)
                ups.Add(KeyInput(vk, keyUp: true));

        if (ups.Count > 0)
            NativeMethods.SendInput((uint)ups.Count, ups.ToArray(), Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static string? TryGetText()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch { return null; }
    }

    private static IDataObject? SnapshotClipboard()
    {
        try
        {
            IDataObject? current = Clipboard.GetDataObject();
            if (current is null) return null;

            var copy = new DataObject();
            bool any = false;
            foreach (var format in current.GetFormats())
            {
                try
                {
                    var data = current.GetData(format);
                    if (data is not null) { copy.SetData(format, data); any = true; }
                }
                catch { /* skip formats we can't clone */ }
            }
            return any ? copy : null;
        }
        catch { return null; }
    }

    private static void RestoreClipboard(IDataObject? saved)
    {
        try
        {
            if (saved is not null)
                Clipboard.SetDataObject(saved, copy: true);
            else
                Clipboard.Clear();
        }
        catch { /* clipboard may be locked; best effort */ }
    }

    /// <summary>Injects Ctrl+C. Returns the number of input events accepted (0 means injection was blocked).</summary>
    private static uint SendCtrlC()
    {
        var inputs = new[]
        {
            KeyInput(NativeMethods.VK_CONTROL, keyUp: false),
            KeyInput(NativeMethods.VK_C, keyUp: false),
            KeyInput(NativeMethods.VK_C, keyUp: true),
            KeyInput(NativeMethods.VK_CONTROL, keyUp: true),
        };
        return NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT KeyInput(ushort vk, bool keyUp) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
            },
        },
    };

    private static string Preview(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var oneLine = text.Replace('\r', ' ').Replace('\n', ' ');
        return oneLine.Length <= 40 ? oneLine : oneLine[..40] + "…";
    }
}
