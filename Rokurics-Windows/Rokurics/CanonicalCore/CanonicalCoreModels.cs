using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── Forward-reference stub types defined in other Swift files ────────────────

public readonly struct CanonicalLibraryObjectID : IEquatable<CanonicalLibraryObjectID>
{
    public string RawValue { get; }

    public CanonicalLibraryObjectID(string rawValue, string fallback = "unknownUnsupported:unknown")
    {
        RawValue = (rawValue?.Trim().NilIfEmpty()) ?? fallback;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryObjectID other && Equals(other);
    public bool Equals(CanonicalLibraryObjectID other) => RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();
    public override string ToString() => RawValue;
    public static bool operator ==(CanonicalLibraryObjectID left, CanonicalLibraryObjectID right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryObjectID left, CanonicalLibraryObjectID right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalObjectKind
{
    recording,
    folder,
    standaloneStudyItem,
    standaloneNote,
    recordingAssociatedStudyItem,
    generatedArtifactEnvelope,
    unknownUnsupported
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalStudyItemKind
{
    recordingBundle,
    standaloneNote,
    externalResource,
    unknown
}

public readonly struct CanonicalParentReference : IEquatable<CanonicalParentReference>
{
    public CanonicalLibraryObjectID ParentID { get; }
    public string Relation { get; }

    public CanonicalParentReference(CanonicalLibraryObjectID parentID, string relation = "parent")
    {
        ParentID = parentID;
        Relation = relation.Trim().NilIfEmpty() ?? "parent";
    }

    public override bool Equals(object? obj) => obj is CanonicalParentReference other && Equals(other);
    public bool Equals(CanonicalParentReference other) => ParentID.Equals(other.ParentID) && Relation == other.Relation;
    public override int GetHashCode() => HashCode.Combine(ParentID, Relation);
    public static bool operator ==(CanonicalParentReference left, CanonicalParentReference right) => left.Equals(right);
    public static bool operator !=(CanonicalParentReference left, CanonicalParentReference right) => !left.Equals(right);
}

public readonly struct CanonicalHierarchyPath : IEquatable<CanonicalHierarchyPath>
{
    public string[] Components { get; }

    public CanonicalHierarchyPath(string[]? components = null)
    {
        Components = (components ?? Array.Empty<string>())
            .Select(v => v.Trim().NilIfEmpty())
            .Where(v => v != null)
            .Cast<string>()
            .ToArray();
    }

    public string StableKey => string.Join("\u001F", Components);

    public override bool Equals(object? obj) => obj is CanonicalHierarchyPath other && Equals(other);
    public bool Equals(CanonicalHierarchyPath other) => Components.SequenceEqual(other.Components);
    public override int GetHashCode() => Components.Aggregate(0, HashCode.Combine);
    public static bool operator ==(CanonicalHierarchyPath left, CanonicalHierarchyPath right) => left.Equals(right);
    public static bool operator !=(CanonicalHierarchyPath left, CanonicalHierarchyPath right) => !left.Equals(right);
}

public sealed class CanonicalFolderMetadata : IEquatable<CanonicalFolderMetadata>
{
    public CanonicalLibraryObjectID FolderID { get; }
    public string Name { get; }
    public CanonicalLibraryObjectID? ParentID { get; }
    public CanonicalHierarchyPath HierarchyPath { get; }
    public string? HierarchyLevel { get; }
    public string? ColorToken { get; }
    public string? OrderingKey { get; }
    public bool IsDeleted { get; }
    public CanonicalTimestamp? DeletedAt { get; }
    public CanonicalTimestamp BusinessModifiedAt { get; }

    public CanonicalFolderMetadata(
        CanonicalLibraryObjectID folderID,
        string name,
        CanonicalLibraryObjectID? parentID = null,
        CanonicalHierarchyPath hierarchyPath = default,
        string? hierarchyLevel = null,
        string? colorToken = null,
        string? orderingKey = null,
        bool isDeleted = false,
        CanonicalTimestamp? deletedAt = null,
        CanonicalTimestamp businessModifiedAt = default)
    {
        FolderID = folderID;
        Name = CanonicalProjectionContract.NormalizeFolderName(name);
        ParentID = parentID;
        HierarchyPath = hierarchyPath;
        HierarchyLevel = hierarchyLevel?.Trim().NilIfEmpty();
        ColorToken = colorToken?.Trim().NilIfEmpty();
        OrderingKey = orderingKey?.Trim().NilIfEmpty();
        IsDeleted = isDeleted;
        DeletedAt = isDeleted ? deletedAt : null;
        BusinessModifiedAt = businessModifiedAt;
    }

    public CanonicalHash MetadataHash =>
        CanonicalHash.Sha256Of(CanonicalProjectionContract.MetadataHashPayloadFor(this));

    public override bool Equals(object? obj) => obj is CanonicalFolderMetadata other && Equals(other);
    public bool Equals(CanonicalFolderMetadata? other) =>
        other is not null &&
        FolderID.Equals(other.FolderID) &&
        Name == other.Name &&
        Nullable.Equals(ParentID, other.ParentID) &&
        HierarchyPath.Equals(other.HierarchyPath) &&
        HierarchyLevel == other.HierarchyLevel &&
        ColorToken == other.ColorToken &&
        OrderingKey == other.OrderingKey &&
        IsDeleted == other.IsDeleted &&
        Nullable.Equals(DeletedAt, other.DeletedAt) &&
        BusinessModifiedAt.Equals(other.BusinessModifiedAt);
    public override int GetHashCode() => HashCode.Combine(
        FolderID, Name, ParentID, HierarchyPath, HierarchyLevel, ColorToken, OrderingKey, IsDeleted, DeletedAt, BusinessModifiedAt);
    public static bool operator ==(CanonicalFolderMetadata left, CanonicalFolderMetadata right) => left.Equals(right);
    public static bool operator !=(CanonicalFolderMetadata left, CanonicalFolderMetadata right) => !left.Equals(right);
}

public sealed class CanonicalStudyItemMetadata : IEquatable<CanonicalStudyItemMetadata>
{
    public CanonicalLibraryObjectID ItemID { get; }
    public CanonicalStudyItemKind ItemKind { get; }
    public string Title { get; }
    public CanonicalHierarchyPath FilingPath { get; }
    public CanonicalLibraryObjectID[] FolderIDs { get; }
    public CanonicalParentReference[] ParentReferences { get; }
    public string[] Tags { get; }
    public string[] LogicalResourceTokens { get; }
    public string? AssociatedRecordingID { get; }
    public bool IsDeleted { get; }
    public CanonicalTimestamp? DeletedAt { get; }
    public CanonicalTimestamp BusinessModifiedAt { get; }

    public CanonicalStudyItemMetadata(
        CanonicalLibraryObjectID itemID,
        CanonicalStudyItemKind itemKind,
        string title,
        CanonicalHierarchyPath filingPath = default,
        CanonicalLibraryObjectID[]? folderIDs = null,
        CanonicalParentReference[]? parentReferences = null,
        string[]? tags = null,
        string[]? logicalResourceTokens = null,
        string? associatedRecordingID = null,
        bool isDeleted = false,
        CanonicalTimestamp? deletedAt = null,
        CanonicalTimestamp businessModifiedAt = default)
    {
        ItemID = itemID;
        ItemKind = itemKind;
        Title = CanonicalProjectionContract.NormalizeStudyItemTitle(title, itemKind);
        FilingPath = CanonicalProjectionContract.NormalizeFilingPath(filingPath);
        FolderIDs = CanonicalProjectionContract.DeduplicateFolderIDs(folderIDs);
        ParentReferences = CanonicalProjectionContract.NormalizeParentReferences(parentReferences);
        Tags = CanonicalProjectionContract.NormalizeTags(tags);
        LogicalResourceTokens = (logicalResourceTokens ?? Array.Empty<string>())
            .Select(CanonicalProjectionContract.SafeLogicalResourceToken)
            .Where(v => v != null)
            .Cast<string>()
            .Distinct()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToArray();
        AssociatedRecordingID = associatedRecordingID?.Trim().NilIfEmpty();
        IsDeleted = isDeleted;
        DeletedAt = isDeleted ? deletedAt : null;
        BusinessModifiedAt = businessModifiedAt;
    }

    public CanonicalHash MetadataHash =>
        CanonicalHash.Sha256Of(CanonicalProjectionContract.MetadataHashPayloadFor(this));

    public override bool Equals(object? obj) => obj is CanonicalStudyItemMetadata other && Equals(other);
    public bool Equals(CanonicalStudyItemMetadata? other) =>
        other is not null &&
        ItemID.Equals(other.ItemID) &&
        ItemKind == other.ItemKind &&
        Title == other.Title &&
        FilingPath.Equals(other.FilingPath) &&
        FolderIDs.SequenceEqual(other.FolderIDs) &&
        ParentReferences.SequenceEqual(other.ParentReferences) &&
        Tags.SequenceEqual(other.Tags) &&
        LogicalResourceTokens.SequenceEqual(other.LogicalResourceTokens) &&
        AssociatedRecordingID == other.AssociatedRecordingID &&
        IsDeleted == other.IsDeleted &&
        Nullable.Equals(DeletedAt, other.DeletedAt) &&
        BusinessModifiedAt.Equals(other.BusinessModifiedAt);
    public override int GetHashCode() => HashCode.Combine(ItemID, ItemKind, Title, FilingPath, IsDeleted, BusinessModifiedAt);
    public static bool operator ==(CanonicalStudyItemMetadata left, CanonicalStudyItemMetadata right) => left.Equals(right);
    public static bool operator !=(CanonicalStudyItemMetadata left, CanonicalStudyItemMetadata right) => !left.Equals(right);
}

public sealed class CanonicalFolderObject : IEquatable<CanonicalFolderObject>
{
    public string Id => FolderID.RawValue;
    public CanonicalLibraryObjectID FolderID { get; }
    public CanonicalFolderMetadata Metadata { get; }
    public CanonicalHash MetadataHash { get; }

    public CanonicalFolderObject(CanonicalFolderMetadata metadata)
    {
        FolderID = metadata.FolderID;
        Metadata = metadata;
        MetadataHash = metadata.MetadataHash;
    }
    
    [JsonConstructor]
    public CanonicalFolderObject(CanonicalLibraryObjectID folderID, CanonicalFolderMetadata metadata, CanonicalHash metadataHash)
    {
        FolderID = folderID;
        Metadata = metadata;
        MetadataHash = metadataHash;
    }

    public override bool Equals(object? obj) => obj is CanonicalFolderObject other && Equals(other);
    public bool Equals(CanonicalFolderObject? other) =>
        other is not null &&
        FolderID.Equals(other.FolderID) &&
        MetadataHash.Equals(other.MetadataHash);
    public override int GetHashCode() => HashCode.Combine(FolderID, MetadataHash);
    public static bool operator ==(CanonicalFolderObject left, CanonicalFolderObject right) => left.Equals(right);
    public static bool operator !=(CanonicalFolderObject left, CanonicalFolderObject right) => !left.Equals(right);
}

public sealed class CanonicalStudyItemObject : IEquatable<CanonicalStudyItemObject>
{
    public string Id => ItemID.RawValue;
    public CanonicalLibraryObjectID ItemID { get; }
    public CanonicalStudyItemMetadata Metadata { get; }
    public CanonicalHash MetadataHash { get; }

    public CanonicalStudyItemObject(CanonicalStudyItemMetadata metadata)
    {
        ItemID = metadata.ItemID;
        Metadata = metadata;
        MetadataHash = metadata.MetadataHash;
    }
    
    [JsonConstructor]
    public CanonicalStudyItemObject(CanonicalLibraryObjectID itemID, CanonicalStudyItemMetadata metadata, CanonicalHash metadataHash)
    {
        ItemID = itemID;
        Metadata = metadata;
        MetadataHash = metadataHash;
    }

    public override bool Equals(object? obj) => obj is CanonicalStudyItemObject other && Equals(other);
    public bool Equals(CanonicalStudyItemObject? other) =>
        other is not null &&
        ItemID.Equals(other.ItemID) &&
        MetadataHash.Equals(other.MetadataHash);
    public override int GetHashCode() => HashCode.Combine(ItemID, MetadataHash);
    public static bool operator ==(CanonicalStudyItemObject left, CanonicalStudyItemObject right) => left.Equals(right);
    public static bool operator !=(CanonicalStudyItemObject left, CanonicalStudyItemObject right) => !left.Equals(right);
}

public sealed class CanonicalStandaloneNoteObject : IEquatable<CanonicalStandaloneNoteObject>
{
    public string Id => NoteID.RawValue;
    public CanonicalLibraryObjectID NoteID { get; }
    public CanonicalStudyItemObject StudyItem { get; }
    public CanonicalHash MetadataHash { get; }

    public CanonicalStandaloneNoteObject(CanonicalStudyItemObject studyItem)
    {
        NoteID = studyItem.ItemID;
        StudyItem = studyItem;
        MetadataHash = studyItem.MetadataHash;
    }
    
    [JsonConstructor]
    public CanonicalStandaloneNoteObject(CanonicalLibraryObjectID noteID, CanonicalStudyItemObject studyItem, CanonicalHash metadataHash)
    {
        NoteID = noteID;
        StudyItem = studyItem;
        MetadataHash = metadataHash;
    }

    public override bool Equals(object? obj) => obj is CanonicalStandaloneNoteObject other && Equals(other);
    public bool Equals(CanonicalStandaloneNoteObject? other) =>
        other is not null &&
        NoteID.Equals(other.NoteID) &&
        MetadataHash.Equals(other.MetadataHash);
    public override int GetHashCode() => HashCode.Combine(NoteID, MetadataHash);
    public static bool operator ==(CanonicalStandaloneNoteObject left, CanonicalStandaloneNoteObject right) => left.Equals(right);
    public static bool operator !=(CanonicalStandaloneNoteObject left, CanonicalStandaloneNoteObject right) => !left.Equals(right);
}

public sealed class CanonicalLibraryObject : IEquatable<CanonicalLibraryObject>
{
    public string Id => ObjectID.RawValue;
    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalObjectKind Kind { get; }
    public CanonicalFolderObject? Folder { get; }
    public CanonicalStudyItemObject? StudyItem { get; }
    public CanonicalStandaloneNoteObject? StandaloneNote { get; }
    public string? UnsupportedReason { get; }

    public CanonicalLibraryObject(
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind kind,
        CanonicalFolderObject? folder = null,
        CanonicalStudyItemObject? studyItem = null,
        CanonicalStandaloneNoteObject? standaloneNote = null,
        string? unsupportedReason = null)
    {
        ObjectID = objectID;
        Kind = kind;
        Folder = folder;
        StudyItem = studyItem;
        StandaloneNote = standaloneNote;
        UnsupportedReason = unsupportedReason?.Trim().NilIfEmpty();
    }

    [JsonConstructor]
    public CanonicalLibraryObject(
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind kind,
        CanonicalFolderObject? folder,
        CanonicalStudyItemObject? studyItem,
        CanonicalStandaloneNoteObject? standaloneNote,
        string? unsupportedReason,
        string id,
        CanonicalHash metadataHash,
        CanonicalTimestamp? businessModifiedAt,
        bool isDeleted,
        CanonicalTimestamp? deletedAt)
        : this(objectID, kind, folder, studyItem, standaloneNote, unsupportedReason)
    {
    }

    public CanonicalHash MetadataHash
    {
        get
        {
            return Kind switch
            {
                CanonicalObjectKind.folder => Folder?.MetadataHash ?? CanonicalHash.Sha256String(ObjectID.RawValue),
                CanonicalObjectKind.standaloneStudyItem or CanonicalObjectKind.recordingAssociatedStudyItem =>
                    StudyItem?.MetadataHash ?? CanonicalHash.Sha256String(ObjectID.RawValue),
                CanonicalObjectKind.standaloneNote =>
                    StandaloneNote?.MetadataHash ?? StudyItem?.MetadataHash ?? CanonicalHash.Sha256String(ObjectID.RawValue),
                CanonicalObjectKind.generatedArtifactEnvelope or CanonicalObjectKind.unknownUnsupported =>
                    CanonicalHash.Sha256Of(new Dictionary<string, string>
                    {
                        ["schema"] = "canonical-library-object-unsupported-v1",
                        ["objectID"] = ObjectID.RawValue,
                        ["kind"] = Kind.ToString(),
                        ["reason"] = UnsupportedReason ?? ""
                    }),
                _ => CanonicalHash.Sha256String(ObjectID.RawValue)
            };
        }
    }

    public CanonicalTimestamp? BusinessModifiedAt =>
        Folder?.Metadata.BusinessModifiedAt ?? StudyItem?.Metadata.BusinessModifiedAt;

    public bool IsDeleted =>
        Folder?.Metadata.IsDeleted ?? StudyItem?.Metadata.IsDeleted ?? false;

    public CanonicalTimestamp? DeletedAt =>
        Folder?.Metadata.DeletedAt ?? StudyItem?.Metadata.DeletedAt;

    public override bool Equals(object? obj) => obj is CanonicalLibraryObject other && Equals(other);
    public bool Equals(CanonicalLibraryObject? other) =>
        other is not null && ObjectID.Equals(other.ObjectID) && Kind == other.Kind;
    public override int GetHashCode() => HashCode.Combine(ObjectID, Kind);
    public static bool operator ==(CanonicalLibraryObject left, CanonicalLibraryObject right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryObject left, CanonicalLibraryObject right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryTombstoneReason
{
    softDelete,
    peerTombstoneNewer,
    localTombstoneNewer,
    antiResurrection
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstonePolicy
{
    softDeleteOnly,
    antiResurrection,
    noPhysicalDelete,
    noPermanentDelete,
    noGarbageCollection
}

public sealed class CanonicalLibraryTombstone : IEquatable<CanonicalLibraryTombstone>
{
    public string Id => TombstoneID;
    public string TombstoneID { get; }
    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalObjectKind ObjectKind { get; }
    public CanonicalTimestamp? DeletedAt { get; }
    public string? SourceNodeID { get; }
    public CanonicalLibraryTombstoneReason Reason { get; }
    public CanonicalTombstonePolicy[] Policies { get; }

    public CanonicalLibraryTombstone(
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        CanonicalTimestamp? deletedAt,
        string? sourceNodeID = null,
        CanonicalLibraryTombstoneReason reason = default,
        CanonicalTombstonePolicy[]? policies = null)
    {
        ObjectID = objectID;
        ObjectKind = objectKind;
        DeletedAt = deletedAt;
        SourceNodeID = sourceNodeID?.Trim().NilIfEmpty();
        Reason = reason;
        Policies = (policies ?? new[] { CanonicalTombstonePolicy.softDeleteOnly, CanonicalTombstonePolicy.antiResurrection, CanonicalTombstonePolicy.noPhysicalDelete, CanonicalTombstonePolicy.noPermanentDelete, CanonicalTombstonePolicy.noGarbageCollection })
            .Distinct()
            .OrderBy(p => p.ToString(), StringComparer.Ordinal)
            .ToArray();
        TombstoneID = string.Join("|", "libraryTombstone", objectKind.ToString(), objectID.RawValue);
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryTombstone other && Equals(other);
    public bool Equals(CanonicalLibraryTombstone? other) =>
        other is not null && TombstoneID == other.TombstoneID;
    public override int GetHashCode() => TombstoneID.GetHashCode();
    public static bool operator ==(CanonicalLibraryTombstone left, CanonicalLibraryTombstone right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryTombstone left, CanonicalLibraryTombstone right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncRuntimeMode
{
    disabled,
    diagnosticsOnly,
    canonicalPlanNoCommit,
    canonicalPlanPrimaryWithLegacyFallback,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncRuntimeDiagnosticKind
{
    canonicalSyncRuntimeModeEvaluated,
    canonicalSyncRuntimeAuthorityGateAllowed,
    canonicalSyncRuntimeAuthorityGateBlocked,
    canonicalSyncRuntimePlanEvaluated,
    canonicalSyncRuntimePlanAllowed,
    canonicalSyncRuntimePlanUsed,
    canonicalSyncRuntimePlanNoCommit,
    canonicalSyncRuntimePlanFallback,
    canonicalSyncRuntimePlanBlocked,
    canonicalSyncRuntimeLegacyHashMismatchIgnored,
    canonicalSyncRuntimeUnsupportedObjectBlocked,
    canonicalSyncRuntimeConflictBlocked,
    canonicalSyncRuntimePeerSnapshotUnavailable,
    canonicalSyncRuntimeDuplicateLegacySuppressed,
    canonicalSyncRuntimeDuplicateExecutionPrevented,
    canonicalSyncRuntimeMetadataHashEqual,
    canonicalSyncRuntimeModifiedAtLWWApplied,
    canonicalSyncRuntimeModifiedAtUnavailable,
    canonicalSyncRuntimeSchemaMismatch,
    canonicalExistenceTruthEvaluated,
    canonicalExistenceApplyBridgeEvaluated,
    canonicalExistenceApplyBridgeBlocked,
    canonicalExistenceMetadataOnlyRecordWritten,
    canonicalExistenceMetadataOnlyRecordNoOp,
    canonicalExistenceApplyBridgeRollbackStarted,
    canonicalExistenceApplyBridgeRollbackCompleted,
    canonicalExistenceApplyBridgeRollbackFailed,
    canonicalExistencePeerMetadataOnlyUploadCandidate,
    canonicalExistencePeerAbsentMetadataBridgeRequired,
    canonicalExistencePeerUnknownDeferred,
    canonicalExistenceAudioSameNoOp,
    canonicalExistenceAudioConflict,
    canonicalExistenceManifestRecordingsConsumed,
    canonicalExistenceManifestRecordingsIgnoredBlocked,
    canonicalExistenceDidNotWriteAudio,
    canonicalExistenceDidNotMarkAudioAvailable,
    canonicalApplyRuntimeModeEvaluated,
    canonicalApplyRuntimeGateAllowed,
    canonicalApplyRuntimeGateBlocked,
    canonicalApplyRuntimeActionStarted,
    canonicalApplyRuntimeActionCompleted,
    canonicalApplyRuntimeActionFailed,
    canonicalApplyRuntimeRollbackStarted,
    canonicalApplyRuntimeRollbackCompleted,
    canonicalApplyRuntimeRollbackFailed,
    canonicalApplyRuntimeLegacyFallbackUsed,
    canonicalApplyRuntimeDuplicateLegacySuppressed,
    canonicalApplyRuntimeAudioActionBlocked,
    canonicalApplyRuntimeReportBuilt
}

public sealed class CanonicalSyncRuntimeDiagnostic : IEquatable<CanonicalSyncRuntimeDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), SyncRunID ?? "", ObjectID ?? "", ActionKind ?? "", Detail ?? "");
    public CanonicalSyncRuntimeDiagnosticKind Kind { get; }
    public string? SyncRunID { get; }
    public CanonicalSyncRuntimeMode Mode { get; }
    public string? ObjectID { get; }
    public string? ActionKind { get; }
    public string? HashPrefix { get; }
    public int? Count { get; }
    public string? Detail { get; }

    public CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind kind,
        string? syncRunID = null,
        CanonicalSyncRuntimeMode mode = default,
        string? objectID = null,
        string? actionKind = null,
        CanonicalHash? hash = null,
        string? hashPrefix = null,
        int? count = null,
        string? detail = null)
    {
        Kind = kind;
        SyncRunID = SafeText(syncRunID);
        Mode = mode;
        ObjectID = SafeText(objectID) is { } s ? s[..Math.Min(s.Length, 48)] : null;
        ActionKind = SafeText(actionKind);
        HashPrefix = hash is { } h ? HashPrefixOf(h.Value) : hashPrefix is { } hp ? HashPrefixOf(hp) : null;
        Count = count;
        Detail = SafeText(detail);
    }

    public bool IsRedacted
    {
        get
        {
            var values = new[] { SyncRunID, ObjectID, ActionKind, HashPrefix, Detail };
            var nonNull = values.Where(v => v != null).Cast<string>().ToArray();
            if (nonNull.Any(v => v.Contains('/') || v.Contains('\\') || v.Contains("://") || v.Contains('{') || v.Contains('}')))
                return false;
            return !HashPrefix.HasValue() || HashPrefix!.Length <= 12;
        }
    }

    public string Summary()
    {
        var parts = new List<string> { $"mode={Mode.ToString()}" };
        if (SyncRunID != null) parts.Add($"syncRunID={SyncRunID}");
        if (ObjectID != null) parts.Add($"objectID={ObjectID}");
        if (ActionKind != null) parts.Add($"action={ActionKind}");
        if (HashPrefix != null) parts.Add($"hashPrefix={HashPrefix}");
        if (Count.HasValue) parts.Add($"count={Count.Value}");
        if (Detail != null) parts.Add($"detail={Detail}");
        return string.Join(",", parts);
    }

    private static string HashPrefixOf(string value) =>
        value.Trim()[..Math.Min(value.Trim().Length, 12)];

    private static string? SafeText(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        var forbidden = new[] { "/", "\\", "://", "{", "}", "\n", "\r" };
        if (!forbidden.Any(f => trimmed!.Contains(f, StringComparison.Ordinal)))
            return trimmed;
        var sanitized = forbidden.Aggregate(trimmed!, (current, token) => current.Replace(token, "_"));
        return sanitized[..Math.Min(sanitized.Length, 12)];
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncRuntimeDiagnostic other && Equals(other);
    public bool Equals(CanonicalSyncRuntimeDiagnostic? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalSyncRuntimeDiagnostic left, CanonicalSyncRuntimeDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncRuntimeDiagnostic left, CanonicalSyncRuntimeDiagnostic right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalArtifactProducer
{
    audioCapture,
    transcription,
    noteGeneration,
    unknown
}

public static class CanonicalProjectionContract
{
    public static readonly HashSet<CanonicalArtifact.Kind> GeneratedArtifactKinds = new()
    {
        CanonicalArtifact.Kind.transcriptJSON,
        CanonicalArtifact.Kind.transcriptMarkdown,
        CanonicalArtifact.Kind.noteMarkdown,
        CanonicalArtifact.Kind.noteJSON,
        CanonicalArtifact.Kind.summaryJSON
    };

    public static string ArtifactID(string objectID, CanonicalArtifact.Kind kind) =>
        kind.ArtifactIDFor(NormalizedRequired(objectID, "unknown-recording"));

    public static string ArtifactKey(string objectID, CanonicalArtifact.Kind kind) =>
        $"{NormalizedRequired(objectID, "unknown-recording")}|{kind.ToString()}";

    public static CanonicalLibraryObjectID MakeCanonicalFolderID(string folderID) =>
        new(folderID, "folder:unknown");

    public static CanonicalLibraryObjectID MakeCanonicalStudyItemID(string itemID) =>
        new(itemID, "studyItem:unknown");

    public static string NormalizeFolderName(string name) =>
        name.Trim().NilIfEmpty() ?? "未命名文件夹";

    public static string NormalizeStudyItemTitle(string title, CanonicalStudyItemKind itemKind = CanonicalStudyItemKind.unknown) =>
        title.Trim().NilIfEmpty() ?? (itemKind == CanonicalStudyItemKind.standaloneNote ? "未命名笔记" : "未命名条目");

    public static string[] NormalizeTags(string[]? tags) =>
        (tags ?? Array.Empty<string>())
            .Select(t => t.Trim().NilIfEmpty()?.ToLowerInvariant())
            .Where(t => t != null)
            .Cast<string>()
            .Distinct()
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();

    public static CanonicalHierarchyPath NormalizeFilingPath(CanonicalHierarchyPath path) =>
        new(path.Components);

    public static CanonicalParentReference[] NormalizeParentReferences(CanonicalParentReference[]? references)
    {
        if (references == null) return Array.Empty<CanonicalParentReference>();
        var seen = new HashSet<string>();
        return references
            .OrderBy(r => r.ParentID.RawValue, StringComparer.Ordinal)
            .Where(r => seen.Add($"{r.Relation}|{r.ParentID.RawValue}"))
            .ToArray();
    }

    public static CanonicalLibraryObjectID[] DeduplicateFolderIDs(CanonicalLibraryObjectID[]? folderIDs) =>
        (folderIDs ?? Array.Empty<CanonicalLibraryObjectID>())
            .Distinct()
            .OrderBy(id => id.RawValue, StringComparer.Ordinal)
            .ToArray();

    public static Dictionary<string, string> MetadataHashPayloadFor(CanonicalFolderMetadata folder) =>
        new()
        {
            ["schema"] = "canonical-folder-business-metadata-v1",
            ["folderID"] = folder.FolderID.RawValue,
            ["name"] = folder.Name,
            ["parentID"] = folder.ParentID?.RawValue ?? "",
            ["hierarchyPath"] = folder.HierarchyPath.StableKey,
            ["hierarchyLevel"] = folder.HierarchyLevel ?? "",
            ["colorToken"] = folder.ColorToken ?? "",
            ["orderingKey"] = folder.OrderingKey ?? "",
            ["isDeleted"] = folder.IsDeleted ? "true" : "false",
            ["deletedAt"] = folder.DeletedAt.HasValue ? TimestampString(folder.DeletedAt.Value) : ""
        };

    public static Dictionary<string, string> MetadataHashPayloadFor(CanonicalStudyItemMetadata item) =>
        new()
        {
            ["schema"] = "canonical-study-item-business-metadata-v1",
            ["itemID"] = item.ItemID.RawValue,
            ["itemKind"] = item.ItemKind.ToString(),
            ["title"] = item.Title,
            ["filingPath"] = item.FilingPath.StableKey,
            ["folderIDs"] = string.Join("\u001F", item.FolderIDs.Select(f => f.RawValue)),
            ["parentReferences"] = string.Join("\u001F", item.ParentReferences.Select(p => $"{p.Relation}:{p.ParentID.RawValue}")),
            ["tags"] = string.Join("\u001F", item.Tags),
            ["resourceTokens"] = string.Join("\u001F", item.LogicalResourceTokens),
            ["associatedRecordingID"] = item.AssociatedRecordingID ?? "",
            ["isDeleted"] = item.IsDeleted ? "true" : "false",
            ["deletedAt"] = item.DeletedAt.HasValue ? TimestampString(item.DeletedAt.Value) : ""
        };

    public static string? SafeLogicalResourceToken(string? token) => SafeLogicalPathToken(token);

    public static string? SafeLogicalPathToken(string? token)
    {
        var trimmed = token?.Trim();
        if (string.IsNullOrEmpty(trimmed) ||
            trimmed!.StartsWith('/') ||
            trimmed.Contains("://", StringComparison.Ordinal) ||
            trimmed.Contains('\\'))
            return null;
        var components = trimmed.Split('/', StringSplitOptions.None);
        if (components.Length == 0 || components.Any(c => string.IsNullOrEmpty(c) || c == "." || c == ".."))
            return null;
        return trimmed;
    }

    public static bool ProvesGeneratedArtifactAvailability(CanonicalArtifact? artifact) =>
        artifact is not null &&
        GeneratedArtifactKinds.Contains(artifact.ArtifactKind) &&
        artifact.Availability == CanonicalArtifact.AvailabilityKind.available &&
        artifact.ContentHash is not null &&
        artifact.ByteSize.HasValue &&
        artifact.Tombstone != true;

    public static bool SameContent(CanonicalArtifact left, CanonicalArtifact right) =>
        left.ContentHash?.Algorithm == right.ContentHash?.Algorithm
        && left.ContentHash?.Value == right.ContentHash?.Value
        && left.ByteSize == right.ByteSize
        && left.ContentHash is not null
        && left.ByteSize is not null;

    public static bool IsAuthoritativeProducer(CanonicalArtifact artifact, CanonicalNode node)
    {
        if (artifact.Tombstone == true) return false;
        var requiredCap = RequiredCapability(artifact.ArtifactKind);
        if (requiredCap is null) return false;
        if (!node.Capabilities.Contains(requiredCap.Value)) return false;
        if (artifact.ProducedByNodeID is not null && artifact.ProducedByNodeID != node.NodeID)
            return false;

        switch (artifact.ArtifactKind)
        {
            case CanonicalArtifact.Kind.audio:
                return artifact.ProducedBy == CanonicalArtifactProducer.audioCapture
                    && node.Platform.ToLowerInvariant().Contains("iphone");
            case CanonicalArtifact.Kind.transcriptJSON:
            case CanonicalArtifact.Kind.transcriptMarkdown:
                return artifact.ProducedBy == CanonicalArtifactProducer.transcription
                    && node.Platform.ToLowerInvariant().Contains("mac");
            case CanonicalArtifact.Kind.noteMarkdown:
            case CanonicalArtifact.Kind.noteJSON:
            case CanonicalArtifact.Kind.summaryJSON:
                return artifact.ProducedBy == CanonicalArtifactProducer.noteGeneration
                    && node.Platform.ToLowerInvariant().Contains("mac");
            default:
                return false;
        }
    }

    internal static CanonicalCapability? RequiredCapability(CanonicalArtifact.Kind kind) =>
        kind switch
        {
            CanonicalArtifact.Kind.audio => CanonicalCapability.audioArtifact,
            CanonicalArtifact.Kind.transcriptJSON => CanonicalCapability.transcriptArtifact,
            CanonicalArtifact.Kind.transcriptMarkdown => CanonicalCapability.transcriptArtifact,
            CanonicalArtifact.Kind.noteMarkdown => CanonicalCapability.noteArtifact,
            CanonicalArtifact.Kind.noteJSON => CanonicalCapability.noteArtifact,
            CanonicalArtifact.Kind.summaryJSON => CanonicalCapability.summaryArtifact,
            _ => null
        };

    private static string TimestampString(CanonicalTimestamp timestamp) =>
        ToUnixTimeDouble(timestamp.Date).ToString("F6", CultureInfo.InvariantCulture);

    private static double ToUnixTimeDouble(DateTime date) =>
        (date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;

    private static string NormalizedRequired(string value, string fallback) =>
        value.Trim().NilIfEmpty() ?? fallback;
}

// ─── Core types from CanonicalCore.swift ─────────────────────────────────────

public readonly struct CanonicalTimestamp : IEquatable<CanonicalTimestamp>, IComparable<CanonicalTimestamp>
{
    public DateTime Date { get; }

    public CanonicalTimestamp(DateTime date)
    {
        Date = date;
    }

    public override bool Equals(object? obj) => obj is CanonicalTimestamp other && Equals(other);
    public bool Equals(CanonicalTimestamp other) => Date == other.Date;
    public override int GetHashCode() => Date.GetHashCode();
    public static bool operator <(CanonicalTimestamp left, CanonicalTimestamp right) => left.Date < right.Date;
    public static bool operator >(CanonicalTimestamp left, CanonicalTimestamp right) => left.Date > right.Date;
    public static bool operator <=(CanonicalTimestamp left, CanonicalTimestamp right) => left.Date <= right.Date;
    public static bool operator >=(CanonicalTimestamp left, CanonicalTimestamp right) => left.Date >= right.Date;
    public static bool operator ==(CanonicalTimestamp left, CanonicalTimestamp right) => left.Equals(right);
    public static bool operator !=(CanonicalTimestamp left, CanonicalTimestamp right) => !left.Equals(right);
    public int CompareTo(CanonicalTimestamp other) => Date.CompareTo(other.Date);
}

public readonly struct CanonicalHash : IEquatable<CanonicalHash>
{
    public string Algorithm { get; }
    public string Value { get; }

    public CanonicalHash(string value, string algorithm = "sha256")
    {
        Algorithm = algorithm;
        Value = value.Trim().ToLowerInvariant();
    }

    public static CanonicalHash Sha256Of(Dictionary<string, string> payload)
    {
        var sorted = new SortedDictionary<string, string>(payload, StringComparer.Ordinal);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        foreach (var kv in sorted)
        {
            writer.WriteString(kv.Key, kv.Value);
        }
        writer.WriteEndObject();
        writer.Flush();
        var data = stream.ToArray();
        return Sha256(data);
    }

    public static CanonicalHash Sha256String(string value) =>
        Sha256(Encoding.UTF8.GetBytes(value));

    private static CanonicalHash Sha256(byte[] data)
    {
        var digest = SHA256.HashData(data);
        var hex = string.Concat(digest.Select(b => b.ToString("x2")));
        return new CanonicalHash(hex);
    }

    public override bool Equals(object? obj) => obj is CanonicalHash other && Equals(other);
    public bool Equals(CanonicalHash other) => Algorithm == other.Algorithm && Value == other.Value;
    public override int GetHashCode() => HashCode.Combine(Algorithm, Value);
    public override string ToString() => $"{Algorithm}:{Value}";
    public static bool operator ==(CanonicalHash left, CanonicalHash right) => left.Equals(right);
    public static bool operator !=(CanonicalHash left, CanonicalHash right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalCapability
{
    recordingMetadata,
    audioArtifact,
    receiveRecord,
    transcriptArtifact,
    noteArtifact,
    summaryArtifact,
    objectProjection,
    canonicalLibraryObjectsV1,
    canonicalFolderObjectsV1,
    canonicalStudyItemObjectsV1,
    canonicalTransferStateV1,
    canonicalObjectProjectionV1,
    canonicalInventoryBuilderV1,
    canonicalRetirementReadinessV1
}

public sealed class CanonicalNode : IEquatable<CanonicalNode>
{
    public string Id => NodeID;

    public string NodeID { get; }
    public string Platform { get; }
    public string? DisplayName { get; }
    public CanonicalCapability[] Capabilities { get; }

    public CanonicalNode(
        string nodeID,
        string platform,
        string? displayName = null,
        CanonicalCapability[]? capabilities = null)
    {
        NodeID = nodeID;
        Platform = platform;
        DisplayName = displayName?.Trim().NilIfEmpty();
        Capabilities = (capabilities ?? Array.Empty<CanonicalCapability>())
            .OrderBy(c => c.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public override bool Equals(object? obj) => obj is CanonicalNode other && Equals(other);
    public bool Equals(CanonicalNode? other) =>
        other is not null &&
        NodeID == other.NodeID &&
        Platform == other.Platform;
    public override int GetHashCode() => HashCode.Combine(NodeID, Platform);
    public static bool operator ==(CanonicalNode left, CanonicalNode right) => left.Equals(right);
    public static bool operator !=(CanonicalNode left, CanonicalNode right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadata : IEquatable<CanonicalRecordingMetadata>
{
    public const string BusinessMetadataHashSchemaVersion = "canonical-recording-business-metadata-v1";

    public sealed class Filing : IEquatable<Filing>
    {
        public string? Type { get; }
        public string? Subject { get; }
        public string? Chapter { get; }
        public string? Topic { get; }

        public Filing(string? type = null, string? subject = null, string? chapter = null, string? topic = null)
        {
            Type = Normalized(type);
            Subject = Normalized(subject);
            Chapter = Normalized(chapter);
            Topic = Normalized(topic);
        }

        public bool IsEmpty => Type == null && Subject == null && Chapter == null && Topic == null;

        private static string? Normalized(string? value) => value?.Trim().NilIfEmpty();

        public override bool Equals(object? obj) => obj is Filing other && Equals(other);
        public bool Equals(Filing? other) =>
            other is not null &&
            Type == other.Type && Subject == other.Subject && Chapter == other.Chapter && Topic == other.Topic;
        public override int GetHashCode() => HashCode.Combine(Type, Subject, Chapter, Topic);
        public static bool operator ==(Filing left, Filing right) => left.Equals(right);
        public static bool operator !=(Filing left, Filing right) => !left.Equals(right);
    }

    public string ObjectID { get; }
    public string Title { get; }
    public CanonicalTimestamp CreatedAt { get; }
    public CanonicalTimestamp ModifiedAt { get; }
    public TimeSpan? Duration { get; }
    public Filing? FilingValue { get; }
    public string[] Tags { get; }
    public bool IsDeleted { get; }
    public CanonicalTimestamp? DeletedAt { get; }

    public CanonicalRecordingMetadata(
        string objectID,
        string title,
        CanonicalTimestamp createdAt,
        CanonicalTimestamp modifiedAt,
        TimeSpan? duration = null,
        Filing? filing = null,
        string[]? tags = null,
        bool isDeleted = false,
        CanonicalTimestamp? deletedAt = null)
    {
        ObjectID = NormalizedRequired(objectID, "unknown-recording");
        Title = NormalizedRequiredPreservingInput(title, "未命名录音");
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
        Duration = duration;
        FilingValue = filing?.IsEmpty == true ? null : filing;
        Tags = NormalizedTags(tags ?? Array.Empty<string>());
        IsDeleted = isDeleted;
        DeletedAt = deletedAt;
    }

    public CanonicalHash MetadataHash => CanonicalHash.Sha256Of(new Dictionary<string, string>
    {
        ["schema"] = BusinessMetadataHashSchemaVersion,
        ["objectID"] = ObjectID,
        ["title"] = Title,
        ["filing.type"] = FilingValue?.Type ?? "",
        ["filing.subject"] = FilingValue?.Subject ?? "",
        ["filing.chapter"] = FilingValue?.Chapter ?? "",
        ["filing.topic"] = FilingValue?.Topic ?? "",
        ["tags"] = string.Join("\u001F", Tags),
        ["isDeleted"] = IsDeleted ? "true" : "false",
        ["deletedAt"] = DeletedAt.HasValue ? TimestampString(DeletedAt.Value) : ""
    });

    private static string NormalizedRequired(string value, string fallback) =>
        value.Trim().NilIfEmpty() ?? fallback;

    private static string NormalizedRequiredPreservingInput(string value, string fallback) =>
        value.Trim().Length == 0 ? fallback : value;

    private static string[] NormalizedTags(string[] tags) =>
        tags
            .Select(t => t.Trim().NilIfEmpty()?.ToLowerInvariant())
            .Where(t => t != null)
            .Cast<string>()
            .Distinct()
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();

    private static string TimestampString(CanonicalTimestamp timestamp) =>
        NumberString(ToUnixTimeDouble(timestamp.Date));

    private static string NumberString(double value) =>
        value.ToString("F6", CultureInfo.InvariantCulture);

    private static double ToUnixTimeDouble(DateTime date) =>
        (date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadata other && Equals(other);
    public bool Equals(CanonicalRecordingMetadata? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        Title == other.Title &&
        CreatedAt.Equals(other.CreatedAt) &&
        ModifiedAt.Equals(other.ModifiedAt) &&
        Duration == other.Duration &&
        Equals(FilingValue, other.FilingValue) &&
        Tags.SequenceEqual(other.Tags) &&
        IsDeleted == other.IsDeleted &&
        Nullable.Equals(DeletedAt, other.DeletedAt);
    public override int GetHashCode() => HashCode.Combine(ObjectID, Title, CreatedAt, ModifiedAt, IsDeleted);
    public static bool operator ==(CanonicalRecordingMetadata left, CanonicalRecordingMetadata right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadata left, CanonicalRecordingMetadata right) => !left.Equals(right);
}

public sealed class CanonicalArtifact : IEquatable<CanonicalArtifact>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Kind
    {
        audio,
        metadata,
        receiveRecord,
        transcriptJSON,
        transcriptMarkdown,
        noteMarkdown,
        noteJSON,
        summaryJSON
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AvailabilityKind
    {
        unknown,
        missing,
        availableWithoutHash,
        available
    }

    public string Id => ArtifactID;

    public string ArtifactID { get; }
    public string ObjectID { get; }
    public Kind ArtifactKind { get; }
    public AvailabilityKind Availability { get; }
    public CanonicalHash? ContentHash { get; }
    public long? ByteSize { get; }
    public string? LogicalName { get; }
    public string? LogicalPathToken { get; }
    public CanonicalTimestamp? ModifiedAt { get; }
    public CanonicalTimestamp? ObservedAt { get; }
    public CanonicalArtifactProducer? ProducedBy { get; }
    public string? ProducedByNodeID { get; }
    public bool? Tombstone { get; }

    public CanonicalArtifact(
        string artifactID,
        string objectID,
        Kind kind,
        AvailabilityKind availability,
        CanonicalHash? contentHash = null,
        long? byteSize = null,
        string? logicalName = null,
        string? logicalPathToken = null,
        CanonicalTimestamp? modifiedAt = null,
        CanonicalTimestamp? observedAt = null,
        CanonicalArtifactProducer? producedBy = null,
        string? producedByNodeID = null,
        bool? tombstone = null)
    {
        ArtifactID = artifactID.Trim().NilIfEmpty() ?? kind.ArtifactIDFor(objectID);
        ObjectID = objectID.Trim();
        ArtifactKind = kind;
        Availability = availability;
        ContentHash = contentHash;
        ByteSize = byteSize;
        LogicalName = logicalName?.Trim().NilIfEmpty();
        LogicalPathToken = CanonicalProjectionContract.SafeLogicalPathToken(logicalPathToken);
        ModifiedAt = modifiedAt;
        ObservedAt = observedAt;
        ProducedBy = producedBy;
        ProducedByNodeID = producedByNodeID?.Trim().NilIfEmpty();
        Tombstone = tombstone;
    }

    public bool ProvesCanonicalAudioAvailability =>
        ArtifactKind == Kind.audio && Availability == AvailabilityKind.available &&
        ContentHash != null && ByteSize.HasValue && Tombstone != true;

    public bool ProvesCanonicalGeneratedArtifactAvailability =>
        CanonicalProjectionContract.ProvesGeneratedArtifactAvailability(this);

    public bool IsCanonicalGeneratedArtifact =>
        CanonicalProjectionContract.GeneratedArtifactKinds.Contains(ArtifactKind);

    public override bool Equals(object? obj) => obj is CanonicalArtifact other && Equals(other);
    public bool Equals(CanonicalArtifact? other) =>
        other is not null && ArtifactID == other.ArtifactID;
    public override int GetHashCode() => ArtifactID.GetHashCode();
    public static bool operator ==(CanonicalArtifact left, CanonicalArtifact right) => left.Equals(right);
    public static bool operator !=(CanonicalArtifact left, CanonicalArtifact right) => !left.Equals(right);
}

public sealed class CanonicalArtifactFact : IEquatable<CanonicalArtifactFact>
{
    public CanonicalArtifact.Kind Kind { get; }
    public CanonicalArtifact.AvailabilityKind Availability { get; }
    public CanonicalHash? ContentHash { get; }
    public long? ByteSize { get; }
    public string? LogicalName { get; }
    public string? LogicalPathToken { get; }
    public CanonicalTimestamp? ModifiedAt { get; }
    public CanonicalTimestamp? ObservedAt { get; }
    public CanonicalArtifactProducer? ProducedBy { get; }
    public string? ProducedByNodeID { get; }
    public bool? Tombstone { get; }

    public CanonicalArtifactFact(
        CanonicalArtifact.Kind kind,
        CanonicalArtifact.AvailabilityKind availability,
        CanonicalHash? contentHash = null,
        long? byteSize = null,
        string? logicalName = null,
        string? logicalPathToken = null,
        CanonicalTimestamp? modifiedAt = null,
        CanonicalTimestamp? observedAt = null,
        CanonicalArtifactProducer? producedBy = null,
        string? producedByNodeID = null,
        bool? tombstone = null)
    {
        Kind = kind;
        Availability = availability;
        ContentHash = contentHash;
        ByteSize = byteSize;
        LogicalName = logicalName?.Trim().NilIfEmpty();
        LogicalPathToken = CanonicalProjectionContract.SafeLogicalPathToken(logicalPathToken);
        ModifiedAt = modifiedAt;
        ObservedAt = observedAt;
        ProducedBy = producedBy;
        ProducedByNodeID = producedByNodeID?.Trim().NilIfEmpty();
        Tombstone = tombstone;
    }

    public static CanonicalArtifactFact Audio(
        CanonicalArtifact.AvailabilityKind availability,
        CanonicalHash? contentHash = null,
        long? byteSize = null,
        string? logicalName = null,
        string? logicalPathToken = null,
        CanonicalTimestamp? modifiedAt = null,
        CanonicalTimestamp? observedAt = null,
        string? producedByNodeID = null) =>
        new(
            kind: CanonicalArtifact.Kind.audio,
            availability: availability,
            contentHash: contentHash,
            byteSize: byteSize,
            logicalName: logicalName,
            logicalPathToken: logicalPathToken,
            modifiedAt: modifiedAt,
            observedAt: observedAt,
            producedBy: CanonicalArtifactProducer.audioCapture,
            producedByNodeID: producedByNodeID
        );

    public CanonicalArtifact MakeArtifact(string objectID, string? fallbackProducedByNodeID = null) =>
        new(
            artifactID: CanonicalProjectionContract.ArtifactID(objectID, Kind),
            objectID: objectID,
            kind: Kind,
            availability: Availability,
            contentHash: ContentHash,
            byteSize: ByteSize,
            logicalName: LogicalName,
            logicalPathToken: LogicalPathToken,
            modifiedAt: ModifiedAt,
            observedAt: ObservedAt,
            producedBy: ProducedBy,
            producedByNodeID: ProducedByNodeID ?? fallbackProducedByNodeID,
            tombstone: Tombstone
        );

    public override bool Equals(object? obj) => obj is CanonicalArtifactFact other && Equals(other);
    public bool Equals(CanonicalArtifactFact? other) =>
        other is not null &&
        Kind == other.Kind &&
        Availability == other.Availability &&
        Nullable.Equals(ContentHash, other.ContentHash) &&
        ByteSize == other.ByteSize &&
        LogicalName == other.LogicalName &&
        LogicalPathToken == other.LogicalPathToken &&
        Nullable.Equals(ModifiedAt, other.ModifiedAt) &&
        Nullable.Equals(ObservedAt, other.ObservedAt) &&
        ProducedBy == other.ProducedBy &&
        ProducedByNodeID == other.ProducedByNodeID &&
        Tombstone == other.Tombstone;
    public override int GetHashCode() => HashCode.Combine(Kind, Availability, ContentHash, ByteSize, LogicalPathToken, ProducedByNodeID);
    public static bool operator ==(CanonicalArtifactFact left, CanonicalArtifactFact right) => left.Equals(right);
    public static bool operator !=(CanonicalArtifactFact left, CanonicalArtifactFact right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncState
{
    unknown,
    localOnly,
    synced,
    diverged,
    deleted,
    conflict
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTransferState
{
    none,
    queued,
    inFlight,
    retryPending,
    completed,
    failed,
    conflict
}

public sealed class CanonicalProcessingState : IEquatable<CanonicalProcessingState>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Stage
    {
        notStarted,
        queued,
        processing,
        completed,
        failed,
        unknown
    }

    public Stage Transcription { get; }
    public Stage Note { get; }

    public CanonicalProcessingState(Stage transcription = Stage.unknown, Stage note = Stage.unknown)
    {
        Transcription = transcription;
        Note = note;
    }

    public static readonly CanonicalProcessingState Unknown = new(Stage.unknown, Stage.unknown);

    public override bool Equals(object? obj) => obj is CanonicalProcessingState other && Equals(other);
    public bool Equals(CanonicalProcessingState? other) =>
        other is not null && Transcription == other.Transcription && Note == other.Note;
    public override int GetHashCode() => HashCode.Combine(Transcription, Note);
    public static bool operator ==(CanonicalProcessingState left, CanonicalProcessingState right) => left.Equals(right);
    public static bool operator !=(CanonicalProcessingState left, CanonicalProcessingState right) => !left.Equals(right);
}

public sealed class CanonicalRecordingObject : IEquatable<CanonicalRecordingObject>
{
    public string Id => ObjectID;

    public string ObjectID { get; }
    public string? NodeID { get; }
    private CanonicalRecordingMetadata _metadata;
    public CanonicalRecordingMetadata Metadata
    {
        get => _metadata;
        private set
        {
            _metadata = value;
            MetadataHash = value.MetadataHash;
        }
    }
    public CanonicalHash MetadataHash { get; private set; }
    public CanonicalArtifact[] Artifacts { get; }
    public CanonicalSyncState SyncState { get; }
    public CanonicalTransferState TransferState { get; }
    public CanonicalProcessingState ProcessingState { get; }
    public CanonicalTimestamp? ReceivedAt { get; }
    public CanonicalTimestamp? ObservedAt { get; }

    public CanonicalRecordingObject(
        string objectID,
        string? nodeID = null,
        CanonicalRecordingMetadata? metadata = null,
        CanonicalArtifact[]? artifacts = null,
        CanonicalSyncState syncState = CanonicalSyncState.unknown,
        CanonicalTransferState transferState = CanonicalTransferState.none,
        CanonicalProcessingState? processingState = null,
        CanonicalTimestamp? receivedAt = null,
        CanonicalTimestamp? observedAt = null)
    {
        var md = metadata ?? throw new ArgumentNullException(nameof(metadata));
        ObjectID = objectID.Trim().NilIfEmpty() ?? md.ObjectID;
        NodeID = nodeID?.Trim().NilIfEmpty();
        _metadata = md;
        MetadataHash = md.MetadataHash;
        Artifacts = (artifacts ?? Array.Empty<CanonicalArtifact>())
            .OrderBy(a => a.ArtifactID, StringComparer.Ordinal)
            .ToArray();
        SyncState = syncState;
        TransferState = transferState;
        ProcessingState = processingState ?? CanonicalProcessingState.Unknown;
        ReceivedAt = receivedAt;
        ObservedAt = observedAt;
    }

    public CanonicalArtifact? AudioArtifact =>
        Artifacts.FirstOrDefault(a => a.ArtifactKind == CanonicalArtifact.Kind.audio);

    public bool AudioAvailable =>
        AudioArtifact?.ProvesCanonicalAudioAvailability == true;

    public CanonicalRecordingObject ReplacingArtifacts(CanonicalArtifact[] artifacts) =>
        new(
            objectID: ObjectID,
            nodeID: NodeID,
            metadata: Metadata,
            artifacts: artifacts,
            syncState: SyncState,
            transferState: TransferState,
            processingState: ProcessingState,
            receivedAt: ReceivedAt,
            observedAt: ObservedAt
        );

    public override bool Equals(object? obj) => obj is CanonicalRecordingObject other && Equals(other);
    public bool Equals(CanonicalRecordingObject? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        MetadataHash.Equals(other.MetadataHash);
    public override int GetHashCode() => HashCode.Combine(ObjectID, MetadataHash);
    public static bool operator ==(CanonicalRecordingObject left, CanonicalRecordingObject right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingObject left, CanonicalRecordingObject right) => !left.Equals(right);
}

public sealed class CanonicalManifest : IEquatable<CanonicalManifest>
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public CanonicalNode Node { get; }
    public CanonicalTimestamp GeneratedAt { get; }
    public CanonicalRecordingObject[] Objects { get; }
    public CanonicalLibraryObject[] LibraryObjects { get; }
    public CanonicalFolderObject[] Folders { get; }
    public CanonicalStudyItemObject[] StudyItems { get; }
    public CanonicalStandaloneNoteObject[] StandaloneNotes { get; }
    public CanonicalLibraryTombstone[] LibraryTombstones { get; }
    public CanonicalCapability[] ManifestCapabilities { get; }
    public CanonicalHash ManifestHash { get; internal set; }

    [JsonConstructor]
    public CanonicalManifest(
        int schemaVersion,
        CanonicalNode node,
        CanonicalTimestamp generatedAt,
        CanonicalRecordingObject[] objects,
        CanonicalLibraryObject[]? libraryObjects = null,
        CanonicalFolderObject[]? folders = null,
        CanonicalStudyItemObject[]? studyItems = null,
        CanonicalStandaloneNoteObject[]? standaloneNotes = null,
        CanonicalLibraryTombstone[]? libraryTombstones = null,
        CanonicalCapability[]? manifestCapabilities = null,
        CanonicalHash manifestHash = default)
    {
        SchemaVersion = schemaVersion;
        Node = node;
        GeneratedAt = generatedAt;
        Objects = (objects ?? Array.Empty<CanonicalRecordingObject>())
            .OrderBy(o => o.ObjectID, StringComparer.Ordinal).ToArray();
        LibraryObjects = (libraryObjects ?? Array.Empty<CanonicalLibraryObject>())
            .OrderBy(o => o.ObjectID.RawValue, StringComparer.Ordinal).ToArray();
        Folders = (folders ?? Array.Empty<CanonicalFolderObject>())
            .OrderBy(f => f.FolderID.RawValue, StringComparer.Ordinal).ToArray();
        StudyItems = (studyItems ?? Array.Empty<CanonicalStudyItemObject>())
            .OrderBy(s => s.ItemID.RawValue, StringComparer.Ordinal).ToArray();
        StandaloneNotes = (standaloneNotes ?? Array.Empty<CanonicalStandaloneNoteObject>())
            .OrderBy(s => s.NoteID.RawValue, StringComparer.Ordinal).ToArray();
        LibraryTombstones = (libraryTombstones ?? Array.Empty<CanonicalLibraryTombstone>())
            .OrderBy(t => t.TombstoneID, StringComparer.Ordinal).ToArray();
        ManifestCapabilities = (manifestCapabilities ?? Array.Empty<CanonicalCapability>())
            .Distinct()
            .OrderBy(c => c.ToString(), StringComparer.Ordinal)
            .ToArray();
        ManifestHash = manifestHash;
    }

    public static CanonicalManifest Make(
        CanonicalNode node,
        DateTime? generatedAt = null,
        CanonicalRecordingObject[]? objects = null,
        CanonicalLibraryObject[]? libraryObjects = null,
        CanonicalFolderObject[]? folders = null,
        CanonicalStudyItemObject[]? studyItems = null,
        CanonicalStandaloneNoteObject[]? standaloneNotes = null,
        CanonicalLibraryTombstone[]? libraryTombstones = null,
        CanonicalCapability[]? manifestCapabilities = null)
    {
        var sortedObjects = (objects ?? Array.Empty<CanonicalRecordingObject>())
            .OrderBy(o => o.ObjectID, StringComparer.Ordinal).ToArray();
        var sortedLibraryObjects = (libraryObjects ?? Array.Empty<CanonicalLibraryObject>())
            .OrderBy(o => o.ObjectID.RawValue, StringComparer.Ordinal).ToArray();
        var sortedFolders = (folders ?? Array.Empty<CanonicalFolderObject>())
            .OrderBy(f => f.FolderID.RawValue, StringComparer.Ordinal).ToArray();
        var sortedStudyItems = (studyItems ?? Array.Empty<CanonicalStudyItemObject>())
            .OrderBy(s => s.ItemID.RawValue, StringComparer.Ordinal).ToArray();
        var sortedStandaloneNotes = (standaloneNotes ?? Array.Empty<CanonicalStandaloneNoteObject>())
            .OrderBy(s => s.NoteID.RawValue, StringComparer.Ordinal).ToArray();
        var sortedTombstones = (libraryTombstones ?? Array.Empty<CanonicalLibraryTombstone>())
            .OrderBy(t => t.TombstoneID, StringComparer.Ordinal).ToArray();
        var caps = (manifestCapabilities ?? Array.Empty<CanonicalCapability>())
            .Distinct()
            .OrderBy(c => c.ToString(), StringComparer.Ordinal)
            .ToArray();

        var manifest = new CanonicalManifest(
            schemaVersion: CurrentSchemaVersion,
            node: node,
            generatedAt: new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow),
            objects: sortedObjects,
            libraryObjects: sortedLibraryObjects,
            folders: sortedFolders,
            studyItems: sortedStudyItems,
            standaloneNotes: sortedStandaloneNotes,
            libraryTombstones: sortedTombstones,
            manifestCapabilities: caps,
            manifestHash: new CanonicalHash("")
        );
        manifest.ManifestHash = manifest.ComputedManifestHash();
        return manifest;
    }

    public CanonicalRecordingObject? ObjectWithID(string objectID) =>
        Objects.FirstOrDefault(o => o.ObjectID == objectID);

    public CanonicalHash ComputedManifestHash() =>
        CanonicalHash.Sha256Of(new Dictionary<string, string>
        {
            ["schemaVersion"] = SchemaVersion.ToString(),
            ["nodeID"] = Node.NodeID,
            ["nodePlatform"] = Node.Platform,
            ["nodeCapabilities"] = string.Join("\u001F", Node.Capabilities.Select(c => c.ToString())),
            ["manifestCapabilities"] = string.Join("\u001F", ManifestCapabilities.Select(c => c.ToString())),
            ["generatedAt"] = TimestampString(GeneratedAt),
            ["objects"] = string.Join("\u001E", Objects.Select(ObjectHashSummary)),
            ["libraryObjects"] = string.Join("\u001E", LibraryObjects.Select(LibraryObjectHashSummary)),
            ["folders"] = string.Join("\u001E", Folders.Select(f => f.MetadataHash.Value)),
            ["studyItems"] = string.Join("\u001E", StudyItems.Select(s => s.MetadataHash.Value)),
            ["standaloneNotes"] = string.Join("\u001E", StandaloneNotes.Select(s => s.MetadataHash.Value)),
            ["libraryTombstones"] = string.Join("\u001E", LibraryTombstones.Select(LibraryTombstoneHashSummary))
        });

    public bool HasValidManifestHash
    {
        get
        {
            var computed = ComputedManifestHash();
            return ManifestHash.Algorithm == computed.Algorithm && ManifestHash.Value == computed.Value;
        }
    }

    private static string ObjectHashSummary(CanonicalRecordingObject obj)
    {
        var artifacts = obj.Artifacts.Select(a =>
            string.Join("\u001F", new[]
            {
                a.ArtifactID,
                a.ArtifactKind.ToString(),
                a.Availability.ToString(),
                a.ContentHash?.Value ?? "",
                a.ByteSize.HasValue ? a.ByteSize.Value.ToString() : ""
            })
        );
        return string.Join("\u001F", new[]
        {
            obj.ObjectID,
            obj.MetadataHash.Value,
            obj.MetadataHash.Algorithm,
            obj.SyncState.ToString(),
            obj.TransferState.ToString(),
            obj.Metadata.IsDeleted ? "deleted" : "active",
            string.Join("\u001D", artifacts)
        });
    }

    private static string LibraryObjectHashSummary(CanonicalLibraryObject obj) =>
        string.Join("\u001F", new[]
        {
            obj.ObjectID.RawValue,
            obj.Kind.ToString(),
            obj.MetadataHash.Value,
            obj.IsDeleted ? "deleted" : "active",
            obj.BusinessModifiedAt.HasValue ? TimestampString(obj.BusinessModifiedAt.Value) : ""
        });

    private static string LibraryTombstoneHashSummary(CanonicalLibraryTombstone tombstone) =>
        string.Join("\u001F", new[]
        {
            tombstone.TombstoneID,
            tombstone.ObjectID.RawValue,
            tombstone.ObjectKind.ToString(),
            tombstone.DeletedAt.HasValue ? TimestampString(tombstone.DeletedAt.Value) : "",
            tombstone.Reason.ToString()
        });

    private static string TimestampString(CanonicalTimestamp timestamp) =>
        ToUnixTimeDouble(timestamp.Date).ToString("F6", CultureInfo.InvariantCulture);

    private static double ToUnixTimeDouble(DateTime date) =>
        (date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;

    public override bool Equals(object? obj) => obj is CanonicalManifest other && Equals(other);
    public bool Equals(CanonicalManifest? other) =>
        other is not null &&
        ManifestHash.Equals(other.ManifestHash);
    public override int GetHashCode() => ManifestHash.GetHashCode();
    public static bool operator ==(CanonicalManifest left, CanonicalManifest right) => left.Equals(right);
    public static bool operator !=(CanonicalManifest left, CanonicalManifest right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConflictReason
{
    objectIdentityCollision,
    metadataModifiedOnBothSides,
    artifactHashMismatch,
    artifactSizeMismatch,
    artifactUnavailableMismatch
}

public sealed class SyncDecision : IEquatable<SyncDecision>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KindEnum
    {
        noOp,
        uploadMetadata,
        downloadMetadata,
        deferUntilPeerKnown,
        conflict
    }

    public KindEnum Kind { get; }
    public string ObjectID { get; }
    public string Reason { get; }
    public ConflictReason? ConflictReasonValue { get; }

    public SyncDecision(KindEnum kind, string objectID, string reason, ConflictReason? conflictReason = null)
    {
        Kind = kind;
        ObjectID = objectID;
        Reason = reason;
        ConflictReasonValue = conflictReason;
    }

    public static SyncDecision Metadata(CanonicalRecordingObject local, CanonicalRecordingObject? peer)
    {
        if (peer == null)
            return new SyncDecision(KindEnum.uploadMetadata, local.ObjectID, "peer_missing_metadata");

        if (SameHash(local.MetadataHash, peer.MetadataHash))
            return new SyncDecision(KindEnum.noOp, local.ObjectID, "metadata_hash_equal");

        if (local.Metadata.ModifiedAt > peer.Metadata.ModifiedAt)
            return new SyncDecision(KindEnum.uploadMetadata, local.ObjectID, "local_metadata_newer");

        if (peer.Metadata.ModifiedAt > local.Metadata.ModifiedAt)
            return new SyncDecision(KindEnum.downloadMetadata, local.ObjectID, "peer_metadata_newer");

        return new SyncDecision(
            KindEnum.conflict,
            local.ObjectID,
            "metadata_hash_mismatch_same_modified_at",
            ConflictReason.metadataModifiedOnBothSides
        );
    }

    private static bool SameHash(CanonicalHash left, CanonicalHash right) =>
        left.Algorithm == right.Algorithm && left.Value == right.Value;

    public override bool Equals(object? obj) => obj is SyncDecision other && Equals(other);
    public bool Equals(SyncDecision? other) =>
        other is not null &&
        Kind == other.Kind &&
        ObjectID == other.ObjectID &&
        Reason == other.Reason &&
        ConflictReasonValue == other.ConflictReasonValue;
    public override int GetHashCode() => HashCode.Combine(Kind, ObjectID, Reason, ConflictReasonValue);
    public static bool operator ==(SyncDecision left, SyncDecision right) => left.Equals(right);
    public static bool operator !=(SyncDecision left, SyncDecision right) => !left.Equals(right);
}

public sealed class TransferDecision : IEquatable<TransferDecision>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KindEnum
    {
        noOp,
        upload,
        download,
        deferUntilPeerKnown,
        conflict,
        localUnavailable
    }

    public KindEnum Kind { get; }
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public string Reason { get; }
    public ConflictReason? ConflictReasonValue { get; }

    public TransferDecision(KindEnum kind, string objectID, string? artifactID, string reason, ConflictReason? conflictReason = null)
    {
        Kind = kind;
        ObjectID = objectID;
        ArtifactID = artifactID;
        Reason = reason;
        ConflictReasonValue = conflictReason;
    }

    public static TransferDecision Audio(CanonicalRecordingObject local, CanonicalRecordingObject? peer)
    {
        var localAudio = local.AudioArtifact;
        var peerAudio = peer?.AudioArtifact;
        var artifactID = localAudio?.ArtifactID ?? peerAudio?.ArtifactID;

        if (localAudio == null || !localAudio.ProvesCanonicalAudioAvailability)
            return new TransferDecision(
                KindEnum.localUnavailable,
                local.ObjectID,
                artifactID,
                "local_audio_unproven"
            );

        if (peer == null)
            return new TransferDecision(
                KindEnum.deferUntilPeerKnown,
                local.ObjectID,
                artifactID,
                "peer_unknown_is_not_missing"
            );

        if (peerAudio == null)
            return new TransferDecision(
                KindEnum.upload,
                local.ObjectID,
                artifactID,
                "peer_audio_missing"
            );

        if (peerAudio.Availability == CanonicalArtifact.AvailabilityKind.missing)
            return new TransferDecision(
                KindEnum.upload,
                local.ObjectID,
                artifactID,
                "peer_audio_missing"
            );

        if (!peerAudio.ProvesCanonicalAudioAvailability)
            return new TransferDecision(
                KindEnum.deferUntilPeerKnown,
                local.ObjectID,
                artifactID,
                "peer_audio_unproven"
            );

        if (!SameHash(localAudio.ContentHash, peerAudio.ContentHash))
            return new TransferDecision(
                KindEnum.conflict,
                local.ObjectID,
                artifactID,
                "audio_hash_mismatch",
                ConflictReason.artifactHashMismatch
            );

        if (localAudio.ByteSize != peerAudio.ByteSize)
            return new TransferDecision(
                KindEnum.conflict,
                local.ObjectID,
                artifactID,
                "audio_size_mismatch",
                ConflictReason.artifactSizeMismatch
            );

        return new TransferDecision(
            KindEnum.noOp,
            local.ObjectID,
            artifactID,
            "peer_audio_same_hash_and_size"
        );
    }

    private static bool SameHash(CanonicalHash? left, CanonicalHash? right) =>
        left?.Algorithm == right?.Algorithm && left?.Value == right?.Value;

    public override bool Equals(object? obj) => obj is TransferDecision other && Equals(other);
    public bool Equals(TransferDecision? other) =>
        other is not null &&
        Kind == other.Kind &&
        ObjectID == other.ObjectID &&
        ArtifactID == other.ArtifactID &&
        Reason == other.Reason &&
        ConflictReasonValue == other.ConflictReasonValue;
    public override int GetHashCode() => HashCode.Combine(Kind, ObjectID, ArtifactID, Reason);
    public static bool operator ==(TransferDecision left, TransferDecision right) => left.Equals(right);
    public static bool operator !=(TransferDecision left, TransferDecision right) => !left.Equals(right);
}

public sealed class ObjectProjection : IEquatable<ObjectProjection>
{
    public string Id => ObjectID;

    public string ObjectID { get; }
    public string DisplayTitle { get; }
    public CanonicalHash MetadataHash { get; }
    public bool AudioAvailable { get; }
    public CanonicalSyncState SyncState { get; }
    public CanonicalTransferState TransferState { get; }
    public CanonicalProcessingState ProcessingState { get; }
    public ConflictReason[] ConflictReasons { get; }

    public ObjectProjection(
        string objectID,
        string displayTitle,
        CanonicalHash metadataHash,
        bool audioAvailable,
        CanonicalSyncState syncState,
        CanonicalTransferState transferState,
        CanonicalProcessingState processingState,
        ConflictReason[] conflictReasons)
    {
        ObjectID = objectID;
        DisplayTitle = displayTitle;
        MetadataHash = metadataHash;
        AudioAvailable = audioAvailable;
        SyncState = syncState;
        TransferState = transferState;
        ProcessingState = processingState;
        ConflictReasons = conflictReasons;
    }

    public static ObjectProjection Make(CanonicalRecordingObject obj, ConflictReason[]? conflictReasons = null) =>
        new(
            objectID: obj.ObjectID,
            displayTitle: obj.Metadata.Title,
            metadataHash: obj.MetadataHash,
            audioAvailable: obj.AudioAvailable,
            syncState: obj.SyncState,
            transferState: obj.TransferState,
            processingState: obj.ProcessingState,
            conflictReasons: conflictReasons ?? Array.Empty<ConflictReason>()
        );

    public override bool Equals(object? obj) => obj is ObjectProjection other && Equals(other);
    public bool Equals(ObjectProjection? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        MetadataHash.Equals(other.MetadataHash);
    public override int GetHashCode() => HashCode.Combine(ObjectID, MetadataHash);
    public static bool operator ==(ObjectProjection left, ObjectProjection right) => left.Equals(right);
    public static bool operator !=(ObjectProjection left, ObjectProjection right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingExistenceState
{
    absent,
    metadataOnly,
    receiveRecordOnly,
    studyItemOnly,
    metadataAndStudyItem,
    audioAvailable,
    audioHashSizeMatched,
    audioConflict,
    peerUnknown,
    tombstoned,
    unsupported
}

public static class CanonicalRecordingExistenceStateExtensions
{
    public static bool IsAudioProof(this CanonicalRecordingExistenceState state) =>
        state is CanonicalRecordingExistenceState.audioAvailable or CanonicalRecordingExistenceState.audioHashSizeMatched;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingExistenceSource
{
    canonicalManifest,
    studyLibraryManifest,
    localInventory,
    peerInventory,
    recordingMetadata,
    receiveRecord,
    studyItem,
    audioArtifact,
    completedUploadLedger,
    canonicalExistenceLedger
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingExistenceDecision
{
    noOp,
    applyMetadataOnlyBridge,
    uploadAudioCandidate,
    audioSameNoOp,
    conflict,
    deferred,
    blocked,
    unsupported
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingExistenceBlocker
{
    tombstonedParent,
    peerUnknown,
    missingLocalAudio,
    localAudioUnproven,
    peerAudioUnproven,
    audioHashMismatch,
    audioSizeMismatch,
    completedLedgerNotAudioProof,
    metadataOnlyNotAudioProof,
    receiveRecordNotAudioProof,
    studyItemNotAudioProof,
    unsupportedObject
}

public sealed class CanonicalRecordingExistenceTruth : IEquatable<CanonicalRecordingExistenceTruth>
{
    public string ObjectID { get; }
    public CanonicalRecordingExistenceState LocalState { get; }
    public CanonicalRecordingExistenceState PeerState { get; }
    public CanonicalRecordingExistenceDecision Decision { get; }
    public CanonicalRecordingExistenceSource[] Sources { get; }
    public CanonicalRecordingExistenceBlocker[] Blockers { get; }
    public string? LocalMetadataHashPrefix { get; }
    public string? PeerMetadataHashPrefix { get; }
    public string? LocalAudioHashPrefix { get; }
    public string? PeerAudioHashPrefix { get; }
    public long? LocalByteSize { get; }
    public long? PeerByteSize { get; }

    public CanonicalRecordingExistenceTruth(
        string objectID,
        CanonicalRecordingExistenceState localState,
        CanonicalRecordingExistenceState peerState,
        CanonicalRecordingExistenceDecision decision,
        CanonicalRecordingExistenceSource[] sources,
        CanonicalRecordingExistenceBlocker[] blockers,
        string? localMetadataHashPrefix = null,
        string? peerMetadataHashPrefix = null,
        string? localAudioHashPrefix = null,
        string? peerAudioHashPrefix = null,
        long? localByteSize = null,
        long? peerByteSize = null)
    {
        ObjectID = objectID;
        LocalState = localState;
        PeerState = peerState;
        Decision = decision;
        Sources = sources;
        Blockers = blockers;
        LocalMetadataHashPrefix = localMetadataHashPrefix;
        PeerMetadataHashPrefix = peerMetadataHashPrefix;
        LocalAudioHashPrefix = localAudioHashPrefix;
        PeerAudioHashPrefix = peerAudioHashPrefix;
        LocalByteSize = localByteSize;
        PeerByteSize = peerByteSize;
    }

    public bool PeerAudioAvailable => PeerState.IsAudioProof();

    public bool ShouldCreateUploadCandidate => Decision == CanonicalRecordingExistenceDecision.uploadAudioCandidate;

    public bool RequiresMetadataApplyBridge => Decision == CanonicalRecordingExistenceDecision.applyMetadataOnlyBridge;

    public static CanonicalRecordingExistenceTruth Evaluate(
        string objectID,
        CanonicalRecordingObject? local,
        CanonicalRecordingObject? peer,
        bool peerKnown = true,
        bool peerStudyItemExists = false,
        bool peerReceiveRecordExists = false,
        bool peerCompletedLedgerOnly = false,
        bool tombstonedParent = false)
    {
        var normalizedObjectID = objectID.Trim();
        var sources = new HashSet<CanonicalRecordingExistenceSource> { CanonicalRecordingExistenceSource.canonicalManifest };
        if (local != null)
        {
            sources.Add(CanonicalRecordingExistenceSource.localInventory);
            sources.Add(CanonicalRecordingExistenceSource.recordingMetadata);
        }
        if (peer != null)
        {
            sources.Add(CanonicalRecordingExistenceSource.peerInventory);
            sources.Add(CanonicalRecordingExistenceSource.recordingMetadata);
        }
        if (peerStudyItemExists)
            sources.Add(CanonicalRecordingExistenceSource.studyItem);
        if (peerReceiveRecordExists)
            sources.Add(CanonicalRecordingExistenceSource.receiveRecord);
        if (peerCompletedLedgerOnly)
            sources.Add(CanonicalRecordingExistenceSource.completedUploadLedger);

        var localState = ExistenceState(
            obj: local,
            known: true,
            studyItemExists: false,
            receiveRecordExists: false,
            completedLedgerOnly: false,
            tombstonedParent: tombstonedParent
        );
        var peerState = ExistenceState(
            obj: peer,
            known: peerKnown,
            studyItemExists: peerStudyItemExists,
            receiveRecordExists: peerReceiveRecordExists,
            completedLedgerOnly: peerCompletedLedgerOnly,
            tombstonedParent: tombstonedParent
        );
        var blockers = new HashSet<CanonicalRecordingExistenceBlocker>();

        if (tombstonedParent)
        {
            blockers.Add(CanonicalRecordingExistenceBlocker.tombstonedParent);
            return Truth(
                objectID: normalizedObjectID,
                local: local,
                peer: peer,
                localState: localState,
                peerState: CanonicalRecordingExistenceState.tombstoned,
                decision: CanonicalRecordingExistenceDecision.blocked,
                sources: sources,
                blockers: blockers
            );
        }

        if (!peerKnown)
        {
            blockers.Add(CanonicalRecordingExistenceBlocker.peerUnknown);
            return Truth(
                objectID: normalizedObjectID,
                local: local,
                peer: peer,
                localState: localState,
                peerState: CanonicalRecordingExistenceState.peerUnknown,
                decision: CanonicalRecordingExistenceDecision.deferred,
                sources: sources,
                blockers: blockers
            );
        }

        var localAudio = local?.AudioArtifact;
        if (localAudio == null)
        {
            blockers.Add(CanonicalRecordingExistenceBlocker.missingLocalAudio);
            return Truth(
                objectID: normalizedObjectID,
                local: local,
                peer: peer,
                localState: localState,
                peerState: peerState,
                decision: peerState == CanonicalRecordingExistenceState.absent
                    ? CanonicalRecordingExistenceDecision.applyMetadataOnlyBridge
                    : CanonicalRecordingExistenceDecision.noOp,
                sources: sources,
                blockers: blockers
            );
        }

        if (!localAudio.ProvesCanonicalAudioAvailability)
        {
            blockers.Add(CanonicalRecordingExistenceBlocker.localAudioUnproven);
            return Truth(
                objectID: normalizedObjectID,
                local: local,
                peer: peer,
                localState: localState,
                peerState: peerState,
                decision: CanonicalRecordingExistenceDecision.blocked,
                sources: sources,
                blockers: blockers
            );
        }

        switch (peerState)
        {
            case CanonicalRecordingExistenceState.absent:
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.applyMetadataOnlyBridge,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.metadataOnly:
                blockers.Add(CanonicalRecordingExistenceBlocker.metadataOnlyNotAudioProof);
                if (peerCompletedLedgerOnly)
                    blockers.Add(CanonicalRecordingExistenceBlocker.completedLedgerNotAudioProof);
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.uploadAudioCandidate,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.receiveRecordOnly:
                blockers.Add(CanonicalRecordingExistenceBlocker.receiveRecordNotAudioProof);
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.uploadAudioCandidate,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.studyItemOnly:
                blockers.Add(CanonicalRecordingExistenceBlocker.studyItemNotAudioProof);
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.uploadAudioCandidate,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.metadataAndStudyItem:
                blockers.Add(CanonicalRecordingExistenceBlocker.metadataOnlyNotAudioProof);
                blockers.Add(CanonicalRecordingExistenceBlocker.studyItemNotAudioProof);
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.uploadAudioCandidate,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.audioAvailable:
            {
                var peerAudio = peer?.AudioArtifact;
                if (peerAudio == null || !peerAudio.ProvesCanonicalAudioAvailability)
                {
                    blockers.Add(CanonicalRecordingExistenceBlocker.peerAudioUnproven);
                    return Truth(
                        objectID: normalizedObjectID,
                        local: local,
                        peer: peer,
                        localState: localState,
                        peerState: CanonicalRecordingExistenceState.unsupported,
                        decision: CanonicalRecordingExistenceDecision.deferred,
                        sources: sources,
                        blockers: blockers
                    );
                }
                if (!Nullable.Equals(localAudio.ContentHash, peerAudio.ContentHash))
                {
                    blockers.Add(CanonicalRecordingExistenceBlocker.audioHashMismatch);
                    var conflictPeerState = CanonicalRecordingExistenceState.audioConflict;
                    return Truth(
                        objectID: normalizedObjectID,
                        local: local,
                        peer: peer,
                        localState: localState,
                        peerState: conflictPeerState,
                        decision: CanonicalRecordingExistenceDecision.conflict,
                        sources: sources,
                        blockers: blockers
                    );
                }
                if (localAudio.ByteSize != peerAudio.ByteSize)
                {
                    blockers.Add(CanonicalRecordingExistenceBlocker.audioSizeMismatch);
                    var conflictPeerState = CanonicalRecordingExistenceState.audioConflict;
                    return Truth(
                        objectID: normalizedObjectID,
                        local: local,
                        peer: peer,
                        localState: localState,
                        peerState: conflictPeerState,
                        decision: CanonicalRecordingExistenceDecision.conflict,
                        sources: sources,
                        blockers: blockers
                    );
                }
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: CanonicalRecordingExistenceState.audioHashSizeMatched,
                    decision: CanonicalRecordingExistenceDecision.audioSameNoOp,
                    sources: sources,
                    blockers: blockers
                );
            }
            case CanonicalRecordingExistenceState.audioHashSizeMatched:
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: CanonicalRecordingExistenceState.audioHashSizeMatched,
                    decision: CanonicalRecordingExistenceDecision.audioSameNoOp,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.audioConflict:
                blockers.Add(CanonicalRecordingExistenceBlocker.audioHashMismatch);
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.conflict,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.peerUnknown:
                blockers.Add(CanonicalRecordingExistenceBlocker.peerUnknown);
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.deferred,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.tombstoned:
                blockers.Add(CanonicalRecordingExistenceBlocker.tombstonedParent);
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.blocked,
                    sources: sources,
                    blockers: blockers
                );
            case CanonicalRecordingExistenceState.unsupported:
                blockers.Add(CanonicalRecordingExistenceBlocker.unsupportedObject);
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.unsupported,
                    sources: sources,
                    blockers: blockers
                );
            default:
                return Truth(
                    objectID: normalizedObjectID,
                    local: local,
                    peer: peer,
                    localState: localState,
                    peerState: peerState,
                    decision: CanonicalRecordingExistenceDecision.noOp,
                    sources: sources,
                    blockers: blockers
                );
        }
    }

    public CanonicalSyncRuntimeDiagnostic[] Diagnostics(
        string? syncRunID,
        CanonicalSyncRuntimeMode mode)
    {
        var output = new List<CanonicalSyncRuntimeDiagnostic>
        {
            new(
                kind: CanonicalSyncRuntimeDiagnosticKind.canonicalExistenceTruthEvaluated,
                syncRunID: syncRunID,
                mode: mode,
                objectID: ObjectID,
                hashPrefix: LocalAudioHashPrefix ?? PeerAudioHashPrefix ?? LocalMetadataHashPrefix ?? PeerMetadataHashPrefix,
                count: LocalByteSize.HasValue ? (int)LocalByteSize.Value : null,
                detail: $"{LocalState.ToString()}->{PeerState.ToString()}:{Decision.ToString()}"
            )
        };

        switch (Decision)
        {
            case CanonicalRecordingExistenceDecision.applyMetadataOnlyBridge:
                output.Add(new CanonicalSyncRuntimeDiagnostic(
                    kind: CanonicalSyncRuntimeDiagnosticKind.canonicalExistencePeerAbsentMetadataBridgeRequired,
                    syncRunID: syncRunID, mode: mode, objectID: ObjectID,
                    hashPrefix: LocalMetadataHashPrefix, detail: PeerState.ToString()));
                break;
            case CanonicalRecordingExistenceDecision.uploadAudioCandidate:
                output.Add(new CanonicalSyncRuntimeDiagnostic(
                    kind: CanonicalSyncRuntimeDiagnosticKind.canonicalExistencePeerMetadataOnlyUploadCandidate,
                    syncRunID: syncRunID, mode: mode, objectID: ObjectID,
                    hashPrefix: LocalAudioHashPrefix,
                    count: LocalByteSize.HasValue ? (int)LocalByteSize.Value : null,
                    detail: PeerState.ToString()));
                break;
            case CanonicalRecordingExistenceDecision.audioSameNoOp:
                output.Add(new CanonicalSyncRuntimeDiagnostic(
                    kind: CanonicalSyncRuntimeDiagnosticKind.canonicalExistenceAudioSameNoOp,
                    syncRunID: syncRunID, mode: mode, objectID: ObjectID,
                    hashPrefix: LocalAudioHashPrefix,
                    count: LocalByteSize.HasValue ? (int)LocalByteSize.Value : null,
                    detail: "sameHashAndSize"));
                break;
            case CanonicalRecordingExistenceDecision.conflict:
                output.Add(new CanonicalSyncRuntimeDiagnostic(
                    kind: CanonicalSyncRuntimeDiagnosticKind.canonicalExistenceAudioConflict,
                    syncRunID: syncRunID, mode: mode, objectID: ObjectID,
                    hashPrefix: LocalAudioHashPrefix,
                    count: LocalByteSize.HasValue ? (int)LocalByteSize.Value : null,
                    detail: string.Join("+", Blockers.Select(b => b.ToString()))));
                break;
            case CanonicalRecordingExistenceDecision.deferred when PeerState == CanonicalRecordingExistenceState.peerUnknown:
                output.Add(new CanonicalSyncRuntimeDiagnostic(
                    kind: CanonicalSyncRuntimeDiagnosticKind.canonicalExistencePeerUnknownDeferred,
                    syncRunID: syncRunID, mode: mode, objectID: ObjectID,
                    detail: "peerUnknown"));
                break;
        }

        return output.ToArray();
    }

    private static CanonicalRecordingExistenceState ExistenceState(
        CanonicalRecordingObject? obj,
        bool known,
        bool studyItemExists,
        bool receiveRecordExists,
        bool completedLedgerOnly,
        bool tombstonedParent)
    {
        if (!known)
            return CanonicalRecordingExistenceState.peerUnknown;
        if (tombstonedParent || obj?.SyncState == CanonicalSyncState.deleted)
            return CanonicalRecordingExistenceState.tombstoned;
        if (obj == null)
        {
            if (receiveRecordExists)
                return CanonicalRecordingExistenceState.receiveRecordOnly;
            if (studyItemExists)
                return CanonicalRecordingExistenceState.studyItemOnly;
            return completedLedgerOnly ? CanonicalRecordingExistenceState.metadataOnly : CanonicalRecordingExistenceState.absent;
        }
        if (obj.AudioAvailable)
            return CanonicalRecordingExistenceState.audioAvailable;
        if (receiveRecordExists && studyItemExists)
            return CanonicalRecordingExistenceState.metadataAndStudyItem;
        if (receiveRecordExists)
            return CanonicalRecordingExistenceState.receiveRecordOnly;
        if (studyItemExists)
            return CanonicalRecordingExistenceState.metadataAndStudyItem;
        return CanonicalRecordingExistenceState.metadataOnly;
    }

    private static CanonicalRecordingExistenceTruth Truth(
        string objectID,
        CanonicalRecordingObject? local,
        CanonicalRecordingObject? peer,
        CanonicalRecordingExistenceState localState,
        CanonicalRecordingExistenceState peerState,
        CanonicalRecordingExistenceDecision decision,
        HashSet<CanonicalRecordingExistenceSource> sources,
        HashSet<CanonicalRecordingExistenceBlocker> blockers) =>
        new(
            objectID: objectID,
            localState: localState,
            peerState: peerState,
            decision: decision,
            sources: sources.OrderBy(s => s.ToString(), StringComparer.Ordinal).ToArray(),
            blockers: blockers.OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray(),
            localMetadataHashPrefix: local?.MetadataHash.Value.ShortCanonicalPrefix(),
            peerMetadataHashPrefix: peer?.MetadataHash.Value.ShortCanonicalPrefix(),
            localAudioHashPrefix: local?.AudioArtifact?.ContentHash?.Value.ShortCanonicalPrefix(),
            peerAudioHashPrefix: peer?.AudioArtifact?.ContentHash?.Value.ShortCanonicalPrefix(),
            localByteSize: local?.AudioArtifact?.ByteSize,
            peerByteSize: peer?.AudioArtifact?.ByteSize
        );

    public override bool Equals(object? obj) => obj is CanonicalRecordingExistenceTruth other && Equals(other);
    public bool Equals(CanonicalRecordingExistenceTruth? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        LocalState == other.LocalState &&
        PeerState == other.PeerState &&
        Decision == other.Decision;
    public override int GetHashCode() => HashCode.Combine(ObjectID, LocalState, PeerState, Decision);
    public static bool operator ==(CanonicalRecordingExistenceTruth left, CanonicalRecordingExistenceTruth right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingExistenceTruth left, CanonicalRecordingExistenceTruth right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalExistenceApplyRuntimeMode
{
    disabled,
    diagnosticsOnly,
    noCommit,
    testRootApply,
    productionRootApply,
    blocked
}

public static class CanonicalExistenceApplyRuntimeModeExtensions
{
    public static bool EvaluatesCandidates(this CanonicalExistenceApplyRuntimeMode mode) =>
        mode != CanonicalExistenceApplyRuntimeMode.blocked;

    public static bool CanCommitMetadataOnlyRecord(this CanonicalExistenceApplyRuntimeMode mode) =>
        mode is CanonicalExistenceApplyRuntimeMode.testRootApply or CanonicalExistenceApplyRuntimeMode.productionRootApply;
}

public sealed class CanonicalExistenceApplyRuntimePolicy : IEquatable<CanonicalExistenceApplyRuntimePolicy>
{
    public bool DebugInternalBuild { get; }
    public bool OwnerApproved { get; }
    public bool ReleaseDefaultBuild { get; }
    public bool DiagnosticsRedacted { get; }
    public bool LegacyFallbackAvailable { get; }
    public bool RootBoundRequired { get; }
    public bool RollbackRequired { get; }
    public bool AtomicWriteRequired { get; }
    public bool PostconditionRequired { get; }
    public bool WriteAudioAllowed { get; }
    public bool MarkAudioAvailableAllowed { get; }

    public CanonicalExistenceApplyRuntimePolicy(
        bool debugInternalBuild = false,
        bool ownerApproved = false,
        bool releaseDefaultBuild = true,
        bool diagnosticsRedacted = true,
        bool legacyFallbackAvailable = true,
        bool rootBoundRequired = true,
        bool rollbackRequired = true,
        bool atomicWriteRequired = true,
        bool postconditionRequired = true,
        bool writeAudioAllowed = false,
        bool markAudioAvailableAllowed = false)
    {
        DebugInternalBuild = debugInternalBuild;
        OwnerApproved = ownerApproved;
        ReleaseDefaultBuild = releaseDefaultBuild;
        DiagnosticsRedacted = diagnosticsRedacted;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        RootBoundRequired = rootBoundRequired;
        RollbackRequired = rollbackRequired;
        AtomicWriteRequired = atomicWriteRequired;
        PostconditionRequired = postconditionRequired;
        WriteAudioAllowed = writeAudioAllowed;
        MarkAudioAvailableAllowed = markAudioAvailableAllowed;
    }

    public override bool Equals(object? obj) => obj is CanonicalExistenceApplyRuntimePolicy other && Equals(other);
    public bool Equals(CanonicalExistenceApplyRuntimePolicy? other) =>
        other is not null &&
        DebugInternalBuild == other.DebugInternalBuild &&
        OwnerApproved == other.OwnerApproved &&
        ReleaseDefaultBuild == other.ReleaseDefaultBuild &&
        DiagnosticsRedacted == other.DiagnosticsRedacted &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        RootBoundRequired == other.RootBoundRequired &&
        RollbackRequired == other.RollbackRequired &&
        AtomicWriteRequired == other.AtomicWriteRequired &&
        PostconditionRequired == other.PostconditionRequired &&
        WriteAudioAllowed == other.WriteAudioAllowed &&
        MarkAudioAvailableAllowed == other.MarkAudioAvailableAllowed;
    public override int GetHashCode() => HashCode.Combine(
        DebugInternalBuild, OwnerApproved, ReleaseDefaultBuild, DiagnosticsRedacted,
        LegacyFallbackAvailable, RootBoundRequired, RollbackRequired,
        AtomicWriteRequired, PostconditionRequired, WriteAudioAllowed, MarkAudioAvailableAllowed);
    public static bool operator ==(CanonicalExistenceApplyRuntimePolicy left, CanonicalExistenceApplyRuntimePolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalExistenceApplyRuntimePolicy left, CanonicalExistenceApplyRuntimePolicy right) => !left.Equals(right);
}

public sealed class CanonicalExistenceApplyRuntimeConfiguration : IEquatable<CanonicalExistenceApplyRuntimeConfiguration>
{
    public CanonicalExistenceApplyRuntimeMode Mode { get; }
    public CanonicalExistenceApplyRuntimePolicy Policy { get; }

    public CanonicalExistenceApplyRuntimeConfiguration(
        CanonicalExistenceApplyRuntimeMode mode = CanonicalExistenceApplyRuntimeMode.disabled,
        CanonicalExistenceApplyRuntimePolicy? policy = null)
    {
        Mode = mode;
        Policy = policy ?? new CanonicalExistenceApplyRuntimePolicy();
    }

    public static readonly CanonicalExistenceApplyRuntimeConfiguration Disabled = new();

    public bool CanWriteMetadataOnlyRecord
    {
        get
        {
            if (!Mode.CanCommitMetadataOnlyRecord() ||
                !Policy.DiagnosticsRedacted ||
                !Policy.LegacyFallbackAvailable ||
                !Policy.RootBoundRequired ||
                !Policy.RollbackRequired ||
                !Policy.AtomicWriteRequired ||
                !Policy.PostconditionRequired ||
                Policy.WriteAudioAllowed ||
                Policy.MarkAudioAvailableAllowed)
                return false;

            if (Mode == CanonicalExistenceApplyRuntimeMode.productionRootApply)
                return Policy.DebugInternalBuild && Policy.OwnerApproved && !Policy.ReleaseDefaultBuild;

            return Mode == CanonicalExistenceApplyRuntimeMode.testRootApply;
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalExistenceApplyRuntimeConfiguration other && Equals(other);
    public bool Equals(CanonicalExistenceApplyRuntimeConfiguration? other) =>
        other is not null && Mode == other.Mode && Policy.Equals(other.Policy);
    public override int GetHashCode() => HashCode.Combine(Mode, Policy);
    public static bool operator ==(CanonicalExistenceApplyRuntimeConfiguration left, CanonicalExistenceApplyRuntimeConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalExistenceApplyRuntimeConfiguration left, CanonicalExistenceApplyRuntimeConfiguration right) => !left.Equals(right);
}

// ─── Extensions (Swift private extensions on String) ────────────────────────

public static class CanonicalCoreStringExtensions
{
    public static string? NilIfEmpty(this string? value) =>
        string.IsNullOrEmpty(value) ? null : value;

    public static bool HasValue(this string? value) =>
        !string.IsNullOrEmpty(value);

    internal static string ShortCanonicalPrefix(this string value) =>
        value.Trim()[..Math.Min(value.Trim().Length, 12)];
}

// ─── Extension methods on CanonicalArtifact.Kind ─────────────────────────────

public static class CanonicalArtifactKindExtensions
{
    public static string ArtifactIDFor(this CanonicalArtifact.Kind kind, string objectID) =>
        $"{kind.ToString()}:{objectID}";
}
