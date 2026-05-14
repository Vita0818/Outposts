package com.vita0818.kikaria.util

import com.vita0818.kikaria.data.KnowledgePoint
import java.util.Date
import java.util.UUID

/**
 * Markdown parser for knowledge points, translated from KnowledgePoint.swift.
 *
 * Format:
 *   # Title
 *   tags: tag1, tag2
 *   hint:
 *   hint text...
 *   content:
 *   content text...
 *   ---
 */
object MarkdownParser {

    fun parseKnowledgePoints(markdown: String, date: Date = Date()): List<KnowledgePoint> {
        val normalized = markdown.replace("\r\n", "\n").replace("\r", "\n")
        val chunks = splitIntoChunks(normalized)
        return chunks.mapNotNull { parseChunk(it, date) }
    }

    fun markdownFromPoints(points: List<KnowledgePoint>): String {
        return points.joinToString("\n\n---\n\n") { point ->
            """
# ${point.title}

tags: ${point.tags.joinToString(", ")}

hint:
${point.hint}

content:
${point.content}
            """.trimIndent()
        }
    }

    private fun splitIntoChunks(markdown: String): List<String> {
        val chunks = mutableListOf<String>()
        val currentLines = mutableListOf<String>()

        for (line in markdown.split("\n")) {
            if (line.trim() == "---") {
                val chunk = currentLines.joinToString("\n").trim()
                if (chunk.isNotEmpty()) {
                    chunks.add(chunk)
                }
                currentLines.clear()
            } else {
                currentLines.add(line)
            }
        }

        val finalChunk = currentLines.joinToString("\n").trim()
        if (finalChunk.isNotEmpty()) {
            chunks.add(finalChunk)
        }

        return chunks
    }

    private fun parseChunk(chunk: String, date: Date): KnowledgePoint? {
        val lines = chunk.split("\n")
        val titleIndex = lines.indexOfFirst { it.trim().isNotEmpty() }
        if (titleIndex == -1) return null

        val rawTitle = lines[titleIndex].trim()
        if (!rawTitle.startsWith("#")) return null

        val title = rawTitle.dropWhile { it == '#' }.trim()
        if (title.isEmpty()) return null

        val tags = parseTags(lines)
        val hintIndex = markerIndex("hint:", lines)
        val contentIndex = markerIndex("content:", lines)

        if (hintIndex == -1 || contentIndex == -1 || hintIndex >= contentIndex) return null

        val hint = lines.slice(hintIndex + 1 until contentIndex)
            .joinToString("\n").trim()
        val content = lines.drop(contentIndex + 1)
            .joinToString("\n").trim()

        if (hint.isEmpty() || content.isEmpty()) return null

        return KnowledgePoint(
            id = UUID.randomUUID().toString(),
            title = title,
            tags = tags,
            hint = hint,
            content = content,
            createdAt = date,
            updatedAt = date
        )
    }

    private fun parseTags(lines: List<String>): List<String> {
        val tagLine = lines.firstOrNull {
            it.trim().lowercase().startsWith("tags:")
        } ?: return emptyList()

        val tagText = tagLine.trim().removePrefix("tags:").removePrefix("Tags:")
        return tagText.split(",", "，")
            .map { it.trim() }
            .filter { it.isNotEmpty() }
    }

    private fun markerIndex(marker: String, lines: List<String>): Int {
        return lines.indexOfFirst {
            it.trim().lowercase() == marker.lowercase()
        }
    }
}
