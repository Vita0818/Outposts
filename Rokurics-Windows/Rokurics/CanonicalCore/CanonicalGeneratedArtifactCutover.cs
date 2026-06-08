using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCutoverDomain
{
    generatedArtifacts
}

public static class CanonicalGeneratedArtifactCutoverDomainExtensions
{
    public static CanonicalProductionDomain ToProductionDomain(this CanonicalGeneratedArtifactCutoverDomain d) =>
        CanonicalProductionDomain.generatedArtifacts;

    public static CanonicalCutoverDomain ToCutoverDomain(this CanonicalGeneratedArtifactCutoverDomain d) =>
        CanonicalCutoverDomain.generatedArtifacts;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCutoverActionKind
{
    generatedArtifactApply,
    generatedArtifactDownloadApply,
    generatedArtifactNoOp,
    generatedArtifactConflictRecord,
    unsupported
}

public static class CanonicalGeneratedArtifactCutoverActionKindExtensions
{
    public static bool IsExecutableApply(this CanonicalGeneratedArtifactCutoverActionKind k) =>
        k == CanonicalGeneratedArtifactCutoverActionKind.generatedArtifactApply
        || k == CanonicalGeneratedArtifactCutoverActionKind.generatedArtifactDownloadApply;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCutoverFailure
{
    disabled,
    unsupportedDomain,
    unsupportedMode,
    unsupportedKind,
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
    legacyFallbackUnavailable,
    missingArtifactRequestRouteEvidence,
    productionPortUnavailable,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    rollbackVerificationMissing,
    productionRootEnabledByDefault,
    testRootMissing,
    parentTombstoned,
    peerUnknown,
    peerNotAuthoritative,
    producerAmbiguous,
    artifactIDMismatch,
    objectIDMismatch,
    expectedHashMissing,
    expectedByteSizeMissing,
    localPreviousStateMissing,
    artifactBytesMissing,
    hashMismatchBeforeApply,
    applyFailureBeforeCommit,
    applyFailureAfterPartialCommit,
    postconditionMismatch,
    rollbackFailure,
    missingInternalCanaryConfiguration,
    canaryBudgetAboveOneDenied,
    allEligibleCanaryDenied,
    activePilotNotGeneratedArtifacts,
    matrixValidationBlocked,
    defaultEnablementDenied,
    missingReadSideParallelEvidence,
    commitExecutorUnavailable,
    peerSnapshotUnavailable,
    missingCanaryStageEvidence,
    canaryStageBlocked,
    canaryStageOrderViolation,
    observationWindowIncomplete,
    runtimeSwitchDenied,
    previousStageFailure,
    previousStageRollbackFailure,
    contentLeakRisk,
    unsafePathToken,
    audioConfusionRisk,
    hashUnavailable,
    byteSizeUnavailable
}

public sealed record CanonicalGeneratedArtifactCutoverCandidate : IEquatable<CanonicalGeneratedArtifactCutoverCandidate>
{
    public string Id => Action.ActionID;

    public CanonicalApplyAction Action { get; }
    public CanonicalRecordingObject? LocalObject { get; }
    public CanonicalRecordingObject? PeerObject { get; }
    public CanonicalArtifact? LocalArtifact { get; }
    public CanonicalArtifact? PeerArtifact { get; }
    public string? RollbackCheckpointID { get; }
    public bool UnresolvedConflict { get; }
    public string RoutePath { get; }

    public CanonicalGeneratedArtifactCutoverCandidate(
        CanonicalApplyAction action,
        CanonicalRecordingObject? localObject,
        CanonicalRecordingObject? peerObject,
        CanonicalArtifact? localArtifact = null,
        CanonicalArtifact? peerArtifact = null,
        string? rollbackCheckpointID = null,
        bool unresolvedConflict = false,
        string routePath = "/sync/artifact-request")
    {
        Action = action;
        LocalObject = localObject;
        PeerObject = peerObject;
        var targetKind = action.Target.ArtifactKind;
        LocalArtifact = localArtifact ?? Artifact(targetKind, localObject);
        PeerArtifact = peerArtifact ?? Artifact(targetKind, peerObject);
        RollbackCheckpointID = rollbackCheckpointID != null
            ? CanonicalProductionRedaction.SafeIdentifier(rollbackCheckpointID, "generated-artifact-checkpoint")
            : null;
        UnresolvedConflict = unresolvedConflict;
        RoutePath = CanonicalProductionRedaction.SafeDiagnosticText(routePath) ?? "/sync/artifact-request";
    }

    public string ObjectID => Action.Target.ObjectID;
    public string? ArtifactID => Action.Target.ArtifactID ?? PeerArtifact?.ArtifactID ?? LocalArtifact?.ArtifactID;
    public CanonicalArtifact.Kind? ArtifactKind =>
        Action.Target.ArtifactKind ?? PeerArtifact?.Kind ?? LocalArtifact?.Kind;

    public CanonicalGeneratedArtifactCutoverActionKind CutoverActionKind => Action.Kind switch
    {
        CanonicalApplyActionKind.generatedArtifactDownloadApply =>
            CanonicalGeneratedArtifactCutoverActionKind.generatedArtifactDownloadApply,
        CanonicalApplyActionKind.generatedArtifactNoOp =>
            CanonicalGeneratedArtifactCutoverActionKind.generatedArtifactNoOp,
        CanonicalApplyActionKind.conflictRecord when Action.Target.ArtifactKind != null =>
            CanonicalGeneratedArtifactCutoverActionKind.generatedArtifactConflictRecord,
        _ => CanonicalGeneratedArtifactCutoverActionKind.unsupported
    };

    public CanonicalArtifact? ExpectedArtifact => PeerArtifact;
    public CanonicalHash? ExpectedContentHash => ExpectedArtifact?.ContentHash;
    public long? ExpectedByteSize => ExpectedArtifact?.ByteSize;

    public string? ExpectedLogicalPathToken =>
        ExpectedArtifact?.LogicalPathToken
        ?? Action.Target.ArtifactKind.Map(kind =>
            CanonicalRootBoundGeneratedArtifactTarget.DefaultLogicalPathToken(ObjectID, kind));

    public string EffectiveRollbackCheckpointID =>
        RollbackCheckpointID ?? $"generated-artifact-cutover-{ObjectID}-{ArtifactKind?.ToString() ?? "unknown"}";

    public bool ParentObjectTombstoned =>
        LocalObject?.Metadata?.IsDeleted == true
        || PeerObject?.Metadata?.IsDeleted == true
        || LocalObject?.SyncState == CanonicalSyncState.deleted
        || PeerObject?.SyncState == CanonicalSyncState.deleted;

    public bool PeerIsAuthoritative(CanonicalNode? peerNode)
    {
        if (PeerArtifact == null || peerNode == null) return false;
        return CanonicalProjectionContract.IsAuthoritativeProducer(PeerArtifact, peerNode);
    }

    public static CanonicalGeneratedArtifactCutoverCandidate[] Candidates(
        CanonicalApplyPlan applyPlan,
        CanonicalManifest localManifest,
        CanonicalManifest peerManifest,
        string rollbackCheckpointPrefix = "generated-artifact-cutover")
    {
        var localObjects = localManifest.Objects.ToDictionary(o => o.ObjectID);
        var peerObjects = peerManifest.Objects.ToDictionary(o => o.ObjectID);
        return applyPlan.Actions
            .Where(a => a.Kind == CanonicalApplyActionKind.generatedArtifactDownloadApply)
            .Select(action => new CanonicalGeneratedArtifactCutoverCandidate(
                action,
                localObjects.GetValueOrDefault(action.Target.ObjectID),
                peerObjects.GetValueOrDefault(action.Target.ObjectID),
                rollbackCheckpointID:
                $"{rollbackCheckpointPrefix}-{action.Target.ObjectID}-{action.Target.ArtifactKind?.ToString() ?? "artifact"}"
            ))
            .ToArray();
    }

