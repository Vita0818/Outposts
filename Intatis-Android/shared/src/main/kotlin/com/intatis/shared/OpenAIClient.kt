package com.intatis.shared

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.isString
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import com.intatis.shared.provider.GeneratedImage
import java.io.BufferedReader
import java.io.File
import java.io.InputStreamReader
import java.io.OutputStreamWriter
import java.net.HttpURLConnection
import java.net.URL
import java.util.Base64
import kotlin.math.max

class OpenAIClient(private val config: IntatisConfig) {
    private val json = Json { ignoreUnknownKeys = true }

    data class ToolCall(val id: String, val name: String, val arguments: String)

    data class OpenAIChatMessage(
        val role: String,
        val content: String? = null,
        val toolCalls: List<ToolCall>? = null,
        val toolCallId: String? = null,
        val images: List<ImageAttachment> = emptyList(),
    )

    data class ToolCallResult(
        val text: String,
        val toolCalls: List<ToolCall>,
        val latencyMs: Long,
        val usage: String?,
    )

    suspend fun sendAsync(
        messages: List<IntatisMessage>,
        model: String? = null,
        reasoning: String? = null,
        attachments: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
    ): Triple<String, Long, String?> {
        require(config.apiKey.isNotBlank()) { "INTATIS_API_KEY is required." }

        val resolvedModel = if (model.isNullOrBlank()) config.model else model
        val normalizedImages = attachments.filterIsInstance<ImageAttachment>()
        val request = buildMessageRequest(
            messages = messages,
            model = resolvedModel,
            reasoning = reasoning,
            images = normalizedImages,
            includeUsage = includeUsage,
        )

        val responseText = execute(request)
        val root = json.parseToJsonElement(responseText).jsonObject
        val usage = if (root["usage"] == null) null else root["usage"]!!.toString()

        val choices = root["choices"]?.jsonArray ?: throw IllegalStateException("Empty response from model provider.")
        if (choices.isEmpty()) throw IllegalStateException("Empty response from model provider.")
        val message = choices[0].jsonObject["message"]!!.jsonObject
        val content = extractContentText(message)

        val latency = 0L
        return Triple(content.trim(), latency, usage)
    }

    suspend fun sendWithToolsAsync(
        messages: List<OpenAIChatMessage>,
        tools: List<ToolDescriptor>,
        model: String? = null,
        reasoning: String? = null,
        includeUsage: Boolean = false,
    ): ToolCallResult {
        require(config.apiKey.isNotBlank()) { "INTATIS_API_KEY is required." }

        val resolvedModel = if (model.isNullOrBlank()) config.model else model
        val request = buildToolRequest(messages, tools, resolvedModel, reasoning, includeUsage)
        val responseText = execute(request)
        val root = json.parseToJsonElement(responseText).jsonObject

        val choices = root["choices"]?.jsonArray ?: throw IllegalStateException("Empty response from model provider.")
        if (choices.isEmpty()) throw IllegalStateException("Empty response from model provider.")
        val message = choices[0].jsonObject["message"]!!.jsonObject
        val content = extractContentText(message)
        val calls = parseToolCalls(message)

        val usage = if (root["usage"] == null) null else root["usage"]!!.toString()
        return ToolCallResult(content, calls, 0L, usage)
    }

    suspend fun generateImagesAsync(
        model: String,
        prompt: String,
        size: String,
        count: Int,
    ): List<GeneratedImage> {
        require(config.apiKey.isNotBlank()) { "INTATIS_API_KEY is required." }
        require(model.isNotBlank()) { "image generation model is required." }
        require(prompt.isNotBlank()) { "image prompt is required." }
        require(count in 1..4) { "image count must be in range [1, 4]." }

        val request = buildJsonObject {
            put("model", JsonPrimitive(model))
            put("prompt", JsonPrimitive(prompt))
            put("size", JsonPrimitive(size.ifBlank { "1024x1024" }))
            put("n", JsonPrimitive(count))
            put("response_format", JsonPrimitive("b64_json"))
        }

        val responseText = executeJson("/images/generations", request)
        val root = runCatching { json.parseToJsonElement(responseText).jsonObject }
            .getOrElse { throw imageGenerationResponseError(responseText, underlying = it) }

        val data = root["data"]?.jsonArray ?: throw imageGenerationResponseError(responseText)
        if (data.isEmpty()) {
            throw imageGenerationResponseError(responseText, underlying = null, detail = "provider returned no images")
        }

        return data.map { item ->
            val b64 = item.jsonObject["b64_json"]?.jsonPrimitive?.contentOrNull
                ?: throw imageGenerationResponseError(responseText, detail = "missing data[].b64_json")
            val bytes = runCatching { Base64.getDecoder().decode(b64) }
                .getOrElse { throw imageGenerationResponseError(responseText, detail = "data[].b64_json is not base64", underlying = it) }
            GeneratedImage(data = bytes, mime = "image/png")
        }
    }

