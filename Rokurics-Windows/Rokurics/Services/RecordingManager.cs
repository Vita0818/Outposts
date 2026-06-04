using Rokurics.Models;

namespace Rokurics.Services;

/// <summary>
/// Recording lifecycle manager. Mirrors RecordingManager from Apple source.
/// On Windows, recording uses WASAPI; the audio capture pipeline is structured
/// but platform-specific initialization is deferred to Windows runtime.
/// </summary>
public class RecordingManager : IDisposable
{
    private readonly AudioFileStore _fileStore;
    private readonly StudyLibraryStore _studyLibraryStore;
    private Timer? _elapsedTimer;
    private DateTime _recordingStartedAt;
    private bool _disposed;

    // ── Public state ──────────────────────────────────────────────

    public RokuricsRecordingState State { get; private set; } = RokuricsRecordingState.Idle;
    public TimeSpan ElapsedTime { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public string? LastRecordingPath { get; private set; }
    public List<RecordingMetadata> Recordings { get; private set; } = new();
    public List<RecordingMetadata> TrashedRecordings { get; private set; } = new();
    public RecordingMetadata? LatestRecordingMetadata { get; private set; }
    public string StatusMessage { get; private set; } = "录音默认仅保存在本地";
    public string? PendingDefaultTitle { get; private set; }
    public string? PendingTitle { get; private set; }

    public int PendingUploadCount => Recordings.Count(r =>
        !RecordingUploadStatus.FromMetadata(r.UploadStatus).IsUploaded);

    public string SuggestedRecordingTitle =>
        PendingDefaultTitle ?? RecordingMetadata.DefaultTitle(DateTime.Now);

    public event Action? StateChanged;

    // ── Construction ──────────────────────────────────────────────

    public RecordingManager(AudioFileStore? fileStore = null, StudyLibraryStore? studyStore = null)
    {
        _fileStore = fileStore ?? new AudioFileStore();
        _studyLibraryStore = studyStore ?? new StudyLibraryStore(_fileStore);
        LoadExistingRecordings();
    }

    public StudyLibraryStore StudyLibraryStore => _studyLibraryStore;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopTimer();
        _elapsedTimer?.Dispose();
        _elapsedTimer = null;
    }

    // ── Recording lifecycle ───────────────────────────────────────

    public void LoadExistingRecordings()
    {
        try
        {
            _fileStore.EnsureStorageDirectories();
            _fileStore.RecoverStaleUploadingMetadata();
            Recordings = _fileStore.LoadAllMetadata();
            TrashedRecordings = _fileStore.LoadTrashedMetadata();
            LatestRecordingMetadata = Recordings.FirstOrDefault();

            if (LatestRecordingMetadata is not null)
            {
                ElapsedTime = LatestRecordingMetadata.Duration;
                StatusMessage = RecentStatusMessage(LatestRecordingMetadata, "最近录音");
                LastRecordingPath = _fileStore.AbsolutePath(LatestRecordingMetadata.RelativeAudioPath);
            }
            _studyLibraryStore.Refresh();
        }
        catch (Exception ex)
        {
            LastErrorMessage = $"读取本地录音失败：{ex.Message}";
            StatusMessage = "读取本地录音失败";
        }
    }

    public void ToggleRecording()
    {
        switch (State)
        {
            case RokuricsRecordingState.Idle:
            case RokuricsRecordingState.Saved:
            case RokuricsRecordingState.Failed:
            case RokuricsRecordingState.PermissionDenied:
                StartRecording();
                break;
            case RokuricsRecordingState.Recording:
                StopRecording();
                break;
            case RokuricsRecordingState.Paused:
                ResumeRecording();
                break;
        }
    }

    public void StartRecording()
    {
        if (State.IsBusy() || State == RokuricsRecordingState.Recording || State == RokuricsRecordingState.Paused)
            return;

        LastErrorMessage = null;
        State = RokuricsRecordingState.ConfiguringSession;
        StatusMessage = "正在配置录音";
        NotifyStateChanged();

        // On Windows, audio capture uses WASAPI via Windows.Media.Audio or NAudio.
        // The audio recording pipeline structure follows the Apple source:
        //   1. Request microphone permission (Windows: appxmanifest capability)
        //   2. Configure audio session (16kHz AAC mono, 64kbps primary; 44.1kHz fallback)
        //   3. Start capture and timer
        //
        // Placeholder for platform audio initialization:
        try
        {
            // TODO: Initialize WASAPI audio capture here
            // var capture = new WindowsAudioCapture(sampleRate: 16000, channels: 1);
            // _activeCapture = capture;
            // capture.Start();

            _recordingStartedAt = DateTime.Now;
            ElapsedTime = TimeSpan.Zero;
            State = RokuricsRecordingState.Recording;
            StatusMessage = "正在录音";
            StartTimer();
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            LastErrorMessage = $"音频初始化失败：{ex.Message}";
            State = RokuricsRecordingState.Failed;
            NotifyStateChanged();
        }
    }

