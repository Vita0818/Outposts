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
        val device = LocalNetworkSyncDeviceSection(
            deviceID = deviceID,
            deviceName = deviceName,
            platform = LocalNetworkSyncPlatform.ANDROID,
            generatedAt = System.currentTimeMillis(),
            appSchemaVersion = LocalNetworkSyncInventory.APP_SCHEMA_VERSION
        )

        val recordings = buildRecordingEntries()
        val folders = buildFolderEntries()
        val studyItems = buildStudyItemEntries()
        val artifacts = buildArtifactEntries(studyItems, recordings)
        val manifest = buildManifest(deviceID)

        return LocalNetworkSyncInventory(
            device = device,
            recordings = recordings,
            folders = folders,
            studyItems = studyItems,
            artifacts = artifacts,
            studyManifest = manifest
        )
    }

    private fun buildRecordingEntries(): List<LocalNetworkSyncRecordingEntry> {
        val recordings = audioFileStore.loadAllMetadata()
        return recordings.map { rec ->
            val audioFile = audioFileStore.audioFileFor(rec)
            LocalNetworkSyncRecordingEntry(
                recordingID = rec.id,
                metadataHash = computeRecordingHash(rec),
                audioAvailable = audioFile.exists(),
                audioChecksum = if (audioFile.exists()) fileChecksum(audioFile) else null,
                audioSize = if (audioFile.exists()) audioFile.length() else null,
                updatedAt = rec.endedAt?.time ?: rec.createdAt.time,
                deleted = rec.isDeleted
            )
        }.sortedBy { it.recordingID }
    }

    private fun buildFolderEntries(): List<LocalNetworkSyncFolderEntry> {
        val folders = studyLibraryStore.allStudyFolders()
        return folders.map { f ->
            LocalNetworkSyncFolderEntry(
                folderID = f.folderID,
                parentID = f.parentFolderID,
                path = f.path.displaySummary,
                name = f.name,
                colorToken = f.colorToken.name,
                updatedAt = f.updatedAt,
                revisionHash = computeFolderHash(f),
                deleted = f.isTrashed
            )
        }.sortedBy { it.folderID }
    }

    private fun buildStudyItemEntries(): List<LocalNetworkSyncStudyItemEntry> {
        val items = studyLibraryStore.allStudyItems()
        return items.map { item ->
            LocalNetworkSyncStudyItemEntry(
                itemID = item.itemID,
                kind = item.kind,
                title = item.title,
                folderIDs = item.folderIDs,
                recordingID = item.recordingID,
                updatedAt = item.updatedAt,
                revisionHash = computeItemHash(item),
                deleted = item.isTrashed
            )
        }.sortedBy { it.itemID }
    }

    private fun buildArtifactEntries(
        items: List<LocalNetworkSyncStudyItemEntry>,
        recordings: List<LocalNetworkSyncRecordingEntry>
    ): List<LocalNetworkSyncArtifactEntry> {
        val artifacts = mutableListOf<LocalNetworkSyncArtifactEntry>()
        val allItems = studyLibraryStore.allStudyItems()

        for (item in allItems) {
            // Transcript markdown
            if (item.transcriptMarkdownRelativePath != null) {
                val file = File(studyDir(), item.transcriptMarkdownRelativePath)
                artifacts.add(LocalNetworkSyncArtifactEntry(
                    artifactID = computeArtifactID("transcriptMarkdown", item.itemID, item.transcriptMarkdownRelativePath),
                    kind = LocalNetworkSyncArtifactKind.TRANSCRIPT_MARKDOWN,
                    ownerID = item.itemID,
                    checksum = if (file.exists()) fileChecksum(file) else null,
                    size = if (file.exists()) file.length() else null,
                    updatedAt = item.updatedAt,
                    availability = if (file.exists()) LocalNetworkSyncArtifactAvailability.LOCAL
                    else LocalNetworkSyncArtifactAvailability.MISSING,
                    logicalPathToken = item.transcriptMarkdownRelativePath
                ))
            }
            // Transcript JSON
            if (item.transcriptRelativePath != null) {
                val file = File(studyDir(), item.transcriptRelativePath)
                artifacts.add(LocalNetworkSyncArtifactEntry(
                    artifactID = computeArtifactID("transcriptJSON", item.itemID, item.transcriptRelativePath),
                    kind = LocalNetworkSyncArtifactKind.TRANSCRIPT_JSON,
                    ownerID = item.itemID,
                    checksum = if (file.exists()) fileChecksum(file) else null,
                    size = if (file.exists()) file.length() else null,
                    updatedAt = item.updatedAt,
                    availability = if (file.exists()) LocalNetworkSyncArtifactAvailability.LOCAL
                    else LocalNetworkSyncArtifactAvailability.MISSING,
                    logicalPathToken = item.transcriptRelativePath
                ))
            }
            // Note markdown
            if (item.noteRelativePath != null) {
                val file = File(studyDir(), item.noteRelativePath)
                artifacts.add(LocalNetworkSyncArtifactEntry(
                    artifactID = computeArtifactID("noteMarkdown", item.itemID, item.noteRelativePath),
                    kind = LocalNetworkSyncArtifactKind.NOTE_MARKDOWN,
                    ownerID = item.itemID,
                    checksum = if (file.exists()) fileChecksum(file) else null,
                    size = if (file.exists()) file.length() else null,
                    updatedAt = item.updatedAt,
                    availability = if (file.exists()) LocalNetworkSyncArtifactAvailability.LOCAL
                    else LocalNetworkSyncArtifactAvailability.MISSING,
                    logicalPathToken = item.noteRelativePath
                ))
            }
            // Audio
            if (item.audioRelativePath != null) {
                val file = File(studyDir(), item.audioRelativePath)
                artifacts.add(LocalNetworkSyncArtifactEntry(
                    artifactID = computeArtifactID("audio", item.itemID, item.audioRelativePath),
                    kind = LocalNetworkSyncArtifactKind.AUDIO,
                    ownerID = item.itemID,
                    checksum = if (file.exists()) fileChecksum(file) else null,
                    size = if (file.exists()) file.length() else null,
                    updatedAt = item.updatedAt,
                    availability = if (file.exists()) LocalNetworkSyncArtifactAvailability.LOCAL
                    else LocalNetworkSyncArtifactAvailability.MISSING,
                    logicalPathToken = item.audioRelativePath
                ))
            }
        }
        return artifacts.sortedBy { it.artifactID }
    }

    private fun buildManifest(deviceID: String): StudyLibrarySyncManifest {
        return StudyLibrarySyncManifest(
            deviceID = deviceID,
            generatedAt = System.currentTimeMillis(),
            items = studyLibraryStore.allStudyItems().sortedBy { it.itemID },
            folders = studyLibraryStore.allStudyFolders().sortedBy { it.folderID }
        )
    }

    // ── Hash helpers ─────────────────────────────────────────────────

    private fun studyDir(): File {
        return File(context.filesDir, "Rokurics/study")
    }

    private fun fileChecksum(file: File): String {
        return SecureUploadUtilities.sha256Hex(file)
    }

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
        }
        return SecureUploadUtilities.sha256Hex(json.toString().toByteArray(Charsets.UTF_8))
    }

    private fun computeArtifactID(kindRaw: String, ownerID: String, pathToken: String): String {
        val payload = "$kindRaw|$ownerID|$pathToken"
        return "artifact_${SecureUploadUtilities.sha256Hex(payload.toByteArray(Charsets.UTF_8))}"
    }
}
