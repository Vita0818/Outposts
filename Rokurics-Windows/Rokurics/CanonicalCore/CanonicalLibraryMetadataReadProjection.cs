using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadProjectionSource
{
    legacy,
    canonical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadProjectionFailureKind
{
    snapshotMissing,
    missingMetadata,
    unsupportedObject,
    pathLeakRisk,
    fullContentRejected
}

public sealed record CanonicalLibraryMetadataReadProjectionFailure : IEquatable<CanonicalLibraryMetadataReadProjectionFailure>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", Reason);

    public CanonicalLibraryMetadataReadProjectionFailureKind Kind { get; }
    public string? ObjectID { get; }
    public CanonicalObjectKind? ObjectKind { get; }
    public string Reason { get; }

    public CanonicalLibraryMetadataReadProjectionFailure(
        CanonicalLibraryMetadataReadProjectionFailureKind kind,
        string? objectID = null,
        CanonicalObjectKind? objectKind = null,
        string reason = "")
    {
        Kind = kind;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "library-object") : null;
        ObjectKind = objectKind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? kind.ToString();
    }

    public virtual bool Equals(CanonicalLibraryMetadataReadProjectionFailure? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadProjectionFolder : IEquatable<CanonicalLibraryMetadataReadProjectionFolder>
{
    public string Id => FolderID.RawValue;

    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalLibraryObjectID FolderID { get; }
    public string Title { get; }
    public CanonicalLibraryObjectID? ParentID { get; }
    public string[] HierarchyPath { get; }
    public string? HierarchyLevel { get; }
    public string? ColorToken { get; }
    public string? OrderingKey { get; }
    public bool IsDeleted { get; }
    public string? MetadataHashPrefix { get; }

    public CanonicalLibraryMetadataReadProjectionFolder(CanonicalFolderObject folder)
    {
        ObjectID = folder.FolderID;
        FolderID = folder.FolderID;
        Title = folder.Metadata.Name;
        ParentID = folder.Metadata.ParentID;
        HierarchyPath = folder.Metadata.HierarchyPath.Components;
        HierarchyLevel = folder.Metadata.HierarchyLevel;
        ColorToken = folder.Metadata.ColorToken;
        OrderingKey = folder.Metadata.OrderingKey;
        IsDeleted = folder.Metadata.IsDeleted;
        MetadataHashPrefix = CanonicalProductionRedaction.HashPrefix(folder.MetadataHash.Value);
    }

    public string HierarchyKey => string.Join(">", HierarchyPath);

    public virtual bool Equals(CanonicalLibraryMetadataReadProjectionFolder? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadProjectionItem : IEquatable<CanonicalLibraryMetadataReadProjectionItem>
{
    public string Id => ItemID.RawValue;

    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalLibraryObjectID ItemID { get; }
    public string Title { get; }
    public CanonicalStudyItemKind ItemKind { get; }
    public CanonicalLibraryObjectID[] FolderIDs { get; }
    public CanonicalParentReference[] ParentReferences { get; }
    public string[] Tags { get; }
    public string[] FilingComponents { get; }
    public string ResourceTokenSummary { get; }
    public bool IsDeleted { get; }
    public string? MetadataHashPrefix { get; }
    public string? OrderingKey { get; }

    public CanonicalLibraryMetadataReadProjectionItem(CanonicalStudyItemObject item, CanonicalLibraryObjectID? objectID = null)
    {
        ObjectID = objectID ?? item.ItemID;
        ItemID = item.ItemID;
        Title = item.Metadata.Title;
        ItemKind = item.Metadata.ItemKind;
        FolderIDs = item.Metadata.FolderIDs;
        ParentReferences = item.Metadata.ParentReferences;
        Tags = item.Metadata.Tags;
        FilingComponents = item.Metadata.FilingPath.Components;
        ResourceTokenSummary = SafeResourceTokenSummary(item.Metadata.LogicalResourceTokens);
        IsDeleted = item.Metadata.IsDeleted;
        MetadataHashPrefix = CanonicalProductionRedaction.HashPrefix(item.MetadataHash.Value);
        OrderingKey = null;
    }

    public string FilingKey => string.Join(">", FilingComponents);

    public string FolderIDKey => string.Join("|", FolderIDs.Select(f => f.RawValue));

    public string ParentReferenceKey => string.Join("|", ParentReferences.Select(p => $"{p.Relation}:{p.ParentID.RawValue}"));

    public string TagKey => string.Join("|", Tags);

    public static string SafeResourceTokenSummary(string[] tokens)
    {
        var safePrefixes = tokens
            .Select(token =>
            {
                if (CanonicalProjectionContract.SafeLogicalResourceToken(token) == null
                    || CanonicalProductionRedaction.ContainsSensitivePathSignal(token))
                    return null;
                return CanonicalProductionRedaction.HashPrefix(CanonicalHash.Sha256String(token).Value);
            })
            .Where(p => p != null)
            .Cast<string>()
            .ToArray();
        return $"count={safePrefixes.Length},tokens={string.Join("+", safePrefixes)}";
    }

    public virtual bool Equals(CanonicalLibraryMetadataReadProjectionItem? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadProjectionNote : IEquatable<CanonicalLibraryMetadataReadProjectionNote>
{
    public string Id => NoteItemID.RawValue;

    public CanonicalLibraryObjectID ObjectID { get; }
    public CanonicalLibraryObjectID NoteItemID { get; }
    public string Title { get; }
    public CanonicalLibraryObjectID[] FolderIDs { get; }
    public CanonicalParentReference[] ParentReferences { get; }
    public string[] Tags { get; }
    public string[] FilingComponents { get; }
    public string ResourceTokenSummary { get; }
    public bool IsDeleted { get; }
    public string? MetadataHashPrefix { get; }
    public bool FullContentIncluded { get; }

    public CanonicalLibraryMetadataReadProjectionNote(CanonicalStudyItemObject item, CanonicalLibraryObjectID? objectID = null)
    {
        ObjectID = objectID ?? item.ItemID;
        NoteItemID = item.ItemID;
        Title = item.Metadata.Title;
        FolderIDs = item.Metadata.FolderIDs;
        ParentReferences = item.Metadata.ParentReferences;
        Tags = item.Metadata.Tags;
        FilingComponents = item.Metadata.FilingPath.Components;
        ResourceTokenSummary = CanonicalLibraryMetadataReadProjectionItem.SafeResourceTokenSummary(item.Metadata.LogicalResourceTokens);
        IsDeleted = item.Metadata.IsDeleted;
        MetadataHashPrefix = CanonicalProductionRedaction.HashPrefix(item.MetadataHash.Value);
        FullContentIncluded = false;
    }

    public string FilingKey => string.Join(">", FilingComponents);

    public string FolderIDKey => string.Join("|", FolderIDs.Select(f => f.RawValue));

    public string ParentReferenceKey => string.Join("|", ParentReferences.Select(p => $"{p.Relation}:{p.ParentID.RawValue}"));

    public string TagKey => string.Join("|", Tags);

    public virtual bool Equals(CanonicalLibraryMetadataReadProjectionNote? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadSnapshot : IEquatable<CanonicalLibraryMetadataReadSnapshot>
{
    public CanonicalLibraryMetadataReadProjectionSource Source { get; }
    public CanonicalTimestamp GeneratedAt { get; }
    public CanonicalLibraryMetadataReadProjectionFolder[] Folders { get; }
    public CanonicalLibraryMetadataReadProjectionItem[] StudyItems { get; }
    public CanonicalLibraryMetadataReadProjectionNote[] StandaloneNotes { get; }
    public CanonicalLibraryMetadataReadProjectionFailure[] Failures { get; }
    public int ContentExcludedCount { get; }

    public CanonicalLibraryMetadataReadSnapshot(
        CanonicalLibraryMetadataReadProjectionSource source,
        DateTime? generatedAt = null,
        CanonicalLibraryMetadataReadProjectionFolder[]? folders = null,
        CanonicalLibraryMetadataReadProjectionItem[]? studyItems = null,
        CanonicalLibraryMetadataReadProjectionNote[]? standaloneNotes = null,
        CanonicalLibraryMetadataReadProjectionFailure[]? failures = null,
        int contentExcludedCount = 0)
    {
        Source = source;
        GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
        Folders = (folders ?? Array.Empty<CanonicalLibraryMetadataReadProjectionFolder>())
            .OrderBy(f => f.FolderID.RawValue, StringComparer.Ordinal).ToArray();
        StudyItems = (studyItems ?? Array.Empty<CanonicalLibraryMetadataReadProjectionItem>())
            .OrderBy(i => i.ItemID.RawValue, StringComparer.Ordinal).ToArray();
        StandaloneNotes = (standaloneNotes ?? Array.Empty<CanonicalLibraryMetadataReadProjectionNote>())
            .OrderBy(n => n.NoteItemID.RawValue, StringComparer.Ordinal).ToArray();
        Failures = (failures ?? Array.Empty<CanonicalLibraryMetadataReadProjectionFailure>())
            .OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        ContentExcludedCount = Math.Max(0, contentExcludedCount);
    }

    public int ObjectCount => Folders.Length + StudyItems.Length + StandaloneNotes.Length;

    public int UnsupportedObjectCount =>
        Failures.Count(f => f.Kind == CanonicalLibraryMetadataReadProjectionFailureKind.unsupportedObject
                            || f.Kind == CanonicalLibraryMetadataReadProjectionFailureKind.missingMetadata);

    public int PathLeakRiskCount =>
        Failures.Count(f => f.Kind == CanonicalLibraryMetadataReadProjectionFailureKind.pathLeakRisk);

    public bool FullContentIncluded =>
        StandaloneNotes.Any(n => n.FullContentIncluded);

    public string DiagnosticsSummary => string.Join(",",
        $"source={Source}",
        $"folders={Folders.Length}",
        $"items={StudyItems.Length}",
        $"notes={StandaloneNotes.Length}",
        $"unsupported={UnsupportedObjectCount}",
        $"pathLeakRisk={PathLeakRiskCount}",
        $"contentExcluded={ContentExcludedCount}"
    );

    public virtual bool Equals(CanonicalLibraryMetadataReadSnapshot? other) =>
        other is not null && Source == other.Source && GeneratedAt.Equals(other.GeneratedAt);
    public override int GetHashCode() => HashCode.Combine(Source, GeneratedAt);
}

public sealed record CanonicalLibraryMetadataReadProjection : IEquatable<CanonicalLibraryMetadataReadProjection>
{
    public CanonicalLibraryMetadataReadSnapshot Snapshot { get; }

    public CanonicalLibraryMetadataReadProjection(CanonicalLibraryMetadataReadSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public static CanonicalLibraryMetadataReadProjection Build(
        CanonicalLibraryMetadataReadProjectionSource source,
        CanonicalManifest? manifest,
        DateTime? generatedAt = null)
    {
        if (manifest == null)
        {
            return new CanonicalLibraryMetadataReadProjection(
                new CanonicalLibraryMetadataReadSnapshot(
                    source,
                    generatedAt,
                    failures: new[]
                    {
                        new CanonicalLibraryMetadataReadProjectionFailure(
                            CanonicalLibraryMetadataReadProjectionFailureKind.snapshotMissing,
                            reason: "canonicalManifestMissing")
                    }
                )
            );
        }
        return Build(source, manifest.LibraryObjects, generatedAt);
    }

    public static CanonicalLibraryMetadataReadProjection Build(
        CanonicalLibraryMetadataReadProjectionSource source,
        CanonicalLibraryObject[] objects,
        DateTime? generatedAt = null)
    {
        var folders = new List<CanonicalLibraryMetadataReadProjectionFolder>();
        var studyItems = new List<CanonicalLibraryMetadataReadProjectionItem>();
        var standaloneNotes = new List<CanonicalLibraryMetadataReadProjectionNote>();
        var failures = new List<CanonicalLibraryMetadataReadProjectionFailure>();

        foreach (var obj in objects.OrderBy(o => o.ObjectID.RawValue, StringComparer.Ordinal))
        {
            switch (obj.Kind)
            {
                case CanonicalObjectKind.folder:
                    if (obj.Folder == null)
                    {
                        failures.Add(new CanonicalLibraryMetadataReadProjectionFailure(
                            CanonicalLibraryMetadataReadProjectionFailureKind.missingMetadata,
                            obj.ObjectID.RawValue, obj.Kind, "folderMetadataMissing"));
                        continue;
                    }
                    folders.Add(new CanonicalLibraryMetadataReadProjectionFolder(obj.Folder));
                    break;
                case CanonicalObjectKind.standaloneNote:
                    var noteItem = obj.StandaloneNote?.StudyItem ?? obj.StudyItem;
                    if (noteItem == null)
                    {
                        failures.Add(new CanonicalLibraryMetadataReadProjectionFailure(
                            CanonicalLibraryMetadataReadProjectionFailureKind.missingMetadata,
                            obj.ObjectID.RawValue, obj.Kind, "standaloneNoteMetadataMissing"));
                        continue;
                    }
                    standaloneNotes.Add(new CanonicalLibraryMetadataReadProjectionNote(noteItem, obj.ObjectID));
                    break;
                case CanonicalObjectKind.standaloneStudyItem:
                case CanonicalObjectKind.recordingAssociatedStudyItem:
                    if (obj.StudyItem == null)
                    {
                        failures.Add(new CanonicalLibraryMetadataReadProjectionFailure(
                            CanonicalLibraryMetadataReadProjectionFailureKind.missingMetadata,
                            obj.ObjectID.RawValue, obj.Kind, "studyItemMetadataMissing"));
                        continue;
                    }
                    studyItems.Add(new CanonicalLibraryMetadataReadProjectionItem(obj.StudyItem, obj.ObjectID));
                    break;
                case CanonicalObjectKind.recording:
                case CanonicalObjectKind.generatedArtifactEnvelope:
                case CanonicalObjectKind.unknownUnsupported:
                    failures.Add(new CanonicalLibraryMetadataReadProjectionFailure(
                        CanonicalLibraryMetadataReadProjectionFailureKind.unsupportedObject,
                        obj.ObjectID.RawValue, obj.Kind,
                        obj.UnsupportedReason ?? "unsupportedLibraryMetadataReadObject"));
                    break;
            }
        }

        return new CanonicalLibraryMetadataReadProjection(
            new CanonicalLibraryMetadataReadSnapshot(
                source,
                generatedAt,
                folders: folders.ToArray(),
                studyItems: studyItems.ToArray(),
                standaloneNotes: standaloneNotes.ToArray(),
                failures: failures.ToArray(),
                contentExcludedCount: standaloneNotes.Count
            )
        );
    }

    public virtual bool Equals(CanonicalLibraryMetadataReadProjection? other) =>
        other is not null && Snapshot.Equals(other.Snapshot);
    public override int GetHashCode() => Snapshot.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadSideDivergenceKind
{
    missingInCanonical,
    missingInLegacy,
    titleMismatch,
    parentMismatch,
    folderMembershipMismatch,
    filingMismatch,
    tagsMismatch,
    colorMismatch,
    orderingMismatch,
    trashStateMismatch,
    objectIDMismatch,
    unsupportedLegacyObject,
    unsupportedCanonicalObject,
    contentExcluded,
    pathLeakRisk
}

public sealed record CanonicalLibraryMetadataReadSideDivergence : IEquatable<CanonicalLibraryMetadataReadSideDivergence>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID, Field ?? "");

    public CanonicalLibraryMetadataReadSideDivergenceKind Kind { get; }
    public string ObjectID { get; }
    public CanonicalObjectKind? ObjectKind { get; }
    public string? Field { get; }
    public string? LegacyValue { get; }
    public string? CanonicalValue { get; }
    public bool Fatal { get; }

    public CanonicalLibraryMetadataReadSideDivergence(
        CanonicalLibraryMetadataReadSideDivergenceKind kind,
        string objectID,
        CanonicalObjectKind? objectKind = null,
        string? field = null,
        string? legacyValue = null,
        string? canonicalValue = null,
        bool fatal = false)
    {
        Kind = kind;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "library-object");
        ObjectKind = objectKind;
        Field = CanonicalProductionRedaction.SafeDiagnosticText(field);
        LegacyValue = CanonicalProductionRedaction.SafeDiagnosticText(legacyValue);
        CanonicalValue = CanonicalProductionRedaction.SafeDiagnosticText(canonicalValue);
        Fatal = fatal || kind == CanonicalLibraryMetadataReadSideDivergenceKind.pathLeakRisk;
    }

    public bool IsBlocking => Kind != CanonicalLibraryMetadataReadSideDivergenceKind.contentExcluded;

    public virtual bool Equals(CanonicalLibraryMetadataReadSideDivergence? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadSideEquivalence : IEquatable<CanonicalLibraryMetadataReadSideEquivalence>
{
    public bool Equivalent { get; }
    public int FolderCount { get; }
    public int StudyItemCount { get; }
    public int StandaloneNoteCount { get; }
    public int ContentExcludedCount { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalLibraryMetadataReadSideEquivalence(
        bool equivalent, int folderCount, int studyItemCount, int standaloneNoteCount,
        int contentExcludedCount, string diagnosticsSummary)
    {
        Equivalent = equivalent;
        FolderCount = folderCount;
        StudyItemCount = studyItemCount;
        StandaloneNoteCount = standaloneNoteCount;
        ContentExcludedCount = contentExcludedCount;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public virtual bool Equals(CanonicalLibraryMetadataReadSideEquivalence? other) =>
        other is not null && Equivalent == other.Equivalent;
    public override int GetHashCode() => Equivalent.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadSideBlocker
{
    blockingDivergence,
    unsupportedObject,
    pathLeakRisk,
    missingLegacySnapshot,
    missingCanonicalSnapshot
}

public sealed record CanonicalLibraryMetadataReadSideDiffReport : IEquatable<CanonicalLibraryMetadataReadSideDiffReport>
{
    public CanonicalLibraryMetadataReadSideEquivalence Equivalence { get; }
    public CanonicalLibraryMetadataReadSideDivergence[] Divergences { get; }
    public CanonicalLibraryMetadataReadSideBlocker[] Blockers { get; }
    public string LegacySnapshotSummary { get; }
    public string CanonicalSnapshotSummary { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalLibraryMetadataReadSideDiffReport(
        CanonicalLibraryMetadataReadSideEquivalence equivalence,
        CanonicalLibraryMetadataReadSideDivergence[] divergences,
        CanonicalLibraryMetadataReadSideBlocker[] blockers,
        string legacySnapshotSummary,
        string canonicalSnapshotSummary,
        string diagnosticsSummary)
    {
        Equivalence = equivalence;
        Divergences = divergences;
        Blockers = blockers;
        LegacySnapshotSummary = legacySnapshotSummary;
        CanonicalSnapshotSummary = canonicalSnapshotSummary;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public bool Equivalent => Blockers.Length == 0 && Divergences.All(d => !d.IsBlocking);
    public int DivergenceCount => Divergences.Count(d => d.IsBlocking);
    public int UnsupportedObjectCount => Divergences.Count(d =>
        d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.unsupportedLegacyObject ||
        d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.unsupportedCanonicalObject);
    public int PathLeakRiskCount => Divergences.Count(d =>
        d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.pathLeakRisk);

    public virtual bool Equals(CanonicalLibraryMetadataReadSideDiffReport? other) =>
        other is not null && Equivalence.Equals(other.Equivalence);
    public override int GetHashCode() => Equivalence.GetHashCode();
}

public static class CanonicalLibraryMetadataReadSideParallelDiff
{
    public static CanonicalLibraryMetadataReadSideDiffReport Compare(
        CanonicalLibraryMetadataReadSnapshot legacy,
        CanonicalLibraryMetadataReadSnapshot canonical)
    {
        var divergences = new List<CanonicalLibraryMetadataReadSideDivergence>();

        AppendFailureDivergences(legacy, divergences);
        AppendFailureDivergences(canonical, divergences);
        CompareFolders(legacy.Folders, canonical.Folders, divergences);
        CompareItems(legacy.StudyItems, canonical.StudyItems, divergences);
        CompareNotes(legacy.StandaloneNotes, canonical.StandaloneNotes, divergences);

        var blockers = new List<CanonicalLibraryMetadataReadSideBlocker>();
        if (legacy.Failures.Any(f => f.Kind == CanonicalLibraryMetadataReadProjectionFailureKind.snapshotMissing))
            blockers.Add(CanonicalLibraryMetadataReadSideBlocker.missingLegacySnapshot);
        if (canonical.Failures.Any(f => f.Kind == CanonicalLibraryMetadataReadProjectionFailureKind.snapshotMissing))
            blockers.Add(CanonicalLibraryMetadataReadSideBlocker.missingCanonicalSnapshot);
        if (divergences.Any(d => d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.pathLeakRisk))
            blockers.Add(CanonicalLibraryMetadataReadSideBlocker.pathLeakRisk);
        if (divergences.Any(d => d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.unsupportedLegacyObject ||
                                 d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.unsupportedCanonicalObject))
            blockers.Add(CanonicalLibraryMetadataReadSideBlocker.unsupportedObject);
        if (divergences.Any(d => d.IsBlocking))
            blockers.Add(CanonicalLibraryMetadataReadSideBlocker.blockingDivergence);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataReadSideBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        var equivalent = uniqueBlockers.Length == 0 && divergences.All(d => !d.IsBlocking);
        var equivalence = new CanonicalLibraryMetadataReadSideEquivalence(
            equivalent,
            canonical.Folders.Length,
            canonical.StudyItems.Length,
            canonical.StandaloneNotes.Length,
            canonical.ContentExcludedCount + legacy.ContentExcludedCount,
            $"equivalent={equivalent},folders={canonical.Folders.Length},items={canonical.StudyItems.Length},notes={canonical.StandaloneNotes.Length},contentExcluded={canonical.ContentExcludedCount + legacy.ContentExcludedCount}"
        );
        var divergenceSummary = string.Join("+",
            new HashSet<string>(divergences.Select(d => d.Kind.ToString())).OrderBy(s => s, StringComparer.Ordinal));

        return new CanonicalLibraryMetadataReadSideDiffReport(
            equivalence,
            divergences.OrderBy(d => d.Id, StringComparer.Ordinal).ToArray(),
            uniqueBlockers,
            legacy.DiagnosticsSummary,
            canonical.DiagnosticsSummary,
            $"domain=libraryMetadata,equivalent={equivalent},divergences={divergences.Count(d => d.IsBlocking)},unsupported={divergences.Count(d => d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.unsupportedLegacyObject || d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.unsupportedCanonicalObject)},pathLeakRisk={divergences.Count(d => d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.pathLeakRisk)},kinds={divergenceSummary}"
        );
    }

    private static void AppendFailureDivergences(
        CanonicalLibraryMetadataReadSnapshot snapshot,
        List<CanonicalLibraryMetadataReadSideDivergence> divergences)
    {
        foreach (var failure in snapshot.Failures)
        {
            switch (failure.Kind)
            {
                case CanonicalLibraryMetadataReadProjectionFailureKind.pathLeakRisk:
                    divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                        CanonicalLibraryMetadataReadSideDivergenceKind.pathLeakRisk,
                        failure.ObjectID ?? "run", failure.ObjectKind, "projection",
                        snapshot.Source.ToString(), failure.Reason, fatal: true));
                    break;
                case CanonicalLibraryMetadataReadProjectionFailureKind.unsupportedObject:
                case CanonicalLibraryMetadataReadProjectionFailureKind.missingMetadata:
                    divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                        snapshot.Source == CanonicalLibraryMetadataReadProjectionSource.legacy
                            ? CanonicalLibraryMetadataReadSideDivergenceKind.unsupportedLegacyObject
                            : CanonicalLibraryMetadataReadSideDivergenceKind.unsupportedCanonicalObject,
                        failure.ObjectID ?? "run", failure.ObjectKind, "object",
                        snapshot.Source.ToString(), failure.Reason));
                    break;
                case CanonicalLibraryMetadataReadProjectionFailureKind.snapshotMissing:
                    divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                        snapshot.Source == CanonicalLibraryMetadataReadProjectionSource.legacy
                            ? CanonicalLibraryMetadataReadSideDivergenceKind.missingInLegacy
                            : CanonicalLibraryMetadataReadSideDivergenceKind.missingInCanonical,
                        "snapshot", field: "snapshot",
                        legacyValue: snapshot.Source.ToString(), canonicalValue: failure.Reason));
                    break;
                case CanonicalLibraryMetadataReadProjectionFailureKind.fullContentRejected:
                    break;
            }
        }
    }

    private static void CompareFolders(
        CanonicalLibraryMetadataReadProjectionFolder[] legacy,
        CanonicalLibraryMetadataReadProjectionFolder[] canonical,
        List<CanonicalLibraryMetadataReadSideDivergence> divergences)
    {
        var legacyByID = legacy.ToDictionary(f => f.FolderID.RawValue);
        var canonicalByID = canonical.ToDictionary(f => f.FolderID.RawValue);
        foreach (var id in legacyByID.Keys.Union(canonicalByID.Keys).OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!legacyByID.TryGetValue(id, out var l))
            {
                divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                    CanonicalLibraryMetadataReadSideDivergenceKind.missingInLegacy, id,
                    CanonicalObjectKind.folder));
                continue;
            }
            if (!canonicalByID.TryGetValue(id, out var c))
            {
                divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                    CanonicalLibraryMetadataReadSideDivergenceKind.missingInCanonical, id,
                    CanonicalObjectKind.folder));
                continue;
            }
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.titleMismatch, id, CanonicalObjectKind.folder,
                "title", l.Title, c.Title, divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.parentMismatch, id, CanonicalObjectKind.folder,
                "parentID", l.ParentID?.RawValue ?? "root", c.ParentID?.RawValue ?? "root", divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.parentMismatch, id, CanonicalObjectKind.folder,
                "hierarchyPath", l.HierarchyKey, c.HierarchyKey, divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.parentMismatch, id, CanonicalObjectKind.folder,
                "hierarchyLevel", l.HierarchyLevel ?? "none", c.HierarchyLevel ?? "none", divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.colorMismatch, id, CanonicalObjectKind.folder,
                "color", l.ColorToken ?? "none", c.ColorToken ?? "none", divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.orderingMismatch, id, CanonicalObjectKind.folder,
                "ordering", l.OrderingKey ?? "none", c.OrderingKey ?? "none", divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.trashStateMismatch, id, CanonicalObjectKind.folder,
                "trash", l.IsDeleted.ToString(), c.IsDeleted.ToString(), divergences);
        }
    }

    private static void CompareItems(
        CanonicalLibraryMetadataReadProjectionItem[] legacy,
        CanonicalLibraryMetadataReadProjectionItem[] canonical,
        List<CanonicalLibraryMetadataReadSideDivergence> divergences)
    {
        var legacyByID = legacy.ToDictionary(i => i.ItemID.RawValue);
        var canonicalByID = canonical.ToDictionary(i => i.ItemID.RawValue);
        foreach (var id in legacyByID.Keys.Union(canonicalByID.Keys).OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!legacyByID.TryGetValue(id, out var l))
            {
                divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                    CanonicalLibraryMetadataReadSideDivergenceKind.missingInLegacy, id,
                    CanonicalObjectKind.standaloneStudyItem));
                continue;
            }
            if (!canonicalByID.TryGetValue(id, out var c))
            {
                divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                    CanonicalLibraryMetadataReadSideDivergenceKind.missingInCanonical, id,
                    CanonicalObjectKind.standaloneStudyItem));
                continue;
            }
            CompareItemFields(id, CanonicalObjectKind.standaloneStudyItem, l, c, divergences);
        }
    }

    private static void CompareNotes(
        CanonicalLibraryMetadataReadProjectionNote[] legacy,
        CanonicalLibraryMetadataReadProjectionNote[] canonical,
        List<CanonicalLibraryMetadataReadSideDivergence> divergences)
    {
        var legacyByID = legacy.ToDictionary(n => n.NoteItemID.RawValue);
        var canonicalByID = canonical.ToDictionary(n => n.NoteItemID.RawValue);
        foreach (var id in legacyByID.Keys.Union(canonicalByID.Keys).OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!legacyByID.TryGetValue(id, out var l))
            {
                divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                    CanonicalLibraryMetadataReadSideDivergenceKind.missingInLegacy, id,
                    CanonicalObjectKind.standaloneNote));
                continue;
            }
            if (!canonicalByID.TryGetValue(id, out var c))
            {
                divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
                    CanonicalLibraryMetadataReadSideDivergenceKind.missingInCanonical, id,
                    CanonicalObjectKind.standaloneNote));
                continue;
            }
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.titleMismatch, id, CanonicalObjectKind.standaloneNote,
                "title", l.Title, c.Title, divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.folderMembershipMismatch, id, CanonicalObjectKind.standaloneNote,
                "folders", l.FolderIDKey, c.FolderIDKey, divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.parentMismatch, id, CanonicalObjectKind.standaloneNote,
                "parents", l.ParentReferenceKey, c.ParentReferenceKey, divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.filingMismatch, id, CanonicalObjectKind.standaloneNote,
                "filing", l.FilingKey, c.FilingKey, divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.tagsMismatch, id, CanonicalObjectKind.standaloneNote,
                "tags", l.TagKey, c.TagKey, divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.trashStateMismatch, id, CanonicalObjectKind.standaloneNote,
                "trash", l.IsDeleted.ToString(), c.IsDeleted.ToString(), divergences);
            AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.folderMembershipMismatch, id, CanonicalObjectKind.standaloneNote,
                "resourceSummary", l.ResourceTokenSummary, c.ResourceTokenSummary, divergences);
        }
    }

    private static void CompareItemFields(
        string id, CanonicalObjectKind objectKind,
        CanonicalLibraryMetadataReadProjectionItem legacy,
        CanonicalLibraryMetadataReadProjectionItem canonical,
        List<CanonicalLibraryMetadataReadSideDivergence> divergences)
    {
        AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.titleMismatch, id, objectKind,
            "title", legacy.Title, canonical.Title, divergences);
        AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.folderMembershipMismatch, id, objectKind,
            "folders", legacy.FolderIDKey, canonical.FolderIDKey, divergences);
        AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.parentMismatch, id, objectKind,
            "parents", legacy.ParentReferenceKey, canonical.ParentReferenceKey, divergences);
        AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.filingMismatch, id, objectKind,
            "filing", legacy.FilingKey, canonical.FilingKey, divergences);
        AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.tagsMismatch, id, objectKind,
            "tags", legacy.TagKey, canonical.TagKey, divergences);
        AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.objectIDMismatch, id, objectKind,
            "itemKind", legacy.ItemKind.ToString(), canonical.ItemKind.ToString(), divergences);
        AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.trashStateMismatch, id, objectKind,
            "trash", legacy.IsDeleted.ToString(), canonical.IsDeleted.ToString(), divergences);
        AppendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.folderMembershipMismatch, id, objectKind,
            "resourceSummary", legacy.ResourceTokenSummary, canonical.ResourceTokenSummary, divergences);
    }

    private static void AppendMismatch(
        CanonicalLibraryMetadataReadSideDivergenceKind kind, string objectID, CanonicalObjectKind objectKind,
        string field, string legacyValue, string canonicalValue,
        List<CanonicalLibraryMetadataReadSideDivergence> divergences)
    {
        if (legacyValue == canonicalValue) return;
        divergences.Add(new CanonicalLibraryMetadataReadSideDivergence(
            kind, objectID, objectKind, field, legacyValue, canonicalValue));
    }
}

