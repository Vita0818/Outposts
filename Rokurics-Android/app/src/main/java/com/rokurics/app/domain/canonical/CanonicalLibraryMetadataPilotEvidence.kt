package com.rokurics.app.domain.canonical

import java.util.UUID

enum class CanonicalLibraryMetadataN1EvidenceSource(val rawValue: String) {
    LANDING_REPORT("landingReport"),
    CANARY_OBSERVATION("canaryObservation"),
    SAFETY_PROOF("safetyProof"),
    LANDING_FREEZE("landingFreeze"),
    DIAGNOSTICS("diagnostics"),
    TEST_FIXTURE("testFixture");

    companion object {
        val allCases: List<CanonicalLibraryMetadataN1EvidenceSource> = entries.toList()
    }
}

enum class CanonicalLibraryMetadataN1EvidenceStatus(val rawValue: String) {
    PRESENT("present"),
    MISSING("missing"),
    REDACTED("redacted");

    companion object {
        val allCases: List<CanonicalLibraryMetadataN1EvidenceStatus> = entries.toList()
    }
}

data class CanonicalLibraryMetadataN1EvidenceRecord
private constructor(
    val source: CanonicalLibraryMetadataN1EvidenceSource,
    val status: CanonicalLibraryMetadataN1EvidenceStatus,
    val summary: String?,
    val recordID: String
) {
    companion object {
        operator fun invoke(
            source: CanonicalLibraryMetadataN1EvidenceSource,
            status: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING,
            summary: String? = null,
            recordID: String = UUID.randomUUID().toString()
        ): CanonicalLibraryMetadataN1EvidenceRecord {
            return CanonicalLibraryMetadataN1EvidenceRecord(
                source = source,
                status = status,
                summary = summary?.trim()?.nilIfEmpty,
                recordID = recordID.trim().nilIfEmpty ?: UUID.randomUUID().toString()
            )
        }

        fun present(
            source: CanonicalLibraryMetadataN1EvidenceSource,
            summary: String? = null
        ): CanonicalLibraryMetadataN1EvidenceRecord {
            return CanonicalLibraryMetadataN1EvidenceRecord(
                source = source,
                status = CanonicalLibraryMetadataN1EvidenceStatus.PRESENT,
                summary = summary
            )
        }

        fun missing(
            source: CanonicalLibraryMetadataN1EvidenceSource
        ): CanonicalLibraryMetadataN1EvidenceRecord {
            return CanonicalLibraryMetadataN1EvidenceRecord(
                source = source,
                status = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
            )
        }

        fun redacted(
            source: CanonicalLibraryMetadataN1EvidenceSource
        ): CanonicalLibraryMetadataN1EvidenceRecord {
            return CanonicalLibraryMetadataN1EvidenceRecord(
                source = source,
                status = CanonicalLibraryMetadataN1EvidenceStatus.REDACTED
            )
        }
    }

    val id: String get() = recordID

    val isPresent: Boolean
        get() = status == CanonicalLibraryMetadataN1EvidenceStatus.PRESENT

    val diagnosticsSummary: String
        get() = listOf(
            "source=${source.rawValue}",
            "status=${status.rawValue}",
            "summary=${summary ?: "none"}"
        ).joinToString(",")
}

