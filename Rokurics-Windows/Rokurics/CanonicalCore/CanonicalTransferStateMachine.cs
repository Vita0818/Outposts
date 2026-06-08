using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

public readonly struct CanonicalTransferJobID : IEquatable<CanonicalTransferJobID>
{
    public string RawValue { get; }

    public CanonicalTransferJobID(string rawValue)
    {
        RawValue = rawValue.Trim().NilIfEmpty() ?? "transfer:unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalTransferJobID other && Equals(other);
    public bool Equals(CanonicalTransferJobID other) => RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();
    public override string ToString() => RawValue;
    public static bool operator ==(CanonicalTransferJobID left, CanonicalTransferJobID right) => left.Equals(right);
    public static bool operator !=(CanonicalTransferJobID left, CanonicalTransferJobID right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTransferKind
{
    recordingAudioUpload,
    generatedArtifactDownload,
    metadataSend,
    metadataApply,
    folderMetadataSend,
    folderMetadataApply,
    studyItemMetadataSend,
    studyItemMetadataApply,
    tombstoneSend,
    tombstoneApply
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTransferDirection
{
    localToPeer,
    peerToLocal,
    localOnly,
    peerOnly
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTransferPhase
{
    none,
    planned,
    queued,
    inFlight,
    finalizing,
    completed,
    failedRetryable,
    failedFatal,
    conflict,
    deferred,
    unsupported
}

public sealed class CanonicalTransferFailure : IEquatable<CanonicalTransferFailure>
{
    public string Code { get; }
    public bool Retryable { get; }
    public string? Detail { get; }

    public CanonicalTransferFailure(
        string code,
        bool retryable,
        string? detail = null)
    {
        Code = code.Trim().NilIfEmpty() ?? "unknown";
        Retryable = retryable;
        Detail = detail?.Trim().NilIfEmpty();
    }

    public override bool Equals(object? obj) => obj is CanonicalTransferFailure other && Equals(other);
    public bool Equals(CanonicalTransferFailure? other) =>
        other is not null &&
        Code == other.Code &&
        Retryable == other.Retryable &&
        Detail == other.Detail;
    public override int GetHashCode() => System.HashCode.Combine(Code, Retryable, Detail);
    public static bool operator ==(CanonicalTransferFailure left, CanonicalTransferFailure right) => left.Equals(right);
    public static bool operator !=(CanonicalTransferFailure left, CanonicalTransferFailure right) => !left.Equals(right);
}

public sealed class CanonicalRetryPolicySnapshot : IEquatable<CanonicalRetryPolicySnapshot>
{
    public int RetryCount { get; }
    public CanonicalTimestamp? NextRetryAt { get; }
    public int? MaxAttempts { get; }

    public CanonicalRetryPolicySnapshot(
        int retryCount,
        CanonicalTimestamp? nextRetryAt = null,
        int? maxAttempts = null)
    {
        RetryCount = retryCount;
        NextRetryAt = nextRetryAt;
        MaxAttempts = maxAttempts;
    }

    public override bool Equals(object? obj) => obj is CanonicalRetryPolicySnapshot other && Equals(other);
    public bool Equals(CanonicalRetryPolicySnapshot? other) =>
        other is not null &&
        RetryCount == other.RetryCount &&
        Nullable.Equals(NextRetryAt, other.NextRetryAt) &&
        MaxAttempts == other.MaxAttempts;
    public override int GetHashCode() => System.HashCode.Combine(RetryCount, NextRetryAt, MaxAttempts);
    public static bool operator ==(CanonicalRetryPolicySnapshot left, CanonicalRetryPolicySnapshot right) => left.Equals(right);
    public static bool operator !=(CanonicalRetryPolicySnapshot left, CanonicalRetryPolicySnapshot right) => !left.Equals(right);
}

public sealed class CanonicalTransferJob : IEquatable<CanonicalTransferJob>
{
    public string Id => JobID.RawValue;

    public CanonicalTransferJobID JobID { get; }
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalTransferKind Kind { get; }
    public CanonicalTransferDirection Direction { get; }
    public CanonicalTransferPhase Phase { get; }
    public CanonicalTransferFailure? Failure { get; }
    public CanonicalRetryPolicySnapshot? RetryPolicy { get; }
    public string? Source { get; }

    [JsonConstructor]
    public CanonicalTransferJob(
        CanonicalTransferJobID jobID,
        string objectID,
        string? artifactID = null,
        CanonicalTransferKind kind = default,
        CanonicalTransferDirection direction = default,
        CanonicalTransferPhase phase = CanonicalTransferPhase.none,
        CanonicalTransferFailure? failure = null,
        CanonicalRetryPolicySnapshot? retryPolicy = null,
        string? source = null)
    {
        JobID = jobID;
        ObjectID = objectID.Trim().NilIfEmpty() ?? "unknown";
        ArtifactID = artifactID?.Trim().NilIfEmpty();
        Kind = kind;
        Direction = direction;
        Phase = phase;
        Failure = failure;
        RetryPolicy = retryPolicy;
        Source = source?.Trim().NilIfEmpty();
    }

    public override bool Equals(object? obj) => obj is CanonicalTransferJob other && Equals(other);
    public bool Equals(CanonicalTransferJob? other) =>
        other is not null && JobID.Equals(other.JobID);
    public override int GetHashCode() => JobID.GetHashCode();
    public static bool operator ==(CanonicalTransferJob left, CanonicalTransferJob right) => left.Equals(right);
    public static bool operator !=(CanonicalTransferJob left, CanonicalTransferJob right) => !left.Equals(right);
}

public sealed class CanonicalLedgerProjection : IEquatable<CanonicalLedgerProjection>
{
    public string Source { get; }
    public string State { get; }
    public CanonicalTransferPhase Phase { get; }
    public CanonicalTimestamp? NextRetryAt { get; }
    public CanonicalTransferFailure? Failure { get; }

    public CanonicalLedgerProjection(
        string source,
        string state,
        CanonicalTransferPhase phase,
        CanonicalTimestamp? nextRetryAt = null,
        CanonicalTransferFailure? failure = null)
    {
        Source = source;
        State = state;
        Phase = phase;
        NextRetryAt = nextRetryAt;
        Failure = failure;
    }

    public override bool Equals(object? obj) => obj is CanonicalLedgerProjection other && Equals(other);
    public bool Equals(CanonicalLedgerProjection? other) =>
        other is not null &&
        Source == other.Source &&
        State == other.State &&
        Phase == other.Phase &&
        Nullable.Equals(NextRetryAt, other.NextRetryAt) &&
        Equals(Failure, other.Failure);
    public override int GetHashCode() => System.HashCode.Combine(Source, State, Phase, NextRetryAt, Failure);
    public static bool operator ==(CanonicalLedgerProjection left, CanonicalLedgerProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalLedgerProjection left, CanonicalLedgerProjection right) => !left.Equals(right);
}

public sealed class CanonicalTransferProjection : IEquatable<CanonicalTransferProjection>
{
    public CanonicalTransferJob[] Jobs { get; }
    public CanonicalLedgerProjection[] Ledgers { get; }

    [JsonConstructor]
    public CanonicalTransferProjection(
        CanonicalTransferJob[]? jobs = null,
        CanonicalLedgerProjection[]? ledgers = null)
    {
        Jobs = (jobs ?? System.Array.Empty<CanonicalTransferJob>())
            .OrderBy(j => j.JobID.RawValue, System.StringComparer.Ordinal)
            .ToArray();
        Ledgers = ledgers ?? System.Array.Empty<CanonicalLedgerProjection>();
    }

    public override bool Equals(object? obj) => obj is CanonicalTransferProjection other && Equals(other);
    public bool Equals(CanonicalTransferProjection? other) =>
        other is not null &&
        Jobs.SequenceEqual(other.Jobs) &&
        Ledgers.SequenceEqual(other.Ledgers);
    public override int GetHashCode() => System.HashCode.Combine(Jobs.Length, Ledgers.Length);
    public static bool operator ==(CanonicalTransferProjection left, CanonicalTransferProjection right) => left.Equals(right);
    public static bool operator !=(CanonicalTransferProjection left, CanonicalTransferProjection right) => !left.Equals(right);
}

public static class CanonicalTransferStateMachine
{
    public static CanonicalTransferPhase PhaseFromLegacyState(string? state)
    {
        switch (state?.Trim())
        {
            case "planned":
                return CanonicalTransferPhase.planned;
            case "queued":
            case "pending":
                return CanonicalTransferPhase.queued;
            case "inFlight":
            case "uploading":
            case "downloading":
            case "transferring":
            case "resuming":
                return CanonicalTransferPhase.inFlight;
            case "finalizing":
            case "verifying":
                return CanonicalTransferPhase.finalizing;
            case "completed":
            case "complete":
            case "uploaded":
                return CanonicalTransferPhase.completed;
            case "retryPending":
            case "retryableFailed":
                return CanonicalTransferPhase.failedRetryable;
            case "fatalFailed":
                return CanonicalTransferPhase.failedFatal;
            case "failed":
                return CanonicalTransferPhase.failedFatal;
            case "conflict":
                return CanonicalTransferPhase.conflict;
            case "deferred":
                return CanonicalTransferPhase.deferred;
            case "unsupported":
                return CanonicalTransferPhase.unsupported;
            default:
                return CanonicalTransferPhase.none;
        }
    }

    public static CanonicalTransferJob Job(
        string objectID,
        string? artifactID,
        CanonicalTransferKind kind,
        CanonicalTransferDirection direction,
        string? legacyState,
        DateTime? nextRetryAt = null,
        string? failureCode = null,
        string source = "")
    {
        var phase = PhaseFromLegacyState(legacyState);
        CanonicalRetryPolicySnapshot? retry = null;
        if (phase == CanonicalTransferPhase.failedRetryable)
        {
            retry = new CanonicalRetryPolicySnapshot(
                retryCount: 0,
                nextRetryAt: nextRetryAt.HasValue ? new CanonicalTimestamp(nextRetryAt.Value) : null,
                maxAttempts: null
            );
        }

        CanonicalTransferFailure? failure = null;
        if (failureCode != null)
        {
            failure = new CanonicalTransferFailure(
                code: failureCode,
                retryable: phase == CanonicalTransferPhase.failedRetryable
            );
        }

        return new CanonicalTransferJob(
            jobID: new CanonicalTransferJobID(
                string.Join("|", source, kind.ToString(), objectID, artifactID ?? "")
            ),
            objectID: objectID,
            artifactID: artifactID,
            kind: kind,
            direction: direction,
            phase: phase,
            failure: failure,
            retryPolicy: retry,
            source: source
        );
    }

    public static CanonicalTransferProjection Projection(CanonicalTransferJob[] jobs) =>
        new(jobs: jobs);
}
