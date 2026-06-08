using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyExecutionStatus
{
    applied,
    sent,
    noOp,
    conflictRecorded,
    deferredUnsupported,
    failed
}

public sealed class CanonicalApplyExecutionRecord : IEquatable<CanonicalApplyExecutionRecord>
{
    public string Id => ActionID;
    public string ActionID { get; }
    public CanonicalApplyActionKind Kind { get; }
    public CanonicalApplyTarget Target { get; }
    public CanonicalApplyExecutionStatus Status { get; }
    public string? ContentHashPrefix { get; }
    public long? ByteSize { get; }
    public CanonicalApplyFailureReason? Failure { get; }
    public string? Detail { get; }

    public CanonicalApplyExecutionRecord(
        string actionID,
        CanonicalApplyActionKind kind,
        CanonicalApplyTarget target,
        CanonicalApplyExecutionStatus status,
        string? contentHashPrefix = null,
        long? byteSize = null,
        CanonicalApplyFailureReason? failure = null,
        string? detail = null)
    {
        ActionID = actionID;
        Kind = kind;
        Target = target;
        Status = status;
        ContentHashPrefix = contentHashPrefix;
        ByteSize = byteSize;
        Failure = failure;
        Detail = detail;
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyExecutionRecord other && Equals(other);
    public bool Equals(CanonicalApplyExecutionRecord? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
    public static bool operator ==(CanonicalApplyExecutionRecord left, CanonicalApplyExecutionRecord right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyExecutionRecord left, CanonicalApplyExecutionRecord right) => !left.Equals(right);
}

public sealed class CanonicalApplyExecutionReport : IEquatable<CanonicalApplyExecutionReport>
{
    public CanonicalApplyExecutionRecord[] Records { get; }
    public CanonicalConflictResolverReport ConflictReport { get; }
    public int AppliedCount { get; }
    public int FailedCount { get; }

    public CanonicalApplyExecutionReport(
        CanonicalApplyExecutionRecord[] records,
        CanonicalConflictResolverReport conflictReport,
        int appliedCount,
        int failedCount)
    {
        Records = records ?? Array.Empty<CanonicalApplyExecutionRecord>();
        ConflictReport = conflictReport;
        AppliedCount = appliedCount;
        FailedCount = failedCount;
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyExecutionReport other && Equals(other);
    public bool Equals(CanonicalApplyExecutionReport? other) =>
        other is not null &&
        Records.SequenceEqual(other.Records) &&
        ConflictReport.Equals(other.ConflictReport) &&
        AppliedCount == other.AppliedCount &&
        FailedCount == other.FailedCount;
    public override int GetHashCode() => HashCode.Combine(AppliedCount, FailedCount);
    public static bool operator ==(CanonicalApplyExecutionReport left, CanonicalApplyExecutionReport right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyExecutionReport left, CanonicalApplyExecutionReport right) => !left.Equals(right);
}

public sealed class CanonicalApplyRuntimeContext
{
    public CanonicalManifest LocalManifest { get; }
    public CanonicalManifest PeerManifest { get; }
    public ICanonicalFileStorePort LocalFileStore { get; }
    public ICanonicalFileStorePort PeerFileStore { get; }
    public CanonicalRootToken LocalMetadataRoot { get; }
    public CanonicalRootToken PeerMetadataRoot { get; }
    public CanonicalRootToken LocalGeneratedRoot { get; }
    public CanonicalRootToken PeerGeneratedRoot { get; }

    public CanonicalApplyRuntimeContext(
        CanonicalManifest localManifest,
        CanonicalManifest peerManifest,
        ICanonicalFileStorePort localFileStore,
        ICanonicalFileStorePort peerFileStore,
        CanonicalRootToken localMetadataRoot,
        CanonicalRootToken peerMetadataRoot,
        CanonicalRootToken localGeneratedRoot,
        CanonicalRootToken peerGeneratedRoot)
    {
        LocalManifest = localManifest;
        PeerManifest = peerManifest;
        LocalFileStore = localFileStore;
        PeerFileStore = peerFileStore;
        LocalMetadataRoot = localMetadataRoot;
        PeerMetadataRoot = peerMetadataRoot;
        LocalGeneratedRoot = localGeneratedRoot;
        PeerGeneratedRoot = peerGeneratedRoot;
    }
}

public enum CanonicalApplyRuntimeError
{
    MissingSourceObject,
    MissingSourceArtifact,
    MissingLogicalPathToken,
    HashOrSizeMismatch
}

public class CanonicalApplyRuntimeException : Exception
{
    public CanonicalApplyRuntimeError ErrorKind { get; }

    public CanonicalApplyRuntimeException(CanonicalApplyRuntimeError kind, string message) : base(message)
    {
        ErrorKind = kind;
    }

    public static CanonicalApplyRuntimeException MissingSourceObject(string detail)
        => new(CanonicalApplyRuntimeError.MissingSourceObject, detail);

    public static CanonicalApplyRuntimeException MissingSourceArtifact(string detail)
        => new(CanonicalApplyRuntimeError.MissingSourceArtifact, detail);

    public static CanonicalApplyRuntimeException MissingLogicalPathToken(string detail)
        => new(CanonicalApplyRuntimeError.MissingLogicalPathToken, detail);

    public static CanonicalApplyRuntimeException HashOrSizeMismatch(string detail)
        => new(CanonicalApplyRuntimeError.HashOrSizeMismatch, detail);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyRuntimeMode
{
    disabled,
    diagnosticsOnly,
    noCommit,
    testRootApply,
    productionRootApplyWithLegacyFallback,
    blocked
}

public static class CanonicalApplyRuntimeModeExtensions
{
    public static bool ExecutesCommit(this CanonicalApplyRuntimeMode mode)
        => mode == CanonicalApplyRuntimeMode.testRootApply
           || mode == CanonicalApplyRuntimeMode.productionRootApplyWithLegacyFallback;

    public static CanonicalSyncRuntimeMode SyncDiagnosticMode(this CanonicalApplyRuntimeMode mode)
        => mode switch
        {
            CanonicalApplyRuntimeMode.disabled => CanonicalSyncRuntimeMode.disabled,
            CanonicalApplyRuntimeMode.diagnosticsOnly => CanonicalSyncRuntimeMode.diagnosticsOnly,
            CanonicalApplyRuntimeMode.noCommit => CanonicalSyncRuntimeMode.canonicalPlanNoCommit,
            CanonicalApplyRuntimeMode.testRootApply => CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback,
            CanonicalApplyRuntimeMode.productionRootApplyWithLegacyFallback => CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback,
            CanonicalApplyRuntimeMode.blocked => CanonicalSyncRuntimeMode.blocked,
            _ => CanonicalSyncRuntimeMode.disabled
        };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyRuntimeDomain
{
    recordingMetadata,
    libraryMetadata,
    generatedArtifacts,
    tombstoneConflict,
    recordingExistence,
    audioUpload
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyRuntimeUnsupportedDomain
{
    audioUpload,
    readSideCutover,
    resourceMove,
    standaloneNoteContent,
    permanentDelete,
    tombstoneGarbageCollection
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyRuntimeBlocker
{
    disabledMode,
    diagnosticsOnly,
    noCommit,
    blockedMode,
    canonicalPlanAuthorityUnavailable,
    inventorySnapshotInvalid,
    applyPlanInvalid,
    enabledDomainsMissing,
    domainNotEnabled,
    missingExecutor,
    dryRunOnlyExecutor,
    rootBoundApplyPortUnavailable,
    rollbackUnavailable,
    postconditionUnavailable,
    legacyFallbackUnavailable,
    unresolvedConflict,
    audioActionBlocked,
    resourceMoveBlocked,
    standaloneNoteContentWriteBlocked,
    permanentDeleteBlocked,
    tombstoneGarbageCollectionBlocked,
    diagnosticsNotRedacted,
    releaseDefaultProductionApplyBlocked,
    debugInternalApprovalMissing,
    runtimeSwitchEnabled,
    readPathNotLegacy,
    unsupportedDomain,
    rollbackFailureFatal,
    actionFailed
}

// CanonicalApplyRuntimeGateBlocker is a typealias for CanonicalApplyRuntimeBlocker
using CanonicalApplyRuntimeGateBlocker = CanonicalApplyRuntimeBlocker;

public sealed class CanonicalApplyRuntimePolicy : IEquatable<CanonicalApplyRuntimePolicy>
{
    public bool DebugInternalBuild { get; }
    public bool OwnerApproved { get; }
    public bool ReleaseDefaultBuild { get; }
    public bool LegacyFallbackAvailable { get; }
    public bool DiagnosticsRedacted { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool ReadPathLegacy { get; }
    public CanonicalApplyRuntimeDomain[] EnabledDomains { get; }
    public bool AllowConflictRecordAction { get; }
    public bool AllowTestRootApply { get; }

    public CanonicalApplyRuntimePolicy(
        bool debugInternalBuild = false,
        bool ownerApproved = false,
        bool releaseDefaultBuild = true,
        bool legacyFallbackAvailable = true,
        bool diagnosticsRedacted = true,
        bool runtimeSwitchEnabled = false,
        bool readPathLegacy = true,
        CanonicalApplyRuntimeDomain[]? enabledDomains = null,
        bool allowConflictRecordAction = true,
        bool allowTestRootApply = true)
    {
        DebugInternalBuild = debugInternalBuild;
        OwnerApproved = ownerApproved;
        ReleaseDefaultBuild = releaseDefaultBuild;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        DiagnosticsRedacted = diagnosticsRedacted;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        ReadPathLegacy = readPathLegacy;
        EnabledDomains = (enabledDomains ?? Array.Empty<CanonicalApplyRuntimeDomain>())
            .Distinct()
            .OrderBy(d => d.ToString(), StringComparer.Ordinal)
            .ToArray();
        AllowConflictRecordAction = allowConflictRecordAction;
        AllowTestRootApply = allowTestRootApply;
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyRuntimePolicy other && Equals(other);
    public bool Equals(CanonicalApplyRuntimePolicy? other) =>
        other is not null &&
        DebugInternalBuild == other.DebugInternalBuild &&
        OwnerApproved == other.OwnerApproved &&
        ReleaseDefaultBuild == other.ReleaseDefaultBuild &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        DiagnosticsRedacted == other.DiagnosticsRedacted &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        ReadPathLegacy == other.ReadPathLegacy &&
        EnabledDomains.SequenceEqual(other.EnabledDomains) &&
        AllowConflictRecordAction == other.AllowConflictRecordAction &&
        AllowTestRootApply == other.AllowTestRootApply;
    public override int GetHashCode() => HashCode.Combine(DebugInternalBuild, OwnerApproved, ReleaseDefaultBuild);
    public static bool operator ==(CanonicalApplyRuntimePolicy left, CanonicalApplyRuntimePolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyRuntimePolicy left, CanonicalApplyRuntimePolicy right) => !left.Equals(right);
}

public sealed class CanonicalApplyRuntimeConfiguration : IEquatable<CanonicalApplyRuntimeConfiguration>
{
    public CanonicalApplyRuntimeMode Mode { get; }
    public CanonicalApplyRuntimePolicy Policy { get; }

    public CanonicalApplyRuntimeConfiguration(
        CanonicalApplyRuntimeMode mode = CanonicalApplyRuntimeMode.disabled,
        CanonicalApplyRuntimePolicy? policy = null)
    {
        Mode = mode;
        Policy = policy ?? new CanonicalApplyRuntimePolicy();
    }

    public static readonly CanonicalApplyRuntimeConfiguration Disabled = new();

    public override bool Equals(object? obj) => obj is CanonicalApplyRuntimeConfiguration other && Equals(other);
    public bool Equals(CanonicalApplyRuntimeConfiguration? other) =>
        other is not null && Mode == other.Mode && Policy.Equals(other.Policy);
    public override int GetHashCode() => HashCode.Combine(Mode, Policy);
    public static bool operator ==(CanonicalApplyRuntimeConfiguration left, CanonicalApplyRuntimeConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyRuntimeConfiguration left, CanonicalApplyRuntimeConfiguration right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyRuntimeGateState
{
    legacyOwner,
    diagnosticsOnly,
    noCommit,
    allowed,
    blocked
}

public sealed class CanonicalApplyRuntimeGateResult : IEquatable<CanonicalApplyRuntimeGateResult>
{
    public CanonicalApplyRuntimeGateState State { get; }
    public CanonicalApplyRuntimeGateBlocker[] Blockers { get; }
    public CanonicalApplyRuntimeMode Mode { get; }

    public bool IsAllowed => Blockers.Length == 0 && State == CanonicalApplyRuntimeGateState.allowed;
    public bool ExecutesCommit => IsAllowed && Mode.ExecutesCommit();
    public bool UsesLegacyFallback => !ExecutesCommit;

    public CanonicalApplyRuntimeGateResult(
        CanonicalApplyRuntimeGateState state,
        CanonicalApplyRuntimeGateBlocker[] blockers,
        CanonicalApplyRuntimeMode mode)
    {
        State = state;
        Blockers = (blockers ?? Array.Empty<CanonicalApplyRuntimeGateBlocker>())
            .Distinct()
            .OrderBy(b => b.ToString(), StringComparer.Ordinal)
            .ToArray();
        Mode = mode;
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyRuntimeGateResult other && Equals(other);
    public bool Equals(CanonicalApplyRuntimeGateResult? other) =>
        other is not null && State == other.State && Mode == other.Mode;
    public override int GetHashCode() => HashCode.Combine(State, Mode);
    public static bool operator ==(CanonicalApplyRuntimeGateResult left, CanonicalApplyRuntimeGateResult right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyRuntimeGateResult left, CanonicalApplyRuntimeGateResult right) => !left.Equals(right);
}

public sealed class CanonicalApplyRuntimeOwnerContext
{
    public CanonicalApplyRuntimeConfiguration Configuration { get; }
    public CanonicalApplyPlan ApplyPlan { get; }
    public CanonicalLibrarySyncPlan? LibraryPlan { get; }
    public CanonicalManifest? LocalManifest { get; }
    public CanonicalManifest? PeerManifest { get; }
    public bool InventorySnapshotValid { get; }
    public bool CanonicalPlanAuthorityAllowed { get; }
    public bool LegacyFallbackAvailable { get; }
    public CanonicalApplyRuntimeExecutorRegistry Registry { get; }
    public string? SyncRunID { get; }

    public CanonicalApplyRuntimeOwnerContext(
        CanonicalApplyRuntimeConfiguration configuration,
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan? libraryPlan = null,
        CanonicalManifest? localManifest = null,
        CanonicalManifest? peerManifest = null,
        bool inventorySnapshotValid = false,
        bool canonicalPlanAuthorityAllowed = false,
        bool legacyFallbackAvailable = true,
        CanonicalApplyRuntimeExecutorRegistry? registry = null,
        string? syncRunID = null)
    {
        Configuration = configuration;
        ApplyPlan = applyPlan;
        LibraryPlan = libraryPlan;
        LocalManifest = localManifest;
        PeerManifest = peerManifest;
        InventorySnapshotValid = inventorySnapshotValid;
        CanonicalPlanAuthorityAllowed = canonicalPlanAuthorityAllowed;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        Registry = registry ?? new CanonicalApplyRuntimeExecutorRegistry();
        SyncRunID = syncRunID;
    }

    public CanonicalApplyAction[] AllActions
    {
        get
        {
            var seen = new HashSet<string>();
            var combined = new List<CanonicalApplyAction>();
            foreach (var action in ApplyPlan.Actions)
                if (seen.Add(action.ActionID))
                    combined.Add(action);
            if (LibraryPlan != null)
                foreach (var action in LibraryPlan.ApplyActions)
                    if (seen.Add(action.ActionID))
                        combined.Add(action);
            return combined.ToArray();
        }
    }
}

public sealed class CanonicalApplyRuntimeExecutorContext
{
    public CanonicalApplyAction Action { get; }
    public CanonicalApplyPlan ApplyPlan { get; }
    public CanonicalLibrarySyncPlan? LibraryPlan { get; }
    public CanonicalManifest LocalManifest { get; }
    public CanonicalManifest PeerManifest { get; }
    public string? SyncRunID { get; }

    public CanonicalApplyRuntimeExecutorContext(
        CanonicalApplyAction action,
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan? libraryPlan,
        CanonicalManifest localManifest,
        CanonicalManifest peerManifest,
        string? syncRunID = null)
    {
        Action = action;
        ApplyPlan = applyPlan;
        LibraryPlan = libraryPlan;
        LocalManifest = localManifest;
        PeerManifest = peerManifest;
        SyncRunID = syncRunID;
    }
}

public sealed class CanonicalApplyRuntimeExecutorResult : IEquatable<CanonicalApplyRuntimeExecutorResult>
{
    public string ActionID { get; }
    public string ObjectID { get; }
    public CanonicalApplyRuntimeDomain Domain { get; }
    public bool Committed { get; }
    public bool PreconditionVerified { get; }
    public bool PostconditionVerified { get; }
    public bool RollbackAttempted { get; }
    public bool? RollbackSucceeded { get; }
    public bool RollbackFatal { get; }
    public string? FailureReason { get; }
    public string? Detail { get; }

    public CanonicalApplyRuntimeExecutorResult(
        string actionID,
        string objectID,
        CanonicalApplyRuntimeDomain domain,
        bool committed,
        bool preconditionVerified = true,
        bool postconditionVerified = true,
        bool rollbackAttempted = false,
        bool? rollbackSucceeded = null,
        bool rollbackFatal = false,
        string? failureReason = null,
        string? detail = null)
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, domain.ToString());
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "canonical-object");
        Domain = domain;
        Committed = committed;
        PreconditionVerified = preconditionVerified;
        PostconditionVerified = postconditionVerified;
        RollbackAttempted = rollbackAttempted;
        RollbackSucceeded = rollbackSucceeded;
        RollbackFatal = rollbackFatal;
        FailureReason = CanonicalProductionRedaction.SafeDiagnosticText(failureReason);
        Detail = CanonicalProductionRedaction.SafeDiagnosticText(detail);
    }

    public bool Succeeded => Committed && PreconditionVerified && PostconditionVerified && RollbackFatal == false;

    public static CanonicalApplyRuntimeExecutorResult Success(
        CanonicalApplyAction action,
        CanonicalApplyRuntimeDomain domain,
        string? detail = null)
        => new(
            actionID: action.ActionID,
            objectID: action.Target.ObjectID,
            domain: domain,
            committed: true,
            detail: detail);

    public static CanonicalApplyRuntimeExecutorResult Failure(
        CanonicalApplyAction action,
        CanonicalApplyRuntimeDomain domain,
        bool preconditionVerified = true,
        bool postconditionVerified = true,
        bool rollbackAttempted = false,
        bool? rollbackSucceeded = null,
        bool rollbackFatal = false,
        string reason = "")
        => new(
            actionID: action.ActionID,
            objectID: action.Target.ObjectID,
            domain: domain,
            committed: false,
            preconditionVerified: preconditionVerified,
            postconditionVerified: postconditionVerified,
            rollbackAttempted: rollbackAttempted,
            rollbackSucceeded: rollbackSucceeded,
            rollbackFatal: rollbackFatal,
            failureReason: reason);

    public override bool Equals(object? obj) => obj is CanonicalApplyRuntimeExecutorResult other && Equals(other);
    public bool Equals(CanonicalApplyRuntimeExecutorResult? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
    public static bool operator ==(CanonicalApplyRuntimeExecutorResult left, CanonicalApplyRuntimeExecutorResult right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyRuntimeExecutorResult left, CanonicalApplyRuntimeExecutorResult right) => !left.Equals(right);
}

public sealed class CanonicalApplyRuntimeExecutorEntry
{
    public CanonicalApplyRuntimeDomain Domain { get; }
    public bool DryRunOnly { get; }
    public bool RollbackAvailable { get; }
    public bool PostconditionAvailable { get; }
    public bool RootBoundApplyPortAvailable { get; }
    private readonly Func<CanonicalApplyRuntimeExecutorContext, Task<CanonicalApplyRuntimeExecutorResult>> _executeClosure;

    public CanonicalApplyRuntimeExecutorEntry(
        CanonicalApplyRuntimeDomain domain,
        bool dryRunOnly = false,
        bool rollbackAvailable = true,
        bool postconditionAvailable = true,
        bool rootBoundApplyPortAvailable = true,
        Func<CanonicalApplyRuntimeExecutorContext, Task<CanonicalApplyRuntimeExecutorResult>>? execute = null)
    {
        Domain = domain;
        DryRunOnly = dryRunOnly;
        RollbackAvailable = rollbackAvailable;
        PostconditionAvailable = postconditionAvailable;
        RootBoundApplyPortAvailable = rootBoundApplyPortAvailable;
        _executeClosure = execute ?? (_ => Task.FromResult(CanonicalApplyRuntimeExecutorResult.Failure(
            new CanonicalApplyAction(CanonicalApplyActionKind.deferredUnsupported,
                CanonicalApplySource.planner,
                new CanonicalApplyTarget("unknown")),
            CanonicalApplyRuntimeDomain.audioUpload,
            reason: CanonicalApplyRuntimeBlocker.audioActionBlocked.ToString())));
    }

    public Task<CanonicalApplyRuntimeExecutorResult> Execute(CanonicalApplyRuntimeExecutorContext context)
        => _executeClosure(context);

    public static CanonicalApplyRuntimeExecutorEntry UnsupportedAudioUpload()
        => new(
            domain: CanonicalApplyRuntimeDomain.audioUpload,
            dryRunOnly: true,
            rollbackAvailable: false,
            postconditionAvailable: false,
            rootBoundApplyPortAvailable: false,
            execute: context =>
                Task.FromResult(CanonicalApplyRuntimeExecutorResult.Failure(
                    context.Action,
                    CanonicalApplyRuntimeDomain.audioUpload,
                    reason: CanonicalApplyRuntimeBlocker.audioActionBlocked.ToString())));
}

public sealed class CanonicalApplyRuntimeExecutorRegistry
{
    private readonly Dictionary<CanonicalApplyRuntimeDomain, CanonicalApplyRuntimeExecutorEntry> _entries;

    public CanonicalApplyRuntimeExecutorRegistry(CanonicalApplyRuntimeExecutorEntry[]? entries = null)
    {
        var mapped = new Dictionary<CanonicalApplyRuntimeDomain, CanonicalApplyRuntimeExecutorEntry>();
        if (entries != null)
            foreach (var entry in entries)
                mapped[entry.Domain] = entry;
        if (!mapped.ContainsKey(CanonicalApplyRuntimeDomain.audioUpload))
            mapped[CanonicalApplyRuntimeDomain.audioUpload] = CanonicalApplyRuntimeExecutorEntry.UnsupportedAudioUpload();
        _entries = mapped;
    }

    public CanonicalApplyRuntimeExecutorEntry? EntryFor(CanonicalApplyRuntimeDomain domain)
        => _entries.TryGetValue(domain, out var entry) ? entry : null;

    public bool Contains(CanonicalApplyRuntimeDomain domain)
        => _entries.ContainsKey(domain);

    public CanonicalApplyRuntimeExecutorRegistry Adding(CanonicalApplyRuntimeExecutorEntry entry)
    {
        var next = new Dictionary<CanonicalApplyRuntimeDomain, CanonicalApplyRuntimeExecutorEntry>(_entries);
        next[entry.Domain] = entry;
        return new CanonicalApplyRuntimeExecutorRegistry { _entriesInner = next };
    }

    private CanonicalApplyRuntimeExecutorRegistry() { _entries = new Dictionary<CanonicalApplyRuntimeDomain, CanonicalApplyRuntimeExecutorEntry>(); }

    private Dictionary<CanonicalApplyRuntimeDomain, CanonicalApplyRuntimeExecutorEntry> _entriesInner
    {
        get => _entries;
        init => _entries = value;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyRuntimeActionStatus
{
    notExecuted,
    completed,
    failed,
    blocked
}

public sealed class CanonicalApplyRuntimeActionRecord : IEquatable<CanonicalApplyRuntimeActionRecord>
{
    public string Id => ActionID;
    public string ActionID { get; }
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }
    public CanonicalApplyActionKind ActionKind { get; }
    public CanonicalApplyRuntimeDomain Domain { get; }
    public CanonicalApplyRuntimeActionStatus Status { get; }
    public bool DuplicateLegacySuppressionAllowed { get; }
    public CanonicalApplyRuntimeBlocker? Blocker { get; }
    public string? Detail { get; }

    public CanonicalApplyRuntimeActionRecord(
        CanonicalApplyAction action,
        CanonicalApplyRuntimeDomain domain,
        CanonicalApplyRuntimeActionStatus status,
        bool duplicateLegacySuppressionAllowed = false,
        CanonicalApplyRuntimeBlocker? blocker = null,
        string? detail = null)
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(action.ActionID, action.Kind.ToString());
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(action.Target.ObjectID, "canonical-object");
        ArtifactID = action.Target.ArtifactID != null
            ? CanonicalProductionRedaction.SafeIdentifier(action.Target.ArtifactID, "artifact")
            : null;
        ArtifactKind = action.Target.ArtifactKind;
        ActionKind = action.Kind;
        Domain = domain;
        Status = status;
        DuplicateLegacySuppressionAllowed = duplicateLegacySuppressionAllowed;
        Blocker = blocker;
        Detail = CanonicalProductionRedaction.SafeDiagnosticText(detail);
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyRuntimeActionRecord other && Equals(other);
    public bool Equals(CanonicalApplyRuntimeActionRecord? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
    public static bool operator ==(CanonicalApplyRuntimeActionRecord left, CanonicalApplyRuntimeActionRecord right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyRuntimeActionRecord left, CanonicalApplyRuntimeActionRecord right) => !left.Equals(right);
}

public sealed class CanonicalApplyRuntimeReport : IEquatable<CanonicalApplyRuntimeReport>
{
    public CanonicalApplyRuntimeMode Mode { get; }
    public CanonicalApplyRuntimeGateResult GateResult { get; }
    public CanonicalApplyRuntimeActionRecord[] ActionRecords { get; }
    public bool LegacyFallbackUsed { get; }
    public string[] DuplicateLegacySuppressedActionIDs { get; }
    public bool FatalBlocker { get; }
    public CanonicalSyncRuntimeDiagnostic[] Diagnostics { get; }

    public CanonicalApplyRuntimeReport(
        CanonicalApplyRuntimeMode mode,
        CanonicalApplyRuntimeGateResult gateResult,
        CanonicalApplyRuntimeActionRecord[] actionRecords,
        bool legacyFallbackUsed,
        string[] duplicateLegacySuppressedActionIDs,
        bool fatalBlocker,
        CanonicalSyncRuntimeDiagnostic[] diagnostics)
    {
        Mode = mode;
        GateResult = gateResult;
        ActionRecords = actionRecords ?? Array.Empty<CanonicalApplyRuntimeActionRecord>();
        LegacyFallbackUsed = legacyFallbackUsed;
        DuplicateLegacySuppressedActionIDs = duplicateLegacySuppressedActionIDs ?? Array.Empty<string>();
        FatalBlocker = fatalBlocker;
        Diagnostics = diagnostics ?? Array.Empty<CanonicalSyncRuntimeDiagnostic>();
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyRuntimeReport other && Equals(other);
    public bool Equals(CanonicalApplyRuntimeReport? other) =>
        other is not null &&
        Mode == other.Mode &&
        GateResult.Equals(other.GateResult) &&
        LegacyFallbackUsed == other.LegacyFallbackUsed &&
        FatalBlocker == other.FatalBlocker;
    public override int GetHashCode() => HashCode.Combine(Mode, LegacyFallbackUsed, FatalBlocker);
    public static bool operator ==(CanonicalApplyRuntimeReport left, CanonicalApplyRuntimeReport right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyRuntimeReport left, CanonicalApplyRuntimeReport right) => !left.Equals(right);
}

public sealed class CanonicalApplyRuntimeResult : IEquatable<CanonicalApplyRuntimeResult>
{
    public CanonicalApplyRuntimeMode Mode { get; }
    public CanonicalApplyRuntimeGateResult GateResult { get; }
    public CanonicalApplyRuntimeReport Report { get; }

    public string[] ExecutedActionIDs =>
        Report.ActionRecords.Where(r => r.Status == CanonicalApplyRuntimeActionStatus.completed)
            .Select(r => r.ActionID).ToArray();

    public string[] DuplicateLegacySuppressedActionIDs => Report.DuplicateLegacySuppressedActionIDs;
    public bool LegacyFallbackUsed => Report.LegacyFallbackUsed;

    public CanonicalApplyRuntimeResult(
        CanonicalApplyRuntimeMode mode,
        CanonicalApplyRuntimeGateResult gateResult,
        CanonicalApplyRuntimeReport report)
    {
        Mode = mode;
        GateResult = gateResult;
        Report = report;
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyRuntimeResult other && Equals(other);
    public bool Equals(CanonicalApplyRuntimeResult? other) =>
        other is not null && Mode == other.Mode && GateResult.Equals(other.GateResult);
    public override int GetHashCode() => HashCode.Combine(Mode, GateResult);
    public static bool operator ==(CanonicalApplyRuntimeResult left, CanonicalApplyRuntimeResult right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyRuntimeResult left, CanonicalApplyRuntimeResult right) => !left.Equals(right);
}

public sealed class CanonicalApplyRuntimeGate
{
    public CanonicalApplyRuntimeGateResult Evaluate(CanonicalApplyRuntimeOwnerContext context)
    {
        var configuration = context.Configuration;
        switch (configuration.Mode)
        {
            case CanonicalApplyRuntimeMode.disabled:
                return new CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.legacyOwner,
                    new[] { CanonicalApplyRuntimeGateBlocker.disabledMode }, configuration.Mode);
            case CanonicalApplyRuntimeMode.diagnosticsOnly:
                return new CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.diagnosticsOnly,
                    new[] { CanonicalApplyRuntimeGateBlocker.diagnosticsOnly }, configuration.Mode);
            case CanonicalApplyRuntimeMode.noCommit:
                return new CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.noCommit,
                    new[] { CanonicalApplyRuntimeGateBlocker.noCommit }, configuration.Mode);
            case CanonicalApplyRuntimeMode.blocked:
                return new CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.blocked,
                    new[] { CanonicalApplyRuntimeGateBlocker.blockedMode }, configuration.Mode);
            case CanonicalApplyRuntimeMode.testRootApply:
            case CanonicalApplyRuntimeMode.productionRootApplyWithLegacyFallback:
                break;
            default:
                return new CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.blocked,
                    new[] { CanonicalApplyRuntimeGateBlocker.blockedMode }, configuration.Mode);
        }

        var blockers = new List<CanonicalApplyRuntimeGateBlocker>();
        if (context.CanonicalPlanAuthorityAllowed == false)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.canonicalPlanAuthorityUnavailable);
        if (context.InventorySnapshotValid == false)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.inventorySnapshotInvalid);
        if (context.LocalManifest == null || context.PeerManifest == null)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.inventorySnapshotInvalid);
        if (context.ApplyPlan.SchemaVersion != CanonicalApplyPlan.CurrentSchemaVersion)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.applyPlanInvalid);
        if (configuration.Policy.EnabledDomains.Length == 0)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.enabledDomainsMissing);
        if (context.LegacyFallbackAvailable == false || configuration.Policy.LegacyFallbackAvailable == false)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.legacyFallbackUnavailable);
        if (configuration.Policy.DiagnosticsRedacted == false)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.diagnosticsNotRedacted);
        if (configuration.Policy.RuntimeSwitchEnabled)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.runtimeSwitchEnabled);
        if (configuration.Policy.ReadPathLegacy == false)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.readPathNotLegacy);
        if (configuration.Mode == CanonicalApplyRuntimeMode.productionRootApplyWithLegacyFallback)
        {
            if (configuration.Policy.ReleaseDefaultBuild)
                blockers.Add(CanonicalApplyRuntimeGateBlocker.releaseDefaultProductionApplyBlocked);
            if (configuration.Policy.DebugInternalBuild == false || configuration.Policy.OwnerApproved == false)
                blockers.Add(CanonicalApplyRuntimeGateBlocker.debugInternalApprovalMissing);
        }
        if (configuration.Mode == CanonicalApplyRuntimeMode.testRootApply
            && configuration.Policy.AllowTestRootApply == false)
            blockers.Add(CanonicalApplyRuntimeGateBlocker.debugInternalApprovalMissing);

        var enabledDomains = new HashSet<CanonicalApplyRuntimeDomain>(configuration.Policy.EnabledDomains);
        var actions = context.AllActions;
        foreach (var action in actions)
        {
            var domain = DomainFor(action);
            if (domain == CanonicalApplyRuntimeDomain.audioUpload)
            {
                blockers.Add(CanonicalApplyRuntimeGateBlocker.audioActionBlocked);
                continue;
            }
            if (enabledDomains.Contains(domain) == false)
                blockers.Add(CanonicalApplyRuntimeGateBlocker.domainNotEnabled);

            var entry = context.Registry.EntryFor(domain);
            if (entry == null)
            {
                blockers.Add(CanonicalApplyRuntimeGateBlocker.missingExecutor);
                continue;
            }
            if (entry.DryRunOnly && configuration.Mode.ExecutesCommit())
                blockers.Add(CanonicalApplyRuntimeGateBlocker.dryRunOnlyExecutor);
            if (entry.RootBoundApplyPortAvailable == false && configuration.Mode.ExecutesCommit())
                blockers.Add(CanonicalApplyRuntimeGateBlocker.rootBoundApplyPortUnavailable);
            if (entry.RollbackAvailable == false)
                blockers.Add(CanonicalApplyRuntimeGateBlocker.rollbackUnavailable);
            if (entry.PostconditionAvailable == false)
                blockers.Add(CanonicalApplyRuntimeGateBlocker.postconditionUnavailable);
            if (action.Kind == CanonicalApplyActionKind.deferredUnsupported)
                blockers.Add(CanonicalApplyRuntimeGateBlocker.unsupportedDomain);
        }
        if (HasAudioConflict(context.ApplyPlan))
            blockers.Add(CanonicalApplyRuntimeGateBlocker.audioActionBlocked);
        if (HasUnresolvedConflictRequiringRecord(context)
            && (enabledDomains.Contains(CanonicalApplyRuntimeDomain.tombstoneConflict) == false
                || configuration.Policy.AllowConflictRecordAction == false))
            blockers.Add(CanonicalApplyRuntimeGateBlocker.unresolvedConflict);

        return new CanonicalApplyRuntimeGateResult(
            state: blockers.Count == 0 ? CanonicalApplyRuntimeGateState.allowed : CanonicalApplyRuntimeGateState.blocked,
            blockers: blockers.ToArray(),
            mode: configuration.Mode);
    }

