package com.rokurics.app.domain.canonical

enum class CanonicalConflictResolutionPolicy {
    manualReview,
    keepBothNoOverwrite,
    tombstoneRequiresManualReview
}

enum class CanonicalConflictKind {
    recordingMetadataConcurrentEdit,
    recordingAudioContentMismatch,
    generatedArtifactContentMismatch,
    activeVsTombstone
}

enum class CanonicalLibraryConflictKind {
    folderMetadataConcurrentEdit,
    studyItemMetadataConcurrentEdit,
    activeVsTombstone,
    recordingEnvelopeMetadataDisagreement,
    unsupportedLibraryObject
}

data class CanonicalApplyTarget(
    val objectID: String,
    val artifactID: String? = null,
    val artifactKind: CanonicalArtifact.Kind? = null
)

data class CanonicalConflictRecord(
    val conflictID: String,
    val kind: CanonicalConflictKind,
    val target: CanonicalApplyTarget,
    val resolutionPolicy: CanonicalConflictResolutionPolicy,
    val resolutionState: CanonicalConflictResolutionState = CanonicalConflictResolutionState.unresolved,
    val localHashPrefix: String? = null,
    val peerHashPrefix: String? = null,
    val localModifiedAt: CanonicalTimestamp? = null,
    val peerModifiedAt: CanonicalTimestamp? = null,
    val detail: String? = null
) {
    val id: String get() = conflictID
}

data class CanonicalLibraryConflict(
    val conflictID: String,
    val objectID: CanonicalLibraryObjectID,
    val kind: CanonicalLibraryConflictKind,
    val objectKind: CanonicalObjectKind = CanonicalObjectKind.UNKNOWN_UNSUPPORTED,
    val localHashPrefix: String? = null,
    val peerHashPrefix: String? = null,
    val localModifiedAt: CanonicalTimestamp? = null,
    val peerModifiedAt: CanonicalTimestamp? = null,
    val detail: String? = null
) {
    val id: String get() = conflictID
}

enum class CanonicalConflictResolverAction {
    recordOnly,
    keepBothNoOverwrite,
    requireManualReview,
    tombstoneManualReview
}

enum class CanonicalConflictResolutionState {
    unresolved,
    resolved,
    deferred
}

data class CanonicalConflictResolutionDecision(
    val conflictID: String,
    val target: CanonicalApplyTarget,
    val action: CanonicalConflictResolverAction,
    val state: CanonicalConflictResolutionState,
    val detail: String? = null
) {
    val id: String get() = conflictID
}

data class CanonicalConflictResolverReport(
    val decisions: List<CanonicalConflictResolutionDecision>,
    val unresolvedCount: Int,
    val manualReviewCount: Int,
    val keepBothCount: Int
)

class CanonicalConflictResolver {

    fun resolve(
        conflicts: List<CanonicalConflictRecord>,
        libraryConflicts: List<CanonicalLibraryConflict> = emptyList()
    ): CanonicalConflictResolverReport {
        val recordingDecisions = conflicts.map { decision(it) }
        val libraryDecisions = libraryConflicts.map { decision(it) }
        val decisions = (recordingDecisions + libraryDecisions).sortedBy { it.conflictID }
        return CanonicalConflictResolverReport(
            decisions = decisions,
            unresolvedCount = decisions.count { it.state == CanonicalConflictResolutionState.unresolved },
            manualReviewCount = decisions.count {
                it.action == CanonicalConflictResolverAction.requireManualReview ||
                        it.action == CanonicalConflictResolverAction.tombstoneManualReview
            },
            keepBothCount = decisions.count { it.action == CanonicalConflictResolverAction.keepBothNoOverwrite }
        )
    }

    private fun decision(conflict: CanonicalConflictRecord): CanonicalConflictResolutionDecision {
        val action = when (conflict.resolutionPolicy) {
            CanonicalConflictResolutionPolicy.manualReview -> CanonicalConflictResolverAction.requireManualReview
            CanonicalConflictResolutionPolicy.keepBothNoOverwrite -> CanonicalConflictResolverAction.keepBothNoOverwrite
            CanonicalConflictResolutionPolicy.tombstoneRequiresManualReview -> CanonicalConflictResolverAction.tombstoneManualReview
        }
        return CanonicalConflictResolutionDecision(
            conflictID = conflict.conflictID,
            target = conflict.target,
            action = action,
            state = CanonicalConflictResolutionState.unresolved,
            detail = conflict.kind.name
        )
    }

    private fun decision(conflict: CanonicalLibraryConflict): CanonicalConflictResolutionDecision {
        val action = if (conflict.kind == CanonicalLibraryConflictKind.activeVsTombstone)
            CanonicalConflictResolverAction.tombstoneManualReview
        else
            CanonicalConflictResolverAction.requireManualReview
        return CanonicalConflictResolutionDecision(
            conflictID = conflict.conflictID,
            target = CanonicalApplyTarget(objectID = conflict.objectID.rawValue),
            action = action,
            state = CanonicalConflictResolutionState.unresolved,
            detail = conflict.kind.name
        )
    }
}
