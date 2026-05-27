package com.rokurics.app.service

import android.Manifest
import android.content.pm.PackageManager
import android.media.MediaRecorder
import android.os.Build
import androidx.core.content.ContextCompat
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.rokurics.app.RokuricsApp
import com.rokurics.app.data.AudioFileStore
import com.rokurics.app.domain.model.RecordingMetadata
import com.rokurics.app.domain.model.RecordingUploadStatus
import com.rokurics.app.domain.model.StudyFilingPath
import com.rokurics.app.domain.provider.*
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

enum class RokuricsRecordingState {
    IDLE, REQUESTING_PERMISSION, CONFIGURING_SESSION,
    RECORDING, PAUSED, STOPPING, FILING, SAVING, SAVED,
    PERMISSION_DENIED, NOTIFICATION_PERMISSION_DENIED, FAILED;

    val isRecording: Boolean get() = this == RECORDING
    val isPaused: Boolean get() = this == PAUSED
    val isBusy: Boolean get() = this == REQUESTING_PERMISSION || this == CONFIGURING_SESSION
            || this == STOPPING || this == FILING || this == SAVING
}

class RecordingManager : ViewModel() {
    private val fileStore = AudioFileStore()
    private val context = RokuricsApp.instance
    val uploadQueue: com.rokurics.app.data.UploadQueue by lazy {
        com.rokurics.app.data.UploadQueue()
    }

    private val _state = MutableStateFlow(RokuricsRecordingState.IDLE)
    val state: StateFlow<RokuricsRecordingState> = _state.asStateFlow()

    private val _elapsedSeconds = MutableStateFlow(0.0)
    val elapsedSeconds: StateFlow<Double> = _elapsedSeconds.asStateFlow()

    private val _amplitudeLevel = MutableStateFlow(0f)
    val amplitudeLevel: StateFlow<Float> = _amplitudeLevel.asStateFlow()

    private val _recordings = MutableStateFlow<List<RecordingMetadata>>(emptyList())
    val recordings: StateFlow<List<RecordingMetadata>> = _recordings.asStateFlow()

    private val _trashedRecordings = MutableStateFlow<List<RecordingMetadata>>(emptyList())
    val trashedRecordings: StateFlow<List<RecordingMetadata>> = _trashedRecordings.asStateFlow()

    private val _statusMessage = MutableStateFlow("录音默认仅保存在本地")
    val statusMessage: StateFlow<String> = _statusMessage.asStateFlow()

    private val _latestRecordingMetadata = MutableStateFlow<RecordingMetadata?>(null)
    val latestRecordingMetadata: StateFlow<RecordingMetadata?> = _latestRecordingMetadata.asStateFlow()

    private val _lastRecordingFile = MutableStateFlow<File?>(null)
    val lastRecordingFile: StateFlow<File?> = _lastRecordingFile.asStateFlow()

    var pendingTitle: String? = null
    var pendingDefaultTitle: String? = null
    var suggestedRecordingTitle: String = ""

    private var mediaRecorder: MediaRecorder? = null
    private var activeRecordingFile: File? = null
    private var recordingStartedAt: Date? = null
    private var timerJob: Job? = null
    private var activeSettingsLabel: String? = null

    init {
        loadExistingRecordings()
        recoverUploadState()
    }

    private fun recoverUploadState() {
        try {
            val ledgerStore = com.rokurics.app.data.UploadJobLedgerStore()
            val recovered = ledgerStore.recoverStaleInProgressJobs()
            if (recovered.isNotEmpty()) {
                ledgerStore.propagateToMetadata(recovered, this)
                loadExistingRecordings()
            }
            // Auto-retry eligible jobs
            val retryable = ledgerStore.getRetryableJobs()
            if (retryable.isNotEmpty()) {
                ledgerStore.propagateToMetadata(retryable, this)
                loadExistingRecordings()
                for (job in retryable) {
                    val metadata = _recordings.value.find { it.id == job.recordingID }
                        ?: fileStore.loadMetadata(job.recordingID) ?: continue
                    uploadQueue.enqueue(metadata) { updated ->
                        fileStore.updateMetadata(updated)
                        loadExistingRecordings()
                    }
                }
            }
        } catch (_: Exception) {
            // Recovery is best-effort; don't block startup
        }
    }

