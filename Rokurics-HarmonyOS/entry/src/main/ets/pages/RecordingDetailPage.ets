/**
 * Recording detail page - mirrors RecordingStudyDetailPage.swift
 */
import { router } from '@kit.ArkUI'
import { media } from '@kit.MediaKit'
import { fileIo } from '@kit.CoreFileKit'
import { Want, common } from '@kit.AbilityKit'
import { getSharedRecordingManager, RecordingManager } from '../services/RecordingManager'
import { RecordingMetadata, StudyFilingPath, filingLevelTitle } from '../models/RecordingModels'
import { formatDuration, formatShortTime, fileSizeText } from '../utils/FormatHelpers'
import { TranscriptionPipeline } from '../services/TranscriptionPipeline'
import { ExportManager, ExportFormat } from '../services/ExportManager'
import { RecordingUploadClient, UploadConfig, UploadStage } from '../services/RecordingUploadClient'
import { SettingsStore } from '../services/SettingsStore'
import { TranscriptionResult, NoteGenerationResult } from '../models/ProcessingModels'
import { colorAlpha,  RokuricsColors, FontWeight, glassFillOpacity, glassStrokeHighOpacity,
  glassStrokeMidOpacity, glassAccentOpacity } from '../utils/RokuricsTheme'
import { TrashIcon, CloudUploadIcon, EditIcon } from '../utils/CustomIcons'

const LEVELS: string[] = ['type', 'subject', 'chapter', 'topic']
const FILING_OPTIONS: Record<string, string[]> = {
  'type': ['课堂录音', '自学笔记', '会议记录', '访谈采访', '灵感记录', '其他'],
  'subject': ['数学', '物理', '化学', '生物', '计算机', '英语', '历史', '文学', '哲学', '艺术'],
  'chapter': [],
  'topic': []
}

@Entry
@Component
struct RecordingDetailPage {
  private recordingManager: RecordingManager = getSharedRecordingManager()
  @State recordingId: string = ''
  @State recording: RecordingMetadata | null = null
  @State showRenameDialog: boolean = false
  @State renameText: string = ''
  @State showFilingEditor: boolean = false
  @State editType: string = ''
  @State editSubject: string = ''
  @State editChapter: string = ''
  @State editTopic: string = ''
  @State actualFileSize: number = 0
  @State isPlaying: boolean = false
  @State playbackError: string = ''
  @State isProcessing: boolean = false
  @State processingStage: string = ''
  @State processingError: string = ''
  @State hasTranscript: boolean = false
  @State hasNote: boolean = false
  @State showTranscriptView: boolean = false
  @State showNoteView: boolean = false
  @State transcriptText: string = ''
  @State noteMarkdown: string = ''
  @State isExporting: boolean = false
  @State exportMessage: string = ''
  @State exportList: string[] = []
  @State showExportList: boolean = false
  @State exportFormat: ExportFormat = 'Markdown'
  @State isUploading: boolean = false
  @State uploadProgressText: string = ''
  @State uploadError: string = ''
  private avPlayer: media.AVPlayer | null = null
  private pipeline: TranscriptionPipeline | null = null
  private exportManager: ExportManager | null = null
  private uploadClient: RecordingUploadClient | null = null

  aboutToAppear(): void {
    this.loadData()
    this.pipeline = new TranscriptionPipeline(getContext(this))
    this.exportManager = new ExportManager(getContext(this))
  }

  async loadData(): Promise<void> {
    await this.recordingManager.reloadRecordings()
    const found: RecordingMetadata | undefined = this.recordingManager.recordings.find(
      (r: RecordingMetadata) => r.id === this.recordingId
    )
    if (found) {
      this.recording = found
      this.actualFileSize = await this.recordingManager.getAudioFileSize(found)
      // Initialize filing editor from existing values
      this.editType = found.studyFiling?.type ?? ''
      this.editSubject = found.studyFiling?.subject ?? ''
      this.editChapter = found.studyFiling?.chapter ?? ''
      this.editTopic = found.studyFiling?.topic ?? ''
    }
  }

  private getFilingValue(level: string): string {
    if (!this.recording) return '未填写'
    return this.recording.studyFiling?.valueForLevel(level) ?? '未填写'
  }

  private formatDateStr(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
  }

  private async deleteAndBack(): Promise<void> {
    if (this.recording) {
      await this.recordingManager.deleteRecording(this.recording.id)
    }
    this.getUIContext().getRouter().back()
  }

  private async handleRename(): Promise<void> {
    const title = this.renameText.trim()
    if (title.length === 0 || !this.recording) return
    try {
      await this.recordingManager.renameRecording(this.recording.id, title)
      this.showRenameDialog = false
      await this.loadData()
    } catch {
      // ignore
    }
  }

  private openRenameDialog(): void {
    if (!this.recording) return
    this.renameText = this.recording.title
    this.showRenameDialog = true
  }

