package com.rokurics.app.domain.canonical

import java.util.Date
import java.util.UUID

data class CanonicalLibraryMetadataObservationWindow
private constructor(
    val windowOpen: Boolean,
    val minObservationPeriod: Double,
    val requiredEvidenceCount: Int
) {
    companion object {
        operator fun invoke(
            windowOpen: Boolean = true,
            minObservationPeriod: Double = 3600.0,
            requiredEvidenceCount: Int = 1
        ): CanonicalLibraryMetadataObservationWindow {
            return CanonicalLibraryMetadataObservationWindow(
                windowOpen = windowOpen,
                minObservationPeriod = maxOf(0.0, minObservationPeriod),
                requiredEvidenceCount = maxOf(1, requiredEvidenceCount)
            )
        }

        val DEFAULT = CanonicalLibraryMetadataObservationWindow()

        fun closed(): CanonicalLibraryMetadataObservationWindow {
            return CanonicalLibraryMetadataObservationWindow(windowOpen = false)
        }
    }

    val diagnosticsSummary: String
        get() = listOf(
            "windowOpen=$windowOpen",
            "minPeriod=$minObservationPeriod",
            "requiredEvidence=$requiredEvidenceCount"
        ).joinToString(",")
}

object CanonicalLibraryMetadataObservationGate {
    enum class GateDecision(val rawValue: String) {
        ALLOW("allow"),
        BLOCK_WINDOW_CLOSED("blockWindowClosed"),
        BLOCK_INSUFFICIENT_EVIDENCE("blockInsufficientEvidence"),
        BLOCK_PERIOD_NOT_MET("blockPeriodNotMet"),
        BLOCK_NO_EVENTS("blockNoEvents")
    }

    data class GateResult(
        val allowed: Boolean,
        val decision: GateDecision,
        val window: CanonicalLibraryMetadataObservationWindow,
        val evidenceCount: Int,
        val observationPeriod: Double,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun allowed(
                window: CanonicalLibraryMetadataObservationWindow,
                evidenceCount: Int,
                observationPeriod: Double
            ): GateResult {
                return GateResult(
                    allowed = true,
                    decision = GateDecision.ALLOW,
                    window = window,
                    evidenceCount = evidenceCount,
                    observationPeriod = observationPeriod,
                    diagnosticsSummary = listOf(
                        "allowed=true",
                        "decision=allow",
                        "evidence=$evidenceCount",
                        "period=$observationPeriod"
                    ).joinToString(",")
                )
            }

            fun blocked(
                decision: GateDecision,
                window: CanonicalLibraryMetadataObservationWindow,
                evidenceCount: Int = 0,
                observationPeriod: Double = 0.0
            ): GateResult {
                return GateResult(
                    allowed = false,
                    decision = decision,
                    window = window,
                    evidenceCount = evidenceCount,
                    observationPeriod = observationPeriod,
                    diagnosticsSummary = listOf(
                        "allowed=false",
                        "decision=${decision.rawValue}",
                        "evidence=$evidenceCount",
                        "period=$observationPeriod"
                    ).joinToString(",")
                )
            }
        }
    }

    fun evaluate(
        window: CanonicalLibraryMetadataObservationWindow,
        events: List<CanonicalLibraryMetadataObservationEvent>
    ): GateResult {
        if (!window.windowOpen) {
            return GateResult.blocked(
                decision = GateDecision.BLOCK_WINDOW_CLOSED,
                window = window,
                evidenceCount = events.size
            )
        }

        if (events.isEmpty()) {
            return GateResult.blocked(
                decision = GateDecision.BLOCK_NO_EVENTS,
                window = window
            )
        }

        val observationPeriod = computeObservationPeriod(events)
        if (observationPeriod < window.minObservationPeriod) {
            return GateResult.blocked(
                decision = GateDecision.BLOCK_PERIOD_NOT_MET,
                window = window,
                evidenceCount = events.size,
                observationPeriod = observationPeriod
            )
        }

        val evidenceCount = events.count { it.equivalent != null }
        if (evidenceCount < window.requiredEvidenceCount) {
            return GateResult.blocked(
                decision = GateDecision.BLOCK_INSUFFICIENT_EVIDENCE,
                window = window,
                evidenceCount = evidenceCount,
                observationPeriod = observationPeriod
            )
        }

        return GateResult.allowed(
            window = window,
            evidenceCount = evidenceCount,
            observationPeriod = observationPeriod
        )
    }

    private fun computeObservationPeriod(
        events: List<CanonicalLibraryMetadataObservationEvent>
    ): Double {
        if (events.isEmpty()) return 0.0
        val sorted = events.sortedBy { it.timestamp.date.time }
        val first = sorted.first().timestamp.date.time
        val last = sorted.last().timestamp.date.time
        return (last - first) / 1000.0
    }
}

