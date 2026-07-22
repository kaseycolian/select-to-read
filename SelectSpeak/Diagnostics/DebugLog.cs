namespace SelectSpeak.Diagnostics;

/// <summary>
/// Opt-in file logging for diagnosing capture/speech issues. Enabled by setting the
/// environment variable <c>SELECTSPEAK_DEBUG=1</c>; writes to
/// <c>%AppData%\SelectSpeak\debug.log</c>. No-op (and near-zero cost) otherwise.
/// </summary>
public static class DebugLog
{
    private static readonly bool Enabled =
        Environment.GetEnvironmentVariable("SELECTSPEAK_DEBUG") == "1";

    private static readonly object Gate = new();

    public static void Write(string message)
    {
        if (!Enabled) return;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SelectSpeak");
            Directory.CreateDirectory(dir);
            lock (Gate)
                File.AppendAllText(
                    Path.Combine(dir, "debug.log"),
                    $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
        }
        catch
        {
            // Never let logging break the app.
        }
    }
}
