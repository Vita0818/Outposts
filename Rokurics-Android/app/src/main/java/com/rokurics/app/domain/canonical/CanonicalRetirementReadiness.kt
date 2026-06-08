package com.rokurics.app.domain.canonical

import java.util.Date

// ── Type 1: CanonicalRetirementReadinessDomain ──

enum class CanonicalRetirementReadinessDomain(val rawValue: String) {
    RECORDING_METADATA("recordingMetadata"),
    LIBRARY_METADATA("libraryMetadata"),
    GENERATED_ARTIFACTS("generatedArtifacts"),
    TOMBSTONE_CONFLICT("tombstoneConflict"),
    AUDIO_UPLOAD("audioUpload"),
    SYNC_DECISION_RUNTIME("syncDecisionRuntime"),
    APPLY_RUNTIME("applyRuntime")
}

// ── Type 2: CanonicalRetirementReadinessDomainStatus ──

data class CanonicalRetirementReadinessDomainStatus(
    val domain: CanonicalRetirementReadinessDomain,
    val legacyUsageCount: Int,
    val canonicalCoverage: Double,
    val readyToRetire: Boolean,
    val blockers: List<String>
) {
    val coveragePercentage: Double
        get() = canonicalCoverage * 100.0

    val hasLegacyActivity: Boolean
        get() = legacyUsageCount > 0

    val isFullyCovered: Boolean
        get() = canonicalCoverage >= 1.0

    val diagnosticsSummary: String
        get() = listOf(
            "domain=${domain.rawValue}",
            "legacyUsage=$legacyUsageCount",
            "coverage=%.4f".format(canonicalCoverage),
            "readyToRetire=$readyToRetire",
            "blockers=${blockers.joinToString("+").nilIfEmpty ?: "none"}"
        ).joinToString(",")

    companion object {
        fun evaluate(
            domain: CanonicalRetirementReadinessDomain,
            legacyUsageCount: Int = 0,
            canonicalCoverage: Double = 0.0,
            additionalBlockers: List<String> = emptyList()
        ): CanonicalRetirementReadinessDomainStatus {
            val blockers = mutableListOf<String>()
            if (legacyUsageCount > 0) {
                blockers.add("legacyStillActive:count=$legacyUsageCount")
            }
            if (canonicalCoverage < 0.5) {
                blockers.add("canonicalCoverageBelowThreshold:coverage=${"%.4f".format(canonicalCoverage)}")
            }
            if (canonicalCoverage < 1.0) {
                blockers.add("canonicalCoverageNotComplete:coverage=${"%.4f".format(canonicalCoverage)}")
            }
            blockers.addAll(additionalBlockers)
            val ready = blockers.isEmpty() || (legacyUsageCount == 0 && canonicalCoverage >= 1.0)
            return CanonicalRetirementReadinessDomainStatus(
                domain = domain,
                legacyUsageCount = legacyUsageCount.coerceAtLeast(0),
                canonicalCoverage = canonicalCoverage.coerceIn(0.0, 1.0),
                readyToRetire = ready,
                blockers = blockers.toSet().sorted()
            )
        }

        fun ready(domain: CanonicalRetirementReadinessDomain): CanonicalRetirementReadinessDomainStatus {
            return CanonicalRetirementReadinessDomainStatus(
                domain = domain,
                legacyUsageCount = 0,
                canonicalCoverage = 1.0,
                readyToRetire = true,
                blockers = emptyList()
            )
        }
    }
}

// ── Type 3: CanonicalRetirementReadinessBlocker ──

enum class CanonicalRetirementReadinessBlocker(val rawValue: String) {
    LEGACY_STILL_ACTIVE("legacyStillActive"),
    CANONICAL_COVERAGE_INCOMPLETE("canonicalCoverageIncomplete"),
    AUDIO_UPLOAD_PATH_ACTIVE("audioUploadPathActive"),
    SYNC_ENGINE_LEGACY_PATH_REQUIRED("syncEngineLegacyPathRequired"),
    APPLY_RUNTIME_LEGACY_FALLBACK_REQUIRED("applyRuntimeLegacyFallbackRequired"),
    READ_RUNTIME_LEGACY_PATH_ACTIVE("readRuntimeLegacyPathActive"),
    KERNEL_SWITCH_NOT_REVERSIBLE("kernelSwitchNotReversible"),
    LEGACY_COMPATIBILITY_NOT_PROVEN("legacyCompatibilityNotProven"),
    DEVICE_EVIDENCE_MISSING("deviceEvidenceMissing"),
    MANUAL_OWNER_OVERRIDE_MISSING("manualOwnerOverrideMissing")
}

