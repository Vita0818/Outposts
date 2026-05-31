/**
 * StudyLibraryStore - Core study library persistence and logic
 * Ported from iOS Rokurics/StudyLibraryStore.swift
 *
 * Manages StudyItemMetadata, StudyFolderMetadata, StudyMetadataIndex,
 * manifest generation, sync apply with conflict merge, tombstone handling,
 * legacy migration, atomic writes, and sandbox path safety.
 */
import { fileIo } from '@kit.CoreFileKit'
import { cryptoFramework } from '@kit.CryptoArchitectureKit'
import {
  StudyItemMetadata,
  StudyItemMetadataFields,
  StudyItemKind,
  StudyFolderMetadata,
  StudyFolderMetadataFields,
  StudyMetadataIndex,
  StudyFilingPath,
  StudyHierarchyRule,
  StudyFolderLevel,
  StudyFolderColorToken,
  StudyBrowsePath,
  StudyFilingCandidates,
  RecordingReceiveRecord,
  StudyPathSanitizer,
  StudyLibraryBrowser,
  normalized,
  filterSyncCustomProperties,
  uniqueIDs,
  RecordingMetadataLike,
  StudyTag
} from '../models/StudyLibraryModels'
import { AudioFileStore } from './AudioFileStore'

const TAG = 'RokuricsStudyLibraryStore'

// =========================================================================
// Error types
// =========================================================================
export enum StudyLibraryStoreErrorCode {
  UNABLE_TO_CREATE_DIRECTORY = 'study_directory_unavailable',
  UNSAFE_DESTINATION = 'unsafe_study_destination',
  ITEM_MISSING = 'study_item_missing',
  FOLDER_MISSING = 'study_folder_missing',
  UNSUPPORTED_FOLDER_LEVEL = 'study_folder_level_unsupported',
  WRITE_FAILED = 'study_item_metadata_write_failed',
  FOLDER_WRITE_FAILED = 'study_folder_metadata_write_failed',
  CHECKSUM_MISMATCH = 'sync_manifest_checksum_mismatch',
  FOLDER_NOT_EMPTY = 'study_folder_not_empty',
  FOLDER_DUPLICATE_NAME = 'study_folder_duplicate_name'
}

export class StudyLibraryStoreError extends Error {
  code: StudyLibraryStoreErrorCode
  constructor(code: StudyLibraryStoreErrorCode, message?: string) {
    super(message ?? code)
    this.code = code
    this.name = 'StudyLibraryStoreError'
  }
}

// =========================================================================
// Sync manifest types (inline, minimal port)
// =========================================================================
export enum StudyLibrarySyncEntityKind {
  ITEM = 'item',
  FOLDER = 'folder'
}

export enum StudyLibrarySyncOperation {
  UPSERT = 'upsert',
  DELETE = 'delete',
  TRASH = 'trash',
  RESTORE = 'restore',
  DELETE_METADATA_ONLY = 'deleteMetadataOnly'
}

export class StudyLibrarySyncTombstone {
  id: string
  entityKind: StudyLibrarySyncEntityKind
  entityID: string
  operation: StudyLibrarySyncOperation
  updatedAt: string
  modifiedByDeviceID: string | null

  constructor(
    id: string,
    entityKind: StudyLibrarySyncEntityKind,
    entityID: string,
    operation: StudyLibrarySyncOperation,
    updatedAt: string,
    modifiedByDeviceID: string | null = null
  ) {
    this.id = id
    this.entityKind = entityKind
    this.entityID = entityID
    this.operation = operation
    this.updatedAt = updatedAt
    this.modifiedByDeviceID = modifiedByDeviceID
  }
}

export class StudyLibrarySyncApplyResult {
  appliedItemCount: number = 0
  appliedFolderCount: number = 0
  tombstoneCount: number = 0
  conflictCount: number = 0
  skippedOlderCount: number = 0
  failedChanges: number = 0

  get summaryText(): string {
    if (this.failedChanges > 0) return `同步失败 ${this.failedChanges} 项`
    if (this.appliedItemCount === 0 && this.appliedFolderCount === 0 &&
      this.tombstoneCount === 0 && this.conflictCount === 0) {
      return '已是最新'
    }
    const parts: string[] = []
    if (this.appliedItemCount > 0) parts.push(`${this.appliedItemCount} 项`)
    if (this.appliedFolderCount > 0) parts.push(`${this.appliedFolderCount} 个文件夹`)
    if (this.tombstoneCount > 0) parts.push(`${this.tombstoneCount} 个废纸篓状态`)
    if (this.conflictCount > 0) parts.push(`${this.conflictCount} 个冲突已保留`)
    return parts.join(' · ')
  }
}

export class StudyLibrarySyncManifest {
  deviceID: string
  generatedAt: string
  libraryVersion: number
  items: StudyItemMetadata[]
  folders: StudyFolderMetadata[]
  tombstones: StudyLibrarySyncTombstone[]
  pendingUploads: PendingRecordingUpload[]
  baseCommitID: string | null
  commitID: string | null
  localManifestHash: string | null
  checksum: string

  constructor(
    deviceID: string,
    generatedAt: string,
    libraryVersion: number,
    items: StudyItemMetadata[],
    folders: StudyFolderMetadata[],
    tombstones: StudyLibrarySyncTombstone[],
    pendingUploads: PendingRecordingUpload[],
    baseCommitID: string | null = null,
    commitID: string | null = null,
    localManifestHash: string | null = null,
    checksum: string = ''
  ) {
    this.deviceID = deviceID
    this.generatedAt = generatedAt
    this.libraryVersion = libraryVersion
    this.items = [...items].sort((a, b) => a.itemID.localeCompare(b.itemID))
    this.folders = [...folders].sort((a, b) => a.folderID.localeCompare(b.folderID))
    this.tombstones = [...tombstones].sort((a, b) => a.id.localeCompare(b.id))
    this.pendingUploads = [...pendingUploads].sort((a, b) => a.id.localeCompare(b.id))
    this.baseCommitID = baseCommitID
    this.commitID = commitID
    this.localManifestHash = localManifestHash
    this.checksum = checksum
  }

  static make(
    deviceID: string,
    generatedAt: string,
    libraryVersion: number,
    items: StudyItemMetadata[],
    folders: StudyFolderMetadata[],
    tombstones: StudyLibrarySyncTombstone[] = [],
    pendingUploads: PendingRecordingUpload[] = [],
    baseCommitID: string | null = null,
    commitID: string | null = null,
    localManifestHash: string | null = null
  ): StudyLibrarySyncManifest {
    const manifest = new StudyLibrarySyncManifest(
      deviceID, generatedAt, libraryVersion,
      items, folders, tombstones, pendingUploads,
      baseCommitID, commitID, localManifestHash, ''
    )
    manifest.checksum = StudyLibrarySyncManifest.computeChecksum(manifest)
    return manifest
  }

  static computeChecksum(manifest: StudyLibrarySyncManifest): string {
    // Build a deterministic checksum payload matching iOS logic
    const payload: Record<string, Object> = {
      deviceID: manifest.deviceID,
      generatedAt: manifest.generatedAt,
      libraryVersion: manifest.libraryVersion,
      items: manifest.items.map(i => i.toJSON()),
      folders: manifest.folders.map(f => f.toJSON()),
      tombstones: manifest.tombstones.map(t => ({
        id: t.id,
        entityKind: t.entityKind,
        entityID: t.entityID,
        operation: t.operation,
        updatedAt: t.updatedAt,
        modifiedByDeviceID: t.modifiedByDeviceID
      })),
      pendingUploads: manifest.pendingUploads.map(p => ({
        id: p.id,
        itemID: p.itemID,
        recordingID: p.recordingID,
        localAudioRelativePath: p.localAudioRelativePath,
        targetDeviceID: p.targetDeviceID,
        status: p.status,
        createdAt: p.createdAt,
        updatedAt: p.updatedAt
      }))
    }
    return StudyLibrarySyncManifest.sha256(JSON.stringify(payload, sortedKeysReplacer))
  }

  private static sha256(data: string): string {
    try {
      const input: cryptoFramework.DataBlob = { data: new Uint8Array(
        Array.from(data).map(c => c.charCodeAt(0))
      ) }
      const md = cryptoFramework.createMd('SHA256')
      md.updateSync(input)
      const result = md.digestSync()
      // Convert to hex
      let hex = ''
      for (const byte of result.data) {
        hex += byte.toString(16).padStart(2, '0')
      }
      return hex
    } catch {
      // Fallback: simple hash for preview/testing
      let hash = 0
      for (let i = 0; i < data.length; i++) {
        const ch = data.charCodeAt(i)
        hash = ((hash << 5) - hash) + ch
        hash |= 0
      }
      return Math.abs(hash).toString(16).padStart(8, '0')
    }
  }

