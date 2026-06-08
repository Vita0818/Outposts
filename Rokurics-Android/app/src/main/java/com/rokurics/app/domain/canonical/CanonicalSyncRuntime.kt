package com.rokurics.app.domain.canonical

// ── Type 1: CanonicalSyncRuntimeDecisionScope ──

enum class CanonicalSyncRuntimeDecisionScope {
    RECORDING_METADATA,
    LIBRARY_METADATA,
    RECORDING_EXISTENCE
}

// ── Type 2: CanonicalSyncRuntimePolicy ──

data class CanonicalSyncRuntimePolicy(
    val debugInternalBuild: Boolean,
    val ownerApproved: Boolean,
    val releaseDefaultBuild: Boolean,
    val legacyFallbackAvailable: Boolean,
    val diagnosticsRedacted: Boolean,
    val runtimeSwitchEnabled: Boolean,
    val readPathLegacy: Boolean,
    val otherActiveMigrationDomainConflicting: Boolean,
    val allowDocumentedModifiedAtFallback: Boolean,
    val enabledScopes: List<CanonicalSyncRuntimeDecisionScope>
) {
    companion object {
        operator fun invoke(
            debugInternalBuild: Boolean = false,
            ownerApproved: Boolean = false,
            releaseDefaultBuild: Boolean = true,
            legacyFallbackAvailable: Boolean = true,
            diagnosticsRedacted: Boolean = true,
            runtimeSwitchEnabled: Boolean = false,
            readPathLegacy: Boolean = true,
            otherActiveMigrationDomainConflicting: Boolean = false,
            allowDocumentedModifiedAtFallback: Boolean = false,
            enabledScopes: List<CanonicalSyncRuntimeDecisionScope> = CanonicalSyncRuntimeDecisionScope.entries
        ): CanonicalSyncRuntimePolicy {
            return CanonicalSyncRuntimePolicy(
                debugInternalBuild = debugInternalBuild,
                ownerApproved = ownerApproved,
                releaseDefaultBuild = releaseDefaultBuild,
                legacyFallbackAvailable = legacyFallbackAvailable,
                diagnosticsRedacted = diagnosticsRedacted,
                runtimeSwitchEnabled = runtimeSwitchEnabled,
                readPathLegacy = readPathLegacy,
                otherActiveMigrationDomainConflicting = otherActiveMigrationDomainConflicting,
                allowDocumentedModifiedAtFallback = allowDocumentedModifiedAtFallback,
                enabledScopes = enabledScopes.toSet().sortedBy { it.name }
            )
        }
    }
}

// ── Type 3: CanonicalSyncRuntimeConfiguration ──

data class CanonicalSyncRuntimeConfiguration(
    val mode: CanonicalSyncRuntimeMode,
    val policy: CanonicalSyncRuntimePolicy
) {
    companion object {
        val DISABLED = CanonicalSyncRuntimeConfiguration(
            mode = CanonicalSyncRuntimeMode.DISABLED,
            policy = CanonicalSyncRuntimePolicy()
        )

        operator fun invoke(
            mode: CanonicalSyncRuntimeMode = CanonicalSyncRuntimeMode.DISABLED,
            policy: CanonicalSyncRuntimePolicy = CanonicalSyncRuntimePolicy()
        ): CanonicalSyncRuntimeConfiguration {
            return CanonicalSyncRuntimeConfiguration(
                mode = mode,
                policy = policy
            )
        }
    }
}

// ── Type 4: CanonicalSyncPlanAuthorityGateState ──

enum class CanonicalSyncPlanAuthorityGateState {
    ALLOWED,
    ALLOWED_NO_COMMIT,
    BLOCKED_MISSING_SNAPSHOT,
    BLOCKED_INVALID_MANIFEST,
    BLOCKED_PEER_UNAVAILABLE,
    BLOCKED_SCHEMA_MISMATCH,
    BLOCKED_UNSUPPORTED_OBJECTS,
    BLOCKED_FALLBACK_REQUIRED_OBJECTS,
    BLOCKED_CONFLICTS,
    BLOCKED_PEER_UNKNOWN,
    BLOCKED_RELEASE_DEFAULT,
    BLOCKED
}

