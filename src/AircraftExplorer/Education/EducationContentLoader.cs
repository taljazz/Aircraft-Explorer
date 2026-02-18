using System.Text.Json;

namespace AircraftExplorer.Education;

public class EducationContentLoader : IEducationProvider
{
    private readonly Dictionary<string, List<EducationTopic>> _byComponentId = new();
    private readonly Dictionary<string, List<EducationTopic>> _byAircraftId = new();
    private readonly List<EducationTopic> _commonTopics = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EducationContentLoader(string educationDataDirectory)
    {
        LoadAll(educationDataDirectory);
    }

    private void LoadAll(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        foreach (var file in Directory.GetFiles(directoryPath, "*.json"))
        {
            var json = File.ReadAllText(file);
            var topics = JsonSerializer.Deserialize<List<EducationTopic>>(json, JsonOptions);
            if (topics is null)
                continue;

            foreach (var topic in topics)
            {
                bool isCommon = topic.ComponentIds.Count == 0 && topic.AircraftIds.Count == 0;

                if (isCommon)
                {
                    _commonTopics.Add(topic);
                }

                foreach (var componentId in topic.ComponentIds)
                {
                    if (!_byComponentId.TryGetValue(componentId, out var list))
                    {
                        list = [];
                        _byComponentId[componentId] = list;
                    }
                    list.Add(topic);
                }

                foreach (var aircraftId in topic.AircraftIds)
                {
                    if (!_byAircraftId.TryGetValue(aircraftId, out var list))
                    {
                        list = [];
                        _byAircraftId[aircraftId] = list;
                    }
                    list.Add(topic);
                }
            }
        }
    }

    public IReadOnlyList<EducationTopic> GetTopicsForComponent(string componentId)
    {
        return _byComponentId.TryGetValue(componentId, out var list) ? list : [];
    }

    public IReadOnlyList<EducationTopic> GetTopicsForAircraft(string aircraftId)
    {
        return _byAircraftId.TryGetValue(aircraftId, out var list) ? list : [];
    }

    public IReadOnlyList<EducationTopic> GetCommonTopics()
    {
        return _commonTopics;
    }
}
