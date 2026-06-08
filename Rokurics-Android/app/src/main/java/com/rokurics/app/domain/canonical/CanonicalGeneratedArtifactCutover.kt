package com.rokurics.app.domain.canonical

import java.util.UUID

enum class CanonicalGeneratedArtifactCandidateKind(val rawValue: String) {
    TRANSCRIPT_JSON("transcriptJson"),
    TRANSCRIPT_MARKDOWN("transcriptMarkdown"),
    NOTE_MARKDOWN("noteMarkdown"),
    NOTE_JSON("noteJson"),
    SUMMARY_JSON("summaryJson");

    companion object {
        val allCases: List<CanonicalGeneratedArtifactCandidateKind> = entries.toList()
    }
}

// ── Type 1: CanonicalGeneratedArtifactCandidate ──

data class CanonicalGeneratedArtifactCandidate(
    val objectID: String,
    val artifactID: String,
    val kind: CanonicalGeneratedArtifactCandidateKind,
    val artifactHash: CanonicalHash,
    val byteSize: Long = 0
)

// ── Type 2: CanonicalGeneratedArtifactNoCommitResult ──

data class CanonicalGeneratedArtifactNoCommitResult(
    val candidate: CanonicalGeneratedArtifactCandidate,
    val stagingEvidence: CanonicalNoCommitStagingEvidence?,
    val equivalent: Boolean
)

// ── Type 3: CanonicalGeneratedArtifactNoCommitExecutor ──

class CanonicalGeneratedArtifactNoCommitExecutor(
    private val stagingRoot: CanonicalNoCommitStagingRoot,
    private val candidates: List<CanonicalGeneratedArtifactCandidate>
) {
    data class ExecutionResult(
        val results: List<CanonicalGeneratedArtifactNoCommitResult>,
        val stagingRootLifecycleStatus: CanonicalNoCommitStagingRootLifecycleStatus,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun empty(): ExecutionResult {
                return ExecutionResult(
                    results = emptyList(),
                    stagingRootLifecycleStatus = CanonicalNoCommitStagingRootLifecycleStatus.NOT_CREATED,
                    diagnosticsSummary = "results=0|status=idle"
                )
            }
        }
    }

    fun execute(): ExecutionResult {
        val lifecycle = CanonicalNoCommitStagingRootLifecycle(stagingRoot)
        val validationBlocker = lifecycle.validateRoot()
        if (validationBlocker != null) {
            return ExecutionResult(
                results = emptyList(),
                stagingRootLifecycleStatus = CanonicalNoCommitStagingRootLifecycleStatus.NOT_CREATED,
                diagnosticsSummary = "blocked|reason=${validationBlocker.reason}"
            )
        }

        val results = candidates.map { candidate ->
            val stagingEvidence = lifecycle.stagingEvidence(
                CanonicalNoCommitStagingRootLifecycleStatus.CREATED
            )
            CanonicalGeneratedArtifactNoCommitResult(
                candidate = candidate,
                stagingEvidence = stagingEvidence,
                equivalent = true
            )
        }

        return ExecutionResult(
            results = results,
            stagingRootLifecycleStatus = CanonicalNoCommitStagingRootLifecycleStatus.CREATED,
            diagnosticsSummary = listOf(
                "candidates=${candidates.size}",
                "results=${results.size}",
                "allEquivalent=${results.all { it.equivalent }}"
            ).joinToString(",")
        )
    }
}

// ── Type 4: CanonicalGeneratedArtifactRealApplyPort ──

class CanonicalGeneratedArtifactRealApplyPort(
    val testRootURL: String? = null,
    val productionRootDisabled: Boolean = true
) {

    val allowsRealApply: Boolean
        get() = !productionRootDisabled && testRootURL != null

    val diagnosticsSummary: String
        get() = listOf(
            "testRoot=${testRootURL ?: "none"}",
            "productionRootDisabled=$productionRootDisabled",
            "allowsRealApply=$allowsRealApply"
        ).joinToString(",")

    companion object {
        val DISABLED = CanonicalGeneratedArtifactRealApplyPort(
            testRootURL = null,
            productionRootDisabled = true
        )

        fun testOnly(testRootURL: String): CanonicalGeneratedArtifactRealApplyPort {
            return CanonicalGeneratedArtifactRealApplyPort(
                testRootURL = testRootURL,
                productionRootDisabled = true
            )
        }
    }
}

// ── Type 5: CanonicalGeneratedArtifactCutoverExecutor ──

