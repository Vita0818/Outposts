package com.rokurics.app.domain.canonical

import java.util.UUID

enum class CanonicalMigrationDomain(val rawValue: String) {
    RECORDING_METADATA("recordingMetadata"),
    AUDIO_ARTIFACT("audioArtifact"),
    RECEIVE_RECORD("receiveRecord"),
    STUDY_ITEM("studyItem"),
    FOLDER_METADATA("folderMetadata"),
    NOTE_ARTIFACT("noteArtifact"),
    TRANSCRIPT_ARTIFACT("transcriptArtifact"),
    LIBRARY_METADATA("libraryMetadata"),
    TOMBSTONE("tombstone"),
    READ_PROJECTION("readProjection");

    companion object {
        val allCases: List<CanonicalMigrationDomain> = entries.toList()
    }
}

data class CanonicalMigrationDomainState
private constructor(
    val domain: CanonicalMigrationDomain,
    val stage: CanonicalMigrationStage,
    val activePilot: Boolean,
    val canaryBudget: Int,
    val readSideParallel: Boolean,
    val readSideCutover: Boolean,
    val runtimeSwitch: Boolean,
    val defaultCutover: Boolean,
    val legacyRetirement: Boolean,
    val staticOnly: Boolean
) {
    companion object {
        operator fun invoke(
            domain: CanonicalMigrationDomain,
            stage: CanonicalMigrationStage = CanonicalMigrationStage.OFF,
            activePilot: Boolean = false,
            canaryBudget: Int = 0,
            readSideParallel: Boolean = false,
            readSideCutover: Boolean = false,
            runtimeSwitch: Boolean = false,
            defaultCutover: Boolean = false,
            legacyRetirement: Boolean = false,
            staticOnly: Boolean = false
        ): CanonicalMigrationDomainState {
            return CanonicalMigrationDomainState(
                domain = domain,
                stage = stage,
                activePilot = activePilot,
                canaryBudget = maxOf(0, canaryBudget),
                readSideParallel = readSideParallel,
                readSideCutover = readSideCutover,
                runtimeSwitch = runtimeSwitch,
                defaultCutover = defaultCutover,
                legacyRetirement = legacyRetirement,
                staticOnly = staticOnly
            )
        }

        fun off(domain: CanonicalMigrationDomain): CanonicalMigrationDomainState {
            return CanonicalMigrationDomainState(
                domain = domain,
                stage = CanonicalMigrationStage.OFF
            )
        }

        fun staticOnly(domain: CanonicalMigrationDomain): CanonicalMigrationDomainState {
            return CanonicalMigrationDomainState(
                domain = domain,
                stage = CanonicalMigrationStage.OFF,
                staticOnly = true
            )
        }
    }

    val isOff: Boolean
        get() = stage == CanonicalMigrationStage.OFF && !activePilot

    val isMigrationActive: Boolean
        get() = stage != CanonicalMigrationStage.OFF || activePilot

    val diagnosticsSummary: String
        get() = listOf(
            "domain=${domain.rawValue}",
            "stage=${stage.rawValue}",
            "activePilot=$activePilot",
            "canaryBudget=$canaryBudget",
            "readSideParallel=$readSideParallel",
            "readSideCutover=$readSideCutover",
            "runtimeSwitch=$runtimeSwitch",
            "defaultCutover=$defaultCutover",
            "legacyRetired=$legacyRetirement",
            "staticOnly=$staticOnly"
        ).joinToString(",")
}

data class CanonicalMigrationMatrixConfig(
    val domains: List<CanonicalMigrationDomainState>,
    val configID: String = UUID.randomUUID().toString(),
    val diagnosticsRedacted: Boolean = true
) {

    fun stateFor(domain: CanonicalMigrationDomain): CanonicalMigrationDomainState? {
        return domains.firstOrNull { it.domain == domain }
    }

    fun activePilotDomains(): List<CanonicalMigrationDomainState> {
        return domains.filter { it.activePilot }
    }

    fun staticOnlyDomains(): List<CanonicalMigrationDomainState> {
        return domains.filter { it.staticOnly }
    }

    fun domainsAtStage(stage: CanonicalMigrationStage): List<CanonicalMigrationDomainState> {
        return domains.filter { it.stage == stage }
    }

    val diagnosticsSummary: String
        get() = listOf(
            "configID=$configID",
            "domains=${domains.size}",
            "activePilots=${activePilotDomains().size}",
            "staticOnly=${staticOnlyDomains().size}",
            "redacted=$diagnosticsRedacted"
        ).joinToString(",")
}

