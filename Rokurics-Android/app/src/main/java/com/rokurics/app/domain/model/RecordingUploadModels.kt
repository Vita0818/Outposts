package com.rokurics.app.domain.model

// ── Upload Result ────────────────────────────────────────────────────

data class RecordingUploadResult(
    val recordingID: String,
    val metadataFileName: String? = null,
    val audioFileName: String? = null,
    val metadataDisposition: String? = null,
    val audioDisposition: String? = null
)

// ── Resume Context ───────────────────────────────────────────────────

data class RecordingUploadResumeContext(
    val metadataStage: String = "notStarted",
    val metadataDisposition: String? = null,
    val resumableSessionID: String? = null,
    val audioConfirmedBytes: Long? = null,
    val audioTotalBytes: Long? = null,
    val audioChunkSize: Int? = null,
    val audioTotalSHA256: String? = null
)

// ── Resumable Upload Session ─────────────────────────────────────────

data class ResumableAudioUploadStartRequest(
    val recordingID: String,
    val fileName: String,
    val totalBytes: Long,
    val totalSHA256: String,
    val chunkSize: Int,
    val metadataHash: String? = null,
    val uploadJobID: String? = null
)

data class ResumableAudioUploadStatusRequest(
    val recordingID: String,
    val sessionID: String,
    val totalSHA256: String
)

data class ResumableAudioUploadFinalizeRequest(
    val recordingID: String,
    val sessionID: String,
    val totalBytes: Long,
    val totalSHA256: String
)

data class ResumableAudioUploadSessionResponse(
    val ok: Boolean = false,
    val disposition: String? = null,
    val status: String? = null,
    val sessionID: String? = null,
    val confirmedBytes: Long = 0,
    val nextOffset: Long = 0,
    val chunkSize: Int? = null,
    val completed: Boolean = false,
    val finalAudioExists: Boolean? = null,
    val chunkAccepted: Boolean? = null,
    val finalAudioRelativePath: String? = null,
    val checksum: String? = null,
    val fileSize: Long? = null,
    val receiveStatus: String? = null,
    val processingStatus: String? = null,
    val error: String? = null,
    val reason: String? = null
)

// ── Upload Mode ──────────────────────────────────────────────────────

enum class RecordingUploadMode {
    SINGLE_REQUEST, RESUMABLE_CHUNKS
}

// ── Upload Job (persistent state) ────────────────────────────────────

data class RecordingUploadJob(
    val recordingID: String,
    val createdAt: Long = System.currentTimeMillis(),
    var updatedAt: Long = System.currentTimeMillis(),
    var metadataStage: RecordingUploadJobStageState = RecordingUploadJobStageState.PENDING,
    var audioStage: RecordingUploadJobStageState = RecordingUploadJobStageState.PENDING,
    var overallState: RecordingUploadJobOverallState = RecordingUploadJobOverallState.PENDING,
    var metadataDisposition: RecordingUploadJobDisposition = RecordingUploadJobDisposition.NONE,
    var audioDisposition: RecordingUploadJobDisposition = RecordingUploadJobDisposition.NONE,
    var attemptCount: Int = 0,
    var lastAttemptAt: Long? = null,
    var nextRetryAfter: Long? = null,
    var lastErrorCode: String? = null,
    var lastErrorMessage: String? = null,
    var isFatal: Boolean = false,
    val localAudioPath: String = "",
    var targetDeviceID: String? = null,
    var targetMacName: String? = null,
    var resumableSessionID: String? = null,
    var uploadMode: RecordingUploadMode? = null,
    var audioTotalBytes: Long = 0,
    var audioConfirmedBytes: Long = 0,
    var audioChunkSize: Int? = null,
    var audioTotalSHA256: String? = null,
    var audioNextOffset: Long = 0,
    var audioChunkCount: Int = 0,
    var audioCompletedChunkCount: Int = 0,
    var currentProgressFraction: Double? = null,
    var lastProgressAt: Long? = null,
    var resumableState: RecordingResumableUploadState? = null,
    var lastConfirmedByMacAt: Long? = null,
    var lastSessionStatusError: String? = null
)

enum class RecordingUploadJobStageState {
    PENDING, IN_PROGRESS, SUCCEEDED, FAILED
}

enum class RecordingUploadJobOverallState {
    PENDING, IN_PROGRESS, SUCCEEDED, RETRYABLE_FAILED, FATAL_FAILED
}

enum class RecordingUploadJobDisposition {
    NONE, ACCEPTED_NEW, ACCEPTED_EXISTING
}

enum class RecordingResumableUploadState {
    NOT_STARTED, STARTING, UPLOADING, PAUSED, RETRYABLE_FAILED, FINALIZING, COMPLETED, FATAL_FAILED
}

// ── Upload constants ─────────────────────────────────────────────────

object UploadConstants {
    const val SINGLE_REQUEST_AUDIO_MAX_BYTES: Long = 512 * 1024 * 1024  // 512 MB
    const val RESUMABLE_AUDIO_MAX_BYTES: Long = 16L * 1024 * 1024 * 1024 // 16 GB
    const val DEFAULT_RESUMABLE_THRESHOLD_BYTES: Long = 64 * 1024 * 1024 // 64 MB
    const val DEFAULT_RESUMABLE_CHUNK_SIZE: Int = 4 * 1024 * 1024         // 4 MB
    const val MAX_CHUNK_SIZE: Int = 8 * 1024 * 1024                       // 8 MB
}
