using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── CanonicalRootToken ──────────────────────────────────────────────────────

public readonly struct CanonicalRootToken : IEquatable<CanonicalRootToken>
{
    public string RawValue { get; }

    public CanonicalRootToken(string rawValue)
    {
        RawValue = rawValue.Trim().NilIfEmpty() ?? "root:unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalRootToken other && Equals(other);
    public bool Equals(CanonicalRootToken other) => RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();
    public override string ToString() => RawValue;
    public static bool operator ==(CanonicalRootToken left, CanonicalRootToken right) => left.Equals(right);
    public static bool operator !=(CanonicalRootToken left, CanonicalRootToken right) => !left.Equals(right);
}

// ─── CanonicalFilePurpose ────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFilePurpose
{
    artifactBytes,
    generatedArtifact,
    metadataBlob,
    tombstoneMarker
}

// ─── CanonicalFileReference / CanonicalFileHandle ────────────────────────────

public sealed class CanonicalFileReference : IEquatable<CanonicalFileReference>
{
    public CanonicalRootToken RootToken { get; }
    public string LogicalPathToken { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }

    public CanonicalFileReference(
        CanonicalRootToken rootToken,
        string logicalPathToken,
        string? artifactID = null,
        CanonicalArtifact.Kind? artifactKind = null)
    {
        RootToken = rootToken;
        LogicalPathToken = logicalPathToken.Trim();
        ArtifactID = artifactID?.Trim().NilIfEmpty();
        ArtifactKind = artifactKind;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileReference other && Equals(other);
    public bool Equals(CanonicalFileReference? other) =>
        other is not null &&
        RootToken.Equals(other.RootToken) &&
        LogicalPathToken == other.LogicalPathToken &&
        ArtifactID == other.ArtifactID &&
        ArtifactKind == other.ArtifactKind;
    public override int GetHashCode() => HashCode.Combine(RootToken, LogicalPathToken, ArtifactID, ArtifactKind);
    public static bool operator ==(CanonicalFileReference left, CanonicalFileReference right) => left.Equals(right);
    public static bool operator !=(CanonicalFileReference left, CanonicalFileReference right) => !left.Equals(right);
}

// Typealias: CanonicalFileHandle = CanonicalFileReference (same type in C#)

// ─── CanonicalAtomicWritePolicy ──────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalAtomicWritePolicy
{
    atomicReplace,
    directInMemoryReplace
}

// ─── CanonicalFileHashPolicy ─────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFileHashPolicy
{
    sha256,
    none
}

// ─── CanonicalFileConflictPolicy ─────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFileConflictPolicy
{
    noOverwrite,
    replace,
    replaceIfExistingHashMatches,
    idempotentIfSameContent
}

// ─── CanonicalMetadataBlob ───────────────────────────────────────────────────

public sealed class CanonicalMetadataBlob : IEquatable<CanonicalMetadataBlob>
{
    public Dictionary<string, string> Fields { get; }

    public CanonicalMetadataBlob(Dictionary<string, string>? fields = null)
    {
        Fields = new Dictionary<string, string>();
        if (fields != null)
        {
            foreach (var kv in fields)
            {
                var key = kv.Key.Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    Fields[key] = kv.Value;
                }
            }
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalMetadataBlob other && Equals(other);
    public bool Equals(CanonicalMetadataBlob? other)
    {
        if (other is null) return false;
        if (Fields.Count != other.Fields.Count) return false;
        foreach (var kv in Fields)
        {
            if (!other.Fields.TryGetValue(kv.Key, out var otherValue) || kv.Value != otherValue)
                return false;
        }
        return true;
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kv in Fields.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            hash.Add(kv.Key);
            hash.Add(kv.Value);
        }
        return hash.ToHashCode();
    }
    public static bool operator ==(CanonicalMetadataBlob left, CanonicalMetadataBlob right) => left.Equals(right);
    public static bool operator !=(CanonicalMetadataBlob left, CanonicalMetadataBlob right) => !left.Equals(right);
}

// ─── CanonicalFileWriteIntent ────────────────────────────────────────────────

public sealed class CanonicalFileWriteIntent : IEquatable<CanonicalFileWriteIntent>
{
    public CanonicalFileReference Reference { get; }
    public byte[] Bytes { get; }
    public CanonicalFilePurpose Purpose { get; }
    public CanonicalHash? ExpectedContentHash { get; }
    public long? ExpectedByteSize { get; }
    public CanonicalHash? ExpectedExistingHash { get; }
    public CanonicalAtomicWritePolicy AtomicPolicy { get; }
    public CanonicalFileHashPolicy HashPolicy { get; }
    public CanonicalFileConflictPolicy ConflictPolicy { get; }
    public CanonicalMetadataBlob? MetadataBlob { get; }

    public CanonicalFileWriteIntent(
        CanonicalFileReference reference,
        byte[] bytes,
        CanonicalFilePurpose purpose = CanonicalFilePurpose.artifactBytes,
        CanonicalHash? expectedContentHash = null,
        long? expectedByteSize = null,
        CanonicalHash? expectedExistingHash = null,
        CanonicalAtomicWritePolicy atomicPolicy = CanonicalAtomicWritePolicy.atomicReplace,
        CanonicalFileHashPolicy hashPolicy = CanonicalFileHashPolicy.sha256,
        CanonicalFileConflictPolicy conflictPolicy = CanonicalFileConflictPolicy.noOverwrite,
        CanonicalMetadataBlob? metadataBlob = null)
    {
        Reference = reference;
        Bytes = bytes;
        Purpose = purpose;
        ExpectedContentHash = expectedContentHash;
        ExpectedByteSize = expectedByteSize;
        ExpectedExistingHash = expectedExistingHash;
        AtomicPolicy = atomicPolicy;
        HashPolicy = hashPolicy;
        ConflictPolicy = conflictPolicy;
        MetadataBlob = metadataBlob;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileWriteIntent other && Equals(other);
    public bool Equals(CanonicalFileWriteIntent? other) =>
        other is not null &&
        Reference.Equals(other.Reference) &&
        Bytes.SequenceEqual(other.Bytes) &&
        Purpose == other.Purpose &&
        Nullable.Equals(ExpectedContentHash, other.ExpectedContentHash) &&
        ExpectedByteSize == other.ExpectedByteSize &&
        Nullable.Equals(ExpectedExistingHash, other.ExpectedExistingHash) &&
        AtomicPolicy == other.AtomicPolicy &&
        HashPolicy == other.HashPolicy &&
        ConflictPolicy == other.ConflictPolicy &&
        Nullable.Equals(MetadataBlob, other.MetadataBlob);
    public override int GetHashCode() => HashCode.Combine(Reference, Purpose, ExpectedContentHash, ExpectedByteSize, AtomicPolicy, HashPolicy, ConflictPolicy);
    public static bool operator ==(CanonicalFileWriteIntent left, CanonicalFileWriteIntent right) => left.Equals(right);
    public static bool operator !=(CanonicalFileWriteIntent left, CanonicalFileWriteIntent right) => !left.Equals(right);
}

// ─── CanonicalFileReadRequest ────────────────────────────────────────────────

public sealed class CanonicalFileReadRequest : IEquatable<CanonicalFileReadRequest>
{
    public CanonicalFileReference Reference { get; }
    public bool AllowTombstonedRead { get; }

    public CanonicalFileReadRequest(CanonicalFileReference reference, bool allowTombstonedRead = false)
    {
        Reference = reference;
        AllowTombstonedRead = allowTombstonedRead;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileReadRequest other && Equals(other);
    public bool Equals(CanonicalFileReadRequest? other) =>
        other is not null &&
        Reference.Equals(other.Reference) &&
        AllowTombstonedRead == other.AllowTombstonedRead;
    public override int GetHashCode() => HashCode.Combine(Reference, AllowTombstonedRead);
    public static bool operator ==(CanonicalFileReadRequest left, CanonicalFileReadRequest right) => left.Equals(right);
    public static bool operator !=(CanonicalFileReadRequest left, CanonicalFileReadRequest right) => !left.Equals(right);
}

// ─── CanonicalFileWriteDisposition ───────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFileWriteDisposition
{
    created,
    replaced,
    acceptedExisting,
    tombstoneMarked
}

// ─── CanonicalPathResolutionResult ───────────────────────────────────────────

public sealed class CanonicalPathResolutionResult : IEquatable<CanonicalPathResolutionResult>
{
    public CanonicalRootToken RootToken { get; }
    public string LogicalPathToken { get; }
    public string ResolvedPathToken { get; }
    public bool IsInsideRoot { get; }

    public CanonicalPathResolutionResult(
        CanonicalRootToken rootToken,
        string logicalPathToken,
        string resolvedPathToken,
        bool isInsideRoot)
    {
        RootToken = rootToken;
        LogicalPathToken = logicalPathToken;
        ResolvedPathToken = resolvedPathToken;
        IsInsideRoot = isInsideRoot;
    }

    public override bool Equals(object? obj) => obj is CanonicalPathResolutionResult other && Equals(other);
    public bool Equals(CanonicalPathResolutionResult? other) =>
        other is not null &&
        RootToken.Equals(other.RootToken) &&
        LogicalPathToken == other.LogicalPathToken &&
        ResolvedPathToken == other.ResolvedPathToken &&
        IsInsideRoot == other.IsInsideRoot;
    public override int GetHashCode() => HashCode.Combine(RootToken, LogicalPathToken, ResolvedPathToken, IsInsideRoot);
    public static bool operator ==(CanonicalPathResolutionResult left, CanonicalPathResolutionResult right) => left.Equals(right);
    public static bool operator !=(CanonicalPathResolutionResult left, CanonicalPathResolutionResult right) => !left.Equals(right);
}

// ─── CanonicalFileWriteResult ────────────────────────────────────────────────

public sealed class CanonicalFileWriteResult : IEquatable<CanonicalFileWriteResult>
{
    public CanonicalFileReference Handle { get; }
    public CanonicalPathResolutionResult Resolution { get; }
    public long ByteSize { get; }
    public CanonicalHash? ContentHash { get; }
    public CanonicalFileWriteDisposition Disposition { get; }
    public CanonicalFilePurpose Purpose { get; }
    public bool Tombstoned { get; }

    public CanonicalFileWriteResult(
        CanonicalFileReference handle,
        CanonicalPathResolutionResult resolution,
        long byteSize,
        CanonicalHash? contentHash,
        CanonicalFileWriteDisposition disposition,
        CanonicalFilePurpose purpose,
        bool tombstoned)
    {
        Handle = handle;
        Resolution = resolution;
        ByteSize = byteSize;
        ContentHash = contentHash;
        Disposition = disposition;
        Purpose = purpose;
        Tombstoned = tombstoned;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileWriteResult other && Equals(other);
    public bool Equals(CanonicalFileWriteResult? other) =>
        other is not null &&
        Handle.Equals(other.Handle) &&
        Resolution.Equals(other.Resolution) &&
        ByteSize == other.ByteSize &&
        Nullable.Equals(ContentHash, other.ContentHash) &&
        Disposition == other.Disposition &&
        Purpose == other.Purpose &&
        Tombstoned == other.Tombstoned;
    public override int GetHashCode() => HashCode.Combine(Handle, Resolution, ByteSize, ContentHash, Disposition, Purpose, Tombstoned);
    public static bool operator ==(CanonicalFileWriteResult left, CanonicalFileWriteResult right) => left.Equals(right);
    public static bool operator !=(CanonicalFileWriteResult left, CanonicalFileWriteResult right) => !left.Equals(right);
}

// ─── CanonicalFileReadResult ─────────────────────────────────────────────────

public sealed class CanonicalFileReadResult : IEquatable<CanonicalFileReadResult>
{
    public CanonicalFileReference Handle { get; }
    public CanonicalPathResolutionResult Resolution { get; }
    public byte[] Bytes { get; }
    public long ByteSize { get; }
    public CanonicalHash? ContentHash { get; }
    public CanonicalFilePurpose Purpose { get; }
    public CanonicalMetadataBlob? MetadataBlob { get; }
    public bool Tombstoned { get; }
    public string? TombstoneReason { get; }

    public CanonicalFileReadResult(
        CanonicalFileReference handle,
        CanonicalPathResolutionResult resolution,
        byte[] bytes,
        long byteSize,
        CanonicalHash? contentHash,
        CanonicalFilePurpose purpose,
        CanonicalMetadataBlob? metadataBlob,
        bool tombstoned,
        string? tombstoneReason)
    {
        Handle = handle;
        Resolution = resolution;
        Bytes = bytes;
        ByteSize = byteSize;
        ContentHash = contentHash;
        Purpose = purpose;
        MetadataBlob = metadataBlob;
        Tombstoned = tombstoned;
        TombstoneReason = tombstoneReason;
    }

    public override bool Equals(object? obj) => obj is CanonicalFileReadResult other && Equals(other);
    public bool Equals(CanonicalFileReadResult? other) =>
        other is not null &&
        Handle.Equals(other.Handle) &&
        Resolution.Equals(other.Resolution) &&
        Bytes.SequenceEqual(other.Bytes) &&
        ByteSize == other.ByteSize &&
        Nullable.Equals(ContentHash, other.ContentHash) &&
        Purpose == other.Purpose &&
        Nullable.Equals(MetadataBlob, other.MetadataBlob) &&
        Tombstoned == other.Tombstoned &&
        TombstoneReason == other.TombstoneReason;
    public override int GetHashCode() => HashCode.Combine(Handle, Resolution, ByteSize, ContentHash, Purpose, Tombstoned);
    public static bool operator ==(CanonicalFileReadResult left, CanonicalFileReadResult right) => left.Equals(right);
    public static bool operator !=(CanonicalFileReadResult left, CanonicalFileReadResult right) => !left.Equals(right);
}

// ─── CanonicalFileRuntimeError ───────────────────────────────────────────────

public sealed class CanonicalFileRuntimeError : Exception, IEquatable<CanonicalFileRuntimeError>
{
    public string ErrorKind { get; }
    public string? Detail { get; }
    public string? Expected { get; }
    public string? Actual { get; }
    public long? ExpectedSize { get; }
    public long? ActualSize { get; }

    private CanonicalFileRuntimeError(
        string errorKind,
        string? detail = null,
        string? expected = null,
        string? actual = null,
        long? expectedSize = null,
        long? actualSize = null)
        : base(detail ?? errorKind)
    {
        ErrorKind = errorKind;
        Detail = detail;
        Expected = expected;
        Actual = actual;
        ExpectedSize = expectedSize;
        ActualSize = actualSize;
    }

    public static CanonicalFileRuntimeError RootNotBound(string rootValue) =>
        new("rootNotBound", rootValue);

    public static CanonicalFileRuntimeError InvalidPathToken(string token) =>
        new("invalidPathToken", token);

    public static CanonicalFileRuntimeError PathTraversalRejected(string token) =>
        new("pathTraversalRejected", token);

    public static CanonicalFileRuntimeError AbsolutePathRejected(string token) =>
        new("absolutePathRejected", token);

    public static CanonicalFileRuntimeError SchemeUrlRejected(string token) =>
        new("schemeURLRejected", token);

    public static CanonicalFileRuntimeError BackslashTraversalRejected(string token) =>
        new("backslashTraversalRejected", token);

    public static CanonicalFileRuntimeError RootEscapeRejected(string token) =>
        new("rootEscapeRejected", token);

    public static CanonicalFileRuntimeError FileNotFound(string pathToken) =>
        new("fileNotFound", pathToken);

    public static CanonicalFileRuntimeError Tombstoned(string pathToken) =>
        new("tombstoned", pathToken);

    public static CanonicalFileRuntimeError Conflict(string pathToken) =>
        new("conflict", pathToken);

    public static CanonicalFileRuntimeError PreWriteHashMismatch(string expected, string actual) =>
        new("preWriteHashMismatch", expected: expected, actual: actual);

    public static CanonicalFileRuntimeError PreWriteSizeMismatch(long expected, long actual) =>
        new("preWriteSizeMismatch", expectedSize: expected, actualSize: actual);

    public static CanonicalFileRuntimeError PostWriteHashMismatch(string expected, string actual) =>
        new("postWriteHashMismatch", expected: expected, actual: actual);

    public static CanonicalFileRuntimeError PostWriteSizeMismatch(long expected, long actual) =>
        new("postWriteSizeMismatch", expectedSize: expected, actualSize: actual);

    public static CanonicalFileRuntimeError ExistingHashMismatch(string expected, string? actual) =>
        new("existingHashMismatch", expected: expected, actual: actual);

    public static CanonicalFileRuntimeError UnsupportedHashPolicy(string policy) =>
        new("unsupportedHashPolicy", policy);

    public override bool Equals(object? obj) => obj is CanonicalFileRuntimeError other && Equals(other);
    public bool Equals(CanonicalFileRuntimeError? other) =>
        other is not null &&
        ErrorKind == other.ErrorKind &&
        Detail == other.Detail &&
        Expected == other.Expected &&
        Actual == other.Actual &&
        ExpectedSize == other.ExpectedSize &&
        ActualSize == other.ActualSize;
    public override int GetHashCode() => HashCode.Combine(ErrorKind, Detail, Expected, Actual, ExpectedSize, ActualSize);
    public static bool operator ==(CanonicalFileRuntimeError left, CanonicalFileRuntimeError right) => left.Equals(right);
    public static bool operator !=(CanonicalFileRuntimeError left, CanonicalFileRuntimeError right) => !left.Equals(right);
}

// ─── CanonicalPathResolver (protocol → interface) ────────────────────────────

public interface ICanonicalPathResolver
{
    CanonicalPathResolutionResult Resolve(CanonicalRootToken rootToken, string logicalPathToken);
}

// ─── CanonicalInMemoryPathResolver ───────────────────────────────────────────

public sealed class CanonicalInMemoryPathResolver : ICanonicalPathResolver
{
    private readonly Dictionary<CanonicalRootToken, string> _rootBindings;

    public CanonicalInMemoryPathResolver(Dictionary<CanonicalRootToken, string> rootBindings)
    {
        _rootBindings = new Dictionary<CanonicalRootToken, string>(rootBindings);
    }

    public CanonicalPathResolutionResult Resolve(CanonicalRootToken rootToken, string logicalPathToken)
    {
        if (!_rootBindings.TryGetValue(rootToken, out var rawRoot))
            throw CanonicalFileRuntimeError.RootNotBound(rootToken.RawValue);

        var rootPath = ValidatedRootPath(rawRoot);
        var safeToken = ValidatedLogicalPath(logicalPathToken);
        var resolved = $"{rootPath}/{safeToken}";

        if (!resolved.StartsWith($"{rootPath}/", StringComparison.Ordinal))
            throw CanonicalFileRuntimeError.RootEscapeRejected(logicalPathToken);

        return new CanonicalPathResolutionResult(
            rootToken: rootToken,
            logicalPathToken: safeToken,
            resolvedPathToken: resolved,
            isInsideRoot: true
        );
    }

    private static string ValidatedRootPath(string token)
    {
        var trimmed = token.Trim();
        if (trimmed.Length == 0)
            throw CanonicalFileRuntimeError.RootEscapeRejected(token);
        if (trimmed.StartsWith('/'))
            throw CanonicalFileRuntimeError.RootEscapeRejected(token);
        if (trimmed.Contains("://", StringComparison.Ordinal))
            throw CanonicalFileRuntimeError.RootEscapeRejected(token);
        if (trimmed.Contains('\\'))
            throw CanonicalFileRuntimeError.RootEscapeRejected(token);

        var components = trimmed.Split('/', StringSplitOptions.None);
        if (components.Any(c => c.Length == 0 || c == "." || c == ".."))
            throw CanonicalFileRuntimeError.RootEscapeRejected(token);

        return trimmed;
    }

    private static string ValidatedLogicalPath(string token)
    {
        var trimmed = token.Trim();
        if (trimmed.Length == 0)
            throw CanonicalFileRuntimeError.InvalidPathToken(token);
        if (trimmed.StartsWith('/'))
            throw CanonicalFileRuntimeError.AbsolutePathRejected(token);
        if (trimmed.Contains("://", StringComparison.Ordinal))
            throw CanonicalFileRuntimeError.SchemeUrlRejected(token);
        if (trimmed.Contains('\\'))
            throw CanonicalFileRuntimeError.BackslashTraversalRejected(token);

        var components = trimmed.Split('/', StringSplitOptions.None);
        if (components.Any(c => c.Length == 0 || c == "." || c == ".."))
            throw CanonicalFileRuntimeError.PathTraversalRejected(token);

        if (CanonicalProjectionContract.SafeLogicalPathToken(trimmed) == null)
            throw CanonicalFileRuntimeError.InvalidPathToken(token);

        return trimmed;
    }
}

// ─── ICanonicalFileStorePort (protocol → interface) ──────────────────────────

public interface ICanonicalFileStorePort
{
    Task<CanonicalPathResolutionResult> ResolveAsync(CanonicalFileReference reference);
    Task<CanonicalFileReadResult> ReadAsync(CanonicalFileReadRequest request);
    Task<CanonicalFileWriteResult> WriteAsync(CanonicalFileWriteIntent intent);
    Task<CanonicalFileWriteResult> MarkTombstoneAsync(CanonicalFileReference reference, string? reason);
    Task<bool> ContainsAsync(CanonicalFileReference reference);
}

// ─── InMemoryCanonicalFileStore ──────────────────────────────────────────────

public sealed class InMemoryCanonicalFileStore : ICanonicalFileStorePort
{
    private sealed class Entry
    {
        public byte[] Bytes { get; set; }
        public CanonicalFilePurpose Purpose { get; set; }
        public CanonicalHash? ContentHash { get; set; }
        public CanonicalMetadataBlob? MetadataBlob { get; set; }
        public bool Tombstoned { get; set; }
        public string? TombstoneReason { get; set; }

        public Entry(
            byte[] bytes,
            CanonicalFilePurpose purpose,
            CanonicalHash? contentHash,
            CanonicalMetadataBlob? metadataBlob,
            bool tombstoned,
            string? tombstoneReason)
        {
            Bytes = bytes;
            Purpose = purpose;
            ContentHash = contentHash;
            MetadataBlob = metadataBlob;
            Tombstoned = tombstoned;
            TombstoneReason = tombstoneReason;
        }
    }

    private readonly ICanonicalPathResolver _resolver;
    private readonly Dictionary<string, Entry> _entries = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public InMemoryCanonicalFileStore(Dictionary<CanonicalRootToken, string> rootBindings)
    {
        _resolver = new CanonicalInMemoryPathResolver(rootBindings);
    }

    public InMemoryCanonicalFileStore(ICanonicalPathResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<CanonicalPathResolutionResult> ResolveAsync(CanonicalFileReference reference)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _resolver.Resolve(reference.RootToken, reference.LogicalPathToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ContainsAsync(CanonicalFileReference reference)
    {
        var resolution = await ResolveAsync(reference);
        await _semaphore.WaitAsync();
        try
        {
            return _entries.ContainsKey(resolution.ResolvedPathToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalFileReadResult> ReadAsync(CanonicalFileReadRequest request)
    {
        var resolution = await ResolveAsync(request.Reference);
        await _semaphore.WaitAsync();
        try
        {
            if (!_entries.TryGetValue(resolution.ResolvedPathToken, out var entry))
                throw CanonicalFileRuntimeError.FileNotFound(resolution.LogicalPathToken);

            if (entry.Tombstoned && !request.AllowTombstonedRead)
                throw CanonicalFileRuntimeError.Tombstoned(resolution.LogicalPathToken);

            return new CanonicalFileReadResult(
                handle: request.Reference,
                resolution: resolution,
                bytes: entry.Bytes,
                byteSize: entry.Bytes.Length,
                contentHash: entry.ContentHash,
                purpose: entry.Purpose,
                metadataBlob: entry.MetadataBlob,
                tombstoned: entry.Tombstoned,
                tombstoneReason: entry.TombstoneReason
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalFileWriteResult> WriteAsync(CanonicalFileWriteIntent intent)
    {
        var resolution = await ResolveAsync(intent.Reference);
        var newHash = Hash(intent.Bytes, intent.HashPolicy);
        ValidatePreWrite(intent, newHash);

        await _semaphore.WaitAsync();
        try
        {
            _entries.TryGetValue(resolution.ResolvedPathToken, out var existing);

            if (existing != null && IsIdempotentSameContent(intent, existing, newHash))
            {
                return new CanonicalFileWriteResult(
                    handle: intent.Reference,
                    resolution: resolution,
                    byteSize: existing.Bytes.Length,
                    contentHash: existing.ContentHash,
                    disposition: CanonicalFileWriteDisposition.acceptedExisting,
                    purpose: existing.Purpose,
                    tombstoned: existing.Tombstoned
                );
            }

            ValidateConflictPolicy(intent, existing);

            var entry = new Entry(
                bytes: intent.Bytes,
                purpose: intent.Purpose,
                contentHash: newHash,
                metadataBlob: intent.MetadataBlob,
                tombstoned: intent.Purpose == CanonicalFilePurpose.tombstoneMarker,
                tombstoneReason: intent.Purpose == CanonicalFilePurpose.tombstoneMarker ? "marker" : null
            );

            _entries[resolution.ResolvedPathToken] = entry;
            _entries.TryGetValue(resolution.ResolvedPathToken, out var stored);
            stored ??= entry;
            ValidatePostWrite(intent, stored);

            return new CanonicalFileWriteResult(
                handle: intent.Reference,
                resolution: resolution,
                byteSize: stored.Bytes.Length,
                contentHash: stored.ContentHash,
                disposition: existing == null ? CanonicalFileWriteDisposition.created : CanonicalFileWriteDisposition.replaced,
                purpose: stored.Purpose,
                tombstoned: stored.Tombstoned
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalFileWriteResult> MarkTombstoneAsync(CanonicalFileReference reference, string? reason)
    {
        var resolution = await ResolveAsync(reference);
        await _semaphore.WaitAsync();
        try
        {
            if (!_entries.TryGetValue(resolution.ResolvedPathToken, out var entry))
            {
                entry = new Entry(
                    bytes: Array.Empty<byte>(),
                    purpose: CanonicalFilePurpose.tombstoneMarker,
                    contentHash: Hash(Array.Empty<byte>(), CanonicalFileHashPolicy.sha256),
                    metadataBlob: null,
                    tombstoned: false,
                    tombstoneReason: null
                );
            }

            entry.Tombstoned = true;
            entry.TombstoneReason = reason?.Trim().NilIfEmpty() ?? "softDelete";
            _entries[resolution.ResolvedPathToken] = entry;

            return new CanonicalFileWriteResult(
                handle: reference,
                resolution: resolution,
                byteSize: entry.Bytes.Length,
                contentHash: entry.ContentHash,
                disposition: CanonicalFileWriteDisposition.tombstoneMarked,
                purpose: CanonicalFilePurpose.tombstoneMarker,
                tombstoned: true
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static void ValidatePreWrite(CanonicalFileWriteIntent intent, CanonicalHash? actualHash)
    {
        if (intent.ExpectedByteSize.HasValue && intent.ExpectedByteSize.Value != intent.Bytes.Length)
            throw CanonicalFileRuntimeError.PreWriteSizeMismatch(intent.ExpectedByteSize.Value, intent.Bytes.Length);

        if (intent.ExpectedContentHash != null)
        {
            if (actualHash == null)
                throw CanonicalFileRuntimeError.UnsupportedHashPolicy(intent.HashPolicy.ToString());

            if (!SameHash(intent.ExpectedContentHash, actualHash))
                throw CanonicalFileRuntimeError.PreWriteHashMismatch(intent.ExpectedContentHash.Value, actualHash.Value);
        }
    }

    private static void ValidatePostWrite(CanonicalFileWriteIntent intent, Entry stored)
    {
        if (intent.ExpectedByteSize.HasValue && intent.ExpectedByteSize.Value != stored.Bytes.Length)
            throw CanonicalFileRuntimeError.PostWriteSizeMismatch(intent.ExpectedByteSize.Value, stored.Bytes.Length);

        if (intent.ExpectedContentHash != null)
        {
            if (stored.ContentHash == null)
                throw CanonicalFileRuntimeError.UnsupportedHashPolicy(intent.HashPolicy.ToString());

            if (!SameHash(intent.ExpectedContentHash, stored.ContentHash))
                throw CanonicalFileRuntimeError.PostWriteHashMismatch(intent.ExpectedContentHash.Value, stored.ContentHash.Value);
        }
    }

    private static void ValidateConflictPolicy(CanonicalFileWriteIntent intent, Entry? existing)
    {
        if (existing == null) return;

        if (intent.ExpectedExistingHash != null)
        {
            if (existing.ContentHash == null || !SameHash(intent.ExpectedExistingHash, existing.ContentHash))
                throw CanonicalFileRuntimeError.ExistingHashMismatch(
                    intent.ExpectedExistingHash.Value,
                    existing.ContentHash?.Value
                );
        }

        var newHash = Hash(intent.Bytes, intent.HashPolicy);
        switch (intent.ConflictPolicy)
        {
            case CanonicalFileConflictPolicy.replace:
                return;

            case CanonicalFileConflictPolicy.replaceIfExistingHashMatches:
                if (intent.ExpectedExistingHash == null)
                    throw CanonicalFileRuntimeError.ExistingHashMismatch("required", existing.ContentHash?.Value);
                break;

            case CanonicalFileConflictPolicy.idempotentIfSameContent:
                if (newHash != null && existing.ContentHash != null &&
                    SameHash(existing.ContentHash, newHash) &&
                    existing.Bytes.Length == intent.Bytes.Length)
                    return;
                throw CanonicalFileRuntimeError.Conflict(intent.Reference.LogicalPathToken);

            case CanonicalFileConflictPolicy.noOverwrite:
                if (newHash != null && existing.ContentHash != null &&
                    SameHash(existing.ContentHash, newHash) &&
                    existing.Bytes.Length == intent.Bytes.Length)
                    return;
                throw CanonicalFileRuntimeError.Conflict(intent.Reference.LogicalPathToken);
        }
    }

    private static bool IsIdempotentSameContent(
        CanonicalFileWriteIntent intent,
        Entry existing,
        CanonicalHash? newHash)
    {
        if (intent.ConflictPolicy != CanonicalFileConflictPolicy.idempotentIfSameContent &&
            intent.ConflictPolicy != CanonicalFileConflictPolicy.noOverwrite)
            return false;

        if (newHash == null || existing.ContentHash == null)
            return false;

        return SameHash(existing.ContentHash, newHash) && existing.Bytes.Length == intent.Bytes.Length;
    }

    public static bool SameHash(CanonicalHash left, CanonicalHash right) =>
        left.Algorithm == right.Algorithm && left.Value == right.Value;

    public static CanonicalHash? Hash(byte[] data, CanonicalFileHashPolicy policy = CanonicalFileHashPolicy.sha256)
    {
        switch (policy)
        {
            case CanonicalFileHashPolicy.none:
                return null;
            case CanonicalFileHashPolicy.sha256:
                var digest = SHA256.HashData(data);
                var hex = string.Concat(digest.Select(b => b.ToString("x2")));
                return new CanonicalHash(hex);
            default:
                return null;
        }
    }
}
