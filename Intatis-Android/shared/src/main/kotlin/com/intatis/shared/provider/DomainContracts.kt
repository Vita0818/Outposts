package com.intatis.shared.provider

import com.intatis.shared.ImageAttachment
import com.intatis.shared.IntatisConfig
import com.intatis.shared.IntatisMessage
import com.intatis.shared.OpenAIClient as LegacyOpenAIClient
import com.intatis.shared.ToolDescriptor
import java.time.Duration
import java.util.Locale

data class ProviderDescriptor(
    val id: String,
    val baseUrl: String,
    val apiKey: String,
)

data class ProviderHealthCheckResult(
    val providerId: String,
    val role: String,
    val model: String,
    val isHealthy: Boolean,
    val latency: Duration,
    val message: String,
    val responsePreview: String? = null,
)

data class ProviderHealthCheckSuite(
    val chat: ProviderHealthCheckResult,
    val agentTool: ProviderHealthCheckResult,
)

data class ChatResult(
    val text: String,
    val latency: Duration,
    val usage: String?,
)

data class ToolCallResult(
    val text: String,
    val toolCalls: List<LegacyOpenAIClient.ToolCall>,
    val latency: Duration,
    val usage: String?,
)

data class OpenAIModelBinding(
    val providerId: String,
    val model: String,
)

interface ChatProvider {
    suspend fun sendAsync(
        messages: List<IntatisMessage>,
        model: String? = null,
        reasoning: String? = null,
        attachments: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
    ): ChatResult
}

interface AgentToolProvider {
    suspend fun sendWithToolsAsync(
        messages: List<LegacyOpenAIClient.OpenAIChatMessage>,
        tools: List<ToolDescriptor>,
        model: String? = null,
        reasoning: String? = null,
        includeUsage: Boolean = false,
    ): ToolCallResult
}

data class ImageRequest(
    val model: String,
    val prompt: String,
    val size: String = "1024x1024",
    val count: Int = 1,
)

data class GeneratedImage(
    val data: ByteArray,
    val mime: String,
)

interface ImageGenerationProvider {
    suspend fun generate(request: ImageRequest): List<GeneratedImage>
}

interface VideoGenerationProvider {
    suspend fun submit(request: VideoRequest): String
    suspend fun poll(jobId: String): VideoJobStatus
}

interface TranscriptionProvider {
    suspend fun transcribe(request: TranscriptionRequest): String
}

data class TranscriptionRequest(
    val model: String,
    val audio: ByteArray,
    val filename: String = "audio.m4a",
    val mime: String = "audio/m4a",
)

data class VideoRequest(
    val model: String,
    val prompt: String,
    val seconds: Int = 4,
)

enum class VideoJobState {
    queued,
    running,
    completed,
    failed,
}

data class VideoJobStatus(
    val state: VideoJobState,
    val progress: Double,
    val resultData: ByteArray? = null,
    val mime: String = "video/mp4",
)

data class TranscriptionResult(
    val text: String,
)

sealed class ProviderLookupError(message: String) : RuntimeException(message)
class MissingProviderError(providerId: String) : ProviderLookupError("provider '$providerId' is not configured")
class ProviderRequestError(message: String) : ProviderLookupError(message)

class ProviderRegistry(private val config: IntatisConfig) {
    companion object {
        const val CHAT_PROVIDER = "chat"
        const val AGENT_TOOL_PROVIDER = "agentTool"
        const val IMAGE_PROVIDER = "image"
        const val TRANSCRIPTION_PROVIDER = "transcription"
        private const val DefaultProviderId = "openai"
        private val OpenAIProviderAliases = setOf(
            "openai",
            "gpt",
            "default",
            "default_openai",
            "",
        )
    }

    private val normalizedConfig = config.copy(
        chatProviderId = config.chatProviderId.ifBlank { DefaultProviderId },
        agentToolProviderId = config.agentToolProviderId.ifBlank { DefaultProviderId },
        imageProviderId = config.imageProviderId.ifBlank { DefaultProviderId },
        transcriptionProviderId = config.transcriptionProviderId.ifBlank { DefaultProviderId },
    )

    private val availableDescriptors = run {
        val imageProviderId = normalizeId(normalizedConfig.imageProviderId)
        val transcriptionProviderId = normalizeId(normalizedConfig.transcriptionProviderId)
        linkedMapOf<String, ProviderDescriptor>(
            imageProviderId to ProviderDescriptor(imageProviderId, normalizedConfig.baseUrl, normalizedConfig.apiKey),
        ).apply {
            put(
                transcriptionProviderId,
                ProviderDescriptor(
                    transcriptionProviderId,
                    normalizedConfig.baseUrl,
                    normalizedConfig.apiKey,
                ),
            )
        }
    }

