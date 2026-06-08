package com.rokurics.app.domain.canonical

import java.security.MessageDigest
import java.util.Date
import java.util.Locale
import java.util.UUID

enum class CanonicalInventoryRuntimeNodeRole {
    IPHONE,
    MAC,
    ANDROID,
    UNKNOWN
}

enum class CanonicalInventoryRuntimeSourceKind {
    LOCAL_FILESYSTEM,
    CACHE_BACKED,
    PEER_MANIFEST,
    UNKNOWN
}

data class CanonicalChecksumCacheKey(
    val logicalToken: String,
    val byteSize: Long,
    val mtime: Long,
    val hashAlgorithm: String,
    val schemaVersion: Int,
    val nodeRole: CanonicalInventoryRuntimeNodeRole
)

data class CanonicalChecksumCacheEntry(
    val key: CanonicalChecksumCacheKey,
    val hashValue: String,
    val createdAt: CanonicalTimestamp,
    val lastAccessAt: CanonicalTimestamp,
    val hitCount: Int
)

enum class CanonicalChecksumCacheEvent {
    HIT,
    MISS,
    STALE,
    ERROR
}

data class CanonicalChecksumCacheResult(
    val hashValue: String?,
    val byteSize: Long,
    val mtime: Long,
    val event: CanonicalChecksumCacheEvent,
    val hashComputed: Boolean,
    val hashUnavailable: Boolean,
    val failure: CanonicalInventoryRuntimeFailure?,
    val computeDurationMs: Int
) {
    val redactedHashPrefix: String?
        get() = hashValue?.take(12)
}

data class CanonicalInventoryObjectCounts(
    val recordingMetadataCount: Int = 0,
    val libraryFolderCount: Int = 0,
    val libraryItemCount: Int = 0,
    val artifactCount: Int = 0,
    val audioDescriptorCount: Int = 0
)

class CanonicalChecksumCacheStore {

    private data class CachePayload(
        val schemaVersion: Int,
        val entries: List<CanonicalChecksumCacheEntry>
    )

    private val recordsByToken: MutableMap<String, CanonicalChecksumCacheEntry> = mutableMapOf()
    private var cacheCorrupted: Boolean = false

    fun get(key: CanonicalChecksumCacheKey): CanonicalChecksumCacheEntry? {
        val entry = recordsByToken[key.logicalToken] ?: return null
        if (entry.key != key) return null
        val updated = entry.copy(
            lastAccessAt = CanonicalTimestamp(Date()),
            hitCount = entry.hitCount + 1
        )
        recordsByToken[key.logicalToken] = updated
        return updated
    }

    fun put(entry: CanonicalChecksumCacheEntry) {
        recordsByToken[entry.key.logicalToken] = entry
    }

    fun evict(logicalToken: String) {
        recordsByToken.remove(logicalToken)
    }

    fun evictAll() {
        recordsByToken.clear()
        cacheCorrupted = false
    }

