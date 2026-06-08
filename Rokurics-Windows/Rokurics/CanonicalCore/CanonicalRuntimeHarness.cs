using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRuntimeHarnessNodeRole
{
    iPhone,
    mac
}

public sealed class CanonicalRuntimeHarnessTickResult : IEquatable<CanonicalRuntimeHarnessTickResult>
{
    public CanonicalRuntimeHarnessNodeRole LocalRole { get; }
    public CanonicalRuntimeHarnessNodeRole PeerRole { get; }
    public CanonicalSyncPlan SyncPlan { get; }
    public CanonicalApplyPlan ApplyPlan { get; }
    public CanonicalLibrarySyncPlan LibraryPlan { get; }
    public CanonicalApplyExecutionReport ExecutionReport { get; }
    public CanonicalLibraryProjection Projection { get; }
    public CanonicalRuntimeReadinessReport RuntimeReadiness { get; }

    public CanonicalRuntimeHarnessTickResult(
        CanonicalRuntimeHarnessNodeRole localRole,
        CanonicalRuntimeHarnessNodeRole peerRole,
        CanonicalSyncPlan syncPlan,
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan libraryPlan,
        CanonicalApplyExecutionReport executionReport,
        CanonicalLibraryProjection projection,
        CanonicalRuntimeReadinessReport runtimeReadiness)
    {
        LocalRole = localRole;
        PeerRole = peerRole;
        SyncPlan = syncPlan;
        ApplyPlan = applyPlan;
        LibraryPlan = libraryPlan;
        ExecutionReport = executionReport;
        Projection = projection;
        RuntimeReadiness = runtimeReadiness;
    }

    public override bool Equals(object? obj) =>
        obj is CanonicalRuntimeHarnessTickResult other && Equals(other);
    public bool Equals(CanonicalRuntimeHarnessTickResult? other) =>
        other is not null &&
        LocalRole == other.LocalRole &&
        PeerRole == other.PeerRole;
    public override int GetHashCode() => HashCode.Combine(LocalRole, PeerRole);
    public static bool operator ==(CanonicalRuntimeHarnessTickResult left, CanonicalRuntimeHarnessTickResult right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalRuntimeHarnessTickResult left, CanonicalRuntimeHarnessTickResult right) =>
        !left.Equals(right);
}

public sealed class CanonicalRuntimeHarnessUploadResult : IEquatable<CanonicalRuntimeHarnessUploadResult>
{
    public CanonicalUploadSessionID? SessionID { get; }
    public long ConfirmedBytes { get; }
    public bool Completed { get; }
    public CanonicalHash? Checksum { get; }
    public long? FileSize { get; }

    public CanonicalRuntimeHarnessUploadResult(
        CanonicalUploadSessionID? sessionID,
        long confirmedBytes,
        bool completed,
        CanonicalHash? checksum,
        long? fileSize)
    {
        SessionID = sessionID;
        ConfirmedBytes = confirmedBytes;
        Completed = completed;
        Checksum = checksum;
        FileSize = fileSize;
    }

    public override bool Equals(object? obj) =>
        obj is CanonicalRuntimeHarnessUploadResult other && Equals(other);
    public bool Equals(CanonicalRuntimeHarnessUploadResult? other) =>
        other is not null &&
        Nullable.Equals(SessionID, other.SessionID) &&
        ConfirmedBytes == other.ConfirmedBytes &&
        Completed == other.Completed &&
        Nullable.Equals(Checksum, other.Checksum) &&
        FileSize == other.FileSize;
    public override int GetHashCode() =>
        HashCode.Combine(SessionID, ConfirmedBytes, Completed, Checksum, FileSize);
    public static bool operator ==(CanonicalRuntimeHarnessUploadResult left, CanonicalRuntimeHarnessUploadResult right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalRuntimeHarnessUploadResult left, CanonicalRuntimeHarnessUploadResult right) =>
        !left.Equals(right);
}

