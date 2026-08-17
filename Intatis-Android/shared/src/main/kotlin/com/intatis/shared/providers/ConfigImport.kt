package com.intatis.shared.providers

import com.intatis.shared.protocol.Jsonx
import com.intatis.shared.protocol.Jsonx.str
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive

enum class SecretSource { ENVIRONMENT, FILE, AUTH_FILE, PROVIDER_CONFIG, LITERAL, NONE }

/**
 * A credential reference, never a credential value. Secrets resolve lazily at
 * provider-construction time and never enter the event log or projections.
 */
data class SecretRef(val source: SecretSource = SecretSource.NONE, val value: String = "") {
    fun describe(): String = when (source) {
        SecretSource.ENVIRONMENT -> "env $value"
        SecretSource.FILE -> "secret file $value"
        SecretSource.AUTH_FILE -> "auth file"
        SecretSource.PROVIDER_CONFIG -> "provider config"
        SecretSource.LITERAL -> "configured key"
        SecretSource.NONE -> "not configured"
    }
}

data class ModelEntry(val id: String, val displayName: String = "", val hidden: Boolean = false)

data class ProviderEntry(
    val id: String,
    val displayName: String = id,
    val baseUrl: String = "",
    val chatEndpoint: String? = null,
    val apiKeyRef: SecretRef = SecretRef(),
    val models: MutableList<ModelEntry> = mutableListOf(),
    val npm: String = "@ai-sdk/openai-compatible",
)

/** <provider-id>/<model-id> role binding. */
data class ModelRef(val providerId: String, val modelId: String) {
    val displayLabel: String get() = "$providerId/$modelId"
}

class ImportedConfig(
    var providers: MutableList<ProviderEntry> = mutableListOf(),
    var chat: ModelRef? = null,
    var reviewer: ModelRef? = null,
    var image: ModelRef? = null,
    var transcription: ModelRef? = null,
    var embedding: ModelRef? = null,
    var reranker: ModelRef? = null,
    var sourcePath: String = "",
    var warnings: MutableList<String> = mutableListOf(),
    var reviewerFailedClosed: Boolean = false,
) {
    fun provider(id: String): ProviderEntry? =
        providers.firstOrNull { it.id.equals(id, ignoreCase = true) }

    fun inferenceModels(): List<ModelEntry> = providers.flatMap { p -> p.models.filter { !it.hidden } }
}

/**
 * Parses the Intatis JSON/JSONC configuration: the provider map plus the top-level
 * role routes (model, permission_reviewer_model, image_model, transcription_model,
 * embedding_model, reranker_model). The reviewer binding is fail-closed when the
 * field is present but unresolvable.
 */
object ConfigImport {
    const val MAXIMUM_BYTE_COUNT = 1_048_576

    private val defaultBaseUrls = mapOf(
        "openai" to "https://api.openai.com/v1",
        "openrouter" to "https://openrouter.ai/api/v1",
        "deepseek" to "https://api.deepseek.com/v1",
        "ollama" to "http://localhost:11434/v1",
        "lmstudio" to "http://localhost:1234/v1",
        "groq" to "https://api.groq.com/openai/v1",
        "xai" to "https://api.x.ai/v1",
        "together" to "https://api.together.xyz/v1",
        "fireworks" to "https://api.fireworks.ai/inference/v1",
        "cerebras" to "https://api.cerebras.ai/v1",
        "moonshot" to "https://api.moonshot.cn/v1",
    )

