/**
 * OnboardingPage.ets - First-launch guide to Kikaria concepts.
 * Uses LiquidGlass card styling matching iOS OnboardingView.
 *
 * Translated from OnboardingView in source/Kikaria/ContentView.swift (line 2635)
 */
import { appState } from '../data/AppState'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

@Builder
export function OnboardingPageBuilder(name: string, param: Object) {
  OnboardingPageContent()
}

@Component
export struct OnboardingPageContent {
  @State currentPage: number = 0
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
  }

  build() {
    NavDestination() {
      Column() {
        Row() {
          Blank()
          Button('跳过')
            .fontSize(15)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.SKY)
            .backgroundColor(Color.Transparent)
            .onClick(() => {
              appState.completeOnboarding()
              navPathStack.pop()
            })
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12, bottom: 12 })

        // Page indicator
        Row({ space: 8 }) {
          ForEach([0, 1, 2], (i: number) => {
            Row()
              .width(this.currentPage === i ? 24 : 8)
              .height(8)
              .borderRadius(4)
              .backgroundColor(this.currentPage === i ? KikariaColors.SKY : KikariaColors.MIST)
          })
        }
        .justifyContent(FlexAlign.Center)
        .width('100%')
        .padding({ top: 16, bottom: 16 })

        // Swipeable content area
        Column() {
          if (this.currentPage === 0) {
            Column({ space: 20 }) {
              Text('📚')
                .fontSize(64)

              Text('欢迎使用 Kikaria')
                .fontSize(28)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)
                .fontFamily('serif')

              Text('Kikaria 帮助你系统化地学习和记忆知识点。\n通过间隔重复和主动回忆，提高学习效率。')
                .fontSize(16)
                .fontColor(KikariaColors.SOFT_TEXT)
                .textAlign(TextAlign.Center)
                .lineHeight(26)
                .padding({ left: 32, right: 32 })
            }
            .width('100%')
            .layoutWeight(1)
            .justifyContent(FlexAlign.Center)
          } else if (this.currentPage === 1) {
            Column({ space: 20 }) {
              Text('🔄')
                .fontSize(64)

              Text('间隔复习')
                .fontSize(28)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)
                .fontFamily('serif')

              Text('每次学习时，先看提示尝试回忆，再看答案验证。\n将重要知识点加入「重点集锦」强化练习，\n掌握的标记为「已掌握」跟踪进度。')
                .fontSize(16)
                .fontColor(KikariaColors.SOFT_TEXT)
                .textAlign(TextAlign.Center)
                .lineHeight(26)
                .padding({ left: 32, right: 32 })
            }
            .width('100%')
            .layoutWeight(1)
            .justifyContent(FlexAlign.Center)
          } else {
            Column({ space: 20 }) {
              Text('📝')
                .fontSize(64)

              Text('Markdown 预设')
                .fontSize(28)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)
                .fontFamily('serif')

              Text('通过 Markdown 创建自定义知识库。\n内置多个学科预设帮助你快速开始，\n也可以创建自己的专属预设。')
                .fontSize(16)
                .fontColor(KikariaColors.SOFT_TEXT)
                .textAlign(TextAlign.Center)
                .lineHeight(26)
                .padding({ left: 32, right: 32 })
            }
            .width('100%')
            .layoutWeight(1)
            .justifyContent(FlexAlign.Center)
          }
        }
        .width('100%')

        // Navigation buttons
        Row({ space: 16 }) {
          Button('上一步')
            .fontSize(15)
            .fontColor(KikariaColors.SOFT_TEXT)
            .backgroundColor(KikariaColors.MIST)
            .borderRadius(14)
            .height(44)
            .layoutWeight(1)
            .enabled(this.currentPage > 0)
            .onClick(() => {
              if (this.currentPage > 0) { this.currentPage-- }
            })

          if (this.currentPage < 2) {
            Button('下一步')
              .fontSize(15)
              .fontWeight(FontWeight.Medium)
              .fontColor('#FFFFFF')
              .linearGradient({
                angle: 135,
                colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
              })
              .borderRadius(14)
              .height(44)
              .layoutWeight(1)
              .onClick(() => { this.currentPage++ })
          } else {
            Button('开始使用')
              .fontSize(15)
              .fontWeight(FontWeight.Bold)
              .fontColor('#FFFFFF')
              .linearGradient({
                angle: 135,
                colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
              })
              .borderRadius(14)
              .height(44)
              .layoutWeight(1)
              .shadow({ radius: 10, color: KikariaColors.SHADOW_COLOR, offsetY: 4 })
              .onClick(() => {
                appState.completeOnboarding()
                navPathStack.pop()
              })
          }
        }
        .width('100%')
        .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
        .padding({ left: this.contentPadH, right: this.contentPadH, bottom: 24 })
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
struct OnboardingPage {
  build() {
    Navigation() {
      OnboardingPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
