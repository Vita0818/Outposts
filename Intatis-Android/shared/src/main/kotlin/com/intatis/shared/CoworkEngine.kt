package com.intatis.shared

import com.intatis.shared.protocol.CapabilityLease
import com.intatis.shared.protocol.CommunicationGrant
import com.intatis.shared.protocol.DelegationGrant
import com.intatis.shared.protocol.WorkspaceAccess
import com.intatis.shared.protocol.WorkspaceLease
import java.io.File
import kotlin.random.Random

class CoworkEngine(
    private val config: IntatisConfig,
    private val baseWorkspace: String,
    shell: IToolShellRunner? = null,
    git: IToolGitService? = null,
    private val responder: IPermissionResponder = AllowAllResponder(),
    private val profile: PermissionProfile = PermissionProfile.Reviewed,
    private val eventSink: IConversationEventSink = NullConversationEventSink(),
    private val messageBus: CoworkMessageBus = CoworkMessageBus(),
    private val allowsShell: Boolean = true,
    private val maxIterations: Int = 8,
    private val permissionReviewer: IPermissionReviewer? = null,
) {
    private val agents = CoworkAgentRegistry()
    private val messageBuffer = mutableListOf<Pair<String, String>>()
    private val defaultResponder = responder
    private val permissionResponder = SwappablePermissionResponder(responder)
    private val shellRunner: IToolShellRunner = shell ?: ProcessShellRunner()
    private val gitService: IToolGitService = git ?: ProcessGitService(shellRunner)

    val agentsNames: List<String>
        get() = agents.names

    fun send(text: String, targetAgent: String?, model: String? = null, reasoning: String? = null): String {
        return runBlockingOrNull {
            sendAsync(text, targetAgent, model, reasoning)
        } ?: "execution interrupted"
    }

    suspend fun sendAsync(
        text: String,
        targetAgent: String?,
        model: String? = null,
        reasoning: String? = null,
        images: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
    ): String {
        if (agents.isEmpty()) return "(no agents attached)"
        val selected = resolveAgent(targetAgent)
            ?: return "no such agent: $targetAgent"

        val selectedModel = model ?: selected.model
        val response = selected.session.sendAsync(
            userText = text,
            model = selectedModel,
            reasoning = reasoning,
            userGoal = "cowork",
            attachments = images,
            to = targetAgent,
            tags = targetAgent?.let { listOf(it) },
            includeUsage = includeUsage,
        )

        messageBuffer.add(selected.name to text)
        messageBuffer.add(selected.name to response.first)
        return response.first
    }

    suspend fun askAsync(
        from: String,
        to: String,
        question: String,
        userGoal: String? = null,
    ): String = askCoreAsync(
        from = from,
        to = to,
        question = question,
        userGoal = userGoal ?: "ask_agent",
        images = emptyList(),
        includeUsage = false,
        replyTo = if (from.isBlank()) "unknown" else from,
    )

    suspend fun askAsync(
        from: String,
        to: String,
        question: String,
        userGoal: String? = null,
        images: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
    ): String {
        return askCoreAsync(
            from = from,
            to = to,
            question = question,
            userGoal = userGoal ?: "ask_agent",
            images = images,
            includeUsage = includeUsage,
            replyTo = if (from.isBlank()) "unknown" else from,
        )
    }

    suspend fun askTaskAsync(
        from: String,
        to: String,
        question: String,
        userGoal: String? = null,
        images: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
        taskID: String? = null,
    ): String = askCoreAsync(
        from = from,
        to = to,
        question = question,
        userGoal = userGoal ?: "ask_agent",
        images = images,
        includeUsage = includeUsage,
        taskID = taskID,
        replyTo = null,
    )

    private suspend fun askCoreAsync(
        from: String,
        to: String,
        question: String,
        userGoal: String? = null,
        images: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
        replyTo: String? = null,
        taskID: String? = null,
    ): String {
        val sender = if (from.isBlank()) "unknown" else from
        val senderState = resolveAgent(sender)
        if (senderState != null && !senderState.capabilityLease.canSendMessage(sender, to)) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = sender,
                        tool = "ask_agent",
                        reason = "tool not in capability lease",
                        target = to,
                    ),
                )
            }
            return "tool blocked: lease denied: ask_agent"
        }

        val target = resolveAgent(to) ?: return "no such agent: $to"
        if (sender.equals(target.name, ignoreCase = true)) return "self-targeted ask is blocked."

        val delivered = messageBus.deliver(sender, target.name, "[$sender] $question")
            ?: return "the reply was blocked by the mediator"
        val answer = target.session.sendAsync(
            userText = delivered,
            model = target.model,
            reasoning = null,
            userGoal = userGoal ?: "ask_agent",
            attachments = images,
            to = to,
            tags = listOf(to),
            includeUsage = includeUsage,
            currentTaskID = taskID,
        )
        messageBuffer.add(target.name to answer.first)
        if (replyTo == null) return answer.first
        return messageBus.deliver(target.name, replyTo, answer.first)
            ?: "the reply was blocked by the mediator"
    }

    suspend fun sendMessageAsync(
        from: String,
        to: String,
        content: String,
    ): String {
        val sender = if (from.isBlank()) "unknown" else from
        val senderState = resolveAgent(sender)
        if (senderState != null && !senderState.capabilityLease.canSendMessage(sender, to)) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = sender,
                        tool = "send_message",
                        reason = "tool not in capability lease",
                        target = to,
                    ),
                )
            }
            return "tool blocked: lease denied: send_message"
        }

        val target = resolveAgent(to) ?: return "no such agent: $to"
        if (sender.equals(target.name, ignoreCase = true)) return "error: agent cannot message itself"

        messageBus.sendMessage(sender, target.name, content)
            ?: return "your message was blocked by the mediator"
        return "sent message to @${target.name}"
    }

    suspend fun requestInformation(
        from: String,
        to: String,
        question: String,
        taskID: String? = null,
        reason: String = "request information",
    ): String {
        val sender = if (from.isBlank()) "unknown" else from
        val senderState = resolveAgent(sender)
        if (senderState != null && !senderState.capabilityLease.canSendMessage(sender, to)) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = sender,
                        tool = "request_information",
                        reason = "tool not in capability lease",
                        target = to,
                    ),
                )
            }
            return "tool blocked: lease denied: request_information"
        }

        val target = resolveAgent(to) ?: return "no such agent: $to"
        if (sender.equals(target.name, ignoreCase = true)) return "error: agent cannot request information from itself"

        messageBus.requestInformation(sender, target.name, question, taskID)
            ?: return "your information request was blocked by the mediator"
        return "requested information from @${target.name}"
    }

    suspend fun replyMessage(
        from: String,
        to: String,
        answer: String,
        inReplyTo: String? = null,
        taskID: String? = null,
    ): String {
        val sender = if (from.isBlank()) "unknown" else from
        val senderState = resolveAgent(sender)
        if (senderState != null && !senderState.capabilityLease.canSendMessage(sender, to)) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = sender,
                        tool = "reply_message",
                        reason = "tool not in capability lease",
                        target = to,
                    ),
                )
            }
            return "tool blocked: lease denied: reply_message"
        }

        val target = resolveAgent(to) ?: return "no such agent: $to"
        if (sender.equals(target.name, ignoreCase = true)) return "self-targeted reply is blocked."

        return messageBus.replyMessage(sender, target.name, "[$sender] $answer", inReplyTo, taskID)
            ?: "your message was blocked by the mediator"
    }

    suspend fun requestDelegation(
        from: String,
        objective: String,
        reason: String = "delegation requested",
    ): String {
        val sender = if (from.isBlank()) "unknown" else from
        val normalizedObjective = objective.trim().ifEmpty { "Additional help requested." }
        val normalizedReason = reason.trim().ifEmpty { "No reason supplied." }
        val senderState = resolveAgent(sender)
        if (senderState != null && !senderState.capabilityLease.canRequestDelegation()) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = sender,
                        tool = "request_delegation",
                        reason = "delegation request not permitted",
                    ),
                )
            }
            return "tool blocked: lease denied: request_delegation"
        }
        if (senderState != null && !senderState.capabilityLease.canUseDelegationBudget()) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = sender,
                        tool = "request_delegation",
                        reason = "delegation budget exhausted",
                        target = null,
                    ),
                )
            }
            return "tool blocked: lease denied: request_delegation"
        }
        val delegatedLease = senderState?.capabilityLease?.consumeDelegationBudget()

        val result = messageBus.requestDelegation(sender, "[$sender] $normalizedObjective", normalizedReason)
            ?: return "your message was blocked by the mediator"
        if (senderState != null && delegatedLease != null) {
            senderState.capabilityLease = delegatedLease
        }
        return result
    }

    suspend fun delegateTask(
        from: String,
        to: String,
        objective: String,
        reason: String = "delegation requested",
        roleHint: String = "",
        expectedDeliverable: String = "",
        taskID: String? = null,
    ): String {
        val sender = if (from.isBlank()) "unknown" else from
        val senderState = resolveAgent(sender)
        if (senderState != null && !senderState.capabilityLease.canSendMessage(sender, to)) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = sender,
                        tool = "delegate_task",
                        reason = "tool not in capability lease",
                        target = to,
                    ),
                )
            }
            return "tool blocked: lease denied: delegate_task"
        }
        val delegatedLease = senderState?.capabilityLease?.consumeForDelegationOrNull("delegate_task")
        if (senderState != null && delegatedLease == null) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.CapabilityLeaseBlocked,
                    ConversationEventPayloads.capabilityLeaseBlocked(
                        agent = sender,
                        tool = "delegate_task",
                        reason = "delegation not permitted",
                        target = to,
                    ),
                )
            }
            return "tool blocked: lease denied: delegate_task"
        }

        val target = resolveAgent(to) ?: return "no such agent: $to"
        if (sender.equals(target.name, ignoreCase = true)) return "self-targeted delegation is blocked."

        val result = messageBus.delegateTask(sender, target.name, "[$sender] $objective", reason)
            ?: return "your message was blocked by the mediator"
        if (senderState != null && delegatedLease != null) {
            senderState.capabilityLease = delegatedLease
        }
        // roleHint and expectedDeliverable are surfaced at orchestration time and are currently carried in task contracts.
        return result
    }

    fun attach(name: String, workspace: String? = null, model: String? = null, canCoordinate: Boolean = true): String {
        if (name.isBlank()) return "agent name is empty."
        val modelToUse = model?.ifBlank { config.model } ?: config.model
        runBlockingOrNull {
            eventSink.appendAsync(
                ConversationEventKinds.AgentAttachRequested,
                ConversationEventPayloads.agentAttachRequested(
                    agent = name,
                    path = workspace?.ifBlank { baseWorkspace } ?: baseWorkspace,
                    model = modelToUse,
                    profile = profile.name,
                ),
            )
        }
        if (agents.isReservedPermissionReviewer(name)) {
            return "$name is a reserved permission reviewer identity."
        }
        if (agents.contains(name)) {
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.WorkspaceLeaseDenied,
                    ConversationEventPayloads.workspaceLeaseDenied(
                        agent = name,
                        rootPath = workspace?.ifBlank { baseWorkspace } ?: baseWorkspace,
                        reason = "agent already exists",
                    ),
                )
            }
            return "$name already exists."
        }

        val requestedWorkspace = workspace?.ifBlank { baseWorkspace } ?: baseWorkspace
        val resolvedWorkspace = runCatching { WorkspaceTools.resolveWorkspace(config.workspace, requestedWorkspace) }.getOrElse { ex ->
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.WorkspaceLeaseDenied,
                    ConversationEventPayloads.workspaceLeaseDenied(
                        agent = name,
                        rootPath = requestedWorkspace,
                        reason = ex.message ?: "invalid workspace",
                    ),
                )
            }
            return "attach failed: ${ex.message}"
        }

        val workspaceLease = WorkspaceLease(
            rootPath = resolvedWorkspace,
            access = WorkspaceAccess.ReadWrite,
        )
        val capabilityLease = if (canCoordinate) CapabilityLease.coordinator() else CapabilityLease.worker()

        runBlockingOrNull {
            eventSink.appendAsync(
                ConversationEventKinds.WorkspaceLeaseRequested,
                ConversationEventPayloads.workspaceLeaseRequested(
                    agent = name,
                    rootPath = workspaceLease.rootPath,
                    access = workspaceLease.access,
                    reason = "attaching cowork agent",
                ),
            )
        }

        val state = createSessionForAgent(name, resolvedWorkspace, workspaceLease, capabilityLease, modelToUse)
        agents.register(state)

        runBlockingOrNull {
            eventSink.appendAsync(
                ConversationEventKinds.AgentAttached,
                ConversationEventPayloads.agentAttached(
                    agent = name,
                    path = workspaceLease.rootPath,
                model = modelToUse,
                profile = profile.name,
            ),
            )
            eventSink.appendAsync(
                ConversationEventKinds.WorkspaceLeaseGranted,
                ConversationEventPayloads.workspaceLeaseGranted(
                    agent = name,
                    lease = workspaceLease,
                ),
            )
            eventSink.appendAsync(
                ConversationEventKinds.CapabilityLeaseCreated,
                ConversationEventPayloads.capabilityLeaseCreated(
                    agent = name,
                    lease = capabilityLease,
                ),
            )
        }
        return "$name attached to $resolvedWorkspace."
    }

    fun enableAutomaticPermissionReview(model: String? = null): String {
        val reviewerIdentity = CoworkAgentRegistry.PermissionReviewerIdentity
        if (agents.isReservedPermissionReviewer(reviewerIdentity)) {
            return "$reviewerIdentity already enabled."
        }

        val modelToUse = model?.ifBlank { config.model } ?: config.model
        val requestedWorkspace = baseWorkspace
        val resolvedWorkspace = runCatching {
            WorkspaceTools.resolveWorkspace(config.workspace, requestedWorkspace)
        }.getOrElse { ex ->
            runBlockingOrNull {
                eventSink.appendAsync(
                    ConversationEventKinds.WorkspaceLeaseDenied,
                    ConversationEventPayloads.workspaceLeaseDenied(
                        agent = reviewerIdentity,
                        rootPath = requestedWorkspace,
                        reason = ex.message ?: "invalid workspace",
                    ),
                )
            }
            return "enable permission reviewer failed: ${ex.message}"
        }

        val workspaceLease = WorkspaceLease(
            rootPath = resolvedWorkspace,
            access = WorkspaceAccess.ReadOnly,
        )
        val capabilityLease = CapabilityLease(
            tools = emptySet(),
            communication = CommunicationGrant.none,
            communicationPayload = null,
            delegation = DelegationGrant.none,
            delegationBudget = null,
            expiresAtTaskCompletion = false,
        )

        runBlockingOrNull {
            eventSink.appendAsync(
                ConversationEventKinds.WorkspaceLeaseRequested,
                ConversationEventPayloads.workspaceLeaseRequested(
                    agent = reviewerIdentity,
                    rootPath = workspaceLease.rootPath,
                    access = workspaceLease.access,
                    reason = "attaching permission reviewer",
                ),
            )
        }

        val state = createSessionForAgent(
            reviewerIdentity,
            resolvedWorkspace,
            workspaceLease,
            capabilityLease,
            modelToUse,
            PermissionProfile.ReadOnly,
        )
        if (!agents.register(state)) {
            return "permission reviewer already enabled."
        }
        permissionResponder.replace(
            AgentPermissionResponder(
                config = config,
                fallbackResponder = defaultResponder,
                model = modelToUse,
            ),
        )

        runBlockingOrNull {
            eventSink.appendAsync(
                ConversationEventKinds.AgentAttached,
                ConversationEventPayloads.agentAttached(
                    agent = reviewerIdentity,
                    path = workspaceLease.rootPath,
                    model = modelToUse,
                    profile = PermissionProfile.ReadOnly.name,
                ),
            )
            eventSink.appendAsync(
                ConversationEventKinds.WorkspaceLeaseGranted,
                ConversationEventPayloads.workspaceLeaseGranted(
                    agent = reviewerIdentity,
                    lease = workspaceLease,
                ),
            )
            eventSink.appendAsync(
                ConversationEventKinds.CapabilityLeaseCreated,
                ConversationEventPayloads.capabilityLeaseCreated(
                    agent = reviewerIdentity,
                    lease = capabilityLease,
                ),
            )
        }
        return "automatic permission review enabled for @$reviewerIdentity."
    }

    fun disableAutomaticPermissionReview(): String {
        val reviewerIdentity = CoworkAgentRegistry.PermissionReviewerIdentity
        val removed = agents.unregister(reviewerIdentity) ?: return "$reviewerIdentity is not enabled."

        runBlockingOrNull {
            eventSink.appendAsync(
                ConversationEventKinds.AgentDetached,
                ConversationEventPayloads.agentDetached(removed.name),
            )
            eventSink.appendAsync(
                ConversationEventKinds.WorkspaceLeaseRevoked,
                ConversationEventPayloads.workspaceLeaseRevoked(
                    agent = removed.name,
                    workspaceLeaseId = removed.workspaceLease.id,
                    reason = "permission review disabled",
                ),
            )
            eventSink.appendAsync(
                ConversationEventKinds.CapabilityLeaseRevoked,
                ConversationEventPayloads.capabilityLeaseRevoked(
                    agent = removed.name,
                    leaseId = removed.capabilityLease.id,
                    reason = "permission review disabled",
                ),
            )
        }
        permissionResponder.reset(defaultResponder)
        return "automatic permission review disabled."
    }

    fun spawnAgent(name: String, path: String, model: String? = null, canCoordinate: Boolean = false): String {
        val trimmedName = name.trim()
        if (trimmedName.isBlank()) return "error: an agent name is required"
        if (path.isBlank()) return "error: workspace path is required"

        val normalizedPath = runCatching {
            path
                .replaceFirst("~${File.separator}", System.getProperty("user.home") ?: "~")
        }.getOrElse { path }
        val folder = File(normalizedPath)
        if (!folder.exists() || !folder.isDirectory) return "error: not a folder: ${folder.path}"

        return attach(trimmedName, folder.absolutePath, model, canCoordinate)
    }

    fun listAgents(): String {
        val visible = agents.all.filter { state -> !agents.isReservedPermissionReviewer(state.name) }
        if (visible.isEmpty()) return "(no agents)"
        return visible.joinToString("\n") { state ->
            "${state.name} · ${state.model} · ${state.workspace}"
        }
    }

    fun removeAgent(name: String): String {
        val trimmedName = name.trim()
        if (trimmedName.equals("main", ignoreCase = true)) return "error: cannot remove @main"
        if (agents.isReservedPermissionReviewer(trimmedName)) return "error: @permission_reviewer is controlled by /default"
        return detach(trimmedName)
    }

    fun detach(name: String): String {
        if (name.isBlank()) return "agent name is empty."
        val removed = agents.unregister(name) ?: return "no such agent: $name"
        runBlockingOrNull {
            eventSink.appendAsync(
                ConversationEventKinds.AgentDetached,
                ConversationEventPayloads.agentDetached(removed.name),
            )
            eventSink.appendAsync(
                ConversationEventKinds.WorkspaceLeaseRevoked,
                ConversationEventPayloads.workspaceLeaseRevoked(
                    agent = removed.name,
                    workspaceLeaseId = removed.workspaceLease.id,
                    reason = "agent detached",
                ),
            )
            eventSink.appendAsync(
                ConversationEventKinds.CapabilityLeaseRevoked,
                ConversationEventPayloads.capabilityLeaseRevoked(
                    agent = removed.name,
                    leaseId = removed.capabilityLease.id,
                    reason = "agent detached",
                ),
            )
        }
        return "${removed.name} detached."

    }

    fun clear() {
        messageBuffer.clear()
        agents.all.forEach { it.session.clear() }
    }

    private fun resolveAgent(name: String?): CoworkAgentState? {
        return agents.resolve(name)
    }

    private fun createSessionForAgent(
        name: String,
        workspace: String,
        workspaceLease: WorkspaceLease,
        capabilityLease: CapabilityLease,
        model: String,
        permissionProfile: PermissionProfile = profile,
    ): CoworkAgentState {
        val resolved = WorkspaceTools.resolveWorkspace(config.workspace, workspace)
        val session = CodeAgentSession(
            config,
            resolved,
            agentName = name,
            permissionProfile = permissionProfile,
            shell = shellRunner,
            git = gitService,
            messenger = InternalCoworkMessenger(this),
            responder = permissionResponder,
            permissionReviewer = permissionReviewer,
            eventSink = eventSink,
            allowsShell = allowsShell,
            maxIterations = maxIterations,
            capabilityLease = capabilityLease,
            workspaceLease = workspaceLease,
        )
        return CoworkAgentState(name, resolved, model, session, workspaceLease, capabilityLease)
    }

    private data class InternalCoworkMessenger(private val owner: CoworkEngine) : IToolAgentMessenger {
        override suspend fun askAsync(from: String, to: String, question: String): String =
            owner.askAsync(from, to, question)

        override suspend fun sendMessageAsync(from: String, to: String, content: String): String =
            owner.sendMessageAsync(from, to, content)

        override suspend fun requestInformationAsync(from: String, to: String, question: String, taskID: String?): String =
            owner.requestInformation(from, to, question, taskID)

        override suspend fun replyMessageAsync(
            from: String,
            to: String,
            answer: String,
            inReplyTo: String? ,
            taskID: String? ,
        ): String = owner.replyMessage(from, to, answer, inReplyTo, taskID)

        override suspend fun requestDelegationAsync(
            from: String,
            objective: String,
            reason: String,
        ): String = owner.requestDelegation(from, objective, reason)

        override suspend fun delegateTaskAsync(
            from: String,
            to: String,
            objective: String,
            reason: String,
            roleHint: String,
            expectedDeliverable: String,
            taskID: String? = null,
        ): String = owner.delegateTask(from, to, objective, reason, roleHint, expectedDeliverable, taskID)

        override suspend fun spawnAgentAsync(name: String, path: String, model: String?, canCoordinate: Boolean): String =
            owner.spawnAgent(name, path, model, canCoordinate)

        override suspend fun listAgentsAsync(): String = owner.listAgents()

        override suspend fun removeAgentAsync(name: String): String = owner.removeAgent(name)
    }

    data class CoworkAgentState(
        val name: String,
        val workspace: String,
        val model: String,
        val session: CodeAgentSession,
        val workspaceLease: WorkspaceLease,
        var capabilityLease: CapabilityLease,
    )
}

