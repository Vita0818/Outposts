package com.intatis.cli

import com.intatis.shared.*
import com.intatis.shared.conversation.CodeProjection
import com.intatis.shared.conversation.ConversationProjection
import com.intatis.shared.conversation.CoworkProjection
import com.intatis.shared.provider.ProviderHealthCheckResult
import com.intatis.shared.provider.ProviderRegistry
import kotlinx.coroutines.runBlocking
import java.io.File
import java.util.UUID

private enum class ReplAction {
    Continue,
    Exit,
    SwitchChat,
    SwitchCode,
    SwitchCowork,
}

private fun printHelp() {
    println(
        """Intatis Android CLI

USAGE
  intatis                  Start default mode (INTATIS_MODE)
  intatis chat             Chat mode (no tools)
  intatis code [dir]       Code mode with tools
  intatis cowork [dir]     Multi-agent mode
  intatis settings         Configure base URL, model, API key, workspace
  intatis config           Print current resolved config
  intatis selftest         Offline self-test (no key)
  intatis health           Run provider health checks
  intatis help             Show this help

ENV CONFIG
  INTATIS_BASE_URL   default https://api.openai.com/v1
  INTATIS_API_KEY    required for chat/code/cowork
  INTATIS_MODEL      default gpt-4o-mini
  INTATIS_REASONING   minimal|low|medium|high|off
  INTATIS_MODE       chat|code|cowork
  INTATIS_WORKSPACE   default workspace for code/cowork
  INTATIS_USAGE       0/1 include usage output

IN-SESSION /slash
  /help
  /clear
  /mode <chat|code|cowork>
  /model [name]
  /reasoning [minimal|low|medium|high|off]
  /attach <path>
  /attach clear
  /config
  /workspace [path] (code/cowork)
  /health
  /exit
"""
    )
}

private fun printChatHelp() {
    println("/help         - this help")
    println("/clear        - clear chat history")
    println("/mode         - /mode <chat|code|cowork>")
    println("/model        - /model [name]")
    println("/reasoning    - /reasoning minimal|low|medium|high|off")
    println("/attach       - /attach <path> (text or image) | /attach clear | /attach list")
    println("/config       - print runtime config")
    println("/workspace    - print workspace hint")
    println("/health       - run provider health checks")
    println("/exit         - leave app")
}

private fun printCodeHelp() {
    println("code mode commands:")
    println("/help")
    println("/attach       - /attach <path> (text or image) | /attach clear | /attach list")
    println("/mode <chat|code|cowork>")
    println("/workspace [path]")
    println("/health")
    println("/clear")
    println("/exit")
}

private fun printCoworkHelp() {
    println("cowork commands:")
    println("/help")
    println("/attach       - /attach <path> (text or image) | /attach clear | /attach list")
    println("/agents")
    println("/agent add <name> [path]")
    println("/mode <chat|code|cowork>")
    println("/health       - run provider health checks")
    println("/clear")
    println("/exit")
}

fun main(args: Array<String>) {
    try {
        val config = ConfigStore.load()
        val command = args.firstOrNull()?.lowercase() ?: ""
        val runner = CliRunner(config)

        when (command) {
            "", "chat", "code", "cowork" -> {
                val workspace = if (command == "code" || command == "cowork") args.getOrNull(1) else null
                runBlocking {
                    runModeLoop(runner, parseMode(command.ifBlank { config.defaultMode.name }), workspace)
                }
            }

            "settings" -> runner.showSettingsWizard()
            "config" -> runner.printConfig(config)
            "selftest" -> runBlocking { runner.runSelftestAsync() }
            "health" -> runBlocking { runner.runHealthCheckAsync() }
            "help", "-h", "--help" -> printHelp()
            else -> {
                println("unknown command: $command\n")
                printHelp()
            }
        }
    } catch (ex: Exception) {
        println("error: ${ex.message}")
    }
}

