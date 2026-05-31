/**
 * Recording library page - mirrors RecordingLibraryView.swift with integrated StudyLibrary browser
 * Shows folder hierarchy tiles at root, recording cards within folders
 */
import { ConfigurationConstant } from '@kit.AbilityKit'
import { getSharedRecordingManager, RecordingManager } from '../services/RecordingManager'
import { RecordingMetadata, StudyFilingPath, filingLevelTitle } from '../models/RecordingModels'
import { formatDuration, formatShortTime } from '../utils/FormatHelpers'
import { colorAlpha, RokuricsColors, FontWeight, glassFillOpacity, glassStrokeHighOpacity,
  glassStrokeMidOpacity, glassAccentOpacity, applyColorMode } from '../utils/RokuricsTheme'
import { StudyFolderStore } from '../services/StudyFolderStore'
import { BackIcon, BulletListIcon, TrashIcon, EllipsisIcon, DocBadgeIcon, NoteBadgeIcon } from '../utils/CustomIcons'

const LEVELS: string[] = ['type', 'subject', 'chapter', 'topic']
const FOLDER_COLORS: string[] = [RokuricsColors.aqua, RokuricsColors.mint, RokuricsColors.skyCyan, RokuricsColors.coral, '#B8A6D6', '#6B9FD4']

interface FilingGroup {
  key: string
  label: string
  level: string
  count: number
}

@Entry
@Component
struct RecordingLibraryPage {
  private recordingManager: RecordingManager = getSharedRecordingManager()
  private folderStore: StudyFolderStore = new StudyFolderStore(getContext(this))
  @StorageLink('colorMode') colorMode: ConfigurationConstant.ColorMode = ConfigurationConstant.ColorMode.COLOR_MODE_NOT_SET
  @State recordings: RecordingMetadata[] = []
  @State trashedRecordings: RecordingMetadata[] = []
  @State showTrash: boolean = false
  @State searchQuery: string = ''
  @State sortOrder: string = 'newest'
  @State browsePath: string[] = []
  @State currentGroups: FilingGroup[] = []
  @State currentRecordings: RecordingMetadata[] = []
  @State isBrowsing: boolean = false
  @State folderColorMap: Record<string, string> = {}
  @State showRenameDialog: boolean = false
  @State renameTarget: string = ''
  @State renameText: string = ''
  @State renameLevel: string = ''
  @State showDeleteConfirm: boolean = false
  @State deleteTarget: string = ''
  @State deleteTargetName: string = ''
  @State showColorPicker: boolean = false
  @State selectedColor: string = RokuricsColors.aqua
  @State filterTranscribedActive: boolean = false
  @State filterNoteGeneratedActive: boolean = false
  @State filterUploadedActive: boolean = false

  aboutToAppear(): void {
    this.loadAll()
  }

  async loadAll(): Promise<void> {
    await this.recordingManager.reloadRecordings()
    this.recordings = this.recordingManager.recordings.filter((r: RecordingMetadata) => !r.isDeleted)
    this.trashedRecordings = this.recordingManager.trashedRecordings
    await this.loadFolderColors()
    if (this.isBrowsing) {
      this.navigateToPath(this.browsePath)
    } else {
      this.buildRootGroups()
    }
  }

  async loadFolderColors(): Promise<void> {
    const allFolders = await this.folderStore.listFolders()
    const map: Record<string, string> = {}
    for (const f of allFolders) {
      if (f.colorToken) {
        map[f.name] = f.colorToken
      }
    }
    this.folderColorMap = map
  }

  private buildRootGroups(): void {
    this.isBrowsing = false
    this.currentGroups = this.extractGroups(0, [])
    this.currentRecordings = []
  }