    private static CanonicalArtifact? Artifact(CanonicalArtifact.Kind? kind, CanonicalRecordingObject? obj)
    {
        if (kind == null) return null;
        return obj?.Artifacts.FirstOrDefault(a => a.Kind == kind.Value);
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCutoverCandidate? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactApplyPortMode
{
    disabled,
    dryRun,
    fakeInMemory,
    testRootBound,
    productionRootDisabled,
    productionRootBound,
    productionRootUnsupported
}

public static class CanonicalGeneratedArtifactApplyPortModeExtensions
{
    public static bool IsNonDryRunRootBound(this CanonicalGeneratedArtifactApplyPortMode m) =>
        m == CanonicalGeneratedArtifactApplyPortMode.testRootBound
        || m == CanonicalGeneratedArtifactApplyPortMode.productionRootBound;

    public static bool IsDefaultDisabled(this CanonicalGeneratedArtifactApplyPortMode m) =>
        m == CanonicalGeneratedArtifactApplyPortMode.disabled
        || m == CanonicalGeneratedArtifactApplyPortMode.dryRun
        || m == CanonicalGeneratedArtifactApplyPortMode.productionRootDisabled;
}

public sealed record CanonicalGeneratedArtifactCutoverEvidence : IEquatable<CanonicalGeneratedArtifactCutoverEvidence>
{
    public bool NoCommitEvidenceAvailable { get; }
    public bool RealDataShadowCopyVerified { get; }
    public bool ExecutionShadowVerified { get; }
    public bool DryRunEquivalenceVerified { get; }
    public bool NoBlockingDivergence { get; }
    public bool NoUnresolvedConflict { get; }
    public bool ArtifactRequestRouteEvidenceAvailable { get; }
    public bool ProductionPortAvailable { get; }
    public bool RealRootBoundApplyPortAvailable { get; }
    public CanonicalGeneratedArtifactApplyPortMode ApplyPortMode { get; }
    public bool RootBoundWriteAvailable { get; }
    public bool AtomicReplaceAvailable { get; }
    public bool RollbackCheckpointAvailable { get; }
    public bool RollbackVerified { get; }
    public bool ProductionRootDisabledByDefault { get; }
    public bool TestRootUsed { get; }
    public bool LegacyFallbackAvailable { get; }
    public CanonicalRollbackPlan? RollbackPlan { get; }
    public bool RollbackRehearsalPassed { get; }
    public bool ReadSideParallelEquivalent { get; }
    public CanonicalGeneratedArtifactCanaryStageEvidence? CanaryStageEvidence { get; }

    public CanonicalGeneratedArtifactCutoverEvidence(
        bool noCommitEvidenceAvailable = false,
        bool realDataShadowCopyVerified = false,
        bool executionShadowVerified = false,
        bool dryRunEquivalenceVerified = false,
        bool noBlockingDivergence = false,
        bool noUnresolvedConflict = false,
        bool artifactRequestRouteEvidenceAvailable = false,
        bool productionPortAvailable = false,
        bool realRootBoundApplyPortAvailable = false,
        CanonicalGeneratedArtifactApplyPortMode applyPortMode = CanonicalGeneratedArtifactApplyPortMode.disabled,
        bool rootBoundWriteAvailable = false,
        bool atomicReplaceAvailable = false,
        bool rollbackCheckpointAvailable = false,
        bool rollbackVerified = false,
        bool productionRootDisabledByDefault = false,
        bool testRootUsed = false,
        bool legacyFallbackAvailable = false,
        CanonicalRollbackPlan? rollbackPlan = null,
        bool rollbackRehearsalPassed = false,
        bool readSideParallelEquivalent = false,
        CanonicalGeneratedArtifactCanaryStageEvidence? canaryStageEvidence = null)
    {
        NoCommitEvidenceAvailable = noCommitEvidenceAvailable;
        RealDataShadowCopyVerified = realDataShadowCopyVerified;
        ExecutionShadowVerified = executionShadowVerified;
        DryRunEquivalenceVerified = dryRunEquivalenceVerified;
        NoBlockingDivergence = noBlockingDivergence;
        NoUnresolvedConflict = noUnresolvedConflict;
        ArtifactRequestRouteEvidenceAvailable = artifactRequestRouteEvidenceAvailable;
        ProductionPortAvailable = productionPortAvailable;
        RealRootBoundApplyPortAvailable = realRootBoundApplyPortAvailable;
        ApplyPortMode = applyPortMode;
        RootBoundWriteAvailable = rootBoundWriteAvailable;
        AtomicReplaceAvailable = atomicReplaceAvailable;
        RollbackCheckpointAvailable = rollbackCheckpointAvailable;
        RollbackVerified = rollbackVerified;
        ProductionRootDisabledByDefault = productionRootDisabledByDefault;
        TestRootUsed = testRootUsed;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        RollbackPlan = rollbackPlan;
        RollbackRehearsalPassed = rollbackRehearsalPassed;
        ReadSideParallelEquivalent = readSideParallelEquivalent;
        CanaryStageEvidence = canaryStageEvidence;
    }

    public static CanonicalGeneratedArtifactCutoverEvidence Passing(CanonicalRollbackPlan rollbackPlan) =>
        new(
            noCommitEvidenceAvailable: true,
            realDataShadowCopyVerified: true,
            executionShadowVerified: true,
            dryRunEquivalenceVerified: true,
            noBlockingDivergence: true,
            noUnresolvedConflict: true,
            artifactRequestRouteEvidenceAvailable: true,
            productionPortAvailable: true,
            realRootBoundApplyPortAvailable: true,
            applyPortMode: CanonicalGeneratedArtifactApplyPortMode.testRootBound,
            rootBoundWriteAvailable: true,
            atomicReplaceAvailable: true,
            rollbackCheckpointAvailable: true,
            rollbackVerified: true,
            productionRootDisabledByDefault: true,
            testRootUsed: true,
            legacyFallbackAvailable: true,
            rollbackPlan: rollbackPlan,
            rollbackRehearsalPassed: true,
            readSideParallelEquivalent: true
        );

    public virtual bool Equals(CanonicalGeneratedArtifactCutoverEvidence? other) =>
        other is not null && ApplyPortMode == other.ApplyPortMode;
    public override int GetHashCode() => ApplyPortMode.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCanaryStage
{
    disabled,
    n1,
    n3,
    n10,
    allEligible
}

public static class CanonicalGeneratedArtifactCanaryStageExtensions
{
    public static bool IsExecutable(this CanonicalGeneratedArtifactCanaryStage s) =>
        s != CanonicalGeneratedArtifactCanaryStage.disabled;

    public static CanonicalGeneratedArtifactCanaryStage? PreviousStage(this CanonicalGeneratedArtifactCanaryStage s) => s switch
    {
        CanonicalGeneratedArtifactCanaryStage.disabled => null,
        CanonicalGeneratedArtifactCanaryStage.n1 => CanonicalGeneratedArtifactCanaryStage.disabled,
        CanonicalGeneratedArtifactCanaryStage.n3 => CanonicalGeneratedArtifactCanaryStage.n1,
        CanonicalGeneratedArtifactCanaryStage.n10 => CanonicalGeneratedArtifactCanaryStage.n3,
        CanonicalGeneratedArtifactCanaryStage.allEligible => CanonicalGeneratedArtifactCanaryStage.n10,
        _ => null
    };

    public static int NominalCanaryBudget(this CanonicalGeneratedArtifactCanaryStage s) => s switch
    {
        CanonicalGeneratedArtifactCanaryStage.disabled => 0,
        CanonicalGeneratedArtifactCanaryStage.n1 => 1,
        CanonicalGeneratedArtifactCanaryStage.n3 => 3,
        CanonicalGeneratedArtifactCanaryStage.n10 => 10,
        CanonicalGeneratedArtifactCanaryStage.allEligible => int.MaxValue,
        _ => 0
    };

    public static int MinimumPreviousStageSuccessCount(this CanonicalGeneratedArtifactCanaryStage s) => s switch
    {
        CanonicalGeneratedArtifactCanaryStage.disabled => 0,
        CanonicalGeneratedArtifactCanaryStage.n1 => 0,
        CanonicalGeneratedArtifactCanaryStage.n3 => 1,
        CanonicalGeneratedArtifactCanaryStage.n10 => 3,
        CanonicalGeneratedArtifactCanaryStage.allEligible => 10,
        _ => 0
    };
}

public sealed record CanonicalGeneratedArtifactCanaryStagePolicy : IEquatable<CanonicalGeneratedArtifactCanaryStagePolicy>
{
    public CanonicalGeneratedArtifactCanaryStage RequestedStage { get; }
    public bool AllowCandidateExecution { get; }
    public bool RuntimeSwitchEnabled { get; }

    public CanonicalGeneratedArtifactCanaryStagePolicy(
        CanonicalGeneratedArtifactCanaryStage requestedStage = CanonicalGeneratedArtifactCanaryStage.disabled,
        bool allowCandidateExecution = false,
        bool runtimeSwitchEnabled = false)
    {
        RequestedStage = requestedStage;
        AllowCandidateExecution = allowCandidateExecution;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
    }

    public static readonly CanonicalGeneratedArtifactCanaryStagePolicy Disabled = new();
    public int CanaryBudget => RequestedStage.NominalCanaryBudget();

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryStagePolicy? other) =>
        other is not null && RequestedStage == other.RequestedStage;
    public override int GetHashCode() => RequestedStage.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactStageEvidenceStatus
{
    missing,
    incomplete,
    passed,
    failed,
    blocked
}

public static class CanonicalGeneratedArtifactStageEvidenceStatusExtensions
{
    public static bool IsPassing(this CanonicalGeneratedArtifactStageEvidenceStatus s) =>
        s == CanonicalGeneratedArtifactStageEvidenceStatus.passed;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactStageEvidenceBlocker
{
    stageDisabled,
    unsupportedDomain,
    runtimeSwitchEnabled,
    candidateExecutionNotApproved,
    previousStageEvidenceMissing,
    stageOrderViolation,
    previousStageInsufficientSuccess,
    previousStageFailure,
    previousStageRollbackFailure,
    previousStageBlockingDivergence,
    previousStageUnresolvedConflict,
    previousStagePostconditionFailure,
    previousStageUnsupportedArtifact,
    previousStageContentLeakRisk,
    previousStageUnsafePathToken,
    previousStageParentTombstone,
    previousStageAudioConfusion,
    previousStageHashUnavailable,
    previousStageByteSizeUnavailable,
    observationWindowIncomplete,
    noCommitEvidenceMissing,
    ownerApprovalMissing,
    rollbackPlanMissing,
    dryRunEquivalenceMissing,
    executionShadowMissing,
    realDataShadowCopyMissing,
    readOnlyTransportProbeMissing,
    productionApplyPortUnavailable,
    artifactRequestRouteEvidenceMissing,
    legacyFallbackUnavailable,
    readSideParallelDivergent
}

public sealed record CanonicalGeneratedArtifactStageObservationWindow : IEquatable<CanonicalGeneratedArtifactStageObservationWindow>
{
    public string ObservationWindowID { get; }
    public bool Complete { get; }

    public CanonicalGeneratedArtifactStageObservationWindow(
        string observationWindowID = "generated-artifact-stage-window", bool complete = false)
    {
        ObservationWindowID = CanonicalProductionRedaction.SafeIdentifier(
            observationWindowID, "generated-artifact-stage-window");
        Complete = complete;
    }

    public static CanonicalGeneratedArtifactStageObservationWindow Complete(string id) =>
        new(id, complete: true);

    public virtual bool Equals(CanonicalGeneratedArtifactStageObservationWindow? other) =>
        other is not null && ObservationWindowID == other.ObservationWindowID;
    public override int GetHashCode() => ObservationWindowID.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanaryStageEvidence : IEquatable<CanonicalGeneratedArtifactCanaryStageEvidence>
{
    public CanonicalGeneratedArtifactCanaryStage PreviousStage { get; }
    public CanonicalGeneratedArtifactCanaryStage RequestedStage { get; }
    public int PreviousStageSuccessCount { get; }
    public int PreviousStageFailureCount { get; }
    public int PreviousStageRollbackFailureCount { get; }
    public int PreviousStageBlockingDivergenceCount { get; }
    public int PreviousStageContentLeakRiskCount { get; }
    public int PreviousStageUnsafePathTokenCount { get; }
    public int PreviousStageParentTombstoneBlockCount { get; }
    public int PreviousStageAudioConfusionBlockCount { get; }
    public int PreviousStageSuppressedLegacyDuplicateCount { get; }
    public int PreviousStagePostconditionFailureCount { get; }
    public int PreviousStageUnsupportedArtifactCount { get; }
    public int PreviousStageHashUnavailableCount { get; }
    public int PreviousStageByteSizeUnavailableCount { get; }
    public int UnresolvedConflictCount { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus DryRunEquivalenceStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ExecutionShadowStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus RealDataShadowCopyStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ReadOnlyTransportProbeStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus NoCommitEvidenceStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus RollbackPlanStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ProductionApplyPortStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ArtifactRequestRouteEvidenceStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus LegacyFallbackStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ReadSideParallelStatus { get; }
    public string ObservationWindowID { get; }
    public bool ObservationWindowComplete { get; }
    public bool OwnerApproved { get; }

    public CanonicalGeneratedArtifactCanaryStageEvidence(
        CanonicalGeneratedArtifactCanaryStage previousStage = CanonicalGeneratedArtifactCanaryStage.disabled,
        CanonicalGeneratedArtifactCanaryStage requestedStage = CanonicalGeneratedArtifactCanaryStage.disabled,
        int previousStageSuccessCount = 0,
        int previousStageFailureCount = 0,
        int previousStageRollbackFailureCount = 0,
        int previousStageBlockingDivergenceCount = 0,
        int previousStageContentLeakRiskCount = 0,
        int previousStageUnsafePathTokenCount = 0,
        int previousStageParentTombstoneBlockCount = 0,
        int previousStageAudioConfusionBlockCount = 0,
        int previousStageSuppressedLegacyDuplicateCount = 0,
        int previousStagePostconditionFailureCount = 0,
        int previousStageUnsupportedArtifactCount = 0,
        int previousStageHashUnavailableCount = 0,
        int previousStageByteSizeUnavailableCount = 0,
        int unresolvedConflictCount = 0,
        CanonicalGeneratedArtifactStageEvidenceStatus dryRunEquivalenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus executionShadowStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus realDataShadowCopyStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus readOnlyTransportProbeStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus noCommitEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus rollbackPlanStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus productionApplyPortStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus artifactRequestRouteEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus legacyFallbackStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageEvidenceStatus readSideParallelStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactStageObservationWindow? observationWindow = null,
        bool ownerApproved = false)
    {
        var obs = observationWindow ?? new CanonicalGeneratedArtifactStageObservationWindow();
        PreviousStage = previousStage;
        RequestedStage = requestedStage;
        PreviousStageSuccessCount = Math.Max(0, previousStageSuccessCount);
        PreviousStageFailureCount = Math.Max(0, previousStageFailureCount);
        PreviousStageRollbackFailureCount = Math.Max(0, previousStageRollbackFailureCount);
        PreviousStageBlockingDivergenceCount = Math.Max(0, previousStageBlockingDivergenceCount);
        PreviousStageContentLeakRiskCount = Math.Max(0, previousStageContentLeakRiskCount);
        PreviousStageUnsafePathTokenCount = Math.Max(0, previousStageUnsafePathTokenCount);
        PreviousStageParentTombstoneBlockCount = Math.Max(0, previousStageParentTombstoneBlockCount);
        PreviousStageAudioConfusionBlockCount = Math.Max(0, previousStageAudioConfusionBlockCount);
        PreviousStageSuppressedLegacyDuplicateCount = Math.Max(0, previousStageSuppressedLegacyDuplicateCount);
        PreviousStagePostconditionFailureCount = Math.Max(0, previousStagePostconditionFailureCount);
        PreviousStageUnsupportedArtifactCount = Math.Max(0, previousStageUnsupportedArtifactCount);
        PreviousStageHashUnavailableCount = Math.Max(0, previousStageHashUnavailableCount);
        PreviousStageByteSizeUnavailableCount = Math.Max(0, previousStageByteSizeUnavailableCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        DryRunEquivalenceStatus = dryRunEquivalenceStatus;
        ExecutionShadowStatus = executionShadowStatus;
        RealDataShadowCopyStatus = realDataShadowCopyStatus;
        ReadOnlyTransportProbeStatus = readOnlyTransportProbeStatus;
        NoCommitEvidenceStatus = noCommitEvidenceStatus;
        RollbackPlanStatus = rollbackPlanStatus;
        ProductionApplyPortStatus = productionApplyPortStatus;
        ArtifactRequestRouteEvidenceStatus = artifactRequestRouteEvidenceStatus;
        LegacyFallbackStatus = legacyFallbackStatus;
        ReadSideParallelStatus = readSideParallelStatus;
        ObservationWindowID = obs.ObservationWindowID;
        ObservationWindowComplete = obs.Complete;
        OwnerApproved = ownerApproved;
    }

    public static CanonicalGeneratedArtifactCanaryStageEvidence Passing(
        CanonicalGeneratedArtifactCanaryStage previousStage,
        CanonicalGeneratedArtifactCanaryStage requestedStage,
        int previousStageSuccessCount,
        string observationWindowID) =>
        new(
            previousStage: previousStage,
            requestedStage: requestedStage,
            previousStageSuccessCount: previousStageSuccessCount,
            dryRunEquivalenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            executionShadowStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            realDataShadowCopyStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            readOnlyTransportProbeStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            noCommitEvidenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            rollbackPlanStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            productionApplyPortStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            artifactRequestRouteEvidenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            legacyFallbackStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            readSideParallelStatus: CanonicalGeneratedArtifactStageEvidenceStatus.passed,
            observationWindow: CanonicalGeneratedArtifactStageObservationWindow.Complete(observationWindowID),
            ownerApproved: true
        );

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryStageEvidence? other) =>
        other is not null && RequestedStage == other.RequestedStage && PreviousStage == other.PreviousStage;
    public override int GetHashCode() => HashCode.Combine(RequestedStage, PreviousStage);
}

public sealed record CanonicalGeneratedArtifactStageEvidenceReport : IEquatable<CanonicalGeneratedArtifactStageEvidenceReport>
{
    public CanonicalGeneratedArtifactStageEvidenceStatus Status { get; }
    public CanonicalGeneratedArtifactStageEvidenceBlocker[] Blockers { get; }
    public CanonicalGeneratedArtifactCanaryStage PreviousStage { get; }
    public CanonicalGeneratedArtifactCanaryStage RequestedStage { get; }
    public int PreviousStageSuccessCount { get; }
    public int PreviousStageFailureCount { get; }
    public int PreviousStageRollbackFailureCount { get; }
    public int PreviousStageBlockingDivergenceCount { get; }
    public int PreviousStageContentLeakRiskCount { get; }
    public int PreviousStageUnsafePathTokenCount { get; }
    public int PreviousStageParentTombstoneBlockCount { get; }
    public int PreviousStageAudioConfusionBlockCount { get; }
    public int PreviousStageSuppressedLegacyDuplicateCount { get; }
    public int PreviousStagePostconditionFailureCount { get; }
    public int PreviousStageUnsupportedArtifactCount { get; }
    public int PreviousStageHashUnavailableCount { get; }
    public int PreviousStageByteSizeUnavailableCount { get; }
    public int UnresolvedConflictCount { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus DryRunEquivalenceStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ExecutionShadowStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus RealDataShadowCopyStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ReadOnlyTransportProbeStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus NoCommitEvidenceStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus RollbackPlanStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ProductionApplyPortStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ArtifactRequestRouteEvidenceStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus LegacyFallbackStatus { get; }
    public CanonicalGeneratedArtifactStageEvidenceStatus ReadSideParallelStatus { get; }
    public string ObservationWindowID { get; }
    public bool ObservationWindowComplete { get; }
    public bool SensitiveFieldsRedacted { get; }

    public CanonicalGeneratedArtifactStageEvidenceReport(
        CanonicalGeneratedArtifactCanaryStageEvidence? evidence,
        CanonicalGeneratedArtifactCanaryStage requestedStage,
        CanonicalGeneratedArtifactStageEvidenceBlocker[] blockers)
    {
        var normalizedBlockers = new HashSet<CanonicalGeneratedArtifactStageEvidenceBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();

        if (evidence == null)
            Status = CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        else if (normalizedBlockers.Length == 0)
            Status = CanonicalGeneratedArtifactStageEvidenceStatus.passed;
        else if (evidence.ObservationWindowComplete == false)
            Status = CanonicalGeneratedArtifactStageEvidenceStatus.incomplete;
        else
            Status = CanonicalGeneratedArtifactStageEvidenceStatus.blocked;

        Blockers = normalizedBlockers;
        PreviousStage = evidence?.PreviousStage ?? (requestedStage.PreviousStage() ?? CanonicalGeneratedArtifactCanaryStage.disabled);
        RequestedStage = requestedStage;
        PreviousStageSuccessCount = evidence?.PreviousStageSuccessCount ?? 0;
        PreviousStageFailureCount = evidence?.PreviousStageFailureCount ?? 0;
        PreviousStageRollbackFailureCount = evidence?.PreviousStageRollbackFailureCount ?? 0;
        PreviousStageBlockingDivergenceCount = evidence?.PreviousStageBlockingDivergenceCount ?? 0;
        PreviousStageContentLeakRiskCount = evidence?.PreviousStageContentLeakRiskCount ?? 0;
        PreviousStageUnsafePathTokenCount = evidence?.PreviousStageUnsafePathTokenCount ?? 0;
        PreviousStageParentTombstoneBlockCount = evidence?.PreviousStageParentTombstoneBlockCount ?? 0;
        PreviousStageAudioConfusionBlockCount = evidence?.PreviousStageAudioConfusionBlockCount ?? 0;
        PreviousStageSuppressedLegacyDuplicateCount = evidence?.PreviousStageSuppressedLegacyDuplicateCount ?? 0;
        PreviousStagePostconditionFailureCount = evidence?.PreviousStagePostconditionFailureCount ?? 0;
        PreviousStageUnsupportedArtifactCount = evidence?.PreviousStageUnsupportedArtifactCount ?? 0;
        PreviousStageHashUnavailableCount = evidence?.PreviousStageHashUnavailableCount ?? 0;
        PreviousStageByteSizeUnavailableCount = evidence?.PreviousStageByteSizeUnavailableCount ?? 0;
        UnresolvedConflictCount = evidence?.UnresolvedConflictCount ?? 0;
        DryRunEquivalenceStatus = evidence?.DryRunEquivalenceStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        ExecutionShadowStatus = evidence?.ExecutionShadowStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        RealDataShadowCopyStatus = evidence?.RealDataShadowCopyStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        ReadOnlyTransportProbeStatus = evidence?.ReadOnlyTransportProbeStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        NoCommitEvidenceStatus = evidence?.NoCommitEvidenceStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        RollbackPlanStatus = evidence?.RollbackPlanStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        ProductionApplyPortStatus = evidence?.ProductionApplyPortStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        ArtifactRequestRouteEvidenceStatus = evidence?.ArtifactRequestRouteEvidenceStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        LegacyFallbackStatus = evidence?.LegacyFallbackStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        ReadSideParallelStatus = evidence?.ReadSideParallelStatus ?? CanonicalGeneratedArtifactStageEvidenceStatus.missing;
        ObservationWindowID = evidence?.ObservationWindowID ?? "missing-observation-window";
        ObservationWindowComplete = evidence?.ObservationWindowComplete ?? false;
        SensitiveFieldsRedacted = true;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"previousStage={PreviousStage}",
        $"requestedStage={RequestedStage}",
        $"successCount={PreviousStageSuccessCount}",
        $"failureCount={PreviousStageFailureCount}",
        $"rollbackFailureCount={PreviousStageRollbackFailureCount}",
        $"blockingDivergence={PreviousStageBlockingDivergenceCount}",
        $"contentLeakRisk={PreviousStageContentLeakRiskCount}",
        $"unsafePathToken={PreviousStageUnsafePathTokenCount}",
        $"parentTombstone={PreviousStageParentTombstoneBlockCount}",
        $"audioConfusion={PreviousStageAudioConfusionBlockCount}",
        $"suppressedLegacyDuplicate={PreviousStageSuppressedLegacyDuplicateCount}",
        $"postconditionFailure={PreviousStagePostconditionFailureCount}",
        $"unsupportedArtifact={PreviousStageUnsupportedArtifactCount}",
        $"hashUnavailable={PreviousStageHashUnavailableCount}",
        $"byteSizeUnavailable={PreviousStageByteSizeUnavailableCount}",
        $"unresolvedConflict={UnresolvedConflictCount}",
        $"noCommit={NoCommitEvidenceStatus}",
        $"artifactRoute={ArtifactRequestRouteEvidenceStatus}",
        $"readSideParallel={ReadSideParallelStatus}",
        $"observationComplete={ObservationWindowComplete}",
        $"blockers={string.Join("|", Blockers.Select(b => b.ToString()))}",
        $"redacted={SensitiveFieldsRedacted}"
    );

    public virtual bool Equals(CanonicalGeneratedArtifactStageEvidenceReport? other) =>
        other is not null && RequestedStage == other.RequestedStage;
    public override int GetHashCode() => RequestedStage.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanaryStageGate : IEquatable<CanonicalGeneratedArtifactCanaryStageGate>
{
    public CanonicalGeneratedArtifactCanaryStage RequestedStage { get; }
    public bool Allowed { get; }
    public int SelectedCandidateLimit { get; }
    public bool SelectsAllEligible { get; }
    public CanonicalGeneratedArtifactStageEvidenceBlocker[] Blockers { get; }
    public CanonicalGeneratedArtifactStageEvidenceReport EvidenceReport { get; }
    public string Reason { get; }

    public CanonicalGeneratedArtifactCanaryStageGate(
        CanonicalGeneratedArtifactCanaryStagePolicy policy,
        CanonicalGeneratedArtifactCutoverDomain domain,
        CanonicalCutoverToken? token,
        CanonicalGeneratedArtifactCutoverEvidence cutoverEvidence)
    {
        var requestedStage = policy.RequestedStage;
        var blockers = new List<CanonicalGeneratedArtifactStageEvidenceBlocker>();

        if (!requestedStage.IsExecutable())
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.stageDisabled);
        if (domain != CanonicalGeneratedArtifactCutoverDomain.generatedArtifacts)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.unsupportedDomain);
        if (policy.RuntimeSwitchEnabled)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.runtimeSwitchEnabled);
        if (!policy.AllowCandidateExecution)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.candidateExecutionNotApproved);
        if (token?.OwnerApproved != true && cutoverEvidence.CanaryStageEvidence?.OwnerApproved != true)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.ownerApprovalMissing);

        if (cutoverEvidence.CanaryStageEvidence == null)
        {
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageEvidenceMissing);
            RequestedStage = requestedStage;
            Allowed = false;
            SelectedCandidateLimit = 0;
            SelectsAllEligible = false;
            Blockers = new HashSet<CanonicalGeneratedArtifactStageEvidenceBlocker>(blockers)
                .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
            EvidenceReport = new CanonicalGeneratedArtifactStageEvidenceReport(null, requestedStage, Blockers);
            Reason = "generatedArtifactCanaryStageBlocked";
            return;
        }

        var evidence = cutoverEvidence.CanaryStageEvidence;
        if (evidence.RequestedStage != requestedStage
            || evidence.PreviousStage != (requestedStage.PreviousStage() ?? CanonicalGeneratedArtifactCanaryStage.disabled))
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.stageOrderViolation);

        if (evidence.PreviousStageSuccessCount < requestedStage.MinimumPreviousStageSuccessCount())
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageInsufficientSuccess);
        if (evidence.PreviousStageFailureCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageFailure);
        if (evidence.PreviousStageRollbackFailureCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageRollbackFailure);
        if (evidence.PreviousStageBlockingDivergenceCount > 0 || !cutoverEvidence.NoBlockingDivergence)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageBlockingDivergence);
        if (evidence.UnresolvedConflictCount > 0 || !cutoverEvidence.NoUnresolvedConflict)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageUnresolvedConflict);
        if (evidence.PreviousStagePostconditionFailureCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStagePostconditionFailure);
        if (evidence.PreviousStageUnsupportedArtifactCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageUnsupportedArtifact);
        if (evidence.PreviousStageContentLeakRiskCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageContentLeakRisk);
        if (evidence.PreviousStageUnsafePathTokenCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageUnsafePathToken);
        if (evidence.PreviousStageParentTombstoneBlockCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageParentTombstone);
        if (evidence.PreviousStageAudioConfusionBlockCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageAudioConfusion);
        if (evidence.PreviousStageHashUnavailableCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageHashUnavailable);
        if (evidence.PreviousStageByteSizeUnavailableCount > 0)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.previousStageByteSizeUnavailable);
        if (!evidence.ObservationWindowComplete)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.observationWindowIncomplete);
        if (!evidence.NoCommitEvidenceStatus.IsPassing() || !cutoverEvidence.NoCommitEvidenceAvailable)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.noCommitEvidenceMissing);
        if (!evidence.DryRunEquivalenceStatus.IsPassing() || !cutoverEvidence.DryRunEquivalenceVerified)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.dryRunEquivalenceMissing);
        if (!evidence.ExecutionShadowStatus.IsPassing() || !cutoverEvidence.ExecutionShadowVerified)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.executionShadowMissing);
        if (!evidence.RealDataShadowCopyStatus.IsPassing() || !cutoverEvidence.RealDataShadowCopyVerified)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.realDataShadowCopyMissing);
        if (!evidence.ReadOnlyTransportProbeStatus.IsPassing() || !cutoverEvidence.ArtifactRequestRouteEvidenceAvailable)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.readOnlyTransportProbeMissing);
        if (!evidence.RollbackPlanStatus.IsPassing()
            || cutoverEvidence.RollbackPlan?.Covers(CanonicalCutoverDomain.generatedArtifacts) != true
            || !cutoverEvidence.RollbackRehearsalPassed
            || !cutoverEvidence.RollbackVerified)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.rollbackPlanMissing);
        if (!evidence.ProductionApplyPortStatus.IsPassing()
            || !cutoverEvidence.ProductionPortAvailable
            || !cutoverEvidence.RealRootBoundApplyPortAvailable
            || !cutoverEvidence.ApplyPortMode.IsNonDryRunRootBound()
            || !cutoverEvidence.RootBoundWriteAvailable
            || !cutoverEvidence.AtomicReplaceAvailable
            || !cutoverEvidence.RollbackCheckpointAvailable)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.productionApplyPortUnavailable);
        if (!evidence.ArtifactRequestRouteEvidenceStatus.IsPassing() || !cutoverEvidence.ArtifactRequestRouteEvidenceAvailable)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.artifactRequestRouteEvidenceMissing);
        if (!evidence.LegacyFallbackStatus.IsPassing() || !cutoverEvidence.LegacyFallbackAvailable)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.legacyFallbackUnavailable);
        if (!evidence.ReadSideParallelStatus.IsPassing() || !cutoverEvidence.ReadSideParallelEquivalent)
            blockers.Add(CanonicalGeneratedArtifactStageEvidenceBlocker.readSideParallelDivergent);

        RequestedStage = requestedStage;
        var uniqueBlockers = new HashSet<CanonicalGeneratedArtifactStageEvidenceBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        Allowed = uniqueBlockers.Length == 0;
        SelectedCandidateLimit = requestedStage.NominalCanaryBudget();
        SelectsAllEligible = requestedStage == CanonicalGeneratedArtifactCanaryStage.allEligible;
        Blockers = uniqueBlockers;
        EvidenceReport = new CanonicalGeneratedArtifactStageEvidenceReport(evidence, requestedStage, uniqueBlockers);
        Reason = Allowed ? "generatedArtifactCanaryStageAllowed" : "generatedArtifactCanaryStageBlocked";
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryStageGate? other) =>
        other is not null && RequestedStage == other.RequestedStage;
    public override int GetHashCode() => RequestedStage.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanaryPolicy : IEquatable<CanonicalGeneratedArtifactCanaryPolicy>
{
    public CanonicalGeneratedArtifactCanaryStagePolicy StagePolicy { get; }
    public int CanaryMaxObjectsPerSyncRun { get; }
    public bool AllowsInternalN1Execution { get; }
    public bool ExplicitInternalTestConfiguration { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool AllowAllEligible { get; }

    public CanonicalGeneratedArtifactCanaryPolicy(
        CanonicalGeneratedArtifactCanaryStagePolicy? stagePolicy = null,
        int canaryMaxObjectsPerSyncRun = 0,
        bool allowsInternalN1Execution = false,
        bool explicitInternalTestConfiguration = false,
        bool runtimeSwitchEnabled = false,
        bool allowAllEligible = false)
    {
        StagePolicy = stagePolicy ?? CanonicalGeneratedArtifactCanaryStagePolicy.Disabled;
        CanaryMaxObjectsPerSyncRun = Math.Max(0, canaryMaxObjectsPerSyncRun);
        AllowsInternalN1Execution = allowsInternalN1Execution;
        ExplicitInternalTestConfiguration = explicitInternalTestConfiguration;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        AllowAllEligible = allowAllEligible;
    }

    public static readonly CanonicalGeneratedArtifactCanaryPolicy Disabled = new();

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryPolicy? other) =>
        other is not null && StagePolicy.Equals(other.StagePolicy);
    public override int GetHashCode() => StagePolicy.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCanaryMode
{
    disabled,
    n1
}

public static class CanonicalGeneratedArtifactCanaryModeExtensions
{
    public static bool IsExecutable(this CanonicalGeneratedArtifactCanaryMode m) =>
        m == CanonicalGeneratedArtifactCanaryMode.n1;
}

public sealed record CanonicalGeneratedArtifactCanaryConfiguration : IEquatable<CanonicalGeneratedArtifactCanaryConfiguration>
{
    public CanonicalGeneratedArtifactCanaryMode Mode { get; }
    public CanonicalMigrationDomain Domain { get; }
    public int CanaryMaxObjectsPerSyncRun { get; }
    public bool ExplicitInternalTestConfiguration { get; }
    public bool ProductionTokenRequired { get; }
    public bool OwnerApprovalRequired { get; }
    public bool RollbackPlanRequired { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool AllowAllEligible { get; }
    public bool ReleaseDefaultEnabled { get; }

    public CanonicalGeneratedArtifactCanaryConfiguration(
        CanonicalGeneratedArtifactCanaryMode mode = CanonicalGeneratedArtifactCanaryMode.disabled,
        CanonicalMigrationDomain domain = CanonicalMigrationDomain.generatedArtifacts,
        int canaryMaxObjectsPerSyncRun = 0,
        bool explicitInternalTestConfiguration = false,
        bool productionTokenRequired = true,
        bool ownerApprovalRequired = true,
        bool rollbackPlanRequired = true,
        bool runtimeSwitchEnabled = false,
        bool allowAllEligible = false,
        bool releaseDefaultEnabled = false)
    {
        Mode = mode;
        Domain = domain;
        CanaryMaxObjectsPerSyncRun = Math.Max(0, canaryMaxObjectsPerSyncRun);
        ExplicitInternalTestConfiguration = explicitInternalTestConfiguration;
        ProductionTokenRequired = productionTokenRequired;
        OwnerApprovalRequired = ownerApprovalRequired;
        RollbackPlanRequired = rollbackPlanRequired;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        AllowAllEligible = allowAllEligible;
        ReleaseDefaultEnabled = releaseDefaultEnabled;
    }

    public static readonly CanonicalGeneratedArtifactCanaryConfiguration Disabled = new();

    public static CanonicalGeneratedArtifactCanaryConfiguration InternalN1(
        bool explicitInternalTestConfiguration = true) =>
        new(
            CanonicalGeneratedArtifactCanaryMode.n1,
            canaryMaxObjectsPerSyncRun: 1,
            explicitInternalTestConfiguration: explicitInternalTestConfiguration
        );

    public CanonicalGeneratedArtifactCanaryConfiguration(CanonicalGeneratedArtifactCutoverAppSeamConfiguration configuration)
    {
        var policy = configuration.Policy.CanaryPolicy;
        var n1Requested = configuration.IsEnabled
                          && configuration.EffectiveMode == CanonicalCutoverAppSeamMode.canaryCommit
                          && policy.CanaryMaxObjectsPerSyncRun == 1;
        Mode = n1Requested ? CanonicalGeneratedArtifactCanaryMode.n1 : CanonicalGeneratedArtifactCanaryMode.disabled;
        Domain = CanonicalMigrationDomain.generatedArtifacts;
        CanaryMaxObjectsPerSyncRun = policy.CanaryMaxObjectsPerSyncRun;
        ExplicitInternalTestConfiguration = policy.ExplicitInternalTestConfiguration;
        RuntimeSwitchEnabled = policy.RuntimeSwitchEnabled || policy.StagePolicy.RuntimeSwitchEnabled;
        AllowAllEligible = policy.AllowAllEligible || policy.StagePolicy.RequestedStage == CanonicalGeneratedArtifactCanaryStage.allEligible;
        ReleaseDefaultEnabled = false;
        ProductionTokenRequired = true;
        OwnerApprovalRequired = true;
        RollbackPlanRequired = true;
    }

    public bool StrictN1Enabled =>
        Mode == CanonicalGeneratedArtifactCanaryMode.n1
        && Domain == CanonicalMigrationDomain.generatedArtifacts
        && CanaryMaxObjectsPerSyncRun == 1
        && ExplicitInternalTestConfiguration
        && !RuntimeSwitchEnabled
        && !AllowAllEligible
        && !ReleaseDefaultEnabled;

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryConfiguration? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

// ─── Canary Blocker / Safety / Selection ───────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCanaryBlocker
{
    disabled, unsupportedMode, canaryBudgetZero, missingInternalCanaryConfiguration,
    canaryBudgetAboveOneDenied, canaryStageEvidenceMissing, canaryStageBlocked,
    unsupportedTrigger, unsupportedAction, unsupportedKind, missingOwnerApproval,
    matrixBlocked, activePilotNotGeneratedArtifacts, commitExecutorUnavailable,
    peerSnapshotUnavailable, runtimeSwitchDenied, allEligibleDenied, defaultEnablementDenied,
    readSideParallelMissing, hashUnavailable, byteSizeUnavailable,
    unsafeLogicalPathToken, contentLeakRisk, audioConfusionRisk, producerAmbiguous,
    generatedArtifactUploadDenied, unsupportedRoute, rollbackCheckpointMissing,
    insufficientEvidence, unresolvedConflict, parentTombstoned, noRollbackCheckpoint,
    realApplyPortUnavailable, peerNotAuthoritative, alreadyAttemptedFailedCandidate,
    noEligibleCandidate
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCanaryCandidateSafetyKind
{
    summaryJSONMetadataAdjacent, noteJSONMetadataAdjacent, noteMarkdownGeneratedText,
    transcriptJSONStructured, transcriptMarkdownFullText, blocked
}

public sealed record CanonicalGeneratedArtifactCanaryCandidate : IEquatable<CanonicalGeneratedArtifactCanaryCandidate>
{
    public string Id => CutoverCandidate.Action.ActionID;
    public CanonicalGeneratedArtifactCutoverCandidate CutoverCandidate { get; }
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }
    public string? HashPrefix { get; }

    public CanonicalGeneratedArtifactCanaryCandidate(CanonicalGeneratedArtifactCutoverCandidate cutoverCandidate)
    {
        CutoverCandidate = cutoverCandidate;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(cutoverCandidate.ObjectID, "unknown-recording");
        ArtifactID = cutoverCandidate.ArtifactID != null
            ? CanonicalProductionRedaction.SafeIdentifier(cutoverCandidate.ArtifactID, "artifact:unknown") : null;
        ArtifactKind = cutoverCandidate.ArtifactKind;
        HashPrefix = cutoverCandidate.ExpectedContentHash != null
            ? CanonicalProductionRedaction.HashPrefix(cutoverCandidate.ExpectedContentHash.Value) : null;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryCandidate? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanaryCandidateSafety : IEquatable<CanonicalGeneratedArtifactCanaryCandidateSafety>
{
    public CanonicalGeneratedArtifactCanaryCandidate Candidate { get; }
    public bool Safe { get; }
    public CanonicalGeneratedArtifactCanaryCandidateSafetyKind Kind { get; }
    public CanonicalGeneratedArtifactCanaryBlocker[] Blockers { get; }
    public bool GeneratedArtifactDownloadOnly { get; }
    public bool GeneratedArtifactUploadAttempted { get; }
    public bool AudioUploadAttempted { get; }
    public bool ContentLeakRisk { get; }
    public bool RouteIsArtifactRequest { get; }
    public bool ReadSideOnly { get; }
    public bool UiMutated { get; }

    public CanonicalGeneratedArtifactCanaryCandidateSafety(
        CanonicalGeneratedArtifactCutoverCandidate candidate,
        CanonicalGeneratedArtifactCutoverEvidence evidence,
        CanonicalNode? peerNode,
        HashSet<string>? attemptedFailedActionIDs = null,
        bool contentLeakRiskObserved = false)
    {
        attemptedFailedActionIDs ??= new HashSet<string>();
        var blockers = CanonicalGeneratedArtifactCanarySelector.CandidateBlockers(
            candidate, evidence, peerNode, attemptedFailedActionIDs).ToList();

        var safetyKind = candidate.ArtifactKind switch
        {
            CanonicalArtifact.Kind.summaryJSON => CanonicalGeneratedArtifactCanaryCandidateSafetyKind.summaryJSONMetadataAdjacent,
            CanonicalArtifact.Kind.noteJSON => CanonicalGeneratedArtifactCanaryCandidateSafetyKind.noteJSONMetadataAdjacent,
            CanonicalArtifact.Kind.noteMarkdown => CanonicalGeneratedArtifactCanaryCandidateSafetyKind.noteMarkdownGeneratedText,
            CanonicalArtifact.Kind.transcriptJSON => CanonicalGeneratedArtifactCanaryCandidateSafetyKind.transcriptJSONStructured,
            CanonicalArtifact.Kind.transcriptMarkdown => CanonicalGeneratedArtifactCanaryCandidateSafetyKind.transcriptMarkdownFullText,
            CanonicalArtifact.Kind.audio => CanonicalGeneratedArtifactCanaryCandidateSafetyKind.blocked,
            _ => CanonicalGeneratedArtifactCanaryCandidateSafetyKind.blocked
        };

        if (candidate.ArtifactKind == CanonicalArtifact.Kind.audio)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.audioConfusionRisk);
        if (candidate.ArtifactKind is null or CanonicalArtifact.Kind.metadata or CanonicalArtifact.Kind.receiveRecord)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.unsupportedKind);
        if (contentLeakRiskObserved)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.contentLeakRisk);

        Candidate = new CanonicalGeneratedArtifactCanaryCandidate(candidate);
        Blockers = new HashSet<CanonicalGeneratedArtifactCanaryBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        Safe = Blockers.Length == 0;
        Kind = Safe ? safetyKind : CanonicalGeneratedArtifactCanaryCandidateSafetyKind.blocked;
        GeneratedArtifactDownloadOnly = candidate.CutoverActionKind == CanonicalGeneratedArtifactCutoverActionKind.generatedArtifactDownloadApply;
        GeneratedArtifactUploadAttempted = candidate.CutoverActionKind != CanonicalGeneratedArtifactCutoverActionKind.generatedArtifactDownloadApply;
        AudioUploadAttempted = candidate.ArtifactKind == CanonicalArtifact.Kind.audio;
        ContentLeakRisk = contentLeakRiskObserved;
        RouteIsArtifactRequest = candidate.RoutePath == "/sync/artifact-request";
        ReadSideOnly = true;
        UiMutated = false;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryCandidateSafety? other) =>
        other is not null && Candidate.Id == other.Candidate.Id;
    public override int GetHashCode() => Candidate.Id.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanarySelectionBlocker : IEquatable<CanonicalGeneratedArtifactCanarySelectionBlocker>
{
    public string Id => string.Join("|", ObjectID ?? "run", ArtifactID ?? "artifact", Reason.ToString());
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalGeneratedArtifactCanaryBlocker Reason { get; }

    public CanonicalGeneratedArtifactCanarySelectionBlocker(
        string? objectID, string? artifactID, CanonicalGeneratedArtifactCanaryBlocker reason)
    {
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording") : null;
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        Reason = reason;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanarySelectionBlocker? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanarySelectionResult : IEquatable<CanonicalGeneratedArtifactCanarySelectionResult>
{
    public CanonicalGeneratedArtifactCanaryCandidate[] SelectedCandidates { get; }
    public CanonicalGeneratedArtifactCanarySelectionBlocker[] Blockers { get; }
    public int EvaluatedCandidateCount { get; }
    public bool NoEligibleCandidate { get; }

    public CanonicalGeneratedArtifactCutoverCandidate[] SelectedCutoverCandidates =>
        SelectedCandidates.Select(c => c.CutoverCandidate).ToArray();

    public CanonicalGeneratedArtifactCanarySelectionResult(
        CanonicalGeneratedArtifactCanaryCandidate[] selectedCandidates,
        CanonicalGeneratedArtifactCanarySelectionBlocker[] blockers,
        int evaluatedCandidateCount,
        bool noEligibleCandidate)
    {
        SelectedCandidates = selectedCandidates;
        Blockers = blockers;
        EvaluatedCandidateCount = evaluatedCandidateCount;
        NoEligibleCandidate = noEligibleCandidate;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanarySelectionResult? other) =>
        other is not null && NoEligibleCandidate == other.NoEligibleCandidate;
    public override int GetHashCode() => NoEligibleCandidate.GetHashCode();
}

public class CanonicalGeneratedArtifactCanarySelector
{
    public CanonicalGeneratedArtifactCanarySelectionResult Select(
        CanonicalCutoverMode mode,
        CanonicalGeneratedArtifactCanaryPolicy policy,
        CanonicalSyncPlanTrigger trigger,
        CanonicalGeneratedArtifactCutoverEvidence evidence,
        CanonicalNode? peerNode,
        CanonicalGeneratedArtifactCutoverCandidate[] candidates,
        HashSet<string>? attemptedFailedActionIDs = null)
    {
        attemptedFailedActionIDs ??= new HashSet<string>();
        var blockers = new List<CanonicalGeneratedArtifactCanarySelectionBlocker>();
        var usesStagePolicy = policy.StagePolicy.RequestedStage.IsExecutable();
        CanonicalGeneratedArtifactCanaryStageGate? stageGate = usesStagePolicy
            ? new CanonicalGeneratedArtifactCanaryStageGate(policy.StagePolicy,
                CanonicalGeneratedArtifactCutoverDomain.generatedArtifacts, null, evidence)
            : null;

        if (mode == CanonicalCutoverMode.disabled)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.disabled));
        if (mode != CanonicalCutoverMode.canary)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.unsupportedMode));
        if (policy.CanaryMaxObjectsPerSyncRun == 0 && !usesStagePolicy)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.canaryBudgetZero));
        if (policy.CanaryMaxObjectsPerSyncRun > 1 && !usesStagePolicy)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.canaryBudgetAboveOneDenied));
        if (policy.CanaryMaxObjectsPerSyncRun == 1 && !usesStagePolicy && !policy.AllowsInternalN1Execution)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.missingInternalCanaryConfiguration));
        if (usesStagePolicy && policy.StagePolicy.RequestedStage == CanonicalGeneratedArtifactCanaryStage.allEligible
            && !policy.AllowAllEligible)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.allEligibleDenied));
        if (!usesStagePolicy && (policy.AllowAllEligible
            || policy.StagePolicy.RequestedStage == CanonicalGeneratedArtifactCanaryStage.allEligible))
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.allEligibleDenied));
        if (policy.RuntimeSwitchEnabled || policy.StagePolicy.RuntimeSwitchEnabled)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.runtimeSwitchDenied));
        if (usesStagePolicy && stageGate?.Allowed != true)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                evidence.CanaryStageEvidence == null
                    ? CanonicalGeneratedArtifactCanaryBlocker.canaryStageEvidenceMissing
                    : CanonicalGeneratedArtifactCanaryBlocker.canaryStageBlocked));
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh || trigger == CanonicalSyncPlanTrigger.retryDrainer)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.unsupportedTrigger));

        var runBlocked = blockers.Count > 0;
        var selectionLimit = usesStagePolicy
            ? (stageGate?.SelectedCandidateLimit ?? 0)
            : policy.CanaryMaxObjectsPerSyncRun;

        var ordered = candidates.OrderBy(c => ArtifactSelectionPriority(c.ArtifactKind))
            .ThenBy(c => c.ObjectID, StringComparer.Ordinal)
            .ThenBy(c => c.ArtifactKind?.ToString() ?? "", StringComparer.Ordinal)
            .ThenBy(c => c.Action.ActionID, StringComparer.Ordinal).ToArray();

        var selected = new List<CanonicalGeneratedArtifactCanaryCandidate>();
        foreach (var candidate in ordered)
        {
            var reasons = CandidateBlockers(candidate, evidence, peerNode, attemptedFailedActionIDs);
            if (reasons.Length == 0 && !runBlocked && selected.Count < selectionLimit)
            {
                selected.Add(new CanonicalGeneratedArtifactCanaryCandidate(candidate));
                continue;
            }
            blockers.AddRange(reasons.Select(r => new CanonicalGeneratedArtifactCanarySelectionBlocker(
                candidate.ObjectID, candidate.ArtifactID, r)));
        }

        if (selected.Count == 0 && candidates.Length > 0 && blockers.Count == 0)
            blockers.Add(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
                CanonicalGeneratedArtifactCanaryBlocker.noEligibleCandidate));

        return new CanonicalGeneratedArtifactCanarySelectionResult(
            selected.ToArray(), blockers.ToArray(), candidates.Length, selected.Count == 0);
    }

    public static CanonicalGeneratedArtifactCanaryBlocker[] CandidateBlockers(
        CanonicalGeneratedArtifactCutoverCandidate candidate,
        CanonicalGeneratedArtifactCutoverEvidence evidence,
        CanonicalNode? peerNode,
        HashSet<string> attemptedFailedActionIDs)
    {
        var blockers = new List<CanonicalGeneratedArtifactCanaryBlocker>();
        if (!candidate.CutoverActionKind.IsExecutableApply())
            blockers.Add(candidate.CutoverActionKind == CanonicalGeneratedArtifactCutoverActionKind.generatedArtifactNoOp
                ? CanonicalGeneratedArtifactCanaryBlocker.generatedArtifactUploadDenied
                : CanonicalGeneratedArtifactCanaryBlocker.unsupportedAction);
        if (candidate.Action.Kind != CanonicalApplyActionKind.generatedArtifactDownloadApply)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.generatedArtifactUploadDenied);
        if (candidate.ArtifactKind == CanonicalArtifact.Kind.audio)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.audioConfusionRisk);
        if (candidate.ArtifactKind == null || !CanonicalProjectionContract.GeneratedArtifactKinds.Contains(candidate.ArtifactKind.Value))
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.unsupportedKind);
        if (candidate.ExpectedArtifact == null)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.producerAmbiguous);
        if (candidate.ExpectedContentHash == null)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.hashUnavailable);
        if (candidate.ExpectedByteSize == null)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.byteSizeUnavailable);
        if (candidate.ExpectedLogicalPathToken != null
            && CanonicalProjectionContract.SafeLogicalPathToken(candidate.ExpectedLogicalPathToken) == null)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.unsafeLogicalPathToken);
        if (candidate.UnresolvedConflict)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.unresolvedConflict);
        if (candidate.ParentObjectTombstoned)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.parentTombstoned);
        if (candidate.RollbackCheckpointID == null || !evidence.RollbackCheckpointAvailable)
        {
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.noRollbackCheckpoint);
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.rollbackCheckpointMissing);
        }
        if (!evidence.RealRootBoundApplyPortAvailable || !evidence.ApplyPortMode.IsNonDryRunRootBound()
            || !evidence.RootBoundWriteAvailable || !evidence.AtomicReplaceAvailable)
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.realApplyPortUnavailable);
        if (!candidate.PeerIsAuthoritative(peerNode))
        {
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.peerNotAuthoritative);
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.producerAmbiguous);
        }
        if (candidate.RoutePath != "/sync/artifact-request")
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.unsupportedRoute);
        if (attemptedFailedActionIDs.Contains(candidate.Action.ActionID))
            blockers.Add(CanonicalGeneratedArtifactCanaryBlocker.alreadyAttemptedFailedCandidate);

        return new HashSet<CanonicalGeneratedArtifactCanaryBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static int ArtifactSelectionPriority(CanonicalArtifact.Kind? kind) => kind switch
    {
        CanonicalArtifact.Kind.summaryJSON => 0,
        CanonicalArtifact.Kind.noteJSON => 1,
        CanonicalArtifact.Kind.noteMarkdown => 2,
        CanonicalArtifact.Kind.transcriptJSON => 3,
        CanonicalArtifact.Kind.transcriptMarkdown => 4,
        CanonicalArtifact.Kind.audio => 90,
        _ => 99
    };
}

