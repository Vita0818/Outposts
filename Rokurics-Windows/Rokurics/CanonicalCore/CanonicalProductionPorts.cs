using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── Forward-reference stub types needed by this file but defined elsewhere ──

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFilePurpose
{
    recordingMetadata,
    recordingAudio,
    generatedArtifact,
    folderMetadata,
    studyItemMetadata,
    standaloneNote,
    tombstone,
    conflict,
    inventory,
    diagnostics
}

public sealed class CanonicalRootToken : IEquatable<CanonicalRootToken>
{
    public string Value { get; }
    public CanonicalRootToken(string value)
    {
        Value = value.Trim().NilIfEmpty() ?? "root:unknown";
    }
    public override bool Equals(object? obj) => obj is CanonicalRootToken other && Equals(other);
    public bool Equals(CanonicalRootToken? other) => other is not null && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(CanonicalRootToken l, CanonicalRootToken r) => l.Equals(r);
    public static bool operator !=(CanonicalRootToken l, CanonicalRootToken r) => !l.Equals(r);
}

public sealed class CanonicalFileReference : IEquatable<CanonicalFileReference>
{
    public string LogicalPathToken { get; }
    public CanonicalRootToken RootToken { get; }
    public CanonicalFileReference(string logicalPathToken, CanonicalRootToken rootToken)
    {
        LogicalPathToken = logicalPathToken;
        RootToken = rootToken;
    }
    public override bool Equals(object? obj) => obj is CanonicalFileReference other && Equals(other);
    public bool Equals(CanonicalFileReference? other) =>
        other is not null && LogicalPathToken == other.LogicalPathToken && RootToken.Equals(other.RootToken);
    public override int GetHashCode() => HashCode.Combine(LogicalPathToken, RootToken);
    public static bool operator ==(CanonicalFileReference l, CanonicalFileReference r) => l.Equals(r);
    public static bool operator !=(CanonicalFileReference l, CanonicalFileReference r) => !l.Equals(r);
}

