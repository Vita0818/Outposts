using Intatis.Core.Protocol;
using Intatis.Core.Providers;
using Intatis.Core.Session;

namespace Intatis.Core;

/// <summary>
/// Tool-free chat turn engine: durable user message, streamed assistant deltas into
/// the EventLog, terminal message_completed + turn_stats + turn_outcome. Chat has no
/// tools; every tool-capable surface goes through the AgentLoop instead.
/// </summary>
public sealed class ChatLoop
{
    private readonly EventLog _log;
    private readonly IChatProvider _provider;
    private readonly string _model;
    private readonly string? _systemPrompt;
    private readonly ReasoningEffort? _reasoningEffort;
    private readonly bool _includeUsage;

    public ChatLoop(
        EventLog log,
        IChatProvider provider,
        string model,
        string? systemPrompt = null,
        ReasoningEffort? reasoningEffort = null,
        bool includeUsage = false)
    {
        _log = log;
        _provider = provider;
        _model = model;
        _systemPrompt = systemPrompt;
        _reasoningEffort = reasoningEffort;
        _includeUsage = includeUsage;
    }

    /// <summary>Returns the terminal event sequence for this turn.</summary>
    public async Task<long> SendAsync(
        string userText,
        List<ImageAttachment>? images = null,
        CancellationToken ct = default)
    {
        var turnId = TurnId.New();
        var submissionId = SubmissionId.New();
        var messageId = MessageId.New();

        // Build history from the durable projection BEFORE appending this turn's
        // user message; the current user text is added explicitly below.
        var priorMessages = BuildHistory();
        var messages = new List<ChatMessage>(priorMessages)
        {
            new() { Role = "user", Content = userText, Images = images ?? [] },
        };

        _log.Append(EventType.UserMessage, new UserMessagePayload
        {
            Text = userText,
            SubmissionId = submissionId,
            TurnId = turnId,
            Attachments = images is { Count: > 0 }
                ? images.Select(_ => ArtifactId.New().Value).ToList()
                : null,
        }.ToJson());

        try
        {
            var usage = Usage.Empty;
            var text = "";
            await foreach (var chunk in _provider.StreamChatAsync(new ChatRequest
                           {
                               Model = _model,
                               Messages = messages,
                               ReasoningEffort = _reasoningEffort,
                               IncludeUsage = _includeUsage,
                           }, ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                switch (chunk)
                {
                    case ChatChunk.Delta delta:
                        text += delta.Text;
                        _log.Append(EventType.MessageDelta, new MessageDeltaPayload
                        {
                            MessageId = messageId,
                            Role = MessageRoleWire.Assistant.ToWire(),
                            TextDelta = delta.Text,
                        }.ToJson(), flush: false);
                        break;

                    case ChatChunk.UsageReport report:
                        usage = usage.MergedWith(report.Usage);
                        break;
                }
            }

            _log.Append(EventType.MessageCompleted, new MessageCompletedPayload
            {
                MessageId = messageId,
                Role = MessageRoleWire.Assistant.ToWire(),
                Text = text,
            }.ToJson());
            _log.Append(EventType.TurnStats, new TurnStatsPayload
            {
                PromptTokens = usage.PromptTokens,
                CachedPromptTokens = usage.CachedPromptTokens,
                CompletionTokens = usage.CompletionTokens,
                TotalTokens = usage.TotalTokens,
                Model = _model,
            }.ToJson(), flush: false);
            var terminal = _log.Append(EventType.TurnOutcome, new TurnOutcomePayload
            {
                TurnId = turnId,
                Outcome = TurnOutcomeWire.Completed.ToWire(),
            }.ToJson());
            return terminal.Seq;
        }
        catch (OperationCanceledException)
        {
            // Never finalize a user-stopped partial into message_completed.
            _log.Append(EventType.TurnOutcome, new TurnOutcomePayload
            {
                TurnId = turnId,
                Outcome = TurnOutcomeWire.Interrupted.ToWire(),
                FailureSource = "turn_cancelled",
                Reason = "cancelled by user",
            }.ToJson());
            throw;
        }
        catch (Exception ex)
        {
            _log.Append(EventType.Error, new ErrorPayload
            {
                Code = "provider",
                Message = ex.Message,
            }.ToJson());
            var terminal = _log.Append(EventType.TurnOutcome, new TurnOutcomePayload
            {
                TurnId = turnId,
                Outcome = TurnOutcomeWire.Failed.ToWire(),
                FailureSource = "runtime_failed",
                Reason = ex.Message,
            }.ToJson());
            return terminal.Seq;
        }
    }

    private List<ChatMessage> BuildHistory()
    {
        // History from the canonical projection; only complete messages count.
        var projection = ConversationProjection.Build(_log.Replay());
        var messages = new List<ChatMessage>();
        if (_systemPrompt is { Length: > 0 })
            messages.Add(new ChatMessage { Role = "system", Content = _systemPrompt });
        foreach (var view in projection)
        {
            if (view.Role == MessageRoleWire.User)
                messages.Add(new ChatMessage { Role = "user", Content = view.Text });
            else if (view.Role is MessageRoleWire.Assistant or MessageRoleWire.Agent && view.IsComplete)
                messages.Add(new ChatMessage { Role = "assistant", Content = view.Text });
        }
        return messages;
    }
}

public static class TurnOutcomeWireExtensions
{
    public static string ToWire(this TurnOutcomeWire outcome) => outcome switch
    {
        TurnOutcomeWire.Completed => "completed",
        TurnOutcomeWire.Interrupted => "interrupted",
        _ => "failed",
    };
}
