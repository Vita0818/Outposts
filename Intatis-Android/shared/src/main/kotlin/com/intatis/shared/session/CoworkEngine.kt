package com.intatis.shared.session

import com.intatis.shared.log.ConversationEventKinds
import com.intatis.shared.log.ConversationEventPayloads
import com.intatis.shared.log.ConversationEventSink
import com.intatis.shared.log.NullConversationEventSink
import com.intatis.shared.model.IntatisConfig
import com.intatis.shared.security.PermissionProfile
import com.intatis.shared.security.PermissionResponder
import com.intatis.shared.security.SecretScanner
import com.intatis.shared.tools.ToolAgentMessenger
import com.intatis.shared.util.WorkspaceTools
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class CoworkEngine(
    private val config: IntatisConfig,
    private val baseWorkspace: String,
    private val responder: PermissionResponder,
    private val permissionReviewer: com.intatis.shared.security.PermissionReviewer? = null,
    private val profile: PermissionProfile = PermissionProfile.REVIEWED,
    private val eventSink: ConversationEventSink = NullConversationEventSink(),
    private val allowsShell: Boolean = true,
    private val maxIterations: Int = 8,
) : ToolAgentMessenger {

    private val agents = LinkedHashMap<String, CoworkAgentState>()
    private val transcript = mutableListOf<Pair<String, String>>()
    private val messageBus = CoworkMessageBus(eventSink)

    init {
        attach("agent-1", baseWorkspace)
    }

    data class CoworkAgentState(
        val name: String,
        var workspace: String,
        var model: String,
        val session: CodeAgentSession,
    )

    val agentNames: List<String>
        get() = agents.keys.toList()

    val transcriptLog: List<Pair<String, String>>
        get() = transcript.toList()

    suspend fun sendAsync(
        text: String,
        target: String?,
        model: String?,
        reasoning: String?,
    ): String = withContext(Dispatchers.IO) {
        if (agents.isEmpty()) return@withContext "(no agents attached)"
        val agent = resolveAgent(target) ?: return@withContext "no such agent: ${target ?: "(default)"}"

        val resolvedModel = model ?: agent.model
        val (reply, _, _) = agent.session.sendAsync(text, resolvedModel, reasoning, "cowork")
        transcript.add(agent.name to text)
        transcript.add(agent.name to reply)
        reply
    }

    fun attach(name: String, workspace: String?): String {
        val key = name.lowercase().trim()
        require(key.isNotBlank()) { "agent name is empty." }
        if (agents.containsKey(key)) return "$name already exists."

        val resolved = WorkspaceTools.resolveWorkspace(baseWorkspace, workspace)
        val state = createAgentState(key, resolved)
        agents[key] = state
        return "$name attached to $resolved."
    }

    fun clear() {
        transcript.clear()
        agents.values.forEach { it.session.clear() }
    }

    override suspend fun askAsync(from: String, to: String, question: String): String = withContext(Dispatchers.IO) {
        val recipient = resolveAgent(to) ?: return@withContext "no such agent: $to"
        val normalizedFrom = if (from.isBlank()) "unknown" else from
        if (recipient.name.equals(normalizedFrom, ignoreCase = true)) return@withContext "self-targeted ask is blocked."

        val content = "[$normalizedFrom] $question"
        val forwarded = messageBus.deliver(normalizedFrom, recipient.name, content)
            ?: return@withContext "the message was blocked by the mediator"

        val (reply, _, _) = recipient.session.sendAsync(forwarded, recipient.model, null, "ask_agent")
        transcript.add(recipient.name to reply)

        val back = messageBus.deliver(recipient.name, normalizedFrom, reply)
            ?: return@withContext "the reply was blocked by the mediator"

        back
    }

    private fun resolveAgent(name: String?): CoworkAgentState? {
        if (!name.isNullOrBlank()) return agents[name.lowercase()]
        return agents.values.firstOrNull()
    }

    private fun createAgentState(name: String, workspace: String): CoworkAgentState {
        val session = CodeAgentSession(
            config = config,
            workspaceRoot = workspace,
            agentName = name,
            permissionProfile = profile,
            responder = responder,
            permissionReviewer = permissionReviewer,
            eventSink = eventSink,
            allowsShell = allowsShell,
            maxIterations = maxIterations,
        )
        return CoworkAgentState(name, workspace, config.model, session)
    }
}

class CoworkMessageBus(private val eventSink: ConversationEventSink = NullConversationEventSink()) {
    private val mediator = CoworkMediator()

    suspend fun deliver(from: String, to: String, content: String): String? {
        val decision = mediator.mediate(from, to, content)
        return if (!decision.allow) {
            eventSink.appendAsync(
                ConversationEventKinds.PermissionReview,
                mapOf(
                    "tool" to "agent_forward",
                    "decision" to "deny",
                    "agent" to from,
                    "risk" to "high",
                    "reason" to decision.reason,
                    "reviewer_model" to "mediator",
                )
            )
            null
        } else {
            eventSink.appendAsync(
                ConversationEventKinds.AgentToAgentMessage,
                ConversationEventPayloads.agentToAgentMessage(from, to, decision.content, true)
            )
            decision.content
        }
    }
}

data class CoworkMediatorDecision(val allow: Boolean, val content: String = "", val reason: String = "")

class CoworkMediator(private val maxChars: Int = 4000) {
    fun mediate(from: String, to: String, content: String): CoworkMediatorDecision {
        if (SecretScanner.containsSecret(content)) {
            return CoworkMediatorDecision(false, reason = "content appears to contain secrets")
        }
        if (content.length > maxChars) {
            return CoworkMediatorDecision(false, reason = "content too large to forward (${content.length} chars); send a summary instead")
        }
        return CoworkMediatorDecision(true, content)
    }
}
