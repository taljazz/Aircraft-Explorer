using AircraftExplorer.Aircraft;

namespace AircraftExplorer.Navigation;

public class GridNavigator : INavigationSpace
{
    private readonly AircraftModel _aircraft;
    private readonly List<Zone> _exteriorZones;
    private readonly BoundaryDefinition _boundary;

    public GridNavigator(AircraftModel aircraft)
    {
        _aircraft = aircraft;
        _boundary = new BoundaryDefinition(aircraft.GridBounds);
        _exteriorZones = aircraft.Zones
            .Where(z => z.IsExterior)
            .OrderBy(z => z.MinBound[1])
            .ThenBy(z => z.MinBound[0])
            .ToList();
    }

    public IReadOnlyList<Zone> ExteriorZones => _exteriorZones;

    public MovementResult TryMove(Coordinate3D from, int dx, int dy, int dz)
    {
        var currentZone = GetZoneAt(from);
        var currentIndex = currentZone is not null ? _exteriorZones.IndexOf(currentZone) : -1;

        // Map movement direction to zone traversal
        int step = 0;
        if (dy > 0 || dx > 0) step = 1;
        else if (dy < 0 || dx < 0) step = -1;

        if (step == 0 && dz != 0)
        {
            // Vertical movement not applicable in exterior grid traversal
            return new MovementResult
            {
                Success = false,
                NewPosition = from,
                CurrentZone = currentZone,
                PreviousZone = currentZone,
                BoundaryMessage = "Use the walk-around mode for detailed vertical exploration.",
                NearbyComponents = GetNearbyComponents(from)
            };
        }

        int targetIndex = currentIndex + step;

        if (targetIndex < 0 || targetIndex >= _exteriorZones.Count)
        {
            return new MovementResult
            {
                Success = false,
                NewPosition = from,
                CurrentZone = currentZone,
                PreviousZone = currentZone,
                BoundaryMessage = step > 0
                    ? "You've reached the end of the aircraft exterior."
                    : "You've reached the beginning of the aircraft exterior.",
                NearbyComponents = GetNearbyComponents(from)
            };
        }

        var nextZone = _exteriorZones[targetIndex];
        // Position at the center of the target zone
        var newPosition = new Coordinate3D(
            (nextZone.MinBound[0] + nextZone.MaxBound[0]) / 2,
            (nextZone.MinBound[1] + nextZone.MaxBound[1]) / 2,
            (nextZone.MinBound[2] + nextZone.MaxBound[2]) / 2
        );

        return new MovementResult
        {
            Success = true,
            NewPosition = newPosition,
            CurrentZone = nextZone,
            PreviousZone = currentZone,
            NearbyComponents = GetNearbyComponents(newPosition)
        };
    }

    public Zone? GetZoneAt(Coordinate3D position)
    {
        Zone? bestMatch = null;

        foreach (var zone in _exteriorZones)
        {
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
