package com.rokurics.app.ui.home

import androidx.compose.animation.core.*
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.*
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.blur
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.scale
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.input.nestedscroll.NestedScrollConnection
import androidx.compose.ui.input.nestedscroll.NestedScrollSource
import androidx.compose.ui.input.nestedscroll.nestedScroll
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.verticalScroll
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.rokurics.app.data.ConnectionStore
import com.rokurics.app.data.StudyLibraryStore
import com.rokurics.app.data.UserPreferencesStore
import com.rokurics.app.service.RecordingManager
import com.rokurics.app.service.RokuricsRecordingState
import com.rokurics.app.ui.recording.RecordingSessionScreen
import com.rokurics.app.ui.library.RecordingLibraryScreen
import com.rokurics.app.ui.connection.MacConnectionScreen
import com.rokurics.app.ui.settings.SettingsScreen
import com.rokurics.app.ui.chat.AIChatScreen
import com.rokurics.app.ui.library.PlaybackState
import com.rokurics.app.ui.theme.RokuricsColors
import com.rokurics.app.ui.theme.adaptiveColor
import com.rokurics.app.ui.theme.adaptivePageGradientBrush
import com.rokurics.app.ui.theme.RokuricsAdaptiveMetrics
import com.rokurics.app.ui.theme.rokuricsGlassCapsule
import com.rokurics.app.ui.theme.rokuricsGlassCircle
import com.rokurics.app.ui.theme.rokuricsScaleClickable

@Composable
fun HomeScreen(
    recordingManager: RecordingManager,
    connectionStore: ConnectionStore = remember { ConnectionStore() },
    studyLibraryStore: StudyLibraryStore = remember { StudyLibraryStore() },
    userPreferencesStore: UserPreferencesStore = remember {
        UserPreferencesStore(com.rokurics.app.RokuricsApp.instance)
    }
) {
    val navController = rememberNavController()
    val currentRoute by navController.currentBackStackEntryAsState()
    val currentDestination = currentRoute?.destination?.route ?: "home"

    // Home is the hub; subpages use independent back-stack without global BottomNav
    val isOnHome = currentDestination == "home"

    val playbackState = PlaybackState.shared
    val pbRecordingId by playbackState.recordingId.collectAsState()
    val pbIsPlaying by playbackState.isPlaying.collectAsState()
    val pbTitle by playbackState.title.collectAsState()
    val pbPositionMs by playbackState.positionMs.collectAsState()
    val pbDurationMs by playbackState.durationMs.collectAsState()
    val pbIsSeeking by playbackState.isSeeking.collectAsState()
    val pbSeekFraction by playbackState.seekFraction.collectAsState()
    val pbIsActive = pbRecordingId != null

    // Position tracker for persistent mini-player
    LaunchedEffect(pbIsActive, pbIsPlaying) {
        while (pbIsActive && pbIsPlaying) {
            kotlinx.coroutines.delay(250)
            PlaybackState.shared.updatePosition()
        }
    }

    BoxWithConstraints(modifier = Modifier.fillMaxSize()) {
        val metrics = com.rokurics.app.ui.theme.RokuricsAdaptiveMetrics(maxWidth.value, maxHeight.value)
        val isCompact = metrics.widthCategory == com.rokurics.app.ui.theme.RokuricsWidthCategory.COMPACT

        if (!isCompact && isOnHome) {
            // Medium/Expanded: NavigationRail on the left
            Row(modifier = Modifier.fillMaxSize()) {
                Surface(
                    modifier = Modifier
                        .statusBarsPadding()
                        .navigationBarsPadding()
                        .width(80.dp)
                        .fillMaxHeight(),
                    color = if (isSystemInDarkTheme()) RokuricsColors.glassSurfaceDark.copy(alpha = 0.55f)
                    else Color.White.copy(alpha = 0.55f)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxHeight()
                            .padding(vertical = 12.dp),
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.spacedBy(4.dp)
                    ) {
                        Spacer(Modifier.height(8.dp))
                        RailTab("首页", Icons.Default.Home, currentDestination == "home") {
                            if (currentDestination != "home") {
                                navController.navigate("home") {
                                    popUpTo("home") { saveState = true }
                                    launchSingleTop = true; restoreState = true
                                }
                            }
                        }
                        RailTab("学习库", Icons.AutoMirrored.Filled.MenuBook, currentDestination == "library") {
                            if (currentDestination != "library") {
                                navController.navigate("library") {
                                    popUpTo("home") { saveState = true }
                                    launchSingleTop = true; restoreState = true
                                }
                            }
                        }
                        RailTab("AI 对话", Icons.AutoMirrored.Filled.Chat, currentDestination == "chat") {
                            if (currentDestination != "chat") {
                                navController.navigate("chat") {
                                    popUpTo("home") { saveState = true }
                                    launchSingleTop = true; restoreState = true
                                }
                            }
                        }
                        RailTab("Mac", Icons.Default.PhoneAndroid, currentDestination == "connection") {
                            if (currentDestination != "connection") {
                                navController.navigate("connection") {
                                    popUpTo("home") { saveState = true }
                                    launchSingleTop = true; restoreState = true
                                }
                            }
                        }
                        Spacer(Modifier.weight(1f))
                        RailTab("设置", Icons.Default.Person, currentDestination == "settings") {
                            if (currentDestination != "settings") {
                                navController.navigate("settings") {
                                    launchSingleTop = true
                                }
                            }
                        }
                        Spacer(Modifier.height(8.dp))
                    }
                }
                // Content area
                NavHost(
                    navController = navController,
                    startDestination = "home",
                    modifier = Modifier.fillMaxSize()
                ) {
                    composable("home") {
                        HomeContent(
                            recordingManager = recordingManager,
                            connectionStore = connectionStore,
                            navController = navController
                        )
                    }
                    composable("recording") {
                        RecordingSessionScreen(
                            recordingManager = recordingManager,
                            studyLibraryStore = studyLibraryStore,
                            onBack = { navController.popBackStack() }
                        )
                    }
                    composable("library") {
                        RecordingLibraryScreen(
                            recordingManager = recordingManager,
                            onBack = { navController.popBackStack() }
                        )
                    }
                    composable("connection") {
                        MacConnectionScreen(
                            onBack = { navController.popBackStack() }
                        )
                    }
                    composable("settings") {
                        SettingsScreen(
                            onBack = { navController.popBackStack() }
                        )
                    }
                    composable("chat") {
                        AIChatScreen(
                            studyLibraryStore = studyLibraryStore,
                            userPreferencesStore = userPreferencesStore,
                            onBack = { navController.popBackStack() }
                        )
                    }
                }
            }
        } else {
            // Compact or subpage: standalone NavHost without bottom dock
            NavHost(
                navController = navController,
                startDestination = "home",
                modifier = Modifier.fillMaxSize()
            ) {
                composable("home") {
                    HomeContent(
                        recordingManager = recordingManager,
                        connectionStore = connectionStore,
                        navController = navController
                    )
                }
                composable("recording") {
                    RecordingSessionScreen(
                        recordingManager = recordingManager,
                        studyLibraryStore = studyLibraryStore,
                        onBack = { navController.popBackStack() }
                    )
                }
                composable("library") {
                    RecordingLibraryScreen(
                        recordingManager = recordingManager,
                        onBack = { navController.popBackStack() }
                    )
                }
                composable("connection") {
                    MacConnectionScreen(
                        onBack = { navController.popBackStack() }
                    )
                }
                composable("settings") {
                    SettingsScreen(
                        onBack = { navController.popBackStack() }
                    )
                }
                composable("chat") {
                    AIChatScreen(
                        studyLibraryStore = studyLibraryStore,
                        userPreferencesStore = userPreferencesStore,
                        onBack = { navController.popBackStack() }
                    )
                }
            }
        }

        // Persistent mini-player overlay (Home only; subpages have their own playback bars)
        if (pbIsActive && isOnHome) {
            Box(
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .fillMaxWidth()
            ) {
                PersistentMiniPlayer(
                    title = pbTitle,
                    isPlaying = pbIsPlaying,
                    positionMs = pbPositionMs,
                    durationMs = pbDurationMs,
                    isSeeking = pbIsSeeking,
                    seekFraction = pbSeekFraction,
                    onPlayPause = { PlaybackState.shared.togglePlayPause() },
                    onSeekStart = { PlaybackState.shared.beginSeek() },
                    onSeekEnd = { fraction -> PlaybackState.shared.endSeek(fraction) },
                    onClose = { PlaybackState.shared.stopPlayback() }
                )
            }
        }
    }
}

