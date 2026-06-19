using System.Diagnostics;

namespace Intatis.Windows.Shared;

public sealed class CodeAgentSession
{
    public IReadOnlyList<OpenAIClient.OpenAIChatMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyList<string> ToolNames => _toolRegistry.Descriptors.Select(d => d.Name).ToList();

    private readonly OpenAIClient _client;
    private readonly List<OpenAIClient.OpenAIChatMessage> _messages = [];
    private readonly IToolShellRunner _shell;
    private readonly IToolGitService _git;
    private readonly IPermissionResponder _responder;
    private readonly PermissionEngine _permissionEngine;
    private readonly string _workspaceRoot;
    private readonly string _agentName;
    private readonly int _maxIterations;
    private readonly bool _allowsShell;
    private readonly string _systemPrompt;
    private readonly PermissionProfile _permissionProfile;
    private readonly bool _includeUsage;
    private readonly IToolAgentMessenger? _messenger;
    private readonly IConversationEventSink _eventSink;

    public CodeAgentSession(
        IntatisConfig config,
        string workspaceRoot,
        string agentName = "agent",
        PermissionProfile permissionProfile = PermissionProfile.Reviewed,
        IToolShellRunner? shell = null,
        IToolGitService? git = null,
        IToolAgentMessenger? messenger = null,
        IPermissionResponder? responder = null,
        IPermissionReviewer? permissionReviewer = null,
        IConversationEventSink? eventSink = null,
        bool allowsShell = true,
        int maxIterations = 8,
        string? systemPrompt = null)
    {
        _workspaceRoot = workspaceRoot;
        _agentName = agentName;
        _permissionProfile = permissionProfile;
        _shell = shell ?? new ProcessShellRunner();
        _git = git ?? new ProcessGitService(_shell);
        _messenger = messenger;
        _responder = responder ?? new AllowAllResponder();
        _permissionEngine = new PermissionEngine(permissionReviewer);
        _eventSink = eventSink ?? new NullConversationEventSink();
        _allowsShell = allowsShell;
        _maxIterations = Math.Max(1, maxIterations);
        _systemPrompt = string.IsNullOrWhiteSpace(systemPrompt)
            ? $"You are a Windows code assistant. Operate inside workspace: {_workspaceRoot}. Keep tool usage deterministic and short."
            : systemPrompt;
        _includeUsage = config.IncludeUsage;

        _client = new OpenAIClient(config);
        _messages.Add(new OpenAIClient.OpenAIChatMessage("system", _systemPrompt));
        _messages.Add(new OpenAIClient.OpenAIChatMessage("system", "Use tools when helpful. Never guess file content."));
    }

    public void Clear()
    {
        _messages.Clear();
        _messages.Add(new OpenAIClient.OpenAIChatMessage("system", _systemPrompt));
        _messages.Add(new OpenAIClient.OpenAIChatMessage("system", "Use tools when helpful. Never guess file content."));
    }

    public async Task<(string Text, TimeSpan Latency, string? Usage)> SendAsync(
        string userText,
        string? model = null,
        string? reasoning = null,
        string? userGoal = null,
        IReadOnlyList<ImageAttachment>? images = null,
        bool? includeUsage = null,
        CancellationToken cancellationToken = default)
    {
        _messages.Add(new OpenAIClient.OpenAIChatMessage("user", userText, Images: images));
        var totalLatency = TimeSpan.Zero;
        string? usage = null;
        var toolAware = _toolRegistry;
        var resolvedIncludeUsage = includeUsage ?? _includeUsage;

        for (var iteration = 0; iteration < _maxIterations; iteration++)
        {
            var result = await _client.SendWithToolsAsync(
                _messages,
                toolAware.Descriptors.ToList(),
                model,
                reasoning,
                includeUsage: resolvedIncludeUsage,
                cancellationToken: cancellationToken);
            totalLatency += result.LatencyMs;
            usage ??= result.Usage;

            var hasCalls = result.ToolCalls.Count > 0;
            var response = result.Text;

            if (!hasCalls)
            {
                _messages.Add(new OpenAIClient.OpenAIChatMessage("assistant", response));
                return (response, totalLatency, usage);
            }

            _messages.Add(new OpenAIClient.OpenAIChatMessage(
                "assistant",
                string.IsNullOrWhiteSpace(response) ? null : response,
                result.ToolCalls.ToList()));

            foreach (var toolCall in result.ToolCalls)
            {
                await _eventSink.AppendAsync(ConversationEventKinds.ToolCall, ConversationEventPayloads.ToolCall(
                    toolCall.Id,
                    _agentName,
                    toolCall.Name,
                    toolCall.Arguments));

                var observation = await ExecuteToolAsync(toolCall, toolAware, model, reasoning, userGoal, cancellationToken);
                _messages.Add(new OpenAIClient.OpenAIChatMessage(
                    "tool",
                    observation,
                    ToolCallId: toolCall.Id));

                await _eventSink.AppendAsync(ConversationEventKinds.ToolResult, ConversationEventPayloads.ToolResult(
                    toolCall.Id,
                    observation));
            }
        }

        var timeout = "tool loop reached max iterations.";
        _messages.Add(new OpenAIClient.OpenAIChatMessage("assistant", timeout));
        return (timeout, totalLatency, usage);
    }

