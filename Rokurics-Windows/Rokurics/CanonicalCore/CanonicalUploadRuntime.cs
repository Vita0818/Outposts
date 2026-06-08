using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── CanonicalUploadSessionID ────────────────────────────────────────────────

public readonly struct CanonicalUploadSessionID : IEquatable<CanonicalUploadSessionID>
{
    public string RawValue { get; }

    public CanonicalUploadSessionID(string rawValue)
    {
        RawValue = rawValue.Trim().NilIfEmpty() ?? "upload-session:unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadSessionID other && Equals(other);
    public bool Equals(CanonicalUploadSessionID other) => RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();
    public override string ToString() => RawValue;
    public static bool operator ==(CanonicalUploadSessionID left, CanonicalUploadSessionID right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadSessionID left, CanonicalUploadSessionID right) => !left.Equals(right);
}

// ─── CanonicalUploadDisposition ──────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalUploadDisposition
{
    acceptedNew,
    acceptedExisting,
    resumed,
    finalized
}

// ─── CanonicalUploadSessionPhase ─────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalUploadSessionPhase
{
    active,
    retryPending,
    finalizing,
    completed,
    conflict,
    failed
}

// ─── CanonicalUploadRetryPolicy ──────────────────────────────────────────────

public sealed class CanonicalUploadRetryPolicy : IEquatable<CanonicalUploadRetryPolicy>
{
    public int MaxAttempts { get; }
    public TimeSpan RetryDelay { get; }

    public CanonicalUploadRetryPolicy(int maxAttempts = 3, TimeSpan? retryDelay = null)
    {
        MaxAttempts = Math.Max(1, maxAttempts);
        RetryDelay = TimeSpan.FromSeconds(Math.Max(0, (retryDelay ?? TimeSpan.FromSeconds(5)).TotalSeconds));
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadRetryPolicy other && Equals(other);
    public bool Equals(CanonicalUploadRetryPolicy? other) =>
        other is not null &&
        MaxAttempts == other.MaxAttempts &&
        RetryDelay == other.RetryDelay;
    public override int GetHashCode() => HashCode.Combine(MaxAttempts, RetryDelay);
    public static bool operator ==(CanonicalUploadRetryPolicy left, CanonicalUploadRetryPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadRetryPolicy left, CanonicalUploadRetryPolicy right) => !left.Equals(right);
}

// ─── CanonicalUploadChunkRecord ──────────────────────────────────────────────

public sealed class CanonicalUploadChunkRecord : IEquatable<CanonicalUploadChunkRecord>
{
    public long Offset { get; }
    public int Length { get; }
    public CanonicalHash Sha256 { get; }

    public CanonicalUploadChunkRecord(long offset, int length, CanonicalHash sha256)
    {
        Offset = offset;
        Length = length;
        Sha256 = sha256;
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadChunkRecord other && Equals(other);
    public bool Equals(CanonicalUploadChunkRecord? other) =>
        other is not null &&
        Offset == other.Offset &&
        Length == other.Length &&
        Sha256.Equals(other.Sha256);
    public override int GetHashCode() => HashCode.Combine(Offset, Length, Sha256);
    public static bool operator ==(CanonicalUploadChunkRecord left, CanonicalUploadChunkRecord right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadChunkRecord left, CanonicalUploadChunkRecord right) => !left.Equals(right);
}

// ─── CanonicalUploadSession ──────────────────────────────────────────────────

public sealed class CanonicalUploadSession : IEquatable<CanonicalUploadSession>
{
    public CanonicalUploadSessionID SessionID { get; set; }
    public string ObjectID { get; set; }
    public CanonicalFileReference TargetReference { get; set; }
    public long TotalBytes { get; set; }
    public CanonicalHash TotalHash { get; set; }
    public int ChunkSize { get; set; }
    public long ConfirmedBytes { get; set; }
    public List<CanonicalUploadChunkRecord> Chunks { get; set; }
    public CanonicalUploadSessionPhase Phase { get; set; }
    public int RetryCount { get; set; }
    public CanonicalTimestamp? NextRetryAt { get; set; }
    public CanonicalTimestamp CreatedAt { get; set; }
    public CanonicalTimestamp UpdatedAt { get; set; }
    public CanonicalTimestamp? FinalizedAt { get; set; }
    public string? LastError { get; set; }

    public CanonicalUploadSession(
        CanonicalUploadSessionID sessionID,
        string objectID,
        CanonicalFileReference targetReference,
        long totalBytes,
        CanonicalHash totalHash,
        int chunkSize,
        long confirmedBytes,
        List<CanonicalUploadChunkRecord> chunks,
        CanonicalUploadSessionPhase phase,
        int retryCount,
        CanonicalTimestamp? nextRetryAt,
        CanonicalTimestamp createdAt,
        CanonicalTimestamp updatedAt,
        CanonicalTimestamp? finalizedAt,
        string? lastError)
    {
        SessionID = sessionID;
        ObjectID = objectID;
        TargetReference = targetReference;
        TotalBytes = totalBytes;
        TotalHash = totalHash;
        ChunkSize = chunkSize;
        ConfirmedBytes = confirmedBytes;
        Chunks = chunks;
        Phase = phase;
        RetryCount = retryCount;
        NextRetryAt = nextRetryAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        FinalizedAt = finalizedAt;
        LastError = lastError;
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadSession other && Equals(other);
    public bool Equals(CanonicalUploadSession? other) =>
        other is not null && SessionID.Equals(other.SessionID);
    public override int GetHashCode() => SessionID.GetHashCode();
    public static bool operator ==(CanonicalUploadSession left, CanonicalUploadSession right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadSession left, CanonicalUploadSession right) => !left.Equals(right);
}

// ─── CanonicalUploadStartRequest ─────────────────────────────────────────────

public sealed class CanonicalUploadStartRequest : IEquatable<CanonicalUploadStartRequest>
{
    public string ObjectID { get; }
    public CanonicalFileReference TargetReference { get; }
    public long TotalBytes { get; }
    public CanonicalHash TotalHash { get; }
    public int ChunkSize { get; }
    public string? IdempotencyKey { get; }

    public CanonicalUploadStartRequest(
        string objectID,
        CanonicalFileReference targetReference,
        long totalBytes,
        CanonicalHash totalHash,
        int chunkSize,
        string? idempotencyKey = null)
    {
        ObjectID = objectID.Trim().NilIfEmpty() ?? "unknown-recording";
        TargetReference = targetReference;
        TotalBytes = totalBytes;
        TotalHash = totalHash;
        ChunkSize = chunkSize;
        IdempotencyKey = idempotencyKey?.Trim().NilIfEmpty();
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadStartRequest other && Equals(other);
    public bool Equals(CanonicalUploadStartRequest? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        TargetReference.Equals(other.TargetReference) &&
        TotalBytes == other.TotalBytes &&
        TotalHash.Equals(other.TotalHash) &&
        ChunkSize == other.ChunkSize &&
        IdempotencyKey == other.IdempotencyKey;
    public override int GetHashCode() => HashCode.Combine(ObjectID, TargetReference, TotalBytes, TotalHash, ChunkSize);
    public static bool operator ==(CanonicalUploadStartRequest left, CanonicalUploadStartRequest right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadStartRequest left, CanonicalUploadStartRequest right) => !left.Equals(right);
}

// ─── CanonicalUploadStatusRequest ────────────────────────────────────────────

public sealed class CanonicalUploadStatusRequest : IEquatable<CanonicalUploadStatusRequest>
{
    public string ObjectID { get; }
    public CanonicalUploadSessionID SessionID { get; }
    public CanonicalHash TotalHash { get; }

    public CanonicalUploadStatusRequest(string objectID, CanonicalUploadSessionID sessionID, CanonicalHash totalHash)
    {
        ObjectID = objectID;
        SessionID = sessionID;
        TotalHash = totalHash;
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadStatusRequest other && Equals(other);
    public bool Equals(CanonicalUploadStatusRequest? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        SessionID.Equals(other.SessionID) &&
        TotalHash.Equals(other.TotalHash);
    public override int GetHashCode() => HashCode.Combine(ObjectID, SessionID, TotalHash);
    public static bool operator ==(CanonicalUploadStatusRequest left, CanonicalUploadStatusRequest right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadStatusRequest left, CanonicalUploadStatusRequest right) => !left.Equals(right);
}

// ─── CanonicalUploadChunk ────────────────────────────────────────────────────

public sealed class CanonicalUploadChunk : IEquatable<CanonicalUploadChunk>
{
    public string ObjectID { get; }
    public CanonicalUploadSessionID SessionID { get; }
    public long Offset { get; }
    public byte[] Bytes { get; }
    public CanonicalHash ChunkHash { get; }
    public CanonicalHash TotalHash { get; }
    public string? IdempotencyKey { get; }

    public CanonicalUploadChunk(
        string objectID,
        CanonicalUploadSessionID sessionID,
        long offset,
        byte[] bytes,
        CanonicalHash chunkHash,
        CanonicalHash totalHash,
        string? idempotencyKey = null)
    {
        ObjectID = objectID.Trim().NilIfEmpty() ?? "unknown-recording";
        SessionID = sessionID;
        Offset = offset;
        Bytes = bytes;
        ChunkHash = chunkHash;
        TotalHash = totalHash;
        IdempotencyKey = idempotencyKey?.Trim().NilIfEmpty();
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadChunk other && Equals(other);
    public bool Equals(CanonicalUploadChunk? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        SessionID.Equals(other.SessionID) &&
        Offset == other.Offset &&
        Bytes.SequenceEqual(other.Bytes) &&
        ChunkHash.Equals(other.ChunkHash) &&
        TotalHash.Equals(other.TotalHash) &&
        IdempotencyKey == other.IdempotencyKey;
    public override int GetHashCode() => HashCode.Combine(ObjectID, SessionID, Offset, ChunkHash, TotalHash);
    public static bool operator ==(CanonicalUploadChunk left, CanonicalUploadChunk right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadChunk left, CanonicalUploadChunk right) => !left.Equals(right);
}

// ─── CanonicalUploadFinalizeRequest ──────────────────────────────────────────

public sealed class CanonicalUploadFinalizeRequest : IEquatable<CanonicalUploadFinalizeRequest>
{
    public string ObjectID { get; }
    public CanonicalUploadSessionID SessionID { get; }
    public long TotalBytes { get; }
    public CanonicalHash TotalHash { get; }

    public CanonicalUploadFinalizeRequest(string objectID, CanonicalUploadSessionID sessionID, long totalBytes, CanonicalHash totalHash)
    {
        ObjectID = objectID;
        SessionID = sessionID;
        TotalBytes = totalBytes;
        TotalHash = totalHash;
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadFinalizeRequest other && Equals(other);
    public bool Equals(CanonicalUploadFinalizeRequest? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        SessionID.Equals(other.SessionID) &&
        TotalBytes == other.TotalBytes &&
        TotalHash.Equals(other.TotalHash);
    public override int GetHashCode() => HashCode.Combine(ObjectID, SessionID, TotalBytes, TotalHash);
    public static bool operator ==(CanonicalUploadFinalizeRequest left, CanonicalUploadFinalizeRequest right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadFinalizeRequest left, CanonicalUploadFinalizeRequest right) => !left.Equals(right);
}

// ─── CanonicalUploadSessionStatus ────────────────────────────────────────────

public sealed class CanonicalUploadSessionStatus : IEquatable<CanonicalUploadSessionStatus>
{
    public bool Ok { get; }
    public CanonicalUploadDisposition? Disposition { get; }
    public CanonicalUploadSessionPhase Phase { get; }
    public CanonicalUploadSessionID? SessionID { get; }
    public long ConfirmedBytes { get; }
    public long NextOffset { get; }
    public int? ChunkSize { get; }
    public bool Completed { get; }
    public CanonicalFileReference? FinalFile { get; }
    public CanonicalHash? Checksum { get; }
    public long? FileSize { get; }
    public CanonicalRetryPolicySnapshot? Retry { get; }
    public string? Error { get; }

    public CanonicalUploadSessionStatus(
        bool ok,
        CanonicalUploadDisposition? disposition,
        CanonicalUploadSessionPhase phase,
        CanonicalUploadSessionID? sessionID,
        long confirmedBytes,
        long nextOffset,
        int? chunkSize,
        bool completed,
        CanonicalFileReference? finalFile,
        CanonicalHash? checksum,
        long? fileSize,
        CanonicalRetryPolicySnapshot? retry,
        string? error)
    {
        Ok = ok;
        Disposition = disposition;
        Phase = phase;
        SessionID = sessionID;
        ConfirmedBytes = confirmedBytes;
        NextOffset = nextOffset;
        ChunkSize = chunkSize;
        Completed = completed;
        FinalFile = finalFile;
        Checksum = checksum;
        FileSize = fileSize;
        Retry = retry;
        Error = error;
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadSessionStatus other && Equals(other);
    public bool Equals(CanonicalUploadSessionStatus? other) =>
        other is not null &&
        Ok == other.Ok &&
        Disposition == other.Disposition &&
        Phase == other.Phase &&
        Nullable.Equals(SessionID, other.SessionID) &&
        ConfirmedBytes == other.ConfirmedBytes &&
        NextOffset == other.NextOffset &&
        ChunkSize == other.ChunkSize &&
        Completed == other.Completed &&
        Nullable.Equals(FinalFile, other.FinalFile) &&
        Nullable.Equals(Checksum, other.Checksum) &&
        FileSize == other.FileSize &&
        Nullable.Equals(Retry, other.Retry) &&
        Error == other.Error;
    public override int GetHashCode() => HashCode.Combine(Ok, Phase, SessionID, ConfirmedBytes, Completed);
    public static bool operator ==(CanonicalUploadSessionStatus left, CanonicalUploadSessionStatus right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadSessionStatus left, CanonicalUploadSessionStatus right) => !left.Equals(right);
}

// ─── CanonicalUploadResumeState ──────────────────────────────────────────────

public sealed class CanonicalUploadResumeState : IEquatable<CanonicalUploadResumeState>
{
    public CanonicalUploadSessionID SessionID { get; }
    public long ConfirmedBytes { get; }
    public long NextOffset { get; }
    public long TotalBytes { get; }
    public CanonicalHash TotalHash { get; }
    public int ChunkSize { get; }
    public CanonicalUploadSessionPhase Phase { get; }

    public CanonicalUploadResumeState(
        CanonicalUploadSessionID sessionID,
        long confirmedBytes,
        long nextOffset,
        long totalBytes,
        CanonicalHash totalHash,
        int chunkSize,
        CanonicalUploadSessionPhase phase)
    {
        SessionID = sessionID;
        ConfirmedBytes = confirmedBytes;
        NextOffset = nextOffset;
        TotalBytes = totalBytes;
        TotalHash = totalHash;
        ChunkSize = chunkSize;
        Phase = phase;
    }

    public override bool Equals(object? obj) => obj is CanonicalUploadResumeState other && Equals(other);
    public bool Equals(CanonicalUploadResumeState? other) =>
        other is not null &&
        SessionID.Equals(other.SessionID) &&
        ConfirmedBytes == other.ConfirmedBytes &&
        NextOffset == other.NextOffset &&
        TotalBytes == other.TotalBytes &&
        TotalHash.Equals(other.TotalHash) &&
        ChunkSize == other.ChunkSize &&
        Phase == other.Phase;
    public override int GetHashCode() => HashCode.Combine(SessionID, ConfirmedBytes, NextOffset, TotalBytes, TotalHash, ChunkSize, Phase);
    public static bool operator ==(CanonicalUploadResumeState left, CanonicalUploadResumeState right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadResumeState left, CanonicalUploadResumeState right) => !left.Equals(right);
}

// ─── CanonicalUploadRuntimeError ─────────────────────────────────────────────

public sealed class CanonicalUploadRuntimeError : Exception, IEquatable<CanonicalUploadRuntimeError>
{
    public string ErrorKind { get; }
    public string? Detail { get; }
    public string? Expected { get; }
    public string? Actual { get; }
    public long? ExpectedOffset { get; }
    public long? ActualOffset { get; }
    public long? ConfirmedBytes { get; }
    public long? TotalBytes { get; }

    private CanonicalUploadRuntimeError(
        string errorKind,
        string? detail = null,
        string? expected = null,
        string? actual = null,
        long? expectedOffset = null,
        long? actualOffset = null,
        long? confirmedBytes = null,
        long? totalBytes = null)
        : base(detail ?? errorKind)
    {
        ErrorKind = errorKind;
        Detail = detail;
        Expected = expected;
        Actual = actual;
        ExpectedOffset = expectedOffset;
        ActualOffset = actualOffset;
        ConfirmedBytes = confirmedBytes;
        TotalBytes = totalBytes;
    }

    public static CanonicalUploadRuntimeError InvalidRequest(string objectID) =>
        new("invalidRequest", detail: objectID);

    public static CanonicalUploadRuntimeError InvalidSession(string sessionID) =>
        new("invalidSession", detail: sessionID);

    public static CanonicalUploadRuntimeError SessionMissing(string sessionID) =>
        new("sessionMissing", detail: sessionID);

    public static CanonicalUploadRuntimeError SessionConflict(string sessionID) =>
        new("sessionConflict", detail: sessionID);

    public static CanonicalUploadRuntimeError ChunkOffsetMismatch(long expected, long actual) =>
        new("chunkOffsetMismatch", expectedOffset: expected, actualOffset: actual);

    public static CanonicalUploadRuntimeError ChunkHashMismatch(string expected, string actual) =>
        new("chunkHashMismatch", expected: expected, actual: actual);

    public static CanonicalUploadRuntimeError SessionIncomplete(long confirmedBytes, long totalBytes) =>
        new("sessionIncomplete", confirmedBytes: confirmedBytes, totalBytes: totalBytes);

    public static CanonicalUploadRuntimeError FinalHashMismatch(string expected, string actual) =>
        new("finalHashMismatch", expected: expected, actual: actual);

    public static CanonicalUploadRuntimeError TargetConflict(string objectID) =>
        new("targetConflict", detail: objectID);

    public static CanonicalUploadRuntimeError RetryLimitExceeded(string sessionID) =>
        new("retryLimitExceeded", detail: sessionID);

    public override bool Equals(object? obj) => obj is CanonicalUploadRuntimeError other && Equals(other);
    public bool Equals(CanonicalUploadRuntimeError? other) =>
        other is not null &&
        ErrorKind == other.ErrorKind &&
        Detail == other.Detail &&
        Expected == other.Expected &&
        Actual == other.Actual &&
        ExpectedOffset == other.ExpectedOffset &&
        ActualOffset == other.ActualOffset &&
        ConfirmedBytes == other.ConfirmedBytes &&
        TotalBytes == other.TotalBytes;
    public override int GetHashCode() => HashCode.Combine(ErrorKind, Detail, Expected, Actual, ExpectedOffset, ActualOffset, ConfirmedBytes, TotalBytes);
    public static bool operator ==(CanonicalUploadRuntimeError left, CanonicalUploadRuntimeError right) => left.Equals(right);
    public static bool operator !=(CanonicalUploadRuntimeError left, CanonicalUploadRuntimeError right) => !left.Equals(right);
}

// ─── ICanonicalUploadRuntimePort (protocol → interface) ──────────────────────

public interface ICanonicalUploadRuntimePort
{
    Task<CanonicalUploadSessionStatus> StartAsync(CanonicalUploadStartRequest request, DateTime? now = null);
    Task<CanonicalUploadSessionStatus> StatusAsync(CanonicalUploadStatusRequest request, DateTime? now = null);
    Task<CanonicalUploadSessionStatus> AppendAsync(CanonicalUploadChunk chunk, DateTime? now = null);
    Task<CanonicalUploadSessionStatus> FinalizeAsync(CanonicalUploadFinalizeRequest request, DateTime? now = null);
}

// ─── CanonicalResumableUploadRuntime ─────────────────────────────────────────

public sealed class CanonicalResumableUploadRuntime : ICanonicalUploadRuntimePort
{
    private sealed class SessionState
    {
        public CanonicalUploadSession Session { get; set; }
        public List<byte> Buffer { get; set; }
        public Dictionary<string, CanonicalUploadSessionStatus> IdempotencyResponses { get; set; }

        public SessionState(
            CanonicalUploadSession session,
            List<byte> buffer,
            Dictionary<string, CanonicalUploadSessionStatus>? idempotencyResponses = null)
        {
            Session = session;
            Buffer = buffer;
            IdempotencyResponses = idempotencyResponses ?? new Dictionary<string, CanonicalUploadSessionStatus>();
        }
    }

    private readonly ICanonicalFileStorePort _fileStore;
    private readonly CanonicalUploadRetryPolicy _retryPolicy;
    private readonly Dictionary<CanonicalUploadSessionID, SessionState> _sessions = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CanonicalResumableUploadRuntime(
        ICanonicalFileStorePort fileStore,
        CanonicalUploadRetryPolicy? retryPolicy = null)
    {
        _fileStore = fileStore;
        _retryPolicy = retryPolicy ?? new CanonicalUploadRetryPolicy();
    }

    public async Task<CanonicalUploadSessionStatus> StartAsync(CanonicalUploadStartRequest request, DateTime? now = null)
    {
        var currentTime = now ?? DateTime.UtcNow;
        ValidateStart(request);

        var completed = await CompletedStatusIfPresentAsync(request);
        if (completed != null)
            return completed;

        var sessionID = MakeSessionID(request);

        await _semaphore.WaitAsync();
        try
        {
            if (_sessions.TryGetValue(sessionID, out var existing))
            {
                if (existing.Session.ObjectID != request.ObjectID ||
                    !SameHash(existing.Session.TotalHash, request.TotalHash) ||
                    existing.Session.TotalBytes != request.TotalBytes ||
                    existing.Session.ChunkSize != request.ChunkSize)
                    throw CanonicalUploadRuntimeError.SessionConflict(sessionID.RawValue);

                return StatusFor(existing.Session, CanonicalUploadDisposition.acceptedExisting);
            }

            var session = new CanonicalUploadSession(
                sessionID: sessionID,
                objectID: request.ObjectID,
                targetReference: request.TargetReference,
                totalBytes: request.TotalBytes,
                totalHash: request.TotalHash,
                chunkSize: request.ChunkSize,
                confirmedBytes: 0,
                chunks: new List<CanonicalUploadChunkRecord>(),
                phase: CanonicalUploadSessionPhase.active,
                retryCount: 0,
                nextRetryAt: null,
                createdAt: new CanonicalTimestamp(currentTime),
                updatedAt: new CanonicalTimestamp(currentTime),
                finalizedAt: null,
                lastError: null
            );

            _sessions[sessionID] = new SessionState(session, new List<byte>());
            return StatusFor(session, CanonicalUploadDisposition.acceptedNew);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalUploadSessionStatus> StatusAsync(CanonicalUploadStatusRequest request, DateTime? now = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(request.SessionID, out var state))
                throw CanonicalUploadRuntimeError.SessionMissing(request.SessionID.RawValue);

            if (state.Session.ObjectID != request.ObjectID ||
                !SameHash(state.Session.TotalHash, request.TotalHash))
                throw CanonicalUploadRuntimeError.SessionConflict(request.SessionID.RawValue);

            return StatusFor(state.Session, CanonicalUploadDisposition.resumed);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalUploadSessionStatus> AppendAsync(CanonicalUploadChunk chunk, DateTime? now = null)
    {
        var currentTime = now ?? DateTime.UtcNow;

        await _semaphore.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(chunk.SessionID, out var state))
                throw CanonicalUploadRuntimeError.SessionMissing(chunk.SessionID.RawValue);

            if (chunk.IdempotencyKey != null &&
                state.IdempotencyResponses.TryGetValue(chunk.IdempotencyKey, out var cachedResponse))
                return cachedResponse;

            if (state.Session.Phase != CanonicalUploadSessionPhase.active &&
                state.Session.Phase != CanonicalUploadSessionPhase.retryPending)
                throw CanonicalUploadRuntimeError.SessionConflict(chunk.SessionID.RawValue);

            if (state.Session.ObjectID != chunk.ObjectID ||
                !SameHash(state.Session.TotalHash, chunk.TotalHash))
                throw CanonicalUploadRuntimeError.SessionConflict(chunk.SessionID.RawValue);

            var actualChunkHash = InMemoryCanonicalFileStore.Hash(chunk.Bytes, CanonicalFileHashPolicy.sha256)
                                  ?? new CanonicalHash("");
            if (!SameHash(actualChunkHash, chunk.ChunkHash))
            {
                state.Session.Phase = CanonicalUploadSessionPhase.conflict;
                state.Session.LastError = "chunkHashMismatch";
                throw CanonicalUploadRuntimeError.ChunkHashMismatch(chunk.ChunkHash.Value, actualChunkHash.Value);
            }

            var existingChunk = state.Session.Chunks.FirstOrDefault(c => c.Offset == chunk.Offset);
            if (existingChunk != null)
            {
                if (existingChunk.Length != chunk.Bytes.Length ||
                    !SameHash(existingChunk.Sha256, chunk.ChunkHash))
                {
                    state.Session.Phase = CanonicalUploadSessionPhase.conflict;
                    state.Session.LastError = "chunkConflict";
                    throw CanonicalUploadRuntimeError.SessionConflict(chunk.SessionID.RawValue);
                }

                var acceptResponse = StatusFor(state.Session, CanonicalUploadDisposition.acceptedExisting);
                if (chunk.IdempotencyKey != null)
                    state.IdempotencyResponses[chunk.IdempotencyKey] = acceptResponse;

                return acceptResponse;
            }

            if (chunk.Offset != state.Session.ConfirmedBytes)
                throw CanonicalUploadRuntimeError.ChunkOffsetMismatch(state.Session.ConfirmedBytes, chunk.Offset);

            if (state.Session.ConfirmedBytes + chunk.Bytes.Length > state.Session.TotalBytes)
                throw CanonicalUploadRuntimeError.SessionConflict(chunk.SessionID.RawValue);

            state.Buffer.AddRange(chunk.Bytes);
            state.Session.ConfirmedBytes += chunk.Bytes.Length;
            state.Session.Chunks.Add(new CanonicalUploadChunkRecord(chunk.Offset, chunk.Bytes.Length, chunk.ChunkHash));
            state.Session.Phase = CanonicalUploadSessionPhase.active;
            state.Session.UpdatedAt = new CanonicalTimestamp(currentTime);
            state.Session.LastError = null;

            var response = StatusFor(state.Session, CanonicalUploadDisposition.acceptedNew);
            if (chunk.IdempotencyKey != null)
                state.IdempotencyResponses[chunk.IdempotencyKey] = response;

            return response;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalUploadSessionStatus> FinalizeAsync(CanonicalUploadFinalizeRequest request, DateTime? now = null)
    {
        var currentTime = now ?? DateTime.UtcNow;

        await _semaphore.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(request.SessionID, out var state))
                throw CanonicalUploadRuntimeError.SessionMissing(request.SessionID.RawValue);

            if (state.Session.ObjectID != request.ObjectID ||
                state.Session.TotalBytes != request.TotalBytes ||
                !SameHash(state.Session.TotalHash, request.TotalHash))
                throw CanonicalUploadRuntimeError.SessionConflict(request.SessionID.RawValue);

            if (state.Session.ConfirmedBytes != request.TotalBytes)
                throw CanonicalUploadRuntimeError.SessionIncomplete(state.Session.ConfirmedBytes, request.TotalBytes);

            var actualHash = InMemoryCanonicalFileStore.Hash(state.Buffer.ToArray(), CanonicalFileHashPolicy.sha256)
                             ?? new CanonicalHash("");
            if (!SameHash(actualHash, request.TotalHash))
            {
                state.Session.Phase = CanonicalUploadSessionPhase.conflict;
                state.Session.LastError = "finalHashMismatch";
                throw CanonicalUploadRuntimeError.FinalHashMismatch(request.TotalHash.Value, actualHash.Value);
            }

            state.Session.Phase = CanonicalUploadSessionPhase.finalizing;

            try
            {
                await _fileStore.WriteAsync(new CanonicalFileWriteIntent(
                    reference: state.Session.TargetReference,
                    bytes: state.Buffer.ToArray(),
                    purpose: CanonicalFilePurpose.artifactBytes,
                    expectedContentHash: request.TotalHash,
                    expectedByteSize: request.TotalBytes,
                    conflictPolicy: CanonicalFileConflictPolicy.idempotentIfSameContent
                ));
            }
            catch
            {
                state.Session.Phase = CanonicalUploadSessionPhase.conflict;
                state.Session.LastError = "targetConflict";
                throw CanonicalUploadRuntimeError.TargetConflict(request.ObjectID);
            }

            state.Session.Phase = CanonicalUploadSessionPhase.completed;
            state.Session.UpdatedAt = new CanonicalTimestamp(currentTime);
            state.Session.FinalizedAt = new CanonicalTimestamp(currentTime);
            state.Session.LastError = null;
            return StatusFor(state.Session, CanonicalUploadDisposition.finalized);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalUploadResumeState> ResumeStateAsync(CanonicalUploadSessionID sessionID)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(sessionID, out var state))
                throw CanonicalUploadRuntimeError.SessionMissing(sessionID.RawValue);

            return new CanonicalUploadResumeState(
                sessionID: state.Session.SessionID,
                confirmedBytes: state.Session.ConfirmedBytes,
                nextOffset: state.Session.ConfirmedBytes,
                totalBytes: state.Session.TotalBytes,
                totalHash: state.Session.TotalHash,
                chunkSize: state.Session.ChunkSize,
                phase: state.Session.Phase
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalUploadSessionStatus> RecordRetryableFailureAsync(
        CanonicalUploadSessionID sessionID,
        string code,
        DateTime? now = null)
    {
        var currentTime = now ?? DateTime.UtcNow;

        await _semaphore.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(sessionID, out var state))
                throw CanonicalUploadRuntimeError.SessionMissing(sessionID.RawValue);

            var nextRetryCount = state.Session.RetryCount + 1;
            if (nextRetryCount > _retryPolicy.MaxAttempts)
            {
                state.Session.Phase = CanonicalUploadSessionPhase.failed;
                state.Session.LastError = code;
                throw CanonicalUploadRuntimeError.RetryLimitExceeded(sessionID.RawValue);
            }

            state.Session.RetryCount = nextRetryCount;
            state.Session.Phase = CanonicalUploadSessionPhase.retryPending;
            state.Session.LastError = code.Trim().NilIfEmpty() ?? "retryableFailure";
            state.Session.NextRetryAt = new CanonicalTimestamp(currentTime.Add(_retryPolicy.RetryDelay));
            state.Session.UpdatedAt = new CanonicalTimestamp(currentTime);

            return StatusFor(state.Session, CanonicalUploadDisposition.resumed);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalRetryPolicySnapshot> RetryDriveSnapshotAsync(CanonicalUploadSessionID sessionID)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(sessionID, out var state))
                throw CanonicalUploadRuntimeError.SessionMissing(sessionID.RawValue);

            return new CanonicalRetryPolicySnapshot(
                retryCount: state.Session.RetryCount,
                nextRetryAt: state.Session.NextRetryAt,
                maxAttempts: _retryPolicy.MaxAttempts
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<CanonicalUploadSessionStatus?> CompletedStatusIfPresentAsync(CanonicalUploadStartRequest request)
    {
        try
        {
            var read = await _fileStore.ReadAsync(new CanonicalFileReadRequest(request.TargetReference));
            if (read.ByteSize == request.TotalBytes &&
                read.ContentHash != null &&
                SameHash(read.ContentHash, request.TotalHash))
            {
                return new CanonicalUploadSessionStatus(
                    ok: true,
                    disposition: CanonicalUploadDisposition.acceptedExisting,
                    phase: CanonicalUploadSessionPhase.completed,
                    sessionID: null,
                    confirmedBytes: request.TotalBytes,
                    nextOffset: request.TotalBytes,
                    chunkSize: null,
                    completed: true,
                    finalFile: request.TargetReference,
                    checksum: read.ContentHash,
                    fileSize: request.TotalBytes,
                    retry: null,
                    error: null
                );
            }

            throw CanonicalUploadRuntimeError.TargetConflict(request.ObjectID);
        }
        catch (CanonicalFileRuntimeError ex) when (ex.ErrorKind == "fileNotFound")
        {
            return null;
        }
    }

    private static void ValidateStart(CanonicalUploadStartRequest request)
    {
        if (request.TotalBytes <= 0 || request.ChunkSize <= 0)
            throw CanonicalUploadRuntimeError.InvalidRequest(request.ObjectID);
    }

    private CanonicalUploadSessionStatus StatusFor(
        CanonicalUploadSession session,
        CanonicalUploadDisposition? disposition)
    {
        return new CanonicalUploadSessionStatus(
            ok: true,
            disposition: disposition,
            phase: session.Phase,
            sessionID: session.SessionID,
            confirmedBytes: session.ConfirmedBytes,
            nextOffset: session.ConfirmedBytes,
            chunkSize: session.ChunkSize,
            completed: session.Phase == CanonicalUploadSessionPhase.completed,
            finalFile: session.Phase == CanonicalUploadSessionPhase.completed ? session.TargetReference : null,
            checksum: session.Phase == CanonicalUploadSessionPhase.completed ? session.TotalHash : null,
            fileSize: session.Phase == CanonicalUploadSessionPhase.completed ? session.TotalBytes : null,
            retry: session.Phase == CanonicalUploadSessionPhase.retryPending
                ? new CanonicalRetryPolicySnapshot(
                    retryCount: session.RetryCount,
                    nextRetryAt: session.NextRetryAt,
                    maxAttempts: _retryPolicy.MaxAttempts)
                : null,
            error: session.LastError
        );
    }

    private static CanonicalUploadSessionID MakeSessionID(CanonicalUploadStartRequest request)
    {
        var raw = $"{request.ObjectID}|{request.TargetReference.RootToken.RawValue}|{request.TargetReference.LogicalPathToken}|{request.TotalHash.Value}";
        var digest = CanonicalHash.Sha256String(raw).Value;
        return new CanonicalUploadSessionID($"{request.ObjectID}-{digest[..Math.Min(digest.Length, 16)]}");
    }

    private static bool SameHash(CanonicalHash left, CanonicalHash right) =>
        left.Algorithm == right.Algorithm && left.Value == right.Value;
}
