package com.rokurics.app.domain.model

import org.junit.Assert.*
import org.junit.Test
import java.util.Date

class RecordingMetadataTest {

    @Test
    fun copy_preservesFields() {
        val original = RecordingMetadata(
            id = "test-id-123",
            title = "测试录音",
            fileName = "test.m4a",
            relativeAudioPath = "Recordings/test.m4a",
            relativeMetadataPath = "Metadata/test-id-123.json",
            duration = 120.5,
            fileSize = 1234567L,
            uploadStatus = "localOnly",
            transcriptionStatus = "notStarted",
            noteStatus = "notStarted"
        )
        val copy = original.copy()

        assertEquals(original.id, copy.id)
        assertEquals(original.title, copy.title)
        assertEquals(original.duration, copy.duration, 0.01)
        assertEquals(original.fileSize, copy.fileSize)
        assertEquals(original.uploadStatus, copy.uploadStatus)
    }

    @Test
    fun constructor_defaults() {
        val metadata = RecordingMetadata(
            title = "Default Test",
            fileName = "default.m4a",
            relativeAudioPath = "Recordings/default.m4a",
            relativeMetadataPath = "Metadata/default.json"
        )
        assertEquals("localOnly", metadata.uploadStatus)
        assertEquals("notStarted", metadata.transcriptionStatus)
        assertEquals("notStarted", metadata.noteStatus)
        assertFalse(metadata.isDeleted)
    }

    @Test
    fun defaultTitle_includesDateFormat() {
        val date = Date(1715702400000L) // 2024-05-14T16:00:00Z
        val title = RecordingMetadata.defaultTitle(date)
        assertTrue(title.startsWith("录音"))
    }

    @Test
    fun recordingUploadStatus_fromRawValue() {
        assertEquals(RecordingUploadStatus.LOCAL_ONLY, RecordingUploadStatus.fromRawValue("localOnly"))
        assertEquals(RecordingUploadStatus.PENDING, RecordingUploadStatus.fromRawValue("pending"))
        assertEquals(RecordingUploadStatus.UPLOADING, RecordingUploadStatus.fromRawValue("uploading"))
        assertEquals(RecordingUploadStatus.UPLOADED, RecordingUploadStatus.fromRawValue("uploaded"))
        assertEquals(RecordingUploadStatus.FAILED, RecordingUploadStatus.fromRawValue("failed"))
        assertEquals(RecordingUploadStatus.LOCAL_ONLY, RecordingUploadStatus.fromRawValue("unknown"))
    }
}
