using System.Globalization;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncPlanTrigger
{
    manual,
    periodic,
    appActivation,
    retryDrainer,
    viewRefresh
}

public static class CanonicalSyncPlanTriggerExtensions
{
    public static bool AllowsAudioUpload(this CanonicalSyncPlanTrigger trigger) =>
        trigger switch
        {
            CanonicalSyncPlanTrigger.manual => true,
            CanonicalSyncPlanTrigger.periodic => true,
            CanonicalSyncPlanTrigger.appActivation => true,
            CanonicalSyncPlanTrigger.retryDrainer => false,
            CanonicalSyncPlanTrigger.viewRefresh => false,
            _ => false
        };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncPlanReason
{
    canonicalPlanUsed,
    canonicalPlanFallback,
    incompatibleSchema,
    invalidManifestHash,
    missingRecordingMetadataCapability,
    missingAudioArtifactCapability,
    canonicalMetadataHashConverged,
    canonicalCreatedAtIgnoredForMetadataHash,
    canonicalModifiedAtIgnoredProcessingState,
    canonicalBusinessModifiedAtUsed,
    peerMissingMetadata,
    localMissingMetadata,
    metadataHashEqual,
    localMetadataNewer,
    peerMetadataNewer,
    metadataTieConflict,
    legacyWouldUploadMetadataButCanonicalNoOp,
    legacyMetadataHashMismatchButCanonicalHashMatch,
    localAudioUnavailable,
    peerObjectAbsent,
    peerAudioMissing,
    peerAudioMetadataOnly,
    peerStudyItemOnlyWithoutReceiveRecord,
    peerAudioUnknownDeferred,
    peerAudioSameHashSameSize,
    peerAudioHashConflict,
    peerAudioSizeConflict,
    viewRefreshSuppressed,
    retryDrainerSuppressedNewJob,
    canonicalGeneratedArtifactDownload,
    canonicalGeneratedArtifactPeerSameNoOp,
    canonicalGeneratedArtifactPeerUnknownDeferred,
    canonicalGeneratedArtifactConflict,
    canonicalGeneratedArtifactUnsupportedUpload,
    canonicalGeneratedArtifactAuthoritativePeerNewer,
    canonicalGeneratedArtifactLocalProducerNoRoute,
    legacyWouldDownloadArtifactButCanonicalNoOp,
    legacyArtifactMismatchButCanonicalResolved
}

public enum CanonicalSyncPlanError
{
    incompatibleSchema,
    invalidManifestHash,
    missingCapability
}

public class CanonicalSyncPlanException : Exception
{
    public CanonicalSyncPlanError ErrorKind { get; }
    public int? LocalSchema { get; }
    public int? PeerSchema { get; }
    public string? Side { get; }
    public CanonicalCapability? Capability { get; }

    public CanonicalSyncPlanException(CanonicalSyncPlanError errorKind, string message,
        int? localSchema = null, int? peerSchema = null,
        string? side = null, CanonicalCapability? capability = null)
        : base(message)
    {
        ErrorKind = errorKind;
        LocalSchema = localSchema;
        PeerSchema = peerSchema;
        Side = side;
        Capability = capability;
    }

    public static CanonicalSyncPlanException IncompatibleSchema(int local, int peer) =>
        new(CanonicalSyncPlanError.incompatibleSchema,
            $"Incompatible schema: local={local}, peer={peer}",
            localSchema: local, peerSchema: peer);

    public static CanonicalSyncPlanException InvalidManifestHash(string side) =>
        new(CanonicalSyncPlanError.invalidManifestHash,
            $"Invalid manifest hash on {side} side",
            side: side);

    public static CanonicalSyncPlanException MissingCapability(string side, CanonicalCapability capability) =>
        new(CanonicalSyncPlanError.missingCapability,
            $"Missing capability {capability} on {side} side",
            side: side, capability: capability);
}

public sealed class CanonicalRecordingMetadataAction : IEquatable<CanonicalRecordingMetadataAction>
{
    public string Id => $"{ObjectID}:{Reason}";

    public string ObjectID { get; }
    public CanonicalSyncPlanReason Reason { get; }
    public CanonicalHash? LocalMetadataHash { get; }
    public CanonicalHash? PeerMetadataHash { get; }
    public CanonicalTimestamp? LocalModifiedAt { get; }
    public CanonicalTimestamp? PeerModifiedAt { get; }

    public CanonicalRecordingMetadataAction(
        string objectID,
        CanonicalSyncPlanReason reason,
        CanonicalHash? localMetadataHash = null,
        CanonicalHash? peerMetadataHash = null,
        CanonicalTimestamp? localModifiedAt = null,
        CanonicalTimestamp? peerModifiedAt = null)
    {
        ObjectID = objectID;
        Reason = reason;
        LocalMetadataHash = localMetadataHash;
        PeerMetadataHash = peerMetadataHash;
        LocalModifiedAt = localModifiedAt;
        PeerModifiedAt = peerModifiedAt;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataAction other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataAction? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalRecordingMetadataAction left, CanonicalRecordingMetadataAction right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataAction left, CanonicalRecordingMetadataAction right) => !left.Equals(right);
}

public sealed class CanonicalArtifactTransferAction : IEquatable<CanonicalArtifactTransferAction>
{
    public string Id => $"{ObjectID}:{ArtifactID ?? "audio"}:{Reason}";

    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? Kind { get; }
    public string? LogicalPathToken { get; }
    public CanonicalSyncPlanReason Reason { get; }
    public CanonicalHash? LocalHash { get; }
    public CanonicalHash? PeerHash { get; }
    public long? LocalByteSize { get; }
    public long? PeerByteSize { get; }

