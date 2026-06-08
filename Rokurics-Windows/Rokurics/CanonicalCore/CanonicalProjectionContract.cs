using System.Globalization;

namespace Rokurics.CanonicalCore;

public static class CanonicalProjectionContract
{
    private const string UnitSeparator = "\u001F";

    public static readonly HashSet<CanonicalArtifactKind> GeneratedArtifactKinds = new()
    {
        CanonicalArtifactKind.TranscriptJson,
        CanonicalArtifactKind.TranscriptMarkdown,
        CanonicalArtifactKind.NoteMarkdown,
        CanonicalArtifactKind.NoteJson,
        CanonicalArtifactKind.SummaryJson
    };

    public static string ArtifactId(string objectId, CanonicalArtifactKind kind) =>
        kind.ArtifactIdFor(NormalizedRequired(objectId, "unknown-recording"));

    public static string ArtifactKey(string objectId, CanonicalArtifactKind kind) =>
        $"{NormalizedRequired(objectId, "unknown-recording")}|{CanonicalArtifactKindRawValue.Of(kind)}";

    public static CanonicalLibraryObjectId MakeCanonicalFolderId(string folderId) =>
        new(folderId, "folder:unknown");

    public static CanonicalLibraryObjectId MakeCanonicalStudyItemId(string itemId) =>
        new(itemId, "studyItem:unknown");

    public static string NormalizeFolderName(string name) =>
        StringUtil.NilIfEmpty(name?.Trim()) ?? "未命名文件夹";

    public static string NormalizeStudyItemTitle(string title, CanonicalStudyItemKind itemKind = CanonicalStudyItemKind.Unknown) =>
        StringUtil.NilIfEmpty(title?.Trim()) ?? (itemKind == CanonicalStudyItemKind.StandaloneNote ? "未命名笔记" : "未命名条目");

    public static List<string> NormalizeTags(List<string>? tags)
    {
        if (tags is null) return new List<string>();
        return new HashSet<string>(
            tags
                .Select(t => StringUtil.NilIfEmpty(t?.Trim())?.ToLowerInvariant())
                .Where(t => t is not null)
                .Cast<string>()
        ).OrderBy(t => t).ToList();
    }

    public static CanonicalHierarchyPath NormalizeFilingPath(CanonicalHierarchyPath path) =>
        new(path.Components);

    public static List<CanonicalParentReference> NormalizeParentReferences(List<CanonicalParentReference>? references)
    {
        if (references is null) return new List<CanonicalParentReference>();
        var seen = new HashSet<string>();
        return references
            .OrderBy(r => r.ParentId.RawValue)
            .Where(r => seen.Add($"{r.Relation}|{r.ParentId.RawValue}"))
            .ToList();
    }

    public static CanonicalTimestamp? NormalizeTombstone(bool isDeleted, CanonicalTimestamp? deletedAt) =>
        isDeleted ? deletedAt : null;

    public static Dictionary<string, string> MetadataHashPayload(CanonicalFolderMetadata folder) =>
        new()
        {
            ["schema"] = "canonical-folder-business-metadata-v1",
            ["folderID"] = folder.FolderId.RawValue,
            ["name"] = folder.Name,
            ["parentID"] = folder.ParentId?.RawValue ?? "",
            ["hierarchyPath"] = folder.HierarchyPath.StableKey,
            ["hierarchyLevel"] = folder.HierarchyLevel ?? "",
            ["colorToken"] = folder.ColorToken ?? "",
            ["orderingKey"] = folder.OrderingKey ?? "",
            ["isDeleted"] = folder.IsDeleted ? "true" : "false",
            ["deletedAt"] = folder.DeletedAt is not null ? TimestampString(folder.DeletedAt) : ""
        };

