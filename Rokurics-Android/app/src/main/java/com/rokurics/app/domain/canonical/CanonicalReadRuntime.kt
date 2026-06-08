package com.rokurics.app.domain.canonical

import java.util.Date
import java.util.Locale

// ── CanonicalReadProjectionSource ──

enum class CanonicalReadProjectionSource(val value: String) {
    LEGACY("legacy"),
    CANONICAL("canonical");

    companion object {
        fun fromValue(value: String): CanonicalReadProjectionSource =
            entries.first { it.value == value }
    }
}

// ── CanonicalReadDomain ──

enum class CanonicalReadDomain(val value: String) {
    RECORDING_METADATA("recordingMetadata"),
    LIBRARY_METADATA("libraryMetadata"),
    GENERATED_ARTIFACTS("generatedArtifacts"),
    TOMBSTONE_CONFLICT("tombstoneConflict"),
    AUDIO_UPLOAD_STATUS("audioUploadStatus"),
    SYNC_ENGINE_STATUS("syncEngineStatus");

    companion object {
        fun fromValue(value: String): CanonicalReadDomain =
            entries.first { it.value == value }
    }
}

// ── Type 1: CanonicalReadRuntimeMode ──

enum class CanonicalReadRuntimeMode(val value: String) {
    DISABLED("disabled"),
    PARALLEL_COMPARE("parallelCompare"),
    CANONICAL_READ_CANDIDATE("canonicalReadCandidate"),
    GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK("guardedCanonicalReadWithLegacyFallback"),
    BLOCKED("blocked");

    val buildsCanonicalCandidate: Boolean
        get() = when (this) {
            DISABLED, BLOCKED -> false
            PARALLEL_COMPARE, CANONICAL_READ_CANDIDATE, GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK -> true
        }

    companion object {
        fun fromValue(value: String): CanonicalReadRuntimeMode =
            entries.first { it.value == value }
    }
}

// ── Type 2: CanonicalReadRuntimePolicy ──

data class CanonicalReadRuntimePolicy(
    val debugInternalBuild: Boolean = false,
    val ownerApproved: Boolean = false,
    val manualOwnerApproval: Boolean = false,
    val releaseDefaultBuild: Boolean = true,
    val legacyFallbackAvailable: Boolean = true,
    val diagnosticsRedacted: Boolean = true,
    val applyRuntimeEvidenceValidForNonAudio: Boolean = false,
    val uploadRuntimeEvidenceValidForAudioStatus: Boolean = false,
    val inventorySnapshotAvailable: Boolean = false,
    val planAuthorityEvidenceValid: Boolean = false,
    val existenceTruthEvidenceValid: Boolean = false,
    val otherDomainsNotConflicting: Boolean = true,
    val allowDivergentGuardedReadForTests: Boolean = false,
    val readMustNotTriggerSyncUpload: Boolean = true,
    val readMustNotMutateStore: Boolean = true,
    val maxDiagnosticsEvents: Int = 64,
    val evidenceFromEarlierChain: Boolean = false
) {
    companion object {
        fun explicitGuardedDebugInternal(
            allowDivergentGuardedReadForTests: Boolean = false
        ): CanonicalReadRuntimePolicy = CanonicalReadRuntimePolicy(
            debugInternalBuild = true,
            ownerApproved = true,
            manualOwnerApproval = true,
            releaseDefaultBuild = false,
            legacyFallbackAvailable = true,
            diagnosticsRedacted = true,
            applyRuntimeEvidenceValidForNonAudio = true,
            uploadRuntimeEvidenceValidForAudioStatus = true,
            inventorySnapshotAvailable = true,
            planAuthorityEvidenceValid = true,
            existenceTruthEvidenceValid = true,
            otherDomainsNotConflicting = true,
            allowDivergentGuardedReadForTests = allowDivergentGuardedReadForTests,
            evidenceFromEarlierChain = true
        )
    }
}

// ── Type 3: CanonicalReadRuntimeConfiguration ──

data class CanonicalReadRuntimeConfiguration(
    val mode: CanonicalReadRuntimeMode = CanonicalReadRuntimeMode.DISABLED,
    val policy: CanonicalReadRuntimePolicy = CanonicalReadRuntimePolicy()
) {
    companion object {
        val DISABLED = CanonicalReadRuntimeConfiguration()

        fun explicitGuardedCanonicalRead(
            allowDivergentGuardedReadForTests: Boolean = false
        ): CanonicalReadRuntimeConfiguration = CanonicalReadRuntimeConfiguration(
            mode = CanonicalReadRuntimeMode.GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK,
            policy = CanonicalReadRuntimePolicy.explicitGuardedDebugInternal(
                allowDivergentGuardedReadForTests = allowDivergentGuardedReadForTests
            )
        )
    }
}

// ── CanonicalReadRuntimeFallback ──

enum class CanonicalReadRuntimeFallback(val value: String) {
    NONE("none"),
    LEGACY_DEFAULT("legacyDefault"),
    PARALLEL_COMPARE_RETURNS_LEGACY("parallelCompareReturnsLegacy"),
    CANONICAL_CANDIDATE_NOT_SERVED("canonicalCandidateNotServed"),
    GUARDED_GATE_BLOCKED("guardedGateBlocked"),
    CANONICAL_PROJECTION_MISSING("canonicalProjectionMissing"),
    CANONICAL_READ_EXCEPTION("canonicalReadException"),
    BLOCKED_MODE("blockedMode");

    companion object {
        fun fromValue(value: String): CanonicalReadRuntimeFallback =
            entries.first { it.value == value }
    }
}

// ── CanonicalReadRuntimeDivergenceKind ──

enum class CanonicalReadRuntimeDivergenceKind(val value: String) {
    MISSING_OBJECT("missingObject"),
    METADATA_MISMATCH("metadataMismatch"),
    TITLE_TAGS_FOLDER_MISMATCH("titleTagsFolderMismatch"),
    ARTIFACT_AVAILABILITY_MISMATCH("artifactAvailabilityMismatch"),
    TOMBSTONE_CONFLICT_MISMATCH("tombstoneConflictMismatch"),
    AUDIO_AVAILABILITY_MISMATCH("audioAvailabilityMismatch"),
    UPLOAD_STATUS_MISMATCH("uploadStatusMismatch"),
    UNSUPPORTED_OBJECT("unsupportedObject"),
    PATH_CONTENT_LEAK_RISK("pathContentLeakRisk");

    companion object {
        fun fromValue(value: String): CanonicalReadRuntimeDivergenceKind =
            entries.first { it.value == value }
    }
}

// ── Type 12: CanonicalReadRuntimeDivergence ──

data class CanonicalReadRuntimeDivergence(
    val kind: CanonicalReadRuntimeDivergenceKind,
    val domain: CanonicalReadDomain,
    val objectID: String? = null,
    val field: String? = null,
    val legacyValue: String? = null,
    val canonicalValue: String? = null,
    val fatal: Boolean = false,
    val detail: String? = null
) {
    val id: String
        get() = this.let {
            listOfNotNull(
                it.kind.value,
                it.domain.value,
                it.objectID ?: "run",
                it.field.orEmpty()
            ).joinToString("|")
        }

    companion object {
        fun create(
            kind: CanonicalReadRuntimeDivergenceKind,
            domain: CanonicalReadDomain,
            objectID: String? = null,
            field: String? = null,
            legacyValue: String? = null,
            canonicalValue: String? = null,
            fatal: Boolean = false,
            detail: String? = null
        ): CanonicalReadRuntimeDivergence {
            val effectiveFatal = fatal || kind == CanonicalReadRuntimeDivergenceKind.PATH_CONTENT_LEAK_RISK
            return CanonicalReadRuntimeDivergence(
                kind = kind,
                domain = domain,
                objectID = readableSafeIdentifier(objectID, "object"),
                field = readableSafeText(field),
                legacyValue = readableSafeText(legacyValue),
                canonicalValue = readableSafeText(canonicalValue),
                fatal = effectiveFatal,
                detail = detail
            )
        }
    }
}

// ── CanonicalReadRuntimeEquivalenceReport ──

data class CanonicalReadRuntimeEquivalenceReport(
    val equivalent: Boolean,
    val divergenceCount: Int,
    val fatalDivergenceCount: Int,
    val domainsCompared: List<CanonicalReadDomain>,
    val diagnosticsSummary: String
)

