using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadCutoverDomain
    {
        audioUpload
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadActionKind
    {
        audioUploadNoOp,
        audioUploadShadowRehearsal,
        audioUploadCanaryCandidate,
        audioUploadConflictRecord,
        audioUploadDeferredPeerUnknown,
        unsupported
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadPeerState
    {
        unknown,
        missing,
        metadataOnly,
        available,
        different,
        deleted
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadEvidenceStatus
    {
        complete,
        blocked,
        conflict,
        deferred,
        disabled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadEvidenceBlocker
    {
        localAudioMissing,
        localHashUnavailable,
        localByteSizeUnavailable,
        peerTruthMissing,
        peerUnknown,
        completedLedgerWithoutPeerMatch,
        metadataUploadedNotAudioProof,
        receiveRecordNotAudioProof,
        uiUploadedNotAudioProof,
        viewRefreshSuppressed,
        retryDrainerFreshJobSuppressed,
        manualUploadButtonLegacyOwned,
        differentHashOrSize,
        productionUploadSuppressed,
        canaryStageBlocked,
        unsupportedProductionCommit,
        shadowRehearsalFailed
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadLedgerPhase
    {
        none,
        queued,
        inFlight,
        finalizing,
        completed,
        failed,
        retryPending,
        fatalFailed
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadTriggerSource
    {
        manualUploadButton,
        retryDrainer,
        manualSyncIPhone,
        manualSyncMacHint,
        periodicSync,
        appActivationRefresh,
        viewRefresh,
        ordinarySync
    }

    public static class CanonicalAudioUploadTriggerSourceExtensions
    {
        public static CanonicalAudioUploadTriggerSource From(CanonicalSyncPlanTrigger trigger)
        {
            return trigger switch
            {
                CanonicalSyncPlanTrigger.manual => CanonicalAudioUploadTriggerSource.manualSyncIPhone,
                CanonicalSyncPlanTrigger.periodic => CanonicalAudioUploadTriggerSource.periodicSync,
                CanonicalSyncPlanTrigger.appActivation => CanonicalAudioUploadTriggerSource.appActivationRefresh,
                CanonicalSyncPlanTrigger.retryDrainer => CanonicalAudioUploadTriggerSource.retryDrainer,
                CanonicalSyncPlanTrigger.viewRefresh => CanonicalAudioUploadTriggerSource.viewRefresh,
                _ => CanonicalAudioUploadTriggerSource.ordinarySync
            };
        }

        public static CanonicalSyncPlanTrigger CanonicalSyncPlanTrigger(this CanonicalAudioUploadTriggerSource source)
        {
            return source switch
            {
                CanonicalAudioUploadTriggerSource.manualUploadButton or CanonicalAudioUploadTriggerSource.manualSyncIPhone or CanonicalAudioUploadTriggerSource.manualSyncMacHint
                    => CanonicalSyncPlanTrigger.manual,
                CanonicalAudioUploadTriggerSource.periodicSync or CanonicalAudioUploadTriggerSource.ordinarySync
                    => CanonicalSyncPlanTrigger.periodic,
                CanonicalAudioUploadTriggerSource.appActivationRefresh
                    => CanonicalSyncPlanTrigger.appActivation,
                CanonicalAudioUploadTriggerSource.retryDrainer
                    => CanonicalSyncPlanTrigger.retryDrainer,
                CanonicalAudioUploadTriggerSource.viewRefresh
                    => CanonicalSyncPlanTrigger.viewRefresh,
                _ => CanonicalSyncPlanTrigger.periodic
            };
        }

        public static bool IsViewRefresh(this CanonicalAudioUploadTriggerSource source) => source == CanonicalAudioUploadTriggerSource.viewRefresh;
        public static bool IsRetryDrainer(this CanonicalAudioUploadTriggerSource source) => source == CanonicalAudioUploadTriggerSource.retryDrainer;
        public static bool IsExplicitManualUploadButton(this CanonicalAudioUploadTriggerSource source) => source == CanonicalAudioUploadTriggerSource.manualUploadButton;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadNodeRole
    {
        iPhone,
        mac,
        testHarness
    }

    public record CanonicalAudioUploadLocalTruth : IEquatable<CanonicalAudioUploadLocalTruth>
    {
        public bool AudioAvailable { get; init; }
        public CanonicalHash? ContentHash { get; init; }
        public long? ByteSize { get; init; }
        public CanonicalTimestamp? ModifiedAt { get; init; }
        public string? LogicalPathToken { get; init; }
        public string? SourceDeviceID { get; init; }
        public string? DiagnosticsSummary { get; init; }

        public CanonicalAudioUploadLocalTruth(
            bool audioAvailable,
            CanonicalHash? contentHash = null,
            long? byteSize = null,
            CanonicalTimestamp? modifiedAt = null,
            string? logicalPathToken = null,
            string? sourceDeviceID = null,
            string? diagnosticsSummary = null)
        {
            AudioAvailable = audioAvailable;
            ContentHash = contentHash;
            ByteSize = byteSize.HasValue && byteSize.Value < 0 ? null : byteSize;
            ModifiedAt = modifiedAt;
            LogicalPathToken = CanonicalProjectionContract.SafeLogicalPathToken(logicalPathToken);
            SourceDeviceID = SafeText(sourceDeviceID);
            DiagnosticsSummary = SafeText(diagnosticsSummary);
        }

        public static CanonicalAudioUploadLocalTruth Available(
            CanonicalHash hash,
            long byteSize,
            string? logicalPathToken = null,
            string? sourceDeviceID = null)
        {
            return new CanonicalAudioUploadLocalTruth(
                audioAvailable: true,
                contentHash: hash,
                byteSize: byteSize,
                logicalPathToken: logicalPathToken,
                sourceDeviceID: sourceDeviceID);
        }

        public static CanonicalAudioUploadLocalTruth From(CanonicalRecordingObject obj)
        {
            var audio = obj.AudioArtifact;
            var available = audio?.Availability == CanonicalArtifactAvailability.available
                || audio?.Availability == CanonicalArtifactAvailability.availableWithoutHash;
            return new CanonicalAudioUploadLocalTruth(
                audioAvailable: available && audio?.Tombstone != true,
                contentHash: audio?.ContentHash,
                byteSize: audio?.ByteSize,
                modifiedAt: audio?.ModifiedAt,
                logicalPathToken: audio?.LogicalPathToken,
                sourceDeviceID: obj.NodeID,
                diagnosticsSummary: $"availability={audio?.Availability?.ToString() ?? "missing"}");
        }

        public bool HashAvailable => ContentHash != null;
        public bool ByteSizeAvailable => ByteSize != null;
        public bool SufficientForUploadCandidate => AudioAvailable && HashAvailable && ByteSizeAvailable;

        private static string? SafeText(string? value)
        {
            if (value == null) return null;
            var trimmed = value.Trim();
            if (trimmed.Length == 0) return null;
            return trimmed.Length > 160 ? trimmed[..160] : trimmed;
        }
    }

    public record CanonicalAudioUploadPeerTruth : IEquatable<CanonicalAudioUploadPeerTruth>
    {
        public CanonicalAudioUploadPeerState State { get; init; }
        public CanonicalHash? ContentHash { get; init; }
        public long? ByteSize { get; init; }
        public bool ReceiveRecordExists { get; init; }
        public bool MetadataUploaded { get; init; }
        public bool UiUploaded { get; init; }
        public string? DiagnosticsSummary { get; init; }

        public CanonicalAudioUploadPeerTruth(
            CanonicalAudioUploadPeerState state,
            CanonicalHash? contentHash = null,
            long? byteSize = null,
            bool receiveRecordExists = false,
            bool metadataUploaded = false,
            bool uiUploaded = false,
            string? diagnosticsSummary = null)
        {
            State = state;
            ContentHash = contentHash;
            ByteSize = byteSize.HasValue && byteSize.Value < 0 ? null : byteSize;
            ReceiveRecordExists = receiveRecordExists;
            MetadataUploaded = metadataUploaded;
            UiUploaded = uiUploaded;
            DiagnosticsSummary = SafeText(diagnosticsSummary);
        }

        public static CanonicalAudioUploadPeerTruth From(CanonicalRecordingObject? obj)
        {
            if (obj == null)
                return new CanonicalAudioUploadPeerTruth(CanonicalAudioUploadPeerState.missing, diagnosticsSummary: "peerObjectMissing");

            var audio = obj.AudioArtifact;
            if (audio == null)
                return new CanonicalAudioUploadPeerTruth(CanonicalAudioUploadPeerState.metadataOnly, diagnosticsSummary: "peerMetadataOnly");

            var state = audio.Availability switch
            {
                CanonicalArtifactAvailability.unknown => CanonicalAudioUploadPeerState.unknown,
                CanonicalArtifactAvailability.missing => CanonicalAudioUploadPeerState.missing,
                CanonicalArtifactAvailability.availableWithoutHash or CanonicalArtifactAvailability.available
                    => audio.Tombstone == true ? CanonicalAudioUploadPeerState.deleted : CanonicalAudioUploadPeerState.available,
                _ => CanonicalAudioUploadPeerState.unknown
            };

            return new CanonicalAudioUploadPeerTruth(
                state: state,
                contentHash: audio.ContentHash,
                byteSize: audio.ByteSize,
                diagnosticsSummary: $"availability={audio.Availability}");
        }

        public bool PeerTruthSufficientForNoOp(CanonicalAudioUploadLocalTruth local)
        {
            if (State != CanonicalAudioUploadPeerState.available) return false;
            if (ContentHash == null || ByteSize == null) return false;
            if (local.ContentHash == null || local.ByteSize == null) return false;
            return ContentHash == local.ContentHash && ByteSize == local.ByteSize;
        }

        private static string? SafeText(string? value)
        {
            if (value == null) return null;
            var trimmed = value.Trim();
            if (trimmed.Length == 0) return null;
            return trimmed.Length > 160 ? trimmed[..160] : trimmed;
        }
    }

    public record CanonicalAudioUploadLedgerTruth : IEquatable<CanonicalAudioUploadLedgerTruth>
    {
        public CanonicalAudioUploadLedgerPhase Phase { get; init; }
        public CanonicalHash? ContentHash { get; init; }
        public long? ByteSize { get; init; }
        public bool MetadataUploaded { get; init; }
        public bool UiUploaded { get; init; }
        public bool ReceiveRecordExists { get; init; }

        public CanonicalAudioUploadLedgerTruth(
            CanonicalAudioUploadLedgerPhase phase = CanonicalAudioUploadLedgerPhase.none,
            CanonicalHash? contentHash = null,
            long? byteSize = null,
            bool metadataUploaded = false,
            bool uiUploaded = false,
            bool receiveRecordExists = false)
        {
            Phase = phase;
            ContentHash = contentHash;
            ByteSize = byteSize.HasValue && byteSize.Value < 0 ? null : byteSize;
            MetadataUploaded = metadataUploaded;
            UiUploaded = uiUploaded;
            ReceiveRecordExists = receiveRecordExists;
        }
    }

    public record CanonicalAudioUploadRetryTruth : IEquatable<CanonicalAudioUploadRetryTruth>
    {
        public bool HasExistingEligibleRetry { get; init; }
        public bool RetryPending { get; init; }
        public bool CanFreshCreateJob { get; init; }

        public CanonicalAudioUploadRetryTruth(
            bool hasExistingEligibleRetry = false,
            bool retryPending = false,
            bool canFreshCreateJob = false)
        {
            HasExistingEligibleRetry = hasExistingEligibleRetry;
            RetryPending = retryPending;
            CanFreshCreateJob = canFreshCreateJob;
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadDiagnosticKind
    {
        canonicalAudioUploadCutoverGateEvaluated,
        canonicalAudioUploadCutoverGateAllowed,
        canonicalAudioUploadCutoverGateBlocked,
        canonicalAudioUploadEvidenceBuilt,
        canonicalAudioUploadEvidenceBlocked,
        canonicalAudioUploadPeerSameNoOp,
        canonicalAudioUploadPeerUnknownDeferred,
        canonicalAudioUploadConflictDetected,
        canonicalAudioUploadCompletedLedgerRejectedAsNoOp,
        canonicalAudioUploadMetadataUploadedRejectedAsAudioProof,
        canonicalAudioUploadReceiveRecordRejectedAsAudioProof,
        canonicalAudioUploadUIUploadedRejectedAsAudioProof,
        canonicalAudioUploadViewRefreshSuppressed,
        canonicalAudioUploadRetryDrainerFreshJobSuppressed,
        canonicalAudioUploadManualButtonLegacyOwned,
        canonicalAudioUploadShadowRehearsalStarted,
        canonicalAudioUploadShadowRehearsalCompleted,
        canonicalAudioUploadShadowReceiverWrote,
        canonicalAudioUploadShadowReceiverNoOp,
        canonicalAudioUploadNoCommitStarted,
        canonicalAudioUploadNoCommitCompleted,
        canonicalAudioUploadProductionCoordinatorSuppressed,
        canonicalAudioUploadRecordingUploadClientSuppressed,
        canonicalAudioUploadSecureMacUploadClientSuppressed,
        canonicalAudioUploadInboxWriteSuppressed,
        canonicalAudioUploadReceiveJSONWriteSuppressed,
        canonicalAudioUploadLedgerMutationSuppressed,
        canonicalAudioUploadRetryDrainerMutationSuppressed,
        canonicalAudioUploadLegacyFallbackPreserved,
        canonicalAudioUploadRuntimeModeEvaluated,
        canonicalAudioUploadRuntimeCandidateSelected,
        canonicalAudioUploadRuntimeStarted,
        canonicalAudioUploadRuntimeChunkSent,
        canonicalAudioUploadRuntimeChunkConfirmed,
        canonicalAudioUploadRuntimeResumeStarted,
        canonicalAudioUploadRuntimeFinalizeStarted,
        canonicalAudioUploadRuntimeFinalizeCompleted,
        canonicalAudioUploadRuntimeFinalizeFailed,
        canonicalAudioUploadRuntimeRetryScheduled,
        canonicalAudioUploadRuntimeLegacyFallbackUsed,
        canonicalAudioUploadRuntimePeerUnknownDeferred,
        canonicalAudioUploadRuntimeConflictBlocked,
        canonicalAudioUploadRuntimeExistingDifferentAudioBlocked,
        canonicalAudioUploadRuntimeCompletedLedgerRejectedAsNoOp,
        canonicalAudioUploadReadSideProjectionStarted,
        canonicalAudioUploadReadSideProjectionEquivalent,
        canonicalAudioUploadReadSideProjectionDiverged,
        canonicalAudioUploadAbortRollbackPolicyEvaluated
    }

    public record CanonicalAudioUploadDiagnostic : IEquatable<CanonicalAudioUploadDiagnostic>
    {
        public CanonicalAudioUploadDiagnosticKind Kind { get; init; }
        public string? SyncRunID { get; init; }
        public CanonicalAudioUploadTriggerSource Trigger { get; init; }
        public CanonicalAudioUploadNodeRole NodeRole { get; init; }
        public CanonicalAudioUploadCutoverDomain Domain { get; init; }
        public string? ObjectID { get; init; }
        public CanonicalAudioUploadPeerState? PeerState { get; init; }
        public CanonicalAudioUploadLedgerPhase? LedgerPhase { get; init; }
        public CanonicalAudioUploadActionKind? Action { get; init; }
        public string? Result { get; init; }
        public string? Reason { get; init; }
        public string? HashPrefix { get; init; }

        public CanonicalAudioUploadDiagnostic(
            CanonicalAudioUploadDiagnosticKind kind,
            string? syncRunID = null,
            CanonicalAudioUploadTriggerSource trigger = default,
            CanonicalAudioUploadNodeRole nodeRole = default,
            CanonicalAudioUploadCutoverDomain domain = CanonicalAudioUploadCutoverDomain.audioUpload,
            string? objectID = null,
            CanonicalAudioUploadPeerState? peerState = null,
            CanonicalAudioUploadLedgerPhase? ledgerPhase = null,
            CanonicalAudioUploadActionKind? action = null,
            string? result = null,
            string? reason = null,
            string? hashPrefix = null)
        {
            Kind = kind;
            SyncRunID = SafeText(syncRunID, 96);
            Trigger = trigger;
            NodeRole = nodeRole;
            Domain = domain;
            ObjectID = SafeText(objectID, 96);
            PeerState = peerState;
            LedgerPhase = ledgerPhase;
            Action = action;
            Result = SafeText(result, 96);
            Reason = SafeText(reason, 160);
            HashPrefix = hashPrefix != null ? (hashPrefix.Length > 12 ? hashPrefix[..12] : hashPrefix) : null;
        }

        public string DiagnosticsSummary
        {
            get
            {
                var parts = new List<string?>
                {
                    $"trigger={Trigger}",
                    $"nodeRole={NodeRole}",
                    $"domain={Domain}",
                    ObjectID != null ? $"objectID={ObjectID}" : null,
                    PeerState != null ? $"peerState={PeerState}" : null,
                    LedgerPhase != null ? $"ledgerPhase={LedgerPhase}" : null,
                    Action != null ? $"action={Action}" : null,
                    Result != null ? $"result={Result}" : null,
                    Reason != null ? $"reason={Reason}" : null,
                    HashPrefix != null ? $"hashPrefix={HashPrefix}" : null
                };
                return string.Join(",", parts.Where(p => p != null));
            }
        }

        private static string? SafeText(string? value, int maxLength)
        {
            if (value == null) return null;
            var trimmed = value.Trim();
            if (trimmed.Length == 0) return null;
            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }
    }

    public record CanonicalAudioUploadCutoverCandidate : IEquatable<CanonicalAudioUploadCutoverCandidate>
    {
        public string ObjectID { get; init; }
        public CanonicalAudioUploadLocalTruth LocalTruth { get; init; }
        public CanonicalAudioUploadPeerTruth PeerTruth { get; init; }
        public CanonicalAudioUploadLedgerTruth LedgerTruth { get; init; }
        public CanonicalAudioUploadRetryTruth RetryTruth { get; init; }
        public CanonicalAudioUploadTriggerSource Trigger { get; init; }
        public CanonicalAudioUploadActionKind ActionKind { get; init; }
        public string Reason { get; init; }
        public CanonicalAudioUploadEvidenceStatus EvidenceStatus { get; init; }
        public List<CanonicalAudioUploadEvidenceBlocker> EvidenceBlockers { get; init; }
        public List<CanonicalAudioUploadDiagnosticKind> Diagnostics { get; init; }
        public bool ManualUserAction { get; init; }
        public bool OrdinarySync { get; init; }
        public bool RetryDrainer { get; init; }
        public bool ProductionUploadSuppressed { get; init; }
        public bool LegacyUploadCoordinatorNotCalled { get; init; }
        public bool RecordingUploadClientNotCalled { get; init; }
        public bool SecureMacUploadClientNotCalled { get; init; }

        public CanonicalAudioUploadCutoverCandidate(
            string objectID,
            CanonicalAudioUploadLocalTruth localTruth,
            CanonicalAudioUploadPeerTruth peerTruth,
            CanonicalAudioUploadLedgerTruth? ledgerTruth = null,
            CanonicalAudioUploadRetryTruth? retryTruth = null,
            CanonicalAudioUploadTriggerSource trigger = default,
            CanonicalAudioUploadActionKind actionKind = default,
            string reason = "",
            CanonicalAudioUploadEvidenceStatus evidenceStatus = default,
            List<CanonicalAudioUploadEvidenceBlocker>? evidenceBlockers = null,
            List<CanonicalAudioUploadDiagnosticKind>? diagnostics = null)
        {
            var trimmedObjectID = objectID.Trim();
            ObjectID = trimmedObjectID.Length == 0 ? "object:unknown" : trimmedObjectID;
            LocalTruth = localTruth;
            PeerTruth = peerTruth;
            LedgerTruth = ledgerTruth ?? new CanonicalAudioUploadLedgerTruth();
            RetryTruth = retryTruth ?? new CanonicalAudioUploadRetryTruth();
            Trigger = trigger;
            ActionKind = actionKind;
            Reason = reason;
            EvidenceStatus = evidenceStatus;
            EvidenceBlockers = new HashSet<CanonicalAudioUploadEvidenceBlocker>(evidenceBlockers ?? new List<CanonicalAudioUploadEvidenceBlocker>())
                .OrderBy(b => b.ToString()).ToList();
            Diagnostics = new HashSet<CanonicalAudioUploadDiagnosticKind>(diagnostics ?? new List<CanonicalAudioUploadDiagnosticKind>())
                .OrderBy(d => d.ToString()).ToList();
            ManualUserAction = trigger.IsExplicitManualUploadButton();
            OrdinarySync = trigger == CanonicalAudioUploadTriggerSource.ordinarySync
                || trigger == CanonicalAudioUploadTriggerSource.periodicSync
                || trigger == CanonicalAudioUploadTriggerSource.appActivationRefresh;
            RetryDrainer = trigger.IsRetryDrainer();
            ProductionUploadSuppressed = true;
            LegacyUploadCoordinatorNotCalled = true;
            RecordingUploadClientNotCalled = true;
            SecureMacUploadClientNotCalled = true;
        }

        public bool CanaryEligibleInShadowOnlyModel =>
            ActionKind == CanonicalAudioUploadActionKind.audioUploadCanaryCandidate
            && EvidenceStatus == CanonicalAudioUploadEvidenceStatus.complete
            && ProductionUploadSuppressed;

        public string? HashPrefix =>
            LocalTruth.ContentHash != null
                ? (LocalTruth.ContentHash.Value.Length > 12 ? LocalTruth.ContentHash.Value[..12] : LocalTruth.ContentHash.Value)
                : null;

        public static CanonicalAudioUploadCutoverCandidate Evaluate(
            string objectID,
            CanonicalAudioUploadLocalTruth localTruth,
            CanonicalAudioUploadPeerTruth peerTruth,
            CanonicalAudioUploadLedgerTruth? ledgerTruth = null,
            CanonicalAudioUploadRetryTruth? retryTruth = null,
            CanonicalAudioUploadTriggerSource trigger = default)
        {
            var blockers = new List<CanonicalAudioUploadEvidenceBlocker>();
            var diagnostics = new List<CanonicalAudioUploadDiagnosticKind> { CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadEvidenceBuilt };

            if (trigger.IsViewRefresh())
            {
                blockers.Add(CanonicalAudioUploadEvidenceBlocker.viewRefreshSuppressed);
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadViewRefreshSuppressed);
                return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                    CanonicalAudioUploadActionKind.unsupported, "viewRefreshNeverCreatesAudioUploadCandidate",
                    CanonicalAudioUploadEvidenceStatus.blocked, blockers, diagnostics);
            }

            if (trigger.IsExplicitManualUploadButton())
            {
                blockers.Add(CanonicalAudioUploadEvidenceBlocker.manualUploadButtonLegacyOwned);
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadManualButtonLegacyOwned);
                return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                    CanonicalAudioUploadActionKind.unsupported, "explicitManualUploadButtonStaysLegacyOwnedInV812",
                    CanonicalAudioUploadEvidenceStatus.blocked, blockers, diagnostics);
            }

            if (trigger.IsRetryDrainer() && retryTruth != null && !retryTruth.HasExistingEligibleRetry)
            {
                blockers.Add(CanonicalAudioUploadEvidenceBlocker.retryDrainerFreshJobSuppressed);
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRetryDrainerFreshJobSuppressed);
                return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                    CanonicalAudioUploadActionKind.unsupported, "retryDrainerCannotCreateFreshAudioUploadJob",
                    CanonicalAudioUploadEvidenceStatus.blocked, blockers, diagnostics);
            }

            if (!localTruth.AudioAvailable) blockers.Add(CanonicalAudioUploadEvidenceBlocker.localAudioMissing);
            if (localTruth.ContentHash == null) blockers.Add(CanonicalAudioUploadEvidenceBlocker.localHashUnavailable);
            if (localTruth.ByteSize == null) blockers.Add(CanonicalAudioUploadEvidenceBlocker.localByteSizeUnavailable);

            var peerNoOpProof = peerTruth.PeerTruthSufficientForNoOp(localTruth);
            var lt = ledgerTruth ?? new CanonicalAudioUploadLedgerTruth();
            if (lt.Phase == CanonicalAudioUploadLedgerPhase.completed && !peerNoOpProof)
            {
                blockers.Add(CanonicalAudioUploadEvidenceBlocker.completedLedgerWithoutPeerMatch);
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadCompletedLedgerRejectedAsNoOp);
            }
            if (lt.MetadataUploaded || peerTruth.MetadataUploaded)
            {
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadMetadataUploadedRejectedAsAudioProof);
                if (!peerNoOpProof) blockers.Add(CanonicalAudioUploadEvidenceBlocker.metadataUploadedNotAudioProof);
            }
            if (lt.ReceiveRecordExists || peerTruth.ReceiveRecordExists)
            {
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadReceiveRecordRejectedAsAudioProof);
                if (!peerNoOpProof) blockers.Add(CanonicalAudioUploadEvidenceBlocker.receiveRecordNotAudioProof);
            }
            if (lt.UiUploaded || peerTruth.UiUploaded)
            {
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadUIUploadedRejectedAsAudioProof);
                if (!peerNoOpProof) blockers.Add(CanonicalAudioUploadEvidenceBlocker.uiUploadedNotAudioProof);
            }

            if (peerNoOpProof && localTruth.SufficientForUploadCandidate)
            {
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadPeerSameNoOp);
                return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                    CanonicalAudioUploadActionKind.audioUploadNoOp, "peerAudioHashAndSizeMatch",
                    CanonicalAudioUploadEvidenceStatus.complete, new List<CanonicalAudioUploadEvidenceBlocker>(), diagnostics);
            }

            if (!localTruth.SufficientForUploadCandidate)
            {
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadEvidenceBlocked);
                return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                    CanonicalAudioUploadActionKind.unsupported, "localAudioTruthIncomplete",
                    CanonicalAudioUploadEvidenceStatus.blocked, blockers, diagnostics);
            }

            switch (peerTruth.State)
            {
                case CanonicalAudioUploadPeerState.missing:
                case CanonicalAudioUploadPeerState.metadataOnly:
                    return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                        CanonicalAudioUploadActionKind.audioUploadCanaryCandidate,
                        peerTruth.State == CanonicalAudioUploadPeerState.missing ? "peerAudioMissing" : "peerMetadataOnlyAudioMissing",
                        blockers.Count == 0 ? CanonicalAudioUploadEvidenceStatus.complete : CanonicalAudioUploadEvidenceStatus.blocked,
                        blockers, diagnostics);
                case CanonicalAudioUploadPeerState.unknown:
                    blockers.Add(CanonicalAudioUploadEvidenceBlocker.peerUnknown);
                    diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadPeerUnknownDeferred);
                    return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                        CanonicalAudioUploadActionKind.audioUploadDeferredPeerUnknown,
                        "peerAudioUnknownIsDeferredInV812",
                        CanonicalAudioUploadEvidenceStatus.deferred, blockers, diagnostics);
                case CanonicalAudioUploadPeerState.available:
                    if (peerTruth.ContentHash == null || peerTruth.ByteSize == null)
                    {
                        blockers.Add(CanonicalAudioUploadEvidenceBlocker.peerTruthMissing);
                        diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadEvidenceBlocked);
                        return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                            CanonicalAudioUploadActionKind.unsupported,
                            "peerAvailableWithoutHashOrSizeCannotNoOp",
                            CanonicalAudioUploadEvidenceStatus.blocked, blockers, diagnostics);
                    }
                    blockers.Add(CanonicalAudioUploadEvidenceBlocker.differentHashOrSize);
                    diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadConflictDetected);
                    return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                        CanonicalAudioUploadActionKind.audioUploadConflictRecord,
                        "peerAudioHashOrSizeDifferent",
                        CanonicalAudioUploadEvidenceStatus.conflict, blockers, diagnostics);
                case CanonicalAudioUploadPeerState.different:
                case CanonicalAudioUploadPeerState.deleted:
                    blockers.Add(CanonicalAudioUploadEvidenceBlocker.differentHashOrSize);
                    diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadConflictDetected);
                    return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                        CanonicalAudioUploadActionKind.audioUploadConflictRecord,
                        peerTruth.State == CanonicalAudioUploadPeerState.deleted ? "peerAudioDeletedRequiresConflictNotOverwrite" : "peerAudioDifferent",
                        CanonicalAudioUploadEvidenceStatus.conflict, blockers, diagnostics);
                default:
                    return Make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth, trigger,
                        CanonicalAudioUploadActionKind.unsupported, "unhandled",
                        CanonicalAudioUploadEvidenceStatus.blocked, blockers, diagnostics);
            }
        }

        public static List<CanonicalAudioUploadCutoverCandidate> Candidates(
            CanonicalManifest localManifest,
            CanonicalManifest? peerManifest,
            CanonicalAudioUploadTriggerSource trigger,
            Dictionary<string, CanonicalAudioUploadLedgerTruth>? ledgerTruths = null,
            Dictionary<string, CanonicalAudioUploadRetryTruth>? retryTruths = null)
        {
            var peerObjects = (peerManifest?.Objects ?? new List<CanonicalRecordingObject>())
                .ToDictionary(o => o.ObjectID);
            ledgerTruths ??= new Dictionary<string, CanonicalAudioUploadLedgerTruth>();
            retryTruths ??= new Dictionary<string, CanonicalAudioUploadRetryTruth>();

            var results = new List<CanonicalAudioUploadCutoverCandidate>();
            foreach (var obj in localManifest.Objects)
            {
                if (obj.AudioArtifact == null) continue;
                var objectID = obj.ObjectID;
                results.Add(Evaluate(
                    objectID: objectID,
                    localTruth: CanonicalAudioUploadLocalTruth.From(obj),
                    peerTruth: peerManifest == null
                        ? new CanonicalAudioUploadPeerTruth(CanonicalAudioUploadPeerState.unknown, diagnosticsSummary: "peerManifestUnavailable")
                        : CanonicalAudioUploadPeerTruth.From(peerObjects.GetValueOrDefault(objectID)),
                    ledgerTruth: ledgerTruths.GetValueOrDefault(objectID, new CanonicalAudioUploadLedgerTruth()),
                    retryTruth: retryTruths.GetValueOrDefault(objectID, new CanonicalAudioUploadRetryTruth()),
                    trigger: trigger));
            }
            return results;
        }

        private static CanonicalAudioUploadCutoverCandidate Make(
            string objectID,
            CanonicalAudioUploadLocalTruth localTruth,
            CanonicalAudioUploadPeerTruth peerTruth,
            CanonicalAudioUploadLedgerTruth? ledgerTruth,
            CanonicalAudioUploadRetryTruth? retryTruth,
            CanonicalAudioUploadTriggerSource trigger,
            CanonicalAudioUploadActionKind actionKind,
            string reason,
            CanonicalAudioUploadEvidenceStatus status,
            List<CanonicalAudioUploadEvidenceBlocker> blockers,
            List<CanonicalAudioUploadDiagnosticKind> diagnostics)
        {
            return new CanonicalAudioUploadCutoverCandidate(
                objectID: objectID,
                localTruth: localTruth,
                peerTruth: peerTruth,
                ledgerTruth: ledgerTruth,
                retryTruth: retryTruth,
                trigger: trigger,
                actionKind: actionKind,
                reason: reason,
                evidenceStatus: status,
                evidenceBlockers: blockers,
                diagnostics: diagnostics);
        }
    }

    public record CanonicalAudioUploadEvidenceReport : IEquatable<CanonicalAudioUploadEvidenceReport>
    {
        public List<CanonicalAudioUploadCutoverCandidate> Candidates { get; init; }
        public int CompleteCount { get; init; }
        public int BlockedCount { get; init; }
        public int DeferredCount { get; init; }
        public int ConflictCount { get; init; }
        public bool DiagnosticsRedacted { get; init; }
        public string DiagnosticsSummary { get; init; }

        public CanonicalAudioUploadEvidenceReport(List<CanonicalAudioUploadCutoverCandidate> candidates)
        {
            Candidates = candidates.OrderBy(c => c.ObjectID).ToList();
            CompleteCount = candidates.Count(c => c.EvidenceStatus == CanonicalAudioUploadEvidenceStatus.complete);
            BlockedCount = candidates.Count(c => c.EvidenceStatus == CanonicalAudioUploadEvidenceStatus.blocked);
            DeferredCount = candidates.Count(c => c.EvidenceStatus == CanonicalAudioUploadEvidenceStatus.deferred);
            ConflictCount = candidates.Count(c => c.EvidenceStatus == CanonicalAudioUploadEvidenceStatus.conflict);
            DiagnosticsRedacted = true;
            DiagnosticsSummary = string.Join(",", new[]
            {
                $"candidateCount={candidates.Count}",
                $"complete={CompleteCount}",
                $"blocked={BlockedCount}",
                $"deferred={DeferredCount}",
                $"conflict={ConflictCount}",
                "diagnosticsRedacted=true"
            });
        }
    }

    public record CanonicalAudioUploadCutoverEvidence : IEquatable<CanonicalAudioUploadCutoverEvidence>
    {
        public bool LocalAudioDescriptorsReviewed { get; init; }
        public bool PeerAudioDescriptorsReviewed { get; init; }
        public bool ShadowReceiverRehearsalPassed { get; init; }
        public bool NoCommitObserved { get; init; }
        public bool ReadSideProjectionObserved { get; init; }
        public bool AbortRollbackPolicyDocumented { get; init; }
        public bool LegacyFallbackAvailable { get; init; }
        public CanonicalAudioUploadEvidenceReport? EvidenceReport { get; init; }

        public CanonicalAudioUploadCutoverEvidence(
            bool localAudioDescriptorsReviewed = false,
            bool peerAudioDescriptorsReviewed = false,
            bool shadowReceiverRehearsalPassed = false,
            bool noCommitObserved = false,
            bool readSideProjectionObserved = false,
            bool abortRollbackPolicyDocumented = false,
            bool legacyFallbackAvailable = true,
            CanonicalAudioUploadEvidenceReport? evidenceReport = null)
        {
            LocalAudioDescriptorsReviewed = localAudioDescriptorsReviewed;
            PeerAudioDescriptorsReviewed = peerAudioDescriptorsReviewed;
            ShadowReceiverRehearsalPassed = shadowReceiverRehearsalPassed;
            NoCommitObserved = noCommitObserved;
            ReadSideProjectionObserved = readSideProjectionObserved;
            AbortRollbackPolicyDocumented = abortRollbackPolicyDocumented;
            LegacyFallbackAvailable = legacyFallbackAvailable;
            EvidenceReport = evidenceReport;
        }

        public static CanonicalAudioUploadCutoverEvidence Passing(CanonicalAudioUploadEvidenceReport? report = null) =>
            new CanonicalAudioUploadCutoverEvidence(
                localAudioDescriptorsReviewed: true,
                peerAudioDescriptorsReviewed: true,
                shadowReceiverRehearsalPassed: true,
                noCommitObserved: true,
                readSideProjectionObserved: true,
                abortRollbackPolicyDocumented: true,
                legacyFallbackAvailable: true,
                evidenceReport: report);

        public bool IsPassing =>
            LocalAudioDescriptorsReviewed
            && PeerAudioDescriptorsReviewed
            && ShadowReceiverRehearsalPassed
            && NoCommitObserved
            && ReadSideProjectionObserved
            && AbortRollbackPolicyDocumented
            && LegacyFallbackAvailable;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadCanaryStage
    {
        disabled,
        shadowOnly,
        n0,
        n1,
        n3,
        n10,
        allEligible
    }

    public static class CanonicalAudioUploadCanaryStageExtensions
    {
        public static int CanaryBudget(this CanonicalAudioUploadCanaryStage stage)
        {
            return stage switch
            {
                CanonicalAudioUploadCanaryStage.disabled or CanonicalAudioUploadCanaryStage.shadowOnly or CanonicalAudioUploadCanaryStage.n0 => 0,
                CanonicalAudioUploadCanaryStage.n1 => 1,
                CanonicalAudioUploadCanaryStage.n3 => 3,
                CanonicalAudioUploadCanaryStage.n10 => 10,
                CanonicalAudioUploadCanaryStage.allEligible => int.MaxValue,
                _ => 0
            };
        }

        public static bool RequestsProductionCanary(this CanonicalAudioUploadCanaryStage stage) =>
            stage.CanaryBudget() > 0;
    }

    public record CanonicalAudioUploadCanaryPolicy : IEquatable<CanonicalAudioUploadCanaryPolicy>
    {
        public CanonicalAudioUploadCanaryStage RequestedStage { get; init; }
        public bool AllowsTestOnlyFutureStage { get; init; }
        public bool RequireEvidence { get; init; }
        public int MaxDiagnosticsEvents { get; init; }

        public CanonicalAudioUploadCanaryPolicy(
            CanonicalAudioUploadCanaryStage requestedStage = CanonicalAudioUploadCanaryStage.disabled,
            bool allowsTestOnlyFutureStage = false,
            bool requireEvidence = true,
            int maxDiagnosticsEvents = 200)
        {
            RequestedStage = requestedStage;
            AllowsTestOnlyFutureStage = allowsTestOnlyFutureStage;
            RequireEvidence = requireEvidence;
            MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
        }

        public static readonly CanonicalAudioUploadCanaryPolicy Disabled = new();

        public int CanaryMaxObjectsPerSyncRun =>
            RequestedStage.CanaryBudget() == int.MaxValue ? int.MaxValue : Math.Max(0, RequestedStage.CanaryBudget());

        public bool ProductionCommitAllowedInV812 => false;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadCutoverFailure
    {
        disabled,
        canaryBudgetZero,
        missingCutoverToken,
        insufficientEvidence,
        candidateEvidenceBlocked,
        noEligibleCandidates,
        productionCommitBlockedV812,
        peerSnapshotUnavailable,
        manualUserActionLegacyOwned,
        futureCanaryStageBlocked,
        shadowRehearsalFailed
    }

    public record CanonicalAudioUploadCutoverGate : IEquatable<CanonicalAudioUploadCutoverGate>
    {
        public bool Allowed { get; init; }
        public bool ProductionUploadAllowed { get; init; }
        public bool ShadowOnlyAllowed { get; init; }
        public CanonicalCutoverMode Mode { get; init; }
        public CanonicalAudioUploadCanaryStage CanaryStage { get; init; }
        public List<CanonicalAudioUploadCutoverFailure> Failures { get; init; }
        public string Reason { get; init; }

        public CanonicalAudioUploadCutoverGate(
            bool allowed,
            bool productionUploadAllowed = false,
            bool shadowOnlyAllowed = false,
            CanonicalCutoverMode mode = default,
            CanonicalAudioUploadCanaryStage canaryStage = default,
            List<CanonicalAudioUploadCutoverFailure>? failures = null,
            string reason = "")
        {
            Allowed = allowed;
            ProductionUploadAllowed = productionUploadAllowed;
            ShadowOnlyAllowed = shadowOnlyAllowed;
            Mode = mode;
            CanaryStage = canaryStage;
            Failures = new HashSet<CanonicalAudioUploadCutoverFailure>(failures ?? new List<CanonicalAudioUploadCutoverFailure>())
                .OrderBy(f => f.ToString()).ToList();
            Reason = reason;
        }
    }

    public class CanonicalAudioUploadCutoverRunner
    {
        public CanonicalAudioUploadCutoverRunner() { }

        public CanonicalAudioUploadCutoverGate EvaluateGate(
            CanonicalCutoverMode mode,
            CanonicalAudioUploadCanaryPolicy policy,
            CanonicalCutoverToken? token,
            CanonicalAudioUploadCutoverEvidence evidence,
            List<CanonicalAudioUploadCutoverCandidate> candidates,
            CanonicalAudioUploadTriggerSource trigger)
        {
            var failures = new List<CanonicalAudioUploadCutoverFailure>();
            var eligibleCandidates = candidates.Where(c => c.CanaryEligibleInShadowOnlyModel).ToList();

            if (mode == CanonicalCutoverMode.disabled || policy.RequestedStage == CanonicalAudioUploadCanaryStage.disabled)
                failures.Add(CanonicalAudioUploadCutoverFailure.disabled);
            if (mode.PermitsProductionCommit || policy.RequestedStage.RequestsProductionCanary())
                failures.Add(CanonicalAudioUploadCutoverFailure.productionCommitBlockedV812);
            if (policy.RequestedStage.RequestsProductionCanary() && !policy.AllowsTestOnlyFutureStage)
                failures.Add(CanonicalAudioUploadCutoverFailure.futureCanaryStageBlocked);
            if (mode == CanonicalCutoverMode.canary && policy.CanaryMaxObjectsPerSyncRun == 0)
                failures.Add(CanonicalAudioUploadCutoverFailure.canaryBudgetZero);
            if (mode.PermitsProductionCommit && token == null)
                failures.Add(CanonicalAudioUploadCutoverFailure.missingCutoverToken);
            if (policy.RequireEvidence && !evidence.IsPassing)
                failures.Add(CanonicalAudioUploadCutoverFailure.insufficientEvidence);
            if (candidates.Any(c => c.EvidenceBlockers.Count > 0))
                failures.Add(CanonicalAudioUploadCutoverFailure.candidateEvidenceBlocked);
            if (eligibleCandidates.Count == 0 && mode != CanonicalCutoverMode.disabled)
                failures.Add(CanonicalAudioUploadCutoverFailure.noEligibleCandidates);
            if (trigger.IsExplicitManualUploadButton())
                failures.Add(CanonicalAudioUploadCutoverFailure.manualUserActionLegacyOwned);

            var shadowAllowed = failures.Count == 0
                && (mode == CanonicalCutoverMode.shadowOnly || mode == CanonicalCutoverMode.guardedExecuteNoCommit);
            return new CanonicalAudioUploadCutoverGate(
                allowed: shadowAllowed,
                productionUploadAllowed: false,
                shadowOnlyAllowed: shadowAllowed,
                mode: mode,
                canaryStage: policy.RequestedStage,
                failures: failures,
                reason: failures.Count == 0
                    ? "shadowOnlyNoCommitAllowed"
                    : string.Join(",", failures.Select(f => f.ToString())));
        }
    }

    public record CanonicalAudioUploadNoCommitCandidate : IEquatable<CanonicalAudioUploadNoCommitCandidate>
    {
        public CanonicalAudioUploadCutoverCandidate CutoverCandidate { get; init; }

        public CanonicalAudioUploadNoCommitCandidate(CanonicalAudioUploadCutoverCandidate cutoverCandidate)
        {
            CutoverCandidate = cutoverCandidate;
        }
    }

    public record CanonicalAudioUploadNoCommitResult : IEquatable<CanonicalAudioUploadNoCommitResult>
    {
        public bool Staged { get; init; }
        public string ObjectID { get; init; }
        public CanonicalAudioUploadNodeRole NodeRole { get; init; }
        public CanonicalAudioUploadActionKind ActionKind { get; init; }
        public string? WouldRequestRoute { get; init; }
        public string? PayloadHashPrefix { get; init; }
        public int PayloadByteCount { get; init; }
        public bool ProductionUploadSuppressed { get; init; }
        public bool LegacyUploadCoordinatorNotCalled { get; init; }
        public bool RecordingUploadClientNotCalled { get; init; }
        public bool SecureMacUploadClientNotCalled { get; init; }
        public bool DidNotCreateUploadJob { get; init; }
        public bool DidNotWriteInbox { get; init; }
        public bool DidNotWriteReceiveJSON { get; init; }
        public bool DidNotMutateUploadLedger { get; init; }
        public bool DidNotMutateRetryDrainer { get; init; }

        public CanonicalAudioUploadNoCommitResult(
            CanonicalAudioUploadCutoverCandidate candidate,
            CanonicalAudioUploadNodeRole nodeRole)
        {
            var dict = new Dictionary<string, string>
            {
                ["schema"] = "canonical-audio-upload-no-commit-v812",
                ["objectID"] = candidate.ObjectID,
                ["action"] = candidate.ActionKind.ToString(),
                ["reason"] = candidate.Reason
            };
            var payloadHash = CanonicalHash.Sha256Of(dict);
            Staged = candidate.CanaryEligibleInShadowOnlyModel || candidate.ActionKind == CanonicalAudioUploadActionKind.audioUploadNoOp;
            ObjectID = candidate.ObjectID;
            NodeRole = nodeRole;
            ActionKind = candidate.ActionKind;
            WouldRequestRoute = candidate.CanaryEligibleInShadowOnlyModel ? "/upload-recording-audio-session/start" : null;
            PayloadHashPrefix = payloadHash.Value.Length > 12 ? payloadHash.Value[..12] : payloadHash.Value;
            PayloadByteCount = System.Text.Encoding.UTF8.GetByteCount(candidate.ObjectID) + System.Text.Encoding.UTF8.GetByteCount(candidate.Reason);
            ProductionUploadSuppressed = true;
            LegacyUploadCoordinatorNotCalled = true;
            RecordingUploadClientNotCalled = true;
            SecureMacUploadClientNotCalled = true;
            DidNotCreateUploadJob = true;
            DidNotWriteInbox = true;
            DidNotWriteReceiveJSON = true;
            DidNotMutateUploadLedger = true;
            DidNotMutateRetryDrainer = true;
        }
    }

    public interface ICanonicalAudioUploadNoCommitExecutor
    {
        CanonicalAudioUploadNoCommitResult StageAudioUploadNoCommit(CanonicalAudioUploadNoCommitCandidate candidate);
    }

    public class CanonicalAudioUploadNoCommitRunner
    {
        public CanonicalAudioUploadNoCommitRunner() { }

        public CanonicalAudioUploadCutoverResult Run(
            CanonicalCutoverMode mode,
            CanonicalAudioUploadCanaryPolicy policy,
            CanonicalCutoverToken? token,
            CanonicalAudioUploadCutoverEvidence evidence,
            List<CanonicalAudioUploadNoCommitCandidate> candidates,
            CanonicalAudioUploadTriggerSource trigger,
            CanonicalAudioUploadNodeRole nodeRole,
            string? syncRunID,
            ICanonicalAudioUploadNoCommitExecutor executor)
        {
            var cutoverCandidates = candidates.Select(c => c.CutoverCandidate).ToList();
            var gate = new CanonicalAudioUploadCutoverRunner().EvaluateGate(
                mode: mode,
                policy: policy,
                token: token,
                evidence: evidence,
                candidates: cutoverCandidates,
                trigger: trigger);

            var diagnostics = new List<CanonicalAudioUploadDiagnostic>
            {
                new CanonicalAudioUploadDiagnostic(
                    kind: CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadCutoverGateEvaluated,
                    syncRunID: syncRunID,
                    trigger: trigger,
                    nodeRole: nodeRole,
                    result: gate.Allowed ? "allowed" : "blocked",
                    reason: gate.Reason)
            };
            diagnostics.Add(new CanonicalAudioUploadDiagnostic(
                kind: gate.Allowed
                    ? CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadCutoverGateAllowed
                    : CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadCutoverGateBlocked,
                syncRunID: syncRunID,
                trigger: trigger,
                nodeRole: nodeRole,
                result: gate.Allowed ? "allowed" : "blocked",
                reason: string.Join(",", gate.Failures.Select(f => f.ToString()))));

            List<CanonicalAudioUploadNoCommitResult> results;
            if (mode == CanonicalCutoverMode.guardedExecuteNoCommit || mode == CanonicalCutoverMode.shadowOnly)
            {
                diagnostics.Add(new CanonicalAudioUploadDiagnostic(
                    kind: CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadNoCommitStarted,
                    syncRunID: syncRunID,
                    trigger: trigger,
                    nodeRole: nodeRole,
                    result: $"candidateCount={candidates.Count}",
                    reason: "productionUploadSuppressed"));
                results = candidates.Select(c => executor.StageAudioUploadNoCommit(c)).ToList();
                diagnostics.Add(new CanonicalAudioUploadDiagnostic(
                    kind: CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadNoCommitCompleted,
                    syncRunID: syncRunID,
                    trigger: trigger,
                    nodeRole: nodeRole,
                    result: $"stagedCount={results.Count(r => r.Staged)}",
                    reason: "noCommitOnly"));
            }
            else
            {
                results = new List<CanonicalAudioUploadNoCommitResult>();
            }

            diagnostics.AddRange(new[]
            {
                new CanonicalAudioUploadDiagnostic(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadProductionCoordinatorSuppressed, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole, result: "true"),
                new CanonicalAudioUploadDiagnostic(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRecordingUploadClientSuppressed, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole, result: "true"),
                new CanonicalAudioUploadDiagnostic(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadSecureMacUploadClientSuppressed, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole, result: "true"),
                new CanonicalAudioUploadDiagnostic(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadInboxWriteSuppressed, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole, result: "true"),
                new CanonicalAudioUploadDiagnostic(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadReceiveJSONWriteSuppressed, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole, result: "true"),
                new CanonicalAudioUploadDiagnostic(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadLedgerMutationSuppressed, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole, result: "true"),
                new CanonicalAudioUploadDiagnostic(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRetryDrainerMutationSuppressed, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole, result: "true"),
                new CanonicalAudioUploadDiagnostic(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadLegacyFallbackPreserved, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole, result: "true"),
            });

            return new CanonicalAudioUploadCutoverResult(
                gate: gate,
                candidates: cutoverCandidates,
                noCommitResults: results,
                diagnostics: diagnostics,
                legacyFallbackPreserved: true,
                runtimeSwitchEnabled: false);
        }
    }

    public record CanonicalAudioUploadCutoverResult : IEquatable<CanonicalAudioUploadCutoverResult>
    {
        public CanonicalAudioUploadCutoverGate Gate { get; init; }
        public List<CanonicalAudioUploadCutoverCandidate> Candidates { get; init; }
        public List<CanonicalAudioUploadNoCommitResult> NoCommitResults { get; init; }
        public List<CanonicalAudioUploadDiagnostic> Diagnostics { get; init; }
        public bool LegacyFallbackPreserved { get; init; }
        public bool RuntimeSwitchEnabled { get; init; }
        public bool CalledProductionUploadCoordinator { get; init; }
        public bool CalledRecordingUploadClient { get; init; }
        public bool CalledSecureMacUploadClient { get; init; }
        public bool WroteProductionInbox { get; init; }
        public bool WroteReceiveJSON { get; init; }
        public bool CreatedUploadJob { get; init; }
        public bool MutatedUploadLedger { get; init; }
        public bool MutatedRetryDrainer { get; init; }
        public bool SuppressedLegacyDuplicate { get; init; }

        public CanonicalAudioUploadCutoverResult(
            CanonicalAudioUploadCutoverGate gate,
            List<CanonicalAudioUploadCutoverCandidate>? candidates = null,
            List<CanonicalAudioUploadNoCommitResult>? noCommitResults = null,
            List<CanonicalAudioUploadDiagnostic>? diagnostics = null,
            bool legacyFallbackPreserved = true,
            bool runtimeSwitchEnabled = false)
        {
            Gate = gate;
            Candidates = candidates ?? new List<CanonicalAudioUploadCutoverCandidate>();
            NoCommitResults = noCommitResults ?? new List<CanonicalAudioUploadNoCommitResult>();
            Diagnostics = diagnostics ?? new List<CanonicalAudioUploadDiagnostic>();
            LegacyFallbackPreserved = legacyFallbackPreserved;
            RuntimeSwitchEnabled = runtimeSwitchEnabled;
            CalledProductionUploadCoordinator = false;
            CalledRecordingUploadClient = false;
            CalledSecureMacUploadClient = false;
            WroteProductionInbox = false;
            WroteReceiveJSON = false;
            CreatedUploadJob = false;
            MutatedUploadLedger = false;
            MutatedRetryDrainer = false;
            SuppressedLegacyDuplicate = false;
        }
    }

    public class CanonicalAudioUploadShadowReceiver
    {
        public CanonicalRootToken RootToken { get; }
        private readonly CanonicalShadowUploadReceiver _receiver;

        public CanonicalAudioUploadShadowReceiver(CanonicalRootToken? rootToken = null)
        {
            RootToken = rootToken ?? new CanonicalRootToken("canonical-audio-upload-shadow");
            _receiver = new CanonicalShadowUploadReceiver(RootToken);
        }

        public CanonicalShadowUploadReceiver CanonicalReceiver => _receiver;

        public async Task SeedAsync(CanonicalFileReference reference, byte[] bytes)
        {
            await _receiver.SeedAsync(reference, bytes);
        }

        public async Task<CanonicalFileReadResult> ReadAsync(CanonicalFileReference reference)
        {
            return await _receiver.ReadAsync(reference);
        }
    }

    public record CanonicalAudioUploadShadowRehearsalInput
    {
        public string ObjectID { get; init; }
        public string LogicalPathToken { get; init; }
        public byte[] Bytes { get; init; }
        public int ChunkSize { get; init; }
        public CanonicalHash? DeclaredTotalHash { get; init; }
        public byte[]? ExistingReceiverBytes { get; init; }
        public bool SimulateInterruptionAfterFirstChunk { get; init; }

        public CanonicalAudioUploadShadowRehearsalInput(
            string objectID,
            string logicalPathToken,
            byte[] bytes,
            int chunkSize = 2 * 1024 * 1024,
            CanonicalHash? declaredTotalHash = null,
            byte[]? existingReceiverBytes = null,
            bool simulateInterruptionAfterFirstChunk = false)
        {
            var trimmed = objectID.Trim();
            ObjectID = trimmed.Length == 0 ? "object:unknown" : trimmed;
            LogicalPathToken = CanonicalProjectionContract.SafeLogicalPathToken(logicalPathToken) ?? $"audio/{ObjectID}.m4a";
            Bytes = bytes;
            ChunkSize = Math.Max(1, chunkSize);
            DeclaredTotalHash = declaredTotalHash;
            ExistingReceiverBytes = existingReceiverBytes;
            SimulateInterruptionAfterFirstChunk = simulateInterruptionAfterFirstChunk;
        }
    }

    public record CanonicalAudioUploadShadowRehearsalResult
    {
        public CanonicalShadowUploadResult ShadowResult { get; init; }
        public bool ProductionUploadSuppressed { get; init; }
        public bool CalledProductionUploadCoordinator { get; init; }
        public bool CalledRecordingUploadClient { get; init; }
        public bool CalledSecureMacUploadClient { get; init; }
        public bool WroteProductionInbox { get; init; }
        public bool WroteReceiveJSON { get; init; }
        public List<CanonicalAudioUploadDiagnostic> Diagnostics { get; init; }

        public CanonicalAudioUploadShadowRehearsalResult(
            CanonicalShadowUploadResult shadowResult,
            string objectID,
            string? syncRunID,
            CanonicalAudioUploadTriggerSource trigger,
            CanonicalAudioUploadNodeRole nodeRole)
        {
            string SafeObjectID()
            {
                var trimmed = objectID.Trim();
                return trimmed.Length == 0 ? "object:unknown" : trimmed;
            }
            var safeObjectID = SafeObjectID();
            ShadowResult = shadowResult;
            ProductionUploadSuppressed = true;
            CalledProductionUploadCoordinator = false;
            CalledRecordingUploadClient = false;
            CalledSecureMacUploadClient = false;
            WroteProductionInbox = false;
            WroteReceiveJSON = false;
            Diagnostics = new List<CanonicalAudioUploadDiagnostic>
            {
                new CanonicalAudioUploadDiagnostic(
                    kind: CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadShadowRehearsalCompleted,
                    syncRunID: syncRunID,
                    trigger: trigger,
                    nodeRole: nodeRole,
                    objectID: safeObjectID,
                    action: CanonicalAudioUploadActionKind.audioUploadShadowRehearsal,
                    result: shadowResult.Completed ? "completed" : "failed",
                    reason: shadowResult.Divergence.ToString()),
                new CanonicalAudioUploadDiagnostic(
                    kind: shadowResult.WroteShadowReceiver
                        ? CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadShadowReceiverWrote
                        : CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadShadowReceiverNoOp,
                    syncRunID: syncRunID,
                    trigger: trigger,
                    nodeRole: nodeRole,
                    objectID: safeObjectID,
                    action: CanonicalAudioUploadActionKind.audioUploadShadowRehearsal,
                    result: shadowResult.WroteShadowReceiver ? "shadowReceiverWrote" : "shadowReceiverNoOp",
                    reason: shadowResult.Divergence.ToString())
            };
        }
    }

    public class CanonicalAudioUploadShadowRehearsal
    {
        public CanonicalAudioUploadShadowRehearsal() { }

        public async Task<CanonicalAudioUploadShadowRehearsalResult> RunAsync(
            CanonicalAudioUploadShadowRehearsalInput input,
            CanonicalAudioUploadShadowReceiver? receiver = null,
            string? syncRunID = null,
            CanonicalAudioUploadTriggerSource trigger = CanonicalAudioUploadTriggerSource.ordinarySync,
            CanonicalAudioUploadNodeRole nodeRole = CanonicalAudioUploadNodeRole.testHarness)
        {
            receiver ??= new CanonicalAudioUploadShadowReceiver();
            var reference = new CanonicalFileReference(
                rootToken: receiver.RootToken,
                logicalPathToken: input.LogicalPathToken,
                artifactID: CanonicalProjectionContract.ArtifactID(input.ObjectID, CanonicalArtifactKind.audio),
                artifactKind: CanonicalArtifactKind.audio);

            var result = await new CanonicalShadowUploadRehearsal().RunAsync(
                new CanonicalShadowUploadRehearsalInput(
                    objectID: input.ObjectID,
                    targetReference: reference,
                    bytes: input.Bytes,
                    chunkSize: input.ChunkSize,
                    declaredTotalHash: input.DeclaredTotalHash,
                    existingReceiverBytes: input.ExistingReceiverBytes,
                    simulateInterruptionAfterFirstChunk: input.SimulateInterruptionAfterFirstChunk),
                receiver.CanonicalReceiver);

            return new CanonicalAudioUploadShadowRehearsalResult(
                shadowResult: result,
                objectID: input.ObjectID,
                syncRunID: syncRunID,
                trigger: trigger,
                nodeRole: nodeRole);
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadAbortPhase
    {
        beforeStart,
        beforeFinalize,
        afterFinalize
    }

    public record CanonicalAudioUploadAbortPlan : IEquatable<CanonicalAudioUploadAbortPlan>
    {
        public CanonicalAudioUploadAbortPhase Phase { get; init; }
        public bool CanCancelSession { get; init; }
        public bool CanDeleteProductionAudio { get; init; }
        public bool ShouldPreserveLegacyFallback { get; init; }
        public string Reason { get; init; }

        public static CanonicalAudioUploadAbortPlan Plan(CanonicalAudioUploadAbortPhase phase)
        {
            return phase switch
            {
                CanonicalAudioUploadAbortPhase.beforeStart => new CanonicalAudioUploadAbortPlan
                {
                    Phase = phase, CanCancelSession = true, CanDeleteProductionAudio = false,
                    ShouldPreserveLegacyFallback = true, Reason = "abortBeforeStartNoProductionState"
                },
                CanonicalAudioUploadAbortPhase.beforeFinalize => new CanonicalAudioUploadAbortPlan
                {
                    Phase = phase, CanCancelSession = true, CanDeleteProductionAudio = false,
                    ShouldPreserveLegacyFallback = true, Reason = "abortBeforeFinalizeCanCancelShadowSessionOnly"
                },
                CanonicalAudioUploadAbortPhase.afterFinalize => new CanonicalAudioUploadAbortPlan
                {
                    Phase = phase, CanCancelSession = false, CanDeleteProductionAudio = false,
                    ShouldPreserveLegacyFallback = true, Reason = "postFinalizeRollbackNeverDeletesAudio"
                },
                _ => new CanonicalAudioUploadAbortPlan
                {
                    Phase = phase, CanCancelSession = false, CanDeleteProductionAudio = false,
                    ShouldPreserveLegacyFallback = true, Reason = "unknown"
                }
            };
        }
    }

    public record CanonicalAudioUploadCleanupResult : IEquatable<CanonicalAudioUploadCleanupResult>
    {
        public bool ShadowPartialSessionCleaned { get; init; }
        public bool ProductionAudioDeleted { get; init; }
        public bool ReceiveJSONDeleted { get; init; }
        public bool LegacyFallbackPreserved { get; init; }
        public string Reason { get; init; }

        public static CanonicalAudioUploadCleanupResult ShadowOnlyCleanup(string reason) =>
            new CanonicalAudioUploadCleanupResult
            {
                ShadowPartialSessionCleaned = true,
                ProductionAudioDeleted = false,
                ReceiveJSONDeleted = false,
                LegacyFallbackPreserved = true,
                Reason = reason
            };
    }

    public record CanonicalAudioUploadRollbackPolicy : IEquatable<CanonicalAudioUploadRollbackPolicy>
    {
        public CanonicalAudioUploadAbortPlan PreFinalizeAbort { get; init; }
        public CanonicalAudioUploadAbortPlan PostFinalizeRollback { get; init; }
        public bool CleanupShadowPartialSessions { get; init; }
        public bool NeverDeleteProductionAudio { get; init; }
        public bool NeverDeleteReceiveJSON { get; init; }

        public CanonicalAudioUploadRollbackPolicy()
        {
            PreFinalizeAbort = CanonicalAudioUploadAbortPlan.Plan(CanonicalAudioUploadAbortPhase.beforeFinalize);
            PostFinalizeRollback = CanonicalAudioUploadAbortPlan.Plan(CanonicalAudioUploadAbortPhase.afterFinalize);
            CleanupShadowPartialSessions = true;
            NeverDeleteProductionAudio = true;
            NeverDeleteReceiveJSON = true;
        }

        public CanonicalAudioUploadCleanupResult CleanupPartialShadowSession(string reason = "shadowPartialSessionCleanupOnly") =>
            CanonicalAudioUploadCleanupResult.ShadowOnlyCleanup(reason);
    }

    public record CanonicalAudioUploadReadSideParallelProjection : IEquatable<CanonicalAudioUploadReadSideParallelProjection>
    {
        public int CandidateCount { get; init; }
        public bool Equivalent { get; init; }
        public bool MutatedUI { get; init; }
        public bool WroteUIState { get; init; }
        public bool CreatedUploadJob { get; init; }
        public List<CanonicalAudioUploadDiagnostic> Diagnostics { get; init; }

        public static CanonicalAudioUploadReadSideParallelProjection Project(
            List<CanonicalAudioUploadCutoverCandidate> candidates,
            string? syncRunID,
            CanonicalAudioUploadTriggerSource trigger,
            CanonicalAudioUploadNodeRole nodeRole)
        {
            var diverged = candidates.Any(c => c.EvidenceStatus == CanonicalAudioUploadEvidenceStatus.conflict);
            return new CanonicalAudioUploadReadSideParallelProjection
            {
                CandidateCount = candidates.Count,
                Equivalent = !diverged,
                MutatedUI = false,
                WroteUIState = false,
                CreatedUploadJob = false,
                Diagnostics = new List<CanonicalAudioUploadDiagnostic>
                {
                    new CanonicalAudioUploadDiagnostic(
                        CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadReadSideProjectionStarted,
                        syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole,
                        result: $"candidateCount={candidates.Count}"),
                    new CanonicalAudioUploadDiagnostic(
                        diverged ? CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadReadSideProjectionDiverged
                            : CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadReadSideProjectionEquivalent,
                        syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole,
                        result: diverged ? "diverged" : "equivalent")
                }
            };
        }
    }

    public record CanonicalAudioUploadCutoverAppSeamPolicy : IEquatable<CanonicalAudioUploadCutoverAppSeamPolicy>
    {
        public bool RecordDiagnostics { get; init; }
        public int MaxDiagnosticsEvents { get; init; }
        public CanonicalAudioUploadCanaryPolicy CanaryPolicy { get; init; }

        public CanonicalAudioUploadCutoverAppSeamPolicy(
            bool recordDiagnostics = true,
            int maxDiagnosticsEvents = 200,
            CanonicalAudioUploadCanaryPolicy? canaryPolicy = null)
        {
            RecordDiagnostics = recordDiagnostics;
            MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
            CanaryPolicy = canaryPolicy ?? CanonicalAudioUploadCanaryPolicy.Disabled;
        }
    }

    public record CanonicalAudioUploadCutoverAppSeamConfiguration : IEquatable<CanonicalAudioUploadCutoverAppSeamConfiguration>
    {
        public bool IsEnabled { get; init; }
        public CanonicalCutoverAppSeamMode Mode { get; init; }
        public CanonicalAudioUploadCutoverAppSeamPolicy Policy { get; init; }
        public CanonicalAudioUploadCutoverEvidence Evidence { get; init; }
        public CanonicalCutoverToken? CutoverToken { get; init; }

        public CanonicalAudioUploadCutoverAppSeamConfiguration(
            bool isEnabled = false,
            CanonicalCutoverAppSeamMode mode = CanonicalCutoverAppSeamMode.disabled,
            CanonicalAudioUploadCutoverAppSeamPolicy? policy = null,
            CanonicalAudioUploadCutoverEvidence? evidence = null,
            CanonicalCutoverToken? cutoverToken = null)
        {
            IsEnabled = isEnabled;
            Mode = isEnabled ? mode : CanonicalCutoverAppSeamMode.disabled;
            Policy = policy ?? new CanonicalAudioUploadCutoverAppSeamPolicy();
            Evidence = evidence ?? new CanonicalAudioUploadCutoverEvidence();
            CutoverToken = cutoverToken;
        }

        public static readonly CanonicalAudioUploadCutoverAppSeamConfiguration Disabled = new();

        public static CanonicalAudioUploadCutoverAppSeamConfiguration Enabled(
            CanonicalCutoverAppSeamMode mode = CanonicalCutoverAppSeamMode.guardedExecuteNoCommit,
            CanonicalAudioUploadCutoverAppSeamPolicy? policy = null,
            CanonicalAudioUploadCutoverEvidence? evidence = null,
            CanonicalCutoverToken? cutoverToken = null)
        {
            return new CanonicalAudioUploadCutoverAppSeamConfiguration(
                isEnabled: true,
                mode: mode,
                policy: policy ?? new CanonicalAudioUploadCutoverAppSeamPolicy(
                    canaryPolicy: new CanonicalAudioUploadCanaryPolicy(requestedStage: CanonicalAudioUploadCanaryStage.shadowOnly)),
                evidence: evidence ?? new CanonicalAudioUploadCutoverEvidence(),
                cutoverToken: cutoverToken);
        }

        public CanonicalCutoverAppSeamMode EffectiveMode =>
            IsEnabled ? Mode : CanonicalCutoverAppSeamMode.disabled;

        public CanonicalCutoverMode CutoverMode
        {
            get
            {
                return EffectiveMode switch
                {
                    CanonicalCutoverAppSeamMode.disabled => CanonicalCutoverMode.disabled,
                    CanonicalCutoverAppSeamMode.guardedExecuteNoCommit => CanonicalCutoverMode.guardedExecuteNoCommit,
                    CanonicalCutoverAppSeamMode.guardedExecuteCommit or CanonicalCutoverAppSeamMode.productionExecute
                        => CanonicalCutoverMode.guardedExecuteCommit,
                    CanonicalCutoverAppSeamMode.canaryCommit => CanonicalCutoverMode.canary,
                    _ => CanonicalCutoverMode.disabled
                };
            }
        }
    }
}
