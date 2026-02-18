using System.Text.Json.Serialization;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Aircraft;

public class Zone
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public ZoneType Type { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("minBound")]
    public int[] MinBound { get; set; } = [0, 0, 0];

    [JsonPropertyName("maxBound")]
    public int[] MaxBound { get; set; } = [0, 0, 0];

    [JsonPropertyName("isExterior")]
    public bool IsExterior { get; set; }

    public Coordinate3D Min => new(MinBound[0], MinBound[1], MinBound[2]);
    public Coordinate3D Max => new(MaxBound[0], MaxBound[1], MaxBound[2]);

    public bool Contains(Coordinate3D pos) =>
        pos.X >= MinBound[0] && pos.X <= MaxBound[0] &&
        pos.Y >= MinBound[1] && pos.Y <= MaxBound[1] &&
        pos.Z >= MinBound[2] && pos.Z <= MaxBound[2];

    public int Volume =>
        (MaxBound[0] - MinBound[0] + 1) *
        (MaxBound[1] - MinBound[1] + 1) *
        (MaxBound[2] - MinBound[2] + 1);
}
