package com.rokurics.app.domain.model

import com.rokurics.app.data.SecureUploadUtilities
import com.google.gson.GsonBuilder
import org.json.JSONArray
import org.json.JSONObject

// ── Local Network Sync Platform ──────────────────────────────────────

enum class LocalNetworkSyncPlatform(val rawValue: String) {
    IPHONE("iPhone"),
    MAC("Mac"),
    ANDROID("Android")
}

// ── Inventory Sections ───────────────────────────────────────────────

data class LocalNetworkSyncDeviceSection(
    val deviceID: String,
    val deviceName: String,
    val platform: LocalNetworkSyncPlatform = LocalNetworkSyncPlatform.ANDROID,
    val generatedAt: Long = System.currentTimeMillis(),
    val lastKnownPeerRevision: String? = null,
    val appSchemaVersion: Int = 1
)

data class LocalNetworkSyncRecordingEntry(
    val recordingID: String,
    val metadataHash: String? = null,
    val audioAvailable: Boolean = false,
    val audioChecksum: String? = null,
    val audioSize: Long? = null,
    val uploadLedgerState: String? = null,
    val receiveStatus: String? = null,
    val processingStatus: String? = null,
    val updatedAt: Long = System.currentTimeMillis(),
    val deleted: Boolean = false
)

data class LocalNetworkSyncFolderEntry(
    val folderID: String,
    val parentID: String? = null,
    val path: String? = null,
    val name: String,
    val colorToken: String? = null,
    val updatedAt: Long = System.currentTimeMillis(),
    val revisionHash: String? = null,
    val deleted: Boolean = false
)

data class LocalNetworkSyncStudyItemEntry(
    val itemID: String,
    val kind: StudyItemKind = StudyItemKind.RECORDING_BUNDLE,
    val title: String,
    val folderIDs: List<String> = emptyList(),
    val recordingID: String? = null,
    val updatedAt: Long = System.currentTimeMillis(),
    val revisionHash: String? = null,
    val deleted: Boolean = false
)

data class LocalNetworkSyncArtifactEntry(
    val artifactID: String,
    val kind: LocalNetworkSyncArtifactKind,
    val ownerID: String,
    val checksum: String? = null,
    val size: Long? = null,
    val updatedAt: Long = System.currentTimeMillis(),
    val availability: LocalNetworkSyncArtifactAvailability = LocalNetworkSyncArtifactAvailability.LOCAL,
    val logicalPathToken: String? = null
)

enum class LocalNetworkSyncArtifactKind(val rawValue: String) {
    TRANSCRIPT_MARKDOWN("transcriptMarkdown"),
    TRANSCRIPT_JSON("transcriptJSON"),
    NOTE_MARKDOWN("noteMarkdown"),
    NOTE_JSON("noteJSON"),
    AUDIO("audio");

    val isAutoDownloadAllowed: Boolean
        get() = this != AUDIO
}

enum class LocalNetworkSyncArtifactAvailability(val rawValue: String) {
    LOCAL("local"),
    AVAILABLE_ON_PEER("availableOnPeer"),
    MISSING("missing")
}

// ── Inventory ────────────────────────────────────────────────────────

data class LocalNetworkSyncInventory(
    val device: LocalNetworkSyncDeviceSection,
    val recordings: List<LocalNetworkSyncRecordingEntry> = emptyList(),
    val folders: List<LocalNetworkSyncFolderEntry> = emptyList(),
    val studyItems: List<LocalNetworkSyncStudyItemEntry> = emptyList(),
    val artifacts: List<LocalNetworkSyncArtifactEntry> = emptyList(),
    val studyManifest: StudyLibrarySyncManifest? = null
) {
    companion object {
        const val APP_SCHEMA_VERSION = 1
    }

    val inventoryHash: String by lazy { computeInventoryHash() }

    private fun computeInventoryHash(): String {
        val gson = GsonBuilder().disableHtmlEscaping().create()
        val payload = JSONObject().apply {
            put("device", JSONObject(gson.toJson(device)))
            put("recordings", JSONArray().apply {
                recordings.sortedBy { it.recordingID }.forEach { put(JSONObject(gson.toJson(it))) }
            })
            put("folders", JSONArray().apply {
                folders.sortedBy { it.folderID }.forEach { put(JSONObject(gson.toJson(it))) }
            })
            put("studyItems", JSONArray().apply {
                studyItems.sortedBy { it.itemID }.forEach { put(JSONObject(gson.toJson(it))) }
            })
            put("artifacts", JSONArray().apply {
                artifacts.sortedBy { it.artifactID }.forEach { put(JSONObject(gson.toJson(it))) }
            })
        }
        return SecureUploadUtilities.sha256Hex(payload.toString().toByteArray(Charsets.UTF_8))
    }
}

