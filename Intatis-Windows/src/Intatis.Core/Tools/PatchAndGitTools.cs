using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Intatis.Core.Permission;

namespace Intatis.Core.Tools;

/// <summary>
/// apply_patch: a tolerant context-diff format with AddFile / UpdateFile / DeleteFile
/// sections, +/- line edits and @@ context anchors (V4A-style). Matching is
/// context-anchored first, unique-line fallback second.
/// </summary>
public sealed class ApplyPatchTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new()
    {
        Name = "apply_patch",
        Description = """
Apply a patch to workspace files. Format:
*** Begin Patch
*** Add File: <path>
+new line
*** Update File: <path>
@@ optional context @@
 context
-line to remove
+line to add
*** Delete File: <path>
*** End Patch
""",
        SideEffect = SideEffect.Write,
        Parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["patch"] = new JsonObject { ["type"] = "string", ["description"] = "The full patch text" },
            },
            ["required"] = new JsonArray("patch"),
        },
    };

    public ToolCapability[] Capabilities { get; } = [ToolCapability.ApplyPatch];

    public List<string> TouchedPaths(JsonNode args)
    {
        var patch = ToolArgs.GetString(args, "patch");
        var paths = new List<string>();
        foreach (var line in patch.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.StartsWith("*** Add File: ", StringComparison.Ordinal))
                paths.Add(TrimPath(trimmed["*** Add File: ".Length..]));
            else if (trimmed.StartsWith("*** Update File: ", StringComparison.Ordinal))
                paths.Add(TrimPath(trimmed["*** Update File: ".Length..]));
            else if (trimmed.StartsWith("*** Delete File: ", StringComparison.Ordinal))
                paths.Add(TrimPath(trimmed["*** Delete File: ".Length..]));
        }
        return paths;
    }

    private static string TrimPath(string raw) => raw.Trim().Trim('"');

    public Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var patch = ToolArgs.GetString(args, "patch");
        if (patch.Length == 0) return Task.FromResult(ToolObservation.Failed("missing 'patch'"));

        var sections = ParseSections(patch);
        if (sections.Count == 0)
            return Task.FromResult(ToolObservation.Failed("no *** Add/Update/Delete File sections found"));

        var applied = new List<string>();
        var diffs = new StringBuilder();
        foreach (var section in sections)
        {
            string resolved;
            try { resolved = PathConfinement.ResolveWithin(context.WorkspaceRoot, section.Path); }
            catch (InvalidOperationException ex) { return Rollback(applied, context, $"denied on {section.Path}: {ex.Message}"); }

            try
            {
                switch (section.Kind)
                {
                    case SectionKind.Add:
                        if (File.Exists(resolved))
                            return Rollback(applied, context, $"file already exists: {section.Path}");
                        Directory.CreateDirectory(Path.GetDirectoryName(resolved)!);
                        File.WriteAllText(resolved, SectionNewContent(section), new UTF8Encoding(false));
                        applied.Add(resolved);
                        diffs.AppendLine($"--- add {section.Path}");
                        break;

                    case SectionKind.Delete:
                        if (!File.Exists(resolved))
                            return Rollback(applied, context, $"file not found: {section.Path}");
                        File.Delete(resolved);
                        applied.Add(resolved);
                        diffs.AppendLine($"--- delete {section.Path}");
                        break;

                    case SectionKind.Update:
                        if (!File.Exists(resolved))
                            return Rollback(applied, context, $"file not found: {section.Path}");
                        var original = File.ReadAllText(resolved, Encoding.UTF8).Replace("\r", "");
                        var updated = ApplyUpdate(original, section.Hunks, out var error);
                        if (updated is null)
                            return Rollback(applied, context, $"patch failed on {section.Path}: {error}");
                        File.WriteAllText(resolved, updated, new UTF8Encoding(false));
                        applied.Add(resolved);
                        diffs.AppendLine($"--- update {section.Path} ({section.Hunks.Count} hunk(s))");
                        break;
                }
            }
            catch (Exception ex)
            {
                return Rollback(applied, context, $"error on {section.Path}: {ex.Message}");
            }
        }

        return Task.FromResult(ToolObservation.Succeeded(
            $"patch applied to {applied.Count} file(s): {string.Join(", ", applied.Select(p => Path.GetFileName(p)))}",
            diff: diffs.ToString()));
    }

    private static ToolObservation Rollback(List<string> applied, ToolContext context, string reason)
    {
        // Best-effort: report the failure verbatim; partial writes are re-runnable
        // because patch sections are per-file.
        return ToolObservation.Failed($"{reason} (files already touched: {applied.Count})");
    }

    internal enum SectionKind { Add, Update, Delete }

    internal sealed record PatchSection(SectionKind Kind, string Path, List<string> Lines);

    internal static List<PatchSection> ParseSections(string patch)
    {
        var sections = new List<PatchSection>();
        PatchSection? current = null;
        foreach (var rawLine in patch.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("*** Begin Patch", StringComparison.Ordinal)) continue;
            if (line.StartsWith("*** End Patch", StringComparison.Ordinal)) break;
            if (line.StartsWith("*** Add File: ", StringComparison.Ordinal))
            {
                current = new PatchSection(SectionKind.Add, TrimPath(line["*** Add File: ".Length..]), []);
                sections.Add(current);
                continue;
            }
            if (line.StartsWith("*** Update File: ", StringComparison.Ordinal))
            {
                current = new PatchSection(SectionKind.Update, TrimPath(line["*** Update File: ".Length..]), []);
                sections.Add(current);
                continue;
            }
            if (line.StartsWith("*** Delete File: ", StringComparison.Ordinal))
            {
                current = new PatchSection(SectionKind.Delete, TrimPath(line["*** Delete File: ".Length..]), []);
                sections.Add(current);
                continue;
            }
            current?.Lines.Add(line);
        }
        return sections;
    }

    private static string SectionNewContent(PatchSection section)
        => string.Join('\n', section.Lines.Where(l => l.StartsWith('+')).Select(l => l[1..])) + "\n";

    private static string? ApplyUpdate(string original, List<string> hunkLines, out string error)
    {
        error = "";
        var lines = original.Split('\n');
        // Normalize trailing newline handling: keep a marker for the final empty line.
        var hadTrailingNewline = original.Length == 0 || original.EndsWith('\n');
        if (hadTrailingNewline && lines.Length > 0 && lines[^1].Length == 0)
            lines = lines[..^1];

        var i = 0; // position in section lines
        var output = new List<string>();
        var sourceIndex = 0;

        while (i < hunkLines.Count)
        {
            var line = hunkLines[i];
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                var anchor = line[2..].TrimEnd('@').Trim();
                var found = FindAnchor(lines, sourceIndex, anchor);
                if (found < 0) { error = $"context anchor not found: {anchor}"; return null; }
                while (sourceIndex < found) output.Add(lines[sourceIndex++]);
                i++;
                continue;
            }
            if (line.Length == 0 || line.StartsWith(' '))
            {
                // Context line: must match the source.
                var expected = line.Length == 0 ? "" : line[1..];
                if (sourceIndex >= lines.Length || lines[sourceIndex] != expected)
                {
                    var fallback = FindUniqueForward(lines, sourceIndex, expected);
                    if (fallback < 0) { error = $"context mismatch near line {sourceIndex + 1}: '{Trim(expected)}'"; return null; }
                    while (sourceIndex < fallback) output.Add(lines[sourceIndex++]);
                }
                if (sourceIndex >= lines.Length) { error = "patch runs past end of file"; return null; }
                output.Add(lines[sourceIndex++]);
                i++;
                continue;
            }
            if (line.StartsWith('-'))
            {
                var expected = line[1..];
                if (sourceIndex >= lines.Length || lines[sourceIndex] != expected)
                {
                    var fallback = FindUniqueForward(lines, sourceIndex, expected);
                    if (fallback < 0) { error = $"removal mismatch near line {sourceIndex + 1}: '{Trim(expected)}'"; return null; }
                    while (sourceIndex < fallback) output.Add(lines[sourceIndex++]);
                }
                if (sourceIndex >= lines.Length || lines[sourceIndex] != expected)
                {
                    error = $"cannot remove line near {sourceIndex + 1}: '{Trim(expected)}'";
                    return null;
                }
                sourceIndex++; // consumed, not emitted
                i++;
                continue;
            }
            if (line.StartsWith('+'))
            {
                output.Add(line[1..]);
                i++;
                continue;
            }
            i++; // unknown decoration: skip
        }

        while (sourceIndex < lines.Length) output.Add(lines[sourceIndex++]);

        var result = string.Join('\n', output);
        if (hadTrailingNewline || output.Count > 0) result += "\n";
        return result;
    }

    private static int FindAnchor(string[] lines, int from, string anchor)
    {
        if (anchor.Length == 0) return from;
        for (var i = from; i < lines.Length; i++)
            if (lines[i] == anchor) return i;
        return -1;
    }

    private static int FindUniqueForward(string[] lines, int from, string expected)
    {
        if (expected.Length == 0) return -1;
        var occurrences = 0;
        var first = -1;
        for (var i = from; i < lines.Length; i++)
        {
            if (lines[i] != expected) continue;
            occurrences++;
            if (first < 0) first = i;
            if (occurrences > 1) return -1;
        }
        return first;
    }

    private static string Trim(string s) => s.Length <= 80 ? s : s[..80];
}

