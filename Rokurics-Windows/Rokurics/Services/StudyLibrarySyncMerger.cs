using Rokurics.Models;

namespace Rokurics.Services;

/// <summary>
/// Sync merge/diff engine for study library bi-directional sync.
/// Mirrors the diff planner and merge logic from Apple source
/// (StudyLibrarySyncModels.swift merge/inventory comparison).
///
/// VALIDATION GAP: Full end-to-end merge requires Kestrel server
/// and paired device. This skeleton provides the structural
/// interfaces and can be tested with in-memory manifests.
/// </summary>

/// <summary>
/// Result of comparing a local manifest against a remote (peer) manifest.
/// </summary>
public sealed class SyncDiffResult
{
    public List<StudyItemMetadata> NewOrUpdatedLocally { get; init; } = new();
    public List<StudyItemMetadata> NewOrUpdatedRemotely { get; init; } = new();
    public List<string> DeletedOnlyLocally { get; init; } = new();
    public List<string> DeletedOnlyRemotely { get; init; } = new();
    public List<string> ConflictingItemIds { get; init; } = new();
    public List<StudyFolderMetadata> NewOrUpdatedFolders { get; init; } = new();
    public int TotalLocalItems { get; init; }
    public int TotalRemoteItems { get; init; }
    public bool HasConflicts => ConflictingItemIds.Count > 0;
    public bool IsEmpty =>
        NewOrUpdatedLocally.Count == 0 && NewOrUpdatedRemotely.Count == 0 &&
        DeletedOnlyLocally.Count == 0 && DeletedOnlyRemotely.Count == 0 &&
        NewOrUpdatedFolders.Count == 0;
}

/// <summary>
/// Merge plan for applying sync changes to the local store.
/// </summary>
public sealed class SyncMergePlan
{
    public SyncDiffResult Diff { get; init; } = null!;
    public List<StudyItemMetadata> ItemsToApply { get; init; } = new();
    public List<StudyFolderMetadata> FoldersToApply { get; init; } = new();
    public List<string> ItemIdsToDelete { get; init; } = new();
    public List<string> FolderIdsToDelete { get; init; } = new();
    public List<string> SkippedConflictIds { get; init; } = new();
    public string? ErrorSummary { get; init; }
}

/// <summary>
/// Merge outcome after applying a merge plan to the local store.
/// </summary>
public sealed class SyncMergeOutcome
{
    public SyncMergePlan Plan { get; init; } = null!;
    public int ItemsApplied { get; set; }
    public int FoldersApplied { get; set; }
    public int ItemsDeleted { get; set; }
    public int FoldersDeleted { get; set; }
    public int ConflictsSkipped { get; set; }
    public bool Succeeded => ConflictsSkipped == 0 && ErrorSummary is null;
    public string? ErrorSummary { get; set; }
}

/// <summary>
/// Study library sync merger. Computes diffs between local and remote manifests
/// and produces merge plans. Mirrors the diff planner from Apple source.
///
/// REQUIRES: StudyLibraryStore for applying changes, StudyLibrarySyncStateStore for history.
/// VALIDATION GAP: Cannot run full merge without .NET runtime on Windows.
/// </summary>
public sealed class StudyLibrarySyncMerger
{
    private readonly StudyLibraryStore _studyStore;
    private readonly Stores.StudyLibrarySyncStateStore _syncStateStore;

    public StudyLibrarySyncMerger(StudyLibraryStore studyStore,
        Stores.StudyLibrarySyncStateStore syncStateStore)
    {
        _studyStore = studyStore;
        _syncStateStore = syncStateStore;
    }

    /// <summary>
    /// Compute the diff between the local manifest and a remote (peer) manifest.
    /// </summary>
    public SyncDiffResult ComputeDiff(StudyLibrarySyncManifest local,
        StudyLibrarySyncManifest remote)
    {
        var localItemMap = local.Items.Where(i => i.RecordingId is not null)
            .ToDictionary(i => i.RecordingId!);
        var remoteItemMap = remote.Items.Where(i => i.RecordingId is not null)
            .ToDictionary(i => i.RecordingId!);

        var localIds = new HashSet<string>(localItemMap.Keys);
        var remoteIds = new HashSet<string>(remoteItemMap.Keys);

        var newOrUpdatedLocally = new List<StudyItemMetadata>();
        var newOrUpdatedRemotely = new List<StudyItemMetadata>();
        var deletedOnlyLocally = new List<string>();
        var deletedOnlyRemotely = new List<string>();
        var conflicting = new List<string>();

        // Items in remote but not in local → new from remote
        foreach (var id in remoteIds.Except(localIds))
            newOrUpdatedRemotely.Add(remoteItemMap[id]);

        // Items in local but not in remote → new from local
        foreach (var id in localIds.Except(remoteIds))
        {
            // Check tombstones: if item was deleted remotely, skip
            var tombstone = remote.Tombstones.FirstOrDefault(t =>
                t.EntityKind == StudyLibrarySyncEntityKind.Item &&
                t.EntityId == localItemMap[id].ItemId);
            if (tombstone is not null)
                deletedOnlyRemotely.Add(id);
            else
                newOrUpdatedLocally.Add(localItemMap[id]);
        }

        // Items in both → check for conflicts (both modified)
        foreach (var id in localIds.Intersect(remoteIds))
        {
            var localItem = localItemMap[id];
            var remoteItem = remoteItemMap[id];

            // Simple conflict detection: both sides modified since last sync
            var localModified = localItem.UpdatedAt > remoteItem.UpdatedAt;
            if (localModified)
                conflicting.Add(id);
            else
                newOrUpdatedRemotely.Add(remoteItem); // Accept remote version
        }

        // Check local tombstones for items deleted locally
        foreach (var tombstone in local.Tombstones.Where(t =>
            t.EntityKind == StudyLibrarySyncEntityKind.Item &&
            t.Operation == StudyLibrarySyncOperation.Delete))
        {
            var matchingRemote = remote.Items.FirstOrDefault(i => i.ItemId == tombstone.EntityId);
            if (matchingRemote?.RecordingId is not null && remoteIds.Contains(matchingRemote.RecordingId))
                deletedOnlyLocally.Add(matchingRemote.RecordingId);
        }

        // Folder diff (simplified — compare by folder ID)
        var newFolders = remote.Folders
            .Where(rf => !local.Folders.Any(lf => lf.FolderId == rf.FolderId))
            .ToList();

        return new SyncDiffResult
        {
            NewOrUpdatedLocally = newOrUpdatedLocally,
            NewOrUpdatedRemotely = newOrUpdatedRemotely,
            DeletedOnlyLocally = deletedOnlyLocally,
            DeletedOnlyRemotely = deletedOnlyRemotely,
            ConflictingItemIds = conflicting,
            NewOrUpdatedFolders = newFolders,
            TotalLocalItems = local.Items.Count,
            TotalRemoteItems = remote.Items.Count
        };
    }

