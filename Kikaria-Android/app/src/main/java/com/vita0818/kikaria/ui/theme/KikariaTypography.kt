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
 *
 * Token functions map to the iOS typography concept names:
 *   appTitle       → "Kikaria" brand wordmark (serif)
 *   pageTitle      → Page-level title (system, Bold, ~32sp)
 *   largeDisplay   → Extra-large metric/hero numbers
 *   cardTitle      → Card heading (serif, SemiBold)
 *   knowledgeTitle → Knowledge point title (serif, SemiBold)
 *   reviewPrompt   → Review prompt/question text
 *   reviewAnswer   → Answer content in review
 *   body           → Standard body text
 *   secondaryBody  → Secondary/supporting body text
 *   caption        → Small label/caption text
 *   buttonText     → Button label text
 *   settingsTitle  → Settings row title
 *   settingsSubtitle → Settings row subtitle
 *   tagText        → Tag chip text
 */
object KikariaTypography {

    // ── Named token helpers (matching iOS semantics) ──

    /** "Kikaria" app title — serif, size-sp, semibold */
    fun appTitle(size: Int = 39): SpanStyle = SpanStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    /** Page title — system font, bold */
    fun pageTitle(size: Int = 32): SpanStyle = SpanStyle(
        fontWeight = FontWeight.Bold,
        fontSize = size.sp
    )

    /** Large display / metric number — serif, bold */
    fun largeDisplay(size: Int = 54): SpanStyle = SpanStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.Bold,
        fontSize = size.sp
    )

    /** Card title — serif, semibold */
    fun cardTitle(size: Int = 20): SpanStyle = SpanStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    /** Knowledge point title — serif, semibold */
    fun knowledgeTitle(size: Int = 24): SpanStyle = SpanStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    /** Review prompt text */
    fun reviewPrompt(size: Int = 17): SpanStyle = SpanStyle(
        fontWeight = FontWeight.Normal,
        fontSize = size.sp
    )

    /** Review answer content */
    fun reviewAnswer(size: Int = 17): SpanStyle = SpanStyle(
        fontWeight = FontWeight.Normal,
        fontSize = size.sp
    )

    /** Standard body text */
    fun body(size: Int = 15): SpanStyle = SpanStyle(
        fontWeight = FontWeight.Normal,
        fontSize = size.sp
    )

    /** Secondary body text */
    fun secondaryBody(size: Int = 14): SpanStyle = SpanStyle(
        fontWeight = FontWeight.Medium,
        fontSize = size.sp
    )

    /** Caption / small label text */
    fun caption(size: Int = 12): SpanStyle = SpanStyle(
        fontWeight = FontWeight.Medium,
        fontSize = size.sp
    )

    /** Button label text */
    fun buttonText(size: Int = 17): SpanStyle = SpanStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    /** Settings row title */
    fun settingsTitle(size: Int = 16): SpanStyle = SpanStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    /** Settings row subtitle */
    fun settingsSubtitle(size: Int = 13): SpanStyle = SpanStyle(
        fontWeight = FontWeight.Medium,
        fontSize = size.sp
    )

    /** Tag chip text */
    fun tagText(size: Int = 12): SpanStyle = SpanStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    // ── Numbers / serif helpers ──

    /** Metric/display number — serif, semibold */
    fun metricNumber(size: Int): SpanStyle = SpanStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = size.sp
    )

    fun numberBold(size: Int): SpanStyle = SpanStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.Bold,
        fontSize = size.sp
    )

    // ── Legacy convenience (kept for backward compatibility) ──

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

    /** Build a serif-only AnnotatedString (no mixed-font splitting) */
    fun serifText(
        text: String,
        size: Int,
        weight: FontWeight = FontWeight.Normal
    ): AnnotatedString = buildAnnotatedString {
        withStyle(SpanStyle(
            fontFamily = FontFamily.Serif,
            fontSize = size.sp,
            fontWeight = weight
        )) {
            append(text)
        }
    }

    // ── Chinese character detection ──

    fun isChineseCharacter(ch: Char): Boolean {
        val code = ch.code
        if (ch in CHINESE_PUNCTUATION) return true
        return isCjkUnicode(code)
    }

    private val CHINESE_PUNCTUATION = setOf(
        '，', '。', '、', '；', '：', '？', '！', '“', '”', '‘', '’',
        '（', '）', '《', '》', '【', '】', '「', '」', '『', '』',
        '—', '…', '·', '￥'
    )

    private fun isCjkUnicode(code: Int): Boolean {
        return code in 0x3400..0x4DBF ||
                code in 0x4E00..0x9FFF ||
                code in 0xF900..0xFAFF ||
                code in 0x20000..0x2A6DF ||
                code in 0x2A700..0x2B73F ||
                code in 0x2B740..0x2B81F ||
                code in 0x2B820..0x2CEAF ||
                code in 0x2CEB0..0x2EBEF ||
                code in 0x3000..0x303F ||
                code in 0xFF00..0xFFEF
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
