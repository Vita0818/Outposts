using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRootBoundMetadataApplyPortMode
{
    disabled,
    dryRun,
    fakeInMemory,
    testRootBound,
    productionRootDisabled,
    productionRootBound,
    productionRootUnsupported
}

public static class CanonicalRootBoundMetadataApplyPortModeExtensions
{
    public static bool IsNonDryRunRootBound(this CanonicalRootBoundMetadataApplyPortMode mode) =>
        mode == CanonicalRootBoundMetadataApplyPortMode.testRootBound ||
        mode == CanonicalRootBoundMetadataApplyPortMode.productionRootBound;

    public static bool IsDefaultDisabled(this CanonicalRootBoundMetadataApplyPortMode mode) =>
        mode == CanonicalRootBoundMetadataApplyPortMode.disabled ||
        mode == CanonicalRootBoundMetadataApplyPortMode.dryRun ||
        mode == CanonicalRootBoundMetadataApplyPortMode.productionRootDisabled;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRootBoundMetadataWriteFailure
{
    rootEscape,
    productionRootDisabled,
    checkpointFailed,
    atomicWriteFailed,
    postconditionFailed,
    rollbackFailed,
    unsupportedStoreAPI,
    schemaMismatch,
    decodingFailed,
    permissionDenied,
    unknown
}

public sealed class CanonicalRootBoundMetadataTarget : IEquatable<CanonicalRootBoundMetadataTarget>
{
    public CanonicalRootToken RootToken { get; }
    public string ObjectID { get; }
    public CanonicalProductionDomain Domain { get; }
    public string LogicalPathToken { get; }

    public CanonicalRootBoundMetadataTarget(
        CanonicalRootToken rootToken,
        string objectID,
        CanonicalProductionDomain domain,
        string logicalPathToken)
    {
        var sanitizedObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        if (domain != CanonicalProductionDomain.recordingMetadata)
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.unsupportedStoreAPI);
        var safePath = CanonicalProjectionContract.SafeLogicalPathToken(logicalPathToken)
            ?? throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.rootEscape);

        RootToken = rootToken;
        ObjectID = sanitizedObjectID;
        Domain = domain;
        LogicalPathToken = safePath;
    }

    public static string DefaultLogicalPathToken(string objectID)
    {
        return $"recordingMetadata/{SafePathComponent(objectID)}.json";
    }

    private static string SafePathComponent(string value)
    {
        var trimmed = value.Trim();
        var allowed = new HashSet<char>("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.");
        var chars = trimmed.Select(c => allowed.Contains(c) ? c : '-').ToArray();
        var candidate = new string(chars).Trim('-', '.');
        if (!string.IsNullOrEmpty(candidate))
            return candidate;
        return $"recording-{CanonicalProductionRedaction.HashPrefix(CanonicalHash.Sha256String(trimmed).Value) ?? "unknown"}";
    }

    public override bool Equals(object? obj) => obj is CanonicalRootBoundMetadataTarget other && Equals(other);
    public bool Equals(CanonicalRootBoundMetadataTarget? other) =>
        other is not null &&
        RootToken.Equals(other.RootToken) &&
        ObjectID == other.ObjectID &&
        Domain == other.Domain &&
        LogicalPathToken == other.LogicalPathToken;
    public override int GetHashCode() => HashCode.Combine(RootToken, ObjectID, Domain, LogicalPathToken);
    public static bool operator ==(CanonicalRootBoundMetadataTarget l, CanonicalRootBoundMetadataTarget r) => l.Equals(r);
    public static bool operator !=(CanonicalRootBoundMetadataTarget l, CanonicalRootBoundMetadataTarget r) => !l.Equals(r);
}

public class CanonicalRootBoundMetadataWriteException : Exception
{
    public CanonicalRootBoundMetadataWriteFailure Failure { get; }

    public CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure failure)
        : base(failure.ToString())
    {
        Failure = failure;
    }
}

public sealed class CanonicalRootBoundMetadataWrite : IEquatable<CanonicalRootBoundMetadataWrite>
{
    public CanonicalRootBoundMetadataTarget Target { get; }
    public CanonicalRecordingMetadataCutoverActionKind ActionKind { get; }
    public byte[] MetadataBytes { get; }
    public CanonicalHash? MetadataHash { get; }
    public bool Tombstone { get; }
    public CanonicalTimestamp? ModifiedAt { get; }

