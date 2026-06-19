package com.intatis.shared.security

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonPrimitive
import java.io.File

enum class PermissionProfile { MANUAL, REVIEWED, AUTOPILOT, READ_ONLY, LOCKED }
enum class PermissionDecision { ALLOW, DENY, ASK_USER }
enum class GateResult { DENY, ASK, ALLOW, PASS }
enum class RiskLevel { LOW, MEDIUM, HIGH }
enum class SideEffect { READ_ONLY, WRITE, EXEC, NETWORK, DESTRUCTIVE }

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

interface PermissionResponder {
    suspend fun requestApprovalAsync(request: PermissionRequest): PermissionDecision
}

interface PermissionReviewer {
    suspend fun reviewAsync(
        call: ToolCallContext,
        context: PermissionContext,
        gateReason: String,
        risk: RiskLevel,
    ): PermissionOutcome
}

class AllowAllResponder : PermissionResponder {
    override suspend fun requestApprovalAsync(request: PermissionRequest): PermissionDecision = PermissionDecision.ALLOW
}

class PermissionEngine(private val reviewer: PermissionReviewer? = null) {
    private val gate = DeterministicPolicyGate()

    suspend fun decideAsync(call: ToolCallContext, context: PermissionContext): PermissionOutcome {
        val result = gate.evaluate(call, context)
        return when (result.decision) {
            GateResult.DENY -> PermissionOutcome(PermissionDecision.DENY, result.risk, result.reason)
            GateResult.ASK -> PermissionOutcome(PermissionDecision.ASK_USER, result.risk, result.reason)
            GateResult.ALLOW -> PermissionOutcome(PermissionDecision.ALLOW, result.risk, result.reason)
            GateResult.PASS -> reviewer?.reviewAsync(call, context, result.reason, result.risk)
                ?: PermissionOutcome(PermissionDecision.ASK_USER, result.risk, "${result.reason} (no reviewer configured → asking user)")
        }
    }
}

class DeterministicPolicyGate {
    fun evaluate(call: ToolCallContext, ctx: PermissionContext): PermissionGateOutcome {
        if (ctx.profile == PermissionProfile.LOCKED) {
            return PermissionGateOutcome(GateResult.DENY, RiskLevel.LOW, "agent is locked")
        }

        for (path in call.touchedPaths) {
            if (SecretScanner.isSensitivePath(path)) {
                return PermissionGateOutcome(GateResult.DENY, RiskLevel.HIGH, "touches sensitive file: $path")
            }
            if (!WorkspaceSecurity.isWithinWorkspace(path, ctx.workspaceRoot)) {
                return PermissionGateOutcome(GateResult.DENY, RiskLevel.HIGH, "path escapes workspace: $path")
            }
        }

        if (call.risksNetwork) {
            return if (ctx.profile == PermissionProfile.READ_ONLY) {
                PermissionGateOutcome(GateResult.DENY, RiskLevel.MEDIUM, "network not allowed in read_only")
            } else {
                PermissionGateOutcome(GateResult.ASK, RiskLevel.MEDIUM, "network access requested")
            }
        }

        return when (call.sideEffect) {
            SideEffect.READ_ONLY -> PermissionGateOutcome(GateResult.ALLOW, RiskLevel.LOW, "read-only operation within workspace")
            SideEffect.NETWORK -> if (ctx.profile == PermissionProfile.READ_ONLY) {
                PermissionGateOutcome(GateResult.DENY, RiskLevel.MEDIUM, "network not allowed in read_only")
            } else {
                PermissionGateOutcome(GateResult.ASK, RiskLevel.MEDIUM, "network access requested")
            }

            SideEffect.DESTRUCTIVE -> if (ctx.profile == PermissionProfile.READ_ONLY) {
                PermissionGateOutcome(GateResult.DENY, RiskLevel.HIGH, "destructive operation not allowed in read_only")
            } else {
                PermissionGateOutcome(GateResult.ASK, RiskLevel.HIGH, "destructive operation")
            }

            SideEffect.EXEC -> evaluateExec(call, ctx)
            SideEffect.WRITE -> evaluateWrite(call, ctx)
        }
    }

    private fun evaluateExec(call: ToolCallContext, ctx: PermissionContext): PermissionGateOutcome {
        if (!ctx.allowsShell) {
            return PermissionGateOutcome(GateResult.DENY, RiskLevel.HIGH, "shell is disabled in this profile")
        }
        if (ctx.profile == PermissionProfile.READ_ONLY) {
            return PermissionGateOutcome(GateResult.DENY, RiskLevel.HIGH, "shell not allowed in read_only")
        }

        val command = extractShellCommand(call.rawArgs)
        if (ShellInspector.isDangerous(command)) {
            return PermissionGateOutcome(GateResult.DENY, RiskLevel.HIGH, "dangerous shell command")
        }
        if (ShellInspector.risksNetworkOrInstall(command)) {
            return PermissionGateOutcome(GateResult.ASK, RiskLevel.HIGH, "shell command may access network or install packages")
        }
        return if (ShellInspector.isReadOnlyCommand(command)) {
            PermissionGateOutcome(GateResult.ALLOW, RiskLevel.LOW, "read-only shell command")
        } else {
            when (ctx.profile) {
                PermissionProfile.MANUAL -> PermissionGateOutcome(GateResult.ASK, RiskLevel.MEDIUM, "run shell command")
                PermissionProfile.REVIEWED, PermissionProfile.AUTOPILOT -> PermissionGateOutcome(GateResult.PASS, RiskLevel.MEDIUM, "shell command")
                else -> PermissionGateOutcome(GateResult.ASK, RiskLevel.MEDIUM, "run shell command")
            }
        }
    }

