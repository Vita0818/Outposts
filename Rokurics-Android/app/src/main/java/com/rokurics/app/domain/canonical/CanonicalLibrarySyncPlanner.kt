package com.rokurics.app.domain.canonical

import java.util.Date

enum class CanonicalLibrarySyncActionKind {
    NO_OP,
    APPLY_METADATA,
    SEND_METADATA,
    TOMBSTONE_APPLY,
    TOMBSTONE_SEND,
    CONFLICT,
    DEFERRED_UNSUPPORTED
}

data class CanonicalLibrarySyncAction(
    val objectID: CanonicalLibraryObjectID,
    val kind: CanonicalLibrarySyncActionKind,
    val reason: String
)

enum class CanonicalLibrarySyncConflictKind {
    METADATA_MODIFIED_ON_BOTH_SIDES,
    ACTIVE_VS_TOMBSTONE,
    TOMBSTONE_VS_ACTIVE,
    UNSUPPORTED
}

data class CanonicalLibrarySyncConflict(
    val conflictID: String,
    val objectID: CanonicalLibraryObjectID,
    val kind: CanonicalLibrarySyncConflictKind,
    val reason: String
)

data class CanonicalLibrarySyncPlan(
    val folderActions: List<CanonicalLibrarySyncAction> = emptyList(),
    val studyItemActions: List<CanonicalLibrarySyncAction> = emptyList(),
    val noteActions: List<CanonicalLibrarySyncAction> = emptyList(),
    val tombstoneActions: List<CanonicalLibrarySyncAction> = emptyList(),
    val conflicts: List<CanonicalLibrarySyncConflict> = emptyList(),
    val generatedAt: CanonicalTimestamp = CanonicalTimestamp(Date())
)

class CanonicalLibrarySyncPlanner {

    fun plan(
        localManifest: CanonicalManifest,
        peerManifest: CanonicalManifest?
    ): CanonicalLibrarySyncPlan {
        val folderActions = mutableListOf<CanonicalLibrarySyncAction>()
        val studyItemActions = mutableListOf<CanonicalLibrarySyncAction>()
        val noteActions = mutableListOf<CanonicalLibrarySyncAction>()
        val tombstoneActions = mutableListOf<CanonicalLibrarySyncAction>()
        val conflicts = mutableListOf<CanonicalLibrarySyncConflict>()

        if (peerManifest == null) {
            return buildPlan(folderActions, studyItemActions, noteActions, tombstoneActions, conflicts)
        }

        if (!hasLibraryCapability(localManifest) || !hasLibraryCapability(peerManifest)) {
            return buildPlan(folderActions, studyItemActions, noteActions, tombstoneActions, conflicts)
        }

        val localObjectsById = localManifest.libraryObjects.associateBy { it.objectID }
        val peerObjectsById = peerManifest.libraryObjects.associateBy { it.objectID }
        val allObjectIDs = (localObjectsById.keys + peerObjectsById.keys)
            .distinct()
            .sortedBy { it.rawValue }

        for (objectID in allObjectIDs) {
            val localObj = localObjectsById[objectID]
            val peerObj = peerObjectsById[objectID]
            val resolved = localObj ?: peerObj ?: continue

            if (!isSupported(resolved)) {
                appendUnsupported(objectID, resolved, folderActions, studyItemActions, noteActions)
                continue
            }

            val targetActions = targetActionList(resolved.kind, folderActions, studyItemActions, noteActions)
                ?: continue

            appendMetadataDecision(objectID, localObj, peerObj, targetActions, conflicts)
        }

        processLibraryTombstones(
            localTombstones = localManifest.libraryTombstones,
            peerTombstones = peerManifest.libraryTombstones,
            tombstoneActions = tombstoneActions,
            conflicts = conflicts
        )

        return buildPlan(folderActions, studyItemActions, noteActions, tombstoneActions, conflicts)
    }