// ── Type 5: CanonicalSyncPlanAuthorityBlocker ──

enum class CanonicalSyncPlanAuthorityBlocker {
    MISSING_INVENTORY_SNAPSHOT,
    INVALID_LOCAL_MANIFEST,
    INVALID_PEER_MANIFEST,
    PEER_UNAVAILABLE,
    SCHEMA_MISMATCH,
    UNSUPPORTED_OBJECTS,
    FALLBACK_REQUIRED_OBJECTS,
    UNRESOLVED_CONFLICTS,
    PEER_UNKNOWN_AUDIO,
    LEGACY_FALLBACK_UNAVAILABLE,
    DIAGNOSTICS_NOT_REDACTED,
    RUNTIME_SWITCH_ENABLED,
    READ_PATH_NOT_LEGACY,
    OTHER_ACTIVE_MIGRATION_DOMAIN,
    RELEASE_DEFAULT_PRIMARY,
    DEBUG_INTERNAL_APPROVAL_MISSING,
    BLOCKED_MODE,
    CANONICAL_MODIFIED_AT_UNAVAILABLE
}

// ── Type 6: CanonicalSyncPlanAuthorityGateResult ──

data class CanonicalSyncPlanAuthorityGateResult(
    val state: CanonicalSyncPlanAuthorityGateState,
    val blockers: List<CanonicalSyncPlanAuthorityBlocker>,
    val mode: CanonicalSyncRuntimeMode
) {
    val isAllowed: Boolean
        get() = state == CanonicalSyncPlanAuthorityGateState.ALLOWED ||
                state == CanonicalSyncPlanAuthorityGateState.ALLOWED_NO_COMMIT

    val shouldUseCanonicalPrimary: Boolean
        get() = state == CanonicalSyncPlanAuthorityGateState.ALLOWED &&
                mode == CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK

    val shouldRecordNoCommit: Boolean
        get() = state == CanonicalSyncPlanAuthorityGateState.ALLOWED_NO_COMMIT ||
                mode == CanonicalSyncRuntimeMode.CANONICAL_PLAN_NO_COMMIT
}

// ── Type 7: CanonicalSyncPlanAuthorityGateContext ──

