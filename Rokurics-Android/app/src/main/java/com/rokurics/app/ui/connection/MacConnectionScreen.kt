package com.rokurics.app.ui.connection

import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.animation.core.RepeatMode
import androidx.compose.foundation.background
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
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.rokurics.app.data.ConnectionStore
import com.rokurics.app.data.LocalNetworkSyncStateStore
import com.rokurics.app.data.SecureUploadClient
import com.rokurics.app.data.SecureUploadUtilities
import com.rokurics.app.domain.model.LocalNetworkSyncState
import com.rokurics.app.domain.sync.LocalNetworkSyncEngine
import com.rokurics.app.ui.theme.adaptivePageGradientBrush
import com.rokurics.app.ui.theme.RokuricsColors
import com.rokurics.app.ui.theme.rokuricsGlassCircle
import com.rokurics.app.ui.theme.rokuricsScaleClickable
import kotlinx.coroutines.launch

@Composable
fun MacConnectionScreen(onBack: () -> Unit) {
    val connectionStore = remember { ConnectionStore() }
    val secureClient = remember { SecureUploadClient() }
    val scope = rememberCoroutineScope()

    var host by remember { mutableStateOf(connectionStore.macHost) }
    var portText by remember { mutableStateOf(connectionStore.macPort.toString()) }
    var fingerprint by remember { mutableStateOf(connectionStore.macFingerprint) }
    var pairingCode by remember { mutableStateOf("") }
    val syncStateStore = remember { LocalNetworkSyncStateStore() }
    var syncState by remember { mutableStateOf(syncStateStore.load()) }
    var statusText by remember { mutableStateOf(if (connectionStore.isPaired) "已配对" else "未配对") }
    var isConnecting by remember { mutableStateOf(false) }
    var syncStatusText by remember { mutableStateOf<String?>(null) }
    var feedbackText by remember { mutableStateOf<String?>(null) }
    var feedbackIsError by remember { mutableStateOf(false) }

    // iPhone parity: page gradient background with scrollable content
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(adaptivePageGradientBrush())
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp)
                .statusBarsPadding()
        ) {
            Spacer(modifier = Modifier.height(18.dp))

            // Header — iPhone parity: back button + status capsule row
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Back button — iPhone parity: RokuricsIconCircleButton with chevron.left
                Box(
                    modifier = Modifier
                        .size(44.dp)
                        .rokuricsGlassCircle(
                            fillOpacity = 0.36f,
                            strokeOpacity = 0.50f,
                            shadowOpacity = 0.14f,
                            shadowRadius = 12.dp,
                            fillColor = if (isSystemInDarkTheme()) RokuricsColors.glassSurfaceDark else Color.White
                        )
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

                // Status capsule — iPhone parity: MacConnectionStateCapsule
                if (!connectionStore.isPaired) {
                    Surface(
                        shape = RoundedCornerShape(20.dp),
                        color = Color.White.copy(alpha = 0.40f),
                        border = androidx.compose.foundation.BorderStroke(
                            0.5.dp, RokuricsColors.glassStroke.copy(alpha = 0.44f)
                        )
                    ) {
                        Text(
                            text = "未配对",
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = RokuricsColors.softText,
                            modifier = Modifier.padding(horizontal = 14.dp, vertical = 6.dp)
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(4.dp))

            // Page title — iPhone parity: serif "Mac 连接", left-aligned
            Text(
                text = "Mac 连接",
                fontSize = 32.sp,
                fontWeight = FontWeight.Bold,
                fontFamily = androidx.compose.ui.text.font.FontFamily.Serif,
                color = RokuricsColors.deepText
            )

            Spacer(modifier = Modifier.height(22.dp))

            if (connectionStore.isPaired) {
                pairedContent(
                    connectionStore = connectionStore,
                    syncState = syncState,
                    syncStatusText = syncStatusText,
                    isConnecting = isConnecting,
                    onSync = {
                        scope.launch {
                            syncStatusText = "正在同步..."
                            try {
                                val engine = LocalNetworkSyncEngine()
                                val result = engine.performTick("manual")
                                syncState = syncStateStore.load()
                                syncStatusText = if (result.success) "同步完成 · ${result.statusText}"
                                else result.statusText
                            } catch (e: Exception) {
                                syncState = syncStateStore.load()
                                syncStatusText = "同步失败: ${e.message}"
                            }
                        }
                    },
                    onDisconnect = {
                        connectionStore.clearPairing()
                        statusText = "已断开"
                        pairingCode = ""
                        syncState = LocalNetworkSyncState()
                    }
                )
            } else {
                unpairedContent(
                    host = host,
                    portText = portText,
                    fingerprint = fingerprint,
                    pairingCode = pairingCode,
                    isConnecting = isConnecting,
                    feedbackText = feedbackText,
                    feedbackIsError = feedbackIsError,
                    onHostChange = { host = it },
                    onPortChange = { portText = it.filter { c -> c.isDigit() }.take(5) },
                    onFingerprintChange = { fingerprint = SecureUploadUtilities.normalizedCertificateFingerprint(it) },
                    onPairingCodeChange = { pairingCode = it.filter { c -> c.isDigit() }.take(6) },
                    onPair = {
                        scope.launch {
                            isConnecting = true
                            feedbackText = "正在配对..."
                            feedbackIsError = false
                            val port = portText.toIntOrNull() ?: 8787
                            connectionStore.macHost = host
                            connectionStore.macPort = port
                            connectionStore.macFingerprint = fingerprint

                            val result = secureClient.pair(
                                host = host, port = port,
                                pairingCode = pairingCode,
                                macFingerprint = fingerprint,
                                deviceName = android.os.Build.MODEL
                            )
                            result.fold(
                                onSuccess = { pairingResult ->
                                    connectionStore.savePairing(pairingResult, host, port, fingerprint)
                                    statusText = "配对成功"
                                    feedbackText = "配对成功"
                                    feedbackIsError = false
                                },
                                onFailure = { error ->
                                    statusText = "配对失败"
                                    feedbackText = "配对失败: ${error.message}"
                                    feedbackIsError = true
                                }
                            )
                            isConnecting = false
                        }
                    }
                )
            }

            Spacer(modifier = Modifier.height(34.dp))
            Spacer(modifier = Modifier.navigationBarsPadding())
        }
    }
}

@Composable
private fun pairedContent(
    connectionStore: ConnectionStore,
    syncState: LocalNetworkSyncState,
    syncStatusText: String?,
    isConnecting: Boolean,
    onSync: () -> Unit,
    onDisconnect: () -> Unit
) {
    // iPhone parity: ConnectedDeviceBubbleView + ConnectedDeviceCardView
    Column(verticalArrangement = Arrangement.spacedBy(16.dp)) {
        // Connected device bubble — animated breathing circle
        val infiniteTransition = rememberInfiniteTransition(label = "bubbleBreath")
        val breathScale by infiniteTransition.animateFloat(
            initialValue = 1f,
            targetValue = 1.035f,
            animationSpec = infiniteRepeatable(
                animation = tween(1500),
                repeatMode = RepeatMode.Reverse
            ),
            label = "breathScale"
        )
        Box(
            modifier = Modifier.fillMaxWidth(),
            contentAlignment = Alignment.Center
        ) {
            Box(
                modifier = Modifier
                    .size(72.dp)
                    .scale(breathScale)
                    .clip(CircleShape)
                    .background(
                        Brush.linearGradient(
                            colors = listOf(RokuricsColors.aqua, RokuricsColors.mint),
                            start = Offset(0f, 0f),
                            end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
                        )
                    ),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    Icons.Default.PhoneAndroid,
                    contentDescription = null,
                    tint = Color.White,
                    modifier = Modifier.size(36.dp)
                )
            }
        }

        Spacer(modifier = Modifier.height(4.dp))

        // Device info card — iPhone parity: ConnectedDeviceCardView
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.40f)),
            shape = RoundedCornerShape(22.dp),
            border = androidx.compose.foundation.BorderStroke(
                0.5.dp, Color.White.copy(alpha = 0.44f)
            )
        ) {
            Column(
                modifier = Modifier.padding(20.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Text(
                    text = connectionStore.macName.ifEmpty { "Mac" },
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    fontFamily = androidx.compose.ui.text.font.FontFamily.Serif,
                    color = RokuricsColors.deepText
                )
                Text(
                    text = "${connectionStore.macHost}:${connectionStore.macPort}",
                    fontSize = 13.sp,
                    color = RokuricsColors.softText
                )

                HorizontalDivider(color = RokuricsColors.softText.copy(alpha = 0.12f))

                // Connection state row
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text("状态", fontSize = 14.sp, color = RokuricsColors.softText)
                    Text(
                        text = "已连接",
                        fontSize = 14.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = RokuricsColors.mint
                    )
                }

                // Last sync row
                val lastSuccessAt = syncState.lastSuccessfulSyncAt
                if (lastSuccessAt != null) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text("上次同步", fontSize = 14.sp, color = RokuricsColors.softText)
                        Text(
                            text = java.text.SimpleDateFormat("HH:mm:ss", java.util.Locale.CHINA)
                                .format(java.util.Date(lastSuccessAt)),
                            fontSize = 14.sp,
                            color = RokuricsColors.deepText
                        )
                    }
                }
            }
        }

        // Sync status card
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.40f)),
            shape = RoundedCornerShape(20.dp),
            border = androidx.compose.foundation.BorderStroke(
                0.5.dp, Color.White.copy(alpha = 0.44f)
            )
        ) {
            Column(
                modifier = Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Text(
                    text = "本地网络同步",
                    fontSize = 15.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = RokuricsColors.deepText
                )
                if (syncStatusText != null) {
                    Text(
                        text = syncStatusText!!,
                        fontSize = 13.sp,
                        color = RokuricsColors.softText
                    )
                }
                if (!syncState.isSyncAllowed) {
                    Text(
                        text = "退避中 · ${syncState.backoffRemainingSeconds}s 后可重试",
                        fontSize = 12.sp,
                        color = RokuricsColors.coral
                    )
                }
                if (syncState.consecutiveFailureCount > 0 && syncState.lastErrorMessage != null) {
                    Text(
                        text = "最近错误: ${syncState.lastErrorMessage}",
                        fontSize = 12.sp,
                        color = RokuricsColors.coral
                    )
                }
            }
        }

        // Action buttons — iPhone parity
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Button(
                onClick = onDisconnect,
                modifier = Modifier.weight(1f).height(48.dp),
                shape = RoundedCornerShape(24.dp),
                colors = ButtonDefaults.buttonColors(containerColor = RokuricsColors.coral)
            ) {
                Text("断开连接", fontSize = 14.sp)
            }

            Button(
                onClick = onSync,
                modifier = Modifier.weight(1f).height(48.dp),
                shape = RoundedCornerShape(24.dp),
                enabled = !isConnecting && syncState.isSyncAllowed,
                colors = ButtonDefaults.buttonColors(containerColor = RokuricsColors.aqua)
            ) {
                if (!syncState.isSyncAllowed) {
                    Text("退避中", fontSize = 14.sp)
                } else {
                    Text("立即同步", fontSize = 14.sp)
                }
            }
        }
    }
}

