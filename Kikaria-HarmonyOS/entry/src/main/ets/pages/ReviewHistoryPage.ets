/**
 * ReviewHistoryPage.ets - Full activity history for the current preset.
 * Uses LiquidGlass card styling and iPad portrait adaptive top inset.
 *
 * Translated from ReviewHistoryView in source/Kikaria/ContentView.swift (line ~3113)
 */
import { appState } from '../data/AppState'
import { StudyActivityRecord, StudyActivityType } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding, ipadPortraitListPageTopInset } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

@Builder
export function ReviewHistoryPageBuilder(name: string, param: Object) {
  ReviewHistoryPageContent()
}

@Component
export struct ReviewHistoryPageContent {
  @State contentPadH: number = 24
  @State historyRecords: StudyActivityRecord[] = []
  @State groupedRecords: Record<string, StudyActivityRecord[]> = {}
  @State sortedDates: string[] = []

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.refreshState()
  }

  refreshState(): void {
    this.historyRecords = [...appState.getCurrentPresetActivityRecords()]
      .sort((a, b) => b.date - a.date)

    const groups: Record<string, StudyActivityRecord[]> = {}
    for (const record of this.historyRecords) {
      const d = new Date(record.date)
      d.setHours(0, 0, 0, 0)
      const key = d.getTime().toString()
      if (!groups[key]) {
        groups[key] = []
      }
      groups[key].push(record)
    }
    this.groupedRecords = groups
    this.sortedDates = Object.keys(groups).sort((a, b) => parseInt(b) - parseInt(a))
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

          Text('复习历史')
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.DEEP_TEXT)

          Blank()

          Text(`${this.historyRecords.length}条`)
            .fontSize(13)
            .fontColor(KikariaColors.SOFT_TEXT)
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12 + ipadPortraitListPageTopInset(), bottom: 12 })

        if (this.sortedDates.length === 0) {
          Column({ space: 12 }) {
            Text('暂无记录')
              .fontSize(20)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)
            Text('完成复习后，活动记录将出现在这里。')
              .fontSize(15)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
          .width('100%')
          .height('100%')
          .justifyContent(FlexAlign.Center)
        } else {
          List({ space: 0 }) {
            ForEach(this.sortedDates, (dateKey: string) => {
              ListItem() {
                Column({ space: 8 }) {
                  // Date header
                  Row() {
                    Text(this.formatDate(parseInt(dateKey)))
                      .fontSize(14)
                      .fontWeight(FontWeight.Medium)
                      .fontColor(KikariaColors.SOFT_TEXT)
                      .padding({ left: 16, top: 16, bottom: 8 })
                  }
                  .width('100%')

                  ForEach(this.groupedRecords[dateKey], (record: StudyActivityRecord) => {
                    Row({ space: 10 }) {
                      Text(this.activityIcon(record.type))
                        .fontSize(14)

                      Text(record.pointTitle)
                        .fontSize(14)
                        .fontColor(KikariaColors.DEEP_TEXT)
                        .layoutWeight(1)
                        .maxLines(1)
                        .textOverflow({ overflow: TextOverflow.Ellipsis })

                      Text(this.activityLabel(record.type))
                        .fontSize(12)
                        .fontColor(KikariaColors.SOFT_TEXT)

                      Text(this.formatTime(record.date))
                        .fontSize(12)
                        .fontColor(KikariaColors.TERTIARY_TEXT)
                    }
                    .width('100%')
                    .padding({ left: 16, right: 16, top: 10, bottom: 10 })
                    .border({ width: { bottom: 1 }, color: KikariaColors.MIST })
                  })
                }
                .width('100%')
                .borderRadius(18)
                .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
                .backdropBlur(10)
                .shadow({ radius: 6, color: KikariaColors.SHADOW_COLOR, offsetY: 2 })
                .margin({ bottom: 8 })
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
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG)
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
      case StudyActivityType.ADDED_REINFORCEMENT: return '标重点'
      case StudyActivityType.REMOVED_REINFORCEMENT: return '取消重点'
      case StudyActivityType.VIEWED_HINT: return '看提示'
      case StudyActivityType.REVIEWED_ANSWER: return '复习'
    }
  }

  formatDate(timestamp: number): string {
    const d = new Date(timestamp)
    return `${d.getFullYear()}/${d.getMonth() + 1}/${d.getDate()}`
  }

  formatTime(timestamp: number): string {
    const d = new Date(timestamp)
    return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
  }
}

@Entry
@Component
struct ReviewHistoryPage {
  build() {
    Navigation() {
      ReviewHistoryPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
