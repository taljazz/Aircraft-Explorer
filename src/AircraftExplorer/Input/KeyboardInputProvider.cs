using System.Collections.Concurrent;

namespace AircraftExplorer.Input;

public sealed class KeyboardInputProvider : IInputProvider
{
    private readonly ConcurrentQueue<InputAction> _actionQueue = new();

    public bool IsAvailable => true;

    public void HandleKeyDown(Keys key)
    {
        var action = MapKey(key);
        if (action is not null)
            _actionQueue.Enqueue(action.Value);
    }

    public InputAction? Poll()
    {
        return _actionQueue.TryDequeue(out var action) ? action : null;
    }

    public AxisState? GetAxisState() => null;

    private static InputAction? MapKey(Keys key) => key switch
    {
        Keys.Up => InputAction.MoveForward,
        Keys.Down => InputAction.MoveBackward,
        Keys.Left => InputAction.MoveLeft,
        Keys.Right => InputAction.MoveRight,
        Keys.PageUp => InputAction.MoveUp,
        Keys.PageDown => InputAction.MoveDown,
        Keys.Enter => InputAction.Select,
        Keys.Escape => InputAction.Back,
        Keys.I => InputAction.Info,
        Keys.T => InputAction.ToggleExteriorMode,
        Keys.C => InputAction.AnnouncePosition,
        Keys.H => InputAction.Help,
        Keys.F1 => InputAction.Help,
        Keys.Q => InputAction.Quit,
        Keys.OemMinus => InputAction.VolumeDown,
        Keys.Oemplus => InputAction.VolumeUp,
        Keys.R => InputAction.ReadTopic,
        Keys.J => InputAction.JumpToComponent,
        _ => null
    };
}