@Composable
private fun unpairedContent(
    host: String,
    portText: String,
    fingerprint: String,
    pairingCode: String,
    isConnecting: Boolean,
    feedbackText: String?,
    feedbackIsError: Boolean,
    onHostChange: (String) -> Unit,
    onPortChange: (String) -> Unit,
    onFingerprintChange: (String) -> Unit,
    onPairingCodeChange: (String) -> Unit,
    onPair: () -> Unit
) {
    // iPhone parity: PairingInfoFormView with glass capsule styling
    Column(verticalArrangement = Arrangement.spacedBy(14.dp)) {
        // Status icon
        Box(
            modifier = Modifier.fillMaxWidth(),
            contentAlignment = Alignment.Center
        ) {
            Box(
                modifier = Modifier
                    .size(64.dp)
                    .clip(CircleShape)
                    .background(Color.White.copy(alpha = 0.40f)),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    Icons.Default.PhonelinkErase,
                    contentDescription = null,
                    tint = RokuricsColors.softText,
                    modifier = Modifier.size(32.dp)
                )
            }
        }

        Spacer(modifier = Modifier.height(4.dp))

        // Input fields — iPhone parity: glass capsule fields
        MacConnectionInputField(
            value = host,
            onValueChange = onHostChange,
            label = "Mac 地址",
            placeholder = "主机名或 IP",
            enabled = !isConnecting
        )
        MacConnectionInputField(
            value = portText,
            onValueChange = onPortChange,
            label = "端口",
            placeholder = "8787",
            enabled = !isConnecting
        )
        MacConnectionInputField(
            value = fingerprint,
            onValueChange = onFingerprintChange,
            label = "证书指纹 (SHA256)",
            placeholder = "输入或粘贴指纹",
            enabled = !isConnecting,
            singleLine = true
        )
        MacConnectionInputField(
            value = pairingCode,
            onValueChange = onPairingCodeChange,
            label = "配对码",
            placeholder = "6 位数字",
            enabled = !isConnecting
        )

        // Feedback display — iPhone parity: MacConnectionFeedbackView
        if (feedbackText != null) {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = if (feedbackIsError)
                        RokuricsColors.coral.copy(alpha = 0.10f)
                    else
                        RokuricsColors.mint.copy(alpha = 0.10f)
                ),
                shape = RoundedCornerShape(16.dp)
            ) {
                Text(
                    text = feedbackText,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = if (feedbackIsError) RokuricsColors.coral else RokuricsColors.mint,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 12.dp)
                )
            }
        }

        // Pair button — iPhone parity: action button with gradient
        Button(
            onClick = onPair,
            modifier = Modifier.fillMaxWidth().height(50.dp),
            enabled = host.isNotEmpty() && pairingCode.length == 6 && !isConnecting,
            shape = RoundedCornerShape(25.dp),
            colors = ButtonDefaults.buttonColors(
                containerColor = RokuricsColors.aqua,
                disabledContainerColor = RokuricsColors.softText.copy(alpha = 0.30f)
            )
        ) {
            if (isConnecting) {
                CircularProgressIndicator(
                    modifier = Modifier.size(20.dp),
                    color = Color.White,
                    strokeWidth = 2.dp
                )
            } else {
                Icon(Icons.Default.Link, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(modifier = Modifier.width(8.dp))
                Text("配对", fontSize = 16.sp)
            }
        }
    }
}

