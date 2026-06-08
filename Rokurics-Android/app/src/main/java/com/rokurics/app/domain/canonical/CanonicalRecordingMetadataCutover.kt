package com.rokurics.app.domain.canonical

import java.util.Date
import java.util.UUID

// ── CanonicalCutoverDomain ──

enum class CanonicalCutoverDomain(val rawValue: String) {
    RECORDING_METADATA("recordingMetadata"),
    LIBRARY_METADATA("libraryMetadata"),
    GENERATED_ARTIFACTS("generatedArtifacts"),
    TOMBSTONE_CONFLICT("tombstoneConflict"),
    AUDIO_UPLOAD("audioUpload"),
    UI_PROJECTION("uiProjection"),
    LEGACY_RETIREMENT("legacyRetirement");

    companion object {
        val allCases: List<CanonicalCutoverDomain> = entries.toList()
    }
}

// ── CanonicalCutoverAppSeamMode ──

enum class CanonicalCutoverAppSeamMode(val rawValue: String) {
    DISABLED("disabled"),
    GUARDED_EXECUTE_NO_COMMIT("guardedExecuteNoCommit"),
    GUARDED_EXECUTE_COMMIT("guardedExecuteCommit"),
    CANARY_COMMIT("canaryCommit"),
    BLOCKED("blocked");

    companion object {
        val allCases: List<CanonicalCutoverAppSeamMode> = entries.toList()
    }
}

// ── CanonicalCutoverAppSeamFailure ──

enum class CanonicalCutoverAppSeamFailure(val rawValue: String) {
    DISABLED_BY_DEFAULT("disabledByDefault"),
    DOMAIN_NOT_ALLOWED("domainNotAllowed"),
    MISSING_EVIDENCE("missingEvidence"),
    UNSUPPORTED_TRIGGER("unsupportedTrigger"),
    PRODUCTION_ROOT_BLOCKED("productionRootBlocked"),
    UNSAFE_PATH("unsafePath"),
    UNSUPPORTED_OPERATION("unsupportedOperation");

    companion object {
        val allCases: List<CanonicalCutoverAppSeamFailure> = entries.toList()
    }
}

// ── CanonicalCutoverAppSeamGate ──

data class CanonicalCutoverAppSeamGate(
    val domain: CanonicalCutoverDomain,
    val mode: CanonicalCutoverAppSeamMode,
    val allowed: Boolean,
    val failures: List<CanonicalCutoverAppSeamFailure>
)

// ── CanonicalRecordingMetadataNoCommitEquivalenceStatus ──

enum class CanonicalRecordingMetadataNoCommitEquivalenceStatus(val rawValue: String) {
    EQUIVALENT("equivalent"),
    CANONICAL_MORE_CONSERVATIVE("canonicalMoreConservative"),
    DIVERGENT("divergent"),
    INSUFFICIENT_EVIDENCE("insufficientEvidence"),
    UNSUPPORTED("unsupported");

    companion object {
        val allCases: List<CanonicalRecordingMetadataNoCommitEquivalenceStatus> = entries.toList()
    }
}

// ── CanonicalRecordingMetadataNoCommitEquivalenceDirection ──

enum class CanonicalRecordingMetadataNoCommitEquivalenceDirection(val rawValue: String) {
    APPLY("apply"),
    SEND("send"),
    NONE("none");

    companion object {
        val allCases: List<CanonicalRecordingMetadataNoCommitEquivalenceDirection> = entries.toList()
    }
}

// ── CanonicalRecordingMetadataNoCommitEquivalenceCandidate ──

data class CanonicalRecordingMetadataNoCommitEquivalenceCandidate(
    val objectID: String,
    val metadataHashPrefix: String?,
    val canonicalDirection: CanonicalRecordingMetadataNoCommitEquivalenceDirection,
    val routePath: String,
    val blocking: Boolean,
    val status: CanonicalRecordingMetadataNoCommitEquivalenceStatus
)

// ── CanonicalRecordingMetadataNoCommitCandidateStagingInfo ──

