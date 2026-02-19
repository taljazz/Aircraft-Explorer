using AircraftExplorer.Aircraft;
using AircraftExplorer.Audio;
using AircraftExplorer.Config;
using AircraftExplorer.Education;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Modes;

public class ModeContext
{
    public required ISpeechService Speech { get; init; }
    public required ISpatialAudioService SpatialAudio { get; init; }
    public required AircraftRegistry AircraftRegistry { get; init; }
    public required IEducationProvider EducationProvider { get; init; }
    public required AppSettings Settings { get; init; }
    public required InputManager InputManager { get; init; }
    public required string SettingsFilePath { get; init; }

    // Set when an aircraft is selected
    public AircraftModel? SelectedAircraft { get; set; }
    public Coordinate3D CurrentPosition { get; set; }

    /// <summary>Set by ComponentListMode to request a jump. Cleared by the navigation mode on resume.</summary>
    public Coordinate3D? JumpTarget { get; set; }
}
