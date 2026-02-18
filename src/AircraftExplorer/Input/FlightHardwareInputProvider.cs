using SharpDX.DirectInput;

namespace AircraftExplorer.Input;

public sealed class FlightHardwareInputProvider : IInputProvider, IDisposable
{
    private readonly DirectInput? _directInput;
    private readonly Joystick? _joystick;
    private bool _isAvailable;
    private bool _disposed;

    public FlightHardwareInputProvider()
    {
        try
        {
            _directInput = new DirectInput();
            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

            if (devices.Count == 0)
            {
                _isAvailable = false;
                return;
            }

            _joystick = new Joystick(_directInput, devices[0].InstanceGuid);
            _joystick.Properties.BufferSize = 128;
            _joystick.Acquire();
            _isAvailable = true;
        }
        catch
        {
            _isAvailable = false;
        }
    }

    public bool IsAvailable => _isAvailable;

    private bool _button0WasPressed;
    private bool _button1WasPressed;

    public InputAction? Poll()
    {
        if (!_isAvailable || _joystick is null)
            return null;

        try
        {
            _joystick.Poll();
            var state = _joystick.GetCurrentState();
            var buttons = state.Buttons;

            // Button 0 (trigger/primary) -> Select/Enter
            if (buttons.Length > 0 && buttons[0] && !_button0WasPressed)
            {
                _button0WasPressed = true;
                return InputAction.Select;
            }
            if (buttons.Length > 0 && !buttons[0])
                _button0WasPressed = false;

            // Button 1 (secondary) -> Back/Escape
            if (buttons.Length > 1 && buttons[1] && !_button1WasPressed)
            {
                _button1WasPressed = true;
                return InputAction.Back;
            }
            if (buttons.Length > 1 && !buttons[1])
                _button1WasPressed = false;
        }
        catch
        {
            _isAvailable = false;
        }

        return null;
    }

    public AxisState? GetAxisState()
    {
        if (!_isAvailable || _joystick is null)
            return null;

        try
        {
            _joystick.Poll();
            var state = _joystick.GetCurrentState();

            return new AxisState
            {
                Pitch = NormalizeAxis(state.Y),
                Roll = NormalizeAxis(state.X),
                Yaw = NormalizeAxis(state.RotationZ),
                Throttle = NormalizeThrottle(state.Sliders[0])
            };
        }
        catch
        {
            _isAvailable = false;
            return null;
        }
    }

    private static float NormalizeAxis(int raw)
    {
        // Raw range is 0-65535, center at 32767. Normalize to -1.0..1.0.
        return (raw - 32767.5f) / 32767.5f;
    }

    private static float NormalizeThrottle(int raw)
    {
        // Raw range is 0-65535. Normalize to 0.0..1.0.
        return raw / 65535f;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _joystick?.Unacquire();
        _joystick?.Dispose();
        _directInput?.Dispose();
    }
}
