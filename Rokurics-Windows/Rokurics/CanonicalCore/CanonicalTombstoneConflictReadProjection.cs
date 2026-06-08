using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictReadProjectionSource
{
    legacy,
    canonical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictDeletedDisplayState
{
    active,
    deleted,
    trashed,
    tombstoned,
    unknown
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictStatus
{
    none,
    recorded,
    unresolved,
    manualReviewRequired
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictAntiResurrectionStatus
{
    notTriggered,
    blocked,
    risk,
    explicitRestoreRequired
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictReadProjectionFailureKind
{
    snapshotMissing,
    unsupportedObjectKind,
    pathLeakRisk,
    fullMetadataRejected,
    fullContentRejected,
    physicalDeleteRisk,
    permanentDeleteRisk,
    tombstoneGCRisk,
    staleLiveResurrectionRisk,
    autoConflictResolutionRisk
}

public sealed record CanonicalTombstoneConflictReadProjectionFailure : IEquatable<CanonicalTombstoneConflictReadProjectionFailure>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", ObjectKind?.ToString() ?? "unknown", Reason);

    public CanonicalTombstoneConflictReadProjectionFailureKind Kind { get; }
    public CanonicalTombstoneConflictReadProjectionSource Source { get; }
    public string? ObjectID { get; }
    public CanonicalObjectKind? ObjectKind { get; }
    public string Reason { get; }

    public CanonicalTombstoneConflictReadProjectionFailure(
        CanonicalTombstoneConflictReadProjectionFailureKind kind,
        CanonicalTombstoneConflictReadProjectionSource source,
        string? objectID = null, CanonicalObjectKind? objectKind = null, string reason = "")
    {
        Kind = kind;
        Source = source;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "tombstone-object") : null;
        ObjectKind = objectKind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? kind.ToString();
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadProjectionFailure? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalTombstoneConflictReadProjectionFact : IEquatable<CanonicalTombstoneConflictReadProjectionFact>
{
    public string ObjectID { get; }
    public CanonicalObjectKind ObjectKind { get; }
    public CanonicalTombstoneState TombstoneState { get; }
    public CanonicalTombstoneConflictDeletedDisplayState DeletedDisplayState { get; }
    public CanonicalTimestamp? TombstoneTimestamp { get; }
    public string? ConflictKind { get; }
    public CanonicalTombstoneConflictStatus ConflictStatus { get; }
    public string ActiveVsTombstoneState { get; }
    public CanonicalTombstoneConflictAntiResurrectionStatus AntiResurrectionStatus { get; }
    public bool ParentObjectTombstoned { get; }
    public bool GeneratedArtifactResurrectionBlocked { get; }
    public bool SoftDeleteMarkerPresent { get; }
    public string? HashPrefix { get; }
    public bool UnsupportedObjectKind { get; }
    public bool PathLeakRisk { get; }
    public bool FullMetadataIncluded { get; }
    public bool FullContentIncluded { get; }
    public bool PhysicalDeleteRisk { get; }
    public bool PermanentDeleteRisk { get; }
    public bool TombstoneGCRisk { get; }
    public bool StaleLiveResurrectionRisk { get; }
    public bool AutoConflictResolutionRisk { get; }

    public CanonicalTombstoneConflictReadProjectionFact(
        string objectID, CanonicalObjectKind objectKind,
        CanonicalTombstoneState tombstoneState = CanonicalTombstoneState.active,
        CanonicalTombstoneConflictDeletedDisplayState deletedDisplayState = CanonicalTombstoneConflictDeletedDisplayState.active,
        CanonicalTimestamp? tombstoneTimestamp = null,
        string? conflictKind = null,
        CanonicalTombstoneConflictStatus conflictStatus = CanonicalTombstoneConflictStatus.none,
        string activeVsTombstoneState = "activeOnly",
        CanonicalTombstoneConflictAntiResurrectionStatus antiResurrectionStatus = CanonicalTombstoneConflictAntiResurrectionStatus.notTriggered,
        bool parentObjectTombstoned = false, bool generatedArtifactResurrectionBlocked = false,
        bool softDeleteMarkerPresent = false, string? hashPrefix = null,
        bool unsupportedObjectKind = false, bool pathLeakRisk = false,
        bool fullMetadataIncluded = false, bool fullContentIncluded = false,
        bool physicalDeleteRisk = false, bool permanentDeleteRisk = false,
        bool tombstoneGCRisk = false, bool staleLiveResurrectionRisk = false,
        bool autoConflictResolutionRisk = false)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "tombstone-object");
        ObjectKind = objectKind;
        TombstoneState = tombstoneState;
        DeletedDisplayState = deletedDisplayState;
        TombstoneTimestamp = tombstoneTimestamp;
        ConflictKind = conflictKind != null ? CanonicalProductionRedaction.SafeDiagnosticText(conflictKind) : null;
        ConflictStatus = conflictStatus;
        ActiveVsTombstoneState = CanonicalProductionRedaction.SafeDiagnosticText(activeVsTombstoneState) ?? "activeOnly";
        AntiResurrectionStatus = antiResurrectionStatus;
        ParentObjectTombstoned = parentObjectTombstoned;
        GeneratedArtifactResurrectionBlocked = generatedArtifactResurrectionBlocked;
        SoftDeleteMarkerPresent = softDeleteMarkerPresent;
        HashPrefix = CanonicalProductionRedaction.HashPrefix(hashPrefix);
        UnsupportedObjectKind = unsupportedObjectKind;
        PathLeakRisk = pathLeakRisk;
        FullMetadataIncluded = fullMetadataIncluded;
        FullContentIncluded = fullContentIncluded;
        PhysicalDeleteRisk = physicalDeleteRisk;
        PermanentDeleteRisk = permanentDeleteRisk;
        TombstoneGCRisk = tombstoneGCRisk;
        StaleLiveResurrectionRisk = staleLiveResurrectionRisk;
        AutoConflictResolutionRisk = autoConflictResolutionRisk;
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadProjectionFact? other) =>
        other is not null && ObjectID == other.ObjectID;
    public override int GetHashCode() => ObjectID.GetHashCode();
}

public sealed record CanonicalTombstoneConflictReadProjectionItem : IEquatable<CanonicalTombstoneConflictReadProjectionItem>
{
    public string Id => string.Join("|", ObjectID, ObjectKind.ToString());

    public CanonicalTombstoneConflictReadProjectionSource Source { get; }
    public string ObjectID { get; }
    public CanonicalObjectKind ObjectKind { get; }
    public CanonicalTombstoneState TombstoneState { get; }
    public CanonicalTombstoneConflictDeletedDisplayState DeletedDisplayState { get; }
    public string TombstoneTimestampSummary { get; }
    public string ConflictKind { get; }
    public CanonicalTombstoneConflictStatus ConflictStatus { get; }
    public string ActiveVsTombstoneState { get; }
    public CanonicalTombstoneConflictAntiResurrectionStatus AntiResurrectionStatus { get; }
    public string ParentObjectStateSummary { get; }
    public bool GeneratedArtifactResurrectionBlocked { get; }
    public bool SoftDeleteMarkerPresent { get; }
    public string? HashPrefix { get; }
    public bool FullMetadataIncluded { get; }
    public bool FullContentIncluded { get; }
    public bool AbsolutePathIncluded { get; }
    public bool PhysicalDeleteTargetPathIncluded { get; }
    public bool PhysicalDeleteRisk { get; }
    public bool PermanentDeleteRisk { get; }
    public bool TombstoneGCRisk { get; }
    public bool AutoConflictResolutionRisk { get; }
    public bool StaleLiveResurrectionRisk { get; }

    public CanonicalTombstoneConflictReadProjectionItem(
        CanonicalTombstoneConflictReadProjectionSource source,
        CanonicalTombstoneConflictReadProjectionFact fact)
    {
        Source = source;
        ObjectID = fact.ObjectID;
        ObjectKind = fact.ObjectKind;
        TombstoneState = fact.TombstoneState;
        DeletedDisplayState = fact.DeletedDisplayState;
        TombstoneTimestampSummary = TimestampSummary(fact.TombstoneTimestamp);
        ConflictKind = fact.ConflictKind ?? "none";
        ConflictStatus = fact.ConflictStatus;
        ActiveVsTombstoneState = fact.ActiveVsTombstoneState;
        AntiResurrectionStatus = fact.AntiResurrectionStatus;
        ParentObjectStateSummary = fact.ParentObjectTombstoned ? "parentTombstoned" : "parentActiveOrUnknown";
        GeneratedArtifactResurrectionBlocked = fact.GeneratedArtifactResurrectionBlocked;
        SoftDeleteMarkerPresent = fact.SoftDeleteMarkerPresent || fact.TombstoneState == CanonicalTombstoneState.tombstoned;
        HashPrefix = fact.HashPrefix;
        FullMetadataIncluded = false;
        FullContentIncluded = false;
        AbsolutePathIncluded = false;
        PhysicalDeleteTargetPathIncluded = false;
        PhysicalDeleteRisk = fact.PhysicalDeleteRisk;
        PermanentDeleteRisk = fact.PermanentDeleteRisk;
        TombstoneGCRisk = fact.TombstoneGCRisk;
        AutoConflictResolutionRisk = fact.AutoConflictResolutionRisk;
        StaleLiveResurrectionRisk = fact.StaleLiveResurrectionRisk;
    }

    private static string TimestampSummary(CanonicalTimestamp? timestamp)
    {
        if (timestamp == null) return "tombstoneTimestamp=missing";
        var seconds = ((long)(timestamp.Value.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
        var prefix = CanonicalProductionRedaction.HashPrefix(CanonicalHash.Sha256String(seconds).Value) ?? "missing";
        return $"tombstoneTimestampHash={prefix}";
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadProjectionItem? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalTombstoneConflictReadSnapshot : IEquatable<CanonicalTombstoneConflictReadSnapshot>
{
    public CanonicalTombstoneConflictReadProjectionSource Source { get; }
    public CanonicalTimestamp GeneratedAt { get; }
    public CanonicalTombstoneConflictReadProjectionItem[] Items { get; }
    public CanonicalTombstoneConflictReadProjectionFailure[] Failures { get; }
    public int MetadataExcludedCount { get; }
    public int ContentExcludedCount { get; }

    public int ItemCount => Items.Length;
    public int FailureCount => Failures.Length;
    public int UnsupportedObjectCount => Failures.Count(f => f.Kind == CanonicalTombstoneConflictReadProjectionFailureKind.unsupportedObjectKind);
    public int PathLeakRiskCount => Failures.Count(f => f.Kind == CanonicalTombstoneConflictReadProjectionFailureKind.pathLeakRisk);
    public int FullMetadataIncludedCount => Items.Count(i => i.FullMetadataIncluded);
    public int FullContentIncludedCount => Items.Count(i => i.FullContentIncluded);

    public string DiagnosticsSummary => string.Join(",",
        $"source={Source}", $"items={ItemCount}", $"failures={FailureCount}",
        $"metadataIncluded={FullMetadataIncludedCount}",
        $"contentIncluded={FullContentIncludedCount}",
        $"metadataExcluded={MetadataExcludedCount}",
        $"contentExcluded={ContentExcludedCount}",
        $"pathLeakRisk={PathLeakRiskCount}"
    );

    public CanonicalTombstoneConflictReadSnapshot(
        CanonicalTombstoneConflictReadProjectionSource source,
        DateTime? generatedAt = null,
        CanonicalTombstoneConflictReadProjectionItem[]? items = null,
        CanonicalTombstoneConflictReadProjectionFailure[]? failures = null,
        int metadataExcludedCount = 0, int contentExcludedCount = 0)
    {
        Source = source;
        GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
        Items = (items ?? Array.Empty<CanonicalTombstoneConflictReadProjectionItem>())
            .GroupBy(i => i.Id).Select(g => g.First())
            .OrderBy(i => i.Id, StringComparer.Ordinal).ToArray();
        Failures = (failures ?? Array.Empty<CanonicalTombstoneConflictReadProjectionFailure>())
            .GroupBy(f => f.Id).Select(g => g.First())
            .OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        MetadataExcludedCount = Math.Max(0, metadataExcludedCount);
        ContentExcludedCount = Math.Max(0, contentExcludedCount);
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadSnapshot? other) =>
        other is not null && Source == other.Source;
    public override int GetHashCode() => Source.GetHashCode();
}

public static class CanonicalTombstoneConflictReadProjection
{
    public static CanonicalTombstoneConflictReadSnapshot Snapshot(
        CanonicalTombstoneConflictReadProjectionSource source,
        CanonicalTombstoneConflictReadProjectionFact[] facts,
        CanonicalTombstoneConflictReadProjectionFailure[]? seedFailures = null,
        DateTime? generatedAt = null)
    {
        var failures = new List<CanonicalTombstoneConflictReadProjectionFailure>(seedFailures ?? Array.Empty<CanonicalTombstoneConflictReadProjectionFailure>());
        var items = new List<CanonicalTombstoneConflictReadProjectionItem>();

        foreach (var fact in facts.OrderBy(f => f.ObjectID, StringComparer.Ordinal))
        {
            if (fact.UnsupportedObjectKind)
            {
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.unsupportedObjectKind, source,
                    fact.ObjectID, fact.ObjectKind, "unsupportedTombstoneConflictObjectKind"));
                continue;
            }
            if (fact.PathLeakRisk)
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.pathLeakRisk, source,
                    fact.ObjectID, fact.ObjectKind, "unsafePathTokenObserved"));
            if (fact.FullMetadataIncluded)
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.fullMetadataRejected, source,
                    fact.ObjectID, fact.ObjectKind, "fullMetadataExcludedFromProjection"));
            if (fact.FullContentIncluded)
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.fullContentRejected, source,
                    fact.ObjectID, fact.ObjectKind, "fullGeneratedContentExcludedFromProjection"));
            if (fact.PhysicalDeleteRisk)
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.physicalDeleteRisk, source,
                    fact.ObjectID, fact.ObjectKind, "physicalDeleteForbidden"));
            if (fact.PermanentDeleteRisk)
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.permanentDeleteRisk, source,
                    fact.ObjectID, fact.ObjectKind, "permanentDeleteForbidden"));
            if (fact.TombstoneGCRisk)
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.tombstoneGCRisk, source,
                    fact.ObjectID, fact.ObjectKind, "tombstoneGCForbidden"));
            if (fact.StaleLiveResurrectionRisk)
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.staleLiveResurrectionRisk, source,
                    fact.ObjectID, fact.ObjectKind, "staleLiveMetadataResurrectionForbidden"));
            if (fact.AutoConflictResolutionRisk)
                failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                    CanonicalTombstoneConflictReadProjectionFailureKind.autoConflictResolutionRisk, source,
                    fact.ObjectID, fact.ObjectKind, "autoConflictResolutionForbidden"));

            items.Add(new CanonicalTombstoneConflictReadProjectionItem(source, fact));
        }

        return new CanonicalTombstoneConflictReadSnapshot(source, generatedAt,
            items.ToArray(), failures.ToArray(), items.Count, items.Count);
    }

    public static CanonicalTombstoneConflictReadSnapshot Snapshot(
        CanonicalTombstoneConflictReadProjectionSource source,
        CanonicalManifest? localManifest, CanonicalManifest? peerManifest,
        CanonicalApplyPlan? applyPlan = null, CanonicalLibrarySyncPlan? libraryPlan = null,
        DateTime? generatedAt = null)
    {
        var facts = new List<CanonicalTombstoneConflictReadProjectionFact>();
        var failures = new List<CanonicalTombstoneConflictReadProjectionFailure>();

        if (localManifest == null && peerManifest == null && applyPlan == null && libraryPlan == null)
            failures.Add(new CanonicalTombstoneConflictReadProjectionFailure(
                CanonicalTombstoneConflictReadProjectionFailureKind.snapshotMissing, source,
                reason: "tombstoneConflictReadSnapshotMissing"));

        if (localManifest != null) AppendManifestFacts(localManifest, false, facts);
        if (peerManifest != null) AppendManifestFacts(peerManifest, true, facts);
        if (applyPlan != null) AppendApplyPlanFacts(applyPlan, facts);
        if (libraryPlan != null) AppendLibraryPlanFacts(libraryPlan, facts);

        return Snapshot(source, facts.ToArray(), failures.ToArray(), generatedAt);
    }

    private static void AppendManifestFacts(CanonicalManifest manifest, bool peer,
        List<CanonicalTombstoneConflictReadProjectionFact> facts)
    {
        foreach (var obj in manifest.Objects)
        {
            var tombstoned = obj.Metadata?.IsDeleted == true || obj.SyncState == CanonicalSyncState.deleted;
            var conflict = obj.SyncState == CanonicalSyncState.conflict;
            facts.Add(new CanonicalTombstoneConflictReadProjectionFact(
                obj.ObjectID, CanonicalObjectKind.recording,
                tombstoned ? CanonicalTombstoneState.tombstoned : CanonicalTombstoneState.active,
                tombstoned ? CanonicalTombstoneConflictDeletedDisplayState.tombstoned : CanonicalTombstoneConflictDeletedDisplayState.active,
                obj.Metadata?.DeletedAt,
                conflict ? "recordingConflict" : null,
                conflict ? CanonicalTombstoneConflictStatus.unresolved : CanonicalTombstoneConflictStatus.none,
                tombstoned ? (peer ? "peerTombstone" : "localTombstone") : "activeOnly",
                tombstoned ? CanonicalTombstoneConflictAntiResurrectionStatus.blocked : CanonicalTombstoneConflictAntiResurrectionStatus.notTriggered,
                generatedArtifactResurrectionBlocked: tombstoned,
                softDeleteMarkerPresent: tombstoned,
                hashPrefix: CanonicalProductionRedaction.HashPrefix(obj.MetadataHash?.Value)));
        }
        foreach (var obj in manifest.LibraryObjects)
        {
            var tombstoned = obj.IsDeleted;
            facts.Add(new CanonicalTombstoneConflictReadProjectionFact(
                obj.ObjectID.RawValue, obj.Kind,
                tombstoned ? CanonicalTombstoneState.tombstoned : CanonicalTombstoneState.active,
                tombstoned ? CanonicalTombstoneConflictDeletedDisplayState.tombstoned : CanonicalTombstoneConflictDeletedDisplayState.active,
                obj.DeletedAt,
                activeVsTombstoneState: tombstoned ? (peer ? "peerLibraryTombstone" : "localLibraryTombstone") : "activeOnly",
                antiResurrectionStatus: tombstoned ? CanonicalTombstoneConflictAntiResurrectionStatus.blocked : CanonicalTombstoneConflictAntiResurrectionStatus.notTriggered,
                generatedArtifactResurrectionBlocked: tombstoned,
                softDeleteMarkerPresent: tombstoned,
                hashPrefix: CanonicalProductionRedaction.HashPrefix(obj.MetadataHash?.Value)));
        }
        foreach (var tombstone in manifest.LibraryTombstones)
        {
            facts.Add(new CanonicalTombstoneConflictReadProjectionFact(
                tombstone.ObjectID.RawValue, tombstone.ObjectKind,
                CanonicalTombstoneState.tombstoned, CanonicalTombstoneConflictDeletedDisplayState.tombstoned,
                tombstone.DeletedAt,
                activeVsTombstoneState: peer ? "peerLibraryTombstone" : "localLibraryTombstone",
                antiResurrectionStatus: CanonicalTombstoneConflictAntiResurrectionStatus.blocked,
                generatedArtifactResurrectionBlocked: true,
                softDeleteMarkerPresent: true));
        }
    }

    private static void AppendApplyPlanFacts(CanonicalApplyPlan plan,
        List<CanonicalTombstoneConflictReadProjectionFact> facts)
    {
        var tombstones = plan.Tombstones.ToDictionary(t => t.TombstoneID);
        var conflicts = plan.Conflicts.ToDictionary(c => c.ConflictID);
        foreach (var action in plan.Actions)
        {
            switch (action.Kind)
            {
                case CanonicalApplyActionKind.objectTombstoneApply:
                case CanonicalApplyActionKind.objectTombstoneSend:
                case CanonicalApplyActionKind.artifactTombstoneApply:
                    var tomb = action.TombstoneID != null ? tombstones.GetValueOrDefault(action.TombstoneID) : null;
                    facts.Add(new CanonicalTombstoneConflictReadProjectionFact(
                        action.Target.ObjectID,
                        action.Target.ArtifactKind == null ? CanonicalObjectKind.recording : CanonicalObjectKind.generatedArtifactEnvelope,
                        CanonicalTombstoneState.tombstoned, CanonicalTombstoneConflictDeletedDisplayState.tombstoned,
                        tomb?.DeletedAt,
                        activeVsTombstoneState: action.Kind == CanonicalApplyActionKind.artifactTombstoneApply ? "artifactTombstoneUnsupported" : "objectTombstone",
                        antiResurrectionStatus: CanonicalTombstoneConflictAntiResurrectionStatus.blocked,
                        generatedArtifactResurrectionBlocked: true,
                        softDeleteMarkerPresent: true,
                        physicalDeleteRisk: action.Kind == CanonicalApplyActionKind.artifactTombstoneApply));
                    break;
                case CanonicalApplyActionKind.conflictRecord:
                    var conf = action.ConflictID != null ? conflicts.GetValueOrDefault(action.ConflictID) : null;
                    facts.Add(new CanonicalTombstoneConflictReadProjectionFact(
                        action.Target.ObjectID,
                        action.Target.ArtifactKind == null ? CanonicalObjectKind.recording : CanonicalObjectKind.generatedArtifactEnvelope,
                        conflictKind: conf?.Kind.ToString() ?? action.Reason,
                        conflictStatus: CanonicalTombstoneConflictStatus.manualReviewRequired,
                        activeVsTombstoneState: conf?.Kind == CanonicalConflictKind.activeVsTombstone ? "activeVsTombstone" : "conflict",
                        antiResurrectionStatus: conf?.Kind == CanonicalConflictKind.activeVsTombstone
                            ? CanonicalTombstoneConflictAntiResurrectionStatus.blocked
                            : CanonicalTombstoneConflictAntiResurrectionStatus.notTriggered,
                        generatedArtifactResurrectionBlocked: conf?.Kind == CanonicalConflictKind.activeVsTombstone,
                        hashPrefix: conf?.LocalHashPrefix));
                    break;
                case CanonicalApplyActionKind.deferredUnsupported when action.FailureReason == CanonicalApplyFailureReason.tombstoneBlocksResurrection:
                    facts.Add(new CanonicalTombstoneConflictReadProjectionFact(
                        action.Target.ObjectID, CanonicalObjectKind.generatedArtifactEnvelope,
                        CanonicalTombstoneState.tombstoned, CanonicalTombstoneConflictDeletedDisplayState.tombstoned,
                        conflictKind: "tombstoneBlocksResurrection",
                        conflictStatus: CanonicalTombstoneConflictStatus.manualReviewRequired,
                        activeVsTombstoneState: "generatedArtifactBlockedByTombstone",
                        antiResurrectionStatus: CanonicalTombstoneConflictAntiResurrectionStatus.blocked,
                        generatedArtifactResurrectionBlocked: true));
                    break;
            }
        }
    }

    private static void AppendLibraryPlanFacts(CanonicalLibrarySyncPlan plan,
        List<CanonicalTombstoneConflictReadProjectionFact> facts)
    {
        var tombstones = plan.Tombstones.ToDictionary(t => t.TombstoneID);
        var conflicts = plan.Conflicts.ToDictionary(c => c.ConflictID);
        foreach (var action in plan.ApplyActions)
        {
            switch (action.Kind)
            {
                case CanonicalLibraryApplyActionKind.libraryTombstoneApply:
                case CanonicalLibraryApplyActionKind.libraryTombstoneSend:
                    var tomb = action.TombstoneID != null ? tombstones.GetValueOrDefault(action.TombstoneID) : null;
                    facts.Add(new CanonicalTombstoneConflictReadProjectionFact(
                        action.Target.ObjectID,
                        tomb?.ObjectKind ?? CanonicalObjectKind.unknownUnsupported,
                        CanonicalTombstoneState.tombstoned, CanonicalTombstoneConflictDeletedDisplayState.tombstoned,
                        tomb?.DeletedAt,
                        activeVsTombstoneState: "libraryTombstone",
                        antiResurrectionStatus: CanonicalTombstoneConflictAntiResurrectionStatus.blocked,
                        generatedArtifactResurrectionBlocked: true,
                        softDeleteMarkerPresent: true));
                    break;
                case CanonicalLibraryApplyActionKind.conflictRecord:
                    var conf = action.ConflictID != null ? conflicts.GetValueOrDefault(action.ConflictID) : null;
                    facts.Add(new CanonicalTombstoneConflictReadProjectionFact(
                        action.Target.ObjectID,
                        conf?.ObjectKind ?? CanonicalObjectKind.unknownUnsupported,
                        conflictKind: conf?.Kind.ToString() ?? action.Reason,
                        conflictStatus: CanonicalTombstoneConflictStatus.manualReviewRequired,
                        activeVsTombstoneState: conf?.Kind == CanonicalLibraryConflictKind.activeVsTombstone ? "activeVsTombstone" : "conflict",
                        antiResurrectionStatus: conf?.Kind == CanonicalLibraryConflictKind.activeVsTombstone
                            ? CanonicalTombstoneConflictAntiResurrectionStatus.blocked
                            : CanonicalTombstoneConflictAntiResurrectionStatus.notTriggered,
                        generatedArtifactResurrectionBlocked: conf?.Kind == CanonicalLibraryConflictKind.activeVsTombstone,
                        hashPrefix: conf?.LocalHashPrefix));
                    break;
            }
        }
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictReadSideDivergenceKind
{
    missingInCanonical,
    missingInLegacy,
    tombstoneStateMismatch,
    tombstoneTimestampMismatch,
    conflictRecordMismatch,
    activeVsTombstoneMismatch,
    resurrectionBlockMismatch,
    softDeleteMarkerMismatch,
    physicalDeleteRisk,
    permanentDeleteRisk,
    tombstoneGCRisk,
    autoConflictResolutionRisk,
    staleLiveResurrectionRisk,
    unsupportedObjectKind,
    pathLeakRisk
}

public sealed record CanonicalTombstoneConflictReadSideDivergence : IEquatable<CanonicalTombstoneConflictReadSideDivergence>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID, ObjectKind?.ToString() ?? "", Field ?? "");

    public CanonicalTombstoneConflictReadSideDivergenceKind Kind { get; }
    public string ObjectID { get; }
    public CanonicalObjectKind? ObjectKind { get; }
    public string? Field { get; }
    public string? LegacyValue { get; }
    public string? CanonicalValue { get; }
    public bool Fatal { get; }

    private static readonly HashSet<CanonicalTombstoneConflictReadSideDivergenceKind> FatalKinds = new()
    {
        CanonicalTombstoneConflictReadSideDivergenceKind.physicalDeleteRisk,
        CanonicalTombstoneConflictReadSideDivergenceKind.permanentDeleteRisk,
        CanonicalTombstoneConflictReadSideDivergenceKind.tombstoneGCRisk,
        CanonicalTombstoneConflictReadSideDivergenceKind.autoConflictResolutionRisk,
        CanonicalTombstoneConflictReadSideDivergenceKind.staleLiveResurrectionRisk,
        CanonicalTombstoneConflictReadSideDivergenceKind.pathLeakRisk
    };

    public CanonicalTombstoneConflictReadSideDivergence(
        CanonicalTombstoneConflictReadSideDivergenceKind kind,
        string objectID, CanonicalObjectKind? objectKind = null,
        string? field = null, string? legacyValue = null, string? canonicalValue = null,
        bool fatal = false)
    {
        Kind = kind;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "tombstone-object");
        ObjectKind = objectKind;
        Field = field != null ? CanonicalProductionRedaction.SafeDiagnosticText(field) : null;
        LegacyValue = legacyValue != null ? CanonicalProductionRedaction.SafeDiagnosticText(legacyValue) : null;
        CanonicalValue = canonicalValue != null ? CanonicalProductionRedaction.SafeDiagnosticText(canonicalValue) : null;
        Fatal = fatal || FatalKinds.Contains(kind);
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadSideDivergence? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictReadSideBlocker
{
    missingLegacySnapshot,
    missingCanonicalSnapshot,
    blockingDivergence,
    physicalDeleteRisk,
    permanentDeleteRisk,
    tombstoneGCRisk,
    staleLiveResurrectionRisk,
    autoConflictResolutionRisk,
    unsupportedObjectKind,
    pathLeakRisk
}

