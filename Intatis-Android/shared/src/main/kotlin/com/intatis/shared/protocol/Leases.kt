package com.intatis.shared.protocol

enum class ToolCapability {
    readWorkspace,
    listWorkspace,
    searchWorkspace,
    readPDF,
    editPDF,
    reconstructDocument,
    compileLaTeX,
    generateMedia,
    browseWeb,
    runShell,
    proposePatch,
    applyPatch,
    sendMessage,
    requestInformation,
    replyMessage,
    requestDelegation,
    delegateTask,
    attachWorkspace
}

data class DelegationBudget(
    val maxTasks: Int,
    val maxDepth: Int
)

enum class DelegationGrant {
    none,
    requestOnly,
    granted
}

enum class CommunicationGrant {
    none,
    replyOnly,
    selectedAgents,
    taskGroup,
    anyAgentInThread
}

data class CommunicationGrantSelectedAgents(
    val type: CommunicationGrant = CommunicationGrant.selectedAgents,
    val agents: List<AgentID>
)

data class CommunicationGrantTaskGroup(
    val type: CommunicationGrant = CommunicationGrant.taskGroup,
    val taskGroup: String
)

data class CapabilityLease(
    val id: CapabilityLeaseID = newProtocolId("clease"),
    val taskID: TaskID? = null,
    val tools: Set<ToolCapability>,
    val communication: CommunicationGrant = CommunicationGrant.none,
    val communicationPayload: Any? = null,
    val delegation: DelegationGrant = DelegationGrant.none,
    val delegationBudget: DelegationBudget? = null,
    val expiresAtTaskCompletion: Boolean = true
) {
    fun isDefaultCommunication(grant: CommunicationGrant): Boolean = communication == grant

    fun canSendMessage(
        sender: String,
        target: String,
    ): Boolean = when (communication) {
        CommunicationGrant.none -> false
        CommunicationGrant.replyOnly -> false
        CommunicationGrant.selectedAgents ->
            communicationPayload is CommunicationGrantSelectedAgents &&
                communicationPayload.agents.any { it.equals(target, ignoreCase = true) }
            CommunicationGrant.taskGroup -> true
        CommunicationGrant.anyAgentInThread -> true
    }

    fun canRequestDelegation(): Boolean = when (delegation) {
        DelegationGrant.none -> false
        DelegationGrant.requestOnly -> true
        DelegationGrant.granted -> true
    }

    fun canDelegateTask(): Boolean = delegation == DelegationGrant.granted

    fun canUseDelegationBudget(): Boolean = delegationBudget?.maxTasks?.let { it > 0 } ?: true

    fun consumeDelegationBudget(): CapabilityLease = when (val budget = delegationBudget) {
        null -> this
        else -> {
            if (budget.maxTasks <= 0) return this
            copy(delegationBudget = budget.copy(maxTasks = budget.maxTasks - 1))
        }
    }

    companion object {
        private val workerTools: Set<ToolCapability> = setOf(
            ToolCapability.readWorkspace,
            ToolCapability.listWorkspace,
            ToolCapability.searchWorkspace,
            ToolCapability.readPDF,
            ToolCapability.replyMessage,
            ToolCapability.requestDelegation
        )
        private val coordinatorTools: Set<ToolCapability> = setOf(
            ToolCapability.readWorkspace,
            ToolCapability.listWorkspace,
            ToolCapability.searchWorkspace,
            ToolCapability.readPDF,
            ToolCapability.editPDF,
            ToolCapability.reconstructDocument,
            ToolCapability.compileLaTeX,
            ToolCapability.generateMedia,
            ToolCapability.browseWeb,
            ToolCapability.runShell,
            ToolCapability.proposePatch,
            ToolCapability.applyPatch,
            ToolCapability.sendMessage,
            ToolCapability.requestInformation,
            ToolCapability.replyMessage,
            ToolCapability.requestDelegation,
            ToolCapability.delegateTask,
            ToolCapability.attachWorkspace
        )

        fun worker(taskID: TaskID? = null): CapabilityLease = CapabilityLease(
            taskID = taskID,
            tools = workerTools,
            communication = CommunicationGrant.replyOnly,
            delegation = DelegationGrant.requestOnly
        )

        fun coordinator(taskID: TaskID? = null, budget: DelegationBudget = DelegationBudget(8, 1)): CapabilityLease = CapabilityLease(
            taskID = taskID,
            tools = coordinatorTools,
            communication = CommunicationGrant.anyAgentInThread,
            delegation = DelegationGrant.granted,
            delegationBudget = budget,
            expiresAtTaskCompletion = taskID != null
        )
    }
}

enum class WorkspaceAccess {
    readOnly,
    readWrite
}

data class PathRule(val pattern: String)

data class WorkspaceLease(
    val id: WorkspaceLeaseID = newProtocolId("wlease"),
    val workspaceID: WorkspaceID = newProtocolId("ws"),
    val rootPath: String,
    val access: WorkspaceAccess,
    val allowedPathRules: List<PathRule> = listOf(PathRule("."))
    ,
    val deniedPatterns: List<String> = listOf(
        ".env",
        ".ssh",
        "Library/Keychains",
        "**/secret*",
        "**/*token*",
        "**/*key*",
    )
)
