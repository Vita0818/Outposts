using System.Text.Json;

namespace Intatis.Windows.Shared;

public interface IConversationEventSink
{
    Task AppendAsync(string eventType, Dictionary<string, object?> payload);
}

public sealed class NullConversationEventSink : IConversationEventSink
{
    public Task AppendAsync(string eventType, Dictionary<string, object?> payload)
    {
        return Task.CompletedTask;
    }
}

public static class ConversationEventKinds
{
    public const string ToolCall = "tool_call";
    public const string ToolResult = "tool_result";
    public const string PermissionRequest = "permission_request";
    public const string PermissionResolved = "permission_resolved";
    public const string PermissionReview = "permission_review";
    public const string PatchProposed = "patch_proposed";
    public const string AgentToAgentMessage = "agent_to_agent_message";
}

public static class ConversationEventPayloads
{
    public static Dictionary<string, object?> ToolCall(string toolCallId, string agent, string tool, string args)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tool_call_id"] = toolCallId,
            ["agent"] = agent,
            ["tool"] = tool,
            ["args"] = args,
        };
    }

    public static Dictionary<string, object?> ToolResult(string toolCallId, string observation, bool? truncated = null)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tool_call_id"] = toolCallId,
            ["observation"] = observation,
        };

        if (truncated.HasValue)
            payload["truncated"] = truncated.Value;

        return payload;
    }

    public static Dictionary<string, object?> PermissionRequest(
        string requestId,
        string? agent,
        string tool,
        string args,
        RiskLevel risk,
        string reason)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["request_id"] = requestId,
            ["agent"] = agent,
            ["tool"] = tool,
            ["args"] = args,
            ["risk"] = risk.ToString().ToLowerInvariant(),
            ["reason"] = reason,
        };
    }

    public static Dictionary<string, object?> PermissionResolved(
        string? requestId,
        string tool,
        PermissionDecision decision,
        RiskLevel risk,
        string reason,
        string? agent = null)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["request_id"] = requestId,
            ["tool"] = tool,
            ["agent"] = agent,
            ["decision"] = decision switch
            {
                PermissionDecision.Allow => "allow",
                PermissionDecision.Deny => "deny",
                PermissionDecision.AskUser => "ask_user",
                _ => "ask_user",
            },
            ["risk"] = risk.ToString().ToLowerInvariant(),
            ["reason"] = reason,
        };
    }

    public static Dictionary<string, object?> PermissionReview(
        string tool,
        PermissionDecision decision,
        RiskLevel risk,
        string reason,
        string reviewerModel,
        string? agent = null)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["agent"] = agent,
            ["tool"] = tool,
            ["reviewer_model"] = reviewerModel,
            ["decision"] = decision switch
            {
                PermissionDecision.Allow => "allow",
                PermissionDecision.Deny => "deny",
                PermissionDecision.AskUser => "ask_user",
                _ => "ask_user",
            },
            ["risk"] = risk.ToString().ToLowerInvariant(),
            ["reason"] = reason,
        };
    }

    public static Dictionary<string, object?> PatchProposed(string patchId, string agent, IReadOnlyList<string> files, string diff)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["patch_id"] = patchId,
            ["agent"] = agent,
            ["files"] = files,
            ["diff"] = diff,
        };
    }

    public static Dictionary<string, object?> AgentToAgentMessage(string from, string to, string content, bool mediated)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["from"] = from,
            ["to"] = to,
            ["content"] = content,
            ["mediated"] = mediated,
        };
    }
}

public sealed class SessionEventLog : IConversationEventSink
{
    private readonly string _session;
    private readonly string _path;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private int _seq;

    public SessionEventLog(string category, string? session = null)
    {
        var logs = Path.Combine(ConfigStore.ConfigFolder, "logs", category);
        Directory.CreateDirectory(logs);
        _session = session ?? ($"{DateTime.UtcNow:yyyyMMddTHHmmssfff}-{Guid.NewGuid():N}");
        _path = Path.Combine(logs, $"{_session}.jsonl");
    }

    public string Path => _path;

    public async Task AppendAsync(string eventType, Dictionary<string, object?> payload)
    {
        var wrapper = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = eventType,
            ["session"] = _session,
            ["seq"] = Interlocked.Increment(ref _seq) - 1,
            ["ts"] = DateTime.UtcNow.ToString("o"),
            ["payload"] = payload,
        };

        var line = JsonSerializer.Serialize(wrapper);
        await _mutex.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_path, line + Environment.NewLine);
        }
        finally
        {
            _mutex.Release();
        }
    }
}
