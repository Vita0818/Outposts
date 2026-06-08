using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── CanonicalProductionSideEffect ───────────────────────────────────────────

public sealed class CanonicalProductionSideEffect : IEquatable<CanonicalProductionSideEffect>
{
    public string Id => string.Join("|", Kind.ToString(), Domain.ToString(), ObjectID ?? "", ArtifactID ?? "", Route?.ToString() ?? "");

    public CanonicalProductionSideEffectKind Kind { get; }
    public CanonicalProductionDomain Domain { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalTransportRoute? Route { get; }
    public long? ByteSize { get; }
    public string? HashPrefix { get; }
    public string RedactedSummary { get; }

    public CanonicalProductionSideEffect(
        CanonicalProductionSideEffectKind kind,
        CanonicalProductionDomain domain,
        string? objectID = null,
        string? artifactID = null,
        CanonicalTransportRoute? route = null,
        long? byteSize = null,
        CanonicalHash? hash = null,
        string? hashPrefix = null,
        string summary = "")
    {
        Kind = kind;
        Domain = domain;
        ObjectID = objectID is not null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown") : null;
        ArtifactID = artifactID is not null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        Route = route;
        ByteSize = byteSize;
        HashPrefix = hash is not null ? CanonicalProductionRedaction.HashPrefix(hash.Value.Value) :
                     CanonicalProductionRedaction.HashPrefix(hashPrefix);
        RedactedSummary = CanonicalProductionRedaction.SafeDiagnosticText(summary) ?? kind.ToString();
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionSideEffect other && Equals(other);
    public bool Equals(CanonicalProductionSideEffect? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalProductionSideEffect l, CanonicalProductionSideEffect r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionSideEffect l, CanonicalProductionSideEffect r) => !l.Equals(r);
}

// ─── CanonicalProductionExecutionTrace ───────────────────────────────────────

public sealed class CanonicalProductionExecutionTrace : IEquatable<CanonicalProductionExecutionTrace>
{
    public string OperationID { get; }
    public CanonicalKernelExecutionMode Mode { get; }
    public CanonicalTimestamp GeneratedAt { get; }
    public CanonicalProductionSideEffect[] SideEffects { get; }

    public CanonicalProductionExecutionTrace(
        string operationID,
        CanonicalKernelExecutionMode mode,
        CanonicalProductionSideEffect[]? sideEffects = null,
        DateTime generatedAt = default)
    {
        OperationID = CanonicalProductionRedaction.SafeIdentifier(operationID, "canonical-operation");
        Mode = mode;
        GeneratedAt = new CanonicalTimestamp(generatedAt == default ? DateTime.UtcNow : generatedAt);
        SideEffects = sideEffects ?? Array.Empty<CanonicalProductionSideEffect>();
    }

    public string[] RedactedSummaries => SideEffects.Select(s => s.RedactedSummary).ToArray();

    public override bool Equals(object? obj) => obj is CanonicalProductionExecutionTrace other && Equals(other);
    public bool Equals(CanonicalProductionExecutionTrace? other) =>
        other is not null && OperationID == other.OperationID && Mode == other.Mode;
    public override int GetHashCode() => HashCode.Combine(OperationID, Mode);
    public static bool operator ==(CanonicalProductionExecutionTrace l, CanonicalProductionExecutionTrace r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionExecutionTrace l, CanonicalProductionExecutionTrace r) => !l.Equals(r);
}

// ─── CanonicalProductionExecutionFailure ─────────────────────────────────────

public sealed class CanonicalProductionExecutionFailure : IEquatable<CanonicalProductionExecutionFailure>
{
    public string Id => string.Join("|", OperationID, Reason, Domain?.ToString() ?? "");

    public string OperationID { get; }
    public CanonicalProductionDomain? Domain { get; }
    public string Reason { get; }

    public CanonicalProductionExecutionFailure(string operationID, CanonicalProductionDomain? domain = null, string reason = "")
    {
        OperationID = CanonicalProductionRedaction.SafeIdentifier(operationID, "canonical-operation");
        Domain = domain;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? "productionExecutionFailed";
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionExecutionFailure other && Equals(other);
    public bool Equals(CanonicalProductionExecutionFailure? other) =>
        other is not null && OperationID == other.OperationID && Domain == other.Domain && Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(OperationID, Domain, Reason);
    public static bool operator ==(CanonicalProductionExecutionFailure l, CanonicalProductionExecutionFailure r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionExecutionFailure l, CanonicalProductionExecutionFailure r) => !l.Equals(r);
}

// ─── CanonicalProductionExecutionResult ──────────────────────────────────────

public sealed class CanonicalProductionExecutionResult : IEquatable<CanonicalProductionExecutionResult>
{
    public string OperationID { get; }
    public CanonicalKernelExecutionMode Mode { get; }
    public bool Succeeded { get; }
    public CanonicalProductionExecutionTrace Trace { get; }
    public CanonicalProductionExecutionFailure[] Failures { get; }
    public CanonicalProductionExecutionAudit? GuardAudit { get; }

    public CanonicalProductionExecutionResult(
        string operationID,
        CanonicalKernelExecutionMode mode,
        bool succeeded,
        CanonicalProductionSideEffect[]? sideEffects = null,
        CanonicalProductionExecutionFailure[]? failures = null,
        CanonicalProductionExecutionAudit? guardAudit = null,
        DateTime generatedAt = default)
    {
        OperationID = CanonicalProductionRedaction.SafeIdentifier(operationID, "canonical-operation");
        Mode = mode;
        Succeeded = succeeded;
        Trace = new CanonicalProductionExecutionTrace(
            operationID: OperationID,
            mode: mode,
            sideEffects: sideEffects,
            generatedAt: generatedAt
        );
        Failures = failures ?? Array.Empty<CanonicalProductionExecutionFailure>();
        GuardAudit = guardAudit;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionExecutionResult other && Equals(other);
    public bool Equals(CanonicalProductionExecutionResult? other) =>
        other is not null && OperationID == other.OperationID && Mode == other.Mode;
    public override int GetHashCode() => HashCode.Combine(OperationID, Mode);
    public static bool operator ==(CanonicalProductionExecutionResult l, CanonicalProductionExecutionResult r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionExecutionResult l, CanonicalProductionExecutionResult r) => !l.Equals(r);
}

// ─── CanonicalProductionExecutionDomainRole ──────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionExecutionDomainRole
{
    iPhone,
    mac,
    testHarness
}

// ─── CanonicalProductionExecutionToken ───────────────────────────────────────

public sealed class CanonicalProductionExecutionToken : IEquatable<CanonicalProductionExecutionToken>
{
    public CanonicalKernelExecutionMode Mode { get; }
    public CanonicalProductionDomain[] DomainAllowlist { get; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; }
    public string SyncRunID { get; }
    public string? DryRunEquivalentReportID { get; }
    public string? RollbackPlanID { get; }
    public bool OwnerApproved { get; }

    public CanonicalProductionExecutionToken(
        CanonicalKernelExecutionMode mode,
        CanonicalProductionDomain[] domainAllowlist,
        CanonicalProductionExecutionDomainRole nodeRole,
        string syncRunID,
        string? dryRunEquivalentReportID = null,
        string? rollbackPlanID = null,
        bool ownerApproved = false)
    {
        Mode = mode;
        DomainAllowlist = new HashSet<CanonicalProductionDomain>(domainAllowlist)
            .OrderBy(d => d.ToString(), StringComparer.Ordinal)
            .ToArray();
        NodeRole = nodeRole;
        SyncRunID = CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run");
        DryRunEquivalentReportID = dryRunEquivalentReportID is not null
            ? CanonicalProductionRedaction.SafeIdentifier(dryRunEquivalentReportID, "dry-run-report")
            : null;
        RollbackPlanID = rollbackPlanID is not null
            ? CanonicalProductionRedaction.SafeIdentifier(rollbackPlanID, "rollback-plan")
            : null;
        OwnerApproved = ownerApproved;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionExecutionToken other && Equals(other);
    public bool Equals(CanonicalProductionExecutionToken? other) =>
        other is not null && Mode == other.Mode && SyncRunID == other.SyncRunID;
    public override int GetHashCode() => HashCode.Combine(Mode, SyncRunID);
    public static bool operator ==(CanonicalProductionExecutionToken l, CanonicalProductionExecutionToken r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionExecutionToken l, CanonicalProductionExecutionToken r) => !l.Equals(r);
}

// ─── CanonicalProductionExecutionPolicy ──────────────────────────────────────

public sealed class CanonicalProductionExecutionPolicy : IEquatable<CanonicalProductionExecutionPolicy>
{
    public CanonicalProductionDomain[] RequiredDomains { get; }
    public CanonicalProductionPortKind[] RequiredPorts { get; }
    public bool RequireOwnerApproval { get; }
    public bool RequireRollbackPlan { get; }
    public bool RequireDryRunEquivalence { get; }
    public bool RequireMigrationGateUnblocked { get; }
    public bool RejectUnresolvedConflicts { get; }

    public CanonicalProductionExecutionPolicy(
        CanonicalProductionDomain[]? requiredDomains = null,
        CanonicalProductionPortKind[]? requiredPorts = null,
        bool requireOwnerApproval = true,
        bool requireRollbackPlan = true,
        bool requireDryRunEquivalence = true,
        bool requireMigrationGateUnblocked = true,
        bool rejectUnresolvedConflicts = true)
    {
        RequiredDomains = (requiredDomains is not null
            ? new HashSet<CanonicalProductionDomain>(requiredDomains)
            : new HashSet<CanonicalProductionDomain> { CanonicalProductionDomain.recordingMetadata, CanonicalProductionDomain.fileRuntime })
            .OrderBy(d => d.ToString(), StringComparer.Ordinal).ToArray();
        RequiredPorts = (requiredPorts is not null
            ? new HashSet<CanonicalProductionPortKind>(requiredPorts)
            : new HashSet<CanonicalProductionPortKind> { CanonicalProductionPortKind.file, CanonicalProductionPortKind.transport, CanonicalProductionPortKind.upload, CanonicalProductionPortKind.apply })
            .OrderBy(p => p.ToString(), StringComparer.Ordinal).ToArray();
        RequireOwnerApproval = requireOwnerApproval;
        RequireRollbackPlan = requireRollbackPlan;
        RequireDryRunEquivalence = requireDryRunEquivalence;
        RequireMigrationGateUnblocked = requireMigrationGateUnblocked;
        RejectUnresolvedConflicts = rejectUnresolvedConflicts;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionExecutionPolicy other && Equals(other);
    public bool Equals(CanonicalProductionExecutionPolicy? other) =>
        other is not null &&
        RequiredDomains.SequenceEqual(other.RequiredDomains) &&
        RequiredPorts.SequenceEqual(other.RequiredPorts) &&
        RequireOwnerApproval == other.RequireOwnerApproval &&
        RequireRollbackPlan == other.RequireRollbackPlan &&
        RequireDryRunEquivalence == other.RequireDryRunEquivalence &&
        RequireMigrationGateUnblocked == other.RequireMigrationGateUnblocked &&
        RejectUnresolvedConflicts == other.RejectUnresolvedConflicts;
    public override int GetHashCode() => HashCode.Combine(
        RequiredDomains.Length, RequiredPorts.Length, RequireOwnerApproval, RequireRollbackPlan,
        RequireDryRunEquivalence, RequireMigrationGateUnblocked, RejectUnresolvedConflicts);
    public static bool operator ==(CanonicalProductionExecutionPolicy l, CanonicalProductionExecutionPolicy r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionExecutionPolicy l, CanonicalProductionExecutionPolicy r) => !l.Equals(r);
}

// ─── CanonicalProductionExecutionRejectionReason ─────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionExecutionRejectionReason
{
    modeDisabled,
    blockedProductionExecute,
    missingApproval,
    missingRollbackPlan,
    dryRunNotEquivalent,
    unsupportedDomain,
    unresolvedConflict,
    missingProductionPort,
    productionMigrationBlocked
}

// ─── CanonicalProductionExecutionAudit ───────────────────────────────────────

public sealed class CanonicalProductionExecutionAudit : IEquatable<CanonicalProductionExecutionAudit>
{
    public CanonicalTimestamp GeneratedAt { get; }
    public bool Allowed { get; }
    public CanonicalKernelExecutionMode Mode { get; }
    public CanonicalProductionExecutionDomainRole? NodeRole { get; }
    public CanonicalKernelExecutionMode RequestedMode { get; }
    public CanonicalKernelExecutionMode? AllowedMode { get; }
    public string? TokenSyncRunID { get; }
    public CanonicalProductionExecutionRejectionReason[] RejectionReasons { get; }
    public CanonicalProductionSideEffectKind[] DeniedSideEffects { get; }
    public bool RollbackAvailable { get; }
    public bool DryRunEquivalent { get; }
    public int UnresolvedConflictCount { get; }
    public CanonicalProductionDomain[] Domains { get; }

    public CanonicalProductionExecutionAudit(
        bool allowed,
        CanonicalKernelExecutionMode mode,
        CanonicalProductionExecutionDomainRole? nodeRole = null,
        CanonicalKernelExecutionMode? requestedMode = null,
        CanonicalKernelExecutionMode? allowedMode = null,
        string? tokenSyncRunID = null,
        CanonicalProductionExecutionRejectionReason[]? rejectionReasons = null,
        CanonicalProductionSideEffectKind[]? deniedSideEffects = null,
        bool rollbackAvailable = false,
        bool dryRunEquivalent = false,
        int unresolvedConflictCount = 0,
        CanonicalProductionDomain[]? domains = null,
        DateTime generatedAt = default)
    {
        GeneratedAt = new CanonicalTimestamp(generatedAt == default ? DateTime.UtcNow : generatedAt);
        Allowed = allowed;
        Mode = mode;
        NodeRole = nodeRole;
        RequestedMode = requestedMode ?? mode;
        AllowedMode = allowedMode;
        TokenSyncRunID = tokenSyncRunID is not null
            ? CanonicalProductionRedaction.SafeIdentifier(tokenSyncRunID, "sync-run")
            : null;
        RejectionReasons = (rejectionReasons is not null
            ? new HashSet<CanonicalProductionExecutionRejectionReason>(rejectionReasons)
            : new HashSet<CanonicalProductionExecutionRejectionReason>())
            .OrderBy(r => r.ToString(), StringComparer.Ordinal)
            .ToArray();
        DeniedSideEffects = (deniedSideEffects is not null
            ? new HashSet<CanonicalProductionSideEffectKind>(deniedSideEffects)
            : new HashSet<CanonicalProductionSideEffectKind>())
            .OrderBy(s => s.ToString(), StringComparer.Ordinal)
            .ToArray();
        RollbackAvailable = rollbackAvailable;
        DryRunEquivalent = dryRunEquivalent;
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        Domains = (domains is not null
            ? new HashSet<CanonicalProductionDomain>(domains)
            : new HashSet<CanonicalProductionDomain>())
            .OrderBy(d => d.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionExecutionAudit other && Equals(other);
    public bool Equals(CanonicalProductionExecutionAudit? other) =>
        other is not null && Allowed == other.Allowed && Mode == other.Mode;
    public override int GetHashCode() => HashCode.Combine(Allowed, Mode);
    public static bool operator ==(CanonicalProductionExecutionAudit l, CanonicalProductionExecutionAudit r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionExecutionAudit l, CanonicalProductionExecutionAudit r) => !l.Equals(r);
}

// ─── CanonicalRollbackCheckpoint ─────────────────────────────────────────────

public sealed class CanonicalRollbackCheckpoint : IEquatable<CanonicalRollbackCheckpoint>
{
    public string Id => CheckpointID;

    public string CheckpointID { get; }
    public CanonicalProductionDomain Domain { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public string? AtomicBackupToken { get; }

    public CanonicalRollbackCheckpoint(
        string checkpointID,
        CanonicalProductionDomain domain,
        string? objectID = null,
        string? artifactID = null,
        string? atomicBackupToken = null)
    {
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "checkpoint");
        Domain = domain;
        ObjectID = objectID is not null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown") : null;
        ArtifactID = artifactID is not null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        AtomicBackupToken = atomicBackupToken is not null
            ? CanonicalProductionRedaction.SafeIdentifier(atomicBackupToken, "backup-token")
            : null;
    }

    public override bool Equals(object? obj) => obj is CanonicalRollbackCheckpoint other && Equals(other);
    public bool Equals(CanonicalRollbackCheckpoint? other) =>
        other is not null && CheckpointID == other.CheckpointID;
    public override int GetHashCode() => CheckpointID.GetHashCode();
    public static bool operator ==(CanonicalRollbackCheckpoint l, CanonicalRollbackCheckpoint r) => l.Equals(r);
    public static bool operator !=(CanonicalRollbackCheckpoint l, CanonicalRollbackCheckpoint r) => !l.Equals(r);
}

// ─── CanonicalRollbackActionKind ─────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRollbackActionKind
{
    metadataRollback,
    generatedArtifactRollback,
    tombstoneRollback,
    uploadSessionCancel,
    transportNoOpRollback,
    conflictLedgerNoOp,
    fileWriteRollback
}

// ─── CanonicalRollbackAction ─────────────────────────────────────────────────

public sealed class CanonicalRollbackAction : IEquatable<CanonicalRollbackAction>
{
    public string Id => ActionID;

    public string ActionID { get; }
    public CanonicalRollbackActionKind Kind { get; }
    public CanonicalProductionDomain Domain { get; }
    public string? CheckpointID { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }

    public CanonicalRollbackAction(
        string actionID,
        CanonicalRollbackActionKind kind,
        CanonicalProductionDomain domain,
        string? checkpointID = null,
        string? objectID = null,
        string? artifactID = null)
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, kind.ToString());
        Kind = kind;
        Domain = domain;
        CheckpointID = checkpointID is not null
            ? CanonicalProductionRedaction.SafeIdentifier(checkpointID, "checkpoint")
            : null;
        ObjectID = objectID is not null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown") : null;
        ArtifactID = artifactID is not null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
    }

    public override bool Equals(object? obj) => obj is CanonicalRollbackAction other && Equals(other);
    public bool Equals(CanonicalRollbackAction? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
    public static bool operator ==(CanonicalRollbackAction l, CanonicalRollbackAction r) => l.Equals(r);
    public static bool operator !=(CanonicalRollbackAction l, CanonicalRollbackAction r) => !l.Equals(r);
}

// ─── CanonicalRollbackPlan ───────────────────────────────────────────────────

public sealed class CanonicalRollbackPlan : IEquatable<CanonicalRollbackPlan>
{
    public string Id => PlanID;

    public string PlanID { get; }
    public CanonicalRollbackCheckpoint[] Checkpoints { get; }
    public CanonicalRollbackAction[] Actions { get; }
    public CanonicalTimestamp GeneratedAt { get; }

    public CanonicalRollbackPlan(
        string planID,
        CanonicalRollbackCheckpoint[] checkpoints,
        CanonicalRollbackAction[] actions,
        DateTime generatedAt = default)
    {
        PlanID = CanonicalProductionRedaction.SafeIdentifier(planID, "rollback-plan");
        Checkpoints = (checkpoints ?? Array.Empty<CanonicalRollbackCheckpoint>())
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToArray();
        Actions = (actions ?? Array.Empty<CanonicalRollbackAction>())
            .OrderBy(a => a.Id, StringComparer.Ordinal)
            .ToArray();
        GeneratedAt = new CanonicalTimestamp(generatedAt == default ? DateTime.UtcNow : generatedAt);
    }

    public bool Covers(CanonicalProductionDomain domain) =>
        Actions.Any(a => a.Domain == domain);

    public bool CoversAll(CanonicalProductionDomain[] domains) =>
        domains.All(Covers);

    public override bool Equals(object? obj) => obj is CanonicalRollbackPlan other && Equals(other);
    public bool Equals(CanonicalRollbackPlan? other) =>
        other is not null && PlanID == other.PlanID;
    public override int GetHashCode() => PlanID.GetHashCode();
    public static bool operator ==(CanonicalRollbackPlan l, CanonicalRollbackPlan r) => l.Equals(r);
    public static bool operator !=(CanonicalRollbackPlan l, CanonicalRollbackPlan r) => !l.Equals(r);
}

// ─── CanonicalRollbackFailure ────────────────────────────────────────────────

public sealed class CanonicalRollbackFailure : IEquatable<CanonicalRollbackFailure>
{
    public string Id => string.Join("|", ActionID, Reason);

    public string ActionID { get; }
    public string Reason { get; }

    public CanonicalRollbackFailure(string actionID, string reason)
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, "rollback-action");
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? "rollbackFailed";
    }

    public override bool Equals(object? obj) => obj is CanonicalRollbackFailure other && Equals(other);
    public bool Equals(CanonicalRollbackFailure? other) =>
        other is not null && ActionID == other.ActionID && Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(ActionID, Reason);
    public static bool operator ==(CanonicalRollbackFailure l, CanonicalRollbackFailure r) => l.Equals(r);
    public static bool operator !=(CanonicalRollbackFailure l, CanonicalRollbackFailure r) => !l.Equals(r);
}

// ─── CanonicalRollbackResult ─────────────────────────────────────────────────

public sealed class CanonicalRollbackResult : IEquatable<CanonicalRollbackResult>
{
    public string PlanID { get; }
    public bool Succeeded { get; }
    public string[] CompletedActionIDs { get; }
    public CanonicalRollbackFailure[] Failures { get; }

    public CanonicalRollbackResult(
        string planID,
        bool succeeded,
        string[]? completedActionIDs = null,
        CanonicalRollbackFailure[]? failures = null)
    {
        PlanID = CanonicalProductionRedaction.SafeIdentifier(planID, "rollback-plan");
        Succeeded = succeeded;
        CompletedActionIDs = (completedActionIDs is not null
            ? new HashSet<string>(completedActionIDs.Select(id => CanonicalProductionRedaction.SafeIdentifier(id, "rollback-action")))
            : new HashSet<string>())
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        Failures = (failures ?? Array.Empty<CanonicalRollbackFailure>())
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public override bool Equals(object? obj) => obj is CanonicalRollbackResult other && Equals(other);
    public bool Equals(CanonicalRollbackResult? other) =>
        other is not null && PlanID == other.PlanID;
    public override int GetHashCode() => PlanID.GetHashCode();
    public static bool operator ==(CanonicalRollbackResult l, CanonicalRollbackResult r) => l.Equals(r);
    public static bool operator !=(CanonicalRollbackResult l, CanonicalRollbackResult r) => !l.Equals(r);
}

// ─── CanonicalRollbackAudit ──────────────────────────────────────────────────

public sealed class CanonicalRollbackAudit : IEquatable<CanonicalRollbackAudit>
{
    public string PlanID { get; }
    public CanonicalProductionDomain[] RequiredDomains { get; }
    public CanonicalProductionDomain[] MissingDomains { get; }
    public bool RollbackRequiredForProduction { get; }

    public CanonicalRollbackAudit(CanonicalRollbackPlan? plan, CanonicalProductionDomain[] requiredDomains)
    {
        var required = new HashSet<CanonicalProductionDomain>(requiredDomains)
            .OrderBy(d => d.ToString(), StringComparer.Ordinal)
            .ToArray();
        PlanID = plan?.PlanID ?? "missing";
        RequiredDomains = required;
        MissingDomains = required.Where(domain => plan?.Covers(domain) != true).ToArray();
        RollbackRequiredForProduction = true;
    }

    public override bool Equals(object? obj) => obj is CanonicalRollbackAudit other && Equals(other);
    public bool Equals(CanonicalRollbackAudit? other) =>
        other is not null && PlanID == other.PlanID;
    public override int GetHashCode() => PlanID.GetHashCode();
    public static bool operator ==(CanonicalRollbackAudit l, CanonicalRollbackAudit r) => l.Equals(r);
    public static bool operator !=(CanonicalRollbackAudit l, CanonicalRollbackAudit r) => !l.Equals(r);
}

// ─── CanonicalProductionExecutionGuard ───────────────────────────────────────

public static class CanonicalProductionExecutionGuard
{
    public static CanonicalProductionExecutionAudit Evaluate(
        CanonicalKernelExecutionMode mode,
        CanonicalProductionExecutionToken? token,
        CanonicalProductionExecutionPolicy policy,
        CanonicalProductionDomain[] domains,
        CanonicalProductionPortSet ports,
        CanonicalRollbackPlan? rollbackPlan,
        string? dryRunReportID,
        CanonicalDryRunEquivalenceReport? dryRunEquivalence,
        CanonicalDryRunReadinessReport? readinessReport,
        int unresolvedConflictCount,
        DateTime generatedAt = default)
    {
        var reasons = new List<CanonicalProductionExecutionRejectionReason>();
        var rollbackAvailable = rollbackPlan?.CoversAll(policy.RequiredDomains) == true;
        var dryRunEquivalent = dryRunEquivalence?.LegacyEquivalence.AllEquivalent == true
            && dryRunEquivalence?.LegacyEquivalence.HasBlockingDivergence != true;

        if (mode != CanonicalKernelExecutionMode.productionExecute || token is null || token.Mode != CanonicalKernelExecutionMode.productionExecute)
        {
            return new CanonicalProductionExecutionAudit(
                allowed: false,
                mode: mode,
                nodeRole: token?.NodeRole,
                requestedMode: token?.Mode ?? mode,
                allowedMode: null,
                tokenSyncRunID: token?.SyncRunID,
                rejectionReasons: new[] { CanonicalProductionExecutionRejectionReason.modeDisabled },
                deniedSideEffects: Enum.GetValues<CanonicalProductionSideEffectKind>(),
                rollbackAvailable: rollbackAvailable,
                dryRunEquivalent: dryRunEquivalent,
                unresolvedConflictCount: unresolvedConflictCount,
                domains: domains,
                generatedAt: generatedAt
            );
        }

        if (token.NodeRole != CanonicalProductionExecutionDomainRole.testHarness)
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.blockedProductionExecute);
        }
        if (policy.RequireOwnerApproval && !token.OwnerApproved)
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.missingApproval);
        }
        if (!new HashSet<CanonicalProductionDomain>(policy.RequiredDomains).IsSubsetOf(new HashSet<CanonicalProductionDomain>(token.DomainAllowlist))
            || !new HashSet<CanonicalProductionDomain>(domains).IsSubsetOf(new HashSet<CanonicalProductionDomain>(token.DomainAllowlist)))
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.unsupportedDomain);
        }
        if (policy.RequireRollbackPlan)
        {
            if (rollbackPlan is null
                || token.RollbackPlanID is null
                || token.RollbackPlanID != rollbackPlan?.PlanID
                || rollbackPlan?.CoversAll(policy.RequiredDomains) != true)
            {
                reasons.Add(CanonicalProductionExecutionRejectionReason.missingRollbackPlan);
            }
        }
        if (policy.RequireDryRunEquivalence)
        {
            if (dryRunEquivalence is null
                || dryRunEquivalence.LegacyEquivalence.AllEquivalent != true
                || dryRunEquivalence.LegacyEquivalence.HasBlockingDivergence == true
                || token.DryRunEquivalentReportID is null
                || token.DryRunEquivalentReportID != dryRunReportID)
            {
                reasons.Add(CanonicalProductionExecutionRejectionReason.dryRunNotEquivalent);
            }
        }
        if (policy.RejectUnresolvedConflicts && unresolvedConflictCount > 0)
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.unresolvedConflict);
        }
        if (policy.RequiredPorts.Any(port => PortMissingOrDryRun(port, ports)))
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.missingProductionPort);
        }
        if (policy.RequireMigrationGateUnblocked && readinessReport?.ProductionMigrationBlocked != false)
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.productionMigrationBlocked);
        }

        return new CanonicalProductionExecutionAudit(
            allowed: reasons.Count == 0,
            mode: mode,
            nodeRole: token.NodeRole,
            requestedMode: token.Mode,
            allowedMode: reasons.Count == 0 ? CanonicalKernelExecutionMode.productionExecute : null,
            tokenSyncRunID: token.SyncRunID,
            rejectionReasons: reasons.ToArray(),
            deniedSideEffects: reasons.Count == 0
                ? Array.Empty<CanonicalProductionSideEffectKind>()
                : Enum.GetValues<CanonicalProductionSideEffectKind>(),
            rollbackAvailable: rollbackAvailable,
            dryRunEquivalent: dryRunEquivalent,
            unresolvedConflictCount: unresolvedConflictCount,
            domains: domains,
            generatedAt: generatedAt
        );
    }

    public static CanonicalProductionExecutionAudit EvaluateShadow(
        CanonicalKernelExecutionMode mode,
        CanonicalProductionExecutionToken? token,
        CanonicalProductionDomain[] domains,
        CanonicalRollbackPlan? rollbackPlan,
        CanonicalDryRunEquivalenceReport? dryRunEquivalence,
        int unresolvedConflictCount,
        DateTime generatedAt = default)
    {
        var requestedMode = token?.Mode ?? mode;
        var nodeRole = token?.NodeRole;
        var reasons = new List<CanonicalProductionExecutionRejectionReason>();

        if (mode == CanonicalKernelExecutionMode.productionExecute || requestedMode == CanonicalKernelExecutionMode.productionExecute)
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.blockedProductionExecute);
        }
        if (!mode.IsShadowPreparationMode() || requestedMode != mode)
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.modeDisabled);
        }
        var roleAllowed = nodeRole is CanonicalProductionExecutionDomainRole.iPhone
            or CanonicalProductionExecutionDomainRole.mac
            or CanonicalProductionExecutionDomainRole.testHarness;
        if (!roleAllowed)
        {
            reasons.Add(CanonicalProductionExecutionRejectionReason.productionMigrationBlocked);
        }

        var dryRunEquivalent = dryRunEquivalence?.LegacyEquivalence.AllEquivalent == true
            && dryRunEquivalence?.LegacyEquivalence.HasBlockingDivergence != true;

        return new CanonicalProductionExecutionAudit(
            allowed: reasons.Count == 0,
            mode: mode,
            nodeRole: nodeRole,
            requestedMode: requestedMode,
            allowedMode: reasons.Count == 0 ? mode : null,
            tokenSyncRunID: token?.SyncRunID,
            rejectionReasons: reasons.ToArray(),
            deniedSideEffects: Enum.GetValues<CanonicalProductionSideEffectKind>(),
            rollbackAvailable: rollbackPlan is not null,
            dryRunEquivalent: dryRunEquivalent,
            unresolvedConflictCount: unresolvedConflictCount,
            domains: domains,
            generatedAt: generatedAt
        );
    }

    private static bool PortMissingOrDryRun(CanonicalProductionPortKind port, CanonicalProductionPortSet ports)
    {
        return port switch
        {
            CanonicalProductionPortKind.file => ports.File is null || ports.File.IsDryRunOnly,
            CanonicalProductionPortKind.transport => ports.Transport is null || ports.Transport.IsDryRunOnly,
            CanonicalProductionPortKind.upload => ports.Upload is null || ports.Upload.IsDryRunOnly,
            CanonicalProductionPortKind.apply => ports.Apply is null || ports.Apply.IsDryRunOnly,
            CanonicalProductionPortKind.syncClock => ports.SyncClock is null,
            CanonicalProductionPortKind.diagnostics => ports.Diagnostics is null,
            CanonicalProductionPortKind.capability => ports.Capability is null,
            _ => true
        };
    }
}
