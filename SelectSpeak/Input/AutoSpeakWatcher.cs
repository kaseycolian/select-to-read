namespace SelectSpeak.Input;

/// <summary>
/// A low-level global mouse hook that raises <see cref="SelectionFinished"/> on the UI
/// thread when the left button is released while <see cref="Enabled"/> is true.
///
/// The hook runs on its OWN dedicated thread with its own message loop. This is critical
/// for performance: Windows calls a WH_MOUSE_LL hook synchronously on the installing
/// thread for every mouse event system-wide, so installing it on the UI thread makes the
/// whole desktop's mouse lag whenever the UI thread is busy (painting, clipboard, speech).
/// </summary>
public sealed class AutoSpeakWatcher : IDisposable
{
    private readonly NativeMethods.LowLevelMouseProc _proc; // held so it isn't GC'd while hooked
    private readonly SynchronizationContext _ui;
    private readonly Thread _thread;
    private IntPtr _hook;
    private uint _threadId;
    private volatile bool _running;
    private volatile bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public event Action? SelectionFinished;

    public AutoSpeakWatcher()
    {
        _ui = SynchronizationContext.Current ?? new SynchronizationContext();
        _proc = HookCallback;
        _running = true;
        _thread = new Thread(ThreadProc)
        {
            IsBackground = true,
            Name = "SelectSpeak-MouseHook",
        };
        _thread.Start();
    }

    private void ThreadProc()
    {
        _threadId = NativeMethods.GetCurrentThreadId();
        _hook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL, _proc, NativeMethods.GetModuleHandle(null), 0);

        // Pump messages so the hook is serviced; GetMessage blocks cheaply when idle and
        // returns 0 on WM_QUIT (posted by Dispose).
        while (_running && NativeMethods.GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
        {
            // Hook-only thread: nothing to translate/dispatch.
        }

        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _enabled && (int)wParam == NativeMethods.WM_LBUTTONUP)
            _ui.Post(_ => SelectionFinished?.Invoke(), null);

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        _running = false;
        if (_threadId != 0)
            NativeMethods.PostThreadMessage(_threadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(1000);
    }
}
