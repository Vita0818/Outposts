package com.vita0818.kikaria.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

/**
 * Kikaria design tokens exposed via composition local,
 * so every screen can reference kikaria-specific typography
 * without importing KikariaTypography directly.
 */
data class KikariaTokens(
    // Core text styles
    val appTitle: TextStyle = TextStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 39.sp
    ),
    val pageTitle: TextStyle = TextStyle(
        fontWeight = FontWeight.Bold,
        fontSize = 32.sp
    ),
    val headline: TextStyle = TextStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = 17.sp
    ),
    val body: TextStyle = TextStyle(
        fontWeight = FontWeight.Normal,
        fontSize = 15.sp
    ),
    val button: TextStyle = TextStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = 17.sp
    ),
    val caption: TextStyle = TextStyle(
        fontWeight = FontWeight.Medium,
        fontSize = 12.sp
    ),
    val metricValue: TextStyle = TextStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.Bold,
        fontSize = 24.sp
    ),
    val tagStyle: TextStyle = TextStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = 12.sp
    )
)

val LocalKikariaTokens = staticCompositionLocalOf { KikariaTokens() }

private val LightColorScheme = lightColorScheme(
    primary = KikariaColors.Sky,
    onPrimary = Color.White,
    primaryContainer = KikariaColors.Mist,
    onPrimaryContainer = KikariaColors.DeepText,
    secondary = KikariaColors.Cyan,
    onSecondary = KikariaColors.DeepText,
    tertiary = KikariaColors.NextAmber,
    background = Color(0xFFF0F7FA),
    onBackground = KikariaColors.DeepText,
    surface = KikariaColors.GlassSurface,
    onSurface = KikariaColors.DeepText,
    surfaceVariant = Color(0xFFF5FAFD),
    onSurfaceVariant = KikariaColors.SoftText,
    outline = KikariaColors.BlueGray,
    error = KikariaColors.RemoveCoral
)

private val DarkColorScheme = darkColorScheme(
    primary = KikariaColors.SkyDark,
    onPrimary = Color(0xFF0A1A28),
    primaryContainer = KikariaColors.MistDark,
    onPrimaryContainer = KikariaColors.DeepTextDark,
    secondary = KikariaColors.CyanDark,
    onSecondary = KikariaColors.DeepTextDark,
    tertiary = KikariaColors.NextAmberDark,
    background = Color(0xFF081420),
    onBackground = KikariaColors.DeepTextDark,
    surface = KikariaColors.GlassSurfaceDark,
    onSurface = KikariaColors.DeepTextDark,
    surfaceVariant = Color(0xFF142838),
    onSurfaceVariant = KikariaColors.SoftTextDark,
    outline = KikariaColors.BlueGrayDark,
    error = KikariaColors.RemoveCoralDark
)

@Composable
fun KikariaTheme(
    darkTheme: Boolean = false,
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    CompositionLocalProvider(LocalKikariaTokens provides KikariaTokens()) {
        MaterialTheme(
            colorScheme = colorScheme,
            content = content
        )
    }
}
