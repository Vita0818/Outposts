package com.vita0818.kikaria.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color

/**
 * Kikaria 主题,色值逐一对齐 KikariaTheme.swift(格式 Light/Dark)。
 */
data class KikariaColors(
    val sky: Color,
    val cyan: Color,
    val mist: Color,
    val blueGray: Color,
    val masteredGreen: Color,
    val masteredDeepGreen: Color,
    val masteredCompletedGreen: Color,
    val nextAmber: Color,
    val removeCoral: Color,
    val deepText: Color,
    val softText: Color,
    val tertiaryText: Color,
    val glassSurface: Color,
    val glassStrokeAccent: Color,
    val shadow: Color,
    val bubbleMint: Color,
    val bubbleLavender: Color,
    val bubbleGreen: Color,
    val bubbleWhite: Color,
    val pageGradient: List<Color>,
    val actionGradient: List<Color>,
    val masteredGradient: List<Color>,
    val masteredActionGradient: List<Color>,
    val nextGradient: List<Color>,
    val removeGradient: List<Color>,
)

val LightColors = KikariaColors(
    sky = Color(0xFF63BAF5),
    cyan = Color(0xFF92E0E8),
    mist = Color(0xFFE8F7FC),
    blueGray = Color(0xFF9EB8CC),
    masteredGreen = Color(0xFF5CC28A),
    masteredDeepGreen = Color(0xFF1F784D),
    masteredCompletedGreen = Color(0xFFC9EDD6),
    nextAmber = Color(0xFF8A7DBF),
    removeCoral = Color(0xFFDB524D),
    deepText = Color(0xFF214054),
    softText = Color(0xFF6B8A9E),
    tertiaryText = Color(0xFF94ADC2),
    glassSurface = Color(0xFFFFFFFF),
    glassStrokeAccent = Color(0xFF92E0E8),
    shadow = Color(0xFF63BAF5),
    bubbleMint = Color(0xFFBAF2E6),
    bubbleLavender = Color(0xFFBFC7FF),
    bubbleGreen = Color(0xFFC7F2BD),
    bubbleWhite = Color(0xFFFFFFFF),
    pageGradient = listOf(Color(0xFFEDFAFF), Color(0xFFDBF5FA), Color(0xFFF5FAFF)),
    actionGradient = listOf(Color(0xFF59B8F7), Color(0xFF80DEE3)),
    masteredGradient = listOf(Color(0xFF63C78C), Color(0xFFADE8C2)),
    masteredActionGradient = listOf(Color(0xFF40A86B), Color(0xFF8AD1A1)),
    nextGradient = listOf(Color(0xFFC7B8F0), Color(0xFF9487CC)),
    removeGradient = listOf(Color(0xFFE66159), Color(0xFFFA9480)),
)

val DarkColors = KikariaColors(
    sky = Color(0xFF4DB8F5),
    cyan = Color(0xFF52CCD1),
    mist = Color(0xFF142938),
    blueGray = Color(0xFF7A9CB8),
    masteredGreen = Color(0xFF52D199),
    masteredDeepGreen = Color(0xFF94F0BD),
    masteredCompletedGreen = Color(0xFF2E614D),
    nextAmber = Color(0xFF8C75D1),
    removeCoral = Color(0xFFFA6B6B),
    deepText = Color(0xFFE6F5FF),
    softText = Color(0xFFA8C4DB),
    tertiaryText = Color(0xFF6E8CA8),
    glassSurface = Color(0xFF0F212E),
    glassStrokeAccent = Color(0xFF6BD6ED),
    shadow = Color(0xFF00050D),
    bubbleMint = Color(0xFF33948A),
    bubbleLavender = Color(0xFF524D94),
    bubbleGreen = Color(0xFF338057),
    bubbleWhite = Color(0xFF263B54),
    pageGradient = listOf(Color(0xFF05121C), Color(0xFF0A2633), Color(0xFF020A14)),
    actionGradient = listOf(Color(0xFF1470B3), Color(0xFF0F9EA8)),
    masteredGradient = listOf(Color(0xFF1C8A5C), Color(0xFF33BF8A)),
    masteredActionGradient = listOf(Color(0xFF177A54), Color(0xFF2EB37D)),
    nextGradient = listOf(Color(0xFF594A94), Color(0xFF8066C2)),
    removeGradient = listOf(Color(0xFF942429), Color(0xFFDB4747)),
)

@Composable
fun kikariaColors(): KikariaColors = if (isSystemInDarkTheme()) DarkColors else LightColors

fun KikariaColors.brush(gradient: List<Color>): Brush =
    Brush.linearGradient(gradient, start = androidx.compose.ui.geometry.Offset(0f, 0f), end = androidx.compose.ui.geometry.Offset(1f, 1f))

@Composable
fun KikariaTheme(content: @Composable () -> Unit) {
    val dark = isSystemInDarkTheme()
    val scheme = if (dark) {
        darkColorScheme(
            primary = DarkColors.sky,
            onPrimary = Color.White,
            secondary = DarkColors.cyan,
            background = DarkColors.pageGradient.first(),
            onBackground = DarkColors.deepText,
            surface = DarkColors.glassSurface,
            onSurface = DarkColors.deepText,
        )
    } else {
        lightColorScheme(
            primary = LightColors.sky,
            onPrimary = Color.White,
            secondary = LightColors.cyan,
            background = LightColors.pageGradient.first(),
            onBackground = LightColors.deepText,
            surface = LightColors.glassSurface,
            onSurface = LightColors.deepText,
        )
    }
    MaterialTheme(colorScheme = scheme, content = content)
}
