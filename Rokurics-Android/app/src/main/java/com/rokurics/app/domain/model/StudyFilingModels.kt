package com.rokurics.app.domain.model

import com.google.gson.annotations.SerializedName
import java.util.UUID

// ── StudyFilingPath ──────────────────────────────────────────────────────

data class StudyFilingPath(
    val type: String? = null,
    val subject: String? = null,
    val chapter: String? = null,
    val topic: String? = null
) {
    val isEmpty: Boolean get() = type == null && subject == null && chapter == null && topic == null

    val displaySummary: String
        get() {
            val parts = listOfNotNull(type, subject, chapter, topic)
            return if (parts.isEmpty()) uncategorizedTitle else parts.joinToString(" / ")
        }

    fun valueFor(level: StudyFilingLevel): String? = when (level) {
        StudyFilingLevel.TYPE -> type
        StudyFilingLevel.SUBJECT -> subject
        StudyFilingLevel.CHAPTER -> chapter
        StudyFilingLevel.TOPIC -> topic
    }

    fun valueFor(levelKey: String): String? = when (StudyTag.normalizedNamespace(levelKey)) {
        "type" -> type
        "subject" -> subject
        "chapter" -> chapter
        "topic" -> topic
        else -> null
    }

    fun valueFor(level: StudyFolderLevel): String? = valueFor(level.key)

    fun suggestedTitle(defaultTitle: String): String {
        val parts = listOfNotNull(subject, chapter, topic)
        if (parts.isNotEmpty()) return parts.joinToString(" · ")
        return type ?: defaultTitle
    }

    companion object {
        const val uncategorizedTitle = "未分类"
        const val missingTitle = "未填写"

        fun normalized(value: String?): String? =
            value?.trim()?.ifEmpty { null }

    }
}

// ── StudyFilingLevel ─────────────────────────────────────────────────────

enum class StudyFilingLevel(val title: String) {
    TYPE("门类"),
    SUBJECT("课程"),
    CHAPTER("章节"),
    TOPIC("主题")
}

// ── StudyFilingCandidates ─────────────────────────────────────────────────

data class StudyFilingCandidates(
    val types: List<String> = emptyList(),
    val subjects: List<String> = emptyList(),
    val chapters: List<String> = emptyList(),
    val topics: List<String> = emptyList()
) {
    fun valuesFor(level: StudyFilingLevel): List<String> = when (level) {
        StudyFilingLevel.TYPE -> types
        StudyFilingLevel.SUBJECT -> subjects
        StudyFilingLevel.CHAPTER -> chapters
        StudyFilingLevel.TOPIC -> topics
    }

    fun valuesFor(level: StudyFolderLevel): List<String> = when (level) {
        StudyFolderLevel.TYPE -> types
        StudyFolderLevel.SUBJECT -> subjects
        StudyFolderLevel.CHAPTER -> chapters
        StudyFolderLevel.TOPIC -> topics
        StudyFolderLevel.CUSTOM -> emptyList()
    }

    companion object {
        val empty: StudyFilingCandidates = StudyFilingCandidates()

        @JvmName("collectFromRecordings")
        fun collectFrom(recordings: List<RecordingMetadata>): StudyFilingCandidates =
            StudyFilingCandidates(
                types = sortedUnique(recordings.mapNotNull { it.studyFiling?.type }),
                subjects = sortedUnique(recordings.mapNotNull { it.studyFiling?.subject }),
                chapters = sortedUnique(recordings.mapNotNull { it.studyFiling?.chapter }),
                topics = sortedUnique(recordings.mapNotNull { it.studyFiling?.topic })
            )

        @JvmName("collectFromItems")
        fun collectFrom(items: List<StudyItemMetadata>): StudyFilingCandidates =
            StudyFilingCandidates(
                types = sortedUnique(valuesForLevel("type", items)),
                subjects = sortedUnique(valuesForLevel("subject", items)),
                chapters = sortedUnique(valuesForLevel("chapter", items)),
                topics = sortedUnique(valuesForLevel("topic", items))
            )

        private fun valuesForLevel(level: String, items: List<StudyItemMetadata>): List<String> =
            items.flatMap { item ->
                val values = mutableListOf<String>()
                item.filingPath.valueFor(level)?.let { values.add(it) }
                values.addAll(
                    item.tags
                        .filter { it.namespace == StudyTag.normalizedNamespace(level) }
                        .map { it.displayTitle }
                )
                values
            }

        private fun sortedUnique(values: List<String>): List<String> {
            val seen = mutableSetOf<String>()
            val result = mutableListOf<String>()
            for (value in values) {
                val normalized = StudyFilingPath.normalized(value) ?: continue
                val key = normalized.lowercase()
                if (seen.contains(key)) continue
                seen.add(key)
                result.add(normalized)
            }
            return result.sortedWith(String.CASE_INSENSITIVE_ORDER)
        }
    }
}

// ── RecordingStudyNode ────────────────────────────────────────────────────

data class RecordingStudyNode(
    val id: String,
    val level: StudyFilingLevel,
    val title: String,
    val children: List<RecordingStudyNode> = emptyList(),
    val recordings: List<RecordingMetadata> = emptyList()
) {
    val isFallback: Boolean
        get() = title == StudyFilingPath.uncategorizedTitle || title == StudyFilingPath.missingTitle
}

// ── RecordingStudyBrowsePath ──────────────────────────────────────────────

