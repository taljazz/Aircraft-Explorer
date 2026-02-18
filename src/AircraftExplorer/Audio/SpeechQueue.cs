namespace AircraftExplorer.Audio;

public sealed class SpeechQueue
{
    private readonly ISpeechService _speech;
    private readonly TimeSpan _debounceInterval;
    private readonly Dictionary<string, DateTime> _lastSpeakTimes = new();
    private DateTime _lastGlobalSpeakTime = DateTime.MinValue;

    public SpeechQueue(ISpeechService speech, int debounceMilliseconds = 500)
    {
        _speech = speech;
        _debounceInterval = TimeSpan.FromMilliseconds(debounceMilliseconds);
    }

    public void SpeakImmediate(string text)
    {
        _speech.Speak(text, interrupt: true);
        _lastGlobalSpeakTime = DateTime.UtcNow;
    }

    public void SpeakIfReady(string text, string? category = null)
    {
        var now = DateTime.UtcNow;
        var key = category ?? string.Empty;

        if (_lastSpeakTimes.TryGetValue(key, out var lastTime)
            && now - lastTime < _debounceInterval)
        {
            return;
        }

        _lastSpeakTimes[key] = now;
        _lastGlobalSpeakTime = now;
        _speech.Speak(text, interrupt: false);
    }

    public void Silence()
    {
        _speech.Silence();
    }
}
