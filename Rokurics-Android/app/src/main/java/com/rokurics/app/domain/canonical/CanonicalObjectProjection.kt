package com.rokurics.app.domain.canonical

import java.util.Date

enum class CanonicalDisplayState {
    LOCAL_ONLY,
    WAITING_FOR_AUDIO,
    UPLOADING_AUDIO,
    AUDIO_AVAILABLE,
    METADATA_SYNCED,
    TRANSCRIPT_AVAILABLE,
    NOTE_AVAILABLE,
    SUMMARY_AVAILABLE,
    PROCESSING,
    FAILED,
    RETRY_PENDING,
    CONFLICT,
    DELETED,
    TOMBSTONED,
    AVAILABLE,
    SYNCING,
    UNSUPPORTED,
    UNKNOWN
}

data class CanonicalActionAvailability(
    val canUploadAudio: Boolean,
    val canRequestGeneratedArtifact: Boolean,
    val canApplyMetadata: Boolean,
    val canResolveConflict: Boolean
) {
    companion object {
        val READ_ONLY = CanonicalActionAvailability(
            canUploadAudio = false,
            canRequestGeneratedArtifact = false,
            canApplyMetadata = false,
            canResolveConflict = false
        )
    }
}

data class CanonicalRecordingProjection(
    val objectID: String,
    val title: String,
    val displayStates: List<CanonicalDisplayState>,
    val actionAvailability: CanonicalActionAvailability,
    val metadataHashPrefix: String?,
    val audioHashPrefix: String?
)

data class CanonicalFolderProjection(
    val folderID: CanonicalLibraryObjectID,
    val title: String,
    val displayState: CanonicalDisplayState,
    val actionAvailability: CanonicalActionAvailability
)

data class CanonicalStudyItemProjection(
    val itemID: CanonicalLibraryObjectID,
    val title: String,
    val displayState: CanonicalDisplayState,
    val actionAvailability: CanonicalActionAvailability
)

data class CanonicalLibraryProjection(
    val recordings: List<CanonicalRecordingProjection>,
    val folders: List<CanonicalFolderProjection>,
    val studyItems: List<CanonicalStudyItemProjection>,
    val builtAt: CanonicalTimestamp
)

object CanonicalObjectProjectionBuilder {

    fun build(
        manifest: CanonicalManifest,
        applyPlan: CanonicalApplyPlan? = null,
        libraryPlan: CanonicalLibrarySyncPlan? = null,
        transferProjection: CanonicalTransferProjection? = null,
        builtAt: Date = Date()
    ): CanonicalLibraryProjection {
        val conflicts = (applyPlan?.conflicts ?: emptyList())
            .map { it.target.objectID }
            .toSet()
        val libraryConflicts = (libraryPlan?.conflicts ?: emptyList())
            .map { it.objectID.rawValue }
            .toSet()
        val transferByObject: Map<String, List<CanonicalTransferJob>> =
            (transferProjection?.jobs ?: emptyList())
                .groupBy { it.objectID }

        return CanonicalLibraryProjection(
            recordings = manifest.objects
                .map { recordingProjection(it, conflicts, transferByObject[it.objectID] ?: emptyList()) }
                .sortedBy { it.objectID },
            folders = manifest.libraryObjects
                .mapNotNull { obj ->
                    if (obj.kind == CanonicalObjectKind.FOLDER && obj.folder != null) {
                        folderProjection(obj.folder, libraryConflicts.contains(obj.objectID.rawValue))
                    } else {
                        null
                    }
                }
                .sortedBy { it.folderID.rawValue },
            studyItems = manifest.libraryObjects
                .mapNotNull { obj ->
                    when (obj.kind) {
                        CanonicalObjectKind.STANDALONE_STUDY_ITEM,
                        CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM -> {
                            obj.studyItem?.let {
                                studyItemProjection(it, libraryConflicts.contains(obj.objectID.rawValue))
                            }
                        }
                        CanonicalObjectKind.STANDALONE_NOTE -> {
                            obj.standaloneNote?.let { note ->
                                CanonicalStudyItemProjection(
                                    itemID = note.noteID,
                                    title = note.metadata.title,
                                    displayState = displayState(
                                        isDeleted = note.metadata.isDeleted,
                                        hasConflict = libraryConflicts.contains(obj.objectID.rawValue)
                                    ),
                                    actionAvailability = CanonicalActionAvailability.READ_ONLY
                                )
                            }
                        }
                        else -> null
                    }
                }
                .sortedBy { it.itemID.rawValue },
            builtAt = CanonicalTimestamp(builtAt)
        )
    }