data class CanonicalRecordingMetadataNoCommitCandidateStagingInfo(
    val stagingEvidence: CanonicalNoCommitStagingEvidence?,
    val cleanupEvidence: CanonicalNoCommitCleanupEvidence?,
    val wouldApply: Boolean,
    val wouldSend: Boolean
)

// ── CanonicalRecordingMetadataNoCommitCandidateResult ──

data class CanonicalRecordingMetadataNoCommitCandidateResult(
    val equivalence: CanonicalRecordingMetadataNoCommitEquivalenceCandidate,
    val staging: CanonicalRecordingMetadataNoCommitCandidateStagingInfo?,
    val failure: CanonicalRecordingMetadataNoCommitCandidateFailure?
)

// ── CanonicalRecordingMetadataNoCommitCandidateFailure ──

enum class CanonicalRecordingMetadataNoCommitCandidateFailure(val rawValue: String) {
    STAGING_CREATION_FAILED("stagingCreationFailed"),
    CLEANUP_FAILED("cleanupFailed"),
    EVIDENCE_COLLECTION_FAILED("evidenceCollectionFailed"),
    ROUTE_BLOCKED("routeBlocked"),
    INSUFFICIENT_METADATA("insufficientMetadata"),
    HASH_MISMATCH("hashMismatch"),
    UNSUPPORTED_OBJECT_KIND("unsupportedObjectKind");

    companion object {
        val allCases: List<CanonicalRecordingMetadataNoCommitCandidateFailure> = entries.toList()
    }
}

// ── CanonicalRecordingMetadataNoCommitRunner ──

