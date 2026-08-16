package com.intatis.shared

import com.intatis.shared.protocol.WorkspaceAccess
import com.intatis.shared.protocol.WorkspaceLease
import com.intatis.shared.provider.ProviderRegistry
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.util.Locale

private val pathSeparator = java.io.File.separator

sealed class PermissionDecision {
    object Allow : PermissionDecision()
    object Deny : PermissionDecision()
    object AskUser : PermissionDecision()
}

sealed class RiskLevel {
    object Low : RiskLevel()
    object Medium : RiskLevel()
    object High : RiskLevel()

    override fun toString(): String = when (this) {
        is Low -> "low"
        is Medium -> "medium"
        is High -> "high"
    }
}

enum class PermissionProfile {
    Manual,
    Reviewed,
    Autopilot,
    ReadOnly,
    Locked,
}

enum class GateResult {
    Deny,
    Ask,
    Allow,
    Pass,
}

enum class SideEffect {
    ReadOnly,
    Write,
    Exec,
    Network,
    Destructive,
}

data class ToolCallContext(
    val toolName: String,
    val sideEffect: SideEffect,
    val touchedPaths: List<String>,
    val risksNetwork: Boolean,
    val rawArgs: String,
)

data class PermissionContext(
    val workspaceRoot: String,
    val profile: PermissionProfile,
    val allowsShell: Boolean,
    val userGoal: String? = null,
    val agent: String? = null,
    val workspaceLease: WorkspaceLease = WorkspaceLease(
        rootPath = workspaceRoot,
        access = WorkspaceAccess.ReadWrite,
    ),
)

data class PermissionOutcome(
    val decision: PermissionDecision,
    val risk: RiskLevel,
    val reason: String,
    val reviewedByModel: Boolean = false,
)

data class PermissionGateOutcome(
    val decision: GateResult,
    val risk: RiskLevel,
    val reason: String,
)

data class PermissionRequest(
    val requestId: String,
    val tool: String,
    val args: String,
    val risk: RiskLevel,
    val reason: String,
    val agent: String? = null,
)

interface IPermissionResponder {
    suspend fun requestApprovalAsync(request: PermissionRequest): PermissionDecision
}

class SwappablePermissionResponder(initial: IPermissionResponder) : IPermissionResponder {
    @Volatile
    private var responder: IPermissionResponder = initial

    fun replace(next: IPermissionResponder) {
        responder = next
    }

    fun reset(initial: IPermissionResponder) {
        responder = initial
    }

    override suspend fun requestApprovalAsync(request: PermissionRequest): PermissionDecision =
        responder.requestApprovalAsync(request)
}

class AgentPermissionResponder(
    config: IntatisConfig,
    private val fallbackResponder: IPermissionResponder,
    model: String? = null,
    private val reviewerIdentity: String = CoworkAgentRegistry.PermissionReviewerIdentity,
) : IPermissionResponder {
    private val chatProvider = ProviderRegistry(config).chatProvider(config.chatProviderId)
    private val reviewerModel = model?.ifBlank { null } ?: config.model

    override suspend fun requestApprovalAsync(request: PermissionRequest): PermissionDecision {
        if (request.agent.equals(reviewerIdentity, ignoreCase = true)) {
            return PermissionDecision.Deny
        }

        val prompt = """
<<<REVIEW_TARGET (untrusted data)>>>
tool: ${request.tool}
request_id: ${request.requestId}
risk: ${request.risk}
reason: ${request.reason}
args: ${request.args}
agent: ${request.agent ?: "(none)"}
<<<END>>>
Return only the JSON object.
""".trimIndent()

        val messages = listOf(
            com.intatis.shared.IntatisMessage(MessageRole.System, """
You are a security reviewer for a local coding agent.
Decide whether a proposed tool call is reasonable for the user's task and safe to run.
Return only a JSON object, no prose:
{"decision":"allow|deny|ask_user","risk":"low|medium|high","reason":"<short>"}.
Prefer ask_user when unsure.
""".trimIndent()),
            com.intatis.shared.IntatisMessage(MessageRole.User, prompt),
        )

        val parsedDecision = runCatching {
            val text = chatProvider.sendAsync(messages, reviewerModel).text
            parseDecision(text)
        }.getOrNull()

        if (parsedDecision != null) {
            return parsedDecision
        }

        return fallbackResponder.requestApprovalAsync(request)
    }

    private fun parseDecision(text: String): PermissionDecision? {
        val start = text.indexOf('{')
        val end = text.lastIndexOf('}')
        if (start < 0 || end <= start) return null

        val payload = text.substring(start, end + 1)
        val json = kotlinx.serialization.json.Json { ignoreUnknownKeys = true }.parseToJsonElement(payload).jsonObject
        val decision = json["decision"]?.jsonPrimitive?.contentOrNull?.lowercase() ?: return null
        return when (decision) {
            "allow" -> PermissionDecision.Allow
            "deny" -> PermissionDecision.Deny
            "ask_user" -> PermissionDecision.AskUser
            else -> null
        }
    }
}

