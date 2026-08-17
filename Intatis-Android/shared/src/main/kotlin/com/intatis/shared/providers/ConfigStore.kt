package com.intatis.shared.providers

import com.intatis.shared.protocol.Jsonx
import com.intatis.shared.protocol.Jsonx.str
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.io.File

interface SecretResolver {
    fun resolveSecret(reference: SecretRef): String
}

/**
 * Resolves credential references lazily at provider-construction time: env vars,
 * secret files, the owner-selected auth.json, or the provider config file.
 */
class ConfigSecretResolver(
    private val authFilePath: File,
    private val configPath: File,
) : SecretResolver {

    override fun resolveSecret(reference: SecretRef): String = try {
        when (reference.source) {
            SecretSource.ENVIRONMENT -> System.getenv(reference.value) ?: ""

            SecretSource.FILE ->
                if (File(reference.value).exists()) File(reference.value).readText().trim() else ""

            SecretSource.AUTH_FILE -> lookupAuthFile(reference.value)

            SecretSource.PROVIDER_CONFIG -> lookupProviderConfig(reference.value)

            SecretSource.LITERAL -> reference.value

            SecretSource.NONE -> ""
        }
    } catch (_: Exception) {
        ""
    }

    private fun lookupAuthFile(providerId: String): String {
        if (!authFilePath.exists()) return ""
        val node = Jsonx.parseObjectOrNull(authFilePath.readText()) ?: return ""
        return node.str(providerId) ?: node.str(providerId.lowercase()) ?: ""
    }

    private fun lookupProviderConfig(providerId: String): String {
        if (!configPath.exists()) return ""
        val node = Jsonx.parseObjectOrNull(Jsonx.stripJsonc(configPath.readText())) ?: return ""
        val provider = node["provider"] as? JsonObject ?: return ""
        val entry = (provider[providerId] ?: provider[providerId.lowercase()]) as? JsonObject ?: return ""
        return (entry["options"] as? JsonObject)?.str("apiKey") ?: entry.str("apiKey") ?: ""
    }
}

/**
 * Builds providers on demand. The API key is only resolved here, at construction
 * time, never at config-load time.
 */
class ProviderRegistry(
    val config: ImportedConfig,
    private val resolver: SecretResolver,
) {
    private val cache = mutableMapOf<String, OpenAIWireProvider>()
    private val http = OpenAIWireProvider.defaultClient()

    @Synchronized
    fun chatProviderFor(providerId: String): OpenAIWireProvider {
        cache[providerId]?.let { return it }
        val entry = config.provider(providerId)
            ?: throw IllegalArgumentException("unknown provider '$providerId'")
        val apiKey = resolver.resolveSecret(entry.apiKeyRef)
        val provider = OpenAIWireProvider(http, entry.baseUrl, apiKey, entry.chatEndpoint)
        cache[providerId] = provider
        return provider
    }

    @Synchronized
    fun hasCredential(providerId: String): Boolean {
        val entry = config.provider(providerId) ?: return false
        return resolver.resolveSecret(entry.apiKeyRef).isNotEmpty()
    }
}

/** Application-level paths and defaults; hosts inject their own roots. */
object AppConfig {
    const val DEFAULT_BASE_URL = "https://api.openai.com/v1"
    const val DEFAULT_MODEL = "gpt-4o-mini"
    const val CONFIG_FILE_NAME = "intatis.json"
    const val CONFIG_FILE_NAME_C = "intatis.jsonc"

    /**
     * Candidate config files in priority order: INTATIS_CONFIG, injected home,
     * ~/.intatis. Android hosts pass their filesDir-backed home via [configCandidates].
     */
    fun configCandidates(home: File? = null): List<File> {
        val candidates = mutableListOf<File>()
        System.getenv("INTATIS_CONFIG")?.takeIf { it.isNotBlank() }?.let { candidates.add(File(it)) }

        home?.let {
            candidates.add(File(it, CONFIG_FILE_NAME_C))
            candidates.add(File(it, CONFIG_FILE_NAME))
        }
        val userHome = File(System.getProperty("user.home"), ".intatis")
        candidates.add(File(userHome, CONFIG_FILE_NAME_C))
        candidates.add(File(userHome, CONFIG_FILE_NAME))
        return candidates
    }