    public CanonicalRootBoundMetadataWrite(
        CanonicalRootBoundMetadataTarget target,
        CanonicalRecordingMetadataCutoverActionKind actionKind,
        byte[] metadataBytes,
        CanonicalHash? metadataHash = null,
        bool tombstone = false,
        CanonicalTimestamp? modifiedAt = null)
    {
        Target = target;
        ActionKind = actionKind;
        MetadataBytes = metadataBytes;
        MetadataHash = metadataHash;
        Tombstone = tombstone;
        ModifiedAt = modifiedAt;
    }

    public override bool Equals(object? obj) => obj is CanonicalRootBoundMetadataWrite other && Equals(other);
    public bool Equals(CanonicalRootBoundMetadataWrite? other) =>
        other is not null &&
        Target.Equals(other.Target) &&
        ActionKind == other.ActionKind &&
        MetadataBytes.SequenceEqual(other.MetadataBytes) &&
        Equals(MetadataHash, other.MetadataHash) &&
        Tombstone == other.Tombstone &&
        Equals(ModifiedAt, other.ModifiedAt);
    public override int GetHashCode() => HashCode.Combine(Target, ActionKind, Tombstone);
    public static bool operator ==(CanonicalRootBoundMetadataWrite l, CanonicalRootBoundMetadataWrite r) => l.Equals(r);
    public static bool operator !=(CanonicalRootBoundMetadataWrite l, CanonicalRootBoundMetadataWrite r) => !l.Equals(r);
}

public sealed class CanonicalRootBoundMetadataCheckpoint : IEquatable<CanonicalRootBoundMetadataCheckpoint>
{
    public string Id => CheckpointID;
    public string CheckpointID { get; }
    public string ObjectID { get; }
    public CanonicalProductionDomain Domain { get; }
    public string RollbackID { get; }
    public string? HashPrefixBefore { get; }
    public long? ByteCountBefore { get; }
    public bool ExistedBeforeWrite { get; }
    public bool RollbackAvailable { get; }

    public CanonicalRootBoundMetadataCheckpoint(
        string checkpointID,
        string objectID,
        string rollbackID,
        bool existedBeforeWrite,
        bool rollbackAvailable,
        CanonicalProductionDomain domain = CanonicalProductionDomain.recordingMetadata,
        CanonicalHash? hashBefore = null,
        long? byteCountBefore = null)
    {
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "metadata-checkpoint");
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        Domain = domain;
        RollbackID = CanonicalProductionRedaction.SafeIdentifier(rollbackID, "metadata-rollback");
        HashPrefixBefore = hashBefore != null ? CanonicalProductionRedaction.HashPrefix(hashBefore.Value) : null;
        ByteCountBefore = byteCountBefore;
        ExistedBeforeWrite = existedBeforeWrite;
        RollbackAvailable = rollbackAvailable;
    }

    public override bool Equals(object? obj) => obj is CanonicalRootBoundMetadataCheckpoint other && Equals(other);
    public bool Equals(CanonicalRootBoundMetadataCheckpoint? other) =>
        other is not null && CheckpointID == other.CheckpointID;
    public override int GetHashCode() => CheckpointID.GetHashCode();
    public static bool operator ==(CanonicalRootBoundMetadataCheckpoint l, CanonicalRootBoundMetadataCheckpoint r) => l.Equals(r);
    public static bool operator !=(CanonicalRootBoundMetadataCheckpoint l, CanonicalRootBoundMetadataCheckpoint r) => !l.Equals(r);
}

public sealed class CanonicalRootBoundMetadataWriteResult : IEquatable<CanonicalRootBoundMetadataWriteResult>
{
    public string ObjectID { get; }
    public CanonicalProductionDomain Domain { get; }
    public CanonicalRecordingMetadataCutoverActionKind ActionKind { get; }
    public string? HashPrefixBefore { get; }
    public string? HashPrefixAfter { get; }
    public long ByteCount { get; }
    public string CheckpointID { get; }
    public bool AtomicWriteUsed { get; }
    public bool RollbackAvailable { get; }
    public bool Tombstone { get; }
    public CanonicalTimestamp? ModifiedAt { get; }
    public CanonicalRootBoundMetadataWriteFailure? Failure { get; }

