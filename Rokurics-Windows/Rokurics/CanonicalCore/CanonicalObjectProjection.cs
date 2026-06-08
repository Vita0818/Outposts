using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalDisplayState
{
    localOnly,
    waitingForAudio,
    uploadingAudio,
    audioAvailable,
    metadataSynced,
    transcriptAvailable,
    noteAvailable,
    summaryAvailable,
    processing,
    failed,
    retryPending,
    conflict,
    deleted,
    tombstoned,
    available,
    syncing,
    unsupported,
    unknown
}

public sealed class CanonicalActionAvailability : IEquatable<CanonicalActionAvailability>
{
    public bool CanUploadAudio { get; }
    public bool CanRequestGeneratedArtifact { get; }
    public bool CanApplyMetadata { get; }
    public bool CanResolveConflict { get; }

    public CanonicalActionAvailability(
        bool canUploadAudio,
        bool canRequestGeneratedArtifact,
        bool canApplyMetadata,
        bool canResolveConflict)
    {
        CanUploadAudio = canUploadAudio;
        CanRequestGeneratedArtifact = canRequestGeneratedArtifact;
        CanApplyMetadata = canApplyMetadata;
        CanResolveConflict = canResolveConflict;
    }

    public static readonly CanonicalActionAvailability ReadOnly = new(
        canUploadAudio: false,
        canRequestGeneratedArtifact: false,
        canApplyMetadata: false,
        canResolveConflict: false
    );

    public override bool Equals(object? obj) => obj is CanonicalActionAvailability other && Equals(other);
    public bool Equals(CanonicalActionAvailability? other) =>
        other is not null &&
        CanUploadAudio == other.CanUploadAudio &&
        CanRequestGeneratedArtifact == other.CanRequestGeneratedArtifact &&
        CanApplyMetadata == other.CanApplyMetadata &&
        CanResolveConflict == other.CanResolveConflict;
    public override int GetHashCode() =>
        System.HashCode.Combine(CanUploadAudio, CanRequestGeneratedArtifact, CanApplyMetadata, CanResolveConflict);
    public static bool operator ==(CanonicalActionAvailability left, CanonicalActionAvailability right) => left.Equals(right);
    public static bool operator !=(CanonicalActionAvailability left, CanonicalActionAvailability right) => !left.Equals(right);
}

public sealed class CanonicalRecordingProjection : IEquatable<CanonicalRecordingProjection>
{
    public string Id => ObjectID;

    public string ObjectID { get; }
    public string Title { get; }
    public CanonicalDisplayState[] DisplayStates { get; }
    public CanonicalActionAvailability ActionAvailability { get; }
    public string? MetadataHashPrefix { get; }
    public string? AudioHashPrefix { get; }

    [JsonConstructor]
    public CanonicalRecordingProjection(
        string objectID,
        string title,
        CanonicalDisplayState[] displayStates,
        CanonicalActionAvailability actionAvailability,
        string? metadataHashPrefix = null,
        string? audioHashPrefix = null)
    {
        ObjectID = objectID;
        Title = title;
        DisplayStates = displayStates;
        ActionAvailability = actionAvailability;
        MetadataHashPrefix = metadataHashPrefix;
        AudioHashPrefix = audioHashPrefix;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingProjection other && Equals(other);
    public bool Equals(CanonicalRecordingProjection? other) =>
        other is not null && ObjectID == other.ObjectID;
    public override int GetHashCode() => ObjectID.GetHashCode();
    public static bool operator ==(CanonicalRecordingProjection left, CanonicalRecordingProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingProjection left, CanonicalRecordingProjection right) => !left.Equals(right);
}

public sealed class CanonicalFolderProjection : IEquatable<CanonicalFolderProjection>
{
    public string Id => FolderID.RawValue;

    public CanonicalLibraryObjectID FolderID { get; }
    public string Title { get; }
    public CanonicalDisplayState DisplayState { get; }
    public CanonicalActionAvailability ActionAvailability { get; }

    [JsonConstructor]
    public CanonicalFolderProjection(
        CanonicalLibraryObjectID folderID,
        string title,
        CanonicalDisplayState displayState,
        CanonicalActionAvailability actionAvailability)
    {
        FolderID = folderID;
        Title = title;
        DisplayState = displayState;
        ActionAvailability = actionAvailability;
    }

    public override bool Equals(object? obj) => obj is CanonicalFolderProjection other && Equals(other);
    public bool Equals(CanonicalFolderProjection? other) =>
        other is not null && FolderID.Equals(other.FolderID);
    public override int GetHashCode() => FolderID.GetHashCode();
    public static bool operator ==(CanonicalFolderProjection left, CanonicalFolderProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalFolderProjection left, CanonicalFolderProjection right) => !left.Equals(right);
}

public sealed class CanonicalStudyItemProjection : IEquatable<CanonicalStudyItemProjection>
{
    public string Id => ItemID.RawValue;

