using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class MainMenuMode : IAppMode
{
    private readonly string[] _menuItems = ["Explore Aircraft", "Guided Tours", "Settings", "Quit"];
    private int _selectedIndex;
    private ModeContext _context = null!;

    public string ModeName => "Main Menu";

    public void OnEnter(ModeContext context)
    {
        _context = context;
        _selectedIndex = 0;
        context.Speech.Speak("Aircraft Explorer. Main Menu. Use arrow keys to navigate, Enter to select.", true);
    }

    public ModeResult HandleInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.MenuUp:
            case InputAction.MoveUp:
            case InputAction.MoveForward:
                _selectedIndex = (_selectedIndex - 1 + _menuItems.Length) % _menuItems.Length;
                _context.Speech.Speak(_menuItems[_selectedIndex], true);
                return ModeResult.Stay;

            case InputAction.MenuDown:
            case InputAction.MoveDown:
            case InputAction.MoveBackward:
                _selectedIndex = (_selectedIndex + 1) % _menuItems.Length;
                _context.Speech.Speak(_menuItems[_selectedIndex], true);
                return ModeResult.Stay;

            case InputAction.Select:
                return HandleSelect();

            case InputAction.Help:
                _context.Speech.Speak("Main menu. Up and Down arrows to browse. Enter to select.", true);
                return ModeResult.Stay;

            case InputAction.Quit:
                _context.Speech.Speak("Goodbye.", true);
                return ModeResult.Quit;

            default:
                return ModeResult.Stay;
        }
    }

    private ModeResult HandleSelect()
    {
        return _selectedIndex switch
        {
            0 => ModeResult.SwitchTo(new AircraftSelectMode()),
            1 => ModeResult.PushMode(new TourAircraftSelectMode()),
            2 => ModeResult.PushMode(new SettingsMode()),
            3 => ConfirmQuit(),
            _ => ModeResult.Stay
        };
    }

    private ModeResult ConfirmQuit()
    {
        _context.Speech.Speak("Exiting Aircraft Explorer.", true);
        return ModeResult.Quit;
    }

    public void OnExit() { }

    public void OnResume()
    {
        _context.Speech.Speak($"Main Menu. {_menuItems[_selectedIndex]}.", true);
    }
}
