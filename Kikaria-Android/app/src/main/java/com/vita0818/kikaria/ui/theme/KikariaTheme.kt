package com.vita0818.kikaria.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

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

    MaterialTheme(
        colorScheme = colorScheme,
        content = content
    )
}
