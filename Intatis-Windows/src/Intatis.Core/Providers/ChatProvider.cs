namespace Intatis.Core.Providers;

public enum ReasoningEffort
{
    Minimal,
    Low,
    Medium,
    High,
}

public static class ReasoningEffortExtensions
{
    public static string ToWire(this ReasoningEffort effort) => effort switch
    {
        ReasoningEffort.Minimal => "minimal",
        ReasoningEffort.Low => "low",
        ReasoningEffort.Medium => "medium",
        _ => "high",
    };

    public static ReasoningEffort? FromWire(string? value) => value?.ToLowerInvariant() switch
    {
        "minimal" => ReasoningEffort.Minimal,
        "low" => ReasoningEffort.Low,
        "medium" => ReasoningEffort.Medium,
        "high" => ReasoningEffort.High,
        _ => null,
    };
}

public sealed record ImageAttachment(string Url);

public sealed record ChatMessage
{
    public required string Role { get; init; } // system | user | assistant
    public required string Content { get; init; }
    public List<ImageAttachment> Images { get; init; } = [];
}

public sealed record ChatRequest
{
    public required string Model { get; init; }
    public required List<ChatMessage> Messages { get; init; }
    public double? Temperature { get; init; }
    public ReasoningEffort? ReasoningEffort { get; init; }
    public bool IncludeUsage { get; init; }
}

public sealed record Usage
{
    public int? PromptTokens { get; init; }
    public int? CachedPromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens { get; init; }

    public static Usage Empty { get; } = new();

    public Usage MergedWith(Usage other) => new()
    {
        PromptTokens = other.PromptTokens ?? PromptTokens,
        CachedPromptTokens = other.CachedPromptTokens ?? CachedPromptTokens,
        CompletionTokens = other.CompletionTokens ?? CompletionTokens,
        TotalTokens = other.TotalTokens ?? TotalTokens,
    };
}

/// <summary>Streaming chunks surfaced by a chat provider (delta text, usage, completion).</summary>
public abstract record ChatChunk
{
    public sealed record Delta(string Text) : ChatChunk;
    public sealed record UsageReport(Usage Usage) : ChatChunk;
    public sealed record Done : ChatChunk;
}

public interface IChatProvider
{
    /// <summary>Must return promptly; network work happens while the sequence is enumerated.</summary>
    IAsyncEnumerable<ChatChunk> StreamChatAsync(ChatRequest request, CancellationToken ct = default);
}

public sealed class ProviderException : Exception
{
    public int? StatusCode { get; }
    public string Code { get; }

    public ProviderException(string code, string message, int? statusCode = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public bool IsRetryable => StatusCode is 408 or 429 or >= 500;
}
