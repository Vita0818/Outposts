using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Intatis.Windows.Shared;

public sealed class OpenAIClient
{
    private readonly IntatisConfig _config;
    private readonly HttpClient _http;

    public sealed record ToolCall(string Id, string Name, string Arguments);

    public sealed record OpenAIChatMessage(
        string Role,
        string? Content,
        IReadOnlyList<ToolCall>? ToolCalls = null,
        string? ToolCallId = null,
        IReadOnlyList<ImageAttachment>? Images = null);

    public OpenAIClient(IntatisConfig config, HttpClient? httpClient = null)
    {
        _config = config;
        _http = httpClient ?? new HttpClient();
    }

    public async Task<(string Text, TimeSpan LatencyMs, string? Usage)> SendAsync(
        IReadOnlyList<IntatisMessage> messages,
        string? model = null,
        string? reasoning = null,
        IReadOnlyList<ChatAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
            throw new InvalidOperationException("INTATIS_API_KEY is required.");

        var sw = Stopwatch.StartNew();
        var uri = new Uri(new Uri(_config.BaseUrl.TrimEnd('/')), "/chat/completions");
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? _config.Model : model;
        var normalizedImageAttachments = attachments?.OfType<ImageAttachment>().ToList() ?? new List<ImageAttachment>();

        var request = new Dictionary<string, object?>
        {
            ["model"] = resolvedModel,
            ["stream"] = false,
            ["messages"] = messages.Select((message, index) =>
            {
                IReadOnlyList<ChatAttachment>? messageAttachments = null;
                if (index == messages.Count - 1 && message.Role == MessageRole.User)
                    messageAttachments = normalizedImageAttachments;

                return BuildMessagePayload(message, messageAttachments);
            }).ToList()
        };

        if (!string.IsNullOrWhiteSpace(reasoning))
            request["reasoning_effort"] = reasoning;

        using var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var req = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = content
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

        using var response = await _http.SendAsync(req, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenAI request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var usage = root.TryGetProperty("usage", out var usageEl)
            ? usageEl.GetRawText()
            : null;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new InvalidOperationException("Empty response from model provider.");

        var firstChoice = choices[0];
        var message = firstChoice.GetProperty("message");
        var contentText = ExtractContentText(message);
        return (contentText.Trim(), sw.Elapsed, usage);
    }

    public async Task<(string Text, IReadOnlyList<ToolCall> ToolCalls, TimeSpan LatencyMs, string? Usage)> SendWithToolsAsync(
        IReadOnlyList<OpenAIChatMessage> messages,
        IReadOnlyList<ToolDescriptor> tools,
        string? model = null,
        string? reasoning = null,
        bool includeUsage = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
            throw new InvalidOperationException("INTATIS_API_KEY is required.");

        var sw = Stopwatch.StartNew();
        var uri = new Uri(new Uri(_config.BaseUrl.TrimEnd('/')), "/chat/completions");
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? _config.Model : model;

        var request = new Dictionary<string, object?>
        {
            ["model"] = resolvedModel,
            ["stream"] = false,
            ["messages"] = messages.Select(BuildMessagePayload).ToList(),
            ["tools"] = tools.Select(t => t.ToOpenAiDefinition()).ToList(),
            ["tool_choice"] = "auto",
        };

        if (!string.IsNullOrWhiteSpace(reasoning))
            request["reasoning_effort"] = reasoning;

        if (includeUsage)
            request["stream_options"] = new Dictionary<string, object?> { ["include_usage"] = true };

        using var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var req = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = content
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

        using var response = await _http.SendAsync(req, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
            throw new HttpException(response.StatusCode, body);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var usage = root.TryGetProperty("usage", out var usageEl)
            ? usageEl.GetRawText()
            : null;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new InvalidOperationException("Empty response from model provider.");

        var firstChoice = choices[0];
        var message = firstChoice.GetProperty("message");
        var contentText = ExtractContentText(message);
        var calls = ParseToolCalls(message);
        return (contentText, calls, sw.Elapsed, usage);
    }

    private static object BuildMessagePayload(IntatisMessage message, IReadOnlyList<ChatAttachment>? attachments)
    {
        var role = message.Role.ToString().ToLowerInvariant();
        var images = attachments?.OfType<ImageAttachment>().ToList() ?? new List<ImageAttachment>();

        if (images.Length == 0)
        {
            return new Dictionary<string, object?>
            {
                ["role"] = role,
                ["content"] = message.Content
            };
        }

        var content = new List<Dictionary<string, object?>>(images.Count + 1);
        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            content.Add(new Dictionary<string, object?>
            {
                ["type"] = "text",
                ["text"] = message.Content
            });
        }

        foreach (var image in images)
        {
            content.Add(new Dictionary<string, object?>
            {
                ["type"] = "image_url",
                ["image_url"] = new Dictionary<string, object?> { ["url"] = image.Url }
            });
        }

        return new Dictionary<string, object?>
        {
            ["role"] = role,
            ["content"] = content
        };
    }

    private static object BuildMessagePayload(OpenAIChatMessage message)
    {
        var payload = new Dictionary<string, object?>
        {
            ["role"] = message.Role
        };

        var content = BuildMessageContent(message.Content, message.Images);

        if (content is not null)
        {
            payload["content"] = content;
        }
        else if (message.ToolCalls is { Count: > 0 })
        {
            payload["content"] = null;
        }

        if (message.ToolCalls is { Count: > 0 })
        {
            var calls = new List<Dictionary<string, object?>>();
            for (var i = 0; i < message.ToolCalls.Count; i++)
            {
                var toolCall = message.ToolCalls[i];
                calls.Add(new Dictionary<string, object?>
                {
                    ["id"] = toolCall.Id,
                    ["type"] = "function",
                    ["function"] = new Dictionary<string, object?>
                    {
                        ["name"] = toolCall.Name,
                        ["arguments"] = toolCall.Arguments,
                    },
                });
            }
            payload["tool_calls"] = calls;
        }

        if (!string.IsNullOrWhiteSpace(message.ToolCallId))
            payload["tool_call_id"] = message.ToolCallId;

        return payload;
    }

    private static object? BuildMessageContent(string? text, IReadOnlyList<ImageAttachment>? images)
    {
        var imageList = images?.ToList() ?? new List<ImageAttachment>();
        if (imageList.Count == 0)
            return text;

        var content = new List<Dictionary<string, object?>>(imageList.Count + 1);
        if (!string.IsNullOrWhiteSpace(text))
        {
            content.Add(new Dictionary<string, object?>
            {
                ["type"] = "text",
                ["text"] = text
            });
        }

        foreach (var image in imageList)
        {
            content.Add(new Dictionary<string, object?>
            {
                ["type"] = "image_url",
                ["image_url"] = new Dictionary<string, object?> { ["url"] = image.Url }
            });
        }

        return content;
    }

    private static string ExtractContentText(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
            return string.Empty;

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString() ?? string.Empty,
            JsonValueKind.Array => ExtractTextFromContentArray(content),
            _ => content.GetRawText()
        };
    }

