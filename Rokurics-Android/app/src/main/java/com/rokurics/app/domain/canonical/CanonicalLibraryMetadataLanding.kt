package com.rokurics.app.domain.canonical

import java.util.UUID

data class CanonicalMigrationLandingFreezeResult(
    val frozen: Boolean,
    val activePilot: String?,
    val blockers: List<CanonicalMigrationLandingFreezeBlocker>,
    val freezeID: String,
    val frozenAt: CanonicalTimestamp?,
    val diagnosticsSummary: String
) {
    constructor(
        frozen: Boolean,
        activePilot: String? = null,
        blockers: List<CanonicalMigrationLandingFreezeBlocker> = emptyList(),
        freezeID: String = UUID.randomUUID().toString(),
        frozenAt: CanonicalTimestamp? = null
    ) : this(
        frozen = frozen,
        activePilot = activePilot?.trim()?.nilIfEmpty,
        blockers = blockers.distinct().sortedBy { it.rawValue },
        freezeID = freezeID,
        frozenAt = frozenAt,
        diagnosticsSummary = listOf(
            "frozen=$frozen",
            "activePilot=${activePilot ?: "none"}",
            "blockers=${blockers.map { it.rawValue }.joinToString("|")}",
            "freezeID=$freezeID"
        ).joinToString(",")
    )

    companion object {
        fun notFrozen(): CanonicalMigrationLandingFreezeResult {
            return CanonicalMigrationLandingFreezeResult(
                frozen = false,
                freezeID = "no-freeze-required"
            )
        }

        fun frozen(
            activePilot: String?,
            blockers: List<CanonicalMigrationLandingFreezeBlocker>
        ): CanonicalMigrationLandingFreezeResult {
            return CanonicalMigrationLandingFreezeResult(
                frozen = true,
                activePilot = activePilot,
                blockers = blockers
            )
        }
    }
}

enum class CanonicalMigrationLandingFreezeBlocker(val rawValue: String) {
    ACTIVE_CANARY_IN_PROGRESS("activeCanaryInProgress"),
    OBSERVATION_PERIOD_ACTIVE("observationPeriodActive"),
    LANDING_INCOMPLETE("landingIncomplete"),
    RETIREMENT_READINESS_NOT_PROVEN("retirementReadinessNotProven"),
    OWNER_APPROVAL_REQUIRED("ownerApprovalRequired"),
    PRODUCTION_ROOT_WRITES_BLOCKED("productionRootWritesBlocked"),
    CUTOVER_DOMAIN_CONFLICT("cutoverDomainConflict"),
    DEBUG_PILOT_ACTIVE("debugPilotActive"),
    LANDING_REPORT_BLOCKED("landingReportBlocked")
}

object CanonicalMigrationLandingFreeze {
    fun freeze(
        activePilot: String?,
        canaryInProgress: Boolean,
        observationActive: Boolean,
        landingComplete: Boolean,
        retirementReady: Boolean,
        ownerApproved: Boolean,
        productionRootWritesAllowed: Boolean,
        cutoverDomainConflict: Boolean,
        debugPilotActive: Boolean
    ): CanonicalMigrationLandingFreezeResult {
        val blockers = mutableListOf<CanonicalMigrationLandingFreezeBlocker>()

        if (canaryInProgress) {
            blockers.add(CanonicalMigrationLandingFreezeBlocker.ACTIVE_CANARY_IN_PROGRESS)
        }
        if (observationActive) {
            blockers.add(CanonicalMigrationLandingFreezeBlocker.OBSERVATION_PERIOD_ACTIVE)
        }
        if (!landingComplete) {
            blockers.add(CanonicalMigrationLandingFreezeBlocker.LANDING_INCOMPLETE)
        }
        if (!retirementReady) {
            blockers.add(CanonicalMigrationLandingFreezeBlocker.RETIREMENT_READINESS_NOT_PROVEN)
        }
        if (!ownerApproved) {
            blockers.add(CanonicalMigrationLandingFreezeBlocker.OWNER_APPROVAL_REQUIRED)
        }
        if (!productionRootWritesAllowed) {
            blockers.add(CanonicalMigrationLandingFreezeBlocker.PRODUCTION_ROOT_WRITES_BLOCKED)
        }
        if (cutoverDomainConflict) {
            blockers.add(CanonicalMigrationLandingFreezeBlocker.CUTOVER_DOMAIN_CONFLICT)
        }
        if (debugPilotActive) {
            blockers.add(CanonicalMigrationLandingFreezeBlocker.DEBUG_PILOT_ACTIVE)
        }

        if (blockers.isEmpty()) {
            return CanonicalMigrationLandingFreezeResult.notFrozen()
        }

        return CanonicalMigrationLandingFreezeResult.frozen(
            activePilot = activePilot,
            blockers = blockers
        )
    }
}

