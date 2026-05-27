/**
 * KikariaComponents - Reusable UI components with Liquid Glass morphism.
 * Mirrors the iOS LiquidGlassCard/Capsule/Circle modifiers,
 * SettingsSectionCard, and other shared views from ContentView.swift.
 */
import { KikariaColors, isDarkModeEnabled } from './KikariaTheme'

/* ── Typed gradient descriptor (ArkTS strict mode) ── */
export class KikariaGradient {
  angle: number
  colors: [string, number][]

  constructor(startColor: string, endColor: string, angle: number = 135) {
    this.angle = angle
    this.colors = [[startColor, 0], [endColor, 1]]
  }
}

/* ── Glass style helpers ── */
function glassFillOpacity(): number {
  return isDarkModeEnabled() ? 0.13 : 0.48
}
function glassStrokeColor(): string {
  return isDarkModeEnabled() ? '#FFFFFF1A' : '#FFFFFF6B'
}
function glassAccentColor(): string {
  return isDarkModeEnabled() ? '#6BD6EF38' : '#91E0E821'
}
function glassShadowOpacity(): number {
  return isDarkModeEnabled() ? 0.08 : 0.12
}

/* ── LiquidGlassCard ── */
@Component
export struct LiquidGlassCard {
  @Prop cornerRadius: number = 25
  @Prop paddingSize: number = 20
  @BuilderParam content: () => void

  build() {
    Column() {
      this.content()
    }
    .width('100%')
    .borderRadius(this.cornerRadius)
    .backgroundColor(KikariaColors.GLASS_SURFACE + (isDarkModeEnabled() ? '33' : '7A'))
    .backdropBlur(24)
    .shadow({
      radius: 17,
      color: KikariaColors.SHADOW_COLOR,
      offsetY: 9
    })
    .padding(this.paddingSize)
  }
}

/* ── LiquidGlassCapsule ── */
@Component
export struct LiquidGlassCapsule {
  @Prop paddingH: number = 24
  @Prop paddingV: number = 16
  @BuilderParam content: () => void

  build() {
    Row() {
      this.content()
    }
    .borderRadius(999)
    .backgroundColor(KikariaColors.GLASS_SURFACE + (isDarkModeEnabled() ? '33' : '7A'))
    .backdropBlur(20)
    .shadow({
      radius: 14,
      color: KikariaColors.SHADOW_COLOR,
      offsetY: 7
    })
    .padding({ left: this.paddingH, right: this.paddingH, top: this.paddingV, bottom: this.paddingV })
  }
}

/* ── LiquidGlassCircle ── */
@Component
export struct LiquidGlassCircle {
  @Prop circleSize: number = 200
  @BuilderParam content: () => void

  build() {
    Stack() {
      this.content()
    }
    .width(this.circleSize)
    .height(this.circleSize)
    .borderRadius(this.circleSize / 2)
    .backgroundColor(KikariaColors.GLASS_SURFACE + (isDarkModeEnabled() ? '33' : '7A'))
    .backdropBlur(20)
    .shadow({
      radius: 14,
      color: KikariaColors.SHADOW_COLOR,
      offsetY: 7
    })
    .alignContent(Alignment.Center)
  }
}

/* ── KikariaCard (kept for simpler use-cases) ── */
@Component
export struct KikariaCard {
  @Prop cornerRadius: number = 20
  @BuilderParam content: () => void

  build() {
    LiquidGlassCard({ cornerRadius: this.cornerRadius }) {
      this.content()
    }
  }
}

/* ── KikariaGlassCard (alias, kept for backward compatibility) ── */
@Component
export struct KikariaGlassCard {
  @Prop cornerRadius: number = 25
  @Prop paddingSize: number = 20
  @BuilderParam content: () => void

  build() {
    LiquidGlassCard({ cornerRadius: this.cornerRadius, paddingSize: this.paddingSize }) {
      this.content()
    }
  }
}

/* ── SettingsSectionCard - matching iOS SettingsSectionCard ── */
@Component
export struct SettingsSectionCard {
  @BuilderParam content: () => void

  build() {
    Column() {
      this.content()
    }
    .width('100%')
    .borderRadius(22)
    .backgroundColor(KikariaColors.CARD_BG)
    .shadow({
      radius: 8,
      color: KikariaColors.SHADOW_COLOR,
      offsetY: 2
    })
    .padding(0)
  }
}

/* ── SettingsSectionDivider - matching iOS SettingsSectionDivider ── */
@Component
export struct SettingsSectionDivider {
  build() {
    Row()
      .width('100%')
      .height(0.5)
      .backgroundColor(KikariaColors.BLUE_GRAY_LIGHT + '1E')
      .margin({ left: 16, right: 16 })
  }
}

