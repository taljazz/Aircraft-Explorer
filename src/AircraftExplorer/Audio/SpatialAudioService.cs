using AircraftExplorer.Aircraft;
using AircraftExplorer.Navigation;
using OpenTK.Audio.OpenAL;

namespace AircraftExplorer.Audio;

public sealed class SpatialAudioService : ISpatialAudioService
{
    // Frequency range for Z-to-pitch mapping
    private const float MinFrequency = 200f;  // ground level
    private const float MaxFrequency = 800f;  // top of aircraft
    private const float BoundaryFrequency = 100f;

    // Beacon timing
    private const int BeaconMinIntervalMs = 150;
    private const int BeaconMaxIntervalMs = 1000;
    private const double BeaconMaxDistance = 10.0;
    private const float BeaconFrequency = 440f;

    private const int SampleRate = 44100;

    private readonly ALDevice _device;
    private readonly ALContext _context;
    private readonly bool _initialized;
    private readonly object _lock = new();

    // Reusable tone buffers (keyed by frequency bucket to avoid regenerating constantly)
    private readonly Dictionary<int, int> _toneBufferCache = new();

    // Track one-shot sources so we can clean them up
    private readonly List<int> _activeSources = new();

    // Store timer references to prevent GC collection
    private readonly List<System.Threading.Timer> _activeTimers = new();

    // Beacon state
    private int _beaconSource;
    private int _beaconBuffer;
    private bool _beaconActive;
    private System.Threading.Timer? _beaconTimer;
    private Coordinate3D _beaconPosition;

    private float _toneVolume = 0.5f;
    private bool _disposed;

    public float ToneVolume => _toneVolume;

