using System.Text.Json;
using Rokurics.Models;

namespace Rokurics.Services;

/// <summary>
/// Study library store — manages study items, folders, hierarchy rules, sync manifests.
/// Mirrors StudyLibraryStore from source.
/// </summary>
public class StudyLibraryStore
{
    private readonly AudioFileStore _audioFileStore;
    private readonly string _studyDir;
    private readonly string _itemsDir;
    private readonly string _foldersDir;
    private readonly string _indexPath;
    private readonly string _hierarchyRulesPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<StudyItemMetadata> AllStudyItems { get; private set; } = new();
    public List<StudyFolderMetadata> AllStudyFolders { get; private set; } = new();
    public List<StudyHierarchyRuleDef> HierarchyRules { get; private set; } = new() { StudyHierarchyRuleDef.DefaultCourseView };
    public StudyHierarchyRuleDef SelectedHierarchyRule => HierarchyRules.FirstOrDefault() ?? StudyHierarchyRuleDef.DefaultCourseView;

    public StudyLibraryStore(AudioFileStore? audioFileStore = null)
    {
        _audioFileStore = audioFileStore ?? new AudioFileStore();
        var root = _audioFileStore.BaseDirectory;
        _studyDir = Path.Combine(root, "study");
        _itemsDir = Path.Combine(_studyDir, "items");
        _foldersDir = Path.Combine(_studyDir, "folders");
        _indexPath = Path.Combine(_studyDir, "index.json");
        _hierarchyRulesPath = Path.Combine(_studyDir, "hierarchy-rules.json");

        EnsureDirectories();
        HierarchyRules = LoadHierarchyRules();
        Refresh();
    }

    public void Refresh()
    {
        var recordings = _audioFileStore.LoadAllMetadata();
        var storedItems = LoadAllStoredItemMetadata();
        var itemsById = new Dictionary<string, StudyItemMetadata>();

        foreach (var recording in recordings)
        {
            var fallback = StudyItemMetadata.DefaultForRecording(recording);
            var existing = storedItems.FirstOrDefault(i => i.RecordingId == recording.Id);
            var merged = existing?.MergedWithCurrentRecording(recording) ?? fallback;
            itemsById[merged.ItemId] = merged;
        }

        foreach (var item in storedItems.Where(i => !itemsById.ContainsKey(i.ItemId)))
            itemsById[item.ItemId] = item;

        AllStudyItems = itemsById.Values
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AllStudyFolders = LoadAllFolderMetadata().Where(f => !f.IsTrashed).ToList();
    }

    public StudyItemMetadata? FindItem(string itemId) =>
        AllStudyItems.FirstOrDefault(i => i.ItemId == itemId || i.RecordingId == itemId);

    public StudyItemMetadata UpsertRecordingMetadata(RecordingMetadata recording)
    {
        var existing = AllStudyItems.FirstOrDefault(i => i.RecordingId == recording.Id);
        var fallback = StudyItemMetadata.DefaultForRecording(recording);
        var metadata = existing?.MergedWithCurrentRecording(recording) ?? fallback;
        SaveItem(metadata);
        Refresh();
        return metadata;
    }

    public void UpdateFiling(string recordingId, StudyFilingPath? filing)
    {
        var item = FindItem(StudyItemMetadata.RecordingBundleItemId(recordingId));
        if (item is null) return;

        item.StudyFiling = filing;
        item.UpdatedAt = DateTime.UtcNow;
        SaveItem(item);
        Refresh();
    }

    public StudyFolderMetadata CreateFolder(string rawName, StudyBrowserPath path)
    {
        var level = StudyFolderMetadata.LevelForDepth(path.Depth)
            ?? throw new InvalidOperationException("unsupported folder level");
        var name = string.IsNullOrWhiteSpace(rawName) ? StudyHierarchyRule.MissingValue : rawName.Trim();
        var components = new List<string>(path.Components) { name };
        var filing = StudyFolderMetadata.FilingPathFor(components);

        var folder = new StudyFolderMetadata(
            folderId: null, name: name, level: level, path: filing,
            parentFolderId: null);

        SaveFolder(folder);
        Refresh();
        return folder;
    }

