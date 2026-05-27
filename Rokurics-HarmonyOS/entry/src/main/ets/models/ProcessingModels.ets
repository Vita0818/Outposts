/**
 * Processing models - transcription and note generation
 */

export class TranscriptionResult {
  taskID: string = ''
  recordingID: string = ''
  providerID: string = ''
  providerName: string = ''
  modelName: string | null = null
  language: string | null = null
  text: string = ''
  segments: TranscriptionSegment[] = []
  startedAt: Date = new Date()
  completedAt: Date = new Date()
  status: string = 'transcribed'

  static create(fields: TranscriptionResultFields): TranscriptionResult {
    const r = new TranscriptionResult()
    r.taskID = fields.taskID
    r.recordingID = fields.recordingID
    r.providerID = fields.providerID
    r.providerName = fields.providerName
    r.text = fields.text
    r.modelName = fields.modelName ?? null
    r.language = fields.language ?? null
    r.segments = fields.segments ?? []
    r.startedAt = fields.startedAt ?? new Date()
    r.completedAt = fields.completedAt ?? new Date()
    r.status = fields.status ?? 'transcribed'
    return r
  }
}

export interface TranscriptionResultFields {
  taskID: string
  recordingID: string
  providerID: string
  providerName: string
  text: string
  segments?: TranscriptionSegment[]
  modelName?: string | null
  language?: string | null
  startedAt?: Date
  completedAt?: Date
  status?: string
}

export class TranscriptionSegment {
  id: string = ''
  startTime: number = 0
  endTime: number = 0
  text: string = ''
  confidence: number | null = null

  constructor(id: string, start: number, end: number, text: string) {
    this.id = id
    this.startTime = start
    this.endTime = end
    this.text = text
  }
}

export class NoteGenerationResult {
  taskID: string = ''
  recordingID: string = ''
  providerID: string = ''
  providerName: string = ''
  modelName: string | null = null
  markdown: string = ''
  startedAt: Date = new Date()
  completedAt: Date = new Date()
  status: string = 'generated'
  modelOutputWasTruncated: boolean = false
  transcriptInputWasTruncated: boolean = false

  static create(fields: NoteResultFields): NoteGenerationResult {
    const r = new NoteGenerationResult()
    r.taskID = fields.taskID
    r.recordingID = fields.recordingID
    r.providerID = fields.providerID
    r.providerName = fields.providerName
    r.markdown = fields.markdown
    r.modelName = fields.modelName ?? null
    r.startedAt = fields.startedAt ?? new Date()
    r.completedAt = fields.completedAt ?? new Date()
    r.status = fields.status ?? 'generated'
    r.modelOutputWasTruncated = fields.modelOutputWasTruncated ?? false
    r.transcriptInputWasTruncated = fields.transcriptInputWasTruncated ?? false
    return r
  }
}

export interface NoteResultFields {
  taskID: string
  recordingID: string
  providerID: string
  providerName: string
  markdown: string
  modelName?: string | null
  startedAt?: Date
  completedAt?: Date
  status?: string
  modelOutputWasTruncated?: boolean
  transcriptInputWasTruncated?: boolean
}

export enum ProcessingMode {
  SINGLE_PASS = 'singlePass',
  CHUNKED = 'chunked',
  SECTIONED = 'sectioned'
}
