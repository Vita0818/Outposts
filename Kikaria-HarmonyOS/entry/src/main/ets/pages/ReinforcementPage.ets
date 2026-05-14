/**
 * ReinforcementPage.ets - Shows all knowledge points marked as "important" (reinforced).
 * Allows browsing, removing from the list, and starting a focused review.
 *
 * Translated from the reinforcement screen in source/Kikaria/ContentView.swift
 */

import { router } from '@kit.ArkUI'
import { appState } from '../data/AppState'
import { ReviewMode, KnowledgePoint } from '../model/KnowledgePoint'
import { KikariaCard, KikariaButton } from '../components/KikariaComponents'
import { KikariaColors } from '../components/KikariaTheme'

@Entry
@Component
struct ReinforcementPage {
  @State reinforcedPoints: KnowledgePoint[] = []
  @State expandedId: string = ''

  aboutToAppear(): void {
    this.refreshState()
  }

  onPageShow(): void {
    this.refreshState()
  }

  refreshState(): void {
    this.reinforcedPoints = [...appState.reinforcedPoints]
  }

  build() {
    Navigation() {
      Column() {
        // Top bar
        Row() {
          Button() {
            Text('← Back')
              .fontSize(17)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => {
            router.back()
          })

          Blank()

          Text(`Important (${this.reinforcedPoints.length})`)
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.DEEP_TEXT)
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12, bottom: 12 })

        if (this.reinforcedPoints.length === 0) {
          Column({ space: 16 }) {
            Text('No Important Items')
              .fontSize(20)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)

            Text('Add items to your important list during review by tapping "Add to Important".')
              .fontSize(15)
              .fontColor(KikariaColors.SOFT_TEXT)
              .textAlign(TextAlign.Center)
              .padding({ left: 40, right: 40 })

            KikariaButton({
              label: 'Start Review',
              buttonType: 'primary',
              onClick: () => {
                router.back()
              }
            })
          }
          .width('100%')
          .height('100%')
          .justifyContent(FlexAlign.Center)
          .padding(24)
        } else {
          Column({ space: 4 }) {
            // Review all button
            Row() {
              KikariaButton({
                label: `Review All (${this.reinforcedPoints.length})`,
                buttonType: 'primary',
                onClick: () => {
                  appState.startReview(ReviewMode.REINFORCEMENT)
                  router.pushUrl({ url: 'pages/ReviewPage' })
                }
              })
            }
            .width('100%')
            .padding({ left: 24, right: 24, top: 12, bottom: 8 })

            // List of reinforced points
            List({ space: 10 }) {
              ForEach(this.reinforcedPoints, (point: KnowledgePoint) => {
                ListItem() {
                  this.buildPointCard(point)
                }
              })
            }
            .width('100%')
            .layoutWeight(1)
            .padding({ left: 24, right: 24 })
            .scrollBar(BarState.Off)
            .divider({ strokeWidth: 0 })
          }
          .width('100%')
          .layoutWeight(1)
        }
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG_LIGHT)
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG_LIGHT)
    .hideTitleBar(true)
    .hideBackButton(true)
  }

  @Builder
  buildPointCard(point: KnowledgePoint) {
    KikariaCard({ cornerRadius: 16 }) {
      Column({ space: 8 }) {
        Row() {
          Column({ space: 4 }) {
            Text(point.title)
              .fontSize(17)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)

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
            if (this.expandedId === point.id) {
              this.expandedId = ''
            } else {
              this.expandedId = point.id
            }
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

            KikariaButton({
              label: 'Remove from Important',
              buttonType: 'danger',
              onClick: () => {
                const idx = appState.knowledgePoints.findIndex(kp => kp.id === point.id)
                if (idx >= 0) {
                  appState.knowledgePoints[idx].clearReinforcement()
                }
                this.refreshState()
              }
            })
          }
          .width('100%')
          .padding({ top: 8 })
        }
      }
      .width('100%')
      .alignItems(HorizontalAlign.Start)
    }
  }
}
