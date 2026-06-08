package com.rokurics.app.domain.canonical

import java.util.UUID

enum class CanonicalAudioUploadCutoverMode(val rawValue: String) {
    DISABLED("disabled"),
    DIAGNOSTICS_ONLY("diagnosticsOnly"),
    NO_COMMIT("noCommit"),
    TEST_TRANSPORT_UPLOAD("testTransportUpload");

    companion object {
        val allCases: List<CanonicalAudioUploadCutoverMode> = entries.toList()
    }
}

data class CanonicalAudioUploadCutoverConfig
private constructor(
    val mode: CanonicalAudioUploadCutoverMode,
    val configID: String,
    val maxCandidates: Int,
    val diagnosticsRedacted: Boolean
) {
    companion object {
        operator fun invoke(
            mode: CanonicalAudioUploadCutoverMode = CanonicalAudioUploadCutoverMode.DISABLED,
            configID: String = UUID.randomUUID().toString(),
            maxCandidates: Int = 0,
            diagnosticsRedacted: Boolean = true
        ): CanonicalAudioUploadCutoverConfig {
            return CanonicalAudioUploadCutoverConfig(
                mode = mode,
                configID = configID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
                maxCandidates = maxOf(0, maxCandidates),
                diagnosticsRedacted = diagnosticsRedacted
            )
        }
        val DISABLED = CanonicalAudioUploadCutoverConfig()

        fun diagnosticsOnly(maxCandidates: Int = 0): CanonicalAudioUploadCutoverConfig {
            return CanonicalAudioUploadCutoverConfig(
                mode = CanonicalAudioUploadCutoverMode.DIAGNOSTICS_ONLY,
                maxCandidates = maxCandidates
            )
        }

        fun noCommit(maxCandidates: Int = 0): CanonicalAudioUploadCutoverConfig {
            return CanonicalAudioUploadCutoverConfig(
                mode = CanonicalAudioUploadCutoverMode.NO_COMMIT,
                maxCandidates = maxCandidates
            )
        }

        fun testTransportUpload(maxCandidates: Int = 0): CanonicalAudioUploadCutoverConfig {
            return CanonicalAudioUploadCutoverConfig(
                mode = CanonicalAudioUploadCutoverMode.TEST_TRANSPORT_UPLOAD,
                maxCandidates = maxCandidates
            )
        }
    }

    val isEnabled: Boolean
        get() = mode != CanonicalAudioUploadCutoverMode.DISABLED

    val allowsUpload: Boolean
        get() = mode == CanonicalAudioUploadCutoverMode.TEST_TRANSPORT_UPLOAD

    val diagnosticsSummary: String
        get() = listOf(
            "mode=${mode.rawValue}",
            "configID=$configID",
            "maxCandidates=$maxCandidates",
            "redacted=$diagnosticsRedacted"
        ).joinToString(",")
}

enum class CanonicalAudioUploadPeerState(val rawValue: String) {
    ABSENT("absent"),
    METADATA_ONLY("metadataOnly"),
    RECEIVE_RECORD_ONLY("receiveRecordOnly"),
    STUDY_ITEM_ONLY("studyItemOnly"),
    AUDIO_AVAILABLE("audioAvailable"),
    CONFLICT("conflict"),
    UNKNOWN("unknown");

    companion object {
        val allCases: List<CanonicalAudioUploadPeerState> = entries.toList()
    }
}