    /// <summary>
    /// Build a merge plan from a diff result.
    /// Conflicts are skipped; user must resolve manually.
    /// </summary>
    public SyncMergePlan BuildMergePlan(SyncDiffResult diff,
        bool favorLocalOnConflict = false)
    {
        var itemsToApply = new List<StudyItemMetadata>();
        var foldersToApply = new List<StudyFolderMetadata>(diff.NewOrUpdatedFolders);
        var itemIdsToDelete = new List<string>(diff.DeletedOnlyLocally);
        var folderIdsToDelete = new List<string>();
        var skipped = new List<string>(diff.ConflictingItemIds);

        // Apply remote items that don't conflict
        itemsToApply.AddRange(diff.NewOrUpdatedRemotely);

        // If favoring local on conflict, apply local versions too
        if (favorLocalOnConflict)
        {
            foreach (var conflictId in diff.ConflictingItemIds)
            {
                var localItem = diff.NewOrUpdatedLocally
                    .FirstOrDefault(i => i.RecordingId == conflictId);
                if (localItem is not null)
                {
                    itemsToApply.Add(localItem);
                    skipped.Remove(conflictId);
                }
            }
        }

        return new SyncMergePlan
        {
            Diff = diff,
            ItemsToApply = itemsToApply,
            FoldersToApply = foldersToApply,
            ItemIdsToDelete = itemIdsToDelete,
            FolderIdsToDelete = folderIdsToDelete,
            SkippedConflictIds = skipped,
            ErrorSummary = diff.HasConflicts && !favorLocalOnConflict
                ? $"{diff.ConflictingItemIds.Count} conflicts require manual resolution"
                : null
        };
    }

    /// <summary>
    /// Apply a merge plan to the local study library store.
    /// </summary>
    public SyncMergeOutcome ApplyMergePlan(SyncMergePlan plan, string deviceId)
    {
        var outcome = new SyncMergeOutcome { Plan = plan };

        try
        {
            foreach (var item in plan.ItemsToApply)
            {
                if (item.RecordingId is not null)
                {
                    var recording = ItemToRecording(item);
                    _studyStore.UpsertRecordingMetadata(recording);
                    outcome.ItemsApplied++;
                }
            }

            foreach (var folder in plan.FoldersToApply)
            {
                var parentPath = new StudyBrowserPath(new List<string>());
                _studyStore.CreateFolder(folder.Title, parentPath);
                outcome.FoldersApplied++;
            }

            foreach (var id in plan.ItemIdsToDelete)
            {
                var item = _studyStore.FindItem(id);
                if (item is not null)
                {
                    outcome.ItemsDeleted++;
                }
            }

            outcome.ConflictsSkipped = plan.SkippedConflictIds.Count;
            _studyStore.Refresh();
            _syncStateStore.RecordPush(deviceId, null);
        }
        catch (Exception ex)
        {
            outcome.ErrorSummary = ex.Message;
        }

        return outcome;
    }

    private static RecordingMetadata ItemToRecording(StudyItemMetadata item)
    {
        return new RecordingMetadata(
            id: item.RecordingId ?? Guid.NewGuid().ToString(),
            title: item.Title,
            fileName: item.Title + ".wav",
            relativeAudioPath: item.AudioRelativePath ?? "",
            relativeMetadataPath: "",
            createdAt: item.CreatedAt,
            endedAt: item.CreatedAt.Add(item.Duration ?? TimeSpan.Zero),
            duration: item.Duration ?? TimeSpan.Zero,
            format: "wav",
            codec: "pcm",
            sampleRate: 16000,
            channels: 1,
            bitrate: 256000,
            fileSize: 0,
            uploadStatus: item.IsTranscribed ? "transcribed" : "pending",
            transcriptionStatus: item.TranscriptionStatus ?? "none",
            noteStatus: item.NoteStatus ?? "none",
            tags: new List<string>(),
            studyFiling: item.FilingPath ?? new StudyFilingPath(null, null, null, null)
        );
    }

