using SelectSpeak.Config;

namespace SelectSpeak.Input;

/// <summary>
/// Registers global hotkeys against a hidden message window and raises <see cref="Pressed"/>
/// (on the UI thread) when one fires. Registration reports failure so the UI can tell the
/// user a combo is already in use.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly MessageWindow _window = new();
    private readonly HashSet<int> _registered = new();

    public event Action<int>? Pressed;

    public HotkeyManager() => _window.HotkeyPressed += id => Pressed?.Invoke(id);

    /// <summary>Register (replacing any prior binding for this id). Returns false if the OS rejects the combo.</summary>
    public bool TryRegister(int id, HotkeyCombo combo)
    {
        Unregister(id);
        if (NativeMethods.RegisterHotKey(_window.Handle, id, combo.Win32Modifiers, combo.VirtualKey))
        {
            _registered.Add(id);
            return true;
        }
        return false;
    }

    public void Unregister(int id)
    {
        if (_registered.Remove(id))
            NativeMethods.UnregisterHotKey(_window.Handle, id);
    }

    /// <summary>
    /// Test whether a combo is free by briefly registering it on a probe id. Note: a combo
    /// this app has already registered will report false, so callers should skip the check
    /// when the combo is unchanged.
    /// </summary>
    public bool CanRegister(HotkeyCombo combo)
    {
        const int probeId = 0x7FFE;
        if (!NativeMethods.RegisterHotKey(_window.Handle, probeId, combo.Win32Modifiers, combo.VirtualKey))
            return false;
        NativeMethods.UnregisterHotKey(_window.Handle, probeId);
        return true;
    }

    public void Dispose()
    {
        foreach (var id in _registered)
            NativeMethods.UnregisterHotKey(_window.Handle, id);
        _registered.Clear();
        _window.DestroyHandle();
    }

    /// <summary>A hidden native window that receives WM_HOTKEY on the thread that created it.</summary>
    private sealed class MessageWindow : NativeWindow
    {
        public event Action<int>? HotkeyPressed;

        public MessageWindow() => CreateHandle(new CreateParams());

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
                HotkeyPressed?.Invoke((int)m.WParam);
            base.WndProc(ref m);
        }
    }
}
