using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class FlightControlMode : IAppMode
{
    private ModeContext _context = null!;

    // Axis zones: centered, moderate, full deflection
    private const float DeadZone = 0.15f;
    private const float ModerateZone = 0.6f;

    // Track previous zone per axis to announce only on changes
    private int _prevPitchZone;
    private int _prevRollZone;
    private int _prevYawZone;
    private int _prevThrottleZone;
    private bool _hardwareDetected;

    public string ModeName => "Flight Controls";

    public void OnEnter(ModeContext context)
    {
        _context = context;

        var state = context.InputManager.GetAxisState();
        _hardwareDetected = state is not null;

        if (_hardwareDetected)
        {
            // Initialize zones from current position so we don't announce on entry
            _prevPitchZone = GetAxisZone(state!.Pitch);
            _prevRollZone = GetAxisZone(state.Roll);
            _prevYawZone = GetAxisZone(state.Yaw);
            _prevThrottleZone = GetThrottleZone(state.Throttle);

            context.Speech.Speak(
                "Flight controls active. Move your yoke, pedals, and throttle to explore control surfaces. " +
                "Press Escape to return.",
                true);
        }
        else
        {
            context.Speech.Speak(
                "Flight controls mode. No flight hardware detected. " +
                "Connect a yoke or joystick and restart. Press Escape to return.",
                true);
        }
    }

    public ModeResult HandleInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    _hardwareDetected
                        ? "Flight control mode. Move your yoke forward and back for pitch. " +
                          "Turn left and right for roll. Rudder pedals for yaw. Throttle lever for thrust. " +
                          "Escape to return."
                        : "No flight hardware detected. Press Escape to return.",
                    true);
                return ModeResult.Stay;

            case InputAction.AnnouncePosition:
                AnnounceCurrentAxes();
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    public void OnTick()
    {
        if (!_hardwareDetected)
            return;

        var state = _context.InputManager.GetAxisState();
        if (state is null)
            return;

        int pitchZone = GetAxisZone(state.Pitch);
        int rollZone = GetAxisZone(state.Roll);
        int yawZone = GetAxisZone(state.Yaw);
        int throttleZone = GetThrottleZone(state.Throttle);

        // Announce only when crossing zone boundaries
        if (pitchZone != _prevPitchZone)
        {
            _prevPitchZone = pitchZone;
            AnnouncePitch(pitchZone);
        }
        else if (rollZone != _prevRollZone)
        {
            _prevRollZone = rollZone;
            AnnounceRoll(rollZone);
        }
        else if (yawZone != _prevYawZone)
        {
            _prevYawZone = yawZone;
            AnnounceYaw(yawZone);
        }
        else if (throttleZone != _prevThrottleZone)
        {
            _prevThrottleZone = throttleZone;
            AnnounceThrottle(throttleZone);
        }
    }

    private void AnnouncePitch(int zone)
    {
        string message = zone switch
        {
            -2 => "Yoke full forward. Elevator down, nose pitching down.",
            -1 => "Yoke forward. Elevator deflecting down.",
            0 => "Yoke centered. Elevator neutral.",
            1 => "Yoke back. Elevator deflecting up.",
            2 => "Yoke full back. Elevator up, nose pitching up.",
            _ => ""
        };
        if (message.Length > 0)
            _context.Speech.Speak(message, true);
    }

    private void AnnounceRoll(int zone)
    {
        string message = zone switch
        {
            -2 => "Yoke full left. Left aileron up, right aileron down. Banking left.",
            -1 => "Yoke left. Ailerons deflecting for left roll.",
            0 => "Yoke wings level. Ailerons neutral.",
            1 => "Yoke right. Ailerons deflecting for right roll.",
            2 => "Yoke full right. Right aileron up, left aileron down. Banking right.",
            _ => ""
        };
        if (message.Length > 0)
            _context.Speech.Speak(message, true);
    }

    private void AnnounceYaw(int zone)
    {
        string message = zone switch
        {
            -2 => "Full left rudder. Rudder deflected left, nose yawing left.",
            -1 => "Left rudder. Rudder deflecting left.",
            0 => "Rudder centered. Rudder neutral.",
            1 => "Right rudder. Rudder deflecting right.",
            2 => "Full right rudder. Rudder deflected right, nose yawing right.",
            _ => ""
        };
        if (message.Length > 0)
            _context.Speech.Speak(message, true);
    }

    private void AnnounceThrottle(int zone)
    {
        string message = zone switch
        {
            0 => "Throttle idle. Engines at minimum thrust.",
            1 => "Throttle low. Engines at low thrust.",
            2 => "Throttle mid range. Engines at moderate thrust.",
            3 => "Throttle high. Engines at high thrust.",
            4 => "Throttle full forward. Maximum thrust.",
            _ => ""
        };
        if (message.Length > 0)
            _context.Speech.Speak(message, true);
    }

    private void AnnounceCurrentAxes()
    {
        var state = _context.InputManager.GetAxisState();
        if (state is null)
        {
            _context.Speech.Speak("No flight hardware detected.", true);
            return;
        }

        string pitch = state.Pitch switch
        {
            < -0.5f => "full forward",
            < -0.15f => "forward",
            > 0.5f => "full back",
            > 0.15f => "back",
            _ => "centered"
        };
        string roll = state.Roll switch
        {
            < -0.5f => "full left",
            < -0.15f => "left",
            > 0.5f => "full right",
            > 0.15f => "right",
            _ => "wings level"
        };

        _context.Speech.Speak($"Yoke {pitch}, {roll}. " +
            $"Throttle at {(int)(state.Throttle * 100)} percent.", true);
    }

    public void OnExit()
    {
        _context.Speech.Speak("Leaving flight controls.", true);
    }

    public void OnResume() { }

    /// <summary>Returns -2, -1, 0, 1, or 2 based on axis deflection.</summary>
    private static int GetAxisZone(float value)
    {
        if (value < -ModerateZone) return -2;
        if (value < -DeadZone) return -1;
        if (value > ModerateZone) return 2;
        if (value > DeadZone) return 1;
        return 0;
    }

    /// <summary>Returns 0-4 based on throttle position.</summary>
    private static int GetThrottleZone(float value)
    {
        if (value < 0.1f) return 0;
        if (value < 0.35f) return 1;
        if (value < 0.65f) return 2;
        if (value < 0.9f) return 3;
        return 4;
    }
}
