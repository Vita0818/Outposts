using System.Collections.ObjectModel;
using Intatis.App.Services;
using Intatis.Core;
using Intatis.Core.Permission;
using Intatis.Core.Protocol;
using Intatis.Core.Session;
using Intatis.Core.Tools;
using Microsoft.UI.Dispatching;

namespace Intatis.App.ViewModels;

public enum CodeItemKind
{
    User,
    Assistant,
    ToolCall,
    ToolResult,
    Permission,
    Error,
}

public sealed class CodeItemVm : ObservableBase
{
    private string _detail = "";

    public CodeItemKind Kind { get; init; }
    public string Title { get; init; } = "";
    public bool IsMono => Kind is CodeItemKind.ToolCall or CodeItemKind.ToolResult
        or CodeItemKind.Permission or CodeItemKind.Error;

    public string Detail
    {
        get => _detail;
        set => Set(ref _detail, value);
    }

    public string KindCaption => Kind switch
    {
        CodeItemKind.User => "YOU",
        CodeItemKind.Assistant => "CODER",
        CodeItemKind.ToolCall => "TOOL CALL",
        CodeItemKind.ToolResult => "TOOL RESULT",
        CodeItemKind.Permission => "PERMISSION",
        _ => "ERROR",
    };
}

public sealed class PermissionCardVm : ObservableBase
{
    public required string RequestId { get; init; }
    public required string Tool { get; init; }
    public required string Args { get; init; }
    public string Risk { get; init; } = "medium";
    public string Reason { get; init; } = "";
    public string Agent { get; init; } = "";
}

/// <summary>Code surface: single-workspace agent with tools and a permission card.</summary>
public sealed class CodeViewModel : ObservableBase
{
    private readonly AppEnvironment _environment;
    private readonly DispatcherQueue _dispatcher;
    private readonly GuiPermissionResponder _responder;
    private EventLog? _log;
    private CancellationTokenSource? _cancellation;
    private string _input = "";
    private bool _isWorking;
    private string _agentState = "idle";
    private string _workspace = "";
    private string _errorText = "";
    private string _selectedModel = "";
    private PermissionCardVm? _pendingPermission;
    private Action<PermissionDecision>? _pendingSettle;

    public ObservableCollection<CodeItemVm> Items { get; } = [];
    public ObservableCollection<string> ModelOptions { get; } = [];

    public string Input
    {
        get => _input;
        set => Set(ref _input, value);
    }

    public bool IsWorking
    {
        get => _isWorking;
        private set => Set(ref _isWorking, value);
    }

    public string AgentState
    {
        get => _agentState;
        private set => Set(ref _agentState, value);
    }

    public string Workspace
    {
        get => _workspace;
        private set => Set(ref _workspace, value);
    }

    public string WorkspaceName => Workspace.Length > 0
        ? Path.GetFileName(Workspace.TrimEnd(Path.DirectorySeparatorChar))
        : "no workspace";

    public string ErrorText
    {
        get => _errorText;
        set => Set(ref _errorText, value);
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set => Set(ref _selectedModel, value);
    }

    public PermissionCardVm? PendingPermission
    {
        get => _pendingPermission;
        private set => Set(ref _pendingPermission, value);
    }

    public CodeViewModel(AppEnvironment environment, DispatcherQueue dispatcher)
    {
        _environment = environment;
        _dispatcher = dispatcher;
        _responder = new GuiPermissionResponder(dispatcher,
            (card, settle) => PresentPermission(card, settle));
        var (reference, _, fallbackModel) = environment.DefaultInference();
        _selectedModel = reference?.ModelId ?? fallbackModel;
        RefreshModelOptions();
        StartNewSession();
    }

    public void RefreshModelOptions()
    {
        ModelOptions.Clear();
        foreach (var model in _environment.Config.InferenceModels())
            ModelOptions.Add(model.Id);
        if (ModelOptions.Count > 0 && !ModelOptions.Contains(SelectedModel))
            SelectedModel = ModelOptions[0];
    }

    public void SetWorkspace(string path)
    {
        Workspace = path;
        Raise(nameof(WorkspaceName));
    }

