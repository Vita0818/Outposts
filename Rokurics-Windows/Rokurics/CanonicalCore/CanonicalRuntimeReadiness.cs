using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRuntimeReadinessDomain
{
    fileRuntime,
    transportRuntime,
    uploadRuntime,
    applyExecutor,
    conflictResolver,
    simulationHarness,
    productionMigration
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRuntimeReadinessStatus
{
    notEvaluated,
    notStarted,
    semanticsModeled,
    offlineRuntimeComplete,
    offlineKernelReady,
    productionPortsDeclared,
    dryRunAvailable,
    dryRunEquivalent,
    productionAdapterMissing,
    productionBlocked,
    eligibleForManualMigrationDesign,
    eligibleForShadowMigration,
    eligibleForRuntimeSwitch,
    retired,
    blockedForProduction,
    failed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRuntimeReadinessBlockerKind
{
    rootBindingMissing,
    hashVerificationMissing,
    routeValidationMissing,
    resumableStateMissing,
    applyExecutorMissing,
    conflictResolverMissing,
    harnessMissing,
    legacyProductionOwner
}

public sealed class CanonicalRuntimeReadinessBlocker : IEquatable<CanonicalRuntimeReadinessBlocker>
{
    public string Id => string.Join("|", Domain.ToString(), Kind.ToString(), Detail ?? "");

    public CanonicalRuntimeReadinessDomain Domain { get; }
    public CanonicalRuntimeReadinessBlockerKind Kind { get; }
    public string? Detail { get; }

    public CanonicalRuntimeReadinessBlocker(
        CanonicalRuntimeReadinessDomain domain,
        CanonicalRuntimeReadinessBlockerKind kind,
        string? detail = null)
    {
        Domain = domain;
        Kind = kind;
        Detail = detail?.Trim().NilIfEmpty();
    }

    public override bool Equals(object? obj) =>
        obj is CanonicalRuntimeReadinessBlocker other && Equals(other);
    public bool Equals(CanonicalRuntimeReadinessBlocker? other) =>
        other is not null &&
        Domain == other.Domain &&
        Kind == other.Kind &&
        Detail == other.Detail;
    public override int GetHashCode() =>
        HashCode.Combine(Domain, Kind, Detail);
    public static bool operator ==(CanonicalRuntimeReadinessBlocker left, CanonicalRuntimeReadinessBlocker right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalRuntimeReadinessBlocker left, CanonicalRuntimeReadinessBlocker right) =>
        !left.Equals(right);
}

public sealed class CanonicalRuntimeReadinessEvidence : IEquatable<CanonicalRuntimeReadinessEvidence>
{
    public bool FileRootBinding { get; }
    public bool FileHashVerification { get; }
    public bool TransportRouteValidation { get; }
    public bool UploadResumableState { get; }
    public bool ApplyExecutor { get; }
    public bool ConflictResolver { get; }
    public bool TwoNodeHarness { get; }
    public bool ProductionStillLegacyOwned { get; }

    public CanonicalRuntimeReadinessEvidence(
        bool fileRootBinding = false,
        bool fileHashVerification = false,
        bool transportRouteValidation = false,
        bool uploadResumableState = false,
        bool applyExecutor = false,
        bool conflictResolver = false,
        bool twoNodeHarness = false,
        bool productionStillLegacyOwned = true)
    {
        FileRootBinding = fileRootBinding;
        FileHashVerification = fileHashVerification;
        TransportRouteValidation = transportRouteValidation;
        UploadResumableState = uploadResumableState;
        ApplyExecutor = applyExecutor;
        ConflictResolver = conflictResolver;
        TwoNodeHarness = twoNodeHarness;
        ProductionStillLegacyOwned = productionStillLegacyOwned;
    }

    public override bool Equals(object? obj) =>
        obj is CanonicalRuntimeReadinessEvidence other && Equals(other);
    public bool Equals(CanonicalRuntimeReadinessEvidence? other) =>
        other is not null &&
        FileRootBinding == other.FileRootBinding &&
        FileHashVerification == other.FileHashVerification &&
        TransportRouteValidation == other.TransportRouteValidation &&
        UploadResumableState == other.UploadResumableState &&
        ApplyExecutor == other.ApplyExecutor &&
        ConflictResolver == other.ConflictResolver &&
        TwoNodeHarness == other.TwoNodeHarness &&
        ProductionStillLegacyOwned == other.ProductionStillLegacyOwned;
    public override int GetHashCode() =>
        HashCode.Combine(FileRootBinding, FileHashVerification, TransportRouteValidation,
            UploadResumableState, ApplyExecutor, ConflictResolver, TwoNodeHarness, ProductionStillLegacyOwned);
    public static bool operator ==(CanonicalRuntimeReadinessEvidence left, CanonicalRuntimeReadinessEvidence right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalRuntimeReadinessEvidence left, CanonicalRuntimeReadinessEvidence right) =>
        !left.Equals(right);
}

public sealed class CanonicalRuntimeReadinessReport : IEquatable<CanonicalRuntimeReadinessReport>
{
    public CanonicalTimestamp GeneratedAt { get; }
    public Dictionary<CanonicalRuntimeReadinessDomain, CanonicalRuntimeReadinessStatus> Statuses { get; }
    public CanonicalRuntimeReadinessBlocker[] Blockers { get; }

    public CanonicalRuntimeReadinessReport(
        CanonicalTimestamp generatedAt,
        Dictionary<CanonicalRuntimeReadinessDomain, CanonicalRuntimeReadinessStatus> statuses,
        CanonicalRuntimeReadinessBlocker[] blockers)
    {
        GeneratedAt = generatedAt;
        Statuses = statuses;
        Blockers = blockers ?? Array.Empty<CanonicalRuntimeReadinessBlocker>();
    }

    public CanonicalRuntimeReadinessStatus Status(CanonicalRuntimeReadinessDomain domain) =>
        Statuses.TryGetValue(domain, out var status) ? status : CanonicalRuntimeReadinessStatus.notStarted;

    public override bool Equals(object? obj) =>
        obj is CanonicalRuntimeReadinessReport other && Equals(other);
    public bool Equals(CanonicalRuntimeReadinessReport? other) =>
        other is not null &&
        GeneratedAt.Equals(other.GeneratedAt);
    public override int GetHashCode() => GeneratedAt.GetHashCode();
    public static bool operator ==(CanonicalRuntimeReadinessReport left, CanonicalRuntimeReadinessReport right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalRuntimeReadinessReport left, CanonicalRuntimeReadinessReport right) =>
        !left.Equals(right);
}

public sealed class CanonicalRuntimeReadinessEvaluator
{
    public CanonicalRuntimeReadinessReport Evaluate(
        CanonicalRuntimeReadinessEvidence evidence,
        DateTime? generatedAt = null)
    {
        var genAt = generatedAt ?? DateTime.UtcNow;
        var statuses = Enum.GetValues<CanonicalRuntimeReadinessDomain>()
            .ToDictionary(d => d, _ => CanonicalRuntimeReadinessStatus.notStarted);
        var blockers = new List<CanonicalRuntimeReadinessBlocker>();

        Set(
            CanonicalRuntimeReadinessDomain.fileRuntime,
            passed: evidence.FileRootBinding && evidence.FileHashVerification,
            missing: new CanonicalRuntimeReadinessBlockerKind?[]
            {
                evidence.FileRootBinding ? null : CanonicalRuntimeReadinessBlockerKind.rootBindingMissing,
                evidence.FileHashVerification ? null : CanonicalRuntimeReadinessBlockerKind.hashVerificationMissing
            },
            statuses: statuses,
            blockers: blockers);
        Set(
            CanonicalRuntimeReadinessDomain.transportRuntime,
            passed: evidence.TransportRouteValidation,
            missing: new CanonicalRuntimeReadinessBlockerKind?[]
            {
                evidence.TransportRouteValidation ? null : CanonicalRuntimeReadinessBlockerKind.routeValidationMissing
            },
            statuses: statuses,
            blockers: blockers);
        Set(
            CanonicalRuntimeReadinessDomain.uploadRuntime,
            passed: evidence.UploadResumableState,
            missing: new CanonicalRuntimeReadinessBlockerKind?[]
            {
                evidence.UploadResumableState ? null : CanonicalRuntimeReadinessBlockerKind.resumableStateMissing
            },
            statuses: statuses,
            blockers: blockers);
        Set(
            CanonicalRuntimeReadinessDomain.applyExecutor,
            passed: evidence.ApplyExecutor,
            missing: new CanonicalRuntimeReadinessBlockerKind?[]
            {
                evidence.ApplyExecutor ? null : CanonicalRuntimeReadinessBlockerKind.applyExecutorMissing
            },
            statuses: statuses,
            blockers: blockers);
        Set(
            CanonicalRuntimeReadinessDomain.conflictResolver,
            passed: evidence.ConflictResolver,
            missing: new CanonicalRuntimeReadinessBlockerKind?[]
            {
                evidence.ConflictResolver ? null : CanonicalRuntimeReadinessBlockerKind.conflictResolverMissing
            },
            statuses: statuses,
            blockers: blockers);
        Set(
            CanonicalRuntimeReadinessDomain.simulationHarness,
            passed: evidence.TwoNodeHarness,
            missing: new CanonicalRuntimeReadinessBlockerKind?[]
            {
                evidence.TwoNodeHarness ? null : CanonicalRuntimeReadinessBlockerKind.harnessMissing
            },
            statuses: statuses,
            blockers: blockers);

        if (evidence.ProductionStillLegacyOwned)
        {
            statuses[CanonicalRuntimeReadinessDomain.productionMigration] =
                CanonicalRuntimeReadinessStatus.blockedForProduction;
            blockers.Add(new CanonicalRuntimeReadinessBlocker(
                domain: CanonicalRuntimeReadinessDomain.productionMigration,
                kind: CanonicalRuntimeReadinessBlockerKind.legacyProductionOwner,
                detail: "offlineRuntimeOnly"));
        }
        else
        {
            statuses[CanonicalRuntimeReadinessDomain.productionMigration] =
                CanonicalRuntimeReadinessStatus.offlineRuntimeComplete;
        }

        return new CanonicalRuntimeReadinessReport(
            generatedAt: new CanonicalTimestamp(genAt),
            statuses: statuses,
            blockers: blockers.ToArray());
    }

    private static void Set(
        CanonicalRuntimeReadinessDomain domain,
        bool passed,
        CanonicalRuntimeReadinessBlockerKind?[] missing,
        Dictionary<CanonicalRuntimeReadinessDomain, CanonicalRuntimeReadinessStatus> statuses,
        List<CanonicalRuntimeReadinessBlocker> blockers)
    {
        statuses[domain] = passed
            ? CanonicalRuntimeReadinessStatus.offlineRuntimeComplete
            : CanonicalRuntimeReadinessStatus.failed;
        foreach (var kind in missing)
        {
            if (kind.HasValue)
                blockers.Add(new CanonicalRuntimeReadinessBlocker(domain: domain, kind: kind.Value));
        }
    }
}
