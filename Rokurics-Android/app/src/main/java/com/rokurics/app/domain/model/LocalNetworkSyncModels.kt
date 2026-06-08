package com.rokurics.app.domain.model

import com.rokurics.app.data.SecureUploadUtilities
import com.google.gson.GsonBuilder
import org.json.JSONArray
import org.json.JSONObject

// ── Local Network Sync Platform ──────────────────────────────────────

enum class LocalNetworkSyncPlatform(val rawValue: String) {
    IPHONE("iPhone"),
    MAC("Mac"),
    ANDROID("Android");

    companion object {
        fun from(raw: String?): LocalNetworkSyncPlatform {
            val normalized = raw?.trim()?.lowercase() ?: return ANDROID
            return when (normalized) {
                "iphone", "ios", "iOS", "iphoneos" -> IPHONE
                "mac", "macos", "osx" -> MAC
                "android", "androidos" -> ANDROID
                else -> try {
                    valueOf(normalized.uppercase())
                } catch (_: Exception) {
                    ANDROID
                }
            }
        }
    }
}

enum class LocalNetworkSyncControlPlaneState(val rawValue: String) {
    IDLE("idle"),
    SYNC_START_SIGNAL_SENT("syncStartSignalSent"),
    SYNC_START_SIGNAL_RECEIVED("syncStartSignalReceived"),
    SYNC_START_ACKED("syncStartAcked"),
    INVENTORY_EXCHANGING("inventoryExchanging"),
    PLANNING_TRANSFERS("planningTransfers"),
    TRANSFER_JOBS_CREATED("transferJobsCreated"),
    TRANSFERRING("transferring"),
    PAUSED_DISCONNECTED("pausedDisconnected"),
    RESUMING("resuming"),
    COMPLETED("completed"),
    FAILED("failed"),
    CANCELLED("cancelled")
}

enum class LocalNetworkTransferState(val rawValue: String) {
    PENDING("pending"),
    TRANSFERRING("transferring"),
    PAUSED("paused"),
    PAUSED_DISCONNECTED("pausedDisconnected"),
    RETRY_PENDING("retryPending"),
    RESUMING("resuming"),
    VERIFYING("verifying"),
    COMPLETE("complete"),
    FAILED("failed"),
    CONFLICT("conflict");

    val isVisibleInActionArea: Boolean
        get() = this != COMPLETE
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
    val title: String? = null,
    val createdAt: Long? = null,
    val uploadLedgerState: String? = null,
    val receiveStatus: String? = null,
    val processingStatus: String? = null,
    val tombstone: Boolean? = null,
    val audioAvailability: LocalNetworkSyncArtifactAvailability = LocalNetworkSyncArtifactAvailability.LOCAL,
    val uploadStatus: String? = null,
    val transcriptionStatus: String? = null,
    val noteStatus: String? = null,
    val sourceDeviceID: String? = null,
    val artifactRefs: List<String>? = null,
    val audioLogicalPathToken: String? = null,
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
    val deleted: Boolean = false,
    val path: String? = null,
    val conflictStatus: String? = null
)

data class LocalNetworkSyncArtifactEntry(
    val artifactID: String,
    val kind: LocalNetworkSyncArtifactKind,
    val ownerID: String,
    val checksum: String? = null,
    val size: Long? = null,
    val updatedAt: Long = System.currentTimeMillis(),
    val availability: LocalNetworkSyncArtifactAvailability = LocalNetworkSyncArtifactAvailability.LOCAL,
    val logicalPathToken: String? = null,
    val localAvailability: LocalNetworkSyncArtifactAvailability? = null,
    val peerAvailability: LocalNetworkSyncArtifactAvailability? = null,
    val autoDownloadAllowed: Boolean? = null
)

