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
import java.io.InputStream
import java.time.Instant
import java.net.HttpURLConnection
import java.net.URI
import java.net.URL

private val json = Json { ignoreUnknownKeys = false }

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

private fun ensureNoUnknownArgs(root: JsonObject, toolName: String, allowed: Set<String>) {
    val unknown = root.keys - allowed
    if (unknown.isNotEmpty()) {
        throw IllegalArgumentException("tool args for '$toolName' contains unsupported field(s): ${unknown.sorted().joinToString(", ")}")
    }
}

private fun requiredArg(root: JsonObject, key: String): String {
    return requiredString(root, key, null)
}

private fun requiredWorkspaceRelativePath(root: JsonObject, key: String): String {
    val filePath = requiredString(root, key, null, maxLength = 1_024, minLength = 1).trim()
    if (filePath.isBlank()) {
        throw IllegalArgumentException("tool args '$key' must not be blank")
    }

    if (
        filePath == "." ||
        filePath == ".." ||
        File(filePath).isAbsolute ||
        filePath.startsWith("//") ||
        filePath.startsWith("\\\\") ||
        Regex("^[A-Za-z]:[\\\\/]").containsMatchIn(filePath)
    ) {
        throw IllegalArgumentException("tool args '$key' must be workspace-relative")
    }

    val normalizedSegments = filePath
        .replace('\\', '/')
        .split('/')
        .map { it.trim() }
        .filter { it.isNotEmpty() }

    if (normalizedSegments.any { it == ".." }) {
        throw IllegalArgumentException("tool args '$key' must not contain path traversal segments")
    }

    return filePath
}

private fun optionalArg(root: JsonObject, key: String, fallback: String): String {
    return requiredString(root, key, fallback)
}

private fun optionalInt(root: JsonObject, key: String, defaultValue: Int, minimum: Int = Int.MIN_VALUE, maximum: Int = Int.MAX_VALUE): Int {
    val value = root[key] ?: return defaultValue
    val intValue = value.jsonPrimitive.intOrNull ?: throw IllegalArgumentException("tool args '$key' must be an integer")
    if (intValue !in minimum..maximum) {
        throw IllegalArgumentException("tool args '$key' must be in range [$minimum, $maximum]")
    }
    return intValue
}

private fun ensureParentDirectory(path: File) {
    path.parentFile?.mkdirs()
}

private fun parseAndValidateHttpUrl(rawUrl: String): URL {
    val trimmed = rawUrl.trim()
    if (trimmed.isBlank()) {
        throw IllegalArgumentException("tool args 'url' must not be blank")
    }

    val uri = runCatching { URI(trimmed) }.getOrElse {
        throw IllegalArgumentException("tool args 'url' must be a valid URL")
    }

    if (!uri.isAbsolute) {
        throw IllegalArgumentException("tool args 'url' must be an absolute HTTP(S) URL")
    }

    if (uri.scheme == null) {
        throw IllegalArgumentException("tool args 'url' must include a URL scheme, for example http or https")
    }

    val scheme = uri.scheme.lowercase()
    if (scheme != "http" && scheme != "https") {
        throw IllegalArgumentException("tool args 'url' must use http or https scheme")
    }

    if (uri.host.isNullOrBlank()) {
        throw IllegalArgumentException("tool args 'url' must include a host")
    }

    return runCatching { uri.toURL() }.getOrElse {
        throw IllegalArgumentException("tool args 'url' cannot be converted to URL")
    }
}

private fun readTextWithLimit(stream: InputStream, maxCharacters: Int): Pair<String, Boolean> {
    val reader = BufferedReader(InputStreamReader(stream))
    val output = StringBuilder()
    val buffer = CharArray(8_192)
    var truncated = false

    while (output.length < maxCharacters) {
        val read = reader.read(buffer)
        if (read == -1) break

        val remaining = maxCharacters - output.length
        if (read > remaining) {
            output.append(buffer, 0, remaining)
            truncated = true
            break
        }

        output.append(buffer, 0, read)
    }

    if (output.length >= maxCharacters && !truncated) {
        if (reader.read(buffer) != -1) {
            truncated = true
        }
    }

    reader.close()
    return Pair(output.toString(), truncated)
}

data class ToolContext(
    val workspaceRoot: String,
    val agentName: String,
    val shell: ToolShellRunner,
    val git: ToolGitService,
    val messenger: ToolAgentMessenger? = null,
    val imageGenerator: com.intatis.shared.ImageGenerationToolService? = null,
)

interface ITool {
    val descriptor: ToolDescriptor
    fun touchedPaths(args: ToolArgs): List<String>
    fun risksNetwork(args: ToolArgs): Boolean
    suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation
}

interface ToolAgentMessenger {
    suspend fun askAsync(from: String, to: String, question: String): String
    suspend fun sendMessageAsync(from: String, to: String, content: String): String
    suspend fun requestInformationAsync(from: String, to: String, question: String): String
    suspend fun replyMessageAsync(from: String, to: String, answer: String, inReplyTo: String? = null): String
    suspend fun requestDelegationAsync(from: String, objective: String, reason: String = "delegation requested"): String
    suspend fun delegateTaskAsync(from: String, to: String, objective: String, reason: String = "delegation requested"): String
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
                ReadPDFTool(),
                WebFetchTool(),
                UnsupportedTool("edit_pdf_pages", "Page-level PDF editing: extract selected pages to one PDF or split selected pages into one PDF per page.", SideEffect.WRITE),
                UnsupportedTool("reconstruct_document_image", "Convert a photographed/scanned document image into an editable document file using installed mature OCR/layout CLIs such as Docling, Marker, or Tesseract.", SideEffect.EXEC),
                UnsupportedTool("compile_latex", "Compile a LaTeX .tex file in the workspace to PDF using installed Tectonic, latexmk, xelatex, or pdflatex.", SideEffect.EXEC),
                GenerateImageTool(),
                BrowserDiagnosticsTool(),
            BrowserProfilesTool(),
            BrowserProfileDeleteTool(),
            BrowserHistoryTool(),
            BrowserNavigateTool(),
            BrowserSnapshotTool(),
            BrowserHandoffTool(),
            BrowserClickTool(),
            BrowserReloadTool(),
            BrowserBackTool(),
            BrowserForwardTool(),
            BrowserTypeTool(),
            BrowserSubmitTool(),
            BrowserSelectOptionTool(),
            BrowserPressKeyTool(),
            BrowserScrollTool(),
            BrowserWaitTool(),
            BrowserScreenshotTool(),
            BrowserUploadFileTool(),
            BrowserDownloadTool(),
            BrowserDownloadsTool(),
            BrowserSearchTool(),
            )
        )

        fun standardWithAskAgent(messenger: ToolAgentMessenger?): ToolRegistry {
            val base = standard().tools.toMutableMap()
            if (messenger != null) {
                base[AskAgentTool.descriptor.name] = AskAgentTool()
                base[SendMessageTool.descriptor.name] = SendMessageTool()
                base[RequestInformationTool.descriptor.name] = RequestInformationTool()
                base[ReplyMessageTool.descriptor.name] = ReplyMessageTool()
                base[RequestDelegationTool.descriptor.name] = RequestDelegationTool()
                base[DelegateTaskTool.descriptor.name] = DelegateTaskTool()
            }
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

class ProcessShellRunner : ToolShellRunner {
    override suspend fun runAsync(command: String, workingDirectory: String): ShellResult = withContext(Dispatchers.IO) {
        val os = System.getProperty("os.name").lowercase()
        val pb = when {
            os.contains("win") -> ProcessBuilder("cmd", "/c", command)
            os.contains("linux") || os.contains("mac") || os.contains("darwin") || os.contains("android") -> ProcessBuilder("/bin/sh", "-c", command)
            else -> throw UnsupportedOperationException("Shell runner unavailable on platform: $os")
        }
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

class InMemoryGitService(
    private val statusText: String = "",
    private val diffText: String = "",
) : ToolGitService {
    override suspend fun statusAsync(workspaceRoot: String): String = statusText
    override suspend fun diffAsync(workspaceRoot: String): String = diffText
}

class ReadFileTool : ITool {
    override val descriptor = ToolDescriptor(
        "read_file",
        "Read a UTF-8 text file within the workspace.",
        SideEffect.READ_ONLY,
        mapOf(
            "type" to "object",
        "properties" to mapOf(
                "path" to mapOf("type" to "string", "maxLength" to 1024, "minLength" to 1),
                "maxBytes" to mapOf("type" to "integer", "minimum" to 1, "maximum" to 1_000_000),
            ),
            "required" to listOf("path"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(requiredString(args.root, "path", maxLength = 1024, minLength = 1))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "read_file", setOf("path", "maxBytes"))
        val path = requiredString(args.root, "path", maxLength = 1024, minLength = 1)
        val maxBytes = optionalInt(args.root, "maxBytes", -1, minimum = 1, maximum = 1_000_000)
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
        "properties" to mapOf("path" to mapOf("type" to "string", "maxLength" to 1024, "minLength" to 1)),
            "required" to emptyList<String>(),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(stringValue(args.root, "path", ".", maxLength = 1024, minLength = 1))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "list_files", setOf("path"))
        val path = stringValue(args.root, "path", ".", maxLength = 1024, minLength = 1)
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
                "query" to mapOf("type" to "string", "maxLength" to 1_024, "minLength" to 1),
                "path" to mapOf("type" to "string", "maxLength" to 1_024, "minLength" to 1),
            ),
            "required" to listOf("query"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(stringValue(args.root, "path", ".", maxLength = 1024, minLength = 1))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "search_text", setOf("query", "path"))
        val query = requiredString(args.root, "query", maxLength = 1_024, minLength = 1)
        val path = stringValue(args.root, "path", ".", maxLength = 1_024, minLength = 1)
        val root = File(WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path))
        val files = if (root.isFile) {
            sequenceOf(root)
        } else {
            root.walkTopDown()
                .onEnter { file -> file == root || !isHiddenPath(file) }
                .filter { it.isFile && !isHiddenPath(it) }
        }

        val results = mutableListOf<String>()
        val limit = 200
        for (file in files) {
            if (results.size >= limit) break
            val lines = runCatching { file.readLines() }.getOrNull() ?: continue
            for ((idx, line) in lines.withIndex()) {
                if (results.size >= limit) break
                if (line.contains(query, ignoreCase = false)) {
                    val rel = file.relativeTo(File(context.workspaceRoot)).path
                    results.add("$rel:${idx + 1}: $line")
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
                "path" to mapOf("type" to "string", "maxLength" to 1_024, "minLength" to 1),
                "content" to mapOf("type" to "string", "maxLength" to 200_000),
            ),
            "required" to listOf("path", "content"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = listOf(requiredString(args.root, "path", maxLength = 1_024, minLength = 1))
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "write_file", setOf("path", "content"))
        val path = requiredString(args.root, "path", maxLength = 1_024, minLength = 1)
        val content = requiredString(args.root, "content", maxLength = 200_000)
        val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
        val target = File(resolved)
        ensureParentDirectory(target)
        target.writeText(content)
        ToolObservation("wrote ${content.toByteArray().size} bytes to $path", changedFiles = listOf(path))
    }
}

class ApplyPatchTool : ITool {
    override val descriptor = ToolDescriptor(
        "apply_patch",
        "Apply a unified diff to files within the workspace.",
        SideEffect.WRITE,
        mapOf(
            "type" to "object",
            "properties" to mapOf("diff" to mapOf("type" to "string", "maxLength" to 300_000, "minLength" to 1)),
            "required" to listOf("diff"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "apply_patch", setOf("diff"))
        val diff = requiredString(args.root, "diff", maxLength = 300_000, minLength = 1)
        return parsePatch(diff).map { it.path }
    }
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "apply_patch", setOf("diff"))
        val diff = requiredString(args.root, "diff", maxLength = 300_000, minLength = 1)
        val patches = parsePatch(diff)
        if (patches.isEmpty()) throw IllegalArgumentException("no file sections found in diff")

        val changed = mutableListOf<String>()
        for (patch in patches) {
            val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, patch.path)
            val target = File(resolved)
            val original = runCatching { target.readText() }.getOrDefault("")
            val updated = applyPatch(original, patch.hunks)
            ensureParentDirectory(target)
            target.writeText(updated)
            changed.add(patch.path)
        }

        ToolObservation("applied patch to: ${changed.joinToString(", ")}", changedFiles = changed, diff = diff)
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
                            else -> {
                                // Ignore non-standard hunk lines to match Apple patch parser behavior.
                            }
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
            "properties" to mapOf("command" to mapOf("type" to "string", "maxLength" to 8_000, "minLength" to 1)),
            "required" to listOf("command"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = com.intatis.shared.security.ShellInspector.risksNetworkOrInstall(requiredString(args.root, "command", maxLength = 8_000, minLength = 1))

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "run_shell", setOf("command"))
        val command = requiredString(args.root, "command", maxLength = 8_000, minLength = 1)
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
        val entries = parseGitStatus(context.git.statusAsync(context.workspaceRoot))
        ToolObservation(
            if (entries.isEmpty()) "clean"
            else entries.joinToString("\n") { "${it.x}${it.y} ${it.path}" }
        )
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
        val truncated = diff.toByteArray(Charsets.UTF_8).size > 200_000
        val text = if (truncated) diff.take(200_000) else diff
        ToolObservation(text, truncated = truncated, diff = diff)
    }
}

class GenerateImageTool : ITool {
    override val descriptor = ToolDescriptor(
        "generate_image",
        "Generate image files from a prompt using the configured image provider or injected local image model backend.",
        SideEffect.WRITE,
        unsupportedSchema("generate_image"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "generate_image", setOf("prompt", "outputPath", "size", "count"))
        return listOf(requiredString(args.root, "outputPath", maxLength = 1_024, minLength = 1))
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "generate_image", setOf("prompt", "outputPath", "size", "count"))
        val prompt = requiredString(args.root, "prompt", maxLength = 100_000, minLength = 1)
        val outputPath = requiredString(args.root, "outputPath", maxLength = 1_024, minLength = 1)
        val size = stringValue(args.root, "size", "1024x1024", maxLength = 64, minLength = 1)
            .trim()
            .ifBlank { "1024x1024" }
        val count = optionalInt(args.root, "count", 1, minimum = 1, maximum = 4)
        val generator = context.imageGenerator
            ?: throw IllegalStateException("generate_image is not configured; attach an image provider or local image backend before using this tool")

        val confinedOutputPath = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, outputPath)
        val result = generator.generateImage(prompt, size, count, confinedOutputPath, context.workspaceRoot)
        return ToolObservation(
            text = result.text,
            truncated = result.truncated,
            diff = result.diff,
            changedFiles = result.changedFiles,
        )
    }
}

class ReadPDFTool : ITool {
    override val descriptor = ToolDescriptor(
        "read_pdf",
        "Extract readable text from a PDF in the workspace, with optional 1-based page ranges such as '1-3,5'.",
        SideEffect.READ_ONLY,
        mapOf(
            "type" to "object",
            "properties" to mapOf(
                "path" to stringSchema(minLength = 1),
                "pages" to stringSchema(minLength = 1),
                "maxCharacters" to integerSchema(1, 500_000),
            ),
            "required" to listOf("path"),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "read_pdf", setOf("path", "pages", "maxCharacters"))
        return listOf(requiredString(args.root, "path", maxLength = 1_024, minLength = 1))
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, "read_pdf", setOf("path", "pages", "maxCharacters"))
        val path = requiredString(args.root, "path", maxLength = 1_024, minLength = 1)
        val rawPages = stringValue(args.root, "pages", "", maxLength = 1_024, minLength = 1).trim()
        val limit = optionalInt(args.root, "maxCharacters", 200_000, minimum = 1, maximum = 500_000)

