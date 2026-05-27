/**
 * Transcript persistence service - mirrors TranscriptStore.swift
 * Saves transcription results as JSON and Markdown to filesystem
 */
import { fileIo } from '@kit.CoreFileKit'
import { TranscriptionResult, TranscriptionSegment, NoteGenerationResult } from '../models/ProcessingModels'

const TAG = 'RokuricsTranscript'

function writeTextFile(path: string, content: string): void {
  const file: fileIo.File = fileIo.openSync(path, fileIo.OpenMode.CREATE | fileIo.OpenMode.WRITE_ONLY | fileIo.OpenMode.TRUNC)
  try {
    fileIo.writeSync(file.fd, content)
  } finally {
    fileIo.closeSync(file)
  }
}

function sanitizePathComponent(value: string): string {
  let result = ''
  for (let i = 0; i < value.length; i++) {
    const c = value.charAt(i)
    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c === '_' || c === '-') {
      result += c
    } else {
      result += '_'
    }
  }
  return result.replace(/_+/g, '_').replace(/^[_.-]+|[_.-]+$/g, '')
}

function formatDateKey(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function timecode(seconds: number): string {
  const total = Math.max(0, Math.floor(seconds))
  const mins = Math.floor(total / 60)
  const secs = total % 60
  return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
}

export interface TranscriptStoreSaveResult {
  transcriptRelativePath: string
  transcriptMarkdownRelativePath: string
  outputDirectory: string
}

export class TranscriptStore {
  private baseDir: string

  constructor(context: Context) {
    this.baseDir = context.filesDir + '/Rokurics/Transcripts'
    this.ensureDirectories()
  }

  private ensureDirectories(): void {
    try {
      if (!fileIo.accessSync(this.baseDir)) {
        fileIo.mkdirSync(this.baseDir, true)
      }
    } catch (_e) { /* ignore */ }
  }

  outputDirectory(recordingID: string, createdAt: Date): string {
    const safeID = sanitizePathComponent(recordingID)
    const day = formatDateKey(createdAt)
    return `${this.baseDir}/${day}/${safeID}`
  }

  private isInsideTranscriptsDir(path: string): boolean {
    const base = this.baseDir.endsWith('/') ? this.baseDir : this.baseDir + '/'
    return path === this.baseDir || path.startsWith(base)
  }

  save(
    result: TranscriptionResult,
    recordingTitle: string,
    recordingID: string,
    createdAt: Date
  ): TranscriptStoreSaveResult {
    const outputDir = this.outputDirectory(recordingID, createdAt)
    const transcriptPath = `${outputDir}/transcript.json`
    const markdownPath = `${outputDir}/transcript.md`

    try {
      if (!fileIo.accessSync(outputDir)) {
        fileIo.mkdirSync(outputDir, true)
      }

      // Build segments array
      const segArray: Record<string, Object>[] = []
      for (const s of result.segments) {
        const segObj: Record<string, Object> = {
          'id': s.id,
          'startTime': s.startTime,
          'endTime': s.endTime,
          'text': s.text
        }
        if (s.confidence !== null) {
          segObj['confidence'] = s.confidence
        }
        segArray.push(segObj)
      }

      // Save JSON
      const jsonPayload: Record<string, Object> = {
        'taskID': result.taskID,
        'recordingID': result.recordingID,
        'providerID': result.providerID,
        'providerName': result.providerName,
        'modelName': result.modelName ?? '',
        'language': result.language ?? '',
        'text': result.text,
        'status': result.status,
        'startedAt': result.startedAt.toISOString(),
        'completedAt': result.completedAt.toISOString(),
        'segments': segArray
      }
      writeTextFile(transcriptPath, JSON.stringify(jsonPayload))

      // Save Markdown
      const title = recordingTitle.trim() || '未命名录音'
      const lang = result.language || 'auto'
      const segmentLines = result.segments.map((s: TranscriptionSegment) =>
        `- [${timecode(s.startTime)} - ${timecode(s.endTime)}] ${s.text}`
      ).join('\n')

      const markdown = `# ${title}

- Provider: ${result.providerName}
- Model: ${result.modelName ?? 'unknown'}
- Language: ${lang}

## Transcript

${result.text}

## Segments

${segmentLines}
`
      writeTextFile(markdownPath, markdown)

      console.info(`[${TAG}] transcript saved: ${outputDir}`)

      return {
        transcriptRelativePath: transcriptPath.substring(this.baseDir.length + 1),
        transcriptMarkdownRelativePath: markdownPath.substring(this.baseDir.length + 1),
        outputDirectory: outputDir
      }
    } catch (err) {
      throw new Error(`Failed to save transcript: ${err}`)
    }
  }

  saveNote(
    result: NoteGenerationResult,
    recordingTitle: string,
    recordingID: string,
    createdAt: Date
  ): string {
    const outputDir = this.outputDirectory(recordingID, createdAt)
    const notePath = `${outputDir}/note.md`

    try {
      if (!fileIo.accessSync(outputDir)) {
        fileIo.mkdirSync(outputDir, true)
      }

      const header = `# ${recordingTitle || '未命名录音'} - 笔记\n\n- Provider: ${result.providerName}\n- Model: ${result.modelName ?? 'unknown'}\n- Generated: ${result.completedAt.toISOString()}\n\n---\n\n`
      writeTextFile(notePath, header + result.markdown)

      console.info(`[${TAG}] note saved: ${notePath}`)
      return notePath
    } catch (err) {
      throw new Error(`Failed to save note: ${err}`)
    }
  }

  async loadTranscript(recordingID: string, createdAt: Date): Promise<TranscriptionResult | null> {
    const outputDir = this.outputDirectory(recordingID, createdAt)
    const transcriptPath = `${outputDir}/transcript.json`
    try {
      if (!fileIo.accessSync(transcriptPath)) return null
      const file: fileIo.File = fileIo.openSync(transcriptPath, fileIo.OpenMode.READ_ONLY)
      try {
        const stat: fileIo.Stat = fileIo.statSync(transcriptPath)
        const buf: ArrayBuffer = new ArrayBuffer(stat.size)
        fileIo.readSync(file.fd, buf)
        const bytes: Uint8Array = new Uint8Array(buf)
        let text = ''
        for (let i = 0; i < bytes.length; i++) {
          text += String.fromCharCode(bytes[i])
        }
        const json: Record<string, Object> = JSON.parse(text) as Record<string, Object>
        const segs: TranscriptionSegment[] = ((json['segments'] as Array<Record<string, Object>>) ?? []).map(
          (s: Record<string, Object>) => {
            const seg = new TranscriptionSegment(
              (s['id'] as string) ?? '',
              (s['startTime'] as number) ?? 0,
              (s['endTime'] as number) ?? 0,
              (s['text'] as string) ?? ''
            )
            seg.confidence = (s['confidence'] as number) ?? null
            return seg
          }
        )
        return TranscriptionResult.create({
          taskID: (json['taskID'] as string) ?? '',
          recordingID: (json['recordingID'] as string) ?? '',
          providerID: (json['providerID'] as string) ?? '',
          providerName: (json['providerName'] as string) ?? '',
          text: (json['text'] as string) ?? '',
          modelName: (json['modelName'] as string) ?? null,
          language: (json['language'] as string) ?? null,
          segments: segs,
          status: (json['status'] as string) ?? 'transcribed'
        })
      } finally {
        fileIo.closeSync(file)
      }
    } catch {
      return null
    }
  }

  async loadTranscriptMarkdown(recordingID: string, createdAt: Date): Promise<string | null> {
    const outputDir = this.outputDirectory(recordingID, createdAt)
    const markdownPath = `${outputDir}/transcript.md`
    try {
      if (!fileIo.accessSync(markdownPath)) return null
      const file: fileIo.File = fileIo.openSync(markdownPath, fileIo.OpenMode.READ_ONLY)
      try {
        const stat: fileIo.Stat = fileIo.statSync(markdownPath)
        const buf: ArrayBuffer = new ArrayBuffer(stat.size)
        fileIo.readSync(file.fd, buf)
        const bytes: Uint8Array = new Uint8Array(buf)
        let text = ''
        for (let i = 0; i < bytes.length; i++) {
          text += String.fromCharCode(bytes[i])
        }
        return text
      } finally {
        fileIo.closeSync(file)
      }
    } catch {
      return null
    }
  }

  async loadNoteMarkdown(recordingID: string, createdAt: Date): Promise<string | null> {
    const outputDir = this.outputDirectory(recordingID, createdAt)
    const notePath = `${outputDir}/note.md`
    try {
      if (!fileIo.accessSync(notePath)) return null
      const file: fileIo.File = fileIo.openSync(notePath, fileIo.OpenMode.READ_ONLY)
      try {
        const stat: fileIo.Stat = fileIo.statSync(notePath)
        const buf: ArrayBuffer = new ArrayBuffer(stat.size)
        fileIo.readSync(file.fd, buf)
        const bytes: Uint8Array = new Uint8Array(buf)
        let text = ''
        for (let i = 0; i < bytes.length; i++) {
          text += String.fromCharCode(bytes[i])
        }
        return text
      } finally {
        fileIo.closeSync(file)
      }
    } catch {
      return null
    }
  }
}