public sealed class CanonicalRuntimeHarness
{
    private sealed class NodeState
    {
        public CanonicalNode Node { get; set; } = null!;
        public InMemoryCanonicalFileStore FileStore { get; set; } = null!;
        public CanonicalResumableUploadRuntime UploadRuntime { get; set; } = null!;
        public CanonicalRootToken MetadataRoot { get; set; }
        public CanonicalRootToken AudioRoot { get; set; }
        public CanonicalRootToken GeneratedRoot { get; set; }
        public Dictionary<string, CanonicalRecordingMetadata> Recordings { get; set; } = new();
        public Dictionary<string, List<CanonicalArtifact>> ArtifactsByObjectID { get; set; } = new();
        public List<CanonicalLibraryObject> LibraryObjects { get; set; } = new();
        public List<CanonicalLibraryTombstone> LibraryTombstones { get; set; } = new();
    }

    private NodeState _iphone;
    private NodeState _mac;
    private readonly InMemoryCanonicalTransportRuntime _transport = new();
    private bool _transportRegistered;

    public CanonicalRuntimeHarness()
    {
        var iphoneRoots = Roots("iphone");
        var macRoots = Roots("mac");
        var iphoneStore = new InMemoryCanonicalFileStore(iphoneRoots.Bindings);
        var macStore = new InMemoryCanonicalFileStore(macRoots.Bindings);
        var iphoneNode = new CanonicalNode(
            nodeID: "iphone-01",
            platform: "iPhone",
            capabilities: new[]
            {
                CanonicalCapability.recordingMetadata,
                CanonicalCapability.audioArtifact,
                CanonicalCapability.objectProjection,
                CanonicalCapability.canonicalLibraryObjectsV1,
                CanonicalCapability.canonicalFolderObjectsV1,
                CanonicalCapability.canonicalStudyItemObjectsV1,
                CanonicalCapability.canonicalTransferStateV1,
                CanonicalCapability.canonicalObjectProjectionV1,
                CanonicalCapability.canonicalInventoryBuilderV1,
                CanonicalCapability.canonicalRetirementReadinessV1
            });
        var macNode = new CanonicalNode(
            nodeID: "mac-01",
            platform: "Mac",
            capabilities: new[]
            {
                CanonicalCapability.recordingMetadata,
                CanonicalCapability.audioArtifact,
                CanonicalCapability.receiveRecord,
                CanonicalCapability.transcriptArtifact,
                CanonicalCapability.noteArtifact,
                CanonicalCapability.summaryArtifact,
                CanonicalCapability.objectProjection,
                CanonicalCapability.canonicalLibraryObjectsV1,
                CanonicalCapability.canonicalFolderObjectsV1,
                CanonicalCapability.canonicalStudyItemObjectsV1,
                CanonicalCapability.canonicalTransferStateV1,
                CanonicalCapability.canonicalObjectProjectionV1,
                CanonicalCapability.canonicalInventoryBuilderV1,
                CanonicalCapability.canonicalRetirementReadinessV1
            });
        _iphone = new NodeState
        {
            Node = iphoneNode,
            FileStore = iphoneStore,
            UploadRuntime = new CanonicalResumableUploadRuntime(iphoneStore),
            MetadataRoot = iphoneRoots.Metadata,
            AudioRoot = iphoneRoots.Audio,
            GeneratedRoot = iphoneRoots.Generated,
            Recordings = new Dictionary<string, CanonicalRecordingMetadata>(),
            ArtifactsByObjectID = new Dictionary<string, List<CanonicalArtifact>>(),
            LibraryObjects = new List<CanonicalLibraryObject>(),
            LibraryTombstones = new List<CanonicalLibraryTombstone>()
        };
        _mac = new NodeState
        {
            Node = macNode,
            FileStore = macStore,
            UploadRuntime = new CanonicalResumableUploadRuntime(macStore),
            MetadataRoot = macRoots.Metadata,
            AudioRoot = macRoots.Audio,
            GeneratedRoot = macRoots.Generated,
            Recordings = new Dictionary<string, CanonicalRecordingMetadata>(),
            ArtifactsByObjectID = new Dictionary<string, List<CanonicalArtifact>>(),
            LibraryObjects = new List<CanonicalLibraryObject>(),
            LibraryTombstones = new List<CanonicalLibraryTombstone>()
        };
    }

