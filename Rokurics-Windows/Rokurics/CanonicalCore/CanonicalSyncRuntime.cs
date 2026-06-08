using System.Globalization;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncRuntimeMode
{
    disabled,
    diagnosticsOnly,
    canonicalPlanNoCommit,
    canonicalPlanPrimaryWithLegacyFallback,
    blocked
}

public static class CanonicalSyncRuntimeModeExtensions
{
    public static bool CanUseCanonicalAsPrimary(this CanonicalSyncRuntimeMode mode)
        => mode == CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback;

    public static bool EvaluatesCanonicalCandidate(this CanonicalSyncRuntimeMode mode)
        => mode switch
        {
            CanonicalSyncRuntimeMode.disabled => true,
            CanonicalSyncRuntimeMode.diagnosticsOnly => true,
            CanonicalSyncRuntimeMode.canonicalPlanNoCommit => true,
            CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback => true,
            CanonicalSyncRuntimeMode.blocked => false,
            _ => false
        };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncRuntimeDecisionScope
{
    recordingMetadata,
    libraryMetadata,
    recordingExistence
}

public sealed class CanonicalSyncRuntimePolicy : IEquatable<CanonicalSyncRuntimePolicy>
{
    public bool DebugInternalBuild { get; }
    public bool OwnerApproved { get; }
    public bool ReleaseDefaultBuild { get; }
    public bool LegacyFallbackAvailable { get; }
    public bool DiagnosticsRedacted { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool ReadPathLegacy { get; }
    public bool OtherActiveMigrationDomainConflicting { get; }
    public bool AllowDocumentedModifiedAtFallback { get; }
    public CanonicalSyncRuntimeDecisionScope[] EnabledScopes { get; }

    public CanonicalSyncRuntimePolicy(
        bool debugInternalBuild = false,
        bool ownerApproved = false,
        bool releaseDefaultBuild = true,
        bool legacyFallbackAvailable = true,
        bool diagnosticsRedacted = true,
        bool runtimeSwitchEnabled = false,
        bool readPathLegacy = true,
        bool otherActiveMigrationDomainConflicting = false,
        bool allowDocumentedModifiedAtFallback = false,
        CanonicalSyncRuntimeDecisionScope[]? enabledScopes = null)
    {
        DebugInternalBuild = debugInternalBuild;
        OwnerApproved = ownerApproved;
        ReleaseDefaultBuild = releaseDefaultBuild;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        DiagnosticsRedacted = diagnosticsRedacted;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        ReadPathLegacy = readPathLegacy;
        OtherActiveMigrationDomainConflicting = otherActiveMigrationDomainConflicting;
        AllowDocumentedModifiedAtFallback = allowDocumentedModifiedAtFallback;
        EnabledScopes = (enabledScopes ?? new[]
            {
                CanonicalSyncRuntimeDecisionScope.recordingMetadata,
                CanonicalSyncRuntimeDecisionScope.libraryMetadata,
                CanonicalSyncRuntimeDecisionScope.recordingExistence
            })
            .Distinct()
            .OrderBy(s => s.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncRuntimePolicy other && Equals(other);
    public bool Equals(CanonicalSyncRuntimePolicy? other) =>
        other is not null &&
        DebugInternalBuild == other.DebugInternalBuild &&
        OwnerApproved == other.OwnerApproved &&
        ReleaseDefaultBuild == other.ReleaseDefaultBuild &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        DiagnosticsRedacted == other.DiagnosticsRedacted &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        ReadPathLegacy == other.ReadPathLegacy &&
        OtherActiveMigrationDomainConflicting == other.OtherActiveMigrationDomainConflicting &&
        AllowDocumentedModifiedAtFallback == other.AllowDocumentedModifiedAtFallback &&
        EnabledScopes.SequenceEqual(other.EnabledScopes);
    public override int GetHashCode() =>
        HashCode.Combine(DebugInternalBuild, OwnerApproved, ReleaseDefaultBuild, LegacyFallbackAvailable, DiagnosticsRedacted);
    public static bool operator ==(CanonicalSyncRuntimePolicy left, CanonicalSyncRuntimePolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncRuntimePolicy left, CanonicalSyncRuntimePolicy right) => !left.Equals(right);
}

public sealed class CanonicalSyncRuntimeConfiguration : IEquatable<CanonicalSyncRuntimeConfiguration>
{
    public CanonicalSyncRuntimeMode Mode { get; }
    public CanonicalSyncRuntimePolicy Policy { get; }

    public CanonicalSyncRuntimeConfiguration(
        CanonicalSyncRuntimeMode mode = CanonicalSyncRuntimeMode.disabled,
        CanonicalSyncRuntimePolicy? policy = null)
    {
        Mode = mode;
        Policy = policy ?? new CanonicalSyncRuntimePolicy();
    }

    public static readonly CanonicalSyncRuntimeConfiguration Disabled = new();

    public override bool Equals(object? obj) => obj is CanonicalSyncRuntimeConfiguration other && Equals(other);
    public bool Equals(CanonicalSyncRuntimeConfiguration? other) =>
        other is not null && Mode == other.Mode && Policy.Equals(other.Policy);
    public override int GetHashCode() => HashCode.Combine(Mode, Policy);
    public static bool operator ==(CanonicalSyncRuntimeConfiguration left, CanonicalSyncRuntimeConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncRuntimeConfiguration left, CanonicalSyncRuntimeConfiguration right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncPlanAuthorityGateState
{
    allowed,
    allowedNoCommit,
    blockedMissingSnapshot,
    blockedInvalidManifest,
    blockedPeerUnavailable,
    blockedSchemaMismatch,
    blockedUnsupportedObjects,
    blockedFallbackRequiredObjects,
    blockedConflicts,
    blockedPeerUnknown,
    blockedReleaseDefault,
    blocked
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncPlanAuthorityBlocker
{
    missingInventorySnapshot,
    invalidLocalManifest,
    invalidPeerManifest,
    peerUnavailable,
    schemaMismatch,
    unsupportedObjects,
    fallbackRequiredObjects,
    unresolvedConflicts,
    peerUnknownAudio,
    legacyFallbackUnavailable,
    diagnosticsNotRedacted,
    runtimeSwitchEnabled,
    readPathNotLegacy,
    otherActiveMigrationDomain,
    releaseDefaultPrimary,
    debugInternalApprovalMissing,
    blockedMode,
    canonicalModifiedAtUnavailable
}

public sealed class CanonicalSyncPlanAuthorityGateResult : IEquatable<CanonicalSyncPlanAuthorityGateResult>
{
    public CanonicalSyncPlanAuthorityGateState State { get; }
    public CanonicalSyncPlanAuthorityBlocker[] Blockers { get; }
    public CanonicalSyncRuntimeMode Mode { get; }

    public bool IsAllowed => State == CanonicalSyncPlanAuthorityGateState.allowed
                             || State == CanonicalSyncPlanAuthorityGateState.allowedNoCommit;

    public bool ShouldUseCanonicalPrimary =>
        State == CanonicalSyncPlanAuthorityGateState.allowed
        && Mode == CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback;

    public bool ShouldRecordNoCommit =>
        State == CanonicalSyncPlanAuthorityGateState.allowedNoCommit
        || Mode == CanonicalSyncRuntimeMode.canonicalPlanNoCommit;

    public CanonicalSyncPlanAuthorityGateResult(
        CanonicalSyncPlanAuthorityGateState state,
        CanonicalSyncPlanAuthorityBlocker[] blockers,
        CanonicalSyncRuntimeMode mode)
    {
        State = state;
        Blockers = blockers ?? Array.Empty<CanonicalSyncPlanAuthorityBlocker>();
        Mode = mode;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncPlanAuthorityGateResult other && Equals(other);
    public bool Equals(CanonicalSyncPlanAuthorityGateResult? other) =>
        other is not null && State == other.State && Mode == other.Mode;
    public override int GetHashCode() => HashCode.Combine(State, Mode);
    public static bool operator ==(CanonicalSyncPlanAuthorityGateResult left, CanonicalSyncPlanAuthorityGateResult right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncPlanAuthorityGateResult left, CanonicalSyncPlanAuthorityGateResult right) => !left.Equals(right);
}

public sealed class CanonicalSyncPlanAuthorityGateContext : IEquatable<CanonicalSyncPlanAuthorityGateContext>
{
    public bool InventorySnapshotAvailable { get; }
    public CanonicalManifest? LocalManifest { get; }
    public CanonicalManifest? PeerManifest { get; }
    public bool PeerAbsenceExplicitlyModeled { get; }
    public string LocalMetadataHashSchemaVersion { get; }
    public string? PeerMetadataHashSchemaVersion { get; }
    public bool CanonicalModifiedAtSemanticsAvailable { get; }
    public int UnsupportedLegacyObjectCount { get; }
    public int LibraryFallbackRequiredObjectCount { get; }
    public int ConflictCount { get; }
    public int PeerUnknownAudioCount { get; }
    public bool LegacyFallbackAvailable { get; }
    public bool DiagnosticsRedacted { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool ReadPathLegacy { get; }
    public bool OtherActiveMigrationDomainConflicting { get; }
    public bool DebugInternalBuild { get; }
    public bool OwnerApproved { get; }
    public bool ReleaseDefaultBuild { get; }

    public CanonicalSyncPlanAuthorityGateContext(
        bool inventorySnapshotAvailable,
        CanonicalManifest? localManifest,
        CanonicalManifest? peerManifest,
        bool peerAbsenceExplicitlyModeled = false,
        string? localMetadataHashSchemaVersion = null,
        string? peerMetadataHashSchemaVersion = null,
        bool canonicalModifiedAtSemanticsAvailable = true,
        int unsupportedLegacyObjectCount = 0,
        int libraryFallbackRequiredObjectCount = 0,
        int conflictCount = 0,
        int peerUnknownAudioCount = 0,
        bool legacyFallbackAvailable = true,
        bool diagnosticsRedacted = true,
        bool runtimeSwitchEnabled = false,
        bool readPathLegacy = true,
        bool otherActiveMigrationDomainConflicting = false,
        bool debugInternalBuild = false,
        bool ownerApproved = false,
        bool releaseDefaultBuild = true)
    {
        InventorySnapshotAvailable = inventorySnapshotAvailable;
        LocalManifest = localManifest;
        PeerManifest = peerManifest;
        PeerAbsenceExplicitlyModeled = peerAbsenceExplicitlyModeled;
        LocalMetadataHashSchemaVersion = localMetadataHashSchemaVersion ?? CanonicalRecordingMetadata.BusinessMetadataHashSchemaVersion;
        PeerMetadataHashSchemaVersion = peerMetadataHashSchemaVersion ?? CanonicalRecordingMetadata.BusinessMetadataHashSchemaVersion;
        CanonicalModifiedAtSemanticsAvailable = canonicalModifiedAtSemanticsAvailable;
        UnsupportedLegacyObjectCount = unsupportedLegacyObjectCount;
        LibraryFallbackRequiredObjectCount = libraryFallbackRequiredObjectCount;
        ConflictCount = conflictCount;
        PeerUnknownAudioCount = peerUnknownAudioCount;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        DiagnosticsRedacted = diagnosticsRedacted;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        ReadPathLegacy = readPathLegacy;
        OtherActiveMigrationDomainConflicting = otherActiveMigrationDomainConflicting;
        DebugInternalBuild = debugInternalBuild;
        OwnerApproved = ownerApproved;
        ReleaseDefaultBuild = releaseDefaultBuild;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncPlanAuthorityGateContext other && Equals(other);
    public bool Equals(CanonicalSyncPlanAuthorityGateContext? other) =>
        other is not null &&
        InventorySnapshotAvailable == other.InventorySnapshotAvailable &&
        Equals(LocalManifest, other.LocalManifest) &&
        Equals(PeerManifest, other.PeerManifest) &&
        PeerAbsenceExplicitlyModeled == other.PeerAbsenceExplicitlyModeled &&
        LocalMetadataHashSchemaVersion == other.LocalMetadataHashSchemaVersion &&
        PeerMetadataHashSchemaVersion == other.PeerMetadataHashSchemaVersion &&
        CanonicalModifiedAtSemanticsAvailable == other.CanonicalModifiedAtSemanticsAvailable &&
        UnsupportedLegacyObjectCount == other.UnsupportedLegacyObjectCount &&
        LibraryFallbackRequiredObjectCount == other.LibraryFallbackRequiredObjectCount &&
        ConflictCount == other.ConflictCount &&
        PeerUnknownAudioCount == other.PeerUnknownAudioCount &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        DiagnosticsRedacted == other.DiagnosticsRedacted &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        ReadPathLegacy == other.ReadPathLegacy &&
        OtherActiveMigrationDomainConflicting == other.OtherActiveMigrationDomainConflicting &&
        DebugInternalBuild == other.DebugInternalBuild &&
        OwnerApproved == other.OwnerApproved &&
        ReleaseDefaultBuild == other.ReleaseDefaultBuild;
    public override int GetHashCode() =>
        HashCode.Combine(InventorySnapshotAvailable, LocalMetadataHashSchemaVersion,
            UnsupportedLegacyObjectCount, ConflictCount, LegacyFallbackAvailable);
    public static bool operator ==(CanonicalSyncPlanAuthorityGateContext left, CanonicalSyncPlanAuthorityGateContext right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncPlanAuthorityGateContext left, CanonicalSyncPlanAuthorityGateContext right) => !left.Equals(right);
}

public sealed class CanonicalSyncPlanAuthorityGate
{
    public CanonicalSyncPlanAuthorityGateResult Evaluate(
        CanonicalSyncRuntimeConfiguration configuration,
        CanonicalSyncPlanAuthorityGateContext context)
    {
        var mode = configuration.Mode;
        if (mode == CanonicalSyncRuntimeMode.blocked)
            return Result(CanonicalSyncPlanAuthorityGateState.blocked,
                new[] { CanonicalSyncPlanAuthorityBlocker.blockedMode }, mode);

        var blockers = new List<CanonicalSyncPlanAuthorityBlocker>();
        if (!context.InventorySnapshotAvailable)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.missingInventorySnapshot);

        if (context.LocalManifest != null)
        {
            if (context.LocalManifest.SchemaVersion != CanonicalManifest.CurrentSchemaVersion
                || !context.LocalManifest.HasValidManifestHash)
                blockers.Add(CanonicalSyncPlanAuthorityBlocker.invalidLocalManifest);
        }
        else
        {
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.invalidLocalManifest);
        }

        if (context.PeerManifest != null)
        {
            if (context.PeerManifest.SchemaVersion != CanonicalManifest.CurrentSchemaVersion
                || !context.PeerManifest.HasValidManifestHash)
                blockers.Add(CanonicalSyncPlanAuthorityBlocker.invalidPeerManifest);
        }
        else if (!context.PeerAbsenceExplicitlyModeled)
        {
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.peerUnavailable);
        }

        if (context.PeerManifest != null
            && context.PeerMetadataHashSchemaVersion != context.LocalMetadataHashSchemaVersion)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.schemaMismatch);

        if (!context.CanonicalModifiedAtSemanticsAvailable
            && !configuration.Policy.AllowDocumentedModifiedAtFallback)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.canonicalModifiedAtUnavailable);

        if (context.UnsupportedLegacyObjectCount > 0)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.unsupportedObjects);

        if (context.LibraryFallbackRequiredObjectCount > 0)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.fallbackRequiredObjects);

        if (mode.CanUseCanonicalAsPrimary() && context.ConflictCount > 0)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.unresolvedConflicts);

        if (mode.CanUseCanonicalAsPrimary() && context.PeerUnknownAudioCount > 0)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.peerUnknownAudio);

        if (!context.LegacyFallbackAvailable || !configuration.Policy.LegacyFallbackAvailable)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.legacyFallbackUnavailable);

