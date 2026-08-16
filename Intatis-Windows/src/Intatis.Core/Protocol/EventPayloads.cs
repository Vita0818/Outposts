using System.Text.Json.Nodes;

namespace Intatis.Core.Protocol;

public sealed record UserMessagePayload
{
    public string Text { get; init; } = "";
    public string? To { get; init; }
    public List<string>? Attachments { get; init; }
    public string? Goal { get; init; }
    public string? SubmissionId { get; init; }
    public string? TurnId { get; init; }

    public JsonObject ToJson() => new()
    {
        ["text"] = Text,
        ["to"] = To,
        ["attachments"] = Attachments is { Count: > 0 } ? new JsonArray(Attachments.Select(a => (JsonNode?)a).ToArray()) : null,
        ["goal"] = Goal,
        ["submission_id"] = SubmissionId,
        ["turn_id"] = TurnId,
    };

    public static UserMessagePayload FromJson(JsonNode? node)
    {
        var o = node as JsonObject ?? new JsonObject();
        List<string>? attachments = null;
        if (o["attachments"] is JsonArray arr)
            attachments = arr.Where(v => v is not null).Select(v => (string)v!).ToList();
        return new UserMessagePayload
        {
            Text = (string?)o["text"] ?? "",
            To = (string?)o["to"],
            Attachments = attachments,
            Goal = (string?)o["goal"],
            SubmissionId = (string?)o["submission_id"],
            TurnId = (string?)o["turn_id"],
        };
    }
}

public sealed record MessageDeltaPayload
{
    public string MessageId { get; init; } = "";
    public string Role { get; init; } = "assistant";
    public string? Agent { get; init; }
    public string TextDelta { get; init; } = "";

    public JsonObject ToJson() => new()
    {
        ["message_id"] = MessageId,
        ["role"] = Role,
        ["agent"] = Agent,
        ["text_delta"] = TextDelta,
    };
}

public sealed record MessageCitation
{
    public string Url { get; init; } = "";
    public string Title { get; init; } = "";
}

public sealed record MessageCompletedPayload
{
    public string MessageId { get; init; } = "";
    public string Role { get; init; } = "assistant";
    public string? Agent { get; init; }
    public string Text { get; init; } = "";
    public List<MessageCitation> Citations { get; init; } = [];

    public JsonObject ToJson() => new()
    {
        ["message_id"] = MessageId,
        ["role"] = Role,
        ["agent"] = Agent,
        ["text"] = Text,
        ["citations"] = Citations.Count == 0 ? null
            : new JsonArray(Citations.Select(c => (JsonNode?)new JsonObject { ["url"] = c.Url, ["title"] = c.Title }).ToArray()),
    };
}

public sealed record ErrorPayload
{
    public string Code { get; init; } = "runtime_failed";
    public string Message { get; init; } = "";
    public bool Fatal { get; init; }

    public JsonObject ToJson() => new()
    {
        ["code"] = Code,
        ["message"] = Message.Length > 1024 ? Message[..1024] : Message,
        ["fatal"] = Fatal,
    };
}

public sealed record ToolCallPayload
{
    public string ToolCallId { get; init; } = "";
    public string? Agent { get; init; }
    public string Name { get; init; } = "";
    public string Args { get; init; } = "";

    public JsonObject ToJson() => new()
    {
        ["tool_call_id"] = ToolCallId,
        ["agent"] = Agent,
        ["name"] = Name,
        ["args"] = Args,
    };
}

public sealed record ToolResultPayload
{
    public string ToolCallId { get; init; } = "";
    public string Observation { get; init; } = "";
    public bool Truncated { get; init; }
    public string Outcome { get; init; } = "succeeded";

    public JsonObject ToJson() => new()
    {
        ["tool_call_id"] = ToolCallId,
        ["observation"] = Observation,
        ["truncated"] = Truncated,
        ["outcome"] = Outcome,
    };
}

public sealed record PermissionRequestPayload
{
    public string RequestId { get; init; } = "";
    public string? Agent { get; init; }
    public string Tool { get; init; } = "";
    public string Args { get; init; } = "";
    public string Risk { get; init; } = "medium";
    public string Reason { get; init; } = "";

    public JsonObject ToJson() => new()
    {
        ["request_id"] = RequestId,
        ["agent"] = Agent,
        ["tool"] = Tool,
        ["args"] = Args,
        ["risk"] = Risk,
        ["reason"] = Reason,
    };
}