    suspend fun transcribeAudioAsync(
        model: String,
        audio: ByteArray,
        filename: String,
        mimeType: String,
    ): String {
        require(config.apiKey.isNotBlank()) { "INTATIS_API_KEY is required." }
        require(model.isNotBlank()) { "transcription model is required." }
        require(filename.isNotBlank()) { "transcription filename is required." }
        require(mimeType.isNotBlank()) { "transcription mimeType is required." }
        require(audio.isNotEmpty()) { "transcription audio is required." }

        val boundary = "----IntatisBoundary${System.currentTimeMillis()}"

        val requestUrl = URL(config.baseUrl.trimEnd('/') + "/audio/transcriptions")

        val responseText = withContext(Dispatchers.IO) {
            val connection = (requestUrl.openConnection() as HttpURLConnection).apply {
                requestMethod = "POST"
                connectTimeout = 60_000
                readTimeout = 60_000
                doInput = true
                doOutput = true
                setRequestProperty("Authorization", "Bearer ${config.apiKey}")
                setRequestProperty("Content-Type", "multipart/form-data; boundary=$boundary")
                setRequestProperty("Accept", "application/json")
            }

            connection.outputStream.use { out ->
                writeMultipartFormField(out, boundary, "model", model)
                writeMultipartFileField(
                    out = out,
                    boundary = boundary,
                    fieldName = "file",
                    filename = filename.ifBlank { "audio.m4a" },
                    mimeType = mimeType,
                    audioBytes = audio,
                )
                out.write("--$boundary--\r\n".toByteArray())
            }

            val status = connection.responseCode
            val responseStream = if (status in 200..299) connection.inputStream else connection.errorStream
            val body = BufferedReader(InputStreamReader(responseStream)).use(BufferedReader::readText)
            if (status !in 200..299) {
                throw HttpException(status, body)
            }
            connection.disconnect()
            body
        }

        val root = json.parseToJsonElement(responseText).jsonObject
        val text = root["text"]?.jsonPrimitive?.contentOrNull
            ?: throw IllegalStateException("Invalid transcription response: missing text.")
        return text.trim()
    }

    suspend fun transcribeAudioAsync(
        model: String,
        audioPath: String,
        filename: String,
        mimeType: String,
    ): String {
        require(config.apiKey.isNotBlank()) { "INTATIS_API_KEY is required." }
        require(model.isNotBlank()) { "transcription model is required." }
        require(audioPath.isNotBlank()) { "transcription audio path is required." }
        require(filename.isNotBlank()) { "transcription filename is required." }
        require(mimeType.isNotBlank()) { "transcription mimeType is required." }

        val file = File(audioPath)
        require(file.exists()) { "transcription audio file not found: $audioPath" }
        require(file.isFile) { "transcription audio path must be a file: $audioPath" }

        return transcribeAudioAsync(
            model = model,
            audio = file.readBytes(),
            filename = filename.ifBlank { file.name },
            mimeType = mimeType,
        )
    }

    private suspend fun execute(payload: JsonObject): String = executeJson("/chat/completions", payload)

    private suspend fun executeJson(path: String, payload: JsonObject): String = withContext(Dispatchers.IO) {
        val requestUrl = URL(config.baseUrl.trimEnd('/') + path)
        val connection = (requestUrl.openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            connectTimeout = 60_000
            readTimeout = 60_000
            doInput = true
            doOutput = true
            setRequestProperty("Authorization", "Bearer ${config.apiKey}")
            setRequestProperty("Content-Type", "application/json")
            setRequestProperty("Accept", "application/json")
        }

        OutputStreamWriter(connection.outputStream).use { writer ->
            writer.write(payload.toString())
            writer.flush()
        }

        val status = connection.responseCode
        val stream = if (status in 200..299) connection.inputStream else connection.errorStream
        val body = BufferedReader(InputStreamReader(stream)).use(BufferedReader::readText)
        if (status !in 200..299) {
            throw HttpException(status, body)
        }
        connection.disconnect()
        body
    }

