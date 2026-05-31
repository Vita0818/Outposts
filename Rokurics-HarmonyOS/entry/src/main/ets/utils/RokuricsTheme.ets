/**
 * Theme system - mirrors RokuricsColors.swift + RokuricsTypography.swift + glass modifiers
 * Supports light/dark dual-palette via applyColorMode().
 */

import { ConfigurationConstant } from '@kit.AbilityKit'

// ── Color utility: converts #RRGGBB base + AA alpha suffix to HarmonyOS #AARRGGBB ──
export function colorAlpha(baseHex: string, alphaHex: string): string {
  return '#' + alphaHex + baseHex.substring(1)
}

// ── Color palette interface (enables dot-notation: RokuricsColors.aqua) ──
interface ColorPalette {
  aqua: string
  mint: string
  mistGreen: string
  softTeal: string
  skyCyan: string
  paleAqua: string
  coral: string
  deepText: string
  softText: string
  tertiaryText: string
  glassSurface: string
  glassStroke: string
  glassStrokeAccent: string
  shadowColor: string
  pageBackground: string
  actionStart: string
  actionEnd: string
  recordingAccentStart: string
  recordingAccentEnd: string
  pageGradientStart: string
  pageGradientMid: string
  pageGradientEnd: string
  actionGradientStart: string
  actionGradientEnd: string
}

// ── Dark palette (mirrors RokuricsColors.swift adaptive dark values) ──
const DarkPalette: ColorPalette = {
  aqua: '#57D6D1',
  mint: '#52BD94',
  mistGreen: '#0F2B26',
  softTeal: '#85CCCC',
  skyCyan: '#4DB3EB',
  paleAqua: '#266B61',
  coral: '#F5757A',

  deepText: '#E6FAF7',
  softText: '#A8D1D1',
  tertiaryText: '#759EA1',

  glassSurface: '#0D2424',
  glassStroke: '#8ADBD1',
  glassStrokeAccent: '#61D4C2',
  shadowColor: '#000808',

  pageBackground: '#051414',

  actionStart: '#128080',
  actionEnd: '#2BAB82',

  recordingAccentStart: '#D14257',
  recordingAccentEnd: '#E0706B',

  pageGradientStart: '#051414',
  pageGradientMid: '#0A2B29',
  pageGradientEnd: '#030D12',

  actionGradientStart: '#128080',
  actionGradientEnd: '#2BAB82',
}

// ── Light palette (derived from iOS light mode adaptive values) ──
const LightPalette: ColorPalette = {
  aqua: '#1A8A85',
  mint: '#3A9D74',
  mistGreen: '#C8F0E6',
  softTeal: '#5AAAAA',
  skyCyan: '#2D93CB',
  paleAqua: '#A8DFD4',
  coral: '#E0555A',

  deepText: '#0A1F1C',
  softText: '#3D6B66',
  tertiaryText: '#5C8A84',

  glassSurface: '#EAF7F4',
  glassStroke: '#6BBBB1',
  glassStrokeAccent: '#4DAFA2',
  shadowColor: '#C0D0CC',

  pageBackground: '#EEF7F5',

  actionStart: '#0E6E6E',
  actionEnd: '#23996E',

  recordingAccentStart: '#C83A4F',
  recordingAccentEnd: '#D6605C',

  pageGradientStart: '#EDF6F4',
  pageGradientMid: '#DAEEE9',
  pageGradientEnd: '#F0F7F5',

  actionGradientStart: '#0E6E6E',
  actionGradientEnd: '#23996E',
}

// ── Active color set (mutated by applyColorMode) ──
let _activePalette: ColorPalette = { ...DarkPalette }

// ── Public color accessor (backward-compatible; reads from active palette) ──
export const RokuricsColors: ColorPalette = _activePalette

/**
 * Switch the active color palette. Call this at the top of each page's build()
 * or in response to @StorageLink('colorMode') changes.
 */
export function applyColorMode(mode: ConfigurationConstant.ColorMode): void {
  const isDark = mode === ConfigurationConstant.ColorMode.COLOR_MODE_DARK
  const target = isDark ? DarkPalette : LightPalette
  _activePalette.aqua = target.aqua
  _activePalette.mint = target.mint
  _activePalette.mistGreen = target.mistGreen
  _activePalette.softTeal = target.softTeal
  _activePalette.skyCyan = target.skyCyan
  _activePalette.paleAqua = target.paleAqua
  _activePalette.coral = target.coral
  _activePalette.deepText = target.deepText
  _activePalette.softText = target.softText
  _activePalette.tertiaryText = target.tertiaryText
  _activePalette.glassSurface = target.glassSurface
  _activePalette.glassStroke = target.glassStroke
  _activePalette.glassStrokeAccent = target.glassStrokeAccent
  _activePalette.shadowColor = target.shadowColor
  _activePalette.pageBackground = target.pageBackground
  _activePalette.actionStart = target.actionStart
  _activePalette.actionEnd = target.actionEnd
  _activePalette.recordingAccentStart = target.recordingAccentStart
  _activePalette.recordingAccentEnd = target.recordingAccentEnd
  _activePalette.pageGradientStart = target.pageGradientStart
  _activePalette.pageGradientMid = target.pageGradientMid
  _activePalette.pageGradientEnd = target.pageGradientEnd
  _activePalette.actionGradientStart = target.actionGradientStart
  _activePalette.actionGradientEnd = target.actionGradientEnd
}