    fun toggleRecording() {
        when (_state.value) {
            RokuricsRecordingState.RECORDING -> stopRecording()
            RokuricsRecordingState.PAUSED -> resumeRecording()
            RokuricsRecordingState.IDLE, RokuricsRecordingState.SAVED,
            RokuricsRecordingState.PERMISSION_DENIED, RokuricsRecordingState.FAILED -> startRecording()
            else -> { /* busy, ignore */ }
        }
    }

    fun startRecording() {
        val currentState = _state.value
        if (currentState.isBusy || currentState == RokuricsRecordingState.RECORDING
            || currentState == RokuricsRecordingState.PAUSED) return

        _state.value = RokuricsRecordingState.REQUESTING_PERMISSION
        _elapsedSeconds.value = 0.0
        _statusMessage.value = "正在请求权限"

        viewModelScope.launch {
            // Android 13+: check notification permission first
            if (!hasNotificationPermission()) {
                _state.value = RokuricsRecordingState.NOTIFICATION_PERMISSION_DENIED
                _statusMessage.value = "通知权限未开启，录音需要前台通知"
                return@launch
            }
            if (!hasRecordPermission()) {
                _state.value = RokuricsRecordingState.PERMISSION_DENIED
                _statusMessage.value = "麦克风权限未开启"
                return@launch
            }
            startRecorder()
        }
    }

