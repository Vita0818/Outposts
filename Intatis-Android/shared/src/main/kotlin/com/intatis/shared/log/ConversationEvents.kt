package com.intatis.shared.log

import com.intatis.shared.security.PermissionDecision
import com.intatis.shared.security.RiskLevel
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.io.File
import java.time.Instant
import java.util.concurrent.atomic.AtomicInteger

interface ConversationEventSink {
    suspend fun append(eventType: String, payload: Map<String, Any?>)
}

class NullConversationEventSink : ConversationEventSink {
    override suspend fun append(eventType: String, payload: Map<String, Any?>) {}
}

object ConversationEventKinds {
    const val ToolCall = "tool_call"
    const val ToolResult = "tool_result"
    const val PermissionRequest = "permission_request"
    const val PermissionResolved = "permission_resolved"
    const val PermissionReview = "permission_review"
    const val PatchProposed = "patch_proposed"
    const val AgentToAgentMessage = "agent_to_agent_message"
}

object ConversationEventPayloads {
    fun toolCall(toolCallId: String, agent: String, tool: String, args: String) = mapOf(
        "tool_call_id" to toolCallId,
        "agent" to agent,
        "tool" to tool,
        "args" to args,
    )

    fun toolResult(toolCallId: String, observation: String, truncated: Boolean? = null): Map<String, Any?> {
        val payload = mutableMapOf<String, Any?>("tool_call_id" to toolCallId, "observation" to observation)
        if (truncated != null) payload["truncated"] = truncated
        return payload
    }

    fun permissionRequest(
        requestId: String,
        agent: String,
        tool: String,
        args: String,
        risk: RiskLevel,
        reason: String,
    ): Map<String, Any?> = mapOf(
        "request_id" to requestId,
        "agent" to agent,
        "tool" to tool,
        "args" to args,
        "risk" to risk.name.lowercase(),
        "reason" to reason,
    )

    fun permissionResolved(
        requestId: String?,
        tool: String,
        agent: String?,
        decision: PermissionDecision,
        risk: RiskLevel,
        reason: String,
    ): Map<String, Any?> = mutableMapOf<String, Any?>(
        "request_id" to (requestId ?: ""),
        "tool" to tool,
        "agent" to (agent ?: ""),
        "decision" to when (decision) {
            PermissionDecision.ALLOW -> "allow"
            PermissionDecision.DENY -> "deny"
            PermissionDecision.ASK_USER -> "ask_user"
        },
        "risk" to risk.name.lowercase(),
        "reason" to reason,
    )

    fun permissionReview(
        tool: String,
        decision: PermissionDecision,
        risk: RiskLevel,
        reason: String,
        reviewerModel: String,
        agent: String? = null,
    ): Map<String, Any?> = mutableMapOf<String, Any?>(
        "tool" to tool,
        "agent" to (agent ?: ""),
        "reviewer_model" to reviewerModel,
        "decision" to when (decision) {
            PermissionDecision.ALLOW -> "allow"
            PermissionDecision.DENY -> "deny"
            PermissionDecision.ASK_USER -> "ask_user"
        },
        "risk" to risk.name.lowercase(),
        "reason" to reason,
    )

    fun patchProposed(patchId: String, agent: String, files: List<String>, diff: String): Map<String, Any?> = mapOf(
        "patch_id" to patchId,
        "agent" to agent,
        "files" to files,
        "diff" to diff,
    )

    fun agentToAgentMessage(from: String, to: String, content: String, mediated: Boolean): Map<String, Any?> = mapOf(
        "from" to from,
        "to" to to,
        "content" to content,
        "mediated" to mediated,
    )
}

class SessionEventLog(category: String, session: String? = null) : ConversationEventSink {
    private val sessionId = session ?: "${System.currentTimeMillis()}-${java.util.UUID.randomUUID()}"
    private val path = run {
        val root = File(System.getProperty("user.home"), ".intatis/logs/$category")
        root.mkdirs()
        File(root, "$sessionId.jsonl").absolutePath
    }
    private val mutex = Mutex()
    private val seq = AtomicInteger(0)
    private val json = Json { prettyPrint = false }

    fun path(): String = path

    override suspend fun append(eventType: String, payload: Map<String, Any?>) {
        val wrapper = toJsonObject(
            mapOf(
                "type" to eventType,
                "session" to sessionId,
                "seq" to seq.getAndIncrement(),
                "ts" to Instant.now().toString(),
                "payload" to payload,
            )
        )
        mutex.withLock {
            File(path).appendText(json.encodeToString(wrapper) + "\n")
        }
    }

    private fun toJsonObject(value: Map<String, Any?>): JsonElement =
        buildJsonObject {
            value.forEach { (k, v) -> put(k, toJsonElement(v)) }
        }

    private fun toJsonElement(value: Any?): JsonElement = when (value) {
        null -> JsonPrimitive("null")
        is String -> JsonPrimitive(value)
        is Number -> JsonPrimitive(value)
        is Boolean -> JsonPrimitive(value)
        is Iterable<*> -> buildJsonArray { value.forEach { add(toJsonElement(it)) } }
        is Map<*, *> -> buildJsonObject { value.forEach { (k, v) -> put(k.toString(), toJsonElement(v)) } }
        else -> JsonPrimitive(value.toString())
    }
}