interface IPermissionReviewer {
    suspend fun reviewAsync(
        call: ToolCallContext,
        context: PermissionContext,
        gateReason: String,
        risk: RiskLevel
    ): PermissionOutcome
}

class AllowAllResponder : IPermissionResponder {
    override suspend fun requestApprovalAsync(request: PermissionRequest): PermissionDecision = PermissionDecision.Allow
}

class PermissionEngine(private val reviewer: IPermissionReviewer? = null) {
    private val gate = DeterministicPolicyGate()

    suspend fun decideAsync(call: ToolCallContext, context: PermissionContext): PermissionOutcome {
        val evaluated = gate.evaluate(call, context)
        return when (evaluated.decision) {
            GateResult.Deny -> PermissionOutcome(PermissionDecision.Deny, evaluated.risk, evaluated.reason)
            GateResult.Ask -> PermissionOutcome(PermissionDecision.AskUser, evaluated.risk, evaluated.reason)
            GateResult.Allow -> PermissionOutcome(PermissionDecision.Allow, evaluated.risk, evaluated.reason)
            GateResult.Pass -> reviewOrAsk(call, context, evaluated)
        }
    }

    private suspend fun reviewOrAsk(
        call: ToolCallContext,
        context: PermissionContext,
        result: PermissionGateOutcome
    ): PermissionOutcome {
        if (reviewer == null) {
            return PermissionOutcome(
                PermissionDecision.AskUser,
                result.risk,
                "${result.reason} (no reviewer configured → asking user)"
            )
        }
        return reviewer.reviewAsync(call, context, result.reason, result.risk)
    }
}