data class RecordingStudyBrowsePath(
    val components: List<String> = emptyList()
) {
    val isRoot: Boolean get() = components.isEmpty()
    val depth: Int get() = components.size
    val parent: RecordingStudyBrowsePath
        get() = if (components.isEmpty()) this
            else RecordingStudyBrowsePath(components.dropLast(1))

    val isUncategorizedTypeSelection: Boolean
        get() = components.size == 1 && components.first() == StudyFilingPath.uncategorizedTitle

    fun appending(value: String): RecordingStudyBrowsePath =
        RecordingStudyBrowsePath(components + value)

    fun truncatedTo(depth: Int): RecordingStudyBrowsePath =
        RecordingStudyBrowsePath(components.take(depth.coerceAtLeast(0)))
}

// ── RecordingStudyFolder ──────────────────────────────────────────────────

data class RecordingStudyFolder(
    val id: String,
    val level: StudyFilingLevel,
    val title: String,
    val itemCount: Int = 0,
    val path: RecordingStudyBrowsePath = RecordingStudyBrowsePath()
) {
    val isFallback: Boolean
        get() = title == StudyFilingPath.uncategorizedTitle || title == StudyFilingPath.missingTitle
}

// ── RecordingStudyBrowseContent ───────────────────────────────────────────

data class RecordingStudyBrowseContent(
    val path: RecordingStudyBrowsePath = RecordingStudyBrowsePath(),
    val folders: List<RecordingStudyFolder> = emptyList(),
    val recordings: List<RecordingMetadata> = emptyList()
)

// ── RecordingStudyBrowser ─────────────────────────────────────────────────

object RecordingStudyBrowser {
    fun content(
        recordings: List<RecordingMetadata>,
        path: RecordingStudyBrowsePath
    ): RecordingStudyBrowseContent {
        val matching = recordings.filter { recordingMatches(it, path) }

        if (path.depth >= StudyFilingLevel.entries.size || path.isUncategorizedTypeSelection) {
            return RecordingStudyBrowseContent(
                path = path,
                folders = emptyList(),
                recordings = sortedRecordings(matching)
            )
        }

        val nextLevel = StudyFilingLevel.entries[path.depth]
        val grouped = matching.groupBy { displayValue(it, nextLevel) }
        val folders = grouped.map { (title, recs) ->
            RecordingStudyFolder(
                id = "${path.components.joinToString("/")}/${nextLevel.name}=$title",
                level = nextLevel,
                title = title,
                itemCount = recs.size,
                path = path.appending(title)
            )
        }.sortedWith(folderComparator)

        return RecordingStudyBrowseContent(path = path, folders = folders, recordings = emptyList())
    }

    fun breadcrumbs(path: RecordingStudyBrowsePath): List<Pair<String, RecordingStudyBrowsePath>> {
        val result = mutableListOf("学习库" to RecordingStudyBrowsePath())
        for (i in path.components.indices) {
            val componentPath = path.truncatedTo(i + 1)
            result.add(path.components[i] to componentPath)
        }
        return result
    }

    fun levelTitle(path: RecordingStudyBrowsePath): String {
        if (path.depth >= StudyFilingLevel.entries.size || path.isUncategorizedTypeSelection)
            return "录音"
        return StudyFilingLevel.entries[path.depth].title
    }

    fun recordingMatches(recording: RecordingMetadata, path: RecordingStudyBrowsePath): Boolean {
        if (path.depth > StudyFilingLevel.entries.size) return false
        for ((index, component) in path.components.withIndex()) {
            val level = StudyFilingLevel.entries[index]
            if (displayValue(recording, level) != component) return false
        }
        return true
    }

    private fun displayValue(recording: RecordingMetadata, level: StudyFilingLevel): String {
        recording.studyFiling?.valueFor(level)?.let { if (it.isNotEmpty()) return it }
        return if (level == StudyFilingLevel.TYPE) StudyFilingPath.uncategorizedTitle
            else StudyFilingPath.missingTitle
    }

    private val folderComparator = Comparator<RecordingStudyFolder> { left, right ->
        if (left.isFallback != right.isFallback) return@Comparator if (left.isFallback) 1 else -1
        left.title.compareTo(right.title, ignoreCase = true)
    }

    private fun sortedRecordings(recordings: List<RecordingMetadata>): List<RecordingMetadata> =
        recordings.sortedWith(compareByDescending<RecordingMetadata> { it.createdAt }
            .thenBy { it.title })
}

// ── RecordingStudyTreeBuilder ─────────────────────────────────────────────

object RecordingStudyTreeBuilder {
    fun build(recordings: List<RecordingMetadata>): List<RecordingStudyNode> {
        val root = MutableRecordingStudyNode(id = "root", level = StudyFilingLevel.TYPE, title = "学习库")
        for (recording in recordings) {
            insert(recording, levelIndex = 0, node = root)
        }
        return root.children
            .map { makeNode(it) }
            .sortedWith(nodeComparator)
    }

    private fun insert(
        recording: RecordingMetadata,
        levelIndex: Int,
        node: MutableRecordingStudyNode
    ) {
        if (levelIndex >= StudyFilingLevel.entries.size) {
            node.recordingsByID[recording.id] = recording
            return
        }
        val level = StudyFilingLevel.entries[levelIndex]
        val title = displayValue(recording.studyFiling, level)
        val childID = "${node.id}/${level.name}=$title"
        val child = node.child(id = childID, level = level, title = title)
        insert(recording, levelIndex = levelIndex + 1, node = child)
    }