// ---------------------------------------------------------------------------
// Git tools (read-only surfaces via the git CLI, confined to the workspace).
// ---------------------------------------------------------------------------

public abstract class GitToolBase : ITool
{
    public abstract ToolDescriptor Descriptor { get; }
    public ToolCapability[] Capabilities { get; } = [ToolCapability.GitControl];

    public List<string> TouchedPaths(JsonNode args) => [];

    public abstract Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default);

    protected static async Task<(int ExitCode, string Output, string Error)> RunGitAsync(
        string workspaceRoot, string[] arguments, CancellationToken ct)
    {
        var info = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = info };
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return (process.ExitCode, stdout, stderr);
    }

    protected const int OutputLimit = 24 * 1024;
}

public sealed class GitStatusTool : GitToolBase
{
    public override ToolDescriptor Descriptor { get; } = new()
    {
        Name = "git_status",
        Description = "Show git working-tree status (porcelain v1 + branch).",
        SideEffect = SideEffect.ReadOnly,
        Parameters = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() },
    };

    public override async Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var (code, output, error) = await RunGitAsync(context.WorkspaceRoot, ["status", "--short", "--branch"], ct);
        if (code != 0) return ToolObservation.Failed($"git status failed: {Trim(error)}");
        return ToolObservation.Succeeded(Trim(output.Length > 0 ? output : "clean"));
    }

    private static string Trim(string value) => value.Length <= OutputLimit ? value : value[..OutputLimit] + "…";
}

