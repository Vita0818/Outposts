package com.intatis.shared

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
    private val agents = linkedMapOf<String, CoworkAgentState>(String.CASE_INSENSITIVE_ORDER)
    private val messageBuffer = mutableListOf<Pair<String, String>>()
    private val shellRunner: IToolShellRunner = shell ?: ProcessShellRunner()
    private val gitService: IToolGitService = git ?: ProcessGitService(shellRunner)

    val agentsNames: List<String>
        get() = agents.keys.toList()

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
    ): String {
        if (agents.isEmpty()) return "(no agents attached)"
        val selected = resolveAgent(targetAgent)
            ?: return "no such agent: $targetAgent"

        val selectedModel = model ?: selected.model
        val response = selected.session.sendAsync(text, selectedModel, reasoning, "cowork")

        messageBuffer.add(selected.name to text)
        messageBuffer.add(selected.name to response.first)
        return response.first
    }

    suspend fun askAsync(from: String, to: String, question: String): String {
        val sender = if (from.isBlank()) "unknown" else from
        val target = resolveAgent(to) ?: return "no such agent: $to"
        if (sender.equals(target.name, ignoreCase = true)) return "self-targeted ask is blocked."

        val delivered = messageBus.deliver(sender, target.name, "[$sender] $question") ?: return "the reply was blocked by the mediator"
        val (_, model, workspace) = target
        val answer = target.session.sendAsync(delivered, model)
        messageBuffer.add(target.name to answer.first)
        return messageBus.deliver(target.name, sender, answer.first) ?: "the reply was blocked by the mediator"
    }

    fun attach(name: String, workspace: String? = null): String {
        if (name.isBlank()) return "agent name is empty."
        if (agents.containsKey(name)) return "$name already exists."

        val resolvedWorkspace = workspace?.ifBlank { baseWorkspace } ?: baseWorkspace
        val state = createSessionForAgent(name, resolvedWorkspace, config.model)
        agents[name] = state
        return "$name attached to $resolvedWorkspace."
    }

    fun clear() {
        messageBuffer.clear()
        agents.values.forEach { it.session.clear() }
    }

    private fun resolveAgent(name: String?): CoworkAgentState? {
        if (name != null) return agents[name]
        return agents.values.firstOrNull()
    }

    private fun createSessionForAgent(name: String, workspace: String, model: String): CoworkAgentState {
        val resolved = WorkspaceTools.resolveWorkspace(config.workspace, workspace)
        val session = CodeAgentSession(
            config,
            resolved,
            agentName = name,
            permissionProfile = profile,
            shell = shellRunner,
            git = gitService,
            messenger = InternalCoworkMessenger(::askAsync),
            responder = responder,
            permissionReviewer = permissionReviewer,
            eventSink = eventSink,
            allowsShell = allowsShell,
            maxIterations = maxIterations,
        )
        return CoworkAgentState(name, resolved, model, session)
    }

    private data class InternalCoworkMessenger(private val deliver: suspend (String, String, String) -> String) : IToolAgentMessenger {
        override suspend fun askAsync(from: String, to: String, question: String): String =
            deliver(from, to, question)
    }

    data class CoworkAgentState(val name: String, val workspace: String, val model: String, val session: CodeAgentSession)
}

sealed class CoworkForwardDecision {
    data class Forward(val content: String) : CoworkForwardDecision()
    data class Block(val reason: String) : CoworkForwardDecision()
}

interface ICoworkForwardReviewer {
    suspend fun reviewAsync(from: String, to: String, content: String): CoworkForwardDecision
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
        return when (val decision = mediator.mediate(from, to, content)) {
            is CoworkForwardDecision.Block -> {
                eventSink.appendAsync(
                    ConversationEventKinds.PermissionReview,
                    ConversationEventPayloads.permissionReview(
                        "agent_forward",
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
                        "agent_forward",
                        decision = PermissionDecision.Allow,
                        risk = RiskLevel.Low,
                        reason = "forwarded after mediation",
                        reviewerModel = "mediator",
                        agent = from,
                    )
                )
                eventSink.appendAsync(
                    ConversationEventKinds.AgentToAgentMessage,
                    ConversationEventPayloads.agentToAgentMessage(from, to, decision.content, mediated = true)
                )
                decision.content
            }
        }
    }
}

private fun <T> runBlockingOrNull(block: suspend () -> T): T? {
    return try {
        kotlinx.coroutines.runBlocking(block)
    } catch (_: Throwable) {
        null
    }
}
