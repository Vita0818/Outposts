package com.vita0818.kikaria.math

import android.graphics.Bitmap
import android.graphics.Canvas
import android.util.LruCache
import org.scilab.forge.jlatexmath.TeXConstants
import org.scilab.forge.jlatexmath.TeXFormula
import ru.noties.jlatexmath.awt.AndroidGraphics2D
import ru.noties.jlatexmath.awt.Color as AwtColor

/**
 * 公式位图渲染:jlatexmath-android(与 Apple 版 SwiftMath 同源的排版思路),
 * 归一化规则对齐 KikariaMathFormulaView.swift 的 normalizedLatex。
 */
object MathRenderer {

    enum class Style { INLINE, BLOCK }

    private val cache = object : LruCache<String, Bitmap>(4 * 1024 * 1024) {
        override fun sizeOf(key: String, value: Bitmap): Int = maxOf(1, value.byteCount / 1024)
    }

    fun normalizedLatex(latex: String, style: Style): String {
        var normalized = MathFallback.strippedBlockMathDelimiters(latex)
            .replace("\\dfrac", "\\frac")
            .replace("\\tfrac", "\\frac")
            .replace("\\operatorname{sgn}", "\\mathrm{sgn}")

        if (style == Style.BLOCK) {
            normalized = replacingBracedCommand(normalized, "\\operatorname") { "\\mathrm{$it}" }
            normalized = normalized
                .replace("\\iiint", "\\int\\!\\!\\int\\!\\!\\int")
                .replace("\\iint", "\\int\\!\\!\\int")
        }
        return normalized
    }

    private fun replacingBracedCommand(text: String, command: String, transform: (String) -> String): String {
        val sb = StringBuilder()
        var i = 0
        while (i < text.length) {
            if (text.startsWith(command, i)) {
                val group = bracedGroup(text, i + command.length)
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

    /**
     * 渲染公式为位图;失败(异常或空串)返回 null,由调用方回退到可读文本。
     * sizePx 为字号对应的像素值。
     */
    fun render(latex: String, sizePx: Float, colorArgb: Int, style: Style): Bitmap? {
        val normalized = normalizedLatex(latex, style)
        if (normalized.isBlank()) return null

        val key = "$normalized|$sizePx|$colorArgb|${style.name}"
        cache.get(key)?.let { return it }

        return try {
            val formula = TeXFormula(normalized)
            val icon = formula.createTeXIcon(
                if (style == Style.BLOCK) TeXConstants.STYLE_DISPLAY else TeXConstants.STYLE_TEXT,
                sizePx,
            )
            icon.setForeground(AwtColor(colorArgb))
            val width = icon.iconWidth
            val height = icon.iconHeight
            if (width <= 0 || height <= 0) return null

            val bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888)
            val graphics = AndroidGraphics2D()
            graphics.setCanvas(Canvas(bitmap))
            icon.paintIcon(null, graphics, 0, 0)
            cache.put(key, bitmap)
            bitmap
        } catch (_: Throwable) {
            null
        }
    }
}
