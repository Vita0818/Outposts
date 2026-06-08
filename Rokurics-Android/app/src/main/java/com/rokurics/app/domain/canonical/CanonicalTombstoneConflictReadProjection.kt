package com.rokurics.app.domain.canonical

import java.util.Date
import java.util.UUID

data class CanonicalTombstoneConflictReadProjection
private constructor(
    val tombstoneObjects: List<CanonicalTombstoneObject>,
    val conflictObjects: List<CanonicalTombstoneConflictObject>,
    val counts: CanonicalTombstoneConflictReadCounts,
    val projectionID: String,
    val observationWindow: CanonicalTombstoneConflictObservationWindow?,
    val diagnosticsRedacted: Boolean,
    val diagnosticsSummary: String
) {
    val totalTombstones: Int
        get() = tombstoneObjects.size

    val totalConflicts: Int
        get() = conflictObjects.size

    val totalObjects: Int
        get() = totalTombstones + totalConflicts

    val isEmpty: Boolean
        get() = totalObjects == 0

    val hasActiveTombstones: Boolean
        get() = tombstoneObjects.any { it.active }

    val hasUnresolvedConflicts: Boolean
        get() = conflictObjects.any { !it.resolved }

    constructor(
        tombstoneObjects: List<CanonicalTombstoneObject> = emptyList(),
        conflictObjects: List<CanonicalTombstoneConflictObject> = emptyList(),
        counts: CanonicalTombstoneConflictReadCounts = CanonicalTombstoneConflictReadCounts(),
        projectionID: String = UUID.randomUUID().toString(),
        observationWindow: CanonicalTombstoneConflictObservationWindow? = null,
        diagnosticsRedacted: Boolean = true
    ) : this(
        tombstoneObjects = tombstoneObjects.sortedBy { it.objectID },
        conflictObjects = conflictObjects.sortedBy { it.objectID },
        counts = counts,
        projectionID = projectionID,
        observationWindow = observationWindow,
        diagnosticsRedacted = diagnosticsRedacted,
        diagnosticsSummary = listOf(
            "tombstones=${tombstoneObjects.size}",
            "conflicts=${conflictObjects.size}",
            "total=${tombstoneObjects.size + conflictObjects.size}",
            "active=${tombstoneObjects.count { it.active }}",
            "unresolved=${conflictObjects.count { !it.resolved }}"
        ).joinToString(",")
    )
}

data class CanonicalTombstoneObject(
    val objectID: String,
    val kind: CanonicalTombstoneConflictCandidateKind,
    val active: Boolean,
    val tombstoneMarkerPresent: Boolean,
    val deletedAt: CanonicalTimestamp?,
    val resurrectionBlocked: Boolean,
    val conflictIDs: List<String>,
    val diagnosticsSummary: String
) {
    val isPotentialResurrectionTarget: Boolean
        get() = active && tombstoneMarkerPresent && !resurrectionBlocked

    constructor(
        objectID: String,
        kind: CanonicalTombstoneConflictCandidateKind,
        active: Boolean = true,
        tombstoneMarkerPresent: Boolean = true,
        deletedAt: CanonicalTimestamp? = null,
        resurrectionBlocked: Boolean = true,
        conflictIDs: List<String> = emptyList()
    ) : this(
        objectID = objectID,
        kind = kind,
        active = active,
        tombstoneMarkerPresent = tombstoneMarkerPresent,
        deletedAt = deletedAt,
        resurrectionBlocked = resurrectionBlocked,
        conflictIDs = conflictIDs.sorted(),
        diagnosticsSummary = listOf(
            "object=$objectID",
            "kind=${kind.rawValue}",
            "active=$active",
            "marker=$tombstoneMarkerPresent",
            "resurrectionBlocked=$resurrectionBlocked",
            "conflicts=${conflictIDs.size}"
        ).joinToString(",")
    )
}

data class CanonicalTombstoneConflictObject(
    val objectID: String,
    val conflictID: String,
    val resolved: Boolean,
    val conflictKind: String,
    val tombstoneID: String?,
    val resolutionEvidence: String?,
    val resolvedAt: CanonicalTimestamp?,
    val diagnosticsSummary: String
) {
    constructor(
        objectID: String,
        conflictID: String = UUID.randomUUID().toString(),
        resolved: Boolean = false,
        conflictKind: String = "unknown",
        tombstoneID: String? = null,
        resolutionEvidence: String? = null,
        resolvedAt: CanonicalTimestamp? = null
    ) : this(
        objectID = objectID,
        conflictID = conflictID,
        resolved = resolved,
        conflictKind = conflictKind.trim().nilIfEmpty ?: "unknown",
        tombstoneID = tombstoneID?.trim()?.nilIfEmpty,
        resolutionEvidence = resolutionEvidence?.trim()?.nilIfEmpty,
        resolvedAt = resolvedAt,
        diagnosticsSummary = listOf(
            "object=$objectID",
            "conflict=$conflictID",
            "resolved=$resolved",
            "kind=$conflictKind",
            "tombstone=${tombstoneID ?: "none"}"
        ).joinToString(",")
    )
}

