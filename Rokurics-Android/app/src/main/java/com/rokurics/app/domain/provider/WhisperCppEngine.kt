package com.rokurics.app.domain.provider

import android.content.Context
import com.rokurics.app.RokuricsApp
import java.io.File

// ── WhisperCPP Configuration ─────────────────────────────────────────

data class WhisperCppConfiguration(
    val modelPath: String = "",
    val executablePath: String = "",
    val defaultLanguage: String = "auto",
    val preferSegmentOutput: Boolean = false,
    val useGpu: Boolean = false
) {
    val isValid: Boolean
        get() = modelPath.isNotBlank() && File(modelPath).exists()

    val modelName: String
        get() {
            val path = modelPath.trim()
            return if (path.isEmpty()) "未选择模型" else File(path).name
        }

    val language: String
        get() {
            val lang = defaultLanguage.trim()
            return lang.ifEmpty { "auto" }
        }
}

// ── Whisper Model Kind ───────────────────────────────────────────────

enum class WhisperModelKind(val displayName: String, val isLargePreferred: Boolean) {
    TINY("tiny", false),
    BASE("base", false),
    SMALL("small", false),
    MEDIUM("medium", false),
    LARGE_V1("large-v1", true),
    LARGE_V2("large-v2", true),
    LARGE_V3("large-v3", true),
    UNKNOWN("unknown", false);

    companion object {
        fun infer(modelFileName: String?): WhisperModelKind {
            if (modelFileName == null) return UNKNOWN
            val lower = modelFileName.lowercase()
            return when {
                lower.contains("large-v3") || lower.contains("large-v3-turbo") -> LARGE_V3
                lower.contains("large-v2") -> LARGE_V2
                lower.contains("large-v1") || lower.contains("large") -> LARGE_V1
                lower.contains("medium") -> MEDIUM
                lower.contains("small") -> SMALL
                lower.contains("base") -> BASE
                lower.contains("tiny") -> TINY
                else -> UNKNOWN
            }
        }
    }
}

// ── WhisperCPP Engine Status ─────────────────────────────────────────

enum class WhisperEngineStatus(val label: String) {
    NOT_CONFIGURED("未配置"),
    NATIVE_LIBRARY_MISSING("缺少原生库"),
    MODEL_NOT_FOUND("模型文件未找到"),
    EXECUTABLE_NOT_FOUND("可执行文件未找到"),
    AVAILABLE("可用"),
    BUSY("忙碌中")
}

// ── WhisperCPP Engine ────────────────────────────────────────────────

