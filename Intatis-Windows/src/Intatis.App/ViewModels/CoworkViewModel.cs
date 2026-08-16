using System.Collections.ObjectModel;
using Intatis.App.Services;
using Intatis.Core;
using Intatis.Core.Cowork;
using Intatis.Core.Permission;
using Intatis.Core.Protocol;
using Microsoft.UI.Dispatching;

namespace Intatis.App.ViewModels;

public sealed class AgentRowVm : ObservableBase
{
    private string _status = "idle";

    public required string Name { get; init; }
    public string Model { get; init; } = "";
    public string Role { get; init; } = "";
    public string Workspace { get; init; } = "";
    public bool BindingResolved { get; init; } = true;

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string Glyph => Role == "coordinator" ? "\uE713" : "\uE77B";
    public string StatusGlyph => Status switch
    {
        "thinking" => "\uE9F9",
        "tool" => "\uE90F",
        _ => "\uE73E",
    };
}

/// <summary>Cowork surface: roster rail + selected agent thread + composer.</summary>
public sealed class CoworkViewModel : ObservableBase
{
    private readonly AppEnvironment _environment;
    private readonly DispatcherQueue _dispatcher;
    private readonly GuiPermissionResponder _responder;
    private Orchestrator? _orchestrator;
    private CancellationTokenSource? _cancellation;
    private string _input = "";
    private bool _isWorking;
    private string _workspace = "";
    private string _errorText = "";
    private string _reviewerLabel = "";
    private AgentRowVm? _selectedAgent;

    public ObservableCollection<AgentRowVm> Agents { get; } = [];
    public ObservableCollection<ChatMessageItemVm> Thread { get; } = [];
    public ObservableCollection<string> TaskLines { get; } = [];

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

    public string Workspace
    {
        get => _workspace;
        private set => Set(ref _workspace, value);
    }

    public string ErrorText
    {
        get => _errorText;
        set => Set(ref _errorText, value);
    }

    public string ReviewerLabel
    {
        get => _reviewerLabel;
        private set => Set(ref _reviewerLabel, value);
    }

