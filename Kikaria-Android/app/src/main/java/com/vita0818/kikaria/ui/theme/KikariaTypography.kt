package com.vita0818.kikaria.ui.theme

import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

/**
 * Kikaria typography system, translated from KikariaTypography.swift.
 *
 * Key design decisions from iOS:
 * - Latin/English text uses serif design (FontFamily.Serif)
 * - Chinese/CJK text uses system default font (FontFamily.Default)
 * - App title "Kikaria" is always serif
 * - Numbers/metric values use serif design
 *
 * This object provides named text styles and a mixed Chinese/serif
 * AnnotatedString builder matching the iOS `KikariaTypography.mixedText` system.
 */
object KikariaTypography {

    // ─── Font Sizes (scalable sp) ───

    /** App title: "Kikaria" — 39sp semibold serif, matching iOS appTitle(size: 39, weight: .semibold). */
    val appTitleSize = 39.sp

    /** Large title for page headers. */
    val chineseLargeTitleSize = 34.sp

    /** Title size for section headers. */
    val chineseTitleSize = 32.sp

    /** Headline size for card titles. */
    val chineseHeadlineSize = 17.sp

    /** Body text size. */
    val chineseBodySize = 15.sp

    /** Button text size. */
    val chineseButtonSize = 17.sp

    /** Caption/label size. */
    val chineseCaptionSize = 12.sp

    /** Tag chip text size. */
    val tagSize = 12.sp

    /** Number/metric value default size. */
    val numberSize = 24.sp

    // ─── Font Families ───

    /** Serif font for Latin/English text and numbers, matching iOS `.design: .serif`. */
    val serifFamily: FontFamily = FontFamily.Serif

    /** Default system font for Chinese/CJK text. */
    val chineseFamily: FontFamily = FontFamily.Default

    // ─── Style Builders ───

    /** App title style: serif, semibold, 39sp. */
    fun appTitleStyle(
        size: androidx.compose.ui.unit.TextUnit = appTitleSize,
        weight: FontWeight = FontWeight.SemiBold
    ) = SpanStyle(
        fontFamily = serifFamily,
        fontWeight = weight,
        fontSize = size
    )

    /** Chinese headline style. */
    fun chineseHeadlineStyle(
        size: androidx.compose.ui.unit.TextUnit = chineseHeadlineSize,
        weight: FontWeight = FontWeight.SemiBold
    ) = SpanStyle(
        fontFamily = chineseFamily,
        fontWeight = weight,
        fontSize = size
    )

    /** Chinese body style. */
    fun chineseBodyStyle(
        size: androidx.compose.ui.unit.TextUnit = chineseBodySize,
        weight: FontWeight = FontWeight.Normal
    ) = SpanStyle(
        fontFamily = chineseFamily,
        fontWeight = weight,
        fontSize = size
    )

    /** Chinese caption style. */
    fun chineseCaptionStyle(
        size: androidx.compose.ui.unit.TextUnit = chineseCaptionSize,
        weight: FontWeight = FontWeight.Medium
    ) = SpanStyle(
        fontFamily = chineseFamily,
        fontWeight = weight,
        fontSize = size
    )

    /** Tag style. */
    fun tagStyle(
        size: androidx.compose.ui.unit.TextUnit = tagSize,
        weight: FontWeight = FontWeight.SemiBold
    ) = SpanStyle(
        fontFamily = chineseFamily,
        fontWeight = weight,
        fontSize = size
    )

    /** Number/metric value style: serif, bold. */
    fun numberStyle(
        size: androidx.compose.ui.unit.TextUnit = numberSize,
        weight: FontWeight = FontWeight.Bold
    ) = SpanStyle(
        fontFamily = serifFamily,
        fontWeight = weight,
        fontSize = size
    )

    /** Serif text style for Latin content. */
    fun serifStyle(
        size: androidx.compose.ui.unit.TextUnit,
        weight: FontWeight = FontWeight.Normal
    ) = SpanStyle(
        fontFamily = serifFamily,
        fontWeight = weight,
        fontSize = size
    )

    // ─── Mixed Text (Chinese + Serif) ───

    /**
     * Builds an AnnotatedString where Chinese/CJK characters use the system font
     * and Latin characters use serif. This matches the iOS `KikariaTypography.mixedText`
     * and `KikariaTypography.mixedRuns` system.
     *
     * Chinese characters are detected via Unicode ranges:
     * - CJK Unified Ideographs: U+4E00–U+9FFF
     * - CJK Extension A: U+3400–U+4DBF
     * - CJK Compatibility Ideographs: U+F900–U+FAFF
     * - CJK Symbols/Punctuation: U+3000–U+303F
     * - Fullwidth Forms: U+FF00–U+FFEF
     * - Common Chinese punctuation: ，。、；：？！""''（）《》【】「」『』—…·￥
     */
    fun mixedText(
        text: String,
        size: androidx.compose.ui.unit.TextUnit = chineseBodySize,
        weight: FontWeight = FontWeight.Normal
    ): AnnotatedString {
        return mixedText(
            text = text,
            chineseStyle = chineseBodyStyle(size, weight),
            serifStyle = serifStyle(size, weight)
        )
    }

    /**
     * Builds mixed text with custom Chinese and serif styles.
     */
    fun mixedText(
        text: String,
        chineseStyle: SpanStyle,
        serifStyle: SpanStyle
    ): AnnotatedString = buildAnnotatedString {
        var currentRun = StringBuilder()
        var currentIsChinese: Boolean? = null

        fun flushRun() {
            if (currentRun.isEmpty()) return
            val style = if (currentIsChinese == true) chineseStyle else serifStyle
            append(AnnotatedString(currentRun.toString(), style))
            currentRun = StringBuilder()
        }

        for (char in text) {
            val isChinese = isChineseCharacter(char) || isChinesePunctuation(char)

            if (currentIsChinese != null && currentIsChinese != isChinese) {
                flushRun()
            }

            currentRun.append(char)
            currentIsChinese = isChinese
        }

        flushRun()
    }

    // ─── Character Classification ───

    /**
     * Chinese/CJK system punctuation characters that should use the system font.
     */
    private val chinesePunctuationChars = setOf(
        '，', '。', '、', '；', '：', '？', '！',
        '"', '"', ''', ''', '（', '）', '《', '》',
        '【', '】', '「', '」', '『', '』', '—', '…', '·', '￥'
    )

    private fun isChinesePunctuation(char: Char): Boolean {
        return char in chinesePunctuationChars
    }

    /**
     * Checks whether a character is a CJK unified ideograph or related symbol.
     * Matches iOS `isChineseSystemScalar(_:)`.
     */
    private fun isChineseCharacter(char: Char): Boolean {
        val code = char.code
        return code in 0x3400..0x4DBF ||   // CJK Extension A
                code in 0x4E00..0x9FFF ||   // CJK Unified Ideographs
                code in 0xF900..0xFAFF ||   // CJK Compatibility Ideographs
                code in 0x20000..0x2A6DF || // CJK Extension B
                code in 0x2A700..0x2B73F || // CJK Extension C
                code in 0x2B740..0x2B81F || // CJK Extension D
                code in 0x2B820..0x2CEAF || // CJK Extension E
                code in 0x2CEB0..0x2EBEF || // CJK Extension F
                code in 0x3000..0x303F ||   // CJK Symbols and Punctuation
                code in 0xFF00..0xFFEF      // Halfwidth and Fullwidth Forms
    }
}