    public static CanonicalApplyRuntimeDomain DomainFor(CanonicalApplyAction action)
    {
        if (action.Target.ArtifactKind == CanonicalArtifact.Kind.audio)
            return CanonicalApplyRuntimeDomain.audioUpload;
        if (action.Reason == CanonicalApplyRuntimeOwner.RecordingExistenceBridgeReason)
            return CanonicalApplyRuntimeDomain.recordingExistence;
        return action.Kind switch
        {
            CanonicalApplyActionKind.recordingMetadataApply => CanonicalApplyRuntimeDomain.recordingMetadata,
            CanonicalApplyActionKind.recordingMetadataSend => CanonicalApplyRuntimeDomain.recordingMetadata,
            CanonicalApplyActionKind.folderMetadataApply => CanonicalApplyRuntimeDomain.libraryMetadata,
            CanonicalApplyActionKind.folderMetadataSend => CanonicalApplyRuntimeDomain.libraryMetadata,
            CanonicalApplyActionKind.studyItemMetadataApply => CanonicalApplyRuntimeDomain.libraryMetadata,
            CanonicalApplyActionKind.studyItemMetadataSend => CanonicalApplyRuntimeDomain.libraryMetadata,
            CanonicalApplyActionKind.generatedArtifactDownloadApply => CanonicalApplyRuntimeDomain.generatedArtifacts,
            CanonicalApplyActionKind.generatedArtifactNoOp => CanonicalApplyRuntimeDomain.generatedArtifacts,
            CanonicalApplyActionKind.libraryTombstoneApply => CanonicalApplyRuntimeDomain.tombstoneConflict,
            CanonicalApplyActionKind.libraryTombstoneSend => CanonicalApplyRuntimeDomain.tombstoneConflict,
            CanonicalApplyActionKind.objectTombstoneApply => CanonicalApplyRuntimeDomain.tombstoneConflict,
            CanonicalApplyActionKind.objectTombstoneSend => CanonicalApplyRuntimeDomain.tombstoneConflict,
            CanonicalApplyActionKind.artifactTombstoneApply => CanonicalApplyRuntimeDomain.tombstoneConflict,
            CanonicalApplyActionKind.conflictRecord => CanonicalApplyRuntimeDomain.tombstoneConflict,
            CanonicalApplyActionKind.deferredUnsupported =>
                action.FailureReason == CanonicalApplyFailureReason.tombstoneBlocksResurrection
                    ? CanonicalApplyRuntimeDomain.tombstoneConflict
                    : CanonicalApplyRuntimeDomain.audioUpload,
            _ => CanonicalApplyRuntimeDomain.recordingMetadata
        };
    }

