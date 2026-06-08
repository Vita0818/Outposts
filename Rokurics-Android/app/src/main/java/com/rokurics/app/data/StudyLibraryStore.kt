package com.rokurics.app.data

import android.content.Context
import com.google.gson.GsonBuilder
import com.google.gson.JsonParser
import com.rokurics.app.RokuricsApp
import com.rokurics.app.data.SecureUploadUtilities
import com.rokurics.app.domain.model.PendingRecordingUpload
import com.rokurics.app.domain.model.PendingRecordingUploadStatus
import com.rokurics.app.domain.model.RecordingMetadata
import com.rokurics.app.domain.model.RecordingReceiveRecord
import com.rokurics.app.domain.model.LocalNetworkSyncRecordingEntry
import com.rokurics.app.domain.model.StudyBrowsePath
import com.rokurics.app.domain.model.StudyFilingCandidates
import com.rokurics.app.domain.model.StudyFilingPath
import com.rokurics.app.domain.model.StudyFolderColorToken
import com.rokurics.app.domain.model.StudyFolderLevel
import com.rokurics.app.domain.model.StudyFolderMetadata
import com.rokurics.app.domain.model.StudyHierarchyRule
import com.rokurics.app.domain.model.StudyItemID
import com.rokurics.app.domain.model.StudyItemKind
import com.rokurics.app.domain.model.StudyItemMetadata
import com.rokurics.app.domain.model.StudyLibrarySyncApplyResult
import com.rokurics.app.domain.model.StudyLibrarySyncEntityKind
import com.rokurics.app.domain.model.StudyLibrarySyncManifest
import com.rokurics.app.domain.model.StudyLibrarySyncOperation
import com.rokurics.app.domain.model.StudyLibrarySyncTombstone
import com.rokurics.app.domain.model.StudyPathSanitizer
import com.rokurics.app.domain.model.StudyTag
import com.rokurics.app.domain.model.StudyTagList
import org.json.JSONObject
import java.io.File
import java.util.Date

class StudyLibraryStoreError(message: String) : Exception(message) {
    companion object {
        val UNABLE_TO_CREATE_DIRECTORY = StudyLibraryStoreError("study_directory_unavailable")
        val UNSAFE_DESTINATION = StudyLibraryStoreError("unsafe_study_destination")
        val ITEM_MISSING = StudyLibraryStoreError("study_item_missing")
        val FOLDER_MISSING = StudyLibraryStoreError("study_folder_missing")
        val UNSUPPORTED_FOLDER_LEVEL = StudyLibraryStoreError("study_folder_level_unsupported")
        fun writeFailed(reason: String) = StudyLibraryStoreError(reason)
    }
}

data class StudyMetadataIndex(
    val itemMetadataFilesByItemID: MutableMap<String, String> = mutableMapOf(),
    val itemMetadataFilesByRecordingID: MutableMap<String, String> = mutableMapOf(),
    val folderMetadataFilesByFolderID: MutableMap<String, String> = mutableMapOf(),
    var updatedAt: Long = 0L
)

