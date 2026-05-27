package com.rokurics.app.ui.chat

import android.content.Context
import android.content.SharedPreferences
import com.rokurics.app.RokuricsApp
import com.rokurics.app.data.KeystoreSecureStorage
import com.rokurics.app.data.SecureStorage
import org.json.JSONObject

enum class AIProviderKind(val displayName: String) {
    OPEN_AI_COMPATIBLE("OpenAI-compatible"),
    ANTHROPIC_MESSAGES("Claude / Anthropic")
}

enum class AIProviderPreset(
    val displayName: String,
    val defaultBaseURL: String,
    val defaultModels: List<String>
) {
    CUSTOM("Custom", "https://", emptyList()),
    DEEP_SEEK("DeepSeek", "https://api.deepseek.com", listOf("deepseek-chat", "deepseek-reasoner")),
    OPEN_AI("OpenAI", "https://api.openai.com/v1", listOf("gpt-4o", "gpt-4o-mini")),
    GEMINI("Gemini", "https://generativelanguage.googleapis.com/v1beta/openai", listOf("gemini-2.0-flash"));

    val isAvailableOnPhone: Boolean
        get() = when (this) {
            CUSTOM, DEEP_SEEK, OPEN_AI, GEMINI -> true
        }
}

data class OpenAIConfiguration(
    val baseURLString: String = AIProviderPreset.OPEN_AI.defaultBaseURL,
    val modelName: String = AIProviderPreset.OPEN_AI.defaultModels.firstOrNull() ?: "",
    val apiKey: String = "",
    val temperature: Double = 0.3,
    val maxTokens: Int = 2000,
    val maxTranscriptCharacters: Int = 12000
) {
    val trimmedBaseURLString get() = baseURLString.trim()
    val trimmedModelName get() = modelName.trim()
    val trimmedAPIKey get() = apiKey.trim()

    fun toJson(): String = JSONObject().apply {
        put("baseURLString", baseURLString)
        put("modelName", modelName)
        put("apiKey", apiKey)
        put("temperature", temperature)
        put("maxTokens", maxTokens)
        put("maxTranscriptCharacters", maxTranscriptCharacters)
    }.toString()

    companion object {
        fun fromJson(json: String): OpenAIConfiguration {
            val obj = JSONObject(json)
            return OpenAIConfiguration(
                baseURLString = obj.optString("baseURLString", AIProviderPreset.OPEN_AI.defaultBaseURL),
                modelName = obj.optString("modelName", ""),
                apiKey = obj.optString("apiKey", ""),
                temperature = obj.optDouble("temperature", 0.3),
                maxTokens = obj.optInt("maxTokens", 2000),
                maxTranscriptCharacters = obj.optInt("maxTranscriptCharacters", 12000)
            )
        }
    }
}

data class AnthropicConfiguration(
    val baseURLString: String = "https://api.anthropic.com",
    val modelName: String = "claude-sonnet-4-6",
    val apiKey: String = "",
    val anthropicVersion: String = "2023-06-01",
    val temperature: Double = 0.3,
    val maxTokens: Int = 2000,
    val maxTranscriptCharacters: Int = 12000
) {
    val trimmedBaseURLString get() = baseURLString.trim()
    val trimmedModelName get() = modelName.trim()
    val trimmedAPIKey get() = apiKey.trim()

    fun toJson(): String = JSONObject().apply {
        put("baseURLString", baseURLString)
        put("modelName", modelName)
        put("apiKey", apiKey)
        put("anthropicVersion", anthropicVersion)
        put("temperature", temperature)
        put("maxTokens", maxTokens)
        put("maxTranscriptCharacters", maxTranscriptCharacters)
    }.toString()

    companion object {
        fun fromJson(json: String): AnthropicConfiguration {
            val obj = JSONObject(json)
            return AnthropicConfiguration(
                baseURLString = obj.optString("baseURLString", "https://api.anthropic.com"),
                modelName = obj.optString("modelName", "claude-sonnet-4-6"),
                apiKey = obj.optString("apiKey", ""),
                anthropicVersion = obj.optString("anthropicVersion", "2023-06-01"),
                temperature = obj.optDouble("temperature", 0.3),
                maxTokens = obj.optInt("maxTokens", 2000),
                maxTranscriptCharacters = obj.optInt("maxTranscriptCharacters", 12000)
            )
        }
    }
}

