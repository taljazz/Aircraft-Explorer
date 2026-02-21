using AircraftExplorer.Aircraft;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Modes;

public class InteriorExplorationMode : NavigationModeBase
{
    public override string ModeName => "Interior Exploration";
    protected override bool IsExteriorFilter => false;
    protected override string GetResumePrefix() => "Interior exploration";

    protected override INavigationSpace CreateNavigator(AircraftModel aircraft)
        => new NavigationGrid(aircraft);

    protected override Coordinate3D GetStartPosition(AircraftModel aircraft)
        => aircraft.EntryCoordinate;

    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        var aircraft = context.SelectedAircraft!;

        var zone = Navigator.GetZoneAt(Position);
        var zoneDesc = zone is not null ? zone.Description : "Unknown area";

        context.Speech.Speak(
            $"Entering {aircraft.Name}. {zoneDesc}. Use arrow keys to explore.",
            true);

        context.SpatialAudio.UpdateListenerPosition(Position);
        context.SpatialAudio.PlayMovementTone(Position, aircraft.GridBounds);
        if (context.Settings.Navigation.AnnounceNearbyComponents)
            StartBeaconIfNearComponent();
    }

    public override ModeResult HandleInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.MoveForward:
                return HandleMovement(0, -1, 0);
            case InputAction.MoveBackward:
                return HandleMovement(0, 1, 0);
            case InputAction.MoveLeft:
                return HandleMovement(-1, 0, 0);
            case InputAction.MoveRight:
                return HandleMovement(1, 0, 0);
            case InputAction.MoveUp:
                return HandleMovement(0, 0, 1);
            case InputAction.MoveDown:
                return HandleMovement(0, 0, -1);

            case InputAction.AnnouncePosition:
                AnnounceCurrentPosition();
                return ModeResult.Stay;

            case InputAction.Info:
                return GatherAndShowTopics();

            case InputAction.Select:
                return HandleSelect();

            case InputAction.ToggleExteriorMode:
                Context.SpatialAudio.StopComponentBeacon();
                return ModeResult.SwitchTo(new ExteriorGridMode());

            case InputAction.JumpToComponent:
                return ShowJumpList();

            case InputAction.Quiz:
                return GatherQuizQuestions();

            case InputAction.Help:
                Context.Speech.Speak(
                    "Interior exploration. Arrow keys to move. C to announce position. " +
                    "I for information. K for quiz. J to jump to a component. Enter at cockpit seat for flight controls. " +
                    "T for exterior view. Escape to go back.",
                    true);
                return ModeResult.Stay;

            case InputAction.Back:
                Context.SpatialAudio.StopComponentBeacon();
                return ModeResult.Pop;

            default:
                return ModeResult.Stay;
        }
    }

    private static readonly HashSet<string> FlightControlComponentIds =
        ["captain-yoke", "first-officer-yoke", "throttle-quadrant"];

    private ModeResult HandleSelect()
    {
        var aircraft = Context.SelectedAircraft;
        if (aircraft is null)
            return ModeResult.Stay;

        // Check if standing on a yoke or throttle quadrant
        var nearby = Navigator.GetNearbyComponents(Position, 0.5);
        if (nearby.Any(c => FlightControlComponentIds.Contains(c.Id)))
        {
            Context.SpatialAudio.StopComponentBeacon();
            return ModeResult.PushMode(new FlightControlMode());
        }

        // Otherwise, interact with nearest component
        if (nearby.Count > 0)
        {
            var component = nearby[0];
            if (component.InteractionSteps.Count > 0)
            {
                Context.SpatialAudio.StopComponentBeacon();
                return ModeResult.PushMode(new ComponentInteractionMode(component));
            }
            else if (!string.IsNullOrEmpty(component.InteractionText))
            {
                Context.Speech.Speak(component.InteractionText, true);
            }
            else
            {
                Context.Speech.Speak(component.Description, true);
            }
        }
        else
        {
            Context.Speech.Speak("Nothing to interact with here.", true);
        }

        return ModeResult.Stay;
    }
}
