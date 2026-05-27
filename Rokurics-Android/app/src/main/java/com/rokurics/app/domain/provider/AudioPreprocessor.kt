package com.rokurics.app.domain.provider

import android.media.MediaCodec
import android.media.MediaExtractor
import android.media.MediaFormat
import java.io.ByteArrayOutputStream
import java.io.File
import java.io.FileInputStream
import java.io.FileOutputStream
import java.io.RandomAccessFile
import java.nio.ByteBuffer
import java.nio.ByteOrder

// ── WAV Writer ──────────────────────────────────────────────────────

class WavWriter(private val sampleRate: Int = 16000, private val channels: Int = 1) {
    private val buffer = ByteArrayOutputStream()

    fun writePcm16(pcmData: ByteArray) {
        buffer.write(pcmData)
    }

    fun writePcm16Short(samples: ShortArray) {
        val bytes = ByteArray(samples.size * 2)
        ByteBuffer.wrap(bytes).order(ByteOrder.LITTLE_ENDIAN).asShortBuffer().put(samples)
        buffer.write(bytes)
    }

    fun finish(outputFile: File) {
        val data = buffer.toByteArray()
        val sampleBits = 16
        val byteRate = sampleRate * channels * sampleBits / 8
        val blockAlign = channels * sampleBits / 8

        FileOutputStream(outputFile).use { out ->
            val header = ByteBuffer.allocate(44).order(ByteOrder.LITTLE_ENDIAN)
            header.put("RIFF".toByteArray(Charsets.US_ASCII))
            header.putInt(36 + data.size)
            header.put("WAVE".toByteArray(Charsets.US_ASCII))
            header.put("fmt ".toByteArray(Charsets.US_ASCII))
            header.putInt(16) // PCM
            header.putShort(1) // audio format
            header.putShort(channels.toShort())
            header.putInt(sampleRate)
            header.putInt(byteRate)
            header.putShort(blockAlign.toShort())
            header.putShort(sampleBits.toShort())
            header.put("data".toByteArray(Charsets.US_ASCII))
            header.putInt(data.size)
            out.write(header.array())
            out.write(data)
        }
    }
}

// ── Conversion Result ────────────────────────────────────────────────

data class AudioConversionResult(
    val originalFile: File,
    val convertedFile: File,
    val didConvert: Boolean
)

// ── Audio Converter Interface ────────────────────────────────────────

interface AudioConverter {
    val id: String
    fun isAvailable(): Boolean
    fun convertToWav(inputFile: File, outputFile: File): Result<AudioConversionResult>
}

// ── Android MediaCodec-based Converter ───────────────────────────────

class AndroidMediaCodecAudioConverter : AudioConverter {
    override val id = "android_mediacodec_converter"
    override fun isAvailable(): Boolean = true