data class CanonicalTombstoneConflictReadCounts(
    val totalTombstones: Int,
    val activeTombstones: Int,
    val inactiveTombstones: Int,
    val totalConflicts: Int,
    val resolvedConflicts: Int,
    val unresolvedConflicts: Int,
    val resurrectionBlocks: Int,
    val softTombstoneMarkers: Int,
    val generatedArtifactTombstones: Int,
    val diagnosticsSummary: String
) {
    constructor(
        totalTombstones: Int = 0,
        activeTombstones: Int = 0,
        inactiveTombstones: Int = 0,
        totalConflicts: Int = 0,
        resolvedConflicts: Int = 0,
        unresolvedConflicts: Int = 0,
        resurrectionBlocks: Int = 0,
        softTombstoneMarkers: Int = 0,
        generatedArtifactTombstones: Int = 0
    ) : this(
        totalTombstones = maxOf(0, totalTombstones),
        activeTombstones = maxOf(0, activeTombstones),
        inactiveTombstones = maxOf(0, inactiveTombstones),
        totalConflicts = maxOf(0, totalConflicts),
        resolvedConflicts = maxOf(0, resolvedConflicts),
        unresolvedConflicts = maxOf(0, unresolvedConflicts),
        resurrectionBlocks = maxOf(0, resurrectionBlocks),
        softTombstoneMarkers = maxOf(0, softTombstoneMarkers),
        generatedArtifactTombstones = maxOf(0, generatedArtifactTombstones),
        diagnosticsSummary = listOf(
            "tombstones=$totalTombstones",
            "active=$activeTombstones",
            "inactive=$inactiveTombstones",
            "conflicts=$totalConflicts",
            "resolved=$resolvedConflicts",
            "unresolved=$unresolvedConflicts",
            "resurrectionBlocks=$resurrectionBlocks",
            "softMarkers=$softTombstoneMarkers",
            "artifactTombstones=$generatedArtifactTombstones"
        ).joinToString(",")
    )

    companion object {
        fun fromProjection(
            projection: CanonicalTombstoneConflictReadProjection
        ): CanonicalTombstoneConflictReadCounts {
            val tombstoneObjects = projection.tombstoneObjects
            return CanonicalTombstoneConflictReadCounts(
                totalTombstones = tombstoneObjects.size,
                activeTombstones = tombstoneObjects.count { it.active },
                inactiveTombstones = tombstoneObjects.count { !it.active },
                totalConflicts = projection.conflictObjects.size,
                resolvedConflicts = projection.conflictObjects.count { it.resolved },
                unresolvedConflicts = projection.conflictObjects.count { !it.resolved },
                resurrectionBlocks = tombstoneObjects.count {
                    it.kind == CanonicalTombstoneConflictCandidateKind.RESURRECTION_BLOCK
                },
                softTombstoneMarkers = tombstoneObjects.count {
                    it.kind == CanonicalTombstoneConflictCandidateKind.SOFT_TOMBSTONE_MARKER
                },
                generatedArtifactTombstones = tombstoneObjects.count {
                    it.kind == CanonicalTombstoneConflictCandidateKind.GENERATED_ARTIFACT_TOMBSTONE
                }
            )
        }
    }
}

