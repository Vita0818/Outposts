package com.intatis.shared

import java.io.File
import java.nio.file.Path
import java.nio.file.Paths
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.doubleOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive

object ConfigStore {
    private const val ConfigDir = ".config"
    private const val AppDir = "intatis"
    private const val ConfigFile = "config.json"
    private val json = Json {
        ignoreUnknownKeys = true
    }

    @Volatile
    private var configHomeOverride: File? = null

    fun configureConfigDirectory(directory: File) {
        configHomeOverride = directory
    }

    fun clearConfigDirectoryOverride() {
        configHomeOverride = null
    }

    val configFolder: String
        get() {
            val override = configHomeOverride
            if (override != null) return override.absolutePath

            val home = System.getProperty("user.home")
                ?: System.getProperty("user.dir")
                ?: "."
            return File(home, "$ConfigDir/$AppDir").absolutePath
        }

    val configPath: String
        get() = Paths.get(configFolder, ConfigFile).toString()

    fun load(): IntatisConfig {
        val fileValues = loadFileValues()

        fun value(envKey: String, fileKey: String, fallback: String): String {
            val env = System.getenv(envKey)
            if (!env.isNullOrBlank()) return env
            val fromFile = fileValues[fileKey]
            if (!fromFile.isNullOrBlank()) return fromFile
            return fallback
        }

        val baseUrl = value("INTATIS_BASE_URL", "baseUrl", "https://api.openai.com/v1")
        val apiKey = value("INTATIS_API_KEY", "apiKey", "")
        val model = value("INTATIS_MODEL", "model", "gpt-4o-mini")
        val reasoning = System.getenv("INTATIS_REASONING") ?: fileValues["reasoning"]

        val modeString = value("INTATIS_MODE", "defaultMode", IntatisMode.Chat.name).lowercase()
        val mode = try {
            IntatisMode.valueOf(modeString.replaceFirstChar { it.uppercaseChar() })
        } catch (_: IllegalArgumentException) {
            IntatisMode.Chat
        }

        val workspace = System.getenv("INTATIS_WORKSPACE") ?: fileValues["workspace"]
        val usageValue = value("INTATIS_USAGE", "includeUsage", "1").lowercase()
        val includeUsage = !(usageValue == "0" || usageValue == "false" || usageValue == "off")

        return IntatisConfig(
            baseUrl = baseUrl,
            apiKey = apiKey,
            model = model,
            reasoning = reasoning?.trim()?.takeIf { it.isNotEmpty() && it != "off" },
            defaultMode = mode,
            workspace = workspace?.takeIf { it.isNotBlank() },
            includeUsage = includeUsage,
        )
    }

    fun save(config: IntatisConfig) {
        val folder = File(configFolder)
        folder.mkdirs()

        val payload = buildJsonObject {
            put("baseUrl", JsonPrimitive(config.baseUrl))
            put("apiKey", JsonPrimitive(config.apiKey))
            put("model", JsonPrimitive(config.model))
            put("defaultMode", JsonPrimitive(config.defaultMode.name))
            config.reasoning?.let { put("reasoning", JsonPrimitive(it)) }
            config.workspace?.takeIf { it.isNotBlank() }?.let { put("workspace", JsonPrimitive(it)) }
            put("includeUsage", JsonPrimitive(config.includeUsage))
        }

        val target = File(configPath)
        target.parentFile.mkdirs()
        target.writeText(payload.toString())

        try {
            target.setReadable(false)
            target.setReadable(true, true)
            target.setWritable(true, true)
        } catch (_: Throwable) {
            // best effort
        }
    }

    private fun loadFileValues(): Map<String, String> {
        val file = File(configPath)
        if (!file.exists()) return emptyMap()

        return try {
            val element = json.parseToJsonElement(file.readText()).jsonObject
            element.entries.associate { entry ->
                entry.key to valueToString(entry.value)
            }
        } catch (_: Throwable) {
            emptyMap()
        }
    }

    private fun valueToString(element: kotlinx.serialization.json.JsonElement): String {
        return when (element) {
            is JsonObject -> json.encodeToString(JsonObject.serializer(), element)
            else -> element.jsonPrimitive.contentOrNull?.trim() ?: element.toString().trim('"')
        }
    }
}
