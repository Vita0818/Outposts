/**
 * Study library filing models
 * Ported from iOS Rokurics/StudyFilingModels.swift and StudyLibraryStore.swift
 *
 * Contains: StudyItemMetadata, StudyFolderMetadata, StudyMetadataIndex,
 * StudyHierarchyRule, StudyFolderLevel, StudyBrowsePath,
 * StudyLibraryBrowser, RecordingReceiveRecord, StudyPathSanitizer, etc.
 */

// =========================================================================
// Type aliases
// =========================================================================
export type StudyItemID = string
export type StudyFolderID = string

// =========================================================================
// String helpers
// =========================================================================
function nilIfEmpty(value: string | null | undefined): string | null {
  if (!value) return null
  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : null
}

function normalized(value: string | null | undefined): string | null {
  return nilIfEmpty(value)
}

// =========================================================================
// Array helpers
// =========================================================================
function arraySafe<T>(arr: T[], index: number): T | undefined {
  return index >= 0 && index < arr.length ? arr[index] : undefined
}

function uniqueIDs(values: string[]): string[] {
  const seen = new Set<string>()
  const result: string[] = []
  for (const value of values) {
    const n = normalized(value)
    if (!n || seen.has(n)) continue
    seen.add(n)
    result.push(n)
  }
  return result
}

function uniqueStable(values: string[]): string[] {
  const seen = new Set<string>()
  const result: string[] = []
  for (const value of values) {
    const key = value.toLowerCase()
    if (seen.has(key)) continue
    seen.add(key)
    result.push(value)
  }
  return result
}

// =========================================================================
// StudyItemKind
// =========================================================================
export enum StudyItemKind {
  RECORDING_BUNDLE = 'recordingBundle',
  STANDALONE_NOTE = 'standaloneNote'
}

// =========================================================================
// ProcessingMode
// =========================================================================
export enum ProcessingMode {
  SINGLE_PASS = 'singlePass',
  CHUNKED = 'chunked',
  SECTIONED = 'sectioned'
}

// =========================================================================
// RecordingTranscriptionChunkRecord
// =========================================================================
export class RecordingTranscriptionChunkRecord {
  index: number = 0
  start: number | null = null
  end: number | null = null
  status: string | null = null

  static fromJSON(json: Record<string, Object>): RecordingTranscriptionChunkRecord {
    const r = new RecordingTranscriptionChunkRecord()
    r.index = (json['index'] as number) ?? 0
    r.start = (json['start'] as number) ?? null
    r.end = (json['end'] as number) ?? null
    r.status = (json['status'] as string) ?? null
    return r
  }
}

// =========================================================================
// RecordingNoteSectionRecord
// =========================================================================
export class RecordingNoteSectionRecord {
  index: number = 0
  sourceStart: number | null = null
  sourceEnd: number | null = null
  status: string | null = null
  sectionNoteRelativePath: string | null = null

  static fromJSON(json: Record<string, Object>): RecordingNoteSectionRecord {
    const r = new RecordingNoteSectionRecord()
    r.index = (json['index'] as number) ?? 0
    r.sourceStart = (json['sourceStart'] as number) ?? null
    r.sourceEnd = (json['sourceEnd'] as number) ?? null
    r.status = (json['status'] as string) ?? null
    r.sectionNoteRelativePath = (json['sectionNoteRelativePath'] as string) ?? null
    return r
  }
}

// =========================================================================
// StudyTag
// =========================================================================
export class StudyTag {
  id: string
  namespace: string
  value: string
  displayName: string | null
  createdAt: string | null

  constructor(
    namespace: string,
    value: string,
    id?: string | null,
    displayName?: string | null,
    createdAt?: string | null
  ) {
    const normalizedNamespace = StudyTag.normalizedNamespace(namespace)
    const normalizedValue = StudyTag.normalizedValue(value)
    this.namespace = normalizedNamespace
    this.value = normalizedValue
    this.displayName = nilIfEmpty(displayName) ?? null
    this.createdAt = createdAt ?? new Date().toISOString()
    this.id = nilIfEmpty(id) ?? StudyTag.makeID(normalizedNamespace, normalizedValue)
  }

  get displayTitle(): string {
    return nilIfEmpty(this.displayName) ?? this.value
  }

  static normalizedNamespace(namespace: string): string {
    const n = namespace.trim().toLowerCase()
    return n.length > 0 ? n : 'custom'
  }

  static normalizedValue(value: string): string {
    return value.trim()
  }

  static makeID(namespace: string, value: string): string {
    return `${StudyTag.normalizedNamespace(namespace)}:${StudyTag.normalizedValue(value).toLowerCase()}`
  }

  static fromJSON(json: Record<string, Object>): StudyTag {
    return new StudyTag(
      (json['namespace'] as string) ?? 'custom',
      (json['value'] as string) ?? '',
      (json['id'] as string) ?? null,
      (json['displayName'] as string) ?? null,
      (json['createdAt'] as string) ?? null
    )
  }
}

// =========================================================================
// StudyTagList
// =========================================================================
export class StudyTagList {
  static unique(tags: StudyTag[]): StudyTag[] {
    const seen = new Set<string>()
    const result: StudyTag[] = []
    for (const tag of tags) {
      const value = StudyTag.normalizedValue(tag.value)
      if (value.length === 0) continue
      const normalizedTag = new StudyTag(
        tag.namespace,
        value,
        tag.id,
        tag.displayName,
        tag.createdAt
      )
      const key = `${normalizedTag.namespace}${normalizedTag.value.toLowerCase()}`
      if (seen.has(key)) continue
      seen.add(key)
      result.push(normalizedTag)
    }
    return result
  }
}

// =========================================================================
// StudyFilingPath (extended from iOS to include full logic)
// =========================================================================
export class StudyFilingPath {
  type: string | null = null
  subject: string | null = null
  chapter: string | null = null
  topic: string | null = null

  constructor(type?: string | null, subject?: string | null, chapter?: string | null, topic?: string | null) {
    this.type = StudyFilingPath.normalized(type)
    this.subject = StudyFilingPath.normalized(subject)
    this.chapter = StudyFilingPath.normalized(chapter)
    this.topic = StudyFilingPath.normalized(topic)
  }

  get isEmpty(): boolean {
    return !this.type && !this.subject && !this.chapter && !this.topic
  }

  get displaySummary(): string {
    const parts = [this.type, this.subject, this.chapter, this.topic].filter((p): p is string => !!p)
    return parts.length > 0 ? parts.join(' / ') : StudyFilingPath.UNCategorizedTitle
  }

  valueForLevel(level: string): string | null {
    switch (StudyTag.normalizedNamespace(level)) {
      case 'type': return this.type
      case 'subject': return this.subject
      case 'chapter': return this.chapter
      case 'topic': return this.topic
      default: return null
    }
  }

  valueForFolderLevel(level: StudyFolderLevel): string | null {
    return this.valueForLevel(level)
  }