// ─── Cutover Gate / Commit Result / Rollback ──────────────────────────

public sealed record CanonicalGeneratedArtifactCutoverGate : IEquatable<CanonicalGeneratedArtifactCutoverGate>
{
    public CanonicalGeneratedArtifactCutoverDomain Domain { get; }
    public CanonicalCutoverMode Mode { get; }
    public bool Allowed { get; }
    public CanonicalGeneratedArtifactCutoverFailure[] Failures { get; }
    public bool LegacyFallbackAvailable { get; }
    public string Reason { get; }

    public CanonicalGeneratedArtifactCutoverGate(
        CanonicalGeneratedArtifactCutoverDomain domain, CanonicalCutoverMode mode,
        CanonicalGeneratedArtifactCutoverFailure[] failures, bool legacyFallbackAvailable, string reason)
    {
        Domain = domain;
        Mode = mode;
        Failures = new HashSet<CanonicalGeneratedArtifactCutoverFailure>(failures)
            .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray();
        Allowed = Failures.Length == 0;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (Allowed ? "allowed" : "blocked");
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCutoverGate? other) =>
        other is not null && Domain == other.Domain;
    public override int GetHashCode() => Domain.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCommitFailureInjection
{
    none, artifactBytesMissing, hashMismatchBeforeApply, applyFailureBeforeCommit,
    applyFailureAfterPartialCommit, postconditionMismatch, rollbackFailure,
    parentTombstoned, producerAmbiguous, unsupportedKind
}

public sealed record CanonicalGeneratedArtifactProductionCommitResult : IEquatable<CanonicalGeneratedArtifactProductionCommitResult>
{
    public string ActionID { get; }
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }
    public CanonicalGeneratedArtifactCutoverActionKind ActionKind { get; }
    public bool Committed { get; }
    public bool PartialCommit { get; }
    public bool PreconditionVerified { get; }
    public bool PostconditionVerified { get; }
    public string? ContentHashPrefix { get; }
    public long? ByteSize { get; }
    public CanonicalGeneratedArtifactCutoverFailure? FailureKind { get; }
    public string Reason { get; }

