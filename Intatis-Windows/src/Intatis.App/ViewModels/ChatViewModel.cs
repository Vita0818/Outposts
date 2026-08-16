using System.Collections.ObjectModel;
using Intatis.App.Services;
using Intatis.Core;
using Intatis.Core.Protocol;
using Intatis.Core.Providers;
using Intatis.Core.Session;
using Microsoft.UI.Dispatching;

namespace Intatis.App.ViewModels;

public sealed class CitationVm
{
    public string Url { get; init; } = "";
    public string Host { get; init; } = "";

    public static CitationVm From(MessageCitation citation)
    {
        var host = citation.Url;
        try
        {
            if (Uri.TryCreate(citation.Url, UriKind.Absolute, out var uri))
                host = uri.Host + (uri.AbsolutePath.Length > 1 ? uri.AbsolutePath[..Math.Min(24, uri.AbsolutePath.Length)] : "");
        }
        catch (Exception) { }
        return new CitationVm { Url = citation.Url, Host = host };
    }
}

public sealed class ChatMessageItemVm : ObservableBase
{
    private string _text = "";
    private bool _isComplete;

    public bool IsUser { get; init; }
    public bool IsError { get; init; }
    public string Caption { get; init; } = "";
    public string? Agent { get; init; }
    public string Timestamp { get; init; } = "";
    public int AttachmentCount { get; init; }
    public string MessageId { get; init; } = "";
    public ObservableCollection<CitationVm> Citations { get; init; } = [];

    public string Text
    {
        get => _text;
        set => Set(ref _text, value);
    }

    public bool IsComplete
    {
        get => _isComplete;
        set => Set(ref _isComplete, value);
    }

    public bool HasCitations => Citations.Count > 0;
    public bool HasAttachments => AttachmentCount > 0;

    public void RaiseCitationsChanged() => Raise(nameof(HasCitations));

    public static ChatMessageItemVm FromView(ChatMessageView view) => new()
    {
        IsUser = view.Role == MessageRoleWire.User,
        IsError = view.Role == MessageRoleWire.System,
        Caption = view.Role switch
        {
            MessageRoleWire.User => "You",
            MessageRoleWire.Agent => view.Agent ?? "Agent",
            MessageRoleWire.System => "System",
            _ => "Intatis",
        },
        Agent = view.Agent,
        Timestamp = view.Timestamp.ToLocalTime().ToString("HH:mm"),
        AttachmentCount = view.AttachmentCount,
        MessageId = view.MessageId,
        Text = view.Text,
        IsComplete = view.IsComplete,
        Citations = new ObservableCollection<CitationVm>(
            view.Citations.Select(CitationVm.From)),
    };
}

/// <summary>Chat surface: tool-free streaming conversation over one EventLog session.</summary>
public sealed class ChatViewModel : ObservableBase
{
    private readonly AppEnvironment _environment;
    private readonly DispatcherQueue _dispatcher;
    private EventLog? _log;
    private CancellationTokenSource? _cancellation;
    private string _input = "";
    private bool _isStreaming;
    private string _errorText = "";
    private string _usageText = "";
    private string _sessionTitle = "New Chat";
    private string _selectedModel = "";

    public ObservableCollection<ChatMessageItemVm> Messages { get; } = [];
    public ObservableCollection<string> ModelOptions { get; } = [];

    public string Input
    {
        get => _input;
        set => Set(ref _input, value);
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        private set => Set(ref _isStreaming, value);
    }

    public string ErrorText
    {
        get => _errorText;
        set => Set(ref _errorText, value);
    }

    public string UsageText
    {
        get => _usageText;
        set => Set(ref _usageText, value);
    }

    public string SessionTitle
    {
        get => _sessionTitle;
        private set => Set(ref _sessionTitle, value);
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set => Set(ref _selectedModel, value);
    }

    public ChatViewModel(AppEnvironment environment, DispatcherQueue dispatcher)
    {
        _environment = environment;
        _dispatcher = dispatcher;
        var (reference, _, fallbackModel) = environment.DefaultInference();
        _selectedModel = reference?.ModelId ?? fallbackModel;
        RefreshModelOptions();
        StartNewSession();
    }

    public void RefreshModelOptions()
    {
        ModelOptions.Clear();
        foreach (var model in _environment.Config.InferenceModels())
            ModelOptions.Add(model.DisplayName.Length > 0 ? $"{model.Id}" : model.Id);
        if (ModelOptions.Count > 0 && !ModelOptions.Contains(SelectedModel))
            SelectedModel = ModelOptions[0];
    }

