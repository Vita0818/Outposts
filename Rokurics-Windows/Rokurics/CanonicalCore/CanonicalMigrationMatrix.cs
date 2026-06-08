using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationDomain
{
    recordingMetadata,
    generatedArtifacts,
    libraryMetadata,
    tombstoneConflict,
    audioUpload,
    uiProjection,
    legacyRetirement
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationMatrixStageStatus
{
    notStarted,
    evidenceMissing,
    complete,
    activePilot,
    nextPilotCandidate,
    staticOnly,
    blocked,
    writeSideCanaryObserved,
    readSideObserved,
    observationComplete,
    retirementCandidateReady,
    retirementBlocked
}

public static class CanonicalMigrationMatrixStageStatusExtensions
{
    public static bool IsReached(this CanonicalMigrationMatrixStageStatus status)
    {
        return status switch
        {
            CanonicalMigrationMatrixStageStatus.complete or
            CanonicalMigrationMatrixStageStatus.activePilot or
            CanonicalMigrationMatrixStageStatus.nextPilotCandidate or
            CanonicalMigrationMatrixStageStatus.writeSideCanaryObserved or
            CanonicalMigrationMatrixStageStatus.readSideObserved or
            CanonicalMigrationMatrixStageStatus.observationComplete or
            CanonicalMigrationMatrixStageStatus.retirementCandidateReady => true,
            _ => false
        };
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationDomainBlocker
{
    missingExplicitPilot,
    multipleActivePilots,
    nonLibraryMetadataActivePilot,
    nonPilotDomainNotStaticOnly,
    stageSkipped,
    canaryWithoutPreviousStageEvidence,
    readSideCutoverWithoutWriteSideCutover,
    retiredWithoutReadSideCutover,
    retiredWithoutObservation,
    retiredWithoutFallbackReadiness,
    audioUploadBlockedUntilLibraryMetadataPilotComplete,
    generatedArtifactsNextPilotBeforeLibraryMetadataObservation,
    generatedArtifactsActivePilotDeniedV821,
    generatedArtifactsActivePilotBeforeNextPilotCandidate,
    generatedArtifactsActivePilotBeforeLibraryMetadataObservation,
    generatedArtifactsStagedCanaryBeforeN1,
    tombstoneConflictNextPilotBeforeGeneratedArtifactsObservation,
    audioUploadActivePilotDeniedV821,
    tombstoneConflictActivePilotDeniedV821,
    tombstoneConflictActivePilotDeniedV826,
    tombstoneConflictActivePilotBeforeNextPilotCandidate,
    tombstoneConflictActivePilotBeforeGeneratedArtifactsObservation,
    legacyRetirementEnabledDeniedV821,
    releaseDefaultCutoverEnabled,
    runtimeSwitchEnabled,
    legacyRetirementBeforeReadSideCutover,
    diagnosticsNotRedacted,
    missingMachineParts,
    missingAppSeam,
    cutoverNotDefaultOff,
    productionInjectionPresent,
    readPathNotLegacy,
    testsMissing
}

public sealed class CanonicalMigrationDomainPolicy : IEquatable<CanonicalMigrationDomainPolicy>
{
    public CanonicalMigrationDomain Domain { get; set; }
    public Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus> StageStatuses { get; set; }
    public bool ActivePilot { get; set; }
    public bool ActivePilotExplicit { get; set; }
    public bool StaticOnly { get; set; }
    public bool BlockedForRealMigration { get; set; }
    public bool DefaultCutoverEnabled { get; set; }
    public bool ReleaseDefaultEnabledCutover { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool LegacySuppressionAllowed { get; set; }
    public bool NoProductionInjection { get; set; }
    public bool ReadPathLegacy { get; set; }
    public bool WriteSideCutoverSucceeded { get; set; }
    public bool ObservationComplete { get; set; }
    public bool FallbackReady { get; set; }

    public CanonicalMigrationDomainPolicy(
        CanonicalMigrationDomain domain,
        Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus>? stageStatuses = null,
        bool activePilot = false,
        bool activePilotExplicit = false,
        bool staticOnly = true,
        bool blockedForRealMigration = true,
        bool defaultCutoverEnabled = false,
        bool releaseDefaultEnabledCutover = false,
        bool runtimeSwitchEnabled = false,
        bool legacySuppressionAllowed = false,
        bool noProductionInjection = true,
        bool readPathLegacy = true,
        bool writeSideCutoverSucceeded = false,
        bool observationComplete = false,
        bool fallbackReady = true)
    {
        Domain = domain;
        StageStatuses = stageStatuses ?? new Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus>();
        ActivePilot = activePilot;
        ActivePilotExplicit = activePilotExplicit;
        StaticOnly = staticOnly;
        BlockedForRealMigration = blockedForRealMigration;
        DefaultCutoverEnabled = defaultCutoverEnabled;
        ReleaseDefaultEnabledCutover = releaseDefaultEnabledCutover;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        LegacySuppressionAllowed = legacySuppressionAllowed;
        NoProductionInjection = noProductionInjection;
        ReadPathLegacy = readPathLegacy;
        WriteSideCutoverSucceeded = writeSideCutoverSucceeded;
        ObservationComplete = observationComplete;
        FallbackReady = fallbackReady;
    }

    public CanonicalMigrationMatrixStageStatus StatusFor(CanonicalMigrationStage stage)
    {
        if (stage == CanonicalMigrationStage.notStarted)
            return CanonicalMigrationMatrixStageStatus.complete;
        return StageStatuses.TryGetValue(stage, out var s) ? s : CanonicalMigrationMatrixStageStatus.notStarted;
    }

    public bool HasReached(CanonicalMigrationStage stage)
    {
        return StatusFor(stage).IsReached();
    }

    public bool HasActiveCanaryOrCutover => ActivePilot
        || HasReached(CanonicalMigrationStage.canaryN1)
        || HasReached(CanonicalMigrationStage.expandedCanary)
        || HasReached(CanonicalMigrationStage.domainCutover)
        || HasReached(CanonicalMigrationStage.readSideCutover);

    public string DiagnosticsSummary => string.Join(",",
        $"domain={Domain}",
        $"activePilot={ActivePilot}",
        $"explicit={ActivePilotExplicit}",
        $"staticOnly={StaticOnly}",
        $"blockedForRealMigration={BlockedForRealMigration}",
        $"defaultCutoverEnabled={DefaultCutoverEnabled}",
        $"runtimeSwitchEnabled={RuntimeSwitchEnabled}",
        $"readPathLegacy={ReadPathLegacy}"
    );

    public override bool Equals(object? obj) => obj is CanonicalMigrationDomainPolicy other && Equals(other);
    public bool Equals(CanonicalMigrationDomainPolicy? other) =>
        other is not null &&
        Domain == other.Domain &&
        DictionaryEquals(StageStatuses, other.StageStatuses) &&
        ActivePilot == other.ActivePilot &&
        ActivePilotExplicit == other.ActivePilotExplicit &&
        StaticOnly == other.StaticOnly &&
        BlockedForRealMigration == other.BlockedForRealMigration &&
        DefaultCutoverEnabled == other.DefaultCutoverEnabled &&
        ReleaseDefaultEnabledCutover == other.ReleaseDefaultEnabledCutover &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        LegacySuppressionAllowed == other.LegacySuppressionAllowed &&
        NoProductionInjection == other.NoProductionInjection &&
        ReadPathLegacy == other.ReadPathLegacy &&
        WriteSideCutoverSucceeded == other.WriteSideCutoverSucceeded &&
        ObservationComplete == other.ObservationComplete &&
        FallbackReady == other.FallbackReady;

    public override int GetHashCode() =>
        HashCode.Combine(Domain, ActivePilot, ActivePilotExplicit, StaticOnly, BlockedForRealMigration,
            DefaultCutoverEnabled, ReleaseDefaultEnabledCutover, RuntimeSwitchEnabled, LegacySuppressionAllowed,
            NoProductionInjection, ReadPathLegacy, WriteSideCutoverSucceeded, ObservationComplete, FallbackReady);

    public static bool operator ==(CanonicalMigrationDomainPolicy left, CanonicalMigrationDomainPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalMigrationDomainPolicy left, CanonicalMigrationDomainPolicy right) => !left.Equals(right);

    private static bool DictionaryEquals(
        Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus> a,
        Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
        }
        return true;
    }

    public static CanonicalMigrationDomainPolicy V813RecordingMetadata()
    {
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.recordingMetadata,
            stageStatuses: CompletedThroughAppSeam(),
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true
        );
    }

    public static CanonicalMigrationDomainPolicy V813GeneratedArtifacts()
    {
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.generatedArtifacts,
            stageStatuses: CompletedThroughAppSeam(),
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true
        );
    }

    public static CanonicalMigrationDomainPolicy V821GeneratedArtifactsNextPilotCandidate(
        CanonicalGeneratedArtifactTemplateReport? templateReport = null)
    {
        var report = templateReport ?? CanonicalGeneratedArtifactTemplateReport.CurrentV821Audit();
        var statuses = CompletedThroughAppSeam();
        statuses[CanonicalMigrationStage.nextPilotCandidate] = report.ReadyForNextPilotN0
            ? CanonicalMigrationMatrixStageStatus.nextPilotCandidate
            : CanonicalMigrationMatrixStageStatus.blocked;
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.generatedArtifacts,
            stageStatuses: statuses,
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true,
            defaultCutoverEnabled: false,
            releaseDefaultEnabledCutover: false,
            runtimeSwitchEnabled: false,
            legacySuppressionAllowed: false,
            noProductionInjection: true,
            readPathLegacy: true,
            writeSideCutoverSucceeded: false,
            observationComplete: false,
            fallbackReady: true
        );
    }

    public static CanonicalMigrationDomainPolicy V822GeneratedArtifactsActivePilot(
        CanonicalGeneratedArtifactTemplateReport? templateReport = null)
    {
        var report = templateReport ?? CanonicalGeneratedArtifactTemplateReport.CurrentV821Audit();
        var statuses = CompletedThroughAppSeam();
        bool ready = report.ReadyForNextPilotN0;
        statuses[CanonicalMigrationStage.nextPilotCandidate] = ready
            ? CanonicalMigrationMatrixStageStatus.complete
            : CanonicalMigrationMatrixStageStatus.blocked;
        statuses[CanonicalMigrationStage.canaryN0] = ready
            ? CanonicalMigrationMatrixStageStatus.activePilot
            : CanonicalMigrationMatrixStageStatus.blocked;
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.generatedArtifacts,
            stageStatuses: statuses,
            activePilot: ready,
            activePilotExplicit: ready,
            staticOnly: !ready,
            blockedForRealMigration: !ready,
            defaultCutoverEnabled: false,
            releaseDefaultEnabledCutover: false,
            runtimeSwitchEnabled: false,
            legacySuppressionAllowed: false,
            noProductionInjection: true,
            readPathLegacy: true,
            writeSideCutoverSucceeded: false,
            observationComplete: false,
            fallbackReady: true
        );
    }

    public static CanonicalMigrationDomainPolicy V824GeneratedArtifactsStagedCanary(
        CanonicalGeneratedArtifactTemplateReport? templateReport = null)
    {
        var report = templateReport ?? CanonicalGeneratedArtifactTemplateReport.CurrentV821Audit();
        var statuses = CompletedThroughAppSeam();
        bool ready = report.ReadyForNextPilotN0;
        statuses[CanonicalMigrationStage.nextPilotCandidate] = ready
            ? CanonicalMigrationMatrixStageStatus.complete
            : CanonicalMigrationMatrixStageStatus.blocked;
        statuses[CanonicalMigrationStage.canaryN0] = ready
            ? CanonicalMigrationMatrixStageStatus.complete
            : CanonicalMigrationMatrixStageStatus.blocked;
        statuses[CanonicalMigrationStage.canaryN1] = ready
            ? CanonicalMigrationMatrixStageStatus.complete
            : CanonicalMigrationMatrixStageStatus.blocked;
        statuses[CanonicalMigrationStage.expandedCanary] = ready
            ? CanonicalMigrationMatrixStageStatus.activePilot
            : CanonicalMigrationMatrixStageStatus.blocked;
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.generatedArtifacts,
            stageStatuses: statuses,
            activePilot: ready,
            activePilotExplicit: ready,
            staticOnly: !ready,
            blockedForRealMigration: !ready,
            defaultCutoverEnabled: false,
            releaseDefaultEnabledCutover: false,
            runtimeSwitchEnabled: false,
            legacySuppressionAllowed: false,
            noProductionInjection: true,
            readPathLegacy: true,
            writeSideCutoverSucceeded: false,
            observationComplete: false,
            fallbackReady: true
        );
    }

    public static CanonicalMigrationDomainPolicy V813LibraryMetadataPilot()
    {
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.libraryMetadata,
            stageStatuses: CompletedThroughAppSeam(CanonicalMigrationStage.appSeamDefaultOff),
            activePilot: true,
            activePilotExplicit: true,
            staticOnly: false,
            blockedForRealMigration: false
        );
    }

    public static CanonicalMigrationDomainPolicy V813TombstoneConflict()
    {
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.tombstoneConflict,
            stageStatuses: new Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus>
            {
                [CanonicalMigrationStage.projected] = CanonicalMigrationMatrixStageStatus.complete,
                [CanonicalMigrationStage.planned] = CanonicalMigrationMatrixStageStatus.complete,
                [CanonicalMigrationStage.noCommit] = CanonicalMigrationMatrixStageStatus.complete,
                [CanonicalMigrationStage.realApplyPort] = CanonicalMigrationMatrixStageStatus.complete,
                [CanonicalMigrationStage.commitExecutor] = CanonicalMigrationMatrixStageStatus.complete
            },
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true
        );
    }

    public static CanonicalMigrationDomainPolicy V826TombstoneConflictNextPilotCandidate(
        CanonicalTombstoneConflictTemplateReport? templateReport = null,
        bool generatedArtifactsTemplateCompleteOrObservationReady = false)
    {
        var report = templateReport ?? CanonicalTombstoneConflictTemplateReport.CurrentV826Audit();
        var statuses = CompletedThroughAppSeam();
        bool ready = report.ReadyForNextPilotN0 && generatedArtifactsTemplateCompleteOrObservationReady;
        statuses[CanonicalMigrationStage.nextPilotCandidate] = ready
            ? CanonicalMigrationMatrixStageStatus.nextPilotCandidate
            : CanonicalMigrationMatrixStageStatus.blocked;
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.tombstoneConflict,
            stageStatuses: statuses,
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true,
            defaultCutoverEnabled: false,
            releaseDefaultEnabledCutover: false,
            runtimeSwitchEnabled: false,
            legacySuppressionAllowed: false,
            noProductionInjection: true,
            readPathLegacy: true,
            writeSideCutoverSucceeded: false,
            observationComplete: false,
            fallbackReady: true
        );
    }

    public static CanonicalMigrationDomainPolicy V827TombstoneConflictActivePilot(
        CanonicalTombstoneConflictTemplateReport? templateReport = null,
        bool generatedArtifactsTemplateCompleteOrObservationReady = false)
    {
        var report = templateReport ?? CanonicalTombstoneConflictTemplateReport.CurrentV826Audit();
        var statuses = CompletedThroughAppSeam();
        bool ready = report.ReadyForNextPilotN0 && generatedArtifactsTemplateCompleteOrObservationReady;
        statuses[CanonicalMigrationStage.nextPilotCandidate] = ready
            ? CanonicalMigrationMatrixStageStatus.complete
            : CanonicalMigrationMatrixStageStatus.blocked;
        statuses[CanonicalMigrationStage.canaryN0] = ready
            ? CanonicalMigrationMatrixStageStatus.activePilot
            : CanonicalMigrationMatrixStageStatus.blocked;
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.tombstoneConflict,
            stageStatuses: statuses,
            activePilot: ready,
            activePilotExplicit: ready,
            staticOnly: !ready,
            blockedForRealMigration: !ready,
            defaultCutoverEnabled: false,
            releaseDefaultEnabledCutover: false,
            runtimeSwitchEnabled: false,
            legacySuppressionAllowed: false,
            noProductionInjection: true,
            readPathLegacy: true,
            writeSideCutoverSucceeded: false,
            observationComplete: false,
            fallbackReady: true
        );
    }

    public static CanonicalMigrationDomainPolicy V813AudioUpload()
    {
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.audioUpload,
            stageStatuses: new Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus>
            {
                [CanonicalMigrationStage.projected] = CanonicalMigrationMatrixStageStatus.complete,
                [CanonicalMigrationStage.planned] = CanonicalMigrationMatrixStageStatus.complete,
                [CanonicalMigrationStage.noCommit] = CanonicalMigrationMatrixStageStatus.complete
            },
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true
        );
    }

    public static CanonicalMigrationDomainPolicy V813UIProjection()
    {
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.uiProjection,
            stageStatuses: new Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus>
            {
                [CanonicalMigrationStage.projected] = CanonicalMigrationMatrixStageStatus.staticOnly
            },
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true
        );
    }

    public static CanonicalMigrationDomainPolicy V813LegacyRetirement()
    {
        return new CanonicalMigrationDomainPolicy(
            domain: CanonicalMigrationDomain.legacyRetirement,
            stageStatuses: new Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus>(),
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true
        );
    }

    private static Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus> CompletedThroughAppSeam(
        CanonicalMigrationStage? activeStage = null)
    {
        return new Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus>
        {
            [CanonicalMigrationStage.projected] = activeStage == CanonicalMigrationStage.projected
                ? CanonicalMigrationMatrixStageStatus.activePilot : CanonicalMigrationMatrixStageStatus.complete,
            [CanonicalMigrationStage.planned] = activeStage == CanonicalMigrationStage.planned
                ? CanonicalMigrationMatrixStageStatus.activePilot : CanonicalMigrationMatrixStageStatus.complete,
            [CanonicalMigrationStage.noCommit] = activeStage == CanonicalMigrationStage.noCommit
                ? CanonicalMigrationMatrixStageStatus.activePilot : CanonicalMigrationMatrixStageStatus.complete,
            [CanonicalMigrationStage.realApplyPort] = activeStage == CanonicalMigrationStage.realApplyPort
                ? CanonicalMigrationMatrixStageStatus.activePilot : CanonicalMigrationMatrixStageStatus.complete,
            [CanonicalMigrationStage.commitExecutor] = activeStage == CanonicalMigrationStage.commitExecutor
                ? CanonicalMigrationMatrixStageStatus.activePilot : CanonicalMigrationMatrixStageStatus.complete,
            [CanonicalMigrationStage.appSeamDefaultOff] = activeStage == CanonicalMigrationStage.appSeamDefaultOff
                ? CanonicalMigrationMatrixStageStatus.activePilot : CanonicalMigrationMatrixStageStatus.complete
        };
    }
}

