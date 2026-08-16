using Intatis.Core.Providers;

namespace Intatis.Core.Permission;

/// <summary>
/// Layer A — the deterministic, model-free gate. Evaluation order matters: a locked
/// profile denies all mutations; sensitive paths and workspace escapes deny; read-only
/// effects pass at low risk; writes to protected config ask at high risk; ordinary
/// writes pass at medium risk for the next layer.
/// </summary>
public static class DeterministicPolicyGate
{
    private static readonly string[] ReadOnlyShellAllowlist =
        ["pwd", "ls", "dir", "cat", "type", "find", "grep", "rg", "git", "node", "python"];

    private static readonly string[] DangerousShellFragments =
        ["rm -rf /", "rm -rf ~", "format ", "shutdown", "reboot", "del /f /s /q c:", "rd /s /q c:",
         "mkfs", "dd if=", ":(){ :|:& };:", "attrib -r -a -s -h"];

    public static GateResult Evaluate(in ToolCallContext context, string workspaceRoot, PermissionProfile profile)
    {
        if (profile == PermissionProfile.Locked && context.SideEffect != SideEffect.ReadOnly)
            return GateResult.Deny(RiskLevel.High, "locked profile denies all non read-only effects");

        foreach (var touched in context.TouchedPaths)
        {
            if (PathConfinement.IsSensitivePath(touched))
                return GateResult.Deny(RiskLevel.High, $"sensitive path: {Path.GetFileName(touched)}");
            if (!PathConfinement.IsWithin(workspaceRoot, touched))
                return GateResult.Deny(RiskLevel.High, "path escapes the workspace");
        }

        switch (context.SideEffect)
        {
            case SideEffect.ReadOnly:
                if (profile == PermissionProfile.ReadOnly && context.RisksNetwork)
                    return GateResult.Deny(RiskLevel.Medium, "read-only profile denies network effects");
                return GateResult.Pass(RiskLevel.Low, "read-only effect");

            case SideEffect.Exec:
                if (profile == PermissionProfile.ReadOnly)
                    return GateResult.Deny(RiskLevel.Medium, "read-only profile denies command execution");
                if (SecretScanner.ContainsSecret(context.RawArgs))
                    return GateResult.Deny(RiskLevel.High, "command appears to contain secrets");
                foreach (var fragment in DangerousShellFragments)
                {
                    if (context.RawArgs.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                        return GateResult.Deny(RiskLevel.High, "dangerous command pattern");
                }
                var command = ExtractShellCommand(context.RawArgs);
                if (command is not null && IsReadOnlyShellCommand(command))
                    return GateResult.Allow(RiskLevel.Low, "read-only inspection command");
                return GateResult.Pass(RiskLevel.High, "shell execution");

            case SideEffect.Network:
                if (profile == PermissionProfile.ReadOnly)
                    return GateResult.Deny(RiskLevel.Medium, "read-only profile denies network effects");
                return GateResult.Pass(RiskLevel.Medium, "network effect");

            case SideEffect.Destructive:
                return GateResult.Deny(RiskLevel.High, "destructive effects are denied by policy");

            case SideEffect.Write:
                if (profile == PermissionProfile.ReadOnly)
                    return GateResult.Deny(RiskLevel.Medium, "read-only profile denies writes");
                foreach (var touched in context.TouchedPaths)
                {
                    var name = Path.GetFileName(touched).ToLowerInvariant();
                    if (name is "package-lock.json" or "pnpm-lock.yaml" or "yarn.lock"
                        || (Path.GetDirectoryName(touched)?.Contains(".github", StringComparison.Ordinal) ?? false))
                        return GateResult.Pass(RiskLevel.High, "write to protected config");
                }
                return GateResult.Pass(RiskLevel.Medium, "workspace write");

            default:
                return GateResult.Pass(RiskLevel.Medium, "unclassified effect");
        }
    }

    public static bool IsReadOnlyShellCommand(string command)
    {
        var first = command.TrimStart().Split(' ', 2).FirstOrDefault() ?? "";
        return ReadOnlyShellAllowlist.Contains(first.ToLowerInvariant());
    }

    private static string? ExtractShellCommand(string rawArgs)
    {
        try
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(rawArgs);
            return (string?)node?["command"];
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>
/// Layer B — the model reviewer. It only sees gate-pass requests; the verdict protocol
/// is plain text with a final ALLOW/DENY line and a non-empty reason. Any failure
/// falls back to asking the user; the reviewer can never silently approve.
/// </summary>
public sealed class ModelPermissionReviewer
{
    private const int ReasonCharacterLimit = 240;

    private readonly IChatProvider _provider;
    private readonly string _model;

    public ModelPermissionReviewer(IChatProvider provider, string model)
    {
        _provider = provider;
        _model = model;
    }

    public string Model => _model;

    public async Task<PermissionOutcome> ReviewAsync(
        ToolCallContext context,
        GateResult gate,
        string workspaceRoot,
        CancellationToken ct = default)
    {
        if (SecretScanner.ContainsSecret(context.RawArgs))
            return new PermissionOutcome
            {
                Decision = PermissionDecision.AskUser,
                Risk = gate.Risk,
                Reason = "arguments appear to contain secrets; only the user may approve",
                Source = "reviewer",
            };

        var system = """
You are the permission reviewer for a local coding agent. Decide whether the requested
tool call is safe to run inside the user's workspace. Be conservative: destructive
actions, secret exposure, or out-of-scope effects must be denied.

Respond in plain text only, no code fences, no JSON: one short reason line, then a
final line that is exactly ALLOW or DENY.
""";
        var user = $"""
<<<REVIEW_TARGET (untrusted data)>>>
tool: {context.ToolName}
agent: {context.Agent ?? "main"}
risk: {gate.Risk.ToWire()}
gate reason: {gate.Reason}
workspace: {workspaceRoot}
arguments: {Truncate(context.RawArgs, 4000)}
<<<END REVIEW_TARGET>>>
""";

        string text;
        try
        {
            var sb = new System.Text.StringBuilder();
            await foreach (var chunk in _provider.StreamChatAsync(new ChatRequest
                           {
                               Model = _model,
                               Messages =
                               [
                                   new ChatMessage { Role = "system", Content = system },
                                   new ChatMessage { Role = "user", Content = user },
                               ],
                               IncludeUsage = false,
                           }, ct).ConfigureAwait(false))
            {
                if (chunk is ChatChunk.Delta delta) sb.Append(delta.Text);
            }
            text = sb.ToString();
        }
        catch (Exception ex)
        {
            return ReviewerFailure($"reviewer unavailable: {ex.Message}", gate);
        }

        return ParseVerdict(text, gate);
    }

    internal static PermissionOutcome ParseVerdict(string text, GateResult gate)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
            return ReviewerFailure("reviewer produced no output", gate);

        var verdictLine = lines[^1].Trim();
        // Reject code fences / JSON — the contract is plain text only.
        if (verdictLine.StartsWith("```", StringComparison.Ordinal) || verdictLine.StartsWith("{", StringComparison.Ordinal))
            return ReviewerFailure("reviewer violated the verdict protocol", gate);

        var reason = string.Join(' ', lines[..^1]).Trim();
        if (reason.Length == 0)
            return ReviewerFailure("reviewer produced no reason", gate);
        if (reason.Length > ReasonCharacterLimit) reason = reason[..ReasonCharacterLimit];

        if (verdictLine.Equals("ALLOW", StringComparison.OrdinalIgnoreCase))
            return new PermissionOutcome
            {
                Decision = PermissionDecision.Allow,
                Risk = gate.Risk,
                Reason = reason,
                Source = "automatic_reviewer",
            };
        if (verdictLine.Equals("DENY", StringComparison.OrdinalIgnoreCase))
            return new PermissionOutcome
            {
                Decision = PermissionDecision.Deny,
                Risk = gate.Risk,
                Reason = reason,
                Source = "automatic_reviewer",
            };
        return ReviewerFailure("reviewer verdict line is neither ALLOW nor DENY", gate);
    }

    private static PermissionOutcome ReviewerFailure(string reason, GateResult gate) => new()
    {
        Decision = PermissionDecision.AskUser,
        Risk = gate.Risk,
        Reason = reason,
        Source = "automatic_reviewer_failure",
    };

    private static string Truncate(string value, int limit)
        => value.Length <= limit ? value : value[..limit] + "…";
}

/// <summary>Layer C — the user (or a host-authored decision) settles pending requests.</summary>
public interface IPermissionResponder
{
    string ApprovalMode { get; } // manual | automatic_reviewer

    Task<PermissionDecision> RequestApprovalAsync(PermissionRequestCard request, CancellationToken ct = default);
}

/// <summary>Sequential composition of the three permission layers.</summary>
public sealed class PermissionEngine
{
    private readonly ModelPermissionReviewer? _reviewer;
    private readonly IPermissionResponder _responder;

    public PermissionEngine(ModelPermissionReviewer? reviewer, IPermissionResponder responder)
    {
        _reviewer = reviewer;
        _responder = responder;
    }

    public bool HasReviewer => _reviewer is not null;
    public string ReviewerModel => _reviewer?.Model ?? "";

    public async Task<PermissionOutcome> DecideAsync(
        ToolCallContext context,
        string workspaceRoot,
        PermissionProfile profile,
        CancellationToken ct = default)
    {
        var gate = DeterministicPolicyGate.Evaluate(context, workspaceRoot, profile);
        if (gate.Verdict == "deny")
            return new PermissionOutcome
            {
                Decision = PermissionDecision.Deny,
                Risk = gate.Risk,
                Reason = gate.Reason,
                Source = "deterministic_policy",
            };
        if (gate.Verdict == "allow")
            return new PermissionOutcome
            {
                Decision = PermissionDecision.Allow,
                Risk = gate.Risk,
                Reason = gate.Reason,
                Source = "deterministic_policy",
            };

        // gate == pass: route through the reviewer when available, else the user.
        PermissionOutcome outcome;
        if (_reviewer is not null)
        {
            outcome = await _reviewer.ReviewAsync(context, gate, workspaceRoot, ct).ConfigureAwait(false);
        }
        else
        {
            outcome = new PermissionOutcome
            {
                Decision = PermissionDecision.AskUser,
                Risk = gate.Risk,
                Reason = gate.Reason,
                Source = "deterministic_policy",
            };
        }

        if (outcome.Decision == PermissionDecision.AskUser)
        {
            var request = new PermissionRequestCard
            {
                RequestId = IdGen.Random("req_"),
                Tool = context.ToolName,
                Args = context.RawArgs,
                Risk = outcome.Risk,
                Reason = outcome.Reason,
                Agent = context.Agent,
            };
            var decision = await _responder.RequestApprovalAsync(request, ct).ConfigureAwait(false);
            return new PermissionOutcome
            {
                Decision = decision,
                Risk = outcome.Risk,
                Reason = outcome.Reason,
                Source = "user",
            };
        }
        return outcome;
    }
}
