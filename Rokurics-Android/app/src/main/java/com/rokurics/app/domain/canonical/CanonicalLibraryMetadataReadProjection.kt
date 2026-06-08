package com.rokurics.app.domain.canonical

data class CanonicalLibraryMetadataReadProjection(
    val folders: List<CanonicalFolderObject> = emptyList(),
    val studyItems: List<CanonicalStudyItemObject> = emptyList(),
    val standaloneNotes: List<CanonicalStandaloneNoteObject> = emptyList(),
    val timestamps: CanonicalLibraryMetadataReadTimestamps = CanonicalLibraryMetadataReadTimestamps(),
    val projectionID: String = java.util.UUID.randomUUID().toString(),
    val sourceMode: CanonicalLibraryMetadataReadSourceMode = CanonicalLibraryMetadataReadSourceMode.GUARDED_CANONICAL,
    val diagnosticsRedacted: Boolean = true
) {

    val totalObjectCount: Int
        get() = folders.size + studyItems.size + standaloneNotes.size

    val isEmpty: Boolean
        get() = totalObjectCount == 0

    val diagnosticsSummary: String
        get() = listOf(
            "folders=${folders.size}",
            "studyItems=${studyItems.size}",
            "standaloneNotes=${standaloneNotes.size}",
            "total=$totalObjectCount",
            "source=${sourceMode.rawValue}",
            "builtAt=${timestamps.builtAt}",
            "observedAt=${timestamps.observedAt}"
        ).joinToString(",")
}

data class CanonicalLibraryMetadataReadTimestamps(
    val builtAt: String? = null,
    val observedAt: String? = null,
    val legacyReadAt: String? = null,
    val canonicalReadAt: String? = null
) {
}

data class CanonicalLibraryMetadataReadDiff(
    val divergences: List<CanonicalLibraryMetadataReadDivergence>,
    val legacyProjectionID: String?,
    val canonicalProjectionID: String?,
    val comparedAt: String?,
    val diagnosticsSummary: String
) {
    val equivalent: Boolean get() = divergences.isEmpty()

    companion object {
        fun equivalent(
            legacyProjectionID: String?,
            canonicalProjectionID: String?
        ): CanonicalLibraryMetadataReadDiff {
            return CanonicalLibraryMetadataReadDiff(
                divergences = emptyList(),
                legacyProjectionID = legacyProjectionID,
                canonicalProjectionID = canonicalProjectionID,
                comparedAt = null,
                diagnosticsSummary = "equivalent=true|divergences=0|legacy=${legacyProjectionID ?: "none"}|canonical=${canonicalProjectionID ?: "none"}"
            )
        }

        fun divergent(
            divergences: List<CanonicalLibraryMetadataReadDivergence>,
            legacyProjectionID: String? = null,
            canonicalProjectionID: String? = null
        ): CanonicalLibraryMetadataReadDiff {
            return CanonicalLibraryMetadataReadDiff(
                divergences = divergences,
                legacyProjectionID = legacyProjectionID,
                canonicalProjectionID = canonicalProjectionID,
                comparedAt = null,
                diagnosticsSummary = "equivalent=false|divergences=${divergences.size}|legacy=${legacyProjectionID ?: "none"}|canonical=${canonicalProjectionID ?: "none"}"
            )
        }
    }
}

data class CanonicalLibraryMetadataReadDivergence(
    val objectID: String,
    val kind: String,
    val divergenceType: CanonicalLibraryMetadataReadDivergenceType,
    val legacyValue: String? = null,
    val canonicalValue: String? = null,
    val field: String? = null,
    val blocking: Boolean = false,
    val diagnosticsSummary: String = ""
) {
}

