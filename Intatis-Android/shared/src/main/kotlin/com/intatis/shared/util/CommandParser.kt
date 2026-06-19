package com.intatis.shared.util

object CommandParser {
    private val allowedReasoning = setOf("minimal", "low", "medium", "high")

    fun parseTokens(input: String): List<String> {
        if (input.isBlank()) return emptyList()

        val tokens = mutableListOf<String>()
        val token = StringBuilder()
        var inQuotes = false
        var escaping = false

        for (ch in input) {
            when {
                escaping -> {
                    token.append(ch)
                    escaping = false
                }
                ch == '\\' -> escaping = true
                ch == '"' -> inQuotes = !inQuotes
                ch.isWhitespace() && !inQuotes -> {
                    if (token.isNotEmpty()) {
                        tokens.add(token.toString())
                        token.clear()
                    }
                }
                else -> token.append(ch)
            }
        }

        if (token.isNotEmpty()) tokens.add(token.toString())
        return tokens
    }

    fun expandTilde(path: String): String {
        if (path == "~") return System.getProperty("user.home") ?: path
        if (path.startsWith("~/")) return System.getProperty("user.home") + path.substring(1)
        if (path.startsWith("~\\")) return System.getProperty("user.home") + path.substring(1)
        return path
    }

    fun tryNormalizeReasoning(value: String?): Pair<Boolean, String?> {
        val raw = value?.trim() ?: return true to null
        if (raw.equals("off", ignoreCase = true)) return true to null
        return if (allowedReasoning.contains(raw.lowercase())) true to raw.lowercase() else false to null
    }
}