    public CanonicalGeneratedArtifactProductionCommitResult(
        string actionID, string objectID, string? artifactID,
        CanonicalArtifact.Kind? artifactKind, CanonicalGeneratedArtifactCutoverActionKind actionKind,
        bool committed, bool partialCommit = false, bool preconditionVerified = true,
        bool postconditionVerified = true, CanonicalHash? contentHash = null,
        long? byteSize = null, CanonicalGeneratedArtifactCutoverFailure? failureKind = null,
        string reason = "")
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, actionKind.ToString());
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        ArtifactKind = artifactKind;
        ActionKind = actionKind;
        Committed = committed;
        PartialCommit = partialCommit;
        PreconditionVerified = preconditionVerified;
        PostconditionVerified = postconditionVerified;
        ContentHashPrefix = contentHash != null ? CanonicalProductionRedaction.HashPrefix(contentHash.Value) : null;
        ByteSize = byteSize;
        FailureKind = failureKind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (committed ? "committed" : "failed");
    }

    public static CanonicalGeneratedArtifactProductionCommitResult Success(
        CanonicalGeneratedArtifactCutoverCandidate candidate) =>
        new(candidate.Action.ActionID, candidate.ObjectID, candidate.ArtifactID,
            candidate.ArtifactKind, candidate.CutoverActionKind, true,
            contentHash: candidate.ExpectedContentHash, byteSize: candidate.ExpectedByteSize,
            reason: "generatedArtifactApplyCommitted");

    public static CanonicalGeneratedArtifactProductionCommitResult Failure(
        CanonicalGeneratedArtifactCutoverCandidate candidate,
        CanonicalGeneratedArtifactCutoverFailure failureKind, bool partialCommit = false, string reason = "") =>
        new(candidate.Action.ActionID, candidate.ObjectID, candidate.ArtifactID,
            candidate.ArtifactKind, candidate.CutoverActionKind, false,
            partialCommit,
            failureKind != CanonicalGeneratedArtifactCutoverFailure.objectIDMismatch
                && failureKind != CanonicalGeneratedArtifactCutoverFailure.artifactIDMismatch
                && failureKind != CanonicalGeneratedArtifactCutoverFailure.expectedHashMissing
                && failureKind != CanonicalGeneratedArtifactCutoverFailure.expectedByteSizeMissing
                && failureKind != CanonicalGeneratedArtifactCutoverFailure.parentTombstoned
                && failureKind != CanonicalGeneratedArtifactCutoverFailure.producerAmbiguous
                && failureKind != CanonicalGeneratedArtifactCutoverFailure.unsupportedKind,
            failureKind != CanonicalGeneratedArtifactCutoverFailure.postconditionMismatch,
            contentHash: candidate.ExpectedContentHash, byteSize: candidate.ExpectedByteSize,
            failureKind: failureKind, reason: reason);

    public virtual bool Equals(CanonicalGeneratedArtifactProductionCommitResult? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactRollbackExecutionResult : IEquatable<CanonicalGeneratedArtifactRollbackExecutionResult>
{
    public string CheckpointID { get; }
    public bool Succeeded { get; }
    public bool Fatal { get; }
    public string Reason { get; }

    public CanonicalGeneratedArtifactRollbackExecutionResult(
        string checkpointID, bool succeeded, bool fatal = false, string reason = "")
    {
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "generated-artifact-checkpoint");
        Succeeded = succeeded;
        Fatal = fatal;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (succeeded ? "rollbackCompleted" : "rollbackFailed");
    }

    public virtual bool Equals(CanonicalGeneratedArtifactRollbackExecutionResult? other) =>
        other is not null && CheckpointID == other.CheckpointID;
    public override int GetHashCode() => CheckpointID.GetHashCode();
}

