package com.rokurics.app.domain.provider

import kotlinx.coroutines.runBlocking
import org.junit.Assert.*
import org.junit.Test
import java.io.File

class WhisperCppEngineTest {

    private fun testFilePath(name: String): File {
        val tmpDir = System.getenv("TMPDIR")
            ?: System.getProperty("java.io.tmpdir")
            ?: "/tmp"
        return File(tmpDir, "rokurics_test_${name}_${System.nanoTime()}")
    }

    // ── WhisperCppConfiguration Tests ─────────────────────────────────

    @Test
    fun testConfigurationInvalidWhenModelPathBlank() {
        val config = WhisperCppConfiguration(modelPath = "")
        assertFalse(config.isValid)
    }

    @Test
    fun testConfigurationInvalidWhenModelFileMissing() {
        val config = WhisperCppConfiguration(modelPath = "/nonexistent/ggml-model.bin")
        assertFalse(config.isValid)
    }

    @Test
    fun testConfigurationModelName() {
        val config = WhisperCppConfiguration(modelPath = "/models/ggml-large-v3.bin")
        assertEquals("ggml-large-v3.bin", config.modelName)
    }

    @Test
    fun testConfigurationModelNameUnset() {
        val config = WhisperCppConfiguration()
        assertEquals("未选择模型", config.modelName)
    }

    @Test
    fun testConfigurationLanguageDefault() {
        val config = WhisperCppConfiguration()
        assertEquals("auto", config.language)
    }

    @Test
    fun testConfigurationLanguageCustom() {
        val config = WhisperCppConfiguration(defaultLanguage = "zh")
        assertEquals("zh", config.language)
    }

    @Test
    fun testConfigurationLanguageTrimmed() {
        val config = WhisperCppConfiguration(defaultLanguage = "  en  ")
        assertEquals("en", config.language)
    }

    @Test
    fun testConfigurationFieldsPreserved() {
        val config = WhisperCppConfiguration(
            modelPath = "/models/ggml-base.bin",
            executablePath = "/usr/local/bin/whisper",
            defaultLanguage = "ja",
            preferSegmentOutput = true,
            useGpu = true
        )
        assertEquals("/models/ggml-base.bin", config.modelPath)
        assertEquals("/usr/local/bin/whisper", config.executablePath)
        assertEquals("ja", config.language)
        assertTrue(config.preferSegmentOutput)
        assertTrue(config.useGpu)
    }

    // ── WhisperModelKind Tests ────────────────────────────────────────

    @Test
    fun testInferTiny() {
        assertEquals(WhisperModelKind.TINY, WhisperModelKind.infer("ggml-tiny.bin"))
    }

    @Test
    fun testInferBase() {
        assertEquals(WhisperModelKind.BASE, WhisperModelKind.infer("ggml-base.bin"))
    }

    @Test
    fun testInferSmall() {
        assertEquals(WhisperModelKind.SMALL, WhisperModelKind.infer("ggml-small.bin"))
    }

    @Test
    fun testInferMedium() {
        assertEquals(WhisperModelKind.MEDIUM, WhisperModelKind.infer("ggml-medium.bin"))
    }

    @Test
    fun testInferLargeV1() {
        assertEquals(WhisperModelKind.LARGE_V1, WhisperModelKind.infer("ggml-large-v1.bin"))
    }

    @Test
    fun testInferLargeV2() {
        assertEquals(WhisperModelKind.LARGE_V2, WhisperModelKind.infer("ggml-large-v2.bin"))
    }

    @Test
    fun testInferLargeV3() {
        assertEquals(WhisperModelKind.LARGE_V3, WhisperModelKind.infer("ggml-large-v3.bin"))
    }

    @Test
    fun testInferLargeV3Turbo() {
        assertEquals(WhisperModelKind.LARGE_V3, WhisperModelKind.infer("ggml-large-v3-turbo.bin"))
    }

    @Test
    fun testInferUnknown() {
        assertEquals(WhisperModelKind.UNKNOWN, WhisperModelKind.infer("some-file.bin"))
    }

    @Test
    fun testInferNull() {
        assertEquals(WhisperModelKind.UNKNOWN, WhisperModelKind.infer(null))
    }

    @Test
    fun testInferCaseInsensitive() {
        assertEquals(WhisperModelKind.LARGE_V3, WhisperModelKind.infer("GGML-LARGE-V3.bin"))
    }

    @Test
    fun testModelKindDisplayNames() {
        assertEquals("tiny", WhisperModelKind.TINY.displayName)
        assertEquals("base", WhisperModelKind.BASE.displayName)
        assertEquals("large-v3", WhisperModelKind.LARGE_V3.displayName)
    }

    @Test
    fun testLargePreferred() {
        assertFalse(WhisperModelKind.TINY.isLargePreferred)
        assertFalse(WhisperModelKind.BASE.isLargePreferred)
        assertTrue(WhisperModelKind.LARGE_V3.isLargePreferred)
    }

    // ── Engine Status Tests ───────────────────────────────────────────

    @Test
    fun testEngineStatusNotConfigured() {
        val engine = WhisperCppEngine(WhisperCppConfiguration())
        assertEquals(WhisperEngineStatus.NOT_CONFIGURED, engine.status)
        assertFalse(engine.isAvailable())
    }