    public void PauseRecording()
    {
        if (State != RokuricsRecordingState.Recording) return;

        State = RokuricsRecordingState.Paused;
        StatusMessage = "录音已暂停";
        StopTimer();
        NotifyStateChanged();
    }

    public void ResumeRecording()
    {
        if (State != RokuricsRecordingState.Paused) return;

        State = RokuricsRecordingState.Recording;
        StatusMessage = "正在录音";
        StartTimer();
        NotifyStateChanged();
    }

    public void StopRecording()
    {
        if (State != RokuricsRecordingState.Recording && State != RokuricsRecordingState.Paused)
            return;

        StopTimer();
        State = RokuricsRecordingState.Stopping;
        StatusMessage = "正在停止录音";

        // TODO: Stop WASAPI audio capture here
        // _activeCapture?.Stop();

        State = RokuricsRecordingState.Filing;
        NotifyStateChanged();
    }

    public void FinalizeRecording(string? rawTitle = null,
        StudyFilingPath? studyFiling = null, bool directSave = false)
    {
        var now = DateTime.Now;
        var defaultTitle = RecordingMetadata.DefaultTitle(now);
        var resolvedFiling = studyFiling?.IsEmpty == true ? null : studyFiling;
        var resolvedTitle = ResolveTitle(defaultTitle, rawTitle ?? PendingTitle,
            resolvedFiling, directSave);

        var recordingPath = _fileStore.MakeRecordingPath(now);
        var id = Path.GetFileNameWithoutExtension(recordingPath);

        var metadata = new RecordingMetadata(
            id: id,
            title: resolvedTitle,
            fileName: Path.GetFileName(recordingPath),
            relativeAudioPath: _fileStore.RelativePath(recordingPath),
            relativeMetadataPath: _fileStore.RelativePath(_fileStore.MakeMetadataPath(id)),
            createdAt: now,
            endedAt: now,
            duration: ElapsedTime,
            format: "m4a",
            codec: "AAC",
            sampleRate: 16000,
            channels: 1,
            bitrate: 64000,
            fileSize: 0, // Updated when actual audio file exists
            uploadStatus: "localOnly",
            transcriptionStatus: "notStarted",
            noteStatus: "notStarted",
            tags: new List<string>(),
            studyFiling: directSave ? null : resolvedFiling
        );

        _fileStore.SaveMetadata(metadata);
        _studyLibraryStore.UpsertRecordingMetadata(metadata);
        Recordings = new[] { metadata }
            .Concat(Recordings.Where(r => r.Id != metadata.Id)).ToList();
        TrashedRecordings.RemoveAll(r => r.Id == metadata.Id);
        LatestRecordingMetadata = metadata;
        LastRecordingPath = recordingPath;
        PendingDefaultTitle = null;
        PendingTitle = null;
        LastErrorMessage = null;
        State = RokuricsRecordingState.Saved;
        StatusMessage = RecentStatusMessage(metadata, "已保存");
        NotifyStateChanged();
    }

    public void FinalizeRecordingDirectSave()
    {
        FinalizeRecording(directSave: true);
    }

    // ── Metadata operations ──────────────────────────────────────

    public void RenameRecording(string recordingId, string rawTitle)
    {
        var existing = Recordings.FirstOrDefault(r => r.Id == recordingId)
            ?? _fileStore.LoadMetadata(recordingId);
        if (existing is null) return;

        var title = RecordingTitleEditRules.NormalizedTitle(rawTitle, existing.Title);
        if (title == existing.Title) return;

        var updated = _fileStore.UpdateTitle(recordingId, title);
        _studyLibraryStore.UpsertRecordingMetadata(updated);
        ReplaceInMemory(updated);
        LastErrorMessage = null;
        StatusMessage = RecentStatusMessage(updated, "已重命名");
    }

    public void DeleteRecording(string recordingId)
    {
        try
        {
            var trashed = _fileStore.MoveToTrash(recordingId);
            Recordings.RemoveAll(r => r.Id == recordingId);
            ReplaceTrashedInMemory(trashed);
            _studyLibraryStore.Refresh();
            RefreshLatestAfterDeletion();
        }
        catch (Exception ex)
        {
            LastErrorMessage = $"删除失败：{ex.Message}";
        }
    }

    public void RestoreRecording(string recordingId)
    {
        try
        {
            var restored = _fileStore.RestoreRecording(recordingId);
            TrashedRecordings.RemoveAll(r => r.Id == recordingId);
            _studyLibraryStore.UpsertRecordingMetadata(restored);
            ReplaceInMemory(restored);
            LatestRecordingMetadata = Recordings.FirstOrDefault();
        }
        catch (Exception ex)
        {
            LastErrorMessage = $"恢复失败：{ex.Message}";
        }
    }

    public void PermanentlyDeleteRecording(string recordingId)
    {
        _fileStore.PermanentlyDeleteRecording(recordingId);
        Recordings.RemoveAll(r => r.Id == recordingId);
        TrashedRecordings.RemoveAll(r => r.Id == recordingId);
        _studyLibraryStore.Refresh();
        RefreshLatestAfterDeletion();
    }

