package com.intatis.shared.protocol

import com.intatis.shared.ConfigStore
import com.intatis.shared.IConversationEventSink
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.int
import kotlinx.serialization.json.jsonNull
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.long
import kotlinx.serialization.json.put
import java.io.File
import java.time.Instant
import java.util.concurrent.atomic.AtomicInteger

fun Any?.toJsonElement(): JsonElement = when (this) {
    null -> JsonNull
    is JsonElement -> this
    is String -> JsonPrimitive(this)
    is Int -> JsonPrimitive(this)
    is Long -> JsonPrimitive(this)
    is Double -> JsonPrimitive(this)
    is Boolean -> JsonPrimitive(this)
    is Iterable<*> -> buildJsonArray {
        for (element in this@toJsonElement) {
            add(element.toJsonElement())
        }
    }
    is Map<*, *> -> buildJsonObject {
        for ((k, v) in this@toJsonElement) {
            put(k.toString(), v.toJsonElement())
        }
    }
    is Array<*> -> buildJsonArray {
        this@toJsonElement.forEach { add(it.toJsonElement()) }
    }
    else -> JsonPrimitive(this.toString())
}

data class EventLogRecord(
    val seq: Int,
    val session: String,
    val ts: String,
    val type: String,
    val payload: JsonElement,
)

class EventLog(category: String, session: String? = null) : IConversationEventSink {
    private val eventSeq = AtomicInteger(0)
    private val mutex = Mutex()
    private val json = Json { encodeDefaults = true }
    private val sessionId = session ?: "${System.currentTimeMillis()}-${java.util.UUID.randomUUID()}"
    private val path = run {
        val root = File(ConfigStore.configFolder, "logs/$category")
        root.mkdirs()
        File(root, "$sessionId.jsonl").absolutePath
    }

    val pathValue: String = path
    val sessionIdValue: String = sessionId

    override suspend fun appendAsync(eventType: String, payload: Map<String, Any?>) {
        val wrapper = buildJsonObject {
            put("type", eventType)
            put("session", sessionId)
            put("seq", eventSeq.getAndIncrement())
            put("ts", Instant.now().toString())
            put("payload", payload.toJsonElement())
        }
        mutex.withLock {
            File(path).appendText(json.encodeToString(wrapper) + System.lineSeparator())
        }
    }

    suspend fun readAll(): List<EventLogRecord> {
        val result = mutableListOf<EventLogRecord>()
        val file = File(path)
        if (!file.exists()) {
            return result
        }

        for (line in file.readLines()) {
            if (line.isBlank()) {
                continue
            }

            val raw = runCatching { json.parseToJsonElement(line).jsonObject }.getOrNull() ?: continue
            val type = raw["type"]?.jsonPrimitive?.contentOrNull ?: continue
            val sessionFromFile = raw["session"]?.jsonPrimitive?.contentOrNull ?: continue
            val seq = raw["seq"]?.jsonPrimitive?.intOrNull ?: run {
                raw["seq"]?.jsonPrimitive?.longOrNull?.toIntOrNull() ?: continue
            }
            val ts = raw["ts"]?.jsonPrimitive?.contentOrNull ?: continue

            result.add(
                EventLogRecord(
                    seq = seq,
                    session = sessionFromFile,
                    ts = ts,
                    type = type,
                    payload = raw["payload"] ?: JsonNull,
                )
            )
        }

        return result
    }
}
