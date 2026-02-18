using System.Text.Json;
using SharpDX.DirectInput;

namespace AircraftExplorer.Input;

public sealed class AxisMapping
{
    public int PitchAxisIndex { get; set; } = 1;
    public int RollAxisIndex { get; set; } = 0;
    public int YawAxisIndex { get; set; } = 3;
    public int ThrottleAxisIndex { get; set; } = 4;

    public bool InvertPitch { get; set; }
    public bool InvertRoll { get; set; }
    public bool InvertYaw { get; set; }
    public bool InvertThrottle { get; set; }

    public float Deadzone { get; set; } = 0.05f;

    public static AxisMapping LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AxisMapping>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AxisMapping();
    }

    public AxisState ApplyMapping(JoystickState state)
    {
        var axes = new[]
        {
            state.X, state.Y, state.Z,
            state.RotationZ,
            state.Sliders.Length > 0 ? state.Sliders[0] : 32767
        };

        float pitch = NormalizeAxis(GetAxis(axes, PitchAxisIndex), InvertPitch);
        float roll = NormalizeAxis(GetAxis(axes, RollAxisIndex), InvertRoll);
        float yaw = NormalizeAxis(GetAxis(axes, YawAxisIndex), InvertYaw);
        float throttle = NormalizeThrottle(GetAxis(axes, ThrottleAxisIndex), InvertThrottle);

        return new AxisState
        {
            Pitch = ApplyDeadzone(pitch),
            Roll = ApplyDeadzone(roll),
            Yaw = ApplyDeadzone(yaw),
            Throttle = throttle
        };
    }

    private static int GetAxis(int[] axes, int index)
    {
        if (index >= 0 && index < axes.Length)
            return axes[index];
        return 32767;
    }

    private static float NormalizeAxis(int raw, bool invert)
    {
        float normalized = (raw - 32767.5f) / 32767.5f;
        return invert ? -normalized : normalized;
    }

    private static float NormalizeThrottle(int raw, bool invert)
    {
        float normalized = raw / 65535f;
        return invert ? 1f - normalized : normalized;
    }

    private float ApplyDeadzone(float value)
    {
        if (MathF.Abs(value) < Deadzone)
            return 0f;
        return value;
    }
}