data class CanonicalMigrationMatrixFreezeResult
private constructor(
    val frozen: Boolean,
    val soleActivePilot: CanonicalMigrationDomain?,
    val otherDomainsStatic: Boolean,
    val blockers: List<String>,
    val freezeID: String,
    val diagnosticsSummary: String
) {
    companion object {
        operator fun invoke(
            frozen: Boolean = false,
            soleActivePilot: CanonicalMigrationDomain? = null,
            otherDomainsStatic: Boolean = false,
            blockers: List<String> = emptyList(),
            freezeID: String = UUID.randomUUID().toString()
        ): CanonicalMigrationMatrixFreezeResult {
            return CanonicalMigrationMatrixFreezeResult(
                frozen = frozen,
                soleActivePilot = soleActivePilot,
                otherDomainsStatic = otherDomainsStatic,
                blockers = blockers.sorted(),
                freezeID = freezeID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
                diagnosticsSummary = listOf(
                    "frozen=$frozen",
                    "soleActivePilot=${soleActivePilot?.rawValue ?: "none"}",
                    "otherDomainsStatic=$otherDomainsStatic",
                    "blockers=${blockers.joinToString("+").ifEmpty { "none" }}"
                ).joinToString(",")
            )
        }

        fun notFrozen(): CanonicalMigrationMatrixFreezeResult {
            return CanonicalMigrationMatrixFreezeResult(frozen = false)
        }

        fun frozen(
            soleActivePilot: CanonicalMigrationDomain?,
            otherDomainsStatic: Boolean,
            blockers: List<String>
        ): CanonicalMigrationMatrixFreezeResult {
            return CanonicalMigrationMatrixFreezeResult(
                frozen = true,
                soleActivePilot = soleActivePilot,
                otherDomainsStatic = otherDomainsStatic,
                blockers = blockers
            )
        }
    }
}

object CanonicalMigrationMatrixGuard {
    enum class GuardViolation(val rawValue: String) {
        MULTIPLE_ACTIVE_PILOTS("multipleActivePilots"),
        ACTIVE_PILOT_REQUIRED("activePilotRequired"),
        STATIC_ONLY_CANNOT_HAVE_ACTIVE_PILOT("staticOnlyCannotHaveActivePilot"),
        STAGE_NOT_ALLOWED_FOR_DOMAIN("stageNotAllowedForDomain"),
        CANARY_REQUIRES_BUDGET("canaryRequiresBudget"),
        PRODUCTION_COMMIT_REQUIRES_APPROVAL("productionCommitRequiresApproval"),
        READ_SIDE_CUTOVER_REQUIRES_PARALLEL("readSideCutoverRequiresParallel"),
        LEGACY_RETIREMENT_REQUIRES_CUTOVER("legacyRetirementRequiresCutover"),
        DOMAIN_NOT_IN_MATRIX("domainNotInMatrix"),
        INCONSISTENT_STAGE_CONFIGURATION("inconsistentStageConfiguration")
    }

