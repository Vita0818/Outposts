package com.rokurics.app.ui.theme

import androidx.compose.material3.MaterialTheme
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

@Composable
fun RokuricsTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = LightColorScheme,
        content = content
    )
}
