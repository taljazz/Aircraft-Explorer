using System.Text.Json.Serialization;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Aircraft;

public class Component
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("category")]
    public ComponentCategory Category { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("position")]
    public int[] Position { get; set; } = [0, 0, 0];

    [JsonPropertyName("interactionRadius")]
    public double InteractionRadius { get; set; } = 1.5;

    [JsonPropertyName("interactionText")]
    public string InteractionText { get; set; } = "";

    [JsonPropertyName("educationTopicIds")]
    public List<string> EducationTopicIds { get; set; } = [];

    public Coordinate3D Coordinate => new(Position[0], Position[1], Position[2]);
}
