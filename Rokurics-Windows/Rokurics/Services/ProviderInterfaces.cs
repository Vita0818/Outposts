namespace Rokurics.Services;

/// <summary>
/// Abstract transcription provider. Mirrors TranscriptionProvider protocol from source.
/// Implementations: WhisperCppTranscriptionProvider, MockTranscriptionProvider.
/// </summary>
public interface ITranscriptionProvider
{
    string Id { get; }
    string DisplayName { get; }
    Task ValidateConfigurationAsync();
    Task<TranscriptionResult> TranscribeAsync(TranscriptionRequest request);
}

/// <summary>
/// Abstract note generation provider. Mirrors NoteGenerationProvider protocol from source.
/// Implementations: OpenAIChatClient, AnthropicChatClient, MockNoteGenerationProvider.
/// </summary>
public interface INoteGenerationProvider
{
    string Id { get; }
    string DisplayName { get; }
    Task ValidateConfigurationAsync();
    Task<NoteGenerationResult> GenerateNoteAsync(NoteGenerationRequest request);
    Task<IReadOnlyList<AvailableModelInfo>> GetAvailableModelsAsync();
}

/// <summary>
/// Abstract chat provider. Mirrors ChatProvider protocol from source.
/// </summary>
public interface IChatProvider
{
    string Id { get; }
    string DisplayName { get; }
    HashSet<string> SupportedAttachmentKinds { get; }
    Task ValidateConfigurationAsync();
    Task<ChatResult> SendAsync(ChatRequest request);
    Task<string> GenerateConversationTitleAsync(ChatTitleRequest request);
    Task<IReadOnlyList<AvailableModelInfo>> GetAvailableModelsAsync();

    /// <summary>
    /// Stream chat response tokens as they arrive.
    /// Default implementation falls back to non-streaming SendAsync.
    /// </summary>
    async IAsyncEnumerable<string> StreamAsync(ChatRequest request)
    {
        var result = await SendAsync(request);
        yield return result.Message.Content;
    }

    /// <summary>
    /// Whether this provider supports token streaming.
    /// </summary>
    bool SupportsStreaming => false;
}

/// <summary>
/// Abstract recording upload transport. Mirrors RecordingUploadClientProtocol from source.
/// </summary>
public interface IRecordingUploadClient
{
    Task<RecordingUploadResult> UploadRecordingAsync(
        Models.RecordingMetadata metadata, SecureConnectionSettings settings,
        IProgress<RecordingUploadProgressEvent>? progress = null);
}

// Request/Result types for providers

public sealed class TranscriptionRequest
{
    public string RecordingId { get; init; }
    public string AudioFilePath { get; init; }
    public string Language { get; init; }
    public string? ModelName { get; init; }

    public TranscriptionRequest(string recordingId, string audioFilePath, string language = "auto", string? modelName = null)
    {
        RecordingId = recordingId;
        AudioFilePath = audioFilePath;
        Language = language;
        ModelName = modelName;
    }
}

public sealed class TranscriptionResult
{
    public string RecordingId { get; init; }
    public string Text { get; init; }
    public string? MarkdownText { get; init; }
    public string ProviderId { get; init; }
    public string ProviderName { get; init; }
    public string? ModelName { get; init; }
    public TimeSpan Duration { get; init; }

    public TranscriptionResult(string recordingId, string text, string? markdownText,
        string providerId, string providerName, string? modelName, TimeSpan duration)
    {
        RecordingId = recordingId;
        Text = text;
        MarkdownText = markdownText;
        ProviderId = providerId;
        ProviderName = providerName;
        ModelName = modelName;
        Duration = duration;
    }
}

public sealed class NoteGenerationRequest
{
    public string RecordingId { get; init; }
    public string TranscriptText { get; init; }
    public string? TranscriptMarkdown { get; init; }
    public string? NoteTitle { get; init; }
    public string? ModelName { get; init; }

    public NoteGenerationRequest(string recordingId, string transcriptText,
        string? transcriptMarkdown = null, string? noteTitle = null, string? modelName = null)
    {
        RecordingId = recordingId;
        TranscriptText = transcriptText;
        TranscriptMarkdown = transcriptMarkdown;
        NoteTitle = noteTitle;
        ModelName = modelName;
    }
}

