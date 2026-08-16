package com.vita0818.kikaria.ui.math

import android.graphics.Bitmap
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.text.InlineTextContent
import androidx.compose.foundation.text.appendInlineContent
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.Placeholder
import androidx.compose.ui.text.PlaceholderVerticalAlign
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.math.LatexParser
import com.vita0818.kikaria.math.LatexToken
import com.vita0818.kikaria.math.MathFallback
import com.vita0818.kikaria.math.MathRenderer
import kotlin.math.max

/**
 * 数学混排文本:块结构对齐 KikariaMathText.swift ——
 * 空行分段;段内行内公式以图片占位混排;仅含单个行内公式的段升级为居中块级公式。
 * 行内公式字号 ×1.02;块级 ×1.34 并 clamp 到 [fontSize+5, fontSize+8]。
 */

private sealed class MathBlock {
    data class Paragraph(val items: List<LatexToken>) : MathBlock()
    data class BlockFormula(val body: String) : MathBlock()
}

private fun buildBlocks(text: String): List<MathBlock> {
    val tokens = LatexParser.tokenize(text)
    val blocks = mutableListOf<MathBlock>()
    var paragraph = mutableListOf<LatexToken>()

    fun endParagraph() {
        if (paragraph.isEmpty()) return
        val nonBlank = paragraph.filterNot { it is LatexToken.Text && it.text.isBlank() }
        when {
            nonBlank.size == 1 && nonBlank[0] is LatexToken.InlineMath ->
                blocks.add(MathBlock.BlockFormula((nonBlank[0] as LatexToken.InlineMath).body))
            nonBlank.isNotEmpty() -> blocks.add(MathBlock.Paragraph(paragraph.toList()))
        }
        paragraph = mutableListOf()
    }

    for (token in tokens) {
        when (token) {
            is LatexToken.BlockMath -> {
                endParagraph()
                blocks.add(MathBlock.BlockFormula(token.body))
            }
            is LatexToken.Text -> {
                val parts = token.text.split(Regex("\n[ \t]*\n+"))
                parts.forEachIndexed { index, part ->
                    if (index > 0) endParagraph()
                    if (part.isNotEmpty()) paragraph.add(LatexToken.Text(part))
                }
            }
            else -> paragraph.add(token)
        }
    }
    endParagraph()
    return blocks
}

fun isCjkChar(c: Char): Boolean {
    val code = c.code
    return (code in 0x3400..0x4DBF) || (code in 0x4E00..0x9FFF) || (code in 0xF900..0xFAFF) ||
        (code in 0x20000..0x2A6DF) || (code in 0x2A700..0x2B73F) || (code in 0x2B740..0x2B81F) ||
        (code in 0x2B820..0x2CEAF) || (code in 0x2CEB0..0x2EBEF) || (code in 0x3000..0x303F) ||
        (code in 0xFF00..0xFFEF)
}

private val chinesePunctuation = "，。、；：？！“”‘’（）《》【】「」『』—…·￥".toSet()

/** 中文走默认无衬线,拉丁/数字走衬线 —— 对齐 mixedText 混排规则。 */
private fun androidx.compose.ui.text.AnnotatedString.Builder.appendMixed(text: String) {
    if (text.isEmpty()) return
    var runStart = -1
    var runIsLatin = false
    for (i in text.indices) {
        val c = text[i]
        val latin = !isCjkChar(c) && c !in chinesePunctuation
        if (runStart == -1) {
            runStart = i
            runIsLatin = latin
        } else if (latin != runIsLatin) {
            appendRun(text, runStart, i, runIsLatin)
            runStart = i
            runIsLatin = latin
        }
    }
    if (runStart != -1) appendRun(text, runStart, text.length, runIsLatin)
}

private fun androidx.compose.ui.text.AnnotatedString.Builder.appendRun(text: String, start: Int, end: Int, latin: Boolean) {
    val slice = text.substring(start, end)
    if (latin) {
        withStyle(SpanStyle(fontFamily = FontFamily.Serif)) { append(slice) }
    } else {
        append(slice)
    }
}

private fun Color.toArgbInt(): Int = android.graphics.Color.argb(
    (alpha * 255).toInt(),
    (red * 255).toInt(),
    (green * 255).toInt(),
    (blue * 255).toInt(),
)