data class CanonicalAudioUploadCandidate
private constructor(
    val objectID: String,
    val localAudioAvailable: Boolean,
    val peerState: CanonicalAudioUploadPeerState,
    val candidateID: String
) {
    companion object {
        operator fun invoke(
            objectID: String,
            localAudioAvailable: Boolean = false,
            peerState: CanonicalAudioUploadPeerState = CanonicalAudioUploadPeerState.UNKNOWN,
            candidateID: String = UUID.randomUUID().toString()
        ): CanonicalAudioUploadCandidate {
            return CanonicalAudioUploadCandidate(
                objectID = objectID.trim().nilIfEmpty ?: "unknown-recording",
                localAudioAvailable = localAudioAvailable,
                peerState = peerState,
                candidateID = candidateID.trim().nilIfEmpty ?: UUID.randomUUID().toString()
            )
        }
    }

    val id: String get() = candidateID

    val wouldUpload: Boolean
        get() = localAudioAvailable &&
                peerState != CanonicalAudioUploadPeerState.AUDIO_AVAILABLE &&
                peerState != CanonicalAudioUploadPeerState.CONFLICT &&
                peerState != CanonicalAudioUploadPeerState.UNKNOWN

    val isConflict: Boolean
        get() = peerState == CanonicalAudioUploadPeerState.CONFLICT

    val diagnosticsSummary: String
        get() = listOf(
            "objectID=$objectID",
            "localAudio=$localAudioAvailable",
            "peerState=${peerState.rawValue}",
            "wouldUpload=$wouldUpload"
        ).joinToString(",")
}

data class CanonicalAudioUploadNoCommitResult
private constructor(
    val candidate: CanonicalAudioUploadCandidate,
    val wouldUpload: Boolean,
    val suppressed: Boolean,
    val reason: String?
) {
    companion object {
        operator fun invoke(
            candidate: CanonicalAudioUploadCandidate,
            wouldUpload: Boolean = false,
            suppressed: Boolean = true,
            reason: String? = null
        ): CanonicalAudioUploadNoCommitResult {
            return CanonicalAudioUploadNoCommitResult(
                candidate = candidate,
                wouldUpload = wouldUpload,
                suppressed = suppressed,
                reason = reason?.trim()?.nilIfEmpty
            )
        }

        fun fromCandidate(
            candidate: CanonicalAudioUploadCandidate,
            suppressed: Boolean = true
        ): CanonicalAudioUploadNoCommitResult {
            val wouldUpload = candidate.wouldUpload
            val reason = when {
                !candidate.localAudioAvailable -> "localAudioNotAvailable"
                candidate.peerState == CanonicalAudioUploadPeerState.AUDIO_AVAILABLE -> "peerAudioAlreadyAvailable"
                candidate.peerState == CanonicalAudioUploadPeerState.CONFLICT -> "peerInConflict"
                candidate.peerState == CanonicalAudioUploadPeerState.UNKNOWN -> "peerStateUnknown"
                wouldUpload -> "uploadCandidateReady"
                else -> "noUploadRequired"
            }
            return CanonicalAudioUploadNoCommitResult(
                candidate = candidate,
                wouldUpload = wouldUpload,
                suppressed = suppressed,
                reason = reason
            )
        }
    }

    val diagnosticsSummary: String
        get() = listOf(
            "objectID=${candidate.objectID}",
            "wouldUpload=$wouldUpload",
            "suppressed=$suppressed",
            "reason=${reason ?: "none"}"
        ).joinToString(",")
}

class CanonicalAudioUploadNoCommitExecutor(
    private val config: CanonicalAudioUploadCutoverConfig,
    private val abortPolicy: CanonicalAudioUploadAbortPolicy = CanonicalAudioUploadAbortPolicy()
) {
    data class ExecutionResult(
        val results: List<CanonicalAudioUploadNoCommitResult>,
        val candidatesCount: Int,
        val wouldUploadCount: Int,
        val suppressedCount: Int,
        val noRealUploadJobsCreated: Boolean,
        val diagnosticsSummary: String
    )

    fun execute(
        candidates: List<CanonicalAudioUploadCandidate>
    ): ExecutionResult {
        if (config.mode == CanonicalAudioUploadCutoverMode.DISABLED) {
            return ExecutionResult(
                results = emptyList(),
                candidatesCount = 0,
                wouldUploadCount = 0,
                suppressedCount = 0,
                noRealUploadJobsCreated = true,
                diagnosticsSummary = listOf(
                    "mode=disabled",
                    "candidates=0",
                    "noUploadJobs=true"
                ).joinToString(",")
            )
        }

        val limited = if (config.maxCandidates > 0)
            candidates.take(config.maxCandidates)
        else
            candidates

        val evaluated = limited.map { candidate ->
            if (candidate.isConflict && abortPolicy.abortOnConflict) {
                CanonicalAudioUploadNoCommitResult(
                    candidate = candidate,
                    wouldUpload = false,
                    suppressed = true,
                    reason = "abortOnConflict"
                )
            } else {
                CanonicalAudioUploadNoCommitResult.fromCandidate(candidate)
            }
        }

        return ExecutionResult(
            results = evaluated,
            candidatesCount = evaluated.size,
            wouldUploadCount = evaluated.count { it.wouldUpload },
            suppressedCount = evaluated.count { it.suppressed },
            noRealUploadJobsCreated = true,
            diagnosticsSummary = listOf(
                "mode=${config.mode.rawValue}",
                "candidates=${evaluated.size}",
                "wouldUpload=${evaluated.count { it.wouldUpload }}",
                "suppressed=${evaluated.count { it.suppressed }}",
                "noUploadJobs=true"
            ).joinToString(",")
        )
    }
}

