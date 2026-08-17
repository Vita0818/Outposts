package com.intatis.shared.protocol

import com.intatis.shared.protocol.Jsonx.str
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put

data class UserMessagePayload(
    val text: String,
    val to: String? = null,
    val attachments: List<String>? = null,
    val goal: String? = null,
    val submissionId: String? = null,
    val turnId: String? = null,
) {
    fun toJson(): JsonObject = buildJsonObject {
        put("text", text)
        to?.let { put("to", it) }
        attachments?.takeIf { it.isNotEmpty() }?.let {
            put("attachments", buildJsonArray { it.forEach { a -> add(kotlinx.serialization.json.JsonPrimitive(a)) } })
        }
        goal?.let { put("goal", it) }
        submissionId?.let { put("submission_id", it) }
        turnId?.let { put("turn_id", it) }
    }

    companion object {
        fun fromJson(node: JsonObject?): UserMessagePayload {
            val o = node ?: JsonObject(emptyMap())
            val attachments = (o["attachments"] as? JsonArray)
                ?.mapNotNull { (it as? kotlinx.serialization.json.JsonPrimitive)?.content }
                ?.takeIf { it.isNotEmpty() }
            return UserMessagePayload(
                text = o.str("text") ?: "",
                to = o.str("to"),
                attachments = attachments,
                goal = o.str("goal"),
                submissionId = o.str("submission_id"),
                turnId = o.str("turn_id"),
            )
        }
    }
}

data class MessageDeltaPayload(
    val messageId: String,
    val role: String = "assistant",
    val agent: String? = null,
    val textDelta: String,
) {
    fun toJson(): JsonObject = buildJsonObject {
        put("message_id", messageId)
        put("role", role)
        agent?.let { put("agent", it) }
        put("text_delta", textDelta)
    }
}

data class MessageCitation(val url: String, val title: String)

data class MessageCompletedPayload(
    val messageId: String,
    val role: String = "assistant",
    val agent: String? = null,
    val text: String,
    val citations: List<MessageCitation> = emptyList(),
) {
    fun toJson(): JsonObject = buildJsonObject {
        put("message_id", messageId)
        put("role", role)
        agent?.let { put("agent", it) }
        put("text", text)
        if (citations.isNotEmpty()) {
            put("citations", buildJsonArray {
                citations.forEach {
                    add(buildJsonObject {
                        put("url", it.url)
                        put("title", it.title)
                    })
                }
            })
        }
    }
}

data class ErrorPayload(
    val code: String = "runtime_failed",
    val message: String,
    val fatal: Boolean = false,
) {
    fun toJson(): JsonObject = buildJsonObject {
        put("code", code)
        put("message", if (message.length > 1024) message.take(1024) else message)
        put("fatal", fatal)
    }
}

data class AgentStatusPayload(val agent: String, val state: String = "idle") {
    fun toJson(): JsonObject = buildJsonObject {
        put("agent", agent)
        put("state", state)
    }
}

data class TurnStatsPayload(
    val promptTokens: Int? = null,
    val cachedPromptTokens: Int? = null,
    val completionTokens: Int? = null,
    val totalTokens: Int? = null,
    val model: String? = null,
) {
    fun toJson(): JsonObject = buildJsonObject {
        put("prompt_tokens", promptTokens)
        put("cached_prompt_tokens", cachedPromptTokens)
        put("completion_tokens", completionTokens)
        put("total_tokens", totalTokens)
        put("model", model)
    }
}

enum class TurnOutcomeWire(val wire: String) {
    COMPLETED("completed"),
    INTERRUPTED("interrupted"),
    FAILED("failed");
}

data class TurnOutcomePayload(
    val turnId: String,
    val outcome: String,
    val failureSource: String? = null,
    val reason: String? = null,
) {
    fun toJson(): JsonObject = buildJsonObject {
        put("turn_id", turnId)
        put("outcome", outcome)
        failureSource?.let { put("failure_source", it) }
        reason?.let { put("reason", it.take(512)) }
    }
}

data class SessionSettingsUpdatedPayload(
    val schemaVersion: Int = 1,
    val revision: Int = 1,
    val previousRevision: Int? = null,
    val changeKind: String = "created",
    val kind: String = "chat",
    val displayName: String? = null,
) {
    fun toJson(): JsonObject = buildJsonObject {
        put("schema_version", schemaVersion)
        put("revision", revision)
        previousRevision?.let { put("previous_revision", it) }
        put("change_kind", changeKind)
        put("kind", kind)
        displayName?.let { put("display_name", it) }
    }
}
