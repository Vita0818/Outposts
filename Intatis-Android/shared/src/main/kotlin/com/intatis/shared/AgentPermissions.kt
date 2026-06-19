package com.intatis.shared

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

    private fun evaluateExec(call: ToolCallContext, ctx: PermissionContext): PermissionGateOutcome {
        if (!ctx.allowsShell) {
            return PermissionGateOutcome(GateResult.Deny, RiskLevel.High, "shell is disabled in this profile")
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
        if (ShellInspector.isReadOnlyCommand(command)) {
            return PermissionGateOutcome(GateResult.Allow, RiskLevel.Low, "read-only shell command")
        }

        return when (ctx.profile) {
            PermissionProfile.Manual -> PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "run shell command")
            PermissionProfile.Reviewed, PermissionProfile.Autopilot -> PermissionGateOutcome(GateResult.Pass, RiskLevel.Medium, "shell command")
            else -> PermissionGateOutcome(GateResult.Ask, RiskLevel.Medium, "run shell command")
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

    private val sensitiveDirHints = arrayOf("/.ssh/", "/.aws/", "/.gnupg/", "/.gpg/", "secrets/", "/.config/gh/")

    private val protectedBasenames = setOf(
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "cargo.lock",
        "podfile.lock", "gemfile.lock", "package.resolved", "poetry.lock"
    )
    private val protectedHints = arrayOf(".github/workflows/", ".gitlab-ci", "/dockerfile", "/makefile", "fastlane/", "/ci/")

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

    private val readonlyAllowlist = arrayOf("ls", "pwd", "cat", "grep", "rg", "echo", "head", "tail", "wc", "find", "true", "git status", "git diff")

    fun isDangerous(command: String): Boolean {
        val lower = command.lowercase()
        return dangerous.any { lower.contains(it) }
    }

    fun risksNetworkOrInstall(command: String): Boolean {
        val lower = command.lowercase()
        return networkOrInstall.any { lower.contains(it) }
    }

    fun isReadOnlyCommand(command: String): Boolean {
        val trimmed = command.trim()
        if (trimmed.isEmpty()) return false
        val first = trimmed.split(' ', limit = 2).firstOrNull() ?: ""
        return !isDangerous(command) && readonlyAllowlist.any { first == it }
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
