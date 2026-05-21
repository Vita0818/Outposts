package com.vita0818.kikaria.ui.components

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Star
import androidx.compose.ui.graphics.vector.ImageVector

/**
 * Centralized Kikaria icon mapping from iOS SF Symbols to Android Material Icons.
 *
 * Each source SF Symbol name is mapped to the closest semantic and visual
 * Android/Compose equivalent.  When a precise match is unavailable the
 * mapping uses the nearest available material icon.
 *
 * Do NOT scatter Material Icons directly across screens — use this object
 * so that icon swaps stay in one place.
 */
object KikariaIcons {
    // ── Navigation ──
    /** Source: "chevron.left" — back navigation */
    val back: ImageVector = Icons.AutoMirrored.Filled.ArrowBack

    /** Source: "chevron.right" — forward disclosure */
    val forward: ImageVector = Icons.Filled.ChevronRight

    /** Source: "xmark" — close / dismiss */
    val close: ImageVector = Icons.Filled.Close

    // ── Actions ──
    /** Source: "play.fill" or start-review button — play / start */
    val play: ImageVector = Icons.Filled.PlayArrow

    /** Source: "arrow.triangle.2.circlepath" used in next / shuffle — refresh */
    val next: ImageVector = Icons.Filled.Refresh

    // ── Profile ──
    /** Source: "person.crop.circle.fill" — profile avatar fallback */
    val person: ImageVector = Icons.Filled.Person

    // ── Reinforcement ──
    /** Source: "sparkles" — reinforcement / starred */
    val reinforcement: ImageVector = Icons.Filled.Star

    // ── Text-based fallbacks (when vector icons are too heavy) ──
    const val TEXT_BACK = "\u2039"       // single left-pointing angle quotation mark
    const val TEXT_FORWARD = "\u203A"    // single right-pointing angle quotation mark
    const val TEXT_NEXT = "\u21C4"       // ⇄ left-right arrows
    const val TEXT_PLAY = "\u25B6"       // ▶ (used in iOS start button as "→")
}
