using AircraftExplorer.Aircraft;
using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class ComponentInteractionMode : IAppMode
{
    private readonly Component _component;
    private int _currentStep;
    private ModeContext _context = null!;

    public string ModeName => "Component Interaction";

    public ComponentInteractionMode(Component component)
    {
        _component = component;
    }

    public void OnEnter(ModeContext context)
    {
        _context = context;
        _currentStep = 0;
        AnnounceCurrentStep();
    }

    public ModeResult HandleInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.Select:
                _currentStep++;
                if (_currentStep >= _component.InteractionSteps.Count)
                {
                    _context.Speech.Speak("Interaction complete.", true);
                    return ModeResult.Pop;
                }
                AnnounceCurrentStep();
                return ModeResult.Stay;

            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    "Component interaction. Enter to advance to the next step. Escape to exit.",
                    true);
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    private void AnnounceCurrentStep()
    {
        var step = _component.InteractionSteps[_currentStep];
        _context.Speech.Speak(
            $"{_component.Name}. Step {_currentStep + 1} of {_component.InteractionSteps.Count}. {step}",
            true);
    }

    public void OnExit() { }
    public void OnResume() { }
}
