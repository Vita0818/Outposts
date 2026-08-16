using System.Text.Json.Nodes;
using Intatis.Core.Permission;
using Intatis.Core.Protocol;
using Intatis.Core.Providers;
using Intatis.Core.Session;

namespace Intatis.Core.Tools;

/// <summary>
/// Tool-calling agent engine for Code and Cowork. Every tool call passes the
/// deterministic gate, the (optional) model reviewer and the user responder before
/// execution; the durable permission_request/permission_resolved pair brackets each
/// decision in the EventLog.
/// </summary>
public sealed class AgentLoop
{
    public const int DefaultMaxIterations = 50;

    private readonly EventLog _log;
    private readonly IToolCallingProvider _provider;
    private readonly ToolRegistry _registry;
    private readonly PermissionEngine _permissions;
    private readonly ToolContext _toolContext;
    private readonly string _agentName;
    private readonly string _model;
    private readonly string _systemPrompt;
    private readonly int _maxIterations;

    public AgentLoop(
        EventLog log,
        IToolCallingProvider provider,
        ToolRegistry registry,
        PermissionEngine permissions,
        string workspaceRoot,
        string agentName,
        string model,
        string systemPrompt,
        int maxIterations = DefaultMaxIterations)
    {
        _log = log;
        _provider = provider;
        _registry = registry;
        _permissions = permissions;
        _toolContext = new ToolContext { WorkspaceRoot = workspaceRoot };
        _agentName = agentName;
        _model = model;
        _systemPrompt = systemPrompt;
        _maxIterations = maxIterations;
    }

    /// <summary>Returns the assistant's final text for the turn.</summary>
    public async Task<string> SendAsync(string userText, CancellationToken ct = default)
    {
        var turnId = TurnId.New();

        var messages = BuildHistory();
        messages.Insert(0, new AgentMessage { Role = "system", Content = _systemPrompt });
        messages.Add(new AgentMessage { Role = "user", Content = userText });

        _log.Append(EventType.UserMessage, new UserMessagePayload
        {
            Text = userText,
            To = _agentName,
            TurnId = turnId,
        }.ToJson());
        _log.Append(EventType.AgentStatus, new AgentStatusPayload
        {
            Agent = _agentName,
            State = "thinking",
        }.ToJson());

        var tools = _registry.Specs();
        var usage = Usage.Empty;

        try
        {
            for (var iteration = 0; iteration < _maxIterations; iteration++)
            {
                ct.ThrowIfCancellationRequested();
                var messageId = MessageId.New();
                var text = "";
                List<AgentToolCall>? issuedCalls = null;

                await foreach (var chunk in _provider.StreamAgentAsync(new AgentRequest
                               {
                                   Model = _model,
                                   Messages = messages,
                                   Tools = tools,
                                   IncludeUsage = true,
                               }, ct).ConfigureAwait(false))
                {
                    switch (chunk)
                    {
                        case AgentChunk.TextDelta delta:
                            text += delta.Text;
                            _log.Append(EventType.MessageDelta, new MessageDeltaPayload
                            {
                                MessageId = messageId,
                                Role = "assistant",
                                Agent = _agentName,
                                TextDelta = delta.Text,
                            }.ToJson(), flush: false);
                            break;

                        case AgentChunk.ToolCallsIssued issued:
                            issuedCalls = issued.Calls;
                            break;

                        case AgentChunk.UsageReport report:
                            usage = usage.MergedWith(report.Usage);
                            break;

                        case AgentChunk.Done done when done.FinishReason
                            is not ("stop" or "end_turn" or "completed" or "complete"
                                or "tool_calls" or "function_call"):
                            throw new InvalidOperationException($"model finished abnormally: {done.FinishReason}");
                    }
                }

                if (issuedCalls is not { Count: > 0 })
                {
                    // Final answer: no pending tool calls.
                    _log.Append(EventType.MessageCompleted, new MessageCompletedPayload
                    {
                        MessageId = messageId,
                        Role = "assistant",
                        Agent = _agentName,
                        Text = text,
                    }.ToJson());
                    _log.Append(EventType.TurnStats, new TurnStatsPayload
                    {
                        PromptTokens = usage.PromptTokens,
                        CompletionTokens = usage.CompletionTokens,
                        TotalTokens = usage.TotalTokens,
                        Model = _model,
                        AgentId = _agentName,
                    }.ToJson(), flush: false);
                    _log.Append(EventType.AgentStatus, new AgentStatusPayload
                    {
                        Agent = _agentName,
                        State = "idle",
                    }.ToJson(), flush: false);
                    _log.Append(EventType.TurnOutcome, new TurnOutcomePayload
                    {
                        TurnId = turnId,
                        Outcome = TurnOutcomeWire.Completed.ToWire(),
                        AgentId = _agentName,
                    }.ToJson());
                    return text;
                }

                // The assistant message that carries these tool calls is terminal.
                if (text.Length > 0)
                    _log.Append(EventType.MessageCompleted, new MessageCompletedPayload
                    {
                        MessageId = messageId,
                        Role = "assistant",
                        Agent = _agentName,
                        Text = text,
                    }.ToJson());
                messages.Add(new AgentMessage { Role = "assistant", Content = text, ToolCalls = issuedCalls });

                foreach (var call in issuedCalls)
                {
                    var observation = await RunSingleToolCallAsync(call, turnId, ct).ConfigureAwait(false);
                    messages.Add(new AgentMessage
                    {
                        Role = "tool",
                        ToolCallId = call.Id,
                        Content = observation.Text.Length > 0 ? observation.Text : "(no output)",
                    });
                }
            }

            throw new InvalidOperationException(
                $"exhausted the iteration limit ({_maxIterations}); this is a terminal error, not an empty success");
        }
        catch (OperationCanceledException)
        {
            _log.Append(EventType.AgentStatus, new AgentStatusPayload { Agent = _agentName, State = "idle" }.ToJson(), flush: false);
            _log.Append(EventType.TurnOutcome, new TurnOutcomePayload
            {
                TurnId = turnId,
                Outcome = TurnOutcomeWire.Interrupted.ToWire(),
                FailureSource = "turn_cancelled",
                Reason = "cancelled by user",
                AgentId = _agentName,
            }.ToJson());
            throw;
        }
        catch (Exception ex)
        {
            _log.Append(EventType.Error, new ErrorPayload
            {
                Code = "agent_loop",
                Message = ex.Message,
            }.ToJson());
            _log.Append(EventType.AgentStatus, new AgentStatusPayload { Agent = _agentName, State = "idle" }.ToJson(), flush: false);
            _log.Append(EventType.TurnOutcome, new TurnOutcomePayload
            {
                TurnId = turnId,
                Outcome = TurnOutcomeWire.Failed.ToWire(),
                FailureSource = "runtime_failed",
                Reason = ex.Message,
                AgentId = _agentName,
            }.ToJson());
            throw;
        }
    }

