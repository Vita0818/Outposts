package com.rokurics.app.domain.canonical

import java.io.File
import java.util.Date
import java.util.UUID

enum class CanonicalNoCommitEquivalenceStatus(val rawValue: String) {
    EQUIVALENT("equivalent"),
    CANONICAL_MORE_CONSERVATIVE("canonicalMoreConservative"),
    DIVERGENT("divergent"),
    INSUFFICIENT_EVIDENCE("insufficientEvidence"),
    UNSUPPORTED("unsupported");

    companion object {
        val allCases: List<CanonicalNoCommitEquivalenceStatus> = entries.toList()
    }
}

enum class CanonicalNoCommitEquivalenceDirection(val rawValue: String) {
    SEND("send"),
    RECEIVE("receive"),
    UNKNOWN("unknown");

    companion object {
        val allCases: List<CanonicalNoCommitEquivalenceDirection> = entries.toList()
    }
}

data class CanonicalNoCommitEquivalenceCandidate(
    val status: CanonicalNoCommitEquivalenceStatus,
    val metadataHashPrefix: String?,
    val canonicalDirection: CanonicalNoCommitEquivalenceDirection,
    val routePath: String,
    val blocking: Boolean
)

enum class CanonicalNoCommitCandidateFailure(val rawValue: String) {
    UNSUPPORTED_DOMAIN("unsupportedDomain"),
    UNSUPPORTED_OBJECT("unsupportedObject"),
    STAGING_ROOT_BLOCKED("stagingRootBlocked"),
    VALIDATION_FAILED("validationFailed"),
    CLEANUP_FAILED("cleanupFailed");

    companion object {
        val allCases: List<CanonicalNoCommitCandidateFailure> = entries.toList()
    }
}

data class CanonicalNoCommitCandidateStagingInfo(
    val wouldApply: Boolean,
    val wouldSend: Boolean,
    val stagingEvidence: CanonicalNoCommitStagingEvidence?,
    val cleanupEvidence: CanonicalNoCommitCleanupEvidence?
)

data class CanonicalNoCommitCandidateResult(
    val staging: CanonicalNoCommitCandidateStagingInfo?,
    val equivalence: CanonicalNoCommitEquivalenceCandidate,
    val failure: CanonicalNoCommitCandidateFailure?
)

// CanonicalProductionRedaction is defined in CanonicalProductionExecution.kt

// ── Type 1: CanonicalNoCommitSideEffectClass ──

enum class CanonicalNoCommitSideEffectClass(val rawValue: String) {
    STAGING_ONLY("stagingOnly");

    companion object {
        val allCases: List<CanonicalNoCommitSideEffectClass> = entries.toList()
    }
}

// ── Type 2: CanonicalNoCommitBlockerSeverity ──

enum class CanonicalNoCommitBlockerSeverity(val rawValue: String) {
    WARNING("warning"),
    BLOCKER("blocker");

    companion object {
        val allCases: List<CanonicalNoCommitBlockerSeverity> = entries.toList()
    }
}

// ── Type 3: CanonicalNoCommitBlocker ──

data class CanonicalNoCommitBlocker
private constructor(
    val severity: CanonicalNoCommitBlockerSeverity,
    val reason: String
) {
    val id: String = "${severity.rawValue}|$reason"

    companion object {
        operator fun invoke(
            severity: CanonicalNoCommitBlockerSeverity,
            reason: String
        ): CanonicalNoCommitBlocker {
            val sanitized = CanonicalProductionRedaction.safeDiagnosticText(reason)
                ?: severity.rawValue
            return CanonicalNoCommitBlocker(severity = severity, reason = sanitized)
        }
    }
}

// ── Type 4: CanonicalNoCommitStagingRootKind ──

enum class CanonicalNoCommitStagingRootKind(val rawValue: String) {
    SYSTEM_TEMPORARY("systemTemporary"),
    EXPLICIT_STAGING_ROOT("explicitStagingRoot"),
    REJECTED_PRODUCTION_ROOT("rejectedProductionRoot");

    companion object {
        val allCases: List<CanonicalNoCommitStagingRootKind> = entries.toList()
    }
}

// ── Type 5: CanonicalNoCommitStagingRootLifecycleStatus ──

enum class CanonicalNoCommitStagingRootLifecycleStatus(val rawValue: String) {
    NOT_CREATED("notCreated"),
    CREATED("created"),
    VALIDATION_FAILED("validationFailed");

    companion object {
        val allCases: List<CanonicalNoCommitStagingRootLifecycleStatus> = entries.toList()
    }
}

// ── Type 6: CanonicalNoCommitStagingRootCleanupStatus ──

enum class CanonicalNoCommitStagingRootCleanupStatus(val rawValue: String) {
    REMOVED("removed"),
    RETAINED_FOR_DIAGNOSTICS("retainedForDiagnostics"),
    REFUSED_PRODUCTION_ROOT("refusedProductionRoot"),
    FAILED("failed");

    companion object {
        val allCases: List<CanonicalNoCommitStagingRootCleanupStatus> = entries.toList()
    }
}

// ── Type 7: CanonicalNoCommitStagingRootCleanupPolicy ──

sealed class CanonicalNoCommitStagingRootCleanupPolicy {
    data object CleanupImmediately : CanonicalNoCommitStagingRootCleanupPolicy()

    data class RetainForDiagnostics(
        val maxAge: Double,
        val maxCount: Int,
        val maxBytes: Long
    ) : CanonicalNoCommitStagingRootCleanupPolicy()

    val policyName: String
        get() = when (this) {
            is CleanupImmediately -> "cleanupImmediately"
            is RetainForDiagnostics -> "retainForDiagnostics"
        }
}

// ── Type 8: CanonicalNoCommitStagingRoot ──