    fun pauseRecording() {
        if (_state.value != RokuricsRecordingState.RECORDING) return
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                mediaRecorder?.pause()
            }
            stopTimer()
            _state.value = RokuricsRecordingState.PAUSED
            _statusMessage.value = "已暂停"
        } catch (e: Exception) {
            _statusMessage.value = "暂停失败: ${e.message}"
        }
    }

    fun resumeRecording() {
        if (_state.value != RokuricsRecordingState.PAUSED) return
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                mediaRecorder?.resume()
            }
            _state.value = RokuricsRecordingState.RECORDING
            _statusMessage.value = "正在录音"
            startTimer()
        } catch (e: Exception) {
            _state.value = RokuricsRecordingState.FAILED
            _statusMessage.value = "继续录音失败"
        }
    }

    fun stopRecording() {
        if (_state.value != RokuricsRecordingState.RECORDING
            && _state.value != RokuricsRecordingState.PAUSED) return

        _state.value = RokuricsRecordingState.STOPPING
        _statusMessage.value = "正在停止录音"
        stopTimer()

        try {
            mediaRecorder?.apply {
                stop()
                release()
            }
            mediaRecorder = null
        } catch (e: Exception) {
            // recorder may already be stopped
        }

        val file = activeRecordingFile
        val createdAt = recordingStartedAt ?: Date()
        val endedAt = Date()
        val duration = _elapsedSeconds.value

        if (file != null && file.exists()) {
            val defaultTitle = RecordingMetadata.defaultTitle(createdAt)
            pendingDefaultTitle = defaultTitle
            suggestedRecordingTitle = defaultTitle
            _lastRecordingFile.value = file
            _statusMessage.value = "等待归档"
            _state.value = RokuricsRecordingState.FILING
        } else {
            _state.value = RokuricsRecordingState.FAILED
            _statusMessage.value = "录音文件不存在"
        }
    }

    fun finalizeRecording(
        title: String? = null,
        studyFiling: StudyFilingPath? = null,
        directSave: Boolean = false
    ) {
        val pendingFile = _lastRecordingFile.value
        if (pendingFile == null) {
            _state.value = RokuricsRecordingState.FAILED
            return
        }

        _state.value = RokuricsRecordingState.SAVING
        _statusMessage.value = "正在保存录音"

        val resolvedTitle = title ?: pendingTitle ?: suggestedRecordingTitle
        val createdAt = recordingStartedAt ?: Date()
        val endedAt = Date()
        val id = pendingFile.nameWithoutExtension

        val metadata = RecordingMetadata(
            id = id,
            title = resolvedTitle,
            fileName = pendingFile.name,
            relativeAudioPath = fileStore.relativePath(pendingFile),
            relativeMetadataPath = "Metadata/$id.json",
            createdAt = createdAt,
            endedAt = endedAt,
            duration = _elapsedSeconds.value,
            fileSize = pendingFile.length(),
            studyFiling = if (directSave) null else studyFiling
        )

        fileStore.saveMetadata(metadata)
        loadExistingRecordings()
        _latestRecordingMetadata.value = metadata

        activeRecordingFile = null
        recordingStartedAt = null
        pendingTitle = null
        pendingDefaultTitle = null

        _state.value = RokuricsRecordingState.SAVED
        _statusMessage.value = "已保存: $resolvedTitle"

        // Auto-enqueue for upload (safe: queue handles missing connection gracefully)
        enqueueUpload(id)
    }

    fun finalizeRecordingDirectSave() {
        finalizeRecording(directSave = true)
    }

    fun reloadRecordings() {
        loadExistingRecordings()
    }

    fun renameRecording(recordingID: String, rawTitle: String) {
        fileStore.updateTitle(recordingID, rawTitle)
        loadExistingRecordings()
    }

    fun deleteRecording(recordingID: String) {
        val metadata = _recordings.value.find { it.id == recordingID } ?: return
        fileStore.moveToTrash(metadata)
        loadExistingRecordings()
    }

    fun restoreRecording(recordingID: String) {
        fileStore.restoreRecording(recordingID)
        loadExistingRecordings()
    }

    fun permanentlyDeleteRecording(recordingID: String) {
        fileStore.permanentlyDeleteRecording(recordingID)
        loadExistingRecordings()
    }

    fun updateStudyFiling(recordingID: String, studyFiling: StudyFilingPath?) {
        val metadata = _recordings.value.find { it.id == recordingID }
            ?: fileStore.loadMetadata(recordingID) ?: return
        val updated = metadata.copy(studyFiling = studyFiling)
        fileStore.updateMetadata(updated)
        loadExistingRecordings()
    }

    fun updateUploadProgress(
        recordingID: String,
        uploadStatus: String,
        progressFraction: Double? = null,
        confirmedBytes: Long? = null,
        totalBytes: Long? = null,
        description: String? = null
    ) {
        val metadata = _recordings.value.find { it.id == recordingID }
            ?: fileStore.loadMetadata(recordingID) ?: return
        val updated = metadata.copy(
            uploadStatus = uploadStatus,
            uploadProgressFraction = progressFraction ?: metadata.uploadProgressFraction,
            uploadProgressConfirmedBytes = confirmedBytes ?: metadata.uploadProgressConfirmedBytes,
            uploadProgressTotalBytes = totalBytes ?: metadata.uploadProgressTotalBytes,
            uploadProgressDescription = description ?: metadata.uploadProgressDescription
        )
        fileStore.updateMetadata(updated)
        loadExistingRecordings()
    }

    fun enqueueUpload(recordingID: String) {
        val metadata = _recordings.value.find { it.id == recordingID }
            ?: fileStore.loadMetadata(recordingID) ?: return
        uploadQueue.enqueue(metadata) { updated ->
            fileStore.updateMetadata(updated)
            loadExistingRecordings()
        }
    }

    fun getAudioFilePath(recordingID: String): String? {
        val metadata = _recordings.value.find { it.id == recordingID }
            ?: fileStore.loadMetadata(recordingID) ?: return null
        val file = fileStore.audioFileFor(metadata)
        return if (file.exists()) file.absolutePath else null
    }

    // ── Local Transcription ──────────────────────────────────────────

    var localTranscriptionEngine: com.rokurics.app.domain.provider.LocalTranscriptionEngine? = null
    var transcriptionProvider: TranscriptionProvider = MockTranscriptionProvider()
    var noteGenerationProvider: NoteGenerationProvider = MockNoteGenerationProvider()

    val isLocalTranscriptionAvailable: Boolean
        get() = localTranscriptionEngine?.isAvailable() ?: false

    fun startLocalTranscription(recordingID: String) {
        val engine = localTranscriptionEngine ?: return
        val metadata = _recordings.value.find { it.id == recordingID }
            ?: fileStore.loadMetadata(recordingID) ?: return

        if (!com.rokurics.app.domain.provider.LocalTranscriptionEngine::class.java.isInstance(engine)) return

        val audioFile = fileStore.audioFileFor(metadata)
        if (!audioFile.exists()) {
            val updated = metadata.copy(transcriptionStatus = "failed")
            fileStore.updateMetadata(updated)
            loadExistingRecordings()
            _statusMessage.value = "音频文件不存在"
            return
        }

        // Set queued status
        var updated = metadata.copy(transcriptionStatus = "queued")
        fileStore.updateMetadata(updated)
        loadExistingRecordings()

        viewModelScope.launch(Dispatchers.IO) {
            // Set running status
            updated = (fileStore.loadMetadata(recordingID) ?: metadata).copy(transcriptionStatus = "running")
            fileStore.updateMetadata(updated)
            withContext(Dispatchers.Main) {
                loadExistingRecordings()
                _statusMessage.value = "本地转写中..."
            }

            val request = com.rokurics.app.domain.provider.TranscriptionRequest(
                recordingID = recordingID,
                audioFilePath = audioFile.absolutePath,
                language = "zh-CN"
            )

            val result = engine.transcribe(request)
            result.fold(
                onSuccess = { transcription ->
                    val final = (fileStore.loadMetadata(recordingID) ?: metadata).copy(
                        transcriptionStatus = "succeeded"
                    )
                    fileStore.updateMetadata(final)
                    // Save transcript to file
                    val transcriptFile = fileStore.makeMetadataFile("${recordingID}_transcript")
                    transcriptFile.writeText(transcription.transcript)
                    withContext(Dispatchers.Main) {
                        loadExistingRecordings()
                        _statusMessage.value = "转写完成"
                    }
                },
                onFailure = { error ->
                    val final = (fileStore.loadMetadata(recordingID) ?: metadata).copy(
                        transcriptionStatus = "failed"
                    )
                    fileStore.updateMetadata(final)
                    withContext(Dispatchers.Main) {
                        loadExistingRecordings()
                        _statusMessage.value = "转写失败: ${error.message}"
                    }
                }
            )
        }
    }

    fun startRemoteTranscription(recordingID: String) {
        val metadata = _recordings.value.find { it.id == recordingID }
            ?: fileStore.loadMetadata(recordingID) ?: return
        val audioFile = fileStore.audioFileFor(metadata)
        if (!audioFile.exists()) {
            _statusMessage.value = "音频文件不存在"
            return
        }
        var updated = metadata.copy(transcriptionStatus = "queued")
        fileStore.updateMetadata(updated)
        loadExistingRecordings()

        viewModelScope.launch(Dispatchers.IO) {
            updated = (fileStore.loadMetadata(recordingID) ?: metadata).copy(transcriptionStatus = "running")
            fileStore.updateMetadata(updated)
            withContext(Dispatchers.Main) {
                loadExistingRecordings()
                _statusMessage.value = "云端转写中..."
            }
            val request = TranscriptionRequest(
                recordingID = recordingID,
                audioFilePath = audioFile.absolutePath,
                language = "zh-CN"
            )
            transcriptionProvider.validateConfiguration()
            val result = transcriptionProvider.transcribe(request)
            result.fold(
                onSuccess = { transcription ->
                    val final = (fileStore.loadMetadata(recordingID) ?: metadata).copy(
                        transcriptionStatus = "succeeded"
                    )
                    fileStore.updateMetadata(final)
                    val transcriptFile = fileStore.makeMetadataFile("${recordingID}_transcript")
                    transcriptFile.writeText(transcription.transcript)
                    withContext(Dispatchers.Main) {
                        loadExistingRecordings()
                        _statusMessage.value = "云端转写完成"
                    }
                },
                onFailure = { error ->
                    val final = (fileStore.loadMetadata(recordingID) ?: metadata).copy(
                        transcriptionStatus = "failed"
                    )
                    fileStore.updateMetadata(final)
                    withContext(Dispatchers.Main) {
                        loadExistingRecordings()
                        _statusMessage.value = "云端转写失败: ${error.message}"
                    }
                }
            )
        }
    }

    fun startNoteGeneration(recordingID: String) {
        val metadata = _recordings.value.find { it.id == recordingID }
            ?: fileStore.loadMetadata(recordingID) ?: return

        val transcriptFile = fileStore.makeMetadataFile("${recordingID}_transcript")
        val transcript = if (transcriptFile.exists()) transcriptFile.readText() else ""

        var updated = metadata.copy(noteStatus = "queued")
        fileStore.updateMetadata(updated)
        loadExistingRecordings()

        viewModelScope.launch(Dispatchers.IO) {
            updated = (fileStore.loadMetadata(recordingID) ?: metadata).copy(noteStatus = "running")
            fileStore.updateMetadata(updated)
            withContext(Dispatchers.Main) {
                loadExistingRecordings()
                _statusMessage.value = "AI 笔记生成中..."
            }
            val request = NoteGenerationRequest(
                recordingID = recordingID,
                transcript = transcript,
                title = metadata.title,
                studyFilingPath = metadata.studyFiling?.displaySummary
            )
            noteGenerationProvider.validateConfiguration()
            val result = noteGenerationProvider.generateNote(request)
            result.fold(
                onSuccess = { note ->
                    val final = (fileStore.loadMetadata(recordingID) ?: metadata).copy(
                        noteStatus = "succeeded"
                    )
                    fileStore.updateMetadata(final)
                    val noteFile = fileStore.makeMetadataFile("${recordingID}_note")
                    noteFile.writeText(note.noteMarkdown)
                    withContext(Dispatchers.Main) {
                        loadExistingRecordings()
                        _statusMessage.value = "AI 笔记已生成"
                    }
                },
                onFailure = { error ->
                    val final = (fileStore.loadMetadata(recordingID) ?: metadata).copy(
                        noteStatus = "failed"
                    )
                    fileStore.updateMetadata(final)
                    withContext(Dispatchers.Main) {
                        loadExistingRecordings()
                        _statusMessage.value = "笔记生成失败: ${error.message}"
                    }
                }
            )
        }
    }

    val pendingUploadCount: Int
        get() = _recordings.value.count {
            RecordingUploadStatus.fromRawValue(it.uploadStatus) != RecordingUploadStatus.UPLOADED
        }

    private fun hasNotificationPermission(): Boolean {
        // Only required on Android 13+
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return true
        val result = ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS)
        return result == PackageManager.PERMISSION_GRANTED
    }

    private fun hasRecordPermission(): Boolean {
        val result = ContextCompat.checkSelfPermission(context, Manifest.permission.RECORD_AUDIO)
        return result == PackageManager.PERMISSION_GRANTED
    }

    private fun startRecorder() {
        _state.value = RokuricsRecordingState.CONFIGURING_SESSION
        _statusMessage.value = "正在配置录音"

        try {
            fileStore.ensureStorageDirectories()
            val file = fileStore.makeRecordingFileUrl()
            activeRecordingFile = file
            activeSettingsLabel = "primary"

            val recorder = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                MediaRecorder(context)
            } else {
                MediaRecorder()
            }

            recorder.apply {
                setAudioSource(MediaRecorder.AudioSource.MIC)
                setOutputFormat(MediaRecorder.OutputFormat.MPEG_4)
                setAudioEncoder(MediaRecorder.AudioEncoder.AAC)
                setAudioSamplingRate(16000)
                setAudioEncodingBitRate(64000)
                setAudioChannels(1)
                setOutputFile(file.absolutePath)
                prepare()
                start()
            }

            mediaRecorder = recorder
            recordingStartedAt = Date()
            _elapsedSeconds.value = 0.0
            _amplitudeLevel.value = 0f
            _state.value = RokuricsRecordingState.RECORDING
            _statusMessage.value = "正在录音"
            startTimer()
        } catch (e: Exception) {
            _state.value = RokuricsRecordingState.FAILED
            _statusMessage.value = "录音启动失败: ${e.message}"
            cleanupRecorder()
        }
    }

    private fun startTimer() {
        timerJob?.cancel()
        timerJob = viewModelScope.launch(Dispatchers.Main) {
            while (isActive) {
                delay(250)
                if (_state.value == RokuricsRecordingState.RECORDING) {
                    _elapsedSeconds.value = _elapsedSeconds.value + 0.25
                    val raw = mediaRecorder?.maxAmplitude ?: 0
                    _amplitudeLevel.value = (raw.toFloat() / 32767f).coerceIn(0f, 1f)
                }
            }
        }
    }

    private fun stopTimer() {
        timerJob?.cancel()
        timerJob = null
    }

    private fun cleanupRecorder() {
        stopTimer()
        try {
            mediaRecorder?.apply {
                stop()
                release()
            }
        } catch (_: Exception) {}
        mediaRecorder = null
    }

    private fun loadExistingRecordings() {
        try {
            fileStore.ensureStorageDirectories()
            _recordings.value = fileStore.loadAllMetadata()
            _trashedRecordings.value = fileStore.loadTrashedMetadata()
            _latestRecordingMetadata.value = _recordings.value.firstOrNull()
        } catch (e: Exception) {
            _statusMessage.value = "读取本地录音失败"
        }
    }

    override fun onCleared() {
        super.onCleared()
        cleanupRecorder()
    }
}
