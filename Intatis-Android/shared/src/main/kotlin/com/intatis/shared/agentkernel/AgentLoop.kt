package com.intatis.shared.agentkernel

import com.intatis.shared.ConversationEventKinds
import com.intatis.shared.ConversationEventPayloads
import com.intatis.shared.IPermissionResponder
import com.intatis.shared.IPermissionReviewer
import com.intatis.shared.PermissionContext
import com.intatis.shared.PermissionEngine
import com.intatis.shared.PermissionProfile
import com.intatis.shared.PermissionRequest
import com.intatis.shared.ToolArgs
import com.intatis.shared.ToolCallContext
import com.intatis.shared.ToolContext
import com.intatis.shared.ToolRegistry
import com.intatis.shared.IConversationEventSink
import com.intatis.shared.ITool
import com.intatis.shared.IToolAgentMessenger
import com.intatis.shared.IToolGitService
import com.intatis.shared.IToolShellRunner
import com.intatis.shared.IntatisConfig
import com.intatis.shared.ImageAttachment
import com.intatis.shared.OpenAIClient
import com.intatis.shared.WorkspaceSecurity
import com.intatis.shared.PermissionDecision
import com.intatis.shared.conversation.RuntimeErrorPresentation
import com.intatis.shared.protocol.CapabilityLease
import com.intatis.shared.protocol.WorkspaceLease
import com.intatis.shared.protocol.ToolCapability
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.time.Duration
import java.util.UUID

