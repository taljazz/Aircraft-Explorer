using AircraftExplorer.Aircraft;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Modes;

public class ExteriorWalkAroundMode : NavigationModeBase
{
    public override string ModeName => "Exterior Walk-Around";
    protected override bool IsExteriorFilter => true;
    protected override string GetResumePrefix() => "Walk-around mode";

    protected override INavigationSpace CreateNavigator(AircraftModel aircraft)
        => new WalkAroundNavigator(aircraft);

    protected override Coordinate3D GetStartPosition(AircraftModel aircraft)
    {
        var walkNav = (WalkAroundNavigator)Navigator;
        return new Coordinate3D(
            aircraft.EntryCoordinate.X,
            aircraft.EntryCoordinate.Y,
            walkNav.GroundZ);
    }

    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        var aircraft = context.SelectedAircraft!;

        var zone = Navigator.GetZoneAt(Position);
        var zoneName = zone?.Name ?? "Aircraft exterior";

        context.Speech.Speak(
            $"Walk-around mode. Ground level at {zoneName}. " +
            $"Use arrow keys to walk. Page Up and Page Down to move vertically. T to switch to grid view.",
            true);

        context.SpatialAudio.UpdateListenerPosition(Position);
        context.SpatialAudio.PlayMovementTone(Position, aircraft.GridBounds);
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
                return GatherAndShowTopics(3.0);

            case InputAction.ToggleExteriorMode:
                Context.SpatialAudio.StopComponentBeacon();
                return ModeResult.SwitchTo(new ExteriorGridMode());

            case InputAction.JumpToComponent:
                return ShowJumpList();

            case InputAction.Help:
                Context.Speech.Speak(
                    "Walk-around mode. Arrow keys to walk. " +
                    "Page Up and Page Down to move vertically. " +
                    "T to switch to grid view. C to announce position. " +
                    "I for information. J to jump to a component. Escape to return to interior.",
                    true);
                return ModeResult.Stay;

            case InputAction.Back:
                Context.SpatialAudio.StopComponentBeacon();
                return ModeResult.SwitchTo(new InteriorExplorationMode());

            default:
                return ModeResult.Stay;
        }
    }
}