class DeterministicPolicyGate {
    fun evaluate(call: ToolCallContext, ctx: PermissionContext): PermissionGateOutcome {
        if (ctx.profile == PermissionProfile.Locked) {
            return PermissionGateOutcome(GateResult.Deny, RiskLevel.Low, "agent is locked")
        }

        for (path in call.touchedPaths) {
            if (SecretScanner.isSensitivePath(path)) {
                return PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "touches sensitive file: $path")
            }
            if (!WorkspaceSecurity.isWithinWorkspace(path, ctx.workspaceRoot)) {
                return PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "path escapes workspace: $path")
            }
        }

        evaluateWorkspaceLease(call, ctx)?.let { return it }

        if (call.risksNetwork) {
            return if (ctx.profile == PermissionProfile.ReadOnly) {
                PermissionGateOutcome(GateResult.Deny, RiskLevel.Medium, "network not allowed in read_only")
            } else {
                PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "network access requested")
            }
        }

        return when (call.sideEffect) {
            SideEffect.ReadOnly -> PermissionGateOutcome(GateResult.Allow, RiskLevel.Low, "read-only operation within workspace")
            SideEffect.Network -> if (ctx.profile == PermissionProfile.ReadOnly) {
                PermissionGateOutcome(GateResult.Deny, RiskLevel.Medium, "network not allowed in read_only")
            } else {
                PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "network access requested")
            }

            SideEffect.Destructive -> if (ctx.profile == PermissionProfile.ReadOnly) {
                PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "destructive operation not allowed in read_only")
            } else {
                PermissionGateOutcome(GateResult.Ask, RiskLevel.High, "destructive operation")
            }

            SideEffect.Exec -> evaluateExec(call, ctx)
            SideEffect.Write -> evaluateWrite(call, ctx)
        }
    }

    private fun evaluateWorkspaceLease(
        call: ToolCallContext,
        ctx: PermissionContext,
    ): PermissionGateOutcome? = when {
        ctx.workspaceLease.access == WorkspaceAccess.ReadOnly && call.sideEffect == SideEffect.Write ->
            PermissionGateOutcome(
                GateResult.Deny,
                RiskLevel.High,
                "writes not allowed in read-only workspace lease",
            )
        ctx.workspaceLease.access == WorkspaceAccess.ReadOnly && call.sideEffect == SideEffect.Exec ->
            PermissionGateOutcome(
                GateResult.Deny,
                RiskLevel.High,
                "shell not allowed in read-only workspace lease",
            )
        ctx.workspaceLease.access == WorkspaceAccess.ReadOnly && call.sideEffect == SideEffect.Destructive ->
            PermissionGateOutcome(
                GateResult.Deny,
                RiskLevel.High,
                "destructive operations blocked by read-only workspace lease",
            )
        else -> null
    }

    private fun evaluateExec(call: ToolCallContext, ctx: PermissionContext): PermissionGateOutcome {
        if (!ctx.allowsShell) {
            return PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "shell is disabled in this build (sandbox)")
        }
        if (ctx.profile == PermissionProfile.ReadOnly) {
            return PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "shell not allowed in read_only")
        }

        val command = ShellInspector.extractShellCommand(call.rawArgs)
        if (ShellInspector.isDangerous(command)) {
            return PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "dangerous shell command")
        }
        if (ShellInspector.risksNetworkOrInstall(command)) {
            return PermissionGateOutcome(GateResult.Ask, RiskLevel.High, "shell command may access network or install packages")
        }
        when (val inspection = ShellInspector.inspectReadOnlyCommand(command, ctx.workspaceRoot)) {
            is ShellInspector.ReadOnlyInspection.Allow -> {
                return PermissionGateOutcome(GateResult.Allow, RiskLevel.Low, inspection.reason)
            }
            is ShellInspector.ReadOnlyInspection.Deny -> {
                return PermissionGateOutcome(GateResult.Deny, RiskLevel.High, inspection.reason)
            }
            is ShellInspector.ReadOnlyInspection.Ask -> {
                return PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "run shell command")
            }
        }
    }

    private fun evaluateWrite(call: ToolCallContext, ctx: PermissionContext): PermissionGateOutcome {
        if (ctx.profile == PermissionProfile.ReadOnly) {
            return PermissionGateOutcome(GateResult.Deny, RiskLevel.Medium, "writes not allowed in read_only")
        }
        if (call.touchedPaths.any { SecretScanner.isProtectedConfigPath(it) }) {
            return PermissionGateOutcome(GateResult.Ask, RiskLevel.High, "modifies lockfile / CI / build config")
        }

        return when (ctx.profile) {
            PermissionProfile.Manual -> PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "write to workspace")
            PermissionProfile.Reviewed, PermissionProfile.Autopilot -> PermissionGateOutcome(GateResult.Pass, RiskLevel.Low, "write within workspace")
            else -> PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "write to workspace")
        }
    }
}

object WorkspaceSecurity {
    fun isWithinWorkspace(candidatePath: String, workspaceRoot: String): Boolean {
        return try {
            val root = java.io.File(workspaceRoot).canonicalFile
            val candidate = java.io.File(candidatePath).canonicalFile
            val rootPath = root.toPath().toAbsolutePath().normalize()
            val candidatePathAbs = candidate.toPath().toAbsolutePath().normalize()
            candidatePathAbs == rootPath || candidatePathAbs.startsWith(rootPath)
        } catch (_: Exception) {
            false
        }
    }

