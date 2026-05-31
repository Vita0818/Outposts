/**
 * Recording session page - mirrors RecordingSessionView.swift
 */
import { ConfigurationConstant } from '@kit.AbilityKit'
import { getSharedRecordingManager, RecordingManager } from '../services/RecordingManager'
import { RecordingState, StudyFilingPath, filingLevelTitle, RecordingMetadata } from '../models/RecordingModels'
import { formatClock, formatDuration } from '../utils/FormatHelpers'
import { colorAlpha,  RokuricsColors, FontWeight, applyColorMode } from '../utils/RokuricsTheme'
import { StudyFolderStore } from '../services/StudyFolderStore'
import { PlayIcon, PauseIcon, StopIcon } from '../utils/CustomIcons'

const DEFAULT_TYPE_OPTIONS: string[] = ['课堂录音', '自学笔记', '会议记录', '访谈采访', '灵感记录', '其他']

@Entry
@Component
struct RecordingSessionPage {
  private recordingManager: RecordingManager = getSharedRecordingManager()
  private folderStore: StudyFolderStore = new StudyFolderStore(getContext(this))
  @StorageLink('colorMode') colorMode: ConfigurationConstant.ColorMode = ConfigurationConstant.ColorMode.COLOR_MODE_NOT_SET
  @State state: RecordingState = RecordingState.IDLE
  @State elapsedSeconds: number = 0
  @State statusText: string = '准备中'
  @State suggestedTitle: string = ''
  @State pendingTitle: string = ''
  @State filingType: string = ''
  @State filingSubject: string = ''
  @State filingChapter: string = ''
  @State filingTopic: string = ''
  @State showFilingPicker: boolean = false
  @State isPausedBlinking: boolean = false
  @State pulsePhase: number = 0
  @State filingTypeOptions: string[] = [...DEFAULT_TYPE_OPTIONS]
  @State filingSubjectOptions: string[] = []

  aboutToAppear(): void {
    this.extractFilingCandidates()
    this.recordingManager.onStateChange((s: RecordingState) => {
      this.state = s
      this.elapsedSeconds = this.recordingManager.elapsedSeconds
      this.statusText = this.recordingManager.statusMessage
      if (s === RecordingState.PAUSED) {
        this.isPausedBlinking = true
      } else {
        this.isPausedBlinking = false
      }
    })
    this.state = this.recordingManager.state
    this.elapsedSeconds = this.recordingManager.elapsedSeconds
    this.statusText = this.recordingManager.statusMessage
    this.suggestedTitle = this.recordingManager.suggestedTitle
    this.pendingTitle = this.recordingManager.pendingTitle ?? ''

    // Auto-start recording if idle
    if (this.state === RecordingState.IDLE) {
      this.recordingManager.startRecording()
    }

    // Simple pulse for recording indicator
    const updatePulse = () => {
      const t = Date.now() / 1000
      this.pulsePhase = Math.sin(t * Math.PI) * 0.5 + 0.5
      if (this.state === RecordingState.RECORDING || this.state === RecordingState.PAUSED) {
        setTimeout(updatePulse, 50)
      }
    }
    updatePulse()
  }

  private getFilingPath(): StudyFilingPath {
    return new StudyFilingPath(
      this.filingType || undefined,
      this.filingSubject || undefined,
      this.filingChapter || undefined,
      this.filingTopic || undefined
    )
  }

  private async extractFilingCandidates(): Promise<void> {
    const recordings = this.recordingManager.recordings
    const typeSet = new Set<string>(DEFAULT_TYPE_OPTIONS)
    const subjectSet = new Set<string>()

    for (const r of recordings) {
      const filing = r.studyFiling
      if (filing) {
        if (filing.type) typeSet.add(filing.type)
        if (filing.subject) subjectSet.add(filing.subject)
      }
    }

    const folders = await this.folderStore.listFolders()
    for (const f of folders) {
      if (f.level === 'type') typeSet.add(f.name)
      if (f.level === 'subject') subjectSet.add(f.name)
    }

    this.filingTypeOptions = Array.from(typeSet).sort()
    this.filingSubjectOptions = Array.from(subjectSet).sort()
  }

