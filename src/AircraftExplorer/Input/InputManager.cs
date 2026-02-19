namespace AircraftExplorer.Input;

public sealed class InputManager
{
    private readonly IInputProvider[] _providers;

    public InputManager(IEnumerable<IInputProvider> providers)
    {
        _providers = providers.ToArray();
    }

    public InputAction? Poll()
    {
        foreach (var provider in _providers)
        {
            if (!provider.IsAvailable)
                continue;

            var action = provider.Poll();
            if (action is not null)
                return action;
        }

        return null;
    }

    public AxisState? GetAxisState()
    {
        foreach (var provider in _providers)
        {
            if (!provider.IsAvailable)
                continue;

            var state = provider.GetAxisState();
            if (state is not null)
                return state;
        }

        return null;
    }

    public Dictionary<string, int>? GetRawAxes()
    {
        foreach (var provider in _providers)
        {
            if (!provider.IsAvailable)
                continue;

            var axes = provider.GetRawAxes();
            if (axes is not null)
                return axes;
        }

        return null;
    }
}