class CanonicalAudioUploadShadowReceiver(
    private val executor: CanonicalAudioUploadNoCommitExecutor
) {
    data class RehearseResult(
        val received: Boolean,
        val candidate: CanonicalAudioUploadCandidate?,
        val noCommitResult: CanonicalAudioUploadNoCommitResult?,
        val diagnosticsSummary: String
    ) {
        companion object {
            fun notReceived(): RehearseResult {
                return RehearseResult(
                    received = false,
                    candidate = null,
                    noCommitResult = null,
                    diagnosticsSummary = "received=false"
                )
            }
        }
    }

    fun rehearseReceive(
        candidate: CanonicalAudioUploadCandidate?
    ): RehearseResult {
        if (candidate == null) return RehearseResult.notReceived()

        val result = executor.execute(listOf(candidate))
        val noCommitResult = result.results.firstOrNull()

        return RehearseResult(
            received = true,
            candidate = candidate,
            noCommitResult = noCommitResult,
            diagnosticsSummary = listOf(
                "received=true",
                "objectID=${candidate.objectID}",
                "wouldUpload=${noCommitResult?.wouldUpload ?: false}",
                "suppressed=${noCommitResult?.suppressed ?: false}"
            ).joinToString(",")
        )
    }
}

data class CanonicalAudioUploadAbortPolicy
private constructor(
    val abortOnConflict: Boolean,
    val abortOnHashMismatch: Boolean
) {
    companion object {
        operator fun invoke(
            abortOnConflict: Boolean = true,
            abortOnHashMismatch: Boolean = true
        ): CanonicalAudioUploadAbortPolicy {
            return CanonicalAudioUploadAbortPolicy(
                abortOnConflict = abortOnConflict,
                abortOnHashMismatch = abortOnHashMismatch
            )
        }

        val DEFAULT = CanonicalAudioUploadAbortPolicy()

        val PERMISSIVE = CanonicalAudioUploadAbortPolicy(
            abortOnConflict = false,
            abortOnHashMismatch = false
        )
    }

    val diagnosticsSummary: String
        get() = listOf(
            "abortOnConflict=$abortOnConflict",
            "abortOnHashMismatch=$abortOnHashMismatch"
        ).joinToString(",")
}

data class CanonicalAudioUploadRollbackResult
private constructor(
    val rollbackID: String,
    val objectID: String,
    val uploadSuppressed: Boolean,
    val noRealUploadJobsCancelled: Boolean,
    val stagingEvidenceCleared: Boolean,
    val diagnosticsSummary: String
) {
    companion object {
        operator fun invoke(
            rollbackID: String = UUID.randomUUID().toString(),
            objectID: String,
            uploadSuppressed: Boolean = true,
            noRealUploadJobsCancelled: Boolean = true,
            stagingEvidenceCleared: Boolean = true
        ): CanonicalAudioUploadRollbackResult {
            return CanonicalAudioUploadRollbackResult(
                rollbackID = rollbackID.trim().nilIfEmpty ?: UUID.randomUUID().toString(),
                objectID = objectID.trim().nilIfEmpty ?: "unknown-recording",
                uploadSuppressed = uploadSuppressed,
                noRealUploadJobsCancelled = noRealUploadJobsCancelled,
                stagingEvidenceCleared = stagingEvidenceCleared,
                diagnosticsSummary = listOf(
                    "rollbackID=$rollbackID",
                    "objectID=$objectID",
                    "suppressed=$uploadSuppressed",
                    "noJobsCancelled=$noRealUploadJobsCancelled",
                    "stagingCleared=$stagingEvidenceCleared"
                ).joinToString(",")
            )
        }

        fun success(objectID: String): CanonicalAudioUploadRollbackResult {
            return CanonicalAudioUploadRollbackResult(objectID = objectID)
        }
    }
}