// ── Type 13: CanonicalReadRuntimeDiff ──

data class CanonicalReadRuntimeDiff(
    val divergences: List<CanonicalReadRuntimeDivergence>,
    val equivalenceReport: CanonicalReadRuntimeEquivalenceReport,
    val legacySnapshotSummary: String,
    val canonicalSnapshotSummary: String,
    val diagnosticsSummary: String
) {
    val equivalent: Boolean
        get() = equivalenceReport.equivalent

    val equivalenceCount: Int
        get() = if (equivalent) 1 else 0

    val divergenceCount: Int
        get() = equivalenceReport.divergenceCount

    companion object {
        fun compare(
            legacy: CanonicalReadSnapshot,
            canonical: CanonicalReadSnapshot
        ): CanonicalReadRuntimeDiff {
            val divergences = mutableListOf<CanonicalReadRuntimeDivergence>()

            compareRecordingProjections(
                legacy.recordingProjections, canonical.recordingProjections, divergences
            )
            compareLibraryProjections(
                legacy.libraryProjection, canonical.libraryProjection, divergences
            )
            compareArtifactProjections(
                legacy.artifactProjection, canonical.artifactProjection, divergences
            )
            compareConflictProjections(
                legacy.conflictProjection, canonical.conflictProjection, divergences
            )
            compareUploadProjections(
                legacy.uploadProjection, canonical.uploadProjection, divergences
            )
            compareSyncEngineStatus(
                legacy.syncEngineStatus, canonical.syncEngineStatus, divergences
            )

            if (legacy.pathOrContentLeakRisk || canonical.pathOrContentLeakRisk) {
                divergences.add(
                    CanonicalReadRuntimeDivergence.create(
                        kind = CanonicalReadRuntimeDivergenceKind.PATH_CONTENT_LEAK_RISK,
                        domain = CanonicalReadDomain.SYNC_ENGINE_STATUS,
                        field = "snapshotRedaction",
                        legacyValue = "legacyLeakRisk=${legacy.pathOrContentLeakRisk}",
                        canonicalValue = "canonicalLeakRisk=${canonical.pathOrContentLeakRisk}",
                        fatal = true
                    )
                )
            }

            val uniqueDivergences = divergences
                .distinctBy { it.id }
                .sortedBy { it.id }
            val equivalent = uniqueDivergences.isEmpty()
            val domains = CanonicalReadDomain.entries
            val kindSummary = uniqueDivergences
                .map { it.kind.value }
                .distinct()
                .sorted()
                .joinToString("+")

            val report = CanonicalReadRuntimeEquivalenceReport(
                equivalent = equivalent,
                divergenceCount = uniqueDivergences.size,
                fatalDivergenceCount = uniqueDivergences.count { it.fatal },
                domainsCompared = domains,
                diagnosticsSummary = "equivalent=$equivalent,divergences=${uniqueDivergences.size},fatal=${uniqueDivergences.count { it.fatal }},kinds=$kindSummary"
            )

            return CanonicalReadRuntimeDiff(
                divergences = uniqueDivergences,
                equivalenceReport = report,
                legacySnapshotSummary = legacy.diagnosticsSummary,
                canonicalSnapshotSummary = canonical.diagnosticsSummary,
                diagnosticsSummary = "domains=${domains.joinToString("+") { it.value }},${report.diagnosticsSummary}"
            )
        }

        private fun compareRecordingProjections(
            legacy: CanonicalRecordingReadProjection,
            canonical: CanonicalRecordingReadProjection,
            divergences: MutableList<CanonicalReadRuntimeDivergence>
        ) {
            appendProjectionFailures(legacy.failures + canonical.failures, divergences)

            val legacyByID = legacy.records.associateBy { it.objectID }
            val canonicalByID = canonical.records.associateBy { it.objectID }
            val allIDs = (legacyByID.keys + canonicalByID.keys).sorted()

            for (objectID in allIDs) {
                val legacyRecord = legacyByID[objectID]
                val canonicalRecord = canonicalByID[objectID]
                when {
                    legacyRecord == null -> divergences.add(
                        CanonicalReadRuntimeDivergence.create(
                            kind = CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT,
                            domain = CanonicalReadDomain.RECORDING_METADATA,
                            objectID = objectID,
                            canonicalValue = "present"
                        )
                    )
                    canonicalRecord == null -> divergences.add(
                        CanonicalReadRuntimeDivergence.create(
                            kind = CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT,
                            domain = CanonicalReadDomain.RECORDING_METADATA,
                            objectID = objectID,
                            legacyValue = "present"
                        )
                    )
                    else -> {
                        appendMismatch(
                            CanonicalReadRuntimeDivergenceKind.TITLE_TAGS_FOLDER_MISMATCH,
                            CanonicalReadDomain.RECORDING_METADATA, objectID,
                            "title", legacyRecord.title, canonicalRecord.title, divergences
                        )
                        appendMismatch(
                            CanonicalReadRuntimeDivergenceKind.TITLE_TAGS_FOLDER_MISMATCH,
                            CanonicalReadDomain.RECORDING_METADATA, objectID,
                            "tags", legacyRecord.tagsKey, canonicalRecord.tagsKey, divergences
                        )
                        appendMismatch(
                            CanonicalReadRuntimeDivergenceKind.TITLE_TAGS_FOLDER_MISMATCH,
                            CanonicalReadDomain.RECORDING_METADATA, objectID,
                            "folderSummary", legacyRecord.folderSummary, canonicalRecord.folderSummary, divergences
                        )
                        appendMismatch(
                            CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT,
                            CanonicalReadDomain.RECORDING_METADATA, objectID,
                            "audioAvailable", legacyRecord.audioAvailable.toString(), canonicalRecord.audioAvailable.toString(), divergences
                        )
                        appendMismatch(
                            CanonicalReadRuntimeDivergenceKind.METADATA_MISMATCH,
                            CanonicalReadDomain.RECORDING_METADATA, objectID,
                            "hashPrefix", legacyRecord.hashPrefix ?: "nil", canonicalRecord.hashPrefix ?: "nil", divergences
                        )
                    }
                }
            }
        }

        private fun compareLibraryProjections(
            legacy: CanonicalLibraryReadProjection,
            canonical: CanonicalLibraryReadProjection,
            divergences: MutableList<CanonicalReadRuntimeDivergence>
        ) {
            if (legacy.folders != canonical.folders) {
                divergences.add(
                    CanonicalReadRuntimeDivergence.create(
                        kind = CanonicalReadRuntimeDivergenceKind.TITLE_TAGS_FOLDER_MISMATCH,
                        domain = CanonicalReadDomain.LIBRARY_METADATA,
                        field = "folders",
                        legacyValue = legacy.folders.toString(),
                        canonicalValue = canonical.folders.toString()
                    )
                )
            }
            if (legacy.studyItemCount != canonical.studyItemCount) {
                divergences.add(
                    CanonicalReadRuntimeDivergence.create(
                        kind = CanonicalReadRuntimeDivergenceKind.METADATA_MISMATCH,
                        domain = CanonicalReadDomain.LIBRARY_METADATA,
                        field = "studyItemCount",
                        legacyValue = legacy.studyItemCount.toString(),
                        canonicalValue = canonical.studyItemCount.toString()
                    )
                )
            }
        }

        private fun compareArtifactProjections(
            legacy: CanonicalArtifactReadProjection,
            canonical: CanonicalArtifactReadProjection,
            divergences: MutableList<CanonicalReadRuntimeDivergence>
        ) {
            if (legacy.generatedArtifactCount != canonical.generatedArtifactCount) {
                divergences.add(
                    CanonicalReadRuntimeDivergence.create(
                        kind = CanonicalReadRuntimeDivergenceKind.ARTIFACT_AVAILABILITY_MISMATCH,
                        domain = CanonicalReadDomain.GENERATED_ARTIFACTS,
                        field = "generatedArtifactCount",
                        legacyValue = legacy.generatedArtifactCount.toString(),
                        canonicalValue = canonical.generatedArtifactCount.toString()
                    )
                )
            }
            if (legacy.generatedArtifactSummary != canonical.generatedArtifactSummary) {
                divergences.add(
                    CanonicalReadRuntimeDivergence.create(
                        kind = CanonicalReadRuntimeDivergenceKind.ARTIFACT_AVAILABILITY_MISMATCH,
                        domain = CanonicalReadDomain.GENERATED_ARTIFACTS,
                        field = "summary",
                        legacyValue = legacy.generatedArtifactSummary,
                        canonicalValue = canonical.generatedArtifactSummary
                    )
                )
            }
        }

        private fun compareConflictProjections(
            legacy: CanonicalConflictReadProjection,
            canonical: CanonicalConflictReadProjection,
            divergences: MutableList<CanonicalReadRuntimeDivergence>
        ) {
            if (legacy.conflictCount != canonical.conflictCount) {
                divergences.add(
                    CanonicalReadRuntimeDivergence.create(
                        kind = CanonicalReadRuntimeDivergenceKind.TOMBSTONE_CONFLICT_MISMATCH,
                        domain = CanonicalReadDomain.TOMBSTONE_CONFLICT,
                        field = "conflictCount",
                        legacyValue = legacy.conflictCount.toString(),
                        canonicalValue = canonical.conflictCount.toString()
                    )
                )
            }
        }

        private fun compareUploadProjections(
            legacy: CanonicalUploadReadProjection,
            canonical: CanonicalUploadReadProjection,
            divergences: MutableList<CanonicalReadRuntimeDivergence>
        ) {
            appendProjectionFailures(legacy.failures + canonical.failures, divergences)

            val legacyByID = legacy.records.associateBy { it.objectID }
            val canonicalByID = canonical.records.associateBy { it.objectID }
            val allIDs = (legacyByID.keys + canonicalByID.keys).sorted()

            for (objectID in allIDs) {
                val legacyRecord = legacyByID[objectID]
                val canonicalRecord = canonicalByID[objectID]
                when {
                    legacyRecord == null -> divergences.add(
                        CanonicalReadRuntimeDivergence.create(
                            kind = CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT,
                            domain = CanonicalReadDomain.AUDIO_UPLOAD_STATUS,
                            objectID = objectID,
                            canonicalValue = "present"
                        )
                    )
                    canonicalRecord == null -> divergences.add(
                        CanonicalReadRuntimeDivergence.create(
                            kind = CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT,
                            domain = CanonicalReadDomain.AUDIO_UPLOAD_STATUS,
                            objectID = objectID,
                            legacyValue = "present"
                        )
                    )
                    else -> {
                        appendMismatch(
                            CanonicalReadRuntimeDivergenceKind.AUDIO_AVAILABILITY_MISMATCH,
                            CanonicalReadDomain.AUDIO_UPLOAD_STATUS, objectID,
                            "audioAvailable",
                            legacyRecord.audioAvailable.toString(),
                            canonicalRecord.audioAvailable.toString(), divergences
                        )
                        appendMismatch(
                            CanonicalReadRuntimeDivergenceKind.UPLOAD_STATUS_MISMATCH,
                            CanonicalReadDomain.AUDIO_UPLOAD_STATUS, objectID,
                            "uploadState",
                            legacyRecord.uploadState.value,
                            canonicalRecord.uploadState.value, divergences
                        )
                    }
                }
            }
        }

        private fun compareSyncEngineStatus(
            legacy: CanonicalSyncEngineStatusReadProjection,
            canonical: CanonicalSyncEngineStatusReadProjection,
            divergences: MutableList<CanonicalReadRuntimeDivergence>
        ) {
            if (legacy.syncSummary != canonical.syncSummary) {
                divergences.add(
                    CanonicalReadRuntimeDivergence.create(
                        kind = CanonicalReadRuntimeDivergenceKind.METADATA_MISMATCH,
                        domain = CanonicalReadDomain.SYNC_ENGINE_STATUS,
                        field = "syncSummary",
                        legacyValue = legacy.syncSummary,
                        canonicalValue = canonical.syncSummary
                    )
                )
            }
        }

        private fun appendProjectionFailures(
            failures: List<CanonicalReadProjectionFailure>,
            divergences: MutableList<CanonicalReadRuntimeDivergence>
        ) {
            for (failure in failures) {
                when (failure.kind) {
                    CanonicalReadProjectionFailureKind.SNAPSHOT_MISSING -> divergences.add(
                        CanonicalReadRuntimeDivergence.create(
                            kind = CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT,
                            domain = failure.domain,
                            objectID = failure.objectID,
                            field = "snapshot",
                            canonicalValue = failure.reason
                        )
                    )
                    CanonicalReadProjectionFailureKind.UNSUPPORTED_OBJECT -> divergences.add(
                        CanonicalReadRuntimeDivergence.create(
                            kind = CanonicalReadRuntimeDivergenceKind.UNSUPPORTED_OBJECT,
                            domain = failure.domain,
                            objectID = failure.objectID,
                            field = "object",
                            canonicalValue = failure.reason
                        )
                    )
                    CanonicalReadProjectionFailureKind.PATH_CONTENT_LEAK_RISK -> divergences.add(
                        CanonicalReadRuntimeDivergence.create(
                            kind = CanonicalReadRuntimeDivergenceKind.PATH_CONTENT_LEAK_RISK,
                            domain = failure.domain,
                            objectID = failure.objectID,
                            field = "projection",
                            canonicalValue = failure.reason,
                            fatal = true
                        )
                    )
                }
            }
        }

        private fun appendMismatch(
            kind: CanonicalReadRuntimeDivergenceKind,
            domain: CanonicalReadDomain,
            objectID: String,
            field: String,
            legacyValue: String,
            canonicalValue: String,
            divergences: MutableList<CanonicalReadRuntimeDivergence>
        ) {
            if (legacyValue != canonicalValue) {
                divergences.add(
                    CanonicalReadRuntimeDivergence.create(
                        kind = kind,
                        domain = domain,
                        objectID = objectID,
                        field = field,
                        legacyValue = legacyValue,
                        canonicalValue = canonicalValue
                    )
                )
            }
        }
    }
}