    fun defaultConfigPath(home: File? = null): File =
        configCandidates(home).firstOrNull { it.exists() } ?: configCandidates(home).first()
}

/** Loads the Intatis configuration from the candidate paths, with env overrides. */
object ConfigStore {

    fun load(home: File? = null): Pair<ImportedConfig, File> {
        val candidates = AppConfig.configCandidates(home)
        for (candidate in candidates) {
            if (!candidate.exists()) continue
            val environment = System.getenv().toMap()
            val config = ConfigImport.parse(candidate.readText(), candidate.path, environment)
            return applyEnvOverrides(config) to candidate
        }
        return applyEnvOverrides(ImportedConfig()) to File("")
    }

    fun save(config: ImportedConfig, path: File) {
        path.parentFile?.mkdirs()
        path.writeText(serialize(config))
    }

    /** Writes the canonical modern shape (provider map + role fields). */
    fun serialize(config: ImportedConfig): String {
        val providerMap = buildJsonObject {
            config.providers.forEach { provider ->
                put(provider.id, buildJsonObject {
                    put("npm", provider.npm)
                    put("displayName", provider.displayName)
                    put("options", buildJsonObject {
                        put("baseURL", provider.baseUrl)
                        put("apiKey", when (provider.apiKeyRef.source) {
                            SecretSource.ENVIRONMENT -> "{env:${provider.apiKeyRef.value}}"
                            SecretSource.FILE -> "{file:${provider.apiKeyRef.value}}"
                            else -> provider.apiKeyRef.value
                        })
                        provider.chatEndpoint?.takeIf { it.isNotEmpty() }?.let { put("chatEndpoint", it) }
                    })
                    put("models", buildJsonObject {
                        provider.models.forEach { model ->
                            put(model.id, model.displayName.ifEmpty { model.id })
                        }
                    })
                })
            }
        }

        val root = buildJsonObject {
            put("provider", providerMap)
            config.chat?.let { put("model", it.displayLabel) }
            config.reviewer?.let { put("permission_reviewer_model", it.displayLabel) }
            config.image?.let { put("image_model", it.displayLabel) }
            config.transcription?.let { put("transcription_model", it.displayLabel) }
            config.embedding?.let { put("embedding_model", it.displayLabel) }
            config.reranker?.let { put("reranker_model", it.displayLabel) }
        }
        return Jsonx.pretty.encodeToString(JsonObject.serializer(), root)
    }

    private fun applyEnvOverrides(config: ImportedConfig): ImportedConfig {
        val baseUrl = System.getenv("INTATIS_BASE_URL")
        val apiKey = System.getenv("INTATIS_API_KEY")
        val model = System.getenv("INTATIS_MODEL")
        if (baseUrl == null && apiKey == null && model == null) return config

        var providers: MutableList<ProviderEntry> = config.providers
        if (!baseUrl.isNullOrBlank() || !apiKey.isNullOrBlank()) {
            providers = ArrayList(config.providers)
            providers.add(ProviderEntry(
                id = "env",
                displayName = "Environment",
                baseUrl = baseUrl ?: AppConfig.DEFAULT_BASE_URL,
                apiKeyRef = if (!apiKey.isNullOrBlank()) {
                    SecretRef(SecretSource.ENVIRONMENT, "INTATIS_API_KEY")
                } else {
                    SecretRef(SecretSource.AUTH_FILE, "env")
                },
            ))
        }

        var chat = config.chat
        if (!model.isNullOrBlank()) {
            val slash = model.indexOf('/')
            chat = if (slash > 0) {
                ModelRef(model.substring(0, slash), model.substring(slash + 1))
            } else {
                ModelRef("env", model)
            }
        }

        config.providers = providers
        config.chat = chat
        return config
    }
}