enum class LocalNetworkSyncArtifactKind(val rawValue: String) {
    METADATA_JSON("metadataJSON"),
    RECEIVE_JSON("receiveJSON"),
    TRANSCRIPT_MARKDOWN("transcriptMarkdown"),
    TRANSCRIPT_JSON("transcriptJSON"),
    NOTE_MARKDOWN("noteMarkdown"),
    NOTE_JSON("noteJSON"),
    SUMMARY_MARKDOWN("summaryMarkdown"),
    SUMMARY_JSON("summaryJSON"),
    AUDIO("audio");

    val isAutoDownloadAllowed: Boolean
        get() = this != AUDIO

    val objectKind: LocalNetworkSyncObjectKind
        get() = when (this) {
            METADATA_JSON -> LocalNetworkSyncObjectKind.RECORDING_METADATA
            RECEIVE_JSON -> LocalNetworkSyncObjectKind.RECEIVE_RECORD
            TRANSCRIPT_MARKDOWN -> LocalNetworkSyncObjectKind.TRANSCRIPT_MARKDOWN
            TRANSCRIPT_JSON -> LocalNetworkSyncObjectKind.TRANSCRIPT_JSON
            NOTE_MARKDOWN -> LocalNetworkSyncObjectKind.NOTE_MARKDOWN
            NOTE_JSON -> LocalNetworkSyncObjectKind.NOTE_JSON
            SUMMARY_MARKDOWN -> LocalNetworkSyncObjectKind.SUMMARY_MARKDOWN
            SUMMARY_JSON -> LocalNetworkSyncObjectKind.SUMMARY_JSON
            AUDIO -> LocalNetworkSyncObjectKind.RECORDING_AUDIO
        }

    companion object {
        fun from(raw: String?): LocalNetworkSyncArtifactKind {
            val normalized = raw?.trim()?.lowercase() ?: return AUDIO
            return when (normalized) {
                "metadatajson", "metadata_json", "metdatajson", "metadata-json" -> METADATA_JSON
                "receivejson", "receive_json" -> RECEIVE_JSON
                "transcriptmarkdown", "transcript_markdown", "transcript-markdown" -> TRANSCRIPT_MARKDOWN
                "transcriptjson", "transcript_json", "transcript-json" -> TRANSCRIPT_JSON
                "notejson", "note_json" -> NOTE_JSON
                "notemarkdown", "note_markdown", "note-markdown" -> NOTE_MARKDOWN
                "summarymarkdown", "summary_markdown", "summary-markdown" -> SUMMARY_MARKDOWN
                "summaryjson", "summary_json", "summary-json" -> SUMMARY_JSON
                "audio", "recordingaudio" -> AUDIO
                else -> try {
                    valueOf(normalized.replace(" ", "_").replace("-", "_").uppercase())
                } catch (_: Exception) {
                    AUDIO
                }
            }
        }
    }
}

enum class LocalNetworkSyncArtifactAvailability(val rawValue: String) {
    LOCAL("local"),
    AVAILABLE_ON_PEER("availableOnPeer"),
    MISSING("missing"),
    TRANSFERRING("transferring"),
    COMPLETE("complete");
}

enum class LocalNetworkSyncObjectKind(val rawValue: String) {
    RECORDING_AUDIO("recordingAudio"),
    RECORDING_METADATA("recordingMetadata"),
    RECEIVE_RECORD("receiveRecord"),
    TRANSCRIPT_MARKDOWN("transcriptMarkdown"),
    TRANSCRIPT_JSON("transcriptJSON"),
    NOTE_MARKDOWN("noteMarkdown"),
    NOTE_JSON("noteJSON"),
    SUMMARY_MARKDOWN("summaryMarkdown"),
    SUMMARY_JSON("summaryJSON"),
    STUDY_ITEM("studyItem"),
    STUDY_FOLDER("studyFolder");

