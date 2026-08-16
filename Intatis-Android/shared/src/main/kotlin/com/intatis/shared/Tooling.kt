package com.intatis.shared

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.contentOrNull
import kotlinx.coroutines.CompletableDeferred
import kotlinx.serialization.json.intOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import java.io.File
import java.net.HttpURLConnection
import java.net.URL
import java.nio.charset.Charset
import java.util.ArrayDeque
import java.time.Instant

data class ToolDescriptor(
    val name: String,
    val description: String,
    val sideEffect: SideEffect,
    val parameters: Map<String, Any>
) {
    fun toOpenAiDefinition(): JsonObject {
        return buildJsonObject {
            put("type", JsonPrimitive("function"))
            put(
                "function",
                buildJsonObject {
                    put("name", JsonPrimitive(name))
                    put("description", JsonPrimitive(description))
                    put(
                        "parameters",
                        if (parameters is JsonObject) parameters else Json.parseToJsonElement(Json.encodeToString(parameters)).jsonObject
                    )
                }
            )
        }
    }
}

data class ToolObservation(
    val text: String,
    val truncated: Boolean = false,
    val diff: String? = null,
    val changedFiles: List<String>? = null,
)

data class ToolArgs(val raw: String) {
    private val json = Json { ignoreUnknownKeys = false }
    val root: JsonObject = runCatching { json.parseToJsonElement(raw).jsonObject }
        .getOrElse {
            throw IllegalArgumentException("tool args must be valid JSON object", it)
        }
}

data class ToolContext(
    val workspaceRoot: String,
    val agentName: String,
    val shell: IToolShellRunner,
    val git: IToolGitService,
    val currentTaskID: String? = null,
    val messenger: IToolAgentMessenger? = null,
    val imageGenerator: ImageGenerationToolService? = null,
)

interface ImageGenerationToolService {
    suspend fun generateImage(prompt: String, size: String, count: Int, outputPath: String, workspaceRoot: String): ToolObservation
}

private fun ensureNoUnknownArgs(root: JsonObject, toolName: String, allowed: Set<String>) {
    val unknown = root.keys - allowed
    if (unknown.isNotEmpty()) {
        throw IllegalArgumentException("tool args for '$toolName' contains unsupported fields: ${unknown.sorted().joinToString(", ")}")
    }
}

private fun getRequiredString(root: JsonObject, key: String, maxLength: Int, minLength: Int = 0): String {
    val value = root[key]?.jsonPrimitive ?: throw IllegalArgumentException("tool args missing required '$key'")
    val text = value.contentOrNull ?: throw IllegalArgumentException("tool args '$key' must be a string")
    if (text.length < minLength) {
        throw IllegalArgumentException("tool args '$key' must be at least $minLength characters")
    }
    if (text.length > maxLength) {
        throw IllegalArgumentException("tool args '$key' must be at most $maxLength characters")
    }
    return text
}

private fun getOptionalString(root: JsonObject, key: String, fallback: String, maxLength: Int, minLength: Int = 0): String {
    val value = root[key] ?: return fallback
    val text = value.jsonPrimitive?.contentOrNull ?: throw IllegalArgumentException("tool args '$key' must be a string")
    if (text.length < minLength) {
        throw IllegalArgumentException("tool args '$key' must be at least $minLength characters")
    }
    if (text.length > maxLength) {
        throw IllegalArgumentException("tool args '$key' must be at most $maxLength characters")
    }
    return text
}

private fun getInt(
    root: JsonObject,
    key: String,
    defaultValue: Int,
    minimum: Int = Int.MIN_VALUE,
    maximum: Int = Int.MAX_VALUE
): Int {
    val value = root[key] ?: return defaultValue
    val number = value.jsonPrimitive.intOrNull
        ?: throw IllegalArgumentException("tool args '$key' must be an integer")
        if (number !in minimum..maximum) {
            throw IllegalArgumentException("tool args '$key' must be in range [$minimum, $maximum]")
        }
    return number
}

private fun getOptionalBoolean(
    root: JsonObject,
    key: String,
    defaultValue: Boolean,
): Boolean {
    val value = root[key] ?: return defaultValue
    val boolValue = value.jsonPrimitive.booleanOrNull
        ?: throw IllegalArgumentException("tool args '$key' must be a boolean")
    return boolValue
}

private fun validateHttpUrl(raw: String): URL {
    val text = raw.trim()
    val url = runCatching { URL(text) }.getOrElse { throw IllegalArgumentException("URL must be http(s) with a host") }
    val scheme = url.protocol.lowercase()
    if ((scheme != "http" && scheme != "https") || url.host.isNullOrBlank()) {
        throw IllegalArgumentException("URL must be http(s) with a host")
    }
    return url
}

private fun maxCharacters(raw: Int?): Int = minOf(raw ?: 20_000, 100_000).coerceAtLeast(1)

private const val browserMaxSnapshotCharacters = 100_000
private const val migrationToolStringMaxLength = 1_024
private const val migrationToolUrlMaxLength = 2_048
private const val migrationToolPromptMaxLength = 100_000
private const val migrationToolPromptImageSizeMaxLength = 64
private const val migrationToolBrowserKeyMaxLength = 80
private const val migrationToolBrowserQueryMaxLength = 4_000

private data class BrowserPaths(
    val profile: String,
    val profileDir: String,
    val downloadsDir: String,
    val stateFile: String,
    val historyFile: String,
)

private data class BrowserInteractiveElement(
    val role: String? = null,
    val name: String? = null,
    val text: String? = null,
    val selector: String? = null,
    val tag: String? = null,
    val type: String? = null,
    val placeholder: String? = null,
    val disabled: Boolean? = null,
    val checked: Boolean? = null,
    val options: List<String>? = null,
)

private data class BrowserDownloadEntry(
    val filename: String? = null,
    val path: String? = null,
    val url: String? = null,
    val bytes: Int? = null,
)

private data class BrowserHistoryEntry(
    val ts: String? = null,
    val profile: String? = null,
    val action: String? = null,
    val url: String? = null,
    val title: String? = null,
    val screenshotPath: String? = null,
)

private data class BrowserProfileRuntimeMetadata(
    val activeBrowserMarkerPresent: Boolean,
    val profileLockMarkerPresent: Boolean,
) {
    val hasAnyMarker: Boolean
        get() = activeBrowserMarkerPresent || profileLockMarkerPresent
}

private data class BrowserProfileDeleteSummary(
    val removedProfileData: Boolean,
    val removedDownloads: Boolean,
    val removedState: Boolean,
    val removedHistoryEntries: Int,
    val keptHistoryEntries: Int,
    val runtimeBeforeDelete: BrowserProfileRuntimeMetadata,
)

private class BrowserProfileCommandLocks {
    private val lockedProfiles = HashSet<String>()
    private val waiters = HashMap<String, ArrayDeque<CompletableDeferred<Unit>>>()
    private val gate = Any()

    suspend fun acquire(key: String) {
        val waiter: CompletableDeferred<Unit>? = synchronized(gate) {
            if (lockedProfiles.contains(key).not()) {
                lockedProfiles.add(key)
                null
            } else {
                val queue = waiters.getOrPut(key) { ArrayDeque() }
                CompletableDeferred<Unit>().also { queue.addLast(it) }
            }
        }
        waiter?.await()
    }

    fun release(key: String) {
        val next = synchronized(gate) {
            val queue = waiters[key]
            if (queue == null || queue.isEmpty()) {
                lockedProfiles.remove(key)
                null
            } else {
                queue.removeFirst().also {
                    if (queue.isEmpty()) {
                        waiters.remove(key)
                    }
                }
            }
        }
        next?.complete(Unit)
    }
}

private enum class BrowserHistoryDirection(
    val actionName: String,
    val offset: Int,
    val missingEntryMessage: String,
) {
    Back("back", -1, "no previous browser history entry for this profile"),
    Forward("forward", 1, "no next browser history entry for this profile"),
}

private val browserProfileCommandLocks = BrowserProfileCommandLocks()

private fun browserProfileCommandLockKey(paths: BrowserPaths): String = paths.profileDir

private suspend fun <T> withBrowserProfileCommandLock(
    paths: BrowserPaths,
    operation: suspend () -> T
): T {
    val key = browserProfileCommandLockKey(paths)
    browserProfileCommandLocks.acquire(key)
    try {
        return operation()
    } finally {
        browserProfileCommandLocks.release(key)
    }
}

private object BrowserToolConfig {
    const val defaultProfile = "default"
    const val defaultChannel = "chromium"
    const val profileNameMaxLength = 64
    const val maxSnapshotCharacters = 100_000
    const val minWaitMillis = 0
    const val maxWaitMillis = 10_000
    const val defaultWaitMillis = 600

    private const val browserRootPath = ".intatis/browser"

    fun normalizedProfile(raw: String?): String {
        val trimmed = raw?.trim().orEmpty()
        val value = trimmed.ifBlank { defaultProfile }
        require(value.isNotBlank()) { "browser profile must not be blank" }
        require(value.length <= profileNameMaxLength) {
            "browser profile name must be at most $profileNameMaxLength characters"
        }
        require(value != "." && value != "..") {
            "browser profile must not be '.' or '..'"
        }
        require(value.all { it.isLetterOrDigit() || it == '-' || it == '_' || it == '.' }) {
            "browser profile must use only letters, numbers, '.', '-' or '_'"
        }
        return value
    }

    fun normalizedChannel(raw: String?): String {
        val value = raw?.trim()?.lowercase().orEmpty()
        return when (value) {
            "chrome", "chrome-beta", "chrome-dev", "chrome-canary",
            "msedge", "msedge-beta", "msedge-dev", "msedge-canary",
            "chromium" -> value
            else -> defaultChannel
        }
    }

    fun browserRoot(workspaceRoot: String): String =
        WorkspaceSecurity.resolveInWorkspace(workspaceRoot, browserRootPath)

    fun paths(profile: String, workspaceRoot: String): BrowserPaths {
        val safeProfile = normalizedProfile(profile)
        return BrowserPaths(
            profile = safeProfile,
            profileDir = profileDir(safeProfile, workspaceRoot),
            downloadsDir = downloadsDir(safeProfile, workspaceRoot),
            stateFile = stateFile(safeProfile, workspaceRoot),
            historyFile = historyFile(workspaceRoot),
        )
    }

    fun prepare(profile: String, workspaceRoot: String): BrowserPaths {
        val paths = paths(profile, workspaceRoot)
        File(paths.profileDir).mkdirs()
        File(paths.downloadsDir).mkdirs()
        File(paths.stateFile).parentFile?.mkdirs()
        File(paths.historyFile).parentFile?.mkdirs()
        return paths
    }

    fun clampedWaitMillis(raw: Int?): Int {
        val requested = raw ?: defaultWaitMillis
        return requested.coerceIn(minWaitMillis, maxWaitMillis)
    }

    fun profileDir(profile: String, workspaceRoot: String): String {
        val safeProfile = normalizedProfile(profile)
        return WorkspaceSecurity.resolveInWorkspace(
            workspaceRoot,
            "$browserRootPath/profiles/$safeProfile",
        )
    }

    fun downloadsDir(profile: String, workspaceRoot: String): String {
        val safeProfile = normalizedProfile(profile)
        return WorkspaceSecurity.resolveInWorkspace(
            workspaceRoot,
            "$browserRootPath/downloads/$safeProfile",
        )
    }

    fun stateFile(profile: String, workspaceRoot: String): String {
        val safeProfile = normalizedProfile(profile)
        return WorkspaceSecurity.resolveInWorkspace(
            workspaceRoot,
            "$browserRootPath/state/$safeProfile.json",
        )
    }

    fun historyFile(workspaceRoot: String): String =
        WorkspaceSecurity.resolveInWorkspace(workspaceRoot, "$browserRootPath/history.jsonl")
}

private fun schemaString(minLength: Int = 1): Map<String, Any> = mapOf(
    "type" to "string",
    "minLength" to minLength,
    "maxLength" to migrationToolStringMaxLength,
)

private fun schemaStringWithMax(minLength: Int = 1, maxLength: Int): Map<String, Any> = mapOf(
    "type" to "string",
    "minLength" to minLength,
    "maxLength" to maxLength,
)

private fun schemaInteger(minimum: Int, maximum: Int? = null): Map<String, Any> = buildMap {
    put("type", "integer")
    put("minimum", minimum)
    if (maximum != null) {
        put("maximum", maximum)
    }
}

private fun schemaObject(properties: Map<String, Any>, required: List<String>): Map<String, Any> =
    mapOf(
        "type" to "object",
        "properties" to properties,
        "required" to required,
        "additionalProperties" to false,
    )

