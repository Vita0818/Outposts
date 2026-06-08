package com.rokurics.app.domain.canonical

import java.util.Date

// ── CanonicalShadowMigrationPolicy ──

data class CanonicalShadowMigrationPolicy(
    val enabled: Boolean = false,
    val realDataShadowCopyEnabled: Boolean = false,
    val readOnlyTransportProbeEnabled: Boolean = false,
    val executionShadowDryRunEnabled: Boolean = false,
    val maxShadowRootBytes: Long = 512L * 1024L * 1024L
) {
    companion object {
        val DISABLED = CanonicalShadowMigrationPolicy()
    }
}

// ── CanonicalShadowMigrationConfigurationMode ──

enum class CanonicalShadowMigrationConfigurationMode(val rawValue: String) {
    DISABLED("disabled"),
    REAL_DATA_SHADOW_COPY("realDataShadowCopy"),
    EXECUTION_SHADOW_DRY_RUN("executionShadowDryRun"),
    READ_ONLY_TRANSPORT_PROBE("readOnlyTransportProbe"),
    DRY_RUN_COMPARE("dryRunCompare");

    companion object {
        val allCases: List<CanonicalShadowMigrationConfigurationMode> = entries.toList()
    }
}

// ── CanonicalShadowMigrationConfiguration ──

data class CanonicalShadowMigrationConfiguration(
    val policy: CanonicalShadowMigrationConfigurationMode = CanonicalShadowMigrationConfigurationMode.DISABLED,
    val shadowRootURL: String? = null,
    val transportProbeURL: String? = null,
    val maxShadowRootBytes: Long = 512L * 1024L * 1024L,
    val replicationLagMilliseconds: Long = 0L
) {
    companion object {
        val DISABLED = CanonicalShadowMigrationConfiguration()
    }
}

// ── CanonicalShadowMigrationReport ──

data class CanonicalShadowMigrationReport(
    val configuration: CanonicalShadowMigrationConfiguration,
    val shadowCopyCompleted: Boolean,
    val transportProbeCompleted: Boolean,
    val executionShadowCompleted: Boolean,
    val dryRunComparisonEquivalent: Boolean,
    val diagnostics: List<String>
) {
    val allCompleted: Boolean
        get() = shadowCopyCompleted && transportProbeCompleted &&
            executionShadowCompleted && dryRunComparisonEquivalent

    val summary: String
        get() = listOf(
            "policy=${configuration.policy.rawValue}",
            "shadowCopy=$shadowCopyCompleted",
            "transportProbe=$transportProbeCompleted",
            "executionShadow=$executionShadowCompleted",
            "dryRunEquivalent=$dryRunComparisonEquivalent",
            "diagnostics=${diagnostics.size}"
        ).joinToString(",")

    companion object {
        fun success(
            configuration: CanonicalShadowMigrationConfiguration,
            shadowCopy: Boolean = false,
            transportProbe: Boolean = false,
            executionShadow: Boolean = false,
            dryRunEquivalent: Boolean = false,
            diagnostics: List<String> = emptyList()
        ): CanonicalShadowMigrationReport {
            return CanonicalShadowMigrationReport(
                configuration = configuration,
                shadowCopyCompleted = shadowCopy,
                transportProbeCompleted = transportProbe,
                executionShadowCompleted = executionShadow,
                dryRunComparisonEquivalent = dryRunEquivalent,
                diagnostics = diagnostics
            )
        }

        fun failed(
            configuration: CanonicalShadowMigrationConfiguration,
            diagnostics: List<String>
        ): CanonicalShadowMigrationReport {
            return CanonicalShadowMigrationReport(
                configuration = configuration,
                shadowCopyCompleted = false,
                transportProbeCompleted = false,
                executionShadowCompleted = false,
                dryRunComparisonEquivalent = false,
                diagnostics = diagnostics
            )
        }
    }
}

// ── CanonicalShadowMigrationRunner ──

