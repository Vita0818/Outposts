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
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
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
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaDesign
import com.vita0818.kikaria.ui.theme.KikariaPhoneMetrics
import com.vita0818.kikaria.ui.theme.KikariaTypography

// ═══════════════════════════════════════════════════════════════════
//  Shared Kikaria UI primitives — translated from iOS View extensions
//  and glass-modifier patterns in ContentView.swift.
// ═══════════════════════════════════════════════════════════════════

// ── Glass stroke helpers (shared) ──

/**
 * Returns the iOS LiquidGlassCardModifier gradient stroke colors.
 *
 * iOS formula: white(strokeOpacity) → white(strokeOpacity*0.24) → accent(accentOpacity)
 * with dark-mode adjustment: strokeOpacity *= 0.86, accentOpacity elevated.
 */
private fun glassCardStrokeColors(isDark: Boolean): List<Color> {
    val accent = if (isDark) KikariaColors.GlassStrokeAccentDark else KikariaColors.GlassStrokeAccent
    val strokeOpacity = if (isDark) 0.36f else 0.42f
    val accentOpacity = if (isDark) 0.22f else 0.13f
    return listOf(
        Color.White.copy(alpha = strokeOpacity),
        Color.White.copy(alpha = strokeOpacity * 0.24f),
        accent.copy(alpha = accentOpacity)
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

/**
 * Inner highlight layer — faint blurred white stroke near the top edge,
 * matching the iOS LiquidGlassCardModifier second overlay.
 */
fun Modifier.kikariaGlassInnerHighlight(
    shape: RoundedCornerShape,
    isDark: Boolean
): Modifier = this.drawBehind {
    val hlOpacity = if (isDark) 0.10f else 0.18f
    drawRoundRect(
        color = Color.White.copy(alpha = hlOpacity),
        cornerRadius = CornerRadius(
            shape.topStart.toPx(size, this),
            shape.topEnd.toPx(size, this)
        ),
        style = Stroke(width = 0.5f * density)
    )
}

// ── Circular Icon Button (matches iOS KikariaAdaptiveBackButton / ProfileAvatarView glass circle) ──

/**
 * A circular glass-styled icon button matching the iOS
 * KikariaAdaptiveBackButton and close/settings icon buttons.
 * iOS spec: 42×42, fillOpacity 0.40, strokeOpacity 0.42, shadow 10dp.
 */
@Composable
fun KikariaCircularIconButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    icon: ImageVector? = null,
    textIcon: String? = null,
    size: Dp = 48.dp,
    iconSize: Dp = 22.dp,
    contentDescription: String? = null
) {
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val fillAlpha = if (isDark) 0.38f else 0.40f

    Box(
        modifier = modifier
            .size(size)
            // Dual shadow matching iOS liquidGlassCircle
            .shadow(10.dp, CircleShape,
                ambientColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
                    .copy(alpha = if (isDark) 0.08f else 0.14f),
                spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
                    .copy(alpha = if (isDark) 0.08f else 0.14f))
            .shadow(5.dp, CircleShape,
                ambientColor = Color.Black.copy(alpha = if (isDark) 0.12f else 0.02f),
                spotColor = Color.Black.copy(alpha = if (isDark) 0.12f else 0.02f))
            .clip(CircleShape)
            .background(glassSurface.copy(alpha = fillAlpha))
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

// ═══════════════════════════════════════════════════════════════════
//  Page Shell Variants — matching iOS iPhone shell / chrome patterns
// ═══════════════════════════════════════════════════════════════════

/**
 * Scroll page shell for list/content pages.
 *
 * Provides: gradient background + overlay back button (zIndex above content)
 * + scrollable content area with metrics-driven horizontal padding.
 *
 * iOS equivalent: KikariaAdaptivePage + kikariaAdaptiveNavigationChrome
 * where the back button is in the navigation chrome (system NavigationStack),
 * NOT in the page content.
 *
 * Used by: ScopeSelection, Reinforcement, Mastered, TodayOverview,
 * ReviewHistory, PresetSelection, MarkdownFormatGuide, PrivacyPolicy.
 */
@Composable
fun KikariaScrollPageShell(
    onBack: () -> Unit,
    metrics: KikariaPhoneMetrics,
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
        // Scrollable content — metrics-driven horizontal padding, tablet max-width centering
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = if (metrics.isTablet) Alignment.TopCenter else Alignment.TopStart
        ) {
            Column(
                modifier = Modifier
                    .then(
                        if (metrics.isTablet) Modifier.widthIn(max = metrics.contentMaxWidth)
                        else Modifier
                    )
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = metrics.horizontalPadding)
            ) {
                content()
            }
        }

        // Back button — TRUE overlay, zIndex above content, does NOT affect layout
        KikariaBackButton(
            onClick = onBack,
            metrics = metrics,
            modifier = Modifier.align(Alignment.TopStart)
        )
    }
}

