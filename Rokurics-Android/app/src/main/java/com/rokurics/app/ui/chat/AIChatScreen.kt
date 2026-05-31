package com.rokurics.app.ui.chat

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Chat
import androidx.compose.material.icons.automirrored.filled.Send
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.rokurics.app.data.ChatStore
import com.rokurics.app.data.PersistedConversation
import com.rokurics.app.data.PersistedMessage
import com.rokurics.app.data.StudyLibraryStore
import com.rokurics.app.data.UserPreferencesStore
import com.rokurics.app.ui.theme.adaptivePageGradientBrush
import com.rokurics.app.ui.theme.RokuricsColors
import com.rokurics.app.ui.theme.rokuricsScaleClickable
import com.rokurics.app.domain.model.StudyItemMetadata
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

import kotlinx.coroutines.withContext
import java.util.UUID

data class DisplayChatMessage(
    val id: String = UUID.randomUUID().toString(),
    val role: ChatMessageRole,
    val content: String,
    val timestamp: Long = System.currentTimeMillis()
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AIChatScreen(
    studyLibraryStore: StudyLibraryStore,
    userPreferencesStore: UserPreferencesStore,
    onBack: () -> Unit
) {
    val scope = rememberCoroutineScope()
    val settingsStore = remember { AISettingsStore() }
    val profile = remember { userPreferencesStore.load() }
    val chatStore = remember { ChatStore() }

    var messages by remember { mutableStateOf(listOf<DisplayChatMessage>()) }
    var activeContext by remember { mutableStateOf<ChatContext?>(null) }
    var draft by remember { mutableStateOf("") }
    var errorText by remember { mutableStateOf<String?>(null) }
    var isSending by remember { mutableStateOf(false) }
    var showContextPicker by remember { mutableStateOf(false) }
    var showSettings by remember { mutableStateOf(false) }
    var showConversations by remember { mutableStateOf(false) }
    var activeConversationId by remember { mutableStateOf(UUID.randomUUID().toString()) }
    var recentConversations by remember { mutableStateOf(listOf<SavedConversation>()) }
    val listState = rememberLazyListState()

    LaunchedEffect(Unit) {
        studyLibraryStore.refresh()
        val persisted = withContext(Dispatchers.IO) { chatStore.loadAll() }
        if (persisted.isNotEmpty()) {
            recentConversations = persisted.map { it.toUIModel() }
        }
    }

    SuspendingLaunchedEffect(messages.size) {
        if (messages.isNotEmpty()) {
            listState.animateScrollToItem(messages.size - 1)
        }
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(adaptivePageGradientBrush())
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
        ) {
            // ── Glass-style header (Apple parity: RokuricsMobilePageHeader) ──
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .statusBarsPadding()
                    .padding(horizontal = 20.dp, vertical = 12.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Glass circle back button
                Box(
                    modifier = Modifier
                        .size(44.dp)
                        .clip(CircleShape)
                        .background(if (isSystemInDarkTheme()) Color(0xFF0D2424).copy(alpha = 0.55f) else Color.White.copy(alpha = 0.46f))
                        .rokuricsScaleClickable(onClick = onBack),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        Icons.AutoMirrored.Filled.ArrowBack,
                        contentDescription = "返回",
                        tint = RokuricsColors.deepText,
                        modifier = Modifier.size(20.dp)
                    )
                }

                Spacer(modifier = Modifier.weight(1f))

                // Right action capsule (Apple parity: single glass capsule with history + new chat)
                Surface(
                    shape = RoundedCornerShape(22.dp),
                    color = Color.White.copy(alpha = 0.36f),
                    border = androidx.compose.foundation.BorderStroke(0.5.dp, Color.White.copy(alpha = 0.36f))
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Box(
                            modifier = Modifier
                                .size(40.dp)
                                .rokuricsScaleClickable(onClick = {
                                    saveSnapshot(messages, activeContext, activeConversationId, recentConversations, chatStore, scope) { recentConversations = it }
                                    showConversations = true
                                }),
                            contentAlignment = Alignment.Center
                        ) {
                            Icon(Icons.Default.History, contentDescription = "最近对话", tint = RokuricsColors.deepText, modifier = Modifier.size(18.dp))
                        }
                        VerticalDivider(
                            modifier = Modifier.height(22.dp),
                            thickness = 0.5.dp,
                            color = Color.White.copy(alpha = 0.18f)
                        )
                        Box(
                            modifier = Modifier
                                .size(40.dp)
                                .rokuricsScaleClickable(onClick = {
                                    startNewConversation(messages, activeContext, activeConversationId, recentConversations, chatStore, scope) { newId, clearedMessages, clearedContext, savedConversations ->
                                        activeConversationId = newId; messages = clearedMessages; activeContext = clearedContext; recentConversations = savedConversations; errorText = null
                                    }
                                }),
                            contentAlignment = Alignment.Center
                        ) {
                            Icon(Icons.Default.Edit, contentDescription = "新对话", tint = RokuricsColors.deepText, modifier = Modifier.size(18.dp))
                        }
                    }
                }
            }

            // Page title (Apple parity: serif "AI 对话")
            Text(
                text = "AI 对话",
                style = MaterialTheme.typography.titleLarge,
                color = MaterialTheme.colorScheme.onBackground,
                modifier = Modifier.padding(horizontal = 20.dp)
            )

            if (activeContext != null) {
                Text(
                    text = activeContext!!.pathDisplay,
                    fontSize = 12.sp,
                    color = RokuricsColors.softText,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    modifier = Modifier.padding(horizontal = 20.dp, vertical = 2.dp)
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            // Context attachment chip
            if (activeContext != null) {
                ContextChip(
                    context = activeContext!!,
                    onRemove = {
                        activeContext = null
                        if (messages.isEmpty()) {
                            recentConversations = recentConversations.filter { it.id != activeConversationId }
                        } else {
                            saveSnapshot(
                                messages, activeContext, activeConversationId, recentConversations,
                                chatStore, scope
                            ) { recentConversations = it }
                        }
                    }
                )
            }

            // Messages
            LazyColumn(
                modifier = Modifier
                    .weight(1f)
                    .fillMaxWidth(),
                state = listState,
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                if (messages.isEmpty()) {
                    item {
                        GreetingCard(displayName = profile.displayName)
                    }
                }
                items(messages, key = { it.id }) { msg ->
                    ChatBubble(message = msg)
                }
                if (isSending) {
                    item {
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            contentAlignment = Alignment.Center
                        ) {
                            CircularProgressIndicator(color = RokuricsColors.aqua)
                        }
                    }
                }
                if (errorText != null) {
                    item {
                        Text(
                            text = errorText!!,
                            color = RokuricsColors.coral,
                            fontSize = 13.sp,
                            modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp)
                        )
                    }
                }
            }

            // Input area
            ChatInputBar(
                draft = draft,
                onDraftChange = { draft = it },
                isSending = isSending,
                onAttach = { showContextPicker = true },
                onSettings = { showSettings = true },
                onSend = {
                    val text = draft.trim()
                    if (text.isNotEmpty() && !isSending) {
                        draft = ""
                        errorText = null
                        isSending = true
                        val userMsg = DisplayChatMessage(role = ChatMessageRole.USER, content = text)
                        messages = messages + userMsg
                        saveSnapshot(
                            messages, activeContext, activeConversationId, recentConversations,
                            chatStore, scope
                        ) { recentConversations = it }

                        val provider = buildProvider(settingsStore)

                        scope.launch {
                            try {
                                val chatMessages = messages.map {
                                    ChatMessage(role = it.role, content = it.content)
                                }
                                val response = provider.send(chatMessages, activeContext)
                                messages = messages + DisplayChatMessage(
                                    role = ChatMessageRole.ASSISTANT,
                                    content = response.content
                                )
                                saveSnapshot(
                                    messages, activeContext, activeConversationId, recentConversations,
                                    chatStore, scope
                                ) { recentConversations = it }
                            } catch (e: AIError) {
                                errorText = e.message
                            } catch (e: Exception) {
                                errorText = "发送失败: ${e.message}"
                            }
                            isSending = false
                        }
                    }
                }
            )
        }
    }

    // Context picker dialog
    if (showContextPicker) {
        ContextPickerDialog(
            studyLibraryStore = studyLibraryStore,
            onDismiss = { showContextPicker = false },
            onSelect = { context ->
                saveSnapshot(
                    messages, activeContext, activeConversationId, recentConversations,
                    chatStore, scope
                ) { recentConversations = it }
                activeContext = context
                showContextPicker = false
            }
        )
    }

    // Settings dialog
    if (showSettings) {
        AISettingsDialog(
            settingsStore = settingsStore,
            onDismiss = { showSettings = false }
        )
    }

    // Recent conversations dialog
    if (showConversations) {
        RecentConversationsDialog(
            conversations = recentConversations,
            activeConversationId = activeConversationId,
            onSelect = { conv ->
                activeConversationId = conv.id
                messages = conv.messages
                activeContext = conv.context
                showConversations = false
                errorText = null
            },
            onDelete = { conv ->
                recentConversations = recentConversations.filter { it.id != conv.id }
                scope.launch(Dispatchers.IO) { chatStore.delete(conv.id) }
                if (conv.id == activeConversationId) {
                    activeConversationId = UUID.randomUUID().toString()
                    messages = emptyList()
                    activeContext = null
                    errorText = null
                }
            },
            onDismiss = { showConversations = false }
        )
    }
}

