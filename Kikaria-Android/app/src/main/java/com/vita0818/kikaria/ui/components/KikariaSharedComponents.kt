package com.vita0818.kikaria.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography

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
            Icon(
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

// ── Page Title (matches iOS page title pattern) ──

/**
 * Kikaria-styled page title using mixed serif/CJK typography.
 * Matches iOS `Text(title).font(.chineseTitle()).foregroundStyle(.deepText)`.
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
        text = KikariaTypography.mixedText(
            text = title,
            size = fontSize,
            weight = fontWeight
        ),
        modifier = modifier,
        color = { deepText }
    )
}

// ── Section Header ──

/**
 * Section header matching iOS SettingsSectionCard title pattern.
 */
@Composable
fun KikariaSectionHeader(
    title: String,
    modifier: Modifier = Modifier,
    fontSize: Int = 13,
    fontWeight: FontWeight = FontWeight.SemiBold
) {
    val isDark = isSystemInDarkTheme()
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    Text(
        text = KikariaTypography.mixedText(title, size = fontSize, weight = fontWeight),
        color = softText,
        modifier = modifier.padding(horizontal = 4.dp)
    )
}

// ── Glass Card Container ──

/**
 * A reusable glass card matching the iOS liquidGlassCard modifier pattern.
 */
@Composable
fun KikariaGlassCard(
    modifier: Modifier = Modifier,
    cornerRadius: Dp = 28.dp,
    fillOpacity: Float = 0.40f,
    shadowElevation: Dp = 18.dp,
    shadowOpacity: Float = 0.12f,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = shadowOpacity)
    val shape = RoundedCornerShape(cornerRadius)

    Box(
        modifier = modifier
            .shadow(shadowElevation, shape, ambientColor = shadowColor, spotColor = shadowColor)
            .clip(shape)
            .background(glassSurface.copy(alpha = fillOpacity))
            .kikariaGlassStroke(shape, isDark)
    ) {
        content()
    }
}

// ── Profile Avatar (matches iOS ProfileAvatarView) ──

/**
 * Profile avatar in a glass circle, matching the iOS ProfileAvatarView.
 */
@Composable
fun KikariaProfileAvatar(
    modifier: Modifier = Modifier,
    size: Dp = 44.dp,
    displayName: String = ""
) {
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.16f)
    val displayChar = if (displayName.isNotEmpty()) displayName.first().uppercase() else "K"

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
                text = displayChar,
                color = Color.White,
                fontWeight = FontWeight.Bold,
                fontSize = (size.value * 0.38f).sp,
                fontFamily = FontFamily.Serif
            )
        }
    }
}

// ── Tag Chip ──

/**
 * Reusable tag chip matching iOS LightTagRow tag style with glass capsule.
 */
@Composable
fun KikariaTagChip(
    tag: String,
    modifier: Modifier = Modifier,
    fontSize: Int = 12,
    fontWeight: FontWeight = FontWeight.SemiBold
) {
    val isDark = isSystemInDarkTheme()
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val capsuleShape = RoundedCornerShape(16.dp)

    Box(
        modifier = modifier
            .clip(capsuleShape)
            .shadow(6.dp, capsuleShape,
                ambientColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.04f),
                spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.04f))
            .background(glassSurface.copy(alpha = 0.38f))
            .padding(horizontal = 11.dp, vertical = 6.dp)
    ) {
        Text(
            text = tag,
            fontSize = fontSize.sp,
            fontWeight = fontWeight,
            color = softText,
            maxLines = 1
        )
    }
}

// ── Empty State ──

/**
 * Soft empty / placeholder state matching the iOS SoftEmptyState pattern.
 */
@Composable
fun KikariaEmptyState(
    title: String,
    subtitle: String,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText

    Column(
        modifier = modifier.fillMaxWidth().padding(26.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = title,
            fontSize = 20.sp,
            fontWeight = FontWeight.Bold,
            color = deepText,
            textAlign = TextAlign.Center
        )
        Spacer(Modifier.height(8.dp))
        Text(
            text = subtitle,
            fontSize = 15.sp,
            fontWeight = FontWeight.Normal,
            color = softText,
            textAlign = TextAlign.Center
        )
    }
}

// ── Back Button Overlay ──

/**
 * Positioned back button overlay matching the iOS back button pattern.
 */
@Composable
fun KikariaBackButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    KikariaCircularIconButton(
        onClick = onClick,
        icon = KikariaIcons.back,
        modifier = modifier.padding(start = 24.dp, top = 12.dp),
        size = 42.dp
    )
}

// ── Metric Display Number ──

/**
 * Large metric/display number matching iOS HomeDashboardMetricColumn value text.
 */
@Composable
fun KikariaMetricValue(
    value: String,
    tint: Color,
    modifier: Modifier = Modifier,
    fontSize: Int = 24,
    fontWeight: FontWeight = FontWeight.Bold
) {
    Text(
        text = KikariaTypography.mixedText(value, size = fontSize, weight = fontWeight),
        color = tint,
        maxLines = 1,
        textAlign = TextAlign.Center,
        modifier = modifier
    )
}

// ── Metric Label ──

/**
 * Metric label matching iOS caption pattern.
 */
@Composable
fun KikariaMetricLabel(
    label: String,
    modifier: Modifier = Modifier,
    fontSize: Int = 13,
    fontWeight: FontWeight = FontWeight.SemiBold
) {
    val isDark = isSystemInDarkTheme()
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    Text(
        text = label,
        fontSize = fontSize.sp,
        fontWeight = fontWeight,
        color = softText,
        maxLines = 1,
        modifier = modifier
    )
}