@Composable
private fun PersistentMiniPlayer(
    title: String,
    isPlaying: Boolean,
    positionMs: Int,
    durationMs: Int,
    isSeeking: Boolean,
    seekFraction: Float,
    onPlayPause: () -> Unit,
    onSeekStart: () -> Unit,
    onSeekEnd: (Float) -> Unit,
    onClose: () -> Unit
) {
    var localSeekFraction by remember { mutableFloatStateOf(0f) }
    var isSliderDragging by remember { mutableStateOf(false) }

    val nestedScrollConnection = remember {
        object : NestedScrollConnection {
            override fun onPreScroll(available: Offset, source: NestedScrollSource): Offset {
                return if (isSliderDragging) available else Offset.Zero
            }
        }
    }

    val isDark = isSystemInDarkTheme()
    val miniPlayerFill = if (isDark) Color(0xFF0A1B1B).copy(alpha = 0.96f)
    else Color.White.copy(alpha = 0.96f)
    val miniPlayerText = adaptiveColor(RokuricsColors.deepText, RokuricsColors.deepTextDark)
    val miniPlayerSubText = adaptiveColor(RokuricsColors.softText, RokuricsColors.softTextDark)
    Surface(
        modifier = Modifier
            .fillMaxWidth()
            .navigationBarsPadding()
            .padding(horizontal = 12.dp, vertical = 4.dp),
        shape = RoundedCornerShape(20.dp),
        color = miniPlayerFill,
        border = androidx.compose.foundation.BorderStroke(0.5.dp, Color.White.copy(alpha = 0.12f))
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 10.dp, vertical = 4.dp)
                .nestedScroll(nestedScrollConnection)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                IconButton(
                    onClick = onPlayPause,
                    modifier = Modifier.size(34.dp)
                ) {
                    Icon(
                        imageVector = if (isPlaying) Icons.Default.Pause else Icons.Default.PlayArrow,
                        contentDescription = if (isPlaying) "暂停" else "播放",
                        tint = RokuricsColors.aqua,
                        modifier = Modifier.size(20.dp)
                    )
                }
                Column(
                    modifier = Modifier
                        .weight(1f)
                        .padding(horizontal = 6.dp)
                ) {
                    Text(
                        text = title,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = miniPlayerText,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    Spacer(Modifier.height(2.dp))
                    Slider(
                        value = if (isSeeking) localSeekFraction
                        else if (durationMs > 0) positionMs.toFloat() / durationMs else 0f,
                        onValueChange = { fraction ->
                            if (!isSliderDragging) isSliderDragging = true
                            localSeekFraction = fraction
                            if (!isSeeking) onSeekStart()
                        },
                        onValueChangeFinished = {
                            isSliderDragging = false
                            onSeekEnd(localSeekFraction)
                        },
                        modifier = Modifier.fillMaxWidth().height(16.dp),
                        colors = SliderDefaults.colors(
                            thumbColor = RokuricsColors.aqua,
                            activeTrackColor = RokuricsColors.aqua,
                            inactiveTrackColor = RokuricsColors.aqua.copy(alpha = 0.12f)
                        ),
                        enabled = durationMs > 0
                    )
                }
                Text(
                    text = "${formatPositionMini(positionMs)} / ${formatPositionMini(durationMs)}",
                    fontSize = 10.sp,
                    color = miniPlayerSubText,
                    modifier = Modifier.padding(end = 4.dp)
                )
                IconButton(
                    onClick = onClose,
                    modifier = Modifier.size(28.dp)
                ) {
                    Icon(
                        Icons.Default.Close,
                        contentDescription = "关闭",
                        tint = miniPlayerSubText,
                        modifier = Modifier.size(16.dp)
                    )
                }
            }
        }
    }
}

