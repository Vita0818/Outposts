using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataProductionCanaryMode
{
    disabled,
    diagnosticsOnly,
    canaryN1Armed,
    canaryN1Execute,
    blocked
}

public static class CanonicalLibraryMetadataProductionCanaryModeExtensions
{
    public static bool RequestsExecution(this CanonicalLibraryMetadataProductionCanaryMode mode) => mode == CanonicalLibraryMetadataProductionCanaryMode.canaryN1Execute;
    public static bool IsConfigured(this CanonicalLibraryMetadataProductionCanaryMode mode) => mode != CanonicalLibraryMetadataProductionCanaryMode.disabled;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataProductionCanaryRootMode
{
    disabled,
    testRoot,
    productionRootExplicit
}

public static class CanonicalLibraryMetadataProductionCanaryRootModeExtensions
{
    public static bool IsProductionRoot(this CanonicalLibraryMetadataProductionCanaryRootMode mode) => mode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit;
}

public sealed class CanonicalLibraryMetadataProductionCanaryPolicy : IEquatable<CanonicalLibraryMetadataProductionCanaryPolicy>
{
    public CanonicalMigrationDomain Domain { get; set; }
    public int CanaryMaxObjectsPerSyncRun { get; set; }
    public bool RequiresExplicitInternalDebugConfiguration { get; set; }
    public bool RequiresProductionToken { get; set; }
    public bool RequiresOwnerApproval { get; set; }
    public bool RequiresRollbackPlan { get; set; }
    public bool RequiresReadSideParallelEquivalent { get; set; }
    public bool ProductionRootDisabledByDefault { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool AllowAllEligible { get; set; }
    public bool ReleaseDefaultEnabled { get; set; }

    public CanonicalLibraryMetadataProductionCanaryPolicy(
        CanonicalMigrationDomain domain = CanonicalMigrationDomain.libraryMetadata,
        int canaryMaxObjectsPerSyncRun = 1,
        bool requiresExplicitInternalDebugConfiguration = true,
        bool requiresProductionToken = true,
        bool requiresOwnerApproval = true,
        bool requiresRollbackPlan = true,
        bool requiresReadSideParallelEquivalent = true,
        bool productionRootDisabledByDefault = true,
        bool runtimeSwitchEnabled = false,
        bool allowAllEligible = false,
        bool releaseDefaultEnabled = false)
    {
        Domain = domain;
        CanaryMaxObjectsPerSyncRun = Math.Max(0, canaryMaxObjectsPerSyncRun);
        RequiresExplicitInternalDebugConfiguration = requiresExplicitInternalDebugConfiguration;
        RequiresProductionToken = requiresProductionToken;
        RequiresOwnerApproval = requiresOwnerApproval;
        RequiresRollbackPlan = requiresRollbackPlan;
        RequiresReadSideParallelEquivalent = requiresReadSideParallelEquivalent;
        ProductionRootDisabledByDefault = productionRootDisabledByDefault;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        AllowAllEligible = allowAllEligible;
        ReleaseDefaultEnabled = releaseDefaultEnabled;
    }

    public static readonly CanonicalLibraryMetadataProductionCanaryPolicy StrictLibraryMetadataN1 = new();

    public bool IsStrictN1LibraryMetadata =>
        Domain == CanonicalMigrationDomain.libraryMetadata &&
        CanaryMaxObjectsPerSyncRun == 1 &&
        RequiresExplicitInternalDebugConfiguration &&
        ProductionRootDisabledByDefault &&
        !RuntimeSwitchEnabled &&
        !AllowAllEligible &&
        !ReleaseDefaultEnabled;

    public CanonicalLibraryMetadataCanaryPolicy AsCanaryPolicy =>
        new(canaryMaxObjectsPerSyncRun: CanaryMaxObjectsPerSyncRun,
            allowsInternalN1Execution: true,
            explicitInternalTestConfiguration: true,
            runtimeSwitchEnabled: RuntimeSwitchEnabled,
            allowAllEligible: AllowAllEligible);

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataProductionCanaryPolicy other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataProductionCanaryPolicy? other) =>
        other is not null && Domain == other.Domain && CanaryMaxObjectsPerSyncRun == other.CanaryMaxObjectsPerSyncRun &&
        RequiresExplicitInternalDebugConfiguration == other.RequiresExplicitInternalDebugConfiguration &&
        RequiresProductionToken == other.RequiresProductionToken &&
        RequiresOwnerApproval == other.RequiresOwnerApproval &&
        RequiresRollbackPlan == other.RequiresRollbackPlan &&
        RequiresReadSideParallelEquivalent == other.RequiresReadSideParallelEquivalent &&
        ProductionRootDisabledByDefault == other.ProductionRootDisabledByDefault &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        AllowAllEligible == other.AllowAllEligible &&
        ReleaseDefaultEnabled == other.ReleaseDefaultEnabled;
    public override int GetHashCode() =>
        HashCode.Combine(Domain, CanaryMaxObjectsPerSyncRun, RequiresExplicitInternalDebugConfiguration,
            RequiresProductionToken, RequiresOwnerApproval, RequiresRollbackPlan, RequiresReadSideParallelEquivalent,
            ProductionRootDisabledByDefault, RuntimeSwitchEnabled, AllowAllEligible, ReleaseDefaultEnabled);
    public static bool operator ==(CanonicalLibraryMetadataProductionCanaryPolicy left, CanonicalLibraryMetadataProductionCanaryPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataProductionCanaryPolicy left, CanonicalLibraryMetadataProductionCanaryPolicy right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataProductionCanaryConfiguration : IEquatable<CanonicalLibraryMetadataProductionCanaryConfiguration>
{
    public CanonicalLibraryMetadataProductionCanaryMode Mode { get; set; }
    public CanonicalLibraryMetadataProductionCanaryRootMode RootMode { get; set; }
    public CanonicalLibraryMetadataProductionCanaryPolicy Policy { get; set; }
    public bool ExplicitInternalDebugConfiguration { get; set; }
    public bool AllowProductionRootWrites { get; set; }

    public CanonicalLibraryMetadataProductionCanaryConfiguration(
        CanonicalLibraryMetadataProductionCanaryMode mode = CanonicalLibraryMetadataProductionCanaryMode.disabled,
        CanonicalLibraryMetadataProductionCanaryRootMode rootMode = CanonicalLibraryMetadataProductionCanaryRootMode.disabled,
        CanonicalLibraryMetadataProductionCanaryPolicy? policy = null,
        bool explicitInternalDebugConfiguration = false,
        bool allowProductionRootWrites = false)
    {
        Mode = mode;
        RootMode = rootMode;
        Policy = policy ?? CanonicalLibraryMetadataProductionCanaryPolicy.StrictLibraryMetadataN1;
        ExplicitInternalDebugConfiguration = explicitInternalDebugConfiguration;
        AllowProductionRootWrites = allowProductionRootWrites;
    }

    public static readonly CanonicalLibraryMetadataProductionCanaryConfiguration Disabled = new();

    public static CanonicalLibraryMetadataProductionCanaryConfiguration DiagnosticsOnly(bool explicitInternalDebugConfiguration = true) =>
        new(CanonicalLibraryMetadataProductionCanaryMode.diagnosticsOnly, explicitInternalDebugConfiguration: explicitInternalDebugConfiguration);

    public static CanonicalLibraryMetadataProductionCanaryConfiguration ExplicitTestRootN1Armed() =>
        new(CanonicalLibraryMetadataProductionCanaryMode.canaryN1Armed, CanonicalLibraryMetadataProductionCanaryRootMode.testRoot, explicitInternalDebugConfiguration: true);

    public static CanonicalLibraryMetadataProductionCanaryConfiguration ExplicitTestRootN1Execute() =>
        new(CanonicalLibraryMetadataProductionCanaryMode.canaryN1Execute, CanonicalLibraryMetadataProductionCanaryRootMode.testRoot, explicitInternalDebugConfiguration: true);

    public static CanonicalLibraryMetadataProductionCanaryConfiguration ExplicitProductionRootN1Execute(bool allowProductionRootWrites) =>
        new(CanonicalLibraryMetadataProductionCanaryMode.canaryN1Execute, CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit,
            explicitInternalDebugConfiguration: true, allowProductionRootWrites: allowProductionRootWrites);

    public int CanaryMaxObjectsPerSyncRun => Policy.CanaryMaxObjectsPerSyncRun;

    public CanonicalLibraryMetadataCanaryConfiguration AsN1CanaryConfiguration
    {
        get
        {
            if (!Mode.RequestsExecution()) return CanonicalLibraryMetadataCanaryConfiguration.Disabled;
            return new CanonicalLibraryMetadataCanaryConfiguration(
                CanonicalLibraryMetadataCanaryMode.n1, Policy.Domain,
                Policy.CanaryMaxObjectsPerSyncRun, ExplicitInternalDebugConfiguration,
                Policy.RequiresProductionToken, Policy.RequiresOwnerApproval,
                Policy.RequiresRollbackPlan, Policy.RuntimeSwitchEnabled,
                Policy.AllowAllEligible, Policy.ReleaseDefaultEnabled);
        }
    }

    public bool StrictExecutableN1 =>
        Mode == CanonicalLibraryMetadataProductionCanaryMode.canaryN1Execute &&
        (RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.testRoot || RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit) &&
        ExplicitInternalDebugConfiguration && Policy.IsStrictN1LibraryMetadata &&
        (RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.testRoot ? !AllowProductionRootWrites : AllowProductionRootWrites);

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataProductionCanaryConfiguration other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataProductionCanaryConfiguration? other) =>
        other is not null && Mode == other.Mode && RootMode == other.RootMode &&
        EqualityComparer<CanonicalLibraryMetadataProductionCanaryPolicy>.Default.Equals(Policy, other.Policy) &&
        ExplicitInternalDebugConfiguration == other.ExplicitInternalDebugConfiguration &&
        AllowProductionRootWrites == other.AllowProductionRootWrites;
    public override int GetHashCode() =>
        HashCode.Combine(Mode, RootMode, Policy, ExplicitInternalDebugConfiguration, AllowProductionRootWrites);
    public static bool operator ==(CanonicalLibraryMetadataProductionCanaryConfiguration left, CanonicalLibraryMetadataProductionCanaryConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataProductionCanaryConfiguration left, CanonicalLibraryMetadataProductionCanaryConfiguration right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataProductionRootBlocker
{
    missingExplicitDebugInternalConfiguration,
    modeNotExecuteN1Canary,
    rootModeNotProductionRootExplicit,
    allowProductionRootWritesFalse,
    missingOwnerApproval,
    activePilotNotLibraryMetadata,
    multipleActivePilots,
    runtimeSwitchEnabled,
    landingFreezeNotGreen,
    diagnosticsOnlyEvidenceMissing,
    armN1EvidenceMissing,
    testRootExecuteEvidenceMissing,
    readSideDivergenceNonZero,
    rollbackEvidenceMissing,
    legacyFallbackUnavailable,
    safeCandidateMissing,
    multipleSafeCandidatesDenied,
    unsafeCandidateSelected,
    noResourceMoveGuardMissing,
    resourceMoveAttempted,
    noContentWriteGuardMissing,
    contentWriteAttempted,
    tombstoneDeleteAttempted,
    productionRootContainmentUnverified,
    checkpointUnavailable,
    postconditionVerificationUnavailable,
    n1BudgetRequired,
    allEligibleDenied,
    nonLibraryMetadataDomain,
    releaseDefaultDenied,
    defaultEnablementDenied,
    localSnapshotUnavailable,
    peerSnapshotUnavailable,
    commitExecutorUnavailable,
    unsupportedTrigger,
    productionPortUnavailable,
    realApplyPortUnavailable,
    rootBoundWriteUnavailable,
    atomicWriteUnavailable,
}

public sealed class CanonicalLibraryMetadataProductionRootGateResult : IEquatable<CanonicalLibraryMetadataProductionRootGateResult>
{
    public bool Allowed { get; set; }
    public List<CanonicalLibraryMetadataProductionRootBlocker> Blockers { get; set; }
    public CanonicalLibraryMetadataCanaryCandidate? SelectedCandidate { get; set; }
    public CanonicalLibraryMetadataCanaryCandidateSafety? SelectedCandidateSafety { get; set; }
    public CanonicalMigrationLandingFreezeResult FreezeResult { get; set; }
    public string DiagnosticsSummary { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataProductionRootGateResult(
        List<CanonicalLibraryMetadataProductionRootBlocker> blockers,
        CanonicalLibraryMetadataCanaryCandidate? selectedCandidate,
        CanonicalLibraryMetadataCanaryCandidateSafety? selectedCandidateSafety,
        CanonicalMigrationLandingFreezeResult freezeResult)
    {
        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataProductionRootBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        Allowed = uniqueBlockers.Count == 0;
        Blockers = uniqueBlockers;
        SelectedCandidate = selectedCandidate;
        SelectedCandidateSafety = selectedCandidateSafety;
        FreezeResult = freezeResult;
        DiagnosticsSummary = string.Join(",",
            $"allowed={Allowed}", "rootMode=productionRootExplicit",
            $"candidateKind={selectedCandidateSafety?.Kind.ToString() ?? "none"}",
            $"objectKind={selectedCandidate?.ObjectKind.ToString() ?? "none"}",
            $"domain={selectedCandidate?.Domain.ToString() ?? "none"}",
            $"blockers={string.Join("|", uniqueBlockers.Select(b => b.ToString()))}", "redacted=true");
        Redacted = true;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataProductionRootGateResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataProductionRootGateResult? other) =>
        other is not null && Allowed == other.Allowed && Blockers.SequenceEqual(other.Blockers) &&
        EqualityComparer<CanonicalLibraryMetadataCanaryCandidate?>.Default.Equals(SelectedCandidate, other.SelectedCandidate) &&
        EqualityComparer<CanonicalLibraryMetadataCanaryCandidateSafety?>.Default.Equals(SelectedCandidateSafety, other.SelectedCandidateSafety) &&
        EqualityComparer<CanonicalMigrationLandingFreezeResult>.Default.Equals(FreezeResult, other.FreezeResult) &&
        DiagnosticsSummary == other.DiagnosticsSummary && Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(Allowed, Blockers.Count, SelectedCandidate, SelectedCandidateSafety, FreezeResult, DiagnosticsSummary, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataProductionRootGateResult left, CanonicalLibraryMetadataProductionRootGateResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataProductionRootGateResult left, CanonicalLibraryMetadataProductionRootGateResult right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataProductionRootGate
{
    public CanonicalLibraryMetadataProductionRootGateResult Evaluate(
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration,
        CanonicalCutoverToken? token,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        CanonicalMigrationDomainMatrix matrix,
        List<CanonicalLibraryMetadataCutoverCandidate> candidates,
        CanonicalSyncPlanTrigger trigger,
        bool localSnapshotAvailable,
        bool peerSnapshotAvailable,
        bool executorAvailable)
    {
        var blockers = new List<CanonicalLibraryMetadataProductionRootBlocker>();
        if (!configuration.ExplicitInternalDebugConfiguration) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.missingExplicitDebugInternalConfiguration);
        if (configuration.Mode != CanonicalLibraryMetadataProductionCanaryMode.canaryN1Execute) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.modeNotExecuteN1Canary);
        if (configuration.RootMode != CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.rootModeNotProductionRootExplicit);
        if (!configuration.AllowProductionRootWrites) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.allowProductionRootWritesFalse);
        if (configuration.Policy.Domain != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.nonLibraryMetadataDomain);
        if (configuration.Policy.CanaryMaxObjectsPerSyncRun != 1) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.n1BudgetRequired);
        if (configuration.Policy.RuntimeSwitchEnabled) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.runtimeSwitchEnabled);
        if (configuration.Policy.AllowAllEligible) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.allEligibleDenied);
        if (configuration.Policy.ReleaseDefaultEnabled) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.releaseDefaultDenied);
        if (token?.OwnerApproved != true) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.missingOwnerApproval);
        if (!localSnapshotAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.localSnapshotUnavailable);
        if (!peerSnapshotAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.peerSnapshotUnavailable);
        if (!executorAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.commitExecutorUnavailable);
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh || trigger == CanonicalSyncPlanTrigger.retryDrainer) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.unsupportedTrigger);

        var freeze = new CanonicalMigrationLandingFreeze().Evaluate(matrix,
            releaseDefaultEnabled: configuration.Policy.ReleaseDefaultEnabled,
            runtimeSwitchEnabled: configuration.Policy.RuntimeSwitchEnabled,
            legacyFallbackAvailable: evidence.LegacyFallbackAvailable,
            canaryMaxObjectsPerSyncRun: configuration.Policy.CanaryMaxObjectsPerSyncRun,
            allEligibleEnabled: configuration.Policy.AllowAllEligible);

        if (!freeze.Allowed) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.landingFreezeNotGreen);
        if (freeze.ActivePilotDomain != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.activePilotNotLibraryMetadata);
        if (matrix.Policies.Count(p => p.ActivePilot) > 1) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.multipleActivePilots);
        if (matrix.Policies.Any(p => p.DefaultCutoverEnabled)) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.defaultEnablementDenied);

        if (!evidence.NoCommitEvidenceAvailable || !evidence.DryRunEquivalenceVerified) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.diagnosticsOnlyEvidenceMissing);
        if (!evidence.RealDataShadowCopyVerified || !evidence.ExecutionShadowVerified || !evidence.MetadataManifestRouteEvidenceAvailable)
            blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.armN1EvidenceMissing);
        if (!evidence.TestRootUsed) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.testRootExecuteEvidenceMissing);
        if (!evidence.ReadSideParallelEquivalent) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.readSideDivergenceNonZero);
        if (!evidence.RollbackCheckpointAvailable || !evidence.RollbackVerified || !evidence.RollbackRehearsalPassed)
            blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.rollbackEvidenceMissing);
        if (!evidence.LegacyFallbackAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.legacyFallbackUnavailable);
        if (!evidence.ProductionPortAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.productionPortUnavailable);
        if (!evidence.RealRootBoundApplyPortAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.realApplyPortUnavailable);
        if (evidence.ApplyPortMode != CanonicalLibraryMetadataApplyPortMode.productionRootBound) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.productionRootContainmentUnverified);
        if (!evidence.RootBoundWriteAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.rootBoundWriteUnavailable);
        if (!evidence.AtomicReplaceAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.atomicWriteUnavailable);

        var selector = new CanonicalLibraryMetadataCanarySelector().Select(
            CanonicalCutoverMode.canary,
            new CanonicalLibraryMetadataCanaryPolicy(canaryMaxObjectsPerSyncRun: 1, allowsInternalN1Execution: true, explicitInternalTestConfiguration: true),
            trigger, evidence, candidates);

        var safetyReports = candidates.Select(c => new CanonicalLibraryMetadataCanaryCandidateSafety(c, evidence)).ToList();
        var safeReports = safetyReports.Where(s => s.Safe).ToList();

        if (safeReports.Count == 0) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.safeCandidateMissing);
        if (safeReports.Count > 1) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.multipleSafeCandidatesDenied);
        if (selector.SelectedCandidates.Count != 1) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.safeCandidateMissing);
        if (safetyReports.Any(s => !s.Safe)) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.unsafeCandidateSelected);
        if (safetyReports.Any(s => s.ResourceMoveAttempted)) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.resourceMoveAttempted);
        if (safetyReports.Any(s => s.ContentBytesMutated)) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.contentWriteAttempted);
        if (safetyReports.Any(s => s.PhysicalDeleteAttempted)) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.tombstoneDeleteAttempted);

        var selected = selector.SelectedCandidates.FirstOrDefault();
        var selectedSafety = safetyReports.FirstOrDefault(s => s.Candidate.Id == selected?.Id);

        if (selected == null || selectedSafety == null)
            return new CanonicalLibraryMetadataProductionRootGateResult(blockers, null, null, freeze);

        if (!selectedSafety.MetadataOnly) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.noContentWriteGuardMissing);
        if (selectedSafety.ResourceMoveAttempted) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.noResourceMoveGuardMissing);
        if (selectedSafety.ContentBytesMutated) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.contentWriteAttempted);
        if (selectedSafety.PhysicalDeleteAttempted) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.tombstoneDeleteAttempted);
        if (selected.CutoverCandidate.RollbackCheckpointID == null) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.checkpointUnavailable);
        if (!evidence.AtomicReplaceAvailable || !evidence.RootBoundWriteAvailable) blockers.Add(CanonicalLibraryMetadataProductionRootBlocker.postconditionVerificationUnavailable);

        return new CanonicalLibraryMetadataProductionRootGateResult(blockers, selected, selectedSafety, freeze);
    }
}