    fun checksum(
        hashValue: String?,
        byteSize: Long,
        mtime: Long,
        logicalToken: String?,
        nodeRole: CanonicalInventoryRuntimeNodeRole,
        configuration: CanonicalInventoryRuntimeConfiguration = CanonicalInventoryRuntimeConfiguration(),
        now: Date = Date()
    ): CanonicalChecksumCacheResult {
        val safeToken = safeLogicalToken(logicalToken) ?: "unknown-file"

        val key = CanonicalChecksumCacheKey(
            logicalToken = safeToken,
            byteSize = byteSize,
            mtime = mtime,
            hashAlgorithm = configuration.hashAlgorithm,
            schemaVersion = configuration.checksumSchemaVersion,
            nodeRole = nodeRole
        )

        val hashStartedAt = System.currentTimeMillis()

        if (configuration.persistentChecksumCacheEnabled) {
            val existing = recordsByToken[safeToken]
            if (existing != null && existing.key == key) {
                val updated = existing.copy(
                    lastAccessAt = CanonicalTimestamp(now),
                    hitCount = existing.hitCount + 1
                )
                recordsByToken[safeToken] = updated
                return CanonicalChecksumCacheResult(
                    hashValue = existing.hashValue,
                    byteSize = byteSize,
                    mtime = mtime,
                    event = CanonicalChecksumCacheEvent.HIT,
                    hashComputed = false,
                    hashUnavailable = false,
                    failure = null,
                    computeDurationMs = 0
                )
            }
        }

        val event: CanonicalChecksumCacheEvent =
            if (recordsByToken[safeToken] == null) CanonicalChecksumCacheEvent.MISS
            else CanonicalChecksumCacheEvent.STALE

        val computeDurationMs = (System.currentTimeMillis() - hashStartedAt).toInt().coerceAtLeast(0)

        if (hashValue == null) {
            return CanonicalChecksumCacheResult(
                hashValue = null,
                byteSize = byteSize,
                mtime = mtime,
                event = CanonicalChecksumCacheEvent.ERROR,
                hashComputed = false,
                hashUnavailable = true,
                failure = CanonicalInventoryRuntimeFailure.HASH_UNAVAILABLE,
                computeDurationMs = computeDurationMs
            )
        }

        val entry = CanonicalChecksumCacheEntry(
            key = key,
            hashValue = hashValue,
            createdAt = CanonicalTimestamp(now),
            lastAccessAt = CanonicalTimestamp(now),
            hitCount = 1
        )

        if (configuration.persistentChecksumCacheEnabled) {
            recordsByToken[safeToken] = entry
        }

        return CanonicalChecksumCacheResult(
            hashValue = hashValue,
            byteSize = byteSize,
            mtime = mtime,
            event = event,
            hashComputed = true,
            hashUnavailable = false,
            failure = if (cacheCorrupted) CanonicalInventoryRuntimeFailure.CACHE_CORRUPTED else null,
            computeDurationMs = computeDurationMs
        )
    }

    fun loadFromPayload(entries: List<CanonicalChecksumCacheEntry>, schemaVersion: Int, expectedSchemaVersion: Int) {
        recordsByToken.clear()
        cacheCorrupted = false
        if (schemaVersion != expectedSchemaVersion) {
            cacheCorrupted = true
            return
        }
        entries.forEach { entry ->
            recordsByToken[entry.key.logicalToken] = entry
        }
    }

    fun exportEntries(): List<CanonicalChecksumCacheEntry> {
        return recordsByToken.values.sortedBy { it.key.logicalToken }
    }

    val isCorrupted: Boolean
        get() = cacheCorrupted

    val entryCount: Int
        get() = recordsByToken.size

    companion object {
        fun safeLogicalToken(token: String?): String? {
            val trimmed = token?.trim()
            if (trimmed.isNullOrEmpty() || trimmed.startsWith("/") ||
                trimmed.contains("://") || trimmed.contains("\\")
            ) return null
            val components = trimmed.split("/")
            if (components.isEmpty() || components.any { it.isEmpty() || it == "." || it == ".." }) return null
            return trimmed
        }
    }
}

enum class CanonicalInventoryRuntimeMode {
    DISABLED,
    CACHE_BACKED,
    DIRECT_BUILD,
    BLOCKED
}

