using AircraftExplorer.Audio;
using AircraftExplorer.Input;
using AircraftExplorer.Modes;

namespace AircraftExplorer;

public class AppHost
{
    private readonly ISpeechService _speech;
    private readonly KeyboardInputProvider _keyboard;
    private readonly InputManager _inputManager;
    private readonly AppModeManager _modeManager;
    private readonly ModeContext _context;

    public AppHost(
        ISpeechService speech,
        KeyboardInputProvider keyboard,
        InputManager inputManager,
        AppModeManager modeManager,
        ModeContext context)
    {
        _speech = speech;
        _keyboard = keyboard;
        _inputManager = inputManager;
        _modeManager = modeManager;
        _context = context;
    }

    public void Run()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        var form = new MainForm(_speech, _keyboard, _inputManager, _modeManager, _context);
        Application.Run(form);
    }
}
