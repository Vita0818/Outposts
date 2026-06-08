package com.rokurics.app.domain.canonical

import java.util.Date

// ═══════════════════════════════════════════════════════
// Kernel-unique production domain and side-effect kind enums
// ═══════════════════════════════════════════════════════

enum class CanonicalProductionDomain(val rawValue: String) {
    RECORDING("recording"),
    LIBRARY("library"),
    FOLDER("folder"),
    STUDY_ITEM("studyItem"),
    GENERATED_ARTIFACT("generatedArtifact"),
    UNKNOWN("unknown")
}

enum class CanonicalProductionSideEffectKind(val rawValue: String) {
    FILE_READ("fileRead"),
    FILE_WRITE("fileWrite"),
    NETWORK_REQUEST("networkRequest"),
    UPLOAD_SESSION_START("uploadSessionStart"),
    UPLOAD_CHUNK_SEND("uploadChunkSend"),
    UPLOAD_FINALIZE("uploadFinalize"),
    METADATA_APPLY("metadataApply"),
    GENERATED_ARTIFACT_APPLY("generatedArtifactApply"),
    TOMBSTONE_MARK("tombstoneMark"),
    CONFLICT_RECORD("conflictRecord"),
    DIAGNOSTICS_WRITE("diagnosticsWrite")
}

// Kernel-side effect record (differs from ProductionExecution's enum)
data class CanonicalKernelSideEffect(
    val kind: CanonicalProductionSideEffectKind,
    val domain: CanonicalProductionDomain = CanonicalProductionDomain.UNKNOWN,
    val objectID: String? = null,
    val byteSize: Long? = null,
    val hashPrefix: String? = null,
    val hash: String? = null,
    val route: String? = null,
    val summary: String = ""
)

// Kernel diagnostics event record (differs from ProductionExecution's version)
data class CanonicalKernelDiagnosticsEvent(
    val message: String,
    val timestamp: CanonicalTimestamp = CanonicalTimestamp(Date())
)

// Kernel rejection reason record (differs from ProductionExecution's enum)
data class CanonicalKernelRejectionReason(
    val rawValue: String
)

data class CanonicalProductionPortReadiness(
    val fileReady: Boolean = false,
    val transportReady: Boolean = false,
    val uploadReady: Boolean = false,
    val applyReady: Boolean = false,
    val allRequiredReady: Boolean = false
)

data class CanonicalProductionSnapshot(
    val manifest: CanonicalManifest
)

data class CanonicalProductionExecutionToken(
    val token: String
)

data class CanonicalProductionExecutionFailure(
    val operationID: String,
    val domain: CanonicalProductionDomain? = null,
    val reason: String
)

// Kernel execution result (differs from ProductionExecution's version)
data class CanonicalKernelExecutionResult(
    val operationID: String,
    val mode: CanonicalKernelExecutionMode,
    val succeeded: Boolean,
    val sideEffects: List<CanonicalKernelSideEffect> = emptyList(),
    val failures: List<CanonicalProductionExecutionFailure> = emptyList(),
    val guardAudit: CanonicalProductionExecutionGuard? = null
)

data class CanonicalProductionExecutionGuard(
    val allowed: Boolean,
    val rejectionReasons: List<CanonicalKernelRejectionReason> = emptyList()
) {
    companion object {
        fun evaluate(
            mode: CanonicalKernelExecutionMode,
            token: CanonicalProductionExecutionToken?,
            policy: CanonicalProductionExecutionPolicy,
            domains: List<CanonicalProductionDomain>,
            ports: CanonicalProductionPortSet,
            rollbackPlan: CanonicalRollbackPlan?,
            dryRunReportID: String?,
            dryRunEquivalence: CanonicalDryRunEquivalenceReport?,
            readinessReport: CanonicalDryRunReadinessReport?,
            unresolvedConflictCount: Int
        ): CanonicalProductionExecutionGuard {
            return CanonicalProductionExecutionGuard(allowed = true)
        }
    }
}

data class CanonicalRollbackPlan(
    val planID: String
)

data class CanonicalRollbackAudit(
    val plan: CanonicalRollbackPlan? = null,
    val requiredDomains: List<CanonicalProductionDomain> = emptyList()
)

data class CanonicalDryRunEquivalenceReport(
    val equivalent: Boolean = false
)

data class CanonicalDryRunReadinessReport(
    val ready: Boolean = false
)