/**
 * Return page gradient color stops for the current mode.
 */
export function pageGradientColors(): Array<[string, number]> {
  return [
    [_activePalette.pageGradientStart, 1.0],
    [_activePalette.pageGradientMid, 1.0],
    [_activePalette.pageGradientEnd, 1.0]
  ]
}

// ── Typography ──
export interface Font {
  size: number
  weight: number
  family?: string
}

export enum FontWeight {
  Regular = 400,
  Medium = 500,
  SemiBold = 600,
  Bold = 700
}

export class RokuricsTypography {
  static appTitle(size: number = 39): Font {
    return { size: size, weight: FontWeight.Medium, family: 'serif' }
  }
  static pageTitle(size: number = 30): Font {
    return { size: size, weight: FontWeight.Bold }
  }
  static sectionTitle(size: number = 17): Font {
    return { size: size, weight: FontWeight.SemiBold }
  }
  static body(size: number = 15): Font {
    return { size: size, weight: FontWeight.Regular }
  }
  static caption(size: number = 12): Font {
    return { size: size, weight: FontWeight.Medium }
  }
  static largeNumber(size: number = 42): Font {
    return { size: size, weight: FontWeight.Bold }
  }
  static button(size: number = 17): Font {
    return { size: size, weight: FontWeight.SemiBold }
  }
}

// ── Dark-mode glass opacity scaling (mirrors Apple's colorScheme adaptation) ──
// Apple reduces glass opacity by ~0.78× in dark mode across all glass modifiers.
// These constants are the light-mode values. For dark mode, multiply by 0.78.

let DARK_MODE_SCALE = 0.78

export function darkModeGlassOpacity(lightHexAlpha: string, isDark: boolean): string {
  if (!isDark) return lightHexAlpha
  const alpha = parseInt(lightHexAlpha, 16)
  const scaled = Math.max(4, Math.floor(alpha * DARK_MODE_SCALE))
  return scaled.toString(16).padStart(2, '0').toUpperCase()
}

// Pre-computed glass opacity strings (hex alpha for appending to colors).
// Dark mode defaults (scaled from light 0.78× fill, 0.82× stroke, per Apple RokuricsGlassStyle):
export const glassFillOpacity = '5C'
export const glassStrokeHighOpacity = '38'
export const glassStrokeMidOpacity = '0E'
export const glassAccentOpacity = '0A'

// Reusable glass card backing (mirrors rokuricsLiquidGlassCard dark mode) ──
@Builder
export function GlassCardContent(child: WrappedBuilder<[]>): void {
  Column() {
    child.builder()
  }
  .borderRadius(20)
  .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
  .shadow({
    color: colorAlpha(RokuricsColors.shadowColor, '18'),
    radius: 18,
    offsetY: 10
  })
  .border({
    width: 1,
    color: {
      colors: [
        [0xFFFFFF, 0.22],
        [RokuricsColors.glassStroke, 0.18],
        [RokuricsColors.glassStrokeAccent, 0.24]
      ],
      direction: GradientDirection.RightBottom
    },
    radius: 20
  } as BorderOptions)
}

// ── Shared page background gradient ──
// Replaces the duplicated .linearGradient() at the end of every page's build().
// Pages that import this can call: PageBackground() as the outermost element.
@Builder
export function PageBackground() {
  Column()
    .width('100%')
    .height('100%')
    .linearGradient({
      direction: GradientDirection.RightBottom,
      colors: pageGradientColors()
    })
}

// ── Glass circle (mirrors rokuricsGlassCircle dark mode) ──
@Builder
export function GlassCircle(size: number, child: WrappedBuilder<[]>): void {
  Stack() {
    child.builder()
  }
  .width(size)
  .height(size)
  .borderRadius(size / 2)
  .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'))
  .shadow({
    color: colorAlpha(RokuricsColors.shadowColor, '12'),
    radius: 14,
    offsetY: 7
  })
  .border({
    width: 1,
    color: {
      colors: [
        [0xFFFFFF, 0.22],
        [RokuricsColors.glassStroke, 0.14],
        [RokuricsColors.aqua, 0.24]
      ],
      direction: GradientDirection.RightBottom
    },
    radius: size / 2
  } as BorderOptions)
}

