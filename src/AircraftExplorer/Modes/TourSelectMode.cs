using AircraftExplorer.Input;
using AircraftExplorer.Tours;

namespace AircraftExplorer.Modes;

public class TourSelectMode : IAppMode
{
    private IReadOnlyList<TourDefinition> _tours = [];
    private int _selectedIndex;
    private ModeContext _context = null!;

    public string ModeName => "Tour Select";

    public void OnEnter(ModeContext context)
    {
        _context = context;

        // Filter tours to those that have at least one valid stop for the selected aircraft
        var aircraft = context.SelectedAircraft;
        if (aircraft is null)
        {
            context.Speech.Speak("No aircraft selected. Press Escape to go back.", true);
            return;
        }

        var componentIds = new HashSet<string>(aircraft.Components.Select(c => c.Id));
        _tours = context.AvailableTours
            .Where(t => t.Stops.Any(s => componentIds.Contains(s.ComponentId)))
            .ToList();

        _selectedIndex = 0;

        if (_tours.Count == 0)
        {
            context.Speech.Speak("No tours available for this aircraft. Press Escape to go back.", true);
            return;
        }

        var tour = _tours[_selectedIndex];
        context.Speech.Speak(
            $"Select a Tour. {_tours.Count} tours available. " +
            $"Use arrows to browse. {tour.Name}. {tour.Description}",
            true);
    }

    public ModeResult HandleInput(InputAction action)
    {
        if (_tours.Count == 0 && action == InputAction.Back)
            return ModeResult.Pop;

        switch (action)
        {
            case InputAction.MenuUp:
            case InputAction.MoveUp:
            case InputAction.MoveForward:
                if (_tours.Count == 0) return ModeResult.Stay;
                _selectedIndex = (_selectedIndex - 1 + _tours.Count) % _tours.Count;
                AnnounceCurrentTour();
                return ModeResult.Stay;

            case InputAction.MenuDown:
            case InputAction.MoveDown:
            case InputAction.MoveBackward:
                if (_tours.Count == 0) return ModeResult.Stay;
                _selectedIndex = (_selectedIndex + 1) % _tours.Count;
                AnnounceCurrentTour();
                return ModeResult.Stay;

            case InputAction.Select:
                if (_tours.Count == 0) return ModeResult.Stay;
                return ModeResult.SwitchTo(new GuidedTourMode(_tours[_selectedIndex]));

            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    "Tour selection. Up and Down arrows to browse tours. Enter to start. Escape to go back.",
                    true);
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    private void AnnounceCurrentTour()
    {
        var tour = _tours[_selectedIndex];
        _context.Speech.Speak($"{tour.Name}. {tour.Description}", true);
    }

    public void OnExit() { }

    public void OnResume()
    {
        if (_tours.Count > 0)
        {
            var tour = _tours[_selectedIndex];
            _context.Speech.Speak($"Tour selection. {tour.Name}.", true);
        }
    }
}
