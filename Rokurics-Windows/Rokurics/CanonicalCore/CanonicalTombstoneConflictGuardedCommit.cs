using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictPilotActivationBlocker
{
    templateNotReadyForNextPilotN0,
    generatedArtifactsTemplateMissing,
    libraryMetadataObservationMissing,
    matrixValidationBlocked,
    activePilotNotTombstoneConflict,
    canaryN0NotReached,
    canaryN1Reached,
    releaseDefaultCutoverEnabled,
    runtimeSwitchEnabled,
    legacySuppressionEnabled,
    readPathNotLegacy,
    productionInjectionPresent
}

public sealed record CanonicalTombstoneConflictPilotActivationResult : IEquatable<CanonicalTombstoneConflictPilotActivationResult>
{
    public bool Activated { get; }
    public CanonicalMigrationDomainMatrix Matrix { get; }
    public CanonicalMigrationMatrixReport MatrixReport { get; }
    public CanonicalTombstoneConflictPilotActivationBlocker[] Blockers { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalTombstoneConflictPilotActivationResult(
        CanonicalMigrationDomainMatrix matrix,
        CanonicalTombstoneConflictPilotActivationBlocker[] blockers)
    {
        var matrixReport = matrix.Validate();
        var normalizedBlockers = new HashSet<CanonicalTombstoneConflictPilotActivationBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        Matrix = matrix;
        MatrixReport = matrixReport;
        Blockers = normalizedBlockers;
        Activated = normalizedBlockers.Length == 0 && matrixReport.Allowed
                    && matrixReport.ActivePilotDomain == CanonicalMigrationDomain.tombstoneConflict;
        DiagnosticsSummary = string.Join(",",
            "domain=tombstoneConflict", "version=v8.27",
            $"activated={Activated}",
            $"activePilot={matrixReport.ActivePilotDomain?.ToString() ?? "none"}",
            $"matrixAllowed={matrixReport.Allowed}",
            $"blockers={string.Join("+", normalizedBlockers.Select(b => b.ToString()))}"
        );
    }

    public virtual bool Equals(CanonicalTombstoneConflictPilotActivationResult? other) =>
        other is not null && Activated == other.Activated;
    public override int GetHashCode() => Activated.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictGuardedEvidenceStatus
{
    complete,
    incomplete
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictGateResult
{
    blocked,
    allowedButCanaryBudgetZero,
    missingEvidence,
    unsupportedDomain,
    physicalDeleteBlocked,
    permanentDeleteBlocked,
    tombstoneGCBlocked,
    antiResurrectionBlocked,
    staleLiveResurrectionRisk,
    conflictPolicyAmbiguous,
    readyForN1AfterAudit
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictGuardedSeamFailure
{
    disabled,
    unsupportedMode,
    productionExecuteDenied,
    viewRefreshTriggerDenied,
    retryDrainerFreshTombstoneConflictDenied,
    insufficientLocalSnapshot,
    insufficientPeerSnapshot,
    matrixValidationBlocked,
    activePilotNotTombstoneConflict,
    unsupportedDomain,
    unsupportedAction,
    missingToken,
    missingOwnerApproval,
    missingNoCommitEvidence,
    missingRealDataShadowCopyEvidence,
    missingExecutionShadowEvidence,
    missingDryRunEquivalence,
    blockingDivergence,
    unresolvedConflict,
    missingMetadataRouteEvidence,
    productionPortUnavailable,
    realApplyPortUnavailable,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    missingRollback,
    rollbackVerificationMissing,
    productionRootEnabledByDefault,
    testRootMissing,
    softTombstoneStoreUnsupported,
    conflictLedgerUnsupported,
    missingTombstoneWinsPolicy,
    missingRollbackEvidence,
    missingTombstoneTimestamp,
    legacyFallbackUnavailable,
    commitExecutorUnavailable,
    missingFailureInjectionEvidence,
    missingReadSideParallel,
    missingObservationEvidence,
    missingAntiResurrectionGate,
    missingPhysicalDeleteGuard,
    missingPermanentDeleteGuard,
    missingTombstoneGCGuard,
    missingConflictConservativePolicy,
    physicalDeletePathDetected,
    permanentDeletePathDetected,
    tombstoneGCPathDetected,
    unsupportedRestore,
    staleLiveResurrectionRisk,
    conflictPolicyAmbiguous,
    generatedArtifactTombstonedParentApplyBlocked,
    duplicateSuppressionPolicyUnavailable,
    duplicateSuppressionPolicyEnabled,
    canaryBudgetNonZeroDenied,
    canaryStageExecutionDenied,
    runtimeSwitchDenied
}

public static class CanonicalTombstoneConflictGuardedSeamFailureExtensions
{
    public static bool IsEvidenceMissing(this CanonicalTombstoneConflictGuardedSeamFailure f) => f switch
    {
        CanonicalTombstoneConflictGuardedSeamFailure.missingNoCommitEvidence => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingRealDataShadowCopyEvidence => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingExecutionShadowEvidence => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingDryRunEquivalence => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingMetadataRouteEvidence => true,
        CanonicalTombstoneConflictGuardedSeamFailure.productionPortUnavailable => true,
        CanonicalTombstoneConflictGuardedSeamFailure.realApplyPortUnavailable => true,
        CanonicalTombstoneConflictGuardedSeamFailure.applyPortDryRunOnly => true,
        CanonicalTombstoneConflictGuardedSeamFailure.rootBoundWriteUnavailable => true,
        CanonicalTombstoneConflictGuardedSeamFailure.atomicReplaceUnavailable => true,
        CanonicalTombstoneConflictGuardedSeamFailure.rollbackCheckpointUnavailable => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingRollback => true,
        CanonicalTombstoneConflictGuardedSeamFailure.rollbackVerificationMissing => true,
        CanonicalTombstoneConflictGuardedSeamFailure.commitExecutorUnavailable => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingFailureInjectionEvidence => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingReadSideParallel => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingObservationEvidence => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingAntiResurrectionGate => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingPhysicalDeleteGuard => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingPermanentDeleteGuard => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingTombstoneGCGuard => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingConflictConservativePolicy => true,
        CanonicalTombstoneConflictGuardedSeamFailure.softTombstoneStoreUnsupported => true,
        CanonicalTombstoneConflictGuardedSeamFailure.conflictLedgerUnsupported => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingTombstoneWinsPolicy => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingRollbackEvidence => true,
        CanonicalTombstoneConflictGuardedSeamFailure.missingTombstoneTimestamp => true,
        _ => false
    };
}

public sealed record CanonicalTombstoneConflictGuardedGate : IEquatable<CanonicalTombstoneConflictGuardedGate>
{
    public CanonicalCutoverAppSeamMode Mode { get; }
    public bool Allowed { get; }
    public CanonicalTombstoneConflictGateResult Result { get; }
    public CanonicalTombstoneConflictGuardedSeamFailure[] Failures { get; }
    public string Reason { get; }

    public CanonicalTombstoneConflictGuardedGate(
        CanonicalCutoverAppSeamMode mode,
        CanonicalTombstoneConflictGuardedSeamFailure[] failures,
        bool canaryBudgetZero,
        string reason)
    {
        Mode = mode;
        Failures = new HashSet<CanonicalTombstoneConflictGuardedSeamFailure>(failures)
            .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray();
        Allowed = Failures.Length == 0;
        Result = GateResultFor(Failures, Allowed, canaryBudgetZero);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? Result.ToString();
    }

    private static CanonicalTombstoneConflictGateResult GateResultFor(
        CanonicalTombstoneConflictGuardedSeamFailure[] failures, bool allowed, bool canaryBudgetZero)
    {
        if (allowed && canaryBudgetZero) return CanonicalTombstoneConflictGateResult.allowedButCanaryBudgetZero;
        if (failures.Contains(CanonicalTombstoneConflictGuardedSeamFailure.physicalDeletePathDetected))
            return CanonicalTombstoneConflictGateResult.physicalDeleteBlocked;
        if (failures.Contains(CanonicalTombstoneConflictGuardedSeamFailure.permanentDeletePathDetected))
            return CanonicalTombstoneConflictGateResult.permanentDeleteBlocked;
        if (failures.Contains(CanonicalTombstoneConflictGuardedSeamFailure.tombstoneGCPathDetected))
            return CanonicalTombstoneConflictGateResult.tombstoneGCBlocked;
        if (failures.Contains(CanonicalTombstoneConflictGuardedSeamFailure.missingAntiResurrectionGate))
            return CanonicalTombstoneConflictGateResult.antiResurrectionBlocked;
        if (failures.Contains(CanonicalTombstoneConflictGuardedSeamFailure.staleLiveResurrectionRisk))
            return CanonicalTombstoneConflictGateResult.staleLiveResurrectionRisk;
        if (failures.Contains(CanonicalTombstoneConflictGuardedSeamFailure.conflictPolicyAmbiguous))
            return CanonicalTombstoneConflictGateResult.conflictPolicyAmbiguous;
        if (failures.Contains(CanonicalTombstoneConflictGuardedSeamFailure.unsupportedDomain)
            || failures.Contains(CanonicalTombstoneConflictGuardedSeamFailure.unsupportedAction))
            return CanonicalTombstoneConflictGateResult.unsupportedDomain;
        if (failures.Any(f => f.IsEvidenceMissing()))
            return CanonicalTombstoneConflictGateResult.missingEvidence;
        return CanonicalTombstoneConflictGateResult.blocked;
    }

    public virtual bool Equals(CanonicalTombstoneConflictGuardedGate? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictGuardedDiagnosticKind
{
    canonicalTombstoneConflictV827SeamStarted,
    canonicalTombstoneConflictV827SeamCompleted,
    canonicalTombstoneConflictV827SeamBlocked,
    canonicalTombstoneConflictV827GateEvaluated,
    canonicalTombstoneConflictV827GateAllowedBudgetZero,
    canonicalTombstoneConflictV827GateBlocked,
    canonicalTombstoneConflictV827CanaryBudgetZero,
    canonicalTombstoneConflictV827CommitNotExecuted,
    canonicalTombstoneConflictV827DeleteNotExecuted,
    canonicalTombstoneConflictV827RestoreNotExecuted,
    canonicalTombstoneConflictV827ConflictNotAutoResolved,
    canonicalTombstoneConflictV827LegacyFallbackPreserved,
    canonicalTombstoneConflictV827DuplicateSuppressionNotApplied,
    canonicalTombstoneConflictV827EvidenceReportBuilt,
    canonicalTombstoneConflictV827N1ReadinessReportBuilt,
    canonicalTombstoneConflictCanaryBudgetZero,
    canonicalTombstoneConflictGateAllowedButNoExecution,
    canonicalTombstoneConflictCommitSkippedBecauseCanaryBudgetZero,
    canonicalTombstoneConflictDeleteSkippedBecauseCanaryBudgetZero,
    canonicalTombstoneConflictRestoreSkippedBecauseCanaryBudgetZero,
    canonicalTombstoneConflictResolutionSkippedBecauseCanaryBudgetZero
}

public sealed record CanonicalTombstoneConflictGuardedDiagnostic : IEquatable<CanonicalTombstoneConflictGuardedDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", Domain?.ToString() ?? "", Result ?? "", Reason ?? "");

    public CanonicalTombstoneConflictGuardedDiagnosticKind Kind { get; }
    public string? SyncRunID { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; }
    public CanonicalCutoverAppSeamMode Mode { get; }
    public CanonicalTombstoneConflictDomain? Domain { get; }
    public string? ObjectID { get; }
    public CanonicalTombstoneConflictActionKind? ActionKind { get; }
    public int CandidateCount { get; }
    public int EligibleCandidateCount { get; }
    public int GateFailureCount { get; }
    public int CanaryBudget { get; }
    public int CommitAttemptedCount { get; }
    public int DeleteAttemptedCount { get; }
    public int RestoreAttemptedCount { get; }
    public int ConflictResolutionAttemptedCount { get; }
    public int DuplicateSuppressionCandidateCount { get; }
    public int StaleLiveMetadataRiskCount { get; }
    public int ActiveVsTombstoneConflictCount { get; }
    public string? Result { get; }
    public string? Reason { get; }
    public string? HashPrefix { get; }

    public CanonicalTombstoneConflictGuardedDiagnostic(
        CanonicalTombstoneConflictGuardedDiagnosticKind kind,
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalCutoverAppSeamMode mode,
        CanonicalTombstoneConflictDomain? domain = null,
        string? objectID = null,
        CanonicalTombstoneConflictActionKind? actionKind = null,
        int candidateCount = 0, int eligibleCandidateCount = 0, int gateFailureCount = 0,
        int canaryBudget = 0, int commitAttemptedCount = 0, int deleteAttemptedCount = 0,
        int restoreAttemptedCount = 0, int conflictResolutionAttemptedCount = 0,
        int duplicateSuppressionCandidateCount = 0, int staleLiveMetadataRiskCount = 0,
        int activeVsTombstoneConflictCount = 0, string? result = null, string? reason = null,
        CanonicalHash? hash = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Mode = mode;
        Domain = domain;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "tombstone-object") : null;
        ActionKind = actionKind;
        CandidateCount = Math.Max(0, candidateCount);
        EligibleCandidateCount = Math.Max(0, eligibleCandidateCount);
        GateFailureCount = Math.Max(0, gateFailureCount);
        CanaryBudget = Math.Max(0, canaryBudget);
        CommitAttemptedCount = Math.Max(0, commitAttemptedCount);
        DeleteAttemptedCount = Math.Max(0, deleteAttemptedCount);
        RestoreAttemptedCount = Math.Max(0, restoreAttemptedCount);
        ConflictResolutionAttemptedCount = Math.Max(0, conflictResolutionAttemptedCount);
        DuplicateSuppressionCandidateCount = Math.Max(0, duplicateSuppressionCandidateCount);
        StaleLiveMetadataRiskCount = Math.Max(0, staleLiveMetadataRiskCount);
        ActiveVsTombstoneConflictCount = Math.Max(0, activeVsTombstoneConflictCount);
        Result = CanonicalProductionRedaction.SafeDiagnosticText(result);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = hash != null ? CanonicalProductionRedaction.HashPrefix(hash.Value) : null;
    }

    public string DiagnosticsSummary => string.Join(",",
        new[] {
            $"trigger={Trigger}", $"nodeRole={NodeRole}", $"mode={Mode}",
            Domain != null ? $"domain={Domain}" : null,
            ObjectID != null ? $"objectID={ObjectID}" : null,
            ActionKind != null ? $"actionKind={ActionKind}" : null,
            $"candidateCount={CandidateCount}",
            $"eligibleCandidateCount={EligibleCandidateCount}",
            $"gateFailureCount={GateFailureCount}",
            $"canaryBudget={CanaryBudget}",
            $"commitAttemptedCount={CommitAttemptedCount}",
            $"deleteAttemptedCount={DeleteAttemptedCount}",
            $"restoreAttemptedCount={RestoreAttemptedCount}",
            $"conflictResolutionAttemptedCount={ConflictResolutionAttemptedCount}",
            $"duplicateSuppressionCandidateCount={DuplicateSuppressionCandidateCount}",
            $"staleLiveMetadataRiskCount={StaleLiveMetadataRiskCount}",
            $"activeVsTombstoneConflictCount={ActiveVsTombstoneConflictCount}",
            Result != null ? $"result={Result}" : null,
            Reason != null ? $"reason={Reason}" : null,
            HashPrefix != null ? $"hashPrefix={HashPrefix}" : null
        }.Where(x => x != null)
    );

    public virtual bool Equals(CanonicalTombstoneConflictGuardedDiagnostic? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictN1ReadinessStatus
{
    readyForN1AfterAudit,
    noEligibleCandidate,
    insufficientPeerSnapshot,
    insufficientEvidence,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictN1Blocker
{
    explicitN1EnablementRequired,
    localSnapshotUnavailable,
    peerSnapshotUnavailable,
    matrixBlocked,
    activePilotNotTombstoneConflict,
    ownerApprovalMissing,
    noEligibleCandidate,
    missingNoCommitEvidence,
    missingRealDataShadowCopyEvidence,
    missingExecutionShadowEvidence,
    missingDryRunEquivalence,
    blockingDivergence,
    unresolvedConflict,
    missingMetadataRouteEvidence,
    missingRealApplyPort,
    missingCommitExecutor,
    missingRollbackPlan,
    missingRollbackVerification,
    missingFailureInjection,
    missingLegacyFallback,
    missingReadSideParallel,
    missingObservationEvidence,
    missingAntiResurrectionGate,
    missingPhysicalDeleteGuard,
    missingPermanentDeleteGuard,
    missingTombstoneGCGuard,
    missingConflictConservativePolicy,
    physicalDeleteBlocked,
    permanentDeleteBlocked,
    tombstoneGCBlocked,
    staleLiveResurrectionRisk,
    unsupportedRestore,
    autoConflictResolutionBlocked,
    conflictPolicyAmbiguous,
    generatedArtifactTombstonedParentBlocked,
    canaryBudgetMustRemainZeroForV827,
    executableStagePolicyDeniedForV827,
    duplicateSuppressionMustRemainDisabled
}

public static class CanonicalTombstoneConflictN1BlockerExtensions
{
    public static bool IsV827PolicyOnly(this CanonicalTombstoneConflictN1Blocker b) => b switch
    {
        CanonicalTombstoneConflictN1Blocker.explicitN1EnablementRequired => true,
        CanonicalTombstoneConflictN1Blocker.canaryBudgetMustRemainZeroForV827 => true,
        CanonicalTombstoneConflictN1Blocker.duplicateSuppressionMustRemainDisabled => true,
        _ => false
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictN1NextRecommendedStage
{
    n1AfterAudit,
    fixBlockers,
    remainStatic
}

public sealed record CanonicalTombstoneConflictN1ReadinessReport : IEquatable<CanonicalTombstoneConflictN1ReadinessReport>
{
    public CanonicalTombstoneConflictN1ReadinessStatus Status { get; }
    public CanonicalTombstoneConflictGateResult GateResult { get; }
    public CanonicalMigrationDomain? ActivePilot { get; }
    public bool GateAllowed { get; }
    public int CanaryBudget { get; }
    public bool CanExecuteNow { get; }
    public bool WillExecuteNow { get; }
    public CanonicalTombstoneConflictGuardedSeamFailure[] MissingEvidenceList { get; }
    public CanonicalTombstoneConflictN1Blocker[] BlockerList { get; }
    public int N1CandidateCountEstimate { get; }
    public string[] SafeCandidateKinds { get; }
    public string[] UnsafeCandidateKinds { get; }
    public CanonicalTombstoneConflictN1NextRecommendedStage NextRecommendedStage { get; }
    public bool NoExecutionAssertionPassed { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalTombstoneConflictN1ReadinessReport(
        CanonicalMigrationDomain? activePilot,
        bool gateAllowed,
        CanonicalTombstoneConflictGateResult gateResult,
        CanonicalTombstoneConflictN1Blocker[] blockers,
        CanonicalTombstoneConflictGuardedSeamFailure[] missingEvidenceList,
        int candidateCount,
        int eligibleCandidateCount,
        int canaryBudget,
        bool canExecuteNow,
        bool willExecuteNow,
        bool noExecutionAssertionPassed)
    {
        var normalizedBlockers = new HashSet<CanonicalTombstoneConflictN1Blocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        var normalizedMissing = new HashSet<CanonicalTombstoneConflictGuardedSeamFailure>(missingEvidenceList)
            .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray();

        ActivePilot = activePilot;
        GateAllowed = gateAllowed;
        CanaryBudget = Math.Max(0, canaryBudget);
        CanExecuteNow = canExecuteNow;
        WillExecuteNow = willExecuteNow;
        MissingEvidenceList = normalizedMissing;
        BlockerList = normalizedBlockers;
        N1CandidateCountEstimate = Math.Max(0, eligibleCandidateCount);
        SafeCandidateKinds = new[] { "objectSoftTombstoneMarkerApplySend", "librarySoftTombstoneMarkerApplySend", "conflictRecordOnly", "resurrectionBlockRecordOnly" };
        UnsafeCandidateKinds = new[] { "physicalDelete", "permanentDelete", "tombstoneGC", "restoreWithoutExplicitRestoreSignal", "autoConflictResolution", "staleLiveMetadataApplyOverTombstone", "generatedArtifactApplyOnTombstonedParent" };
        NoExecutionAssertionPassed = noExecutionAssertionPassed;

        if (normalizedBlockers.Contains(CanonicalTombstoneConflictN1Blocker.peerSnapshotUnavailable))
            Status = CanonicalTombstoneConflictN1ReadinessStatus.insufficientPeerSnapshot;
        else if (normalizedBlockers.Contains(CanonicalTombstoneConflictN1Blocker.noEligibleCandidate))
            Status = CanonicalTombstoneConflictN1ReadinessStatus.noEligibleCandidate;
        else if (normalizedBlockers.Any(b => !b.IsV827PolicyOnly()) || normalizedMissing.Length > 0)
            Status = CanonicalTombstoneConflictN1ReadinessStatus.insufficientEvidence;
        else if (eligibleCandidateCount > 0)
            Status = CanonicalTombstoneConflictN1ReadinessStatus.readyForN1AfterAudit;
        else
            Status = CanonicalTombstoneConflictN1ReadinessStatus.blocked;

        GateResult = Status == CanonicalTombstoneConflictN1ReadinessStatus.readyForN1AfterAudit
            ? CanonicalTombstoneConflictGateResult.readyForN1AfterAudit : gateResult;

        NextRecommendedStage = Status switch
        {
            CanonicalTombstoneConflictN1ReadinessStatus.readyForN1AfterAudit => CanonicalTombstoneConflictN1NextRecommendedStage.n1AfterAudit,
            CanonicalTombstoneConflictN1ReadinessStatus.noEligibleCandidate => CanonicalTombstoneConflictN1NextRecommendedStage.remainStatic,
            _ => CanonicalTombstoneConflictN1NextRecommendedStage.fixBlockers
        };

        DiagnosticsSummary = string.Join(",",
            $"status={Status}", $"gateResult={GateResult}",
            $"activePilot={ActivePilot?.ToString() ?? "none"}",
            $"gateAllowed={GateAllowed}",
            $"blockers={string.Join("+", normalizedBlockers.Select(b => b.ToString()))}",
            $"missingEvidence={string.Join("+", normalizedMissing.Select(e => e.ToString()))}",
            $"candidateCount={Math.Max(0, candidateCount)}",
            $"n1CandidateCountEstimate={N1CandidateCountEstimate}",
            $"canaryBudget={CanaryBudget}",
            $"canExecuteNow={CanExecuteNow}",
            $"willExecuteNow={WillExecuteNow}",
            $"nextRecommendedStage={NextRecommendedStage}",
            $"noExecutionAssertionPassed={NoExecutionAssertionPassed}"
        );
    }

    public virtual bool Equals(CanonicalTombstoneConflictN1ReadinessReport? other) =>
        other is not null && Status == other.Status;
    public override int GetHashCode() => Status.GetHashCode();
}

public sealed record CanonicalTombstoneConflictEvidenceReport : IEquatable<CanonicalTombstoneConflictEvidenceReport>
{
    public CanonicalTombstoneConflictGuardedEvidenceStatus Status { get; }
    public CanonicalTombstoneConflictGuardedSeamFailure[] MissingReasons { get; }
    public CanonicalMigrationMatrixReport MatrixReport { get; }
    public CanonicalTombstoneConflictCanaryPolicy CanaryPolicy { get; }
    public bool LocalSnapshotAvailable { get; }
    public bool PeerSnapshotAvailable { get; }
    public int CandidateCount { get; }
    public int EligibleCandidateCount { get; }
    public int LegacyActionCandidateCount { get; }
    public int StaleLiveMetadataRiskCount { get; }
    public int ActiveVsTombstoneConflictCount { get; }
    public bool GeneratedArtifactTombstonedParentApplyBlocked { get; }
    public bool NoCommitEvidenceAvailable { get; }
    public bool RealApplyPortReady { get; }
    public bool CommitExecutorReady { get; }
    public bool RollbackPlanReady { get; }
    public bool FailureInjectionReady { get; }
    public bool ReadSideParallelReady { get; }
    public bool ObservationReady { get; }
    public bool AntiResurrectionGatePassed { get; }
    public bool PhysicalDeleteGuardPassed { get; }
    public bool PermanentDeleteGuardPassed { get; }
    public bool TombstoneGCGuardPassed { get; }
    public bool ConflictConservativePolicyPassed { get; }
    public bool DuplicateSuppressionPolicyDisabledBecauseN0 { get; }
    public bool LegacyFallbackAvailable { get; }

    public CanonicalTombstoneConflictEvidenceReport(
        CanonicalTombstoneConflictGuardedSeamFailure[] missingReasons,
        CanonicalMigrationMatrixReport matrixReport,
        CanonicalTombstoneConflictCanaryPolicy canaryPolicy,
        bool localSnapshotAvailable, bool peerSnapshotAvailable,
        int candidateCount, int eligibleCandidateCount, int legacyActionCandidateCount,
        int staleLiveMetadataRiskCount, int activeVsTombstoneConflictCount,
        bool generatedArtifactTombstonedParentApplyBlocked,
        bool noCommitEvidenceAvailable, bool realApplyPortReady, bool commitExecutorReady,
        bool rollbackPlanReady, bool failureInjectionReady, bool readSideParallelReady,
        bool observationReady, bool antiResurrectionGatePassed, bool physicalDeleteGuardPassed,
        bool permanentDeleteGuardPassed, bool tombstoneGCGuardPassed,
        bool conflictConservativePolicyPassed, bool duplicateSuppressionPolicyDisabledBecauseN0,
        bool legacyFallbackAvailable)
    {
        var normalizedReasons = new HashSet<CanonicalTombstoneConflictGuardedSeamFailure>(missingReasons)
            .OrderBy(r => r.ToString(), StringComparer.Ordinal).ToArray();
        Status = normalizedReasons.Length == 0
            ? CanonicalTombstoneConflictGuardedEvidenceStatus.complete
            : CanonicalTombstoneConflictGuardedEvidenceStatus.incomplete;
        MissingReasons = normalizedReasons;
        MatrixReport = matrixReport;
        CanaryPolicy = canaryPolicy;
        LocalSnapshotAvailable = localSnapshotAvailable;
        PeerSnapshotAvailable = peerSnapshotAvailable;
        CandidateCount = Math.Max(0, candidateCount);
        EligibleCandidateCount = Math.Max(0, eligibleCandidateCount);
        LegacyActionCandidateCount = Math.Max(0, legacyActionCandidateCount);
        StaleLiveMetadataRiskCount = Math.Max(0, staleLiveMetadataRiskCount);
        ActiveVsTombstoneConflictCount = Math.Max(0, activeVsTombstoneConflictCount);
        GeneratedArtifactTombstonedParentApplyBlocked = generatedArtifactTombstonedParentApplyBlocked;
        NoCommitEvidenceAvailable = noCommitEvidenceAvailable;
        RealApplyPortReady = realApplyPortReady;
        CommitExecutorReady = commitExecutorReady;
        RollbackPlanReady = rollbackPlanReady;
        FailureInjectionReady = failureInjectionReady;
        ReadSideParallelReady = readSideParallelReady;
        ObservationReady = observationReady;
        AntiResurrectionGatePassed = antiResurrectionGatePassed;
        PhysicalDeleteGuardPassed = physicalDeleteGuardPassed;
        PermanentDeleteGuardPassed = permanentDeleteGuardPassed;
        TombstoneGCGuardPassed = tombstoneGCGuardPassed;
        ConflictConservativePolicyPassed = conflictConservativePolicyPassed;
        DuplicateSuppressionPolicyDisabledBecauseN0 = duplicateSuppressionPolicyDisabledBecauseN0;
        LegacyFallbackAvailable = legacyFallbackAvailable;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"status={Status}",
        $"missingReasons={string.Join("+", MissingReasons.Select(r => r.ToString()))}",
        $"activePilot={MatrixReport.ActivePilotDomain?.ToString() ?? "none"}",
        $"matrixAllowed={MatrixReport.Allowed}",
        $"candidateCount={CandidateCount}",
        $"eligibleCandidateCount={EligibleCandidateCount}",
        $"legacyActionCandidateCount={LegacyActionCandidateCount}",
        $"staleLiveMetadataRiskCount={StaleLiveMetadataRiskCount}",
        $"activeVsTombstoneConflictCount={ActiveVsTombstoneConflictCount}",
        $"generatedArtifactTombstonedParentApplyBlocked={GeneratedArtifactTombstonedParentApplyBlocked}",
        $"localSnapshotAvailable={LocalSnapshotAvailable}",
        $"peerSnapshotAvailable={PeerSnapshotAvailable}",
        $"canaryMaxObjectsPerSyncRun={CanaryPolicy.CanaryMaxObjectsPerSyncRun}",
        $"requestedStage={CanaryPolicy.RequestedStage}",
        $"allowCandidateExecution={CanaryPolicy.AllowCandidateExecution}",
        $"runtimeSwitchEnabled={CanaryPolicy.RuntimeSwitchEnabled}",
        $"noCommitEvidenceAvailable={NoCommitEvidenceAvailable}",
        $"realApplyPortReady={RealApplyPortReady}",
        $"commitExecutorReady={CommitExecutorReady}",
        $"rollbackPlanReady={RollbackPlanReady}",
        $"failureInjectionReady={FailureInjectionReady}",
        $"readSideParallelReady={ReadSideParallelReady}",
        $"observationReady={ObservationReady}",
        $"antiResurrectionGatePassed={AntiResurrectionGatePassed}",
        $"physicalDeleteGuardPassed={PhysicalDeleteGuardPassed}",
        $"permanentDeleteGuardPassed={PermanentDeleteGuardPassed}",
        $"tombstoneGCGuardPassed={TombstoneGCGuardPassed}",
        $"conflictConservativePolicyPassed={ConflictConservativePolicyPassed}",
        $"duplicateSuppressionPolicyDisabledBecauseN0={DuplicateSuppressionPolicyDisabledBecauseN0}",
        $"legacyFallbackAvailable={LegacyFallbackAvailable}"
    );

    public virtual bool Equals(CanonicalTombstoneConflictEvidenceReport? other) =>
        other is not null && Status == other.Status;
    public override int GetHashCode() => Status.GetHashCode();
}

public sealed record CanonicalTombstoneConflictGuardedContext : IEquatable<CanonicalTombstoneConflictGuardedContext>
{
    public string? SyncRunID { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; }
    public CanonicalManifest? LocalManifest { get; }
    public CanonicalManifest? PeerManifest { get; }
    public CanonicalTombstoneConflictCandidate[] Candidates { get; }
    public CanonicalLegacyActionSnapshot LegacyActionSnapshot { get; }
    public CanonicalMigrationDomainMatrix Matrix { get; }
    public CanonicalTombstoneConflictCutoverEvidence Evidence { get; }
    public CanonicalTombstoneConflictCanaryPolicy CanaryPolicy { get; }
    public CanonicalCutoverToken? CutoverToken { get; }
    public bool LocalSnapshotAvailable { get; }
    public bool PeerSnapshotAvailable { get; }
    public bool CommitExecutorReady { get; }
    public bool FailureInjectionReady { get; }
    public bool ReadSideParallelReady { get; }
    public bool ObservationReady { get; }
    public bool AntiResurrectionGatePassed { get; }
    public bool PhysicalDeleteGuardPassed { get; }
    public bool PermanentDeleteGuardPassed { get; }
    public bool TombstoneGCGuardPassed { get; }
    public bool ConflictConservativePolicyPassed { get; }
    public int StaleLiveMetadataRiskCount { get; }
    public int ActiveVsTombstoneConflictCount { get; }
    public bool GeneratedArtifactTombstonedParentApplyBlocked { get; }
    public bool DuplicateSuppressionPolicyAvailable { get; }
    public bool LegacyFallbackAvailable { get; }

    public CanonicalTombstoneConflictGuardedContext(
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalManifest? localManifest, CanonicalManifest? peerManifest,
        CanonicalTombstoneConflictCandidate[]? candidates = null,
        CanonicalLegacyActionSnapshot? legacyActionSnapshot = null,
        CanonicalMigrationDomainMatrix? matrix = null,
        CanonicalTombstoneConflictCutoverEvidence? evidence = null,
        CanonicalTombstoneConflictCanaryPolicy? canaryPolicy = null,
        CanonicalCutoverToken? cutoverToken = null,
        bool localSnapshotAvailable = false, bool peerSnapshotAvailable = false,
        bool commitExecutorReady = true, bool failureInjectionReady = true,
        bool? readSideParallelReady = null, bool observationReady = true,
        bool antiResurrectionGatePassed = true, bool physicalDeleteGuardPassed = true,
        bool permanentDeleteGuardPassed = true, bool tombstoneGCGuardPassed = true,
        bool conflictConservativePolicyPassed = true,
        int? staleLiveMetadataRiskCount = null, int? activeVsTombstoneConflictCount = null,
        bool? generatedArtifactTombstonedParentApplyBlocked = null,
        bool duplicateSuppressionPolicyAvailable = true, bool? legacyFallbackAvailable = null)
    {
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        LocalManifest = localManifest;
        PeerManifest = peerManifest;
        Candidates = candidates ?? Array.Empty<CanonicalTombstoneConflictCandidate>();
        LegacyActionSnapshot = legacyActionSnapshot ?? CanonicalLegacyActionSnapshot.Empty;
        Matrix = matrix ?? CanonicalMigrationDomainMatrix.V827TombstoneConflictActivePilot(true, true);
        Evidence = evidence ?? new CanonicalTombstoneConflictCutoverEvidence();
        CanaryPolicy = canaryPolicy ?? CanonicalTombstoneConflictCanaryPolicy.Disabled;
        CutoverToken = cutoverToken;
        LocalSnapshotAvailable = localSnapshotAvailable;
        PeerSnapshotAvailable = peerSnapshotAvailable;
        CommitExecutorReady = commitExecutorReady;
        FailureInjectionReady = failureInjectionReady;
        ReadSideParallelReady = readSideParallelReady ?? (Evidence?.ReadSideParallelEquivalent ?? false);
        ObservationReady = observationReady;
        AntiResurrectionGatePassed = antiResurrectionGatePassed;
        PhysicalDeleteGuardPassed = physicalDeleteGuardPassed;
        PermanentDeleteGuardPassed = permanentDeleteGuardPassed;
        TombstoneGCGuardPassed = tombstoneGCGuardPassed;
        ConflictConservativePolicyPassed = conflictConservativePolicyPassed;
        StaleLiveMetadataRiskCount = Math.Max(0, staleLiveMetadataRiskCount ?? Candidates.Count(c => c.StaleLiveMetadataRisk));
        ActiveVsTombstoneConflictCount = Math.Max(0, activeVsTombstoneConflictCount ?? Candidates.Count(c =>
            c.HasActiveVsTombstoneConflict || c.Domain == CanonicalTombstoneConflictDomain.activeVsTombstoneConflict
            || c.ActionKind == CanonicalTombstoneConflictActionKind.resurrectionBlocked));
        GeneratedArtifactTombstonedParentApplyBlocked = generatedArtifactTombstonedParentApplyBlocked
            ?? Candidates.Any(c => c.ActionKind == CanonicalTombstoneConflictActionKind.generatedArtifactTombstoneMarkUnsupported);
        DuplicateSuppressionPolicyAvailable = duplicateSuppressionPolicyAvailable;
        LegacyFallbackAvailable = legacyFallbackAvailable ?? (Evidence?.LegacyFallbackAvailable ?? false);
    }

    public virtual bool Equals(CanonicalTombstoneConflictGuardedContext? other) =>
        other is not null && Trigger == other.Trigger;
    public override int GetHashCode() => Trigger.GetHashCode();
}

public sealed record CanonicalTombstoneConflictGuardedSeamResult : IEquatable<CanonicalTombstoneConflictGuardedSeamResult>
{
    public CanonicalTombstoneConflictGuardedGate Gate { get; set; }
    public CanonicalTombstoneConflictEvidenceReport EvidenceReport { get; set; }
    public CanonicalTombstoneConflictN1ReadinessReport N1ReadinessReport { get; set; }
    public CanonicalTombstoneConflictGuardedDiagnostic[] Diagnostics { get; set; }
    public CanonicalTombstoneConflictNoExecutionAssertion NoExecutionAssertion { get; set; }
    public bool CanaryBudgetZero { get; set; }
    public bool CanExecuteNow { get; set; }
    public bool WillExecuteNow { get; set; }
    public int CommitAttemptedCount { get; set; }
    public int TombstoneMarkerWrittenCount { get; set; }
    public int RestoreAttemptedCount { get; set; }
    public int PhysicalDeleteAttemptedCount { get; set; }
    public int PermanentDeleteAttemptedCount { get; set; }
    public int TombstoneGCAttemptedCount { get; set; }
    public int ConflictResolutionAttemptedCount { get; set; }
    public bool CommitExecutorCalled { get; set; }
    public bool RealApplyPortCalled { get; set; }
    public bool TombstoneMarkerWriteAttempted { get; set; }
    public bool TombstoneClearAttempted { get; set; }
    public bool ReceiveJSONMutated { get; set; }
    public bool GeneratedArtifactApplyOrDownloadCausedByTombstonedObject { get; set; }
    public string[] DuplicateLegacySuppressedActionIDs { get; set; }
    public string[] DuplicateLegacySuppressionCandidates { get; set; }
    public bool LegacyFallbackPreserved { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool LegacyPlanUnchanged { get; set; }
    public bool ProductionPlanUnchanged { get; set; }
    public bool UiMutated { get; set; }
    public bool MacInventoryResponseMutated { get; set; }
    public bool AudioInboxWritten { get; set; }
    public bool TranscriptionOrNoteGenerationTriggered { get; set; }
    public bool UploadJobCreated { get; set; }
    public bool NetworkRequestCalled { get; set; }
    public bool PendingCountsChanged { get; set; }
    public bool RouteBehaviorChanged { get; set; }
    public bool RequestVerifierBypassed { get; set; }
    public int NonfatalFailureCount { get; set; }

    public bool Succeeded => Gate.Allowed && CanaryBudgetZero && !WillExecuteNow && NoExecutionAssertion.Passed;

    public CanonicalTombstoneConflictGuardedSeamResult()
    {
        Gate = default!;
        EvidenceReport = default!;
        N1ReadinessReport = default!;
        Diagnostics = Array.Empty<CanonicalTombstoneConflictGuardedDiagnostic>();
        NoExecutionAssertion = default!;
        DuplicateLegacySuppressedActionIDs = Array.Empty<string>();
        DuplicateLegacySuppressionCandidates = Array.Empty<string>();
    }

    public virtual bool Equals(CanonicalTombstoneConflictGuardedSeamResult? other) =>
        other is not null && CanaryBudgetZero == other.CanaryBudgetZero;
    public override int GetHashCode() => CanaryBudgetZero.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictNoExecutionViolation
{
    willExecuteNow,
    commitExecutorCalled,
    realApplyPortCalled,
    tombstoneMarkerWritten,
    tombstoneCleared,
    restoreAttempted,
    physicalDeleteAttempted,
    permanentDeleteAttempted,
    tombstoneGCAttempted,
    conflictResolutionAttempted,
    receiveJSONMutated,
    generatedArtifactAppliedOrDownloaded,
    duplicateLegacySuppressed,
    legacyFallbackNotPreserved,
    runtimeSwitchEnabled,
    legacyPlanChanged,
    productionPlanChanged,
    uiMutated,
    macInventoryResponseMutated,
    audioInboxWritten,
    transcriptionOrNoteGenerationTriggered,
    uploadJobCreated,
    networkRequestCalled,
    pendingCountsChanged,
    routeBehaviorChanged,
    requestVerifierBypassed
}

public sealed record CanonicalTombstoneConflictNoExecutionAssertion : IEquatable<CanonicalTombstoneConflictNoExecutionAssertion>
{
    public bool Passed { get; }
    public CanonicalTombstoneConflictNoExecutionViolation[] Violations { get; }

    public CanonicalTombstoneConflictNoExecutionAssertion(
        bool passed, CanonicalTombstoneConflictNoExecutionViolation[] violations)
    {
        Passed = passed;
        Violations = violations;
    }

    public static CanonicalTombstoneConflictNoExecutionAssertion Evaluate(
        CanonicalTombstoneConflictGuardedSeamResult result)
    {
        var violations = new List<CanonicalTombstoneConflictNoExecutionViolation>();
        if (result.WillExecuteNow) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.willExecuteNow);
        if (result.CommitExecutorCalled || result.CommitAttemptedCount != 0)
            violations.Add(CanonicalTombstoneConflictNoExecutionViolation.commitExecutorCalled);
        if (result.RealApplyPortCalled) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.realApplyPortCalled);
        if (result.TombstoneMarkerWriteAttempted || result.TombstoneMarkerWrittenCount != 0)
            violations.Add(CanonicalTombstoneConflictNoExecutionViolation.tombstoneMarkerWritten);
        if (result.TombstoneClearAttempted) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.tombstoneCleared);
        if (result.RestoreAttemptedCount != 0) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.restoreAttempted);
        if (result.PhysicalDeleteAttemptedCount != 0) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.physicalDeleteAttempted);
        if (result.PermanentDeleteAttemptedCount != 0) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.permanentDeleteAttempted);
        if (result.TombstoneGCAttemptedCount != 0) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.tombstoneGCAttempted);
        if (result.ConflictResolutionAttemptedCount != 0) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.conflictResolutionAttempted);
        if (result.ReceiveJSONMutated) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.receiveJSONMutated);
        if (result.GeneratedArtifactApplyOrDownloadCausedByTombstonedObject)
            violations.Add(CanonicalTombstoneConflictNoExecutionViolation.generatedArtifactAppliedOrDownloaded);
        if (result.DuplicateLegacySuppressedActionIDs.Length > 0)
            violations.Add(CanonicalTombstoneConflictNoExecutionViolation.duplicateLegacySuppressed);
        if (!result.LegacyFallbackPreserved) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.legacyFallbackNotPreserved);
        if (result.RuntimeSwitchEnabled) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.runtimeSwitchEnabled);
        if (!result.LegacyPlanUnchanged) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.legacyPlanChanged);
        if (!result.ProductionPlanUnchanged) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.productionPlanChanged);
        if (result.UiMutated) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.uiMutated);
        if (result.MacInventoryResponseMutated) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.macInventoryResponseMutated);
        if (result.AudioInboxWritten) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.audioInboxWritten);
        if (result.TranscriptionOrNoteGenerationTriggered) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.transcriptionOrNoteGenerationTriggered);
        if (result.UploadJobCreated) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.uploadJobCreated);
        if (result.NetworkRequestCalled) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.networkRequestCalled);
        if (result.PendingCountsChanged) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.pendingCountsChanged);
        if (result.RouteBehaviorChanged) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.routeBehaviorChanged);
        if (result.RequestVerifierBypassed) violations.Add(CanonicalTombstoneConflictNoExecutionViolation.requestVerifierBypassed);

        var uniqueViolations = new HashSet<CanonicalTombstoneConflictNoExecutionViolation>(violations)
            .OrderBy(v => v.ToString(), StringComparer.Ordinal).ToArray();
        return new CanonicalTombstoneConflictNoExecutionAssertion(uniqueViolations.Length == 0, uniqueViolations);
    }

    public virtual bool Equals(CanonicalTombstoneConflictNoExecutionAssertion? other) =>
        other is not null && Passed == other.Passed;
    public override int GetHashCode() => Passed.GetHashCode();
}
