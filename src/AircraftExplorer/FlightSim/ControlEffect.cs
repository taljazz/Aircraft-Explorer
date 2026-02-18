namespace AircraftExplorer.FlightSim;

public class ControlEffect
{
    public string InputDescription { get; init; } = "";
    public string SurfaceName { get; init; } = "";
    public string SurfaceMovement { get; init; } = "";
    public string FlightEffect { get; init; } = "";

    public string ToNarration() =>
        $"{InputDescription} -- {SurfaceName} {SurfaceMovement} -- {FlightEffect}";
}
