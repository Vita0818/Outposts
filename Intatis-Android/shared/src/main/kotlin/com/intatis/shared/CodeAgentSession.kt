package com.intatis.shared

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.time.Duration

class CodeAgentSession(
    private val config: IntatisConfig,
    private val workspaceRoot: String,
    private val agentName: String = "agent",
    private val permissionProfile: PermissionProfile = PermissionProfile.Reviewed,
    shell: IToolShellRunner? = null,
    git: IToolGitService? = null,
    private val messenger: IToolAgentMessenger? = null,
    private val responder: IPermissionResponder = AllowAllResponder(),
    private val permissionReviewer: IPermissionReviewer? = null,
    private val eventSink: IConversationEventSink = NullConversationEventSink(),
    private val allowsShell: Boolean = true,
    private val maxIterations: Int = 8,
    private val systemPrompt: String? = null,
) {
    private val client = OpenAIClient(config)
    private val shellRunner: IToolShellRunner = shell ?: ProcessShellRunner()
    private val gitService: IToolGitService = git ?: ProcessGitService(shellRunner)
    private val permissionEngine = PermissionEngine(permissionReviewer)
    private val messages = mutableListOf<OpenAIClient.OpenAIChatMessage>()

    val toolNames: List<String>
        get() = toolRegistry.descriptors.map { it.name }

    init {
        val resolvedSystemPrompt = systemPrompt ?: "You are a code assistant. Operate inside workspace: $workspaceRoot. Keep tool usage deterministic and short."
        messages.add(OpenAIClient.OpenAIChatMessage("system", resolvedSystemPrompt))
        messages.add(OpenAIClient.OpenAIChatMessage("system", "Use tools when helpful. Never guess file content."))
    }

    fun clear() {
        val basePrompt = messages.take(2)
        messages.clear()
        messages.addAll(basePrompt)
    }

    suspend fun sendAsync(
        userText: String,
        model: String? = null,
        reasoning: String? = null,
        userGoal: String? = null,
    ): Triple<String, Duration, String?> = withContext(Dispatchers.Default) {
        messages.add(OpenAIClient.OpenAIChatMessage("user", userText))

        var totalLatency = Duration.ZERO
        var usage: String? = null

        for (iteration in 0 until maxIterations.coerceAtLeast(1)) {
            val result = client.sendWithToolsAsync(
                messages,
                toolRegistry.descriptors,
                model,
                reasoning,
                includeUsage = true,
            )
            totalLatency = totalLatency.plusMillis(result.third)
            usage = usage ?: ""

            val response = result.first
            val calls = result.second

            if (calls.isEmpty()) {
                messages.add(OpenAIClient.OpenAIChatMessage("assistant", response))
                return@withContext Triple(response, totalLatency, usage)
            }

            messages.add(
                OpenAIClient.OpenAIChatMessage(
                    role = "assistant",
                    content = response.ifBlank { null },
                    toolCalls = calls,
                )
            )

            for (toolCall in calls) {
                eventSink.appendAsync(
                    ConversationEventKinds.ToolCall,
                    ConversationEventPayloads.toolCall(toolCall.id, agentName, toolCall.name, toolCall.arguments)
                )

                val observation = executeToolAsync(toolCall)
                messages.add(
                    OpenAIClient.OpenAIChatMessage(
                        role = "tool",
                        content = observation,
                        toolCallId = toolCall.id,
                    )
                )

                eventSink.appendAsync(
                    ConversationEventKinds.ToolResult,
                    ConversationEventPayloads.toolResult(toolCall.id, observation)
                )
            }
        }

        val timeoutText = "tool loop reached max iterations"
        messages.add(OpenAIClient.OpenAIChatMessage("assistant", timeoutText))
        return@withContext Triple(timeoutText, totalLatency, usage)
    }

    private suspend fun executeToolAsync(toolCall: OpenAIClient.ToolCall): String {
        val descriptor = toolRegistry.tool(toolCall.name)
            ?: return "unknown tool: ${toolCall.name}"

        val args = ToolArgs(toolCall.arguments)
        val touchedPaths = descriptor.touchedPaths(args).map { WorkspaceSecurity.resolveInWorkspace(workspaceRoot, it) }

        val callContext = ToolCallContext(
            toolName = toolCall.name,
            sideEffect = descriptor.descriptor.sideEffect,
            touchedPaths = touchedPaths,
            risksNetwork = descriptor.risksNetwork(args),
            rawArgs = toolCall.arguments,
        )

        val pContext = PermissionContext(
            workspaceRoot = workspaceRoot,
            profile = permissionProfile,
            allowsShell = allowsShell,
            userGoal = userGoal,
            agent = agentName,
        )

        val decision = permissionEngine.decideAsync(callContext, pContext)
        if (decision.decision == PermissionDecision.Allow) {
            eventSink.appendAsync(
                ConversationEventKinds.PermissionReview,
                ConversationEventPayloads.permissionReview(toolCall.name, decision.decision, decision.risk, decision.reason, "policy")
            )
        }

        if (decision.decision != PermissionDecision.Allow) {
            val finalDecision = if (decision.decision == PermissionDecision.AskUser) {
                val req = PermissionRequest(
                    requestId = java.util.UUID.randomUUID().toString(),
                    tool = toolCall.name,
                    args = toolCall.arguments,
                    risk = decision.risk,
                    reason = decision.reason,
                    agent = agentName,
                )
                eventSink.appendAsync(ConversationEventKinds.PermissionRequest, ConversationEventPayloads.permissionRequest(
                    req.requestId,
                    req.agent,
                    req.tool,
                    req.args,
                    req.risk,
                    req.reason
                ))

                val approved = responder.requestApprovalAsync(req)
                eventSink.appendAsync(
                    ConversationEventKinds.PermissionResolved,
                    ConversationEventPayloads.permissionResolved(req.requestId, req.tool, approved, req.risk, if (approved == PermissionDecision.Allow) "user approved" else "user denied", req.agent)
                )
                approved
            } else {
                PermissionDecision.Deny
            }

            if (finalDecision != PermissionDecision.Allow) {
                return "permission denied: ${decision.reason}"
            }
        }

        val toolContext = ToolContext(
            workspaceRoot = workspaceRoot,
            agentName = agentName,
            shell = shellRunner,
            git = gitService,
            messenger = messenger,
        )

        val observation = try {
            descriptor.executeAsync(args, toolContext)
        } catch (ex: Exception) {
            return "tool error: ${ex.message}"
        }

        if (!observation.changedFiles.isNullOrEmpty()) {
            eventSink.appendAsync(
                ConversationEventKinds.PatchProposed,
                ConversationEventPayloads.patchProposed(
                    "patch-${java.util.UUID.randomUUID()}",
                    agentName,
                    observation.changedFiles,
                    observation.diff ?: observation.text,
                )
            )
        }

        return observation.text
    }

    private val toolRegistry: ToolRegistry by lazy {
        if (messenger == null) {
            ToolRegistry.standard()
        } else {
            ToolRegistry.standard().add(listOf(AskAgentTool()))
        }
    }
}