    public static Dictionary<string, string> MetadataHashPayload(CanonicalStudyItemMetadata item) =>
        new()
        {
            ["schema"] = "canonical-study-item-business-metadata-v1",
            ["itemID"] = item.ItemId.RawValue,
            ["itemKind"] = CanonicalStudyItemKindRawValue.Of(item.ItemKind),
            ["title"] = item.Title,
            ["filingPath"] = item.FilingPath.StableKey,
            ["folderIDs"] = string.Join(UnitSeparator, item.FolderIds.Select(f => f.RawValue)),
            ["parentReferences"] = string.Join(UnitSeparator, item.ParentReferences.Select(p => $"{p.Relation}:{p.ParentId.RawValue}")),
            ["tags"] = string.Join(UnitSeparator, item.Tags),
            ["resourceTokens"] = string.Join(UnitSeparator, item.LogicalResourceTokens),
            ["associatedRecordingID"] = item.AssociatedRecordingId ?? "",
            ["isDeleted"] = item.IsDeleted ? "true" : "false",
            ["deletedAt"] = item.DeletedAt is not null ? TimestampString(item.DeletedAt) : ""
        };

    public static CanonicalObjectKind ObjectKind(string? legacyItemKind, string? recordingId)
    {
        var kind = (legacyItemKind ?? "").Trim();
        if (StringUtil.NilIfEmpty(recordingId?.Trim()) is not null)
            return CanonicalObjectKind.RecordingAssociatedStudyItem;
        if (kind == "standaloneNote")
            return CanonicalObjectKind.StandaloneNote;
        return string.IsNullOrEmpty(kind) ? CanonicalObjectKind.UnknownUnsupported : CanonicalObjectKind.StandaloneStudyItem;
    }

    public static string? SafeLogicalResourceToken(string? token) =>
        SafeLogicalPathToken(token);

    public static string? SafeLogicalPathToken(string? token)
    {
        var trimmed = token?.Trim();
        if (string.IsNullOrEmpty(trimmed)
            || trimmed.StartsWith('/')
            || trimmed.Contains("://")
            || trimmed.Contains('\\'))
            return null;

        var components = trimmed.Split('/', StringSplitOptions.None);
        if (components.Length == 0
            || components.Any(c => string.IsNullOrEmpty(c) || c == "." || c == ".."))
            return null;

        return trimmed;
    }

    public static string? LogicalName(string? token)
    {
        var safeToken = SafeLogicalPathToken(token);
        if (safeToken is null) return null;
        var parts = safeToken.Split('/');
        return parts.Length > 0 ? parts[^1] : null;
    }

    public static CanonicalArtifactProducer Producer(CanonicalArtifactKind kind, string platform)
    {
        var normalizedPlatform = (platform ?? "").Trim().ToLowerInvariant();
        switch (kind)
        {
            case CanonicalArtifactKind.Audio:
                return normalizedPlatform.Contains("iphone") ? CanonicalArtifactProducer.AudioCapture : CanonicalArtifactProducer.Unknown;
            case CanonicalArtifactKind.TranscriptJson:
            case CanonicalArtifactKind.TranscriptMarkdown:
                return normalizedPlatform.Contains("mac") ? CanonicalArtifactProducer.Transcription : CanonicalArtifactProducer.Unknown;
            case CanonicalArtifactKind.NoteMarkdown:
            case CanonicalArtifactKind.NoteJson:
            case CanonicalArtifactKind.SummaryJson:
                return normalizedPlatform.Contains("mac") ? CanonicalArtifactProducer.NoteGeneration : CanonicalArtifactProducer.Unknown;
            default:
                return CanonicalArtifactProducer.Unknown;
        }
    }

    public static CanonicalCapability? RequiredCapability(CanonicalArtifactKind kind) =>
        kind switch
        {
            CanonicalArtifactKind.Audio => CanonicalCapability.AudioArtifact,
            CanonicalArtifactKind.TranscriptJson => CanonicalCapability.TranscriptArtifact,
            CanonicalArtifactKind.TranscriptMarkdown => CanonicalCapability.TranscriptArtifact,
            CanonicalArtifactKind.NoteMarkdown => CanonicalCapability.NoteArtifact,
            CanonicalArtifactKind.NoteJson => CanonicalCapability.NoteArtifact,
            CanonicalArtifactKind.SummaryJson => CanonicalCapability.SummaryArtifact,
            _ => null
        };

