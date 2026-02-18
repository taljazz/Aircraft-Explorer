using AircraftExplorer.Audio;

namespace AircraftExplorer.FlightSim;

public class FlightNarrator
{
    private readonly ISpeechService _speech;
    private readonly int _debounceMilliseconds;
    private readonly Dictionary<string, DateTime> _lastAnnounceTimes = new();

    public FlightNarrator(ISpeechService speech, int debounceMilliseconds = 500)
    {
        _speech = speech;
        _debounceMilliseconds = debounceMilliseconds;
    }

    public void AnnounceEffects(IReadOnlyList<ControlEffect> effects)
    {
        var now = DateTime.UtcNow;

        foreach (var effect in effects)
        {
            string key = effect.InputDescription.Split(' ')[0]; // axis name (Pitch, Roll, Yaw, Throttle)

            if (_lastAnnounceTimes.TryGetValue(key, out var lastTime))
            {
                var elapsed = (now - lastTime).TotalMilliseconds;
                if (elapsed < _debounceMilliseconds)
                    continue;
            }

            _speech.Speak(effect.ToNarration(), interrupt: false);
            _lastAnnounceTimes[key] = now;
        }
    }

    public void Reset()
    {
        _lastAnnounceTimes.Clear();
    }
}
