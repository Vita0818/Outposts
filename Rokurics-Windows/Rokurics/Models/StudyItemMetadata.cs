using System.Text.Json.Serialization;

namespace Rokurics.Models;

/// <summary>
/// Study library item metadata. Mirrors StudyItemMetadata from source.
/// </summary>
public sealed class StudyItemMetadata
{
    public string ItemId { get; set; }
    public StudyItemKind Kind { get; set; }
    public string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public StudyFilingPath Filing { get; set; }
    public List<StudyTag> Tags { get; set; }
    public List<string> FolderIds { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; }
    public string? RecordingId { get; set; }
    public string? SanitizedRecordingId { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? AudioRelativePath { get; set; }
    public string? ReceiveRelativePath { get; set; }
    public string? TranscriptRelativePath { get; set; }
    public string? TranscriptMarkdownRelativePath { get; set; }
    public string? NoteRelativePath { get; set; }
    public string? TranscriptionStatus { get; set; }
    public string? NoteStatus { get; set; }
    public List<RecordingNoteSectionRecord>? NoteSections { get; set; }
    public string? SourceDescription { get; set; }
    public bool IsTrashed { get; set; }
    public DateTime? TrashedAt { get; set; }
    public string? ModifiedByDeviceId { get; set; }
    public string? SyncConflictStatus { get; set; }

    [JsonIgnore]
    public StudyFilingPath FilingPath => Filing;

    [JsonIgnore]
    public StudyFilingPath? StudyFiling
    {
        get => Filing.IsEmpty ? null : Filing;
        set
        {
            Filing = value?.IsEmpty == true ? new StudyFilingPath() : (value ?? new StudyFilingPath());
            FolderIds = DefaultFolderIdsFor(Filing);
        }
    }

    [JsonIgnore]
    public bool HasTranscript => TranscriptMarkdownRelativePath is not null || TranscriptRelativePath is not null;
    [JsonIgnore]
    public bool HasNote => NoteRelativePath is not null;

    public StudyItemMetadata()
    {
        ItemId = "";
        Title = "";
        Filing = new StudyFilingPath();
        Tags = new List<StudyTag>();
        FolderIds = new List<string>();
        CustomProperties = new Dictionary<string, string>();
    }

    [JsonConstructor]
    public StudyItemMetadata(
        string itemId, StudyItemKind kind, string title, DateTime createdAt, DateTime updatedAt,
        StudyFilingPath filing, List<StudyTag> tags, List<string> folderIds,
        Dictionary<string, string> customProperties, string? recordingId, string? sanitizedRecordingId,
        TimeSpan? duration, string? audioRelativePath, string? receiveRelativePath,
        string? transcriptRelativePath, string? transcriptMarkdownRelativePath,
        string? noteRelativePath, string? transcriptionStatus, string? noteStatus,
        List<RecordingNoteSectionRecord>? noteSections, string? sourceDescription,
        bool isTrashed, DateTime? trashedAt, string? modifiedByDeviceId, string? syncConflictStatus)
    {
        ItemId = itemId;
        Kind = kind;
        Title = title;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Filing = filing;
        Tags = tags;
        FolderIds = folderIds;
        CustomProperties = customProperties;
        RecordingId = recordingId;
        SanitizedRecordingId = sanitizedRecordingId;
        Duration = duration;
        AudioRelativePath = audioRelativePath;
        ReceiveRelativePath = receiveRelativePath;
        TranscriptRelativePath = transcriptRelativePath;
        TranscriptMarkdownRelativePath = transcriptMarkdownRelativePath;
        NoteRelativePath = noteRelativePath;
        TranscriptionStatus = transcriptionStatus;
        NoteStatus = noteStatus;
        NoteSections = noteSections;
        SourceDescription = sourceDescription;
        IsTrashed = isTrashed;
        TrashedAt = trashedAt;
        ModifiedByDeviceId = modifiedByDeviceId;
        SyncConflictStatus = syncConflictStatus;
    }

    public static StudyItemMetadata DefaultForRecording(RecordingMetadata recording)
    {
        return new StudyItemMetadata
        {
            ItemId = RecordingBundleItemId(recording.Id),
            Kind = StudyItemKind.RecordingBundle,
            Title = recording.Title,
            CreatedAt = recording.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            Filing = recording.StudyFiling ?? new StudyFilingPath(),
            Tags = recording.Tags.Select(t => new StudyTag(value: t)).ToList(),
            FolderIds = DefaultFolderIdsFor(recording.StudyFiling ?? new StudyFilingPath()),
            RecordingId = recording.Id,
            SanitizedRecordingId = StudyPathSanitizer.SanitizedPathComponent(recording.Id),
            Duration = recording.Duration,
            AudioRelativePath = recording.RelativeAudioPath,
            TranscriptionStatus = recording.TranscriptionStatus,
            NoteStatus = recording.NoteStatus,
            SourceDescription = "Windows",
            IsTrashed = recording.IsDeleted,
            TrashedAt = recording.DeletedAt
        };
    }

    public StudyItemMetadata MergedWithCurrentRecording(RecordingMetadata recording)
    {
        var resolvedFiling = Filing.IsEmpty ? (recording.StudyFiling ?? new StudyFilingPath()) : Filing;
        var resolvedFolderIds = FolderIds.Count == 0 ? DefaultFolderIdsFor(resolvedFiling) : FolderIds;
        return new StudyItemMetadata
        {
            ItemId = ItemId,
            Kind = StudyItemKind.RecordingBundle,
            Title = recording.Title,
            CreatedAt = recording.CreatedAt,
            UpdatedAt = UpdatedAt,
            Filing = resolvedFiling,
            Tags = Tags,
            FolderIds = resolvedFolderIds,
            CustomProperties = CustomProperties,
            RecordingId = recording.Id,
            SanitizedRecordingId = SanitizedRecordingId,
            Duration = recording.Duration,
            AudioRelativePath = recording.RelativeAudioPath,
            ReceiveRelativePath = ReceiveRelativePath,
            TranscriptRelativePath = TranscriptRelativePath,
            TranscriptMarkdownRelativePath = TranscriptMarkdownRelativePath,
            NoteRelativePath = NoteRelativePath,
            TranscriptionStatus = recording.TranscriptionStatus,
            NoteStatus = recording.NoteStatus,
            NoteSections = NoteSections,
            SourceDescription = SourceDescription,
            IsTrashed = IsTrashed || recording.IsDeleted,
            TrashedAt = recording.DeletedAt ?? TrashedAt,
            ModifiedByDeviceId = ModifiedByDeviceId,
            SyncConflictStatus = SyncConflictStatus
        };
    }

    public static string RecordingBundleItemId(string recordingId) =>
        $"item_recording_{StudyPathSanitizer.SanitizedPathComponent(recordingId)}";

    public static List<string> DefaultFolderIdsFor(StudyFilingPath filing)
    {
        var effective = EffectiveFolderPath(filing);
        var deepest = StudyFolderMetadata.DeepestLevelIn(effective);
        return deepest is not null ? new List<string> { StudyFolderMetadata.FolderIdFor(deepest.Value, effective) } : new List<string>();
    }

    public static StudyFilingPath EffectiveFolderPath(StudyFilingPath filing)
    {
        if (filing.IsEmpty)
            return new StudyFilingPath(type: StudyHierarchyRule.UncategorizedValue);

        var hasTopic = filing.Topic is not null;
        var hasChapter = filing.Chapter is not null || hasTopic;
        var hasSubject = filing.Subject is not null || hasChapter;

        return new StudyFilingPath(
            type: filing.Type ?? StudyHierarchyRule.UncategorizedValue,
            subject: hasSubject ? (filing.Subject ?? StudyHierarchyRule.MissingValue) : null,
            chapter: hasChapter ? (filing.Chapter ?? StudyHierarchyRule.MissingValue) : null,
            topic: filing.Topic
        );
    }

    public StudyItemMetadata SyncSanitized(string modifiedByDeviceId)
    {
        return new StudyItemMetadata
        {
            ItemId = ItemId, Kind = Kind, Title = Title, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt,
            Filing = Filing, Tags = Tags, FolderIds = FolderIds, CustomProperties = CustomProperties,
            RecordingId = RecordingId, SanitizedRecordingId = SanitizedRecordingId,
            Duration = Duration, AudioRelativePath = AudioRelativePath,
            ReceiveRelativePath = ReceiveRelativePath, TranscriptRelativePath = TranscriptRelativePath,
            TranscriptMarkdownRelativePath = TranscriptMarkdownRelativePath,
            NoteRelativePath = NoteRelativePath, TranscriptionStatus = TranscriptionStatus,
            NoteStatus = NoteStatus, NoteSections = NoteSections, SourceDescription = SourceDescription,
            IsTrashed = IsTrashed, TrashedAt = TrashedAt,
            ModifiedByDeviceId = modifiedByDeviceId, SyncConflictStatus = SyncConflictStatus
        };
    }
}

public sealed class RecordingNoteSectionRecord
{
    public int Index { get; set; }
    public int? SourceStart { get; set; }
    public int? SourceEnd { get; set; }
    public string? Status { get; set; }
    public string? SectionNoteRelativePath { get; set; }
}

public sealed class RecordingTranscriptionChunkRecord
{
    public int Index { get; set; }
    public TimeSpan? Start { get; set; }
    public TimeSpan? End { get; set; }
    public string? Status { get; set; }
}

public static class StudyPathSanitizer
{
    public static string SanitizedPathComponent(string value)
    {
        var sanitized = SanitizedFileName(value).Replace(".", "_").Trim('_', '-', ' ');
        return string.IsNullOrEmpty(sanitized) ? "recording" : sanitized;
    }

    private static string SanitizedFileName(string? value)
    {
        var raw = value?.Trim() ?? "";
        if (string.IsNullOrEmpty(raw)) raw = "recording";

        var lastComponent = Path.GetFileName(raw);
        var allowed = new System.Text.StringBuilder();
        foreach (var c in lastComponent)
        {
            if (char.IsLetterOrDigit(c) || c is '.' or '_' or '-')
                allowed.Append(c);
            else
                allowed.Append('_');
        }

        var result = System.Text.RegularExpressions.Regex.Replace(allowed.ToString(), "_+", "_");
        return result.Trim('.', ' ');
    }
}

public static class StudyHierarchyRule
{
    public const string UncategorizedValue = "未分类";
    public const string MissingValue = "未填写";
}
