package com.intatis.shared.provider

import com.intatis.shared.ToolDescriptor
import com.intatis.shared.attachments.ImageAttachment
import com.intatis.shared.model.IntatisConfig
import com.intatis.shared.model.IntatisMessage
import com.intatis.shared.model.MessageRole
import kotlinx.coroutines.TimeoutCancellationException
import kotlinx.coroutines.withContext
import kotlinx.coroutines.withTimeout
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import java.io.BufferedInputStream
import java.io.BufferedOutputStream
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL
import java.nio.charset.StandardCharsets
import java.util.concurrent.TimeoutException

private val json = Json { ignoreUnknownKeys = true }

data class ToolCall(val id: String, val name: String, val arguments: String)

private const val HealthCheckPrompt = "Reply with OK."
private const val HealthCheckPreviewChars = 160

data class OpenAIChatMessage(
    val role: String,
    val content: String? = null,
    val toolCalls: List<ToolCall>? = null,
    val toolCallId: String? = null,
)

class OpenAIClient(private val config: IntatisConfig) {
    data class ChatResult(val text: String, val latencyMs: Long, val usage: String?)
    data class ToolCallResult(val text: String, val toolCalls: List<ToolCall>, val latencyMs: Long, val usage: String?)

    private val runtimePolicy = ProviderRuntimePolicy.Streaming

    suspend fun sendAsync(
        messages: List<IntatisMessage>,
        model: String?,
        reasoning: String?,
        attachments: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
    ): ChatResult {
        if (config.apiKey.isBlank()) {
            throw IllegalStateException("INTATIS_API_KEY is required.")
        }

        val finalModel = model?.ifBlank { null } ?: config.model
        val request = buildJsonObject {
            put("model", finalModel)
            put("stream", true)
            put("messages", buildJsonArray {
                for ((index, message) in messages.withIndex()) {
                    val messageImages = if (message.role == MessageRole.USER && attachments.isNotEmpty() && index == messages.lastIndex) {
                        attachments
                    } else {
                        emptyList()
                    }
                    add(messagePayload(message, messageImages))
                }
            })
            if (!reasoning.isNullOrBlank()) put("reasoning_effort", reasoning)
            if (includeUsage) put("stream_options", buildJsonObject { put("include_usage", true) })
        }

        val response = sendStreamingChatWithPolicy(request, "streaming request")
        return ChatResult(response.text, response.latencyMs, response.usage)
    }