data class CanonicalSyncPlanAuthorityGateContext(
    val inventorySnapshotAvailable: Boolean,
    val localManifest: CanonicalManifest?,
    val peerManifest: CanonicalManifest?,
    val peerAbsenceExplicitlyModeled: Boolean,
    val localMetadataHashSchemaVersion: String,
    val peerMetadataHashSchemaVersion: String?,
    val canonicalModifiedAtSemanticsAvailable: Boolean,
    val unsupportedLegacyObjectCount: Int,
    val libraryFallbackRequiredObjectCount: Int,
    val conflictCount: Int,
    val peerUnknownAudioCount: Int,
    val legacyFallbackAvailable: Boolean,
    val diagnosticsRedacted: Boolean,
    val runtimeSwitchEnabled: Boolean,
    val readPathLegacy: Boolean,
    val otherActiveMigrationDomainConflicting: Boolean,
    val debugInternalBuild: Boolean,
    val ownerApproved: Boolean,
    val releaseDefaultBuild: Boolean
) {
    companion object {
        operator fun invoke(
            inventorySnapshotAvailable: Boolean,
            localManifest: CanonicalManifest?,
            peerManifest: CanonicalManifest?,
            peerAbsenceExplicitlyModeled: Boolean = false,
            localMetadataHashSchemaVersion: String = CanonicalRecordingMetadata.BUSINESS_METADATA_HASH_SCHEMA_VERSION,
            peerMetadataHashSchemaVersion: String? = CanonicalRecordingMetadata.BUSINESS_METADATA_HASH_SCHEMA_VERSION,
            canonicalModifiedAtSemanticsAvailable: Boolean = true,
            unsupportedLegacyObjectCount: Int = 0,
            libraryFallbackRequiredObjectCount: Int = 0,
            conflictCount: Int = 0,
            peerUnknownAudioCount: Int = 0,
            legacyFallbackAvailable: Boolean = true,
            diagnosticsRedacted: Boolean = true,
            runtimeSwitchEnabled: Boolean = false,
            readPathLegacy: Boolean = true,
            otherActiveMigrationDomainConflicting: Boolean = false,
            debugInternalBuild: Boolean = false,
            ownerApproved: Boolean = false,
            releaseDefaultBuild: Boolean = true
        ): CanonicalSyncPlanAuthorityGateContext {
            return CanonicalSyncPlanAuthorityGateContext(
                inventorySnapshotAvailable = inventorySnapshotAvailable,
                localManifest = localManifest,
                peerManifest = peerManifest,
                peerAbsenceExplicitlyModeled = peerAbsenceExplicitlyModeled,
                localMetadataHashSchemaVersion = localMetadataHashSchemaVersion,
                peerMetadataHashSchemaVersion = peerMetadataHashSchemaVersion,
                canonicalModifiedAtSemanticsAvailable = canonicalModifiedAtSemanticsAvailable,
                unsupportedLegacyObjectCount = unsupportedLegacyObjectCount,
                libraryFallbackRequiredObjectCount = libraryFallbackRequiredObjectCount,
                conflictCount = conflictCount,
                peerUnknownAudioCount = peerUnknownAudioCount,
                legacyFallbackAvailable = legacyFallbackAvailable,
                diagnosticsRedacted = diagnosticsRedacted,
                runtimeSwitchEnabled = runtimeSwitchEnabled,
                readPathLegacy = readPathLegacy,
                otherActiveMigrationDomainConflicting = otherActiveMigrationDomainConflicting,
                debugInternalBuild = debugInternalBuild,
                ownerApproved = ownerApproved,
                releaseDefaultBuild = releaseDefaultBuild
            )
        }
    }
}

// ── Type 8: CanonicalSyncPlanAuthorityGate ──

object CanonicalSyncPlanAuthorityGate {

