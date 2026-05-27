package com.rokurics.app.ui.settings

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.rokurics.app.data.UserPreferencesStore
import com.rokurics.app.ui.chat.AIProviderKind
import com.rokurics.app.ui.chat.AIProviderPreset
import com.rokurics.app.ui.chat.AISettingsStore
import com.rokurics.app.ui.theme.RokuricsColors

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(onBack: () -> Unit) {
    val context = androidx.compose.ui.platform.LocalContext.current
    val store = remember { UserPreferencesStore(context) }
    val aiStore = remember { AISettingsStore(context) }
    val profile = remember { store.load() }

    var displayName by remember { mutableStateOf(profile.displayName) }
    var handle by remember { mutableStateOf(profile.handle) }
    var avatarSelection by remember { mutableStateOf(profile.avatar) }

    var aiProviderKind by remember { mutableStateOf(aiStore.selectedProviderKind) }
    var aiPreset by remember { mutableStateOf(aiStore.selectedProviderPreset) }
    var openAIBaseURL by remember { mutableStateOf(aiStore.openAIConfiguration.baseURLString) }
    var openAIModel by remember { mutableStateOf(aiStore.openAIConfiguration.modelName) }
    var openAIKey by remember { mutableStateOf(aiStore.openAIConfiguration.apiKey) }
    var anthropicModel by remember { mutableStateOf(aiStore.anthropicConfiguration.modelName) }
    var anthropicKey by remember { mutableStateOf(aiStore.anthropicConfiguration.apiKey) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("设置") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "返回")
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .background(Color(0xFFF0FAF8))
                .verticalScroll(rememberScrollState())
                .padding(24.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Profile card
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.7f)),
                shape = RoundedCornerShape(20.dp)
            ) {
                Column(
                    modifier = Modifier.padding(24.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Icon(
                        imageVector = Icons.Default.Person,
                        contentDescription = null,
                        tint = RokuricsColors.aqua,
                        modifier = Modifier.size(64.dp)
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                    Text(
                        text = "Rokurics",
                        fontSize = 24.sp,
                        fontWeight = FontWeight.Bold,
                        color = RokuricsColors.deepText
                    )
                    Text(
                        text = "课堂录音 · 学习管理",
                        fontSize = 14.sp,
                        color = RokuricsColors.softText
                    )
                }
            }

            // Profile settings section
            Text("个人信息", fontWeight = FontWeight.SemiBold, fontSize = 16.sp, color = RokuricsColors.deepText)
            OutlinedTextField(
                value = displayName,
                onValueChange = { displayName = it },
                label = { Text("显示名称") },
                modifier = Modifier.fillMaxWidth()
            )
            OutlinedTextField(
                value = handle,
                onValueChange = { handle = it },
                label = { Text("用户名") },
                modifier = Modifier.fillMaxWidth()
            )

            Button(
                onClick = {
                    store.update(displayName, handle, avatarSelection)
                },
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("保存资料")
            }

            HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))

            // AI Settings section
            Text("AI 对话设置", fontWeight = FontWeight.SemiBold, fontSize = 16.sp, color = RokuricsColors.deepText)

            Text("提供商", fontSize = 14.sp, color = RokuricsColors.softText)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                AIProviderKind.values().forEach { kind ->
                    FilterChip(
                        selected = aiProviderKind == kind,
                        onClick = { aiProviderKind = kind },
                        label = { Text(kind.displayName, fontSize = 12.sp) }
                    )
                }
            }

            if (aiProviderKind == AIProviderKind.OPEN_AI_COMPATIBLE) {
                Text("预设", fontSize = 14.sp, color = RokuricsColors.softText)
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    AIProviderPreset.values().filter { it.isAvailableOnPhone }.forEach { preset ->
                        FilterChip(
                            selected = aiPreset == preset,
                            onClick = { aiPreset = preset },
                            label = { Text(preset.displayName, fontSize = 12.sp) }
                        )
                    }
                }

                OutlinedTextField(
                    value = openAIBaseURL,
                    onValueChange = { openAIBaseURL = it },
                    label = { Text("Base URL") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                OutlinedTextField(
                    value = openAIModel,
                    onValueChange = { openAIModel = it },
                    label = { Text("模型名称") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                OutlinedTextField(
                    value = openAIKey,
                    onValueChange = { openAIKey = it },
                    label = { Text("API Key") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
            }

            if (aiProviderKind == AIProviderKind.ANTHROPIC_MESSAGES) {
                OutlinedTextField(
                    value = anthropicModel,
                    onValueChange = { anthropicModel = it },
                    label = { Text("模型名称") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                OutlinedTextField(
                    value = anthropicKey,
                    onValueChange = { anthropicKey = it },
                    label = { Text("API Key") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
            }

            Button(
                onClick = {
                    if (aiProviderKind == AIProviderKind.OPEN_AI_COMPATIBLE) {
                        aiStore.updateOpenAI(
                            aiPreset,
                            aiStore.openAIConfiguration.copy(
                                baseURLString = openAIBaseURL,
                                modelName = openAIModel,
                                apiKey = openAIKey
                            )
                        )
                    } else {
                        aiStore.updateAnthropic(
                            aiStore.anthropicConfiguration.copy(
                                modelName = anthropicModel,
                                apiKey = anthropicKey
                            )
                        )
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = RokuricsColors.aqua)
            ) {
                Text("保存 AI 设置")
            }

            HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))

            // Transcription Settings (Apple parity: IPhoneTranscriptionSettingsDetail)
            Text("转写设置", fontWeight = FontWeight.SemiBold, fontSize = 16.sp, color = RokuricsColors.deepText)

            var transcriptionProvider by remember { mutableStateOf("mac_secure") }
            var localModelName by remember { mutableStateOf("ggml-small.bin") }

            Text("转写服务", fontSize = 14.sp, color = RokuricsColors.softText)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                FilterChip(
                    selected = transcriptionProvider == "mac_secure",
                    onClick = { transcriptionProvider = "mac_secure" },
                    label = { Text("Mac 安全转写", fontSize = 12.sp) }
                )
                FilterChip(
                    selected = transcriptionProvider == "local",
                    onClick = { transcriptionProvider = "local" },
                    label = { Text("本地转写", fontSize = 12.sp) }
                )
            }

            if (transcriptionProvider == "local") {
                Text("语音模型", fontSize = 14.sp, color = RokuricsColors.softText)
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    listOf("ggml-tiny.bin", "ggml-small.bin", "ggml-medium.bin").forEach { model ->
                        FilterChip(
                            selected = localModelName == model,
                            onClick = { localModelName = model },
                            label = { Text(model, fontSize = 11.sp) }
                        )
                    }
                }
            } else {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.52f)),
                    border = androidx.compose.foundation.BorderStroke(0.5.dp, Color.White.copy(alpha = 0.22f))
                ) {
                    Row(
                        modifier = Modifier.padding(14.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(
                            Icons.Default.CheckCircle,
                            contentDescription = null,
                            tint = RokuricsColors.mint,
                            modifier = Modifier.size(20.dp)
                        )
                        Spacer(Modifier.width(10.dp))
                        Column {
                            Text(
                                "Mac 安全转写已就绪",
                                fontSize = 14.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = RokuricsColors.deepText
                            )
                            Text(
                                "通过配对的 Mac 进行 whisper.cpp 离线转写",
                                fontSize = 12.sp,
                                color = RokuricsColors.softText
                            )
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.navigationBarsPadding())
            Spacer(modifier = Modifier.height(24.dp))
        }
    }
}
