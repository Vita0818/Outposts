package com.rokurics.app.data

import com.rokurics.app.domain.model.*
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test

class UploadJobLedgerTest {

    @Test
    fun testEmptyLedgerReturnsNoJobs() {
        val ledger = UploadJobLedger()
        assertTrue(ledger.jobs.isEmpty())
        assertEquals(UploadJobLedger.CURRENT_VERSION, ledger.version)
    }

    @Test
    fun testDeduplicatedJobsKeepsLatest() {
        val old = RecordingUploadJob(
            recordingID = "rec-1",
            updatedAt = 1000L,
            overallState = RecordingUploadJobOverallState.PENDING
        )
        val newer = RecordingUploadJob(
            recordingID = "rec-1",
            updatedAt = 2000L,
            overallState = RecordingUploadJobOverallState.SUCCEEDED
        )
        val ledger = UploadJobLedger(jobs = listOf(old, newer))
        val deduped = ledger.deduplicatedJobs()
        assertEquals(1, deduped.size)
        assertEquals(RecordingUploadJobOverallState.SUCCEEDED, deduped[0].overallState)
    }

    @Test
    fun testUploadJobDefaultState() {
        val job = RecordingUploadJob(recordingID = "rec-1")
        assertEquals(RecordingUploadJobStageState.PENDING, job.metadataStage)
        assertEquals(RecordingUploadJobOverallState.PENDING, job.overallState)
        assertEquals(0, job.attemptCount)
        assertFalse(job.isFatal)
    }

    @Test
    fun testJobStateMachineTransition() {
        var job = RecordingUploadJob(recordingID = "rec-1")
        val now = System.currentTimeMillis()

        // Start
        job = job.copy(
            attemptCount = job.attemptCount + 1,
            lastAttemptAt = now,
            overallState = RecordingUploadJobOverallState.IN_PROGRESS,
            updatedAt = now
        )
        assertEquals(1, job.attemptCount)
        assertEquals(RecordingUploadJobOverallState.IN_PROGRESS, job.overallState)

        // Succeed
        job = job.copy(
            metadataStage = RecordingUploadJobStageState.SUCCEEDED,
            audioStage = RecordingUploadJobStageState.SUCCEEDED,
            overallState = RecordingUploadJobOverallState.SUCCEEDED,
            updatedAt = now
        )
        assertEquals(RecordingUploadJobOverallState.SUCCEEDED, job.overallState)

        // Failure
        val failed = job.copy(
            overallState = RecordingUploadJobOverallState.RETRYABLE_FAILED,
            lastErrorCode = "network_error",
            lastErrorMessage = "Connection lost",
            isFatal = false,
            updatedAt = now
        )
        assertEquals(RecordingUploadJobOverallState.RETRYABLE_FAILED, failed.overallState)
        assertFalse(failed.isFatal)
        assertEquals("network_error", failed.lastErrorCode)
    }

    @Test
    fun testFatalFailureIsNotRetryable() {
        val job = RecordingUploadJob(
            recordingID = "rec-1",
            overallState = RecordingUploadJobOverallState.FATAL_FAILED,
            isFatal = true
        )
        assertTrue(job.isFatal)
        assertEquals(RecordingUploadJobOverallState.FATAL_FAILED, job.overallState)
    }

    @Test
    fun testResumableSessionFields() {
        val job = RecordingUploadJob(
            recordingID = "rec-1",
            uploadMode = RecordingUploadMode.RESUMABLE_CHUNKS,
            resumableSessionID = "session-abc-123",
            audioTotalBytes = 128_000_000L,
            audioConfirmedBytes = 64_000_000L,
            audioChunkSize = 4_194_304,
            audioTotalSHA256 = "abcdef1234567890"
        )
        assertEquals(RecordingUploadMode.RESUMABLE_CHUNKS, job.uploadMode)
        assertEquals("session-abc-123", job.resumableSessionID)
        assertEquals(128_000_000L, job.audioTotalBytes)
        assertEquals(64_000_000L, job.audioConfirmedBytes)
    }

