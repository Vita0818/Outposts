package com.rokurics.app.ui.connection

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
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
import com.rokurics.app.ui.theme.RokuricsColors
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
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

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Mac 连接") },
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
                .padding(24.dp)
                .background(Color(0xFFF0FAF8)),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Status card
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.7f)),
                shape = RoundedCornerShape(20.dp)
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Icon(
                        imageVector = if (connectionStore.isPaired) Icons.Default.PhoneAndroid else Icons.Default.PhonelinkErase,
                        contentDescription = null,
                        tint = if (connectionStore.isPaired) RokuricsColors.aqua else RokuricsColors.softText,
                        modifier = Modifier.size(48.dp)
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                    Text(
                        text = statusText,
                        fontSize = 18.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = RokuricsColors.deepText
                    )
                    if (connectionStore.isPaired) {
                        Text(
                            text = "Mac: ${connectionStore.macName}",
                            fontSize = 14.sp,
                            color = RokuricsColors.softText
                        )
                    }
                }
            }

            // Connection settings
            OutlinedTextField(
                value = host,
                onValueChange = { host = it },
                label = { Text("Mac 地址") },
                modifier = Modifier.fillMaxWidth(),
                enabled = !isConnecting
            )
            OutlinedTextField(
                value = portText,
                onValueChange = { portText = it.filter { c -> c.isDigit() }.take(5) },
                label = { Text("端口") },
                modifier = Modifier.fillMaxWidth(),
                enabled = !isConnecting
            )
            OutlinedTextField(
                value = fingerprint,
                onValueChange = { fingerprint = SecureUploadUtilities.normalizedCertificateFingerprint(it) },
                label = { Text("证书指纹 (SHA256)") },
                modifier = Modifier.fillMaxWidth(),
                enabled = !isConnecting,
                singleLine = true
            )

            if (!connectionStore.isPaired) {
                OutlinedTextField(
                    value = pairingCode,
                    onValueChange = { pairingCode = it.filter { c -> c.isDigit() }.take(6) },
                    label = { Text("配对码") },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isConnecting
                )

                Button(
                    onClick = {
                        scope.launch {
                            isConnecting = true
                            statusText = "正在配对..."
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
                                },
                                onFailure = { error ->
                                    statusText = "配对失败: ${error.message}"
                                }
                            )
                            isConnecting = false
                        }
                    },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = host.isNotEmpty() && pairingCode.length == 6 && !isConnecting
                ) {
                    if (isConnecting) {
                        CircularProgressIndicator(modifier = Modifier.size(20.dp))
                    } else {
                        Text("配对")
                    }
                }
            } else {
                // Sync status card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.5f)),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = "本地网络同步",
                                fontSize = 15.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = RokuricsColors.deepText
                            )
                            val lastSuccessAt = syncState.lastSuccessfulSyncAt
                            if (lastSuccessAt != null) {
                                val lastSync = java.text.SimpleDateFormat("HH:mm:ss", java.util.Locale.CHINA)
                                    .format(java.util.Date(lastSuccessAt))
                                Text(
                                    text = "上次: $lastSync",
                                    fontSize = 12.sp,
                                    color = RokuricsColors.softText
                                )
                            }
                        }
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

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    Button(
                        onClick = {
                            connectionStore.clearPairing()
                            statusText = "已断开"
                            pairingCode = ""
                            syncState = LocalNetworkSyncState()
                        },
                        modifier = Modifier.weight(1f),
                        colors = ButtonDefaults.buttonColors(containerColor = RokuricsColors.coral)
                    ) {
                        Text("断开连接")
                    }

                    OutlinedButton(
                        onClick = {
                            scope.launch {
                                syncStatusText = "正在同步..."
                                try {
                                    val engine = LocalNetworkSyncEngine()
                                    val result = engine.performTick("manual")
                                    syncState = syncStateStore.load()
                                    if (result.success) {
                                        syncStatusText = "同步完成 · ${result.statusText}"
                                    } else {
                                        syncStatusText = result.statusText
                                    }
                                } catch (e: Exception) {
                                    syncState = syncStateStore.load()
                                    syncStatusText = "同步失败: ${e.message}"
                                }
                            }
                        },
                        modifier = Modifier.weight(1f),
                        enabled = !isConnecting && syncState.isSyncAllowed
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
    }
}
