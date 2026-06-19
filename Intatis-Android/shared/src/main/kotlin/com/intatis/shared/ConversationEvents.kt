package com.intatis.shared

import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.io.File
import java.util.concurrent.atomic.AtomicInteger

interface IConversationEventSink {
    suspend fun appendAsync(eventType: String, payload: Map<String, Any?>)
}

class NullConversationEventSink : IConversationEventSink {
    override suspend fun appendAsync(eventType: String, payload: Map<String, Any?>) {}
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
    fun toolCall(toolCallId: String, agent: String, tool: String, args: String): Map<String, Any?> =
        mapOf(
            "tool_call_id" to toolCallId,
            "agent" to agent,
            "tool" to tool,
            "args" to args,
        )

    fun toolResult(toolCallId: String, observation: String, truncated: Boolean? = null): Map<String, Any?> {
        val payload = mutableMapOf<String, Any?>(
            "tool_call_id" to toolCallId,
            "observation" to observation,
        )
        truncated?.let { payload["truncated"] = it }
        return payload
    }

    fun permissionRequest(requestId: String, agent: String?, tool: String, args: String, risk: RiskLevel, reason: String): Map<String, Any?> =
        mapOf(
            "request_id" to requestId,
            "agent" to (agent ?: ""),
            "tool" to tool,
            "args" to args,
            "risk" to risk.toString(),
            "reason" to reason,
        )

    fun permissionResolved(
        requestId: String?,
        tool: String,
        decision: PermissionDecision,
        risk: RiskLevel,
        reason: String,
        agent: String? = null
    ): Map<String, Any?> =
        mapOf(
            "request_id" to (requestId ?: ""),
            "tool" to tool,
            "agent" to (agent ?: ""),
            "decision" to when (decision) {
                is PermissionDecision.Allow -> "allow"
                is PermissionDecision.Deny -> "deny"
                is PermissionDecision.AskUser -> "ask_user"
            },
            "risk" to risk.toString(),
            "reason" to reason,
        )

    fun permissionReview(tool: String, decision: PermissionDecision, risk: RiskLevel, reason: String, reviewerModel: String, agent: String? = null): Map<String, Any?> =
        mapOf(
            "agent" to (agent ?: ""),
            "tool" to tool,
            "reviewer_model" to reviewerModel,
            "decision" to when (decision) {
                is PermissionDecision.Allow -> "allow"
                is PermissionDecision.Deny -> "deny"
                is PermissionDecision.AskUser -> "ask_user"
            },
            "risk" to risk.toString(),
            "reason" to reason,
        )

    fun patchProposed(patchId: String, agent: String, files: List<String>, diff: String): Map<String, Any?> =
        mapOf(
            "patch_id" to patchId,
            "agent" to agent,
            "files" to files,
            "diff" to diff,
        )

    fun agentToAgentMessage(from: String, to: String, content: String, mediated: Boolean): Map<String, Any?> =
        mapOf(
            "from" to from,
            "to" to to,
            "content" to content,
            "mediated" to mediated,
        )
}

class SessionEventLog(category: String, session: String? = null) : IConversationEventSink {
    private val mutex = Mutex()
    private val sequence = AtomicInteger(0)
    private val json = Json { encodeDefaults = true }
    private val path: String

    init {
        val folder = File(ConfigStore.configFolder, "logs/$category").apply { mkdirs() }
        path = File(folder, (session ?: java.util.UUID.randomUUID().toString()) + ".jsonl").absolutePath
    }

    val logPath: String
        get() = path

    override suspend fun appendAsync(eventType: String, payload: Map<String, Any?>) {
        val wrapper = mapOf(
            "type" to eventType,
            "session" to path.substringAfterLast('/').removeSuffix(".jsonl"),
            "seq" to sequence.getAndIncrement(),
            "ts" to java.time.Instant.now().toString(),
            "payload" to payload,
        )
        val line = json.encodeToString(wrapper)
        mutex.withLock {
            File(path).appendText(line + System.lineSeparator())
        }
    }
}