    private fun displayValue(filing: StudyFilingPath?, level: StudyFilingLevel): String {
        filing?.valueFor(level)?.let { if (it.isNotEmpty()) return it }
        return if (level == StudyFilingLevel.TYPE) StudyFilingPath.uncategorizedTitle
            else StudyFilingPath.missingTitle
    }

    private fun makeNode(mutable: MutableRecordingStudyNode): RecordingStudyNode =
        RecordingStudyNode(
            id = mutable.id,
            level = mutable.level,
            title = mutable.title,
            children = mutable.children.map { makeNode(it) }.sortedWith(nodeComparator),
            recordings = mutable.recordingsByID.values.sortedWith(
                compareByDescending<RecordingMetadata> { it.createdAt }.thenBy { it.title }
            )
        )

    private val nodeComparator = Comparator<RecordingStudyNode> { left, right ->
        if (left.isFallback != right.isFallback) return@Comparator if (left.isFallback) 1 else -1
        left.title.compareTo(right.title, ignoreCase = true)
    }
}

private class MutableRecordingStudyNode(
    val id: String,
    val level: StudyFilingLevel,
    val title: String
) {
    val recordingsByID: MutableMap<String, RecordingMetadata> = mutableMapOf()
    private val childrenByID: MutableMap<String, MutableRecordingStudyNode> = mutableMapOf()

    val children: List<MutableRecordingStudyNode> get() = childrenByID.values.toList()

    fun child(id: String, level: StudyFilingLevel, title: String): MutableRecordingStudyNode =
        childrenByID.getOrPut(id) { MutableRecordingStudyNode(id, level, title) }
}

// ── RecordingSaveTitleResolver ────────────────────────────────────────────

object RecordingSaveTitleResolver {
    fun title(
        defaultTitle: String,
        pendingTitle: String?,
        studyFiling: StudyFilingPath?,
        directSave: Boolean
    ): String {
        if (directSave) return defaultTitle
        StudyFilingPath.normalized(pendingTitle)?.let { return it }
        return studyFiling?.suggestedTitle(defaultTitle) ?: defaultTitle
    }
}

// ── Type aliases ──────────────────────────────────────────────────────────

typealias StudyItemID = String
typealias StudyFolderID = String

// ── StudyItemKind ─────────────────────────────────────────────────────────

enum class StudyItemKind {
    @SerializedName("recordingBundle") RECORDING_BUNDLE,
    @SerializedName("standaloneNote") STANDALONE_NOTE
}

// ── ProcessingMode ────────────────────────────────────────────────────────

enum class ProcessingMode {
    @SerializedName("singlePass") SINGLE_PASS,
    @SerializedName("chunked") CHUNKED,
    @SerializedName("sectioned") SECTIONED
}

// ── RecordingTranscriptionChunkRecord ─────────────────────────────────────

data class RecordingTranscriptionChunkRecord(
    val index: Int = 0,
    val start: Double? = null,
    val end: Double? = null,
    val status: String? = null
)

// ── RecordingNoteSectionRecord ────────────────────────────────────────────

data class RecordingNoteSectionRecord(
    val index: Int = 0,
    val sourceStart: Int? = null,
    val sourceEnd: Int? = null,
    val status: String? = null,
    val sectionNoteRelativePath: String? = null
)

// ── StudyTag ──────────────────────────────────────────────────────────────

data class StudyTag(
    val id: String,
    val namespace: String = "custom",
    val value: String,
    val displayName: String? = null,
    val createdAt: Long? = null
) {
    val displayTitle: String get() = displayName?.trim()?.ifEmpty { null } ?: value

    override fun equals(other: Any?): Boolean {
        if (other !is StudyTag) return false
        return namespace == other.namespace &&
            value.lowercase() == other.value.lowercase()
    }

    override fun hashCode(): Int {
        var result = namespace.hashCode()
        result = 31 * result + value.lowercase().hashCode()
        return result
    }

    companion object {
        fun normalizedNamespace(namespace: String): String =
            namespace.trim().lowercase().ifEmpty { "custom" }

        fun normalizedValue(value: String): String = value.trim()

        fun makeID(namespace: String, value: String): String =
            "${normalizedNamespace(namespace)}:${normalizedValue(value).lowercase()}"

    }
}

// ── StudyTagList ──────────────────────────────────────────────────────────

object StudyTagList {
    fun unique(tags: List<StudyTag>): List<StudyTag> {
        val seen = mutableSetOf<String>()
        val result = mutableListOf<StudyTag>()
        for (tag in tags) {
            val value = StudyTag.normalizedValue(tag.value)
            if (value.isEmpty()) continue
            val normalizedTag = StudyTag(
                id = tag.id,
                namespace = tag.namespace,
                value = value,
                displayName = tag.displayName,
                createdAt = tag.createdAt
            )
            val key = "${normalizedTag.namespace}${normalizedTag.value.lowercase()}"
            if (seen.contains(key)) continue
            seen.add(key)
            result.add(normalizedTag)
        }
        return result
    }
}

// ── StudyItemMetadata ─────────────────────────────────────────────────────

