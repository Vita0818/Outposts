package com.vita0818.kikaria.util

import android.content.Context
import com.vita0818.kikaria.data.KnowledgePreset
import java.io.IOException

/**
 * Loads bundled knowledge presets from Android assets/presets/ directory.
 *
 * Translated from the iOS KnowledgePreset.loadBuiltInPresets() which reads
 * from Bundle.main.urls(forResourcesWithExtension:subdirectory:).
 */
object PresetLoader {

    private const val PRESETS_DIR = "presets"

    /**
     * Loads all .md files from assets/presets/ as KnowledgePreset instances.
     * Returns an empty list if no assets are found or an error occurs.
     */
    fun loadPresets(context: Context): List<KnowledgePreset> {
        return try {
            val files = context.assets.list(PRESETS_DIR) ?: return emptyList()
            files
                .filter { it.endsWith(".md") }
                .sorted()
                .mapNotNull { fileName -> loadPreset(context, fileName) }
        } catch (_: IOException) {
            emptyList()
        }
    }

    private fun loadPreset(context: Context, fileName: String): KnowledgePreset? {
        return try {
            val markdownText = context.assets
                .open("$PRESETS_DIR/$fileName")
                .bufferedReader(Charsets.UTF_8)
                .readText()
                .trim()

            val displayName = fileName.removeSuffix(".md")
            val id = "builtin-$displayName"

            KnowledgePreset(
                id = id,
                name = displayName,
                subtitle = "${displayName}知识点",
                description = "由内置 Markdown 文件「$PRESETS_DIR/$fileName」提供的知识点预设。",
                category = "内置预设",
                markdownText = markdownText,
                isBuiltIn = true
            )
        } catch (_: IOException) {
            null
        }
    }
}
