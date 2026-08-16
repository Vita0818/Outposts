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
import com.intatis.shared.provider.ProviderHealthCheckResult
import com.intatis.shared.provider.ProviderRegistry
import com.intatis.shared.SessionEventLog
import com.intatis.shared.TextAttachment
import com.intatis.shared.WorkspaceTools
import com.intatis.shared.conversation.CodeProjection
import com.intatis.shared.conversation.CoworkMentionRouting
import com.intatis.shared.conversation.CoworkProjection
import com.intatis.shared.conversation.ConversationProjection
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
    val chatProviderId: String,
    val agentToolProviderId: String,
    val imageProviderId: String,
    val transcriptionProviderId: String,
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
                var settingsHealthReport by rememberSaveable { mutableStateOf("") }

                val mode = viewModel.runtimeMode
                val chatMessages = viewModel.chatMessages
                val codeMessages = viewModel.codeMessages
                val coworkMessages = viewModel.coworkMessages
                val chatAttachmentCount = viewModel.chatAttachmentsCount
                val codeAttachmentCount = viewModel.codeAttachmentsCount
                val coworkAttachmentCount = viewModel.coworkAttachmentsCount
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
                                    attachmentsQueued = chatAttachmentCount,
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
                                    attachmentsQueued = codeAttachmentCount,
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
                                    attachmentsQueued = coworkAttachmentCount,
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
                            healthReport = settingsHealthReport,
                            onHealthCheck = { edit ->
                                scope.launch {
                                    settingsHealthReport = viewModel.checkHealth(edit)
                                }
                            },
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

    private val chatAttachmentQueue = mutableListOf<ChatAttachment>()
    private val codeAttachmentQueue = mutableListOf<ChatAttachment>()
    private val coworkAttachmentQueue = mutableListOf<ChatAttachment>()

    val chatAttachmentsCount: Int
        get() = chatAttachmentQueue.size

    val codeAttachmentsCount: Int
        get() = codeAttachmentQueue.size

    val coworkAttachmentsCount: Int
        get() = coworkAttachmentQueue.size

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

    private var chatEventLog = mutableStateOf(SessionEventLog("gui-chat"))
    private var codeEventLog = mutableStateOf(SessionEventLog("gui-code"))
    private var coworkEventLog = mutableStateOf(SessionEventLog("gui-cowork"))
    private var chatSession = mutableStateOf(ConversationSession(config, chatEventLog.value))
    private var codeSession = mutableStateOf(createCodeSession(config, runtimeWorkspace))
    private var coworkEngine = mutableStateOf(createCoworkEngine(config, runtimeWorkspace))

    private val chatProjection = ConversationProjection()
    private val codeProjection = CodeProjection()
    private val coworkProjection = CoworkProjection()
    private var chatProjectionLines = 0
    private var codeProjectionLines = 0
    private var coworkProjectionLines = 0

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

        if (text.startsWith('/')) {
            return handleChatSlash(text)
        }

        val currentSession = chatSession.value
        val currentModel = runtimeModel
        val currentReasoning = runtimeReasoning
        val usageEnabled = config.includeUsage

        val (effectiveText, imageAttachments) = prepareQueuedMessage(text, chatAttachmentQueue)
        viewModelScope.launch {
            try {
                val (_, _, usage) = currentSession.sendUserMessageAsync(
                    userText = effectiveText,
                    model = currentModel,
                    reasoning = currentReasoning,
                    attachments = imageAttachments,
                    includeUsage = usageEnabled,
                )
                appendChatProjectionLines()
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

        if (text.startsWith('/')) {
            return handleCodeSlash(text)
        }

        val currentSession = codeSession.value
        val currentModel = runtimeModel
        val currentReasoning = runtimeReasoning
        val usageEnabled = config.includeUsage
        val (effectiveText, imageAttachments) = prepareQueuedMessage(text, codeAttachmentQueue)
        viewModelScope.launch {
            try {
                val (_, _, usage) = currentSession.sendAsync(
                    userText = effectiveText,
                    model = currentModel,
                    reasoning = currentReasoning,
                    userGoal = "code mode",
                    attachments = imageAttachments,
                    includeUsage = usageEnabled,
                    to = "code",
                    tags = listOf("code"),
                )
                appendCodeProjectionLines()
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

        if (text.startsWith('/')) {
            return handleCoworkSlash(text)
        }

        val route = CoworkMentionRouting.parse(text)
        val target = route.to
        val body = route.text

        val currentEngine = coworkEngine.value
        val currentModel = runtimeModel
        val currentReasoning = runtimeReasoning
        val usageEnabled = config.includeUsage
        val (effectiveBody, imageAttachments) = prepareQueuedMessage(body, coworkAttachmentQueue)
        viewModelScope.launch {
            try {
                if (target == null) {
                    currentEngine.sendAsync(
                        text = effectiveBody,
                        target = null,
                        model = currentModel,
                        reasoning = currentReasoning,
                        images = imageAttachments,
                        includeUsage = usageEnabled,
                        to = target,
                        userGoal = route.goal ?: "cowork",
                        tags = target?.let { listOf(it) },
                    )
                } else {
                    currentEngine.askAsync(
                        from = "gui",
                        to = target,
                        question = effectiveBody,
                        userGoal = route.goal,
                        images = imageAttachments,
                        includeUsage = usageEnabled,
                    )
                }
                appendCoworkProjectionLines()
            } catch (ex: Exception) {
                coworkMessages.add(UiLine("system", "error: ${ex.message}", isError = true))
            }
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
                chatMessages.clear()
                chatProjectionLines = 0
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
                    chatMessages.add(UiLine("system", "attachments queued: ${chatAttachmentQueue.size}"))
                } else {
                    val arg = tokens.drop(1).joinToString(" ")
                    if (arg.equals("clear", ignoreCase = true)) {
                        chatAttachmentQueue.clear()
                        chatMessages.add(UiLine("system", "attachments cleared"))
                    } else if (arg.equals("list", ignoreCase = true)) {
                        chatMessages.add(UiLine("system", if (chatAttachmentQueue.isEmpty()) "no attachments queued" else describeAttachmentQueue(chatAttachmentQueue)))
                    } else {
                        when (val loaded = AttachmentLoader.load(arg)) {
                            is AttachmentLoadResult -> {
                                if (loaded.isSuccess) {
                                    loaded.attachment?.let { chatAttachmentQueue.add(it) }
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
                chatMessages.add(UiLine("system", "chat provider       : ${config.chatProviderId}"))
                chatMessages.add(UiLine("system", "agent tool provider : ${config.agentToolProviderId}"))
                chatMessages.add(UiLine("system", "image provider      : ${config.imageProviderId}"))
                chatMessages.add(UiLine("system", "transcription provider : ${config.transcriptionProviderId}"))
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
                codeMessages.add(UiLine("system", "code commands: /help /attach /mode <chat|code|cowork> /workspace [path] /clear /exit"))
                ReplAction.Continue
            }

            "attach" -> {
                if (tokens.size == 1) {
                    codeMessages.add(UiLine("system", "attachments queued: ${if (codeAttachmentQueue.isEmpty()) 0 else codeAttachmentQueue.size}"))
                } else {
                    val arg = tokens.drop(1).joinToString(" ")
                    if (arg.equals("clear", ignoreCase = true)) {
                        codeAttachmentQueue.clear()
                        codeMessages.add(UiLine("system", "code attachments cleared"))
                    } else if (arg.equals("list", ignoreCase = true)) {
                        codeMessages.add(UiLine("system", if (codeAttachmentQueue.isEmpty()) "no attachments queued" else describeAttachmentQueue(codeAttachmentQueue)))
                    } else {
                        when (val loaded = AttachmentLoader.load(arg)) {
                            is AttachmentLoadResult -> {
                                if (loaded.isSuccess) {
                                    loaded.attachment?.let { codeAttachmentQueue.add(it) }
                                    codeMessages.add(UiLine("system", "attached ${loaded.attachment?.name ?: "file"}"))
                                } else {
                                    codeMessages.add(UiLine("system", loaded.failure ?: "attach failed", isError = true))
                                }
                            }
                        }
                    }
                }
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
                codeMessages.clear()
                codeProjectionLines = 0
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
                coworkMessages.add(UiLine("system", "cowork commands: /help /attach /agents /agent add <name> [path] /mode <chat|code|cowork> /clear /exit"))
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

            "attach" -> {
                if (tokens.size == 1) {
                    coworkMessages.add(UiLine("system", "attachments queued: ${if (coworkAttachmentQueue.isEmpty()) 0 else coworkAttachmentQueue.size}"))
                } else {
                    val arg = tokens.drop(1).joinToString(" ")
                    if (arg.equals("clear", ignoreCase = true)) {
                        coworkAttachmentQueue.clear()
                        coworkMessages.add(UiLine("system", "cowork attachments cleared"))
                    } else if (arg.equals("list", ignoreCase = true)) {
                        coworkMessages.add(UiLine("system", if (coworkAttachmentQueue.isEmpty()) "no attachments queued" else describeAttachmentQueue(coworkAttachmentQueue)))
                    } else {
                        when (val loaded = AttachmentLoader.load(arg)) {
                            is AttachmentLoadResult -> {
                                if (loaded.isSuccess) {
                                    loaded.attachment?.let { coworkAttachmentQueue.add(it) }
                                    coworkMessages.add(UiLine("system", "attached ${loaded.attachment?.name ?: "file"}"))
                                } else {
                                    coworkMessages.add(UiLine("system", loaded.failure ?: "attach failed", isError = true))
                                }
                            }
                        }
                    }
                }
                ReplAction.Continue
            }

            "clear" -> {
                coworkEngine.value.clear()
                coworkMessages.clear()
                coworkProjectionLines = 0
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
        val next = resolveConfigFromEdit(edit) ?: return false

        val resolvedWorkspace = runCatching {
            resolveWorkspace(next.workspace)
        }.getOrElse { return false }

        ConfigStore.save(next)
        config = next
        runtimeModel = next.model
        runtimeReasoning = next.reasoning
        runtimeWorkspace = resolvedWorkspace
        runtimeMode = next.defaultMode.toUiMode()

        chatSession.value = ConversationSession(next, chatEventLog.value)
        codeSession.value = createCodeSession(next, resolvedWorkspace)
        coworkEngine.value = createCoworkEngine(next, resolvedWorkspace)

        chatProjectionLines = 0
        codeProjectionLines = 0
        coworkProjectionLines = 0
        chatMessages.clear()
        codeMessages.clear()
        coworkMessages.clear()
        chatMessages.add(UiLine("system", "Intatis config updated"))
        codeMessages.add(UiLine("system", "Intatis config updated"))
        coworkMessages.add(UiLine("system", "Intatis config updated"))

        return true
    }

    suspend fun checkHealth(edit: EditConfig): String {
        val editedConfig = resolveConfigFromEdit(edit) ?: return "Health check cancelled: invalid config input."

        return try {
            val registry = ProviderRegistry(editedConfig)
            val suite = registry.checkHealth(editedConfig.chatProviderId, editedConfig.agentToolProviderId)
            listOf(
                formatHealthCheckResult(suite.chat),
                formatHealthCheckResult(suite.agentTool),
            ).joinToString("\n")
        } catch (ex: Exception) {
            "Health check failed: ${ex.message}"
        }
    }

    private fun resolveConfigFromEdit(edit: EditConfig): IntatisConfig? {
        val parsedReasoning = CommandParser.parseReasoning(edit.reasoning)
        if (!parsedReasoning.first && edit.reasoning.isNotBlank()) return null

        val parsedMode = runCatching {
            IntatisMode.valueOf(edit.defaultMode.replaceFirstChar { it.uppercaseChar() })
        }.getOrElse { return null }

        return IntatisConfig(
            baseUrl = edit.baseUrl,
            apiKey = edit.apiKey,
            model = edit.model,
            selectedModel = edit.model,
            reasoning = parsedReasoning.second,
            defaultMode = parsedMode,
            workspace = edit.workspace.ifBlank { null },
            chatProviderId = edit.chatProviderId.ifBlank { "openai" },
            agentToolProviderId = edit.agentToolProviderId.ifBlank { "openai" },
            imageProviderId = edit.imageProviderId.ifBlank { "openai" },
            transcriptionProviderId = edit.transcriptionProviderId.ifBlank { "openai" },
            includeUsage = edit.includeUsage,
        )
    }

    private fun formatHealthCheckResult(result: ProviderHealthCheckResult): String {
        val status = if (result.isHealthy) "PASS" else "FAIL"
        val header = "${result.providerId}/${result.role} (${result.model}) $status - latency ${result.latency.toMillis()}ms"
        val preview = result.responsePreview?.let { "\n  response: $it" } ?: ""
        return "$header\n  message: ${result.message}$preview"
    }

    private fun applyWorkspace(rawPath: String): String {
        return runCatching {
            val resolved = resolveWorkspace(rawPath)
            runtimeWorkspace = resolved
            codeSession.value = createCodeSession(config, resolved)
            coworkEngine.value = createCoworkEngine(config, resolved)
            codeProjectionLines = 0
            coworkProjectionLines = 0
            codeMessages.clear()
            coworkMessages.clear()
            codeMessages.add(UiLine("system", "workspace set to $resolved"))
            coworkMessages.add(UiLine("system", "workspace set to $resolved"))
            "workspace set to $resolved"
        }.getOrElse { "error: ${it.message}" }
    }

    private fun prepareQueuedMessage(
        userText: String,
        queue: MutableList<ChatAttachment>,
    ): Pair<String, List<ImageAttachment>> {
        if (queue.isEmpty()) return userText to emptyList()

        val sb = StringBuilder(userText)
        val images = mutableListOf<ImageAttachment>()

        queue.forEach { attachment ->
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

        queue.clear()
        return sb.toString() to images
    }

    private fun describeAttachmentQueue(queue: List<ChatAttachment>): String {
        val textCount = queue.count { it is TextAttachment }
        val imageCount = queue.count { it is ImageAttachment }
        val names = queue.joinToString(", ") { it.name }
        return "${queue.size} attachment(s) [text=$textCount, image=$imageCount] ($names)"
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
            eventSink = codeEventLog.value,
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
            eventSink = coworkEventLog.value,
            allowsShell = true,
            maxIterations = 8,
            permissionReviewer = reviewer,
        )
    }

    private fun appendChatProjectionLines() {
        val records = chatEventLog.value.readAll()
        val rendered = chatProjection.render(records)
        for (index in chatProjectionLines until rendered.size) {
            val line = rendered[index]
            chatMessages.add(UiLine(line.sender, line.text, line.isError))
        }
        chatProjectionLines = rendered.size
    }

    private fun appendCodeProjectionLines() {
        val records = codeEventLog.value.readAll()
        val rendered = codeProjection.render(records)
        for (index in codeProjectionLines until rendered.size) {
            val line = rendered[index]
            codeMessages.add(UiLine(line.sender, line.text, line.isError))
        }
        codeProjectionLines = rendered.size
    }

    private fun appendCoworkProjectionLines() {
        val records = coworkEventLog.value.readAll()
        val rendered = coworkProjection.render(records)
        for (index in coworkProjectionLines until rendered.size) {
            val line = rendered[index]
            coworkMessages.add(UiLine(line.sender, line.text, line.isError))
        }
        coworkProjectionLines = rendered.size
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
    attachmentsQueued: Int,
    onSend: (String) -> Unit,
) {
    var input by rememberSaveable { mutableStateOf("") }

    Column(modifier = Modifier.fillMaxSize()) {
        if (attachmentsQueued > 0) {
            Text("attachments queued: $attachmentsQueued", style = MaterialTheme.typography.bodySmall)
        }
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
    healthReport: String,
    onSave: (EditConfig) -> Unit,
    onHealthCheck: (EditConfig) -> Unit,
    onDismiss: () -> Unit,
) {
    var baseUrl by rememberSaveable { mutableStateOf(snapshot.baseUrl) }
    var apiKey by rememberSaveable { mutableStateOf(snapshot.apiKey) }
    var model by rememberSaveable { mutableStateOf(snapshot.model) }
    var reasoning by rememberSaveable { mutableStateOf(snapshot.reasoning ?: "") }
    var workspace by rememberSaveable { mutableStateOf(snapshot.workspace ?: "") }
    var chatProviderId by rememberSaveable { mutableStateOf(snapshot.chatProviderId) }
    var agentToolProviderId by rememberSaveable { mutableStateOf(snapshot.agentToolProviderId) }
    var imageProviderId by rememberSaveable { mutableStateOf(snapshot.imageProviderId) }
    var transcriptionProviderId by rememberSaveable { mutableStateOf(snapshot.transcriptionProviderId) }
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
                OutlinedTextField(value = chatProviderId, onValueChange = { chatProviderId = it }, label = { Text("Chat provider ID") })
                OutlinedTextField(value = agentToolProviderId, onValueChange = { agentToolProviderId = it }, label = { Text("Agent/tool provider ID") })
                OutlinedTextField(value = imageProviderId, onValueChange = { imageProviderId = it }, label = { Text("Image provider ID") })
                OutlinedTextField(value = transcriptionProviderId, onValueChange = { transcriptionProviderId = it }, label = { Text("Transcription provider ID") })
                OutlinedTextField(value = workspace, onValueChange = { workspace = it }, label = { Text("Default workspace") })
                OutlinedTextField(value = defaultMode, onValueChange = { defaultMode = it }, label = { Text("Default mode") })
                if (healthReport.isNotBlank()) {
                    Text("Health check:")
                    Text(healthReport)
                }
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    Checkbox(checked = includeUsage, onCheckedChange = { includeUsage = it })
                    Text("Include usage")
                }
                Button(onClick = {
                    onHealthCheck(
                        EditConfig(
                            baseUrl = baseUrl,
                            apiKey = apiKey,
                            model = model,
                            reasoning = reasoning,
                            chatProviderId = chatProviderId.ifBlank { "openai" },
                            agentToolProviderId = agentToolProviderId.ifBlank { "openai" },
                            imageProviderId = imageProviderId.ifBlank { "openai" },
                            transcriptionProviderId = transcriptionProviderId.ifBlank { "openai" },
                            workspace = workspace,
                            defaultMode = defaultMode,
                            includeUsage = includeUsage,
                        )
                    )
                }) {
                    Text("Run health check")
                }
            }
        },
        confirmButton = {
            Button(onClick = {
                onSave(
                    EditConfig(
                        baseUrl = baseUrl,
                        apiKey = apiKey,
                        model = model,
                        reasoning = reasoning,
                        chatProviderId = chatProviderId.ifBlank { "openai" },
                        agentToolProviderId = agentToolProviderId.ifBlank { "openai" },
                        imageProviderId = imageProviderId.ifBlank { "openai" },
                        transcriptionProviderId = transcriptionProviderId.ifBlank { "openai" },
                        workspace = workspace,
                        defaultMode = defaultMode,
                        includeUsage = includeUsage,
                    )
                )
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