    private static bool HasAudioConflict(CanonicalApplyPlan plan)
    {
        return plan.Conflicts.Any(c =>
            c.Kind == CanonicalConflictKind.recordingAudioContentMismatch
            || c.Target.ArtifactKind == CanonicalArtifact.Kind.audio);
    }

    private static bool HasUnresolvedConflictRequiringRecord(CanonicalApplyRuntimeOwnerContext context)
    {
        var unresolvedApply = context.ApplyPlan.Conflicts.Any(c =>
            c.ResolutionState == CanonicalConflictResolutionState.unresolved
            && c.Kind != CanonicalConflictKind.recordingAudioContentMismatch);
        var unresolvedLibrary = context.LibraryPlan?.Conflicts.Count > 0;
        if (!unresolvedApply && unresolvedLibrary != true)
            return false;
        return context.AllActions.All(a =>
            !(a.Kind == CanonicalApplyActionKind.conflictRecord
              && DomainFor(a) == CanonicalApplyRuntimeDomain.tombstoneConflict));
    }
}

public sealed class CanonicalApplyRuntimeOwner
{
    public const string RecordingExistenceBridgeReason = "recordingExistenceMetadataOnlyBridge";

    public async Task<CanonicalApplyRuntimeResult> Execute(CanonicalApplyRuntimeOwnerContext context)
    {
        var gateResult = new CanonicalApplyRuntimeGate().Evaluate(context);
        var diagnostics = BaseDiagnostics(context, gateResult);
        var records = new List<CanonicalApplyRuntimeActionRecord>();
        var duplicateSuppressionActionIDs = new List<string>();
        var fatalBlocker = false;

        if (!gateResult.ExecutesCommit
            || context.LocalManifest == null
            || context.PeerManifest == null)
        {
            diagnostics.Add(
                Diagnostic(
                    CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeLegacyFallbackUsed,
                    context,
                    detail: NonEmpty(string.Join("+", gateResult.Blockers.Select(b => b.ToString()))) ?? "legacyOwner"));
            diagnostics.Add(Diagnostic(CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeReportBuilt,
                context, count: records.Count, detail: "legacyFallback"));
            return Result(
                context: context,
                gateResult: gateResult,
                records: records.ToArray(),
                legacyFallbackUsed: true,
                duplicateSuppressionActionIDs: Array.Empty<string>(),
                fatalBlocker: false,
                diagnostics: diagnostics);
        }

        var localManifest = context.LocalManifest;
        var peerManifest = context.PeerManifest;

        foreach (var action in context.AllActions)
        {
            var domain = CanonicalApplyRuntimeGate.DomainFor(action);
            if (domain == CanonicalApplyRuntimeDomain.audioUpload)
            {
                diagnostics.Add(Diagnostic(
                    CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeAudioActionBlocked,
                    context, action: action, detail: "audioUploadUnsupported"));
                records.Add(new CanonicalApplyRuntimeActionRecord(
                    action, domain, CanonicalApplyRuntimeActionStatus.blocked,
                    blocker: CanonicalApplyRuntimeBlocker.audioActionBlocked));
                break;
            }

            var entry = context.Registry.EntryFor(domain);
            if (entry == null)
            {
                diagnostics.Add(Diagnostic(
                    CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeActionFailed,
                    context, action: action, detail: "missingExecutor"));
                records.Add(new CanonicalApplyRuntimeActionRecord(
                    action, domain, CanonicalApplyRuntimeActionStatus.blocked,
                    blocker: CanonicalApplyRuntimeBlocker.missingExecutor));
                break;
            }

            diagnostics.Add(Diagnostic(
                CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeActionStarted,
                context, action: action, detail: domain.ToString()));

            var executorResult = await entry.Execute(
                new CanonicalApplyRuntimeExecutorContext(
                    action: action,
                    applyPlan: context.ApplyPlan,
                    libraryPlan: context.LibraryPlan,
                    localManifest: localManifest,
                    peerManifest: peerManifest,
                    syncRunID: context.SyncRunID));

            if (executorResult.RollbackAttempted)
            {
                diagnostics.Add(Diagnostic(
                    CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeRollbackStarted,
                    context, action: action, detail: domain.ToString()));
                diagnostics.Add(Diagnostic(
                    executorResult.RollbackSucceeded == true
                        ? CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeRollbackCompleted
                        : CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeRollbackFailed,
                    context, action: action,
                    detail: executorResult.FailureReason ?? executorResult.Detail));
            }

            if (executorResult.Succeeded)
            {
                diagnostics.Add(Diagnostic(
                    CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeActionCompleted,
                    context, action: action, detail: executorResult.Detail ?? "committed"));
                diagnostics.Add(Diagnostic(
                    CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeDuplicateLegacySuppressed,
                    context, action: action, detail: "eligible"));
                records.Add(new CanonicalApplyRuntimeActionRecord(
                    action: action,
                    domain: domain,
                    status: CanonicalApplyRuntimeActionStatus.completed,
                    duplicateLegacySuppressionAllowed: true,
                    detail: executorResult.Detail));
                duplicateSuppressionActionIDs.Add(action.ActionID);
                continue;
            }

            fatalBlocker = executorResult.RollbackFatal;
            diagnostics.Add(Diagnostic(
                CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeActionFailed,
                context, action: action,
                detail: executorResult.FailureReason ?? executorResult.Detail ?? "failed"));
            records.Add(new CanonicalApplyRuntimeActionRecord(
                action: action,
                domain: domain,
                status: CanonicalApplyRuntimeActionStatus.failed,
                blocker: executorResult.RollbackFatal
                    ? CanonicalApplyRuntimeBlocker.rollbackFailureFatal
                    : CanonicalApplyRuntimeBlocker.actionFailed,
                detail: executorResult.FailureReason));
            break;
        }

        var completedCount = records.Count(r => r.Status == CanonicalApplyRuntimeActionStatus.completed);
        var legacyFallbackUsed = completedCount < context.AllActions.Length || fatalBlocker;
        if (legacyFallbackUsed)
        {
            diagnostics.Add(Diagnostic(
                CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeLegacyFallbackUsed,
                context, count: context.AllActions.Length - completedCount,
                detail: fatalBlocker ? "fatalBlocker" : "unexecutedActions"));
        }
        diagnostics.Add(Diagnostic(
            CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeReportBuilt,
            context, count: records.Count, detail: "canonicalRuntime"));

        return Result(
            context: context,
            gateResult: gateResult,
            records: records.ToArray(),
            legacyFallbackUsed: legacyFallbackUsed,
            duplicateSuppressionActionIDs: duplicateSuppressionActionIDs
                .Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            fatalBlocker: fatalBlocker,
            diagnostics: diagnostics);
    }

