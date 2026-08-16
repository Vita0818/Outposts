package com.intatis.shared.provider

import kotlinx.coroutines.delay
import java.io.IOException
import java.net.SocketTimeoutException
import java.time.Instant
import java.time.format.DateTimeFormatter
import java.time.temporal.ChronoField
import java.util.Locale
import java.util.concurrent.TimeoutException
import kotlin.math.max
import kotlin.math.min
import kotlinx.serialization.json.Json

private const val DEFAULT_MAX_ATTEMPTS = 2
private const val DEFAULT_REQUEST_TIMEOUT_SECONDS = 180.0
private const val DEFAULT_INITIAL_RETRY_DELAY_SECONDS = 0.25
private const val DEFAULT_MAX_RETRY_DELAY_SECONDS = 2.0
private const val DEFAULT_MAX_RETRY_AFTER_DELAY_SECONDS = 30.0

private val json = Json { ignoreUnknownKeys = true }

class ProviderRuntimePolicy(
    val maxAttempts: Int = DEFAULT_MAX_ATTEMPTS,
    val requestTimeoutSeconds: Double = DEFAULT_REQUEST_TIMEOUT_SECONDS,
    val initialRetryDelaySeconds: Double = DEFAULT_INITIAL_RETRY_DELAY_SECONDS,
    val maxRetryDelaySeconds: Double = DEFAULT_MAX_RETRY_DELAY_SECONDS,
    val maxRetryAfterDelaySeconds: Double = DEFAULT_MAX_RETRY_AFTER_DELAY_SECONDS,
) {
    init {
        require(maxAttempts >= 1)
    }

    val requestTimeoutMillis: Long = ((max(0.001, requestTimeoutSeconds) * 1000.0)).toLong().coerceAtLeast(1)

    val initialRetryDelay: Double = max(0.0, initialRetryDelaySeconds)
    val maxRetryDelay: Double = max(initialRetryDelay, maxRetryDelaySeconds)
    val maxRetryAfterDelay: Double = max(0.0, maxRetryAfterDelaySeconds)

    companion object {
        val Streaming = ProviderRuntimePolicy(
            maxAttempts = 2,
            requestTimeoutSeconds = 120.0,
            initialRetryDelaySeconds = 0.25,
            maxRetryDelaySeconds = 2.0,
            maxRetryAfterDelaySeconds = 30.0,
        )

        val NonStreaming = ProviderRuntimePolicy(
            maxAttempts = 2,
            requestTimeoutSeconds = 180.0,
            initialRetryDelaySeconds = 0.25,
            maxRetryDelaySeconds = 2.0,
            maxRetryAfterDelaySeconds = 30.0,
        )
    }
}

data class ProviderRetryHint(val delaySeconds: Double, val source: String, val rawValue: String) {
    val display: String get() = ProviderErrorFormatting.formatSeconds(delaySeconds)
}

class ProviderHttpException(
    val statusCode: Int,
    override val message: String,
    val headers: Map<String, String>,
    cause: Throwable? = null,
) : IOException(message, cause)

object ProviderErrorFormatting {
    const val maxBodyChars = 8192

    fun httpStatus(status: Int, body: String, headers: Map<String, String>, operation: String): Exception {
        val parts = mutableListOf<String>()
        parts.add("$operation failed with HTTP $status${statusLabel(status)}.")

        statusGuidance(status)?.let { parts.add(it) }
        retryHint(headers)?.let { parts.add("Provider asked to retry after about ${it.display} via ${it.source}.") }

        val parsedOrPreview = structuredMessage(body) ?: responsePreview(body)
        if (!parsedOrPreview.isNullOrBlank()) {
            parts.add("Preview: $parsedOrPreview")
        }

        return ProviderHttpException(
            status,
            parts.joinToString(" "),
            headers,
        )
    }

    fun isRetryableHTTPStatus(status: Int): Boolean =
        status == 408 || status == 409 || status == 425 || status == 429 || status in 500..599

    fun statusCodeFromMessage(message: String): Int? {
        val match = Regex("HTTP\\s+(\\d{3})", RegexOption.IGNORE_CASE).find(message)
        return match?.groupValues?.getOrNull(1)?.toIntOrNull()
    }

