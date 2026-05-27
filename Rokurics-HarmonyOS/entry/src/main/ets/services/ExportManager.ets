/**
 * Export service - mirrors ExportManager.swift
 * Supports Markdown export of recordings, transcripts, and notes
 */
import { fileIo } from '@kit.CoreFileKit'
import { RecordingMetadata } from '../models/RecordingModels'
import { TranscriptionResult, NoteGenerationResult } from '../models/ProcessingModels'
import { formatDuration, formatDate, fileSizeText } from '../utils/FormatHelpers'
import { TranscriptStore } from './TranscriptStore'

const TAG = 'RokuricsExport'

export type ExportFormat = 'Markdown' | 'JSON'

export class ExportManager {
  private baseDir: string
  private transcriptStore: TranscriptStore

  constructor(context: Context) {
    this.baseDir = context.filesDir + '/Rokurics/Exports'
    this.transcriptStore = new TranscriptStore(context)
    this.ensureDirectories()
  }

  private ensureDirectories(): void {
    try {
      if (!fileIo.accessSync(this.baseDir)) {
        fileIo.mkdirSync(this.baseDir, true)
      }
    } catch (_e) { /* ignore */ }
  }

  getExportsDir(): string {
    return this.baseDir
  }

  getTranscriptStore(): TranscriptStore {
    return this.transcriptStore
  }

  async exportRecordingInfo(
    recording: RecordingMetadata,
    format: ExportFormat
  ): Promise<string> {
    const safeName = this.sanitizeFileName(recording.title)
    const outputPath = `${this.baseDir}/${safeName}.${format === 'JSON' ? 'json' : 'md'}`

    if (format === 'JSON') {
      const payload: Record<string, Object> = {
        'id': recording.id,
        'title': recording.title,
        'fileName': recording.fileName,
        'format': recording.format,
        'codec': recording.codec,
        'sampleRate': recording.sampleRate,
        'channels': recording.channels,
        'bitrate': recording.bitrate,
        'fileSize': recording.fileSize,
        'duration': recording.duration,
        'createdAt': recording.createdAt.toISOString(),
        'endedAt': recording.endedAt.toISOString(),
        'uploadStatus': recording.uploadStatus,
        'transcriptionStatus': recording.transcriptionStatus,
        'noteStatus': recording.noteStatus,
        'tags': recording.tags,
        'isDeleted': recording.isDeleted
      }
      if (recording.studyFiling) {
        const filingObj: Record<string, Object> = {}
        filingObj['type'] = recording.studyFiling.type ?? ''
        filingObj['subject'] = recording.studyFiling.subject ?? ''
        filingObj['chapter'] = recording.studyFiling.chapter ?? ''
        filingObj['topic'] = recording.studyFiling.topic ?? ''
        payload['studyFiling'] = filingObj
      }
      this.writeTextFile(outputPath, JSON.stringify(payload, null, 2))
    } else {
      let md = `# ${recording.title}\n\n`
      md += `- **日期**: ${formatDate(recording.createdAt)}\n`
      md += `- **时长**: ${formatDuration(recording.duration)}\n`
      md += `- **格式**: ${recording.format.toUpperCase()} / ${recording.codec}\n`
      md += `- **采样率**: ${recording.sampleRate} Hz / ${recording.channels === 1 ? '单声道' : '立体声'}\n`
      md += `- **文件大小**: ${fileSizeText(recording.fileSize)}\n`
      md += `- **比特率**: ${(recording.bitrate / 1000).toFixed(0)} kbps\n`
      md += `- **上传状态**: ${this.uploadLabel(recording.uploadStatus)}\n`

      if (recording.studyFiling && !recording.studyFiling.isEmpty) {
        md += `- **学习归档**: ${recording.studyFiling.displaySummary}\n`
      }

      if (recording.tags.length > 0) {
        md += `- **标签**: ${recording.tags.join(', ')}\n`
      }

      md += `\n---\n*由 Rokurics 导出*\n`
      this.writeTextFile(outputPath, md)
    }

    console.info(`[${TAG}] exported recording info: ${outputPath}`)
    return outputPath
  }

