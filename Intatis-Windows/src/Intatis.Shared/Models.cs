using System.Text.Json.Serialization;

namespace Intatis.Windows.Shared;

public enum IntatisMode
{
    Chat,
    Code,
    Cowork
}

public enum MessageRole
{
    User,
    Assistant,
    System,
    Agent
}

public sealed class IntatisMessage
{
    public string Id { get; }
    public MessageRole Role { get; }
    public string Content { get; set; }
    public DateTime AtUtc { get; }

    public IntatisMessage(MessageRole role, string content)
    {
        Id = Guid.NewGuid().ToString("N");
        Role = role;
        Content = content;
        AtUtc = DateTime.UtcNow;
    }
}

public sealed class IntatisConfig
{
    public string BaseUrl { get; }
    public string ApiKey { get; }
    public string Model { get; }
    public string? Reasoning { get; }
    public IntatisMode DefaultMode { get; }
    public string? Workspace { get; }
    public bool IncludeUsage { get; }

    [JsonConstructor]
    public IntatisConfig(
        string baseUrl,
        string apiKey,
        string model,
        string? reasoning,
        IntatisMode defaultMode,
        string? workspace,
        bool includeUsage)
    {
        BaseUrl = baseUrl;
        ApiKey = apiKey;
        Model = model;
        Reasoning = reasoning;
        DefaultMode = defaultMode;
        Workspace = workspace;
        IncludeUsage = includeUsage;
    }

    public IntatisConfig CloneWith(
        string? baseUrl = null,
        string? apiKey = null,
        string? model = null,
        string? reasoning = null,
        IntatisMode? defaultMode = null,
        string? workspace = null,
        bool? includeUsage = null)
    {
        return new IntatisConfig(
            baseUrl ?? BaseUrl,
            apiKey ?? ApiKey,
            model ?? Model,
            reasoning ?? Reasoning,
            defaultMode ?? DefaultMode,
            workspace ?? Workspace,
            includeUsage ?? IncludeUsage);
    }

    public override string ToString()
    {
        return $"BaseUrl={BaseUrl}, Model={Model}, Mode={DefaultMode}, Workspace={Workspace}";
    }
}

public sealed class SearchHit
{
    public string File { get; }
    public int Line { get; }
    public string Text { get; }

    public SearchHit(string file, int line, string text)
    {
        File = file;
        Line = line;
        Text = text;
    }
}