    companion object {
        fun from(raw: String?): LocalNetworkSyncObjectKind {
            val normalized = raw?.trim()?.lowercase() ?: return RECORDING_METADATA
            return when (normalized) {
                "recordingaudio", "audio" -> RECORDING_AUDIO
                "recordingmetadata", "recording_metadata", "metadata" -> RECORDING_METADATA
                "receiverecord", "receiverecord", "receive", "receiverecord" -> RECEIVE_RECORD
                "transcript", "transcriptmarkdown", "transcript_markdown", "transcript-markdown" -> TRANSCRIPT_MARKDOWN
                "transcriptjson", "transcript_json", "transcript-json" -> TRANSCRIPT_JSON
                "notemarkdown", "note-markdown" -> NOTE_MARKDOWN
                "notejson", "note_json", "note-json" -> NOTE_JSON
                "summary", "summarymarkdown", "summary_markdown", "summary-markdown" -> SUMMARY_MARKDOWN
                "summaryjson", "summary_json", "summary-json" -> SUMMARY_JSON
                "studyitem", "study_item" -> STUDY_ITEM
                "studyfolder", "study_folder" -> STUDY_FOLDER
                else -> try {
                    valueOf(normalized.replace(" ", "_").replace("-", "_").uppercase())
                } catch (_: Exception) {
                    RECORDING_METADATA
                }
            }
        }
    }
}

data class LocalNetworkSyncObjectEntry(
    val objectID: String,
    val objectKind: LocalNetworkSyncObjectKind,
    val ownerID: String,
    val displayTitle: String? = null,
    val fileName: String? = null,
    val logicalName: String? = null,
    val sha256: String? = null,
    val size: Long? = null,
    val updatedAt: Long = System.currentTimeMillis(),
    val deleted: Boolean = false,
    val tombstone: Boolean? = null,
    val sourceDeviceID: String? = null,
    val logicalPathToken: String? = null,
    val availability: LocalNetworkSyncArtifactAvailability = LocalNetworkSyncArtifactAvailability.LOCAL,
    val transferState: String? = null,
    val transferProgress: Double? = null,
    val conflictStatus: String? = null,
    val autoDownloadAllowed: Boolean? = null
)

object LocalNetworkSyncArtifactID {
    private const val PREFIX = "artifact_"
    private const val EXPECTED_LENGTH = 73

    fun make(kind: LocalNetworkSyncArtifactKind, ownerID: String, logicalPathToken: String): String {
        val payload = "${kind.rawValue}|$ownerID|$logicalPathToken"
        return "${PREFIX}${SecureUploadUtilities.sha256Hex(payload.toByteArray(Charsets.UTF_8))}"
    }

    fun validate(artifactID: String) {
        require(artifactID.isNotBlank()) { "invalid_artifact_id" }
        require(artifactID.length == EXPECTED_LENGTH && artifactID.startsWith(PREFIX)) {
            "invalid_artifact_id"
        }
        require(artifactID.drop(PREFIX.length).all { it.isDigit() || it in 'a'..'f' || it in 'A'..'F' }) {
            "invalid_artifact_id"
        }
    }

    fun validateLogicalPathToken(token: String) {
        val normalized = token.trim()
        require(normalized.isNotEmpty()) { "artifact_not_found" }
        require(!normalized.contains("\\")) { "path_traversal" }
        require(!normalized.startsWith("/")) { "absolute_path" }
        val components = normalized.split("/")
        require(!components.contains("..")) { "path_traversal" }
    }

