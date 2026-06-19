package com.intatis.shared.model

import java.time.Instant

enum class IntatisMode {
    CHAT,
    CODE,
    COWORK
}

enum class MessageRole {
    USER,
    ASSISTANT,
    SYSTEM,
    TOOL,
    AGENT
}

data class IntatisMessage(
    val id: String,
    val role: MessageRole,
    val content: String,
    val at: Instant,
) {
    constructor(role: MessageRole, content: String) : this(java.util.UUID.randomUUID().toString(), role, content, Instant.now())
}

data class IntatisConfig(
    val baseUrl: String,
    val apiKey: String,
    val model: String,
    val reasoning: String?,
    val defaultMode: IntatisMode,
    val workspace: String?,
    val includeUsage: Boolean,
) {
    fun cloneWith(
        baseUrl: String? = null,
        apiKey: String? = null,
        model: String? = null,
        reasoning: String? = null,
        defaultMode: IntatisMode? = null,
        workspace: String? = null,
        includeUsage: Boolean? = null,
    ) = IntatisConfig(
        baseUrl ?: this.baseUrl,
        apiKey ?: this.apiKey,
        model ?: this.model,
        reasoning,
        defaultMode ?: this.defaultMode,
        workspace,
        includeUsage ?: this.includeUsage,
    )
}

data class SearchHit(
    val file: String,
    val line: Int,
    val text: String,
)
