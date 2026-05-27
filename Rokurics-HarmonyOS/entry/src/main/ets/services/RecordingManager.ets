/**
 * Audio recording service - mirrors RecordingManager.swift
 * Uses @ohos.multimedia.media AVRecorder for AAC/M4A encoding
 */
import { media } from '@kit.MediaKit'
import { fileIo } from '@kit.CoreFileKit'
import { audio } from '@kit.AudioKit'
import { RecordingMetadata, RecordingMetadataFields, RecordingState, StudyFilingPath, RecordingUploadStatus } from '../models/RecordingModels'
import { AudioFileStore } from '../services/AudioFileStore'

const TAG = 'RokuricsRecording'

interface AudioCapabilityResult {
  available: boolean
  message: string
}

let sharedInstance: RecordingManager | null = null

export function getSharedRecordingManager(context?: Context): RecordingManager {
  if (!sharedInstance && context) {
    sharedInstance = new RecordingManager(context)
  }
  if (!sharedInstance) {
    throw new Error('RecordingManager not initialized. Call with context first.')
  }
  return sharedInstance
}

interface PendingRecordingSave {
  filePath: string
  createdAt: Date
  endedAt: Date
  duration: number
  defaultTitle: string
}

export class RecordingManager {
  private fileStore: AudioFileStore
  private avRecorder: media.AVRecorder | null = null
  private _state: RecordingState = RecordingState.IDLE
  private _elapsedSeconds: number = 0
  private _statusMessage: string = '录音默认仅保存在本地'
  private _lastError: string | null = null
  private activeRecordingPath: string = ''
  private activeFileFd: number = -1
  private recordingStartedAt: Date | null = null
  private elapsedTimer: number | null = null
  private _recordings: RecordingMetadata[] = []
  private _trashedRecordings: RecordingMetadata[] = []
  private _latestMetadata: RecordingMetadata | null = null
  private _pendingTitle: string | null = null
  private _pendingDefaultTitle: string | null = null
  private _debugMessage: string | null = null
  private pendingSave: PendingRecordingSave | null = null
  private stateListeners: ((state: RecordingState) => void)[] = []

  get state(): RecordingState { return this._state }
  get elapsedSeconds(): number { return this._elapsedSeconds }
  get statusMessage(): string { return this._statusMessage }
  get lastError(): string | null { return this._lastError }
  get recordings(): RecordingMetadata[] { return this._recordings }
  get trashedRecordings(): RecordingMetadata[] { return this._trashedRecordings }
  get latestMetadata(): RecordingMetadata | null { return this._latestMetadata }
  get pendingTitle(): string | null { return this._pendingTitle }
  get pendingDefaultTitle(): string | null { return this._pendingDefaultTitle }
  get debugMessage(): string | null { return this._debugMessage }

  constructor(context: Context) {
    this.fileStore = new AudioFileStore(context)
    this.loadExistingRecordings()
  }

  onStateChange(listener: (state: RecordingState) => void): void {
    this.stateListeners.push(listener)
  }

  private setState(state: RecordingState): void {
    this._state = state
    for (const listener of this.stateListeners) {
      listener(state)
    }
  }

  private setStatus(msg: string): void {
    this._statusMessage = msg
    this.log(msg)
  }

  private log(msg: string): void {
    this._debugMessage = msg
    console.info(`[${TAG}] ${msg}`)
  }

  get fileStoreInstance(): AudioFileStore {
    return this.fileStore
  }

  async checkAudioCapability(): Promise<AudioCapabilityResult> {
    try {
      const audioManager = audio.getAudioManager()
      const audioDevices: audio.AudioDeviceDescriptors = await audioManager.getDevices(audio.DeviceFlag.INPUT_DEVICES_FLAG)
      const hasMicrophone: boolean = Array.isArray(audioDevices) && audioDevices.length > 0
      if (!hasMicrophone) {
        const result: AudioCapabilityResult = { available: false, message: '未检测到麦克风设备，录音需要设备验证' }
        return result
      }
      const result: AudioCapabilityResult = { available: true, message: '音频设备就绪' }
      return result
    } catch (err) {
      const msg: string = `音频检查失败（需设备验证）: ${err}`
      const result: AudioCapabilityResult = { available: false, message: msg }
      return result
    }
  }

