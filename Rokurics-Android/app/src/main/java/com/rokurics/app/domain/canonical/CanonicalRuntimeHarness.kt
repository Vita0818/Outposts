package com.rokurics.app.domain.canonical

import java.util.Date
import java.util.UUID

// ── Type 1: CanonicalRuntimeHarnessNodeRole ──

enum class CanonicalRuntimeHarnessNodeRole(val rawValue: String) {
    IPHONE("iphone"),
    MAC("mac");

    companion object {
        fun fromValue(value: String): CanonicalRuntimeHarnessNodeRole =
            entries.first { it.rawValue == value }
    }
}

// ── Type 2: CanonicalRuntimeHarnessFileStore ──

data class CanonicalRuntimeHarnessFileStore(
    val storeID: String = UUID.randomUUID().toString(),
    val basePath: String = "/tmp/canonical-runtime-harness",
    val readOnly: Boolean = false
) {
    private val storedFiles: MutableMap<String, String> = mutableMapOf()

    fun write(key: String, value: String): Boolean {
        if (readOnly) return false
        storedFiles[key] = value
        return true
    }

    fun read(key: String): String? {
        return storedFiles[key]
    }

    fun exists(key: String): Boolean {
        return storedFiles.containsKey(key)
    }

    fun delete(key: String): Boolean {
        if (readOnly) return false
        storedFiles.remove(key)
        return true
    }

    val fileCount: Int
        get() = storedFiles.size

    val fileKeys: Set<String>
        get() = storedFiles.keys.toSet()
}

// ── Type 3: CanonicalRuntimeHarnessTransport ──

data class CanonicalRuntimeHarnessTransport(
    val transportID: String = UUID.randomUUID().toString(),
    val enabled: Boolean = true
) {
    private val sentMessages: MutableList<TransportMessage> = mutableListOf()
    private val receivedMessages: MutableList<TransportMessage> = mutableListOf()

    data class TransportMessage(
        val messageID: String,
        val fromNodeID: String,
        val toNodeID: String,
        val payload: String,
        val timestamp: CanonicalTimestamp
    ) {
        val id: String get() = messageID
    }

    fun send(from: String, to: String, payload: String): TransportMessage {
        val msg = TransportMessage(
            messageID = UUID.randomUUID().toString(),
            fromNodeID = from,
            toNodeID = to,
            payload = payload,
            timestamp = CanonicalTimestamp(Date())
        )
        sentMessages.add(msg)
        return msg
    }

    fun receive(targetNodeID: String): List<TransportMessage> {
        val pending = sentMessages.filter { it.toNodeID == targetNodeID && it !in receivedMessages }
        receivedMessages.addAll(pending)
        return pending
    }

    val pendingCount: Int
        get() = sentMessages.size - receivedMessages.size

    val sentCount: Int
        get() = sentMessages.size

    val receivedCount: Int
        get() = receivedMessages.size

    fun clear() {
        sentMessages.clear()
        receivedMessages.clear()
    }
}

// ── Type 4: CanonicalRuntimeHarnessExecutor ──

class CanonicalRuntimeHarnessExecutor(
    val executorID: String = UUID.randomUUID().toString(),
    val maxHistory: Int = 256
) {
    private val executionHistory: MutableList<ExecutionRecord> = mutableListOf()

    data class ExecutionRecord(
        val recordID: String,
        val phase: String,
        val nodeID: String,
        val action: String,
        val result: String,
        val timestamp: CanonicalTimestamp,
        val diagnostics: String?
    ) {
        val id: String get() = recordID
    }

    data class ExecutionResult(
        val record: ExecutionRecord,
        val succeeded: Boolean
    )

    fun execute(
        phase: String,
        nodeID: String,
        action: String,
        result: String = "completed",
        diagnostics: String? = null
    ): ExecutionResult {
        val record = ExecutionRecord(
            recordID = UUID.randomUUID().toString(),
            phase = phase,
            nodeID = nodeID,
            action = action,
            result = result,
            timestamp = CanonicalTimestamp(Date()),
            diagnostics = diagnostics?.trim()?.nilIfEmpty
        )
        executionHistory.add(record)
        if (executionHistory.size > maxHistory) {
            executionHistory.removeAt(0)
        }
        return ExecutionResult(record = record, succeeded = result == "completed")
    }

    val history: List<ExecutionRecord>
        get() = executionHistory.toList()

    val historySummary: String
        get() = "executions=${executionHistory.size},lastPhase=${executionHistory.lastOrNull()?.phase ?: "none"}"

    fun clear() {
        executionHistory.clear()
    }
}

