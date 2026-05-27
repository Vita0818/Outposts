/**
 * Simple HTTP recording upload client
 * Mirrors single-request path of RecordingUploadClient.swift
 * Uses @ohos.net.http for metadata + audio upload
 */
import { http } from '@kit.NetworkKit'
import { fileIo } from '@kit.CoreFileKit'
import { RecordingMetadata } from '../models/RecordingModels'
import { SettingsStore } from './SettingsStore'

const TAG = 'RokuricsUpload'

export enum UploadStage {
  IDLE = 'idle',
  UPLOADING_METADATA = 'uploadingMetadata',
  UPLOADING_AUDIO = 'uploadingAudio',
  COMPLETED = 'completed',
  FAILED = 'failed'
}

export interface UploadProgress {
  stage: UploadStage
  bytesSent: number
  totalBytes: number
  fraction: number
  description: string
}

export interface UploadResult {
  ok: boolean
  recordingID: string
  metadataFileName: string | null
  audioFileName: string | null
  metadataDisposition: string | null
  audioDisposition: string | null
  error: string | null
}

interface ServerResponse {
  ok: boolean
  message?: string
  disposition?: string
  fileName?: string
  recordingID?: string
  metadataFileName?: string
  audioFileName?: string
  receiveStatus?: string
  processingStatus?: string
  error?: string
  reason?: string
}

export interface UploadConfig {
  serverURL: string
  apiKey: string
  requestTimeout: number
}

export type UploadProgressHandler = (progress: UploadProgress) => void

interface StudyFilingPayload {
  type: string | null
  subject: string | null
  chapter: string | null
  topic: string | null
}

interface MetadataPayload {
  recordingID: string
  title: string
  fileName: string
  format: string
  codec: string
  sampleRate: number
  channels: number
  bitrate: number
  fileSize: number
  duration: number
  createdAt: string
  endedAt: string
  tags: string[]
  studyFiling?: StudyFilingPayload
}

export class RecordingUploadClient {
  private config: UploadConfig
  private progressHandler: UploadProgressHandler | null = null
  private _currentProgress: UploadProgress = {
    stage: UploadStage.IDLE, bytesSent: 0, totalBytes: 0, fraction: 0, description: ''
  }

  constructor(config: UploadConfig) {
    this.config = config
  }

  get currentProgress(): UploadProgress { return this._currentProgress }

  onProgress(handler: UploadProgressHandler): void {
    this.progressHandler = handler
  }

  private notifyProgress(stage: UploadStage, bytesSent: number, totalBytes: number, description: string): void {
    const fraction = totalBytes > 0 ? bytesSent / totalBytes : 0
    this._currentProgress = { stage, bytesSent, totalBytes, fraction, description }
    if (this.progressHandler) {
      this.progressHandler(this._currentProgress)
    }
  }

  async uploadRecording(metadata: RecordingMetadata, audioPath: string): Promise<UploadResult> {
    const baseURL = this.config.serverURL.replace(/\/+$/, '')
    const audioSize = await this.getFileSize(audioPath)
    const totalBytes = audioSize + 4096 // rough metadata overhead

    // Upload metadata
    this.notifyProgress(UploadStage.UPLOADING_METADATA, 0, totalBytes, '正在上传 metadata')
    const metadataResult = await this.postJSON(`${baseURL}/upload-recording-metadata`, this.buildMetadataPayload(metadata))

    if (!metadataResult.ok) {
      this.notifyProgress(UploadStage.FAILED, 0, totalBytes,
        metadataResult.error ?? 'metadata_upload_failed')
      return {
        ok: false,
        recordingID: metadata.id,
        metadataFileName: null,
        audioFileName: null,
        metadataDisposition: null,
        audioDisposition: null,
        error: metadataResult.error ?? 'metadata 上传失败'
      }
    }

    this.notifyProgress(UploadStage.UPLOADING_AUDIO, 2048, totalBytes, '正在上传音频')

    // Upload audio
    const audioResult = await this.uploadFile(
      `${baseURL}/upload-recording-audio`,
      audioPath,
      'audio/m4a',
      'recording-audio',
      metadata.id,
      metadata.fileName
    )

    if (!audioResult.ok) {
      this.notifyProgress(UploadStage.FAILED, 2048, totalBytes,
        audioResult.error ?? 'audio_upload_failed')
      return {
        ok: false,
        recordingID: metadata.id,
        metadataFileName: metadataResult.fileName ?? null,
        audioFileName: null,
        metadataDisposition: metadataResult.disposition ?? null,
        audioDisposition: null,
        error: audioResult.error ?? '音频上传失败'
      }
    }

    this.notifyProgress(UploadStage.COMPLETED, totalBytes, totalBytes, '上传完成')

    console.info(`[${TAG}] upload complete: ${metadata.id}`)
    return {
      ok: true,
      recordingID: audioResult.recordingID ?? metadata.id,
      metadataFileName: metadataResult.fileName ?? null,
      audioFileName: audioResult.fileName ?? null,
      metadataDisposition: metadataResult.disposition ?? null,
      audioDisposition: audioResult.disposition ?? null,
      error: null
    }
  }

