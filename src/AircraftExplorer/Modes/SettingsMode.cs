using AircraftExplorer.Config;
using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class SettingsMode : IAppMode
{
    private readonly string[] _settingNames =
    [
        "Step Size",
        "Verbosity",
        "Announce Zone Changes",
        "Announce Nearby Components"
    ];

    private int _selectedIndex;
    private ModeContext _context = null!;
    private AppSettings _settings = null!;

    public string ModeName => "Settings";

    public void OnEnter(ModeContext context)
    {
        _context = context;
        _settings = context.Settings;
        _selectedIndex = 0;

        context.Speech.Speak(
            "Settings. Use Up and Down to navigate. Left and Right to change values. Escape to go back. " +
            GetCurrentSettingAnnouncement(),
            true);
    }

    public ModeResult HandleInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.MenuUp:
            case InputAction.MoveUp:
            case InputAction.MoveForward:
                _selectedIndex = (_selectedIndex - 1 + _settingNames.Length) % _settingNames.Length;
                _context.Speech.Speak(GetCurrentSettingAnnouncement(), true);
                return ModeResult.Stay;

            case InputAction.MenuDown:
            case InputAction.MoveDown:
            case InputAction.MoveBackward:
                _selectedIndex = (_selectedIndex + 1) % _settingNames.Length;
                _context.Speech.Speak(GetCurrentSettingAnnouncement(), true);
                return ModeResult.Stay;

            case InputAction.MoveLeft:
                AdjustSetting(-1);
                return ModeResult.Stay;

            case InputAction.MoveRight:
                AdjustSetting(1);
                return ModeResult.Stay;

            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    "Settings. Up and Down to browse settings. Left and Right to change values. Escape to return.",
                    true);
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    private void AdjustSetting(int direction)
    {
        switch (_selectedIndex)
        {
            case 0: // Step Size (1-3)
                _settings.Navigation.StepSize = Math.Clamp(_settings.Navigation.StepSize + direction, 1, 3);
                break;
            case 1: // Verbosity (1-3)
                _settings.Speech.VerbosityLevel = Math.Clamp(_settings.Speech.VerbosityLevel + direction, 1, 3);
                break;
            case 2: // Announce Zone Changes
                _settings.Navigation.AnnounceZoneChanges = !_settings.Navigation.AnnounceZoneChanges;
                break;
            case 3: // Announce Nearby Components
                _settings.Navigation.AnnounceNearbyComponents = !_settings.Navigation.AnnounceNearbyComponents;
                break;
        }

        _context.Speech.Speak(GetCurrentSettingAnnouncement(), true);
    }

    private string GetCurrentSettingAnnouncement()
    {
        var value = _selectedIndex switch
        {
            0 => _settings.Navigation.StepSize.ToString(),
            1 => _settings.Speech.VerbosityLevel switch
            {
                1 => "1, Minimal",
                2 => "2, Normal",
                3 => "3, Detailed",
                _ => _settings.Speech.VerbosityLevel.ToString()
            },
            2 => _settings.Navigation.AnnounceZoneChanges ? "On" : "Off",
            3 => _settings.Navigation.AnnounceNearbyComponents ? "On" : "Off",
            _ => "Unknown"
        };

        return $"{_settingNames[_selectedIndex]}: {value}.";
    }

    public void OnExit()
    {
        _context.Speech.Speak("Settings saved.", true);
    }

    public void OnResume() { }
}