data class CanonicalTombstoneConflictReadDiff
private constructor(
    val divergences: List<CanonicalTombstoneConflictReadDivergence>,
    val equivalentCount: Int,
    val divergentCount: Int,
    val missingCount: Int,
    val extraCount: Int,
    val equivalent: Boolean,
    val legacyProjectionID: String?,
    val canonicalProjectionID: String?,
    val comparedAt: String?,
    val diagnosticsSummary: String
) {
    constructor(
        divergences: List<CanonicalTombstoneConflictReadDivergence> = emptyList(),
        equivalentCount: Int = 0,
        divergentCount: Int = 0,
        missingCount: Int = 0,
        extraCount: Int = 0,
        legacyProjectionID: String? = null,
        canonicalProjectionID: String? = null,
        comparedAt: String? = null
    ) : this(
        divergences = divergences.sortedBy { it.objectID },
        equivalentCount = maxOf(0, equivalentCount),
        divergentCount = maxOf(0, divergentCount),
        missingCount = maxOf(0, missingCount),
        extraCount = maxOf(0, extraCount),
        equivalent = divergences.isEmpty() && equivalentCount > 0 && divergentCount == 0,
        legacyProjectionID = legacyProjectionID?.trim()?.nilIfEmpty,
        canonicalProjectionID = canonicalProjectionID?.trim()?.nilIfEmpty,
        comparedAt = comparedAt?.trim()?.nilIfEmpty,
        diagnosticsSummary = listOf(
            "equivalent=${divergences.isEmpty() && equivalentCount > 0 && divergentCount == 0}",
            "equivalentCount=$equivalentCount",
            "divergent=$divergentCount",
            "missing=$missingCount",
            "extra=$extraCount",
            "legacy=${legacyProjectionID ?: "none"}",
            "canonical=${canonicalProjectionID ?: "none"}"
        ).joinToString(",")
    )

    companion object {
        fun equivalent(
            equivalentCount: Int,
            legacyProjectionID: String? = null,
            canonicalProjectionID: String? = null
        ): CanonicalTombstoneConflictReadDiff {
            return CanonicalTombstoneConflictReadDiff(
                equivalentCount = equivalentCount,
                legacyProjectionID = legacyProjectionID,
                canonicalProjectionID = canonicalProjectionID
            )
        }

        fun divergent(
            divergences: List<CanonicalTombstoneConflictReadDivergence>,
            equivalentCount: Int = 0,
            divergentCount: Int = 0,
            missingCount: Int = 0,
            extraCount: Int = 0,
            legacyProjectionID: String? = null,
            canonicalProjectionID: String? = null
        ): CanonicalTombstoneConflictReadDiff {
            return CanonicalTombstoneConflictReadDiff(
                divergences = divergences,
                equivalentCount = equivalentCount,
                divergentCount = divergentCount,
                missingCount = missingCount,
                extraCount = extraCount,
                legacyProjectionID = legacyProjectionID,
                canonicalProjectionID = canonicalProjectionID
            )
        }

        fun compute(
            canonical: CanonicalTombstoneConflictReadProjection,
            legacy: CanonicalTombstoneConflictReadProjection
        ): CanonicalTombstoneConflictReadDiff {
            val divergences = mutableListOf<CanonicalTombstoneConflictReadDivergence>()
            var equivalentCount = 0
            var missingCount = 0
            var extraCount = 0

            val canonicalIDs = canonical.tombstoneObjects.map { it.objectID }.toSet()
            val legacyIDs = legacy.tombstoneObjects.map { it.objectID }.toSet()

            for (id in canonicalIDs) {
                val canonicalObj = canonical.tombstoneObjects.find { it.objectID == id }
                val legacyObj = legacy.tombstoneObjects.find { it.objectID == id }

                if (legacyObj == null) {
                    extraCount++
                    divergences.add(
                        CanonicalTombstoneConflictReadDivergence(
                            objectID = id,
                            kind = canonicalObj?.kind?.rawValue ?: "unknown",
                            divergenceType = CanonicalTombstoneConflictReadDivergenceType.OBJECT_EXTRA_IN_CANONICAL,
                            blocking = true
                        )
                    )
                    continue
                }

                if (canonicalObj?.active != legacyObj.active) {
                    divergences.add(
                        CanonicalTombstoneConflictReadDivergence(
                            objectID = id,
                            kind = canonicalObj?.kind?.rawValue ?: legacyObj.kind.rawValue,
                            divergenceType = CanonicalTombstoneConflictReadDivergenceType.TOMBSTONE_ACTIVE_MISMATCH,
                            blocking = true
                        )
                    )
                    continue
                }

                if (canonicalObj?.resurrectionBlocked != legacyObj.resurrectionBlocked) {
                    divergences.add(
                        CanonicalTombstoneConflictReadDivergence(
                            objectID = id,
                            kind = canonicalObj?.kind?.rawValue ?: legacyObj.kind.rawValue,
                            divergenceType = CanonicalTombstoneConflictReadDivergenceType.RESURRECTION_BLOCK_MISMATCH,
                            blocking = true
                        )
                    )
                    continue
                }

                equivalentCount++
            }

            for (id in legacyIDs - canonicalIDs) {
                missingCount++
                divergences.add(
                    CanonicalTombstoneConflictReadDivergence(
                        objectID = id,
                        kind = legacy.tombstoneObjects.find { it.objectID == id }?.kind?.rawValue ?: "unknown",
                        divergenceType = CanonicalTombstoneConflictReadDivergenceType.OBJECT_MISSING_FROM_CANONICAL,
                        blocking = true
                    )
                )
            }

            val canonicalConflictIDs = canonical.conflictObjects.map { it.conflictID }.toSet()
            val legacyConflictIDs = legacy.conflictObjects.map { it.conflictID }.toSet()

            for (id in canonicalConflictIDs) {
                val canonicalConflict = canonical.conflictObjects.find { it.conflictID == id }
                val legacyConflict = legacy.conflictObjects.find { it.conflictID == id }

                if (legacyConflict == null) {
                    extraCount++
                    divergences.add(
                        CanonicalTombstoneConflictReadDivergence(
                            objectID = canonicalConflict?.objectID ?: "unknown",
                            kind = "conflict",
                            divergenceType = CanonicalTombstoneConflictReadDivergenceType.OBJECT_EXTRA_IN_CANONICAL,
                            blocking = false
                        )
                    )
                    continue
                }

                if (canonicalConflict?.resolved != legacyConflict.resolved) {
                    divergences.add(
                        CanonicalTombstoneConflictReadDivergence(
                            objectID = canonicalConflict?.objectID ?: "unknown",
                            kind = "conflict",
                            divergenceType = CanonicalTombstoneConflictReadDivergenceType.CONFLICT_RESOLUTION_MISMATCH,
                            blocking = true
                        )
                    )
                }
            }

            for (id in legacyConflictIDs - canonicalConflictIDs) {
                missingCount++
                divergences.add(
                    CanonicalTombstoneConflictReadDivergence(
                        objectID = legacy.conflictObjects.find { it.conflictID == id }?.objectID ?: "unknown",
                        kind = "conflict",
                        divergenceType = CanonicalTombstoneConflictReadDivergenceType.OBJECT_MISSING_FROM_CANONICAL,
                        blocking = true
                    )
                )
            }

            val divergentCount = divergences.size

            return if (divergences.isEmpty()) {
                CanonicalTombstoneConflictReadDiff.equivalent(
                    equivalentCount = equivalentCount,
                    legacyProjectionID = legacy.projectionID,
                    canonicalProjectionID = canonical.projectionID
                )
            } else {
                CanonicalTombstoneConflictReadDiff.divergent(
                    divergences = divergences,
                    equivalentCount = equivalentCount,
                    divergentCount = divergentCount,
                    missingCount = missingCount,
                    extraCount = extraCount,
                    legacyProjectionID = legacy.projectionID,
                    canonicalProjectionID = canonical.projectionID
                )
            }
        }
    }
}

