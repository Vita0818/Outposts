package com.rokurics.app.domain.model

import com.rokurics.app.data.SecureUploadUtilities
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import org.json.JSONArray
import org.json.JSONObject
import java.util.Date

// ── Sync Runtime Configuration ──────────────────────────────────────

data class StudyLibrarySyncRuntimeConfiguration(
    val gitBackedSyncEnabled: Boolean = false
) {
    companion object {
        val default = StudyLibrarySyncRuntimeConfiguration(gitBackedSyncEnabled = false)
        val disabled = StudyLibrarySyncRuntimeConfiguration(gitBackedSyncEnabled = false)
    }
}

// ── Sync Entity Kinds & Operations ───────────────────────────────────

enum class StudyLibrarySyncEntityKind { ITEM, FOLDER }

enum class StudyLibrarySyncOperation { UPSERT, DELETE, TRASH, RESTORE, DELETE_METADATA_ONLY }

// ── Tombstone ────────────────────────────────────────────────────────

data class StudyLibrarySyncTombstone(
    val id: String,
    val entityKind: StudyLibrarySyncEntityKind,
    val entityID: String,
    val operation: StudyLibrarySyncOperation,
    val updatedAt: Long,
    val modifiedByDeviceID: String? = null
)

// ── Sync Change ──────────────────────────────────────────────────────

data class StudyLibrarySyncChange(
    val id: String,
    val entityKind: StudyLibrarySyncEntityKind,
    val entityID: String,
    val operation: StudyLibrarySyncOperation,
    val updatedAt: Long,
    val modifiedByDeviceID: String? = null,
    val itemPayload: StudyItemMetadata? = null,
    val folderPayload: StudyFolderMetadata? = null
)

// ── Pending Recording Upload ─────────────────────────────────────────

enum class PendingRecordingUploadStatus { PENDING, UPLOADING, UPLOADED, FAILED }

data class PendingRecordingUpload(
    val id: String,
    val itemID: String,
    val recordingID: String,
    val localAudioRelativePath: String,
    val targetDeviceID: String,
    val status: PendingRecordingUploadStatus = PendingRecordingUploadStatus.PENDING,
    val createdAt: Long = System.currentTimeMillis(),
    val updatedAt: Long = System.currentTimeMillis(),
    val lastAttemptAt: Long? = null,
    val retryCount: Int = 0,
    val lastError: String? = null
)

// ── Sync Manifest ────────────────────────────────────────────────────

