using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace SelectSpeak.Speech;

/// <summary>Lightweight view of an installed voice for the UI.</summary>
public sealed record VoiceInfo(string Id, string DisplayName, string Language);

/// <summary>
/// Wraps the WinRT <see cref="SpeechSynthesizer"/> — enumerates every installed voice
/// (classic and natural), synthesizes text to an audio stream, and plays it through a
/// headless <see cref="MediaPlayer"/>. Speaking a new phrase cancels the current one.
/// </summary>
public sealed class SpeechService : IDisposable
{
    private readonly SpeechSynthesizer _synth = new();
    private readonly MediaPlayer _player = new();
    private int _generation;

    /// <summary>Raised when playback of a phrase begins.</summary>
    public event EventHandler? SpeakStarted;

    /// <summary>Raised when a phrase finishes on its own (not when cancelled). May fire on a background thread.</summary>
    public event EventHandler? SpeakEnded;

    public SpeechService()
    {
        _player.MediaEnded += (_, _) => SpeakEnded?.Invoke(this, EventArgs.Empty);
    }

    public static bool HasVoices => SpeechSynthesizer.AllVoices.Count > 0;

    public IReadOnlyList<VoiceInfo> GetVoices() =>
        SpeechSynthesizer.AllVoices
            .Select(v => new VoiceInfo(v.Id, v.DisplayName, v.Language))
            .ToList();

    public async Task SpeakAsync(string text, string? voiceId, int rate, int volume)
    {
        // Stop-first: cancel current playback and mark a new generation so any synthesis
        // still in flight from a previous call won't play once it completes.
        Cancel();
        if (string.IsNullOrWhiteSpace(text)) return;

        int generation = Interlocked.Increment(ref _generation);

        var voice = FindVoice(voiceId);
        if (voice is not null) _synth.Voice = voice;

        _synth.Options.SpeakingRate = MapRate(rate);
        _synth.Options.AudioVolume = Math.Clamp(volume, 0, 100) / 100.0;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        SpeechSynthesisStream stream = await _synth.SynthesizeTextToStreamAsync(text);
        Diagnostics.DebugLog.Write($"speak: synthesized {text.Length} chars in {sw.ElapsedMilliseconds}ms voice='{_synth.Voice?.DisplayName}'");

        // A newer request (or a Cancel) superseded us while synthesizing — discard this audio.
        if (generation != Volatile.Read(ref _generation))
        {
            Diagnostics.DebugLog.Write("speak: superseded before playback");
            return;
        }

        _player.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
        SpeakStarted?.Invoke(this, EventArgs.Empty);
        _player.Play();
    }

    /// <summary>
    /// Initialize the synthesis + audio pipeline once at startup by playing a brief muted
    /// utterance, so the first real read doesn't pay the audio-device warm-up latency.
    /// </summary>
    public async Task WarmUpAsync()
    {
        try
        {
            var stream = await _synth.SynthesizeTextToStreamAsync(" ");
            double volume = _player.Volume;
            _player.Volume = 0;
            _player.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            _player.Play();
            await Task.Delay(250);
            _player.Pause();
            _player.Source = null;
            _player.Volume = volume;
        }
        catch
        {
            // Warm-up is best-effort.
        }
    }

    /// <summary>Stop any current playback and supersede any in-flight synthesis.</summary>
    public void Cancel()
    {
        Interlocked.Increment(ref _generation);
        try
        {
            _player.Pause();
            _player.Source = null;
        }
        catch { /* nothing playing */ }
    }

    private static VoiceInformation? FindVoice(string? id) =>
        string.IsNullOrEmpty(id) ? null : SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == id);

    /// <summary>Map our -10..10 scale to the engine's 0.5..3.0 speaking rate (0 = normal 1.0).</summary>
    private static double MapRate(int rate)
    {
        rate = Math.Clamp(rate, -10, 10);
        return rate >= 0 ? 1.0 + rate * 0.2 : 1.0 + rate * 0.05;
    }

    public void Dispose()
    {
        _player.Dispose();
        _synth.Dispose();
    }
}
