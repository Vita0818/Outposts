using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Intatis.Windows.Shared;

public sealed class ToolDescriptor
{
    public string Name { get; }
    public string Description { get; }
    public SideEffect SideEffect { get; }
    public Dictionary<string, object?> Parameters { get; }

    public ToolDescriptor(string name, string description, SideEffect sideEffect, Dictionary<string, object?> parameters)
    {
        Name = name;
        Description = description;
        SideEffect = sideEffect;
        Parameters = parameters;
    }

    public Dictionary<string, object?> ToOpenAiDefinition()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object?>
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = Parameters,
            },
        };
    }
}

public sealed class ToolObservation
{
    public string Text { get; }
    public bool Truncated { get; }
    public string? Diff { get; }
    public IReadOnlyList<string>? ChangedFiles { get; }

    public ToolObservation(string text, bool truncated = false, string? diff = null, IReadOnlyList<string>? changedFiles = null)
    {
        Text = text;
        Truncated = truncated;
        Diff = diff;
        ChangedFiles = changedFiles;
    }
}

public sealed record ToolArgs(string Raw)
{
    public JsonElement Root => JsonSerializer.Deserialize<JsonElement>(Raw);
}

public sealed class ToolContext
{
    public string WorkspaceRoot { get; }
    public string AgentName { get; }
    public IToolShellRunner Shell { get; }
    public IToolGitService Git { get; }
    public IToolAgentMessenger? Messenger { get; }

    public ToolContext(
        string workspaceRoot,
        string agentName,
        IToolShellRunner shell,
        IToolGitService git,
        IToolAgentMessenger? messenger = null)
    {
        WorkspaceRoot = workspaceRoot;
        AgentName = agentName;
        Shell = shell;
        Git = git;
        Messenger = messenger;
    }
}

public interface ITool
{
    ToolDescriptor Descriptor { get; }
    IReadOnlyList<string> TouchedPaths(ToolArgs args);
    bool RisksNetwork(ToolArgs args);
    Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default);
}

public interface IToolAgentMessenger
{
    Task<string> AskAsync(string from, string to, string question);
}

public sealed class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools.ToDictionary(t => t.Descriptor.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ITool? Tool(string name) => _tools.TryGetValue(name, out var tool) ? tool : null;
    public IReadOnlyList<ToolDescriptor> Descriptors => _tools.Values.Select(t => t.Descriptor).ToList();

    public ToolRegistry Add(IEnumerable<ITool> tools)
    {
        var merged = new List<ITool>(_tools.Values);
        merged.AddRange(tools);
        return new ToolRegistry(merged);
    }

    public static ToolRegistry Standard()
    {
        return new ToolRegistry(new ITool[]
        {
            new ReadFileTool(),
            new ListFilesTool(),
            new SearchTextTool(),
            new WriteFileTool(),
            new ApplyPatchTool(),
            new RunShellTool(),
            new GitStatusTool(),
            new GitDiffTool(),
        });
    }
}

public interface IToolShellRunner
{
    Task<ShellResult> RunAsync(string command, string workingDirectory, CancellationToken cancellationToken = default);
}

public readonly struct ShellResult
{
    public string StdOut { get; }
    public string StdErr { get; }
    public int ExitCode { get; }

    public ShellResult(string stdout, string stderr, int exitCode)
    {
        StdOut = stdout;
        StdErr = stderr;
        ExitCode = exitCode;
    }
}

public sealed class ProcessShellRunner : IToolShellRunner
{
    public async Task<ShellResult> RunAsync(string command, string workingDirectory, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        var isWindows = OperatingSystem.IsWindows();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/sh",
            Arguments = isWindows ? $"/c \"{command}\"" : $"-c \"{command}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new ShellResult(stdout, stderr, process.ExitCode);
    }
}

public interface IToolGitService
{
    Task<string> StatusAsync(string workspaceRoot, CancellationToken cancellationToken = default);
    Task<string> DiffAsync(string workspaceRoot, CancellationToken cancellationToken = default);
}

public sealed class ProcessGitService : IToolGitService
{
    private readonly IToolShellRunner _shell;

    public ProcessGitService(IToolShellRunner? shell = null)
    {
        _shell = shell ?? new ProcessShellRunner();
    }