    public CanonicalArtifactTransferAction(
        string objectID,
        string? artifactID,
        CanonicalArtifact.Kind? kind,
        string? logicalPathToken,
        CanonicalSyncPlanReason reason,
        CanonicalHash? localHash = null,
        CanonicalHash? peerHash = null,
        long? localByteSize = null,
        long? peerByteSize = null)
    {
        ObjectID = objectID;
        ArtifactID = artifactID;
        Kind = kind;
        LogicalPathToken = logicalPathToken;
        Reason = reason;
        LocalHash = localHash;
        PeerHash = peerHash;
        LocalByteSize = localByteSize;
        PeerByteSize = peerByteSize;
    }

    public override bool Equals(object? obj) => obj is CanonicalArtifactTransferAction other && Equals(other);
    public bool Equals(CanonicalArtifactTransferAction? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalArtifactTransferAction left, CanonicalArtifactTransferAction right) => left.Equals(right);
    public static bool operator !=(CanonicalArtifactTransferAction left, CanonicalArtifactTransferAction right) => !left.Equals(right);
}

public sealed class CanonicalSyncPlanBridgeDiagnostics : IEquatable<CanonicalSyncPlanBridgeDiagnostics>
{
    public string Id => string.Join("|", Phase, Reason.ToString(), ObjectID ?? "", ArtifactID ?? "");