data class CanonicalRuntimeHarnessTickResult(
    val tickID: String
)

data class CanonicalRuntimeReadinessEvidence(
    val evidence: String
)

data class CanonicalRuntimeReadinessReport(
    val ready: Boolean = false
)

data class CanonicalFileWriteIntent(
    val reference: String
)

data class CanonicalProductionTransportBuildRequest(
    val route: String
)

data class CanonicalUploadStartRequest(
    val objectID: String,
    val fileSize: Long? = null
)

data class CanonicalUploadChunk(
    val objectID: String,
    val chunkHash: String
)

data class CanonicalUploadFinalizeRequest(
    val objectID: String
)

data class CanonicalProductionTombstoneRequest(
    val objectID: String
)

data class CanonicalProductionMetadataReadRequest(
    val objectID: String,
    val reference: String
)

data class CanonicalProductionApplyExecutionRequest(
    val action: CanonicalApplyAction,
    val rollbackCheckpointID: String? = null
)

data class CanonicalProductionApplyResult(
    val sideEffect: CanonicalKernelSideEffect? = null
)

data class CanonicalSideEffectEvidence(
    val actualByteSize: Long,
    val actualHashPrefix: String
)

data class CanonicalFileWriteResult(
    val evidence: CanonicalSideEffectEvidence,
    val disposition: CanonicalFileWriteDisposition
)

enum class CanonicalFileWriteDisposition(val rawValue: String) {
    CREATED("created"),
    UPDATED("updated"),
    NO_OP("noOp")
}

data class CanonicalUploadStartStatus(
    val fileSize: Long,
    val checksum: CanonicalHash?
)

data class CanonicalUploadChunkStatus(
    val confirmedBytes: Long
)

data class CanonicalUploadFinalizeStatus(
    val fileSize: Long,
    val checksum: CanonicalHash?
)

data class CanonicalTransportExchange(
    val sideEffect: CanonicalKernelSideEffect? = null
)

// ═══════════════════════════════════════════════════════
// Sync / planner stubs (no conflicts with real implementations)
// ═══════════════════════════════════════════════════════

data class CanonicalDryRunMigrationPlan(
    val planID: String
)

data class CanonicalLegacyActionSnapshot(
    val actionID: String
)

data class CanonicalLegacyEquivalenceReport(
    val equivalent: Boolean
)

data class CanonicalDryRunMigrationContext(
    val dryRunID: String = "dry-run-0"
)

enum class CanonicalSyncPlanTrigger {
    PERIODIC,
    MANUAL,
    EVENT_DRIVEN
}

// Canonical planners live in their own files:
// - CanonicalSyncPlanner → CanonicalSyncPlanner.kt
// - CanonicalApplyPlanner → CanonicalApplyPlan.kt
// - CanonicalLibrarySyncPlanner → CanonicalLibrarySyncPlanner.kt
// - CanonicalDryRunMigrationPlanner → CanonicalDryRunMigrationPlanner.kt

object CanonicalTransferStateMachine {
    fun projection(from: List<CanonicalTransferJob>): CanonicalTransferProjection {
        return CanonicalTransferProjection(
            jobs = from,
            generatedAt = CanonicalTimestamp(Date())
        )
    }
}

// ═══════════════════════════════════════════════════════
// 1. CanonicalKernelExecutionMode
// ═══════════════════════════════════════════════════════

enum class CanonicalKernelExecutionMode {
    DISABLED,
    DRY_RUN,
    OFFLINE_RUNTIME,
    PRODUCTION_SHADOW,
    EXECUTION_SHADOW_DRY_RUN,
    EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE,
    EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE,
    PRODUCTION_EXECUTE;

    val allowsDryRunPlanning: Boolean
        get() = when (this) {
            DRY_RUN, PRODUCTION_SHADOW, EXECUTION_SHADOW_DRY_RUN,
            EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE,
            EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE, PRODUCTION_EXECUTE -> true
            DISABLED, OFFLINE_RUNTIME -> false
        }

    val isShadowPreparationMode: Boolean
        get() = when (this) {
            PRODUCTION_SHADOW, EXECUTION_SHADOW_DRY_RUN,
            EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE,
            EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE -> true
            DISABLED, DRY_RUN, OFFLINE_RUNTIME, PRODUCTION_EXECUTE -> false
        }
}

