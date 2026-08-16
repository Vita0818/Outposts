package com.intatis.shared

import com.intatis.shared.agentkernel.Agent
import com.intatis.shared.protocol.CapabilityLease
import com.intatis.shared.protocol.WorkspaceLease
import java.time.Duration

class CodeAgentSession(
    config: IntatisConfig,
    workspaceRoot: String,
    agentName: String = "agent",
    permissionProfile: PermissionProfile = PermissionProfile.Reviewed,
    shell: IToolShellRunner? = null,
    git: IToolGitService? = null,
    private val messenger: IToolAgentMessenger? = null,
    responder: IPermissionResponder = AllowAllResponder(),
    permissionReviewer: IPermissionReviewer? = null,
    eventSink: IConversationEventSink = NullConversationEventSink(),
    allowsShell: Boolean = true,
    maxIterations: Int = 8,
    systemPrompt: String? = null,
    capabilityLease: CapabilityLease? = null,
    workspaceLease: WorkspaceLease = WorkspaceLease(
        rootPath = workspaceRoot,
        access = com.intatis.shared.protocol.WorkspaceAccess.ReadWrite,
    ),
) {
    private val agent = Agent(
        config = config,
        workspaceRoot = workspaceRoot,
        name = agentName,
        permissionProfile = permissionProfile,
        shell = shell,
        git = git,
        messenger = messenger,
        responder = responder,
        permissionReviewer = permissionReviewer,
            eventSink = eventSink,
            allowsShell = allowsShell,
            maxIterations = maxIterations,
            systemPrompt = systemPrompt,
            capabilityLease = capabilityLease,
            workspaceLease = workspaceLease,
        )

    val toolNames: List<String>
        get() = agent.toolNames

    fun clear() {
        agent.clear()
    }

    suspend fun sendAsync(
        userText: String,
        model: String? = null,
        reasoning: String? = null,
        userGoal: String? = null,
        attachments: List<ImageAttachment>? = null,
        includeUsage: Boolean? = null,
        to: String? = null,
        tags: List<String>? = null,
        currentTaskID: String? = null,
    ): Triple<String, Duration, String?> = agent.sendAsync(
        userText = userText,
        model = model,
        reasoning = reasoning,
        userGoal = userGoal,
        attachments = attachments,
        includeUsage = includeUsage,
        to = to,
        tags = tags,
        currentTaskID = currentTaskID,
    )
}
