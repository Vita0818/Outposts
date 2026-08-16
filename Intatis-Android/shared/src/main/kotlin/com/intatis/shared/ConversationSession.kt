package com.intatis.shared

import com.intatis.shared.agentkernel.Agent
import com.intatis.shared.conversation.RuntimeErrorPresentation
import java.io.File
import java.time.Duration

class ConversationSession(
    config: IntatisConfig,
    private val eventSink: IConversationEventSink = NullConversationEventSink()
) {
    private val agent = Agent(
        config = config,
        workspaceRoot = resolveWorkspaceRoot(config),
        responder = AllowAllResponder(),
        eventSink = eventSink,
        systemPrompt = "You are Intatis (Android). Provide short, practical responses.",
    )
    private val messages = mutableListOf<IntatisMessage>()

    init {
        messages.add(
            IntatisMessage(
                role = MessageRole.System,
                content = "You are Intatis (Android). Provide short, practical responses."
            )
        )
    }

    val history: List<IntatisMessage>
        get() = messages.toList()

    suspend fun sendUserMessageAsync(
        userText: String,
        model: String? = null,
        reasoning: String? = null,
        attachments: List<ImageAttachment>? = null,
        includeUsage: Boolean = false,
        to: String? = null,
        goal: String? = null,
        tags: List<String>? = null,
    ): Triple<IntatisMessage, Duration, String?> {
        val user = IntatisMessage(role = MessageRole.User, content = userText)
        messages.add(user)

        return try {
            val (text, elapsed, usage) = agent.sendAsync(
                userText = userText,
                model = model,
                reasoning = reasoning,
                userGoal = goal,
                attachments = attachments,
                includeUsage = includeUsage,
                to = to,
                tags = tags,
            )
            val assistant = IntatisMessage(role = MessageRole.Assistant, content = text)
            messages.add(assistant)
            Triple(assistant, elapsed, usage)
        } catch (ex: Exception) {
            RuntimeErrorPresentation.emit(ex, eventSink)
            throw
        }
    }

    fun clear() {
        val system = messages.firstOrNull { it.role == MessageRole.System }
        messages.clear()
        if (system != null) {
            messages.add(system)
        }
    }

    private fun resolveWorkspaceRoot(config: IntatisConfig): String {
        return runCatching { WorkspaceTools.resolveWorkspace(config.workspace, config.workspace) }
            .getOrElse { File(".").absolutePath }
    }
}
