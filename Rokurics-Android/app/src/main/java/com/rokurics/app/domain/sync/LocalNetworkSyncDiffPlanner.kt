package com.rokurics.app.domain.sync

import com.rokurics.app.domain.model.*

class LocalNetworkSyncDiffPlanner {

    fun plan(
        local: LocalNetworkSyncInventory,
        peer: LocalNetworkSyncInventory,
        lastSuccessfulSyncAt: Long?
    ): LocalNetworkSyncDiffPlan {
        val plan = LocalNetworkSyncDiffPlan(
            uploadMetadataActions = mutableListOf(),
            uploadArtifactActions = mutableListOf(),
            downloadMetadataActions = mutableListOf(),
            downloadArtifactActions = mutableListOf(),
            uploadRecordingAudioActions = mutableListOf(),
            conflictActions = mutableListOf(),
            noOps = mutableListOf()
        )

        // Compare recordings
        val localRecMap = local.recordings.associateBy { it.recordingID }
        val peerRecMap = peer.recordings.associateBy { it.recordingID }
        val allRecIDs = (localRecMap.keys + peerRecMap.keys).toSet()

        for (recID in allRecIDs) {
            val actions = compareRecordings(recID, localRecMap[recID], peerRecMap[recID], lastSuccessfulSyncAt)
            actions.forEach { addAction(plan, it) }
        }

        // Compare folders
        val localFolderMap = local.folders.associateBy { it.folderID }
        val peerFolderMap = peer.folders.associateBy { it.folderID }
        val allFolderIDs = (localFolderMap.keys + peerFolderMap.keys).toSet()

        for (fID in allFolderIDs) {
            val action = compareFolders(fID, localFolderMap[fID], peerFolderMap[fID], lastSuccessfulSyncAt)
            addAction(plan, action)
        }

        // Compare study items
        val localItemMap = local.studyItems.associateBy { it.itemID }
        val peerItemMap = peer.studyItems.associateBy { it.itemID }
        val allItemIDs = (localItemMap.keys + peerItemMap.keys).toSet()

        for (iID in allItemIDs) {
            val action = compareStudyItems(iID, localItemMap[iID], peerItemMap[iID], lastSuccessfulSyncAt)
            addAction(plan, action)
        }

        // Compare artifacts
        val localArtMap = local.artifacts.associateBy { it.artifactID }
        val peerArtMap = peer.artifacts.associateBy { it.artifactID }
        val allArtIDs = (localArtMap.keys + peerArtMap.keys).toSet()

        for (aID in allArtIDs) {
            val action = compareArtifacts(aID, localArtMap[aID], peerArtMap[aID], lastSuccessfulSyncAt)
            addAction(plan, action)
        }

        return plan
    }

    private fun addAction(plan: LocalNetworkSyncDiffPlan, action: LocalNetworkSyncDiffAction) {
        when (action.kind) {
            LocalNetworkSyncDiffActionKind.UPLOAD_METADATA -> (plan.uploadMetadataActions as MutableList).add(action)
            LocalNetworkSyncDiffActionKind.UPLOAD_ARTIFACT -> (plan.uploadArtifactActions as MutableList).add(action)
            LocalNetworkSyncDiffActionKind.DOWNLOAD_METADATA -> (plan.downloadMetadataActions as MutableList).add(action)
            LocalNetworkSyncDiffActionKind.DOWNLOAD_ARTIFACT -> (plan.downloadArtifactActions as MutableList).add(action)
            LocalNetworkSyncDiffActionKind.UPLOAD_RECORDING_AUDIO -> (plan.uploadRecordingAudioActions as MutableList).add(action)
            LocalNetworkSyncDiffActionKind.CONFLICT -> (plan.conflictActions as MutableList).add(action)
            LocalNetworkSyncDiffActionKind.NO_OP -> (plan.noOps as MutableList).add(action)
        }
    }

    // ── Recording Comparison ─────────────────────────────────────────

