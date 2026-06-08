using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalCutoverDomain
{
    recordingMetadata,
    recordingAudio,
    generatedArtifacts,
    folders,
    studyItems,
    standaloneNotes,
    tombstones,
    conflicts,
    apply,
    fileRuntime,
    transportRuntime,
    uploadRuntime,
    objectProjection,
    inventory,
    uiIntegration
}

public static class CanonicalCutoverDomainExtensions
{
    public static CanonicalProductionDomain ToProductionDomain(this CanonicalCutoverDomain domain) => domain switch
    {
        CanonicalCutoverDomain.recordingMetadata => CanonicalProductionDomain.recordingMetadata,
        CanonicalCutoverDomain.recordingAudio => CanonicalProductionDomain.recordingAudio,
        CanonicalCutoverDomain.generatedArtifacts => CanonicalProductionDomain.generatedArtifacts,
        CanonicalCutoverDomain.folders => CanonicalProductionDomain.folders,
        CanonicalCutoverDomain.studyItems => CanonicalProductionDomain.studyItems,
        CanonicalCutoverDomain.standaloneNotes => CanonicalProductionDomain.standaloneNotes,
        CanonicalCutoverDomain.tombstones => CanonicalProductionDomain.tombstones,
        CanonicalCutoverDomain.conflicts => CanonicalProductionDomain.conflicts,
        CanonicalCutoverDomain.apply => CanonicalProductionDomain.apply,
        CanonicalCutoverDomain.fileRuntime => CanonicalProductionDomain.fileRuntime,
        CanonicalCutoverDomain.transportRuntime => CanonicalProductionDomain.transportRuntime,
        CanonicalCutoverDomain.uploadRuntime => CanonicalProductionDomain.uploadRuntime,
        CanonicalCutoverDomain.objectProjection => CanonicalProductionDomain.objectProjection,
        CanonicalCutoverDomain.inventory => CanonicalProductionDomain.inventory,
        CanonicalCutoverDomain.uiIntegration => CanonicalProductionDomain.uiIntegration,
        _ => CanonicalProductionDomain.recordingMetadata
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalCutoverMode
{
    disabled,
    shadowOnly,
    guardedExecuteNoCommit,
    guardedExecuteCommit,
    canary,
    rollbackOnly,
    legacyFallbackOnly
}

public static class CanonicalCutoverModeExtensions
{
    public static bool PermitsProductionCommit(this CanonicalCutoverMode mode) =>
        mode == CanonicalCutoverMode.guardedExecuteCommit || mode == CanonicalCutoverMode.canary;
}

public sealed class CanonicalCutoverPolicy : IEquatable<CanonicalCutoverPolicy>
{
    public int CanaryMaxObjectsPerSyncRun { get; set; }
    public bool AllowsV87CanaryN1InternalExecution { get; set; }
    public CanonicalRecordingMetadataCanaryStagePolicy? RecordingMetadataCanaryStagePolicy { get; set; }
    public bool RequireReadOnlyProbeForSend { get; set; }
    public bool RequireRollbackRehearsal { get; set; }
    public bool RequireProductionExecutionGuardPass { get; set; }
    public int MaxDiagnosticsEvents { get; set; }

    public CanonicalCutoverPolicy(
        int canaryMaxObjectsPerSyncRun = 0,
        bool allowsV87CanaryN1InternalExecution = false,
        CanonicalRecordingMetadataCanaryStagePolicy? recordingMetadataCanaryStagePolicy = null,
        bool requireReadOnlyProbeForSend = true,
        bool requireRollbackRehearsal = true,
        bool requireProductionExecutionGuardPass = true,
        int maxDiagnosticsEvents = 200)
    {
        CanaryMaxObjectsPerSyncRun = Math.Max(0, canaryMaxObjectsPerSyncRun);
        AllowsV87CanaryN1InternalExecution = allowsV87CanaryN1InternalExecution;
        RecordingMetadataCanaryStagePolicy = recordingMetadataCanaryStagePolicy;
        RequireReadOnlyProbeForSend = requireReadOnlyProbeForSend;
        RequireRollbackRehearsal = requireRollbackRehearsal;
        RequireProductionExecutionGuardPass = requireProductionExecutionGuardPass;
        MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
    }

    public CanonicalRecordingMetadataCanaryStagePolicy EffectiveRecordingMetadataCanaryStagePolicy =>
        RecordingMetadataCanaryStagePolicy ?? CanonicalRecordingMetadataCanaryStagePolicy.Disabled;

    public bool UsesExpandedRecordingMetadataStagePolicy =>
        EffectiveRecordingMetadataCanaryStagePolicy.RequestedStage.IsExpandedCanaryStage();

    public override bool Equals(object? obj) => obj is CanonicalCutoverPolicy other && Equals(other);
    public bool Equals(CanonicalCutoverPolicy? other) =>
        other is not null &&
        CanaryMaxObjectsPerSyncRun == other.CanaryMaxObjectsPerSyncRun &&
        AllowsV87CanaryN1InternalExecution == other.AllowsV87CanaryN1InternalExecution &&
        EqualityComparer<CanonicalRecordingMetadataCanaryStagePolicy?>.Default.Equals(RecordingMetadataCanaryStagePolicy, other.RecordingMetadataCanaryStagePolicy) &&
        RequireReadOnlyProbeForSend == other.RequireReadOnlyProbeForSend &&
        RequireRollbackRehearsal == other.RequireRollbackRehearsal &&
        RequireProductionExecutionGuardPass == other.RequireProductionExecutionGuardPass &&
        MaxDiagnosticsEvents == other.MaxDiagnosticsEvents;
    public override int GetHashCode() =>
        HashCode.Combine(CanaryMaxObjectsPerSyncRun, AllowsV87CanaryN1InternalExecution,
            RecordingMetadataCanaryStagePolicy, RequireReadOnlyProbeForSend,
            RequireRollbackRehearsal, RequireProductionExecutionGuardPass, MaxDiagnosticsEvents);
    public static bool operator ==(CanonicalCutoverPolicy left, CanonicalCutoverPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalCutoverPolicy left, CanonicalCutoverPolicy right) => !left.Equals(right);
}

public sealed class CanonicalSingleDomainCutoverConfiguration : IEquatable<CanonicalSingleDomainCutoverConfiguration>
{
    public CanonicalCutoverDomain Domain { get; set; }
    public CanonicalCutoverMode Mode { get; set; }
    public CanonicalCutoverPolicy Policy { get; set; }

    public CanonicalSingleDomainCutoverConfiguration(
        CanonicalCutoverDomain domain = CanonicalCutoverDomain.recordingMetadata,
        CanonicalCutoverMode mode = CanonicalCutoverMode.disabled,
        CanonicalCutoverPolicy? policy = null)
    {
        Domain = domain;
        Mode = mode;
        Policy = policy ?? new CanonicalCutoverPolicy();
    }

    public static readonly CanonicalSingleDomainCutoverConfiguration Disabled = new();

    public static CanonicalSingleDomainCutoverConfiguration Canary(
        int maxObjects,
        bool allowsV87CanaryN1InternalExecution = false) =>
        new()
        {
            Mode = CanonicalCutoverMode.canary,
            Policy = new CanonicalCutoverPolicy(
                canaryMaxObjectsPerSyncRun: maxObjects,
                allowsV87CanaryN1InternalExecution: allowsV87CanaryN1InternalExecution)
        };

    public static CanonicalSingleDomainCutoverConfiguration StagedCanary(
        CanonicalRecordingMetadataCanaryStage stage,
        bool allowCandidateExecution = true) =>
        new()
        {
            Mode = CanonicalCutoverMode.canary,
            Policy = new CanonicalCutoverPolicy(
                canaryMaxObjectsPerSyncRun: stage.NominalCanaryBudget(),
                allowsV87CanaryN1InternalExecution: stage == CanonicalRecordingMetadataCanaryStage.n1,
                recordingMetadataCanaryStagePolicy: new CanonicalRecordingMetadataCanaryStagePolicy(
                    requestedStage: stage,
                    allowCandidateExecution: allowCandidateExecution))
        };

    public override bool Equals(object? obj) => obj is CanonicalSingleDomainCutoverConfiguration other && Equals(other);
    public bool Equals(CanonicalSingleDomainCutoverConfiguration? other) =>
        other is not null &&
        Domain == other.Domain &&
        Mode == other.Mode &&
        EqualityComparer<CanonicalCutoverPolicy>.Default.Equals(Policy, other.Policy);
    public override int GetHashCode() => HashCode.Combine(Domain, Mode, Policy);
    public static bool operator ==(CanonicalSingleDomainCutoverConfiguration left, CanonicalSingleDomainCutoverConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalSingleDomainCutoverConfiguration left, CanonicalSingleDomainCutoverConfiguration right) => !left.Equals(right);
}

public sealed class CanonicalCutoverToken : IEquatable<CanonicalCutoverToken>
{
    public string TokenID { get; set; }
    public string SyncRunID { get; set; }
    public bool OwnerApproved { get; set; }

    public CanonicalCutoverToken(string tokenID, string syncRunID, bool ownerApproved = false)
    {
        TokenID = CanonicalProductionRedaction.SafeIdentifier(tokenID, "cutover-token")!;
        SyncRunID = CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run")!;
        OwnerApproved = ownerApproved;
    }

    public override bool Equals(object? obj) => obj is CanonicalCutoverToken other && Equals(other);
    public bool Equals(CanonicalCutoverToken? other) =>
        other is not null &&
        TokenID == other.TokenID &&
        SyncRunID == other.SyncRunID &&
        OwnerApproved == other.OwnerApproved;
    public override int GetHashCode() => HashCode.Combine(TokenID, SyncRunID, OwnerApproved);
    public static bool operator ==(CanonicalCutoverToken left, CanonicalCutoverToken right) => left.Equals(right);
    public static bool operator !=(CanonicalCutoverToken left, CanonicalCutoverToken right) => !left.Equals(right);
}

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
    rollbackFailed,
}

public sealed class CanonicalRecordingMetadataCutoverEvidence : IEquatable<CanonicalRecordingMetadataCutoverEvidence>
{
    public bool RealDataShadowCopyVerified { get; set; }
    public bool ExecutionShadowVerified { get; set; }
    public bool DryRunEquivalenceVerified { get; set; }
    public bool NoBlockingDivergence { get; set; }
    public bool NoUnresolvedConflict { get; set; }
    public bool ReadOnlyTransportProbePassed { get; set; }
    public bool ProductionPortAvailable { get; set; }
    public bool RealRootBoundApplyPortAvailable { get; set; }
    public CanonicalRecordingMetadataApplyPortMode ApplyPortMode { get; set; }
    public bool RootBoundWriteAvailable { get; set; }
    public bool AtomicReplaceAvailable { get; set; }
    public bool RollbackCheckpointAvailable { get; set; }
    public bool RollbackVerified { get; set; }
    public bool ProductionRootDisabledByDefault { get; set; }
    public bool TestRootUsed { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public CanonicalRollbackPlan? RollbackPlan { get; set; }
    public bool RollbackRehearsalPassed { get; set; }
    public bool ProductionExecutionGuardPassed { get; set; }
    public bool UiParallelReadEquivalent { get; set; }
    public CanonicalRecordingMetadataCanaryStageEvidence? CanaryStageEvidence { get; set; }

    public CanonicalRecordingMetadataCutoverEvidence(
        bool realDataShadowCopyVerified = false,
        bool executionShadowVerified = false,
        bool dryRunEquivalenceVerified = false,
        bool noBlockingDivergence = false,
        bool noUnresolvedConflict = false,
        bool readOnlyTransportProbePassed = false,
        bool productionPortAvailable = false,
        bool realRootBoundApplyPortAvailable = false,
        CanonicalRecordingMetadataApplyPortMode applyPortMode = CanonicalRecordingMetadataApplyPortMode.disabled,
        bool rootBoundWriteAvailable = false,
        bool atomicReplaceAvailable = false,
        bool rollbackCheckpointAvailable = false,
        bool rollbackVerified = false,
        bool productionRootDisabledByDefault = false,
        bool testRootUsed = false,
        bool legacyFallbackAvailable = false,
        CanonicalRollbackPlan? rollbackPlan = null,
        bool rollbackRehearsalPassed = false,
        bool productionExecutionGuardPassed = false,
        bool uiParallelReadEquivalent = false,
        CanonicalRecordingMetadataCanaryStageEvidence? canaryStageEvidence = null)
    {
        RealDataShadowCopyVerified = realDataShadowCopyVerified;
        ExecutionShadowVerified = executionShadowVerified;
        DryRunEquivalenceVerified = dryRunEquivalenceVerified;
        NoBlockingDivergence = noBlockingDivergence;
        NoUnresolvedConflict = noUnresolvedConflict;
        ReadOnlyTransportProbePassed = readOnlyTransportProbePassed;
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
        ProductionExecutionGuardPassed = productionExecutionGuardPassed;
        UiParallelReadEquivalent = uiParallelReadEquivalent;
        CanaryStageEvidence = canaryStageEvidence;
    }

    public static CanonicalRecordingMetadataCutoverEvidence Passing(CanonicalRollbackPlan rollbackPlan) =>
        new(
            realDataShadowCopyVerified: true,
            executionShadowVerified: true,
            dryRunEquivalenceVerified: true,
            noBlockingDivergence: true,
            noUnresolvedConflict: true,
            readOnlyTransportProbePassed: true,
            productionPortAvailable: true,
            realRootBoundApplyPortAvailable: true,
            applyPortMode: CanonicalRecordingMetadataApplyPortMode.testRootBound,
            rootBoundWriteAvailable: true,
            atomicReplaceAvailable: true,
            rollbackCheckpointAvailable: true,
            rollbackVerified: true,
            productionRootDisabledByDefault: true,
            testRootUsed: true,
            legacyFallbackAvailable: true,
            rollbackPlan: rollbackPlan,
            rollbackRehearsalPassed: true,
            productionExecutionGuardPassed: true,
            uiParallelReadEquivalent: true);

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCutoverEvidence other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCutoverEvidence? other)
    {
        if (other is null) return false;
        return RealDataShadowCopyVerified == other.RealDataShadowCopyVerified &&
               ExecutionShadowVerified == other.ExecutionShadowVerified &&
               DryRunEquivalenceVerified == other.DryRunEquivalenceVerified &&
               NoBlockingDivergence == other.NoBlockingDivergence &&
               NoUnresolvedConflict == other.NoUnresolvedConflict &&
               ReadOnlyTransportProbePassed == other.ReadOnlyTransportProbePassed &&
               ProductionPortAvailable == other.ProductionPortAvailable &&
               RealRootBoundApplyPortAvailable == other.RealRootBoundApplyPortAvailable &&
               ApplyPortMode == other.ApplyPortMode &&
               RootBoundWriteAvailable == other.RootBoundWriteAvailable &&
               AtomicReplaceAvailable == other.AtomicReplaceAvailable &&
               RollbackCheckpointAvailable == other.RollbackCheckpointAvailable &&
               RollbackVerified == other.RollbackVerified &&
               ProductionRootDisabledByDefault == other.ProductionRootDisabledByDefault &&
               TestRootUsed == other.TestRootUsed &&
               LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
               EqualityComparer<CanonicalRollbackPlan?>.Default.Equals(RollbackPlan, other.RollbackPlan) &&
               RollbackRehearsalPassed == other.RollbackRehearsalPassed &&
               ProductionExecutionGuardPassed == other.ProductionExecutionGuardPassed &&
               UiParallelReadEquivalent == other.UiParallelReadEquivalent &&
               EqualityComparer<CanonicalRecordingMetadataCanaryStageEvidence?>.Default.Equals(CanaryStageEvidence, other.CanaryStageEvidence);
    }
    public override int GetHashCode() =>
        HashCode.Combine(RealDataShadowCopyVerified, ExecutionShadowVerified, DryRunEquivalenceVerified,
            NoBlockingDivergence, NoUnresolvedConflict, ReadOnlyTransportProbePassed, ProductionPortAvailable,
            RealRootBoundApplyPortAvailable, ApplyPortMode, RootBoundWriteAvailable, AtomicReplaceAvailable,
            RollbackCheckpointAvailable, RollbackVerified, ProductionRootDisabledByDefault, TestRootUsed,
            LegacyFallbackAvailable, RollbackPlan, RollbackRehearsalPassed, ProductionExecutionGuardPassed,
            UiParallelReadEquivalent, CanaryStageEvidence);
    public static bool operator ==(CanonicalRecordingMetadataCutoverEvidence left, CanonicalRecordingMetadataCutoverEvidence right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCutoverEvidence left, CanonicalRecordingMetadataCutoverEvidence right) => !left.Equals(right);
}

public sealed class CanonicalCutoverGate : IEquatable<CanonicalCutoverGate>
{
    public CanonicalCutoverDomain Domain { get; set; }
    public CanonicalCutoverMode Mode { get; set; }
    public bool Allowed { get; set; }
    public List<CanonicalCutoverFailure> Failures { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public string Reason { get; set; }

    public CanonicalCutoverGate(
        CanonicalCutoverDomain domain,
        CanonicalCutoverMode mode,
        List<CanonicalCutoverFailure> failures,
        bool legacyFallbackAvailable,
        string reason)
    {
        Domain = domain;
        Mode = mode;
        Failures = new HashSet<CanonicalCutoverFailure>(failures).OrderBy(f => f.ToString()).ToList();
        Allowed = Failures.Count == 0;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (Allowed ? "allowed" : "blocked") ?? "unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalCutoverGate other && Equals(other);
    public bool Equals(CanonicalCutoverGate? other) =>
        other is not null &&
        Domain == other.Domain && Mode == other.Mode &&
        Allowed == other.Allowed &&
        Failures.SequenceEqual(other.Failures) &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(Domain, Mode, Allowed, Failures.Count, LegacyFallbackAvailable, Reason);
    public static bool operator ==(CanonicalCutoverGate left, CanonicalCutoverGate right) => left.Equals(right);
    public static bool operator !=(CanonicalCutoverGate left, CanonicalCutoverGate right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCutoverActionKind
{
    apply,
    send
}

public sealed class CanonicalRecordingMetadataCutoverCandidate : IEquatable<CanonicalRecordingMetadataCutoverCandidate>
{
    public string Id => Action.ActionID;

    public CanonicalApplyAction Action { get; set; }
    public CanonicalRecordingObject? LocalObject { get; set; }
    public CanonicalRecordingObject? PeerObject { get; set; }
    public string? RollbackCheckpointID { get; set; }
    public bool UnresolvedConflict { get; set; }

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

    public CanonicalRecordingMetadataCutoverActionKind? CutoverActionKind => Action.Kind switch
    {
        CanonicalApplyActionKind.recordingMetadataApply => CanonicalRecordingMetadataCutoverActionKind.apply,
        CanonicalApplyActionKind.recordingMetadataSend => CanonicalRecordingMetadataCutoverActionKind.send,
        _ => null
    };

    public bool RequiresNetworkSend => CutoverActionKind == CanonicalRecordingMetadataCutoverActionKind.send;

    public CanonicalRecordingObject? ExpectedObject => CutoverActionKind switch
    {
        CanonicalRecordingMetadataCutoverActionKind.apply => PeerObject,
        CanonicalRecordingMetadataCutoverActionKind.send => LocalObject,
        _ => null
    };

    public CanonicalHash? StableMetadataHash => ExpectedObject?.MetadataHash;

    public string EffectiveRollbackCheckpointID =>
        RollbackCheckpointID ?? $"recording-metadata-cutover-{ObjectID}";

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCutoverCandidate other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCutoverCandidate? other) =>
        other is not null && Action.ActionID == other.Action.ActionID;
    public override int GetHashCode() => Action.ActionID.GetHashCode();
    public static bool operator ==(CanonicalRecordingMetadataCutoverCandidate left, CanonicalRecordingMetadataCutoverCandidate right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCutoverCandidate left, CanonicalRecordingMetadataCutoverCandidate right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCanaryStage
{
    disabled,
    n1,
    n3,
    n10,
    allEligible
}

public static class CanonicalRecordingMetadataCanaryStageExtensions
{
    public static bool IsExecutable(this CanonicalRecordingMetadataCanaryStage stage) =>
        stage != CanonicalRecordingMetadataCanaryStage.disabled;

    public static bool IsExpandedCanaryStage(this CanonicalRecordingMetadataCanaryStage stage) => stage switch
    {
        CanonicalRecordingMetadataCanaryStage.n3 or CanonicalRecordingMetadataCanaryStage.n10 or CanonicalRecordingMetadataCanaryStage.allEligible => true,
        _ => false
    };

    public static CanonicalRecordingMetadataCanaryStage? PreviousStage(this CanonicalRecordingMetadataCanaryStage stage) => stage switch
    {
        CanonicalRecordingMetadataCanaryStage.disabled => null,
        CanonicalRecordingMetadataCanaryStage.n1 => CanonicalRecordingMetadataCanaryStage.disabled,
        CanonicalRecordingMetadataCanaryStage.n3 => CanonicalRecordingMetadataCanaryStage.n1,
        CanonicalRecordingMetadataCanaryStage.n10 => CanonicalRecordingMetadataCanaryStage.n3,
        CanonicalRecordingMetadataCanaryStage.allEligible => CanonicalRecordingMetadataCanaryStage.n10,
        _ => null
    };

    public static int NominalCanaryBudget(this CanonicalRecordingMetadataCanaryStage stage) => stage switch
    {
        CanonicalRecordingMetadataCanaryStage.disabled => 0,
        CanonicalRecordingMetadataCanaryStage.n1 => 1,
        CanonicalRecordingMetadataCanaryStage.n3 => 3,
        CanonicalRecordingMetadataCanaryStage.n10 => 10,
        CanonicalRecordingMetadataCanaryStage.allEligible => int.MaxValue,
        _ => 0
    };

    public static int MinimumPreviousStageSuccessCount(this CanonicalRecordingMetadataCanaryStage stage) => stage switch
    {
        CanonicalRecordingMetadataCanaryStage.disabled or CanonicalRecordingMetadataCanaryStage.n1 => 0,
        CanonicalRecordingMetadataCanaryStage.n3 => 1,
        CanonicalRecordingMetadataCanaryStage.n10 => 3,
        CanonicalRecordingMetadataCanaryStage.allEligible => 10,
        _ => 0
    };
}

public sealed class CanonicalRecordingMetadataCanaryStagePolicy : IEquatable<CanonicalRecordingMetadataCanaryStagePolicy>
{
    public CanonicalRecordingMetadataCanaryStage RequestedStage { get; set; }
    public bool AllowCandidateExecution { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }

    public CanonicalRecordingMetadataCanaryStagePolicy(
        CanonicalRecordingMetadataCanaryStage requestedStage = CanonicalRecordingMetadataCanaryStage.disabled,
        bool allowCandidateExecution = false,
        bool runtimeSwitchEnabled = false)
    {
        RequestedStage = requestedStage;
        AllowCandidateExecution = allowCandidateExecution;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
    }

    public static readonly CanonicalRecordingMetadataCanaryStagePolicy Disabled = new();

    public int CanaryBudget => RequestedStage.NominalCanaryBudget();

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanaryStagePolicy other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanaryStagePolicy? other) =>
        other is not null &&
        RequestedStage == other.RequestedStage &&
        AllowCandidateExecution == other.AllowCandidateExecution &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled;
    public override int GetHashCode() => HashCode.Combine(RequestedStage, AllowCandidateExecution, RuntimeSwitchEnabled);
    public static bool operator ==(CanonicalRecordingMetadataCanaryStagePolicy left, CanonicalRecordingMetadataCanaryStagePolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanaryStagePolicy left, CanonicalRecordingMetadataCanaryStagePolicy right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataStageEvidenceStatus
{
    missing,
    incomplete,
    passed,
    failed,
    blocked
}

public static class CanonicalRecordingMetadataStageEvidenceStatusExtensions
{
    public static bool IsPassing(this CanonicalRecordingMetadataStageEvidenceStatus status) =>
        status == CanonicalRecordingMetadataStageEvidenceStatus.passed;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataStageEvidenceBlocker
{
    stageDisabled,
    unsupportedDomain,
    runtimeSwitchEnabled,
    candidateExecutionNotApproved,
    previousStageEvidenceMissing,
    stageOrderViolation,
    previousStageObservationIncomplete,
    previousStageInsufficientSuccess,
    previousStageFailure,
    previousStageRollbackFailure,
    previousStageBlockingDivergence,
    previousStageUnresolvedConflict,
    previousStagePostconditionFailure,
    previousStageUnsupportedObject,
    ownerApprovalMissing,
    rollbackPlanMissing,
    dryRunEquivalenceMissing,
    executionShadowMissing,
    realDataShadowCopyMissing,
    readOnlyTransportProbeMissing,
    productionApplyPortUnavailable,
    legacyFallbackUnavailable,
    observationWindowIncomplete,
}

public sealed class CanonicalRecordingMetadataStageObservationWindow : IEquatable<CanonicalRecordingMetadataStageObservationWindow>
{
    public string ObservationWindowID { get; set; }
    public bool Complete { get; set; }

    public CanonicalRecordingMetadataStageObservationWindow(
        string observationWindowID = "recording-metadata-stage-window",
        bool complete = false)
    {
        ObservationWindowID = CanonicalProductionRedaction.SafeIdentifier(observationWindowID, "recording-metadata-stage-window")!;
        Complete = complete;
    }

    public static CanonicalRecordingMetadataStageObservationWindow CompleteWindow(string id) =>
        new(id, true);

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataStageObservationWindow other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataStageObservationWindow? other) =>
        other is not null && ObservationWindowID == other.ObservationWindowID && Complete == other.Complete;
    public override int GetHashCode() => HashCode.Combine(ObservationWindowID, Complete);
    public static bool operator ==(CanonicalRecordingMetadataStageObservationWindow left, CanonicalRecordingMetadataStageObservationWindow right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataStageObservationWindow left, CanonicalRecordingMetadataStageObservationWindow right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataCanaryStageEvidence : IEquatable<CanonicalRecordingMetadataCanaryStageEvidence>
{
    public CanonicalRecordingMetadataCanaryStage PreviousStage { get; set; }
    public CanonicalRecordingMetadataCanaryStage RequestedStage { get; set; }
    public int PreviousStageSuccessCount { get; set; }
    public int PreviousStageFailureCount { get; set; }
    public int PreviousStageRollbackFailureCount { get; set; }
    public int PreviousStageBlockingDivergenceCount { get; set; }
    public int PreviousStageSuppressedLegacyDuplicateCount { get; set; }
    public int UnresolvedConflictCount { get; set; }
    public int PreviousStagePostconditionFailureCount { get; set; }
    public int PreviousStageUnsupportedObjectCount { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus DryRunEquivalenceStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus ExecutionShadowStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus RealDataShadowCopyStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus ReadOnlyTransportProbeStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus RollbackPlanStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus ProductionApplyPortStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus LegacyFallbackStatus { get; set; }
    public string ObservationWindowID { get; set; }
    public bool ObservationWindowComplete { get; set; }
    public bool OwnerApproved { get; set; }

    public CanonicalRecordingMetadataCanaryStageEvidence(
        CanonicalRecordingMetadataCanaryStage previousStage = CanonicalRecordingMetadataCanaryStage.disabled,
        CanonicalRecordingMetadataCanaryStage requestedStage = CanonicalRecordingMetadataCanaryStage.disabled,
        int previousStageSuccessCount = 0,
        int previousStageFailureCount = 0,
        int previousStageRollbackFailureCount = 0,
        int previousStageBlockingDivergenceCount = 0,
        int previousStageSuppressedLegacyDuplicateCount = 0,
        int unresolvedConflictCount = 0,
        int previousStagePostconditionFailureCount = 0,
        int previousStageUnsupportedObjectCount = 0,
        CanonicalRecordingMetadataStageEvidenceStatus dryRunEquivalenceStatus = CanonicalRecordingMetadataStageEvidenceStatus.missing,
        CanonicalRecordingMetadataStageEvidenceStatus executionShadowStatus = CanonicalRecordingMetadataStageEvidenceStatus.missing,
        CanonicalRecordingMetadataStageEvidenceStatus realDataShadowCopyStatus = CanonicalRecordingMetadataStageEvidenceStatus.missing,
        CanonicalRecordingMetadataStageEvidenceStatus readOnlyTransportProbeStatus = CanonicalRecordingMetadataStageEvidenceStatus.missing,
        CanonicalRecordingMetadataStageEvidenceStatus rollbackPlanStatus = CanonicalRecordingMetadataStageEvidenceStatus.missing,
        CanonicalRecordingMetadataStageEvidenceStatus productionApplyPortStatus = CanonicalRecordingMetadataStageEvidenceStatus.missing,
        CanonicalRecordingMetadataStageEvidenceStatus legacyFallbackStatus = CanonicalRecordingMetadataStageEvidenceStatus.missing,
        CanonicalRecordingMetadataStageObservationWindow? observationWindow = null,
        bool ownerApproved = false)
    {
        var window = observationWindow ?? new CanonicalRecordingMetadataStageObservationWindow();
        PreviousStage = previousStage;
        RequestedStage = requestedStage;
        PreviousStageSuccessCount = Math.Max(0, previousStageSuccessCount);
        PreviousStageFailureCount = Math.Max(0, previousStageFailureCount);
        PreviousStageRollbackFailureCount = Math.Max(0, previousStageRollbackFailureCount);
        PreviousStageBlockingDivergenceCount = Math.Max(0, previousStageBlockingDivergenceCount);
        PreviousStageSuppressedLegacyDuplicateCount = Math.Max(0, previousStageSuppressedLegacyDuplicateCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        PreviousStagePostconditionFailureCount = Math.Max(0, previousStagePostconditionFailureCount);
        PreviousStageUnsupportedObjectCount = Math.Max(0, previousStageUnsupportedObjectCount);
        DryRunEquivalenceStatus = dryRunEquivalenceStatus;
        ExecutionShadowStatus = executionShadowStatus;
        RealDataShadowCopyStatus = realDataShadowCopyStatus;
        ReadOnlyTransportProbeStatus = readOnlyTransportProbeStatus;
        RollbackPlanStatus = rollbackPlanStatus;
        ProductionApplyPortStatus = productionApplyPortStatus;
        LegacyFallbackStatus = legacyFallbackStatus;
        ObservationWindowID = window.ObservationWindowID;
        ObservationWindowComplete = window.Complete;
        OwnerApproved = ownerApproved;
    }

    public static CanonicalRecordingMetadataCanaryStageEvidence Passing(
        CanonicalRecordingMetadataCanaryStage previousStage,
        CanonicalRecordingMetadataCanaryStage requestedStage,
        int previousStageSuccessCount,
        int previousStageSuppressedLegacyDuplicateCount = 0,
        string observationWindowID = "")
    {
        return new CanonicalRecordingMetadataCanaryStageEvidence(
            previousStage: previousStage,
            requestedStage: requestedStage,
            previousStageSuccessCount: previousStageSuccessCount,
            previousStageSuppressedLegacyDuplicateCount: previousStageSuppressedLegacyDuplicateCount,
            dryRunEquivalenceStatus: CanonicalRecordingMetadataStageEvidenceStatus.passed,
            executionShadowStatus: CanonicalRecordingMetadataStageEvidenceStatus.passed,
            realDataShadowCopyStatus: CanonicalRecordingMetadataStageEvidenceStatus.passed,
            readOnlyTransportProbeStatus: CanonicalRecordingMetadataStageEvidenceStatus.passed,
            rollbackPlanStatus: CanonicalRecordingMetadataStageEvidenceStatus.passed,
            productionApplyPortStatus: CanonicalRecordingMetadataStageEvidenceStatus.passed,
            legacyFallbackStatus: CanonicalRecordingMetadataStageEvidenceStatus.passed,
            observationWindow: CanonicalRecordingMetadataStageObservationWindow.CompleteWindow(observationWindowID),
            ownerApproved: true);
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanaryStageEvidence other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanaryStageEvidence? other)
    {
        if (other is null) return false;
        return PreviousStage == other.PreviousStage &&
               RequestedStage == other.RequestedStage &&
               PreviousStageSuccessCount == other.PreviousStageSuccessCount &&
               PreviousStageFailureCount == other.PreviousStageFailureCount &&
               PreviousStageRollbackFailureCount == other.PreviousStageRollbackFailureCount &&
               PreviousStageBlockingDivergenceCount == other.PreviousStageBlockingDivergenceCount &&
               PreviousStageSuppressedLegacyDuplicateCount == other.PreviousStageSuppressedLegacyDuplicateCount &&
               UnresolvedConflictCount == other.UnresolvedConflictCount &&
               PreviousStagePostconditionFailureCount == other.PreviousStagePostconditionFailureCount &&
               PreviousStageUnsupportedObjectCount == other.PreviousStageUnsupportedObjectCount &&
               DryRunEquivalenceStatus == other.DryRunEquivalenceStatus &&
               ExecutionShadowStatus == other.ExecutionShadowStatus &&
               RealDataShadowCopyStatus == other.RealDataShadowCopyStatus &&
               ReadOnlyTransportProbeStatus == other.ReadOnlyTransportProbeStatus &&
               RollbackPlanStatus == other.RollbackPlanStatus &&
               ProductionApplyPortStatus == other.ProductionApplyPortStatus &&
               LegacyFallbackStatus == other.LegacyFallbackStatus &&
               ObservationWindowID == other.ObservationWindowID &&
               ObservationWindowComplete == other.ObservationWindowComplete &&
               OwnerApproved == other.OwnerApproved;
    }
    public override int GetHashCode() =>
        HashCode.Combine(PreviousStage, RequestedStage, PreviousStageSuccessCount, PreviousStageFailureCount,
            PreviousStageRollbackFailureCount, PreviousStageBlockingDivergenceCount, PreviousStageSuppressedLegacyDuplicateCount,
            UnresolvedConflictCount, PreviousStagePostconditionFailureCount, PreviousStageUnsupportedObjectCount,
            DryRunEquivalenceStatus, ExecutionShadowStatus, RealDataShadowCopyStatus, ReadOnlyTransportProbeStatus,
            RollbackPlanStatus, ProductionApplyPortStatus, LegacyFallbackStatus, ObservationWindowID,
            ObservationWindowComplete, OwnerApproved);
    public static bool operator ==(CanonicalRecordingMetadataCanaryStageEvidence left, CanonicalRecordingMetadataCanaryStageEvidence right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanaryStageEvidence left, CanonicalRecordingMetadataCanaryStageEvidence right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataStageEvidenceReport : IEquatable<CanonicalRecordingMetadataStageEvidenceReport>
{
    public CanonicalRecordingMetadataStageEvidenceStatus Status { get; set; }
    public List<CanonicalRecordingMetadataStageEvidenceBlocker> Blockers { get; set; }
    public CanonicalRecordingMetadataCanaryStage PreviousStage { get; set; }
    public CanonicalRecordingMetadataCanaryStage RequestedStage { get; set; }
    public int PreviousStageSuccessCount { get; set; }
    public int PreviousStageFailureCount { get; set; }
    public int PreviousStageRollbackFailureCount { get; set; }
    public int PreviousStageBlockingDivergenceCount { get; set; }
    public int PreviousStageSuppressedLegacyDuplicateCount { get; set; }
    public int UnresolvedConflictCount { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus DryRunEquivalenceStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus ExecutionShadowStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus RealDataShadowCopyStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus ReadOnlyTransportProbeStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus RollbackPlanStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus ProductionApplyPortStatus { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus LegacyFallbackStatus { get; set; }
    public string ObservationWindowID { get; set; }
    public bool ObservationWindowComplete { get; set; }
    public bool SensitiveFieldsRedacted { get; set; }

    public CanonicalRecordingMetadataStageEvidenceReport(
        CanonicalRecordingMetadataCanaryStageEvidence? evidence,
        CanonicalRecordingMetadataCanaryStage requestedStage,
        List<CanonicalRecordingMetadataStageEvidenceBlocker> blockers)
    {
        var normalizedBlockers = new HashSet<CanonicalRecordingMetadataStageEvidenceBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        var hasIncompleteEvidence = evidence == null
            || evidence.ObservationWindowComplete == false
            || new[] {
                evidence.DryRunEquivalenceStatus,
                evidence.ExecutionShadowStatus,
                evidence.RealDataShadowCopyStatus,
                evidence.ReadOnlyTransportProbeStatus,
                evidence.RollbackPlanStatus,
                evidence.ProductionApplyPortStatus,
                evidence.LegacyFallbackStatus
            }.Any(s => s == CanonicalRecordingMetadataStageEvidenceStatus.missing || s == CanonicalRecordingMetadataStageEvidenceStatus.incomplete);

        if (evidence == null)
            Status = CanonicalRecordingMetadataStageEvidenceStatus.missing;
        else if (normalizedBlockers.Count == 0)
            Status = CanonicalRecordingMetadataStageEvidenceStatus.passed;
        else if (hasIncompleteEvidence)
            Status = CanonicalRecordingMetadataStageEvidenceStatus.incomplete;
        else
            Status = CanonicalRecordingMetadataStageEvidenceStatus.blocked;

        Blockers = normalizedBlockers;
        PreviousStage = evidence?.PreviousStage ?? (requestedStage.PreviousStage() ?? CanonicalRecordingMetadataCanaryStage.disabled);
        RequestedStage = requestedStage;
        PreviousStageSuccessCount = evidence?.PreviousStageSuccessCount ?? 0;
        PreviousStageFailureCount = evidence?.PreviousStageFailureCount ?? 0;
        PreviousStageRollbackFailureCount = evidence?.PreviousStageRollbackFailureCount ?? 0;
        PreviousStageBlockingDivergenceCount = evidence?.PreviousStageBlockingDivergenceCount ?? 0;
        PreviousStageSuppressedLegacyDuplicateCount = evidence?.PreviousStageSuppressedLegacyDuplicateCount ?? 0;
        UnresolvedConflictCount = evidence?.UnresolvedConflictCount ?? 0;
        DryRunEquivalenceStatus = evidence?.DryRunEquivalenceStatus ?? CanonicalRecordingMetadataStageEvidenceStatus.missing;
        ExecutionShadowStatus = evidence?.ExecutionShadowStatus ?? CanonicalRecordingMetadataStageEvidenceStatus.missing;
        RealDataShadowCopyStatus = evidence?.RealDataShadowCopyStatus ?? CanonicalRecordingMetadataStageEvidenceStatus.missing;
        ReadOnlyTransportProbeStatus = evidence?.ReadOnlyTransportProbeStatus ?? CanonicalRecordingMetadataStageEvidenceStatus.missing;
        RollbackPlanStatus = evidence?.RollbackPlanStatus ?? CanonicalRecordingMetadataStageEvidenceStatus.missing;
        ProductionApplyPortStatus = evidence?.ProductionApplyPortStatus ?? CanonicalRecordingMetadataStageEvidenceStatus.missing;
        LegacyFallbackStatus = evidence?.LegacyFallbackStatus ?? CanonicalRecordingMetadataStageEvidenceStatus.missing;
        ObservationWindowID = evidence?.ObservationWindowID ?? "missing-observation-window";
        ObservationWindowComplete = evidence?.ObservationWindowComplete ?? false;
        SensitiveFieldsRedacted = true;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"status={Status}",
        $"blockers={string.Join("+", Blockers.Select(b => b.ToString()))}",
        $"previousStage={PreviousStage}",
        $"requestedStage={RequestedStage}",
        $"previousStageSuccessCount={PreviousStageSuccessCount}",
        $"previousStageFailureCount={PreviousStageFailureCount}",
        $"previousStageRollbackFailureCount={PreviousStageRollbackFailureCount}",
        $"previousStageBlockingDivergenceCount={PreviousStageBlockingDivergenceCount}",
        $"previousStageSuppressedLegacyDuplicateCount={PreviousStageSuppressedLegacyDuplicateCount}",
        $"unresolvedConflictCount={UnresolvedConflictCount}",
        $"dryRunEquivalenceStatus={DryRunEquivalenceStatus}",
        $"executionShadowStatus={ExecutionShadowStatus}",
        $"realDataShadowCopyStatus={RealDataShadowCopyStatus}",
        $"readOnlyTransportProbeStatus={ReadOnlyTransportProbeStatus}",
        $"rollbackPlanStatus={RollbackPlanStatus}",
        $"productionApplyPortStatus={ProductionApplyPortStatus}",
        $"legacyFallbackStatus={LegacyFallbackStatus}",
        $"observationWindowID={ObservationWindowID}",
        $"observationWindowComplete={ObservationWindowComplete}",
        $"sensitiveFieldsRedacted={SensitiveFieldsRedacted}");

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataStageEvidenceReport other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataStageEvidenceReport? other)
    {
        if (other is null) return false;
        return Status == other.Status &&
               Blockers.SequenceEqual(other.Blockers) &&
               PreviousStage == other.PreviousStage &&
               RequestedStage == other.RequestedStage &&
               PreviousStageSuccessCount == other.PreviousStageSuccessCount &&
               PreviousStageFailureCount == other.PreviousStageFailureCount &&
               PreviousStageRollbackFailureCount == other.PreviousStageRollbackFailureCount &&
               PreviousStageBlockingDivergenceCount == other.PreviousStageBlockingDivergenceCount &&
               PreviousStageSuppressedLegacyDuplicateCount == other.PreviousStageSuppressedLegacyDuplicateCount &&
               UnresolvedConflictCount == other.UnresolvedConflictCount &&
               DryRunEquivalenceStatus == other.DryRunEquivalenceStatus &&
               ExecutionShadowStatus == other.ExecutionShadowStatus &&
               RealDataShadowCopyStatus == other.RealDataShadowCopyStatus &&
               ReadOnlyTransportProbeStatus == other.ReadOnlyTransportProbeStatus &&
               RollbackPlanStatus == other.RollbackPlanStatus &&
               ProductionApplyPortStatus == other.ProductionApplyPortStatus &&
               LegacyFallbackStatus == other.LegacyFallbackStatus &&
               ObservationWindowID == other.ObservationWindowID &&
               ObservationWindowComplete == other.ObservationWindowComplete &&
               SensitiveFieldsRedacted == other.SensitiveFieldsRedacted;
    }
    public override int GetHashCode() =>
        HashCode.Combine(Status, Blockers.Count, PreviousStage, RequestedStage, PreviousStageSuccessCount,
            PreviousStageFailureCount, PreviousStageRollbackFailureCount, PreviousStageBlockingDivergenceCount,
            PreviousStageSuppressedLegacyDuplicateCount, UnresolvedConflictCount, DryRunEquivalenceStatus,
            ExecutionShadowStatus, RealDataShadowCopyStatus, ReadOnlyTransportProbeStatus, RollbackPlanStatus,
            ProductionApplyPortStatus, LegacyFallbackStatus, ObservationWindowID, ObservationWindowComplete,
            SensitiveFieldsRedacted);
    public static bool operator ==(CanonicalRecordingMetadataStageEvidenceReport left, CanonicalRecordingMetadataStageEvidenceReport right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataStageEvidenceReport left, CanonicalRecordingMetadataStageEvidenceReport right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataCanaryStageGate : IEquatable<CanonicalRecordingMetadataCanaryStageGate>
{
    public CanonicalRecordingMetadataCanaryStage RequestedStage { get; set; }
    public bool Allowed { get; set; }
    public int SelectedCandidateLimit { get; set; }
    public bool SelectsAllEligible { get; set; }
    public List<CanonicalRecordingMetadataStageEvidenceBlocker> Blockers { get; set; }
    public CanonicalRecordingMetadataStageEvidenceReport EvidenceReport { get; set; }
    public string Reason { get; set; }

    public CanonicalRecordingMetadataCanaryStageGate(
        CanonicalRecordingMetadataCanaryStagePolicy policy,
        CanonicalCutoverDomain domain,
        CanonicalCutoverToken? token,
        CanonicalRecordingMetadataCutoverEvidence cutoverEvidence)
    {
        var requestedStage = policy.RequestedStage;
        var blockers = new List<CanonicalRecordingMetadataStageEvidenceBlocker>();

        if (!requestedStage.IsExecutable())
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.stageDisabled);
        if (domain != CanonicalCutoverDomain.recordingMetadata)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.unsupportedDomain);
        if (policy.RuntimeSwitchEnabled)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.runtimeSwitchEnabled);
        if (!policy.AllowCandidateExecution)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.candidateExecutionNotApproved);
        if (token?.OwnerApproved != true && cutoverEvidence.CanaryStageEvidence?.OwnerApproved != true)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.ownerApprovalMissing);

        var stageEvidence = cutoverEvidence.CanaryStageEvidence;
        if (stageEvidence == null)
        {
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStageEvidenceMissing);
            RequestedStage = requestedStage;
            Allowed = false;
            SelectedCandidateLimit = 0;
            SelectsAllEligible = false;
            Blockers = new HashSet<CanonicalRecordingMetadataStageEvidenceBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
            EvidenceReport = new CanonicalRecordingMetadataStageEvidenceReport(null, requestedStage, Blockers);
            Reason = "recordingMetadataCanaryStageBlocked";
            return;
        }

        if (stageEvidence.RequestedStage != requestedStage ||
            stageEvidence.PreviousStage != (requestedStage.PreviousStage() ?? CanonicalRecordingMetadataCanaryStage.disabled))
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.stageOrderViolation);
        if (stageEvidence.PreviousStageSuccessCount < requestedStage.MinimumPreviousStageSuccessCount())
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStageInsufficientSuccess);
        if (stageEvidence.PreviousStageFailureCount > 0)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStageFailure);
        if (stageEvidence.PreviousStageRollbackFailureCount > 0)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStageRollbackFailure);
        if (stageEvidence.PreviousStageBlockingDivergenceCount > 0)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStageBlockingDivergence);
        if (stageEvidence.UnresolvedConflictCount > 0)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStageUnresolvedConflict);
        if (stageEvidence.PreviousStagePostconditionFailureCount > 0)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStagePostconditionFailure);
        if (stageEvidence.PreviousStageUnsupportedObjectCount > 0)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStageUnsupportedObject);
        if (!stageEvidence.ObservationWindowComplete)
        {
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.observationWindowIncomplete);
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.previousStageObservationIncomplete);
        }
        if (!stageEvidence.OwnerApproved || token?.OwnerApproved == false)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.ownerApprovalMissing);
        if (!stageEvidence.DryRunEquivalenceStatus.IsPassing() || !cutoverEvidence.DryRunEquivalenceVerified)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.dryRunEquivalenceMissing);
        if (!stageEvidence.ExecutionShadowStatus.IsPassing() || !cutoverEvidence.ExecutionShadowVerified)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.executionShadowMissing);
        if (!stageEvidence.RealDataShadowCopyStatus.IsPassing() || !cutoverEvidence.RealDataShadowCopyVerified)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.realDataShadowCopyMissing);
        if (!stageEvidence.ReadOnlyTransportProbeStatus.IsPassing() || !cutoverEvidence.ReadOnlyTransportProbePassed)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.readOnlyTransportProbeMissing);
        if (!stageEvidence.RollbackPlanStatus.IsPassing()
            || cutoverEvidence.RollbackPlan?.Covers(CanonicalProductionDomain.recordingMetadata) != true
            || !cutoverEvidence.RollbackRehearsalPassed
            || !cutoverEvidence.RollbackVerified)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.rollbackPlanMissing);
        if (!stageEvidence.ProductionApplyPortStatus.IsPassing()
            || !cutoverEvidence.ProductionPortAvailable
            || !cutoverEvidence.RealRootBoundApplyPortAvailable
            || !CanonicalRecordingMetadataApplyPortModeExtensions.IsNonDryRunRootBound(cutoverEvidence.ApplyPortMode)
            || !cutoverEvidence.RootBoundWriteAvailable
            || !cutoverEvidence.AtomicReplaceAvailable
            || !cutoverEvidence.RollbackCheckpointAvailable)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.productionApplyPortUnavailable);
        if (!stageEvidence.LegacyFallbackStatus.IsPassing() || !cutoverEvidence.LegacyFallbackAvailable)
            blockers.Add(CanonicalRecordingMetadataStageEvidenceBlocker.legacyFallbackUnavailable);

        RequestedStage = requestedStage;
        Allowed = blockers.Count == 0;
        SelectedCandidateLimit = requestedStage.NominalCanaryBudget();
        SelectsAllEligible = requestedStage == CanonicalRecordingMetadataCanaryStage.allEligible;
        Blockers = new HashSet<CanonicalRecordingMetadataStageEvidenceBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        EvidenceReport = new CanonicalRecordingMetadataStageEvidenceReport(stageEvidence, requestedStage, Blockers);
        Reason = Allowed ? "recordingMetadataCanaryStageAllowed" : "recordingMetadataCanaryStageBlocked";
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanaryStageGate other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanaryStageGate? other) =>
        other is not null &&
        RequestedStage == other.RequestedStage &&
        Allowed == other.Allowed && SelectedCandidateLimit == other.SelectedCandidateLimit &&
        SelectsAllEligible == other.SelectsAllEligible &&
        Blockers.SequenceEqual(other.Blockers) &&
        EqualityComparer<CanonicalRecordingMetadataStageEvidenceReport>.Default.Equals(EvidenceReport, other.EvidenceReport) &&
        Reason == other.Reason;
    public override int GetHashCode() =>
        HashCode.Combine(RequestedStage, Allowed, SelectedCandidateLimit, SelectsAllEligible, Blockers.Count, EvidenceReport, Reason);
    public static bool operator ==(CanonicalRecordingMetadataCanaryStageGate left, CanonicalRecordingMetadataCanaryStageGate right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanaryStageGate left, CanonicalRecordingMetadataCanaryStageGate right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataCanaryStageResult : IEquatable<CanonicalRecordingMetadataCanaryStageResult>
{
    public CanonicalRecordingMetadataCanaryStage RequestedStage { get; set; }
    public CanonicalRecordingMetadataStageEvidenceStatus Status { get; set; }
    public CanonicalRecordingMetadataCanaryStageGate Gate { get; set; }
    public int SelectedCandidateCount { get; set; }
    public int ExecutedCandidateCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int RollbackFailureCount { get; set; }
    public int SuppressedLegacyDuplicateCount { get; set; }
    public bool RuntimeSwitch { get; set; }
    public CanonicalRecordingMetadataStageEvidenceReport ObservationReport { get; set; }

    public CanonicalRecordingMetadataCanaryStageResult(
        CanonicalRecordingMetadataCanaryStageGate gate,
        CanonicalRecordingMetadataCanarySelectionResult selection,
        CanonicalCutoverResult? result)
    {
        var successCount = result?.Commits.Count(c => c.Committed && c.PreconditionVerified && c.PostconditionVerified) ?? 0;
        var executedCount = result?.CanaryAttemptedCount ?? 0;
        var rollbackFailures = result?.RollbackResults.Count(r => !r.Succeeded) ?? 0;

        RequestedStage = gate.RequestedStage;
        if (!gate.Allowed)
            Status = gate.EvidenceReport.Status;
        else if (rollbackFailures > 0 || (result?.FatalBlocker == true))
            Status = CanonicalRecordingMetadataStageEvidenceStatus.blocked;
        else if (executedCount > 0 && successCount == executedCount)
            Status = CanonicalRecordingMetadataStageEvidenceStatus.passed;
        else
            Status = CanonicalRecordingMetadataStageEvidenceStatus.incomplete;

        Gate = gate;
        SelectedCandidateCount = selection.SelectedCandidates.Count;
        ExecutedCandidateCount = executedCount;
        SuccessCount = successCount;
        FailureCount = Math.Max(0, executedCount - successCount);
        RollbackFailureCount = rollbackFailures;
        SuppressedLegacyDuplicateCount = result?.DuplicateLegacySuppressedActionIDs.Count ?? 0;
        RuntimeSwitch = false;
        ObservationReport = gate.EvidenceReport;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanaryStageResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanaryStageResult? other) =>
        other is not null &&
        RequestedStage == other.RequestedStage && Status == other.Status &&
        EqualityComparer<CanonicalRecordingMetadataCanaryStageGate>.Default.Equals(Gate, other.Gate) &&
        SelectedCandidateCount == other.SelectedCandidateCount &&
        ExecutedCandidateCount == other.ExecutedCandidateCount &&
        SuccessCount == other.SuccessCount && FailureCount == other.FailureCount &&
        RollbackFailureCount == other.RollbackFailureCount &&
        SuppressedLegacyDuplicateCount == other.SuppressedLegacyDuplicateCount &&
        RuntimeSwitch == other.RuntimeSwitch &&
        EqualityComparer<CanonicalRecordingMetadataStageEvidenceReport>.Default.Equals(ObservationReport, other.ObservationReport);
    public override int GetHashCode() =>
        HashCode.Combine(RequestedStage, Status, Gate, SelectedCandidateCount, ExecutedCandidateCount, SuccessCount,
            FailureCount, RollbackFailureCount, SuppressedLegacyDuplicateCount, RuntimeSwitch, ObservationReport);
    public static bool operator ==(CanonicalRecordingMetadataCanaryStageResult left, CanonicalRecordingMetadataCanaryStageResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanaryStageResult left, CanonicalRecordingMetadataCanaryStageResult right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCanaryBlocker
{
    disabled,
    unsupportedDomain,
    unsupportedMode,
    missingInternalCanaryConfiguration,
    canaryBudgetZero,
    canaryBudgetAboveOneDenied,
    unsupportedTrigger,
    unsupportedAction,
    insufficientEvidence,
    unresolvedConflict,
    tombstoneConflict,
    canonicalMoreAggressiveBlockingDivergence,
    noRollbackCheckpoint,
    realApplyPortUnavailable,
    missingReadOnlyTransportProbe,
    alreadyAttemptedFailedCandidate,
    rollbackUnavailable,
    canaryStageEvidenceMissing,
    canaryStageBlocked,
    noEligibleCandidate,
}

public sealed class CanonicalRecordingMetadataCanaryCandidate : IEquatable<CanonicalRecordingMetadataCanaryCandidate>
{
    public string Id => CutoverCandidate.Action.ActionID;
    public CanonicalRecordingMetadataCutoverCandidate CutoverCandidate { get; set; }
    public string ObjectID { get; set; }
    public CanonicalRecordingMetadataCutoverActionKind ActionKind { get; set; }
    public string? HashPrefix { get; set; }

    public CanonicalRecordingMetadataCanaryCandidate(CanonicalRecordingMetadataCutoverCandidate cutoverCandidate)
    {
        CutoverCandidate = cutoverCandidate;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(cutoverCandidate.ObjectID, "unknown-recording")!;
        ActionKind = cutoverCandidate.CutoverActionKind ?? CanonicalRecordingMetadataCutoverActionKind.apply;
        HashPrefix = cutoverCandidate.StableMetadataHash is { } h
            ? CanonicalProductionRedaction.HashPrefix(h.Value)
            : null;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanaryCandidate other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanaryCandidate? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalRecordingMetadataCanaryCandidate left, CanonicalRecordingMetadataCanaryCandidate right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanaryCandidate left, CanonicalRecordingMetadataCanaryCandidate right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataCanarySelectionBlocker : IEquatable<CanonicalRecordingMetadataCanarySelectionBlocker>
{
    public string Id => string.Join("|", ObjectID ?? "run", ActionKind?.ToString() ?? "action", Reason.ToString());
    public string? ObjectID { get; set; }
    public CanonicalRecordingMetadataCutoverActionKind? ActionKind { get; set; }
    public CanonicalRecordingMetadataCanaryBlocker Reason { get; set; }

    public CanonicalRecordingMetadataCanarySelectionBlocker(
        string? objectID,
        CanonicalRecordingMetadataCutoverActionKind? actionKind,
        CanonicalRecordingMetadataCanaryBlocker reason)
    {
        ObjectID = objectID != null
            ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording")
            : null;
        ActionKind = actionKind;
        Reason = reason;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanarySelectionBlocker other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanarySelectionBlocker? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalRecordingMetadataCanarySelectionBlocker left, CanonicalRecordingMetadataCanarySelectionBlocker right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanarySelectionBlocker left, CanonicalRecordingMetadataCanarySelectionBlocker right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataCanarySelectionResult : IEquatable<CanonicalRecordingMetadataCanarySelectionResult>
{
    public List<CanonicalRecordingMetadataCanaryCandidate> SelectedCandidates { get; set; }
    public List<CanonicalRecordingMetadataCanarySelectionBlocker> Blockers { get; set; }
    public int EvaluatedCandidateCount { get; set; }
    public bool NoEligibleCandidate { get; set; }

    public List<CanonicalRecordingMetadataCutoverCandidate> SelectedCutoverCandidates =>
        SelectedCandidates.Select(c => c.CutoverCandidate).ToList();

    public CanonicalRecordingMetadataCanarySelectionResult(
        List<CanonicalRecordingMetadataCanaryCandidate>? selectedCandidates = null,
        List<CanonicalRecordingMetadataCanarySelectionBlocker>? blockers = null,
        int evaluatedCandidateCount = 0,
        bool noEligibleCandidate = false)
    {
        SelectedCandidates = selectedCandidates ?? new List<CanonicalRecordingMetadataCanaryCandidate>();
        Blockers = blockers ?? new List<CanonicalRecordingMetadataCanarySelectionBlocker>();
        EvaluatedCandidateCount = evaluatedCandidateCount;
        NoEligibleCandidate = noEligibleCandidate;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanarySelectionResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanarySelectionResult? other) =>
        other is not null &&
        SelectedCandidates.SequenceEqual(other.SelectedCandidates) &&
        Blockers.SequenceEqual(other.Blockers) &&
        EvaluatedCandidateCount == other.EvaluatedCandidateCount &&
        NoEligibleCandidate == other.NoEligibleCandidate;
    public override int GetHashCode() =>
        HashCode.Combine(SelectedCandidates.Count, Blockers.Count, EvaluatedCandidateCount, NoEligibleCandidate);
    public static bool operator ==(CanonicalRecordingMetadataCanarySelectionResult left, CanonicalRecordingMetadataCanarySelectionResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanarySelectionResult left, CanonicalRecordingMetadataCanarySelectionResult right) => !left.Equals(right);
}

public class CanonicalRecordingMetadataCanarySelector
{
    public CanonicalRecordingMetadataCanarySelector() { }

    public CanonicalRecordingMetadataCanarySelectionResult Select(
        CanonicalSingleDomainCutoverConfiguration configuration,
        CanonicalSyncPlanTrigger trigger,
        CanonicalRecordingMetadataCutoverEvidence evidence,
        List<CanonicalRecordingMetadataCutoverCandidate> candidates,
        HashSet<string>? attemptedFailedActionIDs = null)
    {
        attemptedFailedActionIDs ??= new HashSet<string>();
        var blockers = new List<CanonicalRecordingMetadataCanarySelectionBlocker>();
        var stagePolicy = configuration.Policy.EffectiveRecordingMetadataCanaryStagePolicy;
        var usesStagePolicy = stagePolicy.RequestedStage.IsExecutable();
        CanonicalRecordingMetadataCanaryStageGate? stageGate = usesStagePolicy
            ? new CanonicalRecordingMetadataCanaryStageGate(stagePolicy, configuration.Domain, null, evidence)
            : null;

        if (configuration.Mode == CanonicalCutoverMode.disabled)
            blockers.Add(new(null, null, CanonicalRecordingMetadataCanaryBlocker.disabled));
        if (configuration.Mode != CanonicalCutoverMode.canary)
            blockers.Add(new(null, null, CanonicalRecordingMetadataCanaryBlocker.unsupportedMode));
        if (configuration.Domain != CanonicalCutoverDomain.recordingMetadata)
            blockers.Add(new(null, null, CanonicalRecordingMetadataCanaryBlocker.unsupportedDomain));
        if (configuration.Policy.CanaryMaxObjectsPerSyncRun == 0)
            blockers.Add(new(null, null, CanonicalRecordingMetadataCanaryBlocker.canaryBudgetZero));
        if (configuration.Policy.CanaryMaxObjectsPerSyncRun > 1 && !usesStagePolicy)
            blockers.Add(new(null, null, CanonicalRecordingMetadataCanaryBlocker.canaryBudgetAboveOneDenied));
        if (configuration.Policy.CanaryMaxObjectsPerSyncRun == 1 &&
            !usesStagePolicy &&
            !configuration.Policy.AllowsV87CanaryN1InternalExecution)
            blockers.Add(new(null, null, CanonicalRecordingMetadataCanaryBlocker.missingInternalCanaryConfiguration));
        if (usesStagePolicy && stageGate?.Allowed != true)
        {
            var reason = evidence.CanaryStageEvidence == null
                ? CanonicalRecordingMetadataCanaryBlocker.canaryStageEvidenceMissing
                : CanonicalRecordingMetadataCanaryBlocker.canaryStageBlocked;
            blockers.Add(new(null, null, reason));
        }
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh || trigger == CanonicalSyncPlanTrigger.retryDrainer)
            blockers.Add(new(null, null, CanonicalRecordingMetadataCanaryBlocker.unsupportedTrigger));

        var runBlocked = blockers.Count > 0;
        var selectionLimit = usesStagePolicy
            ? (stageGate?.SelectedCandidateLimit ?? 0)
            : configuration.Policy.CanaryMaxObjectsPerSyncRun;

        var orderedCandidates = candidates.OrderBy(c => c.ObjectID, StringComparer.Ordinal).ToList();
        var selected = new List<CanonicalRecordingMetadataCanaryCandidate>();

        foreach (var candidate in orderedCandidates)
        {
            var reasons = CandidateBlockers(candidate, evidence, attemptedFailedActionIDs);
            if (reasons.Count == 0 && !runBlocked && selected.Count < selectionLimit)
            {
                selected.Add(new CanonicalRecordingMetadataCanaryCandidate(candidate));
                continue;
            }
            blockers.AddRange(reasons.Select(r =>
                new CanonicalRecordingMetadataCanarySelectionBlocker(
                    candidate.ObjectID, candidate.CutoverActionKind, r)));
        }

        if (selected.Count == 0 && candidates.Count > 0 && blockers.Count == 0)
            blockers.Add(new(null, null, CanonicalRecordingMetadataCanaryBlocker.noEligibleCandidate));

        return new CanonicalRecordingMetadataCanarySelectionResult(
            selectedCandidates: selected,
            blockers: blockers,
            evaluatedCandidateCount: candidates.Count,
            noEligibleCandidate: selected.Count == 0);
    }

    private static List<CanonicalRecordingMetadataCanaryBlocker> CandidateBlockers(
        CanonicalRecordingMetadataCutoverCandidate candidate,
        CanonicalRecordingMetadataCutoverEvidence evidence,
        HashSet<string> attemptedFailedActionIDs)
    {
        var blockers = new List<CanonicalRecordingMetadataCanaryBlocker>();
        if (candidate.CutoverActionKind == null)
            return new List<CanonicalRecordingMetadataCanaryBlocker> { CanonicalRecordingMetadataCanaryBlocker.unsupportedAction };

        if (candidate.UnresolvedConflict)
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.unresolvedConflict);
        if (candidate.StableMetadataHash == null || candidate.ExpectedObject == null)
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.insufficientEvidence);
        if ((candidate.LocalObject?.Metadata.IsDeleted ?? false) != (candidate.PeerObject?.Metadata.IsDeleted ?? false) &&
            candidate.LocalObject != null && candidate.PeerObject != null)
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.tombstoneConflict);
        if (!evidence.NoBlockingDivergence)
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.canonicalMoreAggressiveBlockingDivergence);
        if (candidate.RollbackCheckpointID == null || !evidence.RollbackCheckpointAvailable)
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.noRollbackCheckpoint);
        if (!evidence.RollbackVerified || evidence.RollbackPlan?.Covers(CanonicalProductionDomain.recordingMetadata) != true)
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.rollbackUnavailable);
        if (!evidence.RealRootBoundApplyPortAvailable
            || !CanonicalRecordingMetadataApplyPortModeExtensions.IsNonDryRunRootBound(evidence.ApplyPortMode)
            || !evidence.RootBoundWriteAvailable
            || !evidence.AtomicReplaceAvailable)
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.realApplyPortUnavailable);
        if (candidate.CutoverActionKind == CanonicalRecordingMetadataCutoverActionKind.send && !evidence.ReadOnlyTransportProbePassed)
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.missingReadOnlyTransportProbe);
        if (attemptedFailedActionIDs.Contains(candidate.Action.ActionID))
            blockers.Add(CanonicalRecordingMetadataCanaryBlocker.alreadyAttemptedFailedCandidate);

        return blockers;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCanaryObservationStatus
{
    disabled,
    blocked,
    noEligibleCandidate,
    completed,
    fatalBlocker,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCanaryObservationBlocker
{
    gateBlocked,
    noEligibleCandidate,
    commitFailure,
    rollbackFailure,
    sensitiveFieldRedactionRequired,
}

public sealed class CanonicalRecordingMetadataCanaryObservationReport : IEquatable<CanonicalRecordingMetadataCanaryObservationReport>
{
    public CanonicalRecordingMetadataCanaryObservationStatus Status { get; set; }
    public List<CanonicalRecordingMetadataCanaryObservationBlocker> Blockers { get; set; }
    public int CanaryBudget { get; set; }
    public int SelectedCandidateCount { get; set; }
    public int ExecutedCandidateCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int RollbackCount { get; set; }
    public int RollbackFailureCount { get; set; }
    public int LegacyFallbackCount { get; set; }
    public int DuplicateSuppressionCount { get; set; }
    public int NoEligibleCount { get; set; }
    public int FatalBlockerCount { get; set; }
    public CanonicalCutoverDomain Domain { get; set; }
    public bool RuntimeSwitch { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public bool UiMutated { get; set; }
    public bool UploadJobCreated { get; set; }
    public bool SensitiveFieldsRedacted { get; set; }

    public CanonicalRecordingMetadataCanaryObservationReport(
        CanonicalSingleDomainCutoverConfiguration configuration,
        CanonicalRecordingMetadataCanarySelectionResult selection,
        CanonicalCutoverResult? result)
    {
        var rollbackFailures = result?.RollbackResults.Count(r => !r.Succeeded) ?? 0;
        var fatalCount = result?.FatalBlocker == true ? 1 : 0;
        var executedCount = result?.CanaryAttemptedCount ?? 0;
        var successCount = result?.Commits.Count(c => c.Committed) ?? 0;
        var failureCount = Math.Max(0, executedCount - successCount);
        var noEligible = selection.NoEligibleCandidate ? 1 : 0;

        if (configuration.Mode == CanonicalCutoverMode.disabled)
            Status = CanonicalRecordingMetadataCanaryObservationStatus.disabled;
        else if (fatalCount > 0)
            Status = CanonicalRecordingMetadataCanaryObservationStatus.fatalBlocker;
        else if (result?.Gate.Allowed == false)
            Status = CanonicalRecordingMetadataCanaryObservationStatus.blocked;
        else if (noEligible > 0)
            Status = CanonicalRecordingMetadataCanaryObservationStatus.noEligibleCandidate;
        else
            Status = CanonicalRecordingMetadataCanaryObservationStatus.completed;

        var blockers = new List<CanonicalRecordingMetadataCanaryObservationBlocker>();
        if (result?.Gate.Allowed == false)
            blockers.Add(CanonicalRecordingMetadataCanaryObservationBlocker.gateBlocked);
        if (noEligible > 0)
            blockers.Add(CanonicalRecordingMetadataCanaryObservationBlocker.noEligibleCandidate);
        if (failureCount > 0)
            blockers.Add(CanonicalRecordingMetadataCanaryObservationBlocker.commitFailure);
        if (rollbackFailures > 0)
            blockers.Add(CanonicalRecordingMetadataCanaryObservationBlocker.rollbackFailure);

        Status = configuration.Mode == CanonicalCutoverMode.disabled
            ? CanonicalRecordingMetadataCanaryObservationStatus.disabled
            : fatalCount > 0
                ? CanonicalRecordingMetadataCanaryObservationStatus.fatalBlocker
                : result?.Gate.Allowed == false
                    ? CanonicalRecordingMetadataCanaryObservationStatus.blocked
                    : noEligible > 0
                        ? CanonicalRecordingMetadataCanaryObservationStatus.noEligibleCandidate
                        : CanonicalRecordingMetadataCanaryObservationStatus.completed;

        Blockers = new HashSet<CanonicalRecordingMetadataCanaryObservationBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        CanaryBudget = configuration.Policy.CanaryMaxObjectsPerSyncRun;
        SelectedCandidateCount = selection.SelectedCandidates.Count;
        ExecutedCandidateCount = executedCount;
        SuccessCount = successCount;
        FailureCount = failureCount;
        RollbackCount = result?.RollbackResults.Count ?? 0;
        RollbackFailureCount = rollbackFailures;
        LegacyFallbackCount = result?.LegacyFallbackUsed == true ? 1 : 0;
        DuplicateSuppressionCount = result?.DuplicateLegacySuppressedActionIDs.Count ?? 0;
        NoEligibleCount = noEligible;
        FatalBlockerCount = fatalCount;
        Domain = configuration.Domain;
        RuntimeSwitch = false;
        LegacyFallbackAvailable = result?.Gate.LegacyFallbackAvailable ?? false;
        UiMutated = false;
        UploadJobCreated = false;
        SensitiveFieldsRedacted = true;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"status={Status}",
        $"canaryBudget={CanaryBudget}",
        $"selected={SelectedCandidateCount}",
        $"executed={ExecutedCandidateCount}",
        $"success={SuccessCount}",
        $"failure={FailureCount}",
        $"rollback={RollbackCount}",
        $"rollbackFailure={RollbackFailureCount}",
        $"legacyFallback={LegacyFallbackCount}",
        $"duplicateSuppression={DuplicateSuppressionCount}",
        $"noEligible={NoEligibleCount}",
        $"fatalBlocker={FatalBlockerCount}",
        $"domain={Domain}",
        $"runtimeSwitch={RuntimeSwitch}",
        $"legacyFallbackAvailable={LegacyFallbackAvailable}",
        $"uiMutated={UiMutated}",
        $"uploadJobCreated={UploadJobCreated}",
        $"sensitiveFieldsRedacted={SensitiveFieldsRedacted}");

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanaryObservationReport other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanaryObservationReport? other)
    {
        if (other is null) return false;
        return Status == other.Status && Blockers.SequenceEqual(other.Blockers) &&
               CanaryBudget == other.CanaryBudget && SelectedCandidateCount == other.SelectedCandidateCount &&
               ExecutedCandidateCount == other.ExecutedCandidateCount && SuccessCount == other.SuccessCount &&
               FailureCount == other.FailureCount && RollbackCount == other.RollbackCount &&
               RollbackFailureCount == other.RollbackFailureCount && LegacyFallbackCount == other.LegacyFallbackCount &&
               DuplicateSuppressionCount == other.DuplicateSuppressionCount && NoEligibleCount == other.NoEligibleCount &&
               FatalBlockerCount == other.FatalBlockerCount && Domain == other.Domain &&
               RuntimeSwitch == other.RuntimeSwitch && LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
               UiMutated == other.UiMutated && UploadJobCreated == other.UploadJobCreated &&
               SensitiveFieldsRedacted == other.SensitiveFieldsRedacted;
    }
    public override int GetHashCode() =>
        HashCode.Combine(Status, Blockers.Count, CanaryBudget, SelectedCandidateCount, ExecutedCandidateCount, SuccessCount,
            FailureCount, RollbackCount, RollbackFailureCount, LegacyFallbackCount, DuplicateSuppressionCount,
            NoEligibleCount, FatalBlockerCount, Domain, RuntimeSwitch, LegacyFallbackAvailable, UiMutated,
            UploadJobCreated, SensitiveFieldsRedacted);
    public static bool operator ==(CanonicalRecordingMetadataCanaryObservationReport left, CanonicalRecordingMetadataCanaryObservationReport right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanaryObservationReport left, CanonicalRecordingMetadataCanaryObservationReport right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataProductionCommitFailureKind
{
    preconditionMismatch,
    postconditionMismatch,
    transportFailureBeforeSend,
    applyFailureBeforeCommit,
    applyFailureAfterPartialCommit,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCommitFailureInjection
{
    none,
    preconditionMismatch,
    postconditionMismatch,
    applyFailureBeforeCommit,
    applyFailureAfterPartialCommit,
    transportFailureBeforeSend,
    transportFailureAfterAcceptedResponse,
    rollbackFailure,
    duplicateCommit,
    idempotentReplay,
    unsupportedSideEffect,
    unexpectedSideEffect,
    missingRollbackCheckpoint,
}

public sealed class CanonicalRecordingMetadataProductionCommitResult : IEquatable<CanonicalRecordingMetadataProductionCommitResult>
{
    public string ActionID { get; set; }
    public string ObjectID { get; set; }
    public CanonicalRecordingMetadataCutoverActionKind ActionKind { get; set; }
    public bool Committed { get; set; }
    public bool PartialCommit { get; set; }
    public bool PreconditionVerified { get; set; }
    public bool PostconditionVerified { get; set; }
    public string? RoutePath { get; set; }
    public string? MetadataHashPrefix { get; set; }
    public CanonicalProductionSideEffect? SideEffect { get; set; }
    public List<CanonicalProductionSideEffect> SideEffects { get; set; }
    public CanonicalRecordingMetadataProductionCommitFailureKind? FailureKind { get; set; }
    public string Reason { get; set; }

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
        CanonicalProductionSideEffect? sideEffect = null,
        List<CanonicalProductionSideEffect>? sideEffects = null,
        CanonicalRecordingMetadataProductionCommitFailureKind? failureKind = null,
        string reason = "")
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, actionKind.ToString())!;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording")!;
        ActionKind = actionKind;
        Committed = committed;
        PartialCommit = partialCommit;
        PreconditionVerified = preconditionVerified;
        PostconditionVerified = postconditionVerified;
        RoutePath = routePath != null
            ? CanonicalProductionRedaction.SafeDiagnosticText(routePath)
            : null;
        MetadataHashPrefix = metadataHash is { } h
            ? CanonicalProductionRedaction.HashPrefix(h.Value)
            : null;
        SideEffect = sideEffect;
        SideEffects = sideEffects ?? (sideEffect != null ? new List<CanonicalProductionSideEffect> { sideEffect } : new List<CanonicalProductionSideEffect>());
        FailureKind = failureKind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (committed ? "committed" : "failed") ?? "unknown";
    }

    public static CanonicalRecordingMetadataProductionCommitResult Success(
        CanonicalRecordingMetadataCutoverCandidate candidate,
        CanonicalProductionSideEffect? sideEffect = null)
    {
        var kind = candidate.CutoverActionKind ?? CanonicalRecordingMetadataCutoverActionKind.apply;
        return new CanonicalRecordingMetadataProductionCommitResult(
            actionID: candidate.Action.ActionID,
            objectID: candidate.ObjectID,
            actionKind: kind,
            committed: true,
            routePath: kind == CanonicalRecordingMetadataCutoverActionKind.send ? "/sync/apply-metadata" : null,
            metadataHash: candidate.StableMetadataHash,
            sideEffect: sideEffect,
            reason: kind == CanonicalRecordingMetadataCutoverActionKind.send
                ? "recordingMetadataSendCommitted"
                : "recordingMetadataApplyCommitted");
    }

    public static CanonicalRecordingMetadataProductionCommitResult Failure(
        CanonicalRecordingMetadataCutoverCandidate candidate,
        CanonicalRecordingMetadataProductionCommitFailureKind failureKind,
        bool partialCommit = false,
        string reason = "")
    {
        return new CanonicalRecordingMetadataProductionCommitResult(
            actionID: candidate.Action.ActionID,
            objectID: candidate.ObjectID,
            actionKind: candidate.CutoverActionKind ?? CanonicalRecordingMetadataCutoverActionKind.apply,
            committed: false,
            partialCommit: partialCommit,
            preconditionVerified: failureKind != CanonicalRecordingMetadataProductionCommitFailureKind.preconditionMismatch,
            postconditionVerified: failureKind != CanonicalRecordingMetadataProductionCommitFailureKind.postconditionMismatch,
            routePath: candidate.RequiresNetworkSend ? "/sync/apply-metadata" : null,
            metadataHash: candidate.StableMetadataHash,
            failureKind: failureKind,
            reason: reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataProductionCommitResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataProductionCommitResult? other) =>
        other is not null && ActionID == other.ActionID && ObjectID == other.ObjectID &&
        ActionKind == other.ActionKind && Committed == other.Committed && PartialCommit == other.PartialCommit &&
        PreconditionVerified == other.PreconditionVerified && PostconditionVerified == other.PostconditionVerified &&
        RoutePath == other.RoutePath && MetadataHashPrefix == other.MetadataHashPrefix &&
        EqualityComparer<CanonicalProductionSideEffect?>.Default.Equals(SideEffect, other.SideEffect) &&
        SideEffects.SequenceEqual(other.SideEffects) && FailureKind == other.FailureKind && Reason == other.Reason;
    public override int GetHashCode() =>
        HashCode.Combine(ActionID, ObjectID, ActionKind, Committed, PartialCommit, PreconditionVerified,
            PostconditionVerified, RoutePath, MetadataHashPrefix, SideEffect, SideEffects.Count, FailureKind, Reason);
    public static bool operator ==(CanonicalRecordingMetadataProductionCommitResult left, CanonicalRecordingMetadataProductionCommitResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataProductionCommitResult left, CanonicalRecordingMetadataProductionCommitResult right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataRollbackExecutionResult : IEquatable<CanonicalRecordingMetadataRollbackExecutionResult>
{
    public string CheckpointID { get; set; }
    public bool Succeeded { get; set; }
    public bool Fatal { get; set; }
    public string Reason { get; set; }
    public CanonicalRollbackResult? RollbackResult { get; set; }

    public CanonicalRecordingMetadataRollbackExecutionResult(
        string checkpointID,
        bool succeeded,
        bool fatal = false,
        string reason = "",
        CanonicalRollbackResult? rollbackResult = null)
    {
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "recording-metadata-checkpoint")!;
        Succeeded = succeeded;
        Fatal = fatal;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (succeeded ? "rollbackCompleted" : "rollbackFailed") ?? "unknown";
        RollbackResult = rollbackResult;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataRollbackExecutionResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataRollbackExecutionResult? other) =>
        other is not null && CheckpointID == other.CheckpointID && Succeeded == other.Succeeded &&
        Fatal == other.Fatal && Reason == other.Reason &&
        EqualityComparer<CanonicalRollbackResult?>.Default.Equals(RollbackResult, other.RollbackResult);
    public override int GetHashCode() => HashCode.Combine(CheckpointID, Succeeded, Fatal, Reason, RollbackResult);
    public static bool operator ==(CanonicalRecordingMetadataRollbackExecutionResult left, CanonicalRecordingMetadataRollbackExecutionResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataRollbackExecutionResult left, CanonicalRecordingMetadataRollbackExecutionResult right) => !left.Equals(right);
}

public interface ICanonicalRecordingMetadataCutoverExecutor
{
    Task<CanonicalRecordingMetadataProductionCommitResult> CommitRecordingMetadata(CanonicalRecordingMetadataCutoverCandidate candidate);
    Task<CanonicalRecordingMetadataRollbackExecutionResult> RollbackRecordingMetadata(CanonicalRecordingMetadataCutoverCandidate candidate, CanonicalCutoverFailure reason);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCutoverDiagnosticKind
{
    canonicalRecordingMetadataCutoverGateEvaluated,
    canonicalRecordingMetadataCutoverGateBlocked,
    canonicalRecordingMetadataCutoverGateAllowed,
    canonicalRecordingMetadataCanaryN1Configured,
    canonicalRecordingMetadataCanaryCandidateSelectionStarted,
    canonicalRecordingMetadataCanaryCandidateSelected,
    canonicalRecordingMetadataCanaryNoEligibleCandidate,
    canonicalRecordingMetadataCanaryStarted,
    canonicalRecordingMetadataCanaryCompleted,
    canonicalRecordingMetadataCanaryFailed,
    canonicalRecordingMetadataCanaryBudgetExhausted,
    canonicalRecordingMetadataCommitExecutorCreated,
    canonicalRecordingMetadataCommitPreconditionEvaluated,
    canonicalRecordingMetadataCommitPreconditionFailed,
    canonicalRecordingMetadataCanaryCommitStarted,
    canonicalRecordingMetadataCanaryCommitCompleted,
    canonicalRecordingMetadataCanaryCommitFailed,
    canonicalRecordingMetadataProductionCommitStarted,
    canonicalRecordingMetadataProductionCommitCompleted,
    canonicalRecordingMetadataProductionCommitFailed,
    canonicalRecordingMetadataCanaryPostconditionVerified,
    canonicalRecordingMetadataCanaryPostconditionFailed,
    canonicalRecordingMetadataPostconditionVerified,
    canonicalRecordingMetadataPostconditionFailed,
    canonicalRecordingMetadataCanaryLegacyFallbackUsed,
    canonicalRecordingMetadataLegacyFallbackUsed,
    canonicalRecordingMetadataLegacyFallbackPreserved,
    canonicalRecordingMetadataDuplicateSuppressionAllowed,
    canonicalRecordingMetadataDuplicateSuppressionSkipped,
    canonicalRecordingMetadataDuplicateLegacySuppressed,
    canonicalRecordingMetadataRollbackCheckpointCreated,
    canonicalRecordingMetadataCanaryRollbackStarted,
    canonicalRecordingMetadataCanaryRollbackCompleted,
    canonicalRecordingMetadataCanaryRollbackFailed,
    canonicalRecordingMetadataRollbackStarted,
    canonicalRecordingMetadataRollbackCompleted,
    canonicalRecordingMetadataRollbackFailed,
    canonicalRecordingMetadataCanaryFatalBlocker,
    canonicalRecordingMetadataRollbackFatalBlocker,
    canonicalUIProjectionParallelReadStarted,
    canonicalUIProjectionParallelReadEquivalent,
    canonicalUIProjectionParallelReadDivergent,
    canonicalRecordingMetadataRetirementCandidate,
    canonicalRecordingMetadataRetirementBlocked,
}

public sealed class CanonicalRecordingMetadataCutoverDiagnostic : IEquatable<CanonicalRecordingMetadataCutoverDiagnostic>
{
    public string Id => string.Join("|",
        Kind.ToString(),
        ObjectID ?? "run",
        Action ?? "",
        Result ?? "",
        Reason ?? "");

    public CanonicalRecordingMetadataCutoverDiagnosticKind Kind { get; set; }
    public string? SyncRunID { get; set; }
    public CanonicalSyncPlanTrigger Trigger { get; set; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; set; }
    public CanonicalCutoverDomain Domain { get; set; }
    public string? ObjectID { get; set; }
    public string? Action { get; set; }
    public string? Result { get; set; }
    public string? Reason { get; set; }
    public string? HashPrefix { get; set; }

    public CanonicalRecordingMetadataCutoverDiagnostic(
        CanonicalRecordingMetadataCutoverDiagnosticKind kind,
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalCutoverDomain domain = CanonicalCutoverDomain.recordingMetadata,
        string? objectID = null,
        string? action = null,
        string? result = null,
        string? reason = null,
        CanonicalHash? hash = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null
            ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run")
            : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Domain = domain;
        ObjectID = objectID != null
            ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording")
            : null;
        Action = CanonicalProductionRedaction.SafeDiagnosticText(action);
        Result = CanonicalProductionRedaction.SafeDiagnosticText(result);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = hash is { } h
            ? CanonicalProductionRedaction.HashPrefix(h.Value)
            : null;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCutoverDiagnostic other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCutoverDiagnostic? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalRecordingMetadataCutoverDiagnostic left, CanonicalRecordingMetadataCutoverDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCutoverDiagnostic left, CanonicalRecordingMetadataCutoverDiagnostic right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataUIParallelProjectionResult : IEquatable<CanonicalRecordingMetadataUIParallelProjectionResult>
{
    public string ObjectID { get; set; }
    public bool Equivalent { get; set; }
    public bool MutatedUI { get; set; }
    public string? CanonicalHashPrefix { get; set; }
    public string? DisplayHashPrefix { get; set; }
    public string Reason { get; set; }

    public CanonicalRecordingMetadataUIParallelProjectionResult(
        string objectID,
        bool equivalent,
        CanonicalHash? canonicalHash,
        CanonicalHash? displayHash,
        string reason)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording")!;
        Equivalent = equivalent;
        MutatedUI = false;
        CanonicalHashPrefix = canonicalHash is { } ch
            ? CanonicalProductionRedaction.HashPrefix(ch.Value)
            : null;
        DisplayHashPrefix = displayHash is { } dh
            ? CanonicalProductionRedaction.HashPrefix(dh.Value)
            : null;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason)
            ?? (equivalent ? "uiProjectionEquivalent" : "uiProjectionDivergent")
            ?? "unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataUIParallelProjectionResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataUIParallelProjectionResult? other) =>
        other is not null && ObjectID == other.ObjectID && Equivalent == other.Equivalent &&
        MutatedUI == other.MutatedUI && CanonicalHashPrefix == other.CanonicalHashPrefix &&
        DisplayHashPrefix == other.DisplayHashPrefix && Reason == other.Reason;
    public override int GetHashCode() =>
        HashCode.Combine(ObjectID, Equivalent, MutatedUI, CanonicalHashPrefix, DisplayHashPrefix, Reason);
    public static bool operator ==(CanonicalRecordingMetadataUIParallelProjectionResult left, CanonicalRecordingMetadataUIParallelProjectionResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataUIParallelProjectionResult left, CanonicalRecordingMetadataUIParallelProjectionResult right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataRetirementReadiness : IEquatable<CanonicalRecordingMetadataRetirementReadiness>
{
    public bool RetirementCandidate { get; set; }
    public bool CanaryPassed { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public List<CanonicalCutoverFailure> Blockers { get; set; }

    public CanonicalRecordingMetadataRetirementReadiness(
        bool retirementCandidate,
        bool canaryPassed,
        bool legacyFallbackAvailable,
        List<CanonicalCutoverFailure> blockers)
    {
        RetirementCandidate = retirementCandidate;
        CanaryPassed = canaryPassed;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        Blockers = new HashSet<CanonicalCutoverFailure>(blockers)
            .OrderBy(f => f.ToString()).ToList();
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataRetirementReadiness other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataRetirementReadiness? other) =>
        other is not null && RetirementCandidate == other.RetirementCandidate &&
        CanaryPassed == other.CanaryPassed && LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        Blockers.SequenceEqual(other.Blockers);
    public override int GetHashCode() =>
        HashCode.Combine(RetirementCandidate, CanaryPassed, LegacyFallbackAvailable, Blockers.Count);
    public static bool operator ==(CanonicalRecordingMetadataRetirementReadiness left, CanonicalRecordingMetadataRetirementReadiness right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataRetirementReadiness left, CanonicalRecordingMetadataRetirementReadiness right) => !left.Equals(right);
}

public sealed class CanonicalCutoverResult : IEquatable<CanonicalCutoverResult>
{
    public CanonicalCutoverGate Gate { get; set; }
    public List<CanonicalRecordingMetadataProductionCommitResult> Commits { get; set; }
    public List<CanonicalRecordingMetadataRollbackExecutionResult> RollbackResults { get; set; }
    public List<CanonicalRecordingMetadataCutoverDiagnostic> Diagnostics { get; set; }
    public bool LegacyFallbackUsed { get; set; }
    public List<string> DuplicateLegacySuppressedActionIDs { get; set; }
    public int CanaryAttemptedCount { get; set; }
    public bool CanarySucceeded { get; set; }
    public bool FatalBlocker { get; set; }
    public CanonicalRecordingMetadataUIParallelProjectionResult? UiProjection { get; set; }
    public CanonicalRecordingMetadataRetirementReadiness RetirementReadiness { get; set; }
    public CanonicalRecordingMetadataCanaryObservationReport? ObservationReport { get; set; }
    public CanonicalRecordingMetadataCanaryStageResult? CanaryStageResult { get; set; }

    public bool Succeeded => Gate.Allowed && !FatalBlocker && Commits.All(c => c.Committed);

    public CanonicalCutoverResult(
        CanonicalCutoverGate gate,
        List<CanonicalRecordingMetadataProductionCommitResult>? commits = null,
        List<CanonicalRecordingMetadataRollbackExecutionResult>? rollbackResults = null,
        List<CanonicalRecordingMetadataCutoverDiagnostic>? diagnostics = null,
        bool legacyFallbackUsed = false,
        List<string>? duplicateLegacySuppressedActionIDs = null,
        int canaryAttemptedCount = 0,
        bool canarySucceeded = false,
        bool fatalBlocker = false,
        CanonicalRecordingMetadataUIParallelProjectionResult? uiProjection = null,
        CanonicalRecordingMetadataRetirementReadiness? retirementReadiness = null,
        CanonicalRecordingMetadataCanaryObservationReport? observationReport = null,
        CanonicalRecordingMetadataCanaryStageResult? canaryStageResult = null)
    {
        Gate = gate;
        Commits = commits ?? new List<CanonicalRecordingMetadataProductionCommitResult>();
        RollbackResults = rollbackResults ?? new List<CanonicalRecordingMetadataRollbackExecutionResult>();
        Diagnostics = diagnostics ?? new List<CanonicalRecordingMetadataCutoverDiagnostic>();
        LegacyFallbackUsed = legacyFallbackUsed;
        DuplicateLegacySuppressedActionIDs = duplicateLegacySuppressedActionIDs ?? new List<string>();
        CanaryAttemptedCount = canaryAttemptedCount;
        CanarySucceeded = canarySucceeded;
        FatalBlocker = fatalBlocker;
        UiProjection = uiProjection;
        RetirementReadiness = retirementReadiness
            ?? new CanonicalRecordingMetadataRetirementReadiness(false, false, false, new List<CanonicalCutoverFailure>());
        ObservationReport = observationReport;
        CanaryStageResult = canaryStageResult;
    }

    public override bool Equals(object? obj) => obj is CanonicalCutoverResult other && Equals(other);
    public bool Equals(CanonicalCutoverResult? other) =>
        other is not null && EqualityComparer<CanonicalCutoverGate>.Default.Equals(Gate, other.Gate) &&
        Commits.SequenceEqual(other.Commits) && RollbackResults.SequenceEqual(other.RollbackResults) &&
        Diagnostics.SequenceEqual(other.Diagnostics) && LegacyFallbackUsed == other.LegacyFallbackUsed &&
        DuplicateLegacySuppressedActionIDs.SequenceEqual(other.DuplicateLegacySuppressedActionIDs) &&
        CanaryAttemptedCount == other.CanaryAttemptedCount && CanarySucceeded == other.CanarySucceeded &&
        FatalBlocker == other.FatalBlocker &&
        EqualityComparer<CanonicalRecordingMetadataUIParallelProjectionResult?>.Default.Equals(UiProjection, other.UiProjection) &&
        EqualityComparer<CanonicalRecordingMetadataRetirementReadiness>.Default.Equals(RetirementReadiness, other.RetirementReadiness) &&
        EqualityComparer<CanonicalRecordingMetadataCanaryObservationReport?>.Default.Equals(ObservationReport, other.ObservationReport) &&
        EqualityComparer<CanonicalRecordingMetadataCanaryStageResult?>.Default.Equals(CanaryStageResult, other.CanaryStageResult);
    public override int GetHashCode() =>
        HashCode.Combine(Gate, Commits.Count, RollbackResults.Count, Diagnostics.Count, LegacyFallbackUsed,
            DuplicateLegacySuppressedActionIDs.Count, CanaryAttemptedCount, CanarySucceeded, FatalBlocker,
            UiProjection, RetirementReadiness, ObservationReport, CanaryStageResult);
    public static bool operator ==(CanonicalCutoverResult left, CanonicalCutoverResult right) => left.Equals(right);
    public static bool operator !=(CanonicalCutoverResult left, CanonicalCutoverResult right) => !left.Equals(right);
}
