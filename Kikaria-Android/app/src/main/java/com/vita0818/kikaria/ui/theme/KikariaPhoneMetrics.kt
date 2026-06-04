package com.vita0818.kikaria.ui.theme

import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

/**
 * Layout metrics mirroring the iOS KikariaAdaptiveLayout.Metrics.
 *
 * iOS reference: KikariaAdaptiveLayout.swift lines 19–493.
 * All values computed from current window dimensions — no magic pixels.
 *
 * Supports three form factors:
 * - compact (iPhone): width < 600dp
 * - regularPad (tablet 600–899dp)
 * - widePad (tablet ≥ 900dp)
 *
 * plus portrait vs landscape distinction within each tablet tier.
 */
data class KikariaPhoneMetrics(
    val widthDp: Float,
    val heightDp: Float,
    val density: Float,
    val fontScale: Float,
) {
    // ── Width classification (iOS: KikariaAdaptiveLayout.widthCategory) ──

    val isCompactPhone: Boolean get() = widthDp < 600f
    val isRegularPad: Boolean get() = widthDp in 600f..899f
    val isWidePad: Boolean get() = widthDp >= 900f
    val isTablet: Boolean get() = widthDp >= 600f

    /** True for tablet in portrait orientation (iOS: isPadPortrait) */
    val isPadPortrait: Boolean get() = isTablet && heightDp >= widthDp

    /** True for tablet in landscape orientation */
    val isPadLandscape: Boolean get() = isTablet && widthDp > heightDp

    /** True when two-column layout is viable (iOS: isTwoColumnCapable, width ≥ 950 landscape) */
    val isTwoColumnCapable: Boolean get() = isTablet && widthDp >= 950f && widthDp > heightDp

    /** Convenience: large iPad portrait (≥ 900dp wide, portrait) */
    val isLargePadPortrait: Boolean get() = isPadPortrait && isWidePad

    // ── Horizontal padding (iOS: line 484–493) ──
    val horizontalPadding: Dp
        get() = when {
            isWidePad -> 40.dp
            isTablet -> 32.dp
            widthDp < 360f -> 20.dp
            else -> 24.dp
        }

    val innerHorizontalPadding: Dp
        get() = if (isPadPortrait) 32.dp else horizontalPadding

    // ── Max widths (iOS: compact → .infinity; lines 370–428) ──

    /** Home page max content width */
    val homeMaxWidth: Dp
        get() = when {
            isLargePadPortrait -> 760.dp
            isPadPortrait -> 720.dp
            isWidePad -> 780.dp
            isTablet -> 700.dp
            else -> Dp.Unspecified
        }

    /** Main list pages max width */
    val mainMaxWidth: Dp
        get() = when {
            isLargePadPortrait -> 680.dp
            isPadPortrait -> 660.dp
            isWidePad -> 760.dp
            isTablet -> 680.dp
            else -> Dp.Unspecified
        }

    /** Form/settings pages max width */
    val formMaxWidth: Dp
        get() = when {
            isLargePadPortrait -> 620.dp
            isPadPortrait -> 600.dp
            isWidePad -> 640.dp
            isTablet -> 600.dp
            else -> Dp.Unspecified
        }

    /** Review page max width */
    val reviewMaxWidth: Dp
        get() = when {
            isLargePadPortrait -> 720.dp
            isPadPortrait -> 700.dp
            isWidePad -> 820.dp
            isTablet -> 760.dp
            else -> Dp.Unspecified
        }

    /** Generic content max width */
    val contentMaxWidth: Dp get() = mainMaxWidth

    // ── Per-page scales (iOS: lines 118–184, compact → 1.0) ──

    val homeScale: Float
        get() = when {
            isLargePadPortrait -> 1.36f
            isPadPortrait -> 1.30f
            isTablet -> 1.14f
            else -> 1f
        }

    val headerScale: Float
        get() = when {
            isLargePadPortrait -> 1.20f
            isPadPortrait -> 1.16f
            isTablet -> 1.14f
            else -> 1f
        }

    val reviewScale: Float
        get() = when {
            isLargePadPortrait -> 1.20f
            isPadPortrait -> 1.18f
            isTablet -> 1.15f
            else -> 1f
        }

    val reviewButtonScale: Float
        get() = when {
            isLargePadPortrait -> 1.18f
            isPadPortrait -> 1.14f
            else -> 1f
        }

    val cardScale: Float
        get() = when {
            isLargePadPortrait -> 1.24f
            isPadPortrait -> 1.18f
            isTablet -> 1.05f
            else -> 1f
        }

    val scopeScale: Float
        get() = when {
            isLargePadPortrait -> 1.16f
            isPadPortrait -> 1.10f
            else -> 1f
        }

    val overviewScale: Float
        get() = when {
            isLargePadPortrait -> 1.18f
            isPadPortrait -> 1.12f
            else -> 1f
        }

    val settingsScale: Float
        get() = when {
            isLargePadPortrait -> 1.16f
            isPadPortrait -> 1.10f
            else -> 1f
        }

    val settingsRowScale: Float
        get() = when {
            isLargePadPortrait -> 1.14f
            isPadPortrait -> 1.08f
            else -> 1f
        }

    val presetScale: Float
        get() = when {
            isLargePadPortrait -> 1.18f
            isPadPortrait -> 1.12f
            else -> 1f
        }

    val newPresetScale: Float
        get() = when {
            isLargePadPortrait -> 1.16f
            isPadPortrait -> 1.10f
            else -> 1f
        }

    val listCardScale: Float
        get() = when {
            isLargePadPortrait -> 1.16f
            isPadPortrait -> 1.10f
            else -> 1f
        }

    // ── iPad portrait top insets (iOS: ipadPortrait*TopInset properties) ──

    val ipadPortraitListPageTopInset: Dp
        get() = if (isPadPortrait) { if (isLargePadPortrait) 46.dp else 38.dp } else 0.dp

    val ipadPortraitOverviewTopInset: Dp
        get() = if (isPadPortrait) { if (isLargePadPortrait) 44.dp else 36.dp } else 0.dp

    val ipadPortraitFormPageTopInset: Dp
        get() = if (isPadPortrait) { if (isLargePadPortrait) 46.dp else 38.dp } else 0.dp

    val ipadPortraitSettingsTopInset: Dp
        get() = if (isPadPortrait) { if (isLargePadPortrait) 46.dp else 38.dp } else 0.dp

    val ipadPortraitPageTitleTopInset: Dp
        get() = if (isPadPortrait) { if (isLargePadPortrait) 96.dp else 84.dp } else 0.dp

    val ipadPortraitPageTitleFontSize: Float
        get() = if (isPadPortrait) { if (isLargePadPortrait) 36f else 35f } else 32f

    val ipadPortraitPageTitleSpacing: Dp
        get() = if (isPadPortrait) 24.dp else 18.dp

    val ipadPortraitPageSubtitleSpacing: Dp
        get() = if (isPadPortrait) 10.dp else 8.dp

    // ── Review portrait-specific overrides (iOS: lines 430–458) ──

    val reviewContentVerticalOffset: Dp
        get() = when {
            isPadPortrait -> if (heightDp < 760f) 8.dp else 18.dp
            isWidePad -> if (heightDp < 620f) 18.dp else 34.dp
            isTablet -> if (heightDp < 620f) 8.dp else 18.dp
            else -> 0.dp
        }

    val reviewActionBottomPadding: Dp
        get() = when {
            isPadPortrait -> if (heightDp < 760f) 24.dp else 34.dp
            isWidePad -> if (heightDp < 620f) 32.dp else 52.dp
            isTablet -> if (heightDp < 620f) 24.dp else 34.dp
            else -> 16.dp
        }

    val reviewContentVerticalPadding: Dp
        get() = if (isPadPortrait) 34.dp else if (isTablet) 30.dp else 24.dp

    val reviewContentTopPadding: Dp
        get() = if (isTablet) 20.dp else 12.dp

    /** Two-column layout for review on tablets */
    val reviewUsesTwoColumnLayout: Boolean get() = isTwoColumnCapable

    /** Home uses two-column layout on capable tablets */
    val homeUsesTwoColumnLayout: Boolean get() = isTwoColumnCapable

    // ── Top/bottom padding ──

    val titleTopPadding: Dp get() = 14.dp
    val pageTopPadding: Dp get() = 24.dp

    // ── Spacing ──

    val cardSpacing: Dp get() = 12.dp
    val sectionSpacing: Dp get() = 18.dp
    val compactSpacing: Dp get() = 8.dp

    // ── Back button ──

    val backButtonSize: Dp get() = 42.dp
    val backButtonTopPadding: Dp get() = 12.dp

    // ── Bottom safe area ──

    val bottomSafePadding: Dp get() = if (heightDp > 780f) 16.dp else 8.dp

    // ── Form top bar ──

    val formTopBarHeight: Dp get() = 48.dp

    // ── New preset ──

    val newPresetTextEditorMinHeight: Dp
        get() = when {
            isLargePadPortrait -> 380.dp
            isPadPortrait -> 340.dp
            else -> 260.dp
        }

    val newPresetTextEditorMaxHeight: Dp get() = (heightDp * 0.55f).dp

    val newPresetInputHeight: Dp
        get() = when {
            isLargePadPortrait -> 62.dp
            isPadPortrait -> 58.dp
            else -> 0.dp
        }

    // ── Scope grid ──

    val scopeGridMinimumWidth: Dp
        get() = when {
            isLargePadPortrait -> 176.dp
            isPadPortrait -> 164.dp
            else -> 132.dp
        }

    val scopeGridSpacing: Dp
        get() = if (isPadPortrait) 16.dp else 12.dp

    // ── Settings outer max width (iOS: settingsOuterMaxWidth / newPresetOuterMaxWidth) ──

    val settingsOuterMaxWidth: Dp
        get() = when {
            isLargePadPortrait -> 740.dp
            isPadPortrait -> 700.dp
            else -> formMaxWidth
        }

    val newPresetOuterMaxWidth: Dp
        get() = when {
            isLargePadPortrait -> 740.dp
            isPadPortrait -> 700.dp
            else -> formMaxWidth
        }

    // ── Adaptive top bar trailing width ──

    val adaptiveTopBarTrailingWidth: Dp
        get() = if (isPadPortrait) 64.dp else 42.dp

    // ── Landscape two-column widths ──

    /** Effective landscape home left column width */
    val homeLandscapeLeftWidth: Dp
        get() {
            if (!isTwoColumnCapable) return Dp.Unspecified
            val avail = horizontalPadding
            val maxW = minOf(widthDp - avail.value * 2, 1080f)
            val colGap = minOf(maxOf(maxW * 0.06f, 56f), 68f)
            val rightW = minOf(maxOf(maxW * 0.39f, 400f), 430f)
            return minOf(maxOf(maxW - rightW - colGap, 410f), 560f).dp
        }

    /** Effective landscape home right column width */
    val homeLandscapeRightWidth: Dp
        get() {
            if (!isTwoColumnCapable) return Dp.Unspecified
            val avail = horizontalPadding
            val maxW = minOf(widthDp - avail.value * 2, 1080f)
            return minOf(maxOf(maxW * 0.39f, 400f), 430f).dp
        }

    /** Landscape home right-column card scale — clamps to 1.0–1.05 */
    val homeLandscapeCardScale: Float
        get() {
            if (!isTwoColumnCapable) return 1.05f
            val rightW = homeLandscapeRightWidth.value
            return minOf(maxOf(rightW / 420f, 1.0f), 1.05f)
        }

    /** Landscape two-column gap — matches iOS metrics.homeLandscapeColumnSpacing */
    val homeLandscapeColumnSpacing: Dp
        get() {
            if (!isTwoColumnCapable) return 56.dp
            val avail = horizontalPadding
            val maxW = minOf(widthDp - avail.value * 2, 1080f)
            return minOf(maxOf(maxW * 0.06f, 56f), 68f).dp
        }

    /** Home landscape column height — matches iOS homeLandscapeContent columnHeight */
    val homeLandscapeColumnHeight: Dp
        get() {
            val constrainedHeight = minOf(maxOf(heightDp - 112f, 460f), 640f)
            return constrainedHeight.dp
        }

    /** Landscape bubble scale — matches iOS homeLandscapeBubbleScale */
    val homeLandscapeBubbleScale: Float
        get() {
            if (!isTwoColumnCapable) return 1.04f
            val leftW = homeLandscapeLeftWidth.value
            return minOf(maxOf(leftW / 500f * 1.04f, 1.0f), 1.12f)
        }

    /** Two-column review layout max width — matches iOS reviewLandscapeMaxWidth */
    val reviewLandscapeMaxWidth: Dp
        get() = 1160.dp

    /** Two-column review available width — matches iOS reviewLandscapeAvailableWidth */
    val reviewLandscapeAvailableWidth: Dp
        get() {
            if (!isTwoColumnCapable) return Dp.Unspecified
            val available = maxOf(widthDp - horizontalPadding.value * 2, 0f)
            return minOf(available, reviewLandscapeMaxWidth.value).dp
        }

    /** Two-column review column spacing — matches iOS reviewLandscapeColumnSpacing */
    val reviewLandscapeColumnSpacing: Dp
        get() {
            if (!isTwoColumnCapable) return 0.dp
            return minOf(maxOf(reviewLandscapeAvailableWidth.value * 0.055f, 48f), 64f).dp
        }

    /** Two-column review right column width — matches iOS reviewLandscapeRightWidth */
    val reviewLandscapeRightWidth: Dp
        get() {
            if (!isTwoColumnCapable) return Dp.Unspecified
            return minOf(maxOf(reviewLandscapeAvailableWidth.value * 0.32f, 340f), 380f).dp
        }

    /** Two-column review left column width — matches iOS reviewLandscapeLeftWidth */
    val reviewLandscapeLeftWidth: Dp
        get() {
            if (!isTwoColumnCapable) return Dp.Unspecified
            return maxOf(
                reviewLandscapeAvailableWidth.value - reviewLandscapeRightWidth.value - reviewLandscapeColumnSpacing.value,
                0f
            ).dp
        }

    /** iPad portrait card edge inset — matches iOS cardEdgeInset */
    val homeCardEdgeInset: Dp
        get() {
            if (!isPadPortrait) return 0.dp
            val cw = homeMaxWidth.value
            return maxOf(
                horizontalPadding.value,
                (widthDp - cw) / 2f + horizontalPadding.value
            ).dp
        }

    companion object {
        fun compute(
            widthPx: Float,
            heightPx: Float,
            density: Float,
            fontScale: Float = 1f,
        ): KikariaPhoneMetrics {
            val widthDp = widthPx / density
            val heightDp = heightPx / density
            return KikariaPhoneMetrics(
                widthDp = widthDp,
                heightDp = heightDp,
                density = density,
                fontScale = fontScale,
            )
        }
    }
}

@Composable
fun rememberKikariaPhoneMetrics(): KikariaPhoneMetrics {
    val config = LocalConfiguration.current
    val density = LocalDensity.current.density
    val fontScale = config.fontScale
    val widthDp = config.screenWidthDp.toFloat()
    val heightDp = config.screenHeightDp.toFloat()
    return remember(widthDp, heightDp, density, fontScale) {
        KikariaPhoneMetrics(
            widthDp = widthDp,
            heightDp = heightDp,
            density = density,
            fontScale = fontScale,
        )
    }
}
