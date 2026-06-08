package com.rokurics.app.domain.canonical

enum class CanonicalAudioUploadRuntimeMode {
    disabled,
    diagnosticsOnly,
    noCommit,
    testTransportUpload,
    canonicalUploadWithLegacyFallback,
    blocked
}

data class CanonicalAudioUploadRuntimePolicy(
    val debugInternalBuild: Boolean = false,
    val ownerApproved: Boolean = false,
    val releaseDefaultBuild: Boolean = false,
    val diagnosticsRedacted: Boolean = true,
    val legacyFallbackAvailable: Boolean = false,
    val existingSecureUploadPort: Boolean = false
)

data class CanonicalAudioUploadRuntimeConfiguration(
    val mode: CanonicalAudioUploadRuntimeMode,
    val policy: CanonicalAudioUploadRuntimePolicy
) {
    companion object {
        val DISABLED = CanonicalAudioUploadRuntimeConfiguration(
            mode = CanonicalAudioUploadRuntimeMode.disabled,
            policy = CanonicalAudioUploadRuntimePolicy()
        )
    }
}

enum class CanonicalAudioUploadRuntimeResultKind {
    success,
    skippedDisabled,
    skippedDiagnostics,
    fallbackUsed,
    blocked,
    failed,
    noCommit
}

data class CanonicalAudioUploadRuntimeResult(
    val mode: CanonicalAudioUploadRuntimeMode,
    val resultKind: CanonicalAudioUploadRuntimeResultKind,
    val session: CanonicalAudioUploadSession? = null,
    val fallbackUsed: Boolean = false
)

enum class CanonicalAudioUploadSessionState {
    idle,
    starting,
    started,
    chunking,
    interrupted,
    resuming,
    finalizing,
    finalized,
    failed,
    aborted,
    conflict,
    blocked
}

data class CanonicalAudioUploadSession(
    val sessionID: String,
    val objectID: String,
    val state: CanonicalAudioUploadSessionState,
    val confirmedBytes: Long = 0,
    val totalBytes: Long = 0,
    val contentHash: CanonicalHash? = null,
    val chunkSize: Int = 65536
) {
    val id: String get() = sessionID
}

data class CanonicalAudioUploadChunk(
    val offset: Long,
    val length: Int,
    val hashPrefix: String? = null
)

data class CanonicalAudioUploadOffset(
    val confirmed: Long = 0,
    val next: Long = 0
)

data class CanonicalAudioUploadFinalizeProof(
    val totalBytes: Long,
    val contentHash: CanonicalHash
)

data class CanonicalAudioUploadAbort(
    val sessionID: String,
    val reason: String
) {
    val id: String get() = sessionID
}

data class CanonicalAudioUploadRetryRecord(
    val sessionID: String,
    val attemptIndex: Int,
    val offsetBeforeAttempt: CanonicalAudioUploadOffset,
    val failedAt: CanonicalTimestamp,
    val reason: String? = null,
    val recovered: Boolean = false
)

data class CanonicalAudioUploadRetryPolicy(
    val maxAttempts: Int = 5,
    val baseDelayMs: Long = 1000L,
    val maxDelayMs: Long = 60000L,
    val backoffMultiplier: Double = 2.0
) {
    fun delayForAttempt(attempt: Int): Long {
        val delay = baseDelayMs * Math.pow(backoffMultiplier, (attempt - 1).toDouble()).toLong()
        return delay.coerceAtMost(maxDelayMs)
    }
}

data class CanonicalAudioUploadResumeToken(
    val sessionID: String,
    val confirmedOffset: Long,
    val contentHash: CanonicalHash? = null,
    val createdAt: CanonicalTimestamp? = null
)

interface CanonicalAudioUploadByteSource {
    fun getByteSize(): Long
    fun readChunk(offset: Long, maxLength: Int): ByteArray?
}

class CanonicalAudioUploadJobStore {
    private val sessions = mutableMapOf<String, CanonicalAudioUploadSession>()
    private val retryRecords = mutableMapOf<String, MutableList<CanonicalAudioUploadRetryRecord>>()
    private val resumeTokens = mutableMapOf<String, CanonicalAudioUploadResumeToken>()

    fun putSession(session: CanonicalAudioUploadSession) {
        sessions[session.sessionID] = session
    }

    fun getSession(sessionID: String): CanonicalAudioUploadSession? = sessions[sessionID]

    fun removeSession(sessionID: String) {
        sessions.remove(sessionID)
    }