  build() {
    applyColorMode(this.colorMode)
    Stack() {
      // Ambient background
      this.SessionBackground()

      Column() {
        // Header
        Row() {
          // Glass circle back button
          Button() {
            Text('←')
              .fontSize(18)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
          }
          .width(44)
          .height(44)
          .borderRadius(22)
          .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'))
          .shadow({
            color: colorAlpha(RokuricsColors.shadowColor, '10'),
            radius: 12,
            offsetY: 6
          })
          .border({
            width: 1,
            color: {
              colors: [
                [0xFFFFFF, 0.22],
                [RokuricsColors.glassStroke, 0.14],
                [RokuricsColors.aqua, 0.12]
              ],
              direction: GradientDirection.RightBottom
            },
            radius: 22
          } as BorderOptions)
          .onClick(() => this.getUIContext().getRouter().back())

          Blank()

          // Stop / Save button
          if (this.state === RecordingState.RECORDING || this.state === RecordingState.PAUSED) {
            Button() {
              Text('停止')
                .fontSize(16)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(Color.White)
            }
            .padding({ left: 20, right: 20, top: 10, bottom: 10 })
            .borderRadius(20)
            .backgroundColor(RokuricsColors.coral)
            .shadow({
              color: colorAlpha(RokuricsColors.coral, '30'),
              radius: 12,
              offsetY: 4
            })
            .onClick(() => {
              this.recordingManager.stopRecording()
            })
          } else if (this.state === RecordingState.FILING) {
            Button() {
              Text('保存')
                .fontSize(16)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(Color.White)
            }
            .padding({ left: 20, right: 20, top: 10, bottom: 10 })
            .borderRadius(20)
            .backgroundColor(RokuricsColors.aqua)
            .shadow({
              color: colorAlpha(RokuricsColors.aqua, '30'),
              radius: 12,
              offsetY: 4
            })
            .onClick(() => {
              const filing = this.getFilingPath()
              this.recordingManager.finalizeRecording(
                this.pendingTitle || this.suggestedTitle,
                filing.isEmpty ? null : filing
              )
              this.getUIContext().getRouter().back()
            })
          }
        }
        .width('100%')
        .padding({ left: 16, right: 16, top: 56 })

        Blank()

        // Timer card with glass styling (mirrors Apple's liquid glass card)
        Column({ space: 16 }) {
          Text(formatClock(this.elapsedSeconds))
            .fontSize(78)
            .fontWeight(FontWeight.Bold)
            .fontFamily('serif')
            .fontColor(this.getTimerColor())
            .maxLines(1)
            .opacity(this.isPausedBlinking ? (0.4 + this.pulsePhase * 0.6) : 1.0)

          Text(this.statusText)
            .fontSize(15)
            .fontWeight(FontWeight.Medium)
            .fontColor(RokuricsColors.softText)

          // Recording indicator dot
          if (this.state === RecordingState.RECORDING) {
            Row({ space: 6 }) {
              Circle()
                .width(8)
                .height(8)
                .fill(RokuricsColors.coral)
                .opacity(0.7 + this.pulsePhase * 0.3)
              Text('录制中')
                .fontSize(12)
                .fontColor(RokuricsColors.coral)
            }
          }

          if (this.state === RecordingState.FAILED) {
            Text(this.recordingManager.lastError ?? '录音失败')
              .fontSize(13)
              .fontColor(RokuricsColors.coral)
              .textAlign(TextAlign.Center)
              .maxLines(2)
              .padding({ left: 16, right: 16 })
          }
        }
        .width('85%')
        .padding({ top: 36, bottom: 36, left: 18, right: 18 })
        .borderRadius(34)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'))
        .shadow({
          color: colorAlpha(RokuricsColors.shadowColor, '12'),
          radius: 24,
          offsetY: 14
        })
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, 0.22],
              [RokuricsColors.glassStroke, 0.18],
              [RokuricsColors.glassStrokeAccent, 0.14]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 34
        } as BorderOptions)

        Blank()

