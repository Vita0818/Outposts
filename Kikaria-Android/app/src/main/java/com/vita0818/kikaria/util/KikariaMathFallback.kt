package com.vita0818.kikaria.util

/**
 * Converts LaTeX content to readable plain text using Unicode math symbols.
 *
 * Translated from the readableMathFallback() logic in KikariaMathFormulaView.swift.
 * When true math rendering is unavailable (no third-party libs), this provides
 * a readable fallback that replaces LaTeX commands with Unicode equivalents.
 */
object KikariaMathFallback {

    /**
     * Converts a LaTeX body string to readable Unicode math text.
     */
    fun convert(body: String): String {
        var result = body.trim()

        // Handle \frac{num}{den}
        result = replaceFrac(result)

        // Handle \sqrt{val}
        result = replaceBracedCommand(result, "\\sqrt") { "√(${convert(it)})" }

        // Handle \operatorname{name} and similar — just keep the name
        for (cmd in listOf("\\operatorname", "\\mathrm", "\\mathbf", "\\text")) {
            result = replaceBracedCommand(result, cmd) { convert(it) }
        }

        // Handle \bar{x} → x̄
        result = replaceBracedCommand(result, "\\bar") { "${convert(it)}̄" }
        // Handle \hat{x} → x̂
        result = replaceBracedCommand(result, "\\hat") { "${convert(it)}̂" }
        // Handle \tilde{x} → x̃
        result = replaceBracedCommand(result, "\\tilde") { "${convert(it)}̃" }
        // Handle \vec{x} → x⃗
        result = replaceBracedCommand(result, "\\vec") { "${convert(it)}⃗" }
        // Handle \dot{x} → ẋ
        result = replaceBracedCommand(result, "\\dot") { "${convert(it)}̇" }

        // Environment delimiters
        val envReplacements = listOf(
            "\\begin{cases}" to "",
            "\\end{cases}" to "",
            "\\begin{aligned}" to "",
            "\\end{aligned}" to "",
            "\\begin{matrix}" to "",
            "\\end{matrix}" to "",
            "\\begin{pmatrix}" to "",
            "\\end{pmatrix}" to "",
            "\\begin{vmatrix}" to "",
            "\\end{vmatrix}" to ""
        )

        for ((src, rep) in envReplacements) {
            result = result.replace(src, rep)
        }

        // Symbol replacements (ordered longest-first to avoid partial matches)
        val symbolReplacements = listOf(
            // Operators
            "\\iiint" to "∫∫∫",
            "\\iint" to "∫∫",
            "\\int" to "∫",
            "\\oint" to "∮",
            "\\sum" to "Σ",
            "\\prod" to "Π",
            "\\coprod" to "∐",
            "\\bigcup" to "∪",
            "\\bigcap" to "∩",
            "\\bigvee" to "∨",
            "\\bigwedge" to "∧",
            "\\bigoplus" to "⊕",
            "\\bigotimes" to "⊗",
            "\\bigodot" to "⊙",
            "\\biguplus" to "⊎",
            "\\bigsqcup" to "⊔",

            // Relations
            "\\approx" to "≈",
            "\\equiv" to "≡",
            "\\neq" to "≠",
            "\\ne" to "≠",
            "\\leq" to "≤",
            "\\le" to "≤",
            "\\geq" to "≥",
            "\\ge" to "≥",
            "\\ll" to "≪",
            "\\gg" to "≫",
            "\\sim" to "∼",
            "\\simeq" to "≃",
            "\\cong" to "≅",
            "\\propto" to "∝",
            "\\parallel" to "∥",
            "\\perp" to "⊥",

            // 方向
            "\\to" to "→",
            "\\mapsto" to "↦",
            "\\implies" to "⇒",
            "\\iff" to "⇔",
            "\\Leftrightarrow" to "⇔",
            "\\Rightarrow" to "⇒",
            "\\Leftarrow" to "⇐",
            "\\rightarrow" to "→",
            "\\leftarrow" to "←",
            "\\uparrow" to "↑",
            "\\downarrow" to "↓",
            "\\longrightarrow" to "⟶",
            "\\longmapsto" to "⟼",

            // 希腊字母 (uppercase)
            "\\Gamma" to "Γ",
            "\\Delta" to "Δ",
            "\\Theta" to "Θ",
            "\\Lambda" to "Λ",
            "\\Xi" to "Ξ",
            "\\Pi" to "Π",
            "\\Sigma" to "Σ",
            "\\Upsilon" to "Υ",
            "\\Phi" to "Φ",
            "\\Psi" to "Ψ",
            "\\Omega" to "Ω",

            // 希腊字母 (lowercase)
            "\\alpha" to "α",
            "\\beta" to "β",
            "\\gamma" to "γ",
            "\\delta" to "δ",
            "\\epsilon" to "ε",
            "\\varepsilon" to "ε",
            "\\zeta" to "ζ",
            "\\eta" to "η",
            "\\theta" to "θ",
            "\\vartheta" to "ϑ",
            "\\iota" to "ι",
            "\\kappa" to "κ",
            "\\lambda" to "λ",
            "\\mu" to "μ",
            "\\nu" to "ν",
            "\\xi" to "ξ",
            "\\pi" to "π",
            "\\varpi" to "ϖ",
            "\\rho" to "ρ",
            "\\varrho" to "ϱ",
            "\\sigma" to "σ",
            "\\varsigma" to "ς",
            "\\tau" to "τ",
            "\\upsilon" to "υ",
            "\\phi" to "φ",
            "\\varphi" to "ϕ",
            "\\chi" to "χ",
            "\\psi" to "ψ",
            "\\omega" to "ω",

            // 集合符号
            "\\emptyset" to "∅",
            "\\varnothing" to "∅",
            "\\subset" to "⊂",
            "\\supset" to "⊃",
            "\\subseteq" to "⊆",
            "\\supseteq" to "⊇",
            "\\in" to "∈",
            "\\notin" to "∉",
            "\\ni" to "∋",
            "\\cup" to "∪",
            "\\cap" to "∩",
            "\\setminus" to "∖",
            "\\forall" to "∀",
            "\\exists" to "∃",
            "\\nexists" to "∄",

            // 微积分
            "\\partial" to "∂",
            "\\nabla" to "∇",
            "\\infty" to "∞",
            "\\lim" to "lim",

            // 其他符号
            "\\cdots" to "⋯",
            "\\ldots" to "…",
            "\\vdots" to "⋮",
            "\\ddots" to "⋱",
            "\\times" to "×",
            "\\cdot" to "·",
            "\\pm" to "±",
            "\\mp" to "∓",
            "\\div" to "÷",
            "\\circ" to "∘",
            "\\bullet" to "•",
            "\\oplus" to "⊕",
            "\\ominus" to "⊖",
            "\\otimes" to "⊗",
            "\\oslash" to "⊘",
            "\\odot" to "⊙",
            "\\star" to "⋆",
            "\\angle" to "∠",
            "\\triangle" to "△",
            "\\square" to "□",
            "\\Box" to "□",
            "\\diamond" to "⋄",
            "\\clubsuit" to "♣",
            "\\diamondsuit" to "♢",
            "\\heartsuit" to "♡",
            "\\spadesuit" to "♠",
            "\\aleph" to "ℵ",
            "\\hbar" to "ℏ",
            "\\ell" to "ℓ",
            "\\wp" to "℘",
            "\\Re" to "ℜ",
            "\\Im" to "ℑ",
            "\\prime" to "′",
            "\\surd" to "√",
            "\\top" to "⊤",
            "\\bot" to "⊥",
            "\\neg" to "¬",
            "\\wedge" to "∧",
            "\\vee" to "∨",

            // 括号缩放
            "\\left" to "",
            "\\right" to "",
            "\\Bigg" to "",
            "\\bigg" to "",
            "\\Big" to "",
            "\\big" to "",

            // Common aliases
            "\\gets" to "←",
            "\\lnot" to "¬",
            "\\land" to "∧",
            "\\lor" to "∨",
            "\\langle" to "⟨",
            "\\rangle" to "⟩",
            "\\lceil" to "⌈",
            "\\rceil" to "⌉",
            "\\lfloor" to "⌊",
            "\\rfloor" to "⌋",
            "\\colon" to " :",
            "\\binom" to "",

            // 空格
            "\\qquad" to "  ",
            "\\quad" to " ",
            "\\," to " ",
            "\\;" to " ",
            "\\:" to " ",
            "\\!" to "",
            "\\\\" to "\n",
            "&" to "  "
        )

        for ((src, rep) in symbolReplacements) {
            result = result.replace(src, rep)
        }

        // Strip remaining backslash commands
        result = result.replace(Regex("\\\\[a-zA-Z]+"), "")
        result = result.replace("\\", "")

        // Normalize whitespace
        result = normalizeWhitespace(result)
        return result
    }