public sealed class CanonicalMigrationMatrixReport : IEquatable<CanonicalMigrationMatrixReport>
{
    public List<CanonicalMigrationDomainPolicy> Policies { get; }
    public CanonicalMigrationDomain? ActivePilotDomain { get; }
    public List<CanonicalMigrationDomainBlocker> Blockers { get; }
    public string DiagnosticsSummary { get; }
    public bool DiagnosticsRedacted { get; }

    public bool Allowed => Blockers.Count == 0;

    public CanonicalMigrationMatrixReport(
        List<CanonicalMigrationDomainPolicy> policies,
        CanonicalMigrationDomain? activePilotDomain,
        List<CanonicalMigrationDomainBlocker> blockers,
        string diagnosticsSummary,
        bool diagnosticsRedacted)
    {
        Policies = policies;
        ActivePilotDomain = activePilotDomain;
        Blockers = blockers;
        DiagnosticsSummary = diagnosticsSummary;
        DiagnosticsRedacted = diagnosticsRedacted;
    }

    public override bool Equals(object? obj) => obj is CanonicalMigrationMatrixReport other && Equals(other);
    public bool Equals(CanonicalMigrationMatrixReport? other) =>
        other is not null &&
        Policies.SequenceEqual(other.Policies) &&
        ActivePilotDomain == other.ActivePilotDomain &&
        Blockers.SequenceEqual(other.Blockers) &&
        DiagnosticsSummary == other.DiagnosticsSummary &&
        DiagnosticsRedacted == other.DiagnosticsRedacted;
    public override int GetHashCode() => HashCode.Combine(
        string.Join(",", Policies.Select(p => p.Domain)),
        ActivePilotDomain,
        string.Join(",", Blockers),
        DiagnosticsSummary, DiagnosticsRedacted);
    public static bool operator ==(CanonicalMigrationMatrixReport l, CanonicalMigrationMatrixReport r) => l.Equals(r);
    public static bool operator !=(CanonicalMigrationMatrixReport l, CanonicalMigrationMatrixReport r) => !l.Equals(r);
}