    public async Task SeedRecordingAsync(
        CanonicalRuntimeHarnessNodeRole role,
        string objectID,
        string title = "Lecture",
        DateTime? modifiedAt = null,
        byte[]? audioBytes = null)
    {
        var state = NodeState(role);
        var modAt = modifiedAt ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(2000);
        var metadata = new CanonicalRecordingMetadata(
            objectID: objectID,
            title: title,
            createdAt: new CanonicalTimestamp(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(1000)),
            modifiedAt: new CanonicalTimestamp(modAt),
            duration: audioBytes != null ? TimeSpan.FromSeconds(audioBytes.Length) : null);
        state.Recordings[metadata.ObjectID] = metadata;

        if (audioBytes != null)
        {
            var reference = new CanonicalFileReference(
                rootToken: state.AudioRoot,
                logicalPathToken: $"audio/{SafePathComponent(objectID)}.m4a",
                artifactID: CanonicalArtifact.Kind.audio.ArtifactIDFor(objectID),
                artifactKind: CanonicalArtifact.Kind.audio);
            var hash = InMemoryCanonicalFileStore.Hash(audioBytes, CanonicalFileHashPolicy.sha256)
                       ?? new CanonicalHash("");
            var writeResult = await state.FileStore.WriteAsync(new CanonicalFileWriteIntent(
                reference: reference,
                bytes: audioBytes,
                purpose: CanonicalFilePurpose.artifactBytes,
                expectedContentHash: hash,
                expectedByteSize: audioBytes.Length,
                conflictPolicy: CanonicalFileConflictPolicy.idempotentIfSameContent));
            var artifact = CanonicalArtifactFact.Audio(
                    availability: CanonicalArtifact.AvailabilityKind.available,
                    contentHash: hash,
                    byteSize: audioBytes.Length,
                    logicalName: "audio.m4a",
                    logicalPathToken: reference.LogicalPathToken,
                    producedByNodeID: role == CanonicalRuntimeHarnessNodeRole.iPhone
                        ? state.Node.NodeID
                        : "iphone-01")
                .MakeArtifact(objectID: objectID);
            if (!state.ArtifactsByObjectID.TryGetValue(objectID, out var existingList))
            {
                existingList = new List<CanonicalArtifact>();
                state.ArtifactsByObjectID[objectID] = existingList;
            }
            existingList.RemoveAll(a => a.ArtifactKind == CanonicalArtifact.Kind.audio);
            existingList.Add(artifact);
        }
        SetNodeState(state, role);
    }

