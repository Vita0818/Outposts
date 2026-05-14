package com.vita0818.kikaria.data

/**
 * Knowledge preset model, translated from KnowledgePoint.swift.
 *
 * A preset bundles a collection of knowledge points (stored as raw markdownText)
 * with metadata for display in the preset selection UI.
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
    companion object {
        const val DEFAULT_PRESET_ID = "advanced-math"
    }
}