    @Test
    fun testCompletedJobHasCorrectState() {
        val now = System.currentTimeMillis()
        val job = RecordingUploadJob(
            recordingID = "rec-1",
            metadataStage = RecordingUploadJobStageState.SUCCEEDED,
            audioStage = RecordingUploadJobStageState.SUCCEEDED,
            overallState = RecordingUploadJobOverallState.SUCCEEDED,
            currentProgressFraction = 1.0,
            resumableState = RecordingResumableUploadState.COMPLETED,
            updatedAt = now
        )
        assertEquals(1.0, job.currentProgressFraction!!, 0.001)
        assertEquals(RecordingResumableUploadState.COMPLETED, job.resumableState)
    }

    @Test
    fun testProgressFractionCalculation() {
        val job = RecordingUploadJob(
            recordingID = "rec-1",
            audioTotalBytes = 100_000_000L,
            audioConfirmedBytes = 45_000_000L
        )
        val fraction = job.audioConfirmedBytes.toDouble() / job.audioTotalBytes
        assertEquals(0.45, fraction, 0.01)
    }

    @Test
    fun testRetryDelayCalculation() {
        val now = System.currentTimeMillis()

        // First failure: 5s delay
        var job = RecordingUploadJob(recordingID = "rec-1", attemptCount = 1)
        val delay1 = 5_000L
        assertEquals(now + delay1, now + delay1)

        // Second failure: 30s delay
        job = job.copy(attemptCount = 2)
        val delay2 = 30_000L
        assertTrue(delay2 > delay1)

        // Fourth failure: doubled from 120s base
        job = job.copy(attemptCount = 4)
        val delay4 = 120_000L * 2 // 240s
        assertEquals(240_000L, delay4)

        // Very high attempt: capped at 600s
        job = job.copy(attemptCount = 10)
        val delayMax = 600_000L
        assertEquals(600_000L, delayMax)
    }

    @Test
    fun testRetryableJobsFilter() {
        val now = System.currentTimeMillis()
        val retryable = RecordingUploadJob(
            recordingID = "rec-1",
            overallState = RecordingUploadJobOverallState.RETRYABLE_FAILED,
            isFatal = false,
            nextRetryAfter = now - 1000
        )
        val notYet = RecordingUploadJob(
            recordingID = "rec-2",
            overallState = RecordingUploadJobOverallState.RETRYABLE_FAILED,
            isFatal = false,
            nextRetryAfter = now + 60_000
        )
        val fatal = RecordingUploadJob(
            recordingID = "rec-3",
            overallState = RecordingUploadJobOverallState.FATAL_FAILED,
            isFatal = true
        )
        val succeeded = RecordingUploadJob(
            recordingID = "rec-4",
            overallState = RecordingUploadJobOverallState.SUCCEEDED
        )

        val all = listOf(retryable, notYet, fatal, succeeded)
        val eligible = all.filter { j ->
            val retryAfter = j.nextRetryAfter
            j.overallState == RecordingUploadJobOverallState.RETRYABLE_FAILED &&
                    !j.isFatal &&
                    (retryAfter == null || retryAfter <= now)
        }

        assertEquals(1, eligible.size)
        assertEquals("rec-1", eligible[0].recordingID)
    }

    @Test
    fun testUploadConstantsMatchApple() {
        assertEquals(512L * 1024 * 1024, UploadConstants.SINGLE_REQUEST_AUDIO_MAX_BYTES)
        assertEquals(16L * 1024 * 1024 * 1024, UploadConstants.RESUMABLE_AUDIO_MAX_BYTES)
        assertEquals(64L * 1024 * 1024, UploadConstants.DEFAULT_RESUMABLE_THRESHOLD_BYTES)
        assertEquals(4 * 1024 * 1024, UploadConstants.DEFAULT_RESUMABLE_CHUNK_SIZE)
    }

    @Test
    fun testLedgerVersionIs2() {
        assertEquals(2, UploadJobLedger.CURRENT_VERSION)
    }
}