data class CanonicalNoCommitStagingRoot
private constructor(
    val rootID: String,
    val rootKind: CanonicalNoCommitStagingRootKind,
    val rootURL: String,
    val productionRootURL: String?,
    val createdAt: Date
) {
    companion object {
        operator fun invoke(
            rootID: String = UUID.randomUUID().toString(),
            rootKind: CanonicalNoCommitStagingRootKind,
            rootURL: String,
            productionRootURL: String? = null,
            createdAt: Date = Date()
        ): CanonicalNoCommitStagingRoot {
            return CanonicalNoCommitStagingRoot(
                rootID = CanonicalProductionRedaction.safeIdentifier(rootID, "no-commit-root"),
                rootKind = rootKind,
                rootURL = normalizedPath(rootURL),
                productionRootURL = productionRootURL?.let { normalizedPath(it) },
                createdAt = createdAt
            )
        }

        private fun normalizedPath(path: String): String =
            path.trim().nilIfEmpty ?: path
    }
}

// ── Type 9: CanonicalNoCommitStagingRootRetentionRecord ──

data class CanonicalNoCommitStagingRootRetentionRecord
private constructor(
    val rootID: String,
    val rootKind: CanonicalNoCommitStagingRootKind,
    val createdAt: CanonicalTimestamp,
    val retainedBytes: Long,
    val entryCount: Int
) {
    companion object {
        operator fun invoke(
            rootID: String,
            rootKind: CanonicalNoCommitStagingRootKind,
            createdAt: Date = Date(),
            retainedBytes: Long = 0,
            entryCount: Int = 0
        ): CanonicalNoCommitStagingRootRetentionRecord {
            return CanonicalNoCommitStagingRootRetentionRecord(
                rootID = CanonicalProductionRedaction.safeIdentifier(rootID, "no-commit-root"),
                rootKind = rootKind,
                createdAt = CanonicalTimestamp(createdAt),
                retainedBytes = maxOf(0, retainedBytes),
                entryCount = maxOf(0, entryCount)
            )
        }
    }
}

// ── Type 10: CanonicalNoCommitStagingRootCleanupResult ──

data class CanonicalNoCommitStagingRootCleanupResult
private constructor(
    val rootID: String,
    val rootKind: CanonicalNoCommitStagingRootKind,
    val policy: CanonicalNoCommitStagingRootCleanupPolicy,
    val status: CanonicalNoCommitStagingRootCleanupStatus,
    val removedRootCount: Int,
    val retainedRootCount: Int,
    val removedBytes: Long,
    val retainedBytes: Long,
    val fileCount: Int,
    val byteCount: Long,
    val warning: CanonicalNoCommitBlocker?
) {
    val diagnosticsSummary: String
        get() = listOf(
            "rootKind=${rootKind.rawValue}",
            "rootID=$rootID",
            "policy=${policy.policyName}",
            "cleanup=${status.rawValue}",
            "files=$fileCount",
            "bytes=$byteCount",
            "removedRoots=$removedRootCount",
            "retainedRoots=$retainedRootCount",
            "removedBytes=$removedBytes",
            "retainedBytes=$retainedBytes",
            "warning=${warning?.reason ?: "none"}"
        ).joinToString(",")

    companion object {
        operator fun invoke(
            rootID: String,
            rootKind: CanonicalNoCommitStagingRootKind,
            policy: CanonicalNoCommitStagingRootCleanupPolicy,
            status: CanonicalNoCommitStagingRootCleanupStatus,
            removedRootCount: Int = 0,
            retainedRootCount: Int = 0,
            removedBytes: Long = 0,
            retainedBytes: Long = 0,
            fileCount: Int = 0,
            byteCount: Long = 0,
            warning: CanonicalNoCommitBlocker? = null
        ): CanonicalNoCommitStagingRootCleanupResult {
            return CanonicalNoCommitStagingRootCleanupResult(
                rootID = CanonicalProductionRedaction.safeIdentifier(rootID, "no-commit-root"),
                rootKind = rootKind,
                policy = policy,
                status = status,
                removedRootCount = maxOf(0, removedRootCount),
                retainedRootCount = maxOf(0, retainedRootCount),
                removedBytes = maxOf(0, removedBytes),
                retainedBytes = maxOf(0, retainedBytes),
                fileCount = maxOf(0, fileCount),
                byteCount = maxOf(0, byteCount),
                warning = warning
            )
        }
    }
}

// ── Type 11: CanonicalNoCommitStagingEvidence ──

data class CanonicalNoCommitStagingEvidence
private constructor(
    val rootID: String,
    val rootKind: CanonicalNoCommitStagingRootKind,
    val lifecycleStatus: CanonicalNoCommitStagingRootLifecycleStatus,
    val fileCount: Int,
    val byteCount: Long,
    val wroteOnlyStagingRoot: Boolean,
    val sideEffectClass: CanonicalNoCommitSideEffectClass
) {
    val diagnosticsSummary: String
        get() = listOf(
            "rootKind=${rootKind.rawValue}",
            "rootID=$rootID",
            "lifecycle=${lifecycleStatus.rawValue}",
            "files=$fileCount",
            "bytes=$byteCount",
            "sideEffectClass=${sideEffectClass.rawValue}"
        ).joinToString(",")

    companion object {
        operator fun invoke(
            rootID: String,
            rootKind: CanonicalNoCommitStagingRootKind,
            lifecycleStatus: CanonicalNoCommitStagingRootLifecycleStatus,
            fileCount: Int = 0,
            byteCount: Long = 0,
            wroteOnlyStagingRoot: Boolean = true,
            sideEffectClass: CanonicalNoCommitSideEffectClass = CanonicalNoCommitSideEffectClass.STAGING_ONLY
        ): CanonicalNoCommitStagingEvidence {
            return CanonicalNoCommitStagingEvidence(
                rootID = CanonicalProductionRedaction.safeIdentifier(rootID, "no-commit-root"),
                rootKind = rootKind,
                lifecycleStatus = lifecycleStatus,
                fileCount = maxOf(0, fileCount),
                byteCount = maxOf(0, byteCount),
                wroteOnlyStagingRoot = wroteOnlyStagingRoot,
                sideEffectClass = sideEffectClass
            )
        }
    }
}

