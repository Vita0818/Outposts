package com.rokurics.app.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val LightColorScheme = lightColorScheme(
    primary = RokuricsColors.aqua,
    onPrimary = Color.White,
    secondary = RokuricsColors.mint,
    tertiary = RokuricsColors.softTeal,
    surface = RokuricsColors.glassSurface,
    background = RokuricsColors.pageGradient[0],
    onBackground = RokuricsColors.deepText,
    onSurface = RokuricsColors.deepText,
    onSurfaceVariant = RokuricsColors.softText,
    outline = RokuricsColors.glassStroke
)

private val DarkColorScheme = darkColorScheme(
    primary = RokuricsColors.aquaDark,
    onPrimary = Color(0xFF051414),
    secondary = RokuricsColors.mintDark,
    tertiary = RokuricsColors.softTealDark,
    surface = RokuricsColors.glassSurfaceDark,
    background = RokuricsColors.pageGradientDark[0],
    onBackground = RokuricsColors.deepTextDark,
    onSurface = RokuricsColors.deepTextDark,
    onSurfaceVariant = RokuricsColors.softTextDark,
    outline = RokuricsColors.glassStrokeDark
)

@Composable
fun RokuricsTheme(content: @Composable () -> Unit) {
    val useDarkTheme = isSystemInDarkTheme()
    MaterialTheme(
        colorScheme = if (useDarkTheme) DarkColorScheme else LightColorScheme,
        typography = RokuricsTypographyTokens,
        content = content
    )
}
