package com.intatis.shared

import java.nio.file.Paths

private val allowedReasoning = listOf("minimal", "low", "medium", "high")

object CommandParser {
    fun parseTokens(input: String): List<String> {
        if (input.isBlank()) return emptyList()

        val tokens = mutableListOf<String>()
        val token = StringBuilder()
        var inQuotes = false
        var escaping = false

        for (ch in input) {
            if (escaping) {
                token.append(ch)
                escaping = false
                continue
            }
            if (ch == '\\') {
                escaping = true
                continue
            }
            if (ch == '"') {
                inQuotes = !inQuotes
                continue
            }
            if (ch.isWhitespace() && !inQuotes) {
                if (token.isNotEmpty()) {
                    tokens.add(token.toString())
                    token.clear()
                }
            } else {
                token.append(ch)
            }
        }

        if (token.isNotEmpty()) {
            tokens.add(token.toString())
        }
        return tokens
    }

    fun expandTilde(path: String): String {
        if (path.isBlank()) return path
        val home = System.getProperty("user.home")
        if (home.isNullOrBlank()) return path

        if (path == "~") return home
        val sep = java.io.File.separatorChar
        return if (path.startsWith("~$sep") || path.startsWith("~/")) {
            Paths.get(home, path.substring(2)).toString()
        } else {
            path
        }
    }

    fun parseReasoning(value: String?): Pair<Boolean, String?> {
        val normalized = value?.trim()?.lowercase() ?: return true to null
        if (normalized.isEmpty() || normalized == "off") return true to null
        if (normalized in allowedReasoning) return true to normalized
        return false to null
    }
}
