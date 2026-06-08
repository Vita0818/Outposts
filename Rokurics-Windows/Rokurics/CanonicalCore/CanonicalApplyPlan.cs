using System.Globalization;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyActionKind
{
    recordingMetadataApply,
    recordingMetadataSend,
    folderMetadataApply,
    folderMetadataSend,
    studyItemMetadataApply,
    studyItemMetadataSend,
    libraryTombstoneApply,
    libraryTombstoneSend,
    generatedArtifactDownloadApply,
    generatedArtifactNoOp,
    objectTombstoneApply,
    objectTombstoneSend,
    artifactTombstoneApply,
    conflictRecord,
    deferredUnsupported
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplySource
{
    local,
    peer,
    planner
}

public sealed class CanonicalApplyTarget : IEquatable<CanonicalApplyTarget>
{
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }

    public CanonicalApplyTarget(
        string objectID,
        string? artifactID = null,
        CanonicalArtifact.Kind? artifactKind = null)
    {
        ObjectID = NormalizedRequired(objectID, "unknown-recording");
        ArtifactID = artifactID?.Trim().NilIfEmpty();
        ArtifactKind = artifactKind;
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyTarget other && Equals(other);
    public bool Equals(CanonicalApplyTarget? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        ArtifactID == other.ArtifactID &&
        ArtifactKind == other.ArtifactKind;
    public override int GetHashCode() => HashCode.Combine(ObjectID, ArtifactID, ArtifactKind);
    public static bool operator ==(CanonicalApplyTarget left, CanonicalApplyTarget right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyTarget left, CanonicalApplyTarget right) => !left.Equals(right);

    private static string NormalizedRequired(string value, string fallback) =>
        value.Trim().NilIfEmpty() ?? fallback;
}

public sealed class CanonicalApplyPrecondition : IEquatable<CanonicalApplyPrecondition>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Kind
    {
        localModifiedAt,
        peerModifiedAt,
        localHashPrefix,
        peerHashPrefix,
        peerByteSize,
        tombstoneTimestamp,
        noPhysicalDelete,
        legacyBridge
    }

    public Kind PreconditionKind { get; }
    public string Value { get; }

    public CanonicalApplyPrecondition(Kind kind, string value)
    {
        PreconditionKind = kind;
        Value = value.Trim();
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyPrecondition other && Equals(other);
    public bool Equals(CanonicalApplyPrecondition? other) =>
        other is not null &&
        PreconditionKind == other.PreconditionKind &&
        Value == other.Value;
    public override int GetHashCode() => HashCode.Combine(PreconditionKind, Value);
    public static bool operator ==(CanonicalApplyPrecondition left, CanonicalApplyPrecondition right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyPrecondition left, CanonicalApplyPrecondition right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyResult
{
    planned,
    noOp,
    conflictRecorded,
    deferredUnsupported
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyFailureReason
{
    unsupportedRoute,
    conflictDetected,
    tombstoneBlocksResurrection,
    legacyArtifactMissing,
    noPhysicalDeletePolicy,
    hashOrSizeMismatch
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyBridgeHint
{
    legacyMetadataManifestApply,
    legacyMetadataManifestSend,
    legacyArtifactRequestApply,
    noGeneratedArtifactUploadJob,
    noPhysicalDelete,
    unsupportedNoRoute,
    legacyFallbackPreserved
}

public sealed class CanonicalApplyAction : IEquatable<CanonicalApplyAction>
{
    public string Id => ActionID;

    public string ActionID { get; }
    public CanonicalApplyActionKind Kind { get; }
    public CanonicalApplySource Source { get; }
    public CanonicalApplyTarget Target { get; }
    public CanonicalApplyBridgeHint? BridgeHint { get; }
    public List<CanonicalApplyPrecondition> Preconditions { get; }
    public CanonicalApplyResult Result { get; }
    public CanonicalApplyFailureReason? FailureReason { get; }
    public string? ConflictID { get; }
    public string? TombstoneID { get; }
    public string Reason { get; }

    public CanonicalApplyAction(
        CanonicalApplyActionKind kind,
        CanonicalApplySource source,
        CanonicalApplyTarget target,
        CanonicalApplyBridgeHint? bridgeHint = null,
        List<CanonicalApplyPrecondition>? preconditions = null,
        CanonicalApplyResult result = CanonicalApplyResult.planned,
        CanonicalApplyFailureReason? failureReason = null,
        string? conflictID = null,
        string? tombstoneID = null,
        string reason = null!)
    {
        Kind = kind;
        Source = source;
        Target = target;
        BridgeHint = bridgeHint;
        Preconditions = preconditions ?? new List<CanonicalApplyPrecondition>();
        Result = result;
        FailureReason = failureReason;
        ConflictID = conflictID?.Trim().NilIfEmpty();
        TombstoneID = tombstoneID?.Trim().NilIfEmpty();
        Reason = reason?.Trim().NilIfEmpty() ?? Kind.ToString();
        ActionID = MakeActionID(kind, target, Reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyAction other && Equals(other);
    public bool Equals(CanonicalApplyAction? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
    public static bool operator ==(CanonicalApplyAction left, CanonicalApplyAction right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyAction left, CanonicalApplyAction right) => !left.Equals(right);

    private static string MakeActionID(
        CanonicalApplyActionKind kind,
        CanonicalApplyTarget target,
        string reason)
    {
        return string.Join("|",
            kind.ToString(),
            target.ObjectID,
            target.ArtifactKind?.ToString() ?? "object",
            target.ArtifactID ?? "metadata",
            reason);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalConflictKind
{
    recordingMetadataConcurrentEdit,
    recordingAudioContentMismatch,
    generatedArtifactContentMismatch,
    activeVsTombstone
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalConflictSeverity
{
    warning,
    blocking
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalConflictResolutionPolicy
{
    manualReview,
    keepBothNoOverwrite,
    tombstoneRequiresManualReview
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalConflictResolutionState
{
    unresolved,
    resolved,
    ignored
}

public sealed class CanonicalConflictRecord : IEquatable<CanonicalConflictRecord>
{
    public string Id => ConflictID;

    public string ConflictID { get; }
    public CanonicalConflictKind Kind { get; }
    public CanonicalConflictSeverity Severity { get; }
    public CanonicalConflictResolutionPolicy ResolutionPolicy { get; }
    public CanonicalConflictResolutionState ResolutionState { get; }
    public CanonicalApplyTarget Target { get; }
    public string? LocalHashPrefix { get; }
    public string? PeerHashPrefix { get; }
    public CanonicalTimestamp? LocalModifiedAt { get; }
    public CanonicalTimestamp? PeerModifiedAt { get; }
    public string? Detail { get; }

    public CanonicalConflictRecord(
        CanonicalConflictKind kind,
        CanonicalApplyTarget target,
        CanonicalConflictSeverity severity = CanonicalConflictSeverity.blocking,
        CanonicalConflictResolutionPolicy resolutionPolicy = default,
        CanonicalConflictResolutionState resolutionState = CanonicalConflictResolutionState.unresolved,
        CanonicalHash? localHash = null,
        CanonicalHash? peerHash = null,
        CanonicalTimestamp? localModifiedAt = null,
        CanonicalTimestamp? peerModifiedAt = null,
        string? detail = null)
    {
        Kind = kind;
        Severity = severity;
        ResolutionPolicy = resolutionPolicy;
        ResolutionState = resolutionState;
        Target = target;
        LocalHashPrefix = localHash.HasValue ? HashPrefix(localHash.Value) : null;
        PeerHashPrefix = peerHash.HasValue ? HashPrefix(peerHash.Value) : null;
        LocalModifiedAt = localModifiedAt;
        PeerModifiedAt = peerModifiedAt;
        Detail = detail?.Trim().NilIfEmpty();
        ConflictID = string.Join("|",
            "conflict",
            Kind.ToString(),
            target.ObjectID,
            target.ArtifactKind?.ToString() ?? "metadata",
            target.ArtifactID ?? "object");
    }

    public override bool Equals(object? obj) => obj is CanonicalConflictRecord other && Equals(other);
    public bool Equals(CanonicalConflictRecord? other) =>
        other is not null && ConflictID == other.ConflictID;
    public override int GetHashCode() => ConflictID.GetHashCode();
    public static bool operator ==(CanonicalConflictRecord left, CanonicalConflictRecord right) => left.Equals(right);
    public static bool operator !=(CanonicalConflictRecord left, CanonicalConflictRecord right) => !left.Equals(right);

    private static string HashPrefix(CanonicalHash hash) =>
        hash.Value[..Math.Min(hash.Value.Length, 12)];
}

public sealed class CanonicalConflictDiagnostics : IEquatable<CanonicalConflictDiagnostics>
{
    public int Total { get; }
    public int Metadata { get; }
    public int Audio { get; }
    public int GeneratedArtifact { get; }
    public int Tombstone { get; }

    public CanonicalConflictDiagnostics(int total, int metadata, int audio, int generatedArtifact, int tombstone)
    {
        Total = total;
        Metadata = metadata;
        Audio = audio;
        GeneratedArtifact = generatedArtifact;
        Tombstone = tombstone;
    }

    public override bool Equals(object? obj) => obj is CanonicalConflictDiagnostics other && Equals(other);
    public bool Equals(CanonicalConflictDiagnostics? other) =>
        other is not null &&
        Total == other.Total &&
        Metadata == other.Metadata &&
        Audio == other.Audio &&
        GeneratedArtifact == other.GeneratedArtifact &&
        Tombstone == other.Tombstone;
    public override int GetHashCode() => HashCode.Combine(Total, Metadata, Audio, GeneratedArtifact, Tombstone);
    public static bool operator ==(CanonicalConflictDiagnostics left, CanonicalConflictDiagnostics right) => left.Equals(right);
    public static bool operator !=(CanonicalConflictDiagnostics left, CanonicalConflictDiagnostics right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneState
{
    active,
    tombstoned
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalDeletionReason
{
    softDelete,
    peerTombstoneNewer,
    localTombstoneNewer,
    artifactTombstonePresent
}

public sealed class CanonicalTombstone : IEquatable<CanonicalTombstone>
{
    public string Id => TombstoneID;

    public string TombstoneID { get; }
    public CanonicalApplyTarget Target { get; }
    public CanonicalTombstoneState State { get; }
    public CanonicalDeletionReason Reason { get; }
    public CanonicalTimestamp? DeletedAt { get; }
    public string? SourceNodeID { get; }
    public List<CanonicalTombstonePolicy> Policies { get; }

    public CanonicalTombstone(
        CanonicalApplyTarget target,
        CanonicalTombstoneState state,
        CanonicalDeletionReason reason,
        CanonicalTimestamp? deletedAt,
        string? sourceNodeID,
        List<CanonicalTombstonePolicy>? policies = null)
    {
        Target = target;
        State = state;
        Reason = reason;
        DeletedAt = deletedAt;
        SourceNodeID = sourceNodeID?.Trim().NilIfEmpty();
        Policies = policies ?? new List<CanonicalTombstonePolicy>
        {
            CanonicalTombstonePolicy.softDeleteOnly,
            CanonicalTombstonePolicy.antiResurrection,
            CanonicalTombstonePolicy.noPhysicalDelete,
            CanonicalTombstonePolicy.noPermanentDelete,
            CanonicalTombstonePolicy.noGarbageCollection
        };
        Policies = new HashSet<CanonicalTombstonePolicy>(Policies)
            .OrderBy(p => p.ToString(), StringComparer.Ordinal)
            .ToList();
        TombstoneID = string.Join("|",
            "tombstone",
            target.ObjectID,
            target.ArtifactKind?.ToString() ?? "object",
            target.ArtifactID ?? "metadata");
    }

    public override bool Equals(object? obj) => obj is CanonicalTombstone other && Equals(other);
    public bool Equals(CanonicalTombstone? other) =>
        other is not null && TombstoneID == other.TombstoneID;
    public override int GetHashCode() => TombstoneID.GetHashCode();
    public static bool operator ==(CanonicalTombstone left, CanonicalTombstone right) => left.Equals(right);
    public static bool operator !=(CanonicalTombstone left, CanonicalTombstone right) => !left.Equals(right);
}

public sealed class CanonicalApplyDiagnostic : IEquatable<CanonicalApplyDiagnostic>
{
    public string Id => string.Join("|", Phase, Target.ObjectID, Target.ArtifactID ?? "", Detail ?? "");

    public string Phase { get; }
    public CanonicalApplyTarget Target { get; }
    public string? Detail { get; }

    public CanonicalApplyDiagnostic(string phase, CanonicalApplyTarget target, string? detail = null)
    {
        Phase = phase;
        Target = target;
        Detail = detail;
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyDiagnostic other && Equals(other);
    public bool Equals(CanonicalApplyDiagnostic? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalApplyDiagnostic left, CanonicalApplyDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyDiagnostic left, CanonicalApplyDiagnostic right) => !left.Equals(right);
}

public sealed class CanonicalApplyPlan : IEquatable<CanonicalApplyPlan>
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }
    public List<CanonicalApplyAction> Actions { get; }
    public List<CanonicalConflictRecord> Conflicts { get; }
    public List<CanonicalTombstone> Tombstones { get; }
    public List<CanonicalApplyDiagnostic> Diagnostics { get; }
    public CanonicalConflictDiagnostics ConflictDiagnostics { get; }

    public CanonicalApplyPlan(
        CanonicalSyncPlanTrigger trigger,
        List<CanonicalApplyAction>? actions = null,
        List<CanonicalConflictRecord>? conflicts = null,
        List<CanonicalTombstone>? tombstones = null,
        List<CanonicalApplyDiagnostic>? diagnostics = null)
    {
        SchemaVersion = CurrentSchemaVersion;
        Trigger = trigger;
        Actions = actions ?? new List<CanonicalApplyAction>();
        Conflicts = conflicts ?? new List<CanonicalConflictRecord>();
        Tombstones = tombstones ?? new List<CanonicalTombstone>();
        Diagnostics = diagnostics ?? new List<CanonicalApplyDiagnostic>();
        ConflictDiagnostics = MakeConflictDiagnostics(Conflicts);
    }

    public CanonicalApplyPlan Deduplicated()
    {
        var seenActions = new HashSet<string>();
        var seenConflicts = new HashSet<string>();
        var seenTombstones = new HashSet<string>();
        var seenDiagnostics = new HashSet<string>();
        return new CanonicalApplyPlan(
            trigger: Trigger,
            actions: Actions.Where(a => seenActions.Add(a.ActionID)).ToList(),
            conflicts: Conflicts.Where(c => seenConflicts.Add(c.ConflictID)).ToList(),
            tombstones: Tombstones.Where(t => seenTombstones.Add(t.TombstoneID)).ToList(),
            diagnostics: Diagnostics.Where(d => seenDiagnostics.Add(d.Id)).ToList()
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalApplyPlan other && Equals(other);
    public bool Equals(CanonicalApplyPlan? other) =>
        other is not null &&
        Trigger == other.Trigger &&
        Actions.SequenceEqual(other.Actions) &&
        Conflicts.SequenceEqual(other.Conflicts) &&
        Tombstones.SequenceEqual(other.Tombstones) &&
        Diagnostics.SequenceEqual(other.Diagnostics);
    public override int GetHashCode() => HashCode.Combine(Trigger, Actions.Count, Conflicts.Count, Tombstones.Count);
    public static bool operator ==(CanonicalApplyPlan left, CanonicalApplyPlan right) => left.Equals(right);
    public static bool operator !=(CanonicalApplyPlan left, CanonicalApplyPlan right) => !left.Equals(right);

    private static CanonicalConflictDiagnostics MakeConflictDiagnostics(List<CanonicalConflictRecord> conflicts)
    {
        return new CanonicalConflictDiagnostics(
            total: conflicts.Count,
            metadata: conflicts.Count(c => c.Kind == CanonicalConflictKind.recordingMetadataConcurrentEdit),
            audio: conflicts.Count(c => c.Kind == CanonicalConflictKind.recordingAudioContentMismatch),
            generatedArtifact: conflicts.Count(c => c.Kind == CanonicalConflictKind.generatedArtifactContentMismatch),
            tombstone: conflicts.Count(c => c.Kind == CanonicalConflictKind.activeVsTombstone)
        );
    }
}

public class CanonicalApplyPlanner
{
    public CanonicalApplyPlan Plan(
        CanonicalManifest local,
        CanonicalManifest peer,
        CanonicalSyncPlan syncPlan,
        CanonicalSyncPlanTrigger trigger,
        CanonicalSyncPlannerLegacyContext? legacyContext = null)
    {
        var localObjects = local.Objects.ToDictionary(o => o.ObjectID);
        var peerObjects = peer.Objects.ToDictionary(o => o.ObjectID);
        var objectIDs = localObjects.Keys.Union(peerObjects.Keys).OrderBy(id => id, StringComparer.Ordinal).ToList();
        var actions = new List<CanonicalApplyAction>();
        var conflicts = new List<CanonicalConflictRecord>();
        var tombstones = new List<CanonicalTombstone>();
        var diagnostics = new List<CanonicalApplyDiagnostic>();
        var tombstoneOverrideObjectIDs = new HashSet<string>();

        foreach (var objectID in objectIDs)
        {
            AppendObjectTombstoneDecision(
                objectID: objectID,
                localObject: localObjects.GetValueOrDefault(objectID),
                peerObject: peerObjects.GetValueOrDefault(objectID),
                actions: actions,
                conflicts: conflicts,
                tombstones: tombstones,
                diagnostics: diagnostics,
                tombstoneOverrideObjectIDs: tombstoneOverrideObjectIDs);
        }

        AppendMetadataActions(
            syncPlan: syncPlan,
            tombstoneOverrideObjectIDs: tombstoneOverrideObjectIDs,
            actions: actions,
            conflicts: conflicts);
        AppendAudioConflictActions(syncPlan: syncPlan, actions: actions, conflicts: conflicts);
        AppendArtifactTombstones(
            localObjects: localObjects,
            peerObjects: peerObjects,
            actions: actions,
            tombstones: tombstones,
            diagnostics: diagnostics);
        AppendGeneratedArtifactActions(
            syncPlan: syncPlan,
            localObjects: localObjects,
            peerObjects: peerObjects,
            tombstoneOverrideObjectIDs: tombstoneOverrideObjectIDs,
            actions: actions,
            conflicts: conflicts,
            diagnostics: diagnostics);

        diagnostics.Add(
            new CanonicalApplyDiagnostic(
                phase: "canonicalApplyPlanBuilt",
                target: new CanonicalApplyTarget(objectID: "summary"),
                detail: $"actions={actions.Count},conflicts={conflicts.Count},tombstones={tombstones.Count},trigger={trigger},legacyFallbackPreserved={legacyContext != null}"
            )
        );

        return new CanonicalApplyPlan(
            trigger: trigger,
            actions: actions,
            conflicts: conflicts,
            tombstones: tombstones,
            diagnostics: diagnostics
        ).Deduplicated();
    }

    private void AppendObjectTombstoneDecision(
        string objectID,
        CanonicalRecordingObject? localObject,
        CanonicalRecordingObject? peerObject,
        List<CanonicalApplyAction> actions,
        List<CanonicalConflictRecord> conflicts,
        List<CanonicalTombstone> tombstones,
        List<CanonicalApplyDiagnostic> diagnostics,
        HashSet<string> tombstoneOverrideObjectIDs)
    {
        var localTombstone = ObjectTombstone(from: localObject, sourceNodeID: localObject?.NodeID, reason: CanonicalDeletionReason.localTombstoneNewer);
        var peerTombstone = ObjectTombstone(from: peerObject, sourceNodeID: peerObject?.NodeID, reason: CanonicalDeletionReason.peerTombstoneNewer);
        if (localTombstone != null)
        {
            tombstones.Add(localTombstone);
        }
        if (peerTombstone != null)
        {
            tombstones.Add(peerTombstone);
        }

        if (localObject != null && peerObject != null)
        {
            var localDeletedAt = DeletionTimestamp(localObject);
            var peerDeletedAt = DeletionTimestamp(peerObject);
            if (localDeletedAt.HasValue && peerDeletedAt.HasValue)
            {
                if (peerDeletedAt.Value.Date > localDeletedAt.Value.Date)
                {
                    AppendObjectTombstoneAction(
                        kind: CanonicalApplyActionKind.objectTombstoneApply,
                        source: CanonicalApplySource.peer,
                        objectID: objectID,
                        tombstone: peerTombstone,
                        reason: CanonicalDeletionReason.peerTombstoneNewer.ToString(),
                        actions: actions);
                    tombstoneOverrideObjectIDs.Add(objectID);
                }
                else if (localDeletedAt.Value.Date > peerDeletedAt.Value.Date)
                {
                    AppendObjectTombstoneAction(
                        kind: CanonicalApplyActionKind.objectTombstoneSend,
                        source: CanonicalApplySource.local,
                        objectID: objectID,
                        tombstone: localTombstone,
                        reason: CanonicalDeletionReason.localTombstoneNewer.ToString(),
                        actions: actions);
                    tombstoneOverrideObjectIDs.Add(objectID);
                }
            }
            else if (!localDeletedAt.HasValue && peerDeletedAt.HasValue)
            {
                if (peerDeletedAt.Value.Date > localObject.Metadata.ModifiedAt.Date)
                {
                    AppendObjectTombstoneAction(
                        kind: CanonicalApplyActionKind.objectTombstoneApply,
                        source: CanonicalApplySource.peer,
                        objectID: objectID,
                        tombstone: peerTombstone,
                        reason: CanonicalDeletionReason.peerTombstoneNewer.ToString(),
                        actions: actions);
                    tombstoneOverrideObjectIDs.Add(objectID);
                }
                else
                {
                    AppendActiveVsTombstoneConflict(
                        objectID: objectID,
                        localObject: localObject,
                        peerObject: peerObject,
                        conflicts: conflicts,
                        actions: actions);
                    tombstoneOverrideObjectIDs.Add(objectID);
                }
            }
            else if (localDeletedAt.HasValue && !peerDeletedAt.HasValue)
            {
                if (localDeletedAt.Value.Date > peerObject.Metadata.ModifiedAt.Date)
                {
                    AppendObjectTombstoneAction(
                        kind: CanonicalApplyActionKind.objectTombstoneSend,
                        source: CanonicalApplySource.local,
                        objectID: objectID,
                        tombstone: localTombstone,
                        reason: CanonicalDeletionReason.localTombstoneNewer.ToString(),
                        actions: actions);
                    tombstoneOverrideObjectIDs.Add(objectID);
                }
                else
                {
                    AppendActiveVsTombstoneConflict(
                        objectID: objectID,
                        localObject: localObject,
                        peerObject: peerObject,
                        conflicts: conflicts,
                        actions: actions);
                    tombstoneOverrideObjectIDs.Add(objectID);
                }
            }
        }
        else if (localObject == null && peerObject != null && peerObject.Metadata.IsDeleted)
        {
            AppendObjectTombstoneAction(
                kind: CanonicalApplyActionKind.objectTombstoneApply,
                source: CanonicalApplySource.peer,
                objectID: objectID,
                tombstone: peerTombstone,
                reason: CanonicalDeletionReason.peerTombstoneNewer.ToString(),
                actions: actions);
            tombstoneOverrideObjectIDs.Add(objectID);
        }
        else if (localObject != null && peerObject == null && localObject.Metadata.IsDeleted)
        {
            AppendObjectTombstoneAction(
                kind: CanonicalApplyActionKind.objectTombstoneSend,
                source: CanonicalApplySource.local,
                objectID: objectID,
                tombstone: localTombstone,
                reason: CanonicalDeletionReason.localTombstoneNewer.ToString(),
                actions: actions);
            tombstoneOverrideObjectIDs.Add(objectID);
        }

        if (localObject?.Metadata.IsDeleted == true || peerObject?.Metadata.IsDeleted == true)
        {
            diagnostics.Add(
                new CanonicalApplyDiagnostic(
                    phase: "canonicalTombstoneObserved",
                    target: new CanonicalApplyTarget(objectID: objectID),
                    detail: "softDeleteOnly=true,noPhysicalDelete=true"
                )
            );
        }
    }

    private void AppendMetadataActions(
        CanonicalSyncPlan syncPlan,
        HashSet<string> tombstoneOverrideObjectIDs,
        List<CanonicalApplyAction> actions,
        List<CanonicalConflictRecord> conflicts)
    {
        foreach (var action in syncPlan.UploadRecordingMetadata)
        {
            if (!tombstoneOverrideObjectIDs.Contains(action.ObjectID))
            {
                actions.Add(
                    new CanonicalApplyAction(
                        kind: CanonicalApplyActionKind.recordingMetadataSend,
                        source: CanonicalApplySource.local,
                        target: new CanonicalApplyTarget(objectID: action.ObjectID),
                        bridgeHint: CanonicalApplyBridgeHint.legacyMetadataManifestSend,
                        preconditions: MetadataPreconditions(action),
                        reason: action.Reason.ToString()
                    )
                );
            }
        }
        foreach (var action in syncPlan.DownloadRecordingMetadata)
        {
            if (!tombstoneOverrideObjectIDs.Contains(action.ObjectID))
            {
                actions.Add(
                    new CanonicalApplyAction(
                        kind: CanonicalApplyActionKind.recordingMetadataApply,
                        source: CanonicalApplySource.peer,
                        target: new CanonicalApplyTarget(objectID: action.ObjectID),
                        bridgeHint: CanonicalApplyBridgeHint.legacyMetadataManifestApply,
                        preconditions: MetadataPreconditions(action),
                        reason: action.Reason.ToString()
                    )
                );
            }
        }
        foreach (var action in syncPlan.ConflictRecordingMetadata)
        {
            if (!tombstoneOverrideObjectIDs.Contains(action.ObjectID))
            {
                var conflict = new CanonicalConflictRecord(
                    kind: CanonicalConflictKind.recordingMetadataConcurrentEdit,
                    target: new CanonicalApplyTarget(objectID: action.ObjectID),
                    resolutionPolicy: CanonicalConflictResolutionPolicy.manualReview,
                    localHash: action.LocalMetadataHash,
                    peerHash: action.PeerMetadataHash,
                    localModifiedAt: action.LocalModifiedAt,
                    peerModifiedAt: action.PeerModifiedAt,
                    detail: action.Reason.ToString()
                );
                conflicts.Add(conflict);
                actions.Add(ConflictAction(conflict));
            }
        }
    }

    private void AppendAudioConflictActions(
        CanonicalSyncPlan syncPlan,
        List<CanonicalApplyAction> actions,
        List<CanonicalConflictRecord> conflicts)
    {
        foreach (var action in syncPlan.ConflictAudioArtifact)
        {
            var conflict = new CanonicalConflictRecord(
                kind: CanonicalConflictKind.recordingAudioContentMismatch,
                target: new CanonicalApplyTarget(objectID: action.ObjectID, artifactID: action.ArtifactID, artifactKind: CanonicalArtifact.Kind.audio),
                resolutionPolicy: CanonicalConflictResolutionPolicy.keepBothNoOverwrite,
                localHash: action.LocalHash,
                peerHash: action.PeerHash,
                detail: action.Reason.ToString()
            );
            conflicts.Add(conflict);
            actions.Add(ConflictAction(conflict));
        }
    }

    private void AppendArtifactTombstones(
        Dictionary<string, CanonicalRecordingObject> localObjects,
        Dictionary<string, CanonicalRecordingObject> peerObjects,
        List<CanonicalApplyAction> actions,
        List<CanonicalTombstone> tombstones,
        List<CanonicalApplyDiagnostic> diagnostics)
    {
        var objectIDs = localObjects.Keys.Union(peerObjects.Keys).OrderBy(id => id, StringComparer.Ordinal).ToList();
        foreach (var objectID in objectIDs)
        {
            var localArtifacts = localObjects.GetValueOrDefault(objectID)?.Artifacts ?? Array.Empty<CanonicalArtifact>();
            var peerArtifacts = peerObjects.GetValueOrDefault(objectID)?.Artifacts ?? Array.Empty<CanonicalArtifact>();
            var artifacts = localArtifacts.Concat(peerArtifacts).ToList();
            foreach (var artifact in artifacts)
            {
                if (artifact.Tombstone == true && CanonicalProjectionContract.GeneratedArtifactKinds.Contains(artifact.ArtifactKind))
                {
                    var tombstone = new CanonicalTombstone(
                        target: new CanonicalApplyTarget(
                            objectID: objectID,
                            artifactID: artifact.ArtifactID,
                            artifactKind: artifact.ArtifactKind
                        ),
                        state: CanonicalTombstoneState.tombstoned,
                        reason: CanonicalDeletionReason.artifactTombstonePresent,
                        deletedAt: artifact.ModifiedAt,
                        sourceNodeID: artifact.ProducedByNodeID,
                        policies: new List<CanonicalTombstonePolicy>
                        {
                            CanonicalTombstonePolicy.softDeleteOnly,
                            CanonicalTombstonePolicy.antiResurrection,
                            CanonicalTombstonePolicy.noPhysicalDelete,
                            CanonicalTombstonePolicy.noPermanentDelete,
                            CanonicalTombstonePolicy.noGarbageCollection
                        }
                    );
                    tombstones.Add(tombstone);
                    actions.Add(
                        new CanonicalApplyAction(
                            kind: CanonicalApplyActionKind.artifactTombstoneApply,
                            source: CanonicalApplySource.planner,
                            target: tombstone.Target,
                            bridgeHint: CanonicalApplyBridgeHint.noPhysicalDelete,
                            preconditions: new List<CanonicalApplyPrecondition>
                            {
                                new CanonicalApplyPrecondition(
                                    kind: CanonicalApplyPrecondition.Kind.noPhysicalDelete,
                                    value: "true")
                            },
                            result: CanonicalApplyResult.deferredUnsupported,
                            failureReason: CanonicalApplyFailureReason.noPhysicalDeletePolicy,
                            tombstoneID: tombstone.TombstoneID,
                            reason: CanonicalDeletionReason.artifactTombstonePresent.ToString()
                        )
                    );
                    diagnostics.Add(
                        new CanonicalApplyDiagnostic(
                            phase: "canonicalArtifactTombstoneObserved",
                            target: tombstone.Target,
                            detail: "noPhysicalDelete=true"
                        )
                    );
                }
            }
        }
    }

    private void AppendGeneratedArtifactActions(
        CanonicalSyncPlan syncPlan,
        Dictionary<string, CanonicalRecordingObject> localObjects,
        Dictionary<string, CanonicalRecordingObject> peerObjects,
        HashSet<string> tombstoneOverrideObjectIDs,
        List<CanonicalApplyAction> actions,
        List<CanonicalConflictRecord> conflicts,
        List<CanonicalApplyDiagnostic> diagnostics)
    {
        foreach (var action in syncPlan.DownloadGeneratedArtifact)
        {
            if (ObjectIsTombstoned(localObjects.GetValueOrDefault(action.ObjectID)) ||
                ObjectIsTombstoned(peerObjects.GetValueOrDefault(action.ObjectID)) ||
                tombstoneOverrideObjectIDs.Contains(action.ObjectID))
            {
                actions.Add(
                    UnsupportedGeneratedAction(
                        action,
                        failureReason: CanonicalApplyFailureReason.tombstoneBlocksResurrection,
                        reason: "tombstoneBlocksResurrection"
                    )
                );
                diagnostics.Add(
                    new CanonicalApplyDiagnostic(
                        phase: "canonicalGeneratedArtifactDownloadBlockedByTombstone",
                        target: TargetFor(action),
                        detail: "antiResurrection=true"
                    )
                );
            }
            else
            {
                actions.Add(
                    new CanonicalApplyAction(
                        kind: CanonicalApplyActionKind.generatedArtifactDownloadApply,
                        source: CanonicalApplySource.peer,
                        target: TargetFor(action),
                        bridgeHint: CanonicalApplyBridgeHint.legacyArtifactRequestApply,
                        preconditions: ArtifactPreconditions(action),
                        reason: action.Reason.ToString()
                    )
                );
            }
        }
        foreach (var action in syncPlan.NoOpGeneratedArtifact)
        {
            actions.Add(
                new CanonicalApplyAction(
                    kind: CanonicalApplyActionKind.generatedArtifactNoOp,
                    source: CanonicalApplySource.planner,
                    target: TargetFor(action),
                    bridgeHint: CanonicalApplyBridgeHint.noGeneratedArtifactUploadJob,
                    preconditions: ArtifactPreconditions(action),
                    result: CanonicalApplyResult.noOp,
                    reason: action.Reason.ToString()
                )
            );
        }
        foreach (var action in syncPlan.DeferGeneratedArtifact)
        {
            actions.Add(
                UnsupportedGeneratedAction(
                    action,
                    failureReason: CanonicalApplyFailureReason.unsupportedRoute,
                    reason: action.Reason.ToString()
                )
            );
        }
        foreach (var action in syncPlan.ConflictGeneratedArtifact)
        {
            var conflict = new CanonicalConflictRecord(
                kind: CanonicalConflictKind.generatedArtifactContentMismatch,
                target: TargetFor(action),
                resolutionPolicy: CanonicalConflictResolutionPolicy.manualReview,
                localHash: action.LocalHash,
                peerHash: action.PeerHash,
                detail: action.Reason.ToString()
            );
            conflicts.Add(conflict);
            actions.Add(ConflictAction(conflict));
        }
    }

    private void AppendObjectTombstoneAction(
        CanonicalApplyActionKind kind,
        CanonicalApplySource source,
        string objectID,
        CanonicalTombstone? tombstone,
        string reason,
        List<CanonicalApplyAction> actions)
    {
        actions.Add(
            new CanonicalApplyAction(
                kind: kind,
                source: source,
                target: new CanonicalApplyTarget(objectID: objectID),
                bridgeHint: kind == CanonicalApplyActionKind.objectTombstoneApply
                    ? CanonicalApplyBridgeHint.legacyMetadataManifestApply
                    : CanonicalApplyBridgeHint.legacyMetadataManifestSend,
                preconditions: new List<CanonicalApplyPrecondition>
                {
                    new CanonicalApplyPrecondition(
                        kind: CanonicalApplyPrecondition.Kind.tombstoneTimestamp,
                        value: tombstone?.DeletedAt.HasValue == true
                            ? TimestampString(tombstone.DeletedAt.Value)
                            : "missing"),
                    new CanonicalApplyPrecondition(
                        kind: CanonicalApplyPrecondition.Kind.noPhysicalDelete,
                        value: "true")
                },
                tombstoneID: tombstone?.TombstoneID,
                reason: reason
            )
        );
    }

    private void AppendActiveVsTombstoneConflict(
        string objectID,
        CanonicalRecordingObject localObject,
        CanonicalRecordingObject peerObject,
        List<CanonicalConflictRecord> conflicts,
        List<CanonicalApplyAction> actions)
    {
        var conflict = new CanonicalConflictRecord(
            kind: CanonicalConflictKind.activeVsTombstone,
            target: new CanonicalApplyTarget(objectID: objectID),
            resolutionPolicy: CanonicalConflictResolutionPolicy.tombstoneRequiresManualReview,
            localHash: localObject.MetadataHash,
            peerHash: peerObject.MetadataHash,
            localModifiedAt: localObject.Metadata.ModifiedAt,
            peerModifiedAt: peerObject.Metadata.ModifiedAt,
            detail: "activeVsTombstone"
        );
        conflicts.Add(conflict);
        actions.Add(ConflictAction(conflict));
    }

    private CanonicalApplyAction ConflictAction(CanonicalConflictRecord conflict)
    {
        var preconditions = new List<CanonicalApplyPrecondition>();
        if (conflict.LocalHashPrefix != null)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.localHashPrefix,
                value: conflict.LocalHashPrefix));
        if (conflict.PeerHashPrefix != null)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.peerHashPrefix,
                value: conflict.PeerHashPrefix));

        return new CanonicalApplyAction(
            kind: CanonicalApplyActionKind.conflictRecord,
            source: CanonicalApplySource.planner,
            target: conflict.Target,
            bridgeHint: CanonicalApplyBridgeHint.legacyFallbackPreserved,
            preconditions: preconditions,
            result: CanonicalApplyResult.conflictRecorded,
            failureReason: CanonicalApplyFailureReason.conflictDetected,
            conflictID: conflict.ConflictID,
            reason: conflict.Kind.ToString()
        );
    }

    private CanonicalApplyAction UnsupportedGeneratedAction(
        CanonicalArtifactTransferAction action,
        CanonicalApplyFailureReason failureReason,
        string reason)
    {
        return new CanonicalApplyAction(
            kind: CanonicalApplyActionKind.deferredUnsupported,
            source: CanonicalApplySource.planner,
            target: TargetFor(action),
            bridgeHint: failureReason == CanonicalApplyFailureReason.tombstoneBlocksResurrection
                ? CanonicalApplyBridgeHint.noPhysicalDelete
                : CanonicalApplyBridgeHint.unsupportedNoRoute,
            preconditions: ArtifactPreconditions(action),
            result: CanonicalApplyResult.deferredUnsupported,
            failureReason: failureReason,
            reason: reason
        );
    }

    private CanonicalApplyTarget TargetFor(CanonicalArtifactTransferAction action)
    {
        return new CanonicalApplyTarget(
            objectID: action.ObjectID,
            artifactID: action.ArtifactID,
            artifactKind: action.Kind
        );
    }

    private List<CanonicalApplyPrecondition> MetadataPreconditions(CanonicalRecordingMetadataAction action)
    {
        var preconditions = new List<CanonicalApplyPrecondition>();
        if (action.LocalMetadataHash.HasValue)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.localHashPrefix,
                value: HashPrefix(action.LocalMetadataHash.Value)));
        if (action.PeerMetadataHash.HasValue)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.peerHashPrefix,
                value: HashPrefix(action.PeerMetadataHash.Value)));
        if (action.LocalModifiedAt.HasValue)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.localModifiedAt,
                value: TimestampString(action.LocalModifiedAt.Value)));
        if (action.PeerModifiedAt.HasValue)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.peerModifiedAt,
                value: TimestampString(action.PeerModifiedAt.Value)));
        preconditions.Add(new CanonicalApplyPrecondition(
            kind: CanonicalApplyPrecondition.Kind.legacyBridge,
            value: "metadataManifest"));
        return preconditions;
    }

    private List<CanonicalApplyPrecondition> ArtifactPreconditions(CanonicalArtifactTransferAction action)
    {
        var preconditions = new List<CanonicalApplyPrecondition>();
        if (action.LocalHash.HasValue)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.localHashPrefix,
                value: HashPrefix(action.LocalHash.Value)));
        if (action.PeerHash.HasValue)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.peerHashPrefix,
                value: HashPrefix(action.PeerHash.Value)));
        if (action.PeerByteSize.HasValue)
            preconditions.Add(new CanonicalApplyPrecondition(
                kind: CanonicalApplyPrecondition.Kind.peerByteSize,
                value: action.PeerByteSize.Value.ToString()));
        preconditions.Add(new CanonicalApplyPrecondition(
            kind: CanonicalApplyPrecondition.Kind.legacyBridge,
            value: "artifactRequest"));
        return preconditions;
    }

    private CanonicalTombstone? ObjectTombstone(
        CanonicalRecordingObject? from,
        string? sourceNodeID,
        CanonicalDeletionReason reason)
    {
        if (from == null || !from.Metadata.IsDeleted)
            return null;
        return new CanonicalTombstone(
            target: new CanonicalApplyTarget(objectID: from.ObjectID),
            state: CanonicalTombstoneState.tombstoned,
            reason: reason,
            deletedAt: DeletionTimestamp(from),
            sourceNodeID: sourceNodeID
        );
    }

    private bool ObjectIsTombstoned(CanonicalRecordingObject? obj)
    {
        return obj?.Metadata.IsDeleted == true || obj?.SyncState == CanonicalSyncState.deleted;
    }

    private CanonicalTimestamp? DeletionTimestamp(CanonicalRecordingObject obj)
    {
        if (!obj.Metadata.IsDeleted && obj.SyncState != CanonicalSyncState.deleted)
            return null;
        return obj.Metadata.DeletedAt.HasValue ? obj.Metadata.DeletedAt.Value : obj.Metadata.ModifiedAt;
    }

    private static string HashPrefix(CanonicalHash hash)
    {
        return hash.Value[..Math.Min(hash.Value.Length, 12)];
    }

    private static string TimestampString(CanonicalTimestamp timestamp)
    {
        var seconds = (timestamp.Date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
        return seconds.ToString("F6", CultureInfo.InvariantCulture);
    }
}
