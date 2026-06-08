package com.rokurics.app.domain.canonical

data class CanonicalInventoryRuntimeConfiguration(
    val mode: CanonicalInventoryRuntimeMode = CanonicalInventoryRuntimeMode.DIRECT_BUILD,
    val checksumSchemaVersion: Int = 1,
    val hashAlgorithm: String = "sha256",
    val cacheFileName: String = "canonical-checksum-cache-v1.json",
    val redactedDiagnostics: Boolean = true,
    val persistentChecksumCacheEnabled: Boolean = true
) {
    companion object {
        const val CURRENT_SCHEMA_VERSION = 1

        val DISABLED = CanonicalInventoryRuntimeConfiguration(
            mode = CanonicalInventoryRuntimeMode.DISABLED,
            persistentChecksumCacheEnabled = false
        )

        val BLOCKED = CanonicalInventoryRuntimeConfiguration(
            mode = CanonicalInventoryRuntimeMode.BLOCKED,
            persistentChecksumCacheEnabled = false
        )

        val DIRECT_BUILD = CanonicalInventoryRuntimeConfiguration(
            mode = CanonicalInventoryRuntimeMode.DIRECT_BUILD
        )

        val CACHE_BACKED = CanonicalInventoryRuntimeConfiguration(
            mode = CanonicalInventoryRuntimeMode.CACHE_BACKED,
            persistentChecksumCacheEnabled = true
        )
    }

    val isEnabled: Boolean
        get() = mode != CanonicalInventoryRuntimeMode.DISABLED &&
                mode != CanonicalInventoryRuntimeMode.BLOCKED

    val canBuild: Boolean
        get() = mode == CanonicalInventoryRuntimeMode.DIRECT_BUILD ||
                mode == CanonicalInventoryRuntimeMode.CACHE_BACKED
}

enum class CanonicalKernelSwitchMode(val rawValue: String) {
    OLD_KERNEL("oldKernel"),
    DIAGNOSTICS_ONLY("diagnosticsOnly"),
    CANONICAL_SHADOW("canonicalShadow"),
    CANONICAL_DECISION_ONLY("canonicalDecisionOnly"),
    CANONICAL_APPLY_NO_AUDIO("canonicalApplyNoAudio"),
    CANONICAL_FULL_SYNC("canonicalFullSync"),
    BLOCKED("blocked");

    val displayTitle: String
        get() = when (this) {
            OLD_KERNEL -> "旧内核"
            DIAGNOSTICS_ONLY -> "诊断"
            CANONICAL_SHADOW -> "新内核影子"
            CANONICAL_DECISION_ONLY -> "新内核决策"
            CANONICAL_APPLY_NO_AUDIO -> "新内核写入不含音频"
            CANONICAL_FULL_SYNC -> "新内核完整同步"
            BLOCKED -> "已阻断"
        }
}

enum class CanonicalKernelSwitchOwnerState(val rawValue: String) {
    OLD_KERNEL("oldKernel"),
    SHADOW("shadow"),
    CANONICAL_NO_WRITE("canonicalNoWrite"),
    CANONICAL_READ_WRITE("canonicalReadWrite"),
    BLOCKED("blocked")
}

data class CanonicalKernelSwitchModeChoice(
    val rawValue: String,
    val title: String
) {
    constructor(mode: CanonicalKernelSwitchMode) : this(
        rawValue = mode.rawValue,
        title = mode.displayTitle
    )
}

data class CanonicalKernelSwitchPolicy(
    val debugInternalBuild: Boolean = false,
    val ownerApproved: Boolean = false,
    val releaseDefaultBuild: Boolean = true,
    val manualFullSyncConfirmation: Boolean = false,
    val legacyFallbackAvailable: Boolean = true,
    val diagnosticsRedacted: Boolean = true,
    val shadowComparisonEnabled: Boolean = true,
    val legacyReadPathAvailable: Boolean = true,
    val legacyWritePathAvailable: Boolean = true,
    val canonicalWritesLegacyReadable: Boolean = true,
    val noDataFormatMigrationRequired: Boolean = true,
    val canonicalOnlyRequiredFieldsHaveLegacyFallback: Boolean = true,
    val physicalMoveDeleteDisabled: Boolean = true,
    val secretPathHashLeakRedactionEnabled: Boolean = true,
    val shadowCompareAllowedDuringCanonicalOwner: Boolean = true
) {
    companion object {
        val RELEASE_DEFAULT = CanonicalKernelSwitchPolicy()

        fun debugInternal(
            ownerApproved: Boolean = true,
            manualFullSyncConfirmation: Boolean = false
        ): CanonicalKernelSwitchPolicy {
            return CanonicalKernelSwitchPolicy(
                debugInternalBuild = true,
                ownerApproved = ownerApproved,
                releaseDefaultBuild = false,
                manualFullSyncConfirmation = manualFullSyncConfirmation
            )
        }
    }
}

data class CanonicalKernelSwitchAdvancedOverrides(
    val syncRuntimeConfiguration: CanonicalSyncRuntimeConfiguration? = null,
    val applyRuntimeConfiguration: CanonicalApplyRuntimeConfiguration? = null,
    val existenceApplyRuntimeConfiguration: CanonicalExistenceApplyRuntimeConfiguration? = null,
    val audioUploadRuntimeConfiguration: CanonicalAudioUploadRuntimeConfiguration? = null,
    val readRuntimeConfiguration: CanonicalReadRuntimeConfiguration? = null,
    val libraryMetadataDebugPilotConfiguration: CanonicalLibraryMetadataDebugPilotConfiguration? = null
) {
    companion object {
        val NONE = CanonicalKernelSwitchAdvancedOverrides()
    }
}

