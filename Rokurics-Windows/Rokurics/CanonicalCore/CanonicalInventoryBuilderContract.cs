using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

public sealed class CanonicalInventoryUnsupportedObject : IEquatable<CanonicalInventoryUnsupportedObject>
{
    public string Id => ObjectID.RawValue;

    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalObjectKind ObjectKind { get; }
    public string Reason { get; }

    [JsonConstructor]
    public CanonicalInventoryUnsupportedObject(
        CanonicalLibraryObjectID objectID,
        CanonicalObjectKind objectKind,
        string reason)
    {
        ObjectID = objectID;
        ObjectKind = objectKind;
        Reason = reason;
    }

    public override bool Equals(object? obj) => obj is CanonicalInventoryUnsupportedObject other && Equals(other);
    public bool Equals(CanonicalInventoryUnsupportedObject? other) =>
        other is not null && ObjectID.Equals(other.ObjectID) && ObjectKind == other.ObjectKind;
    public override int GetHashCode() => System.HashCode.Combine(ObjectID, ObjectKind);
    public static bool operator ==(CanonicalInventoryUnsupportedObject left, CanonicalInventoryUnsupportedObject right) => left.Equals(right);
    public static bool operator !=(CanonicalInventoryUnsupportedObject left, CanonicalInventoryUnsupportedObject right) => !left.Equals(right);
}

public sealed class CanonicalInventoryInputSnapshot : IEquatable<CanonicalInventoryInputSnapshot>
{
    public CanonicalNode Node { get; }
    public CanonicalTimestamp GeneratedAt { get; }
    public CanonicalRecordingObject[] RecordingObjects { get; }
    public CanonicalLibraryObject[] LibraryObjects { get; }
    public CanonicalLibraryTombstone[] LibraryTombstones { get; }
    public CanonicalInventoryUnsupportedObject[] UnsupportedObjects { get; }

    public CanonicalInventoryInputSnapshot(
        CanonicalNode node,
        DateTime? generatedAt = null,
        CanonicalRecordingObject[]? recordingObjects = null,
        CanonicalLibraryObject[]? libraryObjects = null,
        CanonicalLibraryTombstone[]? libraryTombstones = null,
        CanonicalInventoryUnsupportedObject[]? unsupportedObjects = null)
    {
        Node = node;
        GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
        RecordingObjects = recordingObjects ?? System.Array.Empty<CanonicalRecordingObject>();
        LibraryObjects = libraryObjects ?? System.Array.Empty<CanonicalLibraryObject>();
        LibraryTombstones = libraryTombstones ?? System.Array.Empty<CanonicalLibraryTombstone>();
        UnsupportedObjects = unsupportedObjects ?? System.Array.Empty<CanonicalInventoryUnsupportedObject>();
    }

    [JsonConstructor]
    public CanonicalInventoryInputSnapshot(
        CanonicalNode node,
        CanonicalTimestamp generatedAt,
        CanonicalRecordingObject[]? recordingObjects,
        CanonicalLibraryObject[]? libraryObjects,
        CanonicalLibraryTombstone[]? libraryTombstones,
        CanonicalInventoryUnsupportedObject[]? unsupportedObjects)
        : this(node, null, recordingObjects, libraryObjects, libraryTombstones, unsupportedObjects)
    {
        GeneratedAt = generatedAt;
    }

    public override bool Equals(object? obj) => obj is CanonicalInventoryInputSnapshot other && Equals(other);
    public bool Equals(CanonicalInventoryInputSnapshot? other) =>
        other is not null && Node.Equals(other.Node) && GeneratedAt.Equals(other.GeneratedAt);
    public override int GetHashCode() => System.HashCode.Combine(Node, GeneratedAt);
    public static bool operator ==(CanonicalInventoryInputSnapshot left, CanonicalInventoryInputSnapshot right) => left.Equals(right);
    public static bool operator !=(CanonicalInventoryInputSnapshot left, CanonicalInventoryInputSnapshot right) => !left.Equals(right);
}

