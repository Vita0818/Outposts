package com.intatis.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.BackHandler
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarDuration
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TextField
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.runtime.snapshots.SnapshotStateList
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.intatis.shared.AttachmentLoadResult
import com.intatis.shared.AttachmentLoader
import com.intatis.shared.ChatAttachment
import com.intatis.shared.CodeAgentSession
import com.intatis.shared.CoworkEngine
import com.intatis.shared.ConfigStore
import com.intatis.shared.ConversationSession
import com.intatis.shared.ImageAttachment
import com.intatis.shared.IntatisConfig
import com.intatis.shared.IntatisMode
import com.intatis.shared.IPermissionResponder
import com.intatis.shared.ModelPermissionReviewer
import com.intatis.shared.PermissionDecision
import com.intatis.shared.PermissionProfile
import com.intatis.shared.PermissionRequest
import com.intatis.shared.ProcessGitService
import com.intatis.shared.ProcessShellRunner
import com.intatis.shared.CommandParser
import com.intatis.shared.SessionEventLog
import com.intatis.shared.TextAttachment
import com.intatis.shared.WorkspaceTools
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext

private enum class UiMode {
    Chat,
    Code,
    Cowork,
}

private sealed interface ReplAction {
    data object Continue : ReplAction
    data object Exit : ReplAction
    data class Switch(val mode: UiMode) : ReplAction
}

data class UiLine(
    val sender: String,
    val text: String,
    val isError: Boolean = false,
)

data class PendingPermissionState(
    val request: PermissionRequest,
    val decision: CompletableDeferred<PermissionDecision>,
)

data class EditConfig(
    val baseUrl: String,
    val apiKey: String,
    val model: String,
    val reasoning: String,
    val workspace: String,
    val defaultMode: String,
    val includeUsage: Boolean,
)

private fun IntatisMode.toUiMode(): UiMode = when (this) {
    IntatisMode.Chat -> UiMode.Chat
    IntatisMode.Code -> UiMode.Code
    IntatisMode.Cowork -> UiMode.Cowork
}

private fun parseMode(value: String): UiMode? = when (value.lowercase()) {
    "chat" -> UiMode.Chat
    "code" -> UiMode.Code
    "cowork" -> UiMode.Cowork
    else -> null
}

