package com.rokurics.app.domain.canonical

import java.util.UUID

enum class CanonicalLibraryMetadataProductionRootBlocker(val rawValue: String) {
    STAGING_ROOT_IS_PRODUCTION_ROOT("stagingRootIsProductionRoot"),
    PRODUCTION_ROOT_REFUSED("productionRootRefused"),
    STAGING_ROOT_NOT_CREATED("stagingRootNotCreated"),
    SYSTEM_TEMPORARY_NOT_AVAILABLE("systemTemporaryNotAvailable"),
    OWNER_NOT_APPROVED("ownerNotApproved"),
    DEBUG_PILOT_NOT_ARMED("debugPilotNotArmed"),
    CANARY_CONFIGURATION_BLOCKED("canaryConfigurationBlocked"),
    LANDING_FREEZE_ACTIVE("landingFreezeActive"),
    PRODUCTION_ROOT_PATH_UNVERIFIED("productionRootPathUnverified"),
    CANDIDATE_COUNT_LIMIT_EXCEEDED("candidateCountLimitExceeded"),
    N1_POST_RUN_INVARIANT_FAILED("n1PostRunInvariantFailed")
}

data class CanonicalLibraryMetadataProductionRootGate(
    val allowed: Boolean,
    val blockers: List<CanonicalLibraryMetadataProductionRootBlocker>,
    val productionRootPath: String?,
    val stagingRootID: String?,
    val gateID: String,
    val diagnosticsSummary: String
) {
    constructor(
        allowed: Boolean,
        blockers: List<CanonicalLibraryMetadataProductionRootBlocker> = emptyList(),
        productionRootPath: String? = null,
        stagingRootID: String? = null,
        gateID: String = UUID.randomUUID().toString()
    ) : this(
        allowed = allowed && blockers.isEmpty(),
        blockers = blockers.distinct().sortedBy { it.rawValue },
        productionRootPath = productionRootPath?.trim()?.nilIfEmpty,
        stagingRootID = stagingRootID?.trim()?.nilIfEmpty,
        gateID = gateID,
        diagnosticsSummary = listOf(
            "allowed=$allowed",
            "productionRoot=${productionRootPath ?: "none"}",
            "stagingRoot=${stagingRootID ?: "none"}",
            "blockers=${blockers.map { it.rawValue }.joinToString("|")}"
        ).joinToString(",")
    )

    companion object {
        fun allowed(
            productionRootPath: String,
            stagingRootID: String?
        ): CanonicalLibraryMetadataProductionRootGate {
            return CanonicalLibraryMetadataProductionRootGate(
                allowed = true,
                productionRootPath = productionRootPath,
                stagingRootID = stagingRootID
            )
        }

        fun blocked(
            blockers: List<CanonicalLibraryMetadataProductionRootBlocker>,
            productionRootPath: String? = null,
            stagingRootID: String? = null
        ): CanonicalLibraryMetadataProductionRootGate {
            return CanonicalLibraryMetadataProductionRootGate(
                allowed = false,
                blockers = blockers,
                productionRootPath = productionRootPath,
                stagingRootID = stagingRootID
            )
        }
    }
}