public sealed class CanonicalMigrationDomainMatrix : IEquatable<CanonicalMigrationDomainMatrix>
{
    public List<CanonicalMigrationDomainPolicy> Policies { get; }
    public bool LibraryMetadataPilotComplete { get; }
    public bool LibraryMetadataObservationCompleteOrRetirementCandidateReady { get; }
    public bool GeneratedArtifactsTemplateCompleteOrObservationReady { get; }

    public static readonly List<CanonicalMigrationStage> OrderedStages = new()
    {
        CanonicalMigrationStage.notStarted,
        CanonicalMigrationStage.projected,
        CanonicalMigrationStage.planned,
        CanonicalMigrationStage.noCommit,
        CanonicalMigrationStage.realApplyPort,
        CanonicalMigrationStage.commitExecutor,
        CanonicalMigrationStage.appSeamDefaultOff,
        CanonicalMigrationStage.nextPilotCandidate,
        CanonicalMigrationStage.canaryN0,
        CanonicalMigrationStage.canaryN1,
        CanonicalMigrationStage.expandedCanary,
        CanonicalMigrationStage.domainCutover,
        CanonicalMigrationStage.readSideParallel,
        CanonicalMigrationStage.readSideCutover,
        CanonicalMigrationStage.retirementCandidate,
        CanonicalMigrationStage.retired
    };

    public CanonicalMigrationDomainMatrix(
        List<CanonicalMigrationDomainPolicy> policies,
        bool libraryMetadataPilotComplete = false,
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady = false,
        bool generatedArtifactsTemplateCompleteOrObservationReady = false)
    {
        Policies = policies.OrderBy(p => p.Domain.ToString()).ToList();
        LibraryMetadataPilotComplete = libraryMetadataPilotComplete;
        LibraryMetadataObservationCompleteOrRetirementCandidateReady = libraryMetadataObservationCompleteOrRetirementCandidateReady;
        GeneratedArtifactsTemplateCompleteOrObservationReady = generatedArtifactsTemplateCompleteOrObservationReady;
    }

    public static CanonicalMigrationDomainMatrix DefaultV813()
    {
        return new CanonicalMigrationDomainMatrix(
            policies: new List<CanonicalMigrationDomainPolicy>
            {
                CanonicalMigrationDomainPolicy.V813RecordingMetadata(),
                CanonicalMigrationDomainPolicy.V813GeneratedArtifacts(),
                CanonicalMigrationDomainPolicy.V813LibraryMetadataPilot(),
                CanonicalMigrationDomainPolicy.V813TombstoneConflict(),
                CanonicalMigrationDomainPolicy.V813AudioUpload(),
                CanonicalMigrationDomainPolicy.V813UIProjection(),
                CanonicalMigrationDomainPolicy.V813LegacyRetirement()
            },
            libraryMetadataPilotComplete: false
        );
    }

    public static CanonicalMigrationDomainMatrix V821GeneratedArtifactsNextPilotCandidate(
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady,
        CanonicalGeneratedArtifactTemplateReport? templateReport = null)
    {
        var report = templateReport ?? CanonicalGeneratedArtifactTemplateReport.CurrentV821Audit();
        var baseMatrix = DefaultV813();
        var policies = baseMatrix.Policies.Select(policy =>
        {
            if (policy.Domain != CanonicalMigrationDomain.generatedArtifacts)
                return policy;
            return CanonicalMigrationDomainPolicy.V821GeneratedArtifactsNextPilotCandidate(report);
        }).ToList();
        return new CanonicalMigrationDomainMatrix(
            policies: policies,
            libraryMetadataPilotComplete: baseMatrix.LibraryMetadataPilotComplete,
            libraryMetadataObservationCompleteOrRetirementCandidateReady: libraryMetadataObservationCompleteOrRetirementCandidateReady,
            generatedArtifactsTemplateCompleteOrObservationReady: report.ReadyForNextPilotN0
        );
    }

