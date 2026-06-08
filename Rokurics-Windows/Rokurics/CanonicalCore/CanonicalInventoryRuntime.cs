using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalInventoryRuntimeNodeRole
{
    iPhone,
    mac
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalInventoryRuntimeSourceKind
{
    syncTick,
    inventoryRequest,
    artifactLookup,
    testHarness
}

public sealed class CanonicalInventoryRuntimeConfiguration : IEquatable<CanonicalInventoryRuntimeConfiguration>
{
    public const int CurrentSchemaVersion = 1;

    public int ChecksumSchemaVersion { get; }
    public string HashAlgorithm { get; }
    public string CacheFileName { get; }
    public bool RedactedDiagnostics { get; }
    public bool PersistentChecksumCacheEnabled { get; }

    public CanonicalInventoryRuntimeConfiguration(
        int checksumSchemaVersion = CurrentSchemaVersion,
        string hashAlgorithm = "sha256",
        string cacheFileName = "canonical-checksum-cache-v1.json",
        bool redactedDiagnostics = true,
        bool persistentChecksumCacheEnabled = true)
    {
        ChecksumSchemaVersion = checksumSchemaVersion;
        HashAlgorithm = hashAlgorithm;
        CacheFileName = cacheFileName;
        RedactedDiagnostics = redactedDiagnostics;
        PersistentChecksumCacheEnabled = persistentChecksumCacheEnabled;
    }

    public override bool Equals(object? obj) =>
        obj is CanonicalInventoryRuntimeConfiguration other && Equals(other);
    public bool Equals(CanonicalInventoryRuntimeConfiguration? other) =>
        other is not null &&
        ChecksumSchemaVersion == other.ChecksumSchemaVersion &&
        HashAlgorithm == other.HashAlgorithm &&
        CacheFileName == other.CacheFileName &&
        RedactedDiagnostics == other.RedactedDiagnostics &&
        PersistentChecksumCacheEnabled == other.PersistentChecksumCacheEnabled;
    public override int GetHashCode() =>
        HashCode.Combine(ChecksumSchemaVersion, HashAlgorithm, CacheFileName, RedactedDiagnostics, PersistentChecksumCacheEnabled);
    public static bool operator ==(CanonicalInventoryRuntimeConfiguration left, CanonicalInventoryRuntimeConfiguration right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalInventoryRuntimeConfiguration left, CanonicalInventoryRuntimeConfiguration right) =>
        !left.Equals(right);
}

public sealed class CanonicalInventoryRuntimeDiagnostics : IEquatable<CanonicalInventoryRuntimeDiagnostics>
{
    public int ChecksumCacheHitCount { get; set; }
    public int ChecksumCacheMissCount { get; set; }
    public int ChecksumCacheStaleCount { get; set; }
    public int ChecksumCacheErrorCount { get; set; }
    public int FileScanCount { get; set; }
    public int HashComputedCount { get; set; }
    public int MainActorHashBlockedCount { get; set; }
    public int MainActorScanBlockedCount { get; set; }
    public int DuplicateBuildCount { get; set; }
    public int ScanDurationMs { get; set; }
    public int HashDurationMs { get; set; }

    public CanonicalInventoryRuntimeDiagnostics(
        int checksumCacheHitCount = 0,
        int checksumCacheMissCount = 0,
        int checksumCacheStaleCount = 0,
        int checksumCacheErrorCount = 0,
        int fileScanCount = 0,
        int hashComputedCount = 0,
        int mainActorHashBlockedCount = 0,
        int mainActorScanBlockedCount = 0,
        int duplicateBuildCount = 0,
        int scanDurationMs = 0,
        int hashDurationMs = 0)
    {
        ChecksumCacheHitCount = checksumCacheHitCount;
        ChecksumCacheMissCount = checksumCacheMissCount;
        ChecksumCacheStaleCount = checksumCacheStaleCount;
        ChecksumCacheErrorCount = checksumCacheErrorCount;
        FileScanCount = fileScanCount;
        HashComputedCount = hashComputedCount;
        MainActorHashBlockedCount = mainActorHashBlockedCount;
        MainActorScanBlockedCount = mainActorScanBlockedCount;
        DuplicateBuildCount = duplicateBuildCount;
        ScanDurationMs = scanDurationMs;
        HashDurationMs = hashDurationMs;
    }

    public void Merge(CanonicalChecksumCacheResult result)
    {
        FileScanCount++;
        if (result.HashComputed)
            HashComputedCount++;
        switch (result.Event)
        {
            case CanonicalChecksumCacheEvent.hit:
                ChecksumCacheHitCount++;
                break;
            case CanonicalChecksumCacheEvent.miss:
                ChecksumCacheMissCount++;
                break;
            case CanonicalChecksumCacheEvent.stale:
                ChecksumCacheStaleCount++;
                break;
            case CanonicalChecksumCacheEvent.error:
                ChecksumCacheErrorCount++;
                break;
        }
        HashDurationMs += result.HashDurationMs;
    }

    public override bool Equals(object? obj) =>
        obj is CanonicalInventoryRuntimeDiagnostics other && Equals(other);
    public bool Equals(CanonicalInventoryRuntimeDiagnostics? other) =>
        other is not null &&
        ChecksumCacheHitCount == other.ChecksumCacheHitCount &&
        ChecksumCacheMissCount == other.ChecksumCacheMissCount &&
        ChecksumCacheStaleCount == other.ChecksumCacheStaleCount &&
        ChecksumCacheErrorCount == other.ChecksumCacheErrorCount &&
        FileScanCount == other.FileScanCount &&
        HashComputedCount == other.HashComputedCount &&
        MainActorHashBlockedCount == other.MainActorHashBlockedCount &&
        MainActorScanBlockedCount == other.MainActorScanBlockedCount &&
        DuplicateBuildCount == other.DuplicateBuildCount &&
        ScanDurationMs == other.ScanDurationMs &&
        HashDurationMs == other.HashDurationMs;
    public override int GetHashCode() =>
        HashCode.Combine(ChecksumCacheHitCount, ChecksumCacheMissCount, HashComputedCount);
    public static bool operator ==(CanonicalInventoryRuntimeDiagnostics left, CanonicalInventoryRuntimeDiagnostics right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalInventoryRuntimeDiagnostics left, CanonicalInventoryRuntimeDiagnostics right) =>
        !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalInventoryRuntimeFailure
{
    cacheCorrupted,
    fileMetadataUnavailable,
    hashUnavailable,
    cancelled,
    unknown
}

public sealed class CanonicalInventoryObjectCounts : IEquatable<CanonicalInventoryObjectCounts>
{
    public int RecordingMetadataCount { get; set; }
    public int LibraryFolderCount { get; set; }
    public int LibraryItemCount { get; set; }
    public int ArtifactCount { get; set; }
    public int AudioDescriptorCount { get; set; }

    public override bool Equals(object? obj) =>
        obj is CanonicalInventoryObjectCounts other && Equals(other);
    public bool Equals(CanonicalInventoryObjectCounts? other) =>
        other is not null &&
        RecordingMetadataCount == other.RecordingMetadataCount &&
        LibraryFolderCount == other.LibraryFolderCount &&
        LibraryItemCount == other.LibraryItemCount &&
        ArtifactCount == other.ArtifactCount &&
        AudioDescriptorCount == other.AudioDescriptorCount;
    public override int GetHashCode() =>
        HashCode.Combine(RecordingMetadataCount, LibraryFolderCount, LibraryItemCount, ArtifactCount, AudioDescriptorCount);
    public static bool operator ==(CanonicalInventoryObjectCounts left, CanonicalInventoryObjectCounts right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalInventoryObjectCounts left, CanonicalInventoryObjectCounts right) =>
        !left.Equals(right);
}

public sealed class CanonicalInventoryRuntimeSnapshot : IEquatable<CanonicalInventoryRuntimeSnapshot>
{
    public string SyncRunID { get; set; }
    public CanonicalInventoryRuntimeNodeRole NodeRole { get; set; }
    public DateTime BuildStartedAt { get; set; }
    public DateTime BuildEndedAt { get; set; }
    public CanonicalInventoryRuntimeSourceKind SourceKind { get; set; }
    public CanonicalInventoryObjectCounts ObjectCounts { get; set; } = new();
    public CanonicalInventoryRuntimeDiagnostics Diagnostics { get; set; } = new();
    public bool MainActorBlocked { get; set; }
    public bool ReusedWithinTick { get; set; }
    public bool Redacted { get; set; }

    public override bool Equals(object? obj) =>
        obj is CanonicalInventoryRuntimeSnapshot other && Equals(other);
    public bool Equals(CanonicalInventoryRuntimeSnapshot? other) =>
        other is not null &&
        SyncRunID == other.SyncRunID &&
        NodeRole == other.NodeRole &&
        BuildStartedAt == other.BuildStartedAt &&
        BuildEndedAt == other.BuildEndedAt &&
        SourceKind == other.SourceKind &&
        ObjectCounts.Equals(other.ObjectCounts) &&
        Diagnostics.Equals(other.Diagnostics) &&
        MainActorBlocked == other.MainActorBlocked &&
        ReusedWithinTick == other.ReusedWithinTick &&
        Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(SyncRunID, NodeRole, BuildStartedAt, SourceKind);
    public static bool operator ==(CanonicalInventoryRuntimeSnapshot left, CanonicalInventoryRuntimeSnapshot right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalInventoryRuntimeSnapshot left, CanonicalInventoryRuntimeSnapshot right) =>
        !left.Equals(right);
}

public sealed class CanonicalInventoryRuntimeResult : IEquatable<CanonicalInventoryRuntimeResult>
{
    public CanonicalInventoryRuntimeSnapshot Snapshot { get; set; } = new();
    public List<CanonicalInventoryRuntimeFailure> Failures { get; set; } = new();

    public override bool Equals(object? obj) =>
        obj is CanonicalInventoryRuntimeResult other && Equals(other);
    public bool Equals(CanonicalInventoryRuntimeResult? other) =>
        other is not null &&
        Snapshot.Equals(other.Snapshot) &&
        Failures.SequenceEqual(other.Failures);
    public override int GetHashCode() => HashCode.Combine(Snapshot, Failures.Count);
    public static bool operator ==(CanonicalInventoryRuntimeResult left, CanonicalInventoryRuntimeResult right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalInventoryRuntimeResult left, CanonicalInventoryRuntimeResult right) =>
        !left.Equals(right);
}

public class CanonicalInventoryRuntimeBuilder
{
    private readonly Dictionary<string, CanonicalInventoryRuntimeSnapshot> _snapshotsByScope = new();

    public CanonicalInventoryRuntimeSnapshot? ExistingSnapshot(
        string syncRunID,
        CanonicalInventoryRuntimeNodeRole nodeRole,
        CanonicalInventoryRuntimeSourceKind sourceKind)
    {
        var key = ScopeKey(syncRunID, nodeRole, sourceKind);
        return _snapshotsByScope.TryGetValue(key, out var snapshot) ? snapshot : null;
    }

    public void Remember(CanonicalInventoryRuntimeSnapshot snapshot)
    {
        var key = ScopeKey(snapshot.SyncRunID, snapshot.NodeRole, snapshot.SourceKind);
        _snapshotsByScope[key] = snapshot;
    }

    public CanonicalInventoryRuntimeSnapshot ReusedSnapshot(CanonicalInventoryRuntimeSnapshot snapshot)
    {
        return new CanonicalInventoryRuntimeSnapshot
        {
            SyncRunID = snapshot.SyncRunID,
            NodeRole = snapshot.NodeRole,
            BuildStartedAt = snapshot.BuildStartedAt,
            BuildEndedAt = snapshot.BuildEndedAt,
            SourceKind = snapshot.SourceKind,
            ObjectCounts = snapshot.ObjectCounts,
            Diagnostics = snapshot.Diagnostics,
            MainActorBlocked = snapshot.MainActorBlocked,
            ReusedWithinTick = true,
            Redacted = snapshot.Redacted
        };
    }

    public CanonicalInventoryRuntimeSnapshot DuplicateDetectedSnapshot(CanonicalInventoryRuntimeSnapshot snapshot)
    {
        var diagnostics = new CanonicalInventoryRuntimeDiagnostics
        {
            ChecksumCacheHitCount = snapshot.Diagnostics.ChecksumCacheHitCount,
            ChecksumCacheMissCount = snapshot.Diagnostics.ChecksumCacheMissCount,
            ChecksumCacheStaleCount = snapshot.Diagnostics.ChecksumCacheStaleCount,
            ChecksumCacheErrorCount = snapshot.Diagnostics.ChecksumCacheErrorCount,
            FileScanCount = snapshot.Diagnostics.FileScanCount,
            HashComputedCount = snapshot.Diagnostics.HashComputedCount,
            MainActorHashBlockedCount = snapshot.Diagnostics.MainActorHashBlockedCount,
            MainActorScanBlockedCount = snapshot.Diagnostics.MainActorScanBlockedCount,
            DuplicateBuildCount = snapshot.Diagnostics.DuplicateBuildCount + 1,
            ScanDurationMs = snapshot.Diagnostics.ScanDurationMs,
            HashDurationMs = snapshot.Diagnostics.HashDurationMs
        };
        return new CanonicalInventoryRuntimeSnapshot
        {
            SyncRunID = snapshot.SyncRunID,
            NodeRole = snapshot.NodeRole,
            BuildStartedAt = snapshot.BuildStartedAt,
            BuildEndedAt = snapshot.BuildEndedAt,
            SourceKind = snapshot.SourceKind,
            ObjectCounts = snapshot.ObjectCounts,
            Diagnostics = diagnostics,
            MainActorBlocked = snapshot.MainActorBlocked,
            ReusedWithinTick = snapshot.ReusedWithinTick,
            Redacted = snapshot.Redacted
        };
    }

    public void Reset()
    {
        _snapshotsByScope.Clear();
    }

    private static string ScopeKey(
        string syncRunID,
        CanonicalInventoryRuntimeNodeRole nodeRole,
        CanonicalInventoryRuntimeSourceKind sourceKind)
    {
        return $"{nodeRole.ToString()}|{sourceKind.ToString()}|{syncRunID}";
    }
}

public sealed class CanonicalInventoryRuntimeReport : IEquatable<CanonicalInventoryRuntimeReport>
{
    public string SyncRunID { get; set; } = "";
    public CanonicalInventoryRuntimeNodeRole NodeRole { get; set; }
    public int BuildDurationMs { get; set; }
    public int ScanDurationMs { get; set; }
    public int HashDurationMs { get; set; }
    public int CacheHitCount { get; set; }
    public int CacheMissCount { get; set; }
    public int CacheStaleCount { get; set; }
    public int DuplicateBuildCount { get; set; }
    public int MainActorHashBlockedCount { get; set; }
    public int MainActorScanBlockedCount { get; set; }
    public CanonicalInventoryObjectCounts InventoryObjectCounts { get; set; } = new();
    public bool Redacted { get; set; }

    public override bool Equals(object? obj) =>
        obj is CanonicalInventoryRuntimeReport other && Equals(other);
    public bool Equals(CanonicalInventoryRuntimeReport? other) =>
        other is not null &&
        SyncRunID == other.SyncRunID &&
        NodeRole == other.NodeRole &&
        BuildDurationMs == other.BuildDurationMs &&
        ScanDurationMs == other.ScanDurationMs &&
        HashDurationMs == other.HashDurationMs &&
        CacheHitCount == other.CacheHitCount &&
        CacheMissCount == other.CacheMissCount &&
        CacheStaleCount == other.CacheStaleCount &&
        DuplicateBuildCount == other.DuplicateBuildCount &&
        MainActorHashBlockedCount == other.MainActorHashBlockedCount &&
        MainActorScanBlockedCount == other.MainActorScanBlockedCount &&
        InventoryObjectCounts.Equals(other.InventoryObjectCounts) &&
        Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(SyncRunID, NodeRole, BuildDurationMs, CacheHitCount);
    public static bool operator ==(CanonicalInventoryRuntimeReport left, CanonicalInventoryRuntimeReport right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalInventoryRuntimeReport left, CanonicalInventoryRuntimeReport right) =>
        !left.Equals(right);
}

public static class CanonicalInventoryRuntimeReportExporter
{
    public static CanonicalInventoryRuntimeReport Report(CanonicalInventoryRuntimeSnapshot snapshot)
    {
        return new CanonicalInventoryRuntimeReport
        {
            SyncRunID = snapshot.SyncRunID,
            NodeRole = snapshot.NodeRole,
            BuildDurationMs = Math.Max(0, (int)((snapshot.BuildEndedAt - snapshot.BuildStartedAt).TotalMilliseconds)),
            ScanDurationMs = snapshot.Diagnostics.ScanDurationMs,
            HashDurationMs = snapshot.Diagnostics.HashDurationMs,
            CacheHitCount = snapshot.Diagnostics.ChecksumCacheHitCount,
            CacheMissCount = snapshot.Diagnostics.ChecksumCacheMissCount,
            CacheStaleCount = snapshot.Diagnostics.ChecksumCacheStaleCount,
            DuplicateBuildCount = snapshot.Diagnostics.DuplicateBuildCount,
            MainActorHashBlockedCount = snapshot.Diagnostics.MainActorHashBlockedCount,
            MainActorScanBlockedCount = snapshot.Diagnostics.MainActorScanBlockedCount,
            InventoryObjectCounts = snapshot.ObjectCounts,
            Redacted = snapshot.Redacted
        };
    }

    public static string DiagnosticsSummary(CanonicalInventoryRuntimeSnapshot snapshot)
    {
        var report = Report(snapshot);
        return string.Join(",",
            $"syncRunID={report.SyncRunID}",
            $"nodeRole={report.NodeRole.ToString()}",
            $"buildDurationMs={report.BuildDurationMs}",
            $"scanDurationMs={report.ScanDurationMs}",
            $"hashDurationMs={report.HashDurationMs}",
            $"cacheHitCount={report.CacheHitCount}",
            $"cacheMissCount={report.CacheMissCount}",
            $"cacheStaleCount={report.CacheStaleCount}",
            $"duplicateBuildCount={report.DuplicateBuildCount}",
            $"mainActorHashBlockedCount={report.MainActorHashBlockedCount}",
            $"mainActorScanBlockedCount={report.MainActorScanBlockedCount}",
            $"recordingMetadataCount={report.InventoryObjectCounts.RecordingMetadataCount}",
            $"libraryFolderCount={report.InventoryObjectCounts.LibraryFolderCount}",
            $"libraryItemCount={report.InventoryObjectCounts.LibraryItemCount}",
            $"artifactCount={report.InventoryObjectCounts.ArtifactCount}",
            $"audioDescriptorCount={report.InventoryObjectCounts.AudioDescriptorCount}",
            $"redacted={report.Redacted}");
    }

    public static byte[] JsonData(CanonicalInventoryRuntimeSnapshot snapshot)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        options.Converters.Add(new Iso8601DateTimeConverter());
        return JsonSerializer.SerializeToUtf8Bytes(Report(snapshot), options);
    }

    public static string JsonString(CanonicalInventoryRuntimeSnapshot snapshot)
    {
        var data = JsonData(snapshot);
        return Encoding.UTF8.GetString(data);
    }

    private sealed class Iso8601DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (DateTime.TryParse(value, out var result))
                return result;
            return DateTime.UtcNow;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("o"));
        }
    }
}