data class CanonicalPilotN1EvidenceBundle
private constructor(
    val evidenceID: String,
    val source: CanonicalLibraryMetadataN1EvidenceSource,
    val records: List<CanonicalLibraryMetadataN1EvidenceRecord>,
    val invariantsValid: Boolean?,
    val bundleTimestamp: String?,
    val redacted: Boolean,
    val diagnosticsSummary: String
) {
    constructor(
        evidenceID: String = UUID.randomUUID().toString(),
        source: CanonicalLibraryMetadataN1EvidenceSource = CanonicalLibraryMetadataN1EvidenceSource.DIAGNOSTICS,
        records: List<CanonicalLibraryMetadataN1EvidenceRecord> = emptyList(),
        invariantsValid: Boolean? = null,
        bundleTimestamp: String? = null,
        redacted: Boolean = true
    ) : this(
        evidenceID = evidenceID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
        source = source,
        records = records.sortedBy { it.source.rawValue },
        invariantsValid = invariantsValid,
        bundleTimestamp = bundleTimestamp?.trim()?.nilIfEmpty,
        redacted = redacted,
        diagnosticsSummary = listOf(
            "evidenceID=$evidenceID",
            "source=${source.rawValue}",
            "records=${records.size}",
            "invariantsValid=$invariantsValid",
            "redacted=$redacted"
        ).joinToString(",")
    )

    companion object {
        fun build(
            source: CanonicalLibraryMetadataN1EvidenceSource,
            records: List<CanonicalLibraryMetadataN1EvidenceRecord>,
            redacted: Boolean = true
        ): CanonicalPilotN1EvidenceBundle {
            val bundle = CanonicalPilotN1EvidenceBundle(
                source = source,
                records = records,
                invariantsValid = null,
                redacted = redacted
            )
            val validated = bundle.validateInvariants()
            return bundle.copy(invariantsValid = validated.valid)
        }
    }

    val presentCount: Int
        get() = records.count { it.isPresent }

    val missingCount: Int
        get() = records.count { it.status == CanonicalLibraryMetadataN1EvidenceStatus.MISSING }

    val redactedCount: Int
        get() = records.count { it.status == CanonicalLibraryMetadataN1EvidenceStatus.REDACTED }

    fun validateInvariants(): CanonicalLibraryMetadataN1PostRunValidator.InvariantResult {
        return CanonicalLibraryMetadataN1PostRunValidator.validate(this)
    }

    fun isComplete(): Boolean {
        return missingCount == 0 && invariantsValid == true
    }

    fun requiredSources(): Set<CanonicalLibraryMetadataN1EvidenceSource> {
        return CanonicalLibraryMetadataN1EvidenceSource.allCases.toSet()
    }

    fun missingSources(): Set<CanonicalLibraryMetadataN1EvidenceSource> {
        val presentSources = records
            .filter { it.isPresent }
            .map { it.source }
            .toSet()
        return requiredSources().subtract(presentSources)
    }

    private fun copy(
        invariantsValid: Boolean?
    ): CanonicalPilotN1EvidenceBundle {
        return CanonicalPilotN1EvidenceBundle(
            evidenceID = evidenceID,
            source = source,
            records = records,
            invariantsValid = invariantsValid,
            bundleTimestamp = bundleTimestamp,
            redacted = redacted,
            diagnosticsSummary = diagnosticsSummary
        )
    }
}

