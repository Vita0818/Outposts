package com.rokurics.app.domain.canonical

import java.io.File
import java.util.Date

// ── CanonicalExecutionShadowPreparationMode ──

enum class CanonicalExecutionShadowPreparationMode(val rawValue: String) {
    DISABLED("disabled"),
    SHADOW_ONLY("shadowOnly"),
    PREPARE_SHADOW_WITH_FILE_STORE("prepareShadowWithFileStore"),
    PREPARE_SHADOW_WITH_TRANSPORT_PROBE("prepareShadowWithTransportProbe");

    companion object {
        val allCases: List<CanonicalExecutionShadowPreparationMode> = entries.toList()
    }
}

// ── CanonicalExecutionShadowPreparationResult ──

data class CanonicalExecutionShadowPreparationResult(
    val prepared: Boolean,
    val shadowRoot: String?,
    val fileStoreReady: Boolean,
    val transportProbeReady: Boolean,
    val shadowPlan: List<String>,
    val blocker: String?
) {
    val isReady: Boolean
        get() = prepared && blocker == null

    val summary: String
        get() = listOf(
            "prepared=$prepared",
            "shadowRoot=${shadowRoot ?: "none"}",
            "fileStoreReady=$fileStoreReady",
            "transportProbeReady=$transportProbeReady",
            "blocker=${blocker ?: "none"}"
        ).joinToString(",")

    companion object {
        fun disabled(): CanonicalExecutionShadowPreparationResult {
            return CanonicalExecutionShadowPreparationResult(
                prepared = false,
                shadowRoot = null,
                fileStoreReady = false,
                transportProbeReady = false,
                shadowPlan = emptyList(),
                blocker = "DISABLED"
            )
        }

        fun ready(
            shadowRoot: String,
            fileStoreReady: Boolean = false,
            transportProbeReady: Boolean = false,
            shadowPlan: List<String> = emptyList()
        ): CanonicalExecutionShadowPreparationResult {
            return CanonicalExecutionShadowPreparationResult(
                prepared = true,
                shadowRoot = shadowRoot.trim().nilIfEmpty,
                fileStoreReady = fileStoreReady,
                transportProbeReady = transportProbeReady,
                shadowPlan = shadowPlan,
                blocker = null
            )
        }

        fun blocked(
            reason: String,
            shadowRoot: String? = null,
            shadowPlan: List<String> = emptyList()
        ): CanonicalExecutionShadowPreparationResult {
            return CanonicalExecutionShadowPreparationResult(
                prepared = false,
                shadowRoot = shadowRoot?.trim()?.nilIfEmpty,
                fileStoreReady = false,
                transportProbeReady = false,
                shadowPlan = shadowPlan,
                blocker = reason.trim().nilIfEmpty ?: "unknown_blocker"
            )
        }
    }
}

// ── CanonicalExecutionShadowPreparation ──