enum class CanonicalLibraryMetadataReadDivergenceType(val rawValue: String) {
    METADATA_HASH_MISMATCH("metadataHashMismatch"),
    OBJECT_MISSING_FROM_CANONICAL("objectMissingFromCanonical"),
    OBJECT_MISSING_FROM_LEGACY("objectMissingFromLegacy"),
    FOLDER_NAME_MISMATCH("folderNameMismatch"),
    FOLDER_PARENT_MISMATCH("folderParentMismatch"),
    FOLDER_HIERARCHY_MISMATCH("folderHierarchyMismatch"),
    STUDY_ITEM_TITLE_MISMATCH("studyItemTitleMismatch"),
    STUDY_ITEM_FOLDER_MISMATCH("studyItemFolderMismatch"),
    STUDY_ITEM_KIND_MISMATCH("studyItemKindMismatch"),
    STUDY_ITEM_TAGS_MISMATCH("studyItemTagsMismatch"),
    STANDALONE_NOTE_TITLE_MISMATCH("standaloneNoteTitleMismatch"),
    STANDALONE_NOTE_FOLDER_MISMATCH("standaloneNoteFolderMismatch"),
    STANDALONE_NOTE_TAGS_MISMATCH("standaloneNoteTagsMismatch"),
    FILING_PATH_MISMATCH("filingPathMismatch"),
    DELETED_STATE_MISMATCH("deletedStateMismatch"),
    BUSINESS_MODIFIED_AT_MISMATCH("businessModifiedAtMismatch"),
    UNKNOWN("unknown")
}

enum class CanonicalLibraryMetadataReadSourceMode(val rawValue: String) {
    LEGACY("legacy"),
    GUARDED_CANONICAL("guardedCanonical"),
    BLOCKED("blocked")
}

