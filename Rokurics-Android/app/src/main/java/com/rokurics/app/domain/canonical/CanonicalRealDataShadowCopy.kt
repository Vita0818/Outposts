package com.rokurics.app.domain.canonical

import java.io.File
import java.io.FileInputStream
import java.io.FileOutputStream
import java.security.MessageDigest
import java.util.Date
import java.util.Locale

// ── CanonicalRealDataShadowCopyPolicy ──

data class CanonicalRealDataShadowCopyPolicy(
    val enabled: Boolean = false,
    val maxFiles: Int = 10_000,
    val maxBytes: Long = 512L * 1024L * 1024L,
    val excludeProductionRoot: Boolean = true
) {
    companion object {
        val DISABLED = CanonicalRealDataShadowCopyPolicy()
    }
}

// ── CanonicalRealDataShadowCopyConfigurationMode ──

enum class CanonicalRealDataShadowCopyConfigurationMode(val rawValue: String) {
    DISABLED("disabled"),
    SHADOW_COPY_ONLY("shadowCopyOnly");

    companion object {
        val allCases: List<CanonicalRealDataShadowCopyConfigurationMode> = entries.toList()
    }
}

// ── CanonicalRealDataShadowCopyConfiguration ──

data class CanonicalRealDataShadowCopyConfiguration(
    val mode: CanonicalRealDataShadowCopyConfigurationMode = CanonicalRealDataShadowCopyConfigurationMode.DISABLED,
    val productionRootURL: String? = null,
    val shadowRootURL: String? = null,
    val maxFiles: Int = 10_000,
    val maxBytes: Long = 512L * 1024L * 1024L,
    val excludeProductionRoot: Boolean = true
) {
    companion object {
        val DISABLED = CanonicalRealDataShadowCopyConfiguration()
    }
}

// ── CanonicalRealDataShadowCopyEvidence ──

data class CanonicalRealDataShadowCopyEvidence(
    val fileCount: Int,
    val byteCount: Long,
    val metadataEvidence: List<String>,
    val audioBytesBlocked: Boolean,
    val productionRootRefused: Boolean
) {
    val id: String
        get() = "evidence:$fileCount:$byteCount"

    val isSound: Boolean
        get() = !audioBytesBlocked && !productionRootRefused

    val summary: String
        get() = listOf(
            "files=$fileCount",
            "bytes=$byteCount",
            "metadataEntries=${metadataEvidence.size}",
            "audioBlocked=$audioBytesBlocked",
            "productionRootRefused=$productionRootRefused"
        ).joinToString(",")

    companion object {
        fun empty(reason: String): CanonicalRealDataShadowCopyEvidence {
            return CanonicalRealDataShadowCopyEvidence(
                fileCount = 0,
                byteCount = 0L,
                metadataEvidence = listOf("empty:$reason"),
                audioBytesBlocked = false,
                productionRootRefused = false
            )
        }

        fun refused(): CanonicalRealDataShadowCopyEvidence {
            return CanonicalRealDataShadowCopyEvidence(
                fileCount = 0,
                byteCount = 0L,
                metadataEvidence = listOf("production_root_refused"),
                audioBytesBlocked = false,
                productionRootRefused = true
            )
        }
    }
}

// ── CanonicalRealDataShadowCopy ──

