/**
 * NewPresetPage.ets - Create a new preset from Markdown text or import from .md file.
 * Also supports clipboard export using pasteboard API.
 *
 * Translated from NewPresetView in source/Kikaria/ContentView.swift (line 5168)
 */
import { appState } from '../data/AppState'
import { parseMarkdown } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding, ipadPortraitFormPageTopInset } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'
import { picker } from '@kit.CoreFileKit'
import { fileIo } from '@kit.CoreFileKit'
import { pasteboard } from '@kit.BasicServicesKit'

@Builder
export function NewPresetPageBuilder(name: string, param: Object) {
  NewPresetPageContent()
}

@Component
export struct NewPresetPageContent {
  @State name: string = ''
  @State subtitle: string = ''
  @State description: string = ''
  @State category: string = '自定义'
  @State markdownText: string = ''
  @State parsedCount: number = 0
  @State errorMessage: string = ''
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
  }

  build() {
    NavDestination() {
      Column() {
        Row() {
          Button() {
            Text('取消')
              .fontSize(17)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => { navPathStack.pop() })

          Blank()

          Text('新建预设')
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.DEEP_TEXT)

          Blank()

          Button() {
            Text('创建')
              .fontSize(17)
              .fontWeight(FontWeight.Bold)
              .fontColor(this.canCreate ? KikariaColors.SKY : KikariaColors.TERTIARY_TEXT)
          }
          .backgroundColor(Color.Transparent)
          .enabled(this.canCreate)
          .onClick(() => { this.createPreset() })
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12 + ipadPortraitFormPageTopInset(), bottom: 12 })

        Scroll() {
          Column({ space: 16 }) {
            // Import / Export row
            Row({ space: 10 }) {
              Row() {
                Text('从文件导入 .md')
                  .fontSize(15)
                  .fontWeight(FontWeight.Medium)
                  .fontColor('#FFFFFF')
              }
              .height(44)
              .borderRadius(14)
              .padding({ left: 18, right: 18 })
              .linearGradient({
                angle: 135,
                colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
              })
              .onClick(() => { this.pickMarkdownFile() })
              .layoutWeight(1)

              if (this.markdownText.trim().length > 0) {
                Row() {
                  Text('复制导出')
                    .fontSize(15)
                    .fontWeight(FontWeight.Medium)
                    .fontColor(KikariaColors.SKY)
                }
                .height(44)
                .borderRadius(14)
                .padding({ left: 18, right: 18 })
                .backgroundColor(`${KikariaColors.SKY}10`)
                .border({ width: 1, color: `${KikariaColors.SKY}33` })
                .onClick(() => { this.exportMarkdown() })
                .layoutWeight(1)
              }
            }
            .width('100%')
            .justifyContent(FlexAlign.Start)

            // Form fields
            Column({ space: 6 }) {
              Text('预设名称')
                .fontSize(13)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.SOFT_TEXT)
                .width('100%')

              TextInput({ text: this.name, placeholder: '例如：高等数学' })
                .fontSize(16)
                .fontColor(KikariaColors.DEEP_TEXT)
                .backgroundColor(KikariaColors.CARD_BG)
                .borderRadius(12)
                .padding({ left: 16, right: 16, top: 12, bottom: 12 })
                .width('100%')
                .onChange((value: string) => {
                  this.name = value
                  this.updateParsedCount()
                })
            }
            .width('100%')

            Column({ space: 6 }) {
              Text('副标题')
                .fontSize(13)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.SOFT_TEXT)
                .width('100%')

              TextInput({ text: this.subtitle, placeholder: '简短描述' })
                .fontSize(16)
                .fontColor(KikariaColors.DEEP_TEXT)
                .backgroundColor(KikariaColors.CARD_BG)
                .borderRadius(12)
                .padding({ left: 16, right: 16, top: 12, bottom: 12 })
                .width('100%')
                .onChange((value: string) => { this.subtitle = value })
            }
            .width('100%')

            Column({ space: 6 }) {
              Text('描述')
                .fontSize(13)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.SOFT_TEXT)
                .width('100%')

              TextInput({ text: this.description, placeholder: '详细描述' })
                .fontSize(16)
                .fontColor(KikariaColors.DEEP_TEXT)
                .backgroundColor(KikariaColors.CARD_BG)
                .borderRadius(12)
                .padding({ left: 16, right: 16, top: 12, bottom: 12 })
                .width('100%')
                .onChange((value: string) => { this.description = value })
            }
            .width('100%')

            Column({ space: 6 }) {
              Text('分类')
                .fontSize(13)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.SOFT_TEXT)
                .width('100%')

              TextInput({ text: this.category, placeholder: '自定义' })
                .fontSize(16)
                .fontColor(KikariaColors.DEEP_TEXT)
                .backgroundColor(KikariaColors.CARD_BG)
                .borderRadius(12)
                .padding({ left: 16, right: 16, top: 12, bottom: 12 })
                .width('100%')
                .onChange((value: string) => { this.category = value })
            }
            .width('100%')

            // Markdown editor
            Column({ space: 6 }) {
              Row() {
                Text('Markdown 内容')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)

                Blank()

                Text(`已解析: ${this.parsedCount} 条`)
                  .fontSize(12)
                  .fontColor(this.parsedCount > 0 ? KikariaColors.MASTERED_GREEN : KikariaColors.SOFT_TEXT)
              }
              .width('100%')

              TextArea({ text: this.markdownText, placeholder: `# 知识点标题

tags: 标签1, 标签2

hint:
提示内容

content:
答案内容

---

# 下一个知识点
...` })
                .fontSize(14)
                .fontColor(KikariaColors.DEEP_TEXT)
                .backgroundColor(KikariaColors.CARD_BG)
                .borderRadius(12)
                .padding(12)
                .width('100%')
                .height(260)
                .onChange((value: string) => {
                  this.markdownText = value
                  this.updateParsedCount()
                })
            }
            .width('100%')

            Row() {
              Text('格式指南 →')
                .fontSize(14)
                .fontColor(KikariaColors.SKY)
            }
            .width('100%')
            .onClick(() => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined) })

            if (this.errorMessage.length > 0) {
              Text(this.errorMessage)
                .fontSize(13)
                .fontColor(KikariaColors.REMOVE_CORAL)
                .width('100%')
                .padding(12)
                .borderRadius(12)
                .backgroundColor(`${KikariaColors.REMOVE_CORAL}10`)
            }

            Row().height(40)
          }
          .width('100%')
        }
        .width('100%')
        .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
        .layoutWeight(1)
        .padding({ left: this.contentPadH, right: this.contentPadH })
        .scrollBar(BarState.Off)
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG)
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }

  pickMarkdownFile(): void {
    try {
      const documentPicker = new picker.DocumentViewPicker()
      const pickOptions = new picker.DocumentSelectOptions()
      pickOptions.maxSelectNumber = 1
      pickOptions.fileSuffixFilters = ['.md', '.txt', '.markdown']

      documentPicker.select(pickOptions).then((uris: string[]) => {
        if (uris.length === 0) { return }
        const uri: string = uris[0]
        try {
          const content: string = fileIo.readTextSync(uri)
          if (content.length > 0) {
            this.markdownText = content
            this.updateParsedCount()
            const lastSep: number = Math.max(uri.lastIndexOf('/'), uri.lastIndexOf('\\'))
            let filename: string = uri.substring(lastSep + 1)
            const dotIdx: number = filename.lastIndexOf('.')
            if (dotIdx > 0) { filename = filename.substring(0, dotIdx) }
            if (this.name.trim().length === 0) { this.name = filename }
          }
        } catch (ioErr) {
          this.errorMessage = '读取文件失败，请检查文件格式。'
        }
      })
    } catch (e) {
      this.errorMessage = '无法打开文件选择器，请重试。'
    }
  }

  exportMarkdown(): void {
    try {
      const pasteData = pasteboard.createData(pasteboard.MIMETYPE_TEXT_PLAIN, this.markdownText)
      pasteboard.getSystemPasteboard().setData(pasteData, () => {
        this.errorMessage = ''
        // show brief success via errorMessage as info
        this.errorMessage = `已复制 ${this.parsedCount} 条知识点到剪贴板`
        setTimeout(() => { if (this.errorMessage.indexOf('已复制') >= 0) { this.errorMessage = '' } }, 2000)
      })
    } catch (e) {
      this.errorMessage = '复制失败，请重试'
    }
  }

  get canCreate(): boolean {
    return this.name.trim().length > 0 && this.parsedCount > 0
  }

  updateParsedCount(): void {
    if (this.markdownText.trim().length === 0) {
      this.parsedCount = 0
      return
    }
    try {
      const points = parseMarkdown(this.markdownText)
      this.parsedCount = points.length
      this.errorMessage = points.length === 0 ? '未能解析到有效的知识点，请检查格式。' : ''
    } catch (e) {
      this.parsedCount = 0
      this.errorMessage = 'Markdown 格式有误，请检查。'
    }
  }

  createPreset(): void {
    if (!this.canCreate) { return }
    const preset = appState.createPreset(
      this.name.trim(),
      this.subtitle.trim(),
      this.description.trim() || this.subtitle.trim(),
      this.category.trim() || '自定义',
      this.markdownText
    )
    if (preset) {
      appState.switchToPreset(preset)
      navPathStack.pop()
    } else {
      this.errorMessage = '创建预设失败，请重试。'
    }
  }
}

@Entry
@Component
struct NewPresetPage {
  build() {
    Navigation() {
      NewPresetPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