@Composable
fun KikariaMathText(
    text: String,
    fontSize: Int,
    textColor: Color,
    accentColor: Color,
    modifier: Modifier = Modifier,
    lineSpacing: Int = 3,
) {
    val blocks = remember(text) { buildBlocks(text) }
    // 块级字号:fontSize*1.34,clamp [fontSize+5, fontSize+8]
    val blockFontSize = (fontSize * 1.34f).toInt().coerceIn(fontSize + 5, fontSize + 8)

    BoxWithConstraints(modifier) {
        val maxWidthPx = with(LocalDensity.current) { maxWidth.toPx() }

        Column(verticalArrangement = Arrangement.spacedBy(max(lineSpacing + 6, 9).dp)) {
            blocks.forEach { block ->
                when (block) {
                    is MathBlock.BlockFormula -> BlockFormulaView(
                        body = block.body,
                        fontSizePx = with(LocalDensity.current) { blockFontSize.sp.toPx() },
                        textColor = textColor,
                        maxWidthPx = maxWidthPx,
                        fallbackSize = blockFontSize,
                    )

                    is MathBlock.Paragraph -> ParagraphView(
                        items = block.items,
                        fontSize = fontSize,
                        fontSizePx = with(LocalDensity.current) { (fontSize * 1.02f).sp.toPx() },
                        textColor = textColor,
                        accentColor = accentColor,
                        lineSpacing = lineSpacing,
                        density = LocalDensity.current,
                        maxWidthPx = maxWidthPx,
                    )
                }
            }
        }
    }
}

@Composable
private fun BlockFormulaView(
    body: String,
    fontSizePx: Float,
    textColor: Color,
    maxWidthPx: Float,
    fallbackSize: Int,
) {
    val bitmap: Bitmap? = remember(body, fontSizePx, textColor) {
        MathRenderer.render(body, fontSizePx, textColor.toArgbInt(), MathRenderer.Style.BLOCK)
    }
    BoxCentered {
        if (bitmap != null) {
            if (bitmap.width > maxWidthPx) {
                Image(
                    bitmap = bitmap.asImageBitmap(),
                    contentDescription = MathFallback.readableMathFallback(body),
                    contentScale = ContentScale.Fit,
                    modifier = Modifier.fillMaxWidth().aspectRatio(bitmap.width.toFloat() / bitmap.height.toFloat()),
                )
            } else {
                Image(
                    bitmap = bitmap.asImageBitmap(),
                    contentDescription = MathFallback.readableMathFallback(body),
                )
            }
        } else {
            Text(
                text = MathFallback.readableMathFallback(body),
                color = textColor.copy(alpha = 0.82f),
                fontSize = (fallbackSize * 0.9f).sp,
                fontFamily = FontFamily.Serif,
                lineHeight = (fallbackSize * 0.9f + 4f).sp,
            )
        }
    }
}

@Composable
private fun ParagraphView(
    items: List<LatexToken>,
    fontSize: Int,
    fontSizePx: Float,
    textColor: Color,
    accentColor: Color,
    lineSpacing: Int,
    density: androidx.compose.ui.unit.Density,
    maxWidthPx: Float,
) {
    val accentArgb = accentColor.toArgbInt()
    val rendered: List<Pair<LatexToken, Bitmap?>> = remember(items, fontSizePx, accentArgb, density.density, density.fontScale) {
        items.map { token ->
            if (token is LatexToken.InlineMath) {
                token to MathRenderer.render(token.body, fontSizePx, accentArgb, MathRenderer.Style.INLINE)
            } else {
                token to null
            }
        }
    }

    var id = 0
    val inlineContent = HashMap<String, InlineTextContent>()
    val annotated = buildAnnotatedString {
        rendered.forEach { (token, bitmap) ->
            when {
                token is LatexToken.Text -> appendMixed(token.text)
                token is LatexToken.InlineMath && bitmap != null && bitmap.width <= maxWidthPx * 0.92f -> {
                    val key = "math-${id++}"
                    val w = with(density) { bitmap.width.toSp() }
                    val h = with(density) { bitmap.height.toSp() }
                    inlineContent[key] = InlineTextContent(Placeholder(w, h, PlaceholderVerticalAlign.Center)) {
                        Image(bitmap = bitmap.asImageBitmap(), contentDescription = null)
                    }
                    appendInlineContent(key, MathFallback.readableMathFallback(token.body))
                }
                token is LatexToken.InlineMath -> appendMixed(MathFallback.readableMathFallback(token.body))
                else -> Unit
            }
        }
    }

    Text(
        text = annotated,
        color = textColor,
        fontSize = fontSize.sp,
        lineHeight = (fontSize + lineSpacing).sp,
        inlineContent = inlineContent,
    )
}

@Composable
private fun BoxCentered(content: @Composable () -> Unit) {
    androidx.compose.foundation.layout.Box(
        modifier = Modifier.fillMaxWidth().padding(vertical = 6.dp),
        contentAlignment = androidx.compose.ui.Alignment.Center,
    ) { content() }
}
