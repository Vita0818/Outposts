using System.Text.Json.Nodes;
using Intatis.Core.Protocol;

namespace Intatis.Core.Cowork;

public sealed record ScheduledTask
{
    public required TaskId Id { get; init; }
    public required string Assignee { get; init; }
    public string Kind { get; init; } = "root"; // root | agent_invocation | mailbox_delivery
    public string Objective { get; init; } = "";
    public string Input { get; init; } = "";
    public string? ParentTaskId { get; init; }
    public string? ReplyMode { get; init; } // answer | task_report | none
    public int Attempt { get; init; } = 1;
    public List<string> VisitedAgents { get; init; } = [];
}

public sealed record ExecutionRecord
{
    public required TaskId TaskId { get; init; }
    public required string Assignee { get; init; }
    public string Status { get; init; } = "queued"; // queued | running | completed | failed | cancelled
    public string? Result { get; init; }
    public string? Error { get; init; }
    public string? ParentTaskId { get; init; }
}

public sealed record PendingAgentMessage
{
    public required MessageId Id { get; init; }
    public required string Sender { get; init; }
    public required string Recipient { get; init; }
    public required string Content { get; init; }
    public string Kind { get; init; } = "send_message";
    public string? TaskId { get; init; }
    public string? InReplyTo { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public sealed class AgentMailbox
{
    private readonly object _gate = new();
    private readonly Queue<PendingAgentMessage> _messages = new();
    public string Owner { get; }

    public AgentMailbox(string owner) => Owner = owner;

    public void EnqueueMessage(PendingAgentMessage message)
    {
        lock (_gate) { _messages.Enqueue(message); }
    }

    public List<PendingAgentMessage> PeekMessages()
    {
        lock (_gate) { return _messages.ToList(); }
    }

    public PendingAgentMessage? ConsumeNextMessage()
    {
        lock (_gate) { return _messages.Count > 0 ? _messages.Dequeue() : null; }
    }
}

/// <summary>
/// FIFO scheduler. Claiming is synchronous: a claimed assignee stays busy until the
/// claim is completed, failed, cancelled or released — callers never run an agent
/// loop on the scheduler's behalf, so cowork never recurses into itself.
/// </summary>
public sealed class AgentScheduler
{
    private readonly object _gate = new();
    private readonly List<ScheduledTask> _queue = new();
    private readonly Dictionary<TaskId, ExecutionRecord> _records = new();
    private readonly Dictionary<string, AgentMailbox> _mailboxes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TaskId> _claimedByAgent = new(StringComparer.Ordinal);

    public int MaxConcurrentTasks { get; }

    public AgentScheduler(int maxConcurrentTasks = 4) => MaxConcurrentTasks = maxConcurrentTasks;

    public bool Enqueue(ScheduledTask task)
    {
        lock (_gate)
        {
            if (_queue.Any(t => t.Id == task.Id)) return false;
            if (_records.ContainsKey(task.Id)) return false;
            _queue.Add(task);
            _records[task.Id] = new ExecutionRecord { TaskId = task.Id, Assignee = task.Assignee };
            return true;
        }
    }

    /// <summary>Claims the first queued task whose assignee is idle (FIFO, skip busy).</summary>
    public ScheduledTask? ClaimNext()
    {
        lock (_gate)
        {
            if (_claimedByAgent.Count >= MaxConcurrentTasks) return null;
            for (var i = 0; i < _queue.Count; i++)
            {
                var task = _queue[i];
                if (_claimedByAgent.ContainsKey(task.Assignee)) continue;
                _queue.RemoveAt(i);
                _claimedByAgent[task.Assignee] = task.Id;
                _records[task.Id] = new ExecutionRecord
                {
                    TaskId = task.Id,
                    Assignee = task.Assignee,
                    Status = "queued",
                    ParentTaskId = task.ParentTaskId,
                };
                return task;
            }
            return null;
        }
    }

    public bool HasQueuedOrRunning
    {
        get
        {
            lock (_gate)
            {
                return _queue.Count > 0
                       || _records.Values.Any(r => r.Status is "queued" or "running");
            }
        }
    }

    public void RecordStarted(ScheduledTask task)
    {
        lock (_gate) { _records[task.Id] = new ExecutionRecord { TaskId = task.Id, Assignee = task.Assignee, Status = "running", ParentTaskId = task.ParentTaskId }; }
    }

    public void RecordCompleted(ScheduledTask task, string result)
    {
        lock (_gate)
        {
            _records[task.Id] = new ExecutionRecord { TaskId = task.Id, Assignee = task.Assignee, Status = "completed", Result = result, ParentTaskId = task.ParentTaskId };
            ReleaseClaim(task.Assignee, task.Id);
        }
    }

    public void RecordFailed(ScheduledTask task, string error)
    {
        lock (_gate)
        {
            _records[task.Id] = new ExecutionRecord { TaskId = task.Id, Assignee = task.Assignee, Status = "failed", Error = error, ParentTaskId = task.ParentTaskId };
            ReleaseClaim(task.Assignee, task.Id);
        }
    }

    public void RecordCancelled(ScheduledTask task, string reason)
    {
        lock (_gate)
        {
            _records[task.Id] = new ExecutionRecord { TaskId = task.Id, Assignee = task.Assignee, Status = "cancelled", Error = reason, ParentTaskId = task.ParentTaskId };
            ReleaseClaim(task.Assignee, task.Id);
        }
    }

    private void ReleaseClaim(string assignee, TaskId taskId)
    {
        if (_claimedByAgent.GetValueOrDefault(assignee) == taskId)
            _claimedByAgent.Remove(assignee);
    }

    public AgentMailbox MailboxFor(string agent)
    {
        lock (_gate)
        {
            if (!_mailboxes.TryGetValue(agent, out var mailbox))
            {
                mailbox = new AgentMailbox(agent);
                _mailboxes[agent] = mailbox;
            }
            return mailbox;
        }
    }

    public List<ExecutionRecord> Records()
    {
        lock (_gate) { return _records.Values.ToList(); }
    }
}

/// <summary>
/// Deterministic forwarding policy: secrets block, oversized raw dumps block,
/// everything else forwards verbatim.
/// </summary>
public sealed class Mediator
{
    public const int DefaultMaxChars = 4000;

    private readonly int _maxChars;

    public Mediator(int maxChars = DefaultMaxChars) => _maxChars = maxChars;

    public sealed record ForwardingDecision(string? Forwarded, string? BlockReason)
    {
        public static ForwardingDecision Pass(string content) => new(content, null);
        public static ForwardingDecision Block(string reason) => new(null, reason);
    }

    public ForwardingDecision Mediate(string from, string to, string content)
    {
        if (Permission.SecretScanner.ContainsSecret(content))
            return ForwardingDecision.Block("content appears to contain secrets");
        if (content.Length > _maxChars)
            return ForwardingDecision.Block($"content exceeds {_maxChars} characters");
        return ForwardingDecision.Pass(content);
    }
}

/// <summary>
/// The single channel for agent-to-agent traffic. Every message is mediated and
/// logged — there is no other delivery path.
/// </summary>
public sealed class MessageBus
{
    private readonly EventLog _log;
    private readonly Mediator _mediator;
    private readonly AgentScheduler _scheduler;

    public MessageBus(EventLog log, Mediator mediator, AgentScheduler scheduler)
    {
        _log = log;
        _mediator = mediator;
        _scheduler = scheduler;
    }

    /// <summary>Delivers agent-to-agent content. Returns the forwarded text, or null when blocked.</summary>
    public string? Deliver(string from, string to, string content, string? taskId = null)
    {
        var decision = _mediator.Mediate(from, to, content);
        if (decision.Forwarded is null)
        {
            _log.Append(EventType.PermissionReview, new JsonObjectBuilder()
                .Add("reviewer_model", "mediator")
                .Add("tool", "agent_forward")
                .Add("decision", "deny")
                .Add("reason", decision.BlockReason ?? "blocked")
                .Build());
            return null;
        }

        _log.Append(EventType.AgentToAgentMessage, new JsonObjectBuilder()
            .Add("from", from)
            .Add("to", to)
            .Add("content", decision.Forwarded)
            .Add("task_id", taskId)
            .Build());
        _scheduler.MailboxFor(to).EnqueueMessage(new PendingAgentMessage
        {
            Id = MessageId.New(),
            Sender = from,
            Recipient = to,
            Content = decision.Forwarded,
            TaskId = taskId,
        });
        return decision.Forwarded;
    }

    public string? SendMessage(string from, string to, string content, string? taskId = null)
    {
        var decision = _mediator.Mediate(from, to, content);
        if (decision.Forwarded is null)
        {
            _log.Append(EventType.PermissionReview, new JsonObjectBuilder()
                .Add("reviewer_model", "mediator")
                .Add("tool", "send_message")
                .Add("decision", "deny")
                .Add("reason", decision.BlockReason ?? "blocked")
                .Build());
            return null;
        }
        _log.Append(EventType.AgentMessage, new AgentMessagePayload
        {
            Kind = "send_message",
            From = from,
            To = to,
            Content = decision.Forwarded,
            TaskId = taskId,
        }.ToJson());
        _scheduler.MailboxFor(to).EnqueueMessage(new PendingAgentMessage
        {
            Id = MessageId.New(),
            Sender = from,
            Recipient = to,
            Content = decision.Forwarded,
            TaskId = taskId,
        });
        return decision.Forwarded;
    }

    public string? Reply(string from, string to, string content, string inReplyTo, string? taskId = null)
    {
        var decision = _mediator.Mediate(from, to, content);
        if (decision.Forwarded is null) return null;
        _log.Append(EventType.AgentMessage, new AgentMessagePayload
        {
            Kind = "reply_message",
            From = from,
            To = to,
            Content = decision.Forwarded,
            TaskId = taskId,
        }.ToJson());
        _scheduler.MailboxFor(to).EnqueueMessage(new PendingAgentMessage
        {
            Id = MessageId.New(),
            Sender = from,
            Recipient = to,
            Content = decision.Forwarded,
            Kind = "reply_message",
            TaskId = taskId,
            InReplyTo = inReplyTo,
        });
        return decision.Forwarded;
    }
}

/// <summary>Tiny fluent helper to keep JsonObject construction readable.</summary>
internal sealed class JsonObjectBuilder
{
    private readonly JsonObject _obj = new();

    public JsonObjectBuilder Add(string key, string? value)
    {
        _obj[key] = value;
        return this;
    }

    public JsonObject Build() => _obj;
}
