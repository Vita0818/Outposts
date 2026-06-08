package com.rokurics.app.domain.canonical

import java.util.Date

// ═══════════════════════════════════════════
// CanonicalApplyActionKind
// ═══════════════════════════════════════════

enum class CanonicalApplyActionKind {
    recordingMetadataApply,
    recordingMetadataSend,
    folderMetadataApply,
    folderMetadataSend,
    studyItemMetadataApply,
    studyItemMetadataSend,
    libraryTombstoneApply,
    libraryTombstoneSend,
    generatedArtifactDownloadApply,
    generatedArtifactNoOp,
    objectTombstoneApply,
    objectTombstoneSend,
    artifactTombstoneApply,
    conflictRecord,
    deferredUnsupported
}

// ═══════════════════════════════════════════
// CanonicalApplySource
// ═══════════════════════════════════════════

enum class CanonicalApplySource {
    local,
    peer,
    planner
}

// ═══════════════════════════════════════════
// CanonicalApplyResult
// ═══════════════════════════════════════════

enum class CanonicalApplyResult {
    planned,
    noOp,
    conflictRecorded,
    deferredUnsupported
}

// ═══════════════════════════════════════════
// CanonicalApplyFailureReason
// ═══════════════════════════════════════════

enum class CanonicalApplyFailureReason {
    unsupportedRoute,
    conflictDetected,
    tombstoneBlocksResurrection,
    legacyArtifactMissing,
    noPhysicalDeletePolicy,
    hashOrSizeMismatch
}

// ═══════════════════════════════════════════
// CanonicalApplyBridgeHint
// ═══════════════════════════════════════════

enum class CanonicalApplyBridgeHint {
    legacyMetadataManifestApply,
    legacyMetadataManifestSend,
    legacyArtifactRequestApply,
    noGeneratedArtifactUploadJob,
    noPhysicalDelete,
    unsupportedNoRoute,
    legacyFallbackPreserved
}

// ═══════════════════════════════════════════
// CanonicalApplyPrecondition
// ═══════════════════════════════════════════

data class CanonicalApplyPrecondition(
    val kind: CanonicalApplyPrecondition.Kind,
    val value: String
) {
    enum class Kind {
        localObjectPresent,
        peerObjectPresent,
        localObjectActive,
        peerObjectActive,
        localObjectTombstoned,
        peerObjectTombstoned,
        localMetadataHashDiffers,
        peerMetadataHashDiffers,
        localModifiedAtNewer,
        peerModifiedAtNewer,
        localMissingObject,
        peerMissingObject,
        localArtifactAvailable,
        peerArtifactAvailable,
        localArtifactProven,
        peerArtifactProven,
        hashAndSizeMatch,
        hashOrSizeDiffers,
        tombstoneNewerOrEqualThanActive,
        activeNewerThanTombstone,
        noPhysicalDeleteEnforced,
        antiResurrectionEnforced,
        libraryCapabilityPresent,
        supportedObjectKind,
        resolutionPolicyPresent
    }
}

// ═══════════════════════════════════════════
// CanonicalApplyAction
// ═══════════════════════════════════════════

data class CanonicalApplyAction(
    val kind: CanonicalApplyActionKind,
    val target: CanonicalApplyTarget,
    val source: CanonicalApplySource,
    val preconditions: List<CanonicalApplyPrecondition>,
    val result: CanonicalApplyResult,
    val failureReason: CanonicalApplyFailureReason? = null,
    val bridgeHint: CanonicalApplyBridgeHint? = null
) {
    val id: String
        get() = listOfNotNull(
            kind.name,
            target.objectID,
            target.artifactID,
            source.name,
            result.name
        ).joinToString("|")
}

// ═══════════════════════════════════════════
// CanonicalApplyConflictKind
// ═══════════════════════════════════════════

enum class CanonicalApplyConflictKind {
    recordingMetadataConcurrentEdit,
    recordingAudioContentMismatch,
    generatedArtifactContentMismatch,
    folderMetadataConcurrentEdit,
    studyItemMetadataConcurrentEdit,
    activeVsTombstone,
    tombstoneVsActive,
    activeVsActiveDivergingHash,
    genericConflict
}

// ═══════════════════════════════════════════
// CanonicalApplyConflictRecord
// ═══════════════════════════════════════════

data class CanonicalApplyConflictRecord(
    val conflictID: String,
    val target: CanonicalApplyTarget,
    val kind: CanonicalApplyConflictKind,
    val resolutionPolicy: CanonicalConflictResolutionPolicy,
    val detail: String? = null
) {
    val id: String get() = conflictID

    companion object {
        private fun composeDefaultConflictID(
            target: CanonicalApplyTarget,
            kind: CanonicalApplyConflictKind
        ): String = listOf("applyConflict", target.objectID, kind.name).joinToString("|")
    }
}

// ═══════════════════════════════════════════
// CanonicalApplyPlan
// ═══════════════════════════════════════════

