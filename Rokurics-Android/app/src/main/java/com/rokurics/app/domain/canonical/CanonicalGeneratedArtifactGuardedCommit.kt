package com.rokurics.app.domain.canonical

import java.util.UUID

// ── Type 1: CanonicalGeneratedArtifactGuardedCommitSeam ──

class CanonicalGeneratedArtifactGuardedCommitSeam(
    private val gates: List<CanonicalGeneratedArtifactGuardedCommitGate>,
    private val evidence: List<CanonicalGeneratedArtifactGuardedCommitEvidence>,
    private val n1ReadinessReports: List<CanonicalGeneratedArtifactGuardedCommitN1Readiness>
) {
    data class GateEvaluationResult(
        val gate: CanonicalGeneratedArtifactGuardedCommitGate,
        val evaluated: Boolean,
        val allowed: Boolean,
        val activeBlockers: List<CanonicalGeneratedArtifactGuardedCommitBlocker>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun fromGate(gate: CanonicalGeneratedArtifactGuardedCommitGate): GateEvaluationResult {
                return GateEvaluationResult(
                    gate = gate,
                    evaluated = true,
                    allowed = gate.allowed,
                    activeBlockers = if (gate.allowed) emptyList() else gate.blockers,
                    diagnosticsSummary = listOf(
                        "allowed=${gate.allowed}",
                        "blockers=${gate.blockers.map { it.rawValue }}"
                    ).joinToString(",")
                )
            }

            fun notEvaluated(reason: String): GateEvaluationResult {
                return GateEvaluationResult(
                    gate = CanonicalGeneratedArtifactGuardedCommitGate(
                        id = "not-evaluated",
                        allowed = false,
                        blockers = listOf(CanonicalGeneratedArtifactGuardedCommitBlocker.DISABLED)
                    ),
                    evaluated = false,
                    allowed = false,
                    activeBlockers = listOf(CanonicalGeneratedArtifactGuardedCommitBlocker.DISABLED),
                    diagnosticsSummary = "evaluated=false|reason=$reason"
                )
            }
        }
    }

    fun evaluateGate(): GateEvaluationResult {
        if (gates.isEmpty()) {
            return GateEvaluationResult.notEvaluated("noGatesConfigured")
        }

        val activeBlockers = mutableListOf<CanonicalGeneratedArtifactGuardedCommitBlocker>()
        var allAllowed = true

        for (gate in gates) {
            if (!gate.allowed) {
                allAllowed = false
                activeBlockers.addAll(gate.blockers)
            }
        }

        if (n1ReadinessReports.isEmpty()) {
            activeBlockers.add(CanonicalGeneratedArtifactGuardedCommitBlocker.NO_EVIDENCE)
        }

        if (activeBlockers.isEmpty()) {
            val hasActiveConflict = evidence.any { it.activePilotConflict }
            if (hasActiveConflict) {
                activeBlockers.add(CanonicalGeneratedArtifactGuardedCommitBlocker.ACTIVE_PILOT_CONFLICT)
            }
        }

        if (evidence.all { !it.executed }) {
            activeBlockers.add(CanonicalGeneratedArtifactGuardedCommitBlocker.N0_NO_EXECUTION)
            allAllowed = false
        }

        val hasUnsupportedCandidates = evidence.any { !it.candidateSupported }
        if (hasUnsupportedCandidates) {
            activeBlockers.add(CanonicalGeneratedArtifactGuardedCommitBlocker.UNSUPPORTED_CANDIDATE)
            allAllowed = false
        }

        val representativeGate = CanonicalGeneratedArtifactGuardedCommitGate(
            id = gates.first().id,
            allowed = allAllowed && activeBlockers.isEmpty(),
            blockers = activeBlockers.distinct().sortedBy { it.rawValue }
        )

        return GateEvaluationResult.fromGate(representativeGate)
    }
}

// ── Type 2: CanonicalGeneratedArtifactGuardedCommitGate ──

data class CanonicalGeneratedArtifactGuardedCommitGate(
    val id: String = UUID.randomUUID().toString(),
    val allowed: Boolean = false,
    val blockers: List<CanonicalGeneratedArtifactGuardedCommitBlocker> = emptyList()
) {

    companion object {
        fun allowed(): CanonicalGeneratedArtifactGuardedCommitGate {
            return CanonicalGeneratedArtifactGuardedCommitGate(allowed = true)
        }

        fun blocked(blockers: List<CanonicalGeneratedArtifactGuardedCommitBlocker>): CanonicalGeneratedArtifactGuardedCommitGate {
            return CanonicalGeneratedArtifactGuardedCommitGate(allowed = false, blockers = blockers)
        }
    }
}

