package com.rokurics.app.domain.canonical

import java.util.UUID

enum class CanonicalTombstoneConflictCandidateKind(val rawValue: String) {
    SOFT_TOMBSTONE_MARKER("softTombstoneMarker"),
    CONFLICT_RECORD("conflictRecord"),
    RESURRECTION_BLOCK("resurrectionBlock"),
    GENERATED_ARTIFACT_TOMBSTONE("generatedArtifactTombstone");

    companion object {
        val allCases: List<CanonicalTombstoneConflictCandidateKind> = entries.toList()
    }
}

data class CanonicalTombstoneConflictCandidate
private constructor(
    val objectID: String,
    val kind: CanonicalTombstoneConflictCandidateKind,
    val candidateID: String,
    val tombstoneActive: Boolean,
    val conflictResolved: Boolean,
    val resurrectionBlocked: Boolean,
    val artifactTombstoneValid: Boolean,
    val diagnosticsSummary: String
) {
    val isSoftTombstone: Boolean
        get() = kind == CanonicalTombstoneConflictCandidateKind.SOFT_TOMBSTONE_MARKER

    val isConflictRecord: Boolean
        get() = kind == CanonicalTombstoneConflictCandidateKind.CONFLICT_RECORD

    val isResurrectionBlock: Boolean
        get() = kind == CanonicalTombstoneConflictCandidateKind.RESURRECTION_BLOCK

    companion object {
        operator fun invoke(
            objectID: String,
            kind: CanonicalTombstoneConflictCandidateKind,
            candidateID: String = UUID.randomUUID().toString(),
            tombstoneActive: Boolean = true,
            conflictResolved: Boolean = false,
            resurrectionBlocked: Boolean = true,
            artifactTombstoneValid: Boolean = true
        ): CanonicalTombstoneConflictCandidate {
            return CanonicalTombstoneConflictCandidate(
                objectID = CanonicalProductionRedaction.safeIdentifier(objectID, "tombstone-candidate"),
                kind = kind,
                candidateID = candidateID,
                tombstoneActive = tombstoneActive,
                conflictResolved = conflictResolved,
                resurrectionBlocked = resurrectionBlocked,
                artifactTombstoneValid = artifactTombstoneValid,
                diagnosticsSummary = listOf(
                    "object=$objectID",
                    "kind=${kind.rawValue}",
                    "tombstoneActive=$tombstoneActive",
                    "conflictResolved=$conflictResolved",
                    "resurrectionBlocked=$resurrectionBlocked",
                    "artifactValid=$artifactTombstoneValid"
                ).joinToString(",")
            )
        }
    }
}

data class CanonicalTombstoneConflictNoCommitResult
private constructor(
    val candidate: CanonicalTombstoneConflictCandidate,
    val stagingEvidence: CanonicalTombstoneConflictStagingEvidence?,
    val equivalent: Boolean,
    val executionID: String,
    val blockers: List<CanonicalTombstoneConflictBlocker>,
    val diagnosticsSummary: String
) {
    val success: Boolean
        get() = equivalent && blockers.isEmpty() && stagingEvidence != null

    companion object {
        operator fun invoke(
            candidate: CanonicalTombstoneConflictCandidate,
            stagingEvidence: CanonicalTombstoneConflictStagingEvidence? = null,
            equivalent: Boolean = false,
            executionID: String = UUID.randomUUID().toString(),
            blockers: List<CanonicalTombstoneConflictBlocker> = emptyList()
        ): CanonicalTombstoneConflictNoCommitResult {
            return CanonicalTombstoneConflictNoCommitResult(
                candidate = candidate,
                stagingEvidence = stagingEvidence,
                equivalent = equivalent,
                executionID = executionID,
                blockers = blockers.distinct().sortedBy { it.rawValue },
                diagnosticsSummary = listOf(
                    "candidate=${candidate.candidateID}",
                    "object=${candidate.objectID}",
                    "kind=${candidate.kind.rawValue}",
                    "equivalent=$equivalent",
                    "blockers=${blockers.map { it.rawValue }}",
                    "staging=${if (stagingEvidence != null) "present" else "absent"}"
                ).joinToString(",")
            )
        }
    }
}

