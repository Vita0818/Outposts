package com.rokurics.app.ui.theme

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.composed
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

fun Modifier.rokuricsGlassCard(
    cornerRadius: Dp = 24.dp,
    fillOpacity: Float = 0.40f,
    strokeOpacity: Float = 0.44f,
    shadowOpacity: Float = 0.12f,
    shadowRadius: Dp = 20.dp,
    fillColor: Color = RokuricsColors.glassSurface
): Modifier = this
    .shadow(
        elevation = shadowRadius,
        spotColor = RokuricsColors.shadow.copy(alpha = shadowOpacity),
        ambientColor = Color.Black.copy(alpha = shadowOpacity * 0.3f),
        shape = RoundedCornerShape(cornerRadius)
    )
    .clip(RoundedCornerShape(cornerRadius))
    .background(fillColor.copy(alpha = fillOpacity))
    .drawBehind {
        val strokeWidth = 1.5.dp.toPx()
        drawRoundRect(
            brush = Brush.linearGradient(
                colors = listOf(
                    Color.White.copy(alpha = strokeOpacity),
                    RokuricsColors.glassStroke.copy(alpha = strokeOpacity * 0.44f),
                    RokuricsColors.aqua.copy(alpha = 0.16f)
                ),
                start = Offset(0f, 0f),
                end = Offset(size.width, size.height)
            ),
            cornerRadius = CornerRadius(cornerRadius.toPx()),
            style = Stroke(width = strokeWidth)
        )
    }
    // Inner highlight (Apple parity: subtle white stroke)
    .clip(RoundedCornerShape(cornerRadius))
    .background(
        Brush.linearGradient(
            colors = listOf(
                Color.White.copy(alpha = 0.22f),
                Color.Transparent
            ),
            start = Offset(0f, 0f),
            end = Offset(0f, Float.POSITIVE_INFINITY)
        ),
        RoundedCornerShape(cornerRadius)
    )

fun Modifier.rokuricsScaleClickable(
    onClick: () -> Unit,
    enabled: Boolean = true,
    scaleTarget: Float = 0.985f,
    durationMs: Int = 140
): Modifier = composed {
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()
    val scale by animateFloatAsState(
        targetValue = if (enabled && isPressed) scaleTarget else 1f,
        animationSpec = tween(durationMillis = durationMs),
        label = "pressScale"
    )
    this
        .graphicsLayer {
            scaleX = scale
            scaleY = scale
        }
        .clickable(
            interactionSource = interactionSource,
            indication = null,
            enabled = enabled,
            onClick = onClick
        )
}

fun Modifier.rokuricsGlassCircle(
    fillOpacity: Float = 0.36f,
    strokeOpacity: Float = 0.50f,
    shadowOpacity: Float = 0.14f,
    shadowRadius: Dp = 12.dp,
    fillColor: Color = Color.White
): Modifier = this
    .shadow(
        elevation = shadowRadius,
        spotColor = RokuricsColors.shadow.copy(alpha = shadowOpacity),
        ambientColor = Color.Black.copy(alpha = shadowOpacity * 0.25f)
    )
    .clip(CircleShape)
    .background(fillColor.copy(alpha = fillOpacity))
    .drawBehind {
        val strokeWidth = 1.5.dp.toPx()
        drawCircle(
            brush = Brush.linearGradient(
                colors = listOf(
                    Color.White.copy(alpha = strokeOpacity),
                    Color.White.copy(alpha = 0.12f),
                    RokuricsColors.aqua.copy(alpha = 0.16f)
                ),
                start = Offset(0f, 0f),
                end = Offset(size.width, size.height)
            ),
            radius = size.minDimension / 2f,
            style = Stroke(width = strokeWidth)
        )
    }

fun Modifier.rokuricsGlassCapsule(
    fillOpacity: Float = 0.46f,
    strokeOpacity: Float = 0.40f,
    shadowOpacity: Float = 0.08f,
    shadowRadius: Dp = 12.dp,
    fillColor: Color = Color.White
): Modifier = this
    .shadow(
        elevation = shadowRadius,
        spotColor = RokuricsColors.shadow.copy(alpha = shadowOpacity),
        ambientColor = Color.Black.copy(alpha = shadowOpacity * 0.25f)
    )
    .clip(RoundedCornerShape(50))
    .background(fillColor.copy(alpha = fillOpacity))
    .drawBehind {
        val strokeWidth = 1.5.dp.toPx()
        drawRoundRect(
            brush = Brush.linearGradient(
                colors = listOf(
                    Color.White.copy(alpha = strokeOpacity),
                    Color.White.copy(alpha = strokeOpacity * 0.35f),
                    RokuricsColors.aqua.copy(alpha = 0.16f)
                ),
                start = Offset(0f, 0f),
                end = Offset(size.width, size.height)
            ),
            cornerRadius = CornerRadius(size.minDimension / 2f),
            style = Stroke(width = strokeWidth)
        )
    }
