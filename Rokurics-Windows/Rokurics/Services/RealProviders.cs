using Rokurics.Models;
using Rokurics.ViewModels;

namespace Rokurics.Services;

/// <summary>
/// Real AI provider implementations using HTTP clients.
/// Mirrors the provider structure from Apple source.
/// </summary>

// ─── OpenAI-compatible Note Generation ───────────────────────────

public sealed class OpenAICompatibleNoteGenerationProvider : INoteGenerationProvider
{
    private readonly OpenAICompatibleClient _client;
    private readonly OpenAICompatibleConfiguration _config;

    public string Id => "openAICompatible";
    public string DisplayName => "OpenAI-compatible";

    public OpenAICompatibleNoteGenerationProvider(
        OpenAICompatibleConfiguration config,
        OpenAICompatibleClient? client = null)
    {
        _config = config;
        _client = client ?? new OpenAICompatibleClient();
    }

    public async Task ValidateConfigurationAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TrimmedModelName))
            throw new InvalidOperationException("Model name is required");
        await _client.GetModelsAsync(_config.TrimmedBaseUrl, _config.TrimmedApiKey,
            TimeSpan.FromSeconds(10));
    }

    public async Task<IReadOnlyList<AvailableModelInfo>> GetAvailableModelsAsync()
    {
        try
        {
            var models = await _client.GetModelsAsync(_config.TrimmedBaseUrl,
                _config.TrimmedApiKey, TimeSpan.FromSeconds(10));
            return models.Select(m => new AvailableModelInfo(
                m.Id, m.Id, m.OwnedBy, m.Created)).ToList();
        }
        catch
        {
            return Array.Empty<AvailableModelInfo>();
        }
    }

    public async Task<NoteGenerationResult> GenerateNoteAsync(NoteGenerationRequest request)
    {
        var transcript = TranscriptInput(request);
        if (string.IsNullOrEmpty(transcript))
            throw new InvalidOperationException("Transcript document is missing");

        var (text, wasTruncated) = TruncateTranscript(transcript, _config.MaxTranscriptCharacters);
        var messages = BuildMessages(request, text, wasTruncated);
        var timeout = TimeSpan.FromSeconds(180);

        var result = await _client.ChatCompletionAsync(
            _config.TrimmedBaseUrl, _config.TrimmedModelName, messages,
            _config.TrimmedApiKey, timeout, _config.MaxTokens, _config.Temperature);

        var markdown = CleanMarkdown(result.Content);
        markdown = FinalNoteMarkdown(markdown, result.IsLengthLimited, wasTruncated);

        return new NoteGenerationResult(
            request.RecordingId, markdown,
            Id, DisplayName, _config.TrimmedModelName,
            result.FinishReason, result.IsLengthLimited);
    }

    // ── Transcript handling (shared with Anthropic provider) ──────

    public static string? TranscriptInput(NoteGenerationRequest request)
    {
        var markdown = request.TranscriptMarkdown?.Trim();
        if (!string.IsNullOrEmpty(markdown)) return markdown;
        return request.TranscriptText?.Trim();
    }

    public static (string text, bool wasTruncated) TruncateTranscript(
        string transcript, int maxChars)
    {
        if (transcript.Length <= maxChars)
            return (transcript, false);
        return (transcript[..maxChars], true);
    }

    public static string CleanMarkdown(string raw)
    {
        var text = raw.Trim();
        // Strip markdown code fences
        if (text.StartsWith("```markdown"))
            text = text["```markdown".Length..].TrimStart();
        else if (text.StartsWith("```"))
            text = text[3..].TrimStart();
        if (text.EndsWith("```"))
            text = text[..^3].TrimEnd();

        // Strip lines containing internal reasoning keywords
        var lines = text.Split('\n');
        var filtered = new List<string>();
        foreach (var line in lines)
        {
            var lower = line.Trim().ToLowerInvariant();
            if (lower.Contains("drafting") || lower.Contains("initial thought") ||
                lower.Contains("determine structure") || lower.Contains("analysis") ||
                lower.Contains("reasoning") || lower.Contains("let me") ||
                lower.Contains("i'll") || lower.Contains("i will"))
                continue;
            filtered.Add(line);
        }
        text = string.Join("\n", filtered).Trim();

        // Extract content after "# 录音笔记" heading if present
        var headingIdx = text.IndexOf("# 录音笔记", StringComparison.Ordinal);
        if (headingIdx >= 0)
            text = text[headingIdx..];

        return text;
    }

    // ── Prompt building ───────────────────────────────────────────

    private static readonly string SystemPrompt =
        "You are Rokurics' Chinese classroom note-taking assistant. " +
        "You receive lecture transcriptions and produce structured study notes in Chinese. " +
        "Always output in the required markdown format with the specified sections.";

    private static List<ChatCompletionMessage> BuildMessages(
        NoteGenerationRequest request, string transcript, bool wasTruncated)
    {
        var userContent = BuildUserPrompt(request, transcript, wasTruncated);
        return new List<ChatCompletionMessage>
        {
            new("system", SystemPrompt),
            new("user", userContent)
        };
    }

    public static string BuildUserPrompt(
        NoteGenerationRequest request, string transcript, bool wasTruncated)
    {
        var title = request.NoteTitle ?? "未命名录音";
        var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var truncationNote = wasTruncated
            ? "\n> 注意：转写文本过长，已截断至部分内容。\n" : "";

        return $"""
        课程录音：{title}
        日期：{date}

        {truncationNote}
        ## 转写内容

        {transcript}

        ---

        请根据以上转写内容，按照以下格式生成课堂学习笔记（使用中文）：

        # 录音笔记
        ## 摘要
        （用 3-5 句话概括整个录音的主要内容）

        ## 大纲
        （以层级列表形式呈现录音的结构化大纲）

        ## 重点
        （提炼 5-10 个最重要的知识点或概念）

        ## 待复习问题
        （针对录音内容提出 3-5 个值得复习和思考的问题）

        ## 可整理为 Kikaria 知识卡的候选内容
        （识别适合制作为闪卡的关键概念、定义、公式、重要事实等）
        """;
    }

    // ── Final note formatting ─────────────────────────────────────

    private string FinalNoteMarkdown(string modelMarkdown, bool outputTruncated, bool inputTruncated)
    {
        var header = $$"""
        # 录音笔记
        > 由 Rokurics 本地 AI 根据转写生成
        > Provider: OpenAI-compatible
        > Model: {{_config.TrimmedModelName}}

        """;

        var notices = new List<string>();
        if (outputTruncated) notices.Add("> 注意：模型输出被截断，笔记可能不完整。");
        if (inputTruncated) notices.Add("> 注意：转写文本过长，已截断处理。");
        var noticeBlock = notices.Count > 0 ? "\n" + string.Join("\n", notices) + "\n" : "";

        return header + noticeBlock + "\n" + modelMarkdown;
    }
}

