package com.intatis.shared.tools

internal object PageSelection {
    fun parse(raw: String?, pageCount: Int): List<Int> {
        if (pageCount <= 0) {
            return emptyList()
        }

        val trimmed = raw?.trim().orEmpty()
        if (trimmed.isEmpty() || trimmed.lowercase() == "all") {
            return (0 until pageCount).toList()
        }

        val pages = mutableListOf<Int>()
        val seen = HashSet<Int>()

        for (part in trimmed.split(",")) {
            val token = part.trim()
            if (token.isEmpty()) {
                continue
            }

            val dash = token.indexOf("-")
            if (dash >= 0) {
                val left = token.substring(0, dash).trim()
                val right = token.substring(dash + 1).trim()
                val start = left.toIntOrNull()
                val end = right.toIntOrNull()
                if (start == null || end == null || start <= 0 || end <= 0 || start > end) {
                    throw IllegalArgumentException("invalid page range: $token")
                }
                for (page in start..end) {
                    append(page, pageCount, pages, seen)
                }
            } else {
                val page = token.toIntOrNull()
                if (page == null || page <= 0) {
                    throw IllegalArgumentException("invalid page number: $token")
                }
                append(page, pageCount, pages, seen)
            }
        }

        return pages
    }

    internal fun append(oneBased: Int, pageCount: Int, pages: MutableList<Int>, seen: MutableSet<Int>) {
        if (oneBased > pageCount) {
            throw IllegalArgumentException("page $oneBased exceeds document page count $pageCount")
        }

        val zeroBased = oneBased - 1
        if (seen.add(zeroBased)) {
            pages.add(zeroBased)
        }
    }
}

internal fun shellQuote(text: String): String {
    return "'${text.replace(\"'\", \"'\\\\''\")}'"
}

internal fun outputText(
    stdout: String,
    stderr: String,
    exitCode: Int,
    limit: Int = 20_000,
): String {
    var text = stdout
    if (stderr.isNotEmpty()) {
        text += (if (text.isEmpty()) "" else "\n") + "[stderr]\n" + stderr
    }
    text += "\n[exit $exitCode]"

    if (text.length > limit) {
        return text.take(limit) + "\n[truncated]"
    }

    return text
}