private suspend fun runModeLoop(runner: CliRunner, startMode: IntatisMode, workspaceArg: String?) {
    var mode = startMode
    var workspace = workspaceArg

    while (true) {
        val action = when (mode) {
            IntatisMode.Chat -> runner.runChatAsync()
            IntatisMode.Code -> runner.runCodeAsync(workspace)
            IntatisMode.Cowork -> runner.runCoworkAsync(workspace)
        }

        workspace = null

        when (action) {
            ReplAction.Exit -> return
            ReplAction.SwitchChat -> mode = IntatisMode.Chat
            ReplAction.SwitchCode -> mode = IntatisMode.Code
            ReplAction.SwitchCowork -> mode = IntatisMode.Cowork
            ReplAction.Continue -> {}
        }
    }
}

private fun parseMode(command: String): IntatisMode {
    return try {
        IntatisMode.valueOf(command.replaceFirstChar { it.uppercaseChar() })
    } catch (_: Exception) {
        IntatisMode.Chat
    }
}

private class CliRunner(initialConfig: IntatisConfig) {
    private var config: IntatisConfig = initialConfig
    private val chatEventLog = SessionEventLog("cli-chat")
    private var session = ConversationSession(config, chatEventLog)
    private val chatProjection = ConversationProjection()
    private val codeProjection = CodeProjection()
    private val coworkProjection = CoworkProjection()
    private val permissionResponder = TerminalPermissionResponder()
    private val codeEventLog = SessionEventLog("cli-code")
    private val coworkEventLog = SessionEventLog("cli-cowork")

    private var runtimeModel = config.model
    private var runtimeReasoning = config.reasoning
    private var runtimeWorkspace = resolveWorkspace(config.workspace, null)
    private var chatProjectionLines = 0
    private var codeProjectionLines = 0
    private var coworkProjectionLines = 0

    private var codeSession = createCodeSession(config, runtimeWorkspace)
    private var coworkEngine = createCoworkEngine(config, runtimeWorkspace)
    private val chatAttachments = mutableListOf<ChatAttachment>()
    private val codeAttachments = mutableListOf<ChatAttachment>()
    private val coworkAttachments = mutableListOf<ChatAttachment>()

    fun printConfig(current: IntatisConfig) {
        println("endpoint : ${current.baseUrl}")
        println("model    : ${current.model}")
        println("chat provider       : ${current.chatProviderId}")
        println("agent tool provider : ${current.agentToolProviderId}")
        println("image provider      : ${current.imageProviderId}")
        println("transcription provider : ${current.transcriptionProviderId}")
        println("reasoning: ${current.reasoning ?: "(off)"}")
        println("mode     : ${current.defaultMode}")
        println("workspace: ${current.workspace ?: "(unset)"}")
        println("usage    : ${if (current.includeUsage) "on" else "off"}")
        println("config   : ${ConfigStore.configPath}")
        println("apikey   : ${if (current.apiKey.isBlank()) "(unset)" else "(set, hidden)"}")
    }