private fun migrationToolParameters(name: String): Map<String, Any> = when (name) {
    "read_pdf" -> schemaObject(
        properties = mapOf(
            "path" to schemaString(),
            "pages" to schemaString(),
            "maxCharacters" to schemaInteger(1, 500_000),
        ),
        required = listOf("path"),
    )
    "edit_pdf_pages" -> schemaObject(
        properties = mapOf(
            "mode" to schemaString(),
            "inputPath" to schemaString(),
            "pages" to schemaString(),
            "outputPath" to schemaString(),
            "outputDir" to schemaString(),
            "outputPrefix" to schemaString(),
        ),
        required = listOf("mode", "inputPath"),
    )
    "reconstruct_document_image" -> schemaObject(
        properties = mapOf(
            "imagePath" to schemaString(),
            "outputPath" to schemaString(),
            "format" to schemaString(),
            "backend" to schemaString(),
        ),
        required = listOf("imagePath", "outputPath"),
    )
    "compile_latex" -> schemaObject(
        properties = mapOf(
            "inputPath" to schemaString(),
            "outputDir" to schemaString(),
            "engine" to schemaString(),
        ),
        required = listOf("inputPath"),
    )
    "generate_image" -> schemaObject(
        properties = mapOf(
            "prompt" to schemaStringWithMax(maxLength = migrationToolPromptMaxLength),
            "outputPath" to schemaString(),
            "size" to schemaStringWithMax(maxLength = migrationToolPromptImageSizeMaxLength),
            "count" to schemaInteger(1, 4),
        ),
        required = listOf("prompt", "outputPath"),
    )
    "web_fetch" -> schemaObject(
        properties = mapOf(
            "url" to schemaStringWithMax(maxLength = migrationToolUrlMaxLength),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = listOf("url"),
    )
    "browser_diagnostics" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
        ),
        required = emptyList(),
    )
    "browser_profiles" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "limit" to schemaInteger(1, 100),
            "includeProfileSize" to mapOf("type" to "boolean"),
        ),
        required = emptyList(),
    )
    "browser_profile_delete" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "confirmProfile" to schemaString(),
        ),
        required = listOf("profile", "confirmProfile"),
    )
    "browser_history" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "limit" to schemaInteger(1, 100),
        ),
        required = emptyList(),
    )
    "browser_navigate" -> schemaObject(
        properties = mapOf(
            "url" to schemaStringWithMax(maxLength = migrationToolUrlMaxLength),
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = listOf("url"),
    )
    "browser_snapshot" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_handoff" -> schemaObject(
        properties = mapOf(
            "url" to schemaStringWithMax(maxLength = migrationToolUrlMaxLength),
            "profile" to schemaString(),
            "channel" to schemaString(),
            "handoffSeconds" to schemaInteger(1, 600),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_reload" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "ignoreCache" to mapOf("type" to "boolean"),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_back", "browser_forward" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_click" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "exact" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_type" -> schemaObject(
        properties = mapOf(
            "value" to schemaString(),
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "clear" to mapOf("type" to "boolean"),
            "submit" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = listOf("value"),
    )
    "browser_submit" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "exact" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "timeoutMillis" to schemaInteger(1000, 30_000),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_select_option" -> schemaObject(
        properties = mapOf(
            "optionValue" to schemaString(),
            "optionLabel" to schemaString(),
            "optionIndex" to schemaInteger(0, 500),
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "exact" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_press_key" -> schemaObject(
        properties = mapOf(
            "key" to schemaStringWithMax(maxLength = migrationToolBrowserKeyMaxLength),
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "exact" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = listOf("key"),
    )
    "browser_scroll" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "exact" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "direction" to schemaString(),
            "amount" to schemaInteger(1, 10_000),
            "deltaX" to schemaInteger(-10_000, 10_000),
            "deltaY" to schemaInteger(-10_000, 10_000),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_wait" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "exact" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "state" to schemaString(),
            "timeoutMillis" to schemaInteger(1000, 30_000),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_screenshot" -> schemaObject(
        properties = mapOf(
            "outputPath" to schemaString(),
            "url" to schemaStringWithMax(maxLength = migrationToolUrlMaxLength),
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "fullPage" to mapOf("type" to "boolean"),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = listOf("outputPath"),
    )
    "browser_upload_file" -> schemaObject(
        properties = mapOf(
            "filePath" to schemaString(),
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "exact" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = listOf("filePath"),
    )
    "browser_download" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "selector" to schemaString(),
            "text" to schemaString(),
            "role" to schemaString(),
            "name" to schemaString(),
            "exact" to mapOf("type" to "boolean"),
            "nth" to schemaInteger(0, 100),
            "waitMillis" to schemaInteger(0, 10_000),
            "downloadTimeoutMillis" to schemaInteger(1_000, 60_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = emptyList(),
    )
    "browser_downloads" -> schemaObject(
        properties = mapOf(
            "profile" to schemaString(),
            "limit" to schemaInteger(1, 100),
        ),
        required = emptyList(),
    )
    "browser_search" -> schemaObject(
        properties = mapOf(
            "query" to schemaStringWithMax(maxLength = migrationToolBrowserQueryMaxLength),
            "engine" to schemaString(),
            "profile" to schemaString(),
            "channel" to schemaString(),
            "headless" to mapOf("type" to "boolean"),
            "waitMillis" to schemaInteger(0, 10_000),
            "maxCharacters" to schemaInteger(1, browserMaxSnapshotCharacters),
        ),
        required = listOf("query"),
    )
    else -> mapOf(
        "type" to "object",
        "properties" to emptyMap<String, Any>(),
        "required" to emptyList<String>(),
        "additionalProperties" to false,
    )
}

interface ITool {
    val descriptor: ToolDescriptor
    fun touchedPaths(args: ToolArgs): List<String>
    fun risksNetwork(args: ToolArgs): Boolean
    fun risksNetwork(args: ToolArgs, context: ToolContext): Boolean = risksNetwork(args)
    suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation
}

interface IToolAgentMessenger {
    suspend fun askAsync(from: String, to: String, question: String): String
    suspend fun sendMessageAsync(from: String, to: String, content: String): String
    suspend fun requestInformationAsync(from: String, to: String, question: String, taskID: String? = null): String
    suspend fun replyMessageAsync(
        from: String,
        to: String,
        answer: String,
        inReplyTo: String? = null,
        taskID: String? = null,
    ): String
    suspend fun requestDelegationAsync(from: String, objective: String, reason: String = "delegation requested"): String
    suspend fun delegateTaskAsync(
        from: String,
        to: String,
        objective: String,
        reason: String = "delegation requested",
        roleHint: String = "",
        expectedDeliverable: String = "",
        taskID: String? = null,
    ): String
    suspend fun spawnAgentAsync(name: String, path: String, model: String? = null, canCoordinate: Boolean = false): String
    suspend fun listAgentsAsync(): String
    suspend fun removeAgentAsync(name: String): String
}

class ToolRegistry(private val tools: Collection<ITool>) {
    private val lookup = tools.associateBy({ it.descriptor.name.lowercase() }, { it })

    fun tool(name: String): ITool? = lookup[name.lowercase()]
    val descriptors: List<ToolDescriptor>
        get() = tools.map { it.descriptor }

    fun add(toAdd: Collection<ITool>): ToolRegistry {
        val merged = mutableListOf<ITool>()
        merged.addAll(tools)
        merged.addAll(toAdd)
        return ToolRegistry(merged)
    }

    companion object {
        private val standardToolNames = setOf(
            "read_file",
            "list_files",
            "search_text",
            "write_file",
            "apply_patch",
            "run_shell",
            "git_status",
            "git_diff",
            "read_pdf",
            "edit_pdf_pages",
            "reconstruct_document_image",
            "compile_latex",
            "generate_image",
            "web_fetch",
            "browser_diagnostics",
            "browser_profiles",
            "browser_profile_delete",
            "browser_history",
            "browser_navigate",
            "browser_snapshot",
            "browser_handoff",
            "browser_click",
            "browser_reload",
            "browser_back",
            "browser_forward",
            "browser_type",
            "browser_submit",
            "browser_select_option",
            "browser_press_key",
            "browser_scroll",
            "browser_wait",
            "browser_screenshot",
            "browser_upload_file",
            "browser_download",
            "browser_downloads",
            "browser_search",
        )

        private val agentToolNames = setOf(
            "ask_agent",
            "send_message",
            "request_information",
            "reply_message",
            "request_delegation",
            "delegate_task",
            "spawn_agent",
            "list_agents",
            "remove_agent",
        )

        private fun standardTools(): List<ITool> = listOf(
            ReadFileTool(),
            ListFilesTool(),
            SearchTextTool(),
            WriteFileTool(),
            ApplyPatchTool(),
            RunShellTool(),
            GitStatusTool(),
            GitDiffTool(),
        ) + unsupportedMigrationTools()

        private fun standardWithAgentTools(): List<ITool> = standardTools() + listOf(
            AskAgentTool(),
            SendMessageTool(),
            RequestInformationTool(),
            ReplyMessageTool(),
            RequestDelegationTool(),
            DelegateTaskTool(),
            SpawnAgentTool(),
            ListAgentsTool(),
            RemoveAgentTool(),
        )

        private fun validateStandardTools(tools: List<ITool>): List<ITool> {
            val names = tools.map { it.descriptor.name }.toSet()
            check(names.containsAll(standardToolNames) && names.size >= standardToolNames.size) {
                "ToolRegistry.standard() incomplete migration set. missing=${standardToolNames - names}, extra=${names - standardToolNames}"
            }
            return tools
        }

        private fun validateAgentTools(tools: List<ITool>): List<ITool> {
            val names = tools.map { it.descriptor.name }.toSet()
            val expected = standardToolNames + agentToolNames
            check(names.containsAll(expected) && names.size >= expected.size) {
                "ToolRegistry.standardWithAgentTools() incomplete migration set. missing=${expected - names}, extra=${names - expected}"
            }
            return tools
        }

        fun standard(): ToolRegistry = ToolRegistry(
            validateStandardTools(standardTools())
        )

        fun standardWithAgentTools(messenger: IToolAgentMessenger?): ToolRegistry {
            return if (messenger == null) {
                ToolRegistry(validateStandardTools(standardTools()))
            } else {
                ToolRegistry(validateAgentTools(standardWithAgentTools()))
            }
        }

        private fun unsupportedMigrationTools(): List<ITool> = listOf(
            ReadPDFTool(),
            EditPDFPagesTool(),
            ReconstructDocumentImageTool(),
            CompileLaTeXTool(),
            GenerateImageTool(),
            WebFetchTool(),
            BrowserDiagnosticsTool(),
            BrowserProfilesTool(),
            BrowserProfileDeleteTool(),
            BrowserHistoryTool(),
            BrowserNavigateTool(),
            BrowserSnapshotTool(),
            BrowserHandoffTool(),
            UnsupportedTool("browser_click", "Click an element in the persistent browser profile by CSS selector, visible text, or accessibility role/name.", SideEffect.Exec, migrationToolParameters("browser_click")),
            BrowserReloadTool(),
            BrowserBackTool(),
            BrowserForwardTool(),
            UnsupportedTool("browser_type", "Type or fill text into an element in the persistent browser profile; avoid using this for passwords unless the user explicitly approves.", SideEffect.Exec, migrationToolParameters("browser_type")),
            UnsupportedTool("browser_submit", "Submit the current form in the persistent browser profile, optionally targeting a form control or submit button first.", SideEffect.Exec, migrationToolParameters("browser_submit")),
            UnsupportedTool("browser_select_option", "Select an option from a select/dropdown control in the persistent browser profile by value, label, or index.", SideEffect.Exec, migrationToolParameters("browser_select_option")),
            UnsupportedTool("browser_press_key", "Press a key or shortcut in the persistent browser profile, optionally targeting an element first.", SideEffect.Exec, migrationToolParameters("browser_press_key")),
            UnsupportedTool("browser_scroll", "Scroll the current persistent browser page or a targeted element by direction/amount or explicit pixel deltas.", SideEffect.Exec, migrationToolParameters("browser_scroll")),
            UnsupportedTool("browser_wait", "Wait in the persistent browser profile for text or an element state, or pause briefly for dynamic page updates.", SideEffect.Exec, migrationToolParameters("browser_wait")),
            UnsupportedTool("browser_screenshot", "Capture a PNG screenshot of the current or requested page in a persistent Chromium/Chrome/Edge browser profile.", SideEffect.Exec, migrationToolParameters("browser_screenshot")),
            UnsupportedTool("browser_upload_file", "Attach a workspace file to a file input in the persistent Chromium/Chrome/Edge browser profile.", SideEffect.Exec, migrationToolParameters("browser_upload_file")),
            UnsupportedTool("browser_download", "Click an element expected to start a download and save the file under the persistent browser profile downloads directory.", SideEffect.Exec, migrationToolParameters("browser_download")),
            UnsupportedTool("browser_downloads", "List downloaded file metadata for a persistent browser profile without reading file contents.", SideEffect.ReadOnly, migrationToolParameters("browser_downloads")),
            UnsupportedTool("browser_search", "Search the web in a persistent Chromium/Chrome/Edge browser profile and return visible result text and links.", SideEffect.Exec, migrationToolParameters("browser_search")),
        )
    }
}

private class BrowserDiagnosticsTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_diagnostics",
        description = "Report local Node, Playwright, browser channel, profile, download, state, and history paths for the persistent browser backend.",
        sideEffect = SideEffect.Exec,
        parameters = migrationToolParameters("browser_diagnostics"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_diagnostics", setOf("profile", "channel"))
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "browser_diagnostics", setOf("profile", "channel"))
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        val channel = BrowserToolConfig.normalizedChannel(
            getOptionalString(args.root, "channel", BrowserToolConfig.defaultChannel, migrationToolStringMaxLength)
        )
        val paths = BrowserToolConfig.paths(profile, context.workspaceRoot)
        val diagnostics = executeBrowserDiagnostics(context, channel)
        val lines = buildBrowserDiagnosticsOutput(paths, diagnostics)
        return ToolObservation(lines.joinToString("\n"))
    }

    private suspend fun executeBrowserDiagnostics(context: ToolContext, channel: String): BrowserDiagnosticsData {
        val checkedLocations = mutableListOf<String>()
        val (platform, arch) = runPlatformMetadata()
        val nodeVersion = runShellCommand(context, "node -v", checkedLocations)

        val playwrightCommand =
            "node -e \"try { const pkg = require('playwright/package.json'); console.log(pkg.version || ''); console.log(require.resolve('playwright/package.json')); } catch (e) { process.exit(1); }\""
        val playwrightOutput = runShellCommand(context, playwrightCommand, checkedLocations)
        var playwrightVersion: String? = null
        var playwrightResolvedFrom: String? = null
        var playwrightAvailable = false
        if (playwrightOutput != null) {
            val lines = playwrightOutput
                .split("\n")
                .map { it.trim() }
                .filter { it.isNotBlank() }

            if (lines.isNotEmpty()) {
                playwrightVersion = lines.elementAtOrNull(0)
                playwrightResolvedFrom = lines.elementAtOrNull(1)
                playwrightAvailable = true
            }
        }

        val nodeWebSocketCommand =
            "node -e \"try { require('ws'); console.log('yes'); process.exit(0); } catch (e) { process.exit(1); }\""
        val nodeWebSocketOutput = runShellCommand(context, nodeWebSocketCommand, checkedLocations)
        val nodeWebSocketAvailable = nodeWebSocketOutput?.trim()?.equals("yes", ignoreCase = true) == true

        val appProbes = mutableMapOf<String, Boolean>()
        val cdpExecutable = resolveBrowserExecutable(context, channel, checkedLocations, appProbes)

        return BrowserDiagnosticsData(
            nodeVersion = nodeVersion,
            platform = platform,
            arch = arch,
            channel = channel,
            browserApps = appProbes,
            playwrightAvailable = playwrightAvailable,
            playwrightVersion = playwrightVersion,
            playwrightResolvedFrom = playwrightResolvedFrom,
            nodeWebSocketAvailable = nodeWebSocketAvailable,
            cdpAvailable = cdpExecutable != null,
            cdpExecutable = cdpExecutable,
            checkedLocations = checkedLocations,
        )
    }

    private fun runPlatformMetadata(): Pair<String, String> {
        val os = System.getProperty("os.name").lowercase()
        val platform = when {
            os.contains("win") -> "windows"
            os.contains("mac") || os.contains("darwin") -> "darwin"
            os.contains("linux") -> "linux"
            else -> "unknown"
        }
        val arch = System.getProperty("os.arch").lowercase()
        return platform to arch
    }

    private suspend fun runShellCommand(
        context: ToolContext,
        command: String,
        checkedLocations: MutableList<String>,
    ): String? {
        checkedLocations.add(command)
        return try {
            val result = context.shell.runAsync(command, context.workspaceRoot)
            if (result.exitCode != 0) {
                null
            } else {
                result.stdOut.trim()
            }
        } catch (_: Exception) {
            null
        }
    }

    private fun commandForProbe(name: String): String = if (System.getProperty("os.name").lowercase().contains("win")) {
        "where $name"
    } else {
        "command -v $name"
    }

    private suspend fun resolveBrowserExecutable(
        context: ToolContext,
        channel: String,
        checkedLocations: MutableList<String>,
        appProbes: MutableMap<String, Boolean>,
    ): String? {
        val configuredExecutable = System.getenv("INTATIS_BROWSER_EXECUTABLE")
        checkedLocations.add("env INTATIS_BROWSER_EXECUTABLE")
        if (!configuredExecutable.isNullOrBlank() && File(configuredExecutable).exists()) {
            return configuredExecutable
        }

        for ((candidate, key) in browserExecutableCandidates(channel)) {
            val output = runShellCommand(context, commandForProbe(candidate), checkedLocations)
            val present = !output.isNullOrBlank()
            appProbes[key] = (appProbes[key] == true) || present
            if (present) {
                return output.trim().takeIf { it.isNotBlank() }
            }
        }
        return null
    }

    private fun browserExecutableCandidates(channel: String): List<Pair<String, String>> = when (channel) {
        "chrome", "chrome-beta", "chrome-dev", "chrome-canary" -> listOf(
            "chrome" to "chrome",
            "google-chrome" to "chrome",
            "chromium" to "chromium",
            "msedge" to "msedge",
            "microsoft-edge" to "msedge",
        )
        "msedge", "msedge-beta", "msedge-dev", "msedge-canary" -> listOf(
            "msedge" to "msedge",
            "microsoft-edge" to "msedge",
            "chrome" to "chrome",
            "google-chrome" to "chrome",
            "chromium" to "chromium",
        )
        else -> listOf(
            "chromium" to "chromium",
            "chrome" to "chrome",
            "google-chrome" to "chrome",
            "msedge" to "msedge",
            "microsoft-edge" to "msedge",
        )
    }

    private fun buildBrowserDiagnosticsOutput(
        paths: BrowserPaths,
        result: BrowserDiagnosticsData,
    ): List<String> {
        val lines = mutableListOf<String>(
            "browser action: diagnostics",
            "node: ${result.nodeVersion ?: "unknown"}",
            "platform: ${(result.platform ?: "unknown")}/${(result.arch ?: "unknown")}",
            "channel: ${result.channel}",
            "profile: ${paths.profile}",
            "playwright available: ${if (result.playwrightAvailable) "yes" else "no"}",
        )

        if (!result.playwrightVersion.isNullOrBlank()) {
            lines.add("playwright version: ${result.playwrightVersion}")
        }
        if (!result.playwrightResolvedFrom.isNullOrBlank()) {
            lines.add("playwright resolved from: ${result.playwrightResolvedFrom}")
        }

        lines.add("node WebSocket available: ${if (result.nodeWebSocketAvailable) "yes" else "no"}")
        lines.add("cdp fallback available: ${if (result.cdpAvailable) "yes" else "no"}")
        if (!result.cdpExecutable.isNullOrBlank()) {
            lines.add("cdp executable: ${result.cdpExecutable}")
        }

        lines.add("profile dir: ${paths.profileDir}")
        lines.add("downloads dir: ${paths.downloadsDir}")
        lines.add("state file: ${paths.stateFile}")
        lines.add("history file: ${paths.historyFile}")

        if (result.browserApps.isNotEmpty()) {
            lines.add("")
            lines.add("installed app probes:")
            result.browserApps.keys.toList().sorted().forEach { key ->
                lines.add("- $key: ${if (result.browserApps[key] == true) "yes" else "no"}")
            }
        }

        if (result.checkedLocations.isNotEmpty()) {
            lines.add("")
            lines.add("checked Playwright locations:")
            result.checkedLocations.forEach { item ->
                lines.add("- $item")
            }
        }

        return lines
    }
}

private data class BrowserDiagnosticsData(
    val nodeVersion: String?,
    val platform: String?,
    val arch: String?,
    val channel: String,
    val browserApps: Map<String, Boolean>,
    val playwrightAvailable: Boolean,
    val playwrightVersion: String?,
    val playwrightResolvedFrom: String?,
    val nodeWebSocketAvailable: Boolean,
    val cdpAvailable: Boolean,
    val cdpExecutable: String?,
    val checkedLocations: List<String>,
)

private class BrowserProfilesTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_profiles",
        description = "List persistent browser profiles and safe metadata without reading cookies, localstorage, or browser profile databases.",
        sideEffect = SideEffect.ReadOnly,
        parameters = migrationToolParameters("browser_profiles"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_profiles", setOf("profile", "limit", "includeProfileSize"))
        val requestedProfile = getOptionalString(args.root, "profile", "", migrationToolStringMaxLength, minLength = 1)
            .trim()
            .takeIf { it.isNotBlank() }
            ?.let { BrowserToolConfig.normalizedProfile(it) }

        return if (requestedProfile == null) {
            listOf(
                ".intatis/browser/profiles",
                ".intatis/browser/downloads",
                ".intatis/browser/state",
                ".intatis/browser/history.jsonl",
            )
        } else {
            listOf(
                ".intatis/browser/profiles/$requestedProfile",
                ".intatis/browser/downloads/$requestedProfile",
                ".intatis/browser/state/$requestedProfile.json",
                ".intatis/browser/history.jsonl",
            )
        }
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "browser_profiles", setOf("profile", "limit", "includeProfileSize"))
        val requestedProfile = getOptionalString(args.root, "profile", "", migrationToolStringMaxLength, minLength = 1)
            .trim()
            .takeIf { it.isNotBlank() }
            ?.let { BrowserToolConfig.normalizedProfile(it) }
        val limit = getInt(args.root, "limit", 100, minimum = 1, maximum = 100)
        val includeProfileSize = getOptionalBoolean(args.root, "includeProfileSize", false)

        val historySummaries = readBrowserHistorySummaries(context.workspaceRoot)
        val discoveredProfiles = discoverBrowserProfiles(
            workspaceRoot = context.workspaceRoot,
            requestedProfile = requestedProfile,
            historySummaries = historySummaries,
        )
        val selectedProfiles = discoveredProfiles.take(limit)
        val inventories = selectedProfiles.map { profile ->
            collectBrowserProfileInventory(
                workspaceRoot = context.workspaceRoot,
                profile = profile,
                includeProfileSize = includeProfileSize,
                historySummaries = historySummaries,
            )
        }

        val lines = buildBrowserProfilesOutput(
            requestedProfile = requestedProfile,
            includeProfileSize = includeProfileSize,
            limit = limit,
            totalProfiles = discoveredProfiles.size,
            inventories = inventories,
        )
        return ToolObservation(lines.joinToString("\n"))
    }
}

