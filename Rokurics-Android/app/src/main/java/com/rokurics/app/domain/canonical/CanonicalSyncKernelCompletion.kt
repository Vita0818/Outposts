package com.rokurics.app.domain.canonical

import java.util.Date

// ── Type 1: CanonicalSyncKernelCompletionStatus ──

enum class CanonicalSyncKernelCompletionStatus(val rawValue: String) {
    INCOMPLETE("incomplete"),
    CODE_COMPLETE_NEEDS_DEVICE_EVIDENCE("codeCompleteNeedsDeviceEvidence"),
    READY_FOR_MANUAL_SWITCH_TRIAL("readyForManualSwitchTrial"),
    BLOCKED("blocked"),
    READY_TO_RETIRE_LEGACY_REPORT_ONLY("readyToRetireLegacyReportOnly"),
    UNSAFE("unsafe")
}

// ── Type 2: CanonicalSyncKernelCompletionDomain ──

enum class CanonicalSyncKernelCompletionDomain(val rawValue: String) {
    RECORDING_METADATA("recordingMetadata"),
    LIBRARY_METADATA("libraryMetadata"),
    GENERATED_ARTIFACTS("generatedArtifacts"),
    TOMBSTONE_CONFLICT("tombstoneConflict"),
    RECORDING_EXISTENCE("recordingExistence"),
    AUDIO_UPLOAD("audioUpload"),
    READ_RUNTIME("readRuntime"),
    INVENTORY_RUNTIME("inventoryRuntime"),
    SYNC_DECISION_RUNTIME("syncDecisionRuntime"),
    APPLY_RUNTIME("applyRuntime"),
    KERNEL_SWITCH("kernelSwitch"),
    LEGACY_COMPATIBILITY("legacyCompatibility")
}

// ── Type 3: CanonicalSyncKernelCompletionDomainReadiness ──

data class CanonicalSyncKernelCompletionDomainReadiness(
    val domain: CanonicalSyncKernelCompletionDomain,
    val writeExecutorReady: Boolean,
    val readCutoverReady: Boolean,
    val runtimeOwnerReady: Boolean,
    val legacyFallback: Boolean,
    val switchbackProof: Boolean,
    val diagnosticsClean: Boolean,
    val realDeviceEvidence: Boolean,
    val readyToRetire: Boolean
) {
    val isFullyReady: Boolean
        get() = writeExecutorReady && readCutoverReady && runtimeOwnerReady &&
                switchbackProof && diagnosticsClean && realDeviceEvidence

    val diagnosticsSummary: String
        get() = listOf(
            "domain=${domain.rawValue}",
            "writeExecutor=$writeExecutorReady",
            "readCutover=$readCutoverReady",
            "runtimeOwner=$runtimeOwnerReady",
            "legacyFallback=$legacyFallback",
            "switchbackProof=$switchbackProof",
            "diagnosticsClean=$diagnosticsClean",
            "realDeviceEvidence=$realDeviceEvidence",
            "readyToRetire=$readyToRetire"
        ).joinToString(",")
}

// ── Type 4: CanonicalSyncKernelCompletionScorecard ──

data class CanonicalSyncKernelCompletionScorecard(
    val overallStatus: CanonicalSyncKernelCompletionStatus,
    val domainReadiness: List<CanonicalSyncKernelCompletionDomainReadiness>,
    val generatedAt: CanonicalTimestamp,
    val completionScore: Double,
    val blockedDomains: Int,
    val readyDomains: Int,
    val diagnosticsSummary: String
) {
    val isBlocked: Boolean
        get() = overallStatus == CanonicalSyncKernelCompletionStatus.BLOCKED

    val isUnsafe: Boolean
        get() = overallStatus == CanonicalSyncKernelCompletionStatus.UNSAFE

    val readyToRetire: Boolean
        get() = overallStatus == CanonicalSyncKernelCompletionStatus.READY_TO_RETIRE_LEGACY_REPORT_ONLY

    companion object {
        fun evaluate(
            domainReadiness: List<CanonicalSyncKernelCompletionDomainReadiness>,
            generatedAt: Date = Date()
        ): CanonicalSyncKernelCompletionScorecard {
            val readiness = domainReadiness.sortedBy { it.domain.rawValue }
            val total = readiness.size.toDouble()
            val ready = readiness.count { it.isFullyReady }
            val blocked = readiness.count { !it.legacyFallback && !it.diagnosticsClean }
            val retired = readiness.count { it.readyToRetire }
            val score = if (total > 0.0) ready.toDouble() / total else 0.0

            val hasUnsafe = readiness.any { !it.diagnosticsClean && !it.legacyFallback && !it.switchbackProof }
            val allReadyToRetire = readiness.all { it.readyToRetire }
            val anyBlocked = readiness.any { !it.writeExecutorReady && !it.readCutoverReady && !it.legacyFallback }

            val status = when {
                hasUnsafe -> CanonicalSyncKernelCompletionStatus.UNSAFE
                allReadyToRetire -> CanonicalSyncKernelCompletionStatus.READY_TO_RETIRE_LEGACY_REPORT_ONLY
                anyBlocked -> CanonicalSyncKernelCompletionStatus.BLOCKED
                ready == readiness.size -> CanonicalSyncKernelCompletionStatus.READY_FOR_MANUAL_SWITCH_TRIAL
                ready >= readiness.size - 2 ->
                    CanonicalSyncKernelCompletionStatus.CODE_COMPLETE_NEEDS_DEVICE_EVIDENCE
                else -> CanonicalSyncKernelCompletionStatus.INCOMPLETE
            }

            val summary = listOf(
                "status=${status.rawValue}",
                "score=%.2f".format(score),
                "ready=$ready/$readiness.size",
                "blocked=$blocked",
                "retired=$retired"
            ).joinToString(",")

            return CanonicalSyncKernelCompletionScorecard(
                overallStatus = status,
                domainReadiness = readiness,
                generatedAt = CanonicalTimestamp(generatedAt),
                completionScore = score,
                blockedDomains = blocked,
                readyDomains = ready,
                diagnosticsSummary = summary
            )
        }
    }
}

