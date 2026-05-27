/**
 * Index.ets - Home screen of Kikaria HarmonyOS.
 * Shows app title, animated review bubble, stats dashboard, tag filters.
 * Supports two-column landscape layout on tablet/iPad-width devices.
 *
 * Translated from the home screen in source/Kikaria/ContentView.swift
 */
import { appState } from '../data/AppState'
import { ReviewMode, ordinalSuffix } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import {
  isPadLandscape, isTabletWidth, isPadPortrait, isTwoColumnCapable, pageHorizontalPadding,
  homeLandscapeAvailableWidth, homeLandscapeColumnSpacing,
  homeLandscapeLeftWidth, homeLandscapeRightWidth,
  homeLandscapeBubbleScale, homeLandscapeCardScale, homeScale,
  padPortraitHomeTitleSize, padPortraitHomeAvatarSize, padPortraitHomeTopPadding,
  padPortraitHomeBubbleSize, padPortraitHomeBubbleFontSize
} from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'
import { ScopeSelectionPageBuilder } from './ScopeSelectionPage'
import { TodayOverviewPageBuilder } from './TodayOverviewPage'
import { ReviewHistoryPageBuilder } from './ReviewHistoryPage'
import { MarkdownFormatGuideBuilder } from './MarkdownFormatGuidePage'
import { ReviewPageBuilder } from './ReviewPage'
import { SettingsPageBuilder } from './SettingsPage'
import { ReinforcementPageBuilder } from './ReinforcementPage'
import { MasteredPageBuilder } from './MasteredPage'
import { OnboardingPageBuilder } from './OnboardingPage'
import { PresetSelectionPageBuilder } from './PresetSelectionPage'
import { EditProfilePageBuilder } from './EditProfilePage'
import { EditKnowledgePointPageBuilder } from './EditKnowledgePointPage'
import { EditPresetPageBuilder } from './EditPresetPage'
import { NewPresetPageBuilder } from './NewPresetPage'
import { InitialProfileSetupPageBuilder } from './InitialProfileSetupPage'

@Entry
@Component
struct Index {
  @StorageLink('kikaria_isDarkMode') isDark: boolean = false
  @State contentPadH: number = 24
  @State presetName: string = ''
  @State masteredCount: number = 0
  @State reinforcedCount: number = 0
  @State totalCount: number = 0
  @State allTags: string[] = []
  @State selectedTags: Set<string> = new Set()
  @State dailyGoal: number = 20
  @State todayMastered: number = 0
  @State countdownDays: string = '-- Days Left'
  @State homeDateTitle: string = ''
  @State homeProgressText: string = ''
  @State bubbleScale: number = 1.0
  @State displayName: string = ''
  @State useTwoColumn: boolean = false

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.useTwoColumn = isTwoColumnCapable()
    this.refreshState()
    this.bubbleScale = 0.94

