package com.intatis.shared.providers

import kotlinx.coroutines.flow.Flow

enum class ReasoningEffort(val wire: String) {
    MINIMAL("minimal"),
    LOW("low"),
    MEDIUM("medium"),
    HIGH("high");

    companion object {
        fun fromWire(value: String?): ReasoningEffort? = when (value?.lowercase()) {
            "minimal" -> MINIMAL
            "low" -> LOW
            "medium" -> MEDIUM
            "high" -> HIGH
            else -> null
        }
    }
}

data class ImageAttachment(val url: String)

data class ChatMessage(
    val role: String, // system | user | assistant
    val content: String,
    val images: List<ImageAttachment> = emptyList(),
)

data class ChatRequest(
    val model: String,
    val messages: List<ChatMessage>,
    val temperature: Double? = null,
    val reasoningEffort: ReasoningEffort? = null,
    val includeUsage: Boolean = false,
)

data class Usage(
    val promptTokens: Int? = null,
    val cachedPromptTokens: Int? = null,
    val completionTokens: Int? = null,
    val totalTokens: Int? = null,
) {
    fun mergedWith(other: Usage): Usage = Usage(
        promptTokens = other.promptTokens ?: promptTokens,
        cachedPromptTokens = other.cachedPromptTokens ?: cachedPromptTokens,
        completionTokens = other.completionTokens ?: completionTokens,
        totalTokens = other.totalTokens ?: totalTokens,
    )

    companion object { val EMPTY = Usage() }
}

/** Streaming chunks surfaced by a chat provider (delta text, usage, completion). */
sealed class ChatChunk {
    data class Delta(val text: String) : ChatChunk()
    data class UsageReport(val usage: Usage) : ChatChunk()
    object Done : ChatChunk()
}

interface ChatProvider {
    /**
     * Must return promptly; network work happens while the flow is collected.
     * Collection happens on the caller's dispatcher; the provider hops to IO itself.
     */
    fun streamChat(request: ChatRequest): Flow<ChatChunk>
}

class ProviderException(val code: String, message: String, val statusCode: Int? = null) :
    Exception(message) {
    val isRetryable: Boolean get() = statusCode == 408 || statusCode == 429 || (statusCode ?: 0) >= 500
}
