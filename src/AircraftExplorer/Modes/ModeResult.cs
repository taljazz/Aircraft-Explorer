namespace AircraftExplorer.Modes;

public class ModeResult
{
    public ModeTransition Transition { get; init; } = ModeTransition.None;
    public IAppMode? NextMode { get; init; }

    public static ModeResult Stay => new() { Transition = ModeTransition.None };
    public static ModeResult Pop => new() { Transition = ModeTransition.Pop };
    public static ModeResult Quit => new() { Transition = ModeTransition.Quit };

    public static ModeResult PushMode(IAppMode mode) => new()
    {
        Transition = ModeTransition.Push,
        NextMode = mode
    };

    public static ModeResult SwitchTo(IAppMode mode) => new()
    {
        Transition = ModeTransition.Switch,
        NextMode = mode
    };
}

public enum ModeTransition
{
    None,
    Push,
    Pop,
    Switch,
    Quit
}
