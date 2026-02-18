namespace AircraftExplorer.Audio;

public sealed class TolkSpeechService : ISpeechService, IDisposable
{
    private readonly bool _isActive;
    private bool _disposed;

    public TolkSpeechService()
    {
        try
        {
            TolkNative.Tolk_TrySAPI(true);
            TolkNative.Tolk_Load();
            _isActive = TolkNative.Tolk_IsLoaded() && TolkNative.Tolk_HasSpeech();
        }
        catch
        {
            _isActive = false;
        }
    }

    public bool IsScreenReaderActive => _isActive;

    public void Speak(string text, bool interrupt = false)
    {
        if (_isActive)
        {
            TolkNative.Tolk_Output(text, interrupt);
        }
        else
        {
            Console.WriteLine(text);
        }
    }

    public void Silence()
    {
        if (_isActive)
        {
            TolkNative.Tolk_Silence();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isActive)
        {
            try
            {
                TolkNative.Tolk_Unload();
            }
            catch
            {
                // Ignore errors during unload.
            }
        }
    }
}