// ── Type 12: CanonicalNoCommitCleanupEvidence ──

data class CanonicalNoCommitCleanupEvidence(
    val rootID: String,
    val rootKind: CanonicalNoCommitStagingRootKind,
    val policy: String,
    val status: CanonicalNoCommitStagingRootCleanupStatus,
    val fileCount: Int,
    val byteCount: Long,
    val removedRootCount: Int,
    val retainedRootCount: Int,
    val warning: CanonicalNoCommitBlocker?
) {
    constructor(result: CanonicalNoCommitStagingRootCleanupResult) : this(
        rootID = result.rootID,
        rootKind = result.rootKind,
        policy = result.policy.policyName,
        status = result.status,
        fileCount = result.fileCount,
        byteCount = result.byteCount,
        removedRootCount = result.removedRootCount,
        retainedRootCount = result.retainedRootCount,
        warning = result.warning
    )

    val diagnosticsSummary: String
        get() = listOf(
            "rootKind=${rootKind.rawValue}",
            "rootID=$rootID",
            "policy=$policy",
            "cleanup=${status.rawValue}",
            "files=$fileCount",
            "bytes=$byteCount",
            "removedRoots=$removedRootCount",
            "retainedRoots=$retainedRootCount",
            "warning=${warning?.reason ?: "none"}"
        ).joinToString(",")
}

// ── Type 13: CanonicalNoCommitStagingRootLifecycle ──

