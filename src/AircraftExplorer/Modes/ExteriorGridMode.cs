using AircraftExplorer.Aircraft;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Modes;

public class ExteriorGridMode : NavigationModeBase
{
    public override string ModeName => "Exterior Grid";
    protected override bool IsExteriorFilter => true;
    protected override string GetResumePrefix() => "Exterior grid view";

    protected override INavigationSpace CreateNavigator(AircraftModel aircraft)
        => new GridNavigator(aircraft);

    protected override Coordinate3D GetStartPosition(AircraftModel aircraft)
    {
        var gridNav = (GridNavigator)Navigator;
        var zones = gridNav.ExteriorZones;

        if (zones.Count > 0)
        {
            var firstZone = zones[0];
            return new Coordinate3D(
                (firstZone.MinBound[0] + firstZone.MaxBound[0]) / 2,
                (firstZone.MinBound[1] + firstZone.MaxBound[1]) / 2,
                (firstZone.MinBound[2] + firstZone.MaxBound[2]) / 2);
        }

        return aircraft.EntryCoordinate;
    }

    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        var aircraft = context.SelectedAircraft!;

        var gridNav = (GridNavigator)Navigator;
        var zones = gridNav.ExteriorZones;

        if (zones.Count > 0)
        {
            var nearby = Navigator.GetNearbyComponents(Position);
            var description = NavigationAnnouncer.BuildFullContextAnnouncement(
                Position, aircraft, zones[0], nearby);
            context.Speech.Speak($"Exterior view. {description} Use arrows to navigate sections. T to walk around.", true);
            context.SpatialAudio.UpdateListenerPosition(Position);
            context.SpatialAudio.PlayMovementTone(Position, aircraft.GridBounds);
            StartBeaconIfNearComponent();
        }
        else
        {
            context.Speech.Speak("Exterior view. No exterior zones defined.", true);
        }
    }

    public override ModeResult HandleInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.MoveForward:
            case InputAction.MoveRight:
            case InputAction.MenuDown:
                return HandleMovement(0, -1, 0);

            case InputAction.MoveBackward:
            case InputAction.MoveLeft:
            case InputAction.MenuUp:
                return HandleMovement(0, 1, 0);

            case InputAction.AnnouncePosition:
                AnnounceCurrentPosition();
                return ModeResult.Stay;

            case InputAction.Info:
                return GatherAndShowTopics(5.0);

            case InputAction.ToggleExteriorMode:
                Context.SpatialAudio.StopComponentBeacon();
                return ModeResult.SwitchTo(new ExteriorWalkAroundMode());

            case InputAction.JumpToComponent:
                return ShowJumpList();

            case InputAction.Help:
                Context.Speech.Speak(
                    "Exterior grid view. Arrows to move between sections. " +
                    "T to switch to walk-around mode. C to announce position. " +
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