enum class CanonicalLibraryMetadataDebugPilotMode(val rawValue: String) {
    DISABLED("disabled"),
    DIAGNOSTICS_ONLY("diagnosticsOnly"),
    ARM_TEST_ROOT_N1("armTestRootN1"),
    EXECUTE_TEST_ROOT_N1("executeTestRootN1"),
    EXECUTE_PRODUCTION_ROOT_N1("executeProductionRootN1")
}

data class CanonicalLibraryMetadataDebugPilotConfiguration(
    val mode: CanonicalLibraryMetadataDebugPilotMode = CanonicalLibraryMetadataDebugPilotMode.DISABLED,
    val allowProductionRootWrites: Boolean = false,
    val ownerApproved: Boolean = false,
    val testRootURL: String? = null,
    val diagnosticsRedacted: Boolean = true,
    val candidateCountLimit: Int = 1,
    val landingRecommendation: CanonicalLibraryMetadataLandingRecommendation? = null
) {
    companion object {
        val DISABLED = CanonicalLibraryMetadataDebugPilotConfiguration()

        fun diagnosticsOnly(): CanonicalLibraryMetadataDebugPilotConfiguration {
            return CanonicalLibraryMetadataDebugPilotConfiguration(
                mode = CanonicalLibraryMetadataDebugPilotMode.DIAGNOSTICS_ONLY
            )
        }

        fun armTestRootN1(
            testRootURL: String,
            ownerApproved: Boolean = false,
            candidateCountLimit: Int = 1
        ): CanonicalLibraryMetadataDebugPilotConfiguration {
            return CanonicalLibraryMetadataDebugPilotConfiguration(
                mode = CanonicalLibraryMetadataDebugPilotMode.ARM_TEST_ROOT_N1,
                testRootURL = testRootURL,
                ownerApproved = ownerApproved,
                candidateCountLimit = candidateCountLimit
            )
        }

        fun executeTestRootN1(
            testRootURL: String,
            ownerApproved: Boolean,
            candidateCountLimit: Int = 1
        ): CanonicalLibraryMetadataDebugPilotConfiguration {
            return CanonicalLibraryMetadataDebugPilotConfiguration(
                mode = CanonicalLibraryMetadataDebugPilotMode.EXECUTE_TEST_ROOT_N1,
                testRootURL = testRootURL,
                ownerApproved = ownerApproved,
                candidateCountLimit = candidateCountLimit
            )
        }

        fun executeProductionRootN1(
            ownerApproved: Boolean,
            candidateCountLimit: Int = 1
        ): CanonicalLibraryMetadataDebugPilotConfiguration {
            return CanonicalLibraryMetadataDebugPilotConfiguration(
                mode = CanonicalLibraryMetadataDebugPilotMode.EXECUTE_PRODUCTION_ROOT_N1,
                allowProductionRootWrites = true,
                ownerApproved = ownerApproved,
                candidateCountLimit = candidateCountLimit
            )
        }
    }

    val isEnabled: Boolean
        get() = mode != CanonicalLibraryMetadataDebugPilotMode.DISABLED

    val canWrite: Boolean
        get() = when (mode) {
            CanonicalLibraryMetadataDebugPilotMode.EXECUTE_TEST_ROOT_N1,
            CanonicalLibraryMetadataDebugPilotMode.EXECUTE_PRODUCTION_ROOT_N1 -> ownerApproved
            else -> false
        }

    val writesProductionRoot: Boolean
        get() = mode == CanonicalLibraryMetadataDebugPilotMode.EXECUTE_PRODUCTION_ROOT_N1 &&
                ownerApproved &&
                allowProductionRootWrites
}