    public async Task SeedGeneratedArtifactAsync(
        CanonicalRuntimeHarnessNodeRole role,
        string objectID,
        CanonicalArtifact.Kind kind,
        byte[] bytes,
        string? logicalPathToken = null,
        DateTime? modifiedAt = null)
    {
        var state = NodeState(role);
        var modAt = modifiedAt ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(3000);
        var token = logicalPathToken ?? $"generated/{SafePathComponent(objectID)}/{kind.ToString()}.bin";
        var reference = new CanonicalFileReference(
            rootToken: state.GeneratedRoot,
            logicalPathToken: token,
            artifactID: kind.ArtifactIDFor(objectID),
            artifactKind: kind);
        var hash = InMemoryCanonicalFileStore.Hash(bytes, CanonicalFileHashPolicy.sha256)
                   ?? new CanonicalHash("");
        var writeResult = await state.FileStore.WriteAsync(new CanonicalFileWriteIntent(
            reference: reference,
            bytes: bytes,
            purpose: CanonicalFilePurpose.generatedArtifact,
            expectedContentHash: hash,
            expectedByteSize: bytes.Length,
            conflictPolicy: CanonicalFileConflictPolicy.idempotentIfSameContent));
        var artifact = new CanonicalArtifact(
            artifactID: kind.ArtifactIDFor(objectID),
            objectID: objectID,
            kind: kind,
            availability: CanonicalArtifact.AvailabilityKind.available,
            contentHash: hash,
            byteSize: bytes.Length,
            logicalPathToken: token,
            modifiedAt: new CanonicalTimestamp(modAt),
            observedAt: new CanonicalTimestamp(modAt),
            producedBy: GeneratedProducer(kind, state.Node.Platform),
            producedByNodeID: state.Node.NodeID);
        if (!state.ArtifactsByObjectID.TryGetValue(objectID, out var existingList))
        {
            existingList = new List<CanonicalArtifact>();
            state.ArtifactsByObjectID[objectID] = existingList;
        }
        existingList.RemoveAll(a => a.ArtifactKind == kind);
        existingList.Add(artifact);
        SetNodeState(state, role);
    }

    public CanonicalManifest Manifest(CanonicalRuntimeHarnessNodeRole role, DateTime? generatedAt = null)
    {
        var state = NodeState(role);
        var genAt = generatedAt ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(4000);
        var objects = state.Recordings.Values.Select(metadata =>
        {
            var artifacts = state.ArtifactsByObjectID.TryGetValue(metadata.ObjectID, out var list)
                ? list.ToArray()
                : Array.Empty<CanonicalArtifact>();
            return new CanonicalRecordingObject(
                objectID: metadata.ObjectID,
                nodeID: state.Node.NodeID,
                metadata: metadata,
                artifacts: artifacts);
        }).ToArray();
        return CanonicalManifest.Make(
            node: state.Node,
            generatedAt: genAt,
            objects: objects,
            libraryObjects: state.LibraryObjects.ToArray(),
            libraryTombstones: state.LibraryTombstones.ToArray(),
            manifestCapabilities: new[]
            {
                CanonicalCapability.canonicalLibraryObjectsV1,
                CanonicalCapability.canonicalFolderObjectsV1,
                CanonicalCapability.canonicalStudyItemObjectsV1,
                CanonicalCapability.canonicalInventoryBuilderV1,
                CanonicalCapability.canonicalRetirementReadinessV1
            });
    }

    public async Task<CanonicalRuntimeHarnessTickResult> RunApplyTick(
        CanonicalRuntimeHarnessNodeRole localRole,
        CanonicalRuntimeHarnessNodeRole peerRole,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic)
    {
        await EnsureTransportRegisteredAsync();
        var localManifest = Manifest(role: localRole);
        var peerManifest = Manifest(role: peerRole);
        _transport.ValidateManifest(localManifest);
        _transport.ValidateManifest(peerManifest);

        var syncPlan = new CanonicalSyncPlanner().Plan(
            local: localManifest, peer: peerManifest, trigger: trigger);
        var applyPlan = new CanonicalApplyPlanner().Plan(
            local: localManifest, peer: peerManifest, syncPlan: syncPlan, trigger: trigger);
        var libraryPlan = new CanonicalLibrarySyncPlanner().Plan(
            local: localManifest, peer: peerManifest, trigger: trigger);
        var context = ApplyContext(
            localRole: localRole, peerRole: peerRole,
            localManifest: localManifest, peerManifest: peerManifest);
        var executionReport = await new CanonicalApplyExecutor().Execute(
            applyPlan: applyPlan, context: context, libraryPlan: libraryPlan);
        ApplyStateMutation(
            executionReport: executionReport,
            localRole: localRole, peerRole: peerRole);
        var transferProjection = CanonicalTransferStateMachine.Projection(
            Array.Empty<CanonicalTransferJob>());
        var projection = CanonicalObjectProjectionBuilder.Build(
            manifest: Manifest(role: localRole),
            applyPlan: applyPlan,
            libraryPlan: libraryPlan,
            transferProjection: transferProjection);
        var readiness = new CanonicalRuntimeReadinessEvaluator().Evaluate(
            evidence: new CanonicalRuntimeReadinessEvidence(
                fileRootBinding: true,
                fileHashVerification: true,
                transportRouteValidation: true,
                uploadResumableState: true,
                applyExecutor: true,
                conflictResolver: true,
                twoNodeHarness: true,
                productionStillLegacyOwned: true));
        return new CanonicalRuntimeHarnessTickResult(
            localRole: localRole,
            peerRole: peerRole,
            syncPlan: syncPlan,
            applyPlan: applyPlan,
            libraryPlan: libraryPlan,
            executionReport: executionReport,
            projection: projection,
            runtimeReadiness: readiness);
    }