        if (!context.DiagnosticsRedacted || !configuration.Policy.DiagnosticsRedacted)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.diagnosticsNotRedacted);

        if (context.RuntimeSwitchEnabled || configuration.Policy.RuntimeSwitchEnabled)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.runtimeSwitchEnabled);

        if (!context.ReadPathLegacy || !configuration.Policy.ReadPathLegacy)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.readPathNotLegacy);

        if (context.OtherActiveMigrationDomainConflicting || configuration.Policy.OtherActiveMigrationDomainConflicting)
            blockers.Add(CanonicalSyncPlanAuthorityBlocker.otherActiveMigrationDomain);

        if (mode.CanUseCanonicalAsPrimary())
        {
            if (context.ReleaseDefaultBuild || configuration.Policy.ReleaseDefaultBuild)
                blockers.Add(CanonicalSyncPlanAuthorityBlocker.releaseDefaultPrimary);

            if (!context.DebugInternalBuild || !configuration.Policy.DebugInternalBuild
                || !context.OwnerApproved || !configuration.Policy.OwnerApproved)
                blockers.Add(CanonicalSyncPlanAuthorityBlocker.debugInternalApprovalMissing);
        }

        var uniqueBlockers = blockers.Distinct()
            .OrderBy(b => b.ToString(), StringComparer.Ordinal)
            .ToArray();

        var primaryState = BlockedStateFor(uniqueBlockers);
        if (primaryState.HasValue)
            return Result(primaryState.Value, uniqueBlockers, mode);

        return mode switch
        {
            CanonicalSyncRuntimeMode.disabled => Result(CanonicalSyncPlanAuthorityGateState.allowedNoCommit,
                Array.Empty<CanonicalSyncPlanAuthorityBlocker>(), mode),
            CanonicalSyncRuntimeMode.diagnosticsOnly => Result(CanonicalSyncPlanAuthorityGateState.allowedNoCommit,
                Array.Empty<CanonicalSyncPlanAuthorityBlocker>(), mode),
            CanonicalSyncRuntimeMode.canonicalPlanNoCommit => Result(CanonicalSyncPlanAuthorityGateState.allowedNoCommit,
                Array.Empty<CanonicalSyncPlanAuthorityBlocker>(), mode),
            CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback => Result(
                CanonicalSyncPlanAuthorityGateState.allowed, Array.Empty<CanonicalSyncPlanAuthorityBlocker>(), mode),
            CanonicalSyncRuntimeMode.blocked => Result(CanonicalSyncPlanAuthorityGateState.blocked,
                new[] { CanonicalSyncPlanAuthorityBlocker.blockedMode }, mode),
            _ => Result(CanonicalSyncPlanAuthorityGateState.blocked,
                new[] { CanonicalSyncPlanAuthorityBlocker.blockedMode }, mode)
        };
    }

    private static CanonicalSyncPlanAuthorityGateState? BlockedStateFor(
        CanonicalSyncPlanAuthorityBlocker[] blockers)
    {
        if (blockers.Length == 0) return null;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.missingInventorySnapshot))
            return CanonicalSyncPlanAuthorityGateState.blockedMissingSnapshot;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.invalidLocalManifest)
            || blockers.Contains(CanonicalSyncPlanAuthorityBlocker.invalidPeerManifest))
            return CanonicalSyncPlanAuthorityGateState.blockedInvalidManifest;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.peerUnavailable))
            return CanonicalSyncPlanAuthorityGateState.blockedPeerUnavailable;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.schemaMismatch))
            return CanonicalSyncPlanAuthorityGateState.blockedSchemaMismatch;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.unsupportedObjects))
            return CanonicalSyncPlanAuthorityGateState.blockedUnsupportedObjects;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.fallbackRequiredObjects))
            return CanonicalSyncPlanAuthorityGateState.blockedFallbackRequiredObjects;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.unresolvedConflicts))
            return CanonicalSyncPlanAuthorityGateState.blockedConflicts;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.peerUnknownAudio))
            return CanonicalSyncPlanAuthorityGateState.blockedPeerUnknown;
        if (blockers.Contains(CanonicalSyncPlanAuthorityBlocker.releaseDefaultPrimary)
            || blockers.Contains(CanonicalSyncPlanAuthorityBlocker.debugInternalApprovalMissing))
            return CanonicalSyncPlanAuthorityGateState.blockedReleaseDefault;
        return CanonicalSyncPlanAuthorityGateState.blocked;
    }

    private static CanonicalSyncPlanAuthorityGateResult Result(
        CanonicalSyncPlanAuthorityGateState state,
        CanonicalSyncPlanAuthorityBlocker[] blockers,
        CanonicalSyncRuntimeMode mode)
        => new(state, blockers, mode);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncRuntimeDiagnosticKind
{
    canonicalSyncRuntimeModeEvaluated,
    canonicalSyncRuntimeAuthorityGateAllowed,
    canonicalSyncRuntimeAuthorityGateBlocked,
    canonicalSyncRuntimePlanEvaluated,
    canonicalSyncRuntimePlanAllowed,
    canonicalSyncRuntimePlanUsed,
    canonicalSyncRuntimePlanNoCommit,
    canonicalSyncRuntimePlanFallback,
    canonicalSyncRuntimePlanBlocked,
    canonicalSyncRuntimeLegacyHashMismatchIgnored,
    canonicalSyncRuntimeUnsupportedObjectBlocked,
    canonicalSyncRuntimeConflictBlocked,
    canonicalSyncRuntimePeerSnapshotUnavailable,
    canonicalSyncRuntimeDuplicateLegacySuppressed,
    canonicalSyncRuntimeDuplicateExecutionPrevented,
    canonicalSyncRuntimeMetadataHashEqual,
    canonicalSyncRuntimeModifiedAtLWWApplied,
    canonicalSyncRuntimeModifiedAtUnavailable,
    canonicalSyncRuntimeSchemaMismatch,
    canonicalExistenceTruthEvaluated,
    canonicalExistenceApplyBridgeEvaluated,
    canonicalExistenceApplyBridgeBlocked,
    canonicalExistenceMetadataOnlyRecordWritten,
    canonicalExistenceMetadataOnlyRecordNoOp,
    canonicalExistenceApplyBridgeRollbackStarted,
    canonicalExistenceApplyBridgeRollbackCompleted,
    canonicalExistenceApplyBridgeRollbackFailed,
    canonicalExistencePeerMetadataOnlyUploadCandidate,
    canonicalExistencePeerAbsentMetadataBridgeRequired,
    canonicalExistencePeerUnknownDeferred,
    canonicalExistenceAudioSameNoOp,
    canonicalExistenceAudioConflict,
    canonicalExistenceManifestRecordingsConsumed,
    canonicalExistenceManifestRecordingsIgnoredBlocked,
    canonicalExistenceDidNotWriteAudio,
    canonicalExistenceDidNotMarkAudioAvailable,
    canonicalApplyRuntimeModeEvaluated,
    canonicalApplyRuntimeGateAllowed,
    canonicalApplyRuntimeGateBlocked,
    canonicalApplyRuntimeActionStarted,
    canonicalApplyRuntimeActionCompleted,
    canonicalApplyRuntimeActionFailed,
    canonicalApplyRuntimeRollbackStarted,
    canonicalApplyRuntimeRollbackCompleted,
    canonicalApplyRuntimeRollbackFailed,
    canonicalApplyRuntimeLegacyFallbackUsed,
    canonicalApplyRuntimeDuplicateLegacySuppressed,
    canonicalApplyRuntimeAudioActionBlocked,
    canonicalApplyRuntimeReportBuilt
}