public readonly struct CanonicalChecksumCacheKey : IEquatable<CanonicalChecksumCacheKey>
{
    public string LogicalToken { get; }
    public long ByteSize { get; }
    public long ModifiedAtEpochMs { get; }
    public string HashAlgorithm { get; }
    public int SchemaVersion { get; }
    public CanonicalInventoryRuntimeNodeRole NodeRole { get; }

    public CanonicalChecksumCacheKey(
        string logicalToken,
        long byteSize,
        long modifiedAtEpochMs,
        string hashAlgorithm,
        int schemaVersion,
        CanonicalInventoryRuntimeNodeRole nodeRole)
    {
        LogicalToken = logicalToken;
        ByteSize = byteSize;
        ModifiedAtEpochMs = modifiedAtEpochMs;
        HashAlgorithm = hashAlgorithm;
        SchemaVersion = schemaVersion;
        NodeRole = nodeRole;
    }

    public override bool Equals(object? obj) =>
        obj is CanonicalChecksumCacheKey other && Equals(other);
    public bool Equals(CanonicalChecksumCacheKey other) =>
        LogicalToken == other.LogicalToken &&
        ByteSize == other.ByteSize &&
        ModifiedAtEpochMs == other.ModifiedAtEpochMs &&
        HashAlgorithm == other.HashAlgorithm &&
        SchemaVersion == other.SchemaVersion &&
        NodeRole == other.NodeRole;
    public override int GetHashCode() =>
        HashCode.Combine(LogicalToken, ByteSize, ModifiedAtEpochMs, HashAlgorithm, SchemaVersion, NodeRole);
    public static bool operator ==(CanonicalChecksumCacheKey left, CanonicalChecksumCacheKey right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalChecksumCacheKey left, CanonicalChecksumCacheKey right) =>
        !left.Equals(right);
}

