package com.vita0818.kikaria.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.theme.KikariaColors

// ═══════════════════════════════════════════════════════════════════
//  Shared Kikaria UI primitives — translated from iOS View extensions
//  and glass-modifier patterns in ContentView.swift.
// ═══════════════════════════════════════════════════════════════════

// ── Glass stroke helpers (shared) ──

private fun glassCardStrokeColors(isDark: Boolean): List<Color> {
    val accent = if (isDark) KikariaColors.GlassStrokeAccentDark else KikariaColors.GlassStrokeAccent
    return listOf(
        Color.White.copy(alpha = if (isDark) 0.36f else 0.44f),
        Color.White.copy(alpha = if (isDark) 0.08f else 0.10f),
        accent.copy(alpha = if (isDark) 0.22f else 0.14f)
    )
}

/**
 * Draws the iOS LiquidGlassCardModifier gradient border stroke via drawBehind.
 */
fun Modifier.kikariaGlassStroke(
    shape: RoundedCornerShape,
    isDark: Boolean,
    lineWidth: Float = 1f
): Modifier = this.drawBehind {
    val strokeWidth = lineWidth * density
    val colors = glassCardStrokeColors(isDark)
    drawRoundRect(
        brush = Brush.linearGradient(
            colors = colors,
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

// ── Circular Icon Button (matches iOS KikariaAdaptiveBackButton / ProfileAvatarView glass circle) ──

/**
 * A circular glass-styled icon button matching the iOS
 * KikariaAdaptiveBackButton and close/settings icon buttons.
 *
 * Uses the shared liquid-glass circle treatment from the iOS source.
 */
@Composable
fun KikariaCircularIconButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    icon: ImageVector? = null,
    textIcon: String? = null,
    size: Dp = 42.dp,
    iconSize: Dp = 22.dp,
    contentDescription: String? = null
) {
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val shadowColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.10f)

    Box(
        modifier = modifier
            .size(size)
            .shadow(10.dp, CircleShape, ambientColor = shadowColor, spotColor = shadowColor)
            .clip(CircleShape)
            .background(glassSurface.copy(alpha = 0.40f))
            .clickable { onClick() },
        contentAlignment = Alignment.Center
    ) {
        if (icon != null) {
            androidx.compose.material3.Icon(
                imageVector = icon,
                contentDescription = contentDescription,
                tint = deepText,
                modifier = Modifier.size(iconSize)
            )
        } else if (textIcon != null) {
            Text(
                text = textIcon,
                fontSize = iconSize.value.sp * 0.9f,
                fontWeight = FontWeight.SemiBold,
                color = deepText
            )
        }
    }
}

// ── Page Shell (gradient background + optional scrolling) ──

/**
 * Shared Kikaria page shell that provides the pageGradient background.
 * Matches ZStack { KikariaTheme.pageGradient.ignoresSafeArea() } in iOS.
 */
@Composable
fun KikariaPageShell(
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val pageGradient = if (isDark) KikariaColors.PageGradientDark else KikariaColors.PageGradientLight

    Box(
        modifier = modifier
            .fillMaxSize()
            .background(pageGradient)
    ) {
        content()
    }
}

// ── Page Title (matches iOS Text("PageTitle").font(.chineseTitle()).foregroundStyle(.deepText) pattern) ──

/**
 * Kikaria-styled page title.  Matches the iOS pattern of a serif-mixed
 * Chinese title text placed at the top of a page.
 */
@Composable
fun KikariaPageTitle(
    title: String,
    modifier: Modifier = Modifier,
    fontSize: Int = 32,
    fontWeight: FontWeight = FontWeight.Bold
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    androidx.compose.foundation.text.BasicText(
        text = com.vita0818.kikaria.ui.theme.KikariaTypography.mixedText(
            text = title,
            size = fontSize,
            weight = fontWeight
        ),
        modifier = modifier,
        color = { deepText }
    )
}

// ── Profile Avatar (matches iOS ProfileAvatarView) ──

/**
 * Profile avatar in a glass circle, matching the iOS ProfileAvatarView.
 * Uses the person icon as the fallback (source: "person.crop.circle.fill").
 */
@Composable
fun KikariaProfileAvatar(
    modifier: Modifier = Modifier,
    size: Dp = 44.dp,
    displayName: String = ""
) {
    val isDark = isSystemInDarkTheme()
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.16f)

    Box(
        modifier = modifier
            .size(size)
            .shadow(12.dp, CircleShape, ambientColor = shadowColor, spotColor = shadowColor)
            .clip(CircleShape)
            .background(glassSurface.copy(alpha = 0.36f))
            .padding(3.dp),
        contentAlignment = Alignment.Center
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .clip(CircleShape)
                .background(
                    Brush.linearGradient(
                        if (isDark) listOf(KikariaColors.SkyDark, KikariaColors.CyanDark)
                        else listOf(KikariaColors.Sky, KikariaColors.Cyan)
                    )
                ),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = if (displayName.isNotEmpty()) displayName.first().uppercase() else "V",
                color = Color.White,
                fontWeight = FontWeight.Bold,
                fontSize = (size.value * 0.38f).sp,
                fontFamily = FontFamily.Serif
            )
        }
    }
}

// ── Soft Empty State (matches iOS SoftEmptyState) ──

/**
 * Soft empty / placeholder state matching the iOS SoftEmptyState pattern.
 * Used when a list is empty or a resource is not found.
 */
@Composable
fun KikariaEmptyState(
    title: String,
    subtitle: String,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val tertiaryText = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText

    Box(
        modifier = modifier.fillMaxSize(),
        contentAlignment = Alignment.Center
    ) {
        androidx.compose.foundation.layout.Column(
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = title,
                fontSize = 18.sp,
                fontWeight = FontWeight.SemiBold,
                color = tertiaryText,
                textAlign = androidx.compose.ui.text.style.TextAlign.Center
            )
            if (subtitle.isNotEmpty()) {
                Text(
                    text = subtitle,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Normal,
                    color = softText,
                    textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                    modifier = Modifier.padding(top = 8.dp)
                )
            }
        }
    }
}
