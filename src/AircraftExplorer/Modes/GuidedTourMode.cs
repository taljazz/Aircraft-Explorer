using AircraftExplorer.Aircraft;
using AircraftExplorer.Education;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;
using AircraftExplorer.Tours;

namespace AircraftExplorer.Modes;

public class GuidedTourMode : NavigationModeBase
{
    private readonly TourDefinition _tour;
    private List<ResolvedStop> _stops = [];
    private int _currentStopIndex;
    private bool _awaitingContinue;
    private string _lastTourMessage = "";

    public override string ModeName => "Guided Tour";
    protected override bool IsExteriorFilter => _tour.IsExterior;
    protected override string GetResumePrefix() => $"Guided tour: {_tour.Name}";

    public GuidedTourMode(TourDefinition tour)
    {
        _tour = tour;
    }

    protected override INavigationSpace CreateNavigator(AircraftModel aircraft)
    {
        return _tour.IsExterior
            ? new WalkAroundNavigator(aircraft)
            : new NavigationGrid(aircraft);
    }

    protected override Coordinate3D GetStartPosition(AircraftModel aircraft)
    {
        if (_tour.IsExterior)
        {
            var walkNav = (WalkAroundNavigator)Navigator;
            return new Coordinate3D(
                aircraft.EntryCoordinate.X,
                aircraft.EntryCoordinate.Y,
                walkNav.GroundZ);
        }

        return aircraft.EntryCoordinate;
    }

    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        var aircraft = context.SelectedAircraft!;

        // Resolve tour stops — find components by ID, skip missing ones
        var componentMap = aircraft.Components.ToDictionary(c => c.Id);
        _stops = _tour.Stops
            .Where(s => componentMap.ContainsKey(s.ComponentId))
            .Select(s => new ResolvedStop(s, componentMap[s.ComponentId]))
            .ToList();

        if (_stops.Count == 0)
        {
            context.Speech.Speak("This tour has no valid stops for this aircraft. Press Escape to go back.", true);
            return;
        }

        _currentStopIndex = 0;
        _awaitingContinue = false;

        var firstStop = _stops[0];
        SpeakTourMessage(
            $"Starting tour: {_tour.Name}. {_tour.Description} " +
            $"{_stops.Count} stops. First stop: {firstStop.Component.Name}. {firstStop.Stop.Narration}",
            true);

