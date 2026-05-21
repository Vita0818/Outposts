package com.vita0818.kikaria.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
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
 * Low-level glass-effect drawing helpers and simple glass composables.
 *
 * These provide the iOS LiquidGlassCardModifier / liquidGlassCapsule / liquidGlassCircle
 * visual treatment: semi-transparent surface with gradient border strokes and soft shadows.
 *
 * For higher-level page/screen components (PageShell, PageTitle, KikariaGlassCard, etc.),
 * see KikariaSharedComponents.kt.
 */

// ── Glass stroke drawing modifier (reusable low-level primitive) ──

private fun glassCardStrokeColors(isDark: Boolean): List<Color> {
    val accent = if (isDark) KikariaColors.GlassStrokeAccentDark else KikariaColors.GlassStrokeAccent
    return listOf(
        Color.White.copy(alpha = if (isDark) 0.36f else 0.44f),
        Color.White.copy(alpha = if (isDark) 0.08f else 0.10f),
        accent.copy(alpha = if (isDark) 0.22f else 0.14f)
    )
}

fun Modifier.glassCardStroke(
    shape: RoundedCornerShape,
    isDark: Boolean,
    lineWidth: Float = 1f
): Modifier = this.drawBehind {
    val strokeWidth = lineWidth * density
    drawRoundRect(
        brush = Brush.linearGradient(
            colors = glassCardStrokeColors(isDark),
            start = Offset.Zero,
            end = Offset(size.width, size.height)
        ),
        cornerRadius = CornerRadius(
            shape.topStart.toPx(size, this),
            shape.topEnd.toPx(size, this)
        ),
        style = Stroke(width = strokeWidth)
    )
}

// ── Simple glass composables (thin wrappers over the shadow+background+stroke pattern) ──

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
            .shadow(elevation = 18.dp, shape = shape, ambientColor = shadowColor, spotColor = shadowColor)
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
            .shadow(elevation = 14.dp, shape = androidx.compose.foundation.shape.CircleShape, ambientColor = shadowColor, spotColor = shadowColor),
        shape = androidx.compose.foundation.shape.CircleShape,
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
            .shadow(elevation = 14.dp, shape = androidx.compose.foundation.shape.CircleShape, ambientColor = shadowColor, spotColor = shadowColor),
        shape = androidx.compose.foundation.shape.CircleShape,
        color = glassSurface.copy(alpha = 0.44f),
        tonalElevation = 1.dp
    ) {
        content()
    }
}

/**
 * Floating info card for review hint/answer.
 * Legacy wrapper — for new code prefer KikariaInfoCard from KikariaSharedComponents.
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