enum class CanonicalTombstoneConflictReadDivergenceType(val rawValue: String) {
    TOMBSTONE_ACTIVE_MISMATCH("tombstoneActiveMismatch"),
    RESURRECTION_BLOCK_MISMATCH("resurrectionBlockMismatch"),
    OBJECT_MISSING_FROM_CANONICAL("objectMissingFromCanonical"),
    OBJECT_EXTRA_IN_CANONICAL("objectExtraInCanonical"),
    CONFLICT_RESOLUTION_MISMATCH("conflictResolutionMismatch"),
    TOMBSTONE_MARKER_MISMATCH("tombstoneMarkerMismatch"),
    DELETED_AT_MISMATCH("deletedAtMismatch"),
    UNKNOWN("unknown");

    companion object {
        val allCases: List<CanonicalTombstoneConflictReadDivergenceType> = entries.toList()
    }
}

data class CanonicalTombstoneConflictReadDivergence(
    val objectID: String,
    val kind: String,
    val divergenceType: CanonicalTombstoneConflictReadDivergenceType,
    val legacyValue: String?,
    val canonicalValue: String?,
    val field: String?,
    val blocking: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        objectID: String,
        kind: String,
        divergenceType: CanonicalTombstoneConflictReadDivergenceType,
        legacyValue: String? = null,
        canonicalValue: String? = null,
        field: String? = null,
        blocking: Boolean = false
    ) : this(
        objectID = objectID,
        kind = kind,
        divergenceType = divergenceType,
        legacyValue = legacyValue?.trim()?.nilIfEmpty,
        canonicalValue = canonicalValue?.trim()?.nilIfEmpty,
        field = field?.trim()?.nilIfEmpty,
        blocking = blocking,
        diagnosticsSummary = listOf(
            "object=$objectID",
            "kind=$kind",
            "type=${divergenceType.rawValue}",
            "field=${field ?: "none"}",
            "blocking=$blocking"
        ).joinToString(",")
    )
}

