package com.intatis.shared.session

import com.intatis.shared.log.ConversationEventKinds
import com.intatis.shared.log.ConversationEventPayloads
import com.intatis.shared.log.ConversationEventSink
import com.intatis.shared.log.NullConversationEventSink
import com.intatis.shared.model.IntatisConfig
import com.intatis.shared.provider.OpenAIClient
import com.intatis.shared.provider.OpenAIChatMessage
import com.intatis.shared.provider.ToolCall
import com.intatis.shared.security.AllowAllResponder
import com.intatis.shared.security.PermissionContext
import com.intatis.shared.security.PermissionDecision
import com.intatis.shared.security.PermissionEngine
import com.intatis.shared.security.PermissionOutcome
import com.intatis.shared.security.PermissionProfile
import com.intatis.shared.security.PermissionRequest
import com.intatis.shared.security.PermissionResponder
import com.intatis.shared.security.PermissionReviewer
import com.intatis.shared.security.SideEffect
import com.intatis.shared.security.ToolCallContext
import com.intatis.shared.tools.*
import com.intatis.shared.tools.ToolCallContext as ToolCallContextData
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.time.Duration
import java.util.UUID

class CodeAgentSession(
    private val config: IntatisConfig,
    private val workspaceRoot: String,
    private val agentName: String = "agent",
    private val permissionProfile: PermissionProfile = PermissionProfile.REVIEWED,
    private val shell: ToolShellRunner = ProcessShellRunner(),
    private val git: ToolGitService = ProcessGitService(shell),
    private val responder: PermissionResponder = AllowAllResponder(),
    private val permissionReviewer: PermissionReviewer? = null,
    private val eventSink: ConversationEventSink = NullConversationEventSink(),
    private val allowsShell: Boolean = true,
    private val maxIterations: Int = 8,
    private val systemPrompt: String? = null,
) {
    private val provider = OpenAIClient(config)
    private val permissionEngine = PermissionEngine(permissionReviewer)
    private val messages = mutableListOf<OpenAIChatMessage>()
    private val basePrompt =
        "You are a code assistant. Operate inside workspace: $workspaceRoot. Keep tool usage deterministic and short."

    init {
        messages.add(OpenAIChatMessage("system", systemPrompt ?: basePrompt))
        messages.add(OpenAIChatMessage("system", "Use tools when helpful. Never guess file content."))
    }

    fun clear() {
        val system = messages.filter { it.role == "system" }
        messages.clear()
        messages.addAll(system)
    }

    suspend fun sendAsync(
        userText: String,
        model: String?,
        reasoning: String?,
        userGoal: String?,
    ): Triple<String, Duration, String?> = withContext(Dispatchers.IO) {
        messages.add(OpenAIChatMessage("user", userText))

        var totalMs = 0L
        var usage: String? = null

        repeat(maxIterations) {
            val result = provider.sendWithToolsAsync(
                messages,
                toolRegistry().descriptors(),
                model,
                reasoning,
                includeUsage = true,
            )
            totalMs += result.latencyMs
            if (usage == null) usage = result.usage

            if (result.toolCalls.isEmpty()) {
                messages.add(OpenAIChatMessage("assistant", result.text))
                return@withContext Triple(result.text, Duration.ofMillis(totalMs), usage)
            }

            messages.add(OpenAIChatMessage("assistant", result.text.ifBlank { null }, result.toolCalls))

            for (call in result.toolCalls) {
                eventSink.appendAsync(
                    ConversationEventKinds.ToolCall,
                    ConversationEventPayloads.toolCall(call.id, agentName, call.name, call.arguments),
                )

                val observation = executeToolAsync(call, userGoal)
                messages.add(OpenAIChatMessage("tool", observation, toolCallId = call.id))

                eventSink.appendAsync(
                    ConversationEventKinds.ToolResult,
                    ConversationEventPayloads.toolResult(call.id, observation),
                )
            }
        }

        val timedOut = "tool loop reached max iterations."
        messages.add(OpenAIChatMessage("assistant", timedOut))
        Triple(timedOut, Duration.ofMillis(totalMs), usage)
    }

    private suspend fun executeToolAsync(call: ToolCall, userGoal: String?): String {
        val registry = toolRegistry()
        val tool = registry.tool(call.name) ?: return "unknown tool: ${call.name}"
        val args = ToolArgs(call.arguments)

        val touched = try {
            tool.touchedPaths(args).map { it ->
                com.intatis.shared.security.WorkspaceSecurity.resolveInWorkspace(workspaceRoot, it)
            }
        } catch (ex: Exception) {
            return "tool blocked: cannot resolve touched paths (${ex.message})"
        }

        val callContext = ToolCallContext(
            toolName = call.name,
            sideEffect = tool.descriptor.sideEffect,
            touchedPaths = touched,
            risksNetwork = tool.risksNetwork(args),
            rawArgs = call.arguments,
        )

        val permissionContext = PermissionContext(
            workspaceRoot = workspaceRoot,
            profile = permissionProfile,
            allowsShell = allowsShell,
            userGoal = userGoal,
            agent = agentName,
        )

        val outcome = permissionEngine.decideAsync(callContext, permissionContext)
        if (outcome.reviewedByModel) {
            eventSink.appendAsync(
                ConversationEventKinds.PermissionReview,
                ConversationEventPayloads.permissionReview(
                    tool.descriptor.name,
                    agentName,
                    outcome.decision,
                    outcome.risk,
                    outcome.reason,
                    "model",
                ),
            )
        }

        if (outcome.decision == PermissionDecision.ASK_USER) {
            val final = resolvePermissionAsync(outcome, tool, call)
            if (final != PermissionDecision.ALLOW) {
                return "permission denied: ${outcome.reason}"
            }
        } else if (outcome.decision == PermissionDecision.DENY) {
            return "permission denied: ${outcome.reason}"
        }

        return runCatching {
            val observation = tool.execute(args, ToolContext(workspaceRoot, agentName, shell, git))
            if (!observation.changedFiles.isNullOrEmpty()) {
                eventSink.appendAsync(
                    ConversationEventKinds.PatchProposed,
                    ConversationEventPayloads.patchProposed(
                        "patch-${UUID.randomUUID()}",
                        agentName,
                        observation.changedFiles,
                        observation.diff ?: "",
                    ),
                )
            }
            observation.text
        }.getOrElse { "tool error: ${it.message}" }
    }

    private suspend fun resolvePermissionAsync(
        outcome: PermissionOutcome,
        tool: ITool,
        call: ToolCall,
    ): PermissionDecision {
        val requestId = UUID.randomUUID().toString()
        eventSink.appendAsync(
            ConversationEventKinds.PermissionRequest,
            ConversationEventPayloads.permissionRequest(
                requestId,
                agentName,
                tool.descriptor.name,
                call.arguments,
                outcome.risk,
                outcome.reason,
            ),
        )

        val final = responder.requestApprovalAsync(
            PermissionRequest(
                requestId = requestId,
                tool = tool.descriptor.name,
                args = call.arguments,
                risk = outcome.risk,
                reason = outcome.reason,
                agent = agentName,
            ),
        )

        eventSink.appendAsync(
            ConversationEventKinds.PermissionResolved,
            ConversationEventPayloads.permissionResolved(
                requestId,
                tool.descriptor.name,
                agentName,
                final,
                outcome.risk,
                if (final == PermissionDecision.ALLOW) "user approved" else "user denied",
            ),
        )
        return final
    }

    private fun toolRegistry(): ToolRegistry =
        if (eventSink is NullConversationEventSink) {
            ToolRegistry.standard()
        } else {
            ToolRegistry.standardWithAskAgent(null)
        }
}