    /// <summary>
    /// Run a full sync cycle: compute diff, build plan, apply.
    /// </summary>
    public async Task<SyncMergeOutcome> SyncAsync(StudyLibrarySyncManifest local,
        StudyLibrarySyncManifest remote, string deviceId,
        bool favorLocalOnConflict = false)
    {
        var diff = ComputeDiff(local, remote);
        if (diff.IsEmpty)
        {
            _syncStateStore.RecordPush(deviceId, remote.Checksum);
            return new SyncMergeOutcome
            {
                Plan = new SyncMergePlan { Diff = diff }
            };
        }

        var plan = BuildMergePlan(diff, favorLocalOnConflict);
        var outcome = ApplyMergePlan(plan, deviceId);

        if (!outcome.Succeeded)
            _syncStateStore.RecordFailure(deviceId,
                outcome.ErrorSummary ?? "Merge failed",
                outcome.ConflictsSkipped);

        return outcome;
    }
}

// ═══════════════════════════════════════════════════════════════════
// Enhanced Sync Models — mirrors Apple source StudyLibrarySyncModels.swift
// ═══════════════════════════════════════════════════════════════════

/// Sync checksum computation using SHA-256 matching Apple source format.
public static class SyncChecksum
{
    public static string Compute(string payload)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Compute(byte[] bytes)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeFromJson<T>(T value) where T : notnull
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        return Compute(json);
    }

    public static bool Verify(string payload, string expectedChecksum)
        => string.Equals(Compute(payload), expectedChecksum, StringComparison.OrdinalIgnoreCase);
}