class CanonicalRecordingMetadataNoCommitRunner(
    private val gate: CanonicalCutoverAppSeamGate,
    private val objectIDs: List<String>,
    private val evidenceSource: CanonicalNoCommitEvidenceSource? = null
) {
    fun runNoCommit(): CanonicalNoCommitEvidenceReport {
        if (!gate.allowed) {
            return CanonicalNoCommitEvidenceReport(
                domain = gate.domain,
                mode = gate.mode,
                status = CanonicalNoCommitEvidenceStatus.BLOCKED,
                candidateCount = 0,
                wouldApplyCount = 0,
                wouldSendCount = 0,
                equivalentCount = 0,
                divergentCount = 0,
                insufficientEvidenceCount = 0,
                unsupportedCount = objectIDs.size,
                stagingRootLifecycleStatus = "",
                cleanupStatus = "",
                routeProjectionStatus = "",
                legacyActionComparisonStatus = "",
                productionCommitSuppressed = true,
                legacyDuplicateSuppressed = true,
                sideEffectClass = CanonicalNoCommitSideEffectClass.STAGING_ONLY,
                equivalenceEvidence = CanonicalNoCommitEquivalenceEvidence(
                    equivalentCount = 0,
                    divergentCount = 0,
                    insufficientEvidenceCount = 0,
                    unsupportedCount = objectIDs.size,
                    hashPrefixes = emptyList(),
                    routeProjectionStatus = "",
                    legacyActionComparisonStatus = ""
                ),
                stagingEvidence = emptyList(),
                cleanupEvidence = emptyList(),
                blockers = gate.failures.map {
                    CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.BLOCKER, it.rawValue)
                }
            )
        }

        val candidateResults = objectIDs.map { objectID ->
            val evidence = evidenceSource?.getEvidence(objectID)
            val candidate = CanonicalRecordingMetadataNoCommitEquivalenceCandidate(
                objectID = objectID,
                metadataHashPrefix = evidence?.hashPrefix,
                canonicalDirection = evidence?.direction
                    ?: CanonicalRecordingMetadataNoCommitEquivalenceDirection.NONE,
                routePath = evidence?.routePath ?: "unknown/$objectID",
                blocking = evidence?.blocking ?: false,
                status = evidence?.status
                    ?: CanonicalRecordingMetadataNoCommitEquivalenceStatus.INSUFFICIENT_EVIDENCE
            )

            val staging = if (candidate.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.EQUIVALENT ||
                candidate.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.CANONICAL_MORE_CONSERVATIVE
            ) {
                CanonicalRecordingMetadataNoCommitCandidateStagingInfo(
                    stagingEvidence = CanonicalNoCommitStagingEvidence(
                        rootID = objectID,
                        rootKind = CanonicalNoCommitStagingRootKind.EXPLICIT_STAGING_ROOT,
                        lifecycleStatus = CanonicalNoCommitStagingRootLifecycleStatus.CREATED
                    ),
                    cleanupEvidence = null,
                    wouldApply = candidate.canonicalDirection == CanonicalRecordingMetadataNoCommitEquivalenceDirection.APPLY,
                    wouldSend = candidate.canonicalDirection == CanonicalRecordingMetadataNoCommitEquivalenceDirection.SEND
                )
            } else {
                null
            }

            val failure = when (candidate.status) {
                CanonicalRecordingMetadataNoCommitEquivalenceStatus.UNSUPPORTED ->
                    CanonicalRecordingMetadataNoCommitCandidateFailure.UNSUPPORTED_OBJECT_KIND
                CanonicalRecordingMetadataNoCommitEquivalenceStatus.INSUFFICIENT_EVIDENCE ->
                    CanonicalRecordingMetadataNoCommitCandidateFailure.INSUFFICIENT_METADATA
                else -> null
            }

            CanonicalRecordingMetadataNoCommitCandidateResult(
                equivalence = candidate,
                staging = staging,
                failure = failure
            )
        }

        val equivalentCount = candidateResults.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.EQUIVALENT
        }
        val divergentCount = candidateResults.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.DIVERGENT
        }
        val insufficientEvidenceCount = candidateResults.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.INSUFFICIENT_EVIDENCE
        }
        val unsupportedCount = candidateResults.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.UNSUPPORTED
        }
        val wouldApplyCount = candidateResults.count {
            it.staging?.wouldApply == true
        }
        val wouldSendCount = candidateResults.count {
            it.staging?.wouldSend == true
        }

        val stagingEvidence = candidateResults.mapNotNull { it.staging?.stagingEvidence }
        val cleanupEvidence = candidateResults.mapNotNull { it.staging?.cleanupEvidence }

        val equivalenceEvidence = CanonicalNoCommitEquivalenceEvidence(
            equivalentCount = equivalentCount,
            divergentCount = divergentCount,
            insufficientEvidenceCount = insufficientEvidenceCount,
            unsupportedCount = unsupportedCount,
            hashPrefixes = emptyList(),
            routeProjectionStatus = "",
            legacyActionComparisonStatus = ""
        )

        val blockers = buildList {
            addAll(gate.failures.map {
                CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.BLOCKER, it.rawValue)
            })
            addAll(candidateResults.mapNotNull { result ->
                result.failure?.let {
                    CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.BLOCKER, it.rawValue)
                }
            })
        }.distinctBy { it.id }

        val status = when {
            !gate.allowed -> CanonicalNoCommitEvidenceStatus.BLOCKED
            unsupportedCount > 0 -> CanonicalNoCommitEvidenceStatus.UNSUPPORTED
            insufficientEvidenceCount > 0 -> CanonicalNoCommitEvidenceStatus.INSUFFICIENT_EVIDENCE
            divergentCount > 0 -> CanonicalNoCommitEvidenceStatus.DIVERGENT
            blockers.any { it.severity == CanonicalNoCommitBlockerSeverity.WARNING } ->
                CanonicalNoCommitEvidenceStatus.WARNING
            else -> CanonicalNoCommitEvidenceStatus.COMPLETE
        }

        return CanonicalNoCommitEvidenceReport(
            domain = gate.domain,
            mode = gate.mode,
            status = status,
            candidateCount = candidateResults.size,
            wouldApplyCount = wouldApplyCount,
            wouldSendCount = wouldSendCount,
            equivalentCount = equivalentCount,
            divergentCount = divergentCount,
            insufficientEvidenceCount = insufficientEvidenceCount,
            unsupportedCount = unsupportedCount,
            stagingRootLifecycleStatus = if (stagingEvidence.isNotEmpty()) "created" else "",
            cleanupStatus = if (cleanupEvidence.isNotEmpty()) "removed" else "",
            routeProjectionStatus = if (candidateResults.isNotEmpty()) "projected" else "",
            legacyActionComparisonStatus = if (equivalentCount > 0) "compared" else "",
            productionCommitSuppressed = true,
            legacyDuplicateSuppressed = true,
            sideEffectClass = CanonicalNoCommitSideEffectClass.STAGING_ONLY,
            equivalenceEvidence = equivalenceEvidence,
            stagingEvidence = stagingEvidence,
            cleanupEvidence = cleanupEvidence,
            blockers = blockers
        )
    }
}