data class CanonicalLibraryMetadataObservationEvent
private constructor(
    val objectID: String,
    val operation: String,
    val canonicalHash: CanonicalHash?,
    val legacyHash: CanonicalHash?,
    val equivalent: Boolean?,
    val timestamp: CanonicalTimestamp,
    val eventID: String
) {
    companion object {
        operator fun invoke(
            objectID: String,
            operation: String,
            canonicalHash: CanonicalHash? = null,
            legacyHash: CanonicalHash? = null,
            equivalent: Boolean? = null,
            timestamp: CanonicalTimestamp = CanonicalTimestamp(Date()),
            eventID: String = UUID.randomUUID().toString()
        ): CanonicalLibraryMetadataObservationEvent {
            return CanonicalLibraryMetadataObservationEvent(
                objectID = objectID.trim().nilIfEmpty ?: "unknown-object",
                operation = operation.trim().nilIfEmpty ?: "unknown-operation",
                canonicalHash = canonicalHash,
                legacyHash = legacyHash,
                equivalent = equivalent,
                timestamp = timestamp,
                eventID = eventID.trim().nilIfEmpty ?: UUID.randomUUID().toString()
            )
        }
    }

    val id: String get() = eventID

    val hashMismatch: Boolean
        get() = canonicalHash != null && legacyHash != null &&
                (canonicalHash.algorithm != legacyHash.algorithm ||
                        canonicalHash.value != legacyHash.value)

    val diagnosticsSummary: String
        get() = listOf(
            "objectID=$objectID",
            "operation=$operation",
            "equivalent=$equivalent",
            "hashMismatch=$hashMismatch",
            "eventID=$eventID"
        ).joinToString(",")
}

class CanonicalLibraryMetadataObservationLog {
    private val events = mutableListOf<CanonicalLibraryMetadataObservationEvent>()

    fun record(event: CanonicalLibraryMetadataObservationEvent) {
        events.add(event)
    }

    fun record(
        objectID: String,
        operation: String,
        canonicalHash: CanonicalHash? = null,
        legacyHash: CanonicalHash? = null,
        equivalent: Boolean? = null
    ): CanonicalLibraryMetadataObservationEvent {
        val event = CanonicalLibraryMetadataObservationEvent(
            objectID = objectID,
            operation = operation,
            canonicalHash = canonicalHash,
            legacyHash = legacyHash,
            equivalent = equivalent
        )
        events.add(event)
        return event
    }

    fun query(
        objectID: String? = null,
        operation: String? = null,
        since: CanonicalTimestamp? = null
    ): List<CanonicalLibraryMetadataObservationEvent> {
        return events.filter { event ->
            (objectID == null || event.objectID == objectID) &&
                    (operation == null || event.operation == operation) &&
                    (since == null || event.timestamp.date.time >= since.date.time)
        }
    }

    fun allEvents(): List<CanonicalLibraryMetadataObservationEvent> {
        return events.toList()
    }

    fun distinctObjectIDs(): Set<String> {
        return events.map { it.objectID }.toSet()
    }

