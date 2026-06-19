using System;
using System.Collections.Generic;
using System.Linq;
namespace Rokurics.Models;

public sealed record CanonicalTransferJobID(string RawValue);

public enum CanonicalTransferKind
{
    RecordingAudioUpload,
    GeneratedArtifactDownload,
    MetadataSend,
    MetadataApply,
    FolderMetadataSend,
    FolderMetadataApply,
    StudyItemMetadataSend,
    StudyItemMetadataApply,
    TombstoneSend,
    TombstoneApply
}

public enum CanonicalTransferDirection
{
    LocalToPeer,
    PeerToLocal,
    LocalOnly,
    PeerOnly
}

public enum CanonicalTransferPhase
{
    None,
    Planned,
    Queued,
    InFlight,
    Finalizing,
    Completed,
    FailedRetryable,
    FailedFatal,
    Conflict,
    Deferred,
    Unsupported
}

public sealed record CanonicalTransferFailure(
    string Code,
    bool Retryable,
    string? Detail = null
);

public sealed record CanonicalLedgerProjection(
    string Source,
    string State,
    CanonicalTransferPhase Phase,
    CanonicalTimestamp? NextRetryAt = null,
    CanonicalTransferFailure? Failure = null
);

public sealed record CanonicalRetryPolicySnapshot(
    int RetryCount,
    CanonicalTimestamp? NextRetryAt = null,
    int? MaxAttempts = null
);

public sealed record CanonicalTransferJob(
    CanonicalTransferJobID JobID,
    string ObjectID,
    string? ArtifactID,
    CanonicalTransferKind Kind,
    CanonicalTransferDirection Direction,
    CanonicalTransferPhase Phase,
    CanonicalTransferFailure? Failure = null,
    CanonicalRetryPolicySnapshot? RetryPolicy = null,
    string? Source = null
)
{
    public string Id => JobID.RawValue;
}

public sealed record CanonicalTransferProjection(
    IReadOnlyList<CanonicalTransferJob> Jobs,
    IReadOnlyList<CanonicalLedgerProjection> Ledgers,
    CanonicalTimestamp GeneratedAt
);

public static class CanonicalTransferProjectionBuilder
{
    public static CanonicalTransferProjection Build(
        IReadOnlyList<CanonicalTransferJob> Jobs,
        IReadOnlyList<CanonicalLedgerProjection> Ledgers,
        CanonicalTimestamp? GeneratedAt = null
    )
    {
        var orderedJobs = Jobs.OrderBy(job => job.JobID.RawValue).ToList();
        return new CanonicalTransferProjection(orderedJobs, Ledgers, GeneratedAt ?? new CanonicalTimestamp());
    }
}

public static class CanonicalTransferStateMachineCompat
{
    private static string? NormalizedState(string? state)
    {
        return string.IsNullOrWhiteSpace(state) ? null : state.Trim();
    }

    public static CanonicalTransferPhase PhaseFromLegacyState(string? state)
    {
        return NormalizedState(state) switch
        {
            "planned" => CanonicalTransferPhase.Planned,
            "queued" or "pending" => CanonicalTransferPhase.Queued,
            "inFlight" or "uploading" or "downloading" or "transferring" or "resuming" => CanonicalTransferPhase.InFlight,
            "finalizing" or "verifying" => CanonicalTransferPhase.Finalizing,
            "completed" or "complete" or "uploaded" => CanonicalTransferPhase.Completed,
            "retryPending" or "retryableFailed" => CanonicalTransferPhase.FailedRetryable,
            "fatalFailed" or "failed" => CanonicalTransferPhase.FailedFatal,
            "conflict" => CanonicalTransferPhase.Conflict,
            "deferred" => CanonicalTransferPhase.Deferred,
            "unsupported" => CanonicalTransferPhase.Unsupported,
            null => CanonicalTransferPhase.None,
            _ => CanonicalTransferPhase.None
        };
    }

    public static CanonicalTransferJob Job(
        string objectID,
        string? artifactID,
        CanonicalTransferKind kind,
        CanonicalTransferDirection direction,
        string? legacyState,
        DateTime? nextRetryAt = null,
        string? failureCode = null,
        string source = "windows"
    )
    {
        var transferPhase = PhaseFromLegacyState(legacyState);
        var retryPolicy = transferPhase == CanonicalTransferPhase.FailedRetryable
            ? new CanonicalRetryPolicySnapshot(
                RetryCount: 0,
                NextRetryAt: nextRetryAt is null ? null : new CanonicalTimestamp(nextRetryAt.Value),
                MaxAttempts: null)
            : null;

        var failure = failureCode is null
            ? null
            : new CanonicalTransferFailure(failureCode, transferPhase == CanonicalTransferPhase.FailedRetryable);

        var jobID = string.Join('|',
            SafeToken(source),
            KindRawValue(kind),
            SafeToken(objectID),
            SafeToken(artifactID)
        );

        return new CanonicalTransferJob(
            JobID: new CanonicalTransferJobID(jobID),
            ObjectID: objectID,
            ArtifactID: artifactID,
            Kind: kind,
            Direction: direction,
            Phase: transferPhase,
            Failure: failure,
            RetryPolicy: retryPolicy,
            Source: source
        );
    }

    public static CanonicalTransferProjection ProjectionFrom(IEnumerable<CanonicalTransferJob> jobs)
    {
        return new CanonicalTransferProjection(
            jobs: jobs.ToList(),
            Ledgers: Array.Empty<CanonicalLedgerProjection>(),
            GeneratedAt: new CanonicalTimestamp()
        );
    }

    private static string KindRawValue(CanonicalTransferKind kind)
    {
        return kind switch
        {
            CanonicalTransferKind.RecordingAudioUpload => "recordingAudioUpload",
            CanonicalTransferKind.GeneratedArtifactDownload => "generatedArtifactDownload",
            CanonicalTransferKind.MetadataSend => "metadataSend",
            CanonicalTransferKind.MetadataApply => "metadataApply",
            CanonicalTransferKind.FolderMetadataSend => "folderMetadataSend",
            CanonicalTransferKind.FolderMetadataApply => "folderMetadataApply",
            CanonicalTransferKind.StudyItemMetadataSend => "studyItemMetadataSend",
            CanonicalTransferKind.StudyItemMetadataApply => "studyItemMetadataApply",
            CanonicalTransferKind.TombstoneSend => "tombstoneSend",
            CanonicalTransferKind.TombstoneApply => "tombstoneApply",
            _ => kind.ToString()
        };
    }

    private static string DirectionRawValue(CanonicalTransferDirection direction)
    {
        return direction switch
        {
            CanonicalTransferDirection.LocalToPeer => "localToPeer",
            CanonicalTransferDirection.PeerToLocal => "peerToLocal",
            CanonicalTransferDirection.LocalOnly => "localOnly",
            CanonicalTransferDirection.PeerOnly => "peerOnly",
            _ => direction.ToString()
        };
    }

    private static string SafeToken(string? raw)
    {
        var trimmed = raw?.Trim();
        return string.IsNullOrEmpty(trimmed) ? "unknown" : trimmed;
    }
}