enum class CanonicalKernelSwitchBlocker(val rawValue: String) {
    EXPLICIT_BLOCKED_MODE("explicitBlockedMode"),
    RELEASE_DEFAULT_CANNOT_USE_CANONICAL_FULL_SYNC("releaseDefaultCannotUseCanonicalFullSync"),
    CANONICAL_FULL_SYNC_REQUIRES_DEBUG_INTERNAL_BUILD("canonicalFullSyncRequiresDebugInternalBuild"),
    CANONICAL_FULL_SYNC_REQUIRES_OWNER_APPROVAL("canonicalFullSyncRequiresOwnerApproval"),
    CANONICAL_FULL_SYNC_REQUIRES_MANUAL_CONFIRMATION("canonicalFullSyncRequiresManualConfirmation"),
    LEGACY_FALLBACK_UNAVAILABLE("legacyFallbackUnavailable"),
    DIAGNOSTICS_NOT_REDACTED("diagnosticsNotRedacted"),
    LEGACY_READ_PATH_UNAVAILABLE("legacyReadPathUnavailable"),
    LEGACY_WRITE_PATH_UNAVAILABLE("legacyWritePathUnavailable"),
    CANONICAL_WRITES_NOT_LEGACY_READABLE("canonicalWritesNotLegacyReadable"),
    SWITCH_BACK_WOULD_REQUIRE_DATA_FORMAT_MIGRATION("switchBackWouldRequireDataFormatMigration"),
    CANONICAL_ONLY_REQUIRED_FIELD_WITHOUT_LEGACY_FALLBACK("canonicalOnlyRequiredFieldWithoutLegacyFallback"),
    PHYSICAL_MOVE_OR_DELETE_WOULD_BE_REQUIRED("physicalMoveOrDeleteWouldBeRequired"),
    SECRET_PATH_HASH_LEAK_RISK("secretPathHashLeakRisk"),
    SHADOW_COMPARE_CANNOT_STAY_ENABLED_WITH_CANONICAL_OWNER("shadowCompareCannotStayEnabledWithCanonicalOwner"),
    ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH("advancedOverrideContradictsMasterSwitch")
}

data class CanonicalKernelSwitchReversibilityProof(
    val legacyReadPathStillExists: Boolean,
    val legacyWritePathStillExists: Boolean,
    val canonicalWritesAreLegacyReadable: Boolean,
    val noDataFormatMigrationRequiredToSwitchBack: Boolean,
    val noCanonicalOnlyRequiredFieldWithoutLegacyFallback: Boolean,
    val noPhysicalMoveOrDeleteRequired: Boolean,
    val secretPathHashLeakRedactionEnabled: Boolean,
    val shadowCompareCanStayOnWhileCanonicalOwnerActive: Boolean,
    val requiresDataMigrationToSwitchBack: Boolean,
    val blockers: List<CanonicalKernelSwitchBlocker>
) {
    val isReversible: Boolean
        get() = blockers.isEmpty() && !requiresDataMigrationToSwitchBack
}

object CanonicalKernelSwitchReversibilityGate {
    fun prove(policy: CanonicalKernelSwitchPolicy): CanonicalKernelSwitchReversibilityProof {
        val blockers = mutableListOf<CanonicalKernelSwitchBlocker>()
        if (!policy.legacyReadPathAvailable) {
            blockers.add(CanonicalKernelSwitchBlocker.LEGACY_READ_PATH_UNAVAILABLE)
        }
        if (!policy.legacyWritePathAvailable) {
            blockers.add(CanonicalKernelSwitchBlocker.LEGACY_WRITE_PATH_UNAVAILABLE)
        }
        if (!policy.canonicalWritesLegacyReadable) {
            blockers.add(CanonicalKernelSwitchBlocker.CANONICAL_WRITES_NOT_LEGACY_READABLE)
        }
        if (!policy.noDataFormatMigrationRequired) {
            blockers.add(CanonicalKernelSwitchBlocker.SWITCH_BACK_WOULD_REQUIRE_DATA_FORMAT_MIGRATION)
        }
        if (!policy.canonicalOnlyRequiredFieldsHaveLegacyFallback) {
            blockers.add(CanonicalKernelSwitchBlocker.CANONICAL_ONLY_REQUIRED_FIELD_WITHOUT_LEGACY_FALLBACK)
        }
        if (!policy.physicalMoveDeleteDisabled) {
            blockers.add(CanonicalKernelSwitchBlocker.PHYSICAL_MOVE_OR_DELETE_WOULD_BE_REQUIRED)
        }
        if (!policy.secretPathHashLeakRedactionEnabled) {
            blockers.add(CanonicalKernelSwitchBlocker.SECRET_PATH_HASH_LEAK_RISK)
        }
        if (!policy.shadowCompareAllowedDuringCanonicalOwner) {
            blockers.add(CanonicalKernelSwitchBlocker.SHADOW_COMPARE_CANNOT_STAY_ENABLED_WITH_CANONICAL_OWNER)
        }

        return CanonicalKernelSwitchReversibilityProof(
            legacyReadPathStillExists = policy.legacyReadPathAvailable,
            legacyWritePathStillExists = policy.legacyWritePathAvailable,
            canonicalWritesAreLegacyReadable = policy.canonicalWritesLegacyReadable,
            noDataFormatMigrationRequiredToSwitchBack = policy.noDataFormatMigrationRequired,
            noCanonicalOnlyRequiredFieldWithoutLegacyFallback = policy.canonicalOnlyRequiredFieldsHaveLegacyFallback,
            noPhysicalMoveOrDeleteRequired = policy.physicalMoveDeleteDisabled,
            secretPathHashLeakRedactionEnabled = policy.secretPathHashLeakRedactionEnabled,
            shadowCompareCanStayOnWhileCanonicalOwnerActive = policy.shadowCompareAllowedDuringCanonicalOwner,
            requiresDataMigrationToSwitchBack = !policy.noDataFormatMigrationRequired,
            blockers = blockers
        )
    }
}

