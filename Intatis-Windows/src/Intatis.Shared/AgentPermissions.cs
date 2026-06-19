using System.Text.Json;

namespace Intatis.Windows.Shared;

public enum PermissionProfile
{
    Manual,
    Reviewed,
    Autopilot,
    ReadOnly,
    Locked,
}

public enum PermissionDecision
{
    Allow,
    Deny,
    AskUser,
}

public enum GateResult
{
    Deny,
    Ask,
    Allow,
    Pass,
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
}

public enum SideEffect
{
    ReadOnly,
    Write,
    Exec,
    Network,
    Destructive,
}

public sealed record ToolCallContext(
    string ToolName,
    SideEffect SideEffect,
    IReadOnlyList<string> TouchedPaths,
    bool RisksNetwork,
    string RawArgs);

public sealed record PermissionContext(
    string WorkspaceRoot,
    PermissionProfile Profile,
    bool AllowsShell,
    string? UserGoal = null,
    string? Agent = null);

public sealed record PermissionOutcome(
    PermissionDecision Decision,
    RiskLevel Risk,
    string Reason,
    bool ReviewedByModel = false);

public sealed record PermissionGateOutcome(GateResult Decision, RiskLevel Risk, string Reason);

public sealed record PermissionRequest(
    string RequestId,
    string Tool,
    string Args,
    RiskLevel Risk,
    string Reason,
    string? Agent = null);

public interface IPermissionResponder
{
    Task<PermissionDecision> RequestApprovalAsync(PermissionRequest request, CancellationToken cancellationToken = default);
}

public interface IPermissionReviewer
{
    Task<PermissionOutcome> ReviewAsync(
        ToolCallContext call,
        PermissionContext context,
        string gateReason,
        RiskLevel risk);
}

public sealed class AllowAllResponder : IPermissionResponder
{
    public Task<PermissionDecision> RequestApprovalAsync(PermissionRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(PermissionDecision.Allow);
}

public sealed class PermissionEngine
{
    private readonly DeterministicPolicyGate _gate = new();
    private readonly IPermissionReviewer? _reviewer;

    public PermissionEngine(IPermissionReviewer? reviewer = null)
    {
        _reviewer = reviewer;
    }

    public async Task<PermissionOutcome> DecideAsync(ToolCallContext call, PermissionContext context)
    {
        var result = _gate.Evaluate(call, context);
        return result.Decision switch
        {
            GateResult.Deny => new PermissionOutcome(PermissionDecision.Deny, result.Risk, result.Reason),
            GateResult.Ask => new PermissionOutcome(PermissionDecision.AskUser, result.Risk, result.Reason),
            GateResult.Allow => new PermissionOutcome(PermissionDecision.Allow, result.Risk, result.Reason),
            GateResult.Pass => await ReviewOrAskAsync(call, context, result),
            _ => new PermissionOutcome(PermissionDecision.AskUser, result.Risk, result.Reason),
        };
    }

    private Task<PermissionOutcome> ReviewOrAskAsync(ToolCallContext call, PermissionContext context, PermissionGateOutcome result)
    {
        if (_reviewer is null)
        {
            return Task.FromResult(new PermissionOutcome(
                PermissionDecision.AskUser,
                result.Risk,
                $"{result.Reason} (no reviewer configured → asking user)"));
        }

        return _reviewer.ReviewAsync(call, context, result.Reason, result.Risk);
    }
}

public sealed class DeterministicPolicyGate
{
    public PermissionGateOutcome Evaluate(ToolCallContext call, PermissionContext ctx)
    {
        if (ctx.Profile == PermissionProfile.Locked)
            return new PermissionGateOutcome(GateResult.Deny, RiskLevel.Low, "agent is locked");

        foreach (var path in call.TouchedPaths)
        {
            if (SecretScanner.IsSensitivePath(path))
                return new PermissionGateOutcome(GateResult.Deny, RiskLevel.High, $"touches sensitive file: {path}");

            if (!WorkspaceSecurity.IsWithinWorkspace(path, ctx.WorkspaceRoot))
                return new PermissionGateOutcome(GateResult.Deny, RiskLevel.High, $"path escapes workspace: {path}");
        }

        if (call.RisksNetwork)
            return ctx.Profile == PermissionProfile.ReadOnly
                ? new PermissionGateOutcome(GateResult.Deny, RiskLevel.Medium, "network not allowed in read_only")
                : new PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "network access requested");