// ── CanonicalReadRuntimeGateBlocker ──

enum class CanonicalReadRuntimeGateBlocker(val value: String) {
    BLOCKED_MODE("blockedMode"),
    CANONICAL_SNAPSHOT_MISSING("canonicalSnapshotMissing"),
    APPLY_RUNTIME_EVIDENCE_MISSING("applyRuntimeEvidenceMissing"),
    UPLOAD_RUNTIME_EVIDENCE_MISSING("uploadRuntimeEvidenceMissing"),
    INVENTORY_SNAPSHOT_MISSING("inventorySnapshotMissing"),
    PLAN_AUTHORITY_EVIDENCE_MISSING("planAuthorityEvidenceMissing"),
    EXISTENCE_TRUTH_EVIDENCE_MISSING("existenceTruthEvidenceMissing"),
    DIVERGENCE_PRESENT("divergencePresent"),
    LEGACY_FALLBACK_UNAVAILABLE("legacyFallbackUnavailable"),
    OTHER_DOMAIN_CONFLICT("otherDomainConflict"),
    RELEASE_DEFAULT_BUILD("releaseDefaultBuild"),
    DEBUG_INTERNAL_APPROVAL_MISSING("debugInternalApprovalMissing"),
    MANUAL_OWNER_APPROVAL_MISSING("manualOwnerApprovalMissing"),
    DIAGNOSTICS_NOT_REDACTED("diagnosticsNotRedacted"),
    READ_MAY_TRIGGER_SYNC_UPLOAD("readMayTriggerSyncUpload"),
    READ_MAY_MUTATE_STORE("readMayMutateStore"),
    PATH_CONTENT_LEAK_RISK("pathContentLeakRisk");

    companion object {
        fun fromValue(value: String): CanonicalReadRuntimeGateBlocker =
            entries.first { it.value == value }
    }
}

// ── CanonicalReadRuntimeGateResult ──

data class CanonicalReadRuntimeGateResult(
    val allowed: Boolean,
    val blockers: List<CanonicalReadRuntimeGateBlocker>,
    val diagnosticsSummary: String
) {
    companion object {
        fun evaluate(blockers: List<CanonicalReadRuntimeGateBlocker>): CanonicalReadRuntimeGateResult {
            val uniqueBlockers = blockers.distinct().sortedBy { it.value }
            return CanonicalReadRuntimeGateResult(
                allowed = uniqueBlockers.isEmpty(),
                blockers = uniqueBlockers,
                diagnosticsSummary = "allowed=${uniqueBlockers.isEmpty()},blockers=${uniqueBlockers.joinToString("+") { it.value }}"
            )
        }
    }
}

