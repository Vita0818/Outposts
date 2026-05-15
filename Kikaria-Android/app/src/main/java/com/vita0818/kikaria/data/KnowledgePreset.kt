package com.vita0818.kikaria.data

import com.vita0818.kikaria.util.MarkdownParser

/**
 * Knowledge preset model, translated from KnowledgePoint.swift (KnowledgePreset struct).
 *
 * A preset bundles a collection of knowledge points (stored as raw markdownText)
 * with metadata for display in the preset selection UI.
 *
 * Key properties from source:
 * - id: unique preset identifier
 * - name: display name
 * - subtitle: short description line
 * - description: longer description
 * - category: grouping category (e.g., "数学", "英语", "内置预设")
 * - markdownText: raw markdown source for knowledge points
 * - isBuiltIn: whether this preset ships with the app
 * - knowledgePointCount: computed from parsing markdownText
 */
data class KnowledgePreset(
    val id: String,
    val name: String,
    val subtitle: String = "",
    val description: String = "",
    val category: String = "自定义",
    val markdownText: String = "",
    val isBuiltIn: Boolean = false
) {
    /**
     * Number of knowledge points contained in this preset's markdown text.
     * Computed by parsing the markdown, matching iOS `knowledgePointCount` property.
     */
    val knowledgePointCount: Int
        get() = try {
            MarkdownParser.parseKnowledgePoints(markdownText).size
        } catch (_: Exception) {
            0
        }

    companion object {
        /**
         * Default preset ID used when no preset is explicitly selected.
         * Set to the first built-in preset (微积分).
         */
        const val DEFAULT_PRESET_ID = "builtin-微积分"

        /**
         * Schema version for built-in preset seeding.
         * Incremented when built-in presets change to trigger re-seeding.
         * Matches iOS `KnowledgePreset.builtInSeedVersion = 4`.
         */
        const val BUILT_IN_SEED_VERSION = 4
    }
}