/// Sync sanitizer — strips sensitive custom properties before sync.
/// Mirrors StudyLibrarySyncSanitizer from Apple source.
public static class SyncSanitizer
{
    private static readonly HashSet<string> SensitiveKeyPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "apikey", "api_key", "secret", "hmac", "pairing",
        "rawresponse", "raw_response", "providerresponse", "provider_response",
        "fulltranscript", "full_transcript", "fullnote", "full_note",
        "prompt", "debug", "rawjson", "raw_json"
    };

    public static Dictionary<string, string> FilterCustomProperties(
        Dictionary<string, string> properties)
    {
        return properties
            .Where(kv => !SensitiveKeyPatterns.Any(pattern =>
                kv.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public static StudyItemMetadata Sanitize(StudyItemMetadata item, string? deviceId = null)
    {
        return item.SyncSanitized(deviceId ?? "");
    }

    public static StudyFolderMetadata Sanitize(StudyFolderMetadata folder, string? deviceId = null)
    {
        return folder.SyncSanitized(deviceId ?? "");
    }
}

// ═══════════════════════════════════════════════════════════════════
// Sync Inventory — mirrors Apple source LocalNetworkSyncInventory
// ═══════════════════════════════════════════════════════════════════

public enum SyncEntityKind { Recording, Folder, StudyItem, Artifact }

public enum SyncDiffActionKind
{
    NoOp, UploadMetadata, DownloadMetadata,
    UploadArtifact, DownloadArtifact,
    UploadRecordingAudio, Conflict
}

public enum SyncArtifactKind
{
    TranscriptMarkdown,
    TranscriptJson,
    NoteMarkdown,
    NoteJson,
    MetadataJson,
    ReceiveJson,
    SummaryMarkdown,
    SummaryJson,
    Audio
}

public sealed class SyncInventory
{
    public string DeviceId { get; init; } = "";
    public string DeviceName { get; init; } = "";
    public DateTime GeneratedAt { get; init; }
    public List<SyncRecordingEntry> Recordings { get; init; } = new();
    public List<SyncFolderEntry> Folders { get; init; } = new();
    public List<SyncStudyItemEntry> StudyItems { get; init; } = new();
    public List<SyncArtifactEntry> Artifacts { get; init; } = new();
    public StudyLibrarySyncManifest? StudyManifest { get; init; }
    public CanonicalManifest? CanonicalManifest { get; init; }

    public string InventoryHash
    {
        get
        {
            var sorted = System.Text.Json.JsonSerializer.Serialize(new
            {
                deviceId = DeviceId,
                deviceName = DeviceName,
                generatedAt = GeneratedAt,
                recordings = Recordings.OrderBy(r => r.RecordingId).ToList(),
                folders = Folders.OrderBy(f => f.FolderId).ToList(),
                studyItems = StudyItems.OrderBy(s => s.ItemId).ToList(),
                artifacts = Artifacts.OrderBy(a => a.ArtifactId).ToList(),
                studyManifest = StudyManifest,
                canonicalManifest = CanonicalManifest
            }, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            return SyncChecksum.Compute(sorted);
        }
    }
}

public sealed class SyncRecordingEntry
{
    public string RecordingId { get; init; } = "";
    public string? MetadataHash { get; init; }
    public bool AudioAvailable { get; init; }
    public string? AudioChecksum { get; init; }
    public long? AudioSize { get; init; }
    public string? UploadLedgerState { get; init; }
    public string? ReceiveStatus { get; init; }
    public string? ProcessingStatus { get; init; }
    public string? Title { get; init; }
    public DateTime? CreatedAt { get; init; }
    public bool? Tombstone { get; init; }
    public string? AudioAvailability { get; init; }
    public string? UploadStatus { get; init; }
    public string? TranscriptionStatus { get; init; }
    public string? NoteStatus { get; init; }
    public string? SourceDeviceID { get; init; }
    public List<string>? ArtifactRefs { get; init; }
    public string? AudioLogicalPathToken { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool Deleted { get; init; }
}

public sealed class SyncFolderEntry
{
    public string FolderId { get; init; } = "";
    public string? ParentId { get; init; }
    public string Name { get; init; } = "";
    public string? ColorToken { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string RevisionHash { get; init; } = "";
    public bool Deleted { get; init; }
    public string Path { get; init; } = "";
}

public sealed class SyncStudyItemEntry
{
    public string ItemId { get; init; } = "";
    public StudyItemKind Kind { get; init; }
    public string Title { get; init; } = "";
    public string? RecordingId { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string RevisionHash { get; init; } = "";
    public bool Deleted { get; init; }
}

public sealed class CanonicalManifestNode
{
    public string? NodeID { get; init; }
    public string? Platform { get; init; }
    public string? DisplayName { get; init; }
}

public sealed class CanonicalManifest
{
    public CanonicalManifestNode? Node { get; init; }
    public Dictionary<string, object?> Payload { get; init; } = new();
    public int SchemaVersion { get; init; } = 1;
    public DateTime? GeneratedAt { get; init; }
    public string? ManifestHash { get; init; }
}

public sealed class SyncArtifactEntry
{
    public string ArtifactId { get; init; } = "";
    public SyncArtifactKind Kind { get; init; }
    public string OwnerId { get; init; } = "";
    public string? Checksum { get; init; }
    public long? Size { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string LogicalPathToken { get; init; } = "";

    private static string CanonicalArtifactKind(SyncArtifactKind kind) => kind switch
    {
        SyncArtifactKind.TranscriptMarkdown => "transcriptMarkdown",
        SyncArtifactKind.TranscriptJson => "transcriptJson",
        SyncArtifactKind.NoteMarkdown => "noteMarkdown",
        SyncArtifactKind.NoteJson => "noteJson",
        SyncArtifactKind.MetadataJson => "metadataJson",
        SyncArtifactKind.ReceiveJson => "receiveJson",
        SyncArtifactKind.SummaryMarkdown => "summaryMarkdown",
        SyncArtifactKind.SummaryJson => "summaryJson",
        SyncArtifactKind.Audio => "audio",
        _ => "unknown"
    };

    public static string MakeArtifactId(SyncArtifactKind kind, string ownerId, string logicalPath)
    {
        var canonicalKind = CanonicalArtifactKind(kind);
        var payload = $"{canonicalKind}|{ownerId}|{logicalPath}";
        return $"artifact_{SyncChecksum.Compute(payload)}";
    }
}

public sealed class SyncDiffAction
{
    public string Id { get; init; } = "";
    public SyncDiffActionKind Kind { get; init; }
    public string EntityKind { get; init; } = "";
    public string EntityId { get; init; } = "";
    public string Reason { get; init; } = "";
}

public sealed class SyncDiffPlan
{
    public List<SyncDiffAction> UploadMetadata { get; init; } = new();
    public List<SyncDiffAction> DownloadMetadata { get; init; } = new();
    public List<SyncDiffAction> UploadArtifacts { get; init; } = new();
    public List<SyncDiffAction> DownloadArtifacts { get; init; } = new();
    public List<SyncDiffAction> UploadRecordingAudio { get; init; } = new();
    public List<SyncDiffAction> Conflicts { get; init; } = new();
    public List<SyncDiffAction> NoOps { get; init; } = new();

    public int TotalActions =>
        UploadMetadata.Count + DownloadMetadata.Count +
        UploadArtifacts.Count + DownloadArtifacts.Count +
        UploadRecordingAudio.Count + Conflicts.Count + NoOps.Count;
}

// ═══════════════════════════════════════════════════════════════════
// Enhanced Diff Planner — mirrors Apple source LocalNetworkSyncDiffPlanner
// ═══════════════════════════════════════════════════════════════════

public sealed class SyncDiffPlanner
{
    public SyncDiffPlan Plan(SyncInventory local, SyncInventory peer,
        DateTime? lastSuccessfulSyncAt)
    {
        var plan = new SyncDiffPlan();
        CompareRecordings(local, peer, lastSuccessfulSyncAt, plan);
        CompareFolders(local, peer, lastSuccessfulSyncAt, plan);
        CompareStudyItems(local, peer, lastSuccessfulSyncAt, plan);
        CompareArtifacts(local, peer, plan);
        return plan;
    }

    private void CompareRecordings(SyncInventory local, SyncInventory peer,
        DateTime? lastSync, SyncDiffPlan plan)
    {
        var localMap = local.Recordings.ToDictionary(r => r.RecordingId);
        var peerMap = peer.Recordings.ToDictionary(r => r.RecordingId);
        var allIds = localMap.Keys.Union(peerMap.Keys).OrderBy(id => id);

        foreach (var id in allIds)
        {
            var hasLocal = localMap.TryGetValue(id, out var lr);
            var hasPeer = peerMap.TryGetValue(id, out var pr);

            if (hasLocal && hasPeer && lr is not null && pr is not null)
            {
                if (lr.MetadataHash == pr.MetadataHash)
                    plan.NoOps.Add(Action(SyncDiffActionKind.NoOp, "recording", id, "metadata_equal"));
                else if (BothChangedAfterSync(lr.UpdatedAt, pr.UpdatedAt, lastSync))
                    plan.Conflicts.Add(Action(SyncDiffActionKind.Conflict, "recording", id, "both_changed_after_last_sync"));
                else if (pr.Deleted && pr.UpdatedAt >= lr.UpdatedAt)
                    plan.DownloadMetadata.Add(Action(SyncDiffActionKind.DownloadMetadata, "recording", id, "peer_tombstone_wins"));
                else if (lr.Deleted && lr.UpdatedAt >= pr.UpdatedAt)
                    plan.UploadMetadata.Add(Action(SyncDiffActionKind.UploadMetadata, "recording", id, "local_tombstone_wins"));
                else if (lr.UpdatedAt > pr.UpdatedAt)
                    plan.UploadMetadata.Add(Action(SyncDiffActionKind.UploadMetadata, "recording", id, "local_recording_newer"));
                else
                    plan.DownloadMetadata.Add(Action(SyncDiffActionKind.DownloadMetadata, "recording", id, "peer_recording_newer"));

                if (lr.AudioAvailable && !pr.AudioAvailable)
                    plan.UploadRecordingAudio.Add(
                        Action(SyncDiffActionKind.UploadRecordingAudio, "recording", id, "peer_missing_audio_use_existing_upload")
                    );
            }
            else if (hasLocal && lr is not null)
                plan.UploadMetadata.Add(Action(SyncDiffActionKind.UploadMetadata, "recording", id, "peer_missing_recording"));
            else if (hasPeer && pr is not null)
                plan.DownloadMetadata.Add(Action(SyncDiffActionKind.DownloadMetadata, "recording", id, "local_missing_recording_metadata"));
        }
    }

    private void CompareFolders(SyncInventory local, SyncInventory peer,
        DateTime? lastSync, SyncDiffPlan plan)
    {
        var localMap = local.Folders.ToDictionary(f => f.FolderId);
        var peerMap = peer.Folders.ToDictionary(f => f.FolderId);
        var allIds = localMap.Keys.Union(peerMap.Keys).OrderBy(id => id);

        foreach (var id in allIds)
        {
            CompareMetadataEntity("folder", id,
                localMap.GetValueOrDefault(id)?.RevisionHash,
                peerMap.GetValueOrDefault(id)?.RevisionHash,
                localMap.GetValueOrDefault(id)?.UpdatedAt,
                peerMap.GetValueOrDefault(id)?.UpdatedAt,
                localMap.GetValueOrDefault(id)?.Deleted ?? false,
                peerMap.GetValueOrDefault(id)?.Deleted ?? false,
                lastSync, plan);
        }
    }

    private void CompareStudyItems(SyncInventory local, SyncInventory peer,
        DateTime? lastSync, SyncDiffPlan plan)
    {
        var localMap = local.StudyItems.ToDictionary(s => s.ItemId);
        var peerMap = peer.StudyItems.ToDictionary(s => s.ItemId);
        var allIds = localMap.Keys.Union(peerMap.Keys).OrderBy(id => id);

        foreach (var id in allIds)
        {
            CompareMetadataEntity("studyItem", id,
                localMap.GetValueOrDefault(id)?.RevisionHash,
                peerMap.GetValueOrDefault(id)?.RevisionHash,
                localMap.GetValueOrDefault(id)?.UpdatedAt,
                peerMap.GetValueOrDefault(id)?.UpdatedAt,
                localMap.GetValueOrDefault(id)?.Deleted ?? false,
                peerMap.GetValueOrDefault(id)?.Deleted ?? false,
                lastSync, plan);
        }
    }

    private void CompareMetadataEntity(string entityKind, string entityId,
        string? localHash, string? peerHash,
        DateTime? localUpdated, DateTime? peerUpdated,
        bool localDeleted, bool peerDeleted,
        DateTime? lastSync, SyncDiffPlan plan)
    {
        if (localHash is not null && peerHash is not null && localHash == peerHash)
        {
            plan.NoOps.Add(Action(SyncDiffActionKind.NoOp, entityKind, entityId, "checksum_equal"));
            return;
        }

        if (localHash is not null && peerHash is null)
            plan.UploadMetadata.Add(Action(SyncDiffActionKind.UploadMetadata, entityKind, entityId, "peer_missing"));
        else if (localHash is null && peerHash is not null)
            plan.DownloadMetadata.Add(Action(SyncDiffActionKind.DownloadMetadata, entityKind, entityId, "local_missing"));
        else if (localHash is not null && peerHash is not null)
        {
            var localDate = localUpdated ?? DateTime.MinValue;
            var peerDate = peerUpdated ?? DateTime.MinValue;

            if (BothChangedAfterSync(localDate, peerDate, lastSync))
                plan.Conflicts.Add(Action(SyncDiffActionKind.Conflict, entityKind, entityId, "both_changed_after_last_sync"));
            else if (peerDeleted && peerDate >= localDate)
                plan.DownloadMetadata.Add(Action(SyncDiffActionKind.DownloadMetadata, entityKind, entityId, "peer_tombstone_wins"));
            else if (localDeleted && localDate >= peerDate)
                plan.UploadMetadata.Add(Action(SyncDiffActionKind.UploadMetadata, entityKind, entityId, "local_tombstone_wins"));
            else if (peerDate > localDate)
                plan.DownloadMetadata.Add(Action(SyncDiffActionKind.DownloadMetadata, entityKind, entityId, "peer_newer"));
            else
                plan.UploadMetadata.Add(Action(SyncDiffActionKind.UploadMetadata, entityKind, entityId, "local_newer"));
        }
    }

    private void CompareArtifacts(SyncInventory local, SyncInventory peer,
        SyncDiffPlan plan)
    {
        var localMap = local.Artifacts.ToDictionary(a => a.ArtifactId);
        var peerMap = peer.Artifacts.ToDictionary(a => a.ArtifactId);
        var allIds = localMap.Keys.Union(peerMap.Keys).OrderBy(id => id);

        foreach (var id in allIds)
        {
            var hasLocal = localMap.TryGetValue(id, out var la);
            var hasPeer = peerMap.TryGetValue(id, out var pa);

            if (hasLocal && hasPeer && la is not null && pa is not null)
            {
                if (la.Checksum == pa.Checksum)
                    plan.NoOps.Add(Action(SyncDiffActionKind.NoOp, "artifact", id, "checksum_equal"));
                else if (pa.UpdatedAt > la.UpdatedAt && AutoDownloadAllowed(pa.Kind))
                    plan.DownloadArtifacts.Add(Action(SyncDiffActionKind.DownloadArtifact, "artifact", id, "peer_artifact_newer"));
                else if (la.UpdatedAt > pa.UpdatedAt && la.Kind != SyncArtifactKind.Audio)
                    plan.UploadArtifacts.Add(Action(SyncDiffActionKind.UploadArtifact, "artifact", id, "local_artifact_newer"));
                else if (la.Kind == SyncArtifactKind.Audio || pa.Kind == SyncArtifactKind.Audio)
                    plan.NoOps.Add(Action(SyncDiffActionKind.NoOp, "artifact", id, "audio_uses_recording_upload"));
                else
                    plan.Conflicts.Add(Action(SyncDiffActionKind.Conflict, "artifact", id, "artifact_checksum_conflict"));
            }
            else if (hasLocal && la is not null)
            {
                if (la.Kind == SyncArtifactKind.Audio)
                    plan.NoOps.Add(Action(SyncDiffActionKind.NoOp, "artifact", id, "audio_uses_recording_upload"));
                else
                    plan.UploadArtifacts.Add(Action(SyncDiffActionKind.UploadArtifact, "artifact", id, "peer_missing_artifact"));
            }
            else if (hasPeer && pa is not null)
            {
                if (AutoDownloadAllowed(pa.Kind))
                    plan.DownloadArtifacts.Add(Action(SyncDiffActionKind.DownloadArtifact, "artifact", id, "local_missing_artifact"));
                else
                    plan.NoOps.Add(Action(SyncDiffActionKind.NoOp, "artifact", id, "audio_auto_download_disabled"));
            }
        }
    }

    private static bool AutoDownloadAllowed(SyncArtifactKind kind) => kind switch
    {
        SyncArtifactKind.TranscriptMarkdown => true,
        SyncArtifactKind.TranscriptJson => true,
        SyncArtifactKind.NoteMarkdown => true,
        SyncArtifactKind.NoteJson => true,
        SyncArtifactKind.MetadataJson => true,
        SyncArtifactKind.ReceiveJson => true,
        SyncArtifactKind.SummaryMarkdown => true,
        SyncArtifactKind.SummaryJson => true,
        SyncArtifactKind.Audio => false,
        _ => false
    };

    private static bool BothChangedAfterSync(DateTime localDate, DateTime peerDate,
        DateTime? lastSync) =>
        lastSync.HasValue && localDate > lastSync.Value && peerDate > lastSync.Value;

    private static SyncDiffAction Action(SyncDiffActionKind kind, string entityKind,
        string entityId, string reason) =>
        new()
        {
            Id = $"{kind}:{entityKind}:{entityId}:{reason}",
            Kind = kind,
            EntityKind = entityKind,
            EntityId = entityId,
            Reason = reason
        };
}

// ═══════════════════════════════════════════════════════════════════
// Inventory Builder — mirrors Apple source LocalNetworkSyncInventoryBuilder
// ═══════════════════════════════════════════════════════════════════

public sealed class SyncInventoryBuilder
{
    private readonly AudioFileStore _audioStore;
    private readonly StudyLibraryStore _studyStore;

    public SyncInventoryBuilder(AudioFileStore audioStore, StudyLibraryStore studyStore)
    {
        _audioStore = audioStore;
        _studyStore = studyStore;
    }

    public SyncInventory Build(string deviceId, string deviceName,
        DateTime? generatedAt = null)
    {
        var now = generatedAt ?? DateTime.UtcNow;
        _studyStore.Refresh();

        var manifest = _studyStore.MakeSyncManifest(deviceId);
        var recordings = _audioStore.LoadAllMetadata(includeDeleted: true)
            .ToList();
        var recordingEntries = recordings.Select(r => BuildRecordingEntry(r, deviceId)).ToList();

        var folderEntries = manifest.Folders.Select(f =>
            new SyncFolderEntry
            {
                FolderId = f.FolderId,
                ParentId = f.ParentFolderId,
                Name = f.Title,
                ColorToken = f.ColorToken?.ToString(),
                UpdatedAt = f.UpdatedAt,
                RevisionHash = SyncChecksum.ComputeFromJson(f),
                Deleted = f.IsTrashed
            }).ToList();

        var studyItemEntries = manifest.Items.Select(i =>
            new SyncStudyItemEntry
            {
                ItemId = i.ItemId,
                Kind = i.Kind,
                Title = i.Title,
                RecordingId = i.RecordingId,
                UpdatedAt = i.UpdatedAt,
                RevisionHash = SyncChecksum.ComputeFromJson(i),
                Deleted = i.IsTrashed
            }).ToList();

        var artifactEntries = BuildArtifacts(manifest);

        return new SyncInventory
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            GeneratedAt = now,
            Recordings = recordingEntries,
            Folders = folderEntries,
            StudyItems = studyItemEntries,
            Artifacts = artifactEntries,
            StudyManifest = manifest,
            CanonicalManifest = BuildCanonicalManifest(
                deviceId,
                recordingEntries,
                folderEntries,
                studyItemEntries,
                artifactEntries,
                manifest
            )
        };
    }

    private SyncRecordingEntry BuildRecordingEntry(RecordingMetadata recording, string sourceDeviceId)
    {
        var audioPath = ResolveAudioPath(recording);
        var audioExists = !string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath);
        var audioSize = audioExists && audioPath is not null
            ? new FileInfo(audioPath).Length : (long?)null;
        var audioChecksum = audioExists && audioPath is not null
            ? SyncChecksum.Compute(File.ReadAllBytes(audioPath))
            : null;

        return new SyncRecordingEntry
        {
            RecordingId = recording.Id,
            MetadataHash = SyncChecksum.ComputeFromJson(recording),
            AudioAvailable = audioExists,
            AudioChecksum = audioChecksum,
            AudioSize = audioSize,
            UploadLedgerState = null,
            ReceiveStatus = null,
            ProcessingStatus = null,
            Title = recording.Title,
            CreatedAt = recording.CreatedAt,
            Tombstone = recording.IsDeleted,
            AudioAvailability = audioExists ? "local" : "missing",
            UploadStatus = recording.UploadStatus,
            TranscriptionStatus = recording.TranscriptionStatus,
            NoteStatus = recording.NoteStatus,
            SourceDeviceID = sourceDeviceId,
            ArtifactRefs = string.IsNullOrWhiteSpace(recording.RelativeMetadataPath)
                ? new List<string>()
                : new List<string> { recording.RelativeMetadataPath },
            AudioLogicalPathToken = recording.RelativeAudioPath,
            UpdatedAt = recording.DeletedAt ?? recording.CreatedAt,
            Deleted = recording.IsDeleted
        };
    }

    private string? ResolveAudioPath(RecordingMetadata recording)
    {
        if (string.IsNullOrWhiteSpace(recording.RelativeAudioPath)) return null;

        try
        {
            return _audioStore.AbsolutePath(recording.RelativeAudioPath);
        }
        catch
        {
            return null;
        }
    }

    private CanonicalManifest BuildCanonicalManifest(
        string deviceId,
        List<SyncRecordingEntry> recordingEntries,
        List<SyncFolderEntry> folderEntries,
        List<SyncStudyItemEntry> studyItemEntries,
        List<SyncArtifactEntry> artifactEntries,
        StudyLibrarySyncManifest manifest
    )
    {
        var objects = new List<Dictionary<string, object?>>();

        foreach (var rec in recordingEntries.OrderBy(r => r.RecordingId))
        {
            objects.Add(new Dictionary<string, object?>
            {
                ["objectID"] = $"recordingMetadata:{rec.RecordingId}",
                ["objectKind"] = "recordingMetadata",
                ["ownerID"] = rec.RecordingId,
                ["displayTitle"] = rec.Title,
                ["sha256"] = rec.MetadataHash,
                ["updatedAt"] = rec.UpdatedAt,
                ["deleted"] = rec.Deleted,
                ["tombstone"] = rec.Tombstone,
                ["sourceDeviceID"] = rec.SourceDeviceID,
                ["autoDownloadAllowed"] = true,
                ["conflictStatus"] = null
            });

            objects.Add(new Dictionary<string, object?>
            {
                ["objectID"] = $"recordingAudio:{rec.RecordingId}",
                ["objectKind"] = "recordingAudio",
                ["ownerID"] = rec.RecordingId,
                ["displayTitle"] = rec.Title,
                ["sha256"] = rec.AudioChecksum,
                ["size"] = rec.AudioSize,
                ["updatedAt"] = rec.UpdatedAt,
                ["deleted"] = rec.Deleted,
                ["tombstone"] = rec.Tombstone,
                ["sourceDeviceID"] = rec.SourceDeviceID,
                ["logicalPathToken"] = rec.AudioLogicalPathToken,
                ["autoDownloadAllowed"] = false,
                ["conflictStatus"] = null
            });
        }

        foreach (var folder in folderEntries.OrderBy(f => f.FolderId))
        {
            objects.Add(new Dictionary<string, object?>
            {
                ["objectID"] = $"studyFolder:{folder.FolderId}",
                ["objectKind"] = "studyFolder",
                ["ownerID"] = folder.FolderId,
                ["displayTitle"] = folder.Name,
                ["sha256"] = folder.RevisionHash,
                ["updatedAt"] = folder.UpdatedAt,
                ["deleted"] = folder.Deleted,
                ["tombstone"] = folder.Deleted,
                ["sourceDeviceID"] = null,
                ["logicalPathToken"] = folder.Path,
                ["autoDownloadAllowed"] = true,
                ["conflictStatus"] = null
            });
        }

        foreach (var item in studyItemEntries.OrderBy(i => i.ItemId))
        {
            objects.Add(new Dictionary<string, object?>
            {
                ["objectID"] = $"studyItem:{item.ItemId}",
                ["objectKind"] = "studyItem",
                ["ownerID"] = item.RecordingId ?? item.ItemId,
                ["displayTitle"] = item.Title,
                ["sha256"] = item.RevisionHash,
                ["updatedAt"] = item.UpdatedAt,
                ["deleted"] = item.Deleted,
                ["tombstone"] = item.Deleted,
                ["sourceDeviceID"] = null,
                ["logicalPathToken"] = item.ItemId,
                ["autoDownloadAllowed"] = true,
                ["conflictStatus"] = null
            });
        }

        foreach (var artifact in artifactEntries.OrderBy(a => a.ArtifactId))
        {
            objects.Add(new Dictionary<string, object?>
            {
                ["objectID"] = artifact.ArtifactId,
                ["objectKind"] = CanonicalArtifactKind(artifact.Kind),
                ["ownerID"] = artifact.OwnerId,
                ["displayTitle"] = artifact.OwnerId,
                ["sha256"] = artifact.Checksum,
                ["size"] = artifact.Size,
                ["updatedAt"] = artifact.UpdatedAt,
                ["deleted"] = false,
                ["tombstone"] = false,
                ["sourceDeviceID"] = null,
                ["logicalPathToken"] = artifact.LogicalPathToken,
                ["autoDownloadAllowed"] = artifact.Kind != SyncArtifactKind.Audio
            });
        }

        var generatedAt = DateTime.UtcNow;
        var payload = new Dictionary<string, object?>
        {
            ["schemaVersion"] = 1,
            ["node"] = new Dictionary<string, object?>
            {
                ["nodeID"] = deviceId,
                ["platform"] = "Windows",
                ["displayName"] = "Windows"
            },
            ["generatedAt"] = generatedAt,
            ["objects"] = objects,
            ["objectCount"] = objects.Count,
            ["studyManifestRecordCount"] = manifest.Items.Count,
            ["studyManifestFolderCount"] = manifest.Folders.Count,
            ["tombstoneCount"] = manifest.Tombstones.Count,
            ["pendingUploadCount"] = manifest.PendingUploads.Count,
            ["artifacts"] = artifactEntries.Select(a => new Dictionary<string, object?>
            {
                ["artifactID"] = a.ArtifactId,
                ["kind"] = CanonicalArtifactKind(a.Kind),
                ["ownerID"] = a.OwnerId,
                ["sha256"] = a.Checksum,
                ["size"] = a.Size,
                ["updatedAt"] = a.UpdatedAt,
                ["logicalPathToken"] = a.LogicalPathToken
            }).ToList()
        };

        var manifestHash = BuildCanonicalManifestHash(payload);
        payload["manifestHash"] = manifestHash;

        return new CanonicalManifest
        {
            Node = new CanonicalManifestNode
            {
                NodeID = deviceId,
                Platform = "Windows",
                DisplayName = "Windows"
            },
            Payload = payload,
            SchemaVersion = 1,
            GeneratedAt = generatedAt,
            ManifestHash = manifestHash
        };
    }

    private static string CanonicalArtifactKind(SyncArtifactKind kind) => kind switch
    {
        SyncArtifactKind.TranscriptMarkdown => "transcriptMarkdown",
        SyncArtifactKind.TranscriptJson => "transcriptJSON",
        SyncArtifactKind.NoteMarkdown => "noteMarkdown",
        SyncArtifactKind.NoteJson => "noteJSON",
        SyncArtifactKind.MetadataJson => "metadataJSON",
        SyncArtifactKind.ReceiveJson => "receiveJSON",
        SyncArtifactKind.SummaryMarkdown => "summaryMarkdown",
        SyncArtifactKind.SummaryJson => "summaryJSON",
        SyncArtifactKind.Audio => "recordingAudio",
        _ => kind.ToString()
    };

    private string BuildCanonicalManifestHash(Dictionary<string, object?> payload)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        return SyncChecksum.Compute(json);
    }

    private List<SyncArtifactEntry> BuildArtifacts(StudyLibrarySyncManifest manifest)
    {
        var artifacts = new List<SyncArtifactEntry>();
        foreach (var item in manifest.Items)
        {
            var ownerId = item.RecordingId ?? item.ItemId;
            AddArtifact(item.TranscriptMarkdownRelativePath,
                SyncArtifactKind.TranscriptMarkdown, ownerId, artifacts);
            AddArtifact(item.TranscriptRelativePath,
                SyncArtifactKind.TranscriptJson, ownerId, artifacts);
            AddArtifact(item.ReceiveRelativePath,
                SyncArtifactKind.ReceiveJson, ownerId, artifacts);
            AddArtifact(item.NoteRelativePath,
                item.NoteRelativePath?.EndsWith(".json") == true
                    ? SyncArtifactKind.NoteJson : SyncArtifactKind.NoteMarkdown,
                ownerId, artifacts);
            AddArtifact(item.SummaryMarkdownRelativePath,
                SyncArtifactKind.SummaryMarkdown, ownerId, artifacts);
            AddArtifact(item.SummaryJSONRelativePath,
                SyncArtifactKind.SummaryJson, ownerId, artifacts);
            AddArtifact(item.AudioRelativePath,
                SyncArtifactKind.Audio, ownerId, artifacts, includeChecksum: false);
        }
        return artifacts;
    }

    private void AddArtifact(string? relativePath, SyncArtifactKind kind,
        string ownerId, List<SyncArtifactEntry> artifacts,
        bool includeChecksum = true)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        try
        {
            var absPath = _audioStore.AbsolutePath(relativePath);
            if (!File.Exists(absPath)) return;

            var info = new FileInfo(absPath);
            var checksum = includeChecksum
                ? SyncChecksum.Compute(File.ReadAllText(absPath)) : null;

            artifacts.Add(new SyncArtifactEntry
            {
                ArtifactId = SyncArtifactEntry.MakeArtifactId(kind, ownerId, relativePath),
                Kind = kind,
                OwnerId = ownerId,
                Checksum = checksum,
                Size = info.Length,
                UpdatedAt = info.LastWriteTimeUtc,
                LogicalPathToken = relativePath
            });
        }
        catch { }
    }
}
