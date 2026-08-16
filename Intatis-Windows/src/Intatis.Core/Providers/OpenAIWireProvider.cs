using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Intatis.Core.Protocol;

namespace Intatis.Core.Providers;

// ---------------------------------------------------------------------------
// Tool-calling wire types (shared by Chat and the Code/Cowork agent loops).
// ---------------------------------------------------------------------------

public sealed record ToolSpec
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public JsonObject Parameters { get; init; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
    };
}

public sealed record AgentToolCall
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Arguments { get; init; } // raw JSON string
}

public sealed record AgentMessage
{
    public required string Role { get; init; } // system | user | assistant | tool
    public string? Content { get; init; }
    public List<AgentToolCall>? ToolCalls { get; init; }
    public string? ToolCallId { get; init; }
    public List<ImageAttachment> Images { get; init; } = [];
}

public sealed record AgentRequest
{
    public required string Model { get; init; }
    public required List<AgentMessage> Messages { get; init; }
    public List<ToolSpec> Tools { get; init; } = [];
    public double? Temperature { get; init; }
    public ReasoningEffort? ReasoningEffort { get; init; }
    public bool IncludeUsage { get; init; }
    public bool ParallelToolCalls { get; init; } = false;
}

public abstract record AgentChunk
{
    public sealed record TextDelta(string Text) : AgentChunk;
    public sealed record ToolCallsIssued(List<AgentToolCall> Calls) : AgentChunk;
    public sealed record UsageReport(Usage Usage) : AgentChunk;
    public sealed record Done(string FinishReason) : AgentChunk;
}

public interface IToolCallingProvider
{
    IAsyncEnumerable<AgentChunk> StreamAgentAsync(AgentRequest request, CancellationToken ct = default);
}

// ---------------------------------------------------------------------------
// OpenAI-compatible HTTP adapter (chat/completions, SSE streaming).
// ---------------------------------------------------------------------------

