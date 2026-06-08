package com.rokurics.app.domain.canonical

import java.util.UUID

// ── Type 1: CanonicalGeneratedArtifactReadProjection ──

data class CanonicalGeneratedArtifactReadProjection(
    val artifacts: List<CanonicalArtifact>,
    val availability: CanonicalArtifact.Availability,
    val hashes: List<CanonicalHash>,
    val projectionID: String,
    val projectionKind: CanonicalGeneratedArtifactReadProjectionKind,
    val diagnosticsSummary: String
) {
    constructor(
        artifacts: List<CanonicalArtifact> = emptyList(),
        availability: CanonicalArtifact.Availability = CanonicalArtifact.Availability.UNKNOWN,
        hashes: List<CanonicalHash> = emptyList(),
        projectionID: String = UUID.randomUUID().toString(),
        projectionKind: CanonicalGeneratedArtifactReadProjectionKind = CanonicalGeneratedArtifactReadProjectionKind.CANONICAL_ONLY
    ) : this(
        artifacts = artifacts.sortedBy { it.artifactID },
        availability = computeAvailability(artifacts),
        hashes = artifacts.mapNotNull { it.contentHash },
        projectionID = projectionID,
        projectionKind = projectionKind,
        diagnosticsSummary = listOf(
            "artifacts=${artifacts.size}",
            "availability=${availability.name}",
            "hashes=${hashes.size}",
            "kind=${projectionKind.rawValue}"
        ).joinToString(",")
    )

    companion object {
        private fun computeAvailability(artifacts: List<CanonicalArtifact>): CanonicalArtifact.Availability {
            if (artifacts.isEmpty()) return CanonicalArtifact.Availability.MISSING
            val allAvailable = artifacts.all { it.contentHash != null && it.byteSize != null }
            return if (allAvailable) CanonicalArtifact.Availability.AVAILABLE else CanonicalArtifact.Availability.AVAILABLE_WITHOUT_HASH
        }

        fun empty(): CanonicalGeneratedArtifactReadProjection {
            return CanonicalGeneratedArtifactReadProjection(
                artifacts = emptyList(),
                availability = CanonicalArtifact.Availability.MISSING
            )
        }
    }

    val generatedArtifactCount: Int
        get() = artifacts.count { it.isCanonicalGeneratedArtifact }
}

enum class CanonicalGeneratedArtifactReadProjectionKind(val rawValue: String) {
    CANONICAL_ONLY("canonicalOnly"),
    LEGACY_ONLY("legacyOnly"),
    PARALLEL_COMPARE("parallelCompare"),
    BLOCKED("blocked");

    companion object {
        val allCases: List<CanonicalGeneratedArtifactReadProjectionKind> = entries.toList()
    }
}

// ── Type 2: CanonicalGeneratedArtifactReadDivergence ──

data class CanonicalGeneratedArtifactReadDivergence(
    val objectID: String,
    val artifactID: String,
    val kind: CanonicalGeneratedArtifactCandidateKind,
    val divergenceType: CanonicalGeneratedArtifactReadDivergenceType,
    val canonicalValue: String?,
    val legacyValue: String?,
    val field: String?,
    val blocking: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        objectID: String,
        artifactID: String,
        kind: CanonicalGeneratedArtifactCandidateKind,
        divergenceType: CanonicalGeneratedArtifactReadDivergenceType,
        canonicalValue: String? = null,
        legacyValue: String? = null,
        field: String? = null,
        blocking: Boolean = false
    ) : this(
        objectID = objectID.trim().nilIfEmpty ?: "unknown-object",
        artifactID = artifactID.trim().nilIfEmpty ?: "unknown-artifact",
        kind = kind,
        divergenceType = divergenceType,
        canonicalValue = canonicalValue?.trim()?.nilIfEmpty,
        legacyValue = legacyValue?.trim()?.nilIfEmpty,
        field = field?.trim()?.nilIfEmpty,
        blocking = blocking,
        diagnosticsSummary = listOf(
            "object=$objectID",
            "artifact=$artifactID",
            "kind=${kind.rawValue}",
            "type=${divergenceType.rawValue}",
            "field=${field ?: "none"}",
            "blocking=$blocking"
        ).joinToString(",")
    )
}

