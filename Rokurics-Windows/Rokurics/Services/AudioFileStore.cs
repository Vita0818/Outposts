using System.Text.Json;
using Rokurics.Models;

namespace Rokurics.Services;

/// <summary>
/// File-based audio recording and metadata persistence.
/// Mirrors AudioFileStore from source.
/// </summary>
public class AudioFileStore
{
    private readonly string _baseDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AudioFileStore(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rokurics");
    }

    public string BaseDirectory => _baseDirectory;
    public string RecordingsDirectory => Path.Combine(_baseDirectory, "Recordings");
    public string MetadataDirectory => Path.Combine(_baseDirectory, "Metadata");

    public void EnsureStorageDirectories()
    {
        Directory.CreateDirectory(RecordingsDirectory);
        Directory.CreateDirectory(MetadataDirectory);
    }

    public string MakeRecordingPath(DateTime? date = null, bool fallback = false)
    {
        var d = date ?? DateTime.Now;
        var suffix = fallback ? "_fallback" : "";
        var baseName = $"rokurics_{d:yyyy-MM-dd_HH-mm-ss}{suffix}";
        return Path.Combine(RecordingsDirectory, $"{baseName}.m4a");
    }

    public string MakeMetadataPath(string id)
    {
        var safeId = StudyPathSanitizer.SanitizedPathComponent(id);
        var path = Path.Combine(MetadataDirectory, $"{safeId}.json");
        var fullBase = Path.GetFullPath(_baseDirectory);
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullBase + Path.DirectorySeparatorChar) && fullPath != fullBase)
            throw new InvalidOperationException($"Path outside Rokurics directory: {path}");
        return path;
    }

    public bool FileExists(string path) => File.Exists(path);

    public long FileSize(string path) => new FileInfo(path).Length;

    public string RelativePath(string absolutePath)
    {
        var basePath = Path.GetFullPath(_baseDirectory);
        var filePath = Path.GetFullPath(absolutePath);
        if (!filePath.StartsWith(basePath + Path.DirectorySeparatorChar) && filePath != basePath)
            throw new InvalidOperationException($"Path outside Rokurics directory: {absolutePath}");
        return filePath[basePath.Length..].TrimStart(Path.DirectorySeparatorChar);
    }

    public string AbsolutePath(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(_baseDirectory, relativePath));
        var basePath = Path.GetFullPath(_baseDirectory);
        if (!path.StartsWith(basePath + Path.DirectorySeparatorChar) && path != basePath)
            throw new InvalidOperationException($"Path outside Rokurics directory: {relativePath}");
        return path;
    }

    public void SaveMetadata(RecordingMetadata metadata)
    {
        var path = MakeMetadataPath(metadata.Id);
        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        File.WriteAllText(path, json);
    }

    public void UpdateMetadata(RecordingMetadata metadata) => SaveMetadata(metadata);

    public RecordingMetadata? LoadMetadata(string id)
    {
        var path = MakeMetadataPath(id);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<RecordingMetadata>(json, JsonOptions);
    }

    public List<RecordingMetadata> LoadAllMetadata(bool includeDeleted = false)
    {
        EnsureStorageDirectories();
        var recordings = new List<RecordingMetadata>();
        if (!Directory.Exists(MetadataDirectory)) return recordings;

        foreach (var file in Directory.GetFiles(MetadataDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var metadata = JsonSerializer.Deserialize<RecordingMetadata>(json, JsonOptions);
                if (metadata is not null && (includeDeleted || !metadata.IsDeleted))
                    recordings.Add(metadata);
            }
            catch { /* skip unreadable files */ }
        }

        return recordings
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    public List<RecordingMetadata> LoadTrashedMetadata()
    {
        return LoadAllMetadata(includeDeleted: true)
            .Where(r => r.IsDeleted)
            .OrderByDescending(r => r.DeletedAt ?? r.CreatedAt)
            .ToList();
    }

    public RecordingMetadata? LatestMetadata() => LoadAllMetadata().FirstOrDefault();

    public RecordingMetadata UpdateTitle(string recordingId, string rawTitle)
    {
        var metadata = LoadMetadata(recordingId)
            ?? throw new InvalidOperationException($"Recording not found: {recordingId}");
        var title = RecordingTitleEditRules.NormalizedTitle(rawTitle, metadata.Title);
        var updated = metadata.WithTitle(title);
        SaveMetadata(updated);
        return updated;
    }

    public RecordingMetadata MoveToTrash(string recordingId)
    {
        var metadata = LoadMetadata(recordingId)
            ?? throw new InvalidOperationException($"Recording not found: {recordingId}");
        var updated = metadata.WithTrashState(true, DateTime.UtcNow);
        SaveMetadata(updated);
        return updated;
    }

    public RecordingMetadata RestoreRecording(string recordingId)
    {
        var metadata = LoadMetadata(recordingId)
            ?? throw new InvalidOperationException($"Recording not found: {recordingId}");
        var updated = metadata.WithTrashState(false, null);
        SaveMetadata(updated);
        return updated;
    }

    public void PermanentlyDeleteRecording(string recordingId)
    {
        var metadata = LoadMetadata(recordingId);
        if (metadata is null) return;

        var audioPath = AbsolutePath(metadata.RelativeAudioPath);
        var metadataPath = MakeMetadataPath(recordingId);

        if (File.Exists(audioPath)) File.Delete(audioPath);
        if (File.Exists(metadataPath)) File.Delete(metadataPath);
    }

    public int RecoverStaleUploadingMetadata()
    {
        var all = LoadAllMetadata(includeDeleted: true);
        int count = 0;
        foreach (var m in all.Where(m => m.UploadStatus == RecordingUploadStatus.UploadingValue))
        {
            SaveMetadata(m.RecoveringStaleUploadingStatus());
            count++;
        }
        return count;
    }
}

public static class RecordingTitleEditRules
{
    public static string NormalizedTitle(string rawTitle, string fallback)
    {
        var trimmed = rawTitle.Trim();
        return string.IsNullOrEmpty(trimmed) ? fallback : trimmed;
    }
}