public sealed class CanonicalSyncRuntimeDiagnostic : IEquatable<CanonicalSyncRuntimeDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), SyncRunID ?? "", ObjectID ?? "", ActionKind ?? "", Detail ?? "");
    public CanonicalSyncRuntimeDiagnosticKind Kind { get; }
    public string? SyncRunID { get; }
    public CanonicalSyncRuntimeMode Mode { get; }
    public string? ObjectID { get; }
    public string? ActionKind { get; }
    public string? HashPrefix { get; }
    public int? Count { get; }
    public string? Detail { get; }

    public CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind kind,
        string? syncRunID = null,
        CanonicalSyncRuntimeMode mode = CanonicalSyncRuntimeMode.disabled,
        string? objectID = null,
        string? actionKind = null,
        CanonicalHash? hash = null,
        string? hashPrefix = null,
        int? count = null,
        string? detail = null)
    {
        Kind = kind;
        SyncRunID = SafeText(syncRunID);
        Mode = mode;
        ObjectID = SafeText(objectID)?.NullSafeSubstring(0, 48);
        ActionKind = SafeText(actionKind);
        HashPrefix = hash != null ? HashPrefixValue(hash.Value) : (hashPrefix != null ? HashPrefixValue(hashPrefix) : null);
        Count = count;
        Detail = SafeText(detail);
    }

    public bool IsRedacted
    {
        get
        {
            var values = new[] { SyncRunID, ObjectID, ActionKind, HashPrefix, Detail };
            var nonNull = values.Where(v => v != null).Cast<string>().ToArray();
            if (!nonNull.All(v => !v.Contains("/") && !v.Contains("\\") && !v.Contains("://") && !v.Contains("{") && !v.Contains("}")))
                return false;
            return HashPrefix == null || HashPrefix.Length <= 12;
        }
    }

    public string Summary()
    {
        var parts = new List<string> { $"mode={Mode}" };
        if (SyncRunID != null) parts.Add($"syncRunID={SyncRunID}");
        if (ObjectID != null) parts.Add($"objectID={ObjectID}");
        if (ActionKind != null) parts.Add($"action={ActionKind}");
        if (HashPrefix != null) parts.Add($"hashPrefix={HashPrefix}");
        if (Count.HasValue) parts.Add($"count={Count}");
        if (Detail != null) parts.Add($"detail={Detail}");
        return string.Join(",", parts);
    }

    private static string HashPrefixValue(string value)
        => value.Trim()[..Math.Min(value.Trim().Length, 12)];

    private static string? SafeText(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        var forbidden = new[] { "/", "\\", "://", "{", "}", "\n", "\r" };
        if (forbidden.Any(f => trimmed.Contains(f)))
        {
            var sanitized = forbidden.Aggregate(trimmed, (current, token) => current.Replace(token, "_"));
            return sanitized[..Math.Min(sanitized.Length, 12)];
        }
        return trimmed;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncRuntimeDiagnostic other && Equals(other);
    public bool Equals(CanonicalSyncRuntimeDiagnostic? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalSyncRuntimeDiagnostic left, CanonicalSyncRuntimeDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncRuntimeDiagnostic left, CanonicalSyncRuntimeDiagnostic right) => !left.Equals(right);
}

