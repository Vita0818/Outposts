package com.intatis.shared.session

import com.intatis.shared.protocol.Envelope
import java.time.Instant
import com.intatis.shared.protocol.EventType
import com.intatis.shared.protocol.Jsonx.str
import com.intatis.shared.protocol.MessageCitation
import com.intatis.shared.protocol.MessageRole
import com.intatis.shared.protocol.UserMessagePayload

data class ChatMessageView(
    val messageId: String,
    val role: MessageRole,
    val agent: String? = null,
    val text: String = "",
    val isComplete: Boolean = false,
    val timestamp: Instant = java.time.Instant.now(),
    val attachmentCount: Int = 0,
    val citations: List<MessageCitation> = emptyList(),
)

/**
 * Folds envelopes into the chat text view. The UI only ever consumes this folded
 * projection, never raw model output as truth. Unknown event types are ignored.
 */
object ConversationProjection {

    fun build(envelopes: List<Envelope>): List<ChatMessageView> {
        val messages = mutableListOf<ChatMessageView>()
        var open: ChatMessageView? = null

        fun flush() {
            open?.let { messages.add(it) }
            open = null
        }

        for (envelope in envelopes) {
            when (envelope.type) {
                EventType.USER_MESSAGE -> {
                    flush()
                    val user = UserMessagePayload.fromJson(envelope.payload)
                    messages.add(ChatMessageView(
                        messageId = "user_${envelope.seq}",
                        role = MessageRole.USER,
                        text = user.text,
                        isComplete = true,
                        timestamp = envelope.ts,
                        attachmentCount = user.attachments?.size ?: 0,
                    ))
                }

                EventType.MESSAGE_DELTA -> {
                    val payload = envelope.payload
                    val messageId = payload?.str("message_id") ?: ""
                    if (open == null || open!!.messageId != messageId) {
                        flush()
                        open = ChatMessageView(
                            messageId = messageId,
                            role = MessageRole.fromWire(payload?.str("role") ?: "assistant"),
                            agent = payload?.str("agent"),
                            timestamp = envelope.ts,
                        )
                    }
                    open = open!!.copy(text = open!!.text + (payload?.str("text_delta") ?: ""))
                }

                EventType.MESSAGE_COMPLETED -> {
                    val payload = envelope.payload
                    val messageId = payload?.str("message_id") ?: ""
                    val completed = ChatMessageView(
                        messageId = messageId,
                        role = MessageRole.fromWire(payload?.str("role") ?: "assistant"),
                        agent = payload?.str("agent"),
                        text = payload?.str("text") ?: "",
                        isComplete = true,
                        timestamp = envelope.ts,
                        citations = parseCitations(payload?.get("citations") as? kotlinx.serialization.json.JsonArray),
                    )
                    if (open != null && open!!.messageId == messageId) {
                        messages.add(open!!.copy(
                            text = completed.text,
                            isComplete = true,
                            citations = completed.citations,
                        ))
                    } else {
                        messages.add(completed)
                    }
                    open = null
                }

                EventType.ERROR -> {
                    flush()
                    val code = envelope.payload?.str("code") ?: "error"
                    val message = envelope.payload?.str("message") ?: "unknown error"
                    messages.add(ChatMessageView(
                        messageId = "error_${envelope.seq}",
                        role = MessageRole.SYSTEM,
                        text = "[$code] $message",
                        isComplete = true,
                        timestamp = envelope.ts,
                    ))
                }
            }
        }
        flush()
        return messages
    }

    private fun parseCitations(array: kotlinx.serialization.json.JsonArray?): List<MessageCitation> {
        if (array == null) return emptyList()
        return array.mapNotNull { item ->
            val obj = item as? kotlinx.serialization.json.JsonObject ?: return@mapNotNull null
            MessageCitation(url = obj.str("url") ?: "", title = obj.str("title") ?: "")
        }
    }
}