public sealed class CanonicalChecksumCacheRecord : IEquatable<CanonicalChecksumCacheRecord>
{
    public CanonicalChecksumCacheKey Key { get; set; } = default!;
    public string Sha256 { get; set; } = "";
    public DateTime ComputedAt { get; set; }

    public override bool Equals(object? obj) =>
        obj is CanonicalChecksumCacheRecord other && Equals(other);
    public bool Equals(CanonicalChecksumCacheRecord? other) =>
        other is not null &&
        Key.Equals(other.Key) &&
        Sha256 == other.Sha256 &&
        ComputedAt == other.ComputedAt;
    public override int GetHashCode() => HashCode.Combine(Key, Sha256, ComputedAt);
    public static bool operator ==(CanonicalChecksumCacheRecord left, CanonicalChecksumCacheRecord right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalChecksumCacheRecord left, CanonicalChecksumCacheRecord right) =>
        !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalChecksumCacheEvent
{
    hit,
    miss,
    stale,
    error
}

public sealed class CanonicalChecksumCacheResult : IEquatable<CanonicalChecksumCacheResult>
{
    public string? Sha256 { get; set; }
    public long ByteSize { get; set; }
    public DateTime ModifiedAt { get; set; }
    public CanonicalChecksumCacheEvent Event { get; set; }
    public bool HashComputed { get; set; }
    public bool HashUnavailable { get; set; }
    public CanonicalInventoryRuntimeFailure? Failure { get; set; }
    public int HashDurationMs { get; set; }