    private fun appendMetadataDecision(
        objectID: CanonicalLibraryObjectID,
        localObj: CanonicalLibraryObject?,
        peerObj: CanonicalLibraryObject?,
        targetActions: MutableList<CanonicalLibrarySyncAction>,
        conflicts: MutableList<CanonicalLibrarySyncConflict>
    ) {
        val localDeletedAt = localObj?.deletedAt
        val peerDeletedAt = peerObj?.deletedAt

        if (localDeletedAt != null && peerDeletedAt != null) {
            resolveDoubleTombstone(objectID, localDeletedAt, peerDeletedAt, targetActions)
            return
        }

        if (localDeletedAt != null && peerDeletedAt == null && peerObj != null) {
            resolveLocalTombstonePeerActive(
                objectID, localDeletedAt, peerObj.businessModifiedAt, targetActions, conflicts
            )
            return
        }

        if (localDeletedAt == null && peerDeletedAt != null && localObj != null) {
            resolveLocalActivePeerTombstone(
                objectID, localObj.businessModifiedAt, peerDeletedAt, targetActions, conflicts
            )
            return
        }

        if (localDeletedAt != null && peerObj == null) {
            targetActions.add(
                action(objectID, CanonicalLibrarySyncActionKind.TOMBSTONE_SEND, "localTombstoneNewer")
            )
            return
        }

        if (peerDeletedAt != null && localObj == null) {
            targetActions.add(
                action(objectID, CanonicalLibrarySyncActionKind.TOMBSTONE_APPLY, "peerTombstoneNewer")
            )
            return
        }

        resolveMetadata(objectID, localObj, peerObj, targetActions, conflicts)
    }

    private fun resolveDoubleTombstone(
        objectID: CanonicalLibraryObjectID,
        localDeletedAt: CanonicalTimestamp,
        peerDeletedAt: CanonicalTimestamp,
        targetActions: MutableList<CanonicalLibrarySyncAction>
    ) {
        when {
            localDeletedAt.date.after(peerDeletedAt.date) ->
                targetActions.add(
                    action(objectID, CanonicalLibrarySyncActionKind.TOMBSTONE_SEND, "localTombstoneNewer")
                )
            peerDeletedAt.date.after(localDeletedAt.date) ->
                targetActions.add(
                    action(objectID, CanonicalLibrarySyncActionKind.TOMBSTONE_APPLY, "peerTombstoneNewer")
                )
            else ->
                targetActions.add(
                    action(objectID, CanonicalLibrarySyncActionKind.NO_OP, "tombstoneTie")
                )
        }
    }

    private fun resolveLocalTombstonePeerActive(
        objectID: CanonicalLibraryObjectID,
        localDeletedAt: CanonicalTimestamp,
        peerModifiedAt: CanonicalTimestamp?,
        targetActions: MutableList<CanonicalLibrarySyncAction>,
        conflicts: MutableList<CanonicalLibrarySyncConflict>
    ) {
        if (peerModifiedAt != null && peerModifiedAt.date.after(localDeletedAt.date)) {
            targetActions.add(
                action(objectID, CanonicalLibrarySyncActionKind.CONFLICT, "activeNewerThanLocalTombstone")
            )
            conflicts.add(
                conflict(objectID, CanonicalLibrarySyncConflictKind.ACTIVE_VS_TOMBSTONE, "activeNewerThanLocalTombstone")
            )
        } else {
            targetActions.add(
                action(objectID, CanonicalLibrarySyncActionKind.TOMBSTONE_SEND, "localTombstoneNewer")
            )
        }
    }

    private fun resolveLocalActivePeerTombstone(
        objectID: CanonicalLibraryObjectID,
        localModifiedAt: CanonicalTimestamp?,
        peerDeletedAt: CanonicalTimestamp,
        targetActions: MutableList<CanonicalLibrarySyncAction>,
        conflicts: MutableList<CanonicalLibrarySyncConflict>
    ) {
        if (localModifiedAt != null && localModifiedAt.date.after(peerDeletedAt.date)) {
            targetActions.add(
                action(objectID, CanonicalLibrarySyncActionKind.CONFLICT, "activeNewerThanPeerTombstone")
            )
            conflicts.add(
                conflict(objectID, CanonicalLibrarySyncConflictKind.ACTIVE_VS_TOMBSTONE, "activeNewerThanPeerTombstone")
            )
        } else {
            targetActions.add(
                action(objectID, CanonicalLibrarySyncActionKind.TOMBSTONE_APPLY, "peerTombstoneNewer")
            )
        }
    }