        context.SpatialAudio.UpdateListenerPosition(Position);
        StartBeaconToCurrentStop();
    }

    public override ModeResult HandleInput(InputAction action)
    {
        if (_stops.Count == 0 && action == InputAction.Back)
            return ModeResult.Pop;

        if (_awaitingContinue)
        {
            if (action == InputAction.Select)
            {
                _currentStopIndex++;
                if (_currentStopIndex >= _stops.Count)
                {
                    SpeakTourMessage(
                        $"Tour complete! You have finished the {_tour.Name}.",
                        true);
                    return ModeResult.Pop;
                }

                _awaitingContinue = false;
                var nextStop = _stops[_currentStopIndex];
                SpeakTourMessage(
                    $"Next stop: {nextStop.Component.Name}. {nextStop.Stop.Narration}",
                    true);
                StartBeaconToCurrentStop();
                return ModeResult.Stay;
            }

            if (action == InputAction.Back)
                return ModeResult.Pop;

            if (action == InputAction.Info)
                return GatherAndShowTopics(GetInfoRadius());

            if (action == InputAction.AnnouncePosition)
            {
                AnnouncePositionWithTourProgress();
                return ModeResult.Stay;
            }

            if (action == InputAction.AnnounceObjective)
            {
                AnnounceObjectiveDirection();
                return ModeResult.Stay;
            }

            if (action == InputAction.RepeatTourMessage)
            {
                RepeatLastTourMessage();
                return ModeResult.Stay;
            }

            if (action == InputAction.ReadTopic)
            {
                OpenTourMessageReader();
                return ModeResult.Stay;
            }

            if (action == InputAction.Help)
            {
                AnnounceHelp();
                return ModeResult.Stay;
            }

            // Still allow movement while awaiting continue
            return HandleNavigationAction(action);
        }

        switch (action)
        {
            case InputAction.AnnouncePosition:
                AnnouncePositionWithTourProgress();
                return ModeResult.Stay;

            case InputAction.AnnounceObjective:
                AnnounceObjectiveDirection();
                return ModeResult.Stay;

            case InputAction.RepeatTourMessage:
                RepeatLastTourMessage();
                return ModeResult.Stay;

            case InputAction.ReadTopic:
                OpenTourMessageReader();
                return ModeResult.Stay;

            case InputAction.Info:
                return GatherAndShowTopics(GetInfoRadius());

            case InputAction.Help:
                AnnounceHelp();
                return ModeResult.Stay;

            case InputAction.Back:
                return ModeResult.Pop;

            default:
                return HandleNavigationAction(action);
        }
    }

    private ModeResult HandleNavigationAction(InputAction action)
    {
        switch (action)
        {
            case InputAction.MoveForward:
                return HandleTourMovement(0, -1, 0);
            case InputAction.MoveBackward:
                return HandleTourMovement(0, 1, 0);
            case InputAction.MoveLeft:
                return HandleTourMovement(-1, 0, 0);
            case InputAction.MoveRight:
                return HandleTourMovement(1, 0, 0);
            case InputAction.MoveUp:
                return HandleTourMovement(0, 0, 1);
            case InputAction.MoveDown:
                return HandleTourMovement(0, 0, -1);
            default:
                return ModeResult.Stay;
        }
    }

    private ModeResult HandleTourMovement(int dx, int dy, int dz)
    {
        var result = HandleMovement(dx, dy, dz);

        // Check if we've arrived at the current tour stop
        if (_currentStopIndex < _stops.Count && !_awaitingContinue)
        {
            var currentTarget = _stops[_currentStopIndex];
            double distance = Position.DistanceTo(currentTarget.Component.Coordinate);

            if (distance < 1.0)
            {
                _awaitingContinue = true;
                Context.SpatialAudio.StopComponentBeacon();
                Context.SpatialAudio.PlayComponentArrivedTone();
                SpeakTourMessage(
                    $"Arrived at {currentTarget.Component.Name}. {currentTarget.Stop.ArrivalNarration} " +
                    $"Stop {_currentStopIndex + 1} of {_stops.Count}. Press Enter to continue.",
                    false);
            }
            else
            {
                // Update beacon toward current stop
                Context.SpatialAudio.StartComponentBeacon(currentTarget.Component.Coordinate, distance);
            }
        }

        return result;
    }

    private void StartBeaconToCurrentStop()
    {
        if (_currentStopIndex >= _stops.Count) return;

        var target = _stops[_currentStopIndex];
        double distance = Position.DistanceTo(target.Component.Coordinate);

        if (distance < 1.0)
        {
            // Already at the stop
            _awaitingContinue = true;
            Context.SpatialAudio.PlayComponentArrivedTone();
            SpeakTourMessage(
                $"You are already at {target.Component.Name}. {target.Stop.ArrivalNarration} " +
                $"Stop {_currentStopIndex + 1} of {_stops.Count}. Press Enter to continue.",
                false);
        }
        else
        {
            Context.SpatialAudio.StartComponentBeacon(target.Component.Coordinate, distance);
        }
    }

    private void AnnouncePositionWithTourProgress()
    {
        AnnounceCurrentPosition();
        if (_currentStopIndex < _stops.Count)
        {
            var target = _stops[_currentStopIndex];
            double distance = Position.DistanceTo(target.Component.Coordinate);
            Context.Speech.Speak(
                $"Tour: {_tour.Name}. Stop {_currentStopIndex + 1} of {_stops.Count}: {target.Component.Name}, {(int)Math.Round(distance)} steps away.",
                false);
        }
    }

    private void AnnounceObjectiveDirection()
    {
        if (_currentStopIndex >= _stops.Count)
        {
            Context.Speech.Speak("Tour complete. No more stops.", true);
            return;
        }

        var target = _stops[_currentStopIndex];
        double distance = Position.DistanceTo(target.Component.Coordinate);

        if (distance < 1.0)
        {
            Context.Speech.Speak(
                $"{target.Component.Name} is within reach. Press Enter to continue.",
                true);
            return;
        }

        var directions = new List<string>();
        int dy = target.Component.Coordinate.Y - Position.Y;
        int dx = target.Component.Coordinate.X - Position.X;
        int dz = target.Component.Coordinate.Z - Position.Z;

        if (dy < 0) directions.Add("forward");
        else if (dy > 0) directions.Add("aft");

        if (dx > 0) directions.Add("starboard");
        else if (dx < 0) directions.Add("port");

        if (dz > 0) directions.Add("above");
        else if (dz < 0) directions.Add("below");

        string directionText = directions.Count > 0
            ? string.Join(" and ", directions)
            : "at your position";

        Context.Speech.Speak(
            $"{target.Component.Name}, {(int)Math.Round(distance)} steps away, {directionText}.",
            true);
    }

    private void SpeakTourMessage(string message, bool interrupt)
    {
        _lastTourMessage = message;
        Context.Speech.Speak(message, interrupt);
    }

    private void RepeatLastTourMessage()
    {
        if (string.IsNullOrEmpty(_lastTourMessage))
        {
            Context.Speech.Speak("No tour message to repeat.", true);
            return;
        }

        Context.Speech.Speak(_lastTourMessage, true);
    }

    private void OpenTourMessageReader()
    {
        if (string.IsNullOrEmpty(_lastTourMessage))
        {
            Context.Speech.Speak("No tour message to read.", true);
            return;
        }

        using var form = new TopicReaderForm("Tour Message", _lastTourMessage);
        form.ShowDialog();
    }

    private void AnnounceHelp()
    {
        Context.Speech.Speak(
            $"Guided tour: {_tour.Name}. Arrow keys to move. " +
            "Page Up and Page Down to move vertically. " +
            "Follow the beacon to each tour stop. " +
            "C to announce position and tour progress. " +
            "Shift C to announce objective direction. " +
            "Shift R to repeat last tour message. R to open it in a text window. " +
            "I for information. Enter to continue after arriving at a stop. Escape to exit tour.",
            true);
    }

    private double GetInfoRadius() => _tour.IsExterior ? 3.0 : 2.0;

    private sealed record ResolvedStop(TourStop Stop, Component Component);
}