    fun retryHint(headers: Map<String, String>, now: Instant = Instant.now()): ProviderRetryHint? {
        val normalized = headers.entries.associate { it.key.lowercase(Locale.US) to it.value }
        val candidates = listOf(
            "retry-after",
            "x-ratelimit-reset",
            "x-ratelimit-reset-requests",
            "x-ratelimit-reset-tokens",
            "ratelimit-reset",
        )

        for (key in candidates) {
            val raw = normalized[key]?.trim()?.takeIf { it.isNotEmpty() } ?: continue
            val seconds = retryDelaySeconds(raw, now) ?: continue
            return ProviderRetryHint(max(0.0, seconds), key, raw)
        }

        return null
    }

    fun retryHintFrom(error: Throwable): ProviderRetryHint? =
        retryHintFromMessage(error.message.orEmpty())

    fun transport(error: Throwable): Exception {
        return when (error) {
            is TimeoutException -> error
            is SocketTimeoutException -> TimeoutException(error.message ?: "provider request timed out")
            is IOException -> IOException("Provider transport failed. ${clean(error.message)}", error)
            is ProviderHttpException -> error
            is kotlin.coroutines.cancellation.CancellationException -> error
            else -> IOException("Provider request failed. ${clean(error.message)}", error)
        }
    }

    fun formatSeconds(value: Double): String =
        if (value >= 1.0) {
            String.format(Locale.US, "%.0fs", value)
        } else {
            String.format(Locale.US, "%.2fs", value)
        }

    internal fun retryDelaySeconds(raw: String, now: Instant = Instant.now()): Double? {
        val trimmed = raw.trim()
        if (trimmed.isEmpty()) return null

        trimmed.toDoubleOrNull()?.let { seconds ->
            return if (seconds > 10_000_000) {
                max(0.0, seconds - now.epochSecond)
            } else {
                max(0.0, seconds)
            }
        }

        durationDelaySeconds(trimmed)?.let { return it }

        val parsed = runCatching {
            DateTimeFormatter.RFC_1123_DATE_TIME.parse(trimmed).getLong(ChronoField.INSTANT_SECONDS).toDouble()
        }.getOrNull()

        if (parsed != null) {
            return max(0.0, parsed - now.epochSecond)
        }

        return null
    }

    private fun retryHintFromMessage(message: String): ProviderRetryHint? {
        val match = Regex("retry after about ([0-9]+(?:\\.[0-9]+)?)s", RegexOption.IGNORE_CASE).find(message)
            ?: return null

        val seconds = match.groupValues.getOrNull(1)?.toDoubleOrNull() ?: return null
        return ProviderRetryHint(seconds, "message", match.value)
    }

    private fun statusLabel(status: Int): String = when (status) {
        400 -> " Bad Request"
        401 -> " Unauthorized"
        403 -> " Forbidden"
        404 -> " Not Found"
        408 -> " Request Timeout"
        409 -> " Conflict"
        422 -> " Unprocessable Entity"
        429 -> " Too Many Requests"
        500 -> " Internal Server Error"
        502 -> " Bad Gateway"
        503 -> " Service Unavailable"
        504 -> " Gateway Timeout"
        else -> ""
    }

    private fun statusGuidance(status: Int): String? = when (status) {
        400, 422 -> "Check model id, request shape, tool schema, and endpoint compatibility."
        401 -> "Check the API key source and provider authentication."
        403 -> "The key or account is not allowed to use this model or endpoint."
        404 -> "Check the base URL, chat endpoint, provider path, and model id."
        408, 429 -> "Retry later or reduce request rate/context size."
        in 500..599 -> "The provider or upstream gateway failed; retry later or switch provider."
        else -> null
    }

    private fun responsePreview(body: String): String? {
        if (body.isBlank()) return null
        val cleaned = clean(body)
        if (cleaned.isBlank()) return null
        return if (cleaned.length <= maxBodyChars) cleaned else cleaned.take(maxBodyChars) + "..."
    }

    private fun structuredMessage(body: String): String? {
        val root = runCatching { json.parseToJsonElement(body) }.getOrNull() ?: return null
        val obj = root as? kotlinx.serialization.json.JsonObject ?: return null

        val error = obj["error"]
        if (error is kotlinx.serialization.json.JsonObject) {
            val candidates = listOf("message", "type", "code", "param")
            val parts = mutableListOf<String>()
            for (candidate in candidates) {
                val value = error[candidate]?.toString()?.trim('"')
                if (!value.isNullOrBlank()) parts.add(value)
            }
            if (parts.isNotEmpty()) return parts.joinToString(" ")
        }

        for (candidate in listOf("message", "detail", "error_description")) {
            val value = obj[candidate]?.toString()?.trim('"')
            if (!value.isNullOrBlank()) return value
        }

        return null
    }

