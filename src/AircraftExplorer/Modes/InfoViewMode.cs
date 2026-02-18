using AircraftExplorer.Education;
using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class InfoViewMode : IAppMode
{
    private readonly IReadOnlyList<EducationTopic> _topics;
    private int _selectedIndex;
    private ModeContext _context = null!;

    public string ModeName => "Information View";

    public InfoViewMode(IReadOnlyList<EducationTopic> topics)
    {
        _topics = topics;
    }

    public void OnEnter(ModeContext context)
    {
        _context = context;
        _selectedIndex = 0;

        if (_topics.Count == 0)
        {
            context.Speech.Speak("No information available. Press Escape to return.", true);
            return;
        }

        var topic = _topics[_selectedIndex];
        context.Speech.Speak(
            $"Information. {_topics.Count} topics available. " +
            $"1 of {_topics.Count}: {topic.Category} -- {topic.Title}. " +
            $"Enter to read aloud, R to open in text window.",
            true);
    }

    public ModeResult HandleInput(InputAction action)
    {
        if (_topics.Count == 0 && action == InputAction.Back)
            return ModeResult.Pop;

        switch (action)
        {
            case InputAction.MoveForward:
            case InputAction.MenuUp:
            case InputAction.MoveUp:
                if (_topics.Count == 0) return ModeResult.Stay;
                _selectedIndex = (_selectedIndex - 1 + _topics.Count) % _topics.Count;
                AnnounceCurrentTopic();
                return ModeResult.Stay;

            case InputAction.MoveBackward:
            case InputAction.MenuDown:
            case InputAction.MoveDown:
                if (_topics.Count == 0) return ModeResult.Stay;
                _selectedIndex = (_selectedIndex + 1) % _topics.Count;
                AnnounceCurrentTopic();
                return ModeResult.Stay;

            case InputAction.Select:
                if (_topics.Count == 0) return ModeResult.Stay;
                ReadCurrentTopic();
                return ModeResult.Stay;

            case InputAction.ReadTopic:
                if (_topics.Count == 0) return ModeResult.Stay;
                OpenTopicReader();
                return ModeResult.Stay;

            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    "Information view. Up and Down to browse topics. Enter to read aloud. " +
                    "R to open in a readable text window. Escape to go back.",
                    true);
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    private void AnnounceCurrentTopic()
    {
        var topic = _topics[_selectedIndex];
        _context.Speech.Speak(
            $"{_selectedIndex + 1} of {_topics.Count}: {topic.Category} -- {topic.Title}.",
            true);
    }

    private void ReadCurrentTopic()
    {
        var topic = _topics[_selectedIndex];
        _context.Speech.Speak($"{topic.Title}. {topic.Content}", false);
    }

    private void OpenTopicReader()
    {
        var topic = _topics[_selectedIndex];
        using var form = new TopicReaderForm(topic);
        form.ShowDialog();
    }

    public void OnExit() { }

    public void OnResume()
    {
        if (_topics.Count > 0)
        {
            AnnounceCurrentTopic();
        }
    }
}