class AISettingsStore(
    context: Context = RokuricsApp.instance,
    private val secureStorage: SecureStorage = KeystoreSecureStorage(context)
) {
    private val prefs: SharedPreferences =
        context.getSharedPreferences("rokurics_ai_settings", Context.MODE_PRIVATE)

    init {
        migrateApiKeysIfNeeded()
    }

    private fun migrateApiKeysIfNeeded() {
        if (!prefs.getBoolean(MIGRATED_KEY, false)) {
            // Migrate OpenAI apiKey
            val openAIJson = prefs.getString(KEY_OPENAI_CONFIG, null)
            if (openAIJson != null) {
                try {
                    val config = OpenAIConfiguration.fromJson(openAIJson)
                    if (config.apiKey.isNotEmpty() && !secureStorage.contains(KEY_OPENAI_API_KEY)) {
                        secureStorage.put(KEY_OPENAI_API_KEY, config.apiKey)
                        val sanitized = config.copy(apiKey = "").toJson()
                        prefs.edit().putString(KEY_OPENAI_CONFIG, sanitized).apply()
                    }
                } catch (_: Exception) {}
            }
            // Migrate Anthropic apiKey
            val anthropicJson = prefs.getString(KEY_ANTHROPIC_CONFIG, null)
            if (anthropicJson != null) {
                try {
                    val config = AnthropicConfiguration.fromJson(anthropicJson)
                    if (config.apiKey.isNotEmpty() && !secureStorage.contains(KEY_ANTHROPIC_API_KEY)) {
                        secureStorage.put(KEY_ANTHROPIC_API_KEY, config.apiKey)
                        val sanitized = config.copy(apiKey = "").toJson()
                        prefs.edit().putString(KEY_ANTHROPIC_CONFIG, sanitized).apply()
                    }
                } catch (_: Exception) {}
            }
            prefs.edit().putBoolean(MIGRATED_KEY, true).apply()
        }
    }

    var selectedProviderKind: AIProviderKind
        get() {
            val raw = prefs.getString(KEY_PROVIDER_KIND, null)
            return raw?.let { try { AIProviderKind.valueOf(it) } catch (_: Exception) { null } }
                ?: AIProviderKind.OPEN_AI_COMPATIBLE
        }
        set(value) = prefs.edit().putString(KEY_PROVIDER_KIND, value.name).apply()

    var selectedProviderPreset: AIProviderPreset
        get() {
            val raw = prefs.getString(KEY_PROVIDER_PRESET, null)
            return raw?.let { try { AIProviderPreset.valueOf(it) } catch (_: Exception) { null } }
                ?: AIProviderPreset.OPEN_AI
        }
        set(value) = prefs.edit().putString(KEY_PROVIDER_PRESET, value.name).apply()

    var openAIConfiguration: OpenAIConfiguration
        get() {
            val json = prefs.getString(KEY_OPENAI_CONFIG, null) ?: return OpenAIConfiguration()
            val config = try { OpenAIConfiguration.fromJson(json) } catch (_: Exception) { OpenAIConfiguration() }
            val apiKey = secureStorage.get(KEY_OPENAI_API_KEY) ?: config.apiKey
            return config.copy(apiKey = apiKey)
        }
        set(value) {
            if (value.apiKey.isNotEmpty()) {
                secureStorage.put(KEY_OPENAI_API_KEY, value.apiKey)
            } else {
                secureStorage.remove(KEY_OPENAI_API_KEY)
            }
            prefs.edit().putString(KEY_OPENAI_CONFIG, value.copy(apiKey = "").toJson()).apply()
        }

    var anthropicConfiguration: AnthropicConfiguration
        get() {
            val json = prefs.getString(KEY_ANTHROPIC_CONFIG, null) ?: return AnthropicConfiguration()
            val config = try { AnthropicConfiguration.fromJson(json) } catch (_: Exception) { AnthropicConfiguration() }
            val apiKey = secureStorage.get(KEY_ANTHROPIC_API_KEY) ?: config.apiKey
            return config.copy(apiKey = apiKey)
        }
        set(value) {
            if (value.apiKey.isNotEmpty()) {
                secureStorage.put(KEY_ANTHROPIC_API_KEY, value.apiKey)
            } else {
                secureStorage.remove(KEY_ANTHROPIC_API_KEY)
            }
            prefs.edit().putString(KEY_ANTHROPIC_CONFIG, value.copy(apiKey = "").toJson()).apply()
        }

    fun updateOpenAI(preset: AIProviderPreset, config: OpenAIConfiguration) {
        selectedProviderKind = AIProviderKind.OPEN_AI_COMPATIBLE
        selectedProviderPreset = preset
        openAIConfiguration = config
    }

    fun updateAnthropic(config: AnthropicConfiguration) {
        selectedProviderKind = AIProviderKind.ANTHROPIC_MESSAGES
        anthropicConfiguration = config
    }

    companion object {
        private const val KEY_PROVIDER_KIND = "ai.providerKind"
        private const val KEY_PROVIDER_PRESET = "ai.providerPreset"
        private const val KEY_OPENAI_CONFIG = "ai.openAIConfig"
        private const val KEY_ANTHROPIC_CONFIG = "ai.anthropicConfig"
        private const val KEY_OPENAI_API_KEY = "ai.openai_apikey"
        private const val KEY_ANTHROPIC_API_KEY = "ai.anthropic_apikey"
        private const val MIGRATED_KEY = "_ai_secure_migrated_v1"
    }
}