private class BrowserProfileDeleteTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_profile_delete",
        description = "Delete one workspace-scoped persistent browser profile, including its state, downloads, and Intatis history metadata. Requires confirmProfile to match profile.",
        sideEffect = SideEffect.Destructive,
        parameters = migrationToolParameters("browser_profile_delete"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_profile_delete", setOf("profile", "confirmProfile"))
        val profile = BrowserToolConfig.normalizedProfile(
            getRequiredString(args.root, "profile", migrationToolStringMaxLength, minLength = 1)
        )
        val confirmProfile = BrowserToolConfig.normalizedProfile(
            getRequiredString(args.root, "confirmProfile", migrationToolStringMaxLength, minLength = 1)
        )
        if (profile != confirmProfile) {
            throw IllegalArgumentException("confirmProfile must match profile exactly")
        }

        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_profile_delete", setOf("profile", "confirmProfile"))
        val profile = BrowserToolConfig.normalizedProfile(
            getRequiredString(args.root, "profile", migrationToolStringMaxLength, minLength = 1)
        )
        val confirmProfile = BrowserToolConfig.normalizedProfile(
            getRequiredString(args.root, "confirmProfile", migrationToolStringMaxLength, minLength = 1)
        )
        if (profile != confirmProfile) {
            throw IllegalArgumentException("confirmProfile must match profile exactly")
        }

        val paths = BrowserToolConfig.paths(profile, context.workspaceRoot)
        val runtimeMetadata = describeBrowserProfileRuntimeMetadata(paths.profileDir)
        if (runtimeMetadata.hasAnyMarker) {
            return@withContext ToolObservation(buildBrowserProfileDeleteBlockedOutput(profile, runtimeMetadata))
        }

        val summary = deleteBrowserProfileData(
            profile = profile,
            profileDir = paths.profileDir,
            downloadsDir = paths.downloadsDir,
            stateFile = paths.stateFile,
            historyFile = paths.historyFile,
            runtimeMetadata = runtimeMetadata,
        )
        ToolObservation(buildBrowserProfileDeleteOutput(profile = profile, summary = summary).joinToString("\n"))
    }
}

private class BrowserHistoryTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_history",
        description = "Read recent Intatis browser history metadata without exposing cookies, local storage, or credential files.",
        sideEffect = SideEffect.ReadOnly,
        parameters = migrationToolParameters("browser_history"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_history", setOf("profile", "limit"))
        return listOf(".intatis/browser/history.jsonl")
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_history", setOf("profile", "limit"))
        val requestedProfile = getOptionalString(args.root, "profile", "", migrationToolStringMaxLength, minLength = 1)
            .trim()
            .takeIf { it.isNotBlank() }
            ?.let { BrowserToolConfig.normalizedProfile(it) }
        val limit = getInt(args.root, "limit", 100, minimum = 1, maximum = 100)

        val (entries, matchedEntries) = readBrowserHistoryEntries(context.workspaceRoot, requestedProfile, limit)
        val lines = buildBrowserHistoryOutput(requestedProfile, limit, matchedEntries, entries)
        ToolObservation(lines.joinToString("\n"))
    }
}

private data class BrowserNavigateLink(
    val text: String? = null,
    val url: String? = null,
)

private data class BrowserNavigateResult(
    val title: String? = null,
    val finalUrl: String? = null,
    val text: String? = null,
    val links: List<BrowserNavigateLink> = emptyList(),
)

private data class BrowserSnapshotResult(
    val title: String? = null,
    val finalUrl: String? = null,
    val text: String? = null,
    val links: List<BrowserNavigateLink> = emptyList(),
    val elements: List<BrowserInteractiveElement> = emptyList(),
)

private class BrowserNavigateTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_navigate",
        description = "Open an HTTP(S) URL in a persistent Chromium/Chrome/Edge Playwright profile and return page text plus links.",
        sideEffect = SideEffect.Exec,
        parameters = migrationToolParameters("browser_navigate"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_navigate", setOf("url", "profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val requestedUrl = getRequiredString(args.root, "url", migrationToolUrlMaxLength, minLength = 1).trim()
        validateHttpUrl(requestedUrl)
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )

        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_navigate", setOf("url", "profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val requestedUrl = getRequiredString(args.root, "url", migrationToolUrlMaxLength, minLength = 1).trim()
        validateHttpUrl(requestedUrl)
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        val channel = BrowserToolConfig.normalizedChannel(
            getOptionalString(args.root, "channel", BrowserToolConfig.defaultChannel, migrationToolStringMaxLength)
        )
        val headless = getOptionalBoolean(args.root, "headless", true)
        val waitMillis = BrowserToolConfig.clampedWaitMillis(
            getInt(args.root, "waitMillis", BrowserToolConfig.defaultWaitMillis, minimum = BrowserToolConfig.minWaitMillis, maximum = BrowserToolConfig.maxWaitMillis)
        )
        val maxCharacters = getInt(args.root, "maxCharacters", BrowserToolConfig.maxSnapshotCharacters, minimum = 1, maximum = 100_000)

        val paths = BrowserToolConfig.prepare(profile, context.workspaceRoot)
        val command = buildBrowserNavigateCommand(
            requestedUrl,
            paths.profileDir,
            paths.stateFile,
            channel,
            headless,
            waitMillis,
            maxCharacters,
        )
        val result = withBrowserProfileCommandLock(paths) {
            context.shell.runAsync(command, context.workspaceRoot)
        }

        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = stderr.ifBlank { stdout }.ifBlank { "node command failed" }
            return@withContext ToolObservation("browser_navigate failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserNavigateOutput(result.stdOut)
        val finalUrl = parsed.finalUrl?.ifBlank { null } ?: requestedUrl
        appendBrowserHistoryEntry(paths.historyFile, profile, "navigate", finalUrl, parsed.title)

        val output = buildBrowserNavigateOutput(
            profile = profile,
            requestedUrl = requestedUrl,
            finalUrl = finalUrl,
            result = parsed,
            maxCharacters = maxCharacters,
        )
        ToolObservation(output)
    }
}

private class BrowserReloadTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_reload",
        description = "Reload the current page in a persistent Chromium/Chrome/Edge browser profile and return page text plus links.",
        sideEffect = SideEffect.Exec,
        parameters = migrationToolParameters("browser_reload"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_reload",
            setOf("profile", "channel", "headless", "ignoreCache", "waitMillis", "maxCharacters"),
        )
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_reload",
            setOf("profile", "channel", "headless", "ignoreCache", "waitMillis", "maxCharacters"),
        )
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        val channel = BrowserToolConfig.normalizedChannel(
            getOptionalString(args.root, "channel", BrowserToolConfig.defaultChannel, migrationToolStringMaxLength)
        )
        val headless = getOptionalBoolean(args.root, "headless", true)
        val ignoreCache = getOptionalBoolean(args.root, "ignoreCache", false)
        val waitMillis = BrowserToolConfig.clampedWaitMillis(
            getInt(args.root, "waitMillis", BrowserToolConfig.defaultWaitMillis, minimum = BrowserToolConfig.minWaitMillis, maximum = BrowserToolConfig.maxWaitMillis)
        )
        val maxCharacters = getInt(args.root, "maxCharacters", BrowserToolConfig.maxSnapshotCharacters, minimum = 1, maximum = 100_000)

        val reloadTargetUrl = readBrowserHistoryEntries(context.workspaceRoot, profile, 1).firstOrNull()?.url?.trim()?.let {
            runCatching { validateHttpUrl(it); it }.getOrNull()
        }

        val paths = BrowserToolConfig.prepare(profile, context.workspaceRoot)
        val command = buildBrowserReloadCommand(
            paths.profileDir,
            paths.stateFile,
            channel,
            headless,
            ignoreCache,
            reloadTargetUrl,
            waitMillis,
            maxCharacters,
        )
        val result = withBrowserProfileCommandLock(paths) {
            context.shell.runAsync(command, context.workspaceRoot)
        }

        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = stderr.ifBlank { stdout }.ifBlank { "node command failed" }
            return@withContext ToolObservation("browser_reload failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut)
        val finalUrl = parsed.finalUrl?.ifBlank { null } ?: "unknown"
        appendBrowserHistoryEntry(paths.historyFile, profile, "reload", finalUrl, parsed.title)

        val output = buildBrowserReloadOutput(profile, parsed, maxCharacters)
        ToolObservation(output)
    }
}

private class BrowserBackTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_back",
        description = "Go back to the previous URL recorded for a persistent Chromium/Chrome/Edge browser profile.",
        sideEffect = SideEffect.Exec,
        parameters = migrationToolParameters("browser_back"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_back", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        executeBrowserHistoryNavigation(args, context, BrowserHistoryDirection.Back)
    }
}

private class BrowserForwardTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_forward",
        description = "Go forward to the next URL recorded for a persistent Chromium/Chrome/Edge browser profile.",
        sideEffect = SideEffect.Exec,
        parameters = migrationToolParameters("browser_forward"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_forward", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        executeBrowserHistoryNavigation(args, context, BrowserHistoryDirection.Forward)
    }
}

private class BrowserSnapshotTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_snapshot",
        description = "Reopen the current persistent browser profile and return the current page text plus links.",
        sideEffect = SideEffect.Exec,
        parameters = migrationToolParameters("browser_snapshot"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_snapshot", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_snapshot", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        val channel = BrowserToolConfig.normalizedChannel(
            getOptionalString(args.root, "channel", BrowserToolConfig.defaultChannel, migrationToolStringMaxLength)
        )
        val headless = getOptionalBoolean(args.root, "headless", true)
        val waitMillis = BrowserToolConfig.clampedWaitMillis(
            getInt(args.root, "waitMillis", BrowserToolConfig.defaultWaitMillis, minimum = BrowserToolConfig.minWaitMillis, maximum = BrowserToolConfig.maxWaitMillis)
        )
        val maxCharacters = getInt(args.root, "maxCharacters", BrowserToolConfig.maxSnapshotCharacters, minimum = 1, maximum = 100_000)

        val paths = BrowserToolConfig.prepare(profile, context.workspaceRoot)
        val command = buildBrowserSnapshotCommand(
            paths.profileDir,
            paths.stateFile,
            channel,
            headless,
            waitMillis,
            maxCharacters,
        )
        val result = withBrowserProfileCommandLock(paths) {
            context.shell.runAsync(command, context.workspaceRoot)
        }

        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = stderr.ifBlank { stdout }.ifBlank { "node command failed" }
            return@withContext ToolObservation("browser_snapshot failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut)
        val finalUrl = parsed.finalUrl?.ifBlank { "unknown" } ?: "unknown"
        appendBrowserHistoryEntry(paths.historyFile, profile, "snapshot", finalUrl, parsed.title)

        val output = buildBrowserSnapshotOutput(profile, parsed, maxCharacters)
        ToolObservation(output)
    }
}

private class BrowserHandoffTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "browser_handoff",
        description = "Open a headed persistent Chromium/Chrome/Edge browser profile for user login or manual interaction, then return the resulting page snapshot.",
        sideEffect = SideEffect.Exec,
        parameters = migrationToolParameters("browser_handoff"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_handoff", setOf("url", "profile", "channel", "handoffSeconds", "maxCharacters"))
        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_handoff", setOf("url", "profile", "channel", "handoffSeconds", "maxCharacters"))

        val requestedUrl = getOptionalString(args.root, "url", "", migrationToolUrlMaxLength, minLength = 1)
            .trim()
            .takeIf { it.isNotBlank() }

        requestedUrl?.let { validateHttpUrl(it) }

        val profile = BrowserToolConfig.normalizedProfile(
            getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
        )
        val channel = BrowserToolConfig.normalizedChannel(
            getOptionalString(args.root, "channel", BrowserToolConfig.defaultChannel, migrationToolStringMaxLength)
        )
        val handoffSeconds = getInt(args.root, "handoffSeconds", 60, minimum = 1, maximum = 600)
        val maxCharacters = getInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val paths = BrowserToolConfig.prepare(profile, context.workspaceRoot)
        val command = buildBrowserHandoffCommand(
            requestedUrl ?: "",
            paths.profileDir,
            paths.stateFile,
            channel,
            handoffSeconds,
            maxCharacters,
        )

        val result = withBrowserProfileCommandLock(paths) {
            context.shell.runAsync(command, context.workspaceRoot)
        }

        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = stderr.ifBlank { stdout }.ifBlank { "node command failed" }
            return@withContext ToolObservation("browser_handoff failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut)
        val finalUrl = parsed.finalUrl?.ifBlank { "unknown" } ?: requestedUrl ?: "unknown"
        appendBrowserHistoryEntry(paths.historyFile, profile, "handoff", finalUrl, parsed.title)

        val output = buildBrowserHandoffOutput(profile, requestedUrl, parsed, maxCharacters)
        ToolObservation(output)
    }
}

private data class BrowserProfileHistorySummary(
    var entries: Int = 0,
    var latestTimestamp: String? = null,
    var latestAction: String? = null,
    var latestUrl: String? = null,
)

private data class BrowserProfileFileMetadata(
    val exists: Boolean,
    val sizeBytes: Long?,
    val lastModifiedMs: Long?,
)

private data class BrowserProfileDirectoryMetadata(
    val exists: Boolean,
    val fileCount: Int,
    val sizeBytes: Long?,
    val lastModifiedMs: Long?,
)

private data class BrowserProfileInventory(
    val profile: String,
    val profileDir: String,
    val profileDirectory: BrowserProfileDirectoryMetadata,
    val stateFile: String,
    val stateFileMetadata: BrowserProfileFileMetadata,
    val downloadsDir: String,
    val downloadsMetadata: BrowserProfileDirectoryMetadata,
    val runtimeMetadata: BrowserProfileRuntimeMetadata,
    val historySummary: BrowserProfileHistorySummary?,
)

private fun discoverBrowserProfiles(
    workspaceRoot: String,
    requestedProfile: String?,
    historySummaries: Map<String, BrowserProfileHistorySummary>,
): List<String> {
    if (requestedProfile != null) {
        return listOf(requestedProfile)
    }

    val profiles = linkedSetOf<String>()
    val profilesDir = WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/profiles")
    val downloadsDir = WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/downloads")
    val stateDir = WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/state")

    collectDirectoryProfileNames(profilesDir, profiles)
    collectDirectoryProfileNames(downloadsDir, profiles)
    collectStateProfileNames(stateDir, profiles)

    for (profile in historySummaries.keys) {
        if (isValidBrowserProfileName(profile)) {
            profiles.add(profile)
        }
    }

    return profiles.sorted()
}

private fun collectDirectoryProfileNames(directoryPath: String, profiles: MutableSet<String>) {
    val dir = File(directoryPath)
    if (!dir.isDirectory) {
        return
    }

    val entries = dir.listFiles() ?: return
    for (entry in entries) {
        if (!entry.isDirectory) {
            continue
        }
        if (isValidBrowserProfileName(entry.name)) {
            profiles.add(entry.name)
        }
    }
}

private fun collectStateProfileNames(directoryPath: String, profiles: MutableSet<String>) {
    val dir = File(directoryPath)
    if (!dir.isDirectory) {
        return
    }

    val entries = dir.listFiles() ?: return
    for (entry in entries) {
        if (!entry.isFile || !entry.name.lowercase().endsWith(".json")) {
            continue
        }
        val profile = entry.name.substring(0, entry.name.length - ".json".length)
        if (isValidBrowserProfileName(profile)) {
            profiles.add(profile)
        }
    }
}

private fun collectBrowserProfileInventory(
    workspaceRoot: String,
    profile: String,
    includeProfileSize: Boolean,
    historySummaries: Map<String, BrowserProfileHistorySummary>,
): BrowserProfileInventory {
    val profileDir = WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/profiles/$profile")
    val downloadsDir = WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/downloads/$profile")
    val stateFile = WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/state/$profile.json")
    val runtimeMetadata = describeBrowserProfileRuntimeMetadata(profileDir)

    return BrowserProfileInventory(
        profile = profile,
        profileDir = profileDir,
        profileDirectory = describeDirectoryMetadata(profileDir, includeProfileSize),
        stateFile = stateFile,
        stateFileMetadata = describeFileMetadata(stateFile),
        downloadsDir = downloadsDir,
        downloadsMetadata = describeDirectoryMetadata(downloadsDir, true),
        runtimeMetadata = runtimeMetadata,
        historySummary = historySummaries[profile],
    )
}