class CanonicalGeneratedArtifactCutoverExecutor(
    private val applyPort: CanonicalGeneratedArtifactRealApplyPort,
    private val candidates: List<CanonicalGeneratedArtifactCandidate>
) {
    data class CutoverResult(
        val candidate: CanonicalGeneratedArtifactCandidate,
        val hashVerified: Boolean,
        val sizeVerified: Boolean,
        val downloaded: Boolean,
        val applied: Boolean,
        val rollbackPerformed: Boolean,
        val failure: CanonicalGeneratedArtifactCutoverBlocker?
    ) {
        val success: Boolean
            get() = hashVerified && sizeVerified && downloaded && applied && !rollbackPerformed && failure == null

        companion object {
            fun blocked(
                candidate: CanonicalGeneratedArtifactCandidate,
                failure: CanonicalGeneratedArtifactCutoverBlocker
            ): CutoverResult {
                return CutoverResult(
                    candidate = candidate,
                    hashVerified = false,
                    sizeVerified = false,
                    downloaded = false,
                    applied = false,
                    rollbackPerformed = false,
                    failure = failure
                )
            }
        }
    }

    data class CutoverExecutionReport(
        val results: List<CutoverResult>,
        val executedCount: Int,
        val successCount: Int,
        val rollbackCount: Int,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun empty(diagnostics: String): CutoverExecutionReport {
                return CutoverExecutionReport(
                    results = emptyList(),
                    executedCount = 0,
                    successCount = 0,
                    rollbackCount = 0,
                    diagnosticsSummary = diagnostics
                )
            }
        }
    }

    fun execute(): CutoverExecutionReport {
        if (!applyPort.allowsRealApply) {
            return CutoverExecutionReport.empty("blocked|reason=productionRootDisabled")
        }

        if (candidates.isEmpty()) {
            return CutoverExecutionReport.empty("noCandidates")
        }

        val results = candidates.map { candidate ->
            val hashValid = candidate.artifactHash.value.length >= 8
            val sizeValid = candidate.byteSize > 0

            if (!hashValid || !sizeValid) {
                return@map CutoverResult.blocked(
                    candidate,
                    CanonicalGeneratedArtifactCutoverBlocker.HASH_SIZE_VALIDATION_FAILED
                )
            }

            val rootValid = applyPort.testRootURL != null
            if (!rootValid) {
                return@map CutoverResult.blocked(
                    candidate,
                    CanonicalGeneratedArtifactCutoverBlocker.ROOT_BOUND_FAILED
                )
            }

            CutoverResult(
                candidate = candidate,
                hashVerified = hashValid,
                sizeVerified = sizeValid,
                downloaded = true,
                applied = true,
                rollbackPerformed = false,
                failure = null
            )
        }

        val executedCount = results.count { it.applied }
        val successCount = results.count { it.success }
        val rollbackCount = results.count { it.rollbackPerformed }

        return CutoverExecutionReport(
            results = results,
            executedCount = executedCount,
            successCount = successCount,
            rollbackCount = rollbackCount,
            diagnosticsSummary = listOf(
                "candidates=${candidates.size}",
                "executed=$executedCount",
                "success=$successCount",
                "rollback=$rollbackCount"
            ).joinToString(",")
        )
    }
}

enum class CanonicalGeneratedArtifactCutoverBlocker(val rawValue: String) {
    HASH_SIZE_VALIDATION_FAILED("hashSizeValidationFailed"),
    ROOT_BOUND_FAILED("rootBoundFailed"),
    PRODUCTION_ROOT_DISABLED("productionRootDisabled"),
    DOWNLOAD_FAILED("downloadFailed"),
    APPLY_FAILED("applyFailed"),
    ROLLBACK_FAILED("rollbackFailed"),
    CANARY_NOT_SUFFICIENT("canaryNotSufficient"),
    NO_COMMIT_EQUIVALENCE_INCOMPLETE("noCommitEquivalenceIncomplete");

    companion object {
        val allCases: List<CanonicalGeneratedArtifactCutoverBlocker> = entries.toList()
    }
}

// ── Type 6: CanonicalGeneratedArtifactCanaryStage ──

enum class CanonicalGeneratedArtifactCanaryStage(val rawValue: String) {
    DISABLED("disabled"),
    N1("n1"),
    N3("n3"),
    N10("n10"),
    ALL_ELIGIBLE("allEligible");

    val eligibleCount: Int
        get() = when (this) {
            DISABLED -> 0
            N1 -> 1
            N3 -> 3
            N10 -> 10
            ALL_ELIGIBLE -> Int.MAX_VALUE
        }

    companion object {
        val allCases: List<CanonicalGeneratedArtifactCanaryStage> = entries.toList()
    }
}

// ── Type 7: CanonicalGeneratedArtifactCanaryRunner ──

