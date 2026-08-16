package com.intatis.shared.protocol

import java.time.Instant

enum class CoworkEventScope {
    thread,
    task,
    agent,
    workspace,
    capability
}

enum class CoworkEventVisibility {
    global,
    task,
    agent,
    privateAgent,
}

data class CoworkEventMetadata(
    val threadID: ThreadID? = null,
    val taskID: TaskID? = null,
    val rootTaskID: TaskID? = null,
    val parentTaskID: TaskID? = null,
    val sender: AgentID? = null,
    val recipient: AgentID? = null,
    val agentID: AgentID? = null,
    val issuer: AgentID? = null,
    val assignee: AgentID? = null,
    val workspaceID: WorkspaceID? = null,
    val workspaceLeaseID: WorkspaceLeaseID? = null,
    val capabilityLeaseID: CapabilityLeaseID? = null,
    val causalParentID: TaskID? = null,
    val scope: CoworkEventScope = CoworkEventScope.thread,
    val visibility: CoworkEventVisibility = CoworkEventVisibility.global,
    val createdAt: String = Instant.now().toString()
)

data class AgentAttachRequestedPayload(
    val agent: AgentID,
    val path: String,
    val model: ModelID,
    val profile: String,
    val metadata: CoworkEventMetadata? = null
)

data class AgentAttachedPayload(
    val agent: AgentID,
    val path: String,
    val model: ModelID,
    val profile: String,
    val metadata: CoworkEventMetadata? = null
)

data class AgentDetachedPayload(
    val agent: AgentID,
    val metadata: CoworkEventMetadata? = null
)

data class AgentSpawnRequestedPayload(
    val requestedBy: AgentID? = null,
    val agent: AgentID,
    val path: String,
    val model: ModelID? = null,
    val metadata: CoworkEventMetadata? = null
)

data class AgentSpawnedPayload(
    val agent: AgentID,
    val path: String,
    val model: ModelID,
    val metadata: CoworkEventMetadata? = null
)

data class AgentMessagePayload(
    val agent: AgentID,
    val messageId: MessageID,
    val content: String,
    val from: AgentID? = null,
    val to: AgentID? = null,
    val kind: AgentCommunicationKind? = null,
    val taskID: TaskID? = null,
    val inReplyTo: MessageID? = null,
    val mediated: Boolean? = null,
    val metadata: CoworkEventMetadata? = null
)

enum class AgentCommunicationKind {
    sendMessage,
    requestInformation,
    replyMessage
}

data class InformationRequestedPayload(
    val requestID: MessageID = newProtocolId("msg"),
    val from: AgentID,
    val to: AgentID,
    val question: String,
    val mediated: Boolean,
    val taskID: TaskID? = null,
    val metadata: CoworkEventMetadata? = null
)

data class InformationRepliedPayload(
    val replyID: MessageID = newProtocolId("msg"),
    val inReplyTo: MessageID? = null,
    val from: AgentID,
    val to: AgentID,
    val content: String,
    val mediated: Boolean,
    val taskID: TaskID? = null,
    val metadata: CoworkEventMetadata? = null
)

data class DelegationRequestedPayload(
    val requestID: RequestID = newProtocolId("req"),
    val requester: AgentID,
    val recipient: AgentID? = null,
    val objective: String,
    val reason: String,
    val parentTaskID: TaskID? = null,
    val metadata: CoworkEventMetadata? = null
)

data class DelegationApprovedPayload(
    val requestID: RequestID? = null,
    val contract: TaskContract,
    val reason: String = "delegation approved",
    val metadata: CoworkEventMetadata? = null
)

data class DelegationRejectedPayload(
    val requestID: RequestID? = null,
    val requester: AgentID,
    val assignee: AgentID? = null,
    val objective: String,
    val reason: String,
    val violationKind: String? = null,
    val metadata: CoworkEventMetadata? = null
)

data class TaskDelegatedPayload(
    val contract: TaskContract,
    val metadata: CoworkEventMetadata? = null
) {
    val issuer: AgentID? = contract.issuer
    val assignee: AgentID = contract.assignee
}

data class WorkspaceLeaseRequestedPayload(
    val agent: AgentID? = null,
    val workspaceID: WorkspaceID? = null,
    val workspaceLeaseID: WorkspaceLeaseID? = null,
    val rootPath: String,
    val access: WorkspaceAccess,
    val reason: String,
    val metadata: CoworkEventMetadata? = null
)

data class WorkspaceLeaseGrantedPayload(
    val agent: AgentID? = null,
    val lease: WorkspaceLease,
    val metadata: CoworkEventMetadata? = null
)

data class WorkspaceLeaseDeniedPayload(
    val agent: AgentID? = null,
    val workspaceID: WorkspaceID? = null,
    val workspaceLeaseID: WorkspaceLeaseID? = null,
    val rootPath: String,
    val reason: String,
    val metadata: CoworkEventMetadata? = null
)

data class CapabilityLeaseCreatedPayload(
    val agent: AgentID? = null,
    val lease: CapabilityLease,
    val metadata: CoworkEventMetadata? = null
)

data class CapabilityLeaseRevokedPayload(
    val agent: AgentID? = null,
    val leaseID: CapabilityLeaseID,
    val reason: String,
    val metadata: CoworkEventMetadata? = null
)

data class AgentToAgentMessagePayload(
    val from: AgentID,
    val to: AgentID,
    val content: String,
    val mediated: Boolean
)

data class TaskCreatedPayload(
    val contract: TaskContract,
    val metadata: CoworkEventMetadata? = null
)

data class TaskAssignedPayload(
    val contract: TaskContract,
    val metadata: CoworkEventMetadata? = null
)

data class TaskQueuedPayload(
    val contract: TaskContract,
    val rootTaskID: TaskID? = null,
    val parentTaskID: TaskID? = null,
    val issuer: AgentID? = null,
    val assignee: AgentID,
    val causalParentID: TaskID? = null,
    val hopCount: Int,
    val visitedAgents: List<AgentID>,
    val metadata: CoworkEventMetadata? = null
)

data class TaskStartedPayload(
    val taskID: TaskID,
    val agent: AgentID,
    val metadata: CoworkEventMetadata? = null
)

data class TaskCompletedPayload(
    val taskID: TaskID,
    val agent: AgentID,
    val result: String,
    val metadata: CoworkEventMetadata? = null
)

data class TaskFailedPayload(
    val taskID: TaskID,
    val agent: AgentID,
    val error: String,
    val metadata: CoworkEventMetadata? = null
)

data class TaskRejectedPayload(
    val contract: TaskContract? = null,
    val requester: AgentID? = null,
    val assignee: AgentID? = null,
    val objective: String,
    val reason: String,
    val violationKind: String? = null,
    val metadata: CoworkEventMetadata? = null
)