data class CanonicalKernelSwitchMigrationMatrixPolicy(
    val mode: CanonicalKernelSwitchMode,
    val ownerState: CanonicalKernelSwitchOwnerState,
    val activeCanonicalOwnershipDomains: List<CanonicalMigrationDomain>,
    val legacyReadPathRetained: Boolean,
    val legacyWritePathRetained: Boolean,
    val migrationRequiredToSwitchBack: Boolean,
    val diskFormatPolicy: String,
    val diagnosticsRedacted: Boolean
) {
    companion object {
        fun make(
            mode: CanonicalKernelSwitchMode,
            ownerState: CanonicalKernelSwitchOwnerState,
            activeCanonicalOwnershipDomains: List<CanonicalMigrationDomain>,
            policy: CanonicalKernelSwitchPolicy,
            proof: CanonicalKernelSwitchReversibilityProof
        ): CanonicalKernelSwitchMigrationMatrixPolicy {
            return CanonicalKernelSwitchMigrationMatrixPolicy(
                mode = mode,
                ownerState = ownerState,
                activeCanonicalOwnershipDomains = activeCanonicalOwnershipDomains.sortedBy { it.rawValue },
                legacyReadPathRetained = policy.legacyReadPathAvailable,
                legacyWritePathRetained = policy.legacyWritePathAvailable,
                migrationRequiredToSwitchBack = proof.requiresDataMigrationToSwitchBack,
                diskFormatPolicy = "legacy-readable-or-dual-write-compatible",
                diagnosticsRedacted = policy.diagnosticsRedacted
            )
        }
    }
}

data class CanonicalKernelSwitchEffectiveConfiguration(
    val inventoryRuntimeConfiguration: CanonicalInventoryRuntimeConfiguration,
    val syncRuntimeConfiguration: CanonicalSyncRuntimeConfiguration,
    val applyRuntimeConfiguration: CanonicalApplyRuntimeConfiguration,
    val existenceApplyRuntimeConfiguration: CanonicalExistenceApplyRuntimeConfiguration,
    val audioUploadRuntimeConfiguration: CanonicalAudioUploadRuntimeConfiguration,
    val readRuntimeConfiguration: CanonicalReadRuntimeConfiguration,
    val libraryMetadataDebugPilotConfiguration: CanonicalLibraryMetadataDebugPilotConfiguration,
    val migrationMatrixPolicy: CanonicalKernelSwitchMigrationMatrixPolicy
) {
    companion object {
        fun blocked(
            policy: CanonicalKernelSwitchPolicy,
            proof: CanonicalKernelSwitchReversibilityProof
        ): CanonicalKernelSwitchEffectiveConfiguration {
            return CanonicalKernelSwitchEffectiveConfiguration(
                inventoryRuntimeConfiguration = CanonicalInventoryRuntimeConfiguration(
                    redactedDiagnostics = policy.diagnosticsRedacted
                ),
                syncRuntimeConfiguration = CanonicalSyncRuntimeConfiguration(
                    mode = CanonicalSyncRuntimeMode.BLOCKED,
                    policy = CanonicalSyncRuntimePolicy()
                ),
                applyRuntimeConfiguration = CanonicalApplyRuntimeConfiguration(
                    mode = CanonicalApplyRuntimeMode.BLOCKED,
                    policy = CanonicalApplyRuntimePolicy()
                ),
                existenceApplyRuntimeConfiguration = CanonicalExistenceApplyRuntimeConfiguration(
                    mode = CanonicalExistenceApplyRuntimeMode.BLOCKED,
                    policy = CanonicalExistenceApplyRuntimePolicy()
                ),
                audioUploadRuntimeConfiguration = CanonicalAudioUploadRuntimeConfiguration(
                    mode = CanonicalAudioUploadRuntimeMode.blocked,
                    policy = CanonicalAudioUploadRuntimePolicy()
                ),
                readRuntimeConfiguration = CanonicalReadRuntimeConfiguration(
                    mode = CanonicalReadRuntimeMode.BLOCKED,
                    policy = CanonicalReadRuntimePolicy()
                ),
                libraryMetadataDebugPilotConfiguration = CanonicalLibraryMetadataDebugPilotConfiguration(
                    mode = CanonicalLibraryMetadataDebugPilotMode.DISABLED
                ),
                migrationMatrixPolicy = CanonicalKernelSwitchMigrationMatrixPolicy.make(
                    mode = CanonicalKernelSwitchMode.BLOCKED,
                    ownerState = CanonicalKernelSwitchOwnerState.BLOCKED,
                    activeCanonicalOwnershipDomains = emptyList(),
                    policy = policy,
                    proof = proof
                )
            )
        }
    }
}

data class CanonicalKernelSwitchResult(
    val requestedMode: CanonicalKernelSwitchMode,
    val effectiveMode: CanonicalKernelSwitchMode,
    val ownerState: CanonicalKernelSwitchOwnerState,
    val blockers: List<CanonicalKernelSwitchBlocker>,
    val effectiveConfiguration: CanonicalKernelSwitchEffectiveConfiguration,
    val effectiveSyncConfig: CanonicalSyncRuntimeConfiguration,
    val effectiveApplyConfig: CanonicalApplyRuntimeConfiguration?,
    val effectiveExistenceConfig: CanonicalExistenceApplyRuntimeConfiguration?,
    val reversibilityProof: CanonicalKernelSwitchReversibilityProof,
    val diagnosticsSummary: String,
    val redacted: Boolean,
    val isDefault: Boolean,
    val isReleaseDefault: Boolean,
    val reversible: Boolean,
    val diagnostics: CanonicalKernelSwitchDiagnosticSummary
) {
    val isBlocked: Boolean
        get() = effectiveMode == CanonicalKernelSwitchMode.BLOCKED || blockers.isNotEmpty()
}