    fun evaluate(
        configuration: CanonicalSyncRuntimeConfiguration,
        context: CanonicalSyncPlanAuthorityGateContext
    ): CanonicalSyncPlanAuthorityGateResult {
        val mode = configuration.mode
        if (mode == CanonicalSyncRuntimeMode.BLOCKED) {
            return result(
                CanonicalSyncPlanAuthorityGateState.BLOCKED,
                listOf(CanonicalSyncPlanAuthorityBlocker.BLOCKED_MODE),
                mode
            )
        }

        val blockers = mutableListOf<CanonicalSyncPlanAuthorityBlocker>()

        if (!context.inventorySnapshotAvailable) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.MISSING_INVENTORY_SNAPSHOT)
        }

        val localManifest = context.localManifest
        if (localManifest != null) {
            if (localManifest.schemaVersion != CanonicalManifest.CURRENT_SCHEMA_VERSION ||
                !localManifest.hasValidManifestHash
            ) {
                blockers.add(CanonicalSyncPlanAuthorityBlocker.INVALID_LOCAL_MANIFEST)
            }
        } else {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.INVALID_LOCAL_MANIFEST)
        }

        val peerManifest = context.peerManifest
        if (peerManifest != null) {
            if (peerManifest.schemaVersion != CanonicalManifest.CURRENT_SCHEMA_VERSION ||
                !peerManifest.hasValidManifestHash
            ) {
                blockers.add(CanonicalSyncPlanAuthorityBlocker.INVALID_PEER_MANIFEST)
            }
        } else if (!context.peerAbsenceExplicitlyModeled) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.PEER_UNAVAILABLE)
        }

        if (context.peerManifest != null &&
            context.peerMetadataHashSchemaVersion != context.localMetadataHashSchemaVersion
        ) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.SCHEMA_MISMATCH)
        }

        if (!context.canonicalModifiedAtSemanticsAvailable &&
            !configuration.policy.allowDocumentedModifiedAtFallback
        ) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.CANONICAL_MODIFIED_AT_UNAVAILABLE)
        }

        if (context.unsupportedLegacyObjectCount > 0) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.UNSUPPORTED_OBJECTS)
        }

        if (context.libraryFallbackRequiredObjectCount > 0) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.FALLBACK_REQUIRED_OBJECTS)
        }

        if (mode.canUseCanonicalAsPrimary && context.conflictCount > 0) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.UNRESOLVED_CONFLICTS)
        }

        if (mode.canUseCanonicalAsPrimary && context.peerUnknownAudioCount > 0) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.PEER_UNKNOWN_AUDIO)
        }

        if (!context.legacyFallbackAvailable || !configuration.policy.legacyFallbackAvailable) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.LEGACY_FALLBACK_UNAVAILABLE)
        }

        if (!context.diagnosticsRedacted || !configuration.policy.diagnosticsRedacted) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.DIAGNOSTICS_NOT_REDACTED)
        }

        if (context.runtimeSwitchEnabled || configuration.policy.runtimeSwitchEnabled) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.RUNTIME_SWITCH_ENABLED)
        }

        if (!context.readPathLegacy || !configuration.policy.readPathLegacy) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.READ_PATH_NOT_LEGACY)
        }

        if (context.otherActiveMigrationDomainConflicting ||
            configuration.policy.otherActiveMigrationDomainConflicting
        ) {
            blockers.add(CanonicalSyncPlanAuthorityBlocker.OTHER_ACTIVE_MIGRATION_DOMAIN)
        }

        if (mode.canUseCanonicalAsPrimary) {
            if (context.releaseDefaultBuild || configuration.policy.releaseDefaultBuild) {
                blockers.add(CanonicalSyncPlanAuthorityBlocker.RELEASE_DEFAULT_PRIMARY)
            }
            if (!context.debugInternalBuild || !configuration.policy.debugInternalBuild ||
                !context.ownerApproved || !configuration.policy.ownerApproved
            ) {
                blockers.add(CanonicalSyncPlanAuthorityBlocker.DEBUG_INTERNAL_APPROVAL_MISSING)
            }
        }

        val uniqueBlockers = blockers.toSet().sortedBy { it.name }
        val blockedState = blockedStateFor(uniqueBlockers)
        if (blockedState != null) {
            return result(blockedState, uniqueBlockers, mode)
        }

        return when (mode) {
            CanonicalSyncRuntimeMode.DISABLED,
            CanonicalSyncRuntimeMode.DIAGNOSTICS_ONLY,
            CanonicalSyncRuntimeMode.CANONICAL_PLAN_NO_COMMIT ->
                result(CanonicalSyncPlanAuthorityGateState.ALLOWED_NO_COMMIT, emptyList(), mode)
            CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK ->
                result(CanonicalSyncPlanAuthorityGateState.ALLOWED, emptyList(), mode)
            CanonicalSyncRuntimeMode.BLOCKED ->
                result(CanonicalSyncPlanAuthorityGateState.BLOCKED, listOf(CanonicalSyncPlanAuthorityBlocker.BLOCKED_MODE), mode)
        }
    }

    private fun blockedStateFor(
        blockers: List<CanonicalSyncPlanAuthorityBlocker>
    ): CanonicalSyncPlanAuthorityGateState? {
        if (blockers.isEmpty()) return null
        return when {
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.MISSING_INVENTORY_SNAPSHOT) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_MISSING_SNAPSHOT
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.INVALID_LOCAL_MANIFEST) ||
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.INVALID_PEER_MANIFEST) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_INVALID_MANIFEST
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.PEER_UNAVAILABLE) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_PEER_UNAVAILABLE
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.SCHEMA_MISMATCH) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_SCHEMA_MISMATCH
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.UNSUPPORTED_OBJECTS) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_UNSUPPORTED_OBJECTS
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.FALLBACK_REQUIRED_OBJECTS) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_FALLBACK_REQUIRED_OBJECTS
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.UNRESOLVED_CONFLICTS) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_CONFLICTS
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.PEER_UNKNOWN_AUDIO) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_PEER_UNKNOWN
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.RELEASE_DEFAULT_PRIMARY) ||
            blockers.contains(CanonicalSyncPlanAuthorityBlocker.DEBUG_INTERNAL_APPROVAL_MISSING) ->
                CanonicalSyncPlanAuthorityGateState.BLOCKED_RELEASE_DEFAULT
            else -> CanonicalSyncPlanAuthorityGateState.BLOCKED
        }
    }

    private fun result(
        state: CanonicalSyncPlanAuthorityGateState,
        blockers: List<CanonicalSyncPlanAuthorityBlocker>,
        mode: CanonicalSyncRuntimeMode
    ): CanonicalSyncPlanAuthorityGateResult {
        return CanonicalSyncPlanAuthorityGateResult(
            state = state,
            blockers = blockers,
            mode = mode
        )
    }
}

