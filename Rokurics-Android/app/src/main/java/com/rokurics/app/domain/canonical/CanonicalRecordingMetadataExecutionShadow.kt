package com.rokurics.app.domain.canonical

import java.util.Date

// ── CanonicalExecutionShadowConfiguration ──

data class CanonicalExecutionShadowConfiguration(
    val mode: CanonicalExecutionShadowConfiguration.Mode = Mode.DISABLED,
    val shadowRootURL: String? = null,
    val transportProbeURL: String? = null,
    val maxShadowActions: Int = 1000,
    val redactedDiagnostics: Boolean = true
) {
    enum class Mode(val rawValue: String) {
        DISABLED("disabled"),
        DRY_RUN_COMPARE("dryRunCompare"),
        EXECUTION_SHADOW_DRY_RUN("executionShadowDryRun"),
        EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE("executionShadowWithShadowFileStore"),
        EXECUTION_SHADOW_WITH_TRANSPORT_PROBE("executionShadowWithTransportProbe");

        companion object {
            val allCases: List<Mode> = entries.toList()
        }
    }

    companion object {
        val DISABLED = CanonicalExecutionShadowConfiguration()
    }
}

// ── CanonicalExecutionShadowResult ──

data class CanonicalExecutionShadowResult(
    val action: String,
    val shadowRoot: String?,
    val equivalent: Boolean,
    val divergenceDetail: String?
) {
    val id: String get() = action
}

// ── CanonicalExecutionShadowReport ──

data class CanonicalExecutionShadowReport(
    val actions: List<CanonicalExecutionShadowResult>,
    val equivalentCount: Int,
    val divergentCount: Int,
    val generatedAt: Date = Date()
) {
    val totalActions: Int get() = actions.size

    val summary: String
        get() = listOf(
            "total=$totalActions",
            "equivalent=$equivalentCount",
            "divergent=$divergentCount"
        ).joinToString(",")

    companion object {
        fun fromResults(results: List<CanonicalExecutionShadowResult>): CanonicalExecutionShadowReport {
            val equivalentCount = results.count { it.equivalent }
            val divergentCount = results.count { !it.equivalent }
            return CanonicalExecutionShadowReport(
                actions = results,
                equivalentCount = equivalentCount,
                divergentCount = divergentCount
            )
        }
    }
}