private class AndroidPermissionResponder(
    private val onRequest: suspend (PermissionRequest) -> PermissionDecision,
) : IPermissionResponder {
    override suspend fun requestApprovalAsync(request: PermissionRequest): PermissionDecision = onRequest(request)
}

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContent {
            MaterialTheme {
                val viewModel: MainViewModel = viewModel()
                val snackbarHostState = remember { SnackbarHostState() }
                val scope = rememberCoroutineScope()

                var showSettings by rememberSaveable { mutableStateOf(false) }

                val mode = viewModel.runtimeMode
                val chatMessages = viewModel.chatMessages
                val codeMessages = viewModel.codeMessages
                val coworkMessages = viewModel.coworkMessages
                val attachmentCount = viewModel.attachmentsCount
                val workspace = viewModel.runtimeWorkspace
                val pendingPermission = viewModel.pendingPermission

                BackHandler {
                    finish()
                }

                Surface {
                    Scaffold(
                        topBar = {
                            TopAppBar(
                                title = { Text("Intatis Android") },
                                actions = {
                                    IconButton(onClick = { showSettings = true }) {
                                        Icon(Icons.Default.Settings, contentDescription = "Settings")
                                    }
                                },
                            )
                        },
                        snackbarHost = { SnackbarHost(hostState = snackbarHostState) },
                    ) { insets ->
                        Column(
                            Modifier
                                .fillMaxSize()
                                .padding(insets)
                                .padding(12.dp),
                        ) {
                            val selectedTab = when (mode) {
                                UiMode.Chat -> 0
                                UiMode.Code -> 1
                                UiMode.Cowork -> 2
                            }

                            TabRow(selectedTabIndex = selectedTab) {
                                Tab(selectedTab == 0, onClick = { viewModel.switchMode(UiMode.Chat) }) { Text("Chat") }
                                Tab(selectedTab == 1, onClick = { viewModel.switchMode(UiMode.Code) }) { Text("Code") }
                                Tab(selectedTab == 2, onClick = { viewModel.switchMode(UiMode.Cowork) }) { Text("Cowork") }
                            }

                            Spacer(modifier = Modifier.height(8.dp))

                            when (mode) {
                                UiMode.Chat -> ChatPanel(
                                    messages = chatMessages,
                                    attachmentsQueued = attachmentCount,
                                    workspace = workspace,
                                    onSend = { text ->
                                        when (val action = viewModel.sendChat(text)) {
                                            ReplAction.Exit -> finish()
                                            is ReplAction.Switch -> viewModel.switchMode(action.mode)
                                            ReplAction.Continue -> Unit
                                        }
                                    },
                                )

                                UiMode.Code -> CodePanel(
                                    messages = codeMessages,
                                    workspace = workspace,
                                    onSend = { text ->
                                        when (val action = viewModel.sendCode(text)) {
                                            ReplAction.Exit -> finish()
                                            is ReplAction.Switch -> viewModel.switchMode(action.mode)
                                            ReplAction.Continue -> Unit
                                        }
                                    },
                                )

                                UiMode.Cowork -> CoworkPanel(
                                    messages = coworkMessages,
                                    workspace = workspace,
                                    agents = viewModel.coworkAgents,
                                    onSend = { text ->
                                        when (val action = viewModel.sendCowork(text)) {
                                            ReplAction.Exit -> finish()
                                            is ReplAction.Switch -> viewModel.switchMode(action.mode)
                                            ReplAction.Continue -> Unit
                                        }
                                    },
                                )
                            }
                        }
                    }

                    if (showSettings) {
                        SettingsDialog(
                            snapshot = viewModel.configSnapshot,
                            onSave = {
                                val ok = viewModel.applyConfig(it)
                                scope.launch {
                                    snackbarHostState.showSnackbar(
                                        message = if (ok) "Settings saved." else "Settings not saved.",
                                        duration = SnackbarDuration.Short,
                                    )
                                }
                                showSettings = false
                            },
                            onDismiss = { showSettings = false },
                        )
                    }

                    pendingPermission?.let { state ->
                        AlertDialog(
                            onDismissRequest = { viewModel.resolvePermission(allow = false) },
                            title = { Text("Permission request") },
                            text = {
                                Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                                    Text("tool: ${state.request.tool}")
                                    Text("agent: ${state.request.agent ?: \"(shared)\"}")
                                    Text("risk: ${state.request.risk}")
                                    Text("reason: ${state.request.reason}")
                                    Text("args: ${state.request.args}")
                                }
                            },
                            confirmButton = {
                                Button(onClick = { viewModel.resolvePermission(allow = true) }) {
                                    Text("Allow")
                                }
                            },
                            dismissButton = {
                                TextButton(onClick = { viewModel.resolvePermission(allow = false) }) {
                                    Text("Deny")
                                }
                            },
                        )
                    }
                }
            }
        }
    }
}

class MainViewModel : ViewModel() {
    private var config: IntatisConfig = ConfigStore.load()

    var runtimeMode by mutableStateOf(config.defaultMode.toUiMode())
        private set

    private var runtimeModel by mutableStateOf(config.model)
    private var runtimeReasoning by mutableStateOf(config.reasoning)
    var runtimeWorkspace by mutableStateOf(
        runCatching {
            resolveWorkspace(config.workspace)
        }.getOrElse {
            (System.getProperty("user.dir") ?: ".")
        }
    )
        private set

    private val attachmentQueue = mutableListOf<ChatAttachment>()
    val attachmentsCount: Int
        get() = attachmentQueue.size

    val chatMessages: SnapshotStateList<UiLine> = mutableStateListOf(
        UiLine("system", "Chat mode ready"),
    )
    val codeMessages: SnapshotStateList<UiLine> = mutableStateListOf(
        UiLine("system", "Code mode ready"),
    )
    val coworkMessages: SnapshotStateList<UiLine> = mutableStateListOf(
        UiLine("system", "Cowork mode ready"),
    )

    val configSnapshot: IntatisConfig
        get() = config

    val coworkEngineAgents: List<String>
        get() = coworkEngine.value.agentsNames