    public static CanonicalMigrationDomainMatrix V822GeneratedArtifactsActivePilot(
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady,
        CanonicalGeneratedArtifactTemplateReport? templateReport = null)
    {
        var report = templateReport ?? CanonicalGeneratedArtifactTemplateReport.CurrentV821Audit();
        var baseMatrix = V821GeneratedArtifactsNextPilotCandidate(
            libraryMetadataObservationCompleteOrRetirementCandidateReady, report);
        var policies = baseMatrix.Policies.Select(policy =>
        {
            switch (policy.Domain)
            {
                case CanonicalMigrationDomain.generatedArtifacts:
                    return CanonicalMigrationDomainPolicy.V822GeneratedArtifactsActivePilot(report);
                case CanonicalMigrationDomain.libraryMetadata:
                    return new CanonicalMigrationDomainPolicy(
                        domain: CanonicalMigrationDomain.libraryMetadata,
                        stageStatuses: policy.StageStatuses,
                        activePilot: false,
                        activePilotExplicit: false,
                        staticOnly: true,
                        blockedForRealMigration: true,
                        observationComplete: libraryMetadataObservationCompleteOrRetirementCandidateReady,
                        legacySuppressionAllowed: false,
                        defaultCutoverEnabled: false,
                        releaseDefaultEnabledCutover: false,
                        runtimeSwitchEnabled: false,
                        readPathLegacy: true,
                        noProductionInjection: true
                    );
                default:
                    return new CanonicalMigrationDomainPolicy(
                        domain: policy.Domain,
                        stageStatuses: policy.StageStatuses,
                        activePilot: false,
                        activePilotExplicit: false,
                        staticOnly: true,
                        blockedForRealMigration: true,
                        legacySuppressionAllowed: false,
                        defaultCutoverEnabled: false,
                        releaseDefaultEnabledCutover: false,
                        runtimeSwitchEnabled: false,
                        readPathLegacy: true,
                        noProductionInjection: true
                    );
            }
        }).ToList();
        return new CanonicalMigrationDomainMatrix(
            policies: policies,
            libraryMetadataPilotComplete: false,
            libraryMetadataObservationCompleteOrRetirementCandidateReady: libraryMetadataObservationCompleteOrRetirementCandidateReady,
            generatedArtifactsTemplateCompleteOrObservationReady: report.ReadyForNextPilotN0
        );
    }

    public static CanonicalMigrationDomainMatrix V824GeneratedArtifactsStagedCanary(
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady,
        CanonicalGeneratedArtifactTemplateReport? templateReport = null)
    {
        var report = templateReport ?? CanonicalGeneratedArtifactTemplateReport.CurrentV821Audit();
        var baseMatrix = V822GeneratedArtifactsActivePilot(
            libraryMetadataObservationCompleteOrRetirementCandidateReady, report);
        var policies = baseMatrix.Policies.Select(policy =>
        {
            if (policy.Domain != CanonicalMigrationDomain.generatedArtifacts)
                return policy;
            return CanonicalMigrationDomainPolicy.V824GeneratedArtifactsStagedCanary(report);
        }).ToList();
        return new CanonicalMigrationDomainMatrix(
            policies: policies,
            libraryMetadataPilotComplete: false,
            libraryMetadataObservationCompleteOrRetirementCandidateReady: libraryMetadataObservationCompleteOrRetirementCandidateReady,
            generatedArtifactsTemplateCompleteOrObservationReady: report.ReadyForNextPilotN0
        );
    }

    public static CanonicalMigrationDomainMatrix V826TombstoneConflictNextPilotCandidate(
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady,
        bool generatedArtifactsTemplateCompleteOrObservationReady,
        CanonicalTombstoneConflictTemplateReport? templateReport = null)
    {
        var report = templateReport ?? CanonicalTombstoneConflictTemplateReport.CurrentV826Audit();
        var baseMatrix = generatedArtifactsTemplateCompleteOrObservationReady
            ? V824GeneratedArtifactsStagedCanary(libraryMetadataObservationCompleteOrRetirementCandidateReady)
            : V821GeneratedArtifactsNextPilotCandidate(libraryMetadataObservationCompleteOrRetirementCandidateReady);
        var policies = baseMatrix.Policies.Select(policy =>
        {
            if (policy.Domain != CanonicalMigrationDomain.tombstoneConflict)
                return policy;
            return CanonicalMigrationDomainPolicy.V826TombstoneConflictNextPilotCandidate(
                report, generatedArtifactsTemplateCompleteOrObservationReady);
        }).ToList();
        return new CanonicalMigrationDomainMatrix(
            policies: policies,
            libraryMetadataPilotComplete: false,
            libraryMetadataObservationCompleteOrRetirementCandidateReady: libraryMetadataObservationCompleteOrRetirementCandidateReady,
            generatedArtifactsTemplateCompleteOrObservationReady: generatedArtifactsTemplateCompleteOrObservationReady
        );
    }

    public static CanonicalMigrationDomainMatrix V827TombstoneConflictActivePilot(
        bool libraryMetadataObservationCompleteOrRetirementCandidateReady,
        bool generatedArtifactsTemplateCompleteOrObservationReady,
        CanonicalTombstoneConflictTemplateReport? templateReport = null)
    {
        var report = templateReport ?? CanonicalTombstoneConflictTemplateReport.CurrentV826Audit();
        var baseMatrix = V826TombstoneConflictNextPilotCandidate(
            libraryMetadataObservationCompleteOrRetirementCandidateReady,
            generatedArtifactsTemplateCompleteOrObservationReady, report);
        var policies = baseMatrix.Policies.Select(policy =>
        {
            switch (policy.Domain)
            {
                case CanonicalMigrationDomain.tombstoneConflict:
                    return CanonicalMigrationDomainPolicy.V827TombstoneConflictActivePilot(
                        report, generatedArtifactsTemplateCompleteOrObservationReady);
                case CanonicalMigrationDomain.generatedArtifacts:
                    return StaticV827Policy(policy,
                        acceptedTemplateSource: generatedArtifactsTemplateCompleteOrObservationReady,
                        observationComplete: generatedArtifactsTemplateCompleteOrObservationReady);
                case CanonicalMigrationDomain.libraryMetadata:
                    return StaticV827Policy(policy,
                        acceptedTemplateSource: libraryMetadataObservationCompleteOrRetirementCandidateReady,
                        observationComplete: libraryMetadataObservationCompleteOrRetirementCandidateReady);
                default:
                    return StaticV827Policy(policy);
            }
        }).ToList();
        return new CanonicalMigrationDomainMatrix(
            policies: policies,
            libraryMetadataPilotComplete: false,
            libraryMetadataObservationCompleteOrRetirementCandidateReady: libraryMetadataObservationCompleteOrRetirementCandidateReady,
            generatedArtifactsTemplateCompleteOrObservationReady: generatedArtifactsTemplateCompleteOrObservationReady
        );
    }

    public CanonicalMigrationDomainPolicy? PolicyFor(CanonicalMigrationDomain domain)
    {
        return Policies.FirstOrDefault(p => p.Domain == domain);
    }

