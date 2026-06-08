package com.rokurics.app.domain.canonical

import java.util.UUID

class CanonicalTombstoneConflictGuardedCommitSeam(
    private val candidates: List<CanonicalTombstoneConflictCandidate>,
    private val observationReports: List<CanonicalTombstoneConflictObservationReport>,
    private val antiResurrectionGate: CanonicalTombstoneConflictAntiResurrectionGate
) {
    data class GuardedCommitEvaluation(
        val allowed: Boolean,
        val gate: CanonicalTombstoneConflictGuardedCommitGate,
        val evidence: CanonicalTombstoneConflictGuardedCommitEvidence,
        val readiness: CanonicalTombstoneConflictGuardedCommitN1Readiness,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun blocked(
                gate: CanonicalTombstoneConflictGuardedCommitGate,
                evidence: CanonicalTombstoneConflictGuardedCommitEvidence
            ): GuardedCommitEvaluation {
                return GuardedCommitEvaluation(
                    allowed = false,
                    gate = gate,
                    evidence = evidence,
                    readiness = CanonicalTombstoneConflictGuardedCommitN1Readiness.notReady(
                        reason = "gateBlocked",
                        gate = gate
                    ),
                    diagnosticsSummary = listOf(
                        "allowed=false",
                        "gate=${gate.allowed}",
                        "blockers=${gate.blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun evaluate(): GuardedCommitEvaluation {
        val evidence = CanonicalTombstoneConflictGuardedCommitEvidence(
            candidates = candidates,
            observationReports = observationReports
        )

        val gate = CanonicalTombstoneConflictGuardedCommitGate.evaluate(
            candidates = candidates,
            observationReports = observationReports,
            antiResurrectionGate = antiResurrectionGate
        )

        if (!gate.allowed) {
            return GuardedCommitEvaluation.blocked(gate, evidence)
        }

        val readiness = CanonicalTombstoneConflictGuardedCommitN1Readiness.evaluate(
            gate = gate,
            evidence = evidence
        )

        return GuardedCommitEvaluation(
            allowed = readiness.ready,
            gate = gate,
            evidence = evidence,
            readiness = readiness,
            diagnosticsSummary = listOf(
                "allowed=${readiness.ready}",
                "gate=${gate.allowed}",
                "ready=${readiness.ready}",
                "noExecution=true"
            ).joinToString(",")
        )
    }
}

data class CanonicalTombstoneConflictGuardedCommitGate
private constructor(
    val allowed: Boolean,
    val blockers: List<CanonicalTombstoneConflictBlocker>,
    val gateID: String,
    val candidateCount: Int,
    val observationCount: Int,
    val tombstonePreservationConfirmed: Boolean,
    val antiResurrectionConfirmed: Boolean,
    val noPhysicalDeleteConfirmed: Boolean,
    val noPermanentDeleteConfirmed: Boolean,
    val noTombstoneGCConfirmed: Boolean,
    val noRestoreConfirmed: Boolean,
    val allCandidatesValid: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        allowed: Boolean,
        blockers: List<CanonicalTombstoneConflictBlocker> = emptyList(),
        gateID: String = UUID.randomUUID().toString(),
        candidateCount: Int = 0,
        observationCount: Int = 0,
        tombstonePreservationConfirmed: Boolean = false,
        antiResurrectionConfirmed: Boolean = false,
        noPhysicalDeleteConfirmed: Boolean = false,
        noPermanentDeleteConfirmed: Boolean = false,
        noTombstoneGCConfirmed: Boolean = false,
        noRestoreConfirmed: Boolean = false,
        allCandidatesValid: Boolean = false
    ) : this(
        allowed = allowed && blockers.isEmpty(),
        blockers = blockers.distinct().sortedBy { it.rawValue },
        gateID = gateID,
        candidateCount = candidateCount,
        observationCount = observationCount,
        tombstonePreservationConfirmed = tombstonePreservationConfirmed,
        antiResurrectionConfirmed = antiResurrectionConfirmed,
        noPhysicalDeleteConfirmed = noPhysicalDeleteConfirmed,
        noPermanentDeleteConfirmed = noPermanentDeleteConfirmed,
        noTombstoneGCConfirmed = noTombstoneGCConfirmed,
        noRestoreConfirmed = noRestoreConfirmed,
        allCandidatesValid = allCandidatesValid,
        diagnosticsSummary = listOf(
            "allowed=$allowed",
            "candidates=$candidateCount",
            "observations=$observationCount",
            "tombstonePreservation=$tombstonePreservationConfirmed",
            "antiResurrection=$antiResurrectionConfirmed",
            "noPhysicalDelete=$noPhysicalDeleteConfirmed",
            "noPermanentDelete=$noPermanentDeleteConfirmed",
            "noTombstoneGC=$noTombstoneGCConfirmed",
            "noRestore=$noRestoreConfirmed",
            "allCandidatesValid=$allCandidatesValid",
            "blockers=${blockers.map { it.rawValue }.joinToString("|")}"
        ).joinToString(",")
    )

    companion object {
        fun evaluate(
            candidates: List<CanonicalTombstoneConflictCandidate>,
            observationReports: List<CanonicalTombstoneConflictObservationReport>,
            antiResurrectionGate: CanonicalTombstoneConflictAntiResurrectionGate
        ): CanonicalTombstoneConflictGuardedCommitGate {
            val blockers = mutableListOf<CanonicalTombstoneConflictBlocker>()

            if (candidates.isEmpty()) {
                blockers.add(CanonicalTombstoneConflictBlocker.DOMAIN_NOT_ALLOWED)
            }

            val allTombstonesPreserved = candidates.none {
                it.isSoftTombstone && !it.tombstoneActive
            }

            if (!allTombstonesPreserved) {
                blockers.add(CanonicalTombstoneConflictBlocker.TOMBSTONE_GC_FORBIDDEN)
            }

            val antiResurrectionOk = candidates.none {
                it.kind == CanonicalTombstoneConflictCandidateKind.RESURRECTION_BLOCK &&
                        !it.resurrectionBlocked
            }

            if (!antiResurrectionOk) {
                blockers.add(CanonicalTombstoneConflictBlocker.RESURRECTION_DETECTED)
            }

            val noPhysicalDelete = observationReports.none {
                it.physicalDeleteDetected
            }

            if (!noPhysicalDelete) {
                blockers.add(CanonicalTombstoneConflictBlocker.PHYSICAL_DELETE_FORBIDDEN)
            }

            val noPermanentDelete = observationReports.none {
                it.permanentDeleteDetected
            }

            if (!noPermanentDelete) {
                blockers.add(CanonicalTombstoneConflictBlocker.PERMANENT_DELETE_FORBIDDEN)
            }

            val noTombstoneGC = observationReports.none {
                it.tombstoneGCDetected
            }

            if (!noTombstoneGC) {
                blockers.add(CanonicalTombstoneConflictBlocker.TOMBSTONE_GC_FORBIDDEN)
            }

            val noRestore = observationReports.none {
                it.restoreAttempted
            }

            if (!noRestore) {
                blockers.add(CanonicalTombstoneConflictBlocker.RESTORE_FORBIDDEN)
            }

            val noResurrection = observationReports.none {
                it.resurrectionAttempted
            }

            if (!noResurrection) {
                blockers.add(CanonicalTombstoneConflictBlocker.RESURRECTION_DETECTED)
            }

            val allCandidatesValid = candidates.all {
                it.tombstoneActive && it.resurrectionBlocked
            }

            if (!allCandidatesValid) {
                blockers.add(CanonicalTombstoneConflictBlocker.CONFLICT_UNRESOLVED)
            }

            return CanonicalTombstoneConflictGuardedCommitGate(
                allowed = blockers.isEmpty(),
                blockers = blockers,
                candidateCount = candidates.size,
                observationCount = observationReports.size,
                tombstonePreservationConfirmed = allTombstonesPreserved,
                antiResurrectionConfirmed = antiResurrectionOk,
                noPhysicalDeleteConfirmed = noPhysicalDelete,
                noPermanentDeleteConfirmed = noPermanentDelete,
                noTombstoneGCConfirmed = noTombstoneGC,
                noRestoreConfirmed = noRestore,
                allCandidatesValid = allCandidatesValid
            )
        }
    }
}

data class CanonicalTombstoneConflictGuardedCommitEvidence
private constructor(
    val candidateCount: Int,
    val softTombstoneCount: Int,
    val conflictRecordCount: Int,
    val resurrectionBlockCount: Int,
    val generatedArtifactCount: Int,
    val observationReportCount: Int,
    val successfulObservationCount: Int,
    val failedObservationCount: Int,
    val tombstonePreservationRate: Double,
    val antiResurrectionRate: Double,
    val physicalDeleteEvents: Int,
    val permanentDeleteEvents: Int,
    val tombstoneGCEvents: Int,
    val restoreEvents: Int,
    val resurrectionEvents: Int,
    val diagnosticsSummary: String
) {
    val overallSafetyScore: Double
        get() {
            val factors = listOf(
                tombstonePreservationRate,
                antiResurrectionRate,
                if (physicalDeleteEvents == 0) 1.0 else 0.0,
                if (permanentDeleteEvents == 0) 1.0 else 0.0,
                if (tombstoneGCEvents == 0) 1.0 else 0.0,
                if (restoreEvents == 0) 1.0 else 0.0
            )
            return if (factors.isEmpty()) 0.0 else factors.sum() / factors.size
        }

    constructor(
        candidates: List<CanonicalTombstoneConflictCandidate>,
        observationReports: List<CanonicalTombstoneConflictObservationReport>
    ) : this(
        candidateCount = candidates.size,
        softTombstoneCount = candidates.count {
            it.kind == CanonicalTombstoneConflictCandidateKind.SOFT_TOMBSTONE_MARKER
        },
        conflictRecordCount = candidates.count {
            it.kind == CanonicalTombstoneConflictCandidateKind.CONFLICT_RECORD
        },
        resurrectionBlockCount = candidates.count {
            it.kind == CanonicalTombstoneConflictCandidateKind.RESURRECTION_BLOCK
        },
        generatedArtifactCount = candidates.count {
            it.kind == CanonicalTombstoneConflictCandidateKind.GENERATED_ARTIFACT_TOMBSTONE
        },
        observationReportCount = observationReports.size,
        successfulObservationCount = observationReports.count { it.success },
        failedObservationCount = observationReports.count { !it.success },
        tombstonePreservationRate = if (candidates.isEmpty()) 0.0 else
            candidates.count { it.tombstoneActive }.toDouble() / candidates.size,
        antiResurrectionRate = if (candidates.isEmpty()) 0.0 else
            candidates.count { it.resurrectionBlocked }.toDouble() / candidates.size,
        physicalDeleteEvents = observationReports.count { it.physicalDeleteDetected },
        permanentDeleteEvents = observationReports.count { it.permanentDeleteDetected },
        tombstoneGCEvents = observationReports.count { it.tombstoneGCDetected },
        restoreEvents = observationReports.count { it.restoreAttempted },
        resurrectionEvents = observationReports.count { it.resurrectionAttempted },
        diagnosticsSummary = listOf(
            "candidates=${candidates.size}",
            "softTombstones=${candidates.count { it.kind == CanonicalTombstoneConflictCandidateKind.SOFT_TOMBSTONE_MARKER }}",
            "conflicts=${candidates.count { it.kind == CanonicalTombstoneConflictCandidateKind.CONFLICT_RECORD }}",
            "resurrectionBlocks=${candidates.count { it.kind == CanonicalTombstoneConflictCandidateKind.RESURRECTION_BLOCK }}",
            "artifacts=${candidates.count { it.kind == CanonicalTombstoneConflictCandidateKind.GENERATED_ARTIFACT_TOMBSTONE }}",
            "observations=${observationReports.size}",
            "success=${observationReports.count { it.success }}",
            "failed=${observationReports.count { !it.success }}",
            "tombstonePreservation=${"%.2f".format(if (candidates.isEmpty()) 0.0 else candidates.count { it.tombstoneActive }.toDouble() / candidates.size)}",
            "antiResurrection=${"%.2f".format(if (candidates.isEmpty()) 0.0 else candidates.count { it.resurrectionBlocked }.toDouble() / candidates.size)}",
            "physicalDelete=${observationReports.count { it.physicalDeleteDetected }}",
            "permanentDelete=${observationReports.count { it.permanentDeleteDetected }}",
            "tombstoneGC=${observationReports.count { it.tombstoneGCDetected }}",
            "restore=${observationReports.count { it.restoreAttempted }}",
            "resurrection=${observationReports.count { it.resurrectionAttempted }}"
        ).joinToString(",")
    )
}

data class CanonicalTombstoneConflictGuardedCommitN1Readiness
private constructor(
    val ready: Boolean,
    val reason: String?,
    val gateAllowed: Boolean,
    val evidenceComplete: Boolean,
    val allObservationsSuccessful: Boolean,
    val noViolationsDetected: Boolean,
    val tombstoneIntegrityIntact: Boolean,
    val antiResurrectionEnforced: Boolean,
    val n1RollbackVerified: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        ready: Boolean,
        reason: String? = null,
        gateAllowed: Boolean = false,
        evidenceComplete: Boolean = false,
        allObservationsSuccessful: Boolean = false,
        noViolationsDetected: Boolean = false,
        tombstoneIntegrityIntact: Boolean = false,
        antiResurrectionEnforced: Boolean = false,
        n1RollbackVerified: Boolean = false
    ) : this(
        ready = ready,
        reason = reason,
        gateAllowed = gateAllowed,
        evidenceComplete = evidenceComplete,
        allObservationsSuccessful = allObservationsSuccessful,
        noViolationsDetected = noViolationsDetected,
        tombstoneIntegrityIntact = tombstoneIntegrityIntact,
        antiResurrectionEnforced = antiResurrectionEnforced,
        n1RollbackVerified = n1RollbackVerified,
        diagnosticsSummary = listOf(
            "ready=$ready",
            "reason=${reason ?: "none"}",
            "gate=$gateAllowed",
            "evidence=$evidenceComplete",
            "observations=$allObservationsSuccessful",
            "noViolations=$noViolationsDetected",
            "tombstoneIntegrity=$tombstoneIntegrityIntact",
            "antiResurrection=$antiResurrectionEnforced",
            "n1Rollback=$n1RollbackVerified"
        ).joinToString(",")
    )

    companion object {
        fun notReady(
            reason: String,
            gate: CanonicalTombstoneConflictGuardedCommitGate
        ): CanonicalTombstoneConflictGuardedCommitN1Readiness {
            return CanonicalTombstoneConflictGuardedCommitN1Readiness(
                ready = false,
                reason = reason,
                gateAllowed = gate.allowed,
                evidenceComplete = false,
                allObservationsSuccessful = false,
                noViolationsDetected = false,
                tombstoneIntegrityIntact = gate.tombstonePreservationConfirmed,
                antiResurrectionEnforced = gate.antiResurrectionConfirmed,
                n1RollbackVerified = false
            )
        }

        fun evaluate(
            gate: CanonicalTombstoneConflictGuardedCommitGate,
            evidence: CanonicalTombstoneConflictGuardedCommitEvidence
        ): CanonicalTombstoneConflictGuardedCommitN1Readiness {
            val allObservationsSuccessful = evidence.failedObservationCount == 0
            val noViolationsDetected = evidence.physicalDeleteEvents == 0 &&
                    evidence.permanentDeleteEvents == 0 &&
                    evidence.tombstoneGCEvents == 0 &&
                    evidence.restoreEvents == 0 &&
                    evidence.resurrectionEvents == 0
            val tombstoneIntact = gate.tombstonePreservationConfirmed
            val antiResurrection = gate.antiResurrectionConfirmed
            val n1Rollback = evidence.successfulObservationCount > 0 &&
                    evidence.failedObservationCount == 0

            val ready = gate.allowed &&
                    allObservationsSuccessful &&
                    noViolationsDetected &&
                    tombstoneIntact &&
                    antiResurrection &&
                    n1Rollback

            val reason = when {
                !gate.allowed -> "gateBlocked"
                !allObservationsSuccessful -> "observationsFailed"
                !noViolationsDetected -> "violationsDetected"
                !tombstoneIntact -> "tombstoneIntegrityCompromised"
                !antiResurrection -> "antiResurrectionNotEnforced"
                !n1Rollback -> "n1RollbackNotVerified"
                else -> null
            }

            return CanonicalTombstoneConflictGuardedCommitN1Readiness(
                ready = ready,
                reason = reason,
                gateAllowed = gate.allowed,
                evidenceComplete = evidence.successfulObservationCount > 0,
                allObservationsSuccessful = allObservationsSuccessful,
                noViolationsDetected = noViolationsDetected,
                tombstoneIntegrityIntact = tombstoneIntact,
                antiResurrectionEnforced = antiResurrection,
                n1RollbackVerified = n1Rollback
            )
        }
    }
}