    private var chatSession = mutableStateOf(ConversationSession(config))
    private var codeSession = mutableStateOf(createCodeSession(config, runtimeWorkspace))
    private var coworkEngine = mutableStateOf(createCoworkEngine(config, runtimeWorkspace))
    private val eventSink = SessionEventLog("gui")

    var pendingPermission by mutableStateOf<PendingPermissionState?>(null)
        private set

    private val permissionMutex = Mutex()
    private val permissionResponder = AndroidPermissionResponder { request ->
        val deferred = CompletableDeferred<PermissionDecision>()
        withContext(Dispatchers.Main) {
            permissionMutex.withLock {
                pendingPermission?.let {
                    if (!it.decision.isCompleted) {
                        it.decision.complete(PermissionDecision.Deny)
                    }
                }
                pendingPermission = PendingPermissionState(request, deferred)
            }
        }
        deferred.await()
    }

    fun switchMode(mode: UiMode) {
        runtimeMode = mode
    }

    fun sendChat(raw: String): ReplAction {
        val text = raw.trim()
        if (text.isBlank()) return ReplAction.Continue

        chatMessages.add(UiLine("you", text))

        if (text.startsWith('/')) {
            return handleChatSlash(text)
        }

        val currentSession = chatSession.value
        val currentModel = runtimeModel
        val currentReasoning = runtimeReasoning
        val usageEnabled = config.includeUsage

        val (effectiveText, imageAttachments) = prepareQueuedMessage(text)
        viewModelScope.launch {
            try {
                val (reply, _, usage) = currentSession.sendUserMessageAsync(
                    userText = effectiveText,
                    model = currentModel,
                    reasoning = currentReasoning,
                    attachments = imageAttachments,
                )
                chatMessages.add(UiLine("assistant", reply.content))
                if (usageEnabled && !usage.isNullOrBlank()) {
                    chatMessages.add(UiLine("system", "usage: $usage"))
                }
            } catch (ex: Exception) {
                chatMessages.add(UiLine("system", "error: ${ex.message}", isError = true))
            }
        }

        return ReplAction.Continue
    }

    fun sendCode(raw: String): ReplAction {
        val text = raw.trim()
        if (text.isBlank()) return ReplAction.Continue

        codeMessages.add(UiLine("you", text))

        if (text.startsWith('/')) {
            return handleCodeSlash(text)
        }

        val currentSession = codeSession.value
        val currentModel = runtimeModel
        val currentReasoning = runtimeReasoning
        val usageEnabled = config.includeUsage
        viewModelScope.launch {
            try {
                val (reply, _, usage) = currentSession.sendAsync(
                    userText = text,
                    model = currentModel,
                    reasoning = currentReasoning,
                    userGoal = "code mode",
                )
                codeMessages.add(UiLine("assistant", reply))
                if (usageEnabled && !usage.isNullOrBlank()) {
                    codeMessages.add(UiLine("system", "usage: $usage"))
                }
            } catch (ex: Exception) {
                codeMessages.add(UiLine("system", "error: ${ex.message}", isError = true))
            }
        }

        return ReplAction.Continue
    }