  suggestedTitle(defaultTitle: string): string {
    const parts = [this.subject, this.chapter, this.topic].filter((p): p is string => !!p)
    if (parts.length > 0) return parts.join(' · ')
    return this.type ?? defaultTitle
  }

  static normalized(value?: string | null): string | null {
    return nilIfEmpty(value)
  }

  clone(): StudyFilingPath {
    return new StudyFilingPath(this.type, this.subject, this.chapter, this.topic)
  }

  static fromJSON(json: Record<string, string> | null): StudyFilingPath {
    if (!json) return new StudyFilingPath()
    return new StudyFilingPath(json['type'], json['subject'], json['chapter'], json['topic'])
  }

  toJSON(): Record<string, string | null> {
    return { type: this.type, subject: this.subject, chapter: this.chapter, topic: this.topic }
  }

  static readonly UNCategorizedTitle = '未分类'
  static readonly MissingTitle = '未填写'
}

// =========================================================================
// StudyFilingLevel (recording-level filing)
// =========================================================================
export enum StudyFilingLevel {
  TYPE = 'type',
  SUBJECT = 'subject',
  CHAPTER = 'chapter',
  TOPIC = 'topic'
}

export function filingLevelTitle(level: StudyFilingLevel): string {
  switch (level) {
    case StudyFilingLevel.TYPE: return '门类'
    case StudyFilingLevel.SUBJECT: return '课程'
    case StudyFilingLevel.CHAPTER: return '章节'
    case StudyFilingLevel.TOPIC: return '主题'
  }
}

// =========================================================================
// StudyFolderLevel
// =========================================================================
export enum StudyFolderLevel {
  TYPE = 'type',
  SUBJECT = 'subject',
  CHAPTER = 'chapter',
  TOPIC = 'topic',
  CUSTOM = 'custom'
}

export function folderLevelTitle(level: StudyFolderLevel): string {
  switch (level) {
    case StudyFolderLevel.TYPE: return '门类'
    case StudyFolderLevel.SUBJECT: return '课程'
    case StudyFolderLevel.CHAPTER: return '章节'
    case StudyFolderLevel.TOPIC: return '主题'
    case StudyFolderLevel.CUSTOM: return '文件夹'
  }
}

export function folderLevelNext(level: StudyFolderLevel): StudyFolderLevel | null {
  switch (level) {
    case StudyFolderLevel.TYPE: return StudyFolderLevel.SUBJECT
    case StudyFolderLevel.SUBJECT: return StudyFolderLevel.CHAPTER
    case StudyFolderLevel.CHAPTER: return StudyFolderLevel.TOPIC
    default: return null
  }
}

// =========================================================================
// StudyFolderColorToken
// =========================================================================
export enum StudyFolderColorToken {
  DEFAULT = 'default',
  RED = 'red',
  ORANGE = 'orange',
  YELLOW = 'yellow',
  GREEN = 'green',
  MINT = 'mint',
  TEAL = 'teal',
  CYAN = 'cyan',
  BLUE = 'blue',
  INDIGO = 'indigo',
  PURPLE = 'purple',
  GRAY = 'gray'
}

// =========================================================================
// StudyHierarchyRule
// =========================================================================
export class StudyHierarchyRule {
  id: string
  name: string
  levels: string[]

  constructor(id: string = 'course-view', name: string = '课程视图', levels: string[] = []) {
    this.id = id
    this.name = nilIfEmpty(name.trim()) ?? '学习视图'
    this.levels = levels.map(l => StudyTag.normalizedNamespace(l)).filter(l => l.length > 0)
  }

  static readonly defaultCourseView = new StudyHierarchyRule(
    'course-view',
    '课程视图',
    ['type', 'subject', 'chapter', 'topic']
  )

  static readonly uncategorizedValue = StudyFilingPath.UNCategorizedTitle
  static readonly missingValue = StudyFilingPath.MissingTitle

  static fromJSON(json: Record<string, Object>): StudyHierarchyRule {
    const id = (json['id'] as string) ?? 'course-view'
    const name = (json['name'] as string) ?? '课程视图'
    const levels = (json['levels'] as string[]) ?? []
    return new StudyHierarchyRule(id, name, levels)
  }
}

// =========================================================================
// StudyBrowsePath
// =========================================================================
export class StudyBrowsePath {
  components: string[] = []

  constructor(components: string[] = []) {
    this.components = [...components]
  }

  get isRoot(): boolean {
    return this.components.length === 0
  }

  get depth(): number {
    return this.components.length
  }

  get storageKey(): string {
    return this.components.join('')
  }

  get parent(): StudyBrowsePath {
    if (this.components.length === 0) return this
    return new StudyBrowsePath(this.components.slice(0, -1))
  }

  get isUncategorizedTypeSelection(): boolean {
    return this.components.length === 1 && this.components[0] === StudyHierarchyRule.uncategorizedValue
  }

  appending(value: string): StudyBrowsePath {
    const updated = [...this.components, value]
    return new StudyBrowsePath(updated)
  }

  truncatedTo(depth: number): StudyBrowsePath {
    return new StudyBrowsePath(this.components.slice(0, Math.max(0, depth)))
  }
}

// =========================================================================
// StudyBrowseFolder
// =========================================================================
export class StudyBrowseFolder {
  id: string
  folderID: StudyFolderID | null
  levelKey: string
  title: string
  itemCount: number
  path: StudyBrowsePath
  colorToken: StudyFolderColorToken | null

  constructor(
    id: string,
    folderID: StudyFolderID | null,
    levelKey: string,
    title: string,
    itemCount: number,
    path: StudyBrowsePath,
    colorToken: StudyFolderColorToken | null = null
  ) {
    this.id = id
    this.folderID = folderID
    this.levelKey = levelKey
    this.title = title
    this.itemCount = itemCount
    this.path = path
    this.colorToken = colorToken
  }

  get isFallback(): boolean {
    return this.title === StudyHierarchyRule.uncategorizedValue || this.title === StudyHierarchyRule.missingValue
  }
}

// =========================================================================
// StudyBrowseContent
// =========================================================================
export class StudyBrowseContent {
  path: StudyBrowsePath
  folders: StudyBrowseFolder[]
  items: StudyItemMetadata[]

  constructor(path: StudyBrowsePath, folders: StudyBrowseFolder[] = [], items: StudyItemMetadata[] = []) {
    this.path = path
    this.folders = folders
    this.items = items
  }
}

// =========================================================================
// StudyPathSanitizer
// Ported from iOS StudyPathSanitizer
// =========================================================================
export class StudyPathSanitizer {
  static sanitizedPathComponent(value: string): string {
    return nilIfEmpty(
      StudyPathSanitizer.sanitizedFileName(value)
        .replace(/\./g, '_')
        .replace(/^[ _-]+|[ _-]+$/g, '')
    ) ?? 'recording'
  }

