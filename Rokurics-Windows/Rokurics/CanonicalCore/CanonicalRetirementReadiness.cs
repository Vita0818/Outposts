using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRetirementDomain
{
    recordingMetadata,
    recordingAudio,
    generatedArtifacts,
    folders,
    studyItems,
    tombstones,
    conflicts,
    apply,
    transferState,
    objectProjection,
    inventory,
    transport,
    uploadRuntime,
    physicalStorage
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRetirementStatus
{
    notStarted,
    shadowOnly,
    planningOnly,
    applyBridged,
    semanticsComplete,
    runtimeComplete,
    readyToRetireLegacy,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRetirementBlockerKind
{
    missingCanonicalManifest,
    unsupportedObjectKinds,
    fallbackUsed,
    conflictsUnresolved,
    applyBridgeMissing,
    transferStateUnmapped,
    uiStillReadsLegacyStatus,
    routeStillLegacy,
    physicalStoreStillLegacy
}

public sealed class CanonicalRetirementBlocker : IEquatable<CanonicalRetirementBlocker>
{
    public string Id => string.Join("|", Domain.ToString(), Kind.ToString(), Detail ?? "");
    public CanonicalRetirementDomain Domain { get; }
    public CanonicalRetirementBlockerKind Kind { get; }
    public string? Detail { get; }

    public CanonicalRetirementBlocker(
        CanonicalRetirementDomain domain,
        CanonicalRetirementBlockerKind kind,
        string? detail = null)
    {
        Domain = domain;
        Kind = kind;
        Detail = detail;
    }

    public override bool Equals(object? obj) => obj is CanonicalRetirementBlocker other && Equals(other);
    public bool Equals(CanonicalRetirementBlocker? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalRetirementBlocker l, CanonicalRetirementBlocker r) => l.Equals(r);
    public static bool operator !=(CanonicalRetirementBlocker l, CanonicalRetirementBlocker r) => !l.Equals(r);
}

public sealed class CanonicalRetirementReadinessReport : IEquatable<CanonicalRetirementReadinessReport>
{
    public CanonicalTimestamp GeneratedAt { get; }
    public Dictionary<CanonicalRetirementDomain, CanonicalRetirementStatus> Statuses { get; }
    public List<CanonicalRetirementBlocker> Blockers { get; }

    public CanonicalRetirementReadinessReport(
        CanonicalTimestamp generatedAt,
        Dictionary<CanonicalRetirementDomain, CanonicalRetirementStatus> statuses,
        List<CanonicalRetirementBlocker> blockers)
    {
        GeneratedAt = generatedAt;
        Statuses = statuses;
        Blockers = blockers;
    }

    public CanonicalRetirementStatus StatusFor(CanonicalRetirementDomain domain)
    {
        return Statuses.GetValueOrDefault(domain, CanonicalRetirementStatus.notStarted);
    }

    public override bool Equals(object? obj) => obj is CanonicalRetirementReadinessReport other && Equals(other);
    public bool Equals(CanonicalRetirementReadinessReport? other) =>
        other is not null &&
        GeneratedAt.Equals(other.GeneratedAt) &&
        DictionaryEquals(Statuses, other.Statuses) &&
        Blockers.SequenceEqual(other.Blockers);
    public override int GetHashCode() => HashCode.Combine(GeneratedAt,
        string.Join(",", Statuses.Select(kv => $"{kv.Key}:{kv.Value}")),
        string.Join(",", Blockers.Select(b => b.Id)));
    public static bool operator ==(CanonicalRetirementReadinessReport l, CanonicalRetirementReadinessReport r) => l.Equals(r);
    public static bool operator !=(CanonicalRetirementReadinessReport l, CanonicalRetirementReadinessReport r) => !l.Equals(r);

    private static bool DictionaryEquals(
        Dictionary<CanonicalRetirementDomain, CanonicalRetirementStatus> a,
        Dictionary<CanonicalRetirementDomain, CanonicalRetirementStatus> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
        }
        return true;
    }
}

public sealed class CanonicalRetirementReadinessEvaluator
{
    public CanonicalRetirementReadinessReport Evaluate(
        CanonicalManifest? manifest,
        CanonicalLibrarySyncPlan? libraryPlan,
        CanonicalApplyPlan? applyPlan,
        CanonicalTransferProjection? transferProjection,
        CanonicalInventoryCoverageReport? inventoryCoverage,
        bool fallbackUsed,
        DateTime? generatedAt = null)
    {
        var statuses = Enum.GetValues<CanonicalRetirementDomain>()
            .ToDictionary(d => d, _ => CanonicalRetirementStatus.notStarted);
        var blockers = new List<CanonicalRetirementBlocker>();

        if (manifest == null)
        {
            Add(CanonicalRetirementDomain.inventory, CanonicalRetirementBlockerKind.missingCanonicalManifest,
                "manifestMissing", statuses, blockers);
        }
        else
        {
            statuses[CanonicalRetirementDomain.recordingMetadata] = applyPlan == null
                ? CanonicalRetirementStatus.planningOnly : CanonicalRetirementStatus.applyBridged;
            statuses[CanonicalRetirementDomain.recordingAudio] = CanonicalRetirementStatus.planningOnly;
            statuses[CanonicalRetirementDomain.generatedArtifacts] = applyPlan == null
                ? CanonicalRetirementStatus.planningOnly : CanonicalRetirementStatus.applyBridged;
            statuses[CanonicalRetirementDomain.folders] = libraryPlan == null
                ? CanonicalRetirementStatus.shadowOnly : CanonicalRetirementStatus.applyBridged;
            statuses[CanonicalRetirementDomain.studyItems] = libraryPlan == null
                ? CanonicalRetirementStatus.shadowOnly : CanonicalRetirementStatus.applyBridged;
            statuses[CanonicalRetirementDomain.tombstones] =
                (applyPlan?.Tombstones.Count == 0 && libraryPlan?.Tombstones.Count == 0)
                ? CanonicalRetirementStatus.planningOnly : CanonicalRetirementStatus.applyBridged;
            statuses[CanonicalRetirementDomain.conflicts] =
                (applyPlan?.Conflicts.Count != 0 || libraryPlan?.Conflicts.Count != 0)
                ? CanonicalRetirementStatus.blocked : CanonicalRetirementStatus.semanticsComplete;
            statuses[CanonicalRetirementDomain.apply] = applyPlan == null
                ? CanonicalRetirementStatus.planningOnly : CanonicalRetirementStatus.applyBridged;
            statuses[CanonicalRetirementDomain.transferState] = transferProjection == null
                ? CanonicalRetirementStatus.planningOnly : CanonicalRetirementStatus.semanticsComplete;
            statuses[CanonicalRetirementDomain.objectProjection] = CanonicalRetirementStatus.semanticsComplete;
            statuses[CanonicalRetirementDomain.inventory] = inventoryCoverage == null
                ? CanonicalRetirementStatus.shadowOnly : CanonicalRetirementStatus.semanticsComplete;
            statuses[CanonicalRetirementDomain.transport] = CanonicalRetirementStatus.blocked;
            statuses[CanonicalRetirementDomain.uploadRuntime] = CanonicalRetirementStatus.blocked;
            statuses[CanonicalRetirementDomain.physicalStorage] = CanonicalRetirementStatus.blocked;
        }

        if (fallbackUsed ||
            (libraryPlan?.FallbackRequiredObjectIDs.Count != 0) ||
            (inventoryCoverage?.FallbackRequiredCount ?? 0) > 0)
        {
            foreach (var domain in new[] {
                CanonicalRetirementDomain.folders, CanonicalRetirementDomain.studyItems,
                CanonicalRetirementDomain.inventory, CanonicalRetirementDomain.apply })
            {
                Add(domain, CanonicalRetirementBlockerKind.fallbackUsed,
                    "legacyFallbackPreserved", statuses, blockers);
            }
        }

        if ((inventoryCoverage?.UnsupportedLegacyObjectCount ?? 0) > 0)
        {
            Add(CanonicalRetirementDomain.inventory, CanonicalRetirementBlockerKind.unsupportedObjectKinds,
                $"unsupported={inventoryCoverage?.UnsupportedLegacyObjectCount ?? 0}", statuses, blockers);
        }

        if (applyPlan?.Conflicts.Count != 0 || libraryPlan?.Conflicts.Count != 0)
        {
            Add(CanonicalRetirementDomain.conflicts, CanonicalRetirementBlockerKind.conflictsUnresolved,
                "manualReviewRequired", statuses, blockers);
        }

        if (transferProjection == null)
        {
            Add(CanonicalRetirementDomain.transferState, CanonicalRetirementBlockerKind.transferStateUnmapped,
                "projectionMissing", statuses, blockers);
        }

        Add(CanonicalRetirementDomain.objectProjection, CanonicalRetirementBlockerKind.uiStillReadsLegacyStatus,
            "objectProjectionNotUIDriving", statuses, blockers);
        Add(CanonicalRetirementDomain.transport, CanonicalRetirementBlockerKind.routeStillLegacy,
            "signedHTTPRoutesStillLegacy", statuses, blockers);
        Add(CanonicalRetirementDomain.uploadRuntime, CanonicalRetirementBlockerKind.routeStillLegacy,
            "RecordingUploadCoordinatorStillRuntime", statuses, blockers);
        Add(CanonicalRetirementDomain.physicalStorage, CanonicalRetirementBlockerKind.physicalStoreStillLegacy,
            "legacyStoresStillOwnFiles", statuses, blockers);

        return new CanonicalRetirementReadinessReport(
            generatedAt: new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow),
            statuses: statuses,
            blockers: blockers
        );
    }

    private static void Add(
        CanonicalRetirementDomain domain,
        CanonicalRetirementBlockerKind kind,
        string detail,
        Dictionary<CanonicalRetirementDomain, CanonicalRetirementStatus> statuses,
        List<CanonicalRetirementBlocker> blockers)
    {
        statuses[domain] = CanonicalRetirementStatus.blocked;
        blockers.Add(new CanonicalRetirementBlocker(domain, kind, detail));
    }
}
