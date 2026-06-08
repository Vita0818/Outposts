package com.rokurics.app.domain.canonical

import java.util.Date

// ── Type 1: CanonicalRuntimeReadinessGateMode ──

enum class CanonicalRuntimeReadinessGateMode(val rawValue: String) {
    DISABLED("disabled"),
    EVALUATE_ONLY("evaluateOnly");

    val isEnabled: Boolean
        get() = this != DISABLED
}

// ── Type 2: CanonicalRuntimeReadinessCapability ──

enum class CanonicalRuntimeReadinessCapability(val rawValue: String) {
    INVENTORY_SNAPSHOT_AVAILABLE("inventorySnapshotAvailable"),
    LOCAL_MANIFEST_VALID("localManifestValid"),
    PEER_MANIFEST_VALID("peerManifestValid"),
    SCHEMA_VERSION_MATCH("schemaVersionMatch"),
    MODIFIED_AT_SEMANTICS_AVAILABLE("modifiedAtSemanticsAvailable"),
    LEGACY_FALLBACK_AVAILABLE("legacyFallbackAvailable"),
    DIAGNOSTICS_REDACTED("diagnosticsRedacted"),
    RUNTIME_SWITCH_DISABLED("runtimeSwitchDisabled"),
    READ_PATH_LEGACY("readPathLegacy"),
    NO_OTHER_MIGRATION_CONFLICT("noOtherMigrationConflict"),
    DEAD_PEER_DETECTION_AVAILABLE("deadPeerDetectionAvailable"),
    SYNC_PLAN_AUTHORITY_READY("syncPlanAuthorityReady"),
    APPLY_RUNTIME_READY("applyRuntimeReady"),
    EXISTENCE_RUNTIME_READY("existenceRuntimeReady"),
    AUDIO_UPLOAD_RUNTIME_READY("audioUploadRuntimeReady"),
    READ_RUNTIME_READY("readRuntimeReady"),
    FILE_PORT_READY("filePortReady"),
    TRANSPORT_PORT_READY("transportPortReady"),
    UPLOAD_PORT_READY("uploadPortReady"),
    APPLY_PORT_READY("applyPortReady")
}

// ── Type 3: CanonicalRuntimeReadinessGateResult ──

data class CanonicalRuntimeReadinessGateResult(
    val ready: Boolean,
    val missingCapabilities: List<CanonicalRuntimeReadinessCapability>,
    val presentCapabilities: List<CanonicalRuntimeReadinessCapability>,
    val diagnosticsSummary: String,
    val evaluatedAt: CanonicalTimestamp
) {
    val readyCapabilityCount: Int
        get() = presentCapabilities.size

    val missingCapabilityCount: Int
        get() = missingCapabilities.size

    val totalCapabilityCount: Int
        get() = readyCapabilityCount + missingCapabilityCount

    val readinessPercentage: Double
        get() = if (totalCapabilityCount > 0)
            readyCapabilityCount.toDouble() / totalCapabilityCount.toDouble()
        else 0.0

    companion object {
        fun make(
            missing: List<CanonicalRuntimeReadinessCapability>,
            present: List<CanonicalRuntimeReadinessCapability>,
            evaluatedAt: Date = Date()
        ): CanonicalRuntimeReadinessGateResult {
            val sortedMissing = missing.toSet().sortedBy { it.rawValue }
            val sortedPresent = present.toSet().sortedBy { it.rawValue }
            val isReady = sortedMissing.isEmpty()
            val summary = listOf(
                "runtimeReadinessGate=v1",
                "ready=$isReady",
                "missing=${sortedMissing.joinToString("+") { it.rawValue }.nilIfEmpty ?: "none"}",
                "present=${sortedPresent.size}"
            ).joinToString(",")
            return CanonicalRuntimeReadinessGateResult(
                ready = isReady,
                missingCapabilities = sortedMissing,
                presentCapabilities = sortedPresent,
                diagnosticsSummary = summary,
                evaluatedAt = CanonicalTimestamp(evaluatedAt)
            )
        }
    }
}

// ── Type 4: CanonicalRuntimeReadinessContext ──