  private static sanitizedFileName(value: string | null | undefined): string {
    const rawName = (value ?? '').trim()
    const lastPathComponent = rawName.length === 0 ? 'recording' : rawName.split('/').pop() ?? rawName
    const sanitized = lastPathComponent
      .split('')
      .map(ch => /[a-zA-Z0-9._-]/.test(ch) ? ch : '_')
      .join('')
      .replace(/_+/g, '_')
      .replace(/^\.+|\.+$/g, '')
    return sanitized
  }
}

// =========================================================================
// StudyItemMetadata
// Ported from iOS StudyItemMetadata
// =========================================================================
export class StudyItemMetadata {
  itemID: StudyItemID
  kind: StudyItemKind
  title: string
  createdAt: string // ISO8601
  updatedAt: string // ISO8601
  filing: StudyFilingPath
  tags: StudyTag[]
  folderIDs: StudyFolderID[]
  customProperties: Record<string, string>
  recordingID: string | null
  sanitizedRecordingID: string | null
  duration: number | null
  audioRelativePath: string | null
  receiveRelativePath: string | null
  transcriptRelativePath: string | null
  transcriptMarkdownRelativePath: string | null
  noteRelativePath: string | null
  transcriptionStatus: string | null
  noteStatus: string | null
  noteSections: RecordingNoteSectionRecord[] | null
  sourceDescription: string | null
  isTrashed: boolean
  trashedAt: string | null
  modifiedByDeviceID: string | null
  syncConflictStatus: string | null

  constructor(fields: StudyItemMetadataFields) {
    const normalizedRecordingID = normalized(fields.recordingID)
    const resolvedKind = !normalizedRecordingID && fields.kind === StudyItemKind.RECORDING_BUNDLE
      ? StudyItemKind.STANDALONE_NOTE : fields.kind
    const resolvedItemID = normalized(fields.itemID)
      ?? (normalizedRecordingID ? StudyItemMetadata.recordingBundleItemID(normalizedRecordingID) : null)
      ?? `item_note_${StudyItemMetadata.makeUUID().toLowerCase()}`

    this.itemID = resolvedItemID
    this.kind = resolvedKind
    this.title = normalized(fields.title) ?? (resolvedKind === StudyItemKind.STANDALONE_NOTE ? '未命名笔记' : '未命名录音')
    this.createdAt = fields.createdAt
    this.updatedAt = fields.updatedAt ?? fields.createdAt
    this.filing = fields.filing.isEmpty ? new StudyFilingPath() : fields.filing
    this.tags = StudyTagList.unique(fields.tags)
    this.folderIDs = uniqueIDs(fields.folderIDs)
    this.customProperties = { ...fields.customProperties }
    this.recordingID = normalizedRecordingID
    this.sanitizedRecordingID = normalized(fields.sanitizedRecordingID)
      ?? (normalizedRecordingID ? StudyPathSanitizer.sanitizedPathComponent(normalizedRecordingID) : null)
    this.duration = fields.duration
    this.audioRelativePath = normalized(fields.audioRelativePath)
    this.receiveRelativePath = normalized(fields.receiveRelativePath)
    this.transcriptRelativePath = normalized(fields.transcriptRelativePath)
    this.transcriptMarkdownRelativePath = normalized(fields.transcriptMarkdownRelativePath)
    this.noteRelativePath = normalized(fields.noteRelativePath)
    this.transcriptionStatus = normalized(fields.transcriptionStatus)
    this.noteStatus = normalized(fields.noteStatus)
    this.noteSections = fields.noteSections
    this.sourceDescription = normalized(fields.sourceDescription)
    this.isTrashed = fields.isTrashed ?? false
    this.trashedAt = fields.trashedAt ?? null
    this.modifiedByDeviceID = normalized(fields.modifiedByDeviceID)
    this.syncConflictStatus = normalized(fields.syncConflictStatus)
  }

  static recordingBundleItemID(recordingID: string): StudyItemID {
    return `item_recording_${StudyPathSanitizer.sanitizedPathComponent(recordingID)}`
  }

  static defaultFolderIDs(filing: StudyFilingPath): StudyFolderID[] {
    const path = StudyItemMetadata.effectiveFolderPath(filing)
    const deepestLevel = StudyFolderMetadata.deepestLevelIn(path)
    if (!deepestLevel) return []
    return [StudyFolderMetadata.folderIDFromLevelAndPath(deepestLevel, path)]
  }

  static effectiveFolderPath(filing: StudyFilingPath): StudyFilingPath {
    if (filing.isEmpty) {
      return new StudyFilingPath(StudyHierarchyRule.uncategorizedValue)
    }
    const hasTopic = filing.topic !== null
    const hasChapter = filing.chapter !== null || hasTopic
    const hasSubject = filing.subject !== null || hasChapter
    return new StudyFilingPath(
      filing.type ?? StudyHierarchyRule.uncategorizedValue,
      hasSubject ? (filing.subject ?? StudyHierarchyRule.missingValue) : null,
      hasChapter ? (filing.chapter ?? StudyHierarchyRule.missingValue) : null,
      filing.topic
    )
  }

  static uniqueIDs(values: string[]): string[] {
    return uniqueIDs(values)
  }

  static normalized(value?: string | null): string | null {
    return normalized(value)
  }

  get hasTranscript(): boolean {
    return this.transcriptMarkdownRelativePath !== null || this.transcriptRelativePath !== null
  }

  get hasNote(): boolean {
    return this.noteRelativePath !== null
  }

  get filingPath(): StudyFilingPath {
    return this.filing
  }

  get studyFiling(): StudyFilingPath | null {
    return this.filing.isEmpty ? null : this.filing
  }

  set studyFiling(value: StudyFilingPath | null) {
    this.filing = (value?.isEmpty ?? true) ? new StudyFilingPath() : (value ?? new StudyFilingPath())
  }

  get durationForDisplay(): number {
    return this.duration ?? 0
  }

  mergedWithCurrentRecording(recording: RecordingMetadataLike): StudyItemMetadata {
    const resolvedFiling = this.filing.isEmpty ? (recording.studyFiling ?? new StudyFilingPath()) : this.filing
    const resolvedFolderIDs = this.folderIDs.length === 0
      ? StudyItemMetadata.defaultFolderIDs(resolvedFiling) : this.folderIDs
    return new StudyItemMetadata({
      itemID: this.itemID,
      kind: StudyItemKind.RECORDING_BUNDLE,
      title: recording.title,
      createdAt: recording.createdAt instanceof Date ? recording.createdAt.toISOString() : String(recording.createdAt),
      updatedAt: this.updatedAt,
      filing: resolvedFiling,
      tags: this.tags,
      folderIDs: resolvedFolderIDs,
      customProperties: this.customProperties,
      recordingID: recording.id,
      sanitizedRecordingID: this.sanitizedRecordingID,
      duration: recording.duration,
      audioRelativePath: recording.relativeAudioPath,
      receiveRelativePath: this.receiveRelativePath,
      transcriptRelativePath: this.transcriptRelativePath,
      transcriptMarkdownRelativePath: this.transcriptMarkdownRelativePath,
      noteRelativePath: this.noteRelativePath,
      transcriptionStatus: recording.transcriptionStatus,
      noteStatus: recording.noteStatus,
      noteSections: this.noteSections,
      sourceDescription: this.sourceDescription,
      isTrashed: this.isTrashed || recording.isDeleted,
      trashedAt: recording.deletedAt instanceof Date ? recording.deletedAt.toISOString() : String(recording.deletedAt ?? ''),
      modifiedByDeviceID: this.modifiedByDeviceID,
      syncConflictStatus: this.syncConflictStatus
    })
  }