    /**
     * Renders content that may contain LaTeX formulas into a single readable string.
     * Inline math ($...$) is rendered inline; block math ($$...$$) is rendered on its own line.
     */
    fun renderContent(text: String): String {
        val tokens = KikariaLatexParser.tokenize(text)
        val sb = StringBuilder()
        for (token in tokens) {
            when (token) {
                is LatexToken.Text -> sb.append(token.value)
                is LatexToken.InlineMath -> sb.append(convert(token.body))
                is LatexToken.BlockMath -> {
                    if (sb.isNotEmpty() && sb.last() != '\n') sb.append('\n')
                    sb.append(convert(token.body))
                    sb.append('\n')
                }
                is LatexToken.Fallback -> sb.append(token.value)
            }
        }
        return sb.toString().trim()
    }

    private fun replaceFrac(text: String): String {
        var result = text
        for (cmd in listOf("\\dfrac", "\\tfrac", "\\frac")) {
            while (true) {
                val replacement = replaceBracedPair(result, cmd) { num, den ->
                    "(${convert(num)}) / (${convert(den)})"
                } ?: break
                result = replacement
            }
        }
        return result
    }

    private fun replaceBracedPair(text: String, command: String, transform: (String, String) -> String): String? {
        val cmdIdx = text.indexOf(command)
        if (cmdIdx == -1) return null

        var cursor = cmdIdx + command.length
        cursor = skipWhitespace(text, cursor)

        val firstValue = bracedGroup(text, cursor) ?: return null
        cursor += firstValue.length + 2 // +2 for { and }
        cursor = skipWhitespace(text, cursor)

        val secondValue = bracedGroup(text, cursor) ?: return null

        val before = text.substring(0, cmdIdx)
        val after = text.substring(cursor + secondValue.length + 2)
        return before + transform(firstValue, secondValue) + after
    }

