/**
 * ReinforcementPage.ets - Shows all knowledge points marked as "important" (reinforced).
 * Uses LiquidGlass card styling.
 *
 * Translated from ReinforcementView in source/Kikaria/ContentView.swift (line 9655)
 */
import { appState } from '../data/AppState'
import { ReviewMode, KnowledgePoint } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding, ipadPortraitListPageTopInset } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

@Builder
export function ReinforcementPageBuilder(name: string, param: Object) {
  ReinforcementPageContent()
}

@Component
export struct ReinforcementPageContent {
  @State reinforcedPoints: KnowledgePoint[] = []
  @State expandedId: string = ''
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.refreshState()
  }

  onPageShow(): void {
    this.refreshState()
  }

  refreshState(): void {
    this.reinforcedPoints = [...appState.reinforcedPoints]
  }

  build() {
    NavDestination() {
      Column() {
        Row() {
          Button() {
            Text('← 返回')
              .fontSize(17)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => { navPathStack.pop() })

          Blank()

          Text(`重点集锦 · ${this.reinforcedPoints.length}`)
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.DEEP_TEXT)
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12 + ipadPortraitListPageTopInset(), bottom: 12 })

        if (this.reinforcedPoints.length === 0) {
          Column({ space: 16 }) {
            Text('暂无重点项目')
              .fontSize(20)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)

            Text('在复习过程中点击"加入重点"，即可将重要知识点加入此处集中复习。')
              .fontSize(15)
              .fontColor(KikariaColors.SOFT_TEXT)
              .textAlign(TextAlign.Center)
              .padding({ left: 40, right: 40 })

            Button('返回首页')
              .fontSize(17)
              .fontWeight(FontWeight.Medium)
              .fontColor('#FFFFFF')
              .linearGradient({
                angle: 135,
                colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
              })
              .borderRadius(16)
              .height(50)
              .padding({ left: 32, right: 32 })
              .onClick(() => { navPathStack.pop() })
          }
          .width('100%')
          .height('100%')
          .justifyContent(FlexAlign.Center)
          .padding(24)
        } else {
          Column() {
            Row() {
              Button(`复习全部 (${this.reinforcedPoints.length})`)
                .fontSize(15)
                .fontWeight(FontWeight.Medium)
                .fontColor('#FFFFFF')
                .linearGradient({
                  angle: 135,
                  colors: [[KikariaColors.NEXT_GRADIENT_START, 0], [KikariaColors.NEXT_GRADIENT_END, 1]]
                })
                .borderRadius(16)
                .height(44)
                .width('100%')
                .shadow({ radius: 10, color: KikariaColors.SHADOW_COLOR, offsetY: 4 })
                .onClick(() => {
                  appState.startReview(ReviewMode.REINFORCEMENT)
                  navPathStack.pushPathByName('ReviewPage', undefined)
                })
            }
            .width('100%')
            .padding({ left: 24, right: 24, top: 12, bottom: 8 })

            List({ space: 10 }) {
              ForEach(this.reinforcedPoints, (point: KnowledgePoint) => {
                ListItem() {
                  this.buildPointCard(point)
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
          .layoutWeight(1)
        }
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG)
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }

  @Builder
  buildPointCard(point: KnowledgePoint) {
    Column({ space: 8 }) {
      Row() {
        Column({ space: 4 }) {
          Row({ space: 8 }) {
            Text('★')
              .fontSize(16)
              .fontColor(KikariaColors.NEXT_AMBER)

            Text(point.title)
              .fontSize(17)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)
          }

          if (point.tags.length > 0) {
            Text(point.tags.join(', '))
              .fontSize(12)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
        }
        .layoutWeight(1)
        .alignItems(HorizontalAlign.Start)

        Button() {
          Text(this.expandedId === point.id ? '▲' : '▼')
            .fontSize(16)
            .fontColor(KikariaColors.SOFT_TEXT)
        }
        .backgroundColor(Color.Transparent)
        .width(32)
        .height(32)
        .onClick(() => {
          this.expandedId = this.expandedId === point.id ? '' : point.id
        })
      }
      .width('100%')

      if (this.expandedId === point.id) {
        Column({ space: 8 }) {
          if (point.hint.length > 0) {
            Text(point.hint)
              .fontSize(14)
              .fontColor(KikariaColors.SOFT_TEXT)
              .width('100%')
              .textAlign(TextAlign.Start)
          }

          Text(point.content)
            .fontSize(14)
            .fontColor(KikariaColors.DEEP_TEXT)
            .width('100%')
            .textAlign(TextAlign.Start)

          Button('移出重点')
            .fontSize(14)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.REMOVE_CORAL)
            .backgroundColor(`${KikariaColors.REMOVE_CORAL}10`)
            .borderRadius(12)
            .height(38)
            .width('100%')
            .border({ width: 1, color: `${KikariaColors.REMOVE_CORAL}33` })
            .onClick(() => {
              appState.togglePointReinforcement(point.id)
              this.refreshState()
              this.expandedId = ''
            })
        }
        .width('100%')
        .padding({ top: 8 })
      }
    }
    .width('100%')
    .padding(16)
    .borderRadius(18)
    .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
    .backdropBlur(12)
    .shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 })
  }
}

@Entry
@Component
struct ReinforcementPage {
  build() {
    Navigation() {
      ReinforcementPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