private fun readBrowserHistorySummaries(workspaceRoot: String): Map<String, BrowserProfileHistorySummary> {
    val historyFile = File(WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/history.jsonl"))
    if (!historyFile.isFile) {
        return emptyMap()
    }

    val summaries = linkedMapOf<String, BrowserProfileHistorySummary>()
    try {
        historyFile.forEachLine { rawLine ->
            val line = rawLine.trim()
            if (line.isBlank()) return@forEachLine

            val lineJson = runCatching { Json.parseToJsonElement(line).jsonObject }.getOrNull() ?: return@forEachLine
            val profile = lineJson["profile"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: return@forEachLine
            if (!isValidBrowserProfileName(profile)) {
                return@forEachLine
            }

            val summary = summaries.getOrPut(profile) { BrowserProfileHistorySummary() }
            summary.entries += 1
            summary.latestTimestamp = lineJson["ts"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: summary.latestTimestamp
            summary.latestAction = lineJson["action"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: summary.latestAction
            summary.latestUrl = lineJson["url"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: summary.latestUrl
        }
    } catch (_: Exception) {
        return emptyMap()
    }

    return summaries
}

private fun buildBrowserProfilesOutput(
    requestedProfile: String?,
    includeProfileSize: Boolean,
    limit: Int,
    totalProfiles: Int,
    inventories: List<BrowserProfileInventory>,
): List<String> {
    val lines = mutableListOf(
        "browser action: profile inventory",
        "requested profile: ${requestedProfile ?: "all"}",
        "include profile size: ${if (includeProfileSize) "yes" else "no"}",
    )

    if (totalProfiles == 0) {
        lines += "profiles found: 0"
        lines += "(no profiles found)"
        return lines
    }

    if (requestedProfile == null && totalProfiles > inventories.size) {
        lines += "showing first ${inventories.size} of $totalProfiles profiles (limit=$limit)"
    } else {
        lines += "profiles found: ${inventories.size}"
    }

    for ((index, profile) in inventories.withIndex()) {
        val runtime = profile.runtimeMetadata
        val history = profile.historySummary
        lines += ""
        lines += "profile #${index + 1}: ${profile.profile}"
        lines += "  profile dir: ${profile.profileDir}"
        lines += "  exists: ${if (profile.profileDirectory.exists) "yes" else "no"}"
        lines += "  files: ${profile.profileDirectory.fileCount}"
        lines += "  bytes: ${profile.profileDirectory.sizeBytes?.toString() ?: "n/a"}"
        lines += "  last modified: ${formatLastModified(profile.profileDirectory.lastModifiedMs)}"
        lines += "  runtime markers: DevToolsActivePort=${if (runtime.activeBrowserMarkerPresent) "yes" else "no"}, Singleton*=${if (runtime.profileLockMarkerPresent) "yes" else "no"}"
        lines += "  state file: ${profile.stateFile}"
        lines += "  state exists: ${if (profile.stateFileMetadata.exists) "yes" else "no"}"
        if (profile.stateFileMetadata.exists) {
            lines += "  state bytes: ${profile.stateFileMetadata.sizeBytes?.toString() ?: "n/a"}"
            lines += "  state last modified: ${formatLastModified(profile.stateFileMetadata.lastModifiedMs)}"
        }
        lines += "  downloads dir: ${profile.downloadsDir}"
        lines += "  downloads exists: ${if (profile.downloadsMetadata.exists) "yes" else "no"}"
        lines += "  downloads files: ${profile.downloadsMetadata.fileCount}"
        lines += "  downloads bytes: ${profile.downloadsMetadata.sizeBytes ?: 0}"
        lines += "  downloads last modified: ${formatLastModified(profile.downloadsMetadata.lastModifiedMs)}"
        lines += "  history entries: ${history?.entries ?: 0}"
        if (history != null && history.entries > 0) {
            history.latestTimestamp?.let { lines += "  history latest ts: $it" }
            history.latestAction?.let { lines += "  history latest action: $it" }
            history.latestUrl?.let { lines += "  history latest url: $it" }
        }
    }

    return lines
}

private fun describeBrowserProfileRuntimeMetadata(profileDirPath: String): BrowserProfileRuntimeMetadata {
    val profileDir = File(profileDirPath)
    if (!profileDir.isDirectory) {
        return BrowserProfileRuntimeMetadata(
            activeBrowserMarkerPresent = false,
            profileLockMarkerPresent = false,
        )
    }
    val activeBrowserMarker = File(profileDir, "DevToolsActivePort").exists()
    val singletonMarker = profileDir.listFiles()?.any { it.name.startsWith("Singleton") } == true
    return BrowserProfileRuntimeMetadata(
        activeBrowserMarkerPresent = activeBrowserMarker,
        profileLockMarkerPresent = singletonMarker,
    )
}

private fun isValidBrowserProfileName(profile: String): Boolean {
    val trimmed = profile.trim()
    return trimmed.isNotBlank() &&
        trimmed != "." &&
        trimmed != ".." &&
        trimmed.length <= 64 &&
        trimmed.all { it.isLetterOrDigit() || it == '-' || it == '_' || it == '.' }
}

private fun describeDirectoryMetadata(path: String, includeSize: Boolean): BrowserProfileDirectoryMetadata {
    val directory = File(path)
    if (!directory.isDirectory) {
        return BrowserProfileDirectoryMetadata(
            exists = false,
            fileCount = 0,
            sizeBytes = if (includeSize) 0L else null,
            lastModifiedMs = null,
        )
    }

    var fileCount = 0
    var sizeBytes = 0L
    val stack = mutableListOf(directory)
    while (stack.isNotEmpty()) {
        val current = stack.removeAt(stack.lastIndex)
        val children = current.listFiles() ?: continue
        for (child in children) {
            if (child.isDirectory) {
                stack.add(child)
            } else if (child.isFile) {
                fileCount++
                if (includeSize) {
                    sizeBytes += child.length().coerceAtLeast(0)
                }
            }
        }
    }

    return BrowserProfileDirectoryMetadata(
        exists = true,
        fileCount = fileCount,
        sizeBytes = if (includeSize) sizeBytes else null,
        lastModifiedMs = directory.lastModified().takeIf { it > 0 },
    )
}

private fun describeFileMetadata(path: String): BrowserProfileFileMetadata {
    val file = File(path)
    if (!file.isFile) {
        return BrowserProfileFileMetadata(
            exists = false,
            sizeBytes = null,
            lastModifiedMs = null,
        )
    }

    return BrowserProfileFileMetadata(
        exists = true,
        sizeBytes = file.length().coerceAtLeast(0),
        lastModifiedMs = file.lastModified().takeIf { it > 0 },
    )
}

private fun formatLastModified(lastModifiedMs: Long?): String = lastModifiedMs?.toString() ?: "n/a"

private fun deleteBrowserProfileData(
    profile: String,
    profileDir: String,
    downloadsDir: String,
    stateFile: String,
    historyFile: String,
    runtimeMetadata: BrowserProfileRuntimeMetadata,
): BrowserProfileDeleteSummary {
    val removedProfileData = deleteFileOrDirectory(profileDir)
    val removedDownloads = deleteFileOrDirectory(downloadsDir)
    val removedState = deleteFileOrDirectory(stateFile)
    val (removedHistoryEntries, keptHistoryEntries) = pruneBrowserHistoryEntries(historyFile, profile)

    return BrowserProfileDeleteSummary(
        removedProfileData = removedProfileData,
        removedDownloads = removedDownloads,
        removedState = removedState,
        removedHistoryEntries = removedHistoryEntries,
        keptHistoryEntries = keptHistoryEntries,
        runtimeBeforeDelete = runtimeMetadata,
    )
}

private fun deleteFileOrDirectory(path: String): Boolean {
    val file = File(path)
    if (!file.exists()) {
        return false
    }

    return if (file.isDirectory) {
        if (!file.deleteRecursively()) {
            throw IllegalArgumentException("failed to delete directory: $path")
        }
        true
    } else if (file.isFile) {
        if (!file.delete()) {
            throw IllegalArgumentException("failed to delete file: $path")
        }
        true
    } else {
        false
    }
}

private fun pruneBrowserHistoryEntries(
    historyFilePath: String,
    profile: String,
): Pair<Int, Int> {
    val historyFile = File(historyFilePath)
    if (!historyFile.isFile) {
        return 0 to 0
    }

    val keepLines = mutableListOf<String>()
    var removedHistoryEntries = 0
    var keptHistoryEntries = 0

    for (line in historyFile.readLines()) {
        val trimmed = line.trim()
        if (trimmed.isBlank()) {
            continue
        }

        val lineProfile = runCatching {
            Json.parseToJsonElement(trimmed).jsonObject["profile"]?.jsonPrimitive?.content?.trim()
        }.getOrNull()

        if (lineProfile == profile) {
            removedHistoryEntries += 1
            continue
        }

        keptHistoryEntries += 1
        keepLines.add(line)
    }

    if (removedHistoryEntries > 0) {
        historyFile.writeText(keepLines.joinToString("\n"))
    }

    return removedHistoryEntries to keptHistoryEntries
}

private fun readBrowserHistoryEntries(
    workspaceRoot: String,
    requestedProfile: String?,
    limit: Int,
): Pair<List<BrowserHistoryEntry>, Int> {
    val historyFile = File(WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/history.jsonl"))
    if (!historyFile.isFile) {
        return emptyList<BrowserHistoryEntry>() to 0
    }

    val matchedEntries = mutableListOf<BrowserHistoryEntry>()
    for (line in historyFile.readLines()) {
        val trimmed = line.trim()
        if (trimmed.isBlank()) {
            continue
        }

        val lineJson = runCatching { Json.parseToJsonElement(trimmed).jsonObject }.getOrNull() ?: continue
        val profile = lineJson["profile"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: continue
        if (!isValidBrowserProfileName(profile)) {
            continue
        }
        if (requestedProfile != null && profile != requestedProfile) {
            continue
        }

        matchedEntries.add(
            BrowserHistoryEntry(
                ts = lineJson["ts"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() },
                profile = profile,
                action = lineJson["action"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() },
                url = lineJson["url"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() },
                title = lineJson["title"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() },
                screenshotPath = lineJson["screenshotPath"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() },
            )
        )
    }

    val matchedCount = matchedEntries.size
    val latestEntries = matchedEntries.takeLast(limit).asReversed()
    return latestEntries to matchedCount
}

private fun parseBrowserNavigateOutput(rawOutput: String): BrowserNavigateResult {
    val trimmed = rawOutput.trim()
    if (trimmed.isBlank()) {
        throw IllegalStateException("browser_navigate returned empty output")
    }

    val root = runCatching { Json.parseToJsonElement(trimmed).jsonObject }.getOrNull()
        ?: throw IllegalStateException("browser_navigate output was not valid JSON")

    val links = mutableListOf<BrowserNavigateLink>()
    val rawLinks = root["links"]
    if (rawLinks is JsonArray) {
        for (rawLink in rawLinks) {
            if (rawLink !is JsonObject) {
                continue
            }
            val url = rawLink["url"]?.jsonPrimitive?.contentOrNull?.trim()
            if (url.isNullOrBlank()) {
                continue
            }

            val text = rawLink["text"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null }
            links.add(BrowserNavigateLink(text = text, url = url))
        }
    }

    return BrowserNavigateResult(
        title = root["title"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null },
        finalUrl = root["finalUrl"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null },
        text = root["text"]?.jsonPrimitive?.contentOrNull,
        links = links,
    )
}

private fun parseBrowserSnapshotOutput(rawOutput: String): BrowserSnapshotResult {
    val trimmed = rawOutput.trim()
    if (trimmed.isBlank()) {
        throw IllegalStateException("browser_snapshot output was not valid JSON")
    }

    val root = runCatching { Json.parseToJsonElement(trimmed).jsonObject }.getOrNull()
        ?: throw IllegalStateException("browser_snapshot output was not valid JSON")

    val links = mutableListOf<BrowserNavigateLink>()
    val rawLinks = root["links"]
    if (rawLinks is JsonArray) {
        for (rawLink in rawLinks) {
            if (rawLink !is JsonObject) {
                continue
            }

            val url = rawLink["url"]?.jsonPrimitive?.contentOrNull?.trim()
            if (url.isNullOrBlank()) {
                continue
            }

            val text = rawLink["text"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null }
            links.add(BrowserNavigateLink(text = text, url = url))
        }
    }

    val elements = mutableListOf<BrowserInteractiveElement>()
    val rawElements = root["elements"]
    if (rawElements is JsonArray) {
        for (rawElement in rawElements) {
            if (rawElement !is JsonObject) {
                continue
            }

            val role = rawElement["role"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null }
            val name = rawElement["name"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null }
            val text = rawElement["text"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null }
            val selector = rawElement["selector"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null }
            val tag = rawElement["tag"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null }
            if (role == null && name == null && text == null && selector == null && tag == null) {
                continue
            }

            val options = mutableListOf<String>()
            val rawOptions = rawElement["options"]
            if (rawOptions is JsonArray) {
                for (rawOption in rawOptions) {
                    if (rawOption !is JsonPrimitive) {
                        continue
                    }
                    val option = rawOption.contentOrNull?.trim()?.ifBlank { null }
                    if (!option.isNullOrBlank()) {
                        options.add(option)
                    }
                }
            }

            elements.add(
                BrowserInteractiveElement(
                    role = role,
                    name = name,
                    text = text,
                    selector = selector,
                    tag = tag,
                    type = rawElement["type"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null },
                    placeholder = rawElement["placeholder"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null },
                    disabled = rawElement["disabled"]?.jsonPrimitive?.booleanOrNull,
                    checked = rawElement["checked"]?.jsonPrimitive?.booleanOrNull,
                    options = options.ifEmpty { null },
                )
            )
        }
    }

    return BrowserSnapshotResult(
        title = root["title"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null },
        finalUrl = root["finalUrl"]?.jsonPrimitive?.contentOrNull?.trim()?.ifBlank { null },
        text = root["text"]?.jsonPrimitive?.contentOrNull,
        links = links,
        elements = elements,
    )
}

private fun buildBrowserNavigateOutput(
    profile: String,
    requestedUrl: String,
    finalUrl: String,
    result: BrowserNavigateResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: navigate",
        "profile: $profile",
        "requested url: $requestedUrl",
        "final url: $finalUrl",
        "status: ok",
    )

    if (!result.title.isNullOrBlank()) {
        lines.add("title: ${result.title}")
    }

    lines.add("text:")
    lines.add(truncateBrowserText(result.text.orEmpty(), maxCharacters))

    lines.add("links:")
    if (result.links.isEmpty()) {
        lines.add("(no links)")
    } else {
        val maxLinks = minOf(result.links.size, 40)
        for (index in 0 until maxLinks) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines.add("  ${index + 1}: $text => $url")
        }
    }

    return lines.joinToString("\n")
}

private fun truncateBrowserText(raw: String, maxCharacters: Int): String {
    if (raw.length <= maxCharacters) {
        return raw
    }
    return raw.take(maxCharacters) + "\n[truncated]"
}

private fun buildBrowserSnapshotOutput(profile: String, result: BrowserSnapshotResult, maxCharacters: Int): String {
    val lines = mutableListOf(
        "browser action: snapshot",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )

    if (!result.title.isNullOrBlank()) {
        lines.add("title: ${result.title}")
    }

    lines.add("interactive elements:")
    if (result.elements.isEmpty()) {
        lines.add("(no interactive elements)")
    } else {
        val maxElements = minOf(result.elements.size, 40)
        for (index in 0 until maxElements) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines.add("  ${index + 1}: role=$role | name=$name | text=$text | selector=$selector | tag=$tag")
        }
    }

    lines.add("links:")
    if (result.links.isEmpty()) {
        lines.add("(no links)")
    } else {
        val maxLinks = minOf(result.links.size, 40)
        for (index in 0 until maxLinks) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines.add("  ${index + 1}: $text => $url")
        }
    }

    lines.add("text:")
    lines.add(truncateBrowserText(result.text.orEmpty(), maxCharacters))
    return lines.joinToString("\n")
}

private fun buildBrowserReloadOutput(profile: String, result: BrowserSnapshotResult, maxCharacters: Int): String {
    val lines = mutableListOf(
        "browser action: reload",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )

    if (!result.title.isNullOrBlank()) {
        lines.add("title: ${result.title}")
    }

    lines.add("interactive elements:")
    if (result.elements.isEmpty()) {
        lines.add("(no interactive elements)")
    } else {
        val maxElements = minOf(result.elements.size, 40)
        for (index in 0 until maxElements) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines.add("  ${index + 1}: role=$role | name=$name | text=$text | selector=$selector | tag=$tag")
        }
    }

    lines.add("links:")
    if (result.links.isEmpty()) {
        lines.add("(no links)")
    } else {
        val maxLinks = minOf(result.links.size, 40)
        for (index in 0 until maxLinks) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines.add("  ${index + 1}: $text => $url")
        }
    }

    lines.add("text:")
    lines.add(truncateBrowserText(result.text.orEmpty(), maxCharacters))
    return lines.joinToString("\n")
}

private fun buildBrowserHandoffOutput(
    profile: String,
    requestedUrl: String?,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: handoff",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )

    if (!result.title.isNullOrBlank()) {
        lines.add("title: ${result.title}")
    }

    if (!requestedUrl.isNullOrBlank()) {
        lines.add("requested url: $requestedUrl")
    }

    lines.add("interactive elements:")
    if (result.elements.isEmpty()) {
        lines.add("(no interactive elements)")
    } else {
        val maxElements = minOf(result.elements.size, 40)
        for (index in 0 until maxElements) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines.add("  ${index + 1}: role=$role | name=$name | text=$text | selector=$selector | tag=$tag")
        }
    }

    lines.add("links:")
    if (result.links.isEmpty()) {
        lines.add("(no links)")
    } else {
        val maxLinks = minOf(result.links.size, 40)
        for (index in 0 until maxLinks) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines.add("  ${index + 1}: $text => $url")
        }
    }

    lines.add("text:")
    lines.add(truncateBrowserText(result.text.orEmpty(), maxCharacters))
    return lines.joinToString("\n")
}

private fun appendBrowserHistoryEntry(
    historyFile: String,
    profile: String,
    action: String,
    url: String,
    title: String?,
    screenshotPath: String? = null,
) {
    val payload = buildJsonObject {
        put("ts", JsonPrimitive(Instant.now().toString()))
        put("profile", JsonPrimitive(profile))
        put("action", JsonPrimitive(action))
        put("url", JsonPrimitive(url))
        title?.trim()?.takeIf { it.isNotBlank() }?.let {
            put("title", JsonPrimitive(it))
        }
        screenshotPath?.trim()?.takeIf { it.isNotBlank() }?.let {
            put("screenshotPath", JsonPrimitive(it))
        }
    }.toString()

    try {
        File(historyFile).parentFile?.mkdirs()
        File(historyFile).appendText(payload + "\n")
    } catch (_: Exception) {
        // Ignore best-effort history append failures.
    }
}