  private async postJSON(url: string, payload: MetadataPayload): Promise<ServerResponse> {
    const httpRequest = http.createHttp()
    try {
      const response = await httpRequest.request(url, {
        method: http.RequestMethod.POST,
        header: {
          'Content-Type': 'application/json',
          'Authorization': this.config.apiKey ? `Bearer ${this.config.apiKey}` : '',
          'X-Upload-Type': 'recording-metadata'
        },
        extraData: JSON.stringify(payload, null, 2),
        expectDataType: http.HttpDataType.STRING,
        connectTimeout: this.config.requestTimeout,
        readTimeout: this.config.requestTimeout
      })

      if (response.responseCode === 200 || response.responseCode === 201) {
        try {
          return JSON.parse(response.result as string) as ServerResponse
        } catch {
          return { ok: true }
        }
      }
      return { ok: false, error: `HTTP ${response.responseCode}` }
    } catch (err) {
      return { ok: false, error: `网络错误: ${err}` }
    } finally {
      httpRequest.destroy()
    }
  }

  private async uploadFile(
    url: string,
    filePath: string,
    contentType: string,
    uploadType: string,
    recordingID: string,
    fileName: string
  ): Promise<ServerResponse> {
    const httpRequest = http.createHttp()
    try {
      // Read file into ArrayBuffer
      const stat = fileIo.statSync(filePath)
      const file = fileIo.openSync(filePath, fileIo.OpenMode.READ_ONLY)
      let fileData: ArrayBuffer
      try {
        fileData = new ArrayBuffer(stat.size)
        fileIo.readSync(file.fd, fileData)
      } finally {
        fileIo.closeSync(file)
      }

      const response = await httpRequest.request(url, {
        method: http.RequestMethod.POST,
        header: {
          'Content-Type': contentType,
          'Authorization': this.config.apiKey ? `Bearer ${this.config.apiKey}` : '',
          'X-Upload-Type': uploadType,
          'X-Recording-ID': recordingID,
          'X-File-Name': fileName,
          'Content-Length': String(stat.size)
        },
        extraData: fileData,
        expectDataType: http.HttpDataType.STRING,
        connectTimeout: this.config.requestTimeout,
        readTimeout: this.config.requestTimeout * 3
      })

      if (response.responseCode === 200 || response.responseCode === 201) {
        try {
          return JSON.parse(response.result as string) as ServerResponse
        } catch {
          return { ok: true }
        }
      }
      return { ok: false, error: `HTTP ${response.responseCode}` }
    } catch (err) {
      return { ok: false, error: `网络错误: ${err}` }
    } finally {
      httpRequest.destroy()
    }
  }

  private buildMetadataPayload(metadata: RecordingMetadata): MetadataPayload {
    const payload: MetadataPayload = {
      recordingID: metadata.id,
      title: metadata.title,
      fileName: metadata.fileName,
      format: metadata.format,
      codec: metadata.codec,
      sampleRate: metadata.sampleRate,
      channels: metadata.channels,
      bitrate: metadata.bitrate,
      fileSize: metadata.fileSize,
      duration: metadata.duration,
      createdAt: metadata.createdAt.toISOString(),
      endedAt: metadata.endedAt.toISOString(),
      tags: metadata.tags
    }
    if (metadata.studyFiling && !metadata.studyFiling.isEmpty) {
      payload.studyFiling = {
        type: metadata.studyFiling.type,
        subject: metadata.studyFiling.subject,
        chapter: metadata.studyFiling.chapter,
        topic: metadata.studyFiling.topic
      }
    }
    return payload
  }

  private async getFileSize(path: string): Promise<number> {
    try {
      const stat = fileIo.statSync(path)
      return stat.size
    } catch {
      return 0
    }
  }

  static async loadConfig(settingsStore: SettingsStore): Promise<UploadConfig> {
    const serverURL = await settingsStore.getString('upload.serverURL', '')
    const apiKey = await settingsStore.getString('upload.apiKey', '')
    const requestTimeout = await settingsStore.getNumber('upload.requestTimeout', 30)
    return {
      serverURL: serverURL || 'http://localhost:8000',
      apiKey: apiKey,
      requestTimeout: requestTimeout > 0 ? requestTimeout : 30
    }
  }

  static recoveringStaleUploadingStatus(metadata: RecordingMetadata): RecordingMetadata {
    if (metadata.uploadStatus !== 'uploading') return metadata
    return metadata.copyWithUploadStatus('failed')
  }
}