class CanonicalExecutionShadowPreparation(
    private val mode: CanonicalExecutionShadowPreparationMode = CanonicalExecutionShadowPreparationMode.DISABLED,
    private val maxShadowRootBytes: Long = 512L * 1024L * 1024L
) {

    fun prepare(
        shadowRootCandidate: String? = null,
        transportProbeBaseURL: String? = null
    ): CanonicalExecutionShadowPreparationResult {
        if (mode == CanonicalExecutionShadowPreparationMode.DISABLED) {
            return CanonicalExecutionShadowPreparationResult.disabled()
        }

        val resolvedShadowRoot = resolveShadowRoot(shadowRootCandidate)
            ?: return CanonicalExecutionShadowPreparationResult.blocked(
                reason = "shadow_root_unresolvable",
                shadowPlan = buildPlan(mode, null, transportProbeBaseURL)
            )

        if (!ensureShadowDirectory(resolvedShadowRoot)) {
            return CanonicalExecutionShadowPreparationResult.blocked(
                reason = "shadow_directory_creation_failed",
                shadowRoot = resolvedShadowRoot,
                shadowPlan = buildPlan(mode, resolvedShadowRoot, transportProbeBaseURL)
            )
        }

        val fileStoreReady = when (mode) {
            CanonicalExecutionShadowPreparationMode.PREPARE_SHADOW_WITH_FILE_STORE ->
                prepareFileStore(resolvedShadowRoot)
            else -> false
        }

        val transportProbeReady = when (mode) {
            CanonicalExecutionShadowPreparationMode.PREPARE_SHADOW_WITH_TRANSPORT_PROBE ->
                validateTransportProbeBase(transportProbeBaseURL)
            else -> false
        }

        if (mode == CanonicalExecutionShadowPreparationMode.PREPARE_SHADOW_WITH_FILE_STORE && !fileStoreReady) {
            return CanonicalExecutionShadowPreparationResult.blocked(
                reason = "file_store_preparation_failed",
                shadowRoot = resolvedShadowRoot,
                shadowPlan = buildPlan(mode, resolvedShadowRoot, transportProbeBaseURL)
            )
        }

        if (mode == CanonicalExecutionShadowPreparationMode.PREPARE_SHADOW_WITH_TRANSPORT_PROBE && !transportProbeReady) {
            return CanonicalExecutionShadowPreparationResult.blocked(
                reason = "transport_probe_validation_failed",
                shadowRoot = resolvedShadowRoot,
                shadowPlan = buildPlan(mode, resolvedShadowRoot, transportProbeBaseURL)
            )
        }

        val plan = buildPlan(mode, resolvedShadowRoot, transportProbeBaseURL)

        return CanonicalExecutionShadowPreparationResult.ready(
            shadowRoot = resolvedShadowRoot,
            fileStoreReady = fileStoreReady,
            transportProbeReady = transportProbeReady,
            shadowPlan = plan
        )
    }

    private fun resolveShadowRoot(candidate: String?): String? {
        val trimmed = candidate?.trim()?.nilIfEmpty ?: return null
        val resolved = if (trimmed.startsWith("/")) {
            trimmed
        } else {
            val cwd = System.getProperty("user.dir") ?: return null
            File(cwd, trimmed).absolutePath
        }
        return if (resolved.length > 4096) null else resolved
    }

    private fun ensureShadowDirectory(shadowRoot: String): Boolean {
        return try {
            val dir = File(shadowRoot)
            if (dir.exists()) {
                dir.isDirectory && dir.canWrite()
            } else {
                dir.mkdirs()
            }
        } catch (_: Exception) {
            false
        }
    }

    private fun prepareFileStore(shadowRoot: String): Boolean {
        return try {
            val subDirs = listOf("artifacts", "metadata", "logs")
            subDirs.all { subDir ->
                val dir = File(shadowRoot, subDir)
                (dir.exists() && dir.isDirectory) || dir.mkdirs()
            }
        } catch (_: Exception) {
            false
        }
    }

    private fun validateTransportProbeBase(url: String?): Boolean {
        val trimmed = url?.trim()?.nilIfEmpty ?: return false
        return trimmed.startsWith("http://") ||
            trimmed.startsWith("https://") ||
            trimmed.startsWith("tcp://")
    }

    private fun buildPlan(
        mode: CanonicalExecutionShadowPreparationMode,
        shadowRoot: String?,
        transportProbeBaseURL: String?
    ): List<String> {
        val plan = mutableListOf<String>()
        plan.add("mode=${mode.rawValue}")
        if (shadowRoot != null) {
            plan.add("shadowRoot=$shadowRoot")
            plan.add("shadowRootDiskUsage=${estimateDiskUsage(shadowRoot)}")
            plan.add("shadowRootPrepared=${Date()}")
        }
        when (mode) {
            CanonicalExecutionShadowPreparationMode.PREPARE_SHADOW_WITH_FILE_STORE -> {
                plan.add("fileStore=prepared")
                plan.add("subDirs=artifacts,metadata,logs")
            }
            CanonicalExecutionShadowPreparationMode.PREPARE_SHADOW_WITH_TRANSPORT_PROBE -> {
                plan.add("transportProbeBase=${transportProbeBaseURL ?: "none"}")
                plan.add("transportProbe=prepared")
            }
            else -> {}
        }
        plan.add("maxShadowRootBytes=$maxShadowRootBytes")
        return plan.toList()
    }

    private fun estimateDiskUsage(shadowRoot: String): Long {
        return try {
            File(shadowRoot).walkTopDown()
                .filter { it.isFile }
                .sumOf { it.length() }
        } catch (_: Exception) {
            0L
        }
    }

    companion object {
        val DISABLED = CanonicalExecutionShadowPreparation(
            mode = CanonicalExecutionShadowPreparationMode.DISABLED
        )
    }
}
