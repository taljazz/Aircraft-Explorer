namespace AircraftExplorer.Input;

public interface IInputProvider
{
    bool IsAvailable { get; }
    InputAction? Poll();
    AxisState? GetAxisState();
    Dictionary<string, int>? GetRawAxes() => null;
}