public sealed record CanonicalTombstoneConflictReadSideDiffReport : IEquatable<CanonicalTombstoneConflictReadSideDiffReport>
{
    public CanonicalTombstoneConflictReadSideEquivalence Equivalence { get; }
    public CanonicalTombstoneConflictReadSideDivergence[] Divergences { get; }
    public CanonicalTombstoneConflictReadSideBlocker[] Blockers { get; }
    public string LegacySnapshotSummary { get; }
    public string CanonicalSnapshotSummary { get; }
    public string DiagnosticsSummary { get; }

    public bool Equivalent => Blockers.Length == 0 && Divergences.Length == 0;
    public int DivergenceCount => Divergences.Length;
    public int FatalDivergenceCount => Divergences.Count(d => d.Fatal);
    public int UnsupportedObjectCount => Divergences.Count(d => d.Kind == CanonicalTombstoneConflictReadSideDivergenceKind.unsupportedObjectKind);
    public int PathLeakRiskCount => Divergences.Count(d => d.Kind == CanonicalTombstoneConflictReadSideDivergenceKind.pathLeakRisk);

    public CanonicalTombstoneConflictReadSideDiffReport(
        CanonicalTombstoneConflictReadSideEquivalence equivalence,
        CanonicalTombstoneConflictReadSideDivergence[] divergences,
        CanonicalTombstoneConflictReadSideBlocker[] blockers,
        string legacySnapshotSummary, string canonicalSnapshotSummary, string diagnosticsSummary)
    {
        Equivalence = equivalence;
        Divergences = divergences;
        Blockers = blockers;
        LegacySnapshotSummary = legacySnapshotSummary;
        CanonicalSnapshotSummary = canonicalSnapshotSummary;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadSideDiffReport? other) =>
        other is not null && Equivalent == other.Equivalent;
    public override int GetHashCode() => Equivalent.GetHashCode();
}