  static defaultMetadataForRecording(recording: RecordingMetadataLike): StudyItemMetadata {
    return new StudyItemMetadata({
      itemID: StudyItemMetadata.recordingBundleItemID(recording.id),
      kind: StudyItemKind.RECORDING_BUNDLE,
      title: recording.title,
      createdAt: recording.createdAt instanceof Date ? recording.createdAt.toISOString() : String(recording.createdAt),
      duration: recording.duration,
      audioRelativePath: recording.relativeAudioPath,
      receiveRelativePath: null,
      transcriptRelativePath: null,
      transcriptMarkdownRelativePath: null,
      noteRelativePath: null,
      tags: (recording.tags as string[] | undefined)?.map(t => new StudyTag('custom', t)) ?? [],
      customProperties: {},
      filing: recording.studyFiling ?? new StudyFilingPath(),
      folderIDs: [],
      transcriptionStatus: recording.transcriptionStatus,
      noteStatus: recording.noteStatus,
      sourceDescription: 'HarmonyOS',
      isTrashed: recording.isDeleted ?? false,
      trashedAt: recording.deletedAt instanceof Date ? recording.deletedAt.toISOString()
        : (recording.deletedAt ? String(recording.deletedAt) : null)
    })
  }

  static defaultMetadataForReceiveRecord(
    receiveRecord: RecordingReceiveRecord,
    receiveRelativePath: string | null
  ): StudyItemMetadata | null {
    const recordingID = normalized(receiveRecord.recordingID)
    if (!recordingID) return null
    return new StudyItemMetadata({
      itemID: StudyItemMetadata.recordingBundleItemID(recordingID),
      kind: StudyItemKind.RECORDING_BUNDLE,
      title: receiveRecord.normalizedTitle ?? receiveRecord.originalTitle ?? '未命名录音',
      createdAt: receiveRecord.createdAt ?? receiveRecord.receivedAt ?? new Date(0).toISOString(),
      duration: receiveRecord.duration ?? 0,
      audioRelativePath: receiveRecord.audioRelativePath,
      receiveRelativePath: receiveRelativePath,
      transcriptRelativePath: receiveRecord.transcriptRelativePath,
      transcriptMarkdownRelativePath: receiveRecord.transcriptMarkdownRelativePath,
      noteRelativePath: receiveRecord.noteRelativePath,
      tags: (receiveRecord.tags ?? []).map(t => new StudyTag('custom', t)),
      customProperties: {},
      filing: receiveRecord.studyFiling ?? new StudyFilingPath(),
      folderIDs: [],
      transcriptionStatus: receiveRecord.transcriptionStatus,
      noteStatus: receiveRecord.noteStatus,
      noteSections: receiveRecord.noteSections,
      sourceDescription: receiveRecord.sourceDeviceName,
      updatedAt: receiveRecord.updatedAt ?? new Date(0).toISOString()
    })
  }

  // Sync sanitized copy (port of syncSanitized)
  syncSanitized(modifiedByDeviceID: string | null = null): StudyItemMetadata {
    const copy = this.clone()
    copy.modifiedByDeviceID = copy.modifiedByDeviceID ?? modifiedByDeviceID
    copy.customProperties = filterSyncCustomProperties(this.customProperties)
    return copy
  }

  clone(): StudyItemMetadata {
    return new StudyItemMetadata({
      itemID: this.itemID,
      kind: this.kind,
      title: this.title,
      createdAt: this.createdAt,
      updatedAt: this.updatedAt,
      filing: this.filing.clone(),
      tags: this.tags.map(t => new StudyTag(t.namespace, t.value, t.id, t.displayName, t.createdAt)),
      folderIDs: [...this.folderIDs],
      customProperties: { ...this.customProperties },
      recordingID: this.recordingID,
      sanitizedRecordingID: this.sanitizedRecordingID,
      duration: this.duration,
      audioRelativePath: this.audioRelativePath,
      receiveRelativePath: this.receiveRelativePath,
      transcriptRelativePath: this.transcriptRelativePath,
      transcriptMarkdownRelativePath: this.transcriptMarkdownRelativePath,
      noteRelativePath: this.noteRelativePath,
      transcriptionStatus: this.transcriptionStatus,
      noteStatus: this.noteStatus,
      noteSections: this.noteSections ? this.noteSections.map(s => ({ ...s })) : null,
      sourceDescription: this.sourceDescription,
      isTrashed: this.isTrashed,
      trashedAt: this.trashedAt,
      modifiedByDeviceID: this.modifiedByDeviceID,
      syncConflictStatus: this.syncConflictStatus
    })
  }

  toJSON(): Record<string, Object> {
    const result: Record<string, Object> = {
      itemID: this.itemID,
      kind: this.kind,
      title: this.title,
      createdAt: this.createdAt,
      updatedAt: this.updatedAt,
      filing: this.filing.toJSON(),
      tags: this.tags.map(t => ({
        id: t.id,
        namespace: t.namespace,
        value: t.value,
        displayName: t.displayName,
        createdAt: t.createdAt
      })),
      folderIDs: this.folderIDs,
      customProperties: this.customProperties,
      isTrashed: this.isTrashed
    }
    if (this.recordingID) result['recordingID'] = this.recordingID
    if (this.sanitizedRecordingID) result['sanitizedRecordingID'] = this.sanitizedRecordingID
    if (this.duration !== null) result['duration'] = this.duration
    if (this.audioRelativePath) result['audioRelativePath'] = this.audioRelativePath
    if (this.receiveRelativePath) result['receiveRelativePath'] = this.receiveRelativePath
    if (this.transcriptRelativePath) result['transcriptRelativePath'] = this.transcriptRelativePath
    if (this.transcriptMarkdownRelativePath) result['transcriptMarkdownRelativePath'] = this.transcriptMarkdownRelativePath
    if (this.noteRelativePath) result['noteRelativePath'] = this.noteRelativePath
    if (this.transcriptionStatus) result['transcriptionStatus'] = this.transcriptionStatus
    if (this.noteStatus) result['noteStatus'] = this.noteStatus
    if (this.noteSections) result['noteSections'] = this.noteSections
    if (this.sourceDescription) result['sourceDescription'] = this.sourceDescription
    if (this.trashedAt) result['trashedAt'] = this.trashedAt
    if (this.modifiedByDeviceID) result['modifiedByDeviceID'] = this.modifiedByDeviceID
    if (this.syncConflictStatus) result['syncConflictStatus'] = this.syncConflictStatus
    return result
  }