    public SpatialAudioService()
    {
        try
        {
            _device = ALC.OpenDevice(null);
            if (_device == ALDevice.Null)
            {
                _initialized = false;
                return;
            }

            _context = ALC.CreateContext(_device, (int[])null!);
            ALC.MakeContextCurrent(_context);

            // Configure listener defaults
            AL.Listener(ALListener3f.Position, 0f, 0f, 0f);
            AL.Listener(ALListener3f.Velocity, 0f, 0f, 0f);

            // Set listener orientation: forward = (0,0,-1) maps to +Y in app space,
            // up = (0,1,0) maps to +Z in app space
            float[] orientation = { 0f, 0f, -1f, 0f, 1f, 0f };
            AL.Listener(ALListenerfv.Orientation, orientation);

            // Set distance model for natural attenuation
            AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);

            AL.Listener(ALListenerf.Gain, _toneVolume);
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    public void AdjustToneVolume(float delta)
    {
        _toneVolume = Math.Clamp(_toneVolume + delta, 0f, 1f);
        if (_initialized)
            AL.Listener(ALListenerf.Gain, _toneVolume);
    }

    public void UpdateListenerPosition(Coordinate3D position)
    {
        if (!_initialized) return;

        // Map our coordinate system to OpenAL:
        // Our X (lateral) -> AL X, Our Z (vertical) -> AL Y, Our Y (longitudinal) -> AL -Z
        AL.Listener(ALListener3f.Position, position.X, position.Z, -position.Y);
    }

    public void PlayMovementTone(Coordinate3D position, GridBounds bounds)
    {
        // Non-directional: plays centered at the listener (e.g. OnEnter, OnResume, C-key)
        PlayMovementToneInternal(position, bounds, 0f, 0f, 0f);
    }

    public void PlayMovementTone(Coordinate3D position, GridBounds bounds, int dx, int dy, int dz)
    {
        // Directional: offset the tone in the movement direction so the user hears panning.
        // Map app axes to AL relative offset:
        //   dx (lateral) → AL X,  dz (vertical) → AL Y,  dy (longitudinal) → AL -Z
        const float OffsetScale = 0.6f;
        float alX = dx * OffsetScale;
        float alY = dz * OffsetScale;
        float alZ = -dy * OffsetScale;
        PlayMovementToneInternal(position, bounds, alX, alY, alZ);
    }

    private void PlayMovementToneInternal(Coordinate3D position, GridBounds bounds,
        float alOffsetX, float alOffsetY, float alOffsetZ)
    {
        if (!_initialized) return;

        lock (_lock)
        {
            CleanUpFinishedSources();

            float freq = MapToFrequency(position.Z, bounds.MinZ, bounds.MaxZ);
            int buffer = GetOrCreateToneBuffer(freq, 0.1f);

            int source = AL.GenSource();
            AL.Source(source, ALSourcef.Gain, 0.4f);
            AL.Source(source, ALSourcef.Pitch, 1.0f);
            AL.Source(source, ALSourceb.Looping, false);

            // Use listener-relative positioning so offset creates panning
            AL.Source(source, ALSourceb.SourceRelative, true);
            AL.Source(source, ALSource3f.Position, alOffsetX, alOffsetY, alOffsetZ);
            AL.Source(source, ALSourcef.ReferenceDistance, 0.5f);
            AL.Source(source, ALSourcef.MaxDistance, 5.0f);

            AL.Source(source, ALSourcei.Buffer, buffer);
            AL.SourcePlay(source);
            _activeSources.Add(source);
        }
    }

    public void PlayBoundaryTone()
    {
        if (!_initialized) return;

        lock (_lock)
        {
            CleanUpFinishedSources();

            int buffer = GetOrCreateToneBuffer(BoundaryFrequency, 0.2f);

            int source = AL.GenSource();
            AL.Source(source, ALSourcef.Gain, 0.6f);
            AL.Source(source, ALSourceb.SourceRelative, true); // play at listener position
            AL.Source(source, ALSource3f.Position, 0f, 0f, 0f);
            AL.Source(source, ALSourceb.Looping, false);

            AL.Source(source, ALSourcei.Buffer, buffer);
            AL.SourcePlay(source);
            _activeSources.Add(source);
        }
    }

    public void PlayZoneTransitionTone(bool ascending)
    {
        if (!_initialized) return;

        lock (_lock)
        {
            CleanUpFinishedSources();

            float freq1 = ascending ? 400f : 600f;
            float freq2 = ascending ? 600f : 400f;

            // First note — immediate
            int buffer1 = GetOrCreateToneBuffer(freq1, 0.1f);
            int source1 = AL.GenSource();
            AL.Source(source1, ALSourcef.Gain, 0.35f);
            AL.Source(source1, ALSourceb.SourceRelative, true);
            AL.Source(source1, ALSource3f.Position, 0f, 0f, 0f);
            AL.Source(source1, ALSourceb.Looping, false);
            AL.Source(source1, ALSourcei.Buffer, buffer1);
            AL.SourcePlay(source1);
            _activeSources.Add(source1);

            // Second note — delayed by ~120ms via a timer
            int buffer2 = GetOrCreateToneBuffer(freq2, 0.1f);
            var timer = new System.Threading.Timer(_ =>
            {
                if (_disposed) return;
                lock (_lock)
                {
                    if (_disposed) return;
                    int source2 = AL.GenSource();
                    AL.Source(source2, ALSourcef.Gain, 0.35f);
                    AL.Source(source2, ALSourceb.SourceRelative, true);
                    AL.Source(source2, ALSource3f.Position, 0f, 0f, 0f);
                    AL.Source(source2, ALSourceb.Looping, false);
                    AL.Source(source2, ALSourcei.Buffer, buffer2);
                    AL.SourcePlay(source2);
                    _activeSources.Add(source2);
                }
            }, null, 120, Timeout.Infinite);
            _activeTimers.Add(timer);
        }
    }

    public void StartComponentBeacon(Coordinate3D componentPosition, double distance)
    {
        if (!_initialized) return;

        lock (_lock)
        {
            StopComponentBeaconCore();

            _beaconPosition = componentPosition;
            _beaconBuffer = GetOrCreateToneBuffer(BeaconFrequency, 0.06f);

            _beaconSource = AL.GenSource();
            AL.Source(_beaconSource, ALSourcef.Gain, 0.4f);
            AL.Source(_beaconSource, ALSourcef.ReferenceDistance, 1.0f);
            AL.Source(_beaconSource, ALSourcef.MaxDistance, 15.0f);
            AL.Source(_beaconSource, ALSourceb.Looping, false);
            AL.Source(_beaconSource, ALSource3f.Position,
                componentPosition.X, componentPosition.Z, -componentPosition.Y);
            AL.Source(_beaconSource, ALSourcei.Buffer, _beaconBuffer);

            _beaconActive = true;

            // Start pulsing via timer
            int intervalMs = DistanceToInterval(distance);
            _beaconTimer = new System.Threading.Timer(BeaconPulse, null, 0, intervalMs);
        }
    }

    public void UpdateComponentBeacon(double distance)
    {
        lock (_lock)
        {
            if (!_beaconActive || _beaconTimer is null) return;

            int intervalMs = DistanceToInterval(distance);
            _beaconTimer.Change(0, intervalMs);
        }
    }

    public void StopComponentBeacon()
    {
        lock (_lock)
        {
            StopComponentBeaconCore();
        }
    }

    // Must be called under _lock
    private void StopComponentBeaconCore()
    {
        _beaconActive = false;

        if (_beaconTimer is not null)
        {
            _beaconTimer.Dispose();
            _beaconTimer = null;
        }

        if (_beaconSource != 0)
        {
            AL.SourceStop(_beaconSource);
            AL.DeleteSource(_beaconSource);
            _beaconSource = 0;
        }
    }

    private void BeaconPulse(object? state)
    {
        lock (_lock)
        {
            if (!_beaconActive || _disposed || _beaconSource == 0) return;

            try
            {
                AL.GetSource(_beaconSource, ALGetSourcei.SourceState, out int stateValue);
                if ((ALSourceState)stateValue != ALSourceState.Playing)
                {
                    AL.SourcePlay(_beaconSource);
                }
            }
            catch
            {
                // Source may have been deleted
            }
        }
    }

    public void PlayComponentArrivedTone()
    {
        if (!_initialized) return;

        lock (_lock)
        {
            StopComponentBeaconCore();
            CleanUpFinishedSources();

            int buffer = GetOrCreateToneBuffer(BeaconFrequency, 0.06f);

            // Three rapid beeps at 0ms, 80ms, 160ms
            for (int i = 0; i < 3; i++)
            {
                int delayMs = i * 80;
                int capturedBuffer = buffer;
                var timer = new System.Threading.Timer(_ =>
                {
                    if (_disposed) return;
                    lock (_lock)
                    {
                        if (_disposed) return;
                        int src = AL.GenSource();
                        AL.Source(src, ALSourcef.Gain, 0.5f);
                        AL.Source(src, ALSourceb.SourceRelative, true);
                        AL.Source(src, ALSource3f.Position, 0f, 0f, 0f);
                        AL.Source(src, ALSourceb.Looping, false);
                        AL.Source(src, ALSourcei.Buffer, capturedBuffer);
                        AL.SourcePlay(src);
                        _activeSources.Add(src);
                    }
                }, null, delayMs, Timeout.Infinite);
                _activeTimers.Add(timer);
            }
        }
    }

    private int GetOrCreateToneBuffer(float frequency, float durationSeconds)
    {
        // Bucket frequency to nearest 10Hz to reuse buffers
        int key = ((int)(frequency / 10f)) * 10 + (int)(durationSeconds * 1000);

        if (_toneBufferCache.TryGetValue(key, out int existing))
            return existing;

        var samples = GenerateSineWave(frequency, durationSeconds);
        int buffer = AL.GenBuffer();
        AL.BufferData(buffer, ALFormat.Mono16, samples, SampleRate);

        _toneBufferCache[key] = buffer;
        return buffer;
    }

    private static short[] GenerateSineWave(float frequency, float durationSeconds)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        var samples = new short[sampleCount];
        double phaseStep = 2.0 * Math.PI * frequency / SampleRate;

        int attackSamples = SampleRate / 100;  // 10ms
        int decaySamples = SampleRate / 33;    // 30ms

        for (int i = 0; i < sampleCount; i++)
        {
            float envelope = 1f;

            if (i < attackSamples)
                envelope = (float)i / attackSamples;

            int remaining = sampleCount - i;
            if (remaining < decaySamples)
                envelope = Math.Min(envelope, (float)remaining / decaySamples);

            double sample = Math.Sin(phaseStep * i) * envelope * 0.7;
            samples[i] = (short)(sample * short.MaxValue);
        }

        return samples;
    }

