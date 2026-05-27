/**
 * MarkdownFormatGuidePage.ets - Documentation on Kikaria Markdown format.
 *
 * Translated from MarkdownFormatGuideView in source/Kikaria/ContentView.swift (line 5368)
 */
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

/**
 * NavDestination-compatible builder for NavPathStack migration.
 * Usage: navPathStack.pushPathByName('MarkdownFormatGuide', undefined)
 * Register in root Navigation's navDestination builder.
 */
@Builder
export function MarkdownFormatGuideBuilder(name: string, param: Object) {
  MarkdownFormatGuideContent()
}

/**
 * Reusable page content component (used by both @Entry and NavDestination).
 */
@Component
export struct MarkdownFormatGuideContent {
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
  }

  build() {
    NavDestination() {
      this.pageContent()
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }

  @Builder
  pageContent() {
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

          Text('Markdown 格式指南')
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.DEEP_TEXT)
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12, bottom: 12 })

        Text('Kikaria 使用 Markdown 格式存储知识点。每个知识点由以下部分组成：')
          .fontSize(15)
          .fontColor(KikariaColors.DEEP_TEXT)
          .width('100%')

        Column({ space: 8 }) {
          Text('基本结构')
            .fontSize(17).fontWeight(FontWeight.Bold)
            .fontColor(KikariaColors.DEEP_TEXT).width('100%')
          Text(`# 知识点标题\n\ntags: 标签1, 标签2\n\nhint:\n这里是提示内容。可以包含多行文字，\n支持 Markdown 基本语法。\n\ncontent:\n这里是答案内容。同样支持多行文字，\n以及 LaTeX 数学公式：$E = mc^2$。`)
            .fontSize(13).fontColor(KikariaColors.SOFT_TEXT)
            .backgroundColor(KikariaColors.MIST).borderRadius(12)
            .padding(16).width('100%').fontFamily('monospace')
        }.width('100%')

        Column({ space: 8 }) {
          Text('多个知识点')
            .fontSize(17).fontWeight(FontWeight.Bold)
            .fontColor(KikariaColors.DEEP_TEXT).width('100%')
          Text('使用三个短横线 --- 分隔多个知识点：')
            .fontSize(15).fontColor(KikariaColors.DEEP_TEXT).width('100%')
          Text(`# 第一个知识点\ntags: 标签A\n\nhint:\n提示A\n\ncontent:\n答案A\n\n---\n\n# 第二个知识点\ntags: 标签B\n\nhint:\n提示B\n\ncontent:\n答案B`)
            .fontSize(13).fontColor(KikariaColors.SOFT_TEXT)
            .backgroundColor(KikariaColors.MIST).borderRadius(12)
            .padding(16).width('100%').fontFamily('monospace')
        }.width('100%')

        Column({ space: 8 }) {
          Text('标签格式')
            .fontSize(17).fontWeight(FontWeight.Bold)
            .fontColor(KikariaColors.DEEP_TEXT).width('100%')
          Text('tags: 后跟逗号（中英文均可）分隔的标签列表。标签用于筛选和组织知识点。')
            .fontSize(15).fontColor(KikariaColors.DEEP_TEXT).width('100%')
        }.width('100%')

        Column({ space: 8 }) {
          Text('数学公式')
            .fontSize(17).fontWeight(FontWeight.Bold)
            .fontColor(KikariaColors.DEEP_TEXT).width('100%')
          Text('Kikaria 内嵌支持 LaTeX 数学公式：\n行内公式：$E = mc^2$\n块级公式：$$\\int_a^b f(x) dx$$\n\n(注：HarmonyOS 版本目前以文本形式展示公式)')
            .fontSize(15).fontColor(KikariaColors.DEEP_TEXT).width('100%')
        }.width('100%')

        Column({ space: 8 }) {
          Text('小贴士')
            .fontSize(17).fontWeight(FontWeight.Bold)
            .fontColor(KikariaColors.DEEP_TEXT).width('100%')
          Text('• 标题以 # 开头，后接知识点名称\n• hint: 和 content: 必须各占一行，以冒号结尾\n• 提示和答案的内容从下一行开始\n• 每个知识点至少需要标题、tags、hint 和 content\n• 标签有助于在首页按范围筛选\n• 使用 --- 分隔不同的知识点')
            .fontSize(15).fontColor(KikariaColors.DEEP_TEXT).width('100%')
        }.width('100%')

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
}

@Entry
@Component
struct MarkdownFormatGuidePage {
  build() {
    Navigation() {
      MarkdownFormatGuideContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