    private static List<CanonicalSyncRuntimeDiagnostic> BaseDiagnostics(
        CanonicalApplyRuntimeOwnerContext context,
        CanonicalApplyRuntimeGateResult gateResult)
    {
        return new List<CanonicalSyncRuntimeDiagnostic>
        {
            Diagnostic(CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeModeEvaluated,
                context, count: context.AllActions.Length, detail: gateResult.State.ToString()),
            Diagnostic(
                gateResult.IsAllowed
                    ? CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeGateAllowed
                    : CanonicalSyncRuntimeDiagnosticKind.canonicalApplyRuntimeGateBlocked,
                context,
                count: gateResult.Blockers.Length,
                detail: NonEmpty(string.Join("+", gateResult.Blockers.Select(b => b.ToString()))) ?? "none")
        };
    }

    private static CanonicalSyncRuntimeDiagnostic Diagnostic(
        CanonicalSyncRuntimeDiagnosticKind kind,
        CanonicalApplyRuntimeOwnerContext context,
        CanonicalApplyAction? action = null,
        int? count = null,
        string? detail = null)
    {
        return new CanonicalSyncRuntimeDiagnostic(
            kind: kind,
            syncRunID: context.SyncRunID,
            mode: context.Configuration.Mode.SyncDiagnosticMode(),
            objectID: action?.Target.ObjectID,
            actionKind: action?.Kind.ToString(),
            count: count,
            detail: detail);
    }

    private static CanonicalApplyRuntimeResult Result(
        CanonicalApplyRuntimeOwnerContext context,
        CanonicalApplyRuntimeGateResult gateResult,
        CanonicalApplyRuntimeActionRecord[] records,
        bool legacyFallbackUsed,
        string[] duplicateSuppressionActionIDs,
        bool fatalBlocker,
        List<CanonicalSyncRuntimeDiagnostic> diagnostics)
    {
        var report = new CanonicalApplyRuntimeReport(
            mode: context.Configuration.Mode,
            gateResult: gateResult,
            actionRecords: records,
            legacyFallbackUsed: legacyFallbackUsed,
            duplicateLegacySuppressedActionIDs: duplicateSuppressionActionIDs,
            fatalBlocker: fatalBlocker,
            diagnostics: diagnostics.ToArray());
        return new CanonicalApplyRuntimeResult(
            mode: context.Configuration.Mode,
            gateResult: gateResult,
            report: report);
    }

    private static string? NonEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}

public static class CanonicalApplyRuntimeExecutorAdapters
{
    public static CanonicalApplyRuntimeExecutorEntry RecordingMetadata(
        ICanonicalRecordingMetadataCutoverExecutor executor,
        bool dryRunOnly = false,
        bool rootBoundApplyPortAvailable = true)
    {
        return new CanonicalApplyRuntimeExecutorEntry(
            domain: CanonicalApplyRuntimeDomain.recordingMetadata,
            dryRunOnly: dryRunOnly,
            rootBoundApplyPortAvailable: rootBoundApplyPortAvailable,
            execute: async context =>
            {
                var action = context.Action;
                var localObjects = context.LocalManifest.Objects
                    .ToDictionary(o => o.ObjectID, o => o);
                var peerObjects = context.PeerManifest.Objects
                    .ToDictionary(o => o.ObjectID, o => o);
                var candidate = new CanonicalRecordingMetadataCutoverCandidate(
                    action: action,
                    localObject: localObjects.TryGetValue(action.Target.ObjectID, out var lo) ? lo : null,
                    peerObject: peerObjects.TryGetValue(action.Target.ObjectID, out var po) ? po : null,
                    rollbackCheckpointID: $"apply-runtime-recording-{action.Target.ObjectID}");
                if (candidate.CutoverActionKind == null)
                    return CanonicalApplyRuntimeExecutorResult.Failure(
                        action, CanonicalApplyRuntimeDomain.recordingMetadata,
                        reason: CanonicalApplyRuntimeBlocker.unsupportedDomain.ToString());
                var commit = await executor.CommitRecordingMetadata(candidate);
                if (commit.Committed && commit.PreconditionVerified && commit.PostconditionVerified)
                    return CanonicalApplyRuntimeExecutorResult.Success(
                        action, CanonicalApplyRuntimeDomain.recordingMetadata, detail: commit.Reason);
                var rollback = await executor.RollbackRecordingMetadata(candidate, RecordingRollbackReason(commit));
                return CanonicalApplyRuntimeExecutorResult.Failure(
                    action: action,
                    domain: CanonicalApplyRuntimeDomain.recordingMetadata,
                    preconditionVerified: commit.PreconditionVerified,
                    postconditionVerified: commit.PostconditionVerified,
                    rollbackAttempted: true,
                    rollbackSucceeded: rollback.Succeeded,
                    rollbackFatal: rollback.Fatal || rollback.Succeeded == false,
                    reason: commit.FailureKind?.ToString() ?? commit.Reason);
            });
    }