public sealed class CanonicalLibraryMetadataProductionRootSafetyProof : IEquatable<CanonicalLibraryMetadataProductionRootSafetyProof>
{
    public bool RootContainmentVerified { get; set; }
    public bool ProductionRootModeExplicit { get; set; }
    public bool LogicalTokenSafety { get; set; }
    public string? CheckpointID { get; set; }
    public bool AtomicWriteUsed { get; set; }
    public bool PostconditionVerified { get; set; }
    public bool RollbackAvailable { get; set; }
    public bool RollbackVerifiedIfUsed { get; set; }
    public bool SideEffectWhitelistPassed { get; set; }
    public bool NoResourceMove { get; set; }
    public bool NoContentWrite { get; set; }
    public bool NoOtherDomainMutation { get; set; }
    public string RedactedTargetSummary { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataProductionRootSafetyProof(
        bool rootContainmentVerified = false,
        bool productionRootModeExplicit = false,
        bool logicalTokenSafety = false,
        string? checkpointID = null,
        bool atomicWriteUsed = false,
        bool postconditionVerified = false,
        bool rollbackAvailable = false,
        bool rollbackVerifiedIfUsed = false,
        bool sideEffectWhitelistPassed = false,
        bool noResourceMove = false,
        bool noContentWrite = false,
        bool noOtherDomainMutation = false,
        string redactedTargetSummary = "",
        bool redacted = false)
    {
        RootContainmentVerified = rootContainmentVerified;
        ProductionRootModeExplicit = productionRootModeExplicit;
        LogicalTokenSafety = logicalTokenSafety;
        CheckpointID = checkpointID;
        AtomicWriteUsed = atomicWriteUsed;
        PostconditionVerified = postconditionVerified;
        RollbackAvailable = rollbackAvailable;
        RollbackVerifiedIfUsed = rollbackVerifiedIfUsed;
        SideEffectWhitelistPassed = sideEffectWhitelistPassed;
        NoResourceMove = noResourceMove;
        NoContentWrite = noContentWrite;
        NoOtherDomainMutation = noOtherDomainMutation;
        RedactedTargetSummary = redactedTargetSummary;
        Redacted = redacted;
    }

    public CanonicalLibraryMetadataProductionRootSafetyProof(
        CanonicalLibraryMetadataProductionRootGateResult gate,
        CanonicalLibraryMetadataCutoverResult? cutoverResult,
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration)
    {
        var commit = cutoverResult?.Commits.FirstOrDefault();
        var rollbackResults = cutoverResult?.RollbackResults ?? new List<CanonicalLibraryMetadataRollbackExecutionResult>();
        var candidate = gate.SelectedCandidate;

        RootContainmentVerified = gate.Allowed && configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit;
        ProductionRootModeExplicit = configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit;
        LogicalTokenSafety = gate.SelectedCandidateSafety?.Safe == true;
        CheckpointID = candidate?.CutoverCandidate.EffectiveRollbackCheckpointID;
        AtomicWriteUsed = commit?.Committed == true && (commit?.SideEffects.Any(se => se.Kind == CanonicalProductionSideEffectKind.metadataApply) ?? false);
        PostconditionVerified = commit?.PostconditionVerified == true;
        RollbackAvailable = candidate?.CutoverCandidate.RollbackCheckpointID != null;
        RollbackVerifiedIfUsed = rollbackResults.Count == 0 || rollbackResults.All(r => r.Succeeded && !r.Fatal);
        SideEffectWhitelistPassed = commit?.SideEffects.All(se =>
            se.Kind == CanonicalProductionSideEffectKind.metadataApply &&
            (se.Domain == CanonicalProductionDomain.folders || se.Domain == CanonicalProductionDomain.studyItems || se.Domain == CanonicalProductionDomain.standaloneNotes)) ?? gate.Allowed;
        NoResourceMove = gate.SelectedCandidateSafety?.ResourceMoveAttempted == false;
        NoContentWrite = gate.SelectedCandidateSafety?.ContentBytesMutated == false;
        NoOtherDomainMutation = commit?.SideEffects.All(se =>
            se.Domain == CanonicalProductionDomain.folders || se.Domain == CanonicalProductionDomain.studyItems || se.Domain == CanonicalProductionDomain.standaloneNotes) ?? true;
        RedactedTargetSummary = string.Join(",",
            $"domain={candidate?.Domain.ToString() ?? "none"}",
            $"objectKind={candidate?.ObjectKind.ToString() ?? "none"}",
            $"candidateKind={gate.SelectedCandidateSafety?.Kind.ToString() ?? "none"}",
            $"hashPrefix={candidate?.MetadataHashPrefix ?? "none"}");
        Redacted = true;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataProductionRootSafetyProof other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataProductionRootSafetyProof? other) =>
        other is not null && RootContainmentVerified == other.RootContainmentVerified &&
        ProductionRootModeExplicit == other.ProductionRootModeExplicit &&
        LogicalTokenSafety == other.LogicalTokenSafety && CheckpointID == other.CheckpointID &&
        AtomicWriteUsed == other.AtomicWriteUsed && PostconditionVerified == other.PostconditionVerified &&
        RollbackAvailable == other.RollbackAvailable && RollbackVerifiedIfUsed == other.RollbackVerifiedIfUsed &&
        SideEffectWhitelistPassed == other.SideEffectWhitelistPassed &&
        NoResourceMove == other.NoResourceMove && NoContentWrite == other.NoContentWrite &&
        NoOtherDomainMutation == other.NoOtherDomainMutation &&
        RedactedTargetSummary == other.RedactedTargetSummary && Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(RootContainmentVerified, ProductionRootModeExplicit, LogicalTokenSafety, CheckpointID,
            AtomicWriteUsed, PostconditionVerified, RollbackAvailable, RollbackVerifiedIfUsed, SideEffectWhitelistPassed,
            NoResourceMove, NoContentWrite, NoOtherDomainMutation, RedactedTargetSummary, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataProductionRootSafetyProof left, CanonicalLibraryMetadataProductionRootSafetyProof right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataProductionRootSafetyProof left, CanonicalLibraryMetadataProductionRootSafetyProof right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataRealCanaryBlocker
{
    disabled,
    diagnosticsOnlyNoExecution,
    armedNoExecution,
    blockedMode,
    nonLibraryMetadataDomain,
    n1BudgetRequired,
    canaryBudgetAboveOneDenied,
    missingExplicitInternalDebugConfiguration,
    runtimeSwitchDenied,
    allEligibleDenied,
    releaseDefaultDenied,
    defaultEnablementDenied,
    missingToken,
    missingOwnerApproval,
    matrixValidationBlocked,
    activePilotNotLibraryMetadata,
    localSnapshotUnavailable,
    peerSnapshotUnavailable,
    missingNoCommitEvidence,
    missingRealDataShadowCopyEvidence,
    missingExecutionShadowEvidence,
    missingDryRunEquivalence,
    blockingDivergence,
    unresolvedConflict,
    missingMetadataManifestRouteEvidence,
    productionPortUnavailable,
    realApplyPortUnavailable,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    rollbackPlanMissing,
    rollbackVerificationMissing,
    productionRootGuardMissing,
    productionRootWritesDisabled,
    productionRootNotExplicit,
    productionRootExplicitBlockedV830,
    allowProductionRootWritesDeniedV830,
    testRootMissing,
    legacyFallbackUnavailable,
    readSideParallelMissing,
    readSideParallelDivergent,
    commitExecutorUnavailable,
    unsupportedTrigger,
    noEligibleCandidate,
    unsafeCandidateSkipped,
    rollbackFailure,
    fatalBlocker,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataRealCanaryObservationStatus
{
    disabled,
    diagnosticsOnly,
    armed,
    blocked,
    noEligibleCandidate,
    unsafeCandidateSkipped,
    executedSucceeded,
    executedFailedRolledBack,
    fatalRollbackFailure,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataRealCanaryRecommendation
{
    remainDisabled,
    stayN1,
    readyForN3AfterAudit,
    fixBlockers,
}

public sealed class CanonicalLibraryMetadataRealCanaryObservationReport : IEquatable<CanonicalLibraryMetadataRealCanaryObservationReport>
{
    public CanonicalLibraryMetadataRealCanaryObservationStatus Status { get; set; }
    public CanonicalLibraryMetadataRealCanaryRecommendation Recommendation { get; set; }
    public CanonicalLibraryMetadataProductionCanaryMode Mode { get; set; }
    public CanonicalLibraryMetadataProductionCanaryRootMode RootMode { get; set; }
    public CanonicalMigrationDomain Domain { get; set; }
    public int CanaryMaxObjectsPerSyncRun { get; set; }
    public int SelectedCandidateCount { get; set; }
    public int ExecutedCandidateCount { get; set; }
    public int SuccessfulCommitCount { get; set; }
    public int FailedCommitCount { get; set; }
    public int RollbackCount { get; set; }
    public int RollbackFailureCount { get; set; }
    public int LegacyFallbackCount { get; set; }
    public int DuplicateSuppressionCount { get; set; }
    public int NoEligibleCandidateCount { get; set; }
    public int UnsafeCandidateSkippedCount { get; set; }
    public int FatalBlockerCount { get; set; }
    public bool ReadSideParallelEquivalent { get; set; }
    public bool ReadSideParallelDivergent { get; set; }
    public bool LegacyFallbackPreserved { get; set; }
    public bool DuplicateSuppressionApplied { get; set; }
    public bool ProductionRootWriteAttempted { get; set; }
    public bool ProductionRootWriteExplicitlyAllowed { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool AllEligibleEnabled { get; set; }
    public bool UiMutated { get; set; }
    public bool ResourceMoved { get; set; }
    public bool UploadJobCreated { get; set; }
    public List<CanonicalLibraryMetadataRealCanaryBlocker> Blockers { get; set; }
    public string Reason { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataRealCanaryObservationReport(
        CanonicalLibraryMetadataRealCanaryObservationStatus status,
        CanonicalLibraryMetadataRealCanaryRecommendation recommendation,
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration,
        int selectedCandidateCount = 0,
        int executedCandidateCount = 0,
        int successfulCommitCount = 0,
        int failedCommitCount = 0,
        int rollbackCount = 0,
        int rollbackFailureCount = 0,
        int legacyFallbackCount = 0,
        int duplicateSuppressionCount = 0,
        int noEligibleCandidateCount = 0,
        int unsafeCandidateSkippedCount = 0,
        int fatalBlockerCount = 0,
        bool readSideParallelEquivalent = false,
        bool readSideParallelDivergent = false,
        bool legacyFallbackPreserved = true,
        bool duplicateSuppressionApplied = false,
        bool productionRootWriteAttempted = false,
        List<CanonicalLibraryMetadataRealCanaryBlocker>? blockers = null,
        string reason = "",
        bool redacted = true)
    {
        Status = status;
        Recommendation = recommendation;
        Mode = configuration.Mode;
        RootMode = configuration.RootMode;
        Domain = configuration.Policy.Domain;
        CanaryMaxObjectsPerSyncRun = configuration.Policy.CanaryMaxObjectsPerSyncRun;
        SelectedCandidateCount = Math.Max(0, selectedCandidateCount);
        ExecutedCandidateCount = Math.Max(0, executedCandidateCount);
        SuccessfulCommitCount = Math.Max(0, successfulCommitCount);
        FailedCommitCount = Math.Max(0, failedCommitCount);
        RollbackCount = Math.Max(0, rollbackCount);
        RollbackFailureCount = Math.Max(0, rollbackFailureCount);
        LegacyFallbackCount = Math.Max(0, legacyFallbackCount);
        DuplicateSuppressionCount = Math.Max(0, duplicateSuppressionCount);
        NoEligibleCandidateCount = Math.Max(0, noEligibleCandidateCount);
        UnsafeCandidateSkippedCount = Math.Max(0, unsafeCandidateSkippedCount);
        FatalBlockerCount = Math.Max(0, fatalBlockerCount);
        ReadSideParallelEquivalent = readSideParallelEquivalent;
        ReadSideParallelDivergent = readSideParallelDivergent;
        LegacyFallbackPreserved = legacyFallbackPreserved;
        DuplicateSuppressionApplied = duplicateSuppressionApplied;
        ProductionRootWriteAttempted = productionRootWriteAttempted;
        ProductionRootWriteExplicitlyAllowed = configuration.AllowProductionRootWrites;
        RuntimeSwitchEnabled = configuration.Policy.RuntimeSwitchEnabled;
        AllEligibleEnabled = configuration.Policy.AllowAllEligible;
        UiMutated = false;
        ResourceMoved = false;
        UploadJobCreated = false;
        Blockers = new HashSet<CanonicalLibraryMetadataRealCanaryBlocker>(blockers ?? new List<CanonicalLibraryMetadataRealCanaryBlocker>())
            .OrderBy(b => b.ToString()).ToList();
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? Status.ToString();
        Redacted = redacted;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"status={Status}", $"recommendation={Recommendation}", $"mode={Mode}", $"rootMode={RootMode}",
        $"domain={Domain}", $"budget={CanaryMaxObjectsPerSyncRun}",
        $"selected={SelectedCandidateCount}", $"executed={ExecutedCandidateCount}",
        $"success={SuccessfulCommitCount}", $"failure={FailedCommitCount}",
        $"rollback={RollbackCount}", $"rollbackFailure={RollbackFailureCount}",
        $"legacyFallback={LegacyFallbackCount}", $"duplicateSuppression={DuplicateSuppressionCount}",
        $"noEligible={NoEligibleCandidateCount}", $"unsafeSkipped={UnsafeCandidateSkippedCount}",
        $"fatal={FatalBlockerCount}", $"readSideEquivalent={ReadSideParallelEquivalent}",
        $"readSideDivergent={ReadSideParallelDivergent}",
        $"runtimeSwitch={RuntimeSwitchEnabled}", $"allEligible={AllEligibleEnabled}",
        $"uiMutated={UiMutated}", $"resourceMoved={ResourceMoved}", $"uploadJobCreated={UploadJobCreated}",
        $"blockers={string.Join("|", Blockers.Select(b => b.ToString()))}", $"redacted={Redacted}");

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataRealCanaryObservationReport other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataRealCanaryObservationReport? other) =>
        other is not null && Status == other.Status && Recommendation == other.Recommendation &&
        Mode == other.Mode && RootMode == other.RootMode && Domain == other.Domain &&
        CanaryMaxObjectsPerSyncRun == other.CanaryMaxObjectsPerSyncRun &&
        SelectedCandidateCount == other.SelectedCandidateCount && ExecutedCandidateCount == other.ExecutedCandidateCount &&
        SuccessfulCommitCount == other.SuccessfulCommitCount && FailedCommitCount == other.FailedCommitCount &&
        RollbackCount == other.RollbackCount && RollbackFailureCount == other.RollbackFailureCount &&
        LegacyFallbackCount == other.LegacyFallbackCount && DuplicateSuppressionCount == other.DuplicateSuppressionCount &&
        NoEligibleCandidateCount == other.NoEligibleCandidateCount && UnsafeCandidateSkippedCount == other.UnsafeCandidateSkippedCount &&
        FatalBlockerCount == other.FatalBlockerCount &&
        ReadSideParallelEquivalent == other.ReadSideParallelEquivalent && ReadSideParallelDivergent == other.ReadSideParallelDivergent &&
        LegacyFallbackPreserved == other.LegacyFallbackPreserved && DuplicateSuppressionApplied == other.DuplicateSuppressionApplied &&
        ProductionRootWriteAttempted == other.ProductionRootWriteAttempted &&
        ProductionRootWriteExplicitlyAllowed == other.ProductionRootWriteExplicitlyAllowed &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled && AllEligibleEnabled == other.AllEligibleEnabled &&
        UiMutated == other.UiMutated && ResourceMoved == other.ResourceMoved && UploadJobCreated == other.UploadJobCreated &&
        Blockers.SequenceEqual(other.Blockers) && Reason == other.Reason && Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(Status, Recommendation, Mode, RootMode, Domain, CanaryMaxObjectsPerSyncRun,
            SelectedCandidateCount, ExecutedCandidateCount, SuccessfulCommitCount, FailedCommitCount, RollbackCount,
            RollbackFailureCount, LegacyFallbackCount, DuplicateSuppressionCount, NoEligibleCandidateCount,
            UnsafeCandidateSkippedCount, FatalBlockerCount, ReadSideParallelEquivalent, ReadSideParallelDivergent,
            LegacyFallbackPreserved, DuplicateSuppressionApplied, ProductionRootWriteAttempted,
            ProductionRootWriteExplicitlyAllowed, RuntimeSwitchEnabled, AllEligibleEnabled, UiMutated, ResourceMoved,
            UploadJobCreated, Blockers.Count, Reason, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataRealCanaryObservationReport left, CanonicalLibraryMetadataRealCanaryObservationReport right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataRealCanaryObservationReport left, CanonicalLibraryMetadataRealCanaryObservationReport right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataProductionCanaryInjectionResult : IEquatable<CanonicalLibraryMetadataProductionCanaryInjectionResult>
{
    public CanonicalLibraryMetadataProductionCanaryConfiguration Configuration { get; set; }
    public bool InjectionConfigured { get; set; }
    public bool ExecutorInjected { get; set; }
    public bool ApplyPortInjected { get; set; }
    public bool Armed { get; set; }
    public bool Executed { get; set; }
    public bool Succeeded { get; set; }
    public List<CanonicalLibraryMetadataRealCanaryBlocker> Blockers { get; set; }
    public List<CanonicalLibraryMetadataCutoverDiagnostic> Diagnostics { get; set; }
    public CanonicalLibraryMetadataCanaryResult? CanaryResult { get; set; }
    public CanonicalLibraryMetadataCutoverResult? CutoverResult { get; set; }
    public CanonicalLibraryMetadataCanarySelectionResult? Selection { get; set; }
    public List<CanonicalLibraryMetadataCanaryCandidateSafety>? CandidateSafetyReports { get; set; }
    public CanonicalLibraryMetadataProductionRootGateResult? ProductionRootGate { get; set; }
    public CanonicalLibraryMetadataProductionRootSafetyProof? ProductionRootSafetyProof { get; set; }
    public CanonicalLibraryMetadataRealCanaryObservationReport ObservationReport { get; set; }

    public CanonicalLibraryMetadataProductionCanaryInjectionResult(
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration,
        bool injectionConfigured,
        bool executorInjected,
        bool applyPortInjected,
        bool armed,
        bool executed,
        bool succeeded,
        List<CanonicalLibraryMetadataRealCanaryBlocker> blockers,
        List<CanonicalLibraryMetadataCutoverDiagnostic> diagnostics,
        CanonicalLibraryMetadataCanaryResult? canaryResult = null,
        CanonicalLibraryMetadataCutoverResult? cutoverResult = null,
        CanonicalLibraryMetadataCanarySelectionResult? selection = null,
        List<CanonicalLibraryMetadataCanaryCandidateSafety>? candidateSafetyReports = null,
        CanonicalLibraryMetadataProductionRootGateResult? productionRootGate = null,
        CanonicalLibraryMetadataProductionRootSafetyProof? productionRootSafetyProof = null,
        CanonicalLibraryMetadataRealCanaryObservationReport? observationReport = null)
    {
        Configuration = configuration;
        InjectionConfigured = injectionConfigured;
        ExecutorInjected = executorInjected;
        ApplyPortInjected = applyPortInjected;
        Armed = armed;
        Executed = executed;
        Succeeded = succeeded;
        Blockers = new HashSet<CanonicalLibraryMetadataRealCanaryBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        Diagnostics = diagnostics;
        CanaryResult = canaryResult;
        CutoverResult = cutoverResult;
        Selection = selection;
        CandidateSafetyReports = candidateSafetyReports;
        ProductionRootGate = productionRootGate;
        ProductionRootSafetyProof = productionRootSafetyProof;
        ObservationReport = observationReport
            ?? new CanonicalLibraryMetadataRealCanaryObservationReport(
                CanonicalLibraryMetadataRealCanaryObservationStatus.disabled,
                CanonicalLibraryMetadataRealCanaryRecommendation.remainDisabled, configuration,
                reason: "defaultDisabled");
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataProductionCanaryInjectionResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataProductionCanaryInjectionResult? other) =>
        other is not null &&
        EqualityComparer<CanonicalLibraryMetadataProductionCanaryConfiguration>.Default.Equals(Configuration, other.Configuration) &&
        InjectionConfigured == other.InjectionConfigured && ExecutorInjected == other.ExecutorInjected &&
        ApplyPortInjected == other.ApplyPortInjected && Armed == other.Armed &&
        Executed == other.Executed && Succeeded == other.Succeeded &&
        Blockers.SequenceEqual(other.Blockers) && Diagnostics.SequenceEqual(other.Diagnostics) &&
        EqualityComparer<CanonicalLibraryMetadataCanaryResult?>.Default.Equals(CanaryResult, other.CanaryResult) &&
        EqualityComparer<CanonicalLibraryMetadataCutoverResult?>.Default.Equals(CutoverResult, other.CutoverResult) &&
        EqualityComparer<CanonicalLibraryMetadataCanarySelectionResult?>.Default.Equals(Selection, other.Selection) &&
        (CandidateSafetyReports?.SequenceEqual(other.CandidateSafetyReports ?? new List<CanonicalLibraryMetadataCanaryCandidateSafety>()) ?? (other.CandidateSafetyReports?.Count == 0)) &&
        EqualityComparer<CanonicalLibraryMetadataProductionRootGateResult?>.Default.Equals(ProductionRootGate, other.ProductionRootGate) &&
        EqualityComparer<CanonicalLibraryMetadataProductionRootSafetyProof?>.Default.Equals(ProductionRootSafetyProof, other.ProductionRootSafetyProof) &&
        EqualityComparer<CanonicalLibraryMetadataRealCanaryObservationReport>.Default.Equals(ObservationReport, other.ObservationReport);
    public override int GetHashCode() =>
        HashCode.Combine(Configuration, InjectionConfigured, ExecutorInjected, ApplyPortInjected, Armed, Executed,
            Succeeded, Blockers.Count, Diagnostics.Count, CanaryResult, CutoverResult, Selection,
            CandidateSafetyReports?.Count ?? 0, ProductionRootGate, ProductionRootSafetyProof, ObservationReport);
    public static bool operator ==(CanonicalLibraryMetadataProductionCanaryInjectionResult left, CanonicalLibraryMetadataProductionCanaryInjectionResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataProductionCanaryInjectionResult left, CanonicalLibraryMetadataProductionCanaryInjectionResult right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataProductionCanaryInjection
{
    public async Task<CanonicalLibraryMetadataProductionCanaryInjectionResult> EvaluateOrRun(
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration,
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
        var executorInjected = executor != null;
        var applyPortInjected = evidence.RealRootBoundApplyPortAvailable && evidence.ApplyPortMode.IsNonDryRunRootBound();
        var diagnostics = new List<CanonicalLibraryMetadataCutoverDiagnostic>
        {
            Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryInjectionConfigured,
                configuration, syncRunID, trigger, nodeRole,
                result: configuration.Mode.IsConfigured() ? "configured" : "disabled",
                reason: $"mode={configuration.Mode};rootMode={configuration.RootMode};budget={configuration.CanaryMaxObjectsPerSyncRun};explicitInternal={configuration.ExplicitInternalDebugConfiguration}")
        };

        if (configuration.Mode == CanonicalLibraryMetadataProductionCanaryMode.disabled)
            return MakeResult(configuration, false, false, false, false, false,
                new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.disabled },
                diagnostics,
                report: Observation(CanonicalLibraryMetadataRealCanaryObservationStatus.disabled, configuration,
                    null, new List<CanonicalLibraryMetadataCanaryCandidateSafety>(), null, evidence,
                    new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.disabled },
                    "defaultDisabled"));

        if (configuration.Mode == CanonicalLibraryMetadataProductionCanaryMode.diagnosticsOnly)
            return MakeResult(configuration, executorInjected, applyPortInjected, false, false, false,
                new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.diagnosticsOnlyNoExecution },
                diagnostics,
                selection: null, candidateSafetyReports: new List<CanonicalLibraryMetadataCanaryCandidateSafety>(),
                report: Observation(CanonicalLibraryMetadataRealCanaryObservationStatus.diagnosticsOnly, configuration,
                    null, new List<CanonicalLibraryMetadataCanaryCandidateSafety>(), null, evidence,
                    new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.diagnosticsOnlyNoExecution },
                    "diagnosticsOnlyNoExecution"));

        var productionRootGate = configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit
            ? new CanonicalLibraryMetadataProductionRootGate().Evaluate(configuration, token, evidence, matrix,
                candidates, trigger, localSnapshotAvailable, peerSnapshotAvailable, executorInjected)
            : null;

        if (productionRootGate != null)
        {
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootGateEvaluated,
                configuration, syncRunID, trigger, nodeRole,
                domain: productionRootGate.SelectedCandidate?.Domain,
                objectID: productionRootGate.SelectedCandidate?.ObjectID,
                objectKind: productionRootGate.SelectedCandidate?.ObjectKind,
                action: productionRootGate.SelectedCandidate?.ActionKind.ToString(),
                result: productionRootGate.Allowed ? "allowed" : "blocked",
                reason: productionRootGate.DiagnosticsSummary));
            diagnostics.Add(Diagnostic(
                productionRootGate.Allowed
                    ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootGateAllowed
                    : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootGateBlocked,
                configuration, syncRunID, trigger, nodeRole,
                domain: productionRootGate.SelectedCandidate?.Domain,
                objectID: productionRootGate.SelectedCandidate?.ObjectID,
                objectKind: productionRootGate.SelectedCandidate?.ObjectKind,
                action: productionRootGate.SelectedCandidate?.ActionKind.ToString(),
                result: productionRootGate.Allowed ? "allowed" : "blocked",
                reason: productionRootGate.Allowed ? "productionRootExplicitGateAllowed"
                    : string.Join(",", productionRootGate.Blockers.Select(b => b.ToString()))));
        }

        var strictBlks = StrictBlockers(configuration, token, evidence, matrix, trigger,
            localSnapshotAvailable, peerSnapshotAvailable, executorInjected);
        var allBlockers = new HashSet<CanonicalLibraryMetadataRealCanaryBlocker>(strictBlks);
        if (productionRootGate?.Blockers != null)
            allBlockers.UnionWith(productionRootGate.Blockers.Select(RealCanaryBlocker));
        var blockers = allBlockers.OrderBy(b => b.ToString()).ToList();

        var selection = Selection(evidence, candidates, trigger);
        var safetyReports = candidates.Select(c => new CanonicalLibraryMetadataCanaryCandidateSafety(c, evidence)).ToList();
        var unsafeSkipped = safetyReports.Any(s => !s.Safe);

        if (configuration.Mode == CanonicalLibraryMetadataProductionCanaryMode.canaryN1Armed && blockers.Count == 0)
        {
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryArmed,
                configuration, syncRunID, trigger, nodeRole, result: "armed", reason: "commitSuppressed=true"));
            return MakeResult(configuration, executorInjected, applyPortInjected, true, false, false,
                new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.armedNoExecution },
                diagnostics,
                selection: selection, candidateSafetyReports: safetyReports, productionRootGate: productionRootGate,
                report: Observation(CanonicalLibraryMetadataRealCanaryObservationStatus.armed, configuration,
                    selection, safetyReports, null, evidence,
                    new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.armedNoExecution },
                    "armedNoExecution"));
        }

