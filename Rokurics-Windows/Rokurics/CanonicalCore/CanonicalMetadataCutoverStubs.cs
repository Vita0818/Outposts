using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── Enums referenced from CanonicalRecordingMetadataCutover and CanonicalLibraryMetadataCutover ───

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataApplyPortMode
{
    disabled,
    dryRun,
    testRootBound,
    productionRootBound
}

public static class CanonicalRecordingMetadataApplyPortModeExtensions
{
    public static bool IsNonDryRunRootBound(this CanonicalRecordingMetadataApplyPortMode mode) =>
        mode == CanonicalRecordingMetadataApplyPortMode.testRootBound ||
        mode == CanonicalRecordingMetadataApplyPortMode.productionRootBound;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalActionSource
{
    local,
    peer,
    planner
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationDomain
{
    recordingMetadata,
    recordingAudio,
    generatedArtifacts,
    folders,
    studyItems,
    standaloneNotes,
    tombstones,
    conflicts,
    libraryMetadata,
    audioUpload,
    tombstoneConflict,
    apply,
    fileRuntime,
    transportRuntime,
    uploadRuntime,
    objectProjection,
    inventory,
    uiIntegration
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationStage
{
    projected,
    planned,
    noCommit,
    realApplyPort,
    commitExecutor,
    appSeamDefaultOff,
    nextPilotCandidate,
    canaryN0,
    canaryN1,
    expandedCanary,
    domainCutover,
    readSideParallel,
    readSideCutover,
    retirementCandidate
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationStageStatus
{
    complete,
    writeSideCanaryObserved,
    readSideObserved,
    observationComplete,
    retirementBlocked,
    retirementCandidateReady
}

public sealed class CanonicalMigrationDomainPolicy : IEquatable<CanonicalMigrationDomainPolicy>
{
    public CanonicalMigrationDomain Domain { get; set; }
    public Dictionary<CanonicalMigrationStage, CanonicalMigrationStageStatus> StageStatuses { get; set; }
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
    public bool HasActiveCanaryOrCutover { get; set; }

    public CanonicalMigrationDomainPolicy(
        CanonicalMigrationDomain domain = CanonicalMigrationDomain.libraryMetadata,
        Dictionary<CanonicalMigrationStage, CanonicalMigrationStageStatus>? stageStatuses = null,
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
        bool fallbackReady = false,
        bool hasActiveCanaryOrCutover = false)
    {
        Domain = domain;
        StageStatuses = stageStatuses ?? new Dictionary<CanonicalMigrationStage, CanonicalMigrationStageStatus>();
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
        HasActiveCanaryOrCutover = hasActiveCanaryOrCutover;
    }

    public override bool Equals(object? obj) => obj is CanonicalMigrationDomainPolicy other && Equals(other);
    public bool Equals(CanonicalMigrationDomainPolicy? other) =>
        other is not null && Domain == other.Domain && ActivePilot == other.ActivePilot &&
        StaticOnly == other.StaticOnly && BlockedForRealMigration == other.BlockedForRealMigration;
    public override int GetHashCode() => HashCode.Combine(Domain, ActivePilot, StaticOnly, BlockedForRealMigration);
    public static bool operator ==(CanonicalMigrationDomainPolicy left, CanonicalMigrationDomainPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalMigrationDomainPolicy left, CanonicalMigrationDomainPolicy right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationDomainMatrixBlocker
{
    multipleActivePilots,
    nonLibraryMetadataActivePilot,
    defaultCutoverEnabled,
    runtimeSwitchEnabled,
    allEligibleEnabled,
    noProductionInjectionMissing
}

public sealed class CanonicalMigrationDomainMatrixValidationResult : IEquatable<CanonicalMigrationDomainMatrixValidationResult>
{
    public bool Allowed { get; set; }
    public CanonicalMigrationDomain? ActivePilotDomain { get; set; }
    public List<CanonicalMigrationDomainMatrixBlocker> Blockers { get; set; }
    public string DiagnosticsSummary { get; set; }

    public CanonicalMigrationDomainMatrixValidationResult(
        bool allowed = true,
        CanonicalMigrationDomain? activePilotDomain = null,
        List<CanonicalMigrationDomainMatrixBlocker>? blockers = null,
        string diagnosticsSummary = "")
    {
        Allowed = allowed;
        ActivePilotDomain = activePilotDomain;
        Blockers = blockers ?? new List<CanonicalMigrationDomainMatrixBlocker>();
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalMigrationDomainMatrixValidationResult other && Equals(other);
    public bool Equals(CanonicalMigrationDomainMatrixValidationResult? other) =>
        other is not null && Allowed == other.Allowed && ActivePilotDomain == other.ActivePilotDomain &&
        Blockers.SequenceEqual(other.Blockers) && DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(Allowed, ActivePilotDomain, Blockers.Count, DiagnosticsSummary);
    public static bool operator ==(CanonicalMigrationDomainMatrixValidationResult left, CanonicalMigrationDomainMatrixValidationResult right) => left.Equals(right);
    public static bool operator !=(CanonicalMigrationDomainMatrixValidationResult left, CanonicalMigrationDomainMatrixValidationResult right) => !left.Equals(right);
}

public sealed class CanonicalMigrationDomainMatrix : IEquatable<CanonicalMigrationDomainMatrix>
{
    public List<CanonicalMigrationDomainPolicy> Policies { get; set; }
    public bool LibraryMetadataPilotComplete { get; set; }

    public CanonicalMigrationDomainMatrix(
        List<CanonicalMigrationDomainPolicy>? policies = null,
        bool libraryMetadataPilotComplete = false)
    {
        Policies = policies ?? new List<CanonicalMigrationDomainPolicy>();
        LibraryMetadataPilotComplete = libraryMetadataPilotComplete;
    }

    public CanonicalMigrationDomainPolicy? PolicyFor(CanonicalMigrationDomain domain) =>
        Policies.FirstOrDefault(p => p.Domain == domain);

    public CanonicalMigrationDomainMatrixValidationResult Validate()
    {
        var activePolicies = Policies.Where(p => p.ActivePilot).ToList();
        var blockers = new List<CanonicalMigrationDomainMatrixBlocker>();
        CanonicalMigrationDomain? activePilotDomain = activePolicies.Count == 1 ? activePolicies[0].Domain : null;

        if (activePolicies.Count > 1) blockers.Add(CanonicalMigrationDomainMatrixBlocker.multipleActivePilots);
        if (activePolicies.Any(p => p.Domain != CanonicalMigrationDomain.libraryMetadata)) blockers.Add(CanonicalMigrationDomainMatrixBlocker.nonLibraryMetadataActivePilot);
        if (Policies.Any(p => p.DefaultCutoverEnabled)) blockers.Add(CanonicalMigrationDomainMatrixBlocker.defaultCutoverEnabled);
        if (Policies.Any(p => p.RuntimeSwitchEnabled)) blockers.Add(CanonicalMigrationDomainMatrixBlocker.runtimeSwitchEnabled);

        return new CanonicalMigrationDomainMatrixValidationResult(
            blockers.Count == 0, activePilotDomain, blockers,
            string.Join(",", $"activePilot={activePilotDomain?.ToString() ?? "none"}", $"blockers={string.Join("|", blockers.Select(b => b.ToString()))}"));
    }

    public static CanonicalMigrationDomainMatrix DefaultV813() => new();

    public override bool Equals(object? obj) => obj is CanonicalMigrationDomainMatrix other && Equals(other);
    public bool Equals(CanonicalMigrationDomainMatrix? other) =>
        other is not null && Policies.SequenceEqual(other.Policies) &&
        LibraryMetadataPilotComplete == other.LibraryMetadataPilotComplete;
    public override int GetHashCode() => HashCode.Combine(Policies.Count, LibraryMetadataPilotComplete);
    public static bool operator ==(CanonicalMigrationDomainMatrix left, CanonicalMigrationDomainMatrix right) => left.Equals(right);
    public static bool operator !=(CanonicalMigrationDomainMatrix left, CanonicalMigrationDomainMatrix right) => !left.Equals(right);
}

// ─── Production-related stubs ───

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionSideEffectKind
{
    fileRead,
    fileWrite,
    metadataApply,
    conflictRecord,
    diagnosticsWrite,
    networkRequest,
    uploadSessionStart,
    uploadChunkSend,
    uploadFinalize,
    generatedArtifactApply,
    tombstoneMark
}

public sealed class CanonicalProductionSideEffect : IEquatable<CanonicalProductionSideEffect>
{
    public CanonicalProductionSideEffectKind Kind { get; set; }
    public CanonicalProductionDomain Domain { get; set; }
    public string? Description { get; set; }

    public CanonicalProductionSideEffect(
        CanonicalProductionSideEffectKind kind = CanonicalProductionSideEffectKind.metadataApply,
        CanonicalProductionDomain domain = CanonicalProductionDomain.folders,
        string? description = null)
    {
        Kind = kind;
        Domain = domain;
        Description = description;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionSideEffect other && Equals(other);
    public bool Equals(CanonicalProductionSideEffect? other) =>
        other is not null && Kind == other.Kind && Domain == other.Domain && Description == other.Description;
    public override int GetHashCode() => HashCode.Combine(Kind, Domain, Description);
    public static bool operator ==(CanonicalProductionSideEffect left, CanonicalProductionSideEffect right) => left.Equals(right);
    public static bool operator !=(CanonicalProductionSideEffect left, CanonicalProductionSideEffect right) => !left.Equals(right);
}

// ─── Forward-reference stubs used by the cutover files ───
// CanonicalNoCommitStagingEvidence, CanonicalNoCommitCleanupEvidence, CanonicalNoCommitEvidenceReport
// are now fully defined in CanonicalNoCommitV82.cs

public sealed class CanonicalRealDataShadowCopyResult : IEquatable<CanonicalRealDataShadowCopyResult>
{
    public bool Verified { get; set; }
    public string? Summary { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalRealDataShadowCopyResult other && Equals(other);
    public bool Equals(CanonicalRealDataShadowCopyResult? other) =>
        other is not null && Verified == other.Verified && Summary == other.Summary;
    public override int GetHashCode() => HashCode.Combine(Verified, Summary);
    public static bool operator ==(CanonicalRealDataShadowCopyResult left, CanonicalRealDataShadowCopyResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRealDataShadowCopyResult left, CanonicalRealDataShadowCopyResult right) => !left.Equals(right);
}

public sealed class CanonicalExecutionShadowReport : IEquatable<CanonicalExecutionShadowReport>
{
    public bool Verified { get; set; }
    public string? Summary { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalExecutionShadowReport other && Equals(other);
    public bool Equals(CanonicalExecutionShadowReport? other) =>
        other is not null && Verified == other.Verified && Summary == other.Summary;
    public override int GetHashCode() => HashCode.Combine(Verified, Summary);
    public static bool operator ==(CanonicalExecutionShadowReport left, CanonicalExecutionShadowReport right) => left.Equals(right);
    public static bool operator !=(CanonicalExecutionShadowReport left, CanonicalExecutionShadowReport right) => !left.Equals(right);
}

public sealed class CanonicalReadOnlyTransportProbeResult : IEquatable<CanonicalReadOnlyTransportProbeResult>
{
    public bool Passed { get; set; }
    public string? Summary { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalReadOnlyTransportProbeResult other && Equals(other);
    public bool Equals(CanonicalReadOnlyTransportProbeResult? other) =>
        other is not null && Passed == other.Passed && Summary == other.Summary;
    public override int GetHashCode() => HashCode.Combine(Passed, Summary);
    public static bool operator ==(CanonicalReadOnlyTransportProbeResult left, CanonicalReadOnlyTransportProbeResult right) => left.Equals(right);
    public static bool operator !=(CanonicalReadOnlyTransportProbeResult left, CanonicalReadOnlyTransportProbeResult right) => !left.Equals(right);
}

public sealed class CanonicalLegacyActionSnapshot : IEquatable<CanonicalLegacyActionSnapshot>
{
    public List<string> LegacyActionIDs { get; set; } = new();
    public static readonly CanonicalLegacyActionSnapshot Empty = new();

    public override bool Equals(object? obj) => obj is CanonicalLegacyActionSnapshot other && Equals(other);
    public bool Equals(CanonicalLegacyActionSnapshot? other) =>
        other is not null && LegacyActionIDs.SequenceEqual(other.LegacyActionIDs);
    public override int GetHashCode() => LegacyActionIDs.Count;
    public static bool operator ==(CanonicalLegacyActionSnapshot left, CanonicalLegacyActionSnapshot right) => left.Equals(right);
    public static bool operator !=(CanonicalLegacyActionSnapshot left, CanonicalLegacyActionSnapshot right) => !left.Equals(right);
}

// ─── Observation stubs ───

public sealed class CanonicalLibraryMetadataReadSourceResult : IEquatable<CanonicalLibraryMetadataReadSourceResult>
{
    public bool CanonicalCandidateBuilt { get; set; }
    public bool CanonicalReadServed { get; set; }
    public int FallbackCount { get; set; }
    public bool SyncOrUploadTriggered { get; set; }
    public bool ResourceMoved { get; set; }
    public bool ContentWritten { get; set; }
    public bool UiMutated { get; set; }
    public CanonicalLibraryMetadataDiffReport? DiffReport { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataReadSourceResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataReadSourceResult? other) =>
        other is not null && CanonicalCandidateBuilt == other.CanonicalCandidateBuilt &&
        CanonicalReadServed == other.CanonicalReadServed && FallbackCount == other.FallbackCount &&
        SyncOrUploadTriggered == other.SyncOrUploadTriggered && ResourceMoved == other.ResourceMoved &&
        ContentWritten == other.ContentWritten && UiMutated == other.UiMutated;
    public override int GetHashCode() =>
        HashCode.Combine(CanonicalCandidateBuilt, CanonicalReadServed, FallbackCount, SyncOrUploadTriggered,
            ResourceMoved, ContentWritten, UiMutated);
    public static bool operator ==(CanonicalLibraryMetadataReadSourceResult left, CanonicalLibraryMetadataReadSourceResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataReadSourceResult left, CanonicalLibraryMetadataReadSourceResult right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataDiffReport : IEquatable<CanonicalLibraryMetadataDiffReport>
{
    public bool Equivalent { get; set; }
    public int DivergenceCount { get; set; }
    public int UnsupportedObjectCount { get; set; }
    public int PathLeakRiskCount { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataDiffReport other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataDiffReport? other) =>
        other is not null && Equivalent == other.Equivalent && DivergenceCount == other.DivergenceCount &&
        UnsupportedObjectCount == other.UnsupportedObjectCount && PathLeakRiskCount == other.PathLeakRiskCount;
    public override int GetHashCode() =>
        HashCode.Combine(Equivalent, DivergenceCount, UnsupportedObjectCount, PathLeakRiskCount);
    public static bool operator ==(CanonicalLibraryMetadataDiffReport left, CanonicalLibraryMetadataDiffReport right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataDiffReport left, CanonicalLibraryMetadataDiffReport right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataReadSideCutoverResult : IEquatable<CanonicalLibraryMetadataReadSideCutoverResult>
{
    public CanonicalLibraryMetadataReadSideCutoverCandidate Candidate { get; set; } = new();
    public CanonicalLibraryMetadataDiffReport? DiffReport { get; set; }
    public bool LegacyReadFallbackAvailable { get; set; }
    public bool SyncOrUploadTriggered { get; set; }
    public bool UiMutated { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataReadSideCutoverResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataReadSideCutoverResult? other) =>
        other is not null && EqualityComparer<CanonicalLibraryMetadataReadSideCutoverCandidate>.Default.Equals(Candidate, other.Candidate) &&
        EqualityComparer<CanonicalLibraryMetadataDiffReport?>.Default.Equals(DiffReport, other.DiffReport) &&
        LegacyReadFallbackAvailable == other.LegacyReadFallbackAvailable &&
        SyncOrUploadTriggered == other.SyncOrUploadTriggered && UiMutated == other.UiMutated;
    public override int GetHashCode() =>
        HashCode.Combine(Candidate, DiffReport, LegacyReadFallbackAvailable, SyncOrUploadTriggered, UiMutated);
    public static bool operator ==(CanonicalLibraryMetadataReadSideCutoverResult left, CanonicalLibraryMetadataReadSideCutoverResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataReadSideCutoverResult left, CanonicalLibraryMetadataReadSideCutoverResult right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataReadSideCutoverCandidate : IEquatable<CanonicalLibraryMetadataReadSideCutoverCandidate>
{
    public bool Ready { get; set; }
    public string DiagnosticsSummary { get; set; } = "";

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataReadSideCutoverCandidate other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataReadSideCutoverCandidate? other) =>
        other is not null && Ready == other.Ready && DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(Ready, DiagnosticsSummary);
    public static bool operator ==(CanonicalLibraryMetadataReadSideCutoverCandidate left, CanonicalLibraryMetadataReadSideCutoverCandidate right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataReadSideCutoverCandidate left, CanonicalLibraryMetadataReadSideCutoverCandidate right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataRetirementBlocker
{
    observationWindowIncomplete,
    manualAuditRequired,
    fallbackMissing,
    divergencePresent,
    unsupportedObject,
    rollbackFatal,
    otherDomainsAffected,
    unsafeSideEffect,
    pathLeakRisk,
    runtimeSwitchEnabled,
    defaultReadOrWriteCutoverEnabled
}

// ─── Canary runner stubs ───

public sealed class CanonicalLibraryMetadataCanaryResult : IEquatable<CanonicalLibraryMetadataCanaryResult>
{
    public CanonicalLibraryMetadataCanarySelectionResult Selection { get; set; } = new();
    public CanonicalLibraryMetadataCutoverResult? CutoverResult { get; set; }
    public bool Succeeded { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryResult? other) =>
        other is not null &&
        EqualityComparer<CanonicalLibraryMetadataCanarySelectionResult>.Default.Equals(Selection, other.Selection) &&
        EqualityComparer<CanonicalLibraryMetadataCutoverResult?>.Default.Equals(CutoverResult, other.CutoverResult) &&
        Succeeded == other.Succeeded;
    public override int GetHashCode() => HashCode.Combine(Selection, CutoverResult, Succeeded);
    public static bool operator ==(CanonicalLibraryMetadataCanaryResult left, CanonicalLibraryMetadataCanaryResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryResult left, CanonicalLibraryMetadataCanaryResult right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataCutoverResult : IEquatable<CanonicalLibraryMetadataCutoverResult>
{
    public List<CanonicalLibraryMetadataProductionCommitResult> Commits { get; set; } = new();
    public List<CanonicalLibraryMetadataRollbackExecutionResult> RollbackResults { get; set; } = new();
    public List<CanonicalLibraryMetadataCutoverDiagnostic> Diagnostics { get; set; } = new();
    public bool LegacyFallbackUsed { get; set; }
    public List<string> DuplicateLegacySuppressedActionIDs { get; set; } = new();
    public int CanaryAttemptedCount { get; set; }
    public bool CanarySucceeded { get; set; }
    public bool FatalBlocker { get; set; }
    public CanonicalLibraryMetadataReadSideParallelProjectionResult? ReadSideProjection { get; set; }
    public CanonicalLibraryMetadataCanarySelectionResult? CanarySelection { get; set; }
    public List<CanonicalLibraryMetadataCanaryCandidateSafety>? CandidateSafetyReports { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCutoverResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCutoverResult? other) =>
        other is not null && Commits.SequenceEqual(other.Commits) &&
        RollbackResults.SequenceEqual(other.RollbackResults) &&
        Diagnostics.SequenceEqual(other.Diagnostics) &&
        LegacyFallbackUsed == other.LegacyFallbackUsed &&
        DuplicateLegacySuppressedActionIDs.SequenceEqual(other.DuplicateLegacySuppressedActionIDs) &&
        CanaryAttemptedCount == other.CanaryAttemptedCount && CanarySucceeded == other.CanarySucceeded &&
        FatalBlocker == other.FatalBlocker &&
        EqualityComparer<CanonicalLibraryMetadataReadSideParallelProjectionResult?>.Default.Equals(ReadSideProjection, other.ReadSideProjection) &&
        EqualityComparer<CanonicalLibraryMetadataCanarySelectionResult?>.Default.Equals(CanarySelection, other.CanarySelection) &&
        (CandidateSafetyReports?.SequenceEqual(other.CandidateSafetyReports ?? new List<CanonicalLibraryMetadataCanaryCandidateSafety>()) ?? (other.CandidateSafetyReports?.Count == 0));
    public override int GetHashCode() =>
        HashCode.Combine(Commits.Count, RollbackResults.Count, Diagnostics.Count, LegacyFallbackUsed,
            DuplicateLegacySuppressedActionIDs.Count, CanaryAttemptedCount, CanarySucceeded, FatalBlocker,
            ReadSideProjection, CanarySelection, CandidateSafetyReports?.Count ?? 0);
    public static bool operator ==(CanonicalLibraryMetadataCutoverResult left, CanonicalLibraryMetadataCutoverResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCutoverResult left, CanonicalLibraryMetadataCutoverResult right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataN1CanaryRunner
{
    public async Task<CanonicalLibraryMetadataCanaryResult> Run(
        CanonicalLibraryMetadataCanaryConfiguration configuration,
        CanonicalLibraryMetadataCanaryPolicy policy,
        CanonicalCutoverToken? token,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        CanonicalMigrationDomainMatrix matrix,
        List<CanonicalLibraryMetadataCutoverCandidate> candidates,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        string? syncRunID,
        bool localSnapshotAvailable,
        bool peerSnapshotAvailable,
        ICanonicalLibraryMetadataCutoverExecutor? executor)
    {
        var result = new CanonicalLibraryMetadataCutoverResult();
        if (executor != null && candidates.Count > 0)
        {
            try
            {
                var commit = await executor.CommitLibraryMetadata(candidates[0]);
                result.Commits.Add(commit);
                if (!commit.Committed)
                {
                    var rollback = await executor.RollbackLibraryMetadata(candidates[0],
                        CanonicalLibraryMetadataCutoverFailure.applyFailureBeforeCommit);
                    result.RollbackResults.Add(rollback);
                    result.FatalBlocker = !rollback.Succeeded;
                }
            }
            catch
            {
                result.FatalBlocker = true;
            }
            result.CanaryAttemptedCount = 1;
            result.CanarySucceeded = result.Commits.All(c => c.Committed && c.PreconditionVerified && c.PostconditionVerified);
            result.LegacyFallbackUsed = !result.CanarySucceeded;
        }

        return new CanonicalLibraryMetadataCanaryResult
        {
            Selection = new CanonicalLibraryMetadataCanarySelectionResult(
                selectedCandidates: candidates.Select(c => new CanonicalLibraryMetadataCanaryCandidate(c)).ToList(),
                evaluatedCandidateCount: candidates.Count),
            CutoverResult = result,
            Succeeded = result.CanarySucceeded && !result.FatalBlocker
        };
    }
}