public sealed class OpenAIWireProvider : IChatProvider, IToolCallingProvider
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string? _chatEndpointOverride;
    private readonly string _apiKey;

    public string BaseUrl => _baseUrl;

    public OpenAIWireProvider(HttpClient http, string baseUrl, string apiKey, string? chatEndpointOverride = null)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
        _chatEndpointOverride = chatEndpointOverride;
        _apiKey = apiKey;
    }

    public static string ChatCompletionsUrl(string baseUrl, string? overrideEndpoint)
        => string.IsNullOrWhiteSpace(overrideEndpoint)
            ? baseUrl.TrimEnd('/') + "/chat/completions"
            : overrideEndpoint;

    public IAsyncEnumerable<ChatChunk> StreamChatAsync(ChatRequest request, CancellationToken ct = default)
        => StreamCoreAsync(ToWire(request), chunk => chunk switch
        {
            AgentChunk.TextDelta delta => (ChatChunk?)new ChatChunk.Delta(delta.Text),
            AgentChunk.UsageReport usage => new ChatChunk.UsageReport(usage.Usage),
            AgentChunk.Done => new ChatChunk.Done(),
            _ => null,
        }, ct);

    public IAsyncEnumerable<AgentChunk> StreamAgentAsync(AgentRequest request, CancellationToken ct = default)
        => StreamCoreAsync(request, chunk => chunk, ct);

    private async IAsyncEnumerable<TOut> StreamCoreAsync<TOut>(
        AgentRequest request,
        Func<AgentChunk, TOut?> map,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var sawCompletionMarker = false;
        await foreach (var chunk in OpenStream(request, ct).WithCancellation(ct))
        {
            if (chunk is AgentChunk.Done) sawCompletionMarker = true;
            var mapped = map(chunk);
            if (mapped is not null) yield return mapped;
        }
        if (!sawCompletionMarker)
            throw new ProviderException("incomplete_stream", "stream ended without a completion marker");
    }

    private async IAsyncEnumerable<AgentChunk> OpenStream(
        AgentRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = ChatCompletionsUrl(_baseUrl, _chatEndpointOverride);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https") ||
            string.IsNullOrEmpty(uri.Host))
            throw new ProviderException("config", $"invalid chat endpoint: {url}");

        var body = BuildRequestBody(request);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Accept.ParseAdd("text/event-stream");
        if (!string.IsNullOrEmpty(_apiKey))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (errorBody.Length > 2048) errorBody = errorBody[..2048];
            throw new ProviderException("provider.http",
                $"HTTP {(int)response.StatusCode}: {errorBody}", (int)response.StatusCode);
        }

        await using var byteStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(byteStream, Encoding.UTF8);
        var parser = new SseParser();

        var buffer = new char[8192];
        var toolAccumulator = new Dictionary<int, (string? Id, string Name, StringBuilder Args)>();
        var finishReason = "stop";
        var finishReasonSeen = false;

        int read;
        while ((read = await reader.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            foreach (var dataEvent in parser.Consume(buffer.AsSpan(0, read)))
            {
                if (dataEvent == "[DONE]")
                {
                    yield return new AgentChunk.Done(finishReason);
                    yield break;
                }

                var node = JsonNode.Parse(dataEvent);
                if (node is null) continue;

                var delta = node["choices"]?[0]?["delta"];
                if (delta?["content"] is { } textNode && (string?)textNode is { Length: > 0 } text)
                    yield return new AgentChunk.TextDelta(text);

                if (delta?["tool_calls"] is JsonArray toolCallArray)
                {
                    foreach (var callNode in toolCallArray)
                    {
                        if (callNode is null) continue;
                        var index = (int?)callNode["index"] ?? 0;
                        var id = (string?)callNode["id"];
                        var fn = callNode["function"];
                        var name = (string?)fn?["name"];
                        var argsDelta = (string?)fn?["arguments"];
                        if (!toolAccumulator.TryGetValue(index, out var acc))
                        {
                            acc = (id, name ?? "", new StringBuilder());
                            toolAccumulator[index] = acc;
                        }
                        if (id is not null) acc.Item1 = id;
                        if (name is { Length: > 0 }) acc.Item2 = name;
                        if (argsDelta is { Length: > 0 }) acc.Item3.Append(argsDelta);
                    }
                }

                var reason = (string?)node["choices"]?[0]?["finish_reason"];
                if (reason is { Length: > 0 } && !finishReasonSeen)
                {
                    finishReason = reason;
                    finishReasonSeen = true;
                    foreach (var accumulated in EmitToolCalls(toolAccumulator))
                        yield return accumulated;
                    toolAccumulator.Clear();
                }

                if (node["usage"] is JsonObject usageObj && ParseUsage(usageObj) is { } usage)
                    yield return new AgentChunk.UsageReport(usage);
            }
        }

        foreach (var trailing in parser.Flush())
        {
            if (trailing == "[DONE]") continue;
            var node = JsonNode.Parse(trailing);
            var delta = node?["choices"]?[0]?["delta"];
            if (delta?["content"] is { } textNode && (string?)textNode is { Length: > 0 } text)
                yield return new AgentChunk.TextDelta(text);
        }

        yield return new AgentChunk.Done(finishReason);
    }

    private static IEnumerable<AgentChunk> EmitToolCalls(
        Dictionary<int, (string? Id, string Name, StringBuilder Args)> accumulator)
    {
        if (accumulator.Count == 0) yield break;
        var calls = accumulator.OrderBy(p => p.Key)
            .Where(p => p.Value.Id is not null && p.Value.Name.Length > 0)
            .Select(p => new AgentToolCall
            {
                Id = p.Value.Id!,
                Name = p.Value.Name,
                Arguments = p.Value.Args.ToString(),
            })
            .ToList();
        if (calls.Count > 0)
            yield return new AgentChunk.ToolCallsIssued(calls);
    }

    internal static Usage? ParseUsage(JsonObject obj)
    {
        var prompt = (int?)obj["prompt_tokens"];
        var completion = (int?)obj["completion_tokens"];
        var total = (int?)obj["total_tokens"];
        if (prompt is null && completion is null && total is null) return null;
        return new Usage
        {
            PromptTokens = prompt,
            CompletionTokens = completion,
            TotalTokens = total,
            CachedPromptTokens = (int?)obj["prompt_tokens_details"]?["cached_tokens"],
        };
    }

    /// <summary>Request body bytes are deterministic: sorted keys, images as content parts.</summary>
    internal static string BuildRequestBody(AgentRequest request)
    {
        var body = new JsonObject
        {
            ["model"] = request.Model,
            ["messages"] = new JsonArray(request.Messages.Select(m => (JsonNode?)MessageJson(m)).ToArray()),
            ["stream"] = true,
            ["parallel_tool_calls"] = request.ParallelToolCalls,
        };
        if (request.Temperature is { } temperature) body["temperature"] = temperature;
        if (request.ReasoningEffort is { } effort) body["reasoning_effort"] = effort.ToWire();
        if (request.IncludeUsage)
            body["stream_options"] = new JsonObject { ["include_usage"] = true };
        if (request.Tools is { Count: > 0 } tools)
            body["tools"] = new JsonArray(tools.Select(t => (JsonNode?)new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description,
                    ["parameters"] = t.Parameters.DeepClone(),
                },
            }).ToArray());

        return Jsonx.SerializeSorted(body);
    }

    private static JsonObject MessageJson(AgentMessage message)
    {
        if (message.Images.Count > 0 && message.Role == "user")
        {
            var parts = new JsonArray();
            if (!string.IsNullOrEmpty(message.Content))
                parts.Add(new JsonObject { ["type"] = "text", ["text"] = message.Content });
            foreach (var image in message.Images)
                parts.Add(new JsonObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JsonObject { ["url"] = image.Url },
                });
            return new JsonObject { ["role"] = message.Role, ["content"] = parts };
        }

        var obj = new JsonObject { ["role"] = message.Role };
        if (message.ToolCalls is { Count: > 0 })
        {
            obj["content"] = string.IsNullOrEmpty(message.Content) ? null : message.Content;
            obj["tool_calls"] = new JsonArray(message.ToolCalls.Select(c => (JsonNode?)new JsonObject
            {
                ["id"] = c.Id,
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = c.Name,
                    ["arguments"] = c.Arguments,
                },
            }).ToArray());
        }
        else
        {
            obj["content"] = message.Content ?? "";
        }
        if (message.Role == "tool" && message.ToolCallId is not null)
            obj["tool_call_id"] = message.ToolCallId;
        return obj;
    }

    private static AgentRequest ToWire(ChatRequest request) => new()
    {
        Model = request.Model,
        Messages = request.Messages
            .Select(m => new AgentMessage { Role = m.Role, Content = m.Content, Images = m.Images })
            .ToList(),
        Temperature = request.Temperature,
        ReasoningEffort = request.ReasoningEffort,
        IncludeUsage = request.IncludeUsage,
    };
}
