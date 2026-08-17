package com.intatis.shared.providers

import com.intatis.shared.protocol.Jsonx
import com.intatis.shared.protocol.Jsonx.int
import com.intatis.shared.protocol.Jsonx.str
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flowOn
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.util.concurrent.TimeUnit

/**
 * OpenAI-compatible HTTP adapter (chat/completions, SSE streaming). The mobile app is
 * a strict Chat subset per the Apple contract — no tool calling on this surface.
 */
class OpenAIWireProvider(
    private val client: OkHttpClient,
    private val baseUrl: String,
    private val apiKey: String,
    private val chatEndpointOverride: String? = null,
) : ChatProvider {

    val effectiveBaseUrl: String get() = baseUrl.trimEnd('/')

    companion object {
        fun chatCompletionsUrl(baseUrl: String, override: String?): String =
            if (override.isNullOrBlank()) baseUrl.trimEnd('/') + "/chat/completions"
            else override

        fun defaultClient(): OkHttpClient = OkHttpClient.Builder()
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(300, TimeUnit.SECONDS)
            .build()

        internal fun parseUsage(obj: JsonObject): Usage? {
            val prompt = obj.int("prompt_tokens")
            val completion = obj.int("completion_tokens")
            val total = obj.int("total_tokens")
            if (prompt == null && completion == null && total == null) return null
            val cached = (obj["prompt_tokens_details"] as? JsonObject)?.int("cached_tokens")
            return Usage(prompt, cached, completion, total)
        }
    }

    override fun streamChat(request: ChatRequest): Flow<ChatChunk> = flow {
        val url = chatCompletionsUrl(baseUrl, chatEndpointOverride)
        val parsed = java.net.URI(url)
        require(parsed.scheme == "http" || parsed.scheme == "https") { "invalid chat endpoint: $url" }
        require(parsed.host?.isNotEmpty() == true) { "invalid chat endpoint: $url" }

        val body = buildRequestBody(request)
        val httpRequest = Request.Builder()
            .url(url)
            .post(body.toRequestBody("application/json".toMediaType()))
            .header("Accept", "text/event-stream")
            .apply {
                if (apiKey.isNotEmpty()) header("Authorization", "Bearer $apiKey")
            }
            .build()

        client.newCall(httpRequest).execute().use { response ->
            if (!response.isSuccessful) {
                var errorBody = response.body?.string() ?: ""
                if (errorBody.length > 2048) errorBody = errorBody.take(2048)
                throw ProviderException(
                    "provider.http",
                    "HTTP ${response.code}: $errorBody",
                    response.code,
                )
            }

            val source = response.body?.source()
                ?: throw ProviderException("provider.http", "empty response body", response.code)
            val parser = SseParser()
            var finishReasonSeen = false
            var sawDone = false

            // OkHttp reads are blocking; the flow is pinned to IO below.
            val reader = java.io.BufferedReader(source.inputStream().reader(Charsets.UTF_8))
            while (true) {
                val line = reader.readLine() ?: break
                val chunk = if (line.isEmpty()) "" else line + "\n"
                for (event in parser.consume(chunk)) {
                    if (event == "[DONE]") {
                        sawDone = true
                        emit(ChatChunk.Done)
                        return@use
                    }
                    val node = try {
                        Jsonx.lenient.parseToJsonElement(event)
                    } catch (_: Exception) {
                        null
                    } ?: continue

                    val delta = (node as? JsonObject)
                        ?.get("choices")?.let { it as? JsonArray }
                        ?.firstOrNull()?.let { it as? JsonObject }
                        ?.get("delta")?.let { it as? JsonObject }
                    val text = delta?.str("content")
                    if (!text.isNullOrEmpty()) emit(ChatChunk.Delta(text))

                    val finishReason = (node as? JsonObject)
                        ?.get("choices")?.let { it as? JsonArray }
                        ?.firstOrNull()?.let { it as? JsonObject }
                        ?.str("finish_reason")
                    if (!finishReason.isNullOrEmpty()) finishReasonSeen = true

                    (node as? JsonObject)?.get("usage")?.let { it as? JsonObject }?.let { usageObj ->
                        parseUsage(usageObj)?.let { emit(ChatChunk.UsageReport(it)) }
                    }
                }
            }

            if (finishReasonSeen || sawDone) {
                emit(ChatChunk.Done)
            } else {
                throw ProviderException("incomplete_stream", "stream ended without a completion marker")
            }
        }
    }.flowOn(Dispatchers.IO)

    internal fun buildRequestBody(request: ChatRequest): String {
        val body = buildJsonObject {
            put("model", request.model)
            put("messages", buildJsonArray {
                request.messages.forEach { message ->
                    add(messageJson(message))
                }
            })
            put("stream", true)
            request.temperature?.let { put("temperature", it) }
            request.reasoningEffort?.let { put("reasoning_effort", it.wire) }
            if (request.includeUsage) {
                put("stream_options", buildJsonObject { put("include_usage", true) })
            }
        }
        return Jsonx.serializeSorted(body)
    }

    private fun messageJson(message: ChatMessage): JsonObject {
        if (message.images.isNotEmpty() && message.role == "user") {
            return buildJsonObject {
                put("role", message.role)
                put("content", buildJsonArray {
                    if (message.content.isNotEmpty()) {
                        add(buildJsonObject {
                            put("type", "text")
                            put("text", message.content)
                        })
                    }
                    message.images.forEach { image ->
                        add(buildJsonObject {
                            put("type", "image_url")
                            put("image_url", buildJsonObject { put("url", image.url) })
                        })
                    }
                })
            }
        }
        return buildJsonObject {
            put("role", message.role)
            put("content", message.content)
        }
    }
}