    public CanonicalMigrationMatrixReport Validate()
    {
        var blockers = new List<CanonicalMigrationDomainBlocker>();
        var activePilotPolicies = Policies.Where(p => p.ActivePilot).ToList();
        if (activePilotPolicies.Count == 0)
            blockers.Add(CanonicalMigrationDomainBlocker.missingExplicitPilot);
        if (activePilotPolicies.Count > 1)
            blockers.Add(CanonicalMigrationDomainBlocker.multipleActivePilots);

        foreach (var policy in activePilotPolicies)
        {
            if (!policy.ActivePilotExplicit)
                blockers.Add(CanonicalMigrationDomainBlocker.missingExplicitPilot);
            if (policy.Domain != CanonicalMigrationDomain.libraryMetadata
                && !IsGeneratedArtifactsV822ActivePilotAllowed(policy)
                && !IsTombstoneConflictV827ActivePilotAllowed(policy))
                blockers.Add(CanonicalMigrationDomainBlocker.nonLibraryMetadataActivePilot);
        }

        foreach (var policy in Policies.Where(p => !p.ActivePilot))
        {
            if (!policy.StaticOnly && !policy.BlockedForRealMigration)
                blockers.Add(CanonicalMigrationDomainBlocker.nonPilotDomainNotStaticOnly);
        }

        foreach (var policy in Policies)
        {
            blockers.AddRange(StageBlockers(policy));
            if (policy.ReleaseDefaultEnabledCutover || policy.DefaultCutoverEnabled)
                blockers.Add(CanonicalMigrationDomainBlocker.releaseDefaultCutoverEnabled);
            if (policy.RuntimeSwitchEnabled)
                blockers.Add(CanonicalMigrationDomainBlocker.runtimeSwitchEnabled);
            if (policy.Domain == CanonicalMigrationDomain.audioUpload && policy.ActivePilot && !LibraryMetadataPilotComplete)
                blockers.Add(CanonicalMigrationDomainBlocker.audioUploadBlockedUntilLibraryMetadataPilotComplete);
            if (policy.Domain == CanonicalMigrationDomain.generatedArtifacts)
            {
                if (policy.ActivePilot && !IsGeneratedArtifactsV822ActivePilotAllowed(policy))
                    blockers.Add(CanonicalMigrationDomainBlocker.generatedArtifactsActivePilotDeniedV821);
                if (policy.ActivePilot && !policy.HasReached(CanonicalMigrationStage.nextPilotCandidate))
                    blockers.Add(CanonicalMigrationDomainBlocker.generatedArtifactsActivePilotBeforeNextPilotCandidate);
                if (policy.ActivePilot && !LibraryMetadataObservationCompleteOrRetirementCandidateReady)
                    blockers.Add(CanonicalMigrationDomainBlocker.generatedArtifactsActivePilotBeforeLibraryMetadataObservation);
                if (policy.HasReached(CanonicalMigrationStage.expandedCanary) && !policy.HasReached(CanonicalMigrationStage.canaryN1))
                    blockers.Add(CanonicalMigrationDomainBlocker.generatedArtifactsStagedCanaryBeforeN1);
                if (policy.HasReached(CanonicalMigrationStage.nextPilotCandidate) &&
                    !LibraryMetadataObservationCompleteOrRetirementCandidateReady)
                    blockers.Add(CanonicalMigrationDomainBlocker.generatedArtifactsNextPilotBeforeLibraryMetadataObservation);
            }
            if (policy.Domain == CanonicalMigrationDomain.audioUpload && policy.ActivePilot)
                blockers.Add(CanonicalMigrationDomainBlocker.audioUploadActivePilotDeniedV821);
            if (policy.Domain == CanonicalMigrationDomain.tombstoneConflict && policy.ActivePilot &&
                !IsTombstoneConflictV827ActivePilotAllowed(policy))
            {
                blockers.Add(CanonicalMigrationDomainBlocker.tombstoneConflictActivePilotDeniedV821);
                blockers.Add(CanonicalMigrationDomainBlocker.tombstoneConflictActivePilotDeniedV826);
            }
            if (policy.Domain == CanonicalMigrationDomain.tombstoneConflict &&
                policy.ActivePilot && !policy.HasReached(CanonicalMigrationStage.nextPilotCandidate))
                blockers.Add(CanonicalMigrationDomainBlocker.tombstoneConflictActivePilotBeforeNextPilotCandidate);
            if (policy.Domain == CanonicalMigrationDomain.tombstoneConflict &&
                policy.ActivePilot && !GeneratedArtifactsTemplateCompleteOrObservationReady)
                blockers.Add(CanonicalMigrationDomainBlocker.tombstoneConflictActivePilotBeforeGeneratedArtifactsObservation);
            if (policy.Domain == CanonicalMigrationDomain.tombstoneConflict &&
                policy.HasReached(CanonicalMigrationStage.nextPilotCandidate) &&
                !GeneratedArtifactsTemplateCompleteOrObservationReady)
                blockers.Add(CanonicalMigrationDomainBlocker.tombstoneConflictNextPilotBeforeGeneratedArtifactsObservation);
            if (policy.Domain == CanonicalMigrationDomain.legacyRetirement &&
                (policy.HasReached(CanonicalMigrationStage.retirementCandidate) || policy.HasReached(CanonicalMigrationStage.retired)) &&
                !policy.HasReached(CanonicalMigrationStage.readSideCutover))
                blockers.Add(CanonicalMigrationDomainBlocker.legacyRetirementBeforeReadSideCutover);
            if (policy.Domain == CanonicalMigrationDomain.legacyRetirement &&
                (policy.ActivePilot || policy.HasReached(CanonicalMigrationStage.retirementCandidate) || policy.HasReached(CanonicalMigrationStage.retired)))
                blockers.Add(CanonicalMigrationDomainBlocker.legacyRetirementEnabledDeniedV821);
        }

        var summary = DiagnosticsSummaryFor(policies: Policies, blockers: blockers);
        if (!IsRedacted(summary))
            blockers.Add(CanonicalMigrationDomainBlocker.diagnosticsNotRedacted);

        var uniqueBlockers = new HashSet<CanonicalMigrationDomainBlocker>(blockers)
            .OrderBy(b => b.ToString()).ToList();
        return new CanonicalMigrationMatrixReport(
            policies: Policies,
            activePilotDomain: activePilotPolicies.Count == 1 ? activePilotPolicies.First().Domain : null,
            blockers: uniqueBlockers,
            diagnosticsSummary: summary,
            diagnosticsRedacted: IsRedacted(summary)
        );
    }

    private List<CanonicalMigrationDomainBlocker> StageBlockers(CanonicalMigrationDomainPolicy policy)
    {
        var blockers = new List<CanonicalMigrationDomainBlocker>();
        for (int index = 0; index < OrderedStages.Count; index++)
        {
            var stage = OrderedStages[index];
            if (!policy.HasReached(stage)) continue;
            foreach (var prior in OrderedStages.Take(index).Where(s => s != CanonicalMigrationStage.notStarted))
            {
                if (!policy.HasReached(prior))
                {
                    blockers.Add(CanonicalMigrationDomainBlocker.stageSkipped);
                    break;
                }
            }
        }
        if (policy.HasReached(CanonicalMigrationStage.canaryN0) && !policy.HasReached(CanonicalMigrationStage.appSeamDefaultOff))
            blockers.Add(CanonicalMigrationDomainBlocker.canaryWithoutPreviousStageEvidence);
        if (policy.HasReached(CanonicalMigrationStage.canaryN1) && !policy.HasReached(CanonicalMigrationStage.canaryN0))
            blockers.Add(CanonicalMigrationDomainBlocker.canaryWithoutPreviousStageEvidence);
        if (policy.HasReached(CanonicalMigrationStage.expandedCanary) && !policy.HasReached(CanonicalMigrationStage.canaryN1))
            blockers.Add(CanonicalMigrationDomainBlocker.canaryWithoutPreviousStageEvidence);
        if (policy.HasReached(CanonicalMigrationStage.readSideCutover) && !policy.WriteSideCutoverSucceeded &&
            !policy.HasReached(CanonicalMigrationStage.domainCutover))
            blockers.Add(CanonicalMigrationDomainBlocker.readSideCutoverWithoutWriteSideCutover);
        if (policy.HasReached(CanonicalMigrationStage.retired))
        {
            if (!policy.HasReached(CanonicalMigrationStage.readSideCutover))
                blockers.Add(CanonicalMigrationDomainBlocker.retiredWithoutReadSideCutover);
            if (!policy.ObservationComplete)
                blockers.Add(CanonicalMigrationDomainBlocker.retiredWithoutObservation);
            if (!policy.FallbackReady)
                blockers.Add(CanonicalMigrationDomainBlocker.retiredWithoutFallbackReadiness);
        }
        return blockers;
    }

    private static string DiagnosticsSummaryFor(
        List<CanonicalMigrationDomainPolicy> policies,
        List<CanonicalMigrationDomainBlocker> blockers)
    {
        var domainSummary = string.Join("|", policies.Select(p =>
            $"{p.Domain}:active={p.ActivePilot}:static={p.StaticOnly}:runtimeSwitch={p.RuntimeSwitchEnabled}"));
        var blockerSummary = string.Join("+",
            new HashSet<CanonicalMigrationDomainBlocker>(blockers).OrderBy(b => b.ToString()).Select(b => b.ToString()));
        return $"v8.13,matrixDomains={policies.Count},{domainSummary},blockers={blockerSummary}";
    }

    public static bool IsRedacted(string text)
    {
        return CanonicalProductionRedaction.ContainsSensitivePathSignal(text) == false
            && !text.Contains("-----BEGIN")
            && !text.Contains("sharedSecret")
            && !text.Contains("apiKey");
    }

