/**
 * EditPresetPage.ets - Edit preset metadata, manage knowledge points, export, delete.
 *
 * Translated from EditPresetView in source/Kikaria/ContentView.swift (line 5691)
 */
import { pasteboard } from '@kit.BasicServicesKit'
import { appState } from '../data/AppState'
import { KnowledgePreset, KnowledgePoint, markdownTextFromPoints } from '../model/KnowledgePoint'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding, ipadPortraitFormPageTopInset } from '../components/KikariaAdaptiveLayout'
import { navPathStack, RouteParams } from '../data/NavigationService'

@Builder
export function EditPresetPageBuilder(name: string, param: Object) {
  EditPresetPageContent()
}

@Component
export struct EditPresetPageContent {
  @State presetId: string = ''
  @State presetName: string = ''
  @State presetSubtitle: string = ''
  @State presetDescription: string = ''
  @State knowledgePoints: KnowledgePoint[] = []
  @State searchQuery: string = ''
  @State isEditingName: boolean = false
  @State editNameText: string = ''
  @State editDescText: string = ''
  @State showDeletePresetConfirm: boolean = false
  @State showDeletePointId: string = ''
  @State toastMessage: string = ''
  @State toastVisible: boolean = false
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.presetId = appState.currentPresetID
    this.refreshState()
  }

  onPageShow(): void {
    this.refreshState()
  }

  refreshState(): void {
    const preset = appState.presets.find(p => p.id === this.presetId)
    if (preset) {
      this.presetName = preset.name
      this.presetSubtitle = preset.subtitle
      this.presetDescription = preset.description
      this.editNameText = preset.name
      this.editDescText = preset.description
    }
    this.knowledgePoints = [...appState.knowledgePoints]
  }

  get filteredPoints(): KnowledgePoint[] {
    if (this.searchQuery.length === 0) {
      return this.knowledgePoints
    }
    const q = this.searchQuery.toLowerCase()
    return this.knowledgePoints.filter(kp =>
      kp.title.toLowerCase().indexOf(q) >= 0 ||
      kp.tags.some(t => t.toLowerCase().indexOf(q) >= 0)
    )
  }

  showToast(msg: string): void {
    this.toastMessage = msg
    this.toastVisible = true
    setTimeout(() => { this.toastVisible = false }, 2000)
  }

  build() {
    NavDestination() {
      Stack() {
        Column() {
          // Top bar
          Row() {
            Button() {
              Text('← 返回')
                .fontSize(17)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
            .backgroundColor(Color.Transparent)
            .onClick(() => { navPathStack.pop() })

            Blank()

            Text('编辑预设')
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
            .onClick(() => {
              appState.updatePresetMetadata(this.presetId, this.editNameText,
                this.presetSubtitle, this.editDescText)
              this.presetName = this.editNameText
              this.presetDescription = this.editDescText
              this.isEditingName = false
              this.showToast('预设信息已保存')
            })
          }
          .width('100%')
          .padding({ left: 20, right: 20, top: 12 + ipadPortraitFormPageTopInset(), bottom: 12 })

          Scroll() {
            Column({ space: 14 }) {
              // Preset name
              Column({ space: 6 }) {
                Text('预设名称')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .width('100%')
                  .padding({ left: 4 })

                TextInput({ text: this.editNameText, placeholder: '输入预设名称' })
                  .fontSize(17)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .backgroundColor(KikariaColors.CARD_BG)
                  .borderRadius(14)
                  .padding(16)
                  .onChange((value: string) => {
                    this.editNameText = value
                  })
              }
              .width('100%')

              // Preset description
              Column({ space: 6 }) {
                Text('描述')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .width('100%')
                  .padding({ left: 4 })

                TextInput({ text: this.editDescText, placeholder: '输入预设描述' })
                  .fontSize(15)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .backgroundColor(KikariaColors.CARD_BG)
                  .borderRadius(14)
                  .padding(16)
                  .maxLength(200)
                  .onChange((value: string) => {
                    this.editDescText = value
                  })
              }
              .width('100%')

              // Knowledge points header with search
              Row() {
                Text(`知识点 (${this.knowledgePoints.length})`)
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)

                Blank()

                if (this.knowledgePoints.length > 0) {
                  Button('复制导出')
                    .fontSize(13)
                    .fontWeight(FontWeight.Medium)
                    .fontColor(KikariaColors.SKY)
                    .backgroundColor(Color.Transparent)
                    .onClick(() => {
                      const md = markdownTextFromPoints(this.knowledgePoints)
                      try {
                        const pasteData = pasteboard.createData(pasteboard.MIMETYPE_TEXT_PLAIN, md)
                        pasteboard.getSystemPasteboard().setData(pasteData, () => {
                          this.showToast(`已复制 ${this.knowledgePoints.length} 条知识点到剪贴板`)
                        })
                      } catch (e) {
                        this.showToast('复制失败，请重试')
                      }
                    })
                }
              }
              .width('100%')
              .padding({ left: 4, right: 4 })

              // Search bar
              Row() {
                Text('🔍')
                  .fontSize(14)
                TextInput({ text: this.searchQuery, placeholder: '搜索知识点标题或标签...' })
                  .fontSize(15)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .layoutWeight(1)
                  .backgroundColor(Color.Transparent)
                  .onChange((value: string) => {
                    this.searchQuery = value
                  })
                if (this.searchQuery.length > 0) {
                  Text('✕')
                    .fontSize(14)
                    .fontColor(KikariaColors.TERTIARY_TEXT)
                    .onClick(() => { this.searchQuery = '' })
                }
              }
              .width('100%')
              .padding(14)
              .borderRadius(14)
              .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
              .backdropBlur(12)

              // Create new knowledge point button
              Row() {
                Text('+ 新建知识点')
                  .fontSize(15)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SKY)
              }
              .width('100%')
              .height(48)
              .justifyContent(FlexAlign.Center)
              .borderRadius(14)
              .backgroundColor(`${KikariaColors.SKY}10`)
              .border({ width: 1, color: `${KikariaColors.SKY}33`, style: BorderStyle.Dashed })
              .onClick(() => {
                navPathStack.pushPathByName('EditKnowledgePointPage', undefined)
              })

              // Knowledge point list
              if (this.filteredPoints.length === 0) {
                Column({ space: 8 }) {
                  Text(this.searchQuery.length > 0 ? '没有匹配的知识点' : '暂无知识点')
                    .fontSize(15)
                    .fontColor(KikariaColors.SOFT_TEXT)
                }
                .width('100%')
                .padding(30)
                .justifyContent(FlexAlign.Center)
                .borderRadius(16)
                .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
                .backdropBlur(12)
              } else {
                ForEach(this.filteredPoints, (kp: KnowledgePoint) => {
                  Column({ space: 8 }) {
                    Row({ space: 8 }) {
                      Column({ space: 6 }) {
                        Row({ space: 8 }) {
                          Text(kp.title)
                            .fontSize(15)
                            .fontWeight(FontWeight.Medium)
                            .fontColor(KikariaColors.DEEP_TEXT)
                            .maxLines(2)
                            .textOverflow({ overflow: TextOverflow.Ellipsis })
                            .layoutWeight(1)

                          if (kp.isMastered) {
                            Text('✓')
                              .fontSize(12)
                              .fontWeight(FontWeight.Bold)
                              .fontColor(KikariaColors.MASTERED_GREEN)
                          }
                          if (kp.isReinforced) {
                            Text('★')
                              .fontSize(12)
                              .fontColor(KikariaColors.NEXT_AMBER)
                          }
                        }

                        if (kp.tags.length > 0) {
                          Row({ space: 6 }) {
                            ForEach(kp.tags.slice(0, 4), (tag: string) => {
                              Text(tag)
                                .fontSize(10)
                                .fontColor(KikariaColors.SOFT_TEXT)
                                .backgroundColor(KikariaColors.MIST)
                                .borderRadius(6)
                                .padding({ left: 8, right: 8, top: 2, bottom: 2 })
                            })
                          }
                        }
                      }
                      .layoutWeight(1)
                      .alignItems(HorizontalAlign.Start)

                      Button() {
                        Text('✕')
                          .fontSize(14)
                          .fontColor(KikariaColors.REMOVE_CORAL)
                      }
                      .width(32)
                      .height(32)
                      .borderRadius(16)
                      .backgroundColor(`${KikariaColors.REMOVE_CORAL}10`)
                      .onClick(() => {
                        appState.deleteKnowledgePoint(kp.id)
                        this.refreshState()
                        this.showToast(`已删除: ${kp.title}`)
                      })
                    }
                    .width('100%')
                    .onClick(() => {
                      navPathStack.pushPathByName('EditKnowledgePointPage', new RouteParams(undefined, kp.id))
                    })

                    Divider()
                      .strokeWidth(0.5)
                      .color(KikariaColors.MIST)
                  }
                  .width('100%')
                  .padding(14)
                  .borderRadius(14)
                  .backgroundColor(KikariaColors.CARD_BG)
                  .shadow({ radius: 6, color: KikariaColors.SHADOW_COLOR, offsetY: 2 })
                  .margin({ bottom: 8 })
                })
              }

              // Delete preset button
              if (!appState.currentPreset.isBuiltIn && appState.presets.length > 1) {
                Column() {
                  Button('删除此预设')
                    .fontSize(15)
                    .fontWeight(FontWeight.Medium)
                    .fontColor('#FFFFFF')
                    .width('100%')
                    .height(48)
                    .borderRadius(14)
                    .backgroundColor(KikariaColors.REMOVE_CORAL)
                    .margin({ top: 16 })
                    .onClick(() => {
                      this.showDeletePresetConfirm = true
                    })
                }
                .width('100%')
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

        // Delete preset confirmation dialog
        if (this.showDeletePresetConfirm) {
          Column() {
            Column({ space: 16 }) {
              Text('删除预设')
                .fontSize(17)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)

              Text(`确定要删除「${this.presetName}」吗？此操作不可撤销，所有该预设下的学习进度将丢失。`)
                .fontSize(14)
                .fontColor(KikariaColors.SOFT_TEXT)
                .textAlign(TextAlign.Center)

              Row({ space: 12 }) {
                Button('取消')
                  .fontSize(15)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .backgroundColor(KikariaColors.MIST)
                  .borderRadius(12)
                  .height(42)
                  .layoutWeight(1)
                  .onClick(() => {
                    this.showDeletePresetConfirm = false
                  })

                Button('确认删除')
                  .fontSize(15)
                  .fontWeight(FontWeight.Bold)
                  .fontColor('#FFFFFF')
                  .backgroundColor(KikariaColors.REMOVE_CORAL)
                  .borderRadius(12)
                  .height(42)
                  .layoutWeight(1)
                  .onClick(() => {
                    const success = appState.deletePreset(this.presetId)
                    this.showDeletePresetConfirm = false
                    if (success) {
                      navPathStack.pop()
                    } else {
                      this.showToast('无法删除最后一个预设')
                    }
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
          .onClick(() => {
            this.showDeletePresetConfirm = false
          })
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
struct EditPresetPage {
  build() {
    Navigation() {
      EditPresetPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