private fun formatPositionMini(ms: Int): String {
    val totalSecs = (ms / 1000).coerceAtLeast(0)
    val minutes = totalSecs / 60
    val seconds = totalSecs % 60
    return "%02d:%02d".format(minutes, seconds)
}


@Composable
private fun BottomNavTab(
    label: String,
    icon: ImageVector,
    isSelected: Boolean,
    onClick: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val activeColor = if (isDark) Color.White else RokuricsColors.aqua
    val inactiveColor = if (isDark) RokuricsColors.deepTextDark.copy(alpha = 0.72f) else RokuricsColors.softText
    Column(
        modifier = Modifier
            .rokuricsScaleClickable(onClick = onClick)
            .clip(RoundedCornerShape(12.dp))
            .background(if (isSelected) activeColor.copy(alpha = 0.12f) else Color.Transparent)
            .padding(horizontal = 8.dp, vertical = 5.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(2.dp)
    ) {
        Icon(
            icon,
            contentDescription = label,
            tint = if (isSelected) activeColor else inactiveColor,
            modifier = Modifier.size(20.dp)
        )
        Text(
            text = label,
            fontSize = 10.sp,
            fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium,
            color = if (isSelected) activeColor else inactiveColor
        )
    }
}

@Composable
private fun RailTab(
    label: String,
    icon: ImageVector,
    isSelected: Boolean,
    onClick: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .rokuricsScaleClickable(onClick = onClick)
            .padding(vertical = 8.dp, horizontal = 4.dp)
            .clip(RoundedCornerShape(14.dp))
            .background(if (isSelected) RokuricsColors.aqua.copy(alpha = 0.12f) else Color.Transparent)
            .padding(vertical = 10.dp, horizontal = 4.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(3.dp)
    ) {
        Icon(
            icon,
            contentDescription = label,
            tint = if (isSelected) RokuricsColors.aqua else RokuricsColors.softText,
            modifier = Modifier.size(22.dp)
        )
        Text(
            text = label,
            fontSize = 11.sp,
            fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium,
            color = if (isSelected) RokuricsColors.aqua else RokuricsColors.softText
        )
    }
}

@Composable
fun HomeContent(
    recordingManager: RecordingManager,
    connectionStore: ConnectionStore,
    navController: NavHostController
) {
    val state by recordingManager.state.collectAsState()
    val elapsedSeconds by recordingManager.elapsedSeconds.collectAsState()
    val isMacPaired = connectionStore.isPaired

    val verticalScroll = rememberScrollState()

    BoxWithConstraints(
        modifier = Modifier
            .fillMaxSize()
            .background(adaptivePageGradientBrush())
    ) {
        val metrics = RokuricsAdaptiveMetrics.from(maxWidth, maxHeight)
        val isWide = metrics.isWide
        val isShort = metrics.isShort
        val headerScale = metrics.headerScale
        val orbScale = metrics.orbScale
        val dashboardScale = metrics.dashboardScale
        val contentMaxWidth = metrics.contentMaxWidth

        // Ambient background bubbles (iPhone parity: RokuricsAmbientBubble × 3)
        AmbientBubble(
            sizeDp = if (isWide) 210f else 150f,
            colors = listOf(RokuricsColors.paleAqua, RokuricsColors.mint),
            alpha = 0.30f,
            offsetXFraction = -0.22f,
            offsetYFraction = -0.26f,
            screenWidthDp = metrics.widthDp,
            screenHeightDp = metrics.heightDp
        )
        AmbientBubble(
            sizeDp = if (isWide) 260f else 190f,
            colors = listOf(RokuricsColors.skyCyan, RokuricsColors.mistGreen),
            alpha = 0.22f,
            offsetXFraction = 0.30f,
            offsetYFraction = -0.12f,
            screenWidthDp = metrics.widthDp,
            screenHeightDp = metrics.heightDp
        )
        AmbientBubble(
            sizeDp = if (isWide) 230f else 170f,
            colors = listOf(RokuricsColors.mint, RokuricsColors.aqua),
            alpha = 0.18f,
            offsetXFraction = 0.24f,
            offsetYFraction = 0.32f,
            screenWidthDp = metrics.widthDp,
            screenHeightDp = metrics.heightDp
        )

        // Centered content column (iPhone parity: VStack with .frame(maxWidth: homeMaxWidth))
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(verticalScroll)
                .padding(horizontal = metrics.horizontalPadding)
                .statusBarsPadding()
                .then(if (isWide) Modifier.widthIn(max = contentMaxWidth) else Modifier),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(if (isWide) 24.dp else 18.dp))

            // Header — iPhone parity: HStack "Rokurics" + profile avatar button
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Rokurics",
                    fontFamily = androidx.compose.ui.text.font.FontFamily.Serif,
                    fontSize = (39 * headerScale).sp,
                    fontWeight = FontWeight.SemiBold,
                    color = adaptiveColor(RokuricsColors.deepText, RokuricsColors.deepTextDark)
                )
                SettingsAvatarButton(
                    scale = headerScale,
                    onClick = { navController.navigate("settings") }
                )
            }

            Spacer(modifier = Modifier.height(if (isWide) 34.dp else 22.dp))

            // Recording Orb — iPhone parity: central hero element
            RecordingOrb(
                state = state,
                elapsedSeconds = elapsedSeconds,
                scale = orbScale,
                onClick = { navController.navigate("recording") }
            )

            Spacer(modifier = Modifier.height(if (metrics.isPadWidth) 32.dp else 20.dp))

            // Navigation card — iPhone parity: RokuricsHomeNavigationCard
            HomeNavigationCard(
                isMacPaired = isMacPaired,
                scale = dashboardScale,
                onOpenLibrary = { navController.navigate("library") },
                onOpenAIChat = { navController.navigate("chat") },
                onOpenConnection = { navController.navigate("connection") }
            )

            // Bottom spacer (iPhone parity: homeBottomPadding)
            Spacer(modifier = Modifier.height(metrics.homeBottomPadding))

            Spacer(modifier = Modifier.navigationBarsPadding())
            Spacer(modifier = Modifier.height(16.dp))
        }
    }
}

