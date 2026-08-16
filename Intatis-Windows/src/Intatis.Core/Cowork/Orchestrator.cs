using System.Text.Json.Nodes;
using Intatis.Core.Permission;
using Intatis.Core.Protocol;
using Intatis.Core.Providers;
using Intatis.Core.Session;
using Intatis.Core.Tools;

namespace Intatis.Core.Cowork;

/// <summary>
/// Single coordination point for a Cowork session: roster, FIFO scheduler pump,
/// per-agent frozen inference bindings, mediated message bus, and the independent
/// permission-reviewer binding. Agent loops always run in their own scheduled task —
/// cowork never calls into another agent's loop synchronously.
/// </summary>
public sealed class Orchestrator : IDisposable
{
    private readonly EventLog _log;
    private readonly ProviderRegistry _providers;
    private readonly AgentRegistry _registry = new();
    private readonly AgentScheduler _scheduler = new();
    private readonly MessageBus _bus;
    private readonly Mediator _mediator = new();
    private readonly ToolRegistry _tools = ToolRegistry.Standard();
    private readonly IPermissionResponder _responder;
    private readonly ModelPermissionReviewer? _reviewer;
    private readonly Dictionary<TaskId, TaskCompletionSource<string>> _resultWaiters = new();
    private readonly SemaphoreSlim _pumpLock = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();

    public EventLog Log => _log;
    public AgentRegistry Registry => _registry;
    public AgentScheduler Scheduler => _scheduler;
    public MessageBus Bus => _bus;
    public string? ReviewerModel { get; }
    public bool ReviewerFailClosed { get; }

    public Orchestrator(
        EventLog log,
        ProviderRegistry providers,
        IPermissionResponder responder,
        AgentInferenceBinding? reviewerBinding)
    {
        _log = log;
        _providers = providers;
        _responder = responder;
        _bus = new MessageBus(log, _mediator, _scheduler);

        if (reviewerBinding is { } binding)
        {
            try
            {
                var provider = providers.ChatProviderFor(binding.ProviderId);
                _reviewer = new ModelPermissionReviewer(provider, binding.ModelId);
                ReviewerModel = binding.ModelId;
            }
            catch (Exception)
            {
                ReviewerFailClosed = true;
            }
        }
    }

    public static Orchestrator BootstrapFreshSession(
        EventLog log,
        ProviderRegistry providers,
        IPermissionResponder responder,
        string mainWorkspace,
        AgentInferenceBinding mainBinding,
        AgentInferenceBinding? reviewerBinding)
    {
        var orchestrator = new Orchestrator(log, providers, responder, reviewerBinding);

        var main = new Agent
        {
            Name = Agent.MainAgentId,
            WorkspaceRoot = mainWorkspace,
            Model = mainBinding.ModelId,
            InferenceBinding = mainBinding,
            Profile = PermissionProfile.Reviewed,
            Role = "coordinator",
            CoordinationDepth = 2,
        };
        orchestrator._registry.Upsert(main);
        orchestrator._log.Append(EventType.AgentAttached, new AgentAttachedPayload
        {
            Agent = main.Name,
            Model = main.Model,
            Workspace = mainWorkspace,
            PermissionProfile = main.Profile.ToWire(),
            Role = main.Role,
        }.ToJson());

        if (orchestrator._reviewer is not null)
        {
            orchestrator._log.Append(EventType.AgentAttached, new AgentAttachedPayload
            {
                Agent = Agent.PermissionReviewerId,
                Model = orchestrator._reviewer.Model,
                PermissionProfile = PermissionProfile.ReadOnly.ToWire(),
                Role = "reviewer",
            }.ToJson());
        }
        return orchestrator;
    }