private data class BrowserHistoryNavigationSnapshot(
    val stack: List<String>,
    val index: Int,
    val currentUrl: String?,
)

private fun browserNavigationStateFromStateFile(stateFile: String): BrowserHistoryNavigationSnapshot? {
    val stateFileHandle = File(stateFile)
    if (!stateFileHandle.isFile) {
        return null
    }

    val state = runCatching { Json.parseToJsonElement(stateFileHandle.readText()).jsonObject }.getOrNull() ?: return null
    val stack = when (val rawStack = state["navigationStack"]) {
        is JsonArray -> rawStack.mapNotNull {
            (it as? JsonPrimitive)?.contentOrNull
                ?.trim()
                ?.takeIf { value -> value.isNotBlank() }
        }
        else -> emptyList()
    }
    val rawIndexElement = state["navigationIndex"] as? JsonPrimitive
    val rawIndex = rawIndexElement?.intOrNull ?: rawIndexElement?.contentOrNull?.toIntOrNull()
    val currentUrl = state["url"]?.jsonPrimitive?.contentOrNull?.trim()?.takeIf { it.isNotBlank() }
    if (stack.isEmpty()) {
        return currentUrl?.let { BrowserHistoryNavigationSnapshot(listOf(it), index = 0, currentUrl = it) }
    }
    val clampedIndex = rawIndex?.coerceIn(0, stack.lastIndex)?.coerceAtLeast(0) ?: (stack.lastIndex)
    return BrowserHistoryNavigationSnapshot(stack = stack, index = clampedIndex, currentUrl = currentUrl)
}

private fun readBrowserHistoryEntriesForProfile(
    workspaceRoot: String,
    requestedProfile: String,
): List<BrowserHistoryEntry> {
    val historyFile = File(WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/history.jsonl"))
    if (!historyFile.isFile) {
        return emptyList()
    }

    val entries = mutableListOf<BrowserHistoryEntry>()
    for (line in historyFile.readLines()) {
        val trimmed = line.trim()
        if (trimmed.isBlank()) {
            continue
        }

        val lineJson = runCatching { Json.parseToJsonElement(trimmed).jsonObject }.getOrNull() ?: continue
        val profile = (lineJson["profile"] as? JsonPrimitive)?.content?.trim()?.takeIf { it.isNotBlank() } ?: continue
        if (profile != requestedProfile) {
            continue
        }
        entries.add(
            BrowserHistoryEntry(
                ts = (lineJson["ts"] as? JsonPrimitive)?.content?.trim()?.takeIf { it.isNotBlank() },
                profile = profile,
                action = (lineJson["action"] as? JsonPrimitive)?.content?.trim()?.takeIf { it.isNotBlank() },
                url = (lineJson["url"] as? JsonPrimitive)?.content?.trim()?.takeIf { it.isNotBlank() },
                title = (lineJson["title"] as? JsonPrimitive)?.content?.trim()?.takeIf { it.isNotBlank() },
                screenshotPath = (lineJson["screenshotPath"] as? JsonPrimitive)?.content?.trim()?.takeIf { it.isNotBlank() },
            )
        )
    }
    return entries
}

private fun browserNavigationStateFromHistory(entries: List<BrowserHistoryEntry>): BrowserHistoryNavigationSnapshot {
    if (entries.isEmpty()) return BrowserHistoryNavigationSnapshot(emptyList(), 0, null)

    val stack = mutableListOf<String>()
    var index = 0

    for (entry in entries) {
        val action = entry.action?.trim()?.lowercase()
        val targetUrl = entry.url?.trim() ?: continue
        if (targetUrl.isBlank()) continue

        when (action) {
            BrowserHistoryDirection.Back.actionName -> {
                val targetIndex = stack.lastIndexOf(targetUrl)
                when {
                    targetIndex != -1 && targetIndex <= index -> index = targetIndex
                    index - 1 in stack.indices -> {
                        stack[index - 1] = targetUrl
                        index -= 1
                    }
                }
            }
            BrowserHistoryDirection.Forward.actionName -> {
                val targetIndex = stack.lastIndexOf(targetUrl)
                when {
                    targetIndex != -1 && targetIndex >= index -> index = targetIndex
                    index + 1 in stack.indices -> {
                        stack[index + 1] = targetUrl
                        index += 1
                    }
                }
            }
            else -> {
                if (stack.isEmpty()) {
                    stack.add(targetUrl)
                    index = 0
                } else if (index in stack.indices && stack[index] == targetUrl) {
                    // keep current position for non-navigation actions
                } else {
                    if (index in stack.indices && index < stack.lastIndex) {
                        while (stack.size - 1 > index) {
                            stack.removeAt(stack.lastIndex)
                        }
                    }
                    if (stack.last() != targetUrl) {
                        stack.add(targetUrl)
                    }
                    index = stack.lastIndex
                }
            }
        }

        if (stack.isNotEmpty()) {
            index = index.coerceIn(0, stack.lastIndex)
        }
    }

    return BrowserHistoryNavigationSnapshot(
        stack = stack,
        index = index,
        currentUrl = entries.lastOrNull()?.url?.trim()?.takeIf { it.isNotBlank() },
    )
}

private fun browserHistoryNavigationSnapshot(
    paths: BrowserPaths,
    workspaceRoot: String,
): BrowserHistoryNavigationSnapshot {
    val fromState = browserNavigationStateFromStateFile(paths.stateFile)
    if (fromState != null && fromState.stack.isNotEmpty()) {
        return fromState
    }

    val entries = readBrowserHistoryEntriesForProfile(workspaceRoot, paths.profile)
    val fromHistory = browserNavigationStateFromHistory(entries)
    return when {
        fromHistory.stack.isNotEmpty() -> fromHistory
        else -> BrowserHistoryNavigationSnapshot(
            stack = fromState?.currentUrl?.let { listOf(it) } ?: emptyList(),
            index = 0,
            currentUrl = fromState?.currentUrl,
        )
    }
}

private fun browserHistoryNavigationURL(
    direction: BrowserHistoryDirection,
    paths: BrowserPaths,
    workspaceRoot: String,
): String {
    val snapshot = browserHistoryNavigationSnapshot(paths, workspaceRoot)
    if (snapshot.stack.isEmpty()) {
        throw IllegalArgumentException("no current browser history for this profile; call browser_navigate or browser_search first")
    }

    val targetIndex = snapshot.index + direction.offset
    if (targetIndex !in snapshot.stack.indices) {
        throw IllegalArgumentException(direction.missingEntryMessage)
    }
    return snapshot.stack[targetIndex]
}

private suspend fun executeBrowserHistoryNavigation(
    args: ToolArgs,
    context: ToolContext,
    direction: BrowserHistoryDirection,
): ToolObservation = withContext(Dispatchers.IO) {
    val toolName = when (direction) {
        BrowserHistoryDirection.Back -> "browser_back"
        BrowserHistoryDirection.Forward -> "browser_forward"
    }
    ensureNoUnknownArgs(args.root, toolName, setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
    val profile = BrowserToolConfig.normalizedProfile(
        getOptionalString(args.root, "profile", BrowserToolConfig.defaultProfile, migrationToolStringMaxLength)
    )
    val channel = BrowserToolConfig.normalizedChannel(
        getOptionalString(args.root, "channel", BrowserToolConfig.defaultChannel, migrationToolStringMaxLength)
    )
    val headless = getOptionalBoolean(args.root, "headless", true)
    val waitMillis = BrowserToolConfig.clampedWaitMillis(
        getInt(args.root, "waitMillis", BrowserToolConfig.defaultWaitMillis, minimum = BrowserToolConfig.minWaitMillis, maximum = BrowserToolConfig.maxWaitMillis)
    )
    val maxCharacters = getInt(args.root, "maxCharacters", BrowserToolConfig.maxSnapshotCharacters, minimum = 1, maximum = 100_000)

    val paths = BrowserToolConfig.prepare(profile, context.workspaceRoot)
    val result = withBrowserProfileCommandLock(paths) {
        val targetUrl = browserHistoryNavigationURL(direction, paths, context.workspaceRoot)
        validateHttpUrl(targetUrl)
        val command = buildBrowserNavigateCommand(
            targetUrl,
            paths.profileDir,
            paths.stateFile,
            channel,
            headless,
            waitMillis,
            maxCharacters,
        )
        val commandResult = context.shell.runAsync(command, context.workspaceRoot)

        if (commandResult.exitCode != 0) {
            val stdout = commandResult.stdOut.trim()
            val stderr = commandResult.stdErr.trim()
            val reason = stderr.ifBlank { stdout }.ifBlank { "node command failed" }
            throw IllegalStateException("browser_${direction.actionName} failed (exit ${commandResult.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(commandResult.stdOut)
        val finalUrl = parsed.finalUrl?.ifBlank { targetUrl } ?: targetUrl
        appendBrowserHistoryEntry(paths.historyFile, profile, direction.actionName, finalUrl, parsed.title)
        ToolObservation(buildBrowserHistoryNavigationOutput(profile, direction, parsed, maxCharacters))
    }

    return@withContext result
}

private fun buildBrowserHistoryNavigationOutput(
    profile: String,
    direction: BrowserHistoryDirection,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: ${direction.actionName}",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )

    if (!result.title.isNullOrBlank()) {
        lines.add("title: ${result.title}")
    }

    lines.add("interactive elements:")
    if (result.elements.isEmpty()) {
        lines.add("(no interactive elements)")
    } else {
        val maxElements = minOf(result.elements.size, 40)
        for (index in 0 until maxElements) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines.add("  ${index + 1}: role=$role | name=$name | text=$text | selector=$selector | tag=$tag")
        }
    }

    lines.add("links:")
    if (result.links.isEmpty()) {
        lines.add("(no links)")
    } else {
        val maxLinks = minOf(result.links.size, 40)
        for (index in 0 until maxLinks) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines.add("  ${index + 1}: $text => $url")
        }
    }

    lines.add("text:")
    lines.add(truncateBrowserText(result.text.orEmpty(), maxCharacters))
    return lines.joinToString("\n")
}

private fun buildBrowserNavigateCommand(
    url: String,
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require('playwright');
        const fs = require('fs');
        const path = require('path');

        const requestedUrl = (process.argv[2] || '').trim();
        const profileDir = process.argv[3] || '';
        const stateFile = process.argv[4] || '';
        const channel = (process.argv[5] || 'chromium').toLowerCase();
        const headless = String(process.argv[6]).toLowerCase() === 'true';
        const waitMillis = Number(process.argv[7] || '600');
        const maxCharacters = Number(process.argv[8] || '100000');

        if (!/^https?:\/\//i.test(requestedUrl)) {
            console.error('browser_navigate requires an absolute HTTP(S) URL');
            process.exit(2);
        }

        function normalizeChannel(raw) {
            if (raw === 'chrome' || raw === 'chrome-beta' || raw === 'chrome-dev' || raw === 'chrome-canary') {
                return 'chrome';
            }
            if (raw === 'msedge' || raw === 'msedge-beta' || raw === 'msedge-dev' || raw === 'msedge-canary') {
                return 'msedge';
            }
            return 'chromium';
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== 'chromium') {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== '.') {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const page = await browser.newPage();
                    await page.goto(requestedUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const bodyText = document.body && document.body.innerText ? document.body.innerText : '';
                        const normalizedText = (bodyText || '').replace(/\s+/g, ' ').trim().slice(0, limit);
                        const links = Array.from(document.querySelectorAll('a[href]'))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || '').trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || '').replace(/\s+/g, ' ').trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        return {
                            text: normalizedText,
                            title: document.title || '',
                            links: links,
                        };
                    }, Math.max(1, Math.min(100000, maxCharacters)));

                    await browser.storageState({ path: stateFile });
                    const output = {
                        requestedUrl,
                        finalUrl: page.url(),
                        title: payload.title,
                        text: payload.text,
                        links: payload.links,
                    };
                    console.log(JSON.stringify(output));
                } finally {
                    await browser.close();
                }
            } catch (error) {
                console.error(error && error.stack ? error.stack : String(error));
                process.exit(1);
            }
        })();
    """.trimIndent().replace('\n', ' ')

    return "node -e " + shellQuote(script) + " " +
        shellQuote(url) + " " +
        shellQuote(profileDir) + " " +
        shellQuote(stateFile) + " " +
        shellQuote(channel) + " " +
        (if (headless) "true" else "false") + " " +
        waitMillis + " " +
        maxCharacters
}

private fun buildBrowserReloadCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    ignoreCache: Boolean,
    targetUrl: String?,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require('playwright');
        const fs = require('fs');
        const path = require('path');

        const targetUrl = (process.argv[2] || '').trim();
        const profileDir = process.argv[3] || '';
        const stateFile = process.argv[4] || '';
        const channel = (process.argv[5] || 'chromium').toLowerCase();
        const headless = String(process.argv[6]).toLowerCase() === 'true';
        const ignoreCache = String(process.argv[7]).toLowerCase() === 'true';
        const waitMillis = Number(process.argv[8] || '600');
        const maxCharacters = Number(process.argv[9] || '100000');

        function normalizeText(value) {
            return String(value || '').replace(/\\s+/g, ' ').trim();
        }

        function normalizeChannel(raw) {
            if (raw === 'chrome' || raw === 'chrome-beta' || raw === 'chrome-dev' || raw === 'chrome-canary') {
                return 'chrome';
            }
            if (raw === 'msedge' || raw === 'msedge-beta' || raw === 'msedge-dev' || raw === 'msedge-canary') {
                return 'msedge';
            }
            return 'chromium';
        }

        function isHttpUrl(value) {
            try {
                const parsed = new URL(value);
                return parsed.protocol === 'http:' || parsed.protocol === 'https:';
            } catch (_) {
                return false;
            }
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return '#' + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return node.tagName.toLowerCase() + '[name="' + attribute.replace(/"/g, '\\\\"') + '"]';
                }
            }
            return node.tagName.toLowerCase();
        }

        function readHistoryFallbackUrl(filePath) {
            try {
                const raw = fs.readFileSync(filePath, 'utf8');
                const lines = raw.split('\\n');
                for (let index = lines.length - 1; index >= 0; index--) {
                    const line = (lines[index] || '').trim();
                    if (!line) {
                        continue;
                    }
                    try {
                        const record = JSON.parse(line);
                        const action = typeof record?.action === 'string' ? record.action : '';
                        const url = typeof record?.url === 'string' ? record.url.trim() : '';
                        if (!url) {
                            continue;
                        }
                        return url;
                    } catch (_) {}
                }
            } catch (_) {}
            return '';
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== 'chromium') {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== '.') {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const existingPages = browser.pages();
                    const page = (existingPages && existingPages.length > 0) ? existingPages[0] : await browser.newPage();

                    let urlToLoad = targetUrl;
                    if (!isHttpUrl(urlToLoad)) {
                        urlToLoad = readHistoryFallbackUrl(stateFile);
                    }
                    if (!isHttpUrl(urlToLoad)) {
                        const currentUrl = page.url && page.url();
                        if (!isHttpUrl(currentUrl)) {
                            throw new Error('No reload target URL available for browser_reload');
                        }
                    } else {
                        await page.goto(urlToLoad, { waitUntil: 'domcontentloaded', timeout: 30000 });
                    }

                    const reloadTimeout = ignoreCache ? 0 : 30000;
                    await page.reload({
                        waitUntil: 'domcontentloaded',
                        timeout: reloadTimeout || 30000,
                        ignoreCache,
                    });

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll('a[href], button, input, select, textarea, option, label, summary'));
                        const links = Array.from(document.querySelectorAll('a[href]'))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || '').trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute('role') || element.getAttribute('type') || '').trim();
                                const name = (element.getAttribute('aria-label') || element.getAttribute('name') || element.getAttribute('title') || '').trim();
                                const text = (element.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
                                const tag = (element.tagName || '').trim().toLowerCase();
                                const elementSelector = inferSelector(element);
                                return {
                                    role: role || null,
                                    name: name || null,
                                    text: text || null,
                                    selector: elementSelector,
                                    tag: tag || null,
                                };
                            })
                            .filter((entry) => entry.role || entry.name || entry.text || entry.selector || entry.tag);

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : '';
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || '',
                            finalUrl: window.location.href || '',
                            links: links,
                            elements: elements,
                        };
                    }, Math.max(1, Math.min(100000, maxCharacters)));

                    await browser.storageState({ path: stateFile });
                    const output = {
                        finalUrl: payload.finalUrl,
                        title: payload.title,
                        text: payload.text,
                        links: payload.links,
                        elements: payload.elements,
                    };
                    console.log(JSON.stringify(output));
                } finally {
                    await browser.close();
                }
            } catch (error) {
                console.error(error && error.stack ? error.stack : String(error));
                process.exit(1);
            }
        })();
    """.trimIndent().replace('\n', ' ')

        return "node -e " + shellQuote(script) + " " +
        shellQuote(targetUrl.orEmpty()) + " " +
        shellQuote(profileDir) + " " +
        shellQuote(stateFile) + " " +
        shellQuote(channel) + " " +
        (if (headless) "true" else "false") + " " +
        (if (ignoreCache) "true" else "false") + " " +
        waitMillis + " " +
        maxCharacters
}

