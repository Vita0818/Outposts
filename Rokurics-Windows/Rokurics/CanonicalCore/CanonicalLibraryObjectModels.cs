using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

public sealed class CanonicalRecordingEnvelopeObject : IEquatable<CanonicalRecordingEnvelopeObject>
{
    public string Id => RecordingID;

    public string RecordingID { get; }
    public CanonicalLibraryObjectID? StudyItemID { get; }
    public CanonicalLibraryObjectID[] FolderIDs { get; }
    public CanonicalHierarchyPath FilingPath { get; }
    public string[] Tags { get; }

    public CanonicalRecordingEnvelopeObject(
        string recordingID,
        CanonicalLibraryObjectID? studyItemID = null,
        CanonicalLibraryObjectID[]? folderIDs = null,
        CanonicalHierarchyPath filingPath = default,
        string[]? tags = null)
    {
        RecordingID = recordingID.Trim().NilIfEmpty() ?? "unknown-recording";
        StudyItemID = studyItemID;
        FolderIDs = (folderIDs ?? System.Array.Empty<CanonicalLibraryObjectID>())
            .Distinct()
            .OrderBy(f => f.RawValue, System.StringComparer.Ordinal)
            .ToArray();
        FilingPath = filingPath;
        Tags = CanonicalProjectionContract.NormalizeTags(tags);
    }

    [JsonConstructor]
    public CanonicalRecordingEnvelopeObject(
        string recordingID,
        CanonicalLibraryObjectID? studyItemID,
        CanonicalLibraryObjectID[]? folderIDs,
        CanonicalHierarchyPath filingPath,
        string[]? tags,
        string id) : this(recordingID, studyItemID, folderIDs, filingPath, tags)
    {
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingEnvelopeObject other && Equals(other);
    public bool Equals(CanonicalRecordingEnvelopeObject? other) =>
        other is not null && RecordingID == other.RecordingID;
    public override int GetHashCode() => RecordingID.GetHashCode();
    public static bool operator ==(CanonicalRecordingEnvelopeObject left, CanonicalRecordingEnvelopeObject right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingEnvelopeObject left, CanonicalRecordingEnvelopeObject right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadata : IEquatable<CanonicalLibraryMetadata>
{
    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalObjectKind ObjectKind { get; }
    public string Title { get; }
    public CanonicalHash MetadataHash { get; }
    public CanonicalTimestamp? BusinessModifiedAt { get; }
    public bool IsDeleted { get; }
    public CanonicalTimestamp? DeletedAt { get; }

    [JsonConstructor]
    public CanonicalLibraryMetadata(
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        string title,
        CanonicalHash metadataHash,
        CanonicalTimestamp? businessModifiedAt = null,
        bool isDeleted = false,
        CanonicalTimestamp? deletedAt = null)
    {
        ObjectID = objectID;
        ObjectKind = objectKind;
        Title = title;
        MetadataHash = metadataHash;
        BusinessModifiedAt = businessModifiedAt;
        IsDeleted = isDeleted;
        DeletedAt = isDeleted ? deletedAt : null;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadata other && Equals(other);
    public bool Equals(CanonicalLibraryMetadata? other) =>
        other is not null &&
        ObjectID.Equals(other.ObjectID) &&
        ObjectKind == other.ObjectKind &&
        Title == other.Title &&
        MetadataHash.Equals(other.MetadataHash) &&
        Nullable.Equals(BusinessModifiedAt, other.BusinessModifiedAt) &&
        IsDeleted == other.IsDeleted &&
        Nullable.Equals(DeletedAt, other.DeletedAt);
    public override int GetHashCode() =>
        System.HashCode.Combine(ObjectID, ObjectKind, Title, MetadataHash, BusinessModifiedAt, IsDeleted, DeletedAt);
    public static bool operator ==(CanonicalLibraryMetadata left, CanonicalLibraryMetadata right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadata left, CanonicalLibraryMetadata right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryConflictKind
{
    folderMetadataConcurrentEdit,
    studyItemMetadataConcurrentEdit,
    activeVsTombstone,
    recordingEnvelopeMetadataDisagreement,
    unsupportedLibraryObject
}

public sealed class CanonicalLibraryConflict : IEquatable<CanonicalLibraryConflict>
{
    public string Id => ConflictID;

    public string ConflictID { get; }
    public CanonicalLibraryConflictKind Kind { get; }
    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalObjectKind ObjectKind { get; }
    public string? LocalHashPrefix { get; }
    public string? PeerHashPrefix { get; }
    public CanonicalTimestamp? LocalModifiedAt { get; }
    public CanonicalTimestamp? PeerModifiedAt { get; }
    public string? Detail { get; }

    public CanonicalLibraryConflict(
        CanonicalLibraryConflictKind kind,
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        CanonicalHash? localHash = null,
        CanonicalHash? peerHash = null,
        CanonicalTimestamp? localModifiedAt = null,
        CanonicalTimestamp? peerModifiedAt = null,
        string? detail = null)
    {
        Kind = kind;
        ObjectID = objectID;
        ObjectKind = objectKind;
        LocalHashPrefix = localHash.HasValue
            ? localHash.Value.Value.Length >= 12 ? localHash.Value.Value[..12] : localHash.Value.Value
            : null;
        PeerHashPrefix = peerHash.HasValue
            ? peerHash.Value.Value.Length >= 12 ? peerHash.Value.Value[..12] : peerHash.Value.Value
            : null;
        LocalModifiedAt = localModifiedAt;
        PeerModifiedAt = peerModifiedAt;
        Detail = detail?.Trim().NilIfEmpty();
        ConflictID = string.Join("|", "libraryConflict", kind.ToString(), objectKind.ToString(), objectID.RawValue);
    }

    [JsonConstructor]
    public CanonicalLibraryConflict(
        CanonicalLibraryConflictKind kind,
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        string? localHashPrefix,
        string? peerHashPrefix,
        CanonicalTimestamp? localModifiedAt,
        CanonicalTimestamp? peerModifiedAt,
        string? detail,
        string conflictID) : this(kind, objectID, objectKind, null, null, localModifiedAt, peerModifiedAt, detail)
    {
        ConflictID = conflictID;
        LocalHashPrefix = localHashPrefix;
        PeerHashPrefix = peerHashPrefix;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryConflict other && Equals(other);
    public bool Equals(CanonicalLibraryConflict? other) =>
        other is not null && ConflictID == other.ConflictID;
    public override int GetHashCode() => ConflictID.GetHashCode();
    public static bool operator ==(CanonicalLibraryConflict left, CanonicalLibraryConflict right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryConflict left, CanonicalLibraryConflict right) => !left.Equals(right);
}
