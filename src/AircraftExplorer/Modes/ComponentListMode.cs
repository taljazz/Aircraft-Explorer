using AircraftExplorer.Aircraft;
using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class ComponentListMode : IAppMode
{
    private readonly IReadOnlyList<Component> _components;
    private int _selectedIndex;
    private ModeContext _context = null!;

    public string ModeName => "Jump to Component";

    public ComponentListMode(IReadOnlyList<Component> components)
    {
        _components = components;
    }

    public void OnEnter(ModeContext context)
    {
        _context = context;
        _selectedIndex = 0;

        if (_components.Count == 0)
        {
            context.Speech.Speak("No components on this aircraft. Press Escape to return.", true);
            return;
        }

        var comp = _components[_selectedIndex];
        context.Speech.Speak(
            $"Jump to component. {_components.Count} components. " +
            $"1 of {_components.Count}: {comp.Category} -- {comp.Name}. " +
            $"Enter to jump, Escape to cancel.",
            true);
    }

    public ModeResult HandleInput(InputAction action)
    {
        if (_components.Count == 0 && action == InputAction.Back)
            return ModeResult.Pop;

        switch (action)
        {
            case InputAction.MoveForward:
            case InputAction.MenuUp:
            case InputAction.MoveUp:
                if (_components.Count == 0) return ModeResult.Stay;
                _selectedIndex = (_selectedIndex - 1 + _components.Count) % _components.Count;
                AnnounceCurrent();
                return ModeResult.Stay;

            case InputAction.MoveBackward:
            case InputAction.MenuDown:
            case InputAction.MoveDown:
                if (_components.Count == 0) return ModeResult.Stay;
                _selectedIndex = (_selectedIndex + 1) % _components.Count;
                AnnounceCurrent();
                return ModeResult.Stay;

            case InputAction.Select:
                if (_components.Count == 0) return ModeResult.Stay;
                var comp = _components[_selectedIndex];
                _context.JumpTarget = comp.Coordinate;
                _context.Speech.Speak($"Jumping to {comp.Name}.", true);
                return ModeResult.Pop;

            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    "Jump to component list. Up and Down to browse. Enter to jump to the selected component. Escape to cancel.",
                    true);
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    private void AnnounceCurrent()
    {
        var comp = _components[_selectedIndex];
        _context.Speech.Speak(
            $"{_selectedIndex + 1} of {_components.Count}: {comp.Category} -- {comp.Name}.",
            true);
    }

    public void OnExit() { }

    public void OnResume() { }
}
