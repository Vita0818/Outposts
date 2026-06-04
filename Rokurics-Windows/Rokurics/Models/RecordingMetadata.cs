using System.Text.Json.Serialization;

namespace Rokurics.Models;

/// <summary>
/// Audio recording metadata. Mirrors RecordingMetadata from source.
/// </summary>
public sealed class RecordingMetadata
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string FileName { get; init; } = "";
    public string RelativeAudioPath { get; init; } = "";
    public string RelativeMetadataPath { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime EndedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public string Format { get; init; } = "";
    public string Codec { get; init; } = "";
    public double SampleRate { get; init; }
    public int Channels { get; init; }
    public int Bitrate { get; init; }
    public long FileSize { get; init; }
    public string UploadStatus { get; init; } = RecordingUploadStatus.LocalOnlyValue;
    public string TranscriptionStatus { get; init; } = "";
    public string NoteStatus { get; init; } = "";
    public List<string> Tags { get; init; } = [];
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

    public RecordingMetadata WithTitle(string title) => Clone(title: title);

    public RecordingMetadata WithUploadStatus(RecordingUploadStatus status) =>
        Clone(uploadStatus: status.Value);

    public RecordingMetadata WithUploadProgress(
        double? fraction, long? confirmedBytes, long? totalBytes, string? phase, string? description) =>
        Clone(
            uploadProgressFraction: fraction,
            uploadProgressConfirmedBytes: confirmedBytes,
            uploadProgressTotalBytes: totalBytes,
            uploadPhase: phase,
            uploadProgressDescription: description);

    public RecordingMetadata RecoveringStaleUploadingStatus() =>
        UploadStatus == RecordingUploadStatus.UploadingValue
            ? Clone(uploadStatus: RecordingUploadStatus.FailedValue)
            : this;

    public RecordingMetadata WithTrashState(bool isDeleted, DateTime? deletedAt) =>
        Clone(isDeleted: isDeleted, deletedAt: deletedAt);

    public RecordingMetadata WithStudyFiling(StudyFilingPath? filing) =>
        Clone(keepStudyFiling: false, studyFiling: filing);

    private RecordingMetadata Clone(
        string? id = null,
        string? title = null,
        string? fileName = null,
        string? relativeAudioPath = null,
        string? relativeMetadataPath = null,
        DateTime? createdAt = null,
        DateTime? endedAt = null,
        TimeSpan? duration = null,
        string? format = null,
        string? codec = null,
        double? sampleRate = null,
        int? channels = null,
        int? bitrate = null,
        long? fileSize = null,
        string? uploadStatus = null,
        string? transcriptionStatus = null,
        string? noteStatus = null,
        List<string>? tags = null,
        bool keepStudyFiling = true,
        StudyFilingPath? studyFiling = null,
        double? uploadProgressFraction = null,
        long? uploadProgressConfirmedBytes = null,
        long? uploadProgressTotalBytes = null,
        string? uploadPhase = null,
        string? uploadProgressDescription = null,
        bool? isDeleted = null,
        DateTime? deletedAt = null)
    {
        return new RecordingMetadata(
            id: id ?? Id,
            title: title ?? Title,
            fileName: fileName ?? FileName,
            relativeAudioPath: relativeAudioPath ?? RelativeAudioPath,
            relativeMetadataPath: relativeMetadataPath ?? RelativeMetadataPath,
            createdAt: createdAt ?? CreatedAt,
            endedAt: endedAt ?? EndedAt,
            duration: duration ?? Duration,
            format: format ?? Format,
            codec: codec ?? Codec,
            sampleRate: sampleRate ?? SampleRate,
            channels: channels ?? Channels,
            bitrate: bitrate ?? Bitrate,
            fileSize: fileSize ?? FileSize,
            uploadStatus: uploadStatus ?? UploadStatus,
            transcriptionStatus: transcriptionStatus ?? TranscriptionStatus,
            noteStatus: noteStatus ?? NoteStatus,
            tags: tags ?? Tags,
            studyFiling: keepStudyFiling ? StudyFiling : studyFiling?.IsEmpty == true ? null : studyFiling,
            uploadProgressFraction: uploadProgressFraction ?? UploadProgressFraction,
            uploadProgressConfirmedBytes: uploadProgressConfirmedBytes ?? UploadProgressConfirmedBytes,
            uploadProgressTotalBytes: uploadProgressTotalBytes ?? UploadProgressTotalBytes,
            uploadPhase: uploadPhase ?? UploadPhase,
            uploadProgressDescription: uploadProgressDescription ?? UploadProgressDescription,
            isDeleted: isDeleted ?? IsDeleted,
            deletedAt: deletedAt ?? DeletedAt
        );
    }

    public static string DefaultTitle(DateTime createdAt) =>
        $"录音 {createdAt:yyyy-MM-dd HH:mm}";
}

public sealed class RecordingUploadStatus
{
    public string Value { get; }
    private RecordingUploadStatus(string value) => Value = value;

    public const string LocalOnlyValue = "localOnly";
    public const string PendingValue = "pending";
    public const string UploadingValue = "uploading";
    public const string UploadedValue = "uploaded";
    public const string FailedValue = "failed";

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
