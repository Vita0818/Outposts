package com.intatis.shared.session

import com.intatis.shared.protocol.Envelope
import kotlinx.serialization.json.JsonObject
import java.io.Closeable
import java.io.File
import java.io.FileOutputStream
import java.io.IOException
import java.io.RandomAccessFile
import java.nio.channels.FileLock
import java.time.Instant

class EventLogException(val code: String, message: String) : Exception(message)

/**
 * Append-only JSONL session store. Append is the only mutation; replay and stream are
 * projections. Sequence numbers are monotonic and gap-free from zero per session, and
 * the writer lease guarantees a single runtime per session file across processes.
 */
class EventLog private constructor(
    val sessionId: String,
    val filePath: File,
) : Closeable {

    private val lock = Any()
    private var writerLockHandle: RandomAccessFile? = null
    private var writerLock: FileLock? = null
    private var outputStream: FileOutputStream? = null
    private var lastSeq: Long = -1
    private var closed = false

    val sessionDirectory: File get() = filePath.parentFile!!

    /** Invoked on the writing thread after an envelope has been durably appended. */
    var onEnvelopeAppended: ((Envelope) -> Unit)? = null

    fun lastSequence(): Long = synchronized(lock) { lastSeq }

    companion object {
        fun open(sessionId: String, file: File): EventLog {
            file.parentFile?.mkdirs()

            // Writer lease: exclusive OS-level lock on the sidecar; a second runtime
            // fails closed. A stale lock file left by a crashed process locks cleanly.
            val lockPath = File(file.path + ".writer.lock")
            val handle = RandomAccessFile(lockPath, "rw")
            val fileLock = try {
                handle.channel.tryLock()
            } catch (e: java.nio.channels.OverlappingFileLockException) {
                handle.close()
                throw EventLogException("writer_already_active",
                    "another runtime owns ${lockPath.path}: overlapping lock in this process")
            } catch (e: IOException) {
                handle.close()
                throw EventLogException("writer_already_active", "another runtime owns ${lockPath.path}: ${e.message}")
            }
            if (fileLock == null) {
                handle.close()
                throw EventLogException("writer_already_active", "another runtime already owns the writer lease for $sessionId")
            }

            val log = EventLog(sessionId, file)
            log.writerLockHandle = handle
            log.writerLock = fileLock
            log.rescanTail()
            return log
        }

        fun hasActiveWriter(file: File): Boolean {
            val lockPath = File(file.path + ".writer.lock")
            if (!lockPath.exists()) return false
            return try {
                RandomAccessFile(lockPath, "rw").use { handle ->
                    handle.channel.tryLock() ?: return true
                }
                false
            } catch (_: java.nio.channels.OverlappingFileLockException) {
                true
            } catch (_: IOException) {
                true
            }
        }
    }

    private fun rescanTail() {
        lastSeq = -1
        if (!filePath.exists()) return
        filePath.forEachLine { line ->
            if (line.isEmpty()) return@forEachLine
            val envelope = try {
                Envelope.fromJsonLine(line)
            } catch (_: Exception) {
                return@forEachLine // fail-soft: skip undecodable lines (future types)
            }
            if (envelope.session != sessionId) {
                throw EventLogException("session_mismatch",
                    "event line belongs to session ${envelope.session}, expected $sessionId")
            }
            if (envelope.seq <= lastSeq) {
                throw EventLogException("non_monotonic_sequence",
                    "sequence regression at seq ${envelope.seq} (last $lastSeq)")
            }
            lastSeq = envelope.seq
        }
    }

    fun append(type: String, payload: JsonObject?, ts: Instant = Instant.now(), flush: Boolean = true): Envelope {
        synchronized(lock) {
            check(!closed) { "event log is closed" }
            val stream = outputStream ?: FileOutputStream(filePath, true).also { outputStream = it }
            val envelope = Envelope(
                seq = lastSeq + 1,
                ts = ts,
                session = sessionId,
                type = type,
                payload = payload,
            )
            stream.write((envelope.toJsonLine() + "\n").toByteArray(Charsets.UTF_8))
            if (flush) {
                stream.flush()
                stream.fd.sync()
            }
            lastSeq = envelope.seq
            onEnvelopeAppended?.invoke(envelope)
            return envelope
        }
    }

    fun replay(fromSeq: Long = 0): List<Envelope> {
        val result = mutableListOf<Envelope>()
        if (!filePath.exists()) return result
        synchronized(lock) {
            filePath.forEachLine { line ->
                if (line.isEmpty()) return@forEachLine
                val envelope = try {
                    Envelope.fromJsonLine(line)
                } catch (_: Exception) {
                    return@forEachLine
                }
                if (envelope.seq >= fromSeq) result.add(envelope)
            }
        }
        return result
    }

    override fun close() {
        synchronized(lock) {
            if (closed) return
            closed = true
            runCatching { outputStream?.flush() }
            runCatching { outputStream?.close() }
            outputStream = null
            runCatching { writerLock?.release() }
            runCatching { writerLockHandle?.close() }
            writerLock = null
            writerLockHandle = null
            runCatching { File(filePath.path + ".writer.lock").delete() }
        }
    }
}