    public string? RedactedHashPrefix => Sha256?.Length >= 12 ? Sha256[..12] : Sha256;

    public override bool Equals(object? obj) =>
        obj is CanonicalChecksumCacheResult other && Equals(other);
    public bool Equals(CanonicalChecksumCacheResult? other) =>
        other is not null &&
        Sha256 == other.Sha256 &&
        ByteSize == other.ByteSize &&
        ModifiedAt == other.ModifiedAt &&
        Event == other.Event &&
        HashComputed == other.HashComputed &&
        HashUnavailable == other.HashUnavailable &&
        Failure == other.Failure &&
        HashDurationMs == other.HashDurationMs;
    public override int GetHashCode() =>
        HashCode.Combine(Sha256, ByteSize, ModifiedAt, Event, HashComputed);
    public static bool operator ==(CanonicalChecksumCacheResult left, CanonicalChecksumCacheResult right) =>
        left.Equals(right);
    public static bool operator !=(CanonicalChecksumCacheResult left, CanonicalChecksumCacheResult right) =>
        !left.Equals(right);
}

public class CanonicalChecksumCacheStore
{
    private sealed class CacheFile
    {
        public int SchemaVersion { get; set; }
        public List<CanonicalChecksumCacheRecord> Records { get; set; } = new();
    }

