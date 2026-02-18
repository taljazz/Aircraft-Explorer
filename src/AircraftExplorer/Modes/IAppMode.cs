using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public interface IAppMode
{
    string ModeName { get; }
    void OnEnter(ModeContext context);
    ModeResult HandleInput(InputAction action);
    void OnExit();
    void OnResume();

    /// <summary>Called every frame (~60Hz). Override for continuous input like flight axes.</summary>
    void OnTick() { }
}