// ── Diff Plan ────────────────────────────────────────────────────────

data class LocalNetworkSyncDiffPlan(
    val uploadMetadataActions: List<LocalNetworkSyncDiffAction> = emptyList(),
    val uploadArtifactActions: List<LocalNetworkSyncDiffAction> = emptyList(),
    val downloadMetadataActions: List<LocalNetworkSyncDiffAction> = emptyList(),
    val downloadArtifactActions: List<LocalNetworkSyncDiffAction> = emptyList(),
    val uploadRecordingAudioActions: List<LocalNetworkSyncDiffAction> = emptyList(),
    val conflictActions: List<LocalNetworkSyncDiffAction> = emptyList(),
    val noOps: List<LocalNetworkSyncDiffAction> = emptyList()
) {
    val hasWork: Boolean
        get() = uploadMetadataActions.isNotEmpty() || uploadArtifactActions.isNotEmpty() ||
                downloadMetadataActions.isNotEmpty() || downloadArtifactActions.isNotEmpty() ||
                uploadRecordingAudioActions.isNotEmpty() || conflictActions.isNotEmpty()

    val summary: String
        get() = buildString {
            val parts = mutableListOf<String>()
            if (uploadMetadataActions.isNotEmpty()) parts.add("↑${uploadMetadataActions.size} metadata")
            if (uploadArtifactActions.isNotEmpty()) parts.add("↑${uploadArtifactActions.size} artifacts")
            if (uploadRecordingAudioActions.isNotEmpty()) parts.add("↑${uploadRecordingAudioActions.size} audio")
            if (downloadMetadataActions.isNotEmpty()) parts.add("↓${downloadMetadataActions.size} metadata")
            if (downloadArtifactActions.isNotEmpty()) parts.add("↓${downloadArtifactActions.size} artifacts")
            if (conflictActions.isNotEmpty()) parts.add("${conflictActions.size} conflicts")
            append(parts.ifEmpty { listOf("已同步") }.joinToString(" · "))
        }
}

enum class LocalNetworkSyncDiffActionKind(val rawValue: String) {
    UPLOAD_METADATA("uploadMetadata"),
    UPLOAD_ARTIFACT("uploadArtifact"),
    DOWNLOAD_METADATA("downloadMetadata"),
    DOWNLOAD_ARTIFACT("downloadArtifact"),
    UPLOAD_RECORDING_AUDIO("uploadRecordingAudio"),
    CONFLICT("conflict"),
    NO_OP("noOp")
}

data class LocalNetworkSyncDiffAction(
    val id: String,
    val kind: LocalNetworkSyncDiffActionKind,
    val entityKind: String,
    val entityID: String,
    val reason: String
)

// ── Local Network Sync State ─────────────────────────────────────────

data class LocalNetworkSyncState(
    val version: Int = 1,
    val lastSyncAt: Long? = null,
    val lastSuccessfulSyncAt: Long? = null,
    val lastPeerDeviceID: String? = null,
    val lastLocalInventoryHash: String? = null,
    val lastPeerInventoryHash: String? = null,
    val lastAppliedPeerRevision: String? = null,
    val consecutiveFailureCount: Int = 0,
    val nextAllowedSyncAt: Long? = null,
    val lastErrorCode: String? = null,
    val lastErrorMessage: String? = null,
    val pendingUploadCount: Int = 0,
    val pendingDownloadCount: Int = 0
) {
    companion object {
        const val CURRENT_VERSION = 1
        const val BASE_BACKOFF_SECONDS = 30L
        const val MAX_BACKOFF_SECONDS = 600L
    }

    val isSyncAllowed: Boolean
        get() {
            val nextAllowed = nextAllowedSyncAt ?: return true
            return System.currentTimeMillis() >= nextAllowed
        }

    val backoffRemainingSeconds: Long
        get() {
            val nextAllowed = nextAllowedSyncAt ?: return 0
            val remaining = (nextAllowed - System.currentTimeMillis()) / 1000
            return remaining.coerceAtLeast(0)
        }
}