    private string? _loadedURL;
    private readonly Dictionary<string, CanonicalChecksumCacheRecord> _recordsByToken = new();
    private bool _cacheCorrupted;

    public async Task<CanonicalChecksumCacheResult> ChecksumAsync(
        string fileURL,
        string? logicalToken,
        CanonicalInventoryRuntimeNodeRole nodeRole,
        string cacheDirectoryURL,
        CanonicalInventoryRuntimeConfiguration? configuration = null,
        DateTime? now = null)
    {
        var config = configuration ?? new CanonicalInventoryRuntimeConfiguration();
        var currentNow = now ?? DateTime.UtcNow;
        var standardizedURL = fileURL;

        var metadata = FileMetadata(standardizedURL);
        if (metadata == null)
        {
            return new CanonicalChecksumCacheResult
            {
                Sha256 = null,
                ByteSize = 0,
                ModifiedAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Event = CanonicalChecksumCacheEvent.error,
                HashComputed = false,
                HashUnavailable = true,
                Failure = CanonicalInventoryRuntimeFailure.fileMetadataUnavailable,
                HashDurationMs = 0
            };
        }

        var safeToken = SafeLogicalToken(logicalToken)
                        ?? SafeLogicalToken(Path.GetFileName(standardizedURL))
                        ?? "unknown-file";
        var key = new CanonicalChecksumCacheKey(
            logicalToken: safeToken,
            byteSize: metadata.Value.Size,
            modifiedAtEpochMs: EpochMs(metadata.Value.ModifiedAt),
            hashAlgorithm: config.HashAlgorithm,
            schemaVersion: config.ChecksumSchemaVersion,
            nodeRole: nodeRole);

        var cacheURL = Path.Combine(cacheDirectoryURL, config.CacheFileName);
        await LoadIfNeededAsync(cacheURL, config.ChecksumSchemaVersion);

        if (config.PersistentChecksumCacheEnabled
            && _recordsByToken.TryGetValue(safeToken, out var existing)
            && existing.Key.Equals(key))
        {
            return new CanonicalChecksumCacheResult
            {
                Sha256 = existing.Sha256,
                ByteSize = key.ByteSize,
                ModifiedAt = metadata.Value.ModifiedAt,
                Event = CanonicalChecksumCacheEvent.hit,
                HashComputed = false,
                HashUnavailable = false,
                Failure = null,
                HashDurationMs = 0
            };
        }

        var cacheEvent = _recordsByToken.ContainsKey(safeToken)
            ? CanonicalChecksumCacheEvent.stale
            : CanonicalChecksumCacheEvent.miss;
        var hashStartedAt = DateTime.UtcNow;

        try
        {
            var sha256 = await Sha256HexAsync(standardizedURL);
            var hashDurationMs = Math.Max(0, (int)((DateTime.UtcNow - hashStartedAt).TotalMilliseconds));
            var record = new CanonicalChecksumCacheRecord
            {
                Key = key,
                Sha256 = sha256,
                ComputedAt = currentNow
            };
            if (config.PersistentChecksumCacheEnabled)
            {
                _recordsByToken[safeToken] = record;
                TryPersist(cacheURL, config.ChecksumSchemaVersion);
            }
            return new CanonicalChecksumCacheResult
            {
                Sha256 = sha256,
                ByteSize = key.ByteSize,
                ModifiedAt = metadata.Value.ModifiedAt,
                Event = cacheEvent,
                HashComputed = true,
                HashUnavailable = false,
                Failure = _cacheCorrupted ? CanonicalInventoryRuntimeFailure.cacheCorrupted : null,
                HashDurationMs = hashDurationMs
            };
        }
        catch (OperationCanceledException)
        {
            return new CanonicalChecksumCacheResult
            {
                Sha256 = null,
                ByteSize = key.ByteSize,
                ModifiedAt = metadata.Value.ModifiedAt,
                Event = CanonicalChecksumCacheEvent.error,
                HashComputed = false,
                HashUnavailable = true,
                Failure = CanonicalInventoryRuntimeFailure.cancelled,
                HashDurationMs = Math.Max(0, (int)((DateTime.UtcNow - hashStartedAt).TotalMilliseconds))
            };
        }
        catch
        {
            return new CanonicalChecksumCacheResult
            {
                Sha256 = null,
                ByteSize = key.ByteSize,
                ModifiedAt = metadata.Value.ModifiedAt,
                Event = CanonicalChecksumCacheEvent.error,
                HashComputed = false,
                HashUnavailable = true,
                Failure = CanonicalInventoryRuntimeFailure.hashUnavailable,
                HashDurationMs = Math.Max(0, (int)((DateTime.UtcNow - hashStartedAt).TotalMilliseconds))
            };
        }
    }