    fun showSettingsWizard() {
        var baseUrl = config.baseUrl
        var apiKey = config.apiKey
        var model = config.model
        var reasoning = config.reasoning ?: ""
        var workspace = config.workspace ?: ""
        var mode = config.defaultMode.name.lowercase()
        var chatProviderId = config.chatProviderId
        var agentToolProviderId = config.agentToolProviderId
        var imageProviderId = config.imageProviderId
        var transcriptionProviderId = config.transcriptionProviderId
        var includeUsage = if (config.includeUsage) "1" else "0"

        fun prompt(label: String, current: String): String {
            print("$label [$current]: ")
            val next = readLine()
            return if (next.isNullOrBlank()) current else next.trim()
        }

        baseUrl = prompt("Base URL", baseUrl)
        model = prompt("Model", model)
        reasoning = prompt("Reasoning [minimal|low|medium|high|off]", reasoning)
        apiKey = prompt("API Key", apiKey)
        chatProviderId = prompt("Chat Provider ID", chatProviderId)
        agentToolProviderId = prompt("Agent Tool Provider ID", agentToolProviderId)
        imageProviderId = prompt("Image Provider ID", imageProviderId)
        transcriptionProviderId = prompt("Transcription Provider ID", transcriptionProviderId)
        workspace = prompt("Default Workspace (optional)", workspace)
        mode = prompt("Default mode (chat|code|cowork)", mode)
        includeUsage = prompt("Show usage in responses? (1 on, 0 off)", includeUsage)

        val resolvedReasoning = run {
            val (ok, normalized) = CommandParser.parseReasoning(reasoning)
            if (!ok && reasoning.isNotBlank()) {
                println("invalid reasoning value, keeping previous.")
                config.reasoning
            } else {
                normalized
            }
        }

        val nextMode = try {
            IntatisMode.valueOf(mode.replaceFirstChar { it.uppercaseChar() })
        } catch (_: Exception) {
            config.defaultMode
        }

        val next = IntatisConfig(
            baseUrl = baseUrl,
            apiKey = apiKey,
            model = model,
            selectedModel = model,
            reasoning = resolvedReasoning,
            defaultMode = nextMode,
            workspace = workspace.ifBlank { null },
            chatProviderId = chatProviderId.ifBlank { "openai" },
            agentToolProviderId = agentToolProviderId.ifBlank { "openai" },
            imageProviderId = imageProviderId.ifBlank { "openai" },
            transcriptionProviderId = transcriptionProviderId.ifBlank { "openai" },
            includeUsage = includeUsage != "0",
        )

        ConfigStore.save(next)
        config = next
        runtimeModel = next.model
        runtimeReasoning = next.reasoning
        runtimeWorkspace = resolveWorkspace(next.workspace, null)
        session = ConversationSession(next, chatEventLog)
        codeSession = createCodeSession(next, runtimeWorkspace)
        coworkEngine = createCoworkEngine(next, runtimeWorkspace)
        println("Saved.")
    }

    suspend fun runChatAsync(): ReplAction {
        println("Intatis chat mode. /help for commands.")
        while (true) {
            if (chatAttachments.isNotEmpty()) {
                println("[${describeAttachmentQueue(chatAttachments)} queued for next message]")
            }

            print("> ")
            val raw = readLine() ?: return ReplAction.Exit
            val input = raw.trim()
            if (input.isBlank()) continue

            val action = handleChatSlash(input)
            if (action != ReplAction.Continue) {
                if (action == ReplAction.Exit) return ReplAction.Exit
                if (action != ReplAction.Continue) return action
            }

            if (!input.startsWith('/')) {
                try {
                    val (effectiveText, imageAttachments) = prepareQueuedMessage(input, chatAttachments)
                val (reply, elapsed, usage) = session.sendUserMessageAsync(
                    userText = effectiveText,
                    model = runtimeModel,
                    reasoning = runtimeReasoning,
                    attachments = imageAttachments,
                    includeUsage = config.includeUsage,
                )
                appendChatProjectionLines()
                if (config.includeUsage && !usage.isNullOrBlank()) {
                    println("usage: $usage")
                }
                println("time: ${elapsed.toMillis()}ms\n")
                } catch (ex: Exception) {
                    println("error: ${ex.message}")
                    if (config.apiKey.isBlank()) {
                        println("Hint: run `intatis settings` or set INTATIS_API_KEY")
                    }
                }
            }
        }
    }