    public Task<string> StatusAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        return _shell.RunAsync("git status --porcelain=v1", workspaceRoot, cancellationToken)
            .ContinueWith(t => t.Result.StdOut, cancellationToken);
    }

    public Task<string> DiffAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        return _shell.RunAsync("git diff", workspaceRoot, cancellationToken)
            .ContinueWith(t => t.Result.StdOut, cancellationToken);
    }
}

public sealed class ReadFileTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "read_file",
        "Read a UTF-8 text file within the workspace.",
        SideEffect.ReadOnly,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["path"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["maxBytes"] = new Dictionary<string, object?> { ["type"] = "integer" },
            },
            ["required"] = new[] { "path" },
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args) => [GetRequiredString(args.Root, "path", "")];
    public bool RisksNetwork(ToolArgs args) => false;

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        var path = GetRequiredString(args.Root, "path", null)
                   ?? throw new InvalidOperationException("tool args missing required 'path'");
        var maxBytes = GetInt(args.Root, "maxBytes");
        var resolved = WorkspaceSecurity.ResolveInWorkspace(context.WorkspaceRoot, path);
        var bytes = await File.ReadAllBytesAsync(resolved, cancellationToken);
        var limit = maxBytes > 0 ? maxBytes : 100_000;
        var truncated = bytes.Length > limit;
        var slice = truncated ? bytes.AsSpan(0, limit).ToArray() : bytes;
        var text = Encoding.UTF8.GetString(slice);
        return new ToolObservation(text, truncated);
    }

    private static string? GetRequiredString(JsonElement root, string key, string? fallback)
    {
        if (!root.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String)
            return fallback;
        return value.GetString();
    }

    private static int GetInt(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var value))
            return -1;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n) ? n : -1;
    }
}

public sealed class ListFilesTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "list_files",
        "List entries of a directory within the workspace.",
        SideEffect.ReadOnly,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["path"] = new Dictionary<string, object?> { ["type"] = "string" },
            },
            ["required"] = Array.Empty<string>(),
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args)
    {
        var path = GetString(args.Root, "path", ".");
        return new[] { path };
    }

    public bool RisksNetwork(ToolArgs args) => false;

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        var path = GetString(args.Root, "path", ".");
        var resolved = WorkspaceSecurity.ResolveInWorkspace(context.WorkspaceRoot, path);
        var entries = Directory.EnumerateFileSystemEntries(resolved)
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(name =>
            {
                try
                {
                    if (Directory.Exists(Path.Combine(resolved, name)))
                        return $"{name}/";
                }
                catch
                {
                    // ignore lookup failures for unstable trees.
                }
                return name!;
            })
            .ToList();
        await Task.CompletedTask;
        return new ToolObservation(string.Join("\n", entries));
    }

    private static string GetString(JsonElement root, string key, string fallback)
    {
        return root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? (value.GetString() ?? fallback)
            : fallback;
    }
}

public sealed class SearchTextTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "search_text",
        "Search for a literal substring in text files under a workspace path.",
        SideEffect.ReadOnly,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["query"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["path"] = new Dictionary<string, object?> { ["type"] = "string" },
            },
            ["required"] = new[] { "query" },
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args) => new[] { GetString(args.Root, "path", ".") };
    public bool RisksNetwork(ToolArgs args) => false;

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        var query = GetRequiredString(args.Root, "query")
                    ?? throw new InvalidOperationException("tool args missing required 'query'");
        var path = GetString(args.Root, "path", ".");
        var baseDir = WorkspaceSecurity.ResolveInWorkspace(context.WorkspaceRoot, path);
        var matches = new List<string>();
        const int max = 200;

        foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            if (matches.Count >= max)
                break;

            string text;
            try
            {
                text = await File.ReadAllTextAsync(file, cancellationToken);
            }
            catch
            {
                continue;
            }

            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (matches.Count >= max)
                    break;

                if (!lines[i].Contains(query, StringComparison.Ordinal))
                    continue;

                var rel = Path.GetRelativePath(context.WorkspaceRoot, file);
                matches.Add($"{rel}:{i + 1}:{lines[i]}");
            }
        }

        var truncated = matches.Count >= max;
        if (matches.Count == 0)
            return new ToolObservation("(no matches)", truncated);
        return new ToolObservation(string.Join("\n", matches), truncated);
    }

    private static string? GetRequiredString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();
        return null;
    }

    private static string GetString(JsonElement root, string key, string fallback)
    {
        return root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? (value.GetString() ?? fallback)
            : fallback;
    }
}

