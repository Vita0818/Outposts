/**
 * KikariaAdaptiveLayout - Responsive layout helpers for tablet/phone.
 * Ported from KikariaAdaptiveLayout.swift.
 *
 * Provides width categories, padding, scale factors, and two-column
 * layout constants matching the iOS adaptive metrics.
 */
import { display } from '@kit.ArkUI'

let _cachedWidthVP: number = 0
let _cachedHeightVP: number = 0

function refreshScreenMetrics(): void {
  try {
    const d = display.getDefaultDisplaySync()
    const w = d.width
    const h = d.height
    const density = d.densityPixels
    _cachedWidthVP = density > 0 ? Math.round(w / density) : w
    _cachedHeightVP = density > 0 ? Math.round(h / density) : h
  } catch (e) {
    _cachedWidthVP = 390
    _cachedHeightVP = 844
  }
}

function getScreenWidthVP(): number {
  if (_cachedWidthVP <= 0) {
    refreshScreenMetrics()
  }
  return _cachedWidthVP
}

function getScreenHeightVP(): number {
  if (_cachedHeightVP <= 0) {
    refreshScreenMetrics()
  }
  return _cachedHeightVP
}

export function refreshLayoutCache(): void {
  refreshScreenMetrics()
}

// Width category matching KikariaAdaptiveLayout.WidthCategory
export function isTabletWidth(): boolean {
  return getScreenWidthVP() >= 600
}

export function isPadLandscape(): boolean {
  return getScreenWidthVP() >= 900
}

export function isPadPortrait(): boolean {
  return isTabletWidth() && getScreenHeightVP() >= getScreenWidthVP()
}

export function isTwoColumnCapable(): boolean {
  return getScreenWidthVP() >= 950 && getScreenWidthVP() > getScreenHeightVP()
}

/** Returns max content width in vp based on screen */
export function contentMaxWidth(): number {
  const w = getScreenWidthVP()
  if (w >= 900) return 760
  if (w >= 600) return 600
  return w
}

/** Returns horizontal page padding */
export function pageHorizontalPadding(): number {
  const w = getScreenWidthVP()
  if (w >= 1200) return 40
  if (w >= 900) return 40
  if (w >= 600) return 32
  return 24
}

// ---- Scale factors matching iOS adaptive metrics ----

export function homeScale(): number {
  const w = getScreenWidthVP()
  const h = getScreenHeightVP()
  if (isPadPortrait()) return w >= 900 ? 1.36 : 1.30
  return isTabletWidth() ? 1.14 : 1.0
}

export function homeHeaderScale(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 1.20 : 1.16
  return isTabletWidth() ? 1.14 : 1.0
}

export function reviewScale(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 1.20 : 1.18
  return isTabletWidth() ? 1.15 : 1.0
}

export function reviewButtonScale(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 1.18 : 1.14
  return 1.0
}

export function cardScale(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 1.24 : 1.18
  return isTabletWidth() ? 1.05 : 1.0
}

export function presetScale(): number {
  const w = getScreenWidthVP()
  return isPadPortrait() ? (w >= 900 ? 1.18 : 1.12) : 1.0
}

export function scopeScale(): number {
  const w = getScreenWidthVP()
  return isPadPortrait() ? (w >= 900 ? 1.16 : 1.10) : 1.0
}

export function overviewScale(): number {
  const w = getScreenWidthVP()
  return isPadPortrait() ? (w >= 900 ? 1.18 : 1.12) : 1.0
}

export function settingsScale(): number {
  const w = getScreenWidthVP()
  return isPadPortrait() ? (w >= 900 ? 1.16 : 1.10) : 1.0
}

export function settingsRowScale(): number {
  const w = getScreenWidthVP()
  return isPadPortrait() ? (w >= 900 ? 1.14 : 1.08) : 1.0
}

export function newPresetScale(): number {
  const w = getScreenWidthVP()
  return isPadPortrait() ? (w >= 900 ? 1.16 : 1.10) : 1.0
}

export function listCardScale(): number {
  const w = getScreenWidthVP()
  return isPadPortrait() ? (w >= 900 ? 1.16 : 1.10) : 1.0
}

// ---- Two-column landscape layout constants ----

export function homeLandscapeMaxWidth(): number {
  return 1080
}

export function homeLandscapeAvailableWidth(): number {
  const w = getScreenWidthVP()
  return Math.max(0, Math.min(w - pageHorizontalPadding() * 2, homeLandscapeMaxWidth()))
}

export function homeLandscapeColumnSpacing(): number {
  const aw = homeLandscapeAvailableWidth()
  return Math.min(Math.max(aw * 0.06, 56), 68)
}

