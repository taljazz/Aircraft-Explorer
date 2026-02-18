using System.Text.Json.Serialization;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Aircraft;

public class AircraftModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = "";

    [JsonPropertyName("variant")]
    public string Variant { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("dimensions")]
    public AircraftDimensions Dimensions { get; set; } = new();

    [JsonPropertyName("gridBounds")]
    public GridBounds GridBounds { get; set; } = new();

    [JsonPropertyName("entryPosition")]
    public int[] EntryPosition { get; set; } = [0, 0, 0];

    [JsonPropertyName("cockpitSeatPosition")]
    public int[] CockpitSeatPosition { get; set; } = [0, 0, 0];

    [JsonPropertyName("zones")]
    public List<Zone> Zones { get; set; } = [];

    [JsonPropertyName("components")]
    public List<Component> Components { get; set; } = [];

    [JsonPropertyName("controlSurfaces")]
    public List<ControlSurface> ControlSurfaces { get; set; } = [];

    public Coordinate3D EntryCoordinate => new(EntryPosition[0], EntryPosition[1], EntryPosition[2]);
    public Coordinate3D CockpitSeatCoordinate => new(CockpitSeatPosition[0], CockpitSeatPosition[1], CockpitSeatPosition[2]);
}

public class AircraftDimensions
{
    [JsonPropertyName("lengthMeters")]
    public double LengthMeters { get; set; }

    [JsonPropertyName("wingspanMeters")]
    public double WingspanMeters { get; set; }

    [JsonPropertyName("heightMeters")]
    public double HeightMeters { get; set; }

    [JsonPropertyName("cabinWidthMeters")]
    public double CabinWidthMeters { get; set; }
}

public class GridBounds
{
    [JsonPropertyName("minX")]
    public int MinX { get; set; }

    [JsonPropertyName("maxX")]
    public int MaxX { get; set; }

    [JsonPropertyName("minY")]
    public int MinY { get; set; }

    [JsonPropertyName("maxY")]
    public int MaxY { get; set; }

    [JsonPropertyName("minZ")]
    public int MinZ { get; set; }

    [JsonPropertyName("maxZ")]
    public int MaxZ { get; set; }
}