    fun validateLogicalPathToken(token: String, kind: LocalNetworkSyncArtifactKind) {
        validateLogicalPathToken(token)
        val normalized = token.trim().lowercase()
        val isValidForKind = when (kind) {
            LocalNetworkSyncArtifactKind.METADATA_JSON ->
                normalized.endsWith("/metadata.json") || normalized == "metadata.json" ||
                    (normalized.startsWith("metadata/") && normalized.endsWith(".json"))

            LocalNetworkSyncArtifactKind.RECEIVE_JSON ->
                normalized.endsWith("/receive.json") || normalized == "receive.json"

            LocalNetworkSyncArtifactKind.TRANSCRIPT_MARKDOWN ->
                normalized.startsWith("transcripts/") && normalized.endsWith(".md")

            LocalNetworkSyncArtifactKind.TRANSCRIPT_JSON ->
                normalized.startsWith("transcripts/") && normalized.endsWith(".json")

            LocalNetworkSyncArtifactKind.NOTE_MARKDOWN,
            LocalNetworkSyncArtifactKind.SUMMARY_MARKDOWN ->
                normalized.startsWith("notes/") && normalized.endsWith(".md")

            LocalNetworkSyncArtifactKind.NOTE_JSON,
            LocalNetworkSyncArtifactKind.SUMMARY_JSON ->
                normalized.startsWith("notes/") && normalized.endsWith(".json")

            LocalNetworkSyncArtifactKind.AUDIO -> false
        }
        require(isValidForKind) { "unsupported_artifact_kind" }
    }
}

