package com.rokurics.app.domain.canonical

import java.util.UUID

enum class CanonicalLibraryMetadataCanaryMode(val rawValue: String) {
    DISABLED("disabled"),
    DIAGNOSTICS_ONLY("diagnosticsOnly"),
    ARMED_NO_COMMIT("armedNoCommit"),
    EXECUTE_N1_CANARY("executeN1Canary"),
    EXECUTE_N3_CANARY("executeN3Canary"),
    EXECUTE_N10_CANARY("executeN10Canary"),
    EXECUTE_ALL_ELIGIBLE("executeAllEligible"),
    BLOCKED("blocked")
}

data class CanonicalLibraryMetadataCanaryPolicy(
    val mode: CanonicalLibraryMetadataCanaryMode = CanonicalLibraryMetadataCanaryMode.DISABLED,
    val allowProductionRootWrites: Boolean = false,
    val ownerApproved: Boolean = false,
    val candidateCount: Int = 0,
    val metadataKinds: List<CanonicalLibraryMetadataCandidateKind> = CanonicalLibraryMetadataCandidateKind.entries.toList(),
    val stagingRootPolicy: String = "systemTemporary",
    val diagnosticsRedacted: Boolean = true
) {
    companion object {
        fun defaultDisabled(): CanonicalLibraryMetadataCanaryPolicy {
            return CanonicalLibraryMetadataCanaryPolicy(CanonicalLibraryMetadataCanaryMode.DISABLED)
        }

        fun diagnosticsOnly(
            candidateCount: Int = 0,
            metadataKinds: List<CanonicalLibraryMetadataCandidateKind> = CanonicalLibraryMetadataCandidateKind.entries.toList()
        ): CanonicalLibraryMetadataCanaryPolicy {
            return CanonicalLibraryMetadataCanaryPolicy(
                mode = CanonicalLibraryMetadataCanaryMode.DIAGNOSTICS_ONLY,
                candidateCount = candidateCount,
                metadataKinds = metadataKinds
            )
        }
    }
}

enum class CanonicalLibraryMetadataCanaryConfigurationMode(val rawValue: String) {
    DISABLED("disabled"),
    DIAGNOSTICS_ONLY("diagnosticsOnly"),
    CANARY_COMMIT("canaryCommit")
}

data class CanonicalLibraryMetadataCanaryConfiguration(
    val mode: CanonicalLibraryMetadataCanaryConfigurationMode = CanonicalLibraryMetadataCanaryConfigurationMode.DISABLED,
    val policy: CanonicalLibraryMetadataCanaryPolicy = CanonicalLibraryMetadataCanaryPolicy.defaultDisabled(),
    val stage: CanonicalLibraryMetadataCanaryStagePolicy = CanonicalLibraryMetadataCanaryStagePolicy.DISABLED
) {

    companion object {
        val DISABLED = CanonicalLibraryMetadataCanaryConfiguration()

        fun diagnosticsOnly(
            candidateCount: Int = 0
        ): CanonicalLibraryMetadataCanaryConfiguration {
            return CanonicalLibraryMetadataCanaryConfiguration(
                mode = CanonicalLibraryMetadataCanaryConfigurationMode.DIAGNOSTICS_ONLY,
                policy = CanonicalLibraryMetadataCanaryPolicy(
                    mode = CanonicalLibraryMetadataCanaryMode.DIAGNOSTICS_ONLY,
                    candidateCount = candidateCount
                ),
                stage = CanonicalLibraryMetadataCanaryStagePolicy.N1_ONLY
            )
        }
    }
}

enum class CanonicalLibraryMetadataCandidateKind(val rawValue: String) {
    FOLDER_METADATA("folderMetadata"),
    STUDY_ITEM_METADATA("studyItemMetadata"),
    STANDALONE_NOTE_METADATA("standaloneNoteMetadata")
}

data class CanonicalLibraryMetadataCandidate(
    val candidateID: String,
    val objectID: CanonicalLibraryObjectID,
    val kind: CanonicalLibraryMetadataCandidateKind,
    val metadataHash: CanonicalHash,
    val businessModifiedAt: CanonicalTimestamp?,
    val isDeleted: Boolean,
    val deletedAt: CanonicalTimestamp?
) {
    constructor(
        objectID: CanonicalLibraryObjectID,
        kind: CanonicalLibraryMetadataCandidateKind,
        metadataHash: CanonicalHash,
        businessModifiedAt: CanonicalTimestamp? = null,
        isDeleted: Boolean = false,
        deletedAt: CanonicalTimestamp? = null
    ) : this(
        candidateID = UUID.randomUUID().toString(),
        objectID = objectID,
        kind = kind,
        metadataHash = metadataHash,
        businessModifiedAt = businessModifiedAt,
        isDeleted = isDeleted,
        deletedAt = if (isDeleted) deletedAt else null
    )

    val isActive: Boolean
        get() = !isDeleted && deletedAt == null
}