data class CanonicalInventoryRuntimeDiagnostics(
    val checksumCacheHitCount: Int = 0,
    val checksumCacheMissCount: Int = 0,
    val checksumCacheStaleCount: Int = 0,
    val checksumCacheErrorCount: Int = 0,
    val fileScanCount: Int = 0,
    val hashComputedCount: Int = 0,
    val mainActorHashBlockedCount: Int = 0,
    val mainActorScanBlockedCount: Int = 0,
    val duplicateBuildCount: Int = 0,
    val scanDurationMs: Int = 0,
    val hashDurationMs: Int = 0,
    val events: List<CanonicalInventoryRuntimeDiagnosticEvent> = emptyList()
) {
    fun merge(result: CanonicalChecksumCacheResult): CanonicalInventoryRuntimeDiagnostics {
        return copy(
            fileScanCount = fileScanCount + 1,
            hashComputedCount = hashComputedCount + if (result.hashComputed) 1 else 0,
            checksumCacheHitCount = checksumCacheHitCount + if (result.event == CanonicalChecksumCacheEvent.HIT) 1 else 0,
            checksumCacheMissCount = checksumCacheMissCount + if (result.event == CanonicalChecksumCacheEvent.MISS) 1 else 0,
            checksumCacheStaleCount = checksumCacheStaleCount + if (result.event == CanonicalChecksumCacheEvent.STALE) 1 else 0,
            checksumCacheErrorCount = checksumCacheErrorCount + if (result.event == CanonicalChecksumCacheEvent.ERROR) 1 else 0,
            hashDurationMs = hashDurationMs + result.computeDurationMs
        )
    }
}

data class CanonicalInventoryRuntimeDiagnosticEvent(
    val phase: String,
    val timestamp: CanonicalTimestamp,
    val detail: String?
)

enum class CanonicalInventoryRuntimeFailure {
    CACHE_CORRUPTED,
    FILE_METADATA_UNAVAILABLE,
    HASH_UNAVAILABLE,
    CANCELLED,
    UNKNOWN
}

data class CanonicalInventoryRuntimeSnapshot(
    val syncRunID: String,
    val nodeRole: CanonicalInventoryRuntimeNodeRole,
    val sourceKind: CanonicalInventoryRuntimeSourceKind,
    val buildTimingMs: Int,
    val objectCounts: CanonicalInventoryObjectCounts,
    val cacheHitCount: Int,
    val cacheMissCount: Int,
    val cacheStaleCount: Int,
    val cacheErrorCount: Int,
    val scanCount: Int,
    val hashCount: Int,
    val duplicateBuildCount: Int,
    val mainActorBlockerCount: Int,
    val redacted: Boolean = true,
    val reusedWithinTick: Boolean = false,
    val mainActorBlocked: Boolean = false
) {
    companion object {
        operator fun invoke(
            syncRunID: String,
            nodeRole: CanonicalInventoryRuntimeNodeRole,
            sourceKind: CanonicalInventoryRuntimeSourceKind,
            buildTimingMs: Int,
            objectCounts: CanonicalInventoryObjectCounts,
            diagnostics: CanonicalInventoryRuntimeDiagnostics,
            events: List<CanonicalInventoryRuntimeDiagnosticEvent> = emptyList(),
            duplicateBuildCount: Int = 0,
            mainActorBlocked: Boolean = false,
            mainActorBlockerCount: Int = 0,
            redacted: Boolean = true,
            reusedWithinTick: Boolean = false
        ): CanonicalInventoryRuntimeSnapshot {
            return CanonicalInventoryRuntimeSnapshot(
                syncRunID = syncRunID,
                nodeRole = nodeRole,
                sourceKind = sourceKind,
                buildTimingMs = buildTimingMs,
                objectCounts = objectCounts,
                cacheHitCount = diagnostics.checksumCacheHitCount,
                cacheMissCount = diagnostics.checksumCacheMissCount,
                cacheStaleCount = diagnostics.checksumCacheStaleCount,
                cacheErrorCount = diagnostics.checksumCacheErrorCount,
                scanCount = diagnostics.fileScanCount,
                hashCount = diagnostics.hashComputedCount,
                duplicateBuildCount = duplicateBuildCount,
                mainActorBlockerCount = mainActorBlockerCount,
                redacted = redacted,
                reusedWithinTick = reusedWithinTick,
                mainActorBlocked = mainActorBlocked
            )
        }
    }
}