    public void StartNewSession()
    {
        DetachLog();
        Messages.Clear();
        ErrorText = "";
        UsageText = "";
        var session = SessionId.New(SessionKind.Chat);
        _log = EventLog.Open(session, _environment.SessionFileFor(session));
        _log.EnvelopeAppended += OnEnvelope;
        SessionTitle = "New Chat";
        SessionProjectionStore.UpdateDisplayName(_log, SessionKind.Chat, "New Chat", changeKind: "created");
    }

    public void OpenSession(SessionSummary summary)
    {
        DetachLog();
        Messages.Clear();
        var session = new SessionId(summary.Id);
        try
        {
            _log = EventLog.Open(session, _environment.SessionFileFor(session));
        }
        catch (EventLogException)
        {
            ErrorText = "session is owned by another runtime";
            return;
        }
        _log.EnvelopeAppended += OnEnvelope;
        foreach (var view in ConversationProjection.Build(_log.Replay()))
            Messages.Add(ChatMessageItemVm.FromView(view));
        SessionTitle = summary.DisplayName ?? summary.Id;
    }

    private void DetachLog()
    {
        if (_log is null) return;
        _log.EnvelopeAppended -= OnEnvelope;
        _log.Dispose();
        _log = null;
    }

    public void Stop()
    {
        _cancellation?.Cancel();
    }

    public async void Send(string? attachedText = null)
    {
        var text = (attachedText ?? Input).Trim();
        if (text.Length == 0 || IsStreaming || _log is null) return;
        Input = "";
        ErrorText = "";

        var (reference, fallbackProvider, fallbackModel) = _environment.DefaultInference();
        var providerId = reference?.ProviderId ?? fallbackProvider;
        var model = SelectedModel.Length > 0 ? SelectedModel : (reference?.ModelId ?? fallbackModel);

        IsStreaming = true;
        _cancellation = new CancellationTokenSource();
        try
        {
            var provider = _environment.Providers.ChatProviderFor(providerId);
            var loop = new ChatLoop(_log, provider, model,
                systemPrompt: "You are Intatis, a concise local AI assistant.",
                includeUsage: true);
            await Task.Run(() => loop.SendAsync(text, null, _cancellation.Token));
        }
        catch (OperationCanceledException)
        {
            ErrorText = "stopped";
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
        finally
        {
            IsStreaming = false;
        }
    }

    private void OnEnvelope(Envelope envelope)
    {
        // UI thread hop: the log is written on a background task.
        _dispatcher.TryEnqueue(() =>
        {
            switch (envelope.Type)
            {
                case EventType.UserMessage:
                    var user = UserMessagePayload.FromJson(envelope.Payload);
                    Messages.Add(new ChatMessageItemVm
                    {
                        IsUser = true,
                        Caption = "You",
                        Timestamp = envelope.Ts.ToLocalTime().ToString("HH:mm"),
                        AttachmentCount = user.Attachments?.Count ?? 0,
                        Text = user.Text,
                        IsComplete = true,
                    });
                    break;

                case EventType.MessageDelta:
                    var deltaId = (string?)envelope.Payload?["message_id"] ?? "";
                    var deltaText = (string?)envelope.Payload?["text_delta"] ?? "";
                    if (Messages.LastOrDefault(m => m.MessageId == deltaId) is not { } open)
                    {
                        open = new ChatMessageItemVm
                        {
                            Caption = "Intatis",
                            MessageId = deltaId,
                            Timestamp = envelope.Ts.ToLocalTime().ToString("HH:mm"),
                        };
                        Messages.Add(open);
                    }
                    open.Text += deltaText;
                    break;

                case EventType.MessageCompleted:
                    var completedId = (string?)envelope.Payload?["message_id"] ?? "";
                    var completedText = (string?)envelope.Payload?["text"] ?? "";
                    if (Messages.LastOrDefault(m => m.MessageId == completedId) is { } closing)
                    {
                        closing.Text = completedText;
                        closing.IsComplete = true;
                    }
                    else
                    {
                        Messages.Add(new ChatMessageItemVm
                        {
                            Caption = "Intatis",
                            MessageId = completedId,
                            Timestamp = envelope.Ts.ToLocalTime().ToString("HH:mm"),
                            Text = completedText,
                            IsComplete = true,
                        });
                    }
                    break;

                case EventType.Error:
                    var code = (string?)envelope.Payload?["code"] ?? "error";
                    var message = (string?)envelope.Payload?["message"] ?? "";
                    ErrorText = $"[{code}] {message}";
                    break;

                case EventType.TurnStats:
                    var total = (int?)envelope.Payload?["total_tokens"];
                    if (total is > 0)
                        UsageText = $"{total} tokens";
                    break;
            }
        });
    }
}