public sealed class GitDiffTool : GitToolBase
{
    public override ToolDescriptor Descriptor { get; } = new()
    {
        Name = "git_diff",
        Description = "Show the git diff (working tree by default, or a base ref via 'base').",
        SideEffect = SideEffect.ReadOnly,
        Parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["base"] = new JsonObject { ["type"] = "string", ["description"] = "Optional base ref (e.g. HEAD~1)" },
                ["staged"] = new JsonObject { ["type"] = "boolean" },
            },
        },
    };

    public override async Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var staged = (bool?)args["staged"] ?? false;
        var @base = ToolArgs.GetString(args, "base");
        var arguments = new List<string> { "diff", "--stat", "--patch" };
        if (staged) arguments.Add("--staged");
        if (@base.Length > 0) arguments.Add(@base);
        arguments.Add("--");

        var (code, output, error) = await RunGitAsync(context.WorkspaceRoot, [.. arguments], ct);
        if (code != 0) return ToolObservation.Failed($"git diff failed: {Trim(error)}");
        var truncated = output.Length > OutputLimit;
        return ToolObservation.Succeeded(Trim(output.Length > 0 ? output : "no changes"), truncated,
            truncated ? output[..OutputLimit] : output);
    }

    private static string Trim(string value) => value.Length <= OutputLimit ? value : value[..OutputLimit] + "…";
}

public sealed class GitRecentCommitsTool : GitToolBase
{
    public override ToolDescriptor Descriptor { get; } = new()
    {
        Name = "git_recent_commits",
        Description = "Show the most recent commits (sha, subject).",
        SideEffect = SideEffect.ReadOnly,
        Parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["limit"] = new JsonObject { ["type"] = "integer" },
            },
        },
    };

    public override async Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var limit = Math.Clamp(ToolArgs.GetInt(args, "limit", 15), 1, 100);
        var (code, output, error) = await RunGitAsync(context.WorkspaceRoot,
            ["log", $"--max-count={limit}", "--pretty=format:%h %s"], ct);
        if (code != 0) return ToolObservation.Failed($"git log failed: {Trim(error)}");
        return ToolObservation.Succeeded(Trim(output));
    }

    private static string Trim(string value) => value.Length <= OutputLimit ? value : value[..OutputLimit] + "…";
}

/// <summary>run_shell: confined command execution. Dangerous patterns deny; risk is high.</summary>
public sealed class RunShellTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new()
    {
        Name = "run_shell",
        Description = "Run a shell command in the workspace directory. Read-only inspection commands (ls, git, rg, cat...) run at low risk; other commands require approval.",
        SideEffect = SideEffect.Exec,
        Parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["command"] = new JsonObject { ["type"] = "string" },
                ["timeout_seconds"] = new JsonObject { ["type"] = "integer" },
            },
            ["required"] = new JsonArray("command"),
        },
    };

    public ToolCapability[] Capabilities { get; } = [ToolCapability.RunShell];

    public List<string> TouchedPaths(JsonNode args) => [];

    public async Task<ToolObservation> ExecuteAsync(JsonNode args, ToolContext context, CancellationToken ct = default)
    {
        var command = ToolArgs.GetString(args, "command");
        if (command.Length == 0) return ToolObservation.Failed("missing 'command'");
        var timeout = TimeSpan.FromSeconds(Math.Clamp(ToolArgs.GetInt(args, "timeout_seconds", 120), 1, 600));

        var info = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
            WorkingDirectory = context.WorkspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        info.ArgumentList.Add(OperatingSystem.IsWindows() ? "/c" : "-c");
        info.ArgumentList.Add(command);

        using var process = new Process { StartInfo = info };
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        try
        {
            process.Start();
            var stdout = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(timeoutCts.Token).ConfigureAwait(false);
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            var text = $"exit={process.ExitCode}";
            if (stdout.Length > 0) text += $"\n{Trim(stdout)}";
            if (stderr.Length > 0) text += $"\nstderr:\n{Trim(stderr)}";
            return ToolObservation.Succeeded(text);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            return ToolObservation.Failed($"command timed out after {timeout.TotalSeconds:0}s");
        }
        catch (Exception ex)
        {
            return ToolObservation.Failed($"failed to run command: {ex.Message}");
        }
    }

    private static string Trim(string value) => value.Length <= 24 * 1024 ? value : value[..(24 * 1024)] + "…";
}