    private fun durationDelaySeconds(raw: String): Double? {
        val normalized = raw.replace("\\s+".toRegex(), "").lowercase(Locale.US)
        if (normalized.isEmpty()) return null

        val regex = "([0-9]+(?:\\.[0-9]+)?)(ms|s|m|h)".toRegex()
        val matches = regex.findAll(normalized).toList()
        if (matches.isEmpty()) return null

        var position = 0
        var total = 0.0
        for (match in matches) {
            if (match.range.first != position) return null

            val value = match.groupValues.getOrNull(1)?.toDoubleOrNull() ?: return null
            val unit = match.groupValues.getOrNull(2)
            total += when (unit) {
                "ms" -> value / 1000.0
                "s" -> value
                "m" -> value * 60
                "h" -> value * 3600
                else -> 0.0
            }
            position = match.range.last + 1
        }

        return if (position == normalized.length) total else null
    }

    private fun clean(value: String?): String {
        if (value.isNullOrBlank()) return "provider request failed."
        return value.replace("\\s+".toRegex(), " ").trim().take(360)
    }
}

object ProviderRuntime {
    fun shouldRetry(error: Throwable, attempt: Int, policy: ProviderRuntimePolicy, receivedResponseBytes: Boolean = false): Boolean {
        if (receivedResponseBytes) return false
        if (attempt >= policy.maxAttempts) return false
        return isRetryable(error)
    }

    fun retryDelay(nextAttempt: Int, policy: ProviderRuntimePolicy, retryHint: ProviderRetryHint? = null): Long {
        val delaySeconds = if (retryHint != null) {
            min(policy.maxRetryAfterDelay, max(0.0, retryHint.delaySeconds))
        } else {
            val exponent = max(0, nextAttempt - 2)
            min(policy.maxRetryDelay, policy.initialRetryDelay * Math.pow(2.0, exponent.toDouble()))
        }

        return (delaySeconds * 1000.0).toLong()
    }

    suspend fun sleepBeforeRetry(nextAttempt: Int, policy: ProviderRuntimePolicy, retryHint: ProviderRetryHint? = null) {
        val delayMs = retryDelay(nextAttempt, policy, retryHint)
        if (delayMs > 0L) {
            delay(delayMs)
        }
    }

    fun exhausted(error: Throwable, attempts: Int, operation: String): Exception {
        val normalized = ProviderErrorFormatting.transport(error)
        if (attempts <= 1) return normalized

        val suffix = " Retried ${attempts - 1} time${if (attempts == 2) "" else "s"}; still failed."

        return when (normalized) {
            is ProviderHttpException -> ProviderHttpException(
                normalized.statusCode,
                "${normalized.message}$suffix",
                normalized.headers,
                normalized,
            )

            is TimeoutException -> TimeoutException("${normalized.message}$suffix", normalized)

            is kotlin.coroutines.cancellation.CancellationException -> normalized

            else -> Exception("$operation failed. ${normalized.message}$suffix", normalized)
        }
    }

    fun retryHintFromError(error: Throwable): ProviderRetryHint? {
        return when (error) {
            is ProviderHttpException -> ProviderErrorFormatting.retryHint(error.headers)
            else -> ProviderErrorFormatting.retryHintFrom(error)
        }
    }

    private fun isRetryable(error: Throwable): Boolean {
        if (error is kotlin.coroutines.cancellation.CancellationException) return false

        if (error is ProviderHttpException && ProviderErrorFormatting.isRetryableHTTPStatus(error.statusCode)) {
            return true
        }

        if (error is TimeoutException || error is SocketTimeoutException) return true
        if (error is IOException) return true

        if (error is Exception) {
            return isRetryableMessage(error.message.orEmpty())
        }

        return false
    }

    private fun isRetryableMessage(message: String): Boolean {
        val lower = message.lowercase(Locale.US)
        return lower.contains("timed out")
            || lower.contains("network connection")
            || lower.contains("connection to the provider was lost")
            || lower.contains("could not connect")
            || lower.contains("could not resolve")
    }
}
