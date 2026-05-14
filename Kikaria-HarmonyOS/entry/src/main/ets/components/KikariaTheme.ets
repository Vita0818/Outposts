/**
 * KikariaTheme - Color palette and visual style constants.
 * Translated from the KikariaTheme enum in source/Kikaria/ContentView.swift
 *
 * The original uses adaptive (light/dark) colors via ColorScheme.
 * In HarmonyOS we use resource colors and ColorMode detection for a similar effect.
 */

export class KikariaColors {
  // Primary blues
  static readonly SKY_LIGHT = '#63BAF5'
  static readonly SKY_DARK = '#4DB8F5'
  static readonly CYAN_LIGHT = '#91E0E8'
  static readonly CYAN_DARK = '#52CCD1'
  static readonly MIST_LIGHT = '#E8F7FC'
  static readonly MIST_DARK = '#152938'
  static readonly BLUE_GRAY_LIGHT = '#9EB8CC'
  static readonly BLUE_GRAY_DARK = '#7A9CB8'

  // Mastered greens
  static readonly MASTERED_GREEN_LIGHT = '#5CC28A'
  static readonly MASTERED_GREEN_DARK = '#52D199'
  static readonly MASTERED_DEEP_GREEN_LIGHT = '#1E784D'
  static readonly MASTERED_DEEP_GREEN_DARK = '#94F0BD'
  static readonly MASTERED_COMPLETED_GREEN_LIGHT = '#C9EDD6'
  static readonly MASTERED_COMPLETED_GREEN_DARK = '#2E614D'

  // Action colors
  static readonly NEXT_AMBER_LIGHT = '#8A7DBF'
  static readonly NEXT_AMBER_DARK = '#8C75D1'
  static readonly REMOVE_CORAL_LIGHT = '#DB524D'
  static readonly REMOVE_CORAL_DARK = '#FA6B6B'

  // Text colors
  static readonly DEEP_TEXT_LIGHT = '#214054'
  static readonly DEEP_TEXT_DARK = '#E6F5FF'
  static readonly SOFT_TEXT_LIGHT = '#6B8A9E'
  static readonly SOFT_TEXT_DARK = '#A8C4DB'
  static readonly TERTIARY_TEXT_LIGHT = '#94ADBF'
  static readonly TERTIARY_TEXT_DARK = '#6E8CA8'

  // Surface / glass
  static readonly GLASS_SURFACE_LIGHT = '#FFFFFF'
  static readonly GLASS_SURFACE_DARK = '#0F2130'
  static readonly GLASS_STROKE_ACCENT_LIGHT = '#91E0E8'
  static readonly GLASS_STROKE_ACCENT_DARK = '#6BD6ED'
  static readonly SHADOW_LIGHT = '#63BAF5'
  static readonly SHADOW_DARK = '#00050D'

  // Bubble colors
  static readonly BUBBLE_MINT_LIGHT = '#BAF2E6'
  static readonly BUBBLE_MINT_DARK = '#33948A'
  static readonly BUBBLE_LAVENDER_LIGHT = '#BFC7FF'
  static readonly BUBBLE_LAVENDER_DARK = '#524D94'
  static readonly BUBBLE_GREEN_LIGHT = '#C7F2BD'
  static readonly BUBBLE_GREEN_DARK = '#338057'
  static readonly BUBBLE_WHITE_LIGHT = '#FFFFFF'
  static readonly BUBBLE_WHITE_DARK = '#263B54'

  // Page background approximations (gradients aren't directly available as constants)
  static readonly PAGE_BG_LIGHT = '#EDF9FF'
  static readonly PAGE_BG_DARK = '#05121C'

  // Functional aliases - use light versions as default
  static SKY = KikariaColors.SKY_LIGHT
  static CYAN = KikariaColors.CYAN_LIGHT
  static MIST = KikariaColors.MIST_LIGHT
  static DEEP_TEXT = KikariaColors.DEEP_TEXT_LIGHT
  static SOFT_TEXT = KikariaColors.SOFT_TEXT_LIGHT
  static GLASS_SURFACE = KikariaColors.GLASS_SURFACE_LIGHT
  static MASTERED_GREEN = KikariaColors.MASTERED_GREEN_LIGHT
  static NEXT_AMBER = KikariaColors.NEXT_AMBER_LIGHT
  static REMOVE_CORAL = KikariaColors.REMOVE_CORAL_LIGHT
}

/**
 * Typography scale translated from KikariaTypography in source/Kikaria/KikariaTypography.swift
 * HarmonyOS uses 'vp' (virtual pixels) for font sizes.
 */
export class KikariaTypography {
  static readonly APP_TITLE_SIZE = 39
  static readonly CHINESE_LARGE_TITLE = 34
  static readonly CHINESE_TITLE = 32
  static readonly CHINESE_HEADLINE = 17
  static readonly CHINESE_BODY = 15
  static readonly CHINESE_BUTTON = 17
  static readonly CHINESE_CAPTION = 12
  static readonly TAG_SIZE = 12
}