/**
 * Form page shell for settings / edit pages with a top bar.
 *
 * Provides: gradient background + non-scrolling top bar (back + title + optional action)
 * + scrollable form content below.
 *
 * iOS equivalent: pages like SettingsView, EditProfileView, NewPresetView
 * that have a custom HStack top bar with back/close button, centered title,
 * and optional save/action button.
 */
@Composable
fun KikariaFormPageShell(
    title: String,
    onBack: () -> Unit,
    metrics: KikariaPhoneMetrics,
    modifier: Modifier = Modifier,
    actionLabel: String? = null,
    onAction: (() -> Unit)? = null,
    closeIcon: ImageVector = KikariaIcons.back,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val pageGradient = if (isDark) KikariaColors.PageGradientDark else KikariaColors.PageGradientLight
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    Box(
        modifier = modifier
            .fillMaxSize()
            .background(pageGradient),
        contentAlignment = if (metrics.isTablet) Alignment.TopCenter else Alignment.TopStart
    ) {
        Column(
            modifier = Modifier
                .then(
                    if (metrics.isTablet) Modifier.widthIn(max = metrics.formMaxWidth)
                    else Modifier
                )
                .fillMaxSize()
        ) {
            // ── Non-scrolling top bar ──
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = metrics.horizontalPadding)
                    .padding(top = metrics.backButtonTopPadding, bottom = 14.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Left: back/close button
                KikariaCircularIconButton(
                    onClick = onBack,
                    icon = closeIcon,
                    contentDescription = "返回",
                    size = metrics.backButtonSize
                )

                Spacer(modifier = Modifier.width(12.dp))

                // Center: title
                Text(
                    text = KikariaTypography.mixedText(title, size = 17, weight = FontWeight.SemiBold),
                    color = deepText,
                    modifier = Modifier.weight(1f),
                    textAlign = TextAlign.Center
                )

                // Right: action button or spacer for centering
                if (actionLabel != null && onAction != null) {
                    Text(
                        text = actionLabel,
                        fontSize = 15.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = sky,
                        modifier = Modifier
                            .clip(RoundedCornerShape(14.dp))
                            .clickable { onAction() }
                            .padding(horizontal = 12.dp, vertical = 8.dp)
                    )
                } else {
                    // Invisible spacer to balance left button width for title centering
                    Spacer(modifier = Modifier.width(metrics.backButtonSize))
                }
            }

            // ── Scrollable form content ──
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = metrics.horizontalPadding)
            ) {
                content()
            }
        }
    }
}

/**
 * Review page shell with fixed bottom action region.
 *
 * Provides: gradient background + overlay back button (zIndex above all)
 * + gesture-capable outer Box + scrollable content region + fixed action region.
 *
 * iOS equivalent: ReviewView body with contentRegion (scrollable) and
 * actionRegion (fixed bottom) as VStack siblings, gesture on outer ZStack.
 */