    public string Phase { get; }
    public CanonicalSyncPlanReason Reason { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public string? Detail { get; }

    public CanonicalSyncPlanBridgeDiagnostics(
        string phase,
        CanonicalSyncPlanReason reason,
        string? objectID = null,
        string? artifactID = null,
        string? detail = null)
    {
        Phase = phase;
        Reason = reason;
        ObjectID = objectID;
        ArtifactID = artifactID;
        Detail = detail;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncPlanBridgeDiagnostics other && Equals(other);
    public bool Equals(CanonicalSyncPlanBridgeDiagnostics? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalSyncPlanBridgeDiagnostics left, CanonicalSyncPlanBridgeDiagnostics right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncPlanBridgeDiagnostics left, CanonicalSyncPlanBridgeDiagnostics right) => !left.Equals(right);
}

public sealed class CanonicalShadowLegacyObjectFact : IEquatable<CanonicalShadowLegacyObjectFact>
{
    public string ObjectID { get; }
    public bool HasStudyItem { get; }
    public bool HasReceiveRecord { get; }

    public CanonicalShadowLegacyObjectFact(string objectID, bool hasStudyItem = false, bool hasReceiveRecord = false)
    {
        ObjectID = objectID.Trim().NilIfEmpty() ?? "unknown";
        HasStudyItem = hasStudyItem;
        HasReceiveRecord = hasReceiveRecord;
    }

    public static List<CanonicalShadowLegacyObjectFact> Merged(List<CanonicalShadowLegacyObjectFact> facts)
    {
        return facts
            .GroupBy(f => f.ObjectID)
            .Select(g => new CanonicalShadowLegacyObjectFact(
                objectID: g.Key,
                hasStudyItem: g.Any(f => f.HasStudyItem),
                hasReceiveRecord: g.Any(f => f.HasReceiveRecord)))
            .OrderBy(f => f.ObjectID, StringComparer.Ordinal)
            .ToList();
    }

    public override bool Equals(object? obj) => obj is CanonicalShadowLegacyObjectFact other && Equals(other);
    public bool Equals(CanonicalShadowLegacyObjectFact? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        HasStudyItem == other.HasStudyItem &&
        HasReceiveRecord == other.HasReceiveRecord;
    public override int GetHashCode() => HashCode.Combine(ObjectID, HasStudyItem, HasReceiveRecord);
    public static bool operator ==(CanonicalShadowLegacyObjectFact left, CanonicalShadowLegacyObjectFact right) => left.Equals(right);
    public static bool operator !=(CanonicalShadowLegacyObjectFact left, CanonicalShadowLegacyObjectFact right) => !left.Equals(right);
}

public sealed class CanonicalSyncPlannerLegacyContext : IEquatable<CanonicalSyncPlannerLegacyContext>
{
    public List<string> LegacyUploadMetadataObjectIDs { get; }
    public List<string> LegacyDownloadMetadataObjectIDs { get; }
    public List<string> LegacyUploadAudioObjectIDs { get; }
    public List<string> LegacyDownloadGeneratedArtifactKeys { get; }
    public List<string> LegacyConflictGeneratedArtifactKeys { get; }
    public List<CanonicalShadowLegacyObjectFact> PeerObjectFacts { get; }

    public CanonicalSyncPlannerLegacyContext(
        List<string>? legacyUploadMetadataObjectIDs = null,
        List<string>? legacyDownloadMetadataObjectIDs = null,
        List<string>? legacyUploadAudioObjectIDs = null,
        List<string>? legacyDownloadGeneratedArtifactKeys = null,
        List<string>? legacyConflictGeneratedArtifactKeys = null,
        List<CanonicalShadowLegacyObjectFact>? peerObjectFacts = null)
    {
        LegacyUploadMetadataObjectIDs = NormalizedIDs(legacyUploadMetadataObjectIDs);
        LegacyDownloadMetadataObjectIDs = NormalizedIDs(legacyDownloadMetadataObjectIDs);
        LegacyUploadAudioObjectIDs = NormalizedIDs(legacyUploadAudioObjectIDs);
        LegacyDownloadGeneratedArtifactKeys = NormalizedIDs(legacyDownloadGeneratedArtifactKeys);
        LegacyConflictGeneratedArtifactKeys = NormalizedIDs(legacyConflictGeneratedArtifactKeys);
        PeerObjectFacts = CanonicalShadowLegacyObjectFact.Merged(
            peerObjectFacts ?? new List<CanonicalShadowLegacyObjectFact>());
    }

    public CanonicalShadowLegacyObjectFact? PeerFactFor(string objectID)
    {
        return PeerObjectFacts.FirstOrDefault(f => f.ObjectID == objectID);
    }

    public bool LegacyWouldMoveMetadataFor(string objectID)
    {
        return LegacyUploadMetadataObjectIDs.Contains(objectID) ||
               LegacyDownloadMetadataObjectIDs.Contains(objectID);
    }

    public bool LegacyWouldDownloadGeneratedArtifact(string objectID, CanonicalArtifact.Kind kind)
    {
        return LegacyDownloadGeneratedArtifactKeys.Contains(
            CanonicalProjectionContract.ArtifactKey(objectID, kind));
    }

    public bool LegacyHadGeneratedArtifactConflict(string objectID, CanonicalArtifact.Kind kind)
    {
        return LegacyConflictGeneratedArtifactKeys.Contains(
            CanonicalProjectionContract.ArtifactKey(objectID, kind));
    }

    private static List<string> NormalizedIDs(List<string>? ids)
    {
        return new HashSet<string>(
            (ids ?? new List<string>())
                .Select(id => id.Trim().NilIfEmpty())
                .Where(id => id != null)
                .Cast<string>()
        ).OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncPlannerLegacyContext other && Equals(other);
    public bool Equals(CanonicalSyncPlannerLegacyContext? other) =>
        other is not null &&
        LegacyUploadMetadataObjectIDs.SequenceEqual(other.LegacyUploadMetadataObjectIDs) &&
        LegacyDownloadMetadataObjectIDs.SequenceEqual(other.LegacyDownloadMetadataObjectIDs) &&
        LegacyUploadAudioObjectIDs.SequenceEqual(other.LegacyUploadAudioObjectIDs) &&
        LegacyDownloadGeneratedArtifactKeys.SequenceEqual(other.LegacyDownloadGeneratedArtifactKeys) &&
        LegacyConflictGeneratedArtifactKeys.SequenceEqual(other.LegacyConflictGeneratedArtifactKeys) &&
        PeerObjectFacts.SequenceEqual(other.PeerObjectFacts);
    public override int GetHashCode() => HashCode.Combine(
        LegacyUploadMetadataObjectIDs.Count, LegacyDownloadMetadataObjectIDs.Count, PeerObjectFacts.Count);
    public static bool operator ==(CanonicalSyncPlannerLegacyContext left, CanonicalSyncPlannerLegacyContext right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncPlannerLegacyContext left, CanonicalSyncPlannerLegacyContext right) => !left.Equals(right);
}

public sealed class CanonicalSyncPlan : IEquatable<CanonicalSyncPlan>
{
    public List<CanonicalRecordingMetadataAction> UploadRecordingMetadata { get; set; } = new();
    public List<CanonicalRecordingMetadataAction> DownloadRecordingMetadata { get; set; } = new();
    public List<CanonicalRecordingMetadataAction> NoOpRecordingMetadata { get; set; } = new();
    public List<CanonicalRecordingMetadataAction> ConflictRecordingMetadata { get; set; } = new();
    public List<CanonicalArtifactTransferAction> UploadAudioArtifact { get; set; } = new();
    public List<CanonicalArtifactTransferAction> DeferAudioArtifact { get; set; } = new();
    public List<CanonicalArtifactTransferAction> NoOpAudioArtifact { get; set; } = new();
    public List<CanonicalArtifactTransferAction> ConflictAudioArtifact { get; set; } = new();
    public List<CanonicalArtifactTransferAction> DownloadGeneratedArtifact { get; set; } = new();
    public List<CanonicalArtifactTransferAction> DeferGeneratedArtifact { get; set; } = new();
    public List<CanonicalArtifactTransferAction> NoOpGeneratedArtifact { get; set; } = new();
    public List<CanonicalArtifactTransferAction> ConflictGeneratedArtifact { get; set; } = new();
    public List<CanonicalSyncPlanBridgeDiagnostics> Diagnostics { get; set; } = new();

    public override bool Equals(object? obj) => obj is CanonicalSyncPlan other && Equals(other);
    public bool Equals(CanonicalSyncPlan? other) =>
        other is not null &&
        UploadRecordingMetadata.SequenceEqual(other.UploadRecordingMetadata) &&
        DownloadRecordingMetadata.SequenceEqual(other.DownloadRecordingMetadata) &&
        NoOpRecordingMetadata.SequenceEqual(other.NoOpRecordingMetadata) &&
        ConflictRecordingMetadata.SequenceEqual(other.ConflictRecordingMetadata) &&
        UploadAudioArtifact.SequenceEqual(other.UploadAudioArtifact) &&
        DeferAudioArtifact.SequenceEqual(other.DeferAudioArtifact) &&
        NoOpAudioArtifact.SequenceEqual(other.NoOpAudioArtifact) &&
        ConflictAudioArtifact.SequenceEqual(other.ConflictAudioArtifact) &&
        DownloadGeneratedArtifact.SequenceEqual(other.DownloadGeneratedArtifact) &&
        DeferGeneratedArtifact.SequenceEqual(other.DeferGeneratedArtifact) &&
        NoOpGeneratedArtifact.SequenceEqual(other.NoOpGeneratedArtifact) &&
        ConflictGeneratedArtifact.SequenceEqual(other.ConflictGeneratedArtifact);
    public override int GetHashCode() => HashCode.Combine(
        UploadRecordingMetadata.Count, DownloadRecordingMetadata.Count,
        UploadAudioArtifact.Count, DownloadGeneratedArtifact.Count);
    public static bool operator ==(CanonicalSyncPlan left, CanonicalSyncPlan right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncPlan left, CanonicalSyncPlan right) => !left.Equals(right);
}

public class CanonicalSyncPlanner
{
    public CanonicalSyncPlan Plan(
        CanonicalManifest local,
        CanonicalManifest peer,
        CanonicalSyncPlanTrigger trigger,
        CanonicalSyncPlannerLegacyContext? legacyContext = null)
    {
        Validate(local: local, peer: peer);

        var localObjects = local.Objects.ToDictionary(o => o.ObjectID);
        var peerObjects = peer.Objects.ToDictionary(o => o.ObjectID);
        var objectIDs = localObjects.Keys.Union(peerObjects.Keys).OrderBy(id => id, StringComparer.Ordinal).ToList();
        var plan = new CanonicalSyncPlan();
        plan.Diagnostics.Add(
            new CanonicalSyncPlanBridgeDiagnostics(
                phase: "canonicalPlanUsed",
                reason: CanonicalSyncPlanReason.canonicalPlanUsed,
                objectID: null,
                artifactID: null,
                detail: $"objects={objectIDs.Count}"
            )
        );

        foreach (var objectID in objectIDs)
        {
            var localObject = localObjects.GetValueOrDefault(objectID);
            var peerObject = peerObjects.GetValueOrDefault(objectID);
            AppendMetadataDecision(
                objectID: objectID,
                localObject: localObject,
                peerObject: peerObject,
                legacyContext: legacyContext,
                plan: plan);
            if (localObject != null)
            {
                AppendAudioDecision(
                    objectID: objectID,
                    localObject: localObject,
                    peerObject: peerObject,
                    trigger: trigger,
                    legacyContext: legacyContext,
                    plan: plan);
            }
            AppendGeneratedArtifactDecisions(
                objectID: objectID,
                localObject: localObject,
                peerObject: peerObject,
                localNode: local.Node,
                peerNode: peer.Node,
                trigger: trigger,
                legacyContext: legacyContext,
                plan: plan);
        }

        return plan;
    }

    private static void Validate(CanonicalManifest local, CanonicalManifest peer)
    {
        if (local.SchemaVersion != CanonicalManifest.CurrentSchemaVersion ||
            peer.SchemaVersion != CanonicalManifest.CurrentSchemaVersion)
            throw CanonicalSyncPlanException.IncompatibleSchema(local.SchemaVersion, peer.SchemaVersion);
        if (!local.HasValidManifestHash)
            throw CanonicalSyncPlanException.InvalidManifestHash("local");
        if (!peer.HasValidManifestHash)
            throw CanonicalSyncPlanException.InvalidManifestHash("peer");
        if (!local.Node.Capabilities.Contains(CanonicalCapability.recordingMetadata))
            throw CanonicalSyncPlanException.MissingCapability("local", CanonicalCapability.recordingMetadata);
        if (!peer.Node.Capabilities.Contains(CanonicalCapability.recordingMetadata))
            throw CanonicalSyncPlanException.MissingCapability("peer", CanonicalCapability.recordingMetadata);
        if (!local.Node.Capabilities.Contains(CanonicalCapability.audioArtifact))
            throw CanonicalSyncPlanException.MissingCapability("local", CanonicalCapability.audioArtifact);
        if (!peer.Node.Capabilities.Contains(CanonicalCapability.audioArtifact))
            throw CanonicalSyncPlanException.MissingCapability("peer", CanonicalCapability.audioArtifact);
    }

    private void AppendMetadataDecision(
        string objectID,
        CanonicalRecordingObject? localObject,
        CanonicalRecordingObject? peerObject,
        CanonicalSyncPlannerLegacyContext? legacyContext,
        CanonicalSyncPlan plan)
    {
        if (localObject != null && peerObject != null)
        {
            if (SameHash(localObject.MetadataHash, peerObject.MetadataHash))
            {
                var action = MetadataAction(objectID: objectID, reason: CanonicalSyncPlanReason.metadataHashEqual,
                    localObject: localObject, peerObject: peerObject);
                plan.NoOpRecordingMetadata.Add(action);
                plan.Diagnostics.Add(
                    new CanonicalSyncPlanBridgeDiagnostics(
                        phase: "canonicalMetadataHashConverged",
                        reason: CanonicalSyncPlanReason.canonicalMetadataHashConverged,
                        objectID: objectID,
                        artifactID: null,
                        detail: $"canonicalMetadataHash={HashPrefix(localObject.MetadataHash)}"
                    )
                );
                if (localObject.Metadata.CreatedAt.Date != peerObject.Metadata.CreatedAt.Date)
                {
                    plan.Diagnostics.Add(
                        new CanonicalSyncPlanBridgeDiagnostics(
                            phase: "canonicalCreatedAtIgnoredForMetadataHash",
                            reason: CanonicalSyncPlanReason.canonicalCreatedAtIgnoredForMetadataHash,
                            objectID: objectID,
                            artifactID: null,
                            detail: $"canonicalMetadataHash={HashPrefix(localObject.MetadataHash)}"
                        )
                    );
                }
                if (!localObject.ProcessingState.Equals(peerObject.ProcessingState))
                {
                    plan.Diagnostics.Add(
                        new CanonicalSyncPlanBridgeDiagnostics(
                            phase: "canonicalModifiedAtIgnoredProcessingState",
                            reason: CanonicalSyncPlanReason.canonicalModifiedAtIgnoredProcessingState,
                            objectID: objectID,
                            artifactID: null,
                            detail: $"canonicalMetadataHash={HashPrefix(localObject.MetadataHash)}"
                        )
                    );
                }
                if (legacyContext?.LegacyWouldMoveMetadataFor(objectID) == true)
                {
                    plan.Diagnostics.Add(
                        new CanonicalSyncPlanBridgeDiagnostics(
                            phase: "legacyWouldUploadMetadataButCanonicalNoOp",
                            reason: CanonicalSyncPlanReason.legacyWouldUploadMetadataButCanonicalNoOp,
                            objectID: objectID,
                            artifactID: null,
                            detail: $"canonicalMetadataHash={HashPrefix(localObject.MetadataHash)}"
                        )
                    );
                    plan.Diagnostics.Add(
                        new CanonicalSyncPlanBridgeDiagnostics(
                            phase: "legacyMetadataHashMismatchButCanonicalHashMatch",
                            reason: CanonicalSyncPlanReason.legacyMetadataHashMismatchButCanonicalHashMatch,
                            objectID: objectID,
                            artifactID: null,
                            detail: $"canonicalMetadataHash={HashPrefix(localObject.MetadataHash)}"
                        )
                    );
                }
            }
            else if (localObject.Metadata.ModifiedAt > peerObject.Metadata.ModifiedAt)
            {
                plan.UploadRecordingMetadata.Add(
                    MetadataAction(objectID: objectID, reason: CanonicalSyncPlanReason.localMetadataNewer,
                        localObject: localObject, peerObject: peerObject));
                AppendBusinessModifiedAtDiagnostic(
                    objectID: objectID,
                    direction: "upload",
                    localObject: localObject,
                    peerObject: peerObject,
                    plan: plan);
            }
            else if (peerObject.Metadata.ModifiedAt > localObject.Metadata.ModifiedAt)
            {
                plan.DownloadRecordingMetadata.Add(
                    MetadataAction(objectID: objectID, reason: CanonicalSyncPlanReason.peerMetadataNewer,
                        localObject: localObject, peerObject: peerObject));
                AppendBusinessModifiedAtDiagnostic(
                    objectID: objectID,
                    direction: "download",
                    localObject: localObject,
                    peerObject: peerObject,
                    plan: plan);
            }
            else
            {
                plan.ConflictRecordingMetadata.Add(
                    MetadataAction(objectID: objectID, reason: CanonicalSyncPlanReason.metadataTieConflict,
                        localObject: localObject, peerObject: peerObject));
            }
        }
        else if (localObject != null && peerObject == null)
        {
            plan.UploadRecordingMetadata.Add(
                MetadataAction(objectID: objectID, reason: CanonicalSyncPlanReason.peerMissingMetadata,
                    localObject: localObject, peerObject: null));
        }
        else if (localObject == null && peerObject != null)
        {
            plan.DownloadRecordingMetadata.Add(
                MetadataAction(objectID: objectID, reason: CanonicalSyncPlanReason.localMissingMetadata,
                    localObject: null, peerObject: peerObject));
        }
    }

    private void AppendAudioDecision(
        string objectID,
        CanonicalRecordingObject localObject,
        CanonicalRecordingObject? peerObject,
        CanonicalSyncPlanTrigger trigger,
        CanonicalSyncPlannerLegacyContext? legacyContext,
        CanonicalSyncPlan plan)
    {
        var localAudio = localObject.AudioArtifact;
        if (localAudio == null || !localAudio.ProvesCanonicalAudioAvailability)
        {
            plan.DeferAudioArtifact.Add(
                AudioAction(objectID: objectID, localAudio: localObject.AudioArtifact,
                    peerAudio: peerObject?.AudioArtifact, reason: CanonicalSyncPlanReason.localAudioUnavailable));
            return;
        }

        if (trigger == CanonicalSyncPlanTrigger.viewRefresh)
        {
            plan.DeferAudioArtifact.Add(
                AudioAction(objectID: objectID, localAudio: localAudio,
                    peerAudio: peerObject?.AudioArtifact, reason: CanonicalSyncPlanReason.viewRefreshSuppressed));
            return;
        }

        if (trigger == CanonicalSyncPlanTrigger.retryDrainer)
        {
            plan.DeferAudioArtifact.Add(
                AudioAction(objectID: objectID, localAudio: localAudio,
                    peerAudio: peerObject?.AudioArtifact, reason: CanonicalSyncPlanReason.retryDrainerSuppressedNewJob));
            return;
        }

        if (peerObject == null)
        {
            plan.UploadAudioArtifact.Add(
                AudioAction(objectID: objectID, localAudio: localAudio,
                    peerAudio: null, reason: CanonicalSyncPlanReason.peerObjectAbsent));
            return;
        }

        var peerAudio = peerObject.AudioArtifact;
        if (peerAudio == null)
        {
            plan.UploadAudioArtifact.Add(
                AudioAction(objectID: objectID, localAudio: localAudio,
                    peerAudio: null, reason: PeerAudioMissingReason(objectID: objectID, legacyContext: legacyContext)));
            return;
        }

        switch (peerAudio.Availability)
        {
            case CanonicalArtifact.AvailabilityKind.missing:
                plan.UploadAudioArtifact.Add(
                    AudioAction(objectID: objectID, localAudio: localAudio,
                        peerAudio: peerAudio, reason: PeerAudioMissingReason(objectID: objectID, legacyContext: legacyContext)));
                break;
            case CanonicalArtifact.AvailabilityKind.unknown:
            case CanonicalArtifact.AvailabilityKind.availableWithoutHash:
                plan.DeferAudioArtifact.Add(
                    AudioAction(objectID: objectID, localAudio: localAudio,
                        peerAudio: peerAudio, reason: CanonicalSyncPlanReason.peerAudioUnknownDeferred));
                break;
            case CanonicalArtifact.AvailabilityKind.available:
                if (peerAudio.ContentHash == null || peerAudio.ByteSize == null)
                {
                    plan.DeferAudioArtifact.Add(
                        AudioAction(objectID: objectID, localAudio: localAudio,
                            peerAudio: peerAudio, reason: CanonicalSyncPlanReason.peerAudioUnknownDeferred));
                    return;
                }
                if (!SameHash(localAudio.ContentHash, peerAudio.ContentHash))
                {
                    plan.ConflictAudioArtifact.Add(
                        AudioAction(objectID: objectID, localAudio: localAudio,
                            peerAudio: peerAudio, reason: CanonicalSyncPlanReason.peerAudioHashConflict));
                }
                else if (localAudio.ByteSize != peerAudio.ByteSize)
                {
                    plan.ConflictAudioArtifact.Add(
                        AudioAction(objectID: objectID, localAudio: localAudio,
                            peerAudio: peerAudio, reason: CanonicalSyncPlanReason.peerAudioSizeConflict));
                }
                else
                {
                    plan.NoOpAudioArtifact.Add(
                        AudioAction(objectID: objectID, localAudio: localAudio,
                            peerAudio: peerAudio, reason: CanonicalSyncPlanReason.peerAudioSameHashSameSize));
                }
                break;
        }
    }

    private void AppendGeneratedArtifactDecisions(
        string objectID,
        CanonicalRecordingObject? localObject,
        CanonicalRecordingObject? peerObject,
        CanonicalNode localNode,
        CanonicalNode peerNode,
        CanonicalSyncPlanTrigger trigger,
        CanonicalSyncPlannerLegacyContext? legacyContext,
        CanonicalSyncPlan plan)
    {
        var localArtifacts = GeneratedArtifactsByKind(localObject);
        var peerArtifacts = GeneratedArtifactsByKind(peerObject);
        var kinds = localArtifacts.Keys.Union(peerArtifacts.Keys)
            .OrderBy(k => k.ToString(), StringComparer.Ordinal).ToList();
        foreach (var kind in kinds)
        {
            AppendGeneratedArtifactDecision(
                objectID: objectID,
                kind: kind,
                localArtifact: localArtifacts.GetValueOrDefault(kind),
                peerArtifact: peerArtifacts.GetValueOrDefault(kind),
                localNode: localNode,
                peerNode: peerNode,
                trigger: trigger,
                legacyContext: legacyContext,
                plan: plan);
        }
    }

    private void AppendGeneratedArtifactDecision(
        string objectID,
        CanonicalArtifact.Kind kind,
        CanonicalArtifact? localArtifact,
        CanonicalArtifact? peerArtifact,
        CanonicalNode localNode,
        CanonicalNode peerNode,
        CanonicalSyncPlanTrigger trigger,
        CanonicalSyncPlannerLegacyContext? legacyContext,
        CanonicalSyncPlan plan)
    {
        if (localArtifact?.Tombstone == true || peerArtifact?.Tombstone == true)
        {
            AppendGeneratedDefer(
                objectID: objectID,
                kind: kind,
                localArtifact: localArtifact,
                peerArtifact: peerArtifact,
                reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactPeerUnknownDeferred,
                detail: "tombstonePresent",
                plan: plan);
            return;
        }

        var localProven = CanonicalProjectionContract.ProvesGeneratedArtifactAvailability(localArtifact);
        var peerProven = CanonicalProjectionContract.ProvesGeneratedArtifactAvailability(peerArtifact);
        bool peerAuthoritative = peerArtifact != null &&
            CanonicalProjectionContract.IsAuthoritativeProducer(peerArtifact, peerNode);
        bool localAuthoritative = localArtifact != null &&
            CanonicalProjectionContract.IsAuthoritativeProducer(localArtifact, localNode);

        if (peerProven && !localProven)
        {
            if (localArtifact == null || localArtifact.Availability == CanonicalArtifact.AvailabilityKind.missing)
            {
                AppendGeneratedDownloadIfAllowed(
                    objectID: objectID,
                    kind: kind,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    trigger: trigger,
                    reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactDownload,
                    detail: "localMissingPeerAvailable",
                    legacyContext: legacyContext,
                    plan: plan);
            }
            else if (peerAuthoritative)
            {
                AppendGeneratedDownloadIfAllowed(
                    objectID: objectID,
                    kind: kind,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    trigger: trigger,
                    reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactDownload,
                    detail: "localUnknownPeerAuthoritative",
                    legacyContext: legacyContext,
                    plan: plan);
            }
            else
            {
                AppendGeneratedDefer(
                    objectID: objectID,
                    kind: kind,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactPeerUnknownDeferred,
                    detail: "localUnknownPeerNotAuthoritative",
                    plan: plan);
            }
            return;
        }

        if (!peerProven)
        {
            if (peerArtifact != null && peerArtifact.Availability != CanonicalArtifact.AvailabilityKind.missing)
            {
                AppendGeneratedDefer(
                    objectID: objectID,
                    kind: kind,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactPeerUnknownDeferred,
                    detail: "peerUnproven",
                    plan: plan);
            }
            else if (localProven && localAuthoritative)
            {
                AppendGeneratedDefer(
                    objectID: objectID,
                    kind: kind,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactLocalProducerNoRoute,
                    detail: "localAuthoritativeUploadUnsupported",
                    plan: plan);
                AppendGeneratedDiagnostic(
                    objectID: objectID,
                    artifactID: localArtifact?.ArtifactID ?? peerArtifact?.ArtifactID,
                    kind: kind,
                    reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactUnsupportedUpload,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    detail: "generatedArtifactUploadNotPlanned",
                    plan: plan);
            }
            else if (localProven)
            {
                AppendGeneratedDefer(
                    objectID: objectID,
                    kind: kind,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactUnsupportedUpload,
                    detail: "localGeneratedArtifactNotAuthoritative",
                    plan: plan);
            }
            else
            {
                AppendGeneratedDefer(
                    objectID: objectID,
                    kind: kind,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactPeerUnknownDeferred,
                    detail: "bothUnproven",
                    plan: plan);
            }
            return;
        }

        if (localArtifact == null || peerArtifact == null)
        {
            return;
        }

        if (CanonicalProjectionContract.SameContent(localArtifact, peerArtifact))
        {
            var action = GeneratedAction(
                objectID: objectID,
                kind: kind,
                localArtifact: localArtifact,
                peerArtifact: peerArtifact,
                reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactPeerSameNoOp);
            plan.NoOpGeneratedArtifact.Add(action);
            AppendGeneratedDiagnostic(action, detail: "sameHashAndSize", plan: plan);
            if (legacyContext?.LegacyWouldDownloadGeneratedArtifact(objectID, kind) == true)
            {
                AppendGeneratedDiagnostic(action,
                    overrideReason: CanonicalSyncPlanReason.legacyWouldDownloadArtifactButCanonicalNoOp,
                    detail: "canonicalGeneratedArtifactSame",
                    plan: plan);
            }
            return;
        }

        if (peerAuthoritative &&
            peerArtifact.ModifiedAt.HasValue &&
            localArtifact.ModifiedAt.HasValue &&
            peerArtifact.ModifiedAt.Value > localArtifact.ModifiedAt.Value)
        {
            AppendGeneratedDownloadIfAllowed(
                objectID: objectID,
                kind: kind,
                localArtifact: localArtifact,
                peerArtifact: peerArtifact,
                trigger: trigger,
                reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactAuthoritativePeerNewer,
                detail: "peerAuthoritativeNewer",
                legacyContext: legacyContext,
                plan: plan);
            if (legacyContext?.LegacyHadGeneratedArtifactConflict(objectID, kind) == true)
            {
                AppendGeneratedDiagnostic(
                    objectID: objectID,
                    artifactID: peerArtifact.ArtifactID,
                    kind: kind,
                    reason: CanonicalSyncPlanReason.legacyArtifactMismatchButCanonicalResolved,
                    localArtifact: localArtifact,
                    peerArtifact: peerArtifact,
                    detail: "peerAuthoritativeNewer",
                    plan: plan);
            }
            return;
        }

        if (localAuthoritative &&
            peerArtifact.ModifiedAt.HasValue &&
            localArtifact.ModifiedAt.HasValue &&
            localArtifact.ModifiedAt.Value > peerArtifact.ModifiedAt.Value)
        {
            AppendGeneratedDefer(
                objectID: objectID,
                kind: kind,
                localArtifact: localArtifact,
                peerArtifact: peerArtifact,
                reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactLocalProducerNoRoute,
                detail: "localAuthoritativeNewerUploadUnsupported",
                plan: plan);
            return;
        }

        var conflictAction = GeneratedAction(
            objectID: objectID,
            kind: kind,
            localArtifact: localArtifact,
            peerArtifact: peerArtifact,
            reason: CanonicalSyncPlanReason.canonicalGeneratedArtifactConflict);
        plan.ConflictGeneratedArtifact.Add(conflictAction);
        AppendGeneratedDiagnostic(conflictAction, detail: "hashOrSizeMismatch", plan: plan);
    }

    private void AppendGeneratedDownloadIfAllowed(
        string objectID,
        CanonicalArtifact.Kind kind,
        CanonicalArtifact? localArtifact,
        CanonicalArtifact? peerArtifact,
        CanonicalSyncPlanTrigger trigger,
        CanonicalSyncPlanReason reason,
        string detail,
        CanonicalSyncPlannerLegacyContext? legacyContext,
        CanonicalSyncPlan plan)
    {
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh)
        {
            AppendGeneratedDefer(
                objectID: objectID,
                kind: kind,
                localArtifact: localArtifact,
                peerArtifact: peerArtifact,
                reason: CanonicalSyncPlanReason.viewRefreshSuppressed,
                detail: detail,
                plan: plan);
            return;
        }
        if (trigger == CanonicalSyncPlanTrigger.retryDrainer)
        {
            AppendGeneratedDefer(
                objectID: objectID,
                kind: kind,
                localArtifact: localArtifact,
                peerArtifact: peerArtifact,
                reason: CanonicalSyncPlanReason.retryDrainerSuppressedNewJob,
                detail: detail,
                plan: plan);
            return;
        }
        var action = GeneratedAction(
            objectID: objectID,
            kind: kind,
            localArtifact: localArtifact,
            peerArtifact: peerArtifact,
            reason: reason);
        plan.DownloadGeneratedArtifact.Add(action);
        AppendGeneratedDiagnostic(action, detail: detail, plan: plan);
        if (legacyContext?.LegacyHadGeneratedArtifactConflict(objectID, kind) == true)
        {
            AppendGeneratedDiagnostic(action,
                overrideReason: CanonicalSyncPlanReason.legacyArtifactMismatchButCanonicalResolved,
                detail: detail,
                plan: plan);
        }
    }

    private void AppendGeneratedDefer(
        string objectID,
        CanonicalArtifact.Kind kind,
        CanonicalArtifact? localArtifact,
        CanonicalArtifact? peerArtifact,
        CanonicalSyncPlanReason reason,
        string detail,
        CanonicalSyncPlan plan)
    {
        var action = GeneratedAction(
            objectID: objectID,
            kind: kind,
            localArtifact: localArtifact,
            peerArtifact: peerArtifact,
            reason: reason);
        plan.DeferGeneratedArtifact.Add(action);
        AppendGeneratedDiagnostic(action, detail: detail, plan: plan);
    }

    private Dictionary<CanonicalArtifact.Kind, CanonicalArtifact> GeneratedArtifactsByKind(
        CanonicalRecordingObject? obj)
    {
        if (obj == null)
            return new Dictionary<CanonicalArtifact.Kind, CanonicalArtifact>();

        var result = new Dictionary<CanonicalArtifact.Kind, CanonicalArtifact>();
        foreach (var artifact in obj.Artifacts)
        {
            if (!CanonicalProjectionContract.GeneratedArtifactKinds.Contains(artifact.ArtifactKind))
                continue;

            var existing = result.GetValueOrDefault(artifact.ArtifactKind);
            var epoch = new CanonicalTimestamp(DateTime.UnixEpoch);
            var artifactTime = artifact.ModifiedAt ?? epoch;
            var existingTime = existing?.ModifiedAt ?? epoch;
            if (existing == null || artifactTime > existingTime)
            {
                result[artifact.ArtifactKind] = artifact;
            }
        }
        return result;
    }

    private CanonicalSyncPlanReason PeerAudioMissingReason(
        string objectID,
        CanonicalSyncPlannerLegacyContext? legacyContext)
    {
        var fact = legacyContext?.PeerFactFor(objectID);
        if (fact == null)
            return CanonicalSyncPlanReason.peerAudioMissing;
        if (fact.HasStudyItem && !fact.HasReceiveRecord)
            return CanonicalSyncPlanReason.peerStudyItemOnlyWithoutReceiveRecord;
        if (fact.HasReceiveRecord)
            return CanonicalSyncPlanReason.peerAudioMetadataOnly;
        return CanonicalSyncPlanReason.peerAudioMissing;
    }

    private CanonicalRecordingMetadataAction MetadataAction(
        string objectID,
        CanonicalSyncPlanReason reason,
        CanonicalRecordingObject? localObject,
        CanonicalRecordingObject? peerObject)
    {
        return new CanonicalRecordingMetadataAction(
            objectID: objectID,
            reason: reason,
            localMetadataHash: localObject?.MetadataHash,
            peerMetadataHash: peerObject?.MetadataHash,
            localModifiedAt: localObject?.Metadata.ModifiedAt,
            peerModifiedAt: peerObject?.Metadata.ModifiedAt
        );
    }

    private CanonicalArtifactTransferAction AudioAction(
        string objectID,
        CanonicalArtifact? localAudio,
        CanonicalArtifact? peerAudio,
        CanonicalSyncPlanReason reason)
    {
        return new CanonicalArtifactTransferAction(
            objectID: objectID,
            artifactID: localAudio?.ArtifactID ?? peerAudio?.ArtifactID,
            kind: CanonicalArtifact.Kind.audio,
            logicalPathToken: null,
            reason: reason,
            localHash: localAudio?.ContentHash,
            peerHash: peerAudio?.ContentHash,
            localByteSize: localAudio?.ByteSize,
            peerByteSize: peerAudio?.ByteSize
        );
    }

    private CanonicalArtifactTransferAction GeneratedAction(
        string objectID,
        CanonicalArtifact.Kind kind,
        CanonicalArtifact? localArtifact,
        CanonicalArtifact? peerArtifact,
        CanonicalSyncPlanReason reason)
    {
        return new CanonicalArtifactTransferAction(
            objectID: objectID,
            artifactID: peerArtifact?.ArtifactID ?? localArtifact?.ArtifactID
                ?? CanonicalProjectionContract.ArtifactID(objectID, kind),
            kind: kind,
            logicalPathToken: peerArtifact?.LogicalPathToken ?? localArtifact?.LogicalPathToken,
            reason: reason,
            localHash: localArtifact?.ContentHash,
            peerHash: peerArtifact?.ContentHash,
            localByteSize: localArtifact?.ByteSize,
            peerByteSize: peerArtifact?.ByteSize
        );
    }

    private void AppendGeneratedDiagnostic(
        CanonicalArtifactTransferAction action,
        CanonicalSyncPlanReason? overrideReason = null,
        string detail = null!,
        CanonicalSyncPlan plan = null!)
    {
        plan.Diagnostics.Add(
            new CanonicalSyncPlanBridgeDiagnostics(
                phase: (overrideReason ?? action.Reason).ToString(),
                reason: overrideReason ?? action.Reason,
                objectID: action.ObjectID,
                artifactID: action.ArtifactID,
                detail: string.Join(";",
                    $"kind={action.Kind?.ToString() ?? "unknown"}",
                    $"detail={detail}",
                    $"localHash={HashPrefix(action.LocalHash)}",
                    $"peerHash={HashPrefix(action.PeerHash)}",
                    $"localSize={(action.LocalByteSize.HasValue ? action.LocalByteSize.Value.ToString() : "missing")}",
                    $"peerSize={(action.PeerByteSize.HasValue ? action.PeerByteSize.Value.ToString() : "missing")}"
                )
            )
        );
    }

    private void AppendGeneratedDiagnostic(
        string objectID,
        string? artifactID,
        CanonicalArtifact.Kind kind,
        CanonicalSyncPlanReason reason,
        CanonicalArtifact? localArtifact,
        CanonicalArtifact? peerArtifact,
        string detail,
        CanonicalSyncPlan plan)
    {
        var action = GeneratedAction(
            objectID: objectID,
            kind: kind,
            localArtifact: localArtifact,
            peerArtifact: peerArtifact,
            reason: reason);
        var diagnosticAction = new CanonicalArtifactTransferAction(
            objectID: action.ObjectID,
            artifactID: artifactID ?? action.ArtifactID,
            kind: action.Kind,
            logicalPathToken: action.LogicalPathToken,
            reason: action.Reason,
            localHash: action.LocalHash,
            peerHash: action.PeerHash,
            localByteSize: action.LocalByteSize,
            peerByteSize: action.PeerByteSize);
        AppendGeneratedDiagnostic(diagnosticAction, overrideReason: reason, detail: detail, plan: plan);
    }

    private void AppendBusinessModifiedAtDiagnostic(
        string objectID,
        string direction,
        CanonicalRecordingObject localObject,
        CanonicalRecordingObject peerObject,
        CanonicalSyncPlan plan)
    {
        plan.Diagnostics.Add(
            new CanonicalSyncPlanBridgeDiagnostics(
                phase: "canonicalBusinessModifiedAtUsed",
                reason: CanonicalSyncPlanReason.canonicalBusinessModifiedAtUsed,
                objectID: objectID,
                artifactID: null,
                detail: string.Join(";",
                    $"direction={direction}",
                    $"localModifiedAt={TimestampSeconds(localObject.Metadata.ModifiedAt)}",
                    $"peerModifiedAt={TimestampSeconds(peerObject.Metadata.ModifiedAt)}"
                )
            )
        );
    }

    private static bool SameHash(CanonicalHash left, CanonicalHash right) =>
        left.Algorithm == right.Algorithm && left.Value == right.Value;

    private static bool SameHash(CanonicalHash? left, CanonicalHash? right) =>
        left?.Algorithm == right?.Algorithm && left?.Value == right?.Value;

    private static string HashPrefix(CanonicalHash? hash)
    {
        var value = hash?.Value.Trim();
        if (string.IsNullOrEmpty(value))
            return "missing";
        return value[..Math.Min(value.Length, 12)];
    }

    private static string TimestampSeconds(CanonicalTimestamp timestamp)
    {
        var seconds = (timestamp.Date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
        return seconds.ToString("F3", CultureInfo.InvariantCulture);
    }
}