@Composable
private fun MacConnectionInputField(
    value: String,
    onValueChange: (String) -> Unit,
    label: String,
    placeholder: String,
    enabled: Boolean = true,
    singleLine: Boolean = false
) {
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Text(
            text = label,
            fontSize = 12.sp,
            fontWeight = FontWeight.Bold,
            color = RokuricsColors.softText
        )
        OutlinedTextField(
            value = value,
            onValueChange = onValueChange,
            modifier = Modifier.fillMaxWidth(),
            enabled = enabled,
            singleLine = singleLine,
            placeholder = { Text(placeholder, fontSize = 14.sp, color = RokuricsColors.tertiaryText) },
            textStyle = LocalTextStyle.current.copy(
                fontSize = 14.sp,
                color = RokuricsColors.deepText
            ),
            shape = RoundedCornerShape(16.dp),
            colors = OutlinedTextFieldDefaults.colors(
                focusedBorderColor = RokuricsColors.aqua.copy(alpha = 0.5f),
                unfocusedBorderColor = RokuricsColors.glassStroke.copy(alpha = 0.42f),
                focusedContainerColor = Color.White.copy(alpha = 0.52f),
                unfocusedContainerColor = Color.White.copy(alpha = 0.36f),
                disabledContainerColor = Color.White.copy(alpha = 0.20f),
                disabledBorderColor = RokuricsColors.glassStroke.copy(alpha = 0.20f)
            )
        )
    }
}
