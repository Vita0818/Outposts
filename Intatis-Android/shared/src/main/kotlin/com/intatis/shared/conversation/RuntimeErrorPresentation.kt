package com.intatis.shared.conversation

import com.intatis.shared.ConversationEventKinds
import com.intatis.shared.ConversationEventPayloads
import com.intatis.shared.IConversationEventSink

object RuntimeErrorPresentation {
    fun format(error: Throwable): Pair<String, String> {
        val message = (error.message ?: "unknown error").trim()
        return when {
            message.contains("timeout", ignoreCase = true) -> "timeout_error" to "The request timed out. Try again with a shorter prompt."
            message.contains("permission", ignoreCase = true) -> "permission_error" to "The request was blocked by permission policy."
            message.contains("network", ignoreCase = true) -> "network_error" to "Network request failed. Check your connection and API settings."
            else -> "chat_error" to (message.ifBlank { "Unexpected error." })
        }
    }

    suspend fun emit(error: Throwable, eventSink: IConversationEventSink) {
        val (code, message) = format(error)
        eventSink.appendAsync(
            ConversationEventKinds.Error,
            ConversationEventPayloads.error(
                message = message,
                code = code,
                fatal = true,
            ),
        )
    }
}