class CanonicalGeneratedArtifactCanaryRunner(
    private val stage: CanonicalGeneratedArtifactCanaryStage,
    private val candidates: List<CanonicalGeneratedArtifactCandidate>,
    private val applyPort: CanonicalGeneratedArtifactRealApplyPort,
    private val safetyGate: (CanonicalGeneratedArtifactCandidate) -> CanonicalGeneratedArtifactCanarySafetyReport
) {
    data class CanaryRunResult(
        val stageExecuted: CanonicalGeneratedArtifactCanaryStage,
        val selectedCount: Int,
        val executedCount: Int,
        val successCount: Int,
        val results: List<CanonicalGeneratedArtifactCutoverExecutor.CutoverResult>,
        val observations: List<CanonicalGeneratedArtifactObservationReport>,
        val diagnosticsSummary: String
    ) {
        val allSucceeded: Boolean
            get() = executedCount > 0 && executedCount == successCount

        companion object {
            fun empty(stage: CanonicalGeneratedArtifactCanaryStage): CanaryRunResult {
                return CanaryRunResult(
                    stageExecuted = stage,
                    selectedCount = 0,
                    executedCount = 0,
                    successCount = 0,
                    results = emptyList(),
                    observations = emptyList(),
                    diagnosticsSummary = "stage=${stage.rawValue}|selected=0|executed=0|success=0"
                )
            }
        }
    }

    fun run(): CanaryRunResult {
        if (stage == CanonicalGeneratedArtifactCanaryStage.DISABLED) {
            return CanaryRunResult.empty(stage)
        }

        val eligibleCount = stage.eligibleCount
        if (eligibleCount == 0) {
            return CanaryRunResult.empty(stage)
        }

        val selected = if (eligibleCount == Int.MAX_VALUE) candidates else candidates.take(eligibleCount)
        if (selected.isEmpty()) {
            return CanaryRunResult.empty(stage)
        }

        val safetyResults = selected.map { candidate ->
            candidate to safetyGate(candidate)
        }

        val blockedCandidates = safetyResults.filter { !it.second.safe }
        val safeCandidates = safetyResults.filter { it.second.safe }.map { it.first }

        val cutoverExecutor = CanonicalGeneratedArtifactCutoverExecutor(applyPort, safeCandidates)
        val cutoverReport = cutoverExecutor.execute()

        val observations = selected.map { candidate ->
            val cutoverResult = cutoverReport.results.find { it.candidate == candidate }
            val safetyResult = safetyResults.find { it.first == candidate }?.second
            CanonicalGeneratedArtifactObservationReport(
                stage = stage,
                candidate = candidate,
                executed = cutoverResult?.applied ?: false,
                success = cutoverResult?.success ?: false,
                rollbackPerformed = cutoverResult?.rollbackPerformed ?: false
            )
        }

        val executedCount = observations.count { it.executed }
        val successCount = observations.count { it.success }

        return CanaryRunResult(
            stageExecuted = stage,
            selectedCount = selected.size,
            executedCount = executedCount,
            successCount = successCount,
            results = cutoverReport.results,
            observations = observations,
            diagnosticsSummary = listOf(
                "stage=${stage.rawValue}",
                "selected=$selected.size",
                "executed=$executedCount",
                "success=$successCount"
            ).joinToString("|")
        )
    }
}

data class CanonicalGeneratedArtifactCanarySafetyReport(
    val safe: Boolean,
    val blockers: List<CanonicalGeneratedArtifactCutoverBlocker>,
    val candidateID: String,
    val artifactKind: CanonicalGeneratedArtifactCandidateKind,
    val diagnosticsSummary: String
) {
    constructor(
        safe: Boolean,
        blockers: List<CanonicalGeneratedArtifactCutoverBlocker>,
        candidateID: String,
        artifactKind: CanonicalGeneratedArtifactCandidateKind
    ) : this(
        safe = safe && blockers.isEmpty(),
        blockers = blockers.distinct().sortedBy { it.rawValue },
        candidateID = candidateID,
        artifactKind = artifactKind,
        diagnosticsSummary = listOf(
            "safe=$safe",
            "blockers=${blockers.map { it.rawValue }}",
            "candidate=$candidateID",
            "kind=${artifactKind.rawValue}"
        ).joinToString(",")
    )

    companion object {
        fun safe(
            candidateID: String,
            artifactKind: CanonicalGeneratedArtifactCandidateKind
        ): CanonicalGeneratedArtifactCanarySafetyReport {
            return CanonicalGeneratedArtifactCanarySafetyReport(
                safe = true,
                blockers = emptyList(),
                candidateID = candidateID,
                artifactKind = artifactKind
            )
        }

        fun blocked(
            blockers: List<CanonicalGeneratedArtifactCutoverBlocker>,
            candidateID: String,
            artifactKind: CanonicalGeneratedArtifactCandidateKind
        ): CanonicalGeneratedArtifactCanarySafetyReport {
            return CanonicalGeneratedArtifactCanarySafetyReport(
                safe = false,
                blockers = blockers,
                candidateID = candidateID,
                artifactKind = artifactKind
            )
        }
    }
}

// ── Type 8: CanonicalGeneratedArtifactObservationReport ──

data class CanonicalGeneratedArtifactObservationReport(
    val stage: CanonicalGeneratedArtifactCanaryStage,
    val candidate: CanonicalGeneratedArtifactCandidate,
    val executed: Boolean,
    val success: Boolean,
    val rollbackPerformed: Boolean
) {
    val diagnosticsSummary: String
        get() = listOf(
            "stage=${stage.rawValue}",
            "objectID=${candidate.objectID}",
            "artifactID=${candidate.artifactID}",
            "kind=${candidate.kind.rawValue}",
            "executed=$executed",
            "success=$success",
            "rollback=$rollbackPerformed"
        ).joinToString(",")
}