@Composable
private fun SettingsAvatarButton(
    scale: Float,
    onClick: () -> Unit
) {
    Box(
        modifier = Modifier
            .size((46 * scale).dp)
            .rokuricsGlassCircle(fillOpacity = 0.58f, strokeOpacity = 0.78f, shadowOpacity = 0.18f, shadowRadius = 14.dp, fillColor = if (isSystemInDarkTheme()) RokuricsColors.glassSurfaceDark else Color.White)
            .rokuricsScaleClickable(onClick = onClick),
        contentAlignment = Alignment.Center
    ) {
        Icon(
            imageVector = Icons.Default.Person,
            contentDescription = "打开设置",
            tint = RokuricsColors.aqua,
            modifier = Modifier.size((46 * scale * 0.88f).dp)
        )
    }
}

// ── Dashboard Cards ────────────────────────────────────────────────

@Composable
fun DashboardStatsCard(
    totalCount: Int,
    totalDuration: Double,
    uploadedCount: Int,
    transcriptionCount: Int,
    noteCount: Int,
    typeCategoryCount: Int,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val cardBg = if (isDark) RokuricsColors.glassSurfaceDark.copy(alpha = 0.55f)
    else Color.White.copy(alpha = 0.52f)
    val cardBorderColor = Color.White.copy(alpha = if (isDark) 0.08f else 0.22f)
    val cardTextColor = if (isDark) RokuricsColors.deepTextDark else RokuricsColors.deepText
    Card(
        modifier = modifier,
        colors = CardDefaults.cardColors(containerColor = cardBg),
        shape = RoundedCornerShape(22.dp),
        border = androidx.compose.foundation.BorderStroke(0.5.dp, cardBorderColor)
    ) {
        Column(modifier = Modifier.padding(20.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Surface(
                    modifier = Modifier.size(32.dp),
                    shape = RoundedCornerShape(10.dp),
                    color = RokuricsColors.aqua.copy(alpha = 0.15f)
                ) {
                    Icon(
                        Icons.Default.BarChart,
                        contentDescription = null,
                        tint = RokuricsColors.aqua,
                        modifier = Modifier.padding(6.dp)
                    )
                }
                Spacer(modifier = Modifier.width(10.dp))
                Text(
                    text = "学习概览",
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold,
                    color = cardTextColor
                )
            }

            Spacer(modifier = Modifier.height(16.dp))

            // First row: Total + Duration
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                StatItem(
                    value = "$totalCount",
                    label = "录音总数",
                    color = RokuricsColors.aqua,
                    modifier = Modifier.weight(1f)
                )
                StatItem(
                    value = formatDurationShort(totalDuration),
                    label = "总时长",
                    color = RokuricsColors.mint,
                    modifier = Modifier.weight(1f)
                )
                StatItem(
                    value = "$typeCategoryCount",
                    label = "门类",
                    color = RokuricsColors.softTeal,
                    modifier = Modifier.weight(1f)
                )
            }

            Spacer(modifier = Modifier.height(14.dp))

            // Second row: Status chips
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                DashboardStatusChip(
                    icon = Icons.Default.CloudDone,
                    label = "已上传",
                    count = uploadedCount,
                    color = RokuricsColors.mint
                )
                DashboardStatusChip(
                    icon = Icons.AutoMirrored.Filled.TextSnippet,
                    label = "已转录",
                    count = transcriptionCount,
                    color = RokuricsColors.softTeal
                )
                DashboardStatusChip(
                    icon = Icons.Default.AutoAwesome,
                    label = "已整理",
                    count = noteCount,
                    color = RokuricsColors.aqua
                )
            }
        }
    }
}

