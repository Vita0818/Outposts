/**
 * Local file storage service
 */
import { fileIo } from '@kit.CoreFileKit'
import { RecordingMetadata } from '../models/RecordingModels'

const TAG = 'RokuricsStorage'

function writeTextFile(path: string, content: string): void {
  const file: fileIo.File = fileIo.openSync(path, fileIo.OpenMode.CREATE | fileIo.OpenMode.WRITE_ONLY | fileIo.OpenMode.TRUNC)
  try {
    fileIo.writeSync(file.fd, content)
  } finally {
    fileIo.closeSync(file)
  }
}

function readTextFile(path: string): string {
  const file: fileIo.File = fileIo.openSync(path, fileIo.OpenMode.READ_ONLY)
  try {
    const stat: fileIo.Stat = fileIo.statSync(path)
    const buf: ArrayBuffer = new ArrayBuffer(stat.size)
    fileIo.readSync(file.fd, buf)
    const bytes: Uint8Array = new Uint8Array(buf)
    let result: string = ''
    for (let i: number = 0; i < bytes.length; i++) {
      result += String.fromCharCode(bytes[i])
    }
    return result
  } finally {
    fileIo.closeSync(file)
  }
}

export class AudioFileStore {
  private context: Context
  readonly baseDir: string

  constructor(context: Context) {
    this.context = context
    this.baseDir = context.filesDir + '/Rokurics'
  }

  async ensureStorageDirectories(): Promise<void> {
    const dirs: string[] = [this.baseDir, this.recordingsDir, this.metadataDir]
    for (const dir of dirs) {
      try {
        if (!fileIo.accessSync(dir)) {
          fileIo.mkdirSync(dir, true)
        }
      } catch (e) {
        throw new Error(`Failed to create directory: ${dir}`)
      }
    }
    console.info(`[${TAG}] directories ready:`)
  }

  get recordingsDir(): string {
    return this.baseDir + '/Recordings'
  }

  get metadataDir(): string {
    return this.baseDir + '/Metadata'
  }

  makeRecordingFileName(date: Date = new Date(), fallback: boolean = false): string {
    const suffix: string = fallback ? '_fallback' : ''
    const y: number = date.getFullYear()
    const mo: string = String(date.getMonth() + 1).padStart(2, '0')
    const d: string = String(date.getDate()).padStart(2, '0')
    const h: string = String(date.getHours()).padStart(2, '0')
    const mi: string = String(date.getMinutes()).padStart(2, '0')
    const s: string = String(date.getSeconds()).padStart(2, '0')
    return `${this.recordingsDir}/rokurics_${y}-${mo}-${d}_${h}-${mi}-${s}${suffix}.m4a`
  }

  makeMetadataPath(id: string): string {
    if (id.includes('..') || id.includes('/')) {
      throw new Error(`Invalid recording ID: ${id}`)
    }
    return `${this.metadataDir}/${id}.json`
  }

  async fileExists(path: string): Promise<boolean> {
    try {
      return fileIo.accessSync(path)
    } catch {
      return false
    }
  }

  async fileSize(path: string): Promise<number> {
    try {
      const stat: fileIo.Stat = fileIo.statSync(path)
      return stat.size
    } catch {
      return 0
    }
  }

  relativePath(absolutePath: string): string {
    const basePath: string = this.baseDir + '/'
    if (!absolutePath.startsWith(basePath)) {
      throw new Error(`Path outside Rokurics directory: ${absolutePath}`)
    }
    return absolutePath.substring(basePath.length)
  }

  async saveMetadata(metadata: RecordingMetadata): Promise<void> {
    const path: string = this.makeMetadataPath(metadata.id)
    try {
      const json: string = JSON.stringify(metadata)
      writeTextFile(path, json)
      console.info(`[${TAG}] metadata saved: ${path}`)
    } catch {
      throw new Error(`Failed to save metadata: ${path}`)
    }
  }

  async loadMetadata(id: string): Promise<RecordingMetadata> {
    const path: string = this.makeMetadataPath(id)
    try {
      const text: string = readTextFile(path)
      const json: Record<string, Object> = JSON.parse(text) as Record<string, Object>
      return RecordingMetadata.fromJSON(json)
    } catch {
      throw new Error(`Failed to load metadata for ${id}`)
    }
  }

  async loadAllMetadata(includeDeleted: boolean = false): Promise<RecordingMetadata[]> {
    try {
      const files: string[] = fileIo.listFileSync(this.metadataDir)
      const recordings: RecordingMetadata[] = []
      for (const file of files) {
        if (!file.endsWith('.json')) continue
        try {
          const text: string = readTextFile(this.metadataDir + '/' + file)
          const json: Record<string, Object> = JSON.parse(text) as Record<string, Object>
          const rec: RecordingMetadata = RecordingMetadata.fromJSON(json)
          if (includeDeleted || !rec.isDeleted) {
            recordings.push(rec)
          }
        } catch {
          // skip corrupted files
        }
      }
      recordings.sort((a: RecordingMetadata, b: RecordingMetadata) => b.createdAt.getTime() - a.createdAt.getTime())
      return recordings
    } catch {
      return []
    }
  }

  async updateTitle(recordingID: string, title: string): Promise<RecordingMetadata> {
    const metadata: RecordingMetadata = await this.loadMetadata(recordingID)
    const updated: RecordingMetadata = metadata.copyWithTitle(title)
    await this.saveMetadata(updated)
    return updated
  }

  async moveToTrash(metadata: RecordingMetadata): Promise<RecordingMetadata> {
    const updated: RecordingMetadata = metadata.copyWithTrashState(true, new Date())
    await this.saveMetadata(updated)
    return updated
  }

  async restoreRecording(id: string): Promise<RecordingMetadata> {
    const metadata: RecordingMetadata = await this.loadMetadata(id)
    const updated: RecordingMetadata = metadata.copyWithTrashState(false, null)
    await this.saveMetadata(updated)
    return updated
  }

  async permanentlyDelete(id: string): Promise<void> {
    const metadata: RecordingMetadata = await this.loadMetadata(id)
    const audioPath: string = this.baseDir + '/' + metadata.relativeAudioPath
    const metaPath: string = this.makeMetadataPath(id)
    try {
      if (await this.fileExists(audioPath)) fileIo.unlinkSync(audioPath)
    } catch { /* ignore */ }
    try {
      if (await this.fileExists(metaPath)) fileIo.unlinkSync(metaPath)
    } catch { /* ignore */ }
  }
}