// ─── Anthropic Messages Note Generation ──────────────────────────

public sealed class AnthropicMessagesNoteGenerationProvider : INoteGenerationProvider
{
    private readonly AnthropicMessagesClient _client;
    private readonly AnthropicMessagesConfiguration _config;

    public string Id => "anthropicMessages";
    public string DisplayName => "Claude / Anthropic";

    public AnthropicMessagesNoteGenerationProvider(
        AnthropicMessagesConfiguration config,
        AnthropicMessagesClient? client = null)
    {
        _config = config;
        _client = client ?? new AnthropicMessagesClient();
    }

    public async Task ValidateConfigurationAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TrimmedModelName))
            throw new InvalidOperationException("Model name is required");
        if (string.IsNullOrWhiteSpace(_config.TrimmedApiKey))
            throw new InvalidOperationException("API key is required");
        await _client.GetModelsAsync(_config.TrimmedBaseUrl, _config.TrimmedApiKey,
            _config.AnthropicVersion, TimeSpan.FromSeconds(10));
    }

    public async Task<IReadOnlyList<AvailableModelInfo>> GetAvailableModelsAsync()
    {
        try
        {
            var models = await _client.GetModelsAsync(_config.TrimmedBaseUrl,
                _config.TrimmedApiKey, _config.AnthropicVersion, TimeSpan.FromSeconds(10));
            return models.Select(m => new AvailableModelInfo(
                m.Id, m.DisplayName ?? m.Id)).ToList();
        }
        catch
        {
            return Array.Empty<AvailableModelInfo>();
        }
    }

    public async Task<NoteGenerationResult> GenerateNoteAsync(NoteGenerationRequest request)
    {
        var transcript = OpenAICompatibleNoteGenerationProvider.TranscriptInput(request);
        if (string.IsNullOrEmpty(transcript))
            throw new InvalidOperationException("Transcript document is missing");

        var (text, wasTruncated) = OpenAICompatibleNoteGenerationProvider
            .TruncateTranscript(transcript, _config.MaxTranscriptCharacters);

        var systemPrompt = SystemPromptText;
        var userContent = BuildUserPrompt(request, text, wasTruncated);
        var timeout = TimeSpan.FromSeconds(180);

        var result = await _client.SendMessageAsync(
            _config.TrimmedBaseUrl, _config.TrimmedModelName,
            systemPrompt, userContent,
            _config.TrimmedApiKey, _config.AnthropicVersion,
            timeout, _config.MaxTokens, _config.Temperature);

        var markdown = OpenAICompatibleNoteGenerationProvider.CleanMarkdown(result.Content);
        markdown = FinalNoteMarkdown(markdown, result.IsLengthLimited, wasTruncated);

        return new NoteGenerationResult(
            request.RecordingId, markdown,
            Id, DisplayName, _config.TrimmedModelName,
            result.StopReason, result.IsLengthLimited);
    }

    private static readonly string SystemPromptText =
        "You are Rokurics' Chinese classroom note-taking assistant. " +
        "You receive lecture transcriptions and produce structured study notes in Chinese. " +
        "Always output in the required markdown format with the specified sections.";

    private static string BuildUserPrompt(
        NoteGenerationRequest request, string transcript, bool wasTruncated)
    {
        return OpenAICompatibleNoteGenerationProvider.BuildUserPrompt(
            request, transcript, wasTruncated);
    }

    private string FinalNoteMarkdown(string modelMarkdown, bool outputTruncated, bool inputTruncated)
    {
        var header = $$"""
        # 录音笔记
        > 由 Rokurics AI 根据转写生成
        > Provider: Claude / Anthropic
        > Model: {{_config.TrimmedModelName}}

        """;

        var notices = new List<string>();
        if (outputTruncated) notices.Add("> 注意：模型输出被截断，笔记可能不完整。");
        if (inputTruncated) notices.Add("> 注意：转写文本过长，已截断处理。");
        var noticeBlock = notices.Count > 0 ? "\n" + string.Join("\n", notices) + "\n" : "";

        return header + noticeBlock + "\n" + modelMarkdown;
    }
}