public sealed class WriteFileTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "write_file",
        "Write (create or overwrite) a UTF-8 text file within the workspace.",
        SideEffect.Write,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["path"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["content"] = new Dictionary<string, object?> { ["type"] = "string" },
            },
            ["required"] = new[] { "path", "content" },
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args) => new[] { GetRequiredString(args.Root, "path") };
    public bool RisksNetwork(ToolArgs args) => false;

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        var path = GetRequiredString(args.Root, "path");
        var content = GetRequiredString(args.Root, "content");
        var resolved = WorkspaceSecurity.ResolveInWorkspace(context.WorkspaceRoot, path);
        var dir = Path.GetDirectoryName(resolved);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(resolved, content, cancellationToken);
        return new ToolObservation($"wrote {path} ({Encoding.UTF8.GetByteCount(content)} bytes)");
    }

    private static string GetRequiredString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;
        throw new InvalidOperationException($"tool args missing required '{key}'");
    }
}

public sealed class ApplyPatchTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "apply_patch",
        "Apply a unified diff to files within the workspace.",
        SideEffect.Write,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["diff"] = new Dictionary<string, object?> { ["type"] = "string" },
            },
            ["required"] = new[] { "diff" },
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args)
    {
        var diff = GetRequiredString(args.Root, "diff");
        return ParsePatch(diff).Select(p => p.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public bool RisksNetwork(ToolArgs args) => false;

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        var diff = GetRequiredString(args.Root, "diff");
        var patches = ParsePatch(diff);
        if (patches.Count == 0)
            throw new InvalidOperationException("no file sections found in diff");

        var changed = new List<string>();
        foreach (var patch in patches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = WorkspaceSecurity.ResolveInWorkspace(context.WorkspaceRoot, patch.Path);
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var original = File.Exists(file) ? await File.ReadAllTextAsync(file, cancellationToken) : string.Empty;
            var updated = ApplyPatch(original, patch.Hunks);
            await File.WriteAllTextAsync(file, updated, cancellationToken);
            changed.Add(patch.Path);
        }

        return new ToolObservation($"applied patch to {string.Join(\", \", changed)}", changedFiles: changed, diff: diff);
    }

    private static string GetRequiredString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;
        throw new InvalidOperationException($"tool args missing required '{key}'");
    }

    private static List<PatchFile> ParsePatch(string diff)
    {
        var files = new List<PatchFile>();
        PatchFile? current = null;
        var lines = diff.Split('\n');
        var oldLines = new List<string>();
        var newLines = new List<string>();
        var inHunk = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                if (current is not null)
                {
                    if (inHunk)
                    {
                        current.Hunks.Add(new PatchHunk(new List<string>(oldLines), new List<string>(newLines)));
                        oldLines.Clear();
                        newLines.Clear();
                        inHunk = false;
                    }
                    files.Add(current);
                }
                current = null;
            }
            else if (line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                var p = line[4..];
                if (p.StartsWith("b/"))
                    p = p[2..];
                current = new PatchFile { Path = p };
            }
            else if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                if (inHunk && current is not null)
                {
                    current.Hunks.Add(new PatchHunk(new List<string>(oldLines), new List<string>(newLines)));
                    oldLines.Clear();
                    newLines.Clear();
                }
                inHunk = true;
            }
            else if (inHunk && current is not null)
            {
                if (line.Length == 0)
                {
                    oldLines.Add(string.Empty);
                    newLines.Add(string.Empty);
                }
                else if (line[0] == '+')
                {
                    newLines.Add(line.Substring(1));
                }
                else if (line[0] == '-')
                {
                    oldLines.Add(line.Substring(1));
                }
                else if (line[0] == ' ')
                {
                    var same = line.Substring(1);
                    oldLines.Add(same);
                    newLines.Add(same);
                }
                else
                {
                    inHunk = false;
                }
            }
        }

        if (current is not null)
        {
            if (inHunk)
                current.Hunks.Add(new PatchHunk(new List<string>(oldLines), new List<string>(newLines)));
            files.Add(current);
        }

        return files;
    }

    private static string ApplyPatch(string original, List<PatchHunk> hunks)
    {
        var lines = string.IsNullOrEmpty(original) ? new List<string>() : original.Split('\n').ToList();
        foreach (var hunk in hunks)
        {
            if (hunk.OldLines.Count == 0)
            {
                lines.AddRange(hunk.NewLines);
                continue;
            }

            var range = FindRange(lines, hunk.OldLines);
            if (range is null)
                throw new InvalidOperationException("patch hunk did not match file content");

            lines.RemoveRange(range.Start, range.Length);
            lines.InsertRange(range.Start, hunk.NewLines);
        }

        return string.Join("\n", lines);
    }

    private static Range? FindRange(List<string> source, List<string> needle)
    {
        if (needle.Count == 0 || needle.Count > source.Count)
            return null;
        for (var start = 0; start <= source.Count - needle.Count; start++)
        {
            var match = true;
            for (var i = 0; i < needle.Count; i++)
            {
                if (source[start + i] != needle[i])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return new Range(start, start + needle.Count);
        }

        return null;
    }

    private sealed class PatchFile
    {
        public string Path { get; set; } = string.Empty;
        public List<PatchHunk> Hunks { get; } = [];
    }

    private sealed class PatchHunk
    {
        public List<string> OldLines { get; }
        public List<string> NewLines { get; }
        public PatchHunk(List<string> oldLines, List<string> newLines)
        {
            OldLines = oldLines;
            NewLines = newLines;
        }
    }
}

