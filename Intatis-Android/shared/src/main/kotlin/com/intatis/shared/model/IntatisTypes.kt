package com.intatis.shared.model

import java.time.Instant
import java.util.Locale
import java.net.URL

enum class IntatisMode {
    CHAT,
    CODE,
    COWORK
}

enum class MessageRole {
    USER,
    ASSISTANT,
    SYSTEM,
    TOOL,
    AGENT
}

data class IntatisMessage(
    val id: String,
    val role: MessageRole,
    val content: String,
    val at: Instant,
) {
    constructor(role: MessageRole, content: String) : this(java.util.UUID.randomUUID().toString(), role, content, Instant.now())
}

data class AppProviderSettings(
    val id: String,
    val displayName: String,
    val baseUrl: String,
    val chatEndpoint: String,
    val apiKeySource: AppProviderAPIKeySource? = null,
    val models: List<ProviderModel>,
) {
    val title: String
        get() = displayName.trim().ifBlank { runCatching { URL(baseUrl).host }.getOrElse { id }.ifBlank { id } }

    constructor(
        id: String,
        baseUrl: String,
        chatEndpoint: String,
        models: List<ProviderModel>,
        apiKeySource: AppProviderAPIKeySource? = null,
    ) : this(
        id = normalizeProviderId(id),
        displayName = id,
        baseUrl = baseUrl,
        chatEndpoint = chatEndpoint,
        apiKeySource = apiKeySource,
        models = models,
    )
}

data class AppProviderCatalog(
    val selectedProviderID: String,
    val selectedModelID: String,
    val providers: List<AppProviderSettings>,
) {
    val selectedProvider: AppProviderSettings?
        get() = providers.firstOrNull { it.id == normalizeProviderId(selectedProviderID) } ?: providers.firstOrNull()

    val selectedModel: ProviderModel?
        get() = selectedProvider?.models?.firstOrNull { it.id == selectedModelID }
            ?: selectedProvider?.models?.firstOrNull()
}

data class ProviderModelRef(
    val endpoint: String,
    val model: String,
)

data class ResolvedModels(
    val chat: ProviderModelRef,
    val agent: ProviderModelRef,
    var imageGen: ProviderModelRef,
    var transcription: ProviderModelRef,
)

data class ProviderEndpoint(
    val id: String,
    val baseUrl: String,
    val chatEndpoint: String,
    val apiKeyRef: AppProviderAPIKeyRef,
    val wire: String = "openai",
)

data class ProviderConfig(
    val endpoints: List<ProviderEndpoint>,
    val models: ResolvedModels,
)

data class AppProviderAPIKeyRef(
    val source: String,
    val value: String,
    val providerId: String,
)

data class AppProviderAPIKeySource(
    var type: String,
    var value: String,
) {
    companion object {
        fun environment(name: String): AppProviderAPIKeySource =
            AppProviderAPIKeySource(type = "env", value = name)
        fun file(path: String): AppProviderAPIKeySource =
            AppProviderAPIKeySource(type = "file", value = path)
        fun authFile(): AppProviderAPIKeySource = AppProviderAPIKeySource(type = "authFile", value = "")
        fun providerConfig(path: String): AppProviderAPIKeySource =
            AppProviderAPIKeySource(type = "providerConfig", value = path)
    }

    private val normalizedType: String
        get() = when (type.trim().lowercase(Locale.ROOT)) {
            "env", "environment" -> "env"
            "file", "path" -> "file"
            "authfile", "auth_file", "auth-json", "authjson", "json" -> "authFile"
            "providerconfig", "provider_config", "config", "configfile", "config_file" -> "providerConfig"
            else -> "authFile"
        }

    fun ref(defaultRef: String, providerID: String): AppProviderAPIKeyRef {
        val trimmed = value.trim()
        return when (normalizedType) {
            "env" -> if (trimmed.isEmpty()) AppProviderAPIKeyRef("keychain", defaultRef, providerID) else
                AppProviderAPIKeyRef("environment", trimmed, providerID)
            "file" -> if (trimmed.isEmpty()) AppProviderAPIKeyRef("keychain", defaultRef, providerID) else
                AppProviderAPIKeyRef("file", trimmed, providerID)
            "authFile" -> AppProviderAPIKeyRef("authFile", providerID, providerID)
            "providerConfig" -> if (trimmed.isEmpty()) AppProviderAPIKeyRef("keychain", defaultRef, providerID) else
                AppProviderAPIKeyRef("providerConfig", trimmed, providerID)
            else -> AppProviderAPIKeyRef("keychain", defaultRef, providerID)
        }
    }

    val isLegacyKeychain: Boolean
        get() = normalizedType == "keychain"

    val openCodeAPIKeyValue: String?
        get() = when (normalizedType) {
            "env" -> value.trim().takeIf { it.isNotEmpty() }?.let { "{env:$it}" }
            "file" -> value.trim().takeIf { it.isNotEmpty() }?.let { "{file:$it}" }
            else -> null
        }
}