    // Must be called under _lock
    private void CleanUpFinishedSources()
    {
        for (int i = _activeSources.Count - 1; i >= 0; i--)
        {
            int source = _activeSources[i];
            AL.GetSource(source, ALGetSourcei.SourceState, out int state);

            if ((ALSourceState)state == ALSourceState.Stopped)
            {
                AL.DeleteSource(source);
                _activeSources.RemoveAt(i);
            }
        }

        // Clean up completed one-shot timers
        for (int i = _activeTimers.Count - 1; i >= 0; i--)
        {
            // Dispose and remove timers that have already fired (one-shot timers)
            // We keep them briefly to prevent GC; safe to clean up after sources are done
            if (_activeSources.Count == 0 && _activeTimers.Count > 0)
            {
                _activeTimers[i].Dispose();
                _activeTimers.RemoveAt(i);
            }
        }
    }

    private static float MapToFrequency(int z, int minZ, int maxZ)
    {
        if (maxZ == minZ) return (MinFrequency + MaxFrequency) / 2f;
        float t = (float)(z - minZ) / (maxZ - minZ);
        return MinFrequency + t * (MaxFrequency - MinFrequency);
    }

    private static int DistanceToInterval(double distance)
    {
        double t = Math.Clamp(distance / BeaconMaxDistance, 0.0, 1.0);
        return (int)(BeaconMinIntervalMs + t * (BeaconMaxIntervalMs - BeaconMinIntervalMs));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            StopComponentBeaconCore();

            // Dispose all stored timers
            foreach (var timer in _activeTimers)
                timer.Dispose();
            _activeTimers.Clear();

            // Clean up all active sources
            foreach (int source in _activeSources)
            {
                AL.SourceStop(source);
                AL.DeleteSource(source);
            }
            _activeSources.Clear();

            // Clean up cached buffers
            foreach (int buffer in _toneBufferCache.Values)
                AL.DeleteBuffer(buffer);
            _toneBufferCache.Clear();
        }

        try
        {
            if (_context != ALContext.Null)
            {
                ALC.MakeContextCurrent(ALContext.Null);
                ALC.DestroyContext(_context);
            }

            if (_device != ALDevice.Null)
                ALC.CloseDevice(_device);
        }
        catch { }
    }
}
