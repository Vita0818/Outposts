package com.rokurics.app.domain.canonical

import java.util.UUID

enum class CanonicalDryRunMigrationMode(val rawValue: String) {
    DISABLED("disabled"),
    DRY_RUN_COMPARE("dryRunCompare");

    companion object {
        val allCases: List<CanonicalDryRunMigrationMode> = entries.toList()
    }
}

data class CanonicalDryRunMigrationPolicy
private constructor(
    val enabled: Boolean,
    val compareSyncDecisions: Boolean,
    val compareApplyPlans: Boolean,
    val compareLibraryPlans: Boolean,
    val compareReadProjections: Boolean
) {
    companion object {
        operator fun invoke(
            enabled: Boolean = false,
            compareSyncDecisions: Boolean = true,
            compareApplyPlans: Boolean = true,
            compareLibraryPlans: Boolean = true,
            compareReadProjections: Boolean = true
        ): CanonicalDryRunMigrationPolicy {
            return CanonicalDryRunMigrationPolicy(
                enabled = enabled,
                compareSyncDecisions = compareSyncDecisions,
                compareApplyPlans = compareApplyPlans,
                compareLibraryPlans = compareLibraryPlans,
                compareReadProjections = compareReadProjections
            )
        }

        val DISABLED = CanonicalDryRunMigrationPolicy(enabled = false)

        fun fullCompare(): CanonicalDryRunMigrationPolicy {
            return CanonicalDryRunMigrationPolicy(
                enabled = true,
                compareSyncDecisions = true,
                compareApplyPlans = true,
                compareLibraryPlans = true,
                compareReadProjections = true
            )
        }
    }

    val diagnosticsSummary: String
        get() = listOf(
            "enabled=$enabled",
            "syncDecisions=$compareSyncDecisions",
            "applyPlans=$compareApplyPlans",
            "libraryPlans=$compareLibraryPlans",
            "readProjections=$compareReadProjections"
        ).joinToString(",")
}

data class CanonicalDryRunMigrationConfiguration
private constructor(
    val mode: CanonicalDryRunMigrationMode,
    val policy: CanonicalDryRunMigrationPolicy,
    val configID: String,
    val diagnosticsRedacted: Boolean
) {
    companion object {
        operator fun invoke(
            mode: CanonicalDryRunMigrationMode = CanonicalDryRunMigrationMode.DISABLED,
            policy: CanonicalDryRunMigrationPolicy = CanonicalDryRunMigrationPolicy.DISABLED,
            configID: String = UUID.randomUUID().toString(),
            diagnosticsRedacted: Boolean = true
        ): CanonicalDryRunMigrationConfiguration {
            return CanonicalDryRunMigrationConfiguration(
                mode = mode,
                policy = policy,
                configID = configID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
                diagnosticsRedacted = diagnosticsRedacted
            )
        }

        val DISABLED = CanonicalDryRunMigrationConfiguration()

        fun dryRunCompare(
            policy: CanonicalDryRunMigrationPolicy = CanonicalDryRunMigrationPolicy.fullCompare()
        ): CanonicalDryRunMigrationConfiguration {
            return CanonicalDryRunMigrationConfiguration(
                mode = CanonicalDryRunMigrationMode.DRY_RUN_COMPARE,
                policy = policy
            )
        }
    }

    val isEnabled: Boolean
        get() = mode != CanonicalDryRunMigrationMode.DISABLED && policy.enabled

    val diagnosticsSummary: String
        get() = listOf(
            "mode=${mode.rawValue}",
            "configID=$configID",
            "policy=${policy.diagnosticsSummary}",
            "redacted=$diagnosticsRedacted"
        ).joinToString(",")
}

data class CanonicalDryRunMigrationComparisonResult
private constructor(
    val domain: String,
    val legacyPlan: String?,
    val canonicalPlan: String?,
    val equivalent: Boolean,
    val divergenceDetail: String?
) {
    companion object {
        operator fun invoke(
            domain: String,
            legacyPlan: String? = null,
            canonicalPlan: String? = null,
            equivalent: Boolean = false,
            divergenceDetail: String? = null
        ): CanonicalDryRunMigrationComparisonResult {
            return CanonicalDryRunMigrationComparisonResult(
                domain = domain.trim().nilIfEmpty ?: "unknown-domain",
                legacyPlan = legacyPlan?.trim()?.nilIfEmpty,
                canonicalPlan = canonicalPlan?.trim()?.nilIfEmpty,
                equivalent = equivalent,
                divergenceDetail = divergenceDetail?.trim()?.nilIfEmpty
            )
        }

        fun equivalent(
            domain: String,
            legacyPlan: String,
            canonicalPlan: String
        ): CanonicalDryRunMigrationComparisonResult {
            return CanonicalDryRunMigrationComparisonResult(
                domain = domain,
                legacyPlan = legacyPlan,
                canonicalPlan = canonicalPlan,
                equivalent = true
            )
        }

        fun divergent(
            domain: String,
            legacyPlan: String,
            canonicalPlan: String,
            detail: String
        ): CanonicalDryRunMigrationComparisonResult {
            return CanonicalDryRunMigrationComparisonResult(
                domain = domain,
                legacyPlan = legacyPlan,
                canonicalPlan = canonicalPlan,
                equivalent = false,
                divergenceDetail = detail
            )
        }
    }

    val diagnosticsSummary: String
        get() = listOf(
            "domain=$domain",
            "equivalent=$equivalent",
            "legacy=${legacyPlan ?: "none"}",
            "canonical=${canonicalPlan ?: "none"}",
            "divergence=${divergenceDetail ?: "none"}"
        ).joinToString(",")
}

