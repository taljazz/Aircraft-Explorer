using System.Text.Json.Serialization;

namespace AircraftExplorer.Tours;

public class TourStop
{
    [JsonPropertyName("componentId")]
    public string ComponentId { get; set; } = "";

    [JsonPropertyName("narration")]
    public string Narration { get; set; } = "";

    [JsonPropertyName("arrivalNarration")]
    public string ArrivalNarration { get; set; } = "";
}