@Composable
fun DashboardConnectionCard(
    isPaired: Boolean,
    macName: String,
    macHost: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val cardBg = if (isDark) RokuricsColors.glassSurfaceDark.copy(alpha = 0.55f)
    else Color.White.copy(alpha = 0.52f)
    val cardBorderColor = Color.White.copy(alpha = if (isDark) 0.08f else 0.22f)
    Card(
        modifier = modifier.clickable(onClick = onClick),
        colors = CardDefaults.cardColors(containerColor = cardBg),
        shape = RoundedCornerShape(22.dp),
        border = androidx.compose.foundation.BorderStroke(0.5.dp, cardBorderColor)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(20.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Surface(
                modifier = Modifier.size(32.dp),
                shape = RoundedCornerShape(10.dp),
                color = if (isPaired) RokuricsColors.mint.copy(alpha = 0.15f)
                else RokuricsColors.softText.copy(alpha = 0.15f)
            ) {
                Icon(
                    imageVector = if (isPaired) Icons.Default.PhoneAndroid else Icons.Default.PhonelinkErase,
                    contentDescription = null,
                    tint = if (isPaired) RokuricsColors.mint else RokuricsColors.softText,
                    modifier = Modifier.padding(6.dp)
                )
            }
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = if (isPaired) "已连接 Mac" else "Mac 连接",
                    fontSize = 15.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = RokuricsColors.deepText
                )
                if (isPaired && macName.isNotEmpty()) {
                    Text(
                        text = macName.ifEmpty { macHost },
                        fontSize = 13.sp,
                        color = RokuricsColors.softText,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                } else {
                    Text(
                        text = "点击配对或管理连接",
                        fontSize = 13.sp,
                        color = RokuricsColors.softText
                    )
                }
            }
            Icon(
                Icons.Default.ChevronRight,
                contentDescription = null,
                tint = RokuricsColors.softText
            )
        }
    }
}

@Composable
fun StatItem(
    value: String,
    label: String,
    color: Color,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = value,
            fontSize = 28.sp,
            fontWeight = FontWeight.Bold,
            color = color,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )
        Spacer(modifier = Modifier.height(2.dp))
        Text(
            text = label,
            fontSize = 12.sp,
            fontWeight = FontWeight.Medium,
            color = RokuricsColors.softText
        )
    }
}

@Composable
fun DashboardStatusChip(
    icon: ImageVector,
    label: String,
    count: Int,
    color: Color
) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = color.copy(alpha = 0.7f),
            modifier = Modifier.size(16.dp)
        )
        Spacer(modifier = Modifier.width(4.dp))
        Text(
            text = label,
            fontSize = 12.sp,
            fontWeight = FontWeight.Medium,
            color = RokuricsColors.softText
        )
        Spacer(modifier = Modifier.width(4.dp))
        Text(
            text = "$count",
            fontSize = 15.sp,
            fontWeight = FontWeight.Bold,
            color = color
        )
    }
}

private fun formatDurationShort(seconds: Double): String {
    val totalSecs = seconds.toLong()
    val hours = totalSecs / 3600
    val mins = (totalSecs % 3600) / 60
    return if (hours > 0) "${hours}h${mins}m" else "${mins}m"
}

@Composable
fun TransferQueueCard(pendingUploadCount: Int) {
    val isDark = isSystemInDarkTheme()
    val cardBg = if (isDark) RokuricsColors.glassSurfaceDark.copy(alpha = 0.55f)
    else Color.White.copy(alpha = 0.52f)
    val cardBorderColor = Color.White.copy(alpha = if (isDark) 0.08f else 0.22f)
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = cardBg),
        shape = RoundedCornerShape(20.dp),
        border = androidx.compose.foundation.BorderStroke(0.5.dp, cardBorderColor)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                imageVector = Icons.Default.SwapHoriz,
                contentDescription = null,
                tint = RokuricsColors.aqua,
                modifier = Modifier.size(24.dp)
            )
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "本地传输队列",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = RokuricsColors.deepText
                )
                Text(
                    text = "待传输 · $pendingUploadCount 项",
                    fontSize = 12.sp,
                    color = RokuricsColors.softText
                )
            }
        }
    }
}