  get hasValidChecksum(): boolean {
    return this.checksum === StudyLibrarySyncManifest.computeChecksum(this)
  }

  get summaryText(): string {
    const uploadText = this.pendingUploads.length > 0 ? `${this.pendingUploads.length} 个待上传` : null
    const parts = [`${this.items.length} 项`, `${this.folders.length} 个文件夹`]
    if (uploadText) parts.push(uploadText)
    return parts.join(' · ')
  }

  toJSON(): Record<string, Object> {
    return {
      deviceID: this.deviceID,
      generatedAt: this.generatedAt,
      libraryVersion: this.libraryVersion,
      items: this.items.map(i => i.toJSON()),
      folders: this.folders.map(f => f.toJSON()),
      tombstones: this.tombstones.map(t => ({
        id: t.id,
        entityKind: t.entityKind,
        entityID: t.entityID,
        operation: t.operation,
        updatedAt: t.updatedAt,
        modifiedByDeviceID: t.modifiedByDeviceID
      })),
      pendingUploads: this.pendingUploads.map(p => p.toJSON()),
      baseCommitID: this.baseCommitID,
      commitID: this.commitID,
      localManifestHash: this.localManifestHash,
      checksum: this.checksum
    }
  }

  static fromJSON(json: Record<string, Object>): StudyLibrarySyncManifest {
    const itemsJSON = (json['items'] as Array<Record<string, Object>>) ?? []
    const foldersJSON = (json['folders'] as Array<Record<string, Object>>) ?? []
    const tombstonesJSON = (json['tombstones'] as Array<Record<string, Object>>) ?? []
    const pendingJSON = (json['pendingUploads'] as Array<Record<string, Object>>) ?? []
    return new StudyLibrarySyncManifest(
      (json['deviceID'] as string) ?? '',
      (json['generatedAt'] as string) ?? new Date().toISOString(),
      (json['libraryVersion'] as number) ?? 1,
      itemsJSON.map(i => StudyItemMetadata.fromJSON(i)),
      foldersJSON.map(f => StudyFolderMetadata.fromJSON(f)),
      tombstonesJSON.map(t => new StudyLibrarySyncTombstone(
        (t['id'] as string) ?? '',
        (t['entityKind'] as StudyLibrarySyncEntityKind) ?? StudyLibrarySyncEntityKind.ITEM,
        (t['entityID'] as string) ?? '',
        (t['operation'] as StudyLibrarySyncOperation) ?? StudyLibrarySyncOperation.UPSERT,
        (t['updatedAt'] as string) ?? '',
        (t['modifiedByDeviceID'] as string) ?? null
      )),
      pendingJSON.map(p => PendingRecordingUpload.fromJSON(p)),
      (json['baseCommitID'] as string) ?? null,
      (json['commitID'] as string) ?? null,
      (json['localManifestHash'] as string) ?? null,
      (json['checksum'] as string) ?? ''
    )
  }
}

// =========================================================================
// PendingRecordingUpload
// =========================================================================
export enum PendingRecordingUploadStatus {
  PENDING = 'pending',
  UPLOADING = 'uploading',
  UPLOADED = 'uploaded',
  FAILED = 'failed'
}

export class PendingRecordingUpload {
  id: string
  itemID: string
  recordingID: string
  localAudioRelativePath: string
  targetDeviceID: string
  status: PendingRecordingUploadStatus
  createdAt: string
  updatedAt: string
  lastAttemptAt: string | null
  retryCount: number
  lastError: string | null

  constructor(
    itemID: string,
    recordingID: string,
    localAudioRelativePath: string,
    targetDeviceID: string,
    status: PendingRecordingUploadStatus = PendingRecordingUploadStatus.PENDING,
    createdAt: string = new Date().toISOString(),
    updatedAt: string = new Date().toISOString(),
    lastAttemptAt: string | null = null,
    retryCount: number = 0,
    lastError: string | null = null
  ) {
    this.id = `${itemID}:${recordingID}`
    this.itemID = itemID
    this.recordingID = recordingID
    this.localAudioRelativePath = localAudioRelativePath
    this.targetDeviceID = targetDeviceID
    this.status = status
    this.createdAt = createdAt
    this.updatedAt = updatedAt
    this.lastAttemptAt = lastAttemptAt
    this.retryCount = retryCount
    this.lastError = lastError
  }

  toJSON(): Record<string, Object> {
    const result: Record<string, Object> = {
      id: this.id,
      itemID: this.itemID,
      recordingID: this.recordingID,
      localAudioRelativePath: this.localAudioRelativePath,
      targetDeviceID: this.targetDeviceID,
      status: this.status,
      createdAt: this.createdAt,
      updatedAt: this.updatedAt,
      retryCount: this.retryCount
    }
    if (this.lastAttemptAt) result['lastAttemptAt'] = this.lastAttemptAt
    if (this.lastError) result['lastError'] = this.lastError
    return result
  }

  static fromJSON(json: Record<string, Object>): PendingRecordingUpload {
    return new PendingRecordingUpload(
      (json['itemID'] as string) ?? '',
      (json['recordingID'] as string) ?? (json['itemID'] as string) ?? '',
      (json['localAudioRelativePath'] as string) ?? '',
      (json['targetDeviceID'] as string) ?? '',
      (json['status'] as PendingRecordingUploadStatus) ?? PendingRecordingUploadStatus.PENDING,
      (json['createdAt'] as string) ?? new Date().toISOString(),
      (json['updatedAt'] as string) ?? new Date().toISOString(),
      (json['lastAttemptAt'] as string) ?? null,
      (json['retryCount'] as number) ?? 0,
      (json['lastError'] as string) ?? null
    )
  }
}

// =========================================================================
// StudyLibraryStore
// Ported from iOS StudyLibraryStore
// =========================================================================
export class StudyLibraryStore {
  private fileManager: typeof fileIo
  private rootURL: string
  private studyURL: string
  private itemMetadataURL: string
  private folderMetadataURL: string
  private indexURL: string
  private hierarchyRulesURL: string
  private legacyItemMetadataURL: string
  private legacyIndexURL: string
  private audioFileStore: AudioFileStore

  private _allStudyItems: StudyItemMetadata[] = []
  private _allStudyFolders: StudyFolderMetadata[] = []
  private _hierarchyRules: StudyHierarchyRule[] = [StudyHierarchyRule.defaultCourseView]
  private _selectedHierarchyRule: StudyHierarchyRule = StudyHierarchyRule.defaultCourseView
  private _filingCandidates: StudyFilingCandidates = StudyFilingCandidates.empty

  constructor(audioFileStore: AudioFileStore) {
    this.fileManager = fileIo
    this.audioFileStore = audioFileStore

    this.rootURL = audioFileStore.baseDir
    this.studyURL = this.joinPath(this.rootURL, 'study')
    this.itemMetadataURL = this.joinPath(this.studyURL, 'items')
    this.folderMetadataURL = this.joinPath(this.studyURL, 'folders')
    this.indexURL = this.joinPath(this.studyURL, 'index.json')
    this.hierarchyRulesURL = this.joinPath(this.studyURL, 'hierarchy-rules.json')
    this.legacyItemMetadataURL = this.joinPath(this.studyURL, 'item-metadata')
    this.legacyIndexURL = this.joinPath(this.studyURL, 'study-index.json')

    try { this.ensureStudyDirectories() } catch (_e) { /* ignore */ }
    this._hierarchyRules = this.loadHierarchyRules()
    this._selectedHierarchyRule = this._hierarchyRules.length > 0
      ? this._hierarchyRules[0] : StudyHierarchyRule.defaultCourseView
    this.refresh()
  }

  // =========================================================================
  // Property accessors
  // =========================================================================
  get allStudyItems(): StudyItemMetadata[] { return this._allStudyItems }
  get allStudyFolders(): StudyFolderMetadata[] { return this._allStudyFolders }
  get hierarchyRules(): StudyHierarchyRule[] { return this._hierarchyRules }
  get selectedHierarchyRule(): StudyHierarchyRule { return this._selectedHierarchyRule }
  get filingCandidates(): StudyFilingCandidates { return this._filingCandidates }
  get libraryRootURL(): string { return this.rootURL }
  get studyRootDisplayPath(): string { return this.studyURL }

