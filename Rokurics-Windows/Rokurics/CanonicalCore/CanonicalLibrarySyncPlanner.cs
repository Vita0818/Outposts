using System.Globalization;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryActionKind
{
    folderMetadataApply,
    folderMetadataSend,
    folderMetadataNoOp,
    folderConflict,
    folderTombstoneApply,
    folderTombstoneSend,
    studyItemMetadataApply,
    studyItemMetadataSend,
    studyItemMetadataNoOp,
    studyItemConflict,
    studyItemTombstoneApply,
    studyItemTombstoneSend,
    unsupportedFallback,
    deferred
}

public sealed class CanonicalLibrarySyncAction : IEquatable<CanonicalLibrarySyncAction>
{
    public string Id => ActionID;
    public string ActionID { get; }
    public CanonicalLibraryActionKind Kind { get; }
    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalObjectKind ObjectKind { get; }
    public CanonicalApplySource Source { get; }
    public string Reason { get; }
    public string? LocalHashPrefix { get; }
    public string? PeerHashPrefix { get; }
    public CanonicalTimestamp? LocalModifiedAt { get; }
    public CanonicalTimestamp? PeerModifiedAt { get; }

    public CanonicalLibrarySyncAction(
        CanonicalLibraryActionKind kind,
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        CanonicalApplySource source,
        string reason,
        CanonicalHash? localHash = null,
        CanonicalHash? peerHash = null,
        CanonicalTimestamp? localModifiedAt = null,
        CanonicalTimestamp? peerModifiedAt = null)
    {
        Kind = kind;
        ObjectID = objectID;
        ObjectKind = objectKind;
        Source = source;
        Reason = reason?.Trim().NilIfEmpty() ?? Kind.ToString();
        LocalHashPrefix = localHash.HasValue ? HashPrefix(localHash.Value) : null;
        PeerHashPrefix = peerHash.HasValue ? HashPrefix(peerHash.Value) : null;
        LocalModifiedAt = localModifiedAt;
        PeerModifiedAt = peerModifiedAt;
        ActionID = string.Join("|", Kind.ToString(), ObjectKind.ToString(), ObjectID.RawValue, Reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalLibrarySyncAction other && Equals(other);
    public bool Equals(CanonicalLibrarySyncAction? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
    public static bool operator ==(CanonicalLibrarySyncAction left, CanonicalLibrarySyncAction right) => left.Equals(right);
    public static bool operator !=(CanonicalLibrarySyncAction left, CanonicalLibrarySyncAction right) => !left.Equals(right);

    private static string HashPrefix(CanonicalHash hash) =>
        hash.Value[..Math.Min(hash.Value.Length, 12)];
}

public sealed class CanonicalLibrarySyncDiagnostic : IEquatable<CanonicalLibrarySyncDiagnostic>
{
    public string Id => string.Join("|", Phase, ObjectID?.RawValue ?? "", Detail ?? "");
    public string Phase { get; }
    public CanonicalLibraryObjectID? ObjectID { get; }
    public CanonicalObjectKind? ObjectKind { get; }
    public string? Detail { get; }

    public CanonicalLibrarySyncDiagnostic(
        string phase,
        CanonicalLibraryObjectID? objectID = null,
        CanonicalObjectKind? objectKind = null,
        string? detail = null)
    {
        Phase = phase;
        ObjectID = objectID;
        ObjectKind = objectKind;
        Detail = detail;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibrarySyncDiagnostic other && Equals(other);
    public bool Equals(CanonicalLibrarySyncDiagnostic? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalLibrarySyncDiagnostic left, CanonicalLibrarySyncDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalLibrarySyncDiagnostic left, CanonicalLibrarySyncDiagnostic right) => !left.Equals(right);
}

public sealed class CanonicalLibrarySyncPlan : IEquatable<CanonicalLibrarySyncPlan>
{
    public List<CanonicalLibrarySyncAction> Actions { get; }
    public List<CanonicalApplyAction> ApplyActions { get; }
    public List<CanonicalLibraryConflict> Conflicts { get; }
    public List<CanonicalLibraryTombstone> Tombstones { get; }
    public List<CanonicalLibrarySyncDiagnostic> Diagnostics { get; }
    public List<CanonicalLibraryObjectID> FallbackRequiredObjectIDs { get; }

    public CanonicalLibrarySyncPlan(
        List<CanonicalLibrarySyncAction>? actions = null,
        List<CanonicalApplyAction>? applyActions = null,
        List<CanonicalLibraryConflict>? conflicts = null,
        List<CanonicalLibraryTombstone>? tombstones = null,
        List<CanonicalLibrarySyncDiagnostic>? diagnostics = null,
        List<CanonicalLibraryObjectID>? fallbackRequiredObjectIDs = null)
    {
        Actions = actions ?? new List<CanonicalLibrarySyncAction>();
        ApplyActions = applyActions ?? new List<CanonicalApplyAction>();
        Conflicts = conflicts ?? new List<CanonicalLibraryConflict>();
        Tombstones = tombstones ?? new List<CanonicalLibraryTombstone>();
        Diagnostics = diagnostics ?? new List<CanonicalLibrarySyncDiagnostic>();
        FallbackRequiredObjectIDs = new HashSet<CanonicalLibraryObjectID>(
            fallbackRequiredObjectIDs ?? new List<CanonicalLibraryObjectID>())
            .OrderBy(id => id.RawValue, StringComparer.Ordinal)
            .ToList();
    }

    public CanonicalLibrarySyncPlan Deduplicated()
    {
        var seenActions = new HashSet<string>();
        var seenApplyActions = new HashSet<string>();
        var seenConflicts = new HashSet<string>();
        var seenTombstones = new HashSet<string>();
        var seenDiagnostics = new HashSet<string>();
        return new CanonicalLibrarySyncPlan(
            actions: Actions.Where(a => seenActions.Add(a.ActionID)).ToList(),
            applyActions: ApplyActions.Where(a => seenApplyActions.Add(a.ActionID)).ToList(),
            conflicts: Conflicts.Where(c => seenConflicts.Add(c.ConflictID)).ToList(),
            tombstones: Tombstones.Where(t => seenTombstones.Add(t.TombstoneID)).ToList(),
            diagnostics: Diagnostics.Where(d => seenDiagnostics.Add(d.Id)).ToList(),
            fallbackRequiredObjectIDs: FallbackRequiredObjectIDs
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalLibrarySyncPlan other && Equals(other);
    public bool Equals(CanonicalLibrarySyncPlan? other) =>
        other is not null &&
        Actions.SequenceEqual(other.Actions) &&
        ApplyActions.SequenceEqual(other.ApplyActions) &&
        Conflicts.SequenceEqual(other.Conflicts) &&
        Tombstones.SequenceEqual(other.Tombstones) &&
        Diagnostics.SequenceEqual(other.Diagnostics) &&
        FallbackRequiredObjectIDs.SequenceEqual(other.FallbackRequiredObjectIDs);
    public override int GetHashCode() => HashCode.Combine(Actions.Count, Conflicts.Count, Tombstones.Count);
    public static bool operator ==(CanonicalLibrarySyncPlan left, CanonicalLibrarySyncPlan right) => left.Equals(right);
    public static bool operator !=(CanonicalLibrarySyncPlan left, CanonicalLibrarySyncPlan right) => !left.Equals(right);
}

public class CanonicalLibrarySyncPlanner
{
    public CanonicalLibrarySyncPlan Plan(
        CanonicalManifest local,
        CanonicalManifest peer,
        CanonicalSyncPlanTrigger trigger)
    {
        var plan = new CanonicalLibrarySyncPlan();
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh)
        {
            plan.Diagnostics.Add(
                new CanonicalLibrarySyncDiagnostic(
                    phase: "canonicalDomainFallback",
                    objectID: null,
                    objectKind: null,
                    detail: "viewRefreshProjectionOnly"
                )
            );
            return plan;
        }
        if (trigger == CanonicalSyncPlanTrigger.retryDrainer)
        {
            plan.Diagnostics.Add(
                new CanonicalLibrarySyncDiagnostic(
                    phase: "canonicalDomainFallback",
                    objectID: null,
                    objectKind: null,
                    detail: "retryDrainerNoFreshLibraryTransfer"
                )
            );
            return plan;
        }
        if (!HasLibraryCapability(local) || !HasLibraryCapability(peer))
        {
            plan.FallbackRequiredObjectIDs.AddRange(CombinedObjectIDs(local: local, peer: peer));
            plan.Diagnostics.Add(
                new CanonicalLibrarySyncDiagnostic(
                    phase: "canonicalDomainFallback",
                    objectID: null,
                    objectKind: null,
                    detail: "canonicalLibraryObjectsCapabilityMissing"
                )
            );
            return plan;
        }

        var localObjects = LibraryObjectsByID(local.LibraryObjects);
        var peerObjects = LibraryObjectsByID(peer.LibraryObjects);
        var allIDs = localObjects.Keys.Union(peerObjects.Keys)
            .OrderBy(id => id.RawValue, StringComparer.Ordinal).ToList();
        foreach (var objectID in allIDs)
        {
            AppendDecision(
                objectID: objectID,
                localObject: localObjects.GetValueOrDefault(objectID),
                peerObject: peerObjects.GetValueOrDefault(objectID),
                plan: plan);
        }
        plan.Diagnostics.Add(
            new CanonicalLibrarySyncDiagnostic(
                phase: "canonicalLibraryObjectsProjected",
                objectID: null,
                objectKind: null,
                detail: $"local={local.LibraryObjects.Length},peer={peer.LibraryObjects.Length},actions={plan.Actions.Count}"
            )
        );
        return plan.Deduplicated();
    }

    private void AppendDecision(
        CanonicalLibraryObjectID objectID,
        CanonicalLibraryObject? localObject,
        CanonicalLibraryObject? peerObject,
        CanonicalLibrarySyncPlan plan)
    {
        var obj = localObject ?? peerObject;
        if (obj == null)
            return;
        if (!IsSupported(obj) || (peerObject != null && !IsSupported(peerObject)))
        {
            AppendUnsupported(objectID: objectID, obj: obj, plan: plan);
            return;
        }
        switch (obj.Kind)
        {
            case CanonicalObjectKind.folder:
                AppendMetadataDecision(
                    objectID: objectID,
                    objectKind: CanonicalObjectKind.folder,
                    localObject: localObject,
                    peerObject: peerObject,
                    sendKind: CanonicalLibraryActionKind.folderMetadataSend,
                    applyKind: CanonicalLibraryActionKind.folderMetadataApply,
                    noOpKind: CanonicalLibraryActionKind.folderMetadataNoOp,
                    conflictKind: CanonicalLibraryActionKind.folderConflict,
                    tombstoneSendKind: CanonicalLibraryActionKind.folderTombstoneSend,
                    tombstoneApplyKind: CanonicalLibraryActionKind.folderTombstoneApply,
                    plan: plan);
                break;
            case CanonicalObjectKind.standaloneStudyItem:
            case CanonicalObjectKind.standaloneNote:
            case CanonicalObjectKind.recordingAssociatedStudyItem:
                AppendMetadataDecision(
                    objectID: objectID,
                    objectKind: obj.Kind,
                    localObject: localObject,
                    peerObject: peerObject,
                    sendKind: CanonicalLibraryActionKind.studyItemMetadataSend,
                    applyKind: CanonicalLibraryActionKind.studyItemMetadataApply,
                    noOpKind: CanonicalLibraryActionKind.studyItemMetadataNoOp,
                    conflictKind: CanonicalLibraryActionKind.studyItemConflict,
                    tombstoneSendKind: CanonicalLibraryActionKind.studyItemTombstoneSend,
                    tombstoneApplyKind: CanonicalLibraryActionKind.studyItemTombstoneApply,
                    plan: plan);
                break;
            default:
                AppendUnsupported(objectID: objectID, obj: obj, plan: plan);
                break;
        }
    }

    private void AppendMetadataDecision(
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        CanonicalLibraryObject? localObject,
        CanonicalLibraryObject? peerObject,
        CanonicalLibraryActionKind sendKind,
        CanonicalLibraryActionKind applyKind,
        CanonicalLibraryActionKind noOpKind,
        CanonicalLibraryActionKind conflictKind,
        CanonicalLibraryActionKind tombstoneSendKind,
        CanonicalLibraryActionKind tombstoneApplyKind,
        CanonicalLibrarySyncPlan plan)
    {
        var localDeletedAt = localObject?.DeletedAt;
        var peerDeletedAt = peerObject?.DeletedAt;
        if (AppendTombstoneDecision(
                objectID: objectID,
                objectKind: objectKind,
                localObject: localObject,
                peerObject: peerObject,
                localDeletedAt: localDeletedAt,
                peerDeletedAt: peerDeletedAt,
                tombstoneSendKind: tombstoneSendKind,
                tombstoneApplyKind: tombstoneApplyKind,
                plan: plan))
        {
            return;
        }

        if (localObject != null && peerObject != null)
        {
            if (SameHash(localObject.MetadataHash, peerObject.MetadataHash))
            {
                AppendAction(
                    noOpKind,
                    objectID: objectID,
                    objectKind: objectKind,
                    source: CanonicalApplySource.planner,
                    reason: "metadataHashEqual",
                    localObject: localObject,
                    peerObject: peerObject,
                    plan: plan);
            }
            else if (localObject.BusinessModifiedAt.HasValue &&
                     peerObject.BusinessModifiedAt.HasValue &&
                     localObject.BusinessModifiedAt.Value.Date > peerObject.BusinessModifiedAt.Value.Date)
            {
                AppendActionAndApply(
                    sendKind,
                    objectID: objectID,
                    objectKind: objectKind,
                    source: CanonicalApplySource.local,
                    reason: "localMetadataNewer",
                    localObject: localObject,
                    peerObject: peerObject,
                    plan: plan);
            }
            else if (localObject.BusinessModifiedAt.HasValue &&
                     peerObject.BusinessModifiedAt.HasValue &&
                     peerObject.BusinessModifiedAt.Value.Date > localObject.BusinessModifiedAt.Value.Date)
            {
                AppendActionAndApply(
                    applyKind,
                    objectID: objectID,
                    objectKind: objectKind,
                    source: CanonicalApplySource.peer,
                    reason: "peerMetadataNewer",
                    localObject: localObject,
                    peerObject: peerObject,
                    plan: plan);
            }
            else
            {
                AppendConflict(
                    actionKind: conflictKind,
                    conflictKind: objectKind == CanonicalObjectKind.folder
                        ? CanonicalLibraryConflictKind.folderMetadataConcurrentEdit
                        : CanonicalLibraryConflictKind.studyItemMetadataConcurrentEdit,
                    objectID: objectID,
                    objectKind: objectKind,
                    localObject: localObject,
                    peerObject: peerObject,
                    reason: "metadataTieConflict",
                    plan: plan);
            }
        }
        else if (localObject != null && peerObject == null)
        {
            AppendActionAndApply(
                sendKind,
                objectID: objectID,
                objectKind: objectKind,
                source: CanonicalApplySource.local,
                reason: "peerMissingMetadata",
                localObject: localObject,
                peerObject: null,
                plan: plan);
        }
        else if (localObject == null && peerObject != null)
        {
            AppendActionAndApply(
                applyKind,
                objectID: objectID,
                objectKind: objectKind,
                source: CanonicalApplySource.peer,
                reason: "localMissingMetadata",
                localObject: null,
                peerObject: peerObject,
                plan: plan);
        }
    }

    private bool AppendTombstoneDecision(
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        CanonicalLibraryObject? localObject,
        CanonicalLibraryObject? peerObject,
        CanonicalTimestamp? localDeletedAt,
        CanonicalTimestamp? peerDeletedAt,
        CanonicalLibraryActionKind tombstoneSendKind,
        CanonicalLibraryActionKind tombstoneApplyKind,
        CanonicalLibrarySyncPlan plan)
    {
        if (localObject != null && localObject.IsDeleted)
        {
            plan.Tombstones.Add(LibraryTombstone(obj: localObject, reason: CanonicalLibraryTombstoneReason.localTombstoneNewer));
        }
        if (peerObject != null && peerObject.IsDeleted)
        {
            plan.Tombstones.Add(LibraryTombstone(obj: peerObject, reason: CanonicalLibraryTombstoneReason.peerTombstoneNewer));
        }

        if (localObject != null && peerObject != null && localDeletedAt.HasValue && peerDeletedAt.HasValue)
        {
            if (localDeletedAt.Value.Date > peerDeletedAt.Value.Date)
            {
                AppendActionAndApply(tombstoneSendKind, objectID: objectID, objectKind: objectKind,
                    source: CanonicalApplySource.local, reason: "localTombstoneNewer",
                    localObject: localObject, peerObject: peerObject, plan: plan);
            }
            else if (peerDeletedAt.Value.Date > localDeletedAt.Value.Date)
            {
                AppendActionAndApply(tombstoneApplyKind, objectID: objectID, objectKind: objectKind,
                    source: CanonicalApplySource.peer, reason: "peerTombstoneNewer",
                    localObject: localObject, peerObject: peerObject, plan: plan);
            }
            return true;
        }

        if (localObject != null && peerObject != null && !localDeletedAt.HasValue && peerDeletedAt.HasValue)
        {
            if (localObject.BusinessModifiedAt.HasValue &&
                localObject.BusinessModifiedAt.Value.Date > peerDeletedAt.Value.Date)
            {
                AppendConflict(
                    actionKind: objectKind == CanonicalObjectKind.folder
                        ? CanonicalLibraryActionKind.folderConflict
                        : CanonicalLibraryActionKind.studyItemConflict,
                    conflictKind: CanonicalLibraryConflictKind.activeVsTombstone,
                    objectID: objectID, objectKind: objectKind,
                    localObject: localObject, peerObject: peerObject,
                    reason: "activeNewerThanPeerTombstone", plan: plan);
            }
            else
            {
                AppendActionAndApply(tombstoneApplyKind, objectID: objectID, objectKind: objectKind,
                    source: CanonicalApplySource.peer, reason: "peerTombstoneNewer",
                    localObject: localObject, peerObject: peerObject, plan: plan);
            }
            return true;
        }

        if (localObject != null && peerObject != null && localDeletedAt.HasValue && !peerDeletedAt.HasValue)
        {
            if (peerObject.BusinessModifiedAt.HasValue &&
                peerObject.BusinessModifiedAt.Value.Date > localDeletedAt.Value.Date)
            {
                AppendConflict(
                    actionKind: objectKind == CanonicalObjectKind.folder
                        ? CanonicalLibraryActionKind.folderConflict
                        : CanonicalLibraryActionKind.studyItemConflict,
                    conflictKind: CanonicalLibraryConflictKind.activeVsTombstone,
                    objectID: objectID, objectKind: objectKind,
                    localObject: localObject, peerObject: peerObject,
                    reason: "activeNewerThanLocalTombstone", plan: plan);
            }
            else
            {
                AppendActionAndApply(tombstoneSendKind, objectID: objectID, objectKind: objectKind,
                    source: CanonicalApplySource.local, reason: "localTombstoneNewer",
                    localObject: localObject, peerObject: peerObject, plan: plan);
            }
            return true;
        }

        if (localObject == null && peerObject != null && peerDeletedAt.HasValue)
        {
            AppendActionAndApply(tombstoneApplyKind, objectID: objectID, objectKind: objectKind,
                source: CanonicalApplySource.peer, reason: "peerTombstoneNewer",
                localObject: null, peerObject: peerObject, plan: plan);
            return true;
        }

        if (localObject != null && peerObject == null && localDeletedAt.HasValue)
        {
            AppendActionAndApply(tombstoneSendKind, objectID: objectID, objectKind: objectKind,
                source: CanonicalApplySource.local, reason: "localTombstoneNewer",
                localObject: localObject, peerObject: null, plan: plan);
            return true;
        }

        return false;
    }

    private void AppendActionAndApply(
        CanonicalLibraryActionKind kind,
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        CanonicalApplySource source,
        string reason,
        CanonicalLibraryObject? localObject,
        CanonicalLibraryObject? peerObject,
        CanonicalLibrarySyncPlan plan)
    {
        AppendAction(kind, objectID: objectID, objectKind: objectKind, source: source,
            reason: reason, localObject: localObject, peerObject: peerObject, plan: plan);
        var applyKind = ApplyActionKindFor(kind);
        if (applyKind == null)
            return;
        var bridge = source == CanonicalApplySource.peer
            ? CanonicalApplyBridgeHint.legacyMetadataManifestApply
            : CanonicalApplyBridgeHint.legacyMetadataManifestSend;
        plan.ApplyActions.Add(
            new CanonicalApplyAction(
                kind: applyKind.Value,
                source: source,
                target: new CanonicalApplyTarget(objectID: objectID.RawValue),
                bridgeHint: bridge,
                preconditions: ApplyPreconditions(localObject: localObject, peerObject: peerObject),
                reason: reason
            )
        );
        plan.Diagnostics.Add(
            new CanonicalLibrarySyncDiagnostic(
                phase: "canonicalLibraryActionBridged",
                objectID: objectID,
                objectKind: objectKind,
                detail: $"{kind}->{bridge}"
            )
        );
    }

    private void AppendAction(
        CanonicalLibraryActionKind kind,
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        CanonicalApplySource source,
        string reason,
        CanonicalLibraryObject? localObject,
        CanonicalLibraryObject? peerObject,
        CanonicalLibrarySyncPlan plan)
    {
        plan.Actions.Add(
            new CanonicalLibrarySyncAction(
                kind: kind,
                objectID: objectID,
                objectKind: objectKind,
                source: source,
                reason: reason,
                localHash: localObject?.MetadataHash,
                peerHash: peerObject?.MetadataHash,
                localModifiedAt: localObject?.BusinessModifiedAt,
                peerModifiedAt: peerObject?.BusinessModifiedAt
            )
        );
        string phase;
        switch (objectKind)
        {
            case CanonicalObjectKind.folder:
                phase = kind == CanonicalLibraryActionKind.folderMetadataNoOp
                    ? "canonicalFolderMetadataHashConverged"
                    : "canonicalFolderPlanned";
                break;
            case CanonicalObjectKind.standaloneStudyItem:
            case CanonicalObjectKind.standaloneNote:
            case CanonicalObjectKind.recordingAssociatedStudyItem:
                phase = kind == CanonicalLibraryActionKind.studyItemMetadataNoOp
                    ? "canonicalStudyItemMetadataHashConverged"
                    : "canonicalStudyItemPlanned";
                break;
            default:
                phase = "canonicalLibraryObjectPlanned";
                break;
        }
        plan.Diagnostics.Add(
            new CanonicalLibrarySyncDiagnostic(phase: phase, objectID: objectID,
                objectKind: objectKind, detail: reason)
        );
    }

    private void AppendConflict(
        CanonicalLibraryActionKind actionKind,
        CanonicalLibraryConflictKind conflictKind,
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        CanonicalLibraryObject localObject,
        CanonicalLibraryObject peerObject,
        string reason,
        CanonicalLibrarySyncPlan plan)
    {
        AppendAction(actionKind, objectID: objectID, objectKind: objectKind, source: CanonicalApplySource.planner,
            reason: reason, localObject: localObject, peerObject: peerObject, plan: plan);
        var conflict = new CanonicalLibraryConflict(
            kind: conflictKind,
            objectID: objectID,
            objectKind: objectKind,
            localHash: localObject.MetadataHash,
            peerHash: peerObject.MetadataHash,
            localModifiedAt: localObject.BusinessModifiedAt,
            peerModifiedAt: peerObject.BusinessModifiedAt,
            detail: reason
        );
        plan.Conflicts.Add(conflict);
        plan.ApplyActions.Add(
            new CanonicalApplyAction(
                kind: CanonicalApplyActionKind.conflictRecord,
                source: CanonicalApplySource.planner,
                target: new CanonicalApplyTarget(objectID: objectID.RawValue),
                result: CanonicalApplyResult.conflictRecorded,
                failureReason: CanonicalApplyFailureReason.conflictDetected,
                conflictID: conflict.ConflictID,
                reason: reason
            )
        );
        plan.Diagnostics.Add(
            new CanonicalLibrarySyncDiagnostic(
                phase: "canonicalLibraryConflictRecorded",
                objectID: objectID,
                objectKind: objectKind,
                detail: conflictKind.ToString())
        );
    }

    private void AppendUnsupported(
        CanonicalLibraryObjectID objectID,
        CanonicalLibraryObject obj,
        CanonicalLibrarySyncPlan plan)
    {
        plan.FallbackRequiredObjectIDs.Add(objectID);
        plan.Actions.Add(
            new CanonicalLibrarySyncAction(
                kind: CanonicalLibraryActionKind.unsupportedFallback,
                objectID: objectID,
                objectKind: obj.Kind,
                source: CanonicalApplySource.planner,
                reason: obj.UnsupportedReason ?? "unsupportedLibraryObject",
                localHash: obj.MetadataHash
            )
        );
        plan.Diagnostics.Add(
            new CanonicalLibrarySyncDiagnostic(
                phase: "canonicalLibraryObjectUnsupported",
                objectID: objectID,
                objectKind: obj.Kind,
                detail: obj.UnsupportedReason ?? "unsupportedLibraryObject"
            )
        );
    }

    private CanonicalApplyActionKind? ApplyActionKindFor(CanonicalLibraryActionKind kind) =>
        kind switch
        {
            CanonicalLibraryActionKind.folderMetadataApply => CanonicalApplyActionKind.folderMetadataApply,
            CanonicalLibraryActionKind.folderMetadataSend => CanonicalApplyActionKind.folderMetadataSend,
            CanonicalLibraryActionKind.folderTombstoneApply => CanonicalApplyActionKind.libraryTombstoneApply,
            CanonicalLibraryActionKind.folderTombstoneSend => CanonicalApplyActionKind.libraryTombstoneSend,
            CanonicalLibraryActionKind.studyItemMetadataApply => CanonicalApplyActionKind.studyItemMetadataApply,
            CanonicalLibraryActionKind.studyItemMetadataSend => CanonicalApplyActionKind.studyItemMetadataSend,
            CanonicalLibraryActionKind.studyItemTombstoneApply => CanonicalApplyActionKind.libraryTombstoneApply,
            CanonicalLibraryActionKind.studyItemTombstoneSend => CanonicalApplyActionKind.libraryTombstoneSend,
            CanonicalLibraryActionKind.folderMetadataNoOp => null,
            CanonicalLibraryActionKind.folderConflict => null,
            CanonicalLibraryActionKind.studyItemMetadataNoOp => null,
            CanonicalLibraryActionKind.studyItemConflict => null,
            CanonicalLibraryActionKind.unsupportedFallback => null,
            CanonicalLibraryActionKind.deferred => null,
            _ => null
        };

    private List<CanonicalApplyPrecondition> ApplyPreconditions(
        CanonicalLibraryObject? localObject,
        CanonicalLibraryObject? peerObject)
    {
        var preconditions = new List<CanonicalApplyPrecondition>();
        if (localObject?.BusinessModifiedAt.HasValue == true)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.localModifiedAt,
                value: TimestampString(localObject.BusinessModifiedAt.Value)));
        if (peerObject?.BusinessModifiedAt.HasValue == true)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.peerModifiedAt,
                value: TimestampString(peerObject.BusinessModifiedAt.Value)));
        if (localObject != null)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.localHashPrefix,
                value: localObject.MetadataHash.Value[..Math.Min(localObject.MetadataHash.Value.Length, 12)]));
        if (peerObject != null)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.peerHashPrefix,
                value: peerObject.MetadataHash.Value[..Math.Min(peerObject.MetadataHash.Value.Length, 12)]));
        preconditions.Add(new CanonicalApplyPrecondition(
            kind: CanonicalApplyPrecondition.Kind.legacyBridge,
            value: "metadataManifest"));
        return preconditions;
    }

    private Dictionary<CanonicalLibraryObjectID, CanonicalLibraryObject> LibraryObjectsByID(
        CanonicalLibraryObject[] objects)
    {
        var dict = new Dictionary<CanonicalLibraryObjectID, CanonicalLibraryObject>();
        foreach (var obj in objects)
        {
            if (!dict.ContainsKey(obj.ObjectID))
                dict[obj.ObjectID] = obj;
        }
        return dict;
    }

    private List<CanonicalLibraryObjectID> CombinedObjectIDs(
        CanonicalManifest local, CanonicalManifest peer)
    {
        return local.LibraryObjects.Select(o => o.ObjectID)
            .Concat(peer.LibraryObjects.Select(o => o.ObjectID))
            .Distinct()
            .OrderBy(id => id.RawValue, StringComparer.Ordinal)
            .ToList();
    }

    private static bool HasLibraryCapability(CanonicalManifest manifest)
    {
        return manifest.Node.Capabilities.Contains(CanonicalCapability.canonicalLibraryObjectsV1)
            || manifest.ManifestCapabilities.Contains(CanonicalCapability.canonicalLibraryObjectsV1);
    }

    private static bool IsSupported(CanonicalLibraryObject obj)
    {
        return obj.Kind != CanonicalObjectKind.unknownUnsupported
            && obj.Kind != CanonicalObjectKind.generatedArtifactEnvelope;
    }

    private static bool SameHash(CanonicalHash left, CanonicalHash right)
    {
        return left.Algorithm == right.Algorithm && left.Value == right.Value;
    }

    private CanonicalLibraryTombstone LibraryTombstone(
        CanonicalLibraryObject obj,
        CanonicalLibraryTombstoneReason reason)
    {
        return new CanonicalLibraryTombstone(
            objectID: obj.ObjectID,
            objectKind: obj.Kind,
            deletedAt: obj.DeletedAt,
            reason: reason
        );
    }

    private static string TimestampString(CanonicalTimestamp timestamp)
    {
        var seconds = (timestamp.Date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
        return seconds.ToString("F6", CultureInfo.InvariantCulture);
    }
}