  toggleRecording(): void {
    switch (this._state) {
      case RecordingState.RECORDING:
        this.stopRecording()
        break
      case RecordingState.PAUSED:
        this.resumeRecording()
        break
      case RecordingState.IDLE:
      case RecordingState.SAVED:
      case RecordingState.PERMISSION_DENIED:
      case RecordingState.FAILED:
        this.startRecording()
        break
      default:
        this.log(`button ignored while busy. state=${this._state}`)
    }
  }

  async startRecording(): Promise<void> {
    if (this._state === RecordingState.REQUESTING_PERMISSION ||
        this._state === RecordingState.CONFIGURING_SESSION ||
        this._state === RecordingState.RECORDING ||
        this._state === RecordingState.PAUSED ||
        this._state === RecordingState.STOPPING ||
        this._state === RecordingState.FILING ||
        this._state === RecordingState.SAVING) {
      this.log(`start ignored. state=${this._state}`)
      return
    }

    this._lastError = null
    this._elapsedSeconds = 0
    this.setState(RecordingState.REQUESTING_PERMISSION)
    this.setStatus('正在请求麦克风权限')

    try {
      this.setState(RecordingState.CONFIGURING_SESSION)
      this.setStatus('正在配置录音')

      await this.fileStore.ensureStorageDirectories()

      const filePath = this.fileStore.makeRecordingFileName(new Date(), false)
      this.activeRecordingPath = filePath

      // Open file for AVRecorder to write into
      const file: fileIo.File = fileIo.openSync(filePath, fileIo.OpenMode.CREATE | fileIo.OpenMode.WRITE_ONLY)
      this.activeFileFd = file.fd

      const avRecorderConfig: media.AVRecorderConfig = {
        audioSourceType: media.AudioSourceType.AUDIO_SOURCE_TYPE_MIC,
        profile: {
          audioBitrate: 64000,
          audioChannels: 1,
          audioCodec: media.CodecMimeType.AUDIO_AAC,
          audioSampleRate: 16000,
          fileFormat: media.ContainerFormatType.CFT_MPEG_4
        },
        url: `fd://${file.fd}`
      }

      this.avRecorder = await media.createAVRecorder()
      await this.avRecorder.prepare(avRecorderConfig)
      await this.avRecorder.start()

      this.recordingStartedAt = new Date()
      this._elapsedSeconds = 0
      this._lastError = null
      this.setState(RecordingState.RECORDING)
      this.setStatus('正在录音')
      this.startElapsedTimer()

      const defaultTitle = RecordingMetadata.defaultTitle(new Date())
      this._pendingDefaultTitle = defaultTitle
      this.log('recording started with primary settings (AAC 16kHz mono 64kbps)')
    } catch (err) {
      // Cleanup on failure
      if (this.avRecorder) {
        try { this.avRecorder.release() } catch (_e) { /* ignore */ }
        this.avRecorder = null
      }
      if (this.activeFileFd >= 0) {
        try { fileIo.closeSync(this.activeFileFd) } catch (_e) { /* ignore */ }
        this.activeFileFd = -1
      }
      this.setState(RecordingState.FAILED)
      const msg = `录音启动失败：${err}`
      this._lastError = msg
      this.setStatus(msg)
      console.error(`[${TAG}][ERROR] ${msg}`)
    }
  }

  async pauseRecording(): Promise<void> {
    if (this._state !== RecordingState.RECORDING || !this.avRecorder) {
      this.log(`pause ignored. state=${this._state}`)
      return
    }
    try {
      await this.avRecorder.pause()
    } catch (_e) { /* ignore */ }
    this.stopElapsedTimer()
    this.setState(RecordingState.PAUSED)
    this.setStatus('已暂停')
    this.log('recording paused')
  }

  async resumeRecording(): Promise<void> {
    if (this._state !== RecordingState.PAUSED || !this.avRecorder) {
      this.log(`resume ignored. state=${this._state}`)
      return
    }
    try {
      await this.avRecorder.resume()
    } catch (_e) { /* ignore */ }
    this.setState(RecordingState.RECORDING)
    this.setStatus('正在录音')
    this.startElapsedTimer()
    this.log('recording resumed')
  }