public sealed class CanonicalPathResolutionResult : IEquatable<CanonicalPathResolutionResult>
{
    public bool Resolved { get; }
    public string? ResolvedPath { get; }
    public string? FailureReason { get; }
    public CanonicalPathResolutionResult(bool resolved, string? resolvedPath = null, string? failureReason = null)
    {
        Resolved = resolved;
        ResolvedPath = resolvedPath;
        FailureReason = failureReason;
    }
    public static readonly CanonicalPathResolutionResult Unresolved = new(false, failureReason: "unresolved");
    public override bool Equals(object? obj) => obj is CanonicalPathResolutionResult other && Equals(other);
    public bool Equals(CanonicalPathResolutionResult? other) =>
        other is not null && Resolved == other.Resolved && ResolvedPath == other.ResolvedPath && FailureReason == other.FailureReason;
    public override int GetHashCode() => HashCode.Combine(Resolved, ResolvedPath, FailureReason);
    public static bool operator ==(CanonicalPathResolutionResult l, CanonicalPathResolutionResult r) => l.Equals(r);
    public static bool operator !=(CanonicalPathResolutionResult l, CanonicalPathResolutionResult r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalFileWriteDisposition
{
    created,
    replaced,
    unchanged,
    tombstoned,
    suppressed,
    noOp,
    rollbackRestored
}

public sealed class CanonicalFileWriteIntent : IEquatable<CanonicalFileWriteIntent>
{
    public string Id => $"{Purpose}:{Reference.LogicalPathToken}";
    public CanonicalFileReference Reference { get; }
    public CanonicalFilePurpose Purpose { get; }
    public byte[]? Content { get; }
    public long? ExpectedByteSize { get; }
    public CanonicalHash? ExpectedContentHash { get; }
    public CanonicalHash? ContentHash { get; }
    public bool NoPhysicalDelete { get; }
    public CanonicalFileWriteDisposition Disposition { get; }
    public CanonicalFileWriteIntent(
        CanonicalFileReference reference,
        CanonicalFilePurpose purpose,
        byte[]? content = null,
        long? expectedByteSize = null,
        CanonicalHash? expectedContentHash = null,
        CanonicalHash? contentHash = null,
        bool noPhysicalDelete = true,
        CanonicalFileWriteDisposition disposition = CanonicalFileWriteDisposition.created)
    {
        Reference = reference;
        Purpose = purpose;
        Content = content;
        ExpectedByteSize = expectedByteSize;
        ExpectedContentHash = expectedContentHash;
        ContentHash = contentHash;
        NoPhysicalDelete = noPhysicalDelete;
        Disposition = disposition;
    }
    public override bool Equals(object? obj) => obj is CanonicalFileWriteIntent other && Equals(other);
    public bool Equals(CanonicalFileWriteIntent? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalFileWriteIntent l, CanonicalFileWriteIntent r) => l.Equals(r);
    public static bool operator !=(CanonicalFileWriteIntent l, CanonicalFileWriteIntent r) => !l.Equals(r);
}

public sealed class CanonicalMetadataBlob : IEquatable<CanonicalMetadataBlob>
{
    public byte[] Data { get; }
    public CanonicalHash? Hash { get; }
    public CanonicalMetadataBlob(byte[] data, CanonicalHash? hash = null)
    {
        Data = data;
        Hash = hash;
    }
    public override bool Equals(object? obj) => obj is CanonicalMetadataBlob other && Equals(other);
    public bool Equals(CanonicalMetadataBlob? other) =>
        other is not null && Data.SequenceEqual(other.Data);
    public override int GetHashCode() => Data.Aggregate(0, HashCode.Combine);
    public static bool operator ==(CanonicalMetadataBlob l, CanonicalMetadataBlob r) => l.Equals(r);
    public static bool operator !=(CanonicalMetadataBlob l, CanonicalMetadataBlob r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTransportRoute
{
    uploadStart,
    manifestExchange,
    syncInventory,
    uploadChunk,
    uploadFinalize,
    uploadCancel,
    uploadQuery,
    artifactRequest,
    applyMetadata,
    legacyManifest
}

public sealed class CanonicalTransportResponse : IEquatable<CanonicalTransportResponse>
{
    public int StatusCode { get; }
    public byte[]? Body { get; }
    public CanonicalHash? BodyHash { get; }
    public bool HasValidBodyHash => BodyHash is not null && Body is not null;
    public CanonicalTransportResponse(int statusCode, byte[]? body = null, CanonicalHash? bodyHash = null)
    {
        StatusCode = statusCode;
        Body = body;
        BodyHash = bodyHash;
    }
    public override bool Equals(object? obj) => obj is CanonicalTransportResponse other && Equals(other);
    public bool Equals(CanonicalTransportResponse? other) =>
        other is not null && StatusCode == other.StatusCode && HasValidBodyHash == other.HasValidBodyHash;
    public override int GetHashCode() => HashCode.Combine(StatusCode, BodyHash);
    public static bool operator ==(CanonicalTransportResponse l, CanonicalTransportResponse r) => l.Equals(r);
    public static bool operator !=(CanonicalTransportResponse l, CanonicalTransportResponse r) => !l.Equals(r);
}

public static class CanonicalTransportEnvelope
{
    public static CanonicalHash Hash(byte[] body)
    {
        return CanonicalHash.Sha256Of(new Dictionary<string, string>
        {
            ["bodyHash"] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(body))
        });
    }
}

public static class CanonicalTransportJSON
{
    public static byte[] Encode<T>(T value)
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value);
    }
}

public enum CanonicalTransportRuntimeError
{
    invalidBodyHash
}

public sealed class CanonicalUploadSessionID : IEquatable<CanonicalUploadSessionID>
{
    public string RawValue { get; }
    public CanonicalUploadSessionID(string rawValue) { RawValue = rawValue.Trim().NilIfEmpty() ?? "session:unknown"; }
    public override bool Equals(object? obj) => obj is CanonicalUploadSessionID other && Equals(other);
    public bool Equals(CanonicalUploadSessionID? other) => other is not null && RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();
    public override string ToString() => RawValue;
    public static bool operator ==(CanonicalUploadSessionID l, CanonicalUploadSessionID r) => l.Equals(r);
    public static bool operator !=(CanonicalUploadSessionID l, CanonicalUploadSessionID r) => !l.Equals(r);
}

public sealed class CanonicalUploadStartRequest : IEquatable<CanonicalUploadStartRequest>
{
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public long TotalBytes { get; }
    public CanonicalHash? TotalHash { get; }
    public CanonicalUploadStartRequest(string objectID, string? artifactID = null, long totalBytes = 0, CanonicalHash? totalHash = null)
    {
        ObjectID = objectID.Trim().NilIfEmpty() ?? "unknown-recording";
        ArtifactID = artifactID?.Trim().NilIfEmpty();
        TotalBytes = totalBytes;
        TotalHash = totalHash;
    }
    public override bool Equals(object? obj) => obj is CanonicalUploadStartRequest other && Equals(other);
    public bool Equals(CanonicalUploadStartRequest? other) =>
        other is not null && ObjectID == other.ObjectID && ArtifactID == other.ArtifactID;
    public override int GetHashCode() => HashCode.Combine(ObjectID, ArtifactID);
    public static bool operator ==(CanonicalUploadStartRequest l, CanonicalUploadStartRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalUploadStartRequest l, CanonicalUploadStartRequest r) => !l.Equals(r);
}

public sealed class CanonicalUploadStatusRequest : IEquatable<CanonicalUploadStatusRequest>
{
    public string ObjectID { get; }
    public CanonicalUploadSessionID SessionID { get; }
    public CanonicalUploadStatusRequest(string objectID, CanonicalUploadSessionID sessionID)
    {
        ObjectID = objectID.Trim().NilIfEmpty() ?? "unknown-recording";
        SessionID = sessionID;
    }
    public override bool Equals(object? obj) => obj is CanonicalUploadStatusRequest other && Equals(other);
    public bool Equals(CanonicalUploadStatusRequest? other) =>
        other is not null && ObjectID == other.ObjectID && SessionID.Equals(other.SessionID);
    public override int GetHashCode() => HashCode.Combine(ObjectID, SessionID);
    public static bool operator ==(CanonicalUploadStatusRequest l, CanonicalUploadStatusRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalUploadStatusRequest l, CanonicalUploadStatusRequest r) => !l.Equals(r);
}

public sealed class CanonicalUploadChunk : IEquatable<CanonicalUploadChunk>
{
    public string Id => $"{ObjectID}:{Offset}";
    public string ObjectID { get; }
    public CanonicalUploadSessionID SessionID { get; }
    public long Offset { get; }
    public int Size { get; }
    public byte[] ChunkData { get; }
    public CanonicalHash? ChunkHash { get; }
    public CanonicalUploadChunk(string objectID, CanonicalUploadSessionID sessionID, long offset, int size, byte[] chunkData, CanonicalHash? chunkHash = null)
    {
        ObjectID = objectID.Trim().NilIfEmpty() ?? "unknown-recording";
        SessionID = sessionID;
        Offset = offset;
        Size = size;
        ChunkData = chunkData;
        ChunkHash = chunkHash;
    }
    public override bool Equals(object? obj) => obj is CanonicalUploadChunk other && Equals(other);
    public bool Equals(CanonicalUploadChunk? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalUploadChunk l, CanonicalUploadChunk r) => l.Equals(r);
    public static bool operator !=(CanonicalUploadChunk l, CanonicalUploadChunk r) => !l.Equals(r);
}

public sealed class CanonicalUploadFinalizeRequest : IEquatable<CanonicalUploadFinalizeRequest>
{
    public string ObjectID { get; }
    public CanonicalUploadSessionID SessionID { get; }
    public CanonicalHash? Checksum { get; }
    public CanonicalUploadFinalizeRequest(string objectID, CanonicalUploadSessionID sessionID, CanonicalHash? checksum = null)
    {
        ObjectID = objectID.Trim().NilIfEmpty() ?? "unknown-recording";
        SessionID = sessionID;
        Checksum = checksum;
    }
    public override bool Equals(object? obj) => obj is CanonicalUploadFinalizeRequest other && Equals(other);
    public bool Equals(CanonicalUploadFinalizeRequest? other) =>
        other is not null && ObjectID == other.ObjectID && SessionID.Equals(other.SessionID);
    public override int GetHashCode() => HashCode.Combine(ObjectID, SessionID);
    public static bool operator ==(CanonicalUploadFinalizeRequest l, CanonicalUploadFinalizeRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalUploadFinalizeRequest l, CanonicalUploadFinalizeRequest r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalUploadSessionPhase
{
    notStarted,
    starting,
    inProgress,
    finalizing,
    completed,
    cancelled,
    failed
}

public sealed class CanonicalUploadSessionStatus : IEquatable<CanonicalUploadSessionStatus>
{
    public CanonicalUploadSessionID SessionID { get; }
    public CanonicalUploadSessionPhase Phase { get; }
    public long ConfirmedBytes { get; }
    public long? FileSize { get; }
    public CanonicalHash? Checksum { get; }
    public CanonicalUploadSessionStatus(CanonicalUploadSessionID sessionID, CanonicalUploadSessionPhase phase,
        long confirmedBytes = 0, long? fileSize = null, CanonicalHash? checksum = null)
    {
        SessionID = sessionID;
        Phase = phase;
        ConfirmedBytes = confirmedBytes;
        FileSize = fileSize;
        Checksum = checksum;
    }
    public override bool Equals(object? obj) => obj is CanonicalUploadSessionStatus other && Equals(other);
    public bool Equals(CanonicalUploadSessionStatus? other) =>
        other is not null && SessionID.Equals(other.SessionID) && Phase == other.Phase && ConfirmedBytes == other.ConfirmedBytes;
    public override int GetHashCode() => HashCode.Combine(SessionID, Phase, ConfirmedBytes);
    public static bool operator ==(CanonicalUploadSessionStatus l, CanonicalUploadSessionStatus r) => l.Equals(r);
    public static bool operator !=(CanonicalUploadSessionStatus l, CanonicalUploadSessionStatus r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalApplyExecutionStatus
{
    planned,
    executing,
    completed,
    failed,
    preconditionFailed,
    postconditionFailed,
    rollbackStarted,
    rollbackCompleted,
    rollbackFailed,
    dryRunSuppressed,
    noOp,
    conflictRecorded
}

public sealed class CanonicalDryRunReadinessReport : IEquatable<CanonicalDryRunReadinessReport>
{
    public bool ProductionMigrationBlocked { get; }
    public CanonicalDryRunReadinessReport(bool productionMigrationBlocked = true) { ProductionMigrationBlocked = productionMigrationBlocked; }
    public override bool Equals(object? obj) => obj is CanonicalDryRunReadinessReport other && Equals(other);
    public bool Equals(CanonicalDryRunReadinessReport? other) =>
        other is not null && ProductionMigrationBlocked == other.ProductionMigrationBlocked;
    public override int GetHashCode() => ProductionMigrationBlocked.GetHashCode();
    public static bool operator ==(CanonicalDryRunReadinessReport l, CanonicalDryRunReadinessReport r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunReadinessReport l, CanonicalDryRunReadinessReport r) => !l.Equals(r);
}

public sealed class CanonicalLegacyEquivalenceReport : IEquatable<CanonicalLegacyEquivalenceReport>
{
    public sealed class EquivalenceData : IEquatable<EquivalenceData>
    {
        public bool AllEquivalent { get; }
        public bool HasBlockingDivergence { get; }
        public EquivalenceData(bool allEquivalent = false, bool hasBlockingDivergence = true)
        {
            AllEquivalent = allEquivalent;
            HasBlockingDivergence = hasBlockingDivergence;
        }
        public override bool Equals(object? obj) => obj is EquivalenceData other && Equals(other);
        public bool Equals(EquivalenceData? other) =>
            other is not null && AllEquivalent == other.AllEquivalent && HasBlockingDivergence == other.HasBlockingDivergence;
        public override int GetHashCode() => HashCode.Combine(AllEquivalent, HasBlockingDivergence);
        public static bool operator ==(EquivalenceData l, EquivalenceData r) => l.Equals(r);
        public static bool operator !=(EquivalenceData l, EquivalenceData r) => !l.Equals(r);
    }
    public EquivalenceData LegacyEquivalence { get; }
    public CanonicalLegacyEquivalenceReport(EquivalenceData? legacyEquivalence = null)
    {
        LegacyEquivalence = legacyEquivalence ?? new EquivalenceData();
    }
    public override bool Equals(object? obj) => obj is CanonicalLegacyEquivalenceReport other && Equals(other);
    public bool Equals(CanonicalLegacyEquivalenceReport? other) =>
        other is not null && LegacyEquivalence.Equals(other.LegacyEquivalence);
    public override int GetHashCode() => LegacyEquivalence.GetHashCode();
    public static bool operator ==(CanonicalLegacyEquivalenceReport l, CanonicalLegacyEquivalenceReport r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyEquivalenceReport l, CanonicalLegacyEquivalenceReport r) => !l.Equals(r);
}

public sealed class CanonicalDryRunEquivalenceReport : IEquatable<CanonicalDryRunEquivalenceReport>
{
    public CanonicalLegacyEquivalenceReport LegacyEquivalence { get; }
    public CanonicalDryRunEquivalenceReport(CanonicalLegacyEquivalenceReport? legacyEquivalence = null)
    {
        LegacyEquivalence = legacyEquivalence ?? new CanonicalLegacyEquivalenceReport();
    }
    public override bool Equals(object? obj) => obj is CanonicalDryRunEquivalenceReport other && Equals(other);
    public bool Equals(CanonicalDryRunEquivalenceReport? other) =>
        other is not null && LegacyEquivalence.Equals(other.LegacyEquivalence);
    public override int GetHashCode() => LegacyEquivalence.GetHashCode();
    public static bool operator ==(CanonicalDryRunEquivalenceReport l, CanonicalDryRunEquivalenceReport r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunEquivalenceReport l, CanonicalDryRunEquivalenceReport r) => !l.Equals(r);
}

public sealed class CanonicalRetirementReadinessReport : IEquatable<CanonicalRetirementReadinessReport>
{
    public bool Ready { get; }
    public string[] Blockers { get; }
    public CanonicalRetirementReadinessReport(bool ready = true, string[]? blockers = null)
    {
        Ready = ready;
        Blockers = blockers ?? Array.Empty<string>();
    }
    public override bool Equals(object? obj) => obj is CanonicalRetirementReadinessReport other && Equals(other);
    public bool Equals(CanonicalRetirementReadinessReport? other) =>
        other is not null && Ready == other.Ready && Blockers.SequenceEqual(other.Blockers);
    public override int GetHashCode() => HashCode.Combine(Ready, Blockers.Length);
    public static bool operator ==(CanonicalRetirementReadinessReport l, CanonicalRetirementReadinessReport r) => l.Equals(r);
    public static bool operator !=(CanonicalRetirementReadinessReport l, CanonicalRetirementReadinessReport r) => !l.Equals(r);
}

// Forward types that will be fully defined in CanonicalProductionExecution.cs
// These are needed by CanonicalProductionPortSet and interface extensions
namespace Rokurics.CanonicalCore
{
    // CanonicalRollbackCheckpoint, CanonicalRollbackResult are defined in CanonicalProductionExecution.cs
    // Compile-time references are handled via the shared namespace
}

// ─── CanonicalProductionPorts Enums ──────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionDomain
{
    recordingMetadata,
    recordingAudio,
    generatedArtifacts,
    folders,
    studyItems,
    standaloneNotes,
    tombstones,
    conflicts,
    apply,
    fileRuntime,
    transportRuntime,
    uploadRuntime,
    objectProjection,
    inventory,
    uiIntegration
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionPortKind
{
    file,
    transport,
    upload,
    apply,
    syncClock,
    diagnostics,
    capability
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionOperation
{
    metadataSnapshotRead,
    metadataRead,
    metadataWrite,
    artifactDescriptorRead,
    artifactRead,
    artifactWriteAtomic,
    artifactVerify,
    artifactBytesReadDryRun,
    logicalTokenResolve,
    containmentVerify,
    atomicWriteProject,
    atomicWriteExecute,
    artifactList,
    objectList,
    hashCompute,
    rollbackWrite,
    tombstoneApplyProject,
    tombstoneMark,
    physicalDeleteSuppressed,
    routeEnvelopeBuild,
    signedRequestBuild,
    requestSend,
    responseReceive,
    responseVerify,
    manifestExchange,
    artifactRequest,
    applyMetadataSend,
    uploadSessionStart,
    uploadSessionQuery,
    uploadChunkSend,
    uploadSessionFinalize,
    uploadSessionCancel,
    routeResponseDecode,
    uploadStartProject,
    resumableUploadStart,
    resumableUploadResume,
    uploadChunkProject,
    resumableUploadChunk,
    uploadFinalizeProject,
    resumableUploadFinalize,
    resumableUploadCancel,
    uploadLedgerRead,
    uploadLedgerWrite,
    retryProject,
    uploadStateRollback,
    applyMetadataProject,
    metadataApply,
    metadataSend,
    applyGeneratedArtifactProject,
    generatedArtifactApply,
    generatedArtifactRequest,
    objectTombstoneApply,
    libraryTombstoneApply,
    recordConflictProject,
    conflictRecord,
    preconditionVerify,
    postconditionVerify,
    applyRollback,
    diagnosticsRecord
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionCapability
{
    dryRunOnly,
    rootBoundFileAccess,
    rootBoundRead,
    rootBoundWrite,
    logicalTokenValidation,
    containmentVerification,
    atomicWriteProjection,
    atomicWriteExecution,
    hashSizeVerification,
    streamingHash,
    rollbackCheckpoint,
    noPhysicalDelete,
    routeSigning,
    routeVerification,
    externalSignerRequired,
    signedRequestExecution,
    responseVerification,
    resumableUploadProjection,
    resumableUploadExecution,
    chunkResumeProjection,
    confirmedBytesQuery,
    finalizationProjection,
    finalizationExecution,
    retryProjection,
    uploadLedgerMutation,
    metadataApplyProjection,
    metadataApplyExecution,
    generatedArtifactApplyProjection,
    generatedArtifactApplyExecution,
    tombstoneApplyProjection,
    tombstoneApplyExecution,
    conflictRecordProjection,
    conflictRecordExecution,
    preconditionVerification,
    postconditionVerification,
    inMemoryDiagnostics,
    productionDiagnostics
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionPortError
{
    missingPort,
    capabilityMissing,
    unsafeLogicalToken,
    productionMutationAttempted,
    networkExecutionSuppressed,
    unsupportedObject,
    fullContentRejected,
    routeBypassRisk,
    pathEscapeRisk
}

public sealed class CanonicalProductionPortException : Exception, IEquatable<CanonicalProductionPortException>
{
    public CanonicalProductionPortError ErrorKind { get; }
    public CanonicalProductionPortKind? PortKind { get; }
    public CanonicalProductionDomain? Domain { get; }
    public CanonicalProductionCapability? Capability { get; }
    public string Detail { get; }

    public CanonicalProductionPortException(CanonicalProductionPortError errorKind, string message,
        CanonicalProductionPortKind? portKind = null, CanonicalProductionDomain? domain = null,
        CanonicalProductionCapability? capability = null)
        : base(message)
    {
        ErrorKind = errorKind;
        PortKind = portKind;
        Domain = domain;
        Capability = capability;
        Detail = message;
    }

    public static CanonicalProductionPortException MissingPort(CanonicalProductionPortKind portKind) =>
        new(CanonicalProductionPortError.missingPort, $"Missing port: {portKind}", portKind: portKind);

    public static CanonicalProductionPortException CapabilityMissing(CanonicalProductionDomain domain, CanonicalProductionCapability capability) =>
        new(CanonicalProductionPortError.capabilityMissing,
            $"Capability missing: {domain}/{capability}", domain: domain, capability: capability);

    public static CanonicalProductionPortException UnsafeLogicalToken(string detail) =>
        new(CanonicalProductionPortError.unsafeLogicalToken, detail);

    public static CanonicalProductionPortException ProductionMutationAttempted(string detail) =>
        new(CanonicalProductionPortError.productionMutationAttempted, detail);

    public static CanonicalProductionPortException NetworkExecutionSuppressed(string detail) =>
        new(CanonicalProductionPortError.networkExecutionSuppressed, detail);

    public static CanonicalProductionPortException UnsupportedObject(string detail) =>
        new(CanonicalProductionPortError.unsupportedObject, detail);

    public static CanonicalProductionPortException FullContentRejected(string detail) =>
        new(CanonicalProductionPortError.fullContentRejected, detail);

    public static CanonicalProductionPortException RouteBypassRisk(string detail) =>
        new(CanonicalProductionPortError.routeBypassRisk, detail);

    public static CanonicalProductionPortException PathEscapeRisk(string detail) =>
        new(CanonicalProductionPortError.pathEscapeRisk, detail);

    public override bool Equals(object? obj) => obj is CanonicalProductionPortException other && Equals(other);
    public bool Equals(CanonicalProductionPortException? other) =>
        other is not null && ErrorKind == other.ErrorKind && Detail == other.Detail;
    public override int GetHashCode() => HashCode.Combine(ErrorKind, Detail);
    public static bool operator ==(CanonicalProductionPortException l, CanonicalProductionPortException r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionPortException l, CanonicalProductionPortException r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionDiagnosticEventKind
{
    canonicalProductionPortsDeclared,
    canonicalProductionSnapshotBuilt,
    canonicalDryRunStarted,
    canonicalDryRunCompleted,
    canonicalDryRunBlocked,
    canonicalDryRunDivergenceDetected,
    canonicalLegacyEquivalent,
    canonicalLegacyDivergent,
    canonicalProductionMigrationBlocked,
    canonicalEligibleForManualMigrationDesign,
    canonicalPortMissing,
    canonicalPortCapabilityMissing,
    canonicalDryRunWouldWriteButSuppressed,
    canonicalDryRunWouldUploadButSuppressed,
    canonicalDryRunWouldSendNetworkButSuppressed
}

public sealed class CanonicalProductionDiagnosticsEvent : IEquatable<CanonicalProductionDiagnosticsEvent>
{
    public string Id => string.Join("|", Kind, Domain?.ToString() ?? "", Action ?? "", Blocker ?? "", Reason ?? "");

    public CanonicalProductionDiagnosticEventKind Kind { get; }
    public CanonicalProductionDomain? Domain { get; }
    public string? Action { get; }
    public string? Blocker { get; }
    public string? Reason { get; }
    public string? HashPrefix { get; }
    public bool DryRun { get; }
    public CanonicalTimestamp GeneratedAt { get; }

    public CanonicalProductionDiagnosticsEvent(
        CanonicalProductionDiagnosticEventKind kind,
        CanonicalProductionDomain? domain = null,
        string? action = null,
        string? blocker = null,
        string? reason = null,
        CanonicalHash? hash = null,
        string? hashPrefix = null,
        bool dryRun = true,
        DateTime generatedAt = default)
    {
        Kind = kind;
        Domain = domain;
        Action = CanonicalProductionRedaction.SafeDiagnosticText(action);
        Blocker = CanonicalProductionRedaction.SafeDiagnosticText(blocker);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = hash is not null ? CanonicalProductionRedaction.HashPrefix(hash.Value.Value) :
                     CanonicalProductionRedaction.HashPrefix(hashPrefix);
        DryRun = dryRun;
        GeneratedAt = new CanonicalTimestamp(generatedAt == default ? DateTime.UtcNow : generatedAt);
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionDiagnosticsEvent other && Equals(other);
    public bool Equals(CanonicalProductionDiagnosticsEvent? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalProductionDiagnosticsEvent l, CanonicalProductionDiagnosticsEvent r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionDiagnosticsEvent l, CanonicalProductionDiagnosticsEvent r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionArtifactAvailability
{
    available,
    missing,
    unknown,
    unsupported
}

public sealed class CanonicalProductionArtifactDescriptor : IEquatable<CanonicalProductionArtifactDescriptor>
{
    public string Id => ArtifactID;

    public string ArtifactID { get; }
    public string ObjectID { get; }
    public CanonicalArtifact.Kind Kind { get; }
    public string? LogicalPathToken { get; }
    public string? LogicalName { get; }
    public string? ContentHashPrefix { get; }
    public long? ByteSize { get; }
    public CanonicalProductionArtifactAvailability Availability { get; }
    public string? UnsupportedReason { get; }

    public CanonicalProductionArtifactDescriptor(
        string artifactID,
        string objectID,
        CanonicalArtifact.Kind kind,
        string? logicalPathToken = null,
        string? logicalName = null,
        CanonicalHash? contentHash = null,
        string? contentHashPrefix = null,
        long? byteSize = null,
        CanonicalProductionArtifactAvailability availability = CanonicalProductionArtifactAvailability.unknown,
        string? unsupportedReason = null)
    {
        ArtifactID = CanonicalProductionRedaction.SafeIdentifier(artifactID, $"{kind}:unknown");
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        Kind = kind;
        LogicalPathToken = CanonicalProjectionContract.SafeLogicalPathToken(logicalPathToken);
        LogicalName = CanonicalProductionRedaction.SafeFileName(logicalName);
        ContentHashPrefix = contentHash is not null ? CanonicalProductionRedaction.HashPrefix(contentHash.Value.Value) :
                            CanonicalProductionRedaction.HashPrefix(contentHashPrefix);
        ByteSize = byteSize;
        Availability = availability;
        UnsupportedReason = CanonicalProductionRedaction.SafeDiagnosticText(unsupportedReason);
        if (LogicalPathToken is not null && this.LogicalPathToken is null && this.UnsupportedReason is null)
            UnsupportedReason = "unsafeLogicalPathToken";
    }

    public CanonicalProductionArtifactDescriptor(CanonicalArtifact artifact)
        : this(
            artifactID: artifact.ArtifactID,
            objectID: artifact.ObjectID,
            kind: artifact.ArtifactKind,
            logicalPathToken: artifact.LogicalPathToken,
            logicalName: artifact.LogicalName,
            contentHash: artifact.ContentHash,
            byteSize: artifact.ByteSize,
            availability: artifact.Availability switch
            {
                CanonicalArtifact.AvailabilityKind.available => CanonicalProductionArtifactAvailability.available,
                CanonicalArtifact.AvailabilityKind.availableWithoutHash => CanonicalProductionArtifactAvailability.available,
                CanonicalArtifact.AvailabilityKind.missing => CanonicalProductionArtifactAvailability.missing,
                CanonicalArtifact.AvailabilityKind.unknown => CanonicalProductionArtifactAvailability.unknown,
                _ => CanonicalProductionArtifactAvailability.unknown
            })
    {
    }

    public bool HasUnsafePathSignal =>
        LogicalPathToken is null && UnsupportedReason == "unsafeLogicalPathToken";

    public override bool Equals(object? obj) => obj is CanonicalProductionArtifactDescriptor other && Equals(other);
    public bool Equals(CanonicalProductionArtifactDescriptor? other) => other is not null && ArtifactID == other.ArtifactID;
    public override int GetHashCode() => ArtifactID.GetHashCode();
    public static bool operator ==(CanonicalProductionArtifactDescriptor l, CanonicalProductionArtifactDescriptor r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionArtifactDescriptor l, CanonicalProductionArtifactDescriptor r) => !l.Equals(r);
}

public sealed class CanonicalProductionReadProjection : IEquatable<CanonicalProductionReadProjection>
{
    public CanonicalFileReference Reference { get; }
    public bool WouldReadBytes { get; }
    public long? ByteSize { get; }
    public string? ContentHashPrefix { get; }
    public bool DryRun { get; }

    public CanonicalProductionReadProjection(
        CanonicalFileReference reference,
        bool wouldReadBytes,
        long? byteSize = null,
        CanonicalHash? contentHash = null,
        bool dryRun = true)
    {
        Reference = reference;
        WouldReadBytes = wouldReadBytes;
        ByteSize = byteSize;
        ContentHashPrefix = contentHash is not null ? CanonicalProductionRedaction.HashPrefix(contentHash.Value.Value) : null;
        DryRun = dryRun;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionReadProjection other && Equals(other);
    public bool Equals(CanonicalProductionReadProjection? other) =>
        other is not null && Reference.Equals(other.Reference) && WouldReadBytes == other.WouldReadBytes && DryRun == other.DryRun;
    public override int GetHashCode() => HashCode.Combine(Reference, WouldReadBytes, DryRun);
    public static bool operator ==(CanonicalProductionReadProjection l, CanonicalProductionReadProjection r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionReadProjection l, CanonicalProductionReadProjection r) => !l.Equals(r);
}

public sealed class CanonicalProductionWriteIntentProjection : IEquatable<CanonicalProductionWriteIntentProjection>
{
    public CanonicalFileReference Reference { get; }
    public CanonicalFilePurpose Purpose { get; }
    public bool WouldWrite { get; }
    public bool SuppressedBecauseDryRun { get; }
    public bool NoPhysicalDelete { get; }
    public long? ByteSize { get; }
    public string? ContentHashPrefix { get; }
    public CanonicalFileWriteDisposition? Disposition { get; }
    public string? Reason { get; }

    public CanonicalProductionWriteIntentProjection(
        CanonicalFileReference reference,
        CanonicalFilePurpose purpose,
        bool wouldWrite,
        bool suppressedBecauseDryRun = true,
        bool noPhysicalDelete = true,
        long? byteSize = null,
        CanonicalHash? contentHash = null,
        CanonicalFileWriteDisposition? disposition = null,
        string? reason = null)
    {
        Reference = reference;
        Purpose = purpose;
        WouldWrite = wouldWrite;
        SuppressedBecauseDryRun = suppressedBecauseDryRun;
        NoPhysicalDelete = noPhysicalDelete;
        ByteSize = byteSize;
        ContentHashPrefix = contentHash is not null ? CanonicalProductionRedaction.HashPrefix(contentHash.Value.Value) : null;
        Disposition = disposition;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionWriteIntentProjection other && Equals(other);
    public bool Equals(CanonicalProductionWriteIntentProjection? other) =>
        other is not null && Reference.Equals(other.Reference) && Purpose == other.Purpose && WouldWrite == other.WouldWrite;
    public override int GetHashCode() => HashCode.Combine(Reference, Purpose, WouldWrite);
    public static bool operator ==(CanonicalProductionWriteIntentProjection l, CanonicalProductionWriteIntentProjection r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionWriteIntentProjection l, CanonicalProductionWriteIntentProjection r) => !l.Equals(r);
}

public sealed class CanonicalProductionTransportRouteCapability : IEquatable<CanonicalProductionTransportRouteCapability>
{
    public CanonicalTransportRoute Route { get; }
    public bool RequiresSigning { get; }
    public bool RequiresVerification { get; }
    public bool DryRunOnly { get; }

    public CanonicalProductionTransportRouteCapability(
        CanonicalTransportRoute route,
        bool requiresSigning = true,
        bool requiresVerification = true,
        bool dryRunOnly = true)
    {
        Route = route;
        RequiresSigning = requiresSigning;
        RequiresVerification = requiresVerification;
        DryRunOnly = dryRunOnly;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionTransportRouteCapability other && Equals(other);
    public bool Equals(CanonicalProductionTransportRouteCapability? other) =>
        other is not null && Route == other.Route && RequiresSigning == other.RequiresSigning && RequiresVerification == other.RequiresVerification;
    public override int GetHashCode() => HashCode.Combine(Route, RequiresSigning, RequiresVerification);
    public static bool operator ==(CanonicalProductionTransportRouteCapability l, CanonicalProductionTransportRouteCapability r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionTransportRouteCapability l, CanonicalProductionTransportRouteCapability r) => !l.Equals(r);
}

public sealed class CanonicalProductionTransportEnvelopeDryRun : IEquatable<CanonicalProductionTransportEnvelopeDryRun>
{
    public CanonicalTransportRoute Route { get; }
    public string SourceNodeID { get; }
    public string DestinationNodeID { get; }
    public bool RequiresSigning { get; }
    public bool RequiresVerification { get; }
    public string? BodyHashPrefix { get; }
    public bool WouldSendNetwork { get; }
    public bool SuppressedBecauseDryRun { get; }
    public string Reason { get; }

    public CanonicalProductionTransportEnvelopeDryRun(
        CanonicalTransportRoute route,
        string sourceNodeID,
        string destinationNodeID,
        CanonicalHash? bodyHash = null,
        bool requiresSigning = true,
        bool requiresVerification = true,
        string reason = "networkSuppressedDryRun")
    {
        Route = route;
        SourceNodeID = CanonicalProductionRedaction.SafeIdentifier(sourceNodeID, "source:unknown");
        DestinationNodeID = CanonicalProductionRedaction.SafeIdentifier(destinationNodeID, "destination:unknown");
        RequiresSigning = requiresSigning;
        RequiresVerification = requiresVerification;
        BodyHashPrefix = bodyHash is not null ? CanonicalProductionRedaction.HashPrefix(bodyHash.Value.Value) : null;
        WouldSendNetwork = false;
        SuppressedBecauseDryRun = true;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? "networkSuppressedDryRun";
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionTransportEnvelopeDryRun other && Equals(other);
    public bool Equals(CanonicalProductionTransportEnvelopeDryRun? other) =>
        other is not null && Route == other.Route && SourceNodeID == other.SourceNodeID && DestinationNodeID == other.DestinationNodeID;
    public override int GetHashCode() => HashCode.Combine(Route, SourceNodeID, DestinationNodeID);
    public static bool operator ==(CanonicalProductionTransportEnvelopeDryRun l, CanonicalProductionTransportEnvelopeDryRun r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionTransportEnvelopeDryRun l, CanonicalProductionTransportEnvelopeDryRun r) => !l.Equals(r);
}

public sealed class CanonicalProductionUploadTrace : IEquatable<CanonicalProductionUploadTrace>
{
    public string ObjectID { get; }
    public string? ArtifactID { get; }
    public long? TotalBytes { get; }
    public string? TotalHashPrefix { get; }
    public int? ChunkSize { get; }
    public bool Resumable { get; }
    public bool WouldUpload { get; }
    public bool SuppressedBecauseDryRun { get; }
    public bool MappedToLegacyUploadCapability { get; }
    public CanonicalTransportRoute? Route { get; }
    public string Reason { get; }

    public CanonicalProductionUploadTrace(
        string objectID,
        string? artifactID = null,
        long? totalBytes = null,
        CanonicalHash? totalHash = null,
        int? chunkSize = null,
        bool resumable = true,
        CanonicalTransportRoute? route = CanonicalTransportRoute.uploadStart,
        string reason = "uploadSuppressedDryRun")
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        ArtifactID = artifactID is not null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        TotalBytes = totalBytes;
        TotalHashPrefix = totalHash is not null ? CanonicalProductionRedaction.HashPrefix(totalHash.Value.Value) : null;
        ChunkSize = chunkSize;
        Resumable = resumable;
        WouldUpload = false;
        SuppressedBecauseDryRun = true;
        MappedToLegacyUploadCapability = true;
        Route = route;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? "uploadSuppressedDryRun";
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionUploadTrace other && Equals(other);
    public bool Equals(CanonicalProductionUploadTrace? other) =>
        other is not null && ObjectID == other.ObjectID && ArtifactID == other.ArtifactID;
    public override int GetHashCode() => HashCode.Combine(ObjectID, ArtifactID);
    public static bool operator ==(CanonicalProductionUploadTrace l, CanonicalProductionUploadTrace r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionUploadTrace l, CanonicalProductionUploadTrace r) => !l.Equals(r);
}

public sealed class CanonicalProductionApplyTrace : IEquatable<CanonicalProductionApplyTrace>
{
    public string ActionID { get; }
    public CanonicalApplyActionKind Kind { get; }
    public CanonicalApplyTarget Target { get; }
    public CanonicalApplyBridgeHint? BridgeHint { get; }
    public bool WouldWrite { get; }
    public bool WouldCallApplySyncManifest { get; }
    public bool SuppressedBecauseDryRun { get; }
    public string Reason { get; }

    public CanonicalProductionApplyTrace(CanonicalApplyAction action, bool wouldCallApplySyncManifest = false, string reason = "applySuppressedDryRun")
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(action.ActionID, action.Kind.ToString());
        Kind = action.Kind;
        Target = action.Target;
        BridgeHint = action.BridgeHint;
        WouldWrite = true;
        WouldCallApplySyncManifest = wouldCallApplySyncManifest;
        SuppressedBecauseDryRun = true;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? "applySuppressedDryRun";
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionApplyTrace other && Equals(other);
    public bool Equals(CanonicalProductionApplyTrace? other) =>
        other is not null && ActionID == other.ActionID;
    public override int GetHashCode() => ActionID.GetHashCode();
    public static bool operator ==(CanonicalProductionApplyTrace l, CanonicalProductionApplyTrace r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionApplyTrace l, CanonicalProductionApplyTrace r) => !l.Equals(r);
}

public sealed class CanonicalProductionUnsupportedFact : IEquatable<CanonicalProductionUnsupportedFact>
{
    public string Id => string.Join("|", Domain.ToString(), ObjectID, Reason);

    public string ObjectID { get; }
    public CanonicalProductionDomain Domain { get; }
    public string Reason { get; }

    public CanonicalProductionUnsupportedFact(string objectID, CanonicalProductionDomain domain, string reason)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown");
        Domain = domain;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? "unsupported";
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionUnsupportedFact other && Equals(other);
    public bool Equals(CanonicalProductionUnsupportedFact? other) =>
        other is not null && ObjectID == other.ObjectID && Domain == other.Domain && Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(ObjectID, Domain, Reason);
    public static bool operator ==(CanonicalProductionUnsupportedFact l, CanonicalProductionUnsupportedFact r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionUnsupportedFact l, CanonicalProductionUnsupportedFact r) => !l.Equals(r);
}

public sealed class CanonicalLegacyActionSnapshot : IEquatable<CanonicalLegacyActionSnapshot>
{
    public Dictionary<CanonicalProductionDomain, List<string>> ActionIDsByDomain { get; }

    public CanonicalLegacyActionSnapshot(Dictionary<CanonicalProductionDomain, List<string>>? actionIDsByDomain = null)
    {
        ActionIDsByDomain = new Dictionary<CanonicalProductionDomain, List<string>>();
        if (actionIDsByDomain is not null)
        {
            foreach (var kv in actionIDsByDomain)
            {
                ActionIDsByDomain[kv.Key] = Normalized(kv.Value);
            }
        }
    }

    public static readonly CanonicalLegacyActionSnapshot Empty = new();

    public List<string> ActionIDs(CanonicalProductionDomain domain) =>
        ActionIDsByDomain.TryGetValue(domain, out var ids) ? ids : new List<string>();

    public HashSet<string> ActionIDSet(CanonicalProductionDomain domain) =>
        new(ActionIDs(domain));

    public CanonicalLegacyActionSnapshot Adding(List<string> ids, CanonicalProductionDomain domain)
    {
        var next = new Dictionary<CanonicalProductionDomain, List<string>>(ActionIDsByDomain);
        var existing = next.TryGetValue(domain, out var current) ? current : new List<string>();
        next[domain] = Normalized(existing.Concat(ids).ToList());
        return new CanonicalLegacyActionSnapshot(next);
    }

    private static List<string> Normalized(List<string> ids) =>
        new HashSet<string>(ids
            .Select(id => CanonicalProductionRedaction.SafeDiagnosticText(id))
            .Where(id => id is not null)
            .Cast<string>())
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

    public override bool Equals(object? obj) => obj is CanonicalLegacyActionSnapshot other && Equals(other);
    public bool Equals(CanonicalLegacyActionSnapshot? other) =>
        other is not null && ActionIDsByDomain.Count == other.ActionIDsByDomain.Count &&
        ActionIDsByDomain.All(kv => other.ActionIDsByDomain.TryGetValue(kv.Key, out var v) && kv.Value.SequenceEqual(v));
    public override int GetHashCode() => ActionIDsByDomain.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key, kv.Value.Count));
    public static bool operator ==(CanonicalLegacyActionSnapshot l, CanonicalLegacyActionSnapshot r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyActionSnapshot l, CanonicalLegacyActionSnapshot r) => !l.Equals(r);
}

public sealed class CanonicalRuntimeNodeState : IEquatable<CanonicalRuntimeNodeState>
{
    public CanonicalNode Node { get; }
    public CanonicalManifest Manifest { get; }
    public CanonicalTransferProjection TransferProjection { get; }
    public CanonicalInventoryCoverageReport InventoryCoverage { get; }
    public CanonicalRetirementReadinessReport RetirementReadiness { get; }
    public CanonicalLibraryProjection ObjectProjection { get; }
    public CanonicalProductionUnsupportedFact[] UnsupportedFacts { get; }
    public CanonicalProductionDiagnosticsEvent[] Diagnostics { get; }

    public CanonicalRuntimeNodeState(
        CanonicalNode node,
        CanonicalManifest manifest,
        CanonicalTransferProjection? transferProjection = null,
        CanonicalInventoryCoverageReport inventoryCoverage = null!,
        CanonicalRetirementReadinessReport retirementReadiness = null!,
        CanonicalLibraryProjection? objectProjection = null,
        CanonicalProductionUnsupportedFact[]? unsupportedFacts = null,
        CanonicalProductionDiagnosticsEvent[]? diagnostics = null)
    {
        Node = node;
        Manifest = manifest;
        TransferProjection = transferProjection ?? new CanonicalTransferProjection();
        InventoryCoverage = inventoryCoverage ?? new CanonicalInventoryCoverageReport(0, 0, 0, 0, 0, 0, 0, 0);
        RetirementReadiness = retirementReadiness ?? new CanonicalRetirementReadinessReport();
        ObjectProjection = objectProjection ?? new CanonicalLibraryProjection(
            Array.Empty<CanonicalRecordingProjection>(),
            Array.Empty<CanonicalFolderProjection>(),
            Array.Empty<CanonicalStudyItemProjection>(),
            new CanonicalTimestamp(DateTime.UtcNow));
        UnsupportedFacts = (unsupportedFacts ?? Array.Empty<CanonicalProductionUnsupportedFact>())
            .OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        Diagnostics = diagnostics ?? Array.Empty<CanonicalProductionDiagnosticsEvent>();
    }

    public override bool Equals(object? obj) => obj is CanonicalRuntimeNodeState other && Equals(other);
    public bool Equals(CanonicalRuntimeNodeState? other) =>
        other is not null && Node.Equals(other.Node) && Manifest.Equals(other.Manifest);
    public override int GetHashCode() => HashCode.Combine(Node, Manifest);
    public static bool operator ==(CanonicalRuntimeNodeState l, CanonicalRuntimeNodeState r) => l.Equals(r);
    public static bool operator !=(CanonicalRuntimeNodeState l, CanonicalRuntimeNodeState r) => !l.Equals(r);
}

public sealed class CanonicalProductionSnapshot : IEquatable<CanonicalProductionSnapshot>
{
    public CanonicalNode Node { get; }
    public CanonicalManifest Manifest { get; }
    public CanonicalRuntimeNodeState RuntimeNodeState { get; }
    public CanonicalLegacyActionSnapshot LegacyActions { get; }
    public CanonicalProductionUnsupportedFact[] UnsupportedFacts { get; }
    public CanonicalProductionDiagnosticsEvent[] Diagnostics { get; }

    public CanonicalProductionSnapshot(
        CanonicalNode node,
        CanonicalManifest manifest,
        CanonicalRuntimeNodeState runtimeNodeState,
        CanonicalLegacyActionSnapshot? legacyActions = null,
        CanonicalProductionUnsupportedFact[]? unsupportedFacts = null,
        CanonicalProductionDiagnosticsEvent[]? diagnostics = null)
    {
        Node = node;
        Manifest = manifest;
        RuntimeNodeState = runtimeNodeState;
        LegacyActions = legacyActions ?? CanonicalLegacyActionSnapshot.Empty;
        UnsupportedFacts = (unsupportedFacts ?? Array.Empty<CanonicalProductionUnsupportedFact>())
            .OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        Diagnostics = diagnostics ?? Array.Empty<CanonicalProductionDiagnosticsEvent>();
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionSnapshot other && Equals(other);
    public bool Equals(CanonicalProductionSnapshot? other) =>
        other is not null && Node.Equals(other.Node) && Manifest.Equals(other.Manifest);
    public override int GetHashCode() => HashCode.Combine(Node, Manifest);
    public static bool operator ==(CanonicalProductionSnapshot l, CanonicalProductionSnapshot r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionSnapshot l, CanonicalProductionSnapshot r) => !l.Equals(r);
}

public sealed class CanonicalProductionCapabilitySummary : IEquatable<CanonicalProductionCapabilitySummary>
{
    public string NodeID { get; }
    public CanonicalProductionCapability[] Capabilities { get; }
    public CanonicalProductionDomain[] SupportedDomains { get; }
    public bool DryRunOnly { get; }

    public CanonicalProductionCapabilitySummary(
        string nodeID,
        CanonicalProductionCapability[]? capabilities = null,
        CanonicalProductionDomain[]? supportedDomains = null,
        bool dryRunOnly = true)
    {
        NodeID = CanonicalProductionRedaction.SafeIdentifier(nodeID, "node:unknown");
        Capabilities = (capabilities is not null
            ? new HashSet<CanonicalProductionCapability>(capabilities)
            : new HashSet<CanonicalProductionCapability>())
            .OrderBy(c => c.ToString(), StringComparer.Ordinal)
            .ToArray();
        SupportedDomains = (supportedDomains is not null
            ? new HashSet<CanonicalProductionDomain>(supportedDomains)
            : new HashSet<CanonicalProductionDomain>())
            .OrderBy(d => d.ToString(), StringComparer.Ordinal)
            .ToArray();
        DryRunOnly = dryRunOnly;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionCapabilitySummary other && Equals(other);
    public bool Equals(CanonicalProductionCapabilitySummary? other) =>
        other is not null && NodeID == other.NodeID && Capabilities.SequenceEqual(other.Capabilities);
    public override int GetHashCode() => HashCode.Combine(NodeID, Capabilities.Length);
    public static bool operator ==(CanonicalProductionCapabilitySummary l, CanonicalProductionCapabilitySummary r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionCapabilitySummary l, CanonicalProductionCapabilitySummary r) => !l.Equals(r);
}

public sealed class CanonicalProductionPortReadiness : IEquatable<CanonicalProductionPortReadiness>
{
    public CanonicalTimestamp GeneratedAt { get; }
    public Dictionary<CanonicalProductionPortKind, bool> DeclaredPorts { get; }
    public CanonicalProductionPortKind[] MissingPorts { get; }
    public bool DryRunOnly { get; }

    public CanonicalProductionPortReadiness(
        Dictionary<CanonicalProductionPortKind, bool> declaredPorts,
        CanonicalProductionPortKind[] missingPorts,
        bool dryRunOnly = true,
        DateTime generatedAt = default)
    {
        GeneratedAt = new CanonicalTimestamp(generatedAt == default ? DateTime.UtcNow : generatedAt);
        DeclaredPorts = declaredPorts;
        MissingPorts = new HashSet<CanonicalProductionPortKind>(missingPorts)
            .OrderBy(p => p.ToString(), StringComparer.Ordinal)
            .ToArray();
        DryRunOnly = dryRunOnly;
    }

    public bool HasAllRequiredDryRunPorts =>
        !MissingPorts.Any(p => p is CanonicalProductionPortKind.file or CanonicalProductionPortKind.transport
                                  or CanonicalProductionPortKind.upload or CanonicalProductionPortKind.apply);

    public bool HasAllRequiredProductionPorts =>
        HasAllRequiredDryRunPorts && !DryRunOnly;

    public override bool Equals(object? obj) => obj is CanonicalProductionPortReadiness other && Equals(other);
    public bool Equals(CanonicalProductionPortReadiness? other) =>
        other is not null && MissingPorts.SequenceEqual(other.MissingPorts) && DryRunOnly == other.DryRunOnly;
    public override int GetHashCode() => HashCode.Combine(DryRunOnly, MissingPorts.Length);
    public static bool operator ==(CanonicalProductionPortReadiness l, CanonicalProductionPortReadiness r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionPortReadiness l, CanonicalProductionPortReadiness r) => !l.Equals(r);
}

public sealed class CanonicalProductionFileVerificationEvidence : IEquatable<CanonicalProductionFileVerificationEvidence>
{
    public CanonicalFileReference Reference { get; }
    public CanonicalPathResolutionResult Resolution { get; }
    public string? ExpectedHashPrefix { get; }
    public string? ActualHashPrefix { get; }
    public long? ExpectedByteSize { get; }
    public long? ActualByteSize { get; }
    public bool HashVerified { get; }
    public bool SizeVerified { get; }
    public bool ComputedStreaming { get; }

    public CanonicalProductionFileVerificationEvidence(
        CanonicalFileReference reference,
        CanonicalPathResolutionResult resolution,
        CanonicalHash? expectedHash = null,
        CanonicalHash? actualHash = null,
        long? expectedByteSize = null,
        long? actualByteSize = null,
        bool computedStreaming = false)
    {
        Reference = reference;
        Resolution = resolution;
        ExpectedHashPrefix = expectedHash is not null ? CanonicalProductionRedaction.HashPrefix(expectedHash.Value.Value) : null;
        ActualHashPrefix = actualHash is not null ? CanonicalProductionRedaction.HashPrefix(actualHash.Value.Value) : null;
        ExpectedByteSize = expectedByteSize;
        ActualByteSize = actualByteSize;
        HashVerified = expectedHash is null || Equals(expectedHash, actualHash);
        SizeVerified = expectedByteSize is null || expectedByteSize == actualByteSize;
        ComputedStreaming = computedStreaming;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionFileVerificationEvidence other && Equals(other);
    public bool Equals(CanonicalProductionFileVerificationEvidence? other) =>
        other is not null && Reference.Equals(other.Reference) && HashVerified == other.HashVerified && SizeVerified == other.SizeVerified;
    public override int GetHashCode() => HashCode.Combine(Reference, HashVerified, SizeVerified);
    public static bool operator ==(CanonicalProductionFileVerificationEvidence l, CanonicalProductionFileVerificationEvidence r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionFileVerificationEvidence l, CanonicalProductionFileVerificationEvidence r) => !l.Equals(r);
}

public sealed class CanonicalProductionFileReadResult : IEquatable<CanonicalProductionFileReadResult>
{
    public byte[] Bytes { get; }
    public CanonicalFilePurpose Purpose { get; }
    public CanonicalProductionFileVerificationEvidence Evidence { get; }
    public CanonicalMetadataBlob? MetadataBlob { get; }
    public bool Tombstoned { get; }

    public CanonicalProductionFileReadResult(
        byte[] bytes,
        CanonicalFilePurpose purpose,
        CanonicalProductionFileVerificationEvidence evidence,
        CanonicalMetadataBlob? metadataBlob = null,
        bool tombstoned = false)
    {
        Bytes = bytes;
        Purpose = purpose;
        Evidence = evidence;
        MetadataBlob = metadataBlob;
        Tombstoned = tombstoned;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionFileReadResult other && Equals(other);
    public bool Equals(CanonicalProductionFileReadResult? other) =>
        other is not null && Bytes.SequenceEqual(other.Bytes) && Purpose == other.Purpose;
    public override int GetHashCode() => HashCode.Combine(Bytes.Length, Purpose);
    public static bool operator ==(CanonicalProductionFileReadResult l, CanonicalProductionFileReadResult r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionFileReadResult l, CanonicalProductionFileReadResult r) => !l.Equals(r);
}

public sealed class CanonicalProductionFileWriteResult : IEquatable<CanonicalProductionFileWriteResult>
{
    public CanonicalFileWriteDisposition Disposition { get; }
    public CanonicalFilePurpose Purpose { get; }
    public CanonicalProductionFileVerificationEvidence Evidence { get; }
    public string? RollbackCheckpointID { get; }
    public bool Tombstoned { get; }

    public CanonicalProductionFileWriteResult(
        CanonicalFileWriteDisposition disposition,
        CanonicalFilePurpose purpose,
        CanonicalProductionFileVerificationEvidence evidence,
        string? rollbackCheckpointID = null,
        bool tombstoned = false)
    {
        Disposition = disposition;
        Purpose = purpose;
        Evidence = evidence;
        RollbackCheckpointID = rollbackCheckpointID;
        Tombstoned = tombstoned;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionFileWriteResult other && Equals(other);
    public bool Equals(CanonicalProductionFileWriteResult? other) =>
        other is not null && Disposition == other.Disposition && Purpose == other.Purpose;
    public override int GetHashCode() => HashCode.Combine(Disposition, Purpose);
    public static bool operator ==(CanonicalProductionFileWriteResult l, CanonicalProductionFileWriteResult r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionFileWriteResult l, CanonicalProductionFileWriteResult r) => !l.Equals(r);
}

public sealed class CanonicalProductionMetadataReadRequest : IEquatable<CanonicalProductionMetadataReadRequest>
{
    public string ObjectID { get; }
    public CanonicalFileReference Reference { get; }

    public CanonicalProductionMetadataReadRequest(string objectID, CanonicalFileReference reference)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        Reference = reference;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionMetadataReadRequest other && Equals(other);
    public bool Equals(CanonicalProductionMetadataReadRequest? other) =>
        other is not null && ObjectID == other.ObjectID;
    public override int GetHashCode() => ObjectID.GetHashCode();
    public static bool operator ==(CanonicalProductionMetadataReadRequest l, CanonicalProductionMetadataReadRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionMetadataReadRequest l, CanonicalProductionMetadataReadRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionArtifactReadRequest : IEquatable<CanonicalProductionArtifactReadRequest>
{
    public string ArtifactID { get; }
    public CanonicalFileReference Reference { get; }
    public CanonicalHash? ExpectedContentHash { get; }
    public long? ExpectedByteSize { get; }

    public CanonicalProductionArtifactReadRequest(
        string artifactID,
        CanonicalFileReference reference,
        CanonicalHash? expectedContentHash = null,
        long? expectedByteSize = null)
    {
        ArtifactID = CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown");
        Reference = reference;
        ExpectedContentHash = expectedContentHash;
        ExpectedByteSize = expectedByteSize;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionArtifactReadRequest other && Equals(other);
    public bool Equals(CanonicalProductionArtifactReadRequest? other) =>
        other is not null && ArtifactID == other.ArtifactID;
    public override int GetHashCode() => ArtifactID.GetHashCode();
    public static bool operator ==(CanonicalProductionArtifactReadRequest l, CanonicalProductionArtifactReadRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionArtifactReadRequest l, CanonicalProductionArtifactReadRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionArtifactVerifyRequest : IEquatable<CanonicalProductionArtifactVerifyRequest>
{
    public CanonicalFileReference Reference { get; }
    public CanonicalHash? ExpectedContentHash { get; }
    public long? ExpectedByteSize { get; }
    public bool RequireStreamingHash { get; }

    public CanonicalProductionArtifactVerifyRequest(
        CanonicalFileReference reference,
        CanonicalHash? expectedContentHash = null,
        long? expectedByteSize = null,
        bool requireStreamingHash = true)
    {
        Reference = reference;
        ExpectedContentHash = expectedContentHash;
        ExpectedByteSize = expectedByteSize;
        RequireStreamingHash = requireStreamingHash;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionArtifactVerifyRequest other && Equals(other);
    public bool Equals(CanonicalProductionArtifactVerifyRequest? other) =>
        other is not null && Reference.Equals(other.Reference);
    public override int GetHashCode() => Reference.GetHashCode();
    public static bool operator ==(CanonicalProductionArtifactVerifyRequest l, CanonicalProductionArtifactVerifyRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionArtifactVerifyRequest l, CanonicalProductionArtifactVerifyRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionHashRequest : IEquatable<CanonicalProductionHashRequest>
{
    public CanonicalFileReference Reference { get; }
    public bool RequireStreaming { get; }
    public long? ExpectedByteSize { get; }

    public CanonicalProductionHashRequest(CanonicalFileReference reference, bool requireStreaming = true, long? expectedByteSize = null)
    {
        Reference = reference;
        RequireStreaming = requireStreaming;
        ExpectedByteSize = expectedByteSize;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionHashRequest other && Equals(other);
    public bool Equals(CanonicalProductionHashRequest? other) =>
        other is not null && Reference.Equals(other.Reference) && RequireStreaming == other.RequireStreaming;
    public override int GetHashCode() => HashCode.Combine(Reference, RequireStreaming);
    public static bool operator ==(CanonicalProductionHashRequest l, CanonicalProductionHashRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionHashRequest l, CanonicalProductionHashRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionHashResult : IEquatable<CanonicalProductionHashResult>
{
    public CanonicalHash ContentHash { get; }
    public long ByteSize { get; }
    public bool ComputedStreaming { get; }
    public CanonicalProductionFileVerificationEvidence Evidence { get; }

    public CanonicalProductionHashResult(
        CanonicalHash contentHash,
        long byteSize,
        bool computedStreaming,
        CanonicalProductionFileVerificationEvidence evidence)
    {
        ContentHash = contentHash;
        ByteSize = byteSize;
        ComputedStreaming = computedStreaming;
        Evidence = evidence;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionHashResult other && Equals(other);
    public bool Equals(CanonicalProductionHashResult? other) =>
        other is not null && ContentHash.Equals(other.ContentHash) && ByteSize == other.ByteSize;
    public override int GetHashCode() => HashCode.Combine(ContentHash, ByteSize);
    public static bool operator ==(CanonicalProductionHashResult l, CanonicalProductionHashResult r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionHashResult l, CanonicalProductionHashResult r) => !l.Equals(r);
}

public sealed class CanonicalProductionTombstoneRequest : IEquatable<CanonicalProductionTombstoneRequest>
{
    public CanonicalFileReference Reference { get; }
    public string Reason { get; }

    public CanonicalProductionTombstoneRequest(CanonicalFileReference reference, string reason)
    {
        Reference = reference;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? "softTombstone";
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionTombstoneRequest other && Equals(other);
    public bool Equals(CanonicalProductionTombstoneRequest? other) =>
        other is not null && Reference.Equals(other.Reference);
    public override int GetHashCode() => Reference.GetHashCode();
    public static bool operator ==(CanonicalProductionTombstoneRequest l, CanonicalProductionTombstoneRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionTombstoneRequest l, CanonicalProductionTombstoneRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionFileRollbackRequest : IEquatable<CanonicalProductionFileRollbackRequest>
{
    public string CheckpointID { get; }
    public CanonicalFileReference Reference { get; }

    public CanonicalProductionFileRollbackRequest(string checkpointID, CanonicalFileReference reference)
    {
        CheckpointID = CanonicalProductionRedaction.SafeIdentifier(checkpointID, "rollback-checkpoint");
        Reference = reference;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionFileRollbackRequest other && Equals(other);
    public bool Equals(CanonicalProductionFileRollbackRequest? other) =>
        other is not null && CheckpointID == other.CheckpointID;
    public override int GetHashCode() => CheckpointID.GetHashCode();
    public static bool operator ==(CanonicalProductionFileRollbackRequest l, CanonicalProductionFileRollbackRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionFileRollbackRequest l, CanonicalProductionFileRollbackRequest r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionHTTPMethod
{
    GET,
    POST
}

public sealed class CanonicalProductionTransportBuildRequest : IEquatable<CanonicalProductionTransportBuildRequest>
{
    public CanonicalNode Source { get; }
    public CanonicalNode Destination { get; }
    public CanonicalTransportRoute Route { get; }
    public CanonicalProductionHTTPMethod Method { get; }
    public string ExistingRoutePath { get; }
    public string ContentType { get; }
    public byte[] Body { get; }
    public CanonicalTimestamp Timestamp { get; }
    public string Nonce { get; }
    public bool RequiresExternalSigner { get; }

    public CanonicalProductionTransportBuildRequest(
        CanonicalNode source,
        CanonicalNode destination,
        CanonicalTransportRoute route,
        CanonicalProductionHTTPMethod method = CanonicalProductionHTTPMethod.POST,
        string existingRoutePath = "",
        string contentType = "application/json",
        byte[]? body = null,
        DateTime timestamp = default,
        string nonce = "",
        bool requiresExternalSigner = true)
    {
        Source = source;
        Destination = destination;
        Route = route;
        Method = method;
        ExistingRoutePath = CanonicalProductionRedaction.SafeDiagnosticText(existingRoutePath) ?? route.ToString();
        ContentType = CanonicalProductionRedaction.SafeDiagnosticText(contentType) ?? "application/json";
        Body = body ?? Array.Empty<byte>();
        Timestamp = new CanonicalTimestamp(timestamp == default ? DateTime.UtcNow : timestamp);
        Nonce = CanonicalProductionRedaction.SafeIdentifier(nonce, "nonce:required");
        RequiresExternalSigner = requiresExternalSigner;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionTransportBuildRequest other && Equals(other);
    public bool Equals(CanonicalProductionTransportBuildRequest? other) =>
        other is not null && Source.Equals(other.Source) && Destination.Equals(other.Destination) &&
        Route == other.Route && Nonce == other.Nonce;
    public override int GetHashCode() => HashCode.Combine(Source, Destination, Route, Nonce);
    public static bool operator ==(CanonicalProductionTransportBuildRequest l, CanonicalProductionTransportBuildRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionTransportBuildRequest l, CanonicalProductionTransportBuildRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionSignedRequest : IEquatable<CanonicalProductionSignedRequest>
{
    public CanonicalProductionTransportBuildRequest BuildRequest { get; }
    public CanonicalHash BodyHash { get; }
    public string? SignaturePrefix { get; }
    public string? SignerDescription { get; }

    public CanonicalProductionSignedRequest(
        CanonicalProductionTransportBuildRequest buildRequest,
        CanonicalHash? bodyHash = null,
        string? signature = null,
        string? signerDescription = null)
    {
        BuildRequest = buildRequest;
        BodyHash = bodyHash ?? CanonicalTransportEnvelope.Hash(buildRequest.Body);
        SignaturePrefix = CanonicalProductionRedaction.HashPrefix(signature);
        SignerDescription = CanonicalProductionRedaction.SafeDiagnosticText(signerDescription);
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionSignedRequest other && Equals(other);
    public bool Equals(CanonicalProductionSignedRequest? other) =>
        other is not null && BuildRequest.Equals(other.BuildRequest) && BodyHash.Equals(other.BodyHash);
    public override int GetHashCode() => HashCode.Combine(BuildRequest, BodyHash);
    public static bool operator ==(CanonicalProductionSignedRequest l, CanonicalProductionSignedRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionSignedRequest l, CanonicalProductionSignedRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionTransportExchangeResult : IEquatable<CanonicalProductionTransportExchangeResult>
{
    public CanonicalProductionSignedRequest Request { get; }
    public CanonicalTransportResponse Response { get; }
    public bool ResponseVerified { get; }
    public bool UsedExistingRoute { get; }
    public CanonicalProductionSideEffect? SideEffect { get; }

    public CanonicalProductionTransportExchangeResult(
        CanonicalProductionSignedRequest request,
        CanonicalTransportResponse response,
        bool responseVerified,
        bool usedExistingRoute,
        CanonicalProductionSideEffect? sideEffect = null)
    {
        Request = request;
        Response = response;
        ResponseVerified = responseVerified;
        UsedExistingRoute = usedExistingRoute;
        SideEffect = sideEffect;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionTransportExchangeResult other && Equals(other);
    public bool Equals(CanonicalProductionTransportExchangeResult? other) =>
        other is not null && Request.Equals(other.Request);
    public override int GetHashCode() => Request.GetHashCode();
    public static bool operator ==(CanonicalProductionTransportExchangeResult l, CanonicalProductionTransportExchangeResult r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionTransportExchangeResult l, CanonicalProductionTransportExchangeResult r) => !l.Equals(r);
}

public sealed class CanonicalProductionTransportVerification : IEquatable<CanonicalProductionTransportVerification>
{
    public CanonicalTransportRoute Route { get; }
    public bool BodyHashVerified { get; }
    public bool ResponseHashVerified { get; }
    public bool TimestampAccepted { get; }
    public bool ExternalVerifierRequired { get; }

    public CanonicalProductionTransportVerification(
        CanonicalTransportRoute route,
        bool bodyHashVerified,
        bool responseHashVerified,
        bool timestampAccepted,
        bool externalVerifierRequired)
    {
        Route = route;
        BodyHashVerified = bodyHashVerified;
        ResponseHashVerified = responseHashVerified;
        TimestampAccepted = timestampAccepted;
        ExternalVerifierRequired = externalVerifierRequired;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionTransportVerification other && Equals(other);
    public bool Equals(CanonicalProductionTransportVerification? other) =>
        other is not null && Route == other.Route;
    public override int GetHashCode() => Route.GetHashCode();
    public static bool operator ==(CanonicalProductionTransportVerification l, CanonicalProductionTransportVerification r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionTransportVerification l, CanonicalProductionTransportVerification r) => !l.Equals(r);
}

public sealed class CanonicalProductionManifestExchangeRequest : IEquatable<CanonicalProductionManifestExchangeRequest>
{
    public CanonicalManifest LocalManifest { get; }
    public CanonicalNode PeerNode { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }

    public CanonicalProductionManifestExchangeRequest(
        CanonicalManifest localManifest,
        CanonicalNode peerNode,
        CanonicalSyncPlanTrigger trigger)
    {
        LocalManifest = localManifest;
        PeerNode = peerNode;
        Trigger = trigger;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionManifestExchangeRequest other && Equals(other);
    public bool Equals(CanonicalProductionManifestExchangeRequest? other) =>
        other is not null && LocalManifest.Equals(other.LocalManifest) && PeerNode.Equals(other.PeerNode);
    public override int GetHashCode() => HashCode.Combine(LocalManifest, PeerNode);
    public static bool operator ==(CanonicalProductionManifestExchangeRequest l, CanonicalProductionManifestExchangeRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionManifestExchangeRequest l, CanonicalProductionManifestExchangeRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionArtifactRequest : IEquatable<CanonicalProductionArtifactRequest>
{
    public string ObjectID { get; }
    public string ArtifactID { get; }
    public CanonicalArtifact.Kind Kind { get; }
    public string? LogicalPathToken { get; }

    public CanonicalProductionArtifactRequest(string objectID, string artifactID, CanonicalArtifact.Kind kind, string? logicalPathToken = null)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        ArtifactID = CanonicalProductionRedaction.SafeIdentifier(artifactID, kind.ToString());
        Kind = kind;
        LogicalPathToken = CanonicalProjectionContract.SafeLogicalPathToken(logicalPathToken);
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionArtifactRequest other && Equals(other);
    public bool Equals(CanonicalProductionArtifactRequest? other) =>
        other is not null && ObjectID == other.ObjectID && ArtifactID == other.ArtifactID && Kind == other.Kind;
    public override int GetHashCode() => HashCode.Combine(ObjectID, ArtifactID, Kind);
    public static bool operator ==(CanonicalProductionArtifactRequest l, CanonicalProductionArtifactRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionArtifactRequest l, CanonicalProductionArtifactRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionUploadCancelRequest : IEquatable<CanonicalProductionUploadCancelRequest>
{
    public string ObjectID { get; }
    public CanonicalUploadSessionID SessionID { get; }
    public string Reason { get; }

    public CanonicalProductionUploadCancelRequest(string objectID, CanonicalUploadSessionID sessionID, string reason)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        SessionID = sessionID;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? "cancelled";
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionUploadCancelRequest other && Equals(other);
    public bool Equals(CanonicalProductionUploadCancelRequest? other) =>
        other is not null && ObjectID == other.ObjectID && SessionID.Equals(other.SessionID);
    public override int GetHashCode() => HashCode.Combine(ObjectID, SessionID);
    public static bool operator ==(CanonicalProductionUploadCancelRequest l, CanonicalProductionUploadCancelRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionUploadCancelRequest l, CanonicalProductionUploadCancelRequest r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionUploadFailureKind
{
    retryable,
    conflict,
    fatal
}

public sealed class CanonicalProductionUploadFailure : IEquatable<CanonicalProductionUploadFailure>
{
    public string ObjectID { get; }
    public string Code { get; }
    public string? Message { get; }

    public CanonicalProductionUploadFailure(string objectID, string code, string? message = null)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        Code = CanonicalProductionRedaction.SafeDiagnosticText(code) ?? "unknown";
        Message = CanonicalProductionRedaction.SafeDiagnosticText(message);
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionUploadFailure other && Equals(other);
    public bool Equals(CanonicalProductionUploadFailure? other) =>
        other is not null && ObjectID == other.ObjectID && Code == other.Code;
    public override int GetHashCode() => HashCode.Combine(ObjectID, Code);
    public static bool operator ==(CanonicalProductionUploadFailure l, CanonicalProductionUploadFailure r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionUploadFailure l, CanonicalProductionUploadFailure r) => !l.Equals(r);
}

public sealed class CanonicalProductionUploadFailureClassification : IEquatable<CanonicalProductionUploadFailureClassification>
{
    public CanonicalProductionUploadFailureKind Kind { get; }
    public CanonicalRetryPolicySnapshot? Retry { get; }
    public string Reason { get; }

    public CanonicalProductionUploadFailureClassification(
        CanonicalProductionUploadFailureKind kind,
        CanonicalRetryPolicySnapshot? retry = null,
        string reason = "")
    {
        Kind = kind;
        Retry = retry;
        Reason = reason;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionUploadFailureClassification other && Equals(other);
    public bool Equals(CanonicalProductionUploadFailureClassification? other) =>
        other is not null && Kind == other.Kind && Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(Kind, Reason);
    public static bool operator ==(CanonicalProductionUploadFailureClassification l, CanonicalProductionUploadFailureClassification r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionUploadFailureClassification l, CanonicalProductionUploadFailureClassification r) => !l.Equals(r);
}

public sealed class CanonicalProductionUploadLedgerSnapshot : IEquatable<CanonicalProductionUploadLedgerSnapshot>
{
    public string ObjectID { get; }
    public CanonicalUploadSessionID? SessionID { get; }
    public long ConfirmedBytes { get; }
    public long? TotalBytes { get; }
    public string? ContentHashPrefix { get; }
    public CanonicalUploadSessionPhase? Phase { get; }
    public CanonicalRetryPolicySnapshot? Retry { get; }

    public CanonicalProductionUploadLedgerSnapshot(
        string objectID,
        CanonicalUploadSessionID? sessionID = null,
        long confirmedBytes = 0,
        long? totalBytes = null,
        CanonicalHash? contentHash = null,
        CanonicalUploadSessionPhase? phase = null,
        CanonicalRetryPolicySnapshot? retry = null)
    {
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording");
        SessionID = sessionID;
        ConfirmedBytes = confirmedBytes;
        TotalBytes = totalBytes;
        ContentHashPrefix = contentHash is not null ? CanonicalProductionRedaction.HashPrefix(contentHash.Value.Value) : null;
        Phase = phase;
        Retry = retry;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionUploadLedgerSnapshot other && Equals(other);
    public bool Equals(CanonicalProductionUploadLedgerSnapshot? other) =>
        other is not null && ObjectID == other.ObjectID && Equals(SessionID, other.SessionID);
    public override int GetHashCode() => HashCode.Combine(ObjectID, SessionID);
    public static bool operator ==(CanonicalProductionUploadLedgerSnapshot l, CanonicalProductionUploadLedgerSnapshot r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionUploadLedgerSnapshot l, CanonicalProductionUploadLedgerSnapshot r) => !l.Equals(r);
}

public sealed class CanonicalProductionUploadRollbackRequest : IEquatable<CanonicalProductionUploadRollbackRequest>
{
    public string ObjectID { get; }
    public CanonicalUploadSessionID? SessionID { get; }
    public string CheckpointID { get; }

    public CanonicalProductionUploadRollbackRequest(string objectID, CanonicalUploadSessionID? sessionID, string checkpointID)
    {
        ObjectID = objectID;
        SessionID = sessionID;
        CheckpointID = checkpointID;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionUploadRollbackRequest other && Equals(other);
    public bool Equals(CanonicalProductionUploadRollbackRequest? other) =>
        other is not null && ObjectID == other.ObjectID && Equals(SessionID, other.SessionID);
    public override int GetHashCode() => HashCode.Combine(ObjectID, SessionID);
    public static bool operator ==(CanonicalProductionUploadRollbackRequest l, CanonicalProductionUploadRollbackRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionUploadRollbackRequest l, CanonicalProductionUploadRollbackRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionApplyExecutionRequest : IEquatable<CanonicalProductionApplyExecutionRequest>
{
    public CanonicalApplyAction Action { get; }
    public string? RollbackCheckpointID { get; }

    public CanonicalProductionApplyExecutionRequest(CanonicalApplyAction action, string? rollbackCheckpointID = null)
    {
        Action = action;
        RollbackCheckpointID = rollbackCheckpointID;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionApplyExecutionRequest other && Equals(other);
    public bool Equals(CanonicalProductionApplyExecutionRequest? other) =>
        other is not null && Action.Equals(other.Action);
    public override int GetHashCode() => Action.GetHashCode();
    public static bool operator ==(CanonicalProductionApplyExecutionRequest l, CanonicalProductionApplyExecutionRequest r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionApplyExecutionRequest l, CanonicalProductionApplyExecutionRequest r) => !l.Equals(r);
}

public sealed class CanonicalProductionApplyPrecondition : IEquatable<CanonicalProductionApplyPrecondition>
{
    public string ActionID { get; }
    public CanonicalApplyTarget Target { get; }
    public string? ExpectedHashPrefix { get; }
    public bool Accepted { get; }
    public string? Reason { get; }

    public CanonicalProductionApplyPrecondition(
        string actionID,
        CanonicalApplyTarget target,
        string? expectedHashPrefix = null,
        bool accepted = false,
        string? reason = null)
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, "apply-precondition");
        Target = target;
        ExpectedHashPrefix = CanonicalProductionRedaction.HashPrefix(expectedHashPrefix);
        Accepted = accepted;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionApplyPrecondition other && Equals(other);
    public bool Equals(CanonicalProductionApplyPrecondition? other) =>
        other is not null && ActionID == other.ActionID && Accepted == other.Accepted;
    public override int GetHashCode() => HashCode.Combine(ActionID, Accepted);
    public static bool operator ==(CanonicalProductionApplyPrecondition l, CanonicalProductionApplyPrecondition r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionApplyPrecondition l, CanonicalProductionApplyPrecondition r) => !l.Equals(r);
}

public sealed class CanonicalProductionApplyPostcondition : IEquatable<CanonicalProductionApplyPostcondition>
{
    public string ActionID { get; }
    public CanonicalApplyTarget Target { get; }
    public string? ActualHashPrefix { get; }
    public bool Accepted { get; }
    public string? Reason { get; }

    public CanonicalProductionApplyPostcondition(
        string actionID,
        CanonicalApplyTarget target,
        string? actualHashPrefix = null,
        bool accepted = false,
        string? reason = null)
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(actionID, "apply-postcondition");
        Target = target;
        ActualHashPrefix = CanonicalProductionRedaction.HashPrefix(actualHashPrefix);
        Accepted = accepted;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionApplyPostcondition other && Equals(other);
    public bool Equals(CanonicalProductionApplyPostcondition? other) =>
        other is not null && ActionID == other.ActionID && Accepted == other.Accepted;
    public override int GetHashCode() => HashCode.Combine(ActionID, Accepted);
    public static bool operator ==(CanonicalProductionApplyPostcondition l, CanonicalProductionApplyPostcondition r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionApplyPostcondition l, CanonicalProductionApplyPostcondition r) => !l.Equals(r);
}

public sealed class CanonicalProductionApplyResult : IEquatable<CanonicalProductionApplyResult>
{
    public string ActionID { get; }
    public CanonicalApplyExecutionStatus Status { get; }
    public CanonicalProductionApplyPrecondition? Precondition { get; }
    public CanonicalProductionApplyPostcondition? Postcondition { get; }
    public CanonicalProductionSideEffect? SideEffect { get; }
    public string? RollbackCheckpointID { get; }

    public CanonicalProductionApplyResult(
        string actionID,
        CanonicalApplyExecutionStatus status,
        CanonicalProductionApplyPrecondition? precondition = null,
        CanonicalProductionApplyPostcondition? postcondition = null,
        CanonicalProductionSideEffect? sideEffect = null,
        string? rollbackCheckpointID = null)
    {
        ActionID = actionID;
        Status = status;
        Precondition = precondition;
        Postcondition = postcondition;
        SideEffect = sideEffect;
        RollbackCheckpointID = rollbackCheckpointID;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionApplyResult other && Equals(other);
    public bool Equals(CanonicalProductionApplyResult? other) =>
        other is not null && ActionID == other.ActionID && Status == other.Status;
    public override int GetHashCode() => HashCode.Combine(ActionID, Status);
    public static bool operator ==(CanonicalProductionApplyResult l, CanonicalProductionApplyResult r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionApplyResult l, CanonicalProductionApplyResult r) => !l.Equals(r);
}

// ─── Interfaces (Swift protocols) ────────────────────────────────────────────

public interface ICanonicalProductionFilePort
{
    bool IsDryRunOnly { get; }
    CanonicalProductionCapability[] Capabilities { get; }

    Task<CanonicalPathResolutionResult> ResolveRootBound(CanonicalFileReference reference);
    Task<CanonicalProductionFileReadResult> ReadMetadata(CanonicalProductionMetadataReadRequest request);
    Task<CanonicalProductionFileWriteResult> WriteMetadata(CanonicalFileWriteIntent intent, CanonicalRollbackCheckpoint? rollbackCheckpoint);
    Task<CanonicalProductionFileReadResult> ReadArtifact(CanonicalProductionArtifactReadRequest request);
    Task<CanonicalProductionFileWriteResult> WriteArtifactAtomic(CanonicalFileWriteIntent intent, CanonicalRollbackCheckpoint? rollbackCheckpoint);
    Task<CanonicalProductionFileVerificationEvidence> VerifyArtifact(CanonicalProductionArtifactVerifyRequest request);
    Task<CanonicalProductionFileWriteResult> MarkTombstone(CanonicalProductionTombstoneRequest request);
    Task<CanonicalProductionArtifactDescriptor[]> ListKnownArtifacts(CanonicalRootToken rootToken, string? objectID);
    Task<string[]> ListKnownObjects(CanonicalRootToken rootToken);
    Task<CanonicalProductionHashResult> ComputeHash(CanonicalProductionHashRequest request);
    Task<CanonicalRollbackResult> RollbackWrite(CanonicalProductionFileRollbackRequest request);

    Task<byte[]?> MetadataSnapshot(string objectID);
    Task<CanonicalProductionArtifactDescriptor> ArtifactDescriptor(CanonicalArtifact artifact);
    Task<CanonicalProductionReadProjection> ValidateRead(CanonicalFileReference reference);
    Task<CanonicalPathResolutionResult> ResolveLogicalToken(string token, CanonicalRootToken rootToken);
    Task<bool> VerifyContainment(CanonicalFileReference reference);
    Task<CanonicalProductionWriteIntentProjection> ProjectWrite(CanonicalFileWriteIntent intent);
}

public interface ICanonicalProductionTransportPort
{
    bool IsDryRunOnly { get; }
    CanonicalProductionTransportRouteCapability[] RouteCapabilities { get; }
    bool RealNetworkExecutionEnabled { get; }

    Task<CanonicalProductionSignedRequest> BuildSignedRequest(CanonicalProductionTransportBuildRequest request);
    Task<CanonicalProductionTransportExchangeResult> SendRequest(CanonicalProductionSignedRequest request);
    Task<CanonicalProductionTransportExchangeResult> ReceiveResponse(CanonicalTransportResponse response, CanonicalProductionSignedRequest request);
    Task<CanonicalProductionTransportVerification> VerifyResponse(CanonicalProductionTransportExchangeResult exchange);
    Task<CanonicalProductionTransportExchangeResult> ExchangeManifest(CanonicalProductionManifestExchangeRequest request);
    Task<CanonicalProductionTransportExchangeResult> RequestArtifact(CanonicalProductionArtifactRequest request, CanonicalProductionTransportBuildRequest envelope);
    Task<CanonicalProductionTransportExchangeResult> SendApplyMetadata(CanonicalApplyAction action, CanonicalProductionTransportBuildRequest envelope);
    Task<CanonicalProductionTransportExchangeResult> StartUploadSession(CanonicalUploadStartRequest request, CanonicalProductionTransportBuildRequest envelope);
    Task<CanonicalProductionTransportExchangeResult> QueryUploadSession(CanonicalUploadStatusRequest request, CanonicalProductionTransportBuildRequest envelope);
    Task<CanonicalProductionTransportExchangeResult> SendUploadChunk(CanonicalUploadChunk chunk, CanonicalProductionTransportBuildRequest envelope);
    Task<CanonicalProductionTransportExchangeResult> FinalizeUploadSession(CanonicalUploadFinalizeRequest request, CanonicalProductionTransportBuildRequest envelope);
    Task<CanonicalProductionTransportExchangeResult> CancelUploadSession(CanonicalProductionUploadCancelRequest request, CanonicalProductionTransportBuildRequest envelope);

    Task<CanonicalProductionTransportEnvelopeDryRun> BuildEnvelopeDryRun(
        CanonicalNode source, CanonicalNode destination, CanonicalTransportRoute route, byte[] body);
    Task<CanonicalTransportResponse> DecodeResponseDryRun(CanonicalTransportResponse response);
}

public interface ICanonicalProductionUploadPort
{
    bool IsDryRunOnly { get; }
    bool ResumableSessionSupported { get; }
    int ChunkSizePolicy { get; }

    Task<CanonicalUploadSessionStatus> StartResumableUpload(CanonicalUploadStartRequest request, DateTime now);
    Task<CanonicalUploadSessionStatus> ResumeUpload(CanonicalUploadStatusRequest request, DateTime now);
    Task<CanonicalUploadSessionStatus> UploadChunk(CanonicalUploadChunk chunk, DateTime now);
    Task<long> QueryConfirmedBytes(CanonicalUploadStatusRequest request, DateTime now);
    Task<CanonicalUploadSessionStatus> FinalizeUpload(CanonicalUploadFinalizeRequest request, DateTime now);
    Task<CanonicalRollbackResult> CancelUpload(CanonicalProductionUploadCancelRequest request, DateTime now);
    CanonicalProductionUploadFailureClassification ClassifyUploadFailure(CanonicalProductionUploadFailure failure);
    Task<CanonicalProductionUploadLedgerSnapshot> ReadUploadLedger(string objectID);
    Task<CanonicalProductionUploadLedgerSnapshot> WriteUploadLedger(CanonicalProductionUploadLedgerSnapshot snapshot);
    CanonicalRetryPolicySnapshot? ProjectRetry(CanonicalProductionUploadLedgerSnapshot snapshot, DateTime now);
    Task<CanonicalRollbackResult> RollbackUploadState(CanonicalProductionUploadRollbackRequest request);

    Task<CanonicalProductionUploadTrace> ProjectUploadDryRun(
        CanonicalRecordingObject obj, CanonicalArtifact artifact);
}

public interface ICanonicalProductionApplyPort
{
    bool IsDryRunOnly { get; }
    bool MetadataApplySupported { get; }
    bool GeneratedArtifactApplySupported { get; }
    bool TombstoneApplySupported { get; }
    bool ConflictRecordSupported { get; }

    Task<CanonicalProductionApplyResult> ApplyMetadata(CanonicalProductionApplyExecutionRequest request);
    Task<CanonicalProductionApplyResult> SendMetadata(CanonicalProductionApplyExecutionRequest request);
    Task<CanonicalProductionApplyResult> ApplyGeneratedArtifact(CanonicalProductionApplyExecutionRequest request);
    Task<CanonicalProductionApplyResult> RequestGeneratedArtifact(CanonicalProductionArtifactRequest request);
    Task<CanonicalProductionApplyResult> ApplyObjectTombstone(CanonicalProductionApplyExecutionRequest request);
    Task<CanonicalProductionApplyResult> ApplyLibraryTombstone(CanonicalProductionApplyExecutionRequest request);
    Task<CanonicalProductionApplyResult> RecordConflict(CanonicalProductionApplyExecutionRequest request);
    Task<CanonicalProductionApplyPrecondition> VerifyPrecondition(CanonicalProductionApplyPrecondition precondition);
    Task<CanonicalProductionApplyPostcondition> VerifyPostcondition(CanonicalProductionApplyPostcondition postcondition);
    Task<CanonicalRollbackResult> RollbackApply(CanonicalRollbackAction request);

    Task<CanonicalProductionApplyTrace> ProjectApplyDryRun(CanonicalApplyAction action);
}

public interface ICanonicalProductionSyncClockPort
{
    CanonicalTimestamp Now();
    double MonotonicNow();
    bool ValidateTimestampWindow(CanonicalTimestamp timestamp, CanonicalTimestamp now, double tolerance);
    CanonicalSyncPlanTrigger TriggerContext(CanonicalSyncPlanTrigger defaultTrigger);
}

public interface ICanonicalProductionDiagnosticsPort
{
    Task Record(CanonicalProductionDiagnosticsEvent evt);
    Task RecordKernelEvent(CanonicalProductionDiagnosticsEvent evt);
    Task RecordDryRunEvent(CanonicalProductionDiagnosticsEvent evt);
    Task RecordProductionEvent(CanonicalProductionDiagnosticsEvent evt);
    Task RecordConflict(CanonicalProductionDiagnosticsEvent evt);
    Task RecordMigrationGate(CanonicalProductionExecutionAudit audit);
    Task RecordRedactedTrace(CanonicalProductionExecutionTrace trace);
}

public interface ICanonicalProductionCapabilityPort
{
    CanonicalProductionCapabilitySummary Summary(CanonicalNode node);
    bool Supports(CanonicalProductionDomain domain, CanonicalProductionOperation operation);
    CanonicalProductionCapabilitySummary LocalCapabilities();
    CanonicalProductionCapabilitySummary? PeerCapabilities();
    bool ValidateCapability(CanonicalProductionDomain domain, CanonicalProductionOperation operation);
    bool ValidateSchema(CanonicalManifest manifest);
}

// ─── CanonicalProductionPortSet ──────────────────────────────────────────────

public sealed class CanonicalProductionPortSet : IEquatable<CanonicalProductionPortSet>
{
    public ICanonicalProductionFilePort? File { get; }
    public ICanonicalProductionTransportPort? Transport { get; }
    public ICanonicalProductionUploadPort? Upload { get; }
    public ICanonicalProductionApplyPort? Apply { get; }
    public ICanonicalProductionSyncClockPort? SyncClock { get; }
    public ICanonicalProductionDiagnosticsPort? Diagnostics { get; }
    public ICanonicalProductionCapabilityPort? Capability { get; }

    public CanonicalProductionPortSet(
        ICanonicalProductionFilePort? file = null,
        ICanonicalProductionTransportPort? transport = null,
        ICanonicalProductionUploadPort? upload = null,
        ICanonicalProductionApplyPort? apply = null,
        ICanonicalProductionSyncClockPort? syncClock = null,
        ICanonicalProductionDiagnosticsPort? diagnostics = null,
        ICanonicalProductionCapabilityPort? capability = null)
    {
        File = file;
        Transport = transport;
        Upload = upload;
        Apply = apply;
        SyncClock = syncClock;
        Diagnostics = diagnostics;
        Capability = capability;
    }

    public CanonicalProductionPortKind[] MissingRequiredPorts
    {
        get
        {
            var missing = new List<CanonicalProductionPortKind>();
            if (File is null) missing.Add(CanonicalProductionPortKind.file);
            if (Transport is null) missing.Add(CanonicalProductionPortKind.transport);
            if (Upload is null) missing.Add(CanonicalProductionPortKind.upload);
            if (Apply is null) missing.Add(CanonicalProductionPortKind.apply);
            return missing.ToArray();
        }
    }

    public CanonicalProductionPortReadiness Readiness(DateTime generatedAt = default)
    {
        var declared = new Dictionary<CanonicalProductionPortKind, bool>
        {
            [CanonicalProductionPortKind.file] = File is not null,
            [CanonicalProductionPortKind.transport] = Transport is not null,
            [CanonicalProductionPortKind.upload] = Upload is not null,
            [CanonicalProductionPortKind.apply] = Apply is not null,
            [CanonicalProductionPortKind.syncClock] = SyncClock is not null,
            [CanonicalProductionPortKind.diagnostics] = Diagnostics is not null,
            [CanonicalProductionPortKind.capability] = Capability is not null
        };
        var dryRunOnly = (File?.IsDryRunOnly ?? true)
            && (Transport?.IsDryRunOnly ?? true)
            && (Upload?.IsDryRunOnly ?? true)
            && (Apply?.IsDryRunOnly ?? true)
            && !(Transport?.RealNetworkExecutionEnabled ?? false);
        return new CanonicalProductionPortReadiness(
            declaredPorts: declared,
            missingPorts: MissingRequiredPorts,
            dryRunOnly: dryRunOnly,
            generatedAt: generatedAt
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionPortSet other && Equals(other);
    public bool Equals(CanonicalProductionPortSet? other) => other is not null;
    public override int GetHashCode() => HashCode.Combine(File, Transport, Upload, Apply);
    public static bool operator ==(CanonicalProductionPortSet l, CanonicalProductionPortSet r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionPortSet l, CanonicalProductionPortSet r) => !l.Equals(r);
}

// ─── Interface Default Implementations (Swift protocol extensions) ───────────

public static class CanonicalProductionFilePortExtensions
{
    public static async Task<CanonicalPathResolutionResult> DefaultResolveRootBound(
        this ICanonicalProductionFilePort port, CanonicalFileReference reference)
    {
        return await port.ResolveLogicalToken(reference.LogicalPathToken, reference.RootToken);
    }

    public static Task<CanonicalProductionFileReadResult> DefaultReadMetadata(
        this ICanonicalProductionFilePort port, CanonicalProductionMetadataReadRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionReadMetadataNotImplemented");
    }

    public static Task<CanonicalProductionFileWriteResult> DefaultWriteMetadata(
        this ICanonicalProductionFilePort port, CanonicalFileWriteIntent intent, CanonicalRollbackCheckpoint? rollbackCheckpoint)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionWriteMetadataNotImplemented");
    }

    public static Task<CanonicalProductionFileReadResult> DefaultReadArtifact(
        this ICanonicalProductionFilePort port, CanonicalProductionArtifactReadRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionReadArtifactNotImplemented");
    }

    public static Task<CanonicalProductionFileWriteResult> DefaultWriteArtifactAtomic(
        this ICanonicalProductionFilePort port, CanonicalFileWriteIntent intent, CanonicalRollbackCheckpoint? rollbackCheckpoint)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionWriteArtifactNotImplemented");
    }

    public static async Task<CanonicalProductionFileVerificationEvidence> DefaultVerifyArtifact(
        this ICanonicalProductionFilePort port, CanonicalProductionArtifactVerifyRequest request)
    {
        var resolution = await port.ResolveRootBound(request.Reference);
        return new CanonicalProductionFileVerificationEvidence(
            reference: request.Reference,
            resolution: resolution,
            expectedHash: request.ExpectedContentHash,
            actualHash: null,
            expectedByteSize: request.ExpectedByteSize,
            actualByteSize: null,
            computedStreaming: request.RequireStreamingHash
        );
    }

    public static Task<CanonicalProductionFileWriteResult> DefaultMarkTombstone(
        this ICanonicalProductionFilePort port, CanonicalProductionTombstoneRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionMarkTombstoneNotImplemented");
    }

    public static Task<CanonicalProductionArtifactDescriptor[]> DefaultListKnownArtifacts(
        this ICanonicalProductionFilePort port, CanonicalRootToken rootToken, string? objectID)
    {
        return Task.FromResult(Array.Empty<CanonicalProductionArtifactDescriptor>());
    }

    public static Task<string[]> DefaultListKnownObjects(
        this ICanonicalProductionFilePort port, CanonicalRootToken rootToken)
    {
        return Task.FromResult(Array.Empty<string>());
    }

    public static Task<CanonicalProductionHashResult> DefaultComputeHash(
        this ICanonicalProductionFilePort port, CanonicalProductionHashRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionComputeHashNotImplemented");
    }

    public static Task<CanonicalRollbackResult> DefaultRollbackWrite(
        this ICanonicalProductionFilePort port, CanonicalProductionFileRollbackRequest request)
    {
        return Task.FromResult(new CanonicalRollbackResult(
            planID: request.CheckpointID,
            succeeded: false,
            failures: new[] { new CanonicalRollbackFailure(actionID: request.CheckpointID, reason: "productionRollbackWriteNotImplemented") }
        ));
    }
}

public static class CanonicalProductionTransportPortExtensions
{
    public static async Task<CanonicalProductionSignedRequest> DefaultBuildSignedRequest(
        this ICanonicalProductionTransportPort port, CanonicalProductionTransportBuildRequest request)
    {
        if (!port.RealNetworkExecutionEnabled)
            throw CanonicalProductionPortException.NetworkExecutionSuppressed("productionNetworkExecutionDisabled");
        return new CanonicalProductionSignedRequest(buildRequest: request, signerDescription: "externalSignerRequired");
    }

    public static Task<CanonicalProductionTransportExchangeResult> DefaultSendRequest(
        this ICanonicalProductionTransportPort port, CanonicalProductionSignedRequest request)
    {
        throw CanonicalProductionPortException.NetworkExecutionSuppressed("productionSendRequestNotImplemented");
    }

    public static Task<CanonicalProductionTransportExchangeResult> DefaultReceiveResponse(
        this ICanonicalProductionTransportPort port, CanonicalTransportResponse response, CanonicalProductionSignedRequest request)
    {
        if (!response.HasValidBodyHash)
            throw new InvalidOperationException("production-response invalid body hash");
        return Task.FromResult(new CanonicalProductionTransportExchangeResult(
            request: request,
            response: response,
            responseVerified: true,
            usedExistingRoute: true,
            sideEffect: null
        ));
    }

    public static Task<CanonicalProductionTransportVerification> DefaultVerifyResponse(
        this ICanonicalProductionTransportPort port, CanonicalProductionTransportExchangeResult exchange)
    {
        return Task.FromResult(new CanonicalProductionTransportVerification(
            route: exchange.Request.BuildRequest.Route,
            bodyHashVerified: CanonicalTransportEnvelope.Hash(exchange.Request.BuildRequest.Body).Equals(exchange.Request.BodyHash),
            responseHashVerified: exchange.Response.HasValidBodyHash,
            timestampAccepted: true,
            externalVerifierRequired: true
        ));
    }

    public static async Task<CanonicalProductionTransportExchangeResult> DefaultExchangeManifest(
        this ICanonicalProductionTransportPort port, CanonicalProductionManifestExchangeRequest request)
    {
        var body = CanonicalTransportJSON.Encode(request.LocalManifest);
        var build = new CanonicalProductionTransportBuildRequest(
            source: request.LocalManifest.Node,
            destination: request.PeerNode,
            route: CanonicalTransportRoute.manifestExchange,
            existingRoutePath: "/sync/inventory",
            body: body,
            nonce: "external-nonce-required"
        );
        return await port.SendRequest(await port.BuildSignedRequest(build));
    }

    public static async Task<CanonicalProductionTransportExchangeResult> DefaultRequestArtifact(
        this ICanonicalProductionTransportPort port, CanonicalProductionArtifactRequest request, CanonicalProductionTransportBuildRequest envelope)
    {
        return await port.SendRequest(await port.BuildSignedRequest(envelope));
    }

    public static async Task<CanonicalProductionTransportExchangeResult> DefaultSendApplyMetadata(
        this ICanonicalProductionTransportPort port, CanonicalApplyAction action, CanonicalProductionTransportBuildRequest envelope)
    {
        return await port.SendRequest(await port.BuildSignedRequest(envelope));
    }

    public static async Task<CanonicalProductionTransportExchangeResult> DefaultStartUploadSession(
        this ICanonicalProductionTransportPort port, CanonicalUploadStartRequest request, CanonicalProductionTransportBuildRequest envelope)
    {
        return await port.SendRequest(await port.BuildSignedRequest(envelope));
    }

    public static async Task<CanonicalProductionTransportExchangeResult> DefaultQueryUploadSession(
        this ICanonicalProductionTransportPort port, CanonicalUploadStatusRequest request, CanonicalProductionTransportBuildRequest envelope)
    {
        return await port.SendRequest(await port.BuildSignedRequest(envelope));
    }

    public static async Task<CanonicalProductionTransportExchangeResult> DefaultSendUploadChunk(
        this ICanonicalProductionTransportPort port, CanonicalUploadChunk chunk, CanonicalProductionTransportBuildRequest envelope)
    {
        return await port.SendRequest(await port.BuildSignedRequest(envelope));
    }

    public static async Task<CanonicalProductionTransportExchangeResult> DefaultFinalizeUploadSession(
        this ICanonicalProductionTransportPort port, CanonicalUploadFinalizeRequest request, CanonicalProductionTransportBuildRequest envelope)
    {
        return await port.SendRequest(await port.BuildSignedRequest(envelope));
    }

    public static async Task<CanonicalProductionTransportExchangeResult> DefaultCancelUploadSession(
        this ICanonicalProductionTransportPort port, CanonicalProductionUploadCancelRequest request, CanonicalProductionTransportBuildRequest envelope)
    {
        return await port.SendRequest(await port.BuildSignedRequest(envelope));
    }
}

public static class CanonicalProductionUploadPortExtensions
{
    public static Task<CanonicalUploadSessionStatus> DefaultStartResumableUpload(
        this ICanonicalProductionUploadPort port, CanonicalUploadStartRequest request, DateTime now)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionStartResumableUploadNotImplemented");
    }

    public static Task<CanonicalUploadSessionStatus> DefaultResumeUpload(
        this ICanonicalProductionUploadPort port, CanonicalUploadStatusRequest request, DateTime now)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionResumeUploadNotImplemented");
    }

    public static Task<CanonicalUploadSessionStatus> DefaultUploadChunk(
        this ICanonicalProductionUploadPort port, CanonicalUploadChunk chunk, DateTime now)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionUploadChunkNotImplemented");
    }

    public static async Task<long> DefaultQueryConfirmedBytes(
        this ICanonicalProductionUploadPort port, CanonicalUploadStatusRequest request, DateTime now)
    {
        var status = await port.ResumeUpload(request, now);
        return status.ConfirmedBytes;
    }

    public static Task<CanonicalUploadSessionStatus> DefaultFinalizeUpload(
        this ICanonicalProductionUploadPort port, CanonicalUploadFinalizeRequest request, DateTime now)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionFinalizeUploadNotImplemented");
    }

    public static Task<CanonicalRollbackResult> DefaultCancelUpload(
        this ICanonicalProductionUploadPort port, CanonicalProductionUploadCancelRequest request, DateTime now)
    {
        return Task.FromResult(new CanonicalRollbackResult(
            planID: request.SessionID.RawValue,
            succeeded: false,
            failures: new[] { new CanonicalRollbackFailure(actionID: request.SessionID.RawValue, reason: "productionCancelUploadNotImplemented") }
        ));
    }

    public static CanonicalProductionUploadFailureClassification DefaultClassifyUploadFailure(
        this ICanonicalProductionUploadPort port, CanonicalProductionUploadFailure failure)
    {
        return new CanonicalProductionUploadFailureClassification(kind: CanonicalProductionUploadFailureKind.fatal, retry: null, reason: failure.Code);
    }

    public static Task<CanonicalProductionUploadLedgerSnapshot> DefaultReadUploadLedger(
        this ICanonicalProductionUploadPort port, string objectID)
    {
        return Task.FromResult(new CanonicalProductionUploadLedgerSnapshot(objectID: objectID));
    }

    public static Task<CanonicalProductionUploadLedgerSnapshot> DefaultWriteUploadLedger(
        this ICanonicalProductionUploadPort port, CanonicalProductionUploadLedgerSnapshot snapshot)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionWriteUploadLedgerNotImplemented");
    }

    public static CanonicalRetryPolicySnapshot? DefaultProjectRetry(
        this ICanonicalProductionUploadPort port, CanonicalProductionUploadLedgerSnapshot snapshot, DateTime now)
    {
        return snapshot.Retry;
    }

    public static Task<CanonicalRollbackResult> DefaultRollbackUploadState(
        this ICanonicalProductionUploadPort port, CanonicalProductionUploadRollbackRequest request)
    {
        return Task.FromResult(new CanonicalRollbackResult(
            planID: request.CheckpointID,
            succeeded: false,
            failures: new[] { new CanonicalRollbackFailure(actionID: request.ObjectID, reason: "productionRollbackUploadNotImplemented") }
        ));
    }
}

public static class CanonicalProductionApplyPortExtensions
{
    public static Task<CanonicalProductionApplyResult> DefaultApplyMetadata(
        this ICanonicalProductionApplyPort port, CanonicalProductionApplyExecutionRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionApplyMetadataNotImplemented");
    }

    public static Task<CanonicalProductionApplyResult> DefaultSendMetadata(
        this ICanonicalProductionApplyPort port, CanonicalProductionApplyExecutionRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionSendMetadataNotImplemented");
    }

    public static Task<CanonicalProductionApplyResult> DefaultApplyGeneratedArtifact(
        this ICanonicalProductionApplyPort port, CanonicalProductionApplyExecutionRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionApplyGeneratedArtifactNotImplemented");
    }

    public static Task<CanonicalProductionApplyResult> DefaultRequestGeneratedArtifact(
        this ICanonicalProductionApplyPort port, CanonicalProductionArtifactRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionRequestGeneratedArtifactNotImplemented");
    }

    public static Task<CanonicalProductionApplyResult> DefaultApplyObjectTombstone(
        this ICanonicalProductionApplyPort port, CanonicalProductionApplyExecutionRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionApplyObjectTombstoneNotImplemented");
    }

    public static Task<CanonicalProductionApplyResult> DefaultApplyLibraryTombstone(
        this ICanonicalProductionApplyPort port, CanonicalProductionApplyExecutionRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionApplyLibraryTombstoneNotImplemented");
    }

    public static Task<CanonicalProductionApplyResult> DefaultRecordConflict(
        this ICanonicalProductionApplyPort port, CanonicalProductionApplyExecutionRequest request)
    {
        throw CanonicalProductionPortException.ProductionMutationAttempted("productionRecordConflictNotImplemented");
    }

    public static Task<CanonicalProductionApplyPrecondition> DefaultVerifyPrecondition(
        this ICanonicalProductionApplyPort port, CanonicalProductionApplyPrecondition precondition)
    {
        return Task.FromResult(precondition);
    }

    public static Task<CanonicalProductionApplyPostcondition> DefaultVerifyPostcondition(
        this ICanonicalProductionApplyPort port, CanonicalProductionApplyPostcondition postcondition)
    {
        return Task.FromResult(postcondition);
    }

    public static Task<CanonicalRollbackResult> DefaultRollbackApply(
        this ICanonicalProductionApplyPort port, CanonicalRollbackAction request)
    {
        return Task.FromResult(new CanonicalRollbackResult(
            planID: request.CheckpointID ?? request.ActionID,
            succeeded: false,
            failures: new[] { new CanonicalRollbackFailure(actionID: request.ActionID, reason: "productionRollbackApplyNotImplemented") }
        ));
    }
}

public static class CanonicalProductionSyncClockPortExtensions
{
    public static double DefaultMonotonicNow(this ICanonicalProductionSyncClockPort port)
    {
        return (DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks) / (double)TimeSpan.TicksPerSecond;
    }

    public static bool DefaultValidateTimestampWindow(
        this ICanonicalProductionSyncClockPort port, CanonicalTimestamp timestamp, CanonicalTimestamp now, double tolerance)
    {
        return Math.Abs((now.Date.ToUniversalTime() - timestamp.Date.ToUniversalTime()).TotalSeconds) <= tolerance;
    }
}

public static class CanonicalProductionDiagnosticsPortExtensions
{
    public static async Task DefaultRecordKernelEvent(
        this ICanonicalProductionDiagnosticsPort port, CanonicalProductionDiagnosticsEvent evt)
    {
        await port.Record(evt);
    }

    public static async Task DefaultRecordDryRunEvent(
        this ICanonicalProductionDiagnosticsPort port, CanonicalProductionDiagnosticsEvent evt)
    {
        await port.Record(evt);
    }

    public static async Task DefaultRecordProductionEvent(
        this ICanonicalProductionDiagnosticsPort port, CanonicalProductionDiagnosticsEvent evt)
    {
        await port.Record(evt);
    }

    public static async Task DefaultRecordConflict(
        this ICanonicalProductionDiagnosticsPort port, CanonicalProductionDiagnosticsEvent evt)
    {
        await port.Record(evt);
    }

    public static async Task DefaultRecordMigrationGate(
        this ICanonicalProductionDiagnosticsPort port, CanonicalProductionExecutionAudit audit)
    {
        await port.Record(
            new CanonicalProductionDiagnosticsEvent(
                kind: audit.Allowed
                    ? CanonicalProductionDiagnosticEventKind.canonicalEligibleForManualMigrationDesign
                    : CanonicalProductionDiagnosticEventKind.canonicalProductionMigrationBlocked,
                reason: string.Join(",", audit.RejectionReasons.Select(r => r.ToString())),
                dryRun: false,
                generatedAt: audit.GeneratedAt.Date
            )
        );
    }

    public static async Task DefaultRecordRedactedTrace(
        this ICanonicalProductionDiagnosticsPort port, CanonicalProductionExecutionTrace trace)
    {
        await port.Record(
            new CanonicalProductionDiagnosticsEvent(
                kind: CanonicalProductionDiagnosticEventKind.canonicalProductionPortsDeclared,
                action: trace.OperationID,
                reason: $"sideEffects:{trace.SideEffects.Length}",
                dryRun: trace.Mode != CanonicalKernelExecutionMode.productionExecute,
                generatedAt: trace.GeneratedAt.Date
            )
        );
    }
}

public static class CanonicalProductionCapabilityPortExtensions
{
    public static CanonicalProductionCapabilitySummary DefaultLocalCapabilities(
        this ICanonicalProductionCapabilityPort port)
    {
        return new CanonicalProductionCapabilitySummary(nodeID: "local");
    }

    public static CanonicalProductionCapabilitySummary? DefaultPeerCapabilities(
        this ICanonicalProductionCapabilityPort port)
    {
        return null;
    }

    public static bool DefaultValidateCapability(
        this ICanonicalProductionCapabilityPort port, CanonicalProductionDomain domain, CanonicalProductionOperation operation)
    {
        return port.Supports(domain, operation);
    }

    public static bool DefaultValidateSchema(
        this ICanonicalProductionCapabilityPort port, CanonicalManifest manifest)
    {
        return manifest.SchemaVersion == CanonicalManifest.CurrentSchemaVersion && manifest.HasValidManifestHash;
    }
}

// ─── CanonicalProductionRedaction ────────────────────────────────────────────

public static class CanonicalProductionRedaction
{
    public static string? HashPrefix(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return null;
        return normalized![..Math.Min(normalized.Length, 12)];
    }

    public static string SafeIdentifier(string value, string fallback)
    {
        return SafeDiagnosticText(value) ?? fallback;
    }

    public static string? SafeFileName(string? value)
    {
        var text = SafeDiagnosticText(value);
        if (text is null) return null;
        if (text.Contains('/') || text.Contains('\\'))
        {
            return $"redacted-file-{HashPrefix(CanonicalHash.Sha256String(text).Value) ?? "unknown"}";
        }
        return text;
    }

    public static string? SafeDiagnosticText(string? value)
    {
        if (value is null) return null;
        var trimmed = value
            .Trim()
            .Replace("\n", " ")
            .Replace("\r", " ");
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (ContainsSensitivePathSignal(trimmed))
        {
            return $"redacted-{HashPrefix(CanonicalHash.Sha256String(trimmed).Value) ?? "diagnostic"}";
        }
        return trimmed[..Math.Min(trimmed.Length, 160)];
    }

    public static bool ContainsSensitivePathSignal(string value)
    {
        var lowercased = value.ToLowerInvariant();
        return lowercased.Contains("file://")
            || lowercased.Contains("/users/")
            || lowercased.Contains("/private/")
            || lowercased.Contains("\\")
            || lowercased.StartsWith("~");
    }
}
