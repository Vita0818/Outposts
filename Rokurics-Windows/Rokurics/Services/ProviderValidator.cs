using System.Text.RegularExpressions;

namespace Rokurics.Services;

/// <summary>
/// Provider configuration and request/response validation.
/// Validates structure, formats, and request integrity without requiring
/// real API keys. Can be used for pre-flight checks before making live calls.
///
/// Mirrors validation patterns from Apple source provider configuration
/// validators and transcription configuration validator.
/// </summary>
public static class ProviderValidator
{
    // ── URL Validation ────────────────────────────────────────────

    /// <summary>
    /// Validate a base URL for provider API endpoints.
    /// </summary>
    public static ValidationResult ValidateBaseUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ValidationResult.Fail("Base URL 不能为空");

        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://") && !trimmed.StartsWith("https://"))
            return ValidationResult.Fail("Base URL 必须以 http:// 或 https:// 开头");

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return ValidationResult.Fail("Base URL 格式无效");

        if (uri.HostNameType == UriHostNameType.Unknown)
            return ValidationResult.Fail("Base URL 主机名无效");

        // Localhost is valid for LM Studio / Ollama
        if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "0.0.0.0")
            return ValidationResult.Ok("本地地址 — 适用于 LM Studio / Ollama");

        return ValidationResult.Ok("Base URL 格式有效");
    }

    // ── API Key Validation ────────────────────────────────────────

    /// <summary>
    /// Validate API key format without checking server validity.
    /// Checks for common prefix patterns and minimum length.
    /// </summary>
    public static ValidationResult ValidateApiKey(string? key, string providerKind)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ValidationResult.Fail("API Key 不能为空");

        var trimmed = key.Trim();

        if (trimmed.Length < 8)
            return ValidationResult.Fail("API Key 太短（最少 8 个字符）");

        return providerKind switch
        {
            "anthropic" => ValidateAnthropicApiKey(trimmed),
            "openai" => ValidateOpenAiApiKey(trimmed),
            "openai-compatible" => ValidationResult.Ok("API Key 格式有效（OpenAI-compatible）"),
            _ => ValidationResult.Ok("API Key 已配置")
        };
    }

    private static ValidationResult ValidateAnthropicApiKey(string key)
    {
        if (!key.StartsWith("sk-ant-"))
            return ValidationResult.Warn(
                "Anthropic API Key 通常以 'sk-ant-' 开头，当前格式可能不正确");

        if (key.Length < 20)
            return ValidationResult.Fail("Anthropic API Key 格式无效（长度不足）");

        return ValidationResult.Ok("Anthropic API Key 格式有效");
    }

    private static ValidationResult ValidateOpenAiApiKey(string key)
    {
        if (!key.StartsWith("sk-"))
            return ValidationResult.Warn(
                "OpenAI API Key 通常以 'sk-' 开头，当前格式可能不正确（如果是兼容服务则忽略）");

        if (key.Length < 20)
            return ValidationResult.Fail("API Key 格式无效（长度不足）");

        return ValidationResult.Ok("API Key 格式有效");
    }

    // ── Model Name Validation ─────────────────────────────────────

    public static ValidationResult ValidateModelName(string? name, string providerKind)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Fail("模型名称不能为空");

        var trimmed = name.Trim();

        if (trimmed.Length > 128)
            return ValidationResult.Fail("模型名称过长（最多 128 字符）");

        // Check for common injection patterns
        if (trimmed.Contains('\n') || trimmed.Contains('\r') || trimmed.Contains('\0'))
            return ValidationResult.Fail("模型名称包含非法字符");

        return providerKind switch
        {
            "anthropic" => trimmed.StartsWith("claude-")
                ? ValidationResult.Ok("Claude 模型名称格式有效")
                : ValidationResult.Warn("Anthropic 模型名称通常以 'claude-' 开头"),
            _ => ValidationResult.Ok("模型名称已配置")
        };
    }

    // ── Full Configuration Validation ─────────────────────────────

    /// <summary>
    /// Validate a complete provider configuration.
    /// Returns all issues found.
    /// </summary>
    public static List<ValidationResult> ValidateConfiguration(
        string? baseUrl, string? apiKey, string? modelName,
        string providerKind, string? anthropicVersion = null)
    {
        var results = new List<ValidationResult>();

        var urlResult = ValidateBaseUrl(baseUrl);
        results.Add(urlResult);

        var keyResult = ValidateApiKey(apiKey, providerKind);
        results.Add(keyResult);

        var modelResult = ValidateModelName(modelName, providerKind);
        results.Add(modelResult);

        if (providerKind == "anthropic")
        {
            if (string.IsNullOrWhiteSpace(anthropicVersion))
                results.Add(ValidationResult.Warn(
                    "Anthropic Version header 未设置（将使用默认值 2023-06-01）"));
            else
                results.Add(ValidationResult.Ok("Anthropic Version 已配置"));
        }

        return results;
    }

    // ── Request Structure Validation ──────────────────────────────

    /// <summary>
    /// Validate a Chat Completions request structure without sending it.
    /// Checks message format, token limits, temperature range.
    /// </summary>
    public static ValidationResult ValidateChatRequest(
        List<ChatCompletionMessage> messages, int maxTokens, double temperature)
    {
        if (messages.Count == 0)
            return ValidationResult.Fail("消息列表不能为空");

        if (messages.All(m => m.Role != "user"))
            return ValidationResult.Fail("至少需要一条用户消息");

        if (maxTokens < 1)
            return ValidationResult.Fail("maxTokens 必须为正数");

        if (maxTokens > 1_000_000)
            return ValidationResult.Warn("maxTokens 超过 1M，可能导致请求失败");

        if (temperature < 0 || temperature > 2.0)
            return ValidationResult.Warn("temperature 应在 0.0–2.0 范围内");

        // Check for empty message content
        var emptyMessages = messages.Where(m =>
            string.IsNullOrWhiteSpace(m.Content) &&
            m.Role != "system").ToList();
        if (emptyMessages.Count > 0)
            return ValidationResult.Warn($"{emptyMessages.Count} 条消息内容为空");

        return ValidationResult.Ok("请求结构验证通过");
    }

    /// <summary>
    /// Validate an Anthropic Messages request structure.
    /// </summary>
    public static ValidationResult ValidateAnthropicMessageRequest(
        string systemPrompt, string userContent, int maxTokens, double temperature)
    {
        if (string.IsNullOrWhiteSpace(userContent))
            return ValidationResult.Fail("用户消息不能为空");

        if (maxTokens < 1 || maxTokens > 1_000_000)
            return ValidationResult.Fail("maxTokens 必须在 1–1,000,000 范围内");

        if (temperature < 0 || temperature > 1.0)
            return ValidationResult.Warn("Anthropic temperature 应在 0.0–1.0 范围内");

        return ValidationResult.Ok("Anthropic 请求结构验证通过");
    }

    // ── Response Validation ───────────────────────────────────────

    /// <summary>
    /// Validate a provider response for required fields and error indicators.
    /// Works on parsed responses without making live calls.
    /// </summary>
    public static ValidationResult ValidateChatResponse(
        string? content, string? finishReason, int statusCode)
    {
        if (statusCode < 200 || statusCode >= 300)
            return ValidationResult.Fail($"HTTP {statusCode}: 服务器返回错误状态");

        if (string.IsNullOrWhiteSpace(content))
            return ValidationResult.Fail("响应内容为空");

        if (content.Length > 1_000_000)
            return ValidationResult.Warn("响应内容超过 1MB，可能影响性能");

        if (finishReason == "length" || finishReason == "max_tokens")
            return ValidationResult.Warn("模型输出因长度限制被截断 (finish_reason: length)");

        return ValidationResult.Ok("响应验证通过");
    }

    // ── Diagnostic Summary ────────────────────────────────────────

    /// <summary>
    /// Generate a human-readable diagnostic summary for a provider configuration.
    /// </summary>
    public static string DiagnosticSummary(string providerName,
        string? baseUrl, string? modelName, string? apiKey)
    {
        var parts = new List<string>
        {
            $"Provider: {providerName}",
            $"Base URL: {(string.IsNullOrWhiteSpace(baseUrl) ? "未配置" : MaskUrl(baseUrl))}",
            $"模型: {(string.IsNullOrWhiteSpace(modelName) ? "未配置" : modelName)}",
            $"API Key: {(string.IsNullOrWhiteSpace(apiKey) ? "未配置" : MaskApiKey(apiKey))}"
        };
        return string.Join("\n", parts);
    }

    private static string MaskUrl(string url)
    {
        if (url.Length <= 30) return url;
        return url[..30] + "...";
    }

    private static string MaskApiKey(string key)
        => key.Length <= 8 ? "****" : key[..4] + "****" + key[^4..];

    // ── Pre-flight Connectivity Check Model ───────────────────────

    /// <summary>
    /// Test whether a provider endpoint is reachable (without authentication).
    /// Times out after specified duration. Returns success/failure without
    /// exposing response content.
    /// </summary>
    public static async Task<ValidationResult> CheckEndpointReachableAsync(
        string baseUrl, TimeSpan timeout)
    {
        try
        {
            using var client = new HttpClient { Timeout = timeout };
            client.DefaultRequestHeaders.Add("User-Agent", "Rokurics-Windows/1.0");
            var response = await client.GetAsync(baseUrl, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? ValidationResult.Ok($"端点可达 (HTTP {(int)response.StatusCode})")
                : ValidationResult.Warn($"端点响应 HTTP {(int)response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return ValidationResult.Fail($"连接超时 ({timeout.TotalSeconds:F0}s)");
        }
        catch (HttpRequestException ex)
        {
            return ValidationResult.Fail($"网络错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"连接失败: {ex.Message}");
        }
    }
}

// ── Validation Result Type ────────────────────────────────────────

public sealed class ValidationResult
{
    public enum Severity { Ok, Warning, Error }

    public Severity Level { get; init; }
    public string Message { get; init; } = "";
    public bool IsSuccess => Level == Severity.Ok;
    public bool IsWarning => Level == Severity.Warning;

    public static ValidationResult Ok(string message) => new()
    {
        Level = Severity.Ok,
        Message = message
    };

    public static ValidationResult Warn(string message) => new()
    {
        Level = Severity.Warning,
        Message = message
    };

    public static ValidationResult Fail(string message) => new()
    {
        Level = Severity.Error,
        Message = message
    };

    public override string ToString()
        => Level switch
        {
            Severity.Ok => $"✓ {Message}",
            Severity.Warning => $"⚠ {Message}",
            Severity.Error => $"✗ {Message}",
            _ => Message
        };
}
