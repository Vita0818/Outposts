using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationLandingFreezeViolation
{
    noLibraryMetadataActivePilot,
    multipleActivePilots,
    nonLibraryMetadataActivePilot,
    nonLibraryMetadataDomainNotStaticOnly,
    generatedArtifactsNotStaticOnly,
    tombstoneConflictNotStaticOnly,
    audioUploadNotStaticOnly,
    recordingMetadataActive,
    defaultCutoverEnabled,
    releaseDefaultEnabled,
    runtimeSwitchEnabled,
    legacyDuplicateSuppressionEnabled,
    readPathNotLegacy,
    productionInjectionPresent,
    productionExecutorInjectedByDefault,
    productionRootWriteEnabledByDefault,
    legacyFallbackUnavailable,
    canaryBudgetAboveOneDenied,
    allEligibleEnabled,
    unsafeCandidateAllowed,
    resourceMoveAllowed,
    contentWriteAllowed,
    tombstoneDeleteAllowed,
}

public sealed class CanonicalMigrationLandingFreezeResult : IEquatable<CanonicalMigrationLandingFreezeResult>
{
    public bool Allowed { get; set; }
    public CanonicalMigrationDomain? ActivePilotDomain { get; set; }
    public List<CanonicalMigrationLandingFreezeViolation> Violations { get; set; }
    public bool OtherDomainsStaticOnly { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public string DiagnosticsSummary { get; set; }
    public bool Redacted { get; set; }

    public CanonicalMigrationLandingFreezeResult(
        bool allowed = false,
        CanonicalMigrationDomain? activePilotDomain = null,
        List<CanonicalMigrationLandingFreezeViolation>? violations = null,
        bool otherDomainsStaticOnly = false,
        bool runtimeSwitchEnabled = false,
        string diagnosticsSummary = "",
        bool redacted = true)
    {
        Allowed = allowed;
        ActivePilotDomain = activePilotDomain;
        Violations = violations ?? new List<CanonicalMigrationLandingFreezeViolation>();
        OtherDomainsStaticOnly = otherDomainsStaticOnly;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        DiagnosticsSummary = diagnosticsSummary;
        Redacted = redacted;
    }

    public override bool Equals(object? obj) => obj is CanonicalMigrationLandingFreezeResult other && Equals(other);
    public bool Equals(CanonicalMigrationLandingFreezeResult? other) =>
        other is not null && Allowed == other.Allowed && ActivePilotDomain == other.ActivePilotDomain &&
        Violations.SequenceEqual(other.Violations) && OtherDomainsStaticOnly == other.OtherDomainsStaticOnly &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled && DiagnosticsSummary == other.DiagnosticsSummary &&
        Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(Allowed, ActivePilotDomain, Violations.Count, OtherDomainsStaticOnly, RuntimeSwitchEnabled, DiagnosticsSummary, Redacted);
    public static bool operator ==(CanonicalMigrationLandingFreezeResult left, CanonicalMigrationLandingFreezeResult right) => left.Equals(right);
    public static bool operator !=(CanonicalMigrationLandingFreezeResult left, CanonicalMigrationLandingFreezeResult right) => !left.Equals(right);
}

public class CanonicalMigrationLandingFreeze
{
    public CanonicalMigrationLandingFreeze() { }

