package com.intatis.shared.config

import com.intatis.shared.model.IntatisConfig
import com.intatis.shared.model.IntatisMode
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.jsonPrimitive
import java.io.File
import java.nio.file.Path
import java.nio.file.Paths

private val json = Json { ignoreUnknownKeys = true }

object ConfigStore {
    private const val APP_FOLDER = "Intatis"
    private const val APP_NAME = "Intatis-Android"
    private const val CONFIG_FILE = "config.json"

    private fun defaultFolder(customRoot: String?): Path {
        val root = if (!customRoot.isNullOrBlank()) {
            File(customRoot)
        } else {
            val home = System.getProperty("user.home") ?: "."
            val os = System.getProperty("os.name").lowercase()
            if (os.contains("win")) File(System.getenv("APPDATA") ?: "$home\\AppData\\Roaming")
            else File(home, ".config")
        }
        return Paths.get(root.path, APP_FOLDER, APP_NAME)
    }

    fun configFolder(customRoot: String? = null): Path = defaultFolder(customRoot)
    fun configPath(customRoot: String? = null): Path = defaultFolder(customRoot).resolve(CONFIG_FILE)

    suspend fun load(customRoot: String? = null): IntatisConfig = withContext(Dispatchers.IO) {
        val values = readFromFile(customRoot)
        val baseUrl = readValue("INTATIS_BASE_URL", "baseUrl", values, "https://api.openai.com/v1")
        val apiKey = readValue("INTATIS_API_KEY", "apiKey", values, "")
        val model = readValue("INTATIS_MODEL", "model", values, "gpt-4o-mini")
        val reasoning = readValue("INTATIS_REASONING", "reasoning", values, "", allowBlank = true)
        val modeValue = readValue("INTATIS_MODE", "defaultMode", values, IntatisMode.CHAT.name.lowercase())
        val workspace = readValue("INTATIS_WORKSPACE", "workspace", values, "")
        val usageValue = readValue("INTATIS_USAGE", "includeUsage", values, "1")

        IntatisConfig(
            baseUrl = baseUrl,
            apiKey = apiKey,
            model = model,
            reasoning = reasoning.ifBlank { null },
            defaultMode = runCatching { IntatisMode.valueOf(modeValue.uppercase()) }.getOrDefault(IntatisMode.CHAT),
            workspace = workspace.ifBlank { null },
            includeUsage = usageValue.lowercase() !in setOf("0", "false", "off"),
        )
    }

    suspend fun save(config: IntatisConfig, customRoot: String? = null) = withContext(Dispatchers.IO) {
        val path = configPath(customRoot)
        path.toFile().parentFile?.mkdirs()
        val map = linkedMapOf(
            "baseUrl" to config.baseUrl,
            "apiKey" to config.apiKey,
            "model" to config.model,
            "defaultMode" to config.defaultMode.name.lowercase(),
            "includeUsage" to if (config.includeUsage) "1" else "0",
        )
        if (!config.reasoning.isNullOrBlank()) map["reasoning"] = config.reasoning
        if (!config.workspace.isNullOrBlank()) map["workspace"] = config.workspace
        path.toFile().writeText(json.encodeToString(map))
    }

    private suspend fun readFromFile(customRoot: String?): Map<String, String> = withContext(Dispatchers.IO) {
        val path = configPath(customRoot).toFile()
        if (!path.exists()) return@withContext emptyMap()
        val raw = runCatching { path.readText() }.getOrNull() ?: return@withContext emptyMap()
        val root = runCatching { json.parseToJsonElement(raw).jsonObject }.getOrNull() ?: return@withContext emptyMap()
        root.entries.associate { it.key to runCatching { it.value.jsonPrimitive.content }.getOrNull().orEmpty() }
    }

    private fun readValue(
        envKey: String,
        fileKey: String,
        values: Map<String, String>,
        default: String,
        allowBlank: Boolean = false,
    ): String {
        val env = System.getenv(envKey)
        if (!env.isNullOrBlank()) return env
        val value = values[fileKey]
        if (!value.isNullOrBlank() || allowBlank) return value.ifNullOrEmpty(default)
        return default
    }

    private fun String?.ifNullOrEmpty(defaultValue: String): String = this?.ifBlank { defaultValue } ?: defaultValue
}