public sealed record CanonicalLibraryMetadataWriteSideEvidenceLinkage : IEquatable<CanonicalLibraryMetadataWriteSideEvidenceLinkage>
{
    public CanonicalLibraryMetadataStageEvidenceStatus CanaryStageStatus { get; }
    public CanonicalLibraryMetadataCanaryStage? LatestSuccessfulStage { get; }
    public int RollbackFailureCount { get; }
    public int DuplicateSuppressionCount { get; }
    public int UnresolvedConflictCount { get; }
    public int ResourceMoveBlockedCount { get; }
    public int ReadSideDivergenceCount { get; }
    public bool WriteSideDomainCutoverComplete { get; }

    public CanonicalLibraryMetadataWriteSideEvidenceLinkage(
        CanonicalLibraryMetadataStageEvidenceStatus canaryStageStatus = CanonicalLibraryMetadataStageEvidenceStatus.missing,
        CanonicalLibraryMetadataCanaryStage? latestSuccessfulStage = null,
        int rollbackFailureCount = 0,
        int duplicateSuppressionCount = 0,
        int unresolvedConflictCount = 0,
        int resourceMoveBlockedCount = 0,
        int readSideDivergenceCount = 0,
        bool writeSideDomainCutoverComplete = false)
    {
        CanaryStageStatus = canaryStageStatus;
        LatestSuccessfulStage = latestSuccessfulStage;
        RollbackFailureCount = Math.Max(0, rollbackFailureCount);
        DuplicateSuppressionCount = Math.Max(0, duplicateSuppressionCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        ResourceMoveBlockedCount = Math.Max(0, resourceMoveBlockedCount);
        ReadSideDivergenceCount = Math.Max(0, readSideDivergenceCount);
        WriteSideDomainCutoverComplete = writeSideDomainCutoverComplete;
    }

    public static readonly CanonicalLibraryMetadataWriteSideEvidenceLinkage Missing = new();

    public static CanonicalLibraryMetadataWriteSideEvidenceLinkage From(
        CanonicalLibraryMetadataCanaryStageEvidence? stageEvidence,
        bool writeSideDomainCutoverComplete = false)
    {
        if (stageEvidence == null) return Missing;
        return new CanonicalLibraryMetadataWriteSideEvidenceLinkage(
            stageEvidence.Status,
            stageEvidence.Status == CanonicalLibraryMetadataStageEvidenceStatus.passed ? stageEvidence.Stage : null,
            stageEvidence.RollbackFailureCount,
            stageEvidence.SuppressedLegacyDuplicateCount,
            stageEvidence.UnresolvedConflictCount,
            stageEvidence.ResourceMoveAttemptCount,
            stageEvidence.ReadSideParallelDivergenceCount,
            writeSideDomainCutoverComplete
        );
    }

    public bool HasCleanStagedCanaryEvidence =>
        CanaryStageStatus == CanonicalLibraryMetadataStageEvidenceStatus.passed
        && LatestSuccessfulStage != null
        && RollbackFailureCount == 0
        && UnresolvedConflictCount == 0
        && ResourceMoveBlockedCount == 0
        && ReadSideDivergenceCount == 0;

    public string DiagnosticsSummary => string.Join(",",
        $"stageStatus={CanaryStageStatus}",
        $"latestStage={LatestSuccessfulStage?.ToString() ?? "none"}",
        $"rollbackFailures={RollbackFailureCount}",
        $"duplicateSuppression={DuplicateSuppressionCount}",
        $"unresolvedConflicts={UnresolvedConflictCount}",
        $"resourceMoveBlocked={ResourceMoveBlockedCount}",
        $"readSideDivergence={ReadSideDivergenceCount}",
        $"domainCutover={WriteSideDomainCutoverComplete}"
    );

    public virtual bool Equals(CanonicalLibraryMetadataWriteSideEvidenceLinkage? other) =>
        other is not null && CanaryStageStatus == other.CanaryStageStatus;
    public override int GetHashCode() => CanaryStageStatus.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadSideCutoverMode
{
    disabled,
    parallelOnly,
    canonicalReadCandidate,
    guardedCanonicalRead,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadSideCutoverFailure
{
    disabled,
    blockingDivergence,
    unsupportedObject,
    missingWriteSideEvidence,
    fallbackMissing,
    pathLeakRisk,
    otherActiveDomain,
    rollbackFatal,
    legacyReadSuppressed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadSideCutoverCandidateState
{
    readyForGuardedCanonicalRead,
    blockedByDivergence,
    blockedByUnsupportedObject,
    blockedByMissingWriteSideEvidence,
    blockedByFallbackMissing,
    blockedByPathLeakRisk,
    blockedByOtherActiveDomain,
    disabled
}

public sealed record CanonicalLibraryMetadataReadSideCutoverPolicy : IEquatable<CanonicalLibraryMetadataReadSideCutoverPolicy>
{
    public bool RecordDiagnostics { get; }
    public int MaxDiagnosticsEvents { get; }
    public bool RequireWriteSideEvidence { get; }
    public bool RequireZeroDivergence { get; }
    public bool RequireLegacyFallback { get; }
    public bool RequireOnlyLibraryMetadataActivePilot { get; }

    public CanonicalLibraryMetadataReadSideCutoverPolicy(
        bool recordDiagnostics = true,
        int maxDiagnosticsEvents = 24,
        bool requireWriteSideEvidence = true,
        bool requireZeroDivergence = true,
        bool requireLegacyFallback = true,
        bool requireOnlyLibraryMetadataActivePilot = true)
    {
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(0, maxDiagnosticsEvents);
        RequireWriteSideEvidence = requireWriteSideEvidence;
        RequireZeroDivergence = requireZeroDivergence;
        RequireLegacyFallback = requireLegacyFallback;
        RequireOnlyLibraryMetadataActivePilot = requireOnlyLibraryMetadataActivePilot;
    }

    public virtual bool Equals(CanonicalLibraryMetadataReadSideCutoverPolicy? other) =>
        other is not null && RecordDiagnostics == other.RecordDiagnostics;
    public override int GetHashCode() => RecordDiagnostics.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadSideCutoverConfiguration : IEquatable<CanonicalLibraryMetadataReadSideCutoverConfiguration>
{
    public CanonicalLibraryMetadataReadSideCutoverMode Mode { get; }
    public CanonicalLibraryMetadataReadSideCutoverPolicy Policy { get; }
    public CanonicalLibraryMetadataWriteSideEvidenceLinkage WriteSideEvidence { get; }
    public bool LegacyFallbackAvailable { get; }

    public CanonicalLibraryMetadataReadSideCutoverConfiguration(
        CanonicalLibraryMetadataReadSideCutoverMode mode = CanonicalLibraryMetadataReadSideCutoverMode.disabled,
        CanonicalLibraryMetadataReadSideCutoverPolicy? policy = null,
        CanonicalLibraryMetadataWriteSideEvidenceLinkage? writeSideEvidence = null,
        bool legacyFallbackAvailable = true)
    {
        Mode = mode;
        Policy = policy ?? new CanonicalLibraryMetadataReadSideCutoverPolicy();
        WriteSideEvidence = writeSideEvidence ?? CanonicalLibraryMetadataWriteSideEvidenceLinkage.Missing;
        LegacyFallbackAvailable = legacyFallbackAvailable;
    }

    public static readonly CanonicalLibraryMetadataReadSideCutoverConfiguration Disabled = new();

    public bool IsEnabled => Mode != CanonicalLibraryMetadataReadSideCutoverMode.disabled
                             && Mode != CanonicalLibraryMetadataReadSideCutoverMode.blocked;

    public virtual bool Equals(CanonicalLibraryMetadataReadSideCutoverConfiguration? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadSideCutoverCandidateResult : IEquatable<CanonicalLibraryMetadataReadSideCutoverCandidateResult>
{
    public CanonicalLibraryMetadataReadSideCutoverCandidateState State { get; }
    public CanonicalLibraryMetadataReadSideCutoverFailure[] Failures { get; }
    public bool LegacyFallbackAvailable { get; }
    public bool DivergenceZero { get; }
    public int UnsupportedCount { get; }
    public int PathLeakRiskCount { get; }
    public bool ObjectIDStable { get; }
    public bool NoResourceMove { get; }
    public bool NoTombstoneDeleteCutover { get; }
    public CanonicalLibraryMetadataWriteSideEvidenceLinkage WriteSideEvidence { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalLibraryMetadataReadSideCutoverCandidateResult(
        CanonicalLibraryMetadataReadSideCutoverCandidateState state,
        CanonicalLibraryMetadataReadSideCutoverFailure[] failures,
        bool legacyFallbackAvailable,
        bool divergenceZero,
        int unsupportedCount,
        int pathLeakRiskCount,
        bool objectIDStable,
        bool noResourceMove,
        bool noTombstoneDeleteCutover,
        CanonicalLibraryMetadataWriteSideEvidenceLinkage writeSideEvidence,
        string diagnosticsSummary)
    {
        State = state;
        Failures = failures;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        DivergenceZero = divergenceZero;
        UnsupportedCount = unsupportedCount;
        PathLeakRiskCount = pathLeakRiskCount;
        ObjectIDStable = objectIDStable;
        NoResourceMove = noResourceMove;
        NoTombstoneDeleteCutover = noTombstoneDeleteCutover;
        WriteSideEvidence = writeSideEvidence;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public bool Ready => State == CanonicalLibraryMetadataReadSideCutoverCandidateState.readyForGuardedCanonicalRead;

    public virtual bool Equals(CanonicalLibraryMetadataReadSideCutoverCandidateResult? other) =>
        other is not null && State == other.State;
    public override int GetHashCode() => State.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadSideCutoverResult : IEquatable<CanonicalLibraryMetadataReadSideCutoverResult>
{
    public CanonicalLibraryMetadataReadSideCutoverMode Mode { get; }
    public CanonicalLibraryMetadataReadSideDiffReport? DiffReport { get; }
    public CanonicalLibraryMetadataReadSideCutoverCandidateResult Candidate { get; }
    public CanonicalLibraryMetadataReadSideCutoverFailure[] Failures { get; }
    public bool LegacyReadFallbackAvailable { get; }
    public bool ReadPathSwitched { get; }
    public bool UiMutated { get; }
    public bool SyncOrUploadTriggered { get; }
    public CanonicalLibraryMetadataCutoverDiagnostic[] Diagnostics { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalLibraryMetadataReadSideCutoverResult(
        CanonicalLibraryMetadataReadSideCutoverMode mode,
        CanonicalLibraryMetadataReadSideDiffReport? diffReport,
        CanonicalLibraryMetadataReadSideCutoverCandidateResult candidate,
        CanonicalLibraryMetadataReadSideCutoverFailure[] failures,
        bool legacyReadFallbackAvailable,
        bool readPathSwitched,
        bool uiMutated,
        bool syncOrUploadTriggered,
        CanonicalLibraryMetadataCutoverDiagnostic[] diagnostics,
        string diagnosticsSummary)
    {
        Mode = mode;
        DiffReport = diffReport;
        Candidate = candidate;
        Failures = failures;
        LegacyReadFallbackAvailable = legacyReadFallbackAvailable;
        ReadPathSwitched = readPathSwitched;
        UiMutated = uiMutated;
        SyncOrUploadTriggered = syncOrUploadTriggered;
        Diagnostics = diagnostics;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public virtual bool Equals(CanonicalLibraryMetadataReadSideCutoverResult? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

public static class CanonicalLibraryMetadataReadSideCutoverEvaluator
{
    public static CanonicalLibraryMetadataReadSideCutoverResult Evaluate(
        CanonicalLibraryMetadataReadSideCutoverConfiguration configuration,
        CanonicalLibraryMetadataReadSnapshot legacySnapshot,
        CanonicalLibraryMetadataReadSnapshot canonicalSnapshot,
        CanonicalMigrationDomainMatrix? matrix = null,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        matrix ??= CanonicalMigrationDomainMatrix.DefaultV813();

        if (configuration.Mode == CanonicalLibraryMetadataReadSideCutoverMode.disabled
            || configuration.Mode == CanonicalLibraryMetadataReadSideCutoverMode.blocked)
        {
            var candidate = new CanonicalLibraryMetadataReadSideCutoverCandidateResult(
                CanonicalLibraryMetadataReadSideCutoverCandidateState.disabled,
                new[] { CanonicalLibraryMetadataReadSideCutoverFailure.disabled },
                configuration.LegacyFallbackAvailable,
                false, 0, 0, true, true, true,
                configuration.WriteSideEvidence,
                "state=disabled"
            );
            return new CanonicalLibraryMetadataReadSideCutoverResult(
                configuration.Mode, null, candidate,
                new[] { CanonicalLibraryMetadataReadSideCutoverFailure.disabled },
                configuration.LegacyFallbackAvailable, false, false, false,
                Array.Empty<CanonicalLibraryMetadataCutoverDiagnostic>(),
                $"mode={configuration.Mode},state=disabled"
            );
        }

        var report = CanonicalLibraryMetadataReadSideParallelDiff.Compare(legacySnapshot, canonicalSnapshot);
        var cand = EvaluateCandidate(configuration, report, matrix);
        var failures = new List<CanonicalLibraryMetadataReadSideCutoverFailure>(cand.Failures);
        if (configuration.WriteSideEvidence.RollbackFailureCount > 0)
            failures.Add(CanonicalLibraryMetadataReadSideCutoverFailure.rollbackFatal);

        var diagnostics = configuration.Policy.RecordDiagnostics
            ? MakeDiagnostics(configuration, report, cand, trigger, nodeRole, syncRunID)
            : Array.Empty<CanonicalLibraryMetadataCutoverDiagnostic>();
        var limitedDiagnostics = diagnostics.Take(configuration.Policy.MaxDiagnosticsEvents).ToArray();

        return new CanonicalLibraryMetadataReadSideCutoverResult(
            configuration.Mode, report, cand,
            new HashSet<CanonicalLibraryMetadataReadSideCutoverFailure>(failures)
                .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray(),
            configuration.LegacyFallbackAvailable, false, false, false,
            limitedDiagnostics,
            $"mode={configuration.Mode},candidate={cand.State},divergences={report.DivergenceCount},fallback={configuration.LegacyFallbackAvailable},readPathSwitched=false,uiMutated=false,syncOrUploadTriggered=false"
        );
    }

    private static CanonicalLibraryMetadataReadSideCutoverCandidateResult EvaluateCandidate(
        CanonicalLibraryMetadataReadSideCutoverConfiguration configuration,
        CanonicalLibraryMetadataReadSideDiffReport report,
        CanonicalMigrationDomainMatrix matrix)
    {
        var failures = new List<CanonicalLibraryMetadataReadSideCutoverFailure>();
        var matrixReport = matrix.Validate();

        if (configuration.Policy.RequireOnlyLibraryMetadataActivePilot
            && (matrixReport.ActivePilotDomain != CanonicalMigrationDomain.libraryMetadata
                || matrixReport.Blockers.Contains(CanonicalMigrationMatrixBlocker.multipleActivePilots)))
        {
            failures.Add(CanonicalLibraryMetadataReadSideCutoverFailure.otherActiveDomain);
        }
        if (configuration.Policy.RequireLegacyFallback && !configuration.LegacyFallbackAvailable)
            failures.Add(CanonicalLibraryMetadataReadSideCutoverFailure.fallbackMissing);
        if (configuration.Policy.RequireWriteSideEvidence)
        {
            var evidence = configuration.WriteSideEvidence;
            if (!evidence.HasCleanStagedCanaryEvidence || evidence.RollbackFailureCount > 0)
                failures.Add(evidence.RollbackFailureCount > 0
                    ? CanonicalLibraryMetadataReadSideCutoverFailure.rollbackFatal
                    : CanonicalLibraryMetadataReadSideCutoverFailure.missingWriteSideEvidence);
        }
        if (configuration.Policy.RequireZeroDivergence && report.DivergenceCount > 0)
            failures.Add(CanonicalLibraryMetadataReadSideCutoverFailure.blockingDivergence);
        if (report.UnsupportedObjectCount > 0)
            failures.Add(CanonicalLibraryMetadataReadSideCutoverFailure.unsupportedObject);
        if (report.PathLeakRiskCount > 0)
            failures.Add(CanonicalLibraryMetadataReadSideCutoverFailure.pathLeakRisk);

        var uniqueFailures = new HashSet<CanonicalLibraryMetadataReadSideCutoverFailure>(failures)
            .OrderBy(f => f.ToString(), StringComparer.Ordinal).ToArray();

        CanonicalLibraryMetadataReadSideCutoverCandidateState state;
        if (uniqueFailures.Contains(CanonicalLibraryMetadataReadSideCutoverFailure.pathLeakRisk))
            state = CanonicalLibraryMetadataReadSideCutoverCandidateState.blockedByPathLeakRisk;
        else if (uniqueFailures.Contains(CanonicalLibraryMetadataReadSideCutoverFailure.unsupportedObject))
            state = CanonicalLibraryMetadataReadSideCutoverCandidateState.blockedByUnsupportedObject;
        else if (uniqueFailures.Contains(CanonicalLibraryMetadataReadSideCutoverFailure.missingWriteSideEvidence)
                 || uniqueFailures.Contains(CanonicalLibraryMetadataReadSideCutoverFailure.rollbackFatal))
            state = CanonicalLibraryMetadataReadSideCutoverCandidateState.blockedByMissingWriteSideEvidence;
        else if (uniqueFailures.Contains(CanonicalLibraryMetadataReadSideCutoverFailure.otherActiveDomain))
            state = CanonicalLibraryMetadataReadSideCutoverCandidateState.blockedByOtherActiveDomain;
        else if (uniqueFailures.Contains(CanonicalLibraryMetadataReadSideCutoverFailure.fallbackMissing))
            state = CanonicalLibraryMetadataReadSideCutoverCandidateState.blockedByFallbackMissing;
        else if (uniqueFailures.Contains(CanonicalLibraryMetadataReadSideCutoverFailure.blockingDivergence))
            state = CanonicalLibraryMetadataReadSideCutoverCandidateState.blockedByDivergence;
        else
            state = CanonicalLibraryMetadataReadSideCutoverCandidateState.readyForGuardedCanonicalRead;

        return new CanonicalLibraryMetadataReadSideCutoverCandidateResult(
            state, uniqueFailures,
            configuration.LegacyFallbackAvailable,
            report.DivergenceCount == 0,
            report.UnsupportedObjectCount,
            report.PathLeakRiskCount,
            !report.Divergences.Any(d => d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.objectIDMismatch),
            true, true,
            configuration.WriteSideEvidence,
            $"state={state},divergenceZero={report.DivergenceCount == 0},unsupported={report.UnsupportedObjectCount},pathLeakRisk={report.PathLeakRiskCount},fallback={configuration.LegacyFallbackAvailable},writeSide={configuration.WriteSideEvidence.DiagnosticsSummary}"
        );
    }

    private static CanonicalLibraryMetadataCutoverDiagnostic[] MakeDiagnostics(
        CanonicalLibraryMetadataReadSideCutoverConfiguration configuration,
        CanonicalLibraryMetadataReadSideDiffReport report,
        CanonicalLibraryMetadataReadSideCutoverCandidateResult candidate,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        string? syncRunID)
    {
        var diagnostics = new List<CanonicalLibraryMetadataCutoverDiagnostic>
        {
            new(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSideParallelStarted,
                syncRunID, trigger, nodeRole,
                configuration.Mode.ToString(), "domain=libraryMetadata"
            ),
            new(
                report.Equivalent
                    ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSideEquivalent
                    : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSideDivergent,
                syncRunID, trigger, nodeRole,
                report.Equivalent ? "equivalent" : "divergent",
                report.DiagnosticsSummary
            )
        };

        if (report.UnsupportedObjectCount > 0)
            diagnostics.Add(new CanonicalLibraryMetadataCutoverDiagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSideUnsupportedObject,
                syncRunID, trigger, nodeRole,
                "blocked", $"unsupported={report.UnsupportedObjectCount}"
            ));
        if (report.PathLeakRiskCount > 0)
            diagnostics.Add(new CanonicalLibraryMetadataCutoverDiagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSidePathLeakBlocked,
                syncRunID, trigger, nodeRole,
                "fatal", $"pathLeakRisk={report.PathLeakRiskCount}"
            ));
        diagnostics.Add(new CanonicalLibraryMetadataCutoverDiagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSideParallelCompleted,
            syncRunID, trigger, nodeRole,
            report.Equivalent ? "equivalent" : "divergent",
            $"divergenceCount={report.DivergenceCount}"
        ));
        diagnostics.Add(new CanonicalLibraryMetadataCutoverDiagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSideCutoverCandidateEvaluated,
            syncRunID, trigger, nodeRole,
            candidate.State.ToString(), candidate.DiagnosticsSummary
        ));
        diagnostics.Add(new CanonicalLibraryMetadataCutoverDiagnostic(
            candidate.Ready
                ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSideCutoverCandidateReady
                : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSideCutoverCandidateBlocked,
            syncRunID, trigger, nodeRole,
            candidate.Ready ? "ready" : "blocked",
            string.Join("+", candidate.Failures.Select(f => f.ToString()))
        ));
        diagnostics.Add(new CanonicalLibraryMetadataCutoverDiagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataLegacyReadFallbackAvailable,
            syncRunID, trigger, nodeRole,
            configuration.LegacyFallbackAvailable ? "available" : "missing",
            "fallbackPreserved"
        ));
        if (configuration.Mode == CanonicalLibraryMetadataReadSideCutoverMode.guardedCanonicalRead)
            diagnostics.Add(new CanonicalLibraryMetadataCutoverDiagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataGuardedCanonicalReadSuppressed,
                syncRunID, trigger, nodeRole,
                "suppressed", "defaultOffSeamNoUISwitch"
            ));

        return diagnostics.ToArray();
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadSourceMode
{
    legacy,
    parallelCompare,
    canonicalCandidate,
    guardedCanonicalRead,
    blocked
}

public sealed record CanonicalLibraryMetadataReadSourceConfiguration : IEquatable<CanonicalLibraryMetadataReadSourceConfiguration>
{
    public CanonicalLibraryMetadataReadSourceMode Mode { get; }
    public bool ExplicitInternalTestConfiguration { get; }
    public bool UiCutoverGlobal { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool StrictFallbackOnDivergence { get; }
    public bool RecordDiagnostics { get; }
    public int MaxDiagnosticsEvents { get; }

    public CanonicalLibraryMetadataReadSourceConfiguration(
        CanonicalLibraryMetadataReadSourceMode mode = CanonicalLibraryMetadataReadSourceMode.legacy,
        bool explicitInternalTestConfiguration = false,
        bool uiCutoverGlobal = false,
        bool runtimeSwitchEnabled = false,
        bool strictFallbackOnDivergence = true,
        bool recordDiagnostics = true,
        int maxDiagnosticsEvents = 32)
    {
        Mode = mode;
        ExplicitInternalTestConfiguration = explicitInternalTestConfiguration;
        UiCutoverGlobal = uiCutoverGlobal;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        StrictFallbackOnDivergence = strictFallbackOnDivergence;
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(0, maxDiagnosticsEvents);
    }

    public static readonly CanonicalLibraryMetadataReadSourceConfiguration Legacy = new();

    public static CanonicalLibraryMetadataReadSourceConfiguration ExplicitGuardedCanonicalRead() =>
        new(CanonicalLibraryMetadataReadSourceMode.guardedCanonicalRead, true);

    public virtual bool Equals(CanonicalLibraryMetadataReadSourceConfiguration? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadFallback
{
    none,
    legacyDefault,
    gateBlocked,
    canonicalProjectionMissing,
    unsupportedObject,
    divergenceDetected,
    pathLeakRisk,
    canonicalReadException,
    blockedMode
}

public sealed record CanonicalLibraryMetadataReadSource : IEquatable<CanonicalLibraryMetadataReadSource>
{
    public CanonicalLibraryMetadataReadProjectionSource Source { get; }
    public CanonicalLibraryMetadataReadSnapshot Snapshot { get; }
    public bool MetadataOnly { get; }
    public bool CoversFolderMetadata { get; }
    public bool CoversStudyItemMetadata { get; }
    public bool CoversStandaloneNoteMetadata { get; }
    public bool ExcludesAudioState { get; }
    public bool ExcludesGeneratedArtifactContent { get; }
    public bool ExcludesStandaloneNoteContent { get; }

    public CanonicalLibraryMetadataReadSource(
        CanonicalLibraryMetadataReadProjectionSource source,
        CanonicalLibraryMetadataReadSnapshot snapshot)
    {
        Source = source;
        Snapshot = snapshot;
        MetadataOnly = true;
        CoversFolderMetadata = true;
        CoversStudyItemMetadata = true;
        CoversStandaloneNoteMetadata = true;
        ExcludesAudioState = true;
        ExcludesGeneratedArtifactContent = true;
        ExcludesStandaloneNoteContent = true;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"source={Source}",
        $"metadataOnly={MetadataOnly}",
        $"folders={Snapshot.Folders.Length}",
        $"items={Snapshot.StudyItems.Length}",
        $"notes={Snapshot.StandaloneNotes.Length}",
        $"excludeAudio={ExcludesAudioState}",
        $"excludeGeneratedContent={ExcludesGeneratedArtifactContent}",
        $"excludeNoteContent={ExcludesStandaloneNoteContent}"
    );

    public virtual bool Equals(CanonicalLibraryMetadataReadSource? other) =>
        other is not null && Source == other.Source;
    public override int GetHashCode() => Source.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadCutoverGateState
{
    allowed,
    blockedByWriteSideEvidence,
    blockedByDivergence,
    blockedByUnsupportedObject,
    blockedByPathLeakRisk,
    blockedByFallbackMissing,
    blockedByOtherActiveDomain,
    blockedByDefaultConfig,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadCutoverBlocker
{
    activePilotNotLibraryMetadata,
    multipleActivePilots,
    writeSideCanaryEvidenceMissing,
    writeSideRollbackFatal,
    readSideDivergence,
    unsupportedObject,
    pathLeakRisk,
    legacyFallbackMissing,
    canonicalProjectionIncomplete,
    objectIDUnstable,
    resourceMoveRisk,
    contentWriteRisk,
    tombstoneDeleteCandidate,
    unresolvedConflict,
    explicitInternalTestConfigMissing,
    globalUICutoverRequested,
    runtimeSwitchEnabled,
    otherDomainNotStaticOnly
}

public sealed record CanonicalLibraryMetadataReadCutoverGateContext : IEquatable<CanonicalLibraryMetadataReadCutoverGateContext>
{
    public CanonicalLibraryMetadataReadSourceConfiguration Configuration { get; }
    public CanonicalLibraryMetadataWriteSideEvidenceLinkage WriteSideEvidence { get; }
    public bool LegacyFallbackAvailable { get; }
    public bool CanonicalProjectionComplete { get; }
    public bool ObjectIDStable { get; }
    public bool NoResourceMove { get; }
    public bool NoContentWrite { get; }
    public bool NoTombstoneDeleteCandidate { get; }
    public int UnresolvedConflictCount { get; }

    public CanonicalLibraryMetadataReadCutoverGateContext(
        CanonicalLibraryMetadataReadSourceConfiguration configuration,
        CanonicalLibraryMetadataWriteSideEvidenceLinkage writeSideEvidence,
        bool legacyFallbackAvailable = true,
        bool canonicalProjectionComplete = true,
        bool objectIDStable = true,
        bool noResourceMove = true,
        bool noContentWrite = true,
        bool noTombstoneDeleteCandidate = true,
        int unresolvedConflictCount = 0)
    {
        Configuration = configuration;
        WriteSideEvidence = writeSideEvidence;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        CanonicalProjectionComplete = canonicalProjectionComplete;
        ObjectIDStable = objectIDStable;
        NoResourceMove = noResourceMove;
        NoContentWrite = noContentWrite;
        NoTombstoneDeleteCandidate = noTombstoneDeleteCandidate;
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
    }

    public virtual bool Equals(CanonicalLibraryMetadataReadCutoverGateContext? other) =>
        other is not null && Configuration.Equals(other.Configuration);
    public override int GetHashCode() => Configuration.GetHashCode();
}

public sealed record CanonicalLibraryMetadataReadCutoverGateResult : IEquatable<CanonicalLibraryMetadataReadCutoverGateResult>
{
    public CanonicalLibraryMetadataReadCutoverGateState State { get; }
    public CanonicalLibraryMetadataReadCutoverBlocker[] Blockers { get; }
    public CanonicalLibraryMetadataCutoverDiagnostic[] Diagnostics { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalLibraryMetadataReadCutoverGateResult(
        CanonicalLibraryMetadataReadCutoverGateState state,
        CanonicalLibraryMetadataReadCutoverBlocker[] blockers,
        CanonicalLibraryMetadataCutoverDiagnostic[] diagnostics,
        string diagnosticsSummary)
    {
        State = state;
        Blockers = blockers;
        Diagnostics = diagnostics;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public bool Allowed => State == CanonicalLibraryMetadataReadCutoverGateState.allowed && Blockers.Length == 0;

    public virtual bool Equals(CanonicalLibraryMetadataReadCutoverGateResult? other) =>
        other is not null && State == other.State;
    public override int GetHashCode() => State.GetHashCode();
}

public static class CanonicalLibraryMetadataReadCutoverGate
{
    public static CanonicalLibraryMetadataReadCutoverGateResult Evaluate(
        CanonicalLibraryMetadataReadCutoverGateContext context,
        CanonicalLibraryMetadataReadSideDiffReport diffReport,
        CanonicalMigrationDomainMatrix? matrix = null,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        matrix ??= CanonicalMigrationDomainMatrix.DefaultV813();
        var blockers = new List<CanonicalLibraryMetadataReadCutoverBlocker>();
        var matrixReport = matrix.Validate();

        if (matrixReport.ActivePilotDomain != CanonicalMigrationDomain.libraryMetadata)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.activePilotNotLibraryMetadata);
        if (matrixReport.Blockers.Contains(CanonicalMigrationMatrixBlocker.multipleActivePilots))
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.multipleActivePilots);
        if (matrixReport.Blockers.Contains(CanonicalMigrationMatrixBlocker.nonPilotDomainNotStaticOnly))
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.otherDomainNotStaticOnly);
        if (!context.WriteSideEvidence.HasCleanStagedCanaryEvidence)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.writeSideCanaryEvidenceMissing);
        if (context.WriteSideEvidence.RollbackFailureCount > 0)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.writeSideRollbackFatal);
        if (diffReport.DivergenceCount > 0 || context.WriteSideEvidence.ReadSideDivergenceCount > 0)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.readSideDivergence);
        if (diffReport.UnsupportedObjectCount > 0)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.unsupportedObject);
        if (diffReport.PathLeakRiskCount > 0)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.pathLeakRisk);
        if (!context.LegacyFallbackAvailable)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.legacyFallbackMissing);
        if (!context.CanonicalProjectionComplete)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.canonicalProjectionIncomplete);
        if (!context.ObjectIDStable || diffReport.Divergences.Any(d =>
                d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.objectIDMismatch))
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.objectIDUnstable);
        if (!context.NoResourceMove)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.resourceMoveRisk);
        if (!context.NoContentWrite)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.contentWriteRisk);
        if (!context.NoTombstoneDeleteCandidate)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.tombstoneDeleteCandidate);
        if (context.UnresolvedConflictCount > 0 || context.WriteSideEvidence.UnresolvedConflictCount > 0)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.unresolvedConflict);
        if (!context.Configuration.ExplicitInternalTestConfiguration)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.explicitInternalTestConfigMissing);
        if (context.Configuration.UiCutoverGlobal)
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.globalUICutoverRequested);
        if (context.Configuration.RuntimeSwitchEnabled
            || matrixReport.Blockers.Contains(CanonicalMigrationMatrixBlocker.runtimeSwitchEnabled))
            blockers.Add(CanonicalLibraryMetadataReadCutoverBlocker.runtimeSwitchEnabled);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataReadCutoverBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        var state = StateFor(uniqueBlockers);
        var diagnosticsSummary = string.Join(",",
            $"state={state}",
            "domain=libraryMetadata",
            $"divergences={diffReport.DivergenceCount}",
            $"unsupported={diffReport.UnsupportedObjectCount}",
            $"pathLeakRisk={diffReport.PathLeakRiskCount}",
            $"fallback={context.LegacyFallbackAvailable}",
            $"explicitInternal={context.Configuration.ExplicitInternalTestConfiguration}",
            $"runtimeSwitch={context.Configuration.RuntimeSwitchEnabled}",
            $"uiGlobal={context.Configuration.UiCutoverGlobal}",
            $"blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))}"
        );
        var diagnostics = new[]
        {
            new CanonicalLibraryMetadataCutoverDiagnostic(
                CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadCutoverGateEvaluated,
                syncRunID, trigger, nodeRole,
                state.ToString(), diagnosticsSummary
            ),
            new CanonicalLibraryMetadataCutoverDiagnostic(
                uniqueBlockers.Length == 0
                    ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadCutoverGateAllowed
                    : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadCutoverGateBlocked,
                syncRunID, trigger, nodeRole,
                uniqueBlockers.Length == 0 ? "allowed" : "blocked",
                string.Join("+", uniqueBlockers.Select(b => b.ToString()))
            )
        };

        return new CanonicalLibraryMetadataReadCutoverGateResult(state, uniqueBlockers, diagnostics, diagnosticsSummary);
    }