  async stopRecording(): Promise<void> {
    if (this._state !== RecordingState.RECORDING && this._state !== RecordingState.PAUSED) {
      this.log(`stop ignored. state=${this._state}`)
      return
    }

    this.setState(RecordingState.STOPPING)
    this.setStatus('正在停止录音')
    this.stopElapsedTimer()

    const finalDuration = this._elapsedSeconds
    const createdAt = this.recordingStartedAt ?? new Date(Date.now() - finalDuration * 1000)
    const endedAt = new Date()

    try {
      if (this.avRecorder) {
        await this.avRecorder.stop()
        await this.avRecorder.release()
        this.avRecorder = null
      }
    } catch (err) {
      this.log(`avRecorder stop error: ${err}`)
    }

    // Close the file descriptor
    if (this.activeFileFd >= 0) {
      try { fileIo.closeSync(this.activeFileFd) } catch (_e) { /* ignore */ }
      this.activeFileFd = -1
    }

    this._elapsedSeconds = finalDuration
    this._lastError = null
    const defaultTitle = RecordingMetadata.defaultTitle(createdAt)
    this._pendingDefaultTitle = defaultTitle

    this.pendingSave = {
      filePath: this.activeRecordingPath,
      createdAt: createdAt,
      endedAt: endedAt,
      duration: finalDuration,
      defaultTitle: defaultTitle
    }

    this.setState(RecordingState.FILING)
    this.setStatus('等待归档')
    this.log(`recording stopped for filing: ${this.activeRecordingPath}`)
  }

