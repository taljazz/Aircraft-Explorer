namespace AircraftExplorer.Navigation;

public readonly record struct Coordinate3D(int X, int Y, int Z)
{
    public static Coordinate3D Origin => new(0, 0, 0);

    public Coordinate3D Move(int dx, int dy, int dz) => new(X + dx, Y + dy, Z + dz);

    public double DistanceTo(Coordinate3D other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        int dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public override string ToString() => $"{X},{Y},{Z}";
}