data class CanonicalApplyPlan(
    val actions: List<CanonicalApplyAction> = emptyList(),
    val conflicts: List<CanonicalApplyConflictRecord> = emptyList(),
    val generatedAt: CanonicalTimestamp = CanonicalTimestamp(Date()),
    val localNodeID: String? = null,
    val peerNodeID: String? = null
)

// ═══════════════════════════════════════════
// CanonicalApplyPlanner
// ═══════════════════════════════════════════

class CanonicalApplyPlanner {

    fun build(
        syncPlan: CanonicalSyncPlan,
        libraryPlan: CanonicalLibrarySyncPlan,
        localManifest: CanonicalManifest,
        peerManifest: CanonicalManifest?
    ): CanonicalApplyPlan {
        val actions = mutableListOf<CanonicalApplyAction>()
        val conflicts = mutableListOf<CanonicalApplyConflictRecord>()

        val localObjectsByID = localManifest.objects.associateBy { it.objectID }
        val peerObjectsByID = peerManifest?.objects?.associateBy { it.objectID } ?: emptyMap()

        val localLibByID = localManifestLibraryMap(localManifest)
        val peerLibByID = peerManifest?.let { localManifestLibraryMap(it) } ?: emptyMap()

        processMetadataDecisions(syncPlan.metadataDecisions, localObjectsByID, peerObjectsByID, actions, conflicts)
        processTransferDecisions(syncPlan.audioDecisions, localObjectsByID, peerObjectsByID, actions, conflicts)
        processExistenceTruths(syncPlan.existenceTruths, actions, conflicts)
        processArtifactTombstones(localManifest, peerManifest, actions)
        processLibrarySyncActions(libraryPlan, localLibByID, peerLibByID, actions, conflicts)
        processLibraryConflicts(libraryPlan.conflicts, conflicts)

        return CanonicalApplyPlan(
            actions = actions.sortedBy { it.target.objectID },
            conflicts = conflicts.sortedBy { it.conflictID },
            generatedAt = CanonicalTimestamp(Date()),
            localNodeID = localManifest.node.nodeID,
            peerNodeID = peerManifest?.node?.nodeID
        )
    }

    // ─── Metadata decisions ────────────────────────────────────────

    private fun processMetadataDecisions(
        decisions: List<SyncDecision>,
        localByID: Map<String, CanonicalRecordingObject>,
        peerByID: Map<String, CanonicalRecordingObject>,
        actions: MutableList<CanonicalApplyAction>,
        conflicts: MutableList<CanonicalApplyConflictRecord>
    ) {
        for (decision in decisions) {
            val localObj = localByID[decision.objectID]
            val peerObj = peerByID[decision.objectID]

            when (decision.kind) {
                SyncDecision.Kind.UPLOAD_METADATA -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.recordingMetadataSend,
                            target = CanonicalApplyTarget(objectID = decision.objectID),
                            source = CanonicalApplySource.local,
                            result = CanonicalApplyResult.planned,
                            preconditions = recordingSendPreconditions(localObj, peerObj),
                            bridgeHint = CanonicalApplyBridgeHint.legacyMetadataManifestSend
                        )
                    )
                }