    private fun resolveMetadata(
        objectID: CanonicalLibraryObjectID,
        localObj: CanonicalLibraryObject?,
        peerObj: CanonicalLibraryObject?,
        targetActions: MutableList<CanonicalLibrarySyncAction>,
        conflicts: MutableList<CanonicalLibrarySyncConflict>
    ) {
        when {
            localObj != null && peerObj != null -> {
                if (sameHash(localObj.metadataHash, peerObj.metadataHash)) {
                    targetActions.add(
                        action(objectID, CanonicalLibrarySyncActionKind.NO_OP, "metadataHashEqual")
                    )
                    return
                }

                val localModifiedAt = localObj.businessModifiedAt
                val peerModifiedAt = peerObj.businessModifiedAt

                if (localModifiedAt != null && peerModifiedAt != null) {
                    when {
                        localModifiedAt.date.after(peerModifiedAt.date) ->
                            targetActions.add(
                                action(objectID, CanonicalLibrarySyncActionKind.SEND_METADATA, "localMetadataNewer")
                            )
                        peerModifiedAt.date.after(localModifiedAt.date) ->
                            targetActions.add(
                                action(objectID, CanonicalLibrarySyncActionKind.APPLY_METADATA, "peerMetadataNewer")
                            )
                        else -> {
                            targetActions.add(
                                action(objectID, CanonicalLibrarySyncActionKind.CONFLICT, "metadataTieConflict")
                            )
                            conflicts.add(
                                conflict(objectID, CanonicalLibrarySyncConflictKind.METADATA_MODIFIED_ON_BOTH_SIDES, "metadataTieConflict")
                            )
                        }
                    }
                } else {
                    targetActions.add(
                        action(objectID, CanonicalLibrarySyncActionKind.CONFLICT, "metadataHashMismatchNoTimestamp")
                    )
                    conflicts.add(
                        conflict(objectID, CanonicalLibrarySyncConflictKind.METADATA_MODIFIED_ON_BOTH_SIDES, "metadataHashMismatchNoTimestamp")
                    )
                }
            }
            localObj != null ->
                targetActions.add(
                    action(objectID, CanonicalLibrarySyncActionKind.SEND_METADATA, "peerMissingMetadata")
                )
            peerObj != null ->
                targetActions.add(
                    action(objectID, CanonicalLibrarySyncActionKind.APPLY_METADATA, "localMissingMetadata")
                )
        }
    }

    private fun appendUnsupported(
        objectID: CanonicalLibraryObjectID,
        obj: CanonicalLibraryObject,
        folderActions: MutableList<CanonicalLibrarySyncAction>,
        studyItemActions: MutableList<CanonicalLibrarySyncAction>,
        noteActions: MutableList<CanonicalLibrarySyncAction>
    ) {
        val a = action(objectID, CanonicalLibrarySyncActionKind.DEFERRED_UNSUPPORTED, "unsupportedLibraryObject")
        when (obj.kind) {
            CanonicalObjectKind.FOLDER -> folderActions.add(a)
            CanonicalObjectKind.STANDALONE_STUDY_ITEM,
            CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM -> studyItemActions.add(a)
            CanonicalObjectKind.STANDALONE_NOTE -> noteActions.add(a)
            else -> {}
        }
    }