    private ToolRegistry _toolRegistry
    {
        get
        {
            ITool[] tools = _messenger is null
                ? Array.Empty<ITool>()
                : new ITool[] { new AskAgentTool() };
            return ToolRegistry.Standard().Add(tools);
        }
    }

    private async Task<string> ExecuteToolAsync(
        OpenAIClient.ToolCall toolCall,
        ToolRegistry registry,
        string? model,
        string? reasoning,
        string? userGoal,
        CancellationToken cancellationToken)
    {
        var tool = registry.Tool(toolCall.Name);
        if (tool is null)
            return $"unknown tool: {toolCall.Name}";

        var args = new ToolArgs(toolCall.Arguments);
        List<string> touchedPaths;

        try
        {
            touchedPaths = tool.TouchedPaths(args).Select(p => WorkspaceSecurity.ResolveInWorkspace(_workspaceRoot, p)).ToList();
        }
        catch (Exception ex)
        {
            return $"tool blocked: cannot resolve touched paths ({ex.Message})";
        }

        var callContext = new ToolCallContext(
            toolCall.Name,
            tool.Descriptor.SideEffect,
            touchedPaths,
            tool.RisksNetwork(args),
            toolCall.Arguments);

        var permissionContext = new PermissionContext(
            _workspaceRoot,
            _permissionProfile,
            _allowsShell,
            userGoal,
            _agentName);

        var outcome = await _permissionEngine.DecideAsync(callContext, permissionContext);
        if (outcome.ReviewedByModel)
        {
            await _eventSink.AppendAsync(ConversationEventKinds.PermissionReview, ConversationEventPayloads.PermissionReview(
                tool.Descriptor.Name,
                outcome.Decision,
                outcome.Risk,
                outcome.Reason,
                "model",
                _agentName));
        }

        if (!outcome.Decision.Equals(PermissionDecision.Allow))
        {
            var finalDecision = await ResolvePermissionAsync(outcome, tool, toolCall);
            if (finalDecision != PermissionDecision.Allow)
                return $"permission denied: {outcome.Reason}";
        }

        var context = new ToolContext(
            _workspaceRoot,
            _agentName,
            _shell,
            _git,
            _messenger);

        try
        {
            var observation = await tool.ExecuteAsync(args, context, cancellationToken);
            if (observation.ChangedFiles is { Count: > 0 })
            {
                await _eventSink.AppendAsync(ConversationEventKinds.PatchProposed, ConversationEventPayloads.PatchProposed(
                    $"patch-{Guid.NewGuid():N}",
                    _agentName,
                    observation.ChangedFiles,
                    observation.Diff ?? string.Empty));
            }
            return observation.Text;
        }
        catch (Exception ex)
        {
            return $"tool error: {ex.Message}";
        }
    }

    private async Task<PermissionDecision> ResolvePermissionAsync(
        PermissionOutcome outcome,
        ITool tool,
        OpenAIClient.ToolCall toolCall)
    {
        if (outcome.Decision != PermissionDecision.AskUser)
        {
            await _eventSink.AppendAsync(ConversationEventKinds.PermissionResolved, ConversationEventPayloads.PermissionResolved(
                null,
                tool.Descriptor.Name,
                outcome.Decision,
                outcome.Risk,
                outcome.Reason,
                _agentName));
            return outcome.Decision;
        }

        var requestId = Guid.NewGuid().ToString("N");
        await _eventSink.AppendAsync(ConversationEventKinds.PermissionRequest, ConversationEventPayloads.PermissionRequest(
            requestId,
            _agentName,
            tool.Descriptor.Name,
            toolCall.Arguments,
            outcome.Risk,
            outcome.Reason));

        var finalDecision = await AskUserForPermissionAsync(tool, outcome, requestId, toolCall);
        await _eventSink.AppendAsync(ConversationEventKinds.PermissionResolved, ConversationEventPayloads.PermissionResolved(
            requestId,
            tool.Descriptor.Name,
            finalDecision,
            outcome.Risk,
            finalDecision == PermissionDecision.Allow ? "user approved" : "user denied",
            _agentName));
        return finalDecision;
    }

    private async Task<PermissionDecision> AskUserForPermissionAsync(
        ITool tool,
        PermissionOutcome outcome,
        string requestId,
        OpenAIClient.ToolCall toolCall)
    {
        var request = new PermissionRequest(
            requestId,
            tool.Descriptor.Name,
            toolCall.Arguments,
            outcome.Risk,
            outcome.Reason,
            _agentName);
        var decision = await _responder.RequestApprovalAsync(request);
        return decision;
    }
}
