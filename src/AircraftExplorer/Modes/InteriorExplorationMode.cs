using AircraftExplorer.Aircraft;
using AircraftExplorer.Education;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Modes;

public class InteriorExplorationMode : IAppMode
{
    private NavigationGrid _grid = null!;
    private ModeContext _context = null!;
    private Coordinate3D _position;

    public string ModeName => "Interior Exploration";

    public void OnEnter(ModeContext context)
    {
        _context = context;
        var aircraft = context.SelectedAircraft
            ?? throw new InvalidOperationException("No aircraft selected.");

        _grid = new NavigationGrid(aircraft);
        _position = aircraft.EntryCoordinate;
        context.CurrentPosition = _position;

        var zone = _grid.GetZoneAt(_position);
        var zoneDesc = zone is not null ? zone.Description : "Unknown area";

        context.Speech.Speak(
            $"Entering {aircraft.Name}. {zoneDesc}. Use arrow keys to explore.",
            true);

        // Initial position tone
        context.SpatialAudio.UpdateListenerPosition(_position);
        context.SpatialAudio.PlayMovementTone(_position, aircraft.GridBounds);
        StartBeaconIfNearComponent();
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
                return ShowNearbyInfo();

            case InputAction.Select:
                return HandleSelect();

            case InputAction.ToggleExteriorMode:
                _context.SpatialAudio.StopComponentBeacon();
                return ModeResult.SwitchTo(new ExteriorGridMode());

            case InputAction.JumpToComponent:
                return ShowJumpList();

            case InputAction.Help:
                _context.Speech.Speak(
                    "Interior exploration. Arrow keys to move. C to announce position. " +
                    "I for information. J to jump to a component. Enter at cockpit seat for flight controls. " +
                    "T for exterior view. Escape to go back.",
                    true);
                return ModeResult.Stay;

            case InputAction.Back:
                _context.SpatialAudio.StopComponentBeacon();
                return ModeResult.Pop;

            default:
                return ModeResult.Stay;
        }
    }

    private ModeResult HandleMovement(int dx, int dy, int dz)
    {
        var result = _grid.TryMove(_position, dx, dy, dz);
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

        // Move the OpenAL listener to the new position
        _context.SpatialAudio.UpdateListenerPosition(_position);

        // Spatial audio: directional tone (panned in movement direction)
        _context.SpatialAudio.PlayMovementTone(_position, aircraft.GridBounds, dx, dy, dz);

        // Zone transition chime
        if (result.ZoneChanged)
        {
            bool ascending = _position.Y > previousPosition.Y || _position.Z > previousPosition.Z;
            _context.SpatialAudio.PlayZoneTransitionTone(ascending);
        }

        // Component beacon — triple tone on arrival, pulsing when approaching
        if (result.NearbyComponents.Count > 0)
        {
            var closest = result.NearbyComponents[0];
            double distance = _position.DistanceTo(closest.Coordinate);
            if (distance < 1.0)
                _context.SpatialAudio.PlayComponentArrivedTone();
            else
                _context.SpatialAudio.StartComponentBeacon(closest.Coordinate, distance);
        }
        else
        {
            _context.SpatialAudio.StopComponentBeacon();
        }

        var announcement = NavigationAnnouncer.BuildMovementAnnouncement(
            dx, dy, dz, result, _position, aircraft);
        _context.Speech.Speak(announcement, true);

        return ModeResult.Stay;
    }

    private void AnnounceCurrentPosition()
    {
        var zone = _grid.GetZoneAt(_position);
        var nearby = _grid.GetNearbyComponents(_position);
        var aircraft = _context.SelectedAircraft!;

        var announcement = NavigationAnnouncer.BuildFullContextAnnouncement(
            _position, aircraft, zone, nearby);
        _context.Speech.Speak(announcement, true);

        // Re-emit position tone so user can hear their spatial location
        _context.SpatialAudio.PlayMovementTone(_position, aircraft.GridBounds);
    }

    private ModeResult ShowNearbyInfo()
    {
        var nearby = _grid.GetNearbyComponents(_position);

        if (nearby.Count == 0)
        {
            _context.Speech.Speak("No components nearby to inspect.", true);
            return ModeResult.Stay;
        }

        var aircraftId = _context.SelectedAircraft!.Id;
        var topics = new List<EducationTopic>();
        var seen = new HashSet<string>();
        foreach (var component in nearby)
        {
            var componentTopics = _context.EducationProvider.GetTopicsForComponent(component.Id);
            foreach (var topic in componentTopics)
            {
                if (seen.Add(topic.Id) && (topic.AircraftIds.Count == 0 || topic.AircraftIds.Contains(aircraftId)))
                    topics.Add(topic);
            }
        }

        if (topics.Count == 0)
        {
            _context.Speech.Speak(
                $"Near {nearby[0].Name}. {nearby[0].Description}. No additional information available.",
                true);
            return ModeResult.Stay;
        }

        return ModeResult.PushMode(new InfoViewMode(topics));
    }

    private ModeResult HandleSelect()
    {
        var aircraft = _context.SelectedAircraft;
        if (aircraft is null)
            return ModeResult.Stay;

        // Check if at cockpit seat position
        if (_position == aircraft.CockpitSeatCoordinate)
        {
            _context.SpatialAudio.StopComponentBeacon();
            return ModeResult.PushMode(new FlightControlMode());
        }

        // Otherwise, interact with nearest component
        var nearby = _grid.GetNearbyComponents(_position, 1.5);
        if (nearby.Count > 0)
        {
            var component = nearby[0];
            if (!string.IsNullOrEmpty(component.InteractionText))
            {
                _context.Speech.Speak(component.InteractionText, true);
            }
            else
            {
                _context.Speech.Speak(component.Description, true);
            }
        }
        else
        {
            _context.Speech.Speak("Nothing to interact with here.", true);
        }

        return ModeResult.Stay;
    }

    public void OnExit()
    {
        _context.SpatialAudio.StopComponentBeacon();
    }

    private ModeResult ShowJumpList()
    {
        var aircraft = _context.SelectedAircraft;
        if (aircraft is null)
        {
            _context.Speech.Speak("No components available.", true);
            return ModeResult.Stay;
        }

        var interiorZones = aircraft.Zones.Where(z => !z.IsExterior).ToList();
        var components = aircraft.Components
            .Where(c => interiorZones.Any(z => z.Contains(c.Coordinate)))
            .ToList();

        if (components.Count == 0)
        {
            _context.Speech.Speak("No interior components available.", true);
            return ModeResult.Stay;
        }

        _context.SpatialAudio.StopComponentBeacon();
        return ModeResult.PushMode(new ComponentListMode(components));
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

            var zone = _grid.GetZoneAt(_position);
            var nearby = _grid.GetNearbyComponents(_position);
            var announcement = NavigationAnnouncer.BuildFullContextAnnouncement(
                _position, aircraft, zone, nearby);
            _context.Speech.Speak($"Interior exploration. {announcement}", true);
            return;
        }

        var currentZone = _grid.GetZoneAt(_position);
        var zoneName = currentZone?.Name ?? "Unknown area";
        _context.Speech.Speak($"Interior exploration. {zoneName}.", true);

        if (_context.SelectedAircraft is not null)
            _context.SpatialAudio.PlayMovementTone(_position, _context.SelectedAircraft.GridBounds);
        StartBeaconIfNearComponent();
    }

    private void StartBeaconIfNearComponent()
    {
        var aircraft = _context.SelectedAircraft;
        if (aircraft is null) return;

        var nearby = _grid.GetNearbyComponents(_position);
        if (nearby.Count > 0)
        {
            double distance = _position.DistanceTo(nearby[0].Coordinate);
            if (distance >= 1.0)
                _context.SpatialAudio.StartComponentBeacon(nearby[0].Coordinate, distance);
        }
    }
}