                SyncDecision.Kind.DOWNLOAD_METADATA -> {
                    val source = if (peerObj != null) CanonicalApplySource.peer else CanonicalApplySource.planner
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.recordingMetadataApply,
                            target = CanonicalApplyTarget(objectID = decision.objectID),
                            source = source,
                            result = CanonicalApplyResult.planned,
                            preconditions = recordingApplyPreconditions(localObj, peerObj),
                            bridgeHint = CanonicalApplyBridgeHint.legacyMetadataManifestApply
                        )
                    )
                }

                SyncDecision.Kind.NO_OP -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.recordingMetadataApply,
                            target = CanonicalApplyTarget(objectID = decision.objectID),
                            source = CanonicalApplySource.local,
                            result = CanonicalApplyResult.noOp,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.localObjectPresent, "true"),
                                precondition(CanonicalApplyPrecondition.Kind.peerObjectPresent, "true"),
                                precondition(CanonicalApplyPrecondition.Kind.hashAndSizeMatch, "true")
                            ),
                            bridgeHint = null
                        )
                    )
                }

                SyncDecision.Kind.CONFLICT -> {
                    val conflictID = composeRecordingConflictID(decision)
                    val conflictKind = resolveRecordingConflictKind(decision)
                    val resolutionPolicy = resolveRecordingConflictPolicy(conflictKind)
                    val conflict = CanonicalApplyConflictRecord(
                        conflictID = conflictID,
                        target = CanonicalApplyTarget(objectID = decision.objectID),
                        kind = conflictKind,
                        resolutionPolicy = resolutionPolicy,
                        detail = "metadata:${decision.reason}"
                    )
                    conflicts.add(conflict)

                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.conflictRecord,
                            target = CanonicalApplyTarget(objectID = decision.objectID),
                            source = CanonicalApplySource.planner,
                            result = CanonicalApplyResult.conflictRecorded,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.localObjectPresent, "true"),
                                precondition(CanonicalApplyPrecondition.Kind.peerObjectPresent, "true"),
                                precondition(CanonicalApplyPrecondition.Kind.resolutionPolicyPresent, "true")
                            ),
                            failureReason = CanonicalApplyFailureReason.conflictDetected,
                            bridgeHint = null
                        )
                    )
                }

                SyncDecision.Kind.DEFER_UNTIL_PEER_KNOWN -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.deferredUnsupported,
                            target = CanonicalApplyTarget(objectID = decision.objectID),
                            source = CanonicalApplySource.planner,
                            result = CanonicalApplyResult.deferredUnsupported,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.peerMissingObject, "true")
                            ),
                            failureReason = CanonicalApplyFailureReason.unsupportedRoute,
                            bridgeHint = CanonicalApplyBridgeHint.unsupportedNoRoute
                        )
                    )
                }
            }
        }
    }

    // ─── Transfer decisions ────────────────────────────────────────

    private fun processTransferDecisions(
        decisions: List<TransferDecision>,
        localByID: Map<String, CanonicalRecordingObject>,
        peerByID: Map<String, CanonicalRecordingObject>,
        actions: MutableList<CanonicalApplyAction>,
        conflicts: MutableList<CanonicalApplyConflictRecord>
    ) {
        for (decision in decisions) {
            val kind = decision.kind
            val target = CanonicalApplyTarget(
                objectID = decision.objectID,
                artifactID = decision.artifactID,
                artifactKind = CanonicalArtifact.Kind.AUDIO
            )

            when (kind) {
                TransferDecision.Kind.DOWNLOAD -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.generatedArtifactDownloadApply,
                            target = target,
                            source = CanonicalApplySource.peer,
                            result = CanonicalApplyResult.planned,
                            preconditions = artifactDownloadPreconditions(decision, localByID, peerByID),
                            bridgeHint = CanonicalApplyBridgeHint.legacyArtifactRequestApply
                        )
                    )
                }

                TransferDecision.Kind.NO_OP -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.generatedArtifactNoOp,
                            target = target,
                            source = CanonicalApplySource.local,
                            result = CanonicalApplyResult.noOp,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.hashAndSizeMatch, "true"),
                                precondition(CanonicalApplyPrecondition.Kind.localArtifactProven, "true"),
                                precondition(CanonicalApplyPrecondition.Kind.peerArtifactProven, "true")
                            ),
                            bridgeHint = null
                        )
                    )
                }

                TransferDecision.Kind.CONFLICT -> {
                    val conflictID = composeTransferConflictID(decision)
                    val conflictKind = CanonicalApplyConflictKind.recordingAudioContentMismatch
                    val conflict = CanonicalApplyConflictRecord(
                        conflictID = conflictID,
                        target = target,
                        kind = conflictKind,
                        resolutionPolicy = CanonicalConflictResolutionPolicy.manualReview,
                        detail = "audio:${decision.reason}"
                    )
                    conflicts.add(conflict)

                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.conflictRecord,
                            target = target,
                            source = CanonicalApplySource.planner,
                            result = CanonicalApplyResult.conflictRecorded,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.hashOrSizeDiffers, "true")
                            ),
                            failureReason = CanonicalApplyFailureReason.conflictDetected,
                            bridgeHint = null
                        )
                    )
                }

                TransferDecision.Kind.DEFER_UNTIL_PEER_KNOWN,
                TransferDecision.Kind.LOCAL_UNAVAILABLE -> {
                    val isLocalUnavailable = kind == TransferDecision.Kind.LOCAL_UNAVAILABLE
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.deferredUnsupported,
                            target = target,
                            source = CanonicalApplySource.planner,
                            result = CanonicalApplyResult.deferredUnsupported,
                            preconditions = listOf(
                                if (isLocalUnavailable)
                                    precondition(CanonicalApplyPrecondition.Kind.localArtifactProven, "false")
                                else
                                    precondition(CanonicalApplyPrecondition.Kind.peerObjectPresent, "false")
                            ),
                            failureReason = if (isLocalUnavailable)
                                CanonicalApplyFailureReason.legacyArtifactMissing
                            else
                                CanonicalApplyFailureReason.unsupportedRoute,
                            bridgeHint = if (isLocalUnavailable)
                                CanonicalApplyBridgeHint.noGeneratedArtifactUploadJob
                            else
                                CanonicalApplyBridgeHint.unsupportedNoRoute
                        )
                    )
                }

                TransferDecision.Kind.UPLOAD -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.generatedArtifactNoOp,
                            target = target,
                            source = CanonicalApplySource.local,
                            result = CanonicalApplyResult.noOp,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.localArtifactAvailable, "true"),
                                precondition(CanonicalApplyPrecondition.Kind.localArtifactProven, "true"),
                                precondition(CanonicalApplyPrecondition.Kind.hashOrSizeDiffers, "true")
                            ),
                            bridgeHint = CanonicalApplyBridgeHint.noGeneratedArtifactUploadJob
                        )
                    )
                }
            }
        }
    }

    // ─── Existence truths → object tombstone actions ───────────────

    private fun processExistenceTruths(
        truths: List<CanonicalRecordingExistenceTruth>,
        actions: MutableList<CanonicalApplyAction>,
        conflicts: MutableList<CanonicalApplyConflictRecord>
    ) {
        for (truth in truths) {
            when (truth.localState) {
                CanonicalRecordingExistenceState.TOMBSTONED -> {
                    if (truth.peerState != CanonicalRecordingExistenceState.TOMBSTONED) {
                        actions.add(
                            action(
                                kind = CanonicalApplyActionKind.objectTombstoneSend,
                                target = CanonicalApplyTarget(objectID = truth.objectID),
                                source = CanonicalApplySource.local,
                                result = CanonicalApplyResult.planned,
                                preconditions = listOf(
                                    precondition(CanonicalApplyPrecondition.Kind.localObjectTombstoned, "true"),
                                    precondition(CanonicalApplyPrecondition.Kind.noPhysicalDeleteEnforced, "true"),
                                    precondition(CanonicalApplyPrecondition.Kind.antiResurrectionEnforced, "true")
                                ),
                                bridgeHint = CanonicalApplyBridgeHint.noPhysicalDelete
                            )
                        )
                    }
                }
                CanonicalRecordingExistenceState.ABSENT -> {
                    if (truth.peerState == CanonicalRecordingExistenceState.TOMBSTONED) {
                        actions.add(
                            action(
                                kind = CanonicalApplyActionKind.objectTombstoneApply,
                                target = CanonicalApplyTarget(objectID = truth.objectID),
                                source = CanonicalApplySource.peer,
                                result = CanonicalApplyResult.planned,
                                preconditions = listOf(
                                    precondition(CanonicalApplyPrecondition.Kind.peerObjectTombstoned, "true"),
                                    precondition(CanonicalApplyPrecondition.Kind.tombstoneNewerOrEqualThanActive, "true"),
                                    precondition(CanonicalApplyPrecondition.Kind.noPhysicalDeleteEnforced, "true")
                                ),
                                bridgeHint = CanonicalApplyBridgeHint.noPhysicalDelete
                            )
                        )
                    }
                }
                else -> {}
            }

            if (truth.decision == CanonicalRecordingExistenceDecision.CONFLICT) {
                val conflictKind = when {
                    truth.blockers.contains(CanonicalRecordingExistenceBlocker.AUDIO_HASH_MISMATCH) ||
                    truth.blockers.contains(CanonicalRecordingExistenceBlocker.AUDIO_SIZE_MISMATCH) ->
                        CanonicalApplyConflictKind.recordingAudioContentMismatch
                    truth.blockers.contains(CanonicalRecordingExistenceBlocker.TOMBSTONED_PARENT) ->
                        CanonicalApplyConflictKind.activeVsTombstone
                    else -> CanonicalApplyConflictKind.recordingMetadataConcurrentEdit
                }
                conflicts.add(
                    CanonicalApplyConflictRecord(
                        conflictID = listOf("applyConflict", truth.objectID, conflictKind.name).joinToString("|"),
                        target = CanonicalApplyTarget(objectID = truth.objectID),
                        kind = conflictKind,
                        resolutionPolicy = CanonicalConflictResolutionPolicy.manualReview,
                        detail = "existence:${truth.decision.name}"
                    )
                )
            }
        }
    }

    // ─── Artifact tombstones ───────────────────────────────────────

    private fun processArtifactTombstones(
        localManifest: CanonicalManifest,
        peerManifest: CanonicalManifest?,
        actions: MutableList<CanonicalApplyAction>
    ) {
        val allArtifacts = mutableMapOf<Pair<String, CanonicalArtifact.Kind>, Pair<CanonicalArtifact?, CanonicalArtifact?>>()

        for (obj in localManifest.objects) {
            for (artifact in obj.artifacts) {
                if (artifact.tombstone == true) {
                    val key = obj.objectID to artifact.kind
                    allArtifacts[key] = (artifact as CanonicalArtifact?) to null
                }
            }
        }

        if (peerManifest != null) {
            for (obj in peerManifest.objects) {
                for (artifact in obj.artifacts) {
                    val key = obj.objectID to artifact.kind
                    val existing = allArtifacts[key]
                    if (artifact.tombstone == true) {
                        allArtifacts[key] = (existing?.first) to (artifact as CanonicalArtifact?)
                    } else if (existing != null) {
                        allArtifacts[key] = (existing.first) to (existing.second)
                    }
                }
            }
        }

        for ((key, pair) in allArtifacts) {
            val (objectID, artifactKind) = key
            val (localTombstone, peerTombstone) = pair

            if (localTombstone != null && peerTombstone == null) {
                actions.add(
                    action(
                        kind = CanonicalApplyActionKind.artifactTombstoneApply,
                        target = CanonicalApplyTarget(
                            objectID = objectID,
                            artifactID = localTombstone.artifactID,
                            artifactKind = artifactKind
                        ),
                        source = CanonicalApplySource.local,
                        result = CanonicalApplyResult.planned,
                        preconditions = listOf(
                            precondition(CanonicalApplyPrecondition.Kind.localObjectTombstoned, "true"),
                            precondition(CanonicalApplyPrecondition.Kind.noPhysicalDeleteEnforced, "true"),
                            precondition(CanonicalApplyPrecondition.Kind.antiResurrectionEnforced, "true")
                        ),
                        bridgeHint = CanonicalApplyBridgeHint.noPhysicalDelete
                    )
                )
            }
        }
    }

    // ─── Library sync actions ──────────────────────────────────────

    private fun processLibrarySyncActions(
        libraryPlan: CanonicalLibrarySyncPlan,
        localLibByID: Map<CanonicalLibraryObjectID, CanonicalLibraryObject>,
        peerLibByID: Map<CanonicalLibraryObjectID, CanonicalLibraryObject>,
        actions: MutableList<CanonicalApplyAction>,
        conflicts: MutableList<CanonicalApplyConflictRecord>
    ) {
        processLibraryActionList(
            libraryPlan.folderActions,
            CanonicalApplyActionKind.folderMetadataApply,
            CanonicalApplyActionKind.folderMetadataSend,
            localLibByID, peerLibByID, actions, conflicts
        )

        processLibraryActionList(
            libraryPlan.studyItemActions,
            CanonicalApplyActionKind.studyItemMetadataApply,
            CanonicalApplyActionKind.studyItemMetadataSend,
            localLibByID, peerLibByID, actions, conflicts
        )

        processLibraryActionList(
            libraryPlan.noteActions,
            CanonicalApplyActionKind.studyItemMetadataApply,
            CanonicalApplyActionKind.studyItemMetadataSend,
            localLibByID, peerLibByID, actions, conflicts
        )

        processLibraryTombstoneActions(libraryPlan.tombstoneActions, localLibByID, peerLibByID, actions, conflicts)
    }

    private fun processLibraryActionList(
        libraryActions: List<CanonicalLibrarySyncAction>,
        applyKind: CanonicalApplyActionKind,
        sendKind: CanonicalApplyActionKind,
        localLibByID: Map<CanonicalLibraryObjectID, CanonicalLibraryObject>,
        peerLibByID: Map<CanonicalLibraryObjectID, CanonicalLibraryObject>,
        actions: MutableList<CanonicalApplyAction>,
        conflicts: MutableList<CanonicalApplyConflictRecord>
    ) {
        for (libAction in libraryActions) {
            val localObj = localLibByID[libAction.objectID]
            val peerObj = peerLibByID[libAction.objectID]

            when (libAction.kind) {
                CanonicalLibrarySyncActionKind.APPLY_METADATA -> {
                    actions.add(
                        action(
                            kind = applyKind,
                            target = CanonicalApplyTarget(objectID = libAction.objectID.rawValue),
                            source = CanonicalApplySource.peer,
                            result = CanonicalApplyResult.planned,
                            preconditions = libraryApplyPreconditions(localObj, peerObj),
                            bridgeHint = null
                        )
                    )
                }

                CanonicalLibrarySyncActionKind.SEND_METADATA -> {
                    actions.add(
                        action(
                            kind = sendKind,
                            target = CanonicalApplyTarget(objectID = libAction.objectID.rawValue),
                            source = CanonicalApplySource.local,
                            result = CanonicalApplyResult.planned,
                            preconditions = librarySendPreconditions(localObj, peerObj),
                            bridgeHint = null
                        )
                    )
                }

                CanonicalLibrarySyncActionKind.NO_OP -> {
                    actions.add(
                        action(
                            kind = applyKind,
                            target = CanonicalApplyTarget(objectID = libAction.objectID.rawValue),
                            source = CanonicalApplySource.local,
                            result = CanonicalApplyResult.noOp,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.hashAndSizeMatch, "true")
                            ),
                            bridgeHint = null
                        )
                    )
                }

                CanonicalLibrarySyncActionKind.CONFLICT -> {
                    val conflictKind = resolveLibraryObjectConflictKind(libAction, localObj)
                    conflicts.add(
                        CanonicalApplyConflictRecord(
                            conflictID = listOf("applyConflict", libAction.objectID.rawValue, conflictKind.name).joinToString("|"),
                            target = CanonicalApplyTarget(objectID = libAction.objectID.rawValue),
                            kind = conflictKind,
                            resolutionPolicy = if (conflictKind == CanonicalApplyConflictKind.activeVsTombstone ||
                                conflictKind == CanonicalApplyConflictKind.tombstoneVsActive
                            )
                                CanonicalConflictResolutionPolicy.tombstoneRequiresManualReview
                            else
                                CanonicalConflictResolutionPolicy.manualReview,
                            detail = "library:${libAction.reason}"
                        )
                    )

                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.conflictRecord,
                            target = CanonicalApplyTarget(objectID = libAction.objectID.rawValue),
                            source = CanonicalApplySource.planner,
                            result = CanonicalApplyResult.conflictRecorded,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.resolutionPolicyPresent, "true")
                            ),
                            failureReason = CanonicalApplyFailureReason.conflictDetected,
                            bridgeHint = null
                        )
                    )
                }

                CanonicalLibrarySyncActionKind.DEFERRED_UNSUPPORTED -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.deferredUnsupported,
                            target = CanonicalApplyTarget(objectID = libAction.objectID.rawValue),
                            source = CanonicalApplySource.planner,
                            result = CanonicalApplyResult.deferredUnsupported,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.supportedObjectKind, "false")
                            ),
                            failureReason = CanonicalApplyFailureReason.unsupportedRoute,
                            bridgeHint = CanonicalApplyBridgeHint.unsupportedNoRoute
                        )
                    )
                }

                else -> {}
            }
        }
    }

    // ─── Library tombstone actions ─────────────────────────────────

    private fun processLibraryTombstoneActions(
        tombstoneActions: List<CanonicalLibrarySyncAction>,
        localLibByID: Map<CanonicalLibraryObjectID, CanonicalLibraryObject>,
        peerLibByID: Map<CanonicalLibraryObjectID, CanonicalLibraryObject>,
        actions: MutableList<CanonicalApplyAction>,
        conflicts: MutableList<CanonicalApplyConflictRecord>
    ) {
        for (tsAction in tombstoneActions) {
            val localObj = localLibByID[tsAction.objectID]
            val peerObj = peerLibByID[tsAction.objectID]

            when (tsAction.kind) {
                CanonicalLibrarySyncActionKind.TOMBSTONE_APPLY -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.libraryTombstoneApply,
                            target = CanonicalApplyTarget(objectID = tsAction.objectID.rawValue),
                            source = CanonicalApplySource.peer,
                            result = CanonicalApplyResult.planned,
                            preconditions = libraryTombstoneApplyPreconditions(localObj, peerObj),
                            bridgeHint = CanonicalApplyBridgeHint.noPhysicalDelete
                        )
                    )
                }

                CanonicalLibrarySyncActionKind.TOMBSTONE_SEND -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.libraryTombstoneSend,
                            target = CanonicalApplyTarget(objectID = tsAction.objectID.rawValue),
                            source = CanonicalApplySource.local,
                            result = CanonicalApplyResult.planned,
                            preconditions = libraryTombstoneSendPreconditions(localObj, peerObj),
                            bridgeHint = CanonicalApplyBridgeHint.noPhysicalDelete
                        )
                    )
                }

                CanonicalLibrarySyncActionKind.NO_OP -> {
                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.libraryTombstoneApply,
                            target = CanonicalApplyTarget(objectID = tsAction.objectID.rawValue),
                            source = CanonicalApplySource.local,
                            result = CanonicalApplyResult.noOp,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.tombstoneNewerOrEqualThanActive, "false")
                            ),
                            bridgeHint = null
                        )
                    )
                }

                CanonicalLibrarySyncActionKind.CONFLICT -> {
                    val conflictKind = resolveLibraryObjectConflictKind(tsAction, localObj)
                    conflicts.add(
                        CanonicalApplyConflictRecord(
                            conflictID = listOf("applyConflict", tsAction.objectID.rawValue, conflictKind.name).joinToString("|"),
                            target = CanonicalApplyTarget(objectID = tsAction.objectID.rawValue),
                            kind = conflictKind,
                            resolutionPolicy = CanonicalConflictResolutionPolicy.tombstoneRequiresManualReview,
                            detail = "libraryTombstone:${tsAction.reason}"
                        )
                    )

                    actions.add(
                        action(
                            kind = CanonicalApplyActionKind.conflictRecord,
                            target = CanonicalApplyTarget(objectID = tsAction.objectID.rawValue),
                            source = CanonicalApplySource.planner,
                            result = CanonicalApplyResult.conflictRecorded,
                            preconditions = listOf(
                                precondition(CanonicalApplyPrecondition.Kind.activeNewerThanTombstone, "true")
                            ),
                            failureReason = CanonicalApplyFailureReason.tombstoneBlocksResurrection,
                            bridgeHint = null
                        )
                    )
                }

                else -> {}
            }
        }
    }

    // ─── Library conflicts from library plan ───────────────────────

    private fun processLibraryConflicts(
        libraryConflicts: List<CanonicalLibrarySyncConflict>,
        conflicts: MutableList<CanonicalApplyConflictRecord>
    ) {
        for (libConflict in libraryConflicts) {
            val applyConflictKind = when (libConflict.kind) {
                CanonicalLibrarySyncConflictKind.METADATA_MODIFIED_ON_BOTH_SIDES ->
                    CanonicalApplyConflictKind.activeVsActiveDivergingHash
                CanonicalLibrarySyncConflictKind.ACTIVE_VS_TOMBSTONE ->
                    CanonicalApplyConflictKind.activeVsTombstone
                CanonicalLibrarySyncConflictKind.TOMBSTONE_VS_ACTIVE ->
                    CanonicalApplyConflictKind.tombstoneVsActive
                CanonicalLibrarySyncConflictKind.UNSUPPORTED ->
                    CanonicalApplyConflictKind.genericConflict
            }

            conflicts.add(
                CanonicalApplyConflictRecord(
                    conflictID = listOf("applyConflict", libConflict.objectID.rawValue, applyConflictKind.name).joinToString("|"),
                    target = CanonicalApplyTarget(objectID = libConflict.objectID.rawValue),
                    kind = applyConflictKind,
                    resolutionPolicy = when (applyConflictKind) {
                        CanonicalApplyConflictKind.activeVsTombstone,
                        CanonicalApplyConflictKind.tombstoneVsActive ->
                            CanonicalConflictResolutionPolicy.tombstoneRequiresManualReview
                        else ->
                            CanonicalConflictResolutionPolicy.manualReview
                    },
                    detail = "libraryPlan:${libConflict.reason}"
                )
            )
        }
    }

    // ─── Precondition builders ─────────────────────────────────────

    private fun recordingApplyPreconditions(
        local: CanonicalRecordingObject?,
        peer: CanonicalRecordingObject?
    ): List<CanonicalApplyPrecondition> {
        val preconditions = mutableListOf<CanonicalApplyPrecondition>()

        if (peer != null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerObjectPresent, "true"))
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerMetadataHashDiffers, "true"))
            if (local != null && peer.metadata.modifiedAt.date.after(local.metadata.modifiedAt.date)) {
                preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerModifiedAtNewer, "true"))
            }
        } else {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerMissingObject, "false"))
        }

        if (local != null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localObjectPresent, "true"))
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localObjectActive, "true"))
        }

        return preconditions
    }

    private fun recordingSendPreconditions(
        local: CanonicalRecordingObject?,
        peer: CanonicalRecordingObject?
    ): List<CanonicalApplyPrecondition> {
        val preconditions = mutableListOf<CanonicalApplyPrecondition>()

        if (local != null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localObjectPresent, "true"))
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localObjectActive, "true"))

            if (peer != null) {
                preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localModifiedAtNewer, "true"))
            } else {
                preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerMissingObject, "true"))
            }
        }

        return preconditions
    }

    private fun artifactDownloadPreconditions(
        decision: TransferDecision,
        localByID: Map<String, CanonicalRecordingObject>,
        peerByID: Map<String, CanonicalRecordingObject>
    ): List<CanonicalApplyPrecondition> {
        val preconditions = mutableListOf<CanonicalApplyPrecondition>()

        val localObj = localByID[decision.objectID]
        val peerObj = peerByID[decision.objectID]

        val peerAudio = peerObj?.audioArtifact

        if (peerAudio != null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerArtifactAvailable, "true"))
            if (peerAudio.provesCanonicalAudioAvailability) {
                preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerArtifactProven, "true"))
            }
        }

        if (localObj?.audioArtifact == null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localArtifactAvailable, "false"))
        }

        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.hashOrSizeDiffers, "true"))
        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.antiResurrectionEnforced, "true"))

        return preconditions
    }

    private fun libraryApplyPreconditions(
        local: CanonicalLibraryObject?,
        peer: CanonicalLibraryObject?
    ): List<CanonicalApplyPrecondition> {
        val preconditions = mutableListOf<CanonicalApplyPrecondition>()

        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.libraryCapabilityPresent, "true"))
        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.supportedObjectKind, "true"))

        if (peer != null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerObjectPresent, "true"))
            if (local != null) {
                val peerModifiedAt = peer.businessModifiedAt
                val localModifiedAt = local.businessModifiedAt
                if (peerModifiedAt != null && localModifiedAt != null && peerModifiedAt.date.after(localModifiedAt.date)) {
                    preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerModifiedAtNewer, "true"))
                }
            } else {
                preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localMissingObject, "true"))
            }
        }

        return preconditions
    }

    private fun librarySendPreconditions(
        local: CanonicalLibraryObject?,
        peer: CanonicalLibraryObject?
    ): List<CanonicalApplyPrecondition> {
        val preconditions = mutableListOf<CanonicalApplyPrecondition>()

        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.libraryCapabilityPresent, "true"))
        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.supportedObjectKind, "true"))

        if (local != null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localObjectPresent, "true"))
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localObjectActive, "true"))

            if (peer != null) {
                val localModifiedAt = local.businessModifiedAt
                val peerModifiedAt = peer.businessModifiedAt
                if (localModifiedAt != null && peerModifiedAt != null && localModifiedAt.date.after(peerModifiedAt.date)) {
                    preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localModifiedAtNewer, "true"))
                }
            } else {
                preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerMissingObject, "true"))
            }
        }

        return preconditions
    }

    private fun libraryTombstoneApplyPreconditions(
        local: CanonicalLibraryObject?,
        peer: CanonicalLibraryObject?
    ): List<CanonicalApplyPrecondition> {
        val preconditions = mutableListOf<CanonicalApplyPrecondition>()

        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.noPhysicalDeleteEnforced, "true"))
        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.antiResurrectionEnforced, "true"))

        if (peer != null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerObjectPresent, "true"))
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerObjectTombstoned, "true"))
        }

        if (local != null) {
            val peerDeletedAt = peer?.deletedAt
            val localModifiedAt = local.businessModifiedAt
            if (peerDeletedAt != null && localModifiedAt != null && peerDeletedAt.date.after(localModifiedAt.date)) {
                preconditions.add(precondition(CanonicalApplyPrecondition.Kind.tombstoneNewerOrEqualThanActive, "true"))
            }
        } else {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localMissingObject, "true"))
        }

        return preconditions
    }

    private fun libraryTombstoneSendPreconditions(
        local: CanonicalLibraryObject?,
        peer: CanonicalLibraryObject?
    ): List<CanonicalApplyPrecondition> {
        val preconditions = mutableListOf<CanonicalApplyPrecondition>()

        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.noPhysicalDeleteEnforced, "true"))
        preconditions.add(precondition(CanonicalApplyPrecondition.Kind.antiResurrectionEnforced, "true"))

        if (local != null) {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localObjectPresent, "true"))
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.localObjectTombstoned, "true"))
        }

        if (peer != null) {
            val localDeletedAt = local?.deletedAt
            val peerModifiedAt = peer.businessModifiedAt
            if (localDeletedAt != null && peerModifiedAt != null && localDeletedAt.date.after(peerModifiedAt.date)) {
                preconditions.add(precondition(CanonicalApplyPrecondition.Kind.tombstoneNewerOrEqualThanActive, "true"))
            }
        } else {
            preconditions.add(precondition(CanonicalApplyPrecondition.Kind.peerMissingObject, "true"))
        }

        return preconditions
    }

    // ─── Conflict resolution helpers ───────────────────────────────

    private fun resolveRecordingConflictKind(decision: SyncDecision): CanonicalApplyConflictKind {
        return when {
            decision.reason.contains("metadata_hash_mismatch") ||
            decision.reason.contains("same_modified_at") ->
                CanonicalApplyConflictKind.recordingMetadataConcurrentEdit
            else ->
                CanonicalApplyConflictKind.genericConflict
        }
    }

    private fun resolveRecordingConflictPolicy(
        kind: CanonicalApplyConflictKind
    ): CanonicalConflictResolutionPolicy {
        return when (kind) {
            CanonicalApplyConflictKind.recordingMetadataConcurrentEdit ->
                CanonicalConflictResolutionPolicy.keepBothNoOverwrite
            CanonicalApplyConflictKind.activeVsTombstone,
            CanonicalApplyConflictKind.tombstoneVsActive ->
                CanonicalConflictResolutionPolicy.tombstoneRequiresManualReview
            else ->
                CanonicalConflictResolutionPolicy.manualReview
        }
    }

    private fun resolveLibraryObjectConflictKind(
        action: CanonicalLibrarySyncAction,
        localObj: CanonicalLibraryObject?
    ): CanonicalApplyConflictKind {
        return when (action.reason) {
            "activeNewerThanLocalTombstone" -> CanonicalApplyConflictKind.activeVsTombstone
            "activeNewerThanPeerTombstone" -> CanonicalApplyConflictKind.tombstoneVsActive
            "metadataTieConflict" -> CanonicalApplyConflictKind.activeVsActiveDivergingHash
            "metadataHashMismatchNoTimestamp" -> CanonicalApplyConflictKind.activeVsActiveDivergingHash
            else -> {
                when (localObj?.kind) {
                    CanonicalObjectKind.FOLDER -> CanonicalApplyConflictKind.folderMetadataConcurrentEdit
                    CanonicalObjectKind.STANDALONE_STUDY_ITEM,
                    CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM ->
                        CanonicalApplyConflictKind.studyItemMetadataConcurrentEdit
                    CanonicalObjectKind.STANDALONE_NOTE ->
                        CanonicalApplyConflictKind.studyItemMetadataConcurrentEdit
                    else -> CanonicalApplyConflictKind.genericConflict
                }
            }
        }
    }

    // ─── ID composition ────────────────────────────────────────────

    private fun composeRecordingConflictID(decision: SyncDecision): String {
        return listOf("applyConflict", decision.objectID, "metadata", decision.reason).joinToString("|")
    }

    private fun composeTransferConflictID(decision: TransferDecision): String {
        return listOf("applyConflict", decision.objectID, "audio", decision.reason).joinToString("|")
    }

    // ─── Library manifest helpers ──────────────────────────────────

    private fun localManifestLibraryMap(
        manifest: CanonicalManifest
    ): Map<CanonicalLibraryObjectID, CanonicalLibraryObject> {
        return manifest.libraryObjects.associateBy { it.objectID }
    }

    // ─── Factory helpers ───────────────────────────────────────────

    private fun action(
        kind: CanonicalApplyActionKind,
        target: CanonicalApplyTarget,
        source: CanonicalApplySource,
        result: CanonicalApplyResult,
        preconditions: List<CanonicalApplyPrecondition>,
        failureReason: CanonicalApplyFailureReason? = null,
        bridgeHint: CanonicalApplyBridgeHint? = null
    ) = CanonicalApplyAction(
        kind = kind,
        target = target,
        source = source,
        preconditions = preconditions,
        result = result,
        failureReason = failureReason,
        bridgeHint = bridgeHint
    )

    private fun precondition(
        kind: CanonicalApplyPrecondition.Kind,
        value: String
    ) = CanonicalApplyPrecondition(kind = kind, value = value)
}
