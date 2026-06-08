using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalNoCommitSideEffectClass
{
    stagingOnly
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalNoCommitBlockerSeverity
{
    warning,
    blocker
}

public sealed class CanonicalNoCommitBlocker : IEquatable<CanonicalNoCommitBlocker>
{
    public string Id => string.Join("|", Severity.ToString(), Reason);

    public CanonicalNoCommitBlockerSeverity Severity { get; }
    public string Reason { get; }

    public CanonicalNoCommitBlocker(
        CanonicalNoCommitBlockerSeverity severity,
        string reason)
    {
        Severity = severity;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? severity.ToString();
    }

    public override int GetHashCode() => HashCode.Combine(Severity, Reason);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitBlocker other && Equals(other);
    public bool Equals(CanonicalNoCommitBlocker? other) =>
        other is not null && Severity == other.Severity && Reason == other.Reason;
    public static bool operator ==(CanonicalNoCommitBlocker left, CanonicalNoCommitBlocker right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitBlocker left, CanonicalNoCommitBlocker right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalNoCommitStagingRootKind
{
    systemTemporary,
    explicitStagingRoot,
    rejectedProductionRoot
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalNoCommitStagingRootLifecycleStatus
{
    notCreated,
    created,
    validationFailed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalNoCommitStagingRootCleanupStatus
{
    removed,
    retainedForDiagnostics,
    refusedProductionRoot,
    failed
}

[JsonConverter(typeof(CanonicalNoCommitStagingRootCleanupPolicyConverter))]
public sealed class CanonicalNoCommitStagingRootCleanupPolicy : IEquatable<CanonicalNoCommitStagingRootCleanupPolicy>
{
    public CanonicalNoCommitStagingRootCleanupPolicyKind Kind { get; }
    public TimeSpan MaxAge { get; }
    public int MaxCount { get; }
    public long MaxBytes { get; }

    private CanonicalNoCommitStagingRootCleanupPolicy(
        CanonicalNoCommitStagingRootCleanupPolicyKind kind,
        TimeSpan maxAge = default,
        int maxCount = 0,
        long maxBytes = 0)
    {
        Kind = kind;
        MaxAge = maxAge;
        MaxCount = Math.Max(0, maxCount);
        MaxBytes = Math.Max(0, maxBytes);
    }

    public static readonly CanonicalNoCommitStagingRootCleanupPolicy CleanupImmediately =
        new(CanonicalNoCommitStagingRootCleanupPolicyKind.cleanupImmediately);

    public static CanonicalNoCommitStagingRootCleanupPolicy RetainForDiagnostics(
        TimeSpan maxAge, int maxCount, long maxBytes) =>
        new(CanonicalNoCommitStagingRootCleanupPolicyKind.retainForDiagnostics, maxAge, maxCount, maxBytes);

    public string PolicyName => Kind switch
    {
        CanonicalNoCommitStagingRootCleanupPolicyKind.cleanupImmediately => "cleanupImmediately",
        CanonicalNoCommitStagingRootCleanupPolicyKind.retainForDiagnostics => "retainForDiagnostics",
        _ => Kind.ToString()
    };

    public override int GetHashCode() => HashCode.Combine(Kind, MaxAge, MaxCount, MaxBytes);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitStagingRootCleanupPolicy other && Equals(other);
    public bool Equals(CanonicalNoCommitStagingRootCleanupPolicy? other) =>
        other is not null && Kind == other.Kind && MaxAge == other.MaxAge &&
        MaxCount == other.MaxCount && MaxBytes == other.MaxBytes;
    public static bool operator ==(CanonicalNoCommitStagingRootCleanupPolicy left, CanonicalNoCommitStagingRootCleanupPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitStagingRootCleanupPolicy left, CanonicalNoCommitStagingRootCleanupPolicy right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalNoCommitStagingRootCleanupPolicyKind
{
    cleanupImmediately,
    retainForDiagnostics
}

public sealed class CanonicalNoCommitStagingRootCleanupPolicyConverter : JsonConverter<CanonicalNoCommitStagingRootCleanupPolicy>
{
    private const string KindKey = "kind";
    private const string MaxAgeKey = "maxAge";
    private const string MaxCountKey = "maxCount";
    private const string MaxBytesKey = "maxBytes";

    public override CanonicalNoCommitStagingRootCleanupPolicy Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object");

        CanonicalNoCommitStagingRootCleanupPolicyKind? kind = null;
        double maxAge = 0;
        int maxCount = 0;
        long maxBytes = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case KindKey:
                    kind = JsonSerializer.Deserialize<CanonicalNoCommitStagingRootCleanupPolicyKind>(ref reader, options);
                    break;
                case MaxAgeKey:
                    maxAge = reader.GetDouble();
                    break;
                case MaxCountKey:
                    maxCount = reader.GetInt32();
                    break;
                case MaxBytesKey:
                    maxBytes = reader.GetInt64();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return kind switch
        {
            CanonicalNoCommitStagingRootCleanupPolicyKind.cleanupImmediately =>
                CanonicalNoCommitStagingRootCleanupPolicy.CleanupImmediately,
            CanonicalNoCommitStagingRootCleanupPolicyKind.retainForDiagnostics =>
                CanonicalNoCommitStagingRootCleanupPolicy.RetainForDiagnostics(
                    TimeSpan.FromSeconds(maxAge), maxCount, maxBytes),
            _ => throw new JsonException($"Unknown cleanup policy kind: {kind}")
        };
    }

    public override void Write(
        Utf8JsonWriter writer, CanonicalNoCommitStagingRootCleanupPolicy value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(KindKey, value.Kind.ToString());
        if (value.Kind == CanonicalNoCommitStagingRootCleanupPolicyKind.retainForDiagnostics)
        {
            writer.WriteNumber(MaxAgeKey, Math.Max(0, value.MaxAge.TotalSeconds));
            writer.WriteNumber(MaxCountKey, Math.Max(0, value.MaxCount));
            writer.WriteNumber(MaxBytesKey, Math.Max(0, value.MaxBytes));
        }
        writer.WriteEndObject();
    }
}

public sealed class CanonicalNoCommitStagingRoot : IEquatable<CanonicalNoCommitStagingRoot>
{
    public string RootID { get; }
    public CanonicalNoCommitStagingRootKind RootKind { get; }
    public string RootURL { get; }
    public string? ProductionRootURL { get; }
    public DateTime CreatedAt { get; }

    public CanonicalNoCommitStagingRoot(
        string? rootID = null,
        CanonicalNoCommitStagingRootKind rootKind = CanonicalNoCommitStagingRootKind.explicitStagingRoot,
        string rootURL = "",
        string? productionRootURL = null,
        DateTime? createdAt = null)
    {
        RootID = CanonicalProductionRedaction.SafeIdentifier(
            rootID ?? Guid.NewGuid().ToString(), "no-commit-root");
        RootKind = rootKind;
        RootURL = StandardizeFileURL(rootURL);
        ProductionRootURL = productionRootURL != null ? StandardizeFileURL(productionRootURL) : null;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    private static string StandardizeFileURL(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            path = path[7..];
        return Path.GetFullPath(path).Replace('\\', '/');
    }

    public override int GetHashCode() =>
        HashCode.Combine(RootID, RootKind, RootURL, ProductionRootURL, CreatedAt);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitStagingRoot other && Equals(other);
    public bool Equals(CanonicalNoCommitStagingRoot? other) =>
        other is not null && RootID == other.RootID && RootKind == other.RootKind &&
        RootURL == other.RootURL && ProductionRootURL == other.ProductionRootURL && CreatedAt == other.CreatedAt;
    public static bool operator ==(CanonicalNoCommitStagingRoot left, CanonicalNoCommitStagingRoot right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitStagingRoot left, CanonicalNoCommitStagingRoot right) => !left.Equals(right);
}

public sealed class CanonicalNoCommitStagingRootRetentionRecord : IEquatable<CanonicalNoCommitStagingRootRetentionRecord>
{
    public string Id => RootID;

    public string RootID { get; }
    public CanonicalNoCommitStagingRootKind RootKind { get; }
    public CanonicalTimestamp CreatedAt { get; }
    public long RetainedBytes { get; }
    public int EntryCount { get; }

    public CanonicalNoCommitStagingRootRetentionRecord(
        string rootID,
        CanonicalNoCommitStagingRootKind rootKind,
        DateTime? createdAt = null,
        long retainedBytes = 0,
        int entryCount = 0)
    {
        RootID = CanonicalProductionRedaction.SafeIdentifier(rootID, "no-commit-root");
        RootKind = rootKind;
        CreatedAt = new CanonicalTimestamp(createdAt ?? DateTime.UtcNow);
        RetainedBytes = Math.Max(0, retainedBytes);
        EntryCount = Math.Max(0, entryCount);
    }

    public override int GetHashCode() => HashCode.Combine(RootID, RootKind, CreatedAt, RetainedBytes, EntryCount);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitStagingRootRetentionRecord other && Equals(other);
    public bool Equals(CanonicalNoCommitStagingRootRetentionRecord? other) =>
        other is not null && RootID == other.RootID && RootKind == other.RootKind &&
        CreatedAt.Equals(other.CreatedAt) && RetainedBytes == other.RetainedBytes && EntryCount == other.EntryCount;
    public static bool operator ==(CanonicalNoCommitStagingRootRetentionRecord left, CanonicalNoCommitStagingRootRetentionRecord right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitStagingRootRetentionRecord left, CanonicalNoCommitStagingRootRetentionRecord right) => !left.Equals(right);
}

public sealed class CanonicalNoCommitStagingRootCleanupResult : IEquatable<CanonicalNoCommitStagingRootCleanupResult>
{
    public string RootID { get; }
    public CanonicalNoCommitStagingRootKind RootKind { get; }
    public CanonicalNoCommitStagingRootCleanupPolicy Policy { get; }
    public CanonicalNoCommitStagingRootCleanupStatus Status { get; }
    public int RemovedRootCount { get; }
    public int RetainedRootCount { get; }
    public long RemovedBytes { get; }
    public long RetainedBytes { get; }
    public int FileCount { get; }
    public long ByteCount { get; }
    public CanonicalNoCommitBlocker? Warning { get; }

    public CanonicalNoCommitStagingRootCleanupResult(
        string rootID,
        CanonicalNoCommitStagingRootKind rootKind,
        CanonicalNoCommitStagingRootCleanupPolicy policy,
        CanonicalNoCommitStagingRootCleanupStatus status,
        int removedRootCount = 0,
        int retainedRootCount = 0,
        long removedBytes = 0,
        long retainedBytes = 0,
        int fileCount = 0,
        long byteCount = 0,
        CanonicalNoCommitBlocker? warning = null)
    {
        RootID = CanonicalProductionRedaction.SafeIdentifier(rootID, "no-commit-root");
        RootKind = rootKind;
        Policy = policy;
        Status = status;
        RemovedRootCount = Math.Max(0, removedRootCount);
        RetainedRootCount = Math.Max(0, retainedRootCount);
        RemovedBytes = Math.Max(0, removedBytes);
        RetainedBytes = Math.Max(0, retainedBytes);
        FileCount = Math.Max(0, fileCount);
        ByteCount = Math.Max(0, byteCount);
        Warning = warning;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"rootKind={RootKind}",
        $"rootID={RootID}",
        $"policy={Policy.PolicyName}",
        $"cleanup={Status}",
        $"files={FileCount}",
        $"bytes={ByteCount}",
        $"removedRoots={RemovedRootCount}",
        $"retainedRoots={RetainedRootCount}",
        $"removedBytes={RemovedBytes}",
        $"retainedBytes={RetainedBytes}",
        $"warning={Warning?.Reason ?? "none"}");

    public override int GetHashCode() =>
        HashCode.Combine(RootID, RootKind, Policy, Status, RemovedRootCount, RetainedRootCount,
            RemovedBytes, RetainedBytes, FileCount, ByteCount, Warning);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitStagingRootCleanupResult other && Equals(other);
    public bool Equals(CanonicalNoCommitStagingRootCleanupResult? other) =>
        other is not null && RootID == other.RootID && RootKind == other.RootKind &&
        EqualityComparer<CanonicalNoCommitStagingRootCleanupPolicy>.Default.Equals(Policy, other.Policy) &&
        Status == other.Status && RemovedRootCount == other.RemovedRootCount &&
        RetainedRootCount == other.RetainedRootCount && RemovedBytes == other.RemovedBytes &&
        RetainedBytes == other.RetainedBytes && FileCount == other.FileCount &&
        ByteCount == other.ByteCount && EqualityComparer<CanonicalNoCommitBlocker?>.Default.Equals(Warning, other.Warning);
    public static bool operator ==(CanonicalNoCommitStagingRootCleanupResult left, CanonicalNoCommitStagingRootCleanupResult right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitStagingRootCleanupResult left, CanonicalNoCommitStagingRootCleanupResult right) => !left.Equals(right);
}

public sealed class CanonicalNoCommitStagingEvidence : IEquatable<CanonicalNoCommitStagingEvidence>
{
    public string RootID { get; }
    public CanonicalNoCommitStagingRootKind RootKind { get; }
    public CanonicalNoCommitStagingRootLifecycleStatus LifecycleStatus { get; }
    public int FileCount { get; }
    public long ByteCount { get; }
    public bool WroteOnlyStagingRoot { get; }
    public CanonicalNoCommitSideEffectClass SideEffectClass { get; }

    public CanonicalNoCommitStagingEvidence(
        string rootID,
        CanonicalNoCommitStagingRootKind rootKind,
        CanonicalNoCommitStagingRootLifecycleStatus lifecycleStatus,
        int fileCount = 0,
        long byteCount = 0,
        bool wroteOnlyStagingRoot = true,
        CanonicalNoCommitSideEffectClass sideEffectClass = CanonicalNoCommitSideEffectClass.stagingOnly)
    {
        RootID = CanonicalProductionRedaction.SafeIdentifier(rootID, "no-commit-root");
        RootKind = rootKind;
        LifecycleStatus = lifecycleStatus;
        FileCount = Math.Max(0, fileCount);
        ByteCount = Math.Max(0, byteCount);
        WroteOnlyStagingRoot = wroteOnlyStagingRoot;
        SideEffectClass = sideEffectClass;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"rootKind={RootKind}",
        $"rootID={RootID}",
        $"lifecycle={LifecycleStatus}",
        $"files={FileCount}",
        $"bytes={ByteCount}",
        $"sideEffectClass={SideEffectClass}");

    public override int GetHashCode() =>
        HashCode.Combine(RootID, RootKind, LifecycleStatus, FileCount, ByteCount, WroteOnlyStagingRoot, SideEffectClass);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitStagingEvidence other && Equals(other);
    public bool Equals(CanonicalNoCommitStagingEvidence? other) =>
        other is not null && RootID == other.RootID && RootKind == other.RootKind &&
        LifecycleStatus == other.LifecycleStatus && FileCount == other.FileCount &&
        ByteCount == other.ByteCount && WroteOnlyStagingRoot == other.WroteOnlyStagingRoot &&
        SideEffectClass == other.SideEffectClass;
    public static bool operator ==(CanonicalNoCommitStagingEvidence left, CanonicalNoCommitStagingEvidence right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitStagingEvidence left, CanonicalNoCommitStagingEvidence right) => !left.Equals(right);
}

public sealed class CanonicalNoCommitCleanupEvidence : IEquatable<CanonicalNoCommitCleanupEvidence>
{
    public string RootID { get; }
    public CanonicalNoCommitStagingRootKind RootKind { get; }
    public string Policy { get; }
    public CanonicalNoCommitStagingRootCleanupStatus Status { get; }
    public int FileCount { get; }
    public long ByteCount { get; }
    public int RemovedRootCount { get; }
    public int RetainedRootCount { get; }
    public CanonicalNoCommitBlocker? Warning { get; }

    public CanonicalNoCommitCleanupEvidence(CanonicalNoCommitStagingRootCleanupResult result)
    {
        RootID = result.RootID;
        RootKind = result.RootKind;
        Policy = result.Policy.PolicyName;
        Status = result.Status;
        FileCount = result.FileCount;
        ByteCount = result.ByteCount;
        RemovedRootCount = result.RemovedRootCount;
        RetainedRootCount = result.RetainedRootCount;
        Warning = result.Warning;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"rootKind={RootKind}",
        $"rootID={RootID}",
        $"policy={Policy}",
        $"cleanup={Status}",
        $"files={FileCount}",
        $"bytes={ByteCount}",
        $"removedRoots={RemovedRootCount}",
        $"retainedRoots={RetainedRootCount}",
        $"warning={Warning?.Reason ?? "none"}");

    public override int GetHashCode() =>
        HashCode.Combine(RootID, RootKind, Policy, Status, FileCount, ByteCount, RemovedRootCount, RetainedRootCount, Warning);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitCleanupEvidence other && Equals(other);
    public bool Equals(CanonicalNoCommitCleanupEvidence? other) =>
        other is not null && RootID == other.RootID && RootKind == other.RootKind &&
        Policy == other.Policy && Status == other.Status && FileCount == other.FileCount &&
        ByteCount == other.ByteCount && RemovedRootCount == other.RemovedRootCount &&
        RetainedRootCount == other.RetainedRootCount &&
        EqualityComparer<CanonicalNoCommitBlocker?>.Default.Equals(Warning, other.Warning);
    public static bool operator ==(CanonicalNoCommitCleanupEvidence left, CanonicalNoCommitCleanupEvidence right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitCleanupEvidence left, CanonicalNoCommitCleanupEvidence right) => !left.Equals(right);
}

public sealed class CanonicalNoCommitStagingRootLifecycle : IEquatable<CanonicalNoCommitStagingRootLifecycle>
{
    public CanonicalNoCommitStagingRoot Root { get; }

    public CanonicalNoCommitStagingRootLifecycle(CanonicalNoCommitStagingRoot root)
    {
        Root = root;
    }

    public CanonicalNoCommitStagingRootRetentionRecord RetentionRecord
    {
        get
        {
            var stats = DirectoryStats(Root.RootURL);
            return new CanonicalNoCommitStagingRootRetentionRecord(
                rootID: Root.RootID,
                rootKind: Root.RootKind,
                createdAt: Root.CreatedAt,
                retainedBytes: stats.Bytes,
                entryCount: stats.Files
            );
        }
    }

    public CanonicalNoCommitBlocker? ValidateRoot()
    {
        if (!Uri.TryCreate(Root.RootURL, UriKind.Absolute, out var uri) || uri.Scheme != "file")
            return new CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.blocker, "stagingRootMustBeFileURL");

        if (Root.RootKind == CanonicalNoCommitStagingRootKind.rejectedProductionRoot)
            return new CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.blocker, "productionRootRefused");

        if (Root.ProductionRootURL != null)
        {
            var stagingPath = StandardizePath(Root.RootURL);
            var productionPath = StandardizePath(Root.ProductionRootURL);
            if (stagingPath == productionPath ||
                (stagingPath + "/").StartsWith(productionPath + "/", StringComparison.Ordinal))
            {
                return new CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.blocker, "productionRootRefused");
            }
        }

        if (Root.RootKind == CanonicalNoCommitStagingRootKind.systemTemporary)
        {
            var stagingPath = StandardizePath(Root.RootURL);
            var tempPath = StandardizePath(Path.GetTempPath());
            if (stagingPath != tempPath &&
                !(stagingPath + "/").StartsWith(tempPath + "/", StringComparison.Ordinal))
            {
                return new CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.blocker, "systemTemporaryRootRequired");
            }
        }

        return null;
    }

    public CanonicalNoCommitStagingEvidence StagingEvidence(
        CanonicalNoCommitStagingRootLifecycleStatus status)
    {
        var stats = DirectoryStats(Root.RootURL);
        return new CanonicalNoCommitStagingEvidence(
            rootID: Root.RootID,
            rootKind: Root.RootKind,
            lifecycleStatus: status,
            fileCount: stats.Files,
            byteCount: stats.Bytes,
            wroteOnlyStagingRoot: status == CanonicalNoCommitStagingRootLifecycleStatus.created
        );
    }

    public CanonicalNoCommitStagingRootCleanupResult Cleanup(
        CanonicalNoCommitStagingRootCleanupPolicy policy,
        DateTime? now = null)
    {
        var effectiveNow = now ?? DateTime.UtcNow;
        var stats = DirectoryStats(Root.RootURL);
        var blocker = ValidateRoot();
        if (blocker != null && blocker.Reason == "productionRootRefused")
        {
            return new CanonicalNoCommitStagingRootCleanupResult(
                rootID: Root.RootID,
                rootKind: CanonicalNoCommitStagingRootKind.rejectedProductionRoot,
                policy: policy,
                status: CanonicalNoCommitStagingRootCleanupStatus.refusedProductionRoot,
                retainedRootCount: Directory.Exists(Root.RootURL) ? 1 : 0,
                retainedBytes: stats.Bytes,
                fileCount: stats.Files,
                byteCount: stats.Bytes,
                warning: blocker
            );
        }

        return policy.Kind switch
        {
            CanonicalNoCommitStagingRootCleanupPolicyKind.cleanupImmediately =>
                RemoveCurrentRoot(policy, stats, null),
            CanonicalNoCommitStagingRootCleanupPolicyKind.retainForDiagnostics =>
                HandleRetainForDiagnostics(policy, stats, effectiveNow),
            _ => RemoveCurrentRoot(policy, stats, null)
        };
    }

    private CanonicalNoCommitStagingRootCleanupResult HandleRetainForDiagnostics(
        CanonicalNoCommitStagingRootCleanupPolicy policy,
        (int Files, long Bytes) stats,
        DateTime now)
    {
        var boundedMaxCount = Math.Max(0, policy.MaxCount);
        var boundedMaxBytes = Math.Max(0, policy.MaxBytes);
        var boundedMaxAge = policy.MaxAge > TimeSpan.Zero ? policy.MaxAge : TimeSpan.Zero;

        if (boundedMaxCount == 0 || stats.Bytes > boundedMaxBytes)
        {
            return RemoveCurrentRoot(policy, stats, "retentionBoundsExceeded");
        }

        var parentDir = Path.GetDirectoryName(Root.RootURL);
        if (parentDir == null)
            return RemoveCurrentRoot(policy, stats, "retentionBoundsExceeded");

        var purge = PurgeRetainedRoots(
            parentDirectory: parentDir,
            protectedRootPath: Root.RootURL,
            maxAge: boundedMaxAge,
            maxCount: boundedMaxCount,
            maxBytes: boundedMaxBytes,
            now: now);

        var retainedStats = DirectoryStats(Root.RootURL);
        return new CanonicalNoCommitStagingRootCleanupResult(
            rootID: Root.RootID,
            rootKind: Root.RootKind,
            policy: policy,
            status: CanonicalNoCommitStagingRootCleanupStatus.retainedForDiagnostics,
            removedRootCount: purge.RemovedCount,
            retainedRootCount: Directory.Exists(Root.RootURL) ? 1 : 0,
            removedBytes: purge.RemovedBytes,
            retainedBytes: retainedStats.Bytes,
            fileCount: retainedStats.Files,
            byteCount: retainedStats.Bytes
        );
    }

    private CanonicalNoCommitStagingRootCleanupResult RemoveCurrentRoot(
        CanonicalNoCommitStagingRootCleanupPolicy policy,
        (int Files, long Bytes) stats,
        string? reason)
    {
        try
        {
            if (Directory.Exists(Root.RootURL))
                Directory.Delete(Root.RootURL, true);

            return new CanonicalNoCommitStagingRootCleanupResult(
                rootID: Root.RootID,
                rootKind: Root.RootKind,
                policy: policy,
                status: CanonicalNoCommitStagingRootCleanupStatus.removed,
                removedRootCount: stats.Files > 0 || stats.Bytes > 0 ? 1 : 0,
                removedBytes: stats.Bytes,
                fileCount: stats.Files,
                byteCount: stats.Bytes,
                warning: reason != null
                    ? new CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.warning, reason)
                    : null
            );
        }
        catch
        {
            return new CanonicalNoCommitStagingRootCleanupResult(
                rootID: Root.RootID,
                rootKind: Root.RootKind,
                policy: policy,
                status: CanonicalNoCommitStagingRootCleanupStatus.failed,
                retainedRootCount: 1,
                retainedBytes: stats.Bytes,
                fileCount: stats.Files,
                byteCount: stats.Bytes,
                warning: new CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.warning, "cleanupFailed")
            );
        }
    }

    private static (int RemovedCount, long RemovedBytes) PurgeRetainedRoots(
        string parentDirectory,
        string protectedRootPath,
        TimeSpan maxAge,
        int maxCount,
        long maxBytes,
        DateTime now)
    {
        if (!Directory.Exists(parentDirectory))
            return (0, 0);

        var protectedPath = StandardizePath(protectedRootPath);
        List<DirectoryInfo>? dirs;
        try
        {
            dirs = new DirectoryInfo(parentDirectory)
                .GetDirectories()
                .Where(d => (d.Attributes & FileAttributes.Hidden) == 0)
                .ToList();
        }
        catch
        {
            return (0, 0);
        }

        var candidates = dirs
            .Select(d =>
            {
                var path = StandardizePath(d.FullName);
                if (path == protectedPath) return null;
                var createdAt = d.CreationTimeUtc;
                var bytes = DirectoryStats(path).Bytes;
                return (Dir: d, Path: path, CreatedAt: createdAt, Bytes: bytes);
            })
            .Where(c => c != null)
            .Cast<(DirectoryInfo Dir, string Path, DateTime CreatedAt, long Bytes)>()
            .ToList();

        int removedCount = 0;
        long removedBytes = 0;

        // Phase 1: remove by age
        var agedOut = candidates
            .Where(c => now - c.CreatedAt > maxAge)
            .ToList();
        foreach (var c in agedOut)
        {
            if (TryDeleteDirectory(c.Dir))
            {
                removedCount++;
                removedBytes += c.Bytes;
            }
        }
        candidates.RemoveAll(c => now - c.CreatedAt > maxAge);

        // Phase 2: remove by count/bytes
        var currentBytes = DirectoryStats(protectedPath).Bytes;
        var totalBytes = currentBytes + candidates.Sum(c => c.Bytes);
        var retainedRootCount = 1 + candidates.Count;

        foreach (var c in candidates.OrderBy(c => c.CreatedAt))
        {
            if (retainedRootCount <= maxCount && totalBytes <= maxBytes)
                break;

            if (TryDeleteDirectory(c.Dir))
            {
                removedCount++;
                removedBytes += c.Bytes;
                totalBytes -= c.Bytes;
                retainedRootCount--;
            }
        }

        return (removedCount, removedBytes);
    }

    private static bool TryDeleteDirectory(DirectoryInfo dir)
    {
        try
        {
            if (dir.Exists)
            {
                dir.Delete(true);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static (int Files, long Bytes) DirectoryStats(string path)
    {
        if (!Directory.Exists(path))
        {
            if (File.Exists(path))
            {
                try
                {
                    var fi = new FileInfo(path);
                    return (1, fi.Length);
                }
                catch
                {
                    return (0, 0);
                }
            }
            return (0, 0);
        }

        int files = 0;
        long bytes = 0;
        try
        {
            var dirInfo = new DirectoryInfo(path);
            var items = dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories);
            foreach (var item in items)
            {
                if ((item.Attributes & FileAttributes.Hidden) != 0)
                    continue;
                if (item is FileInfo fi)
                {
                    files++;
                    bytes += fi.Length;
                }
            }
        }
        catch
        {
            return (0, 0);
        }

        return (files, bytes);
    }

    private static string StandardizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace('\\', '/');

    public override int GetHashCode() => Root.GetHashCode();
    public override bool Equals(object? obj) => obj is CanonicalNoCommitStagingRootLifecycle other && Equals(other);
    public bool Equals(CanonicalNoCommitStagingRootLifecycle? other) =>
        other is not null && EqualityComparer<CanonicalNoCommitStagingRoot>.Default.Equals(Root, other.Root);
    public static bool operator ==(CanonicalNoCommitStagingRootLifecycle left, CanonicalNoCommitStagingRootLifecycle right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitStagingRootLifecycle left, CanonicalNoCommitStagingRootLifecycle right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalNoCommitEvidenceStatus
{
    complete,
    blocked,
    divergent,
    insufficientEvidence,
    unsupported,
    warning
}

public sealed class CanonicalNoCommitEquivalenceEvidence : IEquatable<CanonicalNoCommitEquivalenceEvidence>
{
    public int EquivalentCount { get; }
    public int DivergentCount { get; }
    public int InsufficientEvidenceCount { get; }
    public int UnsupportedCount { get; }
    public List<string> HashPrefixes { get; }
    public string RouteProjectionStatus { get; }
    public string LegacyActionComparisonStatus { get; }

    public CanonicalNoCommitEquivalenceEvidence(
        List<CanonicalRecordingMetadataNoCommitCandidateResult> candidateResults)
    {
        EquivalentCount = candidateResults.Count(r =>
            r.Equivalence.Status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.equivalent ||
            r.Equivalence.Status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.canonicalMoreConservative);
        DivergentCount = candidateResults.Count(r =>
            r.Equivalence.Status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.divergent);
        InsufficientEvidenceCount = candidateResults.Count(r =>
            r.Equivalence.Status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.insufficientEvidence);
        UnsupportedCount = candidateResults.Count(r =>
            r.Equivalence.Status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.unsupported);
        HashPrefixes = new HashSet<string>(
                candidateResults.Select(r => r.Equivalence.MetadataHashPrefix).Where(p => p != null)!)
            .OrderBy(p => p).ToList();
        RouteProjectionStatus = candidateResults.Any(r =>
                r.Equivalence.CanonicalDirection == CanonicalRecordingMetadataNoCommitDirection.send &&
                r.Equivalence.RoutePath != "/sync/apply-metadata")
            ? "routeProjectionDivergent"
            : "routeProjectionSafe";
        LegacyActionComparisonStatus = candidateResults.Any(r => r.Equivalence.Blocking)
            ? "legacyActionComparisonBlocked"
            : "legacyActionComparisonEquivalent";
    }

    public override int GetHashCode() =>
        HashCode.Combine(EquivalentCount, DivergentCount, InsufficientEvidenceCount, UnsupportedCount,
            HashPrefixes.Count, RouteProjectionStatus, LegacyActionComparisonStatus);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitEquivalenceEvidence other && Equals(other);
    public bool Equals(CanonicalNoCommitEquivalenceEvidence? other) =>
        other is not null && EquivalentCount == other.EquivalentCount && DivergentCount == other.DivergentCount &&
        InsufficientEvidenceCount == other.InsufficientEvidenceCount && UnsupportedCount == other.UnsupportedCount &&
        HashPrefixes.SequenceEqual(other.HashPrefixes) && RouteProjectionStatus == other.RouteProjectionStatus &&
        LegacyActionComparisonStatus == other.LegacyActionComparisonStatus;
    public static bool operator ==(CanonicalNoCommitEquivalenceEvidence left, CanonicalNoCommitEquivalenceEvidence right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitEquivalenceEvidence left, CanonicalNoCommitEquivalenceEvidence right) => !left.Equals(right);
}

public sealed class CanonicalNoCommitEvidenceReport : IEquatable<CanonicalNoCommitEvidenceReport>
{
    public CanonicalCutoverDomain Domain { get; }
    public CanonicalCutoverAppSeamMode Mode { get; }
    public CanonicalNoCommitEvidenceStatus Status { get; }
    public int CandidateCount { get; }
    public int WouldApplyCount { get; }
    public int WouldSendCount { get; }
    public int EquivalentCount { get; }
    public int DivergentCount { get; }
    public int InsufficientEvidenceCount { get; }
    public int UnsupportedCount { get; }
    public string StagingRootLifecycleStatus { get; }
    public string CleanupStatus { get; }
    public string RouteProjectionStatus { get; }
    public string LegacyActionComparisonStatus { get; }
    public bool ProductionCommitSuppressed { get; }
    public bool LegacyDuplicateSuppressed { get; }
    public CanonicalNoCommitSideEffectClass SideEffectClass { get; }
    public CanonicalNoCommitEquivalenceEvidence EquivalenceEvidence { get; }
    public List<CanonicalNoCommitStagingEvidence> StagingEvidence { get; }
    public List<CanonicalNoCommitCleanupEvidence> CleanupEvidence { get; }
    public List<CanonicalNoCommitBlocker> Blockers { get; }

    public CanonicalNoCommitEvidenceReport()
    {
        Domain = CanonicalCutoverDomain.recordingMetadata;
        Mode = CanonicalCutoverAppSeamMode.disabled;
        Status = CanonicalNoCommitEvidenceStatus.blocked;
        CandidateCount = 0;
        WouldApplyCount = 0;
        WouldSendCount = 0;
        EquivalentCount = 0;
        DivergentCount = 0;
        InsufficientEvidenceCount = 0;
        UnsupportedCount = 0;
        StagingRootLifecycleStatus = "";
        CleanupStatus = "";
        RouteProjectionStatus = "routeProjectionSafe";
        LegacyActionComparisonStatus = "legacyActionComparisonEquivalent";
        ProductionCommitSuppressed = true;
        LegacyDuplicateSuppressed = false;
        SideEffectClass = CanonicalNoCommitSideEffectClass.stagingOnly;
        EquivalenceEvidence = new CanonicalNoCommitEquivalenceEvidence(
            new List<CanonicalRecordingMetadataNoCommitCandidateResult>());
        StagingEvidence = new List<CanonicalNoCommitStagingEvidence>();
        CleanupEvidence = new List<CanonicalNoCommitCleanupEvidence>();
        Blockers = new List<CanonicalNoCommitBlocker>();
    }

    public CanonicalNoCommitEvidenceReport(
        CanonicalCutoverAppSeamGate gate,
        List<CanonicalRecordingMetadataNoCommitCandidateResult> candidateResults,
        bool productionCommitSuppressed = true,
        bool legacyDuplicateSuppressed = false)
    {
        var equivalence = new CanonicalNoCommitEquivalenceEvidence(candidateResults);
        var staging = candidateResults
            .Where(r => r.Staging?.StagingEvidence != null)
            .Select(r => r.Staging!.StagingEvidence!)
            .ToList();
        var cleanup = candidateResults
            .Where(r => r.Staging?.CleanupEvidence != null)
            .Select(r => r.Staging!.CleanupEvidence!)
            .ToList();

        var blockers = new List<CanonicalNoCommitBlocker>();
        blockers.AddRange(gate.Failures.Select(f =>
            new CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.blocker, f.ToString())));
        blockers.AddRange(candidateResults
            .Where(r => r.Failure != null)
            .Select(r => new CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.blocker, r.Failure!.Value.ToString())));
        blockers.AddRange(cleanup
            .Where(c => c.Warning != null)
            .Select(c => c.Warning!));

        CanonicalNoCommitEvidenceStatus status;
        if (!gate.Allowed)
            status = CanonicalNoCommitEvidenceStatus.blocked;
        else if (equivalence.UnsupportedCount > 0)
            status = CanonicalNoCommitEvidenceStatus.unsupported;
        else if (equivalence.InsufficientEvidenceCount > 0)
            status = CanonicalNoCommitEvidenceStatus.insufficientEvidence;
        else if (equivalence.DivergentCount > 0)
            status = CanonicalNoCommitEvidenceStatus.divergent;
        else if (blockers.Any(b => b.Severity == CanonicalNoCommitBlockerSeverity.warning))
            status = CanonicalNoCommitEvidenceStatus.warning;
        else
            status = CanonicalNoCommitEvidenceStatus.complete;

        Domain = gate.Domain;
        Mode = gate.Mode;
        Status = status;
        CandidateCount = candidateResults.Count;
        WouldApplyCount = candidateResults.Count(r => r.Staging?.WouldApply == true);
        WouldSendCount = candidateResults.Count(r => r.Staging?.WouldSend == true);
        EquivalentCount = equivalence.EquivalentCount;
        DivergentCount = equivalence.DivergentCount;
        InsufficientEvidenceCount = equivalence.InsufficientEvidenceCount;
        UnsupportedCount = equivalence.UnsupportedCount;
        StagingRootLifecycleStatus = string.Join(",",
            staging.Select(s => s.LifecycleStatus.ToString()).OrderBy(x => x));
        CleanupStatus = string.Join(",",
            cleanup.Select(c => c.Status.ToString()).OrderBy(x => x));
        RouteProjectionStatus = equivalence.RouteProjectionStatus;
        LegacyActionComparisonStatus = equivalence.LegacyActionComparisonStatus;
        ProductionCommitSuppressed = productionCommitSuppressed;
        LegacyDuplicateSuppressed = legacyDuplicateSuppressed;
        SideEffectClass = CanonicalNoCommitSideEffectClass.stagingOnly;
        EquivalenceEvidence = equivalence;
        StagingEvidence = staging;
        CleanupEvidence = cleanup;
        Blockers = new HashSet<CanonicalNoCommitBlocker>(blockers)
            .OrderBy(b => b.Id).ToList();
    }

    public string DiagnosticsSummary => string.Join(",",
        $"domain={Domain}",
        $"mode={Mode}",
        $"status={Status}",
        $"candidateCount={CandidateCount}",
        $"wouldApply={WouldApplyCount}",
        $"wouldSend={WouldSendCount}",
        $"equivalent={EquivalentCount}",
        $"divergent={DivergentCount}",
        $"insufficientEvidence={InsufficientEvidenceCount}",
        $"unsupported={UnsupportedCount}",
        $"staging={(string.IsNullOrEmpty(StagingRootLifecycleStatus) ? "none" : StagingRootLifecycleStatus)}",
        $"cleanup={(string.IsNullOrEmpty(CleanupStatus) ? "none" : CleanupStatus)}",
        $"routeProjection={RouteProjectionStatus}",
        $"legacyComparison={LegacyActionComparisonStatus}",
        $"productionCommitSuppressed={ProductionCommitSuppressed}",
        $"legacyDuplicateSuppressed={LegacyDuplicateSuppressed}",
        $"sideEffectClass={SideEffectClass}");

    public override int GetHashCode() =>
        HashCode.Combine(Domain, Mode, Status, CandidateCount, WouldApplyCount, WouldSendCount,
            EquivalentCount, DivergentCount, InsufficientEvidenceCount, UnsupportedCount,
            StagingRootLifecycleStatus, CleanupStatus, RouteProjectionStatus, LegacyActionComparisonStatus,
            ProductionCommitSuppressed, LegacyDuplicateSuppressed, SideEffectClass,
            EquivalenceEvidence, StagingEvidence.Count, CleanupEvidence.Count, Blockers.Count);
    public override bool Equals(object? obj) => obj is CanonicalNoCommitEvidenceReport other && Equals(other);
    public bool Equals(CanonicalNoCommitEvidenceReport? other) =>
        other is not null && Domain == other.Domain && Mode == other.Mode &&
        Status == other.Status && CandidateCount == other.CandidateCount &&
        WouldApplyCount == other.WouldApplyCount && WouldSendCount == other.WouldSendCount &&
        EquivalentCount == other.EquivalentCount && DivergentCount == other.DivergentCount &&
        InsufficientEvidenceCount == other.InsufficientEvidenceCount &&
        UnsupportedCount == other.UnsupportedCount &&
        StagingRootLifecycleStatus == other.StagingRootLifecycleStatus &&
        CleanupStatus == other.CleanupStatus &&
        RouteProjectionStatus == other.RouteProjectionStatus &&
        LegacyActionComparisonStatus == other.LegacyActionComparisonStatus &&
        ProductionCommitSuppressed == other.ProductionCommitSuppressed &&
        LegacyDuplicateSuppressed == other.LegacyDuplicateSuppressed &&
        SideEffectClass == other.SideEffectClass &&
        EqualityComparer<CanonicalNoCommitEquivalenceEvidence>.Default.Equals(EquivalenceEvidence, other.EquivalenceEvidence) &&
        StagingEvidence.SequenceEqual(other.StagingEvidence) &&
        CleanupEvidence.SequenceEqual(other.CleanupEvidence) &&
        Blockers.SequenceEqual(other.Blockers);
    public static bool operator ==(CanonicalNoCommitEvidenceReport left, CanonicalNoCommitEvidenceReport right) => left.Equals(right);
    public static bool operator !=(CanonicalNoCommitEvidenceReport left, CanonicalNoCommitEvidenceReport right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalNoCommitMigrationStage
{
    off,
    notStarted,
    projected,
    planned,
    noCommit,
    realApplyPort,
    commitExecutor,
    appSeamDefaultOff,
    nextPilotCandidate,
    canaryN0,
    canaryN1,
    expandedCanary,
    domainCutover,
    readSideParallel,
    readSideCutover,
    retirementCandidate,
    retired,
    diagnosticsOnly,
    decisionShadow,
    executionShadow,
    realDataShadowCopy,
    readOnlyTransportProbe,
    recordingMetadataNoCommit,
    recordingMetadataGuardedCommit,
    unsupported
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationStageSideEffect
{
    diagnosticsWrite,
    shadowRootWrite,
    readOnlyNetworkProbe,
    stagingRootWrite,
    productionCommit
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalMigrationStageEvidence
{
    none,
    dryRunEquivalence,
    executionShadow,
    realDataShadowCopy,
    readOnlyTransportProbe,
    noCommitEvidenceReport,
    ownerApproval,
    rollbackPlan
}

public sealed class CanonicalMigrationStagePolicy : IEquatable<CanonicalMigrationStagePolicy>
{
    public List<CanonicalMigrationStageSideEffect> AllowedSideEffects { get; }
    public List<CanonicalMigrationStageEvidence> RequiredEvidence { get; }
    public List<CanonicalCutoverDomain> AllowedDomains { get; }
    public List<CanonicalCutoverDomain> ForbiddenDomains { get; }
    public bool ProductionCommitAllowed { get; }
    public List<string> ExistingConfigurationKeys { get; }

    public CanonicalMigrationStagePolicy(
        List<CanonicalMigrationStageSideEffect> allowedSideEffects,
        List<CanonicalMigrationStageEvidence> requiredEvidence,
        List<CanonicalCutoverDomain> allowedDomains,
        List<CanonicalCutoverDomain> forbiddenDomains,
        bool productionCommitAllowed = false,
        List<string>? existingConfigurationKeys = null)
    {
        AllowedSideEffects = new HashSet<CanonicalMigrationStageSideEffect>(allowedSideEffects)
            .OrderBy(s => s.ToString()).ToList();
        RequiredEvidence = new HashSet<CanonicalMigrationStageEvidence>(requiredEvidence)
            .OrderBy(e => e.ToString()).ToList();
        AllowedDomains = new HashSet<CanonicalCutoverDomain>(allowedDomains)
            .OrderBy(d => d.ToString()).ToList();
        ForbiddenDomains = new HashSet<CanonicalCutoverDomain>(forbiddenDomains)
            .OrderBy(d => d.ToString()).ToList();
        ProductionCommitAllowed = productionCommitAllowed;
        ExistingConfigurationKeys = (existingConfigurationKeys ?? new List<string>())
            .Select(k => CanonicalProductionRedaction.SafeDiagnosticText(k))
            .Where(k => k != null)
            .Select(k => k!)
            .Distinct()
            .OrderBy(k => k)
            .ToList();
    }

    public static CanonicalMigrationStagePolicy DefaultPolicy(CanonicalNoCommitMigrationStage stage)
    {
        var recordingOnly = new List<CanonicalCutoverDomain> { CanonicalCutoverDomain.recordingMetadata };
        var allExceptRecording = Enum.GetValues<CanonicalCutoverDomain>()
            .Where(d => d != CanonicalCutoverDomain.recordingMetadata)
            .ToList();

        switch (stage)
        {
            case CanonicalNoCommitMigrationStage.off:
            case CanonicalNoCommitMigrationStage.notStarted:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>(),
                    requiredEvidence: new List<CanonicalMigrationStageEvidence> { CanonicalMigrationStageEvidence.none },
                    allowedDomains: new List<CanonicalCutoverDomain>(),
                    forbiddenDomains: Enum.GetValues<CanonicalCutoverDomain>().ToList(),
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        "canonicalShadowMigrationConfiguration.disabled",
                        "canonicalSingleDomainShadowConfiguration.disabled",
                        "canonicalV8CutoverAppSeamConfiguration.disabled"
                    }
                );

            case CanonicalNoCommitMigrationStage.diagnosticsOnly:
            case CanonicalNoCommitMigrationStage.projected:
            case CanonicalNoCommitMigrationStage.planned:
            case CanonicalNoCommitMigrationStage.appSeamDefaultOff:
            case CanonicalNoCommitMigrationStage.nextPilotCandidate:
            case CanonicalNoCommitMigrationStage.canaryN0:
            case CanonicalNoCommitMigrationStage.readSideParallel:
            case CanonicalNoCommitMigrationStage.retirementCandidate:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.none
                    },
                    allowedDomains: Enum.GetValues<CanonicalCutoverDomain>().ToList(),
                    forbiddenDomains: new List<CanonicalCutoverDomain>(),
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        "canonicalMigrationMatrix.diagnosticsOnly"
                    }
                );

            case CanonicalNoCommitMigrationStage.noCommit:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite,
                        CanonicalMigrationStageSideEffect.stagingRootWrite
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.noCommitEvidenceReport
                    },
                    allowedDomains: Enum.GetValues<CanonicalCutoverDomain>().ToList(),
                    forbiddenDomains: new List<CanonicalCutoverDomain>(),
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        "canonicalMigrationMatrix.noCommit"
                    }
                );

            case CanonicalNoCommitMigrationStage.realApplyPort:
            case CanonicalNoCommitMigrationStage.commitExecutor:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.noCommitEvidenceReport,
                        CanonicalMigrationStageEvidence.rollbackPlan
                    },
                    allowedDomains: Enum.GetValues<CanonicalCutoverDomain>().ToList(),
                    forbiddenDomains: new List<CanonicalCutoverDomain>(),
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        $"canonicalMigrationMatrix.{stage}"
                    }
                );

            case CanonicalNoCommitMigrationStage.canaryN1:
            case CanonicalNoCommitMigrationStage.expandedCanary:
            case CanonicalNoCommitMigrationStage.domainCutover:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite,
                        CanonicalMigrationStageSideEffect.productionCommit
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.dryRunEquivalence,
                        CanonicalMigrationStageEvidence.executionShadow,
                        CanonicalMigrationStageEvidence.realDataShadowCopy,
                        CanonicalMigrationStageEvidence.readOnlyTransportProbe,
                        CanonicalMigrationStageEvidence.noCommitEvidenceReport,
                        CanonicalMigrationStageEvidence.ownerApproval,
                        CanonicalMigrationStageEvidence.rollbackPlan
                    },
                    allowedDomains: Enum.GetValues<CanonicalCutoverDomain>().ToList(),
                    forbiddenDomains: new List<CanonicalCutoverDomain>(),
                    productionCommitAllowed: true,
                    existingConfigurationKeys: new List<string>
                    {
                        $"canonicalMigrationMatrix.{stage}"
                    }
                );

            case CanonicalNoCommitMigrationStage.readSideCutover:
            case CanonicalNoCommitMigrationStage.retired:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.ownerApproval,
                        CanonicalMigrationStageEvidence.rollbackPlan
                    },
                    allowedDomains: Enum.GetValues<CanonicalCutoverDomain>().ToList(),
                    forbiddenDomains: new List<CanonicalCutoverDomain>(),
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        $"canonicalMigrationMatrix.{stage}"
                    }
                );

            case CanonicalNoCommitMigrationStage.decisionShadow:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.dryRunEquivalence
                    },
                    allowedDomains: Enum.GetValues<CanonicalCutoverDomain>().ToList(),
                    forbiddenDomains: new List<CanonicalCutoverDomain>(),
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        "canonicalShadowMigrationConfiguration.dryRunCompare"
                    }
                );

            case CanonicalNoCommitMigrationStage.executionShadow:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite,
                        CanonicalMigrationStageSideEffect.shadowRootWrite
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.dryRunEquivalence,
                        CanonicalMigrationStageEvidence.executionShadow
                    },
                    allowedDomains: recordingOnly,
                    forbiddenDomains: allExceptRecording,
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        "canonicalSingleDomainShadowConfiguration.executionShadowDryRun"
                    }
                );

            case CanonicalNoCommitMigrationStage.realDataShadowCopy:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite,
                        CanonicalMigrationStageSideEffect.shadowRootWrite
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.executionShadow,
                        CanonicalMigrationStageEvidence.realDataShadowCopy
                    },
                    allowedDomains: recordingOnly,
                    forbiddenDomains: allExceptRecording,
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        "canonicalShadowMigrationConfiguration.realDataShadowCopyPolicy"
                    }
                );

            case CanonicalNoCommitMigrationStage.readOnlyTransportProbe:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite,
                        CanonicalMigrationStageSideEffect.readOnlyNetworkProbe
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.realDataShadowCopy,
                        CanonicalMigrationStageEvidence.readOnlyTransportProbe
                    },
                    allowedDomains: recordingOnly,
                    forbiddenDomains: allExceptRecording,
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        "canonicalShadowMigrationConfiguration.readOnlyTransportProbePolicy",
                        "canonicalLiveReadOnlyTransportProbePolicy"
                    }
                );

            case CanonicalNoCommitMigrationStage.recordingMetadataNoCommit:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite,
                        CanonicalMigrationStageSideEffect.stagingRootWrite
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.realDataShadowCopy,
                        CanonicalMigrationStageEvidence.readOnlyTransportProbe,
                        CanonicalMigrationStageEvidence.noCommitEvidenceReport
                    },
                    allowedDomains: recordingOnly,
                    forbiddenDomains: allExceptRecording,
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>
                    {
                        "canonicalV8CutoverAppSeamConfiguration.guardedExecuteNoCommit"
                    }
                );

            case CanonicalNoCommitMigrationStage.recordingMetadataGuardedCommit:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>
                    {
                        CanonicalMigrationStageSideEffect.diagnosticsWrite,
                        CanonicalMigrationStageSideEffect.productionCommit
                    },
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>
                    {
                        CanonicalMigrationStageEvidence.dryRunEquivalence,
                        CanonicalMigrationStageEvidence.executionShadow,
                        CanonicalMigrationStageEvidence.realDataShadowCopy,
                        CanonicalMigrationStageEvidence.readOnlyTransportProbe,
                        CanonicalMigrationStageEvidence.noCommitEvidenceReport,
                        CanonicalMigrationStageEvidence.ownerApproval,
                        CanonicalMigrationStageEvidence.rollbackPlan
                    },
                    allowedDomains: recordingOnly,
                    forbiddenDomains: allExceptRecording,
                    productionCommitAllowed: true,
                    existingConfigurationKeys: new List<string>
                    {
                        "CanonicalSingleDomainCutoverConfiguration.guardedExecuteCommit"
                    }
                );

            case CanonicalNoCommitMigrationStage.unsupported:
            default:
                return new CanonicalMigrationStagePolicy(
                    allowedSideEffects: new List<CanonicalMigrationStageSideEffect>(),
                    requiredEvidence: new List<CanonicalMigrationStageEvidence>(),
                    allowedDomains: new List<CanonicalCutoverDomain>(),
                    forbiddenDomains: Enum.GetValues<CanonicalCutoverDomain>().ToList(),
                    productionCommitAllowed: false,
                    existingConfigurationKeys: new List<string>()
                );
        }
    }

    public override int GetHashCode() =>
        HashCode.Combine(AllowedSideEffects.Count, RequiredEvidence.Count, AllowedDomains.Count,
            ForbiddenDomains.Count, ProductionCommitAllowed, ExistingConfigurationKeys.Count);
    public override bool Equals(object? obj) => obj is CanonicalMigrationStagePolicy other && Equals(other);
    public bool Equals(CanonicalMigrationStagePolicy? other) =>
        other is not null && AllowedSideEffects.SequenceEqual(other.AllowedSideEffects) &&
        RequiredEvidence.SequenceEqual(other.RequiredEvidence) &&
        AllowedDomains.SequenceEqual(other.AllowedDomains) &&
        ForbiddenDomains.SequenceEqual(other.ForbiddenDomains) &&
        ProductionCommitAllowed == other.ProductionCommitAllowed &&
        ExistingConfigurationKeys.SequenceEqual(other.ExistingConfigurationKeys);
    public static bool operator ==(CanonicalMigrationStagePolicy left, CanonicalMigrationStagePolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalMigrationStagePolicy left, CanonicalMigrationStagePolicy right) => !left.Equals(right);
}