private fun buildBrowserSnapshotCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require('playwright');
        const fs = require('fs');
        const path = require('path');

        const profileDir = process.argv[2] || '';
        const stateFile = process.argv[3] || '';
        const channel = (process.argv[4] || 'chromium').toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === 'true';
        const waitMillis = Number(process.argv[6] || '600');
        const maxCharacters = Number(process.argv[7] || '100000');

        function normalizeText(value) {
            return String(value || '').replace(/\\s+/g, ' ').trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return '#' + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return node.tagName.toLowerCase() + '[name="' + attribute.replace(/"/g, '\\\\"') + '"]';
                }
            }
            return node.tagName.toLowerCase();
        }

        function normalizeChannel(raw) {
            if (raw === 'chrome' || raw === 'chrome-beta' || raw === 'chrome-dev' || raw === 'chrome-canary') {
                return 'chrome';
            }
            if (raw === 'msedge' || raw === 'msedge-beta' || raw === 'msedge-dev' || raw === 'msedge-canary') {
                return 'msedge';
            }
            return 'chromium';
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== 'chromium') {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== '.') {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const existingPages = browser.pages();
                    const page = (existingPages && existingPages.length > 0) ? existingPages[0] : await browser.newPage();
                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll('a[href], button, input, select, textarea, option, label, summary'));
                        const links = Array.from(document.querySelectorAll('a[href]'))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || '').trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute('role') || element.getAttribute('type') || '').trim();
                                const name = (element.getAttribute('aria-label') || element.getAttribute('name') || element.getAttribute('title') || '').trim();
                                const text = (element.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
                                const tag = (element.tagName || '').trim().toLowerCase();
                                const elementSelector = inferSelector(element);
                                return {
                                    role: role || null,
                                    name: name || null,
                                    text: text || null,
                                    selector: elementSelector,
                                    tag: tag || null,
                                };
                            })
                            .filter((entry) => entry.role || entry.name || entry.text || entry.selector || entry.tag);

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : '';
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || '',
                            finalUrl: window.location.href || '',
                            links: links,
                            elements: elements,
                        };
                    }, Math.max(1, Math.min(100000, maxCharacters)));

                    await browser.storageState({ path: stateFile });
                    const output = {
                        finalUrl: payload.finalUrl,
                        title: payload.title,
                        text: payload.text,
                        links: payload.links,
                        elements: payload.elements,
                    };
                    console.log(JSON.stringify(output));
                } finally {
                    await browser.close();
                }
            } catch (error) {
                console.error(error && error.stack ? error.stack : String(error));
                process.exit(1);
            }
        })();
    """.trimIndent().replace('\n', ' ')

    return "node -e " + shellQuote(script) + " " +
        shellQuote(profileDir) + " " +
        shellQuote(stateFile) + " " +
        shellQuote(channel) + " " +
        (if (headless) "true" else "false") + " " +
        waitMillis + " " +
        maxCharacters
}

private fun buildBrowserHandoffCommand(
    requestedUrl: String,
    profileDir: String,
    stateFile: String,
    channel: String,
    handoffSeconds: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require('playwright');
        const fs = require('fs');
        const path = require('path');

        const requestedUrl = process.argv[2] || '';
        const profileDir = process.argv[3] || '';
        const stateFile = process.argv[4] || '';
        const channel = (process.argv[5] || 'chromium').toLowerCase();
        const handoffTimeoutMillis = Number(process.argv[6] || '60000');
        const maxCharacters = Number(process.argv[7] || '100000');

        function normalizeChannel(raw) {
            if (raw === 'chrome' || raw === 'chrome-beta' || raw === 'chrome-dev' || raw === 'chrome-canary') {
                return 'chrome';
            }
            if (raw === 'msedge' || raw === 'msedge-beta' || raw === 'msedge-dev' || raw === 'msedge-canary') {
                return 'msedge';
            }
            return 'chromium';
        }

        function normalizeText(value) {
            return String(value || '').replace(/\\s+/g, ' ').trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return '#' + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return node.tagName.toLowerCase() + '[name="' + attribute.replace(/"/g, '\\\\"') + '"]';
                }
            }
            return node.tagName.toLowerCase();
        }

        (async () => {
            try {
                const options = {
                    headless: false,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== 'chromium') {
                    options.channel = normalizedChannel;
                }

                if (requestedUrl && !/^https?:\/\//i.test(requestedUrl)) {
                    console.error('browser_handoff requires an absolute HTTP(S) URL');
                    process.exit(2);
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== '.') {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const existingPages = browser.pages();
                    const page = (existingPages && existingPages.length > 0) ? existingPages[0] : await browser.newPage();

                    if (requestedUrl) {
                        await page.goto(requestedUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
                    }

                    if (handoffTimeoutMillis > 0) {
                        await page.waitForTimeout(handoffTimeoutMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll('a[href], button, input, select, textarea, option, label, summary'));
                        const links = Array.from(document.querySelectorAll('a[href]'))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || '').trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute('role') || element.getAttribute('type') || '').trim();
                                const name = (element.getAttribute('aria-label') || element.getAttribute('name') || element.getAttribute('title') || '').trim();
                                const text = (element.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
                                const tag = (element.tagName || '').trim().toLowerCase();
                                const elementSelector = inferSelector(element);
                                return {
                                    role: role || null,
                                    name: name || null,
                                    text: text || null,
                                    selector: elementSelector,
                                    tag: tag || null,
                                };
                            })
                            .filter((entry) => entry.role || entry.name || entry.text || entry.selector || entry.tag);

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : '';
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || '',
                            finalUrl: window.location.href || '',
                            links: links,
                            elements: elements,
                        };
                    }, Math.max(1, Math.min(100000, maxCharacters)));

                    await browser.storageState({ path: stateFile });
                    const output = {
                        finalUrl: payload.finalUrl,
                        title: payload.title,
                        text: payload.text,
                        links: payload.links,
                        elements: payload.elements,
                    };
                    console.log(JSON.stringify(output));
                } finally {
                    await browser.close();
                }
            } catch (error) {
                console.error(error && error.stack ? error.stack : String(error));
                process.exit(1);
            }
        })();
    """.trimIndent().replace('\n', ' ')

    return "node -e " + shellQuote(script) + " " +
        shellQuote(requestedUrl) + " " +
        shellQuote(profileDir) + " " +
        shellQuote(stateFile) + " " +
        shellQuote(channel) + " " +
        handoffSeconds.coerceAtLeast(1).times(1000) + " " +
        maxCharacters
}

private fun buildBrowserHistoryOutput(
    requestedProfile: String?,
    limit: Int,
    matchedEntries: Int,
    entries: List<BrowserHistoryEntry>,
): List<String> {
    val lines = mutableListOf(
        "browser action: history query",
        "requested profile: ${requestedProfile ?: "all"}",
        "limit: $limit",
        "matched entries: $matchedEntries",
        "returned entries: ${entries.size}",
    )

    if (entries.isEmpty()) {
        lines += "(no matching history entries)"
        return lines
    }

    for ((index, entry) in entries.withIndex()) {
        lines += ""
        lines += "entry #${index + 1}"
        lines += "  profile: ${entry.profile}"
        lines += "  timestamp: ${entry.ts ?: "n/a"}"
        entry.action?.let { lines += "  action: $it" }
        entry.url?.let { lines += "  url: $it" }
        entry.title?.let { lines += "  title: $it" }
    }

    return lines
}

private fun buildBrowserProfileDeleteOutput(profile: String, summary: BrowserProfileDeleteSummary): List<String> {
    val runtime = summary.runtimeBeforeDelete
    return listOf(
        "browser action: delete profile",
        "profile: $profile",
        "removed profile data: ${if (summary.removedProfileData) "yes" else "no"}",
        "removed downloads: ${if (summary.removedDownloads) "yes" else "no"}",
        "removed state: ${if (summary.removedState) "yes" else "no"}",
        "removed history entries: ${summary.removedHistoryEntries}",
        "kept history entries: ${summary.keptHistoryEntries}",
        "runtime markers before delete: DevToolsActivePort=${if (runtime.activeBrowserMarkerPresent) "yes" else "no"}, Singleton*=${if (runtime.profileLockMarkerPresent) "yes" else "no"}",
    )
}

private fun buildBrowserProfileDeleteBlockedOutput(
    profile: String,
    runtimeMetadata: BrowserProfileRuntimeMetadata,
): String {
    return """
        |browser action: delete profile
        |profile: $profile
        |status: blocked
        |runtime markers: DevToolsActivePort=${if (runtimeMetadata.activeBrowserMarkerPresent) "yes" else "no"}, Singleton*=${if (runtimeMetadata.profileLockMarkerPresent) "yes" else "no"}
        |reason: profile appears active; close related browser windows/processes and retry after the profile is no longer in use.
    """.trimMargin()
}

private class GenerateImageTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "generate_image",
        description = "Generate image files from a prompt using the configured image provider or injected local image model backend.",
        sideEffect = SideEffect.Write,
        parameters = migrationToolParameters("generate_image"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "generate_image", setOf("prompt", "outputPath", "size", "count"))
        val outputPath = getRequiredString(args.root, "outputPath", 1_024, minLength = 1).trim()
        if (outputPath.isBlank()) {
            throw IllegalArgumentException("tool args outputPath must not be blank")
        }
        return listOf(outputPath)
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override fun risksNetwork(args: ToolArgs, context: ToolContext): Boolean {
        return context.imageGenerator !is ProviderImageGenerationToolService
    }

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "generate_image", setOf("prompt", "outputPath", "size", "count"))
        val prompt = getRequiredString(args.root, "prompt", 100_000, minLength = 1)
        val outputPath = getRequiredString(args.root, "outputPath", 1_024, minLength = 1)
        val size = getOptionalString(args.root, "size", "1024x1024", 64, minLength = 1)
            .trim()
            .ifBlank { "1024x1024" }
        val count = getInt(args.root, "count", 1, minimum = 1, maximum = 4)
        val generator = context.imageGenerator
            ?: throw IllegalStateException("generate_image is not configured; attach an image provider or local image backend before using this tool")

        val confinedOutputPath = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, outputPath)
        return generator.generateImage(
            prompt = prompt,
            size = size,
            count = count,
            outputPath = confinedOutputPath,
            workspaceRoot = context.workspaceRoot,
        )
    }
}

private class WebFetchTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "web_fetch",
        description = "Fetch an HTTP(S) URL without browser state. Use browser_* tools when login, JavaScript, cookies, or history are needed.",
        sideEffect = SideEffect.Network,
        parameters = migrationToolParameters("web_fetch"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "web_fetch", setOf("url", "maxCharacters"))
        getRequiredString(args.root, "url", migrationToolUrlMaxLength, minLength = 1).trim()
        getInt(args.root, "maxCharacters", 20_000, minimum = 1, maximum = browserMaxSnapshotCharacters)
        return emptyList()
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "web_fetch", setOf("url", "maxCharacters"))
        val urlText = getRequiredString(args.root, "url", migrationToolUrlMaxLength, minLength = 1).trim()
        val url = validateHttpUrl(urlText)
        val limit = maxCharacters(getInt(args.root, "maxCharacters", 20_000, minimum = 1, maximum = browserMaxSnapshotCharacters))
        val connection = withContext(Dispatchers.IO) {
            (url.openConnection() as HttpURLConnection).apply {
                connectTimeout = 10_000
                readTimeout = 10_000
                requestMethod = "GET"
                setRequestProperty("User-Agent", "IntatisAgent/0.16")
                instanceFollowRedirects = true
            }
        }

        try {
            val responseCode = withContext(Dispatchers.IO) { connection.responseCode }
            val contentType = withContext(Dispatchers.IO) { connection.contentType ?: "unknown" }
            val rawBody = withContext(Dispatchers.IO) {
                val stream = runCatching { connection.inputStream }.getOrNull() ?: connection.errorStream
                stream?.readBytes()?.toString(Charset.forName("UTF-8")) ?: ""
            }
            val truncated = rawBody.length > limit
            val shown = if (truncated) rawBody.take(limit) else rawBody
            val lines = listOf(
                "status: $responseCode",
                "url: ${connection.url ?: urlText}",
                "content-type: ${contentType.ifBlank { "unknown" }}",
                "",
                shown,
            )
            return ToolObservation(text = lines.joinToString("\n"), truncated = truncated)
        } finally {
            withContext(Dispatchers.IO) {
                connection.disconnect()
            }
        }
    }
}

private class ReadPDFTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "read_pdf",
        description = "Extract readable text from a PDF in the workspace, with optional 1-based page ranges such as '1-3,5'.",
        sideEffect = SideEffect.ReadOnly,
        parameters = migrationToolParameters("read_pdf"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "read_pdf", setOf("path", "pages", "maxCharacters"))
        return listOf(requiredString(args.root, "path", maxLength = 1_024, minLength = 1))
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "read_pdf", setOf("path", "pages", "maxCharacters"))
        return ToolObservation("read_pdf is not implemented on Android yet.")
    }
}

private class EditPDFPagesTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "edit_pdf_pages",
        description = "Page-level PDF editing: extract selected pages to one PDF or split selected pages into one PDF per page.",
        sideEffect = SideEffect.Write,
        parameters = migrationToolParameters("edit_pdf_pages"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "edit_pdf_pages", setOf("mode", "inputPath", "pages", "outputPath", "outputDir", "outputPrefix"))
        val inputPath = requiredString(args.root, "inputPath", maxLength = 1_024, minLength = 1)
        val outputPath = getOptionalString(args.root, "outputPath", "", maxLength = 1_024, minLength = 1)
        val outputDir = getOptionalString(args.root, "outputDir", "", maxLength = 1_024, minLength = 1)
        return buildList {
            add(inputPath)
            if (outputPath.isNotBlank()) add(outputPath)
            if (outputDir.isNotBlank()) add(outputDir)
        }
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "edit_pdf_pages", setOf("mode", "inputPath", "pages", "outputPath", "outputDir", "outputPrefix"))
        val mode = requiredString(args.root, "mode", maxLength = 1_024, minLength = 1).trim().lowercase()
        val inputPath = requiredString(args.root, "inputPath", maxLength = 1_024, minLength = 1)
        val rawPages = getOptionalString(args.root, "pages", "", maxLength = 1_024, minLength = 1).trim()
        val outputPath = getOptionalString(args.root, "outputPath", "", maxLength = 1_024, minLength = 1).trim()
        val outputDir = getOptionalString(args.root, "outputDir", "", maxLength = 1_024, minLength = 1).trim()
        val outputPrefix = getOptionalString(args.root, "outputPrefix", "", maxLength = 1_024, minLength = 1).trim()

        ensureQpdfAvailable(context)

        val resolvedInput = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, inputPath)
        val pageCount = resolvePdfPageCount(context, resolvedInput)
        if (pageCount <= 0) {
            throw IllegalArgumentException("could not determine a valid page count for $inputPath")
        }

        val selectedPages = parsePdfPageSelection(rawPages.ifBlank { null }, pageCount)
        if (selectedPages.isEmpty()) {
            throw IllegalArgumentException("no pages selected")
        }

        val selectedPageArgs = selectedPages.joinToString(" ") { (it + 1).toString() }
        return when (mode) {
            "extract" -> {
                if (outputPath.isBlank()) {
                    throw IllegalArgumentException("edit_pdf_pages mode 'extract' requires outputPath")
                }
                val resolvedOutput = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, outputPath)
                File(resolvedOutput).parentFile?.mkdirs()
                val command = "qpdf --empty --pages ${shellQuote(resolvedInput)} $selectedPageArgs -- ${shellQuote(resolvedOutput)}"
                val result = context.shell.runAsync(command, context.workspaceRoot)
                if (result.exitCode != 0) {
                    throw IllegalStateException(
                        "qpdf failed for extract: exit ${result.exitCode}\n${outputText(result.stdOut, result.stdErr, result.exitCode)}"
                    )
                }
                ToolObservation("extracted ${selectedPages.size} page(s) from $inputPath to $outputPath", changedFiles = listOf(outputPath))
            }

            "split" -> {
                if (outputDir.isBlank()) {
                    throw IllegalArgumentException("edit_pdf_pages mode 'split' requires outputDir")
                }
                val resolvedOutputDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, outputDir)
                File(resolvedOutputDir).mkdirs()
                val sourceName = File(resolvedInput).nameWithoutExtension
                val digits = maxOf(3, pageCount.toString().length)
                val prefix = outputPrefix.ifBlank { sourceName }
                val changed = mutableListOf<String>()

                for (page in selectedPages) {
                    val oneBased = page + 1
                    val pageName = "$prefix-page-${String.format("%0${digits}d", oneBased)}.pdf"
                    val resolvedPage = File(resolvedOutputDir, pageName).path
                    val splitCommand =
                        "qpdf --empty --pages ${shellQuote(resolvedInput)} $oneBased -- ${shellQuote(resolvedPage)}"
                    val splitResult = context.shell.runAsync(splitCommand, context.workspaceRoot)
                    if (splitResult.exitCode != 0) {
                        throw IllegalStateException(
                            "qpdf failed for split page $oneBased: exit ${splitResult.exitCode}\n" +
                                outputText(splitResult.stdOut, splitResult.stdErr, splitResult.exitCode)
                        )
                    }
                    changed.add(File(outputDir).resolve(pageName).path.replace("\\", "/"))
                }

                ToolObservation(
                    text = "split ${selectedPages.size} page(s) from $inputPath into $outputDir",
                    changedFiles = changed
                )
            }

            else -> throw IllegalArgumentException("unsupported edit_pdf_pages mode '$mode'; use 'extract' or 'split'")
        }
    }
}