    if (!appState.hasCompletedProfileSetup) {
      navPathStack.pushPathByName('InitialProfileSetupPage', undefined)
    } else if (!appState.hasCompletedOnboarding) {
      navPathStack.pushPathByName('OnboardingPage', undefined)
    }
  }

  onPageShow(): void {
    this.refreshState()
  }

  refreshState(): void {
    this.presetName = appState.currentPreset.name
    this.masteredCount = appState.masteredCount
    this.reinforcedCount = appState.reinforcedCount
    this.totalCount = appState.totalCount
    this.allTags = appState.allTags
    this.selectedTags = new Set(appState.selectedTags)
    this.dailyGoal = appState.dailyGoal
    this.todayMastered = appState.todayMarkedMasteredCount
    this.displayName = appState.userProfile.displayName

    const days = appState.countdownDayCount
    this.countdownDays = days !== null ? `${days} Days Left` : '-- Days Left'

    const now = new Date()
    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
    const day = now.getDate()
    this.homeDateTitle = `${monthNames[now.getMonth()]} ${day}${ordinalSuffix(day)}`
    this.homeProgressText = `${this.todayMastered}/${this.dailyGoal}`
  }

  @Builder
  navDestinationRouter(name: string, param: Object) {
    if (name === 'ScopeSelectionPage') {
      ScopeSelectionPageBuilder(name, param)
    } else if (name === 'TodayOverviewPage') {
      TodayOverviewPageBuilder(name, param)
    } else if (name === 'ReviewHistoryPage') {
      ReviewHistoryPageBuilder(name, param)
    } else if (name === 'MarkdownFormatGuide') {
      MarkdownFormatGuideBuilder(name, param)
    } else if (name === 'ReviewPage') {
      ReviewPageBuilder(name, param)
    } else if (name === 'SettingsPage') {
      SettingsPageBuilder(name, param)
    } else if (name === 'ReinforcementPage') {
      ReinforcementPageBuilder(name, param)
    } else if (name === 'MasteredPage') {
      MasteredPageBuilder(name, param)
    } else if (name === 'OnboardingPage') {
      OnboardingPageBuilder(name, param)
    } else if (name === 'PresetSelectionPage') {
      PresetSelectionPageBuilder(name, param)
    } else if (name === 'EditProfilePage') {
      EditProfilePageBuilder(name, param)
    } else if (name === 'EditKnowledgePointPage') {
      EditKnowledgePointPageBuilder(name, param)
    } else if (name === 'EditPresetPage') {
      EditPresetPageBuilder(name, param)
    } else if (name === 'NewPresetPage') {
      NewPresetPageBuilder(name, param)
    } else if (name === 'InitialProfileSetupPage') {
      InitialProfileSetupPageBuilder(name, param)
    }
  }

  /* ── Header bar (shared across layouts) ── */
  @Builder
  headerBar() {
    Row() {
      Text('Kikaria')
        .fontSize(39)
        .fontWeight(FontWeight.Bold)
        .fontColor(KikariaColors.DEEP_TEXT)
        .fontFamily('serif')

      Blank()

      Row() {
        Text(this.displayName.charAt(0).toUpperCase())
          .fontSize(18)
          .fontWeight(FontWeight.Bold)
          .fontColor('#FFFFFF')
      }
      .width(44)
      .height(44)
      .borderRadius(22)
      .backgroundColor(KikariaColors.SKY)
      .justifyContent(FlexAlign.Center)
      .onClick(() => {
        navPathStack.pushPathByName('SettingsPage', undefined)
      })
    }
    .width('100%')
    .padding({ top: 14 })
  }

  /* ── Review bubble (animated, gradient) ── */
  @Builder
  reviewBubble(scale: number) {
    Column({ space: 10 }) {
      Text('开始背诵')
        .fontSize(32)
        .fontWeight(FontWeight.Bold)
        .fontColor('#FFFFFF')
        .fontFamily('serif')

      Text('→')
        .fontSize(64)
        .fontWeight(FontWeight.Lighter)
        .fontColor('#FFFFFF')
        .opacity(0.9)
    }
    .width(200)
    .height(200)
    .borderRadius(100)
    .justifyContent(FlexAlign.Center)
    .scale({ x: scale, y: scale })
    .linearGradient({
      angle: 135,
      colors: [[KikariaColors.SKY, 0], [KikariaColors.CYAN, 1]]
    })
    .shadow({
      radius: 28,
      color: `${KikariaColors.SKY}48`,
      offsetY: 18
    })
    .animation({
      duration: 5400,
      curve: Curve.EaseInOut,
      iterations: -1,
      playMode: PlayMode.Alternate
    })
    .onClick(() => {
      this.startNormalReview()
    })
  }

  /* ── Progress card ── */
  @Builder
  progressCard() {
    Row({ space: 14 }) {
      Column({ space: 5 }) {
        Text(this.homeDateTitle)
          .fontSize(23)
          .fontWeight(FontWeight.Medium)
          .fontColor(KikariaColors.DEEP_TEXT)
          .fontFamily('serif')
          .maxLines(1)

        Text(this.countdownDays)
          .fontSize(13)
          .fontWeight(FontWeight.Medium)
          .fontColor(KikariaColors.SOFT_TEXT)
          .maxLines(1)
      }
      .alignItems(HorizontalAlign.Start)

      Blank()

      Text(this.homeProgressText)
        .fontSize(25)
        .fontWeight(FontWeight.Bold)
        .fontColor(KikariaColors.MASTERED_DEEP_GREEN)
        .fontFamily('serif')
    }
    .width('100%')
    .padding(20)
    .borderRadius(25)
    .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
    .backdropBlur(20)
    .shadow({ radius: 17, color: KikariaColors.SHADOW_COLOR, offsetY: 9 })
    .onClick(() => {
      navPathStack.pushPathByName('TodayOverviewPage', undefined)
    })
  }

  /* ── Dashboard grid (Scope / Reinforcement / Mastered) ── */
  @Builder
  dashboardGrid() {
    Row({ space: 0 }) {
      Column({ space: 6 }) {
        Text(appState.selectedScopeCountText)
          .fontSize(26)
          .fontWeight(FontWeight.Bold)
          .fontColor(KikariaColors.SKY)
          .fontFamily('serif')

        Text('范围')
          .fontSize(13)
          .fontColor(KikariaColors.SOFT_TEXT)
      }
      .layoutWeight(1)
      .padding(16)
      .alignItems(HorizontalAlign.Center)
      .onClick(() => {
        navPathStack.pushPathByName('ScopeSelectionPage', undefined)
      })

      Row()
        .width(1)
        .height(40)
        .backgroundColor(`${KikariaColors.BLUE_GRAY_LIGHT}1E`)

      Column({ space: 6 }) {
        Text(`${this.reinforcedCount}`)
          .fontSize(26)
          .fontWeight(FontWeight.Bold)
          .fontColor(KikariaColors.NEXT_AMBER)
          .fontFamily('serif')

        Text('重点集锦')
          .fontSize(13)
          .fontColor(KikariaColors.SOFT_TEXT)
      }
      .layoutWeight(1)
      .padding(16)
      .alignItems(HorizontalAlign.Center)
      .onClick(() => {
        navPathStack.pushPathByName('ReinforcementPage', undefined)
      })

      Row()
        .width(1)
        .height(40)
        .backgroundColor(`${KikariaColors.BLUE_GRAY_LIGHT}1E`)

      Column({ space: 6 }) {
        Text(`${this.masteredCount}`)
          .fontSize(26)
          .fontWeight(FontWeight.Bold)
          .fontColor(KikariaColors.MASTERED_GREEN)
          .fontFamily('serif')

        Text('已掌握')
          .fontSize(13)
          .fontColor(KikariaColors.SOFT_TEXT)
      }
      .layoutWeight(1)
      .padding(16)
      .alignItems(HorizontalAlign.Center)
      .onClick(() => {
        navPathStack.pushPathByName('MasteredPage', undefined)
      })
    }
    .width('100%')
    .borderRadius(25)
    .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
    .backdropBlur(20)
    .shadow({ radius: 17, color: KikariaColors.SHADOW_COLOR, offsetY: 9 })
  }

  /* ── Preset selector card ── */
  @Builder
  presetSelectorCard() {
    Row({ space: 8 }) {
      Text(this.presetName)
        .fontSize(16)
        .fontWeight(FontWeight.Medium)
        .fontColor(KikariaColors.DEEP_TEXT)
        .maxLines(1)
        .layoutWeight(1)

      Text(`${this.totalCount} 条`)
        .fontSize(13)
        .fontColor(KikariaColors.SOFT_TEXT)

      Text('›')
        .fontSize(16)
        .fontColor(KikariaColors.TERTIARY_TEXT)
    }
    .width('100%')
    .padding(18)
    .borderRadius(25)
    .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
    .backdropBlur(20)
    .shadow({ radius: 17, color: KikariaColors.SHADOW_COLOR, offsetY: 9 })
    .onClick(() => {
      navPathStack.pushPathByName('PresetSelectionPage', undefined)
    })
  }

  /* ── Tag filter section ── */
  @Builder
  tagFilters() {
    Column({ space: 10 }) {
      Text('按标签筛选')
        .fontSize(15)
        .fontWeight(FontWeight.Medium)
        .fontColor(KikariaColors.DEEP_TEXT)
        .width('100%')

      if (this.allTags.length > 0) {
        Flex({ wrap: FlexWrap.Wrap, justifyContent: FlexAlign.Start }) {
          ForEach(this.allTags.slice(0, 12), (tag: string) => {
            Text(tag)
              .fontSize(12)
              .fontWeight(FontWeight.Medium)
              .fontColor(this.selectedTags.has(tag) ? '#FFFFFF' : KikariaColors.SOFT_TEXT)
              .backgroundColor(this.selectedTags.has(tag) ? KikariaColors.SKY : KikariaColors.MIST)
              .borderRadius(12)
              .padding({ left: 12, right: 12, top: 6, bottom: 6 })
              .margin({ right: 8, bottom: 8 })
              .onClick(() => {
                appState.toggleTag(tag)
                this.selectedTags = new Set(appState.selectedTags)
                this.refreshState()
              })
          })
        }
        .width('100%')
      }
    }
    .width('100%')
  }

  /* ── Reinforcement / Mastered quick-review rows ── */
  @Builder
  quickReviewRows() {
    Column({ space: 10 }) {
      if (this.reinforcedCount > 0) {
        Row() {
          Text(`★ 复习重点 (${this.reinforcedCount})`)
            .fontSize(15)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.NEXT_AMBER)
        }
        .width('100%')
        .height(44)
        .justifyContent(FlexAlign.Center)
        .borderRadius(14)
        .backgroundColor(`${KikariaColors.NEXT_AMBER}14`)
        .border({ width: 1, color: `${KikariaColors.NEXT_AMBER}33` })
        .onClick(() => {
          this.startReinforcementReview()
        })
      }

      if (this.masteredCount > 0) {
        Row() {
          Text(`✓ 浏览已掌握 (${this.masteredCount})`)
            .fontSize(15)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.MASTERED_GREEN)
        }
        .width('100%')
        .height(44)
        .justifyContent(FlexAlign.Center)
        .borderRadius(14)
        .backgroundColor(`${KikariaColors.MASTERED_GREEN}14`)
        .border({ width: 1, color: `${KikariaColors.MASTERED_GREEN}33` })
        .onClick(() => {
          this.startMasteredReview()
        })
      }
    }
    .width('100%')
  }

  /* ── Left column (compact layout) ── */
  @Builder
  compactLeftColumn() {
    Column({ space: 18 }) {
      // Bubble centered
      Column() {
        this.reviewBubble(this.bubbleScale)
      }
      .width('100%')
      .alignItems(HorizontalAlign.Center)
      .padding({ top: 16, bottom: 16 })

      this.progressCard()
      this.dashboardGrid()
      this.presetSelectorCard()
      this.tagFilters()
      this.quickReviewRows()
    }
  }

  /* ── Left column (landscape layout) ── */
  @Builder
  landscapeLeftColumn() {
    Column({ space: 18 }) {
      Column() {
        this.reviewBubble(this.bubbleScale * homeLandscapeBubbleScale())
      }
      .width('100%')
      .alignItems(HorizontalAlign.Center)
      .padding({ top: 12, bottom: 12 })

      this.progressCard()
      this.dashboardGrid()
    }
  }

  /* ── Right column (landscape layout) ── */
  @Builder
  landscapeRightColumn() {
    Column({ space: 18 }) {
      this.presetSelectorCard()
      this.tagFilters()
      this.quickReviewRows()
      Row().height(20)
    }
  }

  /* ── Two-column landscape layout ── */
  @Builder
  twoColumnLayout() {
    Column() {
      this.headerBar()

      Row({ space: homeLandscapeColumnSpacing() }) {
        Column() {
          this.landscapeLeftColumn()
        }
        .width(homeLandscapeLeftWidth())

        Column() {
          this.landscapeRightColumn()
        }
        .width(homeLandscapeRightWidth())
        .alignItems(HorizontalAlign.Start)
      }
      .width('100%')
      .constraintSize({ maxWidth: homeLandscapeAvailableWidth() })
      .justifyContent(FlexAlign.Center)
    }
  }

  /* ── iPad portrait layout ── */
  @Builder
  padPortraitLayout() {
    Column({ space: 0 }) {
      Row() {
        Text('Kikaria')
          .fontSize(padPortraitHomeTitleSize())
          .fontWeight(FontWeight.Bold)
          .fontColor(KikariaColors.DEEP_TEXT)
          .fontFamily('serif')

        Blank()

        Row() {
          Text(this.displayName.charAt(0).toUpperCase())
            .fontSize(Math.round(padPortraitHomeAvatarSize() * 0.35))
            .fontWeight(FontWeight.Bold)
            .fontColor('#FFFFFF')
        }
        .width(padPortraitHomeAvatarSize())
        .height(padPortraitHomeAvatarSize())
        .borderRadius(padPortraitHomeAvatarSize() / 2)
        .backgroundColor(KikariaColors.SKY)
        .justifyContent(FlexAlign.Center)
        .onClick(() => { navPathStack.pushPathByName('SettingsPage', undefined) })
      }
      .width('100%')
      .padding({ top: padPortraitHomeTopPadding() })

      Column() {
        Column() {
          Blank()
          Column({ space: 10 }) {
            Text('开始背诵')
              .fontSize(padPortraitHomeBubbleFontSize())
              .fontWeight(FontWeight.Bold)
              .fontColor('#FFFFFF')
              .fontFamily('serif')

            Text('→')
              .fontSize(72)
              .fontWeight(FontWeight.Lighter)
              .fontColor('#FFFFFF')
              .opacity(0.9)
          }
          .width(padPortraitHomeBubbleSize())
          .height(padPortraitHomeBubbleSize())
          .borderRadius(padPortraitHomeBubbleSize() / 2)
          .justifyContent(FlexAlign.Center)
          .scale({ x: this.bubbleScale, y: this.bubbleScale })
          .linearGradient({
            angle: 135,
            colors: [[KikariaColors.SKY, 0], [KikariaColors.CYAN, 1]]
          })
          .shadow({ radius: 28, color: `${KikariaColors.SKY}48`, offsetY: 18 })
          .animation({ duration: 5400, curve: Curve.EaseInOut, iterations: -1, playMode: PlayMode.Alternate })
          .onClick(() => { this.startNormalReview() })
          Blank()
        }
        .width('100%')
        .layoutWeight(1)
        .alignItems(HorizontalAlign.Center)
      }
      .width('100%')

      Column({ space: 18 }) {
        this.progressCard()
        this.dashboardGrid()
        this.presetSelectorCard()
        this.tagFilters()
        this.quickReviewRows()
        Row().height(40)
      }
      .width('100%')
    }
  }

  /* ── Single-column layout (compact / portrait tablet) ── */
  @Builder
  singleColumnLayout() {
    Column({ space: 18 }) {
      this.headerBar()
      this.compactLeftColumn()
      Row().height(40)
    }
    .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
  }

  build() {
    Navigation() {
      Scroll() {
        Column() {
          if (this.useTwoColumn) {
            this.twoColumnLayout()
          } else if (isPadPortrait()) {
            this.padPortraitLayout()
          } else {
            this.singleColumnLayout()
          }
        }
        .width('100%')
        .padding({ left: this.contentPadH, right: this.contentPadH })
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG)
      .scrollBar(BarState.Off)
      .align(Alignment.TopStart)
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
    .navDestination(this.navDestinationRouter)
  }

  startNormalReview(): void {
    appState.startReview(ReviewMode.NORMAL)
    navPathStack.pushPathByName('ReviewPage', undefined)
  }

  startReinforcementReview(): void {
    appState.startReview(ReviewMode.REINFORCEMENT)
    navPathStack.pushPathByName('ReviewPage', undefined)
  }

  startMasteredReview(): void {
    appState.startReview(ReviewMode.MASTERED)
    navPathStack.pushPathByName('ReviewPage', undefined)
  }
}
