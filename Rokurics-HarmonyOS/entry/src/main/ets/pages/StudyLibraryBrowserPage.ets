/**
 * Study library filing-hierarchy browser
 * Browse recordings organized by: type → subject → chapter → topic
 */
import { router } from '@kit.ArkUI'
import { getSharedRecordingManager, RecordingManager } from '../services/RecordingManager'
import { RecordingMetadata, StudyFilingPath, filingLevelTitle } from '../models/RecordingModels'
import { formatDuration, formatShortTime } from '../utils/FormatHelpers'
import { colorAlpha,  RokuricsColors, FontWeight } from '../utils/RokuricsTheme'
import { StudyFolderStore } from '../services/StudyFolderStore'

const HIERARCHY_LEVELS: string[] = ['type', 'subject', 'chapter', 'topic']

const FOLDER_COLORS: string[] = [RokuricsColors.aqua, RokuricsColors.mint, RokuricsColors.skyCyan, RokuricsColors.coral, '#B8A6D6', '#6B9FD4']

interface FilingGroup {
  path: string
  level: string
  label: string
  count: number
  children: FilingGroup[]
  recordings: RecordingMetadata[]
}

@Entry
@Component
struct StudyLibraryBrowserPage {
  private recordingManager: RecordingManager = getSharedRecordingManager()
  @State allRecordings: RecordingMetadata[] = []
  @State groups: FilingGroup[] = []
  @State currentPath: string[] = []
  @State breadcrumb: string[] = ['全部']
  @State showRecordings: RecordingMetadata[] = []
  @State showCreateFolder: boolean = false
  @State newFolderName: string = ''
  @State newFolderLevel: string = ''
  @State newFolderColor: string = RokuricsColors.aqua
  @State showRenameFolder: boolean = false
  @State renameTargetId: string = ''
  @State renameText: string = ''
  @State showDeleteConfirm: boolean = false
  @State deleteTargetId: string = ''
  @State deleteTargetName: string = ''
  @State editMessage: string = ''
  @State folderColorMap: Record<string, string> = {}

  aboutToAppear(): void {
    this.loadData()
  }

  async loadData(): Promise<void> {
    await this.recordingManager.reloadRecordings()
    this.allRecordings = this.recordingManager.recordings.filter(
      (r: RecordingMetadata) => !r.isDeleted
    )
    this.groups = this.buildGroups(this.allRecordings, 0, [])
    await this.loadFolderColors()
    this.navigateTo([])
  }

  private async loadFolderColors(): Promise<void> {
    const store = new StudyFolderStore(getContext(this))
    const allFolders = await store.listFolders()
    const map: Record<string, string> = {}
    for (const f of allFolders) {
      if (f.colorToken) {
        map[f.name] = f.colorToken
      }
    }
    this.folderColorMap = map
  }

  private buildGroups(recordings: RecordingMetadata[], levelIndex: number, parentPath: string[]): FilingGroup[] {
    if (levelIndex >= HIERARCHY_LEVELS.length) return []

    const level = HIERARCHY_LEVELS[levelIndex]
    const map = new Map<string, RecordingMetadata[]>()

    for (const r of recordings) {
      const value = r.studyFiling?.valueForLevel(level) ?? null
      const key = value || '未分类'
      if (!map.has(key)) map.set(key, [])
      map.get(key)!.push(r)
    }

    const groupList: FilingGroup[] = []
    const entries = Array.from(map.entries())
    entries.sort((a, b) => a[0].localeCompare(b[0]))

    for (const entry of entries) {
      const label = entry[0]
      const recs = entry[1]
      const path: string[] = []
      for (const p of parentPath) { path.push(p) }
      path.push(label)
      const children = levelIndex < HIERARCHY_LEVELS.length - 1
        ? this.buildGroups(recs, levelIndex + 1, path)
        : []
      const allChildren = this.flattenCount(children)
      const filingGroup: FilingGroup = {
        path: path.join(' / '),
        level: level,
        label: label,
        count: recs.length,
        children: children,
        recordings: levelIndex === HIERARCHY_LEVELS.length - 1 ? recs : allChildren
      }
      groupList.push(filingGroup)
    }

    return groupList
  }