        return call.SideEffect switch
        {
            SideEffect.ReadOnly => new PermissionGateOutcome(
                GateResult.Allow,
                RiskLevel.Low,
                "read-only operation within workspace"),
            SideEffect.Network => ctx.Profile == PermissionProfile.ReadOnly
                ? new PermissionGateOutcome(
                    GateResult.Deny,
                    RiskLevel.Medium,
                    "network not allowed in read_only")
                : new PermissionGateOutcome(
                    GateResult.Ask,
                    RiskLevel.Medium,
                    "network access requested"),
            SideEffect.Destructive => ctx.Profile == PermissionProfile.ReadOnly
                ? new PermissionGateOutcome(
                    GateResult.Deny,
                    RiskLevel.High,
                    "destructive operation not allowed in read_only")
                : new PermissionGateOutcome(
                    GateResult.Ask,
                    RiskLevel.High,
                    "destructive operation"),
            SideEffect.Exec => EvaluateExec(call, ctx),
            SideEffect.Write => EvaluateWrite(call, ctx),
            _ => new PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "unknown side effect"),
        };
    }

    private PermissionGateOutcome EvaluateExec(ToolCallContext call, PermissionContext ctx)
    {
        if (!ctx.AllowsShell)
            return new PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "shell is disabled in this profile");

        if (ctx.Profile == PermissionProfile.ReadOnly)
            return new PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "shell not allowed in read_only");

        var command = ExtractShellCommand(call.RawArgs);
        if (ShellInspector.IsDangerous(command))
            return new PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "dangerous shell command");

        if (ShellInspector.RisksNetworkOrInstall(command))
            return new PermissionGateOutcome(
                GateResult.Ask,
                RiskLevel.High,
                "shell command may access network or install packages");

        if (ShellInspector.IsReadOnlyCommand(command))
            return new PermissionGateOutcome(GateResult.Allow, RiskLevel.Low, "read-only shell command");

        return ctx.Profile switch
        {
            PermissionProfile.Manual => new PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "run shell command"),
            PermissionProfile.Reviewed => new PermissionGateOutcome(GateResult.Pass, RiskLevel.Medium, "shell command"),
            PermissionProfile.Autopilot => new PermissionGateOutcome(GateResult.Pass, RiskLevel.Medium, "shell command"),
            _ => new PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "run shell command"),
        };
    }

    private PermissionGateOutcome EvaluateWrite(ToolCallContext call, PermissionContext ctx)
    {
        if (ctx.Profile == PermissionProfile.ReadOnly)
            return new PermissionGateOutcome(GateResult.Deny, RiskLevel.Medium, "writes not allowed in read_only");

        if (call.TouchedPaths.Any(SecretScanner.IsProtectedConfigPath))
            return new PermissionGateOutcome(
                GateResult.Ask,
                RiskLevel.High,
                "modifies lockfile / CI / build config");

        return ctx.Profile switch
        {
            PermissionProfile.Manual => new PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "write to workspace"),
            PermissionProfile.Reviewed => new PermissionGateOutcome(GateResult.Pass, RiskLevel.Low, "write within workspace"),
            PermissionProfile.Autopilot => new PermissionGateOutcome(GateResult.Pass, RiskLevel.Low, "write within workspace"),
            _ => new PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "write to workspace"),
        };
    }

    private static string ExtractShellCommand(string rawArgs)
    {
        using var doc = JsonDocument.Parse(rawArgs);
        if (doc.RootElement.TryGetProperty("command", out var c) && c.ValueKind == JsonValueKind.String)
            return c.GetString() ?? string.Empty;
        return string.Empty;
    }
}

