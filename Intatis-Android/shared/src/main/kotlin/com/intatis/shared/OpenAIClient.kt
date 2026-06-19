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
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import java.io.BufferedReader
import java.io.InputStreamReader
import java.io.OutputStreamWriter
import java.net.HttpURLConnection
import java.net.URL
import kotlin.math.max

class OpenAIClient(private val config: IntatisConfig) {
    private val json = Json { ignoreUnknownKeys = true }

    data class ToolCall(val id: String, val name: String, val arguments: String)

    data class OpenAIChatMessage(
        val role: String,
        val content: String? = null,
        val toolCalls: List<ToolCall>? = null,
        val toolCallId: String? = null,
    )

    suspend fun sendAsync(
        messages: List<IntatisMessage>,
        model: String? = null,
        reasoning: String? = null,
        attachments: List<ImageAttachment>? = null,
    ): Triple<String, Long, String?> {
        require(config.apiKey.isNotBlank()) { "INTATIS_API_KEY is required." }

        val resolvedModel = if (model.isNullOrBlank()) config.model else model
        val normalizedImages = attachments?.filterIsInstance<ImageAttachment>() ?: emptyList()
        val request = buildMessageRequest(messages, resolvedModel, reasoning, normalizedImages)

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
    ): Triple<String, List<ToolCall>, Long> {
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

        return Triple(content, calls, 0L)
    }

    private suspend fun execute(payload: JsonObject): String = withContext(Dispatchers.IO) {
        val requestUrl = URL(config.baseUrl.trimEnd('/') + "/chat/completions")
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
                put("content", JsonPrimitive(message.content ?: ""))
            } ?: run {
                put("content", JsonPrimitive(message.content ?: ""))
            }

            message.toolCallId?.let { put("tool_call_id", JsonPrimitive(it)) }
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
}

class HttpException(statusCode: Int, message: String) : RuntimeException("OpenAI request failed: $statusCode. $message")