public interface ICanonicalGeneratedArtifactCutoverExecutor
{
    Task<CanonicalGeneratedArtifactProductionCommitResult> CommitGeneratedArtifact(
        CanonicalGeneratedArtifactCutoverCandidate candidate);
    Task<CanonicalGeneratedArtifactRollbackExecutionResult> RollbackGeneratedArtifact(
        CanonicalGeneratedArtifactCutoverCandidate candidate, CanonicalGeneratedArtifactCutoverFailure reason);
}

// ─── Diagnostic / Cutover Result ──────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCutoverDiagnosticKind
{
    canonicalGeneratedArtifactCutoverGateEvaluated, canonicalGeneratedArtifactCutoverGateBlocked,
    canonicalGeneratedArtifactCutoverGateAllowed, canonicalGeneratedArtifactNoCommitStarted,
    canonicalGeneratedArtifactNoCommitCompleted, canonicalGeneratedArtifactCommitStarted,
    canonicalGeneratedArtifactCommitCompleted, canonicalGeneratedArtifactCommitFailed,
    canonicalGeneratedArtifactRollbackStarted, canonicalGeneratedArtifactRollbackCompleted,
    canonicalGeneratedArtifactRollbackFailed, canonicalGeneratedArtifactCanaryStarted,
    canonicalGeneratedArtifactCanaryCompleted, canonicalGeneratedArtifactDuplicateLegacySuppressed,
    canonicalGeneratedArtifactLegacyFallbackUsed, canonicalGeneratedArtifactLegacyFallbackPreserved,
    canonicalGeneratedArtifactParentTombstoneBlocked, canonicalGeneratedArtifactConflictBlocked,
    canonicalGeneratedArtifactReadSideParallelEquivalent, canonicalGeneratedArtifactReadSideParallelDivergent,
    canonicalGeneratedArtifactUIProjectionParallelReadStarted,
    canonicalGeneratedArtifactUIProjectionParallelReadEquivalent,
    canonicalGeneratedArtifactUIProjectionParallelReadDivergent,
    canonicalGeneratedArtifactN1CanaryConfigured, canonicalGeneratedArtifactN1CandidateSelectionStarted,
    canonicalGeneratedArtifactN1CandidateSelected, canonicalGeneratedArtifactN1NoEligibleCandidate,
    canonicalGeneratedArtifactN1CandidateBlocked, canonicalGeneratedArtifactN1CanaryStarted,
    canonicalGeneratedArtifactN1CommitStarted, canonicalGeneratedArtifactN1CommitCompleted,
    canonicalGeneratedArtifactN1CommitFailed, canonicalGeneratedArtifactN1PostconditionVerified,
    canonicalGeneratedArtifactN1PostconditionFailed, canonicalGeneratedArtifactN1RollbackStarted,
    canonicalGeneratedArtifactN1RollbackCompleted, canonicalGeneratedArtifactN1RollbackFailed,
    canonicalGeneratedArtifactN1LegacyFallbackUsed, canonicalGeneratedArtifactN1DuplicateLegacySuppressed,
    canonicalGeneratedArtifactN1FatalBlocker, canonicalGeneratedArtifactN1ObservationRecorded,
    canonicalGeneratedArtifactN1ReadSideParallelStarted,
    canonicalGeneratedArtifactN1ReadSideParallelEquivalent,
    canonicalGeneratedArtifactN1ReadSideParallelDivergent,
    canonicalGeneratedArtifactN1MacPeerSnapshotUnavailable,
    canonicalGeneratedArtifactCanaryStageEvaluated, canonicalGeneratedArtifactCanaryStageBlocked,
    canonicalGeneratedArtifactCanaryStageAllowed, canonicalGeneratedArtifactCanaryStageStarted,
    canonicalGeneratedArtifactCanaryStageCompleted, canonicalGeneratedArtifactCanaryStageFailed,
    canonicalGeneratedArtifactCanaryStageObservationRecorded,
    canonicalGeneratedArtifactCanaryCandidateSkipped, canonicalGeneratedArtifactCanaryCandidateExecuted,
    canonicalGeneratedArtifactCanaryStoppedAfterFailure, canonicalGeneratedArtifactCanaryNextStageEligible,
    canonicalGeneratedArtifactCanaryNextStageBlocked, canonicalGeneratedArtifactCanaryAllEligibleStarted,
    canonicalGeneratedArtifactCanaryAllEligibleCompleted, canonicalGeneratedArtifactContentLeakBlocked,
    canonicalGeneratedArtifactUnsafePathTokenBlocked, canonicalGeneratedArtifactAudioConfusionBlocked,
    canonicalGeneratedArtifactUnsupportedKindBlocked, canonicalGeneratedArtifactProducerAmbiguousBlocked,
    canonicalGeneratedArtifactHashUnavailableBlocked, canonicalGeneratedArtifactByteSizeUnavailableBlocked,
    canonicalGeneratedArtifactExpandedReadSideParallelStarted,
    canonicalGeneratedArtifactExpandedReadSideParallelEquivalent,
    canonicalGeneratedArtifactExpandedReadSideParallelDivergent
}

