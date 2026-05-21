package com.vita0818.kikaria.ui.components

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.AddCircle
import androidx.compose.material.icons.filled.Book
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Lightbulb
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.MenuBook
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Photo
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.RemoveCircle
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Share
import androidx.compose.material.icons.filled.Shuffle
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.Tag
import androidx.compose.material.icons.filled.UploadFile
import androidx.compose.material.icons.filled.Verified
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
    /** Source: "arrow.right" — start review / play */
    val play: ImageVector = Icons.Filled.PlayArrow

    /** Source: "shuffle" — next / random */
    val shuffle: ImageVector = Icons.Filled.Shuffle

    /** Source: "plus.circle.fill" — add to collection */
    val addCircle: ImageVector = Icons.Filled.AddCircle

    /** Source: "minus.circle.fill" — remove from collection */
    val removeCircle: ImageVector = Icons.Filled.RemoveCircle

    /** Source: "plus" — add / create */
    val add: ImageVector = Icons.Filled.Add

    /** Source: "pencil" — edit */
    val edit: ImageVector = Icons.Filled.Edit

    /** Source: "trash" — delete */
    val delete: ImageVector = Icons.Filled.Delete

    /** Source: "square.and.arrow.up" — export / share */
    val share: ImageVector = Icons.Filled.Share

    /** Source: "doc.badge.plus" — import file */
    val uploadFile: ImageVector = Icons.Filled.UploadFile

    // ── Profile ──
    /** Source: "person.crop.circle.fill" — profile avatar fallback */
    val person: ImageVector = Icons.Filled.Person

    /** Source: "photo" — change avatar */
    val photo: ImageVector = Icons.Filled.Photo

    // ── Study ──
    /** Source: "lightbulb" / "lightbulb.max.fill" — hint */
    val hint: ImageVector = Icons.Filled.Lightbulb

    /** Source: "doc.text" — answer / content */
    val document: ImageVector = Icons.Filled.Description

    /** Source: "sparkles" — reinforcement / starred */
    val reinforcement: ImageVector = Icons.Filled.Star

    /** Source: "checkmark.seal" / "checkmark.seal.fill" — mastered */
    val mastered: ImageVector = Icons.Filled.Verified

    /** Source: "checkmark.circle.fill" — completed */
    val checkCircle: ImageVector = Icons.Filled.CheckCircle

    /** Source: "magnifyingglass" — search */
    val search: ImageVector = Icons.Filled.Search

    /** Source: "xmark.circle.fill" — clear search */
    val clearSearch: ImageVector = Icons.Filled.Close

    /** Source: "books.vertical.fill" — onboarding / presets */
    val books: ImageVector = Icons.Filled.MenuBook

    // ── Navigation destinations ──
    /** Source: "calendar" — calendar / today overview */
    val calendar: ImageVector = Icons.Filled.CalendarMonth

    /** Source: "slider.horizontal.3" — preset management */
    val presets: ImageVector = Icons.Filled.List

    /** Source: "rectangle.3.group" — dashboard */
    val dashboard: ImageVector = Icons.Filled.Book

    // ── Settings ──
    /** Source: "gear" / settings */
    val settings: ImageVector = Icons.Filled.Settings

    // ── Status ──
    /** Source: "tag.slash" — empty tags */
    val tagSlash: ImageVector = Icons.Filled.Tag

    // ── Text-based fallbacks (when vector icons are too heavy) ──
    /** Single left-pointing angle quotation mark — lightweight back */
    const val TEXT_BACK = "\u2039"

    /** Single right-pointing angle quotation mark — lightweight forward */
    const val TEXT_FORWARD = "\u203A"

    /** Left-right arrows — lightweight shuffle/next */
    const val TEXT_NEXT = "\u21C4"

    /** ▶ (used in iOS start button as arrow.right) */
    const val TEXT_PLAY = "\u25B6"

    /** Arrow right → used for start button */
    const val TEXT_ARROW_RIGHT = "\u2192"

    /** Check mark ✓ */
    const val TEXT_CHECK = "\u2713"
}
