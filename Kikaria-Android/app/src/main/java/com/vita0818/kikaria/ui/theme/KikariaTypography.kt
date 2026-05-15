package com.vita0818.kikaria.ui.theme

import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.sp

/**
 * Kikaria typography system, translated from KikariaTypography.swift.
 *
 * Provides named text styles that match the iOS serif identity:
 * - App title: serif design (Latin) or system (CJK)
 * - Chinese text: system default font
 * - Numbers: serif design with monospaced digits
 * - Mixed text: Chinese characters use system font, Latin/ASCII uses serif
 *
 * This is a key brand-identity element — Kikaria uses serif for Latin text
 * and system fonts for Chinese/CJK characters.
 */
object KikariaTypography {

    // ── Named font styles (Compose equivalents of iOS Font helpers) ──

    /** "Kikaria" app title — serif, 39sp, semibold */
    val appTitle = FontStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 39.sp
    )

    fun appTitle(size: Int) = FontStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    fun chineseLargeTitle(size: Int = 34) = FontStyle(
        fontWeight = FontWeight.Bold,
        fontSize = size.sp
    )

    fun chineseTitle(size: Int = 32) = FontStyle(
        fontWeight = FontWeight.Bold,
        fontSize = size.sp
    )

    fun chineseHeadline(size: Int = 17) = FontStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    fun chineseBody(size: Int = 15) = FontStyle(
        fontWeight = FontWeight.Normal,
        fontSize = size.sp
    )

    fun chineseButton(size: Int = 17) = FontStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    fun chineseCaption(size: Int = 12) = FontStyle(
        fontWeight = FontWeight.Medium,
        fontSize = size.sp
    )

    fun tag(size: Int = 12) = FontStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    /** Numbers use serif design for the Kikaria look */
    fun number(size: Int) = FontStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    // ── Mixed Chinese/Serif text rendering ──

    /**
     * Builds an AnnotatedString where Chinese/CJK characters and punctuation
     * use the system default font, while Latin/ASCII characters use serif.
     *
     * This matches iOS KikariaTypography.mixedText behavior where the visual
     * identity uses serif for Latin text and system fonts for Chinese.
     */
    fun mixedText(
        text: String,
        chineseStyle: SpanStyle = SpanStyle(),
        serifStyle: SpanStyle = SpanStyle(fontFamily = FontFamily.Serif)
    ): AnnotatedString = buildAnnotatedString {
        var currentRun = StringBuilder()
        var currentIsSerif: Boolean? = null

        fun flushRun() {
            if (currentRun.isEmpty()) return
            val style = if (currentIsSerif == true) serifStyle else chineseStyle
            withStyle(style) {
                append(currentRun.toString())
            }
            currentRun = StringBuilder()
        }

        for (ch in text) {
            val isSerif = !isChineseCharacter(ch)
            if (currentIsSerif != null && currentIsSerif != isSerif) {
                flushRun()
            }
            currentRun.append(ch)
            currentIsSerif = isSerif
        }
        flushRun()
    }

    /**
     * Convenience overload: mixedText with unified size/weight.
     * Chinese gets system font, Latin gets serif — both at the same size/weight.
     */
    fun mixedText(
        text: String,
        size: Int,
        weight: FontWeight = FontWeight.Normal
    ): AnnotatedString = mixedText(
        text = text,
        chineseStyle = SpanStyle(
            fontSize = size.sp,
            fontWeight = weight
        ),
        serifStyle = SpanStyle(
            fontFamily = FontFamily.Serif,
            fontSize = size.sp,
            fontWeight = weight
        )
    )

    // ── Chinese character detection ──

    /**
     * Detects whether a character is a Chinese/CJK character or Chinese punctuation.
     * Uses the same Unicode ranges as the iOS KikariaTypography.
     */
    fun isChineseCharacter(ch: Char): Boolean {
        val code = ch.code
        // Chinese punctuation
        if (ch in CHINESE_PUNCTUATION) return true
        // CJK Unified Ideographs and extensions
        return isCjkUnicode(code)
    }

    private val CHINESE_PUNCTUATION = setOf(
        '，', '。', '、', '；', '：', '？', '！', '“', '”', '‘', '’',
        '（', '）', '《', '》', '【', '】', '「', '」', '『', '』',
        '—', '…', '·', '￥'
    )

    private fun isCjkUnicode(code: Int): Boolean {
        return code in 0x3400..0x4DBF ||      // CJK Extension A
                code in 0x4E00..0x9FFF ||      // CJK Unified Ideographs
                code in 0xF900..0xFAFF ||      // CJK Compatibility Ideographs
                code in 0x20000..0x2A6DF ||    // CJK Extension B
                code in 0x2A700..0x2B73F ||    // CJK Extension C
                code in 0x2B740..0x2B81F ||    // CJK Extension D
                code in 0x2B820..0x2CEAF ||    // CJK Extension E
                code in 0x2CEB0..0x2EBEF ||    // CJK Extension F
                code in 0x3000..0x303F ||      // CJK Symbols and Punctuation
                code in 0xFF00..0xFFEF         // Halfwidth and Fullwidth Forms
    }
}

/**
 * Typography style descriptor used by KikariaTypography.
 * Wraps font family, weight, and size for use with Compose Text.
 */
data class FontStyle(
    val fontFamily: FontFamily? = null,
    val fontWeight: FontWeight = FontWeight.Normal,
    val fontSize: androidx.compose.ui.unit.TextUnit = 16.sp
)