    override fun convertToWav(inputFile: File, outputFile: File): Result<AudioConversionResult> {
        if (!inputFile.exists() || inputFile.length() == 0L) {
            return Result.failure(AudioPreprocessError("输入音频文件不存在或为空"))
        }

        return try {
            val extractor = MediaExtractor()
            extractor.setDataSource(inputFile.absolutePath)

            var audioTrackIndex = -1
            var sampleRate = 16000
            var channelCount = 1

            for (i in 0 until extractor.trackCount) {
                val format = extractor.getTrackFormat(i)
                val mime = format.getString(MediaFormat.KEY_MIME) ?: continue
                if (mime.startsWith("audio/")) {
                    audioTrackIndex = i
                    sampleRate = format.getInteger(MediaFormat.KEY_SAMPLE_RATE, 16000)
                    channelCount = format.getInteger(MediaFormat.KEY_CHANNEL_COUNT, 1)
                    break
                }
            }

            if (audioTrackIndex < 0) {
                extractor.release()
                return Result.failure(AudioPreprocessError("未找到音频轨道"))
            }

            val inputFormat = extractor.getTrackFormat(audioTrackIndex)
            val mime = inputFormat.getString(MediaFormat.KEY_MIME) ?: "audio/mp4a-latm"
            val decoder = MediaCodec.createDecoderByType(mime)
            decoder.configure(inputFormat, null, null, 0)
            decoder.start()

            extractor.selectTrack(audioTrackIndex)

            val writer = WavWriter(sampleRate, channelCount)
            var done = false
            val bufferInfo = MediaCodec.BufferInfo()

            while (!done) {
                val inputIndex = decoder.dequeueInputBuffer(10000)
                if (inputIndex >= 0) {
                    val inputBuffer = decoder.getInputBuffer(inputIndex) ?: continue
                    val sampleSize = extractor.readSampleData(inputBuffer, 0)
                    if (sampleSize < 0) {
                        decoder.queueInputBuffer(inputIndex, 0, 0, 0, MediaCodec.BUFFER_FLAG_END_OF_STREAM)
                    } else {
                        decoder.queueInputBuffer(inputIndex, 0, sampleSize, extractor.sampleTime, 0)
                        extractor.advance()
                    }
                }

                val outputIndex = decoder.dequeueOutputBuffer(bufferInfo, 10000)
                when {
                    outputIndex == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED -> continue
                    outputIndex == MediaCodec.INFO_TRY_AGAIN_LATER -> continue
                    outputIndex >= 0 -> {
                        val outputBuffer = decoder.getOutputBuffer(outputIndex) ?: continue
                        val pcmData = ByteArray(bufferInfo.size)
                        outputBuffer.position(bufferInfo.offset)
                        outputBuffer.get(pcmData, 0, bufferInfo.size)
                        writer.writePcm16(pcmData)
                        decoder.releaseOutputBuffer(outputIndex, false)
                        if (bufferInfo.flags and MediaCodec.BUFFER_FLAG_END_OF_STREAM != 0) {
                            done = true
                        }
                    }
                }
            }

            decoder.stop()
            decoder.release()
            extractor.release()

            writer.finish(outputFile)

            if (!outputFile.exists() || outputFile.length() == 0L) {
                return Result.failure(AudioPreprocessError("转码后未生成有效WAV文件"))
            }

            Result.success(AudioConversionResult(
                originalFile = inputFile,
                convertedFile = outputFile,
                didConvert = true
            ))
        } catch (e: AudioPreprocessError) {
            Result.failure(e)
        } catch (e: Exception) {
            Result.failure(AudioPreprocessError("音频预处理失败: ${e.message}"))
        }
    }
}

// ── Passthrough Converter (for already-WAV input) ────────────────────

class PassthroughAudioConverter : AudioConverter {
    override val id = "passthrough"
    override fun isAvailable(): Boolean = true

    override fun convertToWav(inputFile: File, outputFile: File): Result<AudioConversionResult> {
        if (!inputFile.exists()) {
            return Result.failure(AudioPreprocessError("输入文件不存在"))
        }
        if (inputFile.absolutePath != outputFile.absolutePath) {
            inputFile.copyTo(outputFile, overwrite = true)
        }
        return Result.success(AudioConversionResult(
            originalFile = inputFile,
            convertedFile = outputFile,
            didConvert = inputFile.absolutePath != outputFile.absolutePath
        ))
    }
}

// ── Audio Preprocessor ───────────────────────────────────────────────

enum class PreprocessingStrategy {
    ANDROID_MEDIACODEC,
    PASSTHROUGH_ONLY
}

class AudioPreprocessor(
    private val strategy: PreprocessingStrategy = PreprocessingStrategy.ANDROID_MEDIACODEC
) {
    companion object {
        private val wavExtensions = setOf("wav", "wave")

        fun requiresConversion(file: File): Boolean {
            val ext = file.extension.lowercase()
            return ext !in wavExtensions
        }
    }

    private val converter: AudioConverter = when (strategy) {
        PreprocessingStrategy.ANDROID_MEDIACODEC -> AndroidMediaCodecAudioConverter()
        PreprocessingStrategy.PASSTHROUGH_ONLY -> PassthroughAudioConverter()
    }

    fun preprocess(inputFile: File, outputFile: File): Result<AudioConversionResult> {
        if (!requiresConversion(inputFile)) {
            return PassthroughAudioConverter().convertToWav(inputFile, outputFile)
        }

        if (!converter.isAvailable()) {
            return Result.failure(AudioPreprocessError("音频转换器不可用"))
        }

        return converter.convertToWav(inputFile, outputFile)
    }
}

class AudioPreprocessError(message: String) : Exception(message)
