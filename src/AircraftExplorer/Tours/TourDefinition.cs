using System.Text.Json.Serialization;

namespace AircraftExplorer.Tours;

public class TourDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("isExterior")]
    public bool IsExterior { get; set; }

    [JsonPropertyName("stops")]
    public List<TourStop> Stops { get; set; } = [];
}
