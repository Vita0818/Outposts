package com.intatis.shared.agentkernel

import com.intatis.shared.OpenAIClient
import com.intatis.shared.conversation.ProjectionLine

class ContextProjection {
    fun render(messages: List<OpenAIClient.OpenAIChatMessage>): List<ProjectionLine> = messages.map { message ->
        ProjectionLine(
            sender = message.role,
            text = message.content ?: "",
            isError = false,
        )
    }
}
