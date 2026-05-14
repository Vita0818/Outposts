/**
 * KikariaCard - A soft, rounded card component inspired by the LiquidGlassCard
 * from source/Kikaria/ContentView.swift. Provides a clean, study-focused feel.
 */

@Component
export struct KikariaCard {
  @Prop cornerRadius: number = 20
  @BuilderParam content: () => void

  build() {
    Column() {
      this.content()
    }
    .width('100%')
    .borderRadius(this.cornerRadius)
    .backgroundColor('#FFFFFF')
    .shadow({
      radius: 12,
      color: '#1A63BAF5',
      offsetY: 4
    })
    .padding(20)
  }
}

/**
 * KikariaButton - A styled button matching Kikaria's soft design.
 * Supports primary (sky/cyan gradient feel) and secondary styles.
 */
@Component
export struct KikariaButton {
  @Prop label: string = ''
  @Prop buttonType: string = 'primary' // 'primary' | 'secondary' | 'danger'
  @Prop fontSize: number = 17
  @Prop enabled: boolean = true
  onClick: () => void = () => {}

  build() {
    Button(this.label)
      .fontSize(this.fontSize)
      .fontWeight(FontWeight.Medium)
      .fontColor(this.getTextColor())
      .backgroundColor(this.getBgColor())
      .borderRadius(16)
      .height(50)
      .width('100%')
      .enabled(this.enabled)
      .onClick(() => {
        if (this.enabled) {
          this.onClick()
        }
      })
  }

  getBgColor(): string {
    switch (this.buttonType) {
      case 'primary':
        return '#5CB8E0'
      case 'secondary':
        return '#E8F7FC'
      case 'danger':
        return '#DB524D'
      default:
        return '#5CB8E0'
    }
  }

  getTextColor(): string {
    switch (this.buttonType) {
      case 'primary':
        return '#FFFFFF'
      case 'secondary':
        return '#214054'
      case 'danger':
        return '#FFFFFF'
      default:
        return '#FFFFFF'
    }
  }
}

/**
 * TagChip - A small tag pill for displaying knowledge point tags.
 */
@Component
export struct TagChip {
  @Prop label: string = ''
  @Prop isSelected: boolean = false
  onTap: () => void = () => {}

  build() {
    Text(this.label)
      .fontSize(12)
      .fontWeight(FontWeight.Medium)
      .fontColor(this.isSelected ? '#FFFFFF' : '#6B8A9E')
      .backgroundColor(this.isSelected ? '#5CB8E0' : '#EDF9FF')
      .borderRadius(12)
      .padding({ left: 12, right: 12, top: 6, bottom: 6 })
      .onClick(() => this.onTap())
  }
}
