package com.vita0818.kikaria.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.vita0818.kikaria.ui.theme.KikariaColors

/**
 * Draws a glass card gradient stroke overlay matching iOS LiquidGlassCardModifier.
 */
private fun Modifier.glassCardStroke(
    shape: RoundedCornerShape,
    isDark: Boolean,
    lineWidth: Float = 1f
): Modifier = this.drawBehind {
    val strokeWidth = lineWidth * density
    val accent = if (isDark) KikariaColors.GlassStrokeAccentDark else KikariaColors.GlassStrokeAccent
    drawRoundRect(
        brush = Brush.linearGradient(
            colors = listOf(
                Color.White.copy(alpha = if (isDark) 0.36f else 0.44f),
                Color.White.copy(alpha = if (isDark) 0.08f else 0.10f),
                accent.copy(alpha = if (isDark) 0.22f else 0.14f)
            ),
            start = Offset.Zero,
            end = Offset(size.width, size.height)
        ),
        cornerRadius = CornerRadius(
            shape.topStart.toPx(size, density),
            shape.topEnd.toPx(size, density)
        ),
        style = Stroke(width = strokeWidth)
    )
}

/**
 * Liquid-glass-style card modifier translated from the
 * LiquidGlassCardModifier / liquidGlassCard View extension in ContentView.swift.
 *
 * Provides a soft glass-morphism surface with subtle shadow,
 * preserving Kikaria's clean study-focused aesthetic.
 */
@Composable
fun GlassCard(
    modifier: Modifier = Modifier,
    cornerRadius: Dp = 28.dp,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val shape = RoundedCornerShape(cornerRadius)
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.12f) else KikariaColors.Sky.copy(alpha = 0.12f)

    Box(
        modifier = modifier
            .shadow(
                elevation = 18.dp,
                shape = shape,
                ambientColor = shadowColor,
                spotColor = shadowColor
            )
            .clip(shape)
            .background(glassSurface.copy(alpha = 0.48f))
            .glassCardStroke(shape, isDark)
    ) {
        content()
    }
}

@Composable
fun GlassCapsule(
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.10f) else KikariaColors.Sky.copy(alpha = 0.10f)

    Surface(
        modifier = modifier
            .shadow(
                elevation = 14.dp,
                shape = CircleShape,
                ambientColor = shadowColor,
                spotColor = shadowColor
            ),
        shape = CircleShape,
        color = glassSurface.copy(alpha = 0.48f),
        tonalElevation = 1.dp
    ) {
        content()
    }
}

@Composable
fun GlassCircle(
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.14f) else KikariaColors.Sky.copy(alpha = 0.14f)

    Surface(
        modifier = modifier
            .shadow(
                elevation = 14.dp,
                shape = CircleShape,
                ambientColor = shadowColor,
                spotColor = shadowColor
            ),
        shape = CircleShape,
        color = glassSurface.copy(alpha = 0.44f),
        tonalElevation = 1.dp
    ) {
        content()
    }
}

/**
 * Floating info card used for hint and content display in review,
 * mimicking the FloatingInfoCard style from ContentView.swift.
 */
@Composable
fun InfoCard(
    modifier: Modifier = Modifier,
    label: String,
    text: String
) {
    val isDark = isSystemInDarkTheme()
    val mist = if (isDark) KikariaColors.MistDark else KikariaColors.Mist
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val tertiaryText = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText

    Surface(
        modifier = modifier,
        shape = RoundedCornerShape(20.dp),
        color = mist.copy(alpha = 0.7f),
        tonalElevation = 1.dp
    ) {
        Box(modifier = Modifier.padding(20.dp)) {
            Column {
                Text(
                    text = label,
                    style = MaterialTheme.typography.labelMedium,
                    color = tertiaryText
                )
                Text(
                    text = text,
                    style = MaterialTheme.typography.bodyLarge,
                    color = deepText,
                    modifier = Modifier.padding(top = 8.dp)
                )
            }
        }
    }
}