class StudyLibraryStore(
    private val context: Context = RokuricsApp.instance,
    private val audioFileStore: AudioFileStore? = null
) {
    // -- JSON codec (matching iOS: ISO8601 dates, pretty-print, sorted keys) --
    private val jsonEncoder = GsonBuilder()
        .setPrettyPrinting()
        .disableHtmlEscaping()
        .create()

    // -- Directory layout --
    private val rootDir: File by lazy {
        if (context.filesDir != null) {
            File(context.filesDir, "Rokurics")
        } else {
            File(System.getProperty("java.io.tmpdir") ?: "/tmp", "Rokurics")
        }
    }

    val studyDir: File get() = File(rootDir, "study")
    val itemMetadataDir: File get() = File(studyDir, "items")
    val folderMetadataDir: File get() = File(studyDir, "folders")
    val indexFile: File get() = File(studyDir, "index.json")
    val hierarchyRulesFile: File get() = File(studyDir, "hierarchy-rules.json")
    val legacyItemMetadataDir: File get() = File(studyDir, "item-metadata")
    val legacyIndexFile: File get() = File(studyDir, "study-index.json")

    private val resolvedAudioFileStore: AudioFileStore by lazy {
        audioFileStore ?: AudioFileStore(context)
    }

    // -- State --
    var allStudyItems: List<StudyItemMetadata> = emptyList()
        private set
    var allStudyFolders: List<StudyFolderMetadata> = emptyList()
        private set
    var hierarchyRules: List<StudyHierarchyRule> = listOf(StudyHierarchyRule.defaultCourseView)
        private set
    var selectedHierarchyRule: StudyHierarchyRule = StudyHierarchyRule.defaultCourseView
        private set
    var filingCandidates: StudyFilingCandidates = StudyFilingCandidates.empty
        private set

    val libraryRootDir: File get() = rootDir

    val studyRootDisplayPath: String get() = studyDir.absolutePath

    init {
        ensureStudyDirectories()
        hierarchyRules = loadHierarchyRules()
        selectedHierarchyRule = hierarchyRules.firstOrNull() ?: StudyHierarchyRule.defaultCourseView
        refresh()
    }

    // ── Public API ────────────────────────────────────────────────────────

    fun refresh() {
        val recordings = resolvedAudioFileStore.loadAllMetadata()
        val storedItems = loadAllStoredItemMetadata()
        val receiveItems = loadReceiveRecordDerivedItems()

        val storedItemsByRecordingID = mutableMapOf<String, StudyItemMetadata>()
        for (item in storedItems) {
            item.recordingID?.let { storedItemsByRecordingID[it] = item }
        }
        val liveRecordingIDs = recordings.map { it.id }.toSet()

        val itemsByID = mutableMapOf<StudyItemID, StudyItemMetadata>()
        for (recording in recordings) {
            val fallback = StudyItemMetadata.defaultMetadata(recording)
            val metadata = storedItemsByRecordingID[recording.id]?.mergedWithCurrentRecording(recording)
                ?: fallback
            itemsByID[metadata.itemID] = metadata
        }

        for (item in receiveItems) {
            val recordingID = item.recordingID
            if (recordingID == null || recordingID !in liveRecordingIDs) {
                itemsByID[item.itemID] = item
            }
        }

        for (item in storedItems) {
            if (shouldIncludeStoredItem(item, liveRecordingIDs, itemsByID)) {
                itemsByID[item.itemID] = item
            }
        }

        val items = itemsByID.values.sortedWith(
            compareByDescending<StudyItemMetadata> { it.createdAt }.thenBy { it.title }
        )
        val folders = repairedFolders(
            loadAllFolderMetadata().filter { !it.isTrashed },
            items
        )

        allStudyItems = items
        allStudyFolders = folders
        filingCandidates = StudyFilingCandidates.collectFrom(items)
    }

    fun itemByRecordingID(recordingID: String): StudyItemMetadata? =
        allStudyItems.find { it.recordingID == recordingID }

    fun itemByItemID(itemID: StudyItemID): StudyItemMetadata? =
        allStudyItems.find { it.itemID == itemID || it.recordingID == itemID }

    /** Generate a sync manifest with checksum, tombstones, and pending uploads. */
    fun makeSyncManifest(deviceID: String, generatedAt: Long = System.currentTimeMillis()): StudyLibrarySyncManifest {
        refresh()

        val itemsByID = mutableMapOf<String, StudyItemMetadata>()
        for (item in loadAllStoredItemMetadata() + allStudyItems) {
            itemsByID[item.itemID] = item
        }

        val recordings = resolvedAudioFileStore.loadAllMetadata(includeDeleted = true)
        for (recording in recordings) {
            val fallback = StudyItemMetadata.defaultMetadata(recording)
            val metadata = loadStoredMetadata(recordingID = recording.id)?.mergedWithCurrentRecording(recording)
                ?: fallback
            itemsByID[metadata.itemID] = metadata
        }

        val foldersByID = mutableMapOf<String, StudyFolderMetadata>()
        for (folder in loadAllFolderMetadata() + allStudyFolders) {
            foldersByID[folder.folderID] = folder
        }

        val items = itemsByID.values.map { it.syncSanitized(deviceID) }
        val folders = foldersByID.values.map { it.syncSanitized(deviceID) }
        val tombstones = makeSyncTombstones(items, folders, deviceID)
        val pendingUploads = makePendingRecordingUploads(recordings, itemsByID, deviceID)
        val recordingEntries = makeSyncRecordings(recordings)

        return makeManifestWithChecksum(
            deviceID = deviceID,
            generatedAt = generatedAt,
            items = items,
            folders = folders,
            recordings = recordingEntries,
            tombstones = tombstones,
            pendingUploads = pendingUploads
        )
    }

    @Throws(StudyLibraryStoreError::class)
    fun applySyncManifest(manifest: StudyLibrarySyncManifest, localDeviceID: String): StudyLibrarySyncApplyResult {
        if (!hasValidChecksum(manifest)) {
            throw StudyLibraryStoreError.writeFailed("sync_manifest_checksum_mismatch")
        }

        val result = StudyLibrarySyncApplyResult()

        for (incomingFolder in manifest.folders) {
            try {
                var remote = incomingFolder.syncSanitized(manifest.deviceID)
                val existing = loadStoredFolder(remote.folderID)
                    ?: allStudyFolders.find { it.folderID == remote.folderID }
                val merged = mergedSyncFolder(existing, remote, result) ?: continue
                save(merged)
                result.appliedFolderCount++
            } catch (_: Exception) {
                result.failedChanges++
            }
        }

        for (incomingItem in manifest.items) {
            try {
                var remote = incomingItem.syncSanitized(manifest.deviceID)
                markSyncMetadataOnlyIfNeeded(remote)
                val existing = editableMetadataByItemID(remote.itemID)
                val merged = mergedSyncItem(existing, remote, result) ?: continue
                save(merged)
                applySyncItemToRecordingMetadata(merged)
                result.appliedItemCount++
            } catch (_: Exception) {
                result.failedChanges++
            }
        }

        for (tombstone in manifest.tombstones) {
            try {
                if (applySyncTombstone(tombstone, manifest.deviceID)) {
                    result.tombstoneCount++
                }
            } catch (_: Exception) {
                result.failedChanges++
            }
        }

        refresh()
        return result
    }

    /** Sync study items from recording metadata list (kept for backwards compat). */
    fun syncFromRecordings(recordings: List<RecordingMetadata>) {
        for (rec in recordings) {
            try {
                upsertRecordingMetadata(rec)
            } catch (_: Exception) {
                // skip individual failures
            }
        }
    }

    @Throws(StudyLibraryStoreError::class)
    fun upsertRecordingMetadata(recording: RecordingMetadata): StudyItemMetadata {
        val previous = editableMetadataIfAvailable(recordingID = recording.id)
        val fallback = StudyItemMetadata.defaultMetadata(recording)
        val metadata = previous?.mergedWithCurrentRecording(recording) ?: fallback
        save(metadata, previous)
        refresh()
        return metadata
    }

    @Throws(StudyLibraryStoreError::class)
    fun updateFiling(recordingID: String, studyFiling: StudyFilingPath?) {
        val previous = editableMetadata(recordingID)
        var metadata = previous
        val filing = if (studyFiling?.isEmpty != false) StudyFilingPath() else studyFiling
        val newFolderIDs = StudyItemMetadata.defaultFolderIDsFor(filing)
        metadata = metadata.copy(
            filing = filing,
            folderIDs = newFolderIDs,
            updatedAt = System.currentTimeMillis()
        )
        save(metadata, previous)
        refresh()
    }

    fun folder(folderID: String): StudyFolderMetadata? =
        loadStoredFolder(folderID) ?: allStudyFolders.find { it.folderID == folderID }

    @Throws(StudyLibraryStoreError::class)
    fun createFolder(name: String, path: StudyBrowsePath): StudyFolderMetadata {
        val level = StudyFolderMetadata.levelForDepth(path.depth)
            ?: throw StudyLibraryStoreError.UNSUPPORTED_FOLDER_LEVEL

        val normalizedName = StudyItemMetadata.normalized(name) ?: StudyHierarchyRule.missingValue
        val components = path.components + normalizedName
        val filing = StudyFolderLevel.filingPathFor(components)
        val parentID = parentFolderID(path)
        val folder = StudyFolderMetadata(
            folderID = StudyFolderMetadata.folderIDFor(level, filing),
            name = normalizedName,
            level = level,
            path = filing,
            parentFolderID = parentID,
            childFolderIDs = emptyList(),
            itemIDs = emptyList()
        )

        save(folder)
        if (parentID != null) {
            appendChildFolderID(folder.folderID, parentID, path)
        }
        refresh()
        return folder
    }

    @Throws(StudyLibraryStoreError::class)
    fun renameFolder(folderID: String, rawName: String): StudyFolderMetadata {
        var folder = this.folder(folderID)
            ?: throw StudyLibraryStoreError.FOLDER_MISSING

        val name = StudyItemMetadata.normalized(rawName) ?: return folder
        if (folder.name == name) return folder

        // Check for duplicate at same level under same parent
        val storedFolders = loadAllFolderMetadata()
        val duplicateExists = storedFolders.any { candidate ->
            candidate.folderID != folder.folderID
                && candidate.parentFolderID == folder.parentFolderID
                && candidate.level == folder.level
                && candidate.name.equals(name, ignoreCase = true)
        }
        if (duplicateExists) {
            throw StudyLibraryStoreError.writeFailed("study_folder_duplicate_name")
        }

        val oldPath = folder.path
        val oldPathComponents = folder.pathComponents
        val updatedAt = System.currentTimeMillis()
        val foldersToSave = mutableListOf<StudyFolderMetadata>()

        for (candidate in storedFolders) {
            if (!pathComponentsStartWith(candidate.pathComponents, oldPathComponents)) continue
            val updatedPath = renamedPath(candidate.path, folder.level, name) ?: continue

            var updated = candidate
            if (updated.folderID == folder.folderID) {
                updated = updated.copy(name = name)
            }
            updated = updated.copy(path = updatedPath, updatedAt = updatedAt)
            foldersToSave.add(updated)
            if (updated.folderID == folder.folderID) {
                folder = updated
            }
        }

        if (foldersToSave.none { it.folderID == folder.folderID }) {
            folder = folder.copy(
                name = name,
                path = renamedPath(folder.path, folder.level, name) ?: folder.path,
                updatedAt = updatedAt
            )
            foldersToSave.add(folder)
        }

        // Cascade filing path update to matching items
        val itemsByID = mutableMapOf<String, StudyItemMetadata>()
        for (item in loadAllStoredItemMetadata() + allStudyItems) {
            itemsByID[item.itemID] = item
        }
        for (candidate in itemsByID.values) {
            if (itemMatches(candidate, oldPath, folder.level)) {
                var updated = candidate
                val newFiling = renamedPath(candidate.filing, folder.level, name)
                if (newFiling != null) {
                    updated = updated.copy(filing = newFiling, updatedAt = updatedAt)
                    writeItemMetadataPreservingFolderLinks(updated)
                }
            }
        }

        for (updatedFolder in foldersToSave) {
            save(updatedFolder)
        }

        refresh()
        return folder
    }

    /** Rename by path+level (creates folder if it doesn't exist yet). */
    @Throws(StudyLibraryStoreError::class)
    fun renameFolder(path: StudyBrowsePath, level: StudyFolderLevel, rawName: String): StudyFolderMetadata {
        val filing = StudyFolderLevel.filingPathFor(path.components)
        val folderID = StudyFolderMetadata.folderIDFor(level, filing)
        if (loadStoredFolder(folderID) == null) {
            val items = allStudyItems
                .filter { itemMatches(it, filing, level) }
                .map { it.itemID }
            val folder = StudyFolderMetadata(
                folderID = folderID,
                name = path.components.lastOrNull() ?: StudyHierarchyRule.missingValue,
                level = level,
                path = filing,
                parentFolderID = parentFolderID(path.parent),
                itemIDs = items
            )
            save(folder)
        }
        return renameFolder(folderID, rawName)
    }

    @Throws(StudyLibraryStoreError::class)
    fun setFolderColor(folderID: String, colorToken: StudyFolderColorToken?): StudyFolderMetadata {
        var folder = this.folder(folderID)
            ?: throw StudyLibraryStoreError.FOLDER_MISSING
        folder = folder.copy(
            colorToken = if (colorToken == StudyFolderColorToken.DEFAULT) null else colorToken,
            updatedAt = System.currentTimeMillis()
        )
        save(folder)
        refresh()
        return folder
    }

    @Throws(StudyLibraryStoreError::class)
    fun moveFolderToTrash(folderID: String): StudyFolderMetadata {
        var folder = loadStoredFolder(folderID)
            ?: allStudyFolders.find { it.folderID == folderID }
            ?: throw StudyLibraryStoreError.FOLDER_MISSING

        val folderPathComponents = folder.pathComponents
        val descendantFolders = loadAllFolderMetadata().filter { candidate ->
            candidate.folderID != folder.folderID
                && !candidate.isTrashed
                && pathComponentsStartWith(candidate.pathComponents, folderPathComponents)
        }
        val matchingItems = allStudyItems.filter { itemMatches(it, folder.path, folder.level) }
        val hasIndexedItems = folder.itemIDs.isNotEmpty()

        if (descendantFolders.isNotEmpty() || matchingItems.isNotEmpty() || hasIndexedItems) {
            throw StudyLibraryStoreError.writeFailed("study_folder_not_empty")
        }

        val now = System.currentTimeMillis()
        folder = folder.copy(isTrashed = true, trashedAt = now, updatedAt = now)
        save(folder)
        refresh()
        return folder
    }

    @Throws(StudyLibraryStoreError::class)
    fun save(item: StudyItemMetadata) {
        val previous = editableMetadataIfAvailable(item.itemID)
        save(item, previous)
        refresh()
    }

    @Throws(StudyLibraryStoreError::class)
    fun save(folder: StudyFolderMetadata) {
        ensureStudyDirectories()
        val fileName = folderMetadataFileName(folder)
        val folderFile = File(folderMetadataDir, fileName).canonicalFile

        check(isInsideFolderMetadataDirectory(folderFile)) {
            throw StudyLibraryStoreError.UNSAFE_DESTINATION
        }

        folderFile.writeText(jsonEncoder.toJson(folder))
        val index = loadIndex()
        index.folderMetadataFilesByFolderID[folder.folderID] = fileName
        index.updatedAt = System.currentTimeMillis()
        saveIndex(index)
    }

    // ── Private: save item ────────────────────────────────────────────────

    private fun save(metadata: StudyItemMetadata, previous: StudyItemMetadata?) {
        if (metadata.itemID.trim().isEmpty()) throw StudyLibraryStoreError.ITEM_MISSING

        ensureStudyDirectories()
        var metadataToSave = metadata
        metadataToSave = metadataToSave.copy(tags = StudyTagList.unique(metadata.tags))
        if (metadataToSave.folderIDs.isEmpty()) {
            metadataToSave = metadataToSave.copy(
                folderIDs = StudyItemMetadata.defaultFolderIDsFor(metadataToSave.filing)
            )
        }

        syncFolderLinks(metadataToSave, previous)
        val fileName = itemMetadataFileName(metadataToSave)
        val metadataFile = File(itemMetadataDir, fileName).canonicalFile

        check(isInsideItemMetadataDirectory(metadataFile)) {
            throw StudyLibraryStoreError.UNSAFE_DESTINATION
        }

        // Atomic write via temp file + rename
        val tmpFile = File(metadataFile.parentFile, "$fileName.tmp")
        tmpFile.writeText(jsonEncoder.toJson(metadataToSave))
        tmpFile.renameTo(metadataFile)

        val index = loadIndex()
        index.itemMetadataFilesByItemID[metadataToSave.itemID] = fileName
        metadataToSave.recordingID?.let {
            index.itemMetadataFilesByRecordingID[it] = fileName
        }
        index.updatedAt = System.currentTimeMillis()
        saveIndex(index)
    }

    private fun writeItemMetadataPreservingFolderLinks(metadata: StudyItemMetadata) {
        if (metadata.itemID.trim().isEmpty()) throw StudyLibraryStoreError.ITEM_MISSING

        ensureStudyDirectories()
        var metadataToSave = metadata
        metadataToSave = metadataToSave.copy(tags = StudyTagList.unique(metadata.tags))
        if (metadataToSave.folderIDs.isEmpty()) {
            metadataToSave = metadataToSave.copy(
                folderIDs = StudyItemMetadata.defaultFolderIDsFor(metadataToSave.filing)
            )
        }

        val fileName = itemMetadataFileName(metadataToSave)
        val metadataFile = File(itemMetadataDir, fileName).canonicalFile

        check(isInsideItemMetadataDirectory(metadataFile)) {
            throw StudyLibraryStoreError.UNSAFE_DESTINATION
        }

        val tmpFile = File(metadataFile.parentFile, "$fileName.tmp")
        tmpFile.writeText(jsonEncoder.toJson(metadataToSave))
        tmpFile.renameTo(metadataFile)

        val index = loadIndex()
        index.itemMetadataFilesByItemID[metadataToSave.itemID] = fileName
        metadataToSave.recordingID?.let {
            index.itemMetadataFilesByRecordingID[it] = fileName
        }
        index.updatedAt = System.currentTimeMillis()
        saveIndex(index)
    }

    // ── Private: editable metadata access ─────────────────────────────────

    private fun editableMetadata(recordingID: String): StudyItemMetadata {
        val recording = resolvedAudioFileStore.loadMetadata(recordingID)
        if (recording != null) {
            val fallback = StudyItemMetadata.defaultMetadata(recording)
            return loadStoredMetadata(recordingID)?.mergedWithCurrentRecording(recording)
                ?: fallback
        }
        loadStoredMetadata(recordingID)?.let { return it }
        throw StudyLibraryStoreError.ITEM_MISSING
    }

    private fun editableMetadataByItemID(itemID: StudyItemID): StudyItemMetadata? {
        allStudyItems.find { it.itemID == itemID || it.recordingID == itemID }?.let { return it }
        return loadAllStoredItemMetadata().find { it.itemID == itemID || it.recordingID == itemID }
    }

    private fun editableMetadataIfAvailable(recordingID: String): StudyItemMetadata? =
        allStudyItems.find { it.recordingID == recordingID }
            ?: loadStoredMetadata(recordingID)

    private fun loadStoredMetadata(recordingID: String): StudyItemMetadata? {
        val recordingItemID = StudyItemMetadata.recordingBundleItemID(recordingID)
        return loadAllStoredItemMetadata().find {
            it.recordingID == recordingID || it.itemID == recordingItemID
        }
    }

    // ── Private: stored item inclusion check ──────────────────────────────

    private fun shouldIncludeStoredItem(
        item: StudyItemMetadata,
        liveRecordingIDs: Set<String>,
        alreadyLoaded: Map<StudyItemID, StudyItemMetadata>
    ): Boolean {
        if (alreadyLoaded.containsKey(item.itemID)) return false
        if (item.kind == StudyItemKind.STANDALONE_NOTE || item.recordingID == null) return true
        if (item.customProperties["syncedMetadataOnly"] == "true") return true
        return item.recordingID in liveRecordingIDs
    }

    // ── Private: sync manifest helpers ───────────────────────────────────

    private fun makeSyncTombstones(
        items: List<StudyItemMetadata>,
        folders: List<StudyFolderMetadata>,
        deviceID: String
    ): List<StudyLibrarySyncTombstone> {
        val itemTombstones = items.filter { it.isTrashed }.map { item ->
            StudyLibrarySyncTombstone(
                id = "item:${item.itemID}",
                entityKind = StudyLibrarySyncEntityKind.ITEM,
                entityID = item.itemID,
                operation = StudyLibrarySyncOperation.TRASH,
                updatedAt = item.trashedAt ?: item.updatedAt,
                modifiedByDeviceID = item.modifiedByDeviceID ?: deviceID
            )
        }
        val folderTombstones = folders.filter { it.isTrashed }.map { folder ->
            StudyLibrarySyncTombstone(
                id = "folder:${folder.folderID}",
                entityKind = StudyLibrarySyncEntityKind.FOLDER,
                entityID = folder.folderID,
                operation = StudyLibrarySyncOperation.TRASH,
                updatedAt = folder.trashedAt ?: folder.updatedAt,
                modifiedByDeviceID = folder.modifiedByDeviceID ?: deviceID
            )
        }
        return itemTombstones + folderTombstones
    }

    private fun makePendingRecordingUploads(
        recordings: List<RecordingMetadata>,
        itemsByID: Map<StudyItemID, StudyItemMetadata>,
        targetDeviceID: String
    ): List<PendingRecordingUpload> =
        recordings.mapNotNull { recording ->
            if (recording.isDeleted) return@mapNotNull null
            if (recording.uploadStatus == "uploaded") return@mapNotNull null

            val fallbackItemID = StudyItemMetadata.recordingBundleItemID(recording.id)
            val item = itemsByID[fallbackItemID] ?: StudyItemMetadata.defaultMetadata(recording)
            PendingRecordingUpload(
                id = "${item.itemID}:${recording.id}",
                itemID = item.itemID,
                recordingID = recording.id,
                localAudioRelativePath = recording.relativeAudioPath ?: "",
                targetDeviceID = targetDeviceID,
                status = try {
                    PendingRecordingUploadStatus.valueOf(recording.uploadStatus?.uppercase() ?: "PENDING")
                } catch (_: Exception) { PendingRecordingUploadStatus.PENDING },
                createdAt = recording.createdAt?.time ?: System.currentTimeMillis(),
                updatedAt = item.updatedAt
            )
        }

    private fun makeSyncRecordings(recordings: List<RecordingMetadata>): List<LocalNetworkSyncRecordingEntry> {
        return recordings.map { recording ->
            val audioFile = resolvedAudioFileStore.audioFileFor(recording)
            val audioExists = audioFile.exists()

            LocalNetworkSyncRecordingEntry(
                recordingID = recording.id,
                metadataHash = recordingFingerprint(recording),
                audioAvailable = audioExists,
                audioChecksum = if (audioExists) SecureUploadUtilities.sha256Hex(audioFile) else null,
                audioSize = if (audioExists) audioFile.length() else null,
                updatedAt = recording.endedAt?.time ?: recording.createdAt.time,
                deleted = recording.isDeleted
            )
        }.sortedBy { it.recordingID }
    }

    private fun recordingFingerprint(recording: RecordingMetadata): String {
        return SecureUploadUtilities.sha256Hex(
            recording.toJson().toString().toByteArray(Charsets.UTF_8)
        )
    }

    // ── Private: manifest checksum ────────────────────────────────────────

    private fun makeManifestWithChecksum(
        deviceID: String,
        generatedAt: Long,
        libraryVersion: Int = 1,
        items: List<StudyItemMetadata>,
        folders: List<StudyFolderMetadata>,
        recordings: List<LocalNetworkSyncRecordingEntry>,
        tombstones: List<StudyLibrarySyncTombstone>,
        pendingUploads: List<PendingRecordingUpload>,
        baseCommitID: String? = null,
        commitID: String? = null,
        localManifestHash: String? = null
    ): StudyLibrarySyncManifest {
        // Compute checksum matching iOS: sorted JSON payload then SHA256 hex
        val checksum = computeManifestChecksum(
            deviceID, generatedAt, libraryVersion,
            items, folders, recordings, tombstones, pendingUploads
        )
        return StudyLibrarySyncManifest(
            deviceID = deviceID,
            generatedAt = generatedAt,
            libraryVersion = libraryVersion,
            items = items.sortedBy { it.itemID },
            folders = folders.sortedBy { it.folderID },
            recordings = recordings.sortedBy { it.recordingID },
            tombstones = tombstones.sortedBy { it.id },
            pendingUploads = pendingUploads.sortedBy { it.id },
            baseCommitID = baseCommitID,
            commitID = commitID,
            localManifestHash = localManifestHash,
            checksum = checksum
        )
    }

    private fun computeManifestChecksum(
        deviceID: String, generatedAt: Long, libraryVersion: Int,
        items: List<StudyItemMetadata>, folders: List<StudyFolderMetadata>,
        recordings: List<LocalNetworkSyncRecordingEntry>,
        tombstones: List<StudyLibrarySyncTombstone>,
        pendingUploads: List<PendingRecordingUpload>
    ): String {
        val json = sortedChecksumJSON(
            deviceID, generatedAt, libraryVersion,
            items, folders, recordings, tombstones, pendingUploads
        )
        return sha256Hex(json.toByteArray(Charsets.UTF_8))
    }

    private fun sortedChecksumJSON(
        deviceID: String, generatedAt: Long, libraryVersion: Int,
        items: List<StudyItemMetadata>, folders: List<StudyFolderMetadata>,
        recordings: List<LocalNetworkSyncRecordingEntry>,
        tombstones: List<StudyLibrarySyncTombstone>,
        pendingUploads: List<PendingRecordingUpload>
    ): String {
        // Build a JSON object with sorted keys, matching iOS checksum encoder (sortedKeys, iso8601)
        // We need deterministic JSON output. Using manual sorted-key construction.
        val sb = StringBuilder()
        sb.append("{")
        sb.append("\"deviceID\":\"${jsonEscape(deviceID)}\",")
        sb.append("\"generatedAt\":$generatedAt,")
        sb.append("\"libraryVersion\":$libraryVersion,")

        // items sorted by itemID
        sb.append("\"items\":[")
        val sortedItems = items.sortedBy { it.itemID }
        sortedItems.forEachIndexed { i, item ->
            if (i > 0) sb.append(",")
            sb.append(jsonEncoder.toJson(item))
        }
        sb.append("],")

        // folders sorted by folderID
        sb.append("\"folders\":[")
        val sortedFolders = folders.sortedBy { it.folderID }
        sortedFolders.forEachIndexed { i, folder ->
            if (i > 0) sb.append(",")
            sb.append(jsonEncoder.toJson(folder))
        }
        sb.append("],")

        // recordings sorted by recordingID
        sb.append("\"recordings\":[")
        val sortedRecordings = recordings.sortedBy { it.recordingID }
        sortedRecordings.forEachIndexed { i, recording ->
            if (i > 0) sb.append(",")
            sb.append(jsonEncoder.toJson(recording))
        }
        sb.append("],")

        // tombstones sorted by id
        sb.append("\"tombstones\":[")
        val sortedTombstones = tombstones.sortedBy { it.id }
        sortedTombstones.forEachIndexed { i, t ->
            if (i > 0) sb.append(",")
            val tj = JSONObject().apply {
                put("id", t.id)
                put("entityKind", t.entityKind.name)
                put("entityID", t.entityID)
                put("operation", t.operation.name)
                put("updatedAt", t.updatedAt)
                t.modifiedByDeviceID?.let { put("modifiedByDeviceID", it) }
            }
            sb.append(tj.toString())
        }
        sb.append("],")

        // pendingUploads sorted by id
        sb.append("\"pendingUploads\":[")
        val sortedUploads = pendingUploads.sortedBy { it.id }
        sortedUploads.forEachIndexed { i, u ->
            if (i > 0) sb.append(",")
            val uj = JSONObject().apply {
                put("id", u.id)
                put("itemID", u.itemID)
                put("recordingID", u.recordingID)
                put("localAudioRelativePath", u.localAudioRelativePath)
                put("targetDeviceID", u.targetDeviceID)
                put("status", u.status.name)
                put("createdAt", u.createdAt)
                put("updatedAt", u.updatedAt)
                u.lastAttemptAt?.let { put("lastAttemptAt", it) }
                put("retryCount", u.retryCount)
                u.lastError?.let { put("lastError", it) }
            }
            sb.append(uj.toString())
        }
        sb.append("]")

        sb.append("}")
        return sb.toString()
    }

    private fun legacyComputeManifestChecksum(
        deviceID: String, generatedAt: Long, libraryVersion: Int,
        items: List<StudyItemMetadata>, folders: List<StudyFolderMetadata>,
        tombstones: List<StudyLibrarySyncTombstone>
    ): String {
        val sb = StringBuilder()
        sb.append("{")
        sb.append("\"deviceID\":\"${jsonEscape(deviceID)}\",")
        sb.append("\"generatedAt\":$generatedAt,")
        sb.append("\"libraryVersion\":$libraryVersion,")
        sb.append("\"items\":[")
        items.sortedBy { it.itemID }.forEachIndexed { i, item ->
            if (i > 0) sb.append(",")
            sb.append(jsonEncoder.toJson(item))
        }
        sb.append("],")
        sb.append("\"folders\":[")
        folders.sortedBy { it.folderID }.forEachIndexed { i, folder ->
            if (i > 0) sb.append(",")
            sb.append(jsonEncoder.toJson(folder))
        }
        sb.append("],")
        sb.append("\"tombstones\":[")
        tombstones.sortedBy { it.id }.forEachIndexed { i, t ->
            if (i > 0) sb.append(",")
            val tj = JSONObject().apply {
                put("id", t.id)
                put("entityKind", t.entityKind.name)
                put("entityID", t.entityID)
                put("operation", t.operation.name)
                put("updatedAt", t.updatedAt)
                t.modifiedByDeviceID?.let { put("modifiedByDeviceID", it) }
            }
            sb.append(tj.toString())
        }
        sb.append("]")
        sb.append("}")
        return sha256Hex(sb.toString().toByteArray(Charsets.UTF_8))
    }

    private fun hasValidChecksum(manifest: StudyLibrarySyncManifest): Boolean {
        val computed = computeManifestChecksum(
            manifest.deviceID, manifest.generatedAt, manifest.libraryVersion,
            manifest.items, manifest.folders, manifest.recordings,
            manifest.tombstones, manifest.pendingUploads
        )
        val legacy = legacyComputeManifestChecksum(
            manifest.deviceID, manifest.generatedAt, manifest.libraryVersion,
            manifest.items, manifest.folders, manifest.tombstones
        )
        return manifest.checksum == computed || manifest.checksum == legacy
    }

    // ── Private: sync merge logic ─────────────────────────────────────────

    /** iOS: last-write-wins by updatedAt. Equal timestamps with different content → conflict, preserve local. */
    private fun mergedSyncItem(
        existing: StudyItemMetadata?,
        incoming: StudyItemMetadata,
        result: StudyLibrarySyncApplyResult
    ): StudyItemMetadata? {
        if (existing == null) return incoming

        if (incoming.updatedAt > existing.updatedAt) return incoming

        if (incoming.updatedAt == existing.updatedAt && incoming != existing) {
            result.conflictCount++
            return existing.copy(syncConflictStatus = "conflict_preserved_local")
        }

        result.skippedOlderCount++
        return null
    }

    /** iOS: last-write-wins. On accept, merge IDs from both. On conflict, preserve local but merge IDs. */
    private fun mergedSyncFolder(
        existing: StudyFolderMetadata?,
        incoming: StudyFolderMetadata,
        result: StudyLibrarySyncApplyResult
    ): StudyFolderMetadata? {
        if (existing == null) return incoming

        if (incoming.updatedAt > existing.updatedAt) {
            val mergedItemIDs = StudyItemMetadata.uniqueIDs(existing.itemIDs + incoming.itemIDs)
            val mergedChildIDs = StudyItemMetadata.uniqueIDs(existing.childFolderIDs + incoming.childFolderIDs)
            return incoming.copy(itemIDs = mergedItemIDs, childFolderIDs = mergedChildIDs)
        }

        if (incoming.updatedAt == existing.updatedAt && incoming != existing) {
            val mergedItemIDs = StudyItemMetadata.uniqueIDs(existing.itemIDs + incoming.itemIDs)
            val mergedChildIDs = StudyItemMetadata.uniqueIDs(existing.childFolderIDs + incoming.childFolderIDs)
            result.conflictCount++
            return existing.copy(
                itemIDs = mergedItemIDs,
                childFolderIDs = mergedChildIDs,
                syncConflictStatus = "conflict_preserved_local"
            )
        }

        result.skippedOlderCount++
        return null
    }

    private fun applySyncTombstone(tombstone: StudyLibrarySyncTombstone, remoteDeviceID: String): Boolean {
        when (tombstone.entityKind) {
            StudyLibrarySyncEntityKind.ITEM -> {
                var item = editableMetadataByItemID(tombstone.entityID) ?: return false
                if (tombstone.updatedAt < item.updatedAt) return false
                val isTrashed = tombstone.operation == StudyLibrarySyncOperation.TRASH
                    || tombstone.operation == StudyLibrarySyncOperation.DELETE
                    || tombstone.operation == StudyLibrarySyncOperation.DELETE_METADATA_ONLY
                item = item.copy(
                    isTrashed = isTrashed,
                    trashedAt = if (isTrashed) tombstone.updatedAt else null,
                    updatedAt = tombstone.updatedAt,
                    modifiedByDeviceID = tombstone.modifiedByDeviceID ?: remoteDeviceID
                )
                save(item)
                applySyncItemToRecordingMetadata(item)
                return true
            }
            StudyLibrarySyncEntityKind.FOLDER -> {
                var folder = loadStoredFolder(tombstone.entityID) ?: return false
                if (tombstone.updatedAt < folder.updatedAt) return false
                val isTrashed = tombstone.operation == StudyLibrarySyncOperation.TRASH
                    || tombstone.operation == StudyLibrarySyncOperation.DELETE
                    || tombstone.operation == StudyLibrarySyncOperation.DELETE_METADATA_ONLY
                folder = folder.copy(
                    isTrashed = isTrashed,
                    trashedAt = if (isTrashed) tombstone.updatedAt else null,
                    updatedAt = tombstone.updatedAt,
                    modifiedByDeviceID = tombstone.modifiedByDeviceID ?: remoteDeviceID
                )
                save(folder)
                return true
            }
        }
    }

    private fun applySyncItemToRecordingMetadata(item: StudyItemMetadata) {
        val recordingID = item.recordingID ?: return
        val recording = resolvedAudioFileStore.loadMetadata(recordingID) ?: return

        val updated = RecordingMetadata(
            id = recording.id,
            title = item.title,
            fileName = recording.fileName,
            relativeAudioPath = recording.relativeAudioPath,
            relativeMetadataPath = recording.relativeMetadataPath,
            createdAt = recording.createdAt,
            endedAt = recording.endedAt,
            duration = recording.duration,
            format = recording.format,
            codec = recording.codec,
            sampleRate = recording.sampleRate,
            channels = recording.channels,
            bitrate = recording.bitrate,
            fileSize = recording.fileSize,
            uploadStatus = recording.uploadStatus,
            transcriptionStatus = item.transcriptionStatus ?: recording.transcriptionStatus,
            noteStatus = item.noteStatus ?: recording.noteStatus,
            tags = item.tags.map { it.displayTitle },
            studyFiling = item.studyFiling,
            isDeleted = item.isTrashed,
            deletedAt = if (item.isTrashed) java.util.Date(item.trashedAt ?: recording.deletedAt?.time ?: item.updatedAt) else null
        )

        if (updated != recording) {
            resolvedAudioFileStore.updateMetadata(updated)
        }
    }

    /** Flag items whose recording doesn't exist locally as metadata-only sync entries. */
    private fun markSyncMetadataOnlyIfNeeded(item: StudyItemMetadata) {
        val recordingID = item.recordingID ?: return
        if (resolvedAudioFileStore.loadMetadata(recordingID) == null) {
            // Read-only, so we mark it in customProperties - handled by caller making a copy
        }
    }

    // ── Private: folder chain, links ────────────────────────────────────

    private fun syncFolderLinks(metadata: StudyItemMetadata, previous: StudyItemMetadata?) {
        val foldersByID = mutableMapOf<String, StudyFolderMetadata>()
        for (f in loadAllFolderMetadata()) foldersByID[f.folderID] = f

        val itemID = metadata.itemID
        val previousFolderIDs = previous?.folderIDs?.toSet() ?: emptySet()
        val targetFolderIDs = metadata.folderIDs.toSet()

        // Remove from folders no longer linked
        for (folderID in previousFolderIDs - targetFolderIDs) {
            val folder = foldersByID[folderID] ?: continue
            var updated = folder
            updated = updated.copy(itemIDs = updated.itemIDs.filter { it != itemID }, updatedAt = System.currentTimeMillis())
            foldersByID[folderID] = updated
        }

        // Add folder chain for auto-created hierarchy folders
        val chain = folderChain(metadata.filing, itemID)
        for (folder in chain) {
            var stored = foldersByID[folder.folderID] ?: folder
            stored = stored.copy(
                name = folder.name,
                level = folder.level,
                path = folder.path,
                parentFolderID = folder.parentFolderID,
                childFolderIDs = StudyItemMetadata.uniqueIDs(stored.childFolderIDs + folder.childFolderIDs)
            )
            if (targetFolderIDs.contains(folder.folderID) && itemID !in stored.itemIDs) {
                stored = stored.copy(itemIDs = stored.itemIDs + itemID)
            }
            if (!targetFolderIDs.contains(folder.folderID)) {
                stored = stored.copy(itemIDs = stored.itemIDs.filter { it != itemID })
            }
            stored = stored.copy(updatedAt = System.currentTimeMillis())
            foldersByID[folder.folderID] = stored
        }

        // Add to target folders
        for (folderID in targetFolderIDs) {
            val folder = foldersByID[folderID] ?: continue
            var updated = folder
            if (itemID !in updated.itemIDs) {
                updated = updated.copy(itemIDs = updated.itemIDs + itemID)
            }
            updated = updated.copy(updatedAt = System.currentTimeMillis())
            foldersByID[folderID] = updated
        }

        for (folder in foldersByID.values) {
            save(folder)
        }
    }

    private fun folderChain(filing: StudyFilingPath, itemID: String?): List<StudyFolderMetadata> {
        val effectiveFiling = StudyItemMetadata.effectiveFolderPath(filing)
        val values = listOf(
            StudyFolderLevel.TYPE to effectiveFiling.type,
            StudyFolderLevel.SUBJECT to effectiveFiling.subject,
            StudyFolderLevel.CHAPTER to effectiveFiling.chapter,
            StudyFolderLevel.TOPIC to effectiveFiling.topic
        )

        val folders = mutableListOf<StudyFolderMetadata>()
        var parentFolderID: String? = null
        val presentCount = values.count { it.second != null }

        for ((index, pair) in values.withIndex()) {
            val (level, value) = pair
            if (value == null) break

            val components = values.take(index + 1).mapNotNull { it.second }
            val path = StudyFolderLevel.filingPathFor(components)
            val childFolderIDs: List<String> = if (index + 1 < values.size && values[index + 1].second != null) {
                val childPath = StudyFolderLevel.filingPathFor(values.take(index + 2).mapNotNull { it.second })
                listOf(StudyFolderMetadata.folderIDFor(values[index + 1].first, childPath))
            } else {
                emptyList()
            }
            val isPresent = index < presentCount
            folders.add(StudyFolderMetadata(
                folderID = StudyFolderMetadata.folderIDFor(level, path),
                name = value,
                level = level,
                path = path,
                parentFolderID = parentFolderID,
                childFolderIDs = childFolderIDs,
                itemIDs = if (isPresent && index == presentCount - 1) itemID?.let { listOf(it) } ?: emptyList() else emptyList()
            ))
            parentFolderID = folders.last().folderID
        }

        return folders
    }

    private fun parentFolderID(path: StudyBrowsePath): String? {
        if (path.isRoot) return null
        val parentLevel = StudyFolderMetadata.levelForDepth(path.depth - 1) ?: return null
        return StudyFolderMetadata.folderIDFor(parentLevel, StudyFolderLevel.filingPathFor(path.components))
    }

    private fun appendChildFolderID(childFolderID: String, parentFolderID: String, parentPath: StudyBrowsePath) {
        val parentLevel = StudyFolderMetadata.levelForDepth(parentPath.depth - 1) ?: return

        val parent = loadStoredFolder(parentFolderID) ?: StudyFolderMetadata(
            folderID = parentFolderID,
            name = parentPath.components.lastOrNull() ?: StudyHierarchyRule.uncategorizedValue,
            level = parentLevel,
            path = StudyFolderLevel.filingPathFor(parentPath.components),
            parentFolderID = this.parentFolderID(parentPath.parent)
        )
        var updatedParent = parent
        if (childFolderID !in updatedParent.childFolderIDs) {
            updatedParent = updatedParent.copy(
                childFolderIDs = updatedParent.childFolderIDs + childFolderID,
                updatedAt = System.currentTimeMillis()
            )
            save(updatedParent)
        }
    }

    // ── Private: path helpers ────────────────────────────────────────────

    private fun pathComponentsStartWith(components: List<String>, prefix: List<String>): Boolean {
        if (components.size < prefix.size) return false
        return components.take(prefix.size) == prefix
    }

    private fun itemMatches(item: StudyItemMetadata, folderPath: StudyFilingPath, level: StudyFolderLevel): Boolean {
        return StudyFolderMetadata.pathComponentsFor(item.filing, level) ==
            StudyFolderMetadata.pathComponentsFor(folderPath, level)
    }

    private fun renamedPath(path: StudyFilingPath, level: StudyFolderLevel, name: String): StudyFilingPath? =
        when (level) {
            StudyFolderLevel.TYPE -> {
                if (path.type == null) null
                else path.copy(type = name)
            }
            StudyFolderLevel.SUBJECT -> {
                if (path.subject == null) null
                else path.copy(subject = name)
            }
            StudyFolderLevel.CHAPTER -> {
                if (path.chapter == null) null
                else path.copy(chapter = name)
            }
            StudyFolderLevel.TOPIC -> {
                if (path.topic == null) null
                else path.copy(topic = name)
            }
            StudyFolderLevel.CUSTOM -> null
        }

    // ── Private: loading ─────────────────────────────────────────────────

    private fun loadAllStoredItemMetadata(): List<StudyItemMetadata> =
        loadMetadataFilesFrom<StudyItemMetadata>(itemMetadataDir) + loadMetadataFilesFrom<StudyItemMetadata>(legacyItemMetadataDir)

    private fun loadAllFolderMetadata(): List<StudyFolderMetadata> =
        loadMetadataFilesFrom<StudyFolderMetadata>(folderMetadataDir)

    private fun loadReceiveRecordDerivedItems(): List<StudyItemMetadata> {
        val inboxDir = File(rootDir, "audio/inbox").canonicalFile
        if (!isInsideRoot(inboxDir) || !inboxDir.exists()) return emptyList()

        val items = mutableListOf<StudyItemMetadata>()
        val enumerator = inboxDir.walkTopDown().filter { it.isFile && it.name == "receive.json" }
        for (receiveFile in enumerator) {
            val receiveURL = receiveFile.canonicalFile
            if (!isInsideRoot(receiveURL)) continue
            try {
                val data = receiveURL.readText()
                val record = jsonEncoder.fromJson(data, RecordingReceiveRecord::class.java)
                val relativePath = relativePath(receiveURL)
                val item = StudyItemMetadata.defaultMetadata(record, relativePath) ?: continue
                items.add(item)
            } catch (_: Exception) {
                // skip corrupt receive.json
            }
        }
        return items
    }

    private inline fun <reified T> loadMetadataFilesFrom(dir: File): List<T> {
        val fileNames = loadMetadataFileNames(dir)
        return fileNames.mapNotNull { fileName ->
            val file = File(dir, fileName).canonicalFile
            if (!isInsideStudyDirectory(file)) return@mapNotNull null
            try {
                jsonEncoder.fromJson(file.readText(), T::class.java)
            } catch (_: Exception) {
                null
            }
        }
    }

    private fun loadMetadataFileNames(dir: File): List<String> {
        if (!dir.exists()) return emptyList()
        return dir.listFiles()
            ?.filter { it.isFile && it.extension == "json" }
            ?.map { it.name }
            ?.sortedWith(String.CASE_INSENSITIVE_ORDER)
            ?: emptyList()
    }

    private fun repairedFolders(
        folders: List<StudyFolderMetadata>,
        items: List<StudyItemMetadata>
    ): List<StudyFolderMetadata> {
        val existingItemIDs = items.map { it.itemID }.toSet()
        val foldersByID = mutableMapOf<String, StudyFolderMetadata>()
        for (folder in folders) {
            var repaired = folder
            repaired = repaired.copy(itemIDs = StudyItemMetadata.uniqueIDs(repaired.itemIDs.filter { it in existingItemIDs }))
            foldersByID[folder.folderID] = repaired
        }

        for (item in items) {
            for (folderID in item.folderIDs) {
                var folder = foldersByID[folderID] ?: continue
                if (item.itemID !in folder.itemIDs) {
                    folder = folder.copy(itemIDs = folder.itemIDs + item.itemID)
                }
                foldersByID[folderID] = folder
            }
        }

        return foldersByID.values.sortedWith(
            compareBy<StudyFolderMetadata> { it.pathComponents.joinToString("/") }.thenBy { it.name }
        )
    }

    private fun loadStoredFolder(folderID: String): StudyFolderMetadata? {
        val index = loadIndex()
        val candidateFileNames = mutableListOf<String>()
        index.folderMetadataFilesByFolderID[folderID]?.let { candidateFileNames.add(it) }
        candidateFileNames.add("${StudyPathSanitizer.sanitizedPathComponent(folderID)}.json")
        candidateFileNames.addAll(loadMetadataFileNames(folderMetadataDir))

        for (fileName in candidateFileNames.distinct()) {
            val file = File(folderMetadataDir, fileName).canonicalFile
            if (!isInsideFolderMetadataDirectory(file) || !file.exists()) continue
            try {
                val folder = jsonEncoder.fromJson(file.readText(), StudyFolderMetadata::class.java)
                if (folder.folderID == folderID) return folder
            } catch (_: Exception) {
                // skip
            }
        }
        return null
    }

    // ── Private: directories ─────────────────────────────────────────────

    private fun ensureStudyDirectories() {
        check(isInsideRoot(studyDir) && isInsideStudyDirectory(itemMetadataDir)
            && isInsideStudyDirectory(folderMetadataDir) && isInsideStudyDirectory(indexFile)
            && isInsideStudyDirectory(hierarchyRulesFile)) {
            throw StudyLibraryStoreError.UNSAFE_DESTINATION
        }

        itemMetadataDir.mkdirs()
        folderMetadataDir.mkdirs()

        if (!hierarchyRulesFile.exists()) {
            hierarchyRulesFile.writeText(
                jsonEncoder.toJson(listOf(StudyHierarchyRule.defaultCourseView))
            )
        }
        if (!indexFile.exists()) {
            indexFile.writeText(jsonEncoder.toJson(StudyMetadataIndex()))
        }
    }

    // ── Private: hierarchy rules ────────────────────────────────────────

    private fun loadHierarchyRules(): List<StudyHierarchyRule> {
        if (!hierarchyRulesFile.exists()) return listOf(StudyHierarchyRule.defaultCourseView)
        return try {
            val data = hierarchyRulesFile.readText()
            val rules = jsonEncoder.fromJson(data, Array<StudyHierarchyRule>::class.java).toList()
            if (rules.isEmpty()) listOf(StudyHierarchyRule.defaultCourseView)
            else rules.map { rule ->
                if (rule.id == StudyHierarchyRule.defaultCourseView.id
                    && rule.levels != StudyHierarchyRule.defaultCourseView.levels
                ) StudyHierarchyRule.defaultCourseView
                else rule
            }
        } catch (_: Exception) {
            listOf(StudyHierarchyRule.defaultCourseView)
        }
    }

    // ── Private: index ──────────────────────────────────────────────────

    private fun loadIndex(): StudyMetadataIndex {
        val urls = listOf(indexFile, legacyIndexFile)
        for (file in urls) {
            if (!file.exists()) continue
            try {
                return jsonEncoder.fromJson(file.readText(), StudyMetadataIndex::class.java)
            } catch (_: Exception) {}
        }
        return StudyMetadataIndex()
    }

    private fun saveIndex(index: StudyMetadataIndex) {
        check(isInsideStudyDirectory(indexFile)) {
            throw StudyLibraryStoreError.UNSAFE_DESTINATION
        }
        indexFile.writeText(jsonEncoder.toJson(index))
    }

    // ── Private: file naming ────────────────────────────────────────────

    private fun itemMetadataFileName(item: StudyItemMetadata): String =
        "${StudyPathSanitizer.sanitizedPathComponent(item.itemID)}.json"

    private fun folderMetadataFileName(folder: StudyFolderMetadata): String =
        "${StudyPathSanitizer.sanitizedPathComponent(folder.folderID)}.json"

    // ── Private: path safety ────────────────────────────────────────────

    private fun relativePath(url: File): String {
        val basePath = rootDir.canonicalFile.absolutePath
        val filePath = url.canonicalFile.absolutePath
        val baseWithSlash = if (basePath.endsWith("/")) basePath else "$basePath/"
        check(filePath.startsWith(baseWithSlash)) { throw StudyLibraryStoreError.UNSAFE_DESTINATION }
        return filePath.removePrefix(baseWithSlash)
    }

    private fun isInsideRoot(file: File): Boolean {
        val rootPath = rootDir.canonicalFile.absolutePath
        val path = file.canonicalFile.absolutePath
        return path == rootPath || path.startsWith("$rootPath/")
    }

    private fun isInsideStudyDirectory(file: File): Boolean {
        val studyPath = studyDir.canonicalFile.absolutePath
        val path = file.canonicalFile.absolutePath
        return path == studyPath || path.startsWith("$studyPath/")
    }

    private fun isInsideItemMetadataDirectory(file: File): Boolean {
        val dirPath = itemMetadataDir.canonicalFile.absolutePath
        val path = file.canonicalFile.absolutePath
        return path == dirPath || path.startsWith("$dirPath/")
    }

    private fun isInsideFolderMetadataDirectory(file: File): Boolean {
        val dirPath = folderMetadataDir.canonicalFile.absolutePath
        val path = file.canonicalFile.absolutePath
        return path == dirPath || path.startsWith("$dirPath/")
    }

    // ── Private: helpers ─────────────────────────────────────────────────

    companion object {
        private fun jsonEscape(s: String): String = s
            .replace("\\", "\\\\")
            .replace("\"", "\\\"")
            .replace("\n", "\\n")
            .replace("\r", "\\r")
            .replace("\t", "\\t")

        fun sha256Hex(bytes: ByteArray): String {
            val digest = java.security.MessageDigest.getInstance("SHA-256")
            val hash = digest.digest(bytes)
            return hash.joinToString("") { "%02x".format(it) }
        }
    }
}