  private extractGroups(levelIdx: number, parentPath: string[]): FilingGroup[] {
    if (levelIdx >= LEVELS.length) return []
    const level = LEVELS[levelIdx]
    const map = new Map<string, number>()

    for (const r of this.recordings) {
      const v = r.studyFiling?.valueForLevel(level) ?? null
      const key = v || '未分类'
      // Only count recordings that match the parent path
      if (parentPath.length > 0) {
        let matchesParent = true
        for (let i = 0; i < parentPath.length && i < LEVELS.length; i++) {
          const pv = r.studyFiling?.valueForLevel(LEVELS[i]) ?? null
          if ((pv || '未分类') !== parentPath[i]) {
            matchesParent = false
            break
          }
        }
        if (!matchesParent) continue
      }
      map.set(key, (map.get(key) ?? 0) + 1)
    }

    const groups: FilingGroup[] = []
    const entries = Array.from(map.entries())
    entries.sort((a, b) => a[0].localeCompare(b[0]))
    for (const entry of entries) {
      groups.push({
        key: entry[0],
        label: entry[0],
        level: level,
        count: entry[1]
      })
    }
    return groups
  }

  private navigateToPath(path: string[]): void {
    this.isBrowsing = true
    this.browsePath = path
    this.currentGroups = []

    const levelIdx = path.length
    if (levelIdx < LEVELS.length) {
      this.currentGroups = this.extractGroups(levelIdx, path)
    }

    // Filter recordings matching the path
    this.currentRecordings = this.recordings.filter((r: RecordingMetadata) => {
      for (let i = 0; i < path.length && i < LEVELS.length; i++) {
        const v = r.studyFiling?.valueForLevel(LEVELS[i]) ?? null
        if ((v || '未分类') !== path[i]) return false
      }
      return true
    })
  }

  private navigateBack(): void {
    if (this.browsePath.length <= 1) {
      this.browsePath = []
      this.isBrowsing = false
      this.currentRecordings = []
      this.buildRootGroups()
    } else {
      const parentPath: string[] = []
      for (let i = 0; i < this.browsePath.length - 1; i++) {
        parentPath.push(this.browsePath[i])
      }
      this.navigateToPath(parentPath)
    }
  }

  private toggleTrash(): void {
    this.showTrash = !this.showTrash
  }

  private async restoreAndReload(id: string): Promise<void> {
    await this.recordingManager.restoreRecording(id)
    this.loadAll()
  }

  private async permDeleteAndReload(id: string): Promise<void> {
    await this.recordingManager.permanentlyDelete(id)
    this.loadAll()
  }

  private async deleteAndReload(id: string): Promise<void> {
    await this.recordingManager.deleteRecording(id)
    this.loadAll()
  }

  private openDetail(id: string): void {
    this.getUIContext().getRouter().pushUrl({
      url: 'pages/RecordingDetailPage',
      params: { recordingId: id }
    })
  }

  private openRenameDialog(key: string, level: string): void {
    this.renameTarget = key
    this.renameText = key
    this.renameLevel = level
    this.selectedColor = this.folderColorMap[key] ?? RokuricsColors.aqua
    this.showRenameDialog = true
  }

  private async commitRename(): Promise<void> {
    const newName = this.renameText.trim()
    if (newName.length === 0 || newName === this.renameTarget || this.renameTarget.length === 0) {
      this.showRenameDialog = false
      return
    }

    const level = this.renameLevel
    const toUpdate = this.recordings.filter((r: RecordingMetadata) => {
      const v = r.studyFiling?.valueForLevel(level) ?? null
      return v === this.renameTarget || (v === null && this.renameTarget === '未分类')
    })

    for (const r of toUpdate) {
      const filing = r.studyFiling ? r.studyFiling.clone() : new StudyFilingPath()
      let changed = false
      if (level === 'type') { filing.type = newName; changed = true }
      else if (level === 'subject') { filing.subject = newName; changed = true }
      else if (level === 'chapter') { filing.chapter = newName; changed = true }
      else if (level === 'topic') { filing.topic = newName; changed = true }
      if (changed) {
        await this.recordingManager.updateStudyFiling(r.id, filing)
      }
    }

    // Update persisted folder
    const folders = await this.folderStore.listFolders(level)
    const match = folders.find(f => f.name === this.renameTarget)
    if (match) {
      await this.folderStore.renameFolder(match.id, newName)
      if (this.selectedColor !== (this.folderColorMap[this.renameTarget] ?? RokuricsColors.aqua)) {
        await this.folderStore.setColorToken(match.id, this.selectedColor)
      }
    } else if (this.selectedColor !== RokuricsColors.aqua) {
      // Create folder to persist color
      const record = await this.folderStore.createFolder(newName, level, this.browsePath)
      await this.folderStore.setColorToken(record.id, this.selectedColor)
    }

    this.showRenameDialog = false
    await this.loadAll()
  }

