using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Rokurics.CanonicalCore
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowRootKind
    {
        temporary,
        shadowCopy,
        productionRootRejected
    }

    public record CanonicalShadowRootBinding : IEquatable<CanonicalShadowRootBinding>
    {
        public CanonicalRootToken RootToken { get; init; }
        public CanonicalShadowRootKind RootKind { get; init; }
        public string? RootPath { get; init; }
        public string? ProhibitedProductionRootPath { get; init; }

        public CanonicalShadowRootBinding(
            CanonicalRootToken rootToken,
            CanonicalShadowRootKind rootKind = CanonicalShadowRootKind.temporary,
            string? rootPath = null,
            string? prohibitedProductionRootPath = null)
        {
            RootToken = rootToken;
            RootKind = rootKind;
            RootPath = rootPath != null ? Path.GetFullPath(rootPath) : null;
            ProhibitedProductionRootPath = prohibitedProductionRootPath != null
                ? Path.GetFullPath(prohibitedProductionRootPath) : null;
        }

        public string ValidatedShadowRootPath()
        {
            if (RootKind == CanonicalShadowRootKind.productionRootRejected)
                throw new CanonicalProductionPortError.ProductionMutationAttemptedException(
                    "shadowProductionRootRejected");
            if (RootPath == null)
                throw new CanonicalFileRuntimeError.RootNotBoundException(RootToken.RawValue);
            var standardized = Path.GetFullPath(RootPath);
            if (ProhibitedProductionRootPath == null) return standardized;
            var production = Path.GetFullPath(ProhibitedProductionRootPath)
                .TrimEnd(Path.DirectorySeparatorChar);
            var shadow = standardized.TrimEnd(Path.DirectorySeparatorChar);
            if (shadow == production || shadow.StartsWith(production + Path.DirectorySeparatorChar))
                throw new CanonicalProductionPortError.ProductionMutationAttemptedException(
                    "shadowRootInsideProductionRootRejected");
            return standardized;
        }
    }

    public record CanonicalShadowCopyPolicy : IEquatable<CanonicalShadowCopyPolicy>
    {
        public long MaxBytes { get; init; }
        public bool AllowMetadataBytes { get; init; }
        public bool AllowArtifactBytes { get; init; }
        public bool AllowTombstoneMarkers { get; init; }
        public bool NoPhysicalDelete { get; init; }

        public CanonicalShadowCopyPolicy(
            long maxBytes = 8 * 1024 * 1024,
            bool allowMetadataBytes = true,
            bool allowArtifactBytes = true,
            bool allowTombstoneMarkers = true,
            bool noPhysicalDelete = true)
        {
            MaxBytes = Math.Max(0, maxBytes);
            AllowMetadataBytes = allowMetadataBytes;
            AllowArtifactBytes = allowArtifactBytes;
            AllowTombstoneMarkers = allowTombstoneMarkers;
            NoPhysicalDelete = noPhysicalDelete;
        }
    }

    public record CanonicalShadowCopyEntry : IEquatable<CanonicalShadowCopyEntry>
    {
        public string Id => string.Join("|",
            new[] { Reference.RootToken.RawValue, Reference.LogicalPathToken, Purpose.ToString() });
        public CanonicalFileReference Reference { get; init; }
        public CanonicalFilePurpose Purpose { get; init; }
        public long ByteSize { get; init; }
        public string? ContentHashPrefix { get; init; }
        public bool CopiedToShadowRoot { get; init; }
        public string? Reason { get; init; }

        public CanonicalShadowCopyEntry(
            CanonicalFileReference reference,
            CanonicalFilePurpose purpose,
            long byteSize,
            CanonicalHash? contentHash = null,
            bool copiedToShadowRoot = false,
            string? reason = null)
        {
            Reference = reference;
            Purpose = purpose;
            ByteSize = Math.Max(0, byteSize);
            ContentHashPrefix = contentHash != null
                ? CanonicalProductionRedaction.HashPrefix(contentHash.Value) : null;
            CopiedToShadowRoot = copiedToShadowRoot;
            Reason = CanonicalShadowMigrationRedaction.SafeText(reason);
        }
    }

    public record CanonicalShadowCopyManifest : IEquatable<CanonicalShadowCopyManifest>
    {
        public CanonicalShadowRootKind RootKind { get; init; }
        public CanonicalRootToken RootToken { get; init; }
        public List<CanonicalShadowCopyEntry> Entries { get; init; }
        public long TotalBytes { get; init; }
        public int MissingSourceBytesCount { get; init; }

        public CanonicalShadowCopyManifest(
            CanonicalShadowRootKind rootKind,
            CanonicalRootToken rootToken,
            List<CanonicalShadowCopyEntry>? entries = null,
            int missingSourceBytesCount = 0)
        {
            RootKind = rootKind;
            RootToken = rootToken;
            Entries = (entries ?? new List<CanonicalShadowCopyEntry>())
                .OrderBy(e => e.Id).ToList();
            TotalBytes = Entries.Sum(e => e.ByteSize);
            MissingSourceBytesCount = Math.Max(0, missingSourceBytesCount);
        }
    }

    public record CanonicalShadowFileExecutionReport : IEquatable<CanonicalShadowFileExecutionReport>
    {
        public CanonicalShadowRootKind RootKind { get; init; }
        public int EntryCount { get; init; }
        public long BytesCopied { get; init; }
        public bool WroteToShadowRoot { get; init; }
        public bool WroteProductionRoot { get; init; }
        public bool PhysicalDeletePerformed { get; init; }
        public bool RollbackAvailable { get; init; }
        public int MissingSourceBytesCount { get; init; }
        public string? RejectedReason { get; init; }

        public CanonicalShadowFileExecutionReport(
            CanonicalShadowRootKind rootKind,
            CanonicalShadowCopyManifest? manifest = null,
            bool wroteToShadowRoot = false,
            bool wroteProductionRoot = false,
            bool physicalDeletePerformed = false,
            bool rollbackAvailable = false,
            int missingSourceBytesCount = 0,
            string? rejectedReason = null)
        {
            RootKind = rootKind;
            EntryCount = manifest?.Entries.Count ?? 0;
            BytesCopied = manifest?.TotalBytes ?? 0;
            WroteToShadowRoot = wroteToShadowRoot;
            WroteProductionRoot = wroteProductionRoot;
            PhysicalDeletePerformed = physicalDeletePerformed;
            RollbackAvailable = rollbackAvailable;
            MissingSourceBytesCount = Math.Max(0,
                missingSourceBytesCount + (manifest?.MissingSourceBytesCount ?? 0));
            RejectedReason = CanonicalShadowMigrationRedaction.SafeText(rejectedReason);
        }
    }

    public class CanonicalShadowFileStore : ICanonicalFileStorePort
    {
        public CanonicalRootToken RootToken { get; }
        public CanonicalShadowRootKind RootKind { get; }
        public CanonicalShadowCopyPolicy Policy { get; }

        private readonly InMemoryCanonicalFileStore _store;
        private readonly List<CanonicalShadowCopyEntry> _entries = new();
        private int _missingSourceBytesCount;
        private readonly object _lock = new();

        public CanonicalShadowFileStore(
            CanonicalRootToken? rootToken = null,
            CanonicalShadowRootKind rootKind = CanonicalShadowRootKind.temporary,
            CanonicalShadowCopyPolicy? policy = null)
        {
            RootToken = rootToken ?? new CanonicalRootToken("canonical-shadow-root");
            RootKind = rootKind;
            Policy = policy ?? new CanonicalShadowCopyPolicy();
            _store = new InMemoryCanonicalFileStore(
                new Dictionary<CanonicalRootToken, string>
                { { RootToken, "canonical-shadow-root" } });
        }

        public Task<CanonicalPathResolutionResult> ResolveAsync(CanonicalFileReference reference) =>
            _store.ResolveAsync(reference);

        public Task<CanonicalFileReadResult> ReadAsync(CanonicalFileReadRequest request) =>
            _store.ReadAsync(request);

        public async Task<CanonicalFileWriteResult> WriteAsync(CanonicalFileWriteIntent intent)
        {
            Validate(intent);
            var result = await _store.WriteAsync(intent);
            lock (_lock)
            {
                _entries.Add(new CanonicalShadowCopyEntry(
                    intent.Reference, intent.Purpose, result.ByteSize,
                    result.ContentHash, true, result.Disposition.ToString()));
            }
            return result;
        }

        public async Task<CanonicalFileWriteResult> MarkTombstoneAsync(
            CanonicalFileReference reference, string? reason)
        {
            if (!Policy.AllowTombstoneMarkers)
                throw new CanonicalProductionPortError.ProductionMutationAttemptedException(
                    "shadowTombstoneMarkerDisabled");
            var result = await _store.MarkTombstoneAsync(reference, reason);
            lock (_lock)
            {
                _entries.Add(new CanonicalShadowCopyEntry(
                    reference, CanonicalFilePurpose.tombstoneMarker, result.ByteSize,
                    result.ContentHash, true, "noPhysicalDelete"));
            }
            return result;
        }

        public Task<bool> ContainsAsync(CanonicalFileReference reference) =>
            _store.ContainsAsync(reference);

        public CanonicalShadowFileExecutionReport Report()
        {
            lock (_lock)
            {
                return new CanonicalShadowFileExecutionReport(
                    RootKind,
                    new CanonicalShadowCopyManifest(RootKind, RootToken,
                        new List<CanonicalShadowCopyEntry>(_entries),
                        _missingSourceBytesCount),
                    wroteToShadowRoot: _entries.Count > 0,
                    wroteProductionRoot: false,
                    physicalDeletePerformed: false,
                    rollbackAvailable: _entries.Count > 0,
                    missingSourceBytesCount: _missingSourceBytesCount);
            }
        }

        public void RecordMissingSourceBytes()
        {
            lock (_lock) { _missingSourceBytesCount++; }
        }

        private void Validate(CanonicalFileWriteIntent intent)
        {
            if (!Equals(intent.Reference.RootToken, RootToken))
                throw new CanonicalFileRuntimeError.RootNotBoundException(
                    intent.Reference.RootToken.RawValue);
            if (Policy.MaxBytes > 0 && intent.Bytes.Length > Policy.MaxBytes)
                throw new CanonicalFileRuntimeError.PreWriteSizeMismatchException(
                    Policy.MaxBytes, intent.Bytes.Length);

            switch (intent.Purpose)
            {
                case CanonicalFilePurpose.metadataBlob:
                    if (!Policy.AllowMetadataBytes)
                        throw new CanonicalProductionPortError.FullContentRejectedException(
                            "shadowMetadataBytesDisabled");
                    break;
                case CanonicalFilePurpose.artifactBytes:
                case CanonicalFilePurpose.generatedArtifact:
                    if (!Policy.AllowArtifactBytes)
                        throw new CanonicalProductionPortError.FullContentRejectedException(
                            "shadowArtifactBytesDisabled");
                    break;
                case CanonicalFilePurpose.tombstoneMarker:
                    if (!Policy.AllowTombstoneMarkers)
                        throw new CanonicalProductionPortError.ProductionMutationAttemptedException(
                            "shadowTombstoneMarkerDisabled");
                    break;
            }
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowRouteClassification
    {
        readOnly,
        mutating,
        unknown
    }

    public record CanonicalShadowRoutePolicy : IEquatable<CanonicalShadowRoutePolicy>
    {
        public int ArtifactRequestMaxBytes { get; init; }

        public CanonicalShadowRoutePolicy(int artifactRequestMaxBytes = 256 * 1024)
        {
            ArtifactRequestMaxBytes = Math.Max(0, artifactRequestMaxBytes);
        }

        public CanonicalShadowRouteClassification ClassificationFor(
            CanonicalTransportRoute route, int bodyByteCount = 0)
        {
            return route switch
            {
                CanonicalTransportRoute.manifestExchange => CanonicalShadowRouteClassification.readOnly,
                CanonicalTransportRoute.fileRead =>
                    bodyByteCount <= ArtifactRequestMaxBytes
                        ? CanonicalShadowRouteClassification.readOnly
                        : CanonicalShadowRouteClassification.mutating,
                CanonicalTransportRoute.applyPlan or CanonicalTransportRoute.applyMetadata
                    or CanonicalTransportRoute.uploadStart or CanonicalTransportRoute.uploadStatus
                    or CanonicalTransportRoute.uploadChunk or CanonicalTransportRoute.uploadFinalize
                    => CanonicalShadowRouteClassification.mutating,
                _ => CanonicalShadowRouteClassification.unknown
            };
        }

        public CanonicalShadowNetworkProbeKind ProbeKindFor(CanonicalTransportRoute route)
        {
            return route switch
            {
                CanonicalTransportRoute.manifestExchange => CanonicalShadowNetworkProbeKind.syncInventoryReadOnly,
                CanonicalTransportRoute.fileRead => CanonicalShadowNetworkProbeKind.artifactRequestReadOnly,
                CanonicalTransportRoute.applyPlan or CanonicalTransportRoute.applyMetadata
                    => CanonicalShadowNetworkProbeKind.applyManifest,
                CanonicalTransportRoute.uploadStart => CanonicalShadowNetworkProbeKind.uploadSessionStart,
                CanonicalTransportRoute.uploadStatus => CanonicalShadowNetworkProbeKind.uploadSessionStart,
                CanonicalTransportRoute.uploadChunk => CanonicalShadowNetworkProbeKind.uploadSessionChunk,
                CanonicalTransportRoute.uploadFinalize => CanonicalShadowNetworkProbeKind.uploadSessionFinalize,
                _ => CanonicalShadowNetworkProbeKind.health
            };
        }
    }

    public record CanonicalShadowTransportEnvelopeReport : IEquatable<CanonicalShadowTransportEnvelopeReport>
    {
        public CanonicalTransportRoute Route { get; init; }
        public string RoutePath { get; init; }
        public CanonicalShadowRouteClassification Classification { get; init; }
        public string? BodyHashPrefix { get; init; }
        public bool TimestampPresent { get; init; }
        public bool NoncePresent { get; init; }
        public bool SignatureProjectionPresent { get; init; }
        public bool WouldSendNetwork { get; init; }
        public bool SentNetwork { get; init; }
        public string Reason { get; init; }

        public CanonicalShadowTransportEnvelopeReport(
            CanonicalProductionSignedRequest signedRequest,
            CanonicalShadowRouteClassification classification,
            bool wouldSendNetwork,
            bool sentNetwork,
            string reason)
        {
            Route = signedRequest.BuildRequest.Route;
            RoutePath = CanonicalShadowMigrationRedaction.SafeText(
                signedRequest.BuildRequest.ExistingRoutePath)
                ?? Route.ToString();
            Classification = classification;
            BodyHashPrefix = CanonicalProductionRedaction.HashPrefix(
                signedRequest.BodyHash.Value);
            TimestampPresent = true;
            NoncePresent = !string.IsNullOrEmpty(signedRequest.BuildRequest.Nonce);
            SignatureProjectionPresent = signedRequest.SignaturePrefix != null
                || signedRequest.SignerDescription != null;
            WouldSendNetwork = wouldSendNetwork;
            SentNetwork = sentNetwork;
            Reason = CanonicalShadowMigrationRedaction.SafeText(reason)
                ?? "shadowTransportProjection";
        }
    }

    public record CanonicalShadowTransportProbeResult : IEquatable<CanonicalShadowTransportProbeResult>
    {
        public CanonicalShadowNetworkProbeDecision PolicyDecision { get; init; }
        public CanonicalShadowTransportEnvelopeReport EnvelopeReport { get; init; }
        public bool Accepted { get; init; }
        public bool SentNetwork { get; init; }
        public string? FailureReason { get; init; }

        public CanonicalShadowTransportProbeResult(
            CanonicalShadowNetworkProbeDecision policyDecision,
            CanonicalShadowTransportEnvelopeReport envelopeReport,
            bool accepted,
            bool sentNetwork,
            string? failureReason = null)
        {
            PolicyDecision = policyDecision;
            EnvelopeReport = envelopeReport;
            Accepted = accepted;
            SentNetwork = sentNetwork;
            FailureReason = CanonicalShadowMigrationRedaction.SafeText(failureReason);
        }
    }

    public record CanonicalShadowTransportProbe : IEquatable<CanonicalShadowTransportProbe>
    {
        public CanonicalShadowRoutePolicy RoutePolicy { get; init; }

        public CanonicalShadowTransportProbe(
            CanonicalShadowRoutePolicy? routePolicy = null)
        {
            RoutePolicy = routePolicy ?? new CanonicalShadowRoutePolicy();
        }

        public async Task<CanonicalShadowTransportProbeResult> ProjectAsync(
            CanonicalProductionTransportBuildRequest request,
            ICanonicalProductionTransportPort transport,
            CanonicalShadowNetworkProbePolicy networkPolicy,
            bool allowNetworkSend = false)
        {
            var classification = RoutePolicy.ClassificationFor(
                request.Route, request.Body.Length);
            var probeRequest = new CanonicalShadowNetworkProbeRequest(
                RoutePolicy.ProbeKindFor(request.Route),
                request.ExistingRoutePath,
                request.Body.Length,
                RoutePolicy.ArtifactRequestMaxBytes);
            var decision = networkPolicy.DecisionFor(probeRequest);
            var signed = await transport.BuildSignedRequestAsync(request);
            var maySend = allowNetworkSend && decision.Accepted
                && classification == CanonicalShadowRouteClassification.readOnly
                && transport.RealNetworkExecutionEnabled;
            var sent = false;
            string? failureReason = null;
            if (maySend)
            {
                try
                {
                    await transport.SendRequestAsync(signed);
                    sent = true;
                }
                catch { failureReason = "sendFailed"; }
            }
            else if (decision.Accepted)
                failureReason = "networkSendSuppressedShadow";
            else
                failureReason = decision.Reason;

            var envelope = new CanonicalShadowTransportEnvelopeReport(
                signed, classification, decision.Accepted, sent,
                failureReason ?? decision.Reason);
            return new CanonicalShadowTransportProbeResult(
                decision, envelope, decision.Accepted, sent, failureReason);
        }
    }

    public record CanonicalShadowUploadSession : IEquatable<CanonicalShadowUploadSession>
    {
        public CanonicalUploadSessionID? SessionID { get; init; }
        public long ConfirmedBytes { get; init; }
        public CanonicalUploadSessionPhase Phase { get; init; }
    }

    public class CanonicalShadowUploadReceiver
    {
        public InMemoryCanonicalFileStore Store { get; }
        public CanonicalRootToken RootToken { get; }

        public CanonicalShadowUploadReceiver(
            CanonicalRootToken? rootToken = null)
        {
            RootToken = rootToken ?? new CanonicalRootToken("canonical-shadow-upload-root");
            Store = new InMemoryCanonicalFileStore(
                new Dictionary<CanonicalRootToken, string>
                { { RootToken, "canonical-shadow-upload-root" } });
        }

        public async Task SeedAsync(CanonicalFileReference reference, byte[] bytes)
        {
            var hash = InMemoryCanonicalFileStore.Hash(bytes, CanonicalHashPolicy.sha256);
            await Store.WriteAsync(new CanonicalFileWriteIntent(
                reference, bytes, CanonicalFilePurpose.artifactBytes,
                hash ?? CanonicalHash.Sha256String(""), bytes.Length,
                CanonicalConflictPolicy.replace));
        }

        public async Task<CanonicalFileReadResult> ReadAsync(CanonicalFileReference reference)
        {
            return await Store.ReadAsync(new CanonicalFileReadRequest(reference));
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowUploadDivergence
    {
        none,
        sameHashNoOp,
        differentHashConflict,
        finalizeHashMismatch,
        interruptedAndResumed,
        unexpectedFailure
    }

    public record CanonicalShadowUploadRehearsalInput
    {
        public string ObjectID { get; init; }
        public CanonicalFileReference TargetReference { get; init; }
        public byte[] Bytes { get; init; }
        public int ChunkSize { get; init; }
        public CanonicalHash? DeclaredTotalHash { get; init; }
        public byte[]? ExistingReceiverBytes { get; init; }
        public bool SimulateInterruptionAfterFirstChunk { get; init; }

        public CanonicalShadowUploadRehearsalInput(
            string objectID,
            CanonicalFileReference targetReference,
            byte[] bytes,
            int chunkSize = 4 * 1024 * 1024,
            CanonicalHash? declaredTotalHash = null,
            byte[]? existingReceiverBytes = null,
            bool simulateInterruptionAfterFirstChunk = false)
        {
            ObjectID = CanonicalProductionRedaction.SafeIdentifier(
                objectID, "unknown-recording");
            TargetReference = targetReference;
            Bytes = bytes;
            ChunkSize = Math.Max(1, chunkSize);
            DeclaredTotalHash = declaredTotalHash;
            ExistingReceiverBytes = existingReceiverBytes;
            SimulateInterruptionAfterFirstChunk = simulateInterruptionAfterFirstChunk;
        }
    }

    public record CanonicalShadowUploadResult : IEquatable<CanonicalShadowUploadResult>
    {
        public CanonicalShadowUploadSession? Session { get; init; }
        public bool CalledProductionUploadCoordinator { get; init; }
        public bool CalledRecordingUploadClient { get; init; }
        public bool CalledSecureMacUploadClient { get; init; }
        public bool WroteProductionInbox { get; init; }
        public bool WroteReceiveJSON { get; init; }
        public bool WroteShadowReceiver { get; init; }
        public bool Completed { get; init; }
        public long ConfirmedBytes { get; init; }
        public CanonicalUploadSessionPhase Phase { get; init; }
        public CanonicalShadowUploadDivergence Divergence { get; init; }
        public string? FailureReason { get; init; }
    }

    public class CanonicalShadowUploadRehearsal
    {
        public CanonicalShadowUploadRehearsal() { }

        public async Task<CanonicalShadowUploadResult> RunAsync(
            CanonicalShadowUploadRehearsalInput input,
            CanonicalShadowUploadReceiver? receiver = null,
            DateTime? now = null)
        {
            var nowDt = now ?? DateTime.UtcNow;
            receiver ??= new CanonicalShadowUploadReceiver();

            try
            {
                if (input.ExistingReceiverBytes != null)
                    await receiver.SeedAsync(input.TargetReference, input.ExistingReceiverBytes);

                var runtime = new CanonicalResumableUploadRuntime(receiver.Store);
                var actualHash = InMemoryCanonicalFileStore.Hash(input.Bytes, CanonicalHashPolicy.sha256)
                    ?? CanonicalHash.Sha256String("");
                var totalHash = input.DeclaredTotalHash ?? actualHash;

                var start = await runtime.StartAsync(
                    new CanonicalUploadStartRequest(
                        input.ObjectID, input.TargetReference,
                        input.Bytes.Length, totalHash, input.ChunkSize),
                    nowDt);
                if (start.Completed)
                    return MakeResult(start, false, CanonicalShadowUploadDivergence.sameHashNoOp);

                var sessionID = start.SessionID
                    ?? throw new CanonicalUploadRuntimeError.InvalidSessionException(
                        "shadowSessionMissing");
                var offset = 0;
                var interrupted = false;

                while (offset < input.Bytes.Length)
                {
                    var upper = Math.Min(offset + input.ChunkSize, input.Bytes.Length);
                    var chunkBytes = new byte[upper - offset];
                    Array.Copy(input.Bytes, offset, chunkBytes, 0, chunkBytes.Length);
                    var chunkHash = InMemoryCanonicalFileStore.Hash(chunkBytes, CanonicalHashPolicy.sha256)
                        ?? CanonicalHash.Sha256String("");

                    await runtime.AppendAsync(
                        new CanonicalUploadChunk(
                            input.ObjectID, sessionID, offset, chunkBytes, chunkHash,
                            totalHash, $"shadow-{offset}"),
                        nowDt);
                    offset = upper;

                    if (input.SimulateInterruptionAfterFirstChunk && !interrupted)
                    {
                        interrupted = true;
                        await runtime.StatusAsync(
                            new CanonicalUploadStatusRequest(
                                input.ObjectID, sessionID, totalHash),
                            nowDt);
                    }
                }

                var finalized = await runtime.FinalizeAsync(
                    new CanonicalUploadFinalizeRequest(
                        input.ObjectID, sessionID, input.Bytes.Length, totalHash),
                    nowDt);

                return MakeResult(finalized, true,
                    interrupted ? CanonicalShadowUploadDivergence.interruptedAndResumed
                        : CanonicalShadowUploadDivergence.none);
            }
            catch (CanonicalUploadRuntimeError.FinalHashMismatchException)
            {
                return FailedResult(CanonicalUploadSessionPhase.conflict,
                    CanonicalShadowUploadDivergence.finalizeHashMismatch,
                    "finalHashMismatch");
            }
            catch (CanonicalUploadRuntimeError.TargetConflictException)
            {
                return FailedResult(CanonicalUploadSessionPhase.conflict,
                    CanonicalShadowUploadDivergence.differentHashConflict,
                    "targetConflict");
            }
            catch (Exception ex)
            {
                return FailedResult(CanonicalUploadSessionPhase.failed,
                    CanonicalShadowUploadDivergence.unexpectedFailure,
                    ex.ToString());
            }
        }

        private static CanonicalShadowUploadResult MakeResult(
            CanonicalUploadSessionStatus status, bool wroteShadowReceiver,
            CanonicalShadowUploadDivergence divergence)
        {
            return new CanonicalShadowUploadResult
            {
                Session = new CanonicalShadowUploadSession
                {
                    SessionID = status.SessionID,
                    ConfirmedBytes = status.ConfirmedBytes,
                    Phase = status.Phase
                },
                CalledProductionUploadCoordinator = false,
                CalledRecordingUploadClient = false,
                CalledSecureMacUploadClient = false,
                WroteProductionInbox = false,
                WroteReceiveJSON = false,
                WroteShadowReceiver = wroteShadowReceiver,
                Completed = status.Completed,
                ConfirmedBytes = status.ConfirmedBytes,
                Phase = status.Phase,
                Divergence = divergence,
                FailureReason = null
            };
        }

        private static CanonicalShadowUploadResult FailedResult(
            CanonicalUploadSessionPhase phase,
            CanonicalShadowUploadDivergence divergence, string reason)
        {
            return new CanonicalShadowUploadResult
            {
                Session = null,
                CalledProductionUploadCoordinator = false,
                CalledRecordingUploadClient = false,
                CalledSecureMacUploadClient = false,
                WroteProductionInbox = false,
                WroteReceiveJSON = false,
                WroteShadowReceiver = false,
                Completed = false,
                ConfirmedBytes = 0,
                Phase = phase,
                Divergence = divergence,
                FailureReason = CanonicalShadowMigrationRedaction.SafeText(reason)
            };
        }
    }

    public class CanonicalShadowUploadPort : ICanonicalProductionUploadPort
    {
        public bool IsDryRunOnly => false;
        public bool ResumableSessionSupported => true;
        public int ChunkSizePolicy { get; }

        private readonly CanonicalResumableUploadRuntime _runtime;
        private readonly ConcurrentDictionary<string, CanonicalProductionUploadLedgerSnapshot> _ledgers = new();

        public CanonicalShadowUploadPort(
            CanonicalShadowUploadReceiver? receiver = null,
            int chunkSizePolicy = 4 * 1024 * 1024)
        {
            receiver ??= new CanonicalShadowUploadReceiver();
            ChunkSizePolicy = Math.Max(1, chunkSizePolicy);
            _runtime = new CanonicalResumableUploadRuntime(receiver.Store);
        }

        public async Task<CanonicalUploadSessionStatus> StartResumableUploadAsync(
            CanonicalUploadStartRequest request, DateTime now)
        {
            if (request.ChunkSize > ChunkSizePolicy)
                throw new CanonicalUploadRuntimeError.InvalidRequestException(
                    "shadowChunkSizeExceedsPolicy");
            var status = await _runtime.StartAsync(request, now);
            _ledgers[request.ObjectID] = MakeLedger(
                request.ObjectID, status, request.TotalBytes, request.TotalHash);
            return status;
        }

        public async Task<CanonicalUploadSessionStatus> ResumeUploadAsync(
            CanonicalUploadStatusRequest request, DateTime now)
        {
            var status = await _runtime.StatusAsync(request, now);
            _ledgers[request.ObjectID] = MakeLedger(
                request.ObjectID, status, status.FileSize,
                status.Checksum ?? request.TotalHash);
            return status;
        }

        public async Task<CanonicalUploadSessionStatus> UploadChunkAsync(
            CanonicalUploadChunk chunk, DateTime now)
        {
            var status = await _runtime.AppendAsync(chunk, now);
            _ledgers[chunk.ObjectID] = MakeLedger(
                chunk.ObjectID, status, null, chunk.TotalHash);
            return status;
        }

        public async Task<long> QueryConfirmedBytesAsync(
            CanonicalUploadStatusRequest request, DateTime now)
        {
            var status = await ResumeUploadAsync(request, now);
            return status.ConfirmedBytes;
        }

        public async Task<CanonicalUploadSessionStatus> FinalizeUploadAsync(
            CanonicalUploadFinalizeRequest request, DateTime now)
        {
            var status = await _runtime.FinalizeAsync(request, now);
            _ledgers[request.ObjectID] = MakeLedger(
                request.ObjectID, status, request.TotalBytes, request.TotalHash);
            return status;
        }

        public Task<CanonicalRollbackResult> CancelUploadAsync(
            CanonicalProductionUploadCancelRequest request, DateTime now)
        {
            _ledgers[request.ObjectID] = new CanonicalProductionUploadLedgerSnapshot(
                request.ObjectID, request.SessionID, 0,
                phase: CanonicalUploadSessionPhase.failed);
            return Task.FromResult(new CanonicalRollbackResult(
                request.SessionID.ToString(), true,
                new List<string> { request.SessionID.ToString() }));
        }

        public CanonicalProductionUploadFailureClassification ClassifyUploadFailure(
            CanonicalProductionUploadFailure failure)
        {
            var code = failure.Code.ToLower();
            if (code.Contains("conflict") || code == "409")
                return new CanonicalProductionUploadFailureClassification(
                    CanonicalProductionUploadFailureKind.conflict, null,
                    "shadowUploadConflict");
            if (code.Contains("timeout") || code.Contains("network")
                || code.Contains("retry"))
                return new CanonicalProductionUploadFailureClassification(
                    CanonicalProductionUploadFailureKind.retryable,
                    new CanonicalRetryPolicySnapshot(1, null, 3),
                    "shadowUploadRetryable");
            return new CanonicalProductionUploadFailureClassification(
                CanonicalProductionUploadFailureKind.fatal, null, failure.Code);
        }

        public Task<CanonicalProductionUploadLedgerSnapshot> ReadUploadLedgerAsync(
            string objectID)
        {
            var key = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
            return Task.FromResult(
                _ledgers.GetValueOrDefault(key, new CanonicalProductionUploadLedgerSnapshot(objectID)));
        }

        public Task<CanonicalProductionUploadLedgerSnapshot> WriteUploadLedgerAsync(
            CanonicalProductionUploadLedgerSnapshot snapshot)
        {
            _ledgers[snapshot.ObjectID] = snapshot;
            return Task.FromResult(snapshot);
        }

        public CanonicalRetryPolicySnapshot? ProjectRetry(
            CanonicalProductionUploadLedgerSnapshot snapshot, DateTime now)
        {
            return snapshot.Retry;
        }

        public Task<CanonicalRollbackResult> RollbackUploadStateAsync(
            CanonicalProductionUploadRollbackRequest request)
        {
            _ledgers[request.ObjectID] = new CanonicalProductionUploadLedgerSnapshot(
                request.ObjectID);
            return Task.FromResult(new CanonicalRollbackResult(
                request.CheckpointID, true,
                new List<string> { request.ObjectID }));
        }

        public async Task<CanonicalProductionUploadTrace> ProjectUploadDryRunAsync(
            CanonicalRecordingObject obj, CanonicalArtifact artifact)
        {
            if (artifact.Kind != CanonicalArtifactKind.audio)
                throw new CanonicalProductionPortError.UnsupportedObjectException(
                    "shadowUploadOnlySupportsAudio");
            await Task.CompletedTask;
            return new CanonicalProductionUploadTrace(
                obj.ObjectID, artifact.ArtifactID, artifact.ByteSize,
                artifact.ContentHash, ChunkSizePolicy, true,
                CanonicalTransportRoute.uploadStart, "shadowReceiverOnly");
        }

        private static CanonicalProductionUploadLedgerSnapshot MakeLedger(
            string objectID, CanonicalUploadSessionStatus status,
            long? totalBytes, CanonicalHash? totalHash)
        {
            return new CanonicalProductionUploadLedgerSnapshot(
                objectID, status.SessionID, status.ConfirmedBytes,
                totalBytes, totalHash, status.Phase, status.Retry);
        }
    }

    public class CanonicalShadowApplyStore
    {
        public InMemoryCanonicalFileStore LocalFileStore { get; }
        public InMemoryCanonicalFileStore PeerFileStore { get; }
        public CanonicalRootToken LocalMetadataRoot { get; }
        public CanonicalRootToken PeerMetadataRoot { get; }
        public CanonicalRootToken LocalGeneratedRoot { get; }
        public CanonicalRootToken PeerGeneratedRoot { get; }

        public CanonicalShadowApplyStore(
            CanonicalRootToken? localMetadataRoot = null,
            CanonicalRootToken? peerMetadataRoot = null,
            CanonicalRootToken? localGeneratedRoot = null,
            CanonicalRootToken? peerGeneratedRoot = null)
        {
            LocalMetadataRoot = localMetadataRoot ?? new CanonicalRootToken("shadow-local-metadata");
            PeerMetadataRoot = peerMetadataRoot ?? new CanonicalRootToken("shadow-peer-metadata");
            LocalGeneratedRoot = localGeneratedRoot ?? new CanonicalRootToken("shadow-local-generated");
            PeerGeneratedRoot = peerGeneratedRoot ?? new CanonicalRootToken("shadow-peer-generated");

            LocalFileStore = new InMemoryCanonicalFileStore(new Dictionary<CanonicalRootToken, string>
            {
                { LocalMetadataRoot, "shadow/local/metadata" },
                { LocalGeneratedRoot, "shadow/local/generated" }
            });
            PeerFileStore = new InMemoryCanonicalFileStore(new Dictionary<CanonicalRootToken, string>
            {
                { PeerMetadataRoot, "shadow/peer/metadata" },
                { PeerGeneratedRoot, "shadow/peer/generated" }
            });
        }

        public CanonicalApplyRuntimeContext MakeContext(
            CanonicalManifest localManifest, CanonicalManifest peerManifest)
        {
            return new CanonicalApplyRuntimeContext(
                localManifest, peerManifest, LocalFileStore, PeerFileStore,
                LocalMetadataRoot, PeerMetadataRoot,
                LocalGeneratedRoot, PeerGeneratedRoot);
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowApplyDivergence
    {
        none,
        failedAction,
        rollbackFailed,
        conflictRecorded
    }

    public record CanonicalShadowApplyResult : IEquatable<CanonicalShadowApplyResult>
    {
        public CanonicalApplyExecutionReport ExecutionReport { get; init; }
        public bool CalledApplySyncManifest { get; init; }
        public bool CalledArtifactApply { get; init; }
        public bool WroteProductionStore { get; init; }
        public bool WroteShadowStore { get; init; }
        public bool TombstonePhysicalDelete { get; init; }
        public bool PostconditionVerified { get; init; }
        public CanonicalRollbackResult? RollbackResult { get; init; }
        public CanonicalShadowApplyDivergence Divergence { get; init; }
    }

    public class CanonicalShadowApplyRehearsal
    {
        public CanonicalShadowApplyRehearsal() { }

        public async Task<CanonicalShadowApplyResult> RunAsync(
            CanonicalApplyPlan applyPlan,
            CanonicalLibrarySyncPlan? libraryPlan,
            CanonicalManifest localManifest,
            CanonicalManifest peerManifest,
            CanonicalShadowApplyStore? store = null)
        {
            store ??= new CanonicalShadowApplyStore();
            var report = await new CanonicalApplyExecutor().ExecuteAsync(
                applyPlan, libraryPlan,
                store.MakeContext(localManifest, peerManifest));
            var rollback = new CanonicalRollbackResult(
                "shadow-apply-rollback", true,
                report.Records.Select(r => r.ActionID).ToList());

            var divergence = report.FailedCount > 0
                ? CanonicalShadowApplyDivergence.failedAction
                : report.ConflictReport.UnresolvedCount > 0
                    ? CanonicalShadowApplyDivergence.conflictRecorded
                    : CanonicalShadowApplyDivergence.none;

            return new CanonicalShadowApplyResult
            {
                ExecutionReport = report,
                CalledApplySyncManifest = false,
                CalledArtifactApply = false,
                WroteProductionStore = false,
                WroteShadowStore = report.Records.Count > 0,
                TombstonePhysicalDelete = false,
                PostconditionVerified = report.FailedCount == 0,
                RollbackResult = rollback,
                Divergence = divergence
            };
        }
    }

    public class CanonicalShadowApplyPort : ICanonicalProductionApplyPort
    {
        public bool IsDryRunOnly => false;
        public bool MetadataApplySupported => true;
        public bool GeneratedArtifactApplySupported => true;
        public bool TombstoneApplySupported => true;
        public bool ConflictRecordSupported => true;

        private readonly ConcurrentDictionary<string, CanonicalProductionApplyResult> _results = new();
        private readonly ConcurrentDictionary<string, byte> _tombstones = new();
        private readonly ConcurrentDictionary<string, byte> _conflicts = new();

        public CanonicalShadowApplyPort() { }

        public Task<CanonicalProductionApplyResult> ApplyMetadataAsync(
            CanonicalProductionApplyExecutionRequest request)
        {
            return Task.FromResult(RecordResult(request,
                CanonicalApplyExecutionStatus.applied,
                CanonicalProductionSideEffectKind.metadataApply,
                "shadowMetadataApply"));
        }

        public Task<CanonicalProductionApplyResult> SendMetadataAsync(
            CanonicalProductionApplyExecutionRequest request)
        {
            return Task.FromResult(RecordResult(request,
                CanonicalApplyExecutionStatus.sent,
                CanonicalProductionSideEffectKind.metadataApply,
                "shadowMetadataSend"));
        }

        public Task<CanonicalProductionApplyResult> ApplyGeneratedArtifactAsync(
            CanonicalProductionApplyExecutionRequest request)
        {
            if (request.Action.Target.ArtifactID == null)
                throw new CanonicalProductionPortError.UnsupportedObjectException(
                    "shadowGeneratedArtifactMissingID");
            return Task.FromResult(RecordResult(request,
                CanonicalApplyExecutionStatus.applied,
                CanonicalProductionSideEffectKind.generatedArtifactApply,
                "shadowGeneratedArtifactApply"));
        }

        public Task<CanonicalProductionApplyResult> RequestGeneratedArtifactAsync(
            CanonicalProductionArtifactRequest request)
        {
            var action = new CanonicalApplyAction(
                CanonicalApplyActionKind.generatedArtifactDownloadApply,
                CanonicalApplySource.peer,
                new CanonicalApplyTarget(request.ObjectID, request.ArtifactID,
                    request.Kind),
                "shadowGeneratedArtifactRequest");
            return Task.FromResult(RecordResult(
                new CanonicalProductionApplyExecutionRequest(action, null),
                CanonicalApplyExecutionStatus.sent,
                CanonicalProductionSideEffectKind.generatedArtifactApply,
                "shadowGeneratedArtifactRequest"));
        }

        public Task<CanonicalProductionApplyResult> ApplyObjectTombstoneAsync(
            CanonicalProductionApplyExecutionRequest request)
        {
            _tombstones.TryAdd(request.Action.Target.ObjectID, 0);
            return Task.FromResult(RecordResult(request,
                CanonicalApplyExecutionStatus.applied,
                CanonicalProductionSideEffectKind.tombstoneMark,
                "shadowObjectTombstone"));
        }

        public Task<CanonicalProductionApplyResult> ApplyLibraryTombstoneAsync(
            CanonicalProductionApplyExecutionRequest request)
        {
            _tombstones.TryAdd(request.Action.Target.ObjectID, 0);
            return Task.FromResult(RecordResult(request,
                CanonicalApplyExecutionStatus.applied,
                CanonicalProductionSideEffectKind.tombstoneMark,
                "shadowLibraryTombstone"));
        }

        public Task<CanonicalProductionApplyResult> RecordConflictAsync(
            CanonicalProductionApplyExecutionRequest request)
        {
            _conflicts.TryAdd(
                request.Action.ConflictID ?? request.Action.ActionID, 0);
            return Task.FromResult(RecordResult(request,
                CanonicalApplyExecutionStatus.conflictRecorded,
                CanonicalProductionSideEffectKind.conflictRecord,
                "shadowConflictRecord"));
        }

        public Task<CanonicalProductionApplyPrecondition> VerifyPreconditionAsync(
            CanonicalProductionApplyPrecondition precondition)
        {
            return Task.FromResult(precondition);
        }

        public Task<CanonicalProductionApplyPostcondition> VerifyPostconditionAsync(
            CanonicalProductionApplyPostcondition postcondition)
        {
            return Task.FromResult(postcondition);
        }

        public Task<CanonicalRollbackResult> RollbackApplyAsync(
            CanonicalRollbackAction request)
        {
            if (request.CheckpointID != null)
                _results.TryRemove(request.CheckpointID, out _);
            return Task.FromResult(new CanonicalRollbackResult(
                request.CheckpointID ?? request.ActionID, true,
                new List<string> { request.ActionID }));
        }

        public async Task<CanonicalProductionApplyTrace> ProjectApplyDryRunAsync(
            CanonicalApplyAction action)
        {
            await Task.CompletedTask;
            return new CanonicalProductionApplyTrace(action, false, "shadowStoreOnly");
        }

        private CanonicalProductionApplyResult RecordResult(
            CanonicalProductionApplyExecutionRequest request,
            CanonicalApplyExecutionStatus status,
            CanonicalProductionSideEffectKind sideEffectKind,
            string summary)
        {
            var result = new CanonicalProductionApplyResult(
                request.Action.ActionID, status,
                new CanonicalProductionApplyPrecondition(
                    request.Action.ActionID, request.Action.Target, true),
                new CanonicalProductionApplyPostcondition(
                    request.Action.ActionID, request.Action.Target, true),
                new CanonicalProductionSideEffect(
                    sideEffectKind, CanonicalProductionDomain.apply,
                    request.Action.Target.ObjectID,
                    request.Action.Target.ArtifactID, summary),
                request.RollbackCheckpointID);
            _results[request.Action.ActionID] = result;
            return result;
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalExecutionShadowEventKind
    {
        canonicalExecutionShadowStarted,
        canonicalExecutionShadowCompleted,
        canonicalExecutionShadowBlocked,
        canonicalExecutionShadowFileWriteSuppressed,
        canonicalExecutionShadowFileWriteToShadowRoot,
        canonicalExecutionShadowTransportProbeSuppressed,
        canonicalExecutionShadowTransportProbeCompleted,
        canonicalExecutionShadowUploadRehearsed,
        canonicalExecutionShadowApplyRehearsed,
        canonicalExecutionShadowRollbackRehearsed,
        canonicalExecutionShadowDivergenceDetected,
        canonicalExecutionShadowEquivalent,
        canonicalExecutionShadowProductionExecuteBlocked,
        canonicalRealDataShadowCopyStarted,
        canonicalRealDataShadowCopyCompleted,
        canonicalRealDataShadowCopyFailed,
        canonicalRealDataShadowCopyVerified,
        canonicalRealDataShadowCopyCleanupStarted,
        canonicalRealDataShadowCopyCleanupCompleted,
        canonicalRealDataShadowCopyCleanupFailed,
        canonicalRealDataShadowCopyRetainedForDiagnostics,
        canonicalRealDataShadowCopyUnavailable,
        canonicalReadOnlyTransportProbeStarted,
        canonicalReadOnlyTransportProbeCompleted,
        canonicalReadOnlyTransportProbeBlocked,
        canonicalReadOnlyTransportProbeSuppressed,
        canonicalReadOnlyTransportProbeRouteRejected,
        canonicalReadOnlyTransportProbeAuthBoundaryPreserved
    }

    public record CanonicalExecutionShadowEvent : IEquatable<CanonicalExecutionShadowEvent>
    {
        public string Id => string.Join("|", new[]
        {
            Kind.ToString(), SyncRunID ?? "", Trigger.ToString(),
            NodeRole.ToString(), Mode.ToString(), Domain.ToString(),
            SideEffectClass ?? "", Reason ?? ""
        });
        public CanonicalExecutionShadowEventKind Kind { get; init; }
        public string? SyncRunID { get; init; }
        public CanonicalShadowMigrationTrigger Trigger { get; init; }
        public CanonicalProductionExecutionDomainRole NodeRole { get; init; }
        public CanonicalShadowMigrationMode Mode { get; init; }
        public CanonicalProductionDomain Domain { get; init; }
        public CanonicalShadowRootKind? ShadowRootKind { get; init; }
        public string? SideEffectClass { get; init; }
        public string? SuppressionStatus { get; init; }
        public string? Reason { get; init; }
        public int PlannedFileWriteCount { get; init; }
        public int PlannedUploadCount { get; init; }
        public int PlannedApplyCount { get; init; }
        public int DivergenceCount { get; init; }
        public CanonicalTimestamp GeneratedAt { get; init; }

        public CanonicalExecutionShadowEvent(
            CanonicalExecutionShadowEventKind kind,
            string? syncRunID,
            CanonicalShadowMigrationTrigger trigger,
            CanonicalProductionExecutionDomainRole nodeRole,
            CanonicalShadowMigrationMode mode,
            CanonicalProductionDomain domain,
            CanonicalShadowRootKind? shadowRootKind = null,
            string? sideEffectClass = null,
            string? suppressionStatus = null,
            string? reason = null,
            int plannedFileWriteCount = 0,
            int plannedUploadCount = 0,
            int plannedApplyCount = 0,
            int divergenceCount = 0,
            DateTime? generatedAt = null)
        {
            Kind = kind;
            SyncRunID = CanonicalShadowMigrationRedaction.SafeIdentifier(syncRunID);
            Trigger = trigger;
            NodeRole = nodeRole;
            Mode = mode;
            Domain = domain;
            ShadowRootKind = shadowRootKind;
            SideEffectClass = CanonicalShadowMigrationRedaction.SafeText(sideEffectClass);
            SuppressionStatus = CanonicalShadowMigrationRedaction.SafeText(suppressionStatus);
            Reason = CanonicalShadowMigrationRedaction.SafeText(reason);
            PlannedFileWriteCount = Math.Max(0, plannedFileWriteCount);
            PlannedUploadCount = Math.Max(0, plannedUploadCount);
            PlannedApplyCount = Math.Max(0, plannedApplyCount);
            DivergenceCount = Math.Max(0, divergenceCount);
            GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
        }

        public string DiagnosticsSummary => string.Join(",", new[]
        {
            $"trigger={Trigger}", $"nodeRole={NodeRole}", $"mode={Mode}",
            $"domain={Domain}", $"shadowRootKind={ShadowRootKind?.ToString() ?? "none"}",
            $"sideEffect={SideEffectClass ?? "none"}",
            $"suppression={SuppressionStatus ?? "none"}",
            $"fileWrites={PlannedFileWriteCount}", $"uploads={PlannedUploadCount}",
            $"applies={PlannedApplyCount}", $"divergences={DivergenceCount}",
            $"reason={Reason ?? "none"}"
        });
    }

    public record CanonicalExecutionShadowReport : IEquatable<CanonicalExecutionShadowReport>
    {
        public string Id => RunID;
        public string RunID { get; init; }
        public string? SyncRunID { get; init; }
        public CanonicalShadowMigrationTrigger Trigger { get; init; }
        public CanonicalProductionExecutionDomainRole NodeRole { get; init; }
        public CanonicalShadowMigrationMode Mode { get; init; }
        public CanonicalTimestamp GeneratedAt { get; init; }
        public bool DryRunEquivalent { get; init; }
        public bool Blocked { get; init; }
        public CanonicalShadowRootKind? ShadowRootKind { get; init; }
        public CanonicalShadowFileExecutionReport? FileReport { get; init; }
        public CanonicalShadowUploadResult? UploadResult { get; init; }
        public CanonicalShadowApplyResult? ApplyResult { get; init; }
        public CanonicalShadowTransportProbeResult? TransportProbeResult { get; init; }
        public CanonicalRealDataShadowCopyResult? RealDataShadowCopyResult { get; init; }
        public CanonicalShadowRootCleanupResult? ShadowRootCleanupResult { get; init; }
        public CanonicalReadOnlyTransportProbeResult? ReadOnlyTransportProbeResult { get; init; }
        public CanonicalProductionExecutionAudit? ProductionAudit { get; init; }
        public List<CanonicalExecutionShadowEvent> Events { get; init; }
        public CanonicalShadowMigrationFailure? Failure { get; init; }
        public string? FailureReason { get; init; }

        public CanonicalExecutionShadowReport(
            string runID,
            string? syncRunID,
            CanonicalShadowMigrationTrigger trigger,
            CanonicalProductionExecutionDomainRole nodeRole,
            CanonicalShadowMigrationMode mode,
            bool dryRunEquivalent,
            bool blocked,
            CanonicalShadowRootKind? shadowRootKind = null,
            CanonicalShadowFileExecutionReport? fileReport = null,
            CanonicalShadowUploadResult? uploadResult = null,
            CanonicalShadowApplyResult? applyResult = null,
            CanonicalShadowTransportProbeResult? transportProbeResult = null,
            CanonicalRealDataShadowCopyResult? realDataShadowCopyResult = null,
            CanonicalShadowRootCleanupResult? shadowRootCleanupResult = null,
            CanonicalReadOnlyTransportProbeResult? readOnlyTransportProbeResult = null,
            CanonicalProductionExecutionAudit? productionAudit = null,
            List<CanonicalExecutionShadowEvent>? events = null,
            CanonicalShadowMigrationFailure? failure = null,
            string? failureReason = null,
            DateTime? generatedAt = null)
        {
            RunID = CanonicalShadowMigrationRedaction.SafeIdentifier(runID)
                ?? "execution-shadow-run";
            SyncRunID = CanonicalShadowMigrationRedaction.SafeIdentifier(syncRunID);
            Trigger = trigger;
            NodeRole = nodeRole;
            Mode = mode;
            GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
            DryRunEquivalent = dryRunEquivalent;
            Blocked = blocked;
            ShadowRootKind = shadowRootKind;
            FileReport = fileReport;
            UploadResult = uploadResult;
            ApplyResult = applyResult;
            TransportProbeResult = transportProbeResult;
            RealDataShadowCopyResult = realDataShadowCopyResult;
            ShadowRootCleanupResult = shadowRootCleanupResult;
            ReadOnlyTransportProbeResult = readOnlyTransportProbeResult;
            ProductionAudit = productionAudit;
            Events = events ?? new List<CanonicalExecutionShadowEvent>();
            Failure = failure;
            FailureReason = CanonicalShadowMigrationRedaction.SafeText(failureReason);
        }
    }

    public record CanonicalExecutionShadowResult
    {
        public CanonicalShadowMigrationConfiguration Configuration { get; init; }
        public CanonicalShadowMigrationGate Gate { get; init; }
        public CanonicalDryRunMigrationPlan? DryRunPlan { get; init; }
        public CanonicalExecutionShadowReport Report { get; init; }
        public CanonicalShadowMigrationFailure? Failure { get; init; }
        public bool IsFatal { get; init; }
        public bool Succeeded => Failure == null;
    }

    public class CanonicalExecutionShadowPreparationRunner
    {
        public CanonicalExecutionShadowPreparationRunner() { }

        public CanonicalExecutionShadowResult Run(
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalShadowMigrationTrigger trigger,
            CanonicalProductionExecutionDomainRole nodeRole,
            CanonicalProductionDomain domain,
            CanonicalProductionSnapshot? localSnapshot,
            CanonicalProductionSnapshot? peerSnapshot,
            CanonicalProductionPortSet ports,
            CanonicalRuntimeReadinessReport? currentRuntimeReadiness = null,
            CanonicalDryRunMigrationContext? context = null,
            string? syncRunID = null,
            CanonicalShadowRootKind? shadowRootKind = null,
            CanonicalShadowFileExecutionReport? shadowFileReport = null,
            CanonicalRealDataShadowCopyResult? realDataShadowCopyResult = null,
            CanonicalShadowRootCleanupResult? shadowRootCleanupResult = null,
            CanonicalReadOnlyTransportProbeResult? readOnlyTransportProbeResult = null,
            DateTime? generatedAt = null)
        {
            var genAt = generatedAt ?? DateTime.UtcNow;
            currentRuntimeReadiness ??= CanonicalShadowMigrationRunner.DefaultRuntimeReadiness();
            context ??= new CanonicalDryRunMigrationContext();

            var gate = CanonicalShadowMigrationGate.Evaluate(
                configuration, trigger, nodeRole);
            var events = new List<CanonicalExecutionShadowEvent>
            {
                MakeEvent(CanonicalExecutionShadowEventKind.canonicalExecutionShadowStarted,
                    gate, configuration, domain, syncRunID, shadowRootKind,
                    "executionShadow", "started", gate.Reason, genAt)
            };
            events.AddRange(RealDataCopyEvents(
                realDataShadowCopyResult, gate, configuration, domain, syncRunID,
                shadowRootKind, genAt));
            events.AddRange(ReadOnlyProbeEvents(
                readOnlyTransportProbeResult, gate, configuration,
                CanonicalProductionDomain.transportRuntime, syncRunID,
                shadowRootKind, genAt));

            if (!gate.Allowed || !configuration.EffectiveMode.RunsExecutionShadowPreparation())
            {
                var kind = gate.Failure == CanonicalShadowMigrationFailure.blockedProductionExecute
                    ? CanonicalExecutionShadowEventKind.canonicalExecutionShadowProductionExecuteBlocked
                    : CanonicalExecutionShadowEventKind.canonicalExecutionShadowBlocked;
                events.Add(MakeEvent(kind, gate, configuration, domain, syncRunID,
                    shadowRootKind, "productionExecute", "blocked", gate.Reason, genAt));
                return MakeResult(configuration, gate, null, false, true, events,
                    gate.Failure, gate.Reason, shadowRootKind, shadowFileReport,
                    realDataShadowCopyResult, shadowRootCleanupResult,
                    readOnlyTransportProbeResult, null, genAt);
            }

            if (localSnapshot == null)
            {
                events.Add(BlockedEvent(gate, configuration, domain, syncRunID,
                    shadowRootKind, "insufficientLocalSnapshot", genAt));
                events.AddRange(CleanupEvents(shadowRootCleanupResult, gate,
                    configuration, domain, syncRunID, shadowRootKind, genAt));
                return MakeResult(configuration, gate, null, false, true, events,
                    CanonicalShadowMigrationFailure.insufficientLocalSnapshot,
                    "insufficientLocalSnapshot", shadowRootKind, shadowFileReport,
                    realDataShadowCopyResult, shadowRootCleanupResult,
                    readOnlyTransportProbeResult, null, genAt);
            }
            if (peerSnapshot == null)
            {
                events.Add(BlockedEvent(gate, configuration, domain, syncRunID,
                    shadowRootKind, "insufficientPeerSnapshot", genAt));
                events.AddRange(CleanupEvents(shadowRootCleanupResult, gate,
                    configuration, domain, syncRunID, shadowRootKind, genAt));
                return MakeResult(configuration, gate, null, false, true, events,
                    CanonicalShadowMigrationFailure.insufficientPeerSnapshot,
                    "insufficientPeerSnapshot", shadowRootKind, shadowFileReport,
                    realDataShadowCopyResult, shadowRootCleanupResult,
                    readOnlyTransportProbeResult, null, genAt);
            }

            try
            {
                var plan = new CanonicalDryRunMigrationPlanner().Plan(
                    localSnapshot, peerSnapshot, ports, currentRuntimeReadiness,
                    CanonicalShadowMigrationTrigger.periodic, context, genAt);
                var dryRunEquivalent = plan.EquivalenceReport.LegacyEquivalence.HasBlockingDivergence == false;
                var counts = PlannedCounts(plan);

                events.Add(FileEvent(configuration.EffectiveMode, gate, domain,
                    syncRunID, shadowRootKind, shadowFileReport, counts.FileWrites, genAt));
                events.Add(TransportEvent(configuration, gate, domain, syncRunID,
                    shadowRootKind, genAt));
                events.Add(MakeEvent(CanonicalExecutionShadowEventKind.canonicalExecutionShadowUploadRehearsed,
                    gate, configuration, CanonicalProductionDomain.uploadRuntime,
                    syncRunID, shadowRootKind, "upload", "shadowReceiverOnly",
                    "productionUploadSuppressed", genAt, counts.Uploads));
                events.Add(MakeEvent(CanonicalExecutionShadowEventKind.canonicalExecutionShadowApplyRehearsed,
                    gate, configuration, CanonicalProductionDomain.apply,
                    syncRunID, shadowRootKind, "apply", "shadowStoreOnly",
                    "applySyncManifestSuppressed", genAt, applyCount: counts.Applies));
                events.Add(MakeEvent(CanonicalExecutionShadowEventKind.canonicalExecutionShadowRollbackRehearsed,
                    gate, configuration, CanonicalProductionDomain.fileRuntime,
                    syncRunID, shadowRootKind, "rollback", "shadowOnly",
                    "rollbackAvailable", genAt, counts.FileWrites));
                events.Add(MakeEvent(
                    dryRunEquivalent
                        ? CanonicalExecutionShadowEventKind.canonicalExecutionShadowEquivalent
                        : CanonicalExecutionShadowEventKind.canonicalExecutionShadowDivergenceDetected,
                    gate, configuration, domain, syncRunID, shadowRootKind,
                    "dryRunEquivalence", dryRunEquivalent ? "equivalent" : "divergent",
                    dryRunEquivalent ? "equivalent" : "blockingDivergence", genAt,
                    divergenceCount: plan.EquivalenceReport.LegacyEquivalence.Divergences.Count));
                events.Add(MakeEvent(CanonicalExecutionShadowEventKind.canonicalExecutionShadowCompleted,
                    gate, configuration, domain, syncRunID, shadowRootKind,
                    "executionShadow", "completed",
                    configuration.EffectiveMode.NoSideEffectReason(), genAt,
                    counts.FileWrites, counts.Uploads, counts.Applies,
                    plan.EquivalenceReport.LegacyEquivalence.Divergences.Count));
                events.AddRange(CleanupEvents(shadowRootCleanupResult, gate,
                    configuration, domain, syncRunID, shadowRootKind, genAt));

                var audit = CanonicalProductionExecutionGuard.EvaluateShadow(
                    configuration.EffectiveMode.KernelShadowMode(),
                    new CanonicalProductionExecutionToken(
                        configuration.EffectiveMode.KernelShadowMode(),
                        new List<CanonicalProductionDomain>
                        {
                            CanonicalProductionDomain.fileRuntime,
                            CanonicalProductionDomain.transportRuntime,
                            CanonicalProductionDomain.uploadRuntime,
                            CanonicalProductionDomain.apply
                        },
                        nodeRole, syncRunID ?? context.DryRunID),
                    new List<CanonicalProductionDomain>
                    {
                        CanonicalProductionDomain.fileRuntime,
                        CanonicalProductionDomain.transportRuntime,
                        CanonicalProductionDomain.uploadRuntime,
                        CanonicalProductionDomain.apply
                    },
                    new CanonicalRollbackPlan("execution-shadow-rollback",
                        new List<string>(), new List<string>()),
                    plan.EquivalenceReport,
                    plan.Blockers.Count(b => b.Kind ==
                        CanonicalProductionBlockerKind.unresolvedConflict),
                    genAt);

                return MakeResult(configuration, gate, plan, dryRunEquivalent, false,
                    events, null, null, shadowRootKind, shadowFileReport,
                    realDataShadowCopyResult, shadowRootCleanupResult,
                    readOnlyTransportProbeResult, audit, genAt);
            }
            catch (Exception ex)
            {
                events.Add(BlockedEvent(gate, configuration, domain, syncRunID,
                    shadowRootKind, "dryRunFailed", genAt));
                events.AddRange(CleanupEvents(shadowRootCleanupResult, gate,
                    configuration, domain, syncRunID, shadowRootKind, genAt));
                return MakeResult(configuration, gate, null, false, true, events,
                    CanonicalShadowMigrationFailure.dryRunFailed,
                    ex.ToString(), shadowRootKind, shadowFileReport,
                    realDataShadowCopyResult, shadowRootCleanupResult,
                    readOnlyTransportProbeResult, null, genAt);
            }
        }

        private CanonicalExecutionShadowResult MakeResult(
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalShadowMigrationGate gate,
            CanonicalDryRunMigrationPlan? plan,
            bool dryRunEquivalent, bool blocked,
            List<CanonicalExecutionShadowEvent> events,
            CanonicalShadowMigrationFailure? failure,
            string? failureReason,
            CanonicalShadowRootKind? shadowRootKind,
            CanonicalShadowFileExecutionReport? shadowFileReport,
            CanonicalRealDataShadowCopyResult? realDataShadowCopyResult,
            CanonicalShadowRootCleanupResult? shadowRootCleanupResult,
            CanonicalReadOnlyTransportProbeResult? readOnlyTransportProbeResult,
            CanonicalProductionExecutionAudit? productionAudit,
            DateTime generatedAt)
        {
            var boundedEvents = events.Take(configuration.Policy.MaxDiagnosticsEvents).ToList();
            var report = new CanonicalExecutionShadowReport(
                plan?.DryRunID ?? gate.Reason,
                boundedEvents.FirstOrDefault()?.SyncRunID,
                gate.Trigger, gate.NodeRole, gate.Mode,
                dryRunEquivalent, blocked, shadowRootKind, shadowFileReport,
                null, null, null, realDataShadowCopyResult,
                shadowRootCleanupResult, readOnlyTransportProbeResult,
                productionAudit, boundedEvents, failure, failureReason,
                generatedAt);
            return new CanonicalExecutionShadowResult
            {
                Configuration = configuration,
                Gate = gate,
                DryRunPlan = plan,
                Report = report,
                Failure = failure,
                IsFatal = failure != null && configuration.Policy.FailureIsFatal
            };
        }

        private static (int FileWrites, int Uploads, int Applies) PlannedCounts(
            CanonicalDryRunMigrationPlan plan)
        {
            var uploads = plan.SyncPlan.UploadAudioArtifact.Count;
            var applies = plan.ApplyPlan.Actions.Count + plan.LibraryPlan.ApplyActions.Count;
            var fileWrites = plan.ApplyPlan.Actions.Count(a =>
            {
                return a.Kind switch
                {
                    CanonicalApplyActionKind.recordingMetadataApply
                        or CanonicalApplyActionKind.folderMetadataApply
                        or CanonicalApplyActionKind.studyItemMetadataApply
                        or CanonicalApplyActionKind.generatedArtifactDownloadApply
                        or CanonicalApplyActionKind.objectTombstoneApply
                        or CanonicalApplyActionKind.libraryTombstoneApply
                        or CanonicalApplyActionKind.artifactTombstoneApply => true,
                    _ => false
                };
            }) + plan.LibraryPlan.ApplyActions.Count;
            return (fileWrites, uploads, applies);
        }

        private CanonicalExecutionShadowEvent FileEvent(
            CanonicalShadowMigrationMode mode,
            CanonicalShadowMigrationGate gate,
            CanonicalProductionDomain domain,
            string? syncRunID,
            CanonicalShadowRootKind? shadowRootKind,
            CanonicalShadowFileExecutionReport? shadowFileReport,
            int plannedCount,
            DateTime generatedAt)
        {
            if (mode == CanonicalShadowMigrationMode.executionShadowWithShadowFileStore
                && shadowFileReport?.WroteToShadowRoot == true)
            {
                return MakeEvent(
                    CanonicalExecutionShadowEventKind.canonicalExecutionShadowFileWriteToShadowRoot,
                    gate, new CanonicalShadowMigrationConfiguration(true, mode),
                    CanonicalProductionDomain.fileRuntime, syncRunID, shadowRootKind,
                    "fileWrite", "shadowRootOnly", "wroteToShadowRoot", generatedAt,
                    plannedCount);
            }
            return MakeEvent(
                CanonicalExecutionShadowEventKind.canonicalExecutionShadowFileWriteSuppressed,
                gate, new CanonicalShadowMigrationConfiguration(true, mode),
                domain, syncRunID, shadowRootKind, "fileWrite", "suppressed",
                mode == CanonicalShadowMigrationMode.executionShadowWithShadowFileStore
                    ? "missingShadowSourceBytes" : "fileWriteSuppressed",
                generatedAt, plannedCount);
        }

        private CanonicalExecutionShadowEvent TransportEvent(
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalShadowMigrationGate gate,
            CanonicalProductionDomain domain,
            string? syncRunID,
            CanonicalShadowRootKind? shadowRootKind,
            DateTime generatedAt)
        {
            if (configuration.EffectiveMode !=
                CanonicalShadowMigrationMode.executionShadowWithReadOnlyTransportProbe)
            {
                return MakeEvent(
                    CanonicalExecutionShadowEventKind.canonicalExecutionShadowTransportProbeSuppressed,
                    gate, configuration, CanonicalProductionDomain.transportRuntime,
                    syncRunID, shadowRootKind, "transport", "suppressed",
                    "transportProbeNotRequested", generatedAt);
            }
            var decision = configuration.Policy.NetworkProbePolicy.DecisionFor(
                new CanonicalShadowNetworkProbeRequest(
                    CanonicalShadowNetworkProbeKind.syncInventoryReadOnly,
                    "/sync/inventory"));
            return MakeEvent(
                decision.Accepted
                    ? CanonicalExecutionShadowEventKind.canonicalExecutionShadowTransportProbeCompleted
                    : CanonicalExecutionShadowEventKind.canonicalExecutionShadowTransportProbeSuppressed,
                gate, configuration, CanonicalProductionDomain.transportRuntime,
                syncRunID, shadowRootKind, "transport",
                decision.Accepted ? "readOnlyProbeAccepted" : "suppressed",
                decision.Accepted ? "readOnlyEnvelopeOnly" : decision.Reason,
                generatedAt);
        }

        private List<CanonicalExecutionShadowEvent> RealDataCopyEvents(
            CanonicalRealDataShadowCopyResult? result,
            CanonicalShadowMigrationGate gate,
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalProductionDomain domain,
            string? syncRunID,
            CanonicalShadowRootKind? shadowRootKind,
            DateTime generatedAt)
        {
            if (result == null)
            {
                if (configuration.Policy.RealDataShadowCopyPolicy.IsEnabled
                    && configuration.EffectiveMode ==
                        CanonicalShadowMigrationMode.executionShadowWithShadowFileStore)
                {
                    return new List<CanonicalExecutionShadowEvent>
                    {
                        MakeEvent(
                            CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyUnavailable,
                            gate, configuration, CanonicalProductionDomain.fileRuntime,
                            syncRunID, shadowRootKind, "realDataShadowCopy",
                            "unavailable", "realDataShadowCopyUnavailable", generatedAt)
                    };
                }
                return new List<CanonicalExecutionShadowEvent>();
            }

            var events = new List<CanonicalExecutionShadowEvent>
            {
                MakeEvent(CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyStarted,
                    gate, configuration, CanonicalProductionDomain.fileRuntime,
                    syncRunID, shadowRootKind, "realDataShadowCopy", "started",
                    result.DiagnosticsSummary, generatedAt,
                    result.CopiedEntryCount)
            };

            if (result.Completed)
            {
                events.Add(MakeEvent(
                    CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyCompleted,
                    gate, configuration, CanonicalProductionDomain.fileRuntime,
                    syncRunID, shadowRootKind, "realDataShadowCopy",
                    "shadowRootOnly", result.DiagnosticsSummary, generatedAt,
                    result.CopiedEntryCount));
                events.Add(MakeEvent(
                    CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyVerified,
                    gate, configuration, CanonicalProductionDomain.fileRuntime,
                    syncRunID, shadowRootKind, "copyVerification",
                    result.VerificationStatus, result.DiagnosticsSummary, generatedAt,
                    result.CopiedEntryCount));
            }
            else if (result.Unavailable)
            {
                events.Add(MakeEvent(
                    CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyUnavailable,
                    gate, configuration, CanonicalProductionDomain.fileRuntime,
                    syncRunID, shadowRootKind, "realDataShadowCopy",
                    "unavailable",
                    result.FailureReason ?? result.Failure?.ToString(),
                    generatedAt));
            }
            else
            {
                events.Add(MakeEvent(
                    CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyFailed,
                    gate, configuration, CanonicalProductionDomain.fileRuntime,
                    syncRunID, shadowRootKind, "realDataShadowCopy", "failed",
                    result.FailureReason ?? result.Failure?.ToString(), generatedAt,
                    result.CopiedEntryCount));
            }
            return events;
        }

        private List<CanonicalExecutionShadowEvent> CleanupEvents(
            CanonicalShadowRootCleanupResult? result,
            CanonicalShadowMigrationGate gate,
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalProductionDomain domain,
            string? syncRunID,
            CanonicalShadowRootKind? shadowRootKind,
            DateTime generatedAt)
        {
            if (result == null) return new List<CanonicalExecutionShadowEvent>();
            var completedKind = result.Status switch
            {
                CanonicalShadowRootCleanupStatus.removed =>
                    CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyCleanupCompleted,
                CanonicalShadowRootCleanupStatus.retainedForDiagnostics
                    or CanonicalShadowRootCleanupStatus.retainedForNextLaunch =>
                    CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyRetainedForDiagnostics,
                _ => CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyCleanupFailed
            };
            return new List<CanonicalExecutionShadowEvent>
            {
                MakeEvent(CanonicalExecutionShadowEventKind.canonicalRealDataShadowCopyCleanupStarted,
                    gate, configuration, CanonicalProductionDomain.fileRuntime,
                    syncRunID, shadowRootKind, "shadowRootCleanup", "started",
                    result.RootID, generatedAt),
                MakeEvent(completedKind, gate, configuration,
                    CanonicalProductionDomain.fileRuntime, syncRunID, shadowRootKind,
                    "shadowRootCleanup", result.Status.ToString(),
                    result.DiagnosticsSummary, generatedAt)
            };
        }

        private List<CanonicalExecutionShadowEvent> ReadOnlyProbeEvents(
            CanonicalReadOnlyTransportProbeResult? result,
            CanonicalShadowMigrationGate gate,
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalProductionDomain domain,
            string? syncRunID,
            CanonicalShadowRootKind? shadowRootKind,
            DateTime generatedAt)
        {
            if (result == null)
            {
                if (configuration.Policy.ReadOnlyTransportProbePolicy.IsEnabled
                    && configuration.EffectiveMode ==
                        CanonicalShadowMigrationMode.executionShadowWithReadOnlyTransportProbe)
                {
                    return new List<CanonicalExecutionShadowEvent>
                    {
                        MakeEvent(
                            CanonicalExecutionShadowEventKind.canonicalReadOnlyTransportProbeSuppressed,
                            gate, configuration, domain, syncRunID, shadowRootKind,
                            "readOnlyTransportProbe", "suppressed",
                            "readOnlyProbeUnavailable", generatedAt)
                    };
                }
                return new List<CanonicalExecutionShadowEvent>();
            }

            var events = new List<CanonicalExecutionShadowEvent>
            {
                MakeEvent(CanonicalExecutionShadowEventKind.canonicalReadOnlyTransportProbeStarted,
                    gate, configuration, domain, syncRunID, shadowRootKind,
                    "readOnlyTransportProbe", "started",
                    result.DiagnosticsSummary, generatedAt)
            };

            var mainKind = result.Blocked
                ? (result.RouteStatus ==
                        CanonicalReadOnlyTransportProbeRouteStatus.rejectedMutating
                    || result.RouteStatus ==
                        CanonicalReadOnlyTransportProbeRouteStatus.rejectedUnknown
                    ? CanonicalExecutionShadowEventKind.canonicalReadOnlyTransportProbeRouteRejected
                    : CanonicalExecutionShadowEventKind.canonicalReadOnlyTransportProbeBlocked)
                : result.Suppressed
                    ? CanonicalExecutionShadowEventKind.canonicalReadOnlyTransportProbeSuppressed
                    : CanonicalExecutionShadowEventKind.canonicalReadOnlyTransportProbeCompleted;

            events.Add(MakeEvent(mainKind, gate, configuration, domain, syncRunID,
                shadowRootKind, "readOnlyTransportProbe",
                result.RouteStatus.ToString(),
                result.DiagnosticsSummary, generatedAt));

            if (result.AuthBoundaryPreserved)
            {
                events.Add(MakeEvent(
                    CanonicalExecutionShadowEventKind.canonicalReadOnlyTransportProbeAuthBoundaryPreserved,
                    gate, configuration, domain, syncRunID, shadowRootKind,
                    "transportAuth", "preserved",
                    "tlsHmacNonceTimestampBodyHashPreserved", generatedAt));
            }
            return events;
        }

        private CanonicalExecutionShadowEvent BlockedEvent(
            CanonicalShadowMigrationGate gate,
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalProductionDomain domain,
            string? syncRunID,
            CanonicalShadowRootKind? shadowRootKind,
            string reason,
            DateTime generatedAt)
        {
            return MakeEvent(CanonicalExecutionShadowEventKind.canonicalExecutionShadowBlocked,
                gate, configuration, domain, syncRunID, shadowRootKind,
                "executionShadow", "blocked", reason, generatedAt);
        }

        private static CanonicalExecutionShadowEvent MakeEvent(
            CanonicalExecutionShadowEventKind kind,
            CanonicalShadowMigrationGate gate,
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalProductionDomain domain,
            string? syncRunID,
            CanonicalShadowRootKind? shadowRootKind,
            string? sideEffectClass,
            string? suppressionStatus,
            string? reason,
            DateTime generatedAt,
            int plannedFileWriteCount = 0,
            int plannedUploadCount = 0,
            int plannedApplyCount = 0,
            int divergenceCount = 0)
        {
            return new CanonicalExecutionShadowEvent(
                kind, syncRunID, gate.Trigger, gate.NodeRole,
                configuration.EffectiveMode, domain, shadowRootKind,
                sideEffectClass, suppressionStatus, reason,
                plannedFileWriteCount, plannedUploadCount,
                plannedApplyCount, divergenceCount, generatedAt);
        }
    }
}
