using System.Text;
using System.Text.Json.Nodes;
using Intatis.Core.Permission;

namespace Intatis.Core.Tools;

public sealed class ReadFileTool : ITool
{
    public const int MaximumBytes = 512 * 1024;

    public ToolDescriptor Descriptor { get; } = new()
    {
        Name = "read_file",
        Description = "Read a UTF-8 text file from the workspace. Returns the file content; binary or oversized files are truncated.",
        SideEffect = SideEffect.ReadOnly,
        Parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Workspace-relative or absolute path" },
                ["offset"] = new JsonObject { ["type"] = "integer", ["description"] = "0-based line offset (optional)" },
                ["limit"] = new JsonObject { ["type"] = "integer", ["description"] = "Max lines to return (optional)" },
            },
            ["required"] = new JsonArray("path"),
        },
    };

    public ToolCapability[] Capabilities { get; } = [ToolCapability.ReadWorkspace];

    public List<string> TouchedPaths(JsonNode args) => [ToolArgs.GetString(args, "path")];

    public Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var path = ToolArgs.GetString(args, "path");
        if (path.Length == 0) return Task.FromResult(ToolObservation.Failed("missing 'path'"));

        string resolved;
        try { resolved = PathConfinement.ResolveWithin(context.WorkspaceRoot, path); }
        catch (InvalidOperationException ex) { return Task.FromResult(ToolObservation.Denied(ex.Message)); }

        if (!File.Exists(resolved)) return Task.FromResult(ToolObservation.Failed($"file not found: {path}"));

        var info = new FileInfo(resolved);
        var truncated = false;
        string content;
        try { content = File.ReadAllText(resolved, Encoding.UTF8); }
        catch (Exception ex) { return Task.FromResult(ToolObservation.Failed($"unreadable: {ex.Message}")); }

        if (content.Length > MaximumBytes)
        {
            content = content[..MaximumBytes];
            truncated = true;
        }

        var offset = ToolArgs.GetInt(args, "offset", 0);
        var limit = ToolArgs.GetInt(args, "limit", 2000);
        var lines = content.Split('\n');
        var selected = lines.Skip(Math.Max(0, offset)).Take(Math.Max(1, limit)).ToArray();
        if (offset + selected.Length < lines.Length) truncated = true;

        var body = string.Join('\n', selected)
            .Replace("\r", "");
        return Task.FromResult(ToolObservation.Succeeded(body, truncated));
    }
}

public sealed class ListFilesTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new()
    {
        Name = "list_files",
        Description = "List files and directories under a workspace path. Skips common junk directories. Returns one relative path per line.",
        SideEffect = SideEffect.ReadOnly,
        Parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Directory to list (default: workspace root)" },
                ["max_entries"] = new JsonObject { ["type"] = "integer" },
            },
        },
    };

    public ToolCapability[] Capabilities { get; } = [ToolCapability.ReadWorkspace];

    private static readonly string[] IgnoredDirectories =
        ["node_modules", ".git", "bin", "obj", "build", "dist", ".venv", "__pycache__", ".gradle"];

    public List<string> TouchedPaths(JsonNode args) => [];

    public Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var relative = ToolArgs.GetString(args, "path");
        var maxEntries = ToolArgs.GetInt(args, "max_entries", 400);

        string directory;
        try { directory = relative.Length == 0 ? PathConfinement.Canonicalize(context.WorkspaceRoot) : PathConfinement.ResolveWithin(context.WorkspaceRoot, relative); }
        catch (InvalidOperationException ex) { return Task.FromResult(ToolObservation.Denied(ex.Message)); }

        if (!Directory.Exists(directory)) return Task.FromResult(ToolObservation.Failed($"directory not found: {relative}"));

        var root = PathConfinement.Canonicalize(context.WorkspaceRoot);
        var entries = new List<string>();
        var truncated = false;
        var queue = new Queue<string>();
        queue.Enqueue(directory);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            IEnumerable<string> children;
            try { children = Directory.EnumerateFileSystemEntries(current); }
            catch (Exception) { continue; }

            foreach (var child in children)
            {
                var name = Path.GetFileName(child);
                if (PathConfinement.IsSensitivePath(child)) continue;
                var isDir = Directory.Exists(child);
                if (isDir && IgnoredDirectories.Contains(name, StringComparer.Ordinal))
                {
                    entries.Add(Relative(root, child) + "/");
                    continue;
                }
                if (entries.Count >= maxEntries) { truncated = true; break; }
                entries.Add(Relative(root, child) + (isDir ? "/" : ""));
                if (isDir) queue.Enqueue(child);
            }
            if (truncated) break;
        }

        entries.Sort(StringComparer.Ordinal);
        return Task.FromResult(ToolObservation.Succeeded(string.Join('\n', entries), truncated));
    }

    private static string Relative(string root, string path)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = PathConfinement.Canonicalize(path);
        return full.StartsWith(normalizedRoot, StringComparison.Ordinal)
            ? full[normalizedRoot.Length..]
            : full;
    }
}

