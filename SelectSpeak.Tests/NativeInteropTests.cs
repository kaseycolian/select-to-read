using System.Runtime.InteropServices;
using SelectSpeak.Input;

namespace SelectSpeak.Tests;

public class NativeInteropTests
{
    [Fact]
    public void InputStruct_MatchesWin32Size()
    {
        // SendInput validates cbSize against the real Win32 INPUT size (40 bytes on x64,
        // 28 on x86, sized by the largest union member MOUSEINPUT). If our managed struct is
        // smaller, SendInput silently injects nothing and returns 0 — which broke all copying.
        int expected = IntPtr.Size == 8 ? 40 : 28;
        Assert.Equal(expected, Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