    private fun buildMessageRequest(
        messages: List<IntatisMessage>,
        model: String,
        reasoning: String?,
        images: List<ImageAttachment>,
        includeUsage: Boolean,
    ): JsonObject {
        val messagePayload = messages.mapIndexed { index, msg ->
            val msgRole = msg.role.name.lowercase()
            val messageImages = if (index == messages.lastIndex) images else emptyList()
            buildMessagePayload(msgRole, msg.content, messageImages)
        }

        return buildJsonObject {
            put("model", JsonPrimitive(model))
            put("stream", JsonPrimitive(false))
            put("messages", JsonArray(messagePayload))
            reasoning?.let { put("reasoning_effort", JsonPrimitive(it)) }
            if (includeUsage) {
                put("stream_options", buildJsonObject { put("include_usage", JsonPrimitive(true)) })
            }
        }
    }

    private fun buildToolRequest(
        messages: List<OpenAIChatMessage>,
        tools: List<ToolDescriptor>,
        model: String,
        reasoning: String?,
        includeUsage: Boolean,
    ): JsonObject {
        val payload = buildJsonObject {
            put("model", JsonPrimitive(model))
            put("stream", JsonPrimitive(false))
            put("messages", buildJsonArray {
                messages.forEach {
                    add(buildToolMessagePayload(it))
                }
            })
            put("tools", buildJsonArray {
                tools.forEach { add(it.toOpenAiDefinition()) }
            })
            put("tool_choice", JsonPrimitive("auto"))
            reasoning?.let { put("reasoning_effort", JsonPrimitive(it)) }
            if (includeUsage) {
                put("stream_options", buildJsonObject { put("include_usage", JsonPrimitive(true)) })
            }
        }
        return payload
    }

    private fun buildMessagePayload(role: String, content: String, images: List<ImageAttachment>): JsonObject {
        return if (images.isEmpty()) {
            buildJsonObject {
                put("role", JsonPrimitive(role))
                put("content", JsonPrimitive(content))
            }
        } else {
            buildJsonObject {
                put("role", JsonPrimitive(role))
                put("content", buildJsonArray {
                    if (content.isNotBlank()) {
                        add(buildJsonObject {
                            put("type", JsonPrimitive("text"))
                            put("text", JsonPrimitive(content))
                        })
                    }
                    images.forEach { image ->
                        add(buildJsonObject {
                            put("type", JsonPrimitive("image_url"))
                            put("image_url", buildJsonObject {
                                put("url", JsonPrimitive(image.url))
                            })
                        })
                    }
                })
            }
        }
    }

    private fun buildToolMessagePayload(message: OpenAIChatMessage): JsonObject {
        return buildJsonObject {
            put("role", JsonPrimitive(message.role))

            message.toolCalls?.let { calls ->
                put("tool_calls", buildJsonArray {
                    calls.forEach { call ->
                        add(
                            buildJsonObject {
                                put("id", JsonPrimitive(call.id))
                                put("type", JsonPrimitive("function"))
                                put("function", buildJsonObject {
                                    put("name", JsonPrimitive(call.name))
                                    put("arguments", JsonPrimitive(call.arguments))
                                })
                            }
                        )
                    }
                })
                put("content", buildContentPayload(message.content ?: "", emptyList()))
            } ?: run {
                put("content", buildContentPayload(message.content ?: "", message.images))
            }

            message.toolCallId?.let { put("tool_call_id", JsonPrimitive(it)) }
        }
    }

    private fun buildContentPayload(content: String, images: List<ImageAttachment>): JsonElement {
        return if (images.isEmpty()) {
            JsonPrimitive(content)
        } else {
            buildJsonArray {
                if (content.isNotBlank()) {
                    add(
                        buildJsonObject {
                            put("type", JsonPrimitive("text"))
                            put("text", JsonPrimitive(content))
                        },
                    )
                }
                images.forEach { image ->
                    add(
                        buildJsonObject {
                            put("type", JsonPrimitive("image_url"))
                            put("image_url", buildJsonObject {
                                put("url", JsonPrimitive(image.url))
                            })
                        },
                    )
                }
            }
        }
    }