class CanonicalNoCommitStagingRootLifecycle(
    val root: CanonicalNoCommitStagingRoot
) {
    fun retentionRecord(): CanonicalNoCommitStagingRootRetentionRecord {
        val stats = directoryStats(root.rootURL)
        return CanonicalNoCommitStagingRootRetentionRecord(
            rootID = root.rootID,
            rootKind = root.rootKind,
            createdAt = root.createdAt,
            retainedBytes = stats.bytes,
            entryCount = stats.files
        )
    }

    fun validateRoot(): CanonicalNoCommitBlocker? {
        val rootPath = root.rootURL
        if (rootPath.isBlank()) {
            return CanonicalNoCommitBlocker(
                CanonicalNoCommitBlockerSeverity.BLOCKER,
                "stagingRootMustBeFileURL"
            )
        }
        if (root.rootKind == CanonicalNoCommitStagingRootKind.REJECTED_PRODUCTION_ROOT) {
            return CanonicalNoCommitBlocker(
                CanonicalNoCommitBlockerSeverity.BLOCKER,
                "productionRootRefused"
            )
        }
        val productionPath = root.productionRootURL?.let { resolvedPath(it) }
        if (productionPath != null) {
            val stagingPath = resolvedPath(rootPath)
            if (stagingPath == productionPath ||
                stagingPath.startsWith(productionPath + File.separator)
            ) {
                return CanonicalNoCommitBlocker(
                    CanonicalNoCommitBlockerSeverity.BLOCKER,
                    "productionRootRefused"
                )
            }
        }
        if (root.rootKind == CanonicalNoCommitStagingRootKind.SYSTEM_TEMPORARY) {
            val stagingPath = resolvedPath(rootPath)
            val tempPath = resolvedPath(System.getProperty("java.io.tmpdir"))
            if (stagingPath != tempPath &&
                !stagingPath.startsWith(tempPath + File.separator)
            ) {
                return CanonicalNoCommitBlocker(
                    CanonicalNoCommitBlockerSeverity.BLOCKER,
                    "systemTemporaryRootRequired"
                )
            }
        }
        return null
    }

    fun stagingEvidence(
        status: CanonicalNoCommitStagingRootLifecycleStatus
    ): CanonicalNoCommitStagingEvidence {
        val stats = directoryStats(root.rootURL)
        return CanonicalNoCommitStagingEvidence(
            rootID = root.rootID,
            rootKind = root.rootKind,
            lifecycleStatus = status,
            fileCount = stats.files,
            byteCount = stats.bytes,
            wroteOnlyStagingRoot = status == CanonicalNoCommitStagingRootLifecycleStatus.CREATED
        )
    }

    fun cleanup(
        policy: CanonicalNoCommitStagingRootCleanupPolicy,
        now: Date = Date()
    ): CanonicalNoCommitStagingRootCleanupResult {
        val stats = directoryStats(root.rootURL)
        val blocker = validateRoot()
        if (blocker != null && blocker.reason == "productionRootRefused") {
            return CanonicalNoCommitStagingRootCleanupResult(
                rootID = root.rootID,
                rootKind = CanonicalNoCommitStagingRootKind.REJECTED_PRODUCTION_ROOT,
                policy = policy,
                status = CanonicalNoCommitStagingRootCleanupStatus.REFUSED_PRODUCTION_ROOT,
                retainedRootCount = if (File(root.rootURL).exists()) 1 else 0,
                retainedBytes = stats.bytes,
                fileCount = stats.files,
                byteCount = stats.bytes,
                warning = blocker
            )
        }
        return when (policy) {
            is CanonicalNoCommitStagingRootCleanupPolicy.CleanupImmediately ->
                removeCurrentRoot(policy, stats, null)
            is CanonicalNoCommitStagingRootCleanupPolicy.RetainForDiagnostics -> {
                val boundedMaxCount = maxOf(0, policy.maxCount)
                val boundedMaxBytes = maxOf(0L, policy.maxBytes)
                if (boundedMaxCount == 0 || stats.bytes > boundedMaxBytes) {
                    return removeCurrentRoot(policy, stats, "retentionBoundsExceeded")
                }
                val purge = purgeRetainedRoots(
                    parentDirectory = File(root.rootURL).parentFile?.path ?: return removeCurrentRoot(
                        policy, stats, "noParentDirectory"
                    ),
                    protectedRootPath = resolvedPath(root.rootURL),
                    maxAge = maxOf(0.0, policy.maxAge),
                    maxCount = boundedMaxCount,
                    maxBytes = boundedMaxBytes,
                    now = now
                )
                val retainedStats = directoryStats(root.rootURL)
                return CanonicalNoCommitStagingRootCleanupResult(
                    rootID = root.rootID,
                    rootKind = root.rootKind,
                    policy = policy,
                    status = CanonicalNoCommitStagingRootCleanupStatus.RETAINED_FOR_DIAGNOSTICS,
                    removedRootCount = purge.removedCount,
                    retainedRootCount = if (File(root.rootURL).exists()) 1 else 0,
                    removedBytes = purge.removedBytes,
                    retainedBytes = retainedStats.bytes,
                    fileCount = retainedStats.files,
                    byteCount = retainedStats.bytes
                )
            }
        }
    }

    private fun removeCurrentRoot(
        policy: CanonicalNoCommitStagingRootCleanupPolicy,
        stats: DirectoryStats,
        reason: String?
    ): CanonicalNoCommitStagingRootCleanupResult {
        return try {
            val rootFile = File(root.rootURL)
            if (rootFile.exists()) {
                rootFile.deleteRecursively()
            }
            CanonicalNoCommitStagingRootCleanupResult(
                rootID = root.rootID,
                rootKind = root.rootKind,
                policy = policy,
                status = CanonicalNoCommitStagingRootCleanupStatus.REMOVED,
                removedRootCount = if (stats.files > 0 || stats.bytes > 0) 1 else 0,
                removedBytes = stats.bytes,
                fileCount = stats.files,
                byteCount = stats.bytes,
                warning = reason?.let {
                    CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.WARNING, it)
                }
            )
        } catch (e: Exception) {
            CanonicalNoCommitStagingRootCleanupResult(
                rootID = root.rootID,
                rootKind = root.rootKind,
                policy = policy,
                status = CanonicalNoCommitStagingRootCleanupStatus.FAILED,
                retainedRootCount = 1,
                retainedBytes = stats.bytes,
                fileCount = stats.files,
                byteCount = stats.bytes,
                warning = CanonicalNoCommitBlocker(
                    CanonicalNoCommitBlockerSeverity.WARNING,
                    "cleanupFailed"
                )
            )
        }
    }

    private data class PurgeResult(val removedCount: Int, val removedBytes: Long)
    private data class Candidate(val path: String, val createdAt: Date, val bytes: Long)

    private fun purgeRetainedRoots(
        parentDirectory: String,
        protectedRootPath: String,
        maxAge: Double,
        maxCount: Int,
        maxBytes: Long,
        now: Date
    ): PurgeResult {
        val parentDir = File(parentDirectory)
        val children = parentDir.listFiles()?.filter { !it.isHidden }
            ?: return PurgeResult(0, 0L)

        val candidates = children
            .filter { resolvedPath(it.path) != protectedRootPath }
            .map { file ->
                Candidate(
                    path = file.path,
                    createdAt = Date(file.lastModified()),
                    bytes = directoryStats(file.path).bytes
                )
            }.toMutableList()

        var removedCount = 0
        var removedBytes = 0L

        val expired = candidates.filter { candidate ->
            val ageInSeconds = (now.time - candidate.createdAt.time) / 1000.0
            ageInSeconds > maxAge
        }
        for (candidate in expired) {
            val file = File(candidate.path)
            try {
                if (file.deleteRecursively()) {
                    removedCount++
                    removedBytes += candidate.bytes
                }
            } catch (_: Exception) {
            }
        }
        candidates.removeAll { it in expired }

        val currentBytes = directoryStats(protectedRootPath).bytes
        var totalBytes = currentBytes + candidates.sumOf { it.bytes }
        var retainedRootCount = 1 + candidates.size

        val sortedByAge = candidates.sortedBy { it.createdAt }
        for (candidate in sortedByAge) {
            if (retainedRootCount <= maxCount && totalBytes <= maxBytes) break
            val file = File(candidate.path)
            try {
                if (file.deleteRecursively()) {
                    removedCount++
                    removedBytes += candidate.bytes
                    totalBytes -= candidate.bytes
                    retainedRootCount--
                }
            } catch (_: Exception) {
            }
        }
        return PurgeResult(removedCount, removedBytes)
    }

    private data class DirectoryStats(val files: Int, val bytes: Long)

    private fun directoryStats(path: String): DirectoryStats {
        val file = File(path)
        if (!file.exists()) return DirectoryStats(0, 0L)
        if (!file.isDirectory) return DirectoryStats(1, maxOf(0L, file.length()))
        return try {
            val files = file.walkTopDown()
                .filter { !it.isHidden && it.isFile }
                .toList()
            DirectoryStats(
                files = files.size,
                bytes = files.sumOf { maxOf(0L, it.length()) }
            )
        } catch (e: Exception) {
            DirectoryStats(0, 0L)
        }
    }

    companion object {
        private fun resolvedPath(path: String): String =
            try {
                File(path).canonicalPath
            } catch (e: Exception) {
                File(path).absolutePath
            }
    }
}

// ── Type 14: CanonicalNoCommitEvidenceStatus ──

enum class CanonicalNoCommitEvidenceStatus(val rawValue: String) {
    COMPLETE("complete"),
    BLOCKED("blocked"),
    DIVERGENT("divergent"),
    INSUFFICIENT_EVIDENCE("insufficientEvidence"),
    UNSUPPORTED("unsupported"),
    WARNING("warning");

    companion object {
        val allCases: List<CanonicalNoCommitEvidenceStatus> = entries.toList()
    }
}

// ── Type 15: CanonicalNoCommitEquivalenceEvidence ──

