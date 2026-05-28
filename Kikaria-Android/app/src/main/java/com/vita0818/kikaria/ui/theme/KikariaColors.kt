package com.vita0818.kikaria.ui.theme

import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color

/**
 * Kikaria color palette, translated from KikariaTheme in ContentView.swift.
 *
 * The original iOS app uses adaptive (light/dark) colors with a soft,
 * clean, study-focused feel using sky blues, mint greens, and lavender tones.
 */
object KikariaColors {
    // Light palette
    val Sky = Color(0xFF63BAF5)
    val Cyan = Color(0xFF91E0E8)
    val Mist = Color(0xFFE8F7FD)
    val BlueGray = Color(0xFF9EB7CC)
    val MasteredGreen = Color(0xFF5CC28A)
    val MasteredDeepGreen = Color(0xFF1F784D)
    val MasteredCompletedGreen = Color(0xFFC9EDD6)
    val NextAmber = Color(0xFF8A7DBF)
    val RemoveCoral = Color(0xFFDB524D)
    val DeepText = Color(0xFF214054)
    val SoftText = Color(0xFF6B8A9E)
    val TertiaryText = Color(0xFF94ADBF)
    val GlassSurface = Color(0xFFFFFFFF)
    val GlassStrokeAccent = Color(0xFF91E0E8)
    val Shadow = Color(0xFF63BAF5)
    val BubbleMint = Color(0xFFBAF2E6)
    val BubbleLavender = Color(0xFFBFC7FF)
    val BubbleGreen = Color(0xFFC7F2BD)
    val BubbleWhite = Color(0xFFFFFFFF)

    // Dark palette
    val SkyDark = Color(0xFF4DB8F5)
    val CyanDark = Color(0xFF52CCD1)
    val MistDark = Color(0xFF152938)
    val BlueGrayDark = Color(0xFF7A9BB8)
    val MasteredGreenDark = Color(0xFF52D199)
    val MasteredDeepGreenDark = Color(0xFF94F0BD)
    val MasteredCompletedGreenDark = Color(0xFF2E614D)
    val NextAmberDark = Color(0xFF8C75D1)
    val RemoveCoralDark = Color(0xFFFA6B6B)
    val DeepTextDark = Color(0xFFE6F5FF)
    val SoftTextDark = Color(0xFFA8C4DB)
    val TertiaryTextDark = Color(0xFF6E8CA6)
    val GlassSurfaceDark = Color(0xFF1B3448)
    val GlassStrokeAccentDark = Color(0xFF6BD6ED)
    val ShadowDark = Color(0xFF00050D)
    val BubbleMintDark = Color(0xFF33948A)
    val BubbleLavenderDark = Color(0xFF524D94)
    val BubbleGreenDark = Color(0xFF338056)
    val BubbleWhiteDark = Color(0xFF263B54)

    // Gradients
    val PageGradientLight = Brush.linearGradient(
        colors = listOf(
            Color(0xFFEDFAFF),
            Color(0xFFDBF5FA),
            Color(0xFFF5FAFF)
        )
    )

    val PageGradientDark = Brush.linearGradient(
        colors = listOf(
            Color(0xFF05121C),
            Color(0xFF0A2633),
            Color(0xFF030A14)
        )
    )

    val ActionGradientLight = Brush.linearGradient(
        colors = listOf(
            Color(0xFF59B8F8),
            Color(0xFF80DEE3)
        )
    )

    val ActionGradientDark = Brush.linearGradient(
        colors = listOf(
            Color(0xFF1470B3),
            Color(0xFF0F9EA8)
        )
    )

    val MasteredGradientLight = Brush.linearGradient(
        colors = listOf(
            Color(0xFF63C78C),
            Color(0xFFADE8C2)
        )
    )

    val MasteredGradientDark = Brush.linearGradient(
        colors = listOf(
            Color(0xFF1C8A5C),
            Color(0xFF33BF8A)
        )
    )

    val NextGradientLight = Brush.linearGradient(
        colors = listOf(
            Color(0xFFC7B8F0),
            Color(0xFF9487CC)
        )
    )

    val NextGradientDark = Brush.linearGradient(
        colors = listOf(
            Color(0xFF594A94),
            Color(0xFF8066C2)
        )
    )

    val RemoveGradientLight = Brush.linearGradient(
        colors = listOf(
            Color(0xFFE66059),
            Color(0xFFFA9480)
        )
    )

    val RemoveGradientDark = Brush.linearGradient(
        colors = listOf(
            Color(0xFF942420),
            Color(0xFFDB4747)
        )
    )
}