public sealed record CanonicalGeneratedArtifactCutoverDiagnostic : IEquatable<CanonicalGeneratedArtifactCutoverDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", ArtifactID ?? "", Result ?? "", Reason ?? "");
    public CanonicalGeneratedArtifactCutoverDiagnosticKind Kind { get; }
    public string? SyncRunID { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; }
    public CanonicalGeneratedArtifactCutoverDomain Domain { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }
    public string? Action { get; }
    public string? Result { get; }
    public string? Reason { get; }
    public string? HashPrefix { get; }

    public CanonicalGeneratedArtifactCutoverDiagnostic(
        CanonicalGeneratedArtifactCutoverDiagnosticKind kind, string? syncRunID,
        CanonicalSyncPlanTrigger trigger, CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalGeneratedArtifactCutoverDomain domain = CanonicalGeneratedArtifactCutoverDomain.generatedArtifacts,
        string? objectID = null, string? artifactID = null,
        CanonicalArtifact.Kind? artifactKind = null, string? action = null,
        string? result = null, string? reason = null, CanonicalHash? hash = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Domain = domain;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording") : null;
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        ArtifactKind = artifactKind;
        Action = CanonicalProductionRedaction.SafeDiagnosticText(action);
        Result = CanonicalProductionRedaction.SafeDiagnosticText(result);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = hash != null ? CanonicalProductionRedaction.HashPrefix(hash.Value) : null;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCutoverDiagnostic? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCutoverResult : IEquatable<CanonicalGeneratedArtifactCutoverResult>
{
    public CanonicalGeneratedArtifactCutoverGate Gate { get; set; }
    public CanonicalGeneratedArtifactProductionCommitResult[] Commits { get; set; }
    public CanonicalGeneratedArtifactRollbackExecutionResult[] RollbackResults { get; set; }
    public CanonicalGeneratedArtifactCutoverDiagnostic[] Diagnostics { get; set; }
    public bool LegacyFallbackUsed { get; set; }
    public string[] DuplicateLegacySuppressedActionIDs { get; set; }
    public int CanaryAttemptedCount { get; set; }
    public bool CanarySucceeded { get; set; }
    public bool FatalBlocker { get; set; }
    public CanonicalGeneratedArtifactReadSideParallelProjectionResult? ReadSideProjection { get; set; }
    public CanonicalGeneratedArtifactCanaryConfiguration? CanaryConfiguration { get; set; }
    public CanonicalGeneratedArtifactCanarySelectionResult? CanarySelection { get; set; }
    public CanonicalGeneratedArtifactCanaryCandidateSafety[]? CandidateSafetyReports { get; set; }
    public CanonicalGeneratedArtifactCanaryObservationReport? ObservationReport { get; set; }
    public CanonicalGeneratedArtifactCanaryStageObservationReport? StageObservationReport { get; set; }

    public bool Succeeded => Gate.Allowed && !FatalBlocker && Commits.Length > 0
        && Commits.All(c => c.Committed && c.PreconditionVerified && c.PostconditionVerified);

    public CanonicalGeneratedArtifactCutoverResult()
    {
        Gate = default!;
        Commits = Array.Empty<CanonicalGeneratedArtifactProductionCommitResult>();
        RollbackResults = Array.Empty<CanonicalGeneratedArtifactRollbackExecutionResult>();
        Diagnostics = Array.Empty<CanonicalGeneratedArtifactCutoverDiagnostic>();
        DuplicateLegacySuppressedActionIDs = Array.Empty<string>();
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCutoverResult? other) =>
        other is not null && CanarySucceeded == other.CanarySucceeded;
    public override int GetHashCode() => CanarySucceeded.GetHashCode();
}

// ─── Observation Types ────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCanaryObservationStatus
{
    blocked, noEligibleCandidate, committed, failedRolledBack, fatalRollbackFailure
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCanaryObservationRecommendation
{
    stayDisabled, remainN1, readyForN3AfterAudit, fixBlockers
}

public sealed record CanonicalGeneratedArtifactCanaryObservationReport : IEquatable<CanonicalGeneratedArtifactCanaryObservationReport>
{
    public CanonicalGeneratedArtifactCanaryObservationStatus Status { get; }
    public string? SyncRunID { get; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; }
    public int SelectedCandidateCount { get; }
    public int BlockedCandidateCount { get; }
    public int AttemptedCommitCount { get; }
    public int SuccessfulCommitCount { get; }
    public int RollbackCount { get; }
    public bool DuplicateSuppressionApplied { get; }
    public bool LegacyFallbackPreserved { get; }
    public bool ReadSideParallelEquivalent { get; }
    public bool GeneratedArtifactDownloadOnly { get; }
    public bool GeneratedArtifactUploadAttempted { get; }
    public bool AudioUploadAttempted { get; }
    public bool ContentLeakRiskObserved { get; }
    public bool RouteIsArtifactRequest { get; }
    public bool UiMutated { get; }
    public bool FatalBlocker { get; }
    public CanonicalGeneratedArtifactCanaryObservationRecommendation NextRecommendation { get; }
    public string Reason { get; }

    public CanonicalGeneratedArtifactCanaryObservationReport(
        CanonicalGeneratedArtifactCanaryObservationStatus status, string? syncRunID,
        CanonicalProductionExecutionDomainRole nodeRole, int selectedCandidateCount,
        int blockedCandidateCount, int attemptedCommitCount, int successfulCommitCount,
        int rollbackCount, bool duplicateSuppressionApplied, bool legacyFallbackPreserved,
        bool readSideParallelEquivalent, bool generatedArtifactDownloadOnly,
        bool generatedArtifactUploadAttempted, bool audioUploadAttempted,
        bool contentLeakRiskObserved, bool routeIsArtifactRequest, bool uiMutated,
        bool fatalBlocker, CanonicalGeneratedArtifactCanaryObservationRecommendation nextRecommendation,
        string reason)
    {
        Status = status;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        NodeRole = nodeRole;
        SelectedCandidateCount = Math.Max(0, selectedCandidateCount);
        BlockedCandidateCount = Math.Max(0, blockedCandidateCount);
        AttemptedCommitCount = Math.Max(0, attemptedCommitCount);
        SuccessfulCommitCount = Math.Max(0, successfulCommitCount);
        RollbackCount = Math.Max(0, rollbackCount);
        DuplicateSuppressionApplied = duplicateSuppressionApplied;
        LegacyFallbackPreserved = legacyFallbackPreserved;
        ReadSideParallelEquivalent = readSideParallelEquivalent;
        GeneratedArtifactDownloadOnly = generatedArtifactDownloadOnly;
        GeneratedArtifactUploadAttempted = generatedArtifactUploadAttempted;
        AudioUploadAttempted = audioUploadAttempted;
        ContentLeakRiskObserved = contentLeakRiskObserved;
        RouteIsArtifactRequest = routeIsArtifactRequest;
        UiMutated = uiMutated;
        FatalBlocker = fatalBlocker;
        NextRecommendation = nextRecommendation;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? Status.ToString();
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryObservationReport? other) =>
        other is not null && Status == other.Status;
    public override int GetHashCode() => Status.GetHashCode();
}

// ─── CanonicalGeneratedArtifactReadSideParallelProjectionResult ───────

public sealed record CanonicalGeneratedArtifactReadSideParallelProjectionResult : IEquatable<CanonicalGeneratedArtifactReadSideParallelProjectionResult>
{
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }
    public bool Equivalent { get; }
    public bool MutatedUI { get; }
    public string? CanonicalHashPrefix { get; }
    public string? LegacyHashPrefix { get; }
    public long? CanonicalByteSize { get; }
    public long? LegacyByteSize { get; }
    public string Reason { get; }

    public CanonicalGeneratedArtifactReadSideParallelProjectionResult(
        string objectID, string? artifactID, CanonicalArtifact.Kind? artifactKind,
        bool equivalent, CanonicalHash? canonicalHash, CanonicalHash? legacyHash,
        long? canonicalByteSize, long? legacyByteSize, string reason)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        ArtifactKind = artifactKind;
        Equivalent = equivalent;
        MutatedUI = false;
        CanonicalHashPrefix = canonicalHash != null ? CanonicalProductionRedaction.HashPrefix(canonicalHash.Value) : null;
        LegacyHashPrefix = legacyHash != null ? CanonicalProductionRedaction.HashPrefix(legacyHash.Value) : null;
        CanonicalByteSize = canonicalByteSize;
        LegacyByteSize = legacyByteSize;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (equivalent ? "equivalent" : "divergent");
    }

    public virtual bool Equals(CanonicalGeneratedArtifactReadSideParallelProjectionResult? other) =>
        other is not null && ObjectID == other.ObjectID;
    public override int GetHashCode() => ObjectID.GetHashCode();
}

