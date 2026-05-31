/**
 * ScopeSelectionPage.ets - Tag-based scope selection screen.
 * Uses LiquidGlass card style for consistency.
 *
 * Translated from ScopeSelectionView in source/Kikaria/ContentView.swift (line 7153)
 */
import { appState } from '../data/AppState'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding, ipadPortraitListPageTopInset } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

@Builder
export function ScopeSelectionPageBuilder(name: string, param: Object) {
  ScopeSelectionPageContent()
}

@Component
export struct ScopeSelectionPageContent {
  @State allTags: string[] = []
  @State selectedTags: Set<string> = new Set()
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.allTags = appState.allTags
    this.selectedTags = new Set(appState.selectedTags)
  }

  aboutToDisappear(): void {
    appState.selectedTags = new Set(this.selectedTags)
    appState.saveAppState()
  }

  build() {
    NavDestination() {
      Column() {
        // Top bar
        Row() {
          Button() {
            Text('← 返回')
              .fontSize(17)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => { navPathStack.pop() })

          Blank()

          Text('范围')
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.DEEP_TEXT)

          Blank()

          Text(`${this.selectedTags.size}/${this.allTags.length}`)
            .fontSize(14)
            .fontColor(KikariaColors.SKY)
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12 + ipadPortraitListPageTopInset(), bottom: 12 })

        Scroll() {
          Column({ space: 12 }) {
            Row({ space: 12 }) {
              Button('全选')
                .fontSize(14)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.SKY)
                .backgroundColor(KikariaColors.MIST)
                .borderRadius(12)
                .height(36)
                .padding({ left: 16, right: 16 })
                .onClick(() => {
                  this.selectedTags = new Set([...this.allTags])
                })

              Button('取消全选')
                .fontSize(14)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.SOFT_TEXT)
                .backgroundColor(KikariaColors.MIST)
                .borderRadius(12)
                .height(36)
                .padding({ left: 16, right: 16 })
                .onClick(() => {
                  this.selectedTags = new Set()
                })
            }
            .width('100%')
            .padding({ top: 8 })

            Flex({ wrap: FlexWrap.Wrap, justifyContent: FlexAlign.Start }) {
              ForEach(this.allTags, (tag: string) => {
                Text(tag)
                  .fontSize(14)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(this.selectedTags.has(tag) ? '#FFFFFF' : KikariaColors.SOFT_TEXT)
                  .backgroundColor(this.selectedTags.has(tag) ? KikariaColors.SKY : KikariaColors.MIST)
                  .borderRadius(16)
                  .padding({ left: 16, right: 16, top: 8, bottom: 8 })
                  .margin({ right: 10, bottom: 10 })
                  .onClick(() => {
                    if (this.selectedTags.has(tag)) {
                      this.selectedTags.delete(tag)
                    } else {
                      this.selectedTags.add(tag)
                    }
                  })
              })
            }
            .width('100%')
          }
          .width('100%')
          .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
          .padding({ left: this.contentPadH, right: this.contentPadH })
        }
        .width('100%')
        .layoutWeight(1)
        .scrollBar(BarState.Off)

        Row() {
          Button('应用并开始复习')
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor('#FFFFFF')
            .linearGradient({
              angle: 135,
              colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
            })
            .borderRadius(16)
            .height(50)
            .width('100%')
            .shadow({ radius: 12, color: KikariaColors.SHADOW_COLOR, offsetY: 6 })
            .onClick(() => {
              appState.selectedTags = new Set(this.selectedTags)
              appState.saveAppState()
              navPathStack.pop()
            })
        }
        .width('100%')
        .padding({ left: 24, right: 24, top: 12, bottom: 24 })
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG)
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }
}

@Entry
@Component
struct ScopeSelectionPage {
  build() {
    Navigation() {
      ScopeSelectionPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
