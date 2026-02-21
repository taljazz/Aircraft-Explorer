using AircraftExplorer.Aircraft;
using AircraftExplorer.Navigation;

namespace AircraftExplorer.Audio;

public interface ISpatialAudioService : IDisposable
{
    /// <summary>Updates the OpenAL listener to the user's current position.</summary>
    void UpdateListenerPosition(Coordinate3D position);

    /// <summary>
    /// Plays a short tone centered at the listener (no panning).
    /// Pitch reflects vertical height (Z).
    /// </summary>
    void PlayMovementTone(Coordinate3D position, GridBounds bounds);

    /// <summary>
    /// Plays a short tone panned in the movement direction.
    /// dx/dy/dz indicate the step direction; the tone is offset from the listener
    /// so the user hears spatial feedback matching their movement.
    /// </summary>
    void PlayMovementTone(Coordinate3D position, GridBounds bounds, int dx, int dy, int dz);

    /// <summary>Plays a low buzz at the listener's current position (boundary hit).</summary>
    void PlayBoundaryTone();

    /// <summary>Plays a two-note chime at the listener position on zone transitions.</summary>
    void PlayZoneTransitionTone(bool ascending);

    /// <summary>
    /// Starts a repeating beacon tone at a component's 3D position.
    /// Pulse rate is based on distance: closer = faster pulsing.
    /// OpenAL spatializes the beacon toward the component automatically.
    /// </summary>
    void StartComponentBeacon(Coordinate3D componentPosition, double distance);

    /// <summary>Updates the beacon pulse rate based on new distance.</summary>
    void UpdateComponentBeacon(double distance);

    /// <summary>Stops the component proximity beacon.</summary>
    void StopComponentBeacon();

    /// <summary>Plays a fast triple tone indicating arrival at a component.</summary>
    void PlayComponentArrivedTone();

    /// <summary>Plays an ascending 3-note chime for a correct quiz answer.</summary>
    void PlayCorrectAnswerTone();

    /// <summary>Plays a descending 2-note buzz for an incorrect quiz answer.</summary>
    void PlayIncorrectAnswerTone();

    /// <summary>Gets the current tone volume (0.0 to 1.0).</summary>
    float ToneVolume { get; }

    /// <summary>Adjusts the master tone volume by the given delta, clamped to 0.0–1.0.</summary>
    void AdjustToneVolume(float delta);
}