public sealed class CanonicalSyncRuntimeResult : IEquatable<CanonicalSyncRuntimeResult>
{
    public CanonicalSyncRuntimeMode Mode { get; }
    public CanonicalSyncPlanAuthorityGateResult GateResult { get; }
    public bool CanonicalPlanUsed { get; }
    public bool CanonicalPlanFallback { get; }
    public bool CanonicalPlanBlocked { get; }
    public bool CanonicalPlanNoCommit { get; }
    public CanonicalSyncRuntimeDiagnostic[] Diagnostics { get; }

    public CanonicalSyncRuntimeResult(
        CanonicalSyncRuntimeMode mode,
        CanonicalSyncPlanAuthorityGateResult gateResult,
        bool canonicalPlanUsed,
        bool canonicalPlanFallback,
        bool canonicalPlanBlocked,
        bool canonicalPlanNoCommit,
        CanonicalSyncRuntimeDiagnostic[] diagnostics)
    {
        Mode = mode;
        GateResult = gateResult;
        CanonicalPlanUsed = canonicalPlanUsed;
        CanonicalPlanFallback = canonicalPlanFallback;
        CanonicalPlanBlocked = canonicalPlanBlocked;
        CanonicalPlanNoCommit = canonicalPlanNoCommit;
        Diagnostics = diagnostics ?? Array.Empty<CanonicalSyncRuntimeDiagnostic>();
    }