data class CanonicalInventoryRuntimeResult(
    val snapshot: CanonicalInventoryRuntimeSnapshot,
    val diagnostics: CanonicalInventoryRuntimeDiagnostics,
    val failures: List<CanonicalInventoryRuntimeFailure> = emptyList()
)

class CanonicalInventoryRuntimeBuilder {

    private data class RuntimeScope(
        val syncRunID: String,
        val nodeRole: CanonicalInventoryRuntimeNodeRole,
        val sourceKind: CanonicalInventoryRuntimeSourceKind
    ) {
        val scopeKey: String
            get() = "${nodeRole.name}|${sourceKind.name}|$syncRunID"
    }

    private val snapshotsByScope: MutableMap<String, CanonicalInventoryRuntimeSnapshot> = mutableMapOf()

    fun build(
        from: CanonicalInventoryInputSnapshot,
        syncRunID: String = UUID.randomUUID().toString().take(8),
        nodeRole: CanonicalInventoryRuntimeNodeRole = CanonicalInventoryRuntimeNodeRole.ANDROID,
        sourceKind: CanonicalInventoryRuntimeSourceKind = CanonicalInventoryRuntimeSourceKind.LOCAL_FILESYSTEM,
        configuration: CanonicalInventoryRuntimeConfiguration = CanonicalInventoryRuntimeConfiguration.DIRECT_BUILD
    ): CanonicalInventoryRuntimeResult {
        if (!configuration.canBuild) {
            return CanonicalInventoryRuntimeResult(
                snapshot = CanonicalInventoryRuntimeSnapshot(
                    syncRunID = syncRunID,
                    nodeRole = nodeRole,
                    sourceKind = sourceKind,
                    buildTimingMs = 0,
                    objectCounts = CanonicalInventoryObjectCounts(),
                    cacheHitCount = 0,
                    cacheMissCount = 0,
                    cacheStaleCount = 0,
                    cacheErrorCount = 0,
                    scanCount = 0,
                    hashCount = 0,
                    duplicateBuildCount = 0,
                    mainActorBlockerCount = 0,
                    redacted = configuration.redactedDiagnostics
                ),
                diagnostics = CanonicalInventoryRuntimeDiagnostics(),
                failures = listOf(CanonicalInventoryRuntimeFailure.UNKNOWN)
            )
        }

        val scope = RuntimeScope(syncRunID, nodeRole, sourceKind)
        val existing = snapshotsByScope[scope.scopeKey]
        val duplicateBuildCount: Int

        val startedAt = System.currentTimeMillis()

        val artifactCount = from.recordingObjects.sumOf { it.artifacts.size }
        val audioDescriptorCount = from.recordingObjects.count { it.audioArtifact != null }
        val objectCounts = CanonicalInventoryObjectCounts(
            recordingMetadataCount = from.recordingObjects.size,
            libraryFolderCount = from.libraryObjects.count {
                it.kind == CanonicalObjectKind.FOLDER
            },
            libraryItemCount = from.libraryObjects.count {
                it.kind == CanonicalObjectKind.STANDALONE_STUDY_ITEM ||
                        it.kind == CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM ||
                        it.kind == CanonicalObjectKind.STANDALONE_NOTE
            },
            artifactCount = artifactCount,
            audioDescriptorCount = audioDescriptorCount
        )

        val scanCount = from.recordingObjects.size + from.libraryObjects.size +
                from.libraryTombstones.size + from.unsupportedObjects.size
        val hashCount = from.recordingObjects.size +
                from.libraryObjects.size +
                from.libraryTombstones.size

        val buildTimingMs = (System.currentTimeMillis() - startedAt).toInt().coerceAtLeast(0)

        val diagnostics: CanonicalInventoryRuntimeDiagnostics
        val snapshot: CanonicalInventoryRuntimeSnapshot

        if (existing != null) {
            duplicateBuildCount = existing.duplicateBuildCount + 1
            diagnostics = CanonicalInventoryRuntimeDiagnostics(
                duplicateBuildCount = duplicateBuildCount,
                fileScanCount = scanCount,
                hashComputedCount = hashCount
            )
            snapshot = CanonicalInventoryRuntimeSnapshot(
                syncRunID = syncRunID,
                nodeRole = nodeRole,
                sourceKind = sourceKind,
                buildTimingMs = buildTimingMs,
                objectCounts = objectCounts,
                diagnostics = diagnostics,
                duplicateBuildCount = duplicateBuildCount,
                reusedWithinTick = true,
                redacted = configuration.redactedDiagnostics
            )
        } else {
            duplicateBuildCount = 0
            diagnostics = CanonicalInventoryRuntimeDiagnostics(
                fileScanCount = scanCount,
                hashComputedCount = hashCount
            )
            snapshot = CanonicalInventoryRuntimeSnapshot(
                syncRunID = syncRunID,
                nodeRole = nodeRole,
                sourceKind = sourceKind,
                buildTimingMs = buildTimingMs,
                objectCounts = objectCounts,
                diagnostics = diagnostics,
                duplicateBuildCount = duplicateBuildCount,
                redacted = configuration.redactedDiagnostics
            )
            snapshotsByScope[scope.scopeKey] = snapshot
        }

        return CanonicalInventoryRuntimeResult(
            snapshot = snapshot,
            diagnostics = diagnostics
        )
    }