enum class CanonicalGeneratedArtifactGuardedCommitBlocker(val rawValue: String) {
    DISABLED("disabled"),
    NO_EVIDENCE("noEvidence"),
    ACTIVE_PILOT_CONFLICT("activePilotConflict"),
    N0_NO_EXECUTION("n0NoExecution"),
    UNSUPPORTED_CANDIDATE("unsupportedCandidate");

    companion object {
        val allCases: List<CanonicalGeneratedArtifactGuardedCommitBlocker> = entries.toList()
    }
}

// ── Type 3: CanonicalGeneratedArtifactGuardedCommitEvidence ──

data class CanonicalGeneratedArtifactGuardedCommitEvidence(
    val objectID: String,
    val artifactID: String,
    val kind: CanonicalGeneratedArtifactCandidateKind,
    val executed: Boolean,
    val hashVerified: Boolean,
    val sizeVerified: Boolean,
    val applied: Boolean,
    val candidateSupported: Boolean,
    val activePilotConflict: Boolean,
    val stagingEvidenceID: String?,
    val rollbackEvidenceID: String?,
    val diagnosticsSummary: String
) {
    constructor(
        objectID: String,
        artifactID: String,
        kind: CanonicalGeneratedArtifactCandidateKind,
        executed: Boolean = false,
        hashVerified: Boolean = false,
        sizeVerified: Boolean = false,
        applied: Boolean = false,
        candidateSupported: Boolean = true,
        activePilotConflict: Boolean = false,
        stagingEvidenceID: String? = null,
        rollbackEvidenceID: String? = null
    ) : this(
        objectID = objectID.trim().nilIfEmpty ?: "unknown-object",
        artifactID = artifactID.trim().nilIfEmpty ?: "unknown-artifact",
        kind = kind,
        executed = executed,
        hashVerified = hashVerified,
        sizeVerified = sizeVerified,
        applied = applied,
        candidateSupported = candidateSupported,
        activePilotConflict = activePilotConflict,
        stagingEvidenceID = stagingEvidenceID?.trim()?.nilIfEmpty,
        rollbackEvidenceID = rollbackEvidenceID?.trim()?.nilIfEmpty,
        diagnosticsSummary = listOf(
            "object=$objectID",
            "artifact=$artifactID",
            "kind=${kind.rawValue}",
            "executed=$executed",
            "hashVerified=$hashVerified",
            "sizeVerified=$sizeVerified",
            "applied=$applied",
            "supported=$candidateSupported",
            "pilotConflict=$activePilotConflict"
        ).joinToString(",")
    )

    val fullyVerified: Boolean
        get() = hashVerified && sizeVerified

    val commitReady: Boolean
        get() = executed && fullyVerified && applied && candidateSupported && !activePilotConflict
}

// ── Type 4: CanonicalGeneratedArtifactGuardedCommitN1Readiness ──

data class CanonicalGeneratedArtifactGuardedCommitN1Readiness(
    val objectID: String,
    val artifactID: String,
    val kind: CanonicalGeneratedArtifactCandidateKind,
    val n1Executed: Boolean,
    val n1Succeeded: Boolean,
    val hashVerified: Boolean,
    val sizeVerified: Boolean,
    val equivalent: Boolean,
    val stagingRootLifecycleComplete: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        objectID: String,
        artifactID: String,
        kind: CanonicalGeneratedArtifactCandidateKind,
        n1Executed: Boolean = false,
        n1Succeeded: Boolean = false,
        hashVerified: Boolean = false,
        sizeVerified: Boolean = false,
        equivalent: Boolean = false,
        stagingRootLifecycleComplete: Boolean = false
    ) : this(
        objectID = objectID.trim().nilIfEmpty ?: "unknown-object",
        artifactID = artifactID.trim().nilIfEmpty ?: "unknown-artifact",
        kind = kind,
        n1Executed = n1Executed,
        n1Succeeded = n1Succeeded,
        hashVerified = hashVerified,
        sizeVerified = sizeVerified,
        equivalent = equivalent,
        stagingRootLifecycleComplete = stagingRootLifecycleComplete,
        diagnosticsSummary = listOf(
            "object=$objectID",
            "artifact=$artifactID",
            "kind=${kind.rawValue}",
            "n1Executed=$n1Executed",
            "n1Succeeded=$n1Succeeded",
            "hashVerified=$hashVerified",
            "sizeVerified=$sizeVerified",
            "equivalent=$equivalent",
            "stagingComplete=$stagingRootLifecycleComplete"
        ).joinToString(",")
    )

    val ready: Boolean
        get() = n1Executed && n1Succeeded && hashVerified && sizeVerified && equivalent && stagingRootLifecycleComplete
}
