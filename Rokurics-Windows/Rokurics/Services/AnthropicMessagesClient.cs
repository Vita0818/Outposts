using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Rokurics.Services;

/// <summary>
/// HTTP client for Anthropic Messages API (v1/messages, v1/models).
/// Mirrors AnthropicMessagesNoteGenerationClient from Apple source.
/// </summary>
public sealed class AnthropicMessagesClient
{
    private readonly HttpClient _http;

    public AnthropicMessagesClient(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
    }

    public async Task<List<AnthropicModel>> GetModelsAsync(
        string baseUrl, string apiKey, string anthropicVersion,
        TimeSpan? timeout = null)
    {
        var request = BuildRequest(baseUrl, "/v1/models", HttpMethod.Get,
            apiKey, anthropicVersion, null, timeout);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var models = new List<AnthropicModel>();
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            foreach (var item in data.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString() ?? "";
                models.Add(new AnthropicModel { Id = id });
            }
        }
        return models;
    }

    /// <summary>
    /// Streaming Messages API — parses SSE events for content_block_delta.
    /// Mirrors Anthropic streaming from Apple source.
    /// </summary>
    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string baseUrl, string modelName, string systemPrompt, string userContent,
        string apiKey, string anthropicVersion,
        TimeSpan? timeout = null, int maxTokens = 2000, double temperature = 0.3)
    {
        var bodyObj = new
        {
            model = modelName,
            max_tokens = maxTokens,
            temperature,
            stream = true,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userContent } }
        };

        var json = JsonSerializer.Serialize(bodyObj);
        var request = BuildRequest(baseUrl, "/v1/messages", HttpMethod.Post,
            apiKey, anthropicVersion, json, timeout);

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new AnthropicMessagesClientException(
                $"HTTP {response.StatusCode}: {Truncate(errorBody)}", (int)response.StatusCode);
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") yield break;

            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var eventType))
                {
                    var type = eventType.GetString();
                    if (type == "content_block_delta" &&
                        root.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("type", out var deltaType) &&
                        deltaType.GetString() == "text_delta" &&
                        delta.TryGetProperty("text", out var text))
                    {
                        var token = text.GetString();
                        if (!string.IsNullOrEmpty(token))
                            yield return token;
                    }
                    else if (type == "message_stop")
                    {
                        yield break;
                    }
                }
            }
            catch { }
        }
    }

    public async Task<AnthropicMessageResult> SendMessageAsync(
        string baseUrl, string modelName, string systemPrompt, string userContent,
        string apiKey, string anthropicVersion,
        TimeSpan? timeout = null, int maxTokens = 2000, double temperature = 0.3)
    {
        var bodyObj = new
        {
            model = modelName,
            max_tokens = maxTokens,
            temperature,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userContent }
            }
        };

        var json = JsonSerializer.Serialize(bodyObj);
        var request = BuildRequest(baseUrl, "/v1/messages", HttpMethod.Post,
            apiKey, anthropicVersion, json, timeout);

        var response = await _http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new AnthropicMessagesClientException(
                $"HTTP {response.StatusCode}: {Truncate(responseBody)}",
                (int)response.StatusCode);
        }

        return ParseMessage(responseBody, (int)response.StatusCode);
    }

    private static HttpRequestMessage BuildRequest(
        string baseUrl, string path, HttpMethod method,
        string apiKey, string anthropicVersion,
        string? jsonBody, TimeSpan? timeout)
    {
        var fullUrl = EndpointUrl(baseUrl, path);
        var request = new HttpRequestMessage(method, fullUrl);

        if (string.IsNullOrEmpty(apiKey))
            throw new AnthropicMessagesClientException("Anthropic API key is required");
        request.Headers.Add("x-api-key", apiKey);

        if (string.IsNullOrEmpty(anthropicVersion))
            throw new AnthropicMessagesClientException("Anthropic version header is required");
        request.Headers.Add("anthropic-version", anthropicVersion);

        if (jsonBody is not null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        if (timeout.HasValue)
            request.SetTimeout(timeout.Value);

        return request;
    }

    private static Uri EndpointUrl(string baseUrl, string path)
    {
        var trimmed = baseUrl.TrimEnd('/');
        // Handle duplicates when baseUrl already includes /v1
        if (trimmed.EndsWith("/v1") && path.StartsWith("/v1"))
            path = path[3..]; // Remove leading /v1
        var relative = path.TrimStart('/');
        return new Uri($"{trimmed}/{relative}");
    }

    private static AnthropicMessageResult ParseMessage(string body, int statusCode)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        string? stopReason = null;
        if (root.TryGetProperty("stop_reason", out var sr))
            stopReason = sr.GetString();

        var texts = new List<string>();
        if (root.TryGetProperty("content", out var contentArray))
        {
            foreach (var block in contentArray.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "text")
                {
                    if (block.TryGetProperty("text", out var text))
                        texts.Add(text.GetString() ?? "");
                }
            }
        }

        var content = string.Join("", texts);
        if (string.IsNullOrEmpty(content))
        {
            if (stopReason == "max_tokens")
                throw new AnthropicMessagesClientException("content stopped by max_tokens limit", statusCode);
            throw new AnthropicMessagesClientException("empty content in response", statusCode);
        }

        return new AnthropicMessageResult
        {
            Content = content,
            StopReason = stopReason
        };
    }

    private static string Truncate(string s, int max = 500)
        => s.Length <= max ? s : s[..max] + "...";
}

public sealed class AnthropicModel
{
    public string Id { get; set; } = "";
}

public sealed class AnthropicMessageResult
{
    public string Content { get; set; } = "";
    public string? StopReason { get; set; }
    public bool IsLengthLimited => StopReason == "max_tokens";
}

public sealed class AnthropicMessagesClientException : Exception
{
    public int? StatusCode { get; }
    public AnthropicMessagesClientException(string message, int? statusCode = null)
        : base(message) => StatusCode = statusCode;
}

/// <summary>
/// Configuration for Anthropic Messages provider.
/// Mirrors AnthropicMessagesConfiguration from Apple source.
/// </summary>
public sealed class AnthropicMessagesConfiguration
{
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public string ModelName { get; set; } = "claude-sonnet-4-6";
    public string ApiKey { get; set; } = "";
    public string AnthropicVersion { get; set; } = "2023-06-01";
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 2000;
    public int MaxTranscriptCharacters { get; set; } = 12000;

    public string TrimmedBaseUrl => BaseUrl.Trim();
    public string TrimmedModelName => ModelName.Trim();
    public string TrimmedApiKey => ApiKey.Trim();
    public string EndpointDescription
    {
        get
        {
            try { return new Uri(TrimmedBaseUrl).Host; }
            catch { return TrimmedBaseUrl; }
        }
    }

    public static List<string> DefaultModelCandidates => new()
    {
        "claude-sonnet-4-6", "claude-haiku-4-5", "claude-opus-4-7"
    };
}
