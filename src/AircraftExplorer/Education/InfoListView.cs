namespace AircraftExplorer.Education;

public class InfoListView
{
    private readonly IReadOnlyList<EducationTopic> _topics;
    private int _currentIndex;

    public InfoListView(IReadOnlyList<EducationTopic> topics)
    {
        _topics = topics;
        _currentIndex = 0;
    }

    public int CurrentIndex => _currentIndex;
    public int Count => _topics.Count;
    public bool IsEmpty => _topics.Count == 0;

    public bool MoveNext()
    {
        if (IsEmpty)
            return false;

        _currentIndex = (_currentIndex + 1) % _topics.Count;
        return true;
    }

    public bool MovePrevious()
    {
        if (IsEmpty)
            return false;

        _currentIndex = (_currentIndex - 1 + _topics.Count) % _topics.Count;
        return true;
    }

    public string GetCurrentAnnouncement()
    {
        if (IsEmpty)
            return "No topics available.";

        var topic = _topics[_currentIndex];
        return $"{_currentIndex + 1} of {_topics.Count}: {topic.Category} -- {topic.Title}";
    }

    public EducationTopic? GetCurrentTopic()
    {
        if (IsEmpty)
            return null;

        return _topics[_currentIndex];
    }
}
