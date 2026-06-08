using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactPilotActivationBlocker
{
    templateNotReadyForNextPilotN0,
    libraryMetadataObservationMissing,
    matrixValidationBlocked,
    activePilotNotGeneratedArtifacts,
    canaryN0NotReached,
    canaryN1Reached,
    releaseDefaultCutoverEnabled,
    runtimeSwitchEnabled,
    legacySuppressionEnabled,
    readPathNotLegacy,
    productionInjectionPresent
}

public sealed record CanonicalGeneratedArtifactPilotActivationResult : IEquatable<CanonicalGeneratedArtifactPilotActivationResult>
{
    public bool Activated { get; }
    public CanonicalMigrationDomainMatrix Matrix { get; }
    public CanonicalMigrationMatrixReport MatrixReport { get; }
    public CanonicalGeneratedArtifactPilotActivationBlocker[] Blockers { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalGeneratedArtifactPilotActivationResult(
        CanonicalMigrationDomainMatrix matrix,
        CanonicalGeneratedArtifactPilotActivationBlocker[] blockers)
    {
        var matrixReport = matrix.Validate();
        var normalizedBlockers = new HashSet<CanonicalGeneratedArtifactPilotActivationBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        Matrix = matrix;
        MatrixReport = matrixReport;
        Blockers = normalizedBlockers;
        Activated = normalizedBlockers.Length == 0
                    && matrixReport.Allowed
                    && matrixReport.ActivePilotDomain == CanonicalMigrationDomain.generatedArtifacts;
        DiagnosticsSummary = string.Join(",",
            "domain=generatedArtifacts",
            "version=v8.22",
            $"activated={Activated}",
            $"activePilot={matrixReport.ActivePilotDomain?.ToString() ?? "none"}",
            $"matrixAllowed={matrixReport.Allowed}",
            $"blockers={string.Join("+", normalizedBlockers.Select(b => b.ToString()))}"
        );
    }

    public virtual bool Equals(CanonicalGeneratedArtifactPilotActivationResult? other) =>
        other is not null && Activated == other.Activated;
    public override int GetHashCode() => Activated.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactPilotActivation : IEquatable<CanonicalGeneratedArtifactPilotActivation>
{
    public CanonicalGeneratedArtifactTemplateReport TemplateReport { get; }
    public bool LibraryMetadataObservationCompleteOrRetirementCandidateReady { get; }
    public CanonicalGeneratedArtifactPilotActivationResult Result { get; }

    public CanonicalGeneratedArtifactPilotActivation(
        CanonicalGeneratedArtifactTemplateReport templateReport,
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady,
        CanonicalGeneratedArtifactPilotActivationResult result)
    {
        TemplateReport = templateReport;
        LibraryMetadataObservationCompleteOrRetirementCandidateReady = libraryMetadataObservationCompleteOrRetirementCandidateReady;
        Result = result;
    }

    public static CanonicalGeneratedArtifactPilotActivation V822(
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady,
        CanonicalGeneratedArtifactTemplateReport? templateReport = null)
    {
        templateReport ??= CanonicalGeneratedArtifactTemplateReport.CurrentV821Audit();
        var result = new CanonicalGeneratedArtifactPilotActivationGate().Evaluate(
            libraryMetadataObservationCompleteOrRetirementCandidateReady, templateReport);
        return new CanonicalGeneratedArtifactPilotActivation(
            templateReport,
            libraryMetadataObservationCompleteOrRetirementCandidateReady,
            result
        );
    }

    public virtual bool Equals(CanonicalGeneratedArtifactPilotActivation? other) =>
        other is not null && Result.Equals(other.Result);
    public override int GetHashCode() => Result.GetHashCode();
}

public class CanonicalGeneratedArtifactPilotActivationGate
{
    public CanonicalGeneratedArtifactPilotActivationResult Evaluate(
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady,
        CanonicalGeneratedArtifactTemplateReport? templateReport = null)
    {
        templateReport ??= CanonicalGeneratedArtifactTemplateReport.CurrentV821Audit();
        var matrix = CanonicalMigrationDomainMatrix.V822GeneratedArtifactsActivePilot(
            libraryMetadataObservationCompleteOrRetirementCandidateReady, templateReport);
        var report = matrix.Validate();
        var policy = matrix.PolicyFor(CanonicalMigrationDomain.generatedArtifacts);
        var blockers = new List<CanonicalGeneratedArtifactPilotActivationBlocker>();

        if (!templateReport.ReadyForNextPilotN0)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.templateNotReadyForNextPilotN0);
        if (!libraryMetadataObservationCompleteOrRetirementCandidateReady)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.libraryMetadataObservationMissing);
        if (!report.Allowed)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.matrixValidationBlocked);
        if (report.ActivePilotDomain != CanonicalMigrationDomain.generatedArtifacts)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.activePilotNotGeneratedArtifacts);
        if (policy?.HasReached(CanonicalCutoverAppSeamMode.canaryN0) != true)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.canaryN0NotReached);
        if (policy?.HasReached(CanonicalCutoverAppSeamMode.canaryN1) == true)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.canaryN1Reached);
        if (policy?.DefaultCutoverEnabled == true || policy?.ReleaseDefaultEnabledCutover == true)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.releaseDefaultCutoverEnabled);
        if (policy?.RuntimeSwitchEnabled == true)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.runtimeSwitchEnabled);
        if (policy?.LegacySuppressionAllowed == true)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.legacySuppressionEnabled);
        if (policy?.ReadPathLegacy != true)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.readPathNotLegacy);
        if (policy?.NoProductionInjection != true)
            blockers.Add(CanonicalGeneratedArtifactPilotActivationBlocker.productionInjectionPresent);

        return new CanonicalGeneratedArtifactPilotActivationResult(matrix, blockers.ToArray());
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactGuardedCommitEvidenceStatus
{
    complete,
    incomplete
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactGateResult
{
    blocked,
    allowedButCanaryBudgetZero,
    missingEvidence,
    unsupportedArtifactKind,
    contentLeakBlocked,
    unsafePathBlocked,
    parentTombstoneBlocked,
    audioConfusionBlocked,
    readyForN1AfterAudit
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactGuardedCommitSeamFailure
{
    disabled,
    unsupportedMode,
    productionExecuteDenied,
    viewRefreshTriggerDenied,
    retryDrainerFreshArtifactDenied,
    insufficientLocalSnapshot,
    insufficientPeerSnapshot,
    matrixValidationBlocked,
    activePilotNotGeneratedArtifacts,
    missingToken,
    missingOwnerApproval,
    missingNoCommitEvidence,
    missingRealDataShadowCopyEvidence,
    missingExecutionShadowEvidence,
    missingDryRunEquivalence,
    blockingDivergence,
    unresolvedConflict,
    missingArtifactRequestRouteEvidence,
    productionPortUnavailable,
    realApplyPortUnavailable,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    missingRollback,
    rollbackVerificationMissing,
    rollbackRehearsalMissing,
    productionRootEnabledByDefault,
    testRootMissing,
    legacyFallbackUnavailable,
    commitExecutorUnavailable,
    missingFailureInjectionEvidence,
    missingReadSideParallel,
    missingObservationEvidence,
    contentLeakGuardMissing,
    audioConfusionGuardMissing,
    unsupportedAction,
    unsupportedArtifactKind,
    contentLeakBlocked,
    unsafePathBlocked,
    parentTombstoneBlocked,
    audioConfusionBlocked,
    peerUnknown,
    peerNotAuthoritative,
    expectedHashMissing,
    expectedByteSizeMissing,
    canaryBudgetNonZeroDenied,
    internalN1ExecutionDenied,
    stagePolicyExecutionDenied,
    runtimeSwitchDenied
}

public static class CanonicalGeneratedArtifactGuardedCommitSeamFailureExtensions
{
    public static bool IsEvidenceMissing(this CanonicalGeneratedArtifactGuardedCommitSeamFailure f) => f switch
    {
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingNoCommitEvidence => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingRealDataShadowCopyEvidence => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingExecutionShadowEvidence => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingDryRunEquivalence => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingArtifactRequestRouteEvidence => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.productionPortUnavailable => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.realApplyPortUnavailable => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.applyPortDryRunOnly => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.rootBoundWriteUnavailable => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.atomicReplaceUnavailable => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.rollbackCheckpointUnavailable => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingRollback => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.rollbackVerificationMissing => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.rollbackRehearsalMissing => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.commitExecutorUnavailable => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingFailureInjectionEvidence => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingReadSideParallel => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingObservationEvidence => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.expectedHashMissing => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.expectedByteSizeMissing => true,
        _ => false
    };

    public static bool IsUnsupportedCandidateBlocker(this CanonicalGeneratedArtifactGuardedCommitSeamFailure f) => f switch
    {
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsupportedAction => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsupportedArtifactKind => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.unresolvedConflict => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.peerUnknown => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.peerNotAuthoritative => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.expectedHashMissing => true,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure.expectedByteSizeMissing => true,
        _ => false
    };
}

public sealed record CanonicalGeneratedArtifactGuardedCommitGate : IEquatable<CanonicalGeneratedArtifactGuardedCommitGate>
{
    public CanonicalCutoverAppSeamMode Mode { get; }
    public bool Allowed { get; }
    public CanonicalGeneratedArtifactGateResult Result { get; }
    public CanonicalGeneratedArtifactGuardedCommitSeamFailure[] Failures { get; }
    public string Reason { get; }

    public CanonicalGeneratedArtifactGuardedCommitGate(
        CanonicalCutoverAppSeamMode mode,
        CanonicalGeneratedArtifactGuardedCommitSeamFailure[] failures,
        bool canaryBudgetZero,
        string reason)
    {
        Mode = mode;
        Failures = new HashSet<CanonicalGeneratedArtifactGuardedCommitSeamFailure>(failures)
            .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray();
        Allowed = Failures.Length == 0;
        Result = GateResultFor(Failures, Allowed, canaryBudgetZero);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? Result.ToString();
    }

    private static CanonicalGeneratedArtifactGateResult GateResultFor(
        CanonicalGeneratedArtifactGuardedCommitSeamFailure[] failures, bool allowed, bool canaryBudgetZero)
    {
        if (allowed && canaryBudgetZero) return CanonicalGeneratedArtifactGateResult.allowedButCanaryBudgetZero;
        if (failures.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.audioConfusionBlocked))
            return CanonicalGeneratedArtifactGateResult.audioConfusionBlocked;
        if (failures.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.parentTombstoneBlocked))
            return CanonicalGeneratedArtifactGateResult.parentTombstoneBlocked;
        if (failures.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsafePathBlocked))
            return CanonicalGeneratedArtifactGateResult.unsafePathBlocked;
        if (failures.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.contentLeakBlocked)
            || failures.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.contentLeakGuardMissing))
            return CanonicalGeneratedArtifactGateResult.contentLeakBlocked;
        if (failures.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsupportedArtifactKind))
            return CanonicalGeneratedArtifactGateResult.unsupportedArtifactKind;
        if (failures.Any(f => f.IsEvidenceMissing()))
            return CanonicalGeneratedArtifactGateResult.missingEvidence;
        return CanonicalGeneratedArtifactGateResult.blocked;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactGuardedCommitGate? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactGuardedCommitDiagnosticKind
{
    canonicalGeneratedArtifactV822SeamStarted,
    canonicalGeneratedArtifactV822SeamCompleted,
    canonicalGeneratedArtifactV822SeamBlocked,
    canonicalGeneratedArtifactV822GateEvaluated,
    canonicalGeneratedArtifactV822GateAllowedBudgetZero,
    canonicalGeneratedArtifactV822GateBlocked,
    canonicalGeneratedArtifactV822CanaryBudgetZero,
    canonicalGeneratedArtifactV822CommitNotExecuted,
    canonicalGeneratedArtifactV822DownloadNotExecuted,
    canonicalGeneratedArtifactV822ApplyNotExecuted,
    canonicalGeneratedArtifactV822LegacyFallbackPreserved,
    canonicalGeneratedArtifactV822DuplicateSuppressionNotApplied,
    canonicalGeneratedArtifactV822EvidenceReportBuilt,
    canonicalGeneratedArtifactV822N1ReadinessReportBuilt,
    canonicalGeneratedArtifactCanaryBudgetZero,
    canonicalGeneratedArtifactGateAllowedButNoExecution,
    canonicalGeneratedArtifactCommitSkippedBecauseCanaryBudgetZero,
    canonicalGeneratedArtifactDownloadSkippedBecauseCanaryBudgetZero,
    canonicalGeneratedArtifactApplySkippedBecauseCanaryBudgetZero
}

