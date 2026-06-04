namespace Rokurics.Models;

/// <summary>
/// Device connection status and sync state models.
/// Mirrors connection/sync state from source.
/// </summary>

public sealed class DeviceConnectionStatus
{
    public string DeviceId { get; set; }
    public string DisplayName { get; set; }
    public string State { get; set; }          // unpaired, connecting, connected, offline
    public string? PresenceState { get; set; } // online, stale, disconnected, connecting, securityError, unknown
    public string? MonitoringMode { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime? LastHeartbeatSentAt { get; set; }
    public DateTime? LastHeartbeatReceivedAt { get; set; }
    public DateTime? LastSuccessfulHeartbeatAt { get; set; }
    public DateTime? LastSignedRequestSucceededAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncStatus { get; set; }
    public int? MissedHeartbeatCount { get; set; }
    public int? ConsecutiveFailureCount { get; set; }
    public double? LatencyMilliseconds { get; set; }
    public string? LastError { get; set; }
    public string? LastErrorCode { get; set; }
    public int? ConnectionStatusRevision { get; set; }

    public DeviceConnectionStatus()
    {
        DeviceId = "";
        DisplayName = "";
        State = "unpaired";
    }

    public static DeviceConnectionStatus Unpaired(string displayName = "Mac") => new()
    {
        DeviceId = "",
        DisplayName = displayName,
        State = "unpaired"
    };
}

public sealed class StudyLibrarySyncState
{
    public string? DeviceId { get; set; }
    public DateTime? LastPulledAt { get; set; }
    public DateTime? LastPushedAt { get; set; }
    public DateTime? LastSuccessfulSyncAt { get; set; }
    public string? LastRemoteManifestHash { get; set; }
    public string? LastKnownRemoteCommitId { get; set; }
    public int PendingLocalChanges { get; set; }
    public int PendingUploads { get; set; }
    public int FailedChanges { get; set; }
    public string? LastError { get; set; }

    public static readonly StudyLibrarySyncState Empty = new();
}

public sealed class LocalNetworkSyncState
{
    public int Version { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime? LastSuccessfulSyncAt { get; set; }
    public string? LastPeerDeviceId { get; set; }
    public string? LastLocalInventoryHash { get; set; }
    public string? LastPeerInventoryHash { get; set; }
    public string? LastAppliedPeerRevision { get; set; }
    public int ConsecutiveFailureCount { get; set; }
    public DateTime? NextAllowedSyncAt { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }
    public int PendingUploadCount { get; set; }
    public int PendingDownloadCount { get; set; }

    public static readonly LocalNetworkSyncState Empty = new();
    public const int CurrentVersion = 1;
}

public sealed class StudyBrowserPath : IEquatable<StudyBrowserPath>
{
    public List<string> Components { get; }

    public StudyBrowserPath(List<string>? components = null)
    {
        Components = components ?? new List<string>();
    }

    public bool IsRoot => Components.Count == 0;
    public int Depth => Components.Count;
    public string StorageKey => string.Join("", Components);

    public StudyBrowserPath Parent =>
        Components.Count == 0 ? this : new StudyBrowserPath(Components.Take(Components.Count - 1).ToList());

    public StudyBrowserPath Appending(string value)
    {
        var updated = new List<string>(Components) { value };
        return new StudyBrowserPath(updated);
    }

    public bool Equals(StudyBrowserPath? other)
    {
        if (other is null) return false;
        return Components.SequenceEqual(other.Components);
    }

    public override bool Equals(object? obj) => Equals(obj as StudyBrowserPath);
    public override int GetHashCode() => Components.Aggregate(0, HashCode.Combine);
}

public sealed class StudyBrowserContent
{
    public StudyBrowserPath Path { get; init; }
    public List<StudyBrowserFolder> Folders { get; init; }
    public List<StudyItemMetadata> Items { get; init; }

    public StudyBrowserContent()
    {
        Path = new StudyBrowserPath();
        Folders = new List<StudyBrowserFolder>();
        Items = new List<StudyItemMetadata>();
    }
}

public sealed class StudyBrowserFolder
{
    public string Id { get; set; }
    public string? FolderId { get; set; }
    public string LevelKey { get; set; }
    public string Title { get; set; }
    public int ItemCount { get; set; }
    public StudyBrowserPath Path { get; set; }
    public StudyFolderColorToken? ColorToken { get; set; }