public sealed record CanonicalTombstoneConflictReadSideEquivalence : IEquatable<CanonicalTombstoneConflictReadSideEquivalence>
{
    public bool Equivalent { get; }
    public int ObjectCount { get; }
    public int TombstoneCount { get; }
    public int ConflictCount { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalTombstoneConflictReadSideEquivalence(
        bool equivalent, int objectCount, int tombstoneCount, int conflictCount, string diagnosticsSummary)
    {
        Equivalent = equivalent;
        ObjectCount = objectCount;
        TombstoneCount = tombstoneCount;
        ConflictCount = conflictCount;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadSideEquivalence? other) =>
        other is not null && Equivalent == other.Equivalent;
    public override int GetHashCode() => Equivalent.GetHashCode();
}

public static class CanonicalTombstoneConflictReadSideParallelDiff
{
    public static CanonicalTombstoneConflictReadSideDiffReport Compare(
        CanonicalTombstoneConflictReadSnapshot? legacy,
        CanonicalTombstoneConflictReadSnapshot? canonical)
    {
        var divergences = new List<CanonicalTombstoneConflictReadSideDivergence>();
        var blockers = new List<CanonicalTombstoneConflictReadSideBlocker>();

        if (legacy == null)
            return Report(null, canonical, Array.Empty<CanonicalTombstoneConflictReadSideDivergence>(),
                new[] { CanonicalTombstoneConflictReadSideBlocker.missingLegacySnapshot });
        if (canonical == null)
            return Report(legacy, null, Array.Empty<CanonicalTombstoneConflictReadSideDivergence>(),
                new[] { CanonicalTombstoneConflictReadSideBlocker.missingCanonicalSnapshot });

        AppendFailureDivergences(legacy.Failures.Concat(canonical.Failures).ToArray(), divergences, blockers);

        var legacyByID = legacy.Items.ToDictionary(i => i.Id);
        var canonicalByID = canonical.Items.ToDictionary(i => i.Id);
        foreach (var id in legacyByID.Keys.Union(canonicalByID.Keys).OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!legacyByID.TryGetValue(id, out var legacyItem))
            {
                var canonicalItem = canonicalByID.GetValueOrDefault(id);
                divergences.Add(new CanonicalTombstoneConflictReadSideDivergence(
                    CanonicalTombstoneConflictReadSideDivergenceKind.missingInLegacy,
                    canonicalItem?.ObjectID ?? id, canonicalItem?.ObjectKind,
                    canonicalValue: "present"));
                continue;
            }
            if (!canonicalByID.TryGetValue(id, out var canonicalItem))
            {
                divergences.Add(new CanonicalTombstoneConflictReadSideDivergence(
                    CanonicalTombstoneConflictReadSideDivergenceKind.missingInCanonical,
                    legacyItem.ObjectID, legacyItem.ObjectKind, legacyValue: "present"));
                continue;
            }
            Compare(legacyItem, canonicalItem, divergences);
        }