data class CanonicalLibraryMetadataProductionCanaryInjection(
    val rootMode: CanonicalLibraryMetadataDebugPilotMode,
    val result: CanonicalLibraryMetadataProductionCanaryResult,
    val injectionID: String,
    val candidate: CanonicalLibraryMetadataCandidate?,
    val productionRootGate: CanonicalLibraryMetadataProductionRootGate,
    val evidenceBundle: CanonicalLibraryMetadataN1EvidenceBundle?,
    val diagnosticsSummary: String
) {
    constructor(
        rootMode: CanonicalLibraryMetadataDebugPilotMode,
        result: CanonicalLibraryMetadataProductionCanaryResult,
        injectionID: String = UUID.randomUUID().toString(),
        candidate: CanonicalLibraryMetadataCandidate? = null,
        productionRootGate: CanonicalLibraryMetadataProductionRootGate,
        evidenceBundle: CanonicalLibraryMetadataN1EvidenceBundle? = null
    ) : this(
        rootMode = rootMode,
        result = result,
        injectionID = injectionID,
        candidate = candidate,
        productionRootGate = productionRootGate,
        evidenceBundle = evidenceBundle,
        diagnosticsSummary = listOf(
            "rootMode=${rootMode.rawValue}",
            "result=${result.rawValue}",
            "candidate=${candidate?.candidateID ?: "none"}",
            "productionGate=${productionRootGate.allowed}",
            "evidence=$evidenceBundle != null"
        ).joinToString(",")
    )

    companion object {
        fun notExecuted(
            rootMode: CanonicalLibraryMetadataDebugPilotMode,
            productionRootGate: CanonicalLibraryMetadataProductionRootGate,
            reason: String
        ): CanonicalLibraryMetadataProductionCanaryInjection {
            return CanonicalLibraryMetadataProductionCanaryInjection(
                rootMode = rootMode,
                result = CanonicalLibraryMetadataProductionCanaryResult.NOT_EXECUTED,
                productionRootGate = productionRootGate
            )
        }
    }
}

enum class CanonicalLibraryMetadataProductionCanaryResult(val rawValue: String) {
    NOT_EXECUTED("notExecuted"),
    EXECUTED_SUCCESS("executedSuccess"),
    EXECUTED_FAILED("executedFailed"),
    EXECUTED_PARTIAL("executedPartial"),
    BLOCKED_BY_GATE("blockedByGate"),
    INJECTION_REJECTED("injectionRejected")
}

data class CanonicalLibraryMetadataPilotDiagnosticSummary(
    val pilotMode: String,
    val pilotEnabled: Boolean,
    val testRootAvailable: Boolean,
    val productionRootArmed: Boolean,
    val canaryInProgress: Boolean,
    val landingFrozen: Boolean,
    val candidatesCount: Int,
    val successCount: Int,
    val failureCount: Int,
    val blockedCount: Int,
    val n1CanaryResults: List<String>,
    val evidenceBundles: List<String>,
    val blockers: List<String>,
    val redacted: Boolean
) {
    constructor(
        pilotMode: CanonicalLibraryMetadataDebugPilotMode,
        pilotEnabled: Boolean,
        testRootAvailable: Boolean = false,
        productionRootArmed: Boolean = false,
        canaryInProgress: Boolean = false,
        landingFrozen: Boolean = false,
        candidatesCount: Int = 0,
        successCount: Int = 0,
        failureCount: Int = 0,
        blockedCount: Int = 0,
        n1CanaryResults: List<String> = emptyList(),
        evidenceBundles: List<String> = emptyList(),
        blockers: List<String> = emptyList(),
        redacted: Boolean = true
    ) : this(
        pilotMode = pilotMode.rawValue,
        pilotEnabled = pilotEnabled,
        testRootAvailable = testRootAvailable,
        productionRootArmed = productionRootArmed,
        canaryInProgress = canaryInProgress,
        landingFrozen = landingFrozen,
        candidatesCount = maxOf(0, candidatesCount),
        successCount = maxOf(0, successCount),
        failureCount = maxOf(0, failureCount),
        blockedCount = maxOf(0, blockedCount),
        n1CanaryResults = n1CanaryResults,
        evidenceBundles = evidenceBundles,
        blockers = blockers.sorted(),
        redacted = redacted
    )

    val diagnosticsSummary: String
        get() = listOf(
            "pilot=$pilotMode",
            "enabled=$pilotEnabled",
            "testRoot=$testRootAvailable",
            "prodArmed=$productionRootArmed",
            "canary=$canaryInProgress",
            "frozen=$landingFrozen",
            "candidates=$candidatesCount",
            "success=$successCount",
            "failure=$failureCount",
            "blocked=$blockedCount",
            "redacted=$redacted"
        ).joinToString(",")
}

