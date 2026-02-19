using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class ThrottleCalibrationMode : IAppMode
{
    private const int MovementThreshold = 8000;
    private const int SettleFrames = 30; // ~0.5s at 60Hz
    private const int RawMidpoint = 32768;

    private ModeContext _context = null!;
    private bool _noHardware;
    private int _tickCount;
    private Dictionary<string, int>? _minValues;
    private Dictionary<string, int>? _maxValues;

    // Phase 1: detect which axis. Phase 2: detect inversion.
    private bool _axisDetected;
    private string? _detectedAxis;
    private bool _done;

    public string ModeName => "Throttle Calibration";

    public void OnEnter(ModeContext context)
    {
        _context = context;

        if (context.InputManager.GetRawAxes() is null)
        {
            _noHardware = true;
            context.Speech.Speak("No flight hardware detected. Press Escape to return.", true);
        }
        else
        {
            context.Speech.Speak("Move your throttle lever now. Press Escape to cancel.", true);
        }
    }

    public void OnTick()
    {
        if (_noHardware || _axisDetected)
            return;

        var current = _context.InputManager.GetRawAxes();
        if (current is null)
            return;

        _tickCount++;

        // During settling period, ignore input so transient movement
        // from pressing Enter doesn't contaminate the readings.
        if (_tickCount <= SettleFrames)
            return;

        // Initialize min/max tracking after settling
        if (_minValues is null || _maxValues is null)
        {
            _minValues = new Dictionary<string, int>(current);
            _maxValues = new Dictionary<string, int>(current);
            return;
        }

        // Update min and max for each axis
        foreach (var (axis, value) in current)
        {
            if (_minValues.TryGetValue(axis, out var min))
                _minValues[axis] = Math.Min(min, value);
            if (_maxValues.TryGetValue(axis, out var max))
                _maxValues[axis] = Math.Max(max, value);
        }

        // The axis with the largest peak-to-peak range is the throttle.
        string? bestAxis = null;
        int bestRange = 0;

        foreach (var (axis, min) in _minValues)
        {
            if (!_maxValues.TryGetValue(axis, out var max))
                continue;

            var range = max - min;
            if (range > bestRange)
            {
                bestRange = range;
                bestAxis = axis;
            }
        }

        if (bestRange > MovementThreshold && bestAxis is not null)
        {
            _detectedAxis = bestAxis;
            _axisDetected = true;
            _context.Settings.ThrottleAxis = bestAxis;
            _context.Speech.Speak(
                $"Throttle axis detected: {bestAxis}. " +
                "Now push throttle to full and press Enter.", true);
        }
    }

    public ModeResult HandleInput(InputAction action)
    {
        if (action == InputAction.Back)
            return ModeResult.Pop;

        if (_done)
            return ModeResult.Pop;

        // Phase 2: user presses Enter with throttle at full
        if (_axisDetected && action == InputAction.Select)
        {
            var axes = _context.InputManager.GetRawAxes();
            if (axes is not null && _detectedAxis is not null
                && axes.TryGetValue(_detectedAxis, out var rawValue))
            {
                // If the raw value at full throttle is below midpoint, the axis is inverted.
                _context.Settings.InvertThrottle = rawValue < RawMidpoint;
            }

            var inverted = _context.Settings.InvertThrottle ? " Axis inverted." : "";
            _context.Speech.Speak($"Throttle calibrated.{inverted}", true);
            _done = true;
            return ModeResult.Stay; // pop on next input cycle
        }

        return ModeResult.Stay;
    }

    public void OnExit() { }

    public void OnResume() { }
}