    suspend fun sendWithToolsAsync(
        messages: List<OpenAIChatMessage>,
        tools: List<ToolDescriptor>,
        model: String?,
        reasoning: String?,
        includeUsage: Boolean,
    ): ToolCallResult {
        if (config.apiKey.isBlank()) {
            throw IllegalStateException("INTATIS_API_KEY is required.")
        }

        val finalModel = model?.ifBlank { null } ?: config.model
        val request = buildJsonObject {
            put("model", finalModel)
            put("stream", true)
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

        val response = sendStreamingToolsWithPolicy(request, "tool-calling streaming request")
        return ToolCallResult(
            text = response.text,
            toolCalls = response.toolCalls,
            latencyMs = response.latencyMs,
            usage = response.usage,
        )
    }

    suspend fun checkChatHealthAsync(
        model: String?,
        reasoning: String?,
        includeUsage: Boolean = true,
    ): ProviderHealthCheckResult {
        val resolvedModel = model?.ifBlank { null } ?: config.model
        val response = sendAsync(
            messages = listOf(IntatisMessage(MessageRole.USER, HealthCheckPrompt)),
            model = resolvedModel,
            reasoning = reasoning,
            includeUsage = includeUsage,
        )

        val trimmed = response.text.trim()
        val preview = trimmed.takeIf { it.isNotBlank() }?.let {
            normalizeHealthCheckPreview(trimmed)
        }
        return ProviderHealthCheckResult(
            providerId = "openai",
            role = "chat",
            model = resolvedModel,
            isHealthy = trimmed.isNotBlank(),
            latency = Duration.ofMillis(response.latencyMs),
            message = if (trimmed.isNotBlank()) "provider check passed" else "provider returned empty response",
            responsePreview = preview,
        )
    }

    suspend fun checkAgentToolHealthAsync(
        model: String?,
        reasoning: String?,
        includeUsage: Boolean = true,
    ): ProviderHealthCheckResult {
        val resolvedModel = model?.ifBlank { null } ?: config.model
        val response = sendWithToolsAsync(
            messages = listOf(OpenAIChatMessage("user", HealthCheckPrompt)),
            tools = emptyList(),
            model = resolvedModel,
            reasoning = reasoning,
            includeUsage = includeUsage,
        )

        val toolCallsReturned = response.toolCalls.isNotEmpty()
        val trimmed = response.text.trim()
        val hasText = trimmed.isNotBlank()
        val preview = trimmed.takeIf { it.isNotBlank() }?.let { normalizeHealthCheckPreview(trimmed) }

        val isHealthy = !toolCallsReturned && hasText
        return ProviderHealthCheckResult(
            providerId = "openai",
            role = "agent",
            model = resolvedModel,
            isHealthy = isHealthy,
            latency = Duration.ofMillis(response.latencyMs),
            message = when {
                toolCallsReturned -> "provider returned tool calls during agent health check"
                !hasText -> "provider returned empty response"
                else -> "provider check passed"
            },
            responsePreview = preview,
        )
    }

    private suspend fun sendStreamingChatWithPolicy(payload: JsonObject, operation: String): StreamingResponse {
        var attempt = 1
        while (true) {
            var receivedResponseBytes = false
            try {
                val started = System.currentTimeMillis()
                val response = withTimeout(runtimePolicy.requestTimeoutMillis) {
                    performStreamingChatRequest(payload, operation) {
                        receivedResponseBytes = true
                    }
                }
                return response.copy(latencyMs = System.currentTimeMillis() - started)
            } catch (timeout: TimeoutCancellationException) {
                val timeoutError = TimeoutException(
                    "Request timed out after ${ProviderErrorFormatting.formatSeconds(runtimePolicy.requestTimeoutSeconds)} during $operation."
                )
                if (!ProviderRuntime.shouldRetry(timeoutError, attempt, runtimePolicy, receivedResponseBytes)) {
                    throw ProviderRuntime.exhausted(timeoutError, attempt, operation)
                }

                attempt += 1
                ProviderRuntime.sleepBeforeRetry(
                    nextAttempt = attempt,
                    policy = runtimePolicy,
                    retryHint = ProviderRuntime.retryHintFromError(timeoutError),
                )
            } catch (error: Exception) {
                if (!ProviderRuntime.shouldRetry(error, attempt, runtimePolicy, receivedResponseBytes)) {
                    throw ProviderRuntime.exhausted(error, attempt, operation)
                }

                attempt += 1
                ProviderRuntime.sleepBeforeRetry(
                    nextAttempt = attempt,
                    policy = runtimePolicy,
                    retryHint = ProviderRuntime.retryHintFromError(error),
                )
            }
        }
    }

    private suspend fun sendStreamingToolsWithPolicy(payload: JsonObject, operation: String): StreamingToolResponse {
        var attempt = 1
        while (true) {
            var receivedResponseBytes = false
            try {
                val started = System.currentTimeMillis()
                val response = withTimeout(runtimePolicy.requestTimeoutMillis) {
                    performStreamingToolRequest(payload, operation) {
                        receivedResponseBytes = true
                    }
                }
                return response.copy(latencyMs = System.currentTimeMillis() - started)
            } catch (timeout: TimeoutCancellationException) {
                val timeoutError = TimeoutException(
                    "Request timed out after ${ProviderErrorFormatting.formatSeconds(runtimePolicy.requestTimeoutSeconds)} during $operation."
                )
                if (!ProviderRuntime.shouldRetry(timeoutError, attempt, runtimePolicy, receivedResponseBytes)) {
                    throw ProviderRuntime.exhausted(timeoutError, attempt, operation)
                }

                attempt += 1
                ProviderRuntime.sleepBeforeRetry(
                    nextAttempt = attempt,
                    policy = runtimePolicy,
                    retryHint = ProviderRuntime.retryHintFromError(timeoutError),
                )
            } catch (error: Exception) {
                if (!ProviderRuntime.shouldRetry(error, attempt, runtimePolicy, receivedResponseBytes)) {
                    throw ProviderRuntime.exhausted(error, attempt, operation)
                }

                attempt += 1
                ProviderRuntime.sleepBeforeRetry(
                    nextAttempt = attempt,
                    policy = runtimePolicy,
                    retryHint = ProviderRuntime.retryHintFromError(error),
                )
            }
        }
    }

    private suspend fun performStreamingChatRequest(
        payload: JsonObject,
        operation: String,
        onResponseBytes: () -> Unit,
    ): StreamingResponse = withContext(kotlinx.coroutines.Dispatchers.IO) {
        val uri = URL(config.baseUrl.trimEnd('/') + "/chat/completions")
        val timeoutMs = runtimePolicy.requestTimeoutMillis.toInt()
        val connection = (uri.openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            doInput = true
            doOutput = true
            connectTimeout = timeoutMs
            readTimeout = timeoutMs
            setRequestProperty("Content-Type", "application/json")
            setRequestProperty("Accept", "text/event-stream")
            setRequestProperty("Authorization", "Bearer ${config.apiKey}")
        }

        try {
            val body = json.encodeToString(payload)
            connection.outputStream.use { stream ->
                BufferedOutputStream(stream).use {
                    it.write(body.toByteArray(StandardCharsets.UTF_8))
                }
            }

            val status = connection.responseCode
            val responseStream = if (status in 200..299) {
                connection.inputStream
            } else {
                connection.errorStream ?: connection.inputStream
            }
            if (status !in 200..299) {
                val errorBody = responseStream.use {
                    InputStreamReader(BufferedInputStream(it), StandardCharsets.UTF_8).readText()
                }
                val headers = connection.headerFields
                    .asSequence()
                    .mapNotNull { (key, values) ->
                        if (key.isNullOrBlank() || values.isNullOrEmpty()) return@mapNotNull null
                        key to values.joinToString(",")
                    }
                    .toMap()
                throw ProviderErrorFormatting.httpStatus(status, errorBody, headers, operation)
            }

            val parser = SseEventParser()
            val text = StringBuilder()
            var usage: String? = null
            var sawCompletion = false

            InputStreamReader(BufferedInputStream(responseStream), StandardCharsets.UTF_8).use { responseReader ->
                BufferedReader(responseReader).use { reader ->
                    while (true) {
                        val rawLine = reader.readLine() ?: break
                        onResponseBytes()

                        for (event in parser.consume(rawLine)) {
                            val result = parseChatPayload(event, text, operation)
                            usage = result.usage ?: usage
                            if (result.sawCompletion) sawCompletion = true
                        }
                    }

                    for (event in parser.flush()) {
                        val result = parseChatPayload(event, text, operation)
                        usage = result.usage ?: usage
                        if (result.sawCompletion) sawCompletion = true
                    }
                }
            }

            if (!sawCompletion) {
                throw IllegalStateException(
                    "The streaming request ended before a completion marker. Check endpoint compatibility."
                )
            }

            StreamingResponse(text.toString(), usage)
        } finally {
            connection.disconnect()
        }
    }

    private suspend fun performStreamingToolRequest(
        payload: JsonObject,
        operation: String,
        onResponseBytes: () -> Unit,
    ): StreamingToolResponse = withContext(kotlinx.coroutines.Dispatchers.IO) {
        val uri = URL(config.baseUrl.trimEnd('/') + "/chat/completions")
        val timeoutMs = runtimePolicy.requestTimeoutMillis.toInt()
        val connection = (uri.openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            doInput = true
            doOutput = true
            connectTimeout = timeoutMs
            readTimeout = timeoutMs
            setRequestProperty("Content-Type", "application/json")
            setRequestProperty("Accept", "text/event-stream")
            setRequestProperty("Authorization", "Bearer ${config.apiKey}")
        }

        try {
            val body = json.encodeToString(payload)
            connection.outputStream.use { stream ->
                BufferedOutputStream(stream).use {
                    it.write(body.toByteArray(StandardCharsets.UTF_8))
                }
            }

            val status = connection.responseCode
            val responseStream = if (status in 200..299) {
                connection.inputStream
            } else {
                connection.errorStream ?: connection.inputStream
            }
            if (status !in 200..299) {
                val errorBody = responseStream.use {
                    InputStreamReader(BufferedInputStream(it), StandardCharsets.UTF_8).readText()
                }
                val headers = connection.headerFields
                    .asSequence()
                    .mapNotNull { (key, values) ->
                        if (key.isNullOrBlank() || values.isNullOrEmpty()) return@mapNotNull null
                        key to values.joinToString(",")
                    }
                    .toMap()
                throw ProviderErrorFormatting.httpStatus(status, errorBody, headers, operation)
            }

            val parser = SseEventParser()
            val text = StringBuilder()
            var usage: String? = null
            var sawCompletion = false
            var finishReason: String? = null
            val toolAccumulators = linkedMapOf<ToolCallKey, ToolCallAccumulator>()

            InputStreamReader(BufferedInputStream(responseStream), StandardCharsets.UTF_8).use { responseReader ->
                BufferedReader(responseReader).use { reader ->
                    while (true) {
                        val rawLine = reader.readLine() ?: break
                        onResponseBytes()

                        for (event in parser.consume(rawLine)) {
                            val result = parseToolPayload(event, text, toolAccumulators, finishReason, operation)
                            usage = result.usage ?: usage
                            if (result.sawCompletion) sawCompletion = true
                            result.finishReason?.let { finishReason = it }
                        }
                    }

                    for (event in parser.flush()) {
                        val result = parseToolPayload(event, text, toolAccumulators, finishReason, operation)
                        usage = result.usage ?: usage
                        if (result.sawCompletion) sawCompletion = true
                        result.finishReason?.let { finishReason = it }
                    }
                }
            }

            if (!sawCompletion) {
                throw IllegalStateException(
                    "The tool-calling streaming request ended before a completion marker. Check endpoint compatibility."
                )
            }

            val toolCalls = collectToolCalls(toolAccumulators, finishReason, operation)
            StreamingToolResponse(text.toString(), usage, 0L, toolCalls)
        } finally {
            connection.disconnect()
        }
    }

    private fun parseChatPayload(
        payload: String,
        text: StringBuilder,
        operation: String,
    ): StreamingPayloadResult {
        if (payload == "[DONE]") return StreamingPayloadResult(null, true)

        val trimmed = payload.trim()
        if (trimmed.isBlank()) return StreamingPayloadResult(null, false)

        val root = parseStreamPayload(trimmed, operation)
        if (root is JsonObject) {
            val streamError = parseStreamErrorPayload(root, operation)
            if (streamError != null) throw streamError

            val usage = root["usage"]?.toString()
            val choicesElement = root["choices"]
            if (choicesElement !is JsonArray) return StreamingPayloadResult(usage, false)

            for (choiceElement in choicesElement) {
                if (choiceElement !is JsonObject) {
                    continue
                }
                val choice = choiceElement
                val finishReason = choice["finish_reason"]?.jsonPrimitive?.contentOrNull
                if (!finishReason.isNullOrBlank()) {
                    return StreamingPayloadResult(usage, true)
                }

                val delta = choice["delta"]?.jsonObject ?: continue
                val content = delta["content"]?.jsonPrimitive?.contentOrNull
                if (!content.isNullOrBlank()) {
                    text.append(content)
                }
            }

            return StreamingPayloadResult(usage, false)
        }

        throw IllegalStateException("The provider returned a malformed SSE payload during $operation.")
    }

    private fun parseToolPayload(
        payload: String,
        text: StringBuilder,
        accumulators: MutableMap<ToolCallKey, ToolCallAccumulator>,
        previousFinishReason: String?,
        operation: String,
    ): StreamingToolPayloadResult {
        if (payload == "[DONE]") return StreamingToolPayloadResult(null, true, previousFinishReason)

        val trimmed = payload.trim()
        if (trimmed.isBlank()) return StreamingToolPayloadResult(null, false, previousFinishReason)

        val root = parseStreamPayload(trimmed, operation)
        if (root !is JsonObject) {
            throw IllegalStateException("The provider returned a malformed SSE payload during $operation.")
        }

        val streamError = parseStreamErrorPayload(root, operation)
        if (streamError != null) throw streamError

        var nextFinishReason: String? = previousFinishReason
        var sawCompletion = false
        var usage = root["usage"]?.toString()

        val choicesElement = root["choices"]
        if (choicesElement !is JsonArray) return StreamingToolPayloadResult(usage, false, nextFinishReason)

        for ((choiceOffset, choiceElement) in choicesElement.withIndex()) {
            if (choiceElement !is JsonObject) {
                continue
            }
            val choice = choiceElement
            val choiceIndex = parseJsonInt(choice, "index") ?: choiceOffset
            val finishReason = choice["finish_reason"]?.jsonPrimitive?.contentOrNull
            if (!finishReason.isNullOrBlank()) {
                nextFinishReason = preferredToolFinishReason(nextFinishReason, finishReason)
                sawCompletion = true
            }

            val delta = choice["delta"]?.jsonObject ?: continue
            delta["content"]?.jsonPrimitive?.contentOrNull?.let { text.append(it) }

            val toolCallsElement = delta["tool_calls"]
            if (toolCallsElement !is JsonArray) continue
            val toolCalls = toolCallsElement
            for ((toolOffset, toolElement) in toolCalls.withIndex()) {
                if (toolElement !is JsonObject) {
                    continue
                }
                val tool = toolElement
                val toolIndex = parseJsonInt(tool, "index") ?: toolOffset
                val key = ToolCallKey(choiceIndex, toolIndex)
                val accumulator = accumulators[key] ?: ToolCallAccumulator().also { accumulators[key] = it }

                tool["id"]?.jsonPrimitive?.contentOrNull?.let { accumulator.id = it }

                val functionEl = tool["function"]?.jsonObject
                if (functionEl != null) {
                    functionEl["name"]?.jsonPrimitive?.contentOrNull?.let { accumulator.name = it }
                    functionEl["arguments"]?.let { arguments ->
                        accumulator.arguments += toolCallArgumentsToString(arguments)
                    }
                }
            }
        }

        return StreamingToolPayloadResult(usage, sawCompletion, nextFinishReason)
    }

    private fun collectToolCalls(
        accumulators: Map<ToolCallKey, ToolCallAccumulator>,
        finishReason: String?,
        operation: String,
    ): List<ToolCall> {
        if (finishReason.isNullOrBlank()) return emptyList()
        if (finishReason != "tool_calls" && finishReason != "function_call") return emptyList()

        if (accumulators.isEmpty()) {
            throw IllegalStateException(
                "The provider tool-call stream was incomplete. The provider ended with '$finishReason' but did not emit any tool call deltas during $operation."
            )
        }

        val sortedKeys = accumulators.keys
            .sortedWith(compareBy({ it.choiceIndex }, { it.toolIndex }))

        val missingName = sortedKeys
            .filter { accumulators[it]?.name.isNullOrBlank() == true }
            .joinToString(", ") { "${it.choiceIndex}:${it.toolIndex}" }
        if (missingName.isNotBlank()) {
            throw IllegalStateException(
                "The provider tool-call stream was incomplete. The provider omitted tool names for choice/tool index $missingName."
            )
        }

        return sortedKeys.map { key ->
            val accumulator = accumulators.getValue(key)
            val arguments = validateToolCallArguments(accumulator.arguments, key, finishReason)
            val id = if (accumulator.id.isBlank()) {
                if (key.choiceIndex == 0) "call_${key.toolIndex}" else "call_${key.choiceIndex}_${key.toolIndex}"
            } else {
                accumulator.id
            }
            ToolCall(id, accumulator.name, arguments)
        }
    }

    private fun parseStreamErrorPayload(root: JsonObject, operation: String): Exception? {
        val error = root["error"] as? JsonObject ?: return null
        val messageParts = mutableListOf<String>()
        for (candidate in listOf("message", "type", "code", "param")) {
            error[candidate]?.jsonPrimitive?.contentOrNull?.let { value ->
                if (value.isNotBlank()) messageParts.add(value)
            }
        }
        val message = if (messageParts.isEmpty()) "provider returned an error" else messageParts.joinToString(" ")
        return IllegalStateException("Provider stream returned an error during $operation. $message")
    }

    private fun parseStreamPayload(payload: String, operation: String): kotlinx.serialization.json.JsonElement {
        return try {
            json.parseToJsonElement(payload)
        } catch (ex: Exception) {
            throw IllegalStateException(
                "The provider returned non-JSON SSE data during $operation. Check endpoint compatibility. Payload: $payload",
                ex
            )
        }
    }

    private fun parseJsonInt(root: JsonObject, key: String): Int? {
        val element = root[key] as? JsonPrimitive ?: return null
        return if (element.isString) {
            element.content.toIntOrNull()
        } else {
            element.intOrNull
        }
    }

    private fun toolCallArgumentsToString(arguments: kotlinx.serialization.json.JsonElement): String {
        return when (arguments) {
            is JsonPrimitive -> arguments.contentOrNull ?: ""
            else -> arguments.toString()
        }
    }

    private fun validateToolCallArguments(arguments: String, key: ToolCallKey, finishReason: String): String {
        if (arguments.isBlank()) return arguments
        val trimmed = arguments.trim()
        if (trimmed.isEmpty()) return arguments

        try {
            json.parseToJsonElement(trimmed)
            return arguments
        } catch (ex: Exception) {
            throw IllegalStateException(
                "The provider finished with $finishReason but emitted invalid JSON arguments for choice/tool index ${key.choiceIndex}:${key.toolIndex}."
            )
        }
    }

    private fun preferredToolFinishReason(current: String?, candidate: String): String {
        if (candidate == "tool_calls" || candidate == "function_call") return candidate
        return current ?: candidate
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
                add(
                    buildJsonObject {
                        put("type", "text")
                        put("text", message.content)
                    }
                )
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

    private fun openAiMessagePayload(message: OpenAIChatMessage): JsonObject = buildJsonObject {
        put("role", message.role)
        if (message.toolCallId != null) {
            put("tool_call_id", message.toolCallId)
        }
        put("content", message.content)
        if (!message.toolCalls.isNullOrEmpty()) {
            put(
                "tool_calls",
                buildJsonArray {
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
                                    },
                                )
                            }
                        )
                    }
                },
            )
        }
    }

    private fun parseToolCalls(message: JsonObject): List<ToolCall> {
        val calls = mutableListOf<ToolCall>()
        val toolCalls = message["tool_calls"]?.jsonArray ?: return calls

        for (node in toolCalls) {
            if (node !is JsonObject) continue
            val function = node["function"]?.jsonObject ?: continue
            val name = function["name"]?.jsonPrimitive?.contentOrNull ?: continue
            val arguments = when (val rawArguments = function["arguments"]) {
                is JsonPrimitive -> rawArguments.contentOrNull ?: "{}"
                null -> "{}"
                else -> rawArguments.toString()
            }
            val id = node["id"]?.jsonPrimitive?.contentOrNull ?: "call_${calls.size}"
            calls.add(ToolCall(id, name, arguments))
        }

        return calls
    }

    private data class StreamingResponse(val text: String, val usage: String?, val latencyMs: Long = 0L)
    private data class StreamingToolResponse(val text: String, val usage: String?, val latencyMs: Long = 0L, val toolCalls: List<ToolCall>)
    private data class StreamingPayloadResult(val usage: String?, val sawCompletion: Boolean)
    private data class StreamingToolPayloadResult(val usage: String?, val sawCompletion: Boolean, val finishReason: String?)

    private class SseEventParser {
        private val lines: MutableList<String> = mutableListOf()

        fun consume(rawLine: String): List<String> {
            val line = rawLine.trimEnd('\r')
            if (line.isEmpty()) {
                if (lines.isEmpty()) return emptyList()
                val payload = lines.joinToString("\n")
                lines.clear()
                return listOf(payload)
            }

            if (line.startsWith("data:")) {
                var value = line.substring("data:".length)
                if (value.startsWith(" ")) value = value.drop(1)
                lines.add(value)
            }

            return emptyList()
        }

        fun flush(): List<String> {
            if (lines.isEmpty()) return emptyList()
            val payload = lines.joinToString("\n")
            lines.clear()
            return listOf(payload)
        }
    }

    private data class ToolCallKey(val choiceIndex: Int, val toolIndex: Int) : Comparable<ToolCallKey> {
        override fun compareTo(other: ToolCallKey): Int {
            if (choiceIndex != other.choiceIndex) return choiceIndex.compareTo(other.choiceIndex)
            return toolIndex.compareTo(other.toolIndex)
        }
    }

    private class ToolCallAccumulator {
        var id: String = ""
        var name: String = ""
        var arguments: String = ""
    }
}

private fun normalizeHealthCheckPreview(value: String): String =
    value.trim().replace("\n", " ").replace("\t", " ").take(HealthCheckPreviewChars)