@Composable
fun RecordingOrb(
    state: RokuricsRecordingState,
    elapsedSeconds: Double,
    scale: Float = 1f,
    onClick: () -> Unit
) {
    val effectiveScale = scale.coerceAtLeast(0.1f)

    val infiniteTransition = rememberInfiniteTransition(label = "breathe")
    val breatheScale by infiniteTransition.animateFloat(
        initialValue = 1f,
        targetValue = 1.022f,
        animationSpec = infiniteRepeatable(
            animation = tween(2400, easing = EaseInOutCubic),
            repeatMode = RepeatMode.Reverse
        ),
        label = "breathe"
    )

    val isActiveSession = when (state) {
        RokuricsRecordingState.REQUESTING_PERMISSION, RokuricsRecordingState.CONFIGURING_SESSION,
        RokuricsRecordingState.RECORDING, RokuricsRecordingState.PAUSED,
        RokuricsRecordingState.STOPPING, RokuricsRecordingState.SAVING -> true
        else -> false
    }

    val displayText = when (state) {
        RokuricsRecordingState.REQUESTING_PERMISSION, RokuricsRecordingState.CONFIGURING_SESSION,
        RokuricsRecordingState.STOPPING, RokuricsRecordingState.SAVING -> "..."
        RokuricsRecordingState.RECORDING, RokuricsRecordingState.PAUSED -> {
            val totalSeconds = elapsedSeconds.toInt().coerceAtLeast(0)
            val mins = totalSeconds / 60
            val secs = totalSeconds % 60
            String.format("%02d:%02d", mins, secs)
        }
        else -> ""
    }

    val isIdle = !isActiveSession && state != RokuricsRecordingState.FILING
    val isBreathing = isActiveSession

    // Orbiting bubble rotation (Apple parity: orbitDegrees from TimelineView, 160s period)
    val orbitTransition = rememberInfiniteTransition(label = "orbit")
    val orbitAngle by orbitTransition.animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec = infiniteRepeatable(
            animation = tween(160_000, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "orbitAngle"
    )

    Box(
        modifier = Modifier
            .size((272 * effectiveScale).dp)
            .scale(if (isBreathing) breatheScale else 1f),
        contentAlignment = Alignment.Center
    ) {
        // Orbiting bubbles (Apple parity: RokuricsOrbBubble × 4)
        // Outer rotation layer
        Box(
            modifier = Modifier
                .size((272 * effectiveScale).dp)
                .graphicsLayer { rotationZ = orbitAngle },
            contentAlignment = Alignment.Center
        ) {
            val bubbleBreathScale = if (isBreathing) 1.035f else 0.985f
            // Bubble 1 — top-left
            OrbitingBubble(
                size = 88 * effectiveScale,
                colors = listOf(RokuricsColors.mint, RokuricsColors.paleAqua),
                alpha = 0.42f,
                offsetX = -94 * effectiveScale,
                offsetY = -66 * effectiveScale,
                counterRotation = -orbitAngle,
                breathingScale = bubbleBreathScale
            )
            // Bubble 2 — top-right
            OrbitingBubble(
                size = 76 * effectiveScale,
                colors = listOf(RokuricsColors.skyCyan, RokuricsColors.mistGreen),
                alpha = 0.32f,
                offsetX = 100 * effectiveScale,
                offsetY = -54 * effectiveScale,
                counterRotation = -orbitAngle,
                breathingScale = bubbleBreathScale
            )
            // Bubble 3 — bottom-right
            OrbitingBubble(
                size = 74 * effectiveScale,
                colors = listOf(RokuricsColors.aqua, RokuricsColors.paleAqua),
                alpha = 0.30f,
                offsetX = 90 * effectiveScale,
                offsetY = 76 * effectiveScale,
                counterRotation = -orbitAngle,
                breathingScale = bubbleBreathScale
            )
            // Bubble 4 — bottom-left
            OrbitingBubble(
                size = 68 * effectiveScale,
                colors = listOf(RokuricsColors.mistGreen, RokuricsColors.mint),
                alpha = 0.34f,
                offsetX = -104 * effectiveScale,
                offsetY = 74 * effectiveScale,
                counterRotation = -orbitAngle,
                breathingScale = bubbleBreathScale
            )
        }

        // Sound ripple rings (Apple parity: RokuricsSoundRipple)
        if (isActiveSession || isIdle) {
            val rippleColor = if (state == RokuricsRecordingState.RECORDING) RokuricsColors.coral
            else if (state == RokuricsRecordingState.PAUSED) RokuricsColors.softTeal
            else RokuricsColors.aqua

            Box(
                modifier = Modifier
                    .size((238 * effectiveScale).dp)
                    .clip(CircleShape)
                    .background(Color.Transparent)
                    .scale(breatheScale + 0.023f),
                contentAlignment = Alignment.Center
            ) {
                Canvas(Modifier.fillMaxSize()) {
                    val strokeWidth = 1.4f * density * effectiveScale
                    drawCircle(
                        color = rippleColor.copy(alpha = if (isActiveSession) 0.18f else 0.08f),
                        radius = size.minDimension / 2f - strokeWidth / 2f,
                        style = Stroke(width = strokeWidth)
                    )
                }
            }

            Box(
                modifier = Modifier
                    .size((202 * effectiveScale).dp)
                    .clip(CircleShape)
                    .background(Color.Transparent)
                    .scale(breatheScale - 0.012f),
                contentAlignment = Alignment.Center
            ) {
                Canvas(Modifier.fillMaxSize()) {
                    val strokeWidth = 1.2f * density * effectiveScale
                    drawCircle(
                        color = rippleColor.copy(alpha = if (isActiveSession) 0.13f else 0.06f),
                        radius = size.minDimension / 2f - strokeWidth / 2f,
                        style = Stroke(width = strokeWidth)
                    )
                }
            }
        }

        // Dark outer ring (Apple parity: thick dark ring for depth, ~24dp each side)
        if (isIdle) {
            Box(
                modifier = Modifier
                    .size((238 * effectiveScale).dp)
                    .clip(CircleShape)
                    .background(adaptiveColor(RokuricsColors.glassSurface.copy(alpha = 0.82f), Color(0xFF152222).copy(alpha = 0.82f)))
                    .shadow(
                        elevation = (8 * effectiveScale).dp,
                        spotColor = Color.Black.copy(alpha = 0.20f),
                        ambientColor = Color.Black.copy(alpha = 0.10f)
                    )
            )
        }

        // Main orb circle with glass styling (Apple parity)
        val orbGradient = if (isSystemInDarkTheme()) RokuricsColors.actionGradientDark
            else RokuricsColors.actionGradientLight
        Box(
            modifier = Modifier
                .size((190 * effectiveScale).dp)
                .clip(CircleShape)
                .background(
                    Brush.linearGradient(
                        colors = orbGradient,
                        start = Offset(0f, 0f),
                        end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
                    )
                )
                .rokuricsScaleClickable(onClick = onClick),
            contentAlignment = Alignment.Center
        ) {
            // Glass highlight overlay
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .clip(CircleShape)
                    .background(
                        Brush.radialGradient(
                            colors = listOf(
                                Color.White.copy(alpha = 0.28f),
                                Color.White.copy(alpha = 0.08f),
                                Color.Transparent
                            ),
                            center = Offset(80f * effectiveScale, 40f * effectiveScale),
                            radius = 140f * effectiveScale
                        )
                    )
            )

            // Stroke ring (Apple parity)
            Canvas(Modifier.fillMaxSize()) {
                val strokeWidth = if (isActiveSession) 2.2f * density * effectiveScale
                else 1.2f * density * effectiveScale
                drawCircle(
                    color = Color.White.copy(alpha = 0.34f),
                    radius = size.minDimension / 2f - strokeWidth / 2f,
                    style = Stroke(width = strokeWidth)
                )
            }

            // Center content
            if (isIdle) {
                // Plus glyph (Apple parity: RokuricsPlusGlyph - two Capsule shapes)
                val glyphSize = (74 * effectiveScale).dp
                val thickness = (10 * effectiveScale).dp
                Box(
                    modifier = Modifier.size(glyphSize),
                    contentAlignment = Alignment.Center
                ) {
                    Box(
                        modifier = Modifier
                            .size(glyphSize, thickness)
                            .clip(RoundedCornerShape(50))
                            .background(Color.White.copy(alpha = 0.97f))
                    )
                    Box(
                        modifier = Modifier
                            .size(thickness, glyphSize)
                            .clip(RoundedCornerShape(50))
                            .background(Color.White.copy(alpha = 0.97f))
                    )
                }
            } else if (displayText.isNotEmpty()) {
                Text(
                    text = displayText,
                    fontSize = ((if (displayText.length > 5) 34 else 44) * effectiveScale).sp,
                    fontWeight = FontWeight.Bold,
                    color = Color.White.copy(alpha = 0.97f),
                    textAlign = TextAlign.Center
                )
            }
        }
    }
}