data class CanonicalAudioUploadReadProjection
private constructor(
    val uploadCandidates: List<CanonicalAudioUploadCandidate>,
    val completed: List<String>,
    val failed: List<String>
) {
    companion object {
        operator fun invoke(
            uploadCandidates: List<CanonicalAudioUploadCandidate> = emptyList(),
            completed: List<String> = emptyList(),
            failed: List<String> = emptyList()
        ): CanonicalAudioUploadReadProjection {
            return CanonicalAudioUploadReadProjection(
                uploadCandidates = uploadCandidates.distinctBy { it.objectID },
                completed = completed.mapNotNull { it.trim().nilIfEmpty }.sorted(),
                failed = failed.mapNotNull { it.trim().nilIfEmpty }.sorted()
            )
        }
    }

    val candidateCount: Int get() = uploadCandidates.size
    val completedCount: Int get() = completed.size
    val failedCount: Int get() = failed.size

    val diagnosticsSummary: String
        get() = listOf(
            "candidates=$candidateCount",
            "completed=$completedCount",
            "failed=$failedCount"
        ).joinToString(",")

    fun candidateFor(objectID: String): CanonicalAudioUploadCandidate? {
        return uploadCandidates.firstOrNull { it.objectID == objectID }
    }
}

data class CanonicalAudioUploadDiagnosticsSummary
private constructor(
    val config: CanonicalAudioUploadCutoverConfig,
    val candidatesCount: Int,
    val wouldUploadCount: Int,
    val suppressedCount: Int,
    val conflictCount: Int,
    val noRealUploadJobsCreated: Boolean,
    val completedUploadCount: Int,
    val failedUploadCount: Int,
    val abortPolicy: CanonicalAudioUploadAbortPolicy?
) {
    companion object {
        operator fun invoke(
            config: CanonicalAudioUploadCutoverConfig = CanonicalAudioUploadCutoverConfig.DISABLED,
            candidatesCount: Int = 0,
            wouldUploadCount: Int = 0,
            suppressedCount: Int = 0,
            conflictCount: Int = 0,
            noRealUploadJobsCreated: Boolean = true,
            completedUploadCount: Int = 0,
            failedUploadCount: Int = 0,
            abortPolicy: CanonicalAudioUploadAbortPolicy? = null
        ): CanonicalAudioUploadDiagnosticsSummary {
            return CanonicalAudioUploadDiagnosticsSummary(
                config = config,
                candidatesCount = maxOf(0, candidatesCount),
                wouldUploadCount = maxOf(0, wouldUploadCount),
                suppressedCount = maxOf(0, suppressedCount),
                conflictCount = maxOf(0, conflictCount),
                noRealUploadJobsCreated = noRealUploadJobsCreated,
                completedUploadCount = maxOf(0, completedUploadCount),
                failedUploadCount = maxOf(0, failedUploadCount),
                abortPolicy = abortPolicy
            )
        }
    }

    val diagnosticsSummary: String
        get() = listOf(
            "mode=${config.mode.rawValue}",
            "candidates=$candidatesCount",
            "wouldUpload=$wouldUploadCount",
            "suppressed=$suppressedCount",
            "conflict=$conflictCount",
            "noRealJobs=$noRealUploadJobsCreated",
            "completed=$completedUploadCount",
            "failed=$failedUploadCount",
            "policy=${abortPolicy?.diagnosticsSummary ?: "none"}"
        ).joinToString(",")
}