    private static CanonicalLibraryMetadataReadCutoverGateState StateFor(
        CanonicalLibraryMetadataReadCutoverBlocker[] blockers)
    {
        if (blockers.Length == 0) return CanonicalLibraryMetadataReadCutoverGateState.allowed;
        if (blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.explicitInternalTestConfigMissing)
            || blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.globalUICutoverRequested)
            || blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.runtimeSwitchEnabled))
            return CanonicalLibraryMetadataReadCutoverGateState.blockedByDefaultConfig;
        if (blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.activePilotNotLibraryMetadata)
            || blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.multipleActivePilots)
            || blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.otherDomainNotStaticOnly))
            return CanonicalLibraryMetadataReadCutoverGateState.blockedByOtherActiveDomain;
        if (blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.writeSideCanaryEvidenceMissing)
            || blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.writeSideRollbackFatal))
            return CanonicalLibraryMetadataReadCutoverGateState.blockedByWriteSideEvidence;
        if (blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.pathLeakRisk))
            return CanonicalLibraryMetadataReadCutoverGateState.blockedByPathLeakRisk;
        if (blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.unsupportedObject))
            return CanonicalLibraryMetadataReadCutoverGateState.blockedByUnsupportedObject;
        if (blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.legacyFallbackMissing))
            return CanonicalLibraryMetadataReadCutoverGateState.blockedByFallbackMissing;
        if (blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.readSideDivergence))
            return CanonicalLibraryMetadataReadCutoverGateState.blockedByDivergence;
        return CanonicalLibraryMetadataReadCutoverGateState.blocked;
    }
}