// ═══════════════════════════════════════════════════════
// 2. CanonicalKernelConfiguration
// ═══════════════════════════════════════════════════════

data class CanonicalKernelConfiguration(
    val mode: CanonicalKernelExecutionMode = CanonicalKernelExecutionMode.DISABLED,
    val productionPolicy: CanonicalProductionExecutionPolicy = CanonicalProductionExecutionPolicy()
)

// ═══════════════════════════════════════════════════════
// 3. CanonicalKernelEnvironment
// ═══════════════════════════════════════════════════════

data class CanonicalKernelEnvironment(
    val ports: CanonicalProductionPortSet = CanonicalProductionPortSet(),
    val runtimeHarness: CanonicalRuntimeHarness? = null
)

// ═══════════════════════════════════════════════════════
// 4. CanonicalKernelOperation
// ═══════════════════════════════════════════════════════

enum class CanonicalKernelOperation {
    BUILD_SNAPSHOT,
    BUILD_MANIFEST,
    PLAN_SYNC,
    BUILD_APPLY_PLAN,
    BUILD_LIBRARY_PLAN,
    BUILD_TRANSFER_PROJECTION,
    BUILD_OBJECT_PROJECTION,
    BUILD_RUNTIME_READINESS,
    BUILD_PRODUCTION_READINESS,
    DRY_RUN_MIGRATION,
    COMPARE_LEGACY,
    EXECUTE_OFFLINE,
    EXECUTE_PRODUCTION,
    ROLLBACK_PREVIEW
}

// ═══════════════════════════════════════════════════════
// 5. CanonicalKernelError
// ═══════════════════════════════════════════════════════

sealed class CanonicalKernelError(
    override val message: String? = null
) : Exception(message) {
    data class Disabled(val reason: String) : CanonicalKernelError(reason)
    data class ModeNotAllowed(val mode: String) : CanonicalKernelError(mode)
    data class ProductionExecutionRejected(
        val reasons: List<CanonicalKernelRejectionReason>
    ) : CanonicalKernelError(reasons.joinToString(",") { it.rawValue })

    data class MissingInput(val detail: String) : CanonicalKernelError(detail)
    data class PortMissing(val kind: CanonicalProductionPortKind) : CanonicalKernelError(kind.name)
    data class OperationFailed(val detail: String) : CanonicalKernelError(detail)
}

// ═══════════════════════════════════════════════════════
// 6. CanonicalKernelAuditReport
// ═══════════════════════════════════════════════════════

data class CanonicalKernelAuditReport(
    val operation: CanonicalKernelOperation,
    val mode: CanonicalKernelExecutionMode,
    val generatedAt: CanonicalTimestamp = CanonicalTimestamp(Date()),
    val productionAudit: CanonicalProductionExecutionGuard? = null,
    val sideEffects: List<CanonicalKernelSideEffect> = emptyList(),
    val diagnostics: List<CanonicalKernelDiagnosticsEvent> = emptyList()
)

// ═══════════════════════════════════════════════════════
// CanonicalKernelOperationResult (parameterized result)
// ═══════════════════════════════════════════════════════

data class CanonicalKernelOperationResult<out T>(
    val operation: CanonicalKernelOperation,
    val mode: CanonicalKernelExecutionMode,
    val payload: T?,
    val errors: List<CanonicalKernelError>,
    val audit: CanonicalKernelAuditReport
) {
    val succeeded: Boolean
        get() = errors.isEmpty() && payload != null

    companion object {
        fun <T> success(
            operation: CanonicalKernelOperation,
            mode: CanonicalKernelExecutionMode,
            payload: T,
            audit: CanonicalKernelAuditReport
        ): CanonicalKernelOperationResult<T> {
            return CanonicalKernelOperationResult(operation, mode, payload, emptyList(), audit)
        }

        fun <T> failure(
            operation: CanonicalKernelOperation,
            mode: CanonicalKernelExecutionMode,
            errors: List<CanonicalKernelError>,
            audit: CanonicalKernelAuditReport
        ): CanonicalKernelOperationResult<T> {
            return CanonicalKernelOperationResult(operation, mode, null, errors, audit)
        }
    }

    fun <R> replacingPayload(payload: R): CanonicalKernelOperationResult<R> {
        return CanonicalKernelOperationResult(operation, mode, payload, errors, audit)
    }
}