private class ReconstructDocumentImageTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "reconstruct_document_image",
        description = "Convert a photographed/scanned document image into an editable document file using installed mature OCR/layout CLIs such as Docling, Marker, or Tesseract.",
        sideEffect = SideEffect.Write,
        parameters = migrationToolParameters("reconstruct_document_image"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "reconstruct_document_image", setOf("imagePath", "outputPath", "format", "backend"))
        val imagePath = requiredString(args.root, "imagePath", 1_024, minLength = 1)
        val outputPath = requiredString(args.root, "outputPath", 1_024, minLength = 1)
        return listOf(imagePath, outputPath)
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "reconstruct_document_image", setOf("imagePath", "outputPath", "format", "backend"))
        val imagePath = requiredString(args.root, "imagePath", 1_024, minLength = 1)
        val outputPath = requiredString(args.root, "outputPath", 1_024, minLength = 1)
        val rawFormat = getOptionalString(args.root, "format", "", 1_024)
        val format = normalizedDocumentFormat(rawFormat, outputPath)
        val requestedBackend = getOptionalString(args.root, "backend", "auto", 1_024)
            .trim()
            .lowercase()

        val resolvedInput = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, imagePath)
        val resolvedOutput = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, outputPath)
        File(resolvedOutput).parentFile?.mkdirs()

        val tmpDir = File(
            File(resolvedOutput).parentFile ?: File(context.workspaceRoot),
            ".intatis-doc-reconstruct-${System.currentTimeMillis()}"
        )
        tmpDir.mkdirs()

        val usedBackend = try {
            when (resolveBackend(requestedBackend, context)) {
                "docling" -> runDoclingBackend(context, resolvedInput, resolvedOutput, tmpDir, format)
                "marker" -> runMarkerBackend(context, resolvedInput, resolvedOutput, tmpDir, format)
                "tesseract" -> runTesseractBackend(context, resolvedInput, resolvedOutput, format)
                else -> throw IllegalArgumentException("unsupported backend: $requestedBackend")
            }
        } finally {
            tmpDir.deleteRecursively()
        }

        if (!File(resolvedOutput).exists()) {
            throw IllegalStateException("document reconstruction finished but did not create ${outputPath}")
        }

        return ToolObservation(
            "reconstructed $imagePath to $outputPath using $usedBackend backend",
            changedFiles = listOf(outputPath),
        )
    }

    private fun normalizedDocumentFormat(raw: String?, outputPath: String): String {
        val inferred = File(outputPath).extension.lowercase()
        val value = raw
            ?.trim()
            ?.lowercase()
            ?.ifBlank { inferred.ifBlank { "md" } }
            ?: inferred.ifBlank { "md" }

        return when (value) {
            "markdown", "md" -> "md"
            "html", "htm" -> "html"
            "text", "txt" -> "text"
            else -> "md"
        }
    }

    private fun extensionForDocumentFormat(format: String): String = when (format) {
        "html" -> "html"
        "text" -> "txt"
        else -> "md"
    }

    private fun markerOutputFormat(format: String): String = if (format == "md") "markdown" else format

    private suspend fun commandAvailable(context: ToolContext, name: String): Boolean {
        val checker = if (System.getProperty("os.name").lowercase().contains("win")) {
            "where $name"
        } else {
            "command -v $name"
        }
        return context.shell.runAsync(checker, context.workspaceRoot).exitCode == 0
    }

    private suspend fun resolveBackend(requested: String, context: ToolContext): String {
        val normalized = when (requested.lowercase()) {
            "", "auto" -> "auto"
            "marker_single", "marker" -> "marker"
            "docling" -> "docling"
            "tesseract" -> "tesseract"
            else -> throw IllegalArgumentException("unsupported backend: $requested; supported: auto, docling, marker, tesseract")
        }

        return when (normalized) {
            "auto" -> when {
                commandAvailable(context, "docling") -> "docling"
                commandAvailable(context, "marker_single") -> "marker"
                commandAvailable(context, "tesseract") -> "tesseract"
                else -> throw IllegalStateException("No document reconstruction backend found. Install docling, marker, OCRmyPDF, PaddleOCR, or tesseract.")
            }

            else -> {
                val command = when (normalized) {
                    "marker" -> "marker_single"
                    else -> normalized
                }
                if (!commandAvailable(context, command)) {
                    throw IllegalStateException("$command is not installed")
                }
                normalized
            }
        }
    }

    private suspend fun runDoclingBackend(
        context: ToolContext,
        resolvedInput: String,
        resolvedOutput: String,
        tmpDir: File,
        format: String,
    ): String {
        val command = "docling convert --to ${shellQuote(format)} --output ${shellQuote(tmpDir.path)} ${shellQuote(resolvedInput)}"
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            throw IllegalStateException(
                "docling failed for $format: ${outputText(result.stdOut, result.stdErr, result.exitCode)}"
            )
        }
        val produced = tmpDir.walkTopDown()
            .firstOrNull { it.isFile && it.extension.lowercase() == extensionForDocumentFormat(format) }
        if (produced == null) {
            throw IllegalStateException("docling produced no .${extensionForDocumentFormat(format)} output")
        }
        produced.copyTo(File(resolvedOutput), overwrite = true)
        return "docling"
    }

    private suspend fun runMarkerBackend(
        context: ToolContext,
        resolvedInput: String,
        resolvedOutput: String,
        tmpDir: File,
        format: String,
    ): String {
        val command = "marker_single ${shellQuote(resolvedInput)} --output_dir ${shellQuote(tmpDir.path)} --output_format ${shellQuote(markerOutputFormat(format))}"
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            throw IllegalStateException(
                "marker_single failed: ${outputText(result.stdOut, result.stdErr, result.exitCode)}"
            )
        }
        val produced = tmpDir.walkTopDown()
            .firstOrNull { it.isFile && it.extension.lowercase() == extensionForDocumentFormat(format) }
        if (produced == null) {
            throw IllegalStateException("marker produced no .${extensionForDocumentFormat(format)} output")
        }
        produced.copyTo(File(resolvedOutput), overwrite = true)
        return "marker"
    }

    private suspend fun runTesseractBackend(
        context: ToolContext,
        resolvedInput: String,
        resolvedOutput: String,
        format: String,
    ): String {
        if (format == "html") {
            throw IllegalStateException("tesseract fallback only supports markdown or text output; install docling or marker for HTML layout output")
        }

        val result = context.shell.runAsync("tesseract ${shellQuote(resolvedInput)} stdout --psm 1", context.workspaceRoot)
        if (result.exitCode != 0) {
            throw IllegalStateException(
                "tesseract failed: ${outputText(result.stdOut, result.stdErr, result.exitCode)}"
            )
        }

        val finalText = if (format == "md") {
            "# Reconstructed document\n\n${result.stdOut}"
        } else {
            result.stdOut
        }
        File(resolvedOutput).writeText(finalText)
        return "tesseract"
    }
}

private suspend fun ensureQpdfAvailable(context: ToolContext) {
    val checker = if (System.getProperty("os.name").lowercase().contains("win")) {
        "where qpdf"
    } else {
        "command -v qpdf"
    }
    val check = context.shell.runAsync(checker, context.workspaceRoot)
    if (check.exitCode != 0) {
        throw IllegalStateException("edit_pdf_pages requires qpdf. Install qpdf and retry.")
    }
}

private suspend fun resolvePdfPageCount(context: ToolContext, resolvedPath: String): Int {
    val info = context.shell.runAsync("pdfinfo ${shellQuote(resolvedPath)}", context.workspaceRoot)
    if (info.exitCode != 0) {
        throw IllegalStateException("edit_pdf_pages requires pdfinfo from Poppler to read PDF page count. Install pdfinfo and retry.")
    }
    val output = listOf(info.stdOut, info.stdErr).joinToString("\n")
    val match = Regex("^\\s*Pages:\\s+(\\d+)\\b", RegexOption.MULTILINE).find(output)
        ?: throw IllegalStateException("could not determine PDF page count for $resolvedPath")
    return match.groupValues[1].toInt()
}

private fun parsePdfPageSelection(raw: String?, pageCount: Int): List<Int> {
    if (pageCount <= 0) return emptyList()
    val trimmed = raw?.trim().orEmpty()
    if (trimmed.isEmpty() || trimmed.equals("all", ignoreCase = true)) {
        return (0 until pageCount).toList()
    }

    val pages = mutableListOf<Int>()
    val seen = HashSet<Int>()
    for (part in trimmed.split(",")) {
        val token = part.trim()
        if (token.isEmpty()) continue

        val dash = token.indexOf("-")
        if (dash >= 0) {
            val left = token.substring(0, dash).trim()
            val right = token.substring(dash + 1).trim()
            val start = left.toIntOrNull() ?: throw IllegalArgumentException("invalid page range: $token")
            val end = right.toIntOrNull() ?: throw IllegalArgumentException("invalid page range: $token")
            if (start <= 0 || end <= 0 || start > end) {
                throw IllegalArgumentException("invalid page range: $token")
            }
            for (page in start..end) {
                if (page > pageCount) {
                    throw IllegalArgumentException("page $page exceeds document page count $pageCount")
                }
                val zeroBased = page - 1
                if (seen.add(zeroBased)) {
                    pages.add(zeroBased)
                }
            }
        } else {
            val page = token.toIntOrNull() ?: throw IllegalArgumentException("invalid page number: $token")
            if (page <= 0) {
                throw IllegalArgumentException("invalid page number: $token")
            }
            if (page > pageCount) {
                throw IllegalArgumentException("page $page exceeds document page count $pageCount")
            }
            val zeroBased = page - 1
            if (seen.add(zeroBased)) {
                pages.add(zeroBased)
            }
        }
    }
    return pages
}

private fun outputText(stdout: String, stderr: String, exitCode: Int, limit: Int = 20_000): String {
    val hasStderr = stderr.isNotBlank()
    val text = buildString {
        append(stdout)
        if (hasStderr) {
            append(if (isNotEmpty()) "\n" else "").append("[stderr]\n").append(stderr)
        }
        append("\n[exit ").append(exitCode).append("]")
    }
    return if (text.length > limit) {
        text.take(limit) + "\n[truncated]"
    } else {
        text
    }
}

private fun shellQuote(text: String): String = "'${text.replace("'", "'\\''")}'"

private class CompileLaTeXTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "compile_latex",
        description = "Compile a LaTeX .tex file in the workspace to PDF using installed Tectonic, latexmk, xelatex, or pdflatex.",
        sideEffect = SideEffect.Exec,
        parameters = migrationToolParameters("compile_latex"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "compile_latex", setOf("inputPath", "outputDir", "engine"))
        val inputPath = requiredString(args.root, "inputPath", maxLength = 1_024, minLength = 1)
        val outputDir = getOptionalString(args.root, "outputDir", "", 1_024)
        return buildList {
            add(inputPath)
            if (outputDir.isNotBlank()) add(outputDir)
        }
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "compile_latex", setOf("inputPath", "outputDir", "engine"))
        val inputPath = requiredString(args.root, "inputPath", maxLength = 1_024, minLength = 1)
        val outputDir = getOptionalString(args.root, "outputDir", "", 1_024).trim()
        val requestedEngine = getOptionalString(args.root, "engine", "auto", 32).trim().lowercase()

        val resolvedInput = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, inputPath)
        val inputFile = File(resolvedInput)
        if (!inputFile.exists()) {
            throw IllegalStateException("compile_latex input file not found: $inputPath")
        }

        if (!inputFile.extension.equals("tex", ignoreCase = true)) {
            throw IllegalArgumentException("compile_latex inputPath must be a .tex file: $inputPath")
        }

        val resolvedOutputDir = WorkspaceSecurity.resolveInWorkspace(
            context.workspaceRoot,
            outputDir.ifBlank { inputFile.parent ?: context.workspaceRoot }
        )
        File(resolvedOutputDir).mkdirs()

        val before = existingPdfFiles(resolvedOutputDir)
        val engine = resolveLaTeXEngine(requestedEngine, context)
        val command = buildLaTeXCommand(engine, resolvedInput, resolvedOutputDir)
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            throw IllegalStateException(
                "compile_latex failed with $engine: ${outputText(result.stdOut, result.stdErr, result.exitCode)}"
            )
        }

        val outputPdf = detectLaTeXPdf(
            outputDir = resolvedOutputDir,
            before = before,
            baseName = inputFile.nameWithoutExtension,
        )

        return ToolObservation(
            "compiled $inputPath to ${outputPdf.path} using $engine",
            changedFiles = listOf(relativeFromWorkspace(context.workspaceRoot, outputPdf.path)),
        )
    }
}

private fun resolveLaTeXEngine(requested: String, context: ToolContext): String = when (val normalized = requested.ifBlank { "auto" }.lowercase()) {
    "", "auto" -> when {
        commandAvailable(context, "tectonic") -> "tectonic"
        commandAvailable(context, "latexmk") -> "latexmk"
        commandAvailable(context, "xelatex") -> "xelatex"
        commandAvailable(context, "pdflatex") -> "pdflatex"
        else -> throw IllegalStateException("compile_latex requires one of: tectonic, latexmk, xelatex, pdflatex")
    }
    "tectonic", "latexmk", "xelatex", "pdflatex" -> if (commandAvailable(context, normalized)) normalized else throw IllegalStateException("$normalized is not installed")
    else -> throw IllegalArgumentException("unsupported compile_latex engine '$requested'; supported: auto, tectonic, latexmk, xelatex, pdflatex")
}

private fun buildLaTeXCommand(engine: String, resolvedInput: String, resolvedOutputDir: String): String = when (engine) {
    "tectonic" -> "tectonic --outdir ${shellQuote(resolvedOutputDir)} ${shellQuote(resolvedInput)}"
    "latexmk" -> "latexmk -pdf -interaction=nonstopmode -output-directory=${shellQuote(resolvedOutputDir)} ${shellQuote(resolvedInput)}"
    "xelatex" -> "xelatex -interaction=nonstopmode -output-directory=${shellQuote(resolvedOutputDir)} ${shellQuote(resolvedInput)}"
    "pdflatex" -> "pdflatex -interaction=nonstopmode -output-directory=${shellQuote(resolvedOutputDir)} ${shellQuote(resolvedInput)}"
    else -> throw IllegalArgumentException("unsupported compile command '$engine'")
}

private fun existingPdfFiles(outputDir: String): Set<String> = File(outputDir)
    .listFiles { it.isFile && it.extension.equals("pdf", ignoreCase = true) }
    ?.mapTo(mutableSetOf()) { it.name }
    ?: emptySet()

private fun detectLaTeXPdf(
    outputDir: String,
    before: Set<String>,
    baseName: String,
): File {
    val dir = File(outputDir)
    val after = dir.listFiles { it.isFile && it.extension.equals("pdf", ignoreCase = true) }
        ?: emptyArray()

    val expected = File(dir, "$baseName.pdf")
    if (expected.exists()) {
        return expected
    }

    val produced = after
        .filter { it.name !in before }
        .map { it.name }

    return when {
        produced.size == 1 -> File(dir, produced.first())
        produced.isEmpty() -> throw IllegalStateException(
            "compile_latex did not produce any new PDF in $outputDir"
        )
        else -> throw IllegalStateException(
            "compile_latex produced multiple PDFs in $outputDir; expected a single result: ${produced.joinToString(", ")}"
        )
    }
}

private fun relativeFromWorkspace(workspaceRoot: String, absolutePath: String): String {
    val normalizedWorkspace = File(workspaceRoot).canonicalPath
    val normalizedTarget = File(absolutePath).canonicalPath
    return if (normalizedTarget.startsWith(normalizedWorkspace)) {
        normalizedTarget.removePrefix(normalizedWorkspace).trimStart('/', '\\')
    } else {
        absolutePath
    }
}

private class UnsupportedTool(
    private val name: String,
    private val description: String,
    private val sideEffect: SideEffect,
    private val parameters: Map<String, Any>,
) : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = name,
        description = description,
        sideEffect = sideEffect,
        parameters = parameters,
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, name, emptySet())
        return emptyList()
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, name, emptySet())
        return ToolObservation("$name is not implemented on Android yet.")
    }
}

interface IToolShellRunner {
    suspend fun runAsync(command: String, workingDirectory: String): ShellResult
}

data class ShellResult(val stdOut: String, val stdErr: String, val exitCode: Int)

class ProcessShellRunner : IToolShellRunner {
    override suspend fun runAsync(command: String, workingDirectory: String): ShellResult = withContext(Dispatchers.IO) {
        val os = System.getProperty("os.name").lowercase()
        val commandLine = when {
            os.contains("win") -> listOf("cmd.exe", "/c", command)
            os.contains("linux") || os.contains("mac") || os.contains("darwin") || os.contains("android") -> listOf("/bin/sh", "-c", command)
            else -> throw UnsupportedOperationException("Shell runner unavailable on platform: $os")
        }

        val process = ProcessBuilder()
            .directory(File(workingDirectory))
            .command(commandLine)
            .start()

        val out = process.inputStream.bufferedReader().readText()
        val err = process.errorStream.bufferedReader().readText()
        val code = process.waitFor()
        ShellResult(out, err, code)
    }
}

interface IToolGitService {
    suspend fun statusAsync(workspaceRoot: String): String
    suspend fun diffAsync(workspaceRoot: String): String
}

private data class GitStatusEntry(
    val x: Char,
    val y: Char,
    val path: String,
)

private fun parseGitStatus(porcelain: String): List<GitStatusEntry> = porcelain
    .lineSequence()
    .map { it.trimEnd('\r') }
    .filter { it.isNotBlank() }
    .mapNotNull { line ->
        if (line.length < 4) return@mapNotNull null
        GitStatusEntry(line[0], line[1], line.substring(3))
    }
    .toList()

class ProcessGitService(private val shell: IToolShellRunner = ProcessShellRunner()) : IToolGitService {
    override suspend fun statusAsync(workspaceRoot: String): String {
        return shell.runAsync("git status --porcelain=v1", workspaceRoot).stdOut
    }

    override suspend fun diffAsync(workspaceRoot: String): String {
        return shell.runAsync("git diff", workspaceRoot).stdOut
    }
}

class InMemoryGitService(
    private val statusText: String = "",
    private val diffText: String = "",
) : IToolGitService {
    override suspend fun statusAsync(workspaceRoot: String): String = statusText
    override suspend fun diffAsync(workspaceRoot: String): String = diffText
}

class ReadFileTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "read_file",
        description = "Read a UTF-8 text file within the workspace.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "path" to mapOf("type" to "string", "maxLength" to 1024, "minLength" to 1),
                "maxBytes" to mapOf("type" to "integer", "minimum" to 1, "maximum" to 1_000_000),
            ),
            "required" to listOf("path"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> =
        listOf(getRequiredString(args.root, "path", 1_024, minLength = 1))

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "read_file", setOf("path", "maxBytes"))
        val path = getRequiredString(args.root, "path", 1_024, minLength = 1)
        val maxBytes = getInt(args.root, "maxBytes", -1, minimum = 1, maximum = 1_000_000)
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val bytes = withContext(Dispatchers.IO) { File(resolved).readBytes() }
        val limit = if (maxBytes > 0) maxBytes else 100_000
        val truncated = bytes.size > limit
        val text = String(if (truncated) bytes.copyOfRange(0, limit) else bytes, Charset.forName("UTF-8"))
        return ToolObservation(text, truncated)
    }
}

class ListFilesTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "list_files",
        description = "List entries of a directory within the workspace.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf("path" to mapOf("type" to "string", "maxLength" to 1024, "minLength" to 1)),
            "required" to emptyList<String>(),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(getOptionalString(args.root, "path", ".", 1_024, minLength = 1))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "list_files", setOf("path"))
        val path = getOptionalString(args.root, "path", ".", 1_024, minLength = 1)
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val entries = withContext(Dispatchers.IO) {
            File(resolved).list()?.sorted() ?: emptyList()
        }

        val names = entries.mapNotNull { name ->
            try {
                if (File(resolved, name).isDirectory) "${name}/" else name
            } catch (_: Exception) {
                name
            }
        }
        return ToolObservation(names.joinToString("\n"))
    }
}

class SearchTextTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "search_text",
        description = "Search literal substring in files under a workspace path.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "query" to mapOf("type" to "string", "maxLength" to 1_024, "minLength" to 1),
                "path" to mapOf("type" to "string", "maxLength" to 1_024, "minLength" to 1),
            ),
            "required" to listOf("query"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(getOptionalString(args.root, "path", ".", 1_024, minLength = 1))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "search_text", setOf("query", "path"))
        val query = getRequiredString(args.root, "query", 1_024, minLength = 1)
        val path = getOptionalString(args.root, "path", ".", 1_024, minLength = 1)
        val base = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val baseFile = File(base)
        val matches = mutableListOf<String>()
        val max = 200

        if (baseFile.isFile) {
            searchInFile(baseFile, baseFile.toPath().toString(), query, context.workspaceRoot, matches)
        } else {
            File(base).walkTopDown()
                .onEnter { file -> file == baseFile || !isHiddenPath(file) }
                .filter { it.isFile && !isHiddenPath(it) }
                .forEach { file ->
                searchInFile(file, file.toString(), query, context.workspaceRoot, matches, max)
                }
        }

        val truncated = matches.size >= max
        val output = if (matches.isEmpty()) "(no matches)" else matches.joinToString("\n")
        ToolObservation(output, truncated)
    }

    private fun searchInFile(file: File, absolute: String, query: String, workspace: String, out: MutableList<String>, max: Int = Int.MAX_VALUE) {
        if (out.size >= max) return
        val text = try {
            file.readText(Charset.forName("UTF-8"))
        } catch (_: Exception) {
            return
        }

        val relative = java.nio.file.Paths.get(workspace).relativize(java.nio.file.Paths.get(file.absolutePath)).toString()
        val lines = text.split('\n')
        for ((i, line) in lines.withIndex()) {
            if (out.size >= max) break
            if (line.contains(query)) {
                out.add("$relative:${i + 1}: $line")
            }
        }
    }

}

class WriteFileTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "write_file",
        description = "Write (create or overwrite) a UTF-8 text file within the workspace.",
        sideEffect = SideEffect.Write,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "path" to mapOf("type" to "string", "maxLength" to 1_024, "minLength" to 1),
                "content" to mapOf("type" to "string", "maxLength" to 200_000),
            ),
            "required" to listOf("path", "content"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(getRequiredString(args.root, "path", 1_024, minLength = 1))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "write_file", setOf("path", "content"))
        val path = getRequiredString(args.root, "path", 1_024, minLength = 1)
        val content = getRequiredString(args.root, "content", 200_000)
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        withContext(Dispatchers.IO) {
            val file = File(resolved)
            file.parentFile?.mkdirs()
            file.writeText(content)
        }
        return ToolObservation("wrote ${content.toByteArray().size} bytes to $path", changedFiles = listOf(path))
    }
}

class ApplyPatchTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "apply_patch",
        description = "Apply a unified diff to files within the workspace.",
        sideEffect = SideEffect.Write,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf("diff" to mapOf("type" to "string", "maxLength" to 300_000, "minLength" to 1)),
            "required" to listOf("diff"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "apply_patch", setOf("diff"))
        val diff = getRequiredString(args.root, "diff", 300_000, minLength = 1)
        return parsePatch(diff).map { it.path }
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "apply_patch", setOf("diff"))
        val diff = getRequiredString(args.root, "diff", 300_000, minLength = 1)
        val patches = parsePatch(diff)
        if (patches.isEmpty()) throw IllegalArgumentException("no file sections found in diff")

        val changed = mutableListOf<String>()
        for (patch in patches) {
            val target = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, patch.path)
            val original = withContext(Dispatchers.IO) {
                if (File(target).exists()) File(target).readText() else ""
            }
            val updated = applyPatch(original, patch.hunks)
            withContext(Dispatchers.IO) {
                File(target).parentFile?.mkdirs()
                File(target).writeText(updated)
            }
            changed.add(patch.path)
        }
        return ToolObservation("applied patch to: ${changed.joinToString(", ")}", changedFiles = changed, diff = diff)
    }

    private data class PatchFile(val path: String, val hunks: MutableList<PatchHunk>)
    private data class PatchHunk(val oldLines: List<String>, val newLines: List<String>)

    private fun parsePatch(diff: String): List<PatchFile> {
        val files = mutableListOf<PatchFile>()
        var current: PatchFile? = null
        var oldLines = mutableListOf<String>()
        var newLines = mutableListOf<String>()
        var inHunk = false

        diff.lineSequence().forEach { rawLine ->
            val line = rawLine.removeSuffix("\r")
            when {
                line.startsWith("--- ") -> {
                    if (current != null && inHunk) {
                        current!!.hunks.add(PatchHunk(oldLines.toList(), newLines.toList()))
                        oldLines = mutableListOf()
                        newLines = mutableListOf()
                        inHunk = false
                    }
                    current = null
                }
                line.startsWith("+++ ") -> {
                    val raw = line.substring(4)
                    val path = if (raw.startsWith("b/")) raw.substring(2) else raw
                    current = PatchFile(path = path, hunks = mutableListOf())
                }
                line.startsWith("@@") -> {
                    if (inHunk && current != null) {
                        current!!.hunks.add(PatchHunk(oldLines.toList(), newLines.toList()))
                        oldLines = mutableListOf()
                        newLines = mutableListOf()
                    }
                    inHunk = true
                }
                inHunk && current != null -> {
                    when {
                        line.isEmpty() -> {
                            oldLines.add("")
                            newLines.add("")
                        }
                        line[0] == '+' -> newLines.add(line.substring(1))
                        line[0] == '-' -> oldLines.add(line.substring(1))
                        line[0] == ' ' -> {
                            val same = line.substring(1)
                            oldLines.add(same)
                            newLines.add(same)
                        }
                        else -> {
                            // Ignore non-standard hunk lines to match Apple patch parser behavior.
                        }
                    }
                }
            }
        }

        if (current != null) {
            if (inHunk) {
                current!!.hunks.add(PatchHunk(oldLines.toList(), newLines.toList()))
            }
            files.add(current!!)
        }
        return files
    }

    private fun applyPatch(original: String, hunks: List<PatchHunk>): String {
        val lines = if (original.isEmpty()) mutableListOf() else original.split('\n').toMutableList()
        for (hunk in hunks) {
            if (hunk.oldLines.isEmpty()) {
                lines.addAll(hunk.newLines)
                continue
            }

            val range = findRange(lines, hunk.oldLines)
                ?: throw IllegalArgumentException("patch hunk did not match file content")
            lines.subList(range.first, range.first + range.second).clear()
            lines.addAll(range.first, hunk.newLines)
        }
        return lines.joinToString("\n")
    }

    private fun findRange(source: List<String>, needle: List<String>): Pair<Int, Int>? {
        if (needle.isEmpty() || needle.size > source.size) return null
        for (start in source.indices) {
            if (start + needle.size > source.size) break
            var match = true
            for (i in needle.indices) {
                if (source[start + i] != needle[i]) {
                    match = false
                    break
                }
            }
            if (match) return Pair(start, needle.size)
        }
        return null
    }
}

class RunShellTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "run_shell",
        description = "Run a shell command in workspace directory.",
        sideEffect = SideEffect.Exec,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf("command" to mapOf("type" to "string", "maxLength" to 8_000, "minLength" to 1)),
            "required" to listOf("command"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = RunShellTool.risksNetwork(args)

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "run_shell", setOf("command"))
        val command = getRequiredString(args.root, "command", 8_000, minLength = 1)
        val result = context.shell.runAsync(command, context.workspaceRoot)
        var output = result.stdOut
        if (result.stdErr.isNotBlank()) output += "\n[stderr]\n${result.stdErr}"
        output += "\n[exit ${result.exitCode}]"
        return ToolObservation(output)
    }

    companion object {
        fun risksNetwork(args: ToolArgs): Boolean {
            ensureNoUnknownArgs(args.root, "run_shell", setOf("command"))
            val raw = getRequiredString(args.root, "command", 8_000, minLength = 1)
            return ShellInspector.risksNetworkOrInstall(raw)
        }
    }
}

class GitStatusTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "git_status",
        description = "Show working-tree status (porcelain).",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(),
            "required" to emptyList<String>(),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "git_status", emptySet())
        val entries = parseGitStatus(context.git.statusAsync(context.workspaceRoot))
        return if (entries.isEmpty()) {
            ToolObservation("clean")
        } else {
            ToolObservation(entries.joinToString("\n") { "${it.x}${it.y} ${it.path}" })
        }
    }
}

class GitDiffTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "git_diff",
        description = "Show unstaged changes as a unified diff.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(),
            "required" to emptyList<String>(),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "git_diff", emptySet())
        val diff = context.git.diffAsync(context.workspaceRoot)
        if (diff.isBlank()) return ToolObservation("(no changes)")
        val truncated = diff.toByteArray(Charsets.UTF_8).size > 200_000
        val text = if (truncated) diff.take(200_000) else diff
        return ToolObservation(text, truncated, diff = diff)
    }
}

class AskAgentTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "ask_agent",
        description = "Ask another attached agent a question. Returns their answer.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "to" to mapOf("type" to "string", "maxLength" to 64),
                "question" to mapOf("type" to "string", "maxLength" to 4_000),
            ),
            "required" to listOf("to", "question"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "ask_agent", setOf("to", "question"))
        val messenger = context.messenger
            ?: return ToolObservation("agent messaging is not available in this session")
        val to = getRequiredString(args.root, "to", 64)
        val question = getRequiredString(args.root, "question", 4_000)
        val answer = messenger.askAsync(context.agentName, to, question)
        return ToolObservation(answer)
    }
}

class SpawnAgentTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "spawn_agent",
        description = "Create a new sub-agent bound to a folder so you can delegate work to it. "
            + "Give it a short name and an absolute folder path; model is optional (defaults to yours). "
            + "After spawning, assign work with delegate_task.",
        sideEffect = SideEffect.Write,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "name" to mapOf("type" to "string", "maxLength" to 64),
                "path" to mapOf("type" to "string", "maxLength" to 1_024),
                "model" to mapOf("type" to "string", "maxLength" to 256),
                "canCoordinate" to mapOf("type" to "boolean"),
            ),
            "required" to listOf("name", "path"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "spawn_agent", setOf("name", "path", "model", "canCoordinate"))
        val messenger = context.messenger
            ?: return ToolObservation("agent management is not available in this session")
        val name = getRequiredString(args.root, "name", 64)
        val path = getRequiredString(args.root, "path", 1_024)
        val model = args.root["model"]?.jsonPrimitive?.content
        val canCoordinate = getOptionalBoolean(args.root, "canCoordinate", false)
        val result = messenger.spawnAgentAsync(name, path, model, canCoordinate)
        return ToolObservation(result)
    }
}

class ListAgentsTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "list_agents",
        description = "List the agents currently active in this conversation (name, model, folder).",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf<String, Any>(),
            "required" to emptyList<String>(),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "list_agents", emptySet())
        val messenger = context.messenger
            ?: return ToolObservation("agent management is not available in this session")
        val result = messenger.listAgentsAsync()
        return ToolObservation(result)
    }
}

class RemoveAgentTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "remove_agent",
        description = "Remove a sub-agent you no longer need. You cannot remove @main.",
        sideEffect = SideEffect.Write,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "name" to mapOf("type" to "string", "maxLength" to 64),
            ),
            "required" to listOf("name"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "remove_agent", setOf("name"))
        val messenger = context.messenger
            ?: return ToolObservation("agent management is not available in this session")
        val name = getRequiredString(args.root, "name", 64)
        val result = messenger.removeAgentAsync(name)
        return ToolObservation(result)
    }
}

class SendMessageTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "send_message",
        description = "Send a message to another attached agent without creating a task.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "to" to mapOf("type" to "string", "maxLength" to 64, "description" to "target agent name"),
                "content" to mapOf("type" to "string", "maxLength" to 4_000),
            ),
            "required" to listOf("to", "content"),
            "additionalProperties" to false,
        ),
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "send_message", setOf("to", "content"))
        val messenger = context.messenger
            ?: return ToolObservation("agent messaging is not available in this session")
        val to = getRequiredString(args.root, "to", 64)
        val content = getRequiredString(args.root, "content", 4_000)
        val result = messenger.sendMessageAsync(context.agentName, to, content)
        return ToolObservation(result)
    }
}

class RequestInformationTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "request_information",
        description = "Ask another attached agent for information without creating a delegated task.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "to" to mapOf("type" to "string", "maxLength" to 64, "description" to "target agent name"),
                "question" to mapOf("type" to "string", "maxLength" to 4_000),
            ),
            "required" to listOf("to", "question"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "request_information", setOf("to", "question"))
        val messenger = context.messenger
            ?: return ToolObservation("agent messaging is not available in this session")
        val to = getRequiredString(args.root, "to", 64)
        val question = getRequiredString(args.root, "question", 4_000)
        val result = messenger.requestInformationAsync(context.agentName, to, question, context.currentTaskID)
        return ToolObservation(result)
    }
}

class ReplyMessageTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "reply_message",
        description = "Reply to information request from an attached agent.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "to" to mapOf("type" to "string", "maxLength" to 64),
                "answer" to mapOf("type" to "string", "maxLength" to 4_000),
                "inReplyTo" to mapOf("type" to "string", "maxLength" to 128),
            ),
            "required" to listOf("to", "answer"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "reply_message", setOf("to", "answer", "inReplyTo"))
        val messenger = context.messenger
            ?: return ToolObservation("agent messaging is not available in this session")
        val to = getRequiredString(args.root, "to", 64)
        val answer = getRequiredString(args.root, "answer", 4_000)
        val inReplyTo = getOptionalString(args.root, "inReplyTo", context.currentTaskID ?: "", 128).ifBlank { context.currentTaskID }
        val result = messenger.replyMessageAsync(
            context.agentName,
            to,
            answer,
            inReplyTo = inReplyTo,
            taskID = context.currentTaskID,
        )
        return ToolObservation(result)
    }
}

class RequestDelegationTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "request_delegation",
        description = "Request another attached agent to delegate a task.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
        "properties" to mapOf(
                "objective" to mapOf("type" to "string", "maxLength" to 4_000),
                "reason" to mapOf("type" to "string", "maxLength" to 128),
            ),
            "required" to listOf("objective"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "request_delegation", setOf("objective", "reason"))
        val messenger = context.messenger
            ?: return ToolObservation("agent messaging is not available in this session")
        val objective = getRequiredString(args.root, "objective", 4_000)
        val reason = getOptionalString(args.root, "reason", "delegation requested", 128)
        val result = messenger.requestDelegationAsync(context.agentName, objective, reason)
        return ToolObservation(result)
    }
}

class DelegateTaskTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "delegate_task",
        description = "Delegate a task directly to an attached agent.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
        "properties" to mapOf(
                "to" to mapOf("type" to "string", "maxLength" to 64),
                "objective" to mapOf("type" to "string", "maxLength" to 4_000),
                "reason" to mapOf("type" to "string", "maxLength" to 128),
                "roleHint" to mapOf("type" to "string", "maxLength" to 64),
                "expectedDeliverable" to mapOf("type" to "string", "maxLength" to 128),
            ),
            "required" to listOf("to", "objective"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "delegate_task", setOf("to", "objective", "reason", "roleHint", "expectedDeliverable"))
        val messenger = context.messenger
            ?: return ToolObservation("agent messaging is not available in this session")
        val to = getRequiredString(args.root, "to", 64)
        val objective = getRequiredString(args.root, "objective", 4_000)
        val reason = getOptionalString(args.root, "reason", "delegation requested", 128)
        val roleHint = getOptionalString(args.root, "roleHint", "cowork", 64)
        val expectedDeliverable = getOptionalString(args.root, "expectedDeliverable", "response", 128)
        val result = messenger.delegateTaskAsync(
            context.agentName,
            to,
            objective,
            reason,
            roleHint,
            expectedDeliverable,
            context.currentTaskID,
        )
        return ToolObservation(result)
    }
}
