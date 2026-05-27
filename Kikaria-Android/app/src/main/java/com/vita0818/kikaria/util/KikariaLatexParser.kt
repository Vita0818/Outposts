package com.vita0818.kikaria.util

/**
 * LaTeX math tokenizer, translated from KikariaLatexParser.swift.
 *
 * Scans text and splits it into text, inline-math ($...$),
 * and block-math ($$...$$) tokens. Code spans (```...``` and `...`)
 * are preserved as plain text.
 */
object KikariaLatexParser {

    fun tokenize(text: String): List<LatexToken> {
        return Scanner(text).scan()
    }

    private class Scanner(private val text: String) {
        private val chars = text.toCharArray()
        private var index = 0
        private val textBuffer = StringBuilder()
        private val tokens = mutableListOf<LatexToken>()

        fun scan(): List<LatexToken> {
            while (index < chars.size) {
                when {
                    startsWith("```", index) -> appendCodeSpan("```")
                    chars[index] == '`' -> appendCodeSpan("`")
                    isEscapedDollar(index) -> {
                        textBuffer.append('$')
                        index += 2
                    }
                    chars[index] == '$' -> scanMathToken()
                    else -> {
                        textBuffer.append(chars[index])
                        index++
                    }
                }
            }
            flushText()
            return tokens
        }

        private fun scanMathToken() {
            if (startsWith("$$", index)) {
                scanBlockMath()
            } else {
                scanInlineMath()
            }
        }

        private fun scanBlockMath() {
            val start = index
            val closeIndex = closingDoubleDollarIndex(start + 2)
            if (closeIndex == -1) {
                textBuffer.append(chars.copyOfRange(start, chars.size).concatToString())
                index = chars.size
                return
            }
            val body = chars.copyOfRange(start + 2, closeIndex).concatToString()
            val source = chars.copyOfRange(start, closeIndex + 2).concatToString()
            flushText()
            tokens.add(LatexToken.BlockMath(source, body))
            index = closeIndex + 2
        }

        private fun scanInlineMath() {
            val start = index
            val closeIndex = closingSingleDollarIndex(start + 1)
            if (closeIndex == -1) {
                textBuffer.append(chars.copyOfRange(start, chars.size).concatToString())
                index = chars.size
                return
            }
            val body = chars.copyOfRange(start + 1, closeIndex).concatToString()
            val source = chars.copyOfRange(start, closeIndex + 1).concatToString()
            flushText()
            tokens.add(LatexToken.InlineMath(source, body))
            index = closeIndex + 1
        }

        private fun appendCodeSpan(fence: String) {
            val start = index
            index += fence.length
            while (index < chars.size) {
                if (startsWith(fence, index)) {
                    index += fence.length
                    textBuffer.append(chars.copyOfRange(start, index).concatToString())
                    return
                }
                index++
            }
            textBuffer.append(chars.copyOfRange(start, chars.size).concatToString())
        }

        private fun closingDoubleDollarIndex(startIndex: Int): Int {
            var i = startIndex
            while (i < chars.size - 1) {
                if (startsWith("$$", i) && !isEscaped(i)) return i
                i++
            }
            return -1
        }

        private fun closingSingleDollarIndex(startIndex: Int): Int {
            var i = startIndex
            while (i < chars.size) {
                if (chars[i] == '\n') return -1
                if (chars[i] == '$' && !isEscaped(i) && !startsWith("$$", i)) return i
                i++
            }
            return -1
        }

        private fun flushText() {
            if (textBuffer.isEmpty()) return
            tokens.add(LatexToken.Text(textBuffer.toString()))
            textBuffer.clear()
        }

        private fun isEscapedDollar(pos: Int): Boolean {
            return pos + 1 < chars.size && chars[pos] == '\\' && chars[pos + 1] == '$'
        }

        private fun isEscaped(pos: Int): Boolean {
            var slashCount = 0
            var i = pos - 1
            while (i >= 0 && chars[i] == '\\') {
                slashCount++
                i--
            }
            return slashCount % 2 == 1
        }

        private fun startsWith(marker: String, pos: Int): Boolean {
            if (pos < 0 || pos + marker.length > chars.size) return false
            for (offset in marker.indices) {
                if (chars[pos + offset] != marker[offset]) return false
            }
            return true
        }
    }
}
