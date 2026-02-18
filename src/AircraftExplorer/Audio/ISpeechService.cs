namespace AircraftExplorer.Audio;

public interface ISpeechService
{
    void Speak(string text, bool interrupt = false);
    void Silence();
    bool IsScreenReaderActive { get; }
}