    suspend fun runCodeAsync(workspaceArg: String?): ReplAction {
        if (!workspaceArg.isNullOrBlank()) {
            runtimeWorkspace = resolveWorkspace(config.workspace, workspaceArg)
            configureSessions(runtimeWorkspace)
        }

        println("Code mode: workspace = $runtimeWorkspace")
        println("Describe what to do. The agent decides and runs tools.")
        while (true) {
            if (codeAttachments.isNotEmpty()) {
                println("[${describeAttachmentQueue(codeAttachments)} queued for next code message]")
            }

            print("code> ")
            val raw = readLine() ?: return ReplAction.Exit
            val text = raw.trim()
            if (text.isBlank()) continue

            val action = if (text.startsWith('/')) {
                handleCodeSlash(text)
            } else {
                ReplAction.Continue
            }

            when (action) {
                ReplAction.Exit -> return ReplAction.Exit
                ReplAction.SwitchChat -> return ReplAction.SwitchChat
                ReplAction.SwitchCowork -> return ReplAction.SwitchCowork
                else -> {}
            }

            if (text.startsWith('/')) {
                continue
            }

            try {
                val (effectiveText, imageAttachments) = prepareQueuedMessage(text, codeAttachments)
                val (reply, elapsed, usage) = codeSession.sendAsync(
                    userText = effectiveText,
                    model = runtimeModel,
                    reasoning = runtimeReasoning,
                    userGoal = "code mode",
                    attachments = imageAttachments,
                    includeUsage = config.includeUsage,
                )
                appendCodeProjectionLines()
                if (config.includeUsage && !usage.isNullOrBlank()) {
                    println("usage: $usage")
                }
                println("time: ${elapsed.toMillis()}ms\n")
            } catch (ex: Exception) {
                println("error: ${ex.message}\n")
            }
        }
    }

    suspend fun runCoworkAsync(workspaceArg: String?): ReplAction {
        if (!workspaceArg.isNullOrBlank()) {
            runtimeWorkspace = resolveWorkspace(config.workspace, workspaceArg)
            configureSessions(runtimeWorkspace)
        }

        println("Cowork mode: default workspace = $runtimeWorkspace")
        println("Examples: /agents, /agent add reviewer .")
        println("Use @agent message for explicit routing, or default agent")
        while (true) {
            if (coworkAttachments.isNotEmpty()) {
                println("[${describeAttachmentQueue(coworkAttachments)} queued for next cowork message]")
            }

            print("cowork> ")
            val raw = readLine() ?: return ReplAction.Exit
            val text = raw.trim()
            if (text.isBlank()) continue

            val action = if (text.startsWith('/')) {
                handleCoworkSlash(text)
            } else {
                ReplAction.Continue
            }

            when (action) {
                ReplAction.Exit -> return ReplAction.Exit
                ReplAction.SwitchChat -> return ReplAction.SwitchChat
                ReplAction.SwitchCode -> return ReplAction.SwitchCode
                else -> {}
            }

            if (text.startsWith('/')) {
                continue
            }

            var target: String? = null
            var body = text
            if (text.startsWith('@')) {
                val split = text.substring(1).trim().split(Regex("\\s+"), limit = 2)
                if (split.isNotEmpty()) {
                    target = split[0]
                    body = split.getOrNull(1) ?: ""
                }
            }

            try {
                val (effectiveBody, imageAttachments) = prepareQueuedMessage(body, coworkAttachments)
                if (target == null) {
                    runBlockingOrNull {
                        coworkEngine.sendAsync(
                            text = effectiveBody,
                            target = null,
                            model = runtimeModel,
                            reasoning = runtimeReasoning,
                            images = imageAttachments,
                            includeUsage = config.includeUsage,
                        )
                    } ?: "(no response)"
                } else {
                    runBlockingOrNull {
                        coworkEngine.askAsync(
                            from = "cli",
                            to = target,
                            question = effectiveBody,
                            images = imageAttachments,
                            includeUsage = config.includeUsage,
                        )
                    } ?: "(no response)"
                }
                appendCoworkProjectionLines()
            } catch (ex: Exception) {
                println("error: ${ex.message}\n")
            }
        }
    }

    suspend fun runSelftestAsync() {
        val temp = File(System.getProperty("java.io.tmpdir"), "intatis-android-selftest-${UUID.randomUUID()}")
        temp.mkdirs()
        try {
            WorkspaceTools.writeText(temp.absolutePath, "readme.txt", "hello intatis")
            val text = WorkspaceTools.readText(temp.absolutePath, "readme.txt")
            val hits = WorkspaceTools.search(temp.absolutePath, "intatis")
            val ok = text == "hello intatis" && hits.isNotEmpty()
            println(if (ok) "SELFTEST: OK" else "SELFTEST: FAIL")
            println("temp workspace: ${temp.absolutePath}")
        } finally {
            temp.deleteRecursively()
        }
    }

