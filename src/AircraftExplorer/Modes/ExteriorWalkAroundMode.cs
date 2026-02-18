using AircraftExplorer.Education;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Modes;

public class ExteriorWalkAroundMode : IAppMode
{
    private WalkAroundNavigator _navigator = null!;
    private ModeContext _context = null!;
    private Coordinate3D _position;

    public string ModeName => "Exterior Walk-Around";

    public void OnEnter(ModeContext context)
    {
        _context = context;
        var aircraft = context.SelectedAircraft
            ?? throw new InvalidOperationException("No aircraft selected.");

        _navigator = new WalkAroundNavigator(aircraft);

        // Start at entry position projected to ground level
        _position = new Coordinate3D(
            aircraft.EntryCoordinate.X,
            aircraft.EntryCoordinate.Y,
            _navigator.GroundZ);
        context.CurrentPosition = _position;

        var zone = _navigator.GetZoneAt(_position);
        var zoneName = zone?.Name ?? "Aircraft exterior";

        context.Speech.Speak(
            $"Walk-around mode. Ground level at {zoneName}. " +
            $"Use arrow keys to walk. Page Up and Page Down to move vertically. T to switch to grid view.",
            true);

        context.SpatialAudio.UpdateListenerPosition(_position);
        context.SpatialAudio.PlayMovementTone(_position, aircraft.GridBounds);

        var nearby = _navigator.GetNearbyComponents(_position);
        if (nearby.Count > 0)
        {
            double distance = _position.DistanceTo(nearby[0].Coordinate);
            if (distance >= 1.0)
                context.SpatialAudio.StartComponentBeacon(nearby[0].Coordinate, distance);
        }
    }

    public ModeResult HandleInput(InputAction action)
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
                return ShowInfo();

            case InputAction.ToggleExteriorMode:
                _context.SpatialAudio.StopComponentBeacon();
                return ModeResult.SwitchTo(new ExteriorGridMode());

            case InputAction.JumpToComponent:
                return ShowJumpList();

            case InputAction.Help:
                _context.Speech.Speak(
                    "Walk-around mode. Arrow keys to walk. " +
                    "Page Up and Page Down to move vertically. " +
                    "T to switch to grid view. C to announce position. " +
                    "I for information. J to jump to a component. Escape to return to interior.",
                    true);
                return ModeResult.Stay;

            case InputAction.Back:
                _context.SpatialAudio.StopComponentBeacon();
                return ModeResult.SwitchTo(new InteriorExplorationMode());

            default:
                return ModeResult.Stay;
        }
    }

    private ModeResult HandleMovement(int dx, int dy, int dz)
    {
        var result = _navigator.TryMove(_position, dx, dy, dz);
        var aircraft = _context.SelectedAircraft!;

        if (!result.Success)
        {
            _context.Speech.Speak(result.BoundaryMessage ?? "Can't walk there.", true);
            _context.SpatialAudio.PlayBoundaryTone();
            return ModeResult.Stay;
        }

        var previousPosition = _position;
        _position = result.NewPosition;
        _context.CurrentPosition = _position;

        // Move the OpenAL listener, then play directional tone
        _context.SpatialAudio.UpdateListenerPosition(_position);
        _context.SpatialAudio.PlayMovementTone(_position, aircraft.GridBounds, dx, dy, dz);

        if (result.ZoneChanged)
        {
            bool ascending = _position.Y > previousPosition.Y || _position.Z > previousPosition.Z;
            _context.SpatialAudio.PlayZoneTransitionTone(ascending);
        }

        // Component beacon — triple tone on arrival, pulsing when approaching
        if (result.NearbyComponents.Count > 0)
        {
            double distance = _position.DistanceTo(result.NearbyComponents[0].Coordinate);
            if (distance < 1.0)
                _context.SpatialAudio.PlayComponentArrivedTone();
            else
                _context.SpatialAudio.StartComponentBeacon(
                    result.NearbyComponents[0].Coordinate, distance);
        }
        else
            _context.SpatialAudio.StopComponentBeacon();

        var announcement = NavigationAnnouncer.BuildMovementAnnouncement(
            dx, dy, dz, result, _position, aircraft);
        _context.Speech.Speak(announcement, true);
        return ModeResult.Stay;
    }

    private void AnnounceCurrentPosition()
    {
        var zone = _navigator.GetZoneAt(_position);
        var nearby = _navigator.GetNearbyComponents(_position);
        var aircraft = _context.SelectedAircraft!;

        var announcement = NavigationAnnouncer.BuildFullContextAnnouncement(
            _position, aircraft, zone, nearby);
        _context.Speech.Speak(announcement, true);

        _context.SpatialAudio.PlayMovementTone(_position, aircraft.GridBounds);
    }

    private ModeResult ShowInfo()
    {
        var nearby = _navigator.GetNearbyComponents(_position, 3.0);

        if (nearby.Count == 0)
        {
            _context.Speech.Speak("No components nearby.", true);
            return ModeResult.Stay;
        }

        var aircraftId = _context.SelectedAircraft!.Id;
        var topics = new List<EducationTopic>();
        var seen = new HashSet<string>();
        foreach (var component in nearby)
        {
            foreach (var topic in _context.EducationProvider.GetTopicsForComponent(component.Id))
            {
                if (seen.Add(topic.Id) && (topic.AircraftIds.Count == 0 || topic.AircraftIds.Contains(aircraftId)))
                    topics.Add(topic);
            }
        }

        if (topics.Count == 0)
        {
            _context.Speech.Speak($"Near {nearby[0].Name}. {nearby[0].Description}.", true);
            return ModeResult.Stay;
        }

        return ModeResult.PushMode(new InfoViewMode(topics));
    }

    private ModeResult ShowJumpList()
    {
        var aircraft = _context.SelectedAircraft;
        if (aircraft is null)
        {
            _context.Speech.Speak("No components available.", true);
            return ModeResult.Stay;
        }

        var exteriorZones = aircraft.Zones.Where(z => z.IsExterior).ToList();
        var components = aircraft.Components
            .Where(c => exteriorZones.Any(z => z.Contains(c.Coordinate)))
            .ToList();

        if (components.Count == 0)
        {
            _context.Speech.Speak("No exterior components available.", true);
            return ModeResult.Stay;
        }

        _context.SpatialAudio.StopComponentBeacon();
        return ModeResult.PushMode(new ComponentListMode(components));
    }

    public void OnExit()
    {
        _context.SpatialAudio.StopComponentBeacon();
    }

    public void OnResume()
    {
        if (_context.JumpTarget is { } target)
        {
            _context.JumpTarget = null;
            _position = target;
            _context.CurrentPosition = _position;

            var aircraft = _context.SelectedAircraft!;
            _context.SpatialAudio.UpdateListenerPosition(_position);
            _context.SpatialAudio.PlayComponentArrivedTone();

            var zone = _navigator.GetZoneAt(_position);
            var nearby = _navigator.GetNearbyComponents(_position);
            var announcement = NavigationAnnouncer.BuildFullContextAnnouncement(
                _position, aircraft, zone, nearby);
            _context.Speech.Speak($"Walk-around mode. {announcement}", true);
            return;
        }

        var currentZone = _navigator.GetZoneAt(_position);
        _context.Speech.Speak($"Walk-around mode. {currentZone?.Name ?? "Unknown area"}.", true);

        if (_context.SelectedAircraft is not null)
            _context.SpatialAudio.PlayMovementTone(_position, _context.SelectedAircraft.GridBounds);
    }
}