@Composable
fun GreetingCard(displayName: String) {
    // Apple parity: time-of-day greeting like "用户，晚上好！"
    val hour = java.util.Calendar.getInstance().get(java.util.Calendar.HOUR_OF_DAY)
    val timeGreeting = when (hour) {
        in 5..11 -> "早上好"
        in 12..13 -> "中午好"
        in 14..17 -> "下午好"
        else -> "晚上好"
    }
    val displayLabel = if (displayName.isNotEmpty()) displayName else "用户"
    val greetingText = "$displayLabel，$timeGreeting！"

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 32.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = greetingText,
                style = MaterialTheme.typography.headlineLarge,
                color = MaterialTheme.colorScheme.onBackground
            )
            Text(
                text = "我是 Rokurics 的学习助手。在开始提问前，你可以先导入学习库中的内容作为对话上下文。",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                lineHeight = 20.sp,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(horizontal = 16.dp)
            )
        }
    }
}

@Composable
fun ChatBubble(message: DisplayChatMessage) {
    val isDark = isSystemInDarkTheme()
    val isUser = message.role == ChatMessageRole.USER
    val bgColor = if (isUser) RokuricsColors.aqua.copy(alpha = 0.15f)
    else if (isDark) Color(0xFF142828).copy(alpha = 0.7f)
    else Color.White.copy(alpha = 0.7f)
    val textColor = if (isDark) RokuricsColors.deepTextDark else RokuricsColors.deepText
    val borderColor = if (isUser) RokuricsColors.aqua.copy(alpha = 0.15f)
    else if (isDark) Color.White.copy(alpha = 0.06f)
    else Color.White.copy(alpha = 0.18f)
    val avatarBg = if (isUser) RokuricsColors.mint else RokuricsColors.aqua

    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = if (isUser) Arrangement.End else Arrangement.Start
    ) {
        if (!isUser) {
            Icon(
                imageVector = Icons.Default.Person,
                contentDescription = null,
                tint = RokuricsColors.aqua,
                modifier = Modifier
                    .size(32.dp)
                    .clip(CircleShape)
                    .background(avatarBg.copy(alpha = 0.12f))
                    .padding(6.dp)
            )
            Spacer(modifier = Modifier.width(8.dp))
        }

        Surface(
            modifier = Modifier.widthIn(max = 300.dp),
            shape = RoundedCornerShape(
                topStart = 16.dp,
                topEnd = 16.dp,
                bottomStart = if (isUser) 16.dp else 4.dp,
                bottomEnd = if (isUser) 4.dp else 16.dp
            ),
            color = bgColor,
            border = androidx.compose.foundation.BorderStroke(0.5.dp, borderColor)
        ) {
            Text(
                text = message.content,
                color = textColor,
                fontSize = 15.sp,
                lineHeight = 22.sp,
                modifier = Modifier.padding(12.dp)
            )
        }

        if (isUser) {
            Spacer(modifier = Modifier.width(8.dp))
            Icon(
                imageVector = Icons.Default.Person,
                contentDescription = null,
                tint = RokuricsColors.mint,
                modifier = Modifier
                    .size(32.dp)
                    .clip(CircleShape)
                    .background(RokuricsColors.mint.copy(alpha = 0.12f))
                    .padding(6.dp)
            )
        }
    }
}

