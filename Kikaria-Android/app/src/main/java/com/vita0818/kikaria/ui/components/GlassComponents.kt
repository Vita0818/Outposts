package com.vita0818.kikaria.ui.components

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
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.vita0818.kikaria.ui.theme.KikariaColors

/**
 * Liquid-glass-style card modifier translated from the
 * LiquidGlassCardModifier / liquidGlassCard View extension in ContentView.swift.
 *
 * Provides a soft glass-morphism surface with subtle border gradient
 * and colored shadow, preserving Kikaria's clean study-focused aesthetic.
 */
@Composable
fun GlassCard(
    modifier: Modifier = Modifier,
    cornerRadius: Dp = 28.dp,
    shadowElevation: Dp = 18.dp,
    shadowColor: Color = KikariaColors.Shadow.copy(alpha = 0.12f),
    borderColors: List<Color> = listOf(
        Color.White.copy(alpha = 0.42f),
        Color.White.copy(alpha = 0.10f),
        KikariaColors.GlassStrokeAccent.copy(alpha = 0.13f)
    ),
    content: @Composable () -> Unit
) {
    val shape = RoundedCornerShape(cornerRadius)

    Surface(
        modifier = modifier
            .shadow(
                elevation = shadowElevation,
                shape = shape,
                ambientColor = shadowColor,
                spotColor = shadowColor
            ),
        shape = shape,
        color = KikariaColors.GlassSurface.copy(alpha = 0.48f),
        tonalElevation = 1.dp,
        border = null
    ) {
        Box(modifier = Modifier.fillMaxSize()) {
            content()
        }
    }
}

@Composable
fun GlassCapsule(
    modifier: Modifier = Modifier,
    shadowColor: Color = KikariaColors.Shadow.copy(alpha = 0.10f),
    content: @Composable () -> Unit
) {
    Surface(
        modifier = modifier
            .shadow(
                elevation = 14.dp,
                shape = CircleShape,
                ambientColor = shadowColor,
                spotColor = shadowColor
            ),
        shape = CircleShape,
        color = KikariaColors.GlassSurface.copy(alpha = 0.48f),
        tonalElevation = 1.dp
    ) {
        content()
    }
}

@Composable
fun GlassCircle(
    modifier: Modifier = Modifier,
    shadowColor: Color = KikariaColors.Shadow.copy(alpha = 0.14f),
    content: @Composable () -> Unit
) {
    Surface(
        modifier = modifier
            .shadow(
                elevation = 14.dp,
                shape = CircleShape,
                ambientColor = shadowColor,
                spotColor = shadowColor
            ),
        shape = CircleShape,
        color = KikariaColors.GlassSurface.copy(alpha = 0.44f),
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
    Surface(
        modifier = modifier,
        shape = RoundedCornerShape(20.dp),
        color = KikariaColors.Mist.copy(alpha = 0.7f),
        tonalElevation = 1.dp
    ) {
        Box(modifier = Modifier.padding(20.dp)) {
            Column {
                Text(
                    text = label,
                    style = MaterialTheme.typography.labelMedium,
                    color = KikariaColors.TertiaryText
                )
                Text(
                    text = text,
                    style = MaterialTheme.typography.bodyLarge,
                    color = KikariaColors.DeepText,
                    modifier = Modifier.padding(top = 8.dp)
                )
            }
        }
    }
}
