package com.rokurics.app.ui.settings

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.rokurics.app.data.UserPreferencesStore
import com.rokurics.app.ui.chat.AIProviderKind
import com.rokurics.app.ui.chat.AIProviderPreset
import com.rokurics.app.ui.chat.AISettingsStore
import com.rokurics.app.ui.theme.adaptiveColor
import com.rokurics.app.ui.theme.adaptivePageGradientBrush
import com.rokurics.app.ui.theme.RokuricsColors
import com.rokurics.app.ui.theme.rokuricsGlassCircle
import com.rokurics.app.ui.theme.rokuricsScaleClickable

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

    var transcriptionProvider by remember { mutableStateOf("mac_secure") }
    var localModelName by remember { mutableStateOf("ggml-small.bin") }

    var showTranscriptionSettings by remember { mutableStateOf(false) }
    var showAISettings by remember { mutableStateOf(false) }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(adaptivePageGradientBrush())
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
        ) {
            // ── Glass-style header (Apple parity: RokuricsMobilePageHeader) ──
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .statusBarsPadding()
                    .padding(horizontal = 20.dp, vertical = 12.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Box(
                    modifier = Modifier
                        .size(44.dp)
                        .rokuricsGlassCircle(fillOpacity = 0.42f, strokeOpacity = 0.50f, shadowOpacity = 0.14f, shadowRadius = 12.dp, fillColor = if (isSystemInDarkTheme()) RokuricsColors.glassSurfaceDark else Color.White)
                        .rokuricsScaleClickable(onClick = onBack),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        Icons.AutoMirrored.Filled.ArrowBack,
                        contentDescription = "返回",
                        tint = adaptiveColor(RokuricsColors.deepText, RokuricsColors.deepTextDark),
                        modifier = Modifier.size(20.dp)
                    )
                }
            }

            // Page title (Apple parity: serif "设置")
            Text(
                text = "设置",
                fontSize = 34.sp,
                fontWeight = FontWeight.Bold,
                fontFamily = FontFamily.Serif,
                color = adaptiveColor(RokuricsColors.deepText, RokuricsColors.deepTextDark),
                modifier = Modifier.padding(horizontal = 20.dp, vertical = 4.dp)
            )

            Spacer(modifier = Modifier.height(20.dp))

            // ── Profile section (Apple parity: centered avatar + name + handle) ──
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 24.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Box(
                    modifier = Modifier
                        .size(80.dp)
                        .rokuricsGlassCircle(fillOpacity = 0.36f, strokeOpacity = 0.50f, shadowOpacity = 0.14f, fillColor = if (isSystemInDarkTheme()) RokuricsColors.glassSurfaceDark else Color.White),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        Icons.Default.Person,
                        contentDescription = null,
                        tint = RokuricsColors.aqua,
                        modifier = Modifier.size(44.dp)
                    )
                }
                Spacer(modifier = Modifier.height(12.dp))
                Text(
                    text = displayName.ifEmpty { "用户" },
                    fontSize = 24.sp,
                    fontWeight = FontWeight.Bold,
                    color = adaptiveColor(RokuricsColors.deepText, RokuricsColors.deepTextDark)
                )
                Text(
                    text = if (handle.isNotEmpty()) "@$handle" else "@rokurics_user",
                    fontSize = 14.sp,
                    color = adaptiveColor(RokuricsColors.softText, RokuricsColors.softTextDark)
                )
                Spacer(modifier = Modifier.height(16.dp))
                // Edit profile button (pill)
                Surface(
                    modifier = Modifier
                        .rokuricsScaleClickable(onClick = { /* edit profile action */ }),
                    shape = RoundedCornerShape(50),
                    color = Color.White.copy(alpha = 0.24f),
                    border = androidx.compose.foundation.BorderStroke(0.5.dp, Color.White.copy(alpha = 0.36f))
                ) {
                    Text(
                        text = "编辑个人资料",
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Medium,
                        color = adaptiveColor(RokuricsColors.deepText, RokuricsColors.deepTextDark),
                        modifier = Modifier.padding(horizontal = 24.dp, vertical = 10.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(28.dp))

            // ── 转写 section (Apple parity: IPhoneTranscriptionSettingsDetail) ──
            SettingsSectionHeader("转写")
            SettingsGroupCard {
                SettingsRow(
                    label = "Provider",
                    value = "Mac 安全转写",
                    isSerifLabel = true,
                    onClick = { showTranscriptionSettings = true }
                )
                SettingsDivider()
                SettingsRow(
                    label = "模型",
                    value = localModelName,
                    isMonoValue = true,
                    onClick = { showTranscriptionSettings = true }
                )
                SettingsDivider()
                SettingsRow(
                    label = "授权与测试",
                    value = "查看",
                    isAction = true,
                    onClick = { showTranscriptionSettings = true }
                )
            }

            Spacer(modifier = Modifier.height(20.dp))

            // ── AI section (Apple parity: IPhoneAISettingsDetail) ──
            SettingsSectionHeader("AI")
            SettingsGroupCard {
                SettingsRow(
                    label = "Provider",
                    value = aiProviderKind.displayName,
                    isSerifLabel = true,
                    onClick = { showAISettings = true }
                )
                SettingsDivider()
                SettingsRow(
                    label = "模型",
                    value = when {
                        aiProviderKind == AIProviderKind.OPEN_AI_COMPATIBLE -> openAIModel.ifEmpty { aiPreset.displayName }
                        aiProviderKind == AIProviderKind.ANTHROPIC_MESSAGES -> anthropicModel.ifEmpty { "Claude" }
                        else -> "—"
                    },
                    isMonoValue = true,
                    onClick = { showAISettings = true }
                )
                SettingsDivider()
                SettingsRow(
                    label = "API 设置",
                    value = "查看",
                    isAction = true,
                    onClick = { showAISettings = true }
                )
                SettingsDivider()
                SettingsRow(
                    label = "测试",
                    value = "查看",
                    isAction = true,
                    onClick = { showAISettings = true }
                )
            }

            Spacer(modifier = Modifier.height(20.dp))

            // ── 关于 section (Apple parity) ──
            SettingsSectionHeader("关于")
            SettingsGroupCard {
                SettingsRow(
                    label = "存储",
                    value = "本机"
                )
                SettingsDivider()
                SettingsRow(
                    label = "隐私政策",
                    value = "查看",
                    isAction = true
                )
                SettingsDivider()
                SettingsRow(
                    label = "版权",
                    value = "1.0 (1)"
                )
            }

            Spacer(modifier = Modifier.navigationBarsPadding())
            Spacer(modifier = Modifier.height(24.dp))
        }
    }

    // ── Transcription settings detail sheet ──
    if (showTranscriptionSettings) {
        TranscriptionSettingsSheet(
            provider = transcriptionProvider,
            modelName = localModelName,
            onProviderChange = { transcriptionProvider = it },
            onModelChange = { localModelName = it },
            onDismiss = { showTranscriptionSettings = false }
        )
    }

    // ── AI settings detail sheet ──
    if (showAISettings) {
        AISettingsDetailSheet(
            providerKind = aiProviderKind,
            preset = aiPreset,
            openAIBaseURL = openAIBaseURL,
            openAIModel = openAIModel,
            openAIKey = openAIKey,
            anthropicModel = anthropicModel,
            anthropicKey = anthropicKey,
            onProviderKindChange = { aiProviderKind = it },
            onPresetChange = { aiPreset = it },
            onOpenAIBaseURLChange = { openAIBaseURL = it },
            onOpenAIModelChange = { openAIModel = it },
            onOpenAIKeyChange = { openAIKey = it },
            onAnthropicModelChange = { anthropicModel = it },
            onAnthropicKeyChange = { anthropicKey = it },
            onSave = {
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
            onDismiss = { showAISettings = false }
        )
    }
}