@Composable
fun ContextChip(context: ChatContext, onRemove: () -> Unit) {
    Surface(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 12.dp, vertical = 6.dp),
        shape = RoundedCornerShape(12.dp),
        color = RokuricsColors.aqua.copy(alpha = 0.1f)
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                imageVector = Icons.Default.AttachFile,
                contentDescription = null,
                tint = RokuricsColors.aqua,
                modifier = Modifier.size(18.dp)
            )
            Spacer(modifier = Modifier.width(8.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = context.displayTitle,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = RokuricsColors.deepText,
                    maxLines = 1
                )
                Text(
                    text = "${context.itemCount} 项 · ${context.pathDisplay}",
                    fontSize = 11.sp,
                    color = RokuricsColors.softText,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
            IconButton(onClick = onRemove, modifier = Modifier.size(28.dp)) {
                Icon(
                    Icons.Default.Close,
                    contentDescription = "移除",
                    tint = RokuricsColors.softText,
                    modifier = Modifier.size(16.dp)
                )
            }
        }
    }
}

@Composable
fun ChatInputBar(
    draft: String,
    onDraftChange: (String) -> Unit,
    isSending: Boolean,
    onAttach: () -> Unit,
    onSettings: () -> Unit,
    onSend: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val inputBarColor = if (isDark) Color(0xFF0D2424).copy(alpha = 0.88f) else Color.White.copy(alpha = 0.7f)
    Surface(
        modifier = Modifier.fillMaxWidth(),
        color = inputBarColor,
        border = androidx.compose.foundation.BorderStroke(0.5.dp, Color.White.copy(alpha = if (isDark) 0.06f else 0.14f))
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 12.dp, vertical = 8.dp)
                .navigationBarsPadding(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onAttach) {
                Icon(
                    Icons.Default.AttachFile,
                    contentDescription = "导入上下文",
                    tint = RokuricsColors.aqua.copy(alpha = 0.7f)
                )
            }
            OutlinedTextField(
                value = draft,
                onValueChange = onDraftChange,
                modifier = Modifier.weight(1f),
                placeholder = { Text("消息", fontSize = 14.sp, color = if (isDark) RokuricsColors.tertiaryTextDark else RokuricsColors.tertiaryText) },
                shape = RoundedCornerShape(20.dp),
                maxLines = 3,
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = Color.Transparent,
                    unfocusedBorderColor = Color.Transparent,
                    focusedContainerColor = if (isDark) Color.White.copy(alpha = 0.08f) else Color.White.copy(alpha = 0.24f),
                    unfocusedContainerColor = if (isDark) Color.White.copy(alpha = 0.06f) else Color.White.copy(alpha = 0.18f),
                    focusedTextColor = if (isDark) RokuricsColors.deepTextDark else RokuricsColors.deepText,
                    unfocusedTextColor = if (isDark) RokuricsColors.deepTextDark else RokuricsColors.deepText
                )
            )
            Spacer(modifier = Modifier.width(8.dp))
            if (isSending) {
                CircularProgressIndicator(
                    modifier = Modifier.size(24.dp),
                    color = RokuricsColors.aqua,
                    strokeWidth = 2.dp
                )
            } else {
                IconButton(
                    onClick = onSend,
                    enabled = draft.isNotBlank()
                ) {
                    Icon(
                        Icons.AutoMirrored.Filled.Send,
                        contentDescription = "发送",
                        tint = if (draft.isNotBlank()) RokuricsColors.aqua else RokuricsColors.softText
                    )
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ContextPickerDialog(
    studyLibraryStore: StudyLibraryStore,
    onDismiss: () -> Unit,
    onSelect: (ChatContext) -> Unit
) {
    val items = remember { studyLibraryStore.allStudyItems }
    val isDark = isSystemInDarkTheme()
    val itemBg = if (isDark) Color(0xFF0D2424).copy(alpha = 0.55f) else Color.White.copy(alpha = 0.5f)

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        containerColor = if (isDark) Color(0xFF0A1B1B) else Color.White,
        shape = RoundedCornerShape(topStart = 24.dp, topEnd = 24.dp)
    ) {
        Text(
            text = "导入学习库上下文",
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
            color = if (isDark) RokuricsColors.deepTextDark else RokuricsColors.deepText,
            modifier = Modifier.padding(horizontal = 24.dp, vertical = 8.dp)
        )
        if (items.isEmpty()) {
            Box(Modifier.fillMaxWidth().padding(48.dp), contentAlignment = Alignment.Center) {
                Text("暂无学习库内容。请先录音并归档。", color = RokuricsColors.softText)
            }
        } else {
            LazyColumn(
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(max = 400.dp),
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                items(items.size) { index ->
                    val item = items[index]
                    val context = buildChatContext(item)
                    Surface(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable { onSelect(context) },
                        shape = RoundedCornerShape(12.dp),
                        color = itemBg
                    ) {
                        Row(
                            modifier = Modifier.padding(12.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.Description,
                                contentDescription = null,
                                tint = RokuricsColors.aqua
                            )
                            Spacer(modifier = Modifier.width(12.dp))
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = item.title,
                                    fontSize = 14.sp,
                                    fontWeight = FontWeight.SemiBold,
                                    color = if (isDark) RokuricsColors.deepTextDark else RokuricsColors.deepText
                                )
                                Text(
                                    text = item.filingPath.displaySummary,
                                    fontSize = 12.sp,
                                    color = RokuricsColors.softText
                                )
                            }
                        }
                    }
                }
            }
        }
        Spacer(modifier = Modifier.navigationBarsPadding())
    }
}