class CanonicalLibraryMetadataDebugPilotBootstrap(
    private val configuration: CanonicalLibraryMetadataDebugPilotConfiguration,
    private val landingFreeze: CanonicalMigrationLandingFreezeResult,
    private val canaryConfiguration: CanonicalLibraryMetadataCanaryConfiguration
) {
    data class BootstrapResult(
        val prepared: Boolean,
        val mode: CanonicalLibraryMetadataDebugPilotMode,
        val landingFrozen: Boolean,
        val canaryArmed: Boolean,
        val testRootAvailable: Boolean,
        val productionRootArmed: Boolean,
        val blockers: List<String>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun blocked(
                mode: CanonicalLibraryMetadataDebugPilotMode,
                blockers: List<String>
            ): BootstrapResult {
                return BootstrapResult(
                    prepared = false,
                    mode = mode,
                    landingFrozen = false,
                    canaryArmed = false,
                    testRootAvailable = false,
                    productionRootArmed = false,
                    blockers = blockers.sorted(),
                    diagnosticsSummary = listOf(
                        "prepared=false",
                        "mode=${mode.rawValue}",
                        "blockers=${blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun prepare(): BootstrapResult {
        val mode = configuration.mode
        if (mode == CanonicalLibraryMetadataDebugPilotMode.DISABLED) {
            return BootstrapResult.blocked(
                mode = mode,
                blockers = listOf("pilotDisabled")
            )
        }

        val blockers = mutableListOf<String>()

        if (landingFreeze.frozen) {
            blockers.add("landingFreezeActive")
        }

        if (mode == CanonicalLibraryMetadataDebugPilotMode.EXECUTE_PRODUCTION_ROOT_N1) {
            if (!configuration.ownerApproved) {
                blockers.add("ownerNotApproved")
            }
            if (!configuration.allowProductionRootWrites) {
                blockers.add("productionRootWritesNotAllowed")
            }
            if (canaryConfiguration.mode != CanonicalLibraryMetadataCanaryConfigurationMode.CANARY_COMMIT) {
                blockers.add("canaryNotArmedForCommit")
            }
        }

        if (mode == CanonicalLibraryMetadataDebugPilotMode.EXECUTE_TEST_ROOT_N1 ||
            mode == CanonicalLibraryMetadataDebugPilotMode.ARM_TEST_ROOT_N1) {
            if (configuration.testRootURL == null) {
                blockers.add("testRootURLMissing")
            }
        }

        if (blockers.isNotEmpty()) {
            return BootstrapResult.blocked(mode, blockers)
        }

        return BootstrapResult(
            prepared = true,
            mode = mode,
            landingFrozen = landingFreeze.frozen,
            canaryArmed = canaryConfiguration.mode == CanonicalLibraryMetadataCanaryConfigurationMode.CANARY_COMMIT,
            testRootAvailable = configuration.testRootURL != null,
            productionRootArmed = configuration.mode == CanonicalLibraryMetadataDebugPilotMode.EXECUTE_PRODUCTION_ROOT_N1,
            blockers = emptyList(),
            diagnosticsSummary = listOf(
                "prepared=true",
                "mode=${mode.rawValue}",
                "landingFrozen=${landingFreeze.frozen}",
                "canaryArmed=${canaryConfiguration.mode == CanonicalLibraryMetadataCanaryConfigurationMode.CANARY_COMMIT}",
                "testRoot=${configuration.testRootURL != null}",
                "productionRoot=${
                    configuration.mode == CanonicalLibraryMetadataDebugPilotMode.EXECUTE_PRODUCTION_ROOT_N1
                }"
            ).joinToString(",")
        )
    }
}

data class CanonicalLibraryMetadataLandingReport(
    val landingComplete: Boolean,
    val frozen: Boolean,
    val activePilot: String?,
    val recommendation: CanonicalLibraryMetadataLandingRecommendation?,
    val landingID: String,
    val candidatesCount: Int,
    val successfulCanaryCount: Int,
    val failedCanaryCount: Int,
    val observationReports: List<String>,
    val blockers: List<String>,
    val diagnosticsSummary: String
) {
    constructor(
        landingComplete: Boolean,
        frozen: Boolean = false,
        activePilot: String? = null,
        recommendation: CanonicalLibraryMetadataLandingRecommendation? = null,
        landingID: String = UUID.randomUUID().toString(),
        candidatesCount: Int = 0,
        successfulCanaryCount: Int = 0,
        failedCanaryCount: Int = 0,
        observationReports: List<String> = emptyList(),
        blockers: List<String> = emptyList()
    ) : this(
        landingComplete = landingComplete && blockers.isEmpty(),
        frozen = frozen,
        activePilot = activePilot?.trim()?.nilIfEmpty,
        recommendation = recommendation,
        landingID = landingID,
        candidatesCount = maxOf(0, candidatesCount),
        successfulCanaryCount = maxOf(0, successfulCanaryCount),
        failedCanaryCount = maxOf(0, failedCanaryCount),
        observationReports = observationReports,
        blockers = blockers.sorted(),
        diagnosticsSummary = listOf(
            "landingComplete=$landingComplete",
            "frozen=$frozen",
            "pilot=${activePilot ?: "none"}",
            "recommendation=${recommendation?.rawValue ?: "none"}",
            "candidates=$candidatesCount",
            "success=$successfulCanaryCount",
            "failed=$failedCanaryCount",
            "blockers=${blockers.joinToString("|")}"
        ).joinToString(",")
    )

    companion object {
        fun fromCanaryResults(
            recommendation: CanonicalLibraryMetadataLandingRecommendation,
            candidatesCount: Int,
            successfulCanaryCount: Int,
            failedCanaryCount: Int,
            observationReports: List<String> = emptyList(),
            activePilot: String? = null,
            frozen: Boolean = false
        ): CanonicalLibraryMetadataLandingReport {
            val blockers = mutableListOf<String>()
            if (frozen) blockers.add("landingFrozen")
            if (failedCanaryCount > 0) blockers.add("canaryFailures")
            if (recommendation == CanonicalLibraryMetadataLandingRecommendation.BLOCKED) {
                blockers.add("blockedByRecommendation")
            }
            if (recommendation == CanonicalLibraryMetadataLandingRecommendation.NEED_AUDIT) {
                blockers.add("needsAudit")
            }

            return CanonicalLibraryMetadataLandingReport(
                landingComplete = blockers.isEmpty(),
                frozen = frozen,
                activePilot = activePilot,
                recommendation = recommendation,
                candidatesCount = candidatesCount,
                successfulCanaryCount = successfulCanaryCount,
                failedCanaryCount = failedCanaryCount,
                observationReports = observationReports,
                blockers = blockers
            )
        }
    }
}

enum class CanonicalLibraryMetadataLandingRecommendation(val rawValue: String) {
    PROCEED_TO_NEXT_N1("proceedToNextN1"),
    NEED_AUDIT("needAudit"),
    BLOCKED("blocked")
}