// ── Reusable settings components ──

@Composable
private fun SettingsSectionHeader(title: String) {
    Text(
        text = title,
        fontSize = 13.sp,
        fontWeight = FontWeight.Medium,
        color = adaptiveColor(
            RokuricsColors.softText.copy(alpha = 0.7f),
            RokuricsColors.softTextDark.copy(alpha = 0.7f)
        ),
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
    )
}

@Composable
private fun SettingsGroupCard(content: @Composable ColumnScope.() -> Unit) {
    val isDark = isSystemInDarkTheme()
    val cardBg = if (isDark) Color(0xFF0D2424).copy(alpha = 0.6f) else Color.White.copy(alpha = 0.5f)
    val cardBorder = Color.White.copy(alpha = if (isDark) 0.06f else 0.22f)
    Surface(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp),
        shape = RoundedCornerShape(20.dp),
        color = cardBg,
        border = androidx.compose.foundation.BorderStroke(0.5.dp, cardBorder)
    ) {
        Column(modifier = Modifier.padding(horizontal = 4.dp, vertical = 2.dp), content = content)
    }
}

@Composable
private fun SettingsRow(
    label: String,
    value: String,
    isSerifLabel: Boolean = false,
    isMonoValue: Boolean = false,
    isAction: Boolean = false,
    onClick: (() -> Unit)? = null
) {
    val rowModifier = if (onClick != null) {
        Modifier
            .fillMaxWidth()
            .rokuricsScaleClickable(onClick = onClick)
            .padding(horizontal = 16.dp, vertical = 14.dp)
    } else {
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 14.dp)
    }

    Row(
        modifier = rowModifier,
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = label,
            fontSize = 16.sp,
            fontWeight = FontWeight.Medium,
            fontFamily = if (isSerifLabel) FontFamily.Serif else FontFamily.Default,
            color = adaptiveColor(RokuricsColors.deepText, RokuricsColors.deepTextDark)
        )
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(
                text = value,
                fontSize = 15.sp,
                fontFamily = if (isMonoValue) FontFamily.Monospace else FontFamily.Default,
                color = if (isAction) adaptiveColor(RokuricsColors.aqua, RokuricsColors.aquaDark)
                else adaptiveColor(RokuricsColors.softText, RokuricsColors.softTextDark)
            )
            if (isAction) {
                Spacer(modifier = Modifier.width(2.dp))
                Icon(
                    Icons.Default.ChevronRight,
                    contentDescription = null,
                    tint = adaptiveColor(RokuricsColors.aqua, RokuricsColors.aquaDark),
                    modifier = Modifier.size(18.dp)
                )
            }
        }
    }
}