    private fun processLibraryTombstones(
        localTombstones: List<CanonicalLibraryTombstone>,
        peerTombstones: List<CanonicalLibraryTombstone>,
        tombstoneActions: MutableList<CanonicalLibrarySyncAction>,
        conflicts: MutableList<CanonicalLibrarySyncConflict>
    ) {
        val localByID = localTombstones.associateBy { it.objectID }
        val peerByID = peerTombstones.associateBy { it.objectID }
        val allIDs = (localByID.keys + peerByID.keys).distinct().sortedBy { it.rawValue }

        for (id in allIDs) {
            val local = localByID[id]
            val peer = peerByID[id]

            when {
                local != null && peer != null -> {
                    val localDt = local.deletedAt
                    val peerDt = peer.deletedAt
                    if (localDt != null && peerDt != null) {
                        when {
                            localDt.date.after(peerDt.date) ->
                                tombstoneActions.add(
                                    action(id, CanonicalLibrarySyncActionKind.TOMBSTONE_SEND, "localTombstoneNewer")
                                )
                            peerDt.date.after(localDt.date) ->
                                tombstoneActions.add(
                                    action(id, CanonicalLibrarySyncActionKind.TOMBSTONE_APPLY, "peerTombstoneNewer")
                                )
                            else ->
                                tombstoneActions.add(
                                    action(id, CanonicalLibrarySyncActionKind.NO_OP, "tombstoneTie")
                                )
                        }
                    } else if (localDt != null) {
                        tombstoneActions.add(
                            action(id, CanonicalLibrarySyncActionKind.TOMBSTONE_SEND, "localTombstoneNewer")
                        )
                        conflicts.add(
                            conflict(id, CanonicalLibrarySyncConflictKind.TOMBSTONE_VS_ACTIVE, "localTombstoneVsPeerActive")
                        )
                    } else {
                        tombstoneActions.add(
                            action(id, CanonicalLibrarySyncActionKind.TOMBSTONE_APPLY, "peerTombstoneNewer")
                        )
                        conflicts.add(
                            conflict(id, CanonicalLibrarySyncConflictKind.ACTIVE_VS_TOMBSTONE, "localActiveVsPeerTombstone")
                        )
                    }
                }
                local != null ->
                    tombstoneActions.add(
                        action(id, CanonicalLibrarySyncActionKind.TOMBSTONE_SEND, "peerMissingTombstone")
                    )
                peer != null ->
                    tombstoneActions.add(
                        action(id, CanonicalLibrarySyncActionKind.TOMBSTONE_APPLY, "localMissingTombstone")
                    )
            }
        }
    }

    private fun targetActionList(
        kind: CanonicalObjectKind,
        folderActions: MutableList<CanonicalLibrarySyncAction>,
        studyItemActions: MutableList<CanonicalLibrarySyncAction>,
        noteActions: MutableList<CanonicalLibrarySyncAction>
    ): MutableList<CanonicalLibrarySyncAction>? = when (kind) {
        CanonicalObjectKind.FOLDER -> folderActions
        CanonicalObjectKind.STANDALONE_STUDY_ITEM,
        CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM -> studyItemActions
        CanonicalObjectKind.STANDALONE_NOTE -> noteActions
        else -> null
    }

    private fun action(
        objectID: CanonicalLibraryObjectID,
        kind: CanonicalLibrarySyncActionKind,
        reason: String
    ) = CanonicalLibrarySyncAction(objectID = objectID, kind = kind, reason = reason)

    private fun conflict(
        objectID: CanonicalLibraryObjectID,
        kind: CanonicalLibrarySyncConflictKind,
        reason: String
    ) = CanonicalLibrarySyncConflict(
        conflictID = listOf("conflict", objectID.rawValue, kind.name.lowercase()).joinToString("|"),
        objectID = objectID,
        kind = kind,
        reason = reason
    )

    private fun buildPlan(
        folderActions: List<CanonicalLibrarySyncAction>,
        studyItemActions: List<CanonicalLibrarySyncAction>,
        noteActions: List<CanonicalLibrarySyncAction>,
        tombstoneActions: List<CanonicalLibrarySyncAction>,
        conflicts: List<CanonicalLibrarySyncConflict>
    ) = CanonicalLibrarySyncPlan(
        folderActions = folderActions,
        studyItemActions = studyItemActions,
        noteActions = noteActions,
        tombstoneActions = tombstoneActions,
        conflicts = conflicts,
        generatedAt = CanonicalTimestamp(Date())
    )

    private fun hasLibraryCapability(manifest: CanonicalManifest): Boolean =
        manifest.node.capabilities.contains(CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1) ||
        manifest.manifestCapabilities.contains(CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1)

    private fun isSupported(obj: CanonicalLibraryObject): Boolean =
        obj.kind != CanonicalObjectKind.UNKNOWN_UNSUPPORTED &&
        obj.kind != CanonicalObjectKind.GENERATED_ARTIFACT_ENVELOPE

    private fun sameHash(left: CanonicalHash, right: CanonicalHash): Boolean =
        left.algorithm == right.algorithm && left.value == right.value
}