    fun sendCowork(raw: String): ReplAction {
        val text = raw.trim()
        if (text.isBlank()) return ReplAction.Continue

        coworkMessages.add(UiLine("you", text))

        if (text.startsWith('/')) {
            return handleCoworkSlash(text)
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

        val currentEngine = coworkEngine.value
        val currentModel = runtimeModel
        val currentReasoning = runtimeReasoning
        viewModelScope.launch {
            val reply = try {
                if (target == null) {
                    currentEngine.send(body, null, currentModel, currentReasoning)
                } else {
                    currentEngine.askAsync("gui", target, body)
                }
            } catch (ex: Exception) {
                "error: ${ex.message}"
            }
            coworkMessages.add(UiLine("assistant", reply))
        }

        return ReplAction.Continue
    }

    private fun handleChatSlash(input: String): ReplAction {
        val tokens = CommandParser.parseTokens(input.substring(1))
        if (tokens.isEmpty()) return ReplAction.Continue

        return when (tokens[0].lowercase()) {
            "help" -> {
                chatMessages.add(UiLine("system", "chat commands: /help /clear /mode /model /reasoning /attach /config /workspace /exit"))
                ReplAction.Continue
            }

            "clear" -> {
                chatSession.value.clear()
                chatMessages.add(UiLine("system", "chat session cleared"))
                ReplAction.Continue
            }

            "mode" -> {
                if (tokens.size < 2) {
                    chatMessages.add(UiLine("system", "usage: /mode <chat|code|cowork>"))
                    ReplAction.Continue
                } else {
                    val parsed = parseMode(tokens[1])
                    if (parsed == null) {
                        chatMessages.add(UiLine("system", "usage: /mode <chat|code|cowork>"))
                        ReplAction.Continue
                    } else {
                        ReplAction.Switch(parsed)
                    }
                }
            }

            "model" -> {
                if (tokens.size < 2) {
                    chatMessages.add(UiLine("system", "model: $runtimeModel"))
                } else {
                    runtimeModel = tokens[1]
                    chatMessages.add(UiLine("system", "model -> $runtimeModel"))
                }
                ReplAction.Continue
            }

            "reasoning" -> {
                if (tokens.size < 2) {
                    chatMessages.add(UiLine("system", "reasoning: ${runtimeReasoning ?: "(off)"}"))
                } else {
                    val normalized = CommandParser.parseReasoning(tokens[1])
                    if (!normalized.first) {
                        chatMessages.add(UiLine("system", "usage: /reasoning minimal|low|medium|high|off"))
                    } else {
                        runtimeReasoning = normalized.second
                        chatMessages.add(UiLine("system", "reasoning -> ${runtimeReasoning ?: "(off)"}"))
                    }
                }
                ReplAction.Continue
            }

            "attach" -> {
                if (tokens.size == 1) {
                    chatMessages.add(UiLine("system", "attachments queued: $attachmentsCount"))
                } else {
                    val arg = tokens.drop(1).joinToString(" ")
                    if (arg.equals("clear", ignoreCase = true)) {
                        attachmentQueue.clear()
                        chatMessages.add(UiLine("system", "attachments cleared"))
                    } else {
                        when (val loaded = AttachmentLoader.load(arg)) {
                            is AttachmentLoadResult -> {
                                if (loaded.isSuccess) {
                                    loaded.attachment?.let { attachmentQueue.add(it) }
                                    chatMessages.add(UiLine("system", "attached ${loaded.attachment?.name ?: "file"}"))
                                } else {
                                    chatMessages.add(UiLine("system", loaded.failure ?: "attach failed", isError = true))
                                }
                            }
                        }
                    }
                }
                ReplAction.Continue
            }

            "config" -> {
                chatMessages.add(UiLine("system", "endpoint : ${config.baseUrl}"))
                chatMessages.add(UiLine("system", "model    : $runtimeModel"))
                chatMessages.add(UiLine("system", "reasoning: ${runtimeReasoning ?: "(off)"}"))
                chatMessages.add(UiLine("system", "workspace: $runtimeWorkspace"))
                chatMessages.add(UiLine("system", "usage    : ${if (config.includeUsage) "on" else "off"}"))
                chatMessages.add(UiLine("system", "apikey   : ${if (config.apiKey.isBlank()) "(unset)" else "(set, hidden)"}"))
                chatMessages.add(UiLine("system", "config   : ${ConfigStore.configPath}"))
                ReplAction.Continue
            }

            "workspace" -> {
                chatMessages.add(UiLine("system", "workspace: $runtimeWorkspace"))
                ReplAction.Continue
            }

            "exit", "quit" -> ReplAction.Exit

            else -> {
                chatMessages.add(UiLine("system", "unknown command: /${tokens[0]}", isError = true))
                ReplAction.Continue
            }
        }
    }

    private fun handleCodeSlash(input: String): ReplAction {
        val tokens = CommandParser.parseTokens(input.substring(1))
        if (tokens.isEmpty()) return ReplAction.Continue

        return when (tokens[0].lowercase()) {
            "help" -> {
                codeMessages.add(UiLine("system", "code commands: /help /mode <chat|code|cowork> /workspace [path] /clear /exit"))
                ReplAction.Continue
            }

            "mode" -> {
                if (tokens.size < 2) {
                    codeMessages.add(UiLine("system", "usage: /mode <chat|code|cowork>"))
                    return ReplAction.Continue
                }
                val parsed = parseMode(tokens[1])
                if (parsed == null) {
                    codeMessages.add(UiLine("system", "usage: /mode <chat|code|cowork>"))
                    ReplAction.Continue
                } else {
                    ReplAction.Switch(parsed)
                }
            }

            "workspace" -> {
                codeMessages.add(UiLine("system", if (tokens.size == 1) {
                    "workspace: $runtimeWorkspace"
                } else {
                    applyWorkspace(tokens.drop(1).joinToString(" "))
                }))
                ReplAction.Continue
            }

            "clear" -> {
                codeSession.value.clear()
                codeMessages.add(UiLine("system", "code session cleared"))
                ReplAction.Continue
            }

            "exit", "quit" -> ReplAction.Exit

            else -> {
                codeMessages.add(UiLine("system", "unknown command: /${tokens[0]}", isError = true))
                ReplAction.Continue
            }
        }
    }

    private fun handleCoworkSlash(input: String): ReplAction {
        val tokens = CommandParser.parseTokens(input.substring(1))
        if (tokens.isEmpty()) return ReplAction.Continue

        return when (tokens[0].lowercase()) {
            "help" -> {
                coworkMessages.add(UiLine("system", "cowork commands: /help /agents /agent add <name> [path] /mode <chat|code|cowork> /clear /exit"))
                ReplAction.Continue
            }

            "agents" -> {
                val names = coworkEngine.value.agentsNames
                coworkMessages.add(UiLine("system", if (names.isEmpty()) "agents: (none)" else "agents: ${names.joinToString()}"))
                ReplAction.Continue
            }

            "agent" -> {
                if (tokens.size < 3 || !tokens[1].equals("add", ignoreCase = true)) {
                    coworkMessages.add(UiLine("system", "usage: /agent add <name> [path]"))
                    return ReplAction.Continue
                }
                val name = tokens[2]
                val workspace = if (tokens.size > 3) tokens.drop(3).joinToString(" ") else runtimeWorkspace
                val result = runCatching {
                    coworkEngine.value.attach(name, workspace)
                }.getOrElse { "error: ${it.message}" }
                coworkMessages.add(UiLine("system", result))
                ReplAction.Continue
            }

            "mode" -> {
                if (tokens.size < 2) {
                    coworkMessages.add(UiLine("system", "usage: /mode <chat|code|cowork>"))
                    return ReplAction.Continue
                }
                val parsed = parseMode(tokens[1])
                if (parsed == null) {
                    coworkMessages.add(UiLine("system", "usage: /mode <chat|code|cowork>"))
                    ReplAction.Continue
                } else {
                    ReplAction.Switch(parsed)
                }
            }

            "clear" -> {
                coworkEngine.value.clear()
                coworkMessages.add(UiLine("system", "cowork sessions cleared"))
                ReplAction.Continue
            }

            "exit", "quit" -> ReplAction.Exit

            else -> {
                coworkMessages.add(UiLine("system", "unknown command: /${tokens[0]}", isError = true))
                ReplAction.Continue
            }
        }
    }

    fun applyConfig(edit: EditConfig): Boolean {
        val parsedReasoning = CommandParser.parseReasoning(edit.reasoning)
        if (!parsedReasoning.first && edit.reasoning.isNotBlank()) return false

        val parsedMode = runCatching {
            IntatisMode.valueOf(edit.defaultMode.replaceFirstChar { it.uppercaseChar() })
        }.getOrElse { config.defaultMode }

        val next = IntatisConfig(
            baseUrl = edit.baseUrl,
            apiKey = edit.apiKey,
            model = edit.model,
            reasoning = parsedReasoning.second,
            defaultMode = parsedMode,
            workspace = edit.workspace.ifBlank { null },
            includeUsage = edit.includeUsage,
        )

        val resolvedWorkspace = runCatching {
            resolveWorkspace(next.workspace)
        }.getOrElse { return false }

        ConfigStore.save(next)
        config = next
        runtimeModel = next.model
        runtimeReasoning = next.reasoning
        runtimeWorkspace = resolvedWorkspace
        runtimeMode = next.defaultMode.toUiMode()

        chatSession.value = ConversationSession(next)
        codeSession.value = createCodeSession(next, resolvedWorkspace)
        coworkEngine.value = createCoworkEngine(next, resolvedWorkspace)

        return true
    }

    private fun applyWorkspace(rawPath: String): String {
        return runCatching {
            val resolved = resolveWorkspace(rawPath)
            runtimeWorkspace = resolved
            codeSession.value = createCodeSession(config, resolved)
            coworkEngine.value = createCoworkEngine(config, resolved)
            "workspace set to $resolved"
        }.getOrElse { "error: ${it.message}" }
    }

    private fun prepareQueuedMessage(userText: String): Pair<String, List<ImageAttachment>> {
        if (attachmentQueue.isEmpty()) return userText to emptyList()

        val sb = StringBuilder(userText)
        val images = mutableListOf<ImageAttachment>()

        attachmentQueue.forEach { attachment ->
            when (attachment) {
                is TextAttachment -> {
                    sb.appendLine()
                    sb.appendLine()
                    sb.appendLine("[attached file: ${attachment.name}]")
                    sb.appendLine(attachment.content)
                }

                is ImageAttachment -> images.add(attachment)
            }
        }

        attachmentQueue.clear()
        return sb.toString() to images
    }

    fun resolvePermission(allow: Boolean) {
        viewModelScope.launch {
            permissionMutex.withLock {
                val pending = pendingPermission ?: return@withLock
                if (!pending.decision.isCompleted) {
                    pending.decision.complete(if (allow) PermissionDecision.Allow else PermissionDecision.Deny)
                }
                pendingPermission = null
            }
        }
    }

    private fun createCodeSession(currentConfig: IntatisConfig, workspace: String): CodeAgentSession {
        val shell = ProcessShellRunner()
        val git = ProcessGitService(shell)
        val reviewer = ModelPermissionReviewer(currentConfig, runtimeModel)

        return CodeAgentSession(
            config = currentConfig,
            workspaceRoot = workspace,
            agentName = "gui-code",
            permissionProfile = PermissionProfile.Reviewed,
            shell = shell,
            git = git,
            responder = permissionResponder,
            permissionReviewer = reviewer,
            eventSink = eventSink,
            allowsShell = true,
            maxIterations = 8,
        )
    }

    private fun createCoworkEngine(currentConfig: IntatisConfig, workspace: String): CoworkEngine {
        val shell = ProcessShellRunner()
        val git = ProcessGitService(shell)
        val reviewer = ModelPermissionReviewer(currentConfig, runtimeModel)

        return CoworkEngine(
            config = currentConfig,
            baseWorkspace = workspace,
            shell = shell,
            git = git,
            responder = permissionResponder,
            profile = PermissionProfile.Reviewed,
            eventSink = eventSink,
            allowsShell = true,
            maxIterations = 8,
            permissionReviewer = reviewer,
        )
    }

    private fun resolveWorkspace(workspace: String?): String {
        val requested = workspace?.ifBlank { null } ?: config.workspace?.ifBlank { null } ?: System.getProperty("user.dir") ?: "."
        return WorkspaceTools.resolveWorkspace(null, requested)
    }
}

@Composable
private fun ChatPanel(
    messages: List<UiLine>,
    attachmentsQueued: Int,
    workspace: String,
    onSend: (String) -> Unit,
) {
    var input by rememberSaveable { mutableStateOf("") }

    Column(modifier = Modifier.fillMaxSize()) {
        if (attachmentsQueued > 0) {
            Text("attachments queued: $attachmentsQueued", style = MaterialTheme.typography.bodySmall)
        }
        Text("workspace: $workspace", style = MaterialTheme.typography.bodySmall)
        MessageFeed(title = "Chat", messages = messages)
        MessageComposer(
            text = input,
            onTextChange = { input = it },
            onSend = {
                onSend(input)
                input = ""
            },
        )
    }
}

@Composable
private fun CodePanel(
    messages: List<UiLine>,
    workspace: String,
    onSend: (String) -> Unit,
) {
    var input by rememberSaveable { mutableStateOf("") }

    Column(modifier = Modifier.fillMaxSize()) {
        Text("workspace: $workspace", style = MaterialTheme.typography.bodySmall)
        MessageFeed(title = "Code", messages = messages)
        MessageComposer(
            text = input,
            onTextChange = { input = it },
            onSend = {
                onSend(input)
                input = ""
            },
        )
    }
}

@Composable
private fun CoworkPanel(
    messages: List<UiLine>,
    workspace: String,
    agents: List<String>,
    onSend: (String) -> Unit,
) {
    var input by rememberSaveable { mutableStateOf("") }

    Column(modifier = Modifier.fillMaxSize()) {
        Text("workspace: $workspace", style = MaterialTheme.typography.bodySmall)
        if (agents.isNotEmpty()) {
            Text("agents: ${agents.joinToString()}", style = MaterialTheme.typography.bodySmall)
        } else {
            Text("agents: (none)", style = MaterialTheme.typography.bodySmall)
        }

        MessageFeed(title = "Cowork", messages = messages)
        MessageComposer(
            text = input,
            onTextChange = { input = it },
            onSend = {
                onSend(input)
                input = ""
            },
        )
    }
}

@Composable
private fun MessageFeed(
    title: String,
    messages: List<UiLine>,
) {
    Box(modifier = Modifier.weight(1f).fillMaxWidth()) {
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(6.dp),
        ) {
            item {
                Text(title, fontWeight = FontWeight.Bold)
                HorizontalDivider()
            }

            items(messages) { line ->
                val color = when {
                    line.isError -> Color(0xFFC62828)
                    line.sender == "you" -> Color(0xFF1565C0)
                    line.sender == "assistant" -> Color(0xFF2E7D32)
                    else -> Color.DarkGray
                }

                Text(
                    text = "[${line.sender}] ${line.text}",
                    color = color,
                    fontSize = 12.sp,
                    modifier = Modifier.padding(vertical = 2.dp),
                )
            }

            item {
                Spacer(modifier = Modifier.height(24.dp))
            }
        }
    }
}

