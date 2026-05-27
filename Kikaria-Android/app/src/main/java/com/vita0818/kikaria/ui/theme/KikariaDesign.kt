package com.vita0818.kikaria.ui.theme

import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

/**
 * Centralized design tokens for spacing, shape, and layout,
 * translated from the Kikaria iOS design system.
 *
 * These tokens enforce the soft, spacious, book-like visual rhythm
 * of the iOS app across all Android pages.
 */
object KikariaDesign {

    // ── Corner Radii ──

    /** Default glass card corner radius (iOS liquidGlassCard default) */
    val CardRadius: Dp = 28.dp

    /** Large card corner radius (iOS progress card / hero cards) */
    val CardRadiusLarge: Dp = 40.dp

    /** Medium card corner radius (iOS FloatingInfoCard compact) */
    val CardRadiusMedium: Dp = 26.dp

    /** Small card / input field corner radius */
    val CardRadiusSmall: Dp = 20.dp

    /** Pill / capsule corner radius */
    val PillRadius: Dp = 16.dp

    // ── Page Layout ──

    /** Horizontal page content padding */
    val PageHorizontal: Dp = 24.dp

    /** Top padding after back button / title area */
    val PageTop: Dp = 70.dp

    /** Vertical gap between major card sections on a page */
    val SectionGap: Dp = 12.dp

    /** Vertical gap between minor elements within a section */
    val ElementGap: Dp = 8.dp

    // ── Card Interior ──

    /** Standard card interior padding */
    val CardPadding: Dp = 18.dp

    /** Spacious card interior padding (progress / hero cards) */
    val CardPaddingSpacious: Dp = 22.dp

    // ── Back Button ──

    /** Back button size */
    val BackButtonSize: Dp = 42.dp

    /** Back button left margin */
    val BackButtonLeft: Dp = 24.dp

    /** Back button top margin */
    val BackButtonTop: Dp = 12.dp
}