data class CanonicalNoCommitEquivalenceEvidence(
    val equivalentCount: Int,
    val divergentCount: Int,
    val insufficientEvidenceCount: Int,
    val unsupportedCount: Int,
    val hashPrefixes: List<String>,
    val routeProjectionStatus: String,
    val legacyActionComparisonStatus: String
) {
    constructor(
        candidateResults: List<CanonicalRecordingMetadataNoCommitCandidateResult>
    ) : this(
        equivalentCount = candidateResults.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.EQUIVALENT ||
                it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.CANONICAL_MORE_CONSERVATIVE
        },
        divergentCount = candidateResults.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.DIVERGENT
        },
        insufficientEvidenceCount = candidateResults.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.INSUFFICIENT_EVIDENCE
        },
        unsupportedCount = candidateResults.count {
            it.equivalence.status == CanonicalRecordingMetadataNoCommitEquivalenceStatus.UNSUPPORTED
        },
        hashPrefixes = candidateResults
            .mapNotNull { it.equivalence.metadataHashPrefix }
            .toSet()
            .sorted(),
        routeProjectionStatus = if (candidateResults.any {
                it.equivalence.canonicalDirection == CanonicalRecordingMetadataNoCommitEquivalenceDirection.SEND &&
                    it.equivalence.routePath != "/sync/apply-metadata"
            }) "routeProjectionDivergent" else "routeProjectionSafe",
        legacyActionComparisonStatus = if (candidateResults.any {
                it.equivalence.blocking
            }) "legacyActionComparisonBlocked" else "legacyActionComparisonEquivalent"
    )
}

// ── Type 16: CanonicalNoCommitEvidenceReport ──

data class CanonicalNoCommitEvidenceReport(
    val domain: CanonicalCutoverDomain,
    val mode: CanonicalCutoverAppSeamMode,
    val status: CanonicalNoCommitEvidenceStatus,
    val candidateCount: Int,
    val wouldApplyCount: Int,
    val wouldSendCount: Int,
    val equivalentCount: Int,
    val divergentCount: Int,
    val insufficientEvidenceCount: Int,
    val unsupportedCount: Int,
    val stagingRootLifecycleStatus: String,
    val cleanupStatus: String,
    val routeProjectionStatus: String,
    val legacyActionComparisonStatus: String,
    val productionCommitSuppressed: Boolean,
    val legacyDuplicateSuppressed: Boolean,
    val sideEffectClass: CanonicalNoCommitSideEffectClass,
    val equivalenceEvidence: CanonicalNoCommitEquivalenceEvidence,
    val stagingEvidence: List<CanonicalNoCommitStagingEvidence>,
    val cleanupEvidence: List<CanonicalNoCommitCleanupEvidence>,
    val blockers: List<CanonicalNoCommitBlocker>
) {
    val diagnosticsSummary: String
        get() = listOf(
            "domain=${domain.rawValue}",
            "mode=${mode.rawValue}",
            "status=${status.rawValue}",
            "candidateCount=$candidateCount",
            "wouldApply=$wouldApplyCount",
            "wouldSend=$wouldSendCount",
            "equivalent=$equivalentCount",
            "divergent=$divergentCount",
            "insufficientEvidence=$insufficientEvidenceCount",
            "unsupported=$unsupportedCount",
            "staging=${stagingRootLifecycleStatus.ifEmpty { "none" }}",
            "cleanup=${cleanupStatus.ifEmpty { "none" }}",
            "routeProjection=$routeProjectionStatus",
            "legacyComparison=$legacyActionComparisonStatus",
            "productionCommitSuppressed=$productionCommitSuppressed",
            "legacyDuplicateSuppressed=$legacyDuplicateSuppressed",
            "sideEffectClass=${sideEffectClass.rawValue}"
        ).joinToString(",")

    companion object {
        fun from(
            gate: CanonicalCutoverAppSeamGate,
            candidateResults: List<CanonicalRecordingMetadataNoCommitCandidateResult>,
            productionCommitSuppressed: Boolean = true,
            legacyDuplicateSuppressed: Boolean = false
        ): CanonicalNoCommitEvidenceReport {
            val equivalenceEvidence = CanonicalNoCommitEquivalenceEvidence(candidateResults)
            val stagingEvidence = candidateResults.mapNotNull { it.staging?.stagingEvidence }
            val cleanupEvidence = candidateResults.mapNotNull { it.staging?.cleanupEvidence }
            val blockers = buildBlockers(gate, candidateResults, cleanupEvidence)
            val status = computeStatus(
                gate = gate,
                equivalence = equivalenceEvidence,
                blockers = blockers
            )
            return CanonicalNoCommitEvidenceReport(
                equivalenceEvidence = equivalenceEvidence,
                stagingEvidence = stagingEvidence,
                cleanupEvidence = cleanupEvidence,
                blockers = blockers,
                domain = gate.domain,
                mode = gate.mode,
                status = status,
                candidateCount = candidateResults.size,
                wouldApplyCount = candidateResults.count { it.staging?.wouldApply == true },
                wouldSendCount = candidateResults.count { it.staging?.wouldSend == true },
                equivalentCount = equivalenceEvidence.equivalentCount,
                divergentCount = equivalenceEvidence.divergentCount,
                insufficientEvidenceCount = equivalenceEvidence.insufficientEvidenceCount,
                unsupportedCount = equivalenceEvidence.unsupportedCount,
                stagingRootLifecycleStatus = stagingEvidence.map { it.lifecycleStatus.rawValue }
                    .sorted().joinToString(","),
                cleanupStatus = cleanupEvidence.map { it.status.rawValue }
                    .sorted().joinToString(","),
                routeProjectionStatus = equivalenceEvidence.routeProjectionStatus,
                legacyActionComparisonStatus = equivalenceEvidence.legacyActionComparisonStatus,
                productionCommitSuppressed = productionCommitSuppressed,
                legacyDuplicateSuppressed = legacyDuplicateSuppressed,
                sideEffectClass = CanonicalNoCommitSideEffectClass.STAGING_ONLY
            )
        }

        private fun buildBlockers(
            gate: CanonicalCutoverAppSeamGate,
            candidateResults: List<CanonicalRecordingMetadataNoCommitCandidateResult>,
            cleanupEvidence: List<CanonicalNoCommitCleanupEvidence>
        ): List<CanonicalNoCommitBlocker> {
            val blockers = mutableListOf<CanonicalNoCommitBlocker>()
            blockers.addAll(
                gate.failures.map {
                    CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.BLOCKER, it.rawValue)
                }
            )
            blockers.addAll(
                candidateResults.mapNotNull { result ->
                    result.failure?.let {
                        CanonicalNoCommitBlocker(CanonicalNoCommitBlockerSeverity.BLOCKER, it.rawValue)
                    }
                }
            )
            blockers.addAll(cleanupEvidence.mapNotNull { it.warning })
            return blockers.toSet().sortedBy { it.id }
        }

        private fun computeStatus(
            gate: CanonicalCutoverAppSeamGate,
            equivalence: CanonicalNoCommitEquivalenceEvidence,
            blockers: List<CanonicalNoCommitBlocker>
        ): CanonicalNoCommitEvidenceStatus {
            return when {
                !gate.allowed -> CanonicalNoCommitEvidenceStatus.BLOCKED
                equivalence.unsupportedCount > 0 -> CanonicalNoCommitEvidenceStatus.UNSUPPORTED
                equivalence.insufficientEvidenceCount > 0 -> CanonicalNoCommitEvidenceStatus.INSUFFICIENT_EVIDENCE
                equivalence.divergentCount > 0 -> CanonicalNoCommitEvidenceStatus.DIVERGENT
                blockers.any { it.severity == CanonicalNoCommitBlockerSeverity.WARNING } ->
                    CanonicalNoCommitEvidenceStatus.WARNING
                else -> CanonicalNoCommitEvidenceStatus.COMPLETE
            }
        }
    }
}