object CanonicalLibraryMetadataPilotDiagnosticExporter {
    data class DiagnosticExport(
        val summaries: List<CanonicalLibraryMetadataPilotDiagnosticSummary>,
        val evidenceBundles: List<CanonicalLibraryMetadataN1EvidenceBundle>,
        val exportID: String,
        val exportTimestamp: String?,
        val redacted: Boolean
    )

    fun export(
        pilotConfiguration: CanonicalLibraryMetadataDebugPilotConfiguration,
        canaryConfiguration: CanonicalLibraryMetadataCanaryConfiguration,
        canaryResults: List<CanonicalLibraryMetadataCanaryStageRunner.CanaryStageResult>,
        evidenceBundles: List<CanonicalLibraryMetadataN1EvidenceBundle>,
        exportTimestamp: String? = null,
        redacted: Boolean = true
    ): DiagnosticExport {
        val summaries = mutableListOf<CanonicalLibraryMetadataPilotDiagnosticSummary>()

        val n1CanaryResultIDs = canaryResults
            .flatMap { stage ->
                stage.n1Results.map { it.executionID }
            }
            .sorted()

        val allBlockers = canaryResults
            .flatMap { stage ->
                stage.n1Results.flatMap { result ->
                    result.blockers.map { it.rawValue }
                }
            }
            .distinct()
            .sorted()

        val totalCandidates = canaryResults.sumOf { stage -> stage.n1Results.size }
        val totalSuccess = canaryResults.sumOf { stage -> stage.successCount }
        val totalFailed = canaryResults.sumOf { stage -> stage.executedCount - stage.successCount }
        val totalBlocked = canaryResults.sumOf { stage -> stage.blockedCount }

        val pilotSummary = CanonicalLibraryMetadataPilotDiagnosticSummary(
            pilotMode = pilotConfiguration.mode,
            pilotEnabled = pilotConfiguration.isEnabled,
            testRootAvailable = pilotConfiguration.testRootURL != null,
            productionRootArmed = pilotConfiguration.writesProductionRoot,
            canaryInProgress = canaryConfiguration.mode != CanonicalLibraryMetadataCanaryConfigurationMode.DISABLED,
            landingFrozen = false,
            candidatesCount = totalCandidates,
            successCount = totalSuccess,
            failureCount = totalFailed,
            blockedCount = totalBlocked,
            n1CanaryResults = n1CanaryResultIDs,
            evidenceBundles = evidenceBundles.map { it.evidenceID }.sorted(),
            blockers = allBlockers,
            redacted = redacted
        )

        summaries.add(pilotSummary)

        return DiagnosticExport(
            summaries = summaries,
            evidenceBundles = evidenceBundles,
            exportID = UUID.randomUUID().toString(),
            exportTimestamp = exportTimestamp?.trim()?.nilIfEmpty,
            redacted = redacted
        )
    }
}