// ── Type 5: CanonicalRuntimeHarness ──

data class CanonicalHarnessNode(
    val nodeID: String,
    val role: CanonicalRuntimeHarnessNodeRole,
    val store: CanonicalRuntimeHarnessFileStore = CanonicalRuntimeHarnessFileStore(),
    val transport: CanonicalRuntimeHarnessTransport = CanonicalRuntimeHarnessTransport()
) {
    val id: String get() = nodeID
}

class CanonicalRuntimeHarness(
    private val localRole: CanonicalRuntimeHarnessNodeRole = CanonicalRuntimeHarnessNodeRole.IPHONE,
    private val peerRole: CanonicalRuntimeHarnessNodeRole = CanonicalRuntimeHarnessNodeRole.MAC,
    private val tickLimit: Int = 128
) {
    private val nodes: MutableMap<String, CanonicalHarnessNode> = mutableMapOf()
    private val diagnosticsCollector: MutableList<CanonicalRuntimeHarnessDiagnostic> = mutableListOf()
    private var tickCount: Int = 0
    private val phaseHistory: MutableList<String> = mutableListOf()

    data class CanonicalRuntimeHarnessDiagnostic(
        val kind: CanonicalRuntimeHarnessDiagnosticKind,
        val phase: String,
        val nodeID: String?,
        val detail: String?,
        val timestamp: CanonicalTimestamp
    ) {
        val id: String
            get() = listOfNotNull(kind.rawValue, phase, nodeID, detail).joinToString("|")
    }

    enum class CanonicalRuntimeHarnessDiagnosticKind(val rawValue: String) {
        HARNESS_INIT("harnessInit"),
        NODE_STARTED("nodeStarted"),
        TICK_STARTED("tickStarted"),
        TICK_COMPLETED("tickCompleted"),
        INVARIANT_HELD("invariantHeld"),
        INVARIANT_BROKEN("invariantBroken"),
        PROJECTION_READ("projectionRead"),
        HARNESS_STOPPED("harnessStopped"),
        HARNESS_LIMIT_REACHED("harnessLimitReached"),
        TRANSPORT_MESSAGE_SENT("transportMessageSent"),
        TRANSPORT_MESSAGE_RECEIVED("transportMessageReceived"),
        FILE_WRITTEN("fileWritten"),
        FILE_READ("fileRead")
    }

    fun startNodes(
        localNodeID: String = "local-${UUID.randomUUID().toString().take(8)}",
        peerNodeID: String = "peer-${UUID.randomUUID().toString().take(8)}"
    ): CanonicalRuntimeHarness {
        val local = CanonicalHarnessNode(
            nodeID = localNodeID,
            role = localRole,
            store = CanonicalRuntimeHarnessFileStore(basePath = "/harness/$localNodeID"),
            transport = CanonicalRuntimeHarnessTransport()
        )
        val peer = CanonicalHarnessNode(
            nodeID = peerNodeID,
            role = peerRole,
            store = CanonicalRuntimeHarnessFileStore(basePath = "/harness/$peerNodeID"),
            transport = CanonicalRuntimeHarnessTransport()
        )
        nodes.clear()
        nodes[localNodeID] = local
        nodes[peerNodeID] = peer
        phaseHistory.clear()
        tickCount = 0
        diagnosticsCollector.clear()
        val now = Date()
        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.HARNESS_INIT,
                phase = "init",
                nodeID = null,
                detail = "local=${localNodeID},peer=${peerNodeID}",
                timestamp = CanonicalTimestamp(now)
            )
        )
        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.NODE_STARTED,
                phase = "init",
                nodeID = localNodeID,
                detail = "role=${localRole.rawValue}",
                timestamp = CanonicalTimestamp(now)
            )
        )
        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.NODE_STARTED,
                phase = "init",
                nodeID = peerNodeID,
                detail = "role=${peerRole.rawValue}",
                timestamp = CanonicalTimestamp(now)
            )
        )
        return this
    }

    fun tick(): CanonicalRuntimeHarnessResult {
        tickCount++
        val tickID = "tick-$tickCount"
        phaseHistory.add(tickID)

        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.TICK_STARTED,
                phase = tickID,
                nodeID = null,
                detail = null,
                timestamp = CanonicalTimestamp(Date())
            )
        )

        if (tickCount >= tickLimit) {
            diagnosticsCollector.add(
                CanonicalRuntimeHarnessDiagnostic(
                    kind = CanonicalRuntimeHarnessDiagnosticKind.HARNESS_LIMIT_REACHED,
                    phase = tickID,
                    nodeID = null,
                    detail = "limit=$tickLimit",
                    timestamp = CanonicalTimestamp(Date())
                )
            )
        }

        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.TICK_COMPLETED,
                phase = tickID,
                nodeID = null,
                detail = null,
                timestamp = CanonicalTimestamp(Date())
            )
        )

        return buildResult("active")
    }

    fun assertInvariant(condition: String, predicate: () -> Boolean): Boolean {
        val held = predicate()
        val phase = phaseHistory.lastOrNull() ?: "pre-tick"
        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = if (held) CanonicalRuntimeHarnessDiagnosticKind.INVARIANT_HELD
                else CanonicalRuntimeHarnessDiagnosticKind.INVARIANT_BROKEN,
                phase = phase,
                nodeID = null,
                detail = "condition=$condition,held=$held",
                timestamp = CanonicalTimestamp(Date())
            )
        )
        return held
    }

    fun readProjection(nodeID: String, projectionID: String): String? {
        val node = nodes[nodeID]
        val phase = phaseHistory.lastOrNull() ?: "pre-tick"
        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.PROJECTION_READ,
                phase = phase,
                nodeID = nodeID,
                detail = "projection=$projectionID",
                timestamp = CanonicalTimestamp(Date())
            )
        )
        return node?.store?.read(projectionID)
    }

    fun writeFile(nodeID: String, key: String, value: String): Boolean {
        val node = nodes[nodeID] ?: return false
        val success = node.store.write(key, value)
        val phase = phaseHistory.lastOrNull() ?: "pre-tick"
        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.FILE_WRITTEN,
                phase = phase,
                nodeID = nodeID,
                detail = "key=$key,success=$success",
                timestamp = CanonicalTimestamp(Date())
            )
        )
        return success
    }

    fun sendMessage(fromNodeID: String, toNodeID: String, payload: String): Boolean {
        val fromNode = nodes[fromNodeID] ?: return false
        val toNode = nodes[toNodeID] ?: return false
        val msg = fromNode.transport.send(fromNodeID, toNodeID, payload)
        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.TRANSPORT_MESSAGE_SENT,
                phase = phaseHistory.lastOrNull() ?: "pre-tick",
                nodeID = fromNodeID,
                detail = "to=$toNodeID,msgID=${msg.messageID}",
                timestamp = CanonicalTimestamp(Date())
            )
        )
        return true
    }

    fun receiveMessages(nodeID: String): List<CanonicalRuntimeHarnessTransport.TransportMessage> {
        val node = nodes[nodeID] ?: return emptyList()
        val messages = node.transport.receive(nodeID)
        val phase = phaseHistory.lastOrNull() ?: "pre-tick"
        messages.forEach { msg ->
            diagnosticsCollector.add(
                CanonicalRuntimeHarnessDiagnostic(
                    kind = CanonicalRuntimeHarnessDiagnosticKind.TRANSPORT_MESSAGE_RECEIVED,
                    phase = phase,
                    nodeID = nodeID,
                    detail = "from=${msg.fromNodeID},msgID=${msg.messageID}",
                    timestamp = CanonicalTimestamp(Date())
                )
            )
        }
        return messages
    }

    fun getNode(nodeID: String): CanonicalHarnessNode? {
        return nodes[nodeID]
    }

    fun stop(): CanonicalRuntimeHarnessResult {
        diagnosticsCollector.add(
            CanonicalRuntimeHarnessDiagnostic(
                kind = CanonicalRuntimeHarnessDiagnosticKind.HARNESS_STOPPED,
                phase = phaseHistory.lastOrNull() ?: "pre-tick",
                nodeID = null,
                detail = null,
                timestamp = CanonicalTimestamp(Date())
            )
        )
        return buildResult("stopped")
    }

    val allNodes: List<CanonicalHarnessNode>
        get() = nodes.values.toList().sortedBy { it.nodeID }

    val nodeIDs: List<String>
        get() = nodes.keys.toList().sorted()

    val nodeCount: Int
        get() = nodes.size

    val phaseCount: Int
        get() = phaseHistory.size

    val diagnostics: List<CanonicalRuntimeHarnessDiagnostic>
        get() = diagnosticsCollector.toList()

    val records: List<CanonicalRuntimeHarnessDiagnostic>
        get() = diagnosticsCollector.toList()

    private fun buildResult(state: String): CanonicalRuntimeHarnessResult {
        return CanonicalRuntimeHarnessResult(
            phases = phaseHistory.toList(),
            tickCount = tickCount,
            nodeIDs = nodeIDs,
            state = state,
            diagnostics = diagnosticsCollector.toList(),
            generatedAt = CanonicalTimestamp(Date())
        )
    }
}