public sealed class CanonicalInventoryCoverageReport : IEquatable<CanonicalInventoryCoverageReport>
{
    public int RecordingCoverage { get; }
    public int AudioCoverage { get; }
    public int GeneratedArtifactCoverage { get; }
    public int FolderCoverage { get; }
    public int StudyItemCoverage { get; }
    public int TombstoneCoverage { get; }
    public int UnsupportedLegacyObjectCount { get; }
    public int FallbackRequiredCount { get; }

    public CanonicalInventoryCoverageReport(
        int recordingCoverage,
        int audioCoverage,
        int generatedArtifactCoverage,
        int folderCoverage,
        int studyItemCoverage,
        int tombstoneCoverage,
        int unsupportedLegacyObjectCount,
        int fallbackRequiredCount)
    {
        RecordingCoverage = recordingCoverage;
        AudioCoverage = audioCoverage;
        GeneratedArtifactCoverage = generatedArtifactCoverage;
        FolderCoverage = folderCoverage;
        StudyItemCoverage = studyItemCoverage;
        TombstoneCoverage = tombstoneCoverage;
        UnsupportedLegacyObjectCount = unsupportedLegacyObjectCount;
        FallbackRequiredCount = fallbackRequiredCount;
    }

    public override bool Equals(object? obj) => obj is CanonicalInventoryCoverageReport other && Equals(other);
    public bool Equals(CanonicalInventoryCoverageReport? other) =>
        other is not null &&
        RecordingCoverage == other.RecordingCoverage &&
        AudioCoverage == other.AudioCoverage &&
        GeneratedArtifactCoverage == other.GeneratedArtifactCoverage &&
        FolderCoverage == other.FolderCoverage &&
        StudyItemCoverage == other.StudyItemCoverage &&
        TombstoneCoverage == other.TombstoneCoverage &&
        UnsupportedLegacyObjectCount == other.UnsupportedLegacyObjectCount &&
        FallbackRequiredCount == other.FallbackRequiredCount;
    public override int GetHashCode() =>
        System.HashCode.Combine(RecordingCoverage, AudioCoverage, GeneratedArtifactCoverage,
            FolderCoverage, StudyItemCoverage, TombstoneCoverage,
            UnsupportedLegacyObjectCount, FallbackRequiredCount);
    public static bool operator ==(CanonicalInventoryCoverageReport left, CanonicalInventoryCoverageReport right) => left.Equals(right);
    public static bool operator !=(CanonicalInventoryCoverageReport left, CanonicalInventoryCoverageReport right) => !left.Equals(right);
}

public sealed class CanonicalInventoryBuildDiagnostics : IEquatable<CanonicalInventoryBuildDiagnostics>
{
    public string[] Phases { get; }
    public string[] UnsupportedReasons { get; }

    public CanonicalInventoryBuildDiagnostics(
        string[] phases,
        string[] unsupportedReasons)
    {
        Phases = phases;
        UnsupportedReasons = unsupportedReasons;
    }

    public override bool Equals(object? obj) => obj is CanonicalInventoryBuildDiagnostics other && Equals(other);
    public bool Equals(CanonicalInventoryBuildDiagnostics? other) =>
        other is not null &&
        Phases.SequenceEqual(other.Phases) &&
        UnsupportedReasons.SequenceEqual(other.UnsupportedReasons);
    public override int GetHashCode() =>
        System.HashCode.Combine(Phases.Length, UnsupportedReasons.Length);
    public static bool operator ==(CanonicalInventoryBuildDiagnostics left, CanonicalInventoryBuildDiagnostics right) => left.Equals(right);
    public static bool operator !=(CanonicalInventoryBuildDiagnostics left, CanonicalInventoryBuildDiagnostics right) => !left.Equals(right);
}

public sealed class CanonicalInventoryBuildResult : IEquatable<CanonicalInventoryBuildResult>
{
    public CanonicalManifest Manifest { get; }
    public CanonicalInventoryCoverageReport Coverage { get; }
    public CanonicalInventoryBuildDiagnostics Diagnostics { get; }

