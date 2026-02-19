using AircraftExplorer.Aircraft;

namespace AircraftExplorer.Navigation;

public class NavigationGrid : INavigationSpace
{
    private readonly AircraftModel _aircraft;
    private readonly BoundaryDefinition _boundary;

    public NavigationGrid(AircraftModel aircraft)
    {
        _aircraft = aircraft;
        _boundary = new BoundaryDefinition(aircraft.GridBounds);
    }

    public MovementResult TryMove(Coordinate3D from, int dx, int dy, int dz)
    {
        var target = from.Move(dx, dy, dz);

        if (!IsInBounds(target))
        {
            return new MovementResult
            {
                Success = false,
                NewPosition = from,
                CurrentZone = GetZoneAt(from),
                PreviousZone = GetZoneAt(from),
                BoundaryMessage = "You've reached the boundary of the aircraft.",
                NearbyComponents = GetNearbyComponents(from)
            };
        }

        var targetZone = GetZoneAt(target);
        if (targetZone is null)
        {
            return new MovementResult
            {
                Success = false,
                NewPosition = from,
                CurrentZone = GetZoneAt(from),
                PreviousZone = GetZoneAt(from),
                BoundaryMessage = "You can't go that way.",
                NearbyComponents = GetNearbyComponents(from)
            };
        }

        var previousZone = GetZoneAt(from);
        var currentZone = targetZone;
        var nearby = GetNearbyComponents(target);

        return new MovementResult
        {
            Success = true,
            NewPosition = target,
            CurrentZone = currentZone,
            PreviousZone = previousZone,
            NearbyComponents = nearby
        };
    }

    public Zone? GetZoneAt(Coordinate3D position)
    {
        Zone? bestMatch = null;

        foreach (var zone in _aircraft.Zones)
        {
            if (zone.IsExterior)
                continue;

            if (zone.Contains(position))
            {
                if (bestMatch is null || zone.Volume < bestMatch.Volume)
                    bestMatch = zone;
            }
        }

        return bestMatch;
    }

    public IReadOnlyList<Component> GetNearbyComponents(Coordinate3D position, double radius = 2.0)
    {
        return _aircraft.Components
            .Where(c => position.DistanceTo(c.Coordinate) <= radius)
            .OrderBy(c => position.DistanceTo(c.Coordinate))
            .ToList();
    }

    public bool IsInBounds(Coordinate3D position) => _boundary.IsInBounds(position);
}