    private fun evaluateWrite(call: ToolCallContext, ctx: PermissionContext): PermissionGateOutcome {
        if (ctx.profile == PermissionProfile.READ_ONLY) {
            return PermissionGateOutcome(GateResult.DENY, RiskLevel.MEDIUM, "writes not allowed in read_only")
        }
        if (call.touchedPaths.any { SecretScanner.isProtectedConfigPath(it) }) {
            return PermissionGateOutcome(GateResult.ASK, RiskLevel.HIGH, "modifies lockfile / CI / build config")
        }

        return when (ctx.profile) {
            PermissionProfile.MANUAL -> PermissionGateOutcome(GateResult.ASK, RiskLevel.MEDIUM, "write to workspace")
            PermissionProfile.REVIEWED, PermissionProfile.AUTOPILOT -> PermissionGateOutcome(GateResult.PASS, RiskLevel.LOW, "write within workspace")
            else -> PermissionGateOutcome(GateResult.ASK, RiskLevel.MEDIUM, "write to workspace")
        }
    }

    private fun extractShellCommand(rawArgs: String): String {
        return runCatching {
            val parsed = Json.parseToJsonElement(rawArgs).jsonObject
            parsed["command"]?.let { runCatching { it.jsonPrimitive.content }.getOrNull() } ?: ""
        }.getOrElse { "" }
    }
}

object WorkspaceSecurity {
    fun isWithinWorkspace(candidatePath: String, workspaceRoot: String): Boolean {
        return try {
            val root = File(workspaceRoot).canonicalFile
            val target = File(candidatePath).canonicalFile
            target == root || target.toPath().startsWith(root.toPath())
        } catch (_: Exception) {
            false
        }
    }

    fun resolveInWorkspace(workspaceRoot: String, raw: String): String {
        val root = File(workspaceRoot).canonicalFile
        val path = if (File(raw).isAbsolute) File(raw) else File(root, raw)
        val normalized = path.canonicalFile
        if (!isWithinWorkspace(normalized.path, root.path)) {
            throw IllegalStateException("path escapes workspace: $raw")
        }
        return normalized.path
    }
}

object SecretScanner {
    private val sensitiveBasenames = setOf(
        ".env", ".netrc", ".pgpass", "id_rsa", "id_dsa", "id_ecdsa", "id_ed25519", "credentials", ".npmrc", ".pypirc"
    )
    private val sensitiveExtensions = setOf("pem", "key", "p12", "pfx", "keystore", "jks", "asc")
    private val sensitiveDirHints = arrayOf("/.ssh/", "/.aws/", "/.gnupg/", "/.gpg/", "secrets/", "/.config/gh/")
    private val protectedBasenames = setOf(
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "cargo.lock", "podfile.lock", "gemfile.lock",
        "package.resolved", "poetry.lock"
    )
    private val protectedHints = arrayOf(
        ".github/workflows/", ".gitlab-ci", "/dockerfile", "/makefile", "fastlane/", "/ci/"
    )

    fun isSensitivePath(path: String): Boolean {
        val normalized = path.replace('\\', '/').lowercase()
        val fileName = normalized.substringAfterLast('/')
        if (sensitiveBasenames.contains(fileName)) return true
        if (fileName.startsWith(".env")) return true
        val ext = fileName.substringAfterLast('.', "").lowercase()
        if (ext.isNotBlank() && sensitiveExtensions.contains(ext)) return true
        val padded = "/$normalized"
        return sensitiveDirHints.any { padded.contains(it) }
    }

    fun isProtectedConfigPath(path: String): Boolean {
        val normalized = path.replace('\\', '/').lowercase()
        val fileName = normalized.substringAfterLast('/')
        if (protectedBasenames.contains(fileName)) return true
        val padded = "/$normalized"
        return protectedHints.any { padded.contains(it) }
    }

    fun containsSecret(text: String): Boolean {
        val markers = listOf("-----BEGIN", "PRIVATE KEY", "AKIA", "ASIA", "sk-", "ssh-rsa ", "xoxb-", "xoxp-", "ghp_", "github_pat_", "AIza")
        return markers.any { text.contains(it) }
    }
}

object ShellInspector {
    private val dangerous = arrayOf(
        "sudo", "rm -rf", "rm -fr", "rm -r ", ":(){", "mkfs", "dd if=", "> /dev/sd", "chmod -r 777", "chown -r",
        "/etc/", "~/.ssh", "shutdown", "reboot", "killall"
    )
    private val networkOrInstall = arrayOf(
        "curl ", "wget ", "npm install", "npm i ", "yarn add", "pnpm add", "pip install", "pip3 install",
        "apt ", "apt-get", "brew install", "gem install", "git clone", "git push", "git pull", "git fetch", "nc ", "ssh ", "scp "
    )
    private val readOnlyAllowlist = arrayOf("ls", "pwd", "cat", "grep", "rg", "echo", "head", "tail", "wc", "find", "true")

    fun isDangerous(command: String): Boolean = dangerous.any { command.lowercase().contains(it) }
    fun risksNetworkOrInstall(command: String): Boolean = networkOrInstall.any { command.lowercase().contains(it) }
    fun isReadOnlyCommand(command: String): Boolean {
        val trim = command.trim()
        if (trim.isEmpty()) return false
        val first = trim.split(" ").firstOrNull() ?: return false
        return readOnlyAllowlist.any { it == first } && !isDangerous(command)
    }
}
