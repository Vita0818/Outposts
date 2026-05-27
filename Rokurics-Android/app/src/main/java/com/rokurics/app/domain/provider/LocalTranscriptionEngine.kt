package com.rokurics.app.domain.provider

import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.speech.RecognitionListener
import android.speech.RecognizerIntent
import android.speech.SpeechRecognizer
import com.rokurics.app.RokuricsApp
import kotlinx.coroutines.CompletableDeferred

// ── Local Transcription Engine Interface ─────────────────────────

interface LocalTranscriptionEngine {
    val id: String
    val displayName: String
    fun isAvailable(): Boolean
    suspend fun transcribe(request: TranscriptionRequest): Result<TranscriptionResult>
}

// ── Transcription Status ──────────────────────────────────────────

enum class LocalTranscriptionStatus(val label: String) {
    UNAVAILABLE("不可用"),
    AVAILABLE("可用"),
    QUEUED("排队中"),
    RUNNING("转写中"),
    SUCCEEDED("已完成"),
    FAILED("失败")
}

// ── Android SpeechRecognizer-based Engine ─────────────────────────

class AndroidSpeechRecognizerEngine(
    private val context: Context = RokuricsApp.instance
) : LocalTranscriptionEngine {

    override val id = "android_speech_recognizer"
    override val displayName = "Android 本地语音识别"

    override fun isAvailable(): Boolean {
        return SpeechRecognizer.isRecognitionAvailable(context)
    }

    override suspend fun transcribe(request: TranscriptionRequest): Result<TranscriptionResult> {
        if (!isAvailable()) {
            return Result.failure(TranscriptionError("本地语音识别不可用"))
        }
        val audioFile = java.io.File(request.audioFilePath)
        if (!audioFile.exists() || audioFile.length() == 0L) {
            return Result.failure(TranscriptionError("音频文件不存在或为空"))
        }

        val deferred = CompletableDeferred<Result<TranscriptionResult>>()
        val recognizer = SpeechRecognizer.createSpeechRecognizer(context)
        var completed = false

        try {
            recognizer.setRecognitionListener(object : RecognitionListener {
                override fun onReadyForSpeech(params: Bundle?) {}

                override fun onBeginningOfSpeech() {}

                override fun onRmsChanged(rmsdB: Float) {}

                override fun onBufferReceived(buffer: ByteArray?) {}

                override fun onEndOfSpeech() {}

                override fun onError(error: Int) {
                    if (!completed) {
                        completed = true
                        val msg = when (error) {
                            SpeechRecognizer.ERROR_NETWORK -> "语音识别网络错误"
                            SpeechRecognizer.ERROR_NETWORK_TIMEOUT -> "语音识别网络超时"
                            SpeechRecognizer.ERROR_NO_MATCH -> "语音识别无匹配结果"
                            SpeechRecognizer.ERROR_RECOGNIZER_BUSY -> "语音识别引擎忙碌"
                            SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS -> "语音识别权限不足"
                            SpeechRecognizer.ERROR_CLIENT -> "语音识别客户端错误"
                            SpeechRecognizer.ERROR_SPEECH_TIMEOUT -> "语音输入超时"
                            SpeechRecognizer.ERROR_SERVER -> "语音识别服务错误"
                            else -> "语音识别错误: $error"
                        }
                        recognizer.destroy()
                        deferred.complete(Result.failure(TranscriptionError(msg)))
                    }
                }

                override fun onResults(results: Bundle?) {
                    if (!completed) {
                        completed = true
                        val matches = results?.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION)
                        if (matches.isNullOrEmpty()) {
                            recognizer.destroy()
                            deferred.complete(Result.failure(TranscriptionError("语音识别无结果")))
                        } else {
                            val transcript = matches.joinToString(" ")
                            recognizer.destroy()
                            deferred.complete(Result.success(TranscriptionResult(
                                recordingID = request.recordingID,
                                transcript = transcript,
                                providerID = id,
                                modelName = "android_speech"
                            )))
                        }
                    }
                }

                override fun onPartialResults(partialResults: Bundle?) {}

                override fun onEvent(eventType: Int, params: Bundle?) {}
            })

            val intent = Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH).apply {
                putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM)
                putExtra(RecognizerIntent.EXTRA_LANGUAGE, request.language ?: "zh-CN")
                putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true)
                putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 3)
            }
            recognizer.startListening(intent)

            return deferred.await()
        } catch (e: Exception) {
            try { recognizer.destroy() } catch (_: Exception) {}
            return Result.failure(TranscriptionError("本地转写失败: ${e.message}"))
        }
    }
}

// ── Fake Engine for Testing ───────────────────────────────────────

class FakeLocalTranscriptionEngine(
    private val shouldSucceed: Boolean = true,
    private val simulatedTranscript: String = "[Fake] 这是一段模拟的本地转写结果。",
    private val simulateAvailable: Boolean = true
) : LocalTranscriptionEngine {
    override val id = "fake_local_transcription"
    override val displayName = "Fake Local Transcription"

    var callCount = 0
        private set

    override fun isAvailable(): Boolean = simulateAvailable

    override suspend fun transcribe(request: TranscriptionRequest): Result<TranscriptionResult> {
        callCount++
        return if (shouldSucceed) {
            Result.success(TranscriptionResult(
                recordingID = request.recordingID,
                transcript = simulatedTranscript,
                providerID = id,
                modelName = "fake-v1"
            ))
        } else {
            Result.failure(TranscriptionError("Fake local transcription failure"))
        }
    }
}
