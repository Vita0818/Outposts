using Flotis.Models;
using Flotis.Interop;
using Microsoft.UI.Dispatching;

namespace Flotis.Services;

public sealed class VoiceInputController
{
    private readonly AppState _state;
    private readonly ClipboardPasteService _clipboardService;
    private readonly DispatcherQueue _uiQueue;
    private readonly Action _onStateChanged;

    private SpeechTranscribing? _activeTranscriber;
    private bool _isProcessing;

    public VoiceInputController(AppState state, ClipboardPasteService clipboardService, DispatcherQueue uiQueue, Action onStateChanged)
    {
        _state = state;
        _clipboardService = clipboardService;
        _uiQueue = uiQueue;
        _onStateChanged = onStateChanged;
    }

    public void ToggleRecording()
    {
        if (_isProcessing) return;

        if (_state.VoiceState.Kind == VoiceInputStateKind.Recording)
        {
            _ = StopAndInjectAsync();
        }
        else if (_state.VoiceState.Kind == VoiceInputStateKind.Failed || _state.VoiceState.Kind == VoiceInputStateKind.Idle)
        {
            _ = StartRecordingAsync();
        }
    }

    private async Task StartRecordingAsync()
    {
        try
        {
            _state.VoiceState = new();
            _state.VoiceState.Set(VoiceInputStateKind.RequestingPermission);
            _isProcessing = true;
            _state.PasteError = null;
            _state.TranscriptPreview = string.Empty;
            UpdateUi();

            var transcriber = _state.VoiceMode == VoiceInputMode.ExternalProvider
                ? CreateExternalProviderTranscriber()
                : new WindowsSpeechTranscriber(_state.SelectedSpeechLocale);

            transcriber.PartialTranscriptHandler = text =>
            {
                _uiQueue.TryEnqueue(() =>
                {
                    _state.TranscriptPreview = text;
                    UpdateUi();
                });
            };

            _activeTranscriber = transcriber;
            await transcriber.Start();
            _state.VoiceState.Set(VoiceInputStateKind.Recording);
            _isProcessing = false;
            UpdateUi();
        }
        catch (Exception ex)
        {
            _isProcessing = false;
            _state.HasAccessibilityPermission = ClipboardPasteService.CheckPasteCapability();
            _state.VoiceState.Set(VoiceInputStateKind.Failed, ex.Message);
            _state.PasteError = ex.Message;
            _activeTranscriber = null;
            UpdateUi();
        }
    }

    private async Task StopAndInjectAsync()
    {
        if (_activeTranscriber is null)
        {
            _state.VoiceState.Set(VoiceInputStateKind.Idle);
            UpdateUi();
            return;
        }

        _isProcessing = true;
        _state.VoiceState.Set(VoiceInputStateKind.Transcribing);
        UpdateUi();

        string transcript;
        try
        {
            transcript = await _activeTranscriber.Stop();
        }
        catch (Exception ex)
        {
            _state.VoiceState.Set(VoiceInputStateKind.Failed, ex.Message);
            _state.PasteError = ex.Message;
            _activeTranscriber = null;
            _isProcessing = false;
            _state.TranscriptPreview = string.Empty;
            UpdateUi();
            return;
        }

        _state.TranscriptPreview = transcript;
        UpdateUi();
        _activeTranscriber = null;

        if (string.IsNullOrWhiteSpace(transcript))
        {
            _state.VoiceState.Set(VoiceInputStateKind.Idle);
            _state.TranscriptPreview = string.Empty;
            _isProcessing = false;
            UpdateUi();
            return;
        }

        _state.VoiceState.Set(VoiceInputStateKind.Injecting);
        UpdateUi();

        var injected = await _clipboardService.InjectAsync(transcript);
        if (!injected)
        {
            _state.VoiceState.Set(VoiceInputStateKind.Failed, "注入失败，可能没有权限");
            _state.PasteError = "注入失败，可能没有权限";
        }
        else
        {
            _state.VoiceState.Set(VoiceInputStateKind.Idle);
            _state.PasteError = null;
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                _uiQueue.TryEnqueue(() =>
                {
                    if (_state.VoiceState.Kind == VoiceInputStateKind.Idle)
                    {
                        _state.TranscriptPreview = string.Empty;
                        _onStateChanged();
                    }
                });
            });
        }
        _state.HasAccessibilityPermission = ClipboardPasteService.CheckPasteCapability();

        _isProcessing = false;
        UpdateUi();
    }

    private SpeechTranscribing CreateExternalProviderTranscriber()
    {
        var config = TranscriptionProviderStore.Shared.LoadConfig();
        var key = SecureSecretStore.Load(config.ApiKeyReference ?? "flotis.externalprovider.apikey");
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("请先配置外部转写提供商的 API Key。");
        }

        return new OpenAICompatibleTranscriber(config, key);
    }

    public void Cancel()
    {
        if (_activeTranscriber != null)
        {
            _activeTranscriber.Cancel();
            _activeTranscriber = null;
        }

        _state.VoiceState.Set(VoiceInputStateKind.Idle);
        UpdateUi();
    }

    private void UpdateUi()
    {
        _uiQueue.TryEnqueue(() =>
        {
            _onStateChanged();
        });
    }
}
