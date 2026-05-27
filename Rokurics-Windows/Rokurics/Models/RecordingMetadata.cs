using System.Text.Json.Serialization;

namespace Rokurics.Models;

/// <summary>
/// Audio recording metadata. Mirrors RecordingMetadata from source.
/// </summary>
public sealed class RecordingMetadata
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string FileName { get; init; }
    public string RelativeAudioPath { get; init; }
    public string RelativeMetadataPath { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime EndedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public string Format { get; init; }
    public string Codec { get; init; }
    public double SampleRate { get; init; }
    public int Channels { get; init; }
    public int Bitrate { get; init; }
    public long FileSize { get; init; }
    public string UploadStatus { get; init; }
    public string TranscriptionStatus { get; init; }
    public string NoteStatus { get; init; }
    public List<string> Tags { get; init; }
    public StudyFilingPath? StudyFiling { get; init; }
    public double? UploadProgressFraction { get; init; }
    public long? UploadProgressConfirmedBytes { get; init; }
    public long? UploadProgressTotalBytes { get; init; }
    public string? UploadPhase { get; init; }
    public string? UploadProgressDescription { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAt { get; init; }

    public RecordingMetadata(
        string id, string title, string fileName,
        string relativeAudioPath, string relativeMetadataPath,
        DateTime createdAt, DateTime endedAt, TimeSpan duration,
        string format, string codec, double sampleRate, int channels, int bitrate, long fileSize,
        string uploadStatus, string transcriptionStatus, string noteStatus,
        List<string> tags, StudyFilingPath? studyFiling = null,
        double? uploadProgressFraction = null, long? uploadProgressConfirmedBytes = null,
        long? uploadProgressTotalBytes = null, string? uploadPhase = null,
        string? uploadProgressDescription = null, bool isDeleted = false, DateTime? deletedAt = null)
    {
        Id = id;
        Title = title;
        FileName = fileName;
        RelativeAudioPath = relativeAudioPath;
        RelativeMetadataPath = relativeMetadataPath;
        CreatedAt = createdAt;
        EndedAt = endedAt;
        Duration = duration;
        Format = format;
        Codec = codec;
        SampleRate = sampleRate;
        Channels = channels;
        Bitrate = bitrate;
        FileSize = fileSize;
        UploadStatus = uploadStatus;
        TranscriptionStatus = transcriptionStatus;
        NoteStatus = noteStatus;
        Tags = tags;
        StudyFiling = studyFiling?.IsEmpty == true ? null : studyFiling;
        UploadProgressFraction = uploadProgressFraction;
        UploadProgressConfirmedBytes = uploadProgressConfirmedBytes;
        UploadProgressTotalBytes = uploadProgressTotalBytes;
        UploadPhase = uploadPhase;
        UploadProgressDescription = uploadProgressDescription;
        IsDeleted = isDeleted;
        DeletedAt = deletedAt;
    }

    [JsonConstructor]
    public RecordingMetadata() { }

    public RecordingMetadata WithTitle(string title) => this with { Title = title };

    public RecordingMetadata WithUploadStatus(RecordingUploadStatus status) =>
        this with { UploadStatus = status.Value };

    public RecordingMetadata WithUploadProgress(double? fraction, long? confirmedBytes, long? totalBytes, string? phase, string? description) =>
        this with { UploadProgressFraction = fraction, UploadProgressConfirmedBytes = confirmedBytes, UploadProgressTotalBytes = totalBytes, UploadPhase = phase, UploadProgressDescription = description };

    public RecordingMetadata RecoveringStaleUploadingStatus() =>
        UploadStatus == RecordingUploadStatus.UploadingValue ? this with { UploadStatus = RecordingUploadStatus.FailedValue } : this;

    public RecordingMetadata WithTrashState(bool isDeleted, DateTime? deletedAt) =>
        this with { IsDeleted = isDeleted, DeletedAt = deletedAt };

    public RecordingMetadata WithStudyFiling(StudyFilingPath? filing) =>
        this with { StudyFiling = filing?.IsEmpty == true ? null : filing };

    public static string DefaultTitle(DateTime createdAt) =>
        $"录音 {createdAt:yyyy-MM-dd HH:mm}";
}

public sealed class RecordingUploadStatus
{
    public string Value { get; }
    private RecordingUploadStatus(string value) => Value = value;

    public static readonly string LocalOnlyValue = "localOnly";
    public static readonly string PendingValue = "pending";
    public static readonly string UploadingValue = "uploading";
    public static readonly string UploadedValue = "uploaded";
    public static readonly string FailedValue = "failed";

    public static RecordingUploadStatus LocalOnly => new(LocalOnlyValue);
    public static RecordingUploadStatus Pending => new(PendingValue);
    public static RecordingUploadStatus Uploading => new(UploadingValue);
    public static RecordingUploadStatus Uploaded => new(UploadedValue);
    public static RecordingUploadStatus Failed => new(FailedValue);

    public static RecordingUploadStatus FromMetadata(string value) => value switch
    {
        LocalOnlyValue => LocalOnly,
        PendingValue => Pending,
        UploadingValue => Uploading,
        UploadedValue => Uploaded,
        FailedValue => Failed,
        _ => LocalOnly
    };

    public bool IsLocalOnly => Value == LocalOnlyValue;
    public bool IsUploaded => Value == UploadedValue;
    public bool IsUploading => Value == UploadingValue;
}