    private bool IsGeneratedArtifactsV822ActivePilotAllowed(CanonicalMigrationDomainPolicy policy)
    {
        return policy.Domain == CanonicalMigrationDomain.generatedArtifacts
            && policy.ActivePilot
            && policy.ActivePilotExplicit
            && policy.HasReached(CanonicalMigrationStage.nextPilotCandidate)
            && policy.HasReached(CanonicalMigrationStage.canaryN0)
            && LibraryMetadataObservationCompleteOrRetirementCandidateReady
            && !policy.DefaultCutoverEnabled
            && !policy.ReleaseDefaultEnabledCutover
            && !policy.RuntimeSwitchEnabled
            && policy.ReadPathLegacy
            && policy.NoProductionInjection
            && !policy.LegacySuppressionAllowed;
    }

    private bool IsTombstoneConflictV827ActivePilotAllowed(CanonicalMigrationDomainPolicy policy)
    {
        return policy.Domain == CanonicalMigrationDomain.tombstoneConflict
            && policy.ActivePilot
            && policy.ActivePilotExplicit
            && policy.HasReached(CanonicalMigrationStage.nextPilotCandidate)
            && policy.HasReached(CanonicalMigrationStage.canaryN0)
            && !policy.HasReached(CanonicalMigrationStage.canaryN1)
            && GeneratedArtifactsTemplateCompleteOrObservationReady
            && LibraryMetadataObservationCompleteOrRetirementCandidateReady
            && !policy.DefaultCutoverEnabled
            && !policy.ReleaseDefaultEnabledCutover
            && !policy.RuntimeSwitchEnabled
            && policy.ReadPathLegacy
            && policy.NoProductionInjection
            && !policy.LegacySuppressionAllowed;
    }

