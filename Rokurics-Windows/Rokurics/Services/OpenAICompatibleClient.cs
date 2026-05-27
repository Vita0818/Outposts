using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.Services;

/// <summary>
/// HTTP client for OpenAI-compatible API (chat/completions, models).
/// Mirrors OpenAICompatibleNoteGenerationClient from Apple source.
/// </summary>
public sealed class OpenAICompatibleClient
{
    private readonly HttpClient _http;

    public OpenAICompatibleClient(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
    }

    public async Task<List<OpenAICompatibleModel>> GetModelsAsync(
        string baseUrl, string? apiKey = null, TimeSpan? timeout = null)
    {
        var request = BuildRequest(baseUrl, "/models", HttpMethod.Get, apiKey, null, timeout);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var models = new List<OpenAICompatibleModel>();
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            foreach (var item in data.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString() ?? "";
                models.Add(new OpenAICompatibleModel { Id = id });
            }
        }
        return models;
    }

    public async Task<OpenAICompatibleChatResult> ChatCompletionAsync(
        string baseUrl, string modelName, List<ChatCompletionMessage> messages,
        string? apiKey = null, TimeSpan? timeout = null,
        int maxTokens = 2000, double temperature = 0.3)
    {
        var body = new
        {
            model = modelName,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            temperature,
            max_tokens = maxTokens,
            stream = false
        };

        var json = JsonSerializer.Serialize(body);
        var request = BuildRequest(baseUrl, "/chat/completions", HttpMethod.Post, apiKey, json, timeout);

        var response = await _http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new OpenAICompatibleClientException(
                $"HTTP {response.StatusCode}: {Truncate(responseBody)}",
                (int)response.StatusCode);
        }

        return ParseChatCompletion(responseBody, (int)response.StatusCode);
    }

    /// <summary>
    /// Streaming chat completion — parses SSE (server-sent events) from OpenAI-compatible endpoint.
    /// Yields content tokens as they arrive. Mirrors OpenAI streaming from Apple source.
    /// </summary>
    public async IAsyncEnumerable<string> ChatCompletionStreamAsync(
        string baseUrl, string modelName, List<ChatCompletionMessage> messages,
        string? apiKey = null, TimeSpan? timeout = null,
        int maxTokens = 2000, double temperature = 0.3)
    {
        var body = new
        {
            model = modelName,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            temperature,
            max_tokens = maxTokens,
            stream = true
        };

        var json = JsonSerializer.Serialize(body);
        var request = BuildRequest(baseUrl, "/chat/completions", HttpMethod.Post, apiKey, json, timeout);

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

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
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0) continue;

                var delta = choices[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var content))
                {
                    var token = content.GetString();
                    if (!string.IsNullOrEmpty(token))
                        yield return token;
                }

                // Check finish_reason
                if (choices[0].TryGetProperty("finish_reason", out var fr))
                {
                    var reason = fr.GetString();
                    if (!string.IsNullOrEmpty(reason) && reason != "null")
                        yield break;
                }
            }
            catch { }
        }
    }

    private static HttpRequestMessage BuildRequest(
        string baseUrl, string path, HttpMethod method,
        string? apiKey, string? jsonBody, TimeSpan? timeout)
    {
        var fullUrl = EndpointUrl(baseUrl, path);
        var request = new HttpRequestMessage(method, fullUrl);

        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

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
        var relative = path.TrimStart('/');
        return new Uri($"{trimmed}/{relative}");
    }

    private static OpenAICompatibleChatResult ParseChatCompletion(string body, int statusCode)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new OpenAICompatibleClientException("empty content in response", statusCode);
        }

        var choice = choices[0];
        var message = choice.GetProperty("message");
        var content = message.GetProperty("content").GetString() ?? "";

        string? finishReason = null;
        if (choice.TryGetProperty("finish_reason", out var fr))
            finishReason = fr.GetString();

        if (string.IsNullOrEmpty(content))
        {
            if (finishReason == "length")
                throw new OpenAICompatibleClientException("content stopped by length limit", statusCode);
            throw new OpenAICompatibleClientException("empty content in response", statusCode);
        }

        return new OpenAICompatibleChatResult
        {
            Content = content,
            FinishReason = finishReason
        };
    }

    private static string Truncate(string s, int max = 500)
        => s.Length <= max ? s : s[..max] + "...";
}

// Supporting types

public sealed class OpenAICompatibleModel
{
    public string Id { get; set; } = "";
}

public sealed class OpenAICompatibleChatResult
{
    public string Content { get; set; } = "";
    public string? FinishReason { get; set; }
    public bool IsLengthLimited => FinishReason == "length";
}

public sealed class ChatCompletionMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";

    public ChatCompletionMessage() { }
    public ChatCompletionMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

public sealed class OpenAICompatibleClientException : Exception
{
    public int? StatusCode { get; }
    public OpenAICompatibleClientException(string message, int? statusCode = null)
        : base(message) => StatusCode = statusCode;
}

/// <summary>
/// Configuration for OpenAI-compatible provider.
/// Mirrors OpenAICompatibleNoteGenerationConfiguration from Apple source.
/// </summary>
public sealed class OpenAICompatibleConfiguration
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:1234/v1";
    public string ModelName { get; set; } = "";
    public string ApiKey { get; set; } = "";
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
}