    private static string ExtractTextFromContentArray(JsonElement content)
    {
        var output = new StringBuilder();
        foreach (var item in content.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            if (!item.TryGetProperty("type", out var type) || type.GetString() != "text")
                continue;
            if (item.TryGetProperty("text", out var text))
                output.Append(text.GetString());
        }
        return output.ToString();
    }

    private static List<ToolCall> ParseToolCalls(JsonElement message)
    {
        if (!message.TryGetProperty("tool_calls", out var toolCallsEl) || toolCallsEl.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<ToolCall>();
        foreach (var toolCall in toolCallsEl.EnumerateArray())
        {
            if (toolCall.ValueKind != JsonValueKind.Object)
                continue;

            if (!toolCall.TryGetProperty("function", out var functionEl) || functionEl.ValueKind != JsonValueKind.Object)
                continue;

            if (!functionEl.TryGetProperty("name", out var nameEl) ||
                nameEl.ValueKind != JsonValueKind.String)
                continue;

            var name = nameEl.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var args = functionEl.TryGetProperty("arguments", out var argsEl)
                ? argsEl.GetRawText().Trim()
                : "{}";
            if (argsEl.ValueKind == JsonValueKind.String)
                args = argsEl.GetString() ?? "{}";

            var id = toolCall.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                ? idEl.GetString() ?? $"call_{result.Count}"
                : $"call_{result.Count}";

            result.Add(new ToolCall(id, name, args));
        }

        return result;
    }
}

public sealed class HttpException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public HttpException(HttpStatusCode statusCode, string message)
        : base($"OpenAI request failed: {(int)statusCode}. {message}")
    {
        StatusCode = statusCode;
    }
}
