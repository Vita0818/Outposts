package com.vita0818.kikaria.math

/**
 * 公式渲染失败时的可读文本转换,逐条对齐 KikariaMathFormulaView.swift 的 readableMathFallback。
 */
object MathFallback {

    fun readableMathFallback(source: String): String {
        var readable = strippedBlockMathDelimiters(source)
        readable = replacingFractions(readable)
        readable = replacingBracedCommand(readable, "\\sqrt") { "√(${readableMathFallback(it)})" }
        readable = replacingBracedCommand(readable, "\\operatorname") { it }
        readable = replacingBracedCommand(readable, "\\mathrm") { it }
        readable = replacingBracedCommand(readable, "\\mathbf") { it }
        readable = replacingBracedCommand(readable, "\\text") { it }
        readable = replacingBracedCommand(readable, "\\bar") { "${readableMathFallback(it)}\u0304" }

        val replacements = listOf(
            "\\begin{cases}" to "", "\\end{cases}" to "",
            "\\begin{aligned}" to "", "\\end{aligned}" to "",
            "\\begin{matrix}" to "", "\\end{matrix}" to "",
            "\\\\" to "\n", "&" to "  ",
            "\\qquad" to "  ", "\\quad" to " ",
            "\\," to " ", "\\;" to " ", "\\:" to " ", "\\!" to "",
            "\\left" to "", "\\right" to "",
            "\\Bigg" to "", "\\bigg" to "", "\\Big" to "", "\\big" to "",
            "\\iiint" to "∫∫∫", "\\iint" to "∫∫", "\\int" to "∫",
            "\\partial" to "∂", "\\nabla" to "∇",
            "\\Delta" to "Δ", "\\delta" to "δ", "\\theta" to "θ", "\\rho" to "ρ",
            "\\xi" to "ξ", "\\alpha" to "α", "\\beta" to "β", "\\gamma" to "γ",
            "\\lambda" to "λ", "\\mu" to "μ", "\\pi" to "π",
            "\\neq" to "≠", "\\ne" to "≠",
            "\\leq" to "≤", "\\le" to "≤", "\\geq" to "≥", "\\ge" to "≥",
            "\\to" to "→", "\\infty" to "∞",
            "\\cdots" to "⋯", "\\ldots" to "…", "\\times" to "×",
            "\\pm" to "±", "\\mp" to "∓", "\\in" to "∈",
        )
        for ((from, to) in replacements) readable = readable.replace(from, to)

        readable = readable.replace("\\", "")
        return normalizedFallbackWhitespace(readable)
    }

    fun strippedBlockMathDelimiters(source: String): String {
        val trimmed = source.trim()
        if (trimmed.startsWith("$$") && trimmed.endsWith("$$") && trimmed.length >= 4) {
            return trimmed.drop(2).dropLast(2).trim()
        }
        return trimmed
    }

    private fun replacingFractions(text: String): String =
        replacingBracedPairCommand(text, listOf("\\dfrac", "\\tfrac", "\\frac")) { num, den ->
            val n = readableMathFallback(num)
            val d = readableMathFallback(den)
            "${wrappedMathFallbackComponent(n)} / ${wrappedMathFallbackComponent(d)}"
        }

    private fun replacingBracedPairCommand(
        text: String,
        commands: List<String>,
        transform: (String, String) -> String,
    ): String {
        val sb = StringBuilder()
        var i = 0
        while (i < text.length) {
            val command = commands.firstOrNull { text.startsWith(it, i) }
            if (command != null) {
                var cursor = skipWhitespace(text, i + command.length)
                val first = bracedGroup(text, cursor)
                if (first != null) {
                    cursor = skipWhitespace(text, first.second)
                    val second = bracedGroup(text, cursor)
                    if (second != null) {
                        sb.append(transform(first.first, second.first))
                        i = second.second
                        continue
                    }
                }
            }
            sb.append(text[i])
            i += 1
        }
        return sb.toString()
    }

    private fun replacingBracedCommand(text: String, command: String, transform: (String) -> String): String {
        val sb = StringBuilder()
        var i = 0
        while (i < text.length) {
            if (text.startsWith(command, i)) {
                val cursor = skipWhitespace(text, i + command.length)
                val group = bracedGroup(text, cursor)
                if (group != null) {
                    sb.append(transform(group.first))
                    i = group.second
                    continue
                }
            }
            sb.append(text[i])
            i += 1
        }
        return sb.toString()
    }

    /** 返回 (组内容, 结束后光标);不是 { 开头时返回 null。 */
    private fun bracedGroup(text: String, start: Int): Pair<String, Int>? {
        if (start >= text.length || text[start] != '{') return null
        var cursor = start + 1
        val begin = cursor
        var depth = 1
        while (cursor < text.length) {
            when (text[cursor]) {
                '{' -> depth += 1
                '}' -> {
                    depth -= 1
                    if (depth == 0) return text.substring(begin, cursor) to (cursor + 1)
                }
            }
            cursor += 1
        }
        return null
    }

    private fun skipWhitespace(text: String, from: Int): Int {
        var i = from
        while (i < text.length && text[i].isWhitespace()) i += 1
        return i
    }

    private fun wrappedMathFallbackComponent(value: String): String =
        if (value.contains(' ') || value.contains('\n') || value.contains('/')) "($value)" else value

    private fun normalizedFallbackWhitespace(source: String): String {
        val lines = mutableListOf<String>()
        source.split("\n").forEach { raw ->
            var line = raw.trim()
            while (line.contains("  ")) line = line.replace("  ", " ")
            if (line.isEmpty()) {
                if (lines.lastOrNull()?.isEmpty() == false) lines.add(line)
            } else {
                lines.add(line)
            }
        }
        return lines.joinToString("\n")
    }
}