// ── Type 9: CanonicalSyncRuntimeActionIdentity ──

data class CanonicalSyncRuntimeActionIdentity(
    val scope: CanonicalSyncRuntimeDecisionScope,
    val objectID: String,
    val actionKind: String
) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other !is CanonicalSyncRuntimeActionIdentity) return false
        return scope == other.scope && objectID == other.objectID && actionKind == other.actionKind
    }

    override fun hashCode(): Int {
        var h = scope.hashCode()
        h = 31 * h + objectID.hashCode()
        h = 31 * h + actionKind.hashCode()
        return h
    }
}

// ── Type 10: CanonicalSyncRuntimeDuplicateExecutionGuardResult ──

data class CanonicalSyncRuntimeDuplicateExecutionGuardResult(
    val suppressedLegacyActions: List<CanonicalSyncRuntimeActionIdentity>,
    val preventedDuplicateActions: List<CanonicalSyncRuntimeActionIdentity>,
    val diagnostics: List<CanonicalSyncRuntimeDiagnostic>
)

// ── Type 11: CanonicalSyncRuntimeDuplicateExecutionGuard ──

object CanonicalSyncRuntimeDuplicateExecutionGuard {

    fun evaluate(
        canonicalOwnerUsed: Boolean,
        mode: CanonicalSyncRuntimeMode,
        syncRunID: String?,
        canonicalActions: List<CanonicalSyncRuntimeActionIdentity>,
        legacyActions: List<CanonicalSyncRuntimeActionIdentity>,
        enabledScopes: List<CanonicalSyncRuntimeDecisionScope>
    ): CanonicalSyncRuntimeDuplicateExecutionGuardResult {
        if (!canonicalOwnerUsed) {
            return CanonicalSyncRuntimeDuplicateExecutionGuardResult(
                suppressedLegacyActions = emptyList(),
                preventedDuplicateActions = emptyList(),
                diagnostics = emptyList()
            )
        }
        val enabled = enabledScopes.toSet()
        val canonicalSet = canonicalActions.filter { enabled.contains(it.scope) }.toSet()
        val duplicates = legacyActions.filter { canonicalSet.contains(it) }
            .sortedWith(
                compareBy<CanonicalSyncRuntimeActionIdentity> { it.scope.name }
                    .thenBy { it.objectID }
                    .thenBy { it.actionKind }
            )
        val diagnostics = duplicates.map { identity ->
            CanonicalSyncRuntimeDiagnostic(
                kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_DUPLICATE_EXECUTION_PREVENTED,
                syncRunID = syncRunID,
                mode = mode,
                objectID = identity.objectID,
                detail = identity.scope.name
            )
        } + if (duplicates.isEmpty()) emptyList() else listOf(
            CanonicalSyncRuntimeDiagnostic(
                kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_DUPLICATE_LEGACY_SUPPRESSED,
                syncRunID = syncRunID,
                mode = mode,
                count = duplicates.size,
                detail = "exactScopeObjectAction"
            )
        )
        return CanonicalSyncRuntimeDuplicateExecutionGuardResult(
            suppressedLegacyActions = duplicates,
            preventedDuplicateActions = duplicates,
            diagnostics = diagnostics
        )
    }
}