/* ── SettingsListRow - tappable row with label, value, and chevron ── */
@Component
export struct SettingsListRow {
  @Prop label: string = ''
  @Prop value: string = ''
  @Prop showArrow: boolean = true
  @Prop labelColor: string = KikariaColors.DEEP_TEXT
  @Prop valueColor: string = KikariaColors.SKY
  onTap: () => void = () => {}

  build() {
    Row() {
      Text(this.label)
        .fontSize(15)
        .fontColor(this.labelColor)

      Blank()

      if (this.value.length > 0) {
        Text(this.value)
          .fontSize(15)
          .fontWeight(FontWeight.Medium)
          .fontColor(this.valueColor)
          .padding({ right: 4 })
      }

      if (this.showArrow) {
        Text('›')
          .fontSize(16)
          .fontColor(KikariaColors.TERTIARY_TEXT)
      }
    }
    .width('100%')
    .padding({ left: 16, right: 16, top: 14, bottom: 14 })
    .onClick(() => this.onTap())
  }
}

/* ── SettingsToggleRow - row with label and toggle switch ── */
@Component
export struct SettingsToggleRow {
  @Prop label: string = ''
  @Link isOn: boolean
  onChange: (value: boolean) => void = (_v: boolean) => {}

  build() {
    Row() {
      Text(this.label)
        .fontSize(15)
        .fontColor(KikariaColors.DEEP_TEXT)

      Blank()

      Toggle({ type: ToggleType.Switch, isOn: this.isOn })
        .onChange((value: boolean) => {
          this.onChange(value)
        })
    }
    .width('100%')
    .padding({ left: 16, right: 16, top: 14, bottom: 14 })
  }
}

/* ── SettingsInfoTextRow - secondary info text row ── */
@Component
export struct SettingsInfoTextRow {
  @Prop text: string = ''

  build() {
    Text(this.text)
      .fontSize(12)
      .fontColor(KikariaColors.TERTIARY_TEXT)
      .width('100%')
      .padding({ left: 16, right: 16, top: 8, bottom: 8 })
  }
}

/* ── SettingsStepperRow - row with +/- stepper for numeric values ── */
@Component
export struct SettingsStepperRow {
  @Prop label: string = ''
  @Prop value: number = 20
  @Prop min: number = 1
  @Prop max: number = 200
  @Prop valueColor: string = KikariaColors.DEEP_TEXT
  onDecrement: () => void = () => {}
  onIncrement: () => void = () => {}

  build() {
    Row() {
      Text(this.label)
        .fontSize(15)
        .fontColor(KikariaColors.DEEP_TEXT)

      Blank()

      Row({ space: 8 }) {
        Button('-')
          .fontSize(20)
          .fontWeight(FontWeight.Bold)
          .fontColor(KikariaColors.SKY)
          .width(36).height(36)
          .backgroundColor(KikariaColors.MIST)
          .borderRadius(18)
          .onClick(() => this.onDecrement())

        Text(`${this.value}`)
          .fontSize(18)
          .fontWeight(FontWeight.Bold)
          .fontColor(this.valueColor)
          .width(40)
          .textAlign(TextAlign.Center)

        Button('+')
          .fontSize(20)
          .fontWeight(FontWeight.Bold)
          .fontColor(KikariaColors.SKY)
          .width(36).height(36)
          .backgroundColor(KikariaColors.MIST)
          .borderRadius(18)
          .onClick(() => this.onIncrement())
      }
    }
    .width('100%')
    .padding({ left: 16, right: 16, top: 14, bottom: 14 })
  }
}

/* ── KikariaButton ── */
@Component
export struct KikariaButton {
  @Prop label: string = ''
  @Prop buttonType: string = 'primary'
  @Prop fontSize: number = 17
  @Prop isEnabled: boolean = true
  onTap: () => void = () => {}

  build() {
    Button(this.label)
      .fontSize(this.fontSize)
      .fontWeight(FontWeight.Medium)
      .fontColor(this.getTextColor())
      .linearGradient(this.getGradient())
      .borderRadius(16)
      .height(50)
      .width('100%')
      .enabled(this.isEnabled)
      .opacity(this.isEnabled ? 1.0 : 0.5)
      .shadow({
        radius: 12,
        color: KikariaColors.SHADOW_COLOR,
        offsetY: 6
      })
      .onClick(() => {
        if (this.isEnabled) {
          this.onTap()
        }
      })
  }