    public async Task<CanonicalRuntimeHarnessUploadResult> UploadAudioAsync(
        string objectID,
        CanonicalRuntimeHarnessNodeRole sourceRole,
        CanonicalRuntimeHarnessNodeRole destinationRole,
        int chunkSize = 4)
    {
        var source = NodeState(sourceRole);
        var destination = NodeState(destinationRole);

        CanonicalArtifact? sourceArtifact = null;
        if (source.ArtifactsByObjectID.TryGetValue(objectID, out var sourceArtifacts))
            sourceArtifact = sourceArtifacts.FirstOrDefault(a => a.ArtifactKind == CanonicalArtifact.Kind.audio);

        if (sourceArtifact == null
            || sourceArtifact.LogicalPathToken == null
            || sourceArtifact.ContentHash == null
            || sourceArtifact.ByteSize == null)
            throw CanonicalUploadRuntimeError.InvalidRequest(objectID);

        var totalHash = sourceArtifact.ContentHash;
        var totalBytes = sourceArtifact.ByteSize.Value;

        var sourceRead = await source.FileStore.ReadAsync(new CanonicalFileReadRequest(
            reference: new CanonicalFileReference(
                rootToken: source.AudioRoot,
                logicalPathToken: sourceArtifact.LogicalPathToken,
                artifactID: sourceArtifact.ArtifactID,
                artifactKind: CanonicalArtifact.Kind.audio)));

        var targetReference = new CanonicalFileReference(
            rootToken: destination.AudioRoot,
            logicalPathToken: $"audio/{SafePathComponent(objectID)}.m4a",
            artifactID: sourceArtifact.ArtifactID,
            artifactKind: CanonicalArtifact.Kind.audio);

        var start = await destination.UploadRuntime.StartAsync(
            new CanonicalUploadStartRequest(
                objectID: objectID,
                targetReference: targetReference,
                totalBytes: totalBytes,
                totalHash: totalHash,
                chunkSize: chunkSize));

        if (start.SessionID == null)
        {
            return new CanonicalRuntimeHarnessUploadResult(
                sessionID: null,
                confirmedBytes: start.ConfirmedBytes,
                completed: start.Completed,
                checksum: start.Checksum,
                fileSize: start.FileSize);
        }

        var sessionID = start.SessionID.Value;
        var offset = start.NextOffset;
        while (offset < totalBytes)
        {
            var remaining = (int)Math.Min(chunkSize, sourceRead.Bytes.Length - offset);
            var chunkBytes = new byte[remaining];
            Array.Copy(sourceRead.Bytes, offset, chunkBytes, 0, remaining);
            var chunkHash = InMemoryCanonicalFileStore.Hash(chunkBytes, CanonicalFileHashPolicy.sha256)
                            ?? new CanonicalHash("");
            var response = await destination.UploadRuntime.AppendAsync(
                new CanonicalUploadChunk(
                    objectID: objectID,
                    sessionID: sessionID,
                    offset: offset,
                    bytes: chunkBytes,
                    chunkHash: chunkHash,
                    totalHash: totalHash,
                    idempotencyKey: $"chunk-{offset}"));
            offset = response.NextOffset;
        }

        var finalized = await destination.UploadRuntime.FinalizeAsync(
            new CanonicalUploadFinalizeRequest(
                objectID: objectID,
                sessionID: sessionID,
                totalBytes: totalBytes,
                totalHash: totalHash));

        var receivedArtifact = new CanonicalArtifact(
            artifactID: sourceArtifact.ArtifactID,
            objectID: sourceArtifact.ObjectID,
            kind: sourceArtifact.ArtifactKind,
            availability: sourceArtifact.Availability,
            contentHash: sourceArtifact.ContentHash,
            byteSize: sourceArtifact.ByteSize,
            logicalName: sourceArtifact.LogicalName,
            logicalPathToken: targetReference.LogicalPathToken,
            modifiedAt: sourceArtifact.ModifiedAt,
            observedAt: sourceArtifact.ObservedAt,
            producedBy: sourceArtifact.ProducedBy,
            producedByNodeID: source.Node.NodeID,
            tombstone: sourceArtifact.Tombstone);
        if (!destination.ArtifactsByObjectID.TryGetValue(objectID, out var destList))
        {
            destList = new List<CanonicalArtifact>();
            destination.ArtifactsByObjectID[objectID] = destList;
        }
        destList.RemoveAll(a => a.ArtifactKind == CanonicalArtifact.Kind.audio);
        destList.Add(receivedArtifact);
        if (!destination.Recordings.ContainsKey(objectID)
            && source.Recordings.TryGetValue(objectID, out var sourceMetadata))
        {
            destination.Recordings[objectID] = sourceMetadata;
        }
        SetNodeState(destination, destinationRole);

        return new CanonicalRuntimeHarnessUploadResult(
            sessionID: sessionID,
            confirmedBytes: finalized.ConfirmedBytes,
            completed: finalized.Completed,
            checksum: finalized.Checksum,
            fileSize: finalized.FileSize);
    }

