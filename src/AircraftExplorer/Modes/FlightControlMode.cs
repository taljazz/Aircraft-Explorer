using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class FlightControlMode : IAppMode
{
    private ModeContext _context = null!;

    // Axis zones: centered, moderate, full deflection
    private const float DeadZone = 0.15f;
    private const float ModerateZone = 0.6f;

    // Keyboard virtual axis step sizes
    private const float AxisStep = 0.4f;
    private const float ThrottleStep = 0.25f;

    // Track previous zone per axis to announce only on changes
    private int _prevPitchZone;
    private int _prevRollZone;
    private int _prevYawZone;
    private int _prevThrottleZone;
    private bool _hardwareDetected;

    // Hardware axes only announce after they've moved from their initial zone.
    // This prevents idle axes (e.g. no rudder pedals) from overriding keyboard input.
    private int _hwInitPitchZone;
    private int _hwInitRollZone;
    private int _hwInitYawZone;
    private int _hwInitThrottleZone;
    private bool _hwPitchActive;
    private bool _hwRollActive;
    private bool _hwYawActive;
    private bool _hwThrottleActive;

    // Last hardware zone seen in OnTick — used to reset init zone
    // when keyboard takes over an axis.
    private int _lastHwPitchZone;
    private int _lastHwRollZone;
    private int _lastHwYawZone;
    private int _lastHwThrottleZone;

    // Keyboard virtual axis state
    private float _keyPitch;
    private float _keyRoll;
    private float _keyYaw;
    private float _keyThrottle;

    public string ModeName => "Flight Controls";

    public void OnEnter(ModeContext context)
    {
        _context = context;

        var state = context.InputManager.GetAxisState();
        _hardwareDetected = state is not null;

        // Reset keyboard virtual axes
        _keyPitch = 0f;
        _keyRoll = 0f;
        _keyYaw = 0f;
        _keyThrottle = 0f;

        if (_hardwareDetected)
        {
            // Initialize zones from current position so we don't announce on entry
            _prevPitchZone = GetAxisZone(state!.Pitch);
            _prevRollZone = GetAxisZone(state.Roll);
            _prevYawZone = GetAxisZone(state.Yaw);
            _prevThrottleZone = GetThrottleZone(state.Throttle);

            // Record initial zones — hardware axes only start announcing
            // once they've moved away from these values.
            _hwInitPitchZone = _prevPitchZone;
            _hwInitRollZone = _prevRollZone;
            _hwInitYawZone = _prevYawZone;
            _hwInitThrottleZone = _prevThrottleZone;
            _lastHwPitchZone = _prevPitchZone;
            _lastHwRollZone = _prevRollZone;
            _lastHwYawZone = _prevYawZone;
            _lastHwThrottleZone = _prevThrottleZone;
        }
        else
        {
            _prevPitchZone = 0;
            _prevRollZone = 0;
            _prevYawZone = 0;
            _prevThrottleZone = 0;
        }

        context.Speech.Speak(
            "Flight controls active. " +
            (_hardwareDetected
                ? "Move your yoke, pedals, and throttle to explore control surfaces. "
                : "No flight hardware detected. ") +
            "Use numpad 8 and 2 for pitch, 4 and 6 for roll, Insert and Delete for rudder, " +
            "Home and End for throttle. Press Escape to return.",
            true);
    }

    public ModeResult HandleInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    "Flight control mode. " +
                    (_hardwareDetected
                        ? "Move your yoke forward and back for pitch. " +
                          "Turn left and right for roll. Rudder pedals for yaw. Throttle lever for thrust. "
                        : "") +
                    "Numpad 8 and 2 for pitch. Numpad 4 and 6 for roll. " +
                    "Insert and Delete for rudder. Home and End for throttle. " +
                    "C to announce current position. Escape to return.",
                    true);
                return ModeResult.Stay;

            case InputAction.AnnouncePosition:
                AnnounceCurrentAxes();
                return ModeResult.Stay;

            case InputAction.PitchForward:
            case InputAction.MoveForward:
                DeactivateHardwareAxis(ref _hwPitchActive, ref _hwInitPitchZone, _lastHwPitchZone);
                AdjustKeyAxis(ref _keyPitch, -AxisStep, ref _prevPitchZone, AnnouncePitch);
                return ModeResult.Stay;

            case InputAction.PitchBack:
            case InputAction.MoveBackward:
                DeactivateHardwareAxis(ref _hwPitchActive, ref _hwInitPitchZone, _lastHwPitchZone);
                AdjustKeyAxis(ref _keyPitch, AxisStep, ref _prevPitchZone, AnnouncePitch);
                return ModeResult.Stay;

            case InputAction.RollLeft:
            case InputAction.MoveLeft:
                DeactivateHardwareAxis(ref _hwRollActive, ref _hwInitRollZone, _lastHwRollZone);
                AdjustKeyAxis(ref _keyRoll, -AxisStep, ref _prevRollZone, AnnounceRoll);
                return ModeResult.Stay;

            case InputAction.RollRight:
            case InputAction.MoveRight:
                DeactivateHardwareAxis(ref _hwRollActive, ref _hwInitRollZone, _lastHwRollZone);
                AdjustKeyAxis(ref _keyRoll, AxisStep, ref _prevRollZone, AnnounceRoll);
                return ModeResult.Stay;

            case InputAction.RudderLeft:
                DeactivateHardwareAxis(ref _hwYawActive, ref _hwInitYawZone, _lastHwYawZone);
                AdjustKeyAxis(ref _keyYaw, -AxisStep, ref _prevYawZone, AnnounceYaw);
                return ModeResult.Stay;

            case InputAction.RudderRight:
                DeactivateHardwareAxis(ref _hwYawActive, ref _hwInitYawZone, _lastHwYawZone);
                AdjustKeyAxis(ref _keyYaw, AxisStep, ref _prevYawZone, AnnounceYaw);
                return ModeResult.Stay;

            case InputAction.ThrottleUp:
                DeactivateHardwareAxis(ref _hwThrottleActive, ref _hwInitThrottleZone, _lastHwThrottleZone);
                AdjustKeyThrottle(ThrottleStep);
                return ModeResult.Stay;

            case InputAction.ThrottleDown:
                DeactivateHardwareAxis(ref _hwThrottleActive, ref _hwInitThrottleZone, _lastHwThrottleZone);
                AdjustKeyThrottle(-ThrottleStep);
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    /// <summary>
    /// Deactivate a hardware axis so it stops overriding keyboard input.
    /// Resets the init zone to the current hardware position so the axis
    /// must physically move again before it re-activates.
    /// </summary>
    private static void DeactivateHardwareAxis(ref bool active, ref int initZone, int currentHwZone)
    {
        active = false;
        initZone = currentHwZone;
    }

    private void AdjustKeyAxis(ref float axis, float delta, ref int prevZone, Action<int> announce)
    {
        axis = Math.Clamp(axis + delta, -0.8f, 0.8f);
        int zone = GetAxisZone(axis);
        if (zone != prevZone)
        {
            prevZone = zone;
            announce(zone);
        }
    }

    private void AdjustKeyThrottle(float delta)
    {
        _keyThrottle = Math.Clamp(_keyThrottle + delta, 0f, 1f);
        int zone = GetThrottleZone(_keyThrottle);
        if (zone != _prevThrottleZone)
        {
            _prevThrottleZone = zone;
            AnnounceThrottle(zone);
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

        _lastHwPitchZone = pitchZone;
        _lastHwRollZone = rollZone;
        _lastHwYawZone = yawZone;
        _lastHwThrottleZone = throttleZone;

        // Activate hardware axes once they move from their initial zone.
        // Idle axes (e.g. no rudder pedals) stay inactive and don't
        // interfere with keyboard controls for those axes.
        if (!_hwPitchActive && pitchZone != _hwInitPitchZone) _hwPitchActive = true;
        if (!_hwRollActive && rollZone != _hwInitRollZone) _hwRollActive = true;
        if (!_hwYawActive && yawZone != _hwInitYawZone) _hwYawActive = true;
        if (!_hwThrottleActive && throttleZone != _hwInitThrottleZone) _hwThrottleActive = true;

        // Announce only when crossing zone boundaries on active axes
        if (_hwPitchActive && pitchZone != _prevPitchZone)
        {
            _prevPitchZone = pitchZone;
            AnnouncePitch(pitchZone);
        }
        else if (_hwRollActive && rollZone != _prevRollZone)
        {
            _prevRollZone = rollZone;
            AnnounceRoll(rollZone);
        }
        else if (_hwYawActive && yawZone != _prevYawZone)
        {
            _prevYawZone = yawZone;
            AnnounceYaw(yawZone);
        }
        else if (_hwThrottleActive && throttleZone != _prevThrottleZone)
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
        float pitch, roll, yaw, throttle;

        var state = _context.InputManager.GetAxisState();
        if (state is not null)
        {
            pitch = state.Pitch;
            roll = state.Roll;
            yaw = state.Yaw;
            throttle = state.Throttle;
        }
        else
        {
            pitch = _keyPitch;
            roll = _keyRoll;
            yaw = _keyYaw;
            throttle = _keyThrottle;
        }

        string pitchDesc = pitch switch
        {
            < -0.5f => "full forward",
            < -0.15f => "forward",
            > 0.5f => "full back",
            > 0.15f => "back",
            _ => "centered"
        };
        string rollDesc = roll switch
        {
            < -0.5f => "full left",
            < -0.15f => "left",
            > 0.5f => "full right",
            > 0.15f => "right",
            _ => "wings level"
        };
        string yawDesc = yaw switch
        {
            < -0.5f => "full left",
            < -0.15f => "left",
            > 0.5f => "full right",
            > 0.15f => "right",
            _ => "centered"
        };

        _context.Speech.Speak($"Yoke {pitchDesc}, {rollDesc}. " +
            $"Rudder {yawDesc}. " +
            $"Throttle at {(int)(throttle * 100)} percent.", true);
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