@Composable
private fun OrbitingBubble(
    size: Float,
    colors: List<Color>,
    alpha: Float,
    offsetX: Float,
    offsetY: Float,
    counterRotation: Float,
    breathingScale: Float = 1f
) {
    Box(
        modifier = Modifier
            .offset(x = offsetX.dp, y = offsetY.dp)
            .size(size.dp)
            .scale(breathingScale)
            .graphicsLayer { rotationZ = counterRotation }
            .clip(CircleShape)
            .background(
                Brush.linearGradient(
                    colors = colors.map { it.copy(alpha = alpha) },
                    start = Offset(0f, 0f),
                    end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
                )
            )
            .shadow(
                elevation = 8.dp,
                spotColor = RokuricsColors.deepText.copy(alpha = 0.06f)
            ),
        contentAlignment = Alignment.Center
    ) {
        // Radial gradient highlight (Apple parity: radial gradient overlay)
        Box(
            modifier = Modifier
                .fillMaxSize()
                .clip(CircleShape)
                .background(
                    Brush.radialGradient(
                        colors = listOf(
                            Color.White.copy(alpha = 0.18f),
                            Color.White.copy(alpha = 0.04f),
                            Color.Transparent
                        ),
                        center = Offset(size.dp.value * 0.25f, size.dp.value * 0.25f),
                        radius = size.dp.value * 0.72f
                    )
                )
        )

        // Subtle stroke ring (Apple parity)
        Canvas(Modifier.fillMaxSize()) {
            val pxSize = size * density
            drawCircle(
                color = Color.White.copy(alpha = 0.22f),
                radius = pxSize / 2f - 0.5f * density,
                style = Stroke(width = 0.8f * density)
            )
        }
    }
}