// ── Type 14: CanonicalReadRuntimeGate ──

object CanonicalReadRuntimeGate {

    fun evaluate(
        configuration: CanonicalReadRuntimeConfiguration,
        canonicalSnapshotAvailable: Boolean,
        diff: CanonicalReadRuntimeDiff?
    ): CanonicalReadRuntimeGateResult {
        val policy = configuration.policy
        val blockers = mutableListOf<CanonicalReadRuntimeGateBlocker>()

        if (configuration.mode == CanonicalReadRuntimeMode.BLOCKED) {
            blockers.add(CanonicalReadRuntimeGateBlocker.BLOCKED_MODE)
        }
        if (!canonicalSnapshotAvailable) {
            blockers.add(CanonicalReadRuntimeGateBlocker.CANONICAL_SNAPSHOT_MISSING)
        }
        if (!policy.applyRuntimeEvidenceValidForNonAudio) {
            blockers.add(CanonicalReadRuntimeGateBlocker.APPLY_RUNTIME_EVIDENCE_MISSING)
        }
        if (!policy.uploadRuntimeEvidenceValidForAudioStatus) {
            blockers.add(CanonicalReadRuntimeGateBlocker.UPLOAD_RUNTIME_EVIDENCE_MISSING)
        }
        if (!policy.inventorySnapshotAvailable) {
            blockers.add(CanonicalReadRuntimeGateBlocker.INVENTORY_SNAPSHOT_MISSING)
        }
        if (!policy.planAuthorityEvidenceValid) {
            blockers.add(CanonicalReadRuntimeGateBlocker.PLAN_AUTHORITY_EVIDENCE_MISSING)
        }
        if (!policy.existenceTruthEvidenceValid) {
            blockers.add(CanonicalReadRuntimeGateBlocker.EXISTENCE_TRUTH_EVIDENCE_MISSING)
        }
        if (!policy.legacyFallbackAvailable) {
            blockers.add(CanonicalReadRuntimeGateBlocker.LEGACY_FALLBACK_UNAVAILABLE)
        }
        if (!policy.otherDomainsNotConflicting) {
            blockers.add(CanonicalReadRuntimeGateBlocker.OTHER_DOMAIN_CONFLICT)
        }
        if (policy.releaseDefaultBuild) {
            blockers.add(CanonicalReadRuntimeGateBlocker.RELEASE_DEFAULT_BUILD)
        }
        if (!policy.debugInternalBuild || !policy.ownerApproved) {
            blockers.add(CanonicalReadRuntimeGateBlocker.DEBUG_INTERNAL_APPROVAL_MISSING)
        }
        if (!policy.manualOwnerApproval) {
            blockers.add(CanonicalReadRuntimeGateBlocker.MANUAL_OWNER_APPROVAL_MISSING)
        }
        if (!policy.diagnosticsRedacted) {
            blockers.add(CanonicalReadRuntimeGateBlocker.DIAGNOSTICS_NOT_REDACTED)
        }
        if (!policy.readMustNotTriggerSyncUpload) {
            blockers.add(CanonicalReadRuntimeGateBlocker.READ_MAY_TRIGGER_SYNC_UPLOAD)
        }
        if (!policy.readMustNotMutateStore) {
            blockers.add(CanonicalReadRuntimeGateBlocker.READ_MAY_MUTATE_STORE)
        }
        if (diff != null) {
            if (diff.divergenceCount > 0 && !policy.allowDivergentGuardedReadForTests) {
                blockers.add(CanonicalReadRuntimeGateBlocker.DIVERGENCE_PRESENT)
            }
            if (diff.divergences.any { it.kind == CanonicalReadRuntimeDivergenceKind.PATH_CONTENT_LEAK_RISK }) {
                blockers.add(CanonicalReadRuntimeGateBlocker.PATH_CONTENT_LEAK_RISK)
            }
        }

        return CanonicalReadRuntimeGateResult.evaluate(blockers)
    }
}

// ── CanonicalReadRuntimeDiagnosticKind ──

enum class CanonicalReadRuntimeDiagnosticKind(val value: String) {
    CANONICAL_READ_RUNTIME_MODE_EVALUATED("canonicalReadRuntimeModeEvaluated"),
    CANONICAL_READ_RUNTIME_SERVED_CANONICAL("canonicalReadRuntimeServedCanonical"),
    CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK("canonicalReadRuntimeServedLegacyFallback"),
    CANONICAL_READ_RUNTIME_DIFF_EQUIVALENT("canonicalReadRuntimeDiffEquivalent"),
    CANONICAL_READ_RUNTIME_DIFF_DIVERGENT("canonicalReadRuntimeDiffDivergent"),
    CANONICAL_READ_RUNTIME_BLOCKED("canonicalReadRuntimeBlocked"),
    CANONICAL_READ_RUNTIME_REPORT_BUILT("canonicalReadRuntimeReportBuilt");

    companion object {
        fun fromValue(value: String): CanonicalReadRuntimeDiagnosticKind =
            entries.first { it.value == value }
    }
}

// ── CanonicalReadRuntimeDiagnostic ──

data class CanonicalReadRuntimeDiagnostic(
    val kind: CanonicalReadRuntimeDiagnosticKind,
    val syncRunID: String?,
    val mode: CanonicalReadRuntimeMode,
    val source: CanonicalReadProjectionSource? = null,
    val count: Int? = null,
    val detail: String? = null
) {
    val id: String
        get() = listOfNotNull(
            kind.value,
            syncRunID ?: "",
            mode.value,
            detail
        ).joinToString("|")

    val isRedacted: Boolean
        get() = listOfNotNull(syncRunID, detail).all {
            !readableContainsForbiddenSignal(it)
        }

    val diagnosticsSummary: String
        get() = listOfNotNull(
            "kind=${kind.value}",
            "mode=${mode.value}",
            source?.let { "source=${it.value}" },
            count?.let { "count=$it" },
            detail?.let { "detail=$it" }
        ).joinToString(",")
}

// ── CanonicalReadProjectionFailureKind ──

enum class CanonicalReadProjectionFailureKind(val value: String) {
    SNAPSHOT_MISSING("snapshotMissing"),
    UNSUPPORTED_OBJECT("unsupportedObject"),
    PATH_CONTENT_LEAK_RISK("pathContentLeakRisk");

    companion object {
        fun fromValue(value: String): CanonicalReadProjectionFailureKind =
            entries.first { it.value == value }
    }
}

// ── CanonicalReadProjectionFailure ──

data class CanonicalReadProjectionFailure(
    val kind: CanonicalReadProjectionFailureKind,
    val domain: CanonicalReadDomain,
    val objectID: String? = null,
    val reason: String
) {
    val id: String
        get() = listOfNotNull(
            kind.value,
            domain.value,
            objectID ?: "run",
            reason
        ).joinToString("|")

    companion object {
        fun create(
            kind: CanonicalReadProjectionFailureKind,
            domain: CanonicalReadDomain,
            objectID: String? = null,
            reason: String
        ): CanonicalReadProjectionFailure = CanonicalReadProjectionFailure(
            kind = kind,
            domain = domain,
            objectID = readableSafeIdentifier(objectID, "object"),
            reason = readableSafeText(reason) ?: kind.value
        )
    }
}

// ── CanonicalUploadState ──

enum class CanonicalUploadState(val value: String) {
    NONE("none"),
    QUEUED("queued"),
    IN_FLIGHT("inFlight"),
    COMPLETED("completed"),
    FAILED("failed"),
    RETRY_PENDING("retryPending");

    companion object {
        fun fromValue(value: String): CanonicalUploadState =
            entries.first { it.value == value }
    }
}

// ── Type 5: CanonicalRecordingReadProjection ──