public sealed record CanonicalLibraryMetadataReadSourceResult : IEquatable<CanonicalLibraryMetadataReadSourceResult>
{
    public CanonicalLibraryMetadataReadSourceMode Mode { get; }
    public CanonicalLibraryMetadataReadProjectionSource ReturnedSource { get; }
    public CanonicalLibraryMetadataReadSource ReadSource { get; }
    public CanonicalLibraryMetadataReadSnapshot LegacySnapshot { get; }
    public CanonicalLibraryMetadataReadSnapshot? CanonicalCandidate { get; }
    public CanonicalLibraryMetadataReadSideDiffReport? DiffReport { get; }
    public CanonicalLibraryMetadataReadCutoverGateResult? GateResult { get; }
    public CanonicalLibraryMetadataReadFallback Fallback { get; }
    public int FallbackCount { get; }
    public bool CanonicalReadServed { get; }
    public bool LegacyReadReturned { get; }
    public bool CanonicalCandidateBuilt { get; }
    public bool FatalForFutureStage { get; }
    public bool StoreMutated { get; }
    public bool SyncOrUploadTriggered { get; }
    public bool ResourceMoved { get; }
    public bool ContentWritten { get; }
    public bool UiMutated { get; }
    public CanonicalLibraryMetadataCutoverDiagnostic[] Diagnostics { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalLibraryMetadataReadSourceResult(
        CanonicalLibraryMetadataReadSourceMode mode,
        CanonicalLibraryMetadataReadProjectionSource returnedSource,
        CanonicalLibraryMetadataReadSource readSource,
        CanonicalLibraryMetadataReadSnapshot legacySnapshot,
        CanonicalLibraryMetadataReadSnapshot? canonicalCandidate,
        CanonicalLibraryMetadataReadSideDiffReport? diffReport,
        CanonicalLibraryMetadataReadCutoverGateResult? gateResult,
        CanonicalLibraryMetadataReadFallback fallback,
        int fallbackCount,
        bool canonicalReadServed,
        bool legacyReadReturned,
        bool canonicalCandidateBuilt,
        bool fatalForFutureStage,
        bool storeMutated,
        bool syncOrUploadTriggered,
        bool resourceMoved,
        bool contentWritten,
        bool uiMutated,
        CanonicalLibraryMetadataCutoverDiagnostic[] diagnostics,
        string diagnosticsSummary)
    {
        Mode = mode;
        ReturnedSource = returnedSource;
        ReadSource = readSource;
        LegacySnapshot = legacySnapshot;
        CanonicalCandidate = canonicalCandidate;
        DiffReport = diffReport;
        GateResult = gateResult;
        Fallback = fallback;
        FallbackCount = fallbackCount;
        CanonicalReadServed = canonicalReadServed;
        LegacyReadReturned = legacyReadReturned;
        CanonicalCandidateBuilt = canonicalCandidateBuilt;
        FatalForFutureStage = fatalForFutureStage;
        StoreMutated = storeMutated;
        SyncOrUploadTriggered = syncOrUploadTriggered;
        ResourceMoved = resourceMoved;
        ContentWritten = contentWritten;
        UiMutated = uiMutated;
        Diagnostics = diagnostics;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public virtual bool Equals(CanonicalLibraryMetadataReadSourceResult? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

public class CanonicalLibraryMetadataReadSourceProvider
{
    public CanonicalLibraryMetadataReadSourceConfiguration Configuration { get; }
    public CanonicalMigrationDomainMatrix Matrix { get; }

    public CanonicalLibraryMetadataReadSourceProvider(
        CanonicalLibraryMetadataReadSourceConfiguration? configuration = null,
        CanonicalMigrationDomainMatrix? matrix = null)
    {
        Configuration = configuration ?? CanonicalLibraryMetadataReadSourceConfiguration.Legacy;
        Matrix = matrix ?? CanonicalMigrationDomainMatrix.DefaultV813();
    }

    public CanonicalLibraryMetadataReadSourceResult Read(
        CanonicalLibraryMetadataReadSnapshot legacySnapshot,
        CanonicalLibraryMetadataReadSnapshot? canonicalSnapshot,
        CanonicalLibraryMetadataWriteSideEvidenceLinkage writeSideEvidence,
        bool legacyFallbackAvailable = true,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null,
        string? canonicalReadFailureReason = null,
        int unresolvedConflictCount = 0)
    {
        var projectionComplete = canonicalSnapshot != null && !canonicalSnapshot.Failures.Any(f =>
            f.Kind == CanonicalLibraryMetadataReadProjectionFailureKind.snapshotMissing
            || f.Kind == CanonicalLibraryMetadataReadProjectionFailureKind.missingMetadata);
        var diffReport = canonicalSnapshot != null
            ? CanonicalLibraryMetadataReadSideParallelDiff.Compare(legacySnapshot, canonicalSnapshot)
            : null;
        var evaluatedDiag = Diagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSourceEvaluated,
            syncRunID, trigger, nodeRole,
            Configuration.Mode.ToString(), "domain=libraryMetadata");

        switch (Configuration.Mode)
        {
            case CanonicalLibraryMetadataReadSourceMode.legacy:
                return MakeResult(legacySnapshot, legacySnapshot, canonicalSnapshot, null, null,
                    CanonicalLibraryMetadataReadFallback.legacyDefault,
                    new[] { evaluatedDiag, LegacyReturnedDiag(syncRunID, trigger, nodeRole, "defaultLegacy") },
                    false);
            case CanonicalLibraryMetadataReadSourceMode.blocked:
                return MakeResult(legacySnapshot, legacySnapshot, canonicalSnapshot, diffReport, null,
                    CanonicalLibraryMetadataReadFallback.blockedMode,
                    new[]
                    {
                        evaluatedDiag,
                        GuardedBlockedDiag(syncRunID, trigger, nodeRole, "blockedMode"),
                        FallbackDiag(syncRunID, trigger, nodeRole, "blockedMode")
                    },
                    false);
            case CanonicalLibraryMetadataReadSourceMode.parallelCompare:
                return MakeResult(legacySnapshot, legacySnapshot, canonicalSnapshot, diffReport, null,
                    CanonicalLibraryMetadataReadFallback.legacyDefault,
                    new[]
                    {
                        evaluatedDiag,
                        CanonicalCandidateDiag(syncRunID, trigger, nodeRole, "parallelCompare"),
                        OutputDiag(diffReport, syncRunID, trigger, nodeRole),
                        LegacyReturnedDiag(syncRunID, trigger, nodeRole, "parallelCompareReturnsLegacy")
                    },
                    (diffReport?.DivergenceCount ?? 0) > 0);
            case CanonicalLibraryMetadataReadSourceMode.canonicalCandidate:
                return MakeResult(legacySnapshot, legacySnapshot, canonicalSnapshot, diffReport, null,
                    CanonicalLibraryMetadataReadFallback.legacyDefault,
                    new[]
                    {
                        evaluatedDiag,
                        CanonicalCandidateDiag(syncRunID, trigger, nodeRole, "candidateBuiltNotServed"),
                        OutputDiag(diffReport, syncRunID, trigger, nodeRole),
                        LegacyReturnedDiag(syncRunID, trigger, nodeRole, "canonicalCandidateNotServed")
                    },
                    (diffReport?.DivergenceCount ?? 0) > 0);
            case CanonicalLibraryMetadataReadSourceMode.guardedCanonicalRead:
                if (canonicalSnapshot == null || diffReport == null)
                    return MakeResult(legacySnapshot, legacySnapshot, canonicalSnapshot, null, null,
                        CanonicalLibraryMetadataReadFallback.canonicalProjectionMissing,
                        new[]
                        {
                            evaluatedDiag,
                            GuardedBlockedDiag(syncRunID, trigger, nodeRole, "canonicalProjectionMissing"),
                            FallbackDiag(syncRunID, trigger, nodeRole, "canonicalProjectionMissing")
                        },
                        true);

                var gate = CanonicalLibraryMetadataReadCutoverGate.Evaluate(
                    new CanonicalLibraryMetadataReadCutoverGateContext(
                        Configuration, writeSideEvidence,
                        legacyFallbackAvailable, projectionComplete,
                        !diffReport.Divergences.Any(d =>
                            d.Kind == CanonicalLibraryMetadataReadSideDivergenceKind.objectIDMismatch),
                        true, true, true, unresolvedConflictCount),
                    diffReport, Matrix, trigger, nodeRole, syncRunID);

                var diags = new List<CanonicalLibraryMetadataCutoverDiagnostic>
                {
                    evaluatedDiag,
                    CanonicalCandidateDiag(syncRunID, trigger, nodeRole, "guardedCanonicalRead")
                };
                diags.AddRange(gate.Diagnostics);
                diags.Add(OutputDiag(diffReport, syncRunID, trigger, nodeRole));

                if (canonicalReadFailureReason != null)
                {
                    diags.Add(FallbackDiag(syncRunID, trigger, nodeRole, canonicalReadFailureReason));
                    return MakeResult(legacySnapshot, legacySnapshot, canonicalSnapshot, diffReport, gate,
                        CanonicalLibraryMetadataReadFallback.canonicalReadException, diags.ToArray(), true);
                }

                if (!gate.Allowed)
                {
                    diags.Add(GuardedBlockedDiag(syncRunID, trigger, nodeRole,
                        string.Join("+", gate.Blockers.Select(b => b.ToString()))));
                    diags.Add(FallbackDiag(syncRunID, trigger, nodeRole, FallbackReasonFor(gate)));
                    return MakeResult(legacySnapshot, legacySnapshot, canonicalSnapshot, diffReport, gate,
                        FallbackFor(gate), diags.ToArray(),
                        diffReport.DivergenceCount > 0 || diffReport.PathLeakRiskCount > 0);
                }

                diags.Add(Diagnostic(
                    CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataGuardedCanonicalReadAllowed,
                    syncRunID, trigger, nodeRole, "allowed", "explicitInternalTestConfig"));
                diags.Add(Diagnostic(
                    CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataGuardedCanonicalReadServed,
                    syncRunID, trigger, nodeRole, "served", canonicalSnapshot.DiagnosticsSummary));
                return MakeResult(canonicalSnapshot, legacySnapshot, canonicalSnapshot, diffReport, gate,
                    CanonicalLibraryMetadataReadFallback.none, diags.ToArray(), false);
            default:
                throw new InvalidOperationException($"Unknown mode: {Configuration.Mode}");
        }
    }

    private CanonicalLibraryMetadataReadSourceResult MakeResult(
        CanonicalLibraryMetadataReadSnapshot returnedSnapshot,
        CanonicalLibraryMetadataReadSnapshot legacySnapshot,
        CanonicalLibraryMetadataReadSnapshot? canonicalSnapshot,
        CanonicalLibraryMetadataReadSideDiffReport? diffReport,
        CanonicalLibraryMetadataReadCutoverGateResult? gateResult,
        CanonicalLibraryMetadataReadFallback fallback,
        CanonicalLibraryMetadataCutoverDiagnostic[] diagnostics,
        bool fatalForFutureStage)
    {
        var returnedSource = returnedSnapshot.Source;
        var readSource = new CanonicalLibraryMetadataReadSource(returnedSource, returnedSnapshot);
        var limitedDiags = Configuration.RecordDiagnostics
            ? diagnostics.Take(Configuration.MaxDiagnosticsEvents).ToArray()
            : Array.Empty<CanonicalLibraryMetadataCutoverDiagnostic>();

        return new CanonicalLibraryMetadataReadSourceResult(
            Configuration.Mode, returnedSource, readSource, legacySnapshot,
            canonicalSnapshot, diffReport, gateResult, fallback,
            fallback == CanonicalLibraryMetadataReadFallback.none ? 0 : 1,
            returnedSource == CanonicalLibraryMetadataReadProjectionSource.canonical
            && fallback == CanonicalLibraryMetadataReadFallback.none,
            returnedSource == CanonicalLibraryMetadataReadProjectionSource.legacy,
            canonicalSnapshot != null
            && Configuration.Mode != CanonicalLibraryMetadataReadSourceMode.legacy
            && Configuration.Mode != CanonicalLibraryMetadataReadSourceMode.blocked,
            fatalForFutureStage,
            false, false, false, false, false,
            limitedDiags,
            string.Join(",",
                $"mode={Configuration.Mode}",
                $"returned={returnedSource}",
                $"fallback={fallback}",
                $"canonicalServed={returnedSource == CanonicalLibraryMetadataReadProjectionSource.canonical && fallback == CanonicalLibraryMetadataReadFallback.none}",
                $"folders={returnedSnapshot.Folders.Length}",
                $"items={returnedSnapshot.StudyItems.Length}",
                $"notes={returnedSnapshot.StandaloneNotes.Length}",
                "storeMutated=false",
                "syncOrUploadTriggered=false",
                "resourceMoved=false",
                "contentWritten=false",
                "uiMutated=false"
            )
        );
    }

    private CanonicalLibraryMetadataCutoverDiagnostic Diagnostic(
        CanonicalLibraryMetadataCutoverDiagnosticKind kind,
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole, string result, string reason) =>
        new(kind, syncRunID, trigger, nodeRole, result, reason);

    private CanonicalLibraryMetadataCutoverDiagnostic LegacyReturnedDiag(
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole, string reason) =>
        Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSourceLegacyReturned,
            syncRunID, trigger, nodeRole, "legacy", reason);

    private CanonicalLibraryMetadataCutoverDiagnostic CanonicalCandidateDiag(
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole, string reason) =>
        Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadSourceCanonicalCandidateBuilt,
            syncRunID, trigger, nodeRole, "candidate", reason);

    private CanonicalLibraryMetadataCutoverDiagnostic GuardedBlockedDiag(
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole, string reason) =>
        Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataGuardedCanonicalReadBlocked,
            syncRunID, trigger, nodeRole, "blocked", reason);