    public CanonicalRootBoundMetadataWriteResult(
        string objectID,
        CanonicalRecordingMetadataCutoverActionKind actionKind,
        long byteCount,
        string checkpointID,
        bool atomicWriteUsed,
        bool rollbackAvailable,
        bool tombstone,
        CanonicalTimestamp? modifiedAt,
        CanonicalProductionDomain domain = CanonicalProductionDomain.recordingMetadata,
        CanonicalHash? hashBefore = null,
        CanonicalHash? hashAfter = null,
        CanonicalRootBoundMetadataWriteFailure? failure = null)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        Domain = domain;
        ActionKind = actionKind;
        HashPrefixBefore = hashBefore != null ? CanonicalProductionRedaction.HashPrefix(hashBefore.Value) : null;
        HashPrefixAfter = hashAfter != null ? CanonicalProductionRedaction.HashPrefix(hashAfter.Value) : null;
        ByteCount = Math.Max(0, byteCount);
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "metadata-checkpoint");
        AtomicWriteUsed = atomicWriteUsed;
        RollbackAvailable = rollbackAvailable;
        Tombstone = tombstone;
        ModifiedAt = modifiedAt;
        Failure = failure;
    }

    public override bool Equals(object? obj) => obj is CanonicalRootBoundMetadataWriteResult other && Equals(other);
    public bool Equals(CanonicalRootBoundMetadataWriteResult? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        Domain == other.Domain &&
        ActionKind == other.ActionKind &&
        HashPrefixBefore == other.HashPrefixBefore &&
        HashPrefixAfter == other.HashPrefixAfter &&
        ByteCount == other.ByteCount &&
        CheckpointID == other.CheckpointID &&
        AtomicWriteUsed == other.AtomicWriteUsed &&
        RollbackAvailable == other.RollbackAvailable &&
        Tombstone == other.Tombstone &&
        Equals(ModifiedAt, other.ModifiedAt) &&
        Failure == other.Failure;
    public override int GetHashCode() => HashCode.Combine(ObjectID, Domain, ActionKind, ByteCount,
        CheckpointID, AtomicWriteUsed, RollbackAvailable, Tombstone);
    public static bool operator ==(CanonicalRootBoundMetadataWriteResult l, CanonicalRootBoundMetadataWriteResult r) => l.Equals(r);
    public static bool operator !=(CanonicalRootBoundMetadataWriteResult l, CanonicalRootBoundMetadataWriteResult r) => !l.Equals(r);
}

public sealed class CanonicalRootBoundMetadataRollbackResult : IEquatable<CanonicalRootBoundMetadataRollbackResult>
{
    public string ObjectID { get; }
    public CanonicalProductionDomain Domain { get; }
    public string CheckpointID { get; }
    public bool Succeeded { get; }
    public bool RollbackVerified { get; }
    public string? HashPrefixAfterRollback { get; }
    public long? ByteCount { get; }
    public CanonicalRootBoundMetadataWriteFailure? Failure { get; }

    public CanonicalRootBoundMetadataRollbackResult(
        string objectID,
        string checkpointID,
        bool succeeded,
        bool rollbackVerified,
        CanonicalProductionDomain domain = CanonicalProductionDomain.recordingMetadata,
        CanonicalHash? hashAfterRollback = null,
        long? byteCount = null,
        CanonicalRootBoundMetadataWriteFailure? failure = null)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        Domain = domain;
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "metadata-checkpoint");
        Succeeded = succeeded;
        RollbackVerified = rollbackVerified;
        HashPrefixAfterRollback = hashAfterRollback != null ? CanonicalProductionRedaction.HashPrefix(hashAfterRollback.Value) : null;
        ByteCount = byteCount;
        Failure = failure;
    }

    public override bool Equals(object? obj) => obj is CanonicalRootBoundMetadataRollbackResult other && Equals(other);
    public bool Equals(CanonicalRootBoundMetadataRollbackResult? other) =>
        other is not null &&
        ObjectID == other.ObjectID &&
        Domain == other.Domain &&
        CheckpointID == other.CheckpointID &&
        Succeeded == other.Succeeded &&
        RollbackVerified == other.RollbackVerified &&
        HashPrefixAfterRollback == other.HashPrefixAfterRollback &&
        ByteCount == other.ByteCount &&
        Failure == other.Failure;
    public override int GetHashCode() => HashCode.Combine(ObjectID, Domain, CheckpointID, Succeeded,
        RollbackVerified, HashPrefixAfterRollback, ByteCount);
    public static bool operator ==(CanonicalRootBoundMetadataRollbackResult l, CanonicalRootBoundMetadataRollbackResult r) => l.Equals(r);
    public static bool operator !=(CanonicalRootBoundMetadataRollbackResult l, CanonicalRootBoundMetadataRollbackResult r) => !l.Equals(r);
}