    public Agent Attach(string name, string workspaceRoot, AgentInferenceBinding binding, string role = "worker")
    {
        var agent = new Agent
        {
            Name = name,
            WorkspaceRoot = workspaceRoot,
            Model = binding.ModelId,
            InferenceBinding = binding,
            Role = role,
        };
        _registry.Add(agent);
        _log.Append(EventType.AgentAttached, new AgentAttachedPayload
        {
            Agent = name,
            Model = binding.ModelId,
            Workspace = workspaceRoot,
            PermissionProfile = agent.Profile.ToWire(),
            Role = role,
        }.ToJson());
        return agent;
    }

    public bool Detach(string name, string reason = "removed by user")
    {
        if (name == Agent.MainAgentId) return false;
        var removed = _registry.Remove(name);
        if (removed)
            _log.Append(EventType.AgentDetached, new JsonObject { ["agent"] = name, ["reason"] = reason });
        return removed;
    }

    /// <summary>
    /// Sends a user turn: routes "@name text" to a named agent, everything else to
    /// @main. Pumps the scheduler until idle and returns the terminal agent's text.
    /// </summary>
    public async Task<string> SendAsync(string userText, CancellationToken ct = default)
    {
        var assignee = Agent.MainAgentId;
        var body = userText;
        if (userText.StartsWith('@'))
        {
            var space = userText.IndexOf(' ');
            if (space > 1)
            {
                var target = userText[1..space].Trim();
                if (_registry.AgentNamed(target) is not null)
                {
                    assignee = target;
                    body = userText[(space + 1)..].Trim();
                }
            }
        }

        _log.Append(EventType.UserMessage, new UserMessagePayload { Text = userText }.ToJson());
        var taskId = TaskId.New();
        var task = new ScheduledTask
        {
            Id = taskId,
            Assignee = assignee,
            Kind = "root",
            Objective = body,
            Input = body,
            ReplyMode = "task_report",
        };
        _scheduler.Enqueue(task);
        _log.Append(EventType.TaskCreated, new TaskLifecyclePayload
        {
            TaskId = taskId, Agent = assignee, Objective = body,
        }.ToJson());

        await PumpUntilIdleAsync(ct).ConfigureAwait(false);
        var record = _scheduler.Records().FirstOrDefault(r => r.TaskId == taskId);
        return record?.Status switch
        {
            "completed" => record.Result ?? "",
            "failed" => $"[task failed] {record.Error}",
            "cancelled" => "[cancelled]",
            _ => "[no result]",
        };
    }

