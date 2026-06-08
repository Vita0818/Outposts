package com.rokurics.app.domain.canonical

data class CanonicalTransferJobID(
    val rawValue: String
)

enum class CanonicalTransferKind {
    RECORDING_AUDIO_UPLOAD,
    GENERATED_ARTIFACT_DOWNLOAD,
    METADATA_SEND,
    METADATA_APPLY,
    FOLDER_METADATA_SEND,
    FOLDER_METADATA_APPLY,
    STUDY_ITEM_METADATA_SEND,
    STUDY_ITEM_METADATA_APPLY,
    TOMBSTONE_SEND,
    TOMBSTONE_APPLY
}

enum class CanonicalTransferDirection {
    LOCAL_TO_PEER,
    PEER_TO_LOCAL,
    LOCAL_ONLY,
    PEER_ONLY
}

enum class CanonicalTransferPhase {
    NONE,
    PLANNED,
    QUEUED,
    IN_FLIGHT,
    FINALIZING,
    COMPLETED,
    FAILED_RETRYABLE,
    FAILED_FATAL,
    CONFLICT,
    DEFERRED,
    UNSUPPORTED
}

data class CanonicalTransferFailure(
    val code: String,
    val retryable: Boolean,
    val detail: String? = null
)

data class CanonicalRetryPolicySnapshot(
    val retryCount: Int,
    val nextRetryAt: CanonicalTimestamp? = null,
    val maxAttempts: Int? = null
)

data class CanonicalTransferJob(
    val jobID: CanonicalTransferJobID,
    val objectID: String,
    val artifactID: String? = null,
    val kind: CanonicalTransferKind,
    val direction: CanonicalTransferDirection,
    val phase: CanonicalTransferPhase,
    val failure: CanonicalTransferFailure? = null,
    val retryPolicy: CanonicalRetryPolicySnapshot? = null,
    val source: String? = null
) {
    val id: String get() = jobID.rawValue
}

data class CanonicalTransferProjection(
    val jobs: List<CanonicalTransferJob>,
    val generatedAt: CanonicalTimestamp
)

object CanonicalTransferProjectionBuilder {
    fun build(
        jobs: List<CanonicalTransferJob>,
        generatedAt: CanonicalTimestamp
    ): CanonicalTransferProjection {
        return CanonicalTransferProjection(
            jobs = jobs.sortedBy { it.jobID.rawValue },
            generatedAt = generatedAt
        )
    }
}