public sealed record PermissionResolvedPayload
{
    public string RequestId { get; init; } = "";
    public string Tool { get; init; } = "";
    public string Decision { get; init; } = "deny";
    public string Risk { get; init; } = "medium";
    public string Reason { get; init; } = "";
    public string Source { get; init; } = "deterministic_policy";

    public JsonObject ToJson() => new()
    {
        ["request_id"] = RequestId,
        ["tool"] = Tool,
        ["decision"] = Decision,
        ["risk"] = Risk,
        ["reason"] = Reason,
        ["source"] = Source,
    };
}

public sealed record AgentStatusPayload
{
    public string Agent { get; init; } = "";
    public string State { get; init; } = "idle";

    public JsonObject ToJson() => new()
    {
        ["agent"] = Agent,
        ["state"] = State,
    };
}

public sealed record TurnStatsPayload
{
    public int? PromptTokens { get; init; }
    public int? CachedPromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens { get; init; }
    public string? Model { get; init; }
    public string? AgentId { get; init; }

    public JsonObject ToJson() => new()
    {
        ["prompt_tokens"] = PromptTokens,
        ["cached_prompt_tokens"] = CachedPromptTokens,
        ["completion_tokens"] = CompletionTokens,
        ["total_tokens"] = TotalTokens,
        ["model"] = Model,
        ["agent_id"] = AgentId,
    };
}

public enum TurnOutcomeWire
{
    Completed,
    Interrupted,
    Failed,
}

public sealed record TurnOutcomePayload
{
    public string TurnId { get; init; } = "";
    public string Outcome { get; init; } = "completed";
    public string? FailureSource { get; init; }
    public string? Reason { get; init; }
    public string? AgentId { get; init; }

    public JsonObject ToJson() => new()
    {
        ["turn_id"] = TurnId,
        ["outcome"] = Outcome,
        ["failure_source"] = FailureSource,
        ["reason"] = Reason is { Length: > 512 } ? Reason[..512] : Reason,
        ["agent_id"] = AgentId,
    };
}

public sealed record SessionSettingsUpdatedPayload
{
    public int SchemaVersion { get; init; } = 1;
    public int Revision { get; init; } = 1;
    public int? PreviousRevision { get; init; }
    public string ChangeKind { get; init; } = "created";
    public string Kind { get; init; } = "chat";
    public string? DisplayName { get; init; }

    public JsonObject ToJson() => new()
    {
        ["schema_version"] = SchemaVersion,
        ["revision"] = Revision,
        ["previous_revision"] = PreviousRevision,
        ["change_kind"] = ChangeKind,
        ["kind"] = Kind,
        ["display_name"] = DisplayName,
    };
}

public sealed record AgentAttachedPayload
{
    public string Agent { get; init; } = "";
    public string Model { get; init; } = "";
    public string? Workspace { get; init; }
    public string PermissionProfile { get; init; } = "reviewed";
    public string Role { get; init; } = "worker";

    public JsonObject ToJson() => new()
    {
        ["agent"] = Agent,
        ["model"] = Model,
        ["workspace"] = Workspace,
        ["permission_profile"] = PermissionProfile,
        ["role"] = Role,
    };
}

public sealed record AgentMessagePayload
{
    public string Kind { get; init; } = "send_message";
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public string Content { get; init; } = "";
    public string? TaskId { get; init; }
    public bool Mediated { get; init; } = true;

    public JsonObject ToJson() => new()
    {
        ["kind"] = Kind,
        ["from"] = From,
        ["to"] = To,
        ["content"] = Content,
        ["task_id"] = TaskId,
        ["mediated"] = Mediated,
    };
}

public sealed record TaskLifecyclePayload
{
    public string TaskId { get; init; } = "";
    public string Agent { get; init; } = "";
    public string Objective { get; init; } = "";
    public string? ParentTaskId { get; init; }
    public string? Result { get; init; }
    public string? Error { get; init; }
    public string? Reason { get; init; }

    public JsonObject ToJson() => new()
    {
        ["task_id"] = TaskId,
        ["agent"] = Agent,
        ["objective"] = Objective,
        ["parent_task_id"] = ParentTaskId,
        ["result"] = Result,
        ["error"] = Error,
        ["reason"] = Reason,
    };
}
