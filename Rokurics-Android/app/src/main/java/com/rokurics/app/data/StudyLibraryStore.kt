package com.rokurics.app.data

import android.content.Context
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.rokurics.app.RokuricsApp
import com.rokurics.app.domain.model.RecordingMetadata
import com.rokurics.app.domain.model.StudyBrowsePath
import com.rokurics.app.domain.model.StudyFilingPath
import com.rokurics.app.domain.model.StudyFolderColorToken
import com.rokurics.app.domain.model.StudyFolderLevel
import com.rokurics.app.domain.model.StudyFolderMetadata
import com.rokurics.app.domain.model.StudyItemKind
import com.rokurics.app.domain.model.StudyItemMetadata
import org.json.JSONObject
import java.io.File
import java.util.UUID

/**
 * Candidates for auto-completing each level of the study filing hierarchy.
 *
 * Collects every unique value the user has typed for each filing level
 * across all stored study items.
 */
data class StudyFilingCandidates(
    val types: List<String> = emptyList(),
    val subjects: List<String> = emptyList(),
    val chapters: List<String> = emptyList(),
    val topics: List<String> = emptyList()
)

/**
 * Central repository for the study library.
 *
 * Manages study items and folders organised in a 4-level hierarchy
 * (type → subject → chapter → topic), persisted as JSON files under
 * [RokuricsApp.instance.filesDir]/Rokurics/study/.
 *
 * This is a plain class (not a ViewModel) so it can be used from
 * services, ViewModels, and tests alike.
 */