enum class CanonicalTombstoneConflictBlocker(val rawValue: String) {
    PHYSICAL_DELETE_FORBIDDEN("physicalDeleteForbidden"),
    PERMANENT_DELETE_FORBIDDEN("permanentDeleteForbidden"),
    TOMBSTONE_GC_FORBIDDEN("tombstoneGCForbidden"),
    RESTORE_FORBIDDEN("restoreForbidden"),
    RESURRECTION_DETECTED("resurrectionDetected"),
    STALE_LIVE_METADATA_REFERENCING_TOMBSTONE("staleLiveMetadataReferencingTombstone"),
    CONFLICT_UNRESOLVED("conflictUnresolved"),
    ARTIFACT_TOMBSTONE_INVALID("artifactTombstoneInvalid"),
    STAGING_ROOT_BLOCKED("stagingRootBlocked"),
    TEST_ROOT_ONLY("testRootOnly"),
    DOMAIN_NOT_ALLOWED("domainNotAllowed"),
    N1_CANARY_FAILED("n1CanaryFailed"),
    ROLLBACK_FAILED("rollbackFailed");

    companion object {
        val allCases: List<CanonicalTombstoneConflictBlocker> = entries.toList()
    }
}

data class CanonicalTombstoneConflictStagingEvidence
private constructor(
    val rootID: String,
    val candidateID: String,
    val tombstoneWritten: Boolean,
    val conflictWritten: Boolean,
    val resurrectionBlockWritten: Boolean,
    val fileCount: Int,
    val byteCount: Long,
    val productionRootUntouched: Boolean,
    val diagnosticsSummary: String
) {
    companion object {
        operator fun invoke(
            rootID: String = UUID.randomUUID().toString(),
            candidateID: String,
            tombstoneWritten: Boolean = false,
            conflictWritten: Boolean = false,
            resurrectionBlockWritten: Boolean = false,
            fileCount: Int = 0,
            byteCount: Long = 0,
            productionRootUntouched: Boolean = true
        ): CanonicalTombstoneConflictStagingEvidence {
            return CanonicalTombstoneConflictStagingEvidence(
                rootID = CanonicalProductionRedaction.safeIdentifier(rootID, "tombstone-staging"),
                candidateID = candidateID,
                tombstoneWritten = tombstoneWritten,
                conflictWritten = conflictWritten,
                resurrectionBlockWritten = resurrectionBlockWritten,
                fileCount = maxOf(0, fileCount),
                byteCount = maxOf(0, byteCount),
                productionRootUntouched = productionRootUntouched,
                diagnosticsSummary = listOf(
                    "candidate=$candidateID",
                    "tombstone=$tombstoneWritten",
                    "conflict=$conflictWritten",
                    "resurrectionBlock=$resurrectionBlockWritten",
                    "files=$fileCount",
                    "bytes=$byteCount",
                    "productionUntouched=$productionRootUntouched"
                ).joinToString(",")
            )
        }
    }
}

