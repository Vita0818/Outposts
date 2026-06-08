using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictDomain
{
    objectTombstone,
    libraryTombstone,
    generatedArtifactTombstoneMarker,
    activeVsTombstoneConflict,
    metadataConflictRecord,
    artifactConflictRecord
}

public static class CanonicalTombstoneConflictDomainExtensions
{
    public static CanonicalProductionDomain ToProductionDomain(this CanonicalTombstoneConflictDomain d) => d switch
    {
        CanonicalTombstoneConflictDomain.objectTombstone => CanonicalProductionDomain.tombstones,
        CanonicalTombstoneConflictDomain.libraryTombstone => CanonicalProductionDomain.tombstones,
        CanonicalTombstoneConflictDomain.generatedArtifactTombstoneMarker => CanonicalProductionDomain.tombstones,
        CanonicalTombstoneConflictDomain.activeVsTombstoneConflict => CanonicalProductionDomain.conflicts,
        CanonicalTombstoneConflictDomain.metadataConflictRecord => CanonicalProductionDomain.conflicts,
        CanonicalTombstoneConflictDomain.artifactConflictRecord => CanonicalProductionDomain.conflicts,
        _ => CanonicalProductionDomain.conflicts
    };

    public static bool RequiresConflictLedger(this CanonicalTombstoneConflictDomain d) => d switch
    {
        CanonicalTombstoneConflictDomain.activeVsTombstoneConflict => true,
        CanonicalTombstoneConflictDomain.metadataConflictRecord => true,
        CanonicalTombstoneConflictDomain.artifactConflictRecord => true,
        _ => false
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictActionKind
{
    objectTombstoneApply,
    objectTombstoneSend,
    libraryTombstoneApply,
    libraryTombstoneSend,
    generatedArtifactTombstoneMarkUnsupported,
    conflictRecord,
    resurrectionBlocked,
    unsupported
}

public static class CanonicalTombstoneConflictActionKindExtensions
{
    public static bool IsTombstoneMarkerWrite(this CanonicalTombstoneConflictActionKind k) => k switch
    {
        CanonicalTombstoneConflictActionKind.objectTombstoneApply => true,
        CanonicalTombstoneConflictActionKind.objectTombstoneSend => true,
        CanonicalTombstoneConflictActionKind.libraryTombstoneApply => true,
        CanonicalTombstoneConflictActionKind.libraryTombstoneSend => true,
        _ => false
    };

    public static bool IsConflictLedgerWrite(this CanonicalTombstoneConflictActionKind k) =>
        k == CanonicalTombstoneConflictActionKind.conflictRecord
        || k == CanonicalTombstoneConflictActionKind.resurrectionBlocked;

    public static bool IsExecutable(this CanonicalTombstoneConflictActionKind k) =>
        k.IsTombstoneMarkerWrite() || k.IsConflictLedgerWrite();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictFailure
{
    disabled,
    unsupportedMode,
    unsupportedDomain,
    unsupportedAction,
    missingToken,
    missingOwnerApproval,
    missingRollback,
    missingNoCommitEvidence,
    missingDryRunEquivalence,
    missingExecutionShadowEvidence,
    missingRealDataShadowCopyEvidence,
    blockingDivergence,
    unresolvedConflict,
    missingMetadataRouteEvidence,
    productionPortUnavailable,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    rollbackVerificationMissing,
    productionRootEnabledByDefault,
    testRootMissing,
    softDeleteMarkerUnsupported,
    softTombstoneStoreUnsupported,
    conflictLedgerUnsupported,
    objectIDMismatch,
    tombstoneStateMismatch,
    missingTombstoneTimestamp,
    missingTombstoneWinsPolicy,
    missingRollbackEvidence,
    tombstoneTimestampInvalid,
    tombstonePolicyMissing,
    rollbackEvidenceMissing,
    preconditionMismatch,
    postconditionMismatch,
    applyFailureBeforeCommit,
    applyFailureAfterPartialCommit,
    rollbackFailure,
    resurrectionRiskDetected,
    physicalDeleteAttempted,
    permanentDeleteAttempted,
    tombstoneGCAttempted,
    unsupportedRestore,
    conflictPolicyAmbiguous,
    generatedArtifactTombstoneUnsupported,
    missingInternalCanaryConfiguration,
    canaryBudgetAboveOneDenied,
    missingCanaryStageEvidence,
    canaryStageBlocked,
    canaryStageOrderViolation,
    previousStageFailure,
    previousStageRollbackFailure
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictApplyPortMode
{
    disabled,
    dryRun,
    fakeInMemory,
    testRootBound,
    productionRootDisabled,
    productionRootBound
}

public static class CanonicalTombstoneConflictApplyPortModeExtensions
{
    public static bool IsNonDryRunRootBound(this CanonicalTombstoneConflictApplyPortMode m) =>
        m == CanonicalTombstoneConflictApplyPortMode.testRootBound
        || m == CanonicalTombstoneConflictApplyPortMode.productionRootBound;
}

public sealed record CanonicalTombstoneConflictCandidate : IEquatable<CanonicalTombstoneConflictCandidate>
{
    public string Id => Action.ActionID;
    public CanonicalApplyAction Action { get; }
    public CanonicalTombstone? RecordingTombstone { get; }
    public CanonicalLibraryTombstone? LibraryTombstone { get; }
    public CanonicalConflictRecord? Conflict { get; }
    public CanonicalLibraryConflict? LibraryConflict { get; }
    public CanonicalRecordingObject? LocalRecordingObject { get; }
    public CanonicalRecordingObject? PeerRecordingObject { get; }
    public CanonicalLibraryObject? LocalLibraryObject { get; }
    public CanonicalLibraryObject? PeerLibraryObject { get; }
    public string? RollbackCheckpointID { get; }
    public bool TombstoneWinsIfNewerPolicy { get; }
    public bool RollbackEvidenceAvailable { get; }
    public bool ExplicitRestoreSignal { get; }
    public bool StaleLiveMetadataRisk { get; }
    public bool ConflictPolicyKnown { get; }
    public string RoutePath { get; }

    public CanonicalTombstoneConflictCandidate(
        CanonicalApplyAction action,
        CanonicalTombstone? recordingTombstone = null,
        CanonicalLibraryTombstone? libraryTombstone = null,
        CanonicalConflictRecord? conflict = null,
        CanonicalLibraryConflict? libraryConflict = null,
        CanonicalRecordingObject? localRecordingObject = null,
        CanonicalRecordingObject? peerRecordingObject = null,
        CanonicalLibraryObject? localLibraryObject = null,
        CanonicalLibraryObject? peerLibraryObject = null,
        string? rollbackCheckpointID = null,
        bool tombstoneWinsIfNewerPolicy = false,
        bool rollbackEvidenceAvailable = false,
        bool explicitRestoreSignal = false,
        bool staleLiveMetadataRisk = false,
        bool conflictPolicyKnown = true,
        string routePath = "/sync/apply-metadata")
    {
        Action = action;
        RecordingTombstone = recordingTombstone;
        LibraryTombstone = libraryTombstone;
        Conflict = conflict;
        LibraryConflict = libraryConflict;
        LocalRecordingObject = localRecordingObject;
        PeerRecordingObject = peerRecordingObject;
        LocalLibraryObject = localLibraryObject;
        PeerLibraryObject = peerLibraryObject;
        RollbackCheckpointID = rollbackCheckpointID != null
            ? CanonicalProductionRedaction.SafeIdentifier(rollbackCheckpointID, "tombstone-conflict-checkpoint") : null;
        TombstoneWinsIfNewerPolicy = tombstoneWinsIfNewerPolicy;
        RollbackEvidenceAvailable = rollbackEvidenceAvailable;
        ExplicitRestoreSignal = explicitRestoreSignal;
        StaleLiveMetadataRisk = staleLiveMetadataRisk;
        ConflictPolicyKnown = conflictPolicyKnown;
        RoutePath = CanonicalProductionRedaction.SafeDiagnosticText(routePath) ?? "/sync/apply-metadata";
    }

    public string ObjectID => Action.Target.ObjectID;

    public CanonicalTombstoneConflictDomain Domain => ActionKind switch
    {
        CanonicalTombstoneConflictActionKind.objectTombstoneApply => CanonicalTombstoneConflictDomain.objectTombstone,
        CanonicalTombstoneConflictActionKind.objectTombstoneSend => CanonicalTombstoneConflictDomain.objectTombstone,
        CanonicalTombstoneConflictActionKind.libraryTombstoneApply => CanonicalTombstoneConflictDomain.libraryTombstone,
        CanonicalTombstoneConflictActionKind.libraryTombstoneSend => CanonicalTombstoneConflictDomain.libraryTombstone,
        CanonicalTombstoneConflictActionKind.generatedArtifactTombstoneMarkUnsupported => CanonicalTombstoneConflictDomain.generatedArtifactTombstoneMarker,
        CanonicalTombstoneConflictActionKind.resurrectionBlocked => CanonicalTombstoneConflictDomain.activeVsTombstoneConflict,
        CanonicalTombstoneConflictActionKind.conflictRecord => Conflict?.Kind == CanonicalConflictKind.activeVsTombstone
            || LibraryConflict?.Kind == CanonicalLibraryConflictKind.activeVsTombstone
                ? CanonicalTombstoneConflictDomain.activeVsTombstoneConflict
                : Conflict?.Kind == CanonicalConflictKind.generatedArtifactContentMismatch
                    || Action.Target.ArtifactKind != null
                    ? CanonicalTombstoneConflictDomain.artifactConflictRecord
                    : CanonicalTombstoneConflictDomain.metadataConflictRecord,
        _ => CanonicalTombstoneConflictDomain.metadataConflictRecord
    };

    public CanonicalTombstoneConflictActionKind ActionKind => Action.Kind switch
    {
        CanonicalApplyActionKind.objectTombstoneApply => CanonicalTombstoneConflictActionKind.objectTombstoneApply,
        CanonicalApplyActionKind.objectTombstoneSend => CanonicalTombstoneConflictActionKind.objectTombstoneSend,
        CanonicalApplyActionKind.libraryTombstoneApply => CanonicalTombstoneConflictActionKind.libraryTombstoneApply,
        CanonicalApplyActionKind.libraryTombstoneSend => CanonicalTombstoneConflictActionKind.libraryTombstoneSend,
        CanonicalApplyActionKind.artifactTombstoneApply => CanonicalTombstoneConflictActionKind.generatedArtifactTombstoneMarkUnsupported,
        CanonicalApplyActionKind.conflictRecord => CanonicalTombstoneConflictActionKind.conflictRecord,
        CanonicalApplyActionKind.deferredUnsupported when Action.FailureReason == CanonicalApplyFailureReason.tombstoneBlocksResurrection =>
            CanonicalTombstoneConflictActionKind.resurrectionBlocked,
        _ => CanonicalTombstoneConflictActionKind.unsupported
    };

    public CanonicalTombstoneState TombstoneState =>
        RecordingTombstone?.State == CanonicalTombstoneState.tombstoned
        || LibraryTombstone != null || ActionKind.IsTombstoneMarkerWrite()
            ? CanonicalTombstoneState.tombstoned : CanonicalTombstoneState.active;

    public CanonicalTimestamp? DeletedAt =>
        RecordingTombstone?.DeletedAt
        ?? LibraryTombstone?.DeletedAt
        ?? LocalRecordingObject?.Metadata?.DeletedAt
        ?? PeerRecordingObject?.Metadata?.DeletedAt
        ?? LocalLibraryObject?.DeletedAt
        ?? PeerLibraryObject?.DeletedAt;

    public string DeletedAtSummary
    {
        get
        {
            if (DeletedAt == null) return "deletedAt=missing";
            var value = ((long)(DeletedAt.Value.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            return $"deletedAtHash={CanonicalProductionRedaction.HashPrefix(CanonicalHash.Sha256String(value).Value) ?? "missing"}";
        }
    }

    public string ConflictKindSummary =>
        Conflict?.Kind.ToString() ?? LibraryConflict?.Kind.ToString()
        ?? (ActionKind == CanonicalTombstoneConflictActionKind.resurrectionBlocked ? "resurrectionBlocked" : "none");

    public string ConflictPolicySummary =>
        Conflict?.ResolutionPolicy.ToString()
        ?? (LibraryConflict != null ? "manualReview"
            : (ActionKind == CanonicalTombstoneConflictActionKind.resurrectionBlocked ? "tombstoneRequiresManualReview" : "none"));

    public string EffectiveRollbackCheckpointID =>
        RollbackCheckpointID ?? $"tombstone-conflict-{ObjectID}-{ActionKind}";

    public bool HasActiveVsTombstoneConflict =>
        Conflict?.Kind == CanonicalConflictKind.activeVsTombstone
        || LibraryConflict?.Kind == CanonicalLibraryConflictKind.activeVsTombstone;

    public bool WouldRestoreFromAbsenceOnly =>
        !ExplicitRestoreSignal && ActionKind == CanonicalTombstoneConflictActionKind.conflictRecord
        && !HasActiveVsTombstoneConflict && TombstoneState == CanonicalTombstoneState.active
        && (LocalRecordingObject?.Metadata?.IsDeleted == true || PeerRecordingObject?.Metadata?.IsDeleted == true
            || LocalLibraryObject?.IsDeleted == true || PeerLibraryObject?.IsDeleted == true);

    public virtual bool Equals(CanonicalTombstoneConflictCandidate? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalTombstoneConflictCutoverEvidence : IEquatable<CanonicalTombstoneConflictCutoverEvidence>
{
    public bool NoCommitEvidenceAvailable { get; }
    public bool RealDataShadowCopyVerified { get; }
    public bool ExecutionShadowVerified { get; }
    public bool DryRunEquivalenceVerified { get; }
    public bool NoBlockingDivergence { get; }
    public bool NoUnresolvedConflict { get; }
    public bool MetadataRouteEvidenceAvailable { get; }
    public bool ProductionPortAvailable { get; }
    public bool RealRootBoundApplyPortAvailable { get; }
    public CanonicalTombstoneConflictApplyPortMode ApplyPortMode { get; }
    public bool RootBoundWriteAvailable { get; }
    public bool AtomicReplaceAvailable { get; }
    public bool RollbackCheckpointAvailable { get; }
    public bool RollbackVerified { get; }
    public bool ProductionRootDisabledByDefault { get; }
    public bool TestRootUsed { get; }
    public bool SoftTombstoneStoreSupported { get; }
    public bool ConflictLedgerSupported { get; }
    public bool TombstoneWinsIfNewerPolicyAvailable { get; }
    public bool RollbackEvidenceAvailable { get; }
    public bool LegacyFallbackAvailable { get; }
    public CanonicalRollbackPlan? RollbackPlan { get; }
    public bool ReadSideParallelEquivalent { get; }
    public CanonicalTombstoneConflictCanaryStageEvidence? CanaryStageEvidence { get; }

    public CanonicalTombstoneConflictCutoverEvidence(
        bool noCommitEvidenceAvailable = false, bool realDataShadowCopyVerified = false,
        bool executionShadowVerified = false, bool dryRunEquivalenceVerified = false,
        bool noBlockingDivergence = false, bool noUnresolvedConflict = false,
        bool metadataRouteEvidenceAvailable = false, bool productionPortAvailable = false,
        bool realRootBoundApplyPortAvailable = false,
        CanonicalTombstoneConflictApplyPortMode applyPortMode = CanonicalTombstoneConflictApplyPortMode.disabled,
        bool rootBoundWriteAvailable = false, bool atomicReplaceAvailable = false,
        bool rollbackCheckpointAvailable = false, bool rollbackVerified = false,
        bool productionRootDisabledByDefault = false, bool testRootUsed = false,
        bool softTombstoneStoreSupported = false, bool conflictLedgerSupported = false,
        bool tombstoneWinsIfNewerPolicyAvailable = false, bool rollbackEvidenceAvailable = false,
        bool legacyFallbackAvailable = false, CanonicalRollbackPlan? rollbackPlan = null,
        bool readSideParallelEquivalent = false,
        CanonicalTombstoneConflictCanaryStageEvidence? canaryStageEvidence = null)
    {
        NoCommitEvidenceAvailable = noCommitEvidenceAvailable;
        RealDataShadowCopyVerified = realDataShadowCopyVerified;
        ExecutionShadowVerified = executionShadowVerified;
        DryRunEquivalenceVerified = dryRunEquivalenceVerified;
        NoBlockingDivergence = noBlockingDivergence;
        NoUnresolvedConflict = noUnresolvedConflict;
        MetadataRouteEvidenceAvailable = metadataRouteEvidenceAvailable;
        ProductionPortAvailable = productionPortAvailable;
        RealRootBoundApplyPortAvailable = realRootBoundApplyPortAvailable;
        ApplyPortMode = applyPortMode;
        RootBoundWriteAvailable = rootBoundWriteAvailable;
        AtomicReplaceAvailable = atomicReplaceAvailable;
        RollbackCheckpointAvailable = rollbackCheckpointAvailable;
        RollbackVerified = rollbackVerified;
        ProductionRootDisabledByDefault = productionRootDisabledByDefault;
        TestRootUsed = testRootUsed;
        SoftTombstoneStoreSupported = softTombstoneStoreSupported;
        ConflictLedgerSupported = conflictLedgerSupported;
        TombstoneWinsIfNewerPolicyAvailable = tombstoneWinsIfNewerPolicyAvailable;
        RollbackEvidenceAvailable = rollbackEvidenceAvailable;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        RollbackPlan = rollbackPlan;
        ReadSideParallelEquivalent = readSideParallelEquivalent;
        CanaryStageEvidence = canaryStageEvidence;
    }

    public static CanonicalTombstoneConflictCutoverEvidence Passing(CanonicalRollbackPlan rollbackPlan) =>
        new(
            noCommitEvidenceAvailable: true, realDataShadowCopyVerified: true, executionShadowVerified: true,
            dryRunEquivalenceVerified: true, noBlockingDivergence: true, noUnresolvedConflict: true,
            metadataRouteEvidenceAvailable: true, productionPortAvailable: true, realRootBoundApplyPortAvailable: true,
            applyPortMode: CanonicalTombstoneConflictApplyPortMode.testRootBound,
            rootBoundWriteAvailable: true, atomicReplaceAvailable: true,
            rollbackCheckpointAvailable: true, rollbackVerified: true,
            productionRootDisabledByDefault: true, testRootUsed: true,
            softTombstoneStoreSupported: true, conflictLedgerSupported: true,
            tombstoneWinsIfNewerPolicyAvailable: true, rollbackEvidenceAvailable: true,
            legacyFallbackAvailable: true, rollbackPlan: rollbackPlan, readSideParallelEquivalent: true);

    public virtual bool Equals(CanonicalTombstoneConflictCutoverEvidence? other) =>
        other is not null && ApplyPortMode == other.ApplyPortMode;
    public override int GetHashCode() => ApplyPortMode.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictCanaryStage
{
    disabled,
    n1,
    n3,
    n10,
    allEligible
}

public static class CanonicalTombstoneConflictCanaryStageExtensions
{
    public static bool IsExecutable(this CanonicalTombstoneConflictCanaryStage s) =>
        s != CanonicalTombstoneConflictCanaryStage.disabled;

    public static CanonicalTombstoneConflictCanaryStage? PreviousStage(this CanonicalTombstoneConflictCanaryStage s) => s switch
    {
        CanonicalTombstoneConflictCanaryStage.disabled => null,
        CanonicalTombstoneConflictCanaryStage.n1 => CanonicalTombstoneConflictCanaryStage.disabled,
        CanonicalTombstoneConflictCanaryStage.n3 => CanonicalTombstoneConflictCanaryStage.n1,
        CanonicalTombstoneConflictCanaryStage.n10 => CanonicalTombstoneConflictCanaryStage.n3,
        CanonicalTombstoneConflictCanaryStage.allEligible => CanonicalTombstoneConflictCanaryStage.n10,
        _ => null
    };

    public static int NominalBudget(this CanonicalTombstoneConflictCanaryStage s) => s switch
    {
        CanonicalTombstoneConflictCanaryStage.disabled => 0,
        CanonicalTombstoneConflictCanaryStage.n1 => 1,
        CanonicalTombstoneConflictCanaryStage.n3 => 3,
        CanonicalTombstoneConflictCanaryStage.n10 => 10,
        CanonicalTombstoneConflictCanaryStage.allEligible => int.MaxValue,
        _ => 0
    };

    public static int MinimumPreviousStageSuccessCount(this CanonicalTombstoneConflictCanaryStage s) => s switch
    {
        CanonicalTombstoneConflictCanaryStage.disabled => 0,
        CanonicalTombstoneConflictCanaryStage.n1 => 0,
        CanonicalTombstoneConflictCanaryStage.n3 => 1,
        CanonicalTombstoneConflictCanaryStage.n10 => 3,
        CanonicalTombstoneConflictCanaryStage.allEligible => 10,
        _ => 0
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictStageEvidenceStatus
{
    missing,
    incomplete,
    passed,
    failed,
    blocked
}

public sealed record CanonicalTombstoneConflictCanaryStageEvidence : IEquatable<CanonicalTombstoneConflictCanaryStageEvidence>
{
    public CanonicalTombstoneConflictCanaryStage Stage { get; }
    public CanonicalTombstoneConflictCanaryStage? PreviousStage { get; }
    public CanonicalTombstoneConflictStageEvidenceStatus Status { get; }
    public int SuccessfulCommitCount { get; }
    public int FailedCommitCount { get; }
    public int RollbackFailureCount { get; }
    public int ResurrectionRiskCount { get; }
    public int PhysicalDeleteAttemptCount { get; }
    public int PermanentDeleteAttemptCount { get; }
    public int TombstoneGCAttemptCount { get; }
    public int ConflictAmbiguityCount { get; }
    public bool NoCommitEvidenceAvailable { get; }
    public bool ObservationWindowComplete { get; }

    public CanonicalTombstoneConflictCanaryStageEvidence(
        CanonicalTombstoneConflictCanaryStage stage,
        CanonicalTombstoneConflictCanaryStage? previousStage = null,
        CanonicalTombstoneConflictStageEvidenceStatus status = CanonicalTombstoneConflictStageEvidenceStatus.missing,
        int successfulCommitCount = 0, int failedCommitCount = 0, int rollbackFailureCount = 0,
        int resurrectionRiskCount = 0, int physicalDeleteAttemptCount = 0, int permanentDeleteAttemptCount = 0,
        int tombstoneGCAttemptCount = 0, int conflictAmbiguityCount = 0,
        bool noCommitEvidenceAvailable = false, bool observationWindowComplete = false)
    {
        Stage = stage;
        PreviousStage = previousStage;
        Status = status;
        SuccessfulCommitCount = Math.Max(0, successfulCommitCount);
        FailedCommitCount = Math.Max(0, failedCommitCount);
        RollbackFailureCount = Math.Max(0, rollbackFailureCount);
        ResurrectionRiskCount = Math.Max(0, resurrectionRiskCount);
        PhysicalDeleteAttemptCount = Math.Max(0, physicalDeleteAttemptCount);
        PermanentDeleteAttemptCount = Math.Max(0, permanentDeleteAttemptCount);
        TombstoneGCAttemptCount = Math.Max(0, tombstoneGCAttemptCount);
        ConflictAmbiguityCount = Math.Max(0, conflictAmbiguityCount);
        NoCommitEvidenceAvailable = noCommitEvidenceAvailable;
        ObservationWindowComplete = observationWindowComplete;
    }

    public static CanonicalTombstoneConflictCanaryStageEvidence Passing(
        CanonicalTombstoneConflictCanaryStage stage, int successfulCommitCount) =>
        new(stage, stage.PreviousStage(), CanonicalTombstoneConflictStageEvidenceStatus.passed,
            successfulCommitCount, noCommitEvidenceAvailable: true, observationWindowComplete: true);

    public virtual bool Equals(CanonicalTombstoneConflictCanaryStageEvidence? other) =>
        other is not null && Stage == other.Stage;
    public override int GetHashCode() => Stage.GetHashCode();
}

public sealed record CanonicalTombstoneConflictCanaryPolicy : IEquatable<CanonicalTombstoneConflictCanaryPolicy>
{
    public CanonicalTombstoneConflictCanaryStage RequestedStage { get; }
    public int CanaryMaxObjectsPerSyncRun { get; }
    public bool AllowCandidateExecution { get; }
    public bool AllowsInternalN1Execution { get; }
    public bool ExplicitInternalTestConfiguration { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool AllowAllEligible { get; }

    public CanonicalTombstoneConflictCanaryPolicy(
        CanonicalTombstoneConflictCanaryStage requestedStage = CanonicalTombstoneConflictCanaryStage.disabled,
        int canaryMaxObjectsPerSyncRun = 0, bool allowCandidateExecution = false,
        bool allowsInternalN1Execution = false, bool explicitInternalTestConfiguration = false,
        bool runtimeSwitchEnabled = false, bool allowAllEligible = false)
    {
        RequestedStage = requestedStage;
        CanaryMaxObjectsPerSyncRun = Math.Max(0, canaryMaxObjectsPerSyncRun);
        AllowCandidateExecution = allowCandidateExecution;
        AllowsInternalN1Execution = allowsInternalN1Execution;
        ExplicitInternalTestConfiguration = explicitInternalTestConfiguration;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        AllowAllEligible = allowAllEligible;
    }

    public static readonly CanonicalTombstoneConflictCanaryPolicy Disabled = new();
    public int Budget => RequestedStage.IsExecutable() ? RequestedStage.NominalBudget() : CanaryMaxObjectsPerSyncRun;

    public virtual bool Equals(CanonicalTombstoneConflictCanaryPolicy? other) =>
        other is not null && RequestedStage == other.RequestedStage;
    public override int GetHashCode() => RequestedStage.GetHashCode();
}

public sealed record CanonicalTombstoneConflictCutoverGate : IEquatable<CanonicalTombstoneConflictCutoverGate>
{
    public CanonicalCutoverMode Mode { get; }
    public bool Allowed { get; }
    public CanonicalTombstoneConflictFailure[] Failures { get; }
    public bool LegacyFallbackAvailable { get; }
    public string Reason { get; }

    public CanonicalTombstoneConflictCutoverGate(
        CanonicalCutoverMode mode,
        CanonicalTombstoneConflictFailure[] failures,
        bool legacyFallbackAvailable,
        string reason)
    {
        Mode = mode;
        Failures = new HashSet<CanonicalTombstoneConflictFailure>(failures)
            .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray();
        Allowed = Failures.Length == 0;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (Allowed ? "allowed" : "blocked");
    }

    public virtual bool Equals(CanonicalTombstoneConflictCutoverGate? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

public sealed record CanonicalTombstoneConflictCutoverResult : IEquatable<CanonicalTombstoneConflictCutoverResult>
{
    public CanonicalTombstoneConflictCutoverGate Gate { get; set; }
    public CanonicalTombstoneConflictProductionCommitResult[] Commits { get; set; }
    public CanonicalTombstoneConflictRollbackExecutionResult[] RollbackResults { get; set; }
    public CanonicalTombstoneConflictCutoverDiagnostic[] Diagnostics { get; set; }
    public bool LegacyFallbackUsed { get; set; }
    public string[] DuplicateLegacySuppressedActionIDs { get; set; }
    public int CanaryAttemptedCount { get; set; }
    public bool CanarySucceeded { get; set; }
    public bool FatalBlocker { get; set; }
    public CanonicalTombstoneConflictReadSideParallelProjectionResult? ReadSideProjection { get; set; }

    public bool Succeeded => Gate.Allowed && !FatalBlocker
        && Commits.Length > 0 && Commits.All(c => c.Committed && c.PreconditionVerified && c.PostconditionVerified);

    public CanonicalTombstoneConflictCutoverResult()
    {
        Gate = default!;
        Commits = Array.Empty<CanonicalTombstoneConflictProductionCommitResult>();
        RollbackResults = Array.Empty<CanonicalTombstoneConflictRollbackExecutionResult>();
        Diagnostics = Array.Empty<CanonicalTombstoneConflictCutoverDiagnostic>();
        DuplicateLegacySuppressedActionIDs = Array.Empty<string>();
    }

    public virtual bool Equals(CanonicalTombstoneConflictCutoverResult? other) =>
        other is not null && CanarySucceeded == other.CanarySucceeded;
    public override int GetHashCode() => CanarySucceeded.GetHashCode();
}

public sealed record CanonicalTombstoneConflictProductionCommitResult : IEquatable<CanonicalTombstoneConflictProductionCommitResult>
{
    public string ActionID { get; }
    public string ObjectID { get; }
    public CanonicalTombstoneConflictDomain Domain { get; }
    public CanonicalTombstoneConflictActionKind ActionKind { get; }
    public bool Committed { get; }
    public bool PartialCommit { get; }
    public bool PreconditionVerified { get; }
    public bool PostconditionVerified { get; }
    public CanonicalTombstoneState TombstoneState { get; }
    public string DeletedAtSummary { get; }
    public string ConflictKind { get; }
    public string ConflictPolicy { get; }
    public bool ProductionCommitSuppressed { get; }
    public bool PhysicalDeleteSuppressed { get; }
    public bool PermanentDeleteSuppressed { get; }
    public bool TombstoneGCSuppressed { get; }
    public bool GeneratedArtifactDownloadBlocked { get; }
    public bool ReceiveJSONMutated { get; }
    public bool AudioTranscriptNoteSummaryDeleted { get; }
    public CanonicalTombstoneConflictFailure? FailureKind { get; }
    public string Reason { get; }

    public CanonicalTombstoneConflictProductionCommitResult(
        string actionID, string objectID, CanonicalTombstoneConflictDomain domain,
        CanonicalTombstoneConflictActionKind actionKind, bool committed,
        bool partialCommit = false, bool preconditionVerified = true, bool postconditionVerified = true,
        CanonicalTombstoneState tombstoneState = CanonicalTombstoneState.active,
        string deletedAtSummary = "", string conflictKind = "", string conflictPolicy = "",
        bool productionCommitSuppressed = false, bool physicalDeleteSuppressed = true,
        bool permanentDeleteSuppressed = true, bool tombstoneGCSuppressed = true,
        bool generatedArtifactDownloadBlocked = true, bool receiveJSONMutated = false,
        bool audioTranscriptNoteSummaryDeleted = false,
        CanonicalTombstoneConflictFailure? failureKind = null, string reason = "")
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, actionKind.ToString());
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "tombstone-object");
        Domain = domain;
        ActionKind = actionKind;
        Committed = committed;
        PartialCommit = partialCommit;
        PreconditionVerified = preconditionVerified;
        PostconditionVerified = postconditionVerified;
        TombstoneState = tombstoneState;
        DeletedAtSummary = CanonicalProductionRedaction.SafeDiagnosticText(deletedAtSummary) ?? "deletedAt=missing";
        ConflictKind = CanonicalProductionRedaction.SafeDiagnosticText(conflictKind) ?? "none";
        ConflictPolicy = CanonicalProductionRedaction.SafeDiagnosticText(conflictPolicy) ?? "none";
        ProductionCommitSuppressed = productionCommitSuppressed;
        PhysicalDeleteSuppressed = physicalDeleteSuppressed;
        PermanentDeleteSuppressed = permanentDeleteSuppressed;
        TombstoneGCSuppressed = tombstoneGCSuppressed;
        GeneratedArtifactDownloadBlocked = generatedArtifactDownloadBlocked;
        ReceiveJSONMutated = receiveJSONMutated;
        AudioTranscriptNoteSummaryDeleted = audioTranscriptNoteSummaryDeleted;
        FailureKind = failureKind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (committed ? "committed" : "failed");
    }

    public virtual bool Equals(CanonicalTombstoneConflictProductionCommitResult? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
}

public sealed record CanonicalTombstoneConflictRollbackExecutionResult : IEquatable<CanonicalTombstoneConflictRollbackExecutionResult>
{
    public string CheckpointID { get; }
    public bool Succeeded { get; }
    public bool Fatal { get; }
    public string Reason { get; }

    public CanonicalTombstoneConflictRollbackExecutionResult(
        string checkpointID, bool succeeded, bool fatal = false, string reason = "")
    {
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "tombstone-conflict-checkpoint");
        Succeeded = succeeded;
        Fatal = fatal;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (succeeded ? "rollbackCompleted" : "rollbackFailed");
    }

    public virtual bool Equals(CanonicalTombstoneConflictRollbackExecutionResult? other) =>
        other is not null && CheckpointID == other.CheckpointID;
    public override int GetHashCode() => CheckpointID.GetHashCode();
}

public sealed record CanonicalTombstoneConflictReadSideParallelProjectionResult : IEquatable<CanonicalTombstoneConflictReadSideParallelProjectionResult>
{
    public string ObjectID { get; }
    public CanonicalTombstoneConflictDomain Domain { get; }
    public bool Equivalent { get; }
    public bool MutatedUI { get; }
    public CanonicalTombstoneState CanonicalTombstoneState { get; }
    public CanonicalTombstoneState LegacyDeletedState { get; }
    public bool ConflictRecorded { get; }
    public string AntiResurrectionStatus { get; }
    public bool SyncOrUploadTriggered { get; }
    public string Reason { get; }

    public CanonicalTombstoneConflictReadSideParallelProjectionResult(
        CanonicalTombstoneConflictCandidate candidate, bool equivalent, bool conflictRecorded, string reason)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(candidate.ObjectID, "tombstone-object");
        Domain = candidate.Domain;
        Equivalent = equivalent;
        MutatedUI = false;
        CanonicalTombstoneState = candidate.TombstoneState;
        LegacyDeletedState = candidate.TombstoneState;
        ConflictRecorded = conflictRecorded;
        AntiResurrectionStatus = candidate.ActionKind == CanonicalTombstoneConflictActionKind.resurrectionBlocked
            ? "blocked" : "notTriggered";
        SyncOrUploadTriggered = false;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (equivalent ? "equivalent" : "divergent");
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadSideParallelProjectionResult? other) =>
        other is not null && ObjectID == other.ObjectID;
    public override int GetHashCode() => ObjectID.GetHashCode();
}

public sealed record CanonicalTombstoneConflictCutoverAppSeamConfiguration : IEquatable<CanonicalTombstoneConflictCutoverAppSeamConfiguration>
{
    public bool IsEnabled { get; }
    public CanonicalCutoverAppSeamMode Mode { get; }
    public CanonicalTombstoneConflictCutoverAppSeamPolicy Policy { get; }
    public CanonicalTombstoneConflictCutoverEvidence Evidence { get; }
    public CanonicalCutoverToken? CutoverToken { get; }

    public CanonicalTombstoneConflictCutoverAppSeamConfiguration(
        bool isEnabled = false,
        CanonicalCutoverAppSeamMode mode = CanonicalCutoverAppSeamMode.disabled,
        CanonicalTombstoneConflictCutoverAppSeamPolicy? policy = null,
        CanonicalTombstoneConflictCutoverEvidence? evidence = null,
        CanonicalCutoverToken? cutoverToken = null)
    {
        IsEnabled = isEnabled;
        Mode = isEnabled ? mode : CanonicalCutoverAppSeamMode.disabled;
        Policy = policy ?? new CanonicalTombstoneConflictCutoverAppSeamPolicy();
        Evidence = evidence ?? new CanonicalTombstoneConflictCutoverEvidence();
        CutoverToken = cutoverToken;
    }

    public static readonly CanonicalTombstoneConflictCutoverAppSeamConfiguration Disabled = new();
    public CanonicalCutoverAppSeamMode EffectiveMode => IsEnabled ? Mode : CanonicalCutoverAppSeamMode.disabled;

    public virtual bool Equals(CanonicalTombstoneConflictCutoverAppSeamConfiguration? other) =>
        other is not null && IsEnabled == other.IsEnabled;
    public override int GetHashCode() => IsEnabled.GetHashCode();
}

public sealed record CanonicalTombstoneConflictCutoverAppSeamPolicy : IEquatable<CanonicalTombstoneConflictCutoverAppSeamPolicy>
{
    public bool RecordDiagnostics { get; }
    public int MaxDiagnosticsEvents { get; }
    public CanonicalTombstoneConflictCanaryPolicy CanaryPolicy { get; }

    public CanonicalTombstoneConflictCutoverAppSeamPolicy(
        bool recordDiagnostics = true, int maxDiagnosticsEvents = 200,
        CanonicalTombstoneConflictCanaryPolicy? canaryPolicy = null)
    {
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
        CanaryPolicy = canaryPolicy ?? CanonicalTombstoneConflictCanaryPolicy.Disabled;
    }

    public virtual bool Equals(CanonicalTombstoneConflictCutoverAppSeamPolicy? other) =>
        other is not null && RecordDiagnostics == other.RecordDiagnostics;
    public override int GetHashCode() => RecordDiagnostics.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictCutoverDiagnosticKind
{
    canonicalTombstoneCutoverGateEvaluated,
    canonicalTombstoneCutoverGateBlocked,
    canonicalTombstoneNoCommitStarted,
    canonicalTombstoneNoCommitCompleted,
    canonicalTombstoneCommitStarted,
    canonicalTombstoneCommitCompleted,
    canonicalTombstoneCommitFailed,
    canonicalTombstoneRollbackStarted,
    canonicalTombstoneRollbackCompleted,
    canonicalTombstoneRollbackFailed,
    canonicalTombstoneCanaryStarted,
    canonicalTombstoneCanaryCompleted,
    canonicalTombstoneDuplicateLegacySuppressed,
    canonicalTombstoneLegacyFallbackUsed,
    canonicalTombstonePhysicalDeleteBlocked,
    canonicalTombstonePermanentDeleteBlocked,
    canonicalTombstoneGCBlocked,
    canonicalTombstoneResurrectionBlocked,
    canonicalTombstoneResurrectionRiskDetected,
    canonicalConflictRecordCommitted,
    canonicalConflictPolicyAmbiguousBlocked,
    canonicalTombstoneUIProjectionParallelReadStarted,
    canonicalTombstoneUIProjectionParallelReadEquivalent,
    canonicalTombstoneUIProjectionParallelReadDivergent,
    canonicalConflictUIProjectionParallelReadStarted,
    canonicalConflictUIProjectionParallelReadEquivalent,
    canonicalConflictUIProjectionParallelReadDivergent
}

public sealed record CanonicalTombstoneConflictCutoverDiagnostic : IEquatable<CanonicalTombstoneConflictCutoverDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", Result ?? "", Reason ?? "");

    public CanonicalTombstoneConflictCutoverDiagnosticKind Kind { get; }
    public string? SyncRunID { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; }
    public CanonicalTombstoneConflictDomain? Domain { get; }
    public string? ObjectID { get; }
    public string? Action { get; }
    public CanonicalTombstoneState? TombstoneState { get; }
    public string? ConflictKind { get; }
    public string? Result { get; }
    public string? Reason { get; }
    public string? HashPrefix { get; }

    public CanonicalTombstoneConflictCutoverDiagnostic(
        CanonicalTombstoneConflictCutoverDiagnosticKind kind,
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalTombstoneConflictDomain? domain = null, string? objectID = null,
        string? action = null, CanonicalTombstoneState? tombstoneState = null,
        string? conflictKind = null, string? result = null,
        string? reason = null, CanonicalHash? hash = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Domain = domain;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "tombstone-object") : null;
        Action = CanonicalProductionRedaction.SafeDiagnosticText(action);
        TombstoneState = tombstoneState;
        ConflictKind = CanonicalProductionRedaction.SafeDiagnosticText(conflictKind);
        Result = CanonicalProductionRedaction.SafeDiagnosticText(result);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = hash != null ? CanonicalProductionRedaction.HashPrefix(hash.Value) : null;
    }

    public virtual bool Equals(CanonicalTombstoneConflictCutoverDiagnostic? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}