    fun isEvidenceSufficient(
        window: CanonicalLibraryMetadataObservationWindow
    ): Boolean {
        val gateResult = CanonicalLibraryMetadataObservationGate.evaluate(window, events)
        return gateResult.allowed
    }

    fun clear() {
        events.clear()
    }
}

enum class CanonicalLibraryMetadataObservationRecommendation(val rawValue: String) {
    PROCEED("proceed"),
    EXTEND_OBSERVATION("extendObservation"),
    BLOCKED("blocked"),
    NEED_AUDIT("needAudit");

    companion object {
        val allCases: List<CanonicalLibraryMetadataObservationRecommendation> = entries.toList()
    }
}

data class CanonicalLibraryMetadataObservationReport
private constructor(
    val events: List<CanonicalLibraryMetadataObservationEvent>,
    val evidenceSufficient: Boolean,
    val recommendation: CanonicalLibraryMetadataObservationRecommendation,
    val reportID: String,
    val observationStart: CanonicalTimestamp?,
    val observationEnd: CanonicalTimestamp?,
    val equivalentCount: Int,
    val divergentCount: Int,
    val unmappedCount: Int,
    val diagnosticsSummary: String
) {
    constructor(
        events: List<CanonicalLibraryMetadataObservationEvent>,
        evidenceSufficient: Boolean,
        recommendation: CanonicalLibraryMetadataObservationRecommendation,
        reportID: String = UUID.randomUUID().toString()
    ) : this(
        events = events.sortedBy { it.timestamp.date.time },
        evidenceSufficient = evidenceSufficient,
        recommendation = recommendation,
        reportID = reportID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
        observationStart = events.minByOrNull { it.timestamp.date.time }?.timestamp,
        observationEnd = events.maxByOrNull { it.timestamp.date.time }?.timestamp,
        equivalentCount = events.count { it.equivalent == true },
        divergentCount = events.count { it.equivalent == false },
        unmappedCount = events.count { it.equivalent == null },
        diagnosticsSummary = buildDiagnosticsSummary(
            events = events,
            evidenceSufficient = evidenceSufficient,
            recommendation = recommendation
        )
    )

    companion object {
        fun buildDiagnosticsSummary(
            events: List<CanonicalLibraryMetadataObservationEvent>,
            evidenceSufficient: Boolean,
            recommendation: CanonicalLibraryMetadataObservationRecommendation
        ): String {
            return listOf(
                "events=${events.size}",
                "equivalent=${events.count { it.equivalent == true }}",
                "divergent=${events.count { it.equivalent == false }}",
                "unmapped=${events.count { it.equivalent == null }}",
                "evidenceSufficient=$evidenceSufficient",
                "recommendation=${recommendation.rawValue}"
            ).joinToString(",")
        }

        fun fromLog(
            log: CanonicalLibraryMetadataObservationLog,
            window: CanonicalLibraryMetadataObservationWindow
        ): CanonicalLibraryMetadataObservationReport {
            val events = log.allEvents()
            val gateResult = CanonicalLibraryMetadataObservationGate.evaluate(window, events)

            val recommendation = when {
                gateResult.allowed -> CanonicalLibraryMetadataObservationRecommendation.PROCEED
                gateResult.decision == CanonicalLibraryMetadataObservationGate.GateDecision.BLOCK_PERIOD_NOT_MET ->
                    CanonicalLibraryMetadataObservationRecommendation.EXTEND_OBSERVATION
                gateResult.decision == CanonicalLibraryMetadataObservationGate.GateDecision.BLOCK_INSUFFICIENT_EVIDENCE ->
                    CanonicalLibraryMetadataObservationRecommendation.EXTEND_OBSERVATION
                else -> CanonicalLibraryMetadataObservationRecommendation.BLOCKED
            }

            return CanonicalLibraryMetadataObservationReport(
                events = events,
                evidenceSufficient = gateResult.allowed,
                recommendation = recommendation
            )
        }
    }

    fun divergentObjectIDs(): List<String> {
        return events.filter { it.equivalent == false }.map { it.objectID }.distinct().sorted()
    }
}
