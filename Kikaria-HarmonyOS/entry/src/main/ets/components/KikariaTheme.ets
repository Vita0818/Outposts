/**
 * KikariaTheme - Color palette and visual style constants.
 * Translated from KikariaTheme enum in source/Kikaria/ContentView.swift
 *
 * Uses adaptive (light/dark) colors via AppStorage reactivity.
 * AppStorage is a built-in ArkTS global, no import needed.
 */

const DARK_MODE_KEY = 'kikaria_isDarkMode'

/** Helper: reads current dark mode state from AppStorage at call time. */
function isDark(): boolean {
  const ref = AppStorage.ref<boolean>(DARK_MODE_KEY)
  return ref !== undefined ? ref.get() === true : false
}

/** Adaptive color helper — returns light or dark value based on current mode. */
function adaptive(light: string, dark: string): string {
  return isDark() ? dark : light
}

export class KikariaColors {
  static get SKY(): string { return adaptive('#63BAF5', '#4DAFDF') }
  static get SKY_LIGHT(): string { return adaptive('#59A4E8', '#4590CC') }
  static get CYAN(): string { return adaptive('#91E0E8', '#52CCD2') }
  static get CYAN_LIGHT(): string { return adaptive('#7BCFD6', '#45B3BA') }
  static get MIST(): string { return adaptive('#E8F7FC', '#152838') }
  static get BLUE_GRAY_LIGHT(): string { return adaptive('#9EB8CC', '#7A9CB8') }
  static get BLUE_GRAY_DARK(): string { return adaptive('#7A9CB8', '#5A7A96') }

  static get MASTERED_GREEN(): string { return adaptive('#5CC28A', '#52D180') }
  static get MASTERED_DEEP_GREEN(): string { return adaptive('#1E784D', '#93EFBD') }
  static get MASTERED_COMPLETED_GREEN(): string { return adaptive('#C9EDD6', '#2E614C') }

  static get NEXT_AMBER(): string { return adaptive('#8A7DBF', '#8C75D1') }
  static get REMOVE_CORAL(): string { return adaptive('#DB524D', '#FA6B6B') }

  static get DEEP_TEXT(): string { return adaptive('#214054', '#E5F0FF') }
  static get SOFT_TEXT(): string { return adaptive('#6B8A9E', '#A9C0D6') }
  static get TERTIARY_TEXT(): string { return adaptive('#94ADBF', '#6E8BA5') }

  static get GLASS_SURFACE(): string { return adaptive('#FFFFFF', '#0F1F2D') }
  static get GLASS_STROKE_ACCENT(): string { return adaptive('#91E0E8', '#6BD6EF') }

  static get PAGE_BG(): string { return adaptive('#EDF9FF', '#0A1625') }
  static get PAGE_BG_START(): string { return adaptive('#EDF9FF', '#0A1625') }
  static get PAGE_BG_MID(): string { return adaptive('#DBF2F8', '#0D1E30') }
  static get PAGE_BG_END(): string { return adaptive('#F5FAFC', '#081420') }

  static get BUBBLE_MINT(): string { return adaptive('#BAF2E6', '#33947A') }
  static get BUBBLE_LAVENDER(): string { return adaptive('#BFC7FF', '#4D458F') }
  static get BUBBLE_GREEN(): string { return adaptive('#C7F2BD', '#3A6E33') }
  static get BUBBLE_WHITE(): string { return adaptive('#FFFFFF', '#1A2A3A') }

  static get ACTION_GRADIENT_START(): string { return adaptive('#59A4E8', '#4590CC') }
  static get ACTION_GRADIENT_END(): string { return adaptive('#7FD6DA', '#4FB4BA') }
  static get MASTERED_GRADIENT_START(): string { return adaptive('#63C78B', '#4AAD70') }
  static get MASTERED_GRADIENT_END(): string { return adaptive('#ADE8C2', '#3C8A5C') }
  static get NEXT_GRADIENT_START(): string { return adaptive('#C7B8F0', '#7A6ABB') }
  static get NEXT_GRADIENT_END(): string { return adaptive('#9487CC', '#5C4F94') }
  static get REMOVE_GRADIENT_START(): string { return adaptive('#E56159', '#CC3D3D') }
  static get REMOVE_GRADIENT_END(): string { return adaptive('#FA9480', '#E06060') }

  static get SHADOW_COLOR(): string { return adaptive('#1A63BAF5', '#1A000000') }
  static get CARD_BG(): string { return adaptive('#FFFFFF', '#132333') }
  static get CARD_BG_TRANSLUCENT(): string { return adaptive('#FFFFFFCC', '#132333CC') }
  static get OVERLAY_BG(): string { return adaptive('#80000000', '#B0000000') }
}

/** Toggle dark mode programmatically. Call from any component. */
export function toggleDarkMode(): void {
  const ref = AppStorage.setAndRef<boolean>(DARK_MODE_KEY, false)
  ref.set(!ref.get())
}

export function isDarkModeEnabled(): boolean {
  const ref = AppStorage.ref<boolean>(DARK_MODE_KEY)
  return ref !== undefined ? ref.get() === true : false
}

/**
 * Typography scale translated from KikariaTypography
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
  static readonly NUMBER_MEDIUM = 28
}