    public void Reset(string cacheDirectoryURL, CanonicalInventoryRuntimeConfiguration? configuration = null)
    {
        var config = configuration ?? new CanonicalInventoryRuntimeConfiguration();
        var cacheURL = Path.Combine(cacheDirectoryURL, config.CacheFileName);
        _recordsByToken.Clear();
        _loadedURL = cacheURL;
        _cacheCorrupted = false;
        try { File.Delete(cacheURL); } catch { }
    }

    private Task LoadIfNeededAsync(string cacheURL, int schemaVersion)
    {
        if (_loadedURL == cacheURL)
            return Task.CompletedTask;

        _loadedURL = cacheURL;
        _recordsByToken.Clear();
        _cacheCorrupted = false;

        if (!File.Exists(cacheURL))
            return Task.CompletedTask;

        try
        {
            var data = File.ReadAllText(cacheURL);
            var options = new JsonSerializerOptions();
            var decoded = JsonSerializer.Deserialize<CacheFile>(data, options);
            if (decoded == null || decoded.SchemaVersion != schemaVersion)
            {
                _cacheCorrupted = true;
                return Task.CompletedTask;
            }
            _recordsByToken.Clear();
            foreach (var record in decoded.Records)
            {
                _recordsByToken[record.Key.LogicalToken] = record;
            }
        }
        catch
        {
            _cacheCorrupted = true;
            _recordsByToken.Clear();
        }

        return Task.CompletedTask;
    }