    suspend fun runHealthCheckAsync() {
        try {
            val registry = ProviderRegistry(config)
            val suite = registry.checkHealth(config.chatProviderId, config.agentToolProviderId)
            printHealthCheckResult(suite.chat)
            printHealthCheckResult(suite.agentTool)
        } catch (ex: Exception) {
            println("health check failed: ${ex.message}")
        }
    }

    private fun handleChatSlash(input: String): ReplAction {
        if (!input.startsWith('/')) return ReplAction.Continue
        val tokens = CommandParser.parseTokens(input.substring(1))
        if (tokens.isEmpty()) return ReplAction.Continue

        return when (tokens[0].lowercase()) {
            "help" -> {
                printChatHelp()
                ReplAction.Continue
            }
            "clear" -> {
                session.clear()
                chatProjectionLines = chatProjection.render(chatEventLog.readAll()).size
                println("session cleared.")
                ReplAction.Continue
            }
            "mode" -> {
                if (tokens.size < 2) {
                    println("usage: /mode <chat|code|cowork>")
                    ReplAction.Continue
                } else {
                    when (parseMode(tokens[1])) {
                        IntatisMode.Chat -> {
                            println("switching to chat mode")
                            ReplAction.SwitchChat
                        }

                        IntatisMode.Code -> {
                            println("switching to code mode")
                            ReplAction.SwitchCode
                        }

                        IntatisMode.Cowork -> {
                            println("switching to cowork mode")
                            ReplAction.SwitchCowork
                        }
                    }
                }
            }

            "model" -> {
                if (tokens.size < 2) {
                    println("model: $runtimeModel")
                } else {
                    runtimeModel = tokens[1]
                    println("model -> $runtimeModel")
                }
                ReplAction.Continue
            }

            "reasoning" -> {
                if (tokens.size < 2) {
                    println("reasoning: ${runtimeReasoning ?: "(off)"}")
                } else {
                    val candidate = tokens[1]
                    val normalized = CommandParser.parseReasoning(candidate)
                    if (!normalized.first) {
                        println("usage: /reasoning minimal|low|medium|high|off")
                    } else {
                        runtimeReasoning = normalized.second
                        println("reasoning -> ${runtimeReasoning ?: "off"}")
                    }
                }
                ReplAction.Continue
            }

            "attach" -> {
                if (tokens.size == 1) {
                    println(if (chatAttachments.isEmpty()) "no attachments queued." else describeAttachmentQueue(chatAttachments))
                    ReplAction.Continue
                } else {
                    val arg = tokens.drop(1).joinToString(" ")
                    if (arg.equals("clear", ignoreCase = true)) {
                        chatAttachments.clear()
                        println("attachments cleared.")
                        ReplAction.Continue
                    } else if (arg.equals("list", ignoreCase = true)) {
                        println(describeAttachmentQueue(chatAttachments))
                        ReplAction.Continue
                    } else {
                        when (val result = AttachmentLoader.load(arg)) {
                            is AttachmentLoadResult -> {
                                if (result.isSuccess) {
                                    result.attachment?.let { chatAttachments.add(it) }
                                    println("attached ${result.attachment?.name ?: "file"}")
                                } else {
                                    println(result.failure)
                                }
                            }
                        }
                        ReplAction.Continue
                    }
                }
            }

            "config" -> {
                printRuntimeConfig()
                ReplAction.Continue
            }

            "workspace" -> {
                println("workspace: $runtimeWorkspace")
                ReplAction.Continue
            }

            "health" -> {
                runBlocking {
                    runHealthCheckAsync()
                }
                ReplAction.Continue
            }

            "exit", "quit" -> ReplAction.Exit
            else -> {
                println("unknown command: /${tokens[0]}")
                ReplAction.Continue
            }
        }
    }

