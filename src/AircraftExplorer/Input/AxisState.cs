namespace AircraftExplorer.Input;

public class AxisState
{
    /// <summary>Pitch axis: -1.0 (full forward) to 1.0 (full back).</summary>
    public float Pitch { get; set; }

    /// <summary>Roll axis: -1.0 (full left) to 1.0 (full right).</summary>
    public float Roll { get; set; }

    /// <summary>Yaw axis: -1.0 (full left) to 1.0 (full right).</summary>
    public float Yaw { get; set; }

    /// <summary>Throttle: 0.0 (idle) to 1.0 (full).</summary>
    public float Throttle { get; set; }
}
