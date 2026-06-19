package com.intatis.shared.tools

import com.intatis.shared.security.SideEffect
import com.intatis.shared.security.WorkspaceSecurity
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.*
import java.io.BufferedReader
import java.io.File
import java.io.InputStreamReader
import java.io.IOException

private val json = Json { ignoreUnknownKeys = true }

data class ToolDescriptor(
    val name: String,
    val description: String,
    val sideEffect: SideEffect,
    val parameters: Map<String, Any?>,
) {
    fun toOpenAiDefinition(): Map<String, Any?> = mapOf(
        "type" to "function",
        "function" to mapOf(
            "name" to name,
            "description" to description,
            "parameters" to parameters,
        )
    )
}

data class ToolObservation(val text: String, val truncated: Boolean = false, val diff: String? = null, val changedFiles: List<String>? = null)

data class ToolArgs(val raw: String) {
    val root: JsonObject = json.parseToJsonElement(raw).jsonObject
}

data class ToolContext(
    val workspaceRoot: String,
    val agentName: String,
    val shell: ToolShellRunner,
    val git: ToolGitService,
    val messenger: ToolAgentMessenger? = null,
)

interface ITool {
    val descriptor: ToolDescriptor
    fun touchedPaths(args: ToolArgs): List<String>
    fun risksNetwork(args: ToolArgs): Boolean
    suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation
}

interface ToolAgentMessenger {
    suspend fun askAsync(from: String, to: String, question: String): String
}

class ToolRegistry(private val tools: Map<String, ITool>) {
    constructor(tools: Iterable<ITool>) : this(tools.associateBy { it.descriptor.name })

    fun tool(name: String): ITool? = tools[name]
    fun descriptors(): List<ToolDescriptor> = tools.values.map { it.descriptor }

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

        fun standardWithAskAgent(messenger: ToolAgentMessenger?): ToolRegistry {
            val base = standard().tools.toMutableMap()
            if (messenger != null) base[AskAgentTool.descriptor.name] = AskAgentTool()
            return ToolRegistry(base)
        }
    }
}

interface ToolShellRunner {
    suspend fun runAsync(command: String, workingDirectory: String): ShellResult
}

data class ShellResult(val stdOut: String, val stdErr: String, val exitCode: Int)

interface ToolGitService {
    suspend fun statusAsync(workspaceRoot: String): String
    suspend fun diffAsync(workspaceRoot: String): String
}

class ProcessShellRunner : ToolShellRunner {
    override suspend fun runAsync(command: String, workingDirectory: String): ShellResult = withContext(Dispatchers.IO) {
        val isWindows = System.getProperty("os.name").lowercase().contains("win")
        val pb = if (isWindows) ProcessBuilder("cmd", "/c", command) else ProcessBuilder("/bin/sh", "-c", command)
        pb.directory(File(workingDirectory))
        pb.redirectErrorStream(false)

        val proc = pb.start()
        val stdout = proc.inputStream.bufferedReader().readText()
        val stderr = proc.errorStream.bufferedReader().readText()
        val code = proc.waitFor()
        ShellResult(stdout, stderr, code)
    }
}

class ProcessGitService(private val shell: ToolShellRunner = ProcessShellRunner()) : ToolGitService {
    override suspend fun statusAsync(workspaceRoot: String): String =
        shell.runAsync("git status --porcelain=v1", workspaceRoot).stdOut

    override suspend fun diffAsync(workspaceRoot: String): String =
        shell.runAsync("git diff", workspaceRoot).stdOut
}

