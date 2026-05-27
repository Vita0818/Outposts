package com.rokurics.app.ui.chat

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL

sealed class AIError(message: String) : Exception(message) {
    class ProviderNotConfigured(name: String) : AIError("$name 未配置")
    object InvalidEndpoint : AIError("Endpoint 无效")
    class RequestFailed(statusCode: Int) : AIError("AI 请求失败：HTTP $statusCode")
    object EmptyResponse : AIError("AI 没有返回内容")
    object NetworkUnavailable : AIError("网络不可用")
}

interface IPhoneChatProvider {
    val displayName: String
    suspend fun send(
        messages: List<ChatMessage>,
        context: ChatContext?
    ): ChatMessage
}

data class ChatMessage(
    val role: ChatMessageRole,
    val content: String
)

enum class ChatMessageRole(val rawValue: String) {
    SYSTEM("system"),
    USER("user"),
    ASSISTANT("assistant")
}

data class ChatContext(
    val id: String,
    val pathDisplay: String,
    val itemCount: Int,
    val totalCharacterCount: Int,
    val formattedContext: String,
    val displayTitle: String
)

class OpenAICompatibleChatProvider(
    val preset: AIProviderPreset,
    val configuration: OpenAIConfiguration
) : IPhoneChatProvider {
    override val displayName = "OpenAI-compatible"

    override suspend fun send(
        messages: List<ChatMessage>,
        context: ChatContext?
    ): ChatMessage = withContext(Dispatchers.IO) {
        if (configuration.trimmedBaseURLString.isEmpty() || configuration.trimmedModelName.isEmpty()) {
            throw AIError.ProviderNotConfigured(preset.displayName)
        }

        val messagesPayload = JSONArray()
        messagesPayload.put(JSONObject().apply {
            put("role", "system")
            put("content", systemPrompt(context))
        })
        messages.forEach { msg ->
            messagesPayload.put(JSONObject().apply {
                put("role", msg.role.rawValue)
                put("content", msg.content)
            })
        }

        val body = JSONObject().apply {
            put("model", configuration.trimmedModelName)
            put("messages", messagesPayload)
            put("temperature", configuration.temperature)
            put("max_tokens", configuration.maxTokens)
            put("stream", false)
        }

        val responseData = sendJSON(
            baseURLString = configuration.trimmedBaseURLString,
            path = "chat/completions",
            apiKey = configuration.trimmedAPIKey,
            body = body.toString()
        )

        val response = JSONObject(responseData)
        val content = response.getJSONArray("choices")
            .getJSONObject(0)
            .getJSONObject("message")
            .optString("content", "").trim()

        if (content.isEmpty()) throw AIError.EmptyResponse
        ChatMessage(role = ChatMessageRole.ASSISTANT, content = content)
    }

    private fun systemPrompt(context: ChatContext?): String {
        var prompt = "你是 Rokurics 的学习助手。回答必须基于用户显式导入的学习库上下文；上下文不足时请直接说明。"
        if (context != null && context.formattedContext.isNotEmpty()) {
            prompt += "\n\n已导入上下文：${context.pathDisplay} · ${context.itemCount} 项\n\n${context.formattedContext}"
        }
        return prompt
    }

    private fun sendJSON(baseURLString: String, path: String, apiKey: String, body: String): String {
        val normalizedBase = baseURLString.trimEnd('/')
        val url = URL("$normalizedBase/$path")
        val connection = url.openConnection() as HttpURLConnection
        connection.apply {
            requestMethod = "POST"
            doOutput = true
            connectTimeout = 30000
            readTimeout = 120000
            setRequestProperty("Content-Type", "application/json")
            if (apiKey.isNotEmpty()) {
                setRequestProperty("Authorization", "Bearer $apiKey")
            }
        }

        try {
            connection.outputStream.use { it.write(body.toByteArray(Charsets.UTF_8)) }

            val code = connection.responseCode
            if (code !in 200..299) {
                throw AIError.RequestFailed(code)
            }
            val responseText = connection.inputStream.bufferedReader().readText()
            if (responseText.isEmpty()) throw AIError.EmptyResponse
            return responseText
        } catch (e: AIError) {
            throw e
        } catch (e: Exception) {
            throw AIError.NetworkUnavailable
        } finally {
            connection.disconnect()
        }
    }
}