@Composable
private fun MessageComposer(
    text: String,
    onTextChange: (String) -> Unit,
    onSend: () -> Unit,
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        TextField(
            modifier = Modifier.weight(1f),
            value = text,
            onValueChange = onTextChange,
            placeholder = { Text("Type message or /command") },
            keyboardOptions = KeyboardOptions(imeAction = ImeAction.Send),
            keyboardActions = KeyboardActions(onSend = { onSend() }),
        )
        Button(onClick = onSend) {
            Text("Send")
        }
    }
}

@Composable
private fun SettingsDialog(
    snapshot: IntatisConfig,
    onSave: (EditConfig) -> Unit,
    onDismiss: () -> Unit,
) {
    var baseUrl by rememberSaveable { mutableStateOf(snapshot.baseUrl) }
    var apiKey by rememberSaveable { mutableStateOf(snapshot.apiKey) }
    var model by rememberSaveable { mutableStateOf(snapshot.model) }
    var reasoning by rememberSaveable { mutableStateOf(snapshot.reasoning ?: "") }
    var workspace by rememberSaveable { mutableStateOf(snapshot.workspace ?: "") }
    var defaultMode by rememberSaveable { mutableStateOf(snapshot.defaultMode.name.lowercase()) }
    var includeUsage by rememberSaveable { mutableStateOf(snapshot.includeUsage) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Settings") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(value = baseUrl, onValueChange = { baseUrl = it }, label = { Text("Base URL") })
                OutlinedTextField(value = apiKey, onValueChange = { apiKey = it }, label = { Text("API Key") })
                OutlinedTextField(value = model, onValueChange = { model = it }, label = { Text("Model") })
                OutlinedTextField(value = reasoning, onValueChange = { reasoning = it }, label = { Text("Reasoning (minimal|low|medium|high|off)") })
                OutlinedTextField(value = workspace, onValueChange = { workspace = it }, label = { Text("Default workspace") })
                OutlinedTextField(value = defaultMode, onValueChange = { defaultMode = it }, label = { Text("Default mode") })
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    Checkbox(checked = includeUsage, onCheckedChange = { includeUsage = it })
                    Text("Include usage")
                }
            }
        },
        confirmButton = {
            Button(onClick = {
                onSave(EditConfig(baseUrl, apiKey, model, reasoning, workspace, defaultMode, includeUsage))
            }) {
                Text("Save")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel")
            }
        },
    )
}