class ReadFileTool : ITool {
    override val descriptor = ToolDescriptor(
        "read_file",
        "Read a UTF-8 text file within the workspace.",
        SideEffect.READ_ONLY,
        mapOf(
            "type" to "object",
            "properties" to mapOf(
                "path" to mapOf("type" to "string"),
                "maxBytes" to mapOf("type" to "integer"),
            ),
            "required" to listOf("path"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(requiredString(args.root, "path", ""))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val path = requiredString(args.root, "path")
        val maxBytes = intValue(args.root, "maxBytes")
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val bytes = File(resolved).readBytes()
        val limit = if (maxBytes > 0) maxBytes else 100_000
        val truncated = bytes.size > limit
        val text = bytes.decodeToString(0, kotlin.math.min(bytes.size, limit))
        ToolObservation(text, truncated)
    }
}

class ListFilesTool : ITool {
    override val descriptor = ToolDescriptor(
        "list_files",
        "List entries of a directory within the workspace.",
        SideEffect.READ_ONLY,
        mapOf(
            "type" to "object",
            "properties" to mapOf("path" to mapOf("type" to "string")),
            "required" to emptyList<String>(),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(stringValue(args.root, "path", "."))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val path = stringValue(args.root, "path", ".")
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val file = File(resolved)
        val entries = file.list()?.toList() ?: emptyList()
        ToolObservation(entries.sorted().joinToString("\n"))
    }
}

class SearchTextTool : ITool {
    override val descriptor = ToolDescriptor(
        "search_text",
        "Search for a literal substring in text files under a workspace path.",
        SideEffect.READ_ONLY,
        mapOf(
            "type" to "object",
            "properties" to mapOf(
                "query" to mapOf("type" to "string"),
                "path" to mapOf("type" to "string"),
            ),
            "required" to listOf("query"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(stringValue(args.root, "path", "."))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val query = requiredString(args.root, "query")
        val path = stringValue(args.root, "path", ".")
        val root = File(WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path))
        val files = if (root.isFile) sequenceOf(root) else root.walkTopDown().filter { it.isFile }

        val results = mutableListOf<String>()
        val limit = 200
        for (file in files) {
            if (results.size >= limit) break
            val lines = runCatching { file.readLines() }.getOrNull() ?: continue
            for ((idx, line) in lines.withIndex()) {
                if (results.size >= limit) break
                if (line.contains(query, ignoreCase = false)) {
                    val rel = file.relativeTo(File(context.workspaceRoot)).path
                    results.add("$rel:${idx + 1}:$line")
                }
            }
        }
        if (results.isEmpty()) ToolObservation("(no matches)") else ToolObservation(results.joinToString("\n"), truncated = results.size >= limit)
    }
}

class WriteFileTool : ITool {
    override val descriptor = ToolDescriptor(
        "write_file",
        "Write (create or overwrite) a UTF-8 text file within the workspace.",
        SideEffect.WRITE,
        mapOf(
            "type" to "object",
            "properties" to mapOf(
                "path" to mapOf("type" to "string"),
                "content" to mapOf("type" to "string"),
            ),
            "required" to listOf("path", "content"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(requiredString(args.root, "path"))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val path = requiredString(args.root, "path")
        val content = requiredString(args.root, "content")
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val target = File(resolved)
        target.parentFile?.mkdirs()
        target.writeText(content)
        ToolObservation("wrote $path (${content.toByteArray().size} bytes)")
    }
}

class ApplyPatchTool : ITool {
    override val descriptor = ToolDescriptor(
        "apply_patch",
        "Apply a unified diff to files within the workspace.",
        SideEffect.WRITE,
        mapOf(
            "type" to "object",
            "properties" to mapOf("diff" to mapOf("type" to "string")),
            "required" to listOf("diff"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = parsePatch(requiredString(args.root, "diff")).map { it.path }.distinct()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val diff = requiredString(args.root, "diff")
        val patches = parsePatch(diff)
        if (patches.isEmpty()) throw IllegalStateException("no file sections found in diff")

        val changed = mutableListOf<String>()
        for (patch in patches) {
            val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, patch.path)
            val target = File(resolved)
            val original = runCatching { target.readText() }.getOrDefault("")
            val updated = applyPatch(original, patch.hunks)
            target.parentFile?.mkdirs()
            target.writeText(updated)
            changed.add(patch.path)
        }

        ToolObservation("applied patch to ${changed.joinToString(", ")}", changedFiles = changed, diff = diff)
    }

    private data class PatchFile(val path: String, val hunks: MutableList<Pair<List<String>, List<String>>>)

    private fun parsePatch(diff: String): List<PatchFile> {
        val files = mutableListOf<PatchFile>()
        var current: PatchFile? = null
        var inHunk = false
        var oldLines = mutableListOf<String>()
        var newLines = mutableListOf<String>()

        fun flushHunk() {
            current?.let {
                if (inHunk) {
                    it.hunks.add(oldLines.toList() to newLines.toList())
                }
            }
            oldLines = mutableListOf()
            newLines = mutableListOf()
            inHunk = false
        }

        for (raw in diff.lines()) {
            val line = raw.trimEnd('\r')
            when {
                line.startsWith("--- ") -> {
                    flushHunk()
                    if (current != null) files.add(current!!)
                    current = null
                }
                line.startsWith("+++ ") -> {
                    flushHunk()
                    var p = line.substring(4)
                    if (p.startsWith("b/")) p = p.substring(2)
                    current = PatchFile(p, mutableListOf())
                }
                line.startsWith("@@") -> {
                    if (inHunk) flushHunk()
                    inHunk = true
                }
                inHunk && current != null -> {
                    if (line.isNotEmpty()) {
                        when (line.first()) {
                            '+' -> newLines.add(line.substring(1))
                            '-' -> oldLines.add(line.substring(1))
                            ' ' -> {
                                val both = line.substring(1)
                                oldLines.add(both)
                                newLines.add(both)
                            }
                            else -> inHunk = false
                        }
                    } else {
                        oldLines.add("")
                        newLines.add("")
                    }
                }
            }
        }
        if (current != null) {
            if (inHunk) {
                current!!.hunks.add(oldLines.toList() to newLines.toList())
            }
            files.add(current!!)
        }
        return files
    }

    private fun findRange(source: List<String>, needle: List<String>): IntRange? {
        if (needle.isEmpty() || needle.size > source.size) return null
        for (start in 0..(source.size - needle.size)) {
            if (needle.indices.all { source[start + it] == needle[it] }) {
                return start until start + needle.size
            }
        }
        return null
    }

    private fun applyPatch(original: String, hunks: List<Pair<List<String>, List<String>>>): String {
        val lines = original.split("\n").toMutableList()
        for ((oldLines, newLines) in hunks) {
            if (oldLines.isEmpty()) {
                lines.addAll(newLines)
                continue
            }

            val range = findRange(lines, oldLines) ?: throw IllegalStateException("patch hunk did not match file content")
            for (i in 0 until oldLines.size) lines.removeAt(range.first)
            lines.addAll(range.first, newLines)
        }
        return lines.joinToString("\n")
    }
}

class RunShellTool : ITool {
    override val descriptor = ToolDescriptor(
        "run_shell",
        "Run a shell command in the workspace directory.",
        SideEffect.EXEC,
        mapOf(
            "type" to "object",
            "properties" to mapOf("command" to mapOf("type" to "string")),
            "required" to listOf("command"),
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = com.intatis.shared.security.ShellInspector.risksNetworkOrInstall(requiredString(args.root, "command"))

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val command = requiredString(args.root, "command")
        val result = context.shell.runAsync(command, context.workspaceRoot)
        val text = buildString {
            append(result.stdOut)
            if (result.stdErr.isNotBlank()) {
                append("\n[stderr]\n")
                append(result.stdErr)
            }
            append("\n[exit ${result.exitCode}]")
        }
        ToolObservation(text)
    }
}

class GitStatusTool : ITool {
    override val descriptor = ToolDescriptor(
        "git_status",
        "Show working-tree status (porcelain).",
        SideEffect.READ_ONLY,
        mapOf("type" to "object", "properties" to emptyMap<String, Any?>(), "required" to emptyList<String>())
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val text = context.git.statusAsync(context.workspaceRoot)
        ToolObservation(if (text.isBlank()) "clean" else text.trim())
    }
}

class GitDiffTool : ITool {
    override val descriptor = ToolDescriptor(
        "git_diff",
        "Show unstaged changes as unified diff.",
        SideEffect.READ_ONLY,
        mapOf("type" to "object", "properties" to emptyMap<String, Any?>(), "required" to emptyList<String>())
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val diff = context.git.diffAsync(context.workspaceRoot)
        if (diff.isBlank()) return@withContext ToolObservation("(no changes)")
        val truncated = diff.length > 200_000
        val text = if (truncated) diff.substring(0, 200_000) else diff
        ToolObservation(text, truncated = truncated, diff = diff)
    }
}

class AskAgentTool : ITool {
    companion object {
        val descriptor = ToolDescriptor(
            "ask_agent",
            "Ask another attached agent a question for answer.",
            SideEffect.READ_ONLY,
            mapOf(
                "type" to "object",
                "properties" to mapOf(
                    "to" to mapOf("type" to "string"),
                    "question" to mapOf("type" to "string"),
                ),
                "required" to listOf("to", "question"),
            )
        )
    }

    override val descriptor: ToolDescriptor = AskAgentTool.descriptor

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val messenger = context.messenger ?: return@withContext ToolObservation("agent messaging is not available in this session")
        val to = requiredString(args.root, "to")
        val question = requiredString(args.root, "question")
        val answer = runCatching { messenger.askAsync(context.agentName, to, question) }.getOrElse { "agent message failure: ${it.message}" }
        ToolObservation(answer)
    }
}

private fun requiredString(obj: JsonObject, key: String, fallback: String? = null): String {
    return obj[key]?.jsonPrimitive?.content ?: fallback ?: throw IllegalArgumentException("tool args missing '$key'")
}

private fun stringValue(obj: JsonObject, key: String, fallback: String): String = obj[key]?.jsonPrimitive?.content ?: fallback
private fun intValue(obj: JsonObject, key: String): Int = obj[key]?.jsonPrimitive?.intOrNull ?: -1
