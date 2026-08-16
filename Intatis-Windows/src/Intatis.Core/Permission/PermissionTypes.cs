namespace Intatis.Core.Permission;

public enum SideEffect
{
    ReadOnly,
    Write,
    Exec,
    Network,
    Destructive,
}

public enum PermissionProfile
{
    Manual,
    Reviewed,
    ReadOnly,
    Locked,
}

public static class PermissionProfileExtensions
{
    public static string ToWire(this PermissionProfile profile) => profile switch
    {
        PermissionProfile.Reviewed => "reviewed",
        PermissionProfile.ReadOnly => "read_only",
        PermissionProfile.Locked => "locked",
        _ => "manual",
    };

    public static PermissionProfile FromWire(string? value) => value switch
    {
        "reviewed" => PermissionProfile.Reviewed,
        "read_only" or "readonly" => PermissionProfile.ReadOnly,
        "locked" => PermissionProfile.Locked,
        _ => PermissionProfile.Manual,
    };
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
}

public static class RiskLevelExtensions
{
    public static string ToWire(this RiskLevel risk) => risk switch
    {
        RiskLevel.Low => "low",
        RiskLevel.Medium => "medium",
        _ => "high",
    };

    public static RiskLevel FromWire(string? value) => value switch
    {
        "low" => RiskLevel.Low,
        "high" => RiskLevel.High,
        _ => RiskLevel.Medium,
    };
}

public enum PermissionDecision
{
    Allow,
    Deny,
    AskUser,
}

/// <summary>Layer A verdict: a deterministic deny is final and never reviewed.</summary>
public sealed record GateResult
{
    public string Verdict { get; init; } = "pass"; // deny | pass | allow
    public RiskLevel Risk { get; init; } = RiskLevel.Medium;
    public string Reason { get; init; } = "";

    public static GateResult Deny(RiskLevel risk, string reason) => new() { Verdict = "deny", Risk = risk, Reason = reason };
    public static GateResult Pass(RiskLevel risk, string reason) => new() { Verdict = "pass", Risk = risk, Reason = reason };
    public static GateResult Allow(RiskLevel risk, string reason) => new() { Verdict = "allow", Risk = risk, Reason = reason };
}

public sealed record ToolCallContext
{
    public required string ToolName { get; init; }
    public SideEffect SideEffect { get; init; }
    public List<string> TouchedPaths { get; init; } = [];
    public bool RisksNetwork { get; init; }
    public string RawArgs { get; init; } = "";
    public string? Agent { get; init; }
}

public sealed record PermissionOutcome
{
    public required PermissionDecision Decision { get; init; }
    public RiskLevel Risk { get; init; } = RiskLevel.Medium;
    public string Reason { get; init; } = "";
    public string Source { get; init; } = "deterministic_policy";
}

/// <summary>A pending permission request as surfaced to the user (Layer C).</summary>
public sealed record PermissionRequestCard
{
    public required string RequestId { get; init; }
    public required string Tool { get; init; }
    public required string Args { get; init; }
    public RiskLevel Risk { get; init; } = RiskLevel.Medium;
    public string Reason { get; init; } = "";
    public string? Agent { get; init; }
}