// ── Type 17: CanonicalMigrationStage ──

enum class CanonicalMigrationStage(val rawValue: String) {
    OFF("off"),
    NOT_STARTED("notStarted"),
    PROJECTED("projected"),
    PLANNED("planned"),
    NO_COMMIT("noCommit"),
    REAL_APPLY_PORT("realApplyPort"),
    COMMIT_EXECUTOR("commitExecutor"),
    APP_SEAM_DEFAULT_OFF("appSeamDefaultOff"),
    NEXT_PILOT_CANDIDATE("nextPilotCandidate"),
    CANARY_N0("canaryN0"),
    CANARY_N1("canaryN1"),
    EXPANDED_CANARY("expandedCanary"),
    DOMAIN_CUTOVER("domainCutover"),
    READ_SIDE_PARALLEL("readSideParallel"),
    READ_SIDE_CUTOVER("readSideCutover"),
    RETIREMENT_CANDIDATE("retirementCandidate"),
    RETIRED("retired"),
    DIAGNOSTICS_ONLY("diagnosticsOnly"),
    DECISION_SHADOW("decisionShadow"),
    EXECUTION_SHADOW("executionShadow"),
    REAL_DATA_SHADOW_COPY("realDataShadowCopy"),
    READ_ONLY_TRANSPORT_PROBE("readOnlyTransportProbe"),
    RECORDING_METADATA_NO_COMMIT("recordingMetadataNoCommit"),
    RECORDING_METADATA_GUARDED_COMMIT("recordingMetadataGuardedCommit"),
    UNSUPPORTED("unsupported");

    companion object {
        val allCases: List<CanonicalMigrationStage> = entries.toList()
    }
}

// ── Type 18: CanonicalMigrationStageSideEffect ──

enum class CanonicalMigrationStageSideEffect(val rawValue: String) {
    DIAGNOSTICS_WRITE("diagnosticsWrite"),
    SHADOW_ROOT_WRITE("shadowRootWrite"),
    READ_ONLY_NETWORK_PROBE("readOnlyNetworkProbe"),
    STAGING_ROOT_WRITE("stagingRootWrite"),
    PRODUCTION_COMMIT("productionCommit");

    companion object {
        val allCases: List<CanonicalMigrationStageSideEffect> = entries.toList()
    }
}

// ── Type 19: CanonicalMigrationStageEvidence ──

enum class CanonicalMigrationStageEvidence(val rawValue: String) {
    NONE("none"),
    DRY_RUN_EQUIVALENCE("dryRunEquivalence"),
    EXECUTION_SHADOW("executionShadow"),
    REAL_DATA_SHADOW_COPY("realDataShadowCopy"),
    READ_ONLY_TRANSPORT_PROBE("readOnlyTransportProbe"),
    NO_COMMIT_EVIDENCE_REPORT("noCommitEvidenceReport"),
    OWNER_APPROVAL("ownerApproval"),
    ROLLBACK_PLAN("rollbackPlan");

    companion object {
        val allCases: List<CanonicalMigrationStageEvidence> = entries.toList()
    }
}

// ── Type 20: CanonicalMigrationStagePolicy ──