    public static CanonicalApplyRuntimeExecutorEntry LibraryMetadata(
        ICanonicalLibraryMetadataCutoverExecutor executor,
        bool dryRunOnly = false,
        bool rootBoundApplyPortAvailable = true)
    {
        return new CanonicalApplyRuntimeExecutorEntry(
            domain: CanonicalApplyRuntimeDomain.libraryMetadata,
            dryRunOnly: dryRunOnly,
            rootBoundApplyPortAvailable: rootBoundApplyPortAvailable,
            execute: async context =>
            {
                if (context.LibraryPlan == null
                    || !CanonicalLibraryMetadataCutoverCandidate.Candidates(
                        context.LibraryPlan, context.LocalManifest, context.PeerManifest)
                        .FirstOrDefault(c => c.Action.ActionID == context.Action.ActionID)
                        is { } candidate)
                    return CanonicalApplyRuntimeExecutorResult.Failure(
                        context.Action, CanonicalApplyRuntimeDomain.libraryMetadata,
                        reason: CanonicalApplyRuntimeBlocker.missingExecutor.ToString());

                var commit = await executor.CommitLibraryMetadata(candidate);
                if (commit.Committed && commit.PreconditionVerified && commit.PostconditionVerified)
                    return CanonicalApplyRuntimeExecutorResult.Success(
                        context.Action, CanonicalApplyRuntimeDomain.libraryMetadata, detail: commit.Reason);

                var rollback = await executor.RollbackLibraryMetadata(candidate,
                    commit.FailureKind ?? CanonicalCutoverFailure.applyFailureBeforeCommit);
                return CanonicalApplyRuntimeExecutorResult.Failure(
                    action: context.Action,
                    domain: CanonicalApplyRuntimeDomain.libraryMetadata,
                    preconditionVerified: commit.PreconditionVerified,
                    postconditionVerified: commit.PostconditionVerified,
                    rollbackAttempted: true,
                    rollbackSucceeded: rollback.Succeeded,
                    rollbackFatal: rollback.Fatal || rollback.Succeeded == false,
                    reason: commit.FailureKind?.ToString() ?? commit.Reason);
            });
    }

    public static CanonicalApplyRuntimeExecutorEntry GeneratedArtifacts(
        ICanonicalGeneratedArtifactCutoverExecutor executor,
        bool dryRunOnly = false,
        bool rootBoundApplyPortAvailable = true)
    {
        return new CanonicalApplyRuntimeExecutorEntry(
            domain: CanonicalApplyRuntimeDomain.generatedArtifacts,
            dryRunOnly: dryRunOnly,
            rootBoundApplyPortAvailable: rootBoundApplyPortAvailable,
            execute: async context =>
            {
                if (!CanonicalGeneratedArtifactCutoverCandidate.Candidates(
                        context.ApplyPlan, context.LocalManifest, context.PeerManifest)
                        .FirstOrDefault(c => c.Action.ActionID == context.Action.ActionID)
                        is { } candidate)
                    return CanonicalApplyRuntimeExecutorResult.Failure(
                        context.Action, CanonicalApplyRuntimeDomain.generatedArtifacts,
                        reason: CanonicalApplyRuntimeBlocker.missingExecutor.ToString());

                var commit = await executor.CommitGeneratedArtifact(candidate);
                if (commit.Committed && commit.PreconditionVerified && commit.PostconditionVerified)
                    return CanonicalApplyRuntimeExecutorResult.Success(
                        context.Action, CanonicalApplyRuntimeDomain.generatedArtifacts, detail: commit.Reason);

                var rollback = await executor.RollbackGeneratedArtifact(candidate,
                    commit.FailureKind ?? CanonicalCutoverFailure.applyFailureBeforeCommit);
                return CanonicalApplyRuntimeExecutorResult.Failure(
                    action: context.Action,
                    domain: CanonicalApplyRuntimeDomain.generatedArtifacts,
                    preconditionVerified: commit.PreconditionVerified,
                    postconditionVerified: commit.PostconditionVerified,
                    rollbackAttempted: true,
                    rollbackSucceeded: rollback.Succeeded,
                    rollbackFatal: rollback.Fatal || rollback.Succeeded == false,
                    reason: commit.FailureKind?.ToString() ?? commit.Reason);
            });
    }

    public static CanonicalApplyRuntimeExecutorEntry TombstoneConflict(
        ICanonicalTombstoneConflictCutoverExecutor executor,
        bool dryRunOnly = false,
        bool rootBoundApplyPortAvailable = true)
    {
        return new CanonicalApplyRuntimeExecutorEntry(
            domain: CanonicalApplyRuntimeDomain.tombstoneConflict,
            dryRunOnly: dryRunOnly,
            rootBoundApplyPortAvailable: rootBoundApplyPortAvailable,
            execute: async context =>
            {
                if (!CanonicalTombstoneConflictCandidate.Candidates(
                        context.ApplyPlan, context.LibraryPlan,
                        context.LocalManifest, context.PeerManifest)
                        .FirstOrDefault(c => c.Action.ActionID == context.Action.ActionID)
                        is { } candidate)
                    return CanonicalApplyRuntimeExecutorResult.Failure(
                        context.Action, CanonicalApplyRuntimeDomain.tombstoneConflict,
                        reason: CanonicalApplyRuntimeBlocker.missingExecutor.ToString());

                var commit = await executor.CommitTombstoneConflict(candidate);
                if (commit.Committed && commit.PreconditionVerified && commit.PostconditionVerified)
                    return CanonicalApplyRuntimeExecutorResult.Success(
                        context.Action, CanonicalApplyRuntimeDomain.tombstoneConflict, detail: commit.Reason);

                var rollback = await executor.RollbackTombstoneConflict(candidate,
                    commit.FailureKind ?? CanonicalCutoverFailure.applyFailureBeforeCommit);
                return CanonicalApplyRuntimeExecutorResult.Failure(
                    action: context.Action,
                    domain: CanonicalApplyRuntimeDomain.tombstoneConflict,
                    preconditionVerified: commit.PreconditionVerified,
                    postconditionVerified: commit.PostconditionVerified,
                    rollbackAttempted: true,
                    rollbackSucceeded: rollback.Succeeded,
                    rollbackFatal: rollback.Fatal || rollback.Succeeded == false,
                    reason: commit.FailureKind?.ToString() ?? commit.Reason);
            });
    }

    private static CanonicalCutoverFailure RecordingRollbackReason(
        CanonicalRecordingMetadataProductionCommitResult commit)
    {
        if (commit.FailureKind == CanonicalCutoverFailure.preconditionMismatch || commit.PreconditionVerified == false)
            return CanonicalCutoverFailure.preconditionMismatch;
        if (commit.FailureKind == CanonicalCutoverFailure.postconditionMismatch || commit.PostconditionVerified == false)
            return CanonicalCutoverFailure.postconditionMismatch;
        if (commit.PartialCommit)
            return CanonicalCutoverFailure.applyFailureAfterPartialCommit;
        return CanonicalCutoverFailure.applyFailureBeforeCommit;
    }
}

// ─── CanonicalApplyExecutor (file I/O based, from Swift lines 1012-1271) ─────

public sealed class CanonicalApplyExecutor
{
    private readonly CanonicalConflictResolver _conflictResolver;

    public CanonicalApplyExecutor(CanonicalConflictResolver? conflictResolver = null)
    {
        _conflictResolver = conflictResolver ?? new CanonicalConflictResolver();
    }

    public async Task<CanonicalApplyExecutionReport> Execute(
        CanonicalApplyPlan applyPlan,
        CanonicalApplyRuntimeContext context,
        CanonicalLibrarySyncPlan? libraryPlan = null)
    {
        var records = new List<CanonicalApplyExecutionRecord>();
        var allActions = new List<CanonicalApplyAction>(applyPlan.Actions);
        if (libraryPlan != null)
            allActions.AddRange(libraryPlan.ApplyActions);

        foreach (var action in allActions)
        {
            CanonicalApplyExecutionRecord record;
            try
            {
                record = await Execute(action, context);
            }
            catch (Exception ex)
            {
                record = new CanonicalApplyExecutionRecord(
                    actionID: action.ActionID,
                    kind: action.Kind,
                    target: action.Target,
                    status: CanonicalApplyExecutionStatus.failed,
                    contentHashPrefix: null,
                    byteSize: null,
                    failure: CanonicalApplyFailureReason.hashOrSizeMismatch,
                    detail: ex.Message);
            }
            records.Add(record);
        }

        var conflictReport = _conflictResolver.Resolve(
            conflicts: applyPlan.Conflicts,
            libraryConflicts: libraryPlan?.Conflicts?.ToArray()
                ?? Array.Empty<CanonicalLibraryConflict>());

        return new CanonicalApplyExecutionReport(
            records: Deduplicated(records),
            conflictReport: conflictReport,
            appliedCount: records.Count(r => r.Status == CanonicalApplyExecutionStatus.applied
                                             || r.Status == CanonicalApplyExecutionStatus.sent),
            failedCount: records.Count(r => r.Status == CanonicalApplyExecutionStatus.failed));
    }

    private async Task<CanonicalApplyExecutionRecord> Execute(
        CanonicalApplyAction action,
        CanonicalApplyRuntimeContext context)
    {
        return action.Kind switch
        {
            CanonicalApplyActionKind.recordingMetadataApply
                or CanonicalApplyActionKind.folderMetadataApply
                or CanonicalApplyActionKind.studyItemMetadataApply =>
                await WriteMetadata(action, context.PeerManifest, context.LocalFileStore,
                    context.LocalMetadataRoot, CanonicalApplyExecutionStatus.applied),

            CanonicalApplyActionKind.recordingMetadataSend
                or CanonicalApplyActionKind.folderMetadataSend
                or CanonicalApplyActionKind.studyItemMetadataSend =>
                await WriteMetadata(action, context.LocalManifest, context.PeerFileStore,
                    context.PeerMetadataRoot, CanonicalApplyExecutionStatus.sent),

            CanonicalApplyActionKind.objectTombstoneApply
                or CanonicalApplyActionKind.libraryTombstoneApply =>
                await MarkTombstone(action, context.LocalFileStore, context.LocalMetadataRoot,
                    CanonicalApplyExecutionStatus.applied),

            CanonicalApplyActionKind.objectTombstoneSend
                or CanonicalApplyActionKind.libraryTombstoneSend =>
                await MarkTombstone(action, context.PeerFileStore, context.PeerMetadataRoot,
                    CanonicalApplyExecutionStatus.sent),

            CanonicalApplyActionKind.generatedArtifactDownloadApply =>
                await DownloadGeneratedArtifact(action, context),

            CanonicalApplyActionKind.generatedArtifactNoOp =>
                Record(action, CanonicalApplyExecutionStatus.noOp, "generatedArtifactSameContent"),

            CanonicalApplyActionKind.artifactTombstoneApply =>
                await MarkTombstone(action, context.LocalFileStore, context.LocalGeneratedRoot,
                    CanonicalApplyExecutionStatus.deferredUnsupported),

            CanonicalApplyActionKind.conflictRecord =>
                Record(action, CanonicalApplyExecutionStatus.conflictRecorded, action.ConflictID),

            CanonicalApplyActionKind.deferredUnsupported =>
                Record(action, CanonicalApplyExecutionStatus.deferredUnsupported,
                    action.FailureReason?.ToString()),

            _ => Record(action, CanonicalApplyExecutionStatus.deferredUnsupported, "unknownActionKind")
        };
    }

    private async Task<CanonicalApplyExecutionRecord> WriteMetadata(
        CanonicalApplyAction action,
        CanonicalManifest sourceManifest,
        ICanonicalFileStorePort targetStore,
        CanonicalRootToken root,
        CanonicalApplyExecutionStatus status)
    {
        var data = MetadataData(action, sourceManifest);
        var hash = InMemoryCanonicalFileStore.Hash(data, CanonicalFileHashPolicy.sha256);
        var reference = new CanonicalFileReference(
            rootToken: root,
            logicalPathToken: MetadataPathToken(action),
            artifactID: action.Target.ArtifactID,
            artifactKind: action.Target.ArtifactKind);
        var result = await targetStore.Write(
            new CanonicalFileWriteIntent(
                reference: reference,
                bytes: data,
                purpose: CanonicalFilePurpose.metadataBlob,
                expectedContentHash: hash,
                expectedByteSize: data.Length,
                conflictPolicy: CanonicalFileConflictPolicy.replace,
                metadataBlob: new CanonicalMetadataBlob(new Dictionary<string, string>
                {
                    ["action"] = action.Kind.ToString(),
                    ["objectID"] = action.Target.ObjectID,
                    ["hashPrefix"] = hash.HasValue ? hash.Value.Value[..Math.Min(hash.Value.Value.Length, 12)] : ""
                })));
        return new CanonicalApplyExecutionRecord(
            actionID: action.ActionID,
            kind: action.Kind,
            target: action.Target,
            status: status,
            contentHashPrefix: result.ContentHash.HasValue
                ? result.ContentHash.Value.Value[..Math.Min(result.ContentHash.Value.Value.Length, 12)]
                : null,
            byteSize: result.ByteSize,
            failure: null,
            detail: result.Disposition.ToString());
    }