    public CanonicalLibraryObjectID ItemID { get; }
    public string Title { get; }
    public CanonicalDisplayState DisplayState { get; }
    public CanonicalActionAvailability ActionAvailability { get; }

    [JsonConstructor]
    public CanonicalStudyItemProjection(
        CanonicalLibraryObjectID itemID,
        string title,
        CanonicalDisplayState displayState,
        CanonicalActionAvailability actionAvailability)
    {
        ItemID = itemID;
        Title = title;
        DisplayState = displayState;
        ActionAvailability = actionAvailability;
    }

    public override bool Equals(object? obj) => obj is CanonicalStudyItemProjection other && Equals(other);
    public bool Equals(CanonicalStudyItemProjection? other) =>
        other is not null && ItemID.Equals(other.ItemID);
    public override int GetHashCode() => ItemID.GetHashCode();
    public static bool operator ==(CanonicalStudyItemProjection left, CanonicalStudyItemProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalStudyItemProjection left, CanonicalStudyItemProjection right) => !left.Equals(right);
}

public sealed class CanonicalLibraryProjection : IEquatable<CanonicalLibraryProjection>
{
    public CanonicalRecordingProjection[] Recordings { get; }
    public CanonicalFolderProjection[] Folders { get; }
    public CanonicalStudyItemProjection[] StudyItems { get; }
    public CanonicalTimestamp BuiltAt { get; }

    public CanonicalLibraryProjection(
        CanonicalRecordingProjection[] recordings,
        CanonicalFolderProjection[] folders,
        CanonicalStudyItemProjection[] studyItems,
        CanonicalTimestamp builtAt)
    {
        Recordings = recordings;
        Folders = folders;
        StudyItems = studyItems;
        BuiltAt = builtAt;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryProjection other && Equals(other);
    public bool Equals(CanonicalLibraryProjection? other) =>
        other is not null &&
        Recordings.SequenceEqual(other.Recordings) &&
        Folders.SequenceEqual(other.Folders) &&
        StudyItems.SequenceEqual(other.StudyItems);
    public override int GetHashCode() =>
        System.HashCode.Combine(Recordings.Length, Folders.Length, StudyItems.Length);
    public static bool operator ==(CanonicalLibraryProjection left, CanonicalLibraryProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryProjection left, CanonicalLibraryProjection right) => !left.Equals(right);
}

public static class CanonicalObjectProjectionBuilder
{
    public static CanonicalLibraryProjection Build(
        CanonicalManifest manifest,
        CanonicalApplyPlan? applyPlan = null,
        CanonicalLibrarySyncPlan? libraryPlan = null,
        CanonicalTransferProjection? transferProjection = null,
        DateTime? builtAt = null)
    {
        var conflicts = new HashSet<string>(
            (applyPlan?.Conflicts ?? System.Array.Empty<CanonicalApplyPlan.ApplyConflict>())
                .Select(c => c.Target.ObjectID)
        );
        var libraryConflicts = new HashSet<string>(
            (libraryPlan?.Conflicts ?? System.Array.Empty<CanonicalLibrarySyncPlan.SyncConflict>())
                .Select(c => c.ObjectID.RawValue)
        );
        var transferByObject = (transferProjection?.Jobs ?? System.Array.Empty<CanonicalTransferJob>())
            .GroupBy(j => j.ObjectID)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var recordings = manifest.Objects
            .Select(obj => RecordingProjection(obj, conflicts, transferByObject.TryGetValue(obj.ObjectID, out var jobs) ? jobs : System.Array.Empty<CanonicalTransferJob>()))
            .OrderBy(r => r.ObjectID, System.StringComparer.Ordinal)
            .ToArray();

        var folders = manifest.LibraryObjects
            .Where(obj => obj.Kind == CanonicalObjectKind.folder && obj.Folder != null)
            .Select(obj => FolderProjection(obj.Folder!, libraryConflicts.Contains(obj.ObjectID.RawValue)))
            .OrderBy(f => f.FolderID.RawValue, System.StringComparer.Ordinal)
            .ToArray();

        var studyItems = manifest.LibraryObjects
            .Where(obj =>
                obj.Kind == CanonicalObjectKind.standaloneStudyItem
                || obj.Kind == CanonicalObjectKind.standaloneNote
                || obj.Kind == CanonicalObjectKind.recordingAssociatedStudyItem)
            .Select(obj => obj.StudyItem ?? obj.StandaloneNote?.StudyItem)
            .Where(item => item != null)
            .Select(item => StudyItemProjection(item!, libraryConflicts.Contains(item!.ItemID.RawValue)))
            .OrderBy(s => s.ItemID.RawValue, System.StringComparer.Ordinal)
            .ToArray();

        return new CanonicalLibraryProjection(
            recordings: recordings,
            folders: folders,
            studyItems: studyItems,
            builtAt: new CanonicalTimestamp(builtAt ?? DateTime.UtcNow)
        );
    }

