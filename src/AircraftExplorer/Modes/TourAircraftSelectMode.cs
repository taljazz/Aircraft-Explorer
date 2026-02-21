using AircraftExplorer.Aircraft;
using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class TourAircraftSelectMode : IAppMode
{
    private IReadOnlyList<AircraftModel> _aircraftList = [];
    private int _selectedIndex;
    private ModeContext _context = null!;

    public string ModeName => "Tour Aircraft Select";

    public void OnEnter(ModeContext context)
    {
        _context = context;
        _aircraftList = context.AircraftRegistry.GetAll();
        _selectedIndex = 0;

        if (_aircraftList.Count == 0)
        {
            context.Speech.Speak("No aircraft available. Press Escape to return.", true);
            return;
        }

        var aircraft = _aircraftList[_selectedIndex];
        context.Speech.Speak(
            $"Guided Tours. First, select an aircraft. {_aircraftList.Count} available. " +
            $"Use arrows to browse. {aircraft.Name}, {aircraft.Variant}.",
            true);
    }

    public ModeResult HandleInput(InputAction action)
    {
        if (_aircraftList.Count == 0 && action == InputAction.Back)
            return ModeResult.Pop;

        switch (action)
        {
            case InputAction.MenuUp:
            case InputAction.MoveUp:
            case InputAction.MoveForward:
                if (_aircraftList.Count == 0) return ModeResult.Stay;
                _selectedIndex = (_selectedIndex - 1 + _aircraftList.Count) % _aircraftList.Count;
                AnnounceCurrentAircraft();
                return ModeResult.Stay;

            case InputAction.MenuDown:
            case InputAction.MoveDown:
            case InputAction.MoveBackward:
                if (_aircraftList.Count == 0) return ModeResult.Stay;
                _selectedIndex = (_selectedIndex + 1) % _aircraftList.Count;
                AnnounceCurrentAircraft();
                return ModeResult.Stay;

            case InputAction.Select:
                if (_aircraftList.Count == 0) return ModeResult.Stay;
                _context.SelectedAircraft = _aircraftList[_selectedIndex];
                return ModeResult.PushMode(new TourSelectMode());

            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    "Select an aircraft for the guided tour. Up and Down arrows to browse. Enter to select. Escape to go back.",
                    true);
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    private void AnnounceCurrentAircraft()
    {
        var aircraft = _aircraftList[_selectedIndex];
        _context.Speech.Speak($"{aircraft.Name}, {aircraft.Variant}.", true);
    }

    public void OnExit() { }

    public void OnResume()
    {
        if (_aircraftList.Count > 0)
        {
            var aircraft = _aircraftList[_selectedIndex];
            _context.Speech.Speak($"Tour aircraft selection. {aircraft.Name}, {aircraft.Variant}.", true);
        }
    }
}
