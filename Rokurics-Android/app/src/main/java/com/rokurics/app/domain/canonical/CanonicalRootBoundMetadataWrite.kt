package com.rokurics.app.domain.canonical

import java.util.UUID

// ── CanonicalApplyPortMode ──

enum class CanonicalApplyPortMode(val rawValue: String) {
    DISABLED("disabled"),
    FAKE_IN_MEMORY("fakeInMemory"),
    TEST_ROOT_URL("testRootUrl"),
    PRODUCTION_ROOT_DISABLED("productionRootDisabled");

    companion object {
        val allCases: List<CanonicalApplyPortMode> = entries.toList()
    }
}

// ── CanonicalRootBoundMetadataWriteTarget ──

data class CanonicalRootBoundMetadataWriteTarget(
    val rootURL: String,
    val objectID: String,
    val metadataBytes: ByteArray
) {
    val id: String get() = objectID

    val metadataSize: Int get() = metadataBytes.size

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other !is CanonicalRootBoundMetadataWriteTarget) return false
        if (rootURL != other.rootURL) return false
        if (objectID != other.objectID) return false
        if (!metadataBytes.contentEquals(other.metadataBytes)) return false
        return true
    }

    override fun hashCode(): Int {
        var result = rootURL.hashCode()
        result = 31 * result + objectID.hashCode()
        result = 31 * result + metadataBytes.contentHashCode()
        return result
    }

    companion object {
        fun create(
            rootURL: String,
            objectID: String,
            metadataBytes: ByteArray
        ): CanonicalRootBoundMetadataWriteTarget {
            val normalizedRoot = rootURL.trimEnd { it == '/' }
            return CanonicalRootBoundMetadataWriteTarget(
                rootURL = normalizedRoot,
                objectID = objectID.trim().nilIfEmpty ?: "unknown-object",
                metadataBytes = metadataBytes
            )
        }
    }
}

// ── CanonicalRootBoundMetadataWriteResult ──

data class CanonicalRootBoundMetadataWriteResult(
    val success: Boolean,
    val rollbackCheckpointID: String?,
    val atomicWritePerformed: Boolean,
    val postconditionVerified: Boolean,
    val failureReason: String?
) {
    val diagnosticSummary: String
        get() = listOf(
            "success=$success",
            "atomicWritePerformed=$atomicWritePerformed",
            "postconditionVerified=$postconditionVerified",
            "rollbackCheckpointID=${rollbackCheckpointID?.take(8) ?: "none"}",
            "failureReason=${failureReason ?: "none"}"
        ).joinToString(",")
}

// ── CanonicalRootBoundMetadataWriteRollbackResult ──

data class CanonicalRootBoundMetadataWriteRollbackResult(
    val rolledBack: Boolean,
    val checkpointRestored: Boolean
) {
    val diagnosticSummary: String
        get() = "rolledBack=$rolledBack,checkpointRestored=$checkpointRestored"
}

// ── CanonicalRootBoundMetadataWriteCore ──

object CanonicalRootBoundMetadataWriteCore {
    private val checkpoints = mutableMapOf<String, CanonicalRootBoundMetadataWriteTarget>()

    fun validateTarget(
        target: CanonicalRootBoundMetadataWriteTarget,
        mode: CanonicalApplyPortMode
    ): Boolean {
        if (mode == CanonicalApplyPortMode.DISABLED) return false

        val normalizedRoot = target.rootURL.trim()
        if (normalizedRoot.isEmpty()) return false

        if (normalizedRoot.contains("..")) return false

        if (mode == CanonicalApplyPortMode.PRODUCTION_ROOT_DISABLED) {
            if (normalizedRoot.startsWith("file:///") ||
                normalizedRoot.startsWith("/")
            ) {
                return false
            }
        }

        if (mode == CanonicalApplyPortMode.TEST_ROOT_URL) {
            if (!normalizedRoot.contains("test") &&
                !normalizedRoot.contains("staging") &&
                !normalizedRoot.contains("sandbox")
            ) {
                return false
            }
        }

        return true
    }

    fun atomicWrite(
        target: CanonicalRootBoundMetadataWriteTarget,
        mode: CanonicalApplyPortMode
    ): CanonicalRootBoundMetadataWriteResult {
        if (!validateTarget(target, mode)) {
            return CanonicalRootBoundMetadataWriteResult(
                success = false,
                rollbackCheckpointID = null,
                atomicWritePerformed = false,
                postconditionVerified = false,
                failureReason = "validationFailed: rootURL=${target.rootURL}, mode=${mode.rawValue}"
            )
        }

        val checkpointID = UUID.randomUUID().toString()
        checkpoints[checkpointID] = target

        val writePerformed = when (mode) {
            CanonicalApplyPortMode.FAKE_IN_MEMORY -> {
                inMemoryWrite(target)
            }
            CanonicalApplyPortMode.TEST_ROOT_URL -> {
                testWrite(target)
            }
            else -> false
        }

        val postconditionOK = if (writePerformed) {
            postcondition(target, mode)
        } else {
            false
        }

        return if (writePerformed && postconditionOK) {
            CanonicalRootBoundMetadataWriteResult(
                success = true,
                rollbackCheckpointID = checkpointID,
                atomicWritePerformed = true,
                postconditionVerified = true,
                failureReason = null
            )
        } else {
            checkpoints.remove(checkpointID)
            CanonicalRootBoundMetadataWriteResult(
                success = false,
                rollbackCheckpointID = null,
                atomicWritePerformed = writePerformed,
                postconditionVerified = postconditionOK,
                failureReason = when {
                    !writePerformed -> "writeFailed"
                    !postconditionOK -> "postconditionFailed"
                    else -> "unknownFailure"
                }
            )
        }
    }

    fun rollback(checkpointID: String): CanonicalRootBoundMetadataWriteRollbackResult {
        val target = checkpoints.remove(checkpointID)
        return if (target != null) {
            CanonicalRootBoundMetadataWriteRollbackResult(
                rolledBack = true,
                checkpointRestored = true
            )
        } else {
            CanonicalRootBoundMetadataWriteRollbackResult(
                rolledBack = false,
                checkpointRestored = false
            )
        }
    }

    fun postcondition(
        target: CanonicalRootBoundMetadataWriteTarget,
        mode: CanonicalApplyPortMode
    ): Boolean {
        if (mode == CanonicalApplyPortMode.DISABLED ||
            mode == CanonicalApplyPortMode.PRODUCTION_ROOT_DISABLED
        ) {
            return false
        }

        return target.metadataBytes.isNotEmpty() &&
            target.objectID.isNotEmpty() &&
            target.rootURL.isNotEmpty()
    }

    fun validateExistingCheckpoint(checkpointID: String): Boolean {
        return checkpoints.containsKey(checkpointID)
    }

    fun clearCheckpoint(checkpointID: String): Boolean {
        return checkpoints.remove(checkpointID) != null
    }

    fun activeCheckpointCount(): Int = checkpoints.size

    private fun inMemoryWrite(target: CanonicalRootBoundMetadataWriteTarget): Boolean {
        return target.metadataBytes.isNotEmpty() && target.objectID.isNotEmpty()
    }

    private fun testWrite(target: CanonicalRootBoundMetadataWriteTarget): Boolean {
        return inMemoryWrite(target)
    }
}
