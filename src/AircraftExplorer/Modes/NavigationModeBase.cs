using AircraftExplorer.Aircraft;
using AircraftExplorer.Education;
using AircraftExplorer.Input;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Modes;

public abstract class NavigationModeBase : IAppMode
{
    private ModeContext? _contextField;

    protected ModeContext Context
    {
        get => _contextField ?? throw new InvalidOperationException("Mode not yet entered — OnEnter has not been called.");
        private set => _contextField = value;
    }

    protected INavigationSpace Navigator { get; set; } = null!;
    protected Coordinate3D Position { get; set; }

    public abstract string ModeName { get; }
    protected abstract INavigationSpace CreateNavigator(AircraftModel aircraft);
    protected abstract Coordinate3D GetStartPosition(AircraftModel aircraft);
    protected abstract bool IsExteriorFilter { get; }
    protected abstract string GetResumePrefix();

    public virtual void OnEnter(ModeContext context)
    {
        Context = context;
        var aircraft = context.SelectedAircraft
            ?? throw new InvalidOperationException("No aircraft selected.");

        Navigator = CreateNavigator(aircraft);
        Position = GetStartPosition(aircraft);
        context.CurrentPosition = Position;
    }

    public abstract ModeResult HandleInput(InputAction action);

    public virtual void OnExit()
    {
        Context.SpatialAudio.StopComponentBeacon();
    }

    public virtual void OnResume()
    {
        if (Context.JumpTarget is { } target)
        {
            Context.JumpTarget = null;
            Position = target;
            Context.CurrentPosition = Position;

            var aircraft = Context.SelectedAircraft!;
            Context.SpatialAudio.UpdateListenerPosition(Position);
            Context.SpatialAudio.PlayComponentArrivedTone();

            var zone = Navigator.GetZoneAt(Position);
            var nearby = Navigator.GetNearbyComponents(Position);
            var announcement = NavigationAnnouncer.BuildFullContextAnnouncement(
                Position, aircraft, zone, nearby,
                Context.Settings.Navigation.AnnounceNearbyComponents);
            Context.Speech.Speak($"{GetResumePrefix()}. {announcement}", true);
            return;
        }

        OnResumeDefault();
    }

    protected virtual void OnResumeDefault()
    {
        var currentZone = Navigator.GetZoneAt(Position);
        var zoneName = currentZone?.Name ?? "Unknown area";
        Context.Speech.Speak($"{GetResumePrefix()}. {zoneName}.", true);

        if (Context.SelectedAircraft is not null)
            Context.SpatialAudio.PlayMovementTone(Position, Context.SelectedAircraft.GridBounds);
        if (Context.Settings.Navigation.AnnounceNearbyComponents)
            StartBeaconIfNearComponent();
    }

    protected ModeResult HandleMovement(int dx, int dy, int dz)
    {
        var result = Navigator.TryMove(Position, dx, dy, dz);
        var aircraft = Context.SelectedAircraft!;
        var navSettings = Context.Settings.Navigation;

        if (!result.Success)
        {
            Context.Speech.Speak(result.BoundaryMessage ?? "Can't move there.", true);
            Context.SpatialAudio.PlayBoundaryTone();
            return ModeResult.Stay;
        }

        var previousPosition = Position;
        Position = result.NewPosition;
        Context.CurrentPosition = Position;

        // Move the OpenAL listener to the new position
        Context.SpatialAudio.UpdateListenerPosition(Position);

        // Spatial audio: directional tone (panned in movement direction)
        Context.SpatialAudio.PlayMovementTone(Position, aircraft.GridBounds, dx, dy, dz);

        // Zone transition chime
        if (navSettings.AnnounceZoneChanges && result.ZoneChanged)
        {
            bool ascending = Position.Y > previousPosition.Y || Position.Z > previousPosition.Z;
            Context.SpatialAudio.PlayZoneTransitionTone(ascending);
        }

        // Component beacon — triple tone on arrival, pulsing when approaching
        if (navSettings.AnnounceNearbyComponents && result.NearbyComponents.Count > 0)
        {
            var closest = result.NearbyComponents[0];
            double distance = Position.DistanceTo(closest.Coordinate);
            if (distance < 1.0)
                Context.SpatialAudio.PlayComponentArrivedTone();
            else
                Context.SpatialAudio.StartComponentBeacon(closest.Coordinate, distance);
        }
        else if (navSettings.AnnounceNearbyComponents)
        {
            Context.SpatialAudio.StopComponentBeacon();
        }
        else
        {
            Context.SpatialAudio.StopComponentBeacon();
        }

        var announcement = NavigationAnnouncer.BuildMovementAnnouncement(
            dx, dy, dz, result, Position, aircraft,
            navSettings.AnnounceZoneChanges,
            navSettings.AnnounceNearbyComponents);
        Context.Speech.Speak(announcement, true);

        return ModeResult.Stay;
    }