    private void TryPersist(string cacheURL, int schemaVersion)
    {
        try
        {
            var dir = Path.GetDirectoryName(cacheURL);
            if (dir != null)
                Directory.CreateDirectory(dir);
            var payload = new CacheFile
            {
                SchemaVersion = schemaVersion,
                Records = new List<CanonicalChecksumCacheRecord>(
                    _recordsByToken.Values.OrderBy(r => r.Key.LogicalToken, StringComparer.Ordinal))
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(payload, options);
            var tempPath = cacheURL + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, cacheURL, overwrite: true);
        }
        catch { }
    }

    private static async Task<string> Sha256HexAsync(string fileURL)
    {
        return await Task.Run(() =>
        {
            using var sha256 = SHA256.Create();
            using var stream = new FileStream(fileURL, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
            var hash = sha256.ComputeHash(stream);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        });
    }

    private static (long Size, DateTime ModifiedAt)? FileMetadata(string url)
    {
        try
        {
            var info = new FileInfo(url);
            if (!info.Exists)
                return null;
            return (info.Length, info.LastWriteTimeUtc);
        }
        catch
        {
            return null;
        }
    }

    private static string? SafeLogicalToken(string? token)
    {
        var trimmed = token?.Trim();
        if (string.IsNullOrEmpty(trimmed)
            || trimmed!.StartsWith('/')
            || trimmed.Contains("://")
            || trimmed.Contains('\\'))
            return null;
        var components = trimmed.Split('/', StringSplitOptions.None);
        if (components.Length == 0
            || components.Any(c => string.IsNullOrEmpty(c) || c == "." || c == ".."))
            return null;
        return trimmed;
    }

    private static long EpochMs(DateTime date)
    {
        return (long)Math.Round((date.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds);
    }
}
