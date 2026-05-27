package com.rokurics.app.domain.sync

import com.rokurics.app.domain.model.*
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test

class LocalNetworkSyncDiffPlannerTest {

    private lateinit var planner: LocalNetworkSyncDiffPlanner

    @Before
    fun setUp() {
        planner = LocalNetworkSyncDiffPlanner()
    }

    @Test
    fun testEmptyInventoriesProduceNoWork() {
        val local = makeInventory("local-device", "Local")
        val peer = makeInventory("peer-device", "Peer")
        val plan = planner.plan(local, peer, null)
        assertFalse(plan.hasWork)
    }

    @Test
    fun testPeerMissingRecordingTriggersUpload() {
        val local = makeInventory("local", "Local").copy(
            recordings = listOf(makeRecording("rec1", metadataHash = "abc", audioAvailable = true))
        )
        val peer = makeInventory("peer", "Peer")
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.uploadMetadataActions.any { it.entityID == "rec1" })
        assertTrue(plan.hasWork)
    }

    @Test
    fun testLocalMissingRecordingTriggersDownload() {
        val local = makeInventory("local", "Local")
        val peer = makeInventory("peer", "Peer").copy(
            recordings = listOf(makeRecording("rec1", metadataHash = "abc"))
        )
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.downloadMetadataActions.any { it.entityID == "rec1" })
    }

    @Test
    fun testEqualRecordingHashesProduceNoOp() {
        val rec = makeRecording("rec1", metadataHash = "abc123")
        val local = makeInventory("local", "Local").copy(recordings = listOf(rec))
        val peer = makeInventory("peer", "Peer").copy(recordings = listOf(rec))
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.noOps.any { it.entityID == "rec1" && it.reason == "metadata_equal" })
    }

    @Test
    fun testBothChangedAfterLastSyncProducesConflict() {
        val lastSync = System.currentTimeMillis() - 10000
        val localRec = makeRecording("rec1", metadataHash = "abc", updatedAt = lastSync + 5000)
        val peerRec = makeRecording("rec1", metadataHash = "def", updatedAt = lastSync + 3000)
        val local = makeInventory("local", "Local").copy(recordings = listOf(localRec))
        val peer = makeInventory("peer", "Peer").copy(recordings = listOf(peerRec))
        val plan = planner.plan(local, peer, lastSync)

        assertTrue(plan.conflictActions.any { it.entityID == "rec1" })
    }

    @Test
    fun testPeerNewerRecordingTriggersDownload() {
        val localRec = makeRecording("rec1", metadataHash = "abc", updatedAt = 1000)
        val peerRec = makeRecording("rec1", metadataHash = "def", updatedAt = 2000)
        val local = makeInventory("local", "Local").copy(recordings = listOf(localRec))
        val peer = makeInventory("peer", "Peer").copy(recordings = listOf(peerRec))
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.downloadMetadataActions.any { it.entityID == "rec1" })
    }

    @Test
    fun testLocalNewerRecordingTriggersUpload() {
        val localRec = makeRecording("rec1", metadataHash = "def", updatedAt = 2000)
        val peerRec = makeRecording("rec1", metadataHash = "abc", updatedAt = 1000)
        val local = makeInventory("local", "Local").copy(recordings = listOf(localRec))
        val peer = makeInventory("peer", "Peer").copy(recordings = listOf(peerRec))
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.uploadMetadataActions.any { it.entityID == "rec1" })
    }

    @Test
    fun testEqualFolderHashesProduceNoOp() {
        val folder = makeFolder("f1", revisionHash = "hash123")
        val local = makeInventory("local", "Local").copy(folders = listOf(folder))
        val peer = makeInventory("peer", "Peer").copy(folders = listOf(folder))
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.noOps.any { it.entityID == "f1" && it.reason == "checksum_equal" })
    }

    @Test
    fun testEqualItemHashesProduceNoOp() {
        val item = makeStudyItem("i1", "Test Item", "rec1", revisionHash = "hash456")
        val local = makeInventory("local", "Local").copy(studyItems = listOf(item))
        val peer = makeInventory("peer", "Peer").copy(studyItems = listOf(item))
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.noOps.any { it.entityID == "i1" && it.reason == "checksum_equal" })
    }

    @Test
    fun testEqualArtifactChecksumProducesNoOp() {
        val art = makeArtifact("artifact_abc", LocalNetworkSyncArtifactKind.NOTE_MARKDOWN, "i1", checksum = "csum")
        val local = makeInventory("local", "Local").copy(artifacts = listOf(art))
        val peer = makeInventory("peer", "Peer").copy(artifacts = listOf(art))
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.noOps.any { it.entityID == "artifact_abc" && it.reason == "checksum_equal" })
    }

    @Test
    fun testAudioArtifactDoesNotAutoDownload() {
        val local = makeInventory("local", "Local")
        val art = makeArtifact("artifact_audio", LocalNetworkSyncArtifactKind.AUDIO, "i1", checksum = "audio1", updatedAt = 5000)
        val peer = makeInventory("peer", "Peer").copy(artifacts = listOf(art))
        val plan = planner.plan(local, peer, null)

        assertFalse(plan.downloadArtifactActions.any { it.entityID == "artifact_audio" })
    }

    @Test
    fun testDiffPlanSummary() {
        val localRec = makeRecording("rec1", metadataHash = "abc", audioAvailable = true, updatedAt = 2000)
        val local = makeInventory("local", "Local").copy(recordings = listOf(localRec))
        val peer = makeInventory("peer", "Peer")
        val plan = planner.plan(local, peer, null)

        assertTrue(plan.summary.contains("metadata"))
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private fun makeInventory(deviceID: String, deviceName: String) = LocalNetworkSyncInventory(
        device = LocalNetworkSyncDeviceSection(
            deviceID = deviceID,
            deviceName = deviceName,
            platform = LocalNetworkSyncPlatform.ANDROID
        )
    )

    private fun makeRecording(
        id: String,
        metadataHash: String? = null,
        audioAvailable: Boolean = false,
        updatedAt: Long = System.currentTimeMillis()
    ) = LocalNetworkSyncRecordingEntry(
        recordingID = id,
        metadataHash = metadataHash,
        audioAvailable = audioAvailable,
        updatedAt = updatedAt
    )

    private fun makeFolder(
        id: String,
        name: String = "Test Folder",
        revisionHash: String? = null,
        updatedAt: Long = System.currentTimeMillis()
    ) = LocalNetworkSyncFolderEntry(
        folderID = id,
        name = name,
        updatedAt = updatedAt,
        revisionHash = revisionHash
    )

    private fun makeStudyItem(
        id: String,
        title: String = "Test Item",
        recordingID: String? = null,
        revisionHash: String? = null,
        updatedAt: Long = System.currentTimeMillis()
    ) = LocalNetworkSyncStudyItemEntry(
        itemID = id,
        title = title,
        recordingID = recordingID,
        updatedAt = updatedAt,
        revisionHash = revisionHash
    )

    private fun makeArtifact(
        artifactID: String,
        kind: LocalNetworkSyncArtifactKind,
        ownerID: String = "i1",
        checksum: String? = null,
        updatedAt: Long = System.currentTimeMillis()
    ) = LocalNetworkSyncArtifactEntry(
        artifactID = artifactID,
        kind = kind,
        ownerID = ownerID,
        checksum = checksum,
        updatedAt = updatedAt
    )
}