// ── Type 4: CanonicalRetirementReadinessReport ──

data class CanonicalRetirementReadinessReport(
    val domainStatuses: List<CanonicalRetirementReadinessDomainStatus>,
    val overallReady: Boolean,
    val legacyStillActive: Boolean,
    val activeLegacyDomains: List<CanonicalRetirementReadinessDomain>,
    val readyDomains: List<CanonicalRetirementReadinessDomain>,
    val blockers: List<CanonicalRetirementReadinessBlocker>,
    val evaluatedAt: CanonicalTimestamp,
    val diagnosticsSummary: String
) {
    val readyDomainCount: Int
        get() = readyDomains.size

    val totalDomainCount: Int
        get() = domainStatuses.size

    val activeLegacyDomainCount: Int
        get() = activeLegacyDomains.size

    val retirementProgress: Double
        get() = if (totalDomainCount > 0)
            readyDomainCount.toDouble() / totalDomainCount.toDouble()
        else 0.0

    val retirementProgressPercentage: Int
        get() = (retirementProgress * 100.0).toInt()

    companion object {
        fun build(
            domainStatuses: List<CanonicalRetirementReadinessDomainStatus>,
            additionalBlockers: List<CanonicalRetirementReadinessBlocker> = emptyList(),
            evaluatedAt: Date = Date()
        ): CanonicalRetirementReadinessReport {
            val sorted = domainStatuses.sortedBy { it.domain.rawValue }
            val allReady = sorted.all { it.readyToRetire }
            val anyLegacy = sorted.any { it.legacyUsageCount > 0 }
            val activeLegacyDomains = sorted.filter { it.legacyUsageCount > 0 }
                .map { it.domain }
                .sortedBy { it.rawValue }
            val readyDomains = sorted.filter { it.readyToRetire }
                .map { it.domain }
                .sortedBy { it.rawValue }

            val blockers = mutableListOf<CanonicalRetirementReadinessBlocker>()
            if (anyLegacy) {
                blockers.add(CanonicalRetirementReadinessBlocker.LEGACY_STILL_ACTIVE)
            }
            if (!allReady) {
                blockers.add(CanonicalRetirementReadinessBlocker.CANONICAL_COVERAGE_INCOMPLETE)
            }
            val activeAudioUpload = sorted.any {
                it.domain == CanonicalRetirementReadinessDomain.AUDIO_UPLOAD && it.legacyUsageCount > 0
            }
            if (activeAudioUpload) {
                blockers.add(CanonicalRetirementReadinessBlocker.AUDIO_UPLOAD_PATH_ACTIVE)
            }
            blockers.addAll(additionalBlockers)
            val uniqueBlockers = blockers.toSet().sortedBy { it.rawValue }

            val summary = listOf(
                "retirementReadiness=v1",
                "overallReady=$allReady",
                "legacyStillActive=$anyLegacy",
                "readyDomains=${readyDomains.size}/${sorted.size}",
                "activeLegacyDomains=${activeLegacyDomains.joinToString("+") { it.rawValue }.nilIfEmpty ?: "none"}",
                "blockers=${uniqueBlockers.joinToString("+") { it.rawValue }.nilIfEmpty ?: "none"}"
            ).joinToString(",")

            return CanonicalRetirementReadinessReport(
                domainStatuses = sorted,
                overallReady = allReady,
                legacyStillActive = anyLegacy,
                activeLegacyDomains = activeLegacyDomains,
                readyDomains = readyDomains,
                blockers = uniqueBlockers,
                evaluatedAt = CanonicalTimestamp(evaluatedAt),
                diagnosticsSummary = summary
            )
        }
    }
}

// ── Type 5: CanonicalRetirementReadinessContext ──

