package com.rokurics.app.domain.canonical

import java.io.File
import java.security.MessageDigest

data class CanonicalFileHandle(
    val logicalPath: String,
    val byteSize: Long,
    val contentHash: CanonicalHash? = null
)

interface CanonicalFileStore {
    fun exists(handle: CanonicalFileHandle): Boolean
    fun hash(handle: CanonicalFileHandle): CanonicalHash
    fun read(handle: CanonicalFileHandle): ByteArray?
    fun write(handle: CanonicalFileHandle, data: ByteArray): Boolean
}

object CanonicalFileRuntime {
    private val store = object : CanonicalFileStore {
        override fun exists(handle: CanonicalFileHandle): Boolean {
            return File(handle.logicalPath).exists()
        }

        override fun hash(handle: CanonicalFileHandle): CanonicalHash {
            val file = File(handle.logicalPath)
            if (!file.exists()) return CanonicalHash(algorithm = "sha256", value = "0".repeat(64))
            val digest = MessageDigest.getInstance("SHA-256")
            file.inputStream().use { input ->
                val buffer = ByteArray(8192)
                var bytesRead: Int
                while (input.read(buffer).also { bytesRead = it } != -1) {
                    digest.update(buffer, 0, bytesRead)
                }
            }
            val hashBytes = digest.digest()
            val value = hashBytes.joinToString("") { "%02x".format(it) }
            return CanonicalHash(algorithm = "sha256", value = value)
        }

        override fun read(handle: CanonicalFileHandle): ByteArray? {
            val file = File(handle.logicalPath)
            if (!file.exists()) return null
            return file.readBytes()
        }

        override fun write(handle: CanonicalFileHandle, data: ByteArray): Boolean {
            return try {
                val file = File(handle.logicalPath)
                file.parentFile?.mkdirs()
                file.writeBytes(data)
                true
            } catch (e: Exception) {
                false
            }
        }
    }

    fun validate(handle: CanonicalFileHandle): Boolean {
        if (!store.exists(handle)) return false
        if (handle.contentHash == null) return true
        val computed = store.hash(handle)
        return computed.algorithm == handle.contentHash.algorithm &&
            computed.value == handle.contentHash.value
    }

    fun getByteSize(handle: CanonicalFileHandle): Long {
        if (!store.exists(handle)) return -1L
        return File(handle.logicalPath).length()
    }

    fun read(handle: CanonicalFileHandle): ByteArray? = store.read(handle)

    fun write(handle: CanonicalFileHandle, data: ByteArray): Boolean = store.write(handle, data)

    fun computeHash(logicalPath: String): CanonicalHash {
        val handle = CanonicalFileHandle(logicalPath = logicalPath, byteSize = File(logicalPath).length())
        return store.hash(handle)
    }

    fun handleFor(logicalPath: String, contentHash: CanonicalHash? = null): CanonicalFileHandle {
        val size = if (File(logicalPath).exists()) File(logicalPath).length() else -1L
        return CanonicalFileHandle(
            logicalPath = logicalPath,
            byteSize = size,
            contentHash = contentHash
        )
    }
}