    fun parse(source: String, sourcePath: String, environment: Map<String, String> = emptyMap()): ImportedConfig {
        require(source.length <= MAXIMUM_BYTE_COUNT) { "configuration exceeds 1 MiB" }
        val root = Jsonx.parseObject(Jsonx.stripJsonc(source))
        val warnings = mutableListOf<String>()

        val enabled = readStringList(root, "enabled_providers", "enabledProviders")
            .map { it.lowercase() }.toSet()
        val disabled = readStringList(root, "disabled_providers", "disabledProviders")
            .map { it.lowercase() }.toSet()

        val providers = mutableListOf<ProviderEntry>()
        val providerMap = root["provider"] as? JsonObject
        if (providerMap != null) {
            for ((providerId, providerNode) in providerMap) {
                if (providerNode == null) continue
                if (providerId.lowercase() in disabled) continue
                if (enabled.isNotEmpty() && providerId.lowercase() !in enabled) continue
                val entry = parseProvider(providerId, providerNode, environment, warnings) ?: run {
                    warnings.add("provider '$providerId' skipped: no resolvable base URL")
                    continue
                }
                providers.add(entry)
            }
        } else if (root["providers"] is JsonArray) {
            for (node in root["providers"] as JsonArray) {
                val obj = node as? JsonObject ?: continue
                val id = obj.str("id") ?: ""
                if (id.isEmpty()) continue
                val entry = parseProvider(id, obj, environment, warnings) ?: run {
                    warnings.add("provider '$id' skipped: no resolvable base URL")
                    continue
                }
                providers.add(entry)
            }
        }

        val chat = parseModelRef(root, "model", environment)
            ?: parseModelRef(root, "small_model", environment)
            ?: parseModelRef(root, "smallModel", environment)

        var reviewer: ModelRef? = null
        var reviewerFailedClosed = false
        if (root["permission_reviewer_model"] != null) {
            reviewer = parseModelRef(root, "permission_reviewer_model", environment)
            if (reviewer == null || resolve(reviewer, providers) == null) {
                reviewer = null
                reviewerFailedClosed = true
                warnings.add("permission_reviewer_model present but unresolvable; automatic review is disabled (fail closed)")
            }
        } else {
            // Compatibility: a missing field inherits the same document's top-level model.
            reviewer = if (chat != null && resolve(chat, providers) != null) chat else null
        }

        val image = optionalRole(root, "image_model", environment, providers, warnings)
        val transcription = optionalRole(root, "transcription_model", environment, providers, warnings)
        val embedding = optionalRole(root, "embedding_model", environment, providers, warnings)
        val reranker = optionalRole(root, "reranker_model", environment, providers, warnings)

        markRoleRoutedModelsHidden(providers, chat, reviewer, listOf(image, transcription, embedding, reranker))

        if (chat == null && providers.isNotEmpty()) {
            warnings.add("no top-level 'model' field; the active model must be picked in the UI")
        }

        return ImportedConfig(
            providers = providers,
            chat = chat,
            reviewer = reviewer,
            image = image,
            transcription = transcription,
            embedding = embedding,
            reranker = reranker,
            sourcePath = sourcePath,
            warnings = warnings,
            reviewerFailedClosed = reviewerFailedClosed,
        )
    }

    private fun parseProvider(
        providerId: String,
        node: JsonElement,
        environment: Map<String, String>,
        warnings: MutableList<String>,
    ): ProviderEntry? {
        val o = node as? JsonObject ?: return null
        val options = o["options"] as? JsonObject

        val baseUrl = options?.str("baseURL")
            ?: options?.str("baseUrl")
            ?: o.str("baseURL")
            ?: o.str("baseUrl")
            ?: defaultBaseUrls[providerId.lowercase()]
            ?: ""
        if (baseUrl.isEmpty()) return null

        val entry = ProviderEntry(
            id = providerId,
            displayName = o.str("displayName") ?: o.str("name") ?: providerId,
            baseUrl = baseUrl,
            chatEndpoint = options?.str("chatEndpoint") ?: o.str("chatEndpoint"),
            npm = o.str("npm") ?: "@ai-sdk/openai-compatible",
            apiKeyRef = parseCredential(providerId, options, o, environment),
        )

        val modelMap = o["models"] as? JsonObject
        if (modelMap != null) {
            for ((modelId, modelNode) in modelMap) {
                if (modelNode is JsonPrimitive) {
                    entry.models.add(ModelEntry(id = modelId, displayName = modelNode.content))
                    continue
                }
                val modelObj = modelNode as? JsonObject ?: continue
                entry.models.add(ModelEntry(
                    id = modelObj.str("id") ?: modelId,
                    displayName = modelObj.str("displayName") ?: modelObj.str("name") ?: modelId,
                ))
            }
        } else {
            val modelArray = o["models"] as? JsonArray
            modelArray?.forEach { modelNode ->
                val id = (modelNode as? JsonObject)?.str("id")
                    ?: (modelNode as? JsonPrimitive)?.content
                    ?: return@forEach
                val obj = modelNode as? JsonObject
                entry.models.add(ModelEntry(
                    id = id,
                    displayName = obj?.str("displayName") ?: obj?.str("name") ?: id,
                ))
            }
        }
        return entry
    }