data class CanonicalMigrationStagePolicy(
    val allowedSideEffects: List<CanonicalMigrationStageSideEffect>,
    val requiredEvidence: List<CanonicalMigrationStageEvidence>,
    val allowedDomains: List<CanonicalCutoverDomain>,
    val forbiddenDomains: List<CanonicalCutoverDomain>,
    val productionCommitAllowed: Boolean = false,
    val existingConfigurationKeys: List<String>
) {

    companion object {
        fun defaultPolicy(
            stage: CanonicalMigrationStage
        ): CanonicalMigrationStagePolicy {
            val recordingOnly = listOf(CanonicalCutoverDomain.RECORDING_METADATA)
            val allExceptRecording = CanonicalCutoverDomain.allCases
                .filter { it != CanonicalCutoverDomain.RECORDING_METADATA }

            return when (stage) {
                CanonicalMigrationStage.OFF,
                CanonicalMigrationStage.NOT_STARTED ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = emptyList(),
                        requiredEvidence = listOf(CanonicalMigrationStageEvidence.NONE),
                        allowedDomains = emptyList(),
                        forbiddenDomains = CanonicalCutoverDomain.allCases,
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalShadowMigrationConfiguration.disabled",
                            "canonicalSingleDomainShadowConfiguration.disabled",
                            "canonicalV8CutoverAppSeamConfiguration.disabled"
                        )
                    )
                CanonicalMigrationStage.DIAGNOSTICS_ONLY,
                CanonicalMigrationStage.PROJECTED,
                CanonicalMigrationStage.PLANNED,
                CanonicalMigrationStage.APP_SEAM_DEFAULT_OFF,
                CanonicalMigrationStage.NEXT_PILOT_CANDIDATE,
                CanonicalMigrationStage.CANARY_N0,
                CanonicalMigrationStage.READ_SIDE_PARALLEL,
                CanonicalMigrationStage.RETIREMENT_CANDIDATE ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE
                        ),
                        requiredEvidence = listOf(CanonicalMigrationStageEvidence.NONE),
                        allowedDomains = CanonicalCutoverDomain.allCases,
                        forbiddenDomains = emptyList(),
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalMigrationMatrix.diagnosticsOnly"
                        )
                    )
                CanonicalMigrationStage.NO_COMMIT ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE,
                            CanonicalMigrationStageSideEffect.STAGING_ROOT_WRITE
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT
                        ),
                        allowedDomains = CanonicalCutoverDomain.allCases,
                        forbiddenDomains = emptyList(),
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalMigrationMatrix.noCommit"
                        )
                    )
                CanonicalMigrationStage.REAL_APPLY_PORT,
                CanonicalMigrationStage.COMMIT_EXECUTOR ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT,
                            CanonicalMigrationStageEvidence.ROLLBACK_PLAN
                        ),
                        allowedDomains = CanonicalCutoverDomain.allCases,
                        forbiddenDomains = emptyList(),
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalMigrationMatrix.${stage.rawValue}"
                        )
                    )
                CanonicalMigrationStage.CANARY_N1,
                CanonicalMigrationStage.EXPANDED_CANARY,
                CanonicalMigrationStage.DOMAIN_CUTOVER ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE,
                            CanonicalMigrationStageSideEffect.PRODUCTION_COMMIT
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.DRY_RUN_EQUIVALENCE,
                            CanonicalMigrationStageEvidence.EXECUTION_SHADOW,
                            CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY,
                            CanonicalMigrationStageEvidence.READ_ONLY_TRANSPORT_PROBE,
                            CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT,
                            CanonicalMigrationStageEvidence.OWNER_APPROVAL,
                            CanonicalMigrationStageEvidence.ROLLBACK_PLAN
                        ),
                        allowedDomains = CanonicalCutoverDomain.allCases,
                        forbiddenDomains = emptyList(),
                        productionCommitAllowed = true,
                        existingConfigurationKeys = listOf(
                            "canonicalMigrationMatrix.${stage.rawValue}"
                        )
                    )
                CanonicalMigrationStage.READ_SIDE_CUTOVER,
                CanonicalMigrationStage.RETIRED ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.OWNER_APPROVAL,
                            CanonicalMigrationStageEvidence.ROLLBACK_PLAN
                        ),
                        allowedDomains = CanonicalCutoverDomain.allCases,
                        forbiddenDomains = emptyList(),
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalMigrationMatrix.${stage.rawValue}"
                        )
                    )
                CanonicalMigrationStage.DECISION_SHADOW ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.DRY_RUN_EQUIVALENCE
                        ),
                        allowedDomains = CanonicalCutoverDomain.allCases,
                        forbiddenDomains = emptyList(),
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalShadowMigrationConfiguration.dryRunCompare"
                        )
                    )
                CanonicalMigrationStage.EXECUTION_SHADOW ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE,
                            CanonicalMigrationStageSideEffect.SHADOW_ROOT_WRITE
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.DRY_RUN_EQUIVALENCE,
                            CanonicalMigrationStageEvidence.EXECUTION_SHADOW
                        ),
                        allowedDomains = recordingOnly,
                        forbiddenDomains = allExceptRecording,
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalSingleDomainShadowConfiguration.executionShadowDryRun"
                        )
                    )
                CanonicalMigrationStage.REAL_DATA_SHADOW_COPY ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE,
                            CanonicalMigrationStageSideEffect.SHADOW_ROOT_WRITE
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.EXECUTION_SHADOW,
                            CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY
                        ),
                        allowedDomains = recordingOnly,
                        forbiddenDomains = allExceptRecording,
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalShadowMigrationConfiguration.realDataShadowCopyPolicy"
                        )
                    )
                CanonicalMigrationStage.READ_ONLY_TRANSPORT_PROBE ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE,
                            CanonicalMigrationStageSideEffect.READ_ONLY_NETWORK_PROBE
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY,
                            CanonicalMigrationStageEvidence.READ_ONLY_TRANSPORT_PROBE
                        ),
                        allowedDomains = recordingOnly,
                        forbiddenDomains = allExceptRecording,
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalShadowMigrationConfiguration.readOnlyTransportProbePolicy",
                            "canonicalLiveReadOnlyTransportProbePolicy"
                        )
                    )
                CanonicalMigrationStage.RECORDING_METADATA_NO_COMMIT ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE,
                            CanonicalMigrationStageSideEffect.STAGING_ROOT_WRITE
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY,
                            CanonicalMigrationStageEvidence.READ_ONLY_TRANSPORT_PROBE,
                            CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT
                        ),
                        allowedDomains = recordingOnly,
                        forbiddenDomains = allExceptRecording,
                        productionCommitAllowed = false,
                        existingConfigurationKeys = listOf(
                            "canonicalV8CutoverAppSeamConfiguration.guardedExecuteNoCommit"
                        )
                    )
                CanonicalMigrationStage.RECORDING_METADATA_GUARDED_COMMIT ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = listOf(
                            CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE,
                            CanonicalMigrationStageSideEffect.PRODUCTION_COMMIT
                        ),
                        requiredEvidence = listOf(
                            CanonicalMigrationStageEvidence.DRY_RUN_EQUIVALENCE,
                            CanonicalMigrationStageEvidence.EXECUTION_SHADOW,
                            CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY,
                            CanonicalMigrationStageEvidence.READ_ONLY_TRANSPORT_PROBE,
                            CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT,
                            CanonicalMigrationStageEvidence.OWNER_APPROVAL,
                            CanonicalMigrationStageEvidence.ROLLBACK_PLAN
                        ),
                        allowedDomains = recordingOnly,
                        forbiddenDomains = allExceptRecording,
                        productionCommitAllowed = true,
                        existingConfigurationKeys = listOf(
                            "CanonicalSingleDomainCutoverConfiguration.guardedExecuteCommit"
                        )
                    )
                CanonicalMigrationStage.UNSUPPORTED ->
                    CanonicalMigrationStagePolicy(
                        allowedSideEffects = emptyList(),
                        requiredEvidence = emptyList(),
                        allowedDomains = emptyList(),
                        forbiddenDomains = CanonicalCutoverDomain.allCases,
                        productionCommitAllowed = false,
                        existingConfigurationKeys = emptyList()
                    )
            }
        }
    }
}