// ── Inventory ────────────────────────────────────────────────────────

    data class LocalNetworkSyncInventory(
    val device: LocalNetworkSyncDeviceSection,
    val schemaVersion: Int = APP_SCHEMA_VERSION,
    val sourceDeviceID: String = device.deviceID,
    val sourcePlatform: LocalNetworkSyncPlatform = device.platform,
    val generatedAt: Long = device.generatedAt,
    val inventoryRevision: String = computeInventoryRevision(
        device,
        recordings = emptyList(),
        folders = emptyList(),
        studyItems = emptyList(),
        artifacts = emptyList(),
        objects = emptyList()
    ),
    val lastKnownPeerRevision: String? = device.lastKnownPeerRevision,
    val recordings: List<LocalNetworkSyncRecordingEntry> = emptyList(),
    val folders: List<LocalNetworkSyncFolderEntry> = emptyList(),
    val studyItems: List<LocalNetworkSyncStudyItemEntry> = emptyList(),
    val artifacts: List<LocalNetworkSyncArtifactEntry> = emptyList(),
    val objects: List<LocalNetworkSyncObjectEntry> = emptyList(),
    val studyManifest: StudyLibrarySyncManifest? = null,
    val canonicalManifest: CanonicalManifest? = null
) {
    companion object {
        const val APP_SCHEMA_VERSION = 1

        private fun computeInventoryRevision(
            device: LocalNetworkSyncDeviceSection,
            recordings: List<LocalNetworkSyncRecordingEntry>,
            folders: List<LocalNetworkSyncFolderEntry>,
            studyItems: List<LocalNetworkSyncStudyItemEntry>,
            artifacts: List<LocalNetworkSyncArtifactEntry>,
            objects: List<LocalNetworkSyncObjectEntry>
        ): String {
            val payload = JSONObject().apply {
                put("deviceID", device.deviceID)
                put("deviceName", device.deviceName)
                put("platform", device.platform.rawValue)
                put("recordingCount", recordings.size)
                put("folderCount", folders.size)
                put("studyItemCount", studyItems.size)
                put("artifactCount", artifacts.size)
                put("objectCount", objects.size)
                put("generatedAt", device.generatedAt)
            }
            return SecureUploadUtilities.sha256Hex(payload.toString().toByteArray(Charsets.UTF_8))
        }

    private fun objectKind(kind: LocalNetworkSyncArtifactKind): LocalNetworkSyncObjectKind = when (kind) {
        LocalNetworkSyncArtifactKind.TRANSCRIPT_MARKDOWN -> LocalNetworkSyncObjectKind.TRANSCRIPT_MARKDOWN
        LocalNetworkSyncArtifactKind.TRANSCRIPT_JSON -> LocalNetworkSyncObjectKind.TRANSCRIPT_JSON
        LocalNetworkSyncArtifactKind.NOTE_MARKDOWN -> LocalNetworkSyncObjectKind.NOTE_MARKDOWN
        LocalNetworkSyncArtifactKind.NOTE_JSON -> LocalNetworkSyncObjectKind.NOTE_JSON
        LocalNetworkSyncArtifactKind.METADATA_JSON -> LocalNetworkSyncObjectKind.RECORDING_METADATA
        LocalNetworkSyncArtifactKind.RECEIVE_JSON -> LocalNetworkSyncObjectKind.RECEIVE_RECORD
        LocalNetworkSyncArtifactKind.SUMMARY_MARKDOWN -> LocalNetworkSyncObjectKind.SUMMARY_MARKDOWN
        LocalNetworkSyncArtifactKind.SUMMARY_JSON -> LocalNetworkSyncObjectKind.SUMMARY_JSON
        LocalNetworkSyncArtifactKind.AUDIO -> LocalNetworkSyncObjectKind.RECORDING_AUDIO
    }
    }

    fun withObjectEntries(
        objects: List<LocalNetworkSyncObjectEntry> = emptyList()
    ): LocalNetworkSyncInventory {
        val canonicalObjects = if (objects.isNotEmpty()) objects else makeObjectEntries()
        return copy(objects = canonicalObjects)
    }

    private fun makeObjectEntries(): List<LocalNetworkSyncObjectEntry> {
        val recordingObjects = recordings.map { rec ->
            LocalNetworkSyncObjectEntry(
                objectID = "recordingMetadata:${rec.recordingID}",
                objectKind = LocalNetworkSyncObjectKind.RECORDING_METADATA,
                ownerID = rec.recordingID,
                displayTitle = rec.title,
                sha256 = rec.metadataHash,
                updatedAt = rec.updatedAt,
                deleted = rec.deleted,
                tombstone = rec.tombstone,
                sourceDeviceID = rec.sourceDeviceID,
                logicalPathToken = null,
                availability = LocalNetworkSyncArtifactAvailability.LOCAL,
                conflictStatus = null,
                autoDownloadAllowed = true
            )
        }
        val recordingAudioObjects = recordings.mapNotNull { rec ->
            if (rec.audioAvailable || rec.audioSize != null || rec.audioChecksum != null) {
                LocalNetworkSyncObjectEntry(
                    objectID = "recordingAudio:${rec.recordingID}",
                    objectKind = LocalNetworkSyncObjectKind.RECORDING_AUDIO,
                    ownerID = rec.recordingID,
                    displayTitle = rec.title,
                    sha256 = rec.audioChecksum,
                    size = rec.audioSize,
                    updatedAt = rec.updatedAt,
                    deleted = rec.deleted,
                    tombstone = rec.tombstone,
                    sourceDeviceID = rec.sourceDeviceID,
                    logicalPathToken = rec.audioLogicalPathToken,
                    availability = rec.audioAvailability,
                    conflictStatus = null,
                    autoDownloadAllowed = false
                )
            } else null
        }

        val folderObjects = folders.map { folder ->
            LocalNetworkSyncObjectEntry(
                objectID = "studyFolder:${folder.folderID}",
                objectKind = LocalNetworkSyncObjectKind.STUDY_FOLDER,
                ownerID = folder.folderID,
                displayTitle = folder.name,
                sha256 = folder.revisionHash,
                updatedAt = folder.updatedAt,
                deleted = folder.deleted,
                tombstone = folder.deleted,
                sourceDeviceID = null,
                logicalPathToken = folder.path,
                availability = LocalNetworkSyncArtifactAvailability.LOCAL,
                conflictStatus = null,
                autoDownloadAllowed = true
            )
        }
        val studyItemObjects = studyItems.map { item ->
            LocalNetworkSyncObjectEntry(
                objectID = "studyItem:${item.itemID}",
                objectKind = LocalNetworkSyncObjectKind.STUDY_ITEM,
                ownerID = item.recordingID ?: item.itemID,
                displayTitle = item.title,
                sha256 = item.revisionHash,
                updatedAt = item.updatedAt,
                deleted = item.deleted,
                tombstone = item.deleted,
                sourceDeviceID = null,
                logicalPathToken = item.path,
                availability = LocalNetworkSyncArtifactAvailability.LOCAL,
                conflictStatus = item.conflictStatus,
                autoDownloadAllowed = true
            )
        }
        val artifactObjects = artifacts.map { artifact ->
            LocalNetworkSyncObjectEntry(
                objectID = artifact.artifactID,
                objectKind = objectKind(artifact.kind),
                ownerID = artifact.ownerID,
                displayTitle = artifact.ownerID,
                fileName = artifact.logicalPathToken?.substringAfterLast('/'),
                logicalName = artifact.logicalPathToken,
                sha256 = artifact.checksum,
                size = artifact.size,
                updatedAt = artifact.updatedAt,
                deleted = false,
                tombstone = false,
                sourceDeviceID = null,
                logicalPathToken = artifact.logicalPathToken,
                availability = artifact.availability,
                conflictStatus = null,
                autoDownloadAllowed = artifact.autoDownloadAllowed ?: artifact.kind.isAutoDownloadAllowed
            )
        }

        return recordingObjects + recordingAudioObjects + folderObjects + studyItemObjects + artifactObjects
    }

    val inventoryHash: String by lazy { computeInventoryHash() }

    private fun computeInventoryHash(): String {
        val gson = GsonBuilder().disableHtmlEscaping().create()
        val payload = JSONObject().apply {
            put("schemaVersion", schemaVersion)
            put("sourceDeviceID", sourceDeviceID)
            put("sourcePlatform", sourcePlatform.rawValue)
            put("generatedAt", generatedAt)
            put("inventoryRevision", inventoryRevision)
            put("lastKnownPeerRevision", lastKnownPeerRevision)
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
            put("objects", JSONArray().apply {
                objects.sortedBy { it.objectID }.forEach { put(JSONObject(gson.toJson(it))) }
            })
            canonicalManifest?.let {
                put("canonicalManifest", JSONObject(gson.toJson(it)))
            }
            studyManifest?.let {
                put("studyManifest", JSONObject(gson.toJson(it)))
            }
        }
        return SecureUploadUtilities.sha256Hex(payload.toString().toByteArray(Charsets.UTF_8))
    }
}