  // =========================================================================
  // Core: refresh
  // Ported from iOS refresh()
  // =========================================================================
  async refresh(): Promise<void> {
    const recordings = await this.audioFileStore.loadAllMetadata(true) as RecordingMetadataLike[]
    const storedItems = this.loadAllStoredItemMetadata()
    const receiveItems = this.loadReceiveRecordDerivedItems()

    const storedItemsByRecordingID = new Map<string, StudyItemMetadata>()
    for (const item of storedItems) {
      if (item.recordingID) {
        storedItemsByRecordingID.set(item.recordingID, item)
      }
    }

    const liveRecordingIDs = new Set(recordings.map(r => r.id))
    const itemsByID = new Map<string, StudyItemMetadata>()

    for (const recording of recordings) {
      const stored = storedItemsByRecordingID.get(recording.id)
      const fallback = StudyItemMetadata.defaultMetadataForRecording(recording)
      const metadata = stored ? stored.mergedWithCurrentRecording(recording) : fallback
      itemsByID.set(metadata.itemID, metadata)
    }

    for (const item of receiveItems) {
      const recordingID = item.recordingID
      if (recordingID && liveRecordingIDs.has(recordingID)) continue
      itemsByID.set(item.itemID, item)
    }

    for (const item of storedItems) {
      if (this.shouldIncludeStoredItem(item, liveRecordingIDs, itemsByID)) {
        itemsByID.set(item.itemID, item)
      }
    }

    const items = [...itemsByID.values()].sort((a, b) => {
      if (a.createdAt === b.createdAt) {
        return a.title.localeCompare(b.title)
      }
      return b.createdAt.localeCompare(a.createdAt)
    })

    const folders = this.repairedFolders(
      this.loadAllFolderMetadata().filter(f => !f.isTrashed),
      items
    )

    this._allStudyItems = items
    this._allStudyFolders = folders
    this._filingCandidates = StudyFilingCandidates.collectFromItems(items)
  }

  // =========================================================================
  // Item/folder lookup
  // =========================================================================
  itemByRecordingID(recordingID: string): StudyItemMetadata | null {
    return this._allStudyItems.find(i => i.recordingID === recordingID) ?? null
  }

  itemByItemID(itemID: string): StudyItemMetadata | null {
    return this._allStudyItems.find(i => i.itemID === itemID || i.recordingID === itemID) ?? null
  }

  folderByFolderID(folderID: string): StudyFolderMetadata | null {
    return this.loadStoredFolder(folderID)
      ?? this._allStudyFolders.find(f => f.folderID === folderID)
      ?? null
  }

  // =========================================================================
  // Save items
  // Ported from iOS save(_ metadata: StudyItemMetadata)
  // =========================================================================
  async saveItem(metadata: StudyItemMetadata): Promise<void> {
    const previous = this.editableMetadataIfAvailableByItemID(metadata.itemID)
    await this.saveItemInternal(metadata, previous)
    await this.refresh()
  }