    private fun handleCodeSlash(input: String): ReplAction {
        if (!input.startsWith('/')) return ReplAction.Continue
        val tokens = CommandParser.parseTokens(input.substring(1))
        if (tokens.isEmpty()) return ReplAction.Continue

        return when (tokens[0].lowercase()) {
            "help" -> {
                printCodeHelp()
                ReplAction.Continue
            }
            "mode" -> {
                if (tokens.size < 2) {
                    println("usage: /mode <chat|code|cowork>")
                    return ReplAction.Continue
                }
                when (parseMode(tokens[1])) {
                    IntatisMode.Chat -> {
                        println("switching to chat mode")
                        ReplAction.SwitchChat
                    }
                    IntatisMode.Code -> {
                        println("already in code mode")
                        ReplAction.Continue
                    }
                    IntatisMode.Cowork -> {
                        println("switching to cowork mode")
                        ReplAction.SwitchCowork
                    }
                }
            }
            "workspace" -> {
                if (tokens.size == 1) {
                    println("workspace: $runtimeWorkspace")
                } else {
                    val requested = tokens.drop(1).joinToString(" ")
                    runtimeWorkspace = resolveWorkspace(config.workspace, requested)
                    configureSessions(runtimeWorkspace)
                    println("workspace set to $runtimeWorkspace")
                }
                ReplAction.Continue
            }
            "attach" -> {
                if (tokens.size == 1) {
                    println(if (codeAttachments.isEmpty()) "no attachments queued. usage: /attach <path>" else describeAttachmentQueue(codeAttachments))
                    ReplAction.Continue
                } else {
                    val arg = tokens.drop(1).joinToString(" ")
                    if (arg.equals("clear", ignoreCase = true)) {
                        codeAttachments.clear()
                        println("code attachments cleared.")
                        ReplAction.Continue
                    } else if (arg.equals("list", ignoreCase = true)) {
                        println(describeAttachmentQueue(codeAttachments))
                        ReplAction.Continue
                    } else {
                        when (val result = AttachmentLoader.load(arg)) {
                            is AttachmentLoadResult -> {
                                if (result.isSuccess) {
                                    result.attachment?.let { codeAttachments.add(it) }
                                    println("attached ${result.attachment?.name ?: "file"}")
                                } else {
                                    println(result.failure)
                                }
                            }
                        }
                        ReplAction.Continue
                    }
                }
            }
            "clear" -> {
                codeSession.clear()
                codeProjectionLines = codeProjection.render(codeEventLog.readAll()).size
                println("code session cleared.")
                ReplAction.Continue
            }

            "health" -> {
                runBlocking {
                    runHealthCheckAsync()
                }
                ReplAction.Continue
            }

            "exit", "quit" -> ReplAction.Exit
            else -> {
                println("unknown command: /${tokens[0]}")
                ReplAction.Continue
            }
        }
    }