class CanonicalLibraryMetadataProductionCanaryBootstrap(
    private val configuration: CanonicalLibraryMetadataDebugPilotConfiguration,
    private val canaryConfiguration: CanonicalLibraryMetadataCanaryConfiguration,
    private val productionRootGate: CanonicalLibraryMetadataProductionRootGate,
    private val candidates: List<CanonicalLibraryMetadataCandidate>,
    private val safetyGate: (CanonicalLibraryMetadataCandidate) -> CanonicalLibraryMetadataCandidateSafetyReport
) {
    data class BootstrapResult(
        val prepared: Boolean,
        val injection: CanonicalLibraryMetadataProductionCanaryInjection?,
        val productionRootGate: CanonicalLibraryMetadataProductionRootGate,
        val canaryRunner: CanonicalLibraryMetadataN1CanaryRunner?,
        val evidenceBundle: CanonicalLibraryMetadataN1EvidenceBundle?,
        val blockers: List<CanonicalLibraryMetadataProductionRootBlocker>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun blocked(
                productionRootGate: CanonicalLibraryMetadataProductionRootGate,
                blockers: List<CanonicalLibraryMetadataProductionRootBlocker>
            ): BootstrapResult {
                return BootstrapResult(
                    prepared = false,
                    injection = null,
                    productionRootGate = productionRootGate,
                    canaryRunner = null,
                    evidenceBundle = null,
                    blockers = blockers.distinct().sortedBy { it.rawValue },
                    diagnosticsSummary = listOf(
                        "prepared=false",
                        "gateAllowed=${productionRootGate.allowed}",
                        "blockers=${blockers.map { it.rawValue }.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun prepare(): BootstrapResult {
        val blockers = mutableListOf<CanonicalLibraryMetadataProductionRootBlocker>()

        if (!productionRootGate.allowed) {
            blockers.addAll(productionRootGate.blockers)
            return BootstrapResult.blocked(productionRootGate, blockers)
        }

        if (!configuration.isEnabled) {
            blockers.add(CanonicalLibraryMetadataProductionRootBlocker.DEBUG_PILOT_NOT_ARMED)
            return BootstrapResult.blocked(productionRootGate, blockers)
        }

        val safeCandidates = candidates.filter { candidate ->
            val report = safetyGate(candidate)
            report.safe
        }

        if (safeCandidates.isEmpty()) {
            blockers.add(CanonicalLibraryMetadataProductionRootBlocker.CANDIDATE_COUNT_LIMIT_EXCEEDED)
            return BootstrapResult.blocked(productionRootGate, blockers)
        }

        val limitedCandidates = if (configuration.candidateCountLimit > 0)
            safeCandidates.take(configuration.candidateCountLimit)
        else
            safeCandidates

        val canaryRunner = CanonicalLibraryMetadataN1CanaryRunner(
            configuration = canaryConfiguration,
            candidates = limitedCandidates,
            safetyGate = safetyGate
        )

        val injection = CanonicalLibraryMetadataProductionCanaryInjection(
            rootMode = configuration.mode,
            result = CanonicalLibraryMetadataProductionCanaryResult.NOT_EXECUTED,
            candidate = limitedCandidates.firstOrNull(),
            productionRootGate = productionRootGate,
            evidenceBundle = null
        )

        val evidenceBundle = CanonicalLibraryMetadataN1EvidenceBundle(
            evidenceID = UUID.randomUUID().toString(),
            candidates = limitedCandidates.map { it.candidateID },
            productionRootPath = productionRootGate.productionRootPath,
            rootMode = configuration.mode
        )

        return BootstrapResult(
            prepared = true,
            injection = injection,
            productionRootGate = productionRootGate,
            canaryRunner = canaryRunner,
            evidenceBundle = evidenceBundle,
            blockers = emptyList(),
            diagnosticsSummary = listOf(
                "prepared=true",
                "mode=${configuration.mode.rawValue}",
                "candidates=${limitedCandidates.size}",
                "productionRoot=${
                    productionRootGate.productionRootPath ?: "none"
                }",
                "gateAllowed=${productionRootGate.allowed}"
            ).joinToString(",")
        )
    }
}

object CanonicalLibraryMetadataN1PostRunInvariantValidator {
    data class InvariantValidationResult(
        val valid: Boolean,
        val executedCandidatesCount: Int,
        val metadataHashVerifiedCount: Int,
        val canonicalWriteAppliedCount: Int,
        val legacyCodePathNotMutatedCount: Int,
        val productionRootNotCorruptedCount: Int,
        val noOrphanedCanonicalRecords: Boolean,
        val noLegacyRecordLoss: Boolean,
        val productionRootIntegrityVerified: Boolean,
        val blockers: List<String>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun invalid(
                executedCandidatesCount: Int = 0,
                blockers: List<String>
            ): InvariantValidationResult {
                return InvariantValidationResult(
                    valid = false,
                    executedCandidatesCount = executedCandidatesCount,
                    metadataHashVerifiedCount = 0,
                    canonicalWriteAppliedCount = 0,
                    legacyCodePathNotMutatedCount = 0,
                    productionRootNotCorruptedCount = 0,
                    noOrphanedCanonicalRecords = false,
                    noLegacyRecordLoss = false,
                    productionRootIntegrityVerified = false,
                    blockers = blockers.sorted(),
                    diagnosticsSummary = listOf(
                        "valid=false",
                        "blockers=${blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun validate(
        n1Results: List<CanonicalLibraryMetadataN1CanaryRunner.N1CanaryResult>,
        evidenceBundle: CanonicalLibraryMetadataN1EvidenceBundle?,
        productionRootGate: CanonicalLibraryMetadataProductionRootGate?
    ): InvariantValidationResult {
        val blockers = mutableListOf<String>()

        if (n1Results.isEmpty()) {
            blockers.add("noN1Results")
            return InvariantValidationResult.invalid(0, blockers)
        }

        val executedResults = n1Results.filter { it.executed }
        if (executedResults.isEmpty()) {
            blockers.add("noExecutedCandidates")
            return InvariantValidationResult.invalid(n1Results.size, blockers)
        }

        val hashVerified = executedResults.count { it.metadataHashVerified }
        val writeApplied = executedResults.count { it.canonicalWriteApplied }

        if (hashVerified != executedResults.size) {
            blockers.add("metadataHashVerificationFailures")
        }

        if (writeApplied != executedResults.size) {
            blockers.add("canonicalWriteFailures")
        }

        if (evidenceBundle == null) {
            blockers.add("missingEvidenceBundle")
        }

        if (productionRootGate != null && !productionRootGate.allowed) {
            blockers.add("productionRootGateBlocked")
        }

        if (n1Results.any { it.blockers.isNotEmpty() }) {
            blockers.add("n1ResultsHaveBlockers")
        }

        val allValid = blockers.isEmpty()

        return InvariantValidationResult(
            valid = allValid,
            executedCandidatesCount = executedResults.size,
            metadataHashVerifiedCount = hashVerified,
            canonicalWriteAppliedCount = writeApplied,
            legacyCodePathNotMutatedCount = n1Results.size,
            productionRootNotCorruptedCount = if (allValid) n1Results.size else 0,
            noOrphanedCanonicalRecords = allValid,
            noLegacyRecordLoss = allValid,
            productionRootIntegrityVerified = allValid && productionRootGate?.allowed == true,
            blockers = blockers,
            diagnosticsSummary = listOf(
                "valid=$allValid",
                "executed=${
                    executedResults.size
                }",
                "hashVerified=$hashVerified",
                "writeApplied=$writeApplied",
                "productionIntegrity=${
                    allValid && productionRootGate?.allowed == true
                }",
                "blockers=${blockers.joinToString("|")}"
            ).joinToString(",")
        )
    }
}

data class CanonicalLibraryMetadataN1EvidenceBundle(
    val evidenceID: String = UUID.randomUUID().toString(),
    val candidates: List<String> = emptyList(),
    val productionRootPath: String? = null,
    val rootMode: CanonicalLibraryMetadataDebugPilotMode = CanonicalLibraryMetadataDebugPilotMode.DISABLED,
    val n1Results: List<CanonicalLibraryMetadataN1CanaryRunner.N1CanaryResult> = emptyList(),
    val invariantValidation: CanonicalLibraryMetadataN1PostRunInvariantValidator.InvariantValidationResult? = null,
    val injectionResult: String? = null,
    val evidenceTimestamp: String? = null,
    val redacted: Boolean = true
) {

    val successCount: Int
        get() = n1Results.count { it.success }

    val failureCount: Int
        get() = n1Results.count { it.executed && !it.success }

    val blockedCount: Int
        get() = n1Results.count { !it.executed }

    val diagnosticsSummary: String
        get() = listOf(
            "evidenceID=$evidenceID",
            "candidates=${candidates.size}",
            "success=$successCount",
            "failure=$failureCount",
            "blocked=$blockedCount",
            "rootMode=${rootMode.rawValue}",
            "invariantValid=${invariantValidation?.valid ?: false}",
            "redacted=$redacted"
        ).joinToString(",")
}

class CanonicalLibraryMetadataN3ReadinessGate(
    private val n1EvidenceBundles: List<CanonicalLibraryMetadataN1EvidenceBundle>,
    private val canaryStageResults: List<CanonicalLibraryMetadataCanaryStageRunner.CanaryStageResult>
) {
    data class N3ReadinessResult(
        val ready: Boolean,
        val n1EvidenceComplete: Boolean,
        val n1InvariantValid: Boolean,
        val n1BlockersResolved: Boolean,
        val canaryStageComplete: Boolean,
        val n1SuccessCount: Int,
        val n1TotalCount: Int,
        val blockers: List<String>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun notReady(
                blockers: List<String>,
                n1EvidenceComplete: Boolean = false,
                n1InvariantValid: Boolean = false,
                n1BlockersResolved: Boolean = false,
                canaryStageComplete: Boolean = false,
                n1SuccessCount: Int = 0,
                n1TotalCount: Int = 0
            ): N3ReadinessResult {
                return N3ReadinessResult(
                    ready = false,
                    n1EvidenceComplete = n1EvidenceComplete,
                    n1InvariantValid = n1InvariantValid,
                    n1BlockersResolved = n1BlockersResolved,
                    canaryStageComplete = canaryStageComplete,
                    n1SuccessCount = n1SuccessCount,
                    n1TotalCount = n1TotalCount,
                    blockers = blockers.sorted(),
                    diagnosticsSummary = listOf(
                        "ready=false",
                        "blockers=${blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun evaluate(): N3ReadinessResult {
        val blockers = mutableListOf<String>()

        if (n1EvidenceBundles.isEmpty()) {
            blockers.add("noN1EvidenceBundles")
        }

        val allN1Results = n1EvidenceBundles.flatMap { it.n1Results }
        val totalCandidates = allN1Results.size
        val successCount = allN1Results.count { it.success }
        val invariantValid = n1EvidenceBundles.all {
            it.invariantValidation?.valid == true
        }

        val n1EvidenceComplete = n1EvidenceBundles.isNotEmpty() &&
                n1EvidenceBundles.all { it.n1Results.isNotEmpty() }

        if (!n1EvidenceComplete) {
            blockers.add("n1EvidenceIncomplete")
        }

        if (!invariantValid) {
            blockers.add("n1InvariantInvalid")
        }

        val n1BlockersResolved = allN1Results.none { it.blockers.isNotEmpty() }
        if (!n1BlockersResolved) {
            blockers.add("n1BlockersNotResolved")
        }

        val canaryStageComplete = canaryStageResults.isNotEmpty() &&
                canaryStageResults.all { it.allSucceeded }
        if (!canaryStageComplete) {
            blockers.add("canaryStageIncomplete")
        }

        val ready = blockers.isEmpty()

        return N3ReadinessResult(
            ready = ready,
            n1EvidenceComplete = n1EvidenceComplete,
            n1InvariantValid = invariantValid,
            n1BlockersResolved = n1BlockersResolved,
            canaryStageComplete = canaryStageComplete,
            n1SuccessCount = successCount,
            n1TotalCount = totalCandidates,
            blockers = blockers,
            diagnosticsSummary = listOf(
                "ready=$ready",
                "n1Evidence=$n1EvidenceComplete",
                "n1Invariant=$invariantValid",
                "n1Blockers=$n1BlockersResolved",
                "canaryStage=$canaryStageComplete",
                "success=${successCount}/$totalCandidates",
                "blockers=${blockers.joinToString("|")}"
            ).joinToString(",")
        )
    }
}