// ── Type 12: CanonicalSyncRuntimeResult ──

data class CanonicalSyncRuntimeResult(
    val mode: CanonicalSyncRuntimeMode,
    val gateResult: CanonicalSyncPlanAuthorityGateResult,
    val canonicalPlanUsed: Boolean,
    val canonicalPlanFallback: Boolean,
    val canonicalPlanBlocked: Boolean,
    val canonicalPlanNoCommit: Boolean,
    val syncRunID: String?,
    val diagnostics: List<CanonicalSyncRuntimeDiagnostic>,
    val legacyActions: List<CanonicalSyncRuntimeActionIdentity> = emptyList(),
    val canonicalActions: List<CanonicalSyncRuntimeActionIdentity> = emptyList()
) {
    companion object {
        fun make(
            mode: CanonicalSyncRuntimeMode,
            gateResult: CanonicalSyncPlanAuthorityGateResult,
            syncRunID: String?,
            extraDiagnostics: List<CanonicalSyncRuntimeDiagnostic> = emptyList(),
            legacyActions: List<CanonicalSyncRuntimeActionIdentity> = emptyList(),
            canonicalActions: List<CanonicalSyncRuntimeActionIdentity> = emptyList()
        ): CanonicalSyncRuntimeResult {
            val used = gateResult.shouldUseCanonicalPrimary
            val noCommit = !used && gateResult.isAllowed
            val blocked = !gateResult.isAllowed
            val diagnostics = mutableListOf(
                CanonicalSyncRuntimeDiagnostic(
                    kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_MODE_EVALUATED,
                    syncRunID = syncRunID,
                    mode = mode,
                    detail = "state=${gateResult.state.name}"
                ),
                CanonicalSyncRuntimeDiagnostic(
                    kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_EVALUATED,
                    syncRunID = syncRunID,
                    mode = mode,
                    count = gateResult.blockers.size,
                    detail = gateResult.state.name
                ),
                CanonicalSyncRuntimeDiagnostic(
                    kind = if (gateResult.isAllowed)
                        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_AUTHORITY_GATE_ALLOWED
                    else
                        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_AUTHORITY_GATE_BLOCKED,
                    syncRunID = syncRunID,
                    mode = mode,
                    count = gateResult.blockers.size,
                    detail = gateResult.blockers.joinToString("+") { it.name.lowercase() }.nilIfEmpty ?: "none"
                ),
                CanonicalSyncRuntimeDiagnostic(
                    kind = if (gateResult.isAllowed)
                        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_ALLOWED
                    else
                        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_BLOCKED,
                    syncRunID = syncRunID,
                    mode = mode,
                    count = gateResult.blockers.size,
                    detail = gateResult.state.name
                )
            )
            if (used) {
                diagnostics.add(
                    CanonicalSyncRuntimeDiagnostic(
                        kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_USED,
                        syncRunID = syncRunID,
                        mode = mode,
                        detail = "primary"
                    )
                )
            } else if (noCommit) {
                diagnostics.add(
                    CanonicalSyncRuntimeDiagnostic(
                        kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_NO_COMMIT,
                        syncRunID = syncRunID,
                        mode = mode,
                        detail = "legacyOwner"
                    )
                )
            } else {
                diagnostics.add(
                    CanonicalSyncRuntimeDiagnostic(
                        kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_FALLBACK,
                        syncRunID = syncRunID,
                        mode = mode,
                        count = gateResult.blockers.size,
                        detail = gateResult.state.name
                    )
                )
            }
            diagnostics.addAll(extraDiagnostics)
            return CanonicalSyncRuntimeResult(
                mode = mode,
                gateResult = gateResult,
                canonicalPlanUsed = used,
                canonicalPlanFallback = !used,
                canonicalPlanBlocked = blocked,
                canonicalPlanNoCommit = noCommit,
                syncRunID = syncRunID,
                diagnostics = diagnostics,
                legacyActions = legacyActions,
                canonicalActions = canonicalActions
            )
        }
    }
}
