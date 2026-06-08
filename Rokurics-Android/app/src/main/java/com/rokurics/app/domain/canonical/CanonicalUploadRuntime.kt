package com.rokurics.app.domain.canonical

data class CanonicalUploadJob(
    val jobID: String,
    val objectID: String,
    val kind: CanonicalTransferKind,
    val phase: CanonicalTransferPhase = CanonicalTransferPhase.NONE,
    val retryCount: Int = 0
) {
    val id: String get() = jobID
}

class CanonicalUploadQueue {
    private val items = mutableListOf<CanonicalUploadJob>()
    private val completed = mutableListOf<CanonicalUploadJob>()

    fun enqueue(job: CanonicalUploadJob) {
        items.add(job.copy(phase = CanonicalTransferPhase.QUEUED))
    }

    fun dequeue(): CanonicalUploadJob? {
        if (items.isEmpty()) return null
        val job = items.removeAt(0)
        val inFlight = job.copy(phase = CanonicalTransferPhase.IN_FLIGHT)
        items.add(0, inFlight)
        return inFlight
    }

    fun status(jobID: String): CanonicalUploadJob? {
        return items.find { it.jobID == jobID }
            ?: completed.find { it.jobID == jobID }
    }

    fun markCompleted(jobID: String) {
        val index = items.indexOfFirst { it.jobID == jobID }
        if (index >= 0) {
            val job = items.removeAt(index)
            completed.add(job.copy(phase = CanonicalTransferPhase.COMPLETED))
        }
    }

    fun markFailed(jobID: String, retryable: Boolean) {
        val index = items.indexOfFirst { it.jobID == jobID }
        if (index >= 0) {
            val job = items[index]
            items[index] = job.copy(
                phase = if (retryable)
                    CanonicalTransferPhase.FAILED_RETRYABLE
                else
                    CanonicalTransferPhase.FAILED_FATAL,
                retryCount = job.retryCount + 1
            )
        }
    }

    fun allJobs(): List<CanonicalUploadJob> = items.toList()

    fun pendingCount(): Int = items.count {
        it.phase == CanonicalTransferPhase.QUEUED ||
            it.phase == CanonicalTransferPhase.IN_FLIGHT
    }

    fun completedCount(): Int = completed.size
}

object CanonicalUploadRuntime {
    private val queue = CanonicalUploadQueue()

    fun process(
        fileHandle: CanonicalFileHandle,
        uploadConfig: CanonicalAudioUploadRuntimeConfiguration,
        sessionID: String,
        objectID: String
    ): CanonicalAudioUploadRuntimeResult {
        val fileRuntime = CanonicalFileRuntime

        if (!fileRuntime.validate(fileHandle)) {
            return CanonicalAudioUploadRuntimeResult(
                mode = uploadConfig.mode,
                resultKind = CanonicalAudioUploadRuntimeResultKind.failed,
                session = CanonicalAudioUploadSession(
                    sessionID = sessionID,
                    objectID = objectID,
                    state = CanonicalAudioUploadSessionState.failed
                )
            )
        }

        val job = CanonicalUploadJob(
            jobID = sessionID,
            objectID = objectID,
            kind = CanonicalTransferKind.RECORDING_AUDIO_UPLOAD
        )
        queue.enqueue(job)

        val store = CanonicalAudioUploadJobStore()
        val byteSource = object : CanonicalAudioUploadByteSource {
            override fun getByteSize(): Long = fileHandle.byteSize

            override fun readChunk(offset: Long, maxLength: Int): ByteArray? {
                val data = fileRuntime.read(fileHandle) ?: return null
                if (offset >= data.size) return null
                val remaining = (data.size - offset).toInt()
                val length = remaining.coerceAtMost(maxLength)
                return data.copyOfRange(offset.toInt(), offset.toInt() + length)
            }
        }

        val executor = CanonicalAudioUploadRuntimeExecutor(
            store = store,
            byteSource = byteSource
        )

        val result = executor.execute(uploadConfig, sessionID, objectID)

        if (result.resultKind == CanonicalAudioUploadRuntimeResultKind.success) {
            queue.markCompleted(sessionID)
        } else {
            queue.markFailed(
                sessionID,
                result.resultKind == CanonicalAudioUploadRuntimeResultKind.fallbackUsed
            )
        }

        return result
    }

    fun queueStatus(): List<CanonicalUploadJob> = queue.allJobs()

    fun enqueue(job: CanonicalUploadJob) = queue.enqueue(job)

    fun dequeue(): CanonicalUploadJob? = queue.dequeue()
}