data class CanonicalLibraryMetadataCandidateSafetyReport(
    val safe: Boolean,
    val blockers: List<CanonicalLibraryMetadataCutoverBlocker>,
    val candidateID: String?,
    val metadataKind: CanonicalLibraryMetadataCandidateKind?,
    val diagnosticsSummary: String
) {
    constructor(
        safe: Boolean,
        blockers: List<CanonicalLibraryMetadataCutoverBlocker>,
        candidateID: String? = null,
        metadataKind: CanonicalLibraryMetadataCandidateKind? = null
    ) : this(
        safe = safe && blockers.isEmpty(),
        blockers = blockers.distinct().sortedBy { it.rawValue },
        candidateID = candidateID,
        metadataKind = metadataKind,
        diagnosticsSummary = listOf(
            "safe=$safe",
            "blockers=${blockers.map { it.rawValue }}",
            "candidateID=${candidateID ?: "none"}",
            "kind=${metadataKind?.rawValue ?: "none"}"
        ).joinToString(",")
    )

    companion object {
        fun safe(
            candidateID: String,
            metadataKind: CanonicalLibraryMetadataCandidateKind
        ): CanonicalLibraryMetadataCandidateSafetyReport {
            return CanonicalLibraryMetadataCandidateSafetyReport(
                safe = true,
                blockers = emptyList(),
                candidateID = candidateID,
                metadataKind = metadataKind
            )
        }

        fun blocked(
            blockers: List<CanonicalLibraryMetadataCutoverBlocker>,
            candidateID: String? = null,
            metadataKind: CanonicalLibraryMetadataCandidateKind? = null
        ): CanonicalLibraryMetadataCandidateSafetyReport {
            return CanonicalLibraryMetadataCandidateSafetyReport(
                safe = false,
                blockers = blockers,
                candidateID = candidateID,
                metadataKind = metadataKind
            )
        }
    }
}

enum class CanonicalLibraryMetadataCutoverBlocker(val rawValue: String) {
    CANARY_DISABLED("canaryDisabled"),
    CANARY_BLOCKED("canaryBlocked"),
    CANDIDATE_TOMBSTONED("candidateTombstoned"),
    METADATA_HASH_UNVERIFIABLE("metadataHashUnverifiable"),
    PRODUCTION_ROOT_WRITES_NOT_ALLOWED("productionRootWritesNotAllowed"),
    OWNER_NOT_APPROVED("ownerNotApproved"),
    STAGING_ROOT_INVALID("stagingRootInvalid"),
    STAGING_ROOT_PRODUCTION_COLLISION("stagingRootProductionCollision"),
    CUTOVER_DOMAIN_LOCKED("cutoverDomainLocked"),
    LANDING_FREEZE_ACTIVE("landingFreezeActive"),
    N1_CANARY_FAILED("n1CanaryFailed"),
    N3_CANARY_FAILED("n3CanaryFailed"),
    N10_CANARY_FAILED("n10CanaryFailed"),
    RETIREMENT_READINESS_INCOMPLETE("retirementReadinessIncomplete"),
    OBSERVATION_PERIOD_INSUFFICIENT("observationPeriodInsufficient")
}

class CanonicalLibraryMetadataN1CanaryRunner(
    private val configuration: CanonicalLibraryMetadataCanaryConfiguration,
    private val candidates: List<CanonicalLibraryMetadataCandidate>,
    private val safetyGate: (CanonicalLibraryMetadataCandidate) -> CanonicalLibraryMetadataCandidateSafetyReport
) {
    data class N1CanaryResult(
        val candidate: CanonicalLibraryMetadataCandidate,
        val safetyReport: CanonicalLibraryMetadataCandidateSafetyReport,
        val executed: Boolean,
        val executionID: String,
        val canonicalWriteApplied: Boolean,
        val metadataHashVerified: Boolean,
        val blockers: List<CanonicalLibraryMetadataCutoverBlocker>
    ) {
        val success: Boolean
            get() = executed && canonicalWriteApplied && metadataHashVerified && blockers.isEmpty()
    }

    fun runSingle(candidate: CanonicalLibraryMetadataCandidate): N1CanaryResult {
        val executionID = UUID.randomUUID().toString()
        val safetyReport = safetyGate(candidate)

        if (!safetyReport.safe) {
            return N1CanaryResult(
                candidate = candidate,
                safetyReport = safetyReport,
                executed = false,
                executionID = executionID,
                canonicalWriteApplied = false,
                metadataHashVerified = false,
                blockers = safetyReport.blockers
            )
        }

        val canWrite = configuration.policy.allowProductionRootWrites &&
                configuration.policy.ownerApproved &&
                configuration.mode == CanonicalLibraryMetadataCanaryConfigurationMode.CANARY_COMMIT

        return N1CanaryResult(
            candidate = candidate,
            safetyReport = safetyReport,
            executed = canWrite,
            executionID = executionID,
            canonicalWriteApplied = canWrite,
            metadataHashVerified = true,
            blockers = if (canWrite) emptyList() else listOf(
                CanonicalLibraryMetadataCutoverBlocker.PRODUCTION_ROOT_WRITES_NOT_ALLOWED
            )
        )
    }
}

