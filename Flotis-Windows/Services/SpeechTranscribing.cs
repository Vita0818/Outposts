namespace Flotis.Services;

public interface SpeechTranscribing
{
    Action<string>? PartialTranscriptHandler { get; set; }

    Task Start();
    Task<string> Stop();
    void Cancel();
}
