package com.rokurics.app.domain.provider

import org.junit.Assert.*
import org.junit.Test
import java.io.File

class AudioPreprocessorTest {

    private fun testFilePath(name: String): File {
        val tmpDir = System.getenv("TMPDIR")
            ?: System.getProperty("java.io.tmpdir")
            ?: "/tmp"
        return File(tmpDir, "rokurics_test_${name}_${System.nanoTime()}")
    }

    // ── WavWriter Tests ───────────────────────────────────────────────

    @Test
    fun testWavWriterCreatesValidHeader() {
        val tempFile = testFilePath("header.wav")
        try {
            val writer = WavWriter(16000, 1)
            val samples = ShortArray(16000) { (it % 256).toShort() }
            writer.writePcm16Short(samples)
            writer.finish(tempFile)

            assertTrue(tempFile.exists())
            assertTrue(tempFile.length() > 44)

            val bytes = tempFile.readBytes()
            val header = String(bytes, 0, 4, Charsets.US_ASCII)
            assertEquals("RIFF", header)
            val wave = String(bytes, 8, 4, Charsets.US_ASCII)
            assertEquals("WAVE", wave)
        } finally {
            tempFile.delete()
        }
    }

    @Test
    fun testWavWriterEmpty() {
        val tempFile = testFilePath("empty.wav")
        try {
            val writer = WavWriter(16000, 1)
            writer.finish(tempFile)
            assertTrue(tempFile.exists())
            assertEquals(44, tempFile.length())
        } finally {
            tempFile.delete()
        }
    }

    @Test
    fun testWavWriterPcm16Bytes() {
        val tempFile = testFilePath("pcm16.wav")
        try {
            val writer = WavWriter(8000, 2)
            val pcmData = ByteArray(1024) { (it % 128).toByte() }
            writer.writePcm16(pcmData)
            writer.finish(tempFile)

            assertTrue(tempFile.length() > 44)
            val bytes = tempFile.readBytes()
            val sr = ((bytes[27].toInt() and 0xFF) shl 24) or
                     ((bytes[26].toInt() and 0xFF) shl 16) or
                     ((bytes[25].toInt() and 0xFF) shl 8) or
                     (bytes[24].toInt() and 0xFF)
            assertEquals(8000, sr)
            val ch = ((bytes[23].toInt() and 0xFF) shl 8) or
                     (bytes[22].toInt() and 0xFF)
            assertEquals(2, ch)
        } finally {
            tempFile.delete()
        }
    }

    // ── RequiresConversion Tests ──────────────────────────────────────

    @Test
    fun testRequiresConversionForM4a() {
        assertTrue(AudioPreprocessor.requiresConversion(File("recording.m4a")))
    }

    @Test
    fun testRequiresConversionForMp3() {
        assertTrue(AudioPreprocessor.requiresConversion(File("audio.mp3")))
    }

    @Test
    fun testNoConversionForWav() {
        assertFalse(AudioPreprocessor.requiresConversion(File("audio.wav")))
    }

    @Test
    fun testNoConversionForWave() {
        assertFalse(AudioPreprocessor.requiresConversion(File("audio.WAVE")))
    }

    @Test
    fun testRequiresConversionForAac() {
        assertTrue(AudioPreprocessor.requiresConversion(File("recording.aac")))
    }

    // ── AudioPreprocessor Tests ───────────────────────────────────────

    @Test
    fun testPreprocessorPassthroughForWav() {
        val tempFile = testFilePath("src.wav")
        val outFile = testFilePath("out.wav")
        try {
            tempFile.writeBytes(ByteArray(1024))
            val preprocessor = AudioPreprocessor(PreprocessingStrategy.PASSTHROUGH_ONLY)
            val result = preprocessor.preprocess(tempFile, outFile)
            assertTrue(result.isSuccess)
        } finally {
            tempFile.delete()
            outFile.delete()
        }
    }

    @Test
    fun testPreprocessorRequiresConversionForNonWav() {
        val tempFile = testFilePath("src.m4a")
        val outFile = testFilePath("out.wav")
        try {
            tempFile.writeBytes(ByteArray(100))
            val preprocessor = AudioPreprocessor(PreprocessingStrategy.PASSTHROUGH_ONLY)
            val result = preprocessor.preprocess(tempFile, outFile)
            assertTrue(result.isSuccess)
        } finally {
            tempFile.delete()
            outFile.delete()
        }
    }

    // ── PassthroughAudioConverter Tests ───────────────────────────────

    @Test
    fun testPassthroughConverterCopy() {
        val srcFile = testFilePath("src.m4a")
        val outFile = testFilePath("dst.wav")
        try {
            srcFile.writeBytes("dummy audio data".toByteArray())
            val converter = PassthroughAudioConverter()
            val result = converter.convertToWav(srcFile, outFile)
            assertTrue(result.isSuccess)
            assertTrue(outFile.exists())
            assertEquals(srcFile.length(), outFile.length())
        } finally {
            srcFile.delete()
            outFile.delete()
        }
    }

    @Test
    fun testPassthroughConverterMissingFile() {
        val missing = File("/nonexistent/audio.m4a")
        val outFile = testFilePath("out.wav")
        try {
            val converter = PassthroughAudioConverter()
            val result = converter.convertToWav(missing, outFile)
            assertTrue(result.isFailure)
            assertTrue(result.exceptionOrNull() is AudioPreprocessError)
        } finally {
            outFile.delete()
        }
    }

    // ── AudioConversionResult Tests ───────────────────────────────────

    @Test
    fun testAudioConversionResultFields() {
        val original = File("/tmp/original.m4a")
        val converted = File("/tmp/converted.wav")
        val result = AudioConversionResult(original, converted, didConvert = true)
        assertEquals(original, result.originalFile)
        assertEquals(converted, result.convertedFile)
        assertTrue(result.didConvert)
    }

    @Test
    fun testAudioConversionResultNoConversion() {
        val file = File("/tmp/audio.wav")
        val result = AudioConversionResult(file, file, didConvert = false)
        assertFalse(result.didConvert)
        assertEquals(file, result.originalFile)
    }

    // ── AudioPreprocessError Tests ────────────────────────────────────

    @Test
    fun testAudioPreprocessError() {
        val error = AudioPreprocessError("some error")
        assertEquals("some error", error.message)
        assertTrue(error is Exception)
    }

    // ── AndroidMediaCodecConverter Tests ──────────────────────────────

    @Test
    fun testMediaCodecConverterIsAvailable() {
        val converter = AndroidMediaCodecAudioConverter()
        assertTrue(converter.isAvailable())
        assertEquals("android_mediacodec_converter", converter.id)
    }

    @Test
    fun testMediaCodecConverterMissingFile() {
        val converter = AndroidMediaCodecAudioConverter()
        val result = converter.convertToWav(
            File("/nonexistent.m4a"),
            File("/tmp/out.wav")
        )
        assertTrue(result.isFailure)
    }
}
