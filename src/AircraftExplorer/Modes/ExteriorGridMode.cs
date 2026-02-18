using AircraftExplorer.Education;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Modes;

public class ExteriorGridMode : IAppMode
{
    private GridNavigator _navigator = null!;
    private ModeContext _context = null!;
    private Coordinate3D _position;

    public string ModeName => "Exterior Grid";

    public void OnEnter(ModeContext context)
    {
        _context = context;
        var aircraft = context.SelectedAircraft
            ?? throw new InvalidOperationException("No aircraft selected.");

        _navigator = new GridNavigator(aircraft);
        var zones = _navigator.ExteriorZones;

        if (zones.Count > 0)
        {
            var firstZone = zones[0];
            _position = new Coordinate3D(
                (firstZone.MinBound[0] + firstZone.MaxBound[0]) / 2,
                (firstZone.MinBound[1] + firstZone.MaxBound[1]) / 2,
                (firstZone.MinBound[2] + firstZone.MaxBound[2]) / 2);

            var nearby = _navigator.GetNearbyComponents(_position);
            var description = NavigationAnnouncer.BuildFullContextAnnouncement(
                _position, aircraft, firstZone, nearby);
            context.Speech.Speak($"Exterior view. {description} Use arrows to navigate sections. T to walk around.", true);
            context.SpatialAudio.UpdateListenerPosition(_position);
            context.SpatialAudio.PlayMovementTone(_position, aircraft.GridBounds);

            if (nearby.Count > 0)
            {
                double distance = _position.DistanceTo(nearby[0].Coordinate);
                if (distance >= 1.0)
                    context.SpatialAudio.StartComponentBeacon(nearby[0].Coordinate, distance);
            }
        }
        else
        {
            context.Speech.Speak("Exterior view. No exterior zones defined.", true);
        }
    }

    public ModeResult HandleInput(InputAction action)
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
                return ShowInfo();

            case InputAction.ToggleExteriorMode:
                _context.SpatialAudio.StopComponentBeacon();
                return ModeResult.SwitchTo(new ExteriorWalkAroundMode());

            case InputAction.JumpToComponent:
                return ShowJumpList();

            case InputAction.Help:
                _context.Speech.Speak(
                    "Exterior grid view. Arrows to move between sections. " +
                    "T to switch to walk-around mode. C to announce position. " +
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
            _context.Speech.Speak(result.BoundaryMessage ?? "Can't move there.", true);
            _context.SpatialAudio.PlayBoundaryTone();
            return ModeResult.Stay;
        }

        var previousPosition = _position;
        _position = result.NewPosition;
        _context.CurrentPosition = _position;

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
    }

    private ModeResult ShowInfo()
    {
        var nearby = _navigator.GetNearbyComponents(_position, 5.0);

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
            _context.Speech.Speak($"Exterior grid view. {announcement}", true);
            return;
        }

        var currentZone = _navigator.GetZoneAt(_position);
        _context.Speech.Speak($"Exterior grid view. {currentZone?.Name ?? "Unknown area"}.", true);
    }
}
