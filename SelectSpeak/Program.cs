using SelectSpeak.UI;

namespace SelectSpeak;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Single instance: a second launch exits quietly.
        using var mutex = new Mutex(initiallyOwned: true, @"Local\SelectSpeak_SingleInstance", out bool isNew);
        if (!isNew) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
