package com.rokurics.app.data

import com.rokurics.app.domain.model.*
import kotlinx.coroutines.*
import java.io.RandomAccessFile
import kotlin.math.min
import kotlin.math.pow

class UploadQueue(
    private val uploadClient: SecureUploadClient = SecureUploadClient(),
    private val connectionStore: ConnectionStore = ConnectionStore(),
    private val ledgerStore: UploadJobLedgerStore = UploadJobLedgerStore(),
    private val dispatcher: CoroutineDispatcher = Dispatchers.IO
) {
    private val scope = CoroutineScope(SupervisorJob() + dispatcher)
    private val maxRetries = 5
    private val baseDelayMs = 2_000L
    private val maxDelayMs = 300_000L

    // ── Recovery on creation ──────────────────────────────────────

    init {
        scope.launch {
            recoverStaleJobs()
        }
    }

    private suspend fun recoverStaleJobs() {
        val recovered = withContext(dispatcher) {
            ledgerStore.recoverStaleInProgressJobs()
        }
        // Propagation to metadata could be done here if RecordingManager is available
        // For now, the ledger itself is corrected; metadata will reflect on next load
    }

    fun enqueue(recording: RecordingMetadata, onStatusUpdate: (RecordingMetadata) -> Unit) {
        val settings = connectionStore.snapshot
        if (!settings.isPaired) {
            onStatusUpdate(recording.copy(
                uploadStatus = RecordingUploadStatus.FAILED.rawValue,
                uploadProgressDescription = "未配对，无法上传"
            ))
            return
        }
        scope.launch {
            // Ensure job in ledger
            val audioFile = AudioFileStore().audioFileFor(recording)
            ledgerStore.ensureJob(recording, settings, audioFile.absolutePath)
            ledgerStore.markAttemptStarted(recording.id)
            attemptUpload(recording, settings, 0, onStatusUpdate)
        }
    }

    fun cancelAll() {
        scope.coroutineContext.cancelChildren()
    }

    // ── Main upload entry ────────────────────────────────────────────

    private suspend fun attemptUpload(
        recording: RecordingMetadata,
        settings: SecureMacConnectionSnapshot,
        attempt: Int,
        onStatusUpdate: (RecordingMetadata) -> Unit
    ) {
        withContext(Dispatchers.Main) {
            onStatusUpdate(recording.copy(
                uploadStatus = RecordingUploadStatus.UPLOADING.rawValue,
                uploadProgressDescription = if (attempt > 0) "重试 $attempt/$maxRetries" else "准备上传",
                uploadProgressFraction = 0.0
            ))
        }

        val audioFile = AudioFileStore().audioFileFor(recording)
        if (!audioFile.exists()) {
            ledgerStore.markFailure(recording.id, "audio_file_missing", "音频文件不存在", isFatal = true)
            withContext(Dispatchers.Main) {
                onStatusUpdate(recording.copy(
                    uploadStatus = RecordingUploadStatus.FAILED.rawValue,
                    uploadProgressDescription = "音频文件不存在"
                ))
            }
            return
        }

        val fileSize = audioFile.length()
        val mode = selectUploadMode(fileSize)

        try {
            when (mode) {
                RecordingUploadMode.SINGLE_REQUEST -> {
                    uploadSingleRequest(recording, audioFile, settings, onStatusUpdate)
                }
                RecordingUploadMode.RESUMABLE_CHUNKS -> {
                    uploadResumableChunks(recording, audioFile, fileSize, settings, onStatusUpdate)
                }
            }
            // Mark success in ledger
            ledgerStore.markSucceeded(recording.id)
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            ledgerStore.markFailure(
                recording.id,
                "upload_failed",
                e.message ?: "unknown",
                isFatal = false
            )
            handleFailure(recording, settings, attempt, onStatusUpdate, e.message ?: "unknown")
        }
    }

    // ── Mode selection ───────────────────────────────────────────────

    private fun selectUploadMode(fileSize: Long): RecordingUploadMode {
        return if (fileSize >= UploadConstants.DEFAULT_RESUMABLE_THRESHOLD_BYTES) {
            RecordingUploadMode.RESUMABLE_CHUNKS
        } else {
            RecordingUploadMode.SINGLE_REQUEST
        }
    }

    // ── Single-request upload ────────────────────────────────────────

    private suspend fun uploadSingleRequest(
        recording: RecordingMetadata,
        audioFile: java.io.File,
        settings: SecureMacConnectionSnapshot,
        onStatusUpdate: (RecordingMetadata) -> Unit
    ) {
        withContext(Dispatchers.Main) {
            onStatusUpdate(recording.copy(
                uploadProgressDescription = "上传中",
                uploadProgressFraction = 0.5
            ))
        }

        val result = uploadClient.uploadSignedFile(
            settings = settings,
            path = "/upload-recording-audio",
            file = audioFile,
            contentType = "audio/mp4",
            uploadType = "recording-audio",
            recordingID = recording.id,
            fileName = recording.fileName
        )

        result.fold(
            onSuccess = { response ->
                if (response.ok) {
                    withContext(Dispatchers.Main) {
                        onStatusUpdate(recording.copy(
                            uploadStatus = RecordingUploadStatus.UPLOADED.rawValue,
                            uploadProgressDescription = "已上传",
                            uploadProgressFraction = 1.0
                        ))
                    }
                } else {
                    throw RuntimeException(response.error ?: "upload_rejected")
                }
            },
            onFailure = { throw it }
        )
    }

    // ── Resumable chunked upload ─────────────────────────────────────

    private suspend fun uploadResumableChunks(
        recording: RecordingMetadata,
        audioFile: java.io.File,
        fileSize: Long,
        settings: SecureMacConnectionSnapshot,
        onStatusUpdate: (RecordingMetadata) -> Unit
    ) {
        val totalSHA256 = SecureUploadUtilities.sha256Hex(audioFile)
        val chunkSize = UploadConstants.DEFAULT_RESUMABLE_CHUNK_SIZE

        withContext(Dispatchers.Main) {
            onStatusUpdate(recording.copy(
                uploadProgressDescription = "计算文件校验值...",
                uploadProgressFraction = 0.01
            ))
        }

        // 1. Start session
        val startRequest = ResumableAudioUploadStartRequest(
            recordingID = recording.id,
            fileName = recording.fileName,
            totalBytes = fileSize,
            totalSHA256 = totalSHA256,
            chunkSize = chunkSize,
            uploadJobID = recording.id
        )

        val startResult = uploadClient.startResumableUploadSession(settings, startRequest)
        if (startResult.isFailure) throw startResult.exceptionOrNull()!!

        val sessionResponse = startResult.getOrThrow()
        if (!sessionResponse.ok) {
            throw RuntimeException(sessionResponse.error ?: "session_start_failed")
        }

        // Already completed
        if (sessionResponse.completed || sessionResponse.finalAudioExists == true) {
            withContext(Dispatchers.Main) {
                onStatusUpdate(recording.copy(
                    uploadStatus = RecordingUploadStatus.UPLOADED.rawValue,
                    uploadProgressDescription = "已在 Mac 上完成",
                    uploadProgressFraction = 1.0
                ))
            }
            return
        }

        val sessionID = sessionResponse.sessionID ?: throw RuntimeException("no_session_id")
        val effectiveChunkSize = sessionResponse.chunkSize ?: chunkSize
        var confirmed = sessionResponse.confirmedBytes
        var nextOffset = sessionResponse.nextOffset

        // Persist session to ledger
        ledgerStore.markSessionStarted(recording.id, sessionID, fileSize, effectiveChunkSize, totalSHA256)

        // 2. Send chunks
        var completedChunks = 0
        RandomAccessFile(audioFile, "r").use { raf ->
            while (confirmed < fileSize) {
                raf.seek(nextOffset)
                val remaining = fileSize - nextOffset
                val readSize = minOf(effectiveChunkSize.toLong(), remaining).toInt()
                val buffer = ByteArray(readSize)
                raf.readFully(buffer)

                val chunkResult = uploadClient.uploadResumableAudioChunk(
                    settings = settings,
                    recordingID = recording.id,
                    sessionID = sessionID,
                    chunk = buffer,
                    offset = nextOffset,
                    totalSHA256 = totalSHA256
                )

                if (chunkResult.isFailure) throw chunkResult.exceptionOrNull()!!

                val chunkResponse = chunkResult.getOrThrow()
                if (!chunkResponse.ok && chunkResponse.chunkAccepted != true) {
                    throw RuntimeException(chunkResponse.error ?: "chunk_rejected")
                }

                // Server-authoritative byte tracking
                confirmed = chunkResponse.confirmedBytes
                nextOffset = chunkResponse.nextOffset
                completedChunks++

                val progress = if (fileSize > 0) confirmed.toDouble() / fileSize else 0.0

                // Persist chunk progress to ledger
                ledgerStore.markChunkCompleted(
                    recording.id, confirmed, completedChunks, progress
                )

                withContext(Dispatchers.Main) {
                    onStatusUpdate(recording.copy(
                        uploadStatus = RecordingUploadStatus.UPLOADING.rawValue,
                        uploadProgressDescription = "上传中 ${(progress * 100).toInt()}%",
                        uploadProgressFraction = progress,
                        uploadProgressConfirmedBytes = confirmed,
                        uploadProgressTotalBytes = fileSize
                    ))
                }
            }
        }

        // 3. Finalize
        ledgerStore.markFinalizing(recording.id)
        withContext(Dispatchers.Main) {
            onStatusUpdate(recording.copy(
                uploadProgressDescription = "确认提交中...",
                uploadProgressFraction = 0.98
            ))
        }

        val finalizeRequest = ResumableAudioUploadFinalizeRequest(
            recordingID = recording.id,
            sessionID = sessionID,
            totalBytes = fileSize,
            totalSHA256 = totalSHA256
        )

        val finalizeResult = uploadClient.finalizeResumableUploadSession(settings, finalizeRequest)
        if (finalizeResult.isFailure) throw finalizeResult.exceptionOrNull()!!

        val finalResponse = finalizeResult.getOrThrow()
        if (!finalResponse.ok) {
            throw RuntimeException(finalResponse.error ?: "finalize_failed")
        }

        withContext(Dispatchers.Main) {
            onStatusUpdate(recording.copy(
                uploadStatus = RecordingUploadStatus.UPLOADED.rawValue,
                uploadProgressDescription = "已上传",
                uploadProgressFraction = 1.0
            ))
        }
    }

    // ── Retry with backoff ───────────────────────────────────────────

    private suspend fun handleFailure(
        recording: RecordingMetadata,
        settings: SecureMacConnectionSnapshot,
        attempt: Int,
        onStatusUpdate: (RecordingMetadata) -> Unit,
        errorMessage: String
    ) {
        if (attempt >= maxRetries - 1) {
            ledgerStore.markFailure(
                recording.id, "max_retries_exceeded",
                errorMessage, isFatal = false
            )
            withContext(Dispatchers.Main) {
                onStatusUpdate(recording.copy(
                    uploadStatus = RecordingUploadStatus.FAILED.rawValue,
                    uploadProgressDescription = "上传失败: $errorMessage"
                ))
            }
            return
        }

        val delayMs = min(baseDelayMs * 2.0.pow(attempt).toLong(), maxDelayMs)
        delay(delayMs)
        attemptUpload(recording, settings, attempt + 1, onStatusUpdate)
    }
}
