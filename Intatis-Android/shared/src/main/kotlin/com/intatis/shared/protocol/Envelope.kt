package com.intatis.shared.protocol

import com.intatis.shared.protocol.Jsonx.int
import com.intatis.shared.protocol.Jsonx.str
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.time.Instant
import java.time.format.DateTimeFormatter

/**
 * Wire shape (one JSON object per line, mirrors the Apple EventLog contract):
 * {"seq":1421,"ts":"2026-06-11T09:14:22Z","session":"sess_8f2a","v":1,"type":"message_delta","payload":{...}}
 * The JSONL event log is the canonical session truth; every projection is rebuildable.
 */
data class Envelope(
    val seq: Long,
    val ts: Instant,
    val session: String,
    val v: Int = 1,
    val type: String,
    val payload: JsonObject?,
) {
    fun toJsonLine(): String = Jsonx.serializeSorted(buildJsonObject {
        put("seq", seq)
        put("ts", DateTimeFormatter.ISO_INSTANT.format(ts))
        put("session", session)
        put("v", v)
        put("type", type)
        put("payload", payload ?: JsonNull)
    })

    companion object {
        fun fromJsonLine(line: String): Envelope {
            val obj = Jsonx.parseObject(line)
            val seq = obj.int("seq")?.toLong() ?: -1L
            val ts = try {
                Instant.parse(obj.str("ts") ?: "")
            } catch (_: Exception) {
                Instant.now()
            }
            return Envelope(
                seq = seq,
                ts = ts,
                session = obj.str("session") ?: "",
                v = obj.int("v") ?: 1,
                type = obj.str("type") ?: "",
                payload = obj["payload"] as? JsonObject,
            )
        }
    }
}

/**
 * Event tags use snake_case wire names and evolve additively only: readers must
 * skip unknown future types while reserving their sequence space.
 */
object EventType {
    const val SESSION_SETTINGS_UPDATED = "session_settings_updated"
    const val USER_MESSAGE = "user_message"
    const val MESSAGE_DELTA = "message_delta"
    const val MESSAGE_COMPLETED = "message_completed"
    const val ERROR = "error"
    const val AGENT_STATUS = "agent_status"
    const val TURN_STATS = "turn_stats"
    const val TURN_OUTCOME = "turn_outcome"
    const val ARTIFACT_ADDED = "artifact_added"
}

enum class MessageRole(val wire: String) {
    USER("user"),
    ASSISTANT("assistant"),
    AGENT("agent"),
    SYSTEM("system");

    companion object {
        fun fromWire(value: String): MessageRole = when (value) {
            "assistant" -> ASSISTANT
            "agent" -> AGENT
            "system" -> SYSTEM
            else -> USER
        }
    }
}
