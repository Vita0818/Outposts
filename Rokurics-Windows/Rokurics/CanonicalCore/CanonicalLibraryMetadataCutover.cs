using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCutoverDomain
{
    folderMetadata,
    studyItemMetadata,
    standaloneNoteMetadata
}

public static class CanonicalLibraryMetadataCutoverDomainExtensions
{
    public static CanonicalProductionDomain ToProductionDomain(this CanonicalLibraryMetadataCutoverDomain domain) => domain switch
    {
        CanonicalLibraryMetadataCutoverDomain.folderMetadata => CanonicalProductionDomain.folders,
        CanonicalLibraryMetadataCutoverDomain.studyItemMetadata => CanonicalProductionDomain.studyItems,
        CanonicalLibraryMetadataCutoverDomain.standaloneNoteMetadata => CanonicalProductionDomain.standaloneNotes,
        _ => CanonicalProductionDomain.folders
    };

    public static CanonicalCutoverDomain ToCutoverDomain(this CanonicalLibraryMetadataCutoverDomain domain) => domain switch
    {
        CanonicalLibraryMetadataCutoverDomain.folderMetadata => CanonicalCutoverDomain.folders,
        CanonicalLibraryMetadataCutoverDomain.studyItemMetadata => CanonicalCutoverDomain.studyItems,
        CanonicalLibraryMetadataCutoverDomain.standaloneNoteMetadata => CanonicalCutoverDomain.standaloneNotes,
        _ => CanonicalCutoverDomain.folders
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCutoverActionKind
{
    folderApply,
    folderSend,
    studyItemApply,
    studyItemSend,
    standaloneNoteApply,
    standaloneNoteSend,
    conflictRecord,
    tombstoneMarkerUnsupportedForThisRound,
    unsupported
}

public static class CanonicalLibraryMetadataCutoverActionKindExtensions
{
    public static bool IsExecutableMetadata(this CanonicalLibraryMetadataCutoverActionKind kind) => kind switch
    {
        CanonicalLibraryMetadataCutoverActionKind.folderApply or
        CanonicalLibraryMetadataCutoverActionKind.folderSend or
        CanonicalLibraryMetadataCutoverActionKind.studyItemApply or
        CanonicalLibraryMetadataCutoverActionKind.studyItemSend or
        CanonicalLibraryMetadataCutoverActionKind.standaloneNoteApply or
        CanonicalLibraryMetadataCutoverActionKind.standaloneNoteSend => true,
        _ => false
    };

    public static bool IsSend(this CanonicalLibraryMetadataCutoverActionKind kind) =>
        kind == CanonicalLibraryMetadataCutoverActionKind.folderSend ||
        kind == CanonicalLibraryMetadataCutoverActionKind.studyItemSend ||
        kind == CanonicalLibraryMetadataCutoverActionKind.standaloneNoteSend;

    public static bool IsApply(this CanonicalLibraryMetadataCutoverActionKind kind) =>
        kind == CanonicalLibraryMetadataCutoverActionKind.folderApply ||
        kind == CanonicalLibraryMetadataCutoverActionKind.studyItemApply ||
        kind == CanonicalLibraryMetadataCutoverActionKind.standaloneNoteApply;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCutoverFailure
{
    disabled,
    unsupportedMode,
    unsupportedDomain,
    unsupportedObjectKind,
    unsupportedAction,
    missingToken,
    missingOwnerApproval,
    missingRollback,
    missingNoCommitEvidence,
    missingDryRunEquivalence,
    missingExecutionShadowEvidence,
    missingRealDataShadowCopyEvidence,
    blockingDivergence,
    unresolvedConflict,
    activeVsTombstoneConflict,
    legacyFallbackUnavailable,
    missingMetadataManifestRouteEvidence,
    productionPortUnavailable,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    rollbackVerificationMissing,
    productionRootEnabledByDefault,
    testRootMissing,
    objectIDMismatch,
    objectKindMismatch,
    expectedMetadataHashMissing,
    expectedBusinessModifiedAtMissing,
    businessModifiedAtDirectionMismatch,
    applyFailureBeforeCommit,
    applyFailureAfterPartialCommit,
    postconditionMismatch,
    rollbackFailure,
    parentMissing,
    cycleDetected,
    resourceMoveAttempted,
    tombstoneUnsupportedForThisRound,
    conflictDetected,
    missingInternalCanaryConfiguration,
    canaryBudgetAboveOneDenied,
    missingCanaryStageEvidence,
    canaryStageBlocked,
    canaryStageOrderViolation,
    observationWindowIncomplete,
    previousStageFailure,
    previousStageRollbackFailure,
    previousStageBlockingDivergence,
    previousStageUnresolvedConflict,
    previousStagePostconditionFailure,
    previousStageUnsupportedObject,
    allEligibleCanaryDenied,
    activePilotNotLibraryMetadata,
    matrixValidationBlocked,
    defaultEnablementDenied,
    missingReadSideParallelEvidence,
    readSideParallelDivergence,
    multipleEligibleCandidatesDenied,
    folderHierarchyMutationUnsupported,
    standaloneNoteContentMutationUnsupported,
    commitExecutorUnavailable,
    objectIDInstability,
    runtimeSwitchDenied,
    peerSnapshotUnavailable,
}

public sealed class CanonicalLibraryMetadataCutoverCandidate : IEquatable<CanonicalLibraryMetadataCutoverCandidate>
{
    public string Id => Action.ActionID;

    public CanonicalApplyAction Action { get; set; }
    public CanonicalLibraryObject? LocalObject { get; set; }
    public CanonicalLibraryObject? PeerObject { get; set; }
    public string? RollbackCheckpointID { get; set; }
    public bool UnresolvedConflict { get; set; }
    public bool ParentMissingKnown { get; set; }
    public string RoutePath { get; set; }

    public CanonicalLibraryMetadataCutoverCandidate(
        CanonicalApplyAction action,
        CanonicalLibraryObject? localObject,
        CanonicalLibraryObject? peerObject,
        string? rollbackCheckpointID = null,
        bool unresolvedConflict = false,
        bool parentMissingKnown = false,
        string routePath = "/sync/apply-metadata")
    {
        Action = action;
        LocalObject = localObject;
        PeerObject = peerObject;
        RollbackCheckpointID = rollbackCheckpointID != null
            ? CanonicalProductionRedaction.SafeIdentifier(rollbackCheckpointID, "library-metadata-checkpoint")
            : null;
        UnresolvedConflict = unresolvedConflict;
        ParentMissingKnown = parentMissingKnown;
        RoutePath = CanonicalProductionRedaction.SafeDiagnosticText(routePath) ?? "/sync/apply-metadata";
    }

    public string ObjectID => Action.Target.ObjectID;

    public CanonicalObjectKind ObjectKind =>
        ExpectedObject?.Kind ?? LocalObject?.Kind ?? PeerObject?.Kind ?? InferredObjectKind;

    public CanonicalLibraryMetadataCutoverDomain Domain => ObjectKind switch
    {
        CanonicalObjectKind.folder => CanonicalLibraryMetadataCutoverDomain.folderMetadata,
        CanonicalObjectKind.standaloneNote => CanonicalLibraryMetadataCutoverDomain.standaloneNoteMetadata,
        _ => CanonicalLibraryMetadataCutoverDomain.studyItemMetadata
    };

    public CanonicalLibraryMetadataCutoverActionKind CutoverActionKind => Action.Kind switch
    {
        CanonicalApplyActionKind.folderMetadataApply => ObjectKind == CanonicalObjectKind.folder
            ? CanonicalLibraryMetadataCutoverActionKind.folderApply : CanonicalLibraryMetadataCutoverActionKind.unsupported,
        CanonicalApplyActionKind.folderMetadataSend => ObjectKind == CanonicalObjectKind.folder
            ? CanonicalLibraryMetadataCutoverActionKind.folderSend : CanonicalLibraryMetadataCutoverActionKind.unsupported,
        CanonicalApplyActionKind.studyItemMetadataApply => ObjectKind == CanonicalObjectKind.standaloneNote
            ? CanonicalLibraryMetadataCutoverActionKind.standaloneNoteApply : CanonicalLibraryMetadataCutoverActionKind.studyItemApply,
        CanonicalApplyActionKind.studyItemMetadataSend => ObjectKind == CanonicalObjectKind.standaloneNote
            ? CanonicalLibraryMetadataCutoverActionKind.standaloneNoteSend : CanonicalLibraryMetadataCutoverActionKind.studyItemSend,
        CanonicalApplyActionKind.conflictRecord => CanonicalLibraryMetadataCutoverActionKind.conflictRecord,
        CanonicalApplyActionKind.libraryTombstoneApply or CanonicalApplyActionKind.libraryTombstoneSend =>
            CanonicalLibraryMetadataCutoverActionKind.tombstoneMarkerUnsupportedForThisRound,
        _ => CanonicalLibraryMetadataCutoverActionKind.unsupported
    };

    public CanonicalLibraryObject? ExpectedObject => Action.Source switch
    {
        CanonicalActionSource.peer => PeerObject ?? LocalObject,
        CanonicalActionSource.local => LocalObject ?? PeerObject,
        CanonicalActionSource.planner => PeerObject ?? LocalObject,
        _ => PeerObject ?? LocalObject
    };

    public CanonicalHash? ExpectedMetadataHash => ExpectedObject?.MetadataHash;

    public CanonicalTimestamp? ExpectedBusinessModifiedAt => ExpectedObject?.BusinessModifiedAt;

    public string EffectiveRollbackCheckpointID =>
        RollbackCheckpointID ?? $"library-metadata-cutover-{ObjectID}-{CutoverActionKind}";

    public string MetadataTitle => ExpectedObject?.Metadata?.Title ?? "metadata";

    public string ParentSummary
    {
        get
        {
            var obj = ExpectedObject;
            if (obj == null) return "parent=unknown";
            if (obj.Folder?.Metadata != null)
                return $"parent={obj.Folder.Metadata.ParentID?.RawValue ?? "root"}";
            if (obj.StudyItem?.Metadata != null)
            {
                var item = obj.StudyItem.Metadata;
                var folders = string.Join("|", item.FolderIDs.Select(f => f.RawValue).Take(3));
                var parents = string.Join("|", item.ParentReferences.Select(p => p.ParentID.RawValue).Take(3));
                return $"folders={(string.IsNullOrEmpty(folders) ? "none" : folders)},parents={(string.IsNullOrEmpty(parents) ? "none" : parents)}";
            }
            return "parent=none";
        }
    }

    public int TagCount => ExpectedObject?.StudyItem?.Metadata.Tags.Count ?? 0;

    public string FilingSummary =>
        ExpectedObject?.StudyItem?.Metadata.FilingPath.Components is { } comps
            ? string.Join("/", comps)
            : "none";

    public string ColorSummary => ExpectedObject?.Folder?.Metadata.ColorToken ?? "none";

    public List<string> LogicalResourceTokens =>
        ExpectedObject?.StudyItem?.Metadata.LogicalResourceTokens ?? new List<string>();

    public bool HasResourceMoveAttempt
    {
        get
        {
            var local = LocalObject?.StudyItem?.Metadata.LogicalResourceTokens;
            var peer = PeerObject?.StudyItem?.Metadata.LogicalResourceTokens;
            if ((local == null || local.Count == 0) && (peer == null || peer.Count == 0))
                return false;
            return !(local?.SequenceEqual(peer ?? new List<string>()) ?? (peer?.Count == 0));
        }
    }

    public bool FolderHierarchyMutationAttempted
    {
        get
        {
            var local = LocalObject?.Folder?.Metadata;
            var peer = PeerObject?.Folder?.Metadata;
            if (local == null || peer == null) return false;
            return !Equals(local.ParentID, peer.ParentID)
                || !Equals(local.HierarchyPath, peer.HierarchyPath)
                || local.HierarchyLevel != peer.HierarchyLevel;
        }
    }

    public bool HasObjectIDInstability => ObjectKind switch
    {
        CanonicalObjectKind.folder => ExpectedObject?.Folder?.FolderID.RawValue != ObjectID,
        CanonicalObjectKind.standaloneNote => ExpectedObject?.StandaloneNote?.NoteID.RawValue != ObjectID,
        CanonicalObjectKind.standaloneStudyItem or CanonicalObjectKind.recordingAssociatedStudyItem =>
            ExpectedObject?.StudyItem?.ItemID.RawValue != ObjectID,
        _ => false
    };

    public bool HasActiveVsTombstoneConflict
    {
        get
        {
            if (LocalObject == null || PeerObject == null) return false;
            return LocalObject.IsDeleted != PeerObject.IsDeleted && CutoverActionKind == CanonicalLibraryMetadataCutoverActionKind.conflictRecord;
        }
    }

    public static List<CanonicalLibraryMetadataCutoverCandidate> Candidates(
        CanonicalLibrarySyncPlan libraryPlan,
        CanonicalManifest localManifest,
        CanonicalManifest peerManifest,
        string rollbackCheckpointPrefix = "library-metadata-cutover")
    {
        var localObjects = localManifest.LibraryObjects.ToDictionary(o => o.ObjectID);
        var peerObjects = peerManifest.LibraryObjects.ToDictionary(o => o.ObjectID);

        return libraryPlan.ApplyActions
            .Where(a => a.Kind == CanonicalApplyActionKind.folderMetadataApply
                || a.Kind == CanonicalApplyActionKind.folderMetadataSend
                || a.Kind == CanonicalApplyActionKind.studyItemMetadataApply
                || a.Kind == CanonicalApplyActionKind.studyItemMetadataSend
                || a.Kind == CanonicalApplyActionKind.conflictRecord
                || a.Kind == CanonicalApplyActionKind.libraryTombstoneApply
                || a.Kind == CanonicalApplyActionKind.libraryTombstoneSend)
            .Select(a =>
            {
                var objectID = new CanonicalLibraryObjectID(a.Target.ObjectID);
                return new CanonicalLibraryMetadataCutoverCandidate(
                    a,
                    localObjects.TryGetValue(objectID, out var lo) ? lo : null,
                    peerObjects.TryGetValue(objectID, out var po) ? po : null,
                    rollbackCheckpointID: $"{rollbackCheckpointPrefix}-{a.Target.ObjectID}");
            })
            .ToList();
    }

    public static bool FolderHierarchyCycleDetected(List<CanonicalLibraryMetadataCutoverCandidate> candidates)
    {
        var parentsByFolderID = new Dictionary<string, string>();
        foreach (var candidate in candidates)
        {
            var folder = candidate.ExpectedObject?.Folder?.Metadata;
            if (folder == null) continue;
            if (folder.ParentID?.RawValue == folder.FolderID.RawValue) return true;
            if (folder.ParentID is { } parentID)
                parentsByFolderID[folder.FolderID.RawValue] = parentID.RawValue;
        }

        foreach (var folderID in parentsByFolderID.Keys)
        {
            var seen = new HashSet<string>();
            string? cursor = folderID;
            while (cursor != null)
            {
                if (!seen.Add(cursor)) return true;
                parentsByFolderID.TryGetValue(cursor, out cursor);
            }
        }
        return false;
    }

    private CanonicalObjectKind InferredObjectKind => Action.Kind switch
    {
        CanonicalApplyActionKind.folderMetadataApply or CanonicalApplyActionKind.folderMetadataSend =>
            CanonicalObjectKind.folder,
        CanonicalApplyActionKind.studyItemMetadataApply or CanonicalApplyActionKind.studyItemMetadataSend =>
            CanonicalObjectKind.standaloneStudyItem,
        _ => CanonicalObjectKind.unknownUnsupported
    };

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCutoverCandidate other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCutoverCandidate? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalLibraryMetadataCutoverCandidate left, CanonicalLibraryMetadataCutoverCandidate right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCutoverCandidate left, CanonicalLibraryMetadataCutoverCandidate right) => !left.Equals(right);
}

public static class CanonicalLibraryObjectExtensions
{
    public static CanonicalLibraryMetadata? GetMetadata(this CanonicalLibraryObject obj) => obj.Kind switch
    {
        CanonicalObjectKind.folder => obj.Folder?.Metadata is { } fm ? new CanonicalLibraryMetadata(
            obj.ObjectID, obj.Kind, fm.Name, fm.MetadataHash,
            fm.BusinessModifiedAt, fm.IsDeleted, fm.DeletedAt) : null,
        CanonicalObjectKind.standaloneStudyItem or CanonicalObjectKind.standaloneNote or CanonicalObjectKind.recordingAssociatedStudyItem =>
            obj.StudyItem?.Metadata is { } sm ? new CanonicalLibraryMetadata(
                obj.ObjectID, obj.Kind, sm.Title, sm.MetadataHash,
                sm.BusinessModifiedAt, sm.IsDeleted, sm.DeletedAt) : null,
        _ => null
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataApplyPortMode
{
    disabled,
    dryRun,
    fakeInMemory,
    testRootBound,
    productionRootDisabled,
    productionRootBound,
    productionRootUnsupported
}

public static class CanonicalLibraryMetadataApplyPortModeExtensions
{
    public static bool IsNonDryRunRootBound(this CanonicalLibraryMetadataApplyPortMode mode) =>
        mode == CanonicalLibraryMetadataApplyPortMode.testRootBound ||
        mode == CanonicalLibraryMetadataApplyPortMode.productionRootBound;

    public static bool IsDefaultDisabled(this CanonicalLibraryMetadataApplyPortMode mode) =>
        mode == CanonicalLibraryMetadataApplyPortMode.disabled ||
        mode == CanonicalLibraryMetadataApplyPortMode.dryRun ||
        mode == CanonicalLibraryMetadataApplyPortMode.productionRootDisabled;
}

public sealed class CanonicalLibraryMetadataCutoverEvidence : IEquatable<CanonicalLibraryMetadataCutoverEvidence>
{
    public bool NoCommitEvidenceAvailable { get; set; }
    public bool RealDataShadowCopyVerified { get; set; }
    public bool ExecutionShadowVerified { get; set; }
    public bool DryRunEquivalenceVerified { get; set; }
    public bool NoBlockingDivergence { get; set; }
    public bool NoUnresolvedConflict { get; set; }
    public bool MetadataManifestRouteEvidenceAvailable { get; set; }
    public bool ProductionPortAvailable { get; set; }
    public bool RealRootBoundApplyPortAvailable { get; set; }
    public CanonicalLibraryMetadataApplyPortMode ApplyPortMode { get; set; }
    public bool RootBoundWriteAvailable { get; set; }
    public bool AtomicReplaceAvailable { get; set; }
    public bool RollbackCheckpointAvailable { get; set; }
    public bool RollbackVerified { get; set; }
    public bool ProductionRootDisabledByDefault { get; set; }
    public bool TestRootUsed { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public CanonicalRollbackPlan? RollbackPlan { get; set; }
    public bool RollbackRehearsalPassed { get; set; }
    public bool ReadSideParallelEquivalent { get; set; }
    public CanonicalLibraryMetadataCanaryStageEvidence? CanaryStageEvidence { get; set; }

    public CanonicalLibraryMetadataCutoverEvidence(
        bool noCommitEvidenceAvailable = false,
        bool realDataShadowCopyVerified = false,
        bool executionShadowVerified = false,
        bool dryRunEquivalenceVerified = false,
        bool noBlockingDivergence = false,
        bool noUnresolvedConflict = false,
        bool metadataManifestRouteEvidenceAvailable = false,
        bool productionPortAvailable = false,
        bool realRootBoundApplyPortAvailable = false,
        CanonicalLibraryMetadataApplyPortMode applyPortMode = CanonicalLibraryMetadataApplyPortMode.disabled,
        bool rootBoundWriteAvailable = false,
        bool atomicReplaceAvailable = false,
        bool rollbackCheckpointAvailable = false,
        bool rollbackVerified = false,
        bool productionRootDisabledByDefault = false,
        bool testRootUsed = false,
        bool legacyFallbackAvailable = false,
        CanonicalRollbackPlan? rollbackPlan = null,
        bool rollbackRehearsalPassed = false,
        bool readSideParallelEquivalent = false,
        CanonicalLibraryMetadataCanaryStageEvidence? canaryStageEvidence = null)
    {
        NoCommitEvidenceAvailable = noCommitEvidenceAvailable;
        RealDataShadowCopyVerified = realDataShadowCopyVerified;
        ExecutionShadowVerified = executionShadowVerified;
        DryRunEquivalenceVerified = dryRunEquivalenceVerified;
        NoBlockingDivergence = noBlockingDivergence;
        NoUnresolvedConflict = noUnresolvedConflict;
        MetadataManifestRouteEvidenceAvailable = metadataManifestRouteEvidenceAvailable;
        ProductionPortAvailable = productionPortAvailable;
        RealRootBoundApplyPortAvailable = realRootBoundApplyPortAvailable;
        ApplyPortMode = applyPortMode;
        RootBoundWriteAvailable = rootBoundWriteAvailable;
        AtomicReplaceAvailable = atomicReplaceAvailable;
        RollbackCheckpointAvailable = rollbackCheckpointAvailable;
        RollbackVerified = rollbackVerified;
        ProductionRootDisabledByDefault = productionRootDisabledByDefault;
        TestRootUsed = testRootUsed;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        RollbackPlan = rollbackPlan;
        RollbackRehearsalPassed = rollbackRehearsalPassed;
        ReadSideParallelEquivalent = readSideParallelEquivalent;
        CanaryStageEvidence = canaryStageEvidence;
    }

    public static CanonicalLibraryMetadataCutoverEvidence Passing(CanonicalRollbackPlan rollbackPlan) =>
        new(
            noCommitEvidenceAvailable: true,
            realDataShadowCopyVerified: true,
            executionShadowVerified: true,
            dryRunEquivalenceVerified: true,
            noBlockingDivergence: true,
            noUnresolvedConflict: true,
            metadataManifestRouteEvidenceAvailable: true,
            productionPortAvailable: true,
            realRootBoundApplyPortAvailable: true,
            applyPortMode: CanonicalLibraryMetadataApplyPortMode.testRootBound,
            rootBoundWriteAvailable: true,
            atomicReplaceAvailable: true,
            rollbackCheckpointAvailable: true,
            rollbackVerified: true,
            productionRootDisabledByDefault: true,
            testRootUsed: true,
            legacyFallbackAvailable: true,
            rollbackPlan: rollbackPlan,
            rollbackRehearsalPassed: true,
            readSideParallelEquivalent: true);

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCutoverEvidence other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCutoverEvidence? other) =>
        other is not null &&
        NoCommitEvidenceAvailable == other.NoCommitEvidenceAvailable &&
        RealDataShadowCopyVerified == other.RealDataShadowCopyVerified &&
        ExecutionShadowVerified == other.ExecutionShadowVerified &&
        DryRunEquivalenceVerified == other.DryRunEquivalenceVerified &&
        NoBlockingDivergence == other.NoBlockingDivergence &&
        NoUnresolvedConflict == other.NoUnresolvedConflict &&
        MetadataManifestRouteEvidenceAvailable == other.MetadataManifestRouteEvidenceAvailable &&
        ProductionPortAvailable == other.ProductionPortAvailable &&
        RealRootBoundApplyPortAvailable == other.RealRootBoundApplyPortAvailable &&
        ApplyPortMode == other.ApplyPortMode &&
        RootBoundWriteAvailable == other.RootBoundWriteAvailable &&
        AtomicReplaceAvailable == other.AtomicReplaceAvailable &&
        RollbackCheckpointAvailable == other.RollbackCheckpointAvailable &&
        RollbackVerified == other.RollbackVerified &&
        ProductionRootDisabledByDefault == other.ProductionRootDisabledByDefault &&
        TestRootUsed == other.TestRootUsed &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        EqualityComparer<CanonicalRollbackPlan?>.Default.Equals(RollbackPlan, other.RollbackPlan) &&
        RollbackRehearsalPassed == other.RollbackRehearsalPassed &&
        ReadSideParallelEquivalent == other.ReadSideParallelEquivalent &&
        EqualityComparer<CanonicalLibraryMetadataCanaryStageEvidence?>.Default.Equals(CanaryStageEvidence, other.CanaryStageEvidence);
    public override int GetHashCode() =>
        HashCode.Combine(NoCommitEvidenceAvailable, RealDataShadowCopyVerified, ExecutionShadowVerified,
            DryRunEquivalenceVerified, NoBlockingDivergence, NoUnresolvedConflict, MetadataManifestRouteEvidenceAvailable,
            ProductionPortAvailable, RealRootBoundApplyPortAvailable, ApplyPortMode, RootBoundWriteAvailable,
            AtomicReplaceAvailable, RollbackCheckpointAvailable, RollbackVerified, ProductionRootDisabledByDefault,
            TestRootUsed, LegacyFallbackAvailable, RollbackPlan, RollbackRehearsalPassed, ReadSideParallelEquivalent,
            CanaryStageEvidence);
    public static bool operator ==(CanonicalLibraryMetadataCutoverEvidence left, CanonicalLibraryMetadataCutoverEvidence right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCutoverEvidence left, CanonicalLibraryMetadataCutoverEvidence right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCanaryStage
{
    disabled,
    n1,
    n3,
    n10,
    allEligible
}

public static class CanonicalLibraryMetadataCanaryStageExtensions
{
    public static bool IsExecutable(this CanonicalLibraryMetadataCanaryStage stage) =>
        stage != CanonicalLibraryMetadataCanaryStage.disabled;

    public static CanonicalLibraryMetadataCanaryStage? PreviousStage(this CanonicalLibraryMetadataCanaryStage stage) => stage switch
    {
        CanonicalLibraryMetadataCanaryStage.disabled => null,
        CanonicalLibraryMetadataCanaryStage.n1 => CanonicalLibraryMetadataCanaryStage.disabled,
        CanonicalLibraryMetadataCanaryStage.n3 => CanonicalLibraryMetadataCanaryStage.n1,
        CanonicalLibraryMetadataCanaryStage.n10 => CanonicalLibraryMetadataCanaryStage.n3,
        CanonicalLibraryMetadataCanaryStage.allEligible => CanonicalLibraryMetadataCanaryStage.n10,
        _ => null
    };

    public static int NominalCanaryBudget(this CanonicalLibraryMetadataCanaryStage stage) => stage switch
    {
        CanonicalLibraryMetadataCanaryStage.disabled => 0,
        CanonicalLibraryMetadataCanaryStage.n1 => 1,
        CanonicalLibraryMetadataCanaryStage.n3 => 3,
        CanonicalLibraryMetadataCanaryStage.n10 => 10,
        CanonicalLibraryMetadataCanaryStage.allEligible => int.MaxValue,
        _ => 0
    };

    public static int MinimumPreviousStageSuccessCount(this CanonicalLibraryMetadataCanaryStage stage) => stage switch
    {
        CanonicalLibraryMetadataCanaryStage.disabled or CanonicalLibraryMetadataCanaryStage.n1 => 0,
        CanonicalLibraryMetadataCanaryStage.n3 => 1,
        CanonicalLibraryMetadataCanaryStage.n10 => 3,
        CanonicalLibraryMetadataCanaryStage.allEligible => 10,
        _ => 0
    };
}

public sealed class CanonicalLibraryMetadataCanaryStagePolicy : IEquatable<CanonicalLibraryMetadataCanaryStagePolicy>
{
    public CanonicalLibraryMetadataCanaryStage RequestedStage { get; set; }
    public bool AllowCandidateExecution { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }

    public CanonicalLibraryMetadataCanaryStagePolicy(
        CanonicalLibraryMetadataCanaryStage requestedStage = CanonicalLibraryMetadataCanaryStage.disabled,
        bool allowCandidateExecution = false,
        bool runtimeSwitchEnabled = false)
    {
        RequestedStage = requestedStage;
        AllowCandidateExecution = allowCandidateExecution;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
    }

    public static readonly CanonicalLibraryMetadataCanaryStagePolicy Disabled = new();
    public int CanaryBudget => RequestedStage.NominalCanaryBudget();

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryStagePolicy other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryStagePolicy? other) =>
        other is not null && RequestedStage == other.RequestedStage &&
        AllowCandidateExecution == other.AllowCandidateExecution &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled;
    public override int GetHashCode() => HashCode.Combine(RequestedStage, AllowCandidateExecution, RuntimeSwitchEnabled);
    public static bool operator ==(CanonicalLibraryMetadataCanaryStagePolicy left, CanonicalLibraryMetadataCanaryStagePolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryStagePolicy left, CanonicalLibraryMetadataCanaryStagePolicy right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataStageEvidenceStatus
{
    missing,
    incomplete,
    passed,
    failed,
    blocked
}

public sealed class CanonicalLibraryMetadataCanaryStageEvidence : IEquatable<CanonicalLibraryMetadataCanaryStageEvidence>
{
    public CanonicalLibraryMetadataCanaryStage Stage { get; set; }
    public CanonicalLibraryMetadataCanaryStage? PreviousStage { get; set; }
    public CanonicalLibraryMetadataStageEvidenceStatus Status { get; set; }
    public int SuccessfulCommitCount { get; set; }
    public int FailedCommitCount { get; set; }
    public int RollbackFailureCount { get; set; }
    public int BlockingDivergenceCount { get; set; }
    public int UnresolvedConflictCount { get; set; }
    public int PostconditionFailureCount { get; set; }
    public int UnsupportedObjectCount { get; set; }
    public int ResourceMoveAttemptCount { get; set; }
    public int FolderCycleCount { get; set; }
    public int ObjectIDInstabilityCount { get; set; }
    public int SuppressedLegacyDuplicateCount { get; set; }
    public int ReadSideParallelDivergenceCount { get; set; }
    public bool NoCommitEvidenceAvailable { get; set; }
    public bool ObservationWindowComplete { get; set; }
    public string? ObservationWindowID { get; set; }

    public CanonicalLibraryMetadataCanaryStageEvidence(
        CanonicalLibraryMetadataCanaryStage stage,
        CanonicalLibraryMetadataCanaryStage? previousStage = null,
        CanonicalLibraryMetadataStageEvidenceStatus status = CanonicalLibraryMetadataStageEvidenceStatus.missing,
        int successfulCommitCount = 0,
        int failedCommitCount = 0,
        int rollbackFailureCount = 0,
        int blockingDivergenceCount = 0,
        int unresolvedConflictCount = 0,
        int postconditionFailureCount = 0,
        int unsupportedObjectCount = 0,
        int resourceMoveAttemptCount = 0,
        int folderCycleCount = 0,
        int objectIDInstabilityCount = 0,
        int suppressedLegacyDuplicateCount = 0,
        int readSideParallelDivergenceCount = 0,
        bool noCommitEvidenceAvailable = false,
        bool observationWindowComplete = false,
        string? observationWindowID = null)
    {
        Stage = stage;
        PreviousStage = previousStage;
        Status = status;
        SuccessfulCommitCount = Math.Max(0, successfulCommitCount);
        FailedCommitCount = Math.Max(0, failedCommitCount);
        RollbackFailureCount = Math.Max(0, rollbackFailureCount);
        BlockingDivergenceCount = Math.Max(0, blockingDivergenceCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        PostconditionFailureCount = Math.Max(0, postconditionFailureCount);
        UnsupportedObjectCount = Math.Max(0, unsupportedObjectCount);
        ResourceMoveAttemptCount = Math.Max(0, resourceMoveAttemptCount);
        FolderCycleCount = Math.Max(0, folderCycleCount);
        ObjectIDInstabilityCount = Math.Max(0, objectIDInstabilityCount);
        SuppressedLegacyDuplicateCount = Math.Max(0, suppressedLegacyDuplicateCount);
        ReadSideParallelDivergenceCount = Math.Max(0, readSideParallelDivergenceCount);
        NoCommitEvidenceAvailable = noCommitEvidenceAvailable;
        ObservationWindowComplete = observationWindowComplete;
        ObservationWindowID = observationWindowID != null
            ? CanonicalProductionRedaction.SafeIdentifier(observationWindowID, "stage-observation")
            : null;
    }

    public static CanonicalLibraryMetadataCanaryStageEvidence Passing(
        CanonicalLibraryMetadataCanaryStage stage, int successfulCommitCount) =>
        new(stage, previousStage: stage.PreviousStage(), status: CanonicalLibraryMetadataStageEvidenceStatus.passed,
            successfulCommitCount: successfulCommitCount, noCommitEvidenceAvailable: true,
            observationWindowComplete: true, observationWindowID: $"{stage}-observation");

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryStageEvidence other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryStageEvidence? other) =>
        other is not null && Stage == other.Stage && PreviousStage == other.PreviousStage &&
        Status == other.Status && SuccessfulCommitCount == other.SuccessfulCommitCount &&
        FailedCommitCount == other.FailedCommitCount && RollbackFailureCount == other.RollbackFailureCount &&
        BlockingDivergenceCount == other.BlockingDivergenceCount &&
        UnresolvedConflictCount == other.UnresolvedConflictCount &&
        PostconditionFailureCount == other.PostconditionFailureCount &&
        UnsupportedObjectCount == other.UnsupportedObjectCount &&
        ResourceMoveAttemptCount == other.ResourceMoveAttemptCount && FolderCycleCount == other.FolderCycleCount &&
        ObjectIDInstabilityCount == other.ObjectIDInstabilityCount &&
        SuppressedLegacyDuplicateCount == other.SuppressedLegacyDuplicateCount &&
        ReadSideParallelDivergenceCount == other.ReadSideParallelDivergenceCount &&
        NoCommitEvidenceAvailable == other.NoCommitEvidenceAvailable &&
        ObservationWindowComplete == other.ObservationWindowComplete &&
        ObservationWindowID == other.ObservationWindowID;
    public override int GetHashCode() =>
        HashCode.Combine(Stage, PreviousStage, Status, SuccessfulCommitCount, FailedCommitCount, RollbackFailureCount,
            BlockingDivergenceCount, UnresolvedConflictCount, PostconditionFailureCount, UnsupportedObjectCount,
            ResourceMoveAttemptCount, FolderCycleCount, ObjectIDInstabilityCount, SuppressedLegacyDuplicateCount,
            ReadSideParallelDivergenceCount, NoCommitEvidenceAvailable, ObservationWindowComplete, ObservationWindowID);
    public static bool operator ==(CanonicalLibraryMetadataCanaryStageEvidence left, CanonicalLibraryMetadataCanaryStageEvidence right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryStageEvidence left, CanonicalLibraryMetadataCanaryStageEvidence right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataStageEvidenceProbeStatus
{
    missing,
    equivalent,
    verified,
    divergent,
    blocked
}

public static class CanonicalLibraryMetadataStageEvidenceProbeStatusExtensions
{
    public static bool IsPassing(this CanonicalLibraryMetadataStageEvidenceProbeStatus status) =>
        status == CanonicalLibraryMetadataStageEvidenceProbeStatus.equivalent ||
        status == CanonicalLibraryMetadataStageEvidenceProbeStatus.verified;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataStageEvidenceBlocker
{
    missingPreviousStageEvidence,
    observationWindowIncomplete,
    previousStageFailed,
    rollbackFailed,
    blockingDivergence,
    unresolvedConflict,
    postconditionFailed,
    unsupportedObject,
    resourceMoveAttempted,
    hierarchyCycle,
    objectIDInstability,
    missingNoCommitEvidence,
    dryRunEquivalenceMissing,
    executionShadowMissing,
    realDataShadowCopyMissing,
    readOnlyTransportProbeMissing,
    rollbackPlanMissing,
    productionApplyPortMissing,
    legacyFallbackMissing,
    readSideParallelDivergent,
}

public sealed class CanonicalLibraryMetadataStageObservationWindow : IEquatable<CanonicalLibraryMetadataStageObservationWindow>
{
    public string ObservationWindowID { get; set; }
    public bool Complete { get; set; }

    public CanonicalLibraryMetadataStageObservationWindow(string observationWindowID, bool complete)
    {
        ObservationWindowID = CanonicalProductionRedaction.SafeIdentifier(observationWindowID, "stage-observation")!;
        Complete = complete;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataStageObservationWindow other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataStageObservationWindow? other) =>
        other is not null && ObservationWindowID == other.ObservationWindowID && Complete == other.Complete;
    public override int GetHashCode() => HashCode.Combine(ObservationWindowID, Complete);
    public static bool operator ==(CanonicalLibraryMetadataStageObservationWindow left, CanonicalLibraryMetadataStageObservationWindow right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataStageObservationWindow left, CanonicalLibraryMetadataStageObservationWindow right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataStageEvidenceReport : IEquatable<CanonicalLibraryMetadataStageEvidenceReport>
{
    public CanonicalLibraryMetadataCanaryStage? PreviousStage { get; set; }
    public CanonicalLibraryMetadataCanaryStage RequestedStage { get; set; }
    public int PreviousStageSuccessCount { get; set; }
    public int PreviousStageFailureCount { get; set; }
    public int PreviousStageRollbackFailureCount { get; set; }
    public int PreviousStageBlockingDivergenceCount { get; set; }
    public int PreviousStageResourceMoveAttemptCount { get; set; }
    public int PreviousStageObjectIDInstabilityCount { get; set; }
    public int PreviousStageHierarchyCycleCount { get; set; }
    public int PreviousStageSuppressedLegacyDuplicateCount { get; set; }
    public int UnresolvedConflictCount { get; set; }
    public int PostconditionFailureCount { get; set; }
    public int UnsupportedObjectCount { get; set; }
    public CanonicalLibraryMetadataStageEvidenceProbeStatus DryRunEquivalenceStatus { get; set; }
    public CanonicalLibraryMetadataStageEvidenceProbeStatus ExecutionShadowStatus { get; set; }
    public CanonicalLibraryMetadataStageEvidenceProbeStatus RealDataShadowCopyStatus { get; set; }
    public CanonicalLibraryMetadataStageEvidenceProbeStatus ReadOnlyTransportProbeStatus { get; set; }
    public CanonicalLibraryMetadataStageEvidenceProbeStatus RollbackPlanStatus { get; set; }
    public CanonicalLibraryMetadataStageEvidenceProbeStatus ProductionApplyPortStatus { get; set; }
    public CanonicalLibraryMetadataStageEvidenceProbeStatus LegacyFallbackStatus { get; set; }
    public CanonicalLibraryMetadataStageEvidenceProbeStatus ReadSideParallelStatus { get; set; }
    public CanonicalLibraryMetadataStageObservationWindow ObservationWindow { get; set; }
    public List<CanonicalLibraryMetadataStageEvidenceBlocker> Blockers { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataStageEvidenceReport(
        CanonicalLibraryMetadataCanaryStage? previousStage,
        CanonicalLibraryMetadataCanaryStage requestedStage,
        int previousStageSuccessCount = 0,
        int previousStageFailureCount = 0,
        int previousStageRollbackFailureCount = 0,
        int previousStageBlockingDivergenceCount = 0,
        int previousStageResourceMoveAttemptCount = 0,
        int previousStageObjectIDInstabilityCount = 0,
        int previousStageHierarchyCycleCount = 0,
        int previousStageSuppressedLegacyDuplicateCount = 0,
        int unresolvedConflictCount = 0,
        int postconditionFailureCount = 0,
        int unsupportedObjectCount = 0,
        CanonicalLibraryMetadataStageEvidenceProbeStatus dryRunEquivalenceStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
        CanonicalLibraryMetadataStageEvidenceProbeStatus executionShadowStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
        CanonicalLibraryMetadataStageEvidenceProbeStatus realDataShadowCopyStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
        CanonicalLibraryMetadataStageEvidenceProbeStatus readOnlyTransportProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
        CanonicalLibraryMetadataStageEvidenceProbeStatus rollbackPlanStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
        CanonicalLibraryMetadataStageEvidenceProbeStatus productionApplyPortStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
        CanonicalLibraryMetadataStageEvidenceProbeStatus legacyFallbackStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
        CanonicalLibraryMetadataStageEvidenceProbeStatus readSideParallelStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
        CanonicalLibraryMetadataStageObservationWindow? observationWindow = null,
        List<CanonicalLibraryMetadataStageEvidenceBlocker>? blockers = null,
        bool redacted = true)
    {
        PreviousStage = previousStage;
        RequestedStage = requestedStage;
        PreviousStageSuccessCount = Math.Max(0, previousStageSuccessCount);
        PreviousStageFailureCount = Math.Max(0, previousStageFailureCount);
        PreviousStageRollbackFailureCount = Math.Max(0, previousStageRollbackFailureCount);
        PreviousStageBlockingDivergenceCount = Math.Max(0, previousStageBlockingDivergenceCount);
        PreviousStageResourceMoveAttemptCount = Math.Max(0, previousStageResourceMoveAttemptCount);
        PreviousStageObjectIDInstabilityCount = Math.Max(0, previousStageObjectIDInstabilityCount);
        PreviousStageHierarchyCycleCount = Math.Max(0, previousStageHierarchyCycleCount);
        PreviousStageSuppressedLegacyDuplicateCount = Math.Max(0, previousStageSuppressedLegacyDuplicateCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        PostconditionFailureCount = Math.Max(0, postconditionFailureCount);
        UnsupportedObjectCount = Math.Max(0, unsupportedObjectCount);
        DryRunEquivalenceStatus = dryRunEquivalenceStatus;
        ExecutionShadowStatus = executionShadowStatus;
        RealDataShadowCopyStatus = realDataShadowCopyStatus;
        ReadOnlyTransportProbeStatus = readOnlyTransportProbeStatus;
        RollbackPlanStatus = rollbackPlanStatus;
        ProductionApplyPortStatus = productionApplyPortStatus;
        LegacyFallbackStatus = legacyFallbackStatus;
        ReadSideParallelStatus = readSideParallelStatus;
        ObservationWindow = observationWindow ?? new CanonicalLibraryMetadataStageObservationWindow("stage-observation", false);
        Blockers = new HashSet<CanonicalLibraryMetadataStageEvidenceBlocker>(blockers ?? new List<CanonicalLibraryMetadataStageEvidenceBlocker>())
            .OrderBy(b => b.ToString()).ToList();
        Redacted = redacted;
    }

    public static CanonicalLibraryMetadataStageEvidenceReport From(
        CanonicalLibraryMetadataCutoverEvidence evidence, CanonicalLibraryMetadataCanaryStagePolicy policy)
    {
        var stageEvidence = evidence.CanaryStageEvidence;
        var expectedPreviousStage = policy.RequestedStage.PreviousStage();
        var blockers = new List<CanonicalLibraryMetadataStageEvidenceBlocker>();

        if (stageEvidence == null) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.missingPreviousStageEvidence);
        if (stageEvidence?.ObservationWindowComplete != true) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.observationWindowIncomplete);
        if ((stageEvidence?.FailedCommitCount ?? 0) > 0) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.previousStageFailed);
        if ((stageEvidence?.RollbackFailureCount ?? 0) > 0) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.rollbackFailed);
        if ((stageEvidence?.BlockingDivergenceCount ?? 0) > 0 || !evidence.NoBlockingDivergence) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.blockingDivergence);
        if ((stageEvidence?.UnresolvedConflictCount ?? 0) > 0 || !evidence.NoUnresolvedConflict) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.unresolvedConflict);
        if ((stageEvidence?.PostconditionFailureCount ?? 0) > 0) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.postconditionFailed);
        if ((stageEvidence?.UnsupportedObjectCount ?? 0) > 0) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.unsupportedObject);
        if ((stageEvidence?.ResourceMoveAttemptCount ?? 0) > 0) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.resourceMoveAttempted);
        if ((stageEvidence?.FolderCycleCount ?? 0) > 0) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.hierarchyCycle);
        if ((stageEvidence?.ObjectIDInstabilityCount ?? 0) > 0) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.objectIDInstability);
        if (!evidence.NoCommitEvidenceAvailable) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.missingNoCommitEvidence);
        if (!evidence.DryRunEquivalenceVerified) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.dryRunEquivalenceMissing);
        if (!evidence.ExecutionShadowVerified) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.executionShadowMissing);
        if (!evidence.RealDataShadowCopyVerified) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.realDataShadowCopyMissing);
        if (!evidence.MetadataManifestRouteEvidenceAvailable) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.readOnlyTransportProbeMissing);
        if (evidence.RollbackPlan == null || !evidence.RollbackCheckpointAvailable || !evidence.RollbackVerified || !evidence.RollbackRehearsalPassed)
            blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.rollbackPlanMissing);
        if (!evidence.ProductionPortAvailable || !evidence.RealRootBoundApplyPortAvailable || !evidence.ApplyPortMode.IsNonDryRunRootBound())
            blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.productionApplyPortMissing);
        if (!evidence.LegacyFallbackAvailable) blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.legacyFallbackMissing);
        if ((stageEvidence?.ReadSideParallelDivergenceCount ?? 0) > 0 || !evidence.ReadSideParallelEquivalent)
            blockers.Add(CanonicalLibraryMetadataStageEvidenceBlocker.readSideParallelDivergent);

        return new CanonicalLibraryMetadataStageEvidenceReport(
            previousStage: expectedPreviousStage,
            requestedStage: policy.RequestedStage,
            previousStageSuccessCount: stageEvidence?.SuccessfulCommitCount ?? 0,
            previousStageFailureCount: stageEvidence?.FailedCommitCount ?? 0,
            previousStageRollbackFailureCount: stageEvidence?.RollbackFailureCount ?? 0,
            previousStageBlockingDivergenceCount: stageEvidence?.BlockingDivergenceCount ?? 0,
            previousStageResourceMoveAttemptCount: stageEvidence?.ResourceMoveAttemptCount ?? 0,
            previousStageObjectIDInstabilityCount: stageEvidence?.ObjectIDInstabilityCount ?? 0,
            previousStageHierarchyCycleCount: stageEvidence?.FolderCycleCount ?? 0,
            previousStageSuppressedLegacyDuplicateCount: stageEvidence?.SuppressedLegacyDuplicateCount ?? 0,
            unresolvedConflictCount: stageEvidence?.UnresolvedConflictCount ?? 0,
            postconditionFailureCount: stageEvidence?.PostconditionFailureCount ?? 0,
            unsupportedObjectCount: stageEvidence?.UnsupportedObjectCount ?? 0,
            dryRunEquivalenceStatus: evidence.DryRunEquivalenceVerified ? CanonicalLibraryMetadataStageEvidenceProbeStatus.equivalent : CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
            executionShadowStatus: evidence.ExecutionShadowVerified ? CanonicalLibraryMetadataStageEvidenceProbeStatus.verified : CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
            realDataShadowCopyStatus: evidence.RealDataShadowCopyVerified ? CanonicalLibraryMetadataStageEvidenceProbeStatus.verified : CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
            readOnlyTransportProbeStatus: evidence.MetadataManifestRouteEvidenceAvailable ? CanonicalLibraryMetadataStageEvidenceProbeStatus.verified : CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
            rollbackPlanStatus: blockers.Contains(CanonicalLibraryMetadataStageEvidenceBlocker.rollbackPlanMissing) ? CanonicalLibraryMetadataStageEvidenceProbeStatus.missing : CanonicalLibraryMetadataStageEvidenceProbeStatus.verified,
            productionApplyPortStatus: blockers.Contains(CanonicalLibraryMetadataStageEvidenceBlocker.productionApplyPortMissing) ? CanonicalLibraryMetadataStageEvidenceProbeStatus.missing : CanonicalLibraryMetadataStageEvidenceProbeStatus.verified,
            legacyFallbackStatus: evidence.LegacyFallbackAvailable ? CanonicalLibraryMetadataStageEvidenceProbeStatus.verified : CanonicalLibraryMetadataStageEvidenceProbeStatus.missing,
            readSideParallelStatus: evidence.ReadSideParallelEquivalent ? CanonicalLibraryMetadataStageEvidenceProbeStatus.equivalent : CanonicalLibraryMetadataStageEvidenceProbeStatus.divergent,
            observationWindow: new CanonicalLibraryMetadataStageObservationWindow(
                stageEvidence?.ObservationWindowID ?? $"{policy.RequestedStage}-observation",
                stageEvidence?.ObservationWindowComplete == true),
            blockers: blockers,
            redacted: true);
    }

    public CanonicalLibraryMetadataCanaryStageEvidence CanaryStageEvidence => new(
        stage: PreviousStage ?? CanonicalLibraryMetadataCanaryStage.disabled,
        previousStage: PreviousStage?.PreviousStage(),
        status: Blockers.Count == 0 ? CanonicalLibraryMetadataStageEvidenceStatus.passed : CanonicalLibraryMetadataStageEvidenceStatus.blocked,
        successfulCommitCount: PreviousStageSuccessCount,
        failedCommitCount: PreviousStageFailureCount,
        rollbackFailureCount: PreviousStageRollbackFailureCount,
        blockingDivergenceCount: PreviousStageBlockingDivergenceCount,
        unresolvedConflictCount: UnresolvedConflictCount,
        postconditionFailureCount: PostconditionFailureCount,
        unsupportedObjectCount: UnsupportedObjectCount,
        resourceMoveAttemptCount: PreviousStageResourceMoveAttemptCount,
        folderCycleCount: PreviousStageHierarchyCycleCount,
        objectIDInstabilityCount: PreviousStageObjectIDInstabilityCount,
        suppressedLegacyDuplicateCount: PreviousStageSuppressedLegacyDuplicateCount,
        readSideParallelDivergenceCount: ReadSideParallelStatus == CanonicalLibraryMetadataStageEvidenceProbeStatus.divergent ? 1 : 0,
        noCommitEvidenceAvailable: !Blockers.Contains(CanonicalLibraryMetadataStageEvidenceBlocker.missingNoCommitEvidence),
        observationWindowComplete: ObservationWindow.Complete,
        observationWindowID: ObservationWindow.ObservationWindowID);

    public string DiagnosticsSummary => string.Join(",",
        $"previousStage={PreviousStage?.ToString() ?? "none"}",
        $"requestedStage={RequestedStage}",
        $"successCount={PreviousStageSuccessCount}",
        $"failureCount={PreviousStageFailureCount}",
        $"rollbackFailureCount={PreviousStageRollbackFailureCount}",
        $"resourceMoveCount={PreviousStageResourceMoveAttemptCount}",
        $"objectIDInstabilityCount={PreviousStageObjectIDInstabilityCount}",
        $"hierarchyCycleCount={PreviousStageHierarchyCycleCount}",
        $"readSideParallel={ReadSideParallelStatus}",
        $"observationComplete={ObservationWindow.Complete}",
        $"blockers={string.Join("|", Blockers.Select(b => b.ToString()))}",
        $"redacted={Redacted}");

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataStageEvidenceReport other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataStageEvidenceReport? other) =>
        other is not null && PreviousStage == other.PreviousStage && RequestedStage == other.RequestedStage &&
        PreviousStageSuccessCount == other.PreviousStageSuccessCount &&
        PreviousStageFailureCount == other.PreviousStageFailureCount &&
        PreviousStageRollbackFailureCount == other.PreviousStageRollbackFailureCount &&
        PreviousStageBlockingDivergenceCount == other.PreviousStageBlockingDivergenceCount &&
        PreviousStageResourceMoveAttemptCount == other.PreviousStageResourceMoveAttemptCount &&
        PreviousStageObjectIDInstabilityCount == other.PreviousStageObjectIDInstabilityCount &&
        PreviousStageHierarchyCycleCount == other.PreviousStageHierarchyCycleCount &&
        PreviousStageSuppressedLegacyDuplicateCount == other.PreviousStageSuppressedLegacyDuplicateCount &&
        UnresolvedConflictCount == other.UnresolvedConflictCount &&
        PostconditionFailureCount == other.PostconditionFailureCount &&
        UnsupportedObjectCount == other.UnsupportedObjectCount &&
        DryRunEquivalenceStatus == other.DryRunEquivalenceStatus &&
        ExecutionShadowStatus == other.ExecutionShadowStatus &&
        RealDataShadowCopyStatus == other.RealDataShadowCopyStatus &&
        ReadOnlyTransportProbeStatus == other.ReadOnlyTransportProbeStatus &&
        RollbackPlanStatus == other.RollbackPlanStatus &&
        ProductionApplyPortStatus == other.ProductionApplyPortStatus &&
        LegacyFallbackStatus == other.LegacyFallbackStatus &&
        ReadSideParallelStatus == other.ReadSideParallelStatus &&
        EqualityComparer<CanonicalLibraryMetadataStageObservationWindow>.Default.Equals(ObservationWindow, other.ObservationWindow) &&
        Blockers.SequenceEqual(other.Blockers) && Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(PreviousStage, RequestedStage, PreviousStageSuccessCount, PreviousStageFailureCount,
            PreviousStageRollbackFailureCount, PreviousStageBlockingDivergenceCount, PreviousStageResourceMoveAttemptCount,
            PreviousStageObjectIDInstabilityCount, PreviousStageHierarchyCycleCount, PreviousStageSuppressedLegacyDuplicateCount,
            UnresolvedConflictCount, PostconditionFailureCount, UnsupportedObjectCount,
            DryRunEquivalenceStatus, ExecutionShadowStatus, RealDataShadowCopyStatus, ReadOnlyTransportProbeStatus,
            RollbackPlanStatus, ProductionApplyPortStatus, LegacyFallbackStatus, ReadSideParallelStatus,
            ObservationWindow, Blockers.Count, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataStageEvidenceReport left, CanonicalLibraryMetadataStageEvidenceReport right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataStageEvidenceReport left, CanonicalLibraryMetadataStageEvidenceReport right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataCanaryStageGate : IEquatable<CanonicalLibraryMetadataCanaryStageGate>
{
    public bool Allowed { get; set; }
    public int SelectedCandidateLimit { get; set; }
    public List<CanonicalLibraryMetadataCutoverFailure> Failures { get; set; }

    public CanonicalLibraryMetadataCanaryStageGate(
        CanonicalLibraryMetadataCanaryStagePolicy policy,
        CanonicalLibraryMetadataCutoverEvidence evidence)
    {
        var failures = new List<CanonicalLibraryMetadataCutoverFailure>();
        if (!policy.RequestedStage.IsExecutable())
            failures.Add(CanonicalLibraryMetadataCutoverFailure.disabled);
        if (policy.RuntimeSwitchEnabled)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.runtimeSwitchDenied);
        if (!policy.AllowCandidateExecution)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.missingInternalCanaryConfiguration);

        var stageEvidence = evidence.CanaryStageEvidence;
        if (stageEvidence == null)
        {
            failures.Add(CanonicalLibraryMetadataCutoverFailure.missingCanaryStageEvidence);
            Failures = new HashSet<CanonicalLibraryMetadataCutoverFailure>(failures).OrderBy(f => f.ToString()).ToList();
            Allowed = false;
            SelectedCandidateLimit = 0;
            return;
        }

        if (stageEvidence.Status != CanonicalLibraryMetadataStageEvidenceStatus.passed)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.canaryStageBlocked);
        if (stageEvidence.Stage != policy.RequestedStage.PreviousStage())
            failures.Add(CanonicalLibraryMetadataCutoverFailure.canaryStageOrderViolation);
        if (!stageEvidence.ObservationWindowComplete)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.observationWindowIncomplete);
        if (stageEvidence.SuccessfulCommitCount < policy.RequestedStage.MinimumPreviousStageSuccessCount())
            failures.Add(CanonicalLibraryMetadataCutoverFailure.canaryStageOrderViolation);
        if (stageEvidence.FailedCommitCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.previousStageFailure);
        if (stageEvidence.RollbackFailureCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.previousStageRollbackFailure);
        if (stageEvidence.BlockingDivergenceCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.previousStageBlockingDivergence);
        if (stageEvidence.UnresolvedConflictCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.previousStageUnresolvedConflict);
        if (stageEvidence.PostconditionFailureCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.previousStagePostconditionFailure);
        if (stageEvidence.UnsupportedObjectCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.previousStageUnsupportedObject);
        if (stageEvidence.ResourceMoveAttemptCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.resourceMoveAttempted);
        if (stageEvidence.FolderCycleCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.cycleDetected);
        if (stageEvidence.ObjectIDInstabilityCount > 0)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.objectIDInstability);
        if (!stageEvidence.NoCommitEvidenceAvailable || !evidence.NoCommitEvidenceAvailable)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.missingNoCommitEvidence);
        if (stageEvidence.ReadSideParallelDivergenceCount > 0 || !evidence.ReadSideParallelEquivalent)
            failures.Add(CanonicalLibraryMetadataCutoverFailure.readSideParallelDivergence);

        Failures = new HashSet<CanonicalLibraryMetadataCutoverFailure>(failures).OrderBy(f => f.ToString()).ToList();
        Allowed = Failures.Count == 0;
        SelectedCandidateLimit = Allowed ? policy.RequestedStage.NominalCanaryBudget() : 0;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryStageGate other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryStageGate? other) =>
        other is not null && Allowed == other.Allowed && SelectedCandidateLimit == other.SelectedCandidateLimit &&
        Failures.SequenceEqual(other.Failures);
    public override int GetHashCode() => HashCode.Combine(Allowed, SelectedCandidateLimit, Failures.Count);
    public static bool operator ==(CanonicalLibraryMetadataCanaryStageGate left, CanonicalLibraryMetadataCanaryStageGate right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryStageGate left, CanonicalLibraryMetadataCanaryStageGate right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataCanaryPolicy : IEquatable<CanonicalLibraryMetadataCanaryPolicy>
{
    public CanonicalLibraryMetadataCanaryStagePolicy StagePolicy { get; set; }
    public int CanaryMaxObjectsPerSyncRun { get; set; }
    public bool AllowsInternalN1Execution { get; set; }
    public bool ExplicitInternalTestConfiguration { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool AllowAllEligible { get; set; }

    public CanonicalLibraryMetadataCanaryPolicy(
        CanonicalLibraryMetadataCanaryStagePolicy? stagePolicy = null,
        int canaryMaxObjectsPerSyncRun = 0,
        bool allowsInternalN1Execution = false,
        bool explicitInternalTestConfiguration = false,
        bool runtimeSwitchEnabled = false,
        bool allowAllEligible = false)
    {
        StagePolicy = stagePolicy ?? CanonicalLibraryMetadataCanaryStagePolicy.Disabled;
        CanaryMaxObjectsPerSyncRun = Math.Max(0, canaryMaxObjectsPerSyncRun);
        AllowsInternalN1Execution = allowsInternalN1Execution;
        ExplicitInternalTestConfiguration = explicitInternalTestConfiguration;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        AllowAllEligible = allowAllEligible;
    }

    public static readonly CanonicalLibraryMetadataCanaryPolicy Disabled = new();

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryPolicy other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryPolicy? other) =>
        other is not null &&
        EqualityComparer<CanonicalLibraryMetadataCanaryStagePolicy>.Default.Equals(StagePolicy, other.StagePolicy) &&
        CanaryMaxObjectsPerSyncRun == other.CanaryMaxObjectsPerSyncRun &&
        AllowsInternalN1Execution == other.AllowsInternalN1Execution &&
        ExplicitInternalTestConfiguration == other.ExplicitInternalTestConfiguration &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        AllowAllEligible == other.AllowAllEligible;
    public override int GetHashCode() =>
        HashCode.Combine(StagePolicy, CanaryMaxObjectsPerSyncRun, AllowsInternalN1Execution,
            ExplicitInternalTestConfiguration, RuntimeSwitchEnabled, AllowAllEligible);
    public static bool operator ==(CanonicalLibraryMetadataCanaryPolicy left, CanonicalLibraryMetadataCanaryPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryPolicy left, CanonicalLibraryMetadataCanaryPolicy right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCanaryMode
{
    disabled,
    n1
}

public static class CanonicalLibraryMetadataCanaryModeExtensions
{
    public static bool IsExecutable(this CanonicalLibraryMetadataCanaryMode mode) => mode == CanonicalLibraryMetadataCanaryMode.n1;
}

public sealed class CanonicalLibraryMetadataCanaryConfiguration : IEquatable<CanonicalLibraryMetadataCanaryConfiguration>
{
    public CanonicalLibraryMetadataCanaryMode Mode { get; set; }
    public CanonicalMigrationDomain Domain { get; set; }
    public int CanaryMaxObjectsPerSyncRun { get; set; }
    public bool ExplicitInternalTestConfiguration { get; set; }
    public bool ProductionTokenRequired { get; set; }
    public bool OwnerApprovalRequired { get; set; }
    public bool RollbackPlanRequired { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool AllowAllEligible { get; set; }
    public bool ReleaseDefaultEnabled { get; set; }

    public CanonicalLibraryMetadataCanaryConfiguration(
        CanonicalLibraryMetadataCanaryMode mode = CanonicalLibraryMetadataCanaryMode.disabled,
        CanonicalMigrationDomain domain = CanonicalMigrationDomain.libraryMetadata,
        int canaryMaxObjectsPerSyncRun = 0,
        bool explicitInternalTestConfiguration = false,
        bool productionTokenRequired = true,
        bool ownerApprovalRequired = true,
        bool rollbackPlanRequired = true,
        bool runtimeSwitchEnabled = false,
        bool allowAllEligible = false,
        bool releaseDefaultEnabled = false)
    {
        Mode = mode;
        Domain = domain;
        CanaryMaxObjectsPerSyncRun = Math.Max(0, canaryMaxObjectsPerSyncRun);
        ExplicitInternalTestConfiguration = explicitInternalTestConfiguration;
        ProductionTokenRequired = productionTokenRequired;
        OwnerApprovalRequired = ownerApprovalRequired;
        RollbackPlanRequired = rollbackPlanRequired;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        AllowAllEligible = allowAllEligible;
        ReleaseDefaultEnabled = releaseDefaultEnabled;
    }

    public static readonly CanonicalLibraryMetadataCanaryConfiguration Disabled = new();

    public static CanonicalLibraryMetadataCanaryConfiguration InternalN1(bool explicitInternalTestConfiguration = true) =>
        new(CanonicalLibraryMetadataCanaryMode.n1, canaryMaxObjectsPerSyncRun: 1, explicitInternalTestConfiguration: explicitInternalTestConfiguration);

    public bool StrictN1Enabled =>
        Mode == CanonicalLibraryMetadataCanaryMode.n1 &&
        Domain == CanonicalMigrationDomain.libraryMetadata &&
        CanaryMaxObjectsPerSyncRun == 1 &&
        ExplicitInternalTestConfiguration &&
        !RuntimeSwitchEnabled &&
        !AllowAllEligible &&
        !ReleaseDefaultEnabled;

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryConfiguration other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryConfiguration? other) =>
        other is not null && Mode == other.Mode && Domain == other.Domain &&
        CanaryMaxObjectsPerSyncRun == other.CanaryMaxObjectsPerSyncRun &&
        ExplicitInternalTestConfiguration == other.ExplicitInternalTestConfiguration &&
        ProductionTokenRequired == other.ProductionTokenRequired &&
        OwnerApprovalRequired == other.OwnerApprovalRequired &&
        RollbackPlanRequired == other.RollbackPlanRequired &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        AllowAllEligible == other.AllowAllEligible &&
        ReleaseDefaultEnabled == other.ReleaseDefaultEnabled;
    public override int GetHashCode() =>
        HashCode.Combine(Mode, Domain, CanaryMaxObjectsPerSyncRun, ExplicitInternalTestConfiguration,
            ProductionTokenRequired, OwnerApprovalRequired, RollbackPlanRequired, RuntimeSwitchEnabled,
            AllowAllEligible, ReleaseDefaultEnabled);
    public static bool operator ==(CanonicalLibraryMetadataCanaryConfiguration left, CanonicalLibraryMetadataCanaryConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryConfiguration left, CanonicalLibraryMetadataCanaryConfiguration right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCanaryCandidateSafetyKind
{
    folderRenameOrColorMetadata,
    studyItemTagsFilingOrFolderMembershipMetadata,
    standaloneNoteTitleTagsOrFilingMetadata,
    blocked,
}

public sealed class CanonicalLibraryMetadataCanaryCandidateSafety : IEquatable<CanonicalLibraryMetadataCanaryCandidateSafety>
{
    public CanonicalLibraryMetadataCanaryCandidate Candidate { get; set; }
    public bool Safe { get; set; }
    public CanonicalLibraryMetadataCanaryCandidateSafetyKind Kind { get; set; }
    public List<CanonicalLibraryMetadataCanaryBlocker> Blockers { get; set; }
    public bool MetadataOnly { get; set; }
    public bool ResourceMoveAttempted { get; set; }
    public bool PhysicalDeleteAttempted { get; set; }
    public bool ContentBytesMutated { get; set; }

    public CanonicalLibraryMetadataCanaryCandidateSafety(
        CanonicalLibraryMetadataCutoverCandidate candidate,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        HashSet<string>? attemptedFailedActionIDs = null)
    {
        attemptedFailedActionIDs ??= new HashSet<string>();
        var blockers = CanonicalLibraryMetadataCanarySelector.CandidateBlockers(candidate, evidence, attemptedFailedActionIDs);

        var metadataKind = candidate.ObjectKind switch
        {
            CanonicalObjectKind.folder => CanonicalLibraryMetadataCanaryCandidateSafetyKind.folderRenameOrColorMetadata,
            CanonicalObjectKind.standaloneNote => CanonicalLibraryMetadataCanaryCandidateSafetyKind.standaloneNoteTitleTagsOrFilingMetadata,
            CanonicalObjectKind.standaloneStudyItem or CanonicalObjectKind.recordingAssociatedStudyItem =>
                CanonicalLibraryMetadataCanaryCandidateSafetyKind.studyItemTagsFilingOrFolderMembershipMetadata,
            _ => CanonicalLibraryMetadataCanaryCandidateSafetyKind.blocked
        };

        if (candidate.FolderHierarchyMutationAttempted && candidate.ObjectKind == CanonicalObjectKind.folder)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.folderHierarchyMutationUnsupported);

        if (candidate.ExpectedObject?.IsDeleted == true)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.activeVsTombstoneConflict);

        Candidate = new CanonicalLibraryMetadataCanaryCandidate(candidate);
        Blockers = new HashSet<CanonicalLibraryMetadataCanaryBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        Safe = Blockers.Count == 0;
        Kind = Safe ? metadataKind : CanonicalLibraryMetadataCanaryCandidateSafetyKind.blocked;
        MetadataOnly = true;
        ResourceMoveAttempted = candidate.HasResourceMoveAttempt;
        PhysicalDeleteAttempted = candidate.ExpectedObject?.IsDeleted == true;
        ContentBytesMutated = false;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryCandidateSafety other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryCandidateSafety? other) =>
        other is not null && EqualityComparer<CanonicalLibraryMetadataCanaryCandidate>.Default.Equals(Candidate, other.Candidate) &&
        Safe == other.Safe && Kind == other.Kind && Blockers.SequenceEqual(other.Blockers) &&
        MetadataOnly == other.MetadataOnly && ResourceMoveAttempted == other.ResourceMoveAttempted &&
        PhysicalDeleteAttempted == other.PhysicalDeleteAttempted && ContentBytesMutated == other.ContentBytesMutated;
    public override int GetHashCode() =>
        HashCode.Combine(Candidate, Safe, Kind, Blockers.Count, MetadataOnly, ResourceMoveAttempted, PhysicalDeleteAttempted, ContentBytesMutated);
    public static bool operator ==(CanonicalLibraryMetadataCanaryCandidateSafety left, CanonicalLibraryMetadataCanaryCandidateSafety right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryCandidateSafety left, CanonicalLibraryMetadataCanaryCandidateSafety right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCanaryBlocker
{
    disabled,
    unsupportedMode,
    canaryBudgetZero,
    missingInternalCanaryConfiguration,
    canaryBudgetAboveOneDenied,
    canaryStageEvidenceMissing,
    canaryStageBlocked,
    unsupportedTrigger,
    unsupportedAction,
    insufficientEvidence,
    missingOwnerApproval,
    matrixBlocked,
    activePilotNotLibraryMetadata,
    commitExecutorUnavailable,
    peerSnapshotUnavailable,
    runtimeSwitchDenied,
    allEligibleDenied,
    defaultEnablementDenied,
    readSideParallelMissing,
    unresolvedConflict,
    noRollbackCheckpoint,
    realApplyPortUnavailable,
    conflictDetected,
    activeVsTombstoneConflict,
    resourceMoveAttempted,
    folderHierarchyMutationUnsupported,
    parentMissing,
    cycleDetected,
    objectIDInstability,
    alreadyAttemptedFailedCandidate,
    noEligibleCandidate,
}

public sealed class CanonicalLibraryMetadataCanaryCandidate : IEquatable<CanonicalLibraryMetadataCanaryCandidate>
{
    public string Id => CutoverCandidate.Action.ActionID;
    public CanonicalLibraryMetadataCutoverCandidate CutoverCandidate { get; set; }
    public string ObjectID { get; set; }
    public CanonicalObjectKind ObjectKind { get; set; }
    public CanonicalLibraryMetadataCutoverActionKind ActionKind { get; set; }
    public CanonicalLibraryMetadataCutoverDomain Domain { get; set; }
    public string? MetadataHashPrefix { get; set; }

    public CanonicalLibraryMetadataCanaryCandidate(CanonicalLibraryMetadataCutoverCandidate cutoverCandidate)
    {
        CutoverCandidate = cutoverCandidate;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(cutoverCandidate.ObjectID, "library-object")!;
        ObjectKind = cutoverCandidate.ObjectKind;
        ActionKind = cutoverCandidate.CutoverActionKind;
        Domain = cutoverCandidate.Domain;
        MetadataHashPrefix = cutoverCandidate.ExpectedMetadataHash is { } h
            ? CanonicalProductionRedaction.HashPrefix(h.Value) : null;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryCandidate other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryCandidate? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalLibraryMetadataCanaryCandidate left, CanonicalLibraryMetadataCanaryCandidate right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryCandidate left, CanonicalLibraryMetadataCanaryCandidate right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataCanarySelectionBlocker : IEquatable<CanonicalLibraryMetadataCanarySelectionBlocker>
{
    public string Id => string.Join("|", ObjectID ?? "run", Reason.ToString());
    public string? ObjectID { get; set; }
    public CanonicalLibraryMetadataCanaryBlocker Reason { get; set; }

    public CanonicalLibraryMetadataCanarySelectionBlocker(string? objectID, CanonicalLibraryMetadataCanaryBlocker reason)
    {
        ObjectID = objectID != null
            ? CanonicalProductionRedaction.SafeIdentifier(objectID, "library-object") : null;
        Reason = reason;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanarySelectionBlocker other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanarySelectionBlocker? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalLibraryMetadataCanarySelectionBlocker left, CanonicalLibraryMetadataCanarySelectionBlocker right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanarySelectionBlocker left, CanonicalLibraryMetadataCanarySelectionBlocker right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataCanarySelectionResult : IEquatable<CanonicalLibraryMetadataCanarySelectionResult>
{
    public List<CanonicalLibraryMetadataCanaryCandidate> SelectedCandidates { get; set; }
    public List<CanonicalLibraryMetadataCanarySelectionBlocker> Blockers { get; set; }
    public int EvaluatedCandidateCount { get; set; }
    public bool NoEligibleCandidate { get; set; }

    public List<CanonicalLibraryMetadataCutoverCandidate> SelectedCutoverCandidates =>
        SelectedCandidates.Select(c => c.CutoverCandidate).ToList();

    public CanonicalLibraryMetadataCanarySelectionResult(
        List<CanonicalLibraryMetadataCanaryCandidate>? selectedCandidates = null,
        List<CanonicalLibraryMetadataCanarySelectionBlocker>? blockers = null,
        int evaluatedCandidateCount = 0,
        bool noEligibleCandidate = false)
    {
        SelectedCandidates = selectedCandidates ?? new List<CanonicalLibraryMetadataCanaryCandidate>();
        Blockers = blockers ?? new List<CanonicalLibraryMetadataCanarySelectionBlocker>();
        EvaluatedCandidateCount = evaluatedCandidateCount;
        NoEligibleCandidate = noEligibleCandidate;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanarySelectionResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanarySelectionResult? other) =>
        other is not null && SelectedCandidates.SequenceEqual(other.SelectedCandidates) &&
        Blockers.SequenceEqual(other.Blockers) &&
        EvaluatedCandidateCount == other.EvaluatedCandidateCount &&
        NoEligibleCandidate == other.NoEligibleCandidate;
    public override int GetHashCode() =>
        HashCode.Combine(SelectedCandidates.Count, Blockers.Count, EvaluatedCandidateCount, NoEligibleCandidate);
    public static bool operator ==(CanonicalLibraryMetadataCanarySelectionResult left, CanonicalLibraryMetadataCanarySelectionResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanarySelectionResult left, CanonicalLibraryMetadataCanarySelectionResult right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataCanarySelector
{
    public CanonicalLibraryMetadataCanarySelector() { }

    public CanonicalLibraryMetadataCanarySelectionResult Select(
        CanonicalCutoverMode mode,
        CanonicalLibraryMetadataCanaryPolicy policy,
        CanonicalSyncPlanTrigger trigger,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        List<CanonicalLibraryMetadataCutoverCandidate> candidates,
        HashSet<string>? attemptedFailedActionIDs = null)
    {
        attemptedFailedActionIDs ??= new HashSet<string>();
        var blockers = new List<CanonicalLibraryMetadataCanarySelectionBlocker>();
        var usesStagePolicy = policy.StagePolicy.RequestedStage.IsExecutable();
        CanonicalLibraryMetadataCanaryStageGate? stageGate = usesStagePolicy
            ? new CanonicalLibraryMetadataCanaryStageGate(policy.StagePolicy, evidence) : null;

        if (mode == CanonicalCutoverMode.disabled)
            blockers.Add(new(null, CanonicalLibraryMetadataCanaryBlocker.disabled));
        if (mode != CanonicalCutoverMode.canary)
            blockers.Add(new(null, CanonicalLibraryMetadataCanaryBlocker.unsupportedMode));
        if (policy.CanaryMaxObjectsPerSyncRun == 0 && !usesStagePolicy)
            blockers.Add(new(null, CanonicalLibraryMetadataCanaryBlocker.canaryBudgetZero));
        if (policy.CanaryMaxObjectsPerSyncRun > 1 && !usesStagePolicy)
            blockers.Add(new(null, CanonicalLibraryMetadataCanaryBlocker.canaryBudgetAboveOneDenied));
        if (policy.CanaryMaxObjectsPerSyncRun == 1 && !usesStagePolicy && !policy.AllowsInternalN1Execution)
            blockers.Add(new(null, CanonicalLibraryMetadataCanaryBlocker.missingInternalCanaryConfiguration));
        if (usesStagePolicy && stageGate?.Allowed != true)
            blockers.Add(new(null,
                evidence.CanaryStageEvidence == null
                    ? CanonicalLibraryMetadataCanaryBlocker.canaryStageEvidenceMissing
                    : CanonicalLibraryMetadataCanaryBlocker.canaryStageBlocked));
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh || trigger == CanonicalSyncPlanTrigger.retryDrainer)
            blockers.Add(new(null, CanonicalLibraryMetadataCanaryBlocker.unsupportedTrigger));

        var runBlocked = blockers.Count > 0;
        var selectionLimit = usesStagePolicy ? (stageGate?.SelectedCandidateLimit ?? 0) : policy.CanaryMaxObjectsPerSyncRun;
        var cycleDetected = CanonicalLibraryMetadataCutoverCandidate.FolderHierarchyCycleDetected(candidates);

        var ordered = candidates
            .OrderBy(c => c.ObjectKind.ToString(), StringComparer.Ordinal)
            .ThenBy(c => c.ObjectID, StringComparer.Ordinal)
            .ThenBy(c => c.Action.ActionID, StringComparer.Ordinal)
            .ToList();

        var selected = new List<CanonicalLibraryMetadataCanaryCandidate>();
        foreach (var candidate in ordered)
        {
            var reasons = CandidateBlockers(candidate, evidence, attemptedFailedActionIDs);
            if (cycleDetected && candidate.ObjectKind == CanonicalObjectKind.folder)
                reasons.Add(CanonicalLibraryMetadataCanaryBlocker.cycleDetected);

            if (reasons.Count == 0 && !runBlocked && selected.Count < selectionLimit)
            {
                selected.Add(new CanonicalLibraryMetadataCanaryCandidate(candidate));
            }
            else
            {
                blockers.AddRange(reasons.Select(r => new CanonicalLibraryMetadataCanarySelectionBlocker(candidate.ObjectID, r)));
            }
        }

        if (selected.Count == 0)
            blockers.Add(new(null, CanonicalLibraryMetadataCanaryBlocker.noEligibleCandidate));

        return new CanonicalLibraryMetadataCanarySelectionResult(
            selectedCandidates: selected,
            blockers: blockers,
            evaluatedCandidateCount: candidates.Count,
            noEligibleCandidate: selected.Count == 0);
    }

    public static List<CanonicalLibraryMetadataCanaryBlocker> CandidateBlockers(
        CanonicalLibraryMetadataCutoverCandidate candidate,
        CanonicalLibraryMetadataCutoverEvidence evidence,
        HashSet<string> attemptedFailedActionIDs)
    {
        var blockers = new List<CanonicalLibraryMetadataCanaryBlocker>();

        if (!candidate.CutoverActionKind.IsExecutableMetadata())
            blockers.Add(candidate.CutoverActionKind == CanonicalLibraryMetadataCutoverActionKind.conflictRecord
                ? CanonicalLibraryMetadataCanaryBlocker.conflictDetected
                : CanonicalLibraryMetadataCanaryBlocker.unsupportedAction);

        if (candidate.UnresolvedConflict)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.unresolvedConflict);
        if (candidate.HasActiveVsTombstoneConflict)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.activeVsTombstoneConflict);
        if (candidate.RollbackCheckpointID == null)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.noRollbackCheckpoint);
        if (!evidence.RealRootBoundApplyPortAvailable || !evidence.ApplyPortMode.IsNonDryRunRootBound())
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.realApplyPortUnavailable);
        if (candidate.HasResourceMoveAttempt)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.resourceMoveAttempted);
        if (candidate.FolderHierarchyMutationAttempted)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.folderHierarchyMutationUnsupported);
        if (candidate.ParentMissingKnown)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.parentMissing);
        if (candidate.ExpectedObject?.IsDeleted == true)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.activeVsTombstoneConflict);
        if (candidate.HasObjectIDInstability)
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.objectIDInstability);
        if (attemptedFailedActionIDs.Contains(candidate.Action.ActionID))
            blockers.Add(CanonicalLibraryMetadataCanaryBlocker.alreadyAttemptedFailedCandidate);

        return new HashSet<CanonicalLibraryMetadataCanaryBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
    }
}

public sealed class CanonicalLibraryMetadataCutoverGate : IEquatable<CanonicalLibraryMetadataCutoverGate>
{
    public CanonicalCutoverMode Mode { get; set; }
    public bool Allowed { get; set; }
    public List<CanonicalLibraryMetadataCutoverFailure> Failures { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public string Reason { get; set; }

    public CanonicalLibraryMetadataCutoverGate(
        CanonicalCutoverMode mode,
        List<CanonicalLibraryMetadataCutoverFailure> failures,
        bool legacyFallbackAvailable,
        string reason)
    {
        Mode = mode;
        Failures = new HashSet<CanonicalLibraryMetadataCutoverFailure>(failures).OrderBy(f => f.ToString()).ToList();
        Allowed = Failures.Count == 0;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (Allowed ? "allowed" : "blocked") ?? "unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCutoverGate other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCutoverGate? other) =>
        other is not null && Mode == other.Mode && Allowed == other.Allowed &&
        Failures.SequenceEqual(other.Failures) && LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(Mode, Allowed, Failures.Count, LegacyFallbackAvailable, Reason);
    public static bool operator ==(CanonicalLibraryMetadataCutoverGate left, CanonicalLibraryMetadataCutoverGate right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCutoverGate left, CanonicalLibraryMetadataCutoverGate right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCommitFailureInjection
{
    none,
    preconditionMismatch,
    postconditionMismatch,
    applyFailureBeforeCommit,
    applyFailureAfterPartialCommit,
    rollbackFailure,
    parentMissing,
    cycleDetected,
    resourceMoveAttempted,
    unsupportedObjectKind,
    conflictDetected,
}

public sealed class CanonicalLibraryMetadataProductionCommitResult : IEquatable<CanonicalLibraryMetadataProductionCommitResult>
{
    public string ActionID { get; set; }
    public string ObjectID { get; set; }
    public CanonicalObjectKind ObjectKind { get; set; }
    public CanonicalLibraryMetadataCutoverDomain Domain { get; set; }
    public CanonicalLibraryMetadataCutoverActionKind ActionKind { get; set; }
    public bool Committed { get; set; }
    public bool PartialCommit { get; set; }
    public bool PreconditionVerified { get; set; }
    public bool PostconditionVerified { get; set; }
    public string? RoutePath { get; set; }
    public string? MetadataHashPrefix { get; set; }
    public string ParentSummary { get; set; }
    public int TagCount { get; set; }
    public string FilingSummary { get; set; }
    public int PayloadByteCount { get; set; }
    public CanonicalProductionSideEffect? SideEffect { get; set; }
    public List<CanonicalProductionSideEffect> SideEffects { get; set; }
    public CanonicalLibraryMetadataCutoverFailure? FailureKind { get; set; }
    public string Reason { get; set; }

    public CanonicalLibraryMetadataProductionCommitResult(
        string actionID,
        string objectID,
        CanonicalObjectKind objectKind,
        CanonicalLibraryMetadataCutoverDomain domain,
        CanonicalLibraryMetadataCutoverActionKind actionKind,
        bool committed,
        bool partialCommit = false,
        bool preconditionVerified = true,
        bool postconditionVerified = true,
        string? routePath = "/sync/apply-metadata",
        CanonicalHash? metadataHash = null,
        string? metadataHashPrefix = null,
        string parentSummary = "parent=none",
        int tagCount = 0,
        string filingSummary = "none",
        int payloadByteCount = 0,
        CanonicalProductionSideEffect? sideEffect = null,
        List<CanonicalProductionSideEffect>? sideEffects = null,
        CanonicalLibraryMetadataCutoverFailure? failureKind = null,
        string reason = "")
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, actionKind.ToString())!;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "library-object")!;
        ObjectKind = objectKind;
        Domain = domain;
        ActionKind = actionKind;
        Committed = committed;
        PartialCommit = partialCommit;
        PreconditionVerified = preconditionVerified;
        PostconditionVerified = postconditionVerified;
        RoutePath = routePath != null ? CanonicalProductionRedaction.SafeDiagnosticText(routePath) : null;
        MetadataHashPrefix = metadataHash is { } h
            ? CanonicalProductionRedaction.HashPrefix(h.Value)
            : CanonicalProductionRedaction.HashPrefix(metadataHashPrefix);
        ParentSummary = CanonicalProductionRedaction.SafeDiagnosticText(parentSummary) ?? "parent=none";
        TagCount = Math.Max(0, tagCount);
        FilingSummary = CanonicalProductionRedaction.SafeDiagnosticText(filingSummary) ?? "none";
        PayloadByteCount = Math.Max(0, payloadByteCount);
        SideEffect = sideEffect;
        SideEffects = sideEffects ?? (sideEffect != null ? new List<CanonicalProductionSideEffect> { sideEffect } : new List<CanonicalProductionSideEffect>());
        FailureKind = failureKind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (committed ? "committed" : "failed") ?? "unknown";
    }

    public static CanonicalLibraryMetadataProductionCommitResult Success(
        CanonicalLibraryMetadataCutoverCandidate candidate,
        int payloadByteCount,
        List<CanonicalProductionSideEffect> sideEffects) =>
        new(
            actionID: candidate.Action.ActionID,
            objectID: candidate.ObjectID,
            objectKind: candidate.ObjectKind,
            domain: candidate.Domain,
            actionKind: candidate.CutoverActionKind,
            committed: true,
            metadataHash: candidate.ExpectedMetadataHash,
            parentSummary: candidate.ParentSummary,
            tagCount: candidate.TagCount,
            filingSummary: candidate.FilingSummary,
            payloadByteCount: payloadByteCount,
            sideEffect: sideEffects.FirstOrDefault(),
            sideEffects: sideEffects,
            reason: "libraryMetadataCommitted");

    public static CanonicalLibraryMetadataProductionCommitResult Failure(
        CanonicalLibraryMetadataCutoverCandidate candidate,
        CanonicalLibraryMetadataCutoverFailure failureKind,
        bool partialCommit = false,
        string reason = "")
    {
        var preconditionKinds = new HashSet<CanonicalLibraryMetadataCutoverFailure>
        {
            CanonicalLibraryMetadataCutoverFailure.objectIDMismatch,
            CanonicalLibraryMetadataCutoverFailure.objectKindMismatch,
            CanonicalLibraryMetadataCutoverFailure.expectedMetadataHashMissing,
            CanonicalLibraryMetadataCutoverFailure.parentMissing,
            CanonicalLibraryMetadataCutoverFailure.cycleDetected,
            CanonicalLibraryMetadataCutoverFailure.resourceMoveAttempted,
            CanonicalLibraryMetadataCutoverFailure.folderHierarchyMutationUnsupported,
            CanonicalLibraryMetadataCutoverFailure.unsupportedObjectKind,
            CanonicalLibraryMetadataCutoverFailure.conflictDetected,
            CanonicalLibraryMetadataCutoverFailure.activeVsTombstoneConflict,
        };

        return new CanonicalLibraryMetadataProductionCommitResult(
            actionID: candidate.Action.ActionID,
            objectID: candidate.ObjectID,
            objectKind: candidate.ObjectKind,
            domain: candidate.Domain,
            actionKind: candidate.CutoverActionKind,
            committed: false,
            partialCommit: partialCommit,
            preconditionVerified: !preconditionKinds.Contains(failureKind),
            postconditionVerified: failureKind != CanonicalLibraryMetadataCutoverFailure.postconditionMismatch,
            metadataHash: candidate.ExpectedMetadataHash,
            parentSummary: candidate.ParentSummary,
            tagCount: candidate.TagCount,
            filingSummary: candidate.FilingSummary,
            failureKind: failureKind,
            reason: reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataProductionCommitResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataProductionCommitResult? other) =>
        other is not null && ActionID == other.ActionID && ObjectID == other.ObjectID &&
        ObjectKind == other.ObjectKind && Domain == other.Domain && ActionKind == other.ActionKind &&
        Committed == other.Committed && PartialCommit == other.PartialCommit &&
        PreconditionVerified == other.PreconditionVerified && PostconditionVerified == other.PostconditionVerified &&
        RoutePath == other.RoutePath && MetadataHashPrefix == other.MetadataHashPrefix &&
        ParentSummary == other.ParentSummary && TagCount == other.TagCount && FilingSummary == other.FilingSummary &&
        PayloadByteCount == other.PayloadByteCount &&
        EqualityComparer<CanonicalProductionSideEffect?>.Default.Equals(SideEffect, other.SideEffect) &&
        SideEffects.SequenceEqual(other.SideEffects) && FailureKind == other.FailureKind && Reason == other.Reason;
    public override int GetHashCode() =>
        HashCode.Combine(ActionID, ObjectID, ObjectKind, Domain, ActionKind, Committed, PartialCommit,
            PreconditionVerified, PostconditionVerified, RoutePath, MetadataHashPrefix, ParentSummary, TagCount,
            FilingSummary, PayloadByteCount, SideEffect, SideEffects.Count, FailureKind, Reason);
    public static bool operator ==(CanonicalLibraryMetadataProductionCommitResult left, CanonicalLibraryMetadataProductionCommitResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataProductionCommitResult left, CanonicalLibraryMetadataProductionCommitResult right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataRollbackExecutionResult : IEquatable<CanonicalLibraryMetadataRollbackExecutionResult>
{
    public string CheckpointID { get; set; }
    public bool Succeeded { get; set; }
    public bool Fatal { get; set; }
    public string Reason { get; set; }
    public CanonicalRollbackResult? RollbackResult { get; set; }

    public CanonicalLibraryMetadataRollbackExecutionResult(
        string checkpointID, bool succeeded, bool fatal = false, string reason = "", CanonicalRollbackResult? rollbackResult = null)
    {
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "library-metadata-checkpoint")!;
        Succeeded = succeeded;
        Fatal = fatal;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (succeeded ? "rollbackCompleted" : "rollbackFailed") ?? "unknown";
        RollbackResult = rollbackResult;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataRollbackExecutionResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataRollbackExecutionResult? other) =>
        other is not null && CheckpointID == other.CheckpointID && Succeeded == other.Succeeded &&
        Fatal == other.Fatal && Reason == other.Reason &&
        EqualityComparer<CanonicalRollbackResult?>.Default.Equals(RollbackResult, other.RollbackResult);
    public override int GetHashCode() => HashCode.Combine(CheckpointID, Succeeded, Fatal, Reason, RollbackResult);
    public static bool operator ==(CanonicalLibraryMetadataRollbackExecutionResult left, CanonicalLibraryMetadataRollbackExecutionResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataRollbackExecutionResult left, CanonicalLibraryMetadataRollbackExecutionResult right) => !left.Equals(right);
}

public interface ICanonicalLibraryMetadataCutoverExecutor
{
    Task<CanonicalLibraryMetadataProductionCommitResult> CommitLibraryMetadata(CanonicalLibraryMetadataCutoverCandidate candidate);
    Task<CanonicalLibraryMetadataRollbackExecutionResult> RollbackLibraryMetadata(CanonicalLibraryMetadataCutoverCandidate candidate, CanonicalLibraryMetadataCutoverFailure reason);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCutoverDiagnosticKind
{
    canonicalLibraryMetadataCutoverGateEvaluated,
    canonicalLibraryMetadataCutoverGateBlocked,
    canonicalLibraryMetadataNoCommitStarted,
    canonicalLibraryMetadataNoCommitCompleted,
    canonicalLibraryMetadataCommitStarted,
    canonicalLibraryMetadataCommitCompleted,
    canonicalLibraryMetadataCommitFailed,
    canonicalLibraryMetadataRollbackStarted,
    canonicalLibraryMetadataRollbackCompleted,
    canonicalLibraryMetadataRollbackFailed,
    canonicalLibraryMetadataCanaryStarted,
    canonicalLibraryMetadataCanaryCompleted,
    canonicalLibraryMetadataDuplicateLegacySuppressed,
    canonicalLibraryMetadataLegacyFallbackUsed,
    canonicalLibraryMetadataResourceMoveBlocked,
    canonicalLibraryMetadataHierarchyCycleBlocked,
    canonicalLibraryMetadataObjectIDInstabilityBlocked,
    canonicalLibraryMetadataUnsafeCandidateSkipped,
    canonicalLibraryMetadataCycleBlocked,
    canonicalLibraryMetadataConflictBlocked,
    canonicalLibraryMetadataReadSideParallelEquivalent,
    canonicalLibraryMetadataReadSideParallelDivergent,
    canonicalLibraryMetadataUIProjectionParallelReadStarted,
    canonicalLibraryMetadataUIProjectionParallelReadEquivalent,
    canonicalLibraryMetadataUIProjectionParallelReadDivergent,
    canonicalLibraryMetadataN1CanaryConfigured,
    canonicalLibraryMetadataN1CandidateSelectionStarted,
    canonicalLibraryMetadataN1CandidateSelected,
    canonicalLibraryMetadataN1NoEligibleCandidate,
    canonicalLibraryMetadataN1CandidateBlocked,
    canonicalLibraryMetadataN1CanaryStarted,
    canonicalLibraryMetadataN1CommitStarted,
    canonicalLibraryMetadataN1CommitCompleted,
    canonicalLibraryMetadataN1CommitFailed,
    canonicalLibraryMetadataN1PostconditionVerified,
    canonicalLibraryMetadataN1PostconditionFailed,
    canonicalLibraryMetadataN1RollbackStarted,
    canonicalLibraryMetadataN1RollbackCompleted,
    canonicalLibraryMetadataN1RollbackFailed,
    canonicalLibraryMetadataN1LegacyFallbackUsed,
    canonicalLibraryMetadataN1DuplicateLegacySuppressed,
    canonicalLibraryMetadataN1FatalBlocker,
    canonicalLibraryMetadataN1ObservationRecorded,
    canonicalLibraryMetadataN1ReadSideParallelStarted,
    canonicalLibraryMetadataN1ReadSideParallelEquivalent,
    canonicalLibraryMetadataN1ReadSideParallelDivergent,
    canonicalLibraryMetadataN1MacPeerSnapshotUnavailable,
    canonicalLibraryMetadataCanaryStageEvaluated,
    canonicalLibraryMetadataCanaryStageBlocked,
    canonicalLibraryMetadataCanaryStageAllowed,
    canonicalLibraryMetadataCanaryStageStarted,
    canonicalLibraryMetadataCanaryStageCompleted,
    canonicalLibraryMetadataCanaryStageFailed,
    canonicalLibraryMetadataCanaryStageObservationRecorded,
    canonicalLibraryMetadataCanaryStageCandidateSkipped,
    canonicalLibraryMetadataCanaryStageCandidateExecuted,
    canonicalLibraryMetadataCanaryStageStoppedAfterFailure,
    canonicalLibraryMetadataCanaryStageNextStageEligible,
    canonicalLibraryMetadataCanaryStageNextStageBlocked,
    canonicalLibraryMetadataCanaryStageAllEligibleStarted,
    canonicalLibraryMetadataCanaryStageAllEligibleCompleted,
    canonicalLibraryMetadataExpandedReadSideParallelStarted,
    canonicalLibraryMetadataExpandedReadSideParallelEquivalent,
    canonicalLibraryMetadataExpandedReadSideParallelDivergent,
    canonicalLibraryMetadataReadSideParallelStarted,
    canonicalLibraryMetadataReadSideParallelCompleted,
    canonicalLibraryMetadataReadSideParallelFailed,
    canonicalLibraryMetadataReadSideEquivalent,
    canonicalLibraryMetadataReadSideDivergent,
    canonicalLibraryMetadataReadSideUnsupportedObject,
    canonicalLibraryMetadataReadSidePathLeakBlocked,
    canonicalLibraryMetadataReadSideCutoverCandidateEvaluated,
    canonicalLibraryMetadataReadSideCutoverCandidateBlocked,
    canonicalLibraryMetadataReadSideCutoverCandidateReady,
    canonicalLibraryMetadataGuardedCanonicalReadSuppressed,
    canonicalLibraryMetadataLegacyReadFallbackAvailable,
    canonicalLibraryMetadataRetirementCandidateEvaluated,
    canonicalLibraryMetadataRetirementCandidateBlocked,
    canonicalLibraryMetadataRetirementCandidateReady,
    canonicalLibraryMetadataRealCanaryInjectionConfigured,
    canonicalLibraryMetadataRealCanaryBlocked,
    canonicalLibraryMetadataRealCanaryArmed,
    canonicalLibraryMetadataRealCanaryExecutionStarted,
    canonicalLibraryMetadataRealCanaryExecutionCompleted,
    canonicalLibraryMetadataRealCanaryExecutionFailed,
    canonicalLibraryMetadataRealCanaryNoEligibleCandidate,
    canonicalLibraryMetadataRealCanaryUnsafeCandidateSkipped,
    canonicalLibraryMetadataRealCanaryProductionRootWriteStarted,
    canonicalLibraryMetadataRealCanaryProductionRootWriteCompleted,
    canonicalLibraryMetadataRealCanaryProductionRootWriteFailed,
    canonicalLibraryMetadataRealCanaryRollbackStarted,
    canonicalLibraryMetadataRealCanaryRollbackCompleted,
    canonicalLibraryMetadataRealCanaryRollbackFailed,
    canonicalLibraryMetadataRealCanaryLegacyFallbackUsed,
    canonicalLibraryMetadataRealCanaryDuplicateLegacySuppressed,
    canonicalLibraryMetadataRealCanaryReadSideEquivalent,
    canonicalLibraryMetadataRealCanaryReadSideDivergent,
    canonicalLibraryMetadataRealCanaryFatalBlocker,
    canonicalLibraryMetadataProductionRootGateEvaluated,
    canonicalLibraryMetadataProductionRootGateBlocked,
    canonicalLibraryMetadataProductionRootGateAllowed,
    canonicalLibraryMetadataProductionRootN1Started,
    canonicalLibraryMetadataProductionRootN1Completed,
    canonicalLibraryMetadataProductionRootN1Failed,
    canonicalLibraryMetadataProductionRootSafetyProofBuilt,
    canonicalLibraryMetadataProductionRootCheckpointCreated,
    canonicalLibraryMetadataProductionRootAtomicWriteStarted,
    canonicalLibraryMetadataProductionRootAtomicWriteCompleted,
    canonicalLibraryMetadataProductionRootPostconditionVerified,
    canonicalLibraryMetadataProductionRootRollbackStarted,
    canonicalLibraryMetadataProductionRootRollbackCompleted,
    canonicalLibraryMetadataProductionRootRollbackFailed,
    canonicalLibraryMetadataProductionRootLegacyFallbackUsed,
    canonicalLibraryMetadataProductionRootDuplicateSuppressed,
    canonicalLibraryMetadataProductionRootReadSideEquivalent,
    canonicalLibraryMetadataProductionRootReadSideDivergent,
    canonicalLibraryMetadataLandingConfigEvaluated,
    canonicalLibraryMetadataLandingDisabled,
    canonicalLibraryMetadataLandingArmed,
    canonicalLibraryMetadataLandingBlocked,
    canonicalLibraryMetadataLandingN1Started,
    canonicalLibraryMetadataLandingCandidateSelected,
    canonicalLibraryMetadataLandingNoEligibleCandidate,
    canonicalLibraryMetadataLandingCommitStarted,
    canonicalLibraryMetadataLandingCommitCompleted,
    canonicalLibraryMetadataLandingCommitFailed,
    canonicalLibraryMetadataLandingRollbackStarted,
    canonicalLibraryMetadataLandingRollbackCompleted,
    canonicalLibraryMetadataLandingRollbackFailed,
    canonicalLibraryMetadataLandingLegacyFallbackUsed,
    canonicalLibraryMetadataLandingDuplicateSuppressed,
    canonicalLibraryMetadataLandingReadSideEquivalent,
    canonicalLibraryMetadataLandingReadSideDivergent,
    canonicalLibraryMetadataLandingReportBuilt,
    canonicalMigrationLandingFreezeViolation,
    canonicalLibraryMetadataReadSourceEvaluated,
    canonicalLibraryMetadataReadSourceLegacyReturned,
    canonicalLibraryMetadataReadSourceCanonicalCandidateBuilt,
    canonicalLibraryMetadataGuardedCanonicalReadAllowed,
    canonicalLibraryMetadataGuardedCanonicalReadBlocked,
    canonicalLibraryMetadataGuardedCanonicalReadServed,
    canonicalLibraryMetadataGuardedCanonicalReadFallback,
    canonicalLibraryMetadataReadCutoverGateEvaluated,
    canonicalLibraryMetadataReadCutoverGateBlocked,
    canonicalLibraryMetadataReadCutoverGateAllowed,
    canonicalLibraryMetadataReadOutputEquivalent,
    canonicalLibraryMetadataReadOutputDivergent,
    canonicalLibraryMetadataRetirementCandidateUpdated,
    canonicalLibraryMetadataRetirementStillBlocked,
    canonicalLibraryMetadataObservationWindowStarted,
    canonicalLibraryMetadataObservationWriteSideRecorded,
    canonicalLibraryMetadataObservationReadSideRecorded,
    canonicalLibraryMetadataObservationWindowCompleted,
    canonicalLibraryMetadataObservationGateEvaluated,
    canonicalLibraryMetadataObservationGateBlocked,
    canonicalLibraryMetadataObservationGateReady,
    canonicalLibraryMetadataRetirementCandidateGateEvaluated,
    canonicalLibraryMetadataRetirementCandidateGateBlocked,
    canonicalLibraryMetadataRollbackDrillSummarized,
    canonicalLibraryMetadataEndToEndPilotReportGenerated,
}

public sealed class CanonicalLibraryMetadataCutoverDiagnostic : IEquatable<CanonicalLibraryMetadataCutoverDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", Result ?? "", Reason ?? "");
    public CanonicalLibraryMetadataCutoverDiagnosticKind Kind { get; set; }
    public string? SyncRunID { get; set; }
    public CanonicalSyncPlanTrigger Trigger { get; set; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; set; }
    public CanonicalLibraryMetadataCutoverDomain? Domain { get; set; }
    public string? ObjectID { get; set; }
    public CanonicalObjectKind? ObjectKind { get; set; }
    public string? Action { get; set; }
    public string? Result { get; set; }
    public string? Reason { get; set; }
    public string? HashPrefix { get; set; }

    public CanonicalLibraryMetadataCutoverDiagnostic(
        CanonicalLibraryMetadataCutoverDiagnosticKind kind,
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalLibraryMetadataCutoverDomain? domain = null,
        string? objectID = null,
        CanonicalObjectKind? objectKind = null,
        string? action = null,
        string? result = null,
        string? reason = null,
        CanonicalHash? hash = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Domain = domain;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "library-object") : null;
        ObjectKind = objectKind;
        Action = CanonicalProductionRedaction.SafeDiagnosticText(action);
        Result = CanonicalProductionRedaction.SafeDiagnosticText(result);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = hash is { } h ? CanonicalProductionRedaction.HashPrefix(h.Value) : null;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"syncRunID={SyncRunID ?? "none"}",
        $"trigger={Trigger}",
        $"nodeRole={NodeRole}",
        $"domain={Domain?.ToString() ?? "none"}",
        $"objectID={ObjectID ?? "none"}",
        $"objectKind={ObjectKind?.ToString() ?? "none"}",
        $"action={Action ?? "none"}",
        $"result={Result ?? "none"}",
        $"reason={Reason ?? "none"}",
        $"hashPrefix={HashPrefix ?? "none"}");

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCutoverDiagnostic other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCutoverDiagnostic? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalLibraryMetadataCutoverDiagnostic left, CanonicalLibraryMetadataCutoverDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCutoverDiagnostic left, CanonicalLibraryMetadataCutoverDiagnostic right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataReadSideParallelProjectionResult : IEquatable<CanonicalLibraryMetadataReadSideParallelProjectionResult>
{
    public string ObjectID { get; set; }
    public CanonicalObjectKind ObjectKind { get; set; }
    public CanonicalLibraryMetadataCutoverDomain Domain { get; set; }
    public bool Equivalent { get; set; }
    public bool MutatedUI { get; set; }
    public string? CanonicalHashPrefix { get; set; }
    public string? LegacyHashPrefix { get; set; }
    public string ParentSummary { get; set; }
    public int TagCount { get; set; }
    public string FilingSummary { get; set; }
    public bool NoResourceFileMove { get; set; }
    public bool SyncOrUploadTriggered { get; set; }
    public string Reason { get; set; }

    public CanonicalLibraryMetadataReadSideParallelProjectionResult(
        CanonicalLibraryMetadataCutoverCandidate candidate,
        bool equivalent,
        CanonicalHash? legacyHash,
        string reason)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(candidate.ObjectID, "library-object")!;
        ObjectKind = candidate.ObjectKind;
        Domain = candidate.Domain;
        Equivalent = equivalent;
        MutatedUI = false;
        CanonicalHashPrefix = candidate.ExpectedMetadataHash is { } h ? CanonicalProductionRedaction.HashPrefix(h.Value) : null;
        LegacyHashPrefix = legacyHash is { } lh ? CanonicalProductionRedaction.HashPrefix(lh.Value) : null;
        ParentSummary = candidate.ParentSummary;
        TagCount = candidate.TagCount;
        FilingSummary = candidate.FilingSummary;
        NoResourceFileMove = true;
        SyncOrUploadTriggered = false;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (equivalent ? "equivalent" : "divergent") ?? "unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataReadSideParallelProjectionResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataReadSideParallelProjectionResult? other) =>
        other is not null && ObjectID == other.ObjectID && ObjectKind == other.ObjectKind &&
        Domain == other.Domain && Equivalent == other.Equivalent && MutatedUI == other.MutatedUI &&
        CanonicalHashPrefix == other.CanonicalHashPrefix && LegacyHashPrefix == other.LegacyHashPrefix &&
        ParentSummary == other.ParentSummary && TagCount == other.TagCount &&
        FilingSummary == other.FilingSummary && NoResourceFileMove == other.NoResourceFileMove &&
        SyncOrUploadTriggered == other.SyncOrUploadTriggered && Reason == other.Reason;
    public override int GetHashCode() =>
        HashCode.Combine(ObjectID, ObjectKind, Domain, Equivalent, MutatedUI, CanonicalHashPrefix, LegacyHashPrefix,
            ParentSummary, TagCount, FilingSummary, NoResourceFileMove, SyncOrUploadTriggered, Reason);
    public static bool operator ==(CanonicalLibraryMetadataReadSideParallelProjectionResult left, CanonicalLibraryMetadataReadSideParallelProjectionResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataReadSideParallelProjectionResult left, CanonicalLibraryMetadataReadSideParallelProjectionResult right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataCanaryObservationStatus
{
    disabled,
    blocked,
    noEligibleCandidate,
    committed,
    failedRolledBack,
    fatalRollbackFailure,
}

public sealed class CanonicalLibraryMetadataCanaryObservationReport : IEquatable<CanonicalLibraryMetadataCanaryObservationReport>
{
    public CanonicalLibraryMetadataCanaryObservationStatus Status { get; set; }
    public string? SyncRunID { get; set; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; set; }
    public int SelectedCandidateCount { get; set; }
    public int BlockedCandidateCount { get; set; }
    public int AttemptedCommitCount { get; set; }
    public int SuccessfulCommitCount { get; set; }
    public int RollbackCount { get; set; }
    public bool DuplicateSuppressionApplied { get; set; }
    public bool LegacyFallbackPreserved { get; set; }
    public bool ReadSideParallelEquivalent { get; set; }
    public bool UiMutated { get; set; }
    public bool ResourceMoved { get; set; }
    public bool PhysicalDeleteAttempted { get; set; }
    public bool ContentBytesMutated { get; set; }
    public bool FatalBlocker { get; set; }
    public string Reason { get; set; }

    public CanonicalLibraryMetadataCanaryObservationReport(
        CanonicalLibraryMetadataCanaryObservationStatus status,
        string? syncRunID,
        CanonicalProductionExecutionDomainRole nodeRole,
        int selectedCandidateCount,
        int blockedCandidateCount,
        int attemptedCommitCount,
        int successfulCommitCount,
        int rollbackCount,
        bool duplicateSuppressionApplied,
        bool legacyFallbackPreserved,
        bool readSideParallelEquivalent,
        bool uiMutated = false,
        bool resourceMoved = false,
        bool physicalDeleteAttempted = false,
        bool contentBytesMutated = false,
        bool fatalBlocker = false,
        string reason = "")
    {
        Status = status;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        NodeRole = nodeRole;
        SelectedCandidateCount = Math.Max(0, selectedCandidateCount);
        BlockedCandidateCount = Math.Max(0, blockedCandidateCount);
        AttemptedCommitCount = Math.Max(0, attemptedCommitCount);
        SuccessfulCommitCount = Math.Max(0, successfulCommitCount);
        RollbackCount = Math.Max(0, rollbackCount);
        DuplicateSuppressionApplied = duplicateSuppressionApplied;
        LegacyFallbackPreserved = legacyFallbackPreserved;
        ReadSideParallelEquivalent = readSideParallelEquivalent;
        UiMutated = uiMutated;
        ResourceMoved = resourceMoved;
        PhysicalDeleteAttempted = physicalDeleteAttempted;
        ContentBytesMutated = contentBytesMutated;
        FatalBlocker = fatalBlocker;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? Status.ToString();
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataCanaryObservationReport other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataCanaryObservationReport? other) =>
        other is not null && Status == other.Status && SyncRunID == other.SyncRunID &&
        NodeRole == other.NodeRole && SelectedCandidateCount == other.SelectedCandidateCount &&
        BlockedCandidateCount == other.BlockedCandidateCount && AttemptedCommitCount == other.AttemptedCommitCount &&
        SuccessfulCommitCount == other.SuccessfulCommitCount && RollbackCount == other.RollbackCount &&
        DuplicateSuppressionApplied == other.DuplicateSuppressionApplied &&
        LegacyFallbackPreserved == other.LegacyFallbackPreserved &&
        ReadSideParallelEquivalent == other.ReadSideParallelEquivalent &&
        UiMutated == other.UiMutated && ResourceMoved == other.ResourceMoved &&
        PhysicalDeleteAttempted == other.PhysicalDeleteAttempted && ContentBytesMutated == other.ContentBytesMutated &&
        FatalBlocker == other.FatalBlocker && Reason == other.Reason;
    public override int GetHashCode() =>
        HashCode.Combine(Status, SyncRunID, NodeRole, SelectedCandidateCount, BlockedCandidateCount, AttemptedCommitCount,
            SuccessfulCommitCount, RollbackCount, DuplicateSuppressionApplied, LegacyFallbackPreserved,
            ReadSideParallelEquivalent, UiMutated, ResourceMoved, PhysicalDeleteAttempted, ContentBytesMutated,
            FatalBlocker, Reason);
    public static bool operator ==(CanonicalLibraryMetadataCanaryObservationReport left, CanonicalLibraryMetadataCanaryObservationReport right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataCanaryObservationReport left, CanonicalLibraryMetadataCanaryObservationReport right) => !left.Equals(right);
}