    private fun parseCredential(
        providerId: String,
        options: JsonObject?,
        providerObj: JsonObject?,
        environment: Map<String, String>,
    ): SecretRef {
        val apiKeySource = providerObj?.get("apiKeySource") as? JsonObject
        if (apiKeySource != null) {
            return when (val type = (apiKeySource.str("type") ?: "").lowercase()) {
                "env" -> SecretRef(SecretSource.ENVIRONMENT, apiKeySource.str("value") ?: "")
                "file" -> SecretRef(SecretSource.FILE, apiKeySource.str("value") ?: "")
                "providerconfig" -> SecretRef(SecretSource.PROVIDER_CONFIG, providerId)
                else -> SecretRef(SecretSource.AUTH_FILE, providerId)
            }
        }

        val apiKey = options?.str("apiKey") ?: providerObj?.str("apiKey")
        if (!apiKey.isNullOrEmpty()) {
            val variable = parseConfigVariable(apiKey)
            return when {
                variable?.first == "env" -> SecretRef(SecretSource.ENVIRONMENT, variable.second)
                variable?.first == "file" -> SecretRef(SecretSource.FILE, variable.second)
                else -> SecretRef(SecretSource.LITERAL, apiKey)
            }
        }

        val envArray = providerObj?.get("env") as? JsonArray
        val apiKeyEnv = providerObj?.str("apiKeyEnv")
            ?: envArray?.firstOrNull()?.let { (it as? JsonPrimitive)?.content }
        if (!apiKeyEnv.isNullOrEmpty()) return SecretRef(SecretSource.ENVIRONMENT, apiKeyEnv)

        val apiKeyFile = providerObj?.str("apiKeyFile")
        if (!apiKeyFile.isNullOrEmpty()) return SecretRef(SecretSource.FILE, apiKeyFile)

        return SecretRef(SecretSource.AUTH_FILE, providerId)
    }

    private fun parseModelRef(root: JsonObject, field: String, environment: Map<String, String>): ModelRef? {
        var raw = root.str(field) ?: return null
        val variable = parseConfigVariable(raw)
        if (variable?.first == "env") {
            raw = environment[variable.second] ?: return null
        }
        val slash = raw.indexOf('/')
        if (slash <= 0 || slash >= raw.length - 1) return null
        return ModelRef(providerId = raw.substring(0, slash), modelId = raw.substring(slash + 1))
    }

    private fun optionalRole(
        root: JsonObject,
        field: String,
        environment: Map<String, String>,
        providers: List<ProviderEntry>,
        warnings: MutableList<String>,
    ): ModelRef? {
        val reference = parseModelRef(root, field, environment)
        if (reference == null) {
            if (root[field] != null) warnings.add("$field present but not in '<provider>/<model>' form; route disabled")
            return null
        }
        val provider = providers.firstOrNull { it.id.equals(reference.providerId, ignoreCase = true) }
        if (provider == null) {
            warnings.add("$field references unknown provider '${reference.providerId}'; route disabled")
            return null
        }
        return reference
    }

    internal fun resolve(reference: ModelRef, providers: List<ProviderEntry>): ProviderEntry? =
        providers.firstOrNull {
            it.id.equals(reference.providerId, ignoreCase = true) &&
                it.models.any { m -> m.id == reference.modelId }
        }

    /** Whole trimmed value must be {kind:value}; kinds are case-insensitive. */
    internal fun parseConfigVariable(value: String): Pair<String, String>? {
        val trimmed = value.trim()
        if (trimmed.length < 3 || trimmed[0] != '{' || trimmed[trimmed.length - 1] != '}') return null
        val inner = trimmed.substring(1, trimmed.length - 1)
        val colon = inner.indexOf(':')
        if (colon <= 0 || colon >= inner.length - 1) return null
        val kind = inner.substring(0, colon).trim().lowercase()
        val body = inner.substring(colon + 1).trim()
        if (kind !in setOf("env", "file")) return null
        return kind to body
    }

    /** Models bound only to extension roles stay out of the chat inference menus. */
    private fun markRoleRoutedModelsHidden(
        providers: MutableList<ProviderEntry>,
        chat: ModelRef?,
        reviewer: ModelRef?,
        extensionRoles: List<ModelRef?>,
    ) {
        val extensionKeys = extensionRoles.filterNotNull()
            .map { it.providerId.lowercase() to it.modelId }.toHashSet()
        val menuKeys = buildSet {
            chat?.let { add(it.providerId.lowercase() to it.modelId) }
            reviewer?.let { add(it.providerId.lowercase() to it.modelId) }
        }
        for (provider in providers) {
            for (i in provider.models.indices) {
                val model = provider.models[i]
                val key = provider.id.lowercase() to model.id
                if (key in extensionKeys && key !in menuKeys) {
                    provider.models[i] = model.copy(hidden = true)
                }
            }
        }
    }

    private fun readStringList(root: JsonObject, vararg fields: String): List<String> {
        for (field in fields) {
            val array = root[field] as? JsonArray ?: continue
            return array.mapNotNull { (it as? JsonPrimitive)?.content?.trim()?.lowercase() }
        }
        return emptyList()
    }
}
