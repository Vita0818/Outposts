package com.rokurics.app.domain.sync

import android.content.Context
import com.rokurics.app.RokuricsApp
import com.rokurics.app.data.AudioFileStore
import com.rokurics.app.data.ConnectionStore
import com.rokurics.app.data.SecureUploadUtilities
import com.rokurics.app.data.StudyLibraryStore
import com.rokurics.app.domain.model.*
import org.json.JSONObject
import java.io.File

class LocalNetworkSyncInventoryBuilder(
    private val context: Context = RokuricsApp.instance
) {
    private val audioFileStore = AudioFileStore(context)
    private val studyLibraryStore = StudyLibraryStore(context)
    private val connectionStore = ConnectionStore(context)

    fun buildInventory(
        deviceID: String,
        deviceName: String
    ): LocalNetworkSyncInventory {
        val sourceDeviceID = connectionStore.deviceID.ifBlank { deviceID }
        val sourcePlatform = "Android"

        val device = LocalNetworkSyncDeviceSection(
            deviceID = deviceID,
            deviceName = deviceName,
            platform = LocalNetworkSyncPlatform.ANDROID,
            generatedAt = System.currentTimeMillis(),
            appSchemaVersion = LocalNetworkSyncInventory.APP_SCHEMA_VERSION
        )

        val recordings = buildRecordingEntries(sourceDeviceID)
        val folders = buildFolderEntries()
        val studyItems = buildStudyItemEntries()
        val artifacts = buildArtifactEntries(studyItems, recordings)
        val manifest = buildManifest(deviceID, recordings)
        val canonicalObjects = buildCanonicalObjects(
            sourceDeviceID = sourceDeviceID,
            recordings = recordings,
            folders = folders,
            studyItems = studyItems,
            artifacts = artifacts
        )

        return LocalNetworkSyncInventory(
            device = device,
            recordings = recordings,
            folders = folders,
            studyItems = studyItems,
            artifacts = artifacts,
            canonicalManifest = buildCanonicalManifest(
                deviceID = deviceID,
                sourceDeviceID = sourceDeviceID,
                sourcePlatform = sourcePlatform,
                rawStudyManifest = manifest,
                canonicalObjects = canonicalObjects,
                artifacts = artifacts
            ),
            studyManifest = manifest
        ).withObjectEntries(canonicalObjects)
    }

    private fun buildRecordingEntries(sourceDeviceID: String): List<LocalNetworkSyncRecordingEntry> {
        val recordings = audioFileStore.loadAllMetadata(includeDeleted = true)
        return recordings.map { rec ->
            val audioFile = audioFileStore.audioFileFor(rec)
            val audioExists = audioFile.exists()
            val audioAvailability = if (audioExists) {
                LocalNetworkSyncArtifactAvailability.LOCAL
            } else {
                LocalNetworkSyncArtifactAvailability.MISSING
            }

            LocalNetworkSyncRecordingEntry(
                recordingID = rec.id,
                metadataHash = computeRecordingHash(rec),
                audioAvailable = audioExists,
                audioChecksum = if (audioExists) fileChecksum(audioFile) else null,
                audioSize = if (audioExists) audioFile.length() else null,
                title = rec.title,
                createdAt = rec.createdAt.time,
                tombstone = rec.isDeleted,
                audioAvailability = audioAvailability,
                uploadStatus = rec.uploadStatus,
                transcriptionStatus = rec.transcriptionStatus,
                noteStatus = rec.noteStatus,
                sourceDeviceID = sourceDeviceID,
                artifactRefs = listOfNotNull(rec.relativeMetadataPath.takeIf { it.isNotBlank() }),
                audioLogicalPathToken = rec.relativeAudioPath,
                updatedAt = rec.endedAt?.time ?: rec.createdAt.time,
                deleted = rec.isDeleted
            )
        }.sortedBy { it.recordingID }
    }

    private fun buildFolderEntries(): List<LocalNetworkSyncFolderEntry> {
        val folders = studyLibraryStore.allStudyFolders
        return folders.map { f ->
            LocalNetworkSyncFolderEntry(
                folderID = f.folderID,
                parentID = f.parentFolderID,
                path = f.path.displaySummary,
                name = f.name,
                colorToken = f.colorToken?.name,
                updatedAt = f.updatedAt,
                revisionHash = computeFolderHash(f),
                deleted = f.isTrashed
            )
        }.sortedBy { it.folderID }
    }

    private fun buildStudyItemEntries(): List<LocalNetworkSyncStudyItemEntry> {
        val items = studyLibraryStore.allStudyItems
        return items.map { item ->
            LocalNetworkSyncStudyItemEntry(
                itemID = item.itemID,
                kind = item.kind,
                title = item.title,
                folderIDs = item.folderIDs,
                recordingID = item.recordingID,
                updatedAt = item.updatedAt,
                revisionHash = computeItemHash(item),
                deleted = item.isTrashed,
                conflictStatus = item.syncConflictStatus
            )
        }.sortedBy { it.itemID }
    }

    private fun buildArtifactEntries(
        items: List<LocalNetworkSyncStudyItemEntry>,
        recordings: List<LocalNetworkSyncRecordingEntry>
    ): List<LocalNetworkSyncArtifactEntry> {
        val artifacts = mutableListOf<LocalNetworkSyncArtifactEntry>()

        for (item in studyLibraryStore.allStudyItems) {
            // Transcript markdown
            if (item.transcriptMarkdownRelativePath != null) {
                val file = File(studyDir(), item.transcriptMarkdownRelativePath)
                artifacts.add(buildArtifact(
                    kind = LocalNetworkSyncArtifactKind.TRANSCRIPT_MARKDOWN,
                    ownerID = item.itemID,
                    itemID = item.itemID,
                    relativePath = item.transcriptMarkdownRelativePath,
                    fallbackUpdatedAt = item.updatedAt,
                    file = file
                ))
            }
            // Transcript JSON
            if (item.transcriptRelativePath != null) {
                val file = File(studyDir(), item.transcriptRelativePath)
                artifacts.add(buildArtifact(
                    kind = LocalNetworkSyncArtifactKind.TRANSCRIPT_JSON,
                    ownerID = item.itemID,
                    itemID = item.itemID,
                    relativePath = item.transcriptRelativePath,
                    fallbackUpdatedAt = item.updatedAt,
                    file = file
                ))
            }
            // Receive
            if (item.receiveRelativePath != null) {
                val file = File(studyDir(), item.receiveRelativePath)
                artifacts.add(buildArtifact(
                    kind = LocalNetworkSyncArtifactKind.RECEIVE_JSON,
                    ownerID = item.itemID,
                    itemID = item.itemID,
                    relativePath = item.receiveRelativePath,
                    fallbackUpdatedAt = item.updatedAt,
                    file = file
                ))
            }
            // Note markdown
            if (item.noteRelativePath != null) {
                val file = File(studyDir(), item.noteRelativePath)
                val kind = if (item.noteRelativePath.endsWith(".json", ignoreCase = true)) {
                    LocalNetworkSyncArtifactKind.NOTE_JSON
                } else {
                    LocalNetworkSyncArtifactKind.NOTE_MARKDOWN
                }
                artifacts.add(buildArtifact(
                    kind = kind,
                    ownerID = item.itemID,
                    itemID = item.itemID,
                    relativePath = item.noteRelativePath,
                    fallbackUpdatedAt = item.updatedAt,
                    file = file
                ))
            }
            // Summary markdown
            if (item.summaryMarkdownRelativePath != null) {
                val file = File(studyDir(), item.summaryMarkdownRelativePath)
                artifacts.add(buildArtifact(
                    kind = LocalNetworkSyncArtifactKind.SUMMARY_MARKDOWN,
                    ownerID = item.itemID,
                    itemID = item.itemID,
                    relativePath = item.summaryMarkdownRelativePath,
                    fallbackUpdatedAt = item.updatedAt,
                    file = file
                ))
            }
            // Summary JSON
            if (item.summaryJSONRelativePath != null) {
                val file = File(studyDir(), item.summaryJSONRelativePath)
                artifacts.add(buildArtifact(
                    kind = LocalNetworkSyncArtifactKind.SUMMARY_JSON,
                    ownerID = item.itemID,
                    itemID = item.itemID,
                    relativePath = item.summaryJSONRelativePath,
                    fallbackUpdatedAt = item.updatedAt,
                    file = file
                ))
            }
            // Audio
            if (item.audioRelativePath != null) {
                val file = File(studyDir(), item.audioRelativePath)
                artifacts.add(buildArtifact(
                    kind = LocalNetworkSyncArtifactKind.AUDIO,
                    ownerID = item.itemID,
                    itemID = item.itemID,
                    relativePath = item.audioRelativePath,
                    fallbackUpdatedAt = item.updatedAt,
                    file = file
                ))
            }
        }
        return artifacts.sortedBy { it.artifactID }
    }

    private fun buildArtifact(
        kind: LocalNetworkSyncArtifactKind,
        ownerID: String,
        itemID: String,
        relativePath: String,
        fallbackUpdatedAt: Long,
        file: File
    ): LocalNetworkSyncArtifactEntry {
        val exists = file.exists()
        return LocalNetworkSyncArtifactEntry(
            artifactID = LocalNetworkSyncArtifactID.make(
                kind = kind,
                ownerID = ownerID,
                logicalPathToken = relativePath
            ),
            kind = kind,
            ownerID = itemID,
            checksum = if (exists) fileChecksum(file) else null,
            size = if (exists) file.length() else null,
            updatedAt = fallbackUpdatedAt,
            availability = if (exists) LocalNetworkSyncArtifactAvailability.LOCAL else LocalNetworkSyncArtifactAvailability.MISSING,
            logicalPathToken = relativePath
        )
    }

    private fun buildManifest(deviceID: String, recordings: List<LocalNetworkSyncRecordingEntry>): StudyLibrarySyncManifest {
        return StudyLibrarySyncManifest(
            deviceID = deviceID,
            generatedAt = System.currentTimeMillis(),
            recordings = recordings,
            items = studyLibraryStore.allStudyItems.sortedBy { it.itemID },
            folders = studyLibraryStore.allStudyFolders.sortedBy { it.folderID }
        )
    }

    private fun buildCanonicalObjects(
        sourceDeviceID: String,
        recordings: List<LocalNetworkSyncRecordingEntry>,
        folders: List<LocalNetworkSyncFolderEntry>,
        studyItems: List<LocalNetworkSyncStudyItemEntry>,
        artifacts: List<LocalNetworkSyncArtifactEntry>
    ): List<LocalNetworkSyncObjectEntry> {
        val objects = mutableListOf<LocalNetworkSyncObjectEntry>()

        recordings.forEach { rec ->
            objects.add(
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
                    autoDownloadAllowed = true
                )
            )
            objects.add(
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
                    availability = rec.audioAvailability
                        ?: if (rec.audioAvailable) LocalNetworkSyncArtifactAvailability.LOCAL else LocalNetworkSyncArtifactAvailability.MISSING,
                    conflictStatus = null,
                    autoDownloadAllowed = false
                )
            )
        }

        folders.forEach { folder ->
            objects.add(
                LocalNetworkSyncObjectEntry(
                    objectID = "studyFolder:${folder.folderID}",
                    objectKind = LocalNetworkSyncObjectKind.STUDY_FOLDER,
                    ownerID = folder.folderID,
                    displayTitle = folder.name,
                    sha256 = folder.revisionHash,
                    updatedAt = folder.updatedAt,
                    deleted = folder.deleted,
                    tombstone = folder.deleted,
                    sourceDeviceID = sourceDeviceID,
                    logicalPathToken = folder.path,
                    availability = LocalNetworkSyncArtifactAvailability.LOCAL,
                    autoDownloadAllowed = true
                )
            )
        }

        studyItems.forEach { item ->
            objects.add(
                LocalNetworkSyncObjectEntry(
                    objectID = "studyItem:${item.itemID}",
                    objectKind = LocalNetworkSyncObjectKind.STUDY_ITEM,
                    ownerID = item.recordingID ?: item.itemID,
                    displayTitle = item.title,
                    sha256 = item.revisionHash,
                    updatedAt = item.updatedAt,
                    deleted = item.deleted,
                    tombstone = item.deleted,
                    sourceDeviceID = sourceDeviceID,
                    logicalPathToken = item.itemID,
                    availability = LocalNetworkSyncArtifactAvailability.LOCAL,
                    conflictStatus = item.conflictStatus,
                    autoDownloadAllowed = true
                )
            )
        }

        artifacts.forEach { artifact ->
            objects.add(
                LocalNetworkSyncObjectEntry(
                    objectID = artifact.artifactID,
                    objectKind = artifactObjectKind(artifact.kind),
                    ownerID = artifact.ownerID,
                    displayTitle = artifact.ownerID,
                    sha256 = artifact.checksum,
                    size = artifact.size,
                    updatedAt = artifact.updatedAt,
                    deleted = false,
                    tombstone = false,
                    sourceDeviceID = sourceDeviceID,
                    logicalPathToken = artifact.logicalPathToken,
                    availability = artifact.availability,
                    autoDownloadAllowed = artifact.autoDownloadAllowed ?: artifact.kind.isAutoDownloadAllowed
                )
            )
        }

        return objects.sortedBy { it.objectID }
    }

    private fun buildCanonicalManifest(
        deviceID: String,
        sourceDeviceID: String,
        sourcePlatform: String,
        rawStudyManifest: StudyLibrarySyncManifest,
        canonicalObjects: List<LocalNetworkSyncObjectEntry>,
        artifacts: List<LocalNetworkSyncArtifactEntry>
    ): com.rokurics.app.domain.model.CanonicalManifest {
        val payload = linkedMapOf<String, Any?>(
            "schemaVersion" to 1,
            "sourceDeviceID" to sourceDeviceID,
            "sourcePlatform" to sourcePlatform,
            "node" to linkedMapOf(
                "nodeID" to deviceID,
                "platform" to sourcePlatform,
                "displayName" to "Android"
            ),
            "generatedAt" to System.currentTimeMillis(),
            "objects" to canonicalObjects.map { obj ->
                linkedMapOf(
                    "objectID" to obj.objectID,
                    "objectKind" to obj.objectKind.rawValue,
                    "ownerID" to obj.ownerID,
                    "displayTitle" to obj.displayTitle,
                    "sha256" to obj.sha256,
                    "size" to obj.size,
                    "updatedAt" to obj.updatedAt,
                    "deleted" to obj.deleted,
                    "tombstone" to obj.tombstone,
                    "sourceDeviceID" to obj.sourceDeviceID,
                    "logicalPathToken" to obj.logicalPathToken,
                    "availability" to obj.availability.rawValue,
                    "transferState" to obj.transferState,
                    "transferProgress" to obj.transferProgress,
                    "conflictStatus" to obj.conflictStatus,
                    "autoDownloadAllowed" to obj.autoDownloadAllowed
                )
            },
            "objectCount" to canonicalObjects.size,
            "studyManifestRecordCount" to rawStudyManifest.items.size,
            "studyManifestFolderCount" to rawStudyManifest.folders.size,
            "tombstoneCount" to rawStudyManifest.tombstones.size,
            "pendingUploadCount" to rawStudyManifest.pendingUploads.size,
            "artifacts" to artifacts.map {
                linkedMapOf(
                    "artifactID" to it.artifactID,
                    "kind" to it.kind.rawValue,
                    "ownerID" to it.ownerID,
                    "sha256" to it.checksum,
                    "updatedAt" to it.updatedAt,
                    "size" to it.size,
                    "availability" to it.availability.rawValue,
                    "logicalPathToken" to it.logicalPathToken
                )
            }
        )

        return com.rokurics.app.domain.model.CanonicalManifest(
            node = com.rokurics.app.domain.model.CanonicalManifestNode(
                nodeID = deviceID,
                platform = sourcePlatform,
                displayName = "Android"
            ),
            payload = payload,
            schemaVersion = 1,
            generatedAt = System.currentTimeMillis()
        )
    }

    // ── Hash helpers ─────────────────────────────────────────────────

    private fun studyDir(): File = File(context.filesDir, "Rokurics/study")

    private fun fileChecksum(file: File): String = SecureUploadUtilities.sha256Hex(file)

    private fun computeRecordingHash(rec: com.rokurics.app.domain.model.RecordingMetadata): String {
        val json = rec.toJson()
        return SecureUploadUtilities.sha256Hex(json.toString().toByteArray(Charsets.UTF_8))
    }

    private fun computeFolderHash(folder: StudyFolderMetadata): String {
        val json = JSONObject().apply {
            put("folderID", folder.folderID)
            put("name", folder.name)
            put("level", folder.level.name)
            put("updatedAt", folder.updatedAt)
        }
        return SecureUploadUtilities.sha256Hex(json.toString().toByteArray(Charsets.UTF_8))
    }

    private fun computeItemHash(item: StudyItemMetadata): String {
        val json = JSONObject().apply {
            put("itemID", item.itemID)
            put("title", item.title)
            put("kind", item.kind.name)
            put("updatedAt", item.updatedAt)
            put("conflictStatus", item.syncConflictStatus)
        }
        return SecureUploadUtilities.sha256Hex(json.toString().toByteArray(Charsets.UTF_8))
    }

    private fun artifactObjectKind(kind: LocalNetworkSyncArtifactKind): LocalNetworkSyncObjectKind =
        kind.objectKind
}