public sealed class CanonicalRootBoundMetadataWriteCore
{
    private sealed class StoredCheckpoint
    {
        public CanonicalRootBoundMetadataCheckpoint PublicCheckpoint { get; set; } = null!;
        public CanonicalRootBoundMetadataTarget Target { get; set; } = null!;
        public byte[]? PreviousBytes { get; set; }
        public CanonicalHash? PreviousHash { get; set; }
    }

    private readonly string _rootPath;
    private readonly CanonicalRootToken _rootToken;
    private readonly CanonicalRootBoundMetadataApplyPortMode _mode;
    private readonly Dictionary<string, CanonicalRootBoundMetadataWrite> _payloadsByActionID = new();
    private readonly Dictionary<string, CanonicalRootBoundMetadataWrite> _payloadsByObjectAndKind = new();
    private readonly Dictionary<string, StoredCheckpoint> _checkpoints = new();
    private readonly Dictionary<string, string> _actionIDsByCheckpointID = new();
    private readonly Dictionary<string, CanonicalRootBoundMetadataWriteResult> _lastWriteByActionID = new();
    private readonly Dictionary<string, CanonicalRootBoundMetadataRollbackResult> _lastRollbackByCheckpointID = new();
    private readonly HashSet<string> _checkpointFailureObjectIDs = new();
    private readonly HashSet<string> _postconditionFailureObjectIDs = new();
    private readonly HashSet<string> _rollbackFailureCheckpointIDs = new();

    public CanonicalRootBoundMetadataApplyPortMode ApplyPortMode => _mode;