data class CanonicalRuntimeReadinessContext(
    val inventorySnapshotAvailable: Boolean,
    val localManifestValid: Boolean,
    val peerManifestValid: Boolean,
    val schemaVersionMatch: Boolean,
    val modifiedAtSemanticsAvailable: Boolean,
    val legacyFallbackAvailable: Boolean,
    val diagnosticsRedacted: Boolean,
    val runtimeSwitchEnabled: Boolean,
    val readPathLegacy: Boolean,
    val otherMigrationDomainConflicting: Boolean,
    val deadPeerDetectionAvailable: Boolean,
    val syncPlanAuthorityReady: Boolean,
    val applyRuntimeReady: Boolean,
    val existenceRuntimeReady: Boolean,
    val audioUploadRuntimeReady: Boolean,
    val readRuntimeReady: Boolean,
    val filePortReady: Boolean,
    val transportPortReady: Boolean,
    val uploadPortReady: Boolean,
    val applyPortReady: Boolean
) {
    companion object {
        fun allReady(): CanonicalRuntimeReadinessContext {
            return CanonicalRuntimeReadinessContext(
                inventorySnapshotAvailable = true,
                localManifestValid = true,
                peerManifestValid = true,
                schemaVersionMatch = true,
                modifiedAtSemanticsAvailable = true,
                legacyFallbackAvailable = true,
                diagnosticsRedacted = true,
                runtimeSwitchEnabled = false,
                readPathLegacy = true,
                otherMigrationDomainConflicting = false,
                deadPeerDetectionAvailable = true,
                syncPlanAuthorityReady = true,
                applyRuntimeReady = true,
                existenceRuntimeReady = true,
                audioUploadRuntimeReady = false,
                readRuntimeReady = true,
                filePortReady = true,
                transportPortReady = true,
                uploadPortReady = true,
                applyPortReady = true
            )
        }

        fun noneReady(): CanonicalRuntimeReadinessContext {
            return CanonicalRuntimeReadinessContext(
                inventorySnapshotAvailable = false,
                localManifestValid = false,
                peerManifestValid = false,
                schemaVersionMatch = false,
                modifiedAtSemanticsAvailable = false,
                legacyFallbackAvailable = false,
                diagnosticsRedacted = false,
                runtimeSwitchEnabled = true,
                readPathLegacy = false,
                otherMigrationDomainConflicting = true,
                deadPeerDetectionAvailable = false,
                syncPlanAuthorityReady = false,
                applyRuntimeReady = false,
                existenceRuntimeReady = false,
                audioUploadRuntimeReady = false,
                readRuntimeReady = false,
                filePortReady = false,
                transportPortReady = false,
                uploadPortReady = false,
                applyPortReady = false
            )
        }
    }
}

// ── Type 5: CanonicalRuntimeReadinessEvaluator ──