@Composable
fun KikariaReviewShell(
    onBack: () -> Unit,
    metrics: KikariaPhoneMetrics,
    modifier: Modifier = Modifier,
    gestureModifier: Modifier = Modifier,
    progressContent: @Composable () -> Unit = {},
    scrollContent: @Composable () -> Unit,
    bottomContent: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val pageGradient = if (isDark) KikariaColors.PageGradientDark else KikariaColors.PageGradientLight

    Box(
        modifier = modifier
            .fillMaxSize()
            .background(pageGradient)
            .then(gestureModifier)
    ) {
        Column(modifier = Modifier.fillMaxSize()) {
            // Progress area
            progressContent()

            // Scrollable content — fills remaining space, metrics-driven padding
            Column(
                modifier = Modifier
                    .weight(1f)
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = metrics.horizontalPadding)
            ) {
                scrollContent()
            }

            // Fixed bottom action region
            bottomContent()
        }

        // Back button — TRUE overlay on top of everything
        Box(modifier = Modifier.fillMaxSize()) {
            KikariaBackButton(
                onClick = onBack,
                metrics = metrics,
                modifier = Modifier.align(Alignment.TopStart)
            )
        }
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
 *
 * iOS layers:
 *   1. fill (glassSurface at adjustedFillOpacity, dark mode reduces by 0.82x, capped at 0.38)
 *   2. .ultraThinMaterial background (not reproducible in Compose — approximated by fill)
 *   3. gradient stroke (white → white*0.24 → accent)
 *   4. inner highlight (blurred white stroke, 0.5px)
 *   5. colored shadow (sky, radius ~18dp, y-offset)
 *   6. subtle black shadow beneath
 */
@Composable
fun KikariaGlassCard(
    modifier: Modifier = Modifier,
    cornerRadius: Dp = 28.dp,
    fillOpacity: Float = 0.48f,
    shadowElevation: Dp = 18.dp,
    shadowOpacity: Float = 0.12f,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    // Dark mode: slightly reduced opacity for visual comfort
    val adjustedFill = if (isDark) minOf(fillOpacity * 0.94f, 0.56f) else fillOpacity
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
        .copy(alpha = if (isDark) maxOf(shadowOpacity * 0.58f, 0.08f) else shadowOpacity)
    val shape = RoundedCornerShape(cornerRadius)

    Box(
        modifier = modifier
            // Primary shadow
            .shadow(shadowElevation, shape, ambientColor = shadowColor, spotColor = shadowColor)
            // Secondary subtle black shadow
            .shadow(
                (shadowElevation.value * 0.55f).dp, shape,
                ambientColor = Color.Black.copy(alpha = if (isDark) 0.18f else 0.025f),
                spotColor = Color.Black.copy(alpha = if (isDark) 0.18f else 0.025f)
            )
            .clip(shape)
            .background(glassSurface.copy(alpha = adjustedFill))
            .kikariaGlassStroke(shape, isDark)
            .kikariaGlassInnerHighlight(shape, isDark)
    ) {
        content()
    }
}

// ── Profile Avatar (matches iOS ProfileAvatarView) ──

/**
 * Profile avatar in a glass circle with gradient fill, matching iOS ProfileAvatarView.
 * iOS spec: size ~44dp, outer glass circle fill 0.36, inner gradient (sky → cyan),
 * serif initial letter in white.
 */
@Composable
fun KikariaProfileAvatar(
    modifier: Modifier = Modifier,
    size: Dp = 44.dp,
    displayName: String = ""
) {
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = if (isDark) 0.10f else 0.16f)
    val displayChar = if (displayName.isNotEmpty()) displayName.first().uppercase() else "K"

    Box(
        modifier = modifier
            .size(size)
            .shadow(12.dp, CircleShape, ambientColor = shadowColor, spotColor = shadowColor)
            .shadow(5.dp, CircleShape,
                ambientColor = Color.Black.copy(alpha = if (isDark) 0.10f else 0.02f),
                spotColor = Color.Black.copy(alpha = if (isDark) 0.10f else 0.02f))
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
 * Reusable tag chip matching iOS LightTagRow tag style.
 * iOS spec: liquidGlassCapsule, fillOpacity 0.38, strokeOpacity 0.34,
 * shadow 6dp, horizontal padding 11, vertical padding 6, semibold 12sp.
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
    val fillAlpha = if (isDark) 0.36f else 0.38f
    val capsuleShape = RoundedCornerShape(KikariaDesign.PillRadius)

    Box(
        modifier = modifier
            .clip(capsuleShape)
            .shadow(6.dp, capsuleShape,
                ambientColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.04f),
                spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.04f))
            .background(glassSurface.copy(alpha = fillAlpha))
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
 *
 * iOS reference: KikariaAdaptiveBackButton + overlay at topLeading
 * with .padding(.leading, metrics.horizontalPadding).padding(.top, 12).
 */
@Composable
fun KikariaBackButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    metrics: KikariaPhoneMetrics? = null
) {
    val hp = metrics?.horizontalPadding ?: 24.dp
    val tp = metrics?.backButtonTopPadding ?: 12.dp
    val size = metrics?.backButtonSize ?: 42.dp
    KikariaCircularIconButton(
        onClick = onClick,
        icon = KikariaIcons.back,
        contentDescription = "返回",
        modifier = modifier.padding(start = hp, top = tp),
        size = size
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

// ── Math-Enabled Text ──

/**
 * Displays text with LaTeX math formulas converted to readable Unicode symbols.
 *
 * Translates the KikariaMathText view from the iOS source, using the fallback
 * path (readableMathFallback) since Android has no native LaTeX renderer without
 * third-party libraries. The project's no-dependency principle precludes
 * adding a math rendering library.
 *
 * Inline math ($...$) is rendered in a monospace-styled span;
 * block math ($$...$$) is rendered as a distinct code-block.
 */
@Composable
fun KikariaMathText(
    text: String,
    fontSize: Int = 17,
    fontWeight: FontWeight = FontWeight.Normal,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    // If the text contains no math delimiters, use plain mixedText
    if (!text.contains("$")) {
        Text(
            text = KikariaTypography.mixedText(text, size = fontSize, weight = fontWeight),
            color = deepText,
            modifier = modifier,
            lineHeight = (fontSize * 1.5).sp
        )
        return
    }

    // Render with math fallback
    val rendered = com.vita0818.kikaria.util.KikariaMathFallback.renderContent(text)
    Text(
        text = KikariaTypography.mixedText(rendered, size = fontSize, weight = fontWeight),
        color = deepText,
        modifier = modifier,
        lineHeight = (fontSize * 1.5).sp
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
