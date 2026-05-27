package com.rokurics.app.domain.provider

import com.rokurics.app.domain.model.ChatMessage
import com.rokurics.app.domain.model.ChatContext

data class ChatCompletionRequest(
    val messages: List<ChatMessage>,
    val context: ChatContext? = null,
    val modelName: String? = null,
    val maxTokens: Int = 4096,
    val temperature: Double = 0.7
)

data class ChatCompletionResult(
    val message: ChatMessage,
    val providerID: String,
    val modelName: String? = null,
    val finishReason: String? = null
)

interface ChatProvider {
    val id: String
    val displayName: String
    suspend fun validateConfiguration()
    suspend fun chat(request: ChatCompletionRequest): Result<ChatCompletionResult>
}

class MockChatProvider : ChatProvider {
    override val id = "mock_chat"
    override val displayName = "Mock Chat"
    override suspend fun validateConfiguration() {}
    override suspend fun chat(request: ChatCompletionRequest): Result<ChatCompletionResult> =
        Result.success(ChatCompletionResult(
            message = ChatMessage(
                role = com.rokurics.app.domain.model.ChatMessageRole.ASSISTANT,
                content = "[Mock] This is a simulated AI response. In production, this would connect to an LLM API."
            ),
            providerID = id,
            modelName = "mock-v1"
        ))
}