  async exportTranscriptAsMarkdown(
    transcript: TranscriptionResult,
    recording: RecordingMetadata
  ): Promise<string> {
    const safeName = this.sanitizeFileName(recording.title)
    const outputPath = `${this.baseDir}/${safeName}_transcript.md`

    let md = `# ${recording.title} - 转写文本\n\n`
    md += `- Provider: ${transcript.providerName}\n`
    md += `- Model: ${transcript.modelName ?? 'unknown'}\n`
    md += `- Language: ${transcript.language ?? 'auto'}\n`
    md += `- Duration: ${formatDuration(recording.duration)}\n\n`
    md += `## 完整文本\n\n${transcript.text}\n`

    if (transcript.segments.length > 0) {
      md += `\n## 分段\n\n`
      for (const seg of transcript.segments) {
        const tc = `${String(Math.floor(seg.startTime / 60)).padStart(2, '0')}:${String(Math.floor(seg.startTime % 60)).padStart(2, '0')}`
        md += `- **[${tc}]** ${seg.text}\n`
      }
    }

    md += `\n---\n*由 Rokurics 导出*\n`
    this.writeTextFile(outputPath, md)

    console.info(`[${TAG}] exported transcript: ${outputPath}`)
    return outputPath
  }

  async exportNoteAsMarkdown(
    note: NoteGenerationResult,
    recording: RecordingMetadata
  ): Promise<string> {
    const safeName = this.sanitizeFileName(recording.title)
    const outputPath = `${this.baseDir}/${safeName}_note.md`

    const header = `# ${recording.title} - AI 笔记\n\n- Provider: ${note.providerName}\n- Model: ${note.modelName ?? 'unknown'}\n- Generated: ${note.completedAt.toISOString()}\n\n---\n\n`
    this.writeTextFile(outputPath, header + note.markdown + `\n\n---\n*由 Rokurics 导出*\n`)

    console.info(`[${TAG}] exported note: ${outputPath}`)
    return outputPath
  }

  async exportAllForRecording(
    recording: RecordingMetadata,
    transcript: TranscriptionResult | null,
    note: NoteGenerationResult | null
  ): Promise<string[]> {
    const paths: string[] = []
    paths.push(await this.exportRecordingInfo(recording, 'Markdown'))
    if (transcript) {
      paths.push(await this.exportTranscriptAsMarkdown(transcript, recording))
    }
    if (note) {
      paths.push(await this.exportNoteAsMarkdown(note, recording))
    }
    return paths
  }

  async listExports(): Promise<string[]> {
    try {
      return fileIo.listFileSync(this.baseDir).filter(f => f.endsWith('.md') || f.endsWith('.json'))
    } catch {
      return []
    }
  }

  async deleteExport(filename: string): Promise<void> {
    const path = `${this.baseDir}/${filename}`
    try {
      if (fileIo.accessSync(path)) {
        fileIo.unlinkSync(path)
      }
    } catch { /* ignore */ }
  }

  private writeTextFile(path: string, content: string): void {
    const file: fileIo.File = fileIo.openSync(path, fileIo.OpenMode.CREATE | fileIo.OpenMode.WRITE_ONLY | fileIo.OpenMode.TRUNC)
    try {
      fileIo.writeSync(file.fd, content)
    } finally {
      fileIo.closeSync(file)
    }
  }

  private sanitizeFileName(name: string): string {
    let result = ''
    for (let i = 0; i < name.length; i++) {
      const c = name.charAt(i)
      if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c === '_' || c === '-' || c >= '一') {
        result += c
      } else if (c === ' ' || c === '/') {
        result += '_'
      }
    }
    return result.substring(0, 80) || 'export'
  }

  private uploadLabel(s: string): string {
    if (s === 'localOnly') return '仅本地'
    if (s === 'uploading') return '上传中'
    if (s === 'uploaded') return '已上传'
    if (s === 'failed') return '失败'
    return s
  }
}