    private val openAIClient = LegacyOpenAIClient(normalizedConfig)

    suspend fun checkHealth(
        chatProviderId: String? = null,
        agentToolProviderId: String? = null,
    ): ProviderHealthCheckSuite {
        val resolvedModel = normalizedConfig.model
        val resolvedChatProviderId = normalizeId(chatProviderId ?: normalizedConfig.chatProviderId)
        val resolvedAgentToolProviderId = normalizeId(agentToolProviderId ?: normalizedConfig.agentToolProviderId)

        val chatHealth = checkChatHealth(resolvedChatProviderId, resolvedModel, normalizedConfig.reasoning)
        val agentHealth = checkAgentToolHealth(resolvedAgentToolProviderId, resolvedModel, normalizedConfig.reasoning)

        return ProviderHealthCheckSuite(chat = chatHealth, agentTool = agentHealth)
    }

    fun availableProviderIdsFor(_role: String): List<String> = availableDescriptors.keys.toList()

    fun defaultChatProviderId(): String = normalizedConfig.chatProviderId

    fun defaultAgentToolProviderId(): String = normalizedConfig.agentToolProviderId

    fun defaultImageProviderId(): String = normalizedConfig.imageProviderId

    fun defaultTranscriptionProviderId(): String = normalizedConfig.transcriptionProviderId

    fun defaultImageProvider(): ImageGenerationProvider? = try {
        imageProvider()
    } catch (_: MissingProviderError) {
        null
    }

    fun defaultTranscriptionProvider(): TranscriptionProvider? = try {
        transcriptionProvider()
    } catch (_: MissingProviderError) {
        null
    }

    fun chatProvider(id: String? = null): ChatProvider {
        val providerId = normalizeId(id ?: normalizedConfig.chatProviderId)
        return when (providerId) {
            in OpenAIProviderAliases -> OpenAIProvider(openAIClient)
            else -> resolveFromKnown(id = providerId)
        }
    }

    fun agentToolProvider(id: String? = null): AgentToolProvider {
        val providerId = normalizeId(id ?: normalizedConfig.agentToolProviderId)
        return when (providerId) {
            in OpenAIProviderAliases -> OpenAIProvider(openAIClient)
            else -> resolveFromKnown(id = providerId)
        }
    }

    fun imageProvider(id: String? = null): ImageGenerationProvider {
        val providerId = normalizeId(id ?: normalizedConfig.imageProviderId)
        return when (providerId) {
            in OpenAIProviderAliases -> OpenAIImageProvider(openAIClient)
            else -> resolveImageFromKnown(providerId)
        }
    }

    fun transcriptionProvider(id: String? = null): TranscriptionProvider {
        val providerId = normalizeId(id ?: normalizedConfig.transcriptionProviderId)
        return when (providerId) {
            in OpenAIProviderAliases -> OpenAITranscriptionProvider(openAIClient)
            else -> resolveTranscriptionFromKnown(providerId)
        }
    }

    private fun normalizeId(raw: String): String = raw.trim().lowercase(Locale.ROOT)

    private suspend fun checkChatHealth(
        providerId: String,
        model: String,
        reasoning: String?,
    ): ProviderHealthCheckResult = when {
        providerId in OpenAIProviderAliases -> runCatching {
            openAIClient.checkChatHealthAsync(model, reasoning)
        }.getOrElse {
            ProviderHealthCheckResult(
                providerId = providerId,
                role = "chat",
                model = model,
                isHealthy = false,
                latency = Duration.ZERO,
                message = "health check failed: ${it.message}",
            )
        }

        else -> ProviderHealthCheckResult(
            providerId = providerId,
            role = "chat",
            model = model,
            isHealthy = false,
            latency = Duration.ZERO,
            message = "health check unsupported for provider '$providerId'",
        )
    }

    private suspend fun checkAgentToolHealth(
        providerId: String,
        model: String,
        reasoning: String?,
    ): ProviderHealthCheckResult = when {
        providerId in OpenAIProviderAliases -> runCatching {
            openAIClient.checkAgentToolHealthAsync(model, reasoning)
        }.getOrElse {
            ProviderHealthCheckResult(
                providerId = providerId,
                role = "agent",
                model = model,
                isHealthy = false,
                latency = Duration.ZERO,
                message = "health check failed: ${it.message}",
            )
        }

        else -> ProviderHealthCheckResult(
            providerId = providerId,
            role = "agent",
            model = model,
            isHealthy = false,
            latency = Duration.ZERO,
            message = "health check unsupported for provider '$providerId'",
        )
    }

