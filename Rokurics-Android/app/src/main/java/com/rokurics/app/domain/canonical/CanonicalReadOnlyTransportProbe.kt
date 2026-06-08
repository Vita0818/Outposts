package com.rokurics.app.domain.canonical

import java.net.HttpURLConnection
import java.net.URI
import java.security.MessageDigest
import java.util.Date
import java.util.Locale

// ── CanonicalReadOnlyTransportProbePolicy ──

data class CanonicalReadOnlyTransportProbePolicy(
    val enabled: Boolean = false,
    val allowSignedShadowRequest: Boolean = false,
    val maxProbeCount: Int = 1000
) {
    companion object {
        val DISABLED = CanonicalReadOnlyTransportProbePolicy()
    }
}

// ── CanonicalReadOnlyTransportProbeConfigurationMode ──

enum class CanonicalReadOnlyTransportProbeConfigurationMode(val rawValue: String) {
    DISABLED("disabled"),
    PROBE_ONLY("probeOnly"),
    PROBE_WITH_SIGNED_REQUEST("probeWithSignedRequest");

    companion object {
        val allCases: List<CanonicalReadOnlyTransportProbeConfigurationMode> = entries.toList()
    }
}

// ── CanonicalReadOnlyTransportProbeConfiguration ──

data class CanonicalReadOnlyTransportProbeConfiguration(
    val mode: CanonicalReadOnlyTransportProbeConfigurationMode = CanonicalReadOnlyTransportProbeConfigurationMode.DISABLED,
    val baseURL: String? = null,
    val timeoutMilliseconds: Long = 30_000L,
    val maxProbeCount: Int = 1000,
    val allowedMethods: List<String> = listOf("GET", "HEAD")
) {
    companion object {
        val DISABLED = CanonicalReadOnlyTransportProbeConfiguration()
    }
}

// ── CanonicalReadOnlyTransportProbeResult ──

data class CanonicalReadOnlyTransportProbeResult(
    val route: String,
    val status: Int,
    val responseHash: String?,
    val blocked: Boolean
) {
    val id: String get() = route

    val isSuccess: Boolean
        get() = !blocked && status in 200..399

    val summary: String
        get() = listOf(
            "route=$route",
            "status=$status",
            "blocked=$blocked",
            "responseHash=${responseHash?.take(8) ?: "none"}"
        ).joinToString(",")

    companion object {
        fun blocked(route: String, reason: String): CanonicalReadOnlyTransportProbeResult {
            return CanonicalReadOnlyTransportProbeResult(
                route = route,
                status = -1,
                responseHash = null,
                blocked = true
            )
        }

        fun success(route: String, status: Int, responseHash: String?): CanonicalReadOnlyTransportProbeResult {
            return CanonicalReadOnlyTransportProbeResult(
                route = route,
                status = status,
                responseHash = responseHash,
                blocked = false
            )
        }

        fun error(route: String, status: Int): CanonicalReadOnlyTransportProbeResult {
            return CanonicalReadOnlyTransportProbeResult(
                route = route,
                status = status,
                responseHash = null,
                blocked = false
            )
        }
    }
}

// ── CanonicalReadOnlyTransportProbe ──

