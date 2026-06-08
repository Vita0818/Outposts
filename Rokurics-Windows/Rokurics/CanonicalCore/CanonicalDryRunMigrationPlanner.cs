using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLegacyEquivalenceDomain
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
    uiIntegration
}

public static class CanonicalLegacyEquivalenceDomainExtensions
{
    public static CanonicalProductionDomain ToProductionDomain(this CanonicalLegacyEquivalenceDomain domain)
    {
        return domain switch
        {
            CanonicalLegacyEquivalenceDomain.recordingMetadata => CanonicalProductionDomain.recordingMetadata,
            CanonicalLegacyEquivalenceDomain.recordingAudio => CanonicalProductionDomain.recordingAudio,
            CanonicalLegacyEquivalenceDomain.generatedArtifacts => CanonicalProductionDomain.generatedArtifacts,
            CanonicalLegacyEquivalenceDomain.folders => CanonicalProductionDomain.folders,
            CanonicalLegacyEquivalenceDomain.studyItems => CanonicalProductionDomain.studyItems,
            CanonicalLegacyEquivalenceDomain.standaloneNotes => CanonicalProductionDomain.standaloneNotes,
            CanonicalLegacyEquivalenceDomain.tombstones => CanonicalProductionDomain.tombstones,
            CanonicalLegacyEquivalenceDomain.conflicts => CanonicalProductionDomain.conflicts,
            CanonicalLegacyEquivalenceDomain.apply => CanonicalProductionDomain.apply,
            CanonicalLegacyEquivalenceDomain.fileRuntime => CanonicalProductionDomain.fileRuntime,
            CanonicalLegacyEquivalenceDomain.transportRuntime => CanonicalProductionDomain.transportRuntime,
            CanonicalLegacyEquivalenceDomain.uploadRuntime => CanonicalProductionDomain.uploadRuntime,
            CanonicalLegacyEquivalenceDomain.objectProjection => CanonicalProductionDomain.objectProjection,
            CanonicalLegacyEquivalenceDomain.uiIntegration => CanonicalProductionDomain.uiIntegration,
            _ => CanonicalProductionDomain.inventory
        };
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLegacyEquivalenceStatus
{
    equivalent,
    canonicalMoreConservative,
    canonicalMoreAggressive,
    legacyOnly,
    canonicalOnly,
    unsupported,
    conflict,
    blocked,
    unknown
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLegacyDivergenceSeverity
{
    info,
    warning,
    blocking
}

public sealed class CanonicalLegacyDivergence : IEquatable<CanonicalLegacyDivergence>
{
    public string Id => string.Join("|", Domain.ToString(), Status.ToString(), Severity.ToString(), Reason);
    public CanonicalLegacyEquivalenceDomain Domain { get; }
    public CanonicalLegacyEquivalenceStatus Status { get; }
    public CanonicalLegacyDivergenceSeverity Severity { get; }
    public string Reason { get; }
    public List<string> CanonicalActionIDs { get; }
    public List<string> LegacyActionIDs { get; }
    public string? HashPrefix { get; }

    public CanonicalLegacyDivergence(
        CanonicalLegacyEquivalenceDomain domain,
        CanonicalLegacyEquivalenceStatus status,
        CanonicalLegacyDivergenceSeverity severity,
        string reason,
        List<string>? canonicalActionIDs = null,
        List<string>? legacyActionIDs = null,
        CanonicalHash? hash = null)
    {
        Domain = domain;
        Status = status;
        Severity = severity;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? status.ToString();
        CanonicalActionIDs = Normalized(canonicalActionIDs ?? new List<string>());
        LegacyActionIDs = Normalized(legacyActionIDs ?? new List<string>());
        HashPrefix = hash != null ? CanonicalProductionRedaction.HashPrefix(hash.Value) : null;
    }

    public bool IsBlocking => Severity == CanonicalLegacyDivergenceSeverity.blocking;

    private static List<string> Normalized(List<string> ids)
    {
        return new HashSet<string>(ids
            .Select(id => CanonicalProductionRedaction.SafeDiagnosticText(id))
            .Where(id => id != null)
            .Cast<string>())
            .OrderBy(id => id).ToList();
    }

    public override bool Equals(object? obj) => obj is CanonicalLegacyDivergence other && Equals(other);
    public bool Equals(CanonicalLegacyDivergence? other) =>
        other is not null &&
        Domain == other.Domain &&
        Status == other.Status &&
        Severity == other.Severity &&
        Reason == other.Reason &&
        CanonicalActionIDs.SequenceEqual(other.CanonicalActionIDs) &&
        LegacyActionIDs.SequenceEqual(other.LegacyActionIDs) &&
        HashPrefix == other.HashPrefix;
    public override int GetHashCode() => HashCode.Combine(Domain, Status, Severity, Reason, HashPrefix);
    public static bool operator ==(CanonicalLegacyDivergence l, CanonicalLegacyDivergence r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyDivergence l, CanonicalLegacyDivergence r) => !l.Equals(r);
}

public sealed class CanonicalLegacyEquivalenceDomainReport : IEquatable<CanonicalLegacyEquivalenceDomainReport>
{
    public string Id => Domain.ToString();
    public CanonicalLegacyEquivalenceDomain Domain { get; }
    public CanonicalLegacyEquivalenceStatus Status { get; }
    public List<string> CanonicalActionIDs { get; }
    public List<string> LegacyActionIDs { get; }
    public List<CanonicalLegacyDivergence> Divergences { get; }

    public CanonicalLegacyEquivalenceDomainReport(
        CanonicalLegacyEquivalenceDomain domain,
        CanonicalLegacyEquivalenceStatus status,
        List<string>? canonicalActionIDs = null,
        List<string>? legacyActionIDs = null,
        List<CanonicalLegacyDivergence>? divergences = null)
    {
        Domain = domain;
        Status = status;
        CanonicalActionIDs = Normalized(canonicalActionIDs ?? new List<string>());
        LegacyActionIDs = Normalized(legacyActionIDs ?? new List<string>());
        Divergences = divergences ?? new List<CanonicalLegacyDivergence>();
    }

    public bool IsBlocking =>
        Divergences.Any(d => d.IsBlocking) ||
        new[] {
            CanonicalLegacyEquivalenceStatus.canonicalMoreAggressive,
            CanonicalLegacyEquivalenceStatus.legacyOnly,
            CanonicalLegacyEquivalenceStatus.canonicalOnly,
            CanonicalLegacyEquivalenceStatus.unsupported,
            CanonicalLegacyEquivalenceStatus.conflict,
            CanonicalLegacyEquivalenceStatus.blocked,
            CanonicalLegacyEquivalenceStatus.unknown
        }.Contains(Status);

    private static List<string> Normalized(List<string> ids)
    {
        return new HashSet<string>(ids
            .Select(id => CanonicalProductionRedaction.SafeDiagnosticText(id))
            .Where(id => id != null)
            .Cast<string>())
            .OrderBy(id => id).ToList();
    }

    public override bool Equals(object? obj) => obj is CanonicalLegacyEquivalenceDomainReport other && Equals(other);
    public bool Equals(CanonicalLegacyEquivalenceDomainReport? other) =>
        other is not null &&
        Domain == other.Domain &&
        Status == other.Status &&
        CanonicalActionIDs.SequenceEqual(other.CanonicalActionIDs) &&
        LegacyActionIDs.SequenceEqual(other.LegacyActionIDs) &&
        Divergences.SequenceEqual(other.Divergences);
    public override int GetHashCode() => HashCode.Combine(Domain, Status);
    public static bool operator ==(CanonicalLegacyEquivalenceDomainReport l, CanonicalLegacyEquivalenceDomainReport r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyEquivalenceDomainReport l, CanonicalLegacyEquivalenceDomainReport r) => !l.Equals(r);
}

public sealed class CanonicalLegacyEquivalenceReport : IEquatable<CanonicalLegacyEquivalenceReport>
{
    public CanonicalTimestamp GeneratedAt { get; }
    public List<CanonicalLegacyEquivalenceDomainReport> DomainReports { get; }
    public List<CanonicalLegacyDivergence> Divergences { get; }
    public bool AllEquivalent { get; }
    public bool HasBlockingDivergence { get; }

    public CanonicalLegacyEquivalenceReport(
        List<CanonicalLegacyEquivalenceDomainReport> domainReports,
        DateTime? generatedAt = null)
    {
        GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
        DomainReports = domainReports.OrderBy(r => r.Domain.ToString()).ToList();
        Divergences = DomainReports.SelectMany(r => r.Divergences).OrderBy(d => d.Id).ToList();
        AllEquivalent = DomainReports.All(r =>
            r.Status == CanonicalLegacyEquivalenceStatus.equivalent ||
            r.Status == CanonicalLegacyEquivalenceStatus.canonicalMoreConservative);
        HasBlockingDivergence = DomainReports.Any(r => r.IsBlocking);
    }

    public CanonicalLegacyEquivalenceStatus StatusFor(CanonicalLegacyEquivalenceDomain domain)
    {
        return DomainReports.FirstOrDefault(r => r.Domain == domain)?.Status ?? CanonicalLegacyEquivalenceStatus.unknown;
    }

    public override bool Equals(object? obj) => obj is CanonicalLegacyEquivalenceReport other && Equals(other);
    public bool Equals(CanonicalLegacyEquivalenceReport? other) =>
        other is not null &&
        GeneratedAt.Equals(other.GeneratedAt) &&
        DomainReports.SequenceEqual(other.DomainReports) &&
        AllEquivalent == other.AllEquivalent &&
        HasBlockingDivergence == other.HasBlockingDivergence;
    public override int GetHashCode() => HashCode.Combine(GeneratedAt, AllEquivalent, HasBlockingDivergence);
    public static bool operator ==(CanonicalLegacyEquivalenceReport l, CanonicalLegacyEquivalenceReport r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyEquivalenceReport l, CanonicalLegacyEquivalenceReport r) => !l.Equals(r);
}

public sealed class CanonicalDryRunEquivalenceReport : IEquatable<CanonicalDryRunEquivalenceReport>
{
    public CanonicalLegacyEquivalenceReport LegacyEquivalence { get; }
    public List<CanonicalLegacyEquivalenceDomain> EquivalentDomains { get; }
    public List<CanonicalLegacyEquivalenceDomain> DivergentDomains { get; }

    public CanonicalDryRunEquivalenceReport(CanonicalLegacyEquivalenceReport legacyEquivalence)
    {
        LegacyEquivalence = legacyEquivalence;
        EquivalentDomains = legacyEquivalence.DomainReports
            .Where(r => r.Status == CanonicalLegacyEquivalenceStatus.equivalent ||
                        r.Status == CanonicalLegacyEquivalenceStatus.canonicalMoreConservative)
            .Select(r => r.Domain)
            .OrderBy(d => d.ToString()).ToList();
        DivergentDomains = legacyEquivalence.DomainReports
            .Where(r => r.IsBlocking)
            .Select(r => r.Domain)
            .OrderBy(d => d.ToString()).ToList();
    }

    public override bool Equals(object? obj) => obj is CanonicalDryRunEquivalenceReport other && Equals(other);
    public bool Equals(CanonicalDryRunEquivalenceReport? other) =>
        other is not null &&
        LegacyEquivalence.Equals(other.LegacyEquivalence);
    public override int GetHashCode() => LegacyEquivalence.GetHashCode();
    public static bool operator ==(CanonicalDryRunEquivalenceReport l, CanonicalDryRunEquivalenceReport r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunEquivalenceReport l, CanonicalDryRunEquivalenceReport r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalDryRunActionKind
{
    wouldNoOp,
    wouldUpload,
    wouldDownload,
    wouldApply,
    wouldSend,
    wouldRecordConflict,
    wouldSuppress
}

public sealed class CanonicalDryRunAction : IEquatable<CanonicalDryRunAction>
{
    public string Id => ActionID;
    public string ActionID { get; }
    public CanonicalProductionDomain Domain { get; }
    public CanonicalDryRunActionKind Kind { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public string Reason { get; }
    public bool SuppressedBecauseDryRun { get; }

    public CanonicalDryRunAction(
        CanonicalProductionDomain domain,
        CanonicalDryRunActionKind kind,
        string? objectID = null,
        string? artifactID = null,
        string reason = "")
    {
        Domain = domain;
        Kind = kind;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown") : null;
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? kind.ToString();
        SuppressedBecauseDryRun = true;
        ActionID = string.Join("|", Domain.ToString(), Kind.ToString(), ObjectID ?? "", ArtifactID ?? "", Reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalDryRunAction other && Equals(other);
    public bool Equals(CanonicalDryRunAction? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
    public static bool operator ==(CanonicalDryRunAction l, CanonicalDryRunAction r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunAction l, CanonicalDryRunAction r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalDryRunRiskKind
{
    dryRunOnly,
    legacyRuntimeStillOwner,
    canonicalMoreAggressive,
    canonicalOnly,
    legacyOnly,
    unresolvedConflict,
    unsupportedObject
}

public sealed class CanonicalDryRunRisk : IEquatable<CanonicalDryRunRisk>
{
    public string Id => string.Join("|", Domain.ToString(), Kind.ToString(), Reason);
    public CanonicalProductionDomain Domain { get; }
    public CanonicalDryRunRiskKind Kind { get; }
    public string Reason { get; }

    public CanonicalDryRunRisk(CanonicalProductionDomain domain, CanonicalDryRunRiskKind kind, string reason)
    {
        Domain = domain;
        Kind = kind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? kind.ToString();
    }

    public override bool Equals(object? obj) => obj is CanonicalDryRunRisk other && Equals(other);
    public bool Equals(CanonicalDryRunRisk? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalDryRunRisk l, CanonicalDryRunRisk r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunRisk l, CanonicalDryRunRisk r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalDryRunBlockerKind
{
    missingProductionFilePort,
    missingProductionTransportPort,
    missingProductionUploadPort,
    missingProductionApplyPort,
    dryRunDivergence,
    unresolvedConflict,
    unsupportedObject,
    fullContentLeak,
    routeBypassRisk,
    pathEscapeRisk,
    uiLegacyRuntime,
    retryRuntimeNotMigrated,
    macPendingSyncLegacy,
    userDataMigrationNotDesigned
}

public sealed class CanonicalDryRunBlocker : IEquatable<CanonicalDryRunBlocker>
{
    public string Id => string.Join("|", Domain.ToString(), Kind.ToString(), Reason);
    public CanonicalProductionDomain Domain { get; }
    public CanonicalDryRunBlockerKind Kind { get; }
    public string Reason { get; }

    public CanonicalDryRunBlocker(CanonicalProductionDomain domain, CanonicalDryRunBlockerKind kind, string reason)
    {
        Domain = domain;
        Kind = kind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? kind.ToString();
    }

    public override bool Equals(object? obj) => obj is CanonicalDryRunBlocker other && Equals(other);
    public bool Equals(CanonicalDryRunBlocker? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalDryRunBlocker l, CanonicalDryRunBlocker r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunBlocker l, CanonicalDryRunBlocker r) => !l.Equals(r);
}

public sealed class CanonicalDryRunMigrationContext : IEquatable<CanonicalDryRunMigrationContext>
{
    public string DryRunID { get; }
    public bool LegacyRuntimeStillProductionOwner { get; }
    public bool RetryRuntimeMigrated { get; }
    public bool MacPendingSyncMigrated { get; }
    public bool UserDataMigrationDesigned { get; }
    public bool UiIntegrationMigrated { get; }

    public CanonicalDryRunMigrationContext(
        string? dryRunID = null,
        bool legacyRuntimeStillProductionOwner = true,
        bool retryRuntimeMigrated = false,
        bool macPendingSyncMigrated = false,
        bool userDataMigrationDesigned = false,
        bool uiIntegrationMigrated = false)
    {
        DryRunID = CanonicalProductionRedaction.SafeIdentifier(dryRunID ?? Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        LegacyRuntimeStillProductionOwner = legacyRuntimeStillProductionOwner;
        RetryRuntimeMigrated = retryRuntimeMigrated;
        MacPendingSyncMigrated = macPendingSyncMigrated;
        UserDataMigrationDesigned = userDataMigrationDesigned;
        UiIntegrationMigrated = uiIntegrationMigrated;
    }

    public override bool Equals(object? obj) => obj is CanonicalDryRunMigrationContext other && Equals(other);
    public bool Equals(CanonicalDryRunMigrationContext? other) =>
        other is not null &&
        DryRunID == other.DryRunID &&
        LegacyRuntimeStillProductionOwner == other.LegacyRuntimeStillProductionOwner &&
        RetryRuntimeMigrated == other.RetryRuntimeMigrated &&
        MacPendingSyncMigrated == other.MacPendingSyncMigrated &&
        UserDataMigrationDesigned == other.UserDataMigrationDesigned &&
        UiIntegrationMigrated == other.UiIntegrationMigrated;
    public override int GetHashCode() => HashCode.Combine(DryRunID, LegacyRuntimeStillProductionOwner,
        RetryRuntimeMigrated, MacPendingSyncMigrated, UserDataMigrationDesigned, UiIntegrationMigrated);
    public static bool operator ==(CanonicalDryRunMigrationContext l, CanonicalDryRunMigrationContext r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunMigrationContext l, CanonicalDryRunMigrationContext r) => !l.Equals(r);
}

public sealed class CanonicalDryRunReadinessReport : IEquatable<CanonicalDryRunReadinessReport>
{
    public CanonicalTimestamp GeneratedAt { get; }
    public List<CanonicalRuntimeReadinessStatus> States { get; }
    public CanonicalProductionPortReadiness PortReadiness { get; }
    public List<CanonicalDryRunBlocker> Blockers { get; }
    public bool EligibleForRuntimeSwitch { get; }
    public bool Retired { get; }

    public CanonicalDryRunReadinessReport(
        List<CanonicalRuntimeReadinessStatus> states,
        CanonicalProductionPortReadiness portReadiness,
        List<CanonicalDryRunBlocker> blockers,
        bool eligibleForRuntimeSwitch = false,
        bool retired = false,
        DateTime? generatedAt = null)
    {
        GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
        States = new HashSet<CanonicalRuntimeReadinessStatus>(states).OrderBy(s => s.ToString()).ToList();
        PortReadiness = portReadiness;
        Blockers = blockers.OrderBy(b => b.Id).ToList();
        EligibleForRuntimeSwitch = eligibleForRuntimeSwitch;
        Retired = retired;
    }

    public bool ProductionMigrationBlocked =>
        States.Contains(CanonicalRuntimeReadinessStatus.productionBlocked) ||
        Blockers.Count != 0 ||
        !EligibleForRuntimeSwitch;

    public override bool Equals(object? obj) => obj is CanonicalDryRunReadinessReport other && Equals(other);
    public bool Equals(CanonicalDryRunReadinessReport? other) =>
        other is not null &&
        GeneratedAt.Equals(other.GeneratedAt) &&
        States.SequenceEqual(other.States) &&
        PortReadiness.Equals(other.PortReadiness) &&
        Blockers.SequenceEqual(other.Blockers) &&
        EligibleForRuntimeSwitch == other.EligibleForRuntimeSwitch &&
        Retired == other.Retired;
    public override int GetHashCode() => HashCode.Combine(GeneratedAt, EligibleForRuntimeSwitch, Retired);
    public static bool operator ==(CanonicalDryRunReadinessReport l, CanonicalDryRunReadinessReport r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunReadinessReport l, CanonicalDryRunReadinessReport r) => !l.Equals(r);
}

public sealed class CanonicalDryRunMigrationPlan : IEquatable<CanonicalDryRunMigrationPlan>
{
    public string DryRunID { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }
    public CanonicalSyncPlan SyncPlan { get; }
    public CanonicalApplyPlan ApplyPlan { get; }
    public CanonicalLibrarySyncPlan LibraryPlan { get; }
    public List<CanonicalDryRunAction> Actions { get; }
    public List<CanonicalDryRunRisk> Risks { get; }
    public List<CanonicalDryRunBlocker> Blockers { get; }
    public CanonicalDryRunEquivalenceReport EquivalenceReport { get; }
    public CanonicalDryRunReadinessReport ReadinessReport { get; }
    public List<CanonicalProductionDiagnosticsEvent> Diagnostics { get; }

    public CanonicalDryRunMigrationPlan(
        string dryRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalSyncPlan syncPlan,
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan libraryPlan,
        List<CanonicalDryRunAction> actions,
        List<CanonicalDryRunRisk> risks,
        List<CanonicalDryRunBlocker> blockers,
        CanonicalDryRunEquivalenceReport equivalenceReport,
        CanonicalDryRunReadinessReport readinessReport,
        List<CanonicalProductionDiagnosticsEvent> diagnostics)
    {
        DryRunID = dryRunID;
        Trigger = trigger;
        SyncPlan = syncPlan;
        ApplyPlan = applyPlan;
        LibraryPlan = libraryPlan;
        Actions = actions;
        Risks = risks;
        Blockers = blockers;
        EquivalenceReport = equivalenceReport;
        ReadinessReport = readinessReport;
        Diagnostics = diagnostics;
    }

    public override bool Equals(object? obj) => obj is CanonicalDryRunMigrationPlan other && Equals(other);
    public bool Equals(CanonicalDryRunMigrationPlan? other) =>
        other is not null && DryRunID == other.DryRunID;
    public override int GetHashCode() => DryRunID.GetHashCode();
    public static bool operator ==(CanonicalDryRunMigrationPlan l, CanonicalDryRunMigrationPlan r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunMigrationPlan l, CanonicalDryRunMigrationPlan r) => !l.Equals(r);
}

public sealed class CanonicalDryRunMigrationPlanner
{
    public CanonicalDryRunMigrationPlan Plan(
        CanonicalProductionSnapshot local,
        CanonicalProductionSnapshot peer,
        CanonicalProductionPortSet ports,
        CanonicalRuntimeReadinessReport currentRuntimeReadiness,
        CanonicalSyncPlanTrigger trigger,
        CanonicalDryRunMigrationContext? context = null,
        DateTime? generatedAt = null)
    {
        var ctx = context ?? new CanonicalDryRunMigrationContext();
        var now = generatedAt ?? DateTime.UtcNow;
        var resolvedTrigger = ports.SyncClock?.TriggerContext(trigger) ?? trigger;

        var syncPlan = new CanonicalSyncPlanner().Plan(local.Manifest, peer.Manifest, resolvedTrigger);
        var applyPlan = new CanonicalApplyPlanner().Plan(local.Manifest, peer.Manifest, syncPlan, resolvedTrigger);
        var libraryPlan = new CanonicalLibrarySyncPlanner().Plan(local.Manifest, peer.Manifest, resolvedTrigger);

        var actions = Actions(syncPlan, applyPlan, libraryPlan);
        var portReadiness = ports.Readiness(now);
        var equivalence = EquivalenceReport(syncPlan, applyPlan, libraryPlan, local.LegacyActions, portReadiness, now);
        var dryRunEquivalence = new CanonicalDryRunEquivalenceReport(equivalence);

        var blockers = Blockers(portReadiness, equivalence, local, peer, applyPlan, libraryPlan, ctx);
        var risks = Risks(equivalence, blockers);
        var readiness = Readiness(currentRuntimeReadiness, portReadiness, equivalence, blockers, now);
        var diagnostics = Diagnostics(ctx.DryRunID, equivalence, readiness, blockers, now);

        return new CanonicalDryRunMigrationPlan(
            dryRunID: ctx.DryRunID,
            trigger: resolvedTrigger,
            syncPlan: syncPlan,
            applyPlan: applyPlan,
            libraryPlan: libraryPlan,
            actions: actions,
            risks: risks,
            blockers: blockers,
            equivalenceReport: dryRunEquivalence,
            readinessReport: readiness,
            diagnostics: diagnostics
        );
    }

    public static CanonicalLegacyEquivalenceReport EquivalenceReport(
        CanonicalSyncPlan syncPlan,
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan libraryPlan,
        CanonicalLegacyActionSnapshot localLegacyActions,
        CanonicalProductionPortReadiness portReadiness,
        DateTime? generatedAt = null)
    {
        var canonical = CanonicalActionIDs(syncPlan, applyPlan, libraryPlan);
        var conflictDomains = new HashSet<CanonicalLegacyEquivalenceDomain>();
        if (syncPlan.ConflictRecordingMetadata.Count != 0) conflictDomains.Add(CanonicalLegacyEquivalenceDomain.recordingMetadata);
        if (syncPlan.ConflictAudioArtifact.Count != 0) conflictDomains.Add(CanonicalLegacyEquivalenceDomain.recordingAudio);
        if (syncPlan.ConflictGeneratedArtifact.Count != 0) conflictDomains.Add(CanonicalLegacyEquivalenceDomain.generatedArtifacts);
        if (applyPlan.Conflicts.Count != 0 || libraryPlan.Conflicts.Count != 0) conflictDomains.Add(CanonicalLegacyEquivalenceDomain.conflicts);

        var allDomains = Enum.GetValues<CanonicalLegacyEquivalenceDomain>();
        var reports = allDomains.Select(domain =>
            DomainReport(domain,
                canonicalActionIDs: canonical.GetValueOrDefault(domain, new List<string>()),
                legacyActionIDs: localLegacyActions.ActionIDsFor(domain.ToProductionDomain()),
                conflictDomains: conflictDomains,
                portReadiness: portReadiness)
        ).ToList();

        return new CanonicalLegacyEquivalenceReport(reports, generatedAt);
    }

    private static CanonicalLegacyEquivalenceDomainReport DomainReport(
        CanonicalLegacyEquivalenceDomain domain,
        List<string> canonicalActionIDs,
        List<string> legacyActionIDs,
        HashSet<CanonicalLegacyEquivalenceDomain> conflictDomains,
        CanonicalProductionPortReadiness portReadiness)
    {
        var canonicalSet = new HashSet<string>(canonicalActionIDs);
        var legacySet = new HashSet<string>(legacyActionIDs);

        var missingPort = MissingPortBlocking(domain, portReadiness);
        if (missingPort != null)
        {
            var divergence = new CanonicalLegacyDivergence(
                domain, CanonicalLegacyEquivalenceStatus.blocked, CanonicalLegacyDivergenceSeverity.blocking,
                $"productionPortMissing:{missingPort}", canonicalActionIDs, legacyActionIDs);
            return new CanonicalLegacyEquivalenceDomainReport(domain, CanonicalLegacyEquivalenceStatus.blocked,
                canonicalActionIDs, legacyActionIDs, new List<CanonicalLegacyDivergence> { divergence });
        }

        if (conflictDomains.Contains(domain))
        {
            var divergence = new CanonicalLegacyDivergence(
                domain, CanonicalLegacyEquivalenceStatus.conflict, CanonicalLegacyDivergenceSeverity.blocking,
                "canonicalConflictRequiresManualReview", canonicalActionIDs, legacyActionIDs);
            return new CanonicalLegacyEquivalenceDomainReport(domain, CanonicalLegacyEquivalenceStatus.conflict,
                canonicalActionIDs, legacyActionIDs, new List<CanonicalLegacyDivergence> { divergence });
        }

        if (canonicalSet.SetEquals(legacySet))
        {
            return new CanonicalLegacyEquivalenceDomainReport(domain, CanonicalLegacyEquivalenceStatus.equivalent,
                canonicalActionIDs, legacyActionIDs);
        }

        if (canonicalSet.Count == 0 && legacySet.Count != 0)
        {
            if (domain == CanonicalLegacyEquivalenceDomain.recordingMetadata &&
                legacyActionIDs.All(IsSafeMetadataChurnSuppression))
            {
                var divergence = new CanonicalLegacyDivergence(
                    domain, CanonicalLegacyEquivalenceStatus.canonicalMoreConservative,
                    CanonicalLegacyDivergenceSeverity.info,
                    "legacyMetadataChurnSuppressed", canonicalActionIDs, legacyActionIDs);
                return new CanonicalLegacyEquivalenceDomainReport(domain,
                    CanonicalLegacyEquivalenceStatus.canonicalMoreConservative,
                    canonicalActionIDs, legacyActionIDs, new List<CanonicalLegacyDivergence> { divergence });
            }
            var legacyDivergence = new CanonicalLegacyDivergence(
                domain, CanonicalLegacyEquivalenceStatus.legacyOnly, CanonicalLegacyDivergenceSeverity.blocking,
                "legacyWouldActButCanonicalNoOp", canonicalActionIDs, legacyActionIDs);
            return new CanonicalLegacyEquivalenceDomainReport(domain, CanonicalLegacyEquivalenceStatus.legacyOnly,
                canonicalActionIDs, legacyActionIDs, new List<CanonicalLegacyDivergence> { legacyDivergence });
        }

        if (canonicalSet.Count != 0 && legacySet.Count == 0)
        {
            var status = domain == CanonicalLegacyEquivalenceDomain.recordingAudio ||
                         domain == CanonicalLegacyEquivalenceDomain.uploadRuntime
                ? CanonicalLegacyEquivalenceStatus.canonicalMoreAggressive
                : CanonicalLegacyEquivalenceStatus.canonicalOnly;
            var reason = status == CanonicalLegacyEquivalenceStatus.canonicalMoreAggressive
                ? "canonicalWouldUploadWhereLegacyNoOp" : "canonicalOnlyAction";
            var divergence = new CanonicalLegacyDivergence(
                domain, status, CanonicalLegacyDivergenceSeverity.blocking, reason,
                canonicalActionIDs, legacyActionIDs);
            return new CanonicalLegacyEquivalenceDomainReport(domain, status,
                canonicalActionIDs, legacyActionIDs, new List<CanonicalLegacyDivergence> { divergence });
        }

        var unknownDivergence = new CanonicalLegacyDivergence(
            domain, CanonicalLegacyEquivalenceStatus.unknown, CanonicalLegacyDivergenceSeverity.blocking,
            "canonicalLegacyActionSetMismatch", canonicalActionIDs, legacyActionIDs);
        return new CanonicalLegacyEquivalenceDomainReport(domain, CanonicalLegacyEquivalenceStatus.unknown,
            canonicalActionIDs, legacyActionIDs, new List<CanonicalLegacyDivergence> { unknownDivergence });
    }

    private static CanonicalProductionPortKind? MissingPortBlocking(
        CanonicalLegacyEquivalenceDomain domain, CanonicalProductionPortReadiness portReadiness)
    {
        CanonicalProductionPortKind? required = domain switch
        {
            CanonicalLegacyEquivalenceDomain.recordingMetadata => CanonicalProductionPortKind.file,
            CanonicalLegacyEquivalenceDomain.generatedArtifacts => CanonicalProductionPortKind.file,
            CanonicalLegacyEquivalenceDomain.folders => CanonicalProductionPortKind.file,
            CanonicalLegacyEquivalenceDomain.studyItems => CanonicalProductionPortKind.file,
            CanonicalLegacyEquivalenceDomain.standaloneNotes => CanonicalProductionPortKind.file,
            CanonicalLegacyEquivalenceDomain.tombstones => CanonicalProductionPortKind.file,
            CanonicalLegacyEquivalenceDomain.fileRuntime => CanonicalProductionPortKind.file,
            CanonicalLegacyEquivalenceDomain.recordingAudio => CanonicalProductionPortKind.upload,
            CanonicalLegacyEquivalenceDomain.uploadRuntime => CanonicalProductionPortKind.upload,
            CanonicalLegacyEquivalenceDomain.transportRuntime => CanonicalProductionPortKind.transport,
            CanonicalLegacyEquivalenceDomain.apply => CanonicalProductionPortKind.apply,
            CanonicalLegacyEquivalenceDomain.conflicts => CanonicalProductionPortKind.apply,
            _ => null
        };
        if (required != null && portReadiness.MissingPorts.Contains(required.Value))
            return required;
        return null;
    }

    private static bool IsSafeMetadataChurnSuppression(string actionID)
    {
        var lower = actionID.ToLowerInvariant();
        return lower.Contains("metadatachurn")
            || lower.Contains("legacywoulduploadmetadatabutcanonicalnoop")
            || lower.Contains("canonicalmetadatahashconverged");
    }

    private static Dictionary<CanonicalLegacyEquivalenceDomain, List<string>> CanonicalActionIDs(
        CanonicalSyncPlan syncPlan, CanonicalApplyPlan applyPlan, CanonicalLibrarySyncPlan libraryPlan)
    {
        var result = new Dictionary<CanonicalLegacyEquivalenceDomain, List<string>>();

        Append(syncPlan.UploadRecordingMetadata.Select(e => $"recordingMetadataSend:{e.ObjectID}").ToList(),
            CanonicalLegacyEquivalenceDomain.recordingMetadata, result);
        Append(syncPlan.DownloadRecordingMetadata.Select(e => $"recordingMetadataApply:{e.ObjectID}").ToList(),
            CanonicalLegacyEquivalenceDomain.recordingMetadata, result);
        Append(syncPlan.UploadAudioArtifact.Select(e => $"recordingAudioUpload:{e.ObjectID}:{e.ArtifactID ?? "audio"}").ToList(),
            CanonicalLegacyEquivalenceDomain.recordingAudio, result);
        Append(syncPlan.DownloadGeneratedArtifact.Select(e =>
            $"generatedArtifactDownload:{e.ObjectID}:{e.ArtifactID ?? e.Kind?.ToString() ?? "artifact"}").ToList(),
            CanonicalLegacyEquivalenceDomain.generatedArtifacts, result);

        Append(libraryPlan.Actions
            .Where(a => a.ObjectKind == CanonicalObjectKind.folder)
            .Select(a => $"folder:{a.Kind}:{a.ObjectID.RawValue}").ToList(),
            CanonicalLegacyEquivalenceDomain.folders, result);
        Append(libraryPlan.Actions
            .Where(a => a.ObjectKind == CanonicalObjectKind.standaloneStudyItem ||
                        a.ObjectKind == CanonicalObjectKind.recordingAssociatedStudyItem)
            .Select(a => $"studyItem:{a.Kind}:{a.ObjectID.RawValue}").ToList(),
            CanonicalLegacyEquivalenceDomain.studyItems, result);
        Append(libraryPlan.Actions
            .Where(a => a.ObjectKind == CanonicalObjectKind.standaloneNote)
            .Select(a => $"standaloneNote:{a.Kind}:{a.ObjectID.RawValue}").ToList(),
            CanonicalLegacyEquivalenceDomain.standaloneNotes, result);

        Append(applyPlan.Tombstones.Select(t => $"tombstone:{t.Target.ObjectID}").Concat(
            libraryPlan.Tombstones.Select(t => $"libraryTombstone:{t.ObjectID.RawValue}")).ToList(),
            CanonicalLegacyEquivalenceDomain.tombstones, result);
        Append(applyPlan.Conflicts.Select(c => c.ConflictID).Concat(
            libraryPlan.Conflicts.Select(c => c.ConflictID)).ToList(),
            CanonicalLegacyEquivalenceDomain.conflicts, result);
        Append(applyPlan.Actions.Select(a => a.ActionID).Concat(
            libraryPlan.ApplyActions.Select(a => a.ActionID)).ToList(),
            CanonicalLegacyEquivalenceDomain.apply, result);

        if (syncPlan.UploadAudioArtifact.Count != 0)
            Append(syncPlan.UploadAudioArtifact.Select(e => $"uploadRuntime:{e.ObjectID}").ToList(),
                CanonicalLegacyEquivalenceDomain.uploadRuntime, result);

        return result.ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value).OrderBy(s => s).ToList());
    }

    private static void Append(List<string> ids, CanonicalLegacyEquivalenceDomain domain,
        Dictionary<CanonicalLegacyEquivalenceDomain, List<string>> result)
    {
        if (ids.Count == 0) return;
        if (!result.ContainsKey(domain))
            result[domain] = new List<string>();
        result[domain].AddRange(ids);
    }

    private static List<CanonicalDryRunAction> Actions(
        CanonicalSyncPlan syncPlan, CanonicalApplyPlan applyPlan, CanonicalLibrarySyncPlan libraryPlan)
    {
        var actions = new List<CanonicalDryRunAction>();

        actions.AddRange(syncPlan.UploadRecordingMetadata.Select(e =>
            new CanonicalDryRunAction(CanonicalProductionDomain.recordingMetadata, CanonicalDryRunActionKind.wouldSend,
                e.ObjectID, reason: e.Reason.ToString())));
        actions.AddRange(syncPlan.DownloadRecordingMetadata.Select(e =>
            new CanonicalDryRunAction(CanonicalProductionDomain.recordingMetadata, CanonicalDryRunActionKind.wouldApply,
                e.ObjectID, reason: e.Reason.ToString())));
        actions.AddRange(syncPlan.UploadAudioArtifact.Select(e =>
            new CanonicalDryRunAction(CanonicalProductionDomain.recordingAudio, CanonicalDryRunActionKind.wouldUpload,
                e.ObjectID, e.ArtifactID, e.Reason.ToString())));
        actions.AddRange(syncPlan.DownloadGeneratedArtifact.Select(e =>
            new CanonicalDryRunAction(CanonicalProductionDomain.generatedArtifacts, CanonicalDryRunActionKind.wouldDownload,
                e.ObjectID, e.ArtifactID, e.Reason.ToString())));
        actions.AddRange(applyPlan.Actions.Select(a =>
            new CanonicalDryRunAction(CanonicalProductionDomain.apply,
                a.Kind == CanonicalApplyActionKind.conflictRecord ? CanonicalDryRunActionKind.wouldRecordConflict : CanonicalDryRunActionKind.wouldApply,
                a.Target.ObjectID, a.Target.ArtifactID, a.Reason)));
        actions.AddRange(libraryPlan.ApplyActions.Select(a =>
            new CanonicalDryRunAction(CanonicalProductionDomain.apply,
                a.Kind == CanonicalApplyActionKind.conflictRecord ? CanonicalDryRunActionKind.wouldRecordConflict : CanonicalDryRunActionKind.wouldApply,
                a.Target.ObjectID, a.Target.ArtifactID, a.Reason)));
        actions.AddRange(libraryPlan.Actions.Select(a =>
        {
            var domain = a.ObjectKind switch
            {
                CanonicalObjectKind.folder => CanonicalProductionDomain.folders,
                CanonicalObjectKind.standaloneNote => CanonicalProductionDomain.standaloneNotes,
                _ => CanonicalProductionDomain.studyItems
            };
            return new CanonicalDryRunAction(domain, CanonicalDryRunActionKind.wouldApply,
                a.ObjectID.RawValue, reason: a.Reason);
        }));

        return actions.OrderBy(a => a.ActionID).ToList();
    }

    private static List<CanonicalDryRunBlocker> Blockers(
        CanonicalProductionPortReadiness portReadiness,
        CanonicalLegacyEquivalenceReport equivalence,
        CanonicalProductionSnapshot local,
        CanonicalProductionSnapshot peer,
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan libraryPlan,
        CanonicalDryRunMigrationContext context)
    {
        var blockers = new List<CanonicalDryRunBlocker>();

        if (portReadiness.MissingPorts.Contains(CanonicalProductionPortKind.file))
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.fileRuntime,
                CanonicalDryRunBlockerKind.missingProductionFilePort, "filePortMissing"));
        if (portReadiness.MissingPorts.Contains(CanonicalProductionPortKind.transport))
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.transportRuntime,
                CanonicalDryRunBlockerKind.missingProductionTransportPort, "transportPortMissing"));
        if (portReadiness.MissingPorts.Contains(CanonicalProductionPortKind.upload))
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.uploadRuntime,
                CanonicalDryRunBlockerKind.missingProductionUploadPort, "uploadPortMissing"));
        if (portReadiness.MissingPorts.Contains(CanonicalProductionPortKind.apply))
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.apply,
                CanonicalDryRunBlockerKind.missingProductionApplyPort, "applyPortMissing"));

        blockers.AddRange(equivalence.Divergences.Where(d => d.IsBlocking).Select(d =>
            new CanonicalDryRunBlocker(d.Domain.ToProductionDomain(), CanonicalDryRunBlockerKind.dryRunDivergence, d.Reason)));

        if (applyPlan.Conflicts.Count != 0 || libraryPlan.Conflicts.Count != 0)
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.conflicts,
                CanonicalDryRunBlockerKind.unresolvedConflict, "manualReviewRequired"));

        foreach (var fact in local.UnsupportedFacts.Concat(peer.UnsupportedFacts))
            blockers.Add(new CanonicalDryRunBlocker(fact.Domain, CanonicalDryRunBlockerKind.unsupportedObject, fact.Reason));

        if (!context.UiIntegrationMigrated)
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.uiIntegration,
                CanonicalDryRunBlockerKind.uiLegacyRuntime, "legacyUIStillRuntimeOwner"));
        if (!context.RetryRuntimeMigrated)
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.uploadRuntime,
                CanonicalDryRunBlockerKind.retryRuntimeNotMigrated, "legacyRetryRuntimePreserved"));
        if (!context.MacPendingSyncMigrated)
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.transportRuntime,
                CanonicalDryRunBlockerKind.macPendingSyncLegacy, "macPendingSyncStillLegacy"));
        if (!context.UserDataMigrationDesigned)
            blockers.Add(new CanonicalDryRunBlocker(CanonicalProductionDomain.inventory,
                CanonicalDryRunBlockerKind.userDataMigrationNotDesigned, "noUserDataMigrationDesign"));

        return blockers.GroupBy(b => b.Id).Select(g => g.First()).OrderBy(b => b.Id).ToList();
    }

    private static List<CanonicalDryRunRisk> Risks(
        CanonicalLegacyEquivalenceReport equivalence, List<CanonicalDryRunBlocker> blockers)
    {
        var risks = new List<CanonicalDryRunRisk>
        {
            new(CanonicalProductionDomain.apply, CanonicalDryRunRiskKind.dryRunOnly, "productionMutationSuppressed")
        };

        risks.AddRange(equivalence.Divergences.Select(d =>
        {
            var kind = d.Status switch
            {
                CanonicalLegacyEquivalenceStatus.canonicalMoreAggressive => CanonicalDryRunRiskKind.canonicalMoreAggressive,
                CanonicalLegacyEquivalenceStatus.canonicalOnly => CanonicalDryRunRiskKind.canonicalOnly,
                CanonicalLegacyEquivalenceStatus.legacyOnly => CanonicalDryRunRiskKind.legacyOnly,
                CanonicalLegacyEquivalenceStatus.conflict => CanonicalDryRunRiskKind.unresolvedConflict,
                CanonicalLegacyEquivalenceStatus.unsupported => CanonicalDryRunRiskKind.unsupportedObject,
                _ => CanonicalDryRunRiskKind.legacyRuntimeStillOwner
            };
            return new CanonicalDryRunRisk(d.Domain.ToProductionDomain(), kind, d.Reason);
        }));

        risks.AddRange(blockers.Select(b =>
            new CanonicalDryRunRisk(b.Domain, CanonicalDryRunRiskKind.legacyRuntimeStillOwner, b.Reason)));

        return risks.GroupBy(r => r.Id).Select(g => g.First()).OrderBy(r => r.Id).ToList();
    }

    private static CanonicalDryRunReadinessReport Readiness(
        CanonicalRuntimeReadinessReport runtimeReadiness,
        CanonicalProductionPortReadiness portReadiness,
        CanonicalLegacyEquivalenceReport equivalence,
        List<CanonicalDryRunBlocker> blockers,
        DateTime generatedAt)
    {
        var states = new List<CanonicalRuntimeReadinessStatus> { CanonicalRuntimeReadinessStatus.notEvaluated };
        var offlineDomains = new[]
        {
            CanonicalRuntimeReadinessDomain.fileRuntime,
            CanonicalRuntimeReadinessDomain.transportRuntime,
            CanonicalRuntimeReadinessDomain.uploadRuntime,
            CanonicalRuntimeReadinessDomain.applyExecutor,
            CanonicalRuntimeReadinessDomain.conflictResolver,
            CanonicalRuntimeReadinessDomain.simulationHarness
        };

        if (offlineDomains.All(d => runtimeReadiness.StatusFor(d) == CanonicalRuntimeReadinessStatus.offlineRuntimeComplete))
            states.Add(CanonicalRuntimeReadinessStatus.offlineKernelReady);

        if (portReadiness.MissingPorts.Count == 0)
        {
            states.Add(CanonicalRuntimeReadinessStatus.productionPortsDeclared);
            states.Add(CanonicalRuntimeReadinessStatus.dryRunAvailable);
        }
        else
        {
            states.Add(CanonicalRuntimeReadinessStatus.productionAdapterMissing);
        }

        if (!equivalence.HasBlockingDivergence)
            states.Add(CanonicalRuntimeReadinessStatus.dryRunEquivalent);

        states.Add(CanonicalRuntimeReadinessStatus.productionBlocked);

        if (portReadiness.HasAllRequiredDryRunPorts && !equivalence.HasBlockingDivergence)
            states.Add(CanonicalRuntimeReadinessStatus.eligibleForManualMigrationDesign);

        return new CanonicalDryRunReadinessReport(states, portReadiness, blockers,
            eligibleForRuntimeSwitch: false, retired: false, generatedAt: generatedAt);
    }

    private static List<CanonicalProductionDiagnosticsEvent> Diagnostics(
        string dryRunID,
        CanonicalLegacyEquivalenceReport equivalence,
        CanonicalDryRunReadinessReport readiness,
        List<CanonicalDryRunBlocker> blockers,
        DateTime generatedAt)
    {
        var events = new List<CanonicalProductionDiagnosticsEvent>
        {
            new(CanonicalProductionDiagnosticsEventKind.canonicalDryRunStarted, action: dryRunID,
                reason: "dryRunMigrationPlanner", generatedAt: generatedAt),
            new(CanonicalProductionDiagnosticsEventKind.canonicalProductionPortsDeclared,
                reason: $"declared={string.Join(",", readiness.PortReadiness.DeclaredPorts.Where(kv => kv.Value).Select(kv => kv.Key.ToString()).OrderBy(k => k))}",
                generatedAt: generatedAt)
        };

        foreach (var missing in readiness.PortReadiness.MissingPorts)
            events.Add(new CanonicalProductionDiagnosticsEvent(
                CanonicalProductionDiagnosticsEventKind.canonicalPortMissing,
                domain: DomainForMissingPort(missing), blocker: missing.ToString(),
                reason: "portMissing", generatedAt: generatedAt));

        foreach (var report in equivalence.DomainReports)
            events.Add(new CanonicalProductionDiagnosticsEvent(
                report.IsBlocking ? CanonicalProductionDiagnosticsEventKind.canonicalLegacyDivergent : CanonicalProductionDiagnosticsEventKind.canonicalLegacyEquivalent,
                domain: report.Domain.ToProductionDomain(), action: report.Status.ToString(),
                reason: report.Divergences.FirstOrDefault()?.Reason ?? "equivalent", generatedAt: generatedAt));

        foreach (var blocker in blockers)
            events.Add(new CanonicalProductionDiagnosticsEvent(
                CanonicalProductionDiagnosticsEventKind.canonicalDryRunBlocked,
                domain: blocker.Domain, blocker: blocker.Kind.ToString(),
                reason: blocker.Reason, generatedAt: generatedAt));

        if (equivalence.HasBlockingDivergence)
            events.Add(new CanonicalProductionDiagnosticsEvent(
                CanonicalProductionDiagnosticsEventKind.canonicalDryRunDivergenceDetected,
                reason: "blockingDivergence", generatedAt: generatedAt));

        if (readiness.ProductionMigrationBlocked)
            events.Add(new CanonicalProductionDiagnosticsEvent(
                CanonicalProductionDiagnosticsEventKind.canonicalProductionMigrationBlocked,
                reason: "runtimeSwitchFalse", generatedAt: generatedAt));

        if (readiness.States.Contains(CanonicalRuntimeReadinessStatus.eligibleForManualMigrationDesign))
            events.Add(new CanonicalProductionDiagnosticsEvent(
                CanonicalProductionDiagnosticsEventKind.canonicalEligibleForManualMigrationDesign,
                reason: "manualDesignOnly", generatedAt: generatedAt));

        events.Add(new CanonicalProductionDiagnosticsEvent(
            CanonicalProductionDiagnosticsEventKind.canonicalDryRunCompleted,
            reason: "dryRunOnly", generatedAt: generatedAt));

        return events;
    }

    private static CanonicalProductionDomain DomainForMissingPort(CanonicalProductionPortKind port)
    {
        return port switch
        {
            CanonicalProductionPortKind.file => CanonicalProductionDomain.fileRuntime,
            CanonicalProductionPortKind.transport => CanonicalProductionDomain.transportRuntime,
            CanonicalProductionPortKind.upload => CanonicalProductionDomain.uploadRuntime,
            CanonicalProductionPortKind.apply => CanonicalProductionDomain.apply,
            _ => CanonicalProductionDomain.inventory
        };
    }
}