    public AgentRowVm? SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (Set(ref _selectedAgent, value)) RebuildThread();
        }
    }

    public PermissionCardVm? PendingPermission => CodePending;
    private PermissionCardVm? CodePending { get; set; }

    private Action<bool>? _settle;

    public CoworkViewModel(AppEnvironment environment, DispatcherQueue dispatcher)
    {
        _environment = environment;
        _dispatcher = dispatcher;
        _responder = new GuiPermissionResponder(dispatcher, (card, settle) =>
            _dispatcher.TryEnqueue(() => PresentPermission(card, settle)));
    }

    public void StartNewSession(string workspace)
    {
        Workspace = workspace;
        _orchestrator?.Dispose();
        Agents.Clear();
        Thread.Clear();
        TaskLines.Clear();

        var (reference, fallbackProvider, fallbackModel) = _environment.DefaultInference();
        var providerId = reference?.ProviderId ?? fallbackProvider;
        var modelId = reference?.ModelId ?? fallbackModel;

        AgentInferenceBinding? reviewerBinding = null;
        if (_environment.Config.Reviewer is { } reviewer)
            reviewerBinding = new AgentInferenceBinding
            {
                ProviderId = reviewer.ProviderId,
                ModelId = reviewer.ModelId,
            };

        var session = SessionId.New(SessionKind.Cowork);
        var log = EventLog.Open(session, _environment.SessionFileFor(session));
        log.EnvelopeAppended += OnEnvelope;
        try
        {
            _orchestrator = Orchestrator.BootstrapFreshSession(
                log, _environment.Providers, _responder, workspace,
                new AgentInferenceBinding { ProviderId = providerId, ModelId = modelId },
                reviewerBinding);
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            return;
        }

        ReviewerLabel = _orchestrator.ReviewerModel is { Length: > 0 } reviewerModel
            ? $"reviewer · {reviewerModel}"
            : _orchestrator.ReviewerFailClosed ? "reviewer · fail closed" : "reviewer · manual";
        RefreshRoster();
        SelectedAgent = Agents.FirstOrDefault();
    }

    public void RefreshRoster()
    {
        if (_orchestrator is null) return;
        Agents.Clear();
        foreach (var agent in _orchestrator.Registry.All())
        {
            Agents.Add(new AgentRowVm
            {
                Name = agent.Name,
                Model = agent.InferenceBinding?.DisplayLabel ?? agent.Model,
                Role = agent.Role,
                Workspace = agent.WorkspaceRoot,
            });
        }
        if (SelectedAgent is null || Agents.All(a => a.Name != SelectedAgent.Name))
            SelectedAgent = Agents.FirstOrDefault();
        Raise(nameof(SelectedAgent));
    }

    public bool AddAgent(string name, string workspace)
    {
        if (_orchestrator is null || name.Trim().Length == 0) return false;
        var (reference, fallbackProvider, fallbackModel) = _environment.DefaultInference();
        try
        {
            _orchestrator.Attach(name.Trim(),
                Directory.Exists(workspace) ? workspace : Workspace,
                new AgentInferenceBinding
                {
                    ProviderId = reference?.ProviderId ?? fallbackProvider,
                    ModelId = reference?.ModelId ?? fallbackModel,
                });
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        RefreshRoster();
        return true;
    }

    public void RemoveAgent(string name)
    {
        if (_orchestrator?.Detach(name) == true)
            RefreshRoster();
    }

    public void Stop()
    {
        _cancellation?.Cancel();
    }

    public async void Send()
    {
        var text = Input.Trim();
        if (text.Length == 0 || IsWorking || _orchestrator is null) return;
        Input = "";
        ErrorText = "";
        IsWorking = true;
        _cancellation = new CancellationTokenSource();
        try
        {
            await Task.Run(() => _orchestrator.SendAsync(text, _cancellation.Token));
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
            RefreshTasks();
        }
    }

    private void RefreshTasks()
    {
        if (_orchestrator is null) return;
        _dispatcher.TryEnqueue(() =>
        {
            TaskLines.Clear();
            foreach (var record in _orchestrator.Scheduler.Records().Take(30))
            {
                TaskLines.Add($"{record.TaskId} @{record.Assignee} · {record.Status}" +
                    (record.Error is { Length: > 0 } ? $" · {record.Error}" : ""));
            }
        });
    }

    private void RebuildThread()
    {
        Thread.Clear();
        if (_orchestrator is null || SelectedAgent is null) return;
        var agent = SelectedAgent.Name;
        foreach (var view in ConversationProjection.Build(_orchestrator.Log.Replay()))
        {
            if (agent != "main" && view.Agent != agent && view.Role != MessageRoleWire.User) continue;
            if (agent == "main" && view.Agent is not (null or "main")) continue;
            Thread.Add(ChatMessageItemVm.FromView(view));
        }
    }

    private void PresentPermission(PermissionRequestCard card, Action<PermissionDecision> settle)
    {
        CodePending = new PermissionCardVm
        {
            RequestId = card.RequestId,
            Tool = card.Tool,
            Args = card.Args,
            Risk = card.Risk.ToWire(),
            Reason = card.Reason,
            Agent = card.Agent ?? "agent",
        };
        _settle = allow => settle(allow ? PermissionDecision.Allow : PermissionDecision.Deny);
        Raise(nameof(PendingPermission));
    }

    public void SettlePermission(bool allow)
    {
        CodePending = null;
        Raise(nameof(PendingPermission));
        _settle?.Invoke(allow);
        _settle = null;
    }

    private void OnEnvelope(Envelope envelope)
    {
        _dispatcher.TryEnqueue(() =>
        {
            switch (envelope.Type)
            {
                case EventType.AgentAttached:
                case EventType.AgentDetached:
                    RefreshRoster();
                    break;

                case EventType.AgentStatus:
                    var agentName = (string?)envelope.Payload?["agent"] ?? "";
                    var state = (string?)envelope.Payload?["state"] ?? "idle";
                    foreach (var row in Agents.Where(a => a.Name == agentName))
                        row.Status = state;
                    break;

                case EventType.TaskCreated:
                case EventType.TaskStarted:
                case EventType.TaskCompleted:
                case EventType.TaskFailed:
                    RefreshTasks();
                    break;
            }

            // Live thread append: fold the newest chat events for the selected agent.
            if (SelectedAgent is { } selected && envelope.Type
                    is EventType.MessageDelta or EventType.MessageCompleted or EventType.UserMessage)
            {
                AppendLive(selected.Name, envelope);
            }
        });
    }

    private void AppendLive(string selected, Envelope envelope)
    {
        switch (envelope.Type)
        {
            case EventType.UserMessage:
                Thread.Add(ChatMessageItemVm.FromView(new ChatMessageView
                {
                    MessageId = "u" + envelope.Seq,
                    Role = MessageRoleWire.User,
                    Text = (string?)envelope.Payload?["text"] ?? "",
                    IsComplete = true,
                    Timestamp = envelope.Ts,
                }));
                break;

            case EventType.MessageDelta:
                var deltaId = (string?)envelope.Payload?["message_id"] ?? "";
                var deltaAgent = (string?)envelope.Payload?["agent"];
                if (deltaAgent != selected && selected != "main") break;
                if (selected != "main" && deltaAgent is null) break;
                var deltaText = (string?)envelope.Payload?["text_delta"] ?? "";
                if (Thread.LastOrDefault(m => m.MessageId == deltaId) is not { } open)
                {
                    open = new ChatMessageItemVm
                    {
                        Caption = "@" + (deltaAgent ?? "main"),
                        Agent = deltaAgent,
                        MessageId = deltaId,
                        Timestamp = envelope.Ts.ToLocalTime().ToString("HH:mm"),
                    };
                    Thread.Add(open);
                }
                open.Text += deltaText;
                break;

            case EventType.MessageCompleted:
                var completedId = (string?)envelope.Payload?["message_id"] ?? "";
                var completedText = (string?)envelope.Payload?["text"] ?? "";
                if (Thread.LastOrDefault(m => m.MessageId == completedId) is { } closing)
                    closing.Text = completedText;
                break;
        }
    }
}