public static class WorkspaceSecurity
{
    public static bool IsWithinWorkspace(string candidatePath, string workspaceRoot)
    {
        try
        {
            var root = Path.GetFullPath(workspaceRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(candidatePath);
            var withSlash = root + Path.DirectorySeparatorChar;
            return full == root || full.StartsWith(withSlash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static string ResolveInWorkspace(string workspaceRoot, string path)
    {
        var root = Path.GetFullPath(workspaceRoot);
        var full = Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(root, path));
        if (!IsWithinWorkspace(full, root))
            throw new InvalidOperationException($"path escapes workspace: {path}");
        return full;
    }
}

public static class SecretScanner
{
    private static readonly HashSet<string> SensitiveBasenames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env", ".netrc", ".pgpass", "id_rsa", "id_dsa", "id_ecdsa", "id_ed25519",
        "credentials", ".npmrc", ".pypirc",
    };

    private static readonly HashSet<string> SensitiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "pem", "key", "p12", "pfx", "keystore", "jks", "asc",
    };

    private static readonly string[] SensitiveDirHints = new[]
    {
        "/.ssh/", "/.aws/", "/.gnupg/", "/.gpg/", "secrets/", "/.config/gh/",
    };

    private static readonly HashSet<string> ProtectedBasenames = new(StringComparer.OrdinalIgnoreCase)
    {
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "cargo.lock",
        "podfile.lock", "gemfile.lock", "package.resolved", "poetry.lock",
    };

    private static readonly string[] ProtectedHints = new[]
    {
        ".github/workflows/", ".gitlab-ci", "/dockerfile", "/makefile",
        "fastlane/", "/ci/",
    };

    public static bool IsSensitivePath(string path)
    {
        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        var fileName = Path.GetFileName(normalized);
        if (SensitiveBasenames.Contains(fileName))
            return true;
        if (fileName.StartsWith(".env", StringComparison.OrdinalIgnoreCase))
            return true;
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (!string.IsNullOrEmpty(ext) && SensitiveExtensions.Contains(ext))
            return true;
        var padded = "/" + normalized;
        return SensitiveDirHints.Any(h => padded.Contains(h, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsProtectedConfigPath(string path)
    {
        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        var fileName = Path.GetFileName(normalized);
        if (ProtectedBasenames.Contains(fileName))
            return true;
        var padded = "/" + normalized;
        return ProtectedHints.Any(h => padded.Contains(h, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsSecret(string text)
    {
        var markers = new[]
        {
            "-----BEGIN",
            "PRIVATE KEY",
            "AKIA",
            "ASIA",
            "sk-",
            "ssh-rsa ",
            "xoxb-",
            "xoxp-",
            "ghp_",
            "github_pat_",
            "AIza",
        };

        return markers.Any(marker => text.Contains(marker, StringComparison.Ordinal));
    }
}

public static class ShellInspector
{
    private static readonly string[] Dangerous = new[]
    {
        "sudo", "rm -rf", "rm -fr", "rm -r ", ":(){", "mkfs", "dd if=", "> /dev/sd",
        "chmod -r 777", "chown -r", "/etc/", "~/.ssh", "shutdown", "reboot", "killall",
    };

    private static readonly string[] NetworkOrInstall = new[]
    {
        "curl ", "wget ", "npm install", "npm i ", "yarn add", "pnpm add", "pip install",
        "pip3 install", "apt ", "apt-get", "brew install", "gem install", "git clone",
        "git push", "git pull", "git fetch", "nc ", "ssh ", "scp ",
    };

    private static readonly string[] ReadOnlyAllowlist = new[]
    {
        "ls", "pwd", "cat", "grep", "rg", "echo", "head", "tail", "wc", "find", "true",
    };

    public static bool IsDangerous(string command)
    {
        var lower = command.ToLowerInvariant();
        return Dangerous.Any(x => lower.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    public static bool RisksNetworkOrInstall(string command)
    {
        var lower = command.ToLowerInvariant();
        return NetworkOrInstall.Any(x => lower.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsReadOnlyCommand(string command)
    {
        var trimmed = command.Trim();
        if (trimmed.Length == 0)
            return false;
        var first = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return ReadOnlyAllowlist.Any(x => string.Equals(first, x, StringComparison.Ordinal))
               && !IsDangerous(command);
    }
}
