package com.rokurics.app.domain.model

import java.util.Date
import java.util.UUID

enum class ChatMessageRole {
    SYSTEM, USER, ASSISTANT
}

data class ChatMessage(
    val id: String = UUID.randomUUID().toString(),
    val role: ChatMessageRole,
    val content: String,
    val createdAt: Date = Date(),
    val attachmentIDs: List<String> = emptyList()
)

data class ChatContextItem(
    val id: String,
    val title: String,
    val content: String,
    val filingPath: StudyFilingPath = StudyFilingPath(),
    val sourcePath: String? = null,
    val contentCharacterCount: Int = 0,
    val isTruncated: Boolean = false
)

data class ChatContext(
    val id: String = UUID.randomUUID().toString(),
    val title: String = "学习库",
    val browsePathComponents: List<String> = emptyList(),
    val itemCount: Int = 0,
    val items: List<ChatContextItem> = emptyList(),
    val isTruncated: Boolean = false
)

data class ChatConversation(
    val id: String = UUID.randomUUID().toString(),
    val title: String = "新对话",
    val messages: List<ChatMessage> = emptyList(),
    val activeContextID: String? = null,
    val createdAt: Date = Date(),
    val updatedAt: Date = Date()
)

data class ChatRequest(
    val messages: List<ChatMessage>,
    val context: ChatContext? = null,
    val modelName: String? = null,
    val maxTokens: Int = 4096,
    val temperature: Double = 0.7
)