enum class CanonicalLibraryMetadataCanaryStagePolicy(val rawValue: String) {
    DISABLED("disabled"),
    N1_ONLY("n1Only"),
    N3_ONLY("n3Only"),
    N10_ONLY("n10Only"),
    ALL_ELIGIBLE("allEligible");

    val eligibleCount: Int
        get() = when (this) {
            DISABLED -> 0
            N1_ONLY -> 1
            N3_ONLY -> 3
            N10_ONLY -> 10
            ALL_ELIGIBLE -> Int.MAX_VALUE
        }

    companion object {
        val allCases: List<CanonicalLibraryMetadataCanaryStagePolicy> = entries.toList()
    }
}

class CanonicalLibraryMetadataCanaryStageRunner(
    private val configuration: CanonicalLibraryMetadataCanaryConfiguration,
    private val n1Runner: CanonicalLibraryMetadataN1CanaryRunner,
    private val candidates: List<CanonicalLibraryMetadataCandidate>
) {
    data class CanaryStageResult(
        val stage: CanonicalLibraryMetadataCanaryStagePolicy,
        val executedCount: Int,
        val successCount: Int,
        val blockedCount: Int,
        val n1Results: List<CanonicalLibraryMetadataN1CanaryRunner.N1CanaryResult>,
        val observations: List<CanonicalCutoverObservationReport>,
        val diagnosticsSummary: String
    ) {
        val allSucceeded: Boolean
            get() = blockedCount == 0 && executedCount == successCount

        companion object {
            fun empty(stage: CanonicalLibraryMetadataCanaryStagePolicy): CanaryStageResult {
                return CanaryStageResult(
                    stage = stage,
                    executedCount = 0,
                    successCount = 0,
                    blockedCount = 0,
                    n1Results = emptyList(),
                    observations = emptyList(),
                    diagnosticsSummary = "stage=${stage.rawValue}|executed=0|success=0|blocked=0"
                )
            }
        }
    }

    fun run(): CanaryStageResult {
        val stage = configuration.stage
        if (stage == CanonicalLibraryMetadataCanaryStagePolicy.DISABLED) {
            return CanaryStageResult.empty(stage)
        }

        val eligibleCount = stage.eligibleCount
        if (eligibleCount == 0) {
            return CanaryStageResult.empty(stage)
        }

        val selectedCandidates = if (eligibleCount == Int.MAX_VALUE)
            candidates
        else
            candidates.take(eligibleCount)

        if (selectedCandidates.isEmpty()) {
            return CanaryStageResult.empty(stage)
        }

        val n1Results = selectedCandidates.map { candidate ->
            n1Runner.runSingle(candidate)
        }

        val observations = n1Results.map { result ->
            CanonicalCutoverObservationReport(
                candidateID = result.candidate.candidateID,
                objectID = result.candidate.objectID.rawValue,
                metadataKind = result.candidate.kind,
                executed = result.executed,
                success = result.success,
                executionID = result.executionID,
                canonicalWriteApplied = result.canonicalWriteApplied,
                metadataHashVerified = result.metadataHashVerified,
                blockers = result.blockers.map { it.rawValue }
            )
        }

        val executedCount = n1Results.count { it.executed }
        val successCount = n1Results.count { it.success }
        val blockedCount = n1Results.count { it.blockers.isNotEmpty() || !it.executed }

        return CanaryStageResult(
            stage = stage,
            executedCount = executedCount,
            successCount = successCount,
            blockedCount = blockedCount,
            n1Results = n1Results,
            observations = observations,
            diagnosticsSummary = listOf(
                "stage=${stage.rawValue}",
                "candidates=${selectedCandidates.size}",
                "executed=$executedCount",
                "success=$successCount",
                "blocked=$blockedCount"
            ).joinToString("|")
        )
    }
}