public sealed record CanonicalGeneratedArtifactGuardedCommitDiagnostic : IEquatable<CanonicalGeneratedArtifactGuardedCommitDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", ArtifactID ?? "", Result ?? "", Reason ?? "");

    public CanonicalGeneratedArtifactGuardedCommitDiagnosticKind Kind { get; }
    public string? SyncRunID { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; }
    public CanonicalCutoverAppSeamMode Mode { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }
    public int CandidateCount { get; }
    public int EligibleCandidateCount { get; }
    public int GateFailureCount { get; }
    public int CanaryBudget { get; }
    public int CommitAttemptedCount { get; }
    public int DownloadAttemptedCount { get; }
    public int ApplyAttemptedCount { get; }
    public int DuplicateSuppressionCandidateCount { get; }
    public string? Result { get; }
    public string? Reason { get; }
    public string? HashPrefix { get; }

    public CanonicalGeneratedArtifactGuardedCommitDiagnostic(
        CanonicalGeneratedArtifactGuardedCommitDiagnosticKind kind,
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalCutoverAppSeamMode mode,
        string? objectID = null,
        string? artifactID = null,
        CanonicalArtifact.Kind? artifactKind = null,
        int candidateCount = 0,
        int eligibleCandidateCount = 0,
        int gateFailureCount = 0,
        int canaryBudget = 0,
        int commitAttemptedCount = 0,
        int downloadAttemptedCount = 0,
        int applyAttemptedCount = 0,
        int duplicateSuppressionCandidateCount = 0,
        string? result = null,
        string? reason = null,
        CanonicalHash? hash = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Mode = mode;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording") : null;
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        ArtifactKind = artifactKind;
        CandidateCount = Math.Max(0, candidateCount);
        EligibleCandidateCount = Math.Max(0, eligibleCandidateCount);
        GateFailureCount = Math.Max(0, gateFailureCount);
        CanaryBudget = Math.Max(0, canaryBudget);
        CommitAttemptedCount = Math.Max(0, commitAttemptedCount);
        DownloadAttemptedCount = Math.Max(0, downloadAttemptedCount);
        ApplyAttemptedCount = Math.Max(0, applyAttemptedCount);
        DuplicateSuppressionCandidateCount = Math.Max(0, duplicateSuppressionCandidateCount);
        Result = CanonicalProductionRedaction.SafeDiagnosticText(result);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = hash != null ? CanonicalProductionRedaction.HashPrefix(hash.Value) : null;
    }

    public string DiagnosticsSummary => string.Join(",",
        new[] {
            $"trigger={Trigger}",
            $"nodeRole={NodeRole}",
            $"mode={Mode}",
            ObjectID != null ? $"objectID={ObjectID}" : null,
            ArtifactID != null ? $"artifactID={ArtifactID}" : null,
            ArtifactKind != null ? $"artifactKind={ArtifactKind}" : null,
            $"candidateCount={CandidateCount}",
            $"eligibleCandidateCount={EligibleCandidateCount}",
            $"gateFailureCount={GateFailureCount}",
            $"canaryBudget={CanaryBudget}",
            $"commitAttemptedCount={CommitAttemptedCount}",
            $"downloadAttemptedCount={DownloadAttemptedCount}",
            $"applyAttemptedCount={ApplyAttemptedCount}",
            $"duplicateSuppressionCandidateCount={DuplicateSuppressionCandidateCount}",
            Result != null ? $"result={Result}" : null,
            Reason != null ? $"reason={Reason}" : null,
            HashPrefix != null ? $"hashPrefix={HashPrefix}" : null
        }.Where(x => x != null)
    );

    public virtual bool Equals(CanonicalGeneratedArtifactGuardedCommitDiagnostic? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactEvidenceReport : IEquatable<CanonicalGeneratedArtifactEvidenceReport>
{
    public CanonicalGeneratedArtifactGuardedCommitEvidenceStatus Status { get; }
    public CanonicalGeneratedArtifactGuardedCommitSeamFailure[] MissingReasons { get; }
    public CanonicalMigrationMatrixReport MatrixReport { get; }
    public CanonicalGeneratedArtifactCanaryPolicy CanaryPolicy { get; }
    public bool LocalSnapshotAvailable { get; }
    public bool PeerSnapshotAvailable { get; }
    public int CandidateCount { get; }
    public int EligibleCandidateCount { get; }
    public int LegacyActionCandidateCount { get; }
    public int UnresolvedConflictCount { get; }
    public bool NoCommitEvidenceAvailable { get; }
    public bool RealApplyPortReady { get; }
    public bool CommitExecutorReady { get; }
    public bool RollbackPlanReady { get; }
    public bool FailureInjectionReady { get; }
    public bool ReadSideParallelReady { get; }
    public bool ObservationReady { get; }
    public bool NoContentLeakGuardReady { get; }
    public bool NoAudioConfusionGuardReady { get; }
    public bool DuplicateSuppressionPolicyDisabledBecauseN0 { get; }
    public bool LegacyFallbackAvailable { get; }

    public CanonicalGeneratedArtifactEvidenceReport(
        CanonicalGeneratedArtifactGuardedCommitSeamFailure[] missingReasons,
        CanonicalMigrationMatrixReport matrixReport,
        CanonicalGeneratedArtifactCanaryPolicy canaryPolicy,
        bool localSnapshotAvailable,
        bool peerSnapshotAvailable,
        int candidateCount,
        int eligibleCandidateCount,
        int legacyActionCandidateCount,
        int unresolvedConflictCount,
        bool noCommitEvidenceAvailable,
        bool realApplyPortReady,
        bool commitExecutorReady,
        bool rollbackPlanReady,
        bool failureInjectionReady,
        bool readSideParallelReady,
        bool observationReady,
        bool noContentLeakGuardReady,
        bool noAudioConfusionGuardReady,
        bool duplicateSuppressionPolicyDisabledBecauseN0,
        bool legacyFallbackAvailable)
    {
        var normalizedReasons = new HashSet<CanonicalGeneratedArtifactGuardedCommitSeamFailure>(missingReasons)
            .OrderBy(r => r.ToString(), StringComparer.Ordinal).ToArray();
        Status = normalizedReasons.Length == 0
            ? CanonicalGeneratedArtifactGuardedCommitEvidenceStatus.complete
            : CanonicalGeneratedArtifactGuardedCommitEvidenceStatus.incomplete;
        MissingReasons = normalizedReasons;
        MatrixReport = matrixReport;
        CanaryPolicy = canaryPolicy;
        LocalSnapshotAvailable = localSnapshotAvailable;
        PeerSnapshotAvailable = peerSnapshotAvailable;
        CandidateCount = Math.Max(0, candidateCount);
        EligibleCandidateCount = Math.Max(0, eligibleCandidateCount);
        LegacyActionCandidateCount = Math.Max(0, legacyActionCandidateCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        NoCommitEvidenceAvailable = noCommitEvidenceAvailable;
        RealApplyPortReady = realApplyPortReady;
        CommitExecutorReady = commitExecutorReady;
        RollbackPlanReady = rollbackPlanReady;
        FailureInjectionReady = failureInjectionReady;
        ReadSideParallelReady = readSideParallelReady;
        ObservationReady = observationReady;
        NoContentLeakGuardReady = noContentLeakGuardReady;
        NoAudioConfusionGuardReady = noAudioConfusionGuardReady;
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
        $"unresolvedConflictCount={UnresolvedConflictCount}",
        $"localSnapshotAvailable={LocalSnapshotAvailable}",
        $"peerSnapshotAvailable={PeerSnapshotAvailable}",
        $"canaryMaxObjectsPerSyncRun={CanaryPolicy.CanaryMaxObjectsPerSyncRun}",
        $"stagePolicy={CanaryPolicy.StagePolicy.RequestedStage}",
        $"allowsInternalN1Execution={CanaryPolicy.AllowsInternalN1Execution}",
        $"noCommitEvidenceAvailable={NoCommitEvidenceAvailable}",
        $"realApplyPortReady={RealApplyPortReady}",
        $"commitExecutorReady={CommitExecutorReady}",
        $"rollbackPlanReady={RollbackPlanReady}",
        $"failureInjectionReady={FailureInjectionReady}",
        $"readSideParallelReady={ReadSideParallelReady}",
        $"observationReady={ObservationReady}",
        $"noContentLeakGuardReady={NoContentLeakGuardReady}",
        $"noAudioConfusionGuardReady={NoAudioConfusionGuardReady}",
        $"duplicateSuppressionPolicyDisabledBecauseN0={DuplicateSuppressionPolicyDisabledBecauseN0}",
        $"legacyFallbackAvailable={LegacyFallbackAvailable}"
    );

    public virtual bool Equals(CanonicalGeneratedArtifactEvidenceReport? other) =>
        other is not null && Status == other.Status;
    public override int GetHashCode() => Status.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactN1ReadinessStatus
{
    readyForN1AfterAudit,
    noEligibleCandidate,
    insufficientPeerSnapshot,
    insufficientEvidence,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactN1Blocker
{
    explicitN1EnablementRequired,
    localSnapshotUnavailable,
    peerSnapshotUnavailable,
    matrixBlocked,
    activePilotNotGeneratedArtifacts,
    ownerApprovalMissing,
    noEligibleCandidate,
    missingNoCommitEvidence,
    missingRealDataShadowCopyEvidence,
    missingExecutionShadowEvidence,
    missingDryRunEquivalence,
    blockingDivergence,
    unresolvedConflict,
    missingArtifactRequestRouteEvidence,
    missingRealApplyPort,
    missingCommitExecutor,
    missingRollbackPlan,
    missingRollbackVerification,
    missingFailureInjection,
    missingLegacyFallback,
    missingReadSideParallel,
    missingObservationEvidence,
    contentLeakGuardMissing,
    audioConfusionGuardMissing,
    unsupportedCandidate,
    unsafePathBlocked,
    parentTombstoneBlocked,
    audioConfusionBlocked,
    canaryBudgetMustRemainZeroForV822,
    executableStagePolicyDeniedForV822,
    duplicateSuppressionMustRemainDisabled
}

public static class CanonicalGeneratedArtifactN1BlockerExtensions
{
    public static bool IsV822PolicyOnly(this CanonicalGeneratedArtifactN1Blocker b) => b switch
    {
        CanonicalGeneratedArtifactN1Blocker.explicitN1EnablementRequired => true,
        CanonicalGeneratedArtifactN1Blocker.canaryBudgetMustRemainZeroForV822 => true,
        CanonicalGeneratedArtifactN1Blocker.duplicateSuppressionMustRemainDisabled => true,
        _ => false
    };
}

public sealed record CanonicalGeneratedArtifactN1ReadinessReport : IEquatable<CanonicalGeneratedArtifactN1ReadinessReport>
{
    public CanonicalGeneratedArtifactN1ReadinessStatus Status { get; }
    public CanonicalGeneratedArtifactGateResult GateResult { get; }
    public CanonicalGeneratedArtifactN1Blocker[] Blockers { get; }
    public int CandidateCount { get; }
    public int EligibleCandidateCount { get; }
    public int CanaryBudget { get; }
    public bool CanExecuteNow { get; }
    public bool WillExecuteNow { get; }
    public bool NoExecutionAssertionPassed { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalGeneratedArtifactN1ReadinessReport(
        CanonicalGeneratedArtifactN1Blocker[] blockers,
        int candidateCount,
        int eligibleCandidateCount,
        int canaryBudget,
        bool canExecuteNow,
        bool willExecuteNow,
        bool noExecutionAssertionPassed)
    {
        var normalizedBlockers = new HashSet<CanonicalGeneratedArtifactN1Blocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        Blockers = normalizedBlockers;
        CandidateCount = Math.Max(0, candidateCount);
        EligibleCandidateCount = Math.Max(0, eligibleCandidateCount);
        CanaryBudget = Math.Max(0, canaryBudget);
        CanExecuteNow = canExecuteNow;
        WillExecuteNow = willExecuteNow;
        NoExecutionAssertionPassed = noExecutionAssertionPassed;

        if (normalizedBlockers.Contains(CanonicalGeneratedArtifactN1Blocker.peerSnapshotUnavailable))
            Status = CanonicalGeneratedArtifactN1ReadinessStatus.insufficientPeerSnapshot;
        else if (normalizedBlockers.Contains(CanonicalGeneratedArtifactN1Blocker.noEligibleCandidate))
            Status = CanonicalGeneratedArtifactN1ReadinessStatus.noEligibleCandidate;
        else if (normalizedBlockers.Any(b => !b.IsV822PolicyOnly()))
            Status = CanonicalGeneratedArtifactN1ReadinessStatus.insufficientEvidence;
        else if (EligibleCandidateCount > 0)
            Status = CanonicalGeneratedArtifactN1ReadinessStatus.readyForN1AfterAudit;
        else
            Status = CanonicalGeneratedArtifactN1ReadinessStatus.blocked;

        GateResult = Status == CanonicalGeneratedArtifactN1ReadinessStatus.readyForN1AfterAudit
            ? CanonicalGeneratedArtifactGateResult.readyForN1AfterAudit
            : CanonicalGeneratedArtifactGateResult.blocked;

        DiagnosticsSummary = string.Join(",",
            $"status={Status}",
            $"gateResult={GateResult}",
            $"blockers={string.Join("+", normalizedBlockers.Select(b => b.ToString()))}",
            $"candidateCount={CandidateCount}",
            $"eligibleCandidateCount={EligibleCandidateCount}",
            $"canaryBudget={CanaryBudget}",
            $"canExecuteNow={CanExecuteNow}",
            $"willExecuteNow={WillExecuteNow}",
            $"noExecutionAssertionPassed={NoExecutionAssertionPassed}"
        );
    }

    public virtual bool Equals(CanonicalGeneratedArtifactN1ReadinessReport? other) =>
        other is not null && Status == other.Status;
    public override int GetHashCode() => Status.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactNoExecutionViolation
{
    willExecuteNow,
    commitAttempted,
    downloadAttempted,
    applyAttempted,
    committedArtifact,
    productionCommitCalled,
    realApplyPortCommitCalled,
    networkRequestCalled,
    artifactRequestRouteCalled,
    generatedArtifactDownloaded,
    generatedArtifactApplied,
    generatedArtifactFileWritten,
    generatedArtifactUploadJobCreated,
    audioAutoDownloadTriggered,
    duplicateLegacySuppressed,
    legacyFallbackNotPreserved,
    runtimeSwitchEnabled,
    legacyPlanChanged,
    productionPlanChanged
}

public sealed record CanonicalGeneratedArtifactNoExecutionAssertion : IEquatable<CanonicalGeneratedArtifactNoExecutionAssertion>
{
    public bool Passed { get; }
    public CanonicalGeneratedArtifactNoExecutionViolation[] Violations { get; }

    public CanonicalGeneratedArtifactNoExecutionAssertion(
        bool passed, CanonicalGeneratedArtifactNoExecutionViolation[] violations)
    {
        Passed = passed;
        Violations = violations;
    }

    public static CanonicalGeneratedArtifactNoExecutionAssertion Evaluate(
        CanonicalGeneratedArtifactGuardedCommitSeamResult result)
    {
        var violations = new List<CanonicalGeneratedArtifactNoExecutionViolation>();
        if (result.WillExecuteNow) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.willExecuteNow);
        if (result.CommitAttemptedCount != 0) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.commitAttempted);
        if (result.DownloadAttemptedCount != 0) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.downloadAttempted);
        if (result.ApplyAttemptedCount != 0) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.applyAttempted);
        if (result.CommittedArtifactCount != 0) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.committedArtifact);
        if (result.ProductionCommitCalled) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.productionCommitCalled);
        if (result.RealApplyPortCommitCalled) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.realApplyPortCommitCalled);
        if (result.NetworkRequestCalled) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.networkRequestCalled);
        if (result.ArtifactRequestRouteCalled) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.artifactRequestRouteCalled);
        if (result.GeneratedArtifactDownloaded) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.generatedArtifactDownloaded);
        if (result.GeneratedArtifactApplied) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.generatedArtifactApplied);
        if (result.GeneratedArtifactFileWritten) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.generatedArtifactFileWritten);
        if (result.GeneratedArtifactUploadJobCreated) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.generatedArtifactUploadJobCreated);
        if (result.AudioAutoDownloadTriggered) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.audioAutoDownloadTriggered);
        if (result.DuplicateLegacySuppressedActionIDs.Length > 0) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.duplicateLegacySuppressed);
        if (!result.LegacyFallbackPreserved) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.legacyFallbackNotPreserved);
        if (result.RuntimeSwitchEnabled) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.runtimeSwitchEnabled);
        if (!result.LegacyPlanUnchanged) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.legacyPlanChanged);
        if (!result.ProductionPlanUnchanged) violations.Add(CanonicalGeneratedArtifactNoExecutionViolation.productionPlanChanged);

        var uniqueViolations = new HashSet<CanonicalGeneratedArtifactNoExecutionViolation>(violations)
            .OrderBy(v => v.ToString(), StringComparer.Ordinal).ToArray();
        return new CanonicalGeneratedArtifactNoExecutionAssertion(uniqueViolations.Length == 0, uniqueViolations);
    }

    public virtual bool Equals(CanonicalGeneratedArtifactNoExecutionAssertion? other) =>
        other is not null && Passed == other.Passed;
    public override int GetHashCode() => Passed.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactGuardedCommitContext : IEquatable<CanonicalGeneratedArtifactGuardedCommitContext>
{
    public string? SyncRunID { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; }
    public CanonicalManifest? LocalManifest { get; }
    public CanonicalManifest? PeerManifest { get; }
    public CanonicalLegacyActionSnapshot LegacyActionSnapshot { get; }
    public CanonicalMigrationDomainMatrix Matrix { get; }
    public CanonicalGeneratedArtifactCutoverEvidence Evidence { get; }
    public CanonicalGeneratedArtifactCanaryPolicy CanaryPolicy { get; }
    public CanonicalCutoverToken? CutoverToken { get; }
    public CanonicalGeneratedArtifactCutoverCandidate[] Candidates { get; }
    public bool LocalSnapshotAvailable { get; }
    public bool PeerSnapshotAvailable { get; }
    public int UnresolvedConflictCount { get; }
    public bool CommitExecutorReady { get; }
    public bool FailureInjectionReady { get; }
    public bool ReadSideParallelReady { get; }
    public bool ObservationReady { get; }
    public bool NoContentLeakGuardReady { get; }
    public bool NoAudioConfusionGuardReady { get; }
    public bool DuplicateSuppressionPolicyAvailable { get; }
    public bool LegacyFallbackAvailable { get; }

    public CanonicalGeneratedArtifactGuardedCommitContext(
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalManifest? localManifest,
        CanonicalManifest? peerManifest,
        CanonicalLegacyActionSnapshot? legacyActionSnapshot = null,
        CanonicalMigrationDomainMatrix? matrix = null,
        CanonicalGeneratedArtifactCutoverEvidence? evidence = null,
        CanonicalGeneratedArtifactCanaryPolicy? canaryPolicy = null,
        CanonicalCutoverToken? cutoverToken = null,
        CanonicalGeneratedArtifactCutoverCandidate[]? candidates = null,
        bool localSnapshotAvailable = false,
        bool peerSnapshotAvailable = false,
        int unresolvedConflictCount = 0,
        bool commitExecutorReady = true,
        bool failureInjectionReady = true,
        bool? readSideParallelReady = null,
        bool observationReady = true,
        bool noContentLeakGuardReady = true,
        bool noAudioConfusionGuardReady = true,
        bool duplicateSuppressionPolicyAvailable = true,
        bool? legacyFallbackAvailable = null)
    {
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        LocalManifest = localManifest;
        PeerManifest = peerManifest;
        LegacyActionSnapshot = legacyActionSnapshot ?? CanonicalLegacyActionSnapshot.Empty;
        Matrix = matrix ?? CanonicalMigrationDomainMatrix.V822GeneratedArtifactsActivePilot(true);
        Evidence = evidence ?? new CanonicalGeneratedArtifactCutoverEvidence();
        CanaryPolicy = canaryPolicy ?? CanonicalGeneratedArtifactCanaryPolicy.Disabled;
        CutoverToken = cutoverToken;
        Candidates = candidates ?? Array.Empty<CanonicalGeneratedArtifactCutoverCandidate>();
        LocalSnapshotAvailable = localSnapshotAvailable;
        PeerSnapshotAvailable = peerSnapshotAvailable;
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        CommitExecutorReady = commitExecutorReady;
        FailureInjectionReady = failureInjectionReady;
        ReadSideParallelReady = readSideParallelReady ?? Evidence.ReadSideParallelEquivalent;
        ObservationReady = observationReady;
        NoContentLeakGuardReady = noContentLeakGuardReady;
        NoAudioConfusionGuardReady = noAudioConfusionGuardReady;
        DuplicateSuppressionPolicyAvailable = duplicateSuppressionPolicyAvailable;
        LegacyFallbackAvailable = legacyFallbackAvailable ?? Evidence.LegacyFallbackAvailable;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactGuardedCommitContext? other) =>
        other is not null && Trigger == other.Trigger;
    public override int GetHashCode() => Trigger.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactGuardedCommitSeamResult : IEquatable<CanonicalGeneratedArtifactGuardedCommitSeamResult>
{
    public CanonicalGeneratedArtifactGuardedCommitGate Gate { get; set; }
    public CanonicalGeneratedArtifactEvidenceReport EvidenceReport { get; set; }
    public CanonicalGeneratedArtifactN1ReadinessReport N1ReadinessReport { get; set; }
    public CanonicalGeneratedArtifactGuardedCommitDiagnostic[] Diagnostics { get; set; }
    public CanonicalGeneratedArtifactNoExecutionAssertion NoExecutionAssertion { get; set; }
    public bool CanaryBudgetZero { get; set; }
    public bool CanExecuteNow { get; set; }
    public bool WillExecuteNow { get; set; }
    public int CommitAttemptedCount { get; set; }
    public int DownloadAttemptedCount { get; set; }
    public int ApplyAttemptedCount { get; set; }
    public int CommittedArtifactCount { get; set; }
    public bool ProductionCommitCalled { get; set; }
    public bool RealApplyPortCommitCalled { get; set; }
    public bool NetworkRequestCalled { get; set; }
    public bool ArtifactRequestRouteCalled { get; set; }
    public bool GeneratedArtifactDownloaded { get; set; }
    public bool GeneratedArtifactApplied { get; set; }
    public bool GeneratedArtifactFileWritten { get; set; }
    public bool GeneratedArtifactUploadJobCreated { get; set; }
    public bool AudioAutoDownloadTriggered { get; set; }
    public string[] DuplicateLegacySuppressedActionIDs { get; set; }
    public string[] DuplicateLegacySuppressionCandidates { get; set; }
    public bool LegacyFallbackPreserved { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool LegacyPlanUnchanged { get; set; }
    public bool ProductionPlanUnchanged { get; set; }
    public int NonfatalFailureCount { get; set; }

    public bool Succeeded => Gate.Allowed && CanaryBudgetZero && !WillExecuteNow && NoExecutionAssertion.Passed;

    public CanonicalGeneratedArtifactGuardedCommitSeamResult()
    {
        Gate = default!;
        EvidenceReport = default!;
        N1ReadinessReport = default!;
        Diagnostics = Array.Empty<CanonicalGeneratedArtifactGuardedCommitDiagnostic>();
        NoExecutionAssertion = default!;
        DuplicateLegacySuppressedActionIDs = Array.Empty<string>();
        DuplicateLegacySuppressionCandidates = Array.Empty<string>();
    }

    public virtual bool Equals(CanonicalGeneratedArtifactGuardedCommitSeamResult? other) =>
        other is not null && CanaryBudgetZero == other.CanaryBudgetZero;
    public override int GetHashCode() => CanaryBudgetZero.GetHashCode();
}

public class CanonicalGeneratedArtifactGuardedCommitSeam
{
    public CanonicalGeneratedArtifactGuardedCommitSeamResult Evaluate(
        CanonicalGeneratedArtifactCutoverAppSeamConfiguration configuration,
        CanonicalGeneratedArtifactGuardedCommitContext context)
    {
        var canaryPolicy = configuration.Policy.CanaryPolicy;
        var duplicateCandidates = DuplicateSuppressionCandidates(context);
        var evidenceReport = MakeEvidenceReport(context, canaryPolicy, duplicateCandidates);
        var canaryBudgetZero = IsCanaryBudgetZero(canaryPolicy);
        var gate = EvaluateGate(configuration, context, evidenceReport, canaryPolicy, canaryBudgetZero);
        var eligibleCandidateCount = EligibleCandidateCount(context.Candidates, context.PeerManifest?.Node);
        var canExecuteNow = gate.Allowed;
        var willExecuteNow = false;
        var emptyAssertion = new CanonicalGeneratedArtifactNoExecutionAssertion(true, Array.Empty<CanonicalGeneratedArtifactNoExecutionViolation>());

        var preliminaryReadiness = MakeN1ReadinessReport(context, evidenceReport, canaryPolicy,
            canExecuteNow, willExecuteNow, emptyAssertion.Passed);

        var diagnostics = BaseDiagnostics(configuration, context, gate, evidenceReport, preliminaryReadiness,
            context.Candidates.Length, eligibleCandidateCount, duplicateCandidates.Length,
            canaryPolicy.CanaryMaxObjectsPerSyncRun, canaryBudgetZero, willExecuteNow);

        var result = new CanonicalGeneratedArtifactGuardedCommitSeamResult
        {
            Gate = gate,
            EvidenceReport = evidenceReport,
            N1ReadinessReport = preliminaryReadiness,
            Diagnostics = diagnostics.Take(configuration.Policy.MaxDiagnosticsEvents).ToArray(),
            NoExecutionAssertion = emptyAssertion,
            CanaryBudgetZero = canaryBudgetZero,
            CanExecuteNow = canExecuteNow,
            WillExecuteNow = willExecuteNow,
            CommitAttemptedCount = 0,
            DownloadAttemptedCount = 0,
            ApplyAttemptedCount = 0,
            CommittedArtifactCount = 0,
            ProductionCommitCalled = false,
            RealApplyPortCommitCalled = false,
            NetworkRequestCalled = false,
            ArtifactRequestRouteCalled = false,
            GeneratedArtifactDownloaded = false,
            GeneratedArtifactApplied = false,
            GeneratedArtifactFileWritten = false,
            GeneratedArtifactUploadJobCreated = false,
            AudioAutoDownloadTriggered = false,
            DuplicateLegacySuppressedActionIDs = Array.Empty<string>(),
            DuplicateLegacySuppressionCandidates = duplicateCandidates,
            LegacyFallbackPreserved = true,
            RuntimeSwitchEnabled = false,
            LegacyPlanUnchanged = true,
            ProductionPlanUnchanged = true,
            NonfatalFailureCount = gate.Failures.Length
        };

        var assertion = CanonicalGeneratedArtifactNoExecutionAssertion.Evaluate(result);
        result.NoExecutionAssertion = assertion;
        result.N1ReadinessReport = MakeN1ReadinessReport(context, evidenceReport, canaryPolicy,
            canExecuteNow, willExecuteNow, assertion.Passed);

        diagnostics = BaseDiagnostics(configuration, context, gate, evidenceReport, result.N1ReadinessReport,
            context.Candidates.Length, eligibleCandidateCount, duplicateCandidates.Length,
            canaryPolicy.CanaryMaxObjectsPerSyncRun, canaryBudgetZero, willExecuteNow);
        result.Diagnostics = diagnostics.Take(configuration.Policy.MaxDiagnosticsEvents).ToArray();

        return result;
    }

    private CanonicalGeneratedArtifactGuardedCommitGate EvaluateGate(
        CanonicalGeneratedArtifactCutoverAppSeamConfiguration configuration,
        CanonicalGeneratedArtifactGuardedCommitContext context,
        CanonicalGeneratedArtifactEvidenceReport evidenceReport,
        CanonicalGeneratedArtifactCanaryPolicy canaryPolicy,
        bool canaryBudgetZero)
    {
        var failures = new List<CanonicalGeneratedArtifactGuardedCommitSeamFailure>();
        var mode = configuration.EffectiveMode;

        if (mode == CanonicalCutoverAppSeamMode.disabled)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.disabled);

        switch (mode)
        {
            case CanonicalCutoverAppSeamMode.disabled:
            case CanonicalCutoverAppSeamMode.guardedExecuteCommit:
            case CanonicalCutoverAppSeamMode.canaryCommit:
                break;
            case CanonicalCutoverAppSeamMode.guardedExecuteNoCommit:
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsupportedMode);
                break;
            case CanonicalCutoverAppSeamMode.productionExecute:
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.productionExecuteDenied);
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsupportedMode);
                break;
        }

        if (context.Trigger == CanonicalSyncPlanTrigger.viewRefresh)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.viewRefreshTriggerDenied);
        if (context.Trigger == CanonicalSyncPlanTrigger.retryDrainer)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.retryDrainerFreshArtifactDenied);
        if (!context.LocalSnapshotAvailable || context.LocalManifest == null)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.insufficientLocalSnapshot);
        if (!context.PeerSnapshotAvailable || context.PeerManifest == null)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.insufficientPeerSnapshot);
        if (!evidenceReport.MatrixReport.Allowed)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.matrixValidationBlocked);
        if (evidenceReport.MatrixReport.ActivePilotDomain != CanonicalMigrationDomain.generatedArtifacts)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.activePilotNotGeneratedArtifacts);
        if (context.CutoverToken == null)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingToken);
        if (context.CutoverToken?.OwnerApproved != true)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingOwnerApproval);
        if (canaryPolicy.CanaryMaxObjectsPerSyncRun > 0)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.canaryBudgetNonZeroDenied);
        if (canaryPolicy.AllowsInternalN1Execution)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.internalN1ExecutionDenied);
        if (canaryPolicy.StagePolicy.RequestedStage.IsExecutable() || canaryPolicy.StagePolicy.AllowCandidateExecution)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.stagePolicyExecutionDenied);
        if (canaryPolicy.StagePolicy.RuntimeSwitchEnabled)
            failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.runtimeSwitchDenied);

        failures.AddRange(evidenceReport.MissingReasons);

        return new CanonicalGeneratedArtifactGuardedCommitGate(
            mode, failures.ToArray(), canaryBudgetZero,
            failures.Count == 0 ? "canonicalGeneratedArtifactV822GateAllowedBudgetZero"
                                : string.Join(",", failures.Select(f => f.ToString())));
    }

    private CanonicalGeneratedArtifactEvidenceReport MakeEvidenceReport(
        CanonicalGeneratedArtifactGuardedCommitContext context,
        CanonicalGeneratedArtifactCanaryPolicy canaryPolicy,
        string[] duplicateCandidates)
    {
        var evidence = context.Evidence;
        var matrixReport = context.Matrix.Validate();
        var candidateFailures = CandidateFailures(context.Candidates, context.PeerManifest?.Node);
        var missing = new List<CanonicalGeneratedArtifactGuardedCommitSeamFailure>();

        if (!evidence.NoCommitEvidenceAvailable) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingNoCommitEvidence);
        if (!evidence.RealDataShadowCopyVerified) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingRealDataShadowCopyEvidence);
        if (!evidence.ExecutionShadowVerified) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingExecutionShadowEvidence);
        if (!evidence.DryRunEquivalenceVerified) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingDryRunEquivalence);
        if (!evidence.NoBlockingDivergence) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.blockingDivergence);
        if (!evidence.NoUnresolvedConflict || context.UnresolvedConflictCount > 0
            || context.Candidates.Any(c => c.UnresolvedConflict))
            missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unresolvedConflict);
        if (!evidence.ArtifactRequestRouteEvidenceAvailable) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingArtifactRequestRouteEvidence);
        if (!evidence.ProductionPortAvailable) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.productionPortUnavailable);
        if (!evidence.RealRootBoundApplyPortAvailable) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.realApplyPortUnavailable);
        if (!evidence.ApplyPortMode.IsNonDryRunRootBound()) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.applyPortDryRunOnly);
        if (!evidence.RootBoundWriteAvailable) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.rootBoundWriteUnavailable);
        if (!evidence.AtomicReplaceAvailable) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.atomicReplaceUnavailable);
        if (!evidence.RollbackCheckpointAvailable) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.rollbackCheckpointUnavailable);
        if (evidence.RollbackPlan?.Covers(CanonicalCutoverDomain.generatedArtifacts) != true) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingRollback);
        if (!evidence.RollbackVerified) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.rollbackVerificationMissing);
        if (!evidence.RollbackRehearsalPassed) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.rollbackRehearsalMissing);
        if (!evidence.ProductionRootDisabledByDefault) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.productionRootEnabledByDefault);
        if (evidence.ApplyPortMode == CanonicalGeneratedArtifactApplyPortMode.testRootBound && !evidence.TestRootUsed)
            missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.testRootMissing);
        if (!context.LegacyFallbackAvailable) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.legacyFallbackUnavailable);
        if (!context.CommitExecutorReady) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.commitExecutorUnavailable);
        if (!context.FailureInjectionReady) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingFailureInjectionEvidence);
        if (!context.ReadSideParallelReady) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingReadSideParallel);
        if (!context.ObservationReady) missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.missingObservationEvidence);
        if (!context.NoContentLeakGuardReady)
        {
            missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.contentLeakGuardMissing);
            missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.contentLeakBlocked);
        }
        if (!context.NoAudioConfusionGuardReady)
        {
            missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.audioConfusionGuardMissing);
            missing.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.audioConfusionBlocked);
        }
        missing.AddRange(candidateFailures);

        return new CanonicalGeneratedArtifactEvidenceReport(
            missing.ToArray(), matrixReport, canaryPolicy,
            context.LocalSnapshotAvailable, context.PeerSnapshotAvailable,
            context.Candidates.Length,
            EligibleCandidateCount(context.Candidates, context.PeerManifest?.Node),
            duplicateCandidates.Length,
            context.UnresolvedConflictCount,
            evidence.NoCommitEvidenceAvailable,
            evidence.RealRootBoundApplyPortAvailable && evidence.ApplyPortMode.IsNonDryRunRootBound() && evidence.RootBoundWriteAvailable,
            context.CommitExecutorReady,
            evidence.RollbackPlan?.Covers(CanonicalCutoverDomain.generatedArtifacts) == true,
            context.FailureInjectionReady,
            context.ReadSideParallelReady,
            context.ObservationReady,
            context.NoContentLeakGuardReady,
            context.NoAudioConfusionGuardReady,
            IsCanaryBudgetZero(canaryPolicy),
            context.LegacyFallbackAvailable
        );
    }

    private CanonicalGeneratedArtifactN1ReadinessReport MakeN1ReadinessReport(
        CanonicalGeneratedArtifactGuardedCommitContext context,
        CanonicalGeneratedArtifactEvidenceReport evidenceReport,
        CanonicalGeneratedArtifactCanaryPolicy canaryPolicy,
        bool canExecuteNow, bool willExecuteNow, bool noExecutionAssertionPassed)
    {
        var blockers = new List<CanonicalGeneratedArtifactN1Blocker>
        {
            CanonicalGeneratedArtifactN1Blocker.explicitN1EnablementRequired,
            CanonicalGeneratedArtifactN1Blocker.canaryBudgetMustRemainZeroForV822,
            CanonicalGeneratedArtifactN1Blocker.duplicateSuppressionMustRemainDisabled
        };

        if (!context.LocalSnapshotAvailable || context.LocalManifest == null)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.localSnapshotUnavailable);
        if (!context.PeerSnapshotAvailable || context.PeerManifest == null)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.peerSnapshotUnavailable);
        if (!evidenceReport.MatrixReport.Allowed)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.matrixBlocked);
        if (evidenceReport.MatrixReport.ActivePilotDomain != CanonicalMigrationDomain.generatedArtifacts)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.activePilotNotGeneratedArtifacts);
        if (context.CutoverToken?.OwnerApproved != true)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.ownerApprovalMissing);
        if (!context.Evidence.NoCommitEvidenceAvailable)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingNoCommitEvidence);
        if (!context.Evidence.RealDataShadowCopyVerified)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingRealDataShadowCopyEvidence);
        if (!context.Evidence.ExecutionShadowVerified)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingExecutionShadowEvidence);
        if (!context.Evidence.DryRunEquivalenceVerified)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingDryRunEquivalence);
        if (!context.Evidence.NoBlockingDivergence)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.blockingDivergence);
        if (!context.Evidence.NoUnresolvedConflict || context.UnresolvedConflictCount > 0)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.unresolvedConflict);
        if (!context.Evidence.ArtifactRequestRouteEvidenceAvailable)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingArtifactRequestRouteEvidence);
        if (!evidenceReport.RealApplyPortReady)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingRealApplyPort);
        if (!context.CommitExecutorReady)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingCommitExecutor);
        if (!evidenceReport.RollbackPlanReady)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingRollbackPlan);
        if (!context.Evidence.RollbackVerified || !context.Evidence.RollbackRehearsalPassed)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingRollbackVerification);
        if (!context.FailureInjectionReady)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingFailureInjection);
        if (!context.LegacyFallbackAvailable)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingLegacyFallback);
        if (!context.ReadSideParallelReady)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingReadSideParallel);
        if (!context.ObservationReady)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.missingObservationEvidence);
        if (!context.NoContentLeakGuardReady)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.contentLeakGuardMissing);
        if (!context.NoAudioConfusionGuardReady)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.audioConfusionGuardMissing);
        if (canaryPolicy.StagePolicy.RequestedStage.IsExecutable() || canaryPolicy.StagePolicy.AllowCandidateExecution)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.executableStagePolicyDeniedForV822);

        var candidateBlockers = CandidateFailures(context.Candidates, context.PeerManifest?.Node);
        if (candidateBlockers.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsafePathBlocked))
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.unsafePathBlocked);
        if (candidateBlockers.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.parentTombstoneBlocked))
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.parentTombstoneBlocked);
        if (candidateBlockers.Contains(CanonicalGeneratedArtifactGuardedCommitSeamFailure.audioConfusionBlocked))
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.audioConfusionBlocked);
        if (candidateBlockers.Any(f => f.IsUnsupportedCandidateBlocker()))
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.unsupportedCandidate);

        var eligibleCount = EligibleCandidateCount(context.Candidates, context.PeerManifest?.Node);
        if (eligibleCount == 0)
            blockers.Add(CanonicalGeneratedArtifactN1Blocker.noEligibleCandidate);

        return new CanonicalGeneratedArtifactN1ReadinessReport(
            blockers.ToArray(), context.Candidates.Length, eligibleCount,
            canaryPolicy.CanaryMaxObjectsPerSyncRun, canExecuteNow, willExecuteNow, noExecutionAssertionPassed);
    }

    private List<CanonicalGeneratedArtifactGuardedCommitDiagnostic> BaseDiagnostics(
        CanonicalGeneratedArtifactCutoverAppSeamConfiguration configuration,
        CanonicalGeneratedArtifactGuardedCommitContext context,
        CanonicalGeneratedArtifactGuardedCommitGate gate,
        CanonicalGeneratedArtifactEvidenceReport evidenceReport,
        CanonicalGeneratedArtifactN1ReadinessReport n1ReadinessReport,
        int candidateCount, int eligibleCandidateCount,
        int duplicateSuppressionCandidateCount,
        int canaryBudget, bool canaryBudgetZero, bool willExecuteNow)
    {
        var diagnostics = new List<CanonicalGeneratedArtifactGuardedCommitDiagnostic>
        {
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822SeamStarted,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, gate.Allowed ? "allowed" : "blocked", gate.Reason),
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822EvidenceReportBuilt,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, evidenceReport.Status.ToString(), evidenceReport.DiagnosticsSummary),
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822N1ReadinessReportBuilt,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, n1ReadinessReport.Status.ToString(), n1ReadinessReport.DiagnosticsSummary),
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822GateEvaluated,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, gate.Result.ToString(), gate.Allowed ? "canonicalGeneratedArtifactV822GateAllowedBudgetZero" : string.Join(",", gate.Failures.Select(f => f.ToString())))
        };

        diagnostics.Add(Diagnostic(
            gate.Allowed && canaryBudgetZero
                ? CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822GateAllowedBudgetZero
                : CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822GateBlocked,
            configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
            duplicateSuppressionCandidateCount, gate.Allowed && canaryBudgetZero ? "allowedBudgetZero" : "blocked", gate.Reason));

        if (!gate.Allowed)
            diagnostics.Add(Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822SeamBlocked,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, "blocked", string.Join(",", gate.Failures.Select(f => f.ToString()))));

        if (canaryBudgetZero)
        {
            diagnostics.AddRange(new[]
            {
                Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822CanaryBudgetZero,
                    configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                    duplicateSuppressionCandidateCount, "canaryBudgetZero", "canonicalGeneratedArtifactV822CanaryBudgetZero"),
                Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactCanaryBudgetZero,
                    configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                    duplicateSuppressionCandidateCount, "canaryBudgetZero", "canonicalGeneratedArtifactCanaryBudgetZero"),
                Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactCommitSkippedBecauseCanaryBudgetZero,
                    configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                    duplicateSuppressionCandidateCount, "commitSkipped", "canaryBudgetZero"),
                Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactDownloadSkippedBecauseCanaryBudgetZero,
                    configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                    duplicateSuppressionCandidateCount, "downloadSkipped", "canaryBudgetZero"),
                Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactApplySkippedBecauseCanaryBudgetZero,
                    configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                    duplicateSuppressionCandidateCount, "applySkipped", "canaryBudgetZero")
            });
        }

        if (gate.Allowed && !willExecuteNow)
            diagnostics.Add(Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactGateAllowedButNoExecution,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, "gateAllowedButNoExecution",
                canaryBudgetZero ? "canaryBudgetZero" : "executionDeniedForV822"));

        diagnostics.AddRange(new[]
        {
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822CommitNotExecuted,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, "commitNotExecuted", "v822GeneratedArtifactsGuardedCommitSeamNZero"),
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822DownloadNotExecuted,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, "downloadNotExecuted", "v822DoesNotCallArtifactRequest"),
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822ApplyNotExecuted,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, "applyNotExecuted", "v822DoesNotWriteGeneratedArtifact"),
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822LegacyFallbackPreserved,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, "legacyFallbackPreserved", "v822DoesNotReplaceLegacyArtifactPlan"),
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822DuplicateSuppressionNotApplied,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, "duplicateSuppressionNotApplied", "v822NZeroDoesNotSuppressLegacyDuplicates"),
            Diagnostic(CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.canonicalGeneratedArtifactV822SeamCompleted,
                configuration, context, candidateCount, eligibleCandidateCount, gate.Failures.Length, canaryBudget,
                duplicateSuppressionCandidateCount, "completed", gate.Allowed ? "nonfatalNoExecution" : "nonfatalBlocked")
        });

        return diagnostics;
    }

    private CanonicalGeneratedArtifactGuardedCommitSeamFailure[] CandidateFailures(
        CanonicalGeneratedArtifactCutoverCandidate[] candidates, CanonicalNode? peerNode)
    {
        var failures = new List<CanonicalGeneratedArtifactGuardedCommitSeamFailure>();
        foreach (var c in candidates)
        {
            if (!c.CutoverActionKind.IsExecutableApply())
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsupportedAction);
            var kind = c.ArtifactKind;
            if (kind == null)
            {
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsupportedArtifactKind);
                continue;
            }
            if (kind == CanonicalArtifact.Kind.audio)
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.audioConfusionBlocked);
            if (!CanonicalProjectionContract.GeneratedArtifactKinds.Contains(kind.Value))
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsupportedArtifactKind);
            if (c.ExpectedContentHash == null)
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.expectedHashMissing);
            if (c.ExpectedByteSize == null)
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.expectedByteSizeMissing);
            if (c.UnresolvedConflict)
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unresolvedConflict);
            if (c.ParentObjectTombstoned)
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.parentTombstoneBlocked);
            if (c.ExpectedLogicalPathToken != null && CanonicalProjectionContract.SafeLogicalPathToken(c.ExpectedLogicalPathToken) == null)
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.unsafePathBlocked);
            if (peerNode == null)
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.peerUnknown);
            else if (!c.PeerIsAuthoritative(peerNode))
                failures.Add(CanonicalGeneratedArtifactGuardedCommitSeamFailure.peerNotAuthoritative);
        }
        return new HashSet<CanonicalGeneratedArtifactGuardedCommitSeamFailure>(failures)
            .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray();
    }

    private int EligibleCandidateCount(CanonicalGeneratedArtifactCutoverCandidate[] candidates, CanonicalNode? peerNode) =>
        candidates.Count(c => CandidateFailures(new[] { c }, peerNode).Length == 0);

    private string[] DuplicateSuppressionCandidates(CanonicalGeneratedArtifactGuardedCommitContext context)
    {
        var legacyIDs = context.LegacyActionSnapshot.ActionIDSetFor(CanonicalProductionDomain.generatedArtifacts);
        return context.Candidates
            .Select(c => c.Action.ActionID)
            .Where(id => legacyIDs.Contains(id))
            .Select(id => CanonicalProductionRedaction.SafeDiagnosticText(id))
            .Where(id => id != null)
            .Cast<string>()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsCanaryBudgetZero(CanonicalGeneratedArtifactCanaryPolicy policy) =>
        policy.CanaryMaxObjectsPerSyncRun == 0
        && !policy.AllowsInternalN1Execution
        && !policy.StagePolicy.RequestedStage.IsExecutable()
        && !policy.StagePolicy.AllowCandidateExecution
        && !policy.StagePolicy.RuntimeSwitchEnabled;

    private CanonicalGeneratedArtifactGuardedCommitDiagnostic Diagnostic(
        CanonicalGeneratedArtifactGuardedCommitDiagnosticKind kind,
        CanonicalGeneratedArtifactCutoverAppSeamConfiguration configuration,
        CanonicalGeneratedArtifactGuardedCommitContext context,
        int candidateCount, int eligibleCandidateCount,
        int gateFailureCount, int canaryBudget,
        int duplicateSuppressionCandidateCount,
        string? result = null, string? reason = null,
        CanonicalHash? hash = null,
        string? objectID = null, string? artifactID = null, CanonicalArtifact.Kind? artifactKind = null) =>
        new(kind, context.SyncRunID, context.Trigger, context.NodeRole, configuration.EffectiveMode,
            objectID, artifactID, artifactKind,
            candidateCount, eligibleCandidateCount, gateFailureCount, canaryBudget,
            duplicateSuppressionCandidateCount: duplicateSuppressionCandidateCount,
            result: result, reason: reason, hash: hash);
}