    private fun resolveFromKnown(id: String): AgentToolProvider =
        throw MissingProviderError(id)

    private fun resolveImageFromKnown(id: String): ImageGenerationProvider =
        if (availableDescriptors.containsKey(id)) OpenAIImageProvider(openAIClient) else throw MissingProviderError(id)

    private fun resolveTranscriptionFromKnown(id: String): TranscriptionProvider =
        if (availableDescriptors.containsKey(id)) OpenAITranscriptionProvider(openAIClient) else throw MissingProviderError(id)

    fun imageModel(): String = normalizedConfig.model

    private class OpenAIProvider(private val client: LegacyOpenAIClient) :
        ChatProvider,
        AgentToolProvider {

        override suspend fun sendAsync(
            messages: List<IntatisMessage>,
            model: String?,
            reasoning: String?,
            attachments: List<ImageAttachment>,
            includeUsage: Boolean,
        ): ChatResult {
            val response = client.sendAsync(messages, model, reasoning, attachments, includeUsage)
            return ChatResult(
                text = response.first,
                latency = Duration.ofMillis(response.second),
                usage = response.third,
            )
        }

        override suspend fun sendWithToolsAsync(
            messages: List<LegacyOpenAIClient.OpenAIChatMessage>,
            tools: List<ToolDescriptor>,
            model: String?,
            reasoning: String?,
            includeUsage: Boolean,
        ): ToolCallResult {
            val response = client.sendWithToolsAsync(messages, tools, model, reasoning, includeUsage)
            return ToolCallResult(
                text = response.first,
                toolCalls = response.toolCalls,
                latency = Duration.ofMillis(response.latencyMs),
                usage = response.usage,
            )
        }
    }

    private class OpenAIImageProvider(private val client: LegacyOpenAIClient) : ImageGenerationProvider {
        override suspend fun generate(request: ImageRequest): List<GeneratedImage> {
            val normalized = requireImageGenerationRequest(request)
            return client.generateImagesAsync(
                model = normalized.model,
                prompt = normalized.prompt,
                size = normalized.size,
                count = normalized.count,
            )
        }
    }

    private class OpenAITranscriptionProvider(private val client: LegacyOpenAIClient) : TranscriptionProvider {
        override suspend fun transcribe(request: TranscriptionRequest): String {
            val normalized = requireTranscriptionRequest(request)
            return client.transcribeAudioAsync(
                model = normalized.model,
                audio = normalized.audio,
                filename = normalized.filename,
                mimeType = normalized.mime,
            )
        }
    }

    private class UnsupportedTranscriptionProvider : TranscriptionProvider {
        override suspend fun transcribe(request: TranscriptionRequest): String {
            throw UnsupportedOperationException("transcription is not configured")
        }
    }

    private fun requireImageGenerationRequest(request: ImageRequest): ImageRequest {
        val model = request.model.trim()
        if (model.isBlank()) {
            throw IllegalArgumentException("image generation model is required")
        }

        val prompt = request.prompt.trim()
        if (prompt.isBlank()) {
            throw IllegalArgumentException("image generation prompt is required")
        }

        val count = request.count.coerceIn(1, 4)
        if (count != request.count) {
            throw IllegalArgumentException("image generation count must be in range [1, 4]")
        }

        return request.copy(
            model = model,
            prompt = prompt,
            size = request.size.ifBlank { "1024x1024" },
            count = count,
        )
    }

    private fun requireTranscriptionRequest(request: TranscriptionRequest): TranscriptionRequest {
        val model = request.model.trim()
        if (model.isBlank()) {
            throw IllegalArgumentException("transcription model is required")
        }

        val audio = request.audio
        if (audio.isEmpty()) {
            throw IllegalArgumentException("transcription audio is required")
        }

        val filename = request.filename.trim()
            .ifEmpty { "audio.m4a" }
        if (filename.isBlank()) {
            throw IllegalArgumentException("transcription filename is required")
        }

        val mimeType = request.mime.trim()
        if (mimeType.isBlank()) {
            throw IllegalArgumentException("transcription mimeType is required")
        }

        return request.copy(
            model = model,
            audio = audio,
            filename = filename,
            mime = mimeType,
        )
    }
}