class AgentLoop(
    private val config: IntatisConfig,
    private val permissionProfile: PermissionProfile,
    private val shell: IToolShellRunner,
    private val git: IToolGitService,
    private val responder: IPermissionResponder,
    private val permissionReviewer: IPermissionReviewer? = null,
    private val eventSink: IConversationEventSink,
    private val allowsShell: Boolean,
    private val maxIterations: Int,
    private val messenger: IToolAgentMessenger?,
    private val imageGenerator: com.intatis.shared.ImageGenerationToolService?,
    private val workspaceRoot: String,
    private val agentName: String,
    private val capabilityLease: CapabilityLease,
    private val workspaceLease: WorkspaceLease,
) {
    private val toolCallingProvider = ToolCallingProvider(config)
    private val permissionEngine = PermissionEngine(permissionReviewer)

    private val toolRegistry: ToolRegistry by lazy {
        if (messenger == null) {
            ToolRegistry.standard()
        } else {
            ToolRegistry.standardWithAgentTools(messenger)
        }
    }

    val toolNames: List<String>
        get() = toolRegistry.descriptors.map { it.name }

    suspend fun runAsync(
        messages: MutableList<OpenAIClient.OpenAIChatMessage>,
        userText: String,
        model: String? = null,
        reasoning: String? = null,
        attachments: List<ImageAttachment>? = null,
        includeUsage: Boolean? = null,
        to: String? = null,
        tags: List<String>? = null,
        userGoal: String? = null,
        currentTaskID: String? = null,
    ): Triple<String, Duration, String?> = withContext(Dispatchers.Default) {
        messages.add(OpenAIClient.OpenAIChatMessage("user", userText, images = attachments))
        val userMessageId = UUID.randomUUID().toString()
        runCatching {
            eventSink.appendAsync(
                ConversationEventKinds.UserMessage,
                ConversationEventPayloads.userMessage(
                    messageId = userMessageId,
                    text = userText,
                    attachments = attachments?.map { it.name },
                    role = "user",
                    to = to,
                    tags = tags,
                    goal = userGoal,
                ),
            )
        }

        var totalLatency = Duration.ZERO
        var usage: String? = null
        val resolvedIncludeUsage = includeUsage ?: config.includeUsage

        for (iteration in 0 until maxIterations.coerceAtLeast(1)) {
            val result = toolCallingProvider.sendAsync(
                messages,
                toolRegistry.descriptors,
                model,
                reasoning,
                includeUsage = resolvedIncludeUsage,
            )
            totalLatency = totalLatency.plus(result.latency)
            usage = usage ?: (result.usage ?: "")

            val response = result.text
            val calls = result.toolCalls

            if (calls.isEmpty()) {
                messages.add(OpenAIClient.OpenAIChatMessage("assistant", response))
                val assistantMessageId = UUID.randomUUID().toString()
                runCatching {
                    eventSink.appendAsync(
                        ConversationEventKinds.MessageDelta,
                        ConversationEventPayloads.messageDelta(
                            messageId = assistantMessageId,
                            role = "assistant",
                            textDelta = response,
                            agent = agentName,
                            to = to,
                            goal = userGoal,
                        ),
                    )
                    eventSink.appendAsync(
                        ConversationEventKinds.MessageCompleted,
                        ConversationEventPayloads.messageCompleted(
                            messageId = assistantMessageId,
                            role = "assistant",
                            text = response,
                            agent = agentName,
                            to = to,
                            goal = userGoal,
                        ),
                    )
                }
                return@withContext Triple(response, totalLatency, usage)
            }

            messages.add(
                OpenAIClient.OpenAIChatMessage(
                    role = "assistant",
                    content = response.ifBlank { null },
                    toolCalls = result.toolCalls,
                ),
            )

            for (toolCall in calls) {
                runCatching {
                    eventSink.appendAsync(
                        ConversationEventKinds.ToolCall,
                        ConversationEventPayloads.toolCall(toolCall.id, agentName, toolCall.name, toolCall.arguments),
                    )
                }

                val observation = executeToolAsync(toolCall, userGoal, currentTaskID)
                messages.add(
                    OpenAIClient.OpenAIChatMessage("tool", observation, toolCallId = toolCall.id),
                )

                runCatching {
                    eventSink.appendAsync(
                        ConversationEventKinds.ToolResult,
                        ConversationEventPayloads.toolResult(toolCall.id, observation),
                    )
                }
            }
        }

        val timeoutText = "tool loop reached max iterations"
        messages.add(OpenAIClient.OpenAIChatMessage("assistant", timeoutText))
        val timeoutMessageId = UUID.randomUUID().toString()
        runCatching {
            eventSink.appendAsync(
                ConversationEventKinds.MessageCompleted,
                ConversationEventPayloads.messageCompleted(
                    messageId = timeoutMessageId,
                    role = "assistant",
                    text = timeoutText,
                    agent = agentName,
                    to = to,
                    goal = userGoal,
                ),
            )
        }

        return@withContext Triple(timeoutText, totalLatency, usage)
    } catch (ex: Exception) {
        RuntimeErrorPresentation.emit(ex, eventSink)
        throw ex
    }

    private suspend fun executeToolAsync(
        toolCall: OpenAIClient.ToolCall,
        userGoal: String? = null,
        currentTaskID: String? = null,
    ): String {
        val descriptor = toolRegistry.tool(toolCall.name) ?: return "unknown tool: ${toolCall.name}"
        if (!isToolAllowed(toolCall.name)) {
            runCatching {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = agentName,
                        tool = toolCall.name,
                        reason = "tool not in capability lease",
                    ),
                )
            }
            return "tool blocked: lease denied: ${toolCall.name}"
        }

        val args = ToolArgs(toolCall.arguments)
        val touchedPaths = runCatching {
            descriptor.touchedPaths(args).map { path -> WorkspaceSecurity.resolveInWorkspace(workspaceRoot, path) }
        }.getOrElse { ex -> return "tool blocked: ${ex.message}" }

        val toolContext = ToolContext(
            workspaceRoot = workspaceRoot,
            agentName = agentName,
            shell = shell,
            git = git,
            currentTaskID = currentTaskID,
            messenger = messenger,
            imageGenerator = imageGenerator,
        )

        val callContext = ToolCallContext(
            toolName = toolCall.name,
            sideEffect = descriptor.descriptor.sideEffect,
            touchedPaths = touchedPaths,
            risksNetwork = descriptor.risksNetwork(args, toolContext),
            rawArgs = toolCall.arguments,
        )

        val permissionContext = PermissionContext(
            workspaceRoot = workspaceRoot,
            profile = permissionProfile,
            allowsShell = allowsShell,
            userGoal = userGoal,
            agent = agentName,
            workspaceLease = workspaceLease,
        )

        val decision = permissionEngine.decideAsync(callContext, permissionContext)
        if (decision.reviewedByModel) {
            runCatching {
                eventSink.appendAsync(
                    ConversationEventKinds.PermissionReview,
                    ConversationEventPayloads.permissionReview(
                        toolCall.name,
                        decision.decision,
                        decision.risk,
                        decision.reason,
                        "model",
                        agentName,
                    ),
                )
            }
        }

        if (decision.decision != PermissionDecision.Allow) {
            val finalDecision = if (decision.decision == PermissionDecision.AskUser) {
                val request = PermissionRequest(
                    requestId = UUID.randomUUID().toString(),
                    tool = toolCall.name,
                    args = toolCall.arguments,
                    risk = decision.risk,
                    reason = decision.reason,
                    agent = agentName,
                )
                runCatching {
                    eventSink.appendAsync(
                        ConversationEventKinds.PermissionRequest,
                        ConversationEventPayloads.permissionRequest(
                            request.requestId,
                            request.agent,
                            request.tool,
                            request.args,
                            request.risk,
                            request.reason,
                        ),
                    )
                }

                val approved = responder.requestApprovalAsync(request)
                runCatching {
                    eventSink.appendAsync(
                        ConversationEventKinds.PermissionResolved,
                        ConversationEventPayloads.permissionResolved(
                            request.requestId,
                            request.tool,
                            approved,
                            request.risk,
                            if (approved == PermissionDecision.Allow) "user approved" else "user denied",
                            request.agent,
                        ),
                    )
                }
                approved
            } else {
                PermissionDecision.Deny
            }

            if (finalDecision != PermissionDecision.Allow) {
                return "permission denied: ${decision.reason}"
            }
        }

        return runCatching {
            descriptor.executeAsync(args, toolContext)
        }.fold(
            onSuccess = { observation ->
                if (!observation.changedFiles.isNullOrEmpty()) {
                    runCatching {
                        eventSink.appendAsync(
                            ConversationEventKinds.PatchProposed,
                            ConversationEventPayloads.patchProposed(
                                patchId = "patch-${UUID.randomUUID()}",
                                agent = agentName,
                                files = observation.changedFiles,
                                diff = observation.diff ?: observation.text,
                            ),
                        )
                    }
                }
                observation.text
            },
            onFailure = { ex ->
                "tool error: ${ex.message}"
            },
        )
    }

    private fun isToolAllowed(toolName: String): Boolean {
        val required = when (toolName) {
            "read_file" -> ToolCapability.readWorkspace
            "list_files" -> ToolCapability.listWorkspace
            "search_text" -> ToolCapability.searchWorkspace
            "git_status" -> ToolCapability.readWorkspace
            "git_diff" -> ToolCapability.readWorkspace
            "run_shell" -> ToolCapability.runShell
            "write_file" -> ToolCapability.applyPatch
            "apply_patch" -> ToolCapability.applyPatch
            "read_pdf" -> ToolCapability.readPDF
            "edit_pdf_pages" -> ToolCapability.editPDF
            "reconstruct_document_image" -> ToolCapability.reconstructDocument
            "compile_latex" -> ToolCapability.compileLaTeX
            "generate_image" -> ToolCapability.generateMedia
            "web_fetch" -> ToolCapability.browseWeb
            "browser_diagnostics",
            "browser_profiles",
            "browser_profile_delete",
            "browser_history",
            "browser_navigate",
            "browser_snapshot",
            "browser_handoff",
            "browser_click",
            "browser_reload",
            "browser_back",
            "browser_forward",
            "browser_type",
            "browser_submit",
            "browser_select_option",
            "browser_press_key",
            "browser_scroll",
            "browser_wait",
            "browser_screenshot",
            "browser_upload_file",
            "browser_download",
            "browser_downloads",
            "browser_search" -> ToolCapability.browseWeb
            "ask_agent" -> ToolCapability.sendMessage
            "send_message" -> ToolCapability.sendMessage
            "request_information" -> ToolCapability.requestInformation
            "reply_message" -> ToolCapability.replyMessage
            "request_delegation" -> ToolCapability.requestDelegation
            "delegate_task" -> ToolCapability.delegateTask
            "spawn_agent" -> ToolCapability.attachWorkspace
            "list_agents" -> ToolCapability.delegateTask
            "remove_agent" -> ToolCapability.delegateTask
            else -> null
        } ?: return false

        if (required == ToolCapability.requestDelegation && !capabilityLease.canRequestDelegation()) return false
        if (required == ToolCapability.delegateTask && !capabilityLease.canDelegateTask()) return false
        return required in capabilityLease.tools
    }
}