    fun resolveInWorkspace(workspaceRoot: String, path: String): String {
        val root = java.io.File(workspaceRoot).absoluteFile
        val candidate = if (java.io.File(path).isAbsolute) java.io.File(path).absoluteFile else java.io.File(root, path).absoluteFile
        if (!isWithinWorkspace(candidate.absolutePath, root.absolutePath)) {
            throw IllegalArgumentException("path escapes workspace: $path")
        }
        return candidate.absolutePath
    }
}

object SecretScanner {
    private val sensitiveBasenames = setOf(
        ".env", ".netrc", ".pgpass", "id_rsa", "id_dsa", "id_ecdsa", "id_ed25519", "credentials", ".npmrc", ".pypirc"
    )

    private val sensitiveExtensions = setOf("pem", "key", "p12", "pfx", "keystore", "jks", "asc")

    private val sensitiveDirHints = arrayOf(
        "/.ssh/", "/.aws/", "/.gnupg/", "/.gpg/", "secrets/", "/.config/gh/",
        ".config/opencode/", ".config/intatis/", ".local/share/opencode/", ".local/share/intatis/",
    )

    private val protectedBasenames = setOf(
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "cargo.lock",
        "podfile.lock", "gemfile.lock", "package.resolved", "poetry.lock"
    )
    private val protectedHints = arrayOf(
        ".github/workflows/", ".gitlab-ci", "/dockerfile", "/makefile", ".circleci/",
        "fastlane/", "/ci/",
    )

    fun isSensitivePath(path: String): Boolean {
        val normalized = path.replace('\\', '/').lowercase(Locale.getDefault())
        val fileName = normalized.substringAfterLast('/')
        if (sensitiveBasenames.contains(fileName)) return true
        if (fileName.startsWith(".env")) return true
        val ext = fileName.substringAfterLast('.', "").lowercase(Locale.getDefault())
        if (ext.isNotBlank() && sensitiveExtensions.contains(ext)) return true
        val padded = "/" + normalized
        return sensitiveDirHints.any { padded.contains(it) }
    }

    fun isProtectedConfigPath(path: String): Boolean {
        val normalized = path.replace('\\', '/').lowercase(Locale.getDefault())
        val fileName = normalized.substringAfterLast('/')
        if (protectedBasenames.contains(fileName)) return true
        val padded = "/" + normalized
        return protectedHints.any { padded.contains(it) }
    }

    fun containsSecret(text: String): Boolean {
        val markers = arrayOf("-----BEGIN", "PRIVATE KEY", "AKIA", "ASIA", "sk-", "ssh-rsa ", "xoxb-", "xoxp-", "ghp_", "github_pat_", "AIza")
        return markers.any { text.contains(it, ignoreCase = false) }
    }
}

object ShellInspector {
    private val dangerous = arrayOf("sudo", "rm -rf", "rm -fr", "rm -r ", ":(){", "mkfs", "dd if=", "chmod -r 777", "chown -r", "/etc/", "~/.ssh", "shutdown", "reboot", "killall")

    private val networkOrInstall = arrayOf(
        "curl ", "wget ", "npm install", "npm i ", "yarn add", "pnpm add",
        "pip install", "pip3 install", "apt ", "apt-get", "brew install", "gem install", "git clone",
        "git push", "git pull", "git fetch", "nc ", "ssh ", "scp "
    )

    private val readonlyAllowlist = arrayOf("ls", "pwd", "cat", "rg", "grep", "find")
    private val shellMetacharacters = arrayOf("\n", "\r", "|", ">", "<", "&&", "||", "$", "`", "*", "?", "~", "&")

    sealed class ReadOnlyInspection {
        data class Allow(val reason: String) : ReadOnlyInspection()
        data class Ask(val reason: String) : ReadOnlyInspection()
        data class Deny(val reason: String) : ReadOnlyInspection()
    }

    fun isDangerous(command: String): Boolean {
        val lower = command.lowercase()
        return dangerous.any { lower.contains(it) }
    }

    fun risksNetworkOrInstall(command: String): Boolean {
        val lower = command.lowercase()
        return networkOrInstall.any { lower.contains(it) }
    }