    private fun extractContentText(message: JsonObject): String {
        val content = message["content"] ?: return ""
        return when {
            content is JsonPrimitive && content.isString -> content.contentOrNull ?: ""
            content is kotlinx.serialization.json.JsonArray -> {
                val pieces = content.mapNotNull { entry ->
                    val item = entry.jsonObject
                    if ((item["type"]?.jsonPrimitive?.contentOrNull ?: "") != "text") return@mapNotNull null
                    item["text"]?.jsonPrimitive?.contentOrNull
                }
                pieces.joinToString("")
            }
            else -> content.toString()
        }
    }

    private fun parseToolCalls(message: JsonObject): List<ToolCall> {
        val toolCalls = message["tool_calls"]?.jsonArray ?: return emptyList()
        return toolCalls.mapNotNull { element ->
            if (element !is JsonObject) return@mapNotNull null
            val function = element["function"]?.jsonObject ?: return@mapNotNull null
            val name = function["name"]?.jsonPrimitive?.contentOrNull ?: return@mapNotNull null
            val argsElement = function["arguments"]
            val args = when {
                argsElement == null -> "{}"
                argsElement is JsonPrimitive && argsElement.isString -> argsElement.contentOrNull ?: "{}"
                else -> argsElement.toString()
            }
            val id = element["id"]?.jsonPrimitive?.contentOrNull
                ?: "call_${System.currentTimeMillis()}"
            ToolCall(id, name, args)
        }
    }

    private fun writeMultipartFormField(
        output: java.io.OutputStream,
        boundary: String,
        name: String,
        value: String,
    ) {
        output.write("--$boundary\r\n".toByteArray())
        output.write("Content-Disposition: form-data; name=\"$name\"\r\n".toByteArray())
        output.write("Content-Type: text/plain\r\n\r\n".toByteArray())
        output.write(value.toByteArray())
        output.write("\r\n".toByteArray())
    }

    private fun writeMultipartFileField(
        output: java.io.OutputStream,
        boundary: String,
        fieldName: String,
        filename: String,
        mimeType: String,
        audioBytes: ByteArray,
    ) {
        output.write("--$boundary\r\n".toByteArray())
        output.write("Content-Disposition: form-data; name=\"$fieldName\"; filename=\"$filename\"\r\n".toByteArray())
        output.write("Content-Type: $mimeType\r\n\r\n".toByteArray())
        output.write(audioBytes)
        output.write("\r\n".toByteArray())
    }

    private fun imageGenerationResponseError(
        responseText: String,
        detail: String? = null,
        underlying: Throwable? = null,
    ): IllegalStateException {
        val parts = mutableListOf(
            "image generation returned a response that did not match OpenAI-compatible image JSON with data[].b64_json.",
            "Check endpoint compatibility, provider path, selected model, and response format.",
        )

        imageGenerationProviderMessage(responseText)?.let { parts.add("Provider said: $it") }
            ?: imageGenerationResponsePreview(responseText)?.let { parts.add("Preview: $it") }

        detail?.let { parts.add("Details: $it.") }

        underlying?.let { ex ->
            val raw = ex.message?.trim().orEmpty().take(180)
            if (raw.isNotBlank()) parts.add("Decoder said: $raw")
        }

        return IllegalStateException(parts.joinToString(" "))
    }

    private fun imageGenerationProviderMessage(responseText: String): String? {
        val root = runCatching { json.parseToJsonElement(responseText).jsonObject }
            .getOrNull() ?: return null

        val error = root["error"] as? kotlinx.serialization.json.JsonObject ?: return null
        val pieces = mutableListOf<String>()
        listOf("message", "type", "code", "param").forEach { key ->
            val value = error[key]?.jsonPrimitive?.contentOrNull
            if (!value.isNullOrBlank()) pieces.add(value)
        }
        if (pieces.isNotEmpty()) return pieces.joinToString(" ")

        for (key in listOf("message", "detail", "error_description")) {
            val value = root[key]?.jsonPrimitive?.contentOrNull
            if (!value.isNullOrBlank()) return value
        }

        return null
    }

    private fun imageGenerationResponsePreview(responseText: String): String? {
        val trimmed = responseText.trim()
        if (trimmed.isEmpty()) return null
        val compact = trimmed.replace("\n", " ").replace("\r", " ").replace("\t", " ").trim()
        return if (compact.isEmpty()) null else compact.take(180)
    }
}

class HttpException(statusCode: Int, message: String) : RuntimeException("OpenAI request failed: $statusCode. $message")