    private async Task<ToolObservation> RunSingleToolCallAsync(AgentToolCall call, string turnId, CancellationToken ct)
    {
        var tool = _registry.Tool(call.Name);
        if (tool is null)
        {
            var available = string.Join(", ", _registry.Names());
            var unknown = ToolObservation.Failed($"unknown tool '{call.Name}'; available tools: {available}");
            _log.Append(EventType.ToolCall, new ToolCallPayload
            {
                ToolCallId = call.Id,
                Agent = _agentName,
                Name = call.Name,
                Args = call.Arguments,
            }.ToJson());
            _log.Append(EventType.ToolResult, new ToolResultPayload
            {
                ToolCallId = call.Id,
                Observation = unknown.Text,
                Outcome = "failed",
            }.ToJson());
            return unknown;
        }

        JsonNode? args;
        try { args = JsonNode.Parse(string.IsNullOrWhiteSpace(call.Arguments) ? "{}" : call.Arguments); }
        catch (Exception)
        {
            var malformed = ToolObservation.Failed("tool arguments are not valid JSON");
            _log.Append(EventType.ToolCall, new ToolCallPayload
            {
                ToolCallId = call.Id, Agent = _agentName, Name = call.Name, Args = call.Arguments,
            }.ToJson());
            _log.Append(EventType.ToolResult, new ToolResultPayload
            {
                ToolCallId = call.Id, Observation = malformed.Text, Outcome = "failed",
            }.ToJson());
            return malformed;
        }

        _log.Append(EventType.ToolCall, new ToolCallPayload
        {
            ToolCallId = call.Id,
            Agent = _agentName,
            Name = call.Name,
            Args = call.Arguments,
        }.ToJson());
        _log.Append(EventType.AgentStatus, new AgentStatusPayload
        {
            Agent = _agentName,
            State = "tool",
        }.ToJson(), flush: false);

        var touched = tool.TouchedPaths(args ?? new JsonObject());
        var context = new ToolCallContext
        {
            ToolName = call.Name,
            SideEffect = tool.Descriptor.SideEffect,
            TouchedPaths = touched
                .Select(p => Path.IsPathRooted(p) ? p : Path.Join(_toolContext.WorkspaceRoot, p))
                .ToList(),
            RisksNetwork = tool.Descriptor.SideEffect == SideEffect.Network,
            RawArgs = call.Arguments,
            Agent = _agentName,
        };

        var profile = PermissionProfile.Reviewed;

        var requestId = RequestId.New();
        var reason = tool.Descriptor.SideEffect == SideEffect.ReadOnly
            ? "read-only tool call"
            : $"{tool.Descriptor.SideEffect} effect via {call.Name}";
        var risk = tool.Descriptor.SideEffect == SideEffect.ReadOnly ? RiskLevel.Low
            : tool.Descriptor.SideEffect == SideEffect.Write ? RiskLevel.Medium
            : RiskLevel.High;

        _log.Append(EventType.PermissionRequest, new PermissionRequestPayload
        {
            RequestId = requestId,
            Agent = _agentName,
            Tool = call.Name,
            Args = call.Arguments,
            Risk = risk.ToWire(),
            Reason = reason,
        }.ToJson());

        var outcome = await _permissions.DecideAsync(context, _toolContext.WorkspaceRoot, profile, ct).ConfigureAwait(false);

        _log.Append(EventType.PermissionResolved, new PermissionResolvedPayload
        {
            RequestId = requestId,
            Tool = call.Name,
            Decision = outcome.Decision switch
            {
                PermissionDecision.Allow => "allow",
                PermissionDecision.Deny => "deny",
                _ => "ask_user",
            },
            Risk = outcome.Risk.ToWire(),
            Reason = outcome.Reason,
            Source = outcome.Source,
        }.ToJson());

        ToolObservation observation;
        if (outcome.Decision != PermissionDecision.Allow)
        {
            observation = ToolObservation.Denied(outcome.Decision == PermissionDecision.Deny
                ? outcome.Reason
                : "declined by user");
        }
        else
        {
            try
            {
                observation = await tool.ExecuteAsync(args ?? new JsonObject(), _toolContext, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                observation = ToolObservation.Failed(ex.Message);
            }
        }

        _log.Append(EventType.ToolResult, new ToolResultPayload
        {
            ToolCallId = call.Id,
            Observation = observation.Text,
            Truncated = observation.Truncated,
            Outcome = observation.Text.StartsWith("DENIED", StringComparison.Ordinal) ? "denied"
                : observation.Text.StartsWith("ERROR", StringComparison.Ordinal) ? "failed"
                : "succeeded",
        }.ToJson());
        _log.Append(EventType.AgentStatus, new AgentStatusPayload
        {
            Agent = _agentName,
            State = "thinking",
        }.ToJson(), flush: false);

        return observation;
    }

    private List<AgentMessage> BuildHistory()
    {
        // Conversation-level history (complete user/assistant messages). Tool traffic
        // from earlier turns is summarized by the messages themselves.
        var projection = ConversationProjection.Build(_log.Replay());
        var messages = new List<AgentMessage>();
        foreach (var view in projection)
        {
            if (view.Role == MessageRoleWire.User)
                messages.Add(new AgentMessage { Role = "user", Content = view.Text });
            else if (view.Role is MessageRoleWire.Assistant or MessageRoleWire.Agent && view.IsComplete)
                messages.Add(new AgentMessage { Role = "assistant", Content = view.Text });
        }
        var window = messages.Count > 40 ? messages[^40..] : messages;
        return new List<AgentMessage>(window);
    }
}

/// <summary>Mode system prompts (Windows port of the RuntimeEnvironmentManifest texts).</summary>
public static class AgentPrompts
{
    public static string CodePrompt(string workspaceRoot) => $"""
You are Intatis Code, a local coding agent working inside one workspace.

Workspace: {workspaceRoot}

Rules:
- Inspect before you edit: use list_files, search_text and read_file first.
- Prefer apply_patch for edits; use write_file only for new files or full rewrites.
- Every tool call goes through the permission system; a denial is final for that call.
- Never touch files outside the workspace, secrets, credentials or .git internals.
- Keep answers concise; quote file paths exactly as they appear on disk.
- When the task is done, reply with a short summary of what changed.
""";

    public static string CoworkCoordinatorPrompt(string workspaceRoot, string roster) => $"""
You are the coordinator agent (@main) of an Intatis Cowork session.

Workspace: {workspaceRoot}
Agents: {roster}

You decompose the user's goal into work for the roster and synthesize results.
Delegate by addressing agents; the host scheduler runs each agent's loop in its own
task — you never run another agent's loop yourself. Keep the user informed of
progress and produce the final consolidated answer.
""";

    public static string CoworkWorkerPrompt(string name, string workspaceRoot) => $"""
You are agent @{name} in an Intatis Cowork session, working in: {workspaceRoot}

You receive delegated work from the coordinator or other agents. Use the file tools
read-only unless a change is required by your objective; edits pass the permission
chain. Answer with the concrete result, not a plan.
""";
}