class CanonicalLibraryMetadataReadSourceProvider(
    private val configuration: CanonicalReadRuntimeConfiguration,
    private val sourceGate: CanonicalLibraryMetadataReadSourceGate
) {
    data class ReadSourceResult(
        val mode: CanonicalLibraryMetadataReadSourceMode,
        val projection: CanonicalLibraryMetadataReadProjection?,
        val legacyProjection: CanonicalLibraryMetadataReadProjection?,
        val diff: CanonicalLibraryMetadataReadDiff?,
        val usedCanonical: Boolean,
        val usedLegacy: Boolean,
        val blocked: Boolean,
        val blockers: List<String>,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun blocked(
                mode: CanonicalLibraryMetadataReadSourceMode,
                blockers: List<String>
            ): ReadSourceResult {
                return ReadSourceResult(
                    mode = mode,
                    projection = null,
                    legacyProjection = null,
                    diff = null,
                    usedCanonical = false,
                    usedLegacy = false,
                    blocked = true,
                    blockers = blockers.sorted(),
                    diagnosticsSummary = listOf(
                        "blocked=true",
                        "mode=${mode.rawValue}",
                        "blockers=${blockers.joinToString("|")}"
                    ).joinToString(",")
                )
            }
        }
    }

    fun provide(
        canonicalProjection: CanonicalLibraryMetadataReadProjection?,
        legacyProjection: CanonicalLibraryMetadataReadProjection?
    ): ReadSourceResult {
        if (!sourceGate.allowed) {
            return ReadSourceResult.blocked(
                CanonicalLibraryMetadataReadSourceMode.BLOCKED,
                sourceGate.blockers
            )
        }

        if (configuration.mode == CanonicalReadRuntimeMode.DISABLED) {
            return ReadSourceResult.blocked(
                CanonicalLibraryMetadataReadSourceMode.BLOCKED,
                listOf("readRuntimeDisabled")
            )
        }

        if (configuration.mode == CanonicalReadRuntimeMode.BLOCKED) {
            return ReadSourceResult.blocked(
                CanonicalLibraryMetadataReadSourceMode.BLOCKED,
                listOf("readRuntimeBlocked")
            )
        }

        val mode = when (configuration.mode) {
            CanonicalReadRuntimeMode.GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK ->
                CanonicalLibraryMetadataReadSourceMode.GUARDED_CANONICAL
            CanonicalReadRuntimeMode.PARALLEL_COMPARE ->
                CanonicalLibraryMetadataReadSourceMode.GUARDED_CANONICAL
            else -> CanonicalLibraryMetadataReadSourceMode.LEGACY
        }

        if (configuration.mode == CanonicalReadRuntimeMode.PARALLEL_COMPARE) {
            val diff = if (canonicalProjection != null && legacyProjection != null) {
                computeDiff(canonicalProjection, legacyProjection)
            } else {
                null
            }

            val canonicalAvailable = canonicalProjection != null && !canonicalProjection.isEmpty
            val legacyAvailable = legacyProjection != null && !legacyProjection.isEmpty

            if (!canonicalAvailable && !legacyAvailable) {
                return ReadSourceResult.blocked(mode, listOf("noProjectionsAvailable"))
            }

            return ReadSourceResult(
                mode = mode,
                projection = legacyProjection,
                legacyProjection = legacyProjection,
                diff = diff,
                usedCanonical = false,
                usedLegacy = true,
                blocked = false,
                blockers = emptyList(),
                diagnosticsSummary = listOf(
                    "mode=${mode.rawValue}",
                    "canonicalAvailable=$canonicalAvailable",
                    "legacyAvailable=$legacyAvailable",
                    "equivalent=${diff?.equivalent ?: false}",
                    "divergences=${diff?.divergences?.size ?: 0}"
                ).joinToString(",")
            )
        }

        val useCanonical = mode == CanonicalLibraryMetadataReadSourceMode.GUARDED_CANONICAL &&
                canonicalProjection != null &&
                configuration.policy.ownerApproved &&
                configuration.policy.legacyFallbackAvailable

        if (!useCanonical && (legacyProjection == null || legacyProjection.isEmpty)) {
            return ReadSourceResult.blocked(mode, listOf("noProjectionsAvailable"))
        }

        val projection = if (useCanonical) canonicalProjection else legacyProjection

        return ReadSourceResult(
            mode = mode,
            projection = projection,
            legacyProjection = legacyProjection,
            diff = null,
            usedCanonical = useCanonical,
            usedLegacy = !useCanonical,
            blocked = false,
            blockers = emptyList(),
            diagnosticsSummary = listOf(
                "mode=${mode.rawValue}",
                "usedCanonical=$useCanonical",
                "usedLegacy=${!useCanonical}",
                "totalObjects=${projection?.totalObjectCount ?: 0}"
            ).joinToString(",")
        )
    }

    private fun computeDiff(
        canonical: CanonicalLibraryMetadataReadProjection,
        legacy: CanonicalLibraryMetadataReadProjection
    ): CanonicalLibraryMetadataReadDiff {
        val divergences = mutableListOf<CanonicalLibraryMetadataReadDivergence>()

        val canonicalFolderIDs = canonical.folders.map { it.folderID.rawValue }.toSet()
        val legacyFolderIDs = legacy.folders.map { it.folderID.rawValue }.toSet()

        for (id in canonicalFolderIDs - legacyFolderIDs) {
            divergences.add(
                CanonicalLibraryMetadataReadDivergence(
                    objectID = id,
                    kind = "folder",
                    divergenceType = CanonicalLibraryMetadataReadDivergenceType.OBJECT_MISSING_FROM_LEGACY,
                    blocking = true
                )
            )
        }

        for (id in legacyFolderIDs - canonicalFolderIDs) {
            divergences.add(
                CanonicalLibraryMetadataReadDivergence(
                    objectID = id,
                    kind = "folder",
                    divergenceType = CanonicalLibraryMetadataReadDivergenceType.OBJECT_MISSING_FROM_CANONICAL,
                    blocking = true
                )
            )
        }

        val canonicalStudyIDs = canonical.studyItems.map { it.itemID.rawValue }.toSet()
        val legacyStudyIDs = legacy.studyItems.map { it.itemID.rawValue }.toSet()

        for (id in canonicalStudyIDs - legacyStudyIDs) {
            divergences.add(
                CanonicalLibraryMetadataReadDivergence(
                    objectID = id,
                    kind = "studyItem",
                    divergenceType = CanonicalLibraryMetadataReadDivergenceType.OBJECT_MISSING_FROM_LEGACY,
                    blocking = true
                )
            )
        }

        for (id in legacyStudyIDs - canonicalStudyIDs) {
            divergences.add(
                CanonicalLibraryMetadataReadDivergence(
                    objectID = id,
                    kind = "studyItem",
                    divergenceType = CanonicalLibraryMetadataReadDivergenceType.OBJECT_MISSING_FROM_CANONICAL,
                    blocking = true
                )
            )
        }

        val canonicalNoteIDs = canonical.standaloneNotes.map { it.noteID.rawValue }.toSet()
        val legacyNoteIDs = legacy.standaloneNotes.map { it.noteID.rawValue }.toSet()

        for (id in canonicalNoteIDs - legacyNoteIDs) {
            divergences.add(
                CanonicalLibraryMetadataReadDivergence(
                    objectID = id,
                    kind = "standaloneNote",
                    divergenceType = CanonicalLibraryMetadataReadDivergenceType.OBJECT_MISSING_FROM_LEGACY,
                    blocking = true
                )
            )
        }

        for (id in legacyNoteIDs - canonicalNoteIDs) {
            divergences.add(
                CanonicalLibraryMetadataReadDivergence(
                    objectID = id,
                    kind = "standaloneNote",
                    divergenceType = CanonicalLibraryMetadataReadDivergenceType.OBJECT_MISSING_FROM_CANONICAL,
                    blocking = true
                )
            )
        }

        return if (divergences.isEmpty()) {
            CanonicalLibraryMetadataReadDiff.equivalent(
                legacyProjectionID = legacy.projectionID,
                canonicalProjectionID = canonical.projectionID
            )
        } else {
            CanonicalLibraryMetadataReadDiff.divergent(
                divergences = divergences,
                legacyProjectionID = legacy.projectionID,
                canonicalProjectionID = canonical.projectionID
            )
        }
    }
}

