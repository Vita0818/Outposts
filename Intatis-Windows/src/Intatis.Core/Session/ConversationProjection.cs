using System.Text.Json.Nodes;
using Intatis.Core.Protocol;

namespace Intatis.Core.Session;

public sealed record ChatMessageView
{
    public string MessageId { get; init; } = "";
    public MessageRoleWire Role { get; init; }
    public string? Agent { get; init; }
    public string Text { get; init; } = "";
    public bool IsComplete { get; init; }
    public DateTime Timestamp { get; init; }
    public int AttachmentCount { get; init; }
    public List<MessageCitation> Citations { get; init; } = [];
}

/// <summary>
/// Folds envelopes into the chat text view. The UI only ever consumes this folded
/// projection, never raw model output as truth. Unknown event types are ignored.
/// </summary>
public static class ConversationProjection
{
    public static List<ChatMessageView> Build(IEnumerable<Envelope> envelopes)
    {
        var messages = new List<ChatMessageView>();
        ChatMessageView? open = null;
        foreach (var envelope in envelopes)
        {
            switch (envelope.Type)
            {
                case EventType.UserMessage:
                    Flush(ref open, messages);
                    var user = UserMessagePayload.FromJson(envelope.Payload);
                    messages.Add(new ChatMessageView
                    {
                        MessageId = "user_" + envelope.Seq,
                        Role = MessageRoleWire.User,
                        Text = user.Text,
                        IsComplete = true,
                        Timestamp = envelope.Ts,
                        AttachmentCount = user.Attachments?.Count ?? 0,
                    });
                    break;

                case EventType.MessageDelta:
                    var delta = ParseDelta(envelope.Payload);
                    if (open is null || open.MessageId != delta.MessageId)
                    {
                        Flush(ref open, messages);
                        open = new ChatMessageView
                        {
                            MessageId = delta.MessageId,
                            Role = MessageRoleWireExtensions.FromWire(delta.Role),
                            Agent = delta.Agent,
                            Timestamp = envelope.Ts,
                        };
                    }
                    open = open with { Text = open.Text + delta.TextDelta };
                    break;

                case EventType.MessageCompleted:
                    var completed = ParseCompleted(envelope.Payload);
                    if (open is not null && open.MessageId == completed.MessageId)
                        messages.Add(open with { Text = completed.Text, IsComplete = true, Citations = completed.Citations });
                    else
                        messages.Add(new ChatMessageView
                        {
                            MessageId = completed.MessageId,
                            Role = MessageRoleWireExtensions.FromWire(completed.Role),
                            Agent = completed.Agent,
                            Text = completed.Text,
                            IsComplete = true,
                            Timestamp = envelope.Ts,
                            Citations = completed.Citations,
                        });
                    open = null;
                    break;

                case EventType.Error:
                    Flush(ref open, messages);
                    var code = (string?)envelope.Payload?["code"] ?? "error";
                    var message = (string?)envelope.Payload?["message"] ?? "unknown error";
                    messages.Add(new ChatMessageView
                    {
                        MessageId = "error_" + envelope.Seq,
                        Role = MessageRoleWire.System,
                        Text = $"[{code}] {message}",
                        IsComplete = true,
                        Timestamp = envelope.Ts,
                    });
                    break;
            }
        }
        Flush(ref open, messages);
        return messages;
    }

    private static void Flush(ref ChatMessageView? open, List<ChatMessageView> messages)
    {
        if (open is not null) messages.Add(open);
        open = null;
    }

    private static MessageDeltaPayload ParseDelta(JsonNode? node)
    {
        var o = node as JsonObject;
        return new MessageDeltaPayload
        {
            MessageId = (string?)o?["message_id"] ?? "",
            Role = (string?)o?["role"] ?? "assistant",
            Agent = (string?)o?["agent"],
            TextDelta = (string?)o?["text_delta"] ?? "",
        };
    }

    private static MessageCompletedPayload ParseCompleted(JsonNode? node)
    {
        var o = node as JsonObject;
        var citations = new List<MessageCitation>();
        if (o?["citations"] is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is null) continue;
                citations.Add(new MessageCitation
                {
                    Url = (string?)item["url"] ?? "",
                    Title = (string?)item["title"] ?? "",
                });
            }
        }
        return new MessageCompletedPayload
        {
            MessageId = (string?)o?["message_id"] ?? "",
            Role = (string?)o?["role"] ?? "assistant",
            Agent = (string?)o?["agent"],
            Text = (string?)o?["text"] ?? "",
            Citations = citations,
        };
    }
}