    fun recordRetry(record: CanonicalAudioUploadRetryRecord) {
        retryRecords.getOrPut(record.sessionID) { mutableListOf() }.add(record)
    }

    fun getRetryRecords(sessionID: String): List<CanonicalAudioUploadRetryRecord> =
        retryRecords[sessionID]?.toList() ?: emptyList()

    fun putResumeToken(token: CanonicalAudioUploadResumeToken) {
        resumeTokens[token.sessionID] = token
    }

    fun getResumeToken(sessionID: String): CanonicalAudioUploadResumeToken? = resumeTokens[sessionID]

    fun clearSession(sessionID: String) {
        sessions.remove(sessionID)
        retryRecords.remove(sessionID)
        resumeTokens.remove(sessionID)
    }
}

class CanonicalAudioUploadRuntimeExecutor(
    private val store: CanonicalAudioUploadJobStore,
    private val byteSource: CanonicalAudioUploadByteSource,
    private val retryPolicy: CanonicalAudioUploadRetryPolicy = CanonicalAudioUploadRetryPolicy()
) {
    private val appliedRetryPolicy = retryPolicy

    fun execute(
        configuration: CanonicalAudioUploadRuntimeConfiguration,
        sessionID: String,
        objectID: String
    ): CanonicalAudioUploadRuntimeResult {
        val mode = configuration.mode

        when (mode) {
            CanonicalAudioUploadRuntimeMode.disabled -> {
                return CanonicalAudioUploadRuntimeResult(
                    mode = mode,
                    resultKind = CanonicalAudioUploadRuntimeResultKind.skippedDisabled
                )
            }
            CanonicalAudioUploadRuntimeMode.diagnosticsOnly -> {
                return CanonicalAudioUploadRuntimeResult(
                    mode = mode,
                    resultKind = CanonicalAudioUploadRuntimeResultKind.skippedDiagnostics
                )
            }
            CanonicalAudioUploadRuntimeMode.noCommit -> {
                return CanonicalAudioUploadRuntimeResult(
                    mode = mode,
                    resultKind = CanonicalAudioUploadRuntimeResultKind.noCommit
                )
            }
            CanonicalAudioUploadRuntimeMode.blocked -> {
                return CanonicalAudioUploadRuntimeResult(
                    mode = mode,
                    resultKind = CanonicalAudioUploadRuntimeResultKind.blocked
                )
            }
            CanonicalAudioUploadRuntimeMode.testTransportUpload,
            CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback -> {
                return executeUploadSession(mode, configuration.policy, sessionID, objectID)
            }
        }
    }

    private fun executeUploadSession(
        mode: CanonicalAudioUploadRuntimeMode,
        policy: CanonicalAudioUploadRuntimePolicy,
        sessionID: String,
        objectID: String
    ): CanonicalAudioUploadRuntimeResult {
        val totalBytes = byteSource.getByteSize()

        val existingToken = store.getResumeToken(sessionID)
        val startOffset = existingToken?.confirmedOffset ?: 0L

        val session = CanonicalAudioUploadSession(
            sessionID = sessionID,
            objectID = objectID,
            state = CanonicalAudioUploadSessionState.starting,
            totalBytes = totalBytes
        )
        store.putSession(session)

        try {
            val updatedSession = session.copy(state = CanonicalAudioUploadSessionState.started)
            store.putSession(updatedSession)

            var offset = startOffset
            var attemptIndex = 1

            while (offset < totalBytes && attemptIndex <= appliedRetryPolicy.maxAttempts) {
                val chunk = readNextChunk(offset, totalBytes)
                if (chunk == null) {
                    store.putSession(
                        updatedSession.copy(
                            state = CanonicalAudioUploadSessionState.failed,
                            confirmedBytes = offset
                        )
                    )
                    return CanonicalAudioUploadRuntimeResult(
                        mode = mode,
                        resultKind = CanonicalAudioUploadRuntimeResultKind.failed,
                        session = store.getSession(sessionID)
                    )
                }

                val success = simulateTransport(chunk)
                if (success) {
                    offset += chunk.length
                    store.putSession(
                        updatedSession.copy(
                            state = if (offset >= totalBytes)
                                CanonicalAudioUploadSessionState.finalizing
                            else
                                CanonicalAudioUploadSessionState.chunking,
                            confirmedBytes = offset
                        )
                    )
                    attemptIndex = 1
                } else {
                    store.recordRetry(
                        CanonicalAudioUploadRetryRecord(
                            sessionID = sessionID,
                            attemptIndex = attemptIndex,
                            offsetBeforeAttempt = CanonicalAudioUploadOffset(
                                confirmed = offset,
                                next = offset + chunk.length
                            ),
                            failedAt = CanonicalTimestamp(java.util.Date())
                        )
                    )

                    val delay = appliedRetryPolicy.delayForAttempt(attemptIndex)
                    Thread.sleep(delay)
                    attemptIndex++
                }
            }

            if (offset >= totalBytes) {
                val finalSession = session.copy(
                    state = CanonicalAudioUploadSessionState.finalized,
                    confirmedBytes = totalBytes
                )
                store.putSession(finalSession)
                return CanonicalAudioUploadRuntimeResult(
                    mode = mode,
                    resultKind = CanonicalAudioUploadRuntimeResultKind.success,
                    session = finalSession
                )
            }

            val failedSession = session.copy(
                state = CanonicalAudioUploadSessionState.failed,
                confirmedBytes = offset
            )
            store.putSession(failedSession)

            val fallbackAvailable = policy.legacyFallbackAvailable
            val usedFallback = fallbackAvailable &&
                mode == CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback

            return CanonicalAudioUploadRuntimeResult(
                mode = mode,
                resultKind = if (usedFallback)
                    CanonicalAudioUploadRuntimeResultKind.fallbackUsed
                else
                    CanonicalAudioUploadRuntimeResultKind.failed,
                session = failedSession,
                fallbackUsed = usedFallback
            )
        } catch (e: Exception) {
            val abortedSession = session.copy(
                state = CanonicalAudioUploadSessionState.failed
            )
            store.putSession(abortedSession)
            return CanonicalAudioUploadRuntimeResult(
                mode = mode,
                resultKind = CanonicalAudioUploadRuntimeResultKind.failed,
                session = abortedSession
            )
        }
    }

    private fun readNextChunk(offset: Long, totalBytes: Long): CanonicalAudioUploadChunk? {
        if (offset >= totalBytes) return null
        val remaining = totalBytes - offset
        val maxLength = remaining.coerceAtMost(65536).toInt()
        val data = byteSource.readChunk(offset, maxLength) ?: return null
        return CanonicalAudioUploadChunk(
            offset = offset,
            length = data.size,
            hashPrefix = data.take(8).joinToString("") { "%02x".format(it) }
        )
    }

    private fun simulateTransport(chunk: CanonicalAudioUploadChunk): Boolean {
        return chunk.length > 0
    }
}

