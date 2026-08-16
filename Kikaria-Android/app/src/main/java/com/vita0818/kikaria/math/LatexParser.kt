package com.vita0818.kikaria.math

/**
 * $...$ 行内公式与 $$...$$ 块级公式的词法扫描,规则逐行对齐 KikariaLatexParser.swift。
 * 代码围栏(``` 与 `)内容按纯文本处理;\$ 为字面美元符;闭合 $ 前奇数个反斜杠视为转义;
 * 行内公式不得跨行;找不到闭合时整段回退为纯文本。
 */
sealed class LatexToken {
    data class Text(val text: String) : LatexToken()
    data class InlineMath(val source: String, val body: String) : LatexToken()
    data class BlockMath(val source: String, val body: String) : LatexToken()
}

object LatexParser {

    fun tokenize(text: String): List<LatexToken> = Scanner(text).scan()

    private class Scanner(text: String) {
        private val chars = text.toCharArray()
        private var index = 0
        private val textBuffer = StringBuilder()
        private val tokens = mutableListOf<LatexToken>()

        fun scan(): List<LatexToken> {
            while (index < chars.size) {
                when {
                    startsWith("```") -> appendCodeSpan("```")
                    chars[index] == '`' -> appendCodeSpan("`")
                    isEscapedDollar(index) -> {
                        textBuffer.append('$')
                        index += 2
                    }
                    chars[index] == '$' -> scanMathToken()
                    else -> {
                        textBuffer.append(chars[index])
                        index += 1
                    }
                }
            }
            flushText()
            return tokens
        }

        private fun scanMathToken() {
            if (startsWith("$$")) scanBlockMath() else scanInlineMath()
        }

        private fun scanBlockMath() {
            val start = index
            val close = closingDoubleDollarIndex(start + 2)
            if (close == null) {
                textBuffer.append(chars, start, chars.size)
                index = chars.size
                return
            }
            val body = String(chars, start + 2, close - (start + 2))
            val source = String(chars, start, (close + 2) - start)
            flushText()
            tokens.add(LatexToken.BlockMath(source, body))
            index = close + 2
        }

        private fun scanInlineMath() {
            val start = index
            val close = closingSingleDollarIndex(start + 1)
            if (close == null) {
                textBuffer.append(chars, start, chars.size)
                index = chars.size
                return
            }
            val body = String(chars, start + 1, close - (start + 1))
            val source = String(chars, start, (close + 1) - start)
            flushText()
            tokens.add(LatexToken.InlineMath(source, body))
            index = close + 1
        }

        private fun appendCodeSpan(fence: String) {
            val start = index
            index += fence.length
            while (index < chars.size) {
                if (startsWith(fence, index)) {
                    index += fence.length
                    textBuffer.append(chars, start, index)
                    return
                }
                index += 1
            }
            textBuffer.append(chars, start, chars.size)
        }

        private fun closingDoubleDollarIndex(startIndex: Int): Int? {
            var i = startIndex
            while (i < chars.size - 1) {
                if (startsWith("$$", i) && !isEscaped(i)) return i
                i += 1
            }
            return null
        }

        private fun closingSingleDollarIndex(startIndex: Int): Int? {
            var i = startIndex
            while (i < chars.size) {
                if (chars[i] == '\n') return null
                if (chars[i] == '$' && !isEscaped(i) && !startsWith("$$", i)) return i
                i += 1
            }
            return null
        }

        private fun flushText() {
            if (textBuffer.isNotEmpty()) {
                tokens.add(LatexToken.Text(textBuffer.toString()))
                textBuffer.setLength(0)
            }
        }

        private fun isEscapedDollar(i: Int): Boolean =
            i + 1 < chars.size && chars[i] == '\\' && chars[i + 1] == '$'

        private fun isEscaped(i: Int): Boolean {
            var slashes = 0
            var j = i - 1
            while (j >= 0 && chars[j] == '\\') {
                slashes += 1
                j -= 1
            }
            return slashes % 2 == 1
        }

        private fun startsWith(marker: String, at: Int = index): Boolean {
            if (at < 0 || at + marker.length > chars.size) return false
            for (k in marker.indices) if (chars[at + k] != marker[k]) return false
            return true
        }
    }
}
