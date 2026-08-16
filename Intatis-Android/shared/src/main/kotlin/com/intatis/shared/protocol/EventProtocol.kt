package com.intatis.shared.protocol

import com.intatis.shared.MessageRole
import kotlinx.serialization.json.JsonObject
import java.time.Instant
import java.util.UUID

typealias IConversationEventSink = com.intatis.shared.IConversationEventSink
typealias NullConversationEventSink = com.intatis.shared.NullConversationEventSink
typealias ConversationEventKinds = com.intatis.shared.ConversationEventKinds
typealias ConversationEventPayloads = com.intatis.shared.ConversationEventPayloads
typealias SessionEventLog = com.intatis.shared.SessionEventLog

typealias SessionID = String
typealias ThreadID = String
typealias MessageID = String
typealias AgentID = String
typealias RequestID = String
typealias ArtifactID = String
typealias TaskID = String
typealias TaskGroupID = String
typealias WorkspaceID = String
typealias WorkspaceLeaseID = String
typealias CapabilityLeaseID = String
typealias ModelID = String

enum class SessionKind {
    chat,
    code,
    cowork;

    val usesWorkspace: Boolean
        get() = this == code || this == cowork
}

typealias AgentMessageRole = MessageRole

enum class EventType {
    userMessage,
    messageDelta,
    messageCompleted,
    error,
    toolCall,
    toolResult,
    permissionRequest,
    permissionResolved,
    patchProposed,
    agentStatus,
    agentAttached,
    agentAttachRequested,
    agentDetached,
    agentSpawnRequested,
    agentSpawned,
    agentMessage,
    agentToAgentMessage,
    informationRequested,
    informationReplied,
    delegationRequested,
    delegationApproved,
    delegationRejected,
    taskDelegated,
    workspaceLeaseRequested,
    workspaceLeaseGranted,
    workspaceLeaseDenied,
    capabilityLeaseCreated,
    capabilityLeaseRevoked,
    permissionReview,
    taskCreated,
    taskAssigned,
    taskQueued,
    taskStarted,
    taskCompleted,
    taskFailed,
    taskRejected,
    artifactAdded,
    artifactProgress,
    turnStats
}

enum class RiskLevel {
    low,
    medium,
    high
}

enum class PermissionDecision {
    allow,
    deny,
    ask_user
}

enum class AgentState {
    idle,
    thinking,
    tool,
    blocked
}

fun newProtocolId(prefix: String): String =
    "${prefix}_" + UUID.randomUUID().toString().replace("-", "").take(8)

data class EventEnvelope(
    val seq: Int,
    val ts: String = Instant.now().toString(),
    val session: SessionID,
    val v: Int = 1,
    val type: EventType,
    val payload: JsonObject? = null
)

data class UserMessagePayload(
    val text: String,
    val attachments: List<ArtifactID>? = null,
    val to: AgentID? = null,
    val tags: List<String>? = null,
    val goal: String? = null
)

data class MessageDeltaPayload(
    val messageId: MessageID,
    val role: AgentMessageRole,
    val agent: AgentID? = null,
    val textDelta: String
)

data class MessageCompletedPayload(
    val messageId: MessageID,
    val role: AgentMessageRole,
    val agent: AgentID? = null,
    val text: String
)

data class ErrorPayload(
    val code: String,
    val message: String,
    val fatal: Boolean = false
)

data class ToolCallPayload(
    val toolCallId: String,
    val agent: AgentID? = null,
    val name: String,
    val args: String
)

data class ToolResultPayload(
    val toolCallId: String,
    val observation: String,
    val truncated: Boolean? = null
)

data class PermissionRequestPayload(
    val requestId: RequestID,
    val agent: AgentID? = null,
    val tool: String,
    val args: String,
    val risk: RiskLevel,
    val reason: String
)

data class PermissionResolvedPayload(
    val requestId: RequestID? = null,
    val tool: String,
    val decision: PermissionDecision,
    val risk: RiskLevel,
    val reason: String,
    val agent: AgentID? = null
)

data class PermissionReviewPayload(
    val agent: AgentID? = null,
    val tool: String,
    val reviewerModel: String,
    val decision: PermissionDecision,
    val risk: RiskLevel,
    val reason: String
)

data class PatchProposedPayload(
    val patchId: String,
    val agent: AgentID? = null,
    val files: List<String>,
    val diff: String
)

data class AgentStatusPayload(
    val agent: AgentID? = null,
    val state: AgentState,
    val task: String? = null
)
