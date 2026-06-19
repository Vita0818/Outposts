package com.intatis.shared.provider

import com.intatis.shared.attachments.ImageAttachment
import com.intatis.shared.model.IntatisConfig
import com.intatis.shared.model.IntatisMessage
import com.intatis.shared.model.MessageRole
import com.intatis.shared.tools.ToolDescriptor
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.put
import java.io.BufferedInputStream
import java.io.BufferedOutputStream
import java.net.HttpURLConnection
import java.net.URL
import java.nio.charset.StandardCharsets

private val json = Json { ignoreUnknownKeys = true }

data class ToolCall(val id: String, val name: String, val arguments: String)

data class OpenAIChatMessage(
    val role: String,
    val content: String? = null,
    val toolCalls: List<ToolCall>? = null,
    val toolCallId: String? = null,
)

class OpenAIClient(private val config: IntatisConfig) {
    data class ChatResult(val text: String, val latencyMs: Long, val usage: String?)
    data class ToolCallResult(val text: String, val toolCalls: List<ToolCall>, val latencyMs: Long, val usage: String?)

    suspend fun sendAsync(
        messages: List<IntatisMessage>,
        model: String?,
        reasoning: String?,
        attachments: List<ImageAttachment> = emptyList(),
    ): ChatResult {
        val finalModel = model?.ifBlank { null } ?: config.model
        val payload = buildJsonObject {
            put("model", finalModel)
            put("stream", false)
            put("messages", buildJsonArray {
                for ((index, message) in messages.withIndex()) {
                    val msgImages = if (message.role == MessageRole.USER && attachments.isNotEmpty() && index == messages.lastIndex) {
                        attachments
                    } else {
                        emptyList()
                    }
                    add(messagePayload(message, msgImages))
                }
            })
            if (!reasoning.isNullOrBlank()) put("reasoning_effort", reasoning)
        }

        val response = post(payload)
        return response.firstChoiceText(config.includeUsage)
            .let { (text, usage) -> ChatResult(text, 0L, usage) }
            .copy(latencyMs = response.latencyMs)
    }

    suspend fun sendWithToolsAsync(
        messages: List<OpenAIChatMessage>,
        tools: List<ToolDescriptor>,
        model: String?,
        reasoning: String?,
        includeUsage: Boolean,
    ): ToolCallResult {
        val finalModel = model?.ifBlank { null } ?: config.model
        val payload = buildJsonObject {
            put("model", finalModel)
            put("stream", false)
            put("messages", buildJsonArray {
                messages.forEach { add(openAiMessagePayload(it)) }
            })
            put("tools", buildJsonArray {
                tools.forEach { tool -> add(json.encodeToJsonElement(tool.toOpenAiDefinition())) }
            })
            put("tool_choice", "auto")
            if (!reasoning.isNullOrBlank()) put("reasoning_effort", reasoning)
            if (includeUsage) put("stream_options", buildJsonObject { put("include_usage", true) })
        }

        val response = post(payload)
        val choice = response.firstChoice()
        return ToolCallResult(
            text = choice.second,
            toolCalls = parseToolCalls(choice.first),
            latencyMs = response.latencyMs,
            usage = response.usage,
        )
    }

    private fun messagePayload(message: IntatisMessage, images: List<ImageAttachment>): JsonObject {
        val role = when (message.role) {
            MessageRole.USER -> "user"
            MessageRole.ASSISTANT -> "assistant"
            MessageRole.SYSTEM -> "system"
            MessageRole.TOOL -> "tool"
            MessageRole.AGENT -> "assistant"
        }

        if (images.isEmpty()) {
            return buildJsonObject {
                put("role", role)
                put("content", message.content)
            }
        }

        val content = buildJsonArray {
            if (message.content.isNotBlank()) {
                add(buildJsonObject {
                    put("type", "text")
                    put("text", message.content)
                })
            }
            images.forEach { image ->
                add(
                    buildJsonObject {
                        put("type", "image_url")
                        put("image_url", buildJsonObject { put("url", image.url) })
                    }
                )
            }
        }

        return buildJsonObject {
            put("role", role)
            put("content", content)
        }
    }