// ── CanonicalNoCommitEvidenceSource (internal interface for runner) ──

data class CanonicalNoCommitEvidenceEntry(
    val hashPrefix: String?,
    val direction: CanonicalRecordingMetadataNoCommitEquivalenceDirection,
    val routePath: String,
    val blocking: Boolean,
    val status: CanonicalRecordingMetadataNoCommitEquivalenceStatus
)

interface CanonicalNoCommitEvidenceSource {
    fun getEvidence(objectID: String): CanonicalNoCommitEvidenceEntry?
}

// ── CanonicalRecordingMetadataGuardedCommitSeam ──

class CanonicalRecordingMetadataGuardedCommitSeam(
    private val domain: CanonicalCutoverDomain = CanonicalCutoverDomain.RECORDING_METADATA,
    private val evidenceCollection: CanonicalNoCommitEvidenceSource? = null,
    private val canaryPolicy: CanonicalRecordingMetadataCanaryPolicy? = null,
    private val allowedModes: Set<CanonicalCutoverAppSeamMode> = setOf(
        CanonicalCutoverAppSeamMode.GUARDED_EXECUTE_NO_COMMIT,
        CanonicalCutoverAppSeamMode.GUARDED_EXECUTE_COMMIT,
        CanonicalCutoverAppSeamMode.CANARY_COMMIT
    ),
    private val blockedModes: Set<CanonicalCutoverAppSeamMode> = setOf(
        CanonicalCutoverAppSeamMode.DISABLED,
        CanonicalCutoverAppSeamMode.BLOCKED
    )
) {
    fun evaluateGate(
        requestedMode: CanonicalCutoverAppSeamMode,
        evidence: List<CanonicalRecordingMetadataNoCommitCandidateResult> = emptyList()
    ): CanonicalCutoverAppSeamGate {
        val failures = mutableListOf<CanonicalCutoverAppSeamFailure>()

        if (blockedModes.contains(requestedMode)) {
            when (requestedMode) {
                CanonicalCutoverAppSeamMode.DISABLED ->
                    failures.add(CanonicalCutoverAppSeamFailure.DISABLED_BY_DEFAULT)
                CanonicalCutoverAppSeamMode.BLOCKED ->
                    failures.add(CanonicalCutoverAppSeamFailure.PRODUCTION_ROOT_BLOCKED)
                else -> {}
            }
        }

        if (!allowedModes.contains(requestedMode)) {
            failures.add(CanonicalCutoverAppSeamFailure.UNSUPPORTED_TRIGGER)
        }

        if (requestedMode == CanonicalCutoverAppSeamMode.GUARDED_EXECUTE_COMMIT ||
            requestedMode == CanonicalCutoverAppSeamMode.CANARY_COMMIT
        ) {
            val canary = canaryPolicy
            if (canary != null && !canary.readyForCommit) {
                failures.add(CanonicalCutoverAppSeamFailure.MISSING_EVIDENCE)
            }
        }

        val hasFailures = failures.isNotEmpty()
        val hasEquivalenceFailures = evidence.isNotEmpty() &&
            evidence.all { it.failure != null }

        if (evidence.isEmpty() && requestedMode != CanonicalCutoverAppSeamMode.DISABLED) {
            failures.add(CanonicalCutoverAppSeamFailure.MISSING_EVIDENCE)
        }

        return CanonicalCutoverAppSeamGate(
            domain = domain,
            mode = requestedMode,
            allowed = !hasFailures && !hasEquivalenceFailures,
            failures = failures.distinct().sortedBy { it.rawValue }
        )
    }

    fun getEvidence(objectIDs: List<String>): List<CanonicalRecordingMetadataNoCommitCandidateResult> {
        if (evidenceCollection == null) return emptyList()
        return objectIDs.mapNotNull { objectID ->
            val entry = evidenceCollection.getEvidence(objectID) ?: return@mapNotNull null

            val candidate = CanonicalRecordingMetadataNoCommitEquivalenceCandidate(
                objectID = objectID,
                metadataHashPrefix = entry.hashPrefix,
                canonicalDirection = entry.direction,
                routePath = entry.routePath,
                blocking = entry.blocking,
                status = entry.status
            )

            val staging = if (candidate.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.EQUIVALENT) {
                CanonicalRecordingMetadataNoCommitCandidateStagingInfo(
                    stagingEvidence = CanonicalNoCommitStagingEvidence(
                        rootID = objectID,
                        rootKind = CanonicalNoCommitStagingRootKind.EXPLICIT_STAGING_ROOT,
                        lifecycleStatus = CanonicalNoCommitStagingRootLifecycleStatus.CREATED
                    ),
                    cleanupEvidence = null,
                    wouldApply = entry.direction == CanonicalRecordingMetadataNoCommitEquivalenceDirection.APPLY,
                    wouldSend = entry.direction == CanonicalRecordingMetadataNoCommitEquivalenceDirection.SEND
                )
            } else null

            val failure = when (candidate.status) {
                CanonicalRecordingMetadataNoCommitEquivalenceStatus.UNSUPPORTED ->
                    CanonicalRecordingMetadataNoCommitCandidateFailure.UNSUPPORTED_OBJECT_KIND
                CanonicalRecordingMetadataNoCommitEquivalenceStatus.INSUFFICIENT_EVIDENCE ->
                    CanonicalRecordingMetadataNoCommitCandidateFailure.INSUFFICIENT_METADATA
                CanonicalRecordingMetadataNoCommitEquivalenceStatus.DIVERGENT ->
                    CanonicalRecordingMetadataNoCommitCandidateFailure.HASH_MISMATCH
                else -> null
            }

            CanonicalRecordingMetadataNoCommitCandidateResult(
                equivalence = candidate,
                staging = staging,
                failure = failure
            )
        }
    }

    fun readinessReport(
        mode: CanonicalCutoverAppSeamMode,
        objectIDs: List<String>
    ): CanonicalRecordingMetadataRetirementReadiness {
        val evidence = getEvidence(objectIDs)
        val gate = evaluateGate(mode, evidence)

        val equivalentCount = evidence.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.EQUIVALENT
        }
        val divergentCount = evidence.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.DIVERGENT
        }
        val unsupportedCount = evidence.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.UNSUPPORTED
        }
        val insufficientCount = evidence.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.INSUFFICIENT_EVIDENCE
        }

        val report = listOf(
            "domain=${domain.rawValue}",
            "mode=${mode.rawValue}",
            "allowed=${gate.allowed}",
            "failures=${gate.failures.joinToString(",") { it.rawValue }}",
            "equivalent=$equivalentCount",
            "divergent=$divergentCount",
            "unsupported=$unsupportedCount",
            "insufficientEvidence=$insufficientCount",
            "total=${objectIDs.size}"
        ).joinToString("; ")

        return CanonicalRecordingMetadataRetirementReadiness(
            domain = domain,
            mode = mode,
            ready = gate.allowed && divergentCount == 0 && insufficientCount == 0,
            report = report,
            equivalentObjectCount = equivalentCount,
            divergentObjectCount = divergentCount,
            unsupportedObjectCount = unsupportedCount,
            insufficientEvidenceCount = insufficientCount,
            totalObjectCount = objectIDs.size
        )
    }
}