// ── Type 21: CanonicalMigrationConfigurationSummary ──

data class CanonicalMigrationConfigurationSummary(
    val stage: CanonicalMigrationStage,
    val domain: CanonicalCutoverDomain,
    val allowed: Boolean,
    val blockers: List<String>,
    val allowedSideEffects: List<CanonicalMigrationStageSideEffect>,
    val requiredEvidence: List<CanonicalMigrationStageEvidence>,
    val allowedDomains: List<CanonicalCutoverDomain>,
    val forbiddenDomains: List<CanonicalCutoverDomain>,
    val productionCommitAllowed: Boolean,
    val existingConfigurationKeys: List<String>
) {
    constructor(
        stage: CanonicalMigrationStage,
        domain: CanonicalCutoverDomain,
        allowed: Boolean,
        blockers: List<String>,
        policy: CanonicalMigrationStagePolicy
    ) : this(
        stage = stage,
        domain = domain,
        allowed = allowed,
        blockers = blockers
            .mapNotNull { CanonicalProductionRedaction.safeDiagnosticText(it) }
            .toSet()
            .sorted(),
        allowedSideEffects = policy.allowedSideEffects,
        requiredEvidence = policy.requiredEvidence,
        allowedDomains = policy.allowedDomains,
        forbiddenDomains = policy.forbiddenDomains,
        productionCommitAllowed = policy.productionCommitAllowed,
        existingConfigurationKeys = policy.existingConfigurationKeys
    )

    val diagnosticsSummary: String
        get() = listOf(
            "stage=${stage.rawValue}",
            "domain=${domain.rawValue}",
            "allowed=$allowed",
            "sideEffects=${allowedSideEffects.joinToString("+") { it.rawValue }}",
            "requiredEvidence=${requiredEvidence.joinToString("+") { it.rawValue }}",
            "productionCommitAllowed=$productionCommitAllowed",
            "blockers=${blockers.joinToString("+")}"
        ).joinToString(",")
}

// ── Type 22: CanonicalMigrationStageConfiguration ──

data class CanonicalMigrationStageConfiguration(
    val stage: CanonicalMigrationStage,
    val domain: CanonicalCutoverDomain,
    val policy: CanonicalMigrationStagePolicy
) {
    constructor(
        stage: CanonicalMigrationStage = CanonicalMigrationStage.OFF,
        domain: CanonicalCutoverDomain = CanonicalCutoverDomain.RECORDING_METADATA,
        policy: CanonicalMigrationStagePolicy? = null
    ) : this(
        stage = stage,
        domain = domain,
        policy = policy ?: CanonicalMigrationStagePolicy.defaultPolicy(stage)
    )

    fun summary(): CanonicalMigrationConfigurationSummary {
        val blockers = mutableListOf<String>()
        if (stage == CanonicalMigrationStage.OFF) {
            blockers.add("stageOff")
        }
        if (stage == CanonicalMigrationStage.UNSUPPORTED) {
            blockers.add("unsupportedStage")
        }
        if (policy.allowedDomains.isNotEmpty() &&
            !policy.allowedDomains.contains(domain)
        ) {
            blockers.add("domainNotAllowed")
        }
        if (policy.forbiddenDomains.contains(domain)) {
            blockers.add("domainForbidden")
        }
        val productionCommitStages = setOf(
            CanonicalMigrationStage.RECORDING_METADATA_GUARDED_COMMIT,
            CanonicalMigrationStage.CANARY_N1,
            CanonicalMigrationStage.EXPANDED_CANARY,
            CanonicalMigrationStage.DOMAIN_CUTOVER
        )
        if (policy.allowedSideEffects.contains(
                CanonicalMigrationStageSideEffect.PRODUCTION_COMMIT
            ) && !productionCommitStages.contains(stage)
        ) {
            blockers.add("illegalProductionCommitSideEffect")
        }
        if (policy.productionCommitAllowed !=
            policy.allowedSideEffects.contains(
                CanonicalMigrationStageSideEffect.PRODUCTION_COMMIT
            )
        ) {
            blockers.add("productionCommitPolicyMismatch")
        }
        return CanonicalMigrationConfigurationSummary(
            stage = stage,
            domain = domain,
            allowed = blockers.isEmpty(),
            blockers = blockers,
            policy = policy
        )
    }

    companion object {
        val OFF = CanonicalMigrationStageConfiguration()
    }
}