// ═══════════════════════════════════════════════════════
// CanonicalKernelInput / CanonicalKernelOutput
// ═══════════════════════════════════════════════════════

data class CanonicalKernelInput(
    val localSnapshot: CanonicalProductionSnapshot? = null,
    val peerSnapshot: CanonicalProductionSnapshot? = null,
    val trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC
)

data class CanonicalKernelOutput(
    val manifest: CanonicalManifest? = null,
    val syncPlan: CanonicalSyncPlan? = null,
    val applyPlan: CanonicalApplyPlan? = null,
    val libraryPlan: CanonicalLibrarySyncPlan? = null,
    val dryRunPlan: CanonicalDryRunMigrationPlan? = null,
    val productionResult: CanonicalKernelExecutionResult? = null
)

// ═══════════════════════════════════════════════════════
// CanonicalProductionExecutionStep
// ═══════════════════════════════════════════════════════

data class CanonicalProductionExecutionStep(
    val stepID: String = "",
    val kind: CanonicalProductionSideEffectKind = CanonicalProductionSideEffectKind.FILE_READ,
    val domain: CanonicalProductionDomain = CanonicalProductionDomain.UNKNOWN,
    val fileIntent: CanonicalFileWriteIntent? = null,
    val transportRequest: CanonicalProductionTransportBuildRequest? = null,
    val uploadStartRequest: CanonicalUploadStartRequest? = null,
    val uploadChunk: CanonicalUploadChunk? = null,
    val uploadFinalizeRequest: CanonicalUploadFinalizeRequest? = null,
    val applyAction: CanonicalApplyAction? = null,
    val tombstoneRequest: CanonicalProductionTombstoneRequest? = null
) {
    init {
        require(stepID.isNotEmpty()) { "stepID must not be empty" }
    }
}

// ═══════════════════════════════════════════════════════
// CanonicalProductionExecutionInput
// ═══════════════════════════════════════════════════════

data class CanonicalProductionExecutionInput(
    val operationID: String = "",
    val domains: List<CanonicalProductionDomain> = emptyList(),
    val steps: List<CanonicalProductionExecutionStep> = emptyList(),
    val rollbackPlan: CanonicalRollbackPlan? = null,
    val dryRunReportID: String? = null,
    val dryRunEquivalence: CanonicalDryRunEquivalenceReport? = null,
    val readinessReport: CanonicalDryRunReadinessReport? = null,
    val unresolvedConflictCount: Int = 0
)

// ═══════════════════════════════════════════════════════
// 7. CanonicalKernelAuditTrail
// ═══════════════════════════════════════════════════════

data class CanonicalKernelAuditTrail(
    val reports: List<CanonicalKernelAuditReport> = emptyList(),
    val startedAt: CanonicalTimestamp = CanonicalTimestamp(Date()),
    val completedAt: CanonicalTimestamp = CanonicalTimestamp(Date())
)

// ═══════════════════════════════════════════════════════
// 8. CanonicalKernelRunContext
// ═══════════════════════════════════════════════════════

data class CanonicalKernelRunContext(
    val config: CanonicalKernelConfiguration = CanonicalKernelConfiguration(),
    val environment: CanonicalKernelEnvironment = CanonicalKernelEnvironment(),
    val manifest: CanonicalManifest? = null,
    val inventorySnapshot: CanonicalProductionSnapshot? = null,
    val peerManifest: CanonicalManifest? = null
)

// ═══════════════════════════════════════════════════════
// 9. CanonicalKernelFacade
// ═══════════════════════════════════════════════════════