    private async Task<CanonicalApplyExecutionRecord> DownloadGeneratedArtifact(
        CanonicalApplyAction action,
        CanonicalApplyRuntimeContext context)
    {
        var kind = action.Target.ArtifactKind;
        if (kind == null)
            throw CanonicalApplyRuntimeException.MissingSourceArtifact(action.Target.ObjectID);

        var artifact = FindArtifact(action.Target.ObjectID, kind.Value, context.PeerManifest);
        if (artifact == null)
            throw CanonicalApplyRuntimeException.MissingSourceArtifact(action.Target.ObjectID);

        var token = artifact.LogicalPathToken;
        if (token == null)
            throw CanonicalApplyRuntimeException.MissingLogicalPathToken(action.Target.ObjectID);

        var peerReference = new CanonicalFileReference(
            rootToken: context.PeerGeneratedRoot,
            logicalPathToken: token,
            artifactID: artifact.ArtifactID,
            artifactKind: artifact.ArtifactKind);
        var read = await context.PeerFileStore.Read(
            new CanonicalFileReadRequest(reference: peerReference));
        Validate(read, artifact);

        var localReference = new CanonicalFileReference(
            rootToken: context.LocalGeneratedRoot,
            logicalPathToken: token,
            artifactID: artifact.ArtifactID,
            artifactKind: artifact.ArtifactKind);
        var write = await context.LocalFileStore.Write(
            new CanonicalFileWriteIntent(
                reference: localReference,
                bytes: read.Bytes,
                purpose: CanonicalFilePurpose.generatedArtifact,
                expectedContentHash: artifact.ContentHash,
                expectedByteSize: artifact.ByteSize,
                conflictPolicy: CanonicalFileConflictPolicy.idempotentIfSameContent));

        return new CanonicalApplyExecutionRecord(
            actionID: action.ActionID,
            kind: action.Kind,
            target: action.Target,
            status: CanonicalApplyExecutionStatus.applied,
            contentHashPrefix: write.ContentHash.HasValue
                ? write.ContentHash.Value.Value[..Math.Min(write.ContentHash.Value.Value.Length, 12)]
                : null,
            byteSize: write.ByteSize,
            failure: null,
            detail: write.Disposition.ToString());
    }

    private async Task<CanonicalApplyExecutionRecord> MarkTombstone(
        CanonicalApplyAction action,
        ICanonicalFileStorePort targetStore,
        CanonicalRootToken root,
        CanonicalApplyExecutionStatus status)
    {
        var reference = new CanonicalFileReference(
            rootToken: root,
            logicalPathToken: action.Target.ArtifactKind == null
                ? MetadataPathToken(action)
                : ArtifactTombstoneToken(action),
            artifactID: action.Target.ArtifactID,
            artifactKind: action.Target.ArtifactKind);
        var result = await targetStore.MarkTombstone(reference, action.Reason);
        return new CanonicalApplyExecutionRecord(
            actionID: action.ActionID,
            kind: action.Kind,
            target: action.Target,
            status: status,
            contentHashPrefix: result.ContentHash.HasValue
                ? result.ContentHash.Value.Value[..Math.Min(result.ContentHash.Value.Value.Length, 12)]
                : null,
            byteSize: result.ByteSize,
            failure: action.FailureReason,
            detail: "noPhysicalDelete");
    }

    private byte[] MetadataData(CanonicalApplyAction action, CanonicalManifest sourceManifest)
    {
        var obj = sourceManifest.Objects.FirstOrDefault(o => o.ObjectID == action.Target.ObjectID);
        if (obj != null)
            return CanonicalTransportJSON.Encode(obj.Metadata);

        var libObj = sourceManifest.LibraryObjects.FirstOrDefault(
            lo => lo.ObjectID.RawValue == action.Target.ObjectID);
        if (libObj != null)
            return CanonicalTransportJSON.Encode(libObj);

        throw CanonicalApplyRuntimeException.MissingSourceObject(action.Target.ObjectID);
    }

    private static void Validate(CanonicalFileReadResult read, CanonicalArtifact artifact)
    {
        if (artifact.ByteSize.HasValue && read.ByteSize != artifact.ByteSize.Value)
            throw CanonicalApplyRuntimeException.HashOrSizeMismatch(artifact.ArtifactID);
        if (artifact.ContentHash.HasValue && read.ContentHash.HasValue
            && artifact.ContentHash.Value != read.ContentHash.Value)
            throw CanonicalApplyRuntimeException.HashOrSizeMismatch(artifact.ArtifactID);
    }

    private static CanonicalArtifact? FindArtifact(string objectID, CanonicalArtifact.Kind kind, CanonicalManifest manifest)
        => manifest.Objects.FirstOrDefault(o => o.ObjectID == objectID)?
            .Artifacts.FirstOrDefault(a => a.ArtifactKind == kind);

    private static CanonicalApplyExecutionRecord Record(
        CanonicalApplyAction action,
        CanonicalApplyExecutionStatus status,
        string? detail)
        => new(
            actionID: action.ActionID,
            kind: action.Kind,
            target: action.Target,
            status: status,
            contentHashPrefix: null,
            byteSize: null,
            failure: action.FailureReason,
            detail: detail);

    private static string MetadataPathToken(CanonicalApplyAction action)
    {
        var kind = action.Kind.ToString();
        return $"metadata/{SafePathComponent(kind)}/{SafePathComponent(action.Target.ObjectID)}.json";
    }

    private static string ArtifactTombstoneToken(CanonicalApplyAction action)
    {
        var kind = action.Target.ArtifactKind?.ToString() ?? "artifact";
        return $"tombstones/{SafePathComponent(action.Target.ObjectID)}/{SafePathComponent(kind)}.marker";
    }

    private static string SafePathComponent(string value)
    {
        var allowed = new HashSet<char>(
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_");
        var pieces = value.Select(c => allowed.Contains(c) ? c.ToString() : "-");
        var component = string.Concat(pieces).Trim('-');
        if (component.Length == 0)
            return CanonicalHash.Sha256String(value).Value[..Math.Min(CanonicalHash.Sha256String(value).Value.Length, 12)];
        return component;
    }

    private static CanonicalApplyExecutionRecord[] Deduplicated(CanonicalApplyExecutionRecord[] records)
    {
        var seen = new HashSet<string>();
        return records.Where(r => seen.Add(r.ActionID)).ToArray();
    }
}

// ─── Forward-reference types for cutover executors ──────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalCutoverFailure
{
    disabled,
    unsupportedDomain,
    modeNotExecutable,
    missingToken,
    missingOwnerApproval,
    missingRollback,
    missingRealDataShadowCopyEvidence,
    missingExecutionShadowEvidence,
    missingDryRunEquivalence,
    blockingDivergence,
    unresolvedConflict,
    missingReadOnlyTransportProbe,
    productionPortUnavailable,
    legacyFallbackUnavailable,
    viewRefreshTriggerDenied,
    retryDrainerFreshMetadataDenied,
    unsupportedAction,
    unstableMetadataHash,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    rollbackVerificationMissing,
    productionRootEnabledByDefault,
    testRootMissing,
    missingInternalCanaryConfiguration,
    canaryBudgetAboveOneDenied,
    missingCanaryStageEvidence,
    canaryStageBlocked,
    canaryStageOrderViolation,
    observationWindowIncomplete,
    runtimeSwitchDenied,
    unsupportedObject,
    previousStageFailure,
    previousStageRollbackFailure,
    previousStageBlockingDivergence,
    previousStageUnresolvedConflict,
    preconditionMismatch,
    postconditionMismatch,
    transportFailureBeforeSend,
    applyFailureBeforeCommit,
    applyFailureAfterPartialCommit,
    rollbackFailed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCutoverActionKind
{
    apply,
    send
}

public sealed class CanonicalRecordingMetadataCutoverCandidate
{
    public CanonicalApplyAction Action { get; }
    public CanonicalRecordingObject? LocalObject { get; }
    public CanonicalRecordingObject? PeerObject { get; }
    public string? RollbackCheckpointID { get; }
    public bool UnresolvedConflict { get; }

    public CanonicalRecordingMetadataCutoverCandidate(
        CanonicalApplyAction action,
        CanonicalRecordingObject? localObject,
        CanonicalRecordingObject? peerObject,
        string? rollbackCheckpointID = null,
        bool unresolvedConflict = false)
    {
        Action = action;
        LocalObject = localObject;
        PeerObject = peerObject;
        RollbackCheckpointID = rollbackCheckpointID != null
            ? CanonicalProductionRedaction.SafeIdentifier(rollbackCheckpointID, "recording-metadata-checkpoint")
            : null;
        UnresolvedConflict = unresolvedConflict;
    }

    public string ObjectID => Action.Target.ObjectID;

    public CanonicalRecordingMetadataCutoverActionKind? CutoverActionKind
        => Action.Kind switch
        {
            CanonicalApplyActionKind.recordingMetadataApply => CanonicalRecordingMetadataCutoverActionKind.apply,
            CanonicalApplyActionKind.recordingMetadataSend => CanonicalRecordingMetadataCutoverActionKind.send,
            _ => null
        };

    public bool RequiresNetworkSend => CutoverActionKind == CanonicalRecordingMetadataCutoverActionKind.send;

    public CanonicalRecordingObject? ExpectedObject
        => CutoverActionKind switch
        {
            CanonicalRecordingMetadataCutoverActionKind.apply => PeerObject,
            CanonicalRecordingMetadataCutoverActionKind.send => LocalObject,
            _ => null
        };

    public CanonicalHash? StableMetadataHash => ExpectedObject?.MetadataHash;

    public string EffectiveRollbackCheckpointID
        => RollbackCheckpointID ?? $"recording-metadata-cutover-{ObjectID}";
}

public interface ICanonicalRecordingMetadataCutoverExecutor
{
    Task<CanonicalRecordingMetadataProductionCommitResult> CommitRecordingMetadata(
        CanonicalRecordingMetadataCutoverCandidate candidate);

    Task<CanonicalRecordingMetadataRollbackExecutionResult> RollbackRecordingMetadata(
        CanonicalRecordingMetadataCutoverCandidate candidate,
        CanonicalCutoverFailure reason);
}

public sealed class CanonicalRecordingMetadataProductionCommitResult
{
    public string ActionID { get; }
    public string ObjectID { get; }
    public CanonicalRecordingMetadataCutoverActionKind ActionKind { get; }
    public bool Committed { get; }
    public bool PartialCommit { get; }
    public bool PreconditionVerified { get; }
    public bool PostconditionVerified { get; }
    public string? RoutePath { get; }
    public string? MetadataHashPrefix { get; }
    public CanonicalCutoverFailure? FailureKind { get; }
    public string Reason { get; }

    public CanonicalRecordingMetadataProductionCommitResult(
        string actionID,
        string objectID,
        CanonicalRecordingMetadataCutoverActionKind actionKind,
        bool committed,
        bool partialCommit = false,
        bool preconditionVerified = true,
        bool postconditionVerified = true,
        string? routePath = null,
        CanonicalHash? metadataHash = null,
        CanonicalCutoverFailure? failureKind = null,
        string reason = "")
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, actionKind.ToString());
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        ActionKind = actionKind;
        Committed = committed;
        PartialCommit = partialCommit;
        PreconditionVerified = preconditionVerified;
        PostconditionVerified = postconditionVerified;
        RoutePath = routePath != null ? CanonicalProductionRedaction.SafeDiagnosticText(routePath) : null;
        MetadataHashPrefix = metadataHash.HasValue
            ? CanonicalProductionRedaction.HashPrefix(metadataHash.Value.Value)
            : null;
        FailureKind = failureKind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (committed ? "committed" : "failed");
    }
}

public sealed class CanonicalRecordingMetadataRollbackExecutionResult
{
    public string CheckpointID { get; }
    public bool Succeeded { get; }
    public bool Fatal { get; }
    public string Reason { get; }

    public CanonicalRecordingMetadataRollbackExecutionResult(
        string checkpointID,
        bool succeeded,
        bool fatal = false,
        string reason = "")
    {
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "recording-metadata-checkpoint");
        Succeeded = succeeded;
        Fatal = fatal;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason)
                 ?? (succeeded ? "rollbackCompleted" : "rollbackFailed");
    }
}

public interface ICanonicalLibraryMetadataCutoverExecutor
{
    Task<CanonicalRecordingMetadataProductionCommitResult> CommitLibraryMetadata(
        CanonicalLibraryMetadataCutoverCandidate candidate);

    Task<CanonicalRecordingMetadataRollbackExecutionResult> RollbackLibraryMetadata(
        CanonicalLibraryMetadataCutoverCandidate candidate,
        CanonicalCutoverFailure reason);
}

public sealed class CanonicalLibraryMetadataCutoverCandidate
{
    public CanonicalApplyAction Action { get; }
    public string ObjectID { get; }