  static fromJSON(json: Record<string, Object>): StudyItemMetadata {
    const recordingID = (json['recordingID'] as string) ?? null
    const itemID = (json['itemID'] as string) ?? null
      ?? (recordingID ? StudyItemMetadata.recordingBundleItemID(recordingID) : null)
    const kind = (json['kind'] as StudyItemKind) ?? (recordingID ? StudyItemKind.RECORDING_BUNDLE : StudyItemKind.STANDALONE_NOTE)
    const filingJSON = (json['filing'] ?? json['studyFiling']) as Record<string, string> | null
    const filing = StudyFilingPath.fromJSON(filingJSON)
    const tagsJSON = (json['tags'] as Array<Record<string, Object>>) ?? []
    const folderIDs = (json['folderIDs'] as string[]) ?? (filing.isEmpty ? [] : StudyItemMetadata.defaultFolderIDs(filing))
    const customProperties = (json['customProperties'] as Record<string, string>) ?? {}
    const noteSectionsJSON = json['noteSections'] as Array<Record<string, Object>> | null
    const isTrashed = (json['isTrashed'] as boolean) ?? false

    return new StudyItemMetadata({
      itemID: itemID ?? undefined,
      kind: kind ?? StudyItemKind.RECORDING_BUNDLE,
      title: (json['title'] as string) ?? '未命名录音',
      createdAt: (json['createdAt'] as string) ?? new Date(0).toISOString(),
      updatedAt: (json['updatedAt'] as string) ?? new Date(0).toISOString(),
      filing: filing,
      tags: tagsJSON.map(t => StudyTag.fromJSON(t)),
      folderIDs: folderIDs,
      customProperties: customProperties,
      recordingID: recordingID,
      sanitizedRecordingID: (json['sanitizedRecordingID'] as string) ?? null,
      duration: (json['duration'] as number) ?? null,
      audioRelativePath: (json['audioRelativePath'] as string) ?? null,
      receiveRelativePath: (json['receiveRelativePath'] as string) ?? null,
      transcriptRelativePath: (json['transcriptRelativePath'] as string) ?? null,
      transcriptMarkdownRelativePath: (json['transcriptMarkdownRelativePath'] as string) ?? null,
      noteRelativePath: (json['noteRelativePath'] as string) ?? null,
      transcriptionStatus: (json['transcriptionStatus'] as string) ?? null,
      noteStatus: (json['noteStatus'] as string) ?? null,
      noteSections: noteSectionsJSON ? noteSectionsJSON.map(s => RecordingNoteSectionRecord.fromJSON(s)) : null,
      sourceDescription: (json['sourceDescription'] as string) ?? null,
      isTrashed: isTrashed,
      trashedAt: (json['trashedAt'] as string) ?? null,
      modifiedByDeviceID: (json['modifiedByDeviceID'] as string) ?? null,
      syncConflictStatus: (json['syncConflictStatus'] as string) ?? null
    })
  }

  private static makeUUID(): string {
    const chars = 'abcdef0123456789'
    let id = ''
    for (let i = 0; i < 32; i++) {
      id += chars.charAt(Math.floor(Math.random() * chars.length))
    }
    return id.substring(0, 8) + '-' + id.substring(8, 12) + '-' +
      id.substring(12, 16) + '-' + id.substring(16, 20) + '-' + id.substring(20, 32)
  }
}

export interface StudyItemMetadataFields {
  itemID?: string | null
  kind: StudyItemKind
  title: string
  createdAt: string
  updatedAt?: string | null
  filing?: StudyFilingPath
  tags?: StudyTag[]
  folderIDs?: StudyFolderID[]
  customProperties?: Record<string, string>
  recordingID?: string | null
  sanitizedRecordingID?: string | null
  duration?: number | null
  audioRelativePath?: string | null
  receiveRelativePath?: string | null
  transcriptRelativePath?: string | null
  transcriptMarkdownRelativePath?: string | null
  noteRelativePath?: string | null
  transcriptionStatus?: string | null
  noteStatus?: string | null
  noteSections?: RecordingNoteSectionRecord[] | null
  sourceDescription?: string | null
  isTrashed?: boolean
  trashedAt?: string | null
  modifiedByDeviceID?: string | null
  syncConflictStatus?: string | null
}

// Minimal interface for RecordingMetadata compatibility
export interface RecordingMetadataLike {
  id: string
  title: string
  fileName?: string
  relativeAudioPath: string | null
  relativeMetadataPath?: string | null
  createdAt: Date | string
  endedAt?: Date | null
  duration: number
  format?: string
  codec?: string
  sampleRate?: number
  channels?: number
  bitrate?: number
  fileSize?: number
  uploadStatus?: string
  transcriptionStatus: string | null
  noteStatus: string | null
  tags?: string[]
  studyFiling?: StudyFilingPath | null
  isDeleted?: boolean
  deletedAt?: Date | string | null
}

// =========================================================================
// StudyFolderMetadata
// Ported from iOS StudyFolderMetadata
// =========================================================================
export class StudyFolderMetadata {
  folderID: StudyFolderID
  name: string
  level: StudyFolderLevel
  path: StudyFilingPath
  parentFolderID: StudyFolderID | null
  childFolderIDs: StudyFolderID[]
  itemIDs: StudyItemID[]
  createdAt: string // ISO8601
  updatedAt: string // ISO8601
  colorToken: StudyFolderColorToken | null
  isTrashed: boolean
  trashedAt: string | null
  customProperties: Record<string, string>
  modifiedByDeviceID: string | null
  syncConflictStatus: string | null

  constructor(fields: StudyFolderMetadataFields) {
    this.name = normalized(fields.name) ?? StudyHierarchyRule.missingValue
    this.level = fields.level
    this.path = fields.path
    this.folderID = normalized(fields.folderID) ?? StudyFolderMetadata.folderIDFromLevelAndPath(fields.level, fields.path)
    this.parentFolderID = normalized(fields.parentFolderID)
    this.childFolderIDs = uniqueIDs(fields.childFolderIDs)
    this.itemIDs = uniqueIDs(fields.itemIDs)
    this.createdAt = fields.createdAt
    this.updatedAt = fields.updatedAt ?? fields.createdAt
    this.colorToken = fields.colorToken ?? null
    this.isTrashed = fields.isTrashed ?? false
    this.trashedAt = fields.trashedAt ?? null
    this.customProperties = { ...fields.customProperties }
    this.modifiedByDeviceID = normalized(fields.modifiedByDeviceID)
    this.syncConflictStatus = normalized(fields.syncConflictStatus)
  }