        if (configuration.Mode == CanonicalLibraryMetadataProductionCanaryMode.canaryN1Armed ||
            configuration.Mode == CanonicalLibraryMetadataProductionCanaryMode.blocked || blockers.Count > 0)
        {
            var effectiveBlockers = configuration.Mode == CanonicalLibraryMetadataProductionCanaryMode.canaryN1Armed && blockers.Count == 0
                ? new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.armedNoExecution }
                : blockers;
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryBlocked,
                configuration, syncRunID, trigger, nodeRole, result: "blocked",
                reason: string.Join(",", effectiveBlockers.Select(b => b.ToString()))));
            return MakeResult(configuration, executorInjected, applyPortInjected, false, false, false,
                effectiveBlockers, diagnostics,
                selection: selection, candidateSafetyReports: safetyReports, productionRootGate: productionRootGate,
                report: Observation(CanonicalLibraryMetadataRealCanaryObservationStatus.blocked, configuration,
                    selection, safetyReports, null, evidence, effectiveBlockers,
                    string.Join(",", effectiveBlockers.Select(b => b.ToString()))));
        }

        // No eligible candidates
        if (selection.SelectedCutoverCandidates.Count == 0)
        {
            var noEligibleBlks = new List<CanonicalLibraryMetadataRealCanaryBlocker>
            {
                unsafeSkipped ? CanonicalLibraryMetadataRealCanaryBlocker.unsafeCandidateSkipped : CanonicalLibraryMetadataRealCanaryBlocker.noEligibleCandidate
            };
            diagnostics.Add(Diagnostic(
                unsafeSkipped ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryUnsafeCandidateSkipped
                    : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryNoEligibleCandidate,
                configuration, syncRunID, trigger, nodeRole, result: "blocked",
                reason: string.Join(",", selection.Blockers.Select(b => b.Reason.ToString()))));
            return MakeResult(configuration, executorInjected, applyPortInjected, true, false, false,
                noEligibleBlks, diagnostics,
                selection: selection, candidateSafetyReports: safetyReports, productionRootGate: productionRootGate,
                report: Observation(
                    unsafeSkipped ? CanonicalLibraryMetadataRealCanaryObservationStatus.unsafeCandidateSkipped
                        : CanonicalLibraryMetadataRealCanaryObservationStatus.noEligibleCandidate,
                    configuration, selection, safetyReports, null, evidence, noEligibleBlks,
                    unsafeSkipped ? "unsafeCandidateSkipped" : "noEligibleCandidate"));
        }

        // Execute
        diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryExecutionStarted,
            configuration, syncRunID, trigger, nodeRole,
            domain: selection.SelectedCutoverCandidates.FirstOrDefault()?.Domain,
            objectID: selection.SelectedCutoverCandidates.FirstOrDefault()?.ObjectID,
            objectKind: selection.SelectedCutoverCandidates.FirstOrDefault()?.ObjectKind,
            action: selection.SelectedCutoverCandidates.FirstOrDefault()?.CutoverActionKind.ToString(),
            result: "started", reason: "strictN1"));

        if (configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit)
        {
            var prodSel = selection.SelectedCutoverCandidates.FirstOrDefault();
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootN1Started,
                configuration, syncRunID, trigger, nodeRole,
                domain: prodSel?.Domain, objectID: prodSel?.ObjectID, objectKind: prodSel?.ObjectKind,
                action: prodSel?.CutoverActionKind.ToString(), result: "started", reason: "explicitProductionRootStrictN1"));
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootCheckpointCreated,
                configuration, syncRunID, trigger, nodeRole,
                domain: prodSel?.Domain, objectID: prodSel?.ObjectID, objectKind: prodSel?.ObjectKind,
                action: prodSel?.CutoverActionKind.ToString(), result: "created", reason: prodSel?.EffectiveRollbackCheckpointID));
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootAtomicWriteStarted,
                configuration, syncRunID, trigger, nodeRole,
                domain: prodSel?.Domain, objectID: prodSel?.ObjectID, objectKind: prodSel?.ObjectKind,
                action: prodSel?.CutoverActionKind.ToString(), result: "started", reason: "rootBoundAtomicMetadataWrite"));
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryProductionRootWriteStarted,
                configuration, syncRunID, trigger, nodeRole, result: "started", reason: "explicitProductionRoot"));
        }

        var canaryResult = await new CanonicalLibraryMetadataN1CanaryRunner().Run(
            configuration.AsN1CanaryConfiguration, configuration.Policy.AsCanaryPolicy, token, evidence, matrix,
            selection.SelectedCutoverCandidates, trigger, nodeRole, syncRunID, localSnapshotAvailable, peerSnapshotAvailable, executor);

        var cutoverResult = canaryResult.CutoverResult;
        var succeeded = canaryResult.Succeeded;

        var safetyProof = productionRootGate != null
            ? new CanonicalLibraryMetadataProductionRootSafetyProof(productionRootGate, cutoverResult, configuration)
            : null;

        diagnostics.AddRange(cutoverResult?.Diagnostics ?? new List<CanonicalLibraryMetadataCutoverDiagnostic>());
        diagnostics.Add(Diagnostic(
            succeeded ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryExecutionCompleted
                : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryExecutionFailed,
            configuration, syncRunID, trigger, nodeRole,
            result: succeeded ? "completed" : "failed",
            reason: succeeded ? "strictN1Success" : "legacyFallbackPreserved"));

        if (configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit && cutoverResult != null)
        {
            if (cutoverResult.Commits.FirstOrDefault() is { Committed: true } commit)
            {
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootAtomicWriteCompleted,
                    configuration, syncRunID, trigger, nodeRole,
                    domain: commit.Domain, objectID: commit.ObjectID, objectKind: commit.ObjectKind,
                    action: commit.ActionKind.ToString(), result: "completed", reason: "rootBoundAtomicMetadataWrite",
                    hashPrefix: commit.MetadataHashPrefix));
            }
            if (cutoverResult.Commits.FirstOrDefault() is { PostconditionVerified: true } pc)
            {
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootPostconditionVerified,
                    configuration, syncRunID, trigger, nodeRole,
                    domain: pc.Domain, objectID: pc.ObjectID, objectKind: pc.ObjectKind,
                    action: pc.ActionKind.ToString(), result: "verified", reason: "postconditionVerified",
                    hashPrefix: pc.MetadataHashPrefix));
            }
            diagnostics.Add(Diagnostic(
                succeeded ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootN1Completed
                    : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootN1Failed,
                configuration, syncRunID, trigger, nodeRole,
                result: succeeded ? "completed" : "failed",
                reason: succeeded ? "strictN1Success" : "rollbackOrFallbackRequired"));
            diagnostics.Add(Diagnostic(
                succeeded ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryProductionRootWriteCompleted
                    : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryProductionRootWriteFailed,
                configuration, syncRunID, trigger, nodeRole,
                result: succeeded ? "completed" : "failed",
                reason: succeeded ? "explicitProductionRoot" : "rollbackOrFallbackRequired"));
            if (safetyProof != null)
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootSafetyProofBuilt,
                    configuration, syncRunID, trigger, nodeRole, result: "built", reason: safetyProof.RedactedTargetSummary));
        }

        if (cutoverResult != null)
        {
            foreach (var rollback in cutoverResult.RollbackResults)
            {
                if (configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit)
                    diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootRollbackStarted,
                        configuration, syncRunID, trigger, nodeRole, result: "started", reason: rollback.CheckpointID));
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryRollbackStarted,
                    configuration, syncRunID, trigger, nodeRole, result: "started", reason: rollback.CheckpointID));
                if (configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit)
                    diagnostics.Add(Diagnostic(
                        rollback.Succeeded ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootRollbackCompleted
                            : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootRollbackFailed,
                        configuration, syncRunID, trigger, nodeRole,
                        result: rollback.Succeeded ? "completed" : "failed", reason: rollback.Reason));
                diagnostics.Add(Diagnostic(
                    rollback.Succeeded ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryRollbackCompleted
                        : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryRollbackFailed,
                    configuration, syncRunID, trigger, nodeRole,
                    result: rollback.Succeeded ? "completed" : "failed", reason: rollback.Reason));
            }

            if (cutoverResult.LegacyFallbackUsed)
            {
                if (configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit)
                    diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootLegacyFallbackUsed,
                        configuration, syncRunID, trigger, nodeRole, result: "used", reason: "commitFailureOrRollback"));
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryLegacyFallbackUsed,
                    configuration, syncRunID, trigger, nodeRole, result: "used", reason: "commitFailureOrRollback"));
            }

            if (cutoverResult.DuplicateLegacySuppressedActionIDs.Count > 0)
            {
                if (configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit)
                    diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootDuplicateSuppressed,
                        configuration, syncRunID, trigger, nodeRole, result: "successOnly", reason: "matchingLegacyLibraryMetadataOnly"));
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryDuplicateLegacySuppressed,
                    configuration, syncRunID, trigger, nodeRole, result: "successOnly", reason: "matchingLegacyLibraryMetadataOnly"));
            }

            var readSideEquivalent = cutoverResult.ReadSideProjection?.Equivalent ?? evidence.ReadSideParallelEquivalent;
            if (configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit)
                diagnostics.Add(Diagnostic(
                    readSideEquivalent ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootReadSideEquivalent
                        : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootReadSideDivergent,
                    configuration, syncRunID, trigger, nodeRole,
                    domain: cutoverResult.ReadSideProjection?.Domain, objectID: cutoverResult.ReadSideProjection?.ObjectID,
                    objectKind: cutoverResult.ReadSideProjection?.ObjectKind,
                    result: readSideEquivalent ? "equivalent" : "divergent",
                    reason: cutoverResult.ReadSideProjection?.Reason ?? "readSideEvidence"));
            diagnostics.Add(Diagnostic(
                readSideEquivalent ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryReadSideEquivalent
                    : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryReadSideDivergent,
                configuration, syncRunID, trigger, nodeRole,
                result: readSideEquivalent ? "equivalent" : "divergent",
                reason: cutoverResult.ReadSideProjection?.Reason ?? "readSideEvidence"));

            if (cutoverResult.FatalBlocker)
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRealCanaryFatalBlocker,
                    configuration, syncRunID, trigger, nodeRole, result: "fatal", reason: "rollbackFailure"));
        }

        var finalReport = Observation(
            succeeded ? CanonicalLibraryMetadataRealCanaryObservationStatus.executedSucceeded
                : (cutoverResult?.FatalBlocker == true ? CanonicalLibraryMetadataRealCanaryObservationStatus.fatalRollbackFailure : CanonicalLibraryMetadataRealCanaryObservationStatus.executedFailedRolledBack),
            configuration, selection, safetyReports, cutoverResult, evidence,
            cutoverResult?.FatalBlocker == true ? new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.fatalBlocker } : new List<CanonicalLibraryMetadataRealCanaryBlocker>(),
            succeeded ? "strictN1Success" : "legacyFallbackPreserved");

        return MakeResult(configuration, executorInjected, applyPortInjected, true,
            cutoverResult?.Commits.Count > 0, succeeded,
            cutoverResult?.FatalBlocker == true ? new List<CanonicalLibraryMetadataRealCanaryBlocker> { CanonicalLibraryMetadataRealCanaryBlocker.fatalBlocker } : new List<CanonicalLibraryMetadataRealCanaryBlocker>(),
            diagnostics,
            canaryResult: canaryResult, cutoverResult: cutoverResult,
            selection: selection, candidateSafetyReports: safetyReports,
            productionRootGate: productionRootGate, productionRootSafetyProof: safetyProof,
            report: finalReport);
    }

    private List<CanonicalLibraryMetadataRealCanaryBlocker> StrictBlockers(
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration,
        CanonicalCutoverToken? token,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        CanonicalMigrationDomainMatrix matrix,
        CanonicalSyncPlanTrigger trigger,
        bool localSnapshotAvailable,
        bool peerSnapshotAvailable,
        bool executorAvailable)
    {
        var blockers = new List<CanonicalLibraryMetadataRealCanaryBlocker>();
        if (configuration.Mode == CanonicalLibraryMetadataProductionCanaryMode.blocked) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.blockedMode);
        if (configuration.Mode != CanonicalLibraryMetadataProductionCanaryMode.canaryN1Execute && configuration.Mode != CanonicalLibraryMetadataProductionCanaryMode.canaryN1Armed)
            blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.blockedMode);
        if (configuration.Policy.Domain != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.nonLibraryMetadataDomain);
        if (configuration.Policy.CanaryMaxObjectsPerSyncRun != 1)
            blockers.Add(configuration.Policy.CanaryMaxObjectsPerSyncRun > 1 ? CanonicalLibraryMetadataRealCanaryBlocker.canaryBudgetAboveOneDenied : CanonicalLibraryMetadataRealCanaryBlocker.n1BudgetRequired);
        if (configuration.Policy.RuntimeSwitchEnabled) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.runtimeSwitchDenied);
        if (configuration.Policy.AllowAllEligible) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.allEligibleDenied);
        if (configuration.Policy.ReleaseDefaultEnabled) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.releaseDefaultDenied);
        if (configuration.AllowProductionRootWrites && configuration.RootMode != CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit)
            blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.allowProductionRootWritesDeniedV830);
        if (configuration.Policy.RequiresExplicitInternalDebugConfiguration && !configuration.ExplicitInternalDebugConfiguration)
            blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingExplicitInternalDebugConfiguration);
        var matrixReport = matrix.Validate();
        if (!matrixReport.Allowed) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.matrixValidationBlocked);
        if (matrixReport.ActivePilotDomain != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.activePilotNotLibraryMetadata);
        if (configuration.Policy.RequiresProductionToken && token == null) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingToken);
        if (configuration.Policy.RequiresOwnerApproval && token?.OwnerApproved != true) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingOwnerApproval);
        if (!localSnapshotAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.localSnapshotUnavailable);
        if (!peerSnapshotAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.peerSnapshotUnavailable);
        if (!executorAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.commitExecutorUnavailable);
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh || trigger == CanonicalSyncPlanTrigger.retryDrainer) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.unsupportedTrigger);
        if (!evidence.NoCommitEvidenceAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingNoCommitEvidence);
        if (!evidence.RealDataShadowCopyVerified) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingRealDataShadowCopyEvidence);
        if (!evidence.ExecutionShadowVerified) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingExecutionShadowEvidence);
        if (!evidence.DryRunEquivalenceVerified) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingDryRunEquivalence);
        if (!evidence.NoBlockingDivergence) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.blockingDivergence);
        if (!evidence.NoUnresolvedConflict) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.unresolvedConflict);
        if (!evidence.MetadataManifestRouteEvidenceAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingMetadataManifestRouteEvidence);
        if (!evidence.ProductionPortAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.productionPortUnavailable);
        if (!evidence.RealRootBoundApplyPortAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.realApplyPortUnavailable);
        if (!evidence.ApplyPortMode.IsNonDryRunRootBound()) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.applyPortDryRunOnly);
        if (!evidence.RootBoundWriteAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.rootBoundWriteUnavailable);
        if (!evidence.AtomicReplaceAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.atomicReplaceUnavailable);
        if (!evidence.RollbackCheckpointAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.rollbackCheckpointUnavailable);
        if (configuration.Policy.RequiresRollbackPlan && evidence.RollbackPlan == null) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.rollbackPlanMissing);
        if (!evidence.RollbackVerified || !evidence.RollbackRehearsalPassed) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.rollbackVerificationMissing);
        if (!configuration.Policy.ProductionRootDisabledByDefault || !evidence.ProductionRootDisabledByDefault)
            blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.productionRootGuardMissing);
        switch (configuration.RootMode)
        {
            case CanonicalLibraryMetadataProductionCanaryRootMode.disabled:
                blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.productionRootNotExplicit); break;
            case CanonicalLibraryMetadataProductionCanaryRootMode.testRoot:
                if (evidence.ApplyPortMode != CanonicalLibraryMetadataApplyPortMode.testRootBound || !evidence.TestRootUsed)
                    blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.testRootMissing); break;
            case CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit:
                if (!configuration.AllowProductionRootWrites) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.productionRootWritesDisabled);
                if (evidence.ApplyPortMode != CanonicalLibraryMetadataApplyPortMode.productionRootBound)
                    blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.productionRootGuardMissing); break;
        }
        if (!evidence.LegacyFallbackAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.legacyFallbackUnavailable);
        if (configuration.Policy.RequiresReadSideParallelEquivalent && !evidence.ReadSideParallelEquivalent)
            blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.readSideParallelDivergent);

        return new HashSet<CanonicalLibraryMetadataRealCanaryBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
    }

    private CanonicalLibraryMetadataRealCanaryBlocker RealCanaryBlocker(CanonicalLibraryMetadataProductionRootBlocker blocker) => blocker switch
    {
        CanonicalLibraryMetadataProductionRootBlocker.missingExplicitDebugInternalConfiguration => CanonicalLibraryMetadataRealCanaryBlocker.missingExplicitInternalDebugConfiguration,
        CanonicalLibraryMetadataProductionRootBlocker.modeNotExecuteN1Canary => CanonicalLibraryMetadataRealCanaryBlocker.blockedMode,
        CanonicalLibraryMetadataProductionRootBlocker.rootModeNotProductionRootExplicit => CanonicalLibraryMetadataRealCanaryBlocker.productionRootNotExplicit,
        CanonicalLibraryMetadataProductionRootBlocker.allowProductionRootWritesFalse => CanonicalLibraryMetadataRealCanaryBlocker.productionRootWritesDisabled,
        CanonicalLibraryMetadataProductionRootBlocker.missingOwnerApproval => CanonicalLibraryMetadataRealCanaryBlocker.missingOwnerApproval,
        CanonicalLibraryMetadataProductionRootBlocker.activePilotNotLibraryMetadata or CanonicalLibraryMetadataProductionRootBlocker.multipleActivePilots => CanonicalLibraryMetadataRealCanaryBlocker.activePilotNotLibraryMetadata,
        CanonicalLibraryMetadataProductionRootBlocker.runtimeSwitchEnabled => CanonicalLibraryMetadataRealCanaryBlocker.runtimeSwitchDenied,
        CanonicalLibraryMetadataProductionRootBlocker.landingFreezeNotGreen => CanonicalLibraryMetadataRealCanaryBlocker.matrixValidationBlocked,
        CanonicalLibraryMetadataProductionRootBlocker.diagnosticsOnlyEvidenceMissing => CanonicalLibraryMetadataRealCanaryBlocker.missingNoCommitEvidence,
        CanonicalLibraryMetadataProductionRootBlocker.armN1EvidenceMissing => CanonicalLibraryMetadataRealCanaryBlocker.missingExecutionShadowEvidence,
        CanonicalLibraryMetadataProductionRootBlocker.testRootExecuteEvidenceMissing => CanonicalLibraryMetadataRealCanaryBlocker.testRootMissing,
        CanonicalLibraryMetadataProductionRootBlocker.readSideDivergenceNonZero => CanonicalLibraryMetadataRealCanaryBlocker.readSideParallelDivergent,
        CanonicalLibraryMetadataProductionRootBlocker.rollbackEvidenceMissing => CanonicalLibraryMetadataRealCanaryBlocker.rollbackVerificationMissing,
        CanonicalLibraryMetadataProductionRootBlocker.legacyFallbackUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.legacyFallbackUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.safeCandidateMissing => CanonicalLibraryMetadataRealCanaryBlocker.noEligibleCandidate,
        CanonicalLibraryMetadataProductionRootBlocker.multipleSafeCandidatesDenied or CanonicalLibraryMetadataProductionRootBlocker.n1BudgetRequired => CanonicalLibraryMetadataRealCanaryBlocker.canaryBudgetAboveOneDenied,
        CanonicalLibraryMetadataProductionRootBlocker.unsafeCandidateSelected or CanonicalLibraryMetadataProductionRootBlocker.noResourceMoveGuardMissing or CanonicalLibraryMetadataProductionRootBlocker.resourceMoveAttempted or CanonicalLibraryMetadataProductionRootBlocker.noContentWriteGuardMissing or CanonicalLibraryMetadataProductionRootBlocker.contentWriteAttempted or CanonicalLibraryMetadataProductionRootBlocker.tombstoneDeleteAttempted => CanonicalLibraryMetadataRealCanaryBlocker.unsafeCandidateSkipped,
        CanonicalLibraryMetadataProductionRootBlocker.productionRootContainmentUnverified => CanonicalLibraryMetadataRealCanaryBlocker.productionRootGuardMissing,
        CanonicalLibraryMetadataProductionRootBlocker.checkpointUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.rollbackCheckpointUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.postconditionVerificationUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.atomicReplaceUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.allEligibleDenied => CanonicalLibraryMetadataRealCanaryBlocker.allEligibleDenied,
        CanonicalLibraryMetadataProductionRootBlocker.nonLibraryMetadataDomain => CanonicalLibraryMetadataRealCanaryBlocker.nonLibraryMetadataDomain,
        CanonicalLibraryMetadataProductionRootBlocker.releaseDefaultDenied => CanonicalLibraryMetadataRealCanaryBlocker.releaseDefaultDenied,
        CanonicalLibraryMetadataProductionRootBlocker.defaultEnablementDenied => CanonicalLibraryMetadataRealCanaryBlocker.defaultEnablementDenied,
        CanonicalLibraryMetadataProductionRootBlocker.localSnapshotUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.localSnapshotUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.peerSnapshotUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.peerSnapshotUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.commitExecutorUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.commitExecutorUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.unsupportedTrigger => CanonicalLibraryMetadataRealCanaryBlocker.unsupportedTrigger,
        CanonicalLibraryMetadataProductionRootBlocker.productionPortUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.productionPortUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.realApplyPortUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.realApplyPortUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.rootBoundWriteUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.rootBoundWriteUnavailable,
        CanonicalLibraryMetadataProductionRootBlocker.atomicWriteUnavailable => CanonicalLibraryMetadataRealCanaryBlocker.atomicReplaceUnavailable,
        _ => CanonicalLibraryMetadataRealCanaryBlocker.blockedMode
    };

    private CanonicalLibraryMetadataCanarySelectionResult Selection(
        CanonicalLibraryMetadataCutoverEvidence evidence,
        List<CanonicalLibraryMetadataCutoverCandidate> candidates,
        CanonicalSyncPlanTrigger trigger) =>
        new CanonicalLibraryMetadataCanarySelector().Select(
            CanonicalCutoverMode.canary,
            new CanonicalLibraryMetadataCanaryPolicy(canaryMaxObjectsPerSyncRun: 1, allowsInternalN1Execution: true, explicitInternalTestConfiguration: true),
            trigger, evidence, candidates);

    private CanonicalLibraryMetadataRealCanaryObservationReport Observation(
        CanonicalLibraryMetadataRealCanaryObservationStatus status,
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration,
        CanonicalLibraryMetadataCanarySelectionResult? selection,
        List<CanonicalLibraryMetadataCanaryCandidateSafety> safetyReports,
        CanonicalLibraryMetadataCutoverResult? cutoverResult,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        List<CanonicalLibraryMetadataRealCanaryBlocker> blockers,
        string reason)
    {
        var successfulCommitCount = cutoverResult?.Commits.Count(c => c.Committed && c.PreconditionVerified && c.PostconditionVerified) ?? 0;
        var failedCommitCount = (cutoverResult?.Commits.Count ?? 0) - successfulCommitCount;
        var rollbackFailureCount = cutoverResult?.RollbackResults.Count(r => !r.Succeeded || r.Fatal) ?? 0;
        var noEligible = status == CanonicalLibraryMetadataRealCanaryObservationStatus.noEligibleCandidate || (selection?.SelectedCandidates.Count == 0);
        var unsafeSkipped = safetyReports.Count(s => !s.Safe);
        var fatal = cutoverResult?.FatalBlocker == true || status == CanonicalLibraryMetadataRealCanaryObservationStatus.fatalRollbackFailure;
        var duplicateSuppressionCount = cutoverResult?.DuplicateLegacySuppressedActionIDs.Count ?? 0;

        return new CanonicalLibraryMetadataRealCanaryObservationReport(
            status, Recommendation(status, blockers, successfulCommitCount, failedCommitCount, rollbackFailureCount,
                cutoverResult?.ReadSideProjection?.Equivalent ?? evidence.ReadSideParallelEquivalent),
            configuration,
            selectedCandidateCount: selection?.SelectedCandidates.Count ?? 0,
            executedCandidateCount: cutoverResult?.Commits.Count ?? 0,
            successfulCommitCount: successfulCommitCount,
            failedCommitCount: failedCommitCount,
            rollbackCount: cutoverResult?.RollbackResults.Count ?? 0,
            rollbackFailureCount: rollbackFailureCount,
            legacyFallbackCount: (cutoverResult?.LegacyFallbackUsed == true || blockers.Count > 0) ? 1 : 0,
            duplicateSuppressionCount: duplicateSuppressionCount,
            noEligibleCandidateCount: noEligible ? 1 : 0,
            unsafeCandidateSkippedCount: unsafeSkipped,
            fatalBlockerCount: fatal ? 1 : 0,
            readSideParallelEquivalent: cutoverResult?.ReadSideProjection?.Equivalent ?? evidence.ReadSideParallelEquivalent,
            readSideParallelDivergent: (cutoverResult?.ReadSideProjection?.Equivalent == false) || !evidence.ReadSideParallelEquivalent,
            legacyFallbackPreserved: cutoverResult?.LegacyFallbackUsed == true || successfulCommitCount == 0 || blockers.Count > 0,
            duplicateSuppressionApplied: duplicateSuppressionCount > 0,
            productionRootWriteAttempted: configuration.RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit && (cutoverResult?.Commits.Count > 0),
            blockers: blockers, reason: reason);
    }

    private CanonicalLibraryMetadataRealCanaryRecommendation Recommendation(
        CanonicalLibraryMetadataRealCanaryObservationStatus status,
        List<CanonicalLibraryMetadataRealCanaryBlocker> blockers,
        int successfulCommitCount, int failedCommitCount, int rollbackFailureCount, bool readSideEquivalent)
    {
        if (status == CanonicalLibraryMetadataRealCanaryObservationStatus.disabled || status == CanonicalLibraryMetadataRealCanaryObservationStatus.diagnosticsOnly)
            return CanonicalLibraryMetadataRealCanaryRecommendation.remainDisabled;
        if (blockers.Count > 0 || failedCommitCount > 0 || rollbackFailureCount > 0 || !readSideEquivalent)
            return CanonicalLibraryMetadataRealCanaryRecommendation.fixBlockers;
        return CanonicalLibraryMetadataRealCanaryRecommendation.stayN1;
    }

    private CanonicalLibraryMetadataProductionCanaryInjectionResult MakeResult(
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration,
        bool executorInjected, bool applyPortInjected,
        bool armed, bool executed, bool succeeded,
        List<CanonicalLibraryMetadataRealCanaryBlocker> blockers,
        List<CanonicalLibraryMetadataCutoverDiagnostic> diagnostics,
        CanonicalLibraryMetadataCanaryResult? canaryResult = null,
        CanonicalLibraryMetadataCutoverResult? cutoverResult = null,
        CanonicalLibraryMetadataCanarySelectionResult? selection = null,
        List<CanonicalLibraryMetadataCanaryCandidateSafety>? candidateSafetyReports = null,
        CanonicalLibraryMetadataProductionRootGateResult? productionRootGate = null,
        CanonicalLibraryMetadataProductionRootSafetyProof? productionRootSafetyProof = null,
        CanonicalLibraryMetadataRealCanaryObservationReport? report = null) =>
        new(configuration, configuration.Mode.IsConfigured(), executorInjected, applyPortInjected,
            armed, executed, succeeded, blockers, diagnostics,
            canaryResult, cutoverResult, selection, candidateSafetyReports,
            productionRootGate, productionRootSafetyProof, report);

    private CanonicalLibraryMetadataCutoverDiagnostic Diagnostic(
        CanonicalLibraryMetadataCutoverDiagnosticKind kind,
        CanonicalLibraryMetadataProductionCanaryConfiguration configuration,
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalLibraryMetadataCutoverDomain? domain = null,
        string? objectID = null,
        CanonicalObjectKind? objectKind = null,
        string? action = null,
        string? result = null,
        string? reason = null,
        CanonicalHash? hash = null,
        string? hashPrefix = null) =>
        new(kind, syncRunID, trigger, nodeRole, domain, objectID, objectKind, action, result,
            string.Join(";", new List<string?> { reason, $"mode={configuration.Mode}", $"rootMode={configuration.RootMode}" }.Where(r => r != null)),
            hash ?? (hashPrefix != null ? new CanonicalHash(hashPrefix) : null));
}