    public static CanonicalSyncRuntimeResult Make(
        CanonicalSyncRuntimeMode mode,
        CanonicalSyncPlanAuthorityGateResult gateResult,
        string? syncRunID,
        CanonicalSyncRuntimeDiagnostic[]? extraDiagnostics = null)
    {
        var used = gateResult.ShouldUseCanonicalPrimary;
        var noCommit = !used && gateResult.IsAllowed;
        var blocked = !gateResult.IsAllowed;

        var diagnostics = new List<CanonicalSyncRuntimeDiagnostic>
        {
            new(
                kind: CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimeModeEvaluated,
                syncRunID: syncRunID,
                mode: mode,
                detail: $"state={gateResult.State}")
            ,
            new(
                kind: CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimePlanEvaluated,
                syncRunID: syncRunID,
                mode: mode,
                count: gateResult.Blockers.Length,
                detail: gateResult.State.ToString())
            ,
            new(
                kind: gateResult.IsAllowed
                    ? CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimeAuthorityGateAllowed
                    : CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimeAuthorityGateBlocked,
                syncRunID: syncRunID,
                mode: mode,
                count: gateResult.Blockers.Length,
                detail: string.Join("+", gateResult.Blockers.Select(b => b.ToString())).NilIfEmpty() ?? "none")
            ,
            new(
                kind: gateResult.IsAllowed
                    ? CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimePlanAllowed
                    : CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimePlanBlocked,
                syncRunID: syncRunID,
                mode: mode,
                count: gateResult.Blockers.Length,
                detail: gateResult.State.ToString())
        };

        if (used)
            diagnostics.Add(new CanonicalSyncRuntimeDiagnostic(
                CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimePlanUsed,
                syncRunID: syncRunID, mode: mode, detail: "primary"));
        else if (noCommit)
            diagnostics.Add(new CanonicalSyncRuntimeDiagnostic(
                CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimePlanNoCommit,
                syncRunID: syncRunID, mode: mode, detail: "legacyOwner"));
        else
            diagnostics.Add(new CanonicalSyncRuntimeDiagnostic(
                CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimePlanFallback,
                syncRunID: syncRunID, mode: mode, count: gateResult.Blockers.Length,
                detail: gateResult.State.ToString()));

        if (extraDiagnostics != null)
            diagnostics.AddRange(extraDiagnostics);

        return new CanonicalSyncRuntimeResult(
            mode: mode,
            gateResult: gateResult,
            canonicalPlanUsed: used,
            canonicalPlanFallback: !used,
            canonicalPlanBlocked: blocked,
            canonicalPlanNoCommit: noCommit,
            diagnostics: diagnostics.ToArray());
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncRuntimeResult other && Equals(other);
    public bool Equals(CanonicalSyncRuntimeResult? other) =>
        other is not null &&
        Mode == other.Mode &&
        CanonicalPlanUsed == other.CanonicalPlanUsed &&
        CanonicalPlanFallback == other.CanonicalPlanFallback &&
        CanonicalPlanBlocked == other.CanonicalPlanBlocked &&
        CanonicalPlanNoCommit == other.CanonicalPlanNoCommit;
    public override int GetHashCode() =>
        HashCode.Combine(Mode, CanonicalPlanUsed, CanonicalPlanFallback, CanonicalPlanBlocked, CanonicalPlanNoCommit);
    public static bool operator ==(CanonicalSyncRuntimeResult left, CanonicalSyncRuntimeResult right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncRuntimeResult left, CanonicalSyncRuntimeResult right) => !left.Equals(right);
}

public sealed class CanonicalSyncRuntimeActionIdentity : IEquatable<CanonicalSyncRuntimeActionIdentity>
{
    public CanonicalSyncRuntimeDecisionScope Scope { get; }
    public string ObjectID { get; }
    public string ActionKind { get; }

