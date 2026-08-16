using System.Text.Json.Nodes;

namespace Intatis.Core.Protocol;

/// <summary>
/// Wire shape (one JSON object per line, mirrors the Apple EventLog contract):
/// {"seq":1421,"ts":"2026-06-11T09:14:22Z","session":"sess_8f2a","v":1,"type":"message_delta","payload":{...}}
/// The JSONL event log is the canonical session truth; every projection is rebuildable.
/// </summary>
public sealed record Envelope
{
    public long Seq { get; init; }
    public DateTime Ts { get; init; }
    public string Session { get; init; } = "";
    public int V { get; init; } = 1;
    public string Type { get; init; } = "";
    public JsonNode? Payload { get; init; }

    public string ToJsonLine() => Jsonx.SerializeSorted(new JsonObject
    {
        ["seq"] = Seq,
        ["ts"] = new DateTimeOffset(DateTime.SpecifyKind(Ts, DateTimeKind.Utc), TimeSpan.Zero).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
        ["session"] = Session,
        ["v"] = V,
        ["type"] = Type,
        ["payload"] = Payload?.DeepClone(),
    });

    public static Envelope FromJsonLine(string line)
    {
        var obj = JsonNode.Parse(line) as JsonObject
            ?? throw new FormatException("event line is not a JSON object");
        long seq = (int?)obj["seq"] ?? -1;
        var tsRaw = (string?)obj["ts"] ?? "";
        DateTime ts = DateTimeOffset.TryParse(tsRaw, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var dto)
            ? dto.UtcDateTime : DateTime.UtcNow;
        return new Envelope
        {
            Seq = seq,
            Ts = ts,
            Session = (string?)obj["session"] ?? "",
            V = (int?)obj["v"] ?? 1,
            Type = (string?)obj["type"] ?? "",
            Payload = obj["payload"]?.DeepClone(),
        };
    }
}

/// <summary>
/// Event tags use snake_case wire names and evolve additively only: readers must
/// skip unknown future types while reserving their sequence space.
/// </summary>
public static class EventType
{
    public const string SessionSettingsUpdated = "session_settings_updated";
    public const string UserMessage = "user_message";
    public const string MessageDelta = "message_delta";
    public const string MessageCompleted = "message_completed";
    public const string Error = "error";
    public const string ToolCall = "tool_call";
    public const string ToolResult = "tool_result";
    public const string PermissionRequest = "permission_request";
    public const string PermissionResolved = "permission_resolved";
    public const string AgentStatus = "agent_status";
    public const string AgentAttached = "agent_attached";
    public const string AgentDetached = "agent_detached";
    public const string AgentMessage = "agent_message";
    public const string AgentToAgentMessage = "agent_to_agent_message";
    public const string TaskCreated = "task_created";
    public const string TaskStarted = "task_started";
    public const string TaskCompleted = "task_completed";
    public const string TaskFailed = "task_failed";
    public const string TaskCancelled = "task_cancelled";
    public const string WorkTaskCreated = "work_task_created";
    public const string WorkTaskUpdated = "work_task_updated";
    public const string GoalCreated = "goal_created";
    public const string GoalProgressed = "goal_progressed";
    public const string GoalPaused = "goal_paused";
    public const string GoalResumed = "goal_resumed";
    public const string GoalCompleted = "goal_completed";
    public const string PermissionReview = "permission_review";
    public const string TurnStats = "turn_stats";
    public const string TurnOutcome = "turn_outcome";
    public const string ArtifactAdded = "artifact_added";
}

public enum MessageRoleWire
{
    User,
    Assistant,
    Agent,
    System,
}

public static class MessageRoleWireExtensions
{
    public static string ToWire(this MessageRoleWire role) => role switch
    {
        MessageRoleWire.User => "user",
        MessageRoleWire.Assistant => "assistant",
        MessageRoleWire.Agent => "agent",
        _ => "system",
    };

    public static MessageRoleWire FromWire(string value) => value switch
    {
        "assistant" => MessageRoleWire.Assistant,
        "agent" => MessageRoleWire.Agent,
        "system" => MessageRoleWire.System,
        _ => MessageRoleWire.User,
    };
}
