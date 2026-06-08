using System.Globalization;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── Projection Source / Domain / Mode ──────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalReadProjectionSource
{
    legacy,
    canonical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalReadDomain
{
    recordingMetadata,
    libraryMetadata,
    generatedArtifacts,
    tombstoneConflict,
    audioUploadStatus,
    syncEngineStatus
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalReadRuntimeMode
{
    disabled,
    parallelCompare,
    canonicalReadCandidate,
    guardedCanonicalReadWithLegacyFallback,
    blocked
}

public static class CanonicalReadRuntimeModeExtensions
{
    public static bool BuildsCanonicalCandidate(this CanonicalReadRuntimeMode mode)
        => mode switch
        {
            CanonicalReadRuntimeMode.disabled => false,
            CanonicalReadRuntimeMode.blocked => false,
            CanonicalReadRuntimeMode.parallelCompare => true,
            CanonicalReadRuntimeMode.canonicalReadCandidate => true,
            CanonicalReadRuntimeMode.guardedCanonicalReadWithLegacyFallback => true,
            _ => false
        };
}

// ─── Policy / Configuration ─────────────────────────────────────────────────

public sealed class CanonicalReadRuntimePolicy : IEquatable<CanonicalReadRuntimePolicy>
{
    public bool DebugInternalBuild { get; }
    public bool OwnerApproved { get; }
    public bool ManualOwnerApproval { get; }
    public bool ReleaseDefaultBuild { get; }
    public bool LegacyFallbackAvailable { get; }
    public bool DiagnosticsRedacted { get; }
    public bool ApplyRuntimeEvidenceValidForNonAudio { get; }
    public bool UploadRuntimeEvidenceValidForAudioStatus { get; }
    public bool InventorySnapshotAvailable { get; }
    public bool PlanAuthorityEvidenceValid { get; }
    public bool ExistenceTruthEvidenceValid { get; }
    public bool OtherDomainsNotConflicting { get; }
    public bool AllowDivergentGuardedReadForTests { get; }
    public bool ReadMustNotTriggerSyncUpload { get; }
    public bool ReadMustNotMutateStore { get; }
    public int MaxDiagnosticsEvents { get; }

    public CanonicalReadRuntimePolicy(
        bool debugInternalBuild = false,
        bool ownerApproved = false,
        bool manualOwnerApproval = false,
        bool releaseDefaultBuild = true,
        bool legacyFallbackAvailable = true,
        bool diagnosticsRedacted = true,
        bool applyRuntimeEvidenceValidForNonAudio = false,
        bool uploadRuntimeEvidenceValidForAudioStatus = false,
        bool inventorySnapshotAvailable = false,
        bool planAuthorityEvidenceValid = false,
        bool existenceTruthEvidenceValid = false,
        bool otherDomainsNotConflicting = true,
        bool allowDivergentGuardedReadForTests = false,
        bool readMustNotTriggerSyncUpload = true,
        bool readMustNotMutateStore = true,
        int maxDiagnosticsEvents = 64)
    {
        DebugInternalBuild = debugInternalBuild;
        OwnerApproved = ownerApproved;
        ManualOwnerApproval = manualOwnerApproval;
        ReleaseDefaultBuild = releaseDefaultBuild;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        DiagnosticsRedacted = diagnosticsRedacted;
        ApplyRuntimeEvidenceValidForNonAudio = applyRuntimeEvidenceValidForNonAudio;
        UploadRuntimeEvidenceValidForAudioStatus = uploadRuntimeEvidenceValidForAudioStatus;
        InventorySnapshotAvailable = inventorySnapshotAvailable;
        PlanAuthorityEvidenceValid = planAuthorityEvidenceValid;
        ExistenceTruthEvidenceValid = existenceTruthEvidenceValid;
        OtherDomainsNotConflicting = otherDomainsNotConflicting;
        AllowDivergentGuardedReadForTests = allowDivergentGuardedReadForTests;
        ReadMustNotTriggerSyncUpload = readMustNotTriggerSyncUpload;
        ReadMustNotMutateStore = readMustNotMutateStore;
        MaxDiagnosticsEvents = Math.Max(0, maxDiagnosticsEvents);
    }

    public static CanonicalReadRuntimePolicy ExplicitGuardedDebugInternal(
        bool allowDivergentGuardedReadForTests = false)
        => new(
            debugInternalBuild: true,
            ownerApproved: true,
            manualOwnerApproval: true,
            releaseDefaultBuild: false,
            legacyFallbackAvailable: true,
            diagnosticsRedacted: true,
            applyRuntimeEvidenceValidForNonAudio: true,
            uploadRuntimeEvidenceValidForAudioStatus: true,
            inventorySnapshotAvailable: true,
            planAuthorityEvidenceValid: true,
            existenceTruthEvidenceValid: true,
            otherDomainsNotConflicting: true,
            allowDivergentGuardedReadForTests: allowDivergentGuardedReadForTests);

    public override bool Equals(object? obj) => obj is CanonicalReadRuntimePolicy other && Equals(other);
    public bool Equals(CanonicalReadRuntimePolicy? other) =>
        other is not null &&
        DebugInternalBuild == other.DebugInternalBuild &&
        OwnerApproved == other.OwnerApproved &&
        ManualOwnerApproval == other.ManualOwnerApproval &&
        ReleaseDefaultBuild == other.ReleaseDefaultBuild &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        DiagnosticsRedacted == other.DiagnosticsRedacted &&
        ApplyRuntimeEvidenceValidForNonAudio == other.ApplyRuntimeEvidenceValidForNonAudio &&
        UploadRuntimeEvidenceValidForAudioStatus == other.UploadRuntimeEvidenceValidForAudioStatus &&
        InventorySnapshotAvailable == other.InventorySnapshotAvailable &&
        PlanAuthorityEvidenceValid == other.PlanAuthorityEvidenceValid &&
        ExistenceTruthEvidenceValid == other.ExistenceTruthEvidenceValid &&
        OtherDomainsNotConflicting == other.OtherDomainsNotConflicting &&
        AllowDivergentGuardedReadForTests == other.AllowDivergentGuardedReadForTests &&
        ReadMustNotTriggerSyncUpload == other.ReadMustNotTriggerSyncUpload &&
        ReadMustNotMutateStore == other.ReadMustNotMutateStore &&
        MaxDiagnosticsEvents == other.MaxDiagnosticsEvents;
    public override int GetHashCode() =>
        HashCode.Combine(DebugInternalBuild, OwnerApproved, ReleaseDefaultBuild, MaxDiagnosticsEvents);
    public static bool operator ==(CanonicalReadRuntimePolicy left, CanonicalReadRuntimePolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalReadRuntimePolicy left, CanonicalReadRuntimePolicy right) => !left.Equals(right);
}

public sealed class CanonicalReadRuntimeConfiguration : IEquatable<CanonicalReadRuntimeConfiguration>
{
    public CanonicalReadRuntimeMode Mode { get; }
    public CanonicalReadRuntimePolicy Policy { get; }

    public CanonicalReadRuntimeConfiguration(
        CanonicalReadRuntimeMode mode = CanonicalReadRuntimeMode.disabled,
        CanonicalReadRuntimePolicy? policy = null)
    {
        Mode = mode;
        Policy = policy ?? new CanonicalReadRuntimePolicy();
    }

    public static readonly CanonicalReadRuntimeConfiguration Disabled = new();

    public static CanonicalReadRuntimeConfiguration ExplicitGuardedCanonicalRead(
        bool allowDivergentGuardedReadForTests = false)
        => new(
            mode: CanonicalReadRuntimeMode.guardedCanonicalReadWithLegacyFallback,
            policy: CanonicalReadRuntimePolicy.ExplicitGuardedDebugInternal(
                allowDivergentGuardedReadForTests: allowDivergentGuardedReadForTests));

    public override bool Equals(object? obj) => obj is CanonicalReadRuntimeConfiguration other && Equals(other);
    public bool Equals(CanonicalReadRuntimeConfiguration? other) =>
        other is not null && Mode == other.Mode && Policy.Equals(other.Policy);
    public override int GetHashCode() => HashCode.Combine(Mode, Policy);
    public static bool operator ==(CanonicalReadRuntimeConfiguration left, CanonicalReadRuntimeConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalReadRuntimeConfiguration left, CanonicalReadRuntimeConfiguration right) => !left.Equals(right);
}

// ─── Projection Failure Types ───────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalReadProjectionFailureKind
{
    snapshotMissing,
    unsupportedObject,
    pathContentLeakRisk
}

public sealed class CanonicalReadProjectionFailure : IEquatable<CanonicalReadProjectionFailure>
{
    public string Id => string.Join("|", Kind.ToString(), Domain.ToString(), ObjectID ?? "run", Reason);

    public CanonicalReadProjectionFailureKind Kind { get; }
    public CanonicalReadDomain Domain { get; }
    public string? ObjectID { get; }
    public string Reason { get; }

    public CanonicalReadProjectionFailure(
        CanonicalReadProjectionFailureKind kind,
        CanonicalReadDomain domain,
        string? objectID = null,
        string reason = "")
    {
        Kind = kind;
        Domain = domain;
        ObjectID = objectID != null ? CanonicalReadRuntimeRedaction.SafeIdentifier(objectID, "object") : null;
        Reason = CanonicalReadRuntimeRedaction.SafeText(reason) ?? kind.ToString();
    }

    public override bool Equals(object? obj) => obj is CanonicalReadProjectionFailure other && Equals(other);
    public bool Equals(CanonicalReadProjectionFailure? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalReadProjectionFailure left, CanonicalReadProjectionFailure right) => left.Equals(right);
    public static bool operator !=(CanonicalReadProjectionFailure left, CanonicalReadProjectionFailure right) => !left.Equals(right);
}

// ─── Recording Read Projection ──────────────────────────────────────────────

public sealed class CanonicalRecordingReadProjectionRecord : IEquatable<CanonicalRecordingReadProjectionRecord>
{
    public string Id => ObjectID;
    public string ObjectID { get; }
    public string Title { get; }
    public string[] Tags { get; }
    public string FilingSummary { get; }
    public string CreatedAtSummary { get; }
    public string ModifiedAtSummary { get; }
    public int? DurationSeconds { get; }
    public string? MetadataHashPrefix { get; }
    public bool IsDeleted { get; }
    public CanonicalSyncState SyncState { get; }
    public string ProcessingSummary { get; }

    public CanonicalRecordingReadProjectionRecord(CanonicalRecordingObject obj) : this(
        objectID: obj.ObjectID,
        title: obj.Metadata.Title,
        tags: obj.Metadata.Tags ?? Array.Empty<string>(),
        filingSummary: FilingSummaryString(obj.Metadata.FilingValue),
        createdAt: obj.Metadata.CreatedAt,
        modifiedAt: obj.Metadata.ModifiedAt,
        duration: obj.Metadata.Duration.HasValue
            ? obj.Metadata.Duration.Value.TotalSeconds
            : (double?)null,
        metadataHashPrefix: CanonicalReadRuntimeRedaction.HashPrefix(obj.MetadataHash.Value),
        isDeleted: obj.Metadata.IsDeleted,
        syncState: obj.SyncState,
        processingSummary: $"transcription={obj.ProcessingState.Transcription},note={obj.ProcessingState.Note}")
    { }

    public CanonicalRecordingReadProjectionRecord(
        string objectID,
        string title,
        string[]? tags = null,
        string filingSummary = "none",
        CanonicalTimestamp createdAt = default,
        CanonicalTimestamp modifiedAt = default,
        double? duration = null,
        string? metadataHashPrefix = null,
        bool isDeleted = false,
        CanonicalSyncState syncState = CanonicalSyncState.unknown,
        string processingSummary = "unknown")
    {
        ObjectID = CanonicalReadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
        Title = CanonicalReadRuntimeRedaction.SafeDisplayText(title, "Untitled");
        Tags = (tags ?? Array.Empty<string>())
            .Select(t => CanonicalReadRuntimeRedaction.SafeDisplayText(t, ""))
            .Where(t => t != null && t.Length > 0)
            .Cast<string>()
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();
        FilingSummary = CanonicalReadRuntimeRedaction.SafeText(filingSummary) ?? "none";
        CreatedAtSummary = TimestampSummary(createdAt);
        ModifiedAtSummary = TimestampSummary(modifiedAt);
        DurationSeconds = duration.HasValue ? Math.Max(0, (int)Math.Round(duration.Value)) : null;
        MetadataHashPrefix = CanonicalReadRuntimeRedaction.HashPrefix(metadataHashPrefix);
        IsDeleted = isDeleted;
        SyncState = syncState;
        ProcessingSummary = CanonicalReadRuntimeRedaction.SafeText(processingSummary) ?? "unknown";
    }

    public string TagsKey => string.Join("|", Tags);

    private static string FilingSummaryString(CanonicalRecordingMetadata.Filing? filing)
    {
        if (filing == null) return "none";
        var items = new List<string>();
        if (filing.Type != null) items.Add($"type={filing.Type}");
        if (filing.Subject != null) items.Add($"subject={filing.Subject}");
        if (filing.Chapter != null) items.Add($"chapter={filing.Chapter}");
        if (filing.Topic != null) items.Add($"topic={filing.Topic}");
        var summary = string.Join(",", items);
        return summary.Length == 0 ? "none" : summary;
    }

    private static string TimestampSummary(CanonicalTimestamp timestamp)
        => $"unixSeconds={(long)(timestamp.Date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds}";

    public override bool Equals(object? obj) => obj is CanonicalRecordingReadProjectionRecord other && Equals(other);
    public bool Equals(CanonicalRecordingReadProjectionRecord? other) =>
        other is not null && ObjectID == other.ObjectID;
    public override int GetHashCode() => ObjectID.GetHashCode();
    public static bool operator ==(CanonicalRecordingReadProjectionRecord left, CanonicalRecordingReadProjectionRecord right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingReadProjectionRecord left, CanonicalRecordingReadProjectionRecord right) => !left.Equals(right);
}

public sealed class CanonicalRecordingReadProjection : IEquatable<CanonicalRecordingReadProjection>
{
    public CanonicalReadProjectionSource Source { get; }
    public CanonicalRecordingReadProjectionRecord[] Records { get; }
    public CanonicalReadProjectionFailure[] Failures { get; }

    public CanonicalRecordingReadProjection(
        CanonicalReadProjectionSource source,
        CanonicalRecordingReadProjectionRecord[]? records = null,
        CanonicalReadProjectionFailure[]? failures = null)
    {
        Source = source;
        Records = (records ?? Array.Empty<CanonicalRecordingReadProjectionRecord>())
            .OrderBy(r => r.ObjectID, StringComparer.Ordinal).ToArray();
        Failures = (failures ?? Array.Empty<CanonicalReadProjectionFailure>())
            .OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
    }

    public static CanonicalRecordingReadProjection Build(
        CanonicalReadProjectionSource source,
        CanonicalManifest? manifest)
    {
        if (manifest == null)
            return new CanonicalRecordingReadProjection(
                source: source,
                failures: new[]
                {
                    new CanonicalReadProjectionFailure(
                        CanonicalReadProjectionFailureKind.snapshotMissing,
                        CanonicalReadDomain.recordingMetadata,
                        reason: "recordingManifestMissing")
                });

        return new CanonicalRecordingReadProjection(
            source: source,
            records: manifest.Objects
                .Select(o => new CanonicalRecordingReadProjectionRecord(o))
                .ToArray());
    }

    public string DiagnosticsSummary
        => $"source={Source},records={Records.Length},failures={Failures.Length}";

    public override bool Equals(object? obj) => obj is CanonicalRecordingReadProjection other && Equals(other);
    public bool Equals(CanonicalRecordingReadProjection? other) =>
        other is not null &&
        Source == other.Source &&
        Records.SequenceEqual(other.Records) &&
        Failures.SequenceEqual(other.Failures);
    public override int GetHashCode() => HashCode.Combine(Source, Records.Length, Failures.Length);
    public static bool operator ==(CanonicalRecordingReadProjection left, CanonicalRecordingReadProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingReadProjection left, CanonicalRecordingReadProjection right) => !left.Equals(right);
}

// ─── Library / Artifact / Conflict / Upload / Sync Projections ──────────────

public sealed class CanonicalLibraryReadProjection : IEquatable<CanonicalLibraryReadProjection>
{
    public CanonicalReadProjectionSource Source { get; }
    public CanonicalLibraryMetadataReadSnapshot Snapshot { get; }

    public CanonicalLibraryReadProjection(
        CanonicalReadProjectionSource source,
        CanonicalLibraryMetadataReadSnapshot snapshot)
    {
        Source = source;
        Snapshot = snapshot;
    }

    public static CanonicalLibraryReadProjection Build(
        CanonicalReadProjectionSource source,
        CanonicalManifest? manifest)
        => new(
            source: source,
            snapshot: CanonicalLibraryMetadataReadProjection.Build(
                source: source.LibraryMetadataSource, manifest: manifest).Snapshot);

    public override bool Equals(object? obj) => obj is CanonicalLibraryReadProjection other && Equals(other);
    public bool Equals(CanonicalLibraryReadProjection? other) =>
        other is not null && Source == other.Source && Snapshot.Equals(other.Snapshot);
    public override int GetHashCode() => HashCode.Combine(Source, Snapshot);
    public static bool operator ==(CanonicalLibraryReadProjection left, CanonicalLibraryReadProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryReadProjection left, CanonicalLibraryReadProjection right) => !left.Equals(right);
}

public sealed class CanonicalArtifactReadProjection : IEquatable<CanonicalArtifactReadProjection>
{
    public CanonicalReadProjectionSource Source { get; }
    public CanonicalGeneratedArtifactReadSnapshot Snapshot { get; }

    public CanonicalArtifactReadProjection(
        CanonicalReadProjectionSource source,
        CanonicalGeneratedArtifactReadSnapshot snapshot)
    {
        Source = source;
        Snapshot = snapshot;
    }

    public static CanonicalArtifactReadProjection Build(
        CanonicalReadProjectionSource source,
        CanonicalManifest? localManifest,
        CanonicalManifest? peerManifest = null)
    {
        var facts = new List<CanonicalGeneratedArtifactReadProjectionArtifactFact>();
        if (localManifest != null)
            facts.AddRange(GeneratedArtifactFacts(localManifest, peerAuthoritative: false));
        if (peerManifest != null)
            facts.AddRange(GeneratedArtifactFacts(peerManifest, peerAuthoritative: true));

        List<CanonicalGeneratedArtifactReadProjectionFailure> failures;
        if (localManifest == null && peerManifest == null)
            failures = new List<CanonicalGeneratedArtifactReadProjectionFailure>
            {
                new(
                    kind: CanonicalGeneratedArtifactReadProjectionFailureKind.snapshotMissing,
                    source: source.GeneratedArtifactSource,
                    reason: "generatedArtifactReadProjectionSnapshotMissing")
            };
        else
            failures = new List<CanonicalGeneratedArtifactReadProjectionFailure>();

        return new CanonicalArtifactReadProjection(
            source: source,
            snapshot: CanonicalGeneratedArtifactReadProjection.Snapshot(
                source: source.GeneratedArtifactSource,
                facts: facts.ToArray(),
                failures: failures.ToArray()));
    }

    private static CanonicalGeneratedArtifactReadProjectionArtifactFact[] GeneratedArtifactFacts(
        CanonicalManifest manifest,
        bool peerAuthoritative)
    {
        var facts = new List<CanonicalGeneratedArtifactReadProjectionArtifactFact>();
        foreach (var obj in manifest.Objects)
        foreach (var artifact in obj.Artifacts.Where(a => a.IsCanonicalGeneratedArtifact))
            facts.Add(new CanonicalGeneratedArtifactReadProjectionArtifactFact(
                artifact: artifact,
                parentTombstoned: obj.Metadata.IsDeleted || obj.SyncState == CanonicalSyncState.deleted,
                localAvailability: !peerAuthoritative && CanonicalProjectionContract.ProvesGeneratedArtifactAvailability(artifact),
                peerAuthoritativeAvailability: peerAuthoritative && CanonicalProjectionContract.IsAuthoritativeProducer(artifact, manifest.Node),
                producerSummary: artifact.ProducedBy?.ToString() ?? manifest.Node.Platform,
                unsafePathTokenObserved: false));
        return facts.ToArray();
    }

    public override bool Equals(object? obj) => obj is CanonicalArtifactReadProjection other && Equals(other);
    public bool Equals(CanonicalArtifactReadProjection? other) =>
        other is not null && Source == other.Source && Snapshot.Equals(other.Snapshot);
    public override int GetHashCode() => HashCode.Combine(Source, Snapshot);
    public static bool operator ==(CanonicalArtifactReadProjection left, CanonicalArtifactReadProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalArtifactReadProjection left, CanonicalArtifactReadProjection right) => !left.Equals(right);
}

public sealed class CanonicalConflictReadProjection : IEquatable<CanonicalConflictReadProjection>
{
    public CanonicalReadProjectionSource Source { get; }
    public CanonicalTombstoneConflictReadSnapshot Snapshot { get; }

    public CanonicalConflictReadProjection(
        CanonicalReadProjectionSource source,
        CanonicalTombstoneConflictReadSnapshot snapshot)
    {
        Source = source;
        Snapshot = snapshot;
    }

    public static CanonicalConflictReadProjection Build(
        CanonicalReadProjectionSource source,
        CanonicalManifest? localManifest,
        CanonicalManifest? peerManifest = null,
        CanonicalApplyPlan? applyPlan = null,
        CanonicalLibrarySyncPlan? libraryPlan = null)
        => new(
            source: source,
            snapshot: CanonicalTombstoneConflictReadProjection.Snapshot(
                source: source.TombstoneConflictSource,
                localManifest: localManifest,
                peerManifest: peerManifest,
                applyPlan: applyPlan,
                libraryPlan: libraryPlan));

    public override bool Equals(object? obj) => obj is CanonicalConflictReadProjection other && Equals(other);
    public bool Equals(CanonicalConflictReadProjection? other) =>
        other is not null && Source == other.Source && Snapshot.Equals(other.Snapshot);
    public override int GetHashCode() => HashCode.Combine(Source, Snapshot);
    public static bool operator ==(CanonicalConflictReadProjection left, CanonicalConflictReadProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalConflictReadProjection left, CanonicalConflictReadProjection right) => !left.Equals(right);
}

public sealed class CanonicalUploadReadProjectionRecord : IEquatable<CanonicalUploadReadProjectionRecord>
{
    public string Id => ObjectID;
    public string ObjectID { get; }
    public bool AudioAvailable { get; }
    public CanonicalArtifact.AvailabilityKind AudioAvailability { get; }
    public long? ByteSize { get; }
    public string? AudioHashPrefix { get; }
    public CanonicalAudioUploadPeerState? PeerState { get; }
    public CanonicalAudioUploadActionKind? UploadAction { get; }
    public CanonicalAudioUploadEvidenceStatus? UploadEvidenceStatus { get; }
    public CanonicalAudioUploadLedgerPhase? UploadLedgerPhase { get; }
    public bool RetryEligible { get; }
    public bool CreatedUploadJob { get; }
    public bool PathIncluded { get; }
    public bool ContentIncluded { get; }

    public CanonicalUploadReadProjectionRecord(
        string objectID,
        bool audioAvailable,
        CanonicalArtifact.AvailabilityKind audioAvailability,
        long? byteSize = null,
        string? audioHashPrefix = null,
        CanonicalAudioUploadPeerState? peerState = null,
        CanonicalAudioUploadActionKind? uploadAction = null,
        CanonicalAudioUploadEvidenceStatus? uploadEvidenceStatus = null,
        CanonicalAudioUploadLedgerPhase? uploadLedgerPhase = null,
        bool retryEligible = false)
    {
        ObjectID = CanonicalReadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
        AudioAvailable = audioAvailable;
        AudioAvailability = audioAvailability;
        ByteSize = byteSize;
        AudioHashPrefix = CanonicalReadRuntimeRedaction.HashPrefix(audioHashPrefix);
        PeerState = peerState;
        UploadAction = uploadAction;
        UploadEvidenceStatus = uploadEvidenceStatus;
        UploadLedgerPhase = uploadLedgerPhase;
        RetryEligible = retryEligible;
        CreatedUploadJob = false;
        PathIncluded = false;
        ContentIncluded = false;
    }

    public CanonicalUploadReadProjectionRecord(
        CanonicalRecordingObject obj,
        CanonicalAudioUploadCutoverCandidate? candidate)
    {
        var audio = obj.AudioArtifact;
        ObjectID = CanonicalReadRuntimeRedaction.SafeIdentifier(obj.ObjectID, "unknown-recording");
        AudioAvailable = obj.AudioAvailable;
        AudioAvailability = audio?.Availability ?? CanonicalArtifact.AvailabilityKind.missing;
        ByteSize = audio?.ByteSize;
        AudioHashPrefix = CanonicalReadRuntimeRedaction.HashPrefix(audio?.ContentHash?.Value);
        PeerState = candidate?.PeerTruth?.State;
        UploadAction = candidate?.ActionKind;
        UploadEvidenceStatus = candidate?.EvidenceStatus;
        UploadLedgerPhase = candidate?.LedgerTruth?.Phase;
        RetryEligible = candidate?.RetryTruth?.HasExistingEligibleRetry == true;
        CreatedUploadJob = false;
        PathIncluded = false;
        ContentIncluded = false;
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadReadProjectionRecord other && Equals(other);
    public bool Equals(CanonicalUploadReadProjectionRecord? other) =>
        other is not null && ObjectID == other.ObjectID;
    public override int GetHashCode() => ObjectID.GetHashCode();
    public static bool operator ==(CanonicalUploadReadProjectionRecord left, CanonicalUploadReadProjectionRecord right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadReadProjectionRecord left, CanonicalUploadReadProjectionRecord right) => !left.Equals(right);
}

public sealed class CanonicalUploadReadProjection : IEquatable<CanonicalUploadReadProjection>
{
    public CanonicalReadProjectionSource Source { get; }
    public CanonicalUploadReadProjectionRecord[] Records { get; }
    public CanonicalReadProjectionFailure[] Failures { get; }

    public CanonicalUploadReadProjection(
        CanonicalReadProjectionSource source,
        CanonicalUploadReadProjectionRecord[]? records = null,
        CanonicalReadProjectionFailure[]? failures = null)
    {
        Source = source;
        Records = (records ?? Array.Empty<CanonicalUploadReadProjectionRecord>())
            .OrderBy(r => r.ObjectID, StringComparer.Ordinal).ToArray();
        Failures = (failures ?? Array.Empty<CanonicalReadProjectionFailure>())
            .OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
    }

    public static CanonicalUploadReadProjection Build(
        CanonicalReadProjectionSource source,
        CanonicalManifest? manifest,
        CanonicalAudioUploadCutoverCandidate[]? uploadCandidates = null)
    {
        if (manifest == null)
            return new CanonicalUploadReadProjection(
                source: source,
                failures: new[]
                {
                    new CanonicalReadProjectionFailure(
                        CanonicalReadProjectionFailureKind.snapshotMissing,
                        CanonicalReadDomain.audioUploadStatus,
                        reason: "uploadManifestMissing")
                });

        var candidatesDict = (uploadCandidates ?? Array.Empty<CanonicalAudioUploadCutoverCandidate>())
            .GroupBy(c => c.ObjectID)
            .ToDictionary(g => g.Key, g => g.First());

        return new CanonicalUploadReadProjection(
            source: source,
            records: manifest.Objects
                .Select(obj =>
                {
                    candidatesDict.TryGetValue(obj.ObjectID, out var candidate);
                    return new CanonicalUploadReadProjectionRecord(obj, candidate);
                })
                .ToArray());
    }

    public string DiagnosticsSummary
    {
        get
        {
            var available = Records.Count(r => r.AudioAvailable);
            var uploadCandidates = Records.Count(r => r.UploadAction == CanonicalAudioUploadActionKind.audioUploadCanaryCandidate);
            return $"source={Source},records={Records.Length},audioAvailable={available},uploadCandidates={uploadCandidates},failures={Failures.Length}";
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadReadProjection other && Equals(other);
    public bool Equals(CanonicalUploadReadProjection? other) =>
        other is not null && Source == other.Source && Records.SequenceEqual(other.Records) && Failures.SequenceEqual(other.Failures);
    public override int GetHashCode() => HashCode.Combine(Source, Records.Length, Failures.Length);
    public static bool operator ==(CanonicalUploadReadProjection left, CanonicalUploadReadProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadReadProjection left, CanonicalUploadReadProjection right) => !left.Equals(right);
}

public sealed class CanonicalSyncEngineStatusReadProjection : IEquatable<CanonicalSyncEngineStatusReadProjection>
{
    public CanonicalReadProjectionSource Source { get; }
    public CanonicalReadRuntimeMode? Mode { get; }
    public CanonicalSyncRuntimeMode? SyncRuntimeMode { get; }
    public bool CanonicalPlanUsed { get; }
    public bool CanonicalPlanFallback { get; }
    public bool CanonicalPlanBlocked { get; }
    public bool CanonicalPlanNoCommit { get; }
    public int PendingTransferCount { get; }
    public int InFlightTransferCount { get; }
    public int FailedTransferCount { get; }
    public string? LastStatusSummary { get; }
    public bool SyncOrUploadTriggeredByRead { get; }

    public CanonicalSyncEngineStatusReadProjection(
        CanonicalReadProjectionSource source,
        CanonicalReadRuntimeMode? mode = null,
        CanonicalSyncRuntimeResult? syncRuntimeResult = null,
        int pendingTransferCount = 0,
        int inFlightTransferCount = 0,
        int failedTransferCount = 0,
        string? lastStatusSummary = null)
    {
        Source = source;
        Mode = mode;
        SyncRuntimeMode = syncRuntimeResult?.Mode;
        CanonicalPlanUsed = syncRuntimeResult?.CanonicalPlanUsed ?? false;
        CanonicalPlanFallback = syncRuntimeResult?.CanonicalPlanFallback ?? false;
        CanonicalPlanBlocked = syncRuntimeResult?.CanonicalPlanBlocked ?? false;
        CanonicalPlanNoCommit = syncRuntimeResult?.CanonicalPlanNoCommit ?? false;
        PendingTransferCount = Math.Max(0, pendingTransferCount);
        InFlightTransferCount = Math.Max(0, inFlightTransferCount);
        FailedTransferCount = Math.Max(0, failedTransferCount);
        LastStatusSummary = lastStatusSummary != null
            ? CanonicalReadRuntimeRedaction.SafeText(lastStatusSummary)
            : null;
        SyncOrUploadTriggeredByRead = false;
    }

    public string DiagnosticsSummary
    {
        get
        {
            var parts = new List<string> { $"source={Source}" };
            if (Mode.HasValue) parts.Add($"readMode={Mode}");
            if (SyncRuntimeMode.HasValue) parts.Add($"syncMode={SyncRuntimeMode}");
            parts.Add($"canonicalPlanUsed={CanonicalPlanUsed}");
            parts.Add($"fallback={CanonicalPlanFallback}");
            parts.Add($"blocked={CanonicalPlanBlocked}");
            parts.Add($"pending={PendingTransferCount}");
            parts.Add($"inFlight={InFlightTransferCount}");
            parts.Add($"failed={FailedTransferCount}");
            parts.Add("syncOrUploadTriggeredByRead=false");
            return string.Join(",", parts);
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncEngineStatusReadProjection other && Equals(other);
    public bool Equals(CanonicalSyncEngineStatusReadProjection? other) =>
        other is not null && Source == other.Source;
    public override int GetHashCode() => Source.GetHashCode();
    public static bool operator ==(CanonicalSyncEngineStatusReadProjection left, CanonicalSyncEngineStatusReadProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncEngineStatusReadProjection left, CanonicalSyncEngineStatusReadProjection right) => !left.Equals(right);
}

// ─── Read Snapshot ──────────────────────────────────────────────────────────

public sealed class CanonicalReadSnapshotRedaction : IEquatable<CanonicalReadSnapshotRedaction>
{
    public bool ExcludesAbsolutePaths { get; }
    public bool ExcludesFullHashes { get; }
    public bool ExcludesSecrets { get; }
    public bool ExcludesFullGeneratedContent { get; }
    public bool ExcludesRequestResponseBodies { get; }

    public CanonicalReadSnapshotRedaction(
        bool excludesAbsolutePaths,
        bool excludesFullHashes,
        bool excludesSecrets,
        bool excludesFullGeneratedContent,
        bool excludesRequestResponseBodies)
    {
        ExcludesAbsolutePaths = excludesAbsolutePaths;
        ExcludesFullHashes = excludesFullHashes;
        ExcludesSecrets = excludesSecrets;
        ExcludesFullGeneratedContent = excludesFullGeneratedContent;
        ExcludesRequestResponseBodies = excludesRequestResponseBodies;
    }

    public static readonly CanonicalReadSnapshotRedaction Redacted = new(
        excludesAbsolutePaths: true,
        excludesFullHashes: true,
        excludesSecrets: true,
        excludesFullGeneratedContent: true,
        excludesRequestResponseBodies: true);

    public bool IsRedacted =>
        ExcludesAbsolutePaths
        && ExcludesFullHashes
        && ExcludesSecrets
        && ExcludesFullGeneratedContent
        && ExcludesRequestResponseBodies;

    public override bool Equals(object? obj) => obj is CanonicalReadSnapshotRedaction other && Equals(other);
    public bool Equals(CanonicalReadSnapshotRedaction? other) =>
        other is not null &&
        ExcludesAbsolutePaths == other.ExcludesAbsolutePaths &&
        ExcludesFullHashes == other.ExcludesFullHashes &&
        ExcludesSecrets == other.ExcludesSecrets &&
        ExcludesFullGeneratedContent == other.ExcludesFullGeneratedContent &&
        ExcludesRequestResponseBodies == other.ExcludesRequestResponseBodies;
    public override int GetHashCode() =>
        HashCode.Combine(ExcludesAbsolutePaths, ExcludesFullHashes, ExcludesSecrets);
    public static bool operator ==(CanonicalReadSnapshotRedaction left, CanonicalReadSnapshotRedaction right) => left.Equals(right);
    public static bool operator !=(CanonicalReadSnapshotRedaction left, CanonicalReadSnapshotRedaction right) => !left.Equals(right);
}

public sealed class CanonicalReadSnapshot : IEquatable<CanonicalReadSnapshot>
{
    public CanonicalReadProjectionSource Source { get; }
    public CanonicalTimestamp GeneratedAt { get; }
    public CanonicalRecordingReadProjection RecordingMetadata { get; }
    public CanonicalLibraryReadProjection LibraryMetadata { get; }
    public CanonicalArtifactReadProjection ArtifactMetadata { get; }
    public CanonicalConflictReadProjection ConflictState { get; }
    public CanonicalUploadReadProjection UploadState { get; }
    public CanonicalSyncEngineStatusReadProjection SyncStatus { get; }
    public CanonicalReadSnapshotRedaction Redaction { get; }

    public CanonicalReadSnapshot(
        CanonicalReadProjectionSource source,
        DateTime? generatedAt = null,
        CanonicalRecordingReadProjection? recordingMetadata = null,
        CanonicalLibraryReadProjection? libraryMetadata = null,
        CanonicalArtifactReadProjection? artifactMetadata = null,
        CanonicalConflictReadProjection? conflictState = null,
        CanonicalUploadReadProjection? uploadState = null,
        CanonicalSyncEngineStatusReadProjection? syncStatus = null,
        CanonicalReadSnapshotRedaction? redaction = null)
    {
        var now = generatedAt ?? DateTime.UtcNow;
        Source = source;
        GeneratedAt = new CanonicalTimestamp(now);
        RecordingMetadata = recordingMetadata
                            ?? new CanonicalRecordingReadProjection(source);
        LibraryMetadata = libraryMetadata
                          ?? new CanonicalLibraryReadProjection(source, new CanonicalLibraryMetadataReadSnapshot());
        ArtifactMetadata = artifactMetadata
                           ?? new CanonicalArtifactReadProjection(source, new CanonicalGeneratedArtifactReadSnapshot());
        ConflictState = conflictState
                        ?? new CanonicalConflictReadProjection(source, new CanonicalTombstoneConflictReadSnapshot());
        UploadState = uploadState
                      ?? new CanonicalUploadReadProjection(source);
        SyncStatus = syncStatus
                     ?? new CanonicalSyncEngineStatusReadProjection(source);
        Redaction = redaction ?? CanonicalReadSnapshotRedaction.Redacted;
    }

    public static CanonicalReadSnapshot Build(
        CanonicalReadProjectionSource source,
        CanonicalManifest? manifest,
        CanonicalManifest? peerManifest = null,
        CanonicalApplyPlan? applyPlan = null,
        CanonicalLibrarySyncPlan? libraryPlan = null,
        CanonicalAudioUploadCutoverCandidate[]? uploadCandidates = null,
        CanonicalSyncRuntimeResult? syncRuntimeResult = null,
        DateTime? generatedAt = null)
        => new(
            source: source,
            generatedAt: generatedAt,
            recordingMetadata: CanonicalRecordingReadProjection.Build(source, manifest),
            libraryMetadata: CanonicalLibraryReadProjection.Build(source, manifest),
            artifactMetadata: CanonicalArtifactReadProjection.Build(source, manifest, peerManifest),
            conflictState: CanonicalConflictReadProjection.Build(source, manifest, peerManifest, applyPlan, libraryPlan),
            uploadState: CanonicalUploadReadProjection.Build(source, manifest, uploadCandidates),
            syncStatus: new CanonicalSyncEngineStatusReadProjection(source, syncRuntimeResult: syncRuntimeResult));

    public bool PathOrContentLeakRisk
    {
        get
        {
            if (!Redaction.IsRedacted) return true;
            var as = ArtifactMetadata.Snapshot;
            if (as.ContentIncludedCount > 0
                || as.Failures.Any(f => f.Kind == CanonicalGeneratedArtifactReadProjectionFailureKind.contentLeakRisk
                                        || f.Kind == CanonicalGeneratedArtifactReadProjectionFailureKind.unsafePathToken))
                return true;
            var cs = ConflictState.Snapshot;
            if (cs.FullContentIncludedCount > 0 || cs.AbsolutePathIncludedCount > 0 || cs.PathLeakRiskCount > 0)
                return true;
            var ls = LibraryMetadata.Snapshot;
            if (ls.PathLeakRiskCount > 0 || ls.FullContentIncluded)
                return true;
            if (UploadState.Records.Any(r => r.PathIncluded || r.ContentIncluded))
                return true;
            return false;
        }
    }

    public string DiagnosticsSummary
    {
        get
        {
            var items = new List<string>
            {
                $"source={Source}",
                $"recordings={RecordingMetadata.Records.Length}",
                $"folders={LibraryMetadata.Snapshot.Folders.Length}",
                $"items={LibraryMetadata.Snapshot.StudyItems.Length}",
                $"artifacts={ArtifactMetadata.Snapshot.ItemCount}",
                $"conflicts={ConflictState.Snapshot.Items.Count(i => i.ConflictStatus != "none")}",
                $"uploadRecords={UploadState.Records.Length}",
                $"redacted={Redaction.IsRedacted}",
                $"syncStatus={SyncStatus.DiagnosticsSummary}"
            };
            return string.Join(",", items);
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalReadSnapshot other && Equals(other);
    public bool Equals(CanonicalReadSnapshot? other) =>
        other is not null && Source == other.Source && GeneratedAt.Equals(other.GeneratedAt);
    public override int GetHashCode() => HashCode.Combine(Source, GeneratedAt);
    public static bool operator ==(CanonicalReadSnapshot left, CanonicalReadSnapshot right) => left.Equals(right);
    public static bool operator !=(CanonicalReadSnapshot left, CanonicalReadSnapshot right) => !left.Equals(right);
}

// ─── Divergence Types ───────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalReadRuntimeDivergenceKind
{
    missingObject,
    metadataMismatch,
    titleTagsFolderMismatch,
    artifactAvailabilityMismatch,
    tombstoneConflictMismatch,
    audioAvailabilityMismatch,
    uploadStatusMismatch,
    unsupportedObject,
    pathContentLeakRisk
}

public sealed class CanonicalReadRuntimeDivergence : IEquatable<CanonicalReadRuntimeDivergence>
{
    public string Id => string.Join("|", Kind.ToString(), Domain.ToString(), ObjectID ?? "run", Field ?? "");

    public CanonicalReadRuntimeDivergenceKind Kind { get; }
    public CanonicalReadDomain Domain { get; }
    public string? ObjectID { get; }
    public string? Field { get; }
    public string? LegacyValue { get; }
    public string? CanonicalValue { get; }
    public bool Fatal { get; }

    public CanonicalReadRuntimeDivergence(
        CanonicalReadRuntimeDivergenceKind kind,
        CanonicalReadDomain domain,
        string? objectID = null,
        string? field = null,
        string? legacyValue = null,
        string? canonicalValue = null,
        bool fatal = false)
    {
        Kind = kind;
        Domain = domain;
        ObjectID = objectID != null ? CanonicalReadRuntimeRedaction.SafeIdentifier(objectID, "object") : null;
        Field = CanonicalReadRuntimeRedaction.SafeText(field);
        LegacyValue = CanonicalReadRuntimeRedaction.SafeText(legacyValue);
        CanonicalValue = CanonicalReadRuntimeRedaction.SafeText(canonicalValue);
        Fatal = fatal || kind == CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk;
    }

    public override bool Equals(object? obj) => obj is CanonicalReadRuntimeDivergence other && Equals(other);
    public bool Equals(CanonicalReadRuntimeDivergence? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalReadRuntimeDivergence left, CanonicalReadRuntimeDivergence right) => left.Equals(right);
    public static bool operator !=(CanonicalReadRuntimeDivergence left, CanonicalReadRuntimeDivergence right) => !left.Equals(right);
}

public sealed class CanonicalReadRuntimeEquivalenceReport : IEquatable<CanonicalReadRuntimeEquivalenceReport>
{
    public bool Equivalent { get; }
    public int DivergenceCount { get; }
    public int FatalDivergenceCount { get; }
    public CanonicalReadDomain[] DomainsCompared { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalReadRuntimeEquivalenceReport(
        bool equivalent,
        int divergenceCount,
        int fatalDivergenceCount,
        CanonicalReadDomain[] domainsCompared,
        string diagnosticsSummary)
    {
        Equivalent = equivalent;
        DivergenceCount = divergenceCount;
        FatalDivergenceCount = fatalDivergenceCount;
        DomainsCompared = domainsCompared ?? Array.Empty<CanonicalReadDomain>();
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalReadRuntimeEquivalenceReport other && Equals(other);
    public bool Equals(CanonicalReadRuntimeEquivalenceReport? other) =>
        other is not null && Equivalent == other.Equivalent && DivergenceCount == other.DivergenceCount;
    public override int GetHashCode() => HashCode.Combine(Equivalent, DivergenceCount);
    public static bool operator ==(CanonicalReadRuntimeEquivalenceReport left, CanonicalReadRuntimeEquivalenceReport right) => left.Equals(right);
    public static bool operator !=(CanonicalReadRuntimeEquivalenceReport left, CanonicalReadRuntimeEquivalenceReport right) => !left.Equals(right);
}

public sealed class CanonicalReadRuntimeDiff : IEquatable<CanonicalReadRuntimeDiff>
{
    public CanonicalReadRuntimeDivergence[] Divergences { get; }
    public CanonicalReadRuntimeEquivalenceReport EquivalenceReport { get; }
    public string LegacySnapshotSummary { get; }
    public string CanonicalSnapshotSummary { get; }
    public string DiagnosticsSummary { get; }

    public bool Equivalent => EquivalenceReport.Equivalent;
    public int DivergenceCount => EquivalenceReport.DivergenceCount;

    public CanonicalReadRuntimeDiff(
        CanonicalReadRuntimeDivergence[] divergences,
        CanonicalReadRuntimeEquivalenceReport equivalenceReport,
        string legacySnapshotSummary,
        string canonicalSnapshotSummary,
        string diagnosticsSummary)
    {
        Divergences = divergences ?? Array.Empty<CanonicalReadRuntimeDivergence>();
        EquivalenceReport = equivalenceReport;
        LegacySnapshotSummary = legacySnapshotSummary;
        CanonicalSnapshotSummary = canonicalSnapshotSummary;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public static CanonicalReadRuntimeDiff Compare(
        CanonicalReadSnapshot legacy,
        CanonicalReadSnapshot canonical)
    {
        var divergences = new List<CanonicalReadRuntimeDivergence>();
        CompareRecordingMetadata(legacy.RecordingMetadata, canonical.RecordingMetadata, divergences);
        MapLibraryDiff(legacy.LibraryMetadata.Snapshot, canonical.LibraryMetadata.Snapshot, divergences);
        MapArtifactDiff(legacy.ArtifactMetadata.Snapshot, canonical.ArtifactMetadata.Snapshot, divergences);
        MapConflictDiff(legacy.ConflictState.Snapshot, canonical.ConflictState.Snapshot, divergences);
        CompareUploadState(legacy.UploadState, canonical.UploadState, divergences);
        CompareSyncStatus(legacy.SyncStatus, canonical.SyncStatus, divergences);

        if (legacy.PathOrContentLeakRisk || canonical.PathOrContentLeakRisk)
            divergences.Add(new CanonicalReadRuntimeDivergence(
                kind: CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                domain: CanonicalReadDomain.syncEngineStatus,
                field: "snapshotRedaction",
                legacyValue: $"legacyLeakRisk={legacy.PathOrContentLeakRisk}",
                canonicalValue: $"canonicalLeakRisk={canonical.PathOrContentLeakRisk}",
                fatal: true));

        var uniqueDivergences = divergences
            .GroupBy(d => d.Id)
            .Select(g => g.First())
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .ToArray();

        var equivalent = uniqueDivergences.Length == 0;
        var domains = Enum.GetValues<CanonicalReadDomain>();
        var kindSummary = string.Join("+",
            uniqueDivergences.Select(d => d.Kind.ToString()).Distinct().OrderBy(k => k, StringComparer.Ordinal));

        var report = new CanonicalReadRuntimeEquivalenceReport(
            equivalent: equivalent,
            divergenceCount: uniqueDivergences.Length,
            fatalDivergenceCount: uniqueDivergences.Count(d => d.Fatal),
            domainsCompared: domains,
            diagnosticsSummary:
            $"equivalent={equivalent},divergences={uniqueDivergences.Length},fatal={uniqueDivergences.Count(d => d.Fatal)},kinds={kindSummary}");

        return new CanonicalReadRuntimeDiff(
            divergences: uniqueDivergences,
            equivalenceReport: report,
            legacySnapshotSummary: legacy.DiagnosticsSummary,
            canonicalSnapshotSummary: canonical.DiagnosticsSummary,
            diagnosticsSummary:
            $"domains={string.Join("+", domains.Select(d => d.ToString()))},{report.DiagnosticsSummary}");
    }

    private static void CompareRecordingMetadata(
        CanonicalRecordingReadProjection legacy,
        CanonicalRecordingReadProjection canonical,
        List<CanonicalReadRuntimeDivergence> divergences)
    {
        AppendProjectionFailures(legacy.Failures.Concat(canonical.Failures).ToArray(), divergences);
        var legacyByID = legacy.Records.ToDictionary(r => r.ObjectID, r => r);
        var canonicalByID = canonical.Records.ToDictionary(r => r.ObjectID, r => r);
        var allIDs = legacyByID.Keys.Concat(canonicalByID.Keys).Distinct().OrderBy(id => id, StringComparer.Ordinal);
        foreach (var objectID in allIDs)
        {
            if (!legacyByID.TryGetValue(objectID, out var legacyRecord))
            {
                divergences.Add(new CanonicalReadRuntimeDivergence(
                    CanonicalReadRuntimeDivergenceKind.missingObject, CanonicalReadDomain.recordingMetadata,
                    objectID: objectID, canonicalValue: "present"));
                continue;
            }
            if (!canonicalByID.TryGetValue(objectID, out var canonicalRecord))
            {
                divergences.Add(new CanonicalReadRuntimeDivergence(
                    CanonicalReadRuntimeDivergenceKind.missingObject, CanonicalReadDomain.recordingMetadata,
                    objectID: objectID, legacyValue: "present"));
                continue;
            }
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.titleTagsFolderMismatch,
                CanonicalReadDomain.recordingMetadata, objectID, "title",
                legacyRecord.Title, canonicalRecord.Title, divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.titleTagsFolderMismatch,
                CanonicalReadDomain.recordingMetadata, objectID, "tags",
                legacyRecord.TagsKey, canonicalRecord.TagsKey, divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.titleTagsFolderMismatch,
                CanonicalReadDomain.recordingMetadata, objectID, "filing",
                legacyRecord.FilingSummary, canonicalRecord.FilingSummary, divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.metadataMismatch,
                CanonicalReadDomain.recordingMetadata, objectID, "deleted",
                legacyRecord.IsDeleted.ToString(), canonicalRecord.IsDeleted.ToString(), divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.metadataMismatch,
                CanonicalReadDomain.recordingMetadata, objectID, "metadataHashPrefix",
                legacyRecord.MetadataHashPrefix ?? "nil", canonicalRecord.MetadataHashPrefix ?? "nil", divergences);
        }
    }

    private static void AppendProjectionFailures(
        CanonicalReadProjectionFailure[] failures,
        List<CanonicalReadRuntimeDivergence> divergences)
    {
        foreach (var failure in failures)
        {
            switch (failure.Kind)
            {
                case CanonicalReadProjectionFailureKind.snapshotMissing:
                    divergences.Add(new CanonicalReadRuntimeDivergence(
                        CanonicalReadRuntimeDivergenceKind.missingObject, failure.Domain,
                        objectID: failure.ObjectID, field: "snapshot", canonicalValue: failure.Reason));
                    break;
                case CanonicalReadProjectionFailureKind.unsupportedObject:
                    divergences.Add(new CanonicalReadRuntimeDivergence(
                        CanonicalReadRuntimeDivergenceKind.unsupportedObject, failure.Domain,
                        objectID: failure.ObjectID, field: "object", canonicalValue: failure.Reason));
                    break;
                case CanonicalReadProjectionFailureKind.pathContentLeakRisk:
                    divergences.Add(new CanonicalReadRuntimeDivergence(
                        CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk, failure.Domain,
                        objectID: failure.ObjectID, field: "projection", canonicalValue: failure.Reason, fatal: true));
                    break;
            }
        }
    }

    private static void MapLibraryDiff(
        CanonicalLibraryMetadataReadSnapshot legacy,
        CanonicalLibraryMetadataReadSnapshot canonical,
        List<CanonicalReadRuntimeDivergence> divergences)
    {
        var report = CanonicalLibraryMetadataReadSideParallelDiff.Compare(legacy, canonical);
        foreach (var divergence in report.Divergences.Where(d => d.IsBlocking))
        {
            var kind = divergence.Kind switch
            {
                CanonicalLibraryMetadataReadDiffKind.missingInCanonical => CanonicalReadRuntimeDivergenceKind.missingObject,
                CanonicalLibraryMetadataReadDiffKind.missingInLegacy => CanonicalReadRuntimeDivergenceKind.missingObject,
                CanonicalLibraryMetadataReadDiffKind.titleMismatch => CanonicalReadRuntimeDivergenceKind.titleTagsFolderMismatch,
                CanonicalLibraryMetadataReadDiffKind.parentMismatch => CanonicalReadRuntimeDivergenceKind.titleTagsFolderMismatch,
                CanonicalLibraryMetadataReadDiffKind.folderMembershipMismatch => CanonicalReadRuntimeDivergenceKind.titleTagsFolderMismatch,
                CanonicalLibraryMetadataReadDiffKind.filingMismatch => CanonicalReadRuntimeDivergenceKind.titleTagsFolderMismatch,
                CanonicalLibraryMetadataReadDiffKind.tagsMismatch => CanonicalReadRuntimeDivergenceKind.titleTagsFolderMismatch,
                CanonicalLibraryMetadataReadDiffKind.unsupportedLegacyObject => CanonicalReadRuntimeDivergenceKind.unsupportedObject,
                CanonicalLibraryMetadataReadDiffKind.unsupportedCanonicalObject => CanonicalReadRuntimeDivergenceKind.unsupportedObject,
                CanonicalLibraryMetadataReadDiffKind.pathLeakRisk => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                _ => CanonicalReadRuntimeDivergenceKind.metadataMismatch
            };
            divergences.Add(new CanonicalReadRuntimeDivergence(
                kind: kind,
                domain: CanonicalReadDomain.libraryMetadata,
                objectID: divergence.ObjectID,
                field: divergence.Field,
                legacyValue: divergence.LegacyValue,
                canonicalValue: divergence.CanonicalValue,
                fatal: divergence.Fatal));
        }
    }

    private static void MapArtifactDiff(
        CanonicalGeneratedArtifactReadSnapshot legacy,
        CanonicalGeneratedArtifactReadSnapshot canonical,
        List<CanonicalReadRuntimeDivergence> divergences)
    {
        var report = CanonicalGeneratedArtifactReadSideParallelDiff.Compare(legacy, canonical);
        foreach (var divergence in report.Divergences)
        {
            var kind = divergence.Kind switch
            {
                CanonicalGeneratedArtifactReadDiffKind.missingCanonical => CanonicalReadRuntimeDivergenceKind.missingObject,
                CanonicalGeneratedArtifactReadDiffKind.missingLegacy => CanonicalReadRuntimeDivergenceKind.missingObject,
                CanonicalGeneratedArtifactReadDiffKind.availabilityMismatch => CanonicalReadRuntimeDivergenceKind.artifactAvailabilityMismatch,
                CanonicalGeneratedArtifactReadDiffKind.byteSizeMismatch => CanonicalReadRuntimeDivergenceKind.artifactAvailabilityMismatch,
                CanonicalGeneratedArtifactReadDiffKind.hashPrefixMismatch => CanonicalReadRuntimeDivergenceKind.artifactAvailabilityMismatch,
                CanonicalGeneratedArtifactReadDiffKind.producerMismatch => CanonicalReadRuntimeDivergenceKind.artifactAvailabilityMismatch,
                CanonicalGeneratedArtifactReadDiffKind.artifactKindMismatch => CanonicalReadRuntimeDivergenceKind.artifactAvailabilityMismatch,
                CanonicalGeneratedArtifactReadDiffKind.localDownloadedStateMismatch => CanonicalReadRuntimeDivergenceKind.artifactAvailabilityMismatch,
                CanonicalGeneratedArtifactReadDiffKind.peerAuthoritativeStateMismatch => CanonicalReadRuntimeDivergenceKind.artifactAvailabilityMismatch,
                CanonicalGeneratedArtifactReadDiffKind.parentStateMismatch => CanonicalReadRuntimeDivergenceKind.artifactAvailabilityMismatch,
                CanonicalGeneratedArtifactReadDiffKind.unsafePathToken => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                CanonicalGeneratedArtifactReadDiffKind.contentLeakRisk => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                CanonicalGeneratedArtifactReadDiffKind.unsupportedArtifactKind => CanonicalReadRuntimeDivergenceKind.unsupportedObject,
                CanonicalGeneratedArtifactReadDiffKind.audioConfusionRisk => CanonicalReadRuntimeDivergenceKind.unsupportedObject,
                CanonicalGeneratedArtifactReadDiffKind.tombstonedParentResurrectionRisk => CanonicalReadRuntimeDivergenceKind.unsupportedObject,
                _ => CanonicalReadRuntimeDivergenceKind.metadataMismatch
            };
            divergences.Add(new CanonicalReadRuntimeDivergence(
                kind: kind,
                domain: CanonicalReadDomain.generatedArtifacts,
                objectID: divergence.ObjectID,
                field: divergence.ArtifactKind,
                legacyValue: divergence.LegacyValue,
                canonicalValue: divergence.CanonicalValue,
                fatal: divergence.Fatal));
        }
    }

    private static void MapConflictDiff(
        CanonicalTombstoneConflictReadSnapshot legacy,
        CanonicalTombstoneConflictReadSnapshot canonical,
        List<CanonicalReadRuntimeDivergence> divergences)
    {
        var report = CanonicalTombstoneConflictReadSideParallelDiff.Compare(legacy, canonical);
        foreach (var divergence in report.Divergences)
        {
            var kind = divergence.Kind switch
            {
                CanonicalTombstoneConflictReadDiffKind.missingInCanonical => CanonicalReadRuntimeDivergenceKind.missingObject,
                CanonicalTombstoneConflictReadDiffKind.missingInLegacy => CanonicalReadRuntimeDivergenceKind.missingObject,
                CanonicalTombstoneConflictReadDiffKind.unsupportedObjectKind => CanonicalReadRuntimeDivergenceKind.unsupportedObject,
                CanonicalTombstoneConflictReadDiffKind.pathLeakRisk => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                CanonicalTombstoneConflictReadDiffKind.physicalDeleteRisk => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                CanonicalTombstoneConflictReadDiffKind.permanentDeleteRisk => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                CanonicalTombstoneConflictReadDiffKind.tombstoneGCRisk => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                CanonicalTombstoneConflictReadDiffKind.autoConflictResolutionRisk => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                CanonicalTombstoneConflictReadDiffKind.staleLiveResurrectionRisk => CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk,
                _ => CanonicalReadRuntimeDivergenceKind.tombstoneConflictMismatch
            };
            divergences.Add(new CanonicalReadRuntimeDivergence(
                kind: kind,
                domain: CanonicalReadDomain.tombstoneConflict,
                objectID: divergence.ObjectID,
                field: divergence.Field,
                legacyValue: divergence.LegacyValue,
                canonicalValue: divergence.CanonicalValue,
                fatal: divergence.Fatal));
        }
    }

    private static void CompareUploadState(
        CanonicalUploadReadProjection legacy,
        CanonicalUploadReadProjection canonical,
        List<CanonicalReadRuntimeDivergence> divergences)
    {
        AppendProjectionFailures(legacy.Failures.Concat(canonical.Failures).ToArray(), divergences);
        var legacyByID = legacy.Records.ToDictionary(r => r.ObjectID, r => r);
        var canonicalByID = canonical.Records.ToDictionary(r => r.ObjectID, r => r);
        var allIDs = legacyByID.Keys.Concat(canonicalByID.Keys).Distinct().OrderBy(id => id, StringComparer.Ordinal);
        foreach (var objectID in allIDs)
        {
            if (!legacyByID.TryGetValue(objectID, out var legacyRecord))
            {
                divergences.Add(new CanonicalReadRuntimeDivergence(
                    CanonicalReadRuntimeDivergenceKind.missingObject, CanonicalReadDomain.audioUploadStatus,
                    objectID: objectID, canonicalValue: "present"));
                continue;
            }
            if (!canonicalByID.TryGetValue(objectID, out var canonicalRecord))
            {
                divergences.Add(new CanonicalReadRuntimeDivergence(
                    CanonicalReadRuntimeDivergenceKind.missingObject, CanonicalReadDomain.audioUploadStatus,
                    objectID: objectID, legacyValue: "present"));
                continue;
            }
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.audioAvailabilityMismatch,
                CanonicalReadDomain.audioUploadStatus, objectID, "audioAvailable",
                legacyRecord.AudioAvailable.ToString(), canonicalRecord.AudioAvailable.ToString(), divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.audioAvailabilityMismatch,
                CanonicalReadDomain.audioUploadStatus, objectID, "audioAvailability",
                legacyRecord.AudioAvailability.ToString(), canonicalRecord.AudioAvailability.ToString(), divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.audioAvailabilityMismatch,
                CanonicalReadDomain.audioUploadStatus, objectID, "byteSize",
                legacyRecord.ByteSize?.ToString() ?? "nil", canonicalRecord.ByteSize?.ToString() ?? "nil", divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.audioAvailabilityMismatch,
                CanonicalReadDomain.audioUploadStatus, objectID, "audioHashPrefix",
                legacyRecord.AudioHashPrefix ?? "nil", canonicalRecord.AudioHashPrefix ?? "nil", divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.uploadStatusMismatch,
                CanonicalReadDomain.audioUploadStatus, objectID, "uploadAction",
                legacyRecord.UploadAction?.ToString() ?? "nil", canonicalRecord.UploadAction?.ToString() ?? "nil", divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.uploadStatusMismatch,
                CanonicalReadDomain.audioUploadStatus, objectID, "uploadEvidenceStatus",
                legacyRecord.UploadEvidenceStatus?.ToString() ?? "nil", canonicalRecord.UploadEvidenceStatus?.ToString() ?? "nil", divergences);
            AppendMismatch(CanonicalReadRuntimeDivergenceKind.uploadStatusMismatch,
                CanonicalReadDomain.audioUploadStatus, objectID, "ledgerPhase",
                legacyRecord.UploadLedgerPhase?.ToString() ?? "nil", canonicalRecord.UploadLedgerPhase?.ToString() ?? "nil", divergences);
        }
    }

    private static void CompareSyncStatus(
        CanonicalSyncEngineStatusReadProjection legacy,
        CanonicalSyncEngineStatusReadProjection canonical,
        List<CanonicalReadRuntimeDivergence> divergences)
    {
        AppendMismatch(CanonicalReadRuntimeDivergenceKind.metadataMismatch,
            CanonicalReadDomain.syncEngineStatus, "sync-engine", "pending",
            legacy.PendingTransferCount.ToString(), canonical.PendingTransferCount.ToString(), divergences);
        AppendMismatch(CanonicalReadRuntimeDivergenceKind.metadataMismatch,
            CanonicalReadDomain.syncEngineStatus, "sync-engine", "inFlight",
            legacy.InFlightTransferCount.ToString(), canonical.InFlightTransferCount.ToString(), divergences);
        AppendMismatch(CanonicalReadRuntimeDivergenceKind.metadataMismatch,
            CanonicalReadDomain.syncEngineStatus, "sync-engine", "failed",
            legacy.FailedTransferCount.ToString(), canonical.FailedTransferCount.ToString(), divergences);
    }

    private static void AppendMismatch(
        CanonicalReadRuntimeDivergenceKind kind,
        CanonicalReadDomain domain,
        string objectID,
        string field,
        string legacyValue,
        string canonicalValue,
        List<CanonicalReadRuntimeDivergence> divergences)
    {
        if (legacyValue == canonicalValue) return;
        divergences.Add(new CanonicalReadRuntimeDivergence(
            kind: kind,
            domain: domain,
            objectID: objectID,
            field: field,
            legacyValue: legacyValue,
            canonicalValue: canonicalValue));
    }

    public override bool Equals(object? obj) => obj is CanonicalReadRuntimeDiff other && Equals(other);
    public bool Equals(CanonicalReadRuntimeDiff? other) =>
        other is not null && EquivalenceReport.Equals(other.EquivalenceReport);
    public override int GetHashCode() => EquivalenceReport.GetHashCode();
    public static bool operator ==(CanonicalReadRuntimeDiff left, CanonicalReadRuntimeDiff right) => left.Equals(right);
    public static bool operator !=(CanonicalReadRuntimeDiff left, CanonicalReadRuntimeDiff right) => !left.Equals(right);
}

// ─── Gate Types ─────────────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalReadRuntimeGateBlocker
{
    blockedMode,
    canonicalSnapshotMissing,
    applyRuntimeEvidenceMissing,
    uploadRuntimeEvidenceMissing,
    inventorySnapshotMissing,
    planAuthorityEvidenceMissing,
    existenceTruthEvidenceMissing,
    divergencePresent,
    legacyFallbackUnavailable,
    otherDomainConflict,
    releaseDefaultBuild,
    debugInternalApprovalMissing,
    manualOwnerApprovalMissing,
    diagnosticsNotRedacted,
    readMayTriggerSyncUpload,
    readMayMutateStore,
    pathContentLeakRisk
}

public sealed class CanonicalReadRuntimeGateResult : IEquatable<CanonicalReadRuntimeGateResult>
{
    public bool Allowed { get; }
    public CanonicalReadRuntimeGateBlocker[] Blockers { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalReadRuntimeGateResult(CanonicalReadRuntimeGateBlocker[] blockers)
    {
        var uniqueBlockers = (blockers ?? Array.Empty<CanonicalReadRuntimeGateBlocker>())
            .Distinct()
            .OrderBy(b => b.ToString(), StringComparer.Ordinal)
            .ToArray();
        Allowed = uniqueBlockers.Length == 0;
        Blockers = uniqueBlockers;
        DiagnosticsSummary =
            $"allowed={uniqueBlockers.Length == 0},blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))}";
    }

    public override bool Equals(object? obj) => obj is CanonicalReadRuntimeGateResult other && Equals(other);
    public bool Equals(CanonicalReadRuntimeGateResult? other) =>
        other is not null && Allowed == other.Allowed && Blockers.SequenceEqual(other.Blockers);
    public override int GetHashCode() => HashCode.Combine(Allowed, Blockers.Length);
    public static bool operator ==(CanonicalReadRuntimeGateResult left, CanonicalReadRuntimeGateResult right) => left.Equals(right);
    public static bool operator !=(CanonicalReadRuntimeGateResult left, CanonicalReadRuntimeGateResult right) => !left.Equals(right);
}

public static class CanonicalReadRuntimeGate
{
    public static CanonicalReadRuntimeGateResult Evaluate(
        CanonicalReadRuntimeConfiguration configuration,
        bool canonicalSnapshotAvailable,
        CanonicalReadRuntimeDiff? diff)
    {
        var policy = configuration.Policy;
        var blockers = new List<CanonicalReadRuntimeGateBlocker>();
        if (configuration.Mode == CanonicalReadRuntimeMode.blocked)
            blockers.Add(CanonicalReadRuntimeGateBlocker.blockedMode);
        if (!canonicalSnapshotAvailable)
            blockers.Add(CanonicalReadRuntimeGateBlocker.canonicalSnapshotMissing);
        if (!policy.ApplyRuntimeEvidenceValidForNonAudio)
            blockers.Add(CanonicalReadRuntimeGateBlocker.applyRuntimeEvidenceMissing);
        if (!policy.UploadRuntimeEvidenceValidForAudioStatus)
            blockers.Add(CanonicalReadRuntimeGateBlocker.uploadRuntimeEvidenceMissing);
        if (!policy.InventorySnapshotAvailable)
            blockers.Add(CanonicalReadRuntimeGateBlocker.inventorySnapshotMissing);
        if (!policy.PlanAuthorityEvidenceValid)
            blockers.Add(CanonicalReadRuntimeGateBlocker.planAuthorityEvidenceMissing);
        if (!policy.ExistenceTruthEvidenceValid)
            blockers.Add(CanonicalReadRuntimeGateBlocker.existenceTruthEvidenceMissing);
        if (!policy.LegacyFallbackAvailable)
            blockers.Add(CanonicalReadRuntimeGateBlocker.legacyFallbackUnavailable);
        if (!policy.OtherDomainsNotConflicting)
            blockers.Add(CanonicalReadRuntimeGateBlocker.otherDomainConflict);
        if (policy.ReleaseDefaultBuild)
            blockers.Add(CanonicalReadRuntimeGateBlocker.releaseDefaultBuild);
        if (!policy.DebugInternalBuild || !policy.OwnerApproved)
            blockers.Add(CanonicalReadRuntimeGateBlocker.debugInternalApprovalMissing);
        if (!policy.ManualOwnerApproval)
            blockers.Add(CanonicalReadRuntimeGateBlocker.manualOwnerApprovalMissing);
        if (!policy.DiagnosticsRedacted)
            blockers.Add(CanonicalReadRuntimeGateBlocker.diagnosticsNotRedacted);
        if (!policy.ReadMustNotTriggerSyncUpload)
            blockers.Add(CanonicalReadRuntimeGateBlocker.readMayTriggerSyncUpload);
        if (!policy.ReadMustNotMutateStore)
            blockers.Add(CanonicalReadRuntimeGateBlocker.readMayMutateStore);
        if (diff != null)
        {
            if (diff.DivergenceCount > 0 && !policy.AllowDivergentGuardedReadForTests)
                blockers.Add(CanonicalReadRuntimeGateBlocker.divergencePresent);
            if (diff.Divergences.Any(d => d.Kind == CanonicalReadRuntimeDivergenceKind.pathContentLeakRisk))
                blockers.Add(CanonicalReadRuntimeGateBlocker.pathContentLeakRisk);
        }
        return new CanonicalReadRuntimeGateResult(blockers.ToArray());
    }
}

// ─── Diagnostic Types ───────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalReadRuntimeDiagnosticKind
{
    canonicalReadRuntimeModeEvaluated,
    canonicalReadRuntimeServedCanonical,
    canonicalReadRuntimeServedLegacyFallback,
    canonicalReadRuntimeDiffEquivalent,
    canonicalReadRuntimeDiffDivergent,
    canonicalReadRuntimeBlocked,
    canonicalReadRuntimeReportBuilt
}

public sealed class CanonicalReadRuntimeDiagnostic : IEquatable<CanonicalReadRuntimeDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), SyncRunID ?? "", Mode.ToString(), Detail ?? "");
    public CanonicalReadRuntimeDiagnosticKind Kind { get; }
    public string? SyncRunID { get; }
    public CanonicalReadRuntimeMode Mode { get; }
    public CanonicalReadProjectionSource? Source { get; }
    public int? Count { get; }
    public string? Detail { get; }

    public CanonicalReadRuntimeDiagnostic(
        CanonicalReadRuntimeDiagnosticKind kind,
        string? syncRunID,
        CanonicalReadRuntimeMode mode,
        CanonicalReadProjectionSource? source = null,
        int? count = null,
        string? detail = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null ? CanonicalReadRuntimeRedaction.SafeText(syncRunID) : null;
        Mode = mode;
        Source = source;
        Count = count;
        Detail = detail != null ? CanonicalReadRuntimeRedaction.SafeText(detail) : null;
    }

    public bool IsRedacted =>
        new[] { SyncRunID, Detail }
            .Where(v => v != null)
            .All(v => !CanonicalReadRuntimeRedaction.ContainsForbiddenSignal(v!));

    public string DiagnosticsSummary
    {
        get
        {
            var parts = new List<string> { $"kind={Kind}", $"mode={Mode}" };
            if (Source.HasValue) parts.Add($"source={Source}");
            if (Count.HasValue) parts.Add($"count={Count}");
            if (Detail != null) parts.Add($"detail={Detail}");
            return string.Join(",", parts);
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalReadRuntimeDiagnostic other && Equals(other);
    public bool Equals(CanonicalReadRuntimeDiagnostic? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalReadRuntimeDiagnostic left, CanonicalReadRuntimeDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalReadRuntimeDiagnostic left, CanonicalReadRuntimeDiagnostic right) => !left.Equals(right);
}

// ─── Fallback & Result ──────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalReadRuntimeFallback
{
    none,
    legacyDefault,
    parallelCompareReturnsLegacy,
    canonicalCandidateNotServed,
    guardedGateBlocked,
    canonicalProjectionMissing,
    canonicalReadException,
    blockedMode
}

public sealed class CanonicalReadRuntimeResult : IEquatable<CanonicalReadRuntimeResult>
{
    public CanonicalReadRuntimeMode Mode { get; }
    public CanonicalReadProjectionSource ReturnedSource { get; }
    public CanonicalReadSnapshot ReadSnapshot { get; }
    public CanonicalReadSnapshot LegacySnapshot { get; }
    public CanonicalReadSnapshot? CanonicalCandidate { get; }
    public CanonicalReadRuntimeDiff? Diff { get; }
    public CanonicalReadRuntimeGateResult? GateResult { get; }
    public CanonicalReadRuntimeFallback Fallback { get; }
    public bool CanonicalReadServed { get; }
    public bool LegacyFallbackServed { get; }
    public bool CanonicalCandidateBuilt { get; }
    public bool StoreMutated { get; }
    public bool SyncOrUploadTriggered { get; }
    public bool UploadJobCreated { get; }
    public bool ResourceMoved { get; }
    public bool ProductionDataWritten { get; }
    public CanonicalReadRuntimeDiagnostic[] Diagnostics { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalReadRuntimeResult(
        CanonicalReadRuntimeMode mode,
        CanonicalReadProjectionSource returnedSource,
        CanonicalReadSnapshot readSnapshot,
        CanonicalReadSnapshot legacySnapshot,
        CanonicalReadSnapshot? canonicalCandidate,
        CanonicalReadRuntimeDiff? diff,
        CanonicalReadRuntimeGateResult? gateResult,
        CanonicalReadRuntimeFallback fallback,
        bool canonicalReadServed,
        bool legacyFallbackServed,
        bool canonicalCandidateBuilt,
        bool storeMutated,
        bool syncOrUploadTriggered,
        bool uploadJobCreated,
        bool resourceMoved,
        bool productionDataWritten,
        CanonicalReadRuntimeDiagnostic[] diagnostics,
        string diagnosticsSummary)
    {
        Mode = mode;
        ReturnedSource = returnedSource;
        ReadSnapshot = readSnapshot;
        LegacySnapshot = legacySnapshot;
        CanonicalCandidate = canonicalCandidate;
        Diff = diff;
        GateResult = gateResult;
        Fallback = fallback;
        CanonicalReadServed = canonicalReadServed;
        LegacyFallbackServed = legacyFallbackServed;
        CanonicalCandidateBuilt = canonicalCandidateBuilt;
        StoreMutated = storeMutated;
        SyncOrUploadTriggered = syncOrUploadTriggered;
        UploadJobCreated = uploadJobCreated;
        ResourceMoved = resourceMoved;
        ProductionDataWritten = productionDataWritten;
        Diagnostics = diagnostics ?? Array.Empty<CanonicalReadRuntimeDiagnostic>();
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalReadRuntimeResult other && Equals(other);
    public bool Equals(CanonicalReadRuntimeResult? other) =>
        other is not null &&
        Mode == other.Mode &&
        ReturnedSource == other.ReturnedSource &&
        Fallback == other.Fallback;
    public override int GetHashCode() => HashCode.Combine(Mode, ReturnedSource, Fallback);
    public static bool operator ==(CanonicalReadRuntimeResult left, CanonicalReadRuntimeResult right) => left.Equals(right);
    public static bool operator !=(CanonicalReadRuntimeResult left, CanonicalReadRuntimeResult right) => !left.Equals(right);
}

// ─── Read Runtime Provider ──────────────────────────────────────────────────

public sealed class CanonicalReadRuntimeProvider
{
    public CanonicalReadRuntimeConfiguration Configuration { get; }

    public CanonicalReadRuntimeProvider(
        CanonicalReadRuntimeConfiguration? configuration = null)
    {
        Configuration = configuration ?? CanonicalReadRuntimeConfiguration.Disabled;
    }

    public CanonicalReadRuntimeResult Read(
        CanonicalReadSnapshot legacySnapshot,
        CanonicalReadSnapshot? canonicalSnapshot,
        string? syncRunID = null,
        string? canonicalReadFailureReason = null)
    {
        var mode = Configuration.Mode;
        var evaluated = Diagnostic(
            CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeModeEvaluated,
            syncRunID, source: null, count: null,
            detail: $"mode={mode}");

        switch (mode)
        {
            case CanonicalReadRuntimeMode.disabled:
                return MakeResult(
                    returnedSnapshot: legacySnapshot,
                    legacySnapshot: legacySnapshot,
                    canonicalSnapshot: null,
                    diff: null,
                    gate: null,
                    fallback: CanonicalReadRuntimeFallback.legacyDefault,
                    diagnostics: new[]
                    {
                        evaluated,
                        Diagnostic(CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeServedLegacyFallback,
                            syncRunID, CanonicalReadProjectionSource.legacy, detail: "disabledDefaultLegacy")
                    });

            case CanonicalReadRuntimeMode.blocked:
            {
                var blockedDiff = canonicalSnapshot != null
                    ? CanonicalReadRuntimeDiff.Compare(legacySnapshot, canonicalSnapshot)
                    : null;
                return MakeResult(
                    returnedSnapshot: legacySnapshot,
                    legacySnapshot: legacySnapshot,
                    canonicalSnapshot: canonicalSnapshot,
                    diff: blockedDiff,
                    gate: new CanonicalReadRuntimeGateResult(
                        new[] { CanonicalReadRuntimeGateBlocker.blockedMode }),
                    fallback: CanonicalReadRuntimeFallback.blockedMode,
                    diagnostics: new[]
                    {
                        evaluated,
                        Diagnostic(CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeBlocked,
                            syncRunID, CanonicalReadProjectionSource.legacy, count: 1, detail: "blockedMode"),
                        Diagnostic(CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeServedLegacyFallback,
                            syncRunID, CanonicalReadProjectionSource.legacy, detail: "blockedMode")
                    }.Concat(DiffDiagnostics(blockedDiff, syncRunID)).ToArray());
            }

            case CanonicalReadRuntimeMode.parallelCompare:
            case CanonicalReadRuntimeMode.canonicalReadCandidate:
            {
                var compareDiff = canonicalSnapshot != null
                    ? CanonicalReadRuntimeDiff.Compare(legacySnapshot, canonicalSnapshot)
                    : null;
                var fallback = mode == CanonicalReadRuntimeMode.parallelCompare
                    ? CanonicalReadRuntimeFallback.parallelCompareReturnsLegacy
                    : CanonicalReadRuntimeFallback.canonicalCandidateNotServed;
                var reason = mode == CanonicalReadRuntimeMode.parallelCompare
                    ? "parallelCompareReturnsLegacy"
                    : "canonicalCandidateNotServed";
                return MakeResult(
                    returnedSnapshot: legacySnapshot,
                    legacySnapshot: legacySnapshot,
                    canonicalSnapshot: canonicalSnapshot,
                    diff: compareDiff,
                    gate: null,
                    fallback: fallback,
                    diagnostics: new[]
                    {
                        evaluated,
                        Diagnostic(CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeServedLegacyFallback,
                            syncRunID, CanonicalReadProjectionSource.legacy, detail: reason)
                    }.Concat(DiffDiagnostics(compareDiff, syncRunID)).ToArray());
            }

            case CanonicalReadRuntimeMode.guardedCanonicalReadWithLegacyFallback:
            {
                if (canonicalSnapshot == null)
                    return MakeResult(
                        returnedSnapshot: legacySnapshot,
                        legacySnapshot: legacySnapshot,
                        canonicalSnapshot: null,
                        diff: null,
                        gate: CanonicalReadRuntimeGate.Evaluate(Configuration,
                            canonicalSnapshotAvailable: false, diff: null),
                        fallback: CanonicalReadRuntimeFallback.canonicalProjectionMissing,
                        diagnostics: new[]
                        {
                            evaluated,
                            Diagnostic(CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeBlocked,
                                syncRunID, CanonicalReadProjectionSource.legacy, count: 1,
                                detail: "canonicalProjectionMissing"),
                            Diagnostic(CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeServedLegacyFallback,
                                syncRunID, CanonicalReadProjectionSource.legacy,
                                detail: "canonicalProjectionMissing")
                        });

                var guardDiff = CanonicalReadRuntimeDiff.Compare(legacySnapshot, canonicalSnapshot);
                var guardGate = CanonicalReadRuntimeGate.Evaluate(Configuration,
                    canonicalSnapshotAvailable: true, diff: guardDiff);
                var guardDiagnostics = new List<CanonicalReadRuntimeDiagnostic> { evaluated };
                guardDiagnostics.AddRange(DiffDiagnostics(guardDiff, syncRunID));

                if (canonicalReadFailureReason != null)
                {
                    guardDiagnostics.Add(Diagnostic(
                        CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeServedLegacyFallback,
                        syncRunID, CanonicalReadProjectionSource.legacy,
                        detail: canonicalReadFailureReason));
                    return MakeResult(
                        returnedSnapshot: legacySnapshot,
                        legacySnapshot: legacySnapshot,
                        canonicalSnapshot: canonicalSnapshot,
                        diff: guardDiff,
                        gate: guardGate,
                        fallback: CanonicalReadRuntimeFallback.canonicalReadException,
                        diagnostics: guardDiagnostics.ToArray());
                }

                if (!guardGate.Allowed)
                {
                    guardDiagnostics.Add(Diagnostic(
                        CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeBlocked,
                        syncRunID, CanonicalReadProjectionSource.legacy,
                        count: guardGate.Blockers.Length,
                        detail: string.Join("+", guardGate.Blockers.Select(b => b.ToString()))));
                    guardDiagnostics.Add(Diagnostic(
                        CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeServedLegacyFallback,
                        syncRunID, CanonicalReadProjectionSource.legacy,
                        detail: "guardedGateBlocked"));
                    return MakeResult(
                        returnedSnapshot: legacySnapshot,
                        legacySnapshot: legacySnapshot,
                        canonicalSnapshot: canonicalSnapshot,
                        diff: guardDiff,
                        gate: guardGate,
                        fallback: CanonicalReadRuntimeFallback.guardedGateBlocked,
                        diagnostics: guardDiagnostics.ToArray());
                }

                guardDiagnostics.Add(Diagnostic(
                    CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeServedCanonical,
                    syncRunID, CanonicalReadProjectionSource.canonical,
                    detail: "guardedCanonicalReadWithLegacyFallback"));
                return MakeResult(
                    returnedSnapshot: canonicalSnapshot,
                    legacySnapshot: legacySnapshot,
                    canonicalSnapshot: canonicalSnapshot,
                    diff: guardDiff,
                    gate: guardGate,
                    fallback: CanonicalReadRuntimeFallback.none,
                    diagnostics: guardDiagnostics.ToArray());
            }

            default:
                return MakeResult(
                    returnedSnapshot: legacySnapshot,
                    legacySnapshot: legacySnapshot,
                    canonicalSnapshot: null,
                    diff: null,
                    gate: null,
                    fallback: CanonicalReadRuntimeFallback.legacyDefault,
                    diagnostics: new[] { evaluated });
        }
    }

    private CanonicalReadRuntimeResult MakeResult(
        CanonicalReadSnapshot returnedSnapshot,
        CanonicalReadSnapshot legacySnapshot,
        CanonicalReadSnapshot? canonicalSnapshot,
        CanonicalReadRuntimeDiff? diff,
        CanonicalReadRuntimeGateResult? gate,
        CanonicalReadRuntimeFallback fallback,
        CanonicalReadRuntimeDiagnostic[] diagnostics)
    {
        var diagList = new List<CanonicalReadRuntimeDiagnostic>(diagnostics);
        diagList.Add(Diagnostic(
            CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeReportBuilt,
            diagList.FirstOrDefault()?.SyncRunID,
            source: returnedSnapshot.Source,
            count: diff?.DivergenceCount,
            detail: $"fallback={fallback}"));

        var limitedDiagnostics = diagList
            .Take(Configuration.Policy.MaxDiagnosticsEvents)
            .ToArray();

        var canonicalServed = returnedSnapshot.Source == CanonicalReadProjectionSource.canonical
                              && fallback == CanonicalReadRuntimeFallback.none;

        return new CanonicalReadRuntimeResult(
            mode: Configuration.Mode,
            returnedSource: returnedSnapshot.Source,
            readSnapshot: returnedSnapshot,
            legacySnapshot: legacySnapshot,
            canonicalCandidate: canonicalSnapshot,
            diff: diff,
            gateResult: gate,
            fallback: fallback,
            canonicalReadServed: canonicalServed,
            legacyFallbackServed: returnedSnapshot.Source == CanonicalReadProjectionSource.legacy
                                 && fallback != CanonicalReadRuntimeFallback.none,
            canonicalCandidateBuilt: canonicalSnapshot != null
                                     && Configuration.Mode.BuildsCanonicalCandidate(),
            storeMutated: false,
            syncOrUploadTriggered: false,
            uploadJobCreated: false,
            resourceMoved: false,
            productionDataWritten: false,
            diagnostics: limitedDiagnostics,
            diagnosticsSummary: string.Join(",",
                $"mode={Configuration.Mode}",
                $"returned={returnedSnapshot.Source}",
                $"fallback={fallback}",
                $"canonicalServed={canonicalServed}",
                $"canonicalCandidateBuilt={canonicalSnapshot != null && Configuration.Mode.BuildsCanonicalCandidate()}",
                $"divergences={diff?.DivergenceCount ?? 0}",
                "storeMutated=false",
                "syncOrUploadTriggered=false",
                "uploadJobCreated=false",
                "resourceMoved=false",
                "productionDataWritten=false"));
    }

    private CanonicalReadRuntimeDiagnostic[] DiffDiagnostics(
        CanonicalReadRuntimeDiff? diff,
        string? syncRunID)
    {
        if (diff == null) return Array.Empty<CanonicalReadRuntimeDiagnostic>();
        return new[]
        {
            Diagnostic(
                diff.Equivalent
                    ? CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeDiffEquivalent
                    : CanonicalReadRuntimeDiagnosticKind.canonicalReadRuntimeDiffDivergent,
                syncRunID,
                source: CanonicalReadProjectionSource.canonical,
                count: diff.DivergenceCount,
                detail: diff.EquivalenceReport.DiagnosticsSummary)
        };
    }

    private CanonicalReadRuntimeDiagnostic Diagnostic(
        CanonicalReadRuntimeDiagnosticKind kind,
        string? syncRunID,
        CanonicalReadProjectionSource? source,
        int? count = null,
        string? detail = null)
        => new(
            kind: kind,
            syncRunID: syncRunID,
            mode: Configuration.Mode,
            source: source,
            count: count,
            detail: detail);
}

// ─── Forward-reference types for read projection snapshots ─────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadProjectionSource { legacy, canonical }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadProjectionSource { legacy, canonical }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictReadProjectionSource { legacy, canonical }

public sealed class CanonicalLibraryMetadataReadSnapshot : IEquatable<CanonicalLibraryMetadataReadSnapshot>
{
    public int PathLeakRiskCount { get; set; }
    public bool FullContentIncluded { get; set; }
    public CanonicalLibraryMetadataReadProjectionRecord[] Folders { get; set; } = Array.Empty<CanonicalLibraryMetadataReadProjectionRecord>();
    public CanonicalLibraryMetadataReadProjectionRecord[] StudyItems { get; set; } = Array.Empty<CanonicalLibraryMetadataReadProjectionRecord>();

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataReadSnapshot other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataReadSnapshot? other) =>
        other is not null &&
        PathLeakRiskCount == other.PathLeakRiskCount &&
        FullContentIncluded == other.FullContentIncluded;
    public override int GetHashCode() => HashCode.Combine(PathLeakRiskCount, FullContentIncluded);
    public static bool operator ==(CanonicalLibraryMetadataReadSnapshot left, CanonicalLibraryMetadataReadSnapshot right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataReadSnapshot left, CanonicalLibraryMetadataReadSnapshot right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataReadProjectionRecord : IEquatable<CanonicalLibraryMetadataReadProjectionRecord>
{
    public string ObjectID { get; set; } = "";
    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataReadProjectionRecord other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataReadProjectionRecord? other) => other is not null && ObjectID == other.ObjectID;
    public override int GetHashCode() => ObjectID.GetHashCode();
    public static bool operator ==(CanonicalLibraryMetadataReadProjectionRecord left, CanonicalLibraryMetadataReadProjectionRecord right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataReadProjectionRecord left, CanonicalLibraryMetadataReadProjectionRecord right) => !left.Equals(right);
}

public static class CanonicalLibraryMetadataReadProjection
{
    public static CanonicalLibraryReadProjection Build(
        CanonicalLibraryMetadataReadProjectionSource source,
        CanonicalManifest? manifest)
        => new(
            CanonicalReadProjectionSource.legacy,
            new CanonicalLibraryMetadataReadSnapshot
            {
                Folders = manifest?.Folders.Select(f =>
                        new CanonicalLibraryMetadataReadProjectionRecord { ObjectID = f.FolderID.RawValue }).ToArray()
                          ?? Array.Empty<CanonicalLibraryMetadataReadProjectionRecord>(),
                StudyItems = manifest?.StudyItems.Select(s =>
                        new CanonicalLibraryMetadataReadProjectionRecord { ObjectID = s.ItemID.RawValue }).ToArray()
                             ?? Array.Empty<CanonicalLibraryMetadataReadProjectionRecord>()
            });
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataReadDiffKind
{
    missingInCanonical, missingInLegacy,
    titleMismatch, parentMismatch, folderMembershipMismatch, filingMismatch, tagsMismatch,
    unsupportedLegacyObject, unsupportedCanonicalObject, pathLeakRisk
}

public sealed class CanonicalLibraryMetadataReadDiff : IEquatable<CanonicalLibraryMetadataReadDiff>
{
    public CanonicalLibraryMetadataReadDiffKind Kind { get; set; }
    public string? ObjectID { get; set; }
    public string? Field { get; set; }
    public string? LegacyValue { get; set; }
    public string? CanonicalValue { get; set; }
    public bool IsBlocking { get; set; }
    public bool Fatal { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataReadDiff other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataReadDiff? other) =>
        other is not null && Kind == other.Kind && ObjectID == other.ObjectID && Field == other.Field;
    public override int GetHashCode() => HashCode.Combine(Kind, ObjectID, Field);
    public static bool operator ==(CanonicalLibraryMetadataReadDiff left, CanonicalLibraryMetadataReadDiff right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataReadDiff left, CanonicalLibraryMetadataReadDiff right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataReadSideParallelDiffReport
{
    public CanonicalLibraryMetadataReadDiff[] Divergences { get; set; } = Array.Empty<CanonicalLibraryMetadataReadDiff>();
}

public static class CanonicalLibraryMetadataReadSideParallelDiff
{
    public static CanonicalLibraryMetadataReadSideParallelDiffReport Compare(
        CanonicalLibraryMetadataReadSnapshot legacy,
        CanonicalLibraryMetadataReadSnapshot canonical)
        => new() { Divergences = Array.Empty<CanonicalLibraryMetadataReadDiff>() };
}

public sealed class CanonicalGeneratedArtifactReadSnapshot : IEquatable<CanonicalGeneratedArtifactReadSnapshot>
{
    public int ContentIncludedCount { get; set; }
    public int ItemCount { get; set; }
    public CanonicalGeneratedArtifactReadProjectionFailure[] Failures { get; set; } = Array.Empty<CanonicalGeneratedArtifactReadProjectionFailure>();

    public override bool Equals(object? obj) => obj is CanonicalGeneratedArtifactReadSnapshot other && Equals(other);
    public bool Equals(CanonicalGeneratedArtifactReadSnapshot? other) =>
        other is not null && ContentIncludedCount == other.ContentIncludedCount && ItemCount == other.ItemCount;
    public override int GetHashCode() => HashCode.Combine(ContentIncludedCount, ItemCount);
    public static bool operator ==(CanonicalGeneratedArtifactReadSnapshot left, CanonicalGeneratedArtifactReadSnapshot right) => left.Equals(right);
    public static bool operator !=(CanonicalGeneratedArtifactReadSnapshot left, CanonicalGeneratedArtifactReadSnapshot right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadProjectionFailureKind
{
    snapshotMissing, unsafePathToken, contentLeakRisk
}

public sealed class CanonicalGeneratedArtifactReadProjectionFailure : IEquatable<CanonicalGeneratedArtifactReadProjectionFailure>
{
    public CanonicalGeneratedArtifactReadProjectionFailureKind Kind { get; set; }
    public CanonicalGeneratedArtifactReadProjectionSource Source { get; set; }
    public string Reason { get; set; } = "";

    public CanonicalGeneratedArtifactReadProjectionFailure() { }

    public CanonicalGeneratedArtifactReadProjectionFailure(
        CanonicalGeneratedArtifactReadProjectionFailureKind kind,
        CanonicalGeneratedArtifactReadProjectionSource source,
        string reason)
    {
        Kind = kind;
        Source = source;
        Reason = reason;
    }

    public override bool Equals(object? obj) => obj is CanonicalGeneratedArtifactReadProjectionFailure other && Equals(other);
    public bool Equals(CanonicalGeneratedArtifactReadProjectionFailure? other) =>
        other is not null && Kind == other.Kind && Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(Kind, Reason);
    public static bool operator ==(CanonicalGeneratedArtifactReadProjectionFailure left, CanonicalGeneratedArtifactReadProjectionFailure right) => left.Equals(right);
    public static bool operator !=(CanonicalGeneratedArtifactReadProjectionFailure left, CanonicalGeneratedArtifactReadProjectionFailure right) => !left.Equals(right);
}

public sealed class CanonicalGeneratedArtifactReadProjectionArtifactFact : IEquatable<CanonicalGeneratedArtifactReadProjectionArtifactFact>
{
    public string ArtifactID { get; set; } = "";
    public bool ParentTombstoned { get; set; }
    public bool LocalAvailability { get; set; }
    public bool PeerAuthoritativeAvailability { get; set; }
    public string ProducerSummary { get; set; } = "";
    public bool UnsafePathTokenObserved { get; set; }

    public CanonicalGeneratedArtifactReadProjectionArtifactFact() { }

    public CanonicalGeneratedArtifactReadProjectionArtifactFact(
        CanonicalArtifact artifact,
        bool parentTombstoned,
        bool localAvailability,
        bool peerAuthoritativeAvailability,
        string producerSummary,
        bool unsafePathTokenObserved)
    {
        ArtifactID = artifact.ArtifactID;
        ParentTombstoned = parentTombstoned;
        LocalAvailability = localAvailability;
        PeerAuthoritativeAvailability = peerAuthoritativeAvailability;
        ProducerSummary = producerSummary;
        UnsafePathTokenObserved = unsafePathTokenObserved;
    }

    public override bool Equals(object? obj) => obj is CanonicalGeneratedArtifactReadProjectionArtifactFact other && Equals(other);
    public bool Equals(CanonicalGeneratedArtifactReadProjectionArtifactFact? other) =>
        other is not null && ArtifactID == other.ArtifactID;
    public override int GetHashCode() => ArtifactID.GetHashCode();
    public static bool operator ==(CanonicalGeneratedArtifactReadProjectionArtifactFact left, CanonicalGeneratedArtifactReadProjectionArtifactFact right) => left.Equals(right);
    public static bool operator !=(CanonicalGeneratedArtifactReadProjectionArtifactFact left, CanonicalGeneratedArtifactReadProjectionArtifactFact right) => !left.Equals(right);
}

public static class CanonicalGeneratedArtifactReadProjection
{
    public static CanonicalGeneratedArtifactReadSnapshot Snapshot(
        CanonicalGeneratedArtifactReadProjectionSource source,
        CanonicalGeneratedArtifactReadProjectionArtifactFact[] facts,
        CanonicalGeneratedArtifactReadProjectionFailure[] failures)
        => new()
        {
            ItemCount = facts?.Length ?? 0,
            ContentIncludedCount = 0,
            Failures = failures ?? Array.Empty<CanonicalGeneratedArtifactReadProjectionFailure>()
        };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadDiffKind
{
    missingCanonical, missingLegacy,
    availabilityMismatch, byteSizeMismatch, hashPrefixMismatch,
    producerMismatch, artifactKindMismatch,
    localDownloadedStateMismatch, peerAuthoritativeStateMismatch, parentStateMismatch,
    unsafePathToken, contentLeakRisk,
    unsupportedArtifactKind, audioConfusionRisk, tombstonedParentResurrectionRisk
}

public sealed class CanonicalGeneratedArtifactReadDiff : IEquatable<CanonicalGeneratedArtifactReadDiff>
{
    public CanonicalGeneratedArtifactReadDiffKind Kind { get; set; }
    public string? ObjectID { get; set; }
    public string? ArtifactKind { get; set; }
    public string? LegacyValue { get; set; }
    public string? CanonicalValue { get; set; }
    public bool Fatal { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalGeneratedArtifactReadDiff other && Equals(other);
    public bool Equals(CanonicalGeneratedArtifactReadDiff? other) =>
        other is not null && Kind == other.Kind && ObjectID == other.ObjectID;
    public override int GetHashCode() => HashCode.Combine(Kind, ObjectID);
    public static bool operator ==(CanonicalGeneratedArtifactReadDiff left, CanonicalGeneratedArtifactReadDiff right) => left.Equals(right);
    public static bool operator !=(CanonicalGeneratedArtifactReadDiff left, CanonicalGeneratedArtifactReadDiff right) => !left.Equals(right);
}

public sealed class CanonicalGeneratedArtifactReadSideParallelDiffReport
{
    public CanonicalGeneratedArtifactReadDiff[] Divergences { get; set; } = Array.Empty<CanonicalGeneratedArtifactReadDiff>();
}

public static class CanonicalGeneratedArtifactReadSideParallelDiff
{
    public static CanonicalGeneratedArtifactReadSideParallelDiffReport Compare(
        CanonicalGeneratedArtifactReadSnapshot legacy,
        CanonicalGeneratedArtifactReadSnapshot canonical)
        => new() { Divergences = Array.Empty<CanonicalGeneratedArtifactReadDiff>() };
}

public sealed class CanonicalTombstoneConflictReadSnapshot : IEquatable<CanonicalTombstoneConflictReadSnapshot>
{
    public int FullContentIncludedCount { get; set; }
    public int AbsolutePathIncludedCount { get; set; }
    public int PathLeakRiskCount { get; set; }
    public CanonicalTombstoneConflictReadItem[] Items { get; set; } = Array.Empty<CanonicalTombstoneConflictReadItem>();

    public override bool Equals(object? obj) => obj is CanonicalTombstoneConflictReadSnapshot other && Equals(other);
    public bool Equals(CanonicalTombstoneConflictReadSnapshot? other) =>
        other is not null &&
        FullContentIncludedCount == other.FullContentIncludedCount &&
        AbsolutePathIncludedCount == other.AbsolutePathIncludedCount &&
        PathLeakRiskCount == other.PathLeakRiskCount;
    public override int GetHashCode() => HashCode.Combine(FullContentIncludedCount, AbsolutePathIncludedCount, PathLeakRiskCount);
    public static bool operator ==(CanonicalTombstoneConflictReadSnapshot left, CanonicalTombstoneConflictReadSnapshot right) => left.Equals(right);
    public static bool operator !=(CanonicalTombstoneConflictReadSnapshot left, CanonicalTombstoneConflictReadSnapshot right) => !left.Equals(right);
}

public sealed class CanonicalTombstoneConflictReadItem : IEquatable<CanonicalTombstoneConflictReadItem>
{
    public string ConflictStatus { get; set; } = "none";
    public override bool Equals(object? obj) => obj is CanonicalTombstoneConflictReadItem other && Equals(other);
    public bool Equals(CanonicalTombstoneConflictReadItem? other) =>
        other is not null && ConflictStatus == other.ConflictStatus;
    public override int GetHashCode() => ConflictStatus.GetHashCode();
    public static bool operator ==(CanonicalTombstoneConflictReadItem left, CanonicalTombstoneConflictReadItem right) => left.Equals(right);
    public static bool operator !=(CanonicalTombstoneConflictReadItem left, CanonicalTombstoneConflictReadItem right) => !left.Equals(right);
}

public static class CanonicalTombstoneConflictReadProjection
{
    public static CanonicalTombstoneConflictReadSnapshot Snapshot(
        CanonicalTombstoneConflictReadProjectionSource source,
        CanonicalManifest? localManifest,
        CanonicalManifest? peerManifest,
        CanonicalApplyPlan? applyPlan,
        CanonicalLibrarySyncPlan? libraryPlan)
        => new()
        {
            Items = Array.Empty<CanonicalTombstoneConflictReadItem>(),
            FullContentIncludedCount = 0,
            AbsolutePathIncludedCount = 0,
            PathLeakRiskCount = 0
        };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTombstoneConflictReadDiffKind
{
    missingInCanonical, missingInLegacy,
    unsupportedObjectKind,
    pathLeakRisk, physicalDeleteRisk, permanentDeleteRisk,
    tombstoneGCRisk, autoConflictResolutionRisk, staleLiveResurrectionRisk
}

public sealed class CanonicalTombstoneConflictReadDiff : IEquatable<CanonicalTombstoneConflictReadDiff>
{
    public CanonicalTombstoneConflictReadDiffKind Kind { get; set; }
    public string? ObjectID { get; set; }
    public string? Field { get; set; }
    public string? LegacyValue { get; set; }
    public string? CanonicalValue { get; set; }
    public bool Fatal { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalTombstoneConflictReadDiff other && Equals(other);
    public bool Equals(CanonicalTombstoneConflictReadDiff? other) =>
        other is not null && Kind == other.Kind && ObjectID == other.ObjectID;
    public override int GetHashCode() => HashCode.Combine(Kind, ObjectID);
    public static bool operator ==(CanonicalTombstoneConflictReadDiff left, CanonicalTombstoneConflictReadDiff right) => left.Equals(right);
    public static bool operator !=(CanonicalTombstoneConflictReadDiff left, CanonicalTombstoneConflictReadDiff right) => !left.Equals(right);
}

public sealed class CanonicalTombstoneConflictReadSideParallelDiffReport
{
    public CanonicalTombstoneConflictReadDiff[] Divergences { get; set; } = Array.Empty<CanonicalTombstoneConflictReadDiff>();
}

public static class CanonicalTombstoneConflictReadSideParallelDiff
{
    public static CanonicalTombstoneConflictReadSideParallelDiffReport Compare(
        CanonicalTombstoneConflictReadSnapshot legacy,
        CanonicalTombstoneConflictReadSnapshot canonical)
        => new() { Divergences = Array.Empty<CanonicalTombstoneConflictReadDiff>() };
}

// ─── Audio Upload Forward-Reference Types ───────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalAudioUploadPeerState
{
    unknown, missing, metadataOnly, available, different, deleted
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalAudioUploadActionKind
{
    audioUploadNoOp, audioUploadShadowRehearsal, audioUploadCanaryCandidate,
    audioUploadConflictRecord, audioUploadDeferredPeerUnknown, unsupported
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalAudioUploadEvidenceStatus
{
    complete, blocked, conflict, deferred, disabled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalAudioUploadLedgerPhase
{
    none, queued, inFlight, finalizing, completed, failed, retryPending, fatalFailed
}

public sealed class CanonicalAudioUploadPeerTruth
{
    public CanonicalAudioUploadPeerState State { get; set; }
}

public sealed class CanonicalAudioUploadLedgerTruth
{
    public CanonicalAudioUploadLedgerPhase Phase { get; set; }
}

public sealed class CanonicalAudioUploadRetryTruth
{
    public bool HasExistingEligibleRetry { get; set; }
}

public sealed class CanonicalAudioUploadCutoverCandidate
{
    public string ObjectID { get; set; } = "";
    public CanonicalAudioUploadPeerTruth? PeerTruth { get; set; }
    public CanonicalAudioUploadActionKind? ActionKind { get; set; }
    public CanonicalAudioUploadEvidenceStatus? EvidenceStatus { get; set; }
    public CanonicalAudioUploadLedgerTruth? LedgerTruth { get; set; }
    public CanonicalAudioUploadRetryTruth? RetryTruth { get; set; }
}

// ─── Projection Source Extensions ───────────────────────────────────────────

public static class CanonicalReadProjectionSourceExtensions
{
    public static CanonicalLibraryMetadataReadProjectionSource LibraryMetadataSource(
        this CanonicalReadProjectionSource source)
        => source == CanonicalReadProjectionSource.legacy
            ? CanonicalLibraryMetadataReadProjectionSource.legacy
            : CanonicalLibraryMetadataReadProjectionSource.canonical;

    public static CanonicalGeneratedArtifactReadProjectionSource GeneratedArtifactSource(
        this CanonicalReadProjectionSource source)
        => source == CanonicalReadProjectionSource.legacy
            ? CanonicalGeneratedArtifactReadProjectionSource.legacy
            : CanonicalGeneratedArtifactReadProjectionSource.canonical;

    public static CanonicalTombstoneConflictReadProjectionSource TombstoneConflictSource(
        this CanonicalReadProjectionSource source)
        => source == CanonicalReadProjectionSource.legacy
            ? CanonicalTombstoneConflictReadProjectionSource.legacy
            : CanonicalTombstoneConflictReadProjectionSource.canonical;
}

// ─── Read Runtime Redaction Helpers ─────────────────────────────────────────

internal static class CanonicalReadRuntimeRedaction
{
    public static string SafeIdentifier(string value, string fallback)
        => CanonicalProductionRedaction.SafeIdentifier(value, fallback);

    public static string SafeDisplayText(string value, string fallback)
        => CanonicalProductionRedaction.SafeDiagnosticText(value) ?? fallback;

    public static string? SafeText(string? value)
        => value != null ? CanonicalProductionRedaction.SafeDiagnosticText(value) : null;

    public static string? HashPrefix(string? value)
    {
        if (value == null) return null;
        return CanonicalProductionRedaction.HashPrefix(value);
    }

    public static bool ContainsForbiddenSignal(string value)
        => ContainsSensitivePathSignal(value)
           || value.Contains("{")
           || value.Contains("}")
           || value.Contains("://")
           || value.Length > 320;

    private static bool ContainsSensitivePathSignal(string value)
        => CanonicalProductionRedaction.ContainsSensitivePathSignal(value)
           || value.Contains("/")
           || value.Contains("\\");
}
