package com.rokurics.app.ui.theme

import androidx.compose.runtime.Composable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color

/** Returns the adaptive color for dark/light mode. Apple parity: adaptive(light:dark:) */
@Composable
fun adaptiveColor(light: Color, dark: Color): Color =
    if (isSystemInDarkTheme()) dark else light

object RokuricsColors {
    // ── Exact iPhone source parity ──────────────────────────────────
    // From: Rokurics/RokuricsColors.swift

    // Light mode (default, non-Composable)
    val aqua = Color(0xFF59C7C2)
    val mint = Color(0xFF9EE8C7)
    val mistGreen = Color(0xFFE3FAF0)
    val softTeal = Color(0xFF75B3B5)
    val skyCyan = Color(0xFF73C7F0)
    val paleAqua = Color(0xFFC4F5E8)
    val coral = Color(0xFFE06B6E)

    // Dark mode variants (iPhone dark: values)
    val aquaDark = Color(0xFF57D6D1)
    val mintDark = Color(0xFF52BD94)
    val mistGreenDark = Color(0xFF0F2B26)
    val softTealDark = Color(0xFF85CCCC)
    val skyCyanDark = Color(0xFF4DB3EB)
    val paleAquaDark = Color(0xFF266B61)
    val coralDark = Color(0xFFF5757A)

    // ── Text colors ─────────────────────────────────────────────────
    val deepText = Color(0xFF1A4250)
    val softText = Color(0xFF638F94)
    val tertiaryText = Color(0xFF94B3B8)

    val deepTextDark = Color(0xFFE6FAF8)
    val softTextDark = Color(0xFFA8D1D1)
    val tertiaryTextDark = Color(0xFF759EA0)

    // ── Glass colors ────────────────────────────────────────────────
    val glassSurface = Color(0xFFFFFFFF)
    val glassStroke = Color(0xFFF0FFF9)
    val shadow = Color(0xFF4AB8A8)

    val glassSurfaceDark = Color(0xFF0D2424)
    val glassStrokeDark = Color(0xFF8ADBD1)
    val shadowDark = Color(0xFF000808)

    // ── Gradients ───────────────────────────────────────────────────
    val actionGradientLight = listOf(Color(0xFF4FC2C0), Color(0xFF99E6C2))
    val actionGradientDark = listOf(Color(0xFF128080), Color(0xFF2BAB82))
    val actionGradient = actionGradientLight

    val pageGradientLight = listOf(Color(0xFFF0FFF8), Color(0xFFDCF8F5), Color(0xFFF2FAFF))
    val pageGradientDark = listOf(Color(0xFF051414), Color(0xFF0A2B29), Color(0xFF030D12))
    val pageGradient = pageGradientLight

    val quietGradientLight = listOf(Color(0xFFD9FAF0), Color(0xFFE8FAFF))
    val quietGradientDark = listOf(Color(0xFF143833), Color(0xFF0D2933))
    val quietGradient = quietGradientLight

    // ── Old aliases ─────────────────────────────────────────────────
    val white = Color(0xFFFFFFFF)
    val pageBackground = pageGradient[0]

    // ── Brush helpers (light mode, non-Composable) ──────────────────
    val actionGradientBrush: Brush = Brush.linearGradient(
        colors = actionGradientLight,
        start = Offset(0f, 0f),
        end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
    )

    val pageGradientBrush: Brush = Brush.linearGradient(
        colors = pageGradientLight,
        start = Offset(0f, 0f),
        end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
    )

    val quietGradientBrush: Brush = Brush.linearGradient(
        colors = quietGradientLight,
        start = Offset(0f, 0f),
        end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
    )
}

/** Adaptive page gradient that respects dark/light mode. Apple parity: pageGradient with adaptive(light:dark:) */
@Composable
fun adaptivePageGradientBrush(): Brush {
    val colors = if (isSystemInDarkTheme()) RokuricsColors.pageGradientDark
    else RokuricsColors.pageGradientLight
    return Brush.linearGradient(
        colors = colors,
        start = Offset(0f, 0f),
        end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
    )
}
