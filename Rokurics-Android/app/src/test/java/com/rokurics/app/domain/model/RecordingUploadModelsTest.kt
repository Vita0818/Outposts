package com.rokurics.app.domain.model

import org.junit.Assert.*
import org.junit.Test

class RecordingUploadModelsTest {

    @Test
    fun testResumableThresholdIs64MB() {
        assertEquals(64 * 1024 * 1024, UploadConstants.DEFAULT_RESUMABLE_THRESHOLD_BYTES)
    }

    @Test
    fun testResumableChunkSizeIs4MB() {
        assertEquals(4 * 1024 * 1024, UploadConstants.DEFAULT_RESUMABLE_CHUNK_SIZE)
    }

    @Test
    fun testMaxChunkSizeIs8MB() {
        assertEquals(8 * 1024 * 1024, UploadConstants.MAX_CHUNK_SIZE)
    }

    @Test
    fun testSingleRequestMaxIs512MB() {
        assertEquals(512L * 1024 * 1024, UploadConstants.SINGLE_REQUEST_AUDIO_MAX_BYTES)
    }

    @Test
    fun testResumableMaxIs16GB() {
        assertEquals(16L * 1024 * 1024 * 1024, UploadConstants.RESUMABLE_AUDIO_MAX_BYTES)
    }

    @Test
    fun testResumeContextDefaultValues() {
        val context = RecordingUploadResumeContext()
        assertEquals("notStarted", context.metadataStage)
        assertNull(context.resumableSessionID)
        assertNull(context.audioConfirmedBytes)
        assertNull(context.audioTotalSHA256)
    }

    @Test
    fun testResumeContextWithSession() {
        val context = RecordingUploadResumeContext(
            metadataStage = "succeeded",
            resumableSessionID = "session-123",
            audioConfirmedBytes = 4096L,
            audioTotalBytes = 128_000_000L,
            audioChunkSize = 4_194_304,
            audioTotalSHA256 = "abc123"
        )
        assertEquals("succeeded", context.metadataStage)
        assertEquals("session-123", context.resumableSessionID)
        assertEquals(4096L, context.audioConfirmedBytes)
    }

    @Test
    fun testSessionResponseDefaults() {
        val resp = ResumableAudioUploadSessionResponse()
        assertFalse(resp.ok)
        assertEquals(0L, resp.confirmedBytes)
        assertEquals(0L, resp.nextOffset)
        assertFalse(resp.completed)
        assertNull(resp.sessionID)
    }

    @Test
    fun testSessionResponseCompleted() {
        val resp = ResumableAudioUploadSessionResponse(
            ok = true,
            sessionID = "sess-1",
            completed = true,
            finalAudioExists = true
        )
        assertTrue(resp.ok)
        assertTrue(resp.completed)
        assertTrue(resp.finalAudioExists!!)
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
    fun testUploadJobStateTransitions() {
        val job = RecordingUploadJob(
            recordingID = "rec-1",
            metadataStage = RecordingUploadJobStageState.SUCCEEDED,
            audioStage = RecordingUploadJobStageState.IN_PROGRESS,
            overallState = RecordingUploadJobOverallState.IN_PROGRESS,
            uploadMode = RecordingUploadMode.RESUMABLE_CHUNKS,
            resumableSessionID = "session-abc",
            audioTotalBytes = 128_000_000L,
            audioConfirmedBytes = 64_000_000L,
            audioChunkSize = 4_194_304,
            audioTotalSHA256 = "sha256hex"
        )
        assertEquals(RecordingUploadMode.RESUMABLE_CHUNKS, job.uploadMode)
        assertEquals("session-abc", job.resumableSessionID)
        assertEquals(64_000_000L, job.audioConfirmedBytes)
        assertEquals(128_000_000L, job.audioTotalBytes)
    }

    @Test
    fun testUploadModeEnumValues() {
        assertEquals(2, RecordingUploadMode.values().size)
        assertEquals(RecordingUploadMode.SINGLE_REQUEST, RecordingUploadMode.valueOf("SINGLE_REQUEST"))
        assertEquals(RecordingUploadMode.RESUMABLE_CHUNKS, RecordingUploadMode.valueOf("RESUMABLE_CHUNKS"))
    }

    @Test
    fun testResumableUploadStateEnumValues() {
        assertEquals(8, RecordingResumableUploadState.values().size)
        assertNotNull(RecordingResumableUploadState.valueOf("NOT_STARTED"))
        assertNotNull(RecordingResumableUploadState.valueOf("COMPLETED"))
    }

    @Test
    fun testJobDispositionEnumValues() {
        assertEquals(3, RecordingUploadJobDisposition.values().size)
        assertEquals(RecordingUploadJobDisposition.NONE, RecordingUploadJobDisposition.valueOf("NONE"))
        assertEquals(RecordingUploadJobDisposition.ACCEPTED_NEW, RecordingUploadJobDisposition.valueOf("ACCEPTED_NEW"))
        assertEquals(RecordingUploadJobDisposition.ACCEPTED_EXISTING, RecordingUploadJobDisposition.valueOf("ACCEPTED_EXISTING"))
    }
}