class CanonicalRuntimeReadinessEvaluator(
    private val mode: CanonicalRuntimeReadinessGateMode = CanonicalRuntimeReadinessGateMode.EVALUATE_ONLY
) {

    data class ReadinessEvaluationResult(
        val result: CanonicalRuntimeReadinessGateResult,
        val sectionResults: List<CapabilitySectionResult>,
        val allSectionsReady: Boolean
    )

    enum class CapabilitySection(val rawValue: String) {
        INVENTORY_AND_MANIFEST("inventoryAndManifest"),
        RUNTIME_SWITCH_CONTEXT("runtimeSwitchContext"),
        DEAD_PEER_DETECTION("deadPeerDetection"),
        PLAN_AND_APPLY_RUNTIME("planAndApplyRuntime"),
        AUDIO_UPLOAD_RUNTIME("audioUploadRuntime"),
        READ_RUNTIME("readRuntime"),
        PRODUCTION_PORTS("productionPorts")
    }

    data class CapabilitySectionResult(
        val section: CapabilitySection,
        val ready: Boolean,
        val capabilities: List<CanonicalRuntimeReadinessCapability>,
        val missing: List<CanonicalRuntimeReadinessCapability>
    ) {
        val diagnosticsSummary: String
            get() = "section=${section.rawValue},ready=$ready,missing=${missing.joinToString("+") { it.rawValue }.nilIfEmpty ?: "none"}"
    }

    fun evaluate(context: CanonicalRuntimeReadinessContext): ReadinessEvaluationResult {
        if (!mode.isEnabled) {
            val emptyResult = CanonicalRuntimeReadinessGateResult.make(
                missing = emptyList(),
                present = emptyList()
            )
            return ReadinessEvaluationResult(
                result = emptyResult,
                sectionResults = emptyList(),
                allSectionsReady = true
            )
        }

        val sections = evaluateAllSections(context)
        val allMissing = sections.flatMap { it.missing }.toSet().sortedBy { it.rawValue }
        val allPresent = CanonicalRuntimeReadinessCapability.entries
            .filter { it !in allMissing }
            .sortedBy { it.rawValue }
        val result = CanonicalRuntimeReadinessGateResult.make(
            missing = allMissing,
            present = allPresent
        )
        return ReadinessEvaluationResult(
            result = result,
            sectionResults = sections,
            allSectionsReady = sections.all { it.ready }
        )
    }

    private fun evaluateAllSections(
        context: CanonicalRuntimeReadinessContext
    ): List<CapabilitySectionResult> {
        return listOf(
            evaluateInventoryAndManifest(context),
            evaluateRuntimeSwitchContext(context),
            evaluateDeadPeerDetection(context),
            evaluatePlanAndApplyRuntime(context),
            evaluateAudioUploadRuntime(context),
            evaluateReadRuntime(context),
            evaluateProductionPorts(context)
        )
    }

    private fun evaluateInventoryAndManifest(
        context: CanonicalRuntimeReadinessContext
    ): CapabilitySectionResult {
        val capabilities = listOf(
            CanonicalRuntimeReadinessCapability.INVENTORY_SNAPSHOT_AVAILABLE,
            CanonicalRuntimeReadinessCapability.LOCAL_MANIFEST_VALID,
            CanonicalRuntimeReadinessCapability.PEER_MANIFEST_VALID,
            CanonicalRuntimeReadinessCapability.SCHEMA_VERSION_MATCH,
            CanonicalRuntimeReadinessCapability.MODIFIED_AT_SEMANTICS_AVAILABLE
        )
        val missing = mutableListOf<CanonicalRuntimeReadinessCapability>()
        if (!context.inventorySnapshotAvailable) {
            missing.add(CanonicalRuntimeReadinessCapability.INVENTORY_SNAPSHOT_AVAILABLE)
        }
        if (!context.localManifestValid) {
            missing.add(CanonicalRuntimeReadinessCapability.LOCAL_MANIFEST_VALID)
        }
        if (!context.peerManifestValid) {
            missing.add(CanonicalRuntimeReadinessCapability.PEER_MANIFEST_VALID)
        }
        if (!context.schemaVersionMatch) {
            missing.add(CanonicalRuntimeReadinessCapability.SCHEMA_VERSION_MATCH)
        }
        if (!context.modifiedAtSemanticsAvailable) {
            missing.add(CanonicalRuntimeReadinessCapability.MODIFIED_AT_SEMANTICS_AVAILABLE)
        }
        return CapabilitySectionResult(
            section = CapabilitySection.INVENTORY_AND_MANIFEST,
            ready = missing.isEmpty(),
            capabilities = capabilities,
            missing = missing
        )
    }

    private fun evaluateRuntimeSwitchContext(
        context: CanonicalRuntimeReadinessContext
    ): CapabilitySectionResult {
        val capabilities = listOf(
            CanonicalRuntimeReadinessCapability.LEGACY_FALLBACK_AVAILABLE,
            CanonicalRuntimeReadinessCapability.DIAGNOSTICS_REDACTED,
            CanonicalRuntimeReadinessCapability.RUNTIME_SWITCH_DISABLED,
            CanonicalRuntimeReadinessCapability.READ_PATH_LEGACY,
            CanonicalRuntimeReadinessCapability.NO_OTHER_MIGRATION_CONFLICT
        )
        val missing = mutableListOf<CanonicalRuntimeReadinessCapability>()
        if (!context.legacyFallbackAvailable) {
            missing.add(CanonicalRuntimeReadinessCapability.LEGACY_FALLBACK_AVAILABLE)
        }
        if (!context.diagnosticsRedacted) {
            missing.add(CanonicalRuntimeReadinessCapability.DIAGNOSTICS_REDACTED)
        }
        if (context.runtimeSwitchEnabled) {
            missing.add(CanonicalRuntimeReadinessCapability.RUNTIME_SWITCH_DISABLED)
        }
        if (!context.readPathLegacy) {
            missing.add(CanonicalRuntimeReadinessCapability.READ_PATH_LEGACY)
        }
        if (context.otherMigrationDomainConflicting) {
            missing.add(CanonicalRuntimeReadinessCapability.NO_OTHER_MIGRATION_CONFLICT)
        }
        return CapabilitySectionResult(
            section = CapabilitySection.RUNTIME_SWITCH_CONTEXT,
            ready = missing.isEmpty(),
            capabilities = capabilities,
            missing = missing
        )
    }

    private fun evaluateDeadPeerDetection(
        context: CanonicalRuntimeReadinessContext
    ): CapabilitySectionResult {
        val capabilities = listOf(
            CanonicalRuntimeReadinessCapability.DEAD_PEER_DETECTION_AVAILABLE
        )
        val missing = mutableListOf<CanonicalRuntimeReadinessCapability>()
        if (!context.deadPeerDetectionAvailable) {
            missing.add(CanonicalRuntimeReadinessCapability.DEAD_PEER_DETECTION_AVAILABLE)
        }
        return CapabilitySectionResult(
            section = CapabilitySection.DEAD_PEER_DETECTION,
            ready = missing.isEmpty(),
            capabilities = capabilities,
            missing = missing
        )
    }

    private fun evaluatePlanAndApplyRuntime(
        context: CanonicalRuntimeReadinessContext
    ): CapabilitySectionResult {
        val capabilities = listOf(
            CanonicalRuntimeReadinessCapability.SYNC_PLAN_AUTHORITY_READY,
            CanonicalRuntimeReadinessCapability.APPLY_RUNTIME_READY,
            CanonicalRuntimeReadinessCapability.EXISTENCE_RUNTIME_READY
        )
        val missing = mutableListOf<CanonicalRuntimeReadinessCapability>()
        if (!context.syncPlanAuthorityReady) {
            missing.add(CanonicalRuntimeReadinessCapability.SYNC_PLAN_AUTHORITY_READY)
        }
        if (!context.applyRuntimeReady) {
            missing.add(CanonicalRuntimeReadinessCapability.APPLY_RUNTIME_READY)
        }
        if (!context.existenceRuntimeReady) {
            missing.add(CanonicalRuntimeReadinessCapability.EXISTENCE_RUNTIME_READY)
        }
        return CapabilitySectionResult(
            section = CapabilitySection.PLAN_AND_APPLY_RUNTIME,
            ready = missing.isEmpty(),
            capabilities = capabilities,
            missing = missing
        )
    }

    private fun evaluateAudioUploadRuntime(
        context: CanonicalRuntimeReadinessContext
    ): CapabilitySectionResult {
        val capabilities = listOf(
            CanonicalRuntimeReadinessCapability.AUDIO_UPLOAD_RUNTIME_READY
        )
        val missing = mutableListOf<CanonicalRuntimeReadinessCapability>()
        if (!context.audioUploadRuntimeReady) {
            missing.add(CanonicalRuntimeReadinessCapability.AUDIO_UPLOAD_RUNTIME_READY)
        }
        return CapabilitySectionResult(
            section = CapabilitySection.AUDIO_UPLOAD_RUNTIME,
            ready = missing.isEmpty(),
            capabilities = capabilities,
            missing = missing
        )
    }

    private fun evaluateReadRuntime(
        context: CanonicalRuntimeReadinessContext
    ): CapabilitySectionResult {
        val capabilities = listOf(
            CanonicalRuntimeReadinessCapability.READ_RUNTIME_READY
        )
        val missing = mutableListOf<CanonicalRuntimeReadinessCapability>()
        if (!context.readRuntimeReady) {
            missing.add(CanonicalRuntimeReadinessCapability.READ_RUNTIME_READY)
        }
        return CapabilitySectionResult(
            section = CapabilitySection.READ_RUNTIME,
            ready = missing.isEmpty(),
            capabilities = capabilities,
            missing = missing
        )
    }

    private fun evaluateProductionPorts(
        context: CanonicalRuntimeReadinessContext
    ): CapabilitySectionResult {
        val capabilities = listOf(
            CanonicalRuntimeReadinessCapability.FILE_PORT_READY,
            CanonicalRuntimeReadinessCapability.TRANSPORT_PORT_READY,
            CanonicalRuntimeReadinessCapability.UPLOAD_PORT_READY,
            CanonicalRuntimeReadinessCapability.APPLY_PORT_READY
        )
        val missing = mutableListOf<CanonicalRuntimeReadinessCapability>()
        if (!context.filePortReady) {
            missing.add(CanonicalRuntimeReadinessCapability.FILE_PORT_READY)
        }
        if (!context.transportPortReady) {
            missing.add(CanonicalRuntimeReadinessCapability.TRANSPORT_PORT_READY)
        }
        if (!context.uploadPortReady) {
            missing.add(CanonicalRuntimeReadinessCapability.UPLOAD_PORT_READY)
        }
        if (!context.applyPortReady) {
            missing.add(CanonicalRuntimeReadinessCapability.APPLY_PORT_READY)
        }
        return CapabilitySectionResult(
            section = CapabilitySection.PRODUCTION_PORTS,
            ready = missing.isEmpty(),
            capabilities = capabilities,
            missing = missing
        )
    }
}
