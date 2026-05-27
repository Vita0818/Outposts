/**
 * EditKnowledgePointPage.ets - Edit or create a single knowledge point.
 * Uses LiquidGlass styling and adaptive colors throughout.
 *
 * Translated from EditKnowledgePointView in source/Kikaria/ContentView.swift (line 5951)
 */
import { appState } from '../data/AppState'
import { KnowledgePoint, generateId } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding } from '../components/KikariaAdaptiveLayout'
import { navPathStack, getRouteParams } from '../data/NavigationService'

@Builder
export function EditKnowledgePointPageBuilder(name: string, param: Object) {
  EditKnowledgePointPageContent()
}

@Component
export struct EditKnowledgePointPageContent {
  @State pointId: string = ''
  @State isNew: boolean = false
  @State title: string = ''
  @State tagsText: string = ''
  @State hint: string = ''
  @State content: string = ''
  @State toastMessage: string = ''
  @State toastVisible: boolean = false
  @State showDeleteConfirm: boolean = false
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    const params = getRouteParams()
    if (params && params['pointId']) {
      this.pointId = params['pointId'] as string
      this.isNew = false
      const point = appState.knowledgePoints.find(kp => kp.id === this.pointId)
      if (point) {
        this.title = point.title
        this.tagsText = point.tags.join(', ')
        this.hint = point.hint
        this.content = point.content
      }
    } else {
      this.isNew = true
      this.pointId = generateId()
    }
  }

  showToast(msg: string): void {
    this.toastMessage = msg
    this.toastVisible = true
    setTimeout(() => { this.toastVisible = false }, 2000)
  }

  savePoint(): void {
    if (this.title.trim().length === 0) {
      this.showToast('标题不能为空')
      return
    }
    const tags = this.tagsText.split(/[,，]/).map(t => t.trim()).filter(t => t.length > 0)
    const now = Date.now()
    const point = new KnowledgePoint(
      this.pointId, this.title.trim(), tags, this.hint.trim(), this.content.trim(),
      false, false, now, now, 0, null
    )
    appState.upsertKnowledgePoint(point)
    this.showToast(this.isNew ? '知识点已创建' : '知识点已更新')
    setTimeout(() => { navPathStack.pop() }, 400)
  }

  build() {
    NavDestination() {
      Stack() {
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

            Text(this.isNew ? '新建知识点' : '编辑知识点')
              .fontSize(17)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)

            Blank()

            Button() {
              Text('保存')
                .fontSize(15)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.SKY)
            }
            .backgroundColor(Color.Transparent)
            .onClick(() => { this.savePoint() })
          }
          .width('100%')
          .padding({ left: 20, right: 20, top: 12, bottom: 12 })

          Scroll() {
            Column({ space: 14 }) {
              // Title
              Column({ space: 6 }) {
                Text('标题')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .width('100%')
                  .padding({ left: 4 })

                TextInput({ text: this.title, placeholder: '知识点标题，以 # 开头' })
                  .fontSize(17)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .backgroundColor(KikariaColors.CARD_BG)
                  .borderRadius(14)
                  .padding(16)
                  .onChange((value: string) => { this.title = value })
              }
              .width('100%')

              // Tags
              Column({ space: 6 }) {
                Text('标签（逗号分隔）')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .width('100%')
                  .padding({ left: 4 })

                TextInput({ text: this.tagsText, placeholder: '例如: 数学, 微积分, 公式' })
                  .fontSize(15)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .backgroundColor(KikariaColors.CARD_BG)
                  .borderRadius(14)
                  .padding(16)
                  .onChange((value: string) => { this.tagsText = value })
              }
              .width('100%')

              // Hint
              Column({ space: 6 }) {
                Text('提示')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SKY)
                  .width('100%')
                  .padding({ left: 4 })

                TextArea({ text: this.hint, placeholder: '提示文本（可选）' })
                  .fontSize(15)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .backgroundColor(KikariaColors.CARD_BG)
                  .borderRadius(14)
                  .padding(16)
                  .height(100)
                  .onChange((value: string) => { this.hint = value })
              }
              .width('100%')

              // Content
              Column({ space: 6 }) {
                Text('答案')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.MASTERED_GREEN)
                  .width('100%')
                  .padding({ left: 4 })

                TextArea({ text: this.content, placeholder: '答案/内容（支持 Markdown 和 LaTeX）' })
                  .fontSize(15)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .backgroundColor(KikariaColors.CARD_BG)
                  .borderRadius(14)
                  .padding(16)
                  .height(160)
                  .onChange((value: string) => { this.content = value })
              }
              .width('100%')

              // Delete button (only for existing)
              if (!this.isNew) {
                Button('删除此知识点')
                  .fontSize(15)
                  .fontWeight(FontWeight.Medium)
                  .fontColor('#FFFFFF')
                  .width('100%')
                  .height(48)
                  .borderRadius(14)
                  .linearGradient({
                    angle: 135,
                    colors: [[KikariaColors.REMOVE_GRADIENT_START, 0], [KikariaColors.REMOVE_GRADIENT_END, 1]]
                  })
                  .margin({ top: 12 })
                  .onClick(() => { this.showDeleteConfirm = true })
              }

              Row().height(40)
            }
            .width('100%')
            .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
            .padding({ left: this.contentPadH, right: this.contentPadH })
          }
          .width('100%')
          .layoutWeight(1)
          .scrollBar(BarState.Off)
        }
        .width('100%')
        .height('100%')
        .backgroundColor(KikariaColors.PAGE_BG)

        // Delete confirmation
        if (this.showDeleteConfirm) {
          Column() {
            Column({ space: 16 }) {
              Text('删除知识点')
                .fontSize(17)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)

              Text('确定要删除此知识点吗？此操作不可撤销。')
                .fontSize(14)
                .fontColor(KikariaColors.SOFT_TEXT)
                .textAlign(TextAlign.Center)

              Row({ space: 12 }) {
                Button('取消')
                  .fontSize(15).fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .backgroundColor(KikariaColors.MIST)
                  .borderRadius(12).height(42).layoutWeight(1)
                  .onClick(() => { this.showDeleteConfirm = false })

                Button('确认删除')
                  .fontSize(15).fontWeight(FontWeight.Bold)
                  .fontColor('#FFFFFF')
                  .linearGradient({
                    angle: 135,
                    colors: [[KikariaColors.REMOVE_GRADIENT_START, 0], [KikariaColors.REMOVE_GRADIENT_END, 1]]
                  })
                  .borderRadius(12).height(42).layoutWeight(1)
                  .onClick(() => {
                    appState.deleteKnowledgePoint(this.pointId)
                    this.showDeleteConfirm = false
                    this.showToast('知识点已删除')
                    setTimeout(() => { navPathStack.pop() }, 400)
                  })
              }
              .width('100%')
            }
            .width('80%')
            .padding(24)
            .borderRadius(20)
            .backgroundColor(KikariaColors.CARD_BG)
            .shadow({ radius: 24, color: KikariaColors.SHADOW_COLOR, offsetY: 8 })
          }
          .width('100%')
          .height('100%')
          .justifyContent(FlexAlign.Center)
          .backgroundColor(KikariaColors.OVERLAY_BG)
          .onClick(() => { this.showDeleteConfirm = false })
        }

        // Toast
        if (this.toastVisible) {
          Row() {
            Text(this.toastMessage)
              .fontSize(15)
              .fontColor('#FFFFFF')
              .padding({ left: 20, right: 20, top: 12, bottom: 12 })
          }
          .borderRadius(20)
          .backgroundColor(`${KikariaColors.DEEP_TEXT}E6`)
          .position({ top: 80, left: '10%' })
          .width('80%')
          .justifyContent(FlexAlign.Center)
        }
      }
      .width('100%')
      .height('100%')
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }
}

@Entry
@Component
struct EditKnowledgePointPage {
  build() {
    Navigation() {
      EditKnowledgePointPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