@Composable
fun AISettingsDialog(
    settingsStore: AISettingsStore,
    onDismiss: () -> Unit
) {
    var providerKind by remember { mutableStateOf(settingsStore.selectedProviderKind) }
    var preset by remember { mutableStateOf(settingsStore.selectedProviderPreset) }
    var openAIConfig by remember { mutableStateOf(settingsStore.openAIConfiguration) }
    var anthropicConfig by remember { mutableStateOf(settingsStore.anthropicConfiguration) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("AI 设置") },
        text = {
            LazyColumn(
                modifier = Modifier.height(400.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                item {
                    Text("提供商", fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        AIProviderKind.values().forEach { kind ->
                            FilterChip(
                                selected = providerKind == kind,
                                onClick = { providerKind = kind },
                                label = { Text(kind.displayName, fontSize = 12.sp) }
                            )
                        }
                    }
                }

                if (providerKind == AIProviderKind.OPEN_AI_COMPATIBLE) {
                    item {
                        Text("预设", fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            AIProviderPreset.values().filter { it.isAvailableOnPhone }.forEach { p ->
                                FilterChip(
                                    selected = preset == p,
                                    onClick = { preset = p },
                                    label = { Text(p.displayName, fontSize = 12.sp) }
                                )
                            }
                        }
                    }
                    item {
                        OutlinedTextField(
                            value = openAIConfig.baseURLString,
                            onValueChange = { openAIConfig = openAIConfig.copy(baseURLString = it) },
                            label = { Text("Base URL") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                        )
                    }
                    item {
                        OutlinedTextField(
                            value = openAIConfig.modelName,
                            onValueChange = { openAIConfig = openAIConfig.copy(modelName = it) },
                            label = { Text("模型名称") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                        )
                    }
                    item {
                        OutlinedTextField(
                            value = openAIConfig.apiKey,
                            onValueChange = { openAIConfig = openAIConfig.copy(apiKey = it) },
                            label = { Text("API Key") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                        )
                    }
                }

                if (providerKind == AIProviderKind.ANTHROPIC_MESSAGES) {
                    item {
                        OutlinedTextField(
                            value = anthropicConfig.modelName,
                            onValueChange = { anthropicConfig = anthropicConfig.copy(modelName = it) },
                            label = { Text("模型名称") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                        )
                    }
                    item {
                        OutlinedTextField(
                            value = anthropicConfig.apiKey,
                            onValueChange = { anthropicConfig = anthropicConfig.copy(apiKey = it) },
                            label = { Text("API Key") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                        )
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = {
                if (providerKind == AIProviderKind.OPEN_AI_COMPATIBLE) {
                    settingsStore.updateOpenAI(preset, openAIConfig)
                } else {
                    settingsStore.updateAnthropic(anthropicConfig)
                }
                onDismiss()
            }) {
                Text("保存")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("取消") }
        }
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RecentConversationsDialog(
    conversations: List<SavedConversation>,
    activeConversationId: String,
    onSelect: (SavedConversation) -> Unit,
    onDelete: (SavedConversation) -> Unit,
    onDismiss: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val itemBgInactive = if (isDark) Color(0xFF0D2424).copy(alpha = 0.55f) else Color.White.copy(alpha = 0.5f)
    val textColor = if (isDark) RokuricsColors.deepTextDark else RokuricsColors.deepText

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        containerColor = if (isDark) Color(0xFF0A1B1B) else Color.White,
        shape = RoundedCornerShape(topStart = 24.dp, topEnd = 24.dp)
    ) {
        Text(
            text = "最近对话",
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
            color = textColor,
            modifier = Modifier.padding(horizontal = 24.dp, vertical = 8.dp)
        )
        if (conversations.isEmpty()) {
            Box(Modifier.fillMaxWidth().padding(48.dp), contentAlignment = Alignment.Center) {
                Text("暂无最近对话", color = RokuricsColors.softText)
            }
        } else {
            LazyColumn(
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(max = 400.dp),
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                items(conversations.size) { index ->
                    val conv = conversations[index]
                    val isActive = conv.id == activeConversationId
                    Surface(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable { onSelect(conv) },
                        shape = RoundedCornerShape(12.dp),
                        color = if (isActive) RokuricsColors.aqua.copy(alpha = 0.12f)
                        else itemBgInactive
                    ) {
                        Row(
                            modifier = Modifier.padding(12.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.ChatBubble,
                                contentDescription = null,
                                tint = if (isActive) RokuricsColors.aqua else RokuricsColors.softText,
                                modifier = Modifier.size(20.dp)
                            )
                            Spacer(modifier = Modifier.width(12.dp))
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = conv.title,
                                    fontSize = 14.sp,
                                    fontWeight = FontWeight.SemiBold,
                                    color = textColor,
                                    maxLines = 1
                                )
                                if (conv.preview != null) {
                                    Text(
                                        text = conv.preview,
                                        fontSize = 12.sp,
                                        color = RokuricsColors.softText,
                                        maxLines = 2,
                                        overflow = TextOverflow.Ellipsis
                                    )
                                }
                            }
                            IconButton(onClick = { onDelete(conv) }) {
                                Icon(
                                    Icons.Default.Delete,
                                    contentDescription = "删除",
                                    tint = RokuricsColors.softText,
                                    modifier = Modifier.size(18.dp)
                                )
                            }
                        }
                    }
                }
            }
        }
        Spacer(modifier = Modifier.navigationBarsPadding())
    }
}

data class SavedConversation(
    val id: String,
    val title: String,
    val messages: List<DisplayChatMessage>,
    val context: ChatContext?,
    val preview: String?
)

// Utilities

private fun buildChatContext(item: StudyItemMetadata): ChatContext {
    val parts = listOfNotNull(
        item.filingPath.type, item.filingPath.subject,
        item.filingPath.chapter, item.filingPath.topic
    )
    val pathDisplay = parts.ifEmpty { listOf("未分类") }.joinToString(" / ")
    val content = buildString {
        append("标题：${item.title}\n")
        append("分类：$pathDisplay\n")
        if (item.duration != null) {
            append("时长：${"%.1f".format(item.duration / 60)} 分钟\n")
        }
        append("类型：${item.kind.name}\n")
    }
    return ChatContext(
        id = item.itemID,
        pathDisplay = pathDisplay,
        itemCount = 1,
        totalCharacterCount = content.length,
        formattedContext = content,
        displayTitle = item.title
    )
}

private fun buildProvider(store: AISettingsStore): IPhoneChatProvider {
    return when (store.selectedProviderKind) {
        AIProviderKind.OPEN_AI_COMPATIBLE -> OpenAICompatibleChatProvider(
            preset = store.selectedProviderPreset,
            configuration = store.openAIConfiguration
        )
        AIProviderKind.ANTHROPIC_MESSAGES -> AnthropicChatProvider(
            configuration = store.anthropicConfiguration
        )
    }
}

private fun saveSnapshot(
    messages: List<DisplayChatMessage>,
    context: ChatContext?,
    conversationId: String,
    existingConversations: List<SavedConversation>,
    chatStore: ChatStore,
    scope: kotlinx.coroutines.CoroutineScope,
    onUpdate: (List<SavedConversation>) -> Unit
) {
    if (messages.isEmpty() && context == null) return

    val title = if (context != null) {
        context.displayTitle
    } else {
        messages.firstOrNull { it.role == ChatMessageRole.USER }?.content
            ?.trim()?.take(28) ?: "新对话"
    }

    val preview = messages.lastOrNull { it.role != ChatMessageRole.SYSTEM }?.content?.trim()?.take(80)

    val snapshot = SavedConversation(
        id = conversationId,
        title = title,
        messages = messages,
        context = context,
        preview = preview
    )

    val updated = listOf(snapshot) + existingConversations.filter { it.id != conversationId }
    onUpdate(updated.take(12))

    // Persist to disk
    val persisted = PersistedConversation(
        id = conversationId,
        title = title,
        messages = messages.map { PersistedMessage(
            id = it.id,
            role = it.role.rawValue,
            content = it.content,
            timestamp = it.timestamp
        ) }.toMutableList(),
        contextID = context?.id,
        contextPathDisplay = context?.pathDisplay,
        contextFormattedContext = context?.formattedContext,
        createdAt = System.currentTimeMillis()
    )
    scope.launch(Dispatchers.IO) { chatStore.save(persisted); chatStore.pruneOldest() }
}

private fun startNewConversation(
    messages: List<DisplayChatMessage>,
    context: ChatContext?,
    conversationId: String,
    existingConversations: List<SavedConversation>,
    chatStore: ChatStore,
    scope: kotlinx.coroutines.CoroutineScope,
    onComplete: (String, List<DisplayChatMessage>, ChatContext?, List<SavedConversation>) -> Unit
) {
    saveSnapshot(messages, context, conversationId, existingConversations, chatStore, scope) { updated ->
        onComplete(UUID.randomUUID().toString(), emptyList(), null, updated)
    }
}

private fun PersistedConversation.toUIModel(): SavedConversation {
    val messages = this.messages.map { m ->
        DisplayChatMessage(
            id = m.id,
            role = try { ChatMessageRole.valueOf(m.role.uppercase()) } catch (_: Exception) { ChatMessageRole.USER },
            content = m.content,
            timestamp = m.timestamp
        )
    }
    val ctxID = this.contextID
    val context = if (ctxID != null) {
        ChatContext(
            id = ctxID,
            pathDisplay = contextPathDisplay ?: "",
            itemCount = 0,
            totalCharacterCount = contextFormattedContext?.length ?: 0,
            formattedContext = contextFormattedContext ?: "",
            displayTitle = title
        )
    } else null
    return SavedConversation(
        id = id,
        title = title,
        messages = messages,
        context = context,
        preview = messages.lastOrNull { it.role != ChatMessageRole.SYSTEM }?.content?.trim()?.take(80)
    )
}

@Composable
private fun SuspendingLaunchedEffect(key: Any, block: suspend () -> Unit) {
    LaunchedEffect(key) {
        block()
    }
}