    private static CanonicalRecordingProjection RecordingProjection(
        CanonicalRecordingObject obj,
        HashSet<string> conflicts,
        CanonicalTransferJob[] transferJobs)
    {
        var states = new System.Collections.Generic.List<CanonicalDisplayState>();

        if (obj.Metadata.IsDeleted)
            states.Add(CanonicalDisplayState.deleted);

        if (conflicts.Contains(obj.ObjectID) || obj.SyncState == CanonicalSyncState.conflict)
            states.Add(CanonicalDisplayState.conflict);

        if (transferJobs.Any(j => j.Phase is CanonicalTransferPhase.inFlight or CanonicalTransferPhase.queued or CanonicalTransferPhase.planned))
            states.Add(CanonicalDisplayState.uploadingAudio);

        if (transferJobs.Any(j => j.Phase == CanonicalTransferPhase.failedRetryable))
            states.Add(CanonicalDisplayState.retryPending);

        if (transferJobs.Any(j => j.Phase == CanonicalTransferPhase.failedFatal))
            states.Add(CanonicalDisplayState.failed);

        if (obj.AudioAvailable)
            states.Add(CanonicalDisplayState.audioAvailable);
        else if (!obj.Metadata.IsDeleted)
            states.Add(CanonicalDisplayState.waitingForAudio);

        if (GeneratedAvailable(obj, CanonicalArtifact.Kind.transcriptMarkdown)
            || GeneratedAvailable(obj, CanonicalArtifact.Kind.transcriptJSON))
            states.Add(CanonicalDisplayState.transcriptAvailable);

        if (GeneratedAvailable(obj, CanonicalArtifact.Kind.noteMarkdown)
            || GeneratedAvailable(obj, CanonicalArtifact.Kind.noteJSON))
            states.Add(CanonicalDisplayState.noteAvailable);

        if (GeneratedAvailable(obj, CanonicalArtifact.Kind.summaryJSON))
            states.Add(CanonicalDisplayState.summaryAvailable);

        if (states.Count == 0)
            states.Add(CanonicalDisplayState.metadataSynced);

        var audio = obj.AudioArtifact;

        return new CanonicalRecordingProjection(
            objectID: obj.ObjectID,
            title: obj.Metadata.Title,
            displayStates: Unique(states),
            actionAvailability: CanonicalActionAvailability.ReadOnly,
            metadataHashPrefix: obj.MetadataHash.Value.Length >= 12
                ? obj.MetadataHash.Value[..12]
                : obj.MetadataHash.Value,
            audioHashPrefix: audio?.ContentHash != null
                ? (audio.ContentHash.Value.Value.Length >= 12
                    ? audio.ContentHash.Value.Value[..12]
                    : audio.ContentHash.Value.Value)
                : null
        );
    }

    private static CanonicalFolderProjection FolderProjection(CanonicalFolderObject folder, bool hasConflict) =>
        new(
            folderID: folder.FolderID,
            title: folder.Metadata.Name,
            displayState: DisplayState(isDeleted: folder.Metadata.IsDeleted, hasConflict: hasConflict),
            actionAvailability: CanonicalActionAvailability.ReadOnly
        );

    private static CanonicalStudyItemProjection StudyItemProjection(CanonicalStudyItemObject item, bool hasConflict) =>
        new(
            itemID: item.ItemID,
            title: item.Metadata.Title,
            displayState: DisplayState(isDeleted: item.Metadata.IsDeleted, hasConflict: hasConflict),
            actionAvailability: CanonicalActionAvailability.ReadOnly
        );

    private static CanonicalDisplayState DisplayState(bool isDeleted, bool hasConflict)
    {
        if (isDeleted)
            return CanonicalDisplayState.tombstoned;
        if (hasConflict)
            return CanonicalDisplayState.conflict;
        return CanonicalDisplayState.available;
    }

    private static bool GeneratedAvailable(CanonicalRecordingObject obj, CanonicalArtifact.Kind kind) =>
        obj.Artifacts.Any(a =>
            a.ArtifactKind == kind && a.ProvesCanonicalGeneratedArtifactAvailability);

    private static CanonicalDisplayState[] Unique(System.Collections.Generic.List<CanonicalDisplayState> states)
    {
        var seen = new HashSet<CanonicalDisplayState>();
        return states.Where(s => seen.Add(s)).ToArray();
    }
}