data class IntatisConfig(
    val baseUrl: String,
    val apiKey: String,
    val model: String,
    val selectedModel: String,
    val reasoning: String?,
    val defaultMode: IntatisMode,
    val workspace: String?,
    val chatProviderId: String,
    val agentToolProviderId: String,
    val imageProviderId: String,
    val transcriptionProviderId: String,
    val includeUsage: Boolean,
) {
    private val selectedModelID: String
        get() = selectedModel.trim().ifBlank { model.trim().ifBlank { "gpt-4o-mini" } }

    fun providerConfig(): ProviderConfig {
        val fallbackProvider = appProviderCatalog.selectedProvider ?: AppProviderSettings(
            id = "openai",
            displayName = providerDisplayName("openai"),
            baseUrl = baseUrl,
            chatEndpoint = chatEndpointFrom(baseUrl),
            models = listOf(
                ProviderModel(
                    id = selectedModelID,
                    displayName = defaultModelDisplayName(selectedModelID),
                ),
            ),
            apiKeySource = AppProviderAPIKeySource.authFile(),
        )
        val selectedProvider = appProviderCatalog.selectedProvider ?: fallbackProvider
        val selectedModel = appProviderCatalog.selectedModel
            ?: selectedProvider.models.firstOrNull()
            ?: ProviderModel(
                id = selectedModelID,
                displayName = defaultModelDisplayName(selectedModelID),
            )
        val endpointProviders = appProviderCatalog.providers.ifEmpty { listOf(fallbackProvider) }
        val endpoints = endpointProviders.map { provider ->
            ProviderEndpoint(
                id = provider.id,
                baseUrl = provider.baseUrl,
                chatEndpoint = provider.chatEndpoint.ifBlank { chatEndpointFrom(provider.baseUrl) },
                apiKeyRef = provider.apiKeySource?.ref(apiKey, provider.id)
                    ?: AppProviderAPIKeyRef(source = "keychain", value = apiKey, providerId = provider.id),
                wire = "openai",
            )
        }

        return ProviderConfig(
            endpoints = if (endpoints.isEmpty()) listOf(endpoint(forProvider = selectedProvider)) else endpoints,
            models = ResolvedModels(
                chat = ProviderModelRef(endpoint = selectedProvider.id, model = selectedModel.id),
                agent = ProviderModelRef(endpoint = selectedProvider.id, model = selectedModel.id),
                imageGen = ProviderModelRef(endpoint = selectedProvider.id, model = "dall-e-3"),
                transcription = ProviderModelRef(endpoint = selectedProvider.id, model = "whisper-1"),
            ),
        )
    }

    val appProviderCatalog: AppProviderCatalog
        get() {
            val selectedProviderID = appProviderSettings.firstOrNull()?.id ?: normalizeProviderId(chatProviderId)
            return AppProviderCatalog(
                selectedProviderID = selectedProviderID,
                selectedModelID = selectedModelID,
                providers = appProviderSettings,
            )
        }

    fun selectProviderModel(providerID: String, modelID: String): IntatisConfig {
        val provider = appProviderCatalog.providers.firstOrNull { it.id == normalizeProviderId(providerID) } ?: return this
        val selected = provider.models.firstOrNull { it.id == modelID }?.id
            ?: provider.models.firstOrNull()?.id
            ?: selectedModelID
        return copy(
            chatProviderId = provider.id,
            selectedModel = selected,
        )
    }

    val appProviderSettings: List<AppProviderSettings>
        get() {
            val providerIds = listOfNotNull(
                chatProviderId.takeUnless { it.isBlank() },
                agentToolProviderId.takeUnless { it.isBlank() },
                imageProviderId.takeUnless { it.isBlank() },
                transcriptionProviderId.takeUnless { it.isBlank() },
            ).map(::normalizeProviderId).distinct()

            return providerIds.map { providerId ->
                AppProviderSettings(
                    id = providerId,
                    displayName = providerDisplayName(providerId),
                    baseUrl = baseUrl,
                    chatEndpoint = chatEndpointFrom(baseUrl),
                    models = providerModelCatalog[providerId] ?: listOf(
                        ProviderModel(
                            id = selectedModelID,
                            displayName = defaultModelDisplayName(selectedModelID),
                        ),
                    ),
                    apiKeySource = AppProviderAPIKeySource.authFile(),
                )
            }
        }

    val providerModelCatalog: Map<String, List<ProviderModel>>
        get() {
            val effectiveModel = selectedModelID
            val defaultModel = ProviderModel(
                id = effectiveModel,
                displayName = defaultModelDisplayName(effectiveModel),
            )
            val providerIds = listOfNotNull(
                chatProviderId.takeUnless { it.isBlank() },
                agentToolProviderId.takeUnless { it.isBlank() },
                imageProviderId.takeUnless { it.isBlank() },
                transcriptionProviderId.takeUnless { it.isBlank() },
            )
            return providerIds.distinct().associate {
                it.trim().lowercase(Locale.ROOT) to listOf(defaultModel)
            }
        }

    fun cloneWith(
        baseUrl: String? = null,
        apiKey: String? = null,
        model: String? = null,
        reasoning: String? = null,
        defaultMode: IntatisMode? = null,
        workspace: String? = null,
        chatProviderId: String? = null,
        agentToolProviderId: String? = null,
        imageProviderId: String? = null,
        transcriptionProviderId: String? = null,
        selectedModel: String? = null,
        includeUsage: Boolean? = null,
    ) = IntatisConfig(
        baseUrl ?: this.baseUrl,
        apiKey ?: this.apiKey,
        model ?: this.model,
        selectedModel = selectedModel ?: this.selectedModel,
        reasoning,
        defaultMode ?: this.defaultMode,
        workspace,
        chatProviderId = chatProviderId ?: this.chatProviderId,
        agentToolProviderId = agentToolProviderId ?: this.agentToolProviderId,
        imageProviderId = imageProviderId ?: this.imageProviderId,
        transcriptionProviderId = transcriptionProviderId ?: this.transcriptionProviderId,
        includeUsage ?: this.includeUsage,
    )

    private fun endpoint(forProvider: AppProviderSettings) = ProviderEndpoint(
        id = forProvider.id,
        baseUrl = forProvider.baseUrl,
        chatEndpoint = forProvider.chatEndpoint.ifBlank { chatEndpointFrom(forProvider.baseUrl) },
        apiKeyRef = forProvider.apiKeySource?.ref(apiKey, forProvider.id)
            ?: AppProviderAPIKeyRef(source = "keychain", value = apiKey, providerId = forProvider.id),
        wire = "openai",
    )
}

