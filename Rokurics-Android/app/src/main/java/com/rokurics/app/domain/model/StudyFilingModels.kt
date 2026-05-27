package com.rokurics.app.domain.model

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
            return parts.ifEmpty { listOf("未分类") }.joinToString(" / ")
        }

    fun valueFor(level: StudyFilingLevel): String? = when (level) {
        StudyFilingLevel.TYPE -> type
        StudyFilingLevel.SUBJECT -> subject
        StudyFilingLevel.CHAPTER -> chapter
        StudyFilingLevel.TOPIC -> topic
    }

    companion object {
        fun normalized(value: String?): String? =
            value?.trim()?.ifEmpty { null }
    }
}

enum class StudyFilingLevel(val title: String) {
    TYPE("门类"),
    SUBJECT("课程"),
    CHAPTER("章节"),
    TOPIC("主题")
}

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
    val sourceDescription: String? = null,
    val isTrashed: Boolean = false,
    val trashedAt: Long? = null,
    val modifiedByDeviceID: String? = null,
    val syncConflictStatus: String? = null
) {
    val filingPath: StudyFilingPath get() = filing
    val hasTranscript: Boolean get() = transcriptMarkdownRelativePath != null || transcriptRelativePath != null
    val hasNote: Boolean get() = noteRelativePath != null
}

enum class StudyItemKind {
    RECORDING_BUNDLE,
    STANDALONE_NOTE
}

data class StudyTag(
    val id: String,
    val namespace: String = "custom",
    val value: String,
    val displayName: String? = null,
    val createdAt: Long? = null
) {
    val displayTitle: String get() = displayName ?: value
}

data class StudyFolderMetadata(
    val folderID: String,
    val name: String,
    val level: StudyFolderLevel = StudyFolderLevel.CUSTOM,
    val path: StudyFilingPath = StudyFilingPath(),
    val parentFolderID: String? = null,
    val childFolderIDs: List<String> = emptyList(),
    val itemIDs: List<String> = emptyList(),
    val colorToken: StudyFolderColorToken = StudyFolderColorToken.DEFAULT,
    val createdAt: Long = System.currentTimeMillis(),
    val updatedAt: Long = System.currentTimeMillis(),
    val isTrashed: Boolean = false,
    val trashedAt: Long? = null
)

enum class StudyFolderLevel(val title: String) {
    TYPE("门类"),
    SUBJECT("课程"),
    CHAPTER("章节"),
    TOPIC("主题"),
    CUSTOM("文件夹")
}

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

data class StudyBrowsePath(
    val components: List<String> = emptyList()
) {
    val isRoot: Boolean get() = components.isEmpty()
    val depth: Int get() = components.size
    val parent: StudyBrowsePath get() = StudyBrowsePath(components.dropLast(1))

    fun appending(value: String): StudyBrowsePath =
        StudyBrowsePath(components + value)

    fun truncatedTo(depth: Int): StudyBrowsePath =
        StudyBrowsePath(components.take(depth.coerceAtLeast(0)))
}

data class StudyBrowseFolder(
    val id: String,
    val folderID: String? = null,
    val levelKey: String,
    val title: String,
    val itemCount: Int = 0,
    val path: StudyBrowsePath = StudyBrowsePath(),
    val colorToken: StudyFolderColorToken? = null,
    val isFallback: Boolean = false
)

data class StudyBrowseContent(
    val path: StudyBrowsePath = StudyBrowsePath(),
    val folders: List<StudyBrowseFolder> = emptyList(),
    val items: List<StudyItemMetadata> = emptyList()
) {
    val isEmpty: Boolean get() = folders.isEmpty() && items.isEmpty()
    val showsRecordings: Boolean get() = folders.isEmpty()
}