// ─── OpenAI-compatible Chat Provider ─────────────────────────────

public sealed class OpenAICompatibleChatProvider : IChatProvider
{
    private readonly OpenAICompatibleClient _client;
    private readonly OpenAICompatibleConfiguration _config;

    public string Id => "openAICompatibleChat";
    public string DisplayName => "OpenAI-compatible";
    public HashSet<string> SupportedAttachmentKinds => new() { "image", "document" };
    public bool SupportsStreaming => true;

    public async IAsyncEnumerable<string> StreamAsync(ChatRequest request)
    {
        var messages = BuildMessages(request);
        var timeout = TimeSpan.FromSeconds(180);
        await foreach (var token in _client.ChatCompletionStreamAsync(
            _config.TrimmedBaseUrl,
            request.ModelName ?? _config.TrimmedModelName,
            messages,
            _config.TrimmedApiKey,
            timeout,
            request.MaxTokens,
            request.Temperature))
        {
            yield return token;
        }
    }

    public OpenAICompatibleChatProvider(
        OpenAICompatibleConfiguration config,
        OpenAICompatibleClient? client = null)
    {
        _config = config;
        _client = client ?? new OpenAICompatibleClient();
    }

    public async Task ValidateConfigurationAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TrimmedModelName))
            throw new InvalidOperationException("Model name is required");
        await _client.GetModelsAsync(_config.TrimmedBaseUrl, _config.TrimmedApiKey,
            TimeSpan.FromSeconds(10));
    }

    public async Task<IReadOnlyList<AvailableModelInfo>> GetAvailableModelsAsync()
    {
        try
        {
            var models = await _client.GetModelsAsync(_config.TrimmedBaseUrl,
                _config.TrimmedApiKey, TimeSpan.FromSeconds(10));
            return models.Select(m => new AvailableModelInfo(
                m.Id, m.Id, m.OwnedBy, m.Created)).ToList();
        }
        catch
        {
            return Array.Empty<AvailableModelInfo>();
        }
    }

    public async Task<ChatResult> SendAsync(ChatRequest request)
    {
        var messages = BuildMessages(request);
        var timeout = TimeSpan.FromSeconds(180);

        var result = await _client.ChatCompletionAsync(
            _config.TrimmedBaseUrl,
            request.ModelName ?? _config.TrimmedModelName,
            messages,
            _config.TrimmedApiKey,
            timeout,
            request.MaxTokens,
            request.Temperature);

        var message = new ChatMessage(
            Guid.NewGuid().ToString(),
            ChatMessageRole.Assistant,
            result.Content);

        return new ChatResult(message, Id, DisplayName,
            request.ModelName ?? _config.TrimmedModelName,
            result.FinishReason, result.IsLengthLimited);
    }

    public async Task<string> GenerateConversationTitleAsync(ChatTitleRequest request)
    {
        var messages = new List<ChatCompletionMessage>
        {
            new("system", "You are a title generator. Generate a concise title (8-20 characters) in Chinese for the conversation. Reply with ONLY the title, no other text."),
            new("user", BuildTitleUserPrompt(request))
        };

        var result = await _client.ChatCompletionAsync(
            _config.TrimmedBaseUrl, _config.TrimmedModelName, messages,
            _config.TrimmedApiKey, TimeSpan.FromSeconds(30), 50, 0.3);

        return CleanTitle(result.Content);
    }

    private static List<ChatCompletionMessage> BuildMessages(ChatRequest request)
    {
        var systemContent = "You are Rokurics, a helpful AI learning assistant. " +
            "Help the user understand and organize their study materials. " +
            "Respond in Chinese unless the user asks in another language.";

        if (request.Context is not null && request.Context.Items.Count > 0)
        {
            var contextText = FormatContext(request.Context);
            systemContent += "\n\nCurrent study context:\n" + contextText;
        }

        var messages = new List<ChatCompletionMessage> { new("system", systemContent) };

        foreach (var msg in request.Messages)
        {
            var role = msg.Role switch
            {
                ChatMessageRole.User => "user",
                ChatMessageRole.Assistant => "assistant",
                _ => "system"
            };
            messages.Add(new ChatCompletionMessage(role, msg.Content));
        }

        return messages;
    }

    private static string FormatContext(ChatContext context)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var item in context.Items)
        {
            sb.AppendLine($"### {item.Title}");
            sb.AppendLine($"来源：{item.FilingPathDisplay}");
            var content = item.Content;
            if (content.Length > 1000)
                content = content[..1000] + "...";
            sb.AppendLine(content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string BuildTitleUserPrompt(ChatTitleRequest request)
    {
        var userMessages = string.Join("\n", request.FirstUserMessages
            .Select(m => "用户：" + TruncateForTitle(m, 120)));
        var assistantReply = string.IsNullOrEmpty(request.FirstAssistantMessage)
            ? "" : "助手：" + TruncateForTitle(request.FirstAssistantMessage, 160);
        var contextLine = string.IsNullOrEmpty(request.ContextPathDisplay)
            ? "" : "上下文：" + request.ContextPathDisplay + "\n";
        return $"{contextLine}{userMessages}\n{assistantReply}";
    }

    private static string TruncateForTitle(string s, int max)
        => s.Length <= max ? s : s[..max];

    private static string CleanTitle(string raw)
    {
        var title = raw.Trim().Split('\n')[0].Trim();
        title = title.Trim('"', '\'', '《', '》', '「', '」');
        return title.Length > 24 ? title[..24] : title;
    }
}

// ─── Anthropic Messages Chat Provider ────────────────────────────

public sealed class AnthropicMessagesChatProvider : IChatProvider
{
    private readonly AnthropicMessagesClient _client;
    private readonly AnthropicMessagesConfiguration _config;

    public string Id => "anthropicMessagesChat";
    public string DisplayName => "Claude / Anthropic";
    public HashSet<string> SupportedAttachmentKinds => new() { "image", "document" };
    public bool SupportsStreaming => true;

    public async IAsyncEnumerable<string> StreamAsync(ChatRequest request)
    {
        var systemPrompt = "You are Rokurics, a helpful AI learning assistant. " +
            "Help the user understand and organize their study materials. " +
            "Respond in Chinese unless the user asks in another language.";
        if (request.Context is not null && request.Context.Items.Count > 0)
            systemPrompt += "\n\nCurrent study context:\n" + FormatAnthropicContext(request.Context);

        var userContent = BuildAnthropicUserContent(request.Messages);
        var timeout = TimeSpan.FromSeconds(180);
        await foreach (var token in _client.SendMessageStreamAsync(
            _config.TrimmedBaseUrl,
            request.ModelName ?? _config.TrimmedModelName,
            systemPrompt, userContent,
            _config.TrimmedApiKey, _config.AnthropicVersion,
            timeout, request.MaxTokens, request.Temperature))
        {
            yield return token;
        }
    }

    public AnthropicMessagesChatProvider(
        AnthropicMessagesConfiguration config,
        AnthropicMessagesClient? client = null)
    {
        _config = config;
        _client = client ?? new AnthropicMessagesClient();
    }

    public async Task ValidateConfigurationAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TrimmedModelName))
            throw new InvalidOperationException("Model name is required");
        if (string.IsNullOrWhiteSpace(_config.TrimmedApiKey))
            throw new InvalidOperationException("Anthropic API key is required");
        await _client.GetModelsAsync(_config.TrimmedBaseUrl, _config.TrimmedApiKey,
            _config.AnthropicVersion, TimeSpan.FromSeconds(10));
    }

    public async Task<IReadOnlyList<AvailableModelInfo>> GetAvailableModelsAsync()
    {
        try
        {
            var models = await _client.GetModelsAsync(_config.TrimmedBaseUrl,
                _config.TrimmedApiKey, _config.AnthropicVersion, TimeSpan.FromSeconds(10));
            return models.Select(m => new AvailableModelInfo(
                m.Id, m.DisplayName ?? m.Id)).ToList();
        }
        catch
        {
            return Array.Empty<AvailableModelInfo>();
        }
    }

    public async Task<ChatResult> SendAsync(ChatRequest request)
    {
        var systemPrompt = "You are Rokurics, a helpful AI learning assistant. " +
            "Help the user understand and organize their study materials. " +
            "Respond in Chinese unless the user asks in another language.";

        if (request.Context is not null && request.Context.Items.Count > 0)
        {
            systemPrompt += "\n\nCurrent study context:\n" + FormatAnthropicContext(request.Context);
        }

        var userContent = BuildAnthropicUserContent(request.Messages);
        var timeout = TimeSpan.FromSeconds(180);

        var result = await _client.SendMessageAsync(
            _config.TrimmedBaseUrl,
            request.ModelName ?? _config.TrimmedModelName,
            systemPrompt, userContent,
            _config.TrimmedApiKey, _config.AnthropicVersion,
            timeout, request.MaxTokens, request.Temperature);

        var message = new ChatMessage(
            Guid.NewGuid().ToString(),
            ChatMessageRole.Assistant,
            result.Content);

        return new ChatResult(message, Id, DisplayName,
            request.ModelName ?? _config.TrimmedModelName,
            result.StopReason, result.IsLengthLimited);
    }

    public async Task<string> GenerateConversationTitleAsync(ChatTitleRequest request)
    {
        var systemPrompt = "Generate a concise title (8-20 characters) in Chinese for the conversation. Reply with ONLY the title, no other text.";
        var userPrompt = BuildAnthropicTitlePrompt(request);

        var result = await _client.SendMessageAsync(
            _config.TrimmedBaseUrl, _config.TrimmedModelName,
            systemPrompt, userPrompt,
            _config.TrimmedApiKey, _config.AnthropicVersion,
            TimeSpan.FromSeconds(30), 50, 0.3);

        return CleanTitle(result.Content);
    }

    private static string FormatAnthropicContext(ChatContext context)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var item in context.Items)
        {
            sb.AppendLine($"### {item.Title}");
            sb.AppendLine($"来源：{item.FilingPathDisplay}");
            var content = item.Content;
            if (content.Length > 1000) content = content[..1000] + "...";
            sb.AppendLine(content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string BuildAnthropicUserContent(List<ChatMessage> messages)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var msg in messages)
        {
            var label = msg.Role switch
            {
                ChatMessageRole.User => "用户",
                ChatMessageRole.Assistant => "助手",
                _ => "系统"
            };
            sb.AppendLine($"{label}：{msg.Content}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private static string BuildAnthropicTitlePrompt(ChatTitleRequest request)
    {
        var userMessages = string.Join("\n", request.FirstUserMessages
            .Select(m => "用户：" + TruncateForTitle(m, 120)));
        var assistantReply = string.IsNullOrEmpty(request.FirstAssistantMessage)
            ? "" : "助手：" + TruncateForTitle(request.FirstAssistantMessage, 160);
        var contextLine = string.IsNullOrEmpty(request.ContextPathDisplay)
            ? "" : "上下文：" + request.ContextPathDisplay + "\n";
        return $"{contextLine}{userMessages}\n{assistantReply}";
    }

    private static string TruncateForTitle(string s, int max)
        => s.Length <= max ? s : s[..max];

    private static string CleanTitle(string raw)
    {
        var title = raw.Trim().Split('\n')[0].Trim();
        title = title.Trim('"', '\'', '《', '》', '「', '」');
        return title.Length > 24 ? title[..24] : title;
    }
}

// ─── Provider Factory ────────────────────────────────────────────

/// <summary>
/// Creates the correct provider based on settings.
/// Mirrors the factory pattern from Apple source.
/// </summary>
public static class ProviderFactory
{
    public static IChatProvider CreateChatProvider(AppSettings settings)
    {
        return settings.ChatProvider switch
        {
            "OpenAI-compatible" => CreateOpenAIChatProvider(settings),
            "Claude / Anthropic" => CreateAnthropicChatProvider(settings),
            _ => new MockChatProvider()
        };
    }

    public static INoteGenerationProvider CreateNoteProvider(AppSettings settings)
    {
        return settings.NoteProvider switch
        {
            "OpenAI-compatible" => CreateOpenAINoteProvider(settings),
            "Claude / Anthropic" => CreateAnthropicNoteProvider(settings),
            _ => new MockNoteGenerationProvider()
        };
    }

    private static IChatProvider CreateOpenAIChatProvider(AppSettings settings)
    {
        return new OpenAICompatibleChatProvider(new OpenAICompatibleConfiguration
        {
            BaseUrl = settings.OpenAIBaseUrl,
            ModelName = settings.OpenAIModelName,
            ApiKey = settings.OpenAIApiKey
        });
    }

    private static IChatProvider CreateAnthropicChatProvider(AppSettings settings)
    {
        return new AnthropicMessagesChatProvider(new AnthropicMessagesConfiguration
        {
            BaseUrl = settings.AnthropicBaseUrl,
            ModelName = settings.AnthropicModelName,
            ApiKey = settings.AnthropicApiKey
        });
    }

    private static INoteGenerationProvider CreateOpenAINoteProvider(AppSettings settings)
    {
        return new OpenAICompatibleNoteGenerationProvider(new OpenAICompatibleConfiguration
        {
            BaseUrl = settings.OpenAIBaseUrl,
            ModelName = settings.OpenAIModelName,
            ApiKey = settings.OpenAIApiKey
        });
    }

    private static INoteGenerationProvider CreateAnthropicNoteProvider(AppSettings settings)
    {
        return new AnthropicMessagesNoteGenerationProvider(new AnthropicMessagesConfiguration
        {
            BaseUrl = settings.AnthropicBaseUrl,
            ModelName = settings.AnthropicModelName,
            ApiKey = settings.AnthropicApiKey
        });
    }
}