    private static CanonicalMigrationDomainPolicy StaticV827Policy(
        CanonicalMigrationDomainPolicy policy,
        bool acceptedTemplateSource = false,
        bool observationComplete = false)
    {
        var stageStatuses = new Dictionary<CanonicalMigrationStage, CanonicalMigrationMatrixStageStatus>(policy.StageStatuses);
        foreach (var key in stageStatuses.Keys.ToList())
        {
            if (stageStatuses[key] == CanonicalMigrationMatrixStageStatus.activePilot)
                stageStatuses[key] = CanonicalMigrationMatrixStageStatus.complete;
        }
        if (acceptedTemplateSource)
            stageStatuses[CanonicalMigrationStage.nextPilotCandidate] = CanonicalMigrationMatrixStageStatus.complete;
        stageStatuses.Remove(CanonicalMigrationStage.canaryN0);
        stageStatuses.Remove(CanonicalMigrationStage.canaryN1);
        stageStatuses.Remove(CanonicalMigrationStage.expandedCanary);
        stageStatuses.Remove(CanonicalMigrationStage.domainCutover);
        stageStatuses.Remove(CanonicalMigrationStage.readSideCutover);
        stageStatuses.Remove(CanonicalMigrationStage.retired);

        return new CanonicalMigrationDomainPolicy(
            domain: policy.Domain,
            stageStatuses: stageStatuses,
            activePilot: false,
            activePilotExplicit: false,
            staticOnly: true,
            blockedForRealMigration: true,
            defaultCutoverEnabled: false,
            releaseDefaultEnabledCutover: false,
            runtimeSwitchEnabled: false,
            legacySuppressionAllowed: false,
            noProductionInjection: true,
            readPathLegacy: true,
            writeSideCutoverSucceeded: false,
            observationComplete: observationComplete,
            fallbackReady: true
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalMigrationDomainMatrix other && Equals(other);
    public bool Equals(CanonicalMigrationDomainMatrix? other) =>
        other is not null &&
        Policies.SequenceEqual(other.Policies) &&
        LibraryMetadataPilotComplete == other.LibraryMetadataPilotComplete &&
        LibraryMetadataObservationCompleteOrRetirementCandidateReady == other.LibraryMetadataObservationCompleteOrRetirementCandidateReady &&
        GeneratedArtifactsTemplateCompleteOrObservationReady == other.GeneratedArtifactsTemplateCompleteOrObservationReady;
    public override int GetHashCode() => HashCode.Combine(
        string.Join(",", Policies.Select(p => p.Domain)),
        LibraryMetadataPilotComplete,
        LibraryMetadataObservationCompleteOrRetirementCandidateReady,
        GeneratedArtifactsTemplateCompleteOrObservationReady);
    public static bool operator ==(CanonicalMigrationDomainMatrix l, CanonicalMigrationDomainMatrix r) => l.Equals(r);
    public static bool operator !=(CanonicalMigrationDomainMatrix l, CanonicalMigrationDomainMatrix r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationConfigViolation
{
    multipleActiveCanaryDomains,
    activeDomainNotLibraryMetadata,
    audioUploadActiveBeforePilotComplete,
    tombstoneConflictActiveBeforePilotComplete,
    generatedArtifactsActiveBeforePilotComplete,
    generatedArtifactsNextPilotBeforeLibraryMetadataObservation,
    releaseDefaultEnabledCutover,
    runtimeSwitchEnabled,
    legacyRetirementBeforeReadSideCutover
}

public sealed class CanonicalMigrationConfigValidationResult : IEquatable<CanonicalMigrationConfigValidationResult>
{
    public List<CanonicalMigrationConfigViolation> Violations { get; }
    public string DiagnosticsSummary { get; }

    public bool Valid => Violations.Count == 0;

    public CanonicalMigrationConfigValidationResult(
        List<CanonicalMigrationConfigViolation> violations,
        string diagnosticsSummary)
    {
        Violations = violations;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalMigrationConfigValidationResult other && Equals(other);
    public bool Equals(CanonicalMigrationConfigValidationResult? other) =>
        other is not null &&
        Violations.SequenceEqual(other.Violations) &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(
        string.Join(",", Violations), DiagnosticsSummary);
    public static bool operator ==(CanonicalMigrationConfigValidationResult l, CanonicalMigrationConfigValidationResult r) => l.Equals(r);
    public static bool operator !=(CanonicalMigrationConfigValidationResult l, CanonicalMigrationConfigValidationResult r) => !l.Equals(r);
}

public sealed class CanonicalMigrationGlobalConfigValidator
{
    public CanonicalMigrationConfigValidationResult Validate(CanonicalMigrationDomainMatrix matrix)
    {
        var violations = new List<CanonicalMigrationConfigViolation>();
        var activeDomains = matrix.Policies.Where(p => p.HasActiveCanaryOrCutover).ToList();
        if (activeDomains.Count > 1)
            violations.Add(CanonicalMigrationConfigViolation.multipleActiveCanaryDomains);

        foreach (var policy in activeDomains)
        {
            if (policy.Domain != CanonicalMigrationDomain.libraryMetadata
                && !GeneratedArtifactsV822ActivePilotAllowed(policy, matrix)
                && !TombstoneConflictV827ActivePilotAllowed(policy, matrix))
                violations.Add(CanonicalMigrationConfigViolation.activeDomainNotLibraryMetadata);
        }

        if (matrix.PolicyFor(CanonicalMigrationDomain.audioUpload)?.HasActiveCanaryOrCutover == true &&
            !matrix.LibraryMetadataPilotComplete)
            violations.Add(CanonicalMigrationConfigViolation.audioUploadActiveBeforePilotComplete);

        var tombstone = matrix.PolicyFor(CanonicalMigrationDomain.tombstoneConflict);
        if (tombstone != null && tombstone.HasActiveCanaryOrCutover &&
            !TombstoneConflictV827ActivePilotAllowed(tombstone, matrix) &&
            !matrix.LibraryMetadataPilotComplete)
            violations.Add(CanonicalMigrationConfigViolation.tombstoneConflictActiveBeforePilotComplete);

        var generated = matrix.PolicyFor(CanonicalMigrationDomain.generatedArtifacts);
        if (generated != null && generated.HasActiveCanaryOrCutover &&
            !generated.StaticOnly &&
            !GeneratedArtifactsV822ActivePilotAllowed(generated, matrix))
            violations.Add(CanonicalMigrationConfigViolation.generatedArtifactsActiveBeforePilotComplete);

        if (generated != null && generated.HasReached(CanonicalMigrationStage.nextPilotCandidate) &&
            !matrix.LibraryMetadataObservationCompleteOrRetirementCandidateReady)
            violations.Add(CanonicalMigrationConfigViolation.generatedArtifactsNextPilotBeforeLibraryMetadataObservation);

        if (matrix.Policies.Any(p => p.ReleaseDefaultEnabledCutover || p.DefaultCutoverEnabled))
            violations.Add(CanonicalMigrationConfigViolation.releaseDefaultEnabledCutover);

        if (matrix.Policies.Any(p => p.RuntimeSwitchEnabled))
            violations.Add(CanonicalMigrationConfigViolation.runtimeSwitchEnabled);

        var legacy = matrix.PolicyFor(CanonicalMigrationDomain.legacyRetirement);
        if (legacy != null &&
            (legacy.HasReached(CanonicalMigrationStage.retirementCandidate) || legacy.HasReached(CanonicalMigrationStage.retired)) &&
            !legacy.HasReached(CanonicalMigrationStage.readSideCutover))
            violations.Add(CanonicalMigrationConfigViolation.legacyRetirementBeforeReadSideCutover);

        var uniqueViolations = new HashSet<CanonicalMigrationConfigViolation>(violations)
            .OrderBy(v => v.ToString()).ToList();
        return new CanonicalMigrationConfigValidationResult(
            violations: uniqueViolations,
            diagnosticsSummary: $"v8.13,globalConfig,violations={string.Join("+", uniqueViolations.Select(v => v.ToString()))}"
        );
    }

    private static bool GeneratedArtifactsV822ActivePilotAllowed(
        CanonicalMigrationDomainPolicy policy, CanonicalMigrationDomainMatrix matrix)
    {
        return policy.Domain == CanonicalMigrationDomain.generatedArtifacts
            && policy.ActivePilot
            && policy.ActivePilotExplicit
            && policy.HasReached(CanonicalMigrationStage.nextPilotCandidate)
            && policy.HasReached(CanonicalMigrationStage.canaryN0)
            && matrix.LibraryMetadataObservationCompleteOrRetirementCandidateReady
            && !policy.DefaultCutoverEnabled
            && !policy.ReleaseDefaultEnabledCutover
            && !policy.RuntimeSwitchEnabled
            && policy.ReadPathLegacy
            && policy.NoProductionInjection
            && !policy.LegacySuppressionAllowed;
    }

    private static bool TombstoneConflictV827ActivePilotAllowed(
        CanonicalMigrationDomainPolicy policy, CanonicalMigrationDomainMatrix matrix)
    {
        return policy.Domain == CanonicalMigrationDomain.tombstoneConflict
            && policy.ActivePilot
            && policy.ActivePilotExplicit
            && policy.HasReached(CanonicalMigrationStage.nextPilotCandidate)
            && policy.HasReached(CanonicalMigrationStage.canaryN0)
            && !policy.HasReached(CanonicalMigrationStage.canaryN1)
            && matrix.GeneratedArtifactsTemplateCompleteOrObservationReady
            && matrix.LibraryMetadataObservationCompleteOrRetirementCandidateReady
            && !policy.DefaultCutoverEnabled
            && !policy.ReleaseDefaultEnabledCutover
            && !policy.RuntimeSwitchEnabled
            && policy.ReadPathLegacy
            && policy.NoProductionInjection
            && !policy.LegacySuppressionAllowed;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataPilotReadiness
{
    readyForN0,
    readyForN1,
    missingNoCommit,
    missingRealApplyPort,
    missingCommitExecutor,
    missingAppSeam,
    missingReadSideParallel,
    missingTests,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataPilotBlocker
{
    missingCanonicalProjection,
    missingPlanner,
    missingApplyPlanBridge,
    missingNoCommit,
    missingRealApplyPort,
    missingCommitExecutor,
    missingAppSeam,
    missingCanaryPolicy,
    missingRollbackPlan,
    missingFailureInjection,
    missingLegacyFallback,
    duplicateSuppressionBeforeSuccess,
    missingReadSideParallel,
    resourceMoveGuardMissing,
    physicalDeleteGuardMissing,
    missingTests,
    missingDocs
}

public static class CanonicalLibraryMetadataPilotBlockerExtensions
{
    public static bool BlocksN0(this CanonicalLibraryMetadataPilotBlocker blocker)
    {
        return blocker switch
        {
            CanonicalLibraryMetadataPilotBlocker.missingReadSideParallel => false,
            CanonicalLibraryMetadataPilotBlocker.missingDocs => false,
            _ => true
        };
    }
}

public sealed class CanonicalLibraryMetadataPilotReport : IEquatable<CanonicalLibraryMetadataPilotReport>
{
    public CanonicalLibraryMetadataPilotReadiness Readiness { get; }
    public List<CanonicalLibraryMetadataPilotBlocker> Blockers { get; }
    public bool ReadyForN0 { get; }
    public bool ReadyForN1 { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalLibraryMetadataPilotReport(
        CanonicalLibraryMetadataPilotReadiness readiness,
        List<CanonicalLibraryMetadataPilotBlocker> blockers,
        bool readyForN0,
        bool readyForN1,
        string diagnosticsSummary)
    {
        Readiness = readiness;
        Blockers = blockers;
        ReadyForN0 = readyForN0;
        ReadyForN1 = readyForN1;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public static CanonicalLibraryMetadataPilotReport Audit(
        bool canonicalProjection,
        bool planner,
        bool applyPlanBridge,
        bool noCommitExecutor,
        bool realApplyPort,
        bool commitExecutor,
        bool appSeamDefaultOff,
        bool canaryPolicy,
        bool rollbackPlan,
        bool failureInjection,
        bool legacyFallback,
        bool duplicateSuppressionAfterSuccessOnly,
        bool readSideParallelProjection,
        bool noResourceMoveGuard,
        bool noPhysicalDeleteGuard,
        bool tests,
        bool docs)
    {
        var blockers = new List<CanonicalLibraryMetadataPilotBlocker>();
        if (!canonicalProjection) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingCanonicalProjection);
        if (!planner) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingPlanner);
        if (!applyPlanBridge) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingApplyPlanBridge);
        if (!noCommitExecutor) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingNoCommit);
        if (!realApplyPort) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingRealApplyPort);
        if (!commitExecutor) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingCommitExecutor);
        if (!appSeamDefaultOff) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingAppSeam);
        if (!canaryPolicy) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingCanaryPolicy);
        if (!rollbackPlan) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingRollbackPlan);
        if (!failureInjection) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingFailureInjection);
        if (!legacyFallback) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingLegacyFallback);
        if (!duplicateSuppressionAfterSuccessOnly) blockers.Add(CanonicalLibraryMetadataPilotBlocker.duplicateSuppressionBeforeSuccess);
        if (!readSideParallelProjection) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingReadSideParallel);
        if (!noResourceMoveGuard) blockers.Add(CanonicalLibraryMetadataPilotBlocker.resourceMoveGuardMissing);
        if (!noPhysicalDeleteGuard) blockers.Add(CanonicalLibraryMetadataPilotBlocker.physicalDeleteGuardMissing);
        if (!tests) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingTests);
        if (!docs) blockers.Add(CanonicalLibraryMetadataPilotBlocker.missingDocs);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataPilotBlocker>(blockers)
            .OrderBy(b => b.ToString()).ToList();
        bool n0Ready = uniqueBlockers.All(b => !b.BlocksN0());
        bool n1Ready = uniqueBlockers.Count == 0;

        CanonicalLibraryMetadataPilotReadiness readiness;
        if (uniqueBlockers.Contains(CanonicalLibraryMetadataPilotBlocker.missingNoCommit))
            readiness = CanonicalLibraryMetadataPilotReadiness.missingNoCommit;
        else if (uniqueBlockers.Contains(CanonicalLibraryMetadataPilotBlocker.missingRealApplyPort))
            readiness = CanonicalLibraryMetadataPilotReadiness.missingRealApplyPort;
        else if (uniqueBlockers.Contains(CanonicalLibraryMetadataPilotBlocker.missingCommitExecutor))
            readiness = CanonicalLibraryMetadataPilotReadiness.missingCommitExecutor;
        else if (uniqueBlockers.Contains(CanonicalLibraryMetadataPilotBlocker.missingAppSeam))
            readiness = CanonicalLibraryMetadataPilotReadiness.missingAppSeam;
        else if (uniqueBlockers.Contains(CanonicalLibraryMetadataPilotBlocker.missingReadSideParallel))
            readiness = CanonicalLibraryMetadataPilotReadiness.missingReadSideParallel;
        else if (uniqueBlockers.Contains(CanonicalLibraryMetadataPilotBlocker.missingTests))
            readiness = CanonicalLibraryMetadataPilotReadiness.missingTests;
        else if (n1Ready)
            readiness = CanonicalLibraryMetadataPilotReadiness.readyForN1;
        else if (n0Ready)
            readiness = CanonicalLibraryMetadataPilotReadiness.readyForN0;
        else
            readiness = CanonicalLibraryMetadataPilotReadiness.blocked;

        return new CanonicalLibraryMetadataPilotReport(
            readiness: readiness,
            blockers: uniqueBlockers,
            readyForN0: n0Ready,
            readyForN1: n1Ready,
            diagnosticsSummary: $"domain=libraryMetadata,readiness={readiness},blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))}"
        );
    }

    public static CanonicalLibraryMetadataPilotReport CurrentV813Audit(bool readSideParallelProjection = true)
    {
        return Audit(
            canonicalProjection: true,
            planner: true,
            applyPlanBridge: true,
            noCommitExecutor: true,
            realApplyPort: true,
            commitExecutor: true,
            appSeamDefaultOff: true,
            canaryPolicy: true,
            rollbackPlan: true,
            failureInjection: true,
            legacyFallback: true,
            duplicateSuppressionAfterSuccessOnly: true,
            readSideParallelProjection: readSideParallelProjection,
            noResourceMoveGuard: true,
            noPhysicalDeleteGuard: true,
            tests: true,
            docs: true
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataPilotReport other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataPilotReport? other) =>
        other is not null &&
        Readiness == other.Readiness &&
        Blockers.SequenceEqual(other.Blockers) &&
        ReadyForN0 == other.ReadyForN0 &&
        ReadyForN1 == other.ReadyForN1 &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(Readiness, string.Join(",", Blockers), ReadyForN0, ReadyForN1, DiagnosticsSummary);
    public static bool operator ==(CanonicalLibraryMetadataPilotReport l, CanonicalLibraryMetadataPilotReport r) => l.Equals(r);
    public static bool operator !=(CanonicalLibraryMetadataPilotReport l, CanonicalLibraryMetadataPilotReport r) => !l.Equals(r);
}