    public CanonicalRootBoundMetadataWriteCore(
        string rootPath,
        CanonicalRootToken rootToken,
        CanonicalRootBoundMetadataApplyPortMode mode)
    {
        if (!Path.IsPathRooted(rootPath))
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.rootEscape);
        _rootPath = Path.GetFullPath(rootPath);
        _rootToken = rootToken;
        _mode = mode;
    }

    public void SetPayload(
        string objectID,
        CanonicalRecordingMetadataCutoverActionKind actionKind,
        byte[] metadataBytes,
        CanonicalHash? metadataHash = null,
        bool tombstone = false,
        CanonicalTimestamp? modifiedAt = null,
        string? logicalPathToken = null,
        string? actionID = null)
    {
        var target = new CanonicalRootBoundMetadataTarget(
            _rootToken,
            objectID,
            CanonicalProductionDomain.recordingMetadata,
            logicalPathToken ?? CanonicalRootBoundMetadataTarget.DefaultLogicalPathToken(objectID));

        var write = new CanonicalRootBoundMetadataWrite(
            target, actionKind, metadataBytes, metadataHash, tombstone, modifiedAt);

        _payloadsByObjectAndKind[Key(target.ObjectID, actionKind)] = write;
        if (actionID != null)
            _payloadsByActionID[CanonicalProductionRedaction.SafeIdentifier(actionID, actionKind.ToString())] = write;
    }

    public void InjectCheckpointFailure(string objectID)
    {
        _checkpointFailureObjectIDs.Add(CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording"));
    }

    public void InjectPostconditionFailure(string objectID)
    {
        _postconditionFailureObjectIDs.Add(CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording"));
    }

    public void InjectRollbackFailure(string checkpointID)
    {
        _rollbackFailureCheckpointIDs.Add(CanonicalProductionRedaction.SafeIdentifier(checkpointID, "metadata-checkpoint"));
    }

    public CanonicalRootBoundMetadataWriteResult Write(
        CanonicalApplyAction action,
        CanonicalRecordingMetadataCutoverActionKind actionKind,
        string? checkpointID = null)
    {
        RequireWritableMode();
        var objectID = CanonicalProductionRedaction.SafeIdentifier(action.Target.ObjectID, "unknown-recording");

        if (!_payloadsByActionID.TryGetValue(action.ActionID, out var payload) &&
            !_payloadsByObjectAndKind.TryGetValue(Key(objectID, actionKind), out payload))
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.unsupportedStoreAPI);

        if (payload.ActionKind != actionKind || payload.Target.ObjectID != objectID)
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.schemaMismatch);

        if (_checkpointFailureObjectIDs.Contains(objectID))
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.checkpointFailed);

        var effectiveCheckpointID = CanonicalProductionRedaction.SafeIdentifier(
            checkpointID ?? $"root-bound-metadata-{objectID}-{actionKind}",
            "metadata-checkpoint");

        var targetPath = ResolvePath(payload.Target);
        byte[]? previousBytes = File.Exists(targetPath) ? File.ReadAllBytes(targetPath) : null;
        var previousHash = previousBytes != null ? Sha256(previousBytes) : null;

        var checkpoint = new CanonicalRootBoundMetadataCheckpoint(
            effectiveCheckpointID, objectID,
            $"rollback-{effectiveCheckpointID}",
            existedBeforeWrite: previousBytes != null,
            rollbackAvailable: true,
            hashBefore: previousHash,
            byteCountBefore: previousBytes != null ? previousBytes.LongLength : null);

        _checkpoints[effectiveCheckpointID] = new StoredCheckpoint
        {
            PublicCheckpoint = checkpoint,
            Target = payload.Target,
            PreviousBytes = previousBytes,
            PreviousHash = previousHash
        };
        _actionIDsByCheckpointID[effectiveCheckpointID] = action.ActionID;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            using (var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                fs.Write(payload.MetadataBytes, 0, payload.MetadataBytes.Length);
                fs.Flush(true);
            }

            var reread = File.ReadAllBytes(targetPath);
            if (!reread.SequenceEqual(payload.MetadataBytes))
            {
                Restore(effectiveCheckpointID);
                throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.postconditionFailed);
            }

            var afterHash = payload.MetadataHash ?? Sha256(reread);
            var result = new CanonicalRootBoundMetadataWriteResult(
                objectID, actionKind, reread.LongLength,
                effectiveCheckpointID, atomicWriteUsed: true, rollbackAvailable: true,
                tombstone: payload.Tombstone, modifiedAt: payload.ModifiedAt,
                hashBefore: previousHash, hashAfter: afterHash);

            _lastWriteByActionID[action.ActionID] = result;
            return result;
        }
        catch (CanonicalRootBoundMetadataWriteException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            RestoreSafe(effectiveCheckpointID);
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.permissionDenied);
        }
        catch (IOException)
        {
            RestoreSafe(effectiveCheckpointID);
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.atomicWriteFailed);
        }
    }

    public CanonicalProductionApplyPostcondition VerifyPostcondition(CanonicalProductionApplyPostcondition postcondition)
    {
        var checkedResult = postcondition;
        var objectID = CanonicalProductionRedaction.SafeIdentifier(postcondition.Target.ObjectID, "unknown-recording");

        if (_postconditionFailureObjectIDs.Contains(objectID))
        {
            checkedResult.Accepted = false;
            checkedResult.Reason = CanonicalRootBoundMetadataWriteFailure.postconditionFailed.ToString();
            return checkedResult;
        }

        if (!_lastWriteByActionID.TryGetValue(postcondition.ActionID, out var write))
        {
            checkedResult.Accepted = false;
            checkedResult.Reason = CanonicalRootBoundMetadataWriteFailure.postconditionFailed.ToString();
            return checkedResult;
        }

        if (postcondition.ActualHashPrefix != null && write.HashPrefixAfter != null)
        {
            if (CanonicalProductionRedaction.HashPrefix(postcondition.ActualHashPrefix) != write.HashPrefixAfter)
            {
                checkedResult.Accepted = false;
                checkedResult.Reason = CanonicalRootBoundMetadataWriteFailure.postconditionFailed.ToString();
            }
        }

        return checkedResult;
    }

    public CanonicalRootBoundMetadataRollbackResult Rollback(CanonicalRollbackAction request)
    {
        var checkpointID = CanonicalProductionRedaction.SafeIdentifier(
            request.CheckpointID ?? request.ActionID, "metadata-checkpoint");
        var objectID = request.ObjectID != null
            ? CanonicalProductionRedaction.SafeIdentifier(request.ObjectID, "unknown-recording")
            : "unknown-recording";

        if (_rollbackFailureCheckpointIDs.Contains(checkpointID))
        {
            var failResult = new CanonicalRootBoundMetadataRollbackResult(
                objectID, checkpointID, succeeded: false, rollbackVerified: false,
                failure: CanonicalRootBoundMetadataWriteFailure.rollbackFailed);
            _lastRollbackByCheckpointID[checkpointID] = failResult;
            return failResult;
        }

        try
        {
            Restore(checkpointID);
            if (!_checkpoints.TryGetValue(checkpointID, out var storedCheckpoint))
                throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.rollbackFailed);

            var targetPath = ResolvePath(storedCheckpoint.Target);
            byte[]? currentBytes = File.Exists(targetPath) ? File.ReadAllBytes(targetPath) : null;
            var verified = (currentBytes == null && storedCheckpoint.PreviousBytes == null) ||
                           (currentBytes != null && storedCheckpoint.PreviousBytes != null &&
                            currentBytes.SequenceEqual(storedCheckpoint.PreviousBytes));

            var result = new CanonicalRootBoundMetadataRollbackResult(
                storedCheckpoint.PublicCheckpoint.ObjectID, checkpointID,
                succeeded: verified, rollbackVerified: verified,
                hashAfterRollback: currentBytes != null ? Sha256(currentBytes) : null,
                byteCount: currentBytes?.LongLength,
                failure: verified ? null : CanonicalRootBoundMetadataWriteFailure.rollbackFailed);

            _lastRollbackByCheckpointID[checkpointID] = result;
            return result;
        }
        catch
        {
            var errorResult = new CanonicalRootBoundMetadataRollbackResult(
                objectID, checkpointID, succeeded: false, rollbackVerified: false,
                failure: CanonicalRootBoundMetadataWriteFailure.rollbackFailed);
            _lastRollbackByCheckpointID[checkpointID] = errorResult;
            return errorResult;
        }
    }

    public CanonicalRootBoundMetadataWriteResult? LastWriteResult(string actionID)
    {
        return _lastWriteByActionID.GetValueOrDefault(actionID);
    }

    public CanonicalRootBoundMetadataRollbackResult? LastRollbackResult(string checkpointID)
    {
        var safeID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "metadata-checkpoint");
        return _lastRollbackByCheckpointID.GetValueOrDefault(safeID);
    }

    public byte[]? ReadMetadataBytes(string objectID, CanonicalRecordingMetadataCutoverActionKind actionKind = CanonicalRecordingMetadataCutoverActionKind.apply)
    {
        var safeObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        if (!_payloadsByObjectAndKind.TryGetValue(Key(safeObjectID, actionKind), out var payload))
            return null;
        var path = ResolvePath(payload.Target);
        if (!File.Exists(path))
            return null;
        return File.ReadAllBytes(path);
    }

    private void RequireWritableMode()
    {
        switch (_mode)
        {
            case CanonicalRootBoundMetadataApplyPortMode.testRootBound:
            case CanonicalRootBoundMetadataApplyPortMode.productionRootBound:
                return;
            case CanonicalRootBoundMetadataApplyPortMode.productionRootDisabled:
            case CanonicalRootBoundMetadataApplyPortMode.disabled:
            case CanonicalRootBoundMetadataApplyPortMode.dryRun:
                throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.productionRootDisabled);
            case CanonicalRootBoundMetadataApplyPortMode.productionRootUnsupported:
            case CanonicalRootBoundMetadataApplyPortMode.fakeInMemory:
                throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.unsupportedStoreAPI);
        }
    }

    private void Restore(string checkpointID)
    {
        if (!_checkpoints.TryGetValue(checkpointID, out var stored))
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.rollbackFailed);

        var path = ResolvePath(stored.Target);
        if (stored.PreviousBytes != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, stored.PreviousBytes);
        }
        else if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void RestoreSafe(string checkpointID)
    {
        try { Restore(checkpointID); } catch { }
    }

    private string ResolvePath(CanonicalRootBoundMetadataTarget target)
    {
        if (target.RootToken != _rootToken || CanonicalProjectionContract.SafeLogicalPathToken(target.LogicalPathToken) == null)
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.rootEscape);

        var path = Path.GetFullPath(Path.Combine(_rootPath, target.LogicalPathToken));
        if (!IsInsideRoot(path) || path == _rootPath)
            throw new CanonicalRootBoundMetadataWriteException(CanonicalRootBoundMetadataWriteFailure.rootEscape);

        return path;
    }

    private bool IsInsideRoot(string path)
    {
        var rootPath = Path.GetFullPath(_rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var filePath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return filePath == rootPath || filePath.StartsWith(rootPath + Path.DirectorySeparatorChar);
    }

    private static string Key(string objectID, CanonicalRecordingMetadataCutoverActionKind actionKind)
    {
        return $"{CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording")}|{actionKind}";
    }

    private static CanonicalHash Sha256(byte[] data)
    {
        var digest = System.Security.Cryptography.SHA256.HashData(data);
        return new CanonicalHash(string.Join("", digest.Select(b => b.ToString("x2"))));
    }
}