  private async saveFiling(): Promise<void> {
    if (!this.recording) return
    const filing = new StudyFilingPath(
      this.editType || undefined,
      this.editSubject || undefined,
      this.editChapter || undefined,
      this.editTopic || undefined
    )
    await this.recordingManager.updateStudyFiling(this.recording.id, filing)
    this.showFilingEditor = false
    await this.loadData()
  }

  private releasePlayer(): void {
    if (this.avPlayer) {
      try { this.avPlayer.stop() } catch (_e) { /* ignore */ }
      try { this.avPlayer.release() } catch (_e) { /* ignore */ }
      this.avPlayer = null
    }
    this.isPlaying = false
  }

  private async togglePlayback(): Promise<void> {
    if (!this.recording) return
    this.playbackError = ''

    if (this.isPlaying) {
      if (this.avPlayer) {
        try { await this.avPlayer.pause() } catch (_e) { /* ignore */ }
      }
      this.isPlaying = false
      return
    }

    if (!this.avPlayer) {
      const audioPath = this.recordingManager.getAudioAbsolutePath(this.recording)
      try {
        if (!fileIo.accessSync(audioPath)) {
          this.playbackError = '音频文件不存在'
          return
        }
        const file: fileIo.File = fileIo.openSync(audioPath, fileIo.OpenMode.READ_ONLY)
        this.avPlayer = await media.createAVPlayer()
        this.avPlayer.url = `fd://${file.fd}`
      } catch (err) {
        this.playbackError = `播放器创建失败：${err}`
        return
      }
    }

    try {
      await this.avPlayer.play()
      this.isPlaying = true
    } catch (err) {
      this.playbackError = `播放失败：${err}`
      this.releasePlayer()
    }
  }

  build() {
    Stack() {
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
              [RokuricsColors.glassStroke, 0.12],
              [RokuricsColors.aqua, 0.24]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 22
        } as BorderOptions)
        .onClick(() => this.getUIContext().getRouter().back())

        Text(this.recording?.title ?? '录音详情')
          .fontSize(20)
          .fontWeight(FontWeight.Bold)
          .fontColor(RokuricsColors.deepText)
          .maxLines(1)
          .textOverflow({ overflow: TextOverflow.Ellipsis })
          .layoutWeight(1)
          .margin({ left: 8 })