  get pathComponents(): string[] {
    return StudyFolderMetadata.pathComponentsForPath(this.path, this.level)
  }

  static folderIDFromLevelAndPath(level: StudyFolderLevel, path: StudyFilingPath): StudyFolderID {
    const components = StudyFolderMetadata.pathComponentsForPath(path, level)
    const raw = [level, ...components].join('_')
    return `folder_${StudyPathSanitizer.sanitizedPathComponent(raw)}`
  }

  static pathComponentsForPath(path: StudyFilingPath, level: StudyFolderLevel): string[] {
    const values: Array<[StudyFolderLevel, string | null]> = [
      [StudyFolderLevel.TYPE, path.type],
      [StudyFolderLevel.SUBJECT, path.subject],
      [StudyFolderLevel.CHAPTER, path.chapter],
      [StudyFolderLevel.TOPIC, path.topic]
    ]
    const result: string[] = []
    for (const [candidateLevel, value] of values) {
      const v = StudyFilingPath.normalized(value)
      if (!v) break
      result.push(v)
      if (candidateLevel === level) break
    }
    return result
  }

  static deepestLevelIn(path: StudyFilingPath): StudyFolderLevel | null {
    if (path.topic !== null) return StudyFolderLevel.TOPIC
    if (path.chapter !== null) return StudyFolderLevel.CHAPTER
    if (path.subject !== null) return StudyFolderLevel.SUBJECT
    if (path.type !== null) return StudyFolderLevel.TYPE
    return null
  }

  static levelForDepth(depth: number): StudyFolderLevel | null {
    switch (depth) {
      case 0: return StudyFolderLevel.TYPE
      case 1: return StudyFolderLevel.SUBJECT
      case 2: return StudyFolderLevel.CHAPTER
      case 3: return StudyFolderLevel.TOPIC
      default: return null
    }
  }

  static filingPathForComponents(components: string[]): StudyFilingPath {
    return new StudyFilingPath(
      arraySafe(components, 0),
      arraySafe(components, 1),
      arraySafe(components, 2),
      arraySafe(components, 3)
    )
  }

  syncSanitized(modifiedByDeviceID: string | null = null): StudyFolderMetadata {
    const copy = this.clone()
    copy.modifiedByDeviceID = copy.modifiedByDeviceID ?? modifiedByDeviceID
    copy.customProperties = filterSyncCustomProperties(this.customProperties)
    copy.itemIDs = uniqueIDs(copy.itemIDs)
    copy.childFolderIDs = uniqueIDs(copy.childFolderIDs)
    return copy
  }

  clone(): StudyFolderMetadata {
    return new StudyFolderMetadata({
      folderID: this.folderID,
      name: this.name,
      level: this.level,
      path: this.path.clone(),
      parentFolderID: this.parentFolderID,
      childFolderIDs: [...this.childFolderIDs],
      itemIDs: [...this.itemIDs],
      createdAt: this.createdAt,
      updatedAt: this.updatedAt,
      colorToken: this.colorToken,
      isTrashed: this.isTrashed,
      trashedAt: this.trashedAt,
      customProperties: { ...this.customProperties },
      modifiedByDeviceID: this.modifiedByDeviceID,
      syncConflictStatus: this.syncConflictStatus
    })
  }

  toJSON(): Record<string, Object> {
    const result: Record<string, Object> = {
      folderID: this.folderID,
      name: this.name,
      level: this.level,
      path: this.path.toJSON(),
      parentFolderID: this.parentFolderID,
      childFolderIDs: this.childFolderIDs,
      itemIDs: this.itemIDs,
      createdAt: this.createdAt,
      updatedAt: this.updatedAt,
      customProperties: this.customProperties,
      isTrashed: this.isTrashed
    }
    if (this.colorToken) result['colorToken'] = this.colorToken
    if (this.trashedAt) result['trashedAt'] = this.trashedAt
    if (this.modifiedByDeviceID) result['modifiedByDeviceID'] = this.modifiedByDeviceID
    if (this.syncConflictStatus) result['syncConflictStatus'] = this.syncConflictStatus
    return result
  }

  static fromJSON(json: Record<string, Object>): StudyFolderMetadata {
    return new StudyFolderMetadata({
      folderID: (json['folderID'] as string) ?? undefined,
      name: (json['name'] as string) ?? StudyHierarchyRule.missingValue,
      level: (json['level'] as StudyFolderLevel) ?? StudyFolderLevel.CUSTOM,
      path: StudyFilingPath.fromJSON(json['path'] as Record<string, string> | null),
      parentFolderID: (json['parentFolderID'] as string) ?? null,
      childFolderIDs: (json['childFolderIDs'] as string[]) ?? [],
      itemIDs: (json['itemIDs'] as string[]) ?? [],
      createdAt: (json['createdAt'] as string) ?? new Date().toISOString(),
      updatedAt: (json['updatedAt'] as string) ?? new Date().toISOString(),
      colorToken: (json['colorToken'] as StudyFolderColorToken) ?? null,
      isTrashed: (json['isTrashed'] as boolean) ?? false,
      trashedAt: (json['trashedAt'] as string) ?? null,
      customProperties: (json['customProperties'] as Record<string, string>) ?? {},
      modifiedByDeviceID: (json['modifiedByDeviceID'] as string) ?? null,
      syncConflictStatus: (json['syncConflictStatus'] as string) ?? null
    })
  }
}

export interface StudyFolderMetadataFields {
  folderID?: string | null
  name: string
  level: StudyFolderLevel
  path: StudyFilingPath
  parentFolderID?: string | null
  childFolderIDs?: StudyFolderID[]
  itemIDs?: StudyItemID[]
  createdAt?: string
  updatedAt?: string | null
  colorToken?: StudyFolderColorToken | null
  isTrashed?: boolean
  trashedAt?: string | null
  customProperties?: Record<string, string>
  modifiedByDeviceID?: string | null
  syncConflictStatus?: string | null
}

// =========================================================================
// StudyMetadataIndex
// Ported from iOS StudyMetadataIndex
// =========================================================================
export class StudyMetadataIndex {
  itemMetadataFilesByItemID: Record<string, string> = {}
  itemMetadataFilesByRecordingID: Record<string, string> = {}
  folderMetadataFilesByFolderID: Record<string, string> = {}
  updatedAt: string = new Date(0).toISOString()

  static fromJSON(json: Record<string, Object>): StudyMetadataIndex {
    const index = new StudyMetadataIndex()
    index.itemMetadataFilesByItemID = (json['itemMetadataFilesByItemID'] as Record<string, string>) ?? {}
    index.itemMetadataFilesByRecordingID = (json['itemMetadataFilesByRecordingID'] as Record<string, string>) ?? {}
    index.folderMetadataFilesByFolderID = (json['folderMetadataFilesByFolderID'] as Record<string, string>) ?? {}
    index.updatedAt = (json['updatedAt'] as string) ?? new Date(0).toISOString()
    return index
  }
}

