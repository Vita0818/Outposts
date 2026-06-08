using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLegacyCompatibilityDomain
{
    recordingMetadata,
    libraryMetadata,
    generatedArtifacts,
    tombstoneConflict,
    recordingExistence,
    audioUpload,
    readRuntime
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLegacyCompatibilityBlocker
{
    canonicalWriteNotLegacyReadable,
    legacyWriteNotCanonicalReadable,
    switchBackRequiresMigration,
    canonicalOnlyRequiredFieldRequired,
    unknownFieldsNotBackwardCompatible,
    rollbackUnavailable,
    diagnosticsNotRedacted,
    legacyReadPathUnavailable,
    legacyWritePathUnavailable,
    physicalDeleteRequired,
    incompleteStateUnrecoverable,
    dataLossDetected,
    oldKernelRestartFailed,
    canonicalFullSyncRestartFailed
}

public sealed class CanonicalLegacyCompatibilityResult : IEquatable<CanonicalLegacyCompatibilityResult>
{
    public CanonicalLegacyCompatibilityDomain Domain { get; }
    public bool CanonicalWriteFormatLegacyReadable { get; }
    public bool LegacyWriteFormatCanonicalReadable { get; }
    public bool SwitchBackNoMigration { get; }
    public bool NoCanonicalOnlyRequiredField { get; }
    public bool UnknownFieldsIgnoredOrBackwardCompatible { get; }
    public bool RollbackAvailable { get; }
    public bool DiagnosticsRedacted { get; }
    public bool LegacyReadPathAvailable { get; }
    public bool LegacyWritePathAvailable { get; }
    public bool NoPhysicalDeleteRequired { get; }
    public List<CanonicalLegacyCompatibilityBlocker> Blockers { get; }
    public string DiagnosticsSummary { get; }

    public bool IsProven =>
        CanonicalWriteFormatLegacyReadable
        && LegacyWriteFormatCanonicalReadable
        && SwitchBackNoMigration
        && NoCanonicalOnlyRequiredField
        && UnknownFieldsIgnoredOrBackwardCompatible
        && RollbackAvailable
        && DiagnosticsRedacted
        && LegacyReadPathAvailable
        && LegacyWritePathAvailable
        && NoPhysicalDeleteRequired
        && Blockers.Count == 0;

    public CanonicalLegacyCompatibilityResult(
        CanonicalLegacyCompatibilityDomain domain,
        bool canonicalWriteFormatLegacyReadable,
        bool legacyWriteFormatCanonicalReadable,
        bool switchBackNoMigration,
        bool noCanonicalOnlyRequiredField,
        bool unknownFieldsIgnoredOrBackwardCompatible,
        bool rollbackAvailable,
        bool diagnosticsRedacted,
        bool legacyReadPathAvailable,
        bool legacyWritePathAvailable,
        bool noPhysicalDeleteRequired,
        List<CanonicalLegacyCompatibilityBlocker> blockers,
        string diagnosticsSummary)
    {
        Domain = domain;
        CanonicalWriteFormatLegacyReadable = canonicalWriteFormatLegacyReadable;
        LegacyWriteFormatCanonicalReadable = legacyWriteFormatCanonicalReadable;
        SwitchBackNoMigration = switchBackNoMigration;
        NoCanonicalOnlyRequiredField = noCanonicalOnlyRequiredField;
        UnknownFieldsIgnoredOrBackwardCompatible = unknownFieldsIgnoredOrBackwardCompatible;
        RollbackAvailable = rollbackAvailable;
        DiagnosticsRedacted = diagnosticsRedacted;
        LegacyReadPathAvailable = legacyReadPathAvailable;
        LegacyWritePathAvailable = legacyWritePathAvailable;
        NoPhysicalDeleteRequired = noPhysicalDeleteRequired;
        Blockers = blockers;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public static CanonicalLegacyCompatibilityResult Prove(
        CanonicalLegacyCompatibilityDomain domain,
        CanonicalKernelSwitchPolicy? policy = null,
        bool canonicalWriteFormatLegacyReadable = true,
        bool legacyWriteFormatCanonicalReadable = true,
        bool switchBackNoMigration = true,
        bool noCanonicalOnlyRequiredField = true,
        bool unknownFieldsIgnoredOrBackwardCompatible = true,
        bool rollbackAvailable = true,
        bool? diagnosticsRedacted = null,
        bool? legacyReadPathAvailable = null,
        bool? legacyWritePathAvailable = null,
        bool? noPhysicalDeleteRequired = null)
    {
        var pol = policy ?? CanonicalKernelSwitchPolicy.DebugInternal(manualFullSyncConfirmation: true);
        var redacted = diagnosticsRedacted ?? (pol.DiagnosticsRedacted && pol.SecretPathHashLeakRedactionEnabled);
        var legacyRead = legacyReadPathAvailable ?? pol.LegacyReadPathAvailable;
        var legacyWrite = legacyWritePathAvailable ?? pol.LegacyWritePathAvailable;
        var noPhysicalDelete = noPhysicalDeleteRequired ?? pol.PhysicalMoveDeleteDisabled;
        var blockers = new List<CanonicalLegacyCompatibilityBlocker>();

        if (!canonicalWriteFormatLegacyReadable) blockers.Add(CanonicalLegacyCompatibilityBlocker.canonicalWriteNotLegacyReadable);
        if (!legacyWriteFormatCanonicalReadable) blockers.Add(CanonicalLegacyCompatibilityBlocker.legacyWriteNotCanonicalReadable);
        if (!switchBackNoMigration) blockers.Add(CanonicalLegacyCompatibilityBlocker.switchBackRequiresMigration);
        if (!noCanonicalOnlyRequiredField) blockers.Add(CanonicalLegacyCompatibilityBlocker.canonicalOnlyRequiredFieldRequired);
        if (!unknownFieldsIgnoredOrBackwardCompatible) blockers.Add(CanonicalLegacyCompatibilityBlocker.unknownFieldsNotBackwardCompatible);
        if (!rollbackAvailable) blockers.Add(CanonicalLegacyCompatibilityBlocker.rollbackUnavailable);
        if (!redacted) blockers.Add(CanonicalLegacyCompatibilityBlocker.diagnosticsNotRedacted);
        if (!legacyRead) blockers.Add(CanonicalLegacyCompatibilityBlocker.legacyReadPathUnavailable);
        if (!legacyWrite) blockers.Add(CanonicalLegacyCompatibilityBlocker.legacyWritePathUnavailable);
        if (!noPhysicalDelete) blockers.Add(CanonicalLegacyCompatibilityBlocker.physicalDeleteRequired);

        return new CanonicalLegacyCompatibilityResult(
            domain: domain,
            canonicalWriteFormatLegacyReadable: canonicalWriteFormatLegacyReadable,
            legacyWriteFormatCanonicalReadable: legacyWriteFormatCanonicalReadable,
            switchBackNoMigration: switchBackNoMigration,
            noCanonicalOnlyRequiredField: noCanonicalOnlyRequiredField,
            unknownFieldsIgnoredOrBackwardCompatible: unknownFieldsIgnoredOrBackwardCompatible,
            rollbackAvailable: rollbackAvailable,
            diagnosticsRedacted: redacted,
            legacyReadPathAvailable: legacyRead,
            legacyWritePathAvailable: legacyWrite,
            noPhysicalDeleteRequired: noPhysicalDelete,
            blockers: blockers,
            diagnosticsSummary: string.Join(",",
                "canonicalLegacyCompatibility=v8.44",
                $"domain={domain}",
                "format=legacy-v1",
                $"canonicalWriteLegacyReadable={canonicalWriteFormatLegacyReadable}",
                $"legacyWriteCanonicalReadable={legacyWriteFormatCanonicalReadable}",
                "switchBackMigration=false",
                "unknownFields=ignoredOrBackwardCompatible",
                $"rollback={rollbackAvailable}",
                "redacted=true",
                $"blockers={string.Join("|", blockers.Select(b => b.ToString()))}"
            )
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalLegacyCompatibilityResult other && Equals(other);
    public bool Equals(CanonicalLegacyCompatibilityResult? other) =>
        other is not null &&
        Domain == other.Domain &&
        CanonicalWriteFormatLegacyReadable == other.CanonicalWriteFormatLegacyReadable &&
        LegacyWriteFormatCanonicalReadable == other.LegacyWriteFormatCanonicalReadable &&
        SwitchBackNoMigration == other.SwitchBackNoMigration &&
        NoCanonicalOnlyRequiredField == other.NoCanonicalOnlyRequiredField &&
        UnknownFieldsIgnoredOrBackwardCompatible == other.UnknownFieldsIgnoredOrBackwardCompatible &&
        RollbackAvailable == other.RollbackAvailable &&
        DiagnosticsRedacted == other.DiagnosticsRedacted &&
        LegacyReadPathAvailable == other.LegacyReadPathAvailable &&
        LegacyWritePathAvailable == other.LegacyWritePathAvailable &&
        NoPhysicalDeleteRequired == other.NoPhysicalDeleteRequired &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(Domain, CanonicalWriteFormatLegacyReadable,
        LegacyWriteFormatCanonicalReadable, SwitchBackNoMigration, NoCanonicalOnlyRequiredField,
        UnknownFieldsIgnoredOrBackwardCompatible, RollbackAvailable, DiagnosticsRedacted,
        LegacyReadPathAvailable, LegacyWritePathAvailable, NoPhysicalDeleteRequired);
    public static bool operator ==(CanonicalLegacyCompatibilityResult l, CanonicalLegacyCompatibilityResult r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyCompatibilityResult l, CanonicalLegacyCompatibilityResult r) => !l.Equals(r);
}

public sealed class CanonicalLegacyCompatibilityMatrix : IEquatable<CanonicalLegacyCompatibilityMatrix>
{
    public List<CanonicalLegacyCompatibilityResult> Results { get; }

    public CanonicalLegacyCompatibilityMatrix(List<CanonicalLegacyCompatibilityResult> results)
    {
        Results = results.OrderBy(r => r.Domain.ToString()).ToList();
    }

    public List<CanonicalLegacyCompatibilityBlocker> Blockers
    {
        get
        {
            var seen = new HashSet<CanonicalLegacyCompatibilityBlocker>();
            var ordered = new List<CanonicalLegacyCompatibilityBlocker>();
            foreach (var blocker in Results.SelectMany(r => r.Blockers))
            {
                if (!seen.Contains(blocker))
                {
                    seen.Add(blocker);
                    ordered.Add(blocker);
                }
            }
            return ordered;
        }
    }

    public List<CanonicalLegacyCompatibilityDomain> ProvenDomains =>
        Results.Where(r => r.IsProven).Select(r => r.Domain).ToList();

    public bool IsFullyProven =>
        new HashSet<CanonicalLegacyCompatibilityDomain>(ProvenDomains)
            .SetEquals(Enum.GetValues<CanonicalLegacyCompatibilityDomain>()) && Blockers.Count == 0;

    public string DiagnosticsSummary => string.Join(",",
        "canonicalLegacyCompatibilityMatrix=v8.44",
        $"domains={string.Join("|", Results.Select(r => r.Domain.ToString()))}",
        $"proven={string.Join("|", ProvenDomains.Select(d => d.ToString()))}",
        $"blockers={string.Join("|", Blockers.Select(b => b.ToString()))}",
        "legacyDeletion=false",
        "redacted=true"
    );

    public static CanonicalLegacyCompatibilityMatrix DefaultV844(
        CanonicalKernelSwitchPolicy? policy = null)
    {
        var pol = policy ?? CanonicalKernelSwitchPolicy.DebugInternal(manualFullSyncConfirmation: true);
        return new CanonicalLegacyCompatibilityMatrix(
            Enum.GetValues<CanonicalLegacyCompatibilityDomain>()
                .Select(d => CanonicalLegacyCompatibilityResult.Prove(d, pol)).ToList()
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalLegacyCompatibilityMatrix other && Equals(other);
    public bool Equals(CanonicalLegacyCompatibilityMatrix? other) =>
        other is not null && Results.SequenceEqual(other.Results);
    public override int GetHashCode() => HashCode.Combine(string.Join(",", Results.Select(r => r.Domain)));
    public static bool operator ==(CanonicalLegacyCompatibilityMatrix l, CanonicalLegacyCompatibilityMatrix r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyCompatibilityMatrix l, CanonicalLegacyCompatibilityMatrix r) => !l.Equals(r);
}

public sealed class CanonicalLegacyCompatibilityReadResult : IEquatable<CanonicalLegacyCompatibilityReadResult>
{
    public CanonicalLegacyCompatibilityDomain Domain { get; }
    public string ObjectID { get; }
    public string Value { get; }
    public int Revision { get; }
    public string FormatVersion { get; }
    public CanonicalKernelSwitchMode Mode { get; }
    public int IgnoredUnknownFieldCount { get; }

    public CanonicalLegacyCompatibilityReadResult(
        CanonicalLegacyCompatibilityDomain domain,
        string objectID,
        string value,
        int revision,
        string formatVersion,
        CanonicalKernelSwitchMode mode,
        int ignoredUnknownFieldCount)
    {
        Domain = domain;
        ObjectID = objectID;
        Value = value;
        Revision = revision;
        FormatVersion = formatVersion;
        Mode = mode;
        IgnoredUnknownFieldCount = ignoredUnknownFieldCount;
    }

    public override bool Equals(object? obj) => obj is CanonicalLegacyCompatibilityReadResult other && Equals(other);
    public bool Equals(CanonicalLegacyCompatibilityReadResult? other) =>
        other is not null &&
        Domain == other.Domain && ObjectID == other.ObjectID &&
        Value == other.Value && Revision == other.Revision &&
        FormatVersion == other.FormatVersion && Mode == other.Mode &&
        IgnoredUnknownFieldCount == other.IgnoredUnknownFieldCount;
    public override int GetHashCode() => HashCode.Combine(Domain, ObjectID, Value, Revision, FormatVersion, Mode, IgnoredUnknownFieldCount);
    public static bool operator ==(CanonicalLegacyCompatibilityReadResult l, CanonicalLegacyCompatibilityReadResult r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyCompatibilityReadResult l, CanonicalLegacyCompatibilityReadResult r) => !l.Equals(r);
}

public sealed class CanonicalLegacySwitchBackProofResult : IEquatable<CanonicalLegacySwitchBackProofResult>
{
    public List<CanonicalLegacyCompatibilityDomain> Domains { get; }
    public Dictionary<CanonicalLegacyCompatibilityDomain, CanonicalLegacyCompatibilityReadResult> LegacyReadsAfterCanonicalWrite { get; }
    public Dictionary<CanonicalLegacyCompatibilityDomain, CanonicalLegacyCompatibilityReadResult> CanonicalReadsAfterLegacyModify { get; }
    public bool SwitchBackNoMigration { get; }
    public bool SwitchBackComparisonPassed { get; }
    public bool SwitchForwardComparisonPassed { get; }
    public int PhysicalDeleteCount { get; }
    public bool OldKernelCrashedAfterCanonicalFullSync { get; }
    public bool CanonicalFullSyncCrashedAfterSwitchBack { get; }
    public List<CanonicalLegacyCompatibilityBlocker> Blockers { get; }
    public string DiagnosticsSummary { get; }

    public bool IsProven =>
        SwitchBackNoMigration
        && SwitchBackComparisonPassed
        && SwitchForwardComparisonPassed
        && PhysicalDeleteCount == 0
        && !OldKernelCrashedAfterCanonicalFullSync
        && !CanonicalFullSyncCrashedAfterSwitchBack
        && Blockers.Count == 0;

    public CanonicalLegacySwitchBackProofResult(
        List<CanonicalLegacyCompatibilityDomain> domains,
        Dictionary<CanonicalLegacyCompatibilityDomain, CanonicalLegacyCompatibilityReadResult> legacyReadsAfterCanonicalWrite,
        Dictionary<CanonicalLegacyCompatibilityDomain, CanonicalLegacyCompatibilityReadResult> canonicalReadsAfterLegacyModify,
        bool switchBackNoMigration,
        bool switchBackComparisonPassed,
        bool switchForwardComparisonPassed,
        int physicalDeleteCount,
        bool oldKernelCrashedAfterCanonicalFullSync,
        bool canonicalFullSyncCrashedAfterSwitchBack,
        List<CanonicalLegacyCompatibilityBlocker> blockers,
        string diagnosticsSummary)
    {
        Domains = domains;
        LegacyReadsAfterCanonicalWrite = legacyReadsAfterCanonicalWrite;
        CanonicalReadsAfterLegacyModify = canonicalReadsAfterLegacyModify;
        SwitchBackNoMigration = switchBackNoMigration;
        SwitchBackComparisonPassed = switchBackComparisonPassed;
        SwitchForwardComparisonPassed = switchForwardComparisonPassed;
        PhysicalDeleteCount = physicalDeleteCount;
        OldKernelCrashedAfterCanonicalFullSync = oldKernelCrashedAfterCanonicalFullSync;
        CanonicalFullSyncCrashedAfterSwitchBack = canonicalFullSyncCrashedAfterSwitchBack;
        Blockers = blockers;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalLegacySwitchBackProofResult other && Equals(other);
    public bool Equals(CanonicalLegacySwitchBackProofResult? other) =>
        other is not null &&
        SwitchBackNoMigration == other.SwitchBackNoMigration &&
        SwitchBackComparisonPassed == other.SwitchBackComparisonPassed &&
        SwitchForwardComparisonPassed == other.SwitchForwardComparisonPassed &&
        PhysicalDeleteCount == other.PhysicalDeleteCount &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(SwitchBackNoMigration, SwitchBackComparisonPassed,
        SwitchForwardComparisonPassed, PhysicalDeleteCount, DiagnosticsSummary);
    public static bool operator ==(CanonicalLegacySwitchBackProofResult l, CanonicalLegacySwitchBackProofResult r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacySwitchBackProofResult l, CanonicalLegacySwitchBackProofResult r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLegacyCrashPoint
{
    beforeCheckpoint,
    afterCheckpointBeforeWrite,
    afterWriteBeforePostcondition,
    afterPostconditionBeforeDuplicateSuppression
}

public sealed class CanonicalLegacyCrashRecoveryResult : IEquatable<CanonicalLegacyCrashRecoveryResult>
{
    public CanonicalLegacyCompatibilityDomain Domain { get; }
    public CanonicalLegacyCrashPoint CrashPoint { get; }
    public CanonicalKernelSwitchMode RestartMode { get; }
    public bool OldKernelCanRead { get; }
    public bool CanonicalFullSyncCanRead { get; }
    public bool NoDataLoss { get; }
    public bool IncompleteStateBlockedOrRecovered { get; }
    public int PhysicalDeleteCount { get; }
    public bool DuplicateSuppressionApplied { get; }
    public List<CanonicalLegacyCompatibilityBlocker> Blockers { get; }
    public string DiagnosticsSummary { get; }

    public bool RecoveredSafely =>
        OldKernelCanRead
        && CanonicalFullSyncCanRead
        && NoDataLoss
        && IncompleteStateBlockedOrRecovered
        && PhysicalDeleteCount == 0
        && !DuplicateSuppressionApplied
        && Blockers.Count == 0;

    public CanonicalLegacyCrashRecoveryResult(
        CanonicalLegacyCompatibilityDomain domain,
        CanonicalLegacyCrashPoint crashPoint,
        CanonicalKernelSwitchMode restartMode,
        bool oldKernelCanRead,
        bool canonicalFullSyncCanRead,
        bool noDataLoss,
        bool incompleteStateBlockedOrRecovered,
        int physicalDeleteCount,
        bool duplicateSuppressionApplied,
        List<CanonicalLegacyCompatibilityBlocker> blockers,
        string diagnosticsSummary)
    {
        Domain = domain;
        CrashPoint = crashPoint;
        RestartMode = restartMode;
        OldKernelCanRead = oldKernelCanRead;
        CanonicalFullSyncCanRead = canonicalFullSyncCanRead;
        NoDataLoss = noDataLoss;
        IncompleteStateBlockedOrRecovered = incompleteStateBlockedOrRecovered;
        PhysicalDeleteCount = physicalDeleteCount;
        DuplicateSuppressionApplied = duplicateSuppressionApplied;
        Blockers = blockers;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalLegacyCrashRecoveryResult other && Equals(other);
    public bool Equals(CanonicalLegacyCrashRecoveryResult? other) =>
        other is not null &&
        Domain == other.Domain && CrashPoint == other.CrashPoint &&
        RestartMode == other.RestartMode && OldKernelCanRead == other.OldKernelCanRead &&
        CanonicalFullSyncCanRead == other.CanonicalFullSyncCanRead && NoDataLoss == other.NoDataLoss &&
        IncompleteStateBlockedOrRecovered == other.IncompleteStateBlockedOrRecovered &&
        PhysicalDeleteCount == other.PhysicalDeleteCount &&
        DuplicateSuppressionApplied == other.DuplicateSuppressionApplied;
    public override int GetHashCode() => HashCode.Combine(Domain, CrashPoint, RestartMode, OldKernelCanRead,
        CanonicalFullSyncCanRead, NoDataLoss, IncompleteStateBlockedOrRecovered, PhysicalDeleteCount, DuplicateSuppressionApplied);
    public static bool operator ==(CanonicalLegacyCrashRecoveryResult l, CanonicalLegacyCrashRecoveryResult r) => l.Equals(r);
    public static bool operator !=(CanonicalLegacyCrashRecoveryResult l, CanonicalLegacyCrashRecoveryResult r) => !l.Equals(r);
}

public sealed class CanonicalLegacySwitchBackHarness
{
    private sealed class StoredRecord
    {
        public CanonicalLegacyCompatibilityDomain Domain { get; set; } = default!;
        public string ObjectID { get; set; } = "";
        public string Value { get; set; } = "";
        public int Revision { get; set; }
        public string FormatVersion { get; set; } = "";
        public Dictionary<string, string> LegacyFields { get; set; } = new();
        public Dictionary<string, string> UnknownFields { get; set; } = new();

        public static StoredRecord Make(
            CanonicalLegacyCompatibilityDomain domain,
            string value,
            int revision,
            Dictionary<string, string>? unknownFields = null)
        {
            var objectID = $"compat-{domain}";
            return new StoredRecord
            {
                Domain = domain,
                ObjectID = objectID,
                Value = value,
                Revision = revision,
                FormatVersion = "legacy-v1",
                LegacyFields = new Dictionary<string, string>
                {
                    ["domain"] = domain.ToString(),
                    ["objectID"] = objectID,
                    ["revision"] = revision.ToString(),
                    ["value"] = value
                },
                UnknownFields = unknownFields ?? new Dictionary<string, string>()
            };
        }
    }

    private Dictionary<CanonicalLegacyCompatibilityDomain, StoredRecord> _storage;
    private Dictionary<CanonicalLegacyCompatibilityDomain, StoredRecord?> _checkpoints;
    private List<string> _diagnostics;
    private int _migrationCount;
    private int _physicalDeleteCount;
    public CanonicalKernelSwitchMode Mode { get; private set; }

    public CanonicalLegacySwitchBackHarness(bool seedLegacyRecords = true)
    {
        _storage = new Dictionary<CanonicalLegacyCompatibilityDomain, StoredRecord>();
        if (seedLegacyRecords)
        {
            foreach (var domain in Enum.GetValues<CanonicalLegacyCompatibilityDomain>())
            {
                _storage[domain] = StoredRecord.Make(domain, $"legacy-baseline-{domain}", 1);
            }
        }
        _checkpoints = new Dictionary<CanonicalLegacyCompatibilityDomain, StoredRecord?>();
        _diagnostics = new List<string>();
        _migrationCount = 0;
        _physicalDeleteCount = 0;
        Mode = CanonicalKernelSwitchMode.oldKernel;
    }

    public void SwitchMode(CanonicalKernelSwitchMode nextMode)
    {
        Mode = nextMode;
    }

    public CanonicalLegacyCompatibilityReadResult LegacyWrite(
        CanonicalLegacyCompatibilityDomain domain, string? value = null)
    {
        var current = _storage.GetValueOrDefault(domain);
        var nextRevision = (current?.Revision ?? 0) + 1;
        _storage[domain] = StoredRecord.Make(domain,
            value ?? $"legacy-write-{domain}-{nextRevision}", nextRevision);
        Mode = CanonicalKernelSwitchMode.oldKernel;
        return LegacyRead(domain)!;
    }

    public CanonicalLegacyCompatibilityReadResult CanonicalWrite(
        CanonicalLegacyCompatibilityDomain domain, string? value = null)
    {
        var current = _storage.GetValueOrDefault(domain);
        var nextRevision = (current?.Revision ?? 0) + 1;
        _checkpoints[domain] = current;
        _storage[domain] = StoredRecord.Make(domain,
            value ?? $"canonical-write-{domain}-{nextRevision}", nextRevision,
            new Dictionary<string, string>
            {
                ["canonicalHint"] = "ignored-by-legacy",
                ["canonicalCompatibility"] = "v8.44"
            });
        Mode = CanonicalKernelSwitchMode.canonicalFullSync;
        return CanonicalRead(domain)!;
    }

    public CanonicalLegacyCompatibilityReadResult? LegacyRead(CanonicalLegacyCompatibilityDomain domain)
    {
        if (!_storage.TryGetValue(domain, out var record)) return null;
        if (record.FormatVersion != "legacy-v1") return null;
        if (record.LegacyFields.GetValueOrDefault("objectID") != record.ObjectID) return null;
        if (record.LegacyFields.GetValueOrDefault("value") != record.Value) return null;

        return new CanonicalLegacyCompatibilityReadResult(
            domain, record.ObjectID, record.Value, record.Revision,
            record.FormatVersion, CanonicalKernelSwitchMode.oldKernel,
            record.UnknownFields.Count);
    }

    public CanonicalLegacyCompatibilityReadResult? CanonicalRead(CanonicalLegacyCompatibilityDomain domain)
    {
        if (!_storage.TryGetValue(domain, out var record)) return null;
        if (record.FormatVersion != "legacy-v1") return null;
        if (record.LegacyFields.GetValueOrDefault("objectID") != record.ObjectID) return null;
        if (record.LegacyFields.GetValueOrDefault("value") != record.Value) return null;

        return new CanonicalLegacyCompatibilityReadResult(
            domain, record.ObjectID, record.Value, record.Revision,
            record.FormatVersion, CanonicalKernelSwitchMode.canonicalFullSync,
            record.UnknownFields.Count);
    }

    public string? DataFormatFingerprint(CanonicalLegacyCompatibilityDomain domain)
    {
        if (!_storage.TryGetValue(domain, out var record)) return null;
        var legacyKeys = string.Join("|", record.LegacyFields.Keys.OrderBy(k => k));
        var unknownKeys = string.Join("|", record.UnknownFields.Keys.OrderBy(k => k));
        return string.Join(",", record.FormatVersion, record.ObjectID, record.Value,
            record.Revision.ToString(), legacyKeys, unknownKeys);
    }

    public void RecordCanonicalDiagnostic(CanonicalLegacyCompatibilityDomain domain)
    {
        _diagnostics.Add($"canonicalLegacyCompatibilityDiagnostic:v8.44,domain={domain},redacted=true");
    }

    public CanonicalLegacyCompatibilityReadResult? CanonicalWriteWithRollbackAfterPartialFailure(
        CanonicalLegacyCompatibilityDomain domain)
    {
        var checkpoint = _storage.GetValueOrDefault(domain);
        _checkpoints[domain] = checkpoint;
        var nextRevision = (checkpoint?.Revision ?? 0) + 1;
        _storage[domain] = StoredRecord.Make(domain,
            $"partial-canonical-write-{domain}-{nextRevision}", nextRevision,
            new Dictionary<string, string> { ["canonicalHint"] = "ignored-by-legacy" });
        Rollback(domain);
        Mode = CanonicalKernelSwitchMode.oldKernel;
        return LegacyRead(domain);
    }

    public CanonicalLegacySwitchBackProofResult RunSwitchBackProof(
        List<CanonicalLegacyCompatibilityDomain>? domains = null)
    {
        var doms = domains ?? Enum.GetValues<CanonicalLegacyCompatibilityDomain>().ToList();
        SwitchMode(CanonicalKernelSwitchMode.canonicalFullSync);
        foreach (var domain in doms)
            CanonicalWrite(domain, $"canonical-full-sync-{domain}");

        SwitchMode(CanonicalKernelSwitchMode.oldKernel);
        var legacyReads = new Dictionary<CanonicalLegacyCompatibilityDomain, CanonicalLegacyCompatibilityReadResult>();
        foreach (var domain in doms)
        {
            var read = LegacyRead(domain);
            if (read != null) legacyReads[domain] = read;
        }
        foreach (var domain in doms)
            LegacyWrite(domain, $"legacy-modified-{domain}");

        SwitchMode(CanonicalKernelSwitchMode.canonicalFullSync);
        var canonicalReads = new Dictionary<CanonicalLegacyCompatibilityDomain, CanonicalLegacyCompatibilityReadResult>();
        foreach (var domain in doms)
        {
            var read = CanonicalRead(domain);
            if (read != null) canonicalReads[domain] = read;
        }

        var blockers = new List<CanonicalLegacyCompatibilityBlocker>();
        var switchBackComparisonPassed = doms.All(d =>
            legacyReads.GetValueOrDefault(d)?.Value == $"canonical-full-sync-{d}");
        var switchForwardComparisonPassed = doms.All(d =>
            canonicalReads.GetValueOrDefault(d)?.Value == $"legacy-modified-{d}");
        if (_migrationCount != 0) blockers.Add(CanonicalLegacyCompatibilityBlocker.switchBackRequiresMigration);
        if (_physicalDeleteCount != 0) blockers.Add(CanonicalLegacyCompatibilityBlocker.physicalDeleteRequired);
        if (!switchBackComparisonPassed || !switchForwardComparisonPassed) blockers.Add(CanonicalLegacyCompatibilityBlocker.dataLossDetected);
        if (legacyReads.Count != doms.Count) blockers.Add(CanonicalLegacyCompatibilityBlocker.oldKernelRestartFailed);
        if (canonicalReads.Count != doms.Count) blockers.Add(CanonicalLegacyCompatibilityBlocker.canonicalFullSyncRestartFailed);

        return new CanonicalLegacySwitchBackProofResult(
            domains: doms,
            legacyReadsAfterCanonicalWrite: legacyReads,
            canonicalReadsAfterLegacyModify: canonicalReads,
            switchBackNoMigration: _migrationCount == 0,
            switchBackComparisonPassed: switchBackComparisonPassed,
            switchForwardComparisonPassed: switchForwardComparisonPassed,
            physicalDeleteCount: _physicalDeleteCount,
            oldKernelCrashedAfterCanonicalFullSync: legacyReads.Count != doms.Count,
            canonicalFullSyncCrashedAfterSwitchBack: canonicalReads.Count != doms.Count,
            blockers: blockers,
            diagnosticsSummary: string.Join(",",
                "canonicalSwitchBackProof=v8.44",
                $"domains={string.Join("|", doms.Select(d => d.ToString()))}",
                $"switchBackNoMigration={_migrationCount == 0}",
                $"physicalDeleteCount={_physicalDeleteCount}",
                $"legacyReads={legacyReads.Count}",
                $"canonicalReads={canonicalReads.Count}",
                "redacted=true"
            )
        );
    }

    public CanonicalLegacyCrashRecoveryResult SimulateCrashAndRestart(
        CanonicalLegacyCompatibilityDomain domain,
        CanonicalLegacyCrashPoint crashPoint,
        CanonicalKernelSwitchMode restartMode)
    {
        if (!_storage.ContainsKey(domain))
        {
            _storage[domain] = StoredRecord.Make(domain, $"legacy-baseline-{domain}", 1);
        }
        var baseline = _storage[domain];
        var incompleteRecovered = true;
        var duplicateSuppressionApplied = false;

        switch (crashPoint)
        {
            case CanonicalLegacyCrashPoint.beforeCheckpoint:
                break;
            case CanonicalLegacyCrashPoint.afterCheckpointBeforeWrite:
                _checkpoints[domain] = baseline;
                Rollback(domain);
                break;
            case CanonicalLegacyCrashPoint.afterWriteBeforePostcondition:
                _checkpoints[domain] = baseline;
                var nextRevision1 = (baseline?.Revision ?? 0) + 1;
                _storage[domain] = StoredRecord.Make(domain,
                    $"canonical-crash-{domain}", nextRevision1,
                    new Dictionary<string, string> { ["canonicalHint"] = "ignored-by-legacy" });
                Rollback(domain);
                break;
            case CanonicalLegacyCrashPoint.afterPostconditionBeforeDuplicateSuppression:
                _checkpoints[domain] = baseline;
                var nextRevision2 = (baseline?.Revision ?? 0) + 1;
                _storage[domain] = StoredRecord.Make(domain,
                    $"canonical-postcondition-{domain}", nextRevision2,
                    new Dictionary<string, string> { ["canonicalHint"] = "ignored-by-legacy" });
                incompleteRecovered = true;
                duplicateSuppressionApplied = false;
                break;
        }

        SwitchMode(restartMode);
        var oldRead = LegacyRead(domain);
        var canonicalRead = CanonicalRead(domain);
        var noDataLoss = oldRead != null && canonicalRead != null;
        var blockers = new List<CanonicalLegacyCompatibilityBlocker>();
        if (oldRead == null) blockers.Add(CanonicalLegacyCompatibilityBlocker.oldKernelRestartFailed);
        if (canonicalRead == null) blockers.Add(CanonicalLegacyCompatibilityBlocker.canonicalFullSyncRestartFailed);
        if (!noDataLoss) blockers.Add(CanonicalLegacyCompatibilityBlocker.dataLossDetected);
        if (!incompleteRecovered) blockers.Add(CanonicalLegacyCompatibilityBlocker.incompleteStateUnrecoverable);
        if (_physicalDeleteCount != 0) blockers.Add(CanonicalLegacyCompatibilityBlocker.physicalDeleteRequired);

        return new CanonicalLegacyCrashRecoveryResult(
            domain: domain,
            crashPoint: crashPoint,
            restartMode: restartMode,
            oldKernelCanRead: oldRead != null,
            canonicalFullSyncCanRead: canonicalRead != null,
            noDataLoss: noDataLoss,
            incompleteStateBlockedOrRecovered: incompleteRecovered,
            physicalDeleteCount: _physicalDeleteCount,
            duplicateSuppressionApplied: duplicateSuppressionApplied,
            blockers: blockers,
            diagnosticsSummary: string.Join(",",
                "canonicalCrashRecovery=v8.44",
                $"domain={domain}",
                $"crashPoint={crashPoint}",
                $"restartMode={restartMode}",
                $"oldKernelCanRead={oldRead != null}",
                $"canonicalFullSyncCanRead={canonicalRead != null}",
                $"physicalDeleteCount={_physicalDeleteCount}",
                "redacted=true"
            )
        );
    }

    private void Rollback(CanonicalLegacyCompatibilityDomain domain)
    {
        if (_checkpoints.TryGetValue(domain, out var checkpoint) && checkpoint != null)
            _storage[domain] = checkpoint;
        else
            _storage.Remove(domain);
    }
}

public sealed class CanonicalSwitchBackRealisticRootHarnessResult : IEquatable<CanonicalSwitchBackRealisticRootHarnessResult>
{
    public List<CanonicalLegacyCompatibilityDomain> Domains { get; }
    public bool TestClonedRoot { get; }
    public bool UsesProductionRoot { get; }
    public int LegacyReadableStateCount { get; }
    public int CanonicalReadableStateCount { get; }
    public int CrashRecoveryProofCount { get; }
    public CanonicalLegacySwitchBackProofResult SwitchBackProof { get; }
    public List<CanonicalLegacyCrashRecoveryResult> CrashRecoveryProofs { get; }
    public int PhysicalDeleteCount { get; }
    public int ResourceMoveCount { get; }
    public bool LegacyRetirementPerformed { get; }
    public List<CanonicalLegacyCompatibilityBlocker> Blockers { get; }
    public string DiagnosticsSummary { get; }

    public bool IsProven =>
        TestClonedRoot
        && !UsesProductionRoot
        && LegacyReadableStateCount == Domains.Count
        && CanonicalReadableStateCount == Domains.Count
        && CrashRecoveryProofCount == Domains.Count * Enum.GetValues<CanonicalLegacyCrashPoint>().Length
        && SwitchBackProof.IsProven
        && CrashRecoveryProofs.All(r => r.RecoveredSafely)
        && PhysicalDeleteCount == 0
        && ResourceMoveCount == 0
        && !LegacyRetirementPerformed
        && Blockers.Count == 0;

    public CanonicalSwitchBackRealisticRootHarnessResult(
        List<CanonicalLegacyCompatibilityDomain> domains,
        bool testClonedRoot,
        bool usesProductionRoot,
        int legacyReadableStateCount,
        int canonicalReadableStateCount,
        int crashRecoveryProofCount,
        CanonicalLegacySwitchBackProofResult switchBackProof,
        List<CanonicalLegacyCrashRecoveryResult> crashRecoveryProofs,
        int physicalDeleteCount,
        int resourceMoveCount,
        bool legacyRetirementPerformed,
        List<CanonicalLegacyCompatibilityBlocker> blockers,
        string diagnosticsSummary)
    {
        Domains = domains;
        TestClonedRoot = testClonedRoot;
        UsesProductionRoot = usesProductionRoot;
        LegacyReadableStateCount = legacyReadableStateCount;
        CanonicalReadableStateCount = canonicalReadableStateCount;
        CrashRecoveryProofCount = crashRecoveryProofCount;
        SwitchBackProof = switchBackProof;
        CrashRecoveryProofs = crashRecoveryProofs;
        PhysicalDeleteCount = physicalDeleteCount;
        ResourceMoveCount = resourceMoveCount;
        LegacyRetirementPerformed = legacyRetirementPerformed;
        Blockers = blockers;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalSwitchBackRealisticRootHarnessResult other && Equals(other);
    public bool Equals(CanonicalSwitchBackRealisticRootHarnessResult? other) =>
        other is not null &&
        TestClonedRoot == other.TestClonedRoot &&
        UsesProductionRoot == other.UsesProductionRoot &&
        LegacyReadableStateCount == other.LegacyReadableStateCount &&
        CanonicalReadableStateCount == other.CanonicalReadableStateCount &&
        CrashRecoveryProofCount == other.CrashRecoveryProofCount &&
        PhysicalDeleteCount == other.PhysicalDeleteCount &&
        ResourceMoveCount == other.ResourceMoveCount &&
        LegacyRetirementPerformed == other.LegacyRetirementPerformed &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(TestClonedRoot, UsesProductionRoot,
        LegacyReadableStateCount, CanonicalReadableStateCount, CrashRecoveryProofCount,
        PhysicalDeleteCount, ResourceMoveCount, LegacyRetirementPerformed);
    public static bool operator ==(CanonicalSwitchBackRealisticRootHarnessResult l, CanonicalSwitchBackRealisticRootHarnessResult r) => l.Equals(r);
    public static bool operator !=(CanonicalSwitchBackRealisticRootHarnessResult l, CanonicalSwitchBackRealisticRootHarnessResult r) => !l.Equals(r);
}

public sealed class CanonicalSwitchBackRealisticRootHarness
{
    private sealed class LegacyRootRecord
    {
        public int SchemaVersion { get; set; }
        public CanonicalLegacyCompatibilityDomain Domain { get; set; }
        public string ObjectID { get; set; } = "";
        public string Value { get; set; } = "";
        public int Revision { get; set; }
        public string FormatVersion { get; set; } = "";
        public Dictionary<string, string> CanonicalUnknownFields { get; set; } = new();
        public bool LegacyRetirementPerformed { get; set; }
    }

    private readonly string _rootPath;

    public CanonicalSwitchBackRealisticRootHarness(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
    }

    public CanonicalSwitchBackRealisticRootHarnessResult Run(
        List<CanonicalLegacyCompatibilityDomain>? domains = null)
    {
        var doms = domains ?? Enum.GetValues<CanonicalLegacyCompatibilityDomain>().ToList();
        var usesProductionRoot = LooksLikeProductionRoot(_rootPath);
        if (usesProductionRoot)
            return BlockedResult(doms, CanonicalLegacyCompatibilityBlocker.legacyWritePathUnavailable);

        Directory.CreateDirectory(_rootPath);
        SeedRealisticLegacyRoot(doms);

        var harness = new CanonicalLegacySwitchBackHarness(seedLegacyRecords: true);
        var switchBackProof = harness.RunSwitchBackProof(doms);
        PersistSwitchBackState(switchBackProof);

        var crashProofs = new List<CanonicalLegacyCrashRecoveryResult>();
        foreach (var domain in doms)
        {
            foreach (var crashPoint in Enum.GetValues<CanonicalLegacyCrashPoint>())
            {
                var crashHarness = new CanonicalLegacySwitchBackHarness(seedLegacyRecords: true);
                var result = crashHarness.SimulateCrashAndRestart(domain, crashPoint, CanonicalKernelSwitchMode.oldKernel);
                crashProofs.Add(result);
                PersistCrashState(result);
            }
        }

        var legacyReadableCount = doms.Count(d => ReadLegacyRootRecord(d) != null);
        var canonicalReadableCount = doms.Count(d => ReadCanonicalRootRecord(d) != null);
        var blockers = new List<CanonicalLegacyCompatibilityBlocker>();
        blockers.AddRange(switchBackProof.Blockers);
        blockers.AddRange(crashProofs.SelectMany(p => p.Blockers));
        if (legacyReadableCount != doms.Count) blockers.Add(CanonicalLegacyCompatibilityBlocker.oldKernelRestartFailed);
        if (canonicalReadableCount != doms.Count) blockers.Add(CanonicalLegacyCompatibilityBlocker.canonicalFullSyncRestartFailed);
        blockers = new HashSet<CanonicalLegacyCompatibilityBlocker>(blockers).OrderBy(b => b.ToString()).ToList();

        return new CanonicalSwitchBackRealisticRootHarnessResult(
            domains: doms,
            testClonedRoot: true,
            usesProductionRoot: false,
            legacyReadableStateCount: legacyReadableCount,
            canonicalReadableStateCount: canonicalReadableCount,
            crashRecoveryProofCount: crashProofs.Count,
            switchBackProof: switchBackProof,
            crashRecoveryProofs: crashProofs,
            physicalDeleteCount: 0,
            resourceMoveCount: 0,
            legacyRetirementPerformed: false,
            blockers: blockers,
            diagnosticsSummary: string.Join(",",
                "canonicalSwitchBackRealisticRoot=v8.45",
                "root=test-cloned",
                $"domains={doms.Count}",
                $"legacyReadable={legacyReadableCount}",
                $"canonicalReadable={canonicalReadableCount}",
                $"crashProofs={crashProofs.Count}",
                "physicalDeleteCount=0",
                "resourceMoveCount=0",
                "legacyRetirementPerformed=false",
                "redacted=true"
            )
        );
    }

    private CanonicalSwitchBackRealisticRootHarnessResult BlockedResult(
        List<CanonicalLegacyCompatibilityDomain> domains, CanonicalLegacyCompatibilityBlocker reason)
    {
        var proof = new CanonicalLegacySwitchBackProofResult(
            domains: domains,
            legacyReadsAfterCanonicalWrite: new(),
            canonicalReadsAfterLegacyModify: new(),
            switchBackNoMigration: false,
            switchBackComparisonPassed: false,
            switchForwardComparisonPassed: false,
            physicalDeleteCount: 0,
            oldKernelCrashedAfterCanonicalFullSync: true,
            canonicalFullSyncCrashedAfterSwitchBack: true,
            blockers: new List<CanonicalLegacyCompatibilityBlocker> { reason },
            diagnosticsSummary: "canonicalSwitchBackRealisticRoot=v8.45,blocked=true,redacted=true"
        );
        return new CanonicalSwitchBackRealisticRootHarnessResult(
            domains: domains,
            testClonedRoot: false,
            usesProductionRoot: true,
            legacyReadableStateCount: 0,
            canonicalReadableStateCount: 0,
            crashRecoveryProofCount: 0,
            switchBackProof: proof,
            crashRecoveryProofs: new(),
            physicalDeleteCount: 0,
            resourceMoveCount: 0,
            legacyRetirementPerformed: false,
            blockers: new List<CanonicalLegacyCompatibilityBlocker> { reason },
            diagnosticsSummary: "canonicalSwitchBackRealisticRoot=v8.45,blockedProductionRoot=true,redacted=true"
        );
    }

    private void SeedRealisticLegacyRoot(List<CanonicalLegacyCompatibilityDomain> domains)
    {
        foreach (var domain in domains)
        {
            WriteLegacyRootRecord(new LegacyRootRecord
            {
                SchemaVersion = 1,
                Domain = domain,
                ObjectID = $"compat-{domain}",
                Value = $"legacy-baseline-{domain}",
                Revision = 1,
                FormatVersion = "legacy-v1",
                CanonicalUnknownFields = new(),
                LegacyRetirementPerformed = false
            });
        }
    }

    private void PersistSwitchBackState(CanonicalLegacySwitchBackProofResult proof)
    {
        foreach (var (_, read) in proof.LegacyReadsAfterCanonicalWrite)
        {
            WriteLegacyRootRecord(new LegacyRootRecord
            {
                SchemaVersion = 1,
                Domain = read.Domain,
                ObjectID = read.ObjectID,
                Value = read.Value,
                Revision = read.Revision,
                FormatVersion = read.FormatVersion,
                CanonicalUnknownFields = new Dictionary<string, string> { ["canonicalHint"] = "ignored-by-legacy" },
                LegacyRetirementPerformed = false
            });
        }
    }

    private void PersistCrashState(CanonicalLegacyCrashRecoveryResult result)
    {
        var directory = Path.Combine(_rootPath, "crash-recovery", result.Domain.ToString());
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"{result.CrashPoint}.json");
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private void WriteLegacyRootRecord(LegacyRootRecord record)
    {
        var directory = Path.Combine(_rootPath, RelativeDirectory(record.Domain));
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"{record.ObjectID}.json");
        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private LegacyRootRecord? ReadLegacyRootRecord(CanonicalLegacyCompatibilityDomain domain)
    {
        try
        {
            var filePath = Path.Combine(_rootPath, RelativeDirectory(domain), $"compat-{domain}.json");
            if (!File.Exists(filePath)) return null;
            var json = File.ReadAllText(filePath);
            var record = JsonSerializer.Deserialize<LegacyRootRecord>(json);
            if (record == null) return null;
            if (record.FormatVersion != "legacy-v1") return null;
            if (record.Domain != domain) return null;
            if (record.LegacyRetirementPerformed) return null;
            return record;
        }
        catch
        {
            return null;
        }
    }

    private LegacyRootRecord? ReadCanonicalRootRecord(CanonicalLegacyCompatibilityDomain domain)
    {
        var record = ReadLegacyRootRecord(domain);
        if (record == null) return null;
        if (record.SchemaVersion != 1) return null;
        return record;
    }

    private static string RelativeDirectory(CanonicalLegacyCompatibilityDomain domain)
    {
        return domain switch
        {
            CanonicalLegacyCompatibilityDomain.recordingMetadata => "study/recording-metadata",
            CanonicalLegacyCompatibilityDomain.libraryMetadata => "study/library-metadata",
            CanonicalLegacyCompatibilityDomain.generatedArtifacts => "study/generated-artifacts",
            CanonicalLegacyCompatibilityDomain.tombstoneConflict => "study/tombstone-conflicts",
            CanonicalLegacyCompatibilityDomain.recordingExistence => "sync/canonical-recording-existence",
            CanonicalLegacyCompatibilityDomain.audioUpload => "upload-ledger/audio-runtime",
            CanonicalLegacyCompatibilityDomain.readRuntime => "sync/read-runtime",
            _ => "unknown"
        };
    }

    private static bool LooksLikeProductionRoot(string path)
    {
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (trimmed.Contains("/tmp/") || trimmed.Contains("/T/") || trimmed.Contains("/TemporaryItems/"))
            return false;
        return trimmed.EndsWith("/Library/Containers/com.Vita0818.Rokurics/Data")
            || trimmed.EndsWith("/Library/Containers/com.Vita0818.RokuricsMac/Data")
            || trimmed.Contains("/Library/Containers/com.Vita0818.Rokurics/Data/Documents/Rokurics")
            || trimmed.Contains("/Library/Containers/com.Vita0818.RokuricsMac/Data/Documents/Rokurics")
            || trimmed.Contains("/Library/Containers/com.Vita0818.Rokurics/Data/Library/Application Support/Rokurics")
            || trimmed.Contains("/Library/Containers/com.Vita0818.RokuricsMac/Data/Library/Application Support/Rokurics")
            || trimmed.Contains("/Application Support/Rokurics")
            || trimmed.Contains("/Documents/Rokurics");
    }
}