export function homeLandscapeRightWidth(): number {
  const aw = homeLandscapeAvailableWidth()
  return Math.min(Math.max(aw * 0.39, 400), 430)
}

export function homeLandscapeLeftWidth(): number {
  const aw = homeLandscapeAvailableWidth()
  return Math.min(Math.max(aw - homeLandscapeRightWidth() - homeLandscapeColumnSpacing(), 410), 560)
}

export function homeLandscapeBubbleScale(): number {
  return Math.min(Math.max(homeLandscapeLeftWidth() / 500 * 1.04, 1.0), 1.12)
}

export function homeLandscapeCardScale(): number {
  return Math.min(Math.max(homeLandscapeRightWidth() / 420, 1.0), 1.05)
}

export function reviewLandscapeMaxWidth(): number {
  return 1160
}

export function reviewLandscapeAvailableWidth(): number {
  const w = getScreenWidthVP()
  return Math.max(0, Math.min(w - pageHorizontalPadding() * 2, reviewLandscapeMaxWidth()))
}

export function reviewLandscapeColumnSpacing(): number {
  const aw = reviewLandscapeAvailableWidth()
  return Math.min(Math.max(aw * 0.055, 48), 64)
}

export function reviewLandscapeRightWidth(): number {
  const aw = reviewLandscapeAvailableWidth()
  return Math.min(Math.max(aw * 0.32, 340), 380)
}

export function reviewLandscapeLeftWidth(): number {
  const aw = reviewLandscapeAvailableWidth()
  return Math.max(0, aw - reviewLandscapeRightWidth() - reviewLandscapeColumnSpacing())
}

export function settingsLandscapeMaxWidth(): number {
  return 1080
}

export function settingsLandscapeAvailableWidth(): number {
  const w = getScreenWidthVP()
  return Math.max(0, Math.min(w - pageHorizontalPadding() * 2, settingsLandscapeMaxWidth()))
}

export function settingsLandscapeColumnSpacing(): number {
  const aw = settingsLandscapeAvailableWidth()
  return Math.min(Math.max(aw * 0.06, 56), 72)
}

export function settingsLandscapeLeftWidth(): number {
  const aw = settingsLandscapeAvailableWidth()
  return Math.min(Math.max(aw * 0.34, 320), 380)
}

export function settingsLandscapeRightWidth(): number {
  const aw = settingsLandscapeAvailableWidth()
  return Math.max(0, aw - settingsLandscapeLeftWidth() - settingsLandscapeColumnSpacing())
}

// ---- Max-width helpers for different page types ----

export function mainMaxWidth(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 680 : 660
  if (w >= 900) return 760
  if (w >= 600) return 680
  return w
}

export function formMaxWidth(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 620 : 600
  if (w >= 900) return 640
  if (w >= 600) return 600
  return w
}

export function reviewMaxWidth(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 720 : 700
  if (w >= 900) return 820
  if (w >= 600) return 760
  return w
}

// ---- Avatar size helper ----
export function avatarSize(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 116 : 108
  return isTabletWidth() ? 108 : 88
}

// ---- Setup card max width ----
export function setupCardMaxWidth(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return 500
  return isTabletWidth() ? 480 : 370
}

// ---- iPad Portrait top insets (matching iOS ipadPortrait*Inset) ----

export function ipadPortraitListPageTopInset(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 46 : 38
  return 0
}

export function ipadPortraitOverviewTopInset(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 44 : 36
  return 0
}

export function ipadPortraitFormPageTopInset(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 46 : 38
  return 0
}

export function ipadPortraitSettingsTopInset(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 46 : 38
  return 0
}

export function ipadPortraitPageTitleTopInset(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 96 : 84
  return 0
}

export function ipadPortraitPageTitleFontSize(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 36 : 35
  return 32
}

export function ipadPortraitPageTitleSpacing(): number {
  return isPadPortrait() ? 24 : 18
}

export function ipadPortraitPageSubtitleSpacing(): number {
  return isPadPortrait() ? 10 : 8
}

// ---- iPad portrait home screen sizing ----

export function padPortraitHomeTitleSize(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 58 : 54
  return 39
}

export function padPortraitHomeAvatarSize(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 66 : 62
  return 44
}

export function padPortraitHomeTopPadding(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 58 : 48
  return 14
}

export function padPortraitHomeBubbleSize(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 220 : 200
  return 200
}

export function padPortraitHomeBubbleFontSize(): number {
  const w = getScreenWidthVP()
  if (isPadPortrait()) return w >= 900 ? 37 : 34
  return 32
}