public sealed class RunShellTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "run_shell",
        "Run a shell command in the workspace directory.",
        SideEffect.Exec,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["command"] = new Dictionary<string, object?> { ["type"] = "string" },
            },
            ["required"] = new[] { "command" },
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args) => Array.Empty<string>();
    public bool RisksNetwork(ToolArgs args) => RisksNetwork(args.Root);

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        var command = GetRequiredString(args.Root, "command");
        var result = await context.Shell.RunAsync(command, context.WorkspaceRoot, cancellationToken);
        var output = result.StdOut;
        if (!string.IsNullOrWhiteSpace(result.StdErr))
            output += "\n[stderr]\n" + result.StdErr;
        output += $"\n[exit {result.ExitCode}]";
        return new ToolObservation(output);
    }

    public static bool RisksNetwork(string command)
    {
        return ShellInspector.RisksNetworkOrInstall(command);
    }

    private static string GetRequiredString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;
        throw new InvalidOperationException($"tool args missing required '{key}'");
    }
}

public sealed class GitStatusTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "git_status",
        "Show working-tree status (porcelain).",
        SideEffect.ReadOnly,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>(),
            ["required"] = Array.Empty<string>(),
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args) => Array.Empty<string>();
    public bool RisksNetwork(ToolArgs args) => false;

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        var text = await context.Git.StatusAsync(context.WorkspaceRoot, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
            return new ToolObservation("clean");
        return new ToolObservation(text.TrimEnd());
    }
}

public sealed class GitDiffTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "git_diff",
        "Show unstaged changes as a unified diff.",
        SideEffect.ReadOnly,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>(),
            ["required"] = Array.Empty<string>(),
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args) => Array.Empty<string>();
    public bool RisksNetwork(ToolArgs args) => false;

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        var diff = await context.Git.DiffAsync(context.WorkspaceRoot, cancellationToken);
        if (string.IsNullOrWhiteSpace(diff))
            return new ToolObservation("(no changes)");
        var truncated = diff.Length > 200_000;
        var text = truncated ? diff[..200_000] : diff;
        return new ToolObservation(text, truncated, diff: diff);
    }
}

public sealed class AskAgentTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        "ask_agent",
        "Ask another attached agent a question. Returns their answer.",
        SideEffect.ReadOnly,
        new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["to"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["description"] = "Target agent name",
                },
                ["question"] = new Dictionary<string, object?> { ["type"] = "string" },
            },
            ["required"] = new[] { "to", "question" },
        });

    public IReadOnlyList<string> TouchedPaths(ToolArgs args) => Array.Empty<string>();
    public bool RisksNetwork(ToolArgs args) => false;

    public async Task<ToolObservation> ExecuteAsync(ToolArgs args, ToolContext context, CancellationToken cancellationToken = default)
    {
        if (context.Messenger is null)
            return new ToolObservation("agent messaging is not available in this session");

        var to = GetRequiredString(args.Root, "to");
        var question = GetRequiredString(args.Root, "question");
        var answer = await context.Messenger.AskAsync(context.AgentName, to, question);
        return new ToolObservation(answer);
    }

    private static string GetRequiredString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;
        throw new InvalidOperationException($"tool args missing required '{key}'");
    }
}