data class CanonicalRecordingReadProjectionRecord(
    val objectID: String,
    val title: String,
    val tags: List<String>,
    val folderSummary: String,
    val audioAvailable: Boolean,
    val uploadState: CanonicalUploadState,
    val hashPrefix: String?,
    val syncState: CanonicalSyncState,
    val transferState: CanonicalTransferState,
    val createdAtSummary: String,
    val modifiedAtSummary: String,
    val durationSeconds: Int?,
    val isDeleted: Boolean,
    val processingSummary: String
) {
    val id: String get() = objectID

    val tagsKey: String
        get() = tags.joinToString("|")

    companion object {
        fun create(
            objectID: String,
            title: String,
            tags: List<String> = emptyList(),
            folderSummary: String = "none",
            audioAvailable: Boolean = false,
            uploadState: CanonicalUploadState = CanonicalUploadState.NONE,
            hashPrefix: String? = null,
            syncState: CanonicalSyncState = CanonicalSyncState.UNKNOWN,
            transferState: CanonicalTransferState = CanonicalTransferState.NONE,
            createdAtSummary: String = "unknown",
            modifiedAtSummary: String = "unknown",
            durationSeconds: Int? = null,
            isDeleted: Boolean = false,
            processingSummary: String = "unknown"
        ): CanonicalRecordingReadProjectionRecord = CanonicalRecordingReadProjectionRecord(
            objectID = readableSafeIdentifier(objectID, "unknown-recording"),
            title = readableSafeDisplayText(title, "Untitled"),
            tags = tags.mapNotNull { readableSafeDisplayText(it, "") }.filter { it.isNotEmpty() }.sorted(),
            folderSummary = readableSafeText(folderSummary) ?: "none",
            audioAvailable = audioAvailable,
            uploadState = uploadState,
            hashPrefix = readableHashPrefix(hashPrefix),
            syncState = syncState,
            transferState = transferState,
            createdAtSummary = createdAtSummary,
            modifiedAtSummary = modifiedAtSummary,
            durationSeconds = durationSeconds?.coerceAtLeast(0),
            isDeleted = isDeleted,
            processingSummary = readableSafeText(processingSummary) ?: "unknown"
        )

        fun fromRecordingObject(obj: CanonicalRecordingObject): CanonicalRecordingReadProjectionRecord {
            val filing = obj.metadata.filing
            val folderSummary = buildString {
                filing?.type?.let { append("type=$it") }
                filing?.subject?.let { if (isNotEmpty()) append(","); append("subject=$it") }
                filing?.chapter?.let { if (isNotEmpty()) append(","); append("chapter=$it") }
                filing?.topic?.let { if (isNotEmpty()) append(","); append("topic=$it") }
                if (isEmpty()) append("none")
            }
            return CanonicalRecordingReadProjectionRecord(
                objectID = obj.objectID,
                title = obj.metadata.title,
                tags = obj.metadata.tags,
                folderSummary = folderSummary,
                audioAvailable = obj.audioAvailable,
                uploadState = CanonicalUploadState.NONE,
                hashPrefix = readableHashPrefix(obj.metadataHash.value),
                syncState = obj.syncState,
                transferState = obj.transferState,
                createdAtSummary = "unixSeconds=${obj.metadata.createdAt.date.time / 1000L}",
                modifiedAtSummary = "unixSeconds=${obj.metadata.modifiedAt.date.time / 1000L}",
                durationSeconds = obj.metadata.duration?.let { it.toInt().coerceAtLeast(0) },
                isDeleted = obj.metadata.isDeleted,
                processingSummary = "transcription=${obj.processingState.transcription.name},note=${obj.processingState.note.name}"
            )
        }
    }
}

data class CanonicalRecordingReadProjection(
    val source: CanonicalReadProjectionSource,
    val records: List<CanonicalRecordingReadProjectionRecord>,
    val failures: List<CanonicalReadProjectionFailure>
) {
    val diagnosticsSummary: String
        get() = "source=${source.value},records=${records.size},failures=${failures.size}"

    companion object {
        fun create(
            source: CanonicalReadProjectionSource,
            records: List<CanonicalRecordingReadProjectionRecord> = emptyList(),
            failures: List<CanonicalReadProjectionFailure> = emptyList()
        ): CanonicalRecordingReadProjection = CanonicalRecordingReadProjection(
            source = source,
            records = records.sortedBy { it.objectID },
            failures = failures.sortedBy { it.id }
        )

        fun build(
            source: CanonicalReadProjectionSource,
            manifest: CanonicalManifest?
        ): CanonicalRecordingReadProjection {
            if (manifest == null) {
                return create(
                    source = source,
                    failures = listOf(
                        CanonicalReadProjectionFailure.create(
                            kind = CanonicalReadProjectionFailureKind.SNAPSHOT_MISSING,
                            domain = CanonicalReadDomain.RECORDING_METADATA,
                            reason = "recordingManifestMissing"
                        )
                    )
                )
            }
            return create(
                source = source,
                records = manifest.objects.map {
                    CanonicalRecordingReadProjectionRecord.fromRecordingObject(it)
                }
            )
        }
    }
}

// ── Type 6: CanonicalLibraryReadProjection ──

data class CanonicalLibraryReadProjection(
    val source: CanonicalReadProjectionSource,
    val folders: Int,
    val foldersSummary: String,
    val studyItemCount: Int,
    val studyItemSummary: String
) {
    val diagnosticsSummary: String
        get() = "source=${source.value},folders=$folders,studyItems=$studyItemCount"

    companion object {
        fun build(
            source: CanonicalReadProjectionSource,
            manifest: CanonicalManifest?
        ): CanonicalLibraryReadProjection {
            if (manifest == null) {
                return CanonicalLibraryReadProjection(
                    source = source,
                    folders = 0,
                    foldersSummary = "none",
                    studyItemCount = 0,
                    studyItemSummary = "none"
                )
            }
            val folderCount = manifest.folders.size
            val folderSummary = manifest.folders.joinToString(",") { it.folderID.rawValue }
            val itemCount = manifest.studyItems.size + manifest.standaloneNotes.size
            val itemSummary = (manifest.studyItems.map { it.itemID.rawValue } +
                manifest.standaloneNotes.map { it.noteID.rawValue })
                .joinToString(",")
            return CanonicalLibraryReadProjection(
                source = source,
                folders = folderCount,
                foldersSummary = folderSummary,
                studyItemCount = itemCount,
                studyItemSummary = itemSummary
            )
        }
    }
}

// ── Type 7: CanonicalArtifactReadProjection ──

data class CanonicalArtifactReadProjection(
    val source: CanonicalReadProjectionSource,
    val generatedArtifactCount: Int,
    val generatedArtifactSummary: String
) {
    val diagnosticsSummary: String
        get() = "source=${source.value},artifacts=$generatedArtifactCount"

    companion object {
        fun build(
            source: CanonicalReadProjectionSource,
            manifest: CanonicalManifest?
        ): CanonicalArtifactReadProjection {
            if (manifest == null) {
                return CanonicalArtifactReadProjection(
                    source = source,
                    generatedArtifactCount = 0,
                    generatedArtifactSummary = "none"
                )
            }
            val generatedArtifacts = manifest.objects.flatMap { obj ->
                obj.artifacts.filter { it.isCanonicalGeneratedArtifact }
            }
            val summary = generatedArtifacts.joinToString(",") { a ->
                "${a.kind.name}:${a.objectID}"
            }
            return CanonicalArtifactReadProjection(
                source = source,
                generatedArtifactCount = generatedArtifacts.size,
                generatedArtifactSummary = summary
            )
        }
    }
}

// ── Type 8: CanonicalConflictReadProjection ──

data class CanonicalConflictReadProjection(
    val source: CanonicalReadProjectionSource,
    val conflictCount: Int,
    val conflictSummary: String
) {
    val diagnosticsSummary: String
        get() = "source=${source.value},conflicts=$conflictCount"

    companion object {
        fun build(
            source: CanonicalReadProjectionSource,
            manifest: CanonicalManifest?
        ): CanonicalConflictReadProjection {
            val tombstones = manifest?.libraryTombstones ?: emptyList()
            val summary = tombstones.joinToString(",") { t ->
                "${t.objectKind.name}:${t.objectID.rawValue}:${t.reason.name}"
            }
            return CanonicalConflictReadProjection(
                source = source,
                conflictCount = tombstones.size,
                conflictSummary = summary
            )
        }
    }
}

// ── Type 9: CanonicalUploadReadProjection ──