class StudyLibraryStore(
    private val context: Context = RokuricsApp.instance
) {
    private val gson: Gson = GsonBuilder().setPrettyPrinting().create()

    // ── Directory layout ─────────────────────────────────────────

    private val studyDir: File
        get() = File(context.filesDir, "Rokurics/study").also { it.mkdirs() }

    private val itemsDir: File
        get() = File(studyDir, "items").also { it.mkdirs() }

    private val foldersDir: File
        get() = File(studyDir, "folders").also { it.mkdirs() }

    private val indexFile: File
        get() = File(studyDir, "index.json")

    private val hierarchyRulesFile: File
        get() = File(studyDir, "hierarchy-rules.json")

    // ── In-memory caches ─────────────────────────────────────────

    private val items = mutableMapOf<String, StudyItemMetadata>()
    private val folders = mutableMapOf<String, StudyFolderMetadata>()

    private data class StoreIndex(
        val items: MutableMap<String, String> = mutableMapOf(),
        val folders: MutableMap<String, String> = mutableMapOf()
    )

    private var index = StoreIndex()

    // ── Initialisation ───────────────────────────────────────────

    init {
        ensureDirectories()
        loadIndex()
        refresh()
        ensureHierarchyRules()
    }

    private fun ensureDirectories() {
        itemsDir.mkdirs()
        foldersDir.mkdirs()
    }

    // ── Public API ───────────────────────────────────────────────

    /**
     * Reload all items and folders from disk, rebuilding the in-memory
     * cache from the persisted index.
     */
    fun refresh() {
        loadIndex()
        loadItemsFromDisk()
        loadFoldersFromDisk()
    }

    fun syncFromRecordings(recordings: List<RecordingMetadata>) {
        for (rec in recordings) {
            val existing = items.values.find { it.recordingID == rec.id }
            if (existing != null) {
                val updated = existing.copy(
                    title = rec.title,
                    duration = rec.duration,
                    audioRelativePath = rec.relativeAudioPath,
                    transcriptionStatus = rec.transcriptionStatus,
                    noteStatus = rec.noteStatus,
                    updatedAt = System.currentTimeMillis()
                )
                if (updated != existing) save(updated)
            } else {
                val item = StudyItemMetadata(
                    itemID = "item_${rec.id}",
                    kind = StudyItemKind.RECORDING_BUNDLE,
                    title = rec.title,
                    createdAt = rec.createdAt.time,
                    recordingID = rec.id,
                    duration = rec.duration,
                    audioRelativePath = rec.relativeAudioPath,
                    transcriptionStatus = rec.transcriptionStatus,
                    noteStatus = rec.noteStatus
                )
                save(item)
            }
        }
    }

    /** All study items (including trashed). */
    fun allStudyItems(): List<StudyItemMetadata> = items.values.toList()

    /** All study folders (including trashed). */
    fun allStudyFolders(): List<StudyFolderMetadata> = folders.values.toList()

    /**
     * Persist a study item and update the index.
     */
    fun save(item: StudyItemMetadata) {
        val file = itemFile(item.itemID)
        file.writeText(gson.toJson(item))
        items[item.itemID] = item
        index.items[item.itemID] = file.name
        saveIndex()
    }

    /**
     * Persist a study folder and update the index.
     */
    fun save(folder: StudyFolderMetadata) {
        val file = folderFile(folder.folderID)
        file.writeText(gson.toJson(folder))
        folders[folder.folderID] = folder
        index.folders[folder.folderID] = file.name
        saveIndex()
    }

    fun updateFolderColor(folderID: String, colorToken: StudyFolderColorToken) {
        val folder = folders[folderID] ?: return
        save(folder.copy(colorToken = colorToken))
    }

    /**
     * Create a new folder at the given browse path.
     *
     * The folder inherits its hierarchy level from the depth of [path]:
     * depth 0 → TYPE, 1 → SUBJECT, 2 → CHAPTER, 3 → TOPIC, ≥ 4 → CUSTOM.
     *
     * The [name] occupies the level that corresponds to the folder's
     * depth, while higher-level values come from [path.components].
     */
    fun createFolder(name: String, path: StudyBrowsePath): StudyFolderMetadata {
        val folderID = UUID.randomUUID().toString()
        val depth = path.depth

        val level = when (depth) {
            0 -> StudyFolderLevel.TYPE
            1 -> StudyFolderLevel.SUBJECT
            2 -> StudyFolderLevel.CHAPTER
            3 -> StudyFolderLevel.TOPIC
            else -> StudyFolderLevel.CUSTOM
        }

        val filing = when (depth) {
            0 -> StudyFilingPath(type = name)
            1 -> StudyFilingPath(type = path.components[0], subject = name)
            2 -> StudyFilingPath(
                type = path.components[0],
                subject = path.components[1],
                chapter = name
            )
            3 -> StudyFilingPath(
                type = path.components[0],
                subject = path.components[1],
                chapter = path.components[2],
                topic = name
            )
            else -> StudyFilingPath(
                type = path.components.getOrNull(0),
                subject = path.components.getOrNull(1),
                chapter = path.components.getOrNull(2),
                topic = path.components.getOrNull(3)
            )
        }

        val folder = StudyFolderMetadata(
            folderID = folderID,
            name = name,
            level = level,
            path = filing
        )
        save(folder)
        return folder
    }

    /**
     * Rename an existing folder.
     * @throws IllegalArgumentException if [folderID] is not found.
     */
    fun renameFolder(folderID: String, name: String): StudyFolderMetadata {
        val existing = folders[folderID]
            ?: throw IllegalArgumentException("Folder not found: $folderID")
        val updated = existing.copy(name = name, updatedAt = System.currentTimeMillis())
        save(updated)
        return updated
    }

    /**
     * Mark a folder as trashed.
     * @throws IllegalArgumentException if [folderID] is not found.
     */
    fun moveFolderToTrash(folderID: String): StudyFolderMetadata {
        val existing = folders[folderID]
            ?: throw IllegalArgumentException("Folder not found: $folderID")
        val now = System.currentTimeMillis()
        val updated = existing.copy(
            isTrashed = true,
            trashedAt = now,
            updatedAt = now
        )
        save(updated)
        return updated
    }

    /**
     * Create or update a [StudyItemMetadata] from a [RecordingMetadata].
     *
     * If a study item already exists for this recording its fields are
     * refreshed; otherwise a new item is created.
     */
    fun upsertRecordingMetadata(recording: RecordingMetadata): StudyItemMetadata {
        val existing = items.values.find { it.recordingID == recording.id }

        return if (existing != null) {
            val updated = existing.copy(
                title = recording.title,
                duration = recording.duration,
                audioRelativePath = recording.relativeAudioPath,
                transcriptionStatus = recording.transcriptionStatus,
                noteStatus = recording.noteStatus,
                updatedAt = System.currentTimeMillis()
            )
            save(updated)
            updated
        } else {
            val newItem = StudyItemMetadata(
                itemID = UUID.randomUUID().toString(),
                title = recording.title,
                recordingID = recording.id,
                duration = recording.duration,
                audioRelativePath = recording.relativeAudioPath,
                transcriptionStatus = recording.transcriptionStatus,
                noteStatus = recording.noteStatus,
                sourceDescription = "录音: ${recording.title}"
            )
            save(newItem)
            newItem
        }
    }

    /**
     * Update the filing path for the study item associated with the
     * given recording.  No-op if no such item exists.
     */
    fun updateFiling(recordingID: String, filing: StudyFilingPath) {
        val item = items.values.find { it.recordingID == recordingID } ?: return
        val updated = item.copy(filing = filing, updatedAt = System.currentTimeMillis())
        save(updated)
    }

    /**
     * Return every unique value typed for each filing level across all
     * stored items.  Used to power auto-complete suggestions.
     */
    fun filingCandidates(): StudyFilingCandidates {
        val types = mutableSetOf<String>()
        val subjects = mutableSetOf<String>()
        val chapters = mutableSetOf<String>()
        val topics = mutableSetOf<String>()

        for (item in items.values) {
            val f = item.filing
            f.type?.let { types.add(it) }
            f.subject?.let { subjects.add(it) }
            f.chapter?.let { chapters.add(it) }
            f.topic?.let { topics.add(it) }
        }

        return StudyFilingCandidates(
            types = types.toList().sorted(),
            subjects = subjects.toList().sorted(),
            chapters = chapters.toList().sorted(),
            topics = topics.toList().sorted()
        )
    }

    /**
     * Look up a study item by its [recordingID].
     */
    fun item(recordingID: String): StudyItemMetadata? =
        items.values.find { it.recordingID == recordingID }

    // ── Internal helpers ─────────────────────────────────────────

    private fun itemFile(itemID: String): File = File(itemsDir, "$itemID.json")
    private fun folderFile(folderID: String): File = File(foldersDir, "$folderID.json")

    private fun loadIndex() {
        if (!indexFile.exists()) {
            index = StoreIndex()
            return
        }
        try {
            val json = JSONObject(indexFile.readText())
            val idx = StoreIndex()
            val itemsObj = json.optJSONObject("items")
            if (itemsObj != null) {
                val keys = itemsObj.keys()
                while (keys.hasNext()) {
                    val key = keys.next()
                    idx.items[key] = itemsObj.getString(key)
                }
            }
            val foldersObj = json.optJSONObject("folders")
            if (foldersObj != null) {
                val keys = foldersObj.keys()
                while (keys.hasNext()) {
                    val key = keys.next()
                    idx.folders[key] = foldersObj.getString(key)
                }
            }
            index = idx
        } catch (_: Exception) {
            index = StoreIndex()
        }
    }

    private fun saveIndex() {
        val itemsJson = JSONObject()
        index.items.forEach { (k, v) -> itemsJson.put(k, v) }
        val foldersJson = JSONObject()
        index.folders.forEach { (k, v) -> foldersJson.put(k, v) }
        val json = JSONObject().apply {
            put("items", itemsJson)
            put("folders", foldersJson)
        }
        indexFile.writeText(json.toString(2))
    }

    private fun loadItemsFromDisk() {
        items.clear()
        for ((id, filename) in index.items) {
            val file = File(itemsDir, filename)
            if (!file.exists()) continue
            try {
                val item = gson.fromJson(file.readText(), StudyItemMetadata::class.java)
                items[id] = item
            } catch (_: Exception) {
                // skip corrupted file
            }
        }
    }

    private fun loadFoldersFromDisk() {
        folders.clear()
        for ((id, filename) in index.folders) {
            val file = File(foldersDir, filename)
            if (!file.exists()) continue
            try {
                val folder = gson.fromJson(file.readText(), StudyFolderMetadata::class.java)
                folders[id] = folder
            } catch (_: Exception) {
                // skip corrupted file
            }
        }
    }

    private fun ensureHierarchyRules() {
        if (hierarchyRulesFile.exists()) return
        val rules = JSONObject().apply {
            put("maxDepth", 4)
            put("levels", listOf("type", "subject", "chapter", "topic"))
            put("version", 1)
        }
        hierarchyRulesFile.writeText(rules.toString(2))
    }

    // ── Sync helpers ─────────────────────────────────────────────────

    fun makeSyncManifest(deviceID: String): com.rokurics.app.domain.model.StudyLibrarySyncManifest {
        refresh()
        return com.rokurics.app.domain.model.StudyLibrarySyncManifest(
            deviceID = deviceID,
            generatedAt = System.currentTimeMillis(),
            items = items.values.toList().sortedBy { it.itemID },
            folders = folders.values.toList().sortedBy { it.folderID }
        )
    }

    fun applySyncManifest(
        manifest: com.rokurics.app.domain.model.StudyLibrarySyncManifest,
        localDeviceID: String
    ): com.rokurics.app.domain.model.StudyLibrarySyncApplyResult {
        if (!manifest.hasValidChecksum()) {
            throw IllegalStateException("sync_manifest_checksum_mismatch")
        }

        val result = com.rokurics.app.domain.model.StudyLibrarySyncApplyResult()

        // Apply item changes
        for (incoming in manifest.items) {
            val merged = mergedSyncItem(incoming, items[incoming.itemID], result)
            if (merged != null) {
                save(merged)
                result.appliedItemCount++
            }
        }

        // Apply folder changes
        for (incoming in manifest.folders) {
            val merged = mergedSyncFolder(incoming, folders[incoming.folderID], result)
            if (merged != null) {
                save(merged)
                result.appliedFolderCount++
            }
        }

        // Apply tombstones
        for (tombstone in manifest.tombstones) {
            applySyncTombstone(tombstone, result)
        }

        refresh()
        return result
    }

    private fun mergedSyncItem(
        incoming: com.rokurics.app.domain.model.StudyItemMetadata,
        existing: com.rokurics.app.domain.model.StudyItemMetadata?,
        result: com.rokurics.app.domain.model.StudyLibrarySyncApplyResult
    ): com.rokurics.app.domain.model.StudyItemMetadata? {
        if (existing == null) return incoming // new item, accept

        // Incoming is newer: last-write-wins
        if (incoming.updatedAt > existing.updatedAt) return incoming

        // Incoming is older: skip
        if (incoming.updatedAt < existing.updatedAt) {
            result.skippedOlderCount++
            return null
        }

        // Same timestamp but different content: conflict
        val incomingHash = syncItemHash(incoming)
        val existingHash = syncItemHash(existing)
        if (incomingHash != existingHash) {
            result.conflictCount++
            return null // preserve local
        }

        return null // same content, no-op
    }

    private fun mergedSyncFolder(
        incoming: com.rokurics.app.domain.model.StudyFolderMetadata,
        existing: com.rokurics.app.domain.model.StudyFolderMetadata?,
        result: com.rokurics.app.domain.model.StudyLibrarySyncApplyResult
    ): com.rokurics.app.domain.model.StudyFolderMetadata? {
        if (existing == null) return incoming

        if (incoming.updatedAt > existing.updatedAt) {
            // Accept incoming but merge item/child IDs from both
            val mergedItemIDs = (incoming.itemIDs + existing.itemIDs).distinct()
            val mergedChildIDs = (incoming.childFolderIDs + existing.childFolderIDs).distinct()
            return incoming.copy(itemIDs = mergedItemIDs, childFolderIDs = mergedChildIDs)
        }

        if (incoming.updatedAt < existing.updatedAt) {
            result.skippedOlderCount++
            return null
        }

        // Same time, different content: conflict
        val incomingHash = syncFolderHash(incoming)
        val existingHash = syncFolderHash(existing)
        if (incomingHash != existingHash) {
            result.conflictCount++
            // Merge IDs even on conflict
            val mergedItemIDs = (incoming.itemIDs + existing.itemIDs).distinct()
            val mergedChildIDs = (incoming.childFolderIDs + existing.childFolderIDs).distinct()
            save(existing.copy(itemIDs = mergedItemIDs, childFolderIDs = mergedChildIDs))
        }

        return null
    }

    private fun applySyncTombstone(
        tombstone: com.rokurics.app.domain.model.StudyLibrarySyncTombstone,
        result: com.rokurics.app.domain.model.StudyLibrarySyncApplyResult
    ) {
        when (tombstone.entityKind) {
            com.rokurics.app.domain.model.StudyLibrarySyncEntityKind.ITEM -> {
                val item = items[tombstone.entityID] ?: return
                if (tombstone.updatedAt >= item.updatedAt) {
                    val updated = item.copy(isTrashed = true, trashedAt = System.currentTimeMillis(), updatedAt = System.currentTimeMillis())
                    save(updated)
                    result.tombstoneCount++
                }
            }
            com.rokurics.app.domain.model.StudyLibrarySyncEntityKind.FOLDER -> {
                val folder = folders[tombstone.entityID] ?: return
                if (tombstone.updatedAt >= folder.updatedAt) {
                    val updated = folder.copy(isTrashed = true, trashedAt = System.currentTimeMillis(), updatedAt = System.currentTimeMillis())
                    save(updated)
                    result.tombstoneCount++
                }
            }
        }
    }

    private fun syncItemHash(item: com.rokurics.app.domain.model.StudyItemMetadata): String {
        val payload = org.json.JSONObject().apply {
            put("itemID", item.itemID)
            put("title", item.title)
            put("kind", item.kind.name)
            put("updatedAt", item.updatedAt)
        }
        return com.rokurics.app.data.SecureUploadUtilities.sha256Hex(payload.toString())
    }

    private fun syncFolderHash(folder: com.rokurics.app.domain.model.StudyFolderMetadata): String {
        val payload = org.json.JSONObject().apply {
            put("folderID", folder.folderID)
            put("name", folder.name)
            put("level", folder.level.name)
            put("updatedAt", folder.updatedAt)
        }
        return com.rokurics.app.data.SecureUploadUtilities.sha256Hex(payload.toString())
    }
}