public sealed class CanonicalMigrationConfigurationSummary : IEquatable<CanonicalMigrationConfigurationSummary>
{
    public CanonicalNoCommitMigrationStage Stage { get; }
    public CanonicalCutoverDomain Domain { get; }
    public bool Allowed { get; }
    public List<string> Blockers { get; }
    public List<CanonicalMigrationStageSideEffect> AllowedSideEffects { get; }
    public List<CanonicalMigrationStageEvidence> RequiredEvidence { get; }
    public List<CanonicalCutoverDomain> AllowedDomains { get; }
    public List<CanonicalCutoverDomain> ForbiddenDomains { get; }
    public bool ProductionCommitAllowed { get; }
    public List<string> ExistingConfigurationKeys { get; }

    public CanonicalMigrationConfigurationSummary(
        CanonicalNoCommitMigrationStage stage,
        CanonicalCutoverDomain domain,
        bool allowed,
        List<string> blockers,
        CanonicalMigrationStagePolicy policy)
    {
        Stage = stage;
        Domain = domain;
        Allowed = allowed;
        Blockers = new HashSet<string>(
                blockers.Select(b => CanonicalProductionRedaction.SafeDiagnosticText(b) ?? b))
            .OrderBy(b => b).ToList();
        AllowedSideEffects = policy.AllowedSideEffects;
        RequiredEvidence = policy.RequiredEvidence;
        AllowedDomains = policy.AllowedDomains;
        ForbiddenDomains = policy.ForbiddenDomains;
        ProductionCommitAllowed = policy.ProductionCommitAllowed;
        ExistingConfigurationKeys = policy.ExistingConfigurationKeys;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"stage={Stage}",
        $"domain={Domain}",
        $"allowed={Allowed}",
        $"sideEffects={string.Join("+", AllowedSideEffects.Select(s => s.ToString()))}",
        $"requiredEvidence={string.Join("+", RequiredEvidence.Select(e => e.ToString()))}",
        $"productionCommitAllowed={ProductionCommitAllowed}",
        $"blockers={string.Join("+", Blockers)}");