data class CanonicalLibraryMetadataReadSourceGate(
    val allowed: Boolean = false,
    val blockers: List<String> = emptyList(),
    val sourceMode: CanonicalLibraryMetadataReadSourceMode = CanonicalLibraryMetadataReadSourceMode.GUARDED_CANONICAL,
    val gateID: String = java.util.UUID.randomUUID().toString(),
    val diagnosticsSummary: String = ""
) {

    companion object {
        fun allowed(sourceMode: CanonicalLibraryMetadataReadSourceMode): CanonicalLibraryMetadataReadSourceGate {
            return CanonicalLibraryMetadataReadSourceGate(
                allowed = true,
                sourceMode = sourceMode
            )
        }

        fun blocked(
            blockers: List<String>,
            sourceMode: CanonicalLibraryMetadataReadSourceMode = CanonicalLibraryMetadataReadSourceMode.BLOCKED
        ): CanonicalLibraryMetadataReadSourceGate {
            return CanonicalLibraryMetadataReadSourceGate(
                allowed = false,
                blockers = blockers,
                sourceMode = sourceMode
            )
        }
    }
}

data class CanonicalLibraryMetadataRetirementCandidateGate(
    val ready: Boolean = false,
    val blockers: List<String> = emptyList(),
    val gateID: String = java.util.UUID.randomUUID().toString(),
    val sourceGateAllowed: Boolean = false,
    val readProjectionAvailable: Boolean = false,
    val readDiffEquivalent: Boolean = false,
    val canaryStagesComplete: Boolean = false,
    val observationPeriodSatisfied: Boolean = false,
    val legacyCodePathNoLongerNeeded: Boolean = false,
    val diagnosticsSummary: String = ""
) {

    companion object {
        fun evaluate(
            sourceGate: CanonicalLibraryMetadataReadSourceGate,
            readProjectionAvailable: Boolean,
            readDiff: CanonicalLibraryMetadataReadDiff?,
            canaryResults: List<CanonicalLibraryMetadataCanaryStageRunner.CanaryStageResult>
        ): CanonicalLibraryMetadataRetirementCandidateGate {
            val blockers = mutableListOf<String>()

            if (!sourceGate.allowed) {
                blockers.add("sourceGateBlocked")
            }
            if (!readProjectionAvailable) {
                blockers.add("readProjectionUnavailable")
            }
            if (readDiff != null && !readDiff.equivalent) {
                blockers.add("readDiffNotEquivalent")
            }
            if (canaryResults.isEmpty() || canaryResults.any { !it.allSucceeded }) {
                blockers.add("canaryStagesIncomplete")
            }

            return CanonicalLibraryMetadataRetirementCandidateGate(
                ready = blockers.isEmpty(),
                blockers = blockers,
                sourceGateAllowed = sourceGate.allowed,
                readProjectionAvailable = readProjectionAvailable,
                readDiffEquivalent = readDiff?.equivalent ?: false,
                canaryStagesComplete = canaryResults.isNotEmpty() && canaryResults.all { it.allSucceeded },
                observationPeriodSatisfied = true,
                legacyCodePathNoLongerNeeded = blockers.isEmpty()
            )
        }
    }
}