public sealed class NoteGenerationResult
{
    public string RecordingId { get; init; }
    public string NoteMarkdown { get; init; }
    public string ProviderId { get; init; }
    public string ProviderName { get; init; }
    public string? ModelName { get; init; }
    public string? FinishReason { get; init; }
    public bool OutputWasTruncated { get; init; }

    public NoteGenerationResult(string recordingId, string noteMarkdown, string providerId,
        string providerName, string? modelName, string? finishReason, bool outputWasTruncated)
    {
        RecordingId = recordingId;
        NoteMarkdown = noteMarkdown;
        ProviderId = providerId;
        ProviderName = providerName;
        ModelName = modelName;
        FinishReason = finishReason;
        OutputWasTruncated = outputWasTruncated;
    }
}

public sealed class ChatRequest
{
    public List<Models.ChatMessage> Messages { get; init; }
    public Models.ChatContext? Context { get; init; }
    public string? ModelName { get; init; }
    public int MaxTokens { get; init; }
    public double Temperature { get; init; }

    public ChatRequest(List<Models.ChatMessage> messages, Models.ChatContext? context = null,
        string? modelName = null, int maxTokens = 4096, double temperature = 0.7)
    {
        Messages = messages;
        Context = context;
        ModelName = modelName;
        MaxTokens = maxTokens;
        Temperature = temperature;
    }
}

public sealed class ChatTitleRequest
{
    public List<string> FirstUserMessages { get; init; }
    public string? FirstAssistantMessage { get; init; }
    public string? ContextPathDisplay { get; init; }

    public ChatTitleRequest(List<string> firstUserMessages, string? firstAssistantMessage = null, string? contextPathDisplay = null)
    {
        FirstUserMessages = firstUserMessages;
        FirstAssistantMessage = firstAssistantMessage;
        ContextPathDisplay = contextPathDisplay;
    }
}

public sealed class ChatResult
{
    public Models.ChatMessage Message { get; init; }
    public string ProviderId { get; init; }
    public string ProviderName { get; init; }
    public string? ModelName { get; init; }
    public string? FinishReason { get; init; }
    public bool OutputWasTruncated { get; init; }

    public ChatResult(Models.ChatMessage message, string providerId, string providerName,
        string? modelName, string? finishReason, bool outputWasTruncated)
    {
        Message = message;
        ProviderId = providerId;
        ProviderName = providerName;
        ModelName = modelName;
        FinishReason = finishReason;
        OutputWasTruncated = outputWasTruncated;
    }
}

public sealed class RecordingUploadResult
{
    public string RecordingId { get; init; }
    public string? MetadataFileName { get; init; }
    public string? AudioFileName { get; init; }

    public RecordingUploadResult(string recordingId, string? metadataFileName = null, string? audioFileName = null)
    {
        RecordingId = recordingId;
        MetadataFileName = metadataFileName;
        AudioFileName = audioFileName;
    }
}

public enum RecordingUploadProgressEvent
{
    MetadataStarted,
    MetadataSucceeded,
    AudioStarted,
    AudioSucceeded,
    Failed
}

/// <summary>
/// Model metadata returned by provider GetAvailableModelsAsync.
/// Mirrors model candidates fetched from Apple source settings stores.
/// </summary>
public sealed class AvailableModelInfo
{
    public string ModelId { get; init; }
    public string DisplayName { get; init; }
    public string? OwnedBy { get; init; }
    public DateTime? CreatedAt { get; init; }

    public AvailableModelInfo(string modelId, string displayName, string? ownedBy = null, DateTime? createdAt = null)
    {
        ModelId = modelId;
        DisplayName = displayName;
        OwnedBy = ownedBy;
        CreatedAt = createdAt;
    }

    public override string ToString() => $"{DisplayName} ({ModelId})";
}

public sealed class SecureConnectionSettings
{
    public string DeviceId { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public bool IsPaired { get; set; }
    public string? SharedSecret { get; set; }

    public SecureConnectionSettings()
    {
        DeviceId = "";
        Host = "localhost";
        Port = 8787;
    }
}
