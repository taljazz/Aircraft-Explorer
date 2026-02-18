namespace AircraftExplorer.Education;

public interface IEducationProvider
{
    IReadOnlyList<EducationTopic> GetTopicsForComponent(string componentId);
    IReadOnlyList<EducationTopic> GetTopicsForAircraft(string aircraftId);
    IReadOnlyList<EducationTopic> GetCommonTopics();
}
