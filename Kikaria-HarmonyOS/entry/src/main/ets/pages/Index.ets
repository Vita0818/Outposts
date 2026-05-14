/**
 * Index.ets - Home screen of Kikaria HarmonyOS.
 * Shows the app title, review bubble, stats dashboard, and tag selection.
 *
 * Translated from the home screen in source/Kikaria/ContentView.swift
 */

import { router } from '@kit.ArkUI'
import { appState } from '../data/AppState'
import { ReviewMode } from '../model/KnowledgePoint'
import { KikariaCard, KikariaButton, TagChip } from '../components/KikariaComponents'
import { KikariaColors, KikariaTypography } from '../components/KikariaTheme'

@Entry
@Component
struct Index {
  @State currentPresetName: string = appState.currentPreset.name
  @State masteredCount: number = appState.masteredCount
  @State reinforcedCount: number = appState.reinforcedCount
  @State totalCount: number = appState.totalCount
  @State allTags: string[] = appState.allTags
  @State selectedTags: Set<string> = appState.selectedTags
  @State dailyGoal: number = appState.dailyGoal

  aboutToAppear(): void {
    this.refreshState()
  }

  onPageShow(): void {
    this.refreshState()
  }

  refreshState(): void {
    this.currentPresetName = appState.currentPreset.name
    this.masteredCount = appState.masteredCount
    this.reinforcedCount = appState.reinforcedCount
    this.totalCount = appState.totalCount
    this.allTags = appState.allTags
    this.selectedTags = appState.selectedTags
    this.dailyGoal = appState.dailyGoal
  }

  build() {
    Navigation() {
      Scroll() {
        Column({ space: 20 }) {
          // Header
          Column({ space: 4 }) {
            Text('Kikaria')
              .fontSize(39)
              .fontWeight(FontWeight.Bold)
              .fontColor(KikariaColors.DEEP_TEXT)
              .fontFamily('serif')

            Text('Memorization Assistant')
              .fontSize(12)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
          .width('100%')
          .alignItems(HorizontalAlign.Start)
          .padding({ top: 20 })

          // Start Review Bubble
          Column() {
            Column({ space: 12 }) {
              Text(`${this.masteredCount} / ${this.totalCount}`)
                .fontSize(15)
                .fontColor(KikariaColors.SOFT_TEXT)

              Text('Mastered')
                .fontSize(32)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)
                .fontFamily('serif')

              Row({ space: 8 }) {
                Text(`Daily Goal: ${this.dailyGoal}`)
                  .fontSize(12)
                  .fontColor(KikariaColors.SOFT_TEXT)
              }
            }
            .width('100%')
            .alignItems(HorizontalAlign.Center)
            .justifyContent(FlexAlign.Center)
            .padding(40)
            .borderRadius(80)
            .backgroundColor(KikariaColors.MIST)
            .shadow({
              radius: 20,
              color: KikariaColors.SHADOW_LIGHT + '40',
              offsetY: 8
            })
            .onClick(() => {
              this.startNormalReview()
            })
          }
          .width('100%')

          // Stats Dashboard Cards
          Row({ space: 12 }) {
            // Scope / Tags card
            KikariaCard({ cornerRadius: 20 }) {
              Column({ space: 8 }) {
                Text(`${this.selectedTags.size > 0 ? this.selectedTags.size : this.allTags.length}`)
                  .fontSize(28)
                  .fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .fontFamily('serif')

                Text('Tags')
                  .fontSize(13)
                  .fontColor(KikariaColors.SOFT_TEXT)
              }
              .width('100%')
              .alignItems(HorizontalAlign.Center)
            }
            .layoutWeight(1)
            .onClick(() => {
              // Toggle tag selection scrolls below
            })

            // Reinforced card
            KikariaCard({ cornerRadius: 20 }) {
              Column({ space: 8 }) {
                Text(`${this.reinforcedCount}`)
                  .fontSize(28)
                  .fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.NEXT_AMBER)
                  .fontFamily('serif')

                Text('Important')
                  .fontSize(13)
                  .fontColor(KikariaColors.SOFT_TEXT)
              }
              .width('100%')
              .alignItems(HorizontalAlign.Center)
            }
            .layoutWeight(1)
            .onClick(() => {
              this.startReinforcementReview()
            })

            // Mastered card
            KikariaCard({ cornerRadius: 20 }) {
              Column({ space: 8 }) {
                Text(`${this.masteredCount}`)
                  .fontSize(28)
                  .fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.MASTERED_GREEN)
                  .fontFamily('serif')

                Text('Mastered')
                  .fontSize(13)
                  .fontColor(KikariaColors.SOFT_TEXT)
              }
              .width('100%')
              .alignItems(HorizontalAlign.Center)
            }
            .layoutWeight(1)
            .onClick(() => {
              this.startMasteredReview()
            })
          }
          .width('100%')

          // Preset info
          KikariaCard({ cornerRadius: 20 }) {
            Row({ space: 12 }) {
              Column({ space: 4 }) {
                Text(this.currentPresetName)
                  .fontSize(17)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.DEEP_TEXT)

                Text(appState.currentPreset.description)
                  .fontSize(13)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .maxLines(2)
                  .textOverflow({ overflow: TextOverflow.Ellipsis })
              }
              .layoutWeight(1)
              .alignItems(HorizontalAlign.Start)

              Text('▼')
                .fontSize(12)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
            .width('100%')
          }

          // Tag Selection
          Column({ space: 12 }) {
            Text('Filter by Tag')
              .fontSize(15)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)
              .width('100%')

            Flex({ wrap: FlexWrap.Wrap, justifyContent: FlexAlign.Start }) {
              ForEach(this.allTags, (tag: string) => {
                TagChip({
                  label: tag,
                  isSelected: this.selectedTags.has(tag),
                  onTap: () => {
                    appState.toggleTag(tag)
                    this.selectedTags = appState.selectedTags
                    this.refreshState()
                  }
                })
                  .margin({ right: 8, bottom: 8 })
              })
            }
            .width('100%')
          }
          .width('100%')
          .padding({ top: 4 })

          // Quick buttons
          Column({ space: 12 }) {
            KikariaButton({
              label: this.selectedTags.size > 0 ? 'Start Review (Filtered)' : 'Start Review (All)',
              buttonType: 'primary',
              onClick: () => {
                this.startNormalReview()
              }
            })

            if (this.reinforcedCount > 0) {
              KikariaButton({
                label: `Review Important (${this.reinforcedCount})`,
                buttonType: 'secondary',
                onClick: () => {
                  this.startReinforcementReview()
                }
              })
            }

            if (this.masteredCount > 0) {
              KikariaButton({
                label: `Browse Mastered (${this.masteredCount})`,
                buttonType: 'secondary',
                onClick: () => {
                  this.startMasteredReview()
                }
              })
            }
          }

          // Bottom spacer for safe area
          Row().height(40)
        }
        .width('100%')
        .padding({ left: 24, right: 24 })
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG_LIGHT)
      .scrollBar(BarState.Off)
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG_LIGHT)
    .hideTitleBar(true)
    .hideBackButton(true)
  }

  startNormalReview(): void {
    appState.startReview(ReviewMode.NORMAL)
    router.pushUrl({ url: 'pages/ReviewPage' })
  }

  startReinforcementReview(): void {
    appState.startReview(ReviewMode.REINFORCEMENT)
    router.pushUrl({ url: 'pages/ReviewPage' })
  }

  startMasteredReview(): void {
    appState.startReview(ReviewMode.MASTERED)
    router.pushUrl({ url: 'pages/ReviewPage' })
  }
}