  private flattenCount(groups: FilingGroup[]): RecordingMetadata[] {
    const result: RecordingMetadata[] = []
    for (const g of groups) {
      if (g.children.length === 0) {
        for (const r of g.recordings) result.push(r)
      } else {
        for (const r of this.flattenCount(g.children)) result.push(r)
      }
    }
    return result
  }

  private navigateTo(path: string[]): void {
    this.currentPath = path
    this.breadcrumb = ['全部', ...path]

    if (path.length === 0) {
      // Show top-level groups
      this.groups = this.buildGroups(this.allRecordings, 0, [])
      this.showRecordings = []
    } else {
      this.showRecordings = this.getRecordingsAtPath(path)
    }
  }

  private getRecordingsAtPath(path: string[]): RecordingMetadata[] {
    let filtered = this.allRecordings
    for (let i = 0; i < path.length; i++) {
      const level = HIERARCHY_LEVELS[i]
      const segment = path[i]
      filtered = filtered.filter((r: RecordingMetadata) => {
        const v = r.studyFiling?.valueForLevel(level) ?? null
        return (v || '未分类') === segment
      })
    }
    return filtered
  }

  private openDetail(id: string): void {
    this.getUIContext().getRouter().pushUrl({ url: 'pages/RecordingDetailPage', params: { recordingId: id } })
  }