data class StudyItemMetadata(
    val itemID: String,
    val kind: StudyItemKind = StudyItemKind.RECORDING_BUNDLE,
    val title: String = "未命名录音",
    val createdAt: Long = System.currentTimeMillis(),
    val updatedAt: Long = System.currentTimeMillis(),
    val filing: StudyFilingPath = StudyFilingPath(),
    val tags: List<StudyTag> = emptyList(),
    val folderIDs: List<String> = emptyList(),
    val customProperties: Map<String, String> = emptyMap(),
    val recordingID: String? = null,
    val sanitizedRecordingID: String? = null,
    val duration: Double? = null,
    val audioRelativePath: String? = null,
    val receiveRelativePath: String? = null,
    val transcriptRelativePath: String? = null,
    val transcriptMarkdownRelativePath: String? = null,
    val noteRelativePath: String? = null,
    val transcriptionStatus: String? = null,
    val noteStatus: String? = null,
    val noteSections: List<RecordingNoteSectionRecord>? = null,
    val sourceDescription: String? = null,
    val isTrashed: Boolean = false,
    val trashedAt: Long? = null,
    val modifiedByDeviceID: String? = null,
    val syncConflictStatus: String? = null
) {
    val filingPath: StudyFilingPath get() = filing

    var studyFiling: StudyFilingPath?
        get() = if (filing.isEmpty) null else filing
        set(value) {
            throw UnsupportedOperationException("Use copy(filing=...) for immutable updates")
        }

    val hasTranscript: Boolean
        get() = transcriptMarkdownRelativePath != null || transcriptRelativePath != null

    val hasNote: Boolean
        get() = noteRelativePath != null

    val durationForDisplay: Double get() = duration ?: 0.0

    fun mergedWithCurrentRecording(recording: RecordingMetadata): StudyItemMetadata {
        val resolvedFiling = if (filing.isEmpty) (recording.studyFiling ?: StudyFilingPath()) else filing
        val resolvedFolderIDs = if (folderIDs.isEmpty()) defaultFolderIDsFor(resolvedFiling) else folderIDs
        return copy(
            kind = StudyItemKind.RECORDING_BUNDLE,
            title = recording.title,
            createdAt = recording.createdAt?.time ?: createdAt,
            filing = resolvedFiling,
            folderIDs = resolvedFolderIDs,
            recordingID = recording.id,
            duration = recording.duration,
            audioRelativePath = recording.relativeAudioPath,
            transcriptionStatus = recording.transcriptionStatus,
            noteStatus = recording.noteStatus,
            isTrashed = isTrashed || recording.isDeleted,
            trashedAt = recording.deletedAt?.time ?: trashedAt
        )
    }

    fun syncSanitized(fallbackDeviceID: String? = null): StudyItemMetadata =
        copy(
            modifiedByDeviceID = modifiedByDeviceID ?: fallbackDeviceID,
            customProperties = StudyLibrarySyncSanitizer.filteredCustomProperties(customProperties)
        )

    companion object {
        fun recordingBundleItemID(recordingID: String): StudyItemID =
            "item_recording_${StudyPathSanitizer.sanitizedPathComponent(recordingID)}"

        fun defaultFolderIDsFor(filing: StudyFilingPath): List<StudyFolderID> {
            val path = effectiveFolderPath(filing)
            val deepest = StudyFolderMetadata.deepestLevelIn(path) ?: return emptyList()
            return listOf(StudyFolderMetadata.folderIDFor(deepest, path))
        }

        fun effectiveFolderPath(filing: StudyFilingPath): StudyFilingPath {
            if (filing.isEmpty) return StudyFilingPath(type = StudyHierarchyRule.uncategorizedValue)
            val hasTopic = filing.topic != null
            val hasChapter = filing.chapter != null || hasTopic
            val hasSubject = filing.subject != null || hasChapter
            return StudyFilingPath(
                type = filing.type ?: StudyHierarchyRule.uncategorizedValue,
                subject = if (hasSubject) (filing.subject ?: StudyHierarchyRule.missingValue) else null,
                chapter = if (hasChapter) (filing.chapter ?: StudyHierarchyRule.missingValue) else null,
                topic = filing.topic
            )
        }

        fun uniqueIDs(values: List<String>): List<String> {
            val seen = mutableSetOf<String>()
            val result = mutableListOf<String>()
            for (value in values) {
                val normalized = normalized(value) ?: continue
                if (seen.contains(normalized)) continue
                seen.add(normalized)
                result.add(normalized)
            }
            return result
        }

        fun normalized(value: String?): String? =
            value?.trim()?.ifEmpty { null }

        fun defaultMetadata(recording: RecordingMetadata): StudyItemMetadata =
            StudyItemMetadata(
                itemID = recordingBundleItemID(recording.id),
                kind = StudyItemKind.RECORDING_BUNDLE,
                title = recording.title,
                createdAt = recording.createdAt?.time ?: System.currentTimeMillis(),
                recordingID = recording.id,
                sanitizedRecordingID = StudyPathSanitizer.sanitizedPathComponent(recording.id),
                duration = recording.duration,
                audioRelativePath = recording.relativeAudioPath,
                filing = recording.studyFiling ?: StudyFilingPath(),
                tags = recording.tags.map { StudyTag(id = StudyTag.makeID("custom", it), namespace = "custom", value = it) },
                updatedAt = System.currentTimeMillis(),
                transcriptionStatus = recording.transcriptionStatus,
                noteStatus = recording.noteStatus,
                sourceDescription = "Android",
                isTrashed = recording.isDeleted,
                trashedAt = recording.deletedAt?.time
            )

        fun defaultMetadata(
            receiveRecord: RecordingReceiveRecord,
            receiveRelativePath: String?
        ): StudyItemMetadata? {
            val recID = normalizeId(receiveRecord.recordingID) ?: return null
            return StudyItemMetadata(
                itemID = recordingBundleItemID(recID),
                kind = StudyItemKind.RECORDING_BUNDLE,
                title = receiveRecord.normalizedTitle ?: receiveRecord.originalTitle ?: "未命名录音",
                createdAt = receiveRecord.createdAt ?: receiveRecord.receivedAt ?: 0L,
                recordingID = recID,
                sanitizedRecordingID = receiveRecord.sanitizedRecordingID
                    ?: StudyPathSanitizer.sanitizedPathComponent(recID),
                duration = receiveRecord.duration ?: 0.0,
                audioRelativePath = receiveRecord.audioRelativePath,
                receiveRelativePath = receiveRelativePath,
                transcriptRelativePath = receiveRecord.transcriptRelativePath,
                transcriptMarkdownRelativePath = receiveRecord.transcriptMarkdownRelativePath,
                noteRelativePath = receiveRecord.noteRelativePath,
                filing = receiveRecord.studyFiling ?: StudyFilingPath(),
                tags = receiveRecord.tags.map { StudyTag(id = StudyTag.makeID("custom", it), namespace = "custom", value = it) },
                updatedAt = receiveRecord.updatedAt ?: 0L,
                transcriptionStatus = receiveRecord.transcriptionStatus,
                noteStatus = receiveRecord.noteStatus,
                noteSections = receiveRecord.noteSections,
                sourceDescription = receiveRecord.sourceDeviceName
            )
        }

        private fun normalizeId(value: String?): String? = value?.trim()?.ifEmpty { null }


    }
}