// ─── AppSeam Configuration ────────────────────────────────────────────

public sealed record CanonicalGeneratedArtifactCutoverAppSeamPolicy : IEquatable<CanonicalGeneratedArtifactCutoverAppSeamPolicy>
{
    public bool RecordDiagnostics { get; }
    public int MaxDiagnosticsEvents { get; }
    public CanonicalGeneratedArtifactCanaryPolicy CanaryPolicy { get; }

    public CanonicalGeneratedArtifactCutoverAppSeamPolicy(
        bool recordDiagnostics = true, int maxDiagnosticsEvents = 200,
        CanonicalGeneratedArtifactCanaryPolicy? canaryPolicy = null)
    {
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
        CanaryPolicy = canaryPolicy ?? CanonicalGeneratedArtifactCanaryPolicy.Disabled;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCutoverAppSeamPolicy? other) =>
        other is not null && RecordDiagnostics == other.RecordDiagnostics;
    public override int GetHashCode() => RecordDiagnostics.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCutoverAppSeamConfiguration : IEquatable<CanonicalGeneratedArtifactCutoverAppSeamConfiguration>
{
    public bool IsEnabled { get; }
    public CanonicalCutoverAppSeamMode Mode { get; }
    public CanonicalGeneratedArtifactCutoverAppSeamPolicy Policy { get; }
    public CanonicalGeneratedArtifactCutoverEvidence Evidence { get; }
    public CanonicalCutoverToken? CutoverToken { get; }

    public CanonicalGeneratedArtifactCutoverAppSeamConfiguration(
        bool isEnabled = false,
        CanonicalCutoverAppSeamMode mode = CanonicalCutoverAppSeamMode.disabled,
        CanonicalGeneratedArtifactCutoverAppSeamPolicy? policy = null,
        CanonicalGeneratedArtifactCutoverEvidence? evidence = null,
        CanonicalCutoverToken? cutoverToken = null)
    {
        IsEnabled = isEnabled;
        Mode = isEnabled ? mode : CanonicalCutoverAppSeamMode.disabled;
        Policy = policy ?? new CanonicalGeneratedArtifactCutoverAppSeamPolicy();
        Evidence = evidence ?? new CanonicalGeneratedArtifactCutoverEvidence();
        CutoverToken = cutoverToken;
    }

    public static readonly CanonicalGeneratedArtifactCutoverAppSeamConfiguration Disabled = new();
    public CanonicalCutoverAppSeamMode EffectiveMode => IsEnabled ? Mode : CanonicalCutoverAppSeamMode.disabled;

    public static CanonicalGeneratedArtifactCutoverAppSeamConfiguration Enabled(
        CanonicalCutoverAppSeamMode mode = CanonicalCutoverAppSeamMode.guardedExecuteCommit,
        CanonicalGeneratedArtifactCutoverAppSeamPolicy? policy = null,
        CanonicalGeneratedArtifactCutoverEvidence? evidence = null,
        CanonicalCutoverToken? cutoverToken = null) =>
        new(true, mode, policy, evidence, cutoverToken);

    public virtual bool Equals(CanonicalGeneratedArtifactCutoverAppSeamConfiguration? other) =>
        other is not null && IsEnabled == other.IsEnabled;
    public override int GetHashCode() => IsEnabled.GetHashCode();
}

// ─── Stage Observation Report ─────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactCanaryStageRecommendation
{
    stayDisabled, observeCurrentStage, advanceToN10, advanceToAllEligible,
    holdForInvestigation, stopForFatalBlocker
}

public sealed record CanonicalGeneratedArtifactCanaryStageSummary : IEquatable<CanonicalGeneratedArtifactCanaryStageSummary>
{
    public CanonicalGeneratedArtifactCanaryStage Stage { get; }
    public int Budget { get; }
    public int SelectedCount { get; }
    public int ExecutedCount { get; }
    public int SuccessCount { get; }
    public int FailureCount { get; }