    public StudyBrowserFolder()
    {
        Id = "";
        LevelKey = "";
        Title = "";
        Path = new StudyBrowserPath();
    }
}

public enum StudyLibrarySyncEntityKind
{
    Item,
    Folder
}

public enum StudyLibrarySyncOperation
{
    Trash,
    Delete,
    DeleteMetadataOnly
}

public sealed class StudyLibrarySyncTombstone
{
    public string Id { get; set; }
    public StudyLibrarySyncEntityKind EntityKind { get; set; }
    public string EntityId { get; set; }
    public StudyLibrarySyncOperation Operation { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ModifiedByDeviceId { get; set; }

    public StudyLibrarySyncTombstone()
    {
        Id = "";
        EntityId = "";
    }
}

public sealed class StudyLibrarySyncManifest
{
    public string DeviceId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<StudyItemMetadata> Items { get; set; }
    public List<StudyFolderMetadata> Folders { get; set; }
    public List<StudyLibrarySyncTombstone> Tombstones { get; set; }
    public string Checksum { get; set; }

    public bool HasValidChecksum => !string.IsNullOrWhiteSpace(Checksum);

    public StudyLibrarySyncManifest()
    {
        DeviceId = "";
        Items = new List<StudyItemMetadata>();
        Folders = new List<StudyFolderMetadata>();
        Tombstones = new List<StudyLibrarySyncTombstone>();
        Checksum = "";
    }

    public static StudyLibrarySyncManifest Make(string deviceId, DateTime generatedAt,
        List<StudyItemMetadata> items, List<StudyFolderMetadata> folders,
        List<StudyLibrarySyncTombstone> tombstones, List<PendingRecordingUpload> pendingUploads)
    {
        var manifest = new StudyLibrarySyncManifest
        {
            DeviceId = deviceId,
            GeneratedAt = generatedAt,
            Items = items,
            Folders = folders,
            Tombstones = tombstones
        };
        manifest.Checksum = ComputeChecksum(manifest);
        return manifest;
    }

    public static string ComputeChecksum(StudyLibrarySyncManifest manifest)
    {
        var raw = $"{manifest.DeviceId}|{manifest.Items.Count}|{manifest.Folders.Count}|{manifest.Tombstones.Count}";
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class PendingRecordingUpload
{
    public string ItemId { get; set; }
    public string RecordingId { get; set; }
    public string? LocalAudioRelativePath { get; set; }
    public string? TargetDeviceId { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PendingRecordingUpload()
    {
        ItemId = "";
        RecordingId = "";
        Status = "pending";
    }
}

public static class StudyLibraryBrowser
{
    public static readonly string[] LevelKeys = { "type", "subject", "chapter", "topic" };

    public static StudyBrowserContent Browse(
        List<StudyItemMetadata> items, List<StudyFolderMetadata> folders, StudyBrowserPath path)
    {
        var matchingItems = items.Where(i => ItemMatches(i, path) && !i.IsTrashed).ToList();

        if (path.Depth >= LevelKeys.Length)
            return new StudyBrowserContent { Path = path, Items = SortedItems(matchingItems) };

        var nextLevelKey = LevelKeys[path.Depth];
        var grouped = matchingItems.GroupBy(i => DisplayValue(i, nextLevelKey));
        var browseFolders = new Dictionary<string, StudyBrowserFolder>();

        foreach (var group in grouped)
        {
            var folderPath = path.Appending(group.Key);
            browseFolders[folderPath.StorageKey] = new StudyBrowserFolder
            {
                Id = $"{string.Join("/", path.Components)}/{nextLevelKey}={group.Key}",
                LevelKey = nextLevelKey,
                Title = group.Key,
                ItemCount = group.Count(),
                Path = folderPath
            };
        }

        return new StudyBrowserContent
        {
            Path = path,
            Folders = browseFolders.Values.OrderBy(f => f.Title).ToList(),
            Items = new List<StudyItemMetadata>()
        };
    }

    public static bool ItemMatches(StudyItemMetadata item, StudyBrowserPath path)
    {
        if (path.Depth > LevelKeys.Length) return false;
        for (int i = 0; i < path.Components.Count; i++)
            if (DisplayValue(item, LevelKeys[i]) != path.Components[i])
                return false;
        return true;
    }

    private static string DisplayValue(StudyItemMetadata item, string levelKey)
    {
        var val = item.FilingPath.ValueFor(levelKey);
        if (!string.IsNullOrWhiteSpace(val)) return val;
        return StudyTag.NormalizedNamespace(levelKey) == "type"
            ? StudyHierarchyRule.UncategorizedValue
            : StudyHierarchyRule.MissingValue;
    }

    private static List<StudyItemMetadata> SortedItems(List<StudyItemMetadata> items)
    {
        return items
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