// ── Type 5: CanonicalSyncKernelDomainReadyToRetireReport ──

data class CanonicalSyncKernelDomainReadyToRetireReport(
    val domain: CanonicalSyncKernelCompletionDomain,
    val legacyStillActive: Boolean,
    val legacyUsageCount: Int,
    val canonicalCoverage: Double,
    val readyToRetire: Boolean,
    val blockers: List<String>,
    val evidenceSummary: String
) {
    companion object {
        fun forDomain(
            domain: CanonicalSyncKernelCompletionDomain,
            legacyUsageCount: Int = 0,
            canonicalCoverage: Double = 0.0,
            blockers: List<String> = emptyList()
        ): CanonicalSyncKernelDomainReadyToRetireReport {
            val ready = legacyUsageCount == 0 && canonicalCoverage >= 1.0 && blockers.isEmpty()
            return CanonicalSyncKernelDomainReadyToRetireReport(
                domain = domain,
                legacyStillActive = legacyUsageCount > 0,
                legacyUsageCount = legacyUsageCount,
                canonicalCoverage = canonicalCoverage,
                readyToRetire = ready,
                blockers = blockers.sorted(),
                evidenceSummary = "domain=${domain.rawValue},coverage=$canonicalCoverage,legacy=$legacyUsageCount,ready=$ready"
            )
        }
    }

    val redactedEvidenceSummary: String
        get() = "domain=${domain.rawValue},coverage=$canonicalCoverage,legacy=$legacyUsageCount,ready=$readyToRetire"
}

// ── Type 6: CanonicalSyncKernelEvidenceExporter ──

object CanonicalSyncKernelEvidenceExporter {

    fun redactedDomainReport(report: CanonicalSyncKernelDomainReadyToRetireReport): String {
        return listOf(
            "domain=${report.domain.rawValue}",
            "canonicalCoverage=%.3f".format(report.canonicalCoverage),
            "legacyUsageCount=${report.legacyUsageCount}",
            "readyToRetire=${report.readyToRetire}",
            "blockers=${report.blockers.joinToString("|").nilIfEmpty ?: "none"}"
        ).joinToString(",")
    }

    fun redactedScorecard(scorecard: CanonicalSyncKernelCompletionScorecard): String {
        val domainLines = scorecard.domainReadiness.map { readiness ->
            listOf(
                readiness.domain.rawValue,
                if (readiness.isFullyReady) "ready" else "notReady",
                readiness.diagnosticsSummary
            ).joinToString(":")
        }
        return listOf(
            "canonicalSyncKernelCompletion=v8.45",
            "status=${scorecard.overallStatus.rawValue}",
            "score=%.3f".format(scorecard.completionScore),
            "readyDomains=${scorecard.readyDomains}",
            "blockedDomains=${scorecard.blockedDomains}",
            "generatedAt=${scorecard.generatedAt.date.time}",
            "domains=${domainLines.joinToString(";")}"
        ).joinToString(",")
    }

    fun redactedCompatibilityMatrix(matrix: CanonicalLegacyCompatibilityResult): String {
        val domainLines = matrix.domains.map { domainResult ->
            listOf(
                domainResult.domain.rawValue,
                "canonicalWritesLegacyReadable=${domainResult.canonicalWritesLegacyReadable}",
                "legacyWritesCanonicalReadable=${domainResult.legacyWritesCanonicalReadable}",
                "switchBackRequiresNoMigration=${domainResult.switchBackRequiresNoMigration}",
                "rollbackAvailable=${domainResult.rollbackAvailable}"
            ).joinToString(":")
        }
        return listOf(
            "canonicalLegacyCompatibility=v8.44",
            "allCompatible=${matrix.allCompatible}",
            "domains=${domainLines.joinToString(";")}"
        ).joinToString(",")
    }
}

