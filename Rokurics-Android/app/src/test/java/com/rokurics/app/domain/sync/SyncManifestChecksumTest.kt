package com.rokurics.app.domain.sync

import com.rokurics.app.domain.model.*
import org.junit.Assert.*
import org.junit.Test

class SyncManifestChecksumTest {

    @Test
    fun testSyncStateBackoffActive() {
        val futureTime = System.currentTimeMillis() + 60000
        val state = LocalNetworkSyncState(
            version = 1,
            consecutiveFailureCount = 3,
            nextAllowedSyncAt = futureTime
        )
        assertFalse(state.isSyncAllowed)
        assertTrue(state.backoffRemainingSeconds > 0)
    }

    @Test
    fun testSyncStateNoBackoffWhenNone() {
        val state = LocalNetworkSyncState(
            version = 1,
            consecutiveFailureCount = 0
        )
        assertTrue(state.isSyncAllowed)
    }

    @Test
    fun testSyncStateBackoffExpired() {
        val pastTime = System.currentTimeMillis() - 1000
        val state = LocalNetworkSyncState(
            version = 1,
            consecutiveFailureCount = 3,
            nextAllowedSyncAt = pastTime
        )
        assertTrue(state.isSyncAllowed)
    }

    @Test
    fun testApplyResultSummary() {
        val result = StudyLibrarySyncApplyResult(
            appliedItemCount = 3,
            appliedFolderCount = 1,
            conflictCount = 2,
            skippedOlderCount = 1
        )
        assertTrue(result.summaryText.contains("3 items"))
        assertTrue(result.summaryText.contains("2 conflicts preserved"))
        assertTrue(result.summaryText.contains("1 skipped"))
    }

    @Test
    fun testEmptyApplyResultSummary() {
        val result = StudyLibrarySyncApplyResult()
        assertEquals("no changes", result.summaryText)
    }

    @Test
    fun testDiffPlanEmptyHasNoWork() {
        val plan = LocalNetworkSyncDiffPlan()
        assertFalse(plan.hasWork)
        assertEquals("已同步", plan.summary)
    }

    @Test
    fun testArtifactKindAutoDownload() {
        assertTrue(LocalNetworkSyncArtifactKind.TRANSCRIPT_MARKDOWN.isAutoDownloadAllowed)
        assertTrue(LocalNetworkSyncArtifactKind.NOTE_MARKDOWN.isAutoDownloadAllowed)
        assertTrue(LocalNetworkSyncArtifactKind.NOTE_JSON.isAutoDownloadAllowed)
        assertFalse(LocalNetworkSyncArtifactKind.AUDIO.isAutoDownloadAllowed)
    }

    @Test
    fun testDiffPlanWithActionsHasWork() {
        val plan = LocalNetworkSyncDiffPlan(
            uploadMetadataActions = listOf(
                LocalNetworkSyncDiffAction(
                    id = "uploadMetadata:recording:rec1:peer_missing",
                    kind = LocalNetworkSyncDiffActionKind.UPLOAD_METADATA,
                    entityKind = "recording",
                    entityID = "rec1",
                    reason = "peer_missing"
                )
            ),
            downloadArtifactActions = listOf(
                LocalNetworkSyncDiffAction(
                    id = "downloadArtifact:artifact:a1:local_missing",
                    kind = LocalNetworkSyncDiffActionKind.DOWNLOAD_ARTIFACT,
                    entityKind = "artifact",
                    entityID = "a1",
                    reason = "local_missing"
                )
            )
        )
        assertTrue(plan.hasWork)
        assertTrue(plan.summary.contains("metadata"))
        assertTrue(plan.summary.contains("artifact"))
    }
}