class WhisperCppEngine(
    private val configuration: WhisperCppConfiguration = WhisperCppConfiguration(),
    private val preprocessor: AudioPreprocessor = AudioPreprocessor(),
    private val context: Context? = null
) : LocalTranscriptionEngine {

    private fun appContext(): Context? = context ?: runCatching { RokuricsApp.instance }.getOrNull()

    override val id = "whisper_cpp"
    override val displayName = "WhisperCPP 本地转写"

    private val nativeLibAvailable: Boolean by lazy { checkNativeLibrary() }

    val status: WhisperEngineStatus
        get() = when {
            !configuration.isValid && configuration.modelPath.isBlank() -> WhisperEngineStatus.NOT_CONFIGURED
            !configuration.isValid -> WhisperEngineStatus.MODEL_NOT_FOUND
            !nativeLibAvailable && configuration.executablePath.isBlank() -> WhisperEngineStatus.NATIVE_LIBRARY_MISSING
            !nativeLibAvailable && configuration.executablePath.isNotBlank() -> WhisperEngineStatus.EXECUTABLE_NOT_FOUND
            else -> WhisperEngineStatus.AVAILABLE
        }

    val modelKind: WhisperModelKind
        get() = WhisperModelKind.infer(configuration.modelName)

    val configuredLanguage: String
        get() = configuration.language

    override fun isAvailable(): Boolean = status == WhisperEngineStatus.AVAILABLE

    override suspend fun transcribe(request: TranscriptionRequest): Result<TranscriptionResult> {
        val currentStatus = status
        if (currentStatus != WhisperEngineStatus.AVAILABLE) {
            return Result.failure(TranscriptionError("WhisperCPP 引擎不可用: ${currentStatus.label}"))
        }

        val audioFile = File(request.audioFilePath)
        if (!audioFile.exists() || audioFile.length() == 0L) {
            return Result.failure(TranscriptionError("音频文件不存在或为空"))
        }

        val ctx = appContext()
        val workingDir = if (ctx != null) {
            File(ctx.cacheDir, "whisper_working")
        } else {
            File(System.getProperty("java.io.tmpdir") ?: "/tmp", "whisper_working")
        }
        workingDir.mkdirs()

        val wavFile = File(workingDir, "${request.recordingID}_preprocessed.wav")

        val conversionResult = if (AudioPreprocessor.requiresConversion(audioFile)) {
            preprocessor.preprocess(audioFile, wavFile)
        } else {
            Result.success(AudioConversionResult(audioFile, wavFile, didConvert = false))
        }

        if (conversionResult.isFailure) {
            return Result.failure(TranscriptionError(
                "音频预处理失败: ${conversionResult.exceptionOrNull()?.message}"
            ))
        }

        val preparedAudio = conversionResult.getOrThrow().convertedFile

        return if (nativeLibAvailable) {
            transcribeViaNative(preparedAudio, request)
        } else if (configuration.executablePath.isNotBlank()) {
            transcribeViaExecutable(preparedAudio, request)
        } else {
            Result.failure(TranscriptionError("无可用的WhisperCPP运行时"))
        }
    }

    private fun transcribeViaNative(
        wavFile: File,
        request: TranscriptionRequest
    ): Result<TranscriptionResult> {
        // Placeholder: Native whisper.cpp JNI call would go here.
        // When whisper.cpp .so is bundled via CMake/NDK, call:
        //   WhisperLib.transcribe(wavFile.absolutePath, modelPath, language)
        return Result.failure(TranscriptionError(
            "WhisperCPP原生库未集成。请通过NDK/CMake集成whisper.cpp后重新构建。"
        ))
    }

    private fun transcribeViaExecutable(
        wavFile: File,
        request: TranscriptionRequest
    ): Result<TranscriptionResult> {
        // Placeholder: CLI-based transcription via ProcessBuilder.
        // Android sandbox restrictions generally prevent this on non-rooted devices.
        return Result.failure(TranscriptionError(
            "Android沙箱限制无法直接调用外部可执行文件。请使用原生库集成方式。"
        ))
    }

    private fun checkNativeLibrary(): Boolean {
        return try {
            System.loadLibrary("whisper")
            true
        } catch (_: UnsatisfiedLinkError) {
            false
        }
    }

    // ── Configuration Validation ──────────────────────────────────────

    fun validateConfiguration(): Result<Unit> {
        val s = status
        return when (s) {
            WhisperEngineStatus.NOT_CONFIGURED ->
                Result.failure(TranscriptionError("请先配置Whisper模型文件路径"))
            WhisperEngineStatus.MODEL_NOT_FOUND ->
                Result.failure(TranscriptionError("模型文件不存在: ${configuration.modelPath}"))
            WhisperEngineStatus.NATIVE_LIBRARY_MISSING ->
                Result.failure(TranscriptionError("WhisperCPP原生库(libwhisper.so)未集成"))
            WhisperEngineStatus.EXECUTABLE_NOT_FOUND ->
                Result.failure(TranscriptionError("WhisperCPP可执行文件未找到"))
            WhisperEngineStatus.AVAILABLE ->
                Result.success(Unit)
            WhisperEngineStatus.BUSY ->
                Result.failure(TranscriptionError("WhisperCPP引擎忙碌中"))
        }
    }

    // ── Builder ───────────────────────────────────────────────────────

    class Builder {
        private var modelPath: String = ""
        private var executablePath: String = ""
        private var language: String = "auto"
        private var preferSegments: Boolean = false
        private var useGpu: Boolean = false
        private var preprocessor: AudioPreprocessor = AudioPreprocessor()

        fun modelPath(path: String): Builder = apply { this.modelPath = path }
        fun executablePath(path: String): Builder = apply { this.executablePath = path }
        fun language(lang: String): Builder = apply { this.language = lang }
        fun preferSegments(prefer: Boolean): Builder = apply { this.preferSegments = prefer }
        fun useGpu(gpu: Boolean): Builder = apply { this.useGpu = gpu }
        fun preprocessor(p: AudioPreprocessor): Builder = apply { this.preprocessor = p }

        fun build(): WhisperCppEngine {
            val config = WhisperCppConfiguration(
                modelPath = modelPath,
                executablePath = executablePath,
                defaultLanguage = language,
                preferSegmentOutput = preferSegments,
                useGpu = useGpu
            )
            return WhisperCppEngine(config, preprocessor)
        }
    }
}