class CanonicalTombstoneConflictNoCommitExecutor(
    private val candidate: CanonicalTombstoneConflictCandidate,
    private val stagingRoot: CanonicalNoCommitStagingRoot,
    private val lifecycle: CanonicalNoCommitStagingRootLifecycle
) {
    fun execute(): CanonicalTombstoneConflictNoCommitResult {
        val executionID = UUID.randomUUID().toString()

        val rootBlocker = lifecycle.validateRoot()
        if (rootBlocker != null) {
            return CanonicalTombstoneConflictNoCommitResult(
                candidate = candidate,
                equivalent = false,
                executionID = executionID,
                blockers = listOf(
                    CanonicalTombstoneConflictBlocker.STAGING_ROOT_BLOCKED
                )
            )
        }

        if (!candidate.tombstoneActive) {
            return CanonicalTombstoneConflictNoCommitResult(
                candidate = candidate,
                equivalent = false,
                executionID = executionID,
                blockers = listOf(
                    CanonicalTombstoneConflictBlocker.TOMBSTONE_GC_FORBIDDEN
                )
            )
        }

        if (!candidate.resurrectionBlocked) {
            return CanonicalTombstoneConflictNoCommitResult(
                candidate = candidate,
                equivalent = false,
                executionID = executionID,
                blockers = listOf(
                    CanonicalTombstoneConflictBlocker.RESURRECTION_DETECTED
                )
            )
        }

        if (candidate.isConflictRecord && !candidate.conflictResolved) {
            return CanonicalTombstoneConflictNoCommitResult(
                candidate = candidate,
                equivalent = false,
                executionID = executionID,
                blockers = listOf(
                    CanonicalTombstoneConflictBlocker.CONFLICT_UNRESOLVED
                )
            )
        }

        if (candidate.kind == CanonicalTombstoneConflictCandidateKind.GENERATED_ARTIFACT_TOMBSTONE &&
            !candidate.artifactTombstoneValid
        ) {
            return CanonicalTombstoneConflictNoCommitResult(
                candidate = candidate,
                equivalent = false,
                executionID = executionID,
                blockers = listOf(
                    CanonicalTombstoneConflictBlocker.ARTIFACT_TOMBSTONE_INVALID
                )
            )
        }

        val stagingEvidence = lifecycle.stagingEvidence(
            CanonicalNoCommitStagingRootLifecycleStatus.CREATED
        )

        val evidence = CanonicalTombstoneConflictStagingEvidence(
            candidateID = candidate.candidateID,
            tombstoneWritten = true,
            conflictWritten = candidate.isConflictRecord,
            resurrectionBlockWritten = candidate.isResurrectionBlock,
            fileCount = stagingEvidence.fileCount,
            byteCount = stagingEvidence.byteCount,
            productionRootUntouched = stagingEvidence.wroteOnlyStagingRoot
        )

        return CanonicalTombstoneConflictNoCommitResult(
            candidate = candidate,
            stagingEvidence = evidence,
            equivalent = true,
            executionID = executionID
        )
    }
}

class CanonicalTombstoneConflictRealApplyPort(
    private val applyPort: CanonicalProductionApplyPort,
    private val testRootURL: String? = null
) {
    val isTestMode: Boolean
        get() = testRootURL != null

    val rootURL: String
        get() = applyPort.rootURL()

    val isProductionRoot: Boolean
        get() = applyPort.isProductionRoot()

    fun canCommit(): Boolean {
        if (isProductionRoot) return false
        if (testRootURL != null && rootURL.startsWith(testRootURL)) return true
        return !isProductionRoot
    }

    fun validateTombstoneCandidate(
        candidate: CanonicalTombstoneConflictCandidate
    ): List<CanonicalTombstoneConflictBlocker> {
        val blockers = mutableListOf<CanonicalTombstoneConflictBlocker>()

        if (isProductionRoot) {
            blockers.add(CanonicalTombstoneConflictBlocker.TEST_ROOT_ONLY)
        }

        if (!canCommit()) {
            blockers.add(CanonicalTombstoneConflictBlocker.DOMAIN_NOT_ALLOWED)
        }

        if (candidate.kind == CanonicalTombstoneConflictCandidateKind.RESURRECTION_BLOCK &&
            !candidate.resurrectionBlocked
        ) {
            blockers.add(CanonicalTombstoneConflictBlocker.STALE_LIVE_METADATA_REFERENCING_TOMBSTONE)
        }

        return blockers
    }

    companion object {
        fun testMode(testRootURL: String): CanonicalTombstoneConflictRealApplyPort {
            val fakePort = CanonicalProductionPortFactory.fakeApplyPort(
                rootURL = testRootURL,
                production = false
            )
            return CanonicalTombstoneConflictRealApplyPort(
                applyPort = fakePort,
                testRootURL = testRootURL
            )
        }
    }
}