    public CanonicalMigrationLandingFreezeResult Evaluate(
        CanonicalMigrationDomainMatrix matrix,
        bool releaseDefaultEnabled = false,
        bool runtimeSwitchEnabled = false,
        bool productionInjectionPresent = false,
        bool productionExecutorInjectedByDefault = false,
        bool productionRootWriteEnabledByDefault = false,
        bool legacyFallbackAvailable = true,
        int canaryMaxObjectsPerSyncRun = 1,
        bool allEligibleEnabled = false,
        bool unsafeCandidateAllowed = false,
        bool resourceMoveAllowed = false,
        bool contentWriteAllowed = false,
        bool tombstoneDeleteAllowed = false)
    {
        var violations = new List<CanonicalMigrationLandingFreezeViolation>();
        var activePolicies = matrix.Policies.Where(p => p.ActivePilot).ToList();
        CanonicalMigrationDomain? activePilotDomain = activePolicies.Count == 1 ? activePolicies[0].Domain : null;

        if (activePolicies.Count == 0)
            violations.Add(CanonicalMigrationLandingFreezeViolation.noLibraryMetadataActivePilot);
        if (activePolicies.Count > 1)
            violations.Add(CanonicalMigrationLandingFreezeViolation.multipleActivePilots);
        if (activePolicies.Any(p => p.Domain != CanonicalMigrationDomain.libraryMetadata))
            violations.Add(CanonicalMigrationLandingFreezeViolation.nonLibraryMetadataActivePilot);

        var nonLibraryPolicies = matrix.Policies.Where(p => p.Domain != CanonicalMigrationDomain.libraryMetadata).ToList();
        if (nonLibraryPolicies.Any(p => !p.StaticOnly || !p.BlockedForRealMigration))
            violations.Add(CanonicalMigrationLandingFreezeViolation.nonLibraryMetadataDomainNotStaticOnly);
        if (matrix.PolicyFor(CanonicalMigrationDomain.generatedArtifacts) is { } ga && (!ga.StaticOnly || ga.ActivePilot))
            violations.Add(CanonicalMigrationLandingFreezeViolation.generatedArtifactsNotStaticOnly);
        if (matrix.PolicyFor(CanonicalMigrationDomain.tombstoneConflict) is { } tc && (!tc.StaticOnly || tc.ActivePilot))
            violations.Add(CanonicalMigrationLandingFreezeViolation.tombstoneConflictNotStaticOnly);
        if (matrix.PolicyFor(CanonicalMigrationDomain.audioUpload) is { } au && (!au.StaticOnly || au.ActivePilot))
            violations.Add(CanonicalMigrationLandingFreezeViolation.audioUploadNotStaticOnly);
        if (matrix.PolicyFor(CanonicalMigrationDomain.recordingMetadata) is { } rm && (rm.ActivePilot || !rm.StaticOnly))
            violations.Add(CanonicalMigrationLandingFreezeViolation.recordingMetadataActive);
        if (matrix.Policies.Any(p => p.DefaultCutoverEnabled))
            violations.Add(CanonicalMigrationLandingFreezeViolation.defaultCutoverEnabled);
        if (matrix.Policies.Any(p => p.ReleaseDefaultEnabledCutover) || releaseDefaultEnabled)
            violations.Add(CanonicalMigrationLandingFreezeViolation.releaseDefaultEnabled);
        if (matrix.Policies.Any(p => p.RuntimeSwitchEnabled) || runtimeSwitchEnabled)
            violations.Add(CanonicalMigrationLandingFreezeViolation.runtimeSwitchEnabled);
        if (matrix.Policies.Any(p => p.LegacySuppressionAllowed))
            violations.Add(CanonicalMigrationLandingFreezeViolation.legacyDuplicateSuppressionEnabled);
        if (matrix.Policies.Any(p => !p.ReadPathLegacy))
            violations.Add(CanonicalMigrationLandingFreezeViolation.readPathNotLegacy);
        if (matrix.Policies.Any(p => !p.NoProductionInjection && p.Domain != CanonicalMigrationDomain.libraryMetadata) || productionInjectionPresent)
            violations.Add(CanonicalMigrationLandingFreezeViolation.productionInjectionPresent);
        if (productionExecutorInjectedByDefault || productionInjectionPresent)
            violations.Add(CanonicalMigrationLandingFreezeViolation.productionExecutorInjectedByDefault);
        if (productionRootWriteEnabledByDefault)
            violations.Add(CanonicalMigrationLandingFreezeViolation.productionRootWriteEnabledByDefault);
        if (!legacyFallbackAvailable)
            violations.Add(CanonicalMigrationLandingFreezeViolation.legacyFallbackUnavailable);
        if (canaryMaxObjectsPerSyncRun > 1)
            violations.Add(CanonicalMigrationLandingFreezeViolation.canaryBudgetAboveOneDenied);
        if (allEligibleEnabled)
            violations.Add(CanonicalMigrationLandingFreezeViolation.allEligibleEnabled);
        if (unsafeCandidateAllowed)
            violations.Add(CanonicalMigrationLandingFreezeViolation.unsafeCandidateAllowed);
        if (resourceMoveAllowed)
            violations.Add(CanonicalMigrationLandingFreezeViolation.resourceMoveAllowed);
        if (contentWriteAllowed)
            violations.Add(CanonicalMigrationLandingFreezeViolation.contentWriteAllowed);
        if (tombstoneDeleteAllowed)
            violations.Add(CanonicalMigrationLandingFreezeViolation.tombstoneDeleteAllowed);

        var uniqueViolations = new HashSet<CanonicalMigrationLandingFreezeViolation>(violations)
            .OrderBy(v => v.ToString()).ToList();

        var otherDomainsStaticOnly = nonLibraryPolicies.All(p =>
            p.StaticOnly && p.BlockedForRealMigration && !p.ActivePilot &&
            !p.DefaultCutoverEnabled && !p.ReleaseDefaultEnabledCutover &&
            !p.RuntimeSwitchEnabled && !p.LegacySuppressionAllowed && p.ReadPathLegacy);

        var runtimeEnabled = matrix.Policies.Any(p => p.RuntimeSwitchEnabled) || runtimeSwitchEnabled;

        return new CanonicalMigrationLandingFreezeResult(
            allowed: uniqueViolations.Count == 0,
            activePilotDomain: activePilotDomain,
            violations: uniqueViolations,
            otherDomainsStaticOnly: otherDomainsStaticOnly,
            runtimeSwitchEnabled: runtimeEnabled,
            diagnosticsSummary: string.Join(",",
                $"activePilot={activePilotDomain?.ToString() ?? "none"}",
                $"otherDomainsStaticOnly={otherDomainsStaticOnly}",
                $"runtimeSwitch={runtimeEnabled}",
                $"legacyFallbackAvailable={legacyFallbackAvailable}",
                $"canaryMaxObjectsPerSyncRun={canaryMaxObjectsPerSyncRun}",
                $"violations={string.Join("|", uniqueViolations.Select(v => v.ToString()))}",
                "redacted=true"),
            redacted: true);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataDebugPilotMode
{
    disabled,
    diagnosticsOnly,
    armN1Canary,
    executeN1Canary,
    blocked
}

public static class CanonicalLibraryMetadataDebugPilotModeExtensions
{
    public static bool IsConfigured(this CanonicalLibraryMetadataDebugPilotMode mode) => mode != CanonicalLibraryMetadataDebugPilotMode.disabled;
    public static bool RequestsExecution(this CanonicalLibraryMetadataDebugPilotMode mode) => mode == CanonicalLibraryMetadataDebugPilotMode.executeN1Canary;
}

public sealed class CanonicalLibraryMetadataDebugPilotPolicy : IEquatable<CanonicalLibraryMetadataDebugPilotPolicy>
{
    public CanonicalMigrationDomain Domain { get; set; }
    public int CanaryMaxObjectsPerSyncRun { get; set; }
    public bool RequiresExplicitInternalDebugConfiguration { get; set; }
    public bool RequiresProductionToken { get; set; }
    public bool RequiresOwnerApproval { get; set; }
    public bool RequiresRollbackPlan { get; set; }
    public bool RequiresReadSideParallelEquivalent { get; set; }
    public bool RequiresObservationEvidence { get; set; }
    public bool RequiresRealRootBoundApplyPort { get; set; }
    public bool ProductionRootDisabledByDefault { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool AllowAllEligible { get; set; }
    public bool ReleaseDefaultEnabled { get; set; }

    public CanonicalLibraryMetadataDebugPilotPolicy(
        CanonicalMigrationDomain domain = CanonicalMigrationDomain.libraryMetadata,
        int canaryMaxObjectsPerSyncRun = 1,
        bool requiresExplicitInternalDebugConfiguration = true,
        bool requiresProductionToken = true,
        bool requiresOwnerApproval = true,
        bool requiresRollbackPlan = true,
        bool requiresReadSideParallelEquivalent = true,
        bool requiresObservationEvidence = true,
        bool requiresRealRootBoundApplyPort = true,
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
        RequiresObservationEvidence = requiresObservationEvidence;
        RequiresRealRootBoundApplyPort = requiresRealRootBoundApplyPort;
        ProductionRootDisabledByDefault = productionRootDisabledByDefault;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        AllowAllEligible = allowAllEligible;
        ReleaseDefaultEnabled = releaseDefaultEnabled;
    }

    public static readonly CanonicalLibraryMetadataDebugPilotPolicy StrictLibraryMetadataN1 = new();

    public bool IsStrictLibraryMetadataN1 =>
        Domain == CanonicalMigrationDomain.libraryMetadata &&
        CanaryMaxObjectsPerSyncRun == 1 &&
        RequiresExplicitInternalDebugConfiguration &&
        RequiresProductionToken && RequiresOwnerApproval &&
        RequiresRollbackPlan && RequiresReadSideParallelEquivalent &&
        RequiresObservationEvidence && RequiresRealRootBoundApplyPort &&
        ProductionRootDisabledByDefault && !RuntimeSwitchEnabled &&
        !AllowAllEligible && !ReleaseDefaultEnabled;

    public CanonicalLibraryMetadataProductionCanaryPolicy AsProductionCanaryPolicy =>
        new(Domain, CanaryMaxObjectsPerSyncRun,
            RequiresExplicitInternalDebugConfiguration, RequiresProductionToken,
            RequiresOwnerApproval, RequiresRollbackPlan,
            RequiresReadSideParallelEquivalent, ProductionRootDisabledByDefault,
            RuntimeSwitchEnabled, AllowAllEligible, ReleaseDefaultEnabled);

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataDebugPilotPolicy other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataDebugPilotPolicy? other) =>
        other is not null && Domain == other.Domain &&
        CanaryMaxObjectsPerSyncRun == other.CanaryMaxObjectsPerSyncRun &&
        RequiresExplicitInternalDebugConfiguration == other.RequiresExplicitInternalDebugConfiguration &&
        RequiresProductionToken == other.RequiresProductionToken &&
        RequiresOwnerApproval == other.RequiresOwnerApproval &&
        RequiresRollbackPlan == other.RequiresRollbackPlan &&
        RequiresReadSideParallelEquivalent == other.RequiresReadSideParallelEquivalent &&
        RequiresObservationEvidence == other.RequiresObservationEvidence &&
        RequiresRealRootBoundApplyPort == other.RequiresRealRootBoundApplyPort &&
        ProductionRootDisabledByDefault == other.ProductionRootDisabledByDefault &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        AllowAllEligible == other.AllowAllEligible &&
        ReleaseDefaultEnabled == other.ReleaseDefaultEnabled;
    public override int GetHashCode() =>
        HashCode.Combine(Domain, CanaryMaxObjectsPerSyncRun, RequiresExplicitInternalDebugConfiguration,
            RequiresProductionToken, RequiresOwnerApproval, RequiresRollbackPlan, RequiresReadSideParallelEquivalent,
            RequiresObservationEvidence, RequiresRealRootBoundApplyPort, ProductionRootDisabledByDefault,
            RuntimeSwitchEnabled, AllowAllEligible, ReleaseDefaultEnabled);
    public static bool operator ==(CanonicalLibraryMetadataDebugPilotPolicy left, CanonicalLibraryMetadataDebugPilotPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataDebugPilotPolicy left, CanonicalLibraryMetadataDebugPilotPolicy right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataDebugPilotConfiguration : IEquatable<CanonicalLibraryMetadataDebugPilotConfiguration>
{
    public CanonicalLibraryMetadataDebugPilotMode Mode { get; set; }
    public CanonicalLibraryMetadataProductionCanaryRootMode RootMode { get; set; }
    public CanonicalLibraryMetadataDebugPilotPolicy Policy { get; set; }
    public bool ExplicitInternalDebugConfiguration { get; set; }
    public bool AllowProductionRootWrites { get; set; }
    public CanonicalLibraryMetadataCutoverEvidence Evidence { get; set; }
    public CanonicalCutoverToken? CutoverToken { get; set; }
    public bool RecordDiagnostics { get; set; }
    public int MaxDiagnosticsEvents { get; set; }

    public CanonicalLibraryMetadataDebugPilotConfiguration(
        CanonicalLibraryMetadataDebugPilotMode mode = CanonicalLibraryMetadataDebugPilotMode.disabled,
        CanonicalLibraryMetadataProductionCanaryRootMode rootMode = CanonicalLibraryMetadataProductionCanaryRootMode.disabled,
        CanonicalLibraryMetadataDebugPilotPolicy? policy = null,
        bool explicitInternalDebugConfiguration = false,
        bool allowProductionRootWrites = false,
        CanonicalLibraryMetadataCutoverEvidence? evidence = null,
        CanonicalCutoverToken? cutoverToken = null,
        bool recordDiagnostics = true,
        int maxDiagnosticsEvents = 200)
    {
        Mode = mode;
        RootMode = rootMode;
        Policy = policy ?? CanonicalLibraryMetadataDebugPilotPolicy.StrictLibraryMetadataN1;
        ExplicitInternalDebugConfiguration = explicitInternalDebugConfiguration;
        AllowProductionRootWrites = allowProductionRootWrites;
        Evidence = evidence ?? new CanonicalLibraryMetadataCutoverEvidence();
        CutoverToken = cutoverToken;
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
    }

    public static readonly CanonicalLibraryMetadataDebugPilotConfiguration Disabled = new();

    public static CanonicalLibraryMetadataDebugPilotConfiguration DiagnosticsOnly(CanonicalLibraryMetadataCutoverEvidence? evidence = null) =>
        new(CanonicalLibraryMetadataDebugPilotMode.diagnosticsOnly, explicitInternalDebugConfiguration: true, evidence: evidence ?? new CanonicalLibraryMetadataCutoverEvidence());

    public static CanonicalLibraryMetadataDebugPilotConfiguration ArmTestRootN1(CanonicalCutoverToken token, CanonicalLibraryMetadataCutoverEvidence evidence) =>
        new(CanonicalLibraryMetadataDebugPilotMode.armN1Canary, CanonicalLibraryMetadataProductionCanaryRootMode.testRoot,
            explicitInternalDebugConfiguration: true, evidence: evidence, cutoverToken: token);

    public static CanonicalLibraryMetadataDebugPilotConfiguration ExecuteTestRootN1(CanonicalCutoverToken token, CanonicalLibraryMetadataCutoverEvidence evidence) =>
        new(CanonicalLibraryMetadataDebugPilotMode.executeN1Canary, CanonicalLibraryMetadataProductionCanaryRootMode.testRoot,
            explicitInternalDebugConfiguration: true, evidence: evidence, cutoverToken: token);

    public static CanonicalLibraryMetadataDebugPilotConfiguration ExecuteProductionRootN1(CanonicalCutoverToken token, CanonicalLibraryMetadataCutoverEvidence evidence, bool allowProductionRootWrites) =>
        new(CanonicalLibraryMetadataDebugPilotMode.executeN1Canary, CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit,
            explicitInternalDebugConfiguration: true, allowProductionRootWrites: allowProductionRootWrites, evidence: evidence, cutoverToken: token);

    public CanonicalLibraryMetadataProductionCanaryConfiguration AsProductionCanaryConfiguration
    {
        get
        {
            var productionMode = Mode switch
            {
                CanonicalLibraryMetadataDebugPilotMode.disabled => CanonicalLibraryMetadataProductionCanaryMode.disabled,
                CanonicalLibraryMetadataDebugPilotMode.diagnosticsOnly => CanonicalLibraryMetadataProductionCanaryMode.diagnosticsOnly,
                CanonicalLibraryMetadataDebugPilotMode.armN1Canary => CanonicalLibraryMetadataProductionCanaryMode.canaryN1Armed,
                CanonicalLibraryMetadataDebugPilotMode.executeN1Canary => CanonicalLibraryMetadataProductionCanaryMode.canaryN1Execute,
                CanonicalLibraryMetadataDebugPilotMode.blocked => CanonicalLibraryMetadataProductionCanaryMode.blocked,
                _ => CanonicalLibraryMetadataProductionCanaryMode.disabled
            };
            return new CanonicalLibraryMetadataProductionCanaryConfiguration(
                productionMode, RootMode, Policy.AsProductionCanaryPolicy,
                ExplicitInternalDebugConfiguration, AllowProductionRootWrites);
        }
    }

    public bool IsStrictExecutableN1 =>
        Mode == CanonicalLibraryMetadataDebugPilotMode.executeN1Canary &&
        (RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.testRoot || RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.productionRootExplicit) &&
        ExplicitInternalDebugConfiguration && Policy.IsStrictLibraryMetadataN1 &&
        (RootMode == CanonicalLibraryMetadataProductionCanaryRootMode.testRoot ? !AllowProductionRootWrites : AllowProductionRootWrites);

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataDebugPilotConfiguration other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataDebugPilotConfiguration? other) =>
        other is not null && Mode == other.Mode && RootMode == other.RootMode &&
        EqualityComparer<CanonicalLibraryMetadataDebugPilotPolicy>.Default.Equals(Policy, other.Policy) &&
        ExplicitInternalDebugConfiguration == other.ExplicitInternalDebugConfiguration &&
        AllowProductionRootWrites == other.AllowProductionRootWrites &&
        EqualityComparer<CanonicalLibraryMetadataCutoverEvidence>.Default.Equals(Evidence, other.Evidence) &&
        EqualityComparer<CanonicalCutoverToken?>.Default.Equals(CutoverToken, other.CutoverToken) &&
        RecordDiagnostics == other.RecordDiagnostics && MaxDiagnosticsEvents == other.MaxDiagnosticsEvents;
    public override int GetHashCode() =>
        HashCode.Combine(Mode, RootMode, Policy, ExplicitInternalDebugConfiguration, AllowProductionRootWrites,
            Evidence, CutoverToken, RecordDiagnostics, MaxDiagnosticsEvents);
    public static bool operator ==(CanonicalLibraryMetadataDebugPilotConfiguration left, CanonicalLibraryMetadataDebugPilotConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataDebugPilotConfiguration left, CanonicalLibraryMetadataDebugPilotConfiguration right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataLandingStatus
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
public enum CanonicalLibraryMetadataLandingRecommendation
{
    remainDisabled,
    fixBlockers,
    runAnotherN1,
    considerReadSideCutoverAfterAudit,
}

public sealed class CanonicalLibraryMetadataLandingCandidateSummary : IEquatable<CanonicalLibraryMetadataLandingCandidateSummary>
{
    public bool Selected { get; set; }
    public CanonicalLibraryMetadataCanaryCandidateSafetyKind? Kind { get; set; }
    public CanonicalObjectKind? ObjectKind { get; set; }
    public CanonicalLibraryMetadataCutoverDomain? Domain { get; set; }
    public CanonicalLibraryMetadataCutoverActionKind? ActionKind { get; set; }
    public bool MetadataOnly { get; set; }
    public bool ResourceMoveAttempted { get; set; }
    public bool ContentBytesMutated { get; set; }

    public CanonicalLibraryMetadataLandingCandidateSummary(
        bool selected = false,
        CanonicalLibraryMetadataCanaryCandidateSafetyKind? kind = null,
        CanonicalObjectKind? objectKind = null,
        CanonicalLibraryMetadataCutoverDomain? domain = null,
        CanonicalLibraryMetadataCutoverActionKind? actionKind = null,
        bool metadataOnly = false,
        bool resourceMoveAttempted = false,
        bool contentBytesMutated = false)
    {
        Selected = selected;
        Kind = kind;
        ObjectKind = objectKind;
        Domain = domain;
        ActionKind = actionKind;
        MetadataOnly = metadataOnly;
        ResourceMoveAttempted = resourceMoveAttempted;
        ContentBytesMutated = contentBytesMutated;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataLandingCandidateSummary other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataLandingCandidateSummary? other) =>
        other is not null && Selected == other.Selected && Kind == other.Kind &&
        ObjectKind == other.ObjectKind && Domain == other.Domain && ActionKind == other.ActionKind &&
        MetadataOnly == other.MetadataOnly && ResourceMoveAttempted == other.ResourceMoveAttempted &&
        ContentBytesMutated == other.ContentBytesMutated;
    public override int GetHashCode() =>
        HashCode.Combine(Selected, Kind, ObjectKind, Domain, ActionKind, MetadataOnly, ResourceMoveAttempted, ContentBytesMutated);
    public static bool operator ==(CanonicalLibraryMetadataLandingCandidateSummary left, CanonicalLibraryMetadataLandingCandidateSummary right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataLandingCandidateSummary left, CanonicalLibraryMetadataLandingCandidateSummary right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataLandingReport : IEquatable<CanonicalLibraryMetadataLandingReport>
{
    public CanonicalLibraryMetadataLandingStatus Status { get; set; }
    public CanonicalLibraryMetadataDebugPilotMode Mode { get; set; }
    public CanonicalLibraryMetadataProductionCanaryRootMode RootMode { get; set; }
    public CanonicalMigrationDomain? ActivePilot { get; set; }
    public CanonicalLibraryMetadataLandingCandidateSummary Candidate { get; set; }
    public bool CommitAttempted { get; set; }
    public bool CommitSucceeded { get; set; }
    public bool RollbackAttempted { get; set; }
    public bool RollbackSucceeded { get; set; }
    public bool LegacyFallbackUsed { get; set; }
    public bool DuplicateSuppressed { get; set; }
    public int DuplicateSuppressedCount { get; set; }
    public bool ReadSideEquivalent { get; set; }
    public int ReadSideDivergenceCount { get; set; }
    public bool UiReadPathSwitched { get; set; }
    public bool LegacyReadPathPreserved { get; set; }
    public bool OtherDomainsStaticOnly { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool GeneratedArtifactsStaticOnly { get; set; }
    public bool TombstoneConflictStaticOnly { get; set; }
    public bool AudioUploadStaticOnly { get; set; }
    public bool RecordingMetadataStaticOnly { get; set; }
    public CanonicalLibraryMetadataLandingRecommendation Recommendation { get; set; }
    public List<CanonicalMigrationLandingFreezeViolation> FreezeViolations { get; set; }
    public List<string> Blockers { get; set; }
    public string DiagnosticsSummary { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataLandingReport(
        CanonicalLibraryMetadataLandingStatus status,
        CanonicalLibraryMetadataDebugPilotMode mode = CanonicalLibraryMetadataDebugPilotMode.disabled,
        CanonicalLibraryMetadataProductionCanaryRootMode rootMode = CanonicalLibraryMetadataProductionCanaryRootMode.disabled,
        CanonicalMigrationDomain? activePilot = null,
        CanonicalLibraryMetadataLandingCandidateSummary? candidate = null,
        bool commitAttempted = false,
        bool commitSucceeded = false,
        bool rollbackAttempted = false,
        bool rollbackSucceeded = false,
        bool legacyFallbackUsed = false,
        bool duplicateSuppressed = false,
        int duplicateSuppressedCount = 0,
        bool readSideEquivalent = false,
        int readSideDivergenceCount = 0,
        bool uiReadPathSwitched = false,
        bool legacyReadPathPreserved = true,
        bool otherDomainsStaticOnly = false,
        bool runtimeSwitchEnabled = false,
        bool generatedArtifactsStaticOnly = true,
        bool tombstoneConflictStaticOnly = true,
        bool audioUploadStaticOnly = true,
        bool recordingMetadataStaticOnly = true,
        CanonicalLibraryMetadataLandingRecommendation recommendation = CanonicalLibraryMetadataLandingRecommendation.remainDisabled,
        List<CanonicalMigrationLandingFreezeViolation>? freezeViolations = null,
        List<string>? blockers = null,
        string diagnosticsSummary = "",
        bool redacted = true)
    {
        Status = status;
        Mode = mode;
        RootMode = rootMode;
        ActivePilot = activePilot;
        Candidate = candidate ?? new CanonicalLibraryMetadataLandingCandidateSummary();
        CommitAttempted = commitAttempted;
        CommitSucceeded = commitSucceeded;
        RollbackAttempted = rollbackAttempted;
        RollbackSucceeded = rollbackSucceeded;
        LegacyFallbackUsed = legacyFallbackUsed;
        DuplicateSuppressed = duplicateSuppressed;
        DuplicateSuppressedCount = duplicateSuppressedCount;
        ReadSideEquivalent = readSideEquivalent;
        ReadSideDivergenceCount = readSideDivergenceCount;
        UiReadPathSwitched = uiReadPathSwitched;
        LegacyReadPathPreserved = legacyReadPathPreserved;
        OtherDomainsStaticOnly = otherDomainsStaticOnly;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        GeneratedArtifactsStaticOnly = generatedArtifactsStaticOnly;
        TombstoneConflictStaticOnly = tombstoneConflictStaticOnly;
        AudioUploadStaticOnly = audioUploadStaticOnly;
        RecordingMetadataStaticOnly = recordingMetadataStaticOnly;
        Recommendation = recommendation;
        FreezeViolations = freezeViolations ?? new List<CanonicalMigrationLandingFreezeViolation>();
        Blockers = blockers ?? new List<string>();
        DiagnosticsSummary = diagnosticsSummary;
        Redacted = redacted;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataLandingReport other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataLandingReport? other) =>
        other is not null && Status == other.Status && Mode == other.Mode && RootMode == other.RootMode &&
        ActivePilot == other.ActivePilot &&
        EqualityComparer<CanonicalLibraryMetadataLandingCandidateSummary>.Default.Equals(Candidate, other.Candidate) &&
        CommitAttempted == other.CommitAttempted && CommitSucceeded == other.CommitSucceeded &&
        RollbackAttempted == other.RollbackAttempted && RollbackSucceeded == other.RollbackSucceeded &&
        LegacyFallbackUsed == other.LegacyFallbackUsed && DuplicateSuppressed == other.DuplicateSuppressed &&
        DuplicateSuppressedCount == other.DuplicateSuppressedCount && ReadSideEquivalent == other.ReadSideEquivalent &&
        ReadSideDivergenceCount == other.ReadSideDivergenceCount && UiReadPathSwitched == other.UiReadPathSwitched &&
        LegacyReadPathPreserved == other.LegacyReadPathPreserved &&
        OtherDomainsStaticOnly == other.OtherDomainsStaticOnly && RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        GeneratedArtifactsStaticOnly == other.GeneratedArtifactsStaticOnly &&
        TombstoneConflictStaticOnly == other.TombstoneConflictStaticOnly &&
        AudioUploadStaticOnly == other.AudioUploadStaticOnly && RecordingMetadataStaticOnly == other.RecordingMetadataStaticOnly &&
        Recommendation == other.Recommendation && FreezeViolations.SequenceEqual(other.FreezeViolations) &&
        Blockers.SequenceEqual(other.Blockers) && DiagnosticsSummary == other.DiagnosticsSummary &&
        Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(Status, Mode, RootMode, ActivePilot, Candidate, CommitAttempted, CommitSucceeded,
            RollbackAttempted, RollbackSucceeded, LegacyFallbackUsed, DuplicateSuppressed, DuplicateSuppressedCount,
            ReadSideEquivalent, ReadSideDivergenceCount, UiReadPathSwitched, LegacyReadPathPreserved,
            OtherDomainsStaticOnly, RuntimeSwitchEnabled, GeneratedArtifactsStaticOnly, TombstoneConflictStaticOnly,
            AudioUploadStaticOnly, RecordingMetadataStaticOnly, Recommendation, FreezeViolations.Count, Blockers.Count,
            DiagnosticsSummary, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataLandingReport left, CanonicalLibraryMetadataLandingReport right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataLandingReport left, CanonicalLibraryMetadataLandingReport right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataDebugPilotBootstrapResult : IEquatable<CanonicalLibraryMetadataDebugPilotBootstrapResult>
{
    public CanonicalLibraryMetadataDebugPilotConfiguration Configuration { get; set; }
    public CanonicalMigrationLandingFreezeResult FreezeResult { get; set; }
    public CanonicalLibraryMetadataProductionCanaryInjectionResult? InjectionResult { get; set; }
    public CanonicalLibraryMetadataLandingReport Report { get; set; }
    public List<CanonicalLibraryMetadataCutoverDiagnostic> Diagnostics { get; set; }
    public CanonicalLibraryMetadataCutoverResult? CutoverResult { get; set; }

    public CanonicalLibraryMetadataDebugPilotBootstrapResult(
        CanonicalLibraryMetadataDebugPilotConfiguration configuration,
        CanonicalMigrationLandingFreezeResult freezeResult,
        CanonicalLibraryMetadataProductionCanaryInjectionResult? injectionResult = null,
        CanonicalLibraryMetadataLandingReport? report = null,
        List<CanonicalLibraryMetadataCutoverDiagnostic>? diagnostics = null,
        CanonicalLibraryMetadataCutoverResult? cutoverResult = null)
    {
        Configuration = configuration;
        FreezeResult = freezeResult;
        InjectionResult = injectionResult;
        Report = report ?? new CanonicalLibraryMetadataLandingReport(CanonicalLibraryMetadataLandingStatus.disabled);
        Diagnostics = diagnostics ?? new List<CanonicalLibraryMetadataCutoverDiagnostic>();
        CutoverResult = cutoverResult;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataDebugPilotBootstrapResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataDebugPilotBootstrapResult? other) =>
        other is not null &&
        EqualityComparer<CanonicalLibraryMetadataDebugPilotConfiguration>.Default.Equals(Configuration, other.Configuration) &&
        EqualityComparer<CanonicalMigrationLandingFreezeResult>.Default.Equals(FreezeResult, other.FreezeResult) &&
        EqualityComparer<CanonicalLibraryMetadataProductionCanaryInjectionResult?>.Default.Equals(InjectionResult, other.InjectionResult) &&
        EqualityComparer<CanonicalLibraryMetadataLandingReport>.Default.Equals(Report, other.Report) &&
        Diagnostics.SequenceEqual(other.Diagnostics) &&
        EqualityComparer<CanonicalLibraryMetadataCutoverResult?>.Default.Equals(CutoverResult, other.CutoverResult);
    public override int GetHashCode() =>
        HashCode.Combine(Configuration, FreezeResult, InjectionResult, Report, Diagnostics.Count, CutoverResult);
    public static bool operator ==(CanonicalLibraryMetadataDebugPilotBootstrapResult left, CanonicalLibraryMetadataDebugPilotBootstrapResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataDebugPilotBootstrapResult left, CanonicalLibraryMetadataDebugPilotBootstrapResult right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataPilotDiagnosticSummary : IEquatable<CanonicalLibraryMetadataPilotDiagnosticSummary>
{
    public CanonicalLibraryMetadataDebugPilotMode Mode { get; set; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; set; }
    public CanonicalMigrationDomain? ActivePilot { get; set; }
    public string FreezeStatus { get; set; }
    public bool CandidateSelected { get; set; }
    public CanonicalLibraryMetadataCanaryCandidateSafetyKind? CandidateKind { get; set; }
    public bool CanaryAttempted { get; set; }
    public bool CanarySucceeded { get; set; }
    public bool RollbackAttempted { get; set; }
    public bool RollbackSucceeded { get; set; }
    public bool LegacyFallbackUsed { get; set; }
    public int DuplicateSuppressionCount { get; set; }
    public bool ReadSideEquivalent { get; set; }
    public int ReadSideDivergenceCount { get; set; }
    public bool OtherDomainsStatic { get; set; }
    public bool RuntimeSwitchFalse { get; set; }
    public bool DiagnosticsRedacted { get; set; }

    public CanonicalLibraryMetadataPilotDiagnosticSummary(
        CanonicalLibraryMetadataDebugPilotMode mode = CanonicalLibraryMetadataDebugPilotMode.disabled,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        CanonicalMigrationDomain? activePilot = null,
        string freezeStatus = "",
        bool candidateSelected = false,
        CanonicalLibraryMetadataCanaryCandidateSafetyKind? candidateKind = null,
        bool canaryAttempted = false,
        bool canarySucceeded = false,
        bool rollbackAttempted = false,
        bool rollbackSucceeded = false,
        bool legacyFallbackUsed = false,
        int duplicateSuppressionCount = 0,
        bool readSideEquivalent = false,
        int readSideDivergenceCount = 0,
        bool otherDomainsStatic = false,
        bool runtimeSwitchFalse = false,
        bool diagnosticsRedacted = true)
    {
        Mode = mode;
        NodeRole = nodeRole;
        ActivePilot = activePilot;
        FreezeStatus = freezeStatus;
        CandidateSelected = candidateSelected;
        CandidateKind = candidateKind;
        CanaryAttempted = canaryAttempted;
        CanarySucceeded = canarySucceeded;
        RollbackAttempted = rollbackAttempted;
        RollbackSucceeded = rollbackSucceeded;
        LegacyFallbackUsed = legacyFallbackUsed;
        DuplicateSuppressionCount = duplicateSuppressionCount;
        ReadSideEquivalent = readSideEquivalent;
        ReadSideDivergenceCount = readSideDivergenceCount;
        OtherDomainsStatic = otherDomainsStatic;
        RuntimeSwitchFalse = runtimeSwitchFalse;
        DiagnosticsRedacted = diagnosticsRedacted;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataPilotDiagnosticSummary other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataPilotDiagnosticSummary? other) =>
        other is not null && Mode == other.Mode && NodeRole == other.NodeRole &&
        ActivePilot == other.ActivePilot && FreezeStatus == other.FreezeStatus &&
        CandidateSelected == other.CandidateSelected && CandidateKind == other.CandidateKind &&
        CanaryAttempted == other.CanaryAttempted && CanarySucceeded == other.CanarySucceeded &&
        RollbackAttempted == other.RollbackAttempted && RollbackSucceeded == other.RollbackSucceeded &&
        LegacyFallbackUsed == other.LegacyFallbackUsed && DuplicateSuppressionCount == other.DuplicateSuppressionCount &&
        ReadSideEquivalent == other.ReadSideEquivalent && ReadSideDivergenceCount == other.ReadSideDivergenceCount &&
        OtherDomainsStatic == other.OtherDomainsStatic && RuntimeSwitchFalse == other.RuntimeSwitchFalse &&
        DiagnosticsRedacted == other.DiagnosticsRedacted;
    public override int GetHashCode() =>
        HashCode.Combine(Mode, NodeRole, ActivePilot, FreezeStatus, CandidateSelected, CandidateKind,
            CanaryAttempted, CanarySucceeded, RollbackAttempted, RollbackSucceeded, LegacyFallbackUsed,
            DuplicateSuppressionCount, ReadSideEquivalent, ReadSideDivergenceCount, OtherDomainsStatic,
            RuntimeSwitchFalse, DiagnosticsRedacted);
    public static bool operator ==(CanonicalLibraryMetadataPilotDiagnosticSummary left, CanonicalLibraryMetadataPilotDiagnosticSummary right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataPilotDiagnosticSummary left, CanonicalLibraryMetadataPilotDiagnosticSummary right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataPilotDiagnosticRedactor
{
    public CanonicalLibraryMetadataPilotDiagnosticRedactor() { }

    public string Redact(string? value)
    {
        if (value == null) return "redacted";
        if (ContainsUnsafeSignal(value))
            return $"redacted-{CanonicalProductionRedaction.HashPrefix(CanonicalHash.Sha256String(value).Value) ?? "diagnostic"}";
        return CanonicalProductionRedaction.SafeDiagnosticText(value) ?? "redacted";
    }

    public static bool ContainsUnsafeSignal(string value)
    {
        var lowercased = value.ToLowerInvariant();
        if (CanonicalProductionRedaction.ContainsSensitivePathSignal(value))
            return true;
        if (lowercased.Contains("api_key") || lowercased.Contains("apikey") ||
            lowercased.Contains("secret") || lowercased.Contains("token=") ||
            lowercased.Contains("fingerprint") || lowercased.Contains("transcript") ||
            lowercased.Contains("provider response"))
            return true;

        int hexRun = 0;
        foreach (char c in lowercased)
        {
            if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))
            {
                hexRun++;
                if (hexRun >= 32) return true;
            }
            else
                hexRun = 0;
        }
        return false;
    }
}

public class CanonicalLibraryMetadataPilotDiagnosticExporter
{
    public CanonicalLibraryMetadataPilotDiagnosticExporter() { }

    public CanonicalLibraryMetadataPilotDiagnosticSummary Export(
        CanonicalLibraryMetadataDebugPilotBootstrapResult result,
        CanonicalProductionExecutionDomainRole nodeRole)
    {
        var report = result.Report;
        return new CanonicalLibraryMetadataPilotDiagnosticSummary(
            mode: report.Mode,
            nodeRole: nodeRole,
            activePilot: report.ActivePilot,
            freezeStatus: result.FreezeResult.Allowed ? "allowed" : "blocked",
            candidateSelected: report.Candidate.Selected,
            candidateKind: report.Candidate.Kind,
            canaryAttempted: report.CommitAttempted,
            canarySucceeded: report.CommitSucceeded,
            rollbackAttempted: report.RollbackAttempted,
            rollbackSucceeded: report.RollbackSucceeded,
            legacyFallbackUsed: report.LegacyFallbackUsed,
            duplicateSuppressionCount: report.DuplicateSuppressedCount,
            readSideEquivalent: report.ReadSideEquivalent,
            readSideDivergenceCount: report.ReadSideDivergenceCount,
            otherDomainsStatic: report.OtherDomainsStaticOnly,
            runtimeSwitchFalse: !report.RuntimeSwitchEnabled,
            diagnosticsRedacted: report.Redacted && result.FreezeResult.Redacted);
    }
}

public class CanonicalLibraryMetadataDebugPilotBootstrap
{
    public CanonicalLibraryMetadataDebugPilotBootstrap() { }

    public async Task<CanonicalLibraryMetadataDebugPilotBootstrapResult> EvaluateOrRun(
        CanonicalLibraryMetadataDebugPilotConfiguration configuration,
        CanonicalMigrationDomainMatrix matrix,
        List<CanonicalLibraryMetadataCutoverCandidate> candidates,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        string? syncRunID,
        bool localSnapshotAvailable,
        bool peerSnapshotAvailable,
        ICanonicalLibraryMetadataCutoverExecutor? executor)
    {
        var freeze = new CanonicalMigrationLandingFreeze().Evaluate(
            matrix: matrix,
            releaseDefaultEnabled: configuration.Policy.ReleaseDefaultEnabled,
            runtimeSwitchEnabled: configuration.Policy.RuntimeSwitchEnabled,
            legacyFallbackAvailable: configuration.Evidence.LegacyFallbackAvailable,
            canaryMaxObjectsPerSyncRun: configuration.Policy.CanaryMaxObjectsPerSyncRun,
            allEligibleEnabled: configuration.Policy.AllowAllEligible);

        var diagnostics = new List<CanonicalLibraryMetadataCutoverDiagnostic>
        {
            Diagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingConfigEvaluated,
                configuration, syncRunID, trigger, nodeRole,
                result: configuration.Mode.ToString(), reason: freeze.DiagnosticsSummary)
        };

        if (!freeze.Allowed)
        {
            diagnostics.Add(Diagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalMigrationLandingFreezeViolation,
                configuration, syncRunID, trigger, nodeRole,
                result: "blocked", reason: string.Join(",", freeze.Violations.Select(v => v.ToString()))));
            diagnostics.Add(Diagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingBlocked,
                configuration, syncRunID, trigger, nodeRole,
                result: "blocked", reason: "landingFreezeViolation"));

            var blockReport = LandingReport(configuration, matrix, freeze, null,
                CanonicalLibraryMetadataLandingStatus.blocked,
                freeze.Violations.Select(v => v.ToString()).ToList(),
                "landingFreezeViolation",
                injectionResult: null);

            diagnostics.Add(ReportDiagnostic(configuration, blockReport, syncRunID, trigger, nodeRole));
            return new CanonicalLibraryMetadataDebugPilotBootstrapResult(
                configuration, freeze, report: blockReport, diagnostics: diagnostics.Take(configuration.MaxDiagnosticsEvents).ToList());
        }

        if (configuration.Mode == CanonicalLibraryMetadataDebugPilotMode.disabled)
        {
            diagnostics.Add(Diagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingDisabled,
                configuration, syncRunID, trigger, nodeRole,
                result: "disabled", reason: "defaultDisabled"));
        }

        if (configuration.Mode == CanonicalLibraryMetadataDebugPilotMode.diagnosticsOnly)
        {
            var diagReport = LandingReport(configuration, matrix, freeze, null,
                CanonicalLibraryMetadataLandingStatus.diagnosticsOnly, new List<string>(),
                "diagnosticsOnlyNoExecution", injectionResult: null);
            diagnostics.Add(ReportDiagnostic(configuration, diagReport, syncRunID, trigger, nodeRole));
            return new CanonicalLibraryMetadataDebugPilotBootstrapResult(
                configuration, freeze, report: diagReport, diagnostics: diagnostics.Take(configuration.MaxDiagnosticsEvents).ToList());
        }

        if (configuration.Mode == CanonicalLibraryMetadataDebugPilotMode.armN1Canary)
        {
            var injection = ArmReadinessInjection(configuration, configuration.CutoverToken,
                configuration.Evidence, matrix, candidates, trigger, localSnapshotAvailable, peerSnapshotAvailable);

            diagnostics.AddRange(LandingDiagnostics(injection, configuration, syncRunID, trigger, nodeRole));
            var armReport = LandingReport(configuration, matrix, freeze, injection,
                LandingStatus(injection.ObservationReport.Status),
                injection.Blockers.Select(b => b.ToString()).ToList(),
                injection.ObservationReport.Reason, injectionResult: injection);
            diagnostics.Add(ReportDiagnostic(configuration, armReport, syncRunID, trigger, nodeRole));
            return new CanonicalLibraryMetadataDebugPilotBootstrapResult(
                configuration, freeze, injectionResult: injection, report: armReport,
                diagnostics: diagnostics.Take(configuration.MaxDiagnosticsEvents).ToList());
        }

        if (configuration.Mode == CanonicalLibraryMetadataDebugPilotMode.executeN1Canary)
        {
            diagnostics.Add(Diagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingN1Started,
                configuration, syncRunID, trigger, nodeRole,
                result: "started", reason: "strictN1"));
        }

        var execInjection = await new CanonicalLibraryMetadataProductionCanaryInjection().EvaluateOrRun(
            configuration.AsProductionCanaryConfiguration,
            configuration.CutoverToken, configuration.Evidence, matrix, candidates,
            trigger, nodeRole, syncRunID, localSnapshotAvailable, peerSnapshotAvailable, executor);

        diagnostics.AddRange(LandingDiagnostics(execInjection, configuration, syncRunID, trigger, nodeRole));
        var execStatus = LandingStatus(execInjection.ObservationReport.Status);
        var execBlockers = execInjection.Blockers.Select(b => b.ToString()).ToList();
        var execReport = LandingReport(configuration, matrix, freeze, execInjection, execStatus,
            execBlockers, execInjection.ObservationReport.Reason, injectionResult: execInjection);
        diagnostics.Add(ReportDiagnostic(configuration, execReport, syncRunID, trigger, nodeRole));

        return new CanonicalLibraryMetadataDebugPilotBootstrapResult(
            configuration, freeze, injectionResult: execInjection, report: execReport,
            diagnostics: diagnostics.Take(configuration.MaxDiagnosticsEvents).ToList(),
            cutoverResult: execInjection.CutoverResult);
    }

    private CanonicalLibraryMetadataProductionCanaryInjectionResult ArmReadinessInjection(
        CanonicalLibraryMetadataDebugPilotConfiguration configuration,
        CanonicalCutoverToken? token,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        CanonicalMigrationDomainMatrix matrix,
        List<CanonicalLibraryMetadataCutoverCandidate> candidates,
        CanonicalSyncPlanTrigger trigger,
        bool localSnapshotAvailable,
        bool peerSnapshotAvailable)
    {
        var armEvidence = ArmCandidateSafetyEvidence(evidence);
        var selection = new CanonicalLibraryMetadataCanarySelector().Select(
            CanonicalCutoverMode.canary,
            new CanonicalLibraryMetadataCanaryPolicy(canaryMaxObjectsPerSyncRun: 1, allowsInternalN1Execution: true, explicitInternalTestConfiguration: true),
            trigger, armEvidence, candidates);

        var safetyReports = candidates.Select(c => new CanonicalLibraryMetadataCanaryCandidateSafety(c, armEvidence)).ToList();
        var blockers = ArmReadinessBlockers(configuration, token, evidence, matrix, trigger,
            localSnapshotAvailable, peerSnapshotAvailable);

        if (selection.SelectedCandidates.Count == 0)
        {
            var unsafeSkipped = safetyReports.Any(s => !s.Safe);
            blockers.Add(unsafeSkipped ? CanonicalLibraryMetadataRealCanaryBlocker.unsafeCandidateSkipped : CanonicalLibraryMetadataRealCanaryBlocker.noEligibleCandidate);
        }

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataRealCanaryBlocker>(blockers)
            .OrderBy(b => b.ToString()).ToList();
        var status = uniqueBlockers.Count == 0
            ? CanonicalLibraryMetadataRealCanaryObservationStatus.armed
            : CanonicalLibraryMetadataRealCanaryObservationStatus.blocked;

        var observation = new CanonicalLibraryMetadataRealCanaryObservationReport(
            status,
            uniqueBlockers.Count == 0
                ? CanonicalLibraryMetadataRealCanaryRecommendation.stayN1
                : CanonicalLibraryMetadataRealCanaryRecommendation.fixBlockers,
            configuration.AsProductionCanaryConfiguration,
            selectedCandidateCount: selection.SelectedCandidates.Count,
            noEligibleCandidateCount: selection.SelectedCandidates.Count == 0 ? 1 : 0,
            unsafeCandidateSkippedCount: safetyReports.Count(s => !s.Safe),
            readSideParallelEquivalent: evidence.ReadSideParallelEquivalent,
            readSideParallelDivergent: !evidence.ReadSideParallelEquivalent,
            legacyFallbackPreserved: evidence.LegacyFallbackAvailable,
            blockers: uniqueBlockers,
            reason: uniqueBlockers.Count == 0 ? "armN1ReadinessOnly" : string.Join(",", uniqueBlockers.Select(b => b.ToString())));

        return new CanonicalLibraryMetadataProductionCanaryInjectionResult(
            configuration.AsProductionCanaryConfiguration,
            injectionConfigured: true,
            executorInjected: false,
            applyPortInjected: false,
            armed: uniqueBlockers.Count == 0,
            executed: false,
            succeeded: false,
            blockers: uniqueBlockers,
            diagnostics: new List<CanonicalLibraryMetadataCutoverDiagnostic>(),
            selection: selection,
            candidateSafetyReports: safetyReports,
            observationReport: observation);
    }

    private List<CanonicalLibraryMetadataRealCanaryBlocker> ArmReadinessBlockers(
        CanonicalLibraryMetadataDebugPilotConfiguration configuration,
        CanonicalCutoverToken? token,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        CanonicalMigrationDomainMatrix matrix,
        CanonicalSyncPlanTrigger trigger,
        bool localSnapshotAvailable,
        bool peerSnapshotAvailable)
    {
        var blockers = new List<CanonicalLibraryMetadataRealCanaryBlocker>();
        if (configuration.Policy.Domain != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.nonLibraryMetadataDomain);
        if (configuration.Policy.CanaryMaxObjectsPerSyncRun != 1)
            blockers.Add(configuration.Policy.CanaryMaxObjectsPerSyncRun > 1 ? CanonicalLibraryMetadataRealCanaryBlocker.canaryBudgetAboveOneDenied : CanonicalLibraryMetadataRealCanaryBlocker.n1BudgetRequired);
        if (configuration.Policy.RuntimeSwitchEnabled) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.runtimeSwitchDenied);
        if (configuration.Policy.AllowAllEligible) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.allEligibleDenied);
        if (configuration.Policy.ReleaseDefaultEnabled) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.releaseDefaultDenied);
        if (configuration.Policy.RequiresExplicitInternalDebugConfiguration && !configuration.ExplicitInternalDebugConfiguration)
            blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingExplicitInternalDebugConfiguration);

        var matrixReport = matrix.Validate();
        if (!matrixReport.Allowed) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.matrixValidationBlocked);
        if (matrixReport.ActivePilotDomain != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.activePilotNotLibraryMetadata);
        if (configuration.Policy.RequiresProductionToken && token == null) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingToken);
        if (configuration.Policy.RequiresOwnerApproval && token?.OwnerApproved != true) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingOwnerApproval);
        if (!localSnapshotAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.localSnapshotUnavailable);
        if (!peerSnapshotAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.peerSnapshotUnavailable);
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh || trigger == CanonicalSyncPlanTrigger.retryDrainer) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.unsupportedTrigger);
        if (!evidence.NoCommitEvidenceAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingNoCommitEvidence);
        if (!evidence.RealDataShadowCopyVerified) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingRealDataShadowCopyEvidence);
        if (!evidence.ExecutionShadowVerified) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingExecutionShadowEvidence);
        if (!evidence.DryRunEquivalenceVerified) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingDryRunEquivalence);
        if (!evidence.NoBlockingDivergence) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.blockingDivergence);
        if (!evidence.NoUnresolvedConflict) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.unresolvedConflict);
        if (!evidence.MetadataManifestRouteEvidenceAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.missingMetadataManifestRouteEvidence);
        if (evidence.RollbackPlan == null) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.rollbackPlanMissing);
        if (!evidence.RollbackCheckpointAvailable || !evidence.RollbackVerified || !evidence.RollbackRehearsalPassed)
            blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.rollbackVerificationMissing);
        if (!evidence.LegacyFallbackAvailable) blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.legacyFallbackUnavailable);
        if (configuration.Policy.RequiresReadSideParallelEquivalent && !evidence.ReadSideParallelEquivalent)
            blockers.Add(CanonicalLibraryMetadataRealCanaryBlocker.readSideParallelDivergent);

        return new HashSet<CanonicalLibraryMetadataRealCanaryBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
    }

    private CanonicalLibraryMetadataCutoverEvidence ArmCandidateSafetyEvidence(CanonicalLibraryMetadataCutoverEvidence evidence)
    {
        return new CanonicalLibraryMetadataCutoverEvidence(
            noCommitEvidenceAvailable: evidence.NoCommitEvidenceAvailable,
            realDataShadowCopyVerified: evidence.RealDataShadowCopyVerified,
            executionShadowVerified: evidence.ExecutionShadowVerified,
            dryRunEquivalenceVerified: evidence.DryRunEquivalenceVerified,
            noBlockingDivergence: evidence.NoBlockingDivergence,
            noUnresolvedConflict: evidence.NoUnresolvedConflict,
            metadataManifestRouteEvidenceAvailable: evidence.MetadataManifestRouteEvidenceAvailable,
            productionPortAvailable: evidence.ProductionPortAvailable,
            realRootBoundApplyPortAvailable: true,
            applyPortMode: CanonicalLibraryMetadataApplyPortMode.testRootBound,
            rootBoundWriteAvailable: true,
            atomicReplaceAvailable: true,
            rollbackCheckpointAvailable: true,
            rollbackVerified: evidence.RollbackVerified,
            productionRootDisabledByDefault: evidence.ProductionRootDisabledByDefault,
            testRootUsed: true,
            legacyFallbackAvailable: evidence.LegacyFallbackAvailable,
            rollbackPlan: evidence.RollbackPlan,
            rollbackRehearsalPassed: evidence.RollbackRehearsalPassed,
            readSideParallelEquivalent: evidence.ReadSideParallelEquivalent,
            canaryStageEvidence: evidence.CanaryStageEvidence);
    }

    private List<CanonicalLibraryMetadataCutoverDiagnostic> LandingDiagnostics(
        CanonicalLibraryMetadataProductionCanaryInjectionResult injection,
        CanonicalLibraryMetadataDebugPilotConfiguration configuration,
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole)
    {
        var diagnostics = new List<CanonicalLibraryMetadataCutoverDiagnostic>();
        // Forward production-root diagnostics
        diagnostics.AddRange(injection.Diagnostics.Where(d =>
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootGateEvaluated ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootGateBlocked ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootGateAllowed ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootN1Started ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootN1Completed ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootN1Failed ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootSafetyProofBuilt ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootCheckpointCreated ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootAtomicWriteStarted ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootAtomicWriteCompleted ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootPostconditionVerified ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootRollbackStarted ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootRollbackCompleted ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootRollbackFailed ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootLegacyFallbackUsed ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootDuplicateSuppressed ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootReadSideEquivalent ||
            d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataProductionRootReadSideDivergent));

        if (injection.Armed)
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingArmed,
                configuration, syncRunID, trigger, nodeRole, result: "armed", reason: "strictN1Gate"));
        if (injection.Blockers.Count > 0)
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingBlocked,
                configuration, syncRunID, trigger, nodeRole, result: "blocked",
                reason: string.Join(",", injection.Blockers.Select(b => b.ToString()))));

        // Candidate selection diagnostics
        var sel = injection.CanaryResult?.Selection ?? injection.CutoverResult?.CanarySelection ?? injection.Selection;
        if (sel != null)
        {
            if (sel.SelectedCutoverCandidates.FirstOrDefault() is { } selected)
            {
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingCandidateSelected,
                    configuration, syncRunID, trigger, nodeRole,
                    domain: selected.Domain, objectID: selected.ObjectID, objectKind: selected.ObjectKind,
                    action: selected.CutoverActionKind.ToString(), result: "selected", reason: "metadataOnlyN1"));
            }
            else if (sel.SelectedCutoverCandidates.Count == 0)
            {
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingNoEligibleCandidate,
                    configuration, syncRunID, trigger, nodeRole, result: "blocked",
                    reason: string.Join(",", sel.Blockers.Select(b => b.Reason.ToString()))));
            }
        }
        else if (injection.ObservationReport.NoEligibleCandidateCount > 0)
        {
            diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingNoEligibleCandidate,
                configuration, syncRunID, trigger, nodeRole, result: "blocked", reason: "noEligibleCandidate"));
        }

        // Cutover result diagnostics
        if (injection.CutoverResult is { } cutover)
        {
            foreach (var commit in cutover.Commits)
            {
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingCommitStarted,
                    configuration, syncRunID, trigger, nodeRole,
                    domain: commit.Domain, objectID: commit.ObjectID, objectKind: commit.ObjectKind,
                    action: commit.ActionKind.ToString(), result: "started", reason: "rootBoundMetadataApply"));
                diagnostics.Add(Diagnostic(
                    commit.Committed && commit.PostconditionVerified
                        ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingCommitCompleted
                        : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingCommitFailed,
                    configuration, syncRunID, trigger, nodeRole,
                    domain: commit.Domain, objectID: commit.ObjectID, objectKind: commit.ObjectKind,
                    action: commit.ActionKind.ToString(),
                    result: commit.Committed ? "committed" : "failed", reason: commit.Reason));
            }
            foreach (var rollback in cutover.RollbackResults)
            {
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingRollbackStarted,
                    configuration, syncRunID, trigger, nodeRole, result: "started", reason: rollback.CheckpointID));
                diagnostics.Add(Diagnostic(
                    rollback.Succeeded ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingRollbackCompleted
                        : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingRollbackFailed,
                    configuration, syncRunID, trigger, nodeRole,
                    result: rollback.Succeeded ? "completed" : "failed", reason: rollback.Reason));
            }
            if (cutover.LegacyFallbackUsed)
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingLegacyFallbackUsed,
                    configuration, syncRunID, trigger, nodeRole, result: "used", reason: "legacyFallbackPreserved"));
            if (cutover.DuplicateLegacySuppressedActionIDs.Count > 0)
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingDuplicateSuppressed,
                    configuration, syncRunID, trigger, nodeRole, result: "successOnly", reason: "matchingLegacyLibraryMetadataOnly"));

            var equivalent = cutover.ReadSideProjection?.Equivalent ?? injection.ObservationReport.ReadSideParallelEquivalent;
            diagnostics.Add(Diagnostic(
                equivalent ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingReadSideEquivalent
                    : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingReadSideDivergent,
                configuration, syncRunID, trigger, nodeRole,
                result: equivalent ? "equivalent" : "divergent",
                reason: cutover.ReadSideProjection?.Reason ?? "readSideParallelEvidence"));
        }
        else
        {
            if (injection.ObservationReport.ReadSideParallelEquivalent)
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingReadSideEquivalent,
                    configuration, syncRunID, trigger, nodeRole, result: "equivalent", reason: "readSideParallelEvidence"));
            else if (injection.ObservationReport.ReadSideParallelDivergent)
                diagnostics.Add(Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingReadSideDivergent,
                    configuration, syncRunID, trigger, nodeRole, result: "divergent", reason: "readSideParallelEvidence"));
        }

        return diagnostics;
    }

    private CanonicalLibraryMetadataLandingReport LandingReport(
        CanonicalLibraryMetadataDebugPilotConfiguration configuration,
        CanonicalMigrationDomainMatrix matrix,
        CanonicalMigrationLandingFreezeResult freeze,
        CanonicalLibraryMetadataProductionCanaryInjectionResult? injectionResult,
        CanonicalLibraryMetadataLandingStatus status,
        List<string> blockers,
        string reason,
        CanonicalLibraryMetadataProductionCanaryInjectionResult? injectionResult_alias = null)
    {
        var cutover = (injectionResult ?? injectionResult_alias)?.CutoverResult;
        var safetyReports = cutover?.CandidateSafetyReports ?? (injectionResult ?? injectionResult_alias)?.CandidateSafetyReports;
        var selectedSafety = safetyReports?.FirstOrDefault(s => s.Safe) ?? safetyReports?.FirstOrDefault();
        var selectedCandidate = (injectionResult ?? injectionResult_alias)?.CanaryResult?.Selection.SelectedCutoverCandidates.FirstOrDefault()
            ?? cutover?.CanarySelection?.SelectedCutoverCandidates.FirstOrDefault()
            ?? (injectionResult ?? injectionResult_alias)?.Selection?.SelectedCutoverCandidates.FirstOrDefault();
        var selectedByCount = selectedCandidate != null || ((injectionResult ?? injectionResult_alias)?.ObservationReport.SelectedCandidateCount ?? 0) > 0;
        var commitSucceeded = cutover?.Commits.Any(c => c.Committed && c.PreconditionVerified && c.PostconditionVerified) ?? false;
        var rollbackAttempted = !(cutover?.RollbackResults.Count == 0) ?? false;
        var rollbackSucceeded = rollbackAttempted && (cutover?.RollbackResults.All(r => r.Succeeded && !r.Fatal) ?? false);
        var readSideEquivalent = cutover?.ReadSideProjection?.Equivalent
            ?? (injectionResult ?? injectionResult_alias)?.ObservationReport.ReadSideParallelEquivalent
            ?? configuration.Evidence.ReadSideParallelEquivalent;
        var readSideDivergent = (cutover?.ReadSideProjection?.Equivalent == false)
            || ((injectionResult ?? injectionResult_alias)?.ObservationReport.ReadSideParallelDivergent ?? false)
            || !configuration.Evidence.ReadSideParallelEquivalent;

        var allBlockers = new HashSet<string>(blockers.Concat(freeze.Violations.Select(v => v.ToString())));
        var sortedBlockers = allBlockers.OrderBy(b => b).ToList();

        return new CanonicalLibraryMetadataLandingReport(
            status: status,
            mode: configuration.Mode,
            rootMode: configuration.RootMode,
            activePilot: freeze.ActivePilotDomain,
            candidate: new CanonicalLibraryMetadataLandingCandidateSummary(
                selected: selectedByCount,
                kind: selectedSafety?.Kind,
                objectKind: selectedCandidate?.ObjectKind,
                domain: selectedCandidate?.Domain,
                actionKind: selectedCandidate?.CutoverActionKind,
                metadataOnly: selectedSafety?.MetadataOnly ?? selectedByCount,
                resourceMoveAttempted: selectedSafety?.ResourceMoveAttempted ?? selectedCandidate?.HasResourceMoveAttempt ?? false,
                contentBytesMutated: selectedSafety?.ContentBytesMutated ?? false),
            commitAttempted: !(cutover?.Commits.Count == 0),
            commitSucceeded: commitSucceeded,
            rollbackAttempted: rollbackAttempted,
            rollbackSucceeded: rollbackSucceeded,
            legacyFallbackUsed: cutover?.LegacyFallbackUsed ?? (injectionResult ?? injectionResult_alias)?.ObservationReport.LegacyFallbackPreserved ?? true,
            duplicateSuppressed: !(cutover?.DuplicateLegacySuppressedActionIDs.Count == 0),
            duplicateSuppressedCount: cutover?.DuplicateLegacySuppressedActionIDs.Count ?? (injectionResult ?? injectionResult_alias)?.ObservationReport.DuplicateSuppressionCount ?? 0,
            readSideEquivalent: readSideEquivalent,
            readSideDivergenceCount: readSideDivergent ? 1 : 0,
            otherDomainsStaticOnly: freeze.OtherDomainsStaticOnly,
            runtimeSwitchEnabled: freeze.RuntimeSwitchEnabled || configuration.Policy.RuntimeSwitchEnabled,
            generatedArtifactsStaticOnly: matrix.PolicyFor(CanonicalMigrationDomain.generatedArtifacts)?.StaticOnly == true,
            tombstoneConflictStaticOnly: matrix.PolicyFor(CanonicalMigrationDomain.tombstoneConflict)?.StaticOnly == true,
            audioUploadStaticOnly: matrix.PolicyFor(CanonicalMigrationDomain.audioUpload)?.StaticOnly == true,
            recordingMetadataStaticOnly: matrix.PolicyFor(CanonicalMigrationDomain.recordingMetadata)?.StaticOnly == true,
            recommendation: Recommendation(status, sortedBlockers, commitSucceeded, rollbackSucceeded, rollbackAttempted, readSideEquivalent),
            freezeViolations: freeze.Violations,
            blockers: sortedBlockers,
            diagnosticsSummary: CanonicalProductionRedaction.SafeDiagnosticText(string.Join(",",
                $"status={status}", $"mode={configuration.Mode}", $"rootMode={configuration.RootMode}",
                $"candidateSelected={selectedByCount}", $"commitAttempted={!(cutover?.Commits.Count == 0)}",
                $"commitSucceeded={commitSucceeded}", $"rollbackAttempted={rollbackAttempted}",
                $"rollbackSucceeded={rollbackSucceeded}", $"fallback={cutover?.LegacyFallbackUsed ?? false}",
                $"duplicateSuppressed={!(cutover?.DuplicateLegacySuppressedActionIDs.Count == 0)}",
                $"readSideEquivalent={readSideEquivalent}", "uiReadPathSwitched=false",
                $"otherDomainsStaticOnly={freeze.OtherDomainsStaticOnly}",
                $"runtimeSwitch={freeze.RuntimeSwitchEnabled || configuration.Policy.RuntimeSwitchEnabled}",
                $"reason={reason}")) ?? status.ToString(),
            redacted: true);
    }

    private CanonicalLibraryMetadataLandingStatus LandingStatus(CanonicalLibraryMetadataRealCanaryObservationStatus status) => status switch
    {
        CanonicalLibraryMetadataRealCanaryObservationStatus.disabled => CanonicalLibraryMetadataLandingStatus.disabled,
        CanonicalLibraryMetadataRealCanaryObservationStatus.diagnosticsOnly => CanonicalLibraryMetadataLandingStatus.diagnosticsOnly,
        CanonicalLibraryMetadataRealCanaryObservationStatus.armed => CanonicalLibraryMetadataLandingStatus.armed,
        CanonicalLibraryMetadataRealCanaryObservationStatus.blocked => CanonicalLibraryMetadataLandingStatus.blocked,
        CanonicalLibraryMetadataRealCanaryObservationStatus.noEligibleCandidate => CanonicalLibraryMetadataLandingStatus.noEligibleCandidate,
        CanonicalLibraryMetadataRealCanaryObservationStatus.unsafeCandidateSkipped => CanonicalLibraryMetadataLandingStatus.unsafeCandidateSkipped,
        CanonicalLibraryMetadataRealCanaryObservationStatus.executedSucceeded => CanonicalLibraryMetadataLandingStatus.executedSucceeded,
        CanonicalLibraryMetadataRealCanaryObservationStatus.executedFailedRolledBack => CanonicalLibraryMetadataLandingStatus.executedFailedRolledBack,
        CanonicalLibraryMetadataRealCanaryObservationStatus.fatalRollbackFailure => CanonicalLibraryMetadataLandingStatus.fatalRollbackFailure,
        _ => CanonicalLibraryMetadataLandingStatus.disabled
    };

    private CanonicalLibraryMetadataLandingRecommendation Recommendation(
        CanonicalLibraryMetadataLandingStatus status, List<string> blockers,
        bool commitSucceeded, bool rollbackSucceeded, bool rollbackAttempted, bool readSideEquivalent)
    {
        if (status == CanonicalLibraryMetadataLandingStatus.disabled || status == CanonicalLibraryMetadataLandingStatus.diagnosticsOnly)
            return CanonicalLibraryMetadataLandingRecommendation.remainDisabled;
        if (blockers.Count > 0 || !readSideEquivalent || (rollbackAttempted && !rollbackSucceeded))
            return CanonicalLibraryMetadataLandingRecommendation.fixBlockers;
        if (commitSucceeded) return CanonicalLibraryMetadataLandingRecommendation.runAnotherN1;
        if (status == CanonicalLibraryMetadataLandingStatus.armed) return CanonicalLibraryMetadataLandingRecommendation.remainDisabled;
        return CanonicalLibraryMetadataLandingRecommendation.fixBlockers;
    }

    private CanonicalLibraryMetadataCutoverDiagnostic ReportDiagnostic(
        CanonicalLibraryMetadataDebugPilotConfiguration configuration,
        CanonicalLibraryMetadataLandingReport report,
        string? syncRunID, CanonicalSyncPlanTrigger trigger, CanonicalProductionExecutionDomainRole nodeRole) =>
        Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLandingReportBuilt,
            configuration, syncRunID, trigger, nodeRole, result: report.Status.ToString(), reason: report.DiagnosticsSummary);

    private CanonicalLibraryMetadataCutoverDiagnostic Diagnostic(
        CanonicalLibraryMetadataCutoverDiagnosticKind kind,
        CanonicalLibraryMetadataDebugPilotConfiguration configuration,
        string? syncRunID, CanonicalSyncPlanTrigger trigger, CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalLibraryMetadataCutoverDomain? domain = null,
        string? objectID = null, CanonicalObjectKind? objectKind = null,
        string? action = null, string? result = null, string? reason = null) =>
        new(kind, syncRunID, trigger, nodeRole, domain, objectID, objectKind, action, result,
            string.Join(";", new List<string?> { reason, $"mode={configuration.Mode}", $"rootMode={configuration.RootMode}", $"domain={configuration.Policy.Domain}", $"runtimeSwitch={configuration.Policy.RuntimeSwitchEnabled}" }.Where(r => r != null)));
}
