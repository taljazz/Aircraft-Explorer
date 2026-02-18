using AircraftExplorer.Aircraft;
using AircraftExplorer.Input;

namespace AircraftExplorer.FlightSim;

public class FlightControlSession : IFlightControlInterpreter
{
    private const float DefaultThreshold = 0.15f;

    private readonly AircraftModel _aircraft;

    public FlightControlSession(AircraftModel aircraft)
    {
        _aircraft = aircraft;
    }

    public IReadOnlyList<ControlEffect> InterpretAxisChange(AxisState previousState, AxisState currentState)
    {
        var effects = new List<ControlEffect>();

        CheckAxis(effects, "pitch", previousState.Pitch, currentState.Pitch);
        CheckAxis(effects, "roll", previousState.Roll, currentState.Roll);
        CheckAxis(effects, "yaw", previousState.Yaw, currentState.Yaw);
        CheckAxis(effects, "throttle", previousState.Throttle, currentState.Throttle);

        return effects;
    }

    private void CheckAxis(List<ControlEffect> effects, string axis, float previous, float current)
    {
        float delta = current - previous;
        if (MathF.Abs(delta) < DefaultThreshold)
            return;

        string inputDescription = axis switch
        {
            "pitch" => delta > 0 ? "Pitch back" : "Pitch forward",
            "roll" => delta > 0 ? "Roll right" : "Roll left",
            "yaw" => delta > 0 ? "Yaw right" : "Yaw left",
            "throttle" => delta > 0 ? "Throttle increase" : "Throttle decrease",
            _ => $"{axis} change"
        };

        var surfaces = _aircraft.ControlSurfaces
            .Where(s => s.Axis.Equals(axis, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (surfaces.Count == 0)
        {
            effects.Add(new ControlEffect
            {
                InputDescription = inputDescription,
                SurfaceName = axis,
                SurfaceMovement = delta > 0 ? "positive deflection" : "negative deflection",
                FlightEffect = GetDefaultFlightEffect(axis, delta)
            });
            return;
        }

        foreach (var surface in surfaces)
        {
            effects.Add(new ControlEffect
            {
                InputDescription = inputDescription,
                SurfaceName = surface.Name,
                SurfaceMovement = surface.MovementDescription,
                FlightEffect = surface.FlightEffect
            });
        }
    }

    private static string GetDefaultFlightEffect(string axis, float delta) => axis switch
    {
        "pitch" => delta > 0 ? "Nose pitches up" : "Nose pitches down",
        "roll" => delta > 0 ? "Aircraft banks right" : "Aircraft banks left",
        "yaw" => delta > 0 ? "Nose yaws right" : "Nose yaws left",
        "throttle" => delta > 0 ? "Increasing thrust" : "Decreasing thrust",
        _ => "Control input applied"
    };
}