    private fun openAiMessagePayload(message: OpenAIChatMessage): JsonObject {
        val obj = buildJsonObject {
            put("role", message.role)
            if (message.toolCallId != null) {
                put("tool_call_id", message.toolCallId)
            }
            put("content", message.content)
        }

        if (!message.toolCalls.isNullOrEmpty()) {
            obj["tool_calls"]
        }

        if (!message.toolCalls.isNullOrEmpty()) {
            val tc = buildJsonArray {
                message.toolCalls.forEach { call ->
                    add(
                        buildJsonObject {
                            put("id", call.id)
                            put("type", "function")
                            put(
                                "function",
                                buildJsonObject {
                                    put("name", call.name)
                                    put("arguments", call.arguments)
                                }
                            )
                        }
                    )
                }
            }
            return buildJsonObject {
                put("role", message.role)
                if (message.toolCallId != null) put("tool_call_id", message.toolCallId)
                put("tool_calls", tc)
                put("content", message.content)
            }
        }
        return obj
    }

    private fun parseToolCalls(message: JsonObject): List<ToolCall> {
        val calls = message["tool_calls"]?.jsonArray ?: return emptyList()
        val output = mutableListOf<ToolCall>()

        for (node in calls) {
            val toolObj = node.jsonObject
            val fn = toolObj["function"]?.jsonObject ?: continue
            val name = fn["name"]?.jsonPrimitive?.contentOrNull ?: continue
            val args = when (val a = fn["arguments"]) {
                is JsonPrimitive -> a.content
                null -> "{}"
                else -> a.toString()
            }
            val id = toolObj["id"]?.jsonPrimitive?.contentOrNull ?: "call_${output.size}"
            output.add(ToolCall(id, name, args))
        }
        return output
    }

    private data class ProviderResponse(val raw: JsonObject, val latencyMs: Long, val usage: String?)

    private suspend fun post(payload: JsonObject): ProviderResponse {
        val uri = URL(config.baseUrl.trimEnd('/') + "/chat/completions")
        val connection = (uri.openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            doOutput = true
            connectTimeout = 120_000
            readTimeout = 120_000
            setRequestProperty("Content-Type", "application/json")
            setRequestProperty("Authorization", "Bearer ${config.apiKey}")
        }

        val body = json.encodeToString(payload)
        connection.outputStream.use { out ->
            BufferedOutputStream(out).write(body.toByteArray(StandardCharsets.UTF_8))
        }

        val started = System.currentTimeMillis()
        val responseText = if (connection.responseCode in 200..299) {
            BufferedInputStream(connection.inputStream).reader(Charset.forName("UTF-8")).readText()
        } else {
            BufferedInputStream(connection.errorStream ?: connection.inputStream).reader(Charset.forName("UTF-8")).readText()
                .let { throw IllegalStateException("OpenAI request failed: ${connection.responseCode} ${connection.responseMessage}. $it") }
        }
        val elapsed = System.currentTimeMillis() - started
        return ProviderResponse(json.parseToJsonElement(responseText).jsonObject, elapsed, rawUsage(responseText))
    }

    private fun rawUsage(body: String): String? {
        return runCatching {
            val root = json.parseToJsonElement(body).jsonObject
            root["usage"]?.toString()
        }.getOrNull()
    }
}

private fun ProviderResponse.firstChoice(): Pair<JsonObject, String> = firstChoiceText(false)

private fun ProviderResponse.firstChoiceText(includeUsage: Boolean): Pair<JsonObject, String> {
    val choices = raw["choices"]?.jsonArray ?: throw IllegalStateException("Empty response from provider")
    if (choices.isEmpty()) throw IllegalStateException("Empty response from provider")
    val firstChoice = choices[0].jsonObject
    val msg = firstChoice["message"]!!.jsonObject
    val text = extractContentText(msg)
    return msg to text
}

private fun extractContentText(message: JsonObject): String {
    val content = message["content"] ?: return ""
    return when (content) {
        is JsonArray -> content.joinToString("") { item ->
            val obj = item.jsonObject
            if (obj["type"]?.jsonPrimitive?.contentOrNull == "text") {
                obj["text"]?.jsonPrimitive?.contentOrNull.orEmpty()
            } else ""
        }
        else -> content.jsonPrimitive.contentOrNull.orEmpty()
    }
}
