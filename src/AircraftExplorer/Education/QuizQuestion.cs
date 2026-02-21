using System.Text.Json.Serialization;

namespace AircraftExplorer.Education;

public class QuizQuestion
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("question")]
    public string Question { get; set; } = "";

    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = [];

    [JsonPropertyName("correctIndex")]
    public int CorrectIndex { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";
}