data class CanonicalUploadReadProjectionRecord(
    val objectID: String,
    val audioAvailable: Boolean,
    val uploadState: CanonicalUploadState,
    val byteSize: Long?,
    val hashPrefix: String?
) {
    val id: String get() = objectID

    companion object {
        fun create(
            objectID: String,
            audioAvailable: Boolean = false,
            uploadState: CanonicalUploadState = CanonicalUploadState.NONE,
            byteSize: Long? = null,
            hashPrefix: String? = null
        ): CanonicalUploadReadProjectionRecord = CanonicalUploadReadProjectionRecord(
            objectID = readableSafeIdentifier(objectID, "unknown-recording"),
            audioAvailable = audioAvailable,
            uploadState = uploadState,
            byteSize = byteSize,
            hashPrefix = readableHashPrefix(hashPrefix)
        )

        fun fromRecordingObject(obj: CanonicalRecordingObject): CanonicalUploadReadProjectionRecord {
            val audio = obj.audioArtifact
            return CanonicalUploadReadProjectionRecord(
                objectID = obj.objectID,
                audioAvailable = obj.audioAvailable,
                uploadState = CanonicalUploadState.NONE,
                byteSize = audio?.byteSize,
                hashPrefix = audio?.contentHash?.let { readableHashPrefix(it.value) }
            )
        }
    }
}

data class CanonicalUploadReadProjection(
    val source: CanonicalReadProjectionSource,
    val records: List<CanonicalUploadReadProjectionRecord>,
    val failures: List<CanonicalReadProjectionFailure>
) {
    val uploadStatusSummary: String
        get() {
            val available = records.count { it.audioAvailable }
            val uploading = records.count {
                it.uploadState == CanonicalUploadState.IN_FLIGHT ||
                    it.uploadState == CanonicalUploadState.QUEUED
            }
            return "source=${source.value},records=${records.size},audioAvailable=$available,uploading=$uploading,failures=${failures.size}"
        }

    val diagnosticsSummary: String
        get() = uploadStatusSummary

    companion object {
        fun create(
            source: CanonicalReadProjectionSource,
            records: List<CanonicalUploadReadProjectionRecord> = emptyList(),
            failures: List<CanonicalReadProjectionFailure> = emptyList()
        ): CanonicalUploadReadProjection = CanonicalUploadReadProjection(
            source = source,
            records = records.sortedBy { it.objectID },
            failures = failures.sortedBy { it.id }
        )

        fun build(
            source: CanonicalReadProjectionSource,
            manifest: CanonicalManifest?
        ): CanonicalUploadReadProjection {
            if (manifest == null) {
                return create(
                    source = source,
                    failures = listOf(
                        CanonicalReadProjectionFailure.create(
                            kind = CanonicalReadProjectionFailureKind.SNAPSHOT_MISSING,
                            domain = CanonicalReadDomain.AUDIO_UPLOAD_STATUS,
                            reason = "uploadManifestMissing"
                        )
                    )
                )
            }
            return create(
                source = source,
                records = manifest.objects.map {
                    CanonicalUploadReadProjectionRecord.fromRecordingObject(it)
                }
            )
        }
    }
}

// ── Type 10: CanonicalSyncEngineStatusReadProjection ──

data class CanonicalSyncEngineStatusReadProjection(
    val source: CanonicalReadProjectionSource,
    val mode: CanonicalReadRuntimeMode? = null,
    val syncRuntimeMode: CanonicalSyncRuntimeMode? = null,
    val canonicalPlanUsed: Boolean = false,
    val canonicalPlanFallback: Boolean = false,
    val canonicalPlanBlocked: Boolean = false,
    val canonicalPlanNoCommit: Boolean = false,
    val pendingTransferCount: Int = 0,
    val inFlightTransferCount: Int = 0,
    val failedTransferCount: Int = 0,
    val lastStatusSummary: String? = null,
    val syncOrUploadTriggeredByRead: Boolean = false
) {
    val syncSummary: String
        get() = listOfNotNull(
            "source=${source.value}",
            mode?.let { "readMode=${it.value}" },
            syncRuntimeMode?.let { "syncMode=${it.name}" },
            "canonicalPlanUsed=$canonicalPlanUsed",
            "fallback=$canonicalPlanFallback",
            "blocked=$canonicalPlanBlocked",
            "pending=$pendingTransferCount",
            "inFlight=$inFlightTransferCount",
            "failed=$failedTransferCount",
            "syncOrUploadTriggeredByRead=$syncOrUploadTriggeredByRead"
        ).joinToString(",")

    val diagnosticsSummary: String
        get() = syncSummary

    companion object {
        fun create(
            source: CanonicalReadProjectionSource,
            mode: CanonicalReadRuntimeMode? = null,
            syncRuntimeMode: CanonicalSyncRuntimeMode? = null,
            canonicalPlanUsed: Boolean = false,
            canonicalPlanFallback: Boolean = false,
            canonicalPlanBlocked: Boolean = false,
            canonicalPlanNoCommit: Boolean = false,
            pendingTransferCount: Int = 0,
            inFlightTransferCount: Int = 0,
            failedTransferCount: Int = 0,
            lastStatusSummary: String? = null
        ): CanonicalSyncEngineStatusReadProjection = CanonicalSyncEngineStatusReadProjection(
            source = source,
            mode = mode,
            syncRuntimeMode = syncRuntimeMode,
            canonicalPlanUsed = canonicalPlanUsed,
            canonicalPlanFallback = canonicalPlanFallback,
            canonicalPlanBlocked = canonicalPlanBlocked,
            canonicalPlanNoCommit = canonicalPlanNoCommit,
            pendingTransferCount = pendingTransferCount.coerceAtLeast(0),
            inFlightTransferCount = inFlightTransferCount.coerceAtLeast(0),
            failedTransferCount = failedTransferCount.coerceAtLeast(0),
            lastStatusSummary = readableSafeText(lastStatusSummary),
            syncOrUploadTriggeredByRead = false
        )
    }
}

// ── CanonicalReadSnapshotRedaction ──

data class CanonicalReadSnapshotRedaction(
    val excludesAbsolutePaths: Boolean = true,
    val excludesFullHashes: Boolean = true,
    val excludesSecrets: Boolean = true,
    val excludesFullGeneratedContent: Boolean = true,
    val excludesRequestResponseBodies: Boolean = true
) {
    val isRedacted: Boolean
        get() = excludesAbsolutePaths &&
            excludesFullHashes &&
            excludesSecrets &&
            excludesFullGeneratedContent &&
            excludesRequestResponseBodies

    companion object {
        val REDACTED = CanonicalReadSnapshotRedaction()
    }
}

// ── Type 11: CanonicalReadSnapshot ──

data class CanonicalReadSnapshot(
    val source: CanonicalReadProjectionSource,
    val generatedAt: CanonicalTimestamp,
    val recordingProjections: CanonicalRecordingReadProjection,
    val libraryProjection: CanonicalLibraryReadProjection,
    val artifactProjection: CanonicalArtifactReadProjection,
    val conflictProjection: CanonicalConflictReadProjection,
    val uploadProjection: CanonicalUploadReadProjection,
    val syncEngineStatus: CanonicalSyncEngineStatusReadProjection,
    val redaction: CanonicalReadSnapshotRedaction = CanonicalReadSnapshotRedaction.REDACTED
) {
    val pathOrContentLeakRisk: Boolean
        get() {
            if (!redaction.isRedacted) return true
            if (uploadProjection.records.any {
                    it.hashPrefix.isNullOrEmpty() && it.byteSize != null
                }
            ) return true
            return false
        }

    val diagnosticsSummary: String
        get() = listOf(
            "source=${source.value}",
            "recordings=${recordingProjections.records.size}",
            "folders=${libraryProjection.folders}",
            "items=${libraryProjection.studyItemCount}",
            "artifacts=${artifactProjection.generatedArtifactCount}",
            "conflicts=${conflictProjection.conflictCount}",
            "uploadRecords=${uploadProjection.records.size}",
            "redacted=${redaction.isRedacted}",
            "syncStatus=${syncEngineStatus.diagnosticsSummary}"
        ).joinToString(",")

    companion object {
        fun create(
            source: CanonicalReadProjectionSource,
            generatedAt: Date = Date(),
            recordingProjections: CanonicalRecordingReadProjection,
            libraryProjection: CanonicalLibraryReadProjection,
            artifactProjection: CanonicalArtifactReadProjection,
            conflictProjection: CanonicalConflictReadProjection,
            uploadProjection: CanonicalUploadReadProjection,
            syncEngineStatus: CanonicalSyncEngineStatusReadProjection,
            redaction: CanonicalReadSnapshotRedaction = CanonicalReadSnapshotRedaction.REDACTED
        ): CanonicalReadSnapshot = CanonicalReadSnapshot(
            source = source,
            generatedAt = CanonicalTimestamp(generatedAt),
            recordingProjections = recordingProjections,
            libraryProjection = libraryProjection,
            artifactProjection = artifactProjection,
            conflictProjection = conflictProjection,
            uploadProjection = uploadProjection,
            syncEngineStatus = syncEngineStatus,
            redaction = redaction
        )

        fun build(
            source: CanonicalReadProjectionSource,
            manifest: CanonicalManifest?,
            peerManifest: CanonicalManifest? = null,
            syncRuntimeMode: CanonicalSyncRuntimeMode? = null,
            generatedAt: Date = Date()
        ): CanonicalReadSnapshot = CanonicalReadSnapshot.create(
            source = source,
            generatedAt = generatedAt,
            recordingProjections = CanonicalRecordingReadProjection.build(source, manifest),
            libraryProjection = CanonicalLibraryReadProjection.build(source, manifest),
            artifactProjection = CanonicalArtifactReadProjection.build(source, manifest),
            conflictProjection = CanonicalConflictReadProjection.build(source, manifest),
            uploadProjection = CanonicalUploadReadProjection.build(source, manifest),
            syncEngineStatus = CanonicalSyncEngineStatusReadProjection.create(
                source = source,
                syncRuntimeMode = syncRuntimeMode
            )
        )
    }
}