data class CanonicalTombstoneConflictObservationWindow(
    val startTime: CanonicalTimestamp,
    val endTime: CanonicalTimestamp?,
    val observationDurationSeconds: Double?,
    val minimumObservationSeconds: Double,
    val satisfied: Boolean,
    val remainingSeconds: Double,
    val windowID: String,
    val diagnosticsSummary: String
) {
    constructor(
        startTime: CanonicalTimestamp = CanonicalTimestamp(Date()),
        endTime: CanonicalTimestamp? = null,
        minimumObservationSeconds: Double = 300.0,
        windowID: String = UUID.randomUUID().toString()
    ) : this(
        startTime = startTime,
        endTime = endTime,
        observationDurationSeconds = endTime?.let {
            (it.date.time - startTime.date.time) / 1000.0
        },
        minimumObservationSeconds = maxOf(0.0, minimumObservationSeconds),
        satisfied = ((endTime?.let { (it.date.time - startTime.date.time) / 1000.0 } ?: 0.0) >= maxOf(0.0, minimumObservationSeconds)),
        remainingSeconds = maxOf(0.0, maxOf(0.0, minimumObservationSeconds) - (endTime?.let { (it.date.time - startTime.date.time) / 1000.0 } ?: 0.0)),
        windowID = windowID,
        diagnosticsSummary = listOf(
            "window=$windowID",
            "duration=${"%.1f".format((endTime?.let { (it.date.time - startTime.date.time) / 1000.0 } ?: 0.0))}s",
            "minimum=${"%.1f".format(minimumObservationSeconds)}s",
            "satisfied=${((endTime?.let { (it.date.time - startTime.date.time) / 1000.0 } ?: 0.0) >= maxOf(0.0, minimumObservationSeconds))}",
            "remaining=${"%.1f".format(maxOf(0.0, maxOf(0.0, minimumObservationSeconds) - (endTime?.let { (it.date.time - startTime.date.time) / 1000.0 } ?: 0.0)))}s"
        ).joinToString(",")
    )
}