    public CanonicalInventoryBuildResult(
        CanonicalManifest manifest,
        CanonicalInventoryCoverageReport coverage,
        CanonicalInventoryBuildDiagnostics diagnostics)
    {
        Manifest = manifest;
        Coverage = coverage;
        Diagnostics = diagnostics;
    }

    public override bool Equals(object? obj) => obj is CanonicalInventoryBuildResult other && Equals(other);
    public bool Equals(CanonicalInventoryBuildResult? other) =>
        other is not null &&
        Manifest.Equals(other.Manifest) &&
        Coverage.Equals(other.Coverage) &&
        Diagnostics.Equals(other.Diagnostics);
    public override int GetHashCode() => System.HashCode.Combine(Manifest, Coverage, Diagnostics);
    public static bool operator ==(CanonicalInventoryBuildResult left, CanonicalInventoryBuildResult right) => left.Equals(right);
    public static bool operator !=(CanonicalInventoryBuildResult left, CanonicalInventoryBuildResult right) => !left.Equals(right);
}

public sealed class CanonicalInventoryBuilderContract
{
    public CanonicalInventoryBuildResult Build(CanonicalInventoryInputSnapshot snapshot)
    {
        var folders = snapshot.LibraryObjects
            .Select(o => o.Folder)
            .Where(f => f != null)
            .Cast<CanonicalFolderObject>()
            .ToArray();

        var studyItems = snapshot.LibraryObjects
            .Select(o => o.StudyItem ?? o.StandaloneNote?.StudyItem)
            .Where(si => si != null)
            .Cast<CanonicalStudyItemObject>()
            .ToArray();

        var standaloneNotes = snapshot.LibraryObjects
            .Select(o => o.StandaloneNote)
            .Where(sn => sn != null)
            .Cast<CanonicalStandaloneNoteObject>()
            .ToArray();

        var generatedArtifactCount = snapshot.RecordingObjects.Sum(o =>
            o.Artifacts.Count(a => CanonicalProjectionContract.GeneratedArtifactKinds.Contains(a.ArtifactKind))
        );

        var audioCoverage = snapshot.RecordingObjects.Count(o => o.AudioAvailable);
        var fallbackCount = snapshot.UnsupportedObjects.Length;

        var capabilities = new CanonicalCapability[]
        {
            CanonicalCapability.canonicalLibraryObjectsV1,
            CanonicalCapability.canonicalFolderObjectsV1,
            CanonicalCapability.canonicalStudyItemObjectsV1,
            CanonicalCapability.canonicalInventoryBuilderV1
        };

        var manifest = CanonicalManifest.Make(
            node: snapshot.Node,
            generatedAt: snapshot.GeneratedAt.Date,
            objects: snapshot.RecordingObjects,
            libraryObjects: snapshot.LibraryObjects,
            folders: folders,
            studyItems: studyItems,
            standaloneNotes: standaloneNotes,
            libraryTombstones: snapshot.LibraryTombstones,
            manifestCapabilities: capabilities
        );

        var coverage = new CanonicalInventoryCoverageReport(
            recordingCoverage: snapshot.RecordingObjects.Length,
            audioCoverage: audioCoverage,
            generatedArtifactCoverage: generatedArtifactCount,
            folderCoverage: folders.Length,
            studyItemCoverage: studyItems.Length,
            tombstoneCoverage: snapshot.LibraryTombstones.Length,
            unsupportedLegacyObjectCount: snapshot.UnsupportedObjects.Length,
            fallbackRequiredCount: fallbackCount
        );

        var diagnostics = new CanonicalInventoryBuildDiagnostics(
            phases: new[]
            {
                "canonicalInventoryCoverageReportWritten",
                "canonicalLibraryObjectsProjected"
            },
            unsupportedReasons: snapshot.UnsupportedObjects
                .Select(u => u.Reason)
                .OrderBy(r => r, System.StringComparer.Ordinal)
                .ToArray()
        );

        return new CanonicalInventoryBuildResult(
            manifest: manifest,
            coverage: coverage,
            diagnostics: diagnostics
        );
    }
}
