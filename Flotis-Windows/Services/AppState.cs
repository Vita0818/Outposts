using Flotis.Models;

namespace Flotis.Services;

public sealed class AppState
{
    public bool HasAccessibilityPermission { get; set; }
    public bool IsPanelVisible { get; set; }
    public string? PasteError { get; set; }

    public VoiceInputMode VoiceMode { get; set; } = VoiceInputMode.WindowsSpeech;
    public VoiceInputState VoiceState { get; set; } = VoiceInputState.Idle;
    public string TranscriptPreview { get; set; } = string.Empty;
    public string SelectedSpeechLocale { get; set; } = "zh-CN";
}