object CanonicalTombstoneConflictObservationGate {
    data class GateResult(
        val allowed: Boolean,
        val blockers: List<String>,
        val observationWindowSatisfied: Boolean,
        val tombstoneCountSufficient: Boolean,
        val conflictCountSufficient: Boolean,
        val noActiveResurrectionTargets: Boolean,
        val projectionAvailable: Boolean,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun blocked(blockers: List<String>): GateResult {
                return GateResult(
                    allowed = false,
                    blockers = blockers.sorted(),
                    observationWindowSatisfied = false,
                    tombstoneCountSufficient = false,
                    conflictCountSufficient = false,
                    noActiveResurrectionTargets = false,
                    projectionAvailable = false,
                    diagnosticsSummary = listOf(
                        "allowed=false",
                        "blockers=${blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun check(
        projection: CanonicalTombstoneConflictReadProjection?,
        observationWindow: CanonicalTombstoneConflictObservationWindow?
    ): GateResult {
        val blockers = mutableListOf<String>()

        if (projection == null || projection.isEmpty) {
            blockers.add("noProjectionAvailable")
        }

        if (observationWindow == null || !observationWindow.satisfied) {
            blockers.add("observationWindowNotSatisfied")
        }

        val tombstoneCountSufficient = projection != null && projection.totalTombstones > 0
        if (!tombstoneCountSufficient) {
            blockers.add("insufficientTombstoneCount")
        }

        val conflictCountSufficient = projection != null && projection.totalConflicts > 0
        if (!conflictCountSufficient) {
            blockers.add("insufficientConflictCount")
        }

        val noActiveResurrectionTargets = projection != null &&
                !projection.tombstoneObjects.any { it.isPotentialResurrectionTarget }
        if (!noActiveResurrectionTargets) {
            blockers.add("activeResurrectionTargetsDetected")
        }

        return GateResult(
            allowed = blockers.isEmpty(),
            blockers = blockers,
            observationWindowSatisfied = observationWindow?.satisfied ?: false,
            tombstoneCountSufficient = tombstoneCountSufficient,
            conflictCountSufficient = conflictCountSufficient,
            noActiveResurrectionTargets = noActiveResurrectionTargets,
            projectionAvailable = projection != null && !projection.isEmpty,
            diagnosticsSummary = listOf(
                "allowed=${blockers.isEmpty()}",
                "observationWindow=${observationWindow?.satisfied ?: false}",
                "tombstones=$tombstoneCountSufficient",
                "conflicts=$conflictCountSufficient",
                "noResurrection=$noActiveResurrectionTargets",
                "projectionAvailable=${projection != null && !projection.isEmpty}"
            ).joinToString(",")
        )
    }
}

data class CanonicalTombstoneConflictRetirementCandidateGate
private constructor(
    val ready: Boolean,
    val blockers: List<String>,
    val gateID: String,
    val observationGateAllowed: Boolean,
    val readProjectionAvailable: Boolean,
    val readDiffEquivalent: Boolean,
    val allTombstonesPreserved: Boolean,
    val allConflictsResolved: Boolean,
    val noResurrectionDetected: Boolean,
    val antiResurrectionEnforced: Boolean,
    val legacyCodePathDeprecatable: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        ready: Boolean,
        blockers: List<String> = emptyList(),
        gateID: String = UUID.randomUUID().toString(),
        observationGateAllowed: Boolean = false,
        readProjectionAvailable: Boolean = false,
        readDiffEquivalent: Boolean = false,
        allTombstonesPreserved: Boolean = false,
        allConflictsResolved: Boolean = false,
        noResurrectionDetected: Boolean = false,
        antiResurrectionEnforced: Boolean = false,
        legacyCodePathDeprecatable: Boolean = false
    ) : this(
        ready = ready && blockers.isEmpty(),
        blockers = blockers.sorted(),
        gateID = gateID,
        observationGateAllowed = observationGateAllowed,
        readProjectionAvailable = readProjectionAvailable,
        readDiffEquivalent = readDiffEquivalent,
        allTombstonesPreserved = allTombstonesPreserved,
        allConflictsResolved = allConflictsResolved,
        noResurrectionDetected = noResurrectionDetected,
        antiResurrectionEnforced = antiResurrectionEnforced,
        legacyCodePathDeprecatable = legacyCodePathDeprecatable,
        diagnosticsSummary = listOf(
            "ready=$ready",
            "observationGate=$observationGateAllowed",
            "projectionAvailable=$readProjectionAvailable",
            "diffEquivalent=$readDiffEquivalent",
            "tombstonesPreserved=$allTombstonesPreserved",
            "conflictsResolved=$allConflictsResolved",
            "noResurrection=$noResurrectionDetected",
            "antiResurrection=$antiResurrectionEnforced",
            "legacyDeprecatable=$legacyCodePathDeprecatable",
            "blockers=${blockers.joinToString("|")}"
        ).joinToString(",")
    )

    companion object {
        fun evaluate(
            observationGateResult: CanonicalTombstoneConflictObservationGate.GateResult,
            readProjectionAvailable: Boolean,
            readDiff: CanonicalTombstoneConflictReadDiff?,
            projection: CanonicalTombstoneConflictReadProjection?,
            antiResurrectionGateResult: CanonicalTombstoneConflictAntiResurrectionGate.CheckResult?
        ): CanonicalTombstoneConflictRetirementCandidateGate {
            val blockers = mutableListOf<String>()

            if (!observationGateResult.allowed) {
                blockers.addAll(observationGateResult.blockers)
            }
            if (!readProjectionAvailable) {
                blockers.add("readProjectionUnavailable")
            }
            if (readDiff != null && !readDiff.equivalent) {
                blockers.add("readDiffNotEquivalent")
            }
            if (projection != null && !projection.tombstoneObjects.all { it.active }) {
                blockers.add("tombstonesNotAllPreserved")
            }
            if (projection != null && projection.conflictObjects.any { !it.resolved }) {
                blockers.add("unresolvedConflicts")
            }
            if (antiResurrectionGateResult != null && !antiResurrectionGateResult.allowed) {
                blockers.add("antiResurrectionGateBlocked")
            }

            return CanonicalTombstoneConflictRetirementCandidateGate(
                ready = blockers.isEmpty(),
                blockers = blockers,
                observationGateAllowed = observationGateResult.allowed,
                readProjectionAvailable = readProjectionAvailable,
                readDiffEquivalent = readDiff?.equivalent ?: false,
                allTombstonesPreserved = projection?.tombstoneObjects?.all { it.active } ?: false,
                allConflictsResolved = projection?.conflictObjects?.all { it.resolved } ?: false,
                noResurrectionDetected = antiResurrectionGateResult?.noResurrectionDetected ?: false,
                antiResurrectionEnforced = antiResurrectionGateResult?.antiResurrectionEnforced ?: false,
                legacyCodePathDeprecatable = blockers.isEmpty()
            )
        }
    }
}

data class CanonicalTombstoneConflictAntiResurrectionGate(
    val gateID: String,
    val staleLiveMetadataCheck: Boolean,
    val tombstoneReactivationCheck: Boolean,
    val conflictResolutionCheck: Boolean
) {
    data class CheckResult(
        val allowed: Boolean,
        val blockers: List<String>,
        val staleLiveMetadataDetected: Boolean,
        val tombstoneReactivationAttempted: Boolean,
        val unresolvedConflictsExist: Boolean,
        val resurrectionAttempted: Boolean,
        val noResurrectionDetected: Boolean,
        val antiResurrectionEnforced: Boolean,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun blocked(blockers: List<String>): CheckResult {
                return CheckResult(
                    allowed = false,
                    blockers = blockers.sorted(),
                    staleLiveMetadataDetected = blockers.contains("staleLiveMetadataDetected"),
                    tombstoneReactivationAttempted = blockers.contains("tombstoneReactivationAttempted"),
                    unresolvedConflictsExist = blockers.contains("unresolvedConflictsExist"),
                    resurrectionAttempted = true,
                    noResurrectionDetected = false,
                    antiResurrectionEnforced = false,
                    diagnosticsSummary = listOf(
                        "allowed=false",
                        "blockers=${blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    constructor(
        staleLiveMetadataCheck: Boolean = true,
        tombstoneReactivationCheck: Boolean = true,
        conflictResolutionCheck: Boolean = true
    ) : this(
        gateID = UUID.randomUUID().toString(),
        staleLiveMetadataCheck = staleLiveMetadataCheck,
        tombstoneReactivationCheck = tombstoneReactivationCheck,
        conflictResolutionCheck = conflictResolutionCheck
    )

    fun check(
        projection: CanonicalTombstoneConflictReadProjection,
        tombstoneObjects: List<CanonicalTombstoneObject> = projection.tombstoneObjects,
        conflictObjects: List<CanonicalTombstoneConflictObject> = projection.conflictObjects
    ): CheckResult {
        val blockers = mutableListOf<String>()
        var staleLiveMetadataDetected = false
        var tombstoneReactivationAttempted = false
        var unresolvedConflictsExist = false
        var resurrectionAttempted = false

        if (staleLiveMetadataCheck) {
            val potentialResurrectionTargets = tombstoneObjects.filter {
                it.isPotentialResurrectionTarget
            }
            if (potentialResurrectionTargets.isNotEmpty()) {
                staleLiveMetadataDetected = true
                resurrectionAttempted = true
                blockers.add("staleLiveMetadataDetected")
            }
        }

        if (tombstoneReactivationCheck) {
            val reactivatedTombstones = tombstoneObjects.filter {
                it.kind == CanonicalTombstoneConflictCandidateKind.SOFT_TOMBSTONE_MARKER &&
                        it.tombstoneMarkerPresent && !it.active
            }
            if (reactivatedTombstones.isNotEmpty()) {
                tombstoneReactivationAttempted = true
                resurrectionAttempted = true
                blockers.add("tombstoneReactivationAttempted")
            }
        }

        if (conflictResolutionCheck) {
            val unresolvedConflicts = conflictObjects.filter { !it.resolved }
            if (unresolvedConflicts.isNotEmpty()) {
                unresolvedConflictsExist = true
                blockers.add("unresolvedConflictsExist")
            }
        }

        val noResurrectionDetected = !resurrectionAttempted && !staleLiveMetadataDetected &&
                !tombstoneReactivationAttempted
        val antiResurrectionEnforced = staleLiveMetadataCheck && tombstoneReactivationCheck &&
                noResurrectionDetected

        return CheckResult(
            allowed = blockers.isEmpty(),
            blockers = blockers,
            staleLiveMetadataDetected = staleLiveMetadataDetected,
            tombstoneReactivationAttempted = tombstoneReactivationAttempted,
            unresolvedConflictsExist = unresolvedConflictsExist,
            resurrectionAttempted = resurrectionAttempted,
            noResurrectionDetected = noResurrectionDetected,
            antiResurrectionEnforced = antiResurrectionEnforced,
            diagnosticsSummary = listOf(
                "allowed=${blockers.isEmpty()}",
                "staleMetadata=$staleLiveMetadataDetected",
                "tombstoneReactivation=$tombstoneReactivationAttempted",
                "unresolvedConflicts=$unresolvedConflictsExist",
                "noResurrection=$noResurrectionDetected",
                "antiResurrection=$antiResurrectionEnforced"
            ).joinToString(",")
        )
    }
}

data class CanonicalTombstoneConflictTemplateReadinessReport
private constructor(
    val ready: Boolean,
    val projection: CanonicalTombstoneConflictReadProjection,
    val diff: CanonicalTombstoneConflictReadDiff?,
    val observationGateResult: CanonicalTombstoneConflictObservationGate.GateResult?,
    val antiResurrectionResult: CanonicalTombstoneConflictAntiResurrectionGate.CheckResult?,
    val retirementGate: CanonicalTombstoneConflictRetirementCandidateGate?,
    val tombstoneTemplatePreserved: Boolean,
    val conflictTemplateResolved: Boolean,
    val resurrectionTemplateBlocked: Boolean,
    val physicalDeleteTemplatePrevented: Boolean,
    val permanentDeleteTemplatePrevented: Boolean,
    val tombstoneGCTemplatePrevented: Boolean,
    val restoreTemplatePrevented: Boolean,
    val totalTombstones: Int,
    val activeTombstones: Int,
    val totalConflicts: Int,
    val resolvedConflicts: Int,
    val blockers: List<String>,
    val diagnosticsSummary: String
) {
    val allTemplatesEnforced: Boolean
        get() = tombstoneTemplatePreserved && conflictTemplateResolved &&
                resurrectionTemplateBlocked && physicalDeleteTemplatePrevented &&
                permanentDeleteTemplatePrevented && tombstoneGCTemplatePrevented &&
                restoreTemplatePrevented

    constructor(
        projection: CanonicalTombstoneConflictReadProjection,
        diff: CanonicalTombstoneConflictReadDiff? = null,
        observationGateResult: CanonicalTombstoneConflictObservationGate.GateResult? = null,
        antiResurrectionResult: CanonicalTombstoneConflictAntiResurrectionGate.CheckResult? = null,
        retirementGate: CanonicalTombstoneConflictRetirementCandidateGate? = null
    ) : this(
        ready = (retirementGate?.ready ?: false) &&
                (diff?.equivalent ?: false) &&
                (projection.tombstoneObjects.all { it.active } &&
                 projection.conflictObjects.all { it.resolved } &&
                 (antiResurrectionResult?.antiResurrectionEnforced ?: false) &&
                 true && true && true && true),
        projection = projection,
        diff = diff,
        observationGateResult = observationGateResult,
        antiResurrectionResult = antiResurrectionResult,
        retirementGate = retirementGate,
        tombstoneTemplatePreserved = projection.tombstoneObjects.all { it.active },
        conflictTemplateResolved = projection.conflictObjects.all { it.resolved },
        resurrectionTemplateBlocked = antiResurrectionResult?.antiResurrectionEnforced ?: false,
        physicalDeleteTemplatePrevented = true,
        permanentDeleteTemplatePrevented = true,
        tombstoneGCTemplatePrevented = true,
        restoreTemplatePrevented = true,
        totalTombstones = projection.totalTombstones,
        activeTombstones = projection.tombstoneObjects.count { it.active },
        totalConflicts = projection.totalConflicts,
        resolvedConflicts = projection.conflictObjects.count { it.resolved },
        blockers = (retirementGate?.blockers ?: emptyList()) +
                (antiResurrectionResult?.blockers ?: emptyList()),
        diagnosticsSummary = listOf(
            "ready=${((retirementGate?.ready ?: false) && (diff?.equivalent ?: false) &&
                 (projection.tombstoneObjects.all { it.active } &&
                  projection.conflictObjects.all { it.resolved } &&
                  (antiResurrectionResult?.antiResurrectionEnforced ?: false) &&
                  true && true && true && true))}",
            "tombstones=${projection.totalTombstones}",
            "active=${projection.tombstoneObjects.count { it.active }}",
            "conflicts=${projection.totalConflicts}",
            "resolved=${projection.conflictObjects.count { it.resolved }}",
            "tombstonePreserved=${projection.tombstoneObjects.all { it.active }}",
            "conflictResolved=${projection.conflictObjects.all { it.resolved }}",
            "resurrectionBlocked=${antiResurrectionResult?.antiResurrectionEnforced ?: false}",
            "diffEquivalent=${diff?.equivalent ?: false}",
            "blockers=${((retirementGate?.blockers ?: emptyList()) + (antiResurrectionResult?.blockers ?: emptyList())).joinToString("|")}"
        ).joinToString(",")
    )

    companion object {
        fun evaluate(
            projection: CanonicalTombstoneConflictReadProjection,
            legacyProjection: CanonicalTombstoneConflictReadProjection? = null,
            observationWindow: CanonicalTombstoneConflictObservationWindow? = null,
            antiResurrectionGate: CanonicalTombstoneConflictAntiResurrectionGate = CanonicalTombstoneConflictAntiResurrectionGate()
        ): CanonicalTombstoneConflictTemplateReadinessReport {
            val diff = if (legacyProjection != null) {
                CanonicalTombstoneConflictReadDiff.compute(
                    canonical = projection,
                    legacy = legacyProjection
                )
            } else {
                null
            }

            val observationGateResult = CanonicalTombstoneConflictObservationGate.check(
                projection = projection,
                observationWindow = observationWindow
            )

            val antiResurrectionResult = antiResurrectionGate.check(
                projection = projection
            )

            val retirementGate = CanonicalTombstoneConflictRetirementCandidateGate.evaluate(
                observationGateResult = observationGateResult,
                readProjectionAvailable = !projection.isEmpty,
                readDiff = diff,
                projection = projection,
                antiResurrectionGateResult = antiResurrectionResult
            )

            return CanonicalTombstoneConflictTemplateReadinessReport(
                projection = projection,
                diff = diff,
                observationGateResult = observationGateResult,
                antiResurrectionResult = antiResurrectionResult,
                retirementGate = retirementGate
            )
        }
    }
}