    public override int GetHashCode() =>
        HashCode.Combine(Stage, Domain, Allowed, Blockers.Count, AllowedSideEffects.Count,
            RequiredEvidence.Count, AllowedDomains.Count, ForbiddenDomains.Count,
            ProductionCommitAllowed, ExistingConfigurationKeys.Count);
    public override bool Equals(object? obj) => obj is CanonicalMigrationConfigurationSummary other && Equals(other);
    public bool Equals(CanonicalMigrationConfigurationSummary? other) =>
        other is not null && Stage == other.Stage && Domain == other.Domain &&
        Allowed == other.Allowed && Blockers.SequenceEqual(other.Blockers) &&
        AllowedSideEffects.SequenceEqual(other.AllowedSideEffects) &&
        RequiredEvidence.SequenceEqual(other.RequiredEvidence) &&
        AllowedDomains.SequenceEqual(other.AllowedDomains) &&
        ForbiddenDomains.SequenceEqual(other.ForbiddenDomains) &&
        ProductionCommitAllowed == other.ProductionCommitAllowed &&
        ExistingConfigurationKeys.SequenceEqual(other.ExistingConfigurationKeys);
    public static bool operator ==(CanonicalMigrationConfigurationSummary left, CanonicalMigrationConfigurationSummary right) => left.Equals(right);
    public static bool operator !=(CanonicalMigrationConfigurationSummary left, CanonicalMigrationConfigurationSummary right) => !left.Equals(right);
}

