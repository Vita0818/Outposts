package com.rokurics.app.domain.canonical

import java.util.Date

data class CanonicalSyncPlan(
    val metadataDecisions: List<SyncDecision> = emptyList(),
    val libraryDecisions: List<SyncDecision> = emptyList(),
    val audioDecisions: List<TransferDecision> = emptyList(),
    val existenceTruths: List<CanonicalRecordingExistenceTruth> = emptyList(),
    val generatedAt: CanonicalTimestamp = CanonicalTimestamp(Date()),
    val localNodeID: String = "",
    val peerNodeID: String? = null
)

class CanonicalSyncPlanner {

    fun plan(
        localManifest: CanonicalManifest,
        peerManifest: CanonicalManifest?
    ): CanonicalSyncPlan {
        val peerKnown = peerManifest != null

        val localObjectsByID = localManifest.objects.associateBy { it.objectID }
        val peerObjectsByID = peerManifest?.objects?.associateBy { it.objectID } ?: emptyMap()
        val allObjectIDs = (localObjectsByID.keys + peerObjectsByID.keys).sorted()

        val metadataDecisions = mutableListOf<SyncDecision>()
        val audioDecisions = mutableListOf<TransferDecision>()
        val existenceTruths = mutableListOf<CanonicalRecordingExistenceTruth>()

        for (objectID in allObjectIDs) {
            val local = localObjectsByID[objectID]
            val peer = peerObjectsByID[objectID]

            when {
                local != null -> metadataDecisions.add(SyncDecision.metadata(local, peer))
                peer != null -> metadataDecisions.add(
                    SyncDecision(
                        kind = SyncDecision.Kind.DOWNLOAD_METADATA,
                        objectID = peer.objectID,
                        reason = "local_missing_metadata"
                    )
                )
            }

            if (local != null) {
                audioDecisions.add(TransferDecision.audio(local, peer))
            }

            existenceTruths.add(
                CanonicalRecordingExistenceTruth.evaluate(
                    objectID = objectID,
                    local = local,
                    peer = peer,
                    peerKnown = peerKnown
                )
            )
        }

        val localLibByID = localManifest.libraryObjects.associateBy { it.objectID.rawValue }
        val peerLibByID = peerManifest?.libraryObjects?.associateBy { it.objectID.rawValue } ?: emptyMap()
        val allLibIDs = (localLibByID.keys + peerLibByID.keys).sorted()

        val libraryDecisions = allLibIDs.mapNotNull { libID ->
            val localLib = localLibByID[libID]
            val peerLib = peerLibByID[libID]
            when {
                localLib != null && peerLib != null -> {
                    if (sameHash(localLib.metadataHash, peerLib.metadataHash)) {
                        SyncDecision(
                            kind = SyncDecision.Kind.NO_OP,
                            objectID = libID,
                            reason = "library_metadata_hash_equal"
                        )
                    } else {
                        SyncDecision(
                            kind = SyncDecision.Kind.CONFLICT,
                            objectID = libID,
                            reason = "library_metadata_hash_mismatch"
                        )
                    }
                }
                localLib != null -> SyncDecision(
                    kind = SyncDecision.Kind.UPLOAD_METADATA,
                    objectID = libID,
                    reason = "peer_missing_library_object"
                )
                peerLib != null -> SyncDecision(
                    kind = SyncDecision.Kind.DOWNLOAD_METADATA,
                    objectID = libID,
                    reason = "local_missing_library_object"
                )
                else -> null
            }
        }

        return CanonicalSyncPlan(
            metadataDecisions = metadataDecisions,
            libraryDecisions = libraryDecisions,
            audioDecisions = audioDecisions,
            existenceTruths = existenceTruths,
            generatedAt = CanonicalTimestamp(Date()),
            localNodeID = localManifest.node.nodeID,
            peerNodeID = peerManifest?.node?.nodeID
        )
    }

    private fun sameHash(left: CanonicalHash, right: CanonicalHash): Boolean {
        return left.algorithm == right.algorithm && left.value == right.value
    }
}