    public void SetFolderColor(string? folderId, StudyFolderColorToken colorToken)
    {
        if (folderId is null) return;
        var folder = AllStudyFolders.FirstOrDefault(f =>
            string.Equals(f.FolderId, folderId, StringComparison.OrdinalIgnoreCase));
        if (folder is null) return;
        folder.ColorToken = colorToken == StudyFolderColorToken.Default ? null : colorToken;
        folder.UpdatedAt = DateTime.UtcNow;
        SaveFolder(folder);
        Refresh();
    }

    public void RenameFolder(string? folderId, string newName)
    {
        if (folderId is null || string.IsNullOrWhiteSpace(newName)) return;
        var folder = AllStudyFolders.FirstOrDefault(f =>
            string.Equals(f.FolderId, folderId, StringComparison.OrdinalIgnoreCase));
        if (folder is null) return;
        folder.Name = newName.Trim();
        folder.UpdatedAt = DateTime.UtcNow;
        SaveFolder(folder);
        Refresh();
    }

    public void RemoveFolder(string? folderId)
    {
        if (folderId is null) return;
        var folder = AllStudyFolders.FirstOrDefault(f =>
            string.Equals(f.FolderId, folderId, StringComparison.OrdinalIgnoreCase));
        if (folder is null) return;
        folder.IsTrashed = true;
        folder.TrashedAt = DateTime.UtcNow;
        folder.UpdatedAt = DateTime.UtcNow;
        SaveFolder(folder);
        Refresh();
    }

    public StudyLibrarySyncManifest MakeSyncManifest(string deviceId, DateTime? generatedAt = null)
    {
        Refresh();
        var items = AllStudyItems.Select(i => i.SyncSanitized(deviceId)).ToList();
        var folders = AllStudyFolders.Select(f => f.SyncSanitized(deviceId)).ToList();
        var recordings = _audioFileStore.LoadAllMetadata(includeDeleted: true);
        var itemsById = items.ToDictionary(i => i.ItemId, i => i);
        var recordingEntries = makeSyncRecordings(recordings);
        var pendingUploads = makePendingRecordingUploads(recordings, itemsById, deviceId);
        var tombstones = new List<StudyLibrarySyncTombstone>();

        foreach (var item in AllStudyItems.Where(i => i.IsTrashed))
            tombstones.Add(new StudyLibrarySyncTombstone
            {
                Id = $"item:{item.ItemId}",
                EntityKind = StudyLibrarySyncEntityKind.Item,
                EntityId = item.ItemId,
                Operation = StudyLibrarySyncOperation.Trash,
                UpdatedAt = item.TrashedAt ?? item.UpdatedAt,
                ModifiedByDeviceId = item.ModifiedByDeviceId ?? deviceId
            });

        foreach (var folder in AllStudyFolders.Where(f => f.IsTrashed))
            tombstones.Add(new StudyLibrarySyncTombstone
            {
                Id = $"folder:{folder.FolderId}",
                EntityKind = StudyLibrarySyncEntityKind.Folder,
                EntityId = folder.FolderId,
                Operation = StudyLibrarySyncOperation.Trash,
                UpdatedAt = folder.TrashedAt ?? folder.UpdatedAt,
                ModifiedByDeviceId = folder.ModifiedByDeviceId ?? deviceId
            });

        return StudyLibrarySyncManifest.Make(deviceId, generatedAt ?? DateTime.UtcNow,
            items, folders, recordingEntries, tombstones, pendingUploads);
    }

    private List<PendingRecordingUpload> makePendingRecordingUploads(
        List<RecordingMetadata> recordings,
        Dictionary<string, StudyItemMetadata> itemsById,
        string targetDeviceID)
    {
        return recordings
            .Where(r => !r.IsDeleted && !RecordingUploadStatus.FromMetadata(r.UploadStatus).IsUploaded)
            .Select(r =>
            {
                var fallbackItemID = StudyItemMetadata.RecordingBundleItemId(r.Id);
                var item = itemsById.TryGetValue(fallbackItemID, out var matched)
                    ? matched
                    : StudyItemMetadata.DefaultForRecording(r);

                return new PendingRecordingUpload
                {
                    ItemId = item.ItemId,
                    RecordingId = r.Id,
                    LocalAudioRelativePath = r.RelativeAudioPath,
                    TargetDeviceId = targetDeviceID,
                    Status = r.UploadStatus,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                };
            })
            .OrderBy(u => u.ItemId)
            .ToList();
    }