// ── StudyFolderLevel ──────────────────────────────────────────────────────

enum class StudyFolderLevel(val key: String, val title: String) {
    TYPE("type", "门类"),
    SUBJECT("subject", "课程"),
    CHAPTER("chapter", "章节"),
    TOPIC("topic", "主题"),
    CUSTOM("custom", "文件夹");

    companion object {
        fun forDepth(depth: Int): StudyFolderLevel? = when (depth) {
            0 -> TYPE
            1 -> SUBJECT
            2 -> CHAPTER
            3 -> TOPIC
            else -> null
        }

        fun filingPathFor(components: List<String>): StudyFilingPath = StudyFilingPath(
            type = components.getOrNull(0),
            subject = components.getOrNull(1)?.trim()?.ifEmpty { null },
            chapter = components.getOrNull(2)?.trim()?.ifEmpty { null },
            topic = components.getOrNull(3)?.trim()?.ifEmpty { null }
        )
    }
}

// ── StudyFolderMetadata ───────────────────────────────────────────────────

data class StudyFolderMetadata(
    val folderID: String,
    val name: String = StudyHierarchyRule.missingValue,
    val level: StudyFolderLevel = StudyFolderLevel.CUSTOM,
    val path: StudyFilingPath = StudyFilingPath(),
    val parentFolderID: String? = null,
    val childFolderIDs: List<String> = emptyList(),
    val itemIDs: List<String> = emptyList(),
    val createdAt: Long = System.currentTimeMillis(),
    val updatedAt: Long = System.currentTimeMillis(),
    val colorToken: StudyFolderColorToken? = null,
    val isTrashed: Boolean = false,
    val trashedAt: Long? = null,
    val customProperties: Map<String, String> = emptyMap(),
    val modifiedByDeviceID: String? = null,
    val syncConflictStatus: String? = null
) {
    val pathComponents: List<String>
        get() = pathComponentsFor(path, level)

    fun syncSanitized(fallbackDeviceID: String? = null): StudyFolderMetadata =
        copy(
            modifiedByDeviceID = modifiedByDeviceID ?: fallbackDeviceID,
            customProperties = StudyLibrarySyncSanitizer.filteredCustomProperties(customProperties),
            itemIDs = StudyItemMetadata.uniqueIDs(itemIDs),
            childFolderIDs = StudyItemMetadata.uniqueIDs(childFolderIDs)
        )

    companion object {
        fun folderIDFor(level: StudyFolderLevel, path: StudyFilingPath): String {
            val components = pathComponentsFor(path, level = level)
            val raw = (listOf(level.key) + components).joinToString("_")
            return "folder_${StudyPathSanitizer.sanitizedPathComponent(raw)}"
        }

        fun pathComponentsFor(path: StudyFilingPath, level: StudyFolderLevel): List<String> {
            val values = listOf(
                StudyFolderLevel.TYPE to path.type,
                StudyFolderLevel.SUBJECT to path.subject,
                StudyFolderLevel.CHAPTER to path.chapter,
                StudyFolderLevel.TOPIC to path.topic
            )
            val result = mutableListOf<String>()
            for ((candidateLevel, value) in values) {
                val v = StudyFilingPath.normalized(value) ?: break
                result.add(v)
                if (candidateLevel == level) break
            }
            return result
        }

        fun deepestLevelIn(path: StudyFilingPath): StudyFolderLevel? {
            if (path.topic != null) return StudyFolderLevel.TOPIC
            if (path.chapter != null) return StudyFolderLevel.CHAPTER
            if (path.subject != null) return StudyFolderLevel.SUBJECT
            if (path.type != null) return StudyFolderLevel.TYPE
            return null
        }

        fun levelForDepth(depth: Int): StudyFolderLevel? = when (depth) {
            0 -> StudyFolderLevel.TYPE
            1 -> StudyFolderLevel.SUBJECT
            2 -> StudyFolderLevel.CHAPTER
            3 -> StudyFolderLevel.TOPIC
            else -> null
        }

    }
}