    private fun handleCoworkSlash(input: String): ReplAction {
        if (!input.startsWith('/')) return ReplAction.Continue
        val tokens = CommandParser.parseTokens(input.substring(1))
        if (tokens.isEmpty()) return ReplAction.Continue

        return when (tokens[0].lowercase()) {
            "help" -> {
                printCoworkHelp()
                ReplAction.Continue
            }
            "agents" -> {
                val names = coworkEngine.agentsNames
                println("agents: ${if (names.isEmpty()) "(none)" else names.joinToString(", ")}")
                ReplAction.Continue
            }
            "agent" -> {
                if (tokens.size < 3 || !tokens[1].equals("add", ignoreCase = true)) {
                    println("usage: /agent add <name> [path]")
                    return ReplAction.Continue
                }

                val name = tokens[2]
                val workspace = if (tokens.size > 3) tokens.drop(3).joinToString(" ") else runtimeWorkspace
                println(coworkEngine.attach(name, workspace))
                ReplAction.Continue
            }
            "mode" -> {
                if (tokens.size < 2) {
                    println("usage: /mode <chat|code|cowork>")
                    return ReplAction.Continue
                }
                when (parseMode(tokens[1])) {
                    IntatisMode.Chat -> {
                        println("switching to chat mode")
                        ReplAction.SwitchChat
                    }
                    IntatisMode.Code -> {
                        println("switching to code mode")
                        ReplAction.SwitchCode
                    }
                    IntatisMode.Cowork -> {
                        println("already in cowork mode")
                        ReplAction.Continue
                    }
                }
            }
            "attach" -> {
                if (tokens.size == 1) {
                    println(if (coworkAttachments.isEmpty()) "no attachments queued. usage: /attach <path>" else describeAttachmentQueue(coworkAttachments))
                    ReplAction.Continue
                } else {
                    val arg = tokens.drop(1).joinToString(" ")
                    if (arg.equals("clear", ignoreCase = true)) {
                        coworkAttachments.clear()
                        println("cowork attachments cleared.")
                        ReplAction.Continue
                    } else if (arg.equals("list", ignoreCase = true)) {
                        println(describeAttachmentQueue(coworkAttachments))
                        ReplAction.Continue
                    } else {
                        when (val result = AttachmentLoader.load(arg)) {
                            is AttachmentLoadResult -> {
                                if (result.isSuccess) {
                                    result.attachment?.let { coworkAttachments.add(it) }
                                    println("attached ${result.attachment?.name ?: "file"}")
                                } else {
                                    println(result.failure)
                                }
                            }
                        }
                        ReplAction.Continue
                    }
                }
            }
            "clear" -> {
                coworkEngine.clear()
                coworkProjectionLines = coworkProjection.render(coworkEventLog.readAll()).size
                println("cowork sessions cleared.")
                ReplAction.Continue
            }

            "health" -> {
                runBlocking {
                    runHealthCheckAsync()
                }
                ReplAction.Continue
            }
            "exit", "quit" -> ReplAction.Exit
            else -> {
                println("unknown command: /${tokens[0]}")
                ReplAction.Continue
            }
        }
    }

    private fun prepareQueuedMessage(
        userText: String,
        attachmentQueue: MutableList<ChatAttachment>,
    ): Pair<String, List<ImageAttachment>> {
        if (attachmentQueue.isEmpty()) {
            return userText to emptyList()
        }

        val builder = StringBuilder(userText)
        val images = mutableListOf<ImageAttachment>()
        attachmentQueue.forEach { attachment ->
            when (attachment) {
                is TextAttachment -> {
                    builder.appendLine()
                    builder.appendLine()
                    builder.appendLine("[attached file: ${attachment.name}]")
                    builder.appendLine(attachment.content)
                }
                is ImageAttachment -> images.add(attachment)
            }
        }

        attachmentQueue.clear()
        return builder.toString() to images
    }

    private fun describeAttachmentQueue(attachmentQueue: List<ChatAttachment>): String {
        val textCount = attachmentQueue.count { it is TextAttachment }
        val imageCount = attachmentQueue.count { it is ImageAttachment }
        val names = attachmentQueue.joinToString(", ") { it.name }
        return "${attachmentQueue.size} attachment(s) [text=$textCount, image=$imageCount] ($names)"
    }

    private fun printRuntimeConfig() {
        println("endpoint : ${config.baseUrl}")
        println("model    : $runtimeModel")
        println("chat provider       : ${config.chatProviderId}")
        println("agent tool provider : ${config.agentToolProviderId}")
        println("image provider      : ${config.imageProviderId}")
        println("transcription provider : ${config.transcriptionProviderId}")
        println("reasoning: ${runtimeReasoning ?: "(off)"}")
        println("mode     : ${config.defaultMode}")
        println("workspace: ${config.workspace ?: "(unset)"}")
        println("usage    : ${if (config.includeUsage) "on" else "off"}")
        println("config   : ${ConfigStore.configPath}")
        println("apikey   : ${if (config.apiKey.isBlank()) "(unset)" else "(set, hidden)"}")
    }