    private CanonicalLibraryMetadataCutoverDiagnostic FallbackDiag(
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole, string reason) =>
        Diagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataGuardedCanonicalReadFallback,
            syncRunID, trigger, nodeRole, "legacy", reason);

    private CanonicalLibraryMetadataCutoverDiagnostic OutputDiag(
        CanonicalLibraryMetadataReadSideDiffReport? diffReport,
        string? syncRunID, CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole) =>
        Diagnostic(
            diffReport?.Equivalent == true
                ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadOutputEquivalent
                : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataReadOutputDivergent,
            syncRunID, trigger, nodeRole,
            diffReport?.Equivalent == true ? "equivalent" : "divergent",
            diffReport?.DiagnosticsSummary ?? "canonicalCandidateMissing");

    private static CanonicalLibraryMetadataReadFallback FallbackFor(
        CanonicalLibraryMetadataReadCutoverGateResult gate)
    {
        if (gate.Blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.pathLeakRisk))
            return CanonicalLibraryMetadataReadFallback.pathLeakRisk;
        if (gate.Blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.unsupportedObject))
            return CanonicalLibraryMetadataReadFallback.unsupportedObject;
        if (gate.Blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.readSideDivergence))
            return CanonicalLibraryMetadataReadFallback.divergenceDetected;
        if (gate.Blockers.Contains(CanonicalLibraryMetadataReadCutoverBlocker.canonicalProjectionIncomplete))
            return CanonicalLibraryMetadataReadFallback.canonicalProjectionMissing;
        return CanonicalLibraryMetadataReadFallback.gateBlocked;
    }

    private static string FallbackReasonFor(CanonicalLibraryMetadataReadCutoverGateResult gate)
    {
        var reason = string.Join("+", gate.Blockers.Select(b => b.ToString()));
        return string.IsNullOrEmpty(reason) ? "gateBlocked" : reason;
    }
}

