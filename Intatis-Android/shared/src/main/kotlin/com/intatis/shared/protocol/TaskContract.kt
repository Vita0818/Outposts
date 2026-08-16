package com.intatis.shared.protocol

enum class TaskKind {
    root,
    agentInvocation
}

enum class TaskStatus {
    created,
    assigned,
    queued,
    running,
    completed,
    failed,
    cancelled
}

data class TaskContract(
    val id: TaskID = newProtocolId("task"),
    val kind: TaskKind = TaskKind.agentInvocation,
    val issuer: AgentID? = null,
    val assignee: AgentID,
    val parentTaskID: TaskID? = null,
    val objective: String,
    val roleHint: String,
    val expectedDeliverable: String,
    val workspaceID: WorkspaceID? = null,
    val workspaceLeaseID: WorkspaceLeaseID? = null,
    val capabilityLeaseID: CapabilityLeaseID? = null,
    val relatedAgents: List<AgentID> = emptyList(),
    val relatedTasks: List<TaskID> = emptyList(),
    val constraints: List<String> = emptyList()
)
