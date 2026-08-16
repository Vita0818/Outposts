package com.intatis.shared.conversation

import com.intatis.shared.protocol.EventLogRecord
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.jsonPrimitive

data class ProjectionLine(
    val sender: String,
    val text: String,
    val isError: Boolean,
)

private data class MessageState(val sender: String, val text: StringBuilder)

private sealed interface ProjectionItem
private data class MessageItem(val id: String) : ProjectionItem
private data class ErrorItem(val line: ProjectionLine) : ProjectionItem

open class ConversationProjection {
    open fun render(records: List<EventLogRecord>): List<ProjectionLine> {
        val buffer = LinkedHashMap<String, MessageState>()
        val items = mutableListOf<ProjectionItem>()

        for (record in records.sortedBy { it.seq }) {
            val payload = record.payload as? JsonObject ?: continue
            when (record.type) {
                "user_message" -> {
                    val messageId = payload.string("message_id") ?: continue
                    val role = payload.string("role")
                    val text = payload.string("text") ?: continue
                    val sender = resolveSender(role, payload.string("agent"), payload.string("to"), payload.string("goal"))
                    if (!buffer.containsKey(messageId)) {
                        items.add(MessageItem(messageId))
                    }
                    buffer[messageId] = MessageState(sender, StringBuilder(text))
                }

                "message_delta" -> {
                    val messageId = payload.string("message_id") ?: continue
                    val delta = payload.string("text_delta") ?: continue
                    val role = payload.string("role")
                    val sender = resolveSender(role, payload.string("agent"), payload.string("to"), payload.string("goal"))
                    val state = buffer.getOrPut(messageId) {
                        items.add(MessageItem(messageId))
                        MessageState(sender, StringBuilder())
                    }
                    state.text.append(delta)
                }

                "message_completed" -> {
                    val messageId = payload.string("message_id") ?: continue
                    val text = payload.string("text") ?: continue
                    val role = payload.string("role")
                    val sender = resolveSender(role, payload.string("agent"), payload.string("to"), payload.string("goal"))
                    buffer[messageId] = MessageState(sender, StringBuilder(text))
                    if (!items.any { it is MessageItem && it.id == messageId }) {
                        items.add(MessageItem(messageId))
                    }
                }

                "error" -> {
                    val message = payload.string("message") ?: "unknown error"
                    val code = payload.string("code") ?: "chat_error"
                    val isError = payload.boolean("fatal") ?: payload.string("code") == "provider" || code == "io"
                    items.add(ErrorItem(ProjectionLine("system", "⚠️ $code: $message", isError)))
                }

                "tool_call" -> {
                    val tool = payload.string("tool") ?: "tool"
                    items.add(ErrorItem(ProjectionLine("tool", "tool call: $tool", false)))
                }

                "tool_result" -> {
                    val toolCallId = payload.string("tool_call_id") ?: "tool_call"
                    val observation = payload.string("observation") ?: ""
                    items.add(ErrorItem(ProjectionLine("tool", "result · $toolCallId: $observation", false)))
                }

                "permission_request" -> {
                    val tool = payload.string("tool") ?: "unknown tool"
                    items.add(ErrorItem(ProjectionLine("permission", "request permission for $tool", false)))
                }

                "permission_resolved" -> {
                    val tool = payload.string("tool") ?: "unknown tool"
                    val decision = payload.string("decision") ?: "unknown"
                    val reason = payload.string("reason") ?: ""
                    val suffix = reason.ifBlank { decision }
                    items.add(ErrorItem(ProjectionLine("permission", "$tool $decision $suffix", false)))
                }

                "permission_review" -> {
                    val reviewer = payload.string("reviewer_model") ?: "reviewer"
                    val tool = payload.string("tool") ?: "unknown tool"
                    val decision = payload.string("decision") ?: "unknown"
                    items.add(ErrorItem(ProjectionLine("permission", "$reviewer reviewed $tool: $decision", false)))
                }

                "patch_proposed" -> {
                    val patchId = payload.string("patch_id") ?: "patch"
                    val files = payload.stringList("files")
                    val diff = payload.string("diff") ?: ""
                    val fileList = files?.joinToString(prefix = " [", postfix = "]") { it } ?: ""
                    items.add(ErrorItem(ProjectionLine("patch", "patch proposed $patchId$fileList: $diff", false)))
                }

                "agent_to_agent_message" -> {
                    val from = payload.string("from") ?: "agent"
                    val to = payload.string("to") ?: "agent"
                    val content = payload.string("content") ?: ""
                    items.add(ErrorItem(ProjectionLine("agent", "$from -> $to: $content", false)))
                }

                "agent_attached" -> items.add(ErrorItem(ProjectionLine("agent", "agent attached: @${payload.string("agent")}", false)))
                "agent_spawned" -> items.add(ErrorItem(ProjectionLine("agent", "agent spawned: @${payload.string("agent")}", false)))
                "agent_detached" -> items.add(ErrorItem(ProjectionLine("agent", "agent detached: @${payload.string("agent")}", false)))
                "agent_attach_requested" -> items.add(ErrorItem(ProjectionLine("agent", "agent attach requested: @${payload.string("agent")}", false)))
                "agent_spawn_requested" -> items.add(ErrorItem(ProjectionLine("agent", "agent spawn requested: @${payload.string("agent")}", false)))
                "information_requested" -> {
                    val from = payload.string("from") ?: "agent"
                    val to = payload.string("to") ?: "agent"
                    val question = payload.string("question") ?: ""
                    items.add(ErrorItem(ProjectionLine("agent", "info request $from -> $to: $question", false)))
                }

                "information_replied" -> {
                    val from = payload.string("from") ?: "agent"
                    val to = payload.string("to") ?: "agent"
                    val answer = payload.string("content") ?: ""
                    items.add(ErrorItem(ProjectionLine("agent", "info reply $from -> $to: $answer", false)))
                }

                "delegation_requested" -> {
                    val requester = payload.string("requester") ?: "agent"
                    val objective = payload.string("objective") ?: ""
                    items.add(ErrorItem(ProjectionLine("agent", "delegation requested by $requester: $objective", false)))
                }

                "delegation_approved" -> {
                    val requester = payload.string("requester") ?: "agent"
                    items.add(ErrorItem(ProjectionLine("agent", "delegation approved: $requester", false)))
                }

                "delegation_rejected" -> {
                    val reason = payload.string("reason") ?: "reason unknown"
                    items.add(ErrorItem(ProjectionLine("agent", "delegation rejected: $reason", true)))
                }

                "task_created" -> items.add(ErrorItem(ProjectionLine("task", "task created: ${payload.string("contract") ?: "task"}", false)))
                "task_assigned" -> items.add(ErrorItem(ProjectionLine("task", "task assigned: ${payload.string("contract") ?: "task"}", false)))
                "task_queued" -> items.add(ErrorItem(ProjectionLine("task", "task queued: ${payload.string("contract") ?: "task"}", false)))
                "task_started" -> items.add(ErrorItem(ProjectionLine("task", "task started: ${payload.string("task_id") ?: "task"}", false)))
                "task_completed" -> items.add(ErrorItem(ProjectionLine("task", "task completed: ${payload.string("result") ?: "result"}", false)))
                "task_failed" -> items.add(ErrorItem(ProjectionLine("task", "task failed: ${payload.string("error") ?: "error"}", true)))
                "task_rejected" -> items.add(ErrorItem(ProjectionLine("task", "task rejected: ${payload.string("reason") ?: "reason unknown"}", true)))

                "workspace_lease_requested" -> items.add(ErrorItem(ProjectionLine("lease", "workspace lease requested: ${payload.string("root_path")}", false)))
                "workspace_lease_granted" -> items.add(ErrorItem(ProjectionLine("lease", "workspace lease granted: ${payload.string("root_path")}", false)))
                "workspace_lease_denied" -> items.add(ErrorItem(ProjectionLine("lease", "workspace lease denied: ${payload.string("reason")}", true)))
                "workspace_lease_revoked" -> items.add(ErrorItem(ProjectionLine("lease", "workspace lease revoked: ${payload.string("reason")}", false)))
                "capability_lease_created" -> items.add(ErrorItem(ProjectionLine("lease", "capability lease created", false)))
                "capability_lease_revoked" -> items.add(ErrorItem(ProjectionLine("lease", "capability lease revoked: ${payload.string("reason")}", false)))

                "artifact_added" -> items.add(ErrorItem(ProjectionLine("artifact", "artifact: ${payload.string("kind") ?: "unknown"}", false)))
            }
        }

        return items.mapNotNull { item ->
            when (item) {
                is MessageItem -> {
                    val state = buffer[item.id] ?: return@mapNotNull null
                    ProjectionLine(state.sender, state.text.toString().trim(), false)
                }

                is ErrorItem -> item.line
            }
        }
    }