@Composable
private fun SettingsDivider() {
    HorizontalDivider(
        modifier = Modifier.padding(horizontal = 16.dp),
        thickness = 0.5.dp,
        color = RokuricsColors.softText.copy(alpha = 0.10f)
    )
}

// ── Detail sheets ──

@Composable
private fun TranscriptionSettingsSheet(
    provider: String,
    modelName: String,
    onProviderChange: (String) -> Unit,
    onModelChange: (String) -> Unit,
    onDismiss: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text("转写设置", fontWeight = FontWeight.Bold)
        },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text("转写服务", fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    FilterChip(
                        selected = provider == "mac_secure",
                        onClick = { onProviderChange("mac_secure") },
                        label = { Text("Mac 安全转写", fontSize = 12.sp) }
                    )
                    FilterChip(
                        selected = provider == "local",
                        onClick = { onProviderChange("local") },
                        label = { Text("本地转写", fontSize = 12.sp) }
                    )
                }
                if (provider == "local") {
                    Text("语音模型", fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        listOf("ggml-tiny.bin", "ggml-small.bin", "ggml-medium.bin").forEach { model ->
                            FilterChip(
                                selected = modelName == model,
                                onClick = { onModelChange(model) },
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
            }
        },
        confirmButton = {
            TextButton(onClick = onDismiss) { Text("完成") }
        }
    )
}

@Composable
private fun AISettingsDetailSheet(
    providerKind: AIProviderKind,
    preset: AIProviderPreset,
    openAIBaseURL: String,
    openAIModel: String,
    openAIKey: String,
    anthropicModel: String,
    anthropicKey: String,
    onProviderKindChange: (AIProviderKind) -> Unit,
    onPresetChange: (AIProviderPreset) -> Unit,
    onOpenAIBaseURLChange: (String) -> Unit,
    onOpenAIModelChange: (String) -> Unit,
    onOpenAIKeyChange: (String) -> Unit,
    onAnthropicModelChange: (String) -> Unit,
    onAnthropicKeyChange: (String) -> Unit,
    onSave: () -> Unit,
    onDismiss: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text("AI 设置", fontWeight = FontWeight.Bold)
        },
        text = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(max = 440.dp)
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                Text("提供商", fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    AIProviderKind.values().forEach { kind ->
                        FilterChip(
                            selected = providerKind == kind,
                            onClick = { onProviderKindChange(kind) },
                            label = { Text(kind.displayName, fontSize = 12.sp) }
                        )
                    }
                }

                if (providerKind == AIProviderKind.OPEN_AI_COMPATIBLE) {
                    Text("预设", fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        AIProviderPreset.values().filter { it.isAvailableOnPhone }.forEach { p ->
                            FilterChip(
                                selected = preset == p,
                                onClick = { onPresetChange(p) },
                                label = { Text(p.displayName, fontSize = 12.sp) }
                            )
                        }
                    }
                    OutlinedTextField(
                        value = openAIBaseURL,
                        onValueChange = onOpenAIBaseURLChange,
                        label = { Text("Base URL") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                    )
                    OutlinedTextField(
                        value = openAIModel,
                        onValueChange = onOpenAIModelChange,
                        label = { Text("模型名称") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                    )
                    OutlinedTextField(
                        value = openAIKey,
                        onValueChange = onOpenAIKeyChange,
                        label = { Text("API Key") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                    )
                }

                if (providerKind == AIProviderKind.ANTHROPIC_MESSAGES) {
                    OutlinedTextField(
                        value = anthropicModel,
                        onValueChange = onAnthropicModelChange,
                        label = { Text("模型名称") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                    )
                    OutlinedTextField(
                        value = anthropicKey,
                        onValueChange = onAnthropicKeyChange,
                        label = { Text("API Key") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        textStyle = LocalTextStyle.current.copy(fontSize = 13.sp)
                    )
                }
            }
        },
        confirmButton = {
            TextButton(onClick = {
                onSave()
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