  async finalizeRecording(title?: string, studyFiling?: StudyFilingPath | null, directSave: boolean = false): Promise<void> {
    if (!this.pendingSave) {
      this.log(`finalize ignored. no pending save. state=${this._state}`)
      return
    }

    this.setState(RecordingState.SAVING)
    this.setStatus('正在保存录音')

    const resolvedFiling = (studyFiling && !studyFiling.isEmpty) ? studyFiling : null
    const resolvedTitle = directSave
      ? this.pendingSave.defaultTitle
      : (title ?? this._pendingTitle ?? this._pendingDefaultTitle ?? this.pendingSave.defaultTitle)

    const ps = this.pendingSave
    const now = new Date()
    const fileName = ps.filePath.substring(ps.filePath.lastIndexOf('/') + 1)
    const relativeAudioPath = this.fileStore.relativePath(ps.filePath)
    const metadataPath = this.fileStore.makeMetadataPath(fileName.replace('.m4a', ''))
    const relativeMetadataPath = this.fileStore.relativePath(metadataPath)

    // Get actual file size
    const fileSize = await this.fileStore.fileSize(ps.filePath)

    const metadata = RecordingMetadata.create({
      id: fileName.replace('.m4a', '').replace(/^.*\//, ''),
      title: resolvedTitle,
      fileName: fileName,
      relativeAudioPath: relativeAudioPath,
      relativeMetadataPath: relativeMetadataPath,
      createdAt: ps.createdAt,
      endedAt: ps.endedAt,
      duration: ps.duration,
      format: 'm4a',
      codec: 'AAC',
      sampleRate: 16000,
      channels: 1,
      bitrate: 64000,
      fileSize: fileSize,
      uploadStatus: RecordingUploadStatus.LOCAL_ONLY,
      transcriptionStatus: 'notStarted',
      noteStatus: 'notStarted',
      tags: [],
      studyFiling: resolvedFiling
    })

    try {
      this.fileStore.saveMetadata(metadata)
      this._recordings = [metadata, ...this._recordings.filter(r => r.id !== metadata.id)]
      this._latestMetadata = metadata
      this._pendingDefaultTitle = null
      this._pendingTitle = null
      this.pendingSave = null
      this.setState(RecordingState.SAVED)
      this.setStatus(`已保存：${resolvedTitle}`)
      this.log(`saved recording: ${ps.filePath} (${fileSize} bytes)`)
    } catch (err) {
      this._lastError = `录音已保存，但 metadata 写入失败：${err}`
      this.setState(RecordingState.FAILED)
      this.setStatus(this._lastError!)
    }
  }

  async finalizeRecordingDirectSave(): Promise<void> {
    await this.finalizeRecording(undefined, null, true)
  }

  updatePendingTitle(rawTitle: string): void {
    const trimmed = rawTitle.trim()
    if (trimmed.length === 0) return
    this._pendingTitle = trimmed
  }

  async reloadRecordings(): Promise<void> {
    await this.loadExistingRecordings()
  }

  async renameRecording(recordingID: string, rawTitle: string): Promise<void> {
    try {
      const updated = await this.fileStore.updateTitle(recordingID, rawTitle)
      const idx = this._recordings.findIndex(r => r.id === recordingID)
      if (idx >= 0) {
        this._recordings[idx] = updated
        this._recordings = [...this._recordings]
      }
      if (this._latestMetadata?.id === recordingID) {
        this._latestMetadata = updated
      }
      this._lastError = null
    } catch {
      this._lastError = '重命名失败'
      throw new Error('重命名失败')
    }
  }

  async deleteRecording(recordingID: string): Promise<void> {
    try {
      const idx = this._recordings.findIndex(r => r.id === recordingID)
      if (idx < 0) return
      const metadata = this._recordings[idx]
      const trashed = await this.fileStore.moveToTrash(metadata)
      this._recordings.splice(idx, 1)
      this._recordings = [...this._recordings]
      this._trashedRecordings = [trashed, ...this._trashedRecordings]
      this._lastError = null
    } catch (err) {
      this._lastError = '删除失败'
      throw new Error('Operation failed')
    }
  }

  async restoreRecording(recordingID: string): Promise<void> {
    try {
      const restored = await this.fileStore.restoreRecording(recordingID)
      this._trashedRecordings = this._trashedRecordings.filter(r => r.id !== recordingID)
      this._recordings = [restored, ...this._recordings.filter(r => r.id !== recordingID)]
      this._latestMetadata = this._recordings.length > 0 ? this._recordings[0] : null
      this._lastError = null
    } catch (err) {
      this._lastError = '恢复失败'
      throw new Error('Operation failed')
    }
  }

  async permanentlyDelete(recordingID: string): Promise<void> {
    try {
      await this.fileStore.permanentlyDelete(recordingID)
      this._recordings = this._recordings.filter(r => r.id !== recordingID)
      this._trashedRecordings = this._trashedRecordings.filter(r => r.id !== recordingID)
      this._lastError = null
    } catch (err) {
      this._lastError = '删除失败'
      throw new Error('Operation failed')
    }
  }

  async updateStudyFiling(recordingID: string, studyFiling: StudyFilingPath): Promise<void> {
    const idx = this._recordings.findIndex(r => r.id === recordingID)
    if (idx < 0) return
    const updated = this._recordings[idx].copyWithStudyFiling(studyFiling)
    await this.fileStore.saveMetadata(updated)
    this._recordings[idx] = updated
    this._recordings = [...this._recordings]
  }

  get suggestedTitle(): string {
    return this._pendingDefaultTitle ?? RecordingMetadata.defaultTitle(this.recordingStartedAt ?? new Date())
  }

  get isRecording(): boolean { return this._state === RecordingState.RECORDING }
  get isPaused(): boolean { return this._state === RecordingState.PAUSED }

  getAudioAbsolutePath(metadata: RecordingMetadata): string {
    return this.fileStore.baseDir + '/' + metadata.relativeAudioPath
  }

  async getAudioFileSize(metadata: RecordingMetadata): Promise<number> {
    const absPath = this.getAudioAbsolutePath(metadata)
    return await this.fileStore.fileSize(absPath)
  }

  private async loadExistingRecordings(): Promise<void> {
    try {
      await this.fileStore.ensureStorageDirectories()
      const loaded = await this.fileStore.loadAllMetadata(false)
      const allMeta = await this.fileStore.loadAllMetadata(true)
      this._recordings = loaded
      this._trashedRecordings = allMeta.filter(r => r.isDeleted)
      this._latestMetadata = loaded.length > 0 ? loaded[0] : null
      if (this._latestMetadata) {
        this._elapsedSeconds = this._latestMetadata.duration
        this.setStatus(`最近录音：${this._latestMetadata.title}`)
      }
    } catch (err) {
      this._lastError = `读取本地录音失败：${err}`
      this.setStatus('读取本地录音失败')
    }
  }

  private startElapsedTimer(): void {
    this.stopElapsedTimer()
    const timerId: number = setInterval(() => {
      if (this._state === RecordingState.RECORDING) {
        this._elapsedSeconds += 0.25
      }
    }, 250)
    this.elapsedTimer = timerId
  }

  private stopElapsedTimer(): void {
    if (this.elapsedTimer !== null) {
      clearInterval(this.elapsedTimer)
      this.elapsedTimer = null
    }
  }

  static formatClock(seconds: number): string {
    const total = Math.max(0, Math.floor(seconds))
    const mins = Math.floor(total / 60)
    const secs = total % 60
    return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
  }

  static formatDuration(seconds: number): string {
    if (seconds < 60) return `${Math.max(0, Math.floor(seconds))} sec`
    return `${(seconds / 60).toFixed(1)} min`
  }
}