    protected open fun resolveSender(role: String?, agent: String?, to: String?, goal: String?): String {
        return when (role?.lowercase()) {
            "user" -> "you"
            "assistant" -> "assistant"
            "system" -> "system"
            else -> agent ?: role ?: "system"
        }

            .let { sender ->
                when {
                    !to.isNullOrBlank() -> "$sender -> @${to}"
                    !goal.isNullOrBlank() -> "$sender [$goal]"
                    else -> sender
                }
            }
    }
}

private fun JsonElement.string(key: String): String? {
    return when (this) {
        is JsonObject -> this[key]?.jsonPrimitive?.contentOrNull
        else -> null
    }
}

private fun JsonElement.boolean(key: String): Boolean? {
    return when (this) {
        is JsonObject -> when (val value = this[key]) {
            is JsonPrimitive -> value.contentOrNull?.toBooleanStrictOrNull() ?: value.contentOrNull?.toIntOrNull()?.let { it != 0 }
            else -> null
        }
        else -> null
    }
}

private fun JsonElement.stringList(key: String): List<String>? {
    return when (this) {
        is JsonObject -> when (val value = this[key]) {
            is JsonArray -> value.mapNotNull { it.jsonPrimitive.contentOrNull }
            else -> null
        }

        else -> null
    }
}