    fun existingSnapshot(
        syncRunID: String,
        nodeRole: CanonicalInventoryRuntimeNodeRole,
        sourceKind: CanonicalInventoryRuntimeSourceKind
    ): CanonicalInventoryRuntimeSnapshot? {
        val scope = RuntimeScope(syncRunID, nodeRole, sourceKind)
        return snapshotsByScope[scope.scopeKey]
    }

    fun remember(snapshot: CanonicalInventoryRuntimeSnapshot) {
        val scope = RuntimeScope(snapshot.syncRunID, snapshot.nodeRole, snapshot.sourceKind)
        snapshotsByScope[scope.scopeKey] = snapshot
    }

    fun reusedSnapshot(snapshot: CanonicalInventoryRuntimeSnapshot): CanonicalInventoryRuntimeSnapshot {
        return snapshot.copy(reusedWithinTick = true)
    }

    fun duplicateDetectedSnapshot(snapshot: CanonicalInventoryRuntimeSnapshot): CanonicalInventoryRuntimeSnapshot {
        return snapshot.copy(
            duplicateBuildCount = snapshot.duplicateBuildCount + 1
        )
    }

    fun reset() {
        snapshotsByScope.clear()
    }
}

data class CanonicalInventoryRuntimeReport(
    val syncRunID: String,
    val nodeRole: CanonicalInventoryRuntimeNodeRole,
    val buildDurationMs: Int,
    val scanDurationMs: Int,
    val hashDurationMs: Int,
    val cacheHitCount: Int,
    val cacheMissCount: Int,
    val cacheStaleCount: Int,
    val duplicateBuildCount: Int,
    val mainActorHashBlockedCount: Int,
    val mainActorScanBlockedCount: Int,
    val inventoryObjectCounts: CanonicalInventoryObjectCounts,
    val redacted: Boolean
)

object CanonicalInventoryRuntimeReportExporter {

    fun report(from: CanonicalInventoryRuntimeSnapshot): CanonicalInventoryRuntimeReport {
        val redacted = from.redacted
        return CanonicalInventoryRuntimeReport(
            syncRunID = if (redacted) from.syncRunID.take(8) else from.syncRunID,
            nodeRole = from.nodeRole,
            buildDurationMs = from.buildTimingMs.coerceAtLeast(0),
            scanDurationMs = if (redacted) 0 else from.scanCount,
            hashDurationMs = if (redacted) 0 else from.hashCount,
            cacheHitCount = from.cacheHitCount,
            cacheMissCount = from.cacheMissCount,
            cacheStaleCount = from.cacheStaleCount,
            duplicateBuildCount = from.duplicateBuildCount,
            mainActorHashBlockedCount = from.mainActorBlockerCount,
            mainActorScanBlockedCount = if (from.mainActorBlocked) from.scanCount else 0,
            inventoryObjectCounts = if (redacted)
                CanonicalInventoryObjectCounts() else from.objectCounts,
            redacted = redacted
        )
    }