    private async Task EnsureTransportRegisteredAsync()
    {
        if (_transportRegistered)
            return;
        await _transport.RegisterAsync(_iphone.Node,
            new HashSet<CanonicalTransportRoute>(Enum.GetValues<CanonicalTransportRoute>()));
        await _transport.RegisterAsync(_mac.Node,
            new HashSet<CanonicalTransportRoute>(Enum.GetValues<CanonicalTransportRoute>()));
        _transportRegistered = true;
    }

    private CanonicalApplyRuntimeContext ApplyContext(
        CanonicalRuntimeHarnessNodeRole localRole,
        CanonicalRuntimeHarnessNodeRole peerRole,
        CanonicalManifest localManifest,
        CanonicalManifest peerManifest)
    {
        var local = NodeState(localRole);
        var peer = NodeState(peerRole);
        return new CanonicalApplyRuntimeContext(
            localManifest: localManifest,
            peerManifest: peerManifest,
            localFileStore: local.FileStore,
            peerFileStore: peer.FileStore,
            localMetadataRoot: local.MetadataRoot,
            peerMetadataRoot: peer.MetadataRoot,
            localGeneratedRoot: local.GeneratedRoot,
            peerGeneratedRoot: peer.GeneratedRoot);
    }

    private void ApplyStateMutation(
        CanonicalApplyExecutionReport executionReport,
        CanonicalRuntimeHarnessNodeRole localRole,
        CanonicalRuntimeHarnessNodeRole peerRole)
    {
        var local = NodeState(localRole);
        var peer = NodeState(peerRole);
        foreach (var record in executionReport.Records)
        {
            if (record.Status != CanonicalApplyExecutionStatus.applied)
                continue;
            switch (record.Kind)
            {
                case CanonicalApplyActionKind.generatedArtifactDownloadApply:
                {
                    if (record.Target.ArtifactKind == null)
                        continue;
                    var kind = record.Target.ArtifactKind.Value;
                    CanonicalArtifact? artifact = null;
                    if (peer.ArtifactsByObjectID.TryGetValue(record.Target.ObjectID, out var peerArtifacts))
                        artifact = peerArtifacts.FirstOrDefault(a => a.ArtifactKind == kind);
                    if (artifact == null)
                        continue;
                    if (!local.ArtifactsByObjectID.TryGetValue(record.Target.ObjectID, out var localList))
                    {
                        localList = new List<CanonicalArtifact>();
                        local.ArtifactsByObjectID[record.Target.ObjectID] = localList;
                    }
                    localList.RemoveAll(a => a.ArtifactKind == kind);
                    localList.Add(artifact);
                    break;
                }
                case CanonicalApplyActionKind.recordingMetadataApply:
                case CanonicalApplyActionKind.objectTombstoneApply:
                {
                    if (peer.Recordings.TryGetValue(record.Target.ObjectID, out var metadata))
                        local.Recordings[record.Target.ObjectID] = metadata;
                    break;
                }
            }
        }
        SetNodeState(local, localRole);
    }