class CanonicalTombstoneConflictCutoverExecutor(
    private val applyPort: CanonicalTombstoneConflictRealApplyPort,
    private val candidates: List<CanonicalTombstoneConflictCandidate>
) {
    data class CutoverResult(
        val executed: Boolean,
        val candidateCount: Int,
        val successCount: Int,
        val blockedCount: Int,
        val results: List<CanonicalTombstoneConflictNoCommitResult>,
        val tombstonePreserved: Boolean,
        val noPhysicalDelete: Boolean,
        val noPermanentDelete: Boolean,
        val noTombstoneGC: Boolean,
        val noRestore: Boolean,
        val antiResurrection: Boolean,
        val diagnosticsSummary: String
    ) {
        val allConstraintsEnforced: Boolean
            get() = tombstonePreserved && noPhysicalDelete && noPermanentDelete &&
                    noTombstoneGC && noRestore && antiResurrection
    }

    fun execute(): CutoverResult {
        if (candidates.isEmpty()) {
            return CutoverResult(
                executed = false,
                candidateCount = 0,
                successCount = 0,
                blockedCount = 0,
                results = emptyList(),
                tombstonePreserved = true,
                noPhysicalDelete = true,
                noPermanentDelete = true,
                noTombstoneGC = true,
                noRestore = true,
                antiResurrection = true,
                diagnosticsSummary = "noCandidates|executed=false"
            )
        }

        val results = mutableListOf<CanonicalTombstoneConflictNoCommitResult>()

        for (candidate in candidates) {
            val blockers = applyPort.validateTombstoneCandidate(candidate)

            if (blockers.isNotEmpty()) {
                results.add(
                    CanonicalTombstoneConflictNoCommitResult(
                        candidate = candidate,
                        equivalent = false,
                        blockers = blockers
                    )
                )
                continue
            }

            val stagingRoot = CanonicalNoCommitStagingRoot(
                rootKind = CanonicalNoCommitStagingRootKind.SYSTEM_TEMPORARY,
                rootURL = applyPort.rootURL + "/tombstone-staging-" + UUID.randomUUID().toString().take(8)
            )

            val executor = CanonicalTombstoneConflictNoCommitExecutor(
                candidate = candidate,
                stagingRoot = stagingRoot,
                lifecycle = CanonicalNoCommitStagingRootLifecycle(stagingRoot)
            )

            results.add(executor.execute())
        }

        val successCount = results.count { it.success }
        val blockedCount = results.count { !it.equivalent || it.blockers.isNotEmpty() }

        val tombstoneCandidates = candidates.filter { it.isSoftTombstone || it.isResurrectionBlock }
        val tombstonePreserved = tombstoneCandidates.all { it.tombstoneActive && it.resurrectionBlocked }

        val antiResurrection = candidates.none {
            it.kind == CanonicalTombstoneConflictCandidateKind.RESURRECTION_BLOCK && !it.resurrectionBlocked
        }

        val noPhysicalDelete = results.none {
            it.blockers.any { b -> b == CanonicalTombstoneConflictBlocker.PHYSICAL_DELETE_FORBIDDEN }
        } && successCount > 0

        val noPermanentDelete = results.none {
            it.blockers.any { b -> b == CanonicalTombstoneConflictBlocker.PERMANENT_DELETE_FORBIDDEN }
        }

        val noTombstoneGC = results.none {
            it.blockers.any { b -> b == CanonicalTombstoneConflictBlocker.TOMBSTONE_GC_FORBIDDEN }
        }

        val noRestore = results.none {
            it.blockers.any { b -> b == CanonicalTombstoneConflictBlocker.RESTORE_FORBIDDEN }
        }

        return CutoverResult(
            executed = true,
            candidateCount = candidates.size,
            successCount = successCount,
            blockedCount = blockedCount,
            results = results,
            tombstonePreserved = tombstonePreserved,
            noPhysicalDelete = noPhysicalDelete,
            noPermanentDelete = noPermanentDelete,
            noTombstoneGC = noTombstoneGC,
            noRestore = noRestore,
            antiResurrection = antiResurrection,
            diagnosticsSummary = listOf(
                "candidates=${candidates.size}",
                "success=$successCount",
                "blocked=$blockedCount",
                "tombstonePreserved=$tombstonePreserved",
                "noPhysicalDelete=$noPhysicalDelete",
                "noPermanentDelete=$noPermanentDelete",
                "noTombstoneGC=$noTombstoneGC",
                "noRestore=$noRestore",
                "antiResurrection=$antiResurrection"
            ).joinToString(",")
        )
    }
}