  getGradient(): KikariaGradient {
    switch (this.buttonType) {
      case 'primary':
        return new KikariaGradient(KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END)
      case 'mastered':
        return new KikariaGradient(KikariaColors.MASTERED_GRADIENT_START, KikariaColors.MASTERED_GRADIENT_END)
      case 'next':
        return new KikariaGradient(KikariaColors.NEXT_GRADIENT_START, KikariaColors.NEXT_GRADIENT_END)
      case 'danger':
        return new KikariaGradient(KikariaColors.REMOVE_GRADIENT_START, KikariaColors.REMOVE_GRADIENT_END)
      case 'secondary':
        return new KikariaGradient(KikariaColors.MIST, KikariaColors.MIST)
      default:
        return new KikariaGradient(KikariaColors.SKY, KikariaColors.CYAN)
    }
  }

  getTextColor(): string {
    switch (this.buttonType) {
      case 'secondary':
      case 'ghost':
        return KikariaColors.DEEP_TEXT
      default:
        return '#FFFFFF'
    }
  }
}

/* ── TagChip ── */
@Component
export struct TagChip {
  @Prop label: string = ''
  @Prop isSelected: boolean = false
  onTap: () => void = () => {}

  build() {
    Text(this.label)
      .fontSize(12)
      .fontWeight(FontWeight.Medium)
      .fontColor(this.isSelected ? '#FFFFFF' : KikariaColors.SOFT_TEXT)
      .backgroundColor(this.isSelected ? KikariaColors.SKY : KikariaColors.MIST)
      .borderRadius(12)
      .padding({ left: 12, right: 12, top: 6, bottom: 6 })
      .onClick(() => this.onTap())
  }
}

/* ── FloatingInfoCard ── */
@Component
export struct FloatingInfoCard {
  @Prop label: string = ''
  @Prop text: string = ''
  @Prop labelColor: string = '#63BAF5'

  build() {
    Column({ space: 8 }) {
      Text(this.label)
        .fontSize(13)
        .fontWeight(FontWeight.Medium)
        .fontColor(this.labelColor)
        .width('100%')

      Text(this.text)
        .fontSize(15)
        .fontColor(KikariaColors.DEEP_TEXT)
        .width('100%')
        .textAlign(TextAlign.Start)
        .lineHeight(24)
    }
    .width('100%')
    .padding(20)
    .borderRadius(20)
    .backgroundColor(KikariaColors.CARD_BG)
    .shadow({
      radius: 12,
      color: KikariaColors.SHADOW_COLOR,
      offsetY: 4
    })
  }
}

/* ── ProfileAvatar ── */
@Component
export struct ProfileAvatar {
  @Prop displayName: string = 'V'
  @Prop avatarSize: number = 56
  @Prop avatarFontSize: number = 28
  @Prop avatarColor: string = '#63BAF5'
  @Prop textColor: string = '#FFFFFF'

  build() {
    Row() {
      Text(this.displayName.charAt(0).toUpperCase())
        .fontSize(this.avatarFontSize)
        .fontWeight(FontWeight.Bold)
        .fontColor(this.textColor)
    }
    .width(this.avatarSize)
    .height(this.avatarSize)
    .borderRadius(this.avatarSize / 2)
    .backgroundColor(this.avatarColor)
    .justifyContent(FlexAlign.Center)
    .shadow({
      radius: 12,
      color: KikariaColors.SHADOW_COLOR,
      offsetY: 6
    })
  }
}

/* ── DashboardMetricColumn ── */
@Component
export struct DashboardMetricColumn {
  @Prop title: string = ''
  @Prop valueText: string = ''
  @Prop tintColor: string = '#63BAF5'
  @Prop isExpanded: boolean = false

  build() {
    Column({ space: 6 }) {
      Text(this.valueText)
        .fontSize(this.isExpanded ? 28 : 26)
        .fontWeight(FontWeight.Bold)
        .fontColor(this.tintColor)
        .fontFamily('serif')

      Text(this.title)
        .fontSize(13)
        .fontColor(KikariaColors.SOFT_TEXT)
    }
    .layoutWeight(1)
    .padding(16)
    .alignItems(HorizontalAlign.Center)
  }
}

/* ── AdaptiveBackButton ── */
@Component
export struct AdaptiveBackButton {
  @Prop label: string = '← 返回'
  onTap: () => void = () => {}

  build() {
    Button() {
      Text(this.label)
        .fontSize(17)
        .fontColor(KikariaColors.SOFT_TEXT)
    }
    .backgroundColor(Color.Transparent)
    .onClick(() => this.onTap())
  }
}