    private NodeState NodeState(CanonicalRuntimeHarnessNodeRole role)
    {
        return role switch
        {
            CanonicalRuntimeHarnessNodeRole.iPhone => _iphone,
            CanonicalRuntimeHarnessNodeRole.mac => _mac,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    private void SetNodeState(NodeState state, CanonicalRuntimeHarnessNodeRole role)
    {
        switch (role)
        {
            case CanonicalRuntimeHarnessNodeRole.iPhone:
                _iphone = state;
                break;
            case CanonicalRuntimeHarnessNodeRole.mac:
                _mac = state;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }
    }

    private static (CanonicalRootToken Metadata, CanonicalRootToken Audio,
        CanonicalRootToken Generated, Dictionary<CanonicalRootToken, string> Bindings) Roots(string prefix)
    {
        var metadata = new CanonicalRootToken($"{prefix}-metadata-root");
        var audio = new CanonicalRootToken($"{prefix}-audio-root");
        var generated = new CanonicalRootToken($"{prefix}-generated-root");
        return (metadata, audio, generated, new Dictionary<CanonicalRootToken, string>
        {
            [metadata] = $"{prefix}/metadata",
            [audio] = $"{prefix}/audio",
            [generated] = $"{prefix}/generated"
        });
    }

    private static string SafePathComponent(string value)
    {
        var allowed = new HashSet<char>(
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_");
        var component = new string(value.Select(c => allowed.Contains(c) ? c : '-').ToArray())
            .Trim('-');
        if (string.IsNullOrEmpty(component))
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            var hex = string.Concat(digest.Select(b => b.ToString("x2")));
            return hex[..Math.Min(hex.Length, 12)];
        }
        return component;
    }

    private static CanonicalArtifactProducer? GeneratedProducer(
        CanonicalArtifact.Kind kind,
        string platform)
    {
        var isMac = platform.Contains("Mac", StringComparison.OrdinalIgnoreCase);
        return kind switch
        {
            CanonicalArtifact.Kind.audio => CanonicalArtifactProducer.audioCapture,
            CanonicalArtifact.Kind.transcriptJSON or CanonicalArtifact.Kind.transcriptMarkdown
                => isMac ? CanonicalArtifactProducer.transcription : null,
            CanonicalArtifact.Kind.noteMarkdown or CanonicalArtifact.Kind.noteJSON
                or CanonicalArtifact.Kind.summaryJSON
                => isMac ? CanonicalArtifactProducer.noteGeneration : null,
            _ => null
        };
    }
}
