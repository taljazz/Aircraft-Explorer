using AircraftExplorer.Aircraft;

namespace AircraftExplorer.Navigation;

/// <summary>
/// Generates rich, aviation-standard accessible descriptions for blind users.
/// Direction vocabulary:
///   -Y = forward (toward nose), +Y = aft (toward tail)
///   +X = starboard, -X = port
///   +Z = up, -Z = down
/// </summary>
public static class NavigationAnnouncer
{
    private const int BoundaryWarningDistance = 2;

    /// <summary>Describes the movement direction in aviation terms.</summary>
    public static string DescribeMovement(int dx, int dy, int dz)
    {
        var directions = new List<string>();

        if (dy < 0) directions.Add("forward");
        else if (dy > 0) directions.Add("aft");

        if (dx > 0) directions.Add("starboard");
        else if (dx < 0) directions.Add("port");

        if (dz > 0) directions.Add("up");
        else if (dz < 0) directions.Add("down");

        if (directions.Count == 0) return "Moved";
        return "Moved " + string.Join(" and ", directions);
    }

    /// <summary>Describes position relative to aircraft sections (fore/aft, port/starboard, level).</summary>
    public static string DescribeRelativePosition(Coordinate3D pos, AircraftModel aircraft)
    {
        var bounds = aircraft.GridBounds;
        var parts = new List<string>();

        // Longitudinal (Y axis: fore/aft)
        float yRange = bounds.MaxY - bounds.MinY;
        if (yRange > 0)
        {
            float yRatio = (float)(pos.Y - bounds.MinY) / yRange;
            if (yRatio > 0.66f) parts.Add("Aft section");
            else if (yRatio > 0.33f) parts.Add("Mid section");
            else parts.Add("Forward section");
        }

        // Lateral (X axis: port/starboard)
        float xRange = bounds.MaxX - bounds.MinX;
        if (xRange > 0)
        {
            float xMid = (bounds.MinX + bounds.MaxX) / 2f;
            if (pos.X > xMid + 1) parts.Add("starboard side");
            else if (pos.X < xMid - 1) parts.Add("port side");
            else parts.Add("centerline");
        }

        // Vertical (Z axis)
        float zRange = bounds.MaxZ - bounds.MinZ;
        if (zRange > 0)
        {
            float zRatio = (float)(pos.Z - bounds.MinZ) / zRange;
            if (zRatio > 0.66f) parts.Add("upper level");
            else if (zRatio > 0.33f) parts.Add("cabin level");
            else parts.Add("lower level");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "Unknown position";
    }

    /// <summary>Describes nearby components with distance and bearing relative to position.</summary>
    public static string? DescribeNearbyComponents(Coordinate3D pos, IReadOnlyList<Component> components)
    {
        if (components.Count == 0) return null;

        var descriptions = new List<string>();
        foreach (var component in components)
        {
            double distance = pos.DistanceTo(component.Coordinate);
            string proximity = distance < 1.0 ? "within reach" : $"{(int)Math.Round(distance)} steps away";
            string bearing = GetBearing(pos, component.Coordinate);
            string bearingText = string.IsNullOrEmpty(bearing) ? "" : $", {bearing}";
            descriptions.Add($"{component.Name} {proximity}{bearingText}");
        }

        return string.Join(". ", descriptions);
    }

    /// <summary>
    /// Describes structural edges when inside an exterior zone — wing tips, leading/trailing edges,
    /// stabilizer tips, fin top, engine inlet/exhaust, nose tip, tail cone.
    /// Returns null if no structural edge is nearby or zone is not exterior.
    /// </summary>
    public static string? DescribeStructuralEdges(Coordinate3D pos, Zone? zone)
    {
        if (zone is null || !zone.IsExterior)
            return null;

        var descriptions = new List<string>();
        int minX = zone.MinBound[0], maxX = zone.MaxBound[0];
        int minY = zone.MinBound[1], maxY = zone.MaxBound[1];
        int minZ = zone.MinBound[2], maxZ = zone.MaxBound[2];

        switch (zone.Type)
        {
            case ZoneType.LeftWing:
                if (pos.X <= minX + BoundaryWarningDistance) descriptions.Add("Approaching port wingtip");
                if (pos.Y <= minY + BoundaryWarningDistance) descriptions.Add("Approaching wing leading edge");
                if (pos.Y >= maxY - BoundaryWarningDistance) descriptions.Add("Approaching wing trailing edge");
                if (pos.Z <= minZ) descriptions.Add("Near the wing underside");
                if (pos.Z >= maxZ) descriptions.Add("On top of the wing");
                break;

            case ZoneType.RightWing:
                if (pos.X >= maxX - BoundaryWarningDistance) descriptions.Add("Approaching starboard wingtip");
                if (pos.Y <= minY + BoundaryWarningDistance) descriptions.Add("Approaching wing leading edge");
                if (pos.Y >= maxY - BoundaryWarningDistance) descriptions.Add("Approaching wing trailing edge");
                if (pos.Z <= minZ) descriptions.Add("Near the wing underside");
                if (pos.Z >= maxZ) descriptions.Add("On top of the wing");
                break;

            case ZoneType.HorizontalStabilizer:
                if (pos.X <= minX + BoundaryWarningDistance) descriptions.Add("Approaching port stabilizer tip");
                if (pos.X >= maxX - BoundaryWarningDistance) descriptions.Add("Approaching starboard stabilizer tip");
                if (pos.Y <= minY + BoundaryWarningDistance) descriptions.Add("Approaching stabilizer leading edge");
                if (pos.Y >= maxY - BoundaryWarningDistance) descriptions.Add("Approaching stabilizer trailing edge");
                break;

            case ZoneType.VerticalStabilizer:
                if (pos.Z >= maxZ - BoundaryWarningDistance) descriptions.Add("Approaching top of the fin");
                if (pos.Z <= minZ) descriptions.Add("At the base of the vertical stabilizer");
                if (pos.Y <= minY + BoundaryWarningDistance) descriptions.Add("Approaching fin leading edge");
                if (pos.Y >= maxY - BoundaryWarningDistance) descriptions.Add("At the trailing edge of the fin");
                break;

            case ZoneType.LeftEngine:
            case ZoneType.RightEngine:
                if (pos.Y <= minY + BoundaryWarningDistance) descriptions.Add("At the engine inlet");
                if (pos.Y >= maxY - BoundaryWarningDistance) descriptions.Add("At the engine exhaust");
                break;

            case ZoneType.Nose:
                if (pos.Y <= minY + BoundaryWarningDistance) descriptions.Add("At the nose tip");
                break;

            case ZoneType.Tail:
                if (pos.Y >= maxY - BoundaryWarningDistance) descriptions.Add("At the tail cone");
                break;
        }

        return descriptions.Count > 0 ? string.Join(". ", descriptions) : null;
    }

    /// <summary>Returns a boundary warning if within 2 steps of any edge, or null.</summary>
    public static string? DescribeBoundaryProximity(Coordinate3D pos, GridBounds bounds)
    {
        var warnings = new List<string>();

        if (pos.Y >= bounds.MaxY - BoundaryWarningDistance) warnings.Add("tail");
        if (pos.Y <= bounds.MinY + BoundaryWarningDistance) warnings.Add("nose");
        if (pos.X >= bounds.MaxX - BoundaryWarningDistance) warnings.Add("starboard");
        if (pos.X <= bounds.MinX + BoundaryWarningDistance) warnings.Add("port");
        if (pos.Z >= bounds.MaxZ - BoundaryWarningDistance) warnings.Add("upper");
        if (pos.Z <= bounds.MinZ + BoundaryWarningDistance) warnings.Add("lower");

        if (warnings.Count == 0) return null;
        return $"Approaching {string.Join(" and ", warnings)} boundary";
    }

    /// <summary>
    /// Builds the full movement announcement combining direction, zone, components, and boundaries.
    /// </summary>
    public static string BuildMovementAnnouncement(
        int dx, int dy, int dz,
        MovementResult result,
        Coordinate3D position,
        AircraftModel aircraft)
    {
        var parts = new List<string>();

        parts.Add(DescribeMovement(dx, dy, dz));

        // Zone info: name on change, description on first entry
        if (result.ZoneChanged && result.CurrentZone is not null)
        {
            parts.Add(result.CurrentZone.Name);
            if (!string.IsNullOrWhiteSpace(result.CurrentZone.Description))
                parts.Add(result.CurrentZone.Description);
        }

        // Nearby components with bearings
        var componentDesc = DescribeNearbyComponents(position, result.NearbyComponents);
        if (componentDesc is not null)
            parts.Add(componentDesc);

        // Structural edge descriptions for exterior zones
        if (result.CurrentZone is { IsExterior: true })
        {
            var edgeDesc = DescribeStructuralEdges(position, result.CurrentZone);
            if (edgeDesc is not null)
                parts.Add(edgeDesc);
        }

        // Boundary warnings
        var boundaryWarning = DescribeBoundaryProximity(position, aircraft.GridBounds);
        if (boundaryWarning is not null)
            parts.Add(boundaryWarning);

        return string.Join(". ", parts) + ".";
    }

    /// <summary>
    /// Builds a rich context announcement for the C-key (AnnouncePosition).
    /// </summary>
    public static string BuildFullContextAnnouncement(
        Coordinate3D position,
        AircraftModel aircraft,
        Zone? zone,
        IReadOnlyList<Component> nearbyComponents)
    {
        var parts = new List<string>();

        // Zone name and description
        if (zone is not null)
        {
            parts.Add(zone.Name);
            if (!string.IsNullOrWhiteSpace(zone.Description))
                parts.Add(zone.Description);
        }
        else
        {
            parts.Add("Unknown area");
        }

        // Relative position
        parts.Add(DescribeRelativePosition(position, aircraft));

        // Components with bearings
        var componentDesc = DescribeNearbyComponents(position, nearbyComponents);
        if (componentDesc is not null)
            parts.Add(componentDesc);

        // Boundary proximity
        var boundaryWarning = DescribeBoundaryProximity(position, aircraft.GridBounds);
        if (boundaryWarning is not null)
            parts.Add(boundaryWarning);

        return string.Join(". ", parts) + ".";
    }

    /// <summary>Gets a bearing description from one position toward another.</summary>
    private static string GetBearing(Coordinate3D from, Coordinate3D to)
    {
        var bearings = new List<string>();

        int dy = to.Y - from.Y;
        int dx = to.X - from.X;
        int dz = to.Z - from.Z;

        if (dy < 0) bearings.Add("ahead");
        else if (dy > 0) bearings.Add("behind");

        if (dx > 0) bearings.Add("to starboard");
        else if (dx < 0) bearings.Add("to port");

        if (dz > 0) bearings.Add("above");
        else if (dz < 0) bearings.Add("below");

        return string.Join(" and ", bearings);
    }
}