    private fun configureSessions(workspace: String) {
        runtimeWorkspace = workspace
        codeSession = createCodeSession(config, workspace)
        coworkEngine = createCoworkEngine(config, workspace)
    }

    private fun createCodeSession(currentConfig: IntatisConfig, workspace: String): CodeAgentSession {
        val shell = ProcessShellRunner()
        val git = ProcessGitService(shell)
        val reviewer = ModelPermissionReviewer(currentConfig, runtimeModel)

        return CodeAgentSession(
            currentConfig,
            workspace,
            "cli-code",
            PermissionProfile.Reviewed,
            shell,
            git,
            responder = permissionResponder,
            permissionReviewer = reviewer,
            eventSink = codeEventLog,
            allowsShell = true,
            maxIterations = 8,
        )
    }

    private fun createCoworkEngine(currentConfig: IntatisConfig, workspace: String): CoworkEngine {
        val shell = ProcessShellRunner()
        val git = ProcessGitService(shell)
        val reviewer = ModelPermissionReviewer(currentConfig, runtimeModel)

        return CoworkEngine(
            currentConfig,
            workspace,
            shell,
            git,
            permissionResponder,
            PermissionProfile.Reviewed,
            coworkEventLog,
            allowsShell = true,
            maxIterations = 8,
            permissionReviewer = reviewer,
        )
    }

    private fun resolveWorkspace(configured: String?, requested: String?): String {
        val source = requested?.ifBlank { null } ?: configured?.ifBlank { null } ?: System.getProperty("user.dir")
        return WorkspaceTools.resolveWorkspace(source, null)
    }

    private fun appendChatProjectionLines() {
        val records = chatEventLog.readAll()
        val rendered = chatProjection.render(records)
        for (index in chatProjectionLines until rendered.size) {
            val line = rendered[index]
            println("${line.sender}: ${line.text}")
        }
        chatProjectionLines = rendered.size
    }

    private fun appendCodeProjectionLines() {
        val records = codeEventLog.readAll()
        val rendered = codeProjection.render(records)
        for (index in codeProjectionLines until rendered.size) {
            val line = rendered[index]
            println("${line.sender}: ${line.text}")
        }
        codeProjectionLines = rendered.size
    }

    private fun appendCoworkProjectionLines() {
        val records = coworkEventLog.readAll()
        val rendered = coworkProjection.render(records)
        for (index in coworkProjectionLines until rendered.size) {
            val line = rendered[index]
            println("${line.sender}: ${line.text}")
        }
        coworkProjectionLines = rendered.size
    }
}

private class TerminalPermissionResponder : IPermissionResponder {
    override suspend fun requestApprovalAsync(request: PermissionRequest): PermissionDecision {
        while (true) {
            println("Permission requested:")
            println("  tool: ${request.tool}")
            println("  agent: ${request.agent ?: "(shared)"}")
            println("  risk: ${request.risk}")
            println("  reason: ${request.reason}")
            println("  args: ${request.args}")
            print("Approve [y=allow, n=deny, q=quit]? ")

            when (readLine()?.trim()?.lowercase()) {
                "y", "yes", "allow" -> return PermissionDecision.Allow
                "n", "no", "deny" -> return PermissionDecision.Deny
                "q", "quit", "exit" -> return PermissionDecision.Deny
                else -> continue
            }
        }
    }
}

private fun runBlockingOrNull(block: suspend () -> String): String? {
    return try {
        runBlocking { block() }
    } catch (_: Throwable) {
        null
    }
}

private fun printHealthCheckResult(result: ProviderHealthCheckResult) {
    val status = if (result.isHealthy) "PASS" else "FAIL"
    println("[$status] ${result.role} (${result.providerId}/${result.model})")
    println("  latency  : ${result.latency.toMillis()}ms")
    println("  status   : ${result.message}")
    result.responsePreview?.let { preview ->
        if (preview.isNotBlank()) {
            println("  preview  : $preview")
        }
    }
}