        // Right-side action buttons (mirrors iOS: share/upload, edit, delete)
        Row({ space: 8 }) {
          Button() {
            CloudUploadIcon(16, RokuricsColors.deepText)
          }
          .width(36).height(36).borderRadius(18)
          .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '4A'))
          .onClick(() => this.uploadRecording())

          Button() {
            EditIcon(16, RokuricsColors.deepText)
          }
          .width(36).height(36).borderRadius(18)
          .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '4A'))
          .onClick(() => this.openRenameDialog())

          Button() {
            TrashIcon(16, RokuricsColors.coral)
          }
          .width(36).height(36).borderRadius(18)
          .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '4A'))
          .onClick(() => this.deleteAndBack())
        }
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 56, bottom: 16 })

      if (this.recording === null) {
        Column() {
          Text('加载中...').fontColor(RokuricsColors.softText)
        }
        .width('100%')
        .layoutWeight(1)
        .justifyContent(FlexAlign.Center)
      } else {
        Scroll() {
          Column({ space: 16 }) {
            // Metadata subtitle
            Text(`${this.formatDateStr(this.recording!.createdAt)} · ${formatDuration(this.recording!.duration)}`)
              .fontSize(14)
              .fontWeight(FontWeight.Medium)
              .fontColor(RokuricsColors.softText)
              .width('100%')
              .margin({ top: -4 })

            // 2×2 Action Button Grid
            Column({ space: 10 }) {
              Row({ space: 10 }) {
                this.DetailActionButton(
                  this.recording!.uploadStatus === 'uploaded' ? '已上传' : '上传',
                  this.recording!.uploadStatus === 'uploaded' ? RokuricsColors.mint : RokuricsColors.deepText,
                  this.recording!.uploadStatus !== 'uploading',
                  () => this.uploadRecording()
                )
                this.DetailActionButton(
                  this.hasTranscript ? '查看转写' : '未转写',
                  this.hasTranscript ? RokuricsColors.deepText : RokuricsColors.tertiaryText,
                  true,
                  async () => {
                    this.showTranscriptView = !this.showTranscriptView
                    if (this.showTranscriptView && this.transcriptText.length === 0) {
                      await this.loadTranscript()
                    }
                  }
                )
              }
              .width('100%')
              Row({ space: 10 }) {
                this.DetailActionButton(
                  this.hasNote ? '查看总结' : '无总结',
                  this.hasNote ? RokuricsColors.deepText : RokuricsColors.tertiaryText,
                  true,
                  async () => {
                    this.showNoteView = !this.showNoteView
                    if (this.showNoteView && this.noteMarkdown.length === 0) {
                      await this.loadNote()
                    }
                  }
                )
                this.DetailActionButton('重命名', RokuricsColors.deepText, true,
                  () => this.openRenameDialog())
              }
              .width('100%')
            }
            .width('100%')

            // Technical info - glass card
            Text('技术信息')
              .fontSize(15)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
              .margin({ top: 8 })

            Column({ space: 6 }) {
              this.TechRow('格式', this.recording!.format.toUpperCase())
              this.TechRow('编码', this.recording!.codec)
              this.TechRow('采样率', `${this.recording!.sampleRate} Hz`)
              this.TechRow('声道', this.recording!.channels === 1 ? '单声道' : '立体声')
              this.TechRow('大小', this.actualFileSize > 0 ?
                fileSizeText(this.actualFileSize) : fileSizeText(this.recording!.fileSize))
            }
            .padding(14)
            .borderRadius(14)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'A8'))
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, 0.18],
                  [RokuricsColors.glassStroke, 0.10],
                  [RokuricsColors.glassStrokeAccent, 0.18]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 14
            } as BorderOptions)
            .shadow({
              color: colorAlpha(RokuricsColors.shadowColor, '06'),
              radius: 8,
              offsetY: 4
            })

            // Classification tags glass card
            Column({ space: 10 }) {
              Row({ space: 8 }) {
                ForEach(LEVELS, (level: string) => {
                  Column({ space: 2 }) {
                    Text(filingLevelTitle(level))
                      .fontSize(10)
                      .fontWeight(FontWeight.Medium)
                      .fontColor(RokuricsColors.tertiaryText)
                    Text(this.getFilingValue(level))
                      .fontSize(14)
                      .fontWeight(FontWeight.SemiBold)
                      .fontColor(RokuricsColors.deepText)
                  }
                  .padding({ left: 12, right: 12, top: 8, bottom: 8 })
                  .borderRadius(14)
                  .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'))
                  .border({
                    width: 1,
                    color: {
                      colors: [
                        [0xFFFFFF, 0.14],
                        [RokuricsColors.glassStroke, 0.08]
                      ],
                      direction: GradientDirection.RightBottom
                    },
                    radius: 14
                  } as BorderOptions)
                })
              }
              .width('100%')
              .flexShrink(0)

              Row({ space: 6 }) {
                Button() {
                  Row({ space: 4 }) {
                    Text('新建')
                      .fontSize(13)
                      .fontWeight(FontWeight.Medium)
                      .fontColor(RokuricsColors.aqua)
                    Text('+')
                      .fontSize(15)
                      .fontWeight(FontWeight.Bold)
                      .fontColor(RokuricsColors.aqua)
                  }
                }
                .padding({ left: 12, right: 12, top: 6, bottom: 6 })
                .borderRadius(14)
                .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '47'))
                .border({
                  width: 1,
                  color: {
                    colors: [
                      [0xFFFFFF, 0.18],
                      [RokuricsColors.glassStrokeAccent, 0.22]
                    ],
                    direction: GradientDirection.RightBottom
                  },
                  radius: 14
                } as BorderOptions)
                .onClick(() => { this.showFilingEditor = !this.showFilingEditor })

                if (this.showFilingEditor) {
                  Button() {
                    Text('保存').fontSize(13).fontWeight(FontWeight.Medium).fontColor(Color.White)
                  }
                  .padding({ left: 12, right: 12, top: 6, bottom: 6 })
                  .borderRadius(14)
                  .backgroundColor(RokuricsColors.aqua)
                  .onClick(() => this.saveFiling())
                }
              }
            }
            .width('100%')
            .padding(16)
            .borderRadius(24)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, glassStrokeHighOpacity],
                  [RokuricsColors.glassStroke, glassStrokeMidOpacity],
                  [RokuricsColors.glassStrokeAccent, glassAccentOpacity]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 24
            } as BorderOptions)
            .shadow({
              color: colorAlpha(RokuricsColors.shadowColor, '08'),
              radius: 12,
              offsetY: 6
            })

            // Filing editor (expandable)
            if (this.showFilingEditor) {
              Column({ space: 10 }) {
                this.FilingEditRow('门类', this.editType, FILING_OPTIONS['type'],
                  (v: string) => { this.editType = (v === this.editType ? '' : v) })
                this.FilingEditRow('课程', this.editSubject, FILING_OPTIONS['subject'],
                  (v: string) => { this.editSubject = (v === this.editSubject ? '' : v) })
                Column({ space: 4 }) {
                  Text('章节').fontSize(12).fontColor(RokuricsColors.softText)
                  TextInput({ text: this.editChapter, placeholder: '输入章节名' })
                    .fontSize(14).fontColor(RokuricsColors.deepText)
                    .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'))
                    .borderRadius(8)
                    .padding({ left: 12, right: 12, top: 8, bottom: 8 })
                    .onChange((value: string) => { this.editChapter = value })
                }
                Column({ space: 4 }) {
                  Text('主题').fontSize(12).fontColor(RokuricsColors.softText)
                  TextInput({ text: this.editTopic, placeholder: '输入主题名' })
                    .fontSize(14).fontColor(RokuricsColors.deepText)
                    .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'))
                    .borderRadius(8)
                    .padding({ left: 12, right: 12, top: 8, bottom: 8 })
                    .onChange((value: string) => { this.editTopic = value })
                }
              }
              .width('100%')
              .padding(16)
              .borderRadius(16)
              .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '60'))
            }

            // File status list glass card
            Column({ space: 8 }) {
              Text('文件状态')
                .fontSize(13)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.tertiaryText)
                .width('100%')

              this.FileStatusRow('上传', this.uploadLabel(this.recording!.uploadStatus))
              this.FileStatusRow('转写', this.transcriptLabel(this.recording!.transcriptionStatus))
              this.FileStatusRow('笔记', this.noteLabel(this.recording!.noteStatus))
              this.FileStatusRow('格式', this.recording!.format.toUpperCase())
              this.FileStatusRow('大小', this.actualFileSize > 0 ?
                fileSizeText(this.actualFileSize) : fileSizeText(this.recording!.fileSize))
            }
            .width('100%')
            .padding(16)
            .borderRadius(24)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, glassStrokeHighOpacity],
                  [RokuricsColors.glassStroke, glassStrokeMidOpacity],
                  [RokuricsColors.glassStrokeAccent, glassAccentOpacity]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 24
            } as BorderOptions)
            .shadow({
              color: colorAlpha(RokuricsColors.shadowColor, '08'),
              radius: 12,
              offsetY: 6
            })

            // AI Processing
            Column({ space: 8 }) {
              Text('AI 处理')
                .fontSize(15).fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.deepText)
                .width('100%')

              if (this.isProcessing) {
                Text(this.processingStage)
                  .fontSize(13).fontColor(RokuricsColors.aqua)
              }

              if (this.processingError.length > 0) {
                Text(this.processingError)
                  .fontSize(12).fontColor(RokuricsColors.coral)
              }

              Row({ space: 8 }) {
                Button() {
                  Text('转写').fontSize(13).fontWeight(FontWeight.Medium)
                    .fontColor(this.hasTranscript ? RokuricsColors.mint : RokuricsColors.aqua)
                }
                .padding({ left: 14, right: 14, top: 7, bottom: 7 })
                .border({ width: 1, color: colorAlpha(RokuricsColors.aqua, '40'), radius: 8 })
                .backgroundColor(Color.Transparent)
                .enabled(!this.isProcessing)
                .onClick(() => this.runTranscription())

                Button() {
                  Text('生成笔记').fontSize(13).fontWeight(FontWeight.Medium)
                    .fontColor(this.hasNote ? RokuricsColors.mint : RokuricsColors.aqua)
                }
                .padding({ left: 14, right: 14, top: 7, bottom: 7 })
                .border({ width: 1, color: colorAlpha(RokuricsColors.aqua, '40'), radius: 8 })
                .backgroundColor(Color.Transparent)
                .enabled(!this.isProcessing)
                .onClick(() => this.runFullProcessing())

                Button() {
                  Text('全部处理')
                    .fontSize(13).fontWeight(FontWeight.Medium)
                    .fontColor(Color.White)
                }
                .padding({ left: 14, right: 14, top: 7, bottom: 7 })
                .borderRadius(8)
                .backgroundColor(RokuricsColors.aqua)
                .enabled(!this.isProcessing)
                .onClick(() => this.runFullProcessing())
              }
              .width('100%')
              .justifyContent(FlexAlign.Start)

              // View transcript / note links
              if (this.hasTranscript) {
                Button() {
                  Text(this.showTranscriptView ? '收起转写 ▲' : '查看转写 ▼')
                    .fontSize(12).fontColor(RokuricsColors.aqua)
                }
                .backgroundColor(Color.Transparent)
                .margin({ top: 2 })
                .onClick(async () => {
                  this.showTranscriptView = !this.showTranscriptView
                  if (this.showTranscriptView && this.transcriptText.length === 0) {
                    await this.loadTranscript()
                  }
                })
              }

              if (this.showTranscriptView && this.transcriptText.length > 0) {
                Scroll() {
                  Text(this.transcriptText)
                    .fontSize(13).fontColor(RokuricsColors.deepText)
                    .padding(12)
                }
                .constraintSize({ maxHeight: 200 })
                .borderRadius(8)
                .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '60'))
                .width('100%')
              }

              if (this.hasNote) {
                Button() {
                  Text(this.showNoteView ? '收起笔记 ▲' : '查看笔记 ▼')
                    .fontSize(12).fontColor(RokuricsColors.aqua)
                }
                .backgroundColor(Color.Transparent)
                .margin({ top: 2 })
                .onClick(async () => {
                  this.showNoteView = !this.showNoteView
                  if (this.showNoteView && this.noteMarkdown.length === 0) {
                    await this.loadNote()
                  }
                })
              }

              if (this.showNoteView && this.noteMarkdown.length > 0) {
                Scroll() {
                  Text(this.noteMarkdown)
                    .fontSize(13).fontColor(RokuricsColors.deepText)
                    .padding(12)
                }
                .constraintSize({ maxHeight: 200 })
                .borderRadius(8)
                .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '60'))
                .width('100%')
              }
            }
            .width('100%')
            .padding(14).borderRadius(14)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'A8'))
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, 0.18],
                  [RokuricsColors.glassStroke, 0.10],
                  [RokuricsColors.glassStrokeAccent, 0.18]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 14
            } as BorderOptions)
            .shadow({
              color: colorAlpha(RokuricsColors.shadowColor, '06'),
              radius: 8,
              offsetY: 4
            })

            // Export - glass card
            Column({ space: 8 }) {
              Text('导出')
                .fontSize(15).fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.deepText)
                .width('100%')

              Row({ space: 8 }) {
                Button() {
                  Text('Markdown').fontSize(13).fontWeight(FontWeight.Medium)
                    .fontColor(this.exportFormat === 'Markdown' ? Color.White : RokuricsColors.softText)
                }
                .padding({ left: 14, right: 14, top: 6, bottom: 6 })
                .borderRadius(14)
                .backgroundColor(this.exportFormat === 'Markdown' ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'))
                .onClick(() => { this.exportFormat = 'Markdown' })

                Button() {
                  Text('JSON').fontSize(13).fontWeight(FontWeight.Medium)
                    .fontColor(this.exportFormat === 'JSON' ? Color.White : RokuricsColors.softText)
                }
                .padding({ left: 14, right: 14, top: 6, bottom: 6 })
                .borderRadius(14)
                .backgroundColor(this.exportFormat === 'JSON' ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'))
                .onClick(() => { this.exportFormat = 'JSON' })
              }
              .width('100%')

              Row({ space: 8 }) {
                Button() {
                  Text(this.isExporting ? '导出中...' : '导出录音信息')
                    .fontSize(13).fontWeight(FontWeight.Medium)
                    .fontColor(Color.White)
                }
                .padding({ left: 14, right: 14, top: 7, bottom: 7 })
                .borderRadius(8)
                .backgroundColor(RokuricsColors.aqua)
                .enabled(!this.isExporting)
                .onClick(() => this.exportRecording())

                Button() {
                  Text('导出全部')
                    .fontSize(13).fontWeight(FontWeight.Medium)
                    .fontColor(RokuricsColors.mint)
                }
                .padding({ left: 14, right: 14, top: 7, bottom: 7 })
                .border({ width: 1, color: colorAlpha(RokuricsColors.mint, '40'), radius: 8 })
                .backgroundColor(Color.Transparent)
                .enabled(!this.isExporting)
                .onClick(() => this.exportAll())

                Button() {
                  Text('查看导出')
                    .fontSize(13).fontWeight(FontWeight.Medium)
                    .fontColor(RokuricsColors.coral)
                }
                .padding({ left: 14, right: 14, top: 7, bottom: 7 })
                .border({ width: 1, color: colorAlpha(RokuricsColors.coral, '40'), radius: 8 })
                .backgroundColor(Color.Transparent)
                .onClick(() => this.loadExportList())
              }
              .width('100%')

              if (this.exportMessage.length > 0) {
                Text(this.exportMessage)
                  .fontSize(12)
                  .fontColor(this.exportMessage.startsWith('已导出') ? RokuricsColors.mint : RokuricsColors.coral)
              }

              if (this.showExportList && this.exportList.length > 0) {
                Column({ space: 4 }) {
                  Row() {
                    Text('已导出文件').fontSize(12).fontColor(RokuricsColors.softText)
                    Blank()
                    Text(`${this.exportList.length} 个`)
                      .fontSize(11).fontColor(RokuricsColors.tertiaryText)
                  }
                  .width('100%')

                  List({ space: 2 }) {
                    ForEach(this.exportList, (file: string) => {
                      ListItem() {
                        Row() {
                          Text(file)
                            .fontSize(12).fontColor(RokuricsColors.deepText)
                            .maxLines(1).textOverflow({ overflow: TextOverflow.Ellipsis })
                            .layoutWeight(1)
                          Button() {
                            Text('↗').fontSize(12).fontColor(RokuricsColors.aqua)
                          }
                          .width(28).height(28).backgroundColor(Color.Transparent)
                          .onClick(() => this.shareExportedFile(file))
                          Button() {
                            Text('✕').fontSize(12).fontColor(RokuricsColors.coral)
                          }
                          .width(28).height(28).backgroundColor(Color.Transparent)
                          .onClick(() => this.deleteExportedFile(file))
                        }
                        .width('100%').padding({ left: 10, right: 10, top: 6, bottom: 6 })
                        .borderRadius(6)
                        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'))
                      }
                    })
                  }
                  .constraintSize({ maxHeight: 120 })
                  .scrollBar(BarState.Off)
                }
                .width('100%')
                .padding(8).borderRadius(8)
                .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '30'))
              }
            }
            .width('100%')
            .padding(14).borderRadius(14)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'A8'))
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, 0.18],
                  [RokuricsColors.glassStroke, 0.10],
                  [RokuricsColors.glassStrokeAccent, 0.18]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 14
            } as BorderOptions)
            .shadow({
              color: colorAlpha(RokuricsColors.shadowColor, '06'),
              radius: 8,
              offsetY: 4
            })

            // Actions - glass card
            Row({ space: 12 }) {
              Button() {
                Text(this.isPlaying ? '⏸ 暂停' : '▶ 播放')
                  .fontSize(14)
                  .fontColor(RokuricsColors.aqua)
              }
              .padding({ left: 16, right: 16, top: 8, bottom: 8 })
              .border({ width: 1, color: colorAlpha(RokuricsColors.aqua, '40'), radius: 10 })
              .backgroundColor(Color.Transparent)
              .onClick(() => this.togglePlayback())

              if (this.playbackError.length > 0) {
                Text(this.playbackError)
                  .fontSize(11)
                  .fontColor(RokuricsColors.coral)
                  .maxLines(1)
              }

              Button() {
                Text(this.isUploading ? (this.uploadProgressText || '上传中...') : '☁ 上传')
                  .fontSize(14)
                  .fontColor(this.recording?.uploadStatus === 'uploaded' ? RokuricsColors.mint : RokuricsColors.aqua)
              }
              .padding({ left: 16, right: 16, top: 8, bottom: 8 })
              .border({ width: 1, color: colorAlpha(RokuricsColors.aqua, '40'), radius: 10 })
              .backgroundColor(Color.Transparent)
              .enabled(!this.isUploading)
              .onClick(() => this.uploadRecording())

              if (this.uploadError.length > 0) {
                Text(this.uploadError)
                  .fontSize(11)
                  .fontColor(RokuricsColors.coral)
                  .maxLines(1)
              }

              Blank()

              Button() {
                Text('重命名').fontSize(14).fontColor(RokuricsColors.aqua)
              }
              .padding({ left: 16, right: 16, top: 8, bottom: 8 })
              .border({ width: 1, color: colorAlpha(RokuricsColors.aqua, '40'), radius: 10 })
              .backgroundColor(Color.Transparent)
              .onClick(() => this.openRenameDialog())

              Button() {
                Text('删除').fontSize(14).fontColor(RokuricsColors.coral)
              }
              .padding({ left: 16, right: 16, top: 8, bottom: 8 })
              .border({ width: 1, color: colorAlpha(RokuricsColors.coral, '40'), radius: 10 })
              .backgroundColor(Color.Transparent)
              .onClick(() => this.deleteAndBack())
            }
            .width('100%')
            .justifyContent(FlexAlign.End)
            .margin({ top: 12, bottom: 40 })
          }
          .width('100%')
          .padding({ left: 16, right: 16 })
        }
        .width('100%')
        .layoutWeight(1)
        .scrollBar(BarState.Off)
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

    // Rename dialog
    if (this.showRenameDialog) {
      Column() {
        Column({ space: 16 }) {
          Text('重命名录音')
            .fontSize(18)
            .fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.deepText)

          TextInput({ text: this.renameText, placeholder: '输入新标题' })
            .fontSize(16)
            .fontColor(RokuricsColors.deepText)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'))
            .borderRadius(10)
            .padding(14)
            .onChange((value: string) => { this.renameText = value })

          Row({ space: 12 }) {
            Button() {
              Text('取消').fontSize(14).fontColor(RokuricsColors.softText)
            }
            .backgroundColor(Color.Transparent)
            .onClick(() => { this.showRenameDialog = false })

            Button() {
              Text('保存').fontSize(14).fontColor(Color.White)
            }
            .padding({ left: 24, right: 24, top: 10, bottom: 10 })
            .borderRadius(10)
            .backgroundColor(RokuricsColors.aqua)
            .onClick(() => this.handleRename())
          }
          .width('100%')
          .justifyContent(FlexAlign.End)
        }
        .padding(24)
        .borderRadius(20)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'E6'))
        .width('85%')
        .shadow({ radius: 30, color: '#20000000' })
      }
      .width('100%')
      .height('100%')
      .justifyContent(FlexAlign.Center)
      .backgroundColor('#50000000')
      .position({ x: 0, y: 0 })
      .onClick(() => { this.showRenameDialog = false })
    }
    }
  }

  private async runTranscription(): Promise<void> {
    if (!this.recording || this.isProcessing) return
    this.isProcessing = true
    this.processingError = ''
    this.processingStage = '正在转写...'

    try {
      if (!this.pipeline) {
        this.pipeline = new TranscriptionPipeline(getContext(this))
      }
      const audioPath = this.recordingManager.getAudioAbsolutePath(this.recording)
      await this.pipeline.runTranscription(this.recording, audioPath, true)
      this.hasTranscript = true
      this.processingStage = '转写完成'
    } catch (err) {
      this.processingError = `转写失败: ${err}`
    } finally {
      this.isProcessing = false
    }
  }

  private async runFullProcessing(): Promise<void> {
    if (!this.recording || this.isProcessing) return
    this.isProcessing = true
    this.processingError = ''
    this.processingStage = '处理中...'

    try {
      if (!this.pipeline) {
        this.pipeline = new TranscriptionPipeline(getContext(this))
      }
      const audioPath = this.recordingManager.getAudioAbsolutePath(this.recording)
      await this.pipeline.runFullPipeline(this.recording, audioPath, true)
      if (this.pipeline.state.transcript) {
        this.hasTranscript = true
      }
      if (this.pipeline.state.note) {
        this.hasNote = true
      }
      this.processingStage = '处理完成'
    } catch (err) {
      this.processingError = `处理失败: ${err}`
    } finally {
      this.isProcessing = false
    }
  }

  private async loadTranscript(): Promise<void> {
    if (!this.recording || !this.pipeline) return
    try {
      const store = this.pipeline.getTranscriptStore()
      const markdown = await store.loadTranscriptMarkdown(this.recording.id, this.recording.createdAt)
      if (markdown && markdown.length > 0) {
        this.transcriptText = markdown
        this.hasTranscript = true
      }
    } catch {
      this.transcriptText = '(无法加载转写)'
    }
  }

  private async loadNote(): Promise<void> {
    if (!this.recording || !this.pipeline) return
    try {
      const store = this.pipeline.getTranscriptStore()
      const markdown = await store.loadNoteMarkdown(this.recording.id, this.recording.createdAt)
      if (markdown && markdown.length > 0) {
        this.noteMarkdown = markdown
        this.hasNote = true
      }
    } catch {
      this.noteMarkdown = '(无法加载笔记)'
    }
  }

  private async exportRecording(): Promise<void> {
    if (!this.recording || !this.exportManager || this.isExporting) return
    this.isExporting = true
    this.exportMessage = ''
    try {
      await this.exportManager.exportRecordingInfo(this.recording, this.exportFormat)
      this.exportMessage = `已导出为 ${this.exportFormat}`
    } catch (err) {
      this.exportMessage = `导出失败: ${err}`
    } finally {
      this.isExporting = false
    }
  }

  private async exportAll(): Promise<void> {
    if (!this.recording || !this.exportManager || !this.pipeline || this.isExporting) return
    this.isExporting = true
    this.exportMessage = ''
    try {
      const store = this.pipeline.getTranscriptStore()
      const transcript = await store.loadTranscript(this.recording.id, this.recording.createdAt)
      const paths = await this.exportManager.exportAllForRecording(this.recording, transcript, null)
      this.exportMessage = `已导出 ${paths.length} 个文件`
    } catch (err) {
      this.exportMessage = `批量导出失败: ${err}`
    } finally {
      this.isExporting = false
    }
  }

  private async uploadRecording(): Promise<void> {
    if (!this.recording || this.isUploading) return
    this.isUploading = true
    this.uploadProgressText = '准备上传...'
    this.uploadError = ''
    try {
      const settingsStore = new SettingsStore(getContext(this))
      const config = await RecordingUploadClient.loadConfig(settingsStore)
      if (!config.serverURL || config.serverURL === 'http://localhost:8000') {
        this.uploadError = '请先在设置中配置上传服务器地址'
        return
      }
      this.uploadClient = new RecordingUploadClient(config)
      this.uploadClient.onProgress((p) => {
        this.uploadProgressText = p.description
      })
      const audioPath = this.recordingManager.getAudioAbsolutePath(this.recording)
      const result = await this.uploadClient.uploadRecording(this.recording, audioPath)
      if (result.ok) {
        this.uploadProgressText = '上传完成'
        await this.recordingManager.reloadRecordings()
        await this.loadData()
      } else {
        this.uploadError = result.error ?? '上传失败'
      }
    } catch (err) {
      this.uploadError = `上传错误: ${err}`
    } finally {
      this.isUploading = false
    }
  }

  private async loadExportList(): Promise<void> {
    if (!this.exportManager) return
    this.showExportList = !this.showExportList
    if (this.showExportList) {
      this.exportList = await this.exportManager.listExports()
    }
  }

  private async deleteExportedFile(filename: string): Promise<void> {
    if (!this.exportManager) return
    await this.exportManager.deleteExport(filename)
    this.exportList = await this.exportManager.listExports()
  }

  private async shareExportedFile(filename: string): Promise<void> {
    if (!this.exportManager) return
    const fullPath = `${this.exportManager.getExportsDir()}/${filename}`
    try {
      if (!fileIo.accessSync(fullPath)) {
        this.exportMessage = '文件不存在'
        return
      }
      const mimeType = filename.endsWith('.json') ? 'application/json' : 'text/markdown'
      const want: Want = {
        action: 'ohos.want.action.sendData',
        uri: `file://${fullPath}`,
        type: mimeType,
        parameters: {
          'ability.params.stream': [fullPath]
        }
      }
      const ctx = getContext(this) as common.UIAbilityContext
      await ctx.startAbility(want)
      this.exportMessage = `已分享: ${filename}`
    } catch (err) {
      this.exportMessage = `分享失败: ${err}`
    }
  }

  aboutToDisappear(): void {
    this.releasePlayer()
  }

  @Builder
  InfoRow(label: string, value: string) {
    Row() {
      Text(label)
        .fontSize(13)
        .fontColor(RokuricsColors.softText)
        .width(70)
      Text(value)
        .fontSize(16)
        .fontWeight(FontWeight.SemiBold)
        .fontColor(RokuricsColors.deepText)
        .layoutWeight(1)
    }
    .width('100%')
    .padding(14)
    .borderRadius(12)
    .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'))
  }

  @Builder
  TechRow(label: string, value: string) {
    Row() {
      Text(label)
        .fontSize(12)
        .fontColor(RokuricsColors.tertiaryText)
        .width(60)
      Text(value)
        .fontSize(12)
        .fontColor(RokuricsColors.softText)
    }
  }

  @Builder
  FileStatusRow(label: string, value: string) {
    Row() {
      Text(label)
        .fontSize(14)
        .fontWeight(FontWeight.Medium)
        .fontColor(RokuricsColors.softText)
      Blank()
      Row({ space: 4 }) {
        Text(value)
          .fontSize(14)
          .fontWeight(FontWeight.SemiBold)
          .fontColor(RokuricsColors.deepText)
        Text('›')
          .fontSize(16)
          .fontColor(RokuricsColors.tertiaryText)
      }
    }
    .width('100%')
    .padding({ left: 4, right: 4, top: 6, bottom: 6 })
  }

  @Builder
  FilingEditRow(label: string, selectedValue: string, options: string[], onSelect: (v: string) => void) {
    Column({ space: 4 }) {
      Text(label).fontSize(12).fontColor(RokuricsColors.softText)
      Row({ space: 6 }) {
        ForEach(options, (option: string) => {
          Text(option)
            .fontSize(12)
            .fontWeight(FontWeight.Medium)
            .fontColor(selectedValue === option ? Color.White : RokuricsColors.softText)
            .padding({ left: 10, right: 10, top: 5, bottom: 5 })
            .borderRadius(12)
            .backgroundColor(selectedValue === option ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'))
            .onClick(() => onSelect(option))
        })
      }
      .width('100%')
      .flexShrink(0)
    }
  }

  @Builder
  DetailActionButton(title: string, tint: string, isEnabled: boolean, action: () => void) {
    Button() {
      Text(title)
        .fontSize(12)
        .fontWeight(FontWeight.Bold)
        .fontColor(isEnabled ? tint : RokuricsColors.tertiaryText)
        .width('100%')
        .textAlign(TextAlign.Center)
    }
    .layoutWeight(1)
    .padding({ left: 12, right: 12, top: 10, bottom: 10 })
    .borderRadius(20)
    .backgroundColor(isEnabled ?
      colorAlpha(RokuricsColors.glassSurface, '47') :
      colorAlpha(RokuricsColors.glassSurface, '28'))
    .border({
      width: 1,
      color: {
        colors: [
          [0xFFFFFF, isEnabled ? 0.22 : 0.12],
          [RokuricsColors.glassStroke, isEnabled ? 0.14 : 0.08],
          [RokuricsColors.glassStrokeAccent, isEnabled ? 0.24 : 0.10]
        ],
        direction: GradientDirection.RightBottom
      },
      radius: 20
    } as BorderOptions)
    .enabled(isEnabled)
    .onClick(action)
  }

  private uploadLabel(s: string): string {
    if (s === 'localOnly') return '未上传'
    if (s === 'uploading') return '上传中'
    if (s === 'uploaded') return '已上传'
    if (s === 'failed') return '失败'
    return s
  }
  private transcriptLabel(s: string): string {
    if (s === 'notStarted') return '未开始'
    if (s === 'transcribing') return '转写中'
    if (s === 'transcribed') return '已完成'
    if (s === 'failed') return '失败'
    return s
  }
  private noteLabel(s: string): string {
    if (s === 'notStarted') return '未开始'
    if (s === 'generating') return '生成中'
    if (s === 'generated') return '已生成'
    if (s === 'failed') return '失败'
    return s
  }
}
