using System.Text.Json.Serialization;

namespace AircraftExplorer.Education;

public class EducationTopic
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("category")]
    public TopicCategory Category { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("componentIds")]
    public List<string> ComponentIds { get; set; } = [];

    [JsonPropertyName("aircraftIds")]
    public List<string> AircraftIds { get; set; } = [];

    [JsonPropertyName("quizQuestions")]
    public List<QuizQuestion> QuizQuestions { get; set; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TopicCategory
{
    HowItWorks,
    History,
    Specs,
    Safety,
    Operations,
    Design
}