    public static CanonicalArtifactAvailability Availability(bool isPresent, CanonicalHash? contentHash, long? byteSize)
    {
        if (!isPresent) return CanonicalArtifactAvailability.Missing;
        return contentHash is not null && byteSize is not null
            ? CanonicalArtifactAvailability.Available
            : CanonicalArtifactAvailability.AvailableWithoutHash;
    }

    public static CanonicalArtifact MakeArtifact(
        string objectId,
        CanonicalArtifactKind kind,
        CanonicalArtifactAvailability availability,
        CanonicalHash? contentHash = null,
        long? byteSize = null,
        string? logicalPathToken = null,
        CanonicalTimestamp? modifiedAt = null,
        CanonicalTimestamp? observedAt = null,
        string? producedByNodeId = null,
        string platform = "")
    {
        var safeToken = SafeLogicalPathToken(logicalPathToken);
        var producer = Producer(kind, platform);
        return new CanonicalArtifact(
            artifactId: ArtifactId(objectId, kind),
            objectId: objectId,
            kind: kind,
            availability: availability,
            contentHash: contentHash,
            byteSize: byteSize,
            logicalName: LogicalName(safeToken),
            logicalPathToken: safeToken,
            modifiedAt: modifiedAt,
            observedAt: observedAt,
            producedBy: producer == CanonicalArtifactProducer.Unknown ? null : producer,
            producedByNodeId: producedByNodeId
        );
    }

    public static bool ProvesGeneratedArtifactAvailability(CanonicalArtifact? artifact)
    {
        if (artifact is null) return false;
        if (!GeneratedArtifactKinds.Contains(artifact.Kind)) return false;
        if (artifact.Availability != CanonicalArtifactAvailability.Available) return false;
        if (artifact.ContentHash is null) return false;
        if (artifact.ByteSize is null) return false;
        if (artifact.Tombstone == true) return false;
        return true;
    }

    public static bool SameContent(CanonicalArtifact left, CanonicalArtifact right) =>
        left.ContentHash?.Algorithm == right.ContentHash?.Algorithm
        && left.ContentHash?.Value == right.ContentHash?.Value
        && left.ByteSize == right.ByteSize
        && left.ContentHash is not null
        && left.ByteSize is not null;

    public static bool IsAuthoritativeProducer(CanonicalArtifact artifact, CanonicalNode node)
    {
        if (artifact.Tombstone == true) return false;
        var requiredCap = RequiredCapability(artifact.Kind);
        if (requiredCap is null) return false;
        if (!node.Capabilities.Contains(requiredCap.Value)) return false;
        if (artifact.ProducedByNodeId is not null && artifact.ProducedByNodeId != node.NodeId)
            return false;

        switch (artifact.Kind)
        {
            case CanonicalArtifactKind.Audio:
                return artifact.ProducedBy == CanonicalArtifactProducer.AudioCapture
                    && node.Platform.ToLowerInvariant().Contains("iphone");
            case CanonicalArtifactKind.TranscriptJson:
            case CanonicalArtifactKind.TranscriptMarkdown:
                return artifact.ProducedBy == CanonicalArtifactProducer.Transcription
                    && node.Platform.ToLowerInvariant().Contains("mac");
            case CanonicalArtifactKind.NoteMarkdown:
            case CanonicalArtifactKind.NoteJson:
            case CanonicalArtifactKind.SummaryJson:
                return artifact.ProducedBy == CanonicalArtifactProducer.NoteGeneration
                    && node.Platform.ToLowerInvariant().Contains("mac");
            default:
                return false;
        }
    }

    internal static string TimestampString(CanonicalTimestamp timestamp)
    {
        var seconds = (timestamp.Date.ToUniversalTime().Ticks - DateTime.UnixEpoch.Ticks) / (double)TimeSpan.TicksPerSecond;
        return seconds.ToString("F6", CultureInfo.InvariantCulture);
    }

    private static string NormalizedRequired(string value, string fallback) =>
        StringUtil.NilIfEmpty(value?.Trim()) ?? fallback;
}