// ── Type 4: CanonicalReadRuntimeResult ──

data class CanonicalReadRuntimeResult(
    val mode: CanonicalReadRuntimeMode,
    val canonicalSnapshot: CanonicalReadSnapshot?,
    val legacySnapshot: CanonicalReadSnapshot,
    val diff: CanonicalReadRuntimeDiff?,
    val servedCanonical: Boolean,
    val fallbackToLegacy: Boolean,
    val returnedSource: CanonicalReadProjectionSource,
    val readSnapshot: CanonicalReadSnapshot,
    val canonicalCandidate: CanonicalReadSnapshot?,
    val gateResult: CanonicalReadRuntimeGateResult?,
    val fallback: CanonicalReadRuntimeFallback,
    val canonicalReadServed: Boolean,
    val legacyFallbackServed: Boolean,
    val canonicalCandidateBuilt: Boolean,
    val storeMutated: Boolean,
    val syncOrUploadTriggered: Boolean,
    val uploadJobCreated: Boolean,
    val resourceMoved: Boolean,
    val productionDataWritten: Boolean,
    val diagnostics: List<CanonicalReadRuntimeDiagnostic>,
    val diagnosticsSummary: String
) {
    companion object {
        fun create(
            mode: CanonicalReadRuntimeMode,
            returnedSnapshot: CanonicalReadSnapshot,
            legacySnapshot: CanonicalReadSnapshot,
            canonicalCandidate: CanonicalReadSnapshot?,
            diff: CanonicalReadRuntimeDiff?,
            gateResult: CanonicalReadRuntimeGateResult?,
            fallback: CanonicalReadRuntimeFallback,
            diagnostics: List<CanonicalReadRuntimeDiagnostic>
        ): CanonicalReadRuntimeResult {
            val canonicalServed =
                returnedSnapshot.source == CanonicalReadProjectionSource.CANONICAL &&
                    fallback == CanonicalReadRuntimeFallback.NONE
            return CanonicalReadRuntimeResult(
                mode = mode,
                canonicalSnapshot = canonicalCandidate,
                legacySnapshot = legacySnapshot,
                diff = diff,
                servedCanonical = canonicalServed,
                fallbackToLegacy = returnedSnapshot.source == CanonicalReadProjectionSource.LEGACY &&
                    fallback != CanonicalReadRuntimeFallback.NONE,
                returnedSource = returnedSnapshot.source,
                readSnapshot = returnedSnapshot,
                canonicalCandidate = canonicalCandidate,
                gateResult = gateResult,
                fallback = fallback,
                canonicalReadServed = canonicalServed,
                legacyFallbackServed = returnedSnapshot.source == CanonicalReadProjectionSource.LEGACY &&
                    fallback != CanonicalReadRuntimeFallback.NONE,
                canonicalCandidateBuilt = canonicalCandidate != null && mode.buildsCanonicalCandidate,
                storeMutated = false,
                syncOrUploadTriggered = false,
                uploadJobCreated = false,
                resourceMoved = false,
                productionDataWritten = false,
                diagnostics = diagnostics,
                diagnosticsSummary = listOf(
                    "mode=${mode.value}",
                    "returned=${returnedSnapshot.source.value}",
                    "fallback=${fallback.value}",
                    "canonicalServed=$canonicalServed",
                    "canonicalCandidateBuilt=${canonicalCandidate != null && mode.buildsCanonicalCandidate}",
                    "divergences=${diff?.divergenceCount ?: 0}",
                    "storeMutated=false",
                    "syncOrUploadTriggered=false",
                    "uploadJobCreated=false",
                    "resourceMoved=false",
                    "productionDataWritten=false"
                ).joinToString(",")
            )
        }
    }
}

// ── Type 15: CanonicalReadRuntimeProvider ──

