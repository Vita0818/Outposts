package com.intatis.shared

import java.time.Instant
import java.util.UUID

enum class IntatisMode {
    Chat,
    Code,
    Cowork
}

enum class MessageRole {
    User,
    Assistant,
    System,
    Agent
}

data class IntatisMessage(
    val id: String = UUID.randomUUID().toString(),
    val role: MessageRole,
    var content: String,
    val atUtc: Instant = Instant.now()
)

data class IntatisConfig(
    val baseUrl: String,
    val apiKey: String,
    val model: String,
    val reasoning: String?,
    val defaultMode: IntatisMode,
    val workspace: String?,
    val includeUsage: Boolean
)

data class SearchHit(
    val file: String,
    val line: Int,
    val text: String
)
