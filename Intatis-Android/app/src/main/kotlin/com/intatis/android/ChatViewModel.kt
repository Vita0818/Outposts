package com.intatis.android

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.intatis.shared.ChatLoop
import com.intatis.shared.SessionId
import com.intatis.shared.SessionKind
import com.intatis.shared.providers.ConfigSecretResolver
import com.intatis.shared.providers.ConfigStore
import com.intatis.shared.providers.ImportedConfig
import com.intatis.shared.providers.ProviderRegistry
import com.intatis.shared.providers.ReasoningEffort
import com.intatis.shared.protocol.EventType
import com.intatis.shared.protocol.Jsonx.int
import com.intatis.shared.protocol.Jsonx.str
import com.intatis.shared.session.ConversationProjection
import com.intatis.shared.session.EventLog
import com.intatis.shared.session.EventLogException
import com.intatis.shared.session.SessionHistoryStore
import com.intatis.shared.session.SessionProjectionStore
import com.intatis.shared.session.SessionSummary
import kotlinx.coroutines.Job
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.io.File

class ChatViewModel(application: Application) : AndroidViewModel(application) {

    private val home = File(application.filesDir, "intatis")
    private val sessionsRoot = File(home, "sessions")
    private val authFile = File(home.parentFile, "auth.json")

    private var config: ImportedConfig = ImportedConfig()
    private var registry: ProviderRegistry = ProviderRegistry(config,
        ConfigSecretResolver(authFile, File("")))

    private var log: EventLog? = null
    private var sendJob: Job? = null

    data class UiState(
        val title: String = "New Chat",
        val messages: List<com.intatis.shared.session.ChatMessageView> = emptyList(),
        val isStreaming: Boolean = false,
        val error: String = "",
        val usageText: String = "",
        val modelOptions: List<String> = emptyList(),
        val selectedModel: String = "",
        val configSource: String = "",
        val needsApiKey: Boolean = false,
        val providers: List<ProviderLine> = emptyList(),
        val roles: List<Pair<String, String>> = emptyList(),
    ) {
        data class ProviderLine(
            val id: String,
            val displayName: String,
            val baseUrl: String,
            val keyDescription: String,
            val models: List<String>,
        )
    }

    private val _state = MutableStateFlow(UiState())
    val state: StateFlow<UiState> = _state.asStateFlow()

    init {
        reloadConfig()
        startNewSession()
    }

    fun sessionsRootFile(): File = sessionsRoot

    fun reloadConfig() {
        val (loaded, file) = ConfigStore.load(home)
        config = loaded
        registry = ProviderRegistry(config, ConfigSecretResolver(authFile,
            file.takeIf { it.path.isNotEmpty() } ?: File("")))

        val options = config.inferenceModels().map { it.id }
        val selected = loaded.chat?.modelId
            ?: options.firstOrNull()
            ?: com.intatis.shared.providers.AppConfig.DEFAULT_MODEL

        fun role(label: String, ref: com.intatis.shared.providers.ModelRef?) =
            label to (ref?.displayLabel ?: "-")

        _state.update {
            it.copy(
                modelOptions = options,
                selectedModel = selected,
                configSource = file.path,
                needsApiKey = !registry.hasCredential(loaded.chat?.providerId ?: ""),
                providers = loaded.providers.map { p ->
                    UiState.ProviderLine(
                        id = p.id,
                        displayName = p.displayName,
                        baseUrl = p.baseUrl,
                        keyDescription = p.apiKeyRef.describe(),
                        models = p.models.map { m -> m.id },
                    )
                },
                roles = listOf(
                    role("model", loaded.chat),
                    role("reviewer", loaded.reviewer),
                    role("image", loaded.image),
                    role("transcription", loaded.transcription),
                    role("embedding", loaded.embedding),
                    role("reranker", loaded.reranker),
                ),
            )
        }
    }

    fun recentSessions(): List<SessionSummary> =
        SessionHistoryStore.recentSessions(sessionsRoot, SessionKind.CHAT)

    fun startNewSession() {
        closeLog()
        val session = SessionId.new(SessionKind.CHAT)
        val opened = EventLog.open(session.value, SessionHistoryStore.sessionFile(sessionsRoot, session.value))
        wireLog(opened)
        SessionProjectionStore.updateDisplayName(opened, SessionKind.CHAT, "New Chat", changeKind = "created")
        _state.update { it.copy(title = "New Chat", messages = emptyList(), error = "", usageText = "") }
    }

    fun openSession(summary: SessionSummary): Boolean {
        closeLog()
        val session = SessionId(summary.id)
        val opened = try {
            EventLog.open(session.value, SessionHistoryStore.sessionFile(sessionsRoot, session.value))
        } catch (_: EventLogException) {
            _state.update { it.copy(error = "session is owned by another runtime") }
            return false
        }
        wireLog(opened)
        refresh()
        _state.update { it.copy(title = summary.displayName ?: summary.id, error = "") }
        return true
    }

    fun deleteSession(summary: SessionSummary) {
        try {
            if (log?.sessionId == summary.id) closeLog()
            SessionHistoryStore.deleteSession(sessionsRoot, summary.id)
            if (log == null) startNewSession()
        } catch (_: EventLogException) {
            _state.update { it.copy(error = "cannot delete a running session") }
        }
    }

    fun selectModel(model: String) {
        _state.update { it.copy(selectedModel = model) }
    }

    fun stop() {
        sendJob?.cancel()
    }

    fun send(text: String) {
        val trimmed = text.trim()
        if (trimmed.isEmpty() || _state.value.isStreaming) return
        val activeLog = log ?: return

        val chat = config.chat
        val providerId = chat?.providerId ?: return run {
            _state.update { it.copy(error = "no provider configured — open Settings") }
        }

        _state.update { it.copy(isStreaming = true, error = "") }
        sendJob = viewModelScope.launch(Dispatchers.IO) {
            try {
                val provider = registry.chatProviderFor(providerId)
                val loop = ChatLoop(
                    log = activeLog,
                    provider = provider,
                    model = _state.value.selectedModel,
                    systemPrompt = "You are Intatis, a concise local AI assistant.",
                    reasoningEffort = ReasoningEffort.fromWire(null),
                    includeUsage = true,
                )
                loop.send(trimmed)
            } catch (e: kotlinx.coroutines.CancellationException) {
                throw e
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    _state.update { it.copy(error = e.message ?: "provider failure") }
                }
            } finally {
                withContext(Dispatchers.Main) {
                    _state.update { it.copy(isStreaming = false) }
                }
            }
        }
    }

    private fun wireLog(opened: EventLog) {
        log = opened
        opened.onEnvelopeAppended = { refresh() }
    }

    private fun closeLog() {
        log?.let { current ->
            current.onEnvelopeAppended = null
            current.close()
        }
        log = null
    }

    private fun refresh() {
        val activeLog = log ?: return
        val replay = activeLog.replay()
        val messages = ConversationProjection.build(replay)
        val lastError = replay.lastOrNull { it.type == EventType.ERROR }
            ?.payload?.str("message") ?: ""
        val lastStats = replay.lastOrNull { it.type == EventType.TURN_STATS }
            ?.payload?.int("total_tokens")
        _state.update { current ->
            current.copy(
                messages = messages,
                error = lastError,
                usageText = lastStats?.takeIf { it > 0 }?.let { "$it tokens" } ?: current.usageText,
            )
        }
    }

    override fun onCleared() {
        closeLog()
        super.onCleared()
    }
}
