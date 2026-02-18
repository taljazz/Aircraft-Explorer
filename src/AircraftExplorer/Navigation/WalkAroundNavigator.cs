using AircraftExplorer.Aircraft;

namespace AircraftExplorer.Navigation;

public class WalkAroundNavigator : INavigationSpace
{
    private readonly AircraftModel _aircraft;
    private readonly BoundaryDefinition _boundary;
    private readonly int _groundZ;

    public WalkAroundNavigator(AircraftModel aircraft)
    {
        _aircraft = aircraft;
        _boundary = new BoundaryDefinition(aircraft.GridBounds);
        // Ground level is at the bottom of the aircraft grid
        _groundZ = aircraft.GridBounds.MinZ;
    }

    public int GroundZ => _groundZ;

    public MovementResult TryMove(Coordinate3D from, int dx, int dy, int dz)
    {
        var target = from.Move(dx, dy, dz);

        if (!IsInBounds(target))
        {
            string boundaryMessage = dz > 0
                ? "You've reached the top of the aircraft."
                : dz < 0
                    ? "You're at ground level."
                    : "You've reached the edge of the walk-around area.";

            return new MovementResult
            {
                Success = false,
                NewPosition = from,
                CurrentZone = GetZoneAt(from),
                PreviousZone = GetZoneAt(from),
                BoundaryMessage = boundaryMessage,
                NearbyComponents = GetNearbyComponents(from)
            };
        }

        // Check that target is within an exterior zone
        var targetZone = GetZoneAt(target);
        if (targetZone is null)
        {
            return new MovementResult
            {
                Success = false,
                NewPosition = from,
                CurrentZone = GetZoneAt(from),
                PreviousZone = GetZoneAt(from),
                BoundaryMessage = "Nothing to explore in that direction.",
                NearbyComponents = GetNearbyComponents(from)
            };
        }

        var previousZone = GetZoneAt(from);

        return new MovementResult
        {
            Success = true,
            NewPosition = target,
            CurrentZone = targetZone,
            PreviousZone = previousZone,
            NearbyComponents = GetNearbyComponents(target)
        };
    }

    public Zone? GetZoneAt(Coordinate3D position)
    {
        Zone? bestMatch = null;

        foreach (var zone in _aircraft.Zones)
        {
            if (!zone.IsExterior)
                continue;

            if (zone.Contains(position))
            {
                if (bestMatch is null || zone.Volume < bestMatch.Volume)
                    bestMatch = zone;
            }
        }

        return bestMatch;
    }

    public IReadOnlyList<Component> GetNearbyComponents(Coordinate3D position, double radius = 3.0)
    {
        return _aircraft.Components
            .Where(c => position.DistanceTo(c.Coordinate) <= radius)
            .OrderBy(c => position.DistanceTo(c.Coordinate))
            .ToList();
    }

    public bool IsInBounds(Coordinate3D position) =>
        _boundary.IsInBounds(position);
}