    private List<SyncRecordingEntry> makeSyncRecordings(List<RecordingMetadata> recordings)
    {
        return recordings
            .Select(r =>
            {
                var audioPath = string.IsNullOrWhiteSpace(r.RelativeAudioPath)
                    ? null
                    : TryResolveAudioPath(r.RelativeAudioPath);
                var audioAvailable = !string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath);
                var audioSize = audioAvailable
                    ? new FileInfo(audioPath!).Length
                    : (long?)null;

                return new SyncRecordingEntry
                {
                    RecordingId = r.Id,
                    MetadataHash = computeRecordingFingerprint(r),
                    AudioAvailable = audioAvailable,
                    AudioChecksum = audioAvailable
                        ? SyncChecksum.Compute(File.ReadAllBytes(audioPath!))
                        : null,
                    AudioSize = audioSize,
                    UpdatedAt = r.DeletedAt ?? r.CreatedAt,
                    Deleted = r.IsDeleted
                };
            })
            .OrderBy(r => r.RecordingId)
            .ToList();
    }

    private string computeRecordingFingerprint(RecordingMetadata recording)
    {
        return SyncChecksum.ComputeFromJson(recording);
    }

    private string? TryResolveAudioPath(string relativeAudioPath)
    {
        try
        {
            return _audioFileStore.AbsolutePath(relativeAudioPath);
        }
        catch
        {
            return null;
        }
    }

    private void EnsureDirectories()
    {
        Directory.CreateDirectory(_itemsDir);
        Directory.CreateDirectory(_foldersDir);
        if (!File.Exists(_hierarchyRulesPath))
        {
            var json = JsonSerializer.Serialize(new[] { StudyHierarchyRuleDef.DefaultCourseView }, JsonOptions);
            File.WriteAllText(_hierarchyRulesPath, json);
        }
        if (!File.Exists(_indexPath))
        {
            File.WriteAllText(_indexPath, "{}");
        }
    }

    private List<StudyHierarchyRuleDef> LoadHierarchyRules()
    {
        try
        {
            if (File.Exists(_hierarchyRulesPath))
            {
                var json = File.ReadAllText(_hierarchyRulesPath);
                var rules = JsonSerializer.Deserialize<List<StudyHierarchyRuleDef>>(json, JsonOptions);
                if (rules is { Count: > 0 }) return rules;
            }
        }
        catch { }
        return new List<StudyHierarchyRuleDef> { StudyHierarchyRuleDef.DefaultCourseView };
    }

    private List<StudyItemMetadata> LoadAllStoredItemMetadata()
    {
        var items = new List<StudyItemMetadata>();
        if (!Directory.Exists(_itemsDir)) return items;
        foreach (var file in Directory.GetFiles(_itemsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var item = JsonSerializer.Deserialize<StudyItemMetadata>(json, JsonOptions);
                if (item is not null) items.Add(item);
            }
            catch { }
        }
        return items;
    }

    private List<StudyFolderMetadata> LoadAllFolderMetadata()
    {
        var folders = new List<StudyFolderMetadata>();
        if (!Directory.Exists(_foldersDir)) return folders;
        foreach (var file in Directory.GetFiles(_foldersDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var folder = JsonSerializer.Deserialize<StudyFolderMetadata>(json, JsonOptions);
                if (folder is not null) folders.Add(folder);
            }
            catch { }
        }
        return folders;
    }

    private void SaveItem(StudyItemMetadata item)
    {
        var fileName = $"{StudyPathSanitizer.SanitizedPathComponent(item.ItemId)}.json";
        var path = Path.Combine(_itemsDir, fileName);
        var json = JsonSerializer.Serialize(item, JsonOptions);
        File.WriteAllText(path, json);
    }

    private void SaveFolder(StudyFolderMetadata folder)
    {
        var fileName = $"{StudyPathSanitizer.SanitizedPathComponent(folder.FolderId)}.json";
        var path = Path.Combine(_foldersDir, fileName);
        var json = JsonSerializer.Serialize(folder, JsonOptions);
        File.WriteAllText(path, json);
    }
}

public sealed class StudyHierarchyRuleDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> Levels { get; set; }

    public StudyHierarchyRuleDef()
    {
        Id = "course-view";
        Name = "课程视图";
        Levels = new List<string> { "type", "subject", "chapter", "topic" };
    }

    public static readonly StudyHierarchyRuleDef DefaultCourseView = new()
    {
        Id = "course-view",
        Name = "课程视图",
        Levels = new List<string> { "type", "subject", "chapter", "topic" }
    };
}