private fun CapabilityLease.consumeForDelegationOrNull(tool: String): CapabilityLease? = when {
    canUseDelegationBudget().not() -> null
    tool == "request_delegation" && !canRequestDelegation() -> null
    tool == "delegate_task" && !canDelegateTask() -> null
    else -> if (tool == "request_delegation" || tool == "delegate_task") consumeDelegationBudget() else this
}

sealed class CoworkForwardDecision {
    data class Forward(val content: String) : CoworkForwardDecision()
    data class Block(val reason: String) : CoworkForwardDecision()
}

interface ICoworkForwardReviewer {
    suspend fun reviewAsync(from: String, to: String, content: String): CoworkForwardDecision
}

enum class CoworkMessageKind {
    SendMessage,
    RequestInformation,
    ReplyMessage,
    RequestDelegation,
    DelegateTask,
}

class CoworkMediator(private val maxChars: Int = 4000, private val reviewer: ICoworkForwardReviewer? = null) {
    suspend fun mediate(from: String, to: String, content: String): CoworkForwardDecision {
        if (SecretScanner.containsSecret(content)) {
            return CoworkForwardDecision.Block("content appears to contain secrets")
        }
        if (content.length > maxChars) {
            return CoworkForwardDecision.Block("content too large to forward (${content.length} chars); send a summary instead")
        }
        reviewer?.let {
            return it.reviewAsync(from, to, content)
        }
        return CoworkForwardDecision.Forward(content)
    }
}