  private async saveItemInternal(metadata: StudyItemMetadata, previousMetadata: StudyItemMetadata | null): Promise<void> {
    if (!normalized(metadata.itemID)) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.ITEM_MISSING)
    }
    try {
      this.ensureStudyDirectories()
      let metadataToSave = metadata.clone()
      metadataToSave.tags = StudyTagListUnique(metadata.tags)
      if (metadataToSave.folderIDs.length === 0) {
        metadataToSave.folderIDs = StudyItemMetadata.defaultFolderIDs(metadataToSave.filing)
      }

      await this.syncFolderLinks(metadataToSave, previousMetadata)
      const fileName = StudyLibraryStore.itemMetadataFileName(metadataToSave)
      const metadataURL = this.joinPath(this.itemMetadataURL, fileName)

      if (!this.isInsideItemMetadataDirectory(metadataURL)) {
        throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.UNSAFE_DESTINATION)
      }

      this.atomicWriteJSON(metadataURL, metadataToSave.toJSON())

      const index = this.loadIndex()
      index.itemMetadataFilesByItemID[metadataToSave.itemID] = fileName
      if (metadataToSave.recordingID) {
        index.itemMetadataFilesByRecordingID[metadataToSave.recordingID] = fileName
      }
      index.updatedAt = new Date().toISOString()
      this.saveIndex(index)
    } catch (e) {
      if (e instanceof StudyLibraryStoreError) throw e
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.WRITE_FAILED, String(e))
    }
  }

  // =========================================================================
  // Save folders
  // Ported from iOS save(_ folder: StudyFolderMetadata)
  // =========================================================================
  async saveFolder(folder: StudyFolderMetadata): Promise<void> {
    try {
      this.ensureStudyDirectories()
      const fileName = StudyLibraryStore.folderMetadataFileName(folder)
      const folderURL = this.joinPath(this.folderMetadataURL, fileName)

      if (!this.isInsideFolderMetadataDirectory(folderURL)) {
        throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.UNSAFE_DESTINATION)
      }

      this.atomicWriteJSON(folderURL, folder.toJSON())

      const index = this.loadIndex()
      index.folderMetadataFilesByFolderID[folder.folderID] = fileName
      index.updatedAt = new Date().toISOString()
      this.saveIndex(index)
    } catch (e) {
      if (e instanceof StudyLibraryStoreError) throw e
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.FOLDER_WRITE_FAILED, String(e))
    }
  }

  // =========================================================================
  // Upsert recording metadata
  // Ported from iOS upsertRecordingMetadata
  // =========================================================================
  async upsertRecordingMetadata(recording: RecordingMetadataLike): Promise<StudyItemMetadata> {
    const previous = this.editableMetadataIfAvailableByRecordingID(recording.id)
    const fallback = StudyItemMetadata.defaultMetadataForRecording(recording)
    const metadata = previous ? previous.mergedWithCurrentRecording(recording) : fallback
    await this.saveItemInternal(metadata, previous)
    await this.refresh()
    return metadata
  }

  // =========================================================================
  // Update filing
  // Ported from iOS updateFiling
  // =========================================================================
  async updateFiling(recordingID: string, studyFiling: StudyFilingPath | null): Promise<void> {
    const previous = await this.editableMetadata(recordingID)
    const metadata = previous.clone()
    metadata.filing = (studyFiling?.isEmpty ?? true) ? new StudyFilingPath() : (studyFiling ?? new StudyFilingPath())
    metadata.folderIDs = StudyItemMetadata.defaultFolderIDs(metadata.filing)
    metadata.updatedAt = new Date().toISOString()
    await this.saveItemInternal(metadata, previous)
    await this.refresh()
  }

  // =========================================================================
  // Create folder
  // Ported from iOS createFolder
  // =========================================================================
  async createFolder(rawName: string, path: StudyBrowsePath): Promise<StudyFolderMetadata> {
    const level = StudyFolderMetadata.levelForDepth(path.depth)
    if (!level) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.UNSUPPORTED_FOLDER_LEVEL)
    }

    const name = normalized(rawName) ?? StudyHierarchyRule.missingValue
    const components = [...path.components, name]
    const filing = StudyFolderMetadata.filingPathForComponents(components)
    const parentFolderID = this.parentFolderIDForPath(path)

    const folder = new StudyFolderMetadata({
      name: name,
      level: level,
      path: filing,
      parentFolderID: parentFolderID,
      childFolderIDs: [],
      itemIDs: []
    })

    await this.saveFolder(folder)
    if (parentFolderID) {
      await this.appendChildFolderID(folder.folderID, parentFolderID, path)
    }
    await this.refresh()
    return folder
  }

  // =========================================================================
  // Rename folder (with cascade)
  // Ported from iOS renameFolder
  // =========================================================================
  async renameFolder(folderID: string, rawName: string): Promise<StudyFolderMetadata> {
    let folder = this.folderByFolderID(folderID)
    if (!folder) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.FOLDER_MISSING)
    }

    const name = normalized(rawName)
    if (!name) return folder
    if (folder.name === name) return folder

    // Check for duplicate name at same level and parent
    const storedFolders = this.loadAllFolderMetadata()
    const duplicateExists = storedFolders.some(candidate =>
      candidate.folderID !== folder!.folderID &&
      candidate.parentFolderID === folder!.parentFolderID &&
      candidate.level === folder!.level &&
      candidate.name.toLowerCase() === name.toLowerCase()
    )
    if (duplicateExists) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.FOLDER_DUPLICATE_NAME)
    }

    const oldPath = folder.path.clone()
    const oldPathComponents = folder.pathComponents
    const updatedAt = new Date().toISOString()
    const foldersToSave: StudyFolderMetadata[] = []

    // Cascade rename to descendant folders
    for (const candidate of storedFolders) {
      if (!StudyLibraryStore.pathComponentsStartWith(candidate.pathComponents, oldPathComponents)) continue
      const updatedPath = this.renamedPath(candidate.path, folder!.level, name)
      if (!updatedPath) continue

      const updated = candidate.clone()
      if (updated.folderID === folder!.folderID) {
        updated.name = name
      }
      updated.path = updatedPath
      updated.updatedAt = updatedAt
      foldersToSave.push(updated)
      if (updated.folderID === folder!.folderID) {
        folder = updated
      }
    }

    // Ensure the target folder is in the save list
    if (!foldersToSave.some(f => f.folderID === folder!.folderID)) {
      folder.name = name
      folder.path = this.renamedPath(folder.path, folder.level, name) ?? folder.path
      folder.updatedAt = updatedAt
      foldersToSave.push(folder)
    }

    // Cascade filing path on affected items
    const itemsByID = new Map<string, StudyItemMetadata>()
    const allItems = [...this.loadAllStoredItemMetadata(), ...this._allStudyItems]
    for (const item of allItems) {
      itemsByID.set(item.itemID, item)
    }

    for (const [, item] of itemsByID) {
      if (!this.itemMatchesFolder(item, oldPath, folder!.level)) continue
      const updated = item.clone()
      updated.filing = this.renamedPath(item.filing, folder!.level, name) ?? item.filing
      updated.updatedAt = updatedAt
      const fileName = StudyLibraryStore.itemMetadataFileName(updated)
      const metadataURL = this.joinPath(this.itemMetadataURL, fileName)
      if (!this.isInsideItemMetadataDirectory(metadataURL)) continue
      this.atomicWriteJSON(metadataURL, updated.toJSON())
      const index = this.loadIndex()
      index.itemMetadataFilesByItemID[updated.itemID] = fileName
      if (updated.recordingID) {
        index.itemMetadataFilesByRecordingID[updated.recordingID] = fileName
      }
      index.updatedAt = new Date().toISOString()
      this.saveIndex(index)
    }

    for (const updatedFolder of foldersToSave) {
      await this.saveFolder(updatedFolder)
    }

    await this.refresh()
    return folder
  }

  // =========================================================================
  // Set folder color
  // =========================================================================
  async setFolderColor(folderID: string, colorToken: StudyFolderColorToken | null): Promise<StudyFolderMetadata> {
    let folder = this.folderByFolderID(folderID)
    if (!folder) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.FOLDER_MISSING)
    }
    folder = folder.clone()
    folder.colorToken = colorToken === StudyFolderColorToken.DEFAULT ? null : colorToken
    folder.updatedAt = new Date().toISOString()
    await this.saveFolder(folder)
    await this.refresh()
    return folder
  }

  // =========================================================================
  // Move folder to trash
  // =========================================================================
  async moveFolderToTrash(folderID: string): Promise<StudyFolderMetadata> {
    let folder = this.loadStoredFolder(folderID)
      ?? this._allStudyFolders.find(f => f.folderID === folderID)
    if (!folder) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.FOLDER_MISSING)
    }

    const folderPathComponents = folder.pathComponents
    const descendantFolders = this.loadAllFolderMetadata().filter(candidate =>
      candidate.folderID !== folder!.folderID &&
      !candidate.isTrashed &&
      StudyLibraryStore.pathComponentsStartWith(candidate.pathComponents, folderPathComponents)
    )
    const matchingItems = this._allStudyItems.filter(item =>
      this.itemMatchesFolder(item, folder!.path, folder!.level)
    )
    const hasIndexedItems = folder.itemIDs.length > 0

    if (descendantFolders.length > 0 || matchingItems.length > 0 || hasIndexedItems) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.FOLDER_NOT_EMPTY)
    }

    folder = folder.clone()
    folder.isTrashed = true
    folder.trashedAt = new Date().toISOString()
    folder.updatedAt = new Date().toISOString()
    await this.saveFolder(folder)
    await this.refresh()
    return folder
  }

  // =========================================================================
  // Manifest generation
  // Ported from iOS makeSyncManifest
  // =========================================================================
  makeSyncManifest(deviceID: string, generatedAt: string = new Date().toISOString()): StudyLibrarySyncManifest {
    this.refreshSync()

    const itemsByID = new Map<string, StudyItemMetadata>()
    const allItems = [...this.loadAllStoredItemMetadata(), ...this._allStudyItems]
    for (const item of allItems) {
      itemsByID.set(item.itemID, item)
    }

    // Merge with current recordings
    const recordings = this.loadAllRecordingsSync(true)
    for (const recording of recordings) {
      const stored = this.loadStoredMetadataByRecordingID(recording.id)
      const fallback = StudyItemMetadata.defaultMetadataForRecording(recording)
      const metadata = stored ? stored.mergedWithCurrentRecording(recording) : fallback
      itemsByID.set(metadata.itemID, metadata)
    }

    const foldersByID = new Map<string, StudyFolderMetadata>()
    const allFolders = [...this.loadAllFolderMetadata(), ...this._allStudyFolders]
    for (const folder of allFolders) {
      foldersByID.set(folder.folderID, folder)
    }

    const items = [...itemsByID.values()].map(i => i.syncSanitized(deviceID))
    const folders = [...foldersByID.values()].map(f => f.syncSanitized(deviceID))
    const tombstones = this.makeSyncTombstones(items, folders, deviceID)
    const pendingUploads = this.makePendingRecordingUploads(recordings, itemsByID, deviceID)

    return StudyLibrarySyncManifest.make(
      deviceID, generatedAt, 1,
      items, folders, tombstones, pendingUploads
    )
  }

  // =========================================================================
  // Apply sync manifest
  // Ported from iOS applySyncManifest
  // =========================================================================
  async applySyncManifest(manifest: StudyLibrarySyncManifest, localDeviceID: string): Promise<StudyLibrarySyncApplyResult> {
    if (!manifest.hasValidChecksum) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.CHECKSUM_MISMATCH)
    }

    const result = new StudyLibrarySyncApplyResult()

    // Apply folders
    for (const incomingFolder of manifest.folders) {
      try {
        let remote = incomingFolder.syncSanitized(manifest.deviceID)
        const existing = this.folderByFolderID(remote.folderID)
        const merged = this.mergedSyncFolder(existing, remote, result)
        if (merged) {
          await this.saveFolder(merged)
          result.appliedFolderCount++
        }
      } catch (_e) {
        result.failedChanges++
      }
    }

    // Apply items
    for (const incomingItem of manifest.items) {
      try {
        let remote = incomingItem.syncSanitized(manifest.deviceID)
        this.markSyncMetadataOnlyIfNeeded(remote)
        const existing = this.editableMetadataIfAvailableByItemID(remote.itemID)
        const merged = this.mergedSyncItem(existing, remote, result)
        if (merged) {
          await this.saveItemInternal(merged, existing)
          result.appliedItemCount++
        }
      } catch (_e) {
        result.failedChanges++
      }
    }

    // Apply tombstones
    for (const tombstone of manifest.tombstones) {
      try {
        if (await this.applySyncTombstone(tombstone, manifest.deviceID)) {
          result.tombstoneCount++
        }
      } catch (_e) {
        result.failedChanges++
      }
    }

    await this.refresh()
    return result
  }

  // =========================================================================
  // Conflict merge: items (last-write-wins + equal timestamp conflict)
  // Ported from iOS mergedSyncItem
  // =========================================================================
  private mergedSyncItem(
    existing: StudyItemMetadata | null,
    incoming: StudyItemMetadata,
    result: StudyLibrarySyncApplyResult
  ): StudyItemMetadata | null {
    if (!existing) return incoming

    if (incoming.updatedAt > existing.updatedAt) {
      // Incoming is newer → last-write-wins: accept incoming
      return incoming
    }

    if (incoming.updatedAt === existing.updatedAt && incoming.toJSON().itemID !== existing.toJSON().itemID) {
      // Equal timestamp, different content → conflict: preserve local
      const preserved = existing.clone()
      preserved.syncConflictStatus = 'conflict_preserved_local'
      result.conflictCount++
      return preserved
    }

    // Local is newer or equal and same → skip incoming
    result.skippedOlderCount++
    return null
  }

  // =========================================================================
  // Conflict merge: folders (last-write-wins + merge IDs)
  // Ported from iOS mergedSyncFolder
  // =========================================================================
  private mergedSyncFolder(
    existing: StudyFolderMetadata | null,
    incoming: StudyFolderMetadata,
    result: StudyLibrarySyncApplyResult
  ): StudyFolderMetadata | null {
    if (!existing) return incoming

    if (incoming.updatedAt > existing.updatedAt) {
      // Incoming is newer: accept, but merge IDs
      const merged = incoming.clone()
      merged.itemIDs = uniqueIDs([...existing.itemIDs, ...incoming.itemIDs])
      merged.childFolderIDs = uniqueIDs([...existing.childFolderIDs, ...incoming.childFolderIDs])
      return merged
    }

    if (incoming.updatedAt === existing.updatedAt) {
      // Check if content differs
      const incomingJSON = JSON.stringify(incoming.toJSON())
      const existingJSON = JSON.stringify(existing.toJSON())
      if (incomingJSON !== existingJSON) {
        // Equal timestamp, different content → conflict: preserve local, merge IDs
        const preserved = existing.clone()
        preserved.itemIDs = uniqueIDs([...existing.itemIDs, ...incoming.itemIDs])
        preserved.childFolderIDs = uniqueIDs([...existing.childFolderIDs, ...incoming.childFolderIDs])
        preserved.syncConflictStatus = 'conflict_preserved_local'
        result.conflictCount++
        return preserved
      }
    }

    result.skippedOlderCount++
    return null
  }

  // =========================================================================
  // Tombstone handling
  // Ported from iOS applySyncTombstone
  // =========================================================================
  private async applySyncTombstone(
    tombstone: StudyLibrarySyncTombstone,
    remoteDeviceID: string
  ): Promise<boolean> {
    switch (tombstone.entityKind) {
      case StudyLibrarySyncEntityKind.ITEM: {
        const item = this.editableMetadataIfAvailableByItemID(tombstone.entityID)
        if (!item || tombstone.updatedAt < item.updatedAt) return false
        const updated = item.clone()
        updated.isTrashed = tombstone.operation === StudyLibrarySyncOperation.TRASH ||
          tombstone.operation === StudyLibrarySyncOperation.DELETE ||
          tombstone.operation === StudyLibrarySyncOperation.DELETE_METADATA_ONLY
        updated.trashedAt = updated.isTrashed ? tombstone.updatedAt : null
        updated.updatedAt = tombstone.updatedAt
        updated.modifiedByDeviceID = tombstone.modifiedByDeviceID ?? remoteDeviceID
        await this.saveItemInternal(updated, item)
        return true
      }
      case StudyLibrarySyncEntityKind.FOLDER: {
        const folder = this.loadStoredFolder(tombstone.entityID)
        if (!folder || tombstone.updatedAt < folder.updatedAt) return false
        const updated = folder.clone()
        updated.isTrashed = tombstone.operation === StudyLibrarySyncOperation.TRASH ||
          tombstone.operation === StudyLibrarySyncOperation.DELETE ||
          tombstone.operation === StudyLibrarySyncOperation.DELETE_METADATA_ONLY
        updated.trashedAt = updated.isTrashed ? tombstone.updatedAt : null
        updated.updatedAt = tombstone.updatedAt
        updated.modifiedByDeviceID = tombstone.modifiedByDeviceID ?? remoteDeviceID
        await this.saveFolder(updated)
        return true
      }
    }
  }

  // =========================================================================
  // Private: makeSyncTombstones
  // Ported from iOS makeSyncTombstones
  // =========================================================================
  private makeSyncTombstones(
    items: StudyItemMetadata[],
    folders: StudyFolderMetadata[],
    deviceID: string
  ): StudyLibrarySyncTombstone[] {
    const itemTombstones = items.filter(i => i.isTrashed).map(item =>
      new StudyLibrarySyncTombstone(
        `item:${item.itemID}`,
        StudyLibrarySyncEntityKind.ITEM,
        item.itemID,
        StudyLibrarySyncOperation.TRASH,
        item.trashedAt ?? item.updatedAt,
        item.modifiedByDeviceID ?? deviceID
      )
    )
    const folderTombstones = folders.filter(f => f.isTrashed).map(folder =>
      new StudyLibrarySyncTombstone(
        `folder:${folder.folderID}`,
        StudyLibrarySyncEntityKind.FOLDER,
        folder.folderID,
        StudyLibrarySyncOperation.TRASH,
        folder.trashedAt ?? folder.updatedAt,
        folder.modifiedByDeviceID ?? deviceID
      )
    )
    return [...itemTombstones, ...folderTombstones]
  }

  // =========================================================================
  // Private: makePendingRecordingUploads
  // =========================================================================
  private makePendingRecordingUploads(
    recordings: RecordingMetadataLike[],
    itemsByID: Map<string, StudyItemMetadata>,
    targetDeviceID: string
  ): PendingRecordingUpload[] {
    const results: PendingRecordingUpload[] = []
    for (const recording of recordings) {
      if (recording.isDeleted) continue
      if (recording.uploadStatus === 'uploaded') continue

      const fallbackItemID = StudyItemMetadata.recordingBundleItemID(recording.id)
      const item = itemsByID.get(fallbackItemID) ?? StudyItemMetadata.defaultMetadataForRecording(recording)
      results.push(new PendingRecordingUpload(
        item.itemID,
        recording.id,
        recording.relativeAudioPath ?? '',
        targetDeviceID,
        recording.uploadStatus === 'uploading' ? PendingRecordingUploadStatus.UPLOADING
          : recording.uploadStatus === 'failed' ? PendingRecordingUploadStatus.FAILED
            : PendingRecordingUploadStatus.PENDING,
        recording.createdAt instanceof Date ? recording.createdAt.toISOString() : String(recording.createdAt),
        item.updatedAt
      ))
    }
    return results
  }

  // =========================================================================
  // Private: markSyncMetadataOnlyIfNeeded
  // Ported from iOS markSyncMetadataOnlyIfNeeded
  // =========================================================================
  private markSyncMetadataOnlyIfNeeded(item: StudyItemMetadata): void {
    if (!item.recordingID) return
    const recordings = this.loadAllRecordingsSync(false)
    const exists = recordings.some(r => r.id === item.recordingID)
    if (!exists) {
      item.customProperties['syncedMetadataOnly'] = 'true'
    }
  }

  // =========================================================================
  // Private: RecordingReceiveRecord loading
  // Ported from iOS loadReceiveRecordDerivedItems
  // =========================================================================
  private loadReceiveRecordDerivedItems(): StudyItemMetadata[] {
    const inboxURL = this.joinPath(this.rootURL, 'audio', 'inbox')
    if (!this.isInsideRoot(inboxURL)) return []
    if (!this.fileExists(inboxURL)) return []

    const items: StudyItemMetadata[] = []
    try {
      const receiveFiles = this.findFilesRecursive(inboxURL, 'receive.json')
      for (const receiveURL of receiveFiles) {
        if (!this.isInsideRoot(receiveURL)) continue
        try {
          const data = this.readTextFile(receiveURL)
          const json = JSON.parse(data) as Record<string, Object>
          const record = RecordingReceiveRecord.fromJSON(json)
          const relativePath = this.relativePathForURL(receiveURL)
          const item = StudyItemMetadata.defaultMetadataForReceiveRecord(record, relativePath)
          if (item) items.push(item)
        } catch (_e) { /* skip */ }
      }
    } catch (_e) { /* ignore */ }

    return items
  }

  // =========================================================================
  // Private: Folder link sync (port of syncFolderLinks)
  // =========================================================================
  private async syncFolderLinks(
    metadata: StudyItemMetadata,
    previousMetadata: StudyItemMetadata | null
  ): Promise<void> {
    const foldersByID = new Map<string, StudyFolderMetadata>()
    for (const f of this.loadAllFolderMetadata()) {
      foldersByID.set(f.folderID, f)
    }

    const itemID = metadata.itemID
    const previousFolderIDs = new Set(previousMetadata?.folderIDs ?? [])
    const targetFolderIDs = new Set(metadata.folderIDs)

    // Remove from previous folders
    for (const folderID of previousFolderIDs) {
      if (targetFolderIDs.has(folderID)) continue
      const folder = foldersByID.get(folderID)
      if (!folder) continue
      const updated = folder.clone()
      updated.itemIDs = updated.itemIDs.filter(id => id !== itemID)
      updated.updatedAt = new Date().toISOString()
      foldersByID.set(folderID, updated)
    }

    // Build folder chain from filing path
    const chain = this.folderChainForFiling(metadata.filing, itemID)
    for (const folder of chain) {
      let stored = foldersByID.get(folder.folderID)
      if (!stored) {
        stored = folder
      } else {
        stored = stored.clone()
        stored.name = folder.name
        stored.level = folder.level
        stored.path = folder.path
        stored.parentFolderID = folder.parentFolderID
        stored.childFolderIDs = uniqueIDs([...stored.childFolderIDs, ...folder.childFolderIDs])
      }
      if (targetFolderIDs.has(folder.folderID) && !stored.itemIDs.includes(itemID)) {
        stored.itemIDs.push(itemID)
      }
      if (!targetFolderIDs.has(folder.folderID)) {
        stored.itemIDs = stored.itemIDs.filter(id => id !== itemID)
      }
      stored.updatedAt = new Date().toISOString()
      foldersByID.set(folder.folderID, stored)
    }

    // Ensure target folders have the item
    for (const folderID of targetFolderIDs) {
      const folder = foldersByID.get(folderID)
      if (!folder) continue
      const updated = folder.clone()
      if (!updated.itemIDs.includes(itemID)) {
        updated.itemIDs.push(itemID)
      }
      updated.updatedAt = new Date().toISOString()
      foldersByID.set(folderID, updated)
    }

    for (const [, folder] of foldersByID) {
      await this.saveFolder(folder)
    }
  }

  // =========================================================================
  // Private: folder chain from filing path
  // Ported from iOS folderChain
  // =========================================================================
  private folderChainForFiling(filing: StudyFilingPath, itemID: string): StudyFolderMetadata[] {
    const effectiveFiling = StudyItemMetadata.effectiveFolderPath(filing)
    const values: Array<[StudyFolderLevel, string | null]> = [
      [StudyFolderLevel.TYPE, effectiveFiling.type],
      [StudyFolderLevel.SUBJECT, effectiveFiling.subject],
      [StudyFolderLevel.CHAPTER, effectiveFiling.chapter],
      [StudyFolderLevel.TOPIC, effectiveFiling.topic]
    ]

    const folders: StudyFolderMetadata[] = []
    let parentFolderID: string | null = null
    const presentValues = values.filter(([, v]) => v !== null)

    for (let i = 0; i < presentValues.length; i++) {
      const [level, value] = presentValues[i]
      if (!value) break

      const components = presentValues.slice(0, i + 1).map(([, v]) => v!)
      const path = StudyFolderMetadata.filingPathForComponents(components)
      let childFolderIDs: string[] = []
      if (i + 1 < presentValues.length) {
        const childPath = StudyFolderMetadata.filingPathForComponents(
          presentValues.slice(0, i + 2).map(([, v]) => v!)
        )
        childFolderIDs = [StudyFolderMetadata.folderIDFromLevelAndPath(presentValues[i + 1][0], childPath)]
      }
      const isLeaf = i === presentValues.length - 1
      folders.push(new StudyFolderMetadata({
        name: value,
        level: level,
        path: path,
        parentFolderID: parentFolderID,
        childFolderIDs: childFolderIDs,
        itemIDs: isLeaf ? [itemID] : []
      }))
      parentFolderID = folders[folders.length - 1].folderID
    }

    return folders
  }

  // =========================================================================
  // Private: parent folder ID for browse path
  // =========================================================================
  private parentFolderIDForPath(path: StudyBrowsePath): string | null {
    if (path.isRoot) return null
    const parentLevel = StudyFolderMetadata.levelForDepth(path.depth - 1)
    if (!parentLevel) return null
    return StudyFolderMetadata.folderIDFromLevelAndPath(
      parentLevel,
      StudyFolderMetadata.filingPathForComponents(path.components)
    )
  }

  // =========================================================================
  // Private: append child folder ID to parent
  // =========================================================================
  private async appendChildFolderID(
    childFolderID: string,
    parentFolderID: string,
    parentPath: StudyBrowsePath
  ): Promise<void> {
    const parentLevel = StudyFolderMetadata.levelForDepth(parentPath.depth - 1)
    if (!parentLevel) return

    let parent = this.loadStoredFolder(parentFolderID)
    if (!parent) {
      parent = new StudyFolderMetadata({
        name: parentPath.components[parentPath.components.length - 1] ?? StudyHierarchyRule.uncategorizedValue,
        level: parentLevel,
        path: StudyFolderMetadata.filingPathForComponents(parentPath.components),
        parentFolderID: this.parentFolderIDForPath(parentPath.parent)
      })
    }
    const updatedParent = parent.clone()
    if (!updatedParent.childFolderIDs.includes(childFolderID)) {
      updatedParent.childFolderIDs.push(childFolderID)
      updatedParent.updatedAt = new Date().toISOString()
      await this.saveFolder(updatedParent)
    }
  }

  // =========================================================================
  // Private: item matches folder
  // Ported from iOS item(_:matches:through:)
  // =========================================================================
  private itemMatchesFolder(item: StudyItemMetadata, folderPath: StudyFilingPath, level: StudyFolderLevel): boolean {
    const itemComponents = StudyFolderMetadata.pathComponentsForPath(item.filing, level)
    const folderComponents = StudyFolderMetadata.pathComponentsForPath(folderPath, level)
    if (itemComponents.length !== folderComponents.length) return false
    return itemComponents.every((c, i) => c === folderComponents[i])
  }

  // =========================================================================
  // Private: renamed path (replacing level with name)
  // Ported from iOS renamedPath
  // =========================================================================
  private renamedPath(path: StudyFilingPath, level: StudyFolderLevel, name: string): StudyFilingPath | null {
    switch (level) {
      case StudyFolderLevel.TYPE:
        if (!path.type) return null
        return new StudyFilingPath(name, path.subject, path.chapter, path.topic)
      case StudyFolderLevel.SUBJECT:
        if (!path.subject) return null
        return new StudyFilingPath(path.type, name, path.chapter, path.topic)
      case StudyFolderLevel.CHAPTER:
        if (!path.chapter) return null
        return new StudyFilingPath(path.type, path.subject, name, path.topic)
      case StudyFolderLevel.TOPIC:
        if (!path.topic) return null
        return new StudyFilingPath(path.type, path.subject, path.chapter, name)
      default:
        return null
    }
  }

  // =========================================================================
  // Private: repaired folders
  // Ported from iOS repairedFolders
  // =========================================================================
  private repairedFolders(
    folders: StudyFolderMetadata[],
    items: StudyItemMetadata[]
  ): StudyFolderMetadata[] {
    const existingItemIDs = new Set(items.map(i => i.itemID))
    const foldersByID = new Map<string, StudyFolderMetadata>()

    for (const folder of folders) {
      const repaired = folder.clone()
      repaired.itemIDs = uniqueIDs(repaired.itemIDs.filter(id => existingItemIDs.has(id)))
      foldersByID.set(folder.folderID, repaired)
    }

    for (const item of items) {
      for (const folderID of item.folderIDs) {
        const folder = foldersByID.get(folderID)
        if (!folder) continue
        if (!folder.itemIDs.includes(item.itemID)) {
          folder.itemIDs.push(item.itemID)
        }
      }
    }

    return [...foldersByID.values()].sort((a, b) => {
      const aLen = a.pathComponents.length
      const bLen = b.pathComponents.length
      if (aLen === bLen) return a.name.localeCompare(b.name)
      for (let i = 0; i < Math.min(aLen, bLen); i++) {
        const cmp = a.pathComponents[i].localeCompare(b.pathComponents[i])
        if (cmp !== 0) return cmp
      }
      return aLen - bLen
    })
  }

  // =========================================================================
  // Private: shouldIncludeStoredItem
  // Ported from iOS shouldIncludeStoredItem
  // =========================================================================
  private shouldIncludeStoredItem(
    item: StudyItemMetadata,
    liveRecordingIDs: Set<string>,
    alreadyLoaded: Map<string, StudyItemMetadata>
  ): boolean {
    if (alreadyLoaded.has(item.itemID)) return false
    if (item.kind === StudyItemKind.STANDALONE_NOTE || !item.recordingID) return true
    if (item.customProperties['syncedMetadataOnly'] === 'true') return true
    return item.recordingID ? liveRecordingIDs.has(item.recordingID) : false
  }

  // =========================================================================
  // Private: editable metadata
  // Ported from iOS editableMetadata / editableMetadataIfAvailable
  // =========================================================================
  private async editableMetadata(recordingID: string): Promise<StudyItemMetadata> {
    const recordings = this.loadAllRecordingsSync(false)
    const recording = recordings.find(r => r.id === recordingID)
    if (recording) {
      const stored = this.loadStoredMetadataByRecordingID(recordingID)
      const fallback = StudyItemMetadata.defaultMetadataForRecording(recording)
      return stored ? stored.mergedWithCurrentRecording(recording) : fallback
    }

    const stored = this.loadStoredMetadataByRecordingID(recordingID)
    if (stored) return stored

    throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.ITEM_MISSING)
  }

  private editableMetadataIfAvailableByItemID(itemID: string): StudyItemMetadata | null {
    const item = this._allStudyItems.find(i => i.itemID === itemID || i.recordingID === itemID)
    if (item) return item
    return this.loadAllStoredItemMetadata().find(i => i.itemID === itemID || i.recordingID === itemID) ?? null
  }

  private editableMetadataIfAvailableByRecordingID(recordingID: string): StudyItemMetadata | null {
    return this._allStudyItems.find(i => i.recordingID === recordingID) ?? null
      ?? this.loadStoredMetadataByRecordingID(recordingID)
  }

  // =========================================================================
  // File I/O helpers
  // =========================================================================

  private refreshSync(): void {
    // Synchronous refresh without async (for makeSyncManifest)
    const recordings = this.loadAllRecordingsSync(true)
    const storedItems = this.loadAllStoredItemMetadata()
    const receiveItems = this.loadReceiveRecordDerivedItems()
    const itemsByID = new Map<string, StudyItemMetadata>()

    const storedByRecordingID = new Map<string, StudyItemMetadata>()
    for (const item of storedItems) {
      if (item.recordingID) storedByRecordingID.set(item.recordingID, item)
    }

    const liveIDs = new Set(recordings.map(r => r.id))
    for (const recording of recordings) {
      const stored = storedByRecordingID.get(recording.id)
      const fallback = StudyItemMetadata.defaultMetadataForRecording(recording)
      const metadata = stored ? stored.mergedWithCurrentRecording(recording) : fallback
      itemsByID.set(metadata.itemID, metadata)
    }
    for (const item of receiveItems) {
      const rID = item.recordingID
      if (rID && liveIDs.has(rID)) continue
      itemsByID.set(item.itemID, item)
    }
    for (const item of storedItems) {
      if (this.shouldIncludeStoredItem(item, liveIDs, itemsByID)) {
        itemsByID.set(item.itemID, item)
      }
    }
    const items = [...itemsByID.values()].sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    const folders = this.repairedFolders(
      this.loadAllFolderMetadata().filter(f => !f.isTrashed),
      items
    )
    this._allStudyItems = items
    this._allStudyFolders = folders
    this._filingCandidates = StudyFilingCandidates.collectFromItems(items)
  }

  private loadAllRecordingsSync(includeDeleted: boolean): RecordingMetadataLike[] {
    try {
      const metadataDir = this.joinPath(this.rootURL, 'Metadata')
      if (!this.fileExists(metadataDir)) return []
      const files = fileIo.listFileSync(metadataDir)
      const results: RecordingMetadataLike[] = []
      for (const file of files) {
        if (!file.endsWith('.json')) continue
        try {
          const data = this.readTextFile(this.joinPath(metadataDir, file))
          const json = JSON.parse(data) as Record<string, Object>
          const rec: RecordingMetadataLike = {
            id: (json['id'] as string) ?? '',
            title: (json['title'] as string) ?? '',
            relativeAudioPath: (json['relativeAudioPath'] as string) ?? null,
            createdAt: (json['createdAt'] as string) ?? new Date().toISOString(),
            duration: (json['duration'] as number) ?? 0,
            transcriptionStatus: (json['transcriptionStatus'] as string) ?? null,
            noteStatus: (json['noteStatus'] as string) ?? null,
            isDeleted: (json['isDeleted'] as boolean) ?? false,
            uploadStatus: (json['uploadStatus'] as string) ?? 'localOnly',
            studyFiling: json['studyFiling'] ? StudyFilingPath.fromJSON(json['studyFiling'] as Record<string, string>) : null,
            deletedAt: (json['deletedAt'] as string) ?? null
          }
          if (includeDeleted || !rec.isDeleted) results.push(rec)
        } catch (_e) { /* skip */ }
      }
      return results.sort((a, b) => String(b.createdAt).localeCompare(String(a.createdAt)))
    } catch {
      return []
    }
  }

  private loadStoredMetadataByRecordingID(recordingID: string): StudyItemMetadata | null {
    const recordingItemID = StudyItemMetadata.recordingBundleItemID(recordingID)
    return this.loadAllStoredItemMetadata().find(m =>
      m.recordingID === recordingID || m.itemID === recordingItemID
    ) ?? null
  }

  private loadAllStoredItemMetadata(): StudyItemMetadata[] {
    return [
      ...this.loadMetadataFilesFromDir(this.itemMetadataURL),
      ...this.loadMetadataFilesFromDir(this.legacyItemMetadataURL)
    ]
  }

  private loadAllFolderMetadata(): StudyFolderMetadata[] {
    return this.loadMetadataFilesFromDir(this.folderMetadataURL).map(json =>
      StudyFolderMetadata.fromJSON(json)
    )
  }

  private loadMetadataFilesFromDir(dirURL: string): Record<string, Object>[] {
    try {
      if (!this.fileExists(dirURL)) return []
      const fileNames = this.loadMetadataFileNames(dirURL)
      const results: Record<string, Object>[] = []
      for (const fileName of fileNames) {
        const url = this.joinPath(dirURL, fileName)
        if (!this.isInsideStudyDirectory(url)) continue
        try {
          const data = this.readTextFile(url)
          results.push(JSON.parse(data) as Record<string, Object>)
        } catch (_e) { /* skip */ }
      }
      return results
    } catch {
      return []
    }
  }

  private loadMetadataFileNames(dirURL: string): string[] {
    try {
      if (!this.fileExists(dirURL)) return []
      const files = fileIo.listFileSync(dirURL)
      return files
        .filter(f => f.endsWith('.json'))
        .sort((a, b) => a.localeCompare(b))
    } catch {
      return []
    }
  }

  private loadStoredFolder(folderID: string): StudyFolderMetadata | null {
    const index = this.loadIndex()
    const candidateFileNames = [
      index.folderMetadataFilesByFolderID[folderID],
      `${StudyPathSanitizer.sanitizedPathComponent(folderID)}.json`
    ].filter((f): f is string => !!f)

    const allFileNames = [...new Set([...candidateFileNames, ...this.loadMetadataFileNames(this.folderMetadataURL)])]
    for (const fileName of allFileNames) {
      const url = this.joinPath(this.folderMetadataURL, fileName)
      if (!this.isInsideFolderMetadataDirectory(url)) continue
      if (!this.fileExists(url)) continue
      try {
        const data = this.readTextFile(url)
        const json = JSON.parse(data) as Record<string, Object>
        const folder = StudyFolderMetadata.fromJSON(json)
        if (folder.folderID === folderID) return folder
      } catch (_e) { /* skip */ }
    }
    return null
  }

  // =========================================================================
  // Index management
  // =========================================================================
  private loadIndex(): StudyMetadataIndex {
    const urls = [this.indexURL, this.legacyIndexURL]
    for (const url of urls) {
      if (!this.fileExists(url)) continue
      try {
        const data = this.readTextFile(url)
        return StudyMetadataIndex.fromJSON(JSON.parse(data) as Record<string, Object>)
      } catch (_e) { /* continue */ }
    }
    return new StudyMetadataIndex()
  }

  private saveIndex(index: StudyMetadataIndex): void {
    if (!this.isInsideStudyDirectory(this.indexURL)) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.UNSAFE_DESTINATION)
    }
    this.atomicWriteJSON(this.indexURL, {
      itemMetadataFilesByItemID: index.itemMetadataFilesByItemID,
      itemMetadataFilesByRecordingID: index.itemMetadataFilesByRecordingID,
      folderMetadataFilesByFolderID: index.folderMetadataFilesByFolderID,
      updatedAt: index.updatedAt
    })
  }

  // =========================================================================
  // Hierarchy rules
  // =========================================================================
  private loadHierarchyRules(): StudyHierarchyRule[] {
    if (!this.fileExists(this.hierarchyRulesURL)) {
      // Write default
      try {
        this.atomicWriteJSON(this.hierarchyRulesURL,
          [StudyHierarchyRule.defaultCourseView].map(r => ({
            id: r.id,
            name: r.name,
            levels: r.levels
          }))
        )
      } catch (_e) { /* ignore */ }
      return [StudyHierarchyRule.defaultCourseView]
    }
    try {
      const data = this.readTextFile(this.hierarchyRulesURL)
      const rulesJSON = JSON.parse(data) as Array<Record<string, Object>>
      const rules = rulesJSON.map(r => StudyHierarchyRule.fromJSON(r))
      return rules.length > 0 ? rules : [StudyHierarchyRule.defaultCourseView]
    } catch {
      return [StudyHierarchyRule.defaultCourseView]
    }
  }

  // =========================================================================
  // Directory initialization
  // Ported from iOS ensureStudyDirectories
  // =========================================================================
  private ensureStudyDirectories(): void {
    if (!this.isInsideRoot(this.studyURL) ||
      !this.isInsideStudyDirectory(this.itemMetadataURL) ||
      !this.isInsideStudyDirectory(this.folderMetadataURL) ||
      !this.isInsideStudyDirectory(this.indexURL)) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.UNSAFE_DESTINATION)
    }

    try {
      this.mkdirIfNeeded(this.itemMetadataURL)
      this.mkdirIfNeeded(this.folderMetadataURL)
      if (!this.fileExists(this.hierarchyRulesURL)) {
        this.atomicWriteJSON(this.hierarchyRulesURL,
          [StudyHierarchyRule.defaultCourseView].map(r => ({
            id: r.id, name: r.name, levels: r.levels
          }))
        )
      }
      if (!this.fileExists(this.indexURL)) {
        this.atomicWriteJSON(this.indexURL, {
          itemMetadataFilesByItemID: {},
          itemMetadataFilesByRecordingID: {},
          folderMetadataFilesByFolderID: {},
          updatedAt: new Date(0).toISOString()
        })
      }
    } catch (e) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.UNABLE_TO_CREATE_DIRECTORY, String(e))
    }
  }

  // =========================================================================
  // Atomic write
  // Ported from iOS write(to: .atomic) behavior
  // =========================================================================
  private atomicWriteJSON(filePath: string, data: Record<string, Object>): void {
    const jsonString = JSON.stringify(data, sortedKeysReplacer, 2)
    // Atomic write: write to temp file, then rename
    const tempPath = filePath + '.tmp.' + Date.now().toString(16)
    try {
      this.writeTextFile(tempPath, jsonString)
      // Rename is atomic on most filesystems
      try { fileIo.unlinkSync(filePath) } catch (_e) { /* ignore if not exists */ }
      fileIo.renameSync(tempPath, filePath)
    } catch (e) {
      // Clean up temp on failure
      try { fileIo.unlinkSync(tempPath) } catch (_e) { /* ignore */ }
      throw e
    }
  }

  // =========================================================================
  // Sandbox path safety checks
  // Ported from iOS isInsideRoot / isInsideStudyDirectory
  // =========================================================================
  private isInsideRoot(url: string): boolean {
    // Normalize paths
    const rootPath = this.normalizePath(this.rootURL)
    const targetPath = this.normalizePath(url)
    return targetPath === rootPath || targetPath.startsWith(rootPath + '/')
  }

  private isInsideStudyDirectory(url: string): boolean {
    const studyPath = this.normalizePath(this.studyURL)
    const targetPath = this.normalizePath(url)
    return targetPath === studyPath || targetPath.startsWith(studyPath + '/')
  }

  private isInsideItemMetadataDirectory(url: string): boolean {
    const itemMetaPath = this.normalizePath(this.itemMetadataURL)
    const targetPath = this.normalizePath(url)
    return targetPath === itemMetaPath || targetPath.startsWith(itemMetaPath + '/')
  }

  private isInsideFolderMetadataDirectory(url: string): boolean {
    const folderMetaPath = this.normalizePath(this.folderMetadataURL)
    const targetPath = this.normalizePath(url)
    return targetPath === folderMetaPath || targetPath.startsWith(folderMetaPath + '/')
  }

  private relativePathForURL(url: string): string {
    const basePath = this.normalizePath(this.rootURL) + '/'
    const filePath = this.normalizePath(url)
    if (!filePath.startsWith(basePath)) {
      throw new StudyLibraryStoreError(StudyLibraryStoreErrorCode.UNSAFE_DESTINATION)
    }
    return filePath.substring(basePath.length)
  }

  // =========================================================================
  // Path utilities
  // =========================================================================
  private joinPath(...segments: string[]): string {
    return segments.join('/').replace(/\/+/g, '/')
  }

  private normalizePath(path: string): string {
    // Resolve . and .. where possible and remove trailing slash
    const parts = path.split('/').filter(p => p.length > 0 && p !== '.')
    const resolved: string[] = []
    for (const part of parts) {
      if (part === '..') {
        resolved.pop()
      } else {
        resolved.push(part)
      }
    }
    return (path.startsWith('/') ? '/' : '') + resolved.join('/')
  }

  private mkdirIfNeeded(dirPath: string): void {
    try {
      if (!fileIo.accessSync(dirPath)) {
        fileIo.mkdirSync(dirPath, true)
      }
    } catch (_e) {
      try { fileIo.mkdirSync(dirPath, true) } catch (e) { throw e }
    }
  }

  private fileExists(path: string): boolean {
    try {
      return fileIo.accessSync(path)
    } catch {
      return false
    }
  }

  private readTextFile(path: string): string {
    const file = fileIo.openSync(path, fileIo.OpenMode.READ_ONLY)
    try {
      const stat = fileIo.statSync(path)
      const buf = new ArrayBuffer(stat.size)
      fileIo.readSync(file.fd, buf)
      const bytes = new Uint8Array(buf)
      let result = ''
      for (let i = 0; i < bytes.length; i++) {
        result += String.fromCharCode(bytes[i])
      }
      return result
    } finally {
      fileIo.closeSync(file)
    }
  }

  private writeTextFile(path: string, content: string): void {
    const file = fileIo.openSync(path,
      fileIo.OpenMode.CREATE | fileIo.OpenMode.WRITE_ONLY | fileIo.OpenMode.TRUNC)
    try {
      fileIo.writeSync(file.fd, content)
    } finally {
      fileIo.closeSync(file)
    }
  }

  private findFilesRecursive(dirPath: string, targetName: string): string[] {
    const results: string[] = []
    try {
      const entries = fileIo.listFileSync(dirPath)
      for (const entry of entries) {
        const fullPath = this.joinPath(dirPath, entry)
        try {
          const stat = fileIo.statSync(fullPath)
          if (stat.isDirectory()) {
            results.push(...this.findFilesRecursive(fullPath, targetName))
          } else if (entry === targetName) {
            results.push(fullPath)
          }
        } catch (_e) { /* skip */ }
      }
    } catch (_e) { /* ignore */ }
    return results
  }

  // =========================================================================
  // Static helpers
  // =========================================================================
  static itemMetadataFileName(metadata: StudyItemMetadata): string {
    return `${StudyPathSanitizer.sanitizedPathComponent(metadata.itemID)}.json`
  }

  static folderMetadataFileName(folder: StudyFolderMetadata): string {
    return `${StudyPathSanitizer.sanitizedPathComponent(folder.folderID)}.json`
  }

  static pathComponentsStartWith(components: string[], prefix: string[]): boolean {
    if (components.length < prefix.length) return false
    return prefix.every((p, i) => components[i] === p)
  }
}

// =========================================================================
// Helpers
// =========================================================================
function sortedKeysReplacer(_key: string, value: Object): Object {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) {
    return value
  }
  const sorted: Record<string, Object> = {}
  const keys = Object.keys(value as Record<string, Object>).sort()
  for (const key of keys) {
    sorted[key] = (value as Record<string, Object>)[key]
  }
  return sorted
}

function StudyTagListUnique(tags: StudyTag[]): StudyTag[] {
  const seen = new Set<string>()
  const result: StudyTag[] = []
  for (const tag of tags) {
    const value = StudyTag.normalizedValue(tag.value)
    if (value.length === 0) continue
    const normalizedTag = new StudyTag(tag.namespace, value, tag.id, tag.displayName, tag.createdAt)
    const key = `${normalizedTag.namespace}${normalizedTag.value.toLowerCase()}`
    if (seen.has(key)) continue
    seen.add(key)
    result.push(normalizedTag)
  }
  return result
}