data class StudyLibrarySyncManifest(
    val deviceID: String,
    val generatedAt: Long = System.currentTimeMillis(),
    val libraryVersion: Int = 1,
    val items: List<StudyItemMetadata> = emptyList(),
    val folders: List<StudyFolderMetadata> = emptyList(),
    val tombstones: List<StudyLibrarySyncTombstone> = emptyList(),
    val pendingUploads: List<PendingRecordingUpload> = emptyList(),
    val baseCommitID: String? = null,
    val commitID: String? = null,
    val localManifestHash: String? = null,
    val checksum: String = ""
) {
    val resolvedChecksum: String get() = checksum.ifEmpty { computeChecksum() }

    fun hasValidChecksum(): Boolean {
        val computed = computeChecksum()
        val legacy = legacyComputeChecksum()
        return resolvedChecksum == computed || resolvedChecksum == legacy
    }

    fun computeChecksum(): String {
        val gson = GsonBuilder().disableHtmlEscaping().create()
        val itemsSorted = items.sortedBy { it.itemID }
        val foldersSorted = folders.sortedBy { it.folderID }
        val tombstonesSorted = tombstones.sortedBy { it.id }
        val uploadsSorted = pendingUploads.sortedBy { it.id }

        val payload = JSONObject().apply {
            put("deviceID", deviceID)
            put("generatedAt", generatedAt)
            put("libraryVersion", libraryVersion)
            put("items", JSONArray().apply {
                itemsSorted.forEach { put(JSONObject(gson.toJson(it))) }
            })
            put("folders", JSONArray().apply {
                foldersSorted.forEach { put(JSONObject(gson.toJson(it))) }
            })
            put("tombstones", JSONArray().apply {
                tombstonesSorted.forEach { put(JSONObject(gson.toJson(it))) }
            })
            put("pendingUploads", JSONArray().apply {
                uploadsSorted.forEach { put(JSONObject(gson.toJson(it))) }
            })
        }
        return SecureUploadUtilities.sha256Hex(payload.toString().toByteArray(Charsets.UTF_8))
    }

    private fun legacyComputeChecksum(): String {
        val gson = GsonBuilder().disableHtmlEscaping().create()
        val itemsSorted = items.sortedBy { it.itemID }
        val foldersSorted = folders.sortedBy { it.folderID }
        val tombstonesSorted = tombstones.sortedBy { it.id }

        val payload = JSONObject().apply {
            put("deviceID", deviceID)
            put("generatedAt", generatedAt)
            put("libraryVersion", libraryVersion)
            put("items", JSONArray().apply {
                itemsSorted.forEach { put(JSONObject(gson.toJson(it))) }
            })
            put("folders", JSONArray().apply {
                foldersSorted.forEach { put(JSONObject(gson.toJson(it))) }
            })
            put("tombstones", JSONArray().apply {
                tombstonesSorted.forEach { put(JSONObject(gson.toJson(it))) }
            })
        }
        return SecureUploadUtilities.sha256Hex(payload.toString().toByteArray(Charsets.UTF_8))
    }
}

// ── Sync Apply Result ────────────────────────────────────────────────

data class StudyLibrarySyncApplyResult(
    var appliedItemCount: Int = 0,
    var appliedFolderCount: Int = 0,
    var tombstoneCount: Int = 0,
    var conflictCount: Int = 0,
    var skippedOlderCount: Int = 0,
    var failedChanges: Int = 0
) {
    val summaryText: String
        get() = buildString {
            val parts = mutableListOf<String>()
            if (appliedItemCount > 0) parts.add("$appliedItemCount items")
            if (appliedFolderCount > 0) parts.add("$appliedFolderCount folders")
            if (conflictCount > 0) parts.add("$conflictCount conflicts preserved")
            if (skippedOlderCount > 0) parts.add("$skippedOlderCount skipped (older)")
            if (failedChanges > 0) parts.add("$failedChanges failed")
            append(parts.ifEmpty { listOf("no changes") }.joinToString(", "))
        }
}

data class StudyLibrarySyncStatusSummary(
    val lastSyncAt: Long? = null,
    val statusText: String? = null,
    val pendingLocalChanges: Int = 0,
    val pendingUploads: Int = 0
)

// ── Manifest Request / Response ──────────────────────────────────────

data class StudyLibrarySyncManifestRequest(
    val manifest: StudyLibrarySyncManifest? = null
)

data class StudyLibrarySyncManifestResponse(
    val ok: Boolean = false,
    val manifest: StudyLibrarySyncManifest? = null,
    val applyResult: StudyLibrarySyncApplyResult? = null,
    val baseCommitID: String? = null,
    val newCommitID: String? = null,
    val remoteChanges: List<StudyLibrarySyncChange>? = null,
    val rejectedChanges: List<StudyLibrarySyncChange>? = null,
    val error: String? = null
)

// ── Sync State ───────────────────────────────────────────────────────

data class StudyLibrarySyncState(
    val deviceID: String,
    val lastPulledAt: Long? = null,
    val lastPushedAt: Long? = null,
    val lastSuccessfulSyncAt: Long? = null,
    val lastRemoteManifestHash: String? = null,
    val lastKnownRemoteCommitID: String? = null,
    val pendingLocalChanges: Int = 0,
    val pendingUploads: Int = 0,
    val failedChanges: Int = 0,
    val lastError: String? = null
)
