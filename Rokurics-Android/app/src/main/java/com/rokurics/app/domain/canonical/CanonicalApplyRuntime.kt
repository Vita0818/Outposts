package com.rokurics.app.domain.canonical

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeMode
// ═══════════════════════════════════════════

enum class CanonicalApplyRuntimeMode(
    val evaluatesCandidates: Boolean,
    val canExecuteNonAudio: Boolean
) {
    DISABLED(false, false),
    DIAGNOSTICS_ONLY(true, false),
    NO_COMMIT(true, false),
    TEST_ROOT_APPLY(true, true),
    PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK(true, true),
    BLOCKED(false, false)
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeDomain
// ═══════════════════════════════════════════

enum class CanonicalApplyRuntimeDomain(val rawValue: String) {
    RECORDING_METADATA("recordingMetadata"),
    LIBRARY_METADATA("libraryMetadata"),
    GENERATED_ARTIFACTS("generatedArtifacts"),
    TOMBSTONE_CONFLICT("tombstoneConflict"),
    RECORDING_EXISTENCE("recordingExistence"),
    AUDIO_UPLOAD("audioUpload");

    companion object {
        val allCases: List<CanonicalApplyRuntimeDomain> = entries.toList()
    }
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimePolicy
// ═══════════════════════════════════════════

data class CanonicalApplyRuntimePolicy(
    val debugInternalBuild: Boolean = false,
    val ownerApproved: Boolean = false,
    val releaseDefaultBuild: Boolean = true,
    val diagnosticsRedacted: Boolean = true,
    val legacyFallbackAvailable: Boolean = false,
    val runtimeSwitchEnabled: Boolean = false,
    val readPathLegacy: Boolean = true,
    val rootBoundRequired: Boolean = false,
    val rollbackRequired: Boolean = false,
    val postconditionRequired: Boolean = false,
    val enabledDomains: Set<CanonicalApplyRuntimeDomain> = emptySet()
)

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeConfiguration
// ═══════════════════════════════════════════

data class CanonicalApplyRuntimeConfiguration(
    val mode: CanonicalApplyRuntimeMode,
    val policy: CanonicalApplyRuntimePolicy
) {
    companion object {
        val DISABLED = CanonicalApplyRuntimeConfiguration(
            mode = CanonicalApplyRuntimeMode.DISABLED,
            policy = CanonicalApplyRuntimePolicy()
        )
    }
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeGateState
// ═══════════════════════════════════════════

enum class CanonicalApplyRuntimeGateState {
    ALLOWED,
    BLOCKED
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeGateBlocker
// ═══════════════════════════════════════════

enum class CanonicalApplyRuntimeGateBlocker {
    BLOCKED_MODE,
    MISSING_INVENTORY_SNAPSHOT,
    MISSING_PLAN,
    MISSING_EXECUTOR,
    INVALID_EXECUTOR,
    AUDIO_ACTION_BLOCKED,
    UNSUPPORTED_DOMAIN,
    ROOT_BOUND_REQUIRED,
    PRODUCTION_ROOT_NOT_ALLOWED,
    RELEASE_DEFAULT_PRODUCTION_BLOCKED,
    DEBUG_INTERNAL_REQUIRED,
    LEGACY_FALLBACK_UNAVAILABLE,
    DIAGNOSTICS_NOT_REDACTED,
    RUNTIME_SWITCH_ENABLED,
    READ_PATH_NOT_LEGACY,
    POSTCONDITION_FAILED
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeGateResult
// ═══════════════════════════════════════════

data class CanonicalApplyRuntimeGateResult(
    val state: CanonicalApplyRuntimeGateState,
    val blockers: List<CanonicalApplyRuntimeGateBlocker>
) {
    val allowed: Boolean get() = state == CanonicalApplyRuntimeGateState.ALLOWED
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeGate
// ═══════════════════════════════════════════

object CanonicalApplyRuntimeGate {

    fun evaluate(
        config: CanonicalApplyRuntimeConfiguration,
        plan: CanonicalApplyPlan?,
        executorRegistry: CanonicalApplyRuntimeExecutorRegistry,
        inventorySnapshot: CanonicalManifest?
    ): CanonicalApplyRuntimeGateResult {
        val blockers = mutableListOf<CanonicalApplyRuntimeGateBlocker>()

        val mode = config.mode

        if (mode == CanonicalApplyRuntimeMode.BLOCKED) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.BLOCKED_MODE)
            return CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.BLOCKED, blockers)
        }

        if (mode == CanonicalApplyRuntimeMode.DISABLED) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.BLOCKED_MODE)
            return CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.BLOCKED, blockers)
        }

        if (!mode.evaluatesCandidates) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.BLOCKED_MODE)
        }

        if (config.policy.runtimeSwitchEnabled) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.RUNTIME_SWITCH_ENABLED)
        }

        if (!config.policy.diagnosticsRedacted && mode == CanonicalApplyRuntimeMode.DIAGNOSTICS_ONLY) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.DIAGNOSTICS_NOT_REDACTED)
        }

        if (!config.policy.readPathLegacy) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.READ_PATH_NOT_LEGACY)
        }

        if (inventorySnapshot == null) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.MISSING_INVENTORY_SNAPSHOT)
        }

        if (plan == null) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.MISSING_PLAN)
        }

        if (config.policy.rootBoundRequired) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.ROOT_BOUND_REQUIRED)
        }

        val isProduction = mode == CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK
        if (isProduction) {
            if (config.policy.releaseDefaultBuild) {
                blockers.add(CanonicalApplyRuntimeGateBlocker.RELEASE_DEFAULT_PRODUCTION_BLOCKED)
            }
            if (!config.policy.debugInternalBuild) {
                blockers.add(CanonicalApplyRuntimeGateBlocker.DEBUG_INTERNAL_REQUIRED)
            }
            if (!config.policy.ownerApproved) {
                blockers.add(CanonicalApplyRuntimeGateBlocker.PRODUCTION_ROOT_NOT_ALLOWED)
            }
            if (!config.policy.legacyFallbackAvailable) {
                blockers.add(CanonicalApplyRuntimeGateBlocker.LEGACY_FALLBACK_UNAVAILABLE)
            }
        }

        if (config.policy.postconditionRequired) {
            blockers.add(CanonicalApplyRuntimeGateBlocker.POSTCONDITION_FAILED)
        }

        if (plan != null) {
            for (action in plan.actions) {
                val domain = domainForAction(action)
                if (domain != null && domain !in config.policy.enabledDomains) {
                    blockers.add(CanonicalApplyRuntimeGateBlocker.UNSUPPORTED_DOMAIN)
                    break
                }
                val isAudioAction = action.target.artifactKind == CanonicalArtifact.Kind.AUDIO
                if (isAudioAction && !mode.canExecuteNonAudio) {
                    blockers.add(CanonicalApplyRuntimeGateBlocker.AUDIO_ACTION_BLOCKED)
                    break
                }
                if (domain != null && executorRegistry.get(domain) == null) {
                    blockers.add(CanonicalApplyRuntimeGateBlocker.MISSING_EXECUTOR)
                    break
                }
            }
        }

        return if (blockers.isEmpty()) {
            CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.ALLOWED, emptyList())
        } else {
            CanonicalApplyRuntimeGateResult(CanonicalApplyRuntimeGateState.BLOCKED, blockers.distinct())
        }
    }

    internal fun domainForAction(action: CanonicalApplyAction): CanonicalApplyRuntimeDomain? {
        return when (action.kind) {
            CanonicalApplyActionKind.recordingMetadataApply,
            CanonicalApplyActionKind.recordingMetadataSend -> CanonicalApplyRuntimeDomain.RECORDING_METADATA
            CanonicalApplyActionKind.folderMetadataApply,
            CanonicalApplyActionKind.folderMetadataSend,
            CanonicalApplyActionKind.studyItemMetadataApply,
            CanonicalApplyActionKind.studyItemMetadataSend -> CanonicalApplyRuntimeDomain.LIBRARY_METADATA
            CanonicalApplyActionKind.generatedArtifactDownloadApply,
            CanonicalApplyActionKind.generatedArtifactNoOp -> CanonicalApplyRuntimeDomain.GENERATED_ARTIFACTS
            CanonicalApplyActionKind.libraryTombstoneApply,
            CanonicalApplyActionKind.libraryTombstoneSend,
            CanonicalApplyActionKind.objectTombstoneApply,
            CanonicalApplyActionKind.objectTombstoneSend,
            CanonicalApplyActionKind.artifactTombstoneApply,
            CanonicalApplyActionKind.conflictRecord -> CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT
            CanonicalApplyActionKind.deferredUnsupported -> null
        }
    }
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeActionState
// ═══════════════════════════════════════════