public sealed class CanonicalMigrationStaticDomainAudit : IEquatable<CanonicalMigrationStaticDomainAudit>
{
    public CanonicalMigrationDomain Domain { get; }
    public bool MachinePartsPresent { get; }
    public bool AppSeamPresent { get; }
    public bool DefaultOff { get; }
    public bool NoProductionInjection { get; }
    public bool ReadPathLegacy { get; }
    public bool TestsPresent { get; }
    public List<CanonicalMigrationDomainBlocker> Blockers { get; }
    public bool StaticReviewRecommended { get; }
    public bool RealMigrationBlocked { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalMigrationStaticDomainAudit(
        CanonicalMigrationDomain domain,
        bool machinePartsPresent,
        bool appSeamPresent,
        bool defaultOff,
        bool noProductionInjection,
        bool readPathLegacy,
        bool testsPresent,
        bool staticReviewRecommended = true,
        bool realMigrationBlocked = true)
    {
        var blockers = new List<CanonicalMigrationDomainBlocker>();
        if (!machinePartsPresent) blockers.Add(CanonicalMigrationDomainBlocker.missingMachineParts);
        if (!appSeamPresent) blockers.Add(CanonicalMigrationDomainBlocker.missingAppSeam);
        if (!defaultOff) blockers.Add(CanonicalMigrationDomainBlocker.cutoverNotDefaultOff);
        if (!noProductionInjection) blockers.Add(CanonicalMigrationDomainBlocker.productionInjectionPresent);
        if (!readPathLegacy) blockers.Add(CanonicalMigrationDomainBlocker.readPathNotLegacy);
        if (!testsPresent) blockers.Add(CanonicalMigrationDomainBlocker.testsMissing);

        Domain = domain;
        MachinePartsPresent = machinePartsPresent;
        AppSeamPresent = appSeamPresent;
        DefaultOff = defaultOff;
        NoProductionInjection = noProductionInjection;
        ReadPathLegacy = readPathLegacy;
        TestsPresent = testsPresent;
        Blockers = new HashSet<CanonicalMigrationDomainBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        StaticReviewRecommended = staticReviewRecommended;
        RealMigrationBlocked = realMigrationBlocked;
        DiagnosticsSummary = string.Join(",",
            $"domain={domain}",
            $"machinePartsPresent={machinePartsPresent}",
            $"appSeamPresent={appSeamPresent}",
            $"defaultOff={defaultOff}",
            $"readPathLegacy={readPathLegacy}",
            $"realMigrationBlocked={realMigrationBlocked}"
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalMigrationStaticDomainAudit other && Equals(other);
    public bool Equals(CanonicalMigrationStaticDomainAudit? other) =>
        other is not null &&
        Domain == other.Domain &&
        MachinePartsPresent == other.MachinePartsPresent &&
        AppSeamPresent == other.AppSeamPresent &&
        DefaultOff == other.DefaultOff &&
        NoProductionInjection == other.NoProductionInjection &&
        ReadPathLegacy == other.ReadPathLegacy &&
        TestsPresent == other.TestsPresent &&
        Blockers.SequenceEqual(other.Blockers) &&
        StaticReviewRecommended == other.StaticReviewRecommended &&
        RealMigrationBlocked == other.RealMigrationBlocked;
    public override int GetHashCode() => HashCode.Combine(Domain, MachinePartsPresent, AppSeamPresent,
        DefaultOff, NoProductionInjection, ReadPathLegacy, TestsPresent, StaticReviewRecommended, RealMigrationBlocked);
    public static bool operator ==(CanonicalMigrationStaticDomainAudit l, CanonicalMigrationStaticDomainAudit r) => l.Equals(r);
    public static bool operator !=(CanonicalMigrationStaticDomainAudit l, CanonicalMigrationStaticDomainAudit r) => !l.Equals(r);
}

public sealed class CanonicalOtherDomainsStaticAuditReport : IEquatable<CanonicalOtherDomainsStaticAuditReport>
{
    public List<CanonicalMigrationStaticDomainAudit> Audits { get; }

    public CanonicalOtherDomainsStaticAuditReport(List<CanonicalMigrationStaticDomainAudit> audits)
    {
        Audits = audits;
    }

    public static CanonicalOtherDomainsStaticAuditReport V813Default()
    {
        return new CanonicalOtherDomainsStaticAuditReport(new List<CanonicalMigrationStaticDomainAudit>
        {
            new(CanonicalMigrationDomain.recordingMetadata,
                machinePartsPresent: true, appSeamPresent: true, defaultOff: true,
                noProductionInjection: true, readPathLegacy: true, testsPresent: true),
            new(CanonicalMigrationDomain.generatedArtifacts,
                machinePartsPresent: true, appSeamPresent: true, defaultOff: true,
                noProductionInjection: true, readPathLegacy: true, testsPresent: true),
            new(CanonicalMigrationDomain.tombstoneConflict,
                machinePartsPresent: true, appSeamPresent: true, defaultOff: true,
                noProductionInjection: true, readPathLegacy: true, testsPresent: true),
            new(CanonicalMigrationDomain.audioUpload,
                machinePartsPresent: false, appSeamPresent: true, defaultOff: true,
                noProductionInjection: true, readPathLegacy: true, testsPresent: true)
        });
    }

    public CanonicalMigrationStaticDomainAudit? AuditFor(CanonicalMigrationDomain domain)
    {
        return Audits.FirstOrDefault(a => a.Domain == domain);
    }

    public string DiagnosticsSummary => string.Join("|", Audits.Select(a => a.DiagnosticsSummary));

    public override bool Equals(object? obj) => obj is CanonicalOtherDomainsStaticAuditReport other && Equals(other);
    public bool Equals(CanonicalOtherDomainsStaticAuditReport? other) =>
        other is not null && Audits.SequenceEqual(other.Audits);
    public override int GetHashCode() => HashCode.Combine(string.Join(",", Audits.Select(a => a.Domain)));
    public static bool operator ==(CanonicalOtherDomainsStaticAuditReport l, CanonicalOtherDomainsStaticAuditReport r) => l.Equals(r);
    public static bool operator !=(CanonicalOtherDomainsStaticAuditReport l, CanonicalOtherDomainsStaticAuditReport r) => !l.Equals(r);
}