object CanonicalLibraryMetadataN1PostRunValidator {
    data class InvariantResult(
        val valid: Boolean,
        val allSourcesPresent: Boolean,
        val noRedactedBlockers: Boolean,
        val evidenceConsistent: Boolean,
        val blockCount: Int,
        val blockers: List<String>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun invalid(blockers: List<String>): InvariantResult {
                return InvariantResult(
                    valid = false,
                    allSourcesPresent = false,
                    noRedactedBlockers = true,
                    evidenceConsistent = false,
                    blockCount = blockers.size,
                    blockers = blockers.sorted(),
                    diagnosticsSummary = listOf(
                        "valid=false",
                        "blockers=${blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }

            fun valid(): InvariantResult {
                return InvariantResult(
                    valid = true,
                    allSourcesPresent = true,
                    noRedactedBlockers = true,
                    evidenceConsistent = true,
                    blockCount = 0,
                    blockers = emptyList(),
                    diagnosticsSummary = "valid=true,blockers=none"
                )
            }
        }
    }

    fun validate(
        bundle: CanonicalPilotN1EvidenceBundle
    ): InvariantResult {
        val blockers = mutableListOf<String>()

        val requiredSources = bundle.requiredSources()
        val presentSources = bundle.records
            .filter { it.isPresent }
            .map { it.source }
            .toSet()

        val missing = requiredSources.subtract(presentSources)
        if (missing.isNotEmpty()) {
            blockers.addAll(missing.map { "${it.rawValue}:missing" })
        }

        val redactedButNoSummary = bundle.records
            .filter {
                it.status == CanonicalLibraryMetadataN1EvidenceStatus.REDACTED &&
                        it.summary == null
            }
        if (redactedButNoSummary.isNotEmpty()) {
            blockers.add("redactedRecordsWithoutSummary")
        }

        val presentSourcesWithoutSummary = bundle.records
            .filter {
                it.isPresent && it.summary == null
            }
        if (presentSourcesWithoutSummary.isNotEmpty()) {
            blockers.add("presentRecordsWithoutSummary")
        }

        if (bundle.records.isEmpty()) {
            blockers.add("noEvidenceRecords")
        }

        if (bundle.redacted && missing.isNotEmpty()) {
            blockers.add("redactedButMissingEvidence")
        }

        val valid = blockers.isEmpty()
        val allSourcesPresent = presentSources.containsAll(requiredSources)
        val noRedactedBlockers = !blockers.any {
            it.startsWith("redacted")
        }
        val evidenceConsistent = presentSourcesWithoutSummary.isEmpty() &&
                redactedButNoSummary.isEmpty()

        return InvariantResult(
            valid = valid,
            allSourcesPresent = allSourcesPresent,
            noRedactedBlockers = noRedactedBlockers,
            evidenceConsistent = evidenceConsistent,
            blockCount = blockers.size,
            blockers = blockers,
            diagnosticsSummary = listOf(
                "valid=$valid",
                "allSources=$allSourcesPresent",
                "noRedacted=$noRedactedBlockers",
                "consistent=$evidenceConsistent",
                "blockers=${blockers.joinToString("|")}"
            ).joinToString(",")
        )
    }
}

data class CanonicalLibraryMetadataN3ReadinessGateResult
private constructor(
    val ready: Boolean,
    val requiresN1Evidence: Boolean,
    val requiresAudit: Boolean,
    val gateID: String,
    val missingEvidenceSources: List<String>,
    val invariantViolations: List<String>,
    val diagnosticsSummary: String
) {
    companion object {
        operator fun invoke(
            ready: Boolean = false,
            requiresN1Evidence: Boolean = true,
            requiresAudit: Boolean = false,
            gateID: String = UUID.randomUUID().toString(),
            missingEvidenceSources: List<String> = emptyList(),
            invariantViolations: List<String> = emptyList()
        ): CanonicalLibraryMetadataN3ReadinessGateResult {
            return CanonicalLibraryMetadataN3ReadinessGateResult(
                ready = ready,
                requiresN1Evidence = requiresN1Evidence,
                requiresAudit = requiresAudit,
                gateID = gateID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
                missingEvidenceSources = missingEvidenceSources.sorted(),
                invariantViolations = invariantViolations.sorted(),
                diagnosticsSummary = listOf(
                    "ready=$ready",
                    "requiresN1=$requiresN1Evidence",
                    "requiresAudit=$requiresAudit",
                    "missing=${missingEvidenceSources.joinToString("+").ifEmpty { "none" }}",
                    "violations=${invariantViolations.joinToString("+").ifEmpty { "none" }}"
                ).joinToString(",")
            )
        }

        fun ready(): CanonicalLibraryMetadataN3ReadinessGateResult {
            return CanonicalLibraryMetadataN3ReadinessGateResult(
                ready = true,
                requiresN1Evidence = false,
                requiresAudit = false
            )
        }

        fun notReady(
            missingEvidenceSources: List<String>,
            invariantViolations: List<String> = emptyList()
        ): CanonicalLibraryMetadataN3ReadinessGateResult {
            return CanonicalLibraryMetadataN3ReadinessGateResult(
                ready = false,
                requiresN1Evidence = missingEvidenceSources.isNotEmpty(),
                requiresAudit = invariantViolations.isNotEmpty() || missingEvidenceSources.isNotEmpty(),
                missingEvidenceSources = missingEvidenceSources,
                invariantViolations = invariantViolations
            )
        }
    }
}