    public CanonicalLibraryMetadataCutoverCandidate(
        CanonicalApplyAction action,
        string objectID)
    {
        Action = action;
        ObjectID = objectID;
    }

    public static CanonicalLibraryMetadataCutoverCandidate[] Candidates(
        CanonicalLibrarySyncPlan libraryPlan,
        CanonicalManifest localManifest,
        CanonicalManifest peerManifest)
        => libraryPlan.ApplyActions
            .Select(a => new CanonicalLibraryMetadataCutoverCandidate(a, a.Target.ObjectID))
            .ToArray();
}

public interface ICanonicalGeneratedArtifactCutoverExecutor
{
    Task<CanonicalRecordingMetadataProductionCommitResult> CommitGeneratedArtifact(
        CanonicalGeneratedArtifactCutoverCandidate candidate);

    Task<CanonicalRecordingMetadataRollbackExecutionResult> RollbackGeneratedArtifact(
        CanonicalGeneratedArtifactCutoverCandidate candidate,
        CanonicalCutoverFailure reason);
}

public sealed class CanonicalGeneratedArtifactCutoverCandidate
{
    public CanonicalApplyAction Action { get; }
    public string ObjectID { get; }

    public CanonicalGeneratedArtifactCutoverCandidate(
        CanonicalApplyAction action,
        string objectID)
    {
        Action = action;
        ObjectID = objectID;
    }

    public static CanonicalGeneratedArtifactCutoverCandidate[] Candidates(
        CanonicalApplyPlan applyPlan,
        CanonicalManifest localManifest,
        CanonicalManifest peerManifest)
        => applyPlan.Actions
            .Where(a => a.Kind == CanonicalApplyActionKind.generatedArtifactDownloadApply
                        || a.Kind == CanonicalApplyActionKind.generatedArtifactNoOp)
            .Select(a => new CanonicalGeneratedArtifactCutoverCandidate(a, a.Target.ObjectID))
            .ToArray();
}

public interface ICanonicalTombstoneConflictCutoverExecutor
{
    Task<CanonicalRecordingMetadataProductionCommitResult> CommitTombstoneConflict(
        CanonicalTombstoneConflictCandidate candidate);

    Task<CanonicalRecordingMetadataRollbackExecutionResult> RollbackTombstoneConflict(
        CanonicalTombstoneConflictCandidate candidate,
        CanonicalCutoverFailure reason);
}

public sealed class CanonicalTombstoneConflictCandidate
{
    public CanonicalApplyAction Action { get; }
    public string ObjectID { get; }

    public CanonicalTombstoneConflictCandidate(
        CanonicalApplyAction action,
        string objectID)
    {
        Action = action;
        ObjectID = objectID;
    }

    public static CanonicalTombstoneConflictCandidate[] Candidates(
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan? libraryPlan,
        CanonicalManifest localManifest,
        CanonicalManifest peerManifest)
    {
        var candidates = applyPlan.Actions
            .Where(a => a.Kind == CanonicalApplyActionKind.libraryTombstoneApply
                        || a.Kind == CanonicalApplyActionKind.libraryTombstoneSend
                        || a.Kind == CanonicalApplyActionKind.objectTombstoneApply
                        || a.Kind == CanonicalApplyActionKind.objectTombstoneSend
                        || a.Kind == CanonicalApplyActionKind.artifactTombstoneApply
                        || a.Kind == CanonicalApplyActionKind.conflictRecord)
            .Select(a => new CanonicalTombstoneConflictCandidate(a, a.Target.ObjectID));

        if (libraryPlan != null)
        {
            var libCandidates = libraryPlan.ApplyActions
                .Where(a => a.Kind == CanonicalApplyActionKind.libraryTombstoneApply
                            || a.Kind == CanonicalApplyActionKind.libraryTombstoneSend)
                .Select(a => new CanonicalTombstoneConflictCandidate(a, a.Target.ObjectID));
            return candidates.Concat(libCandidates).ToArray();
        }

        return candidates.ToArray();
    }
}

// ─── CanonicalConflictResolver ──────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalConflictResolverAction
{
    recordOnly,
    keepBothNoOverwrite,
    requireManualReview,
    tombstoneManualReview
}

public sealed class CanonicalConflictResolutionDecision : IEquatable<CanonicalConflictResolutionDecision>
{
    public string Id => ConflictID;
    public string ConflictID { get; }
    public CanonicalApplyTarget Target { get; }
    public CanonicalConflictResolverAction Action { get; }
    public CanonicalConflictResolutionState State { get; }
    public string? Detail { get; }

    public CanonicalConflictResolutionDecision(
        string conflictID,
        CanonicalApplyTarget target,
        CanonicalConflictResolverAction action,
        CanonicalConflictResolutionState state = CanonicalConflictResolutionState.unresolved,
        string? detail = null)
    {
        ConflictID = conflictID;
        Target = target;
        Action = action;
        State = state;
        Detail = detail;
    }

    public override bool Equals(object? obj) => obj is CanonicalConflictResolutionDecision other && Equals(other);
    public bool Equals(CanonicalConflictResolutionDecision? other) =>
        other is not null && ConflictID == other.ConflictID;
    public override int GetHashCode() => ConflictID.GetHashCode();
    public static bool operator ==(CanonicalConflictResolutionDecision left, CanonicalConflictResolutionDecision right) => left.Equals(right);
    public static bool operator !=(CanonicalConflictResolutionDecision left, CanonicalConflictResolutionDecision right) => !left.Equals(right);
}

public sealed class CanonicalConflictResolverReport : IEquatable<CanonicalConflictResolverReport>
{
    public CanonicalConflictResolutionDecision[] Decisions { get; }
    public int UnresolvedCount { get; }
    public int ManualReviewCount { get; }
    public int KeepBothCount { get; }

    public CanonicalConflictResolverReport(
        CanonicalConflictResolutionDecision[] decisions,
        int unresolvedCount,
        int manualReviewCount,
        int keepBothCount)
    {
        Decisions = decisions ?? Array.Empty<CanonicalConflictResolutionDecision>();
        UnresolvedCount = unresolvedCount;
        ManualReviewCount = manualReviewCount;
        KeepBothCount = keepBothCount;
    }

    public override bool Equals(object? obj) => obj is CanonicalConflictResolverReport other && Equals(other);
    public bool Equals(CanonicalConflictResolverReport? other) =>
        other is not null &&
        Decisions.SequenceEqual(other.Decisions) &&
        UnresolvedCount == other.UnresolvedCount &&
        ManualReviewCount == other.ManualReviewCount &&
        KeepBothCount == other.KeepBothCount;
    public override int GetHashCode() => HashCode.Combine(UnresolvedCount, ManualReviewCount, KeepBothCount);
    public static bool operator ==(CanonicalConflictResolverReport left, CanonicalConflictResolverReport right) => left.Equals(right);
    public static bool operator !=(CanonicalConflictResolverReport left, CanonicalConflictResolverReport right) => !left.Equals(right);
}

public sealed class CanonicalConflictResolver
{
    public CanonicalConflictResolverReport Resolve(
        CanonicalConflictRecord[] conflicts,
        CanonicalLibraryConflict[] libraryConflicts)
    {
        var recordingDecisions = conflicts.Select(Decision).ToArray();
        var libraryDecisions = libraryConflicts.Select(Decision).ToArray();
        var allDecisions = recordingDecisions.Concat(libraryDecisions)
            .OrderBy(d => d.ConflictID, StringComparer.Ordinal)
            .ToArray();

        return new CanonicalConflictResolverReport(
            decisions: allDecisions,
            unresolvedCount: allDecisions.Count(d => d.State == CanonicalConflictResolutionState.unresolved),
            manualReviewCount: allDecisions.Count(d =>
                d.Action == CanonicalConflictResolverAction.requireManualReview
                || d.Action == CanonicalConflictResolverAction.tombstoneManualReview),
            keepBothCount: allDecisions.Count(d =>
                d.Action == CanonicalConflictResolverAction.keepBothNoOverwrite));
    }

    private static CanonicalConflictResolutionDecision Decision(CanonicalConflictRecord conflict)
    {
        var action = conflict.ResolutionPolicy switch
        {
            CanonicalConflictResolutionPolicy.manualReview => CanonicalConflictResolverAction.requireManualReview,
            CanonicalConflictResolutionPolicy.keepBothNoOverwrite => CanonicalConflictResolverAction.keepBothNoOverwrite,
            CanonicalConflictResolutionPolicy.tombstoneRequiresManualReview => CanonicalConflictResolverAction
                .tombstoneManualReview,
            _ => CanonicalConflictResolverAction.requireManualReview
        };
        return new CanonicalConflictResolutionDecision(
            conflictID: conflict.ConflictID,
            target: conflict.Target,
            action: action,
            state: CanonicalConflictResolutionState.unresolved,
            detail: conflict.Kind.ToString());
    }

    private static CanonicalConflictResolutionDecision Decision(CanonicalLibraryConflict conflict)
    {
        var action = conflict.Kind == CanonicalLibraryConflictKind.activeVsTombstone
            ? CanonicalConflictResolverAction.tombstoneManualReview
            : CanonicalConflictResolverAction.requireManualReview;
        return new CanonicalConflictResolutionDecision(
            conflictID: conflict.ConflictID,
            target: new CanonicalApplyTarget(conflict.ObjectID.RawValue),
            action: action,
            state: CanonicalConflictResolutionState.unresolved,
            detail: conflict.Kind.ToString());
    }
}

// ─── File I/O types used by CanonicalApplyExecutor ──────────────────────────