// ── StudyFolderColorToken ─────────────────────────────────────────────────

enum class StudyFolderColorToken(val hexColor: Long) {
    DEFAULT(0xFF4ECDC4),
    RED(0xFFE07A5F),
    ORANGE(0xFFF2CC8F),
    YELLOW(0xFFF4D35E),
    GREEN(0xFF81B29A),
    MINT(0xFF7BEBC4),
    TEAL(0xFF5BC0BE),
    CYAN(0xFF64B6AC),
    BLUE(0xFF6A9EC7),
    INDIGO(0xFF8577C1),
    PURPLE(0xFFB07CD8),
    GRAY(0xFFA0A0B0)
}

// ── StudyHierarchyRule ────────────────────────────────────────────────────

data class StudyHierarchyRule(
    val id: String = "course-view",
    val name: String = "课程视图",
    val levels: List<String> = listOf("type", "subject", "chapter", "topic")
) {
    companion object {
        val defaultCourseView = StudyHierarchyRule()
        const val uncategorizedValue = StudyFilingPath.uncategorizedTitle
        const val missingValue = StudyFilingPath.missingTitle
    }
}

// ── StudyBrowsePath ───────────────────────────────────────────────────────

data class StudyBrowsePath(
    val components: List<String> = emptyList()
) {
    val isRoot: Boolean get() = components.isEmpty()
    val depth: Int get() = components.size

    val storageKey: String get() = components.joinToString("")

    val parent: StudyBrowsePath
        get() = if (components.isEmpty()) this
            else StudyBrowsePath(components.dropLast(1))

    val isUncategorizedTypeSelection: Boolean
        get() = components.size == 1 && components.first() == StudyHierarchyRule.uncategorizedValue

    fun appending(value: String): StudyBrowsePath =
        StudyBrowsePath(components + value)

    fun truncatedTo(depth: Int): StudyBrowsePath =
        StudyBrowsePath(components.take(depth.coerceAtLeast(0)))
}

// ── StudyBrowseFolder ─────────────────────────────────────────────────────

data class StudyBrowseFolder(
    val id: String,
    val folderID: String? = null,
    val levelKey: String,
    val title: String,
    val itemCount: Int = 0,
    val path: StudyBrowsePath = StudyBrowsePath(),
    val colorToken: StudyFolderColorToken? = null
) {
    val isFallback: Boolean
        get() = title == StudyHierarchyRule.uncategorizedValue || title == StudyHierarchyRule.missingValue
}

// ── StudyBrowseContent ────────────────────────────────────────────────────

data class StudyBrowseContent(
    val path: StudyBrowsePath = StudyBrowsePath(),
    val folders: List<StudyBrowseFolder> = emptyList(),
    val items: List<StudyItemMetadata> = emptyList()
) {
    val isEmpty: Boolean get() = folders.isEmpty() && items.isEmpty()
    val showsRecordings: Boolean get() = folders.isEmpty()
}

// ── StudyLibraryBrowser ───────────────────────────────────────────────────

object StudyLibraryBrowser {
    private val levelKeys = listOf("type", "subject", "chapter", "topic")

    fun content(
        items: List<StudyItemMetadata>,
        folders: List<StudyFolderMetadata> = emptyList(),
        path: StudyBrowsePath
    ): StudyBrowseContent {
        val matchingItems = items.filter { itemMatches(it, path) }

        if (path.depth >= levelKeys.size || path.isUncategorizedTypeSelection) {
            return StudyBrowseContent(path = path, folders = emptyList(), items = sortedItems(matchingItems))
        }

        val nextLevelKey = levelKeys[path.depth]
        val nextLevel = StudyFolderMetadata.levelForDepth(path.depth)
        val grouped = matchingItems.groupBy { displayValue(it, nextLevelKey) }
        val browseFoldersByPath = mutableMapOf<String, StudyBrowseFolder>()

        for ((title, groupedItems) in grouped) {
            val folderPath = path.appending(title)
            browseFoldersByPath[folderPath.storageKey] = StudyBrowseFolder(
                id = "${path.components.joinToString("/")}/$nextLevelKey=$title",
                folderID = null,
                levelKey = nextLevelKey,
                title = title,
                itemCount = groupedItems.size,
                path = folderPath,
                colorToken = null
            )
        }

        if (nextLevel != null) {
            for (folder in folders) {
                if (folderMatchesParent(folder, parentPath = path, nextLevel = nextLevel)) {
                    if (folder.isTrashed) continue
                    val folderPath = StudyBrowsePath(components = folder.pathComponents)
                    val itemCount = items.count { itemMatches(it, folderPath) }
                    browseFoldersByPath[folderPath.storageKey] = StudyBrowseFolder(
                        id = folder.folderID,
                        folderID = folder.folderID,
                        levelKey = folder.level.key,
                        title = folder.name,
                        itemCount = itemCount,
                        path = folderPath,
                        colorToken = folder.colorToken
                    )
                }
            }
        }

        return StudyBrowseContent(
            path = path,
            folders = browseFoldersByPath.values.sortedWith(folderComparator),
            items = emptyList()
        )
    }

