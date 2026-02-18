using AircraftExplorer.Aircraft;

namespace AircraftExplorer.Navigation;

public class BoundaryDefinition
{
    private readonly GridBounds _bounds;

    public BoundaryDefinition(GridBounds bounds)
    {
        _bounds = bounds;
    }

    public bool IsInBounds(Coordinate3D position) =>
        position.X >= _bounds.MinX && position.X <= _bounds.MaxX &&
        position.Y >= _bounds.MinY && position.Y <= _bounds.MaxY &&
        position.Z >= _bounds.MinZ && position.Z <= _bounds.MaxZ;
}
