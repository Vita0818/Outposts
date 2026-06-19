package com.intatis.shared

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import java.io.File
import java.nio.charset.Charset

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
    private val json = Json { ignoreUnknownKeys = true }
    val root: JsonObject = json.parseToJsonElement(raw).jsonObject
}

data class ToolContext(
    val workspaceRoot: String,
    val agentName: String,
    val shell: IToolShellRunner,
    val git: IToolGitService,
    val messenger: IToolAgentMessenger? = null,
)

interface ITool {
    val descriptor: ToolDescriptor
    fun touchedPaths(args: ToolArgs): List<String>
    fun risksNetwork(args: ToolArgs): Boolean
    suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation
}

interface IToolAgentMessenger {
    suspend fun askAsync(from: String, to: String, question: String): String
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
        fun standard(): ToolRegistry = ToolRegistry(
            listOf(
                ReadFileTool(),
                ListFilesTool(),
                SearchTextTool(),
                WriteFileTool(),
                ApplyPatchTool(),
                RunShellTool(),
                GitStatusTool(),
                GitDiffTool(),
            )
        )
    }
}

interface IToolShellRunner {
    suspend fun runAsync(command: String, workingDirectory: String): ShellResult
}

data class ShellResult(val stdOut: String, val stdErr: String, val exitCode: Int)

class ProcessShellRunner : IToolShellRunner {
    override suspend fun runAsync(command: String, workingDirectory: String): ShellResult = withContext(Dispatchers.IO) {
        val isWindows = System.getProperty("os.name").lowercase().contains("win")
        val process = ProcessBuilder()
            .directory(File(workingDirectory))
            .command(
                if (isWindows) listOf("cmd.exe", "/c", command) else listOf("sh", "-c", command)
            )
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

class ProcessGitService(private val shell: IToolShellRunner = ProcessShellRunner()) : IToolGitService {
    override suspend fun statusAsync(workspaceRoot: String): String {
        return shell.runAsync("git status --porcelain=v1", workspaceRoot).stdOut
    }

    override suspend fun diffAsync(workspaceRoot: String): String {
        return shell.runAsync("git diff", workspaceRoot).stdOut
    }
}

class ReadFileTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "read_file",
        description = "Read a UTF-8 text file within the workspace.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "path" to mapOf("type" to "string"),
                "maxBytes" to mapOf("type" to "integer"),
            ),
            "required" to listOf("path")
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> =
        listOf(getRequiredString(args.root, "path", ""))

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        val path = getRequiredString(args.root, "path", null) ?: throw IllegalArgumentException("tool args missing required 'path'")
        val maxBytes = getInt(args.root, "maxBytes")
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val bytes = withContext(Dispatchers.IO) { File(resolved).readBytes() }
        val limit = if (maxBytes > 0) maxBytes else 100_000
        val truncated = bytes.size > limit
        val text = String(if (truncated) bytes.copyOfRange(0, limit) else bytes, Charset.forName("UTF-8"))
        return ToolObservation(text, truncated)
    }

    private fun getRequiredString(root: JsonObject, key: String, fallback: String?): String? {
        val value = root[key]?.jsonPrimitive?.contentOrNull
        return value ?: fallback
    }

    private fun getInt(root: JsonObject, key: String): Int {
        val raw = root[key]?.jsonPrimitive ?: return -1
        return raw.intOrNull ?: -1
    }
}

class ListFilesTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "list_files",
        description = "List entries of a directory within the workspace.",
        sideEffect = SideEffect.ReadOnly,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf("path" to mapOf("type" to "string")),
            "required" to emptyList<String>(),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(getString(args.root, "path", "."))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        val path = getString(args.root, "path", ".")
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

    private fun getString(root: JsonObject, key: String, fallback: String): String {
        return root[key]?.jsonPrimitive?.contentOrNull ?: fallback
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
                "query" to mapOf("type" to "string"),
                "path" to mapOf("type" to "string"),
            ),
            "required" to listOf("query"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(getString(args.root, "path", "."))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val query = args.root["query"]?.jsonPrimitive?.contentOrNull ?: throw IllegalArgumentException("tool args missing required 'query'")
        val path = getString(args.root, "path", ".")
        val base = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val baseFile = File(base)
        val matches = mutableListOf<String>()
        val max = 200

        if (baseFile.isFile) {
            searchInFile(baseFile, baseFile.toPath().toString(), query, context.workspaceRoot, matches)
        } else {
            File(base).walkBottomUp().forEachIndexed { idx, file ->
                if (idx > 50_000 && matches.isNotEmpty()) return@forEachIndexed
                if (!file.isFile) return@forEachIndexed
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
                out.add("$relative:${i + 1}:$line")
            }
        }
    }

    private fun getString(root: JsonObject, key: String, fallback: String): String =
        root[key]?.jsonPrimitive?.contentOrNull ?: fallback
}

class WriteFileTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "write_file",
        description = "Write (create or overwrite) a UTF-8 text file within the workspace.",
        sideEffect = SideEffect.Write,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf(
                "path" to mapOf("type" to "string"),
                "content" to mapOf("type" to "string"),
            ),
            "required" to listOf("path", "content"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(getRequiredString(args.root, "path") ?: "")
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        val path = getRequiredString(args.root, "path") ?: throw IllegalArgumentException("tool args missing required 'path'")
        val content = getRequiredString(args.root, "content") ?: throw IllegalArgumentException("tool args missing required 'content'")
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        withContext(Dispatchers.IO) {
            val file = File(resolved)
            file.parentFile?.mkdirs()
            file.writeText(content)
        }
        return ToolObservation("wrote $path (${content.toByteArray().size} bytes)")
    }

    private fun getRequiredString(root: JsonObject, key: String): String? {
        return root[key]?.jsonPrimitive?.contentOrNull
    }
}

class ApplyPatchTool : ITool {
    override val descriptor: ToolDescriptor = ToolDescriptor(
        name = "apply_patch",
        description = "Apply a unified diff to files within the workspace.",
        sideEffect = SideEffect.Write,
        parameters = mapOf(
            "type" to "object",
            "properties" to mapOf("diff" to mapOf("type" to "string")),
            "required" to listOf("diff"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        val diff = getRequiredString(args.root, "diff") ?: ""
        return parsePatch(diff).map { it.path }.distinct()
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        val diff = getRequiredString(args.root, "diff") ?: throw IllegalArgumentException("tool args missing required 'diff'")
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
        return ToolObservation("applied patch to ${changed.joinToString(", ")}", changedFiles = changed, diff = diff)
    }

    private fun getRequiredString(root: JsonObject, key: String): String? {
        return root[key]?.jsonPrimitive?.contentOrNull
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
                        else -> inHunk = false
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
            "properties" to mapOf("command" to mapOf("type" to "string")),
            "required" to listOf("command"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = RunShellTool.risksNetwork(args)

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        val command = getRequiredString(args.root, "command") ?: throw IllegalArgumentException("tool args missing required 'command'")
        val result = context.shell.runAsync(command, context.workspaceRoot)
        var output = result.stdOut
        if (result.stdErr.isNotBlank()) output += "\n[stderr]\n${result.stdErr}"
        output += "\n[exit ${result.exitCode}]"
        return ToolObservation(output)
    }

    companion object {
        fun risksNetwork(args: ToolArgs): Boolean {
            val raw = args.root["command"]?.jsonPrimitive?.contentOrNull ?: return false
            return ShellInspector.risksNetworkOrInstall(raw)
        }
    }

    private fun getRequiredString(root: JsonObject, key: String): String? {
        return root[key]?.jsonPrimitive?.contentOrNull
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
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        val text = context.git.statusAsync(context.workspaceRoot)
        return if (text.isBlank()) ToolObservation("clean") else ToolObservation(text.trimEnd())
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
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        val diff = context.git.diffAsync(context.workspaceRoot)
        if (diff.isBlank()) return ToolObservation("(no changes)")
        val truncated = diff.length > 200_000
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
                "to" to mapOf("type" to "string"),
                "question" to mapOf("type" to "string"),
            ),
            "required" to listOf("to", "question"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun executeAsync(args: ToolArgs, context: ToolContext): ToolObservation {
        val messenger = context.messenger
            ?: return ToolObservation("agent messaging is not available in this session")
        val to = args.root["to"]?.jsonPrimitive?.contentOrNull
            ?: return ToolObservation("tool args missing required 'to'")
        val question = args.root["question"]?.jsonPrimitive?.contentOrNull
            ?: return ToolObservation("tool args missing required 'question'")
        val answer = messenger.askAsync(context.agentName, to, question)
        return ToolObservation(answer)
    }
}