  build() {
    Stack() {
    Column() {
      // Header
      Row() {
        Button() {
          Text('←').fontSize(20).fontColor(RokuricsColors.deepText)
        }
        .width(44).height(44).backgroundColor(Color.Transparent)
        .onClick(() => this.getUIContext().getRouter().back())

        Text('学习库浏览')
          .fontSize(24).fontWeight(FontWeight.Bold)
          .fontColor(RokuricsColors.deepText)
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 56, bottom: 12 })

      // Breadcrumb
      Row({ space: 4 }) {
        ForEach(this.breadcrumb, (crumb: string, index: number) => {
          Row() {
            Text(crumb)
              .fontSize(13)
              .fontColor(index === this.breadcrumb.length - 1 ?
                RokuricsColors.aqua : RokuricsColors.softText)
              .fontWeight(index === this.breadcrumb.length - 1 ?
                FontWeight.SemiBold : FontWeight.Regular)
          }
          .onClick(() => {
            if (index === 0) {
              this.navigateTo([])
            } else {
              this.navigateTo(this.currentPath.slice(0, index))
            }
          })
        })
        if (this.breadcrumb.length > 1) {
          Text(`(${this.showRecordings.length})`)
            .fontSize(12).fontColor(RokuricsColors.tertiaryText)
        }
      }
      .width('100%')
      .padding({ left: 16, right: 16, bottom: 12 })

      if (this.allRecordings.length === 0) {
        Column({ space: 12 }) {
          Text('暂无已归档录音').fontSize(16).fontColor(RokuricsColors.tertiaryText)
          Text('完成录音后在详情页设置学习归档').fontSize(13).fontColor(RokuricsColors.softText)
        }
        .width('100%').height('60%').justifyContent(FlexAlign.Center)
      } else if (this.currentPath.length === 0) {
        // Create folder button
        Row() {
          Blank()
          Button() {
            Text('+ 新建分类').fontSize(13).fontColor(RokuricsColors.aqua)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => {
            this.showCreateFolder = !this.showCreateFolder
            this.newFolderName = ''
            this.newFolderLevel = 'type'
          })
        }
        .width('100%')
        .padding({ left: 16, right: 16, bottom: 8 })

        // Create folder dialog inline
        if (this.showCreateFolder) {
          Column({ space: 8 }) {
            Row() {
              Text('新建分类').fontSize(14).fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.deepText)
              Blank()
              Button() { Text('✕').fontSize(14).fontColor(RokuricsColors.softText) }
                .width(32).height(32).backgroundColor(Color.Transparent)
                .onClick(() => { this.showCreateFolder = false })
            }.width('100%')

            Row({ space: 6 }) {
              ForEach(['type', 'subject', 'chapter'], (level: string) => {
                Text(filingLevelTitle(level))
                  .fontSize(12)
                  .fontColor(this.newFolderLevel === level ? Color.White : RokuricsColors.softText)
                  .padding({ left: 10, right: 10, top: 5, bottom: 5 })
                  .borderRadius(12)
                  .backgroundColor(this.newFolderLevel === level ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'))
                  .onClick(() => { this.newFolderLevel = level })
              })
            }

            // Color chips
            Row({ space: 6 }) {
              Text('颜色').fontSize(12).fontColor(RokuricsColors.softText).margin({ right: 4 })
              ForEach(FOLDER_COLORS, (color: string) => {
                Row() {
                  if (this.newFolderColor === color) {
                    Text('✓').fontSize(10).fontColor(Color.White)
                  }
                }
                .width(24).height(24).borderRadius(12)
                .backgroundColor(color)
                .justifyContent(FlexAlign.Center)
                .border({ width: this.newFolderColor === color ? 2 : 0, color: Color.White })
                .onClick(() => { this.newFolderColor = color })
              })
            }

            Row({ space: 8 }) {
              TextInput({ text: this.newFolderName, placeholder: '分类名称' })
                .fontSize(14).layoutWeight(1)
                .borderRadius(8)
                .padding({ left: 12, right: 12, top: 8, bottom: 8 })
                .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'))
                .onChange((v: string) => { this.newFolderName = v })
              Button() {
                Text('创建').fontSize(13).fontColor(Color.White)
              }
              .padding({ left: 16, right: 16, top: 8, bottom: 8 })
              .borderRadius(8)
              .backgroundColor(this.newFolderName.trim().length > 0 ? RokuricsColors.aqua : RokuricsColors.tertiaryText)
              .enabled(this.newFolderName.trim().length > 0)
              .onClick(async () => { await this.createFolder() })
            }
            .width('100%')
          }
          .width('90%')
          .padding(14).borderRadius(14)
          .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'))
          .margin({ left: 16, right: 16, bottom: 8 })
        }

        // Group view - show filing categories
        List({ space: 8 }) {
          ForEach(this.groups, (group: FilingGroup) => {
            ListItem() {
              Row() {
                Column({ space: 4 }) {
                  Text(group.label)
                    .fontSize(16).fontWeight(FontWeight.SemiBold)
                    .fontColor(RokuricsColors.deepText)
                  Text(`${filingLevelTitle(group.level)} · ${group.count} 条录音`)
                    .fontSize(12).fontColor(RokuricsColors.tertiaryText)
                }
                .layoutWeight(1).alignItems(HorizontalAlign.Start)

                // Context menu for folder actions
                Button() {
                  Text('…').fontSize(18).fontColor(RokuricsColors.softText)
                    .fontWeight(FontWeight.Bold)
                }
                .width(32).height(32).backgroundColor(Color.Transparent)
                .margin({ right: 4 })
                .onClick(() => {
                  this.renameTargetId = group.label
                  this.renameText = group.label
                  this.showRenameFolder = true
                })

                Button() {
                  Text('✕').fontSize(14).fontColor(RokuricsColors.coral)
                }
                .width(32).height(32).backgroundColor(Color.Transparent)
                .onClick(() => {
                  this.deleteTargetId = group.label
                  this.deleteTargetName = group.label
                  this.showDeleteConfirm = true
                })

                Text('›')
                  .fontSize(22).fontColor(RokuricsColors.softText)
              }
              .width('100%').padding(16).borderRadius(14)
              .backgroundColor(this.folderColorMap[group.label] ?
                colorAlpha(this.folderColorMap[group.label], '22') : colorAlpha(RokuricsColors.glassSurface, '66'))
              .border({ width: this.folderColorMap[group.label] ? 1 : 0,
                color: this.folderColorMap[group.label] ?
                colorAlpha(this.folderColorMap[group.label], '44') : Color.Transparent })
              .onClick(() => {
                const path = [group.label]
                this.navigateTo(path)
              })
            }
          })
        }
        .width('100%').layoutWeight(1)
        .padding({ left: 16, right: 16 }).scrollBar(BarState.Off)

        // Also show unfiled recordings section
        if (this.unfiledCount > 0) {
          Row() {
            Text(`未归档录音 (${this.unfiledCount})`)
              .fontSize(15).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
          }
          .width('100%').padding({ left: 16, right: 16, top: 8, bottom: 8 })
          .onClick(() => {
            this.showRecordings = this.unfiledRecordings
            this.breadcrumb = ['全部', '未归档']
          })

          List({ space: 6 }) {
            ForEach(this.unfiledRecordings, (recording: RecordingMetadata) => {
              ListItem() { this.RecordingRow(recording) }
            })
          }
          .width('100%').layoutWeight(1)
          .padding({ left: 16, right: 16 }).scrollBar(BarState.Off)
        }
      } else {
        // Recording list at current path
        if (this.showRecordings.length === 0) {
          Column({ space: 12 }) {
            Text('此分类下暂无录音').fontSize(16).fontColor(RokuricsColors.tertiaryText)
          }
          .width('100%').height('60%').justifyContent(FlexAlign.Center)
        } else {
          List({ space: 6 }) {
            ForEach(this.showRecordings, (recording: RecordingMetadata) => {
              ListItem() { this.RecordingRow(recording) }
            })
          }
          .width('100%').layoutWeight(1)
          .padding({ left: 16, right: 16 }).scrollBar(BarState.Off)
        }
      }
    }
    .width('100%').height('100%')
    .linearGradient({
      direction: GradientDirection.RightBottom,
      colors: [
        [RokuricsColors.pageGradientStart, 1.0],
        [RokuricsColors.pageGradientMid, 1.0],
        [RokuricsColors.pageGradientEnd, 1.0]
      ]
    })

    // Rename folder dialog
    if (this.showRenameFolder) {
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
          if (this.editMessage.length > 0) {
            Text(this.editMessage).fontSize(12).fontColor(RokuricsColors.coral)
          }
          Row({ space: 12 }) {
            Button() {
              Text('取消').fontSize(14).fontColor(RokuricsColors.softText)
            }
            .backgroundColor(Color.Transparent)
            .onClick(() => { this.showRenameFolder = false; this.editMessage = '' })
            Button() {
              Text('保存').fontSize(14).fontColor(Color.White)
            }
            .padding({ left: 24, right: 24, top: 10, bottom: 10 })
            .borderRadius(10).backgroundColor(RokuricsColors.aqua)
            .onClick(async () => { await this.commitRenameFolder() })
          }
          .width('100%').justifyContent(FlexAlign.End)
        }
        .padding(24).borderRadius(20).backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'E6')).width('85%')
        .shadow({ radius: 30, color: '#20000000' })
      }
      .width('100%').height('100%').justifyContent(FlexAlign.Center)
      .backgroundColor('#50000000').position({ x: 0, y: 0 })
      .onClick(() => { this.showRenameFolder = false; this.editMessage = '' })
    }

    // Delete confirmation dialog
    if (this.showDeleteConfirm) {
      Column() {
        Column({ space: 16 }) {
          Text('删除分类').fontSize(18).fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.deepText)
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
            .onClick(async () => { await this.commitDeleteFolder() })
          }
          .width('100%').justifyContent(FlexAlign.End)
        }
        .padding(24).borderRadius(20).backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'E6')).width('85%')
        .shadow({ radius: 30, color: '#20000000' })
      }
      .width('100%').height('100%').justifyContent(FlexAlign.Center)
      .backgroundColor('#50000000').position({ x: 0, y: 0 })
      .onClick(() => { this.showDeleteConfirm = false })
    }
    }
  }

  @Builder
  RecordingRow(recording: RecordingMetadata) {
    Row() {
      Column({ space: 3 }) {
        Text(recording.title)
          .fontSize(15).fontWeight(FontWeight.Medium)
          .fontColor(RokuricsColors.deepText)
          .maxLines(1).textOverflow({ overflow: TextOverflow.Ellipsis })

        Row({ space: 8 }) {
          Text(formatShortTime(recording.createdAt))
            .fontSize(11).fontColor(RokuricsColors.softText)
          Text(formatDuration(recording.duration))
            .fontSize(11).fontColor(RokuricsColors.tertiaryText)
          if (recording.transcriptionStatus === 'transcribed') {
            Text('📝')
              .fontSize(9)
          }
          if (recording.noteStatus === 'generated') {
            Text('📋')
              .fontSize(9)
          }
          if (recording.uploadStatus === 'uploaded') {
            Text('☁')
              .fontSize(9)
          }
        }
      }
      .layoutWeight(1).alignItems(HorizontalAlign.Start)

      Image($r('sys.symbol.chevron_right'))
        .width(16).height(16)
        .fillColor(RokuricsColors.softText)
    }
    .width('100%').padding({ left: 14, right: 14, top: 12, bottom: 12 })
    .borderRadius(12)
    .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '50'))
    .onClick(() => this.openDetail(recording.id))
  }

  private async createFolder(): Promise<void> {
    const name = this.newFolderName.trim()
    if (name.length === 0) return

    const store = new StudyFolderStore(getContext(this))
    const record = await store.createFolder(name, this.newFolderLevel, this.currentPath)
    await store.setColorToken(record.id, this.newFolderColor)
    this.showCreateFolder = false
    this.newFolderName = ''
    this.newFolderColor = RokuricsColors.aqua
    await this.loadData()
  }

  private async commitRenameFolder(): Promise<void> {
    const newName = this.renameText.trim()
    if (newName.length === 0 || this.renameTargetId.length === 0) {
      this.editMessage = '名称不能为空'
      return
    }
    if (newName === this.renameTargetId) {
      this.showRenameFolder = false
      return
    }

    // Determine level from current path depth
    const levelIdx = this.currentPath.length
    if (levelIdx >= HIERARCHY_LEVELS.length) {
      this.showRenameFolder = false
      return
    }
    const level = HIERARCHY_LEVELS[levelIdx]

    const toUpdate = this.allRecordings.filter((r: RecordingMetadata) => {
      const v = r.studyFiling?.valueForLevel(level) ?? null
      return v === this.renameTargetId || (v === null && this.renameTargetId === '未分类')
    })

    for (const r of toUpdate) {
      const filing = r.studyFiling ?? new StudyFilingPath()
      if (level === 'type') filing.type = newName
      else if (level === 'subject') filing.subject = newName
      else if (level === 'chapter') filing.chapter = newName
      else if (level === 'topic') filing.topic = newName
      await this.recordingManager.updateStudyFiling(r.id, filing)
    }

    // Also update persisted folder store
    const store = new StudyFolderStore(getContext(this))
    const folders = await store.listFolders(level)
    const match = folders.find(f => f.name === this.renameTargetId)
    if (match) await store.renameFolder(match.id, newName)

    this.showRenameFolder = false
    this.editMessage = ''
    await this.loadData()
  }

  private async commitDeleteFolder(): Promise<void> {
    if (this.deleteTargetId.length === 0) return

    const levelIdx = this.currentPath.length
    if (levelIdx >= HIERARCHY_LEVELS.length) {
      this.showDeleteConfirm = false
      return
    }
    const level = HIERARCHY_LEVELS[levelIdx]

    const toClear = this.allRecordings.filter((r: RecordingMetadata) => {
      const v = r.studyFiling?.valueForLevel(level) ?? null
      return v === this.deleteTargetId || (v === null && this.deleteTargetId === '未分类')
    })

    for (const r of toClear) {
      const filing = r.studyFiling ?? new StudyFilingPath()
      if (level === 'type') filing.type = null
      else if (level === 'subject') filing.subject = null
      else if (level === 'chapter') filing.chapter = null
      else if (level === 'topic') filing.topic = null
      await this.recordingManager.updateStudyFiling(r.id, filing)
    }

    // Also delete from persisted folder store
    const store = new StudyFolderStore(getContext(this))
    const folders = await store.listFolders(level)
    const match = folders.find(f => f.name === this.deleteTargetId)
    if (match) await store.deleteFolder(match.id)

    this.showDeleteConfirm = false
    await this.loadData()
  }

  private get unfiledRecordings(): RecordingMetadata[] {
    return this.allRecordings.filter((r: RecordingMetadata) =>
      !r.studyFiling || r.studyFiling.isEmpty
    )
  }

  private get unfiledCount(): number {
    return this.unfiledRecordings.length
  }
}