data class CanonicalDryRunMigrationReport
private constructor(
    val comparisons: List<CanonicalDryRunMigrationComparisonResult>,
    val equivalentCount: Int,
    val divergentCount: Int,
    val overallEquivalent: Boolean,
    val reportID: String,
    val domainsCompared: List<String>
) {
    constructor(
        comparisons: List<CanonicalDryRunMigrationComparisonResult>,
        reportID: String = UUID.randomUUID().toString()
    ) : this(
        comparisons = comparisons.sortedBy { it.domain },
        equivalentCount = comparisons.count { it.equivalent },
        divergentCount = comparisons.count { !it.equivalent },
        overallEquivalent = comparisons.isNotEmpty() && comparisons.all { it.equivalent },
        reportID = reportID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
        domainsCompared = comparisons.map { it.domain }.sorted()
    )

    val diagnosticsSummary: String
        get() = listOf(
            "reportID=$reportID",
            "domains=${domainsCompared.joinToString("+")}",
            "equivalent=$equivalentCount",
            "divergent=$divergentCount",
            "overallEquivalent=$overallEquivalent"
        ).joinToString(",")

    fun divergentDomains(): List<String> {
        return comparisons.filter { !it.equivalent }.map { it.domain }
    }

    fun divergencesByDomain(): Map<String, String> {
        return comparisons
            .filter { !it.equivalent }
            .associate { it.domain to (it.divergenceDetail ?: "unspecified") }
    }
}