public readonly struct CanonicalRootToken : IEquatable<CanonicalRootToken>
{
    public string RawValue { get; }

    public CanonicalRootToken(string rawValue)
    {
        RawValue = rawValue.Trim().NilIfEmpty() ?? "root:unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalRootToken other && Equals(other);
    public bool Equals(CanonicalRootToken other) => RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();
    public override string ToString() => RawValue;
    public static bool operator ==(CanonicalRootToken left, CanonicalRootToken right) => left.Equals(right);
    public static bool operator !=(CanonicalRootToken left, CanonicalRootToken right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFilePurpose
{
    artifactBytes,
    generatedArtifact,
    metadataBlob,
    tombstoneMarker
}

public sealed class CanonicalFileReference : IEquatable<CanonicalFileReference>
{
    public CanonicalRootToken RootToken { get; }
    public string LogicalPathToken { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }

    public CanonicalFileReference(
        CanonicalRootToken rootToken,
        string logicalPathToken,
        string? artifactID = null,
        CanonicalArtifact.Kind? artifactKind = null)
    {
        RootToken = rootToken;
        LogicalPathToken = logicalPathToken.Trim();
        ArtifactID = artifactID?.Trim().NilIfEmpty();
        ArtifactKind = artifactKind;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileReference other && Equals(other);
    public bool Equals(CanonicalFileReference? other) =>
        other is not null &&
        RootToken.Equals(other.RootToken) &&
        LogicalPathToken == other.LogicalPathToken &&
        ArtifactID == other.ArtifactID &&
        ArtifactKind == other.ArtifactKind;
    public override int GetHashCode() => HashCode.Combine(RootToken, LogicalPathToken, ArtifactID, ArtifactKind);
    public static bool operator ==(CanonicalFileReference left, CanonicalFileReference right) => left.Equals(right);
    public static bool operator !=(CanonicalFileReference left, CanonicalFileReference right) => !left.Equals(right);
}

// CanonicalFileHandle is a typealias
using CanonicalFileHandle = CanonicalFileReference;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalAtomicWritePolicy
{
    atomicReplace,
    directInMemoryReplace
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFileHashPolicy
{
    sha256,
    none
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFileConflictPolicy
{
    noOverwrite,
    replace,
    replaceIfExistingHashMatches,
    idempotentIfSameContent
}

public sealed class CanonicalMetadataBlob : IEquatable<CanonicalMetadataBlob>
{
    public Dictionary<string, string> Fields { get; }

    public CanonicalMetadataBlob(Dictionary<string, string>? fields = null)
    {
        Fields = new Dictionary<string, string>();
        if (fields != null)
            foreach (var kv in fields)
                if (!string.IsNullOrWhiteSpace(kv.Key))
                    Fields[kv.Key.Trim()] = kv.Value;
    }

    public override bool Equals(object? obj) => obj is CanonicalMetadataBlob other && Equals(other);
    public bool Equals(CanonicalMetadataBlob? other) =>
        other is not null &&
        Fields.Count == other.Fields.Count &&
        Fields.All(kv => other.Fields.TryGetValue(kv.Key, out var v) && v == kv.Value);
    public override int GetHashCode() => Fields.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key, kv.Value));
    public static bool operator ==(CanonicalMetadataBlob left, CanonicalMetadataBlob right) => left.Equals(right);
    public static bool operator !=(CanonicalMetadataBlob left, CanonicalMetadataBlob right) => !left.Equals(right);
}

public sealed class CanonicalFileWriteIntent : IEquatable<CanonicalFileWriteIntent>
{
    public CanonicalFileReference Reference { get; }
    public byte[] Bytes { get; }
    public CanonicalFilePurpose Purpose { get; }
    public CanonicalHash? ExpectedContentHash { get; }
    public long? ExpectedByteSize { get; }
    public CanonicalHash? ExpectedExistingHash { get; }
    public CanonicalAtomicWritePolicy AtomicPolicy { get; }
    public CanonicalFileHashPolicy HashPolicy { get; }
    public CanonicalFileConflictPolicy ConflictPolicy { get; }
    public CanonicalMetadataBlob? MetadataBlob { get; }

    public CanonicalFileWriteIntent(
        CanonicalFileReference reference,
        byte[] bytes,
        CanonicalFilePurpose purpose = CanonicalFilePurpose.artifactBytes,
        CanonicalHash? expectedContentHash = null,
        long? expectedByteSize = null,
        CanonicalHash? expectedExistingHash = null,
        CanonicalAtomicWritePolicy atomicPolicy = CanonicalAtomicWritePolicy.atomicReplace,
        CanonicalFileHashPolicy hashPolicy = CanonicalFileHashPolicy.sha256,
        CanonicalFileConflictPolicy conflictPolicy = CanonicalFileConflictPolicy.noOverwrite,
        CanonicalMetadataBlob? metadataBlob = null)
    {
        Reference = reference;
        Bytes = bytes ?? Array.Empty<byte>();
        Purpose = purpose;
        ExpectedContentHash = expectedContentHash;
        ExpectedByteSize = expectedByteSize;
        ExpectedExistingHash = expectedExistingHash;
        AtomicPolicy = atomicPolicy;
        HashPolicy = hashPolicy;
        ConflictPolicy = conflictPolicy;
        MetadataBlob = metadataBlob;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileWriteIntent other && Equals(other);
    public bool Equals(CanonicalFileWriteIntent? other) =>
        other is not null && Reference.Equals(other.Reference);
    public override int GetHashCode() => Reference.GetHashCode();
    public static bool operator ==(CanonicalFileWriteIntent left, CanonicalFileWriteIntent right) => left.Equals(right);
    public static bool operator !=(CanonicalFileWriteIntent left, CanonicalFileWriteIntent right) => !left.Equals(right);
}

public sealed class CanonicalFileReadRequest : IEquatable<CanonicalFileReadRequest>
{
    public CanonicalFileReference Reference { get; }
    public bool AllowTombstonedRead { get; }

    public CanonicalFileReadRequest(
        CanonicalFileReference reference,
        bool allowTombstonedRead = false)
    {
        Reference = reference;
        AllowTombstonedRead = allowTombstonedRead;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileReadRequest other && Equals(other);
    public bool Equals(CanonicalFileReadRequest? other) =>
        other is not null && Reference.Equals(other.Reference);
    public override int GetHashCode() => Reference.GetHashCode();
    public static bool operator ==(CanonicalFileReadRequest left, CanonicalFileReadRequest right) => left.Equals(right);
    public static bool operator !=(CanonicalFileReadRequest left, CanonicalFileReadRequest right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFileWriteDisposition
{
    created,
    replaced,
    acceptedExisting,
    tombstoneMarked
}

public sealed class CanonicalFileWriteResult : IEquatable<CanonicalFileWriteResult>
{
    public CanonicalFileHandle Handle { get; }
    public CanonicalRootToken RootToken { get; }
    public string LogicalPathToken { get; }
    public string ResolvedPathToken { get; }
    public bool IsInsideRoot { get; }
    public long ByteSize { get; }
    public CanonicalHash? ContentHash { get; }
    public CanonicalFileWriteDisposition Disposition { get; }
    public CanonicalFilePurpose Purpose { get; }
    public bool Tombstoned { get; }

    public CanonicalFileWriteResult(
        CanonicalFileHandle handle,
        CanonicalRootToken rootToken,
        string logicalPathToken,
        string resolvedPathToken,
        bool isInsideRoot,
        long byteSize,
        CanonicalHash? contentHash,
        CanonicalFileWriteDisposition disposition,
        CanonicalFilePurpose purpose,
        bool tombstoned)
    {
        Handle = handle;
        RootToken = rootToken;
        LogicalPathToken = logicalPathToken;
        ResolvedPathToken = resolvedPathToken;
        IsInsideRoot = isInsideRoot;
        ByteSize = byteSize;
        ContentHash = contentHash;
        Disposition = disposition;
        Purpose = purpose;
        Tombstoned = tombstoned;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileWriteResult other && Equals(other);
    public bool Equals(CanonicalFileWriteResult? other) =>
        other is not null && Handle.Equals(other.Handle) && Disposition == other.Disposition;
    public override int GetHashCode() => HashCode.Combine(Handle, Disposition);
    public static bool operator ==(CanonicalFileWriteResult left, CanonicalFileWriteResult right) => left.Equals(right);
    public static bool operator !=(CanonicalFileWriteResult left, CanonicalFileWriteResult right) => !left.Equals(right);
}

public sealed class CanonicalFileReadResult : IEquatable<CanonicalFileReadResult>
{
    public CanonicalFileHandle Handle { get; }
    public CanonicalRootToken RootToken { get; }
    public string LogicalPathToken { get; }
    public string ResolvedPathToken { get; }
    public bool IsInsideRoot { get; }
    public byte[] Bytes { get; }
    public long ByteSize { get; }
    public CanonicalHash? ContentHash { get; }
    public CanonicalFilePurpose Purpose { get; }
    public CanonicalMetadataBlob? MetadataBlob { get; }
    public bool Tombstoned { get; }
    public string? TombstoneReason { get; }

    public CanonicalFileReadResult(
        CanonicalFileHandle handle,
        CanonicalRootToken rootToken,
        string logicalPathToken,
        string resolvedPathToken,
        bool isInsideRoot,
        byte[] bytes,
        long byteSize,
        CanonicalHash? contentHash,
        CanonicalFilePurpose purpose,
        CanonicalMetadataBlob? metadataBlob = null,
        bool tombstoned = false,
        string? tombstoneReason = null)
    {
        Handle = handle;
        RootToken = rootToken;
        LogicalPathToken = logicalPathToken;
        ResolvedPathToken = resolvedPathToken;
        IsInsideRoot = isInsideRoot;
        Bytes = bytes ?? Array.Empty<byte>();
        ByteSize = byteSize;
        ContentHash = contentHash;
        Purpose = purpose;
        MetadataBlob = metadataBlob;
        Tombstoned = tombstoned;
        TombstoneReason = tombstoneReason;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileReadResult other && Equals(other);
    public bool Equals(CanonicalFileReadResult? other) =>
        other is not null && Handle.Equals(other.Handle) && ByteSize == other.ByteSize;
    public override int GetHashCode() => HashCode.Combine(Handle, ByteSize);
    public static bool operator ==(CanonicalFileReadResult left, CanonicalFileReadResult right) => left.Equals(right);
    public static bool operator !=(CanonicalFileReadResult left, CanonicalFileReadResult right) => !left.Equals(right);
}

public interface ICanonicalFileStorePort
{
    Task<CanonicalFileReadResult> Read(CanonicalFileReadRequest request);
    Task<CanonicalFileWriteResult> Write(CanonicalFileWriteIntent intent);
    Task<CanonicalFileWriteResult> MarkTombstone(CanonicalFileReference reference, string? reason);
    Task<bool> Contains(CanonicalFileReference reference);
}

public sealed class InMemoryCanonicalFileStore : ICanonicalFileStorePort
{
    private sealed class Entry
    {
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public CanonicalFilePurpose Purpose { get; set; }
        public CanonicalHash? ContentHash { get; set; }
        public CanonicalMetadataBlob? MetadataBlob { get; set; }
        public bool Tombstoned { get; set; }
        public string? TombstoneReason { get; set; }
    }

    private readonly Dictionary<string, Entry> _entries = new();
    private readonly Dictionary<string, CanonicalRootToken> _rootBindings = new();

    public InMemoryCanonicalFileStore(Dictionary<CanonicalRootToken, string>? rootBindings = null)
    {
        if (rootBindings != null)
            foreach (var kv in rootBindings)
                _rootBindings[kv.Key.RawValue] = kv.Key;
    }

    public static CanonicalHash? Hash(byte[] data, CanonicalFileHashPolicy policy)
    {
        if (policy == CanonicalFileHashPolicy.none || data.Length == 0)
            return null;
        using var sha = System.Security.Cryptography.SHA256.Create();
        var digest = sha.ComputeHash(data);
        var hex = string.Concat(digest.Select(b => b.ToString("x2")));
        return new CanonicalHash(hex);
    }

    private string ResolvedPath(CanonicalFileReference reference)
    {
        var root = reference.RootToken.RawValue;
        var path = reference.LogicalPathToken;
        return $"{root}:{path}";
    }

    public Task<CanonicalFileReadResult> Read(CanonicalFileReadRequest request)
    {
        var resolved = ResolvedPath(request.Reference);
        if (!_entries.TryGetValue(resolved, out var entry))
            throw new InvalidOperationException($"File not found: {resolved}");

        if (entry.Tombstoned && !request.AllowTombstonedRead)
            throw new InvalidOperationException($"Tombstoned: {resolved}");

        return Task.FromResult(new CanonicalFileReadResult(
            handle: request.Reference,
            rootToken: request.Reference.RootToken,
            logicalPathToken: request.Reference.LogicalPathToken,
            resolvedPathToken: resolved,
            isInsideRoot: true,
            bytes: entry.Bytes,
            byteSize: entry.Bytes.Length,
            contentHash: entry.ContentHash,
            purpose: entry.Purpose,
            metadataBlob: entry.MetadataBlob,
            tombstoned: entry.Tombstoned,
            tombstoneReason: entry.TombstoneReason));
    }

    public Task<CanonicalFileWriteResult> Write(CanonicalFileWriteIntent intent)
    {
        var resolved = ResolvedPath(intent.Reference);
        var disposition = _entries.ContainsKey(resolved)
            ? CanonicalFileWriteDisposition.replaced
            : CanonicalFileWriteDisposition.created;

        var contentHash = Hash(intent.Bytes, intent.HashPolicy);
        _entries[resolved] = new Entry
        {
            Bytes = intent.Bytes,
            Purpose = intent.Purpose,
            ContentHash = contentHash,
            MetadataBlob = intent.MetadataBlob,
            Tombstoned = false
        };

        return Task.FromResult(new CanonicalFileWriteResult(
            handle: intent.Reference,
            rootToken: intent.Reference.RootToken,
            logicalPathToken: intent.Reference.LogicalPathToken,
            resolvedPathToken: resolved,
            isInsideRoot: true,
            byteSize: intent.Bytes.Length,
            contentHash: contentHash,
            disposition: disposition,
            purpose: intent.Purpose,
            tombstoned: false));
    }

    public Task<CanonicalFileWriteResult> MarkTombstone(CanonicalFileReference reference, string? reason)
    {
        var resolved = ResolvedPath(reference);
        if (_entries.TryGetValue(resolved, out var entry))
        {
            entry.Tombstoned = true;
            entry.TombstoneReason = reason;
        }
        else
        {
            _entries[resolved] = new Entry
            {
                Bytes = Array.Empty<byte>(),
                Purpose = CanonicalFilePurpose.tombstoneMarker,
                Tombstoned = true,
                TombstoneReason = reason
            };
        }

        return Task.FromResult(new CanonicalFileWriteResult(
            handle: reference,
            rootToken: reference.RootToken,
            logicalPathToken: reference.LogicalPathToken,
            resolvedPathToken: resolved,
            isInsideRoot: true,
            byteSize: 0,
            contentHash: null,
            disposition: CanonicalFileWriteDisposition.tombstoneMarked,
            purpose: CanonicalFilePurpose.tombstoneMarker,
            tombstoned: true));
    }

    public Task<bool> Contains(CanonicalFileReference reference)
    {
        var resolved = ResolvedPath(reference);
        return Task.FromResult(_entries.ContainsKey(resolved) && !_entries[resolved].Tombstoned);
    }
}