        // Control buttons (glass cards)
        if (this.state === RecordingState.RECORDING || this.state === RecordingState.PAUSED) {
          Row({ space: 12 }) {
            // Pause/Resume button
            Button() {
              Column({ space: 8 }) {
                if (this.state === RecordingState.PAUSED) {
                  PlayIcon(22, RokuricsColors.softTeal)
                } else {
                  PauseIcon(22, RokuricsColors.softTeal)
                }
                Text(this.state === RecordingState.PAUSED ? '继续' : '暂停')
                  .fontSize(13)
                  .fontWeight(FontWeight.SemiBold)
              }
              .foregroundColor(RokuricsColors.softTeal)
              .width('100%')
              .height(76)
              .justifyContent(FlexAlign.Center)
            }
            .layoutWeight(1)
            .height(76)
            .borderRadius(24)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '61'))
            .shadow({
              color: colorAlpha(RokuricsColors.shadowColor, '08'),
              radius: 12,
              offsetY: 6
            })
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, 0.18],
                  [RokuricsColors.glassStroke, 0.14],
                  [RokuricsColors.glassStrokeAccent, 0.10]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 24
            } as BorderOptions)
            .onClick(() => this.recordingManager.toggleRecording())
          }
          .width('100%')
          .padding({ left: 22, right: 22, bottom: 36 })
        }

      }
      .width('100%')
      .height('100%')

      // Filing UI overlay
      if (this.state === RecordingState.FILING) {
        this.FilingOverlay()
      }

      // Saved state
      if (this.state === RecordingState.SAVED) {
        Column({ space: 12 }) {
          Text('已保存')
            .fontSize(24)
            .fontWeight(FontWeight.Bold)
            .fontColor(RokuricsColors.aqua)
          Text(`时长：${formatDuration(this.elapsedSeconds)}`)
            .fontSize(14)
            .fontColor(RokuricsColors.softText)
          Button() {
            Text('返回首页')
              .fontWeight(FontWeight.Medium)
              .fontColor(RokuricsColors.aqua)
          }
          .margin({ top: 20 })
          .onClick(() => {
            this.getUIContext().getRouter().back()
          })
        }
      }

      // Failed state
      if (this.state === RecordingState.FAILED) {
        Column({ space: 12 }) {
          Text('录音失败')
            .fontSize(18)
            .fontColor(RokuricsColors.coral)
          Text(this.recordingManager.lastError ?? '')
            .fontSize(13)
            .fontColor(RokuricsColors.softText)
          Button() {
            Text('返回')
              .fontColor(RokuricsColors.aqua)
          }
          .margin({ top: 16 })
          .onClick(() => this.getUIContext().getRouter().back())
        }
      }
    }
    .width('100%')
    .height('100%')
    .linearGradient({
      direction: GradientDirection.RightBottom,
      colors: [
        [RokuricsColors.pageGradientStart, 1.0],
        [RokuricsColors.pageGradientMid, 1.0],
        [RokuricsColors.pageGradientEnd, 1.0]
      ]
    })
  }

  @Builder
  FilingOverlay() {
    Stack() {
      // Semi-transparent backdrop
      Column()
        .width('100%')
        .height('100%')
        .backgroundColor('#00000028')
        .onClick(() => {})

      // Filing card overlay
      Scroll() {
        Column({ space: 16 }) {
          // Header
          Text('录音归档')
            .fontSize(30)
            .fontWeight(FontWeight.Bold)
            .fontColor(RokuricsColors.deepText)

          Text('选择门类→课程→章节→主题，或直接保存')
            .fontSize(14)
            .fontColor(RokuricsColors.softText)

          // Title input
          Column({ space: 8 }) {
            Text('录音标题')
              .fontSize(13)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.softText)
            TextInput({
              text: this.pendingTitle || this.suggestedTitle,
              placeholder: '输入录音标题'
            })
              .fontSize(20)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
              .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'))
              .borderRadius(12)
              .padding(16)
              .width('100%')
              .onChange((value: string) => {
                this.pendingTitle = value
                this.recordingManager.updatePendingTitle(value)
              })
          }

          // Filing level buttons
          Row({ space: 8 }) {
            this.LevelButton('门类', this.filingType, true)
            this.LevelButton('课程', this.filingSubject, this.filingType.length > 0)
            this.LevelButton('章节', this.filingChapter, this.filingSubject.length > 0)
            this.LevelButton('主题', this.filingTopic, this.filingChapter.length > 0)
          }

          // Show options for active level
          if (this.filingSubject.length > 0 && this.filingChapter.length > 0) {
            Text('主题 (自由输入)')
              .fontSize(12)
              .fontColor(RokuricsColors.softText)
            TextInput({ text: this.filingTopic, placeholder: '输入主题名' })
              .fontSize(14)
              .fontColor(RokuricsColors.deepText)
              .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'))
              .borderRadius(8)
              .padding({ left: 12, right: 12, top: 8, bottom: 8 })
              .onChange((value: string) => { this.filingTopic = value })
          } else if (this.filingType.length > 0 && this.filingSubject.length > 0) {
            Text('章节 (自由输入)')
              .fontSize(12)
              .fontColor(RokuricsColors.softText)
            TextInput({ text: this.filingChapter, placeholder: '输入章节名' })
              .fontSize(14)
              .fontColor(RokuricsColors.deepText)
              .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'))
              .borderRadius(8)
              .padding({ left: 12, right: 12, top: 8, bottom: 8 })
              .onChange((value: string) => { this.filingChapter = value })
          } else if (this.filingType.length > 0) {
            // Subject options
            Row({ space: 6 }) {
              ForEach(this.filingSubjectOptions, (option: string) => {
                Text(option)
                  .fontSize(12)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(this.filingSubject === option ? Color.White : RokuricsColors.softText)
                  .padding({ left: 10, right: 10, top: 6, bottom: 6 })
                  .borderRadius(12)
                  .backgroundColor(this.filingSubject === option ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'))
                  .onClick(() => {
                    if (this.filingSubject === option) {
                      this.filingSubject = ''
                    } else {
                      this.filingSubject = option
                    }
                  })
              })
            }
            .flexShrink(0)
          } else {
            // Type options
            Row({ space: 6 }) {
              ForEach(this.filingTypeOptions, (option: string) => {
                Text(option)
                  .fontSize(12)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(this.filingType === option ? Color.White : RokuricsColors.softText)
                  .padding({ left: 10, right: 10, top: 6, bottom: 6 })
                  .borderRadius(12)
                  .backgroundColor(this.filingType === option ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'))
                  .onClick(() => {
                    if (this.filingType === option) {
                      this.filingType = ''
                    } else {
                      this.filingType = option
                    }
                  })
              })
            }
            .flexShrink(0)
          }

          // Filing summary
          if (this.filingType || this.filingSubject || this.filingChapter || this.filingTopic) {
            Text(`归档路径：${this.getFilingPath().displaySummary}`)
              .fontSize(12)
              .fontColor(RokuricsColors.aqua)
              .margin({ top: 4 })
          }

          // Action buttons
          Row({ space: 12 }) {
            Button() {
              Text('直接保存')
                .fontSize(15)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.softText)
            }
            .layoutWeight(1)
            .height(48)
            .borderRadius(24)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '4D'))
            .border({ width: 1, color: colorAlpha(RokuricsColors.softText, '20'), radius: 24 })
            .onClick(() => {
              this.recordingManager.finalizeRecordingDirectSave()
              this.getUIContext().getRouter().back()
            })

            Button() {
              Text('保存归档')
                .fontSize(15)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(Color.White)
            }
            .layoutWeight(1)
            .height(48)
            .borderRadius(24)
            .backgroundColor(
              (this.filingType || this.filingSubject || this.filingChapter || this.filingTopic) ?
                RokuricsColors.aqua : RokuricsColors.tertiaryText
            )
            .enabled(!!(this.filingType || this.filingSubject || this.filingChapter || this.filingTopic))
            .shadow({
              color: colorAlpha(RokuricsColors.shadowColor, '16'),
              radius: 14,
              offsetY: 8
            })
            .onClick(() => {
              const filing = this.getFilingPath()
              this.recordingManager.finalizeRecording(
                this.pendingTitle || this.suggestedTitle,
                filing.isEmpty ? null : filing
              )
              this.getUIContext().getRouter().back()
            })
          }
          .width('100%')
        }
        .width('100%')
        .padding(22)
        .borderRadius(30)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'F5'))
        .shadow({
          color: colorAlpha(RokuricsColors.shadowColor, '1C'),
          radius: 26,
          offsetY: 14
        })
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, 0.24],
              [RokuricsColors.glassStroke, 0.20],
              [RokuricsColors.glassStrokeAccent, 0.16]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 30
        } as BorderOptions)
        .constraintSize({ maxWidth: 380 })
      }
      .width('100%')
      .height('100%')
      .padding(24)
      .constraintSize({ maxWidth: 420 })
    }
    .width('100%')
    .height('100%')
    .zIndex(10)
    .position({ x: 0, y: 0 })
  }

  @Builder
  LevelButton(label: string, value: string, enabled: boolean) {
    Column({ space: 3 }) {
      Text(label)
        .fontSize(9)
        .fontWeight(FontWeight.Bold)
        .fontColor(RokuricsColors.tertiaryText)
      Text(value || '—')
        .fontSize(11)
        .fontWeight(FontWeight.SemiBold)
        .fontColor(enabled ? RokuricsColors.deepText : RokuricsColors.tertiaryText)
        .maxLines(1)
        .textOverflow({ overflow: TextOverflow.Ellipsis })
        .constraintSize({ maxWidth: 56 })
    }
    .padding({ left: 8, right: 8, top: 6, bottom: 6 })
    .constraintSize({ minWidth: 64 })
    .borderRadius(13)
    .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '56'))
    .border({
      width: 1,
      color: enabled ? colorAlpha(RokuricsColors.aqua, '46') : colorAlpha(RokuricsColors.softText, '18'),
      radius: 13
    })
    .opacity(enabled ? 1 : 0.46)
  }

  @Builder
  SessionBackground() {
    Stack() {
      Circle()
        .width(220)
        .height(220)
        .fill(colorAlpha(RokuricsColors.mint, '20'))
        .position({ x: -50, y: -150 })

      Circle()
        .width(260)
        .height(260)
        .fill(colorAlpha(RokuricsColors.skyCyan, '16'))
        .position({ x: '85%', y: '75%' })
    }
    .width('100%')
    .height('100%')
  }

  private getTimerColor(): string {
    switch (this.state) {
      case RecordingState.RECORDING:
        return RokuricsColors.coral
      case RecordingState.PAUSED:
        return RokuricsColors.softTeal
      default:
        return RokuricsColors.deepText
    }
  }
}