    protected void AnnounceCurrentPosition()
    {
        var zone = Navigator.GetZoneAt(Position);
        var nearby = Navigator.GetNearbyComponents(Position);
        var aircraft = Context.SelectedAircraft!;

        var announcement = NavigationAnnouncer.BuildFullContextAnnouncement(
            Position, aircraft, zone, nearby,
            Context.Settings.Navigation.AnnounceNearbyComponents);
        Context.Speech.Speak(announcement, true);

        // Re-emit position tone so user can hear their spatial location
        Context.SpatialAudio.PlayMovementTone(Position, aircraft.GridBounds);
    }

    protected ModeResult GatherAndShowTopics(double radius = 2.0)
    {
        var nearby = Navigator.GetNearbyComponents(Position, radius);

        if (nearby.Count == 0)
        {
            Context.Speech.Speak("No components nearby to inspect.", true);
            return ModeResult.Stay;
        }

        var aircraftId = Context.SelectedAircraft!.Id;
        var topics = new List<EducationTopic>();
        var seen = new HashSet<string>();
        foreach (var component in nearby)
        {
            foreach (var topic in Context.EducationProvider.GetTopicsForComponent(component.Id))
            {
                if (seen.Add(topic.Id) && (topic.AircraftIds.Count == 0 || topic.AircraftIds.Contains(aircraftId)))
                    topics.Add(topic);
            }
        }

        if (topics.Count == 0)
        {
            Context.Speech.Speak(
                $"Near {nearby[0].Name}. {nearby[0].Description}. No additional information available.",
                true);
            return ModeResult.Stay;
        }

        return ModeResult.PushMode(new InfoViewMode(topics));
    }

    protected ModeResult ShowJumpList()
    {
        var aircraft = Context.SelectedAircraft;
        if (aircraft is null)
        {
            Context.Speech.Speak("No components available.", true);
            return ModeResult.Stay;
        }

        var filteredZones = aircraft.Zones.Where(z => z.IsExterior == IsExteriorFilter).ToList();
        var components = aircraft.Components
            .Where(c => filteredZones.Any(z => z.Contains(c.Coordinate)))
            .ToList();

        string label = IsExteriorFilter ? "exterior" : "interior";
        if (components.Count == 0)
        {
            Context.Speech.Speak($"No {label} components available.", true);
            return ModeResult.Stay;
        }

        Context.SpatialAudio.StopComponentBeacon();
        return ModeResult.PushMode(new ComponentListMode(components));
    }

    protected ModeResult GatherQuizQuestions(double radius = 2.0)
    {
        var nearby = Navigator.GetNearbyComponents(Position, radius);

        if (nearby.Count == 0)
        {
            Context.Speech.Speak("No components nearby to quiz on.", true);
            return ModeResult.Stay;
        }

        var aircraftId = Context.SelectedAircraft!.Id;
        var questions = new List<QuizQuestion>();
        var seen = new HashSet<string>();

        foreach (var component in nearby)
        {
            foreach (var topic in Context.EducationProvider.GetTopicsForComponent(component.Id))
            {
                if (topic.AircraftIds.Count > 0 && !topic.AircraftIds.Contains(aircraftId))
                    continue;

                foreach (var q in topic.QuizQuestions)
                {
                    if (seen.Add(q.Id))
                        questions.Add(q);
                }
            }
        }

        if (questions.Count == 0)
        {
            Context.Speech.Speak("No quiz questions available for nearby components.", true);
            return ModeResult.Stay;
        }

        // Shuffle questions
        var rng = new Random();
        for (int i = questions.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (questions[i], questions[j]) = (questions[j], questions[i]);
        }

        return ModeResult.PushMode(new QuizMode(questions));
    }

    protected void StartBeaconIfNearComponent()
    {
        var aircraft = Context.SelectedAircraft;
        if (aircraft is null) return;

        var nearby = Navigator.GetNearbyComponents(Position);
        if (nearby.Count > 0)
        {
            double distance = Position.DistanceTo(nearby[0].Coordinate);
            if (distance >= 1.0)
                Context.SpatialAudio.StartComponentBeacon(nearby[0].Coordinate, distance);
        }
    }
}