    fun breadcrumbs(path: StudyBrowsePath): List<Pair<String, StudyBrowsePath>> {
        val result = mutableListOf("学习库" to StudyBrowsePath())
        for (i in path.components.indices) {
            val componentPath = path.truncatedTo(i + 1)
            result.add(path.components[i] to componentPath)
        }
        return result
    }

    fun levelTitle(path: StudyBrowsePath): String {
        if (path.depth >= levelKeys.size || path.isUncategorizedTypeSelection) return "录音"
        return when (levelKeys[path.depth]) {
            "type" -> "门类"
            "subject" -> "课程"
            "chapter" -> "章节"
            "topic" -> "主题"
            else -> "文件夹"
        }
    }

    fun itemMatches(item: StudyItemMetadata, path: StudyBrowsePath): Boolean {
        if (path.depth > levelKeys.size) return false
        for ((index, component) in path.components.withIndex()) {
            if (displayValue(item, levelKeys[index]) != component) return false
        }
        return true
    }

    private fun displayValue(item: StudyItemMetadata, levelKey: String): String {
        item.filingPath.valueFor(levelKey)?.let { if (it.isNotEmpty()) return it }
        return if (StudyTag.normalizedNamespace(levelKey) == "type")
            StudyHierarchyRule.uncategorizedValue else StudyHierarchyRule.missingValue
    }

    private fun folderMatchesParent(
        folder: StudyFolderMetadata,
        parentPath: StudyBrowsePath,
        nextLevel: StudyFolderLevel
    ): Boolean {
        if (folder.level != nextLevel) return false
        val folderComponents = folder.pathComponents
        if (folderComponents.size != parentPath.depth + 1) return false
        return folderComponents.take(parentPath.depth) == parentPath.components
    }

    private val folderComparator = Comparator<StudyBrowseFolder> { left, right ->
        if (left.isFallback != right.isFallback) return@Comparator if (left.isFallback) 1 else -1
        left.title.compareTo(right.title, ignoreCase = true)
    }

    private fun sortedItems(items: List<StudyItemMetadata>): List<StudyItemMetadata> =
        items.sortedWith(compareByDescending<StudyItemMetadata> { it.createdAt }
            .thenBy { it.title })
}

// ── StudyFilingSelectionDraft ─────────────────────────────────────────────

data class StudyFilingSelectionDraft(
    var type: String = "",
    var subject: String = "",
    var chapter: String = "",
    var topic: String = ""
) {
    constructor(path: StudyFilingPath) : this(
        type = path.type ?: "",
        subject = path.subject ?: "",
        chapter = path.chapter ?: "",
        topic = path.topic ?: ""
    )

    val filingPath: StudyFilingPath
        get() = StudyFilingPath(
            type = type.ifEmpty { null },
            subject = subject.ifEmpty { null },
            chapter = chapter.ifEmpty { null },
            topic = topic.ifEmpty { null }
        )

    fun valueFor(level: StudyFolderLevel): String = when (level) {
        StudyFolderLevel.TYPE -> type
        StudyFolderLevel.SUBJECT -> subject
        StudyFolderLevel.CHAPTER -> chapter
        StudyFolderLevel.TOPIC -> topic
        StudyFolderLevel.CUSTOM -> ""
    }

    fun select(level: StudyFolderLevel, value: String) {
        val normalized = StudyFilingPath.normalized(value) ?: ""
        when (level) {
            StudyFolderLevel.TYPE -> { type = normalized; subject = ""; chapter = ""; topic = "" }
            StudyFolderLevel.SUBJECT -> { subject = normalized; chapter = ""; topic = "" }
            StudyFolderLevel.CHAPTER -> { chapter = normalized; topic = "" }
            StudyFolderLevel.TOPIC -> { topic = normalized }
            StudyFolderLevel.CUSTOM -> {}
        }
    }

    fun parentBrowsePath(level: StudyFolderLevel): StudyBrowsePath? = when (level) {
        StudyFolderLevel.TYPE -> StudyBrowsePath()
        StudyFolderLevel.SUBJECT -> if (type.isEmpty()) null else StudyBrowsePath(listOf(type))
        StudyFolderLevel.CHAPTER -> if (type.isEmpty() || subject.isEmpty()) null
            else StudyBrowsePath(listOf(type, subject))
        StudyFolderLevel.TOPIC -> if (type.isEmpty() || subject.isEmpty() || chapter.isEmpty()) null
            else StudyBrowsePath(listOf(type, subject, chapter))
        StudyFolderLevel.CUSTOM -> null
    }
}

// ── StudyFilingSelectionFlow ──────────────────────────────────────────────

object StudyFilingSelectionFlow {
    fun nextLevelAfterCommit(level: StudyFolderLevel): StudyFolderLevel? = when (level) {
        StudyFolderLevel.TYPE -> StudyFolderLevel.SUBJECT
        StudyFolderLevel.SUBJECT -> StudyFolderLevel.CHAPTER
        StudyFolderLevel.CHAPTER -> StudyFolderLevel.TOPIC
        StudyFolderLevel.TOPIC, StudyFolderLevel.CUSTOM -> null
    }
}

// ── StudyFilingCandidateResolver ──────────────────────────────────────────