    public void StartNewSession()
    {
        DetachLog();
        Items.Clear();
        ErrorText = "";
        var session = SessionId.New(SessionKind.Code);
        _log = EventLog.Open(session, _environment.SessionFileFor(session));
        _log.EnvelopeAppended += OnEnvelope;
        SessionProjectionStore.UpdateDisplayName(_log, SessionKind.Code,
            WorkspaceName != "no workspace" ? $"Code · {WorkspaceName}" : "New Code Session", changeKind: "created");
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

    public async void Send()
    {
        var text = Input.Trim();
        if (text.Length == 0 || IsWorking) return;
        if (Workspace.Length == 0 || !Directory.Exists(Workspace))
        {
            ErrorText = "choose a workspace first";
            return;
        }
        Input = "";
        ErrorText = "";

        var (reference, fallbackProvider, fallbackModel) = _environment.DefaultInference();
        var providerId = reference?.ProviderId ?? fallbackProvider;
        var model = SelectedModel.Length > 0 ? SelectedModel : (reference?.ModelId ?? fallbackModel);

        IsWorking = true;
        AgentState = "thinking";
        _cancellation = new CancellationTokenSource();
        try
        {
            var provider = _environment.Providers.ChatProviderFor(providerId);
            ModelPermissionReviewer? reviewer = null;
            if (_environment.Config.Reviewer is { } reviewerRef)
            {
                try
                {
                    reviewer = new ModelPermissionReviewer(
                        _environment.Providers.ChatProviderFor(reviewerRef.ProviderId),
                        reviewerRef.ModelId);
                }
                catch (Exception) { /* fail closed */ }
            }
            var permissions = new PermissionEngine(reviewer, _responder);
            var loop = new AgentLoop(_log!, provider, ToolRegistry.Standard(), permissions,
                Workspace, "Coder", model, AgentPrompts.CodePrompt(Workspace));
            await Task.Run(() => loop.SendAsync(text, _cancellation.Token));
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
            IsWorking = false;
            AgentState = "idle";
        }
    }

    private void PresentPermission(PermissionRequestCard card, Action<PermissionDecision> settle)
    {
        PendingPermission = new PermissionCardVm
        {
            RequestId = card.RequestId,
            Tool = card.Tool,
            Args = card.Args,
            Risk = card.Risk.ToWire(),
            Reason = card.Reason,
            Agent = card.Agent ?? "Coder",
        };
        _pendingSettle = settle;
    }

    public void SettlePermission(bool allow)
    {
        PendingPermission = null;
        _pendingSettle?.Invoke(allow ? PermissionDecision.Allow : PermissionDecision.Deny);
        _pendingSettle = null;
    }

    private void OnEnvelope(Envelope envelope)
    {
        _dispatcher.TryEnqueue(() =>
        {
            switch (envelope.Type)
            {
                case EventType.UserMessage:
                    Items.Add(new CodeItemVm
                    {
                        Kind = CodeItemKind.User,
                        Detail = (string?)envelope.Payload?["text"] ?? "",
                    });
                    break;

                case EventType.MessageDelta:
                    var deltaId = (string?)envelope.Payload?["message_id"] ?? "";
                    var deltaText = (string?)envelope.Payload?["text_delta"] ?? "";
                    if (Items.LastOrDefault(i => i.Kind == CodeItemKind.Assistant
                        && i.Title == deltaId) is not { } open)
                    {
                        open = new CodeItemVm { Kind = CodeItemKind.Assistant, Title = deltaId };
                        Items.Add(open);
                    }
                    open.Detail += deltaText;
                    break;

                case EventType.MessageCompleted:
                    var completedId = (string?)envelope.Payload?["message_id"] ?? "";
                    var completedText = (string?)envelope.Payload?["text"] ?? "";
                    if (Items.LastOrDefault(i => i.Kind == CodeItemKind.Assistant
                        && i.Title == completedId) is { } closing)
                    {
                        closing.Detail = completedText;
                    }
                    else
                    {
                        Items.Add(new CodeItemVm
                        {
                            Kind = CodeItemKind.Assistant,
                            Title = completedId,
                            Detail = completedText,
                        });
                    }
                    break;

                case EventType.ToolCall:
                    Items.Add(new CodeItemVm
                    {
                        Kind = CodeItemKind.ToolCall,
                        Title = (string?)envelope.Payload?["name"] ?? "",
                        Detail = (string?)envelope.Payload?["args"] ?? "",
                    });
                    break;

                case EventType.ToolResult:
                    Items.Add(new CodeItemVm
                    {
                        Kind = CodeItemKind.ToolResult,
                        Detail = (string?)envelope.Payload?["observation"] ?? "",
                    });
                    break;

                case EventType.PermissionRequest:
                    Items.Add(new CodeItemVm
                    {
                        Kind = CodeItemKind.Permission,
                        Title = (string?)envelope.Payload?["tool"] ?? "",
                        Detail = $"{(string?)envelope.Payload?["risk"] ?? "medium"} risk · {(string?)envelope.Payload?["reason"] ?? ""}",
                    });
                    break;

                case EventType.PermissionResolved:
                    Items.Add(new CodeItemVm
                    {
                        Kind = CodeItemKind.Permission,
                        Title = (string?)envelope.Payload?["tool"] ?? "",
                        Detail = $"decision: {(string?)envelope.Payload?["decision"] ?? "?"} · {(string?)envelope.Payload?["reason"] ?? ""}",
                    });
                    break;

                case EventType.AgentStatus:
                    AgentState = (string?)envelope.Payload?["state"] ?? "idle";
                    break;

                case EventType.Error:
                    ErrorText = (string?)envelope.Payload?["message"] ?? "error";
                    break;
            }
        });
    }
}