class CanonicalAudioUploadCommitExecutor(
    private val store: CanonicalAudioUploadJobStore
) {
    fun commit(session: CanonicalAudioUploadSession): CanonicalAudioUploadRuntimeResult {
        if (session.state != CanonicalAudioUploadSessionState.finalized) {
            return CanonicalAudioUploadRuntimeResult(
                mode = CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback,
                resultKind = CanonicalAudioUploadRuntimeResultKind.failed,
                session = session
            )
        }
        return CanonicalAudioUploadRuntimeResult(
            mode = CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback,
            resultKind = CanonicalAudioUploadRuntimeResultKind.success,
            session = session
        )
    }
}

class CanonicalAudioUploadRuntimeOwner(
    private val executor: CanonicalAudioUploadRuntimeExecutor,
    private val store: CanonicalAudioUploadJobStore
) {
    fun run(
        configuration: CanonicalAudioUploadRuntimeConfiguration,
        sessionID: String,
        objectID: String
    ): CanonicalAudioUploadRuntimeResult {
        return executor.execute(configuration, sessionID, objectID)
    }

    fun abort(sessionID: String, reason: String): CanonicalAudioUploadAbort {
        val session = store.getSession(sessionID)
        if (session != null) {
            store.putSession(
                session.copy(state = CanonicalAudioUploadSessionState.aborted)
            )
        }
        return CanonicalAudioUploadAbort(sessionID = sessionID, reason = reason)
    }

    fun resume(sessionID: String, objectID: String): CanonicalAudioUploadRuntimeResult {
        val token = store.getResumeToken(sessionID) ?: return CanonicalAudioUploadRuntimeResult(
            mode = CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback,
            resultKind = CanonicalAudioUploadRuntimeResultKind.failed,
            session = store.getSession(sessionID)
        )
        val config = CanonicalAudioUploadRuntimeConfiguration(
            mode = CanonicalAudioUploadRuntimeMode.canonicalUploadWithLegacyFallback,
            policy = CanonicalAudioUploadRuntimePolicy()
        )
        return executor.execute(config, sessionID, objectID)
    }
}
