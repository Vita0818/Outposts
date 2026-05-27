/**
 * TodayOverviewPage.ets - Today's study overview with progress and activity.
 * Uses LiquidGlass card styling matching iOS TodayOverviewView.
 *
 * Translated from TodayOverviewView in source/Kikaria/ContentView.swift (line 2964)
 */
import { appState } from '../data/AppState'
import { StudyActivityRecord, StudyActivityType } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding, ipadPortraitOverviewTopInset } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

@Builder
export function TodayOverviewPageBuilder(name: string, param: Object) {
  TodayOverviewPageContent()
}

@Component
export struct TodayOverviewPageContent {
  @State presetName: string = ''
  @State masteredCount: number = 0
  @State totalCount: number = 0
  @State dailyGoal: number = 0
  @State todayMastered: number = 0
  @State todayHint: number = 0
  @State todayReview: number = 0
  @State countdownDays: string = '--'
  @State recentActivity: StudyActivityRecord[] = []
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.refreshState()
  }

  refreshState(): void {
    this.presetName = appState.currentPreset.name
    this.masteredCount = appState.masteredCount
    this.totalCount = appState.totalCount
    this.dailyGoal = appState.dailyGoal
    this.todayMastered = appState.todayMarkedMasteredCount
    this.todayHint = appState.todayViewedHintCount
    this.todayReview = appState.todayReviewedAnswerCount

    const days = appState.countdownDayCount
    this.countdownDays = days !== null ? `${days} 天` : '--'

    const records = appState.getCurrentPresetActivityRecords()
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    this.recentActivity = records
      .filter(r => r.date >= today.getTime())
      .sort((a, b) => b.date - a.date)
      .slice(0, 20)
  }

  build() {
    NavDestination() {
      Scroll() {
        Column({ space: 18 }) {
          Row() {
            Button() {
              Text('← 返回')
                .fontSize(17)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
            .backgroundColor(Color.Transparent)
            .onClick(() => { navPathStack.pop() })

            Blank()

            Text('今日概览')
              .fontSize(17)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)
          }
          .width('100%')
          .padding({ left: 20, right: 20, top: 12 + ipadPortraitOverviewTopInset(), bottom: 12 })

          // Main progress card
          Column({ space: 12 }) {
            Text(`${this.todayMastered}/${this.dailyGoal}`)
              .fontSize(48)
              .fontWeight(FontWeight.Bold)
              .fontColor(KikariaColors.DEEP_TEXT)
              .fontFamily('serif')

            Text('今日掌握')
              .fontSize(15)
              .fontColor(KikariaColors.SOFT_TEXT)

            Row() {
              Row()
                .width(`${Math.min(100, this.todayMastered / Math.max(1, this.dailyGoal) * 100)}%`)
                .height(6)
                .borderRadius(3)
                .linearGradient({
                  angle: 90,
                  colors: [[KikariaColors.MASTERED_GRADIENT_START, 0], [KikariaColors.MASTERED_GRADIENT_END, 1]]
                })
            }
            .width('100%')
            .height(6)
            .borderRadius(3)
            .backgroundColor(KikariaColors.MIST)
          }
          .width('100%')
          .padding(24)
          .borderRadius(22)
          .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
          .backdropBlur(18)
          .shadow({ radius: 14, color: KikariaColors.SHADOW_COLOR, offsetY: 6 })

          // Stats grid
          Row({ space: 12 }) {
            Column({ space: 6 }) {
              Text(`${this.masteredCount}`)
                .fontSize(28)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.MASTERED_GREEN)
                .fontFamily('serif')

              Text('总掌握')
                .fontSize(12)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
            .layoutWeight(1)
            .padding(16)
            .borderRadius(18)
            .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
            .backdropBlur(12)
            .shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 })

            Column({ space: 6 }) {
              Text(`${this.todayHint}`)
                .fontSize(28)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.SKY)
                .fontFamily('serif')

              Text('查看提示')
                .fontSize(12)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
            .layoutWeight(1)
            .padding(16)
            .borderRadius(18)
            .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
            .backdropBlur(12)
            .shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 })

            Column({ space: 6 }) {
              Text(`${this.todayReview}`)
                .fontSize(28)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.NEXT_AMBER)
                .fontFamily('serif')

              Text('查看答案')
                .fontSize(12)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
            .layoutWeight(1)
            .padding(16)
            .borderRadius(18)
            .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
            .backdropBlur(12)
            .shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 })
          }
          .width('100%')

          // Countdown card
          Column({ space: 8 }) {
            Text('倒计时')
              .fontSize(15)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)
              .width('100%')

            Row({ space: 16 }) {
              Text(this.countdownDays)
                .fontSize(28)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)
                .fontFamily('serif')

              Text(`总知识点: ${this.totalCount}`)
                .fontSize(13)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
          }
          .width('100%')
          .padding(20)
          .borderRadius(18)
          .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
          .backdropBlur(12)
          .shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 })

          // Today's activity
          if (this.recentActivity.length > 0) {
            Column({ space: 8 }) {
              Text('今日活动')
                .fontSize(15)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.DEEP_TEXT)
                .width('100%')

              ForEach(this.recentActivity, (record: StudyActivityRecord) => {
                Row({ space: 12 }) {
                  Text(this.activityIcon(record.type))
                    .fontSize(16)

                  Text(record.pointTitle)
                    .fontSize(14)
                    .fontColor(KikariaColors.DEEP_TEXT)
                    .layoutWeight(1)
                    .maxLines(1)
                    .textOverflow({ overflow: TextOverflow.Ellipsis })

                  Text(this.activityLabel(record.type))
                    .fontSize(12)
                    .fontColor(KikariaColors.SOFT_TEXT)
                }
                .width('100%')
                .padding({ top: 10, bottom: 10 })
                .border({ width: { bottom: 1 }, color: KikariaColors.MIST })
              })
            }
            .width('100%')
            .padding(20)
            .borderRadius(18)
            .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
            .backdropBlur(12)
            .shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 })
          }

          Row() {
            Button('查看历史记录 →')
              .fontSize(15)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.SKY)
              .backgroundColor(Color.Transparent)
              .onClick(() => {
                navPathStack.pushPathByName('ReviewHistoryPage', undefined)
              })
          }
          .width('100%')
          .padding({ top: 8 })

          Row().height(40)
        }
        .width('100%')
        .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
        .padding({ left: this.contentPadH, right: this.contentPadH })
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG)
      .scrollBar(BarState.Off)
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }

  activityIcon(type: StudyActivityType): string {
    switch (type) {
      case StudyActivityType.MARKED_MASTERED: return '✓'
      case StudyActivityType.REMOVED_MASTERED: return '↩'
      case StudyActivityType.ADDED_REINFORCEMENT: return '★'
      case StudyActivityType.REMOVED_REINFORCEMENT: return '☆'
      case StudyActivityType.VIEWED_HINT: return '💡'
      case StudyActivityType.REVIEWED_ANSWER: return '📖'
    }
  }

  activityLabel(type: StudyActivityType): string {
    switch (type) {
      case StudyActivityType.MARKED_MASTERED: return '已掌握'
      case StudyActivityType.REMOVED_MASTERED: return '取消掌握'
      case StudyActivityType.ADDED_REINFORCEMENT: return '已标重点'
      case StudyActivityType.REMOVED_REINFORCEMENT: return '取消重点'
      case StudyActivityType.VIEWED_HINT: return '查看提示'
      case StudyActivityType.REVIEWED_ANSWER: return '复习'
    }
  }
}

@Entry
@Component
struct TodayOverviewPage {
  build() {
    Navigation() {
      TodayOverviewPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