    public CanonicalSyncRuntimeActionIdentity(
        CanonicalSyncRuntimeDecisionScope scope,
        string objectID,
        string actionKind)
    {
        Scope = scope;
        ObjectID = objectID.Trim();
        ActionKind = actionKind.Trim();
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncRuntimeActionIdentity other && Equals(other);
    public bool Equals(CanonicalSyncRuntimeActionIdentity? other) =>
        other is not null &&
        Scope == other.Scope &&
        ObjectID == other.ObjectID &&
        ActionKind == other.ActionKind;
    public override int GetHashCode() => HashCode.Combine(Scope, ObjectID, ActionKind);
    public static bool operator ==(CanonicalSyncRuntimeActionIdentity left, CanonicalSyncRuntimeActionIdentity right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncRuntimeActionIdentity left, CanonicalSyncRuntimeActionIdentity right) => !left.Equals(right);
}

public sealed class CanonicalSyncRuntimeDuplicateExecutionGuardResult : IEquatable<CanonicalSyncRuntimeDuplicateExecutionGuardResult>
{
    public CanonicalSyncRuntimeActionIdentity[] SuppressedLegacyActions { get; }
    public CanonicalSyncRuntimeActionIdentity[] PreventedDuplicateActions { get; }
    public CanonicalSyncRuntimeDiagnostic[] Diagnostics { get; }

