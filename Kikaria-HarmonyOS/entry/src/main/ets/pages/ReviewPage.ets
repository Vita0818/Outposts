/**
 * ReviewPage.ets - Review flow for knowledge points.
 * Shows title, allows revealing hint and answer, marking as important/mastered.
 *
 * Translated from the review flow in source/Kikaria/ContentView.swift
 */

import { router } from '@kit.ArkUI'
import { appState } from '../data/AppState'
import { ReviewMode } from '../model/KnowledgePoint'
import { KikariaCard, KikariaButton } from '../components/KikariaComponents'
import { KikariaColors } from '../components/KikariaTheme'

@Entry
@Component
struct ReviewPage {
  @State title: string = ''
  @State tags: string[] = []
  @State hint: string = ''
  @State content: string = ''
  @State isHintVisible: boolean = false
  @State isContentVisible: boolean = false
  @State isReinforced: boolean = false
  @State isMastered: boolean = false
  @State currentIndex: number = 0
  @State queueLength: number = 0
  @State reviewMode: ReviewMode = ReviewMode.NORMAL
  @State hasNext: boolean = false

  aboutToAppear(): void {
    this.refreshFromState()
  }

  refreshFromState(): void {
    const point = appState.currentPoint
    if (point) {
      this.title = point.title
      this.tags = point.tags
      this.hint = point.hint
      this.content = point.content
      this.isReinforced = point.isReinforced
      this.isMastered = point.isMastered
    }
    this.isHintVisible = appState.isHintVisible
    this.isContentVisible = appState.isContentVisible
    this.currentIndex = appState.currentReviewIndex
    this.queueLength = appState.reviewQueue.length
    this.reviewMode = appState.reviewMode
    this.hasNext = appState.hasMoreReviewPoints
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

          Text(this.getModeTitle())
            .fontSize(13)
            .fontColor(KikariaColors.SOFT_TEXT)

          Blank()

          Text(`${this.currentIndex + 1} / ${this.queueLength}`)
            .fontSize(13)
            .fontColor(KikariaColors.SOFT_TEXT)
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12, bottom: 12 })

        if (this.queueLength === 0) {
          // Empty state
          Column({ space: 16 }) {
            Text('No items to review')
              .fontSize(20)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)

            Text(this.getEmptyMessage())
              .fontSize(15)
              .fontColor(KikariaColors.SOFT_TEXT)

            KikariaButton({
              label: 'Go Back',
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
          // Review content
          Scroll() {
            Column({ space: 20 }) {
              // Tags
              if (this.tags.length > 0) {
                Row({ space: 8 }) {
                  ForEach(this.tags, (tag: string) => {
                    Text(tag)
                      .fontSize(12)
                      .fontColor(KikariaColors.SOFT_TEXT)
                      .backgroundColor(KikariaColors.MIST)
                      .borderRadius(8)
                      .padding({ left: 10, right: 10, top: 4, bottom: 4 })
                  })
                }
                .width('100%')
              }

              // Title card
              KikariaCard({ cornerRadius: 24 }) {
                Column({ space: 8 }) {
                  Text(this.title)
                    .fontSize(24)
                    .fontWeight(FontWeight.Bold)
                    .fontColor(KikariaColors.DEEP_TEXT)
                    .textAlign(TextAlign.Start)
                    .width('100%')
                }
                .width('100%')
              }

              // Hint section
              if (this.isHintVisible) {
                KikariaCard({ cornerRadius: 20 }) {
                  Column({ space: 8 }) {
                    Text('Hint')
                      .fontSize(13)
                      .fontWeight(FontWeight.Medium)
                      .fontColor(KikariaColors.SKY)

                    Text(this.hint)
                      .fontSize(15)
                      .fontColor(KikariaColors.DEEP_TEXT)
                      .width('100%')
                      .textAlign(TextAlign.Start)
                  }
                  .width('100%')
                  .alignItems(HorizontalAlign.Start)
                }
              } else {
                KikariaButton({
                  label: 'Show Hint',
                  buttonType: 'secondary',
                  onClick: () => {
                    appState.showHint()
                    this.refreshFromState()
                  }
                })
              }

              // Content section
              if (this.isContentVisible) {
                KikariaCard({ cornerRadius: 20 }) {
                  Column({ space: 8 }) {
                    Text('Answer')
                      .fontSize(13)
                      .fontWeight(FontWeight.Medium)
                      .fontColor(KikariaColors.MASTERED_GREEN)

                    Text(this.content)
                      .fontSize(15)
                      .fontColor(KikariaColors.DEEP_TEXT)
                      .width('100%')
                      .textAlign(TextAlign.Start)
                  }
                  .width('100%')
                  .alignItems(HorizontalAlign.Start)
                }
              } else {
                KikariaButton({
                  label: 'Show Answer',
                  buttonType: 'primary',
                  onClick: () => {
                    appState.showContent()
                    this.refreshFromState()
                  }
                })
              }

              // Action buttons (only visible after content is revealed)
              if (this.isContentVisible) {
                Column({ space: 12 }) {
                  // Important / Reinforcement toggle
                  KikariaButton({
                    label: this.isReinforced ? '★ Remove from Important' : '☆ Add to Important',
                    buttonType: this.isReinforced ? 'secondary' : 'secondary',
                    onClick: () => {
                      appState.toggleReinforcement()
                      this.refreshFromState()
                    }
                  })

                  // Mastered toggle
                  KikariaButton({
                    label: this.isMastered ? '✓ Mark as Not Mastered' : '✓ Mark as Mastered',
                    buttonType: this.isMastered ? 'secondary' : 'secondary',
                    onClick: () => {
                      appState.toggleMastered()
                      this.refreshFromState()
                    }
                  })

                  // Next button
                  KikariaButton({
                    label: this.hasNext ? 'Next →' : 'Finish Review',
                    buttonType: 'primary',
                    onClick: () => {
                      if (this.hasNext) {
                        appState.nextPoint()
                        this.refreshFromState()
                      } else {
                        router.back()
                      }
                    }
                  })
                }
              }

              // Bottom spacer
              Row().height(40)
            }
            .width('100%')
            .padding({ left: 24, right: 24 })
          }
          .width('100%')
          .layoutWeight(1)
          .scrollBar(BarState.Off)
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

  getModeTitle(): string {
    switch (this.reviewMode) {
      case ReviewMode.NORMAL:
        return 'Review'
      case ReviewMode.REINFORCEMENT:
        return 'Important Review'
      case ReviewMode.MASTERED:
        return 'Mastered Review'
    }
  }

  getEmptyMessage(): string {
    switch (this.reviewMode) {
      case ReviewMode.NORMAL:
        return 'All knowledge points in this scope have been mastered!'
      case ReviewMode.REINFORCEMENT:
        return 'No items in your important list.'
      case ReviewMode.MASTERED:
        return 'No mastered items yet. Keep reviewing!'
    }
  }
}
