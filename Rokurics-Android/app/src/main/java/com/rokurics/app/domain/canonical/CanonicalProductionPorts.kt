package com.rokurics.app.domain.canonical

// ═══════════════════════════════════════════
// CanonicalProductionPortKind
// ═══════════════════════════════════════════

enum class CanonicalProductionPortKind(val rawValue: String) {
    FILE("file"),
    TRANSPORT("transport"),
    UPLOAD("upload"),
    APPLY("apply");

    companion object {
        val allCases: List<CanonicalProductionPortKind> = entries.toList()
    }
}

// ═══════════════════════════════════════════
// CanonicalProductionFilePort
// ═══════════════════════════════════════════

interface CanonicalProductionFilePort {
    fun exists(path: String): Boolean
    fun read(path: String): ByteArray
    fun write(path: String, data: ByteArray)
    fun hash(path: String): CanonicalHash
    fun byteSize(path: String): Long
}

// ═══════════════════════════════════════════
// CanonicalProductionTransportPort
// ═══════════════════════════════════════════

interface CanonicalProductionTransportPort {
    fun send(data: ByteArray, destination: String)
}

// ═══════════════════════════════════════════
// CanonicalProductionUploadPort
// ═══════════════════════════════════════════

interface CanonicalProductionUploadPort {
    fun start(sessionID: String, objectID: String, totalBytes: Long)
    fun status(sessionID: String): CanonicalAudioUploadSession
    fun chunk(sessionID: String, offset: Long, data: ByteArray)
    fun finalize(sessionID: String, proof: CanonicalAudioUploadFinalizeProof)
    fun abort(sessionID: String, reason: String)
}

// ═══════════════════════════════════════════
// CanonicalProductionApplyPort
// ═══════════════════════════════════════════

interface CanonicalProductionApplyPort {
    fun applyMetadata(objectID: String, metadata: CanonicalRecordingMetadata)
    fun isProductionRoot(): Boolean
    fun isTestRoot(): Boolean
    fun rootURL(): String
}

// ═══════════════════════════════════════════
// CanonicalProductionPortSet
// ═══════════════════════════════════════════

data class CanonicalProductionPortSet(
    val filePort: CanonicalProductionFilePort? = null,
    val transportPort: CanonicalProductionTransportPort? = null,
    val uploadPort: CanonicalProductionUploadPort? = null,
    val applyPort: CanonicalProductionApplyPort? = null
) {
    val hasFilePort: Boolean get() = filePort != null
    val hasTransportPort: Boolean get() = transportPort != null
    val hasUploadPort: Boolean get() = uploadPort != null
    val hasApplyPort: Boolean get() = applyPort != null
}

// ═══════════════════════════════════════════
// CanonicalProductionRootToken
// ═══════════════════════════════════════════

data class CanonicalProductionRootToken
private constructor(
    val rootURL: String,
    val token: String,
    val production: Boolean
) {
    val isTestToken: Boolean get() = !production

    companion object {
        operator fun invoke(
            rootURL: String,
            token: String,
            production: Boolean
        ): CanonicalProductionRootToken {
            return CanonicalProductionRootToken(
                rootURL = rootURL.trim(),
                token = CanonicalProductionRedaction.safeIdentifier(token, "unknown-token"),
                production = production
            )
        }
    }
}

// ═══════════════════════════════════════════
// CanonicalProductionPortFactory
// ═══════════════════════════════════════════

object CanonicalProductionPortFactory {

    fun fakeFilePort(): CanonicalProductionFilePort {
        return FakeFilePort()
    }

    fun fakeTransportPort(): CanonicalProductionTransportPort {
        return FakeTransportPort()
    }

    fun fakeUploadPort(): CanonicalProductionUploadPort {
        return FakeUploadPort()
    }

    fun fakeApplyPort(
        rootURL: String = "/tmp/canonical-test-root",
        production: Boolean = false
    ): CanonicalProductionApplyPort {
        return FakeApplyPort(rootURL, production)
    }

    fun testRootToken(): CanonicalProductionRootToken {
        return CanonicalProductionRootToken(
            rootURL = "/tmp/canonical-test-root",
            token = "test-token",
            production = false
        )
    }

    fun productionRootToken(
        rootURL: String,
        token: String
    ): CanonicalProductionRootToken {
        return CanonicalProductionRootToken(
            rootURL = rootURL,
            token = token,
            production = true
        )
    }

    // ── Fake implementations ──────────────────────────────────────

    private class FakeFilePort : CanonicalProductionFilePort {
        private val storage = mutableMapOf<String, ByteArray>()

        override fun exists(path: String): Boolean {
            return storage.containsKey(path.trim())
        }

        override fun read(path: String): ByteArray {
            return storage[path.trim()] ?: ByteArray(0)
        }

        override fun write(path: String, data: ByteArray) {
            storage[path.trim()] = data
        }

        override fun hash(path: String): CanonicalHash {
            val data = storage[path.trim()] ?: return CanonicalHash("sha256", "")
            return CanonicalHash.sha256String(String(data, Charsets.UTF_8))
        }

        override fun byteSize(path: String): Long {
            return (storage[path.trim()]?.size ?: 0).toLong()
        }
    }

    private class FakeTransportPort : CanonicalProductionTransportPort {
        val sent = mutableListOf<Pair<ByteArray, String>>()

        override fun send(data: ByteArray, destination: String) {
            sent.add(data to destination.trim())
        }
    }

    private class FakeUploadPort : CanonicalProductionUploadPort {
        val sessions = mutableMapOf<String, CanonicalAudioUploadSession>()

        override fun start(sessionID: String, objectID: String, totalBytes: Long) {
            sessions[sessionID.trim()] = CanonicalAudioUploadSession(
                sessionID = sessionID.trim(),
                objectID = objectID.trim(),
                state = CanonicalAudioUploadSessionState.started,
                totalBytes = totalBytes
            )
        }

        override fun status(sessionID: String): CanonicalAudioUploadSession {
            return sessions[sessionID.trim()] ?: CanonicalAudioUploadSession(
                sessionID = sessionID.trim(),
                objectID = "",
                state = CanonicalAudioUploadSessionState.failed
            )
        }

        override fun chunk(sessionID: String, offset: Long, data: ByteArray) {
            val key = sessionID.trim()
            val session = sessions[key] ?: return
            sessions[key] = session.copy(
                state = CanonicalAudioUploadSessionState.chunking,
                confirmedBytes = offset + data.size
            )
        }

        override fun finalize(sessionID: String, proof: CanonicalAudioUploadFinalizeProof) {
            val key = sessionID.trim()
            val session = sessions[key] ?: return
            sessions[key] = session.copy(
                state = CanonicalAudioUploadSessionState.finalized,
                confirmedBytes = proof.totalBytes,
                contentHash = proof.contentHash
            )
        }

        override fun abort(sessionID: String, reason: String) {
            val key = sessionID.trim()
            val session = sessions[key] ?: return
            sessions[key] = session.copy(state = CanonicalAudioUploadSessionState.aborted)
        }
    }

    private class FakeApplyPort(
        private val root: String,
        private val isProduction: Boolean
    ) : CanonicalProductionApplyPort {
        val appliedMetadata = mutableMapOf<String, CanonicalRecordingMetadata>()

        override fun applyMetadata(objectID: String, metadata: CanonicalRecordingMetadata) {
            appliedMetadata[objectID.trim()] = metadata
        }

        override fun isProductionRoot(): Boolean = isProduction

        override fun isTestRoot(): Boolean = !isProduction

        override fun rootURL(): String = root
    }
}