    /// <summary>Delegates work to another agent and awaits its mediated answer.</summary>
    public async Task<string> AskAgentAsync(string from, string to, string question, CancellationToken ct = default)
    {
        if (_registry.AgentNamed(to) is null)
            throw new InvalidOperationException($"unknown agent '{to}'");
        if (to == from)
            throw new InvalidOperationException("an agent cannot ask itself");
        if (to == Agent.PermissionReviewerId)
            throw new InvalidOperationException("the permission reviewer is reserved");

        var taskId = TaskId.New();
        var waiter = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _resultWaiters[taskId] = waiter;

        var delivered = _bus.Deliver(from, to, question, taskId);
        if (delivered is null)
            return "[blocked by mediator]";

        _scheduler.Enqueue(new ScheduledTask
        {
            Id = taskId,
            Assignee = to,
            Kind = "agent_invocation",
            Objective = question,
            Input = delivered,
            ParentTaskId = null,
            ReplyMode = "answer",
        });
        _log.Append(EventType.TaskCreated, new TaskLifecyclePayload
        {
            TaskId = taskId, Agent = to, Objective = question,
        }.ToJson());

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _shutdown.Token);
        await PumpUntilIdleAsync(linked.Token).ConfigureAwait(false);
        _resultWaiters.Remove(taskId, out _);
        return waiter.Task.IsCompleted ? await waiter.Task.ConfigureAwait(false) : "[no answer]";
    }

    /// <summary>
    /// Pumps the FIFO scheduler: claims tasks for idle agents, runs each agent's
    /// loop as an independent task, and settles durable lifecycle events.
    /// </summary>
    public async Task PumpUntilIdleAsync(CancellationToken ct = default)
    {
        await _pumpLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var running = new List<Task>();
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                running.RemoveAll(t => t.IsCompleted);
                var claimed = _scheduler.ClaimNext();
                if (claimed is null)
                {
                    if (running.Count == 0) break;
                    await Task.WhenAny(running).ConfigureAwait(false);
                    continue;
                }
                running.Add(RunClaimedAsync(claimed, ct));
            }
            await Task.WhenAll(running).ConfigureAwait(false);
        }
        finally
        {
            _pumpLock.Release();
        }
    }

    private async Task RunClaimedAsync(ScheduledTask task, CancellationToken ct)
    {
        var agent = _registry.AgentNamed(task.Assignee);
        if (agent is null)
        {
            _scheduler.RecordFailed(task, $"agent '{task.Assignee}' is not attached");
            _log.Append(EventType.TaskFailed, new TaskLifecyclePayload
            {
                TaskId = task.Id, Agent = task.Assignee, Error = "agent not attached",
            }.ToJson());
            return;
        }

        // Revalidate the frozen binding before any provider work.
        if (agent.InferenceBinding is null)
        {
            _scheduler.RecordFailed(task, "configurationUnresolved: no inference binding");
            _log.Append(EventType.TaskFailed, new TaskLifecyclePayload
            {
                TaskId = task.Id, Agent = task.Assignee, Error = "no inference binding",
            }.ToJson());
            return;
        }

        _scheduler.RecordStarted(task);
        _log.Append(EventType.TaskStarted, new TaskLifecyclePayload
        {
            TaskId = task.Id, Agent = task.Assignee, Objective = task.Objective,
        }.ToJson());

        try
        {
            var input = task.Input;
            var mailboxNote = ConsumeMailboxNote(task.Assignee);
            if (mailboxNote is { Length: > 0 })
                input = $"{input}\n\n[inbox]\n{mailboxNote}";

            var provider = _providers.ChatProviderFor(agent.InferenceBinding.ProviderId);
            var permissions = new PermissionEngine(_reviewer, _responder);
            var systemPrompt = agent.Role == "coordinator"
                ? AgentPrompts.CoworkCoordinatorPrompt(agent.WorkspaceRoot, _registry.RosterLine())
                : AgentPrompts.CoworkWorkerPrompt(agent.Name, agent.WorkspaceRoot);
            var loop = new AgentLoop(
                _log, provider, _tools, permissions, agent.WorkspaceRoot,
                agent.Name, agent.Model, systemPrompt, maxIterations: 64);

            var result = await loop.SendAsync(input, ct).ConfigureAwait(false);
            _scheduler.RecordCompleted(task, result);
            _log.Append(EventType.TaskCompleted, new TaskLifecyclePayload
            {
                TaskId = task.Id, Agent = task.Assignee, Result = result is { Length: > 2000 } ? result[..2000] : result,
            }.ToJson());

            if (_resultWaiters.TryGetValue(task.Id, out var waiter))
            {
                var mediated = _bus.Reply(task.Assignee, Agent.MainAgentId, result,
                    inReplyTo: task.Id);
                waiter.TrySetResult(mediated ?? "[blocked by mediator]");
            }
        }
        catch (Exception ex)
        {
            _scheduler.RecordFailed(task, ex.Message);
            _log.Append(EventType.TaskFailed, new TaskLifecyclePayload
            {
                TaskId = task.Id, Agent = task.Assignee, Error = ex.Message,
            }.ToJson());
            if (_resultWaiters.TryGetValue(task.Id, out var waiter))
                waiter.TrySetResult($"[task failed] {ex.Message}");
        }
    }

    private string? ConsumeMailboxNote(string agent)
    {
        var mailbox = _scheduler.MailboxFor(agent);
        var notes = new List<string>();
        while (mailbox.ConsumeNextMessage() is { } message)
            notes.Add($"from @{message.Sender}: {message.Content}");
        return notes.Count > 0 ? string.Join('\n', notes) : null;
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        _pumpLock.Dispose();
        _log.Dispose();
    }
}
