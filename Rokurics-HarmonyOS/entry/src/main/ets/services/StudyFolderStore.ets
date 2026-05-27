/**
 * Persistent folder store for study library folder hierarchy
 * Persists folder metadata to JSON filesystem
 */
import { fileIo } from '@kit.CoreFileKit'

const TAG = 'RokuricsFolderStore'

export interface StudyFolderRecord {
  id: string
  name: string
  level: string
  parentPath: string[]
  createdAt: string
  colorToken: string | null
}

export class StudyFolderStore {
  private baseDir: string
  private indexPath: string

  constructor(context: Context) {
    this.baseDir = context.filesDir + '/Rokurics/Folders'
    this.indexPath = this.baseDir + '/folders.json'
    this.ensureDirectories()
  }

  private ensureDirectories(): void {
    try {
      if (!fileIo.accessSync(this.baseDir)) {
        fileIo.mkdirSync(this.baseDir, true)
      }
    } catch (_e) { /* ignore */ }
  }

  async createFolder(name: string, level: string, parentPath: string[]): Promise<StudyFolderRecord> {
    const folders = await this.loadAll()
    const existing = folders.find(f =>
      f.name === name && f.level === level &&
      JSON.stringify(f.parentPath) === JSON.stringify(parentPath)
    )
    if (existing) return existing

    const id = this.makeUUID()
    const record: StudyFolderRecord = {
      id: id,
      name: name,
      level: level,
      parentPath: parentPath,
      createdAt: new Date().toISOString(),
      colorToken: null
    }
    folders.push(record)
    this.saveAll(folders)
    console.info(`[${TAG}] created folder: ${name} (${level}) id=${id}`)
    return record
  }

  async listFolders(level?: string): Promise<StudyFolderRecord[]> {
    const all = await this.loadAll()
    if (level) return all.filter(f => f.level === level)
    return all
  }

  async deleteFolder(id: string): Promise<void> {
    const folders = await this.loadAll()
    const idx = folders.findIndex(f => f.id === id)
    if (idx >= 0) {
      folders.splice(idx, 1)
      this.saveAll(folders)
      console.info(`[${TAG}] deleted folder: ${id}`)
    }
  }

  async renameFolder(id: string, newName: string): Promise<StudyFolderRecord | null> {
    const folders = await this.loadAll()
    const folder = folders.find(f => f.id === id)
    if (folder) {
      folder.name = newName
      this.saveAll(folders)
      console.info(`[${TAG}] renamed folder: ${id} -> ${newName}`)
      return folder
    }
    return null
  }

  async setColorToken(id: string, colorToken: string | null): Promise<StudyFolderRecord | null> {
    const folders = await this.loadAll()
    const folder = folders.find(f => f.id === id)
    if (folder) {
      folder.colorToken = colorToken
      this.saveAll(folders)
      console.info(`[${TAG}] set color token for folder: ${id} -> ${colorToken}`)
      return folder
    }
    return null
  }

  async findFolder(name: string, level: string): Promise<StudyFolderRecord | null> {
    const folders = await this.loadAll()
    return folders.find(f => f.name === name && f.level === level) ?? null
  }

  private async loadAll(): Promise<StudyFolderRecord[]> {
    try {
      if (!fileIo.accessSync(this.indexPath)) return []
      const file: fileIo.File = fileIo.openSync(this.indexPath, fileIo.OpenMode.READ_ONLY)
      try {
        const stat = fileIo.statSync(this.indexPath)
        const buf = new ArrayBuffer(stat.size)
        fileIo.readSync(file.fd, buf)
        const bytes = new Uint8Array(buf)
        let text = ''
        for (let i = 0; i < bytes.length; i++) {
          text += String.fromCharCode(bytes[i])
        }
        return JSON.parse(text) as StudyFolderRecord[]
      } finally {
        fileIo.closeSync(file)
      }
    } catch {
      return []
    }
  }

  private saveAll(folders: StudyFolderRecord[]): void {
    const file = fileIo.openSync(this.indexPath,
      fileIo.OpenMode.CREATE | fileIo.OpenMode.WRITE_ONLY | fileIo.OpenMode.TRUNC)
    try {
      fileIo.writeSync(file.fd, JSON.stringify(folders, null, 2))
    } finally {
      fileIo.closeSync(file)
    }
  }

  private makeUUID(): string {
    const chars = 'abcdef0123456789'
    let id = ''
    for (let i = 0; i < 32; i++) {
      id += chars.charAt(Math.floor(Math.random() * chars.length))
    }
    return id.substring(0, 8) + '-' + id.substring(8, 12) + '-' +
      id.substring(12, 16) + '-' + id.substring(16, 20) + '-' + id.substring(20, 32)
  }
}