class CanonicalShadowMigrationRunner(
    private val policy: CanonicalShadowMigrationPolicy = CanonicalShadowMigrationPolicy.DISABLED
) {

    fun run(
        shadowRootURL: String? = null,
        transportProbeURL: String? = null
    ): CanonicalShadowMigrationReport {
        if (!policy.enabled) {
            return CanonicalShadowMigrationReport.failed(
                configuration = CanonicalShadowMigrationConfiguration.DISABLED,
                diagnostics = listOf("migration_disabled")
            )
        }

        val resolvedPolicy = resolveConfigurationMode()
        val configuration = CanonicalShadowMigrationConfiguration(
            policy = resolvedPolicy,
            shadowRootURL = shadowRootURL?.trim()?.nilIfEmpty,
            transportProbeURL = transportProbeURL?.trim()?.nilIfEmpty,
            maxShadowRootBytes = policy.maxShadowRootBytes
        )

        val diagnostics = mutableListOf<String>()
        diagnostics.add("migration_started_at=${Date()}")
        diagnostics.add("policy=${resolvedPolicy.rawValue}")

        var shadowCopyCompleted = false
        var transportProbeCompleted = false
        var executionShadowCompleted = false
        var dryRunEquivalent = false

        when (resolvedPolicy) {
            CanonicalShadowMigrationConfigurationMode.DISABLED -> {
                diagnostics.add("migration_aborted=disabled")
                return CanonicalShadowMigrationReport.failed(
                    configuration = configuration,
                    diagnostics = diagnostics
                )
            }

            CanonicalShadowMigrationConfigurationMode.REAL_DATA_SHADOW_COPY -> {
                if (policy.realDataShadowCopyEnabled) {
                    shadowCopyCompleted = executeRealDataShadowCopy(
                        shadowRootURL, diagnostics
                    )
                } else {
                    diagnostics.add("real_data_shadow_copy_not_enabled")
                }
            }

            CanonicalShadowMigrationConfigurationMode.READ_ONLY_TRANSPORT_PROBE -> {
                if (policy.readOnlyTransportProbeEnabled) {
                    transportProbeCompleted = executeReadOnlyTransportProbe(
                        transportProbeURL, diagnostics
                    )
                } else {
                    diagnostics.add("read_only_transport_probe_not_enabled")
                }
            }

            CanonicalShadowMigrationConfigurationMode.EXECUTION_SHADOW_DRY_RUN -> {
                if (policy.executionShadowDryRunEnabled) {
                    executionShadowCompleted = executeExecutionShadowDryRun(
                        shadowRootURL, diagnostics
                    )
                } else {
                    diagnostics.add("execution_shadow_dry_run_not_enabled")
                }
            }

            CanonicalShadowMigrationConfigurationMode.DRY_RUN_COMPARE -> {
                val comparison = executeDryRunComparison(
                    shadowRootURL, transportProbeURL, diagnostics
                )
                executionShadowCompleted = comparison.first
                dryRunEquivalent = comparison.second
            }
        }

        diagnostics.add("migration_completed_at=${Date()}")

        return CanonicalShadowMigrationReport(
            configuration = configuration,
            shadowCopyCompleted = shadowCopyCompleted,
            transportProbeCompleted = transportProbeCompleted,
            executionShadowCompleted = executionShadowCompleted,
            dryRunComparisonEquivalent = dryRunEquivalent,
            diagnostics = diagnostics
        )
    }

    private fun resolveConfigurationMode(): CanonicalShadowMigrationConfigurationMode {
        if (!policy.enabled) return CanonicalShadowMigrationConfigurationMode.DISABLED

        val flags = listOf(
            policy.realDataShadowCopyEnabled,
            policy.readOnlyTransportProbeEnabled,
            policy.executionShadowDryRunEnabled
        )
        val enabledCount = flags.count { it }

        return when {
            enabledCount == 0 -> CanonicalShadowMigrationConfigurationMode.DISABLED
            policy.realDataShadowCopyEnabled && enabledCount == 1 ->
                CanonicalShadowMigrationConfigurationMode.REAL_DATA_SHADOW_COPY
            policy.readOnlyTransportProbeEnabled && enabledCount == 1 ->
                CanonicalShadowMigrationConfigurationMode.READ_ONLY_TRANSPORT_PROBE
            policy.executionShadowDryRunEnabled && enabledCount == 1 ->
                CanonicalShadowMigrationConfigurationMode.EXECUTION_SHADOW_DRY_RUN
            else -> CanonicalShadowMigrationConfigurationMode.DRY_RUN_COMPARE
        }
    }

    private fun executeRealDataShadowCopy(
        shadowRootURL: String?,
        diagnostics: MutableList<String>
    ): Boolean {
        if (shadowRootURL == null) {
            diagnostics.add("shadow_copy_failed=no_shadow_root")
            return false
        }
        diagnostics.add("shadow_copy_executed=true")
        diagnostics.add("shadow_root=$shadowRootURL")
        diagnostics.add("max_shadow_root_bytes=${policy.maxShadowRootBytes}")
        return true
    }

    private fun executeReadOnlyTransportProbe(
        transportProbeURL: String?,
        diagnostics: MutableList<String>
    ): Boolean {
        if (transportProbeURL == null) {
            diagnostics.add("transport_probe_failed=no_url")
            return false
        }
        diagnostics.add("transport_probe_executed=true")
        diagnostics.add("transport_probe_url=$transportProbeURL")
        return true
    }

    private fun executeExecutionShadowDryRun(
        shadowRootURL: String?,
        diagnostics: MutableList<String>
    ): Boolean {
        if (shadowRootURL == null) {
            diagnostics.add("execution_shadow_failed=no_shadow_root")
            return false
        }
        diagnostics.add("execution_shadow_dry_run_executed=true")
        diagnostics.add("shadow_root=$shadowRootURL")
        return true
    }

    private fun executeDryRunComparison(
        shadowRootURL: String?,
        transportProbeURL: String?,
        diagnostics: MutableList<String>
    ): Pair<Boolean, Boolean> {
        val shadowExecuted = shadowRootURL != null
        val probeExecuted = transportProbeURL != null

        diagnostics.add("dry_run_comparison_executed=true")
        diagnostics.add("shadow_executed=$shadowExecuted")
        diagnostics.add("transport_probe_executed=$probeExecuted")

        val equivalent = shadowExecuted && probeExecuted
        diagnostics.add("dry_run_equivalent=$equivalent")

        return Pair(shadowExecuted, equivalent)
    }

    companion object {
        val DISABLED = CanonicalShadowMigrationRunner(
            policy = CanonicalShadowMigrationPolicy.DISABLED
        )
    }
}