public sealed class CanonicalMigrationStageConfiguration : IEquatable<CanonicalMigrationStageConfiguration>
{
    public CanonicalNoCommitMigrationStage Stage { get; }
    public CanonicalCutoverDomain Domain { get; }
    public CanonicalMigrationStagePolicy Policy { get; }

    public CanonicalMigrationStageConfiguration(
        CanonicalNoCommitMigrationStage stage = CanonicalNoCommitMigrationStage.off,
        CanonicalCutoverDomain domain = CanonicalCutoverDomain.recordingMetadata,
        CanonicalMigrationStagePolicy? policy = null)
    {
        Stage = stage;
        Domain = domain;
        Policy = policy ?? CanonicalMigrationStagePolicy.DefaultPolicy(stage);
    }

    public static readonly CanonicalMigrationStageConfiguration Off = new();

    public CanonicalMigrationConfigurationSummary Summary()
    {
        var blockers = new List<string>();

        if (Stage == CanonicalNoCommitMigrationStage.off)
            blockers.Add("stageOff");

        if (Stage == CanonicalNoCommitMigrationStage.unsupported)
            blockers.Add("unsupportedStage");

        if (Policy.AllowedDomains.Count > 0 && !Policy.AllowedDomains.Contains(Domain))
            blockers.Add("domainNotAllowed");

        if (Policy.ForbiddenDomains.Contains(Domain))
            blockers.Add("domainForbidden");

        var productionCommitStages = new HashSet<CanonicalNoCommitMigrationStage>
        {
            CanonicalNoCommitMigrationStage.recordingMetadataGuardedCommit,
            CanonicalNoCommitMigrationStage.canaryN1,
            CanonicalNoCommitMigrationStage.expandedCanary,
            CanonicalNoCommitMigrationStage.domainCutover
        };

        if (Policy.AllowedSideEffects.Contains(CanonicalMigrationStageSideEffect.productionCommit) &&
            !productionCommitStages.Contains(Stage))
            blockers.Add("illegalProductionCommitSideEffect");

        if (Policy.ProductionCommitAllowed !=
            Policy.AllowedSideEffects.Contains(CanonicalMigrationStageSideEffect.productionCommit))
            blockers.Add("productionCommitPolicyMismatch");

        return new CanonicalMigrationConfigurationSummary(
            stage: Stage,
            domain: Domain,
            allowed: blockers.Count == 0,
            blockers: blockers,
            policy: Policy
        );
    }

    public override int GetHashCode() => HashCode.Combine(Stage, Domain, Policy);
    public override bool Equals(object? obj) => obj is CanonicalMigrationStageConfiguration other && Equals(other);
    public bool Equals(CanonicalMigrationStageConfiguration? other) =>
        other is not null && Stage == other.Stage && Domain == other.Domain &&
        EqualityComparer<CanonicalMigrationStagePolicy>.Default.Equals(Policy, other.Policy);
    public static bool operator ==(CanonicalMigrationStageConfiguration left, CanonicalMigrationStageConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalMigrationStageConfiguration left, CanonicalMigrationStageConfiguration right) => !left.Equals(right);
}
