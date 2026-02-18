using AircraftExplorer.Aircraft;

namespace AircraftExplorer.Navigation;

public interface INavigationSpace
{
    MovementResult TryMove(Coordinate3D from, int dx, int dy, int dz);
    Zone? GetZoneAt(Coordinate3D position);
    IReadOnlyList<Component> GetNearbyComponents(Coordinate3D position, double radius = 2.0);
    bool IsInBounds(Coordinate3D position);
}