// ── Canonical Compatibility Manifests (iOS bridge)

data class CanonicalManifest(
    val node: CanonicalManifestNode? = null,
    val payload: Map<String, Any?> = emptyMap(),
    val schemaVersion: Int = 1,
    val generatedAt: Long? = null,
    val manifestHash: String? = null
) {
    val stableJson: String by lazy {
        val gson = GsonBuilder().disableHtmlEscaping().create()
        gson.toJson(payload)
    }
}

data class CanonicalManifestNode(
    val nodeID: String? = null,
    val platform: String? = null,
    val displayName: String? = null
)

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
    val localDeviceID: String? = null,
    val peerDeviceID: String? = null,
    val lastSyncStartedAt: Long? = null,
    val lastSyncCompletedAt: Long? = null,
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
    val pendingDownloadCount: Int = 0,
    val lastPlanSummary: String? = null,
    val lastConflictCount: Int? = null,
    val activeTransfers: List<LocalNetworkTransferProgress> = emptyList(),
    val activeSyncRunID: String? = null,
    val controlPlaneState: LocalNetworkSyncControlPlaneState? = null,
    val lastControlPlaneUpdatedAt: Long? = null
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

data class LocalNetworkTransferProgress(
    val objectID: String,
    val objectKind: String,
    val state: LocalNetworkTransferState,
    val progressFraction: Double? = null,
    val receivedBytes: Long? = null,
    val totalBytes: Long? = null,
    val sourceDeviceID: String? = null,
    val statusText: String? = null
) {
    val isVisibleInActionArea: Boolean
        get() = state.isVisibleInActionArea
}
