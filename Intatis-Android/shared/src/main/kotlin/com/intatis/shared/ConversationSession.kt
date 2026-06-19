package com.intatis.shared

import kotlinx.coroutines.delay
import java.time.Duration

class ConversationSession(config: IntatisConfig) {
    private val client = OpenAIClient(config)
    private val messages = mutableListOf<IntatisMessage>()

    init {
        messages.add(
            IntatisMessage(
                role = MessageRole.System,
                content = "You are Intatis (Android). Provide short, practical responses."
            )
        )
    }

    val history: List<IntatisMessage>
        get() = messages.toList()

    suspend fun sendUserMessageAsync(
        userText: String,
        model: String? = null,
        reasoning: String? = null,
        attachments: List<ImageAttachment>? = null
    ): Triple<IntatisMessage, Duration, String?> {
        val user = IntatisMessage(role = MessageRole.User, content = userText)
        messages.add(user)

        val start = System.nanoTime()
        val (text, _, usage) = client.sendAsync(messages, model, reasoning, attachments)
        val elapsed = Duration.ofNanos(System.nanoTime() - start)
        val assistant = IntatisMessage(role = MessageRole.Assistant, content = text)
        messages.add(assistant)
        return Triple(assistant, elapsed, usage)
    }

    fun clear() {
        val system = messages.firstOrNull { it.role == MessageRole.System }
        messages.clear()
        if (system != null) {
            messages.add(system)
        }
    }
}
