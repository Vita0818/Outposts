/**
 * ReviewPage.ets - Review flow for knowledge points.
 * Shows title, allows revealing hint/answer, marking as important/mastered.
 * Action buttons use gradient styling matching iOS ReviewActionButton.
 *
 * Translated from ReviewView in source/Kikaria/ContentView.swift (line 7567)
 */
import { appState } from '../data/AppState'
import { ReviewMode } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import { renderMathRichText } from '../model/KikariaMathRenderer'
import { isPadLandscape, isTwoColumnCapable, pageHorizontalPadding, reviewMaxWidth,
  reviewLandscapeLeftWidth, reviewLandscapeRightWidth, reviewLandscapeColumnSpacing,
  reviewLandscapeMaxWidth } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

@Builder
export function ReviewPageBuilder(name: string, param: Object) {
  ReviewPageContent()
}

@Component
export struct ReviewPageContent {
  @StorageLink('kikaria_isDarkMode') isDark: boolean = false
  @State title: string = ''
  @State contentPadH: number = 24
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
  @State todayReviewCount: number = 0
  @State useTwoColumn: boolean = false

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.useTwoColumn = isTwoColumnCapable()
    this.refreshFromState()
  }

  refreshFromState(): void {
    const point = appState.currentPoint
    if (point) {
      this.title = point.title
      this.tags = point.tags
      this.hint = renderMathRichText(point.hint)
      this.content = renderMathRichText(point.content)
      this.isReinforced = point.isReinforced
      this.isMastered = point.isMastered
    }
    this.isHintVisible = appState.isHintVisible
    this.isContentVisible = appState.isContentVisible
    this.currentIndex = appState.currentReviewIndex
    this.queueLength = appState.reviewQueue.length
    this.reviewMode = appState.reviewMode
    this.hasNext = appState.hasMoreReviewPoints
    this.todayReviewCount = appState.todayReviewedAnswerCount
  }

  /* ── Gradient action button (primary style) ── */
  @Builder
  primaryButton(label: string, gradientStart: string, gradientEnd: string, onTap: () => void, fullWidth: boolean) {
    Button() {
      Text(label)
        .fontSize(17)
        .fontWeight(FontWeight.Bold)
        .fontColor('#FFFFFF')
    }
    .width(fullWidth ? '100%' : undefined)
    .height(50)
    .borderRadius(16)
    .linearGradient({
      angle: 135,
      colors: [[gradientStart, 0], [gradientEnd, 1]]
    })
    .shadow({
      radius: 14,
      color: `${gradientStart}60`,
      offsetY: 8
    })
    .onClick(() => onTap())
  }

  /* ── Bordered secondary action ── */
  @Builder
  secondaryButton(label: string, tint: string, onTap: () => void) {
    Button() {
      Text(label)
        .fontSize(15)
        .fontWeight(FontWeight.Medium)
        .fontColor(tint)
    }
    .width('100%')
    .height(48)
    .borderRadius(14)
    .backgroundColor(`${tint}10`)
    .border({ width: 1, color: `${tint}33` })
    .onClick(() => onTap())
  }

  /* ── Glass card for revealed content ── */
  @Builder
  revealCard(label: string, labelColor: string, text: string) {
    Column({ space: 8 }) {
      Text(label)
        .fontSize(13)
        .fontWeight(FontWeight.Medium)
        .fontColor(labelColor)
        .width('100%')

      Text(text)
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

  /* ── Reading column (title, tags, hint, answer) ── */
  @Builder
  readingColumn() {
    Column({ space: 18 }) {
      if (this.tags.length > 0) {
        Row({ space: 8 }) {
          ForEach(this.tags, (tag: string) => {
            Text(tag)
              .fontSize(12).fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.SOFT_TEXT)
              .backgroundColor(KikariaColors.MIST)
              .borderRadius(8)
              .padding({ left: 10, right: 10, top: 4, bottom: 4 })
          })
        }
        .width('100%').justifyContent(FlexAlign.Center)
      }
      Row() {
        Text(`今日复习: ${this.todayReviewCount} 次`)
          .fontSize(12).fontColor(KikariaColors.TERTIARY_TEXT)
      }.width('100%').justifyContent(FlexAlign.Center)

      Text(this.title)
        .fontSize(32).fontWeight(FontWeight.Bold)
        .fontColor(KikariaColors.DEEP_TEXT)
        .textAlign(TextAlign.Center).width('100%').padding({ top: 8 })

      if (this.isHintVisible) {
        this.revealCard('💡 提示', KikariaColors.SKY, this.hint)
      } else {
        this.primaryButton('💡 查看提示', KikariaColors.NEXT_GRADIENT_START, KikariaColors.NEXT_GRADIENT_END,
          () => { appState.showHint(); this.refreshFromState() }, true)
      }
      if (this.isContentVisible) {
        this.revealCard('📖 答案', KikariaColors.MASTERED_GREEN, this.content)
      } else if (this.isHintVisible) {
        this.primaryButton('📖 查看答案', KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END,
          () => { appState.showContent(); this.refreshFromState() }, true)
      }
    }
  }

  /* ── Action panel (right column in two-column landscape) ── */
  @Builder
  actionPanel() {
    Column({ space: 12 }) {
      if (this.reviewMode === ReviewMode.REINFORCEMENT) {
        this.secondaryButton('☆ 移出重点', KikariaColors.REMOVE_CORAL, () => {
          appState.toggleReinforcement(); this.refreshFromState(); this.advanceOrFinish()
        })
        this.secondaryButton('✓ 标记为已掌握', KikariaColors.MASTERED_GREEN, () => {
          appState.toggleMastered(); this.refreshFromState(); this.advanceOrFinish()
        })
      } else if (this.reviewMode === ReviewMode.MASTERED) {
        this.secondaryButton('★ 加入重点', KikariaColors.NEXT_AMBER, () => {
          appState.toggleReinforcement(); this.refreshFromState(); this.advanceOrFinish()
        })
        this.secondaryButton('↩ 取消掌握', KikariaColors.REMOVE_CORAL, () => {
          appState.toggleMastered(); this.refreshFromState(); this.advanceOrFinish()
        })
      } else {
        this.secondaryButton(this.isReinforced ? '☆ 移出重点' : '★ 加入重点', KikariaColors.NEXT_AMBER, () => {
          appState.toggleReinforcement(); this.refreshFromState()
        })
        this.secondaryButton(this.isMastered ? '↩ 取消掌握' : '✓ 标记为已掌握',
          this.isMastered ? KikariaColors.REMOVE_CORAL : KikariaColors.MASTERED_GREEN, () => {
          appState.toggleMastered(); this.refreshFromState()
        })
      }
      this.primaryButton(this.hasNext ? '下一个 →' : '✓ 完成复习',
        KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END,
        () => { this.advanceOrFinish() }, true)
    }
  }

  build() {
    NavDestination() {
      Column() {
        // Top bar: back | mode name | progress
        Row() {
          Button() {
            Text('← 返回')
              .fontSize(17)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => {
            navPathStack.pop()
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
            Text('没有待复习的项目')
              .fontSize(20)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)

            Text(this.getEmptyMessage())
              .fontSize(15)
              .fontColor(KikariaColors.SOFT_TEXT)
              .textAlign(TextAlign.Center)
              .padding({ left: 40, right: 40 })

            this.primaryButton('返回首页', KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END, () => {
              navPathStack.pop()
            }, false)
          }
          .width('100%')
          .height('100%')
          .justifyContent(FlexAlign.Center)
          .padding(24)
        } else {
          // Review content
          Scroll() {
            if (this.useTwoColumn) {
              Row({ space: reviewLandscapeColumnSpacing() }) {
                Column() { this.readingColumn() }
                  .width(reviewLandscapeLeftWidth())
                Column() {
                  if (this.isContentVisible) { this.actionPanel() }
                }
                .width(reviewLandscapeRightWidth())
              }
              .width('100%')
              .constraintSize({ maxWidth: reviewLandscapeMaxWidth() })
              .justifyContent(FlexAlign.Center)
            } else {
              Column({ space: 18 }) {
                if (this.tags.length > 0) {
                  Row({ space: 8 }) {
                    ForEach(this.tags, (tag: string) => {
                      Text(tag)
                        .fontSize(12).fontWeight(FontWeight.Medium)
                        .fontColor(KikariaColors.SOFT_TEXT)
                        .backgroundColor(KikariaColors.MIST)
                        .borderRadius(8)
                        .padding({ left: 10, right: 10, top: 4, bottom: 4 })
                    })
                  }.width('100%').justifyContent(FlexAlign.Center)
                }
                Row() {
                  Text(`今日复习: ${this.todayReviewCount} 次`)
                    .fontSize(12).fontColor(KikariaColors.TERTIARY_TEXT)
                }.width('100%').justifyContent(FlexAlign.Center)

                Text(this.title)
                  .fontSize(32).fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .textAlign(TextAlign.Center).width('100%').padding({ top: 8 })

                if (this.isHintVisible) {
                  this.revealCard('💡 提示', KikariaColors.SKY, this.hint)
                } else {
                  this.primaryButton('💡 查看提示', KikariaColors.NEXT_GRADIENT_START, KikariaColors.NEXT_GRADIENT_END,
                    () => { appState.showHint(); this.refreshFromState() }, true)
                }
                if (this.isContentVisible) {
                  this.revealCard('📖 答案', KikariaColors.MASTERED_GREEN, this.content)
                  this.actionPanel()
                } else if (this.isHintVisible) {
                  this.primaryButton('📖 查看答案', KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END,
                    () => { appState.showContent(); this.refreshFromState() }, true)
                }
                Row().height(40)
              }
              .width('100%')
              .constraintSize({ maxWidth: isPadLandscape() ? reviewMaxWidth() : '100%' })
            }
          }
          .width('100%')
          .layoutWeight(1)
          .scrollBar(BarState.Off)
          .align(Alignment.TopStart)
          .padding({ left: this.contentPadH, right: this.contentPadH })
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

  advanceOrFinish(): void {
    if (this.hasNext) {
      appState.nextPoint()
      setTimeout(() => {
        this.refreshFromState()
      }, 200)
    } else {
      navPathStack.pop()
    }
  }

  getModeTitle(): string {
    switch (this.reviewMode) {
      case ReviewMode.NORMAL:
        return '复习'
      case ReviewMode.REINFORCEMENT:
        return '重点复习'
      case ReviewMode.MASTERED:
        return '已掌握回顾'
    }
  }

  getEmptyMessage(): string {
    switch (this.reviewMode) {
      case ReviewMode.NORMAL:
        return '当前范围内所有知识点均已掌握！调整标签筛选或继续巩固吧。'
      case ReviewMode.REINFORCEMENT:
        return '重点集锦中暂无项目。在复习过程中将重要知识点加入重点集锦吧。'
      case ReviewMode.MASTERED:
        return '暂无已掌握的知识点。继续复习，掌握更多知识吧！'
    }
  }
}

@Entry
@Component
struct ReviewPage {
  build() {
    Navigation() {
      ReviewPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