data class CanonicalRetirementReadinessContext(
    val domainLegacyUsage: Map<CanonicalRetirementReadinessDomain, Int>,
    val domainCanonicalCoverage: Map<CanonicalRetirementReadinessDomain, Double>,
    val kernelSwitchReversible: Boolean,
    val legacyCompatibilityProven: Boolean,
    val deviceEvidenceAvailable: Boolean,
    val manualOwnerOverride: Boolean
) {
    companion object {
        fun allReady(): CanonicalRetirementReadinessContext {
            val zeroUsage = CanonicalRetirementReadinessDomain.entries.associateWith { 0 }
            val fullCoverage = CanonicalRetirementReadinessDomain.entries.associateWith { 1.0 }
            return CanonicalRetirementReadinessContext(
                domainLegacyUsage = zeroUsage,
                domainCanonicalCoverage = fullCoverage,
                kernelSwitchReversible = true,
                legacyCompatibilityProven = true,
                deviceEvidenceAvailable = true,
                manualOwnerOverride = true
            )
        }
    }
}

// ── Type 6: CanonicalRetirementReadiness ──

object CanonicalRetirementReadiness {

    data class RetirementEvaluationResult(
        val report: CanonicalRetirementReadinessReport,
        val perDomainStatus: List<CanonicalRetirementReadinessDomainStatus>,
        val allDomainsReady: Boolean,
        val evaluationDiagnostics: String
    )

    fun evaluate(context: CanonicalRetirementReadinessContext): RetirementEvaluationResult {
        val domainStatuses = CanonicalRetirementReadinessDomain.entries.map { domain ->
            val usage = context.domainLegacyUsage[domain] ?: 0
            val coverage = context.domainCanonicalCoverage[domain] ?: 0.0
            val domainBlockers = mutableListOf<String>()
            if (!context.kernelSwitchReversible) {
                domainBlockers.add("kernelSwitchNotReversible")
            }
            if (!context.legacyCompatibilityProven) {
                domainBlockers.add("legacyCompatibilityNotProven")
            }
            if (!context.deviceEvidenceAvailable) {
                domainBlockers.add("deviceEvidenceMissing")
            }
            CanonicalRetirementReadinessDomainStatus.evaluate(
                domain = domain,
                legacyUsageCount = usage,
                canonicalCoverage = coverage,
                additionalBlockers = domainBlockers
            )
        }

        val globalBlockers = mutableListOf<CanonicalRetirementReadinessBlocker>()
        if (!context.kernelSwitchReversible) {
            globalBlockers.add(CanonicalRetirementReadinessBlocker.KERNEL_SWITCH_NOT_REVERSIBLE)
        }
        if (!context.legacyCompatibilityProven) {
            globalBlockers.add(CanonicalRetirementReadinessBlocker.LEGACY_COMPATIBILITY_NOT_PROVEN)
        }
        if (!context.deviceEvidenceAvailable) {
            globalBlockers.add(CanonicalRetirementReadinessBlocker.DEVICE_EVIDENCE_MISSING)
        }
        if (!context.manualOwnerOverride) {
            globalBlockers.add(CanonicalRetirementReadinessBlocker.MANUAL_OWNER_OVERRIDE_MISSING)
        }

        val report = CanonicalRetirementReadinessReport.build(
            domainStatuses = domainStatuses,
            additionalBlockers = globalBlockers
        )

        val allReady = report.overallReady && globalBlockers.isEmpty()
        val diagnostics = listOf(
            "retirementEvaluation=v1",
            "domains=${report.readyDomainCount}/${report.totalDomainCount}",
            "overallReady=$allReady",
            "legacyStillActive=${report.legacyStillActive}",
            "kernelSwitchReversible=${context.kernelSwitchReversible}",
            "legacyCompatibilityProven=${context.legacyCompatibilityProven}",
            "deviceEvidenceAvailable=${context.deviceEvidenceAvailable}",
            "manualOwnerOverride=${context.manualOwnerOverride}"
        ).joinToString(",")

        return RetirementEvaluationResult(
            report = report,
            perDomainStatus = domainStatuses,
            allDomainsReady = allReady,
            evaluationDiagnostics = diagnostics
        )
    }

    fun evaluateDomainOnly(
        domain: CanonicalRetirementReadinessDomain,
        legacyUsageCount: Int = 0,
        canonicalCoverage: Double = 0.0
    ): CanonicalRetirementReadinessDomainStatus {
        return CanonicalRetirementReadinessDomainStatus.evaluate(
            domain = domain,
            legacyUsageCount = legacyUsageCount,
            canonicalCoverage = canonicalCoverage
        )
    }

    fun buildReport(
        domainStatuses: List<CanonicalRetirementReadinessDomainStatus>
    ): CanonicalRetirementReadinessReport {
        return CanonicalRetirementReadinessReport.build(domainStatuses)
    }
}
