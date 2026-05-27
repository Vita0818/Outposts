namespace Rokurics.Models;

/// <summary>
/// Chat message, conversation, context models. Mirrors ChatModels from source.
/// </summary>

public enum ChatMessageRole
{
    System,
    User,
    Assistant
}

public sealed class ChatMessage
{
    public string Id { get; set; }
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> AttachmentIds { get; set; }

    public ChatMessage()
    {
        Id = Guid.NewGuid().ToString();
        Content = "";
        AttachmentIds = new List<string>();
    }

    public ChatMessage(string id, ChatMessageRole role, string content, DateTime? createdAt = null, List<string>? attachmentIds = null)
    {
        Id = id;
        Role = role;
        Content = content;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        AttachmentIds = attachmentIds ?? new List<string>();
    }
}

public enum ChatConversationTitleSource
{
    Manual,
    AiGenerated,
    Fallback
}

public enum ChatAttachmentKind
{
    Image,
    Document,
    Audio,
    Other
}

public sealed class ChatAttachment
{
    public string Id { get; set; }
    public string ConversationId { get; set; }
    public string FileName { get; set; }
    public string FileType { get; set; }
    public string? MimeType { get; set; }
    public string RelativePath { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public ChatAttachmentKind Kind { get; set; }

    public ChatAttachment()
    {
        Id = Guid.NewGuid().ToString();
        ConversationId = "";
        FileName = "attachment";
        FileType = "";
        RelativePath = "";
    }
}

public sealed class ChatConversation
{
    public string Id { get; set; }
    public string Title { get; set; }
    public DateTime? TitleGeneratedAt { get; set; }
    public ChatConversationTitleSource TitleSource { get; set; }
    public List<ChatMessage> Messages { get; set; }
    public string? ActiveContextId { get; set; }
    public string? ContextPathDisplay { get; set; }
    public int? ContextItemCount { get; set; }
    public string? LastMessagePreview { get; set; }
    public List<string> AttachmentIds { get; set; }
    public List<ChatAttachment> Attachments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsEmpty => Messages.Count == 0 && Attachments.Count == 0;

    public ChatConversation()
    {
        Id = Guid.NewGuid().ToString();
        Title = "新对话";
        TitleSource = ChatConversationTitleSource.Fallback;
        Messages = new List<ChatMessage>();
        AttachmentIds = new List<string>();
        Attachments = new List<ChatAttachment>();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public string DisplayTitle => Title;

    public ChatConversation WithMessage(ChatMessage message)
    {
        var conv = Clone();
        conv.Messages = new List<ChatMessage>(Messages) { message };
        conv.UpdatedAt = DateTime.UtcNow;
        if (conv.Messages.Count == 1)
            conv.LastMessagePreview = Truncate(message.Content, 80);
        return conv;
    }

    private ChatConversation Clone()
    {
        return new ChatConversation
        {
            Id = Id, Title = Title, TitleGeneratedAt = TitleGeneratedAt, TitleSource = TitleSource,
            Messages = new List<ChatMessage>(Messages), ActiveContextId = ActiveContextId,
            ContextPathDisplay = ContextPathDisplay, ContextItemCount = ContextItemCount,
            LastMessagePreview = LastMessagePreview, AttachmentIds = new List<string>(AttachmentIds),
            Attachments = new List<ChatAttachment>(Attachments), CreatedAt = CreatedAt, UpdatedAt = UpdatedAt
        };
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..(maxLen - 3)] + "...";
}

public sealed class ChatContext
{
    public string Id { get; set; }
    public string Title { get; set; }
    public List<string> BrowsePathComponents { get; set; }
    public int ItemCount { get; set; }
    public List<ChatContextItem> Items { get; set; }
    public string? ContextPathDisplay { get; set; }
    public int MaxContextCharacters { get; set; }
    public int TotalCharacterCount { get; set; }
    public bool IsTruncated { get; set; }

    public ChatContext()
    {
        Id = Guid.NewGuid().ToString();
        Title = "学习库";
        BrowsePathComponents = new List<string>();
        Items = new List<ChatContextItem>();
        MaxContextCharacters = 20000;
    }

    public string DisplayTitle => Title;

    public string PathDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ContextPathDisplay)) return ContextPathDisplay;
            var components = new List<string> { "学习库" };
            components.AddRange(BrowsePathComponents.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()));
            return string.Join(" / ", components);
        }
    }
}

public sealed class ChatContextItem
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string FilingPathDisplay { get; set; }
    public string Content { get; set; }
    public string? SourcePath { get; set; }
    public int ContentCharacterCount { get; set; }
    public bool IsTruncated { get; set; }

    public ChatContextItem()
    {
        Id = "";
        Title = "未命名知识";
        FilingPathDisplay = "";
        Content = "";
    }

    public ChatContextItem(string id, string title, string filingPathDisplay, string content, string? sourcePath = null, bool isTruncated = false)
    {
        Id = id;
        Title = string.IsNullOrWhiteSpace(title) ? "未命名知识" : title;
        FilingPathDisplay = filingPathDisplay;
        Content = content.Trim();
        SourcePath = sourcePath?.Trim() is { Length: > 0 } s ? s : null;
        ContentCharacterCount = Content.Length;
        IsTruncated = isTruncated;
    }
}