class CanonicalTombstoneConflictN1CanaryRunner(
    private val applyPort: CanonicalTombstoneConflictRealApplyPort,
    private val executor: CanonicalTombstoneConflictCutoverExecutor,
    private val candidate: CanonicalTombstoneConflictCandidate
) {
    data class N1CanaryResult(
        val candidate: CanonicalTombstoneConflictCandidate,
        val executed: Boolean,
        val executionID: String,
        val rollbackApplied: Boolean,
        val rollbackClean: Boolean,
        val stagingCleaned: Boolean,
        val cutoverResult: CanonicalTombstoneConflictCutoverExecutor.CutoverResult?,
        val blockers: List<CanonicalTombstoneConflictBlocker>,
        val diagnosticsSummary: String
    ) {
        val success: Boolean
            get() = executed && rollbackApplied && rollbackClean && stagingCleaned &&
                    blockers.isEmpty() && cutoverResult?.allConstraintsEnforced == true

        companion object {
            fun blocked(
                candidate: CanonicalTombstoneConflictCandidate,
                blockers: List<CanonicalTombstoneConflictBlocker>
            ): N1CanaryResult {
                return N1CanaryResult(
                    candidate = candidate,
                    executed = false,
                    executionID = UUID.randomUUID().toString(),
                    rollbackApplied = false,
                    rollbackClean = false,
                    stagingCleaned = false,
                    cutoverResult = null,
                    blockers = blockers,
                    diagnosticsSummary = listOf(
                        "executed=false",
                        "blockers=${blockers.map { it.rawValue }}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun runSingle(): N1CanaryResult {
        val executionID = UUID.randomUUID().toString()

        if (!applyPort.canCommit()) {
            return N1CanaryResult.blocked(
                candidate = candidate,
                blockers = listOf(CanonicalTombstoneConflictBlocker.TEST_ROOT_ONLY)
            )
        }

        val cutoverResult = executor.execute()
        val successResult = cutoverResult.results.firstOrNull() ?: return N1CanaryResult.blocked(
            candidate = candidate,
            blockers = listOf(CanonicalTombstoneConflictBlocker.N1_CANARY_FAILED)
        )

        if (!successResult.success) {
            return N1CanaryResult.blocked(
                candidate = candidate,
                blockers = successResult.blockers
            )
        }

        val canRollback = applyPort.isTestMode
        val rollbackResult = if (canRollback) {
            val stagingRoot = CanonicalNoCommitStagingRoot(
                rootKind = CanonicalNoCommitStagingRootKind.SYSTEM_TEMPORARY,
                rootURL = applyPort.rootURL + "/tombstone-staging-" + UUID.randomUUID().toString().take(8)
            )
            val lifecycle = CanonicalNoCommitStagingRootLifecycle(stagingRoot)
            val cleanupResult = lifecycle.cleanup(
                CanonicalNoCommitStagingRootCleanupPolicy.CleanupImmediately
            )
            cleanupResult.status == CanonicalNoCommitStagingRootCleanupStatus.REMOVED
        } else {
            false
        }

        if (!rollbackResult) {
            return N1CanaryResult(
                candidate = candidate,
                executed = true,
                executionID = executionID,
                rollbackApplied = false,
                rollbackClean = false,
                stagingCleaned = false,
                cutoverResult = cutoverResult,
                blockers = listOf(CanonicalTombstoneConflictBlocker.ROLLBACK_FAILED),
                diagnosticsSummary = listOf(
                    "executed=true",
                    "rollbackFailed=true",
                    "executionID=$executionID"
                ).joinToString(",")
            )
        }

        return N1CanaryResult(
            candidate = candidate,
            executed = true,
            executionID = executionID,
            rollbackApplied = true,
            rollbackClean = true,
            stagingCleaned = true,
            cutoverResult = cutoverResult,
            blockers = emptyList(),
            diagnosticsSummary = listOf(
                "executed=true",
                "rollbackApplied=true",
                "rollbackClean=true",
                "stagingCleaned=true",
                "executionID=$executionID"
            ).joinToString(",")
        )
    }
}

data class CanonicalTombstoneConflictObservationReport
private constructor(
    val candidateID: String,
    val objectID: String,
    val kind: CanonicalTombstoneConflictCandidateKind,
    val executed: Boolean,
    val success: Boolean,
    val executionID: String,
    val tombstonePreserved: Boolean,
    val conflictResolved: Boolean,
    val resurrectionBlocked: Boolean,
    val physicalDeleteDetected: Boolean,
    val permanentDeleteDetected: Boolean,
    val tombstoneGCDetected: Boolean,
    val restoreAttempted: Boolean,
    val resurrectionAttempted: Boolean,
    val observationDurationSeconds: Double?,
    val blockers: List<String>,
    val diagnosticsSummary: String
) {
    constructor(
        candidateID: String,
        objectID: String,
        kind: CanonicalTombstoneConflictCandidateKind,
        executed: Boolean,
        success: Boolean,
        executionID: String,
        tombstonePreserved: Boolean,
        conflictResolved: Boolean,
        resurrectionBlocked: Boolean,
        physicalDeleteDetected: Boolean = false,
        permanentDeleteDetected: Boolean = false,
        tombstoneGCDetected: Boolean = false,
        restoreAttempted: Boolean = false,
        resurrectionAttempted: Boolean = false,
        observationDurationSeconds: Double? = null,
        blockers: List<String> = emptyList()
    ) : this(
        candidateID = candidateID,
        objectID = objectID,
        kind = kind,
        executed = executed,
        success = success,
        executionID = executionID,
        tombstonePreserved = tombstonePreserved,
        conflictResolved = conflictResolved,
        resurrectionBlocked = resurrectionBlocked,
        physicalDeleteDetected = physicalDeleteDetected,
        permanentDeleteDetected = permanentDeleteDetected,
        tombstoneGCDetected = tombstoneGCDetected,
        restoreAttempted = restoreAttempted,
        resurrectionAttempted = resurrectionAttempted,
        observationDurationSeconds = observationDurationSeconds,
        blockers = blockers.sorted(),
        diagnosticsSummary = listOf(
            "candidate=$candidateID",
            "object=$objectID",
            "kind=${kind.rawValue}",
            "executed=$executed",
            "success=$success",
            "tombstonePreserved=$tombstonePreserved",
            "conflictResolved=$conflictResolved",
            "resurrectionBlocked=$resurrectionBlocked",
            "physicalDelete=$physicalDeleteDetected",
            "permanentDelete=$permanentDeleteDetected",
            "tombstoneGC=$tombstoneGCDetected",
            "restore=$restoreAttempted",
            "resurrection=$resurrectionAttempted",
            "blockers=${blockers.joinToString("|")}"
        ).joinToString(",")
    )

    companion object {
        fun fromN1CanaryResult(
            result: CanonicalTombstoneConflictN1CanaryRunner.N1CanaryResult,
            observationDurationSeconds: Double? = null
        ): CanonicalTombstoneConflictObservationReport {
            return CanonicalTombstoneConflictObservationReport(
                candidateID = result.candidate.candidateID,
                objectID = result.candidate.objectID,
                kind = result.candidate.kind,
                executed = result.executed,
                success = result.success,
                executionID = result.executionID,
                tombstonePreserved = result.cutoverResult?.tombstonePreserved ?: false,
                conflictResolved = result.candidate.conflictResolved,
                resurrectionBlocked = result.candidate.resurrectionBlocked,
                physicalDeleteDetected = result.blockers.any {
                    it == CanonicalTombstoneConflictBlocker.PHYSICAL_DELETE_FORBIDDEN
                },
                permanentDeleteDetected = result.blockers.any {
                    it == CanonicalTombstoneConflictBlocker.PERMANENT_DELETE_FORBIDDEN
                },
                tombstoneGCDetected = result.blockers.any {
                    it == CanonicalTombstoneConflictBlocker.TOMBSTONE_GC_FORBIDDEN
                },
                restoreAttempted = result.blockers.any {
                    it == CanonicalTombstoneConflictBlocker.RESTORE_FORBIDDEN
                },
                resurrectionAttempted = result.blockers.any {
                    it == CanonicalTombstoneConflictBlocker.RESURRECTION_DETECTED
                },
                observationDurationSeconds = observationDurationSeconds,
                blockers = result.blockers.map { it.rawValue }
            )
        }
    }
}