// ── Type 7: CanonicalSyncKernelManualSwitchGateBlocker ──

enum class CanonicalSyncKernelManualSwitchGateBlocker(val rawValue: String) {
    KERNEL_SWITCH_RESULT_BLOCKED("kernelSwitchResultBlocked"),
    NOT_REVERSIBLE("notReversible"),
    COMPLETION_NOT_READY("completionNotReady"),
    LEGACY_COMPAT_NOT_PROVEN("legacyCompatNotProven"),
    RUNTIME_READINESS_NOT_PROVEN("runtimeReadinessNotProven"),
    DEVICE_EVIDENCE_MISSING("deviceEvidenceMissing"),
    MANUAL_OWNER_APPROVAL_MISSING("manualOwnerApprovalMissing"),
    DEBUG_INTERNAL_BUILD_REQUIRED("debugInternalBuildRequired"),
    ACTIVE_TRANSFERS_IN_FLIGHT("activeTransfersInFlight"),
    LEGACY_PATH_NOT_AVAILABLE("legacyPathNotAvailable"),
    DIAGNOSTICS_NOT_REDACTED("diagnosticsNotRedacted"),
    RETIREMENT_NOT_READY("retirementNotReady")
}

// ── Type 8: CanonicalSyncKernelManualSwitchGate ──

class CanonicalSyncKernelManualSwitchGate(
    private val completionScorecard: CanonicalSyncKernelCompletionScorecard,
    private val legacyCompatibility: CanonicalLegacyCompatibilityResult?,
    private val switchBackProof: CanonicalKernelSwitchBackProof?,
    private val runtimeReady: Boolean = false,
    private val debugInternalBuild: Boolean = false,
    private val ownerApproved: Boolean = false,
    private val diagnosticsRedacted: Boolean = true,
    private val legacyPathAvailable: Boolean = true,
    private val activeTransfersInFlight: Boolean = false,
    private val allDomainsReadyToRetire: Boolean = false
) {

    data class ManualSwitchGateResult(
        val approved: Boolean,
        val blockers: List<CanonicalSyncKernelManualSwitchGateBlocker>,
        val diagnosticsSummary: String,
        val approvedAt: CanonicalTimestamp
    ) {
        companion object {
            fun blocked(
                blockers: List<CanonicalSyncKernelManualSwitchGateBlocker>
            ): ManualSwitchGateResult {
                val uniqueBlockers = blockers.toSet().sortedBy { it.rawValue }
                return ManualSwitchGateResult(
                    approved = false,
                    blockers = uniqueBlockers,
                    diagnosticsSummary = "approved=false,blockers=${uniqueBlockers.joinToString("+") { it.rawValue }}",
                    approvedAt = CanonicalTimestamp(Date())
                )
            }

            fun approved(): ManualSwitchGateResult {
                return ManualSwitchGateResult(
                    approved = true,
                    blockers = emptyList(),
                    diagnosticsSummary = "approved=true,blockers=none",
                    approvedAt = CanonicalTimestamp(Date())
                )
            }
        }
    }

    fun approve(): ManualSwitchGateResult {
        val blockers = mutableListOf<CanonicalSyncKernelManualSwitchGateBlocker>()

        if (completionScorecard.isBlocked || completionScorecard.isUnsafe) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.KERNEL_SWITCH_RESULT_BLOCKED)
        }
        if (completionScorecard.readyDomains < completionScorecard.domainReadiness.size) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.COMPLETION_NOT_READY)
        }
        if (switchBackProof == null || !switchBackProof.reversible) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.NOT_REVERSIBLE)
        }
        if (legacyCompatibility == null || !legacyCompatibility.allCompatible) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.LEGACY_COMPAT_NOT_PROVEN)
        }
        if (!runtimeReady) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.RUNTIME_READINESS_NOT_PROVEN)
        }
        if (completionScorecard.domainReadiness.any { !it.realDeviceEvidence }) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.DEVICE_EVIDENCE_MISSING)
        }
        if (!ownerApproved) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.MANUAL_OWNER_APPROVAL_MISSING)
        }
        if (!debugInternalBuild) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.DEBUG_INTERNAL_BUILD_REQUIRED)
        }
        if (activeTransfersInFlight) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.ACTIVE_TRANSFERS_IN_FLIGHT)
        }
        if (!legacyPathAvailable) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.LEGACY_PATH_NOT_AVAILABLE)
        }
        if (!diagnosticsRedacted) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.DIAGNOSTICS_NOT_REDACTED)
        }
        if (allDomainsReadyToRetire && !completionScorecard.readyToRetire) {
            blockers.add(CanonicalSyncKernelManualSwitchGateBlocker.RETIREMENT_NOT_READY)
        }

        return if (blockers.isEmpty()) {
            ManualSwitchGateResult.approved()
        } else {
            ManualSwitchGateResult.blocked(blockers)
        }
    }
}
