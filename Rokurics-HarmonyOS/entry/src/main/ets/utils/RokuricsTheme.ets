/**
 * Theme system - mirrors RokuricsColors.swift + RokuricsTypography.swift + glass modifiers
 */

// ── Color utility: converts #RRGGBB base + AA alpha suffix to HarmonyOS #AARRGGBB ──
export function colorAlpha(baseHex: string, alphaHex: string): string {
  return '#' + alphaHex + baseHex.substring(1)
}

// ── Colors (dark mode — mirrors RokuricsColors.swift adaptive dark values) ──
export class RokuricsColors {
  static readonly aqua = '#57D6D1'
  static readonly mint = '#52BD94'
  static readonly mistGreen = '#0F2B26'
  static readonly softTeal = '#85CCCC'
  static readonly skyCyan = '#4DB3EB'
  static readonly paleAqua = '#266B61'
  static readonly coral = '#F5757A'

  static readonly deepText = '#E6FAF7'
  static readonly softText = '#A8D1D1'
  static readonly tertiaryText = '#759EA1'

  static readonly glassSurface = '#0D2424'
  static readonly glassStroke = '#8ADBD1'
  static readonly glassStrokeAccent = '#61D4C2'
  static readonly shadowColor = '#000808'

  static readonly pageBackground = '#051414'

  static readonly actionStart = '#128080'
  static readonly actionEnd = '#2BAB82'

  static readonly recordingAccentStart = '#D14257'
  static readonly recordingAccentEnd = '#E0706B'

  // ── Page gradient color stops (mirrors pageGradient dark in RokuricsColors.swift) ──
  static readonly pageGradientStart = '#051414'
  static readonly pageGradientMid = '#0A2B29'
  static readonly pageGradientEnd = '#030D12'

  // ── Action/orb gradient stops (mirrors actionGradient dark in RokuricsColors.swift) ──
  static readonly actionGradientStart = '#128080'
  static readonly actionGradientEnd = '#2BAB82'
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
// Callers: use the exported constants directly; they already include dark-mode scaling.

let DARK_MODE_SCALE = 0.78

// Utility to adapt a hex alpha (last 2 chars) for dark mode
export function darkModeGlassOpacity(lightHexAlpha: string, isDark: boolean): string {
  if (!isDark) return lightHexAlpha
  const alpha = parseInt(lightHexAlpha, 16)
  const scaled = Math.max(4, Math.floor(alpha * DARK_MODE_SCALE))
  return scaled.toString(16).padStart(2, '0').toUpperCase()
}

// Pre-computed glass opacity strings (hex alpha for appending to colors).
// Dark mode defaults (scaled from light 0.78× fill, 0.82× stroke, per Apple RokuricsGlassStyle):
export const glassFillOpacity = '5C'      // min(0.66*0.78, 0.36) → ~36% fill
export const glassStrokeHighOpacity = '38' // min(0.27*0.82, 0.34) → ~22% stroke
export const glassStrokeMidOpacity = '0E'  // min(0.07*0.82, 0.34) → ~5% stroke
export const glassAccentOpacity = '0A'     // ~4% accent

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