@Composable
fun HomeNavigationCard(
    isMacPaired: Boolean,
    scale: Float = 1f,
    onOpenLibrary: () -> Unit,
    onOpenAIChat: () -> Unit,
    onOpenConnection: () -> Unit
) {
    // iPhone parity: RokuricsHomeNavigationCard with rokuricsLiquidGlassCard styling
    val isDark = isSystemInDarkTheme()
    val navCardFill = if (isDark) RokuricsColors.glassSurfaceDark.copy(alpha = 0.45f)
        else Color.White.copy(alpha = 0.40f)
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .height((104 * scale).dp)
            .shadow(
                elevation = (20 * scale).dp,
                spotColor = RokuricsColors.shadow.copy(alpha = 0.12f)
            )
            .clip(RoundedCornerShape((30 * scale).dp))
            .background(navCardFill)
            .background(Color.White.copy(alpha = if (isDark) 0.04f else 0.12f), RoundedCornerShape((30 * scale).dp))
            .drawBehind {
                drawRoundRect(
                    brush = Brush.linearGradient(
                        colors = listOf(
                            Color.White.copy(alpha = if (isDark) 0.08f else 0.44f),
                            Color.White.copy(alpha = if (isDark) 0.02f else 0.06f),
                            RokuricsColors.aqua.copy(alpha = if (isDark) 0.06f else 0.12f)
                        ),
                        start = Offset(0f, 0f),
                        end = Offset(size.width, size.height)
                    ),
                    cornerRadius = CornerRadius((30 * scale).dp.toPx()),
                    style = Stroke(width = 2.dp.toPx())
                )
            },
        verticalAlignment = Alignment.CenterVertically
    ) {
        HomeNavButton(
            modifier = Modifier.weight(1f),
            title = "学习库",
            icon = Icons.AutoMirrored.Filled.MenuBook,
            tint = RokuricsColors.aqua,
            scale = scale,
            onClick = onOpenLibrary
        )
        VerticalDivider(
            modifier = Modifier.height((54 * scale).dp),
            color = RokuricsColors.softText.copy(alpha = 0.14f)
        )
        HomeNavButton(
            modifier = Modifier.weight(1f),
            title = "AI 对话",
            icon = Icons.AutoMirrored.Filled.Chat,
            tint = RokuricsColors.mint,
            scale = scale,
            onClick = onOpenAIChat
        )
        VerticalDivider(
            modifier = Modifier.height((54 * scale).dp),
            color = RokuricsColors.softText.copy(alpha = 0.14f)
        )
        HomeNavButton(
            modifier = Modifier.weight(1f),
            title = "Mac 连接",
            icon = if (isMacPaired) Icons.Default.PhoneAndroid else Icons.Default.PhonelinkErase,
            tint = RokuricsColors.softTeal,
            scale = scale,
            onClick = onOpenConnection
        )
    }
}

@Composable
fun HomeNavButton(
    modifier: Modifier = Modifier,
    title: String,
    icon: ImageVector,
    tint: Color,
    scale: Float = 1f,
    onClick: () -> Unit
) {
    Column(
        modifier = modifier
            .fillMaxHeight()
            .rokuricsScaleClickable(onClick = onClick),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Icon(
            imageVector = icon,
            contentDescription = title,
            tint = tint,
            modifier = Modifier.size((27 * scale).dp)
        )
        Spacer(modifier = Modifier.height((9 * scale).dp))
        Text(
            text = title,
            fontSize = (13 * scale).sp,
            fontWeight = FontWeight.SemiBold,
            color = RokuricsColors.deepText
        )
    }
}

@Composable
private fun AmbientBubble(
    sizeDp: Float,
    colors: List<Color>,
    alpha: Float,
    offsetXFraction: Float,
    offsetYFraction: Float,
    screenWidthDp: Float,
    screenHeightDp: Float
) {
    val offsetXDp = (screenWidthDp * offsetXFraction).dp
    val offsetYDp = (screenHeightDp * offsetYFraction).dp

    // iPhone parity: RokuricsAmbientBubble with glass circle styling + blur
    Box(
        modifier = Modifier
            .offset(x = offsetXDp, y = offsetYDp)
            .size(sizeDp.dp)
            .blur(radius = 0.5.dp)
            .shadow(
                elevation = ((sizeDp * 0.08f).coerceIn(4f, 18f)).dp,
                shape = CircleShape
            )
            .clip(CircleShape)
            .background(
                Brush.linearGradient(
                    colors = colors.map { it.copy(alpha = alpha) },
                    start = Offset(0f, 0f),
                    end = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
                )
            )
            .background(Color.White.copy(alpha = 0.08f), CircleShape)
    )
}