class CanonicalReadRuntimeProvider(
    val configuration: CanonicalReadRuntimeConfiguration = CanonicalReadRuntimeConfiguration.DISABLED
) {
    fun provide(): CanonicalReadRuntimeProvider = this

    fun read(
        legacySnapshot: CanonicalReadSnapshot,
        canonicalSnapshot: CanonicalReadSnapshot?,
        syncRunID: String? = null,
        canonicalReadFailureReason: String? = null
    ): CanonicalReadRuntimeResult {
        val mode = configuration.mode
        val evaluated = makeDiagnostic(
            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_MODE_EVALUATED,
            syncRunID = syncRunID,
            source = null,
            count = null,
            detail = "mode=${mode.value}"
        )

        return when (mode) {
            CanonicalReadRuntimeMode.DISABLED -> makeResult(
                returnedSnapshot = legacySnapshot,
                legacySnapshot = legacySnapshot,
                canonicalSnapshot = null,
                diff = null,
                gate = null,
                fallback = CanonicalReadRuntimeFallback.LEGACY_DEFAULT,
                diagnostics = listOf(
                    evaluated,
                    makeDiagnostic(
                        CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
                        syncRunID = syncRunID,
                        source = CanonicalReadProjectionSource.LEGACY,
                        detail = "disabledDefaultLegacy"
                    )
                )
            )

            CanonicalReadRuntimeMode.BLOCKED -> {
                val diff = canonicalSnapshot?.let {
                    CanonicalReadRuntimeDiff.compare(legacy = legacySnapshot, canonical = it)
                }
                makeResult(
                    returnedSnapshot = legacySnapshot,
                    legacySnapshot = legacySnapshot,
                    canonicalSnapshot = canonicalSnapshot,
                    diff = diff,
                    gate = CanonicalReadRuntimeGateResult.evaluate(
                        blockers = listOf(CanonicalReadRuntimeGateBlocker.BLOCKED_MODE)
                    ),
                    fallback = CanonicalReadRuntimeFallback.BLOCKED_MODE,
                    diagnostics = listOf(
                        evaluated,
                        makeDiagnostic(
                            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_BLOCKED,
                            syncRunID = syncRunID,
                            source = CanonicalReadProjectionSource.LEGACY,
                            count = 1,
                            detail = "blockedMode"
                        ),
                        makeDiagnostic(
                            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
                            syncRunID = syncRunID,
                            source = CanonicalReadProjectionSource.LEGACY,
                            detail = "blockedMode"
                        )
                    ) + diffDiagnostics(diff, syncRunID)
                )
            }

            CanonicalReadRuntimeMode.PARALLEL_COMPARE,
            CanonicalReadRuntimeMode.CANONICAL_READ_CANDIDATE -> {
                val diff = canonicalSnapshot?.let {
                    CanonicalReadRuntimeDiff.compare(legacy = legacySnapshot, canonical = it)
                }
                val fallback = if (mode == CanonicalReadRuntimeMode.PARALLEL_COMPARE)
                    CanonicalReadRuntimeFallback.PARALLEL_COMPARE_RETURNS_LEGACY
                else
                    CanonicalReadRuntimeFallback.CANONICAL_CANDIDATE_NOT_SERVED
                val reason = if (mode == CanonicalReadRuntimeMode.PARALLEL_COMPARE)
                    "parallelCompareReturnsLegacy"
                else
                    "canonicalCandidateNotServed"
                makeResult(
                    returnedSnapshot = legacySnapshot,
                    legacySnapshot = legacySnapshot,
                    canonicalSnapshot = canonicalSnapshot,
                    diff = diff,
                    gate = null,
                    fallback = fallback,
                    diagnostics = listOf(
                        evaluated,
                        makeDiagnostic(
                            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
                            syncRunID = syncRunID,
                            source = CanonicalReadProjectionSource.LEGACY,
                            detail = reason
                        )
                    ) + diffDiagnostics(diff, syncRunID)
                )
            }

            CanonicalReadRuntimeMode.GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK -> {
                if (canonicalSnapshot == null) {
                    makeResult(
                        returnedSnapshot = legacySnapshot,
                        legacySnapshot = legacySnapshot,
                        canonicalSnapshot = null,
                        diff = null,
                        gate = CanonicalReadRuntimeGate.evaluate(
                            configuration = configuration,
                            canonicalSnapshotAvailable = false,
                            diff = null
                        ),
                        fallback = CanonicalReadRuntimeFallback.CANONICAL_PROJECTION_MISSING,
                        diagnostics = listOf(
                            evaluated,
                            makeDiagnostic(
                                CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_BLOCKED,
                                syncRunID = syncRunID,
                                source = CanonicalReadProjectionSource.LEGACY,
                                count = 1,
                                detail = "canonicalProjectionMissing"
                            ),
                            makeDiagnostic(
                                CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
                                syncRunID = syncRunID,
                                source = CanonicalReadProjectionSource.LEGACY,
                                detail = "canonicalProjectionMissing"
                            )
                        )
                    )
                }

                val diff = CanonicalReadRuntimeDiff.compare(
                    legacy = legacySnapshot!!, canonical = canonicalSnapshot!!
                )
                val gate = CanonicalReadRuntimeGate.evaluate(
                    configuration = configuration,
                    canonicalSnapshotAvailable = true,
                    diff = diff
                )
                val diagnostics = mutableListOf(evaluated)
                diagnostics.addAll(diffDiagnostics(diff, syncRunID))

                if (canonicalReadFailureReason != null) {
                    diagnostics.add(
                        makeDiagnostic(
                            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
                            syncRunID = syncRunID,
                            source = CanonicalReadProjectionSource.LEGACY,
                            detail = canonicalReadFailureReason
                        )
                    )
                    return makeResult(
                        returnedSnapshot = legacySnapshot,
                        legacySnapshot = legacySnapshot,
                        canonicalSnapshot = canonicalSnapshot,
                        diff = diff,
                        gate = gate,
                        fallback = CanonicalReadRuntimeFallback.CANONICAL_READ_EXCEPTION,
                        diagnostics = diagnostics
                    )
                }

                if (!gate.allowed) {
                    diagnostics.add(
                        makeDiagnostic(
                            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_BLOCKED,
                            syncRunID = syncRunID,
                            source = CanonicalReadProjectionSource.LEGACY,
                            count = gate.blockers.size,
                            detail = gate.blockers.joinToString("+") { it.value }
                        )
                    )
                    diagnostics.add(
                        makeDiagnostic(
                            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
                            syncRunID = syncRunID,
                            source = CanonicalReadProjectionSource.LEGACY,
                            detail = "guardedGateBlocked"
                        )
                    )
                    return makeResult(
                        returnedSnapshot = legacySnapshot,
                        legacySnapshot = legacySnapshot,
                        canonicalSnapshot = canonicalSnapshot,
                        diff = diff,
                        gate = gate,
                        fallback = CanonicalReadRuntimeFallback.GUARDED_GATE_BLOCKED,
                        diagnostics = diagnostics
                    )
                }

                diagnostics.add(
                    makeDiagnostic(
                        CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_CANONICAL,
                        syncRunID = syncRunID,
                        source = CanonicalReadProjectionSource.CANONICAL,
                        detail = "guardedCanonicalReadWithLegacyFallback"
                    )
                )

                makeResult(
                    returnedSnapshot = canonicalSnapshot!!,
                    legacySnapshot = legacySnapshot!!,
                    canonicalSnapshot = canonicalSnapshot!!,
                    diff = diff,
                    gate = gate,
                    fallback = CanonicalReadRuntimeFallback.NONE,
                    diagnostics = diagnostics
                )
            }
        }
    }

    private fun makeResult(
        returnedSnapshot: CanonicalReadSnapshot,
        legacySnapshot: CanonicalReadSnapshot,
        canonicalSnapshot: CanonicalReadSnapshot?,
        diff: CanonicalReadRuntimeDiff?,
        gate: CanonicalReadRuntimeGateResult?,
        fallback: CanonicalReadRuntimeFallback,
        diagnostics: List<CanonicalReadRuntimeDiagnostic>
    ): CanonicalReadRuntimeResult {
        val mutableDiagnostics = diagnostics.toMutableList()
        mutableDiagnostics.add(
            makeDiagnostic(
                CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_REPORT_BUILT,
                syncRunID = mutableDiagnostics.firstOrNull()?.syncRunID,
                source = returnedSnapshot.source,
                count = diff?.divergenceCount,
                detail = "fallback=${fallback.value}"
            )
        )
        val limitedDiagnostics = mutableDiagnostics.take(configuration.policy.maxDiagnosticsEvents)
        return CanonicalReadRuntimeResult.create(
            mode = configuration.mode,
            returnedSnapshot = returnedSnapshot,
            legacySnapshot = legacySnapshot,
            canonicalCandidate = canonicalSnapshot,
            diff = diff,
            gateResult = gate,
            fallback = fallback,
            diagnostics = limitedDiagnostics
        )
    }

    private fun diffDiagnostics(
        diff: CanonicalReadRuntimeDiff?,
        syncRunID: String?
    ): List<CanonicalReadRuntimeDiagnostic> {
        val d = diff ?: return emptyList()
        return listOf(
            makeDiagnostic(
                kind = if (d.equivalent)
                    CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_DIFF_EQUIVALENT
                else
                    CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_DIFF_DIVERGENT,
                syncRunID = syncRunID,
                source = CanonicalReadProjectionSource.CANONICAL,
                count = d.divergenceCount,
                detail = d.equivalenceReport.diagnosticsSummary
            )
        )
    }

    private fun makeDiagnostic(
        kind: CanonicalReadRuntimeDiagnosticKind,
        syncRunID: String?,
        source: CanonicalReadProjectionSource?,
        count: Int? = null,
        detail: String? = null
    ): CanonicalReadRuntimeDiagnostic = CanonicalReadRuntimeDiagnostic(
        kind = kind,
        syncRunID = syncRunID,
        mode = configuration.mode,
        source = source,
        count = count,
        detail = detail
    )
}

// ── Redaction helpers (file-private) ──

private fun readableSafeIdentifier(value: String?, fallback: String): String =
    readableSafeText(value) ?: fallback

private fun readableSafeDisplayText(value: String, fallback: String): String =
    readableSafeText(value) ?: fallback

private fun readableSafeText(value: String?): String? {
    if (value == null) return null
    val trimmed = value.trim()
    if (trimmed.isEmpty()) return null
    if (containsForbiddenSignal(trimmed)) return null
    return trimmed.take(320)
}

private fun readableHashPrefix(value: String?): String? {
    if (value == null) return null
    val trimmed = value.trim().lowercase()
    if (trimmed.isEmpty()) return null
    return trimmed.take(8)
}

private fun readableContainsForbiddenSignal(value: String): Boolean =
    containsSensitivePathSignal(value) ||
        value.contains("{") ||
        value.contains("}") ||
        value.contains("://") ||
        value.length > 320

private fun containsSensitivePathSignal(value: String): Boolean =
    value.contains("/") || value.contains("\\")

private fun containsForbiddenSignal(value: String): Boolean =
    readableContainsForbiddenSignal(value)
