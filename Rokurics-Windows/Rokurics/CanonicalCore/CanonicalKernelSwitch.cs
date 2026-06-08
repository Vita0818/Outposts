using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalKernelSwitchMode
{
    oldKernel,
    diagnosticsOnly,
    canonicalShadow,
    canonicalDecisionOnly,
    canonicalApplyNoAudio,
    canonicalFullSync,
    blocked
}

public static class CanonicalKernelSwitchModeExtensions
{
    public static string DisplayTitle(this CanonicalKernelSwitchMode mode)
    {
        return mode switch
        {
            CanonicalKernelSwitchMode.oldKernel => "旧内核",
            CanonicalKernelSwitchMode.diagnosticsOnly => "诊断",
            CanonicalKernelSwitchMode.canonicalShadow => "新内核影子",
            CanonicalKernelSwitchMode.canonicalDecisionOnly => "新内核决策",
            CanonicalKernelSwitchMode.canonicalApplyNoAudio => "新内核写入不含音频",
            CanonicalKernelSwitchMode.canonicalFullSync => "新内核完整同步",
            CanonicalKernelSwitchMode.blocked => "已阻断",
            _ => mode.ToString()
        };
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalKernelSwitchOwnerState
{
    oldKernel,
    shadow,
    canonicalNoWrite,
    canonicalReadWrite,
    blocked
}

public sealed class CanonicalKernelSwitchModeChoice : IEquatable<CanonicalKernelSwitchModeChoice>
{
    public string Id => RawValue;
    public string RawValue { get; }
    public string Title { get; }

    public CanonicalKernelSwitchModeChoice(CanonicalKernelSwitchMode mode)
    {
        RawValue = mode.ToString();
        Title = mode.DisplayTitle();
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelSwitchModeChoice other && Equals(other);
    public bool Equals(CanonicalKernelSwitchModeChoice? other) =>
        other is not null && RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();
    public static bool operator ==(CanonicalKernelSwitchModeChoice l, CanonicalKernelSwitchModeChoice r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelSwitchModeChoice l, CanonicalKernelSwitchModeChoice r) => !l.Equals(r);
}

public sealed class CanonicalKernelSwitchPolicy : IEquatable<CanonicalKernelSwitchPolicy>
{
    public bool DebugInternalBuild { get; set; }
    public bool OwnerApproved { get; set; }
    public bool ReleaseDefaultBuild { get; set; }
    public bool ManualFullSyncConfirmation { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public bool DiagnosticsRedacted { get; set; }
    public bool ShadowComparisonEnabled { get; set; }
    public bool LegacyReadPathAvailable { get; set; }
    public bool LegacyWritePathAvailable { get; set; }
    public bool CanonicalWritesLegacyReadable { get; set; }
    public bool NoDataFormatMigrationRequired { get; set; }
    public bool CanonicalOnlyRequiredFieldsHaveLegacyFallback { get; set; }
    public bool PhysicalMoveDeleteDisabled { get; set; }
    public bool SecretPathHashLeakRedactionEnabled { get; set; }
    public bool ShadowCompareAllowedDuringCanonicalOwner { get; set; }

    public CanonicalKernelSwitchPolicy(
        bool debugInternalBuild = false,
        bool ownerApproved = false,
        bool releaseDefaultBuild = true,
        bool manualFullSyncConfirmation = false,
        bool legacyFallbackAvailable = true,
        bool diagnosticsRedacted = true,
        bool shadowComparisonEnabled = true,
        bool legacyReadPathAvailable = true,
        bool legacyWritePathAvailable = true,
        bool canonicalWritesLegacyReadable = true,
        bool noDataFormatMigrationRequired = true,
        bool canonicalOnlyRequiredFieldsHaveLegacyFallback = true,
        bool physicalMoveDeleteDisabled = true,
        bool secretPathHashLeakRedactionEnabled = true,
        bool shadowCompareAllowedDuringCanonicalOwner = true)
    {
        DebugInternalBuild = debugInternalBuild;
        OwnerApproved = ownerApproved;
        ReleaseDefaultBuild = releaseDefaultBuild;
        ManualFullSyncConfirmation = manualFullSyncConfirmation;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        DiagnosticsRedacted = diagnosticsRedacted;
        ShadowComparisonEnabled = shadowComparisonEnabled;
        LegacyReadPathAvailable = legacyReadPathAvailable;
        LegacyWritePathAvailable = legacyWritePathAvailable;
        CanonicalWritesLegacyReadable = canonicalWritesLegacyReadable;
        NoDataFormatMigrationRequired = noDataFormatMigrationRequired;
        CanonicalOnlyRequiredFieldsHaveLegacyFallback = canonicalOnlyRequiredFieldsHaveLegacyFallback;
        PhysicalMoveDeleteDisabled = physicalMoveDeleteDisabled;
        SecretPathHashLeakRedactionEnabled = secretPathHashLeakRedactionEnabled;
        ShadowCompareAllowedDuringCanonicalOwner = shadowCompareAllowedDuringCanonicalOwner;
    }

    public static CanonicalKernelSwitchPolicy ReleaseDefault => new();

    public static CanonicalKernelSwitchPolicy DebugInternal(
        bool ownerApproved = true, bool manualFullSyncConfirmation = false)
    {
        return new CanonicalKernelSwitchPolicy(
            debugInternalBuild: true,
            ownerApproved: ownerApproved,
            releaseDefaultBuild: false,
            manualFullSyncConfirmation: manualFullSyncConfirmation
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelSwitchPolicy other && Equals(other);
    public bool Equals(CanonicalKernelSwitchPolicy? other) =>
        other is not null &&
        DebugInternalBuild == other.DebugInternalBuild &&
        OwnerApproved == other.OwnerApproved &&
        ReleaseDefaultBuild == other.ReleaseDefaultBuild &&
        ManualFullSyncConfirmation == other.ManualFullSyncConfirmation &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        DiagnosticsRedacted == other.DiagnosticsRedacted &&
        ShadowComparisonEnabled == other.ShadowComparisonEnabled &&
        LegacyReadPathAvailable == other.LegacyReadPathAvailable &&
        LegacyWritePathAvailable == other.LegacyWritePathAvailable &&
        CanonicalWritesLegacyReadable == other.CanonicalWritesLegacyReadable &&
        NoDataFormatMigrationRequired == other.NoDataFormatMigrationRequired &&
        CanonicalOnlyRequiredFieldsHaveLegacyFallback == other.CanonicalOnlyRequiredFieldsHaveLegacyFallback &&
        PhysicalMoveDeleteDisabled == other.PhysicalMoveDeleteDisabled &&
        SecretPathHashLeakRedactionEnabled == other.SecretPathHashLeakRedactionEnabled &&
        ShadowCompareAllowedDuringCanonicalOwner == other.ShadowCompareAllowedDuringCanonicalOwner;
    public override int GetHashCode() => HashCode.Combine(DebugInternalBuild, OwnerApproved, ReleaseDefaultBuild,
        ManualFullSyncConfirmation, LegacyFallbackAvailable, DiagnosticsRedacted, ShadowComparisonEnabled,
        LegacyReadPathAvailable, LegacyWritePathAvailable, CanonicalWritesLegacyReadable, NoDataFormatMigrationRequired,
        CanonicalOnlyRequiredFieldsHaveLegacyFallback, PhysicalMoveDeleteDisabled, SecretPathHashLeakRedactionEnabled,
        ShadowCompareAllowedDuringCanonicalOwner);
    public static bool operator ==(CanonicalKernelSwitchPolicy l, CanonicalKernelSwitchPolicy r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelSwitchPolicy l, CanonicalKernelSwitchPolicy r) => !l.Equals(r);
}

public sealed class CanonicalKernelSwitchAdvancedOverrides : IEquatable<CanonicalKernelSwitchAdvancedOverrides>
{
    public CanonicalSyncRuntimeConfiguration? SyncRuntimeConfiguration { get; }
    public CanonicalApplyRuntimeConfiguration? ApplyRuntimeConfiguration { get; }
    public CanonicalExistenceApplyRuntimeConfiguration? ExistenceApplyRuntimeConfiguration { get; }
    public CanonicalAudioUploadRuntimeConfiguration? AudioUploadRuntimeConfiguration { get; }
    public CanonicalReadRuntimeConfiguration? ReadRuntimeConfiguration { get; }
    public CanonicalLibraryMetadataDebugPilotConfiguration? LibraryMetadataDebugPilotConfiguration { get; }

    public CanonicalKernelSwitchAdvancedOverrides(
        CanonicalSyncRuntimeConfiguration? syncRuntimeConfiguration = null,
        CanonicalApplyRuntimeConfiguration? applyRuntimeConfiguration = null,
        CanonicalExistenceApplyRuntimeConfiguration? existenceApplyRuntimeConfiguration = null,
        CanonicalAudioUploadRuntimeConfiguration? audioUploadRuntimeConfiguration = null,
        CanonicalReadRuntimeConfiguration? readRuntimeConfiguration = null,
        CanonicalLibraryMetadataDebugPilotConfiguration? libraryMetadataDebugPilotConfiguration = null)
    {
        SyncRuntimeConfiguration = syncRuntimeConfiguration;
        ApplyRuntimeConfiguration = applyRuntimeConfiguration;
        ExistenceApplyRuntimeConfiguration = existenceApplyRuntimeConfiguration;
        AudioUploadRuntimeConfiguration = audioUploadRuntimeConfiguration;
        ReadRuntimeConfiguration = readRuntimeConfiguration;
        LibraryMetadataDebugPilotConfiguration = libraryMetadataDebugPilotConfiguration;
    }

    public static CanonicalKernelSwitchAdvancedOverrides None => new();

    public override bool Equals(object? obj) => obj is CanonicalKernelSwitchAdvancedOverrides other && Equals(other);
    public bool Equals(CanonicalKernelSwitchAdvancedOverrides? other) =>
        other is not null &&
        Equals(SyncRuntimeConfiguration, other.SyncRuntimeConfiguration) &&
        Equals(ApplyRuntimeConfiguration, other.ApplyRuntimeConfiguration) &&
        Equals(ExistenceApplyRuntimeConfiguration, other.ExistenceApplyRuntimeConfiguration) &&
        Equals(AudioUploadRuntimeConfiguration, other.AudioUploadRuntimeConfiguration) &&
        Equals(ReadRuntimeConfiguration, other.ReadRuntimeConfiguration) &&
        Equals(LibraryMetadataDebugPilotConfiguration, other.LibraryMetadataDebugPilotConfiguration);
    public override int GetHashCode() => HashCode.Combine(
        SyncRuntimeConfiguration, ApplyRuntimeConfiguration, ExistenceApplyRuntimeConfiguration,
        AudioUploadRuntimeConfiguration, ReadRuntimeConfiguration, LibraryMetadataDebugPilotConfiguration);
    public static bool operator ==(CanonicalKernelSwitchAdvancedOverrides l, CanonicalKernelSwitchAdvancedOverrides r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelSwitchAdvancedOverrides l, CanonicalKernelSwitchAdvancedOverrides r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalKernelSwitchBlocker
{
    explicitBlockedMode,
    releaseDefaultCannotUseCanonicalFullSync,
    canonicalFullSyncRequiresDebugInternalBuild,
    canonicalFullSyncRequiresOwnerApproval,
    canonicalFullSyncRequiresManualConfirmation,
    legacyFallbackUnavailable,
    diagnosticsNotRedacted,
    legacyReadPathUnavailable,
    legacyWritePathUnavailable,
    canonicalWritesNotLegacyReadable,
    switchBackWouldRequireDataFormatMigration,
    canonicalOnlyRequiredFieldWithoutLegacyFallback,
    physicalMoveOrDeleteWouldBeRequired,
    secretPathHashLeakRisk,
    shadowCompareCannotStayEnabledWithCanonicalOwner,
    advancedOverrideContradictsMasterSwitch
}

public sealed class CanonicalKernelSwitchReversibilityProof : IEquatable<CanonicalKernelSwitchReversibilityProof>
{
    public bool LegacyReadPathStillExists { get; }
    public bool LegacyWritePathStillExists { get; }
    public bool CanonicalWritesAreLegacyReadable { get; }
    public bool NoDataFormatMigrationRequiredToSwitchBack { get; }
    public bool NoCanonicalOnlyRequiredFieldWithoutLegacyFallback { get; }
    public bool NoPhysicalMoveOrDeleteRequired { get; }
    public bool SecretPathHashLeakRedactionEnabled { get; }
    public bool ShadowCompareCanStayOnWhileCanonicalOwnerActive { get; }
    public bool RequiresDataMigrationToSwitchBack { get; }
    public List<CanonicalKernelSwitchBlocker> Blockers { get; }

    public bool IsReversible => Blockers.Count == 0 && !RequiresDataMigrationToSwitchBack;

    public CanonicalKernelSwitchReversibilityProof(
        bool legacyReadPathStillExists,
        bool legacyWritePathStillExists,
        bool canonicalWritesAreLegacyReadable,
        bool noDataFormatMigrationRequiredToSwitchBack,
        bool noCanonicalOnlyRequiredFieldWithoutLegacyFallback,
        bool noPhysicalMoveOrDeleteRequired,
        bool secretPathHashLeakRedactionEnabled,
        bool shadowCompareCanStayOnWhileCanonicalOwnerActive,
        bool requiresDataMigrationToSwitchBack,
        List<CanonicalKernelSwitchBlocker> blockers)
    {
        LegacyReadPathStillExists = legacyReadPathStillExists;
        LegacyWritePathStillExists = legacyWritePathStillExists;
        CanonicalWritesAreLegacyReadable = canonicalWritesAreLegacyReadable;
        NoDataFormatMigrationRequiredToSwitchBack = noDataFormatMigrationRequiredToSwitchBack;
        NoCanonicalOnlyRequiredFieldWithoutLegacyFallback = noCanonicalOnlyRequiredFieldWithoutLegacyFallback;
        NoPhysicalMoveOrDeleteRequired = noPhysicalMoveOrDeleteRequired;
        SecretPathHashLeakRedactionEnabled = secretPathHashLeakRedactionEnabled;
        ShadowCompareCanStayOnWhileCanonicalOwnerActive = shadowCompareCanStayOnWhileCanonicalOwnerActive;
        RequiresDataMigrationToSwitchBack = requiresDataMigrationToSwitchBack;
        Blockers = blockers;
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelSwitchReversibilityProof other && Equals(other);
    public bool Equals(CanonicalKernelSwitchReversibilityProof? other) =>
        other is not null &&
        LegacyReadPathStillExists == other.LegacyReadPathStillExists &&
        LegacyWritePathStillExists == other.LegacyWritePathStillExists &&
        CanonicalWritesAreLegacyReadable == other.CanonicalWritesAreLegacyReadable &&
        NoDataFormatMigrationRequiredToSwitchBack == other.NoDataFormatMigrationRequiredToSwitchBack &&
        NoCanonicalOnlyRequiredFieldWithoutLegacyFallback == other.NoCanonicalOnlyRequiredFieldWithoutLegacyFallback &&
        NoPhysicalMoveOrDeleteRequired == other.NoPhysicalMoveOrDeleteRequired &&
        SecretPathHashLeakRedactionEnabled == other.SecretPathHashLeakRedactionEnabled &&
        ShadowCompareCanStayOnWhileCanonicalOwnerActive == other.ShadowCompareCanStayOnWhileCanonicalOwnerActive &&
        RequiresDataMigrationToSwitchBack == other.RequiresDataMigrationToSwitchBack;
    public override int GetHashCode() => HashCode.Combine(
        LegacyReadPathStillExists, LegacyWritePathStillExists, CanonicalWritesAreLegacyReadable,
        NoDataFormatMigrationRequiredToSwitchBack, NoCanonicalOnlyRequiredFieldWithoutLegacyFallback,
        NoPhysicalMoveOrDeleteRequired, SecretPathHashLeakRedactionEnabled,
        ShadowCompareCanStayOnWhileCanonicalOwnerActive, RequiresDataMigrationToSwitchBack);
    public static bool operator ==(CanonicalKernelSwitchReversibilityProof l, CanonicalKernelSwitchReversibilityProof r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelSwitchReversibilityProof l, CanonicalKernelSwitchReversibilityProof r) => !l.Equals(r);
}

public sealed class CanonicalKernelSwitchReversibilityGate
{
    public CanonicalKernelSwitchReversibilityProof Prove(CanonicalKernelSwitchPolicy policy)
    {
        var blockers = new List<CanonicalKernelSwitchBlocker>();
        if (!policy.LegacyReadPathAvailable)
            blockers.Add(CanonicalKernelSwitchBlocker.legacyReadPathUnavailable);
        if (!policy.LegacyWritePathAvailable)
            blockers.Add(CanonicalKernelSwitchBlocker.legacyWritePathUnavailable);
        if (!policy.CanonicalWritesLegacyReadable)
            blockers.Add(CanonicalKernelSwitchBlocker.canonicalWritesNotLegacyReadable);
        if (!policy.NoDataFormatMigrationRequired)
            blockers.Add(CanonicalKernelSwitchBlocker.switchBackWouldRequireDataFormatMigration);
        if (!policy.CanonicalOnlyRequiredFieldsHaveLegacyFallback)
            blockers.Add(CanonicalKernelSwitchBlocker.canonicalOnlyRequiredFieldWithoutLegacyFallback);
        if (!policy.PhysicalMoveDeleteDisabled)
            blockers.Add(CanonicalKernelSwitchBlocker.physicalMoveOrDeleteWouldBeRequired);
        if (!policy.SecretPathHashLeakRedactionEnabled)
            blockers.Add(CanonicalKernelSwitchBlocker.secretPathHashLeakRisk);
        if (!policy.ShadowCompareAllowedDuringCanonicalOwner)
            blockers.Add(CanonicalKernelSwitchBlocker.shadowCompareCannotStayEnabledWithCanonicalOwner);

        return new CanonicalKernelSwitchReversibilityProof(
            legacyReadPathStillExists: policy.LegacyReadPathAvailable,
            legacyWritePathStillExists: policy.LegacyWritePathAvailable,
            canonicalWritesAreLegacyReadable: policy.CanonicalWritesLegacyReadable,
            noDataFormatMigrationRequiredToSwitchBack: policy.NoDataFormatMigrationRequired,
            noCanonicalOnlyRequiredFieldWithoutLegacyFallback: policy.CanonicalOnlyRequiredFieldsHaveLegacyFallback,
            noPhysicalMoveOrDeleteRequired: policy.PhysicalMoveDeleteDisabled,
            secretPathHashLeakRedactionEnabled: policy.SecretPathHashLeakRedactionEnabled,
            shadowCompareCanStayOnWhileCanonicalOwnerActive: policy.ShadowCompareAllowedDuringCanonicalOwner,
            requiresDataMigrationToSwitchBack: !policy.NoDataFormatMigrationRequired,
            blockers: blockers
        );
    }
}

public sealed class CanonicalKernelSwitchMigrationMatrixPolicy : IEquatable<CanonicalKernelSwitchMigrationMatrixPolicy>
{
    public CanonicalKernelSwitchMode Mode { get; }
    public CanonicalKernelSwitchOwnerState OwnerState { get; }
    public List<CanonicalMigrationDomain> ActiveCanonicalOwnershipDomains { get; }
    public bool LegacyReadPathRetained { get; }
    public bool LegacyWritePathRetained { get; }
    public bool MigrationRequiredToSwitchBack { get; }
    public string DiskFormatPolicy { get; }
    public bool DiagnosticsRedacted { get; }

    public CanonicalKernelSwitchMigrationMatrixPolicy(
        CanonicalKernelSwitchMode mode,
        CanonicalKernelSwitchOwnerState ownerState,
        List<CanonicalMigrationDomain> activeCanonicalOwnershipDomains,
        bool legacyReadPathRetained,
        bool legacyWritePathRetained,
        bool migrationRequiredToSwitchBack,
        string diskFormatPolicy,
        bool diagnosticsRedacted)
    {
        Mode = mode;
        OwnerState = ownerState;
        ActiveCanonicalOwnershipDomains = activeCanonicalOwnershipDomains
            .OrderBy(d => d.ToString()).ToList();
        LegacyReadPathRetained = legacyReadPathRetained;
        LegacyWritePathRetained = legacyWritePathRetained;
        MigrationRequiredToSwitchBack = migrationRequiredToSwitchBack;
        DiskFormatPolicy = diskFormatPolicy;
        DiagnosticsRedacted = diagnosticsRedacted;
    }

    public static CanonicalKernelSwitchMigrationMatrixPolicy Make(
        CanonicalKernelSwitchMode mode,
        CanonicalKernelSwitchOwnerState ownerState,
        List<CanonicalMigrationDomain> activeCanonicalOwnershipDomains,
        CanonicalKernelSwitchPolicy policy,
        CanonicalKernelSwitchReversibilityProof proof)
    {
        return new CanonicalKernelSwitchMigrationMatrixPolicy(
            mode: mode,
            ownerState: ownerState,
            activeCanonicalOwnershipDomains: activeCanonicalOwnershipDomains,
            legacyReadPathRetained: policy.LegacyReadPathAvailable,
            legacyWritePathRetained: policy.LegacyWritePathAvailable,
            migrationRequiredToSwitchBack: proof.RequiresDataMigrationToSwitchBack,
            diskFormatPolicy: "legacy-readable-or-dual-write-compatible",
            diagnosticsRedacted: policy.DiagnosticsRedacted
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelSwitchMigrationMatrixPolicy other && Equals(other);
    public bool Equals(CanonicalKernelSwitchMigrationMatrixPolicy? other) =>
        other is not null &&
        Mode == other.Mode &&
        OwnerState == other.OwnerState &&
        ActiveCanonicalOwnershipDomains.SequenceEqual(other.ActiveCanonicalOwnershipDomains) &&
        LegacyReadPathRetained == other.LegacyReadPathRetained &&
        LegacyWritePathRetained == other.LegacyWritePathRetained &&
        MigrationRequiredToSwitchBack == other.MigrationRequiredToSwitchBack &&
        DiskFormatPolicy == other.DiskFormatPolicy &&
        DiagnosticsRedacted == other.DiagnosticsRedacted;
    public override int GetHashCode() => HashCode.Combine(Mode, OwnerState, LegacyReadPathRetained,
        LegacyWritePathRetained, MigrationRequiredToSwitchBack, DiskFormatPolicy, DiagnosticsRedacted);
    public static bool operator ==(CanonicalKernelSwitchMigrationMatrixPolicy l, CanonicalKernelSwitchMigrationMatrixPolicy r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelSwitchMigrationMatrixPolicy l, CanonicalKernelSwitchMigrationMatrixPolicy r) => !l.Equals(r);
}

public sealed class CanonicalKernelSwitchEffectiveConfiguration : IEquatable<CanonicalKernelSwitchEffectiveConfiguration>
{
    public CanonicalInventoryRuntimeConfiguration InventoryRuntimeConfiguration { get; }
    public CanonicalSyncRuntimeConfiguration SyncRuntimeConfiguration { get; }
    public CanonicalApplyRuntimeConfiguration ApplyRuntimeConfiguration { get; }
    public CanonicalExistenceApplyRuntimeConfiguration ExistenceApplyRuntimeConfiguration { get; }
    public CanonicalAudioUploadRuntimeConfiguration AudioUploadRuntimeConfiguration { get; }
    public CanonicalReadRuntimeConfiguration ReadRuntimeConfiguration { get; }
    public CanonicalLibraryMetadataDebugPilotConfiguration LibraryMetadataDebugPilotConfiguration { get; }
    public CanonicalKernelSwitchMigrationMatrixPolicy MigrationMatrixPolicy { get; }

    public CanonicalKernelSwitchEffectiveConfiguration(
        CanonicalInventoryRuntimeConfiguration inventoryRuntimeConfiguration,
        CanonicalSyncRuntimeConfiguration syncRuntimeConfiguration,
        CanonicalApplyRuntimeConfiguration applyRuntimeConfiguration,
        CanonicalExistenceApplyRuntimeConfiguration existenceApplyRuntimeConfiguration,
        CanonicalAudioUploadRuntimeConfiguration audioUploadRuntimeConfiguration,
        CanonicalReadRuntimeConfiguration readRuntimeConfiguration,
        CanonicalLibraryMetadataDebugPilotConfiguration libraryMetadataDebugPilotConfiguration,
        CanonicalKernelSwitchMigrationMatrixPolicy migrationMatrixPolicy)
    {
        InventoryRuntimeConfiguration = inventoryRuntimeConfiguration;
        SyncRuntimeConfiguration = syncRuntimeConfiguration;
        ApplyRuntimeConfiguration = applyRuntimeConfiguration;
        ExistenceApplyRuntimeConfiguration = existenceApplyRuntimeConfiguration;
        AudioUploadRuntimeConfiguration = audioUploadRuntimeConfiguration;
        ReadRuntimeConfiguration = readRuntimeConfiguration;
        LibraryMetadataDebugPilotConfiguration = libraryMetadataDebugPilotConfiguration;
        MigrationMatrixPolicy = migrationMatrixPolicy;
    }

    public static CanonicalKernelSwitchEffectiveConfiguration Blocked(
        CanonicalKernelSwitchPolicy policy,
        CanonicalKernelSwitchReversibilityProof proof)
    {
        return new CanonicalKernelSwitchEffectiveConfiguration(
            inventoryRuntimeConfiguration: new CanonicalInventoryRuntimeConfiguration(redactedDiagnostics: policy.DiagnosticsRedacted),
            syncRuntimeConfiguration: new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.blocked),
            applyRuntimeConfiguration: new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.blocked),
            existenceApplyRuntimeConfiguration: new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeMode.blocked),
            audioUploadRuntimeConfiguration: new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.blocked),
            readRuntimeConfiguration: new CanonicalReadRuntimeConfiguration(CanonicalReadRuntimeMode.blocked),
            libraryMetadataDebugPilotConfiguration: new CanonicalLibraryMetadataDebugPilotConfiguration(CanonicalLibraryMetadataDebugPilotMode.blocked),
            migrationMatrixPolicy: CanonicalKernelSwitchMigrationMatrixPolicy.Make(
                CanonicalKernelSwitchMode.blocked,
                CanonicalKernelSwitchOwnerState.blocked,
                new List<CanonicalMigrationDomain>(),
                policy, proof)
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelSwitchEffectiveConfiguration other && Equals(other);
    public bool Equals(CanonicalKernelSwitchEffectiveConfiguration? other) =>
        other is not null &&
        InventoryRuntimeConfiguration.Equals(other.InventoryRuntimeConfiguration) &&
        SyncRuntimeConfiguration.Equals(other.SyncRuntimeConfiguration) &&
        ApplyRuntimeConfiguration.Equals(other.ApplyRuntimeConfiguration) &&
        ExistenceApplyRuntimeConfiguration.Equals(other.ExistenceApplyRuntimeConfiguration) &&
        AudioUploadRuntimeConfiguration.Equals(other.AudioUploadRuntimeConfiguration) &&
        ReadRuntimeConfiguration.Equals(other.ReadRuntimeConfiguration) &&
        LibraryMetadataDebugPilotConfiguration.Equals(other.LibraryMetadataDebugPilotConfiguration) &&
        MigrationMatrixPolicy.Equals(other.MigrationMatrixPolicy);
    public override int GetHashCode() => HashCode.Combine(
        InventoryRuntimeConfiguration, SyncRuntimeConfiguration, ApplyRuntimeConfiguration,
        ExistenceApplyRuntimeConfiguration, AudioUploadRuntimeConfiguration, ReadRuntimeConfiguration,
        LibraryMetadataDebugPilotConfiguration, MigrationMatrixPolicy);
    public static bool operator ==(CanonicalKernelSwitchEffectiveConfiguration l, CanonicalKernelSwitchEffectiveConfiguration r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelSwitchEffectiveConfiguration l, CanonicalKernelSwitchEffectiveConfiguration r) => !l.Equals(r);
}

public sealed class CanonicalKernelSwitchResult : IEquatable<CanonicalKernelSwitchResult>
{
    public CanonicalKernelSwitchMode RequestedMode { get; }
    public CanonicalKernelSwitchMode EffectiveMode { get; }
    public CanonicalKernelSwitchOwnerState OwnerState { get; }
    public List<CanonicalKernelSwitchBlocker> Blockers { get; }
    public CanonicalKernelSwitchEffectiveConfiguration EffectiveConfiguration { get; }
    public CanonicalKernelSwitchReversibilityProof ReversibilityProof { get; }
    public string DiagnosticsSummary { get; }
    public bool Redacted { get; }

    public bool IsBlocked => EffectiveMode == CanonicalKernelSwitchMode.blocked || Blockers.Count != 0;

    public CanonicalKernelSwitchResult(
        CanonicalKernelSwitchMode requestedMode,
        CanonicalKernelSwitchMode effectiveMode,
        CanonicalKernelSwitchOwnerState ownerState,
        List<CanonicalKernelSwitchBlocker> blockers,
        CanonicalKernelSwitchEffectiveConfiguration effectiveConfiguration,
        CanonicalKernelSwitchReversibilityProof reversibilityProof,
        string diagnosticsSummary,
        bool redacted)
    {
        RequestedMode = requestedMode;
        EffectiveMode = effectiveMode;
        OwnerState = ownerState;
        Blockers = blockers;
        EffectiveConfiguration = effectiveConfiguration;
        ReversibilityProof = reversibilityProof;
        DiagnosticsSummary = diagnosticsSummary;
        Redacted = redacted;
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelSwitchResult other && Equals(other);
    public bool Equals(CanonicalKernelSwitchResult? other) =>
        other is not null &&
        RequestedMode == other.RequestedMode &&
        EffectiveMode == other.EffectiveMode &&
        OwnerState == other.OwnerState &&
        Blockers.SequenceEqual(other.Blockers) &&
        EffectiveConfiguration.Equals(other.EffectiveConfiguration) &&
        ReversibilityProof.Equals(other.ReversibilityProof) &&
        DiagnosticsSummary == other.DiagnosticsSummary &&
        Redacted == other.Redacted;
    public override int GetHashCode() => HashCode.Combine(RequestedMode, EffectiveMode, OwnerState,
        DiagnosticsSummary, Redacted);
    public static bool operator ==(CanonicalKernelSwitchResult l, CanonicalKernelSwitchResult r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelSwitchResult l, CanonicalKernelSwitchResult r) => !l.Equals(r);
}

public sealed class CanonicalKernelSwitchConfiguration : IEquatable<CanonicalKernelSwitchConfiguration>
{
    public const string DebugModeKey = "Rokurics.debug.canonicalKernelSwitch.mode";
    public const string DebugFullSyncConfirmedKey = "Rokurics.debug.canonicalKernelSwitch.fullSyncConfirmed";
    public const string DiagnosticsPathText = "Application Support/Rokurics/Diagnostics/canonical-kernel-switch.log";

    public CanonicalKernelSwitchMode Mode { get; }
    public CanonicalKernelSwitchPolicy Policy { get; }
    public CanonicalKernelSwitchAdvancedOverrides AdvancedOverrides { get; }

    public CanonicalKernelSwitchConfiguration(
        CanonicalKernelSwitchMode mode = CanonicalKernelSwitchMode.oldKernel,
        CanonicalKernelSwitchPolicy? policy = null,
        CanonicalKernelSwitchAdvancedOverrides? advancedOverrides = null)
    {
        Mode = mode;
        Policy = policy ?? CanonicalKernelSwitchPolicy.ReleaseDefault;
        AdvancedOverrides = advancedOverrides ?? CanonicalKernelSwitchAdvancedOverrides.None;
    }

    public static CanonicalKernelSwitchConfiguration Default => new();
    public static CanonicalKernelSwitchConfiguration OldKernel => new();

    public static List<CanonicalKernelSwitchModeChoice> DebugModeChoices => new()
    {
        new(CanonicalKernelSwitchMode.oldKernel),
        new(CanonicalKernelSwitchMode.diagnosticsOnly),
        new(CanonicalKernelSwitchMode.canonicalShadow),
        new(CanonicalKernelSwitchMode.canonicalDecisionOnly),
        new(CanonicalKernelSwitchMode.canonicalApplyNoAudio),
        new(CanonicalKernelSwitchMode.canonicalFullSync)
    };

    public static string DidChangeNotificationName => "RokuricsCanonicalKernelSwitchConfigurationDidChange";

    public static string NormalizedDebugMode(string rawValue)
    {
        if (Enum.TryParse<CanonicalKernelSwitchMode>(rawValue, out var mode))
            return mode.ToString();
        return CanonicalKernelSwitchMode.oldKernel.ToString();
    }

    public static CanonicalKernelSwitchConfiguration DebugStoredConfiguration(
        Dictionary<string, object>? userDefaults = null)
    {
        var defaults = userDefaults ?? new Dictionary<string, object>();
        var storedMode = NormalizedDebugMode(
            defaults.TryGetValue(DebugModeKey, out var v) ? v?.ToString() ?? "" : CanonicalKernelSwitchMode.oldKernel.ToString());
        var mode = Enum.TryParse<CanonicalKernelSwitchMode>(storedMode, out var parsed) ? parsed : CanonicalKernelSwitchMode.oldKernel;
        var fullSyncConfirmed = defaults.TryGetValue(DebugFullSyncConfirmedKey, out var c) && c is bool b && b;
        return new CanonicalKernelSwitchConfiguration(
            mode: mode,
            policy: CanonicalKernelSwitchPolicy.DebugInternal(ownerApproved: true, manualFullSyncConfirmation: fullSyncConfirmed)
        );
    }

    public static CanonicalKernelSwitchConfiguration RuntimeConfigurationFromStoredDefaults(
        Dictionary<string, object>? userDefaults = null)
    {
#if DEBUG
        return DebugStoredConfiguration(userDefaults);
#else
        return OldKernel;
#endif
    }

    public static void SetDebugStoredMode(
        string rawValue,
        Dictionary<string, object>? userDefaults = null,
        bool postNotification = true)
    {
        var defaults = userDefaults ?? new Dictionary<string, object>();
        var normalized = NormalizedDebugMode(rawValue);
        defaults[DebugModeKey] = normalized;
        if (normalized != CanonicalKernelSwitchMode.canonicalFullSync.ToString())
            defaults[DebugFullSyncConfirmedKey] = false;
        if (postNotification)
            OnConfigurationDidChange?.Invoke();
    }

    public static void SetDebugFullSyncConfirmed(
        bool confirmed,
        Dictionary<string, object>? userDefaults = null,
        bool postNotification = true)
    {
        var defaults = userDefaults ?? new Dictionary<string, object>();
        defaults[DebugFullSyncConfirmedKey] = confirmed;
        if (postNotification)
            OnConfigurationDidChange?.Invoke();
    }

    public static event Action? OnConfigurationDidChange;

    public CanonicalKernelSwitchResult Resolve(
        CanonicalKernelSwitchReversibilityGate? reversibilityGate = null)
    {
        var gate = reversibilityGate ?? new CanonicalKernelSwitchReversibilityGate();
        var proof = gate.Prove(Policy);
        var blockers = new List<CanonicalKernelSwitchBlocker>(proof.Blockers);

        if (Mode == CanonicalKernelSwitchMode.blocked)
            blockers.Add(CanonicalKernelSwitchBlocker.explicitBlockedMode);
        if (!Policy.LegacyFallbackAvailable)
            blockers.Add(CanonicalKernelSwitchBlocker.legacyFallbackUnavailable);
        if (!Policy.DiagnosticsRedacted)
            blockers.Add(CanonicalKernelSwitchBlocker.diagnosticsNotRedacted);

        if (Mode == CanonicalKernelSwitchMode.canonicalFullSync)
        {
            if (Policy.ReleaseDefaultBuild)
                blockers.Add(CanonicalKernelSwitchBlocker.releaseDefaultCannotUseCanonicalFullSync);
            if (!Policy.DebugInternalBuild)
                blockers.Add(CanonicalKernelSwitchBlocker.canonicalFullSyncRequiresDebugInternalBuild);
            if (!Policy.OwnerApproved)
                blockers.Add(CanonicalKernelSwitchBlocker.canonicalFullSyncRequiresOwnerApproval);
            if (!Policy.ManualFullSyncConfirmation)
                blockers.Add(CanonicalKernelSwitchBlocker.canonicalFullSyncRequiresManualConfirmation);
        }

        if ((Mode == CanonicalKernelSwitchMode.canonicalShadow || Mode == CanonicalKernelSwitchMode.canonicalFullSync) &&
            !Policy.ShadowComparisonEnabled)
            blockers.Add(CanonicalKernelSwitchBlocker.shadowCompareCannotStayEnabledWithCanonicalOwner);

        var baseConfig = MakeEffectiveConfiguration(Mode, proof, applyingOverrides: false);
        blockers.AddRange(AdvancedOverrideBlockers(baseConfig));
        var effectiveConfiguration = MakeEffectiveConfiguration(Mode, proof, applyingOverrides: true);

        var uniqueBlockers = Unique(blockers);
        if (uniqueBlockers.Count != 0)
        {
            var blockedConfiguration = CanonicalKernelSwitchEffectiveConfiguration.Blocked(Policy, proof);
            return new CanonicalKernelSwitchResult(
                requestedMode: Mode,
                effectiveMode: CanonicalKernelSwitchMode.blocked,
                ownerState: CanonicalKernelSwitchOwnerState.blocked,
                blockers: uniqueBlockers,
                effectiveConfiguration: blockedConfiguration,
                reversibilityProof: proof,
                diagnosticsSummary: DiagnosticsSummary(Mode, CanonicalKernelSwitchMode.blocked,
                    CanonicalKernelSwitchOwnerState.blocked, blockedConfiguration, uniqueBlockers),
                redacted: Policy.DiagnosticsRedacted
            );
        }

        var ownerState = OwnerStateFor(Mode);
        return new CanonicalKernelSwitchResult(
            requestedMode: Mode,
            effectiveMode: Mode,
            ownerState: ownerState,
            blockers: new List<CanonicalKernelSwitchBlocker>(),
            effectiveConfiguration: effectiveConfiguration,
            reversibilityProof: proof,
            diagnosticsSummary: DiagnosticsSummary(Mode, Mode, ownerState, effectiveConfiguration,
                new List<CanonicalKernelSwitchBlocker>()),
            redacted: Policy.DiagnosticsRedacted
        );
    }

    private CanonicalKernelSwitchEffectiveConfiguration MakeEffectiveConfiguration(
        CanonicalKernelSwitchMode mode,
        CanonicalKernelSwitchReversibilityProof proof,
        bool applyingOverrides)
    {
        var inventory = new CanonicalInventoryRuntimeConfiguration(redactedDiagnostics: Policy.DiagnosticsRedacted);
        var syncPolicy = CanonicalSyncPolicy();
        var applyPolicy = CanonicalApplyPolicy();
        var existencePolicy = CanonicalExistencePolicy();
        var audioPolicy = CanonicalAudioPolicy();
        var readPolicy = CanonicalReadPolicy();

        CanonicalSyncRuntimeConfiguration sync;
        CanonicalApplyRuntimeConfiguration apply;
        CanonicalExistenceApplyRuntimeConfiguration existence;
        CanonicalAudioUploadRuntimeConfiguration audio;
        CanonicalReadRuntimeConfiguration read;
        CanonicalLibraryMetadataDebugPilotConfiguration libraryPilot;
        List<CanonicalMigrationDomain> activeDomains;

        switch (mode)
        {
            case CanonicalKernelSwitchMode.oldKernel:
                sync = CanonicalSyncRuntimeConfiguration.Disabled;
                apply = CanonicalApplyRuntimeConfiguration.Disabled;
                existence = CanonicalExistenceApplyRuntimeConfiguration.Disabled;
                audio = CanonicalAudioUploadRuntimeConfiguration.Disabled;
                read = CanonicalReadRuntimeConfiguration.Disabled;
                libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.Disabled;
                activeDomains = new List<CanonicalMigrationDomain>();
                break;
            case CanonicalKernelSwitchMode.diagnosticsOnly:
                sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.diagnosticsOnly, syncPolicy);
                apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.diagnosticsOnly, applyPolicy);
                existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeMode.diagnosticsOnly, existencePolicy);
                audio = new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.diagnosticsOnly, audioPolicy);
                read = CanonicalReadRuntimeConfiguration.Disabled;
                libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DiagnosticsOnly();
                activeDomains = new List<CanonicalMigrationDomain>();
                break;
            case CanonicalKernelSwitchMode.canonicalShadow:
                sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.canonicalPlanNoCommit, syncPolicy);
                apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.noCommit, applyPolicy);
                existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeMode.noCommit, existencePolicy);
                audio = new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.noCommit, audioPolicy);
                read = new CanonicalReadRuntimeConfiguration(
                    Policy.ShadowComparisonEnabled ? CanonicalReadRuntimeMode.parallelCompare : CanonicalReadRuntimeMode.disabled,
                    readPolicy);
                libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.Disabled;
                activeDomains = new List<CanonicalMigrationDomain>();
                break;
            case CanonicalKernelSwitchMode.canonicalDecisionOnly:
                sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback, syncPolicy);
                apply = CanonicalApplyRuntimeConfiguration.Disabled;
                existence = CanonicalExistenceApplyRuntimeConfiguration.Disabled;
                audio = CanonicalAudioUploadRuntimeConfiguration.Disabled;
                read = CanonicalReadRuntimeConfiguration.Disabled;
                libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.Disabled;
                activeDomains = new List<CanonicalMigrationDomain>();
                break;
            case CanonicalKernelSwitchMode.canonicalApplyNoAudio:
                sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback, syncPolicy);
                apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.productionRootApplyWithLegacyFallback, applyPolicy);
                existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeMode.productionRootApply, existencePolicy);
                audio = CanonicalAudioUploadRuntimeConfiguration.Disabled;
                read = CanonicalReadRuntimeConfiguration.Disabled;
                libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.Disabled;
                activeDomains = new List<CanonicalMigrationDomain>
                {
                    CanonicalMigrationDomain.recordingMetadata,
                    CanonicalMigrationDomain.generatedArtifacts,
                    CanonicalMigrationDomain.libraryMetadata,
                    CanonicalMigrationDomain.tombstoneConflict
                };
                break;
            case CanonicalKernelSwitchMode.canonicalFullSync:
                sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.canonicalPlanPrimaryWithLegacyFallback, syncPolicy);
                apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.productionRootApplyWithLegacyFallback, applyPolicy);
                existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeMode.productionRootApply, existencePolicy);
                audio = new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback, audioPolicy);
                read = new CanonicalReadRuntimeConfiguration(CanonicalReadRuntimeMode.guardedCanonicalReadWithLegacyFallback, readPolicy);
                libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.Disabled;
                activeDomains = new List<CanonicalMigrationDomain>
                {
                    CanonicalMigrationDomain.recordingMetadata,
                    CanonicalMigrationDomain.generatedArtifacts,
                    CanonicalMigrationDomain.libraryMetadata,
                    CanonicalMigrationDomain.tombstoneConflict,
                    CanonicalMigrationDomain.audioUpload,
                    CanonicalMigrationDomain.uiProjection
                };
                break;
            default:
                sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.blocked);
                apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.blocked);
                existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeMode.blocked);
                audio = new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.blocked);
                read = new CanonicalReadRuntimeConfiguration(CanonicalReadRuntimeMode.blocked);
                libraryPilot = new CanonicalLibraryMetadataDebugPilotConfiguration(CanonicalLibraryMetadataDebugPilotMode.blocked);
                activeDomains = new List<CanonicalMigrationDomain>();
                break;
        }

        return new CanonicalKernelSwitchEffectiveConfiguration(
            inventoryRuntimeConfiguration: inventory,
            syncRuntimeConfiguration: applyingOverrides ? (AdvancedOverrides.SyncRuntimeConfiguration ?? sync) : sync,
            applyRuntimeConfiguration: applyingOverrides ? (AdvancedOverrides.ApplyRuntimeConfiguration ?? apply) : apply,
            existenceApplyRuntimeConfiguration: applyingOverrides ? (AdvancedOverrides.ExistenceApplyRuntimeConfiguration ?? existence) : existence,
            audioUploadRuntimeConfiguration: applyingOverrides ? (AdvancedOverrides.AudioUploadRuntimeConfiguration ?? audio) : audio,
            readRuntimeConfiguration: applyingOverrides ? (AdvancedOverrides.ReadRuntimeConfiguration ?? read) : read,
            libraryMetadataDebugPilotConfiguration: applyingOverrides ? (AdvancedOverrides.LibraryMetadataDebugPilotConfiguration ?? libraryPilot) : libraryPilot,
            migrationMatrixPolicy: CanonicalKernelSwitchMigrationMatrixPolicy.Make(
                mode, OwnerStateFor(mode), activeDomains, Policy, proof)
        );
    }

    private CanonicalSyncRuntimePolicy CanonicalSyncPolicy()
    {
        return new CanonicalSyncRuntimePolicy(
            debugInternalBuild: Policy.DebugInternalBuild,
            ownerApproved: Policy.OwnerApproved,
            releaseDefaultBuild: Policy.ReleaseDefaultBuild,
            legacyFallbackAvailable: Policy.LegacyFallbackAvailable,
            diagnosticsRedacted: Policy.DiagnosticsRedacted,
            runtimeSwitchEnabled: false,
            readPathLegacy: true,
            otherActiveMigrationDomainConflicting: false,
            allowDocumentedModifiedAtFallback: true
        );
    }

    private CanonicalApplyRuntimePolicy CanonicalApplyPolicy()
    {
        return new CanonicalApplyRuntimePolicy(
            debugInternalBuild: Policy.DebugInternalBuild,
            ownerApproved: Policy.OwnerApproved,
            releaseDefaultBuild: Policy.ReleaseDefaultBuild,
            legacyFallbackAvailable: Policy.LegacyFallbackAvailable,
            diagnosticsRedacted: Policy.DiagnosticsRedacted,
            runtimeSwitchEnabled: false,
            readPathLegacy: true,
            enabledDomains: new List<CanonicalProductionDomain>
            {
                CanonicalProductionDomain.recordingMetadata,
                CanonicalProductionDomain.libraryMetadata,
                CanonicalProductionDomain.generatedArtifacts,
                CanonicalProductionDomain.tombstoneConflict,
                CanonicalProductionDomain.recordingExistence
            },
            allowConflictRecordAction: true,
            allowTestRootApply: false
        );
    }

    private CanonicalExistenceApplyRuntimePolicy CanonicalExistencePolicy()
    {
        return new CanonicalExistenceApplyRuntimePolicy(
            debugInternalBuild: Policy.DebugInternalBuild,
            ownerApproved: Policy.OwnerApproved,
            releaseDefaultBuild: Policy.ReleaseDefaultBuild,
            diagnosticsRedacted: Policy.DiagnosticsRedacted,
            legacyFallbackAvailable: Policy.LegacyFallbackAvailable,
            rootBoundRequired: true,
            rollbackRequired: true,
            atomicWriteRequired: true,
            postconditionRequired: true,
            writeAudioAllowed: false,
            markAudioAvailableAllowed: false
        );
    }

    private CanonicalAudioUploadRuntimePolicy CanonicalAudioPolicy()
    {
        return new CanonicalAudioUploadRuntimePolicy(
            debugInternalBuild: Policy.DebugInternalBuild,
            ownerApprovedCanonicalCommit: Policy.OwnerApproved,
            allowTestTransportUpload: false,
            allowCanonicalUploadWithLegacyFallback: Policy.DebugInternalBuild
                && Policy.OwnerApproved
                && !Policy.ReleaseDefaultBuild
                && Policy.ManualFullSyncConfirmation,
            legacyFallbackEnabled: Policy.LegacyFallbackAvailable,
            requireExistingSecureUploadRoutes: true,
            retryDrainerRequiresExistingRetry: true
        );
    }

    private CanonicalReadRuntimePolicy CanonicalReadPolicy()
    {
        return new CanonicalReadRuntimePolicy(
            debugInternalBuild: Policy.DebugInternalBuild,
            ownerApproved: Policy.OwnerApproved,
            manualOwnerApproval: Policy.ManualFullSyncConfirmation,
            releaseDefaultBuild: Policy.ReleaseDefaultBuild,
            legacyFallbackAvailable: Policy.LegacyFallbackAvailable,
            diagnosticsRedacted: Policy.DiagnosticsRedacted,
            applyRuntimeEvidenceValidForNonAudio: true,
            uploadRuntimeEvidenceValidForAudioStatus: true,
            inventorySnapshotAvailable: true,
            planAuthorityEvidenceValid: true,
            existenceTruthEvidenceValid: true,
            otherDomainsNotConflicting: true,
            readMustNotTriggerSyncUpload: true,
            readMustNotMutateStore: true
        );
    }

    private List<CanonicalKernelSwitchBlocker> AdvancedOverrideBlockers(
        CanonicalKernelSwitchEffectiveConfiguration baseConfig)
    {
        var blockers = new List<CanonicalKernelSwitchBlocker>();
        if (AdvancedOverrides.SyncRuntimeConfiguration != null &&
            AdvancedOverrides.SyncRuntimeConfiguration.Mode != baseConfig.SyncRuntimeConfiguration.Mode)
            blockers.Add(CanonicalKernelSwitchBlocker.advancedOverrideContradictsMasterSwitch);
        if (AdvancedOverrides.ApplyRuntimeConfiguration != null &&
            AdvancedOverrides.ApplyRuntimeConfiguration.Mode != baseConfig.ApplyRuntimeConfiguration.Mode)
            blockers.Add(CanonicalKernelSwitchBlocker.advancedOverrideContradictsMasterSwitch);
        if (AdvancedOverrides.ExistenceApplyRuntimeConfiguration != null &&
            AdvancedOverrides.ExistenceApplyRuntimeConfiguration.Mode != baseConfig.ExistenceApplyRuntimeConfiguration.Mode)
            blockers.Add(CanonicalKernelSwitchBlocker.advancedOverrideContradictsMasterSwitch);
        if (AdvancedOverrides.AudioUploadRuntimeConfiguration != null &&
            AdvancedOverrides.AudioUploadRuntimeConfiguration.Mode != baseConfig.AudioUploadRuntimeConfiguration.Mode)
            blockers.Add(CanonicalKernelSwitchBlocker.advancedOverrideContradictsMasterSwitch);
        if (AdvancedOverrides.ReadRuntimeConfiguration != null &&
            AdvancedOverrides.ReadRuntimeConfiguration.Mode != baseConfig.ReadRuntimeConfiguration.Mode)
            blockers.Add(CanonicalKernelSwitchBlocker.advancedOverrideContradictsMasterSwitch);
        if (AdvancedOverrides.LibraryMetadataDebugPilotConfiguration != null &&
            AdvancedOverrides.LibraryMetadataDebugPilotConfiguration.Mode != baseConfig.LibraryMetadataDebugPilotConfiguration.Mode)
            blockers.Add(CanonicalKernelSwitchBlocker.advancedOverrideContradictsMasterSwitch);
        return blockers;
    }

    private CanonicalKernelSwitchOwnerState OwnerStateFor(CanonicalKernelSwitchMode mode)
    {
        return mode switch
        {
            CanonicalKernelSwitchMode.oldKernel => CanonicalKernelSwitchOwnerState.oldKernel,
            CanonicalKernelSwitchMode.diagnosticsOnly => CanonicalKernelSwitchOwnerState.canonicalNoWrite,
            CanonicalKernelSwitchMode.canonicalDecisionOnly => CanonicalKernelSwitchOwnerState.canonicalNoWrite,
            CanonicalKernelSwitchMode.canonicalShadow => CanonicalKernelSwitchOwnerState.shadow,
            CanonicalKernelSwitchMode.canonicalApplyNoAudio => CanonicalKernelSwitchOwnerState.canonicalReadWrite,
            CanonicalKernelSwitchMode.canonicalFullSync => CanonicalKernelSwitchOwnerState.canonicalReadWrite,
            _ => CanonicalKernelSwitchOwnerState.blocked
        };
    }

    private static List<CanonicalKernelSwitchBlocker> Unique(List<CanonicalKernelSwitchBlocker> blockers)
    {
        var seen = new HashSet<CanonicalKernelSwitchBlocker>();
        var unique = new List<CanonicalKernelSwitchBlocker>();
        foreach (var blocker in blockers)
        {
            if (!seen.Contains(blocker))
            {
                seen.Add(blocker);
                unique.Add(blocker);
            }
        }
        return unique;
    }

    private static string DiagnosticsSummary(
        CanonicalKernelSwitchMode requestedMode,
        CanonicalKernelSwitchMode effectiveMode,
        CanonicalKernelSwitchOwnerState ownerState,
        CanonicalKernelSwitchEffectiveConfiguration configuration,
        List<CanonicalKernelSwitchBlocker> blockers)
    {
        return string.Join(",",
            "canonicalKernelSwitch=v8.43",
            $"requested={requestedMode}",
            $"effective={effectiveMode}",
            $"ownerState={ownerState}",
            $"sync={configuration.SyncRuntimeConfiguration.Mode}",
            $"apply={configuration.ApplyRuntimeConfiguration.Mode}",
            $"existence={configuration.ExistenceApplyRuntimeConfiguration.Mode}",
            $"audio={configuration.AudioUploadRuntimeConfiguration.Mode}",
            $"read={configuration.ReadRuntimeConfiguration.Mode}",
            $"libraryMetadataPilot={configuration.LibraryMetadataDebugPilotConfiguration.Mode}",
            $"diskFormat={configuration.MigrationMatrixPolicy.DiskFormatPolicy}",
            $"switchBackMigration={configuration.MigrationMatrixPolicy.MigrationRequiredToSwitchBack}",
            $"blockers={string.Join("|", blockers.Select(b => b.ToString()))}",
            "redacted=true"
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelSwitchConfiguration other && Equals(other);
    public bool Equals(CanonicalKernelSwitchConfiguration? other) =>
        other is not null &&
        Mode == other.Mode &&
        Policy.Equals(other.Policy) &&
        AdvancedOverrides.Equals(other.AdvancedOverrides);
    public override int GetHashCode() => HashCode.Combine(Mode, Policy, AdvancedOverrides);
    public static bool operator ==(CanonicalKernelSwitchConfiguration l, CanonicalKernelSwitchConfiguration r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelSwitchConfiguration l, CanonicalKernelSwitchConfiguration r) => !l.Equals(r);
}
