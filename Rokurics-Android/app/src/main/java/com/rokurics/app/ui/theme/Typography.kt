package com.rokurics.app.ui.theme

import androidx.compose.material3.Typography
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp
import androidx.compose.material3.MaterialTheme

val RokuricsTypographyTokens = Typography(
    displayLarge = TextStyle(
        fontSize = 39.sp,
        fontWeight = FontWeight.SemiBold,
        fontFamily = FontFamily.Serif,
        letterSpacing = (-0.5).sp
    ),
    headlineLarge = TextStyle(
        fontSize = 22.sp,
        fontWeight = FontWeight.SemiBold
    ),
    titleLarge = TextStyle(
        fontSize = 39.sp,
        fontWeight = FontWeight.SemiBold,
        fontFamily = FontFamily.Serif,
        letterSpacing = (-0.5).sp
    ),
    titleMedium = TextStyle(
        fontSize = 20.sp,
        fontWeight = FontWeight.SemiBold
    ),
    bodyLarge = TextStyle(
        fontSize = 15.sp,
        fontWeight = FontWeight.Normal
    ),
    bodyMedium = TextStyle(
        fontSize = 13.sp,
        fontWeight = FontWeight.SemiBold
    ),
    labelLarge = TextStyle(
        fontSize = 17.sp,
        fontWeight = FontWeight.SemiBold
    ),
    labelMedium = TextStyle(
        fontSize = 12.sp,
        fontWeight = FontWeight.Medium
    ),
    labelSmall = TextStyle(
        fontSize = 15.sp,
        fontWeight = FontWeight.SemiBold,
        fontFamily = FontFamily.Monospace
    )
)

object RokuricsTypography {
    /** iPhone parity: .system(size:size, weight:weight, design:.serif) */
    fun appTitle(size: Int = 39): TextStyle = TextStyle(
        fontSize = size.sp,
        fontWeight = FontWeight.SemiBold,
        fontFamily = FontFamily.Serif,
        letterSpacing = (-0.5).sp
    )

    /** iPhone parity: .system(size:size, weight:weight) for Chinese body */
    fun caption(size: Int = 13, weight: FontWeight = FontWeight.SemiBold): TextStyle = TextStyle(
        fontSize = size.sp,
        fontWeight = weight
    )

    /** iPhone parity: .system(size:size, weight:weight, design:.serif).monospacedDigit() */
    fun largeNumber(size: Int = 44, weight: FontWeight = FontWeight.Bold): TextStyle = TextStyle(
        fontSize = size.sp,
        fontWeight = weight,
        fontFamily = FontFamily.Serif
    )

    /** iPhone parity: .system(size:17, weight:.semibold) */
    fun sectionTitle(size: Int = 22, weight: FontWeight = FontWeight.SemiBold): TextStyle = TextStyle(
        fontSize = size.sp,
        fontWeight = weight
    )

    /** iPhone parity: .system(size:15, weight:.regular) */
    fun body(size: Int = 15): TextStyle = TextStyle(
        fontSize = size.sp,
        fontWeight = FontWeight.Normal
    )

    /** iPhone parity: .system(size:12, weight:.medium) */
    fun label(size: Int = 12, weight: FontWeight = FontWeight.Medium): TextStyle = TextStyle(
        fontSize = size.sp,
        fontWeight = weight
    )

    /** iPhone parity: .system(size:size, weight:weight, design:.monospaced) */
    fun technical(size: Int = 15, weight: FontWeight = FontWeight.SemiBold): TextStyle = TextStyle(
        fontSize = size.sp,
        fontWeight = weight,
        fontFamily = FontFamily.Monospace
    )

    /** iPhone parity: .system(size:17, weight:.semibold) for button text */
    fun button(size: Int = 17, weight: FontWeight = FontWeight.SemiBold): TextStyle = TextStyle(
        fontSize = size.sp,
        fontWeight = weight
    )
}