data class ProviderModel(
    val id: String,
    val displayName: String,
)

data class SearchHit(
    val file: String,
    val line: Int,
    val text: String,
)

private fun defaultModelDisplayName(modelId: String): String = when (modelId) {
    "gpt-4o-mini" -> "GPT-4o mini"
    "gpt-4o" -> "GPT-4o"
    else -> modelId
}

private fun normalizeProviderId(raw: String): String =
    raw.trim().lowercase(Locale.ROOT).ifEmpty { "default" }

private fun providerDisplayName(providerId: String): String = when (normalizeProviderId(providerId)) {
    "openai" -> "OpenAI"
    "openrouter" -> "OpenRouter"
    "deepseek" -> "DeepSeek"
    "ollama" -> "Ollama"
    "lmstudio", "lm-studio" -> "LM Studio"
    "groq" -> "Groq"
    "xai" -> "xAI"
    "together" -> "Together"
    "fireworks" -> "Fireworks"
    "cerebras" -> "Cerebras"
    "moonshot" -> "Moonshot"
    else -> providerId.trim().ifBlank { "default" }
}

private fun chatEndpointFrom(baseURL: String): String {
    val trimmed = baseURL.trim().trimEnd('/')
    return if (trimmed.lowercase().endsWith("/chat/completions")) trimmed else "$trimmed/chat/completions"
}
