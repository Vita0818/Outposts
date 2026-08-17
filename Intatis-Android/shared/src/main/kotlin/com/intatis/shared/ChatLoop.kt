package com.intatis.shared

import com.intatis.shared.protocol.ErrorPayload
import com.intatis.shared.protocol.EventType
import com.intatis.shared.protocol.MessageCompletedPayload
import com.intatis.shared.protocol.MessageDeltaPayload
import com.intatis.shared.protocol.MessageRole
import com.intatis.shared.protocol.TurnOutcomePayload
import com.intatis.shared.protocol.TurnOutcomeWire
import com.intatis.shared.protocol.TurnStatsPayload
import com.intatis.shared.protocol.UserMessagePayload
import com.intatis.shared.providers.ChatChunk
import com.intatis.shared.providers.ChatMessage
import com.intatis.shared.providers.ChatRequest
import com.intatis.shared.providers.ChatProvider
import com.intatis.shared.providers.ImageAttachment
import com.intatis.shared.providers.ReasoningEffort
import com.intatis.shared.providers.Usage
import com.intatis.shared.session.ConversationProjection
import com.intatis.shared.session.EventLog
import kotlinx.coroutines.currentCoroutineContext
import kotlinx.coroutines.ensureActive

/**
 * Tool-free chat turn engine (the mobile surface is a strict Chat subset per the
 * Apple contract): durable user message, streamed assistant deltas into the
 * EventLog, terminal message_completed + turn_stats + turn_outcome.
 */
class ChatLoop(
    private val log: EventLog,
    private val provider: ChatProvider,
    private val model: String,
    private val systemPrompt: String? = null,
    private val reasoningEffort: ReasoningEffort? = null,
    private val includeUsage: Boolean = false,
) {

    /** Returns the terminal event sequence for this turn. */
    suspend fun send(
        userText: String,
        images: List<ImageAttachment>? = null,
    ): Long {
        val turnId = TurnId.new()
        val submissionId = SubmissionId.new()
        val messageId = MessageId.new()

        // History from the canonical projection BEFORE appending this turn's user
        // message; the current user text is added explicitly below.
        val priorMessages = buildHistory()
        val messages = priorMessages.toMutableList()
        messages.add(ChatMessage("user", userText, images ?: emptyList()))

        log.append(EventType.USER_MESSAGE, UserMessagePayload(
            text = userText,
            submissionId = submissionId.value,
            turnId = turnId.value,
            attachments = if (!images.isNullOrEmpty()) {
                images.map { ArtifactId.new().value }
            } else null,
        ).toJson())

        return try {
            var usage = Usage.EMPTY
            val text = StringBuilder()

            provider.streamChat(ChatRequest(
                model = model,
                messages = messages,
                reasoningEffort = reasoningEffort,
                includeUsage = includeUsage,
            )).collect { chunk ->
                currentCoroutineContext().ensureActive()
                when (chunk) {
                    is ChatChunk.Delta -> {
                        text.append(chunk.text)
                        log.append(EventType.MESSAGE_DELTA, MessageDeltaPayload(
                            messageId = messageId.value,
                            role = MessageRole.ASSISTANT.wire,
                            textDelta = chunk.text,
                        ).toJson(), flush = false)
                    }

                    is ChatChunk.UsageReport -> usage = usage.mergedWith(chunk.usage)

                    ChatChunk.Done -> { /* terminal markers appended below */ }
                }
            }

            log.append(EventType.MESSAGE_COMPLETED, MessageCompletedPayload(
                messageId = messageId.value,
                role = MessageRole.ASSISTANT.wire,
                text = text.toString(),
            ).toJson())
            log.append(EventType.TURN_STATS, TurnStatsPayload(
                promptTokens = usage.promptTokens,
                cachedPromptTokens = usage.cachedPromptTokens,
                completionTokens = usage.completionTokens,
                totalTokens = usage.totalTokens,
                model = model,
            ).toJson(), flush = false)
            log.append(EventType.TURN_OUTCOME, TurnOutcomePayload(
                turnId = turnId.value,
                outcome = TurnOutcomeWire.COMPLETED.wire,
            ).toJson()).seq
        } catch (e: kotlinx.coroutines.CancellationException) {
            // Never finalize a user-stopped partial into message_completed.
            log.append(EventType.TURN_OUTCOME, TurnOutcomePayload(
                turnId = turnId.value,
                outcome = TurnOutcomeWire.INTERRUPTED.wire,
                failureSource = "turn_cancelled",
                reason = "cancelled by user",
            ).toJson())
            throw e
        } catch (e: Exception) {
            log.append(EventType.ERROR, ErrorPayload(
                code = "provider",
                message = e.message ?: "provider failure",
            ).toJson())
            log.append(EventType.TURN_OUTCOME, TurnOutcomePayload(
                turnId = turnId.value,
                outcome = TurnOutcomeWire.FAILED.wire,
                failureSource = "runtime_failed",
                reason = e.message ?: "provider failure",
            ).toJson()).seq
        }
    }

    private fun buildHistory(): List<ChatMessage> {
        val projection = ConversationProjection.build(log.replay())
        val messages = mutableListOf<ChatMessage>()
        systemPrompt?.takeIf { it.isNotEmpty() }?.let {
            messages.add(ChatMessage("system", it))
        }
        for (view in projection) {
            when {
                view.role == MessageRole.USER ->
                    messages.add(ChatMessage("user", view.text))

                (view.role == MessageRole.ASSISTANT || view.role == MessageRole.AGENT) && view.isComplete ->
                    messages.add(ChatMessage("assistant", view.text))
            }
        }
        return messages
    }
}