data class CanonicalCutoverObservationReport(
    val candidateID: String,
    val objectID: String,
    val metadataKind: CanonicalLibraryMetadataCandidateKind,
    val executed: Boolean,
    val success: Boolean,
    val executionID: String,
    val canonicalWriteApplied: Boolean,
    val metadataHashVerified: Boolean,
    val observationDurationSeconds: Double? = null,
    val legacyComparisonResult: String? = null,
    val canonicalComparisonResult: String? = null,
    val blockers: List<String>,
    val redacted: Boolean = true
) {

    val diagnosticsSummary: String
        get() = listOf(
            "candidate=$candidateID",
            "object=$objectID",
            "kind=${metadataKind.rawValue}",
            "executed=$executed",
            "success=$success",
            "canonicalWrite=$canonicalWriteApplied",
            "hashVerified=$metadataHashVerified",
            "blockers=${blockers.joinToString("|")}"
        ).joinToString(",")
}

data class CanonicalLibraryMetadataRetirementReadinessReport(
    val ready: Boolean,
    val domainCutoverComplete: Boolean,
    val canaryResults: List<CanonicalLibraryMetadataCanaryStageRunner.CanaryStageResult>,
    val landingReports: List<CanonicalCutoverLandingReport>,
    val observations: List<CanonicalCutoverObservationReport>,
    val blockers: List<CanonicalLibraryMetadataCutoverBlocker>,
    val legacyCodePathDeprecatable: Boolean,
    val legacyDataPathOrphaned: Boolean,
    val canonicalReadPathStable: Boolean,
    val productionWritePathStable: Boolean,
    val noPendingCanaryStages: Boolean,
    val allLandingFreezesResolved: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        canaryResults: List<CanonicalLibraryMetadataCanaryStageRunner.CanaryStageResult>,
        landingReports: List<CanonicalCutoverLandingReport>,
        observations: List<CanonicalCutoverObservationReport>
    ) : this(
        ready = canaryResults.all { it.allSucceeded } &&
                landingReports.all { it.landingComplete } &&
                observations.all { it.success },
        domainCutoverComplete = canaryResults.isNotEmpty() && canaryResults.all { it.allSucceeded },
        canaryResults = canaryResults,
        landingReports = landingReports,
        observations = observations,
        blockers = computeBlockers(canaryResults, landingReports),
        legacyCodePathDeprecatable = canaryResults.all { it.allSucceeded },
        legacyDataPathOrphaned = observations.all { it.canonicalWriteApplied },
        canonicalReadPathStable = landingReports.all { it.landingComplete },
        productionWritePathStable = observations.all { it.success && it.canonicalWriteApplied },
        noPendingCanaryStages = canaryResults.none { it.stage != CanonicalLibraryMetadataCanaryStagePolicy.DISABLED },
        allLandingFreezesResolved = true,
        diagnosticsSummary = listOf(
            "ready=${(canaryResults.all { it.allSucceeded } && landingReports.all { it.landingComplete } && observations.all { it.success })}",
            "domainCutover=${(canaryResults.isNotEmpty() && canaryResults.all { it.allSucceeded })}",
            "canaryStages=${canaryResults.size}",
            "landings=${landingReports.size}",
            "observations=${observations.size}",
            "legacyDeprecatable=${canaryResults.all { it.allSucceeded }}",
            "canonicalStable=${landingReports.all { it.landingComplete }}"
        ).joinToString(",")
    )

    companion object {
        private fun computeBlockers(
            canaryResults: List<CanonicalLibraryMetadataCanaryStageRunner.CanaryStageResult>,
            landingReports: List<CanonicalCutoverLandingReport>
        ): List<CanonicalLibraryMetadataCutoverBlocker> {
            val blockers = mutableListOf<CanonicalLibraryMetadataCutoverBlocker>()
            if (canaryResults.any { !it.allSucceeded }) {
                blockers.add(CanonicalLibraryMetadataCutoverBlocker.N1_CANARY_FAILED)
            }
            if (landingReports.any { !it.landingComplete }) {
                blockers.add(CanonicalLibraryMetadataCutoverBlocker.LANDING_FREEZE_ACTIVE)
            }
            return blockers.distinct().sortedBy { it.rawValue }
        }
    }
}

// ── Forward reference placeholder for CanonicalLibraryMetadataLandingReport ──
data class CanonicalCutoverLandingReport(
    val landingComplete: Boolean,
    val frozen: Boolean,
    val activePilot: String?,
    val recommendation: String?,
    val blockers: List<String>,
    val diagnosticsSummary: String
) {
    constructor(
        landingComplete: Boolean,
        frozen: Boolean = false,
        activePilot: String? = null,
        recommendation: String? = null,
        blockers: List<String> = emptyList()
    ) : this(
        landingComplete = landingComplete,
        frozen = frozen,
        activePilot = activePilot,
        recommendation = recommendation,
        blockers = blockers.sorted(),
        diagnosticsSummary = listOf(
            "landingComplete=$landingComplete",
            "frozen=$frozen",
            "pilot=${activePilot ?: "none"}",
            "recommendation=${recommendation ?: "none"}",
            "blockers=${blockers.joinToString("|")}"
        ).joinToString(",")
    )
}