  private openDeleteConfirm(key: string, name: string, level: string): void {
    this.deleteTarget = key
    this.deleteTargetName = name
    this.renameLevel = level
    this.showDeleteConfirm = true
  }

  private async commitDelete(): Promise<void> {
    if (this.deleteTarget.length === 0) {
      this.showDeleteConfirm = false
      return
    }
    const level = this.renameLevel
    const toClear = this.recordings.filter((r: RecordingMetadata) => {
      const v = r.studyFiling?.valueForLevel(level) ?? null
      return v === this.deleteTarget || (v === null && this.deleteTarget === '未分类')
    })

    for (const r of toClear) {
      const filing = r.studyFiling ? r.studyFiling.clone() : new StudyFilingPath()
      if (level === 'type') filing.type = null
      else if (level === 'subject') filing.subject = null
      else if (level === 'chapter') filing.chapter = null
      else if (level === 'topic') filing.topic = null
      await this.recordingManager.updateStudyFiling(r.id, filing)
    }

    const folders = await this.folderStore.listFolders(level)
    const match = folders.find(f => f.name === this.deleteTarget)
    if (match) await this.folderStore.deleteFolder(match.id)

    this.showDeleteConfirm = false
    await this.loadAll()
  }

  // ── Build ──

  build() {
    applyColorMode(this.colorMode)
    Stack() {
    Column() {
      // Header
      Row() {
        Button() {
          BackIcon(18, RokuricsColors.deepText)
        }
        .width(44)
        .height(44)
        .borderRadius(22)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
        .shadow({
          color: colorAlpha(RokuricsColors.shadowColor, '10'),
          radius: 12,
          offsetY: 6
        })
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, glassStrokeHighOpacity],
              [RokuricsColors.glassStroke, glassStrokeMidOpacity],
              [RokuricsColors.aqua, glassAccentOpacity]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 22
        } as BorderOptions)
        .onClick(() => this.getUIContext().getRouter().back())

        Text('学习库')
          .fontSize(30)
          .fontWeight(FontWeight.Bold)
          .fontColor(RokuricsColors.deepText)
          .margin({ left: 8 })

        Blank()

        // Flat list toggle
        Button() {
          BulletListIcon(14, RokuricsColors.aqua)
        }
        .backgroundColor(Color.Transparent)
        .margin({ right: 8 })
        .onClick(() => {
          if (this.isBrowsing) {
            this.browsePath = []
            this.isBrowsing = false
            this.currentRecordings = []
            this.buildRootGroups()
          }
        })

        Button() {
          Text(this.showTrash ? '录音' : '废纸篓')
            .fontSize(14)
            .fontColor(RokuricsColors.aqua)
            .fontWeight(FontWeight.Medium)
        }
        .backgroundColor(Color.Transparent)
        .onClick(() => { this.toggleTrash() })
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 56, bottom: 16 })

      if (this.showTrash) {
        this.TrashList()
      } else if (this.isBrowsing) {
        // Breadcrumb + recording list for browsed folder
        this.BrowseContent()
      } else {
        // Root: folder tiles + unfiled recordings
        this.RootContent()
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

    // Rename dialog overlay with color picker
    if (this.showRenameDialog) {
      Column() {
        Column({ space: 16 }) {
          Text('重命名分类')
            .fontSize(18).fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.deepText)

          TextInput({ text: this.renameText, placeholder: '新名称' })
            .fontSize(16).fontColor(RokuricsColors.deepText)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'))
            .borderRadius(10).padding(14)
            .onChange((v: string) => { this.renameText = v })

          // Inline color picker
          Column({ space: 8 }) {
            Text('分类颜色').fontSize(13).fontColor(RokuricsColors.softText)
            Row({ space: 8 }) {
              ForEach(FOLDER_COLORS, (color: string) => {
                Row() {
                  if (this.selectedColor === color) {
                    Text('✓').fontSize(11).fontWeight(FontWeight.Bold).fontColor(Color.White)
                  }
                }
                .width(28).height(28).borderRadius(14)
                .backgroundColor(color)
                .justifyContent(FlexAlign.Center)
                .border({
                  width: this.selectedColor === color ? 2 : 1,
                  color: this.selectedColor === color ? Color.White : colorAlpha(color, '40'),
                  radius: 14
                })
                .onClick(() => { this.selectedColor = color })
              })
            }
            .width('100%')
          }
          .width('100%')

          Row({ space: 12 }) {
            Button() { Text('取消').fontSize(14).fontColor(RokuricsColors.softText) }
            .backgroundColor(Color.Transparent).onClick(() => { this.showRenameDialog = false })
            Button() { Text('保存').fontSize(14).fontColor(Color.White) }
            .padding({ left: 24, right: 24, top: 10, bottom: 10 })
            .borderRadius(10).backgroundColor(RokuricsColors.aqua)
            .onClick(() => this.commitRename())
          }
          .width('100%').justifyContent(FlexAlign.End)
        }
        .padding(24).borderRadius(20).backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'E6')).width('85%')
        .shadow({ radius: 30, color: '#20000000' })
        .onClick(() => {}) // consume click to prevent dismiss overlay firing
      }
      .width('100%').height('100%').justifyContent(FlexAlign.Center)
      .backgroundColor('#50000000').position({ x: 0, y: 0 })
      .onClick(() => { this.showRenameDialog = false })
    }

    // Delete confirm overlay
    if (this.showDeleteConfirm) {
      Column() {
        Column({ space: 16 }) {
          Text('删除分类').fontSize(18).fontWeight(FontWeight.SemiBold).fontColor(RokuricsColors.deepText)
          Text(`确定删除「${this.deleteTargetName}」？关联的录音不会被删除。`)
            .fontSize(14).fontColor(RokuricsColors.softText).textAlign(TextAlign.Center)
          Row({ space: 12 }) {
            Button() {
              Text('取消').fontSize(14).fontColor(RokuricsColors.softText)
            }
            .backgroundColor(Color.Transparent)
            .onClick(() => { this.showDeleteConfirm = false })
            Button() {
              Text('删除').fontSize(14).fontColor(Color.White)
            }
            .padding({ left: 24, right: 24, top: 10, bottom: 10 })
            .borderRadius(10).backgroundColor(RokuricsColors.coral)
            .onClick(() => this.commitDelete())
          }
          .width('100%').justifyContent(FlexAlign.End)
        }
        .padding(24).borderRadius(20).backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'E6')).width('85%')
        .shadow({ radius: 30, color: '#20000000' })
        .onClick(() => {}) // consume click to prevent dismiss overlay firing
      }
      .width('100%').height('100%').justifyContent(FlexAlign.Center)
      .backgroundColor('#50000000').position({ x: 0, y: 0 })
      .onClick(() => { this.showDeleteConfirm = false })
    }
    }
  }

  // ── Root content: folder tiles + search ──

  @Builder
  RootContent() {
    Scroll() {
      Column({ space: 16 }) {
        // Empty state or content
        if (this.recordings.length === 0) {
          Column({ space: 12 }) {
            Text('暂无录音')
              .fontSize(16).fontColor(RokuricsColors.tertiaryText)
            Text('收到或保存的录音会在这里按门类、课程、章节和主题逐层显示。')
              .fontSize(13).fontColor(RokuricsColors.softText).textAlign(TextAlign.Center)
              .padding({ left: 32, right: 32 })
            Text('返回首页开始录音')
              .fontSize(13).fontColor(RokuricsColors.aqua)
          }
          .width('100%').height(300).justifyContent(FlexAlign.Center)
        } else {
          // Search
          Row({ space: 8 }) {
            TextInput({ text: this.searchQuery, placeholder: '搜索录音...' })
              .fontSize(13).layoutWeight(1).height(36).borderRadius(18)
              .padding({ left: 14, right: 14 })
              .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
              .onChange((v: string) => { this.searchQuery = v })
            Button() {
              Text(this.sortOrder === 'newest' ? '↓最新' : '↑最早')
                .fontSize(11).fontWeight(FontWeight.Medium).fontColor(RokuricsColors.aqua)
            }
            .height(32).padding({ left: 10, right: 10 }).borderRadius(16)
            .backgroundColor(colorAlpha(RokuricsColors.aqua, '14'))
            .onClick(() => { this.sortOrder = this.sortOrder === 'newest' ? 'oldest' : 'newest' })
          }
          .width('100%')

          // Folder tiles
          if (this.currentGroups.length > 0) {
            this.FolderTileGrid()
          }

          // Filter chips
          Row({ space: 6 }) {
            this.FilterChip('已转写', this.filterTranscribedActive,
              (v: boolean) => { this.filterTranscribedActive = v })
            this.FilterChip('有笔记', this.filterNoteGeneratedActive,
              (v: boolean) => { this.filterNoteGeneratedActive = v })
            this.FilterChip('已上传', this.filterUploadedActive,
              (v: boolean) => { this.filterUploadedActive = v })
          }
          .width('100%')

          // Recording list
          this.RecordingRows(this.getSortedRecordings())
        }
      }
      .width('100%')
      .padding({ left: 16, right: 16, bottom: 40 })
    }
    .width('100%').layoutWeight(1).scrollBar(BarState.Off)
    .padding({ top: 4 })
  }

  @Builder
  FolderTileGrid() {
    Column({ space: 10 }) {
      Text('分类浏览')
        .fontSize(15).fontWeight(FontWeight.SemiBold).fontColor(RokuricsColors.deepText)
        .width('100%')

      Row({ space: 10 }) {
        ForEach(this.currentGroups, (group: FilingGroup) => {
          Button() {
            Column({ space: 12 }) {
              // Folder icon area
              Stack() {
                Circle()
                  .width(48).height(48)
                  .fill(colorAlpha(this.folderColorMap[group.label] ?? RokuricsColors.aqua, '24'))
                // Simple folder shape using rounded rects
                Column({ space: 0 }) {
                  Rect()
                    .width(28).height(4)
                    .radius(2)
                    .fill(this.folderColorMap[group.label] ?? RokuricsColors.aqua)
                    .translate({ x: -4, y: 0 })
                  Rect()
                    .width(36).height(24)
                    .radius([0, 8, 8, 8])
                    .fill(this.folderColorMap[group.label] ?? RokuricsColors.aqua)
                }
              }
              .width(48).height(48)

              // Title
              Text(group.label)
                .fontSize(15)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.deepText)
                .maxLines(1)
                .textOverflow({ overflow: TextOverflow.Ellipsis })

              // Count
              Text(`${group.count} 项`)
                .fontSize(12)
                .fontWeight(FontWeight.Regular)
                .fontColor(RokuricsColors.tertiaryText)
            }
            .width('100%')
            .padding({ top: 20, bottom: 18, left: 12, right: 12 })
            .borderRadius(20)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, 0.12],
                  [RokuricsColors.glassStroke, 0.08],
                  [RokuricsColors.glassStrokeAccent, 0.08]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 20
            } as BorderOptions)
            .shadow({
              color: colorAlpha(RokuricsColors.shadowColor, '06'),
              radius: 10,
              offsetY: 5
            })
          }
          .backgroundColor(Color.Transparent)
          .layoutWeight(1)
          .onClick(() => {
            this.navigateToPath([group.label])
          })
        })
      }
      .width('100%')
      .justifyContent(FlexAlign.Start)
    }
    .width('100%')
  }

  // ── Browse content: breadcrumb + recordings in folder ──

  @Builder
  BrowseContent() {
    Column() {
      // Breadcrumb
      Row({ space: 4 }) {
        Button() {
          Text('全部')
            .fontSize(13).fontColor(RokuricsColors.softText)
        }
        .backgroundColor(Color.Transparent)
        .onClick(() => {
          this.browsePath = []
          this.isBrowsing = false
          this.buildRootGroups()
        })

        ForEach(this.browsePath, (crumb: string, index: number) => {
          Row() {
            Text('›').fontSize(13).fontColor(RokuricsColors.tertiaryText).margin({ left: 2, right: 2 })
            Text(crumb)
              .fontSize(13)
              .fontColor(index === this.browsePath.length - 1 ?
                RokuricsColors.aqua : RokuricsColors.softText)
              .fontWeight(index === this.browsePath.length - 1 ?
                FontWeight.SemiBold : FontWeight.Regular)
          }
          .onClick(() => {
            if (index < this.browsePath.length - 1) {
              const parentPath: string[] = []
              for (let i = 0; i <= index; i++) parentPath.push(this.browsePath[i])
              this.navigateToPath(parentPath)
            }
          })
        })

        Text(`(${this.currentRecordings.length})`)
          .fontSize(11).fontColor(RokuricsColors.tertiaryText)
      }
      .width('100%')
      .padding({ left: 16, right: 16, bottom: 12 })

      // Sub-folder tiles if any at this level
      if (this.currentGroups.length > 0) {
        Column({ space: 6 }) {
          Text('子分类')
            .fontSize(12).fontWeight(FontWeight.Medium)
            .fontColor(RokuricsColors.tertiaryText)
            .padding({ left: 20 })
            .width('100%')
          Row({ space: 10 }) {
            ForEach(this.currentGroups, (group: FilingGroup) => {
              Button() {
                Column({ space: 6 }) {
                  // Small folder icon
                  Stack() {
                    Circle()
                      .width(34).height(34)
                      .fill(colorAlpha(this.folderColorMap[group.label] ?? RokuricsColors.aqua, '20'))
                    Column({ space: 0 }) {
                      Rect().width(18).height(3).radius(1.5)
                        .fill(this.folderColorMap[group.label] ?? RokuricsColors.aqua)
                        .translate({ x: -3, y: 0 })
                      Rect().width(24).height(16)
                        .radius([0, 5, 5, 5])
                        .fill(this.folderColorMap[group.label] ?? RokuricsColors.aqua)
                    }
                  }
                  .width(34).height(34)

                  Text(group.label)
                    .fontSize(13).fontWeight(FontWeight.SemiBold)
                    .fontColor(RokuricsColors.deepText)
                    .maxLines(1)
                  Text(`${group.count} 项`)
                    .fontSize(10).fontColor(RokuricsColors.tertiaryText)
                }
                .width('100%')
                .padding({ top: 14, bottom: 12, left: 10, right: 10 })
                .borderRadius(16)
                .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
                .border({
                  width: 1,
                  color: {
                    colors: [
                      [0xFFFFFF, 0.12],
                      [RokuricsColors.glassStroke, 0.08],
                      [RokuricsColors.glassStrokeAccent, 0.08]
                    ],
                    direction: GradientDirection.RightBottom
                  },
                  radius: 16
                } as BorderOptions)
                .shadow({
                  color: colorAlpha(RokuricsColors.shadowColor, '04'),
                  radius: 6,
                  offsetY: 3
                })
              }
              .backgroundColor(Color.Transparent)
              .layoutWeight(1)
              .onClick(() => {
                const newPath: string[] = []
                for (const p of this.browsePath) newPath.push(p)
                newPath.push(group.label)
                this.navigateToPath(newPath)
              })
            })
          }
          .width('100%')
        }
        .width('100%')
        .padding({ left: 16, right: 16, bottom: 12 })
        .flexShrink(0)
      }

      // Recording rows
      this.RecordingRows(this.currentRecordings)
    }
    .width('100%')
    .layoutWeight(1)
  }

  // ── Recording rows (shared) ──

  @Builder
  RecordingRows(items: RecordingMetadata[]) {
    if (items.length === 0) {
      Column({ space: 8 }) {
        Text('此分类下暂无录音')
          .fontSize(15).fontColor(RokuricsColors.tertiaryText)
      }
      .width('100%').height(200).justifyContent(FlexAlign.Center)
    } else {
    List({ space: 10 }) {
      ForEach(items, (recording: RecordingMetadata) => {
      ListItem() {
        Row() {
          // Waveform icon
          this.WaveformGlyph()

          Column({ space: 5 }) {
            Text(recording.title)
              .fontSize(16).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
              .maxLines(1).textOverflow({ overflow: TextOverflow.Ellipsis })

            Row({ space: 8 }) {
              Text(formatShortTime(recording.createdAt))
                .fontSize(12).fontColor(RokuricsColors.softText)
              Text('·')
                .fontSize(12).fontColor(RokuricsColors.tertiaryText)
              Text(formatDuration(recording.duration))
                .fontSize(12).fontColor(RokuricsColors.tertiaryText)

              if (recording.uploadStatus === 'uploaded') {
                Row({ space: 3 }) {
                  Circle().width(6).height(6).fill(RokuricsColors.mint)
                  Text('已上传').fontSize(10).fontColor(RokuricsColors.mint)
                }
                .padding({ left: 6, right: 6, top: 2, bottom: 2 })
                .borderRadius(6).backgroundColor(colorAlpha(RokuricsColors.mint, '18'))
              }
              if (recording.transcriptionStatus === 'transcribed') {
                DocBadgeIcon(14, RokuricsColors.aqua)
              }
              if (recording.noteStatus === 'generated') {
                NoteBadgeIcon(14, RokuricsColors.mint)
              }
            }
          }
          .layoutWeight(1)
          .alignItems(HorizontalAlign.Start)
          .margin({ left: 12 })

          Button() {
            TrashIcon(16, RokuricsColors.tertiaryText)
          }
          .width(36).height(36).backgroundColor(Color.Transparent)
          .onClick(() => this.deleteAndReload(recording.id))
        }
        .width('100%').padding(14).borderRadius(16)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
        .shadow({
          color: colorAlpha(RokuricsColors.shadowColor, '08'),
          radius: 10, offsetY: 5
        })
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
          radius: 16
        } as BorderOptions)
        .onClick(() => this.openDetail(recording.id))
      }
      })
    }
    .width('100%').layoutWeight(1)
    .padding({ left: 16, right: 16 }).scrollBar(BarState.Off)
    }
  }

  @Builder
  WaveformGlyph() {
    Stack() {
      Row({ space: 2.5 }) {
        Rect().width(3).height(8).radius(1.5).fill(RokuricsColors.aqua)
        Rect().width(3).height(18).radius(1.5).fill(RokuricsColors.aqua)
        Rect().width(3).height(12).radius(1.5).fill(RokuricsColors.aqua)
        Rect().width(3).height(24).radius(1.5).fill(RokuricsColors.aqua)
        Rect().width(3).height(15).radius(1.5).fill(RokuricsColors.aqua)
      }
      .justifyContent(FlexAlign.Center)
      .alignItems(VerticalAlign.Center)
    }
    .width(50).height(50).borderRadius(25)
    .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '56'))
    .shadow({ color: colorAlpha(RokuricsColors.shadowColor, '08'), radius: 9, offsetY: 4 })
    .border({
      width: 1,
      color: {
        colors: [
          [0xFFFFFF, 0.18],
          [RokuricsColors.glassStroke, 0.12],
          [RokuricsColors.aqua, 0.10]
        ],
        direction: GradientDirection.RightBottom
      },
      radius: 25
    } as BorderOptions)
  }

  @Builder
  TrashList() {
    if (this.trashedRecordings.length === 0) {
      Column({ space: 12 }) {
        Text('废纸篓为空').fontSize(16).fontColor(RokuricsColors.tertiaryText)
      }
      .width('100%').height('60%').justifyContent(FlexAlign.Center)
    } else {
      List({ space: 10 }) {
        ForEach(this.trashedRecordings, (recording: RecordingMetadata) => {
          ListItem() {
            Row() {
              Column({ space: 5 }) {
                Text(recording.title)
                  .fontSize(16).fontWeight(FontWeight.SemiBold)
                  .fontColor(RokuricsColors.deepText).maxLines(1)
                Row({ space: 8 }) {
                  Text(formatShortTime(recording.createdAt))
                    .fontSize(12).fontColor(RokuricsColors.softText)
                  Text(formatDuration(recording.duration))
                    .fontSize(12).fontColor(RokuricsColors.tertiaryText)
                }
              }
              .layoutWeight(1).alignItems(HorizontalAlign.Start)

              Button() {
                Text('恢复').fontSize(13).fontWeight(FontWeight.Medium)
                  .fontColor(RokuricsColors.aqua)
              }
              .backgroundColor(Color.Transparent).margin({ right: 8 })
              .onClick(() => this.restoreAndReload(recording.id))

              Button() {
                Text('删除').fontSize(13).fontWeight(FontWeight.Medium)
                  .fontColor(RokuricsColors.coral)
              }
              .backgroundColor(Color.Transparent)
              .onClick(() => this.permDeleteAndReload(recording.id))
            }
            .width('100%').padding(14).borderRadius(16)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
            .shadow({ color: colorAlpha(RokuricsColors.shadowColor, '08'), radius: 8, offsetY: 4 })
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, glassStrokeHighOpacity],
                  [RokuricsColors.glassStroke, glassStrokeMidOpacity],
                  [RokuricsColors.coral, 0.12]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 16
            } as BorderOptions)
          }
        })
      }
      .width('100%').layoutWeight(1)
      .padding({ left: 16, right: 16 }).scrollBar(BarState.Off)
    }
  }

  @Builder
  FilterChip(label: string, active: boolean, onToggle: (v: boolean) => void) {
    Row({ space: 4 }) {
      if (active) { Text('✓').fontSize(10).fontColor(Color.White) }
      Text(label).fontSize(11).fontWeight(FontWeight.Medium)
        .fontColor(active ? Color.White : RokuricsColors.softText)
    }
    .padding({ left: 10, right: 10, top: 5, bottom: 5 })
    .borderRadius(12)
    .backgroundColor(active ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
    .border({ width: active ? 0 : 1, color: colorAlpha(RokuricsColors.softText, '18'), radius: 12 })
  }

  private getSortedRecordings(): RecordingMetadata[] {
    let result = this.recordings
    const q = this.searchQuery.trim().toLowerCase()
    if (q.length > 0) {
      result = result.filter((r: RecordingMetadata) =>
        r.title.toLowerCase().indexOf(q) >= 0
      )
    }
    if (this.filterTranscribedActive) {
      result = result.filter((r: RecordingMetadata) =>
        r.transcriptionStatus === 'transcribed'
      )
    }
    if (this.filterNoteGeneratedActive) {
      result = result.filter((r: RecordingMetadata) =>
        r.noteStatus === 'generated'
      )
    }
    if (this.filterUploadedActive) {
      result = result.filter((r: RecordingMetadata) =>
        r.uploadStatus === 'uploaded'
      )
    }
    result.sort((a: RecordingMetadata, b: RecordingMetadata) => {
      if (this.sortOrder === 'newest') return b.createdAt.getTime() - a.createdAt.getTime()
      return a.createdAt.getTime() - b.createdAt.getTime()
    })
    return result
  }
}