    val resolved = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, path)
    ensurePdfBackendAvailable(context)
    val pageCount = resolvePageCount(context, resolved)
    if (pageCount <= 0) {
        throw IllegalStateException("could not determine a valid page count for $path")
    }
    val selectedPages = PageSelection.parse(rawPages.ifBlank { null }, pageCount)

        val lines = mutableListOf(
            "PDF: $path",
            "Pages: $pageCount",
            "Selected pages: ${selectedPages.joinToString(",") { (it + 1).toString() }}",
        )

        var missingText = false
        for (page in selectedPages) {
            val result = extractPdfPage(context, resolved, page + 1)
            if (result.exitCode != 0) {
                throw IllegalStateException(
                    "pdftotext failed for page ${page + 1}: ${outputText(result.stdOut, result.stdErr, result.exitCode)}"
                )
            }

            val pageText = result.stdOut.trim().ifEmpty { "(no extractable text on this page)" }
            if (pageText == "(no extractable text on this page)") {
                missingText = true
            }

            lines += "--- page ${page + 1} ---"
            lines += pageText
        }

        var text = lines.joinToString("\n")
        var truncated = false
        if (text.length > limit) {
            text = text.take(limit) + "\n[truncated]"
            truncated = true
        }

        if (missingText) {
            text += "\n\nHint: for scanned or photographed documents, use reconstruct_document_image with a Docling/Marker/Tesseract backend."
        }

        return ToolObservation(text, truncated = truncated)
    }
}

class WebFetchTool : ITool {
    override val descriptor = ToolDescriptor(
        "web_fetch",
        "Fetch an HTTP(S) URL without browser state. Use browser_* tools when login, JavaScript, cookies, or history are needed.",
        SideEffect.NETWORK,
        objectSchema(
            required = listOf("url"),
            properties = mapOf(
                "url" to stringSchema(minLength = 1),
                "maxCharacters" to integerSchema(1, 100_000),
            ),
        ),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "web_fetch", setOf("url", "maxCharacters"))
        parseAndValidateHttpUrl(requiredString(args.root, "url", maxLength = 4_096, minLength = 1))
        optionalInt(args.root, "maxCharacters", 20_000, minimum = 1, maximum = 100_000)
        return emptyList()
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "web_fetch", setOf("url", "maxCharacters"))
        val requestedUrl = requiredString(args.root, "url", maxLength = 4_096, minLength = 1)
        val url = parseAndValidateHttpUrl(requestedUrl)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 20_000, minimum = 1, maximum = 100_000)

        val connection = runCatching { url.openConnection() as? HttpURLConnection }.getOrNull()
            ?: throw IllegalStateException("web_fetch requires HTTP(S) URL support in this runtime")
        connection.requestMethod = "GET"
        connection.instanceFollowRedirects = true
        connection.connectTimeout = 15_000
        connection.readTimeout = 15_000
        connection.setRequestProperty("User-Agent", "IntatisAgent/0.16")

        return@withContext try {
            val responseCode = connection.responseCode
            val stream = runCatching { connection.inputStream }.getOrElse { connection.errorStream ?: throw it }
            val (bodyText, truncated) = readTextWithLimit(stream, maxCharacters)

            val contentType = connection.contentType ?: "unknown"
            val finalUrl = connection.url?.toString() ?: url.toString()
            val shown = if (truncated) bodyText + "\n[truncated]" else bodyText

            val text = buildString {
                append("status: ")
                append(responseCode)
                append("\n")
                append("url: ")
                append(finalUrl)
                append("\n")
                append("content-type: ")
                append(contentType)
                append("\n\n")
                append(if (shown.isBlank()) "(no readable response body)" else shown)
            }

            ToolObservation(text, truncated = truncated)
        } catch (error: Exception) {
            throw IllegalStateException("web_fetch failed for $requestedUrl: ${error.message ?: "network failure"}")
        } finally {
            connection.disconnect()
        }
    }
}

private suspend fun ensurePdfBackendAvailable(context: ToolContext) {
    val commandCheck = context.shell.runAsync("pdftotext -v", context.workspaceRoot)
    if (commandCheck.exitCode != 0) {
        throw IllegalStateException(
            "read_pdf requires pdftotext from Poppler or a compatible PDF text extraction CLI. " +
                "Install pdftotext (and keep workspace path permissions intact) before retrying."
        )
    }
}

private suspend fun resolvePageCount(context: ToolContext, resolvedPath: String): Int {
    val infoResult = context.shell.runAsync("pdfinfo ${shellQuote(resolvedPath)}", context.workspaceRoot)
    val output = listOf(infoResult.stdOut, infoResult.stdErr).joinToString("\n")
    val match = Regex("^\\s*Pages:\\s+(\\d+)\\b", RegexOption.MULTILINE).find(output)
        ?: throw IllegalStateException("could not determine PDF page count for ${resolvedPath}; is this a valid PDF?")
    return match.groupValues[1].toInt()
}

private suspend fun extractPdfPage(context: ToolContext, resolvedPath: String, oneBasedPage: Int): ShellResult {
    return context.shell.runAsync(
        "pdftotext -layout -nopgbrk -f $oneBasedPage -l $oneBasedPage ${shellQuote(resolvedPath)} -",
        context.workspaceRoot
    )
}

class UnsupportedTool(
    private val name: String,
    private val description: String,
    private val sideEffect: SideEffect,
) : ITool {
    override val descriptor = ToolDescriptor(
        name,
        description,
        sideEffect,
        unsupportedSchema(name),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, name, emptySet())
        return emptyList()
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation {
        ensureNoUnknownArgs(args.root, name, emptySet())
        return ToolObservation("$name is not implemented on Android yet.")
    }
}

class BrowserDiagnosticsTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_diagnostics",
        "Report local Node, Playwright, browser channel, profile, download, state, and history paths for the persistent browser backend.",
        SideEffect.EXEC,
        mapOf(
            "type" to "object",
            "properties" to mapOf(
                "profile" to stringSchema(minLength = 1),
                "channel" to stringSchema(minLength = 1),
            ),
            "required" to emptyList<String>(),
            "additionalProperties" to false,
        )
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_diagnostics", setOf("profile", "channel"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(".intatis/browser/profiles/$profile", ".intatis/browser/history.jsonl")
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_diagnostics", setOf("profile", "channel"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val downloadsDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/downloads/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        val diagnostics = executeBrowserDiagnostics(context, channel)
        val lines = browserDiagnosticsOutputLines(
            profile = profile,
            profileDir = profileDir,
            downloadsDir = downloadsDir,
            stateFile = stateFile,
            historyFile = historyFile,
            result = diagnostics,
        )
        ToolObservation(lines.joinToString("\n"))
    }
}

class BrowserProfilesTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_profiles",
        "List persistent browser profiles and safe metadata without reading cookies, localStorage, or browser profile databases.",
        SideEffect.READ_ONLY,
        unsupportedSchema("browser_profiles"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_profiles", setOf("profile", "limit", "includeProfileSize"))
        val profile = stringValueOrNull(args.root, "profile")
            ?.trim()
            ?.takeUnless { it.isBlank() }
            ?.let { normalizeBrowserProfile(it) }

        return if (profile == null) {
            listOf(
                ".intatis/browser/profiles",
                ".intatis/browser/downloads",
                ".intatis/browser/state",
                ".intatis/browser/history.jsonl",
            )
        } else {
            listOf(
                ".intatis/browser/profiles/$profile",
                ".intatis/browser/downloads/$profile",
                ".intatis/browser/state/$profile.json",
                ".intatis/browser/history.jsonl",
            )
        }
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_profiles", setOf("profile", "limit", "includeProfileSize"))
        val requestedProfile = stringValueOrNull(args.root, "profile")
            ?.trim()
            ?.takeUnless { it.isBlank() }
            ?.let { normalizeBrowserProfile(it) }
        val limit = optionalInt(args.root, "limit", 100, minimum = 1, maximum = 100)
        val includeProfileSize = optionalBoolean(args.root, "includeProfileSize", false)

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
        ToolObservation(lines.joinToString("\n"))
    }
}

class BrowserProfileDeleteTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_profile_delete",
        "Delete one workspace-scoped persistent browser profile, including its state, downloads, and Intatis history metadata. Requires confirmProfile to match profile.",
        SideEffect.DESTRUCTIVE,
        unsupportedSchema("browser_profile_delete"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_profile_delete", setOf("profile", "confirmProfile"))
        val profile = normalizeBrowserProfile(requiredArg(args.root, "profile"))
        val confirmProfile = normalizeBrowserProfile(requiredArg(args.root, "confirmProfile"))
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

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_profile_delete", setOf("profile", "confirmProfile"))
        val profile = normalizeBrowserProfile(requiredArg(args.root, "profile"))
        val confirmProfile = normalizeBrowserProfile(requiredArg(args.root, "confirmProfile"))
        if (profile != confirmProfile) {
            throw IllegalArgumentException("confirmProfile must match profile exactly")
        }

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val downloadsDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/downloads/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")
        val runtimeMetadata = describeBrowserProfileRuntimeMetadata(profileDir)
        if (runtimeMetadata.activeBrowserMarkerPresent || runtimeMetadata.profileLockMarkerPresent) {
            return@withContext ToolObservation(
                buildBrowserProfileDeleteBlockedOutput(
                    profile = profile,
                    runtimeMetadata = runtimeMetadata,
                ),
            )
        }

        val summary = deleteBrowserProfileData(
            profile = profile,
            profileDir = profileDir,
            downloadsDir = downloadsDir,
            stateFile = stateFile,
            historyFile = historyFile,
            runtimeMetadata = runtimeMetadata,
        )
        ToolObservation(buildBrowserProfileDeleteOutput(profile = profile, summary = summary).joinToString("\n"))
    }
}

class BrowserHistoryTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_history",
        "Read recent Intatis browser history metadata without exposing cookies, local storage, or credential files.",
        SideEffect.READ_ONLY,
        unsupportedSchema("browser_history"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_history", setOf("profile", "limit"))
        return listOf(".intatis/browser/history.jsonl")
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_history", setOf("profile", "limit"))
        val requestedProfile = stringValueOrNull(args.root, "profile")
            ?.trim()
            ?.takeUnless { it.isBlank() }
            ?.let { normalizeBrowserProfile(it) }
        val limit = optionalInt(args.root, "limit", 100, minimum = 1, maximum = 100)

        val (entries, matchedEntries) = readBrowserHistoryEntries(context.workspaceRoot, requestedProfile, limit)
        val lines = buildBrowserHistoryOutput(requestedProfile, limit, matchedEntries, entries)
        ToolObservation(lines.joinToString("\n"))
    }
}

private enum class BrowserHistoryNavigationDirection(val actionName: String, val offset: Int, val missingEntryMessage: String) {
    Back("back", -1, "no previous browser history entry for this profile"),
    Forward("forward", 1, "no next browser history entry for this profile"),
}

private data class BrowserNavigationSnapshot(
    val stack: List<String>,
    val index: Int,
    val currentUrl: String?,
)

class BrowserNavigateTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_navigate",
        "Open an HTTP(S) URL in a persistent Chromium/Chrome/Edge Playwright profile and return page text plus links.",
        SideEffect.EXEC,
        unsupportedSchema("browser_navigate"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_navigate", setOf("url", "profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_navigate", setOf("url", "profile", "channel", "headless", "waitMillis", "maxCharacters"))

        val requestedUrl = requiredArg(args.root, "url")
        validateHttpUrl(requestedUrl)

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val downloadsDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/downloads/$profile")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        File(downloadsDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserNavigateCommand(
            url = requestedUrl,
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_navigate failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserNavigateOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: requestedUrl

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "navigate",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserNavigateOutput(
                profile = profile,
                requestedUrl = requestedUrl,
                finalUrl = finalUrl,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserSnapshotTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_snapshot",
        "Reopen the current persistent browser profile and return the current page text plus links.",
        SideEffect.EXEC,
        unsupportedSchema("browser_snapshot"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_snapshot", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_snapshot", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val downloadsDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/downloads/$profile")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        File(downloadsDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserSnapshotCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_snapshot failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "snapshot",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserSnapshotOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserReloadTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_reload",
        "Reload the current page in a persistent Chromium/Chrome/Edge browser profile and return page text plus links.",
        SideEffect.EXEC,
        unsupportedSchema("browser_reload"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_reload", setOf("profile", "channel", "headless", "ignoreCache", "waitMillis", "maxCharacters"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_reload", setOf("profile", "channel", "headless", "ignoreCache", "waitMillis", "maxCharacters"))

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val ignoreCache = optionalBoolean(args.root, "ignoreCache", false)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val downloadsDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/downloads/$profile")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        File(downloadsDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserReloadCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            ignoreCache = ignoreCache,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )

        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_reload failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "reload",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserReloadOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserBackTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_back",
        "Go back to the previous URL recorded for a persistent Chromium/Chrome/Edge browser profile.",
        SideEffect.EXEC,
        objectSchema(
            required = emptyList(),
            properties = mapOf(
                "profile" to stringSchema(minLength = 1),
                "channel" to stringSchema(minLength = 1),
                "headless" to booleanSchema(),
                "waitMillis" to integerSchema(0, 10_000),
                "maxCharacters" to integerSchema(1, 100_000),
            ),
        ),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_back", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_back", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val targetUrl = executeBrowserHistoryNavigation(args, context, BrowserHistoryNavigationDirection.Back)
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserHistoryNavigationCommand(
            direction = BrowserHistoryNavigationDirection.Back,
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
            targetUrl = targetUrl,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_back failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: targetUrl

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "back",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserHistoryNavigationOutput(
                action = "back",
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserForwardTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_forward",
        "Go forward to the next URL recorded for a persistent Chromium/Chrome/Edge browser profile.",
        SideEffect.EXEC,
        objectSchema(
            required = emptyList(),
            properties = mapOf(
                "profile" to stringSchema(minLength = 1),
                "channel" to stringSchema(minLength = 1),
                "headless" to booleanSchema(),
                "waitMillis" to integerSchema(0, 10_000),
                "maxCharacters" to integerSchema(1, 100_000),
            ),
        ),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_forward", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_forward", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val targetUrl = executeBrowserHistoryNavigation(args, context, BrowserHistoryNavigationDirection.Forward)
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserHistoryNavigationCommand(
            direction = BrowserHistoryNavigationDirection.Forward,
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
            targetUrl = targetUrl,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_forward failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: targetUrl

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "forward",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserHistoryNavigationOutput(
                action = "forward",
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserClickTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_click",
        "Click an element in a persistent Chromium/Chrome/Edge browser profile by CSS selector, visible text, or accessibility role/name.",
        SideEffect.EXEC,
        unsupportedSchema("browser_click"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_click",
            setOf("profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_click",
            setOf("profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        val exact = optionalBoolean(args.root, "exact", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        if (selector.isBlank() && text.isBlank() && (role.isBlank() || name.isBlank())) {
            throw IllegalArgumentException("browser_click requires selector, text, or role+name")
        }

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserClickCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            selector = selector,
            text = text,
            role = role,
            name = name,
            exact = exact,
            nth = nth,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_click failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "click",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserClickOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserTypeTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_type",
        "Type or fill text into an element in a persistent Chromium/Chrome/Edge browser profile; avoid using this for passwords unless the user explicitly approves.",
        SideEffect.EXEC,
        unsupportedSchema("browser_type"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_type",
            setOf("value", "profile", "channel", "headless", "selector", "text", "role", "name", "clear", "submit", "nth", "waitMillis", "maxCharacters")
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_type",
            setOf("value", "profile", "channel", "headless", "selector", "text", "role", "name", "clear", "submit", "nth", "waitMillis", "maxCharacters")
        )
        val value = requiredString(args.root, "value", minLength = 1)
        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        if (selector.isBlank() && text.isBlank() && (role.isBlank() || name.isBlank())) {
            throw IllegalArgumentException("browser_type requires selector, text, or role+name")
        }

        val reason = browserTypeSensitiveTargetReason(selector = selector, text = text, role = role, name = name)
        if (reason != null) {
            throw IllegalArgumentException("browser_type refuses likely sensitive credential entry target ($reason); use browser_handoff for login, password, 2FA, token, or API key entry")
        }

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val clear = optionalBoolean(args.root, "clear", true)
        val submit = optionalBoolean(args.root, "submit", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserTypeCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            value = value,
            selector = selector,
            text = text,
            role = role,
            name = name,
            clear = clear,
            submit = submit,
            nth = nth,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_type failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "type",
            url = finalUrl,
            title = parsed.title,
        )
        ToolObservation(
            buildBrowserTypeOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
                typedValue = value,
            ),
        )
    }
}

class BrowserSubmitTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_submit",
        "Submit the current form in a persistent Chromium/Chrome/Edge browser profile by targeting a form control or submit button first.",
        SideEffect.EXEC,
        unsupportedSchema("browser_submit"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_submit",
            setOf("profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "timeoutMillis", "waitMillis", "maxCharacters")
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_submit",
            setOf("profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "timeoutMillis", "waitMillis", "maxCharacters")
        )
        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        if (selector.isBlank() && text.isBlank() && (role.isBlank() || name.isBlank())) {
            throw IllegalArgumentException("browser_submit requires selector, text, or role+name")
        }

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val exact = optionalBoolean(args.root, "exact", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val timeoutMillis = optionalInt(args.root, "timeoutMillis", 5_000, minimum = 1_000, maximum = 30_000)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserSubmitCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            selector = selector,
            text = text,
            role = role,
            name = name,
            exact = exact,
            nth = nth,
            timeoutMillis = timeoutMillis,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_submit failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "submit",
            url = finalUrl,
            title = parsed.title,
        )
        ToolObservation(
            buildBrowserSubmitOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserSelectOptionTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_select_option",
        "Select an option from a select/dropdown control in a persistent Chromium/Chrome/Edge browser profile by value, label, or index.",
        SideEffect.EXEC,
        unsupportedSchema("browser_select_option"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_select_option",
            setOf("optionValue", "optionLabel", "optionIndex", "profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_select_option",
            setOf("optionValue", "optionLabel", "optionIndex", "profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val optionValue = optionalString(args.root, "optionValue", "").trim()
        val optionLabel = optionalString(args.root, "optionLabel", "").trim()
        val optionIndex = optionalInt(args.root, "optionIndex", -1, minimum = -1, maximum = 500)
        if (optionValue.isBlank() && optionLabel.isBlank() && optionIndex < 0) {
            throw IllegalArgumentException("browser_select_option requires optionValue, optionLabel, or optionIndex")
        }

        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        if (selector.isBlank() && text.isBlank() && (role.isBlank() || name.isBlank())) {
            throw IllegalArgumentException("browser_select_option requires selector, text, or role+name")
        }

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val exact = optionalBoolean(args.root, "exact", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserSelectOptionCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            optionValue = optionValue,
            optionLabel = optionLabel,
            optionIndex = optionIndex,
            selector = selector,
            text = text,
            role = role,
            name = name,
            exact = exact,
            nth = nth,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_select_option failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "select",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserSelectOptionOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
                optionValue = optionValue,
                optionLabel = optionLabel,
                optionIndex = optionIndex,
            ),
        )
    }
}

class BrowserPressKeyTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_press_key",
        "Press a key or shortcut in a persistent Chromium/Chrome/Edge browser profile, optionally targeting an element first.",
        SideEffect.EXEC,
        unsupportedSchema("browser_press_key"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_press_key",
            setOf("key", "profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_press_key",
            setOf("key", "profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val key = normalizeBrowserKey(requiredString(args.root, "key", maxLength = 80, minLength = 1))
        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        val exact = optionalBoolean(args.root, "exact", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserPressKeyCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            key = key,
            selector = selector,
            text = text,
            role = role,
            name = name,
            exact = exact,
            nth = nth,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_press_key failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "press",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserPressKeyOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
                key = key,
            ),
        )
    }
}

class BrowserScrollTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_scroll",
        "Scroll the current persistent Chromium/Chrome/Edge browser page or a targeted element by direction/amount or explicit pixel deltas.",
        SideEffect.EXEC,
        unsupportedSchema("browser_scroll"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_scroll",
            setOf("direction", "amount", "deltaX", "deltaY", "profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_scroll",
            setOf("direction", "amount", "deltaX", "deltaY", "profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val direction = optionalString(args.root, "direction", "down").trim().lowercase()
        val amount = optionalInt(args.root, "amount", 900, minimum = 1, maximum = 10_000)
        val requestedDeltaX = optionalInt(args.root, "deltaX", 0, minimum = -10_000, maximum = 10_000)
        val requestedDeltaY = optionalInt(args.root, "deltaY", 0, minimum = -10_000, maximum = 10_000)
        val (deltaX, deltaY) = calculateBrowserScrollDelta(
            direction = direction,
            amount = amount,
            hasExplicitDeltaX = args.root.containsKey("deltaX"),
            hasExplicitDeltaY = args.root.containsKey("deltaY"),
            explicitDeltaX = requestedDeltaX,
            explicitDeltaY = requestedDeltaY,
        )
        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        val exact = optionalBoolean(args.root, "exact", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserScrollCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            selector = selector,
            text = text,
            role = role,
            name = name,
            exact = exact,
            nth = nth,
            deltaX = deltaX,
            deltaY = deltaY,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_scroll failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "scroll",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserScrollOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
                deltaX = deltaX,
                deltaY = deltaY,
            ),
        )
    }
}

class BrowserWaitTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_wait",
        "Wait in the persistent browser profile for text or an element state, or pause briefly for dynamic page updates.",
        SideEffect.EXEC,
        unsupportedSchema("browser_wait"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_wait",
            setOf("profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "state", "timeoutMillis", "waitMillis", "maxCharacters")
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_wait",
            setOf("profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "state", "timeoutMillis", "waitMillis", "maxCharacters")
        )
        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        val exact = optionalBoolean(args.root, "exact", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val state = normalizedBrowserWaitState(optionalString(args.root, "state", ""))
        val timeoutMillis = optionalInt(args.root, "timeoutMillis", 10_000, minimum = 1_000, maximum = 30_000)
        val waitMillis = optionalInt(args.root, "waitMillis", 100, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserWaitCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            selector = selector,
            text = text,
            role = role,
            name = name,
            exact = exact,
            nth = nth,
            state = state,
            timeoutMillis = timeoutMillis,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_wait failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "wait",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserWaitOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
                state = state,
                timeoutMillis = timeoutMillis,
            ),
        )
    }
}

class BrowserScreenshotTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_screenshot",
        "Capture a PNG screenshot of the current or requested page in a persistent Chromium/Chrome/Edge browser profile.",
        SideEffect.EXEC,
        unsupportedSchema("browser_screenshot"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_screenshot",
            setOf("outputPath", "url", "profile", "channel", "headless", "fullPage", "waitMillis", "maxCharacters")
        )
        val outputPath = requiredString(args.root, "outputPath", maxLength = 1_024, minLength = 1)
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            outputPath,
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_screenshot",
            setOf("outputPath", "url", "profile", "channel", "headless", "fullPage", "waitMillis", "maxCharacters")
        )

        val requestedUrl = optionalString(args.root, "url", "").trim().takeIf { it.isNotBlank() }
        if (requestedUrl != null) {
            validateHttpUrl(requestedUrl)
        }

        val outputPath = requiredString(args.root, "outputPath", maxLength = 1_024, minLength = 1).trim()

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val fullPage = optionalBoolean(args.root, "fullPage", false)
        val waitMillis = optionalInt(args.root, "waitMillis", 100, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")
        val outputFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, outputPath)

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)
        ensureParentDirectory(outputFile)

        val command = buildBrowserScreenshotCommand(
            requestedUrl = requestedUrl.orEmpty(),
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            outputPath = outputFile,
            fullPage = fullPage,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_screenshot failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: requestedUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "screenshot",
            url = finalUrl,
            title = parsed.title,
            screenshotPath = outputFile,
        )

        ToolObservation(
            buildBrowserScreenshotOutput(
                profile = profile,
                outputPath = outputFile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserUploadFileTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_upload_file",
        "Attach a workspace file to a file input in the persistent Chromium/Chrome/Edge browser profile.",
        SideEffect.EXEC,
        unsupportedSchema("browser_upload_file"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_upload_file",
            setOf("filePath", "profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val filePath = requiredWorkspaceRelativePath(args.root, "filePath")
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
            filePath,
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_upload_file",
            setOf("filePath", "profile", "channel", "headless", "selector", "text", "role", "name", "exact", "nth", "waitMillis", "maxCharacters")
        )
        val filePath = requiredWorkspaceRelativePath(args.root, "filePath")
        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        if (selector.isBlank() && text.isBlank() && (role.isBlank() || name.isBlank())) {
            throw IllegalArgumentException("browser_upload_file requires selector, text, or role+name")
        }

        val exact = optionalBoolean(args.root, "exact", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")
        val resolvedFilePath = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, filePath)

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val sourceFile = File(resolvedFilePath)
        if (!sourceFile.exists()) {
            throw IllegalArgumentException("browser_upload_file source file not found: $filePath")
        }
        if (sourceFile.isDirectory) {
            throw IllegalArgumentException("browser_upload_file source file must not be a directory: $filePath")
        }

        val command = buildBrowserUploadFileCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            filePath = resolvedFilePath,
            relativeFilePath = filePath,
            selector = selector,
            text = text,
            role = role,
            name = name,
            exact = exact,
            nth = nth,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_upload_file failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "upload",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserUploadFileOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserDownloadTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_download",
        "Click an element expected to start a download and save the file under the persistent browser profile downloads directory.",
        SideEffect.EXEC,
        unsupportedSchema("browser_download"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(
            args.root,
            "browser_download",
            setOf(
                "profile",
                "channel",
                "headless",
                "selector",
                "text",
                "role",
                "name",
                "exact",
                "nth",
                "waitMillis",
                "downloadTimeoutMillis",
                "maxCharacters",
            )
        )
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(
            args.root,
            "browser_download",
            setOf(
                "profile",
                "channel",
                "headless",
                "selector",
                "text",
                "role",
                "name",
                "exact",
                "nth",
                "waitMillis",
                "downloadTimeoutMillis",
                "maxCharacters",
            )
        )
        val selector = optionalString(args.root, "selector", "").trim()
        val text = optionalString(args.root, "text", "").trim()
        val role = optionalString(args.root, "role", "").trim()
        val name = optionalString(args.root, "name", "").trim()
        if (selector.isBlank() && text.isBlank() && (role.isBlank() || name.isBlank())) {
            throw IllegalArgumentException("browser_download requires selector, text, or role+name")
        }

        val exact = optionalBoolean(args.root, "exact", false)
        val nth = optionalInt(args.root, "nth", 0, minimum = 0, maximum = 100)
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val downloadTimeoutMillis = optionalInt(args.root, "downloadTimeoutMillis", 15_000, minimum = 1_000, maximum = 60_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")
        val downloadsDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/downloads/$profile")
        val downloadsRelativeDir = ".intatis/browser/downloads/$profile"

        File(profileDir).mkdirs()
        File(downloadsDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserDownloadCommand(
            profileDir = profileDir,
            stateFile = stateFile,
            downloadsDir = downloadsDir,
            downloadsRelativeDir = downloadsRelativeDir,
            channel = channel,
            headless = headless,
            selector = selector,
            text = text,
            role = role,
            name = name,
            exact = exact,
            nth = nth,
            waitMillis = waitMillis,
            downloadTimeoutMillis = downloadTimeoutMillis,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_download failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "download",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserDownloadOutput(
                profile = profile,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
            changedFiles = parsed.downloadedFiles,
        )
    }
}

class BrowserDownloadsTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_downloads",
        "List downloaded file metadata for a persistent browser profile without reading file contents.",
        SideEffect.READ_ONLY,
        unsupportedSchema("browser_downloads"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_downloads", setOf("profile", "limit"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_downloads", setOf("profile", "limit"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val limit = optionalInt(args.root, "limit", 100, minimum = 1, maximum = 100)

        val downloadsDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/downloads/$profile")
        val entries = listBrowserDownloads(
            downloadsDir = downloadsDir,
            workspaceRoot = context.workspaceRoot,
            limit = limit,
        )

        ToolObservation(
            buildBrowserDownloadsOutput(
                profile = profile,
                downloads = entries,
            ),
        )
    }
}

class BrowserSearchTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_search",
        "Search the web in a persistent Chromium/Chrome/Edge browser profile and return visible result text and links.",
        SideEffect.EXEC,
        unsupportedSchema("browser_search"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_search", setOf("query", "engine", "profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_search", setOf("query", "engine", "profile", "channel", "headless", "waitMillis", "maxCharacters"))
        val query = requiredString(args.root, "query", maxLength = 4_000, minLength = 1).trim()
        val engine = optionalString(args.root, "engine", "duckduckgo").trim().lowercase()
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val headless = optionalBoolean(args.root, "headless", true)
        val waitMillis = optionalInt(args.root, "waitMillis", 600, minimum = 0, maximum = 10_000)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)
        val normalizedEngine = when (engine) {
            "google", "bing", "duckduckgo" -> engine
            else -> "duckduckgo"
        }

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val encodedQuery = java.net.URLEncoder.encode(query, Charsets.UTF_8.toString())
        val searchUrl = when (normalizedEngine) {
            "google" -> "https://www.google.com/search?q=$encodedQuery"
            "bing" -> "https://www.bing.com/search?q=$encodedQuery"
            else -> "https://duckduckgo.com/?q=$encodedQuery"
        }

        val command = buildBrowserNavigateCommand(
            requestedUrl = searchUrl,
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            headless = headless,
            waitMillis = waitMillis,
            maxCharacters = maxCharacters,
        )

        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_search failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserNavigateOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: searchUrl

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "search",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserSearchOutput(
                profile = profile,
                query = query,
                engine = normalizedEngine,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

class BrowserHandoffTool : ITool {
    override val descriptor = ToolDescriptor(
        "browser_handoff",
        "Open a headed persistent Chromium/Chrome/Edge browser profile for user login or manual interaction, then return the resulting page snapshot.",
        SideEffect.EXEC,
        unsupportedSchema("browser_handoff"),
    )

    override fun touchedPaths(args: ToolArgs): List<String> {
        ensureNoUnknownArgs(args.root, "browser_handoff", setOf("url", "profile", "channel", "handoffSeconds", "maxCharacters"))
        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        return listOf(
            ".intatis/browser/profiles/$profile",
            ".intatis/browser/downloads/$profile",
            ".intatis/browser/state/$profile.json",
            ".intatis/browser/history.jsonl",
        )
    }

    override fun risksNetwork(args: ToolArgs): Boolean = true

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        ensureNoUnknownArgs(args.root, "browser_handoff", setOf("url", "profile", "channel", "handoffSeconds", "maxCharacters"))

        val requestedUrl = optionalString(args.root, "url", "").trim().takeIf { it.isNotBlank() }
        if (requestedUrl != null) {
            validateHttpUrl(requestedUrl)
        }

        val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
        val channel = normalizeBrowserChannel(optionalString(args.root, "channel", ""))
        val handoffSeconds = optionalInt(args.root, "handoffSeconds", 60, minimum = 1, maximum = 600)
        val maxCharacters = optionalInt(args.root, "maxCharacters", 100_000, minimum = 1, maximum = 100_000)

        val profileDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/profiles/$profile")
        val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")
        val downloadsDir = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/downloads/$profile")
        val historyFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/history.jsonl")

        File(profileDir).mkdirs()
        File(downloadsDir).mkdirs()
        ensureParentDirectory(stateFile)
        ensureParentDirectory(historyFile)

        val command = buildBrowserHandoffCommand(
            requestedUrl = requestedUrl.orEmpty(),
            profileDir = profileDir,
            stateFile = stateFile,
            channel = channel,
            handoffSeconds = handoffSeconds,
            maxCharacters = maxCharacters,
        )
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode != 0) {
            val stdout = result.stdOut.trim()
            val stderr = result.stdErr.trim()
            val reason = when {
                stderr.isNotBlank() -> stderr
                stdout.isNotBlank() -> stdout
                else -> "node command failed"
            }
            return@withContext ToolObservation("browser_handoff failed (exit ${result.exitCode}): $reason")
        }

        val parsed = parseBrowserSnapshotOutput(result.stdOut.trim())
        val finalUrl = parsed.finalUrl ?: requestedUrl ?: "about:blank"

        appendBrowserHistoryEntry(
            historyFile = historyFile,
            profile = profile,
            action = "handoff",
            url = finalUrl,
            title = parsed.title,
        )

        ToolObservation(
            buildBrowserHandoffOutput(
                profile = profile,
                requestedUrl = requestedUrl,
                result = parsed,
                maxCharacters = maxCharacters,
            ),
        )
    }
}

private fun executeBrowserHistoryNavigation(
    args: ToolArgs,
    context: ToolContext,
    direction: BrowserHistoryNavigationDirection,
): String {
    ensureNoUnknownArgs(args.root, "browser_${direction.actionName}", setOf("profile", "channel", "headless", "waitMillis", "maxCharacters"))
    val profile = normalizeBrowserProfile(optionalString(args.root, "profile", ""))
    val stateFile = WorkspaceSecurity.resolveInWorkspace(context.workspaceRoot, ".intatis/browser/state/$profile.json")

    return browserHistoryNavigationURL(
        direction = direction,
        profile = profile,
        stateFile = stateFile,
        workspaceRoot = context.workspaceRoot,
    )
}

private fun browserHistoryNavigationURL(
    direction: BrowserHistoryNavigationDirection,
    profile: String,
    stateFile: String,
    workspaceRoot: String,
): String {
    val snapshot = browserNavigationSnapshot(profile, stateFile, workspaceRoot)
    if (snapshot.stack.isEmpty()) {
        throw IllegalArgumentException("no current browser history for this profile; call browser_navigate or browser_search first")
    }

    val targetIndex = snapshot.index + direction.offset
    if (targetIndex !in snapshot.stack.indices) {
        throw IllegalArgumentException(direction.missingEntryMessage)
    }

    return snapshot.stack[targetIndex]
}

private fun browserNavigationSnapshot(
    profile: String,
    stateFile: String,
    workspaceRoot: String,
): BrowserNavigationSnapshot {
    val state = runCatching {
        json.parseToJsonElement(File(stateFile).readText()).jsonObject
    }.getOrNull()

    val currentURL = state?.get("url")
        ?.jsonPrimitive
        ?.content
        ?.trim()
        ?.takeIf { it.isNotBlank() }

    var stack = state
        ?.get("navigationStack")
        ?.jsonArray
        ?.mapNotNull { it.jsonPrimitive.contentOrNull }
        ?.map { it.trim() }
        ?.filter { it.isNotBlank() }
            ?: emptyList()

    if (stack.isEmpty()) {
        stack = browserHistoryStackFromMetadata(profile, workspaceRoot)
    }

    if (stack.isEmpty() && currentURL != null) {
        stack = listOf(currentURL)
    }

    val index = state?.get("navigationIndex")
        ?.jsonPrimitive
        ?.intOrNull
        ?: currentURL?.let { url -> stack.lastIndexOf(url).takeIf { it >= 0 } }
        ?: if (stack.isEmpty()) {
            0
        } else {
            stack.size - 1
        }

    val clampedIndex = if (stack.isEmpty()) {
        0
    } else {
        index.coerceIn(0, stack.size - 1)
    }

    return BrowserNavigationSnapshot(stack, clampedIndex, currentURL)
}

private fun browserHistoryStackFromMetadata(profile: String, workspaceRoot: String): List<String> {
    val (entries, _) = readBrowserHistoryEntries(
        workspaceRoot = workspaceRoot,
        requestedProfile = profile,
        limit = 1000,
    )

    val stack = ArrayList<String>()
    for (entry in entries) {
        val url = entry.url?.trim()?.takeIf { it.isNotBlank() } ?: continue
        if (stack.lastOrNull() != url) {
            stack.add(url)
        }
    }

    return stack
}

private fun readBrowserHistorySummaries(workspaceRoot: String): Map<String, BrowserHistorySummary> {
    val historyFile = File(WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/browser/history.jsonl"))
    if (!historyFile.isFile) return emptyMap()

    val summaries = linkedMapOf<String, BrowserHistorySummary>()
    try {
        historyFile.forEachLine { rawLine ->
            val line = rawLine.trim()
            if (line.isBlank()) return@forEachLine

            val lineJson = runCatching { json.parseToJsonElement(line).jsonObject }.getOrNull() ?: return@forEachLine
            val profile = lineJson["profile"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: return@forEachLine
            if (!isValidBrowserProfileName(profile)) {
                return@forEachLine
            }
            val summary = summaries.getOrPut(profile) { BrowserHistorySummary() }
            summary.entries += 1
            summary.latestTimestamp = lineJson["ts"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: summary.latestTimestamp
            summary.latestAction = lineJson["action"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: summary.latestAction
            summary.latestUrl = lineJson["url"]?.jsonPrimitive?.content?.trim()?.takeIf { it.isNotBlank() } ?: summary.latestUrl
        }
    } catch (_: IOException) {
        // Ignore history read/parse failures and return best-effort metadata.
    }

    return summaries
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

        val lineJson = runCatching { json.parseToJsonElement(trimmed).jsonObject }.getOrNull() ?: continue
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
        if (entry.action != null) {
            lines += "  action: ${entry.action}"
        }
        if (entry.url != null) {
            lines += "  url: ${entry.url}"
        }
        if (entry.title != null) {
            lines += "  title: ${entry.title}"
        }
    }

    return lines
}

private fun discoverBrowserProfiles(
    workspaceRoot: String,
    requestedProfile: String?,
    historySummaries: Map<String, BrowserHistorySummary>,
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
        if (!entry.isFile) {
            continue
        }
        if (!entry.name.lowercase().endsWith(".json")) {
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
    historySummaries: Map<String, BrowserHistorySummary>,
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
            json.parseToJsonElement(trimmed).jsonObject["profile"]?.jsonPrimitive?.content?.trim()
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

private data class BrowserNavigateResult(
    val title: String?,
    val finalUrl: String?,
    val text: String?,
    val links: List<BrowserNavigateLink>,
)

private data class BrowserNavigateLink(
    val text: String?,
    val url: String?,
)

private data class BrowserSnapshotResult(
    val title: String?,
    val finalUrl: String?,
    val text: String?,
    val links: List<BrowserNavigateLink>,
    val elements: List<BrowserInteractiveElement>,
    val uploadedFiles: List<String> = emptyList(),
    val downloadedFiles: List<String> = emptyList(),
)

private data class BrowserDownloadMetadata(
    val fileName: String,
    val path: String,
    val sizeBytes: Long,
    val lastModifiedMs: Long?,
)

private data class BrowserSnapshotLink(
    val text: String?,
    val url: String?,
)

private data class BrowserInteractiveElement(
    val role: String?,
    val name: String?,
    val text: String?,
    val selector: String?,
    val tag: String?,
)

private fun parseBrowserNavigateOutput(raw: String): BrowserNavigateResult {
    if (raw.isBlank()) {
        throw IllegalArgumentException("browser_navigate returned empty output")
    }

    val root = runCatching { json.parseToJsonElement(raw).jsonObject }.getOrElse {
        throw IllegalArgumentException("browser_navigate output was not valid JSON")
    }

    val links = root["links"]?.jsonArray
        ?.mapNotNull { link ->
            val linkObj = runCatching { link.jsonObject }.getOrNull() ?: return@mapNotNull null
            val url = runCatching { linkObj["url"]?.jsonPrimitive?.content?.trim() }.getOrNull()
            val text = runCatching { linkObj["text"]?.jsonPrimitive?.content?.trim() }.getOrNull()
            if (url.isNullOrBlank()) null else BrowserNavigateLink(text = text, url = url)
        } ?: emptyList()

    return BrowserNavigateResult(
        finalUrl = runCatching { root["finalUrl"]?.jsonPrimitive?.content?.trim() }.getOrNull(),
        title = runCatching { root["title"]?.jsonPrimitive?.content?.trim() }.getOrNull(),
        text = runCatching { root["text"]?.jsonPrimitive?.content?.trim() }.getOrNull(),
        links = links,
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
        lines += "title: ${result.title}"
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)
    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    return lines.joinToString("\n")
}

private fun buildBrowserSearchOutput(
    profile: String,
    query: String,
    engine: String,
    finalUrl: String,
    result: BrowserNavigateResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: search",
        "profile: $profile",
        "query: $query",
        "engine: $engine",
        "final url: $finalUrl",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)
    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    return lines.joinToString("\n")
}

private fun parseBrowserSnapshotOutput(raw: String): BrowserSnapshotResult {
    if (raw.isBlank()) {
        throw IllegalArgumentException("browser_snapshot returned empty output")
    }

    val root = runCatching { json.parseToJsonElement(raw).jsonObject }.getOrElse {
        throw IllegalArgumentException("browser_snapshot output was not valid JSON")
    }

    val links = root["links"]?.jsonArray
        ?.mapNotNull { link ->
            val linkObj = runCatching { link.jsonObject }.getOrNull() ?: return@mapNotNull null
            val url = runCatching { linkObj["url"]?.jsonPrimitive?.content?.trim() }.getOrNull()
            val text = runCatching { linkObj["text"]?.jsonPrimitive?.content?.trim() }.getOrNull()
            if (url.isNullOrBlank()) null else BrowserSnapshotLink(text = text, url = url)
        } ?: emptyList()

    val elements = root["elements"]?.jsonArray
        ?.mapNotNull { element ->
            val obj = runCatching { element.jsonObject }.getOrNull() ?: return@mapNotNull null
            val role = runCatching { obj["role"]?.jsonPrimitive?.content?.trim() }.getOrNull()
            val name = runCatching { obj["name"]?.jsonPrimitive?.content?.trim() }.getOrNull()
            val text = runCatching { obj["text"]?.jsonPrimitive?.content?.trim() }.getOrNull()
            val selector = runCatching { obj["selector"]?.jsonPrimitive?.content?.trim() }.getOrNull()
            val tag = runCatching { obj["tag"]?.jsonPrimitive?.content?.trim() }.getOrNull()

            if (role.isNullOrBlank() && name.isNullOrBlank() && text.isNullOrBlank() && selector.isNullOrBlank() && tag.isNullOrBlank()) {
                null
            } else {
                BrowserInteractiveElement(
                    role = role,
                    name = name,
                    text = text,
                    selector = selector,
                    tag = tag,
                )
            }
        } ?: emptyList()

    return BrowserSnapshotResult(
        finalUrl = runCatching { root["finalUrl"]?.jsonPrimitive?.content?.trim() }.getOrNull(),
        title = runCatching { root["title"]?.jsonPrimitive?.content?.trim() }.getOrNull(),
        text = runCatching { root["text"]?.jsonPrimitive?.content?.trim() }.getOrNull(),
        links = links.map { BrowserNavigateLink(it.text, it.url) },
        elements = elements,
        uploadedFiles = root["uploadedFiles"]?.jsonArray
            ?.mapNotNull { entry -> runCatching { entry.jsonPrimitive.content.trim() }.getOrNull() }
            ?.filter { it.isNotBlank() }
            ?: emptyList(),
        downloadedFiles = root["downloadedFiles"]?.jsonArray
            ?.mapNotNull { entry -> runCatching { entry.jsonPrimitive.content.trim() }.getOrNull() }
            ?.filter { it.isNotBlank() }
            ?: emptyList(),
    )
}

private fun buildBrowserUploadFileOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: upload",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "uploaded files:"
    if (result.uploadedFiles.isEmpty()) {
        lines += "(no uploaded files)"
    } else {
        for (index in result.uploadedFiles.indices.coerceAtMost(19)) {
            lines += "  ${(index + 1).toString().padStart(2, '0')}. ${result.uploadedFiles[index]}"
        }
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun listBrowserDownloads(
    downloadsDir: String,
    workspaceRoot: String,
    limit: Int,
): List<BrowserDownloadMetadata> {
    val directory = File(downloadsDir)
    if (!directory.isDirectory) {
        return emptyList()
    }

    return runCatching {
        directory
            .listFiles()
            ?.asSequence()
            ?.filter { it.isFile }
            ?.mapNotNull { file ->
                val lastModified = file.lastModified().takeIf { it > 0L }
                val relativePath = runCatching {
                    file.relativeTo(File(workspaceRoot)).path
                }.getOrElse {
                    file.path
                }

                BrowserDownloadMetadata(
                    fileName = file.name,
                    path = relativePath,
                    sizeBytes = file.length(),
                    lastModifiedMs = lastModified,
                )
            }
            ?.sortedWith(
                compareByDescending<BrowserDownloadMetadata> { it.lastModifiedMs ?: Long.MIN_VALUE }
                    .thenBy { it.fileName.lowercase() },
            )
            ?.take(limit)
            ?.toList()
            ?: emptyList()
    }.getOrElse {
        emptyList()
    }
}

private fun buildBrowserDownloadsOutput(
    profile: String,
    downloads: List<BrowserDownloadMetadata>,
): String {
    val lines = mutableListOf(
        "browser action: downloads",
        "profile: $profile",
        "status: ok",
        "count: ${downloads.size}",
    )

    lines += "downloaded files:"
    if (downloads.isEmpty()) {
        lines += "(no downloaded files)"
    } else {
        for (index in downloads.indices) {
            val download = downloads[index]
            val lastModified = formatLastModified(download.lastModifiedMs)
            lines += "  ${(index + 1).toString().padStart(2, '0')}. ${download.fileName} => ${download.path} (size=${download.sizeBytes}, lastModified=$lastModified)"
        }
    }

    return lines.joinToString("\n")
}

private fun buildBrowserDownloadOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: download",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "downloaded files:"
    if (result.downloadedFiles.isEmpty()) {
        lines += "(no downloaded files)"
    } else {
        for (index in result.downloadedFiles.indices.coerceAtMost(19)) {
            lines += "  ${(index + 1).toString().padStart(2, '0')}. ${result.downloadedFiles[index]}"
        }
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserClickOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: click",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserTypeOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
    typedValue: String,
): String {
    fun redact(value: String): String {
        if (typedValue.isBlank()) return value
        return value.replace(typedValue, "[redacted input]")
    }

    val lines = mutableListOf(
        "browser action: type",
        "profile: ${redact(profile)}",
        "final url: ${redact(result.finalUrl ?: "unknown")}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${redact(result.title)}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = redact(element.role?.ifBlank { "(no role)" } ?: "(no role)")
            val name = redact(element.name?.ifBlank { "(no name)" } ?: "(no name)")
            val text = redact(element.text?.ifBlank { "(no text)" } ?: "(no text)")
            val selector = redact(element.selector?.ifBlank { "(no selector)" } ?: "(no selector)")
            val tag = redact(element.tag?.ifBlank { "(no tag)" } ?: "(no tag)")
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = redact(link.text?.ifBlank { "(no text)" } ?: "(no text)")
            val url = redact(link.url?.ifBlank { "(no url)" } ?: "(no url)")
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = redact(result.text.orEmpty().ifBlank { "(no readable page text)" })
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserSubmitOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: submit",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserSelectOptionOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
    optionValue: String,
    optionLabel: String,
    optionIndex: Int,
): String {
    val lines = mutableListOf(
        "browser action: select",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    when {
        optionValue.isNotBlank() -> lines += "selected option value: $optionValue"
        optionLabel.isNotBlank() -> lines += "selected option label: $optionLabel"
        optionIndex >= 0 -> lines += "selected option index: $optionIndex"
    }
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserPressKeyOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
    key: String,
): String {
    val lines = mutableListOf(
        "browser action: press",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
        "key: $key",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserScrollOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
    deltaX: Int,
    deltaY: Int,
): String {
    val lines = mutableListOf(
        "browser action: scroll",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
        "delta x: $deltaX",
        "delta y: $deltaY",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserWaitOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
    state: String,
    timeoutMillis: Int,
): String {
    val lines = mutableListOf(
        "browser action: wait",
        "profile: $profile",
        "wait state: $state",
        "timeout millis: $timeoutMillis",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun browserTypeSensitiveTargetReason(selector: String?, text: String?, role: String?, name: String?): String? {
    val raw = listOfNotNull(selector, text, role, name)
        .map { it.trim().lowercase() }
        .filter { it.isNotEmpty() }
        .joinToString(" ")

    if (raw.isBlank()) return null

    val terms = raw
        .split(Regex("[^a-z0-9]+"))
        .filter { it.isNotBlank() }
        .toSet()

    if (terms.contains("password") || terms.contains("passwd") || terms.contains("pwd") || terms.contains("passcode")) {
        return "password field"
    }
    if (terms.contains("otp") || terms.contains("totp") || terms.contains("2fa") || terms.contains("mfa") || raw.contains("two factor") || raw.contains("two-factor")) {
        return "two-factor code field"
    }
    if (raw.contains("verification code") || raw.contains("security code") || raw.contains("authentication code") || raw.contains("recovery code") || raw.contains("backup code")) {
        return "verification code field"
    }
    if (terms.contains("token") ||
        terms.contains("secret") ||
        terms.contains("credential") ||
        terms.contains("apikey") ||
        (terms.contains("api") && terms.contains("key")) ||
        (terms.contains("private") && terms.contains("key"))
    ) {
        return "secret or token field"
    }

    return null
}

private fun buildBrowserSnapshotOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    return buildBrowserSnapshotActionOutput(profile = profile, action = "snapshot", result = result, maxCharacters = maxCharacters)
}

private fun buildBrowserHistoryNavigationOutput(
    action: String,
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    return buildBrowserSnapshotActionOutput(profile = profile, action = action, result = result, maxCharacters = maxCharacters)
}

private fun buildBrowserSnapshotActionOutput(
    profile: String,
    action: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: $action",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserScreenshotOutput(
    profile: String,
    outputPath: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: screenshot",
        "profile: $profile",
        "output path: $outputPath",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun buildBrowserReloadOutput(
    profile: String,
    result: BrowserSnapshotResult,
    maxCharacters: Int,
): String {
    val lines = mutableListOf(
        "browser action: reload",
        "profile: $profile",
        "final url: ${result.finalUrl ?: "unknown"}",
        "status: ok",
    )
    if (!result.title.isNullOrBlank()) {
        lines += "title: ${result.title}"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

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
        lines += "title: ${result.title}"
    }
    if (!requestedUrl.isNullOrBlank()) {
        lines += "requested url: $requestedUrl"
    }

    lines += "interactive elements:"
    if (result.elements.isEmpty()) {
        lines += "(no interactive elements)"
    } else {
        for (index in result.elements.indices.coerceAtMost(39)) {
            val element = result.elements[index]
            val role = element.role?.ifBlank { "(no role)" } ?: "(no role)"
            val name = element.name?.ifBlank { "(no name)" } ?: "(no name)"
            val text = element.text?.ifBlank { "(no text)" } ?: "(no text)"
            val selector = element.selector?.ifBlank { "(no selector)" } ?: "(no selector)"
            val tag = element.tag?.ifBlank { "(no tag)" } ?: "(no tag)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. role=$role | name=$name | text=$text | selector=$selector | tag=$tag"
        }
    }

    lines += "links:"
    if (result.links.isEmpty()) {
        lines += "(no links)"
    } else {
        for (index in result.links.indices.coerceAtMost(39)) {
            val link = result.links[index]
            val text = link.text?.ifBlank { "(no text)" } ?: "(no text)"
            val url = link.url?.ifBlank { "(no url)" } ?: "(no url)"
            lines += "  ${(index + 1).toString().padStart(2, '0')}. $text => $url"
        }
    }

    lines += "text:"
    val visibleText = result.text.orEmpty().ifBlank { "(no readable page text)" }
    lines += truncateBrowserText(visibleText, maxCharacters)

    return lines.joinToString("\n")
}

private fun truncateBrowserText(value: String, maxCharacters: Int): String {
    if (value.length <= maxCharacters) {
        return value
    }
    return value.take(maxCharacters) + "\n[truncated]"
}

private fun appendBrowserHistoryEntry(
    historyFile: String,
    profile: String,
    action: String,
    url: String,
    title: String?,
    screenshotPath: String? = null,
) {
    val entry = BrowserHistoryEntry(
        ts = Instant.now().toString(),
        profile = profile,
        action = action,
        url = url,
        title = title,
        screenshotPath = screenshotPath,
    )
    runCatching { File(historyFile).appendText(json.encodeToString(entry) + "\n") }
}

private fun validateHttpUrl(url: String) {
    if (!url.startsWith("http://", ignoreCase = true) && !url.startsWith("https://", ignoreCase = true)) {
        throw IllegalArgumentException("tool args url must be an http(s) URL")
    }
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
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const requestedUrl = (process.argv[2] || "").trim();
        const profileDir = process.argv[3] || "";
        const stateFile = process.argv[4] || "";
        const channel = (process.argv[5] || "chromium").toLowerCase();
        const headless = String(process.argv[6]).toLowerCase() === "true";
        const waitMillis = Number(process.argv[7] || "600");
        const maxCharacters = Number(process.argv[8] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        if (!/^https?:\\/\\//i.test(requestedUrl)) {
            console.error("browser_navigate requires an absolute HTTP(S) URL");
            process.exit(2);
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const page = await browser.newPage();
                    await page.goto(requestedUrl, { waitUntil: "domcontentloaded", timeout: 30000 });
                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const finalUrl = page.url();
                    const payload = await page.evaluate((limit) => {
                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = (bodyText || "").replace(/\\s+/g, " ").trim().slice(0, limit);
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text, url: href };
                            })
                            .filter((link) => link !== null);

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            links,
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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(url)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserClickCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    selector: String,
    text: String,
    role: String,
    name: String,
    exact: Boolean,
    nth: Int,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const selector = process.argv[6] || "";
        const text = process.argv[7] || "";
        const role = process.argv[8] || "";
        const name = process.argv[9] || "";
        const exact = String(process.argv[10]).toLowerCase() === "true";
        const nth = Number(process.argv[11] || "0");
        const waitMillis = Number(process.argv[12] || "600");
        const maxCharacters = Number(process.argv[13] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\"/g, '\\\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role, { name, exact }).nth(index);
            }
            if (text) {
                return page.getByText(text, { exact }).nth(index);
            }
            throw new Error("selector, role+name, or text is required for browser_click");
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        async function followNewPageDuring(context, page, action) {
            const beforePages = context.pages();
            const popupPromise = page.waitForEvent("popup", { timeout: 5000 }).catch(() => null);
            const pagePromise = context.waitForEvent("page", { timeout: 5000 }).catch(() => null);
            await action();
            const opened = await Promise.race([
                popupPromise,
                pagePromise,
                page.waitForTimeout(1200).then(() => null)
            ]);
            const selected = opened || context.pages().find((candidate) => !beforePages.includes(candidate)) || null;
            if (!selected) {
                return { page, openedPage: null };
            }
            await selected.waitForLoadState("domcontentloaded", { timeout: 15000 }).catch(() => undefined);
            await selected.bringToFront().catch(() => undefined);
            return { page: selected, openedPage: null };
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const existingPages = await browser.pages();
                    const activePage = (existingPages && existingPages.length > 0) ? existingPages[0] : await browser.newPage();
                    const followed = await followNewPageDuring(browser, activePage, async () => {
                        const locator = locatorFor(activePage);
                        await locator.click({ timeout: 15000 });
                    });
                    const page = followed.page;

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (exact) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserTypeCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    value: String,
    selector: String,
    text: String,
    role: String,
    name: String,
    clear: Boolean,
    submit: Boolean,
    nth: Int,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const value = process.argv[6] || "";
        const selector = process.argv[7] || "";
        const text = process.argv[8] || "";
        const role = process.argv[9] || "";
        const name = process.argv[10] || "";
        const clear = String(process.argv[11]).toLowerCase() === "true";
        const submit = String(process.argv[12]).toLowerCase() === "true";
        const nth = Number(process.argv[13] || "0");
        const waitMillis = Number(process.argv[14] || "600");
        const maxCharacters = Number(process.argv[15] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\\"/g, '\\\\\\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role).nth(index);
            }
            if (text) {
                return page.getByText(text).nth(index);
            }
            throw new Error("selector, role+name, or text is required for browser_type");
        }

        function sensitiveTypeReasonForElement(node) {
            function lower(value) {
                return String(value || "").toLowerCase();
            }
            function tokens(value) {
                return new Set(lower(value).split(/[^a-z0-9]+/).filter(Boolean));
            }
            function labelText(node) {
                if (!node || !node.getAttribute) return "";
                const parts = [
                    node.getAttribute("type"),
                    node.getAttribute("name"),
                    node.getAttribute("id"),
                    node.getAttribute("autocomplete"),
                    node.getAttribute("placeholder"),
                    node.getAttribute("aria-label"),
                    node.getAttribute("title")
                ];
                if (node.labels && node.labels.length) {
                    parts.push(Array.from(node.labels).map((label) => label.innerText || label.textContent || "").join(" "));
                }
                return parts.filter(Boolean).join(" ");
            }
            const raw = labelText(node);
            const loweredRaw = lower(raw);
            const termSet = tokens(raw);
            if (termSet.has("password") || termSet.has("passwd") || termSet.has("pwd") || termSet.has("passcode")) return "password field";
            if (termSet.has("otp") || termSet.has("totp") || termSet.has("2fa") || termSet.has("mfa") || loweredRaw.includes("two factor") || loweredRaw.includes("two-factor")) return "two-factor code field";
            if (loweredRaw.includes("verification code") || loweredRaw.includes("security code") || loweredRaw.includes("authentication code") || loweredRaw.includes("recovery code") || loweredRaw.includes("backup code")) return "verification code field";
            if (termSet.has("token") || termSet.has("secret") || termSet.has("credential") || termSet.has("apikey") || (termSet.has("api") && termSet.has("key")) || (termSet.has("private") && termSet.has("key"))) {
                return "secret or token field";
            }
            return "";
        }

        async function typeLocatorFor(page) {
            const locator = locatorFor(page);
            const reason = await locator.evaluate(sensitiveTypeReasonForElement);
            if (reason) {
                throw new Error("browser_type refuses likely sensitive credential entry target (" + reason + "); use browser_handoff for login, password, 2FA, token, or API key entry");
            }
            return locator;
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();
                    const locator = await typeLocatorFor(page);
                    await locator.evaluate((element, config) => {
                        const typed = String(config.value || "");
                        element.focus();
                        if ("value" in element) {
                            element.value = config.clear ? typed : String(element.value || "") + typed;
                        } else if (element.textContent !== undefined) {
                            element.textContent = config.clear ? typed : String(element.textContent || "") + typed;
                        } else {
                            throw new Error("selected element has no editable value");
                        }
                        element.dispatchEvent(new Event("input", { bubbles: true }));
                        element.dispatchEvent(new Event("change", { bubbles: true }));
                        if (config.submit) {
                            if (element.form && typeof element.form.requestSubmit === "function") {
                                element.form.requestSubmit();
                            } else {
                                element.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true }));
                            }
                        }
                    }, { value, clear, submit });

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(value)} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (clear) "true" else "false"} " +
        "${if (submit) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserSubmitCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    selector: String,
    text: String,
    role: String,
    name: String,
    exact: Boolean,
    nth: Int,
    timeoutMillis: Int,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const selector = process.argv[6] || "";
        const text = process.argv[7] || "";
        const role = process.argv[8] || "";
        const name = process.argv[9] || "";
        const exact = String(process.argv[10]).toLowerCase() === "true";
        const nth = Number(process.argv[11] || "0");
        const timeoutMillis = clampTimeout(Number(process.argv[12] || "5000"));
        const waitMillis = Number(process.argv[13] || "600");
        const maxCharacters = Number(process.argv[14] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\\"/g, '\\\\\\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role, { name, exact }).nth(index);
            }
            if (text) {
                return page.getByText(text, { exact }).nth(index);
            }
            throw new Error("selector, role+name, or text is required for browser_submit");
        }

        function clampTimeout(value) {
            return Math.max(1000, Math.min(30000, Number(value || 5000)));
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        async function followNewPageDuring(context, page, action) {
            const beforePages = context.pages();
            const popupPromise = page.waitForEvent("popup", { timeout: 5000 }).catch(() => null);
            const pagePromise = context.waitForEvent("page", { timeout: 5000 }).catch(() => null);
            await action();
            const opened = await Promise.race([
                popupPromise,
                pagePromise,
                page.waitForTimeout(1200).then(() => null)
            ]);
            const selected = opened || context.pages().find((candidate) => !beforePages.includes(candidate)) || null;
            if (!selected) {
                return { page, openedPage: null };
            }
            await selected.waitForLoadState("domcontentloaded", { timeout: 15000 }).catch(() => undefined);
            await selected.bringToFront().catch(() => undefined);
            return { page: selected, openedPage: null };
        }

        async function submitByLocator(page) {
            const locator = locatorFor(page);
            await locator.waitFor({ state: "attached", timeout: timeoutMillis });

            await locator.evaluate((element) => {
                const form = (element && element.form) || (element && element.closest ? element.closest("form") : null);
                if (form) {
                    if (typeof form.requestSubmit === "function") {
                        form.requestSubmit(element);
                        return;
                    }
                    if (typeof form.submit === "function") {
                        form.submit();
                        return;
                    }
                }

                const submitTarget = element && element.closest ? element.closest("button, input[type=\"submit\"], input[type=\"button\"], [role=\"button\"]") : null;
                if (submitTarget && typeof submitTarget.click === "function") {
                    submitTarget.click();
                    return;
                }

                if (element && typeof element.dispatchEvent === "function") {
                    element.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true }));
                    element.dispatchEvent(new KeyboardEvent("keypress", { key: "Enter", bubbles: true }));
                    element.dispatchEvent(new KeyboardEvent("keyup", { key: "Enter", bubbles: true }));
                    return;
                }

                throw new Error("browser_submit target could not be submitted");
            });
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const existingPages = await browser.pages();
                    const activePage = (existingPages && existingPages.length > 0) ? existingPages[0] : await browser.newPage();
                    const followed = await followNewPageDuring(browser, activePage, async () => {
                        await submitByLocator(activePage);
                    });
                    const page = followed.page;

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (exact) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "${timeoutMillis} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserSelectOptionCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    optionValue: String,
    optionLabel: String,
    optionIndex: Int,
    selector: String,
    text: String,
    role: String,
    name: String,
    exact: Boolean,
    nth: Int,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const optionValue = (process.argv[6] || "").trim();
        const optionLabel = (process.argv[7] || "").trim();
        const optionIndex = Number(process.argv[8] || "-1");
        const selector = process.argv[9] || "";
        const text = process.argv[10] || "";
        const role = process.argv[11] || "";
        const name = process.argv[12] || "";
        const exact = String(process.argv[13]).toLowerCase() === "true";
        const nth = Number(process.argv[14] || "0");
        const waitMillis = Number(process.argv[15] || "600");
        const maxCharacters = Number(process.argv[16] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\\"/g, '\\\\\\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role, { name, exact }).nth(index);
            }
            if (text) {
                return page.getByText(text, { exact }).nth(index);
            }
            throw new Error("selector, role+name, or text is required for browser_select_option");
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        function isSelectElement(element) {
            return Boolean(element && element.tagName && element.tagName.toLowerCase() === "select");
        }

        async function selectByLocator(page) {
            const locator = locatorFor(page);
            await locator.waitFor({ state: "attached", timeout: 15000 });
            const isSelect = await locator.evaluate((element) => {
                return Boolean(element && element.tagName && element.tagName.toLowerCase() === "select");
            });
            if (!isSelect) {
                throw new Error("browser_select_option requires a select element");
            }

            if (optionValue.length > 0) {
                await locator.selectOption({ value: optionValue });
                return;
            }
            if (optionLabel.length > 0) {
                await locator.selectOption({ label: optionLabel });
                return;
            }
            if (Number.isInteger(optionIndex) && optionIndex >= 0) {
                await locator.selectOption({ index: optionIndex });
                return;
            }
            throw new Error("browser_select_option requires optionValue, optionLabel, or optionIndex");
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();
                    await selectByLocator(page);

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(optionValue)} " +
        "${shellQuote(optionLabel)} " +
        "${optionIndex} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (exact) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserPressKeyCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    key: String,
    selector: String,
    text: String,
    role: String,
    name: String,
    exact: Boolean,
    nth: Int,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const key = (process.argv[6] || "").trim();
        const selector = process.argv[7] || "";
        const text = process.argv[8] || "";
        const role = process.argv[9] || "";
        const name = process.argv[10] || "";
        const exact = String(process.argv[11]).toLowerCase() === "true";
        const nth = Number(process.argv[12] || "0");
        const waitMillis = Number(process.argv[13] || "600");
        const maxCharacters = Number(process.argv[14] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\\"/g, '\\\\\\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role, { name, exact }).nth(index);
            }
            if (text) {
                return page.getByText(text, { exact }).nth(index);
            }
            return null;
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        async function pressByLocator(page) {
            const locator = locatorFor(page);
            if (!locator) {
                await page.keyboard.press(key);
                return;
            }
            await locator.waitFor({ state: "attached", timeout: 15000 });
            await locator.press(key);
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();
                    await pressByLocator(page);

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(key)} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (exact) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserScrollCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    selector: String,
    text: String,
    role: String,
    name: String,
    exact: Boolean,
    nth: Int,
    deltaX: Int,
    deltaY: Int,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const selector = process.argv[6] || "";
        const text = process.argv[7] || "";
        const role = process.argv[8] || "";
        const name = process.argv[9] || "";
        const exact = String(process.argv[10]).toLowerCase() === "true";
        const nth = Number(process.argv[11] || "0");
        const deltaX = Number(process.argv[12] || "0");
        const deltaY = Number(process.argv[13] || "0");
        const waitMillis = Number(process.argv[14] || "600");
        const maxCharacters = Number(process.argv[15] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\\"/g, '\\\\\\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role, { name, exact }).nth(index);
            }
            if (text) {
                return page.getByText(text, { exact }).nth(index);
            }
            return null;
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        async function scrollByLocator(page) {
            const locator = locatorFor(page);
            if (!locator) {
                await page.mouse.wheel(deltaX, deltaY);
                return;
            }

            await locator.waitFor({ state: "attached", timeout: 15000 });
            await locator.scrollIntoViewIfNeeded();
            const scrolled = await locator.evaluate((element, config) => {
                const node = element;
                if (!node) {
                    return false;
                }
                if (typeof node.scrollBy === "function") {
                    node.scrollBy(config.deltaX, config.deltaY);
                    return true;
                }
                if (typeof node.scrollTo === "function") {
                    node.scrollTo({
                        left: (node.scrollLeft || 0) + config.deltaX,
                        top: (node.scrollTop || 0) + config.deltaY,
                        behavior: "auto",
                    });
                    return true;
                }
                return false;
            }, { deltaX, deltaY });

            if (!scrolled) {
                await page.mouse.wheel(deltaX, deltaY);
            }
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();
                    await scrollByLocator(page);

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (exact) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "$deltaX " +
        "$deltaY " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserWaitCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    selector: String,
    text: String,
    role: String,
    name: String,
    exact: Boolean,
    nth: Int,
    state: String,
    timeoutMillis: Int,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const selector = process.argv[6] || "";
        const text = process.argv[7] || "";
        const role = process.argv[8] || "";
        const name = process.argv[9] || "";
        const exact = String(process.argv[10]).toLowerCase() === "true";
        const nth = Number(process.argv[11] || "0");
        const state = ["attached", "detached", "visible", "hidden"].includes(String(process.argv[12] || "visible").toLowerCase())
            ? String(process.argv[12] || "visible").toLowerCase()
            : "visible";
        const timeoutMillis = Number(process.argv[13] || "10000");
        const waitMillis = Number(process.argv[14] || "100");
        const maxCharacters = Number(process.argv[15] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\\"/g, '\\\\\\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role, { name, exact }).nth(index);
            }
            if (text) {
                return page.getByText(text, { exact }).nth(index);
            }
            return null;
        }

        async function waitForTarget(page) {
            const locator = locatorFor(page);
            if (!locator) {
                await page.waitForTimeout(Math.max(0, timeoutMillis));
                return;
            }
            await locator.waitFor({
                state,
                timeout: Math.max(1000, Math.min(Number(timeoutMillis) || 10000, 30000)),
            });
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();
                    await waitForTarget(page);

                    if (waitMillis > 0) {
                        await page.waitForTimeout(Math.max(0, waitMillis));
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (exact) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "${shellQuote(state)} " +
        "$timeoutMillis " +
        "$waitMillis " +
        "$maxCharacters"
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
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const waitMillis = Number(process.argv[6] || "600");
        const maxCharacters = Number(process.argv[7] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\s+/g, " ").trim();
        }

        function summarizeElementText(node) {
            const text = node && node.textContent ? normalizeText(node.textContent) : "";
            return text ? text.slice(0, 200) : "";
        }

        function summarizeInputType(node) {
            const type = node.getAttribute("type") || "";
            const name = node.getAttribute("name") || "";
            const placeholder = node.getAttribute("placeholder") || "";
            return normalizeText([type, name, placeholder].filter(Boolean).join(" "));
        }

        function deriveElementName(node, role) {
            const byAria = node.getAttribute("aria-label");
            const byTitle = node.getAttribute("title");
            const byPlaceholder = node.getAttribute("placeholder");
            const byLabel = summarizeInputType(node);
            return normalizeText(byAria || byTitle || byPlaceholder || byLabel);
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return "";
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/"/g, "\\\"")}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();
                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
                                const elementSelector = (
                                    element.id ? "#" + element.id :
                                    (element.className ? "." + String(element.className).trim().split(/\s+/).filter(Boolean).slice(0, 2).join(".") : tag)
                                );
                                return {
                                    role: role || null,
                                    name: name || null,
                                    text: text || null,
                                    selector: elementSelector || null,
                                    tag: tag || null,
                                };
                            })
                            .filter((entry) => entry.role || entry.name || entry.text || entry.selector || entry.tag);

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizeText = (value) => String(value || "").replace(/\s+/g, " ").trim();
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserUploadFileCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    filePath: String,
    relativeFilePath: String,
    selector: String,
    text: String,
    role: String,
    name: String,
    exact: Boolean,
    nth: Int,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const filePath = process.argv[6] || "";
        const selector = process.argv[7] || "";
        const text = process.argv[8] || "";
        const role = process.argv[9] || "";
        const name = process.argv[10] || "";
        const exact = String(process.argv[11]).toLowerCase() === "true";
        const nth = Number(process.argv[12] || "0");
        const waitMillis = Number(process.argv[13] || "600");
        const maxCharacters = Number(process.argv[14] || "100000");
        const relativeFilePath = process.argv[15] || "";

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\"/g, '\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role, { name, exact }).nth(index);
            }
            if (text) {
                return page.getByText(text, { exact }).nth(index);
            }
            throw new Error("selector, role+name, or text is required for browser_upload_file");
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();
                    const locator = locatorFor(page);
                    await locator.setInputFiles(filePath, { timeout: 15000 });

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                return { text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

                    await browser.storageState({ path: stateFile });
                    const output = {
                        finalUrl: payload.finalUrl,
                        title: payload.title,
                        text: payload.text,
                        links: payload.links,
                        elements: payload.elements,
                        uploadedFiles: [relativeFilePath || filePath],
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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(filePath)} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (exact) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "$waitMillis " +
        "$maxCharacters " +
        "${shellQuote(relativeFilePath)}"
}

private fun buildBrowserDownloadCommand(
    profileDir: String,
    stateFile: String,
    downloadsDir: String,
    downloadsRelativeDir: String,
    channel: String,
    headless: Boolean,
    selector: String,
    text: String,
    role: String,
    name: String,
    exact: Boolean,
    nth: Int,
    waitMillis: Int,
    downloadTimeoutMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const selector = process.argv[6] || "";
        const text = process.argv[7] || "";
        const role = process.argv[8] || "";
        const name = process.argv[9] || "";
        const exact = String(process.argv[10]).toLowerCase() === "true";
        const nth = Number(process.argv[11] || "0");
        const waitMillis = Number(process.argv[12] || "600");
        const downloadTimeoutMillis = Number(process.argv[13] || "15000");
        const maxCharacters = Number(process.argv[14] || "100000");
        const downloadsDir = process.argv[15] || "";
        const downloadsRelativeDir = process.argv[16] || downloadsDir;

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\"/g, '\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function locatorFor(page) {
            const index = Math.max(0, Math.floor(nth));
            if (selector) {
                return page.locator(selector).nth(index);
            }
            if (role && name) {
                return page.getByRole(role, { name, exact }).nth(index);
            }
            if (text) {
                return page.getByText(text, { exact }).nth(index);
            }
            throw new Error("selector, role+name, or text is required for browser_download");
        }

        function clampCharacters(value) {
            return Math.max(1, Math.min(100000, Number(value || 100000)));
        }

        function sanitizeFileName(value) {
            return String(value || "download")
                .replace(/[\\\/:*?"<>|]/g, "_")
                .replace(/\s+/g, " ")
                .trim()
                .slice(0, 150)
                .replace(/^\.+/, "")
                .replace(/^$/, "download");
        }

        function uniqueDownloadPath(dir, baseName) {
            const safeBase = sanitizeFileName(baseName);
            let name = safeBase;
            let candidate = path.join(dir, name);
            let counter = 0;
            while (fs.existsSync(candidate)) {
                counter += 1;
                const dot = safeBase.lastIndexOf(".");
                if (dot > 0) {
                    candidate = path.join(
                        dir,
                        `${safeBase.slice(0, dot)}-${counter}${safeBase.slice(dot)}`,
                    );
                } else {
                    candidate = path.join(dir, `${safeBase}-${counter}`);
                }
            }
            return candidate;
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }
                if (downloadsDir.length > 0) {
                    fs.mkdirSync(downloadsDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();
                    const locator = locatorFor(page);
                    const downloadPromise = page.waitForEvent("download", { timeout: Math.max(1000, downloadTimeoutMillis) });
                    await locator.click({ timeout: 15000 });
                    const download = await downloadPromise;

                    const suggestedFileName = download.suggestedFilename ? download.suggestedFilename() : "download";
                    const finalDownloadPath = uniqueDownloadPath(downloadsDir, suggestedFileName);
                    await download.saveAs(finalDownloadPath);

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, clampCharacters(maxCharacters));

                    await browser.storageState({ path: stateFile });
                    const output = {
                        finalUrl: payload.finalUrl,
                        title: payload.title,
                        text: payload.text,
                        links: payload.links,
                        elements: payload.elements,
                        downloadedFiles: [path.relative(process.cwd(), finalDownloadPath)],
                        downloadsRelativeDir,
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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(selector)} " +
        "${shellQuote(text)} " +
        "${shellQuote(role)} " +
        "${shellQuote(name)} " +
        "${if (exact) "true" else "false"} " +
        "${if (nth > 0) nth else 0} " +
        "$waitMillis " +
        "$downloadTimeoutMillis " +
        "$maxCharacters " +
        "${shellQuote(downloadsDir)} " +
        "${shellQuote(downloadsRelativeDir)}"
}

private fun buildBrowserScreenshotCommand(
    requestedUrl: String,
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    outputPath: String,
    fullPage: Boolean,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const requestedUrl = (process.argv[2] || "").trim();
        const profileDir = process.argv[3] || "";
        const stateFile = process.argv[4] || "";
        const channel = (process.argv[5] || "chromium").toLowerCase();
        const headless = String(process.argv[6]).toLowerCase() === "true";
        const outputPath = process.argv[7] || "";
        const fullPage = String(process.argv[8]).toLowerCase() === "true";
        const waitMillis = Number(process.argv[9] || "600");
        const maxCharacters = Number(process.argv[10] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\s+/g, " ").trim();
        }

        function summarizeElementText(node) {
            const text = node && node.textContent ? normalizeText(node.textContent) : "";
            return text ? text.slice(0, 200) : "";
        }

        function summarizeInputType(node) {
            const type = node.getAttribute("type") || "";
            const name = node.getAttribute("name") || "";
            const placeholder = node.getAttribute("placeholder") || "";
            return normalizeText([type, name, placeholder].filter(Boolean).join(" "));
        }

        function deriveElementName(node, role) {
            const byAria = node.getAttribute("aria-label");
            const byTitle = node.getAttribute("title");
            const byPlaceholder = node.getAttribute("placeholder");
            const byLabel = summarizeInputType(node);
            return normalizeText(byAria || byTitle || byPlaceholder || byLabel);
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return "";
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/"/g, "\\\"")}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        function validateUrl(raw) {
            if (!/^https?:\/\//i.test(raw)) {
                console.error("browser_screenshot requires an absolute HTTP(S) URL");
                process.exit(2);
            }
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = pages && pages.length > 0 ? pages[0] : await browser.newPage();

                    if (requestedUrl.length > 0) {
                        validateUrl(requestedUrl);
                        await page.goto(requestedUrl, { waitUntil: "domcontentloaded", timeout: 30000 });
                    }

                    if (waitMillis > 0) {
                        await page.waitForTimeout(Math.max(0, waitMillis));
                    }

                    if (!outputPath) {
                        console.error("browser_screenshot requires outputPath");
                        process.exit(2);
                    }

                    const outputDir = path.dirname(outputPath);
                    if (outputDir && outputDir !== ".") {
                        fs.mkdirSync(outputDir, { recursive: true });
                    }

                    await page.screenshot({
                        path: outputPath,
                        fullPage: fullPage,
                    });

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = summarizeElementText(element);
                                const tag = (element.tagName || "").trim().toLowerCase();
                                const elementSelector = inferSelector(element);

                                return {
                                    role: role || null,
                                    name: name || null,
                                    text: text || null,
                                    selector: elementSelector || null,
                                    tag: tag || null,
                                };
                            })
                            .filter((entry) => entry.role || entry.name || entry.text || entry.selector || entry.tag);

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
                            links: links,
                            elements: elements,
                        };
                    }, Math.max(1, Math.min(100000, maxCharacters)));

                    await browser.storageState({ path: stateFile });
                    const output = {
                        requestedUrl: requestedUrl,
                        finalUrl: payload.finalUrl,
                        screenshotPath: outputPath,
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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(requestedUrl)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${shellQuote(outputPath)} " +
        "${if (fullPage) "true" else "false"} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserReloadCommand(
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    ignoreCache: Boolean,
    waitMillis: Int,
    maxCharacters: Int,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const profileDir = process.argv[2] || "";
        const stateFile = process.argv[3] || "";
        const channel = (process.argv[4] || "chromium").toLowerCase();
        const headless = String(process.argv[5]).toLowerCase() === "true";
        const ignoreCache = String(process.argv[6]).toLowerCase() === "true";
        const waitMillis = Number(process.argv[7] || "600");
        const maxCharacters = Number(process.argv[8] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\"/g, '\\\\\"')}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = (pages && pages.length > 0) ? pages[0] : await browser.newPage();
                    await page.reload({
                        waitUntil: "domcontentloaded",
                        timeout: 30000,
                        ignoreCache
                    });
                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizeText = (value) => String(value || "").replace(/\\s+/g, " ").trim();
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "${if (ignoreCache) "true" else "false"} " +
        "$waitMillis " +
        "$maxCharacters"
}

private fun buildBrowserHistoryNavigationCommand(
    direction: BrowserHistoryNavigationDirection,
    profileDir: String,
    stateFile: String,
    channel: String,
    headless: Boolean,
    waitMillis: Int,
    maxCharacters: Int,
    targetUrl: String,
): String {
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const action = (process.argv[2] || "back").toLowerCase();
        const profileDir = process.argv[3] || "";
        const stateFile = process.argv[4] || "";
        const channel = (process.argv[5] || "chromium").toLowerCase();
        const headless = String(process.argv[6]).toLowerCase() === "true";
        const waitMillis = Number(process.argv[7] || "600");
        const maxCharacters = Number(process.argv[8] || "100000");
        const targetUrl = process.argv[9] || "";

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\\s+/g, " ").trim();
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return null;
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/\"/g, "\\\\\"")}"]`;
                }
            }
            return node.tagName.toLowerCase();
        }

        (async () => {
            try {
                const options = {
                    headless,
                    ignoreHTTPSErrors: true,
                };
                const normalizedChannel = normalizeChannel(channel);
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const pages = await browser.pages();
                    const page = (pages && pages.length > 0) ? pages[0] : await browser.newPage();

                    let result;
                    if (action === "back") {
                        result = await page.goBack({ waitUntil: "domcontentloaded", timeout: 30000 });
                    } else {
                        result = await page.goForward({ waitUntil: "domcontentloaded", timeout: 30000 });
                    }
                    if (!result) {
                        if (action === "back") {
                            throw new Error("no previous browser history entry for this profile");
                        }
                        throw new Error("no next browser history entry for this profile");
                    }

                    if (waitMillis > 0) {
                        await page.waitForTimeout(waitMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
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

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizeText = (value) => String(value || "").replace(/\\s+/g, " ").trim();
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: "",
                            links: links,
                            elements: elements,
                        };
                    }, Math.max(1, Math.min(100000, maxCharacters)));

                    payload.finalUrl = finalUrl || (targetUrl || "");
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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${if (direction == BrowserHistoryNavigationDirection.Back) "back" else "forward"} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "${if (headless) "true" else "false"} " +
        "$waitMillis " +
        "$maxCharacters " +
        "${shellQuote(targetUrl)}"
}

private fun buildBrowserHandoffCommand(
    requestedUrl: String,
    profileDir: String,
    stateFile: String,
    channel: String,
    handoffSeconds: Int,
    maxCharacters: Int,
): String {
    val normalizedTimeout = Math.max(1, handoffSeconds) * 1000
    val script = """
        const { chromium } = require("playwright");
        const fs = require("fs");
        const path = require("path");

        const requestedUrl = (process.argv[2] || "").trim();
        const profileDir = process.argv[3] || "";
        const stateFile = process.argv[4] || "";
        const channel = (process.argv[5] || "chromium").toLowerCase();
        const handoffTimeoutMillis = Number(process.argv[6] || "60000");
        const maxCharacters = Number(process.argv[7] || "100000");

        function normalizeChannel(raw) {
            if (raw === "chrome" || raw === "chrome-beta" || raw === "chrome-dev" || raw === "chrome-canary") {
                return "chrome";
            }
            if (raw === "msedge" || raw === "msedge-beta" || raw === "msedge-dev" || raw === "msedge-canary") {
                return "msedge";
            }
            return "chromium";
        }

        function normalizeText(value) {
            return String(value || "").replace(/\\s+/g, " ").trim();
        }

        function summarizeElementText(node) {
            const text = node && node.textContent ? normalizeText(node.textContent) : "";
            return text ? text.slice(0, 200) : "";
        }

        function summarizeInputType(node) {
            const type = node.getAttribute("type") || "";
            const name = node.getAttribute("name") || "";
            const placeholder = node.getAttribute("placeholder") || "";
            return normalizeText([type, name, placeholder].filter(Boolean).join(" "));
        }

        function deriveElementName(node, role) {
            const byAria = node.getAttribute("aria-label");
            const byTitle = node.getAttribute("title");
            const byPlaceholder = node.getAttribute("placeholder");
            const byLabel = summarizeInputType(node);
            return normalizeText(byAria || byTitle || byPlaceholder || byLabel);
        }

        function inferSelector(node) {
            if (!node || !node.tagName) {
                return "";
            }
            if (node.id) {
                return "#" + node.id;
            }
            if (node.name) {
                const attribute = normalizeText(node.name);
                if (attribute) {
                    return `${node.tagName.toLowerCase()}[name="${attribute.replace(/"/g, "\\\"")}"]`;
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
                if (normalizedChannel !== "chromium") {
                    options.channel = normalizedChannel;
                }

                if (requestedUrl && !/^https?:\\/\\//i.test(requestedUrl)) {
                    console.error("browser_handoff requires an absolute HTTP(S) URL");
                    process.exit(2);
                }

                if (profileDir.length > 0) {
                    fs.mkdirSync(profileDir, { recursive: true });
                }
                const stateDir = path.dirname(stateFile);
                if (stateDir && stateDir !== ".") {
                    fs.mkdirSync(stateDir, { recursive: true });
                }

                const browser = await chromium.launchPersistentContext(profileDir, options);
                try {
                    const existingPages = await browser.pages();
                    const page = (existingPages && existingPages.length > 0) ? existingPages[0] : await browser.newPage();
                    if (requestedUrl) {
                        await page.goto(requestedUrl, { waitUntil: "domcontentloaded", timeout: 30000 });
                    }
                    if (handoffTimeoutMillis > 0) {
                        await page.waitForTimeout(handoffTimeoutMillis);
                    }

                    const payload = await page.evaluate((limit) => {
                        const allCandidates = Array.from(document.querySelectorAll("a[href], button, input, select, textarea, option, label, summary"));
                        const links = Array.from(document.querySelectorAll("a[href]"))
                            .slice(0, 40)
                            .map((link) => {
                                const href = (link.href || "").trim();
                                if (!href) {
                                    return null;
                                }
                                const text = (link.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                return { text: text, url: href };
                            })
                            .filter((link) => link !== null);

                        const elements = allCandidates
                            .slice(0, 40)
                            .map((element) => {
                                const role = (element.getAttribute("role") || element.getAttribute("type") || "").trim();
                                const name = (element.getAttribute("aria-label") || element.getAttribute("name") || element.getAttribute("title") || "").trim();
                                const text = (element.textContent || "").replace(/\\s+/g, " ").trim().slice(0, 200);
                                const tag = (element.tagName || "").trim().toLowerCase();
                                const elementSelector = (
                                    element.id ? "#" + element.id :
                                    (element.className ? "." + String(element.className).trim().split(/\\s+/).filter(Boolean).slice(0, 2).join(".") : tag)
                                );
                                return {
                                    role: role || null,
                                    name: name || null,
                                    text: text || null,
                                    selector: elementSelector || null,
                                    tag: tag || null,
                                };
                            })
                            .filter((entry) => entry.role || entry.name || entry.text || entry.selector || entry.tag);

                        const bodyText = document.body && document.body.innerText ? document.body.innerText : "";
                        const normalizeText = (value) => String(value || "").replace(/\\s+/g, " ").trim();
                        const normalizedText = normalizeText(bodyText).slice(0, Math.max(1, Math.min(100000, limit)));

                        return {
                            text: normalizedText,
                            title: document.title || "",
                            finalUrl: window.location.href || "",
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
    """.trimIndent().replace("\n", " ")

    return "node -e ${shellQuote(script)} " +
        "${shellQuote(requestedUrl)} " +
        "${shellQuote(profileDir)} " +
        "${shellQuote(stateFile)} " +
        "${shellQuote(channel)} " +
        "$normalizedTimeout " +
        "$maxCharacters"
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
    val runtimeMarker = File(profileDir, "DevToolsActivePort").exists()
    val singletonMarker = profileDir.listFiles()?.any { it.name.startsWith("Singleton") } == true
    return BrowserProfileRuntimeMetadata(
        activeBrowserMarkerPresent = runtimeMarker,
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

private data class BrowserHistorySummary(
    var entries: Int = 0,
    var latestTimestamp: String? = null,
    var latestAction: String? = null,
    var latestUrl: String? = null,
)

private data class BrowserHistoryEntry(
    val ts: String?,
    val profile: String,
    val action: String?,
    val url: String?,
    val title: String?,
    val screenshotPath: String?,
)

private data class BrowserProfileDeleteSummary(
    val removedProfileData: Boolean,
    val removedDownloads: Boolean,
    val removedState: Boolean,
    val removedHistoryEntries: Int,
    val keptHistoryEntries: Int,
    val runtimeBeforeDelete: BrowserProfileRuntimeMetadata,
)

private data class BrowserDirectoryMetadata(
    val exists: Boolean,
    val fileCount: Int,
    val sizeBytes: Long?,
    val lastModifiedMs: Long?,
)

private data class BrowserFileMetadata(
    val exists: Boolean,
    val sizeBytes: Long?,
    val lastModifiedMs: Long?,
)

private data class BrowserProfileInventory(
    val profile: String,
    val profileDir: String,
    val profileDirectory: BrowserDirectoryMetadata,
    val stateFile: String,
    val stateFileMetadata: BrowserFileMetadata,
    val downloadsDir: String,
    val downloadsMetadata: BrowserDirectoryMetadata,
    val runtimeMetadata: BrowserProfileRuntimeMetadata,
    val historySummary: BrowserHistorySummary?,
)

private fun describeDirectoryMetadata(path: String, includeSize: Boolean): BrowserDirectoryMetadata {
    val directory = File(path)
    if (!directory.isDirectory) {
        return BrowserDirectoryMetadata(false, 0, if (includeSize) 0L else null, null)
    }

    var fileCount = 0
    var sizeBytes = 0L
    val stack = mutableListOf(directory)
    while (stack.isNotEmpty()) {
        val current = stack.removeAt(stack.lastIndex)
        val children = runCatching { current.listFiles() }.getOrNull() ?: continue
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

    return BrowserDirectoryMetadata(
        exists = true,
        fileCount = fileCount,
        sizeBytes = if (includeSize) sizeBytes else null,
        lastModifiedMs = directory.lastModified().takeIf { it > 0 },
    )
}

private fun describeFileMetadata(path: String): BrowserFileMetadata {
    val file = File(path)
    if (!file.isFile) {
        return BrowserFileMetadata(false, null, null)
    }
    return BrowserFileMetadata(
        exists = true,
        sizeBytes = file.length().coerceAtLeast(0),
        lastModifiedMs = file.lastModified().takeIf { it > 0 },
    )
}

private fun formatLastModified(lastModifiedMs: Long?): String = lastModifiedMs?.toString() ?: "n/a"

private fun optionalBoolean(root: JsonObject, key: String, default: Boolean): Boolean {
    val raw = root[key] ?: return default
    val primitive = raw as? JsonPrimitive ?: throw IllegalArgumentException("tool args '$key' must be a boolean")
    if (!primitive.isBoolean) {
        throw IllegalArgumentException("tool args '$key' must be a boolean")
    }
    return primitive.booleanOrNull ?: default
}

private fun stringValueOrNull(root: JsonObject, key: String): String? {
    val raw = root[key] ?: return null
    val primitive = raw as? JsonPrimitive ?: throw IllegalArgumentException("tool args '$key' must be a string")
    if (!primitive.isString) {
        throw IllegalArgumentException("tool args '$key' must be a string")
    }
    return primitive.content
}

private suspend fun executeBrowserDiagnostics(
    context: ToolContext,
    channel: String,
): BrowserDiagnosticsData {
    val checkedLocations = mutableListOf<String>()
    val (platform, arch) = runPlatformMetadata(context)
    val nodeResult = runShellCommand(context, "node -v")
    val nodeVersion = nodeResult?.trim()?.takeIf { it.isNotBlank() }

    var playwrightVersion: String? = null
    var playwrightResolvedFrom: String? = null
    var playwrightAvailable = false
    val playwrightCommand = "node -e \"try { const pkg = require('playwright/package.json'); console.log(pkg.version); console.log(require.resolve('playwright/package.json')); } catch (e) { process.exit(1); }\""
    runShellCommand(context, playwrightCommand)?.let { out ->
        val lines = out.lines().map { it.trim() }.filter { it.isNotBlank() }
        if (lines.isNotEmpty()) {
            playwrightVersion = lines.firstOrNull()
            playwrightResolvedFrom = lines.getOrNull(1)
            playwrightAvailable = true
        }
    } ?: run {
        checkedLocations += playwrightCommand
    }
    if (nodeVersion != null) {
        checkedLocations += playwrightCommand
    }

    var nodeWebSocketAvailable = false
    val nodeWebSocketCommand =
        "node -e \"try { require('ws'); console.log('yes'); process.exit(0); } catch (e) { console.error('no'); process.exit(1); }\""
    runShellCommand(context, nodeWebSocketCommand)?.let { stdout ->
        nodeWebSocketAvailable = stdout.trim().equals("yes", ignoreCase = true)
    } ?: run {
        checkedLocations += nodeWebSocketCommand
    }
    checkedLocations += nodeWebSocketCommand

    val appProbes = mutableMapOf<String, Boolean>()
    val browserExecutable = resolveBrowserExecutable(context, channel, checkedLocations, appProbes)

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
        cdpAvailable = browserExecutable != null,
        cdpExecutable = browserExecutable,
        checkedLocations = checkedLocations,
    )
}

private suspend fun runShellCommand(context: ToolContext, command: String): String? {
    return runCatching {
        val result = context.shell.runAsync(command, context.workspaceRoot)
        if (result.exitCode == 0) result.stdOut.trim() else null
    }.getOrNull()
}

private fun runPlatformMetadata(context: ToolContext): Pair<String, String> {
    val platformFromShell = runCatching { runShellCommand(context, "uname -s") }.getOrNull()
    val archFromShell = runCatching { runShellCommand(context, "uname -m") }.getOrNull()
    val platform = platformFromShell?.trim()?.lowercase()?.replace("darwin", "macOS") ?: run {
        System.getProperty("os.name")
            ?.lowercase()
            ?.replace("mac os x", "macOS")
            ?: "unknown"
    }
    val arch = archFromShell?.trim() ?: (System.getProperty("os.arch") ?: "unknown")
    return platform to arch
}

private fun browserDiagnosticsOutputLines(
    profile: String,
    profileDir: String,
    downloadsDir: String,
    stateFile: String,
    historyFile: String,
    result: BrowserDiagnosticsData,
): List<String> {
    val lines = mutableListOf(
        "browser action: diagnostics",
        "node: ${result.nodeVersion ?: "unknown"}",
        "platform: ${result.platform ?: "unknown"}/${result.arch ?: "unknown"}",
        "channel: ${result.channel ?: "chromium"}",
        "profile: $profile",
        "playwright available: ${if (result.playwrightAvailable) "yes" else "no"}",
    )
    result.playwrightVersion?.ifBlank { null }?.let { lines += "playwright version: $it" }
    result.playwrightResolvedFrom?.ifBlank { null }?.let { lines += "playwright resolved from: $it" }
    lines += "node WebSocket available: ${if (result.nodeWebSocketAvailable) "yes" else "no"}"
    lines += "cdp fallback available: ${if (result.cdpAvailable) "yes" else "no"}"
    result.cdpExecutable?.ifBlank { null }?.let { lines += "cdp executable: $it" }
    lines += "profile dir: $profileDir"
    lines += "downloads dir: $downloadsDir"
    lines += "state file: $stateFile"
    lines += "history file: $historyFile"

    if (result.browserApps.isNotEmpty()) {
        lines += ""
        lines += "installed app probes:"
        result.browserApps.keys.sorted().forEach { key ->
            lines += "- $key: ${if (result.browserApps[key] == true) "yes" else "no"}"
        }
    }
    if (result.checkedLocations.isNotEmpty()) {
        lines += ""
        lines += "checked Playwright locations:"
        result.checkedLocations.forEach { lines += "- $it" }
    }
    return lines
}

private suspend fun resolveBrowserExecutable(
    context: ToolContext,
    channel: String,
    checkedLocations: MutableList<String>,
    appProbes: MutableMap<String, Boolean>,
): String? {
    val browserCandidates = when (channel) {
        "chrome", "chrome-beta", "chrome-dev", "chrome-canary" -> listOf(
            "google-chrome" to "chrome",
            "google-chrome-stable" to "chrome",
            "google-chrome-beta" to "chrome-beta",
            "google-chrome-dev" to "chrome-dev",
            "google-chrome-canary" to "chrome-canary",
            "chromium" to "chromium",
            "chromium-browser" to "chromium",
            "microsoft-edge" to "msedge",
            "msedge" to "msedge",
        )
        "msedge", "msedge-beta", "msedge-dev", "msedge-canary" -> listOf(
            "msedge" to "msedge",
            "microsoft-edge" to "msedge",
            "microsoft-edge-stable" to "msedge",
            "google-chrome" to "chrome",
        )
        else -> listOf(
            "chromium" to "chromium",
            "chromium-browser" to "chromium",
            "google-chrome" to "chrome",
            "google-chrome-stable" to "chrome",
            "google-chrome-beta" to "chrome-beta",
            "google-chrome-dev" to "chrome-dev",
            "google-chrome-canary" to "chrome-canary",
            "microsoft-edge" to "msedge",
            "msedge" to "msedge",
            "msedge-dev" to "msedge-dev",
            "msedge-beta" to "msedge-beta",
            "msedge-canary" to "msedge-canary",
        )
    }
    var executablePath: String? = null
    for ((command, probeKey) in browserCandidates) {
        val probeCommand = "command -v $command"
        val probeResult = runShellCommand(context, probeCommand)
        checkedLocations += probeCommand
        appProbes[probeKey] = appProbes[probeKey] == true || probeResult != null
        if (executablePath == null && probeResult != null && probeResult.isNotBlank()) {
            executablePath = probeResult.trim().substringBefore('\n')
            break
        }
    }
    return executablePath
}

private fun normalizeBrowserProfile(rawProfile: String?): String {
    val value = rawProfile?.trim().takeUnless { it.isNullOrBlank() } ?: "default"
    val profile = if (value.length <= 64 && value != "." && value != ".." && value.all { it.isLetterOrDigit() || it == '-' || it == '_' || it == '.' }) value else throw IllegalArgumentException("browser profile must use only letters, numbers, '.', '-' or '_'")
    if (value.length > 64) throw IllegalArgumentException("browser profile must be at most 64 characters")
    return profile
}

private fun normalizeBrowserChannel(rawChannel: String?): String {
    val value = rawChannel?.trim()?.lowercase() ?: ""
    return when (value) {
        "chrome", "chrome-beta", "chrome-dev", "chrome-canary",
        "msedge", "msedge-beta", "msedge-dev", "msedge-canary", "chromium" -> value
        else -> "chromium"
    }
}

private fun normalizeBrowserKey(rawKey: String): String {
    val key = rawKey.trim()
    if (key.any { it.isISOControl() }) {
        throw IllegalArgumentException("browser_press_key key cannot contain control characters")
    }
    return key
}

private fun calculateBrowserScrollDelta(
    direction: String,
    amount: Int,
    hasExplicitDeltaX: Boolean,
    hasExplicitDeltaY: Boolean,
    explicitDeltaX: Int,
    explicitDeltaY: Int,
): Pair<Int, Int> {
    if (hasExplicitDeltaX || hasExplicitDeltaY) {
        if (explicitDeltaX == 0 && explicitDeltaY == 0) {
            throw IllegalArgumentException("browser_scroll requires a non-zero delta")
        }
        return explicitDeltaX to explicitDeltaY
    }

    return when (direction) {
        "down", "" -> 0 to amount
        "up" -> 0 to -amount
        "right" -> amount to 0
        "left" -> -amount to 0
        else -> throw IllegalArgumentException("browser_scroll direction must be one of: down, up, right, left")
    }
}

private fun normalizedBrowserWaitState(raw: String?): String {
    return when (raw?.trim()?.lowercase()) {
        "attached", "detached", "visible", "hidden" -> raw.trim().lowercase()
        else -> "visible"
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

private fun unsupportedSchema(name: String): Map<String, Any?> = when (name) {
    "read_pdf" -> objectSchema(
        required = listOf("path"),
        properties = mapOf(
            "path" to stringSchema(minLength = 1),
            "pages" to stringSchema(minLength = 1),
            "maxCharacters" to integerSchema(1, 500_000),
        )
    )
    "edit_pdf_pages" -> objectSchema(
        required = listOf("mode", "inputPath"),
        properties = mapOf(
            "mode" to stringSchema(minLength = 1),
            "inputPath" to stringSchema(minLength = 1),
            "pages" to stringSchema(minLength = 1),
            "outputPath" to stringSchema(minLength = 1),
            "outputDir" to stringSchema(minLength = 1),
            "outputPrefix" to stringSchema(minLength = 1),
        )
    )
    "reconstruct_document_image" -> objectSchema(
        required = listOf("imagePath", "outputPath"),
        properties = mapOf(
            "imagePath" to stringSchema(minLength = 1),
            "outputPath" to stringSchema(minLength = 1),
            "format" to stringSchema(minLength = 1),
            "backend" to stringSchema(minLength = 1),
        )
    )
    "compile_latex" -> objectSchema(
        required = listOf("inputPath"),
        properties = mapOf(
            "inputPath" to stringSchema(minLength = 1),
            "outputDir" to stringSchema(minLength = 1),
            "engine" to stringSchema(minLength = 1),
        )
    )
    "generate_image" -> objectSchema(
        required = listOf("prompt", "outputPath"),
        properties = mapOf(
            "prompt" to stringSchema(minLength = 1),
            "outputPath" to stringSchema(minLength = 1),
            "size" to stringSchema(minLength = 1),
            "count" to integerSchema(1, 4),
        )
    )
    "web_fetch" -> objectSchema(
        required = listOf("url"),
        properties = mapOf(
            "url" to stringSchema(minLength = 1),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_diagnostics" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
        )
    )
    "browser_profiles" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "limit" to integerSchema(1, 100),
            "includeProfileSize" to booleanSchema(),
        )
    )
    "browser_profile_delete" -> objectSchema(
        required = listOf("profile", "confirmProfile"),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "confirmProfile" to stringSchema(minLength = 1),
        )
    )
    "browser_history" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "limit" to integerSchema(1, 100),
        )
    )
    "browser_navigate" -> objectSchema(
        required = listOf("url"),
        properties = mapOf(
            "url" to stringSchema(minLength = 1),
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_snapshot" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_handoff" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "url" to stringSchema(minLength = 1),
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "handoffSeconds" to integerSchema(1, 600),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_reload" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "ignoreCache" to booleanSchema(),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_back" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_forward" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_click" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "exact" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_type" -> objectSchema(
        required = listOf("value"),
        properties = mapOf(
            "value" to stringSchema(minLength = 1),
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "clear" to booleanSchema(),
            "submit" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_submit" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "exact" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "timeoutMillis" to integerSchema(1_000, 30_000),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_select_option" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "optionValue" to stringSchema(minLength = 1),
            "optionLabel" to stringSchema(minLength = 1),
            "optionIndex" to integerSchema(0, 500),
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "exact" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_press_key" -> objectSchema(
        required = listOf("key"),
        properties = mapOf(
            "key" to stringSchema(minLength = 1, maxLength = 80),
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "exact" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_scroll" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "exact" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "direction" to stringSchema(minLength = 1),
            "amount" to integerSchema(1, 10_000),
            "deltaX" to integerSchema(-10_000, 10_000),
            "deltaY" to integerSchema(-10_000, 10_000),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_wait" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "exact" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "state" to stringSchema(minLength = 1),
            "timeoutMillis" to integerSchema(1_000, 30_000),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_screenshot" -> objectSchema(
        required = listOf("outputPath"),
        properties = mapOf(
            "outputPath" to stringSchema(minLength = 1),
            "url" to stringSchema(minLength = 1),
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "fullPage" to booleanSchema(),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_upload_file" -> objectSchema(
        required = listOf("filePath"),
        properties = mapOf(
            "filePath" to stringSchema(minLength = 1),
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "exact" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_download" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "selector" to stringSchema(minLength = 1),
            "text" to stringSchema(minLength = 1),
            "role" to stringSchema(minLength = 1),
            "name" to stringSchema(minLength = 1),
            "exact" to booleanSchema(),
            "nth" to integerSchema(0, 100),
            "waitMillis" to integerSchema(0, 10_000),
            "downloadTimeoutMillis" to integerSchema(1_000, 60_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    "browser_downloads" -> objectSchema(
        required = emptyList(),
        properties = mapOf(
            "profile" to stringSchema(minLength = 1),
            "limit" to integerSchema(1, 100),
        )
    )
    "browser_search" -> objectSchema(
        required = listOf("query"),
        properties = mapOf(
            "query" to stringSchema(minLength = 1),
            "engine" to stringSchema(minLength = 1),
            "profile" to stringSchema(minLength = 1),
            "channel" to stringSchema(minLength = 1),
            "headless" to booleanSchema(),
            "waitMillis" to integerSchema(0, 10_000),
            "maxCharacters" to integerSchema(1, 100_000),
        )
    )
    else -> objectSchema(emptyList(), emptyMap())
}

private fun objectSchema(required: List<String>, properties: Map<String, Any?>): Map<String, Any?> =
    mapOf(
        "type" to "object",
        "properties" to properties,
        "required" to required,
        "additionalProperties" to false,
    )

private fun stringSchema(minLength: Int? = null, maxLength: Int? = null): Map<String, Any?> {
    val schema = mutableMapOf<String, Any?>("type" to "string")
    if (minLength != null) schema["minLength"] = minLength
    if (maxLength != null) schema["maxLength"] = maxLength
    return schema
}

private fun integerSchema(minimum: Int, maximum: Int?): Map<String, Any?> {
    val schema = mutableMapOf<String, Any?>("type" to "integer", "minimum" to minimum)
    if (maximum != null) schema["maximum"] = maximum
    return schema
}

private fun integerSchema(minimum: Int): Map<String, Any?> = mapOf(
    "type" to "integer",
    "minimum" to minimum,
)

private fun booleanSchema(): Map<String, Any?> = mapOf("type" to "boolean")

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

class SendMessageTool : ITool {
    companion object {
        val descriptor = ToolDescriptor(
            "send_message",
            "Send a message to another attached agent without creating a task.",
            SideEffect.READ_ONLY,
            mapOf(
                "type" to "object",
                "properties" to mapOf(
                    "to" to mapOf("type" to "string", "description" to "target agent name", "maxLength" to 64),
                    "content" to mapOf("type" to "string", "maxLength" to 4_000),
                ),
                "required" to listOf("to", "content"),
                "additionalProperties" to false,
            )
        )
    }

    override val descriptor: ToolDescriptor = SendMessageTool.descriptor

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val messenger = context.messenger ?: return@withContext ToolObservation("agent messaging is not available in this session")
        ensureNoUnknownArgs(args.root, "send_message", setOf("to", "content"))
        val to = requiredString(args.root, "to")
        val content = requiredString(args.root, "content")
        val result = runCatching { messenger.sendMessageAsync(context.agentName, to, content) }.getOrElse { "agent message failure: ${it.message}" }
        ToolObservation(result)
    }
}

class RequestInformationTool : ITool {
    companion object {
        val descriptor = ToolDescriptor(
            "request_information",
            "Ask another attached agent for information without creating a delegated task.",
            SideEffect.READ_ONLY,
            mapOf(
                "type" to "object",
                "properties" to mapOf(
                    "to" to mapOf("type" to "string", "maxLength" to 64, "description" to "target agent name"),
                    "question" to mapOf("type" to "string", "maxLength" to 4_000),
                ),
                "required" to listOf("to", "question"),
            )
        )
    }

    override val descriptor: ToolDescriptor = RequestInformationTool.descriptor

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val messenger = context.messenger ?: return@withContext ToolObservation("agent messaging is not available in this session")
        ensureNoUnknownArgs(args.root, "request_information", setOf("to", "question"))
        val to = requiredString(args.root, "to")
        val question = requiredString(args.root, "question")
        val result = runCatching { messenger.requestInformationAsync(context.agentName, to, question) }.getOrElse { "agent message failure: ${it.message}" }
        ToolObservation(result)
    }
}

class ReplyMessageTool : ITool {
    companion object {
        val descriptor = ToolDescriptor(
            "reply_message",
            "Reply to an information request from an attached agent.",
            SideEffect.READ_ONLY,
            mapOf(
                "type" to "object",
                "properties" to mapOf(
                    "to" to mapOf("type" to "string", "maxLength" to 64),
                    "answer" to mapOf("type" to "string", "maxLength" to 4_000),
                    "inReplyTo" to mapOf("type" to "string", "maxLength" to 128),
                ),
                "required" to listOf("to", "answer"),
            )
        )
    }

    override val descriptor: ToolDescriptor = ReplyMessageTool.descriptor

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val messenger = context.messenger ?: return@withContext ToolObservation("agent messaging is not available in this session")
        ensureNoUnknownArgs(args.root, "reply_message", setOf("to", "answer", "inReplyTo"))
        val to = requiredString(args.root, "to")
        val answer = requiredString(args.root, "answer")
        val inReplyTo = optionalString(args.root, "inReplyTo", "")
        val result = runCatching {
            messenger.replyMessageAsync(
                context.agentName,
                to,
                answer,
                inReplyTo = inReplyTo.ifBlank { null },
            )
        }.getOrElse { "agent message failure: ${it.message}" }
        ToolObservation(result)
    }
}

class RequestDelegationTool : ITool {
    companion object {
        val descriptor = ToolDescriptor(
            "request_delegation",
            "Request another attached agent to delegate a task.",
            SideEffect.READ_ONLY,
            mapOf(
                "type" to "object",
                "properties" to mapOf(
                    "objective" to mapOf("type" to "string", "maxLength" to 4_000),
                    "reason" to mapOf("type" to "string", "maxLength" to 128),
                ),
                "required" to listOf("objective"),
            )
        )
    }

    override val descriptor: ToolDescriptor = RequestDelegationTool.descriptor

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val messenger = context.messenger ?: return@withContext ToolObservation("agent messaging is not available in this session")
        ensureNoUnknownArgs(args.root, "request_delegation", setOf("objective", "reason"))
        val objective = requiredString(args.root, "objective")
        val reason = stringValue(args.root, "reason", "delegation requested")
        val result = runCatching { messenger.requestDelegationAsync(context.agentName, objective, reason) }.getOrElse { "agent message failure: ${it.message}" }
        ToolObservation(result)
    }
}

class DelegateTaskTool : ITool {
    companion object {
        val descriptor = ToolDescriptor(
            "delegate_task",
            "Delegate a task directly to an attached agent.",
            SideEffect.READ_ONLY,
            mapOf(
                "type" to "object",
                "properties" to mapOf(
                    "to" to mapOf("type" to "string", "maxLength" to 64),
                    "objective" to mapOf("type" to "string", "maxLength" to 4_000),
                    "reason" to mapOf("type" to "string", "maxLength" to 128),
                ),
                "required" to listOf("to", "objective"),
            )
        )
    }

    override val descriptor: ToolDescriptor = DelegateTaskTool.descriptor

    override fun touchedPaths(args: ToolArgs): List<String> = emptyList()
    override fun risksNetwork(args: ToolArgs): Boolean = false

    override suspend fun execute(args: ToolArgs, context: ToolContext): ToolObservation = withContext(Dispatchers.IO) {
        val messenger = context.messenger ?: return@withContext ToolObservation("agent messaging is not available in this session")
        ensureNoUnknownArgs(args.root, "delegate_task", setOf("to", "objective", "reason"))
        val to = requiredString(args.root, "to")
        val objective = requiredString(args.root, "objective")
        val reason = stringValue(args.root, "reason", "delegation requested")
        val result = runCatching { messenger.delegateTaskAsync(context.agentName, to, objective, reason) }.getOrElse { "agent message failure: ${it.message}" }
        ToolObservation(result)
    }
}

private fun requiredString(obj: JsonObject, key: String, fallback: String? = null, maxLength: Int? = null, minLength: Int = 0): String {
    val text = obj[key]?.jsonPrimitive?.contentOrNull
        ?: fallback
        ?: throw IllegalArgumentException("tool args missing '$key'")
    if (text.length > (maxLength ?: Int.MAX_VALUE)) {
        throw IllegalArgumentException("tool args '$key' must be at most ${maxLength} characters")
    }
    if (text.length < minLength) {
        throw IllegalArgumentException("tool args '$key' must be at least $minLength characters")
    }
    return text
}

private fun stringValue(obj: JsonObject, key: String, fallback: String, maxLength: Int? = null, minLength: Int = 0): String {
    val raw = obj[key]?.jsonPrimitive?.contentOrNull ?: return fallback
    if (raw.length > (maxLength ?: Int.MAX_VALUE)) {
        throw IllegalArgumentException("tool args '$key' must be at most ${maxLength} characters")
    }
    if (raw.length < minLength) {
        throw IllegalArgumentException("tool args '$key' must be at least $minLength characters")
    }
    return raw
}

private fun isHiddenPath(file: File): Boolean = file.isHidden || file.name.startsWith(".")
