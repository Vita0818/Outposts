package com.intatis.shared.session

import com.intatis.shared.SessionKind
import com.intatis.shared.SessionId
import com.intatis.shared.protocol.EventType
import com.intatis.shared.protocol.Jsonx.int
import com.intatis.shared.protocol.Jsonx.str
import com.intatis.shared.protocol.SessionSettingsUpdatedPayload
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.io.File
import java.time.Instant

data class SessionSummary(
    val id: String,
    val kind: SessionKind,
    val updatedAt: Instant,
    val eventCount: Int,
    val displayName: String?,
)

/**
 * Filesystem layout for session persistence:
 * one session = <root>/<sessionID>/events.jsonl (+ artifacts/, session.json).
 */
object SessionHistoryStore {
    const val EVENTS_FILE_NAME = "events.jsonl"

    fun sessionDirectory(root: File, sessionId: String): File {
        require(!sessionId.contains("..") && !sessionId.contains('/') && !sessionId.contains('\\')) {
            "invalid session id"
        }
        return File(root, sessionId)
    }

    fun sessionFile(root: File, sessionId: String): File =
        File(sessionDirectory(root, sessionId), EVENTS_FILE_NAME)

    fun artifactsDir(root: File, sessionId: String): File =
        File(sessionDirectory(root, sessionId), "artifacts")

    fun recentSessions(root: File, kind: SessionKind? = null, limit: Int = 50): List<SessionSummary> {
        if (!root.exists()) return emptyList()
        val summaries = mutableListOf<SessionSummary>()
        root.listFiles()?.filter { it.isDirectory }?.forEach { dir ->
            val name = dir.name
            val sessionKind = SessionId(name).kind
            if (kind != null && sessionKind != kind) return@forEach

            val eventsFile = File(dir, EVENTS_FILE_NAME)
            if (!eventsFile.exists()) return@forEach

            val displayName = SessionProjectionStore.load(eventsFile)?.displayName
            var count = 0
            eventsFile.forEachLine { if (it.isNotEmpty()) count++ }
            summaries.add(SessionSummary(
                id = name,
                kind = sessionKind,
                updatedAt = Instant.ofEpochMilli(eventsFile.lastModified()),
                eventCount = count,
                displayName = displayName,
            ))
        }
        return summaries.sortedByDescending { it.updatedAt }.take(limit)
    }

    fun deleteSession(root: File, sessionId: String) {
        val dir = sessionDirectory(root, sessionId)
        if (EventLog.hasActiveWriter(sessionFile(root, sessionId))) {
            throw EventLogException("writer_already_active", "cannot delete a session with a running runtime")
        }
        if (dir.exists()) dir.deleteRecursively()
    }
}

data class SessionProjectionDocument(
    val schemaVersion: Int = 2,
    val sessionId: String = "",
    val kind: String = "chat",
    val displayName: String? = null,
    val projectedThroughSeq: Long = -1,
    val settingsRevision: Int? = null,
)

/**
 * session.json is a rebuildable, secret-free derived cache. Deleting it is always
 * safe; events.jsonl is the only canonical authority.
 */
object SessionProjectionStore {
    const val FILE_NAME = "session.json"

    fun fileFor(eventsFile: File): File = File(eventsFile.parentFile, FILE_NAME)

    fun load(eventsFile: File): SessionProjectionDocument? {
        val file = fileFor(eventsFile)
        if (!file.exists()) return null
        return try {
            val obj = JsonxHelper.parse(file.readText())
            SessionProjectionDocument(
                schemaVersion = obj.int("schema_version") ?: 2,
                sessionId = obj.str("session_id") ?: "",
                kind = obj.str("kind") ?: "chat",
                displayName = obj.str("display_name"),
                projectedThroughSeq = obj.int("projected_through_seq")?.toLong() ?: 0,
                settingsRevision = obj.int("settings_revision"),
            )
        } catch (_: Exception) {
            null // derived cache: unreadable means rebuild
        }
    }

    fun rebuild(log: EventLog): SessionProjectionDocument {
        var displayName: String? = null
        var revision: Int? = null
        var kind = "chat"
        var through = -1L
        for (envelope in log.replay()) {
            through = envelope.seq
            if (envelope.type == EventType.SESSION_SETTINGS_UPDATED) {
                envelope.payload?.str("display_name")?.let { displayName = it }
                envelope.payload?.int("revision")?.let { revision = it }
                envelope.payload?.str("kind")?.let { kind = it }
            }
        }
        return SessionProjectionDocument(
            sessionId = log.sessionId,
            kind = kind,
            displayName = displayName,
            projectedThroughSeq = through,
            settingsRevision = revision,
        )
    }

    fun save(eventsFile: File, document: SessionProjectionDocument) {
        val path = fileFor(eventsFile)
        val json = buildJsonObject {
            put("schema_version", document.schemaVersion)
            put("session_id", document.sessionId)
            put("kind", document.kind)
            document.displayName?.let { put("display_name", it) }
            put("projected_through_seq", document.projectedThroughSeq)
            document.settingsRevision?.let { put("settings_revision", it) }
        }
        val tmp = File(path.path + ".tmp")
        tmp.writeText(JsonxHelper.pretty(json))
        if (!tmp.renameTo(path)) {
            path.delete()
            tmp.renameTo(path)
        }
    }

    /** Set or rename the display name EventLog-first via a settings event append. */
    fun updateDisplayName(log: EventLog, kind: SessionKind, rawName: String, changeKind: String = "updated") {
        var name = rawName.trim()
        if (name.isEmpty()) return
        if (name.length > 120) name = name.take(120)

        val current = rebuild(log)
        val revision = (current.settingsRevision ?: 0) + 1
        log.append(EventType.SESSION_SETTINGS_UPDATED, SessionSettingsUpdatedPayload(
            revision = revision,
            previousRevision = current.settingsRevision,
            changeKind = changeKind,
            kind = kind.wire,
            displayName = name,
        ).toJson())
        save(fileFor(log.filePath), rebuild(log))
    }
}

private object JsonxHelper {
    fun parse(text: String): JsonObject = com.intatis.shared.protocol.Jsonx.parseObject(text)
    fun pretty(element: JsonObject): String = com.intatis.shared.protocol.Jsonx.pretty.encodeToString(JsonObject.serializer(), element)
}