data class CanonicalKernelSwitchDiagnosticSummary(
    val requestedMode: String,
    val effectiveMode: String,
    val ownerState: String,
    val syncMode: String,
    val applyMode: String,
    val existenceMode: String,
    val audioMode: String,
    val readMode: String,
    val libraryMetadataPilotMode: String,
    val diskFormat: String,
    val switchBackMigration: Boolean,
    val blockers: List<String>,
    val redacted: Boolean
) {
    val summary: String
        get() = listOf(
            "canonicalKernelSwitch=v8.43",
            "requested=$requestedMode",
            "effective=$effectiveMode",
            "ownerState=$ownerState",
            "sync=$syncMode",
            "apply=$applyMode",
            "existence=$existenceMode",
            "audio=$audioMode",
            "read=$readMode",
            "libraryMetadataPilot=$libraryMetadataPilotMode",
            "diskFormat=$diskFormat",
            "switchBackMigration=$switchBackMigration",
            "blockers=${blockers.joinToString("|")}",
            "redacted=$redacted"
        ).joinToString(",")
}

object CanonicalKernelSwitchSettingsPersistenceKey {
    const val DEBUG_MODE_KEY = "Rokurics.debug.canonicalKernelSwitch.mode"
    const val DEBUG_FULL_SYNC_CONFIRMED_KEY = "Rokurics.debug.canonicalKernelSwitch.fullSyncConfirmed"
    const val DIAGNOSTICS_PATH_TEXT = "Application Support/Rokurics/Diagnostics/canonical-kernel-switch.log"
}