// =========================================================================
// RecordingReceiveRecord
// Ported from iOS RecordingReceiveRecord
// =========================================================================
export class RecordingReceiveRecord {
  recordingID: string | null = null
  sanitizedRecordingID: string | null = null
  receivedAt: string | null = null
  updatedAt: string | null = null
  sourceDeviceID: string | null = null
  sourceDeviceName: string | null = null
  originalTitle: string | null = null
  normalizedTitle: string | null = null
  audioFileName: string | null = null
  originalAudioFileName: string | null = null
  metadataFileName: string | null = null
  status: string | null = null
  transcriptionStatus: string | null = null
  noteStatus: string | null = null
  noteRelativePath: string | null = null
  noteGeneratedAt: string | null = null
  noteProviderID: string | null = null
  noteModelName: string | null = null
  processingStatus: string | null = null
  tags: string[] = []
  studyFiling: StudyFilingPath | null = null
  createdAt: string | null = null
  duration: number | null = null
  fileSize: number | null = null
  audioRelativePath: string | null = null
  metadataRelativePath: string | null = null
  transcriptRelativePath: string | null = null
  transcriptMarkdownRelativePath: string | null = null
  transcriptionProviderID: string | null = null
  transcriptionModelName: string | null = null
  transcriptionStartedAt: string | null = null
  transcriptionCompletedAt: string | null = null
  transcriptionMode: ProcessingMode | null = null
  transcriptionChunks: RecordingTranscriptionChunkRecord[] | null = null
  noteGenerationMode: ProcessingMode | null = null
  noteSections: RecordingNoteSectionRecord[] | null = null

  static fromJSON(json: Record<string, Object>): RecordingReceiveRecord {
    const r = new RecordingReceiveRecord()
    r.recordingID = (json['recordingID'] as string) ?? null
    r.sanitizedRecordingID = (json['sanitizedRecordingID'] as string) ?? null
    r.receivedAt = (json['receivedAt'] as string) ?? null
    r.updatedAt = (json['updatedAt'] as string) ?? null
    r.sourceDeviceID = (json['sourceDeviceID'] as string) ?? null
    r.sourceDeviceName = (json['sourceDeviceName'] as string) ?? null
    r.originalTitle = (json['originalTitle'] as string) ?? null
    r.normalizedTitle = (json['normalizedTitle'] as string) ?? null
    r.audioFileName = (json['audioFileName'] as string) ?? null
    r.originalAudioFileName = (json['originalAudioFileName'] as string) ?? null
    r.metadataFileName = (json['metadataFileName'] as string) ?? null
    r.status = (json['status'] as string) ?? null
    r.transcriptionStatus = (json['transcriptionStatus'] as string) ?? null
    r.noteStatus = RecordingReceiveRecord.normalizedNoteStatus(json['noteStatus'] as string | null)
    r.noteRelativePath = (json['noteRelativePath'] as string) ?? null
    r.noteGeneratedAt = (json['noteGeneratedAt'] as string) ?? null
    r.noteProviderID = (json['noteProviderID'] as string) ?? null
    r.noteModelName = (json['noteModelName'] as string) ?? null
    r.processingStatus = (json['processingStatus'] as string) ?? null
    r.tags = (json['tags'] as string[]) ?? []
    const filingJSON = json['studyFiling'] as Record<string, string> | null
    if (filingJSON) {
      const filing = StudyFilingPath.fromJSON(filingJSON)
      r.studyFiling = filing.isEmpty ? null : filing
    }
    r.createdAt = (json['createdAt'] as string) ?? null
    r.duration = (json['duration'] as number) ?? null
    r.fileSize = (json['fileSize'] as number) ?? null
    r.audioRelativePath = (json['audioRelativePath'] as string) ?? null
    r.metadataRelativePath = (json['metadataRelativePath'] as string) ?? null
    r.transcriptRelativePath = (json['transcriptRelativePath'] as string) ?? null
    r.transcriptMarkdownRelativePath = (json['transcriptMarkdownRelativePath'] as string) ?? null
    r.transcriptionProviderID = (json['transcriptionProviderID'] as string) ?? null
    r.transcriptionModelName = (json['transcriptionModelName'] as string) ?? null
    r.transcriptionStartedAt = (json['transcriptionStartedAt'] as string) ?? null
    r.transcriptionCompletedAt = (json['transcriptionCompletedAt'] as string) ?? null
    r.transcriptionMode = (json['transcriptionMode'] as ProcessingMode) ?? null
    const chunksJSON = json['transcriptionChunks'] as Array<Record<string, Object>> | null
    r.transcriptionChunks = chunksJSON ? chunksJSON.map(c => RecordingTranscriptionChunkRecord.fromJSON(c)) : null
    r.noteGenerationMode = (json['noteGenerationMode'] as ProcessingMode) ?? null
    const sectionsJSON = json['noteSections'] as Array<Record<string, Object>> | null
    r.noteSections = sectionsJSON ? sectionsJSON.map(s => RecordingNoteSectionRecord.fromJSON(s)) : null
    return r
  }

  static normalizedNoteStatus(status: string | null): string | null {
    const normalized = (status ?? '').trim()
    if (normalized === 'notGenerated') return 'notStarted'
    return normalized.length > 0 ? normalized : null
  }
}

// =========================================================================
// StudyLibraryBrowser
// Ported from iOS StudyLibraryBrowser
// =========================================================================
export class StudyLibraryBrowser {
  static readonly levelKeys = ['type', 'subject', 'chapter', 'topic']

  static content(
    items: StudyItemMetadata[],
    folders: StudyFolderMetadata[] = [],
    path: StudyBrowsePath
  ): StudyBrowseContent {
    const matchingItems = items.filter(item => StudyLibraryBrowser.itemMatches(item, path))

    if (path.depth >= StudyLibraryBrowser.levelKeys.length || path.isUncategorizedTypeSelection) {
      return new StudyBrowseContent(path, [], StudyLibraryBrowser.sortedItems(matchingItems))
    }

    const nextLevelKey = StudyLibraryBrowser.levelKeys[path.depth]
    const nextLevel = StudyFolderMetadata.levelForDepth(path.depth)
    const grouped = new Map<string, StudyItemMetadata[]>()
    for (const item of matchingItems) {
      const displayValue = StudyLibraryBrowser.displayValueForItem(item, nextLevelKey)
      if (!grouped.has(displayValue)) grouped.set(displayValue, [])
      grouped.get(displayValue)!.push(item)
    }

    const browseFoldersByPath = new Map<string, StudyBrowseFolder>()

    for (const [title, groupedItems] of grouped) {
      const folderPath = path.appending(title)
      browseFoldersByPath.set(folderPath.storageKey, new StudyBrowseFolder(
        `${path.components.join('/')}/${nextLevelKey}=${title}`,
        null,
        nextLevelKey,
        title,
        groupedItems.length,
        folderPath,
        null
      ))
    }

    if (nextLevel) {
      for (const folder of folders) {
        if (!StudyLibraryBrowser.folderMatchesParent(folder, path, nextLevel)) continue
        if (folder.isTrashed) continue
        const folderPath = new StudyBrowsePath(folder.pathComponents)
        const itemCount = items.filter(item => StudyLibraryBrowser.itemMatches(item, folderPath)).length
        browseFoldersByPath.set(folderPath.storageKey, new StudyBrowseFolder(
          folder.folderID,
          folder.folderID,
          folder.level,
          folder.name,
          itemCount,
          folderPath,
          folder.colorToken
        ))
      }
    }

    const sortedFolders = [...browseFoldersByPath.values()].sort((a, b) => StudyLibraryBrowser.folderSort(a, b))
    return new StudyBrowseContent(path, sortedFolders, [])
  }