    private fun replaceBracedCommand(text: String, command: String, transform: (String) -> String): String {
        var result = text
        while (true) {
            val cmdIdx = result.indexOf(command)
            if (cmdIdx == -1) break

            var cursor = cmdIdx + command.length
            cursor = skipWhitespace(result, cursor)
            val value = bracedGroup(result, cursor)
            if (value == null) {
                // Command without braces — skip it
                result = result.replaceFirst(command, "")
                break
            }

            val before = result.substring(0, cmdIdx)
            val after = result.substring(cursor + value.length + 2)
            result = before + transform(value) + after
        }
        return result
    }

    private fun bracedGroup(text: String, cursor: Int): String? {
        if (cursor >= text.length || text[cursor] != '{') return null
        var i = cursor + 1
        var depth = 1
        while (i < text.length) {
            when (text[i]) {
                '{' -> depth++
                '}' -> {
                    depth--
                    if (depth == 0) return text.substring(cursor + 1, i)
                }
            }
            i++
        }
        return null
    }

    private fun skipWhitespace(text: String, cursor: Int): Int {
        var i = cursor
        while (i < text.length && text[i].isWhitespace()) i++
        return i
    }

    private fun normalizeWhitespace(text: String): String {
        val lines = text.split("\n").map { line ->
            var trimmed = line.trim()
            while (trimmed.contains("  ")) {
                trimmed = trimmed.replace("  ", " ")
            }
            trimmed
        }
        // Remove consecutive blank lines
        val result = mutableListOf<String>()
        for (line in lines) {
            if (line.isEmpty() && result.lastOrNull().isNullOrEmpty()) continue
            result.add(line)
        }
        return result.joinToString("\n")
    }
}
