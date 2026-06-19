using System.Diagnostics;

namespace Intatis.Windows.Shared;

public sealed class ConversationSession
{
    private readonly OpenAIClient _client;
    private readonly List<IntatisMessage> _messages = new();

    public IReadOnlyList<IntatisMessage> Messages => _messages.AsReadOnly();

    public ConversationSession(IntatisConfig config)
    {
        _client = new OpenAIClient(config);
        _messages.Add(new IntatisMessage(MessageRole.System,
            "You are Intatis (Windows). Provide short, practical responses."));
    }

    public async Task<(IntatisMessage Message, TimeSpan Latency, string? Usage)> SendUserMessageAsync(
        string userText,
        string? model = null,
        string? reasoning = null,
        IReadOnlyList<ChatAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var user = new IntatisMessage(MessageRole.User, userText);
        _messages.Add(user);

        var (text, latency, usage) = await _client.SendAsync(
            _messages,
            model,
            reasoning,
            attachments,
            cancellationToken);
        var assistant = new IntatisMessage(MessageRole.Assistant, text);
        _messages.Add(assistant);
        return (assistant, latency, usage);
    }

    public void Clear()
    {
        var system = _messages.FirstOrDefault(m => m.Role == MessageRole.System);
        _messages.Clear();
        if (system is not null)
            _messages.Add(system);
    }
}
