using Rokurics.Models;

namespace Rokurics.Services;

/// <summary>
/// Mock implementations of the provider interfaces for testing/stub purposes.
/// Mirrors MockTranscriptionProvider, MockNoteGenerationProvider, MockChatProvider from source.
/// </summary>

public class MockTranscriptionProvider : ITranscriptionProvider
{
    public string Id => "mockTranscriptionProvider";
    public string DisplayName => "Mock";

    public Task ValidateConfigurationAsync() => Task.CompletedTask;

    public Task<TranscriptionResult> TranscribeAsync(TranscriptionRequest request)
    {
        var text = $"[Mock Transcript for {request.RecordingId}]\n\nThis is a mock transcription.";
        var result = new TranscriptionResult(
            request.RecordingId, text, text,
            Id, DisplayName, "mock-model", TimeSpan.FromSeconds(30));
        return Task.FromResult(result);
    }
}

public class MockNoteGenerationProvider : INoteGenerationProvider
{
    public string Id => "mockNoteProvider";
    public string DisplayName => "Mock";

    public Task ValidateConfigurationAsync() => Task.CompletedTask;

    public Task<IReadOnlyList<AvailableModelInfo>> GetAvailableModelsAsync() =>
        Task.FromResult<IReadOnlyList<AvailableModelInfo>>(new List<AvailableModelInfo>
        {
            new("mock-note-v1", "Mock Note Model v1", "mock")
        });

    public Task<NoteGenerationResult> GenerateNoteAsync(NoteGenerationRequest request)
    {
        var note = $$"""
        # {{request.NoteTitle ?? "学习笔记"}}

        ## 摘要
        这是根据转写文本生成的模拟学习笔记。

        ## 重点
        - 关键概念 1
        - 关键概念 2

        ## 复习要点
        - 复习点 1
        - 复习点 2
        """;

        var result = new NoteGenerationResult(
            request.RecordingId, note, Id, DisplayName,
            "mock-model", "stop", false);
        return Task.FromResult(result);
    }
}

public class MockChatProvider : IChatProvider
{
    public string Id => "mockChatProvider";
    public string DisplayName => "Mock";
    public HashSet<string> SupportedAttachmentKinds => new() { "image", "document", "audio", "other" };
    public bool SupportsStreaming => true; // Mock streams word-by-word

    public Task ValidateConfigurationAsync() => Task.CompletedTask;

    public Task<IReadOnlyList<AvailableModelInfo>> GetAvailableModelsAsync() =>
        Task.FromResult<IReadOnlyList<AvailableModelInfo>>(new List<AvailableModelInfo>
        {
            new("mock-chat-v1", "Mock Chat Model v1", "mock")
        });

    public Task<ChatResult> SendAsync(ChatRequest request)
    {
        var lastQuestion = request.Messages.LastOrDefault(m => m.Role == ChatMessageRole.User)?.Content ?? "";
        var contextHint = request.Context is not null && request.Context.ItemCount > 0
            ? $"已参考 {request.Context.DisplayTitle} 的 {request.Context.ItemCount} 项资料。"
            : "当前没有导入学习库上下文。";

        var content = $"{contextHint}\n\n你问的是：{lastQuestion}";
        var message = new ChatMessage(Guid.NewGuid().ToString(), ChatMessageRole.Assistant, content);

        return Task.FromResult(new ChatResult(message, Id, DisplayName, "mock", "stop", false));
    }

    public Task<string> GenerateConversationTitleAsync(ChatTitleRequest request)
    {
        var firstMessage = request.FirstUserMessages.FirstOrDefault() ?? "";
        var title = firstMessage.Length > 20 ? firstMessage[..20] : firstMessage;
        return Task.FromResult(string.IsNullOrWhiteSpace(title) ? "新对话" : title);
    }
}

public class MockRecordingUploadClient : IRecordingUploadClient
{
    public Task<RecordingUploadResult> UploadRecordingAsync(
        RecordingMetadata metadata, SecureConnectionSettings settings,
        IProgress<RecordingUploadProgressEvent>? progress = null)
    {
        progress?.Report(RecordingUploadProgressEvent.MetadataStarted);
        progress?.Report(RecordingUploadProgressEvent.MetadataSucceeded);
        progress?.Report(RecordingUploadProgressEvent.AudioStarted);
        progress?.Report(RecordingUploadProgressEvent.AudioSucceeded);

        return Task.FromResult(new RecordingUploadResult(metadata.Id));
    }
}
