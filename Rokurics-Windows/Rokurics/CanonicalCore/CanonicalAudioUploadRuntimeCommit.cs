using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Rokurics.CanonicalCore
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadRuntimeMode
    {
        disabled,
        diagnosticsOnly,
        noCommit,
        testTransportUpload,
        canonicalUploadWithLegacyFallback,
        blocked
    }

    public static class CanonicalAudioUploadRuntimeModeExtensions
    {
        public static bool CreatesJob(this CanonicalAudioUploadRuntimeMode mode)
        {
            return mode switch
            {
                CanonicalAudioUploadRuntimeMode.testTransportUpload or CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback => true,
                _ => false
            };
        }

        public static bool SendsNetworkOrTransport(this CanonicalAudioUploadRuntimeMode mode)
        {
            return mode switch
            {
                CanonicalAudioUploadRuntimeMode.testTransportUpload or CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback => true,
                _ => false
            };
        }
    }

    public record CanonicalAudioUploadRuntimePolicy : IEquatable<CanonicalAudioUploadRuntimePolicy>
    {
        public bool DebugInternalBuild { get; init; }
        public bool OwnerApprovedCanonicalCommit { get; init; }
        public bool AllowTestTransportUpload { get; init; }
        public bool AllowCanonicalUploadWithLegacyFallback { get; init; }
        public bool LegacyFallbackEnabled { get; init; }
        public bool RequireExistingSecureUploadRoutes { get; init; }
        public bool RetryDrainerRequiresExistingRetry { get; init; }
        public int ChunkSize { get; init; }
        public CanonicalAudioUploadRetryPolicy RetryPolicy { get; init; }

        public CanonicalAudioUploadRuntimePolicy(
            bool debugInternalBuild = false,
            bool ownerApprovedCanonicalCommit = false,
            bool allowTestTransportUpload = false,
            bool allowCanonicalUploadWithLegacyFallback = false,
            bool legacyFallbackEnabled = true,
            bool requireExistingSecureUploadRoutes = true,
            bool retryDrainerRequiresExistingRetry = true,
            int chunkSize = 4 * 1024 * 1024,
            CanonicalAudioUploadRetryPolicy? retryPolicy = null)
        {
            DebugInternalBuild = debugInternalBuild;
            OwnerApprovedCanonicalCommit = ownerApprovedCanonicalCommit;
            AllowTestTransportUpload = allowTestTransportUpload;
            AllowCanonicalUploadWithLegacyFallback = allowCanonicalUploadWithLegacyFallback;
            LegacyFallbackEnabled = legacyFallbackEnabled;
            RequireExistingSecureUploadRoutes = requireExistingSecureUploadRoutes;
            RetryDrainerRequiresExistingRetry = retryDrainerRequiresExistingRetry;
            ChunkSize = Math.Max(1, chunkSize);
            RetryPolicy = retryPolicy ?? new CanonicalAudioUploadRetryPolicy();
        }

        public static readonly CanonicalAudioUploadRuntimePolicy ReleaseDefault = new();

        public static CanonicalAudioUploadRuntimePolicy TestTransport(
            int chunkSize = 4 * 1024 * 1024,
            CanonicalAudioUploadRetryPolicy? retryPolicy = null)
        {
            return new CanonicalAudioUploadRuntimePolicy(
                debugInternalBuild: true,
                ownerApprovedCanonicalCommit: true,
                allowTestTransportUpload: true,
                allowCanonicalUploadWithLegacyFallback: false,
                legacyFallbackEnabled: true,
                requireExistingSecureUploadRoutes: true,
                chunkSize: chunkSize,
                retryPolicy: retryPolicy ?? new CanonicalAudioUploadRetryPolicy());
        }
    }

    public record CanonicalAudioUploadRuntimeConfiguration : IEquatable<CanonicalAudioUploadRuntimeConfiguration>
    {
        public CanonicalAudioUploadRuntimeMode Mode { get; init; }
        public CanonicalAudioUploadRuntimePolicy Policy { get; init; }

        public CanonicalAudioUploadRuntimeConfiguration(
            CanonicalAudioUploadRuntimeMode mode = CanonicalAudioUploadRuntimeMode.disabled,
            CanonicalAudioUploadRuntimePolicy? policy = null)
        {
            Mode = mode;
            Policy = policy ?? CanonicalAudioUploadRuntimePolicy.ReleaseDefault;
        }

        public static readonly CanonicalAudioUploadRuntimeConfiguration Disabled = new();

        public static CanonicalAudioUploadRuntimeConfiguration DiagnosticsOnly(
            CanonicalAudioUploadRuntimePolicy? policy = null)
        {
            return new CanonicalAudioUploadRuntimeConfiguration(
                CanonicalAudioUploadRuntimeMode.diagnosticsOnly,
                policy ?? CanonicalAudioUploadRuntimePolicy.ReleaseDefault);
        }

        public static CanonicalAudioUploadRuntimeConfiguration NoCommit(
            CanonicalAudioUploadRuntimePolicy? policy = null)
        {
            return new CanonicalAudioUploadRuntimeConfiguration(
                CanonicalAudioUploadRuntimeMode.noCommit,
                policy ?? CanonicalAudioUploadRuntimePolicy.ReleaseDefault);
        }

        public static CanonicalAudioUploadRuntimeConfiguration TestTransportUpload(
            int chunkSize = 4 * 1024 * 1024,
            CanonicalAudioUploadRetryPolicy? retryPolicy = null)
        {
            return new CanonicalAudioUploadRuntimeConfiguration(
                CanonicalAudioUploadRuntimeMode.testTransportUpload,
                CanonicalAudioUploadRuntimePolicy.TestTransport(chunkSize, retryPolicy));
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadRuntimeOutcome
    {
        legacyFallback,
        diagnosticsOnly,
        noCommit,
        noOp,
        deferred,
        uploaded,
        retryScheduled,
        conflict,
        blocked,
        failed
    }

    public record CanonicalAudioUploadRuntimeResult : IEquatable<CanonicalAudioUploadRuntimeResult>
    {
        public CanonicalAudioUploadRuntimeMode Mode { get; init; }
        public CanonicalAudioUploadRuntimeOutcome Outcome { get; init; }
        public string ObjectID { get; init; }
        public CanonicalUploadSessionID? SessionID { get; init; }
        public bool CreatedJob { get; init; }
        public bool StartedTransport { get; init; }
        public int SentChunkCount { get; init; }
        public long ConfirmedBytes { get; init; }
        public bool Completed { get; init; }
        public bool UsedLegacyFallback { get; init; }
        public string? LegacyFallbackReason { get; init; }
        public CanonicalAudioUploadFinalizeProof? FinalizeProof { get; init; }
        public CanonicalAudioUploadRetryRecord? RetryRecord { get; init; }
        public List<CanonicalAudioUploadDiagnostic> Diagnostics { get; init; }

        public CanonicalAudioUploadRuntimeResult(
            CanonicalAudioUploadRuntimeMode mode,
            CanonicalAudioUploadRuntimeOutcome outcome,
            string objectID,
            CanonicalUploadSessionID? sessionID = null,
            bool createdJob = false,
            bool startedTransport = false,
            int sentChunkCount = 0,
            long confirmedBytes = 0,
            bool completed = false,
            bool usedLegacyFallback = false,
            string? legacyFallbackReason = null,
            CanonicalAudioUploadFinalizeProof? finalizeProof = null,
            CanonicalAudioUploadRetryRecord? retryRecord = null,
            List<CanonicalAudioUploadDiagnostic>? diagnostics = null)
        {
            Mode = mode;
            Outcome = outcome;
            ObjectID = CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
            SessionID = sessionID;
            CreatedJob = createdJob;
            StartedTransport = startedTransport;
            SentChunkCount = sentChunkCount;
            ConfirmedBytes = Math.Max(0, confirmedBytes);
            Completed = completed;
            UsedLegacyFallback = usedLegacyFallback;
            LegacyFallbackReason = CanonicalAudioUploadRuntimeRedaction.SafeText(legacyFallbackReason);
            FinalizeProof = finalizeProof;
            RetryRecord = retryRecord;
            Diagnostics = diagnostics ?? new List<CanonicalAudioUploadDiagnostic>();
        }
    }

    public record CanonicalAudioUploadOffset : IEquatable<CanonicalAudioUploadOffset>, IComparable<CanonicalAudioUploadOffset>
    {
        public long Value { get; init; }

        public CanonicalAudioUploadOffset(long value = 0)
        {
            Value = Math.Max(0, value);
        }

        public int CompareTo(CanonicalAudioUploadOffset? other) =>
            other == null ? 1 : Value.CompareTo(other.Value);

        public static bool operator <(CanonicalAudioUploadOffset left, CanonicalAudioUploadOffset right) =>
            left.Value < right.Value;

        public static bool operator >(CanonicalAudioUploadOffset left, CanonicalAudioUploadOffset right) =>
            left.Value > right.Value;

        public static bool operator <=(CanonicalAudioUploadOffset left, CanonicalAudioUploadOffset right) =>
            left.Value <= right.Value;

        public static bool operator >=(CanonicalAudioUploadOffset left, CanonicalAudioUploadOffset right) =>
            left.Value >= right.Value;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalAudioUploadSessionState
    {
        idle,
        starting,
        started,
        chunking,
        interrupted,
        resuming,
        finalizing,
        finalized,
        failed,
        aborted,
        conflict,
        blocked
    }

    public static class CanonicalAudioUploadSessionStateExtensions
    {
        public static bool IsTerminal(this CanonicalAudioUploadSessionState state)
        {
            return state switch
            {
                CanonicalAudioUploadSessionState.finalized or CanonicalAudioUploadSessionState.failed
                    or CanonicalAudioUploadSessionState.aborted or CanonicalAudioUploadSessionState.conflict
                    or CanonicalAudioUploadSessionState.blocked => true,
                _ => false
            };
        }
    }

    public record CanonicalAudioUploadChunk : IEquatable<CanonicalAudioUploadChunk>
    {
        public string ObjectID { get; init; }
        public CanonicalUploadSessionID SessionID { get; init; }
        public CanonicalAudioUploadOffset Offset { get; init; }
        public int Length { get; init; }
        public string? ChunkHashPrefix { get; init; }
        public string IdempotencyKey { get; init; }

        public CanonicalAudioUploadChunk(
            string objectID,
            CanonicalUploadSessionID sessionID,
            CanonicalAudioUploadOffset offset,
            int length,
            CanonicalHash? chunkHash = null,
            string? idempotencyKey = null)
        {
            ObjectID = CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
            SessionID = sessionID;
            Offset = offset;
            Length = Math.Max(0, length);
            ChunkHashPrefix = chunkHash != null
                ? CanonicalAudioUploadRuntimeRedaction.HashPrefix(chunkHash.Value)
                : null;
            IdempotencyKey = idempotencyKey?.Trim().NilIfEmpty()
                ?? $"{ObjectID}:{SessionID}:{Offset.Value}:{Length}";
        }

        public long EndOffset => Offset.Value + Length;
    }

    public record CanonicalAudioUploadFinalizeProof : IEquatable<CanonicalAudioUploadFinalizeProof>
    {
        public string ObjectID { get; init; }
        public CanonicalUploadSessionID SessionID { get; init; }
        public long ByteSize { get; init; }
        public string? ContentHashPrefix { get; init; }
        public bool MacFileSizeVerified { get; init; }
        public bool MacHashVerified { get; init; }
        public bool MacProofReceived { get; init; }
        public bool ReceiveRecordMatchesAudioAvailability { get; init; }

        public CanonicalAudioUploadFinalizeProof(
            string objectID,
            CanonicalUploadSessionID sessionID,
            long byteSize,
            CanonicalHash? contentHash,
            bool macFileSizeVerified,
            bool macHashVerified,
            bool macProofReceived,
            bool receiveRecordMatchesAudioAvailability)
        {
            ObjectID = CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
            SessionID = sessionID;
            ByteSize = Math.Max(0, byteSize);
            ContentHashPrefix = contentHash != null
                ? CanonicalAudioUploadRuntimeRedaction.HashPrefix(contentHash.Value)
                : null;
            MacFileSizeVerified = macFileSizeVerified;
            MacHashVerified = macHashVerified;
            MacProofReceived = macProofReceived;
            ReceiveRecordMatchesAudioAvailability = receiveRecordMatchesAudioAvailability;
        }

        public bool Accepted =>
            MacFileSizeVerified && MacHashVerified && MacProofReceived && ReceiveRecordMatchesAudioAvailability;
    }

    public record CanonicalAudioUploadAbort : IEquatable<CanonicalAudioUploadAbort>
    {
        public string ObjectID { get; init; }
        public CanonicalUploadSessionID? SessionID { get; init; }
        public string Reason { get; init; }
        public bool PreFinalizeOnly { get; init; }
        public bool ProductionAudioDeleted { get; init; }
        public bool ReceiveRecordDeleted { get; init; }

        public CanonicalAudioUploadAbort(
            string objectID,
            CanonicalUploadSessionID? sessionID,
            string reason,
            bool preFinalizeOnly = true,
            bool productionAudioDeleted = false,
            bool receiveRecordDeleted = false)
        {
            ObjectID = CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
            SessionID = sessionID;
            Reason = CanonicalAudioUploadRuntimeRedaction.SafeText(reason) ?? "abort";
            PreFinalizeOnly = preFinalizeOnly;
            ProductionAudioDeleted = productionAudioDeleted;
            ReceiveRecordDeleted = receiveRecordDeleted;
        }
    }

    public record CanonicalAudioUploadResumeToken : IEquatable<CanonicalAudioUploadResumeToken>
    {
        public string ObjectID { get; init; }
        public CanonicalUploadSessionID SessionID { get; init; }
        public CanonicalAudioUploadOffset Offset { get; init; }
        public long ByteSize { get; init; }
        public string? ContentHashPrefix { get; init; }

        public CanonicalAudioUploadResumeToken(
            string objectID,
            CanonicalUploadSessionID sessionID,
            CanonicalAudioUploadOffset offset,
            long byteSize,
            string? contentHashPrefix)
        {
            ObjectID = CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
            SessionID = sessionID;
            Offset = offset;
            ByteSize = Math.Max(0, byteSize);
            ContentHashPrefix = CanonicalAudioUploadRuntimeRedaction.HashPrefix(contentHashPrefix);
        }
    }

    public record CanonicalAudioUploadRetryPolicy : IEquatable<CanonicalAudioUploadRetryPolicy>
    {
        public int MaxAttempts { get; init; }
        public TimeSpan RetryDelay { get; init; }

        public CanonicalAudioUploadRetryPolicy(int maxAttempts = 3, double retryDelaySeconds = 5)
        {
            MaxAttempts = Math.Max(1, maxAttempts);
            RetryDelay = TimeSpan.FromSeconds(Math.Max(0, retryDelaySeconds));
        }

        public double RetryDelaySeconds => RetryDelay.TotalSeconds;
    }

    public record CanonicalAudioUploadRetryRecord : IEquatable<CanonicalAudioUploadRetryRecord>
    {
        public string ObjectID { get; init; }
        public CanonicalUploadSessionID? SessionID { get; init; }
        public CanonicalAudioUploadOffset Offset { get; init; }
        public int ChunkSize { get; init; }
        public string? ContentHashPrefix { get; init; }
        public long ByteSize { get; init; }
        public CanonicalAudioUploadSessionState State { get; init; }
        public int AttemptCount { get; init; }
        public CanonicalTimestamp? NextRetryAt { get; init; }
        public string? LastErrorCode { get; init; }
        public bool TerminalConflict { get; init; }
        public CanonicalTimestamp UpdatedAt { get; init; }

        public CanonicalAudioUploadRetryRecord(
            string objectID,
            CanonicalUploadSessionID? sessionID = null,
            CanonicalAudioUploadOffset? offset = null,
            int chunkSize = default,
            CanonicalHash? contentHash = null,
            string? contentHashPrefix = null,
            long byteSize = default,
            CanonicalAudioUploadSessionState state = default,
            int attemptCount = 0,
            CanonicalTimestamp? nextRetryAt = null,
            string? lastErrorCode = null,
            bool terminalConflict = false,
            DateTime? updatedAt = null)
        {
            ObjectID = CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
            SessionID = sessionID;
            Offset = offset ?? new CanonicalAudioUploadOffset();
            ChunkSize = Math.Max(1, chunkSize);
            ContentHashPrefix = contentHash != null
                ? CanonicalAudioUploadRuntimeRedaction.HashPrefix(contentHash.Value)
                : CanonicalAudioUploadRuntimeRedaction.HashPrefix(contentHashPrefix);
            ByteSize = Math.Max(0, byteSize);
            State = state;
            AttemptCount = Math.Max(0, attemptCount);
            NextRetryAt = nextRetryAt;
            LastErrorCode = CanonicalAudioUploadRuntimeRedaction.SafeText(lastErrorCode);
            TerminalConflict = terminalConflict;
            UpdatedAt = new CanonicalTimestamp(updatedAt ?? DateTime.UtcNow);
        }

        public CanonicalAudioUploadResumeToken? ResumeToken
        {
            get
            {
                if (SessionID == null) return null;
                return new CanonicalAudioUploadResumeToken(
                    ObjectID, SessionID, Offset, ByteSize, ContentHashPrefix);
            }
        }

        public bool IsEligibleRetry(DateTime now)
        {
            if (TerminalConflict) return false;
            if (State.IsTerminal() && State != CanonicalAudioUploadSessionState.interrupted) return false;
            if (State == CanonicalAudioUploadSessionState.conflict
                || State == CanonicalAudioUploadSessionState.blocked
                || State == CanonicalAudioUploadSessionState.aborted
                || State == CanonicalAudioUploadSessionState.finalized
                || State == CanonicalAudioUploadSessionState.failed)
                return false;
            if (NextRetryAt == null) return State == CanonicalAudioUploadSessionState.interrupted;
            return NextRetryAt.Date <= now;
        }
    }

    public record CanonicalAudioUploadSession : IEquatable<CanonicalAudioUploadSession>
    {
        public string ObjectID { get; private set; }
        public CanonicalUploadSessionID? SessionID { get; private set; }
        public CanonicalAudioUploadSessionState State { get; private set; }
        public long ConfirmedBytes { get; private set; }
        public CanonicalAudioUploadOffset Offset { get; private set; }
        public int ChunkSize { get; private set; }
        public long ExpectedByteSize { get; private set; }
        public string? ContentHashPrefix { get; private set; }
        public CanonicalAudioUploadFinalizeProof? FinalizedProof { get; private set; }
        public string? LastErrorCode { get; private set; }

        public CanonicalAudioUploadSession(
            string objectID,
            CanonicalUploadSessionID? sessionID = null,
            CanonicalAudioUploadSessionState state = CanonicalAudioUploadSessionState.idle,
            long confirmedBytes = 0,
            int chunkSize = default,
            long expectedByteSize = default,
            CanonicalHash? contentHash = null,
            string? contentHashPrefix = null)
        {
            ObjectID = CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
            SessionID = sessionID;
            State = state;
            ConfirmedBytes = Math.Max(0, confirmedBytes);
            Offset = new CanonicalAudioUploadOffset(confirmedBytes);
            ChunkSize = Math.Max(1, chunkSize);
            ExpectedByteSize = Math.Max(0, expectedByteSize);
            ContentHashPrefix = contentHash != null
                ? CanonicalAudioUploadRuntimeRedaction.HashPrefix(contentHash.Value)
                : CanonicalAudioUploadRuntimeRedaction.HashPrefix(contentHashPrefix);
            FinalizedProof = null;
            LastErrorCode = null;
        }

        public void MarkStarted(CanonicalUploadSessionID sessionID, long confirmedBytes = 0)
        {
            SessionID = sessionID;
            State = CanonicalAudioUploadSessionState.started;
            UpdateConfirmedBytes(confirmedBytes);
        }

        public void UpdateConfirmedBytes(long newConfirmedBytes)
        {
            var bounded = Math.Min(Math.Max(0, newConfirmedBytes), ExpectedByteSize);
            if (bounded < ConfirmedBytes)
                throw new CanonicalAudioUploadRuntimeError.ConfirmedBytesRegressedException(
                    ConfirmedBytes, bounded);
            ConfirmedBytes = bounded;
            Offset = new CanonicalAudioUploadOffset(bounded);
        }

        public void Confirm(CanonicalAudioUploadChunk chunk, long serverConfirmedBytes)
        {
            if (chunk.ObjectID != ObjectID)
                throw new CanonicalAudioUploadRuntimeError.SessionConflictException("objectMismatch");
            if (chunk.Offset.Value < ConfirmedBytes)
            {
                if (chunk.EndOffset > ConfirmedBytes)
                    throw new CanonicalAudioUploadRuntimeError.ChunkOffsetMismatchException(
                        ConfirmedBytes, chunk.Offset.Value);
                return;
            }
            if (chunk.Offset.Value != ConfirmedBytes)
                throw new CanonicalAudioUploadRuntimeError.ChunkOffsetMismatchException(
                    ConfirmedBytes, chunk.Offset.Value);
            if (chunk.EndOffset > ExpectedByteSize)
                throw new CanonicalAudioUploadRuntimeError.ChunkOffsetMismatchException(
                    ExpectedByteSize, chunk.EndOffset);
            State = CanonicalAudioUploadSessionState.chunking;
            UpdateConfirmedBytes(Math.Max(serverConfirmedBytes, chunk.EndOffset));
        }

        public void MarkFinalized(CanonicalAudioUploadFinalizeProof proof)
        {
            if (!proof.Accepted)
            {
                State = CanonicalAudioUploadSessionState.conflict;
                LastErrorCode = "finalizeProofRejected";
                throw new CanonicalAudioUploadRuntimeError.FinalizeProofRejectedException(
                    "finalizeProofRejected");
            }
            if (proof.ByteSize != ExpectedByteSize)
            {
                State = CanonicalAudioUploadSessionState.conflict;
                LastErrorCode = "finalByteSizeMismatch";
                throw new CanonicalAudioUploadRuntimeError.FinalByteSizeMismatchException(
                    ExpectedByteSize, proof.ByteSize);
            }
            FinalizedProof = proof;
            State = CanonicalAudioUploadSessionState.finalized;
            ConfirmedBytes = ExpectedByteSize;
            Offset = new CanonicalAudioUploadOffset(ExpectedByteSize);
            LastErrorCode = null;
        }
    }

    public class CanonicalAudioUploadRuntimeError : Exception
    {
        protected CanonicalAudioUploadRuntimeError(string message) : base(message) { }

        public class ModeBlockedException : CanonicalAudioUploadRuntimeError
        { public ModeBlockedException(string msg) : base(msg) { } }
        public class MissingSourceException : CanonicalAudioUploadRuntimeError
        { public MissingSourceException(string msg) : base(msg) { } }
        public class LocalAudioIncompleteException : CanonicalAudioUploadRuntimeError
        { public LocalAudioIncompleteException(string msg) : base(msg) { } }
        public class PeerUnknownDeferredException : CanonicalAudioUploadRuntimeError
        { public PeerUnknownDeferredException(string msg) : base(msg) { } }
        public class ConflictBlockedException : CanonicalAudioUploadRuntimeError
        { public ConflictBlockedException(string msg) : base(msg) { } }
        public class RetryDrainerFreshJobSuppressedException : CanonicalAudioUploadRuntimeError
        { public RetryDrainerFreshJobSuppressedException(string msg) : base(msg) { } }
        public class CompletedLedgerRejectedAsNoOpException : CanonicalAudioUploadRuntimeError
        { public CompletedLedgerRejectedAsNoOpException(string msg) : base(msg) { } }
        public class ConfirmedBytesRegressedException : CanonicalAudioUploadRuntimeError
        {
            public long Previous { get; }
            public long Actual { get; }
            public ConfirmedBytesRegressedException(long previous, long actual)
                : base($"confirmedBytesRegressed: previous={previous}, actual={actual}")
            { Previous = previous; Actual = actual; }
        }
        public class ChunkOffsetMismatchException : CanonicalAudioUploadRuntimeError
        {
            public long Expected { get; }
            public long Actual { get; }
            public ChunkOffsetMismatchException(long expected, long actual)
                : base($"chunkOffsetMismatch: expected={expected}, actual={actual}")
            { Expected = expected; Actual = actual; }
        }
        public class ChunkReadReturnedEmptyException : CanonicalAudioUploadRuntimeError
        { public long Offset { get; }
          public ChunkReadReturnedEmptyException(long offset) : base($"chunkReadReturnedEmpty at offset={offset}")
          { Offset = offset; } }
        public class SessionConflictException : CanonicalAudioUploadRuntimeError
        { public SessionConflictException(string msg) : base(msg) { } }
        public class FinalizeProofRejectedException : CanonicalAudioUploadRuntimeError
        { public FinalizeProofRejectedException(string msg) : base(msg) { } }
        public class FinalByteSizeMismatchException : CanonicalAudioUploadRuntimeError
        {
            public long Expected { get; }
            public long Actual { get; }
            public FinalByteSizeMismatchException(long expected, long actual)
                : base($"finalByteSizeMismatch: expected={expected}, actual={actual}")
            { Expected = expected; Actual = actual; }
        }
        public class FinalHashMismatchException : CanonicalAudioUploadRuntimeError
        {
            public string? ExpectedPrefix { get; }
            public string? ActualPrefix { get; }
            public FinalHashMismatchException(string? expectedPrefix, string? actualPrefix)
                : base($"finalHashMismatch: expected={expectedPrefix}, actual={actualPrefix}")
            { ExpectedPrefix = expectedPrefix; ActualPrefix = actualPrefix; }
        }
    }

    public interface ICanonicalAudioUploadByteSource
    {
        string ObjectID { get; }
        CanonicalFileReference TargetReference { get; }
        long ByteSize { get; }
        CanonicalHash ContentHash { get; }
        int PreferredChunkSize { get; }
        Task<byte[]> ReadChunkAsync(CanonicalAudioUploadOffset offset, int maxLength);
    }

    public class CanonicalAudioUploadJobStore
    {
        private class Ledger
        {
            public int SchemaVersion { get; set; }
            public List<CanonicalAudioUploadRetryRecord> Records { get; set; } = new();
        }

        private readonly string? _persistencePath;
        private Dictionary<string, CanonicalAudioUploadRetryRecord> _records;
        private readonly object _lock = new();

        public CanonicalAudioUploadJobStore(
            string? persistencePath = null,
            List<CanonicalAudioUploadRetryRecord>? initialRecords = null)
        {
            _persistencePath = persistencePath;
            _records = (initialRecords ?? new List<CanonicalAudioUploadRetryRecord>())
                .ToDictionary(r => r.ObjectID);

            if (_persistencePath != null && File.Exists(_persistencePath))
            {
                try
                {
                    var data = File.ReadAllText(_persistencePath);
                    var ledger = JsonSerializer.Deserialize<Ledger>(data);
                    if (ledger?.Records != null)
                        _records = ledger.Records.ToDictionary(r => r.ObjectID);
                }
                catch { }
            }
        }

        public CanonicalAudioUploadRetryRecord? RecordFor(string objectID)
        {
            var key = CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording");
            lock (_lock) { return _records.GetValueOrDefault(key); }
        }

        public List<CanonicalAudioUploadRetryRecord> AllRecords()
        {
            lock (_lock) { return _records.Values.OrderBy(r => r.ObjectID).ToList(); }
        }

        public Task<CanonicalAudioUploadRetryRecord> UpsertAsync(CanonicalAudioUploadRetryRecord record)
        {
            lock (_lock)
            {
                _records[record.ObjectID] = record;
                Persist();
                return Task.FromResult(record);
            }
        }

        public Task RemoveAsync(string objectID)
        {
            lock (_lock)
            {
                _records.Remove(CanonicalAudioUploadRuntimeRedaction.SafeIdentifier(objectID, "unknown-recording"));
                Persist();
            }
            return Task.CompletedTask;
        }

        public Task<bool> HasEligibleRetryAsync(string objectID, DateTime now)
        {
            var record = RecordFor(objectID);
            return Task.FromResult(record?.IsEligibleRetry(now) == true);
        }

        public List<CanonicalAudioUploadRetryRecord> EligibleRetryRecords(DateTime now)
        {
            lock (_lock)
            {
                return _records.Values
                    .Where(r => r.IsEligibleRetry(now))
                    .OrderBy(r => r.ObjectID)
                    .ToList();
            }
        }

        public async Task<CanonicalAudioUploadRetryRecord> RecordProgressAsync(
            string objectID,
            CanonicalUploadSessionID? sessionID,
            CanonicalAudioUploadOffset offset,
            int chunkSize,
            CanonicalHash contentHash,
            long byteSize,
            CanonicalAudioUploadSessionState state,
            DateTime now)
        {
            var existing = RecordFor(objectID);
            var monotonicOffset = new CanonicalAudioUploadOffset(
                Math.Max(existing?.Offset.Value ?? 0, offset.Value));
            var record = new CanonicalAudioUploadRetryRecord(
                objectID: objectID,
                sessionID: sessionID,
                offset: monotonicOffset,
                chunkSize: chunkSize,
                contentHash: contentHash,
                byteSize: byteSize,
                state: state,
                attemptCount: existing?.AttemptCount ?? 0,
                nextRetryAt: existing?.NextRetryAt,
                lastErrorCode: existing?.LastErrorCode,
                terminalConflict: existing?.TerminalConflict ?? false,
                updatedAt: now);
            return await UpsertAsync(record);
        }

        public async Task<CanonicalAudioUploadRetryRecord> ScheduleRetryAsync(
            string objectID,
            CanonicalUploadSessionID? sessionID,
            CanonicalAudioUploadOffset offset,
            int chunkSize,
            CanonicalHash contentHash,
            long byteSize,
            CanonicalAudioUploadRetryPolicy policy,
            string errorCode,
            DateTime now)
        {
            var existing = RecordFor(objectID);
            var nextAttempt = (existing?.AttemptCount ?? 0) + 1;
            var state = nextAttempt >= policy.MaxAttempts
                ? CanonicalAudioUploadSessionState.failed
                : CanonicalAudioUploadSessionState.interrupted;
            var nextRetryAt = state == CanonicalAudioUploadSessionState.failed
                ? (CanonicalTimestamp?)null
                : new CanonicalTimestamp(now + policy.RetryDelay);
            var retry = new CanonicalAudioUploadRetryRecord(
                objectID: objectID,
                sessionID: sessionID,
                offset: new CanonicalAudioUploadOffset(Math.Max(existing?.Offset.Value ?? 0, offset.Value)),
                chunkSize: chunkSize,
                contentHash: contentHash,
                byteSize: byteSize,
                state: state,
                attemptCount: nextAttempt,
                nextRetryAt: nextRetryAt,
                lastErrorCode: errorCode,
                terminalConflict: false,
                updatedAt: now);
            return await UpsertAsync(retry);
        }

        public async Task<CanonicalAudioUploadRetryRecord> MarkConflictAsync(
            string objectID,
            CanonicalUploadSessionID? sessionID,
            CanonicalAudioUploadOffset offset,
            int chunkSize,
            CanonicalHash contentHash,
            long byteSize,
            string errorCode,
            DateTime now)
        {
            var existing = RecordFor(objectID);
            var conflict = new CanonicalAudioUploadRetryRecord(
                objectID: objectID,
                sessionID: sessionID,
                offset: new CanonicalAudioUploadOffset(Math.Max(existing?.Offset.Value ?? 0, offset.Value)),
                chunkSize: chunkSize,
                contentHash: contentHash,
                byteSize: byteSize,
                state: CanonicalAudioUploadSessionState.conflict,
                attemptCount: existing?.AttemptCount ?? 0,
                nextRetryAt: null,
                lastErrorCode: errorCode,
                terminalConflict: true,
                updatedAt: now);
            return await UpsertAsync(conflict);
        }

        private void Persist()
        {
            if (_persistencePath == null) return;
            try
            {
                var directory = Path.GetDirectoryName(_persistencePath);
                if (directory != null) Directory.CreateDirectory(directory);
                var ledger = new Ledger
                {
                    SchemaVersion = 1,
                    Records = _records.Values.OrderBy(r => r.ObjectID).ToList()
                };
                var json = JsonSerializer.Serialize(ledger, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(_persistencePath, json);
            }
            catch { }
        }
    }

    public class CanonicalAudioUploadRuntimeExecutor
    {
        public CanonicalAudioUploadRuntimeExecutor() { }

        public async Task<CanonicalAudioUploadRuntimeResult> ExecuteAsync(
            CanonicalAudioUploadCutoverCandidate candidate,
            ICanonicalAudioUploadByteSource source,
            ICanonicalProductionUploadPort uploadPort,
            CanonicalAudioUploadJobStore jobStore,
            CanonicalAudioUploadRuntimeConfiguration configuration,
            string? syncRunID = null,
            CanonicalAudioUploadNodeRole nodeRole = CanonicalAudioUploadNodeRole.iPhone,
            DateTime? nowParam = null)
        {
            var now = nowParam ?? DateTime.UtcNow;
            var diagnostics = CandidateDiagnostics(
                candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeModeEvaluated,
                syncRunID, nodeRole, configuration.Mode.ToString(),
                "defaultReleaseDisabledUnlessExplicitlyEnabled");

            var mode = configuration.Mode;
            var policy = configuration.Policy;

            if (mode == CanonicalAudioUploadRuntimeMode.disabled)
            {
                diagnostics.Add(LegacyFallbackDiagnostic(candidate, syncRunID, nodeRole, "runtimeDisabled"));
                return new CanonicalAudioUploadRuntimeResult(
                    mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.legacyFallback,
                    objectID: candidate.ObjectID,
                    usedLegacyFallback: policy.LegacyFallbackEnabled,
                    legacyFallbackReason: "runtimeDisabled", diagnostics: diagnostics);
            }

            if (mode == CanonicalAudioUploadRuntimeMode.blocked)
            {
                return BlockedResult(mode, candidate, diagnostics, "runtimeBlocked");
            }

            var decision = await CandidateDecisionAsync(candidate, jobStore, policy, now);
            diagnostics.AddRange(decision.Diagnostics.Select(d =>
                CandidateDiagnostics(candidate, d, syncRunID, nodeRole)[0]));

            switch (decision.Action)
            {
                case CandidateDecisionEnum.noOp:
                    return new CanonicalAudioUploadRuntimeResult(
                        mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.noOp,
                        objectID: candidate.ObjectID,
                        confirmedBytes: candidate.LocalTruth.ByteSize ?? 0,
                        completed: true, diagnostics: diagnostics);
                case CandidateDecisionEnum.deferred:
                    return new CanonicalAudioUploadRuntimeResult(
                        mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.deferred,
                        objectID: candidate.ObjectID, diagnostics: diagnostics);
                case CandidateDecisionEnum.conflict:
                    return new CanonicalAudioUploadRuntimeResult(
                        mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.conflict,
                        objectID: candidate.ObjectID, diagnostics: diagnostics);
                case CandidateDecisionEnum blocked when blocked.Reason is string reason:
                    if (ShouldUseLegacyFallback(mode, policy, reason))
                    {
                        diagnostics.Add(LegacyFallbackDiagnostic(candidate, syncRunID, nodeRole, reason));
                        return new CanonicalAudioUploadRuntimeResult(
                            mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.legacyFallback,
                            objectID: candidate.ObjectID,
                            usedLegacyFallback: true, legacyFallbackReason: reason,
                            diagnostics: diagnostics);
                    }
                    return BlockedResult(mode, candidate, diagnostics, reason);
            }

            if (mode == CanonicalAudioUploadRuntimeMode.diagnosticsOnly)
            {
                diagnostics.Add(CandidateDiagnostics(
                    candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeCandidateSelected,
                    syncRunID, nodeRole, "wouldUpload", candidate.Reason)[0]);
                return new CanonicalAudioUploadRuntimeResult(
                    mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.diagnosticsOnly,
                    objectID: candidate.ObjectID, diagnostics: diagnostics);
            }

            if (mode == CanonicalAudioUploadRuntimeMode.noCommit)
            {
                diagnostics.Add(CandidateDiagnostics(
                    candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeCandidateSelected,
                    syncRunID, nodeRole, "noCommitWouldUpload", candidate.Reason)[0]);
                return new CanonicalAudioUploadRuntimeResult(
                    mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.noCommit,
                    objectID: candidate.ObjectID, diagnostics: diagnostics);
            }

            var modeBlocker = ModeBlocker(mode, policy, uploadPort);
            if (modeBlocker == null)
            {
                return await PerformUploadAsync(
                    candidate, source, uploadPort, jobStore, configuration,
                    diagnostics, syncRunID, nodeRole, now);
            }

            if (ShouldUseLegacyFallback(mode, policy, modeBlocker))
            {
                diagnostics.Add(LegacyFallbackDiagnostic(candidate, syncRunID, nodeRole, modeBlocker));
                return new CanonicalAudioUploadRuntimeResult(
                    mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.legacyFallback,
                    objectID: candidate.ObjectID,
                    usedLegacyFallback: true, legacyFallbackReason: modeBlocker,
                    diagnostics: diagnostics);
            }
            return BlockedResult(mode, candidate, diagnostics, modeBlocker);
        }

        private async Task<CanonicalAudioUploadRuntimeResult> PerformUploadAsync(
            CanonicalAudioUploadCutoverCandidate candidate,
            ICanonicalAudioUploadByteSource source,
            ICanonicalProductionUploadPort uploadPort,
            CanonicalAudioUploadJobStore jobStore,
            CanonicalAudioUploadRuntimeConfiguration configuration,
            List<CanonicalAudioUploadDiagnostic> diagnostics,
            string? syncRunID,
            CanonicalAudioUploadNodeRole nodeRole,
            DateTime now)
        {
            var policy = configuration.Policy;
            var chunkSize = new[] { Math.Max(1, source.PreferredChunkSize), Math.Max(1, uploadPort.ChunkSizePolicy), Math.Max(1, policy.ChunkSize) }.Min();
            var sentChunkCount = 0;
            var createdJob = false;
            var startedTransport = false;

            if (source.ObjectID != candidate.ObjectID)
                return BlockedResult(configuration.Mode, candidate, diagnostics, "sourceObjectMismatch");
            if (source.ByteSize <= 0)
                return BlockedResult(configuration.Mode, candidate, diagnostics, "localAudioByteSizeUnavailable");

            try
            {
                diagnostics.Add(CandidateDiagnostics(
                    candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeCandidateSelected,
                    syncRunID, nodeRole, "upload", candidate.Reason)[0]);

                var existingRecord = jobStore.RecordFor(candidate.ObjectID);
                var session = new CanonicalAudioUploadSession(
                    objectID: candidate.ObjectID,
                    sessionID: existingRecord?.SessionID,
                    state: existingRecord?.SessionID == null
                        ? CanonicalAudioUploadSessionState.idle
                        : CanonicalAudioUploadSessionState.interrupted,
                    confirmedBytes: existingRecord?.Offset.Value ?? 0,
                    chunkSize: chunkSize,
                    expectedByteSize: source.ByteSize,
                    contentHash: source.ContentHash);

                if (candidate.Trigger.IsRetryDrainer())
                {
                    var eligible = await jobStore.HasEligibleRetryAsync(candidate.ObjectID, now);
                    if (!eligible)
                    {
                        diagnostics.Add(CandidateDiagnostics(
                            candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRetryDrainerFreshJobSuppressed,
                            syncRunID, nodeRole, "blocked", "retryDrainerCannotCreateFreshAudioUploadJob")[0]);
                        return BlockedResult(configuration.Mode, candidate, diagnostics,
                            "retryDrainerCannotCreateFreshAudioUploadJob");
                    }
                }

                CanonicalUploadSessionStatus status;
                if (existingRecord?.ResumeToken != null)
                {
                    session.MarkStarted(existingRecord.SessionID!, existingRecord.Offset.Value);
                    session.State = CanonicalAudioUploadSessionState.resuming;
                    diagnostics.Add(CandidateDiagnostics(
                        candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeResumeStarted,
                        syncRunID, nodeRole, "status", "staleSessionStatusRefresh")[0]);
                    status = await uploadPort.ResumeUploadAsync(
                        new CanonicalUploadStatusRequest(
                            candidate.ObjectID,
                            existingRecord.ResumeToken.SessionID!,
                            source.ContentHash),
                        now);
                }
                else
                {
                    session.State = CanonicalAudioUploadSessionState.starting;
                    diagnostics.Add(CandidateDiagnostics(
                        candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeStarted,
                        syncRunID, nodeRole, "start",
                        "existingSecureRoute:/upload-recording-audio-session/start")[0]);
                    status = await uploadPort.StartResumableUploadAsync(
                        new CanonicalUploadStartRequest(
                            candidate.ObjectID,
                            source.TargetReference,
                            source.ByteSize,
                            source.ContentHash,
                            chunkSize,
                            $"audio-start:{candidate.ObjectID}:{(source.ContentHash.Value.Length > 12 ? source.ContentHash.Value[..12] : source.ContentHash.Value)}"),
                        now);
                    createdJob = true;
                    startedTransport = true;
                }

                var sessionID = status.SessionID ?? session.SessionID
                    ?? throw new CanonicalAudioUploadRuntimeError.SessionConflictException("missingSessionID");
                if (status.ConfirmedBytes < session.ConfirmedBytes)
                    throw new CanonicalAudioUploadRuntimeError.ConfirmedBytesRegressedException(
                        session.ConfirmedBytes, status.ConfirmedBytes);

                session.MarkStarted(sessionID, status.ConfirmedBytes);
                existingRecord = await jobStore.RecordProgressAsync(
                    candidate.ObjectID, sessionID, session.Offset, chunkSize,
                    source.ContentHash, source.ByteSize, session.State, now);

                while (session.ConfirmedBytes < source.ByteSize)
                {
                    var remaining = source.ByteSize - session.ConfirmedBytes;
                    var readLength = (int)Math.Min(chunkSize, remaining);
                    var data = await source.ReadChunkAsync(session.Offset, readLength);
                    if (data == null || data.Length == 0)
                        throw new CanonicalAudioUploadRuntimeError.ChunkReadReturnedEmptyException(
                            session.Offset.Value);

                    var chunkHash = CanonicalTransportEnvelope.Hash(data);
                    var runtimeChunk = new CanonicalAudioUploadChunk(
                        candidate.ObjectID, sessionID, session.Offset, data.Length, chunkHash);
                    diagnostics.Add(CandidateDiagnostics(
                        candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeChunkSent,
                        syncRunID, nodeRole, $"offset:{runtimeChunk.Offset.Value}",
                        "existingSecureRoute:/upload-recording-audio-session/chunk")[0]);

                    var chunkStatus = await uploadPort.UploadChunkAsync(
                        new CanonicalUploadChunk(
                            candidate.ObjectID, sessionID, session.Offset.Value, data,
                            chunkHash, source.ContentHash, runtimeChunk.IdempotencyKey),
                        now);
                    startedTransport = true;
                    sentChunkCount++;
                    session.Confirm(runtimeChunk, chunkStatus.ConfirmedBytes);
                    diagnostics.Add(CandidateDiagnostics(
                        candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeChunkConfirmed,
                        syncRunID, nodeRole, $"confirmed:{session.ConfirmedBytes}",
                        "monotonicConfirmedBytes")[0]);
                    existingRecord = await jobStore.RecordProgressAsync(
                        candidate.ObjectID, sessionID, session.Offset, chunkSize,
                        source.ContentHash, source.ByteSize,
                        CanonicalAudioUploadSessionState.chunking, now);
                }

                session.State = CanonicalAudioUploadSessionState.finalizing;
                diagnostics.Add(CandidateDiagnostics(
                    candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeFinalizeStarted,
                    syncRunID, nodeRole, "finalize",
                    "existingSecureRoute:/upload-recording-audio-session/finalize")[0]);
                await jobStore.RecordProgressAsync(
                    candidate.ObjectID, sessionID, session.Offset, chunkSize,
                    source.ContentHash, source.ByteSize,
                    CanonicalAudioUploadSessionState.finalizing, now);

                var finalize = await uploadPort.FinalizeUploadAsync(
                    new CanonicalUploadFinalizeRequest(
                        candidate.ObjectID, sessionID, source.ByteSize, source.ContentHash),
                    now);
                var actualFileSize = finalize.FileSize ?? -1;
                if (!finalize.Completed || actualFileSize != source.ByteSize)
                    throw new CanonicalAudioUploadRuntimeError.FinalByteSizeMismatchException(
                        source.ByteSize, actualFileSize);
                var actualChecksum = finalize.Checksum;
                if (!Equals(actualChecksum, source.ContentHash))
                    throw new CanonicalAudioUploadRuntimeError.FinalHashMismatchException(
                        CanonicalAudioUploadRuntimeRedaction.HashPrefix(source.ContentHash.Value),
                        actualChecksum != null
                            ? CanonicalAudioUploadRuntimeRedaction.HashPrefix(actualChecksum.Value)
                            : null);

                var proof = new CanonicalAudioUploadFinalizeProof(
                    candidate.ObjectID, sessionID, source.ByteSize, source.ContentHash,
                    true, true, true, finalize.Completed);
                session.MarkFinalized(proof);
                await uploadPort.WriteUploadLedgerAsync(
                    new CanonicalProductionUploadLedgerSnapshot(
                        candidate.ObjectID, sessionID, source.ByteSize,
                        source.ByteSize, source.ContentHash,
                        CanonicalAudioUploadLedgerPhase.completed));
                var finalizedRecord = await jobStore.RecordProgressAsync(
                    candidate.ObjectID, sessionID, new CanonicalAudioUploadOffset(source.ByteSize),
                    chunkSize, source.ContentHash, source.ByteSize,
                    CanonicalAudioUploadSessionState.finalized, now);

                diagnostics.Add(CandidateDiagnostics(
                    candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeFinalizeCompleted,
                    syncRunID, nodeRole, "verified", "hashAndByteSizeVerified")[0]);
                return new CanonicalAudioUploadRuntimeResult(
                    mode: configuration.Mode, outcome: CanonicalAudioUploadRuntimeOutcome.uploaded,
                    objectID: candidate.ObjectID, sessionID: sessionID,
                    createdJob: createdJob, startedTransport: startedTransport,
                    sentChunkCount: sentChunkCount, confirmedBytes: source.ByteSize,
                    completed: true, finalizeProof: proof, retryRecord: finalizedRecord,
                    diagnostics: diagnostics);
            }
            catch (Exception error)
            {
                var rec = jobStore.RecordFor(candidate.ObjectID);
                var sessionID = rec?.SessionID;
                var offset = rec?.Offset ?? new CanonicalAudioUploadOffset();
                var code = ErrorCode(error);
                diagnostics.Add(CandidateDiagnostics(
                    candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeFinalizeFailed,
                    syncRunID, nodeRole, "failed", code)[0]);

                if (IsConflict(error))
                {
                    CanonicalAudioUploadRetryRecord? conflict = null;
                    try
                    {
                        conflict = await jobStore.MarkConflictAsync(
                            candidate.ObjectID, sessionID, offset, chunkSize,
                            source.ContentHash, source.ByteSize, code, now);
                    }
                    catch { }
                    diagnostics.Add(CandidateDiagnostics(
                        candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeConflictBlocked,
                        syncRunID, nodeRole, "blocked", code)[0]);
                    return new CanonicalAudioUploadRuntimeResult(
                        mode: configuration.Mode, outcome: CanonicalAudioUploadRuntimeOutcome.conflict,
                        objectID: candidate.ObjectID, sessionID: sessionID,
                        createdJob: createdJob, startedTransport: startedTransport,
                        sentChunkCount: sentChunkCount, confirmedBytes: offset.Value,
                        retryRecord: conflict, diagnostics: diagnostics);
                }

                CanonicalAudioUploadRetryRecord? retry = null;
                try
                {
                    retry = await jobStore.ScheduleRetryAsync(
                        candidate.ObjectID, sessionID, offset, chunkSize,
                        source.ContentHash, source.ByteSize, policy.RetryPolicy, code, now);
                }
                catch { }
                diagnostics.Add(CandidateDiagnostics(
                    candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeRetryScheduled,
                    syncRunID, nodeRole, retry?.State.ToString() ?? "failed", code)[0]);
                if (policy.LegacyFallbackEnabled)
                    diagnostics.Add(LegacyFallbackDiagnostic(candidate, syncRunID, nodeRole, code));

                return new CanonicalAudioUploadRuntimeResult(
                    mode: configuration.Mode,
                    outcome: retry?.State == CanonicalAudioUploadSessionState.failed
                        ? CanonicalAudioUploadRuntimeOutcome.failed
                        : CanonicalAudioUploadRuntimeOutcome.retryScheduled,
                    objectID: candidate.ObjectID, sessionID: sessionID,
                    createdJob: createdJob, startedTransport: startedTransport,
                    sentChunkCount: sentChunkCount,
                    confirmedBytes: retry?.Offset.Value ?? offset.Value,
                    usedLegacyFallback: policy.LegacyFallbackEnabled,
                    legacyFallbackReason: policy.LegacyFallbackEnabled ? code : null,
                    retryRecord: retry, diagnostics: diagnostics);
            }
        }

        private enum CandidateDecisionEnum
        {
            noOp, deferred, conflict, upload
        }
        private class CandidateDecisionVal
        {
            public CandidateDecisionEnum Action { get; set; }
            public string? Reason { get; set; }
            public static implicit operator CandidateDecisionVal(CandidateDecisionEnum action) =>
                new() { Action = action };
            public static CandidateDecisionVal Blocked(string reason) =>
                new() { Action = CandidateDecisionEnum.upload, Reason = reason };
            public bool IsBlocked => Reason != null;
            public bool IsUpload => Action == CandidateDecisionEnum.upload && Reason == null;
        }

        private class CandidateDecisionResult
        {
            public CandidateDecisionVal Action { get; set; } = new();
            public List<CanonicalAudioUploadDiagnosticKind> Diagnostics { get; set; } = new();
        }

        private async Task<CandidateDecisionResult> CandidateDecisionAsync(
            CanonicalAudioUploadCutoverCandidate candidate,
            CanonicalAudioUploadJobStore jobStore,
            CanonicalAudioUploadRuntimePolicy policy,
            DateTime now)
        {
            var diagnostics = new List<CanonicalAudioUploadDiagnosticKind>();

            if (candidate.Trigger.IsViewRefresh())
            {
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadViewRefreshSuppressed);
                return new CandidateDecisionResult
                {
                    Action = CandidateDecisionVal.Blocked("viewRefreshNeverCreatesAudioUploadCandidate"),
                    Diagnostics = diagnostics
                };
            }
            if (candidate.Trigger.IsRetryDrainer())
            {
                var storeHasRetry = await jobStore.HasEligibleRetryAsync(candidate.ObjectID, now);
                if (policy.RetryDrainerRequiresExistingRetry
                    && !candidate.RetryTruth.HasExistingEligibleRetry
                    && !storeHasRetry)
                {
                    diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRetryDrainerFreshJobSuppressed);
                    return new CandidateDecisionResult
                    {
                        Action = CandidateDecisionVal.Blocked("retryDrainerCannotCreateFreshAudioUploadJob"),
                        Diagnostics = diagnostics
                    };
                }
            }
            if (candidate.EvidenceBlockers.Contains(CanonicalAudioUploadEvidenceBlocker.completedLedgerWithoutPeerMatch))
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeCompletedLedgerRejectedAsNoOp);
            if (candidate.ActionKind == CanonicalAudioUploadActionKind.audioUploadNoOp)
                return new CandidateDecisionResult { Action = CandidateDecisionEnum.noOp, Diagnostics = diagnostics };
            if (candidate.ActionKind == CanonicalAudioUploadActionKind.audioUploadDeferredPeerUnknown
                || candidate.EvidenceBlockers.Contains(CanonicalAudioUploadEvidenceBlocker.peerUnknown))
            {
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimePeerUnknownDeferred);
                return new CandidateDecisionResult { Action = CandidateDecisionEnum.deferred, Diagnostics = diagnostics };
            }
            if (candidate.ActionKind == CanonicalAudioUploadActionKind.audioUploadConflictRecord
                || candidate.EvidenceStatus == CanonicalAudioUploadEvidenceStatus.conflict)
            {
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeExistingDifferentAudioBlocked);
                diagnostics.Add(CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeConflictBlocked);
                return new CandidateDecisionResult { Action = CandidateDecisionEnum.conflict, Diagnostics = diagnostics };
            }
            if (candidate.EvidenceStatus == CanonicalAudioUploadEvidenceStatus.blocked
                && candidate.EvidenceBlockers.Count > 0)
            {
                return new CandidateDecisionResult
                {
                    Action = CandidateDecisionVal.Blocked(
                        string.Join(",", candidate.EvidenceBlockers.Select(b => b.ToString()))),
                    Diagnostics = diagnostics
                };
            }
            if (candidate.ActionKind != CanonicalAudioUploadActionKind.audioUploadCanaryCandidate
                || candidate.EvidenceStatus != CanonicalAudioUploadEvidenceStatus.complete
                || !candidate.LocalTruth.SufficientForUploadCandidate)
            {
                return new CandidateDecisionResult
                {
                    Action = CandidateDecisionVal.Blocked(candidate.Reason),
                    Diagnostics = diagnostics
                };
            }
            return new CandidateDecisionResult { Action = CandidateDecisionEnum.upload, Diagnostics = diagnostics };
        }

        private string? ModeBlocker(
            CanonicalAudioUploadRuntimeMode mode,
            CanonicalAudioUploadRuntimePolicy policy,
            ICanonicalProductionUploadPort uploadPort)
        {
            if (!policy.RequireExistingSecureUploadRoutes) return "existingSecureUploadRoutesRequired";
            if (!uploadPort.ResumableSessionSupported) return "resumableSessionUnsupported";
            return mode switch
            {
                CanonicalAudioUploadRuntimeMode.testTransportUpload =>
                    policy.AllowTestTransportUpload ? null : "testTransportUploadNotAllowed",
                CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback =>
                    !policy.DebugInternalBuild ? "canonicalUploadRequiresDebugInternalBuild"
                    : !policy.OwnerApprovedCanonicalCommit ? "canonicalUploadOwnerApprovalMissing"
                    : !policy.AllowCanonicalUploadWithLegacyFallback ? "canonicalUploadPolicyDisabled"
                    : uploadPort.IsDryRunOnly ? "canonicalUploadRequiresRealSecureUploadPort"
                    : null,
                _ => null
            };
        }

        private bool ShouldUseLegacyFallback(CanonicalAudioUploadRuntimeMode mode,
            CanonicalAudioUploadRuntimePolicy policy, string reason)
        {
            if (!policy.LegacyFallbackEnabled) return false;
            if (mode == CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback) return true;
            if (reason.Contains("manualUploadButton")) return true;
            return mode == CanonicalAudioUploadRuntimeMode.disabled;
        }

        private CanonicalAudioUploadRuntimeResult BlockedResult(
            CanonicalAudioUploadRuntimeMode mode,
            CanonicalAudioUploadCutoverCandidate candidate,
            List<CanonicalAudioUploadDiagnostic> diagnostics,
            string reason)
        {
            var allDiagnostics = new List<CanonicalAudioUploadDiagnostic>(diagnostics);
            allDiagnostics.AddRange(CandidateDiagnostics(
                candidate, CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeConflictBlocked,
                result: "blocked", reason: reason));
            return new CanonicalAudioUploadRuntimeResult(
                mode: mode, outcome: CanonicalAudioUploadRuntimeOutcome.blocked,
                objectID: candidate.ObjectID, diagnostics: allDiagnostics);
        }

        private CanonicalAudioUploadDiagnostic LegacyFallbackDiagnostic(
            CanonicalAudioUploadCutoverCandidate candidate,
            string? syncRunID, CanonicalAudioUploadNodeRole nodeRole, string reason) =>
            new CanonicalAudioUploadDiagnostic(
                CanonicalAudioUploadDiagnosticKind.canonicalAudioUploadRuntimeLegacyFallbackUsed,
                syncRunID: syncRunID, trigger: candidate.Trigger, nodeRole: nodeRole,
                objectID: candidate.ObjectID, peerState: candidate.PeerTruth.State,
                ledgerPhase: candidate.LedgerTruth.Phase, action: candidate.ActionKind,
                result: "legacyFallback", reason: reason, hashPrefix: candidate.HashPrefix);

        private List<CanonicalAudioUploadDiagnostic> CandidateDiagnostics(
            CanonicalAudioUploadCutoverCandidate candidate,
            CanonicalAudioUploadDiagnosticKind kind,
            string? syncRunID = null,
            CanonicalAudioUploadNodeRole nodeRole = CanonicalAudioUploadNodeRole.iPhone,
            string? result = null,
            string? reason = null)
        {
            return new List<CanonicalAudioUploadDiagnostic>
            {
                new CanonicalAudioUploadDiagnostic(
                    kind: kind, syncRunID: syncRunID, trigger: candidate.Trigger, nodeRole: nodeRole,
                    objectID: candidate.ObjectID, peerState: candidate.PeerTruth.State,
                    ledgerPhase: candidate.LedgerTruth.Phase, action: candidate.ActionKind,
                    result: result, reason: reason, hashPrefix: candidate.HashPrefix)
            };
        }

        private static bool IsConflict(Exception error)
        {
            if (error is CanonicalAudioUploadRuntimeError.ConflictBlockedException
                or CanonicalAudioUploadRuntimeError.CompletedLedgerRejectedAsNoOpException
                or CanonicalAudioUploadRuntimeError.SessionConflictException
                or CanonicalAudioUploadRuntimeError.FinalizeProofRejectedException
                or CanonicalAudioUploadRuntimeError.FinalByteSizeMismatchException
                or CanonicalAudioUploadRuntimeError.FinalHashMismatchException)
                return true;
            var text = error.GetType().Name.ToLower() + error.Message.ToLower();
            return text.Contains("conflict") || text.Contains("mismatch") || text.Contains("different");
        }

        private static string ErrorCode(Exception error)
        {
            var raw = error.ToString();
            return CanonicalAudioUploadRuntimeRedaction.SafeText(
                CanonicalAudioUploadRuntimeRedaction.RedactLongHexRuns(raw)) ?? "uploadRuntimeError";
        }
    }

    public class CanonicalAudioUploadRuntimeOwner
    {
        public CanonicalAudioUploadRuntimeExecutor Executor { get; }
        public CanonicalAudioUploadJobStore JobStore { get; }
        public CanonicalAudioUploadNodeRole NodeRole { get; }

        public CanonicalAudioUploadRuntimeOwner(
            CanonicalAudioUploadRuntimeExecutor? executor = null,
            CanonicalAudioUploadJobStore? jobStore = null,
            CanonicalAudioUploadNodeRole nodeRole = CanonicalAudioUploadNodeRole.iPhone)
        {
            Executor = executor ?? new CanonicalAudioUploadRuntimeExecutor();
            JobStore = jobStore ?? new CanonicalAudioUploadJobStore();
            NodeRole = nodeRole;
        }

        public async Task<CanonicalAudioUploadRuntimeResult> ExecuteAsync(
            CanonicalAudioUploadCutoverCandidate candidate,
            ICanonicalAudioUploadByteSource source,
            ICanonicalProductionUploadPort uploadPort,
            CanonicalAudioUploadRuntimeConfiguration configuration,
            string? syncRunID = null)
        {
            return await Executor.ExecuteAsync(
                candidate, source, uploadPort, JobStore, configuration, syncRunID, NodeRole);
        }
    }

    public static class CanonicalAudioUploadRuntimeRedaction
    {
        public static string? HashPrefix(string? value)
        {
            if (value == null) return null;
            var trimmed = value.Trim();
            if (trimmed.Length == 0) return null;
            return trimmed.Length > 12 ? trimmed[..12] : trimmed;
        }

        public static string SafeIdentifier(string? value, string fallback)
        {
            var allowed = new System.Collections.Generic.HashSet<char>(
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_:.");
            var trimmed = value?.Trim() ?? "";
            var filtered = new string(trimmed.Select(c => allowed.Contains(c) ? c : '-').ToArray());
            return string.IsNullOrEmpty(filtered)
                ? fallback
                : (filtered.Length > 96 ? filtered[..96] : filtered);
        }

        public static string? SafeText(string? value)
        {
            if (value == null) return null;
            var sanitized = value
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();
            if (sanitized.Length == 0) return null;
            return sanitized.Length > 160 ? sanitized[..160] : sanitized;
        }

        public static string RedactLongHexRuns(string value)
        {
            var result = new System.Text.StringBuilder();
            int i = 0;
            while (i < value.Length)
            {
                if (!IsASCIIHexDigit(value[i]))
                {
                    result.Append(value[i]);
                    i++;
                    continue;
                }
                int start = i;
                while (i < value.Length && IsASCIIHexDigit(value[i])) i++;
                int run = i - start;
                if (run >= 32)
                {
                    result.Append(value.AsSpan(start, Math.Min(12, run)));
                    result.Append("...redacted");
                }
                else
                {
                    result.Append(value.AsSpan(start, run));
                }
            }
            return result.ToString();
        }

        private static bool IsASCIIHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }
    }

    internal static class StringExtensions
    {
        public static string? NilIfEmpty(this string? value) =>
            string.IsNullOrEmpty(value) ? null : value;
    }

    internal static class UnicodeScalarExtensions
    {
        public static bool IsASCIIHexDigit(this char c)
        {
            return (c >= 48 && c <= 57) || (c >= 65 && c <= 70) || (c >= 97 && c <= 102);
        }
    }
}