    @Test
    fun testEngineStatusModelNotFound() {
        val config = WhisperCppConfiguration(modelPath = "/nonexistent/model.bin")
        val engine = WhisperCppEngine(config)
        assertEquals(WhisperEngineStatus.MODEL_NOT_FOUND, engine.status)
    }

    @Test
    fun testEngineStatusNativeLibMissing() {
        val modelFile = testFilePath("model.bin")
        try {
            modelFile.writeBytes(ByteArray(1024))
            val config = WhisperCppConfiguration(modelPath = modelFile.absolutePath)
            val engine = WhisperCppEngine(config)
            assertEquals(WhisperEngineStatus.NATIVE_LIBRARY_MISSING, engine.status)
        } finally {
            modelFile.delete()
        }
    }

    @Test
    fun testEngineStatusExecutableNotFound() {
        val modelFile = testFilePath("model.bin")
        try {
            modelFile.writeBytes(ByteArray(1024))
            val config = WhisperCppConfiguration(
                modelPath = modelFile.absolutePath,
                executablePath = "/nonexistent/whisper"
            )
            val engine = WhisperCppEngine(config)
            assertEquals(WhisperEngineStatus.EXECUTABLE_NOT_FOUND, engine.status)
        } finally {
            modelFile.delete()
        }
    }

    @Test
    fun testWhisperEngineStatusLabels() {
        assertEquals("未配置", WhisperEngineStatus.NOT_CONFIGURED.label)
        assertEquals("缺少原生库", WhisperEngineStatus.NATIVE_LIBRARY_MISSING.label)
        assertEquals("可用", WhisperEngineStatus.AVAILABLE.label)
    }

    // ── Engine Identity Tests ─────────────────────────────────────────

    @Test
    fun testEngineId() {
        val engine = WhisperCppEngine()
        assertEquals("whisper_cpp", engine.id)
        assertEquals("WhisperCPP 本地转写", engine.displayName)
    }

    @Test
    fun testModelKindInference() {
        val engine = WhisperCppEngine(WhisperCppConfiguration(
            modelPath = "/models/ggml-small.bin"
        ))
        assertEquals(WhisperModelKind.SMALL, engine.modelKind)
    }

    @Test
    fun testConfiguredLanguage() {
        val engine = WhisperCppEngine(WhisperCppConfiguration(defaultLanguage = "zh"))
        assertEquals("zh", engine.configuredLanguage)
    }

    // ── Transcribe Failure Tests ──────────────────────────────────────

    @Test
    fun testTranscribeFailsWhenNotAvailable() = runBlocking {
        val engine = WhisperCppEngine(WhisperCppConfiguration())
        val result = engine.transcribe(TranscriptionRequest(
            recordingID = "rec-1",
            audioFilePath = "/fake/audio.m4a"
        ))
        assertTrue(result.isFailure)
        assertTrue(result.exceptionOrNull()?.message?.contains("不可用") == true)
    }

    @Test
    fun testTranscribeFailsWithMissingFile() = runBlocking {
        val engine = WhisperCppEngine(WhisperCppConfiguration())
        val result = engine.transcribe(TranscriptionRequest(
            recordingID = "rec-1",
            audioFilePath = "/nonexistent/audio.m4a"
        ))
        assertTrue(result.isFailure)
    }

    // ── ValidateConfiguration Tests ───────────────────────────────────

    @Test
    fun testValidateConfigurationNotConfigured() {
        val engine = WhisperCppEngine(WhisperCppConfiguration())
        val result = engine.validateConfiguration()
        assertTrue(result.isFailure)
        assertTrue(result.exceptionOrNull()?.message?.contains("模型") == true)
    }

    // ── Builder Tests ─────────────────────────────────────────────────

    @Test
    fun testBuilderCreatesEngine() {
        val engine = WhisperCppEngine.Builder()
            .modelPath("/models/ggml-base.bin")
            .language("zh")
            .preferSegments(true)
            .build()

        assertEquals("whisper_cpp", engine.id)
        assertEquals("zh", engine.configuredLanguage)
        assertEquals(WhisperModelKind.BASE, engine.modelKind)
    }

    @Test
    fun testBuilderDefaults() {
        val engine = WhisperCppEngine.Builder().build()
        assertEquals("whisper_cpp", engine.id)
        assertEquals(WhisperEngineStatus.NOT_CONFIGURED, engine.status)
    }

    @Test
    fun testBuilderWithGpu() {
        val modelFile = testFilePath("model.bin")
        try {
            modelFile.writeBytes(ByteArray(512))
            val engine = WhisperCppEngine.Builder()
                .modelPath(modelFile.absolutePath)
                .useGpu(true)
                .build()
            assertEquals(WhisperModelKind.UNKNOWN, engine.modelKind)
        } finally {
            modelFile.delete()
        }
    }

    // ── Status Enum Completeness ──────────────────────────────────────

    @Test
    fun testWhisperEngineStatusAllValues() {
        val values = WhisperEngineStatus.values()
        assertEquals(6, values.size)
    }

    @Test
    fun testWhisperModelKindAllValues() {
        assertEquals(8, WhisperModelKind.values().size)
    }

    @Test
    fun testPreprocessingStrategyValues() {
        assertEquals(2, PreprocessingStrategy.values().size)
    }
}