enum class CanonicalGeneratedArtifactReadDivergenceType(val rawValue: String) {
    ARTIFACT_MISSING_FROM_CANONICAL("artifactMissingFromCanonical"),
    ARTIFACT_MISSING_FROM_LEGACY("artifactMissingFromLegacy"),
    CONTENT_HASH_MISMATCH("contentHashMismatch"),
    BYTE_SIZE_MISMATCH("byteSizeMismatch"),
    AVAILABILITY_MISMATCH("availabilityMismatch"),
    LOGICAL_NAME_MISMATCH("logicalNameMismatch"),
    LOGICAL_PATH_MISMATCH("logicalPathMismatch"),
    PRODUCER_MISMATCH("producerMismatch"),
    TOMBSTONE_MISMATCH("tombstoneMismatch"),
    MODIFIED_AT_MISMATCH("modifiedAtMismatch"),
    UNKNOWN("unknown");

    companion object {
        val allCases: List<CanonicalGeneratedArtifactReadDivergenceType> = entries.toList()
    }
}

// ── Type 3: CanonicalGeneratedArtifactReadDiff ──

data class CanonicalGeneratedArtifactReadDiff(
    val divergences: List<CanonicalGeneratedArtifactReadDivergence>,
    val equivalentCount: Int,
    val divergentCount: Int,
    val canonicalProjectionID: String?,
    val legacyProjectionID: String?,
    val comparedAt: String?,
    val diagnosticsSummary: String
) {
    constructor(
        divergences: List<CanonicalGeneratedArtifactReadDivergence> = emptyList(),
        equivalentCount: Int = 0,
        divergentCount: Int = 0,
        canonicalProjectionID: String? = null,
        legacyProjectionID: String? = null,
        comparedAt: String? = null
    ) : this(
        divergences = divergences.sortedBy { it.artifactID },
        equivalentCount = maxOf(0, equivalentCount),
        divergentCount = maxOf(0, divergentCount),
        canonicalProjectionID = canonicalProjectionID?.trim()?.nilIfEmpty,
        legacyProjectionID = legacyProjectionID?.trim()?.nilIfEmpty,
        comparedAt = comparedAt?.trim()?.nilIfEmpty,
        diagnosticsSummary = listOf(
            "equivalent=$equivalentCount",
            "divergent=$divergentCount",
            "divergences=${divergences.size}",
            "canonical=${canonicalProjectionID ?: "none"}",
            "legacy=${legacyProjectionID ?: "none"}"
        ).joinToString(",")
    )

    val equivalent: Boolean
        get() = divergences.isEmpty() && equivalentCount > 0 && divergentCount == 0

    companion object {
        fun equivalent(
            equivalentCount: Int,
            canonicalProjectionID: String?,
            legacyProjectionID: String?
        ): CanonicalGeneratedArtifactReadDiff {
            return CanonicalGeneratedArtifactReadDiff(
                divergences = emptyList(),
                equivalentCount = equivalentCount,
                divergentCount = 0,
                canonicalProjectionID = canonicalProjectionID,
                legacyProjectionID = legacyProjectionID
            )
        }

        fun divergent(
            divergences: List<CanonicalGeneratedArtifactReadDivergence>,
            equivalentCount: Int = 0,
            canonicalProjectionID: String? = null,
            legacyProjectionID: String? = null
        ): CanonicalGeneratedArtifactReadDiff {
            return CanonicalGeneratedArtifactReadDiff(
                divergences = divergences,
                equivalentCount = equivalentCount,
                divergentCount = divergences.size,
                canonicalProjectionID = canonicalProjectionID,
                legacyProjectionID = legacyProjectionID
            )
        }
    }
}

// ── Type 4: CanonicalGeneratedArtifactObservationWindow ──

data class CanonicalGeneratedArtifactObservationWindow(
    val windowOpen: Boolean = false,
    val evidenceRequired: Boolean = true,
    val observationID: String = UUID.randomUUID().toString(),
    val observationStart: String? = null,
    val observationEnd: String? = null,
    val minDurationSeconds: Double = 0.0,
    val diagnosticsSummary: String = ""
) {

    companion object {
        fun open(
            minDurationSeconds: Double = 86_400.0,
            observationStart: String? = null
        ): CanonicalGeneratedArtifactObservationWindow {
            return CanonicalGeneratedArtifactObservationWindow(
                windowOpen = true,
                evidenceRequired = true,
                observationStart = observationStart,
                minDurationSeconds = minDurationSeconds
            )
        }

        fun closed(reason: String): CanonicalGeneratedArtifactObservationWindow {
            return CanonicalGeneratedArtifactObservationWindow(
                windowOpen = false,
                evidenceRequired = false,
                diagnosticsSummary = "closed|reason=$reason"
            )
        }
    }
}