class CanonicalDryRunMigrationPlanner(
    private val configuration: CanonicalDryRunMigrationConfiguration
) {
    interface LegacyPlanSource {
        fun buildLegacySyncDecisions(objects: List<CanonicalRecordingObject>): List<SyncDecision>
        fun buildLegacyApplyPlans(objects: List<CanonicalRecordingObject>): List<String>
        fun buildLegacyLibraryPlans(objects: List<CanonicalRecordingObject>): List<String>
        fun buildLegacyReadProjections(objects: List<CanonicalRecordingObject>): List<String>
    }

    interface CanonicalPlanSource {
        fun buildCanonicalSyncDecisions(objects: List<CanonicalRecordingObject>): List<SyncDecision>
        fun buildCanonicalApplyPlans(objects: List<CanonicalRecordingObject>): List<String>
        fun buildCanonicalLibraryPlans(objects: List<CanonicalRecordingObject>): List<String>
        fun buildCanonicalReadProjections(objects: List<CanonicalRecordingObject>): List<String>
    }

    data class PlanResult(
        val report: CanonicalDryRunMigrationReport,
        val configuration: CanonicalDryRunMigrationConfiguration,
        val diagnosticsSummary: String
    )

    fun plan(
        domain: String,
        objects: List<CanonicalRecordingObject>,
        legacySource: LegacyPlanSource,
        canonicalSource: CanonicalPlanSource
    ): PlanResult {
        if (!configuration.isEnabled) {
            return PlanResult(
                report = CanonicalDryRunMigrationReport(emptyList()),
                configuration = configuration,
                diagnosticsSummary = "mode=${configuration.mode.rawValue},enabled=false"
            )
        }

        val comparisons = mutableListOf<CanonicalDryRunMigrationComparisonResult>()
        val policy = configuration.policy

        if (policy.compareSyncDecisions) {
            val legacyDecisions = legacySource.buildLegacySyncDecisions(objects)
            val canonicalDecisions = canonicalSource.buildCanonicalSyncDecisions(objects)
            val comparison = compareSyncDecisions(
                domain = domain,
                legacy = legacyDecisions,
                canonical = canonicalDecisions
            )
            comparisons.add(comparison)
        }

        if (policy.compareApplyPlans) {
            val legacyPlans = legacySource.buildLegacyApplyPlans(objects)
            val canonicalPlans = canonicalSource.buildCanonicalApplyPlans(objects)
            val comparison = compareStringPlans(
                domain = "$domain.applyPlans",
                legacy = legacyPlans,
                canonical = canonicalPlans
            )
            comparisons.add(comparison)
        }

        if (policy.compareLibraryPlans) {
            val legacyPlans = legacySource.buildLegacyLibraryPlans(objects)
            val canonicalPlans = canonicalSource.buildCanonicalLibraryPlans(objects)
            val comparison = compareStringPlans(
                domain = "$domain.libraryPlans",
                legacy = legacyPlans,
                canonical = canonicalPlans
            )
            comparisons.add(comparison)
        }

        if (policy.compareReadProjections) {
            val legacyProjections = legacySource.buildLegacyReadProjections(objects)
            val canonicalProjections = canonicalSource.buildCanonicalReadProjections(objects)
            val comparison = compareStringPlans(
                domain = "$domain.readProjections",
                legacy = legacyProjections,
                canonical = canonicalProjections
            )
            comparisons.add(comparison)
        }

        val report = CanonicalDryRunMigrationReport(comparisons)

        return PlanResult(
            report = report,
            configuration = configuration,
            diagnosticsSummary = listOf(
                "domain=$domain",
                "objects=${objects.size}",
                "comparisons=${comparisons.size}",
                "overallEquivalent=${report.overallEquivalent}"
            ).joinToString(",")
        )
    }

    private fun compareSyncDecisions(
        domain: String,
        legacy: List<SyncDecision>,
        canonical: List<SyncDecision>
    ): CanonicalDryRunMigrationComparisonResult {
        if (legacy.isEmpty() && canonical.isEmpty()) {
            return CanonicalDryRunMigrationComparisonResult.equivalent(
                domain = domain,
                legacyPlan = "empty",
                canonicalPlan = "empty"
            )
        }

        val legacySummary = legacy.joinToString("\u001E") {
            "${it.objectID}|${it.kind.name}|${it.reason}"
        }
        val canonicalSummary = canonical.joinToString("\u001E") {
            "${it.objectID}|${it.kind.name}|${it.reason}"
        }

        val legacyKeyed = groupDecisionsByObjectID(legacy)
        val canonicalKeyed = groupDecisionsByObjectID(canonical)
        val allKeys = (legacyKeyed.keys + canonicalKeyed.keys).sorted()

        val divergences = mutableListOf<String>()
        for (key in allKeys) {
            val leg = legacyKeyed[key]
            val can = canonicalKeyed[key]
            if (leg == null) {
                divergences.add("$key:canonicalOnly")
            } else if (can == null) {
                divergences.add("$key:legacyOnly")
            } else if (leg.kind != can.kind) {
                divergences.add("$key:kindMismatch(${leg.kind.name}/${can.kind.name})")
            } else if (leg.reason != can.reason) {
                divergences.add("$key:reasonMismatch(${leg.reason}/${can.reason})")
            }
        }

        return if (divergences.isEmpty()) {
            CanonicalDryRunMigrationComparisonResult.equivalent(
                domain = domain,
                legacyPlan = legacySummary,
                canonicalPlan = canonicalSummary
            )
        } else {
            CanonicalDryRunMigrationComparisonResult.divergent(
                domain = domain,
                legacyPlan = legacySummary,
                canonicalPlan = canonicalSummary,
                detail = divergences.take(20).joinToString("\u001E")
            )
        }
    }

    private fun compareStringPlans(
        domain: String,
        legacy: List<String>,
        canonical: List<String>
    ): CanonicalDryRunMigrationComparisonResult {
        if (legacy.isEmpty() && canonical.isEmpty()) {
            return CanonicalDryRunMigrationComparisonResult.equivalent(
                domain = domain,
                legacyPlan = "empty",
                canonicalPlan = "empty"
            )
        }

        val legacySet = legacy.sorted().toSet()
        val canonicalSet = canonical.sorted().toSet()

        if (legacySet == canonicalSet) {
            return CanonicalDryRunMigrationComparisonResult.equivalent(
                domain = domain,
                legacyPlan = "size=${legacySet.size}",
                canonicalPlan = "size=${canonicalSet.size}"
            )
        }

        val onlyInLegacy = legacySet.subtract(canonicalSet)
        val onlyInCanonical = canonicalSet.subtract(legacySet)
        val details = mutableListOf<String>()
        if (onlyInLegacy.isNotEmpty()) {
            details.add("legacyOnly(${onlyInLegacy.size})")
        }
        if (onlyInCanonical.isNotEmpty()) {
            details.add("canonicalOnly(${onlyInCanonical.size})")
        }

        return CanonicalDryRunMigrationComparisonResult.divergent(
            domain = domain,
            legacyPlan = "size=${legacySet.size}",
            canonicalPlan = "size=${canonicalSet.size}",
            detail = details.joinToString(";")
        )
    }

    private fun groupDecisionsByObjectID(
        decisions: List<SyncDecision>
    ): Map<String, SyncDecision> {
        return decisions.associateBy { it.objectID }
    }
}
