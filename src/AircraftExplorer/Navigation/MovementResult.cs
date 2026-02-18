using AircraftExplorer.Aircraft;

namespace AircraftExplorer.Navigation;

public class MovementResult
{
    public bool Success { get; init; }
    public Coordinate3D NewPosition { get; init; }
    public Zone? CurrentZone { get; init; }
    public Zone? PreviousZone { get; init; }
    public bool ZoneChanged => CurrentZone?.Name != PreviousZone?.Name;
    public string? BoundaryMessage { get; init; }
    public IReadOnlyList<Component> NearbyComponents { get; init; } = [];
}
