/**
 * PresetSelectionPage.ets - Browse, switch, create, edit, and delete presets.
 * Uses LiquidGlass card styling matching iOS PresetCard + PresetSelectionView.
 *
 * Translated from PresetSelectionView in source/Kikaria/ContentView.swift (line 4813)
 */
import { appState } from '../data/AppState'
import { KnowledgePreset } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding, ipadPortraitListPageTopInset } from '../components/KikariaAdaptiveLayout'
import { navPathStack, RouteParams } from '../data/NavigationService'

@Builder
export function PresetSelectionPageBuilder(name: string, param: Object) {
  PresetSelectionPageContent()
}

@Component
export struct PresetSelectionPageContent {
  @State presets: KnowledgePreset[] = []
  @State currentPresetID: string = ''
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.refreshState()
  }

  onPageShow(): void {
    this.refreshState()
  }

  refreshState(): void {
    this.presets = [...appState.presets]
    this.currentPresetID = appState.currentPresetID
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

          Text('预设管理')
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.DEEP_TEXT)

          Blank()

          Button() {
            Text('+ 新建')
              .fontSize(15)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.SKY)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => {
            navPathStack.pushPathByName('NewPresetPage', undefined)
          })
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12 + ipadPortraitListPageTopInset(), bottom: 12 })

        List({ space: 12 }) {
          ForEach(this.presets, (preset: KnowledgePreset) => {
            ListItem() {
              Column({ space: 6 }) {
                Row({ space: 10 }) {
                  Column({ space: 4 }) {
                    Row({ space: 8 }) {
                      if (this.currentPresetID === preset.id) {
                        Text('●')
                          .fontSize(10)
                          .fontColor(KikariaColors.MASTERED_GREEN)
                      }

                      Text(preset.name)
                        .fontSize(17)
                        .fontWeight(this.currentPresetID === preset.id ? FontWeight.Bold : FontWeight.Medium)
                        .fontColor(KikariaColors.DEEP_TEXT)
                    }

                    Text(preset.description)
                      .fontSize(13)
                      .fontColor(KikariaColors.SOFT_TEXT)
                      .maxLines(2)
                      .textOverflow({ overflow: TextOverflow.Ellipsis })
                      .width('100%')
                  }
                  .layoutWeight(1)
                  .alignItems(HorizontalAlign.Start)

                  Text(`${preset.knowledgePointCount} 条`)
                    .fontSize(12)
                    .fontColor(KikariaColors.TERTIARY_TEXT)
                }
                .width('100%')

                Row({ space: 8 }) {
                  if (this.currentPresetID !== preset.id) {
                    Button('切换至此预设')
                      .fontSize(14)
                      .fontWeight(FontWeight.Medium)
                      .fontColor('#FFFFFF')
                      .linearGradient({
                        angle: 135,
                        colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
                      })
                      .borderRadius(12)
                      .height(36)
                      .padding({ left: 16, right: 16 })
                      .onClick(() => {
                        const success = appState.switchToPreset(preset)
                        if (success) {
                          this.currentPresetID = preset.id
                          this.refreshState()
                        }
                      })
                  } else {
                    Text('当前')
                      .fontSize(14)
                      .fontColor(KikariaColors.MASTERED_GREEN)
                  }

                  Blank()

                  Button('编辑')
                    .fontSize(13)
                    .fontWeight(FontWeight.Medium)
                    .fontColor(KikariaColors.SKY)
                    .backgroundColor(`${KikariaColors.SKY}10`)
                    .borderRadius(10)
                    .height(32)
                    .padding({ left: 12, right: 12 })
                    .onClick(() => {
                      if (this.currentPresetID !== preset.id) {
                        const success = appState.switchToPreset(preset)
                        if (success) {
                          this.currentPresetID = preset.id
                        }
                      }
                      navPathStack.pushPathByName('EditPresetPage', new RouteParams(preset.id))
                    })

                  if (!preset.isBuiltIn && appState.presets.length > 1) {
                    Button('删除')
                      .fontSize(13)
                      .fontWeight(FontWeight.Medium)
                      .fontColor(KikariaColors.REMOVE_CORAL)
                      .backgroundColor(`${KikariaColors.REMOVE_CORAL}10`)
                      .borderRadius(10)
                      .height(32)
                      .padding({ left: 12, right: 12 })
                      .onClick(() => {
                        appState.deletePreset(preset.id)
                        this.refreshState()
                      })
                  }
                }
                .width('100%')
                .padding({ top: 8 })
              }
              .width('100%')
              .padding(16)
              .borderRadius(18)
              .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
              .backdropBlur(14)
              .shadow({ radius: 10, color: KikariaColors.SHADOW_COLOR, offsetY: 3 })
            }
          })
        }
        .width('100%')
        .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
        .layoutWeight(1)
        .padding({ left: this.contentPadH, right: this.contentPadH })
        .scrollBar(BarState.Off)
        .divider({ strokeWidth: 0 })
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
struct PresetSelectionPage {
  build() {
    Navigation() {
      PresetSelectionPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
