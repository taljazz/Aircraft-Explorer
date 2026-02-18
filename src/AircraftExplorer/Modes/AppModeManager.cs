namespace AircraftExplorer.Modes;

public class AppModeManager
{
    private readonly Stack<IAppMode> _modeStack = new();

    public IAppMode? CurrentMode => _modeStack.Count > 0 ? _modeStack.Peek() : null;

    public bool HasModes => _modeStack.Count > 0;

    public void Push(IAppMode mode, ModeContext context)
    {
        _modeStack.Push(mode);
        mode.OnEnter(context);
    }

    public void Pop()
    {
        if (_modeStack.Count == 0)
            return;

        var exiting = _modeStack.Pop();
        exiting.OnExit();

        if (_modeStack.Count > 0)
            _modeStack.Peek().OnResume();
    }

    public void Switch(IAppMode mode, ModeContext context)
    {
        if (_modeStack.Count > 0)
        {
            var exiting = _modeStack.Pop();
            exiting.OnExit();
        }

        _modeStack.Push(mode);
        mode.OnEnter(context);
    }

    public void ProcessResult(ModeResult result, ModeContext context)
    {
        switch (result.Transition)
        {
            case ModeTransition.None:
                break;

            case ModeTransition.Push:
                if (result.NextMode is not null)
                    Push(result.NextMode, context);
                break;

            case ModeTransition.Pop:
                Pop();
                break;

            case ModeTransition.Switch:
                if (result.NextMode is not null)
                    Switch(result.NextMode, context);
                break;

            case ModeTransition.Quit:
                // Quit is handled by the AppHost
                break;
        }
    }
}
