package com.rokurics.app.data

import android.content.Context
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.reflect.TypeToken
import com.rokurics.app.RokuricsApp
import com.rokurics.app.domain.model.*
import java.io.File
import java.util.UUID

data class UploadJobLedger(
    val version: Int = CURRENT_VERSION,
    val jobs: List<RecordingUploadJob> = emptyList()
) {
    companion object {
        const val CURRENT_VERSION = 2
    }

    fun deduplicatedJobs(): List<RecordingUploadJob> {
        val byID = linkedMapOf<String, RecordingUploadJob>()
        for (job in jobs) {
            val existing = byID[job.recordingID]
            if (existing == null || job.updatedAt >= existing.updatedAt) {
                byID[job.recordingID] = job
            }
        }
        return byID.values.toList()
    }
}

class UploadJobLedgerStore(
    private val context: Context = RokuricsApp.instance
) {
    private val gson: Gson = GsonBuilder()
        .setPrettyPrinting()
        .disableHtmlEscaping()
        .create()

    private val ledgerDir: File
        get() = File(context.filesDir, "Rokurics/UploadJobs").also { it.mkdirs() }

    private val ledgerFile: File
        get() = File(ledgerDir, "upload-ledger.json")

    // ── Load / Save ──────────────────────────────────────────────

    fun loadLedger(): UploadJobLedger {
        if (!ledgerFile.exists()) return UploadJobLedger()
        return try {
            val json = ledgerFile.readText()
            val type = object : TypeToken<UploadJobLedger>() {}.type
            val ledger: UploadJobLedger = gson.fromJson(json, type)
            ledger.copy(jobs = ledger.deduplicatedJobs())
        } catch (_: Exception) {
            UploadJobLedger()
        }
    }

    fun saveLedger(ledger: UploadJobLedger) {
        val normalized = ledger.copy(
            version = UploadJobLedger.CURRENT_VERSION,
            jobs = ledger.deduplicatedJobs()
        )
        val tmpFile = File(ledgerDir, ".upload-ledger-${UUID.randomUUID()}.tmp")
        try {
            tmpFile.writeText(gson.toJson(normalized))
            if (ledgerFile.exists()) {
                ledgerFile.delete()
            }
            tmpFile.renameTo(ledgerFile)
        } catch (_: Exception) {
            tmpFile.delete()
        } finally {
            if (tmpFile.exists()) tmpFile.delete()
        }
    }

    // ── Job CRUD ─────────────────────────────────────────────────

    fun loadJobs(): List<RecordingUploadJob> = loadLedger().jobs

    fun loadJob(recordingID: String): RecordingUploadJob? =
        loadJobs().find { it.recordingID == recordingID }

    fun saveJob(job: RecordingUploadJob) {
        val ledger = loadLedger()
        val others = ledger.jobs.filter { it.recordingID != job.recordingID }
        val updated = UploadJobLedger(
            version = ledger.version,
            jobs = others + job.copy(updatedAt = System.currentTimeMillis())
        )
        saveLedger(updated)
    }

    fun ensureJob(
        recording: com.rokurics.app.domain.model.RecordingMetadata,
        settings: com.rokurics.app.domain.model.SecureMacConnectionSnapshot,
        audioPath: String
    ): RecordingUploadJob {
        val existing = loadJob(recording.id)
        if (existing != null) return existing
        val newJob = RecordingUploadJob(
            recordingID = recording.id,
            localAudioPath = audioPath,
            targetDeviceID = settings.deviceID,
            targetMacName = settings.macName
        )
        saveJob(newJob)
        return newJob
    }

    fun markAttemptStarted(recordingID: String): RecordingUploadJob? {
        val job = loadJob(recordingID) ?: return null
        val now = System.currentTimeMillis()
        val updated = job.copy(
            attemptCount = job.attemptCount + 1,
            lastAttemptAt = now,
            lastErrorCode = null,
            lastErrorMessage = null,
            overallState = RecordingUploadJobOverallState.IN_PROGRESS,
            updatedAt = now
        )
        saveJob(updated)
        return updated
    }

    // ── Progress Updates ─────────────────────────────────────────

    fun updateChunkProgress(
        recordingID: String,
        confirmedBytes: Long,
        totalBytes: Long,
        progressFraction: Double
    ) {
        val job = loadJob(recordingID) ?: return
        val now = System.currentTimeMillis()
        val updated = job.copy(
            audioConfirmedBytes = confirmedBytes,
            audioNextOffset = confirmedBytes,
            currentProgressFraction = progressFraction,
            lastProgressAt = now,
            lastConfirmedByMacAt = now,
            updatedAt = now
        )
        saveJob(updated)
    }

    fun markSessionStarted(
        recordingID: String,
        sessionID: String,
        totalBytes: Long,
        chunkSize: Int,
        totalSHA256: String
    ) {
        val job = loadJob(recordingID) ?: return
        val chunkCount = if (chunkSize > 0) ((totalBytes + chunkSize - 1) / chunkSize).toInt() else 0
        val now = System.currentTimeMillis()
        val updated = job.copy(
            resumableSessionID = sessionID,
            audioTotalBytes = totalBytes,
            audioChunkSize = chunkSize,
            audioTotalSHA256 = totalSHA256,
            audioConfirmedBytes = 0,
            audioChunkCount = chunkCount,
            audioCompletedChunkCount = 0,
            currentProgressFraction = 0.0,
            lastProgressAt = now,
            lastConfirmedByMacAt = now,
            resumableState = RecordingResumableUploadState.UPLOADING,
            updatedAt = now
        )
        saveJob(updated)
    }

    fun markChunkCompleted(
        recordingID: String,
        confirmedBytes: Long,
        completedChunkCount: Int,
        progressFraction: Double
    ) {
        val job = loadJob(recordingID) ?: return
        val now = System.currentTimeMillis()
        val updated = job.copy(
            audioConfirmedBytes = confirmedBytes,
            audioNextOffset = confirmedBytes,
            audioCompletedChunkCount = completedChunkCount,
            currentProgressFraction = progressFraction,
            lastProgressAt = now,
            lastConfirmedByMacAt = now,
            updatedAt = now
        )
        saveJob(updated)
    }

    fun markFinalizing(recordingID: String) {
        val job = loadJob(recordingID) ?: return
        val updated = job.copy(
            resumableState = RecordingResumableUploadState.FINALIZING,
            updatedAt = System.currentTimeMillis()
        )
        saveJob(updated)
    }

    // ── Completion / Failure ─────────────────────────────────────

    fun markSucceeded(recordingID: String) {
        val job = loadJob(recordingID) ?: return
        val now = System.currentTimeMillis()
        val updated = job.copy(
            metadataStage = RecordingUploadJobStageState.SUCCEEDED,
            audioStage = RecordingUploadJobStageState.SUCCEEDED,
            overallState = RecordingUploadJobOverallState.SUCCEEDED,
            currentProgressFraction = 1.0,
            resumableState = RecordingResumableUploadState.COMPLETED,
            lastProgressAt = now,
            updatedAt = now
        )
        saveJob(updated)
    }

    fun markFailure(
        recordingID: String,
        errorCode: String?,
        errorMessage: String?,
        isFatal: Boolean
    ) {
        val job = loadJob(recordingID) ?: return
        val now = System.currentTimeMillis()
        val overallState = if (isFatal)
            RecordingUploadJobOverallState.FATAL_FAILED
        else
            RecordingUploadJobOverallState.RETRYABLE_FAILED

        // Calculate retry delay: 5s, 30s, 120s for attempts 1/2/3, then exponential capped at 600s
        val retryDelays = longArrayOf(5_000, 30_000, 120_000)
        val retryAfter = if (!isFatal) {
            val extraAttempts = job.attemptCount - 1
            if (extraAttempts < retryDelays.size) {
                now + retryDelays[extraAttempts.coerceAtLeast(0)]
            } else {
                val baseDelay = 120_000L
                val expDelay = baseDelay * (1L shl (extraAttempts - retryDelays.size + 1))
                now + expDelay.coerceAtMost(600_000L)
            }
        } else null

        val updated = job.copy(
            overallState = overallState,
            isFatal = isFatal,
            lastErrorCode = errorCode,
            lastErrorMessage = errorMessage,
            lastAttemptAt = now,
            nextRetryAfter = retryAfter,
            updatedAt = now
        )
        saveJob(updated)
    }

    // ── Recovery ─────────────────────────────────────────────────

    fun recoverStaleInProgressJobs(): List<RecordingUploadJob> {
        val now = System.currentTimeMillis()
        val ledger = loadLedger()
        val recovered = mutableListOf<RecordingUploadJob>()

        for (job in ledger.jobs) {
            val needsRecovery = job.overallState == RecordingUploadJobOverallState.IN_PROGRESS ||
                    job.metadataStage == RecordingUploadJobStageState.IN_PROGRESS ||
                    job.audioStage == RecordingUploadJobStageState.IN_PROGRESS

            if (needsRecovery) {
                var updated = job.copy(updatedAt = now)

                if (updated.metadataStage == RecordingUploadJobStageState.IN_PROGRESS) {
                    updated = updated.copy(metadataStage = RecordingUploadJobStageState.FAILED)
                }
                if (updated.audioStage == RecordingUploadJobStageState.IN_PROGRESS) {
                    updated = updated.copy(audioStage = RecordingUploadJobStageState.FAILED)
                }
                if (updated.uploadMode == RecordingUploadMode.RESUMABLE_CHUNKS &&
                    updated.resumableState in listOf(
                        RecordingResumableUploadState.STARTING,
                        RecordingResumableUploadState.UPLOADING,
                        RecordingResumableUploadState.FINALIZING
                    )
                ) {
                    updated = updated.copy(resumableState = RecordingResumableUploadState.PAUSED)
                }

                updated = updated.copy(
                    overallState = RecordingUploadJobOverallState.RETRYABLE_FAILED,
                    isFatal = false,
                    lastErrorCode = "upload_interrupted",
                    lastErrorMessage = "上次上传中断，可重试。",
                    nextRetryAfter = now
                )
                recovered.add(updated)
            }
        }

        if (recovered.isNotEmpty()) {
            val others = ledger.jobs.filter { j ->
                recovered.none { it.recordingID == j.recordingID }
            }
            saveLedger(UploadJobLedger(version = ledger.version, jobs = others + recovered))
        }

        return recovered
    }

    fun getRetryableJobs(): List<RecordingUploadJob> {
        val now = System.currentTimeMillis()
        return loadJobs().filter { job ->
            job.overallState == RecordingUploadJobOverallState.RETRYABLE_FAILED &&
                    !job.isFatal &&
                    (job.nextRetryAfter == null || (job.nextRetryAfter ?: 0L) <= now)
        }.sortedBy { it.nextRetryAfter ?: 0L }
    }

    // ── Propagation to RecordingMetadata ─────────────────────────

    fun propagateToMetadata(
        jobs: List<RecordingUploadJob>,
        recordingManager: com.rokurics.app.service.RecordingManager
    ) {
        for (job in jobs) {
            when (job.overallState) {
                RecordingUploadJobOverallState.RETRYABLE_FAILED,
                RecordingUploadJobOverallState.FATAL_FAILED -> {
                    val recordings = recordingManager.recordings.value
                    val rec = recordings.find { it.id == job.recordingID } ?: continue
                    if (rec.uploadStatus != RecordingUploadStatus.UPLOADED.rawValue) {
                        recordingManager.updateUploadProgress(
                            recordingID = job.recordingID,
                            uploadStatus = RecordingUploadStatus.FAILED.rawValue,
                            progressFraction = job.currentProgressFraction,
                            confirmedBytes = job.audioConfirmedBytes,
                            totalBytes = job.audioTotalBytes,
                            description = job.lastErrorMessage ?: "上传中断"
                        )
                    }
                }
                RecordingUploadJobOverallState.SUCCEEDED -> {
                    recordingManager.updateUploadProgress(
                        recordingID = job.recordingID,
                        uploadStatus = RecordingUploadStatus.UPLOADED.rawValue,
                        progressFraction = 1.0,
                        confirmedBytes = job.audioTotalBytes,
                        totalBytes = job.audioTotalBytes,
                        description = "已上传"
                    )
                }
                else -> {}
            }
        }
    }
}
