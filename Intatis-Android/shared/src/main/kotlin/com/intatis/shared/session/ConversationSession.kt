package com.intatis.shared.session

import com.intatis.shared.attachments.ImageAttachment
import com.intatis.shared.model.IntatisConfig
import com.intatis.shared.model.IntatisMessage
import com.intatis.shared.model.MessageRole
import com.intatis.shared.provider.OpenAIClient
import java.time.Duration

class ConversationSession(config: IntatisConfig) {
    private val client = OpenAIClient(config)
    private val messages = mutableListOf<IntatisMessage>()

    init {
        messages.add(
            IntatisMessage(
                role = MessageRole.SYSTEM,
                content = "You are Intatis (Android). Provide short, practical responses.",
            ),
        )
    }

    fun clear() {
        val system = messages.firstOrNull { it.role == MessageRole.SYSTEM }
        messages.clear()
        if (system != null) messages.add(system)
    }

    suspend fun sendUserMessageAsync(
        userText: String,
        model: String?,
        reasoning: String?,
        attachments: List<ImageAttachment>,
    ): Triple<IntatisMessage, Duration, String?> {
        messages.add(IntatisMessage(role = MessageRole.USER, content = userText))
        val result = client.sendAsync(messages, model, reasoning, attachments)
        val assistant = IntatisMessage(role = MessageRole.ASSISTANT, content = result.text)
        messages.add(assistant)
        return Triple(assistant, Duration.ofMillis(result.latencyMs), result.usage)
    }
}