    public CanonicalGeneratedArtifactCanaryStageSummary(
        CanonicalGeneratedArtifactCanaryStage stage, int budget, int selectedCount,
        int executedCount, int successCount, int failureCount)
    {
        Stage = stage;
        Budget = Math.Max(0, budget);
        SelectedCount = Math.Max(0, selectedCount);
        ExecutedCount = Math.Max(0, executedCount);
        SuccessCount = Math.Max(0, successCount);
        FailureCount = Math.Max(0, failureCount);
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryStageSummary? other) =>
        other is not null && Stage == other.Stage;
    public override int GetHashCode() => Stage.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanaryStageFailure : IEquatable<CanonicalGeneratedArtifactCanaryStageFailure>
{
    public string Id => string.Join("|", ObjectID ?? "run", ArtifactID ?? "artifact", Blocker.ToString());
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalGeneratedArtifactCanaryBlocker Blocker { get; }

    public CanonicalGeneratedArtifactCanaryStageFailure(
        string? objectID, string? artifactID, CanonicalGeneratedArtifactCanaryBlocker blocker)
    {
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording") : null;
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        Blocker = blocker;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryStageFailure? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanaryStageObservationReport : IEquatable<CanonicalGeneratedArtifactCanaryStageObservationReport>
{
    public CanonicalGeneratedArtifactCanaryStage Stage { get; }
    public int Budget { get; }
    public int SelectedCount { get; }
    public int ExecutedCount { get; }
    public int SuccessCount { get; }
    public int FailureCount { get; }
    public int RollbackCount { get; }
    public int RollbackFailureCount { get; }
    public int LegacyFallbackCount { get; }
    public int DuplicateSuppressionCount { get; }
    public int SkippedCount { get; }
    public int NoEligibleCount { get; }
    public int UnsafeCandidateSkippedCount { get; }
    public int ContentLeakRiskCount { get; }
    public int UnsafePathTokenCount { get; }
    public int ParentTombstoneBlockCount { get; }
    public int AudioConfusionBlockCount { get; }
    public int FatalBlockerCount { get; }
    public int ReadSideParallelEquivalentCount { get; }
    public int ReadSideParallelDivergentCount { get; }
    public bool NextStageEligible { get; }
    public CanonicalGeneratedArtifactCutoverFailure[] NextStageBlockers { get; }
    public bool RuntimeSwitch { get; }
    public CanonicalMigrationDomain Domain { get; }
    public bool UiMutated { get; }
    public bool ArtifactUploadJobCreated { get; }
    public bool AudioAutoDownloaded { get; }
    public CanonicalGeneratedArtifactCanaryStageRecommendation Recommendation { get; }
    public CanonicalGeneratedArtifactCanaryStageSummary Summary { get; }
    public CanonicalGeneratedArtifactCanaryStageFailure[] Failures { get; }
    public CanonicalGeneratedArtifactStageEvidenceReport EvidenceReport { get; }
    public bool Redacted { get; }

    public CanonicalGeneratedArtifactCanaryStageObservationReport(
        CanonicalGeneratedArtifactCanaryStage stage, int budget, int selectedCount, int executedCount,
        int successCount, int failureCount, int rollbackCount, int rollbackFailureCount,
        int legacyFallbackCount, int duplicateSuppressionCount, int skippedCount, int noEligibleCount,
        int unsafeCandidateSkippedCount, int contentLeakRiskCount, int unsafePathTokenCount,
        int parentTombstoneBlockCount, int audioConfusionBlockCount, int fatalBlockerCount,
        int readSideParallelEquivalentCount, int readSideParallelDivergentCount, bool nextStageEligible,
        CanonicalGeneratedArtifactCutoverFailure[] nextStageBlockers,
        CanonicalGeneratedArtifactCanaryStageRecommendation recommendation, bool runtimeSwitch,
        CanonicalGeneratedArtifactCanaryStageFailure[] failures,
        CanonicalGeneratedArtifactStageEvidenceReport evidenceReport, bool redacted = true)
    {
        Stage = stage;
        Budget = Math.Max(0, budget);
        SelectedCount = Math.Max(0, selectedCount);
        ExecutedCount = Math.Max(0, executedCount);
        SuccessCount = Math.Max(0, successCount);
        FailureCount = Math.Max(0, failureCount);
        RollbackCount = Math.Max(0, rollbackCount);
        RollbackFailureCount = Math.Max(0, rollbackFailureCount);
        LegacyFallbackCount = Math.Max(0, legacyFallbackCount);
        DuplicateSuppressionCount = Math.Max(0, duplicateSuppressionCount);
        SkippedCount = Math.Max(0, skippedCount);
        NoEligibleCount = Math.Max(0, noEligibleCount);
        UnsafeCandidateSkippedCount = Math.Max(0, unsafeCandidateSkippedCount);
        ContentLeakRiskCount = Math.Max(0, contentLeakRiskCount);
        UnsafePathTokenCount = Math.Max(0, unsafePathTokenCount);
        ParentTombstoneBlockCount = Math.Max(0, parentTombstoneBlockCount);
        AudioConfusionBlockCount = Math.Max(0, audioConfusionBlockCount);
        FatalBlockerCount = Math.Max(0, fatalBlockerCount);
        ReadSideParallelEquivalentCount = Math.Max(0, readSideParallelEquivalentCount);
        ReadSideParallelDivergentCount = Math.Max(0, readSideParallelDivergentCount);
        NextStageEligible = nextStageEligible;
        NextStageBlockers = new HashSet<CanonicalGeneratedArtifactCutoverFailure>(nextStageBlockers)
            .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray();
        RuntimeSwitch = runtimeSwitch;
        Domain = CanonicalMigrationDomain.generatedArtifacts;
        UiMutated = false;
        ArtifactUploadJobCreated = false;
        AudioAutoDownloaded = false;
        Recommendation = recommendation;
        Summary = new CanonicalGeneratedArtifactCanaryStageSummary(stage,
            budget == int.MaxValue ? selectedCount : budget, selectedCount, executedCount, successCount, failureCount);
        Failures = failures;
        EvidenceReport = evidenceReport;
        Redacted = redacted;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryStageObservationReport? other) =>
        other is not null && Stage == other.Stage;
    public override int GetHashCode() => Stage.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanaryResult : IEquatable<CanonicalGeneratedArtifactCanaryResult>
{
    public CanonicalGeneratedArtifactCanaryConfiguration Configuration { get; }
    public CanonicalGeneratedArtifactCutoverResult CutoverResult { get; }
    public CanonicalGeneratedArtifactCanarySelectionResult Selection { get; }
    public CanonicalGeneratedArtifactCanaryObservationReport ObservationReport { get; }
    public bool Succeeded => CutoverResult.Succeeded;

    public CanonicalGeneratedArtifactCanaryResult(
        CanonicalGeneratedArtifactCanaryConfiguration configuration,
        CanonicalGeneratedArtifactCutoverResult cutoverResult,
        CanonicalGeneratedArtifactCanarySelectionResult selection,
        CanonicalGeneratedArtifactCanaryObservationReport observationReport)
    {
        Configuration = configuration;
        CutoverResult = cutoverResult;
        Selection = selection;
        ObservationReport = observationReport;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryResult? other) =>
        other is not null && Configuration.Equals(other.Configuration);
    public override int GetHashCode() => Configuration.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactCanaryStageResult : IEquatable<CanonicalGeneratedArtifactCanaryStageResult>
{
    public CanonicalGeneratedArtifactCutoverResult CutoverResult { get; }
    public CanonicalGeneratedArtifactCanarySelectionResult Selection { get; }
    public CanonicalGeneratedArtifactCanaryStageObservationReport StageObservationReport { get; }
    public bool Succeeded => CutoverResult.Succeeded;

    public CanonicalGeneratedArtifactCanaryStageResult(
        CanonicalGeneratedArtifactCutoverResult cutoverResult,
        CanonicalGeneratedArtifactCanarySelectionResult selection,
        CanonicalGeneratedArtifactCanaryStageObservationReport stageObservationReport)
    {
        CutoverResult = cutoverResult;
        Selection = selection;
        StageObservationReport = stageObservationReport;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactCanaryStageResult? other) =>
        other is not null && CutoverResult.CanarySucceeded == other.CutoverResult.CanarySucceeded;
    public override int GetHashCode() => CutoverResult.CanarySucceeded.GetHashCode();
}