    data class GuardResult(
        val allowed: Boolean,
        val violations: List<GuardViolation>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun allowed(): GuardResult {
                return GuardResult(
                    allowed = true,
                    violations = emptyList(),
                    diagnosticsSummary = "allowed=true"
                )
            }

            fun blocked(violations: List<GuardViolation>): GuardResult {
                return GuardResult(
                    allowed = false,
                    violations = violations.distinct().sortedBy { it.rawValue },
                    diagnosticsSummary = listOf(
                        "allowed=false",
                        "violations=${violations.map { it.rawValue }.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun evaluate(
        config: CanonicalMigrationMatrixConfig
    ): GuardResult {
        val violations = mutableListOf<GuardViolation>()

        val activePilots = config.activePilotDomains()
        if (activePilots.size > 1) {
            violations.add(GuardViolation.MULTIPLE_ACTIVE_PILOTS)
        }

        for (state in config.domains) {
            if (state.staticOnly && state.activePilot) {
                violations.add(GuardViolation.STATIC_ONLY_CANNOT_HAVE_ACTIVE_PILOT)
            }

            if (state.stage != CanonicalMigrationStage.OFF && state.staticOnly) {
                violations.add(GuardViolation.STAGE_NOT_ALLOWED_FOR_DOMAIN)
            }

            if (state.stage == CanonicalMigrationStage.CANARY_N1 && state.canaryBudget <= 0) {
                violations.add(GuardViolation.CANARY_REQUIRES_BUDGET)
            }

            if (state.readSideCutover && !state.readSideParallel) {
                violations.add(GuardViolation.READ_SIDE_CUTOVER_REQUIRES_PARALLEL)
            }

            if (state.legacyRetirement && !state.defaultCutover) {
                violations.add(GuardViolation.LEGACY_RETIREMENT_REQUIRES_CUTOVER)
            }
        }

        return if (violations.isEmpty()) {
            GuardResult.allowed()
        } else {
            GuardResult.blocked(violations)
        }
    }
}

data class CanonicalMigrationMatrixDiagnostics
private constructor(
    val configID: String,
    val totalDomains: Int,
    val activePilotDomain: String?,
    val activePilotStage: String?,
    val staticOnlyDomains: List<String>,
    val domainsByStage: Map<String, List<String>>,
    val frozen: Boolean,
    val guardAllowed: Boolean,
    val diagnosticsSummary: String
) {
    companion object {
        operator fun invoke(
            config: CanonicalMigrationMatrixConfig,
            freezeResult: CanonicalMigrationMatrixFreezeResult,
            guardResult: CanonicalMigrationMatrixGuard.GuardResult
        ): CanonicalMigrationMatrixDiagnostics {
            val activePilot = config.activePilotDomains().firstOrNull()
            val staticDomains = config.staticOnlyDomains().map { it.domain.rawValue }
            val stageMap = CanonicalMigrationStage.allCases.associate { stage ->
                stage.rawValue to config.domainsAtStage(stage).map { it.domain.rawValue }
            }.filter { it.value.isNotEmpty() }

            return CanonicalMigrationMatrixDiagnostics(
                configID = config.configID,
                totalDomains = config.domains.size,
                activePilotDomain = activePilot?.domain?.rawValue,
                activePilotStage = activePilot?.stage?.rawValue,
                staticOnlyDomains = staticDomains,
                domainsByStage = stageMap,
                frozen = freezeResult.frozen,
                guardAllowed = guardResult.allowed,
                diagnosticsSummary = listOf(
                    "configID=${config.configID}",
                    "total=${config.domains.size}",
                    "activePilot=${activePilot?.domain?.rawValue ?: "none"}",
                    "activeStage=${activePilot?.stage?.rawValue ?: "none"}",
                    "staticDomains=${staticDomains.joinToString("|")}",
                    "stages=${stageMap.keys.joinToString("|")}",
                    "frozen=${freezeResult.frozen}",
                    "guardAllowed=${guardResult.allowed}"
                ).joinToString(",")
            )
        }
    }

    fun domainsAtStage(stage: CanonicalMigrationStage): List<String> {
        return domainsByStage[stage.rawValue] ?: emptyList()
    }
}

object CanonicalMigrationMatrixHelper {
    fun isActivePilot(
        config: CanonicalMigrationMatrixConfig,
        domain: CanonicalMigrationDomain
    ): Boolean {
        return config.stateFor(domain)?.activePilot == true
    }

    fun isStaticOnly(
        config: CanonicalMigrationMatrixConfig,
        domain: CanonicalMigrationDomain
    ): Boolean {
        return config.stateFor(domain)?.staticOnly == true
    }

    fun defaultOff(
        config: CanonicalMigrationMatrixConfig,
        domain: CanonicalMigrationDomain
    ): Boolean {
        val state = config.stateFor(domain) ?: return true
        return state.stage == CanonicalMigrationStage.OFF && !state.activePilot
    }

    fun stageFor(
        config: CanonicalMigrationMatrixConfig,
        domain: CanonicalMigrationDomain
    ): CanonicalMigrationStage {
        return config.stateFor(domain)?.stage ?: CanonicalMigrationStage.OFF
    }

    fun canaryBudgetFor(
        config: CanonicalMigrationMatrixConfig,
        domain: CanonicalMigrationDomain
    ): Int {
        return config.stateFor(domain)?.canaryBudget ?: 0
    }

    fun activePilotDomain(
        config: CanonicalMigrationMatrixConfig
    ): CanonicalMigrationDomain? {
        return config.activePilotDomains().firstOrNull()?.domain
    }

    fun domainsEligibleForStage(
        config: CanonicalMigrationMatrixConfig,
        stage: CanonicalMigrationStage,
        excludeStaticOnly: Boolean = true
    ): List<CanonicalMigrationDomain> {
        return config.domains
            .filter {
                val stageOk = it.stage == stage
                val staticOk = !excludeStaticOnly || !it.staticOnly
                stageOk && staticOk
            }
            .map { it.domain }
            .sortedBy { it.rawValue }
    }

    fun computeFreeze(
        config: CanonicalMigrationMatrixConfig
    ): CanonicalMigrationMatrixFreezeResult {
        val activePilots = config.activePilotDomains()
        val blockers = mutableListOf<String>()

        if (activePilots.size > 1) {
            blockers.add("multipleActivePilots:${activePilots.map { it.domain.rawValue }}")
            return CanonicalMigrationMatrixFreezeResult.frozen(
                soleActivePilot = null,
                otherDomainsStatic = false,
                blockers = blockers
            )
        }

        val soleActivePilot = activePilots.firstOrNull()
        val nonPilotActive = config.domains
            .filter { it != soleActivePilot && it.isMigrationActive }

        if (nonPilotActive.isNotEmpty()) {
            blockers.add("otherDomainsActive:${nonPilotActive.map { it.domain.rawValue }}")
        }

        val allStaticExceptPilot = config.domains
            .filter { it != soleActivePilot }
            .all { it.staticOnly || it.stage == CanonicalMigrationStage.OFF }

        return if (blockers.isEmpty()) {
            CanonicalMigrationMatrixFreezeResult.notFrozen()
        } else {
            CanonicalMigrationMatrixFreezeResult.frozen(
                soleActivePilot = soleActivePilot?.domain,
                otherDomainsStatic = allStaticExceptPilot,
                blockers = blockers
            )
        }
    }
}