// ── Status pill (mirrors RokuricsStatusPill) ──
@Builder
export function StatusPill(text: string, color: string): void {
  Row({ space: 6 }) {
    Circle()
      .width(8)
      .height(8)
      .fill(color)
    Text(text)
      .fontSize(11)
      .fontWeight(FontWeight.Medium)
      .fontColor(color)
      .maxLines(1)
  }
  .padding({ left: 10, right: 10, top: 5, bottom: 5 })
  .borderRadius(12)
  .backgroundColor(colorAlpha(color, '18'))
  .border({ width: 1, color: colorAlpha(color, '30'), radius: 12 })
}

// ── Icon circle button (mirrors RokuricsIconCircleButton dark mode) ──
@Builder
export function IconCircleButton(symbol: string, size: number, tint: string, onClick: () => void): void {
  Button() {
    Text(symbol)
      .fontSize(size * 0.42)
      .fontWeight(FontWeight.SemiBold)
      .fontColor(tint)
  }
  .width(size)
  .height(size)
  .borderRadius(size / 2)
  .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'))
  .shadow({
    color: colorAlpha(RokuricsColors.shadowColor, '10'),
    radius: 12,
    offsetY: 6
  })
  .border({
    width: 1,
    color: {
      colors: [
        [0xFFFFFF, 0.22],
        [RokuricsColors.glassStroke, 0.14],
        [RokuricsColors.aqua, 0.24]
      ],
      direction: GradientDirection.RightBottom
    },
    radius: size / 2
  } as BorderOptions)
  .onClick(onClick)
}

// ── Waveform icon (mirrors recording leading icon) ──
@Builder
export function WaveformIcon(size: number): void {
  Stack() {
    Row({ space: size * 0.1 }) {
      Rect()
        .width(size * 0.12)
        .height(size * 0.18)
        .radius(size * 0.06)
        .fill(RokuricsColors.aqua)
      Rect()
        .width(size * 0.12)
        .height(size * 0.40)
        .radius(size * 0.06)
        .fill(RokuricsColors.aqua)
      Rect()
        .width(size * 0.12)
        .height(size * 0.28)
        .radius(size * 0.06)
        .fill(RokuricsColors.aqua)
      Rect()
        .width(size * 0.12)
        .height(size * 0.56)
        .radius(size * 0.06)
        .fill(RokuricsColors.aqua)
      Rect()
        .width(size * 0.12)
        .height(size * 0.34)
        .radius(size * 0.06)
        .fill(RokuricsColors.aqua)
    }
    .justifyContent(FlexAlign.Center)
    .alignItems(VerticalAlign.Center)
  }
  .width(size)
  .height(size)
  .borderRadius(size / 2)
  .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '4A'))
  .border({
    width: 1,
    color: {
      colors: [
        [0xFFFFFF, 0.20],
        [RokuricsColors.glassStroke, 0.12],
        [RokuricsColors.aqua, 0.20]
      ],
      direction: GradientDirection.RightBottom
    },
    radius: size / 2
  } as BorderOptions)
  .shadow({
    color: colorAlpha(RokuricsColors.shadowColor, '08'),
    radius: 9,
    offsetY: 4
  })
}

// ── Segment button (for provider kind toggle, etc.) ──
@Builder
export function SegmentedButton(
  labels: string[],
  selectedIndex: number,
  onSelect: (index: number) => void
): void {
  Row({ space: 0 }) {
    ForEach(labels, (label: string, index: number) => {
      Button() {
        Text(label)
          .fontSize(13)
          .fontColor(index === selectedIndex ? Color.White : RokuricsColors.softText)
      }
      .padding({ left: 14, right: 14, top: 7, bottom: 7 })
      .backgroundColor(index === selectedIndex ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'))
      .borderRadius(index === 0 ?
        { topLeft: 8, bottomLeft: 8 } :
        index === labels.length - 1 ?
        { topRight: 8, bottomRight: 8 } : 0)
      .onClick(() => onSelect(index))
    })
  }
}

// ── Adaptive layout utilities (mirrors RokuricsAdaptiveLayout.swift) ──

export enum WidthCategory {
  COMPACT = 'compact',
  REGULAR_PAD = 'regularPad',
  WIDE_PAD = 'widePad'
}

export function classifyWidth(width: number): WidthCategory {
  if (width < 600) return WidthCategory.COMPACT
  if (width < 900) return WidthCategory.REGULAR_PAD
  return WidthCategory.WIDE_PAD
}

export function isPadWidth(width: number): boolean {
  return classifyWidth(width) !== WidthCategory.COMPACT
}

export function horizontalPaddingForWidth(width: number): number {
  const cat = classifyWidth(width)
  if (cat === WidthCategory.COMPACT) return width < 360 ? 20 : 24
  if (cat === WidthCategory.REGULAR_PAD) return 32
  return 40
}

export function homeMaxWidthForWidth(width: number): number {
  const cat = classifyWidth(width)
  if (cat === WidthCategory.COMPACT) return width
  if (cat === WidthCategory.REGULAR_PAD) return 680
  return 760
}