class CanonicalReadOnlyTransportProbe(
    private val configuration: CanonicalReadOnlyTransportProbeConfiguration = CanonicalReadOnlyTransportProbeConfiguration.DISABLED,
    private val policy: CanonicalReadOnlyTransportProbePolicy = CanonicalReadOnlyTransportProbePolicy.DISABLED
) {

    fun probe(routes: List<String>): List<CanonicalReadOnlyTransportProbeResult> {
        if (!policy.enabled || configuration.mode == CanonicalReadOnlyTransportProbeConfigurationMode.DISABLED) {
            return routes.map { route ->
                CanonicalReadOnlyTransportProbeResult.blocked(route, "probe_disabled")
            }
        }

        if (!isProbeableTransport(configuration.baseURL)) {
            return routes.map { route ->
                CanonicalReadOnlyTransportProbeResult.blocked(route, "invalid_base_url")
            }
        }

        val normalizedRoutes = routes
            .mapNotNull { normalizeRoute(it) }
            .take(configuration.maxProbeCount.coerceAtMost(policy.maxProbeCount))

        val results = mutableListOf<CanonicalReadOnlyTransportProbeResult>()

        for (route in normalizedRoutes) {
            if (!isAllowedMethod("GET")) {
                results.add(
                    CanonicalReadOnlyTransportProbeResult.blocked(route, "method_not_allowed")
                )
                continue
            }

            when (configuration.mode) {
                CanonicalReadOnlyTransportProbeConfigurationMode.PROBE_ONLY -> {
                    results.add(executeProbe(route, useSignedRequest = false))
                }
                CanonicalReadOnlyTransportProbeConfigurationMode.PROBE_WITH_SIGNED_REQUEST -> {
                    if (!policy.allowSignedShadowRequest) {
                        results.add(
                            CanonicalReadOnlyTransportProbeResult.blocked(
                                route, "signed_request_not_allowed_by_policy"
                            )
                        )
                    } else {
                        results.add(executeProbe(route, useSignedRequest = true))
                    }
                }
                else -> {
                    results.add(
                        CanonicalReadOnlyTransportProbeResult.blocked(route, "unknown_mode")
                    )
                }
            }
        }

        return results
    }

    fun probeSingle(
        route: String,
        method: String = "GET"
    ): CanonicalReadOnlyTransportProbeResult {
        return probe(listOf(route)).firstOrNull()
            ?: CanonicalReadOnlyTransportProbeResult.blocked(route, "probe_failed")
    }

    private fun executeProbe(
        route: String,
        useSignedRequest: Boolean
    ): CanonicalReadOnlyTransportProbeResult {
        return try {
            val baseUrl = configuration.baseURL ?: return CanonicalReadOnlyTransportProbeResult.blocked(
                route, "missing_base_url"
            )
            val fullURL = buildURL(baseUrl, route)
                ?: return CanonicalReadOnlyTransportProbeResult.blocked(route, "url_construction_failed")

            if (!fullURL.startsWith("https://") && !fullURL.startsWith("http://")) {
                return CanonicalReadOnlyTransportProbeResult.blocked(route, "non_http_url_blocked")
            }

            val uri = URI(fullURL)
            val connection = (uri.toURL().openConnection() as HttpURLConnection).apply {
                requestMethod = "GET"
                connectTimeout = configuration.timeoutMilliseconds.toInt()
                readTimeout = configuration.timeoutMilliseconds.toInt()
                setRequestProperty("User-Agent", "CanonicalReadOnlyTransportProbe/1.0")
                instanceFollowRedirects = false
                doOutput = false
                doInput = true
            }

            if (useSignedRequest && policy.allowSignedShadowRequest) {
                connection.setRequestProperty(
                    "X-Canonical-Shadow-Signature",
                    computeShadowSignature(fullURL)
                )
            }

            connection.connect()
            val status = connection.responseCode

            if (status in 200..399) {
                val responseBody = try {
                    connection.inputStream?.bufferedReader()?.use { it.readText() }
                } catch (_: Exception) {
                    null
                }
                val responseHash = responseBody?.let { computeSHA256(it) }
                connection.disconnect()
                CanonicalReadOnlyTransportProbeResult.success(route, status, responseHash)
            } else {
                connection.disconnect()
                CanonicalReadOnlyTransportProbeResult.error(route, status)
            }
        } catch (e: Exception) {
            CanonicalReadOnlyTransportProbeResult.blocked(
                route, "connection_error:${sanitizeExceptionMessage(e)}"
            )
        }
    }

    private fun isProbeableTransport(baseURL: String?): Boolean {
        val trimmed = baseURL?.trim()?.nilIfEmpty ?: return false
        return trimmed.startsWith("http://") || trimmed.startsWith("https://")
    }

    private fun isAllowedMethod(method: String): Boolean {
        return configuration.allowedMethods.contains(method.uppercase())
    }

    private fun normalizeRoute(route: String): String? {
        val trimmed = route.trim().nilIfEmpty ?: return null
        if (trimmed.contains("://") || trimmed.contains("..")) return null
        return if (trimmed.startsWith("/")) trimmed else "/$trimmed"
    }

    private fun buildURL(baseURL: String, route: String): String? {
        val normalizedBase = baseURL.trimEnd('/')
        return try {
            URI(normalizedBase).resolve(route).toString()
        } catch (_: Exception) {
            null
        }
    }

    private fun computeSHA256(input: String): String {
        val digest = MessageDigest.getInstance("SHA-256")
        val hashBytes = digest.digest(input.toByteArray(Charsets.UTF_8))
        return hashBytes.joinToString("") { "%02x".format(Locale.US, it) }
    }

    private fun computeShadowSignature(url: String): String {
        val payload = "canonical-shadow-probe:$url:${Date().time}"
        return computeSHA256(payload)
    }

    private fun sanitizeExceptionMessage(e: Exception): String {
        return e.message?.take(256)
            ?.replace("\n", " ")
            ?.replace("\r", "")
            ?: "unknown_error"
    }

    companion object {
        val DISABLED = CanonicalReadOnlyTransportProbe(
            configuration = CanonicalReadOnlyTransportProbeConfiguration.DISABLED,
            policy = CanonicalReadOnlyTransportProbePolicy.DISABLED
        )
    }
}
