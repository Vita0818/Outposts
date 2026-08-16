package com.vita0818.kikaria.data

/**
 * Kikaria Markdown 知识点解析,规则逐行对齐 Kikaria-Apple/Kikaria/KnowledgePoint.swift。
 */
object Markdown {

    class NoValidKnowledgePoints : Exception()

    fun parseMarkdown(markdown: String, date: Long = System.currentTimeMillis()): List<KnowledgePoint> {
        val normalized = markdown.replace("\r\n", "\n").replace("\r", "\n")
        val points = splitMarkdownIntoChunks(normalized).mapNotNull { parseChunk(it, date) }
        if (points.isEmpty()) throw NoValidKnowledgePoints()
        return points
    }

    fun markdownText(points: List<KnowledgePoint>): String =
        points.joinToString("\n\n---\n\n") { p ->
            "# ${p.title}\n\ntags: ${p.tags.joinToString(", ")}\n\nhint:\n${p.hint}\n\ncontent:\n${p.content}"
        }

    private fun splitMarkdownIntoChunks(markdown: String): List<String> {
        val chunks = mutableListOf<String>()
        val current = StringBuilder()
        var hasContent = false

        fun flush() {
            val chunk = current.toString().trim()
            if (chunk.isNotEmpty()) chunks.add(chunk)
            current.setLength(0)
            hasContent = false
        }

        markdown.split("\n").forEach { line ->
            if (line.trim() == "---") {
                flush()
            } else {
                if (line.isNotBlank()) hasContent = true
                if (current.isNotEmpty()) current.append('\n')
                current.append(line)
            }
        }
        flush()
        return chunks
    }

    private fun parseChunk(chunk: String, date: Long): KnowledgePoint? {
        val lines = chunk.split("\n")
        val titleIndex = lines.indexOfFirst { it.trim().isNotEmpty() }
        if (titleIndex == -1) return null

        val rawTitle = lines[titleIndex].trim()
        if (!rawTitle.startsWith("#")) return null

        val title = rawTitle.dropWhile { it == '#' }.trim()
        if (title.isEmpty()) return null

        val tags = parseTags(lines)
        val hintIndex = markerIndex("hint:", lines) ?: return null
        val contentIndex = markerIndex("content:", lines) ?: return null
        if (hintIndex >= contentIndex) return null

        val hint = lines.subList(hintIndex + 1, contentIndex).joinToString("\n").trim()
        val content = lines.subList(contentIndex + 1, lines.size).joinToString("\n").trim()
        if (hint.isEmpty() || content.isEmpty()) return null

        return KnowledgePoint(
            id = java.util.UUID.randomUUID().toString(),
            title = title,
            tags = tags,
            hint = hint,
            content = content,
            reinforcementCount = 0,
            lastReinforcedAt = null,
            isMastered = false,
            createdAt = date,
            updatedAt = date,
        )
    }

    private fun parseTags(lines: List<String>): List<String> {
        val tagLine = lines.firstOrNull { it.trim().lowercase().startsWith("tags:") } ?: return emptyList()
        val tagText = tagLine.trim().drop("tags:".length)
        return tagText.split(",", "，")
            .map { it.trim() }
            .filter { it.isNotEmpty() }
    }

    private fun markerIndex(marker: String, lines: List<String>): Int? =
        lines.indexOfFirst { it.trim().lowercase() == marker }.takeIf { it != -1 }
}