// ── Type 5: CanonicalGeneratedArtifactObservationGate ──

object CanonicalGeneratedArtifactObservationGate {
    data class ObservationGateResult(
        val allowed: Boolean,
        val window: CanonicalGeneratedArtifactObservationWindow,
        val blockers: List<String>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun blocked(
                window: CanonicalGeneratedArtifactObservationWindow,
                blockers: List<String>
            ): ObservationGateResult {
                return ObservationGateResult(
                    allowed = false,
                    window = window,
                    blockers = blockers.sorted(),
                    diagnosticsSummary = listOf(
                        "allowed=false",
                        "windowOpen=${window.windowOpen}",
                        "blockers=${blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun evaluate(
        window: CanonicalGeneratedArtifactObservationWindow,
        readDiff: CanonicalGeneratedArtifactReadDiff?,
        canaryResults: List<CanonicalGeneratedArtifactCanaryRunner.CanaryRunResult>
    ): ObservationGateResult {
        val blockers = mutableListOf<String>()

        if (!window.windowOpen) {
            blockers.add("observationWindowClosed")
        }

        if (window.evidenceRequired && (canaryResults.isEmpty() || canaryResults.any { !it.allSucceeded })) {
            blockers.add("canaryEvidenceInsufficient")
        }

        if (readDiff == null) {
            blockers.add("readDiffMissing")
        } else if (!readDiff.equivalent) {
            blockers.add("readDiffNotEquivalent")
        }

        val allowed = blockers.isEmpty()

        return ObservationGateResult(
            allowed = allowed,
            window = window,
            blockers = blockers,
            diagnosticsSummary = listOf(
                "allowed=$allowed",
                "windowOpen=${window.windowOpen}",
                "evidenceRequired=${window.evidenceRequired}",
                "canaryResults=${canaryResults.size}",
                "readDiffEquivalent=${readDiff?.equivalent ?: false}",
                "blockers=${blockers.joinToString("|")}"
            ).joinToString(",")
        )
    }
}

// ── Type 6: CanonicalGeneratedArtifactRetirementCandidateGate ──

data class CanonicalGeneratedArtifactRetirementCandidateGate(
    val ready: Boolean,
    val blockers: List<String>,
    val gateID: String,
    val observationGateAllowed: Boolean,
    val readProjectionAvailable: Boolean,
    val readDiffEquivalent: Boolean,
    val canaryStagesComplete: Boolean,
    val observationPeriodSatisfied: Boolean,
    val legacyCodePathNoLongerNeeded: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        ready: Boolean,
        blockers: List<String> = emptyList(),
        gateID: String = UUID.randomUUID().toString(),
        observationGateAllowed: Boolean = false,
        readProjectionAvailable: Boolean = false,
        readDiffEquivalent: Boolean = false,
        canaryStagesComplete: Boolean = false,
        observationPeriodSatisfied: Boolean = false,
        legacyCodePathNoLongerNeeded: Boolean = false
    ) : this(
        ready = ready && blockers.isEmpty(),
        blockers = blockers.sorted(),
        gateID = gateID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
        observationGateAllowed = observationGateAllowed,
        readProjectionAvailable = readProjectionAvailable,
        readDiffEquivalent = readDiffEquivalent,
        canaryStagesComplete = canaryStagesComplete,
        observationPeriodSatisfied = observationPeriodSatisfied,
        legacyCodePathNoLongerNeeded = legacyCodePathNoLongerNeeded,
        diagnosticsSummary = listOf(
            "ready=$ready",
            "observationGate=$observationGateAllowed",
            "projectionAvailable=$readProjectionAvailable",
            "diffEquivalent=$readDiffEquivalent",
            "canaryComplete=$canaryStagesComplete",
            "observationSatisfied=$observationPeriodSatisfied",
            "legacyNoLongerNeeded=$legacyCodePathNoLongerNeeded",
            "blockers=${blockers.joinToString("|")}"
        ).joinToString(",")
    )

    companion object {
        fun evaluate(
            observationGateResult: CanonicalGeneratedArtifactObservationGate.ObservationGateResult,
            readProjection: CanonicalGeneratedArtifactReadProjection?,
            readDiff: CanonicalGeneratedArtifactReadDiff?,
            canaryResults: List<CanonicalGeneratedArtifactCanaryRunner.CanaryRunResult>
        ): CanonicalGeneratedArtifactRetirementCandidateGate {
            val blockers = mutableListOf<String>()

            if (!observationGateResult.allowed) {
                blockers.addAll(observationGateResult.blockers)
            }
            if (readProjection == null || readProjection.generatedArtifactCount == 0) {
                blockers.add("readProjectionUnavailable")
            }
            if (readDiff != null && !readDiff.equivalent) {
                blockers.add("readDiffNotEquivalent")
            }
            if (canaryResults.isEmpty() || canaryResults.any { !it.allSucceeded }) {
                blockers.add("canaryStagesIncomplete")
            }

            return CanonicalGeneratedArtifactRetirementCandidateGate(
                ready = blockers.isEmpty(),
                blockers = blockers,
                observationGateAllowed = observationGateResult.allowed,
                readProjectionAvailable = readProjection != null && readProjection.generatedArtifactCount > 0,
                readDiffEquivalent = readDiff?.equivalent ?: false,
                canaryStagesComplete = canaryResults.isNotEmpty() && canaryResults.all { it.allSucceeded },
                observationPeriodSatisfied = true,
                legacyCodePathNoLongerNeeded = blockers.isEmpty()
            )
        }
    }
}

// ── Type 7: CanonicalGeneratedArtifactTemplateReadinessReport ──

data class CanonicalGeneratedArtifactTemplateReadinessReport(
    val ready: Boolean,
    val domainCutoverComplete: Boolean,
    val canaryResults: List<CanonicalGeneratedArtifactCanaryRunner.CanaryRunResult>,
    val observationGateResult: CanonicalGeneratedArtifactObservationGate.ObservationGateResult?,
    val readDiff: CanonicalGeneratedArtifactReadDiff?,
    val retirementGate: CanonicalGeneratedArtifactRetirementCandidateGate?,
    val blockers: List<String>,
    val legacyCodePathDeprecatable: Boolean,
    val legacyDataPathOrphaned: Boolean,
    val canonicalReadPathStable: Boolean,
    val productionWritePathStable: Boolean,
    val noPendingCanaryStages: Boolean,
    val observationWindowSatisfied: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        canaryResults: List<CanonicalGeneratedArtifactCanaryRunner.CanaryRunResult>,
        observationGateResult: CanonicalGeneratedArtifactObservationGate.ObservationGateResult?,
        readDiff: CanonicalGeneratedArtifactReadDiff?,
        retirementGate: CanonicalGeneratedArtifactRetirementCandidateGate?
    ) : this(
        ready = retirementGate?.ready ?: false,
        domainCutoverComplete = canaryResults.isNotEmpty() && canaryResults.all { it.allSucceeded },
        canaryResults = canaryResults,
        observationGateResult = observationGateResult,
        readDiff = readDiff,
        retirementGate = retirementGate,
        blockers = retirementGate?.blockers ?: emptyList(),
        legacyCodePathDeprecatable = canaryResults.all { it.allSucceeded },
        legacyDataPathOrphaned = canaryResults.isNotEmpty() && canaryResults.all { it.allSucceeded },
        canonicalReadPathStable = readDiff?.equivalent ?: false,
        productionWritePathStable = canaryResults.all { it.allSucceeded },
        noPendingCanaryStages = canaryResults.none { it.stageExecuted != CanonicalGeneratedArtifactCanaryStage.DISABLED },
        observationWindowSatisfied = observationGateResult?.allowed ?: false,
        diagnosticsSummary = listOf(
            "ready=${retirementGate?.ready ?: false}",
            "domainCutover=${canaryResults.isNotEmpty() && canaryResults.all { it.allSucceeded }}",
            "canaryStages=${canaryResults.size}",
            "observationAllowed=${observationGateResult?.allowed ?: false}",
            "readDiffEquivalent=${readDiff?.equivalent ?: false}",
            "retirementReady=${retirementGate?.ready ?: false}",
            "blockers=${retirementGate?.blockers?.joinToString("|") ?: "none"}"
        ).joinToString(",")
    )
}
