package com.rokurics.app.ui.theme

import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color

object RokuricsColors {
    // ── Exact iPhone source parity ──────────────────────────────────
    // From: Rokurics/RokuricsColors.swift
    // iPhone values use adaptive(light:dark:) — Android uses light mode values

    val aqua = Color(0xFF59C7C2)        // iPhone light: rgb(0.35, 0.78, 0.76)
    val mint = Color(0xFF9EE8C7)        // iPhone light: rgb(0.62, 0.91, 0.78)
    val mistGreen = Color(0xFFE3FAF0)   // iPhone light: rgb(0.89, 0.98, 0.94)
    val softTeal = Color(0xFF75B3B5)    // iPhone light: rgb(0.46, 0.70, 0.71)
    val skyCyan = Color(0xFF73C7F0)     // iPhone light: rgb(0.45, 0.78, 0.94)
    val paleAqua = Color(0xFFC4F5E8)    // iPhone light: rgb(0.77, 0.96, 0.91)
    val coral = Color(0xFFE06B6E)       // iPhone light: rgb(0.88, 0.42, 0.43)

    // ── Text colors (iPhone parity) ─────────────────────────────────
    val deepText = Color(0xFF1A4250)    // iPhone light: rgb(0.10, 0.26, 0.29)
    val softText = Color(0xFF638F94)    // iPhone light: rgb(0.39, 0.56, 0.58)
    val tertiaryText = Color(0xFF94B3B8)// iPhone light: rgb(0.58, 0.70, 0.72)

    // ── Glass colors (iPhone parity) ────────────────────────────────
    val glassSurface = Color(0xFFFFFFFF)// iPhone light: rgb(1, 1, 1)
    val glassStroke = Color(0xFFF0FFF9) // iPhone light: rgb(0.94, 1.0, 0.98)
    val shadow = Color(0xFF4AB8A8)      // iPhone light: rgb(0.29, 0.72, 0.66)

    // ── Gradients (iPhone parity) ───────────────────────────────────
    val actionGradient = listOf(
        Color(0xFF4FC2C0),  // light: rgb(0.31, 0.76, 0.75)
        Color(0xFF99E6C2)   // light: rgb(0.60, 0.90, 0.76)
    )

    val pageGradient = listOf(
        Color(0xFFF0FFF8),  // light: rgb(0.94, 1.0, 0.97)
        Color(0xFFDCF8F5),  // light: rgb(0.86, 0.97, 0.96)
        Color(0xFFF2FAFF)   // light: rgb(0.95, 0.98, 1.0)
    )

    val quietGradient = listOf(
        Color(0xFFD9FAF0),  // light: rgb(0.85, 0.98, 0.94)
        Color(0xFFE8FAFF)   // light: rgb(0.91, 0.98, 1.0)
    )

    // ── Old aliases for compatibility ───────────────────────────────
    val white = Color(0xFFFFFFFF)
    val pageBackground = pageGradient[0]

    // ── Brush helpers ───────────────────────────────────────────────
    val actionGradientBrush: Brush = Brush.linearGradient(
        colors = actionGradient,
        start = Offset(0f, 0f),
        end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
    )

    val pageGradientBrush: Brush = Brush.linearGradient(
        colors = pageGradient,
        start = Offset(0f, 0f),
        end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
    )

    val quietGradientBrush: Brush = Brush.linearGradient(
        colors = quietGradient,
        start = Offset(0f, 0f),
        end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
    )
}
