using System.Text.Json.Serialization;

namespace AircraftExplorer.Aircraft;

public class ControlSurface
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("axis")]
    public string Axis { get; set; } = "";

    [JsonPropertyName("movementDescription")]
    public string MovementDescription { get; set; } = "";

    [JsonPropertyName("flightEffect")]
    public string FlightEffect { get; set; } = "";

    [JsonPropertyName("componentId")]
    public string ComponentId { get; set; } = "";
}