object StudyFilingCandidateResolver {
    fun candidates(
        level: StudyFolderLevel,
        current: StudyFilingPath,
        items: List<StudyItemMetadata>,
        folders: List<StudyFolderMetadata>
    ): List<String> {
        if (level == StudyFolderLevel.CUSTOM) return emptyList()

        val values = mutableListOf<String>()
        for (item in items) {
            if (matchesAncestors(item.filingPath, level, current)) {
                if (level == StudyFolderLevel.TYPE) {
                    values.add(item.filingPath.type ?: StudyHierarchyRule.uncategorizedValue)
                } else {
                    item.filingPath.valueFor(level)?.let { values.add(it) }
                }
            }
        }

        for (folder in folders) {
            if (folder.level == level && matchesAncestors(folder.path, level, current)) {
                values.add(folder.name)
            }
        }

        if (level == StudyFolderLevel.TYPE) {
            values.add(StudyHierarchyRule.uncategorizedValue)
        }

        return sortedUnique(values)
    }

    private fun matchesAncestors(
        candidate: StudyFilingPath,
        level: StudyFolderLevel,
        current: StudyFilingPath
    ): Boolean = when (level) {
        StudyFolderLevel.TYPE -> true
        StudyFolderLevel.SUBJECT -> candidate.type == current.type
        StudyFolderLevel.CHAPTER -> candidate.type == current.type && candidate.subject == current.subject
        StudyFolderLevel.TOPIC -> candidate.type == current.type && candidate.subject == current.subject && candidate.chapter == current.chapter
        StudyFolderLevel.CUSTOM -> false
    }

    private fun sortedUnique(values: List<String>): List<String> {
        val seen = mutableSetOf<String>()
        val result = mutableListOf<String>()
        for (value in values) {
            val normalized = StudyFilingPath.normalized(value) ?: continue
            val key = normalized.lowercase()
            if (seen.contains(key)) continue
            seen.add(key)
            result.add(normalized)
        }
        return result.sortedWith(String.CASE_INSENSITIVE_ORDER)
    }
}

// ── StudyPathSanitizer ────────────────────────────────────────────────────

object StudyPathSanitizer {
    fun sanitizedPathComponent(value: String): String {
        val sanitized = sanitizedFileName(value)
            .replace(".", "_")
            .trim('_', '-', ' ')
        return sanitized.ifEmpty { "recording" }
    }

    private fun sanitizedFileName(value: String?): String {
        val rawName = value?.trim() ?: ""
        val lastPathComponent = if (rawName.isEmpty()) "recording"
            else rawName.substringAfterLast("/").ifEmpty { rawName }
        val allowedChars = ('a'..'z') + ('A'..'Z') + ('0'..'9') + listOf('.', '_', '-')
        val sanitized = lastPathComponent.map { if (it in allowedChars) it else '_' }.joinToString("")
        return sanitized
            .replace(Regex("_+"), "_")
            .trim('.', ' ')
    }
}

// ── RecordingReceiveRecord ────────────────────────────────────────────────

data class RecordingReceiveRecord(
    val recordingID: String? = null,
    val sanitizedRecordingID: String? = null,
    val receivedAt: Long? = null,
    val updatedAt: Long? = null,
    val sourceDeviceID: String? = null,
    val sourceDeviceName: String? = null,
    val originalTitle: String? = null,
    val normalizedTitle: String? = null,
    val audioFileName: String? = null,
    val originalAudioFileName: String? = null,
    val metadataFileName: String? = null,
    val status: String? = null,
    val transcriptionStatus: String? = null,
    val noteStatus: String? = null,
    val noteRelativePath: String? = null,
    val noteGeneratedAt: Long? = null,
    val noteProviderID: String? = null,
    val noteModelName: String? = null,
    val processingStatus: String? = null,
    val tags: List<String> = emptyList(),
    val studyFiling: StudyFilingPath? = null,
    val createdAt: Long? = null,
    val duration: Double? = null,
    val fileSize: Long? = null,
    val audioRelativePath: String? = null,
    val metadataRelativePath: String? = null,
    val transcriptRelativePath: String? = null,
    val transcriptMarkdownRelativePath: String? = null,
    val transcriptionProviderID: String? = null,
    val transcriptionModelName: String? = null,
    val transcriptionStartedAt: Long? = null,
    val transcriptionCompletedAt: Long? = null,
    val transcriptionMode: ProcessingMode? = null,
    val transcriptionChunks: List<RecordingTranscriptionChunkRecord>? = null,
    val noteGenerationMode: ProcessingMode? = null,
    val noteSections: List<RecordingNoteSectionRecord>? = null
) {
    companion object {
        fun normalizedNoteStatus(status: String?): String? {
            val normalized = status?.trim() ?: ""
            if (normalized == "notGenerated") return "notStarted"
            return normalized.ifEmpty { null }
        }

    }
}

// ── StudyLibrarySyncSanitizer ─────────────────────────────────────────────

object StudyLibrarySyncSanitizer {
    fun filteredCustomProperties(properties: Map<String, String>): Map<String, String> {
        val blockedKeys = setOf(
            "apikey", "api_key", "secret", "hmac", "pairing",
            "rawresponse", "raw_response", "providerresponse", "provider_response",
            "fulltranscript", "full_transcript", "fullnote", "full_note",
            "prompt", "debug", "rawjson", "raw_json", "localnetworktransfer"
        )
        return properties.filterKeys { key ->
            val normalized = key.lowercase()
            blockedKeys.none { normalized.contains(it) }
        }
    }
}
