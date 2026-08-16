using System.Text.Json.Nodes;

namespace Intatis.Core.Tools;

public enum ToolCapability
{
    ReadWorkspace,
    WriteWorkspace,
    ApplyPatch,
    RunShell,
    GitControl,
}

public sealed record ToolDescriptor
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public SideEffect SideEffect { get; init; } = SideEffect.ReadOnly;
    public JsonObject Parameters { get; init; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
    };
}

public sealed record ToolObservation
{
    public string Text { get; init; } = "";
    public bool Truncated { get; init; }
    public string? Diff { get; init; }

    public static ToolObservation Succeeded(string text, bool truncated = false, string? diff = null)
        => new() { Text = text, Truncated = truncated, Diff = diff };

    public static ToolObservation Denied(string reason)
        => new() { Text = "DENIED: " + reason };

    public static ToolObservation Failed(string reason)
        => new() { Text = "ERROR: " + reason };
}

public sealed class ToolContext
{
    public required string WorkspaceRoot { get; init; }
}

public interface ITool
{
    ToolDescriptor Descriptor { get; }
    ToolCapability[] Capabilities { get; }
    List<string> TouchedPaths(JsonNode args);
    Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default);
}

/// <summary>
/// Immutable registry; duplicate names become unregisterable conflicts rather than
/// last-write-wins, mirroring the Apple ToolRegistry contract.
/// </summary>
public sealed class ToolRegistry
{
    public const string RegistryVersion = "intatis.standard.win.v1";

    private readonly Dictionary<string, ITool> _tools = new(StringComparer.Ordinal);
    private readonly HashSet<string> _conflictedNames = new(StringComparer.Ordinal);

    public ToolRegistry Add(ITool tool)
    {
        if (!_tools.TryAdd(tool.Descriptor.Name, tool))
            _conflictedNames.Add(tool.Descriptor.Name);
        return this;
    }

    public ITool? Tool(string name) => _tools.TryGetValue(name, out var tool) ? tool : null;

    public List<ToolSpec> Specs() => _tools.Values
        .Where(t => !_conflictedNames.Contains(t.Descriptor.Name))
        .Select(t => new ToolSpec
        {
            Name = t.Descriptor.Name,
            Description = t.Descriptor.Description,
            Parameters = t.Descriptor.Parameters.DeepClone().AsObject(),
        })
        .ToList();

    public List<string> Names() => _tools.Keys.Order().ToList();

    public static ToolRegistry Standard()
    {
        var registry = new ToolRegistry();
        registry.Add(new ReadFileTool());
        registry.Add(new ListFilesTool());
        registry.Add(new SearchTextTool());
        registry.Add(new WriteFileTool());
        registry.Add(new ApplyPatchTool());
        registry.Add(new GitStatusTool());
        registry.Add(new GitDiffTool());
        registry.Add(new GitRecentCommitsTool());
        registry.Add(new RunShellTool());
        return registry;
    }
}

/// <summary>Argument helpers shared by tool implementations.</summary>
internal static class ToolArgs
{
    public static string GetString(JsonNode args, string field, string fallback = "")
        => (string?)args[field] ?? fallback;

    public static int GetInt(JsonNode args, string field, int fallback)
        => (int?)args[field] ?? fallback;
}