    private fun compareRecordings(
        recID: String,
        local: LocalNetworkSyncRecordingEntry?,
        peer: LocalNetworkSyncRecordingEntry?,
        lastSuccessfulSyncAt: Long?
    ): List<LocalNetworkSyncDiffAction> {
        val actions = mutableListOf<LocalNetworkSyncDiffAction>()

        // Both exist
        if (local != null && peer != null) {
            if (local.metadataHash != null && local.metadataHash == peer.metadataHash) {
                actions.add(diffAction(LocalNetworkSyncDiffActionKind.NO_OP, "recording", recID, "metadata_equal"))
            } else if (lastSuccessfulSyncAt != null &&
                local.updatedAt > lastSuccessfulSyncAt &&
                peer.updatedAt > lastSuccessfulSyncAt) {
                actions.add(diffAction(LocalNetworkSyncDiffActionKind.CONFLICT, "recording", recID, "both_changed_after_last_sync"))
            } else if (peer.deleted && peer.updatedAt >= local.updatedAt) {
                actions.add(diffAction(LocalNetworkSyncDiffActionKind.DOWNLOAD_METADATA, "recording", recID, "peer_tombstone_wins"))
            } else if (local.deleted && local.updatedAt >= peer.updatedAt) {
                actions.add(diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_METADATA, "recording", recID, "local_tombstone_wins"))
            } else if (local.updatedAt > peer.updatedAt) {
                actions.add(diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_METADATA, "recording", recID, "local_recording_newer"))
            } else {
                actions.add(diffAction(LocalNetworkSyncDiffActionKind.DOWNLOAD_METADATA, "recording", recID, "peer_recording_newer"))
            }
            // Audio check: if local has audio and peer doesn't
            if (local.audioAvailable && (peer.audioChecksum == null || !peer.audioAvailable)) {
                actions.add(diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_RECORDING_AUDIO, "recording", recID, "peer_missing_audio_use_existing_upload"))
            }
            return actions
        }

        // Local only
        if (local != null) {
            actions.add(diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_METADATA, "recording", recID, "peer_missing_recording"))
            if (local.audioAvailable) {
                actions.add(diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_RECORDING_AUDIO, "recording", recID, "peer_missing_audio"))
            }
            return actions
        }

        // Peer only
        actions.add(diffAction(LocalNetworkSyncDiffActionKind.DOWNLOAD_METADATA, "recording", recID, "local_missing_recording_metadata"))
        return actions
    }

    // ── Folder Comparison ────────────────────────────────────────────

    private fun compareFolders(
        fID: String,
        local: LocalNetworkSyncFolderEntry?,
        peer: LocalNetworkSyncFolderEntry?,
        lastSuccessfulSyncAt: Long?
    ): LocalNetworkSyncDiffAction {
        return compareMetadataEntity(
            "folder", fID,
            local?.revisionHash, local?.updatedAt, local?.deleted ?: false,
            peer?.revisionHash, peer?.updatedAt, peer?.deleted ?: false,
            lastSuccessfulSyncAt
        )
    }

    // ── Study Item Comparison ────────────────────────────────────────

    private fun compareStudyItems(
        iID: String,
        local: LocalNetworkSyncStudyItemEntry?,
        peer: LocalNetworkSyncStudyItemEntry?,
        lastSuccessfulSyncAt: Long?
    ): LocalNetworkSyncDiffAction {
        return compareMetadataEntity(
            "studyItem", iID,
            local?.revisionHash, local?.updatedAt, local?.deleted ?: false,
            peer?.revisionHash, peer?.updatedAt, peer?.deleted ?: false,
            lastSuccessfulSyncAt
        )
    }

    // ── Shared Metadata Entity Comparison ────────────────────────────

    private fun compareMetadataEntity(
        entityKind: String,
        entityID: String,
        localHash: String?, localUpdatedAt: Long?, localDeleted: Boolean,
        peerHash: String?, peerUpdatedAt: Long?, peerDeleted: Boolean,
        lastSuccessfulSyncAt: Long?
    ): LocalNetworkSyncDiffAction {
        val lUp = localUpdatedAt ?: 0L
        val pUp = peerUpdatedAt ?: 0L

        // Both exist
        if (localHash != null && peerHash != null) {
            // Hashes equal
            if (localHash == peerHash) {
                return diffAction(LocalNetworkSyncDiffActionKind.NO_OP, entityKind, entityID, "checksum_equal")
            }
            // Both changed after last sync
            if (lastSuccessfulSyncAt != null && lUp > lastSuccessfulSyncAt && pUp > lastSuccessfulSyncAt) {
                return diffAction(LocalNetworkSyncDiffActionKind.CONFLICT, entityKind, entityID, "both_changed_after_last_sync")
            }
            // Peer deleted, peer newer
            if (peerDeleted && pUp >= lUp) {
                return diffAction(LocalNetworkSyncDiffActionKind.DOWNLOAD_METADATA, entityKind, entityID, "peer_tombstone_wins")
            }
            // Local deleted, local newer
            if (localDeleted && lUp >= pUp) {
                return diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_METADATA, entityKind, entityID, "local_tombstone_wins")
            }
            // Peer newer
            if (pUp > lUp) {
                return diffAction(LocalNetworkSyncDiffActionKind.DOWNLOAD_METADATA, entityKind, entityID, "peer_newer")
            }
            // Local newer (default)
            return diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_METADATA, entityKind, entityID, "local_newer")
        }

        // Local only
        if (localHash != null) {
            return diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_METADATA, entityKind, entityID, "peer_missing")
        }

        // Peer only
        return diffAction(LocalNetworkSyncDiffActionKind.DOWNLOAD_METADATA, entityKind, entityID, "local_missing")
    }

    // ── Artifact Comparison ──────────────────────────────────────────

    private fun compareArtifacts(
        aID: String,
        local: LocalNetworkSyncArtifactEntry?,
        peer: LocalNetworkSyncArtifactEntry?,
        lastSuccessfulSyncAt: Long?
    ): LocalNetworkSyncDiffAction {
        // Both exist
        if (local != null && peer != null) {
            // Checksums equal
            if (local.checksum != null && local.checksum == peer.checksum) {
                return diffAction(LocalNetworkSyncDiffActionKind.NO_OP, "artifact", aID, "checksum_equal")
            }
            // Peer newer and auto-download allowed
            if (peer.updatedAt > local.updatedAt && peer.kind.isAutoDownloadAllowed) {
                return diffAction(LocalNetworkSyncDiffActionKind.DOWNLOAD_ARTIFACT, "artifact", aID, "peer_artifact_newer")
            }
            // Local newer and not audio
            if (local.updatedAt > peer.updatedAt && local.kind != LocalNetworkSyncArtifactKind.AUDIO) {
                return diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_ARTIFACT, "artifact", aID, "local_artifact_newer")
            }
            // Audio - no auto-download
            if (local.kind == LocalNetworkSyncArtifactKind.AUDIO) {
                return diffAction(LocalNetworkSyncDiffActionKind.NO_OP, "artifact", aID, "audio_uses_recording_upload")
            }
            // Checksum mismatch without clear winner
            return diffAction(LocalNetworkSyncDiffActionKind.CONFLICT, "artifact", aID, "artifact_checksum_conflict")
        }

        // Local only
        if (local != null) {
            if (local.kind != LocalNetworkSyncArtifactKind.AUDIO) {
                return diffAction(LocalNetworkSyncDiffActionKind.UPLOAD_ARTIFACT, "artifact", aID, "peer_missing_artifact")
            }
            return diffAction(LocalNetworkSyncDiffActionKind.NO_OP, "artifact", aID, "audio_auto_download_disabled")
        }

        // Peer only
        if (peer != null) {
            if (peer.kind.isAutoDownloadAllowed) {
                return diffAction(LocalNetworkSyncDiffActionKind.DOWNLOAD_ARTIFACT, "artifact", aID, "local_missing_artifact")
            }
            return diffAction(LocalNetworkSyncDiffActionKind.NO_OP, "artifact", aID, "audio_auto_download_disabled")
        }

        return diffAction(LocalNetworkSyncDiffActionKind.NO_OP, "artifact", aID, "both_missing")
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private fun diffAction(
        kind: LocalNetworkSyncDiffActionKind,
        entityKind: String,
        entityID: String,
        reason: String
    ): LocalNetworkSyncDiffAction {
        return LocalNetworkSyncDiffAction(
            id = "${kind.rawValue}:$entityKind:$entityID:$reason",
            kind = kind,
            entityKind = entityKind,
            entityID = entityID,
            reason = reason
        )
    }
}
