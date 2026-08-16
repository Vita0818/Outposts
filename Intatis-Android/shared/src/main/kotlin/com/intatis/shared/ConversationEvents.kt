package com.intatis.shared

import com.intatis.shared.protocol.EventLog
import com.intatis.shared.protocol.EventLogRecord
import com.intatis.shared.protocol.WorkspaceAccess
import com.intatis.shared.protocol.WorkspaceLease
import com.intatis.shared.protocol.CapabilityLease
import com.intatis.shared.protocol.newProtocolId

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
    const val AgentMessage = "agent_message"
    const val AgentToAgentMessage = "agent_to_agent_message"
    const val UserMessage = "user_message"
    const val MessageDelta = "message_delta"
    const val MessageCompleted = "message_completed"
    const val Error = "error"
    const val AgentAttached = "agent_attached"
    const val AgentSpawned = "agent_spawned"
    const val AgentDetached = "agent_detached"
    const val AgentAttachRequested = "agent_attach_requested"
    const val AgentSpawnRequested = "agent_spawn_requested"
    const val InformationRequested = "information_requested"
    const val InformationReplied = "information_replied"
    const val DelegationRequested = "delegation_requested"
    const val DelegationApproved = "delegation_approved"
    const val DelegationRejected = "delegation_rejected"
    const val TaskCreated = "task_created"
    const val TaskAssigned = "task_assigned"
    const val TaskQueued = "task_queued"
    const val TaskStarted = "task_started"
    const val TaskCompleted = "task_completed"
    const val TaskFailed = "task_failed"
    const val TaskRejected = "task_rejected"
    const val WorkspaceLeaseRequested = "workspace_lease_requested"
    const val WorkspaceLeaseGranted = "workspace_lease_granted"
    const val WorkspaceLeaseDenied = "workspace_lease_denied"
    const val WorkspaceLeaseRevoked = "workspace_lease_revoked"
    const val CapabilityLeaseCreated = "capability_lease_created"
    const val CapabilityLeaseRevoked = "capability_lease_revoked"
    const val CapabilityLeaseBlocked = "capability_lease_blocked"
    const val ArtifactAdded = "artifact_added"
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

    fun workspaceLeaseRequested(
        agent: String,
        rootPath: String,
        access: WorkspaceAccess,
        reason: String,
        workspaceLeaseId: String? = null,
        workspaceID: String? = null,
    ): Map<String, Any?> =
        buildMap {
            put("agent", agent)
            put("root_path", rootPath)
            put("access", access.toString())
            put("reason", reason)
            if (workspaceLeaseId != null) put("workspace_lease_id", workspaceLeaseId)
            if (workspaceID != null) put("workspace_id", workspaceID)
        }

    fun workspaceLeaseGranted(agent: String, lease: WorkspaceLease): Map<String, Any?> =
        buildMap {
            put("agent", agent)
            put("lease", workspaceLeasePayload(lease))
            put("root_path", lease.rootPath)
            put("access", lease.access.toString())
            put("workspace_lease_id", lease.id)
            put("workspace_id", lease.workspaceID)
        }

    fun workspaceLeaseDenied(
        agent: String,
        rootPath: String,
        reason: String,
        workspaceLeaseId: String? = null,
        workspaceID: String? = null,
    ): Map<String, Any?> =
        buildMap {
            put("agent", agent)
            put("root_path", rootPath)
            put("reason", reason)
            if (workspaceLeaseId != null) put("workspace_lease_id", workspaceLeaseId)
            if (workspaceID != null) put("workspace_id", workspaceID)
        }

    fun workspaceLeaseRevoked(
        agent: String,
        workspaceLeaseId: String,
        reason: String,
    ): Map<String, Any?> = mapOf(
        "agent" to agent,
        "workspace_lease_id" to workspaceLeaseId,
        "reason" to reason,
    )

    fun capabilityLeaseCreated(agent: String, lease: CapabilityLease): Map<String, Any?> =
        buildMap {
            put("agent", agent)
            put("lease", capabilityLeasePayload(lease))
            put("capability_lease_id", lease.id)
        }

    fun capabilityLeaseRevoked(agent: String, leaseId: String, reason: String): Map<String, Any?> =
        mapOf(
            "agent" to agent,
            "lease_id" to leaseId,
            "reason" to reason,
        )

    fun agentAttachRequested(agent: String, path: String, model: String, profile: String): Map<String, Any?> =
        mapOf(
            "agent" to agent,
            "path" to path,
            "model" to model,
            "profile" to profile,
        )

    fun agentAttached(agent: String, path: String, model: String, profile: String): Map<String, Any?> =
        mapOf(
            "agent" to agent,
            "path" to path,
            "model" to model,
            "profile" to profile,
        )

    fun agentDetached(agent: String): Map<String, Any?> = mapOf(
        "agent" to agent,
    )

    fun capabilityLeaseBlocked(
        agent: String,
        tool: String,
        reason: String,
        target: String? = null,
    ): Map<String, Any?> =
        buildMap {
            put("agent", agent)
            put("tool", tool)
            put("reason", reason)
            if (target != null) {
                put("target", target)
            }
        }

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

    fun agentMessage(from: String, to: String, content: String, kind: String, mediated: Boolean): Map<String, Any?> =
        mapOf(
            "agent" to from,
            "message_id" to newProtocolId("msg"),
            "content" to content,
            "from" to from,
            "to" to to,
            "kind" to kind,
            "mediated" to mediated,
        )

    fun informationRequested(from: String, to: String, question: String, mediated: Boolean, taskID: String? = null): Map<String, Any?> =
        buildMap {
            put("from", from)
            put("to", to)
            put("question", question)
            put("mediated", mediated)
            if (!taskID.isNullOrBlank()) {
                put("task_id", taskID)
            }
        }

    fun informationReplied(
        from: String,
        to: String,
        content: String,
        mediated: Boolean,
        inReplyTo: String? = null,
        taskID: String? = null,
    ): Map<String, Any?> =
        buildMap {
            put("from", from)
            put("to", to)
            put("content", content)
            put("mediated", mediated)
            if (!inReplyTo.isNullOrBlank()) {
                put("in_reply_to", inReplyTo)
            }
            if (!taskID.isNullOrBlank()) {
                put("task_id", taskID)
            }
        }

    fun delegationRequested(requester: String, recipient: String?, objective: String, reason: String): Map<String, Any?> =
        buildMap {
            put("requester", requester)
            if (!recipient.isNullOrBlank()) {
                put("recipient", recipient)
            }
            put("objective", objective)
            put("reason", reason)
        }

    fun delegationApproved(requester: String, assignee: String?, objective: String, reason: String): Map<String, Any?> =
        buildMap {
            put("requester", requester)
            if (!assignee.isNullOrBlank()) {
                put("assignee", assignee)
            }
            put("objective", objective)
            put("reason", reason)
        }

    fun delegationRejected(
        requester: String,
        assignee: String?,
        objective: String,
        reason: String,
        violationKind: String? = null,
    ): Map<String, Any?> =
        buildMap {
            put("requester", requester)
            if (!assignee.isNullOrBlank()) {
                put("assignee", assignee)
            }
            put("objective", objective)
            put("reason", reason)
            if (!violationKind.isNullOrBlank()) {
                put("violation_kind", violationKind)
            }
        }

    fun userMessage(
        messageId: String,
        text: String,
        attachments: List<String>? = null,
        to: String? = null,
        tags: List<String>? = null,
        goal: String? = null,
        role: String = "user",
    ): Map<String, Any?> =
        buildMap {
            put("message_id", messageId)
            put("role", role)
            put("text", text)
            if (attachments != null) put("attachments", attachments)
            if (to != null) put("to", to)
            if (tags != null) put("tags", tags)
            if (goal != null) put("goal", goal)
        }

    fun messageDelta(
        messageId: String,
        role: String,
        textDelta: String,
        agent: String? = null,
        to: String? = null,
        goal: String? = null,
    ): Map<String, Any?> =
        buildMap {
            put("message_id", messageId)
            put("role", role)
            put("text_delta", textDelta)
            if (agent != null) put("agent", agent)
            if (to != null) put("to", to)
            if (goal != null) put("goal", goal)
        }

    fun messageCompleted(
        messageId: String,
        role: String,
        text: String,
        agent: String? = null,
        to: String? = null,
        goal: String? = null,
    ): Map<String, Any?> =
        buildMap {
            put("message_id", messageId)
            put("role", role)
            put("text", text)
            if (agent != null) put("agent", agent)
            if (to != null) put("to", to)
            if (goal != null) put("goal", goal)
        }

    fun error(message: String, code: String = "chat_error", fatal: Boolean = false, agent: String? = null): Map<String, Any?> =
        buildMap {
            put("code", code)
            put("message", message)
            put("fatal", fatal)
            if (agent != null) put("agent", agent)
        }

    private fun workspaceLeasePayload(lease: WorkspaceLease): Map<String, Any?> = buildMap {
        put("id", lease.id)
        put("workspace_id", lease.workspaceID)
        put("root_path", lease.rootPath)
        put("access", lease.access.toString())
        put("allowed_path_rules", lease.allowedPathRules.map { it.pattern })
        put("denied_patterns", lease.deniedPatterns)
    }

    private fun capabilityLeasePayload(lease: CapabilityLease): Map<String, Any?> = buildMap {
        put("id", lease.id)
        put("task_id", lease.taskID)
        put("tools", lease.tools.map { it.name })
        put("communication", lease.communication.toString())
        put("communication_payload", lease.communicationPayload)
        put("delegation", lease.delegation.toString())
        if (lease.delegationBudget != null) {
            put(
                "delegation_budget",
                mapOf(
                    "max_tasks" to lease.delegationBudget.maxTasks,
                    "max_depth" to lease.delegationBudget.maxDepth,
                ),
            )
        }
        put("expires_at_task_completion", lease.expiresAtTaskCompletion)
    }
}

class SessionEventLog(category: String, session: String? = null) : IConversationEventSink {
    private val eventLog = EventLog(category, session)
    private val path: String = eventLog.pathValue
    private val sessionId: String = eventLog.sessionIdValue

    val logPath: String = path
    val sessionIdValue: String = sessionId

    override suspend fun appendAsync(eventType: String, payload: Map<String, Any?>) {
        eventLog.appendAsync(eventType, payload)
    }

    suspend fun readAll(): List<EventLogRecord> {
        return eventLog.readAll()
    }
}
