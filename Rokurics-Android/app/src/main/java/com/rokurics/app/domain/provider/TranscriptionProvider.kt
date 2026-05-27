package com.rokurics.app.domain.provider

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.File
import java.net.HttpURLConnection
import java.net.URL

data class TranscriptionRequest(
    val recordingID: String,
    val audioFilePath: String,
    val language: String? = null
)

data class TranscriptionResult(
    val recordingID: String,
    val transcript: String,
    val segments: List<TranscriptionSegment> = emptyList(),
    val providerID: String,
    val modelName: String? = null
)

data class TranscriptionSegment(
    val startSeconds: Double,
    val endSeconds: Double,
    val text: String
)

interface TranscriptionProvider {
    val id: String
    val displayName: String
    suspend fun validateConfiguration()
    suspend fun transcribe(request: TranscriptionRequest): Result<TranscriptionResult>
}

class MockTranscriptionProvider : TranscriptionProvider {
    override val id = "mock_transcription"
    override val displayName = "Mock Transcription"
    override suspend fun validateConfiguration() {}
    override suspend fun transcribe(request: TranscriptionRequest): Result<TranscriptionResult> =
        Result.success(TranscriptionResult(
            recordingID = request.recordingID,
            transcript = "[Mock] This is a simulated transcription for recording ${request.recordingID}.",
            providerID = id,
            modelName = "mock-v1"
        ))
}

class OpenAICompatibleTranscriptionProvider(
    val baseURLString: String,
    val modelName: String,
    val apiKey: String
) : TranscriptionProvider {
    override val id = "openai_compatible_transcription"
    override val displayName = "OpenAI-compatible Transcription"

    override suspend fun validateConfiguration() {
        if (baseURLString.isBlank()) throw TranscriptionError("Base URL 未配置")
    }

    override suspend fun transcribe(request: TranscriptionRequest): Result<TranscriptionResult> =
        withContext(Dispatchers.IO) {
            try {
                val audioFile = File(request.audioFilePath)
                if (!audioFile.exists()) {
                    return@withContext Result.failure(TranscriptionError("音频文件不存在"))
                }

                val normalizedBase = baseURLString.trimEnd('/')
                val url = URL("$normalizedBase/audio/transcriptions")
                val boundary = "RokuricsBoundary${System.currentTimeMillis()}"
                val connection = url.openConnection() as HttpURLConnection

                connection.apply {
                    requestMethod = "POST"
                    doOutput = true
                    connectTimeout = 30000
                    readTimeout = 300000
                    setRequestProperty("Content-Type", "multipart/form-data; boundary=$boundary")
                    if (apiKey.isNotEmpty()) {
                        setRequestProperty("Authorization", "Bearer $apiKey")
                    }
                }

                connection.outputStream.use { output ->
                    fun writeField(name: String, value: String) {
                        val header = StringBuilder()
                            .append("--$boundary\r\n")
                            .append("Content-Disposition: form-data; name=\"$name\"\r\n\r\n")
                            .append("$value\r\n")
                        output.write(header.toString().toByteArray(Charsets.UTF_8))
                    }

                    writeField("model", modelName)
                    writeField("response_format", "verbose_json")
                    if (!request.language.isNullOrBlank()) {
                        writeField("language", request.language)
                    }

                    val fileHeader = StringBuilder()
                        .append("--$boundary\r\n")
                        .append("Content-Disposition: form-data; name=\"file\"; filename=\"${audioFile.name}\"\r\n")
                        .append("Content-Type: audio/mpeg\r\n\r\n")
                    output.write(fileHeader.toString().toByteArray(Charsets.UTF_8))
                    audioFile.inputStream().use { it.copyTo(output) }
                    output.write("\r\n--$boundary--\r\n".toByteArray(Charsets.UTF_8))
                }

                val code = connection.responseCode
                if (code !in 200..299) {
                    val errBody = try {
                        connection.errorStream?.bufferedReader()?.readText() ?: "HTTP $code"
                    } catch (_: Exception) { "HTTP $code" }
                    return@withContext Result.failure(TranscriptionError("转录请求失败: $errBody"))
                }

                val responseText = connection.inputStream.bufferedReader().readText()
                connection.disconnect()

                val response = JSONObject(responseText)
                val transcript = response.optString("text", "").trim()
                if (transcript.isEmpty()) {
                    return@withContext Result.failure(TranscriptionError("转录结果为空"))
                }

                val segments = mutableListOf<TranscriptionSegment>()
                val segmentsArray = response.optJSONArray("segments")
                if (segmentsArray != null) {
                    for (i in 0 until segmentsArray.length()) {
                        val seg = segmentsArray.getJSONObject(i)
                        segments.add(TranscriptionSegment(
                            startSeconds = seg.optDouble("start", 0.0),
                            endSeconds = seg.optDouble("end", 0.0),
                            text = seg.optString("text", "")
                        ))
                    }
                }

                Result.success(TranscriptionResult(
                    recordingID = request.recordingID,
                    transcript = transcript,
                    segments = segments,
                    providerID = id,
                    modelName = modelName
                ))
            } catch (e: TranscriptionError) {
                Result.failure(e)
            } catch (e: Exception) {
                Result.failure(TranscriptionError("转录失败: ${e.message}"))
            }
        }
}

class TranscriptionError(message: String) : Exception(message)
