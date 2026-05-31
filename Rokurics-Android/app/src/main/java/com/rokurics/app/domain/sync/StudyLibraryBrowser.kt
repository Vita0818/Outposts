package com.rokurics.app.domain.sync

import com.rokurics.app.domain.model.*

object StudyLibraryBrowser {

    private const val MAX_DEPTH = 4
    private const val UNCATEGORIZED = "未分类"
    private const val MISSING = "未填写"

    fun content(
        items: List<StudyItemMetadata>,
        folders: List<StudyFolderMetadata>,
        path: StudyBrowsePath
    ): StudyBrowseContent {
        val nonTrashedItems = items.filter { !it.isTrashed }
        val nonTrashedFolders = folders.filter { !it.isTrashed }
        val matchingItems = nonTrashedItems.filter { itemMatches(it, path) }

        if (path.depth >= MAX_DEPTH) {
            return StudyBrowseContent(path = path, items = matchingItems, folders = emptyList())
        }

        val levelKey = levelKeyForDepth(path.depth)
        val levelTitle = folderLevelForDepth(path.depth).title

        // Group items by their filing value at the current level
        val groupedFolders = mutableMapOf<String, MutableList<StudyItemMetadata>>()
        for (item in matchingItems) {
            val value = filingValueAt(item, levelKey)
            val key = value.ifEmpty { UNCATEGORIZED }
            groupedFolders.getOrPut(key) { mutableListOf() }.add(item)
        }

        // Build browse folders from item groups
        val browseFolders = mutableListOf<StudyBrowseFolder>()
        for ((key, groupItems) in groupedFolders) {
            // Try to find a persisted folder that matches
            val persistedFolder = nonTrashedFolders.find { f ->
                filingValueAtPath(f.path, levelKey) == key && folderPathMatches(f.path, path, levelKey)
            }

            browseFolders.add(StudyBrowseFolder(
                id = persistedFolder?.folderID ?: "virtual_${path.depth}_$key",
                folderID = persistedFolder?.folderID,
                levelKey = levelKey,
                title = key,
                itemCount = groupItems.size,
                path = path.appending(key),
                colorToken = persistedFolder?.colorToken
            ))
        }

        // Include persisted folders that have no items but exist at this level
        for (folder in nonTrashedFolders) {
            if (folderMatchesLevel(folder, path, levelKey)) {
                val name = folder.name
                // Don't duplicate virtual folders
                val existing = browseFolders.find { it.folderID == folder.folderID }
                if (existing != null) {
                    continue
                }
                val alreadyCovered = browseFolders.find { it.title == name }
                if (alreadyCovered == null) {
                    browseFolders.add(StudyBrowseFolder(
                        id = folder.folderID,
                        folderID = folder.folderID,
                        levelKey = levelKey,
                        title = name,
                        itemCount = 0,
                        path = path.appending(name),
                        colorToken = folder.colorToken
                    ))
                }
            }
        }

        // Sort: fallback folders last, then localized
        val (fallbackList, normalList) = browseFolders.partition { it.isFallback }
        val sorted = normalList.sortedBy { it.title } + fallbackList.sortedBy { it.title }

        val leafItems = if (path.depth == 0 && matchingItems.isEmpty()) {
            // At root with no items: check if any folder in this path is "未分类"
            matchingItems.filter { it.filingPath.isEmpty || filingValueAt(it, levelKey).isEmpty() }
        } else {
            matchingItems
        }

        return StudyBrowseContent(
            path = path,
            folders = sorted,
            items = if (sorted.isEmpty()) leafItems else emptyList()
        )
    }

    fun breadcrumbs(path: StudyBrowsePath): List<Pair<String, StudyBrowsePath>> {
        val crumbs = mutableListOf<Pair<String, StudyBrowsePath>>()
        crumbs.add("学习库" to StudyBrowsePath())
        for (i in 0 until path.depth) {
            val component = path.components[i]
            crumbs.add(component to path.truncatedTo(i + 1))
        }
        return crumbs
    }

    // ── Internal helpers ──────────────────────────────────

    private fun itemMatches(item: StudyItemMetadata, path: StudyBrowsePath): Boolean {
        if (path.isRoot) return true
        for (i in 0 until path.depth) {
            val levelKey = levelKeyForDepth(i)
            val expected = path.components[i]
            val actual = filingValueAt(item, levelKey)
            if (expected == UNCATEGORIZED) {
                // Uncategorized matches any empty value
                if (actual.isNotEmpty()) return false
            } else {
                if (actual != expected) return false
            }
        }
        return true
    }

    private fun filingValueAt(item: StudyItemMetadata, levelKey: String): String {
        return when (levelKey) {
            "type" -> item.filingPath.type ?: ""
            "subject" -> item.filingPath.subject ?: ""
            "chapter" -> item.filingPath.chapter ?: ""
            "topic" -> item.filingPath.topic ?: ""
            else -> ""
        }
    }

    private fun filingValueAtPath(path: StudyFilingPath, levelKey: String): String {
        return when (levelKey) {
            "type" -> path.type ?: ""
            "subject" -> path.subject ?: ""
            "chapter" -> path.chapter ?: ""
            "topic" -> path.topic ?: ""
            else -> ""
        }
    }

    private fun levelKeyForDepth(depth: Int): String {
        return when (depth) {
            0 -> "type"
            1 -> "subject"
            2 -> "chapter"
            3 -> "topic"
            else -> "custom"
        }
    }

    private fun folderLevelForDepth(depth: Int): StudyFolderLevel {
        return when (depth) {
            0 -> StudyFolderLevel.TYPE
            1 -> StudyFolderLevel.SUBJECT
            2 -> StudyFolderLevel.CHAPTER
            3 -> StudyFolderLevel.TOPIC
            else -> StudyFolderLevel.CUSTOM
        }
    }

    private fun folderMatchesLevel(folder: StudyFolderMetadata, path: StudyBrowsePath, levelKey: String): Boolean {
        if (folder.level.name.lowercase() != levelKey) return false
        return folderPathMatches(folder.path, path, levelKey)
    }

    private fun folderPathMatches(folderPath: StudyFilingPath, browsePath: StudyBrowsePath, targetLevel: String): Boolean {
        val levels = listOf("type", "subject", "chapter", "topic")
        val targetIdx = levels.indexOf(targetLevel)
        if (targetIdx < 0) return true

        for (i in 0 until targetIdx) {
            val expected = browsePath.components.getOrNull(i) ?: return true
            val actual = filingValueAtPath(folderPath, levels[i])
            if (expected == UNCATEGORIZED) continue
            if (actual != expected) return false
        }
        return true
    }
}