    fun isReadOnlyCommand(command: String): Boolean {
        if (containsShellMetacharacter(command)) return false
        val argv = parseArgv(command) ?: return false
        val executable = argv.firstOrNull() ?: return false
        if (executable.contains("/")) return false
        return !isDangerous(command) && readonlyAllowlist.contains(executable)
    }

    fun inspectReadOnlyCommand(command: String, workspaceRoot: String): ReadOnlyInspection {
        if (containsShellMetacharacter(command)) {
            return ReadOnlyInspection.Ask("shell metacharacters require user approval")
        }

        val argv = parseArgv(command) ?: return ReadOnlyInspection.Ask("shell command is not a simple argv form")
        if (argv.isEmpty()) {
            return ReadOnlyInspection.Ask("shell command is not a simple argv form")
        }
        val executable = argv.first()
        if (executable.contains("/")) {
            return ReadOnlyInspection.Ask("shell command is not a simple argv form")
        }
        if (!readonlyAllowlist.contains(executable)) {
            return ReadOnlyInspection.Ask("shell command is not in the read-only allowlist")
        }

        val paths = when (executable) {
            "pwd" -> {
                if (argv.size != 1) return ReadOnlyInspection.Ask("pwd arguments require user approval")
                listOf(".")
            }
            "ls" -> {
                val rest = argv.drop(1)
                if (rest.any { it.startsWith("-") }) return ReadOnlyInspection.Ask("ls arguments require user approval")
                rest.ifEmpty { listOf(".") }
            }
            "cat" -> {
                val rest = argv.drop(1)
                if (rest.isEmpty()) return ReadOnlyInspection.Ask("cat requires explicit file paths")
                if (rest.any { it.startsWith("-") }) return ReadOnlyInspection.Ask("cat options require user approval")
                rest
            }
            "rg", "grep" -> {
                val rest = argv.drop(1)
                if (rest.isEmpty()) return ReadOnlyInspection.Ask("$executable requires a pattern")
                if (rest.any { it.startsWith("-") }) return ReadOnlyInspection.Ask("$executable options require user approval")
                if (rest.size == 1) listOf(".") else rest.drop(1)
            }
            "find" -> {
                val rest = argv.drop(1)
                if (rest.any { it.startsWith("-") || it == "!" || it == "(" || it == ")" }) {
                    return ReadOnlyInspection.Ask("find predicates require user approval")
                }
                if (rest.isEmpty()) listOf(".") else rest
            }
            else -> return ReadOnlyInspection.Ask("shell command is not in the read-only allowlist")
        }

        for (path in paths) {
            if (SecretScanner.isSensitivePath(path)) {
                return ReadOnlyInspection.Deny("touches sensitive path: $path")
            }
            try {
                WorkspaceSecurity.resolveInWorkspace(workspaceRoot, path)
            } catch (ex: Exception) {
                return ReadOnlyInspection.Deny(ex.message ?: "path escapes workspace")
            }
        }

        return ReadOnlyInspection.Allow("simple read-only shell command within workspace")
    }

    private fun containsShellMetacharacter(command: String): Boolean =
        shellMetacharacters.any { command.contains(it) }

    private fun parseArgv(command: String): List<String>? {
        val args = mutableListOf<String>()
        val current = StringBuilder()
        var quote: Char? = null

        for (ch in command) {
            if (ch == '\\') return null
            quote?.let {
                if (ch == it) {
                    quote = null
                } else {
                    current.append(ch)
                }
                continue
            }
            when (ch) {
                '\'', '"' -> quote = ch
                ' ', '\t' -> {
                    if (current.isNotEmpty()) {
                        args.add(current.toString())
                        current.clear()
                    }
                }
                else -> current.append(ch)
            }
        }

        if (quote != null) return null
        if (current.isNotEmpty()) {
            args.add(current.toString())
        }
        return if (args.isEmpty()) null else args
    }

    fun extractShellCommand(rawArgs: String): String {
        return try {
            val parsed = kotlinx.serialization.json.Json.parseToJsonElement(rawArgs).jsonObject
            parsed["command"]?.jsonPrimitive?.contentOrNull ?: ""
        } catch (_: Exception) {
            ""
        }
    }
}