    private fun recordingProjection(
        obj: CanonicalRecordingObject,
        conflicts: Set<String>,
        transferJobs: List<CanonicalTransferJob>
    ): CanonicalRecordingProjection {
        val states = mutableListOf<CanonicalDisplayState>()

        if (obj.metadata.isDeleted) {
            states.add(CanonicalDisplayState.DELETED)
        }
        if (conflicts.contains(obj.objectID) || obj.syncState == CanonicalSyncState.CONFLICT) {
            states.add(CanonicalDisplayState.CONFLICT)
        }
        if (transferJobs.any { it.phase == CanonicalTransferPhase.IN_FLIGHT || it.phase == CanonicalTransferPhase.QUEUED || it.phase == CanonicalTransferPhase.PLANNED }) {
            states.add(CanonicalDisplayState.UPLOADING_AUDIO)
        }
        if (transferJobs.any { it.phase == CanonicalTransferPhase.FAILED_RETRYABLE }) {
            states.add(CanonicalDisplayState.RETRY_PENDING)
        }
        if (transferJobs.any { it.phase == CanonicalTransferPhase.FAILED_FATAL }) {
            states.add(CanonicalDisplayState.FAILED)
        }
        if (obj.audioAvailable) {
            states.add(CanonicalDisplayState.AUDIO_AVAILABLE)
        } else if (!obj.metadata.isDeleted) {
            states.add(CanonicalDisplayState.WAITING_FOR_AUDIO)
        }
        if (generatedAvailable(obj, CanonicalArtifact.Kind.TRANSCRIPT_MARKDOWN) ||
            generatedAvailable(obj, CanonicalArtifact.Kind.TRANSCRIPT_JSON)
        ) {
            states.add(CanonicalDisplayState.TRANSCRIPT_AVAILABLE)
        }
        if (generatedAvailable(obj, CanonicalArtifact.Kind.NOTE_MARKDOWN) ||
            generatedAvailable(obj, CanonicalArtifact.Kind.NOTE_JSON)
        ) {
            states.add(CanonicalDisplayState.NOTE_AVAILABLE)
        }
        if (generatedAvailable(obj, CanonicalArtifact.Kind.SUMMARY_JSON)) {
            states.add(CanonicalDisplayState.SUMMARY_AVAILABLE)
        }
        if (states.isEmpty()) {
            states.add(CanonicalDisplayState.METADATA_SYNCED)
        }

        val audio = obj.audioArtifact
        val hashPrefixLength = 12

        return CanonicalRecordingProjection(
            objectID = obj.objectID,
            title = obj.metadata.title,
            displayStates = unique(states),
            actionAvailability = CanonicalActionAvailability.READ_ONLY,
            metadataHashPrefix = obj.metadataHash.value.take(hashPrefixLength),
            audioHashPrefix = audio?.contentHash?.let { it.value.take(hashPrefixLength) }
        )
    }

    private fun folderProjection(
        folder: CanonicalFolderObject,
        hasConflict: Boolean
    ): CanonicalFolderProjection {
        return CanonicalFolderProjection(
            folderID = folder.folderID,
            title = folder.metadata.name,
            displayState = displayState(isDeleted = folder.metadata.isDeleted, hasConflict = hasConflict),
            actionAvailability = CanonicalActionAvailability.READ_ONLY
        )
    }

    private fun studyItemProjection(
        item: CanonicalStudyItemObject,
        hasConflict: Boolean
    ): CanonicalStudyItemProjection {
        return CanonicalStudyItemProjection(
            itemID = item.itemID,
            title = item.metadata.title,
            displayState = displayState(isDeleted = item.metadata.isDeleted, hasConflict = hasConflict),
            actionAvailability = CanonicalActionAvailability.READ_ONLY
        )
    }

    private fun displayState(isDeleted: Boolean, hasConflict: Boolean): CanonicalDisplayState {
        if (isDeleted) {
            return CanonicalDisplayState.TOMBSTONED
        }
        if (hasConflict) {
            return CanonicalDisplayState.CONFLICT
        }
        return CanonicalDisplayState.AVAILABLE
    }

    private fun generatedAvailable(
        obj: CanonicalRecordingObject,
        kind: CanonicalArtifact.Kind
    ): Boolean {
        return obj.artifacts.any { it.kind == kind && it.provesCanonicalGeneratedArtifactAvailability }
    }

    private fun unique(states: List<CanonicalDisplayState>): List<CanonicalDisplayState> {
        val seen = mutableSetOf<CanonicalDisplayState>()
        return states.filter { seen.add(it) }
    }
}

// NOTE: Forward type stubs have been moved to their canonical files:
// - CanonicalTransferPhase, CanonicalTransferJob, CanonicalTransferKind,
//   CanonicalTransferDirection, CanonicalTransferProjection →
//   CanonicalTransferStateMachine.kt
// - CanonicalApplyTarget, CanonicalConflictRecord, CanonicalLibraryConflict →
//   CanonicalConflictResolver.kt
// - CanonicalApplyConflictRecord, CanonicalApplyPlan →
//   CanonicalApplyPlan.kt
// - CanonicalLibrarySyncPlan, CanonicalLibrarySyncAction,
//   CanonicalLibrarySyncActionKind, CanonicalLibrarySyncConflict,
//   CanonicalLibrarySyncConflictKind →
//   CanonicalLibrarySyncPlanner.kt