    public void UpdateStudyFiling(string recordingId, StudyFilingPath? filing)
    {
        var existing = Recordings.FirstOrDefault(r => r.Id == recordingId)
            ?? _fileStore.LoadMetadata(recordingId);
        if (existing is null) return;

        var updated = existing.WithStudyFiling(filing);
        _fileStore.UpdateMetadata(updated);
        _studyLibraryStore.UpdateFiling(recordingId, filing);
        ReplaceInMemory(updated);
    }

    public void UpdatePendingTitle(string? rawTitle)
    {
        var trimmed = rawTitle?.Trim() ?? "";
        if (!string.IsNullOrEmpty(trimmed))
            PendingTitle = trimmed;
    }

    public void UpdateUploadStatus(string recordingId, RecordingUploadStatus status)
    {
        var existing = Recordings.FirstOrDefault(r => r.Id == recordingId)
            ?? _fileStore.LoadMetadata(recordingId);
        if (existing is null) return;

        var updated = existing.WithUploadStatus(status);
        _fileStore.UpdateMetadata(updated);
        ReplaceInMemory(updated);
    }

    public void UpdateUploadProgress(string recordingId,
        double fraction, long confirmedBytes, long totalBytes,
        string? phase = null, string? description = null)
    {
        var existing = Recordings.FirstOrDefault(r => r.Id == recordingId)
            ?? _fileStore.LoadMetadata(recordingId);
        if (existing is null) return;

        var updated = existing.WithUploadProgress(
            fraction, confirmedBytes, totalBytes, phase, description);
        _fileStore.UpdateMetadata(updated);
        ReplaceInMemory(updated);
    }

    // ── Timer ─────────────────────────────────────────────────────

    private void StartTimer()
    {
        StopTimer();
        _elapsedTimer = new Timer(_ =>
        {
            ElapsedTime = DateTime.Now - _recordingStartedAt;
        }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(250));
    }

    private void StopTimer()
    {
        _elapsedTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    // ── Private helpers ───────────────────────────────────────────

    private string ResolveTitle(string defaultTitle, string? pendingTitle,
        StudyFilingPath? filing, bool directSave)
    {
        if (directSave) return defaultTitle;
        if (!string.IsNullOrWhiteSpace(pendingTitle)) return pendingTitle.Trim();
        return filing?.SuggestedTitle(defaultTitle) ?? defaultTitle;
    }

    private void ReplaceInMemory(RecordingMetadata metadata)
    {
        var idx = Recordings.FindIndex(r => r.Id == metadata.Id);
        if (idx >= 0) Recordings[idx] = metadata;
        else
        {
            Recordings.Add(metadata);
            Recordings = Recordings.OrderByDescending(r => r.CreatedAt).ToList();
        }
    }

    private void ReplaceTrashedInMemory(RecordingMetadata metadata)
    {
        var idx = TrashedRecordings.FindIndex(r => r.Id == metadata.Id);
        if (idx >= 0) TrashedRecordings[idx] = metadata;
        else
        {
            TrashedRecordings.Add(metadata);
            TrashedRecordings = TrashedRecordings
                .OrderByDescending(r => r.DeletedAt ?? r.CreatedAt)
                .ToList();
        }
    }

    private void RefreshLatestAfterDeletion()
    {
        LatestRecordingMetadata = Recordings.FirstOrDefault();
        if (LatestRecordingMetadata is not null)
        {
            StatusMessage = RecentStatusMessage(LatestRecordingMetadata, "最近录音");
            LastRecordingPath = _fileStore.AbsolutePath(LatestRecordingMetadata.RelativeAudioPath);
            ElapsedTime = LatestRecordingMetadata.Duration;
        }
        else
        {
            StatusMessage = "录音默认仅保存在本地";
            LastRecordingPath = null;
            ElapsedTime = TimeSpan.Zero;
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private static string RecentStatusMessage(RecordingMetadata m, string prefix) =>
        $"{prefix}：{m.CreatedAt:HH:mm} · {DurationText(m.Duration)}";

    private static string DurationText(TimeSpan d) =>
        d.TotalSeconds < 60
            ? $"{(int)d.TotalSeconds} sec"
            : $"{d.TotalMinutes:F1} min";
}

// ── Recording state enum ─────────────────────────────────────────

public enum RokuricsRecordingState
{
    Idle,
    RequestingPermission,
    ConfiguringSession,
    Recording,
    Paused,
    Stopping,
    Filing,
    Saving,
    Saved,
    PermissionDenied,
    Failed
}

public static class RokuricsRecordingStateExtensions
{
    public static bool IsRecording(this RokuricsRecordingState state)
        => state == RokuricsRecordingState.Recording;

    public static bool IsPaused(this RokuricsRecordingState state)
        => state == RokuricsRecordingState.Paused;

    public static bool IsBusy(this RokuricsRecordingState state) =>
        state is RokuricsRecordingState.RequestingPermission
            or RokuricsRecordingState.ConfiguringSession
            or RokuricsRecordingState.Stopping
            or RokuricsRecordingState.Filing
            or RokuricsRecordingState.Saving;
}
