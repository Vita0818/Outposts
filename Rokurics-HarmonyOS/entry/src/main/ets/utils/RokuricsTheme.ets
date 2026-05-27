/**
 * Theme system - mirrors RokuricsColors.swift + RokuricsTypography.swift + glass modifiers
 */

// ── Colors ──
export class RokuricsColors {
  static readonly aqua = '#59C7C2'
  static readonly mint = '#9EE8C7'
  static readonly mistGreen = '#E2F9EF'
  static readonly softTeal = '#75B3B5'
  static readonly skyCyan = '#73C7F0'
  static readonly paleAqua = '#C4F5E8'
  static readonly coral = '#E06B6E'

  static readonly deepText = '#1A424A'
  static readonly softText = '#638F94'
  static readonly tertiaryText = '#94B3B8'

  static readonly glassSurface = '#FFFFFF'
  static readonly glassStroke = '#EFFAF8'
  static readonly glassStrokeAccent = '#91E8D6'
  static readonly shadowColor = '#4AB8A8'

  static readonly pageBackground = '#EFFAF8'

  static readonly actionStart = '#4FC2C0'
  static readonly actionEnd = '#99E6C2'

  static readonly recordingAccentStart = '#DB6B6B'
  static readonly recordingAccentEnd = '#F5AD94'
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
// Light mode defaults (matching Apple light mode fill/stroke opacities):
export const glassFillOpacity = 'A8'      // ~66% light → ~51% dark
export const glassStrokeHighOpacity = '44' // ~27% light → ~21% dark
export const glassStrokeMidOpacity = '12'  // ~7% light  → ~5% dark
export const glassAccentOpacity = '08'     // ~3% light  → ~2% dark

// Reusable glass card backing (mirrors rokuricsLiquidGlassCard) ──
@Builder
export function GlassCardContent(child: WrappedBuilder<[]>): void {
  Column() {
    child.builder()
  }
  .borderRadius(20)
  .backgroundColor(RokuricsColors.glassSurface + 'A8')
  .shadow({
    color: RokuricsColors.shadowColor + '18',
    radius: 18,
    offsetY: 10
  })
  .border({
    width: 1,
    color: {
      colors: [
        [0xFFFFFF, 0.44],
        [0xEFFAF8, 0.18],
        [0x91E8D6, 0.14]
      ],
      direction: GradientDirection.RightBottom
    },
    radius: 20
  } as BorderOptions)
}

// ── Glass circle (mirrors rokuricsGlassCircle) ──
@Builder
export function GlassCircle(size: number, child: WrappedBuilder<[]>): void {
  Stack() {
    child.builder()
  }
  .width(size)
  .height(size)
  .borderRadius(size / 2)
  .backgroundColor(RokuricsColors.glassSurface + '66')
  .shadow({
    color: RokuricsColors.shadowColor + '12',
    radius: 14,
    offsetY: 7
  })
  .border({
    width: 1,
    color: {
      colors: [
        [0xFFFFFF, 0.44],
        [0xEFFAF8, 0.14],
        [0x59C7C2, 0.12]
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
  .backgroundColor(color + '18')
  .border({ width: 1, color: color + '30', radius: 12 })
}

// ── Icon circle button (mirrors RokuricsIconCircleButton) ──
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
  .backgroundColor(RokuricsColors.glassSurface + '66')
  .shadow({
    color: RokuricsColors.shadowColor + '10',
    radius: 12,
    offsetY: 6
  })
  .border({
    width: 1,
    color: {
      colors: [
        [0xFFFFFF, 0.44],
        [0xEFFAF8, 0.14],
        [0x59C7C2, 0.12]
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
  .backgroundColor(RokuricsColors.glassSurface + '56')
  .border({
    width: 1,
    color: {
      colors: [
        [0xFFFFFF, 0.38],
        [0xEFFAF8, 0.12],
        [0x59C7C2, 0.10]
      ],
      direction: GradientDirection.RightBottom
    },
    radius: size / 2
  } as BorderOptions)
  .shadow({
    color: RokuricsColors.shadowColor + '08',
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
      .backgroundColor(index === selectedIndex ? RokuricsColors.aqua : RokuricsColors.glassSurface + '50')
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