enum class CanonicalApplyRuntimeActionState {
    PLANNED,
    STARTED,
    COMPLETED,
    FAILED,
    LEGACY_FALLBACK,
    DUPLICATE_SUPPRESSED
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeActionResult
// ═══════════════════════════════════════════

data class CanonicalApplyRuntimeActionResult
private constructor(
    val action: CanonicalApplyAction,
    val state: CanonicalApplyRuntimeActionState,
    val failureReason: String?,
    val rollbackCheckpointID: String?
) {
    val id: String
        get() = listOfNotNull(action.id, state.name, rollbackCheckpointID).joinToString("|")

    companion object {
        operator fun invoke(
            action: CanonicalApplyAction,
            state: CanonicalApplyRuntimeActionState,
            failureReason: String? = null,
            rollbackCheckpointID: String? = null
        ): CanonicalApplyRuntimeActionResult {
            return CanonicalApplyRuntimeActionResult(
                action = action,
                state = state,
                failureReason = CanonicalProductionRedaction.safeDiagnosticText(failureReason ?: ""),
                rollbackCheckpointID = rollbackCheckpointID?.trim()?.nilIfEmpty
            )
        }
    }
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeExecutionResult
// ═══════════════════════════════════════════

data class CanonicalApplyRuntimeExecutionResult(
    val results: List<CanonicalApplyRuntimeActionResult>,
    val rollbackPerformed: Boolean,
    val rollbackSuccessful: Boolean,
    val legacyFallbackCount: Int,
    val duplicateSuppressedCount: Int
) {
    val allCompleted: Boolean
        get() = results.all { it.state == CanonicalApplyRuntimeActionState.COMPLETED }
    val anyFailed: Boolean
        get() = results.any { it.state == CanonicalApplyRuntimeActionState.FAILED }
    val fatalRollback: Boolean
        get() = rollbackPerformed && !rollbackSuccessful
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeExecutor
// ═══════════════════════════════════════════

interface CanonicalApplyRuntimeExecutor {
    fun execute(action: CanonicalApplyAction): CanonicalApplyRuntimeActionResult
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeExecutorRegistry
// ═══════════════════════════════════════════

class CanonicalApplyRuntimeExecutorRegistry {
    private val executors = mutableMapOf<CanonicalApplyRuntimeDomain, CanonicalApplyRuntimeExecutor>()

    fun register(domain: CanonicalApplyRuntimeDomain, executor: CanonicalApplyRuntimeExecutor) {
        executors[domain] = executor
    }

    fun get(domain: CanonicalApplyRuntimeDomain): CanonicalApplyRuntimeExecutor? {
        return executors[domain]
    }

    val registeredDomains: Set<CanonicalApplyRuntimeDomain>
        get() = executors.keys.toSet()
}

// ═══════════════════════════════════════════
// CanonicalApplyRuntimeOwner
// ═══════════════════════════════════════════

class CanonicalApplyRuntimeOwner {

    fun execute(
        plan: CanonicalApplyPlan,
        config: CanonicalApplyRuntimeConfiguration,
        executorRegistry: CanonicalApplyRuntimeExecutorRegistry,
        inventorySnapshot: CanonicalManifest?
    ): CanonicalApplyRuntimeExecutionResult {
        val results = mutableListOf<CanonicalApplyRuntimeActionResult>()
        var rollbackPerformed = false
        var rollbackSuccessful = false
        var legacyFallbackCount = 0
        var duplicateSuppressedCount = 0

        val gateResult = CanonicalApplyRuntimeGate.evaluate(config, plan, executorRegistry, inventorySnapshot)
        if (!gateResult.allowed) {
            for (action in plan.actions) {
                results.add(
                    CanonicalApplyRuntimeActionResult(
                        action = action,
                        state = CanonicalApplyRuntimeActionState.FAILED,
                        failureReason = gateResult.blockers.firstOrNull()?.name
                    )
                )
            }
            return CanonicalApplyRuntimeExecutionResult(
                results = results,
                rollbackPerformed = false,
                rollbackSuccessful = false,
                legacyFallbackCount = 0,
                duplicateSuppressedCount = 0
            )
        }

        var previousDomain: CanonicalApplyRuntimeDomain? = null
        val reversedResults = mutableListOf<CanonicalApplyRuntimeActionResult>()

        for (action in plan.actions) {
            val domain = CanonicalApplyRuntimeGate.domainForAction(action)

            if (domain != null && domain !in config.policy.enabledDomains) {
                val failedResult = CanonicalApplyRuntimeActionResult(
                    action = action,
                    state = CanonicalApplyRuntimeActionState.FAILED,
                    failureReason = CanonicalApplyRuntimeGateBlocker.UNSUPPORTED_DOMAIN.name
                )
                results.add(failedResult)
                if (config.policy.rollbackRequired && previousDomain != null && previousDomain != domain) {
                    rollbackPerformed = true
                    rollbackSuccessful = performDomainRollback(reversedResults)
                    if (!rollbackSuccessful) break
                }
                break
            }

            val isAudioAction = action.target.artifactKind == CanonicalArtifact.Kind.AUDIO
            if (isAudioAction && !config.mode.canExecuteNonAudio) {
                val failedResult = CanonicalApplyRuntimeActionResult(
                    action = action,
                    state = CanonicalApplyRuntimeActionState.FAILED,
                    failureReason = CanonicalApplyRuntimeGateBlocker.AUDIO_ACTION_BLOCKED.name
                )
                results.add(failedResult)
                if (config.policy.rollbackRequired && previousDomain != null && previousDomain != domain) {
                    rollbackPerformed = true
                    rollbackSuccessful = performDomainRollback(reversedResults)
                    if (!rollbackSuccessful) break
                }
                break
            }

            if (domain == null) {
                results.add(
                    CanonicalApplyRuntimeActionResult(
                        action = action,
                        state = CanonicalApplyRuntimeActionState.FAILED,
                        failureReason = CanonicalApplyRuntimeGateBlocker.UNSUPPORTED_DOMAIN.name
                    )
                )
                continue
            }

            val executor = executorRegistry.get(domain)
            if (executor == null) {
                val failedResult = CanonicalApplyRuntimeActionResult(
                    action = action,
                    state = CanonicalApplyRuntimeActionState.FAILED,
                    failureReason = CanonicalApplyRuntimeGateBlocker.MISSING_EXECUTOR.name
                )
                results.add(failedResult)
                if (config.policy.rollbackRequired && previousDomain != null && previousDomain != domain) {
                    rollbackPerformed = true
                    rollbackSuccessful = performDomainRollback(reversedResults)
                    if (!rollbackSuccessful) break
                }
                break
            }

            val result = executor.execute(action)
            results.add(result)

            when (result.state) {
                CanonicalApplyRuntimeActionState.COMPLETED -> {
                    reversedResults.add(result)
                }
                CanonicalApplyRuntimeActionState.LEGACY_FALLBACK -> {
                    legacyFallbackCount++
                    reversedResults.add(result)
                }
                CanonicalApplyRuntimeActionState.DUPLICATE_SUPPRESSED -> {
                    duplicateSuppressedCount++
                }
                CanonicalApplyRuntimeActionState.FAILED -> {
                    if (config.policy.rollbackRequired && previousDomain != null && previousDomain != domain) {
                        rollbackPerformed = true
                        rollbackSuccessful = performDomainRollback(reversedResults)
                        if (!rollbackSuccessful) break
                    }
                    break
                }
                else -> {
                    reversedResults.add(result)
                }
            }

            previousDomain = domain
        }

        return CanonicalApplyRuntimeExecutionResult(
            results = results,
            rollbackPerformed = rollbackPerformed,
            rollbackSuccessful = rollbackSuccessful,
            legacyFallbackCount = legacyFallbackCount,
            duplicateSuppressedCount = duplicateSuppressedCount
        )
    }

    private fun performDomainRollback(
        reversedResults: List<CanonicalApplyRuntimeActionResult>
    ): Boolean {
        for (result in reversedResults.reversed()) {
            if (result.state != CanonicalApplyRuntimeActionState.COMPLETED &&
                result.state != CanonicalApplyRuntimeActionState.LEGACY_FALLBACK
            ) {
                return false
            }
        }
        return true
    }
}