    public CanonicalSyncRuntimeDuplicateExecutionGuardResult(
        CanonicalSyncRuntimeActionIdentity[] suppressedLegacyActions,
        CanonicalSyncRuntimeActionIdentity[] preventedDuplicateActions,
        CanonicalSyncRuntimeDiagnostic[] diagnostics)
    {
        SuppressedLegacyActions = suppressedLegacyActions ?? Array.Empty<CanonicalSyncRuntimeActionIdentity>();
        PreventedDuplicateActions = preventedDuplicateActions ?? Array.Empty<CanonicalSyncRuntimeActionIdentity>();
        Diagnostics = diagnostics ?? Array.Empty<CanonicalSyncRuntimeDiagnostic>();
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncRuntimeDuplicateExecutionGuardResult other && Equals(other);
    public bool Equals(CanonicalSyncRuntimeDuplicateExecutionGuardResult? other) =>
        other is not null &&
        SuppressedLegacyActions.SequenceEqual(other.SuppressedLegacyActions) &&
        PreventedDuplicateActions.SequenceEqual(other.PreventedDuplicateActions);
    public override int GetHashCode() =>
        HashCode.Combine(SuppressedLegacyActions.Length, PreventedDuplicateActions.Length, Diagnostics.Length);
    public static bool operator ==(CanonicalSyncRuntimeDuplicateExecutionGuardResult left, CanonicalSyncRuntimeDuplicateExecutionGuardResult right) => left.Equals(right);
    public static bool operator !=(CanonicalSyncRuntimeDuplicateExecutionGuardResult left, CanonicalSyncRuntimeDuplicateExecutionGuardResult right) => !left.Equals(right);
}

public sealed class CanonicalSyncRuntimeDuplicateExecutionGuard
{
    public CanonicalSyncRuntimeDuplicateExecutionGuardResult Evaluate(
        bool canonicalOwnerUsed,
        CanonicalSyncRuntimeMode mode,
        string? syncRunID,
        CanonicalSyncRuntimeActionIdentity[] canonicalActions,
        CanonicalSyncRuntimeActionIdentity[] legacyActions,
        CanonicalSyncRuntimeDecisionScope[] enabledScopes)
    {
        if (!canonicalOwnerUsed)
            return new CanonicalSyncRuntimeDuplicateExecutionGuardResult(
                Array.Empty<CanonicalSyncRuntimeActionIdentity>(),
                Array.Empty<CanonicalSyncRuntimeActionIdentity>(),
                Array.Empty<CanonicalSyncRuntimeDiagnostic>());

        var enabled = new HashSet<CanonicalSyncRuntimeDecisionScope>(enabledScopes);
        var canonicalSet = new HashSet<CanonicalSyncRuntimeActionIdentity>(
            canonicalActions.Where(a => enabled.Contains(a.Scope)));

        var duplicates = legacyActions
            .Where(a => canonicalSet.Contains(a))
            .OrderBy(a => string.Join("|", a.Scope.ToString(), a.ObjectID, a.ActionKind),
                StringComparer.Ordinal)
            .ToArray();

        var diagnostics = duplicates
            .Select(d => new CanonicalSyncRuntimeDiagnostic(
                kind: CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimeDuplicateExecutionPrevented,
                syncRunID: syncRunID,
                mode: mode,
                objectID: d.ObjectID,
                actionKind: d.ActionKind,
                detail: d.Scope.ToString()))
            .ToList();

        if (duplicates.Length > 0)
            diagnostics.Add(new CanonicalSyncRuntimeDiagnostic(
                kind: CanonicalSyncRuntimeDiagnosticKind.canonicalSyncRuntimeDuplicateLegacySuppressed,
                syncRunID: syncRunID,
                mode: mode,
                count: duplicates.Length,
                detail: "exactScopeObjectAction"));

        return new CanonicalSyncRuntimeDuplicateExecutionGuardResult(
            suppressedLegacyActions: duplicates,
            preventedDuplicateActions: duplicates,
            diagnostics: diagnostics.ToArray());
    }
}

internal static class CanonicalSyncRuntimeStringExtensions
{
    public static string? NilIfEmpty(this string? value) =>
        string.IsNullOrEmpty(value) ? null : value;

    public static string? NullSafeSubstring(this string? value, int startIndex, int length)
    {
        if (value == null) return null;
        return value.Length <= length ? value : value.Substring(startIndex, length);
    }
}