public sealed record CanonicalLibraryMetadataRetirementCandidateEvidence : IEquatable<CanonicalLibraryMetadataRetirementCandidateEvidence>
{
    public bool WriteSideCanarySuccessEvidence { get; }
    public bool GuardedReadSourceEvidence { get; }
    public bool ObservationWindowComplete { get; }
    public bool LegacyFallbackReady { get; }
    public bool DivergenceZero { get; }
    public int UnsupportedObjectCount { get; }
    public int UnresolvedConflictCount { get; }
    public int RollbackFatalCount { get; }
    public bool ReadSourceStable { get; }
    public bool OtherDomainsUnaffected { get; }

    public CanonicalLibraryMetadataRetirementCandidateEvidence(
        bool writeSideCanarySuccessEvidence,
        bool guardedReadSourceEvidence,
        bool observationWindowComplete,
        bool legacyFallbackReady,
        bool divergenceZero,
        int unsupportedObjectCount = 0,
        int unresolvedConflictCount = 0,
        int rollbackFatalCount = 0,
        bool readSourceStable = true,
        bool otherDomainsUnaffected = true)
    {
        WriteSideCanarySuccessEvidence = writeSideCanarySuccessEvidence;
        GuardedReadSourceEvidence = guardedReadSourceEvidence;
        ObservationWindowComplete = observationWindowComplete;
        LegacyFallbackReady = legacyFallbackReady;
        DivergenceZero = divergenceZero;
        UnsupportedObjectCount = Math.Max(0, unsupportedObjectCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        RollbackFatalCount = Math.Max(0, rollbackFatalCount);
        ReadSourceStable = readSourceStable;
        OtherDomainsUnaffected = otherDomainsUnaffected;
    }

    public virtual bool Equals(CanonicalLibraryMetadataRetirementCandidateEvidence? other) =>
        other is not null && WriteSideCanarySuccessEvidence == other.WriteSideCanarySuccessEvidence;
    public override int GetHashCode() => WriteSideCanarySuccessEvidence.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataRetirementReadiness
{
    notCandidate,
    ready,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataRetirementBlocker
{
    missingWriteSideDomainCutoverEvidence,
    missingReadSideCutoverEvidence,
    observationWindowIncomplete,
    fallbackMissing,
    divergencePresent,
    unresolvedConflict,
    unsupportedObject,
    missingWriteSideCanarySuccessEvidence,
    missingGuardedReadSourceEvidence,
    rollbackFatal,
    readSourceUnstable,
    otherDomainsAffected,
    manualAuditRequired,
    unsafeSideEffect,
    pathLeakRisk,
    runtimeSwitchEnabled,
    defaultReadOrWriteCutoverEnabled,
    legacyDeleted,
    legacyDisabled,
    retirementExecutionAttempted,
    resourceMoveAttempted,
    contentWriteAttempted,
    tombstoneDeleteAttempted
}

public sealed record CanonicalLibraryMetadataRetirementCandidate : IEquatable<CanonicalLibraryMetadataRetirementCandidate>
{
    public bool IsCandidate { get; }
    public CanonicalLibraryMetadataRetirementReadiness Readiness { get; }
    public CanonicalLibraryMetadataRetirementBlocker[] Blockers { get; }

    public CanonicalLibraryMetadataRetirementCandidate(
        bool isCandidate,
        CanonicalLibraryMetadataRetirementReadiness readiness,
        CanonicalLibraryMetadataRetirementBlocker[] blockers)
    {
        IsCandidate = isCandidate;
        Readiness = readiness;
        Blockers = blockers;
    }

    public virtual bool Equals(CanonicalLibraryMetadataRetirementCandidate? other) =>
        other is not null && IsCandidate == other.IsCandidate;
    public override int GetHashCode() => IsCandidate.GetHashCode();
}

public sealed record CanonicalLibraryMetadataRetirementReport : IEquatable<CanonicalLibraryMetadataRetirementReport>
{
    public CanonicalLibraryMetadataRetirementCandidate Candidate { get; }
    public bool LegacyDeleted { get; }
    public bool LegacyDisabled { get; }
    public bool ReportOnly { get; }
    public CanonicalLibraryMetadataCutoverDiagnostic[] Diagnostics { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalLibraryMetadataRetirementReport(
        CanonicalLibraryMetadataRetirementCandidate candidate,
        bool legacyDeleted,
        bool legacyDisabled,
        bool reportOnly,
        CanonicalLibraryMetadataCutoverDiagnostic[] diagnostics,
        string diagnosticsSummary)
    {
        Candidate = candidate;
        LegacyDeleted = legacyDeleted;
        LegacyDisabled = legacyDisabled;
        ReportOnly = reportOnly;
        Diagnostics = diagnostics;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public virtual bool Equals(CanonicalLibraryMetadataRetirementReport? other) =>
        other is not null && Candidate.Equals(other.Candidate);
    public override int GetHashCode() => Candidate.GetHashCode();
}

public static class CanonicalLibraryMetadataRetirementCandidateEvaluator
{
    public static CanonicalLibraryMetadataRetirementReport Evaluate(
        CanonicalLibraryMetadataWriteSideEvidenceLinkage writeSideEvidence,
        bool readSideCutoverEvidenceAvailable,
        bool observationWindowComplete,
        bool fallbackAvailable,
        CanonicalLibraryMetadataReadSideDiffReport diffReport,
        int unresolvedConflictCount = 0,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        var blockers = new List<CanonicalLibraryMetadataRetirementBlocker>();
        if (!writeSideEvidence.WriteSideDomainCutoverComplete)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.missingWriteSideDomainCutoverEvidence);
        if (!readSideCutoverEvidenceAvailable)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.missingReadSideCutoverEvidence);
        if (!observationWindowComplete)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.observationWindowIncomplete);
        if (!fallbackAvailable)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.fallbackMissing);
        if (diffReport.DivergenceCount > 0)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.divergencePresent);
        if (unresolvedConflictCount > 0 || writeSideEvidence.UnresolvedConflictCount > 0)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.unresolvedConflict);
        if (diffReport.UnsupportedObjectCount > 0)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.unsupportedObject);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataRetirementBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        var candidate = new CanonicalLibraryMetadataRetirementCandidate(
            uniqueBlockers.Length == 0,
            uniqueBlockers.Length == 0
                ? CanonicalLibraryMetadataRetirementReadiness.ready
                : CanonicalLibraryMetadataRetirementReadiness.blocked,
            uniqueBlockers);
        var evaluated = new CanonicalLibraryMetadataCutoverDiagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementCandidateEvaluated,
            syncRunID, trigger, nodeRole, candidate.Readiness.ToString(), "reportOnly=true");
        var outcome = new CanonicalLibraryMetadataCutoverDiagnostic(
            candidate.IsCandidate
                ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementCandidateReady
                : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementCandidateBlocked,
            syncRunID, trigger, nodeRole,
            candidate.IsCandidate ? "ready" : "blocked",
            string.Join("+", uniqueBlockers.Select(b => b.ToString())));

        return new CanonicalLibraryMetadataRetirementReport(
            candidate, false, false, true, new[] { evaluated, outcome },
            $"candidate={candidate.IsCandidate},readiness={candidate.Readiness},blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))},legacyDeleted=false,legacyDisabled=false,reportOnly=true"
        );
    }

    public static CanonicalLibraryMetadataRetirementReport UpdateAfterGuardedRead(
        CanonicalLibraryMetadataRetirementCandidateEvidence evidence,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        var blockers = new List<CanonicalLibraryMetadataRetirementBlocker>();
        if (!evidence.WriteSideCanarySuccessEvidence)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.missingWriteSideCanarySuccessEvidence);
        if (!evidence.GuardedReadSourceEvidence)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.missingGuardedReadSourceEvidence);
        if (!evidence.ObservationWindowComplete)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.observationWindowIncomplete);
        if (!evidence.LegacyFallbackReady)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.fallbackMissing);
        if (!evidence.DivergenceZero)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.divergencePresent);
        if (evidence.UnsupportedObjectCount > 0)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.unsupportedObject);
        if (evidence.UnresolvedConflictCount > 0)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.unresolvedConflict);
        if (evidence.RollbackFatalCount > 0)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.rollbackFatal);
        if (!evidence.ReadSourceStable)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.readSourceUnstable);
        if (!evidence.OtherDomainsUnaffected)
            blockers.Add(CanonicalLibraryMetadataRetirementBlocker.otherDomainsAffected);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataRetirementBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        var candidate = new CanonicalLibraryMetadataRetirementCandidate(
            uniqueBlockers.Length == 0,
            uniqueBlockers.Length == 0
                ? CanonicalLibraryMetadataRetirementReadiness.ready
                : CanonicalLibraryMetadataRetirementReadiness.blocked,
            uniqueBlockers);
        var updated = new CanonicalLibraryMetadataCutoverDiagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementCandidateUpdated,
            syncRunID, trigger, nodeRole, candidate.Readiness.ToString(), "reportOnly=true");
        var outcome = new CanonicalLibraryMetadataCutoverDiagnostic(
            uniqueBlockers.Length == 0
                ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementCandidateReady
                : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementStillBlocked,
            syncRunID, trigger, nodeRole,
            uniqueBlockers.Length == 0 ? "candidate" : "blocked",
            string.Join("+", uniqueBlockers.Select(b => b.ToString())));

        return new CanonicalLibraryMetadataRetirementReport(
            candidate, false, false, true, new[] { updated, outcome },
            $"candidate={candidate.IsCandidate},readiness={candidate.Readiness},blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))},legacyDeleted=false,legacyDisabled=false,reportOnly=true"
        );
    }
}
