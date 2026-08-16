package com.intatis.shared.agentkernel

import com.intatis.shared.ImageAttachment
import com.intatis.shared.IPermissionResponder
import com.intatis.shared.IPermissionReviewer
import com.intatis.shared.ImageGenerationToolService
import com.intatis.shared.IToolAgentMessenger
import com.intatis.shared.IToolGitService
import com.intatis.shared.IToolShellRunner
import com.intatis.shared.IntatisConfig
import com.intatis.shared.PermissionProfile
import com.intatis.shared.ProcessGitService
import com.intatis.shared.ProcessShellRunner
import com.intatis.shared.ProviderImageGenerationToolService
import com.intatis.shared.provider.ProviderRegistry
import com.intatis.shared.protocol.CapabilityLease
import com.intatis.shared.protocol.WorkspaceLease
import java.time.Duration

class Agent(
    private val config: IntatisConfig,
    private val workspaceRoot: String,
    private val name: String = "agent",
    private val permissionProfile: PermissionProfile = PermissionProfile.Reviewed,
    shell: IToolShellRunner? = null,
    git: IToolGitService? = null,
    private val messenger: IToolAgentMessenger? = null,
    private val responder: IPermissionResponder,
    private val permissionReviewer: IPermissionReviewer? = null,
    private val eventSink: com.intatis.shared.IConversationEventSink,
    private val allowsShell: Boolean = true,
    private val maxIterations: Int = 8,
    systemPrompt: String? = null,
    private val imageGenerationToolService: ImageGenerationToolService? = null,
    private val capabilityLease: CapabilityLease = CapabilityLease.coordinator(),
    private val workspaceLease: WorkspaceLease = WorkspaceLease(
        rootPath = workspaceRoot,
        access = com.intatis.shared.protocol.WorkspaceAccess.ReadWrite,
    ),
) {
    private val systemPromptText =
        systemPrompt ?: "You are a code assistant. Operate inside workspace: $workspaceRoot. Keep tool usage deterministic and short."
    private val assistantRulesText = "Use tools when helpful. Never guess file content."
    private val shellRunner: IToolShellRunner = shell ?: ProcessShellRunner()
    private val gitService: IToolGitService = git ?: ProcessGitService(shellRunner)
    private val contextBuilder = ContextBuilder(systemPromptText, assistantRulesText)
    private val messages = contextBuilder.createBaseContext()
    private val imageGenerator: ImageGenerationToolService = imageGenerationToolService ?: ProviderImageGenerationToolService(ProviderRegistry(config))
    private val loop = AgentLoop(
        config = config,
        permissionProfile = permissionProfile,
        shell = shellRunner,
        git = gitService,
        responder = responder,
        permissionReviewer = permissionReviewer,
        eventSink = eventSink,
        allowsShell = allowsShell,
        maxIterations = maxIterations,
        messenger = messenger,
        imageGenerator = imageGenerator,
        workspaceRoot = workspaceRoot,
        agentName = name,
        capabilityLease = capabilityLease,
        workspaceLease = workspaceLease,
    )

    val toolNames: List<String>
        get() = loop.toolNames

    fun clear() {
        messages.clear()
        messages.addAll(contextBuilder.createBaseContext())
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
    ): Triple<String, Duration, String?> = loop.runAsync(
        messages = messages,
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
