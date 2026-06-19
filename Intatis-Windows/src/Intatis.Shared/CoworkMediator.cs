namespace Intatis.Windows.Shared;

public enum CoworkForwardDecisionType
{
    Forward,
    Block,
}

public sealed class CoworkForwardDecision
{
    private CoworkForwardDecision(CoworkForwardDecisionType type, string? content = null, string? reason = null)
    {
        Type = type;
        Content = content;
        Reason = reason;
    }

    public CoworkForwardDecisionType Type { get; }
    public string? Content { get; }
    public string? Reason { get; }

    public static CoworkForwardDecision Forward(string content) => new(CoworkForwardDecisionType.Forward, content);
    public static CoworkForwardDecision Block(string reason) => new(CoworkForwardDecisionType.Block, null, reason);
}

public interface ICoworkForwardReviewer
{
    Task<CoworkForwardDecision> ReviewAsync(string from, string to, string content);
}

public sealed class CoworkMediator
{
    private readonly int _maxChars;
    private readonly ICoworkForwardReviewer? _reviewer;

    public CoworkMediator(int maxChars = 4000, ICoworkForwardReviewer? reviewer = null)
    {
        _maxChars = maxChars;
        _reviewer = reviewer;
    }

    public async Task<CoworkForwardDecision> MediateAsync(string from, string to, string content)
    {
        if (SecretScanner.ContainsSecret(content))
            return CoworkForwardDecision.Block("content appears to contain secrets");

        if (content.Length > _maxChars)
            return CoworkForwardDecision.Block($"content too large to forward ({content.Length} chars); send a summary instead");

        if (_reviewer is not null)
            return await _reviewer.ReviewAsync(from, to, content);

        return CoworkForwardDecision.Forward(content);
    }
}

public sealed class CoworkMessageBus
{
    private readonly CoworkMediator _mediator;
    private readonly IConversationEventSink _eventSink;

    public CoworkMessageBus(CoworkMediator? mediator = null, IConversationEventSink? eventSink = null)
    {
        _mediator = mediator ?? new CoworkMediator();
        _eventSink = eventSink ?? new NullConversationEventSink();
    }

    public async Task<string?> DeliverAsync(string from, string to, string content)
    {
        var decision = await _mediator.MediateAsync(from, to, content);

        if (decision.Type == CoworkForwardDecisionType.Block)
        {
            await _eventSink.AppendAsync(ConversationEventKinds.PermissionReview, ConversationEventPayloads.PermissionReview(
                "agent_forward",
                PermissionDecision.Deny,
                RiskLevel.High,
                decision.Reason ?? "forwarding blocked",
                "mediator",
                from));
            return null;
        }

        var forwarded = decision.Content ?? string.Empty;
        await _eventSink.AppendAsync(ConversationEventKinds.PermissionReview, ConversationEventPayloads.PermissionReview(
            "agent_forward",
            PermissionDecision.Allow,
            RiskLevel.Low,
            "forwarded after mediation",
            "mediator",
            from));

        await _eventSink.AppendAsync(ConversationEventKinds.AgentToAgentMessage, ConversationEventPayloads.AgentToAgentMessage(
            from,
            to,
            forwarded,
            mediated: true));

        return forwarded;
    }
}