class CanonicalRealDataShadowCopy(
    private val configuration: CanonicalRealDataShadowCopyConfiguration = CanonicalRealDataShadowCopyConfiguration.DISABLED,
    private val policy: CanonicalRealDataShadowCopyPolicy = CanonicalRealDataShadowCopyPolicy.DISABLED
) {

    private val audioExtensions = setOf(
        "mp3", "wav", "m4a", "aac", "ogg", "flac", "wma", "opus",
        "aiff", "caf", "amr", "webm"
    )

    fun copyToShadow(): CanonicalRealDataShadowCopyEvidence {
        if (!policy.enabled || configuration.mode == CanonicalRealDataShadowCopyConfigurationMode.DISABLED) {
            return CanonicalRealDataShadowCopyEvidence.empty("disabled")
        }

        val productionRoot = configuration.productionRootURL?.trim()?.nilIfEmpty
        val shadowRoot = configuration.shadowRootURL?.trim()?.nilIfEmpty

        if (productionRoot == null || shadowRoot == null) {
            return CanonicalRealDataShadowCopyEvidence.empty(
                "missing_roots:production=${productionRoot != null},shadow=${shadowRoot != null}"
            )
        }

        if (policy.excludeProductionRoot && isWithinProductionRoot(productionRoot, shadowRoot)) {
            return CanonicalRealDataShadowCopyEvidence.refused()
        }

        val productionDir = File(productionRoot)
        if (!productionDir.exists() || !productionDir.isDirectory) {
            return CanonicalRealDataShadowCopyEvidence.empty(
                "production_root_invalid:${productionDir.absolutePath}"
            )
        }

        val shadowDir = File(shadowRoot)
        if (!ensureShadowDirectory(shadowDir)) {
            return CanonicalRealDataShadowCopyEvidence.empty(
                "shadow_directory_creation_failed:${shadowDir.absolutePath}"
            )
        }

        val metadataEvidence = mutableListOf<String>()
        metadataEvidence.add("copy_started_at=${Date()}")
        metadataEvidence.add("production_root=${productionDir.absolutePath}")
        metadataEvidence.add("shadow_root=${shadowDir.absolutePath}")

        val maxFiles = configuration.maxFiles.coerceAtMost(policy.maxFiles)
        val maxBytes = configuration.maxBytes.coerceAtMost(policy.maxBytes)

        var fileCount = 0
        var byteCount = 0L
        var audioBytesBlocked = false

        val productionFiles = try {
            productionDir.walkTopDown()
                .filter { it.isFile }
                .sortedBy { it.absolutePath }
                .take(maxFiles)
                .toList()
        } catch (e: Exception) {
            metadataEvidence.add("walk_error=${sanitizeExceptionMessage(e)}")
            return CanonicalRealDataShadowCopyEvidence.empty("production_walk_failed")
        }

        metadataEvidence.add("productionFiles_discovered=${productionFiles.size}")

        for (file in productionFiles) {
            if (fileCount >= maxFiles) {
                metadataEvidence.add("max_files_reached=$maxFiles")
                break
            }
            if (byteCount >= maxBytes) {
                metadataEvidence.add("max_bytes_reached=$maxBytes")
                break
            }

            val extension = file.extension.lowercase()
            if (extension in audioExtensions) {
                audioBytesBlocked = true
                metadataEvidence.add("audio_blocked=${file.name}")
                continue
            }

            val fileSize = file.length()
            if (byteCount + fileSize > maxBytes) {
                metadataEvidence.add("file_skipped_quota=${file.name}:$fileSize")
                continue
            }

            if (isSensitiveFile(file)) {
                metadataEvidence.add("sensitive_skipped=${file.name}")
                continue
            }

            val relativePath = file.relativeTo(productionDir).path
            val targetFile = File(shadowDir, relativePath)

            try {
                targetFile.parentFile?.mkdirs()
                val copied = copyFile(file, targetFile)
                if (copied) {
                    fileCount++
                    byteCount += fileSize
                    metadataEvidence.add(
                        "copied=${relativePath}:$fileSize:" +
                            computeFileHash(targetFile)?.take(8) ?: "nohash"
                    )
                } else {
                    metadataEvidence.add("copy_failed=${relativePath}")
                }
            } catch (e: Exception) {
                metadataEvidence.add(
                    "copy_error=${relativePath}:${sanitizeExceptionMessage(e)}"
                )
            }
        }

        metadataEvidence.add("copy_completed_at=${Date()}")

        return CanonicalRealDataShadowCopyEvidence(
            fileCount = fileCount,
            byteCount = byteCount,
            metadataEvidence = metadataEvidence,
            audioBytesBlocked = audioBytesBlocked,
            productionRootRefused = false
        )
    }

    private fun ensureShadowDirectory(dir: File): Boolean {
        return try {
            if (dir.exists()) {
                dir.isDirectory && dir.canWrite()
            } else {
                dir.mkdirs()
            }
        } catch (_: Exception) {
            false
        }
    }

    private fun isWithinProductionRoot(productionRoot: String, shadowRoot: String): Boolean {
        return try {
            val productionPath = File(productionRoot).canonicalPath.trimEnd('/') + "/"
            val shadowPath = File(shadowRoot).canonicalPath.trimEnd('/') + "/"
            shadowPath.startsWith(productionPath)
        } catch (_: Exception) {
            false
        }
    }

    private fun isSensitiveFile(file: File): Boolean {
        val name = file.name.lowercase()
        if (name.startsWith(".")) return true
        return name in setOf(
            ".env", "credentials", "secrets", "keystore", "tokens",
            "google-services.json", "service-account.json", "apikeys.xml"
        )
    }

    private fun copyFile(source: File, target: File): Boolean {
        return try {
            FileInputStream(source).use { input ->
                FileOutputStream(target).use { output ->
                    input.channel.use { srcChannel ->
                        output.channel.use { dstChannel ->
                            val size = srcChannel.size()
                            var position = 0L
                            while (position < size) {
                                position += srcChannel.transferTo(
                                    position, size - position, dstChannel
                                )
                            }
                        }
                    }
                }
            }
            true
        } catch (_: Exception) {
            false
        }
    }

    private fun computeFileHash(file: File): String? {
        return try {
            val digest = MessageDigest.getInstance("SHA-256")
            FileInputStream(file).use { input ->
                val buffer = ByteArray(8192)
                var bytesRead: Int
                while (input.read(buffer).also { bytesRead = it } != -1) {
                    digest.update(buffer, 0, bytesRead)
                }
            }
            digest.digest().joinToString("") { "%02x".format(Locale.US, it) }
        } catch (_: Exception) {
            null
        }
    }

    private fun sanitizeExceptionMessage(e: Exception): String {
        return e.message?.take(128)
            ?.replace("\n", " ")
            ?.replace("\r", "")
            ?: "unknown_error"
    }

    companion object {
        val DISABLED = CanonicalRealDataShadowCopy(
            configuration = CanonicalRealDataShadowCopyConfiguration.DISABLED,
            policy = CanonicalRealDataShadowCopyPolicy.DISABLED
        )
    }
}