class CoworkMessageBus(
    private val mediator: CoworkMediator = CoworkMediator(),
    private val eventSink: IConversationEventSink = NullConversationEventSink(),
) {
    suspend fun deliver(from: String, to: String, content: String): String? {
        return deliverMessage(from, to, content, CoworkMessageKind.SendMessage)
    }

    suspend fun sendMessage(from: String, to: String, content: String): String? {
        return deliverMessage(from, to, content, CoworkMessageKind.SendMessage)
    }

    suspend fun requestInformation(from: String, to: String, question: String, taskID: String? = null): String? {
        return deliverMessage(from, to, question, CoworkMessageKind.RequestInformation, taskID)
    }

    suspend fun replyMessage(
        from: String,
        to: String,
        answer: String,
        inReplyTo: String? = null,
        taskID: String? = null,
    ): String? {
        return deliverMessage(
            from = from,
            to = to,
            content = answer,
            kind = CoworkMessageKind.ReplyMessage,
            inReplyTo = inReplyTo,
            taskID = taskID,
        )
    }

    suspend fun requestDelegation(from: String, objective: String, reason: String = "delegation requested"): String? {
        return deliverMessage(
            from = from,
            to = from,
            content = objective,
            kind = CoworkMessageKind.RequestDelegation,
            reason = reason,
        )
    }

    suspend fun delegateTask(from: String, to: String, objective: String, reason: String = "delegation requested"): String? {
        return deliverMessage(
            from = from,
            to = to,
            content = objective,
            kind = CoworkMessageKind.DelegateTask,
            reason = reason,
        )
    }

    private suspend fun deliverMessage(
        from: String,
        to: String,
        content: String,
        kind: CoworkMessageKind,
        taskID: String? = null,
        inReplyTo: String? = null,
        reason: String = "request"
    ): String? {
        return when (val decision = mediator.mediate(from, to, content)) {
            is CoworkForwardDecision.Block -> {
                eventSink.appendAsync(
                    ConversationEventKinds.PermissionReview,
                    ConversationEventPayloads.permissionReview(
                        toolNameFor(kind),
                        decision = PermissionDecision.Deny,
                        risk = RiskLevel.High,
                        reason = decision.reason,
                        reviewerModel = "mediator",
                        agent = from,
                    )
                )
                null
            }
            is CoworkForwardDecision.Forward -> {
                eventSink.appendAsync(
                    ConversationEventKinds.PermissionReview,
                    ConversationEventPayloads.permissionReview(
                        toolNameFor(kind),
                        decision = PermissionDecision.Allow,
                        risk = RiskLevel.Low,
                        reason = "forwarded after mediation",
                        reviewerModel = "mediator",
                        agent = from,
                    )
                )
                appendForwardEvent(from, to, decision.content, kind, reason, inReplyTo, taskID)
                decision.content
            }
        }
    }

    private suspend fun appendForwardEvent(
        from: String,
        to: String,
        content: String,
        kind: CoworkMessageKind,
        reason: String,
        taskID: String? = null,
        inReplyTo: String? = null,
    ) {
        when (kind) {
            CoworkMessageKind.SendMessage ->
                eventSink.appendAsync(
                    ConversationEventKinds.AgentMessage,
                    ConversationEventPayloads.agentMessage(
                        from = from,
                        to = to,
                        content = content,
                        kind = "send_message",
                        mediated = true,
                    )
                )
            CoworkMessageKind.RequestInformation ->
                eventSink.appendAsync(
                    ConversationEventKinds.InformationRequested,
                    ConversationEventPayloads.informationRequested(from, to, content, true, taskID)
                )
            CoworkMessageKind.ReplyMessage ->
                eventSink.appendAsync(
                    ConversationEventKinds.InformationReplied,
                    ConversationEventPayloads.informationReplied(from, to, content, true, inReplyTo, taskID)
                )
            CoworkMessageKind.RequestDelegation ->
                eventSink.appendAsync(
                    ConversationEventKinds.DelegationRequested,
                    ConversationEventPayloads.delegationRequested(from, null, content, reason)
                )
            CoworkMessageKind.DelegateTask ->
                eventSink.appendAsync(
                    ConversationEventKinds.DelegationRequested,
                    ConversationEventPayloads.delegationRequested(from, to, content, reason)
                )
        }
    }

    private fun toolNameFor(kind: CoworkMessageKind): String = when (kind) {
        CoworkMessageKind.SendMessage -> "send_message"
        CoworkMessageKind.RequestInformation -> "request_information"
        CoworkMessageKind.ReplyMessage -> "agent_reply_message"
        CoworkMessageKind.RequestDelegation -> "request_delegation"
        CoworkMessageKind.DelegateTask -> "agent_delegation"
    }
}

private fun <T> runBlockingOrNull(block: suspend () -> T): T? {
    return try {
        kotlinx.coroutines.runBlocking(block)
    } catch (_: Throwable) {
        null
    }
}