    fun diagnosticsSummary(from: CanonicalInventoryRuntimeSnapshot): String {
        val report = report(from)
        val parts = mutableListOf(
            "syncRunID=${report.syncRunID}",
            "nodeRole=${report.nodeRole.name.lowercase(Locale.US)}",
            "buildDurationMs=${report.buildDurationMs}",
            "scanDurationMs=${report.scanDurationMs}",
            "hashDurationMs=${report.hashDurationMs}",
            "cacheHitCount=${report.cacheHitCount}",
            "cacheMissCount=${report.cacheMissCount}",
            "cacheStaleCount=${report.cacheStaleCount}",
            "duplicateBuildCount=${report.duplicateBuildCount}",
            "mainActorHashBlockedCount=${report.mainActorHashBlockedCount}",
            "mainActorScanBlockedCount=${report.mainActorScanBlockedCount}",
            "redacted=${report.redacted}"
        )
        if (!report.redacted) {
            parts.addAll(listOf(
                "recordingMetadataCount=${report.inventoryObjectCounts.recordingMetadataCount}",
                "libraryFolderCount=${report.inventoryObjectCounts.libraryFolderCount}",
                "libraryItemCount=${report.inventoryObjectCounts.libraryItemCount}",
                "artifactCount=${report.inventoryObjectCounts.artifactCount}",
                "audioDescriptorCount=${report.inventoryObjectCounts.audioDescriptorCount}"
            ))
        }
        return parts.joinToString(separator = ",")
    }

    fun jsonString(from: CanonicalInventoryRuntimeSnapshot): String {
        val report = report(from)
        val sb = StringBuilder("{")
        sb.append("\"syncRunID\":\"${report.syncRunID.escapeJson()}\",")
        sb.append("\"nodeRole\":\"${report.nodeRole.name.lowercase(Locale.US)}\",")
        sb.append("\"buildDurationMs\":${report.buildDurationMs},")
        sb.append("\"scanDurationMs\":${report.scanDurationMs},")
        sb.append("\"hashDurationMs\":${report.hashDurationMs},")
        sb.append("\"cacheHitCount\":${report.cacheHitCount},")
        sb.append("\"cacheMissCount\":${report.cacheMissCount},")
        sb.append("\"cacheStaleCount\":${report.cacheStaleCount},")
        sb.append("\"duplicateBuildCount\":${report.duplicateBuildCount},")
        sb.append("\"mainActorHashBlockedCount\":${report.mainActorHashBlockedCount},")
        sb.append("\"mainActorScanBlockedCount\":${report.mainActorScanBlockedCount},")
        sb.append("\"inventoryObjectCounts\":{")
        sb.append("\"recordingMetadataCount\":${report.inventoryObjectCounts.recordingMetadataCount},")
        sb.append("\"libraryFolderCount\":${report.inventoryObjectCounts.libraryFolderCount},")
        sb.append("\"libraryItemCount\":${report.inventoryObjectCounts.libraryItemCount},")
        sb.append("\"artifactCount\":${report.inventoryObjectCounts.artifactCount},")
        sb.append("\"audioDescriptorCount\":${report.inventoryObjectCounts.audioDescriptorCount}},")
        sb.append("\"redacted\":${report.redacted}}")
        return sb.toString()
    }

    private fun String.escapeJson(): String {
        return this.replace("\\", "\\\\")
            .replace("\"", "\\\"")
            .replace("\n", "\\n")
            .replace("\r", "\\r")
            .replace("\t", "\\t")
    }
}