// ── CanonicalRecordingMetadataCanaryConfiguration ──

data class CanonicalRecordingMetadataCanaryConfiguration(
    val mode: CanonicalRecordingMetadataCanaryConfiguration.Mode = Mode.DISABLED,
    val maxCanaryCount: Int = 1,
    val minObservationHours: Double = 24.0,
    val canaryObjectIDs: List<String> = emptyList()
) {
    enum class Mode(val rawValue: String) {
        DISABLED("disabled"),
        DIAGNOSTICS_ONLY("diagnosticsOnly"),
        N1_CANARY("n1Canary");

        companion object {
            val allCases: List<Mode> = entries.toList()
        }
    }

    companion object {
        val DISABLED = CanonicalRecordingMetadataCanaryConfiguration()
    }
}

// ── CanonicalRecordingMetadataCanaryPolicy ──

data class CanonicalRecordingMetadataCanaryPolicy(
    val configuration: CanonicalRecordingMetadataCanaryConfiguration = CanonicalRecordingMetadataCanaryConfiguration.DISABLED,
    val observations: List<CanonicalRecordingMetadataCanaryObservation> = emptyList(),
    val readyForCommit: Boolean = false
) {
    val canaryActive: Boolean
        get() = configuration.mode != CanonicalRecordingMetadataCanaryConfiguration.Mode.DISABLED

    val observationHoursAccumulated: Double
        get() = observations.sumOf { it.observationHours }

    val allCanariesHealthy: Boolean
        get() = observations.isNotEmpty() && observations.all { it.healthy }
}