// ── Type 6: CanonicalRuntimeHarnessResult ──

data class CanonicalRuntimeHarnessResult(
    val phases: List<String>,
    val tickCount: Int,
    val nodeIDs: List<String>,
    val state: String,
    val diagnostics: List<CanonicalRuntimeHarness.CanonicalRuntimeHarnessDiagnostic>,
    val generatedAt: CanonicalTimestamp
) {
    val diagnosticsSummary: String
        get() {
            val kindCounts = diagnostics.groupBy { it.kind }
                .mapValues { it.value.size }
                .entries
                .sortedBy { it.key.rawValue }
                .joinToString("|") { "${it.key.rawValue}=${it.value}" }
            val invariantBroken = diagnostics.count {
                it.kind == CanonicalRuntimeHarness.CanonicalRuntimeHarnessDiagnosticKind.INVARIANT_BROKEN
            }
            val invariantHeld = diagnostics.count {
                it.kind == CanonicalRuntimeHarness.CanonicalRuntimeHarnessDiagnosticKind.INVARIANT_HELD
            }
            return listOf(
                "runtimeHarness=v1",
                "state=$state",
                "ticks=$tickCount",
                "phases=${phases.size}",
                "nodes=${nodeIDs.joinToString("+")}",
                "diagnostics=${diagnostics.size}",
                "invariantsHeld=$invariantHeld",
                "invariantsBroken=$invariantBroken",
                "kinds=$kindCounts"
            ).joinToString(",")
        }

    val isHealthy: Boolean
        get() = diagnostics.none {
            it.kind == CanonicalRuntimeHarness.CanonicalRuntimeHarnessDiagnosticKind.INVARIANT_BROKEN
        }

    val invariantBreakages: List<CanonicalRuntimeHarness.CanonicalRuntimeHarnessDiagnostic>
        get() = diagnostics.filter {
            it.kind == CanonicalRuntimeHarness.CanonicalRuntimeHarnessDiagnosticKind.INVARIANT_BROKEN
        }
}
