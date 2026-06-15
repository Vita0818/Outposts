namespace Flotis.Models;

public enum VoiceInputStateKind
{
    Idle,
    RequestingPermission,
    Recording,
    Transcribing,
    Injecting,
    Failed
}

public sealed class VoiceInputState
{
    public VoiceInputStateKind Kind { get; private set; } = VoiceInputStateKind.Idle;
    public string? ErrorMessage { get; private set; }

    public static VoiceInputState Idle => new();
    public static VoiceInputState Failed(string message)
    {
        return new VoiceInputState
        {
            Kind = VoiceInputStateKind.Failed,
            ErrorMessage = message
        };
    }

    public void Set(VoiceInputStateKind kind, string? error = null)
    {
        Kind = kind;
        ErrorMessage = error;
    }

}