// ── CanonicalRecordingMetadataCanarySelector ──

class CanonicalRecordingMetadataCanarySelector(
    private val configuration: CanonicalRecordingMetadataCanaryConfiguration,
    private val candidatePool: List<String> = emptyList()
) {
    fun selectSingle(): String? {
        if (configuration.mode == CanonicalRecordingMetadataCanaryConfiguration.Mode.DISABLED) {
            return null
        }

        if (configuration.canaryObjectIDs.isNotEmpty()) {
            return configuration.canaryObjectIDs.firstOrNull()
        }

        if (candidatePool.isEmpty()) return null

        val excludedIDs = configuration.canaryObjectIDs.toSet()
        return candidatePool.firstOrNull { it !in excludedIDs }
            ?: candidatePool.firstOrNull()
    }
}

// ── CanonicalRecordingMetadataCanaryObservation ──

data class CanonicalRecordingMetadataCanaryObservation(
    val objectID: String,
    val observedAt: Date = Date(),
    val observationHours: Double = 0.0,
    val healthy: Boolean = true,
    val divergenceDetected: Boolean = false,
    val metadataConverged: Boolean = true,
    val detail: String? = null
) {
    val id: String get() = objectID
}

// ── CanonicalRecordingMetadataRetirementReadiness ──

data class CanonicalRecordingMetadataRetirementReadiness(
    val domain: CanonicalCutoverDomain = CanonicalCutoverDomain.RECORDING_METADATA,
    val mode: CanonicalCutoverAppSeamMode = CanonicalCutoverAppSeamMode.DISABLED,
    val ready: Boolean = false,
    val report: String = "",
    val equivalentObjectCount: Int = 0,
    val divergentObjectCount: Int = 0,
    val unsupportedObjectCount: Int = 0,
    val insufficientEvidenceCount: Int = 0,
    val totalObjectCount: Int = 0
)

// ── CanonicalRecordingMetadataUIProjection ──

data class CanonicalRecordingMetadataUIProjection(
    val objectID: String,
    val recordingTitle: String? = null,
    val durationSeconds: Double? = null,
    val createdAt: Date? = null,
    val modifiedAt: Date? = null,
    val metadataSource: String = "canonical",
    val cutoverDomain: CanonicalCutoverDomain = CanonicalCutoverDomain.RECORDING_METADATA,
    val projectionStatus: String = "active",
    val tombstone: Boolean = false
)