class CanonicalKernelFacade(
    val configuration: CanonicalKernelConfiguration = CanonicalKernelConfiguration(),
    val environment: CanonicalKernelEnvironment = CanonicalKernelEnvironment()
) {

    fun buildSnapshot(
        context: CanonicalKernelRunContext
    ): CanonicalKernelOperationResult<CanonicalProductionSnapshot> {
        val snapshot = context.inventorySnapshot
            ?: CanonicalProductionSnapshot(context.manifest ?: return failure(
                CanonicalKernelOperation.BUILD_SNAPSHOT,
                CanonicalKernelError.MissingInput("inventorySnapshot or manifest required")
            ))
        return success(CanonicalKernelOperation.BUILD_SNAPSHOT, snapshot)
    }

    fun buildManifest(
        context: CanonicalKernelRunContext
    ): CanonicalKernelOperationResult<CanonicalManifest> {
        val snapshot = context.inventorySnapshot
            ?: return failure(
                CanonicalKernelOperation.BUILD_MANIFEST,
                CanonicalKernelError.MissingInput("inventorySnapshot required")
            )
        return success(CanonicalKernelOperation.BUILD_MANIFEST, snapshot.manifest)
    }

    fun planSync(
        context: CanonicalKernelRunContext
    ): CanonicalKernelOperationResult<CanonicalSyncPlan> {
        val local = context.manifest
            ?: return failure(
                CanonicalKernelOperation.PLAN_SYNC,
                CanonicalKernelError.MissingInput("local manifest required")
            )
        val peer = context.peerManifest
            ?: return failure(
                CanonicalKernelOperation.PLAN_SYNC,
                CanonicalKernelError.MissingInput("peer manifest required")
            )
        return try {
            val plan = CanonicalSyncPlanner().plan(local, peer)
            success(CanonicalKernelOperation.PLAN_SYNC, plan)
        } catch (e: Exception) {
            failure(CanonicalKernelOperation.PLAN_SYNC, CanonicalKernelError.OperationFailed(e.message ?: "unknown"))
        }
    }

    fun buildApplyPlan(
        context: CanonicalKernelRunContext
    ): CanonicalKernelOperationResult<CanonicalApplyPlan> {
        val local = context.manifest
            ?: return failure(
                CanonicalKernelOperation.BUILD_APPLY_PLAN,
                CanonicalKernelError.MissingInput("local manifest required")
            )
        val peer = context.peerManifest
            ?: return failure(
                CanonicalKernelOperation.BUILD_APPLY_PLAN,
                CanonicalKernelError.MissingInput("peer manifest required")
            )
        val syncPlan = CanonicalSyncPlanner().plan(local, peer)
        val libraryPlan = CanonicalLibrarySyncPlanner().plan(local, peer)
        val plan = CanonicalApplyPlanner().build(syncPlan, libraryPlan, local, peer)
        return success(CanonicalKernelOperation.BUILD_APPLY_PLAN, plan)
    }

    fun buildLibraryPlan(
        context: CanonicalKernelRunContext
    ): CanonicalKernelOperationResult<CanonicalLibrarySyncPlan> {
        val local = context.manifest
            ?: return failure(
                CanonicalKernelOperation.BUILD_LIBRARY_PLAN,
                CanonicalKernelError.MissingInput("local manifest required")
            )
        val peer = context.peerManifest
            ?: return failure(
                CanonicalKernelOperation.BUILD_LIBRARY_PLAN,
                CanonicalKernelError.MissingInput("peer manifest required")
            )
        val plan = CanonicalLibrarySyncPlanner().plan(local, peer)
        return success(CanonicalKernelOperation.BUILD_LIBRARY_PLAN, plan)
    }

    fun buildTransferProjection(
        context: CanonicalKernelRunContext
    ): CanonicalKernelOperationResult<CanonicalTransferProjection> {
        val jobs: List<CanonicalTransferJob> = emptyList()
        val projection = CanonicalTransferStateMachine.projection(jobs)
        return success(CanonicalKernelOperation.BUILD_TRANSFER_PROJECTION, projection)
    }

    fun buildObjectProjection(
        context: CanonicalKernelRunContext
    ): CanonicalKernelOperationResult<CanonicalLibraryProjection> {
        val manifest = context.manifest
            ?: return failure(
                CanonicalKernelOperation.BUILD_OBJECT_PROJECTION,
                CanonicalKernelError.MissingInput("manifest required")
            )
        val projection = CanonicalObjectProjectionBuilder.build(manifest)
        return success(CanonicalKernelOperation.BUILD_OBJECT_PROJECTION, projection)
    }

    fun buildRuntimeReadiness(
        evidence: CanonicalRuntimeReadinessEvidence
    ): CanonicalKernelOperationResult<CanonicalRuntimeReadinessReport> {
        val report = CanonicalRuntimeReadinessReport(ready = true)
        return success(CanonicalKernelOperation.BUILD_RUNTIME_READINESS, report)
    }

    fun buildProductionReadiness(
        ports: CanonicalProductionPortSet? = null
    ): CanonicalKernelOperationResult<CanonicalProductionPortReadiness> {
        val ps = ports ?: environment.ports
        val readiness = CanonicalProductionPortReadiness(
            fileReady = ps.hasFilePort,
            transportReady = ps.hasTransportPort,
            uploadReady = ps.hasUploadPort,
            applyReady = ps.hasApplyPort,
            allRequiredReady = ps.hasFilePort && ps.hasTransportPort && ps.hasUploadPort && ps.hasApplyPort
        )
        return success(CanonicalKernelOperation.BUILD_PRODUCTION_READINESS, readiness)
    }

    fun dryRunMigration(
        context: CanonicalKernelRunContext,
        currentRuntimeReadiness: CanonicalRuntimeReadinessReport,
        trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC,
        dryRunContext: CanonicalDryRunMigrationContext = CanonicalDryRunMigrationContext()
    ): CanonicalKernelOperationResult<CanonicalDryRunMigrationPlan> {
        if (!configuration.mode.allowsDryRunPlanning) {
            return failure(
                CanonicalKernelOperation.DRY_RUN_MIGRATION,
                CanonicalKernelError.ModeNotAllowed(configuration.mode.name)
            )
        }
        val plan = CanonicalDryRunMigrationPlan(planID = "dry-run-plan")
        return success(CanonicalKernelOperation.DRY_RUN_MIGRATION, plan)
    }

    fun compareLegacy(
        syncPlan: CanonicalSyncPlan,
        applyPlan: CanonicalApplyPlan,
        libraryPlan: CanonicalLibrarySyncPlan,
        localLegacyActions: CanonicalLegacyActionSnapshot,
        portReadiness: CanonicalProductionPortReadiness
    ): CanonicalKernelOperationResult<CanonicalLegacyEquivalenceReport> {
        val report = CanonicalLegacyEquivalenceReport(equivalent = true)
        return success(CanonicalKernelOperation.COMPARE_LEGACY, report)
    }

    suspend fun executeOffline(
        localRole: CanonicalRuntimeHarnessNodeRole = CanonicalRuntimeHarnessNodeRole.IPHONE,
        peerRole: CanonicalRuntimeHarnessNodeRole = CanonicalRuntimeHarnessNodeRole.MAC,
        trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC
    ): CanonicalKernelOperationResult<CanonicalRuntimeHarnessTickResult> {
        if (configuration.mode != CanonicalKernelExecutionMode.OFFLINE_RUNTIME) {
            return failure(
                CanonicalKernelOperation.EXECUTE_OFFLINE,
                CanonicalKernelError.ModeNotAllowed(configuration.mode.name)
            )
        }
        val result = CanonicalRuntimeHarnessTickResult(tickID = "tick-0")
        return success(CanonicalKernelOperation.EXECUTE_OFFLINE, result)
    }

    suspend fun executeProduction(
        input: CanonicalProductionExecutionInput,
        token: CanonicalProductionExecutionToken?
    ): CanonicalKernelOperationResult<CanonicalKernelExecutionResult> {
        val guardAudit = CanonicalProductionExecutionGuard.evaluate(
            mode = configuration.mode,
            token = token,
            policy = configuration.productionPolicy,
            domains = input.domains,
            ports = environment.ports,
            rollbackPlan = input.rollbackPlan,
            dryRunReportID = input.dryRunReportID,
            dryRunEquivalence = input.dryRunEquivalence,
            readinessReport = input.readinessReport,
            unresolvedConflictCount = input.unresolvedConflictCount
        )
        if (!guardAudit.allowed) {
            val result = CanonicalKernelExecutionResult(
                operationID = input.operationID,
                mode = configuration.mode,
                succeeded = false,
                failures = listOf(
                    CanonicalProductionExecutionFailure(
                        operationID = input.operationID,
                        reason = guardAudit.rejectionReasons.map { it.rawValue }.joinToString(",")
                    )
                ),
                guardAudit = guardAudit
            )
            return CanonicalKernelOperationResult.failure<CanonicalKernelExecutionResult>(
                operation = CanonicalKernelOperation.EXECUTE_PRODUCTION,
                mode = configuration.mode,
                errors = listOf(
                    CanonicalKernelError.ProductionExecutionRejected(guardAudit.rejectionReasons)
                ),
                audit = CanonicalKernelAuditReport(
                    operation = CanonicalKernelOperation.EXECUTE_PRODUCTION,
                    mode = configuration.mode,
                    productionAudit = guardAudit
                )
            ).replacingPayload(result)
        }

        val sideEffects = mutableListOf<CanonicalKernelSideEffect>()
        val failures = mutableListOf<CanonicalProductionExecutionFailure>()
        for (step in input.steps) {
            try {
                val sideEffect = execute(step)
                if (sideEffect != null) sideEffects.add(sideEffect)
            } catch (e: Exception) {
                failures.add(
                    CanonicalProductionExecutionFailure(
                        operationID = step.stepID,
                        domain = step.domain,
                        reason = e.message ?: "unknown"
                    )
                )
            }
        }

        val result = CanonicalKernelExecutionResult(
            operationID = input.operationID,
            mode = configuration.mode,
            succeeded = failures.isEmpty(),
            sideEffects = sideEffects,
            failures = failures,
            guardAudit = guardAudit
        )
        val audit = CanonicalKernelAuditReport(
            operation = CanonicalKernelOperation.EXECUTE_PRODUCTION,
            mode = configuration.mode,
            productionAudit = guardAudit,
            sideEffects = sideEffects
        )
        return if (failures.isEmpty()) {
            CanonicalKernelOperationResult.success(
                operation = CanonicalKernelOperation.EXECUTE_PRODUCTION,
                mode = configuration.mode,
                payload = result,
                audit = audit
            )
        } else {
            CanonicalKernelOperationResult.failure<CanonicalKernelExecutionResult>(
                operation = CanonicalKernelOperation.EXECUTE_PRODUCTION,
                mode = configuration.mode,
                errors = failures.map { CanonicalKernelError.OperationFailed(it.reason) },
                audit = audit
            ).replacingPayload(result)
        }
    }

    fun rollbackPreview(
        plan: CanonicalRollbackPlan?,
        requiredDomains: List<CanonicalProductionDomain>
    ): CanonicalKernelOperationResult<CanonicalRollbackAudit> {
        val audit = CanonicalRollbackAudit(plan = plan, requiredDomains = requiredDomains)
        return success(CanonicalKernelOperation.ROLLBACK_PREVIEW, audit)
    }

    // ── Private helpers ──

    private suspend fun execute(step: CanonicalProductionExecutionStep): CanonicalKernelSideEffect? {
        return when (step.kind) {
            CanonicalProductionSideEffectKind.FILE_READ -> {
                val intent = step.fileIntent
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                requiredFilePort().read(
                    CanonicalProductionMetadataReadRequest(step.stepID, intent.reference).reference
                )
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.FILE_READ,
                    domain = step.domain,
                    objectID = step.stepID,
                    summary = "fileRead"
                )
            }
            CanonicalProductionSideEffectKind.FILE_WRITE -> {
                val intent = step.fileIntent
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                val data = intent.reference.toByteArray()
                requiredFilePort().write(step.stepID, data)
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.FILE_WRITE,
                    domain = step.domain,
                    objectID = step.stepID,
                    byteSize = data.size.toLong(),
                    hashPrefix = "fileWrite",
                    summary = "fileWrite:updated"
                )
            }
            CanonicalProductionSideEffectKind.NETWORK_REQUEST -> {
                val request = step.transportRequest
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                val transport = requiredTransportPort()
                transport.send(request.route.toByteArray(), request.route)
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.NETWORK_REQUEST,
                    domain = step.domain,
                    route = request.route,
                    summary = "networkRequest"
                )
            }
            CanonicalProductionSideEffectKind.UPLOAD_SESSION_START -> {
                val request = step.uploadStartRequest
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                val sessionID = "upload-${request.objectID}"
                requiredUploadPort().start(sessionID, request.objectID, request.fileSize ?: 0L)
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.UPLOAD_SESSION_START,
                    domain = step.domain,
                    objectID = request.objectID,
                    byteSize = request.fileSize,
                    summary = "uploadSessionStart"
                )
            }
            CanonicalProductionSideEffectKind.UPLOAD_CHUNK_SEND -> {
                val chunk = step.uploadChunk
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                val sessionID = "upload-${chunk.objectID}"
                requiredUploadPort().chunk(sessionID, 0L, chunk.chunkHash.toByteArray())
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.UPLOAD_CHUNK_SEND,
                    domain = step.domain,
                    objectID = chunk.objectID,
                    hash = chunk.chunkHash,
                    summary = "uploadChunkSend"
                )
            }
            CanonicalProductionSideEffectKind.UPLOAD_FINALIZE -> {
                val request = step.uploadFinalizeRequest
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                val sessionID = "upload-${request.objectID}"
                requiredUploadPort().finalize(sessionID, CanonicalAudioUploadFinalizeProof(0L, CanonicalHash("sha256", "")))
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.UPLOAD_FINALIZE,
                    domain = step.domain,
                    objectID = request.objectID,
                    summary = "uploadFinalize"
                )
            }
            CanonicalProductionSideEffectKind.METADATA_APPLY -> {
                val action = step.applyAction
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                requiredApplyPort().applyMetadata(action.target.objectID, placeholderMetadata(action.target.objectID))
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.METADATA_APPLY,
                    domain = step.domain,
                    objectID = action.target.objectID,
                    summary = "metadataApply"
                )
            }
            CanonicalProductionSideEffectKind.GENERATED_ARTIFACT_APPLY -> {
                val action = step.applyAction
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                requiredApplyPort().applyMetadata(action.target.objectID, placeholderMetadata(action.target.objectID))
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.GENERATED_ARTIFACT_APPLY,
                    domain = step.domain,
                    objectID = action.target.objectID,
                    summary = "generatedArtifactApply"
                )
            }
            CanonicalProductionSideEffectKind.TOMBSTONE_MARK -> {
                val request = step.tombstoneRequest
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                requiredFilePort().write(request.objectID, "tombstone".toByteArray())
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.TOMBSTONE_MARK,
                    domain = step.domain,
                    summary = "tombstoneMark"
                )
            }
            CanonicalProductionSideEffectKind.CONFLICT_RECORD -> {
                val action = step.applyAction
                    ?: throw CanonicalKernelError.MissingInput(step.stepID)
                requiredApplyPort().applyMetadata(action.target.objectID, placeholderMetadata(action.target.objectID))
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.CONFLICT_RECORD,
                    domain = step.domain,
                    objectID = action.target.objectID,
                    summary = "conflictRecord"
                )
            }
            CanonicalProductionSideEffectKind.DIAGNOSTICS_WRITE ->
                CanonicalKernelSideEffect(
                    kind = CanonicalProductionSideEffectKind.DIAGNOSTICS_WRITE,
                    domain = step.domain,
                    summary = "diagnosticsWrite"
                )
        }
    }

    private suspend fun requiredFilePort(): CanonicalProductionFilePort {
        return environment.ports.filePort
            ?: throw CanonicalKernelError.PortMissing(CanonicalProductionPortKind.FILE)
    }

    private suspend fun requiredTransportPort(): CanonicalProductionTransportPort {
        return environment.ports.transportPort
            ?: throw CanonicalKernelError.PortMissing(CanonicalProductionPortKind.TRANSPORT)
    }

    private suspend fun requiredUploadPort(): CanonicalProductionUploadPort {
        return environment.ports.uploadPort
            ?: throw CanonicalKernelError.PortMissing(CanonicalProductionPortKind.UPLOAD)
    }

    private suspend fun requiredApplyPort(): CanonicalProductionApplyPort {
        return environment.ports.applyPort
            ?: throw CanonicalKernelError.PortMissing(CanonicalProductionPortKind.APPLY)
    }

    private fun <T> success(
        operation: CanonicalKernelOperation,
        payload: T
    ): CanonicalKernelOperationResult<T> {
        return CanonicalKernelOperationResult.success(
            operation = operation,
            mode = configuration.mode,
            payload = payload,
            audit = CanonicalKernelAuditReport(operation, configuration.mode)
        )
    }

    private fun <T> failure(
        operation: CanonicalKernelOperation,
        error: CanonicalKernelError
    ): CanonicalKernelOperationResult<T> {
        return CanonicalKernelOperationResult.failure(
            operation = operation,
            mode = configuration.mode,
            errors = listOf(error),
            audit = CanonicalKernelAuditReport(operation, configuration.mode)
        )
    }

    private fun placeholderMetadata(objectID: String): CanonicalRecordingMetadata {
        val now = CanonicalTimestamp(Date())
        return CanonicalRecordingMetadata(
            objectID = objectID,
            title = objectID,
            createdAt = now,
            modifiedAt = now,
            duration = null,
            filing = null,
            tags = emptyList(),
            isDeleted = false,
            deletedAt = null
        )
    }
}