  static breadcrumbs(path: StudyBrowsePath): Array<{ title: string, path: StudyBrowsePath }> {
    const result: Array<{ title: string, path: StudyBrowsePath }> = [
      { title: '学习库', path: new StudyBrowsePath() }
    ]
    for (let i = 0; i < path.components.length; i++) {
      const componentPath = path.truncatedTo(i + 1)
      result.push({ title: path.components[i], path: componentPath })
    }
    return result
  }

  static levelTitle(path: StudyBrowsePath): string {
    if (path.depth >= StudyLibraryBrowser.levelKeys.length || path.isUncategorizedTypeSelection) {
      return '录音'
    }
    const levelKey = StudyLibraryBrowser.levelKeys[path.depth]
    switch (levelKey) {
      case 'type': return '门类'
      case 'subject': return '课程'
      case 'chapter': return '章节'
      case 'topic': return '主题'
      default: return '文件夹'
    }
  }

  static itemMatches(item: StudyItemMetadata, path: StudyBrowsePath): boolean {
    if (path.depth > StudyLibraryBrowser.levelKeys.length) return false
    for (let i = 0; i < path.components.length; i++) {
      const component = path.components[i]
      if (StudyLibraryBrowser.displayValueForItem(item, StudyLibraryBrowser.levelKeys[i]) !== component) {
        return false
      }
    }
    return true
  }

  private static displayValueForItem(item: StudyItemMetadata, levelKey: string): string {
    const value = item.filingPath.valueForLevel(levelKey)
    if (value && value.length > 0) return value
    return StudyTag.normalizedNamespace(levelKey) === 'type'
      ? StudyHierarchyRule.uncategorizedValue
      : StudyHierarchyRule.missingValue
  }

  private static folderMatchesParent(
    folder: StudyFolderMetadata,
    parentPath: StudyBrowsePath,
    nextLevel: StudyFolderLevel
  ): boolean {
    if (folder.level !== nextLevel) return false
    const folderComponents = folder.pathComponents
    if (folderComponents.length !== parentPath.depth + 1) return false
    return folderComponents.slice(0, parentPath.depth).every((c, i) => c === parentPath.components[i])
  }

  private static folderSort(a: StudyBrowseFolder, b: StudyBrowseFolder): number {
    if (a.isFallback !== b.isFallback) return a.isFallback ? 1 : -1
    return a.title.localeCompare(b.title)
  }

  private static sortedItems(items: StudyItemMetadata[]): StudyItemMetadata[] {
    return [...items].sort((a, b) => {
      if (a.createdAt === b.createdAt) {
        return a.title.localeCompare(b.title)
      }
      return b.createdAt.localeCompare(a.createdAt)
    })
  }
}

// =========================================================================
// StudyFilingCandidates
// Ported from iOS StudyFilingCandidates
// =========================================================================
export class StudyFilingCandidates {
  types: string[] = []
  subjects: string[] = []
  chapters: string[] = []
  topics: string[] = []

  static readonly empty = new StudyFilingCandidates()

  valuesForLevel(level: StudyFolderLevel): string[] {
    switch (level) {
      case StudyFolderLevel.TYPE: return this.types
      case StudyFolderLevel.SUBJECT: return this.subjects
      case StudyFolderLevel.CHAPTER: return this.chapters
      case StudyFolderLevel.TOPIC: return this.topics
      default: return []
    }
  }

  static collectFromItems(items: StudyItemMetadata[]): StudyFilingCandidates {
    const candidates = new StudyFilingCandidates()
    candidates.types = StudyFilingCandidates.sortedUnique(
      StudyFilingCandidates.valuesForKey('type', items)
    )
    candidates.subjects = StudyFilingCandidates.sortedUnique(
      StudyFilingCandidates.valuesForKey('subject', items)
    )
    candidates.chapters = StudyFilingCandidates.sortedUnique(
      StudyFilingCandidates.valuesForKey('chapter', items)
    )
    candidates.topics = StudyFilingCandidates.sortedUnique(
      StudyFilingCandidates.valuesForKey('topic', items)
    )
    return candidates
  }

  private static valuesForKey(level: string, items: StudyItemMetadata[]): string[] {
    const values: string[] = []
    for (const item of items) {
      const filingValue = item.filingPath.valueForLevel(level)
      if (filingValue && filingValue.length > 0) values.push(filingValue)
      for (const tag of item.tags) {
        if (tag.namespace === StudyTag.normalizedNamespace(level)) {
          values.push(tag.displayTitle)
        }
      }
    }
    return values
  }

  private static sortedUnique(values: string[]): string[] {
    const seen = new Set<string>()
    const result: string[] = []
    for (const value of values) {
      const n = StudyFilingPath.normalized(value)
      if (!n) continue
      const key = n.toLowerCase()
      if (seen.has(key)) continue
      seen.add(key)
      result.push(n)
    }
    return result.sort((a, b) => a.localeCompare(b))
  }
}

// =========================================================================
// syncCustomProperties filter
// =========================================================================
export function filterSyncCustomProperties(properties: Record<string, string>): Record<string, string> {
  const result: Record<string, string> = {}
  for (const key of Object.keys(properties)) {
    const normalized = key.toLowerCase()
    if (normalized.includes('apikey') || normalized.includes('api_key') ||
      normalized.includes('secret') || normalized.includes('hmac') ||
      normalized.includes('pairing') || normalized.includes('rawresponse') ||
      normalized.includes('raw_response') || normalized.includes('providerresponse') ||
      normalized.includes('provider_response') || normalized.includes('fulltranscript') ||
      normalized.includes('full_transcript') || normalized.includes('fullnote') ||
      normalized.includes('full_note') || normalized.includes('prompt') ||
      normalized.includes('debug') || normalized.includes('rawjson') ||
      normalized.includes('raw_json') || normalized.includes('localnetworktransfer')) {
      continue
    }
    result[key] = properties[key]
  }
  return result
}