public sealed class SearchTextTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new()
    {
        Name = "search_text",
        Description = "Search for a literal string (or regex with use_regex) across workspace text files. Returns 'path:line:text' matches.",
        SideEffect = SideEffect.ReadOnly,
        Parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["query"] = new JsonObject { ["type"] = "string" },
                ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Restrict search to this directory (optional)" },
                ["use_regex"] = new JsonObject { ["type"] = "boolean" },
                ["max_matches"] = new JsonObject { ["type"] = "integer" },
            },
            ["required"] = new JsonArray("query"),
        },
    };

    public ToolCapability[] Capabilities { get; } = [ToolCapability.ReadWorkspace];

    public List<string> TouchedPaths(JsonNode args) => [];

    public Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var query = ToolArgs.GetString(args, "query");
        if (query.Length == 0) return Task.FromResult(ToolObservation.Failed("missing 'query'"));
        var useRegex = (bool?)args["use_regex"] ?? false;
        var maxMatches = ToolArgs.GetInt(args, "max_matches", 80);
        var scope = ToolArgs.GetString(args, "path");

        string root;
        try
        {
            root = scope.Length == 0
                ? PathConfinement.Canonicalize(context.WorkspaceRoot)
                : PathConfinement.ResolveWithin(context.WorkspaceRoot, scope);
        }
        catch (InvalidOperationException ex) { return Task.FromResult(ToolObservation.Denied(ex.Message)); }

        System.Text.RegularExpressions.Regex? regex = null;
        if (useRegex)
        {
            try { regex = new System.Text.RegularExpressions.Regex(query, System.Text.RegularExpressions.RegexOptions.Compiled); }
            catch (System.Text.RegularExpressions.ArgumentException ex) { return Task.FromResult(ToolObservation.Failed($"invalid regex: {ex.Message}")); }
        }

        var workspace = PathConfinement.Canonicalize(context.WorkspaceRoot);
        var matches = new List<string>();
        var truncated = false;
        var files = SafeEnumerateFiles(root);
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            if (PathConfinement.IsSensitivePath(file)) continue;
            var info = new FileInfo(file);
            if (info.Length > 1024 * 1024) continue;
            if (LooksBinary(file)) continue;

            string[] lines;
            try { lines = File.ReadAllText(file, Encoding.UTF8).Replace("\r", "").Split('\n'); }
            catch (Exception) { continue; }

            for (var i = 0; i < lines.Length; i++)
            {
                var hit = regex is not null
                    ? regex.IsMatch(lines[i])
                    : lines[i].Contains(query, StringComparison.Ordinal);
                if (!hit) continue;
                if (matches.Count >= maxMatches) { truncated = true; break; }
                var relative = PathConfinement.Canonicalize(file).StartsWith(workspace, StringComparison.Ordinal)
                    ? PathConfinement.Canonicalize(file)[(workspace.TrimEnd(Path.DirectorySeparatorChar).Length + 1)..]
                    : file;
                matches.Add($"{relative.Replace('\\', '/')}:{i + 1}:{TruncateLine(lines[i])}");
            }
            if (truncated) break;
        }

        return Task.FromResult(ToolObservation.Succeeded(
            matches.Count == 0 ? "no matches" : string.Join('\n', matches), truncated));
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root)
    {
        var queue = new Queue<string>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            string[] directories;
            try { directories = Directory.GetDirectories(current); }
            catch (Exception) { continue; }
            foreach (var directory in directories)
            {
                var name = Path.GetFileName(directory);
                if (name is "node_modules" or ".git" or "bin" or "obj" or "build" or "dist" or ".venv" or "__pycache__" or ".gradle")
                    continue;
                queue.Enqueue(directory);
            }
            string[] files;
            try { files = Directory.GetFiles(current); }
            catch (Exception) { continue; }
            foreach (var file in files) yield return file;
        }
    }

    private static bool LooksBinary(string file)
    {
        var extension = Path.GetExtension(file).ToLowerInvariant();
        return extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".ico" or ".pdf"
            or ".zip" or ".gz" or ".tar" or ".exe" or ".dll" or ".so" or ".dylib" or ".bin" or ".class";
    }

    private static string TruncateLine(string line)
        => line.Length <= 300 ? line.TrimEnd() : line[..300].TrimEnd() + "…";
}

public sealed class WriteFileTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new()
    {
        Name = "write_file",
        Description = "Create or overwrite a UTF-8 text file in the workspace. Parent directories are created.",
        SideEffect = SideEffect.Write,
        Parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["path"] = new JsonObject { ["type"] = "string" },
                ["content"] = new JsonObject { ["type"] = "string" },
            },
            ["required"] = new JsonArray("path", "content"),
        },
    };

    public ToolCapability[] Capabilities { get; } = [ToolCapability.WriteWorkspace];

    public List<string> TouchedPaths(JsonNode args) => [ToolArgs.GetString(args, "path")];

    public Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var path = ToolArgs.GetString(args, "path");
        var content = ToolArgs.GetString(args, "content");
        if (path.Length == 0) return Task.FromResult(ToolObservation.Failed("missing 'path'"));

        string resolved;
        try { resolved = PathConfinement.ResolveWithin(context.WorkspaceRoot, path); }
        catch (InvalidOperationException ex) { return Task.FromResult(ToolObservation.Denied(ex.Message)); }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resolved)!);
            var tmp = resolved + ".intatis-tmp";
            File.WriteAllText(tmp, content, new UTF8Encoding(false));
            File.Move(tmp, resolved, overwrite: true);
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolObservation.Failed($"write failed: {ex.Message}"));
        }
        return Task.FromResult(ToolObservation.Succeeded($"wrote {content.Length} chars to {path.Replace('\\', '/')}"));
    }
}