data class CanonicalKernelSwitchConfiguration(
    val mode: CanonicalKernelSwitchMode = CanonicalKernelSwitchMode.OLD_KERNEL,
    val policy: CanonicalKernelSwitchPolicy = CanonicalKernelSwitchPolicy.RELEASE_DEFAULT,
    val advancedOverrides: CanonicalKernelSwitchAdvancedOverrides = CanonicalKernelSwitchAdvancedOverrides.NONE
) {
    companion object {
        val DEFAULT = CanonicalKernelSwitchConfiguration()
        val OLD_KERNEL = CanonicalKernelSwitchConfiguration()

        val debugModeChoices: List<CanonicalKernelSwitchModeChoice>
            get() = listOf(
                CanonicalKernelSwitchMode.OLD_KERNEL,
                CanonicalKernelSwitchMode.DIAGNOSTICS_ONLY,
                CanonicalKernelSwitchMode.CANONICAL_SHADOW,
                CanonicalKernelSwitchMode.CANONICAL_DECISION_ONLY,
                CanonicalKernelSwitchMode.CANONICAL_APPLY_NO_AUDIO,
                CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC
            ).map { CanonicalKernelSwitchModeChoice(it) }

        fun normalizedDebugMode(rawValue: String): String {
            return CanonicalKernelSwitchMode.entries
                .firstOrNull { it.rawValue == rawValue }?.rawValue
                ?: CanonicalKernelSwitchMode.OLD_KERNEL.rawValue
        }

        fun debugStoredConfiguration(
            storedModeRaw: String = CanonicalKernelSwitchMode.OLD_KERNEL.rawValue,
            fullSyncConfirmed: Boolean = false
        ): CanonicalKernelSwitchConfiguration {
            val storedMode = normalizedDebugMode(storedModeRaw)
            val mode = CanonicalKernelSwitchMode.entries
                .firstOrNull { it.rawValue == storedMode }
                ?: CanonicalKernelSwitchMode.OLD_KERNEL
            return CanonicalKernelSwitchConfiguration(
                mode = mode,
                policy = CanonicalKernelSwitchPolicy.debugInternal(
                    ownerApproved = true,
                    manualFullSyncConfirmation = fullSyncConfirmed
                )
            )
        }

        fun runtimeConfigurationFromStoredDefaults(
            storedModeRaw: String = CanonicalKernelSwitchMode.OLD_KERNEL.rawValue,
            fullSyncConfirmed: Boolean = false,
            isDebugBuild: Boolean = false
        ): CanonicalKernelSwitchConfiguration {
            return if (isDebugBuild) {
                debugStoredConfiguration(storedModeRaw, fullSyncConfirmed)
            } else {
                OLD_KERNEL
            }
        }

        fun setDebugStoredMode(
            rawValue: String,
            onStore: (key: String, value: String) -> Unit,
            onNotify: () -> Unit = {}
        ) {
            val normalized = normalizedDebugMode(rawValue)
            onStore(CanonicalKernelSwitchSettingsPersistenceKey.DEBUG_MODE_KEY, normalized)
            if (normalized != CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC.rawValue) {
                onStore(CanonicalKernelSwitchSettingsPersistenceKey.DEBUG_FULL_SYNC_CONFIRMED_KEY, "false")
            }
            onNotify()
        }

        fun setDebugFullSyncConfirmed(
            confirmed: Boolean,
            onStore: (key: String, value: String) -> Unit,
            onNotify: () -> Unit = {}
        ) {
            onStore(
                CanonicalKernelSwitchSettingsPersistenceKey.DEBUG_FULL_SYNC_CONFIRMED_KEY,
                confirmed.toString()
            )
            onNotify()
        }

        private fun canonicalSyncPolicy(policy: CanonicalKernelSwitchPolicy): CanonicalSyncRuntimePolicy {
            return CanonicalSyncRuntimePolicy(
                debugInternalBuild = policy.debugInternalBuild,
                ownerApproved = policy.ownerApproved,
                releaseDefaultBuild = policy.releaseDefaultBuild,
                legacyFallbackAvailable = policy.legacyFallbackAvailable,
                diagnosticsRedacted = policy.diagnosticsRedacted,
                runtimeSwitchEnabled = false,
                readPathLegacy = true,
                otherActiveMigrationDomainConflicting = false,
                allowDocumentedModifiedAtFallback = true,
                enabledScopes = CanonicalSyncRuntimeDecisionScope.entries
            )
        }

        private fun canonicalApplyPolicy(policy: CanonicalKernelSwitchPolicy): CanonicalApplyRuntimePolicy {
            return CanonicalApplyRuntimePolicy(
                debugInternalBuild = policy.debugInternalBuild,
                ownerApproved = policy.ownerApproved,
                releaseDefaultBuild = policy.releaseDefaultBuild,
                legacyFallbackAvailable = policy.legacyFallbackAvailable,
                diagnosticsRedacted = policy.diagnosticsRedacted,
                runtimeSwitchEnabled = false,
                readPathLegacy = true,
                rootBoundRequired = true,
                rollbackRequired = true,
                postconditionRequired = true,
                enabledDomains = setOf(
                    CanonicalApplyRuntimeDomain.RECORDING_METADATA,
                    CanonicalApplyRuntimeDomain.LIBRARY_METADATA,
                    CanonicalApplyRuntimeDomain.GENERATED_ARTIFACTS,
                    CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT
                )
            )
        }

        private fun canonicalExistencePolicy(policy: CanonicalKernelSwitchPolicy): CanonicalExistenceApplyRuntimePolicy {
            return CanonicalExistenceApplyRuntimePolicy(
                debugInternalBuild = policy.debugInternalBuild,
                ownerApproved = policy.ownerApproved,
                releaseDefaultBuild = policy.releaseDefaultBuild,
                diagnosticsRedacted = policy.diagnosticsRedacted,
                legacyFallbackAvailable = policy.legacyFallbackAvailable,
                rootBoundRequired = true,
                rollbackRequired = true,
                atomicWriteRequired = true,
                postconditionRequired = true,
                writeAudioAllowed = false,
                markAudioAvailableAllowed = false
            )
        }

        private fun canonicalAudioPolicy(policy: CanonicalKernelSwitchPolicy): CanonicalAudioUploadRuntimePolicy {
            return CanonicalAudioUploadRuntimePolicy(
                debugInternalBuild = policy.debugInternalBuild,
                ownerApproved = policy.ownerApproved,
                releaseDefaultBuild = policy.releaseDefaultBuild,
                diagnosticsRedacted = policy.diagnosticsRedacted,
                legacyFallbackAvailable = policy.legacyFallbackAvailable,
                existingSecureUploadPort = true
            )
        }

        private fun canonicalReadPolicy(policy: CanonicalKernelSwitchPolicy): CanonicalReadRuntimePolicy {
            return CanonicalReadRuntimePolicy(
                debugInternalBuild = policy.debugInternalBuild,
                ownerApproved = policy.ownerApproved,
                manualOwnerApproval = policy.manualFullSyncConfirmation,
                releaseDefaultBuild = policy.releaseDefaultBuild,
                legacyFallbackAvailable = policy.legacyFallbackAvailable,
                diagnosticsRedacted = policy.diagnosticsRedacted,
                applyRuntimeEvidenceValidForNonAudio = true,
                uploadRuntimeEvidenceValidForAudioStatus = true,
                inventorySnapshotAvailable = true,
                planAuthorityEvidenceValid = true,
                existenceTruthEvidenceValid = true,
                otherDomainsNotConflicting = true,
                readMustNotTriggerSyncUpload = true,
                readMustNotMutateStore = true
            )
        }

        private fun ownerStateFor(mode: CanonicalKernelSwitchMode): CanonicalKernelSwitchOwnerState {
            return when (mode) {
                CanonicalKernelSwitchMode.OLD_KERNEL -> CanonicalKernelSwitchOwnerState.OLD_KERNEL
                CanonicalKernelSwitchMode.DIAGNOSTICS_ONLY,
                CanonicalKernelSwitchMode.CANONICAL_DECISION_ONLY -> CanonicalKernelSwitchOwnerState.CANONICAL_NO_WRITE
                CanonicalKernelSwitchMode.CANONICAL_SHADOW -> CanonicalKernelSwitchOwnerState.SHADOW
                CanonicalKernelSwitchMode.CANONICAL_APPLY_NO_AUDIO,
                CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC -> CanonicalKernelSwitchOwnerState.CANONICAL_READ_WRITE
                CanonicalKernelSwitchMode.BLOCKED -> CanonicalKernelSwitchOwnerState.BLOCKED
            }
        }

        private fun makeEffectiveConfiguration(
            mode: CanonicalKernelSwitchMode,
            policy: CanonicalKernelSwitchPolicy,
            proof: CanonicalKernelSwitchReversibilityProof,
            advancedOverrides: CanonicalKernelSwitchAdvancedOverrides,
            applyingOverrides: Boolean
        ): CanonicalKernelSwitchEffectiveConfiguration {
            val inventory = CanonicalInventoryRuntimeConfiguration(redactedDiagnostics = policy.diagnosticsRedacted)
            val syncPolicy = canonicalSyncPolicy(policy)
            val applyPolicy = canonicalApplyPolicy(policy)
            val existencePolicy = canonicalExistencePolicy(policy)
            val audioPolicy = canonicalAudioPolicy(policy)
            val readPolicy = canonicalReadPolicy(policy)

            val sync: CanonicalSyncRuntimeConfiguration
            val apply: CanonicalApplyRuntimeConfiguration
            val existence: CanonicalExistenceApplyRuntimeConfiguration
            val audio: CanonicalAudioUploadRuntimeConfiguration
            val read: CanonicalReadRuntimeConfiguration
            val libraryPilot: CanonicalLibraryMetadataDebugPilotConfiguration
            val activeDomains: List<CanonicalMigrationDomain>

            when (mode) {
                CanonicalKernelSwitchMode.OLD_KERNEL -> {
                    sync = CanonicalSyncRuntimeConfiguration.DISABLED
                    apply = CanonicalApplyRuntimeConfiguration.DISABLED
                    existence = CanonicalExistenceApplyRuntimeConfiguration.DISABLED
                    audio = CanonicalAudioUploadRuntimeConfiguration.DISABLED
                    read = CanonicalReadRuntimeConfiguration.DISABLED
                    libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
                    activeDomains = emptyList()
                }
                CanonicalKernelSwitchMode.DIAGNOSTICS_ONLY -> {
                    sync = CanonicalSyncRuntimeConfiguration(
                        mode = CanonicalSyncRuntimeMode.DIAGNOSTICS_ONLY,
                        policy = syncPolicy
                    )
                    apply = CanonicalApplyRuntimeConfiguration(
                        mode = CanonicalApplyRuntimeMode.DIAGNOSTICS_ONLY,
                        policy = applyPolicy
                    )
                    existence = CanonicalExistenceApplyRuntimeConfiguration(
                        mode = CanonicalExistenceApplyRuntimeMode.DIAGNOSTICS_ONLY,
                        policy = existencePolicy
                    )
                    audio = CanonicalAudioUploadRuntimeConfiguration(
                        mode = CanonicalAudioUploadRuntimeMode.diagnosticsOnly,
                        policy = audioPolicy
                    )
                    read = CanonicalReadRuntimeConfiguration.DISABLED
                    libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.diagnosticsOnly()
                    activeDomains = emptyList()
                }
                CanonicalKernelSwitchMode.CANONICAL_SHADOW -> {
                    sync = CanonicalSyncRuntimeConfiguration(
                        mode = CanonicalSyncRuntimeMode.CANONICAL_PLAN_NO_COMMIT,
                        policy = syncPolicy
                    )
                    apply = CanonicalApplyRuntimeConfiguration(
                        mode = CanonicalApplyRuntimeMode.NO_COMMIT,
                        policy = applyPolicy
                    )
                    existence = CanonicalExistenceApplyRuntimeConfiguration(
                        mode = CanonicalExistenceApplyRuntimeMode.NO_COMMIT,
                        policy = existencePolicy
                    )
                    audio = CanonicalAudioUploadRuntimeConfiguration(
                        mode = CanonicalAudioUploadRuntimeMode.noCommit,
                        policy = audioPolicy
                    )
                    read = CanonicalReadRuntimeConfiguration(
                        mode = if (policy.shadowComparisonEnabled) CanonicalReadRuntimeMode.PARALLEL_COMPARE
                        else CanonicalReadRuntimeMode.DISABLED,
                        policy = readPolicy
                    )
                    libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
                    activeDomains = emptyList()
                }
                CanonicalKernelSwitchMode.CANONICAL_DECISION_ONLY -> {
                    sync = CanonicalSyncRuntimeConfiguration(
                        mode = CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK,
                        policy = syncPolicy
                    )
                    apply = CanonicalApplyRuntimeConfiguration.DISABLED
                    existence = CanonicalExistenceApplyRuntimeConfiguration.DISABLED
                    audio = CanonicalAudioUploadRuntimeConfiguration.DISABLED
                    read = CanonicalReadRuntimeConfiguration.DISABLED
                    libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
                    activeDomains = emptyList()
                }
                CanonicalKernelSwitchMode.CANONICAL_APPLY_NO_AUDIO -> {
                    sync = CanonicalSyncRuntimeConfiguration(
                        mode = CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK,
                        policy = syncPolicy
                    )
                    apply = CanonicalApplyRuntimeConfiguration(
                        mode = CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK,
                        policy = applyPolicy
                    )
                    existence = CanonicalExistenceApplyRuntimeConfiguration(
                        mode = CanonicalExistenceApplyRuntimeMode.PRODUCTION_ROOT_APPLY,
                        policy = existencePolicy
                    )
                    audio = CanonicalAudioUploadRuntimeConfiguration.DISABLED
                    read = CanonicalReadRuntimeConfiguration.DISABLED
                    libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
                    activeDomains = listOf(
                        CanonicalMigrationDomain.RECORDING_METADATA,
                        CanonicalMigrationDomain.NOTE_ARTIFACT,
                        CanonicalMigrationDomain.LIBRARY_METADATA,
                        CanonicalMigrationDomain.TOMBSTONE
                    )
                }
                CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC -> {
                    sync = CanonicalSyncRuntimeConfiguration(
                        mode = CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK,
                        policy = syncPolicy
                    )
                    apply = CanonicalApplyRuntimeConfiguration(
                        mode = CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK,
                        policy = applyPolicy
                    )
                    existence = CanonicalExistenceApplyRuntimeConfiguration(
                        mode = CanonicalExistenceApplyRuntimeMode.PRODUCTION_ROOT_APPLY,
                        policy = existencePolicy
                    )
                    audio = CanonicalAudioUploadRuntimeConfiguration(
                        mode = CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback,
                        policy = audioPolicy
                    )
                    read = CanonicalReadRuntimeConfiguration(
                        mode = CanonicalReadRuntimeMode.GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK,
                        policy = readPolicy
                    )
                    libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
                    activeDomains = listOf(
                        CanonicalMigrationDomain.RECORDING_METADATA,
                        CanonicalMigrationDomain.NOTE_ARTIFACT,
                        CanonicalMigrationDomain.LIBRARY_METADATA,
                        CanonicalMigrationDomain.TOMBSTONE,
                        CanonicalMigrationDomain.AUDIO_ARTIFACT,
                        CanonicalMigrationDomain.READ_PROJECTION
                    )
                }
                CanonicalKernelSwitchMode.BLOCKED -> {
                    sync = CanonicalSyncRuntimeConfiguration(
                        mode = CanonicalSyncRuntimeMode.BLOCKED,
                        policy = CanonicalSyncRuntimePolicy()
                    )
                    apply = CanonicalApplyRuntimeConfiguration(
                        mode = CanonicalApplyRuntimeMode.BLOCKED,
                        policy = CanonicalApplyRuntimePolicy()
                    )
                    existence = CanonicalExistenceApplyRuntimeConfiguration(
                        mode = CanonicalExistenceApplyRuntimeMode.BLOCKED,
                        policy = CanonicalExistenceApplyRuntimePolicy()
                    )
                    audio = CanonicalAudioUploadRuntimeConfiguration(
                        mode = CanonicalAudioUploadRuntimeMode.blocked,
                        policy = CanonicalAudioUploadRuntimePolicy()
                    )
                    read = CanonicalReadRuntimeConfiguration(
                        mode = CanonicalReadRuntimeMode.BLOCKED,
                        policy = CanonicalReadRuntimePolicy()
                    )
                    libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration(mode = CanonicalLibraryMetadataDebugPilotMode.DISABLED)
                    activeDomains = emptyList()
                }
            }

            return CanonicalKernelSwitchEffectiveConfiguration(
                inventoryRuntimeConfiguration = inventory,
                syncRuntimeConfiguration = if (applyingOverrides)
                    (advancedOverrides.syncRuntimeConfiguration ?: sync) else sync,
                applyRuntimeConfiguration = if (applyingOverrides)
                    (advancedOverrides.applyRuntimeConfiguration ?: apply) else apply,
                existenceApplyRuntimeConfiguration = if (applyingOverrides)
                    (advancedOverrides.existenceApplyRuntimeConfiguration ?: existence) else existence,
                audioUploadRuntimeConfiguration = if (applyingOverrides)
                    (advancedOverrides.audioUploadRuntimeConfiguration ?: audio) else audio,
                readRuntimeConfiguration = if (applyingOverrides)
                    (advancedOverrides.readRuntimeConfiguration ?: read) else read,
                libraryMetadataDebugPilotConfiguration = if (applyingOverrides)
                    (advancedOverrides.libraryMetadataDebugPilotConfiguration ?: libraryPilot) else libraryPilot,
                migrationMatrixPolicy = CanonicalKernelSwitchMigrationMatrixPolicy.make(
                    mode = mode,
                    ownerState = ownerStateFor(mode),
                    activeCanonicalOwnershipDomains = activeDomains,
                    policy = policy,
                    proof = proof
                )
            )
        }

        private fun advancedOverrideBlockers(
            base: CanonicalKernelSwitchEffectiveConfiguration,
            advancedOverrides: CanonicalKernelSwitchAdvancedOverrides
        ): List<CanonicalKernelSwitchBlocker> {
            val blockers = mutableListOf<CanonicalKernelSwitchBlocker>()
            advancedOverrides.syncRuntimeConfiguration?.let { override ->
                if (override.mode != base.syncRuntimeConfiguration.mode) {
                    blockers.add(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
                }
            }
            advancedOverrides.applyRuntimeConfiguration?.let { override ->
                if (override.mode != base.applyRuntimeConfiguration.mode) {
                    blockers.add(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
                }
            }
            advancedOverrides.existenceApplyRuntimeConfiguration?.let { override ->
                if (override.mode != base.existenceApplyRuntimeConfiguration.mode) {
                    blockers.add(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
                }
            }
            advancedOverrides.audioUploadRuntimeConfiguration?.let { override ->
                if (override.mode != base.audioUploadRuntimeConfiguration.mode) {
                    blockers.add(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
                }
            }
            advancedOverrides.readRuntimeConfiguration?.let { override ->
                if (override.mode != base.readRuntimeConfiguration.mode) {
                    blockers.add(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
                }
            }
            advancedOverrides.libraryMetadataDebugPilotConfiguration?.let { override ->
                if (override.mode != base.libraryMetadataDebugPilotConfiguration.mode) {
                    blockers.add(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
                }
            }
            return blockers
        }

        private fun unique(blockers: List<CanonicalKernelSwitchBlocker>): List<CanonicalKernelSwitchBlocker> {
            val seen = mutableSetOf<CanonicalKernelSwitchBlocker>()
            val unique = mutableListOf<CanonicalKernelSwitchBlocker>()
            for (blocker in blockers) {
                if (blocker !in seen) {
                    seen.add(blocker)
                    unique.add(blocker)
                }
            }
            return unique
        }

        private fun diagnosticsSummary(
            requestedMode: CanonicalKernelSwitchMode,
            effectiveMode: CanonicalKernelSwitchMode,
            ownerState: CanonicalKernelSwitchOwnerState,
            configuration: CanonicalKernelSwitchEffectiveConfiguration,
            blockers: List<CanonicalKernelSwitchBlocker>
        ): String {
            return listOf(
                "canonicalKernelSwitch=v8.43",
                "requested=${requestedMode.rawValue}",
                "effective=${effectiveMode.rawValue}",
                "ownerState=${ownerState.rawValue}",
                "sync=${configuration.syncRuntimeConfiguration.mode.name}",
                "apply=${configuration.applyRuntimeConfiguration.mode.name}",
                "existence=${configuration.existenceApplyRuntimeConfiguration.mode.name}",
                "audio=${configuration.audioUploadRuntimeConfiguration.mode.name}",
                "read=${configuration.readRuntimeConfiguration.mode.name}",
                "libraryMetadataPilot=${configuration.libraryMetadataDebugPilotConfiguration.mode.name}",
                "diskFormat=${configuration.migrationMatrixPolicy.diskFormatPolicy}",
                "switchBackMigration=${configuration.migrationMatrixPolicy.migrationRequiredToSwitchBack}",
                "blockers=${blockers.joinToString("|") { it.rawValue }}",
                "redacted=true"
            ).joinToString(",")
        }

        private fun buildDiagnosticSummary(
            requestedMode: CanonicalKernelSwitchMode,
            effectiveMode: CanonicalKernelSwitchMode,
            ownerState: CanonicalKernelSwitchOwnerState,
            configuration: CanonicalKernelSwitchEffectiveConfiguration,
            blockers: List<CanonicalKernelSwitchBlocker>,
            redacted: Boolean
        ): CanonicalKernelSwitchDiagnosticSummary {
            return CanonicalKernelSwitchDiagnosticSummary(
                requestedMode = requestedMode.rawValue,
                effectiveMode = effectiveMode.rawValue,
                ownerState = ownerState.rawValue,
                syncMode = configuration.syncRuntimeConfiguration.mode.name,
                applyMode = configuration.applyRuntimeConfiguration.mode.name,
                existenceMode = configuration.existenceApplyRuntimeConfiguration.mode.name,
                audioMode = configuration.audioUploadRuntimeConfiguration.mode.name,
                readMode = configuration.readRuntimeConfiguration.mode.name,
                libraryMetadataPilotMode = configuration.libraryMetadataDebugPilotConfiguration.mode.name,
                diskFormat = configuration.migrationMatrixPolicy.diskFormatPolicy,
                switchBackMigration = configuration.migrationMatrixPolicy.migrationRequiredToSwitchBack,
                blockers = blockers.map { it.rawValue },
                redacted = redacted
            )
        }
    }

    fun resolve(
        reversibilityGate: CanonicalKernelSwitchReversibilityGate = CanonicalKernelSwitchReversibilityGate
    ): CanonicalKernelSwitchResult {
        val proof = reversibilityGate.prove(policy)
        val blockers = proof.blockers.toMutableList()

        if (mode == CanonicalKernelSwitchMode.BLOCKED) {
            blockers.add(CanonicalKernelSwitchBlocker.EXPLICIT_BLOCKED_MODE)
        }
        if (!policy.legacyFallbackAvailable) {
            blockers.add(CanonicalKernelSwitchBlocker.LEGACY_FALLBACK_UNAVAILABLE)
        }
        if (!policy.diagnosticsRedacted) {
            blockers.add(CanonicalKernelSwitchBlocker.DIAGNOSTICS_NOT_REDACTED)
        }
        if (mode == CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC) {
            if (policy.releaseDefaultBuild) {
                blockers.add(CanonicalKernelSwitchBlocker.RELEASE_DEFAULT_CANNOT_USE_CANONICAL_FULL_SYNC)
            }
            if (!policy.debugInternalBuild) {
                blockers.add(CanonicalKernelSwitchBlocker.CANONICAL_FULL_SYNC_REQUIRES_DEBUG_INTERNAL_BUILD)
            }
            if (!policy.ownerApproved) {
                blockers.add(CanonicalKernelSwitchBlocker.CANONICAL_FULL_SYNC_REQUIRES_OWNER_APPROVAL)
            }
            if (!policy.manualFullSyncConfirmation) {
                blockers.add(CanonicalKernelSwitchBlocker.CANONICAL_FULL_SYNC_REQUIRES_MANUAL_CONFIRMATION)
            }
        }
        if ((mode == CanonicalKernelSwitchMode.CANONICAL_SHADOW ||
                    mode == CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC) &&
            !policy.shadowComparisonEnabled
        ) {
            blockers.add(CanonicalKernelSwitchBlocker.SHADOW_COMPARE_CANNOT_STAY_ENABLED_WITH_CANONICAL_OWNER)
        }

        val base = Companion.makeEffectiveConfiguration(
            mode = mode,
            policy = policy,
            proof = proof,
            advancedOverrides = advancedOverrides,
            applyingOverrides = false
        )
        blockers.addAll(Companion.advancedOverrideBlockers(base, advancedOverrides))
        val effectiveConfiguration = Companion.makeEffectiveConfiguration(
            mode = mode,
            policy = policy,
            proof = proof,
            advancedOverrides = advancedOverrides,
            applyingOverrides = true
        )

        val uniqueBlockers = Companion.unique(blockers)
        if (uniqueBlockers.isNotEmpty()) {
            val blockedConfiguration = CanonicalKernelSwitchEffectiveConfiguration.blocked(policy, proof)
            return CanonicalKernelSwitchResult(
                requestedMode = mode,
                effectiveMode = CanonicalKernelSwitchMode.BLOCKED,
                ownerState = CanonicalKernelSwitchOwnerState.BLOCKED,
                blockers = uniqueBlockers,
                effectiveConfiguration = blockedConfiguration,
                effectiveSyncConfig = blockedConfiguration.syncRuntimeConfiguration,
                effectiveApplyConfig = blockedConfiguration.applyRuntimeConfiguration,
                effectiveExistenceConfig = blockedConfiguration.existenceApplyRuntimeConfiguration,
                reversibilityProof = proof,
                diagnosticsSummary = Companion.diagnosticsSummary(
                    requestedMode = mode,
                    effectiveMode = CanonicalKernelSwitchMode.BLOCKED,
                    ownerState = CanonicalKernelSwitchOwnerState.BLOCKED,
                    configuration = blockedConfiguration,
                    blockers = uniqueBlockers
                ),
                redacted = policy.diagnosticsRedacted,
                isDefault = false,
                isReleaseDefault = policy.releaseDefaultBuild,
                reversible = false,
                diagnostics = Companion.buildDiagnosticSummary(
                    requestedMode = mode,
                    effectiveMode = CanonicalKernelSwitchMode.BLOCKED,
                    ownerState = CanonicalKernelSwitchOwnerState.BLOCKED,
                    configuration = blockedConfiguration,
                    blockers = uniqueBlockers,
                    redacted = policy.diagnosticsRedacted
                )
            )
        }

        val ownerState = Companion.ownerStateFor(mode)
        return CanonicalKernelSwitchResult(
            requestedMode = mode,
            effectiveMode = mode,
            ownerState = ownerState,
            blockers = emptyList(),
            effectiveConfiguration = effectiveConfiguration,
            effectiveSyncConfig = effectiveConfiguration.syncRuntimeConfiguration,
            effectiveApplyConfig = effectiveConfiguration.applyRuntimeConfiguration,
            effectiveExistenceConfig = effectiveConfiguration.existenceApplyRuntimeConfiguration,
            reversibilityProof = proof,
            diagnosticsSummary = Companion.diagnosticsSummary(
                requestedMode = mode,
                effectiveMode = mode,
                ownerState = ownerState,
                configuration = effectiveConfiguration,
                blockers = emptyList()
            ),
            redacted = policy.diagnosticsRedacted,
            isDefault = mode == CanonicalKernelSwitchMode.OLD_KERNEL &&
                policy == CanonicalKernelSwitchPolicy.RELEASE_DEFAULT,
            isReleaseDefault = policy.releaseDefaultBuild,
            reversible = proof.isReversible,
            diagnostics = Companion.buildDiagnosticSummary(
                requestedMode = mode,
                effectiveMode = mode,
                ownerState = ownerState,
                configuration = effectiveConfiguration,
                blockers = emptyList(),
                redacted = policy.diagnosticsRedacted
            )
        )
    }
}