class AnthropicChatProvider(
    val configuration: AnthropicConfiguration
) : IPhoneChatProvider {
    override val displayName = "Claude / Anthropic"

    override suspend fun send(
        messages: List<ChatMessage>,
        context: ChatContext?
    ): ChatMessage = withContext(Dispatchers.IO) {
        if (configuration.trimmedBaseURLString.isEmpty() ||
            configuration.trimmedModelName.isEmpty() ||
            configuration.trimmedAPIKey.isEmpty()
        ) {
            throw AIError.ProviderNotConfigured(displayName)
        }

        var systemPrompt = "你是 Rokurics 的学习助手。回答必须基于用户显式导入的学习库上下文；上下文不足时请直接说明。"
        if (context != null && context.formattedContext.isNotEmpty()) {
            systemPrompt += "\n\n已导入上下文：${context.pathDisplay} · ${context.itemCount} 项\n\n${context.formattedContext}"
        }

        // Collect any explicit system messages into the system prompt
        val systemMessages = messages.filter { it.role == ChatMessageRole.SYSTEM }
        if (systemMessages.isNotEmpty()) {
            systemPrompt += "\n\n" + systemMessages.joinToString("\n") { it.content }
        }

        val messagesArray = JSONArray()
        messages.filter { it.role != ChatMessageRole.SYSTEM }.forEach { msg ->
            messagesArray.put(JSONObject().apply {
                put("role", msg.role.rawValue)
                put("content", msg.content)
            })
        }

        val body = JSONObject().apply {
            put("model", configuration.trimmedModelName)
            put("max_tokens", configuration.maxTokens)
            put("temperature", configuration.temperature)
            put("system", systemPrompt)
            put("messages", messagesArray)
        }

        val responseData = sendJSON(body.toString())
        val response = JSONObject(responseData)
        val contentBlocks = response.getJSONArray("content")
        val texts = mutableListOf<String>()
        for (i in 0 until contentBlocks.length()) {
            val block = contentBlocks.getJSONObject(i)
            if (block.optString("type") == "text") {
                block.optString("text")?.let { texts.add(it) }
            }
        }
        val content = texts.joinToString("\n").trim()
        if (content.isEmpty()) throw AIError.EmptyResponse
        ChatMessage(role = ChatMessageRole.ASSISTANT, content = content)
    }

    private fun sendJSON(body: String): String {
        val normalizedBase = configuration.trimmedBaseURLString.trimEnd('/')
        val url = URL("$normalizedBase/v1/messages")
        val connection = url.openConnection() as HttpURLConnection
        connection.apply {
            requestMethod = "POST"
            doOutput = true
            connectTimeout = 30000
            readTimeout = 120000
            setRequestProperty("Content-Type", "application/json")
            setRequestProperty("x-api-key", configuration.trimmedAPIKey)
            setRequestProperty("anthropic-version", configuration.anthropicVersion)
        }

        try {
            connection.outputStream.use { it.write(body.toByteArray(Charsets.UTF_8)) }

            val code = connection.responseCode
            if (code !in 200..299) {
                throw AIError.RequestFailed(code)
            }
            val responseText = connection.inputStream.bufferedReader().readText()
            if (responseText.isEmpty()) throw AIError.EmptyResponse
            return responseText
        } catch (e: AIError) {
            throw e
        } catch (e: Exception) {
            throw AIError.NetworkUnavailable
        } finally {
            connection.disconnect()
        }
    }
}