        if (divergences.Count > 0)
            blockers.Add(CanonicalTombstoneConflictReadSideBlocker.blockingDivergence);

        return Report(legacy, canonical, divergences, blockers);
    }

    private static void Compare(CanonicalTombstoneConflictReadProjectionItem legacy,
        CanonicalTombstoneConflictReadProjectionItem canonical,
        List<CanonicalTombstoneConflictReadSideDivergence> divergences)
    {
        AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind.tombstoneStateMismatch,
            legacy, canonical, "tombstoneState", legacy.TombstoneState.ToString(), canonical.TombstoneState.ToString(), divergences);
        AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind.tombstoneTimestampMismatch,
            legacy, canonical, "tombstoneTimestamp", legacy.TombstoneTimestampSummary, canonical.TombstoneTimestampSummary, divergences);
        AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind.conflictRecordMismatch,
            legacy, canonical, "conflictKind", legacy.ConflictKind, canonical.ConflictKind, divergences);
        AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind.conflictRecordMismatch,
            legacy, canonical, "conflictStatus", legacy.ConflictStatus.ToString(), canonical.ConflictStatus.ToString(), divergences);
        AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind.activeVsTombstoneMismatch,
            legacy, canonical, "activeVsTombstone", legacy.ActiveVsTombstoneState, canonical.ActiveVsTombstoneState, divergences);
        AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind.resurrectionBlockMismatch,
            legacy, canonical, "antiResurrection", legacy.AntiResurrectionStatus.ToString(), canonical.AntiResurrectionStatus.ToString(), divergences);
        AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind.resurrectionBlockMismatch,
            legacy, canonical, "generatedArtifactBlock", legacy.GeneratedArtifactResurrectionBlocked.ToString(), canonical.GeneratedArtifactResurrectionBlocked.ToString(), divergences);
        AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind.softDeleteMarkerMismatch,
            legacy, canonical, "softDeleteMarker", legacy.SoftDeleteMarkerPresent.ToString(), canonical.SoftDeleteMarkerPresent.ToString(), divergences);
        AppendRiskDivergences(legacy, divergences);
        AppendRiskDivergences(canonical, divergences);
    }

    private static void AppendMismatch(CanonicalTombstoneConflictReadSideDivergenceKind kind,
        CanonicalTombstoneConflictReadProjectionItem legacy,
        CanonicalTombstoneConflictReadProjectionItem canonical,
        string field, string legacyValue, string canonicalValue,
        List<CanonicalTombstoneConflictReadSideDivergence> divergences)
    {
        if (legacyValue == canonicalValue) return;
        divergences.Add(new CanonicalTombstoneConflictReadSideDivergence(
            kind, legacy.ObjectID, legacy.ObjectKind, field, legacyValue, canonicalValue));
    }

    private static void AppendRiskDivergences(CanonicalTombstoneConflictReadProjectionItem item,
        List<CanonicalTombstoneConflictReadSideDivergence> divergences)
    {
        if (item.PhysicalDeleteRisk)
            divergences.Add(new CanonicalTombstoneConflictReadSideDivergence(
                CanonicalTombstoneConflictReadSideDivergenceKind.physicalDeleteRisk,
                item.ObjectID, item.ObjectKind, "physicalDelete", canonicalValue: "true", fatal: true));
        if (item.PermanentDeleteRisk)
            divergences.Add(new CanonicalTombstoneConflictReadSideDivergence(
                CanonicalTombstoneConflictReadSideDivergenceKind.permanentDeleteRisk,
                item.ObjectID, item.ObjectKind, "permanentDelete", canonicalValue: "true", fatal: true));
        if (item.TombstoneGCRisk)
            divergences.Add(new CanonicalTombstoneConflictReadSideDivergence(
                CanonicalTombstoneConflictReadSideDivergenceKind.tombstoneGCRisk,
                item.ObjectID, item.ObjectKind, "tombstoneGC", canonicalValue: "true", fatal: true));
        if (item.StaleLiveResurrectionRisk)
            divergences.Add(new CanonicalTombstoneConflictReadSideDivergence(
                CanonicalTombstoneConflictReadSideDivergenceKind.staleLiveResurrectionRisk,
                item.ObjectID, item.ObjectKind, "staleLiveMetadata", canonicalValue: "true", fatal: true));
        if (item.AutoConflictResolutionRisk)
            divergences.Add(new CanonicalTombstoneConflictReadSideDivergence(
                CanonicalTombstoneConflictReadSideDivergenceKind.autoConflictResolutionRisk,
                item.ObjectID, item.ObjectKind, "autoConflictResolution", canonicalValue: "true", fatal: true));
    }

    private static void AppendFailureDivergences(
        CanonicalTombstoneConflictReadProjectionFailure[] failures,
        List<CanonicalTombstoneConflictReadSideDivergence> divergences,
        List<CanonicalTombstoneConflictReadSideBlocker> blockers)
    {
        foreach (var failure in failures)
        {
            switch (failure.Kind)
            {
                case CanonicalTombstoneConflictReadProjectionFailureKind.snapshotMissing:
                    continue;
                case CanonicalTombstoneConflictReadProjectionFailureKind.unsupportedObjectKind:
                    blockers.Add(CanonicalTombstoneConflictReadSideBlocker.unsupportedObjectKind);
                    divergences.Add(FailureDivergence(CanonicalTombstoneConflictReadSideDivergenceKind.unsupportedObjectKind, failure));
                    break;
                case CanonicalTombstoneConflictReadProjectionFailureKind.pathLeakRisk:
                    blockers.Add(CanonicalTombstoneConflictReadSideBlocker.pathLeakRisk);
                    divergences.Add(FailureDivergence(CanonicalTombstoneConflictReadSideDivergenceKind.pathLeakRisk, failure, true));
                    break;
                case CanonicalTombstoneConflictReadProjectionFailureKind.fullMetadataRejected:
                case CanonicalTombstoneConflictReadProjectionFailureKind.fullContentRejected:
                    continue;
                case CanonicalTombstoneConflictReadProjectionFailureKind.physicalDeleteRisk:
                    blockers.Add(CanonicalTombstoneConflictReadSideBlocker.physicalDeleteRisk);
                    divergences.Add(FailureDivergence(CanonicalTombstoneConflictReadSideDivergenceKind.physicalDeleteRisk, failure, true));
                    break;
                case CanonicalTombstoneConflictReadProjectionFailureKind.permanentDeleteRisk:
                    blockers.Add(CanonicalTombstoneConflictReadSideBlocker.permanentDeleteRisk);
                    divergences.Add(FailureDivergence(CanonicalTombstoneConflictReadSideDivergenceKind.permanentDeleteRisk, failure, true));
                    break;
                case CanonicalTombstoneConflictReadProjectionFailureKind.tombstoneGCRisk:
                    blockers.Add(CanonicalTombstoneConflictReadSideBlocker.tombstoneGCRisk);
                    divergences.Add(FailureDivergence(CanonicalTombstoneConflictReadSideDivergenceKind.tombstoneGCRisk, failure, true));
                    break;
                case CanonicalTombstoneConflictReadProjectionFailureKind.staleLiveResurrectionRisk:
                    blockers.Add(CanonicalTombstoneConflictReadSideBlocker.staleLiveResurrectionRisk);
                    divergences.Add(FailureDivergence(CanonicalTombstoneConflictReadSideDivergenceKind.staleLiveResurrectionRisk, failure, true));
                    break;
                case CanonicalTombstoneConflictReadProjectionFailureKind.autoConflictResolutionRisk:
                    blockers.Add(CanonicalTombstoneConflictReadSideBlocker.autoConflictResolutionRisk);
                    divergences.Add(FailureDivergence(CanonicalTombstoneConflictReadSideDivergenceKind.autoConflictResolutionRisk, failure, true));
                    break;
            }
        }
    }

    private static CanonicalTombstoneConflictReadSideDivergence FailureDivergence(
        CanonicalTombstoneConflictReadSideDivergenceKind kind,
        CanonicalTombstoneConflictReadProjectionFailure failure, bool fatal = false) =>
        new(kind, failure.ObjectID ?? "run", failure.ObjectKind, "projectionFailure",
            failure.Source == CanonicalTombstoneConflictReadProjectionSource.legacy ? failure.Reason : null,
            failure.Source == CanonicalTombstoneConflictReadProjectionSource.canonical ? failure.Reason : null,
            fatal);

    private static CanonicalTombstoneConflictReadSideDiffReport Report(
        CanonicalTombstoneConflictReadSnapshot? legacy,
        CanonicalTombstoneConflictReadSnapshot? canonical,
        List<CanonicalTombstoneConflictReadSideDivergence> divergences,
        List<CanonicalTombstoneConflictReadSideBlocker> blockers)
    {
        var uniqueDivergences = divergences.GroupBy(d => d.Id).Select(g => g.First())
            .OrderBy(d => d.Id, StringComparer.Ordinal).ToArray();
        var uniqueBlockers = new HashSet<CanonicalTombstoneConflictReadSideBlocker>(blockers);
        if (uniqueDivergences.Any(d => d.Kind == CanonicalTombstoneConflictReadSideDivergenceKind.physicalDeleteRisk))
            uniqueBlockers.Add(CanonicalTombstoneConflictReadSideBlocker.physicalDeleteRisk);
        if (uniqueDivergences.Any(d => d.Kind == CanonicalTombstoneConflictReadSideDivergenceKind.permanentDeleteRisk))
            uniqueBlockers.Add(CanonicalTombstoneConflictReadSideBlocker.permanentDeleteRisk);
        if (uniqueDivergences.Any(d => d.Kind == CanonicalTombstoneConflictReadSideDivergenceKind.tombstoneGCRisk))
            uniqueBlockers.Add(CanonicalTombstoneConflictReadSideBlocker.tombstoneGCRisk);
        if (uniqueDivergences.Any(d => d.Kind == CanonicalTombstoneConflictReadSideDivergenceKind.staleLiveResurrectionRisk))
            uniqueBlockers.Add(CanonicalTombstoneConflictReadSideBlocker.staleLiveResurrectionRisk);
        if (uniqueDivergences.Any(d => d.Kind == CanonicalTombstoneConflictReadSideDivergenceKind.autoConflictResolutionRisk))
            uniqueBlockers.Add(CanonicalTombstoneConflictReadSideBlocker.autoConflictResolutionRisk);
        uniqueBlockers = new HashSet<CanonicalTombstoneConflictReadSideBlocker>(uniqueBlockers
            .OrderBy(b => b.ToString(), StringComparer.Ordinal));

        var equivalent = uniqueDivergences.Length == 0 && uniqueBlockers.Count == 0;
        var objectCount = canonical?.Items.Length ?? legacy?.Items.Length ?? 0;
        var tombstoneCount = (canonical ?? legacy)?.Items.Count(i => i.TombstoneState == CanonicalTombstoneState.tombstoned) ?? 0;
        var conflictCount = (canonical ?? legacy)?.Items.Count(i => i.ConflictStatus != CanonicalTombstoneConflictStatus.none) ?? 0;
        var equivalence = new CanonicalTombstoneConflictReadSideEquivalence(
            equivalent, objectCount, tombstoneCount, conflictCount,
            $"equivalent={equivalent},objects={objectCount},tombstones={tombstoneCount},conflicts={conflictCount}");

        return new CanonicalTombstoneConflictReadSideDiffReport(
            equivalence, uniqueDivergences,
            uniqueBlockers.OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray(),
            legacy?.DiagnosticsSummary ?? "legacySnapshot=missing",
            canonical?.DiagnosticsSummary ?? "canonicalSnapshot=missing",
            $"domain=tombstoneConflict,equivalent={equivalent},divergences={uniqueDivergences.Length},fatal={uniqueDivergences.Count(d => d.Fatal)},blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))}"
        );
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictReadSideMode
{
    disabled,
    parallelOnly
}

public sealed record CanonicalTombstoneConflictReadSideConfiguration : IEquatable<CanonicalTombstoneConflictReadSideConfiguration>
{
    public bool IsEnabled { get; }
    public CanonicalTombstoneConflictReadSideMode Mode { get; }
    public CanonicalTombstoneConflictReadSidePolicy Policy { get; }

    public CanonicalTombstoneConflictReadSideConfiguration(
        bool isEnabled = false,
        CanonicalTombstoneConflictReadSideMode mode = CanonicalTombstoneConflictReadSideMode.disabled,
        CanonicalTombstoneConflictReadSidePolicy? policy = null)
    {
        IsEnabled = isEnabled;
        Mode = isEnabled ? mode : CanonicalTombstoneConflictReadSideMode.disabled;
        Policy = policy ?? new CanonicalTombstoneConflictReadSidePolicy();
    }

    public static readonly CanonicalTombstoneConflictReadSideConfiguration Disabled = new();

    public virtual bool Equals(CanonicalTombstoneConflictReadSideConfiguration? other) =>
        other is not null && IsEnabled == other.IsEnabled;
    public override int GetHashCode() => IsEnabled.GetHashCode();
}

public sealed record CanonicalTombstoneConflictReadSidePolicy : IEquatable<CanonicalTombstoneConflictReadSidePolicy>
{
    public bool RecordDiagnostics { get; }
    public int MaxDiagnosticsEvents { get; }

    public CanonicalTombstoneConflictReadSidePolicy(bool recordDiagnostics = true, int maxDiagnosticsEvents = 200)
    {
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
    }

    public virtual bool Equals(CanonicalTombstoneConflictReadSidePolicy? other) =>
        other is not null && RecordDiagnostics == other.RecordDiagnostics;
    public override int GetHashCode() => RecordDiagnostics.GetHashCode();
}
