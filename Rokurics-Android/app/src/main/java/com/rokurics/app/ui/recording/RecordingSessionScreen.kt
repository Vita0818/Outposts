package com.rokurics.app.ui.recording

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLifecycleOwner
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.content.ContextCompat
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import com.rokurics.app.data.StudyLibraryStore
import com.rokurics.app.domain.model.StudyFilingLevel
import com.rokurics.app.domain.model.StudyFilingPath
import com.rokurics.app.domain.model.StudyFolderLevel
import com.rokurics.app.service.RecordingManager
import com.rokurics.app.service.RokuricsRecordingState
import com.rokurics.app.ui.theme.RokuricsAdaptiveMetrics
import com.rokurics.app.ui.theme.RokuricsColors
import com.rokurics.app.ui.theme.rokuricsScaleClickable
import kotlinx.coroutines.delay

private val LOW_POWER_INACTIVITY_DELAY_MS = 5_000L

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun RecordingSessionScreen(
    recordingManager: RecordingManager,
    studyLibraryStore: StudyLibraryStore,
    onBack: () -> Unit
) {
    val state by recordingManager.state.collectAsState()
    val elapsedSeconds by recordingManager.elapsedSeconds.collectAsState()
    val amplitudeLevel by recordingManager.amplitudeLevel.collectAsState()
    val statusMessage by recordingManager.statusMessage.collectAsState()

    var filingType by remember { mutableStateOf("") }
    var filingSubject by remember { mutableStateOf("") }
    var filingChapter by remember { mutableStateOf("") }
    var filingTopic by remember { mutableStateOf("") }

    val context = LocalContext.current
    val isFiling = state == RokuricsRecordingState.FILING

    // Low-power display mode
    var isLowPowerMode by remember { mutableStateOf(false) }
    var lowPowerMinuteText by remember { mutableStateOf("00") }
    var userInteractionTick by remember { mutableLongStateOf(0L) }

    val lifecycleOwner = LocalLifecycleOwner.current
    var isAppActive by remember { mutableStateOf(true) }

    DisposableEffect(lifecycleOwner) {
        val observer = LifecycleEventObserver { _, event ->
            isAppActive = event == Lifecycle.Event.ON_RESUME
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose { lifecycleOwner.lifecycle.removeObserver(observer) }
    }

    fun canEnterLowPowerMode(): Boolean {
        return state == RokuricsRecordingState.RECORDING
                && isAppActive
                && !isFiling
                && state != RokuricsRecordingState.FAILED
                && state != RokuricsRecordingState.PERMISSION_DENIED
                && state != RokuricsRecordingState.NOTIFICATION_PERMISSION_DENIED
    }

    fun recordUserInteraction() {
        userInteractionTick = System.currentTimeMillis()
        if (isLowPowerMode) {
            isLowPowerMode = false
            userInteractionTick = System.currentTimeMillis()
        }
    }

    // Low-power mode entry timer
    LaunchedEffect(state, userInteractionTick, isAppActive, isFiling) {
        if (!canEnterLowPowerMode() || isLowPowerMode) return@LaunchedEffect
        delay(LOW_POWER_INACTIVITY_DELAY_MS)
        if (canEnterLowPowerMode()) {
            val totalSecs = elapsedSeconds.toInt().coerceAtLeast(0)
            lowPowerMinuteText = String.format("%02d", totalSecs / 60)
            isLowPowerMode = true
        }
    }

    // Update low-power minute text
    LaunchedEffect(isLowPowerMode, elapsedSeconds) {
        if (isLowPowerMode) {
            val totalSecs = elapsedSeconds.toInt().coerceAtLeast(0)
            lowPowerMinuteText = String.format("%02d", totalSecs / 60)
        }
    }

    // Exiting low-power mode when scene goes to background or state changes
    LaunchedEffect(state, isAppActive) {
        if (!canEnterLowPowerMode()) {
            isLowPowerMode = false
        }
    }

    // Exit low-power mode on scene resume (refresh timer)
    LaunchedEffect(isAppActive) {
        if (isAppActive && isLowPowerMode) {
            isLowPowerMode = false
        }
    }

    // POST_NOTIFICATIONS permission launcher (Android 13+)
    val notificationPermissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestPermission()
    ) { granted ->
        recordUserInteraction()
        if (granted) {
            recordingManager.startRecording()
        } else {
            recordingManager.startRecording()
        }
    }

    fun startRecordingWithPermissionRequest() {
        recordUserInteraction()
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            val hasPermission = ContextCompat.checkSelfPermission(
                context, Manifest.permission.POST_NOTIFICATIONS
            ) == PackageManager.PERMISSION_GRANTED
            if (!hasPermission) {
                notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
                return
            }
        }
        recordingManager.startRecording()
    }

    fun handleBack() {
        when (state) {
            RokuricsRecordingState.RECORDING, RokuricsRecordingState.PAUSED -> {
                recordingManager.stopRecording()
            }
            RokuricsRecordingState.FILING -> {
                recordingManager.finalizeRecordingDirectSave()
            }
            else -> {}
        }
        onBack()
    }

    // Upload queue status for feedback after save
    val uploadQueueCount = recordingManager.pendingUploadCount

    LaunchedEffect(state) {
        if (state == RokuricsRecordingState.SAVED) {
            // Brief delay so user can see save + upload enqueue confirmation
            delay(1200)
            onBack()
        }
    }

    // ── Low-power display mode ─────────────────────────────────────
    if (isLowPowerMode) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Black)
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null
                ) {
                    isLowPowerMode = false
                },
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = lowPowerMinuteText,
                fontSize = 104.sp,
                fontWeight = FontWeight.Bold,
                color = Color.White,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxSize().wrapContentSize(Alignment.Center)
            )
        }
        return
    }

    // ── Normal recording content ───────────────────────────────────
    BoxWithConstraints(
        modifier = Modifier
            .fillMaxSize()
            .background(
                Brush.verticalGradient(
                    colors = listOf(Color(0xFFF0FAF8), Color(0xFFE8F8F4))
                )
            )
    ) {
        val metrics = RokuricsAdaptiveMetrics(maxWidth.value, maxHeight.value)
        val horzPadding = metrics.horizontalPadding
        val timerFontSize = if (metrics.isPadWidth) 96.sp else 78.sp
        val timerPaddingV = if (metrics.isPadWidth) 48.dp else 36.dp

        // Ambient background circles (Apple parity)
        Box(
            modifier = Modifier
                .offset(x = (-120).dp, y = (-200).dp)
                .size(if (metrics.isPadWidth) 280.dp else 220.dp)
                .clip(CircleShape)
                .background(RokuricsColors.mint.copy(alpha = 0.14f))
        )
        Box(
            modifier = Modifier
                .offset(x = 150.dp, y = 180.dp)
                .size(if (metrics.isPadWidth) 320.dp else 260.dp)
                .clip(CircleShape)
                .background(RokuricsColors.skyCyan.copy(alpha = 0.10f))
        )

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = horzPadding)
                .statusBarsPadding(),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(if (metrics.isPadWidth) 24.dp else 18.dp))

            // Header — back button only
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Box(
                    modifier = Modifier
                        .size(44.dp)
                        .clip(CircleShape)
                        .background(Color.White.copy(alpha = 0.46f))
                        .clickable { handleBack() },
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        Icons.AutoMirrored.Filled.ArrowBack,
                        contentDescription = "返回首页",
                        tint = RokuricsColors.deepText,
                        modifier = Modifier.size(22.dp)
                    )
                }

                Spacer(Modifier.weight(1f))
                // Empty right side for visual balance
                Box(Modifier.size(44.dp))
            }

            Spacer(modifier = Modifier.weight(0.5f))

            // Timer card — glass style (Apple parity)
            val totalSeconds = elapsedSeconds.toInt().coerceAtLeast(0)
            val mins = totalSeconds / 60
            val secs = totalSeconds % 60

            // Paused blinking effect (Apple parity: .rokuricsPausedBlinking)
            val isPaused = state == RokuricsRecordingState.PAUSED
            val blinkTransition = rememberInfiniteTransition(label = "pauseBlink")
            val timerAlpha by blinkTransition.animateFloat(
                initialValue = 1f,
                targetValue = if (isPaused) 0.35f else 1f,
                animationSpec = infiniteRepeatable(
                    animation = tween(800, easing = LinearEasing),
                    repeatMode = RepeatMode.Reverse
                ),
                label = "timerBlink"
            )

            Surface(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(34.dp),
                color = Color.White.copy(alpha = 0.36f),
                shadowElevation = 12.dp
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = timerPaddingV),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(
                        text = String.format("%02d:%02d", mins, secs),
                        fontSize = timerFontSize,
                        fontWeight = FontWeight.Bold,
                        color = RokuricsColors.deepText.copy(alpha = if (isPaused) timerAlpha else 1f),
                        textAlign = TextAlign.Center
                    )
                    if (state == RokuricsRecordingState.FAILED
                        || state == RokuricsRecordingState.PERMISSION_DENIED
                        || state == RokuricsRecordingState.NOTIFICATION_PERMISSION_DENIED) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = statusMessage,
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = RokuricsColors.coral,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.padding(horizontal = 24.dp)
                        )
                    }
                    if (state == RokuricsRecordingState.SAVING || state == RokuricsRecordingState.SAVED) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = statusMessage,
                            fontSize = 13.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = RokuricsColors.mint,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.padding(horizontal = 24.dp)
                        )
                        if (uploadQueueCount > 0) {
                            Spacer(modifier = Modifier.height(4.dp))
                            Row(
                                modifier = Modifier.padding(horizontal = 24.dp),
                                horizontalArrangement = Arrangement.Center,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Icon(
                                    Icons.Default.CloudUpload,
                                    contentDescription = null,
                                    tint = RokuricsColors.aqua,
                                    modifier = Modifier.size(14.dp)
                                )
                                Spacer(Modifier.width(4.dp))
                                Text(
                                    text = "传输队列 · $uploadQueueCount 项待传输",
                                    fontSize = 12.sp,
                                    color = RokuricsColors.softText,
                                    textAlign = TextAlign.Center
                                )
                            }
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.height(8.dp))

            // Waveform bars during recording
            if (state == RokuricsRecordingState.RECORDING) {
                WaveformBars(
                    amplitudeLevel = amplitudeLevel,
                    modifier = Modifier.fillMaxWidth().height(48.dp)
                )
                Spacer(modifier = Modifier.height(8.dp))
            }

            Spacer(modifier = Modifier.weight(0.5f))

            // Control buttons row (Apple parity: 3-button layout)
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(if (metrics.isPadWidth) 16.dp else 12.dp)
            ) {
                when (state) {
                    RokuricsRecordingState.IDLE, RokuricsRecordingState.SAVED,
                    RokuricsRecordingState.PERMISSION_DENIED, RokuricsRecordingState.FAILED,
                    RokuricsRecordingState.NOTIFICATION_PERMISSION_DENIED -> {
                        RecordButton(
                            modifier = Modifier.weight(1f),
                            text = "开始",
                            icon = Icons.Default.Mic,
                            color = RokuricsColors.coral,
                            onClick = { startRecordingWithPermissionRequest() }
                        )
                        // Placeholder buttons for visual balance
                        Spacer(Modifier.weight(1f))
                        Spacer(Modifier.weight(1f))
                    }
                    RokuricsRecordingState.RECORDING -> {
                        RecordButton(
                            modifier = Modifier.weight(1f),
                            text = "暂停",
                            icon = Icons.Default.Pause,
                            color = RokuricsColors.softTeal,
                            onClick = {
                                recordUserInteraction()
                                recordingManager.pauseRecording()
                            }
                        )
                        RecordButton(
                            modifier = Modifier.weight(1f),
                            text = "停止",
                            icon = Icons.Default.Stop,
                            color = RokuricsColors.coral,
                            onClick = {
                                recordUserInteraction()
                                recordingManager.stopRecording()
                            }
                        )
                        RecordButton(
                            modifier = Modifier.weight(1f),
                            text = "上传",
                            icon = Icons.Default.CloudUpload,
                            color = RokuricsColors.softText,
                            enabled = false,
                            onClick = {}
                        )
                    }
                    RokuricsRecordingState.PAUSED -> {
                        RecordButton(
                            modifier = Modifier.weight(1f),
                            text = "继续",
                            icon = Icons.Default.PlayArrow,
                            color = RokuricsColors.aqua,
                            onClick = {
                                recordUserInteraction()
                                recordingManager.resumeRecording()
                            }
                        )
                        RecordButton(
                            modifier = Modifier.weight(1f),
                            text = "停止",
                            icon = Icons.Default.Stop,
                            color = RokuricsColors.coral,
                            onClick = {
                                recordUserInteraction()
                                recordingManager.stopRecording()
                            }
                        )
                        RecordButton(
                            modifier = Modifier.weight(1f),
                            text = "上传",
                            icon = Icons.Default.CloudUpload,
                            color = RokuricsColors.softText,
                            enabled = false,
                            onClick = {}
                        )
                    }
                    else -> {
                        Box(
                            modifier = Modifier.fillMaxWidth(),
                            contentAlignment = Alignment.Center
                        ) {
                            CircularProgressIndicator(color = RokuricsColors.aqua)
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.height(24.dp))
            Spacer(modifier = Modifier.navigationBarsPadding())
        }
    }

        // Filing overlay
        if (isFiling) {
            FilingOverlay(
                type = filingType,
                subject = filingSubject,
                chapter = filingChapter,
                topic = filingTopic,
                onTypeChange = { filingType = it },
                onSubjectChange = { filingSubject = it },
                onChapterChange = { filingChapter = it },
                onTopicChange = { filingTopic = it },
                items = studyLibraryStore.allStudyItems(),
                folders = studyLibraryStore.allStudyFolders(),
                onSave = {
                    val filing = StudyFilingPath(
                        type = filingType.ifEmpty { null },
                        subject = filingSubject.ifEmpty { null },
                        chapter = filingChapter.ifEmpty { null },
                        topic = filingTopic.ifEmpty { null }
                    )
                    recordingManager.finalizeRecording(studyFiling = filing)
                },
                onDirectSave = {
                    recordingManager.finalizeRecordingDirectSave()
                }
            )
        }
    }

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun FilingOverlay(
    type: String, subject: String, chapter: String, topic: String,
    onTypeChange: (String) -> Unit,
    onSubjectChange: (String) -> Unit,
    onChapterChange: (String) -> Unit,
    onTopicChange: (String) -> Unit,
    items: List<com.rokurics.app.domain.model.StudyItemMetadata>,
    folders: List<com.rokurics.app.domain.model.StudyFolderMetadata>,
    onSave: () -> Unit,
    onDirectSave: () -> Unit
) {
    var activeLevel by remember { mutableStateOf(StudyFolderLevel.TYPE) }
    var newValueDraft by remember { mutableStateOf("") }

    val levels = listOf(StudyFolderLevel.TYPE, StudyFolderLevel.SUBJECT, StudyFolderLevel.CHAPTER, StudyFolderLevel.TOPIC)

    fun valueFor(level: StudyFolderLevel): String? = when (level) {
        StudyFolderLevel.TYPE -> type.ifEmpty { null }
        StudyFolderLevel.SUBJECT -> subject.ifEmpty { null }
        StudyFolderLevel.CHAPTER -> chapter.ifEmpty { null }
        StudyFolderLevel.TOPIC -> topic.ifEmpty { null }
        StudyFolderLevel.CUSTOM -> null
    }

    fun hasAnyFiling(): Boolean =
        type.isNotEmpty() || subject.isNotEmpty() || chapter.isNotEmpty() || topic.isNotEmpty()

    fun canActivate(level: StudyFolderLevel): Boolean = when (level) {
        StudyFolderLevel.TYPE -> true
        StudyFolderLevel.SUBJECT -> type.isNotEmpty()
        StudyFolderLevel.CHAPTER -> type.isNotEmpty() && subject.isNotEmpty()
        StudyFolderLevel.TOPIC -> type.isNotEmpty() && subject.isNotEmpty() && chapter.isNotEmpty()
        StudyFolderLevel.CUSTOM -> false
    }

    // Auto-select active level
    val autoLevel = levels.firstOrNull { valueFor(it) == null } ?: StudyFolderLevel.TOPIC
    if (canActivate(autoLevel)) {
        activeLevel = autoLevel
    }

    // Collect candidates
    val candidates = remember(items, folders, activeLevel) {
        val values = mutableSetOf<String>()
        for (item in items) {
            val v = when (activeLevel) {
                StudyFolderLevel.TYPE -> item.filingPath.type
                StudyFolderLevel.SUBJECT -> item.filingPath.subject
                StudyFolderLevel.CHAPTER -> item.filingPath.chapter
                StudyFolderLevel.TOPIC -> item.filingPath.topic
                StudyFolderLevel.CUSTOM -> null
            }
            if (v != null && v.isNotEmpty()) values.add(v)
        }
        for (folder in folders) {
            val v = when (activeLevel) {
                StudyFolderLevel.TYPE -> folder.path.type
                StudyFolderLevel.SUBJECT -> folder.path.subject
                StudyFolderLevel.CHAPTER -> folder.path.chapter
                StudyFolderLevel.TOPIC -> folder.path.topic
                StudyFolderLevel.CUSTOM -> null
            }
            if (v != null && v.isNotEmpty()) values.add(v)
        }
        values.toList().sorted()
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.White.copy(alpha = 0.18f))
            .clickable(enabled = false) { /* consume clicks */ },
        contentAlignment = Alignment.Center
    ) {
        Surface(
            modifier = Modifier
                .padding(horizontal = 24.dp)
                .widthIn(max = 360.dp),
            shape = RoundedCornerShape(30.dp),
            color = Color.White.copy(alpha = 0.52f),
            tonalElevation = 4.dp,
            shadowElevation = 14.dp
        ) {
            Column(
                modifier = Modifier.padding(22.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                // Title
                Column(verticalArrangement = Arrangement.spacedBy(5.dp)) {
                    Text(
                        text = "录音归档",
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Bold,
                        color = RokuricsColors.deepText
                    )
                    Text(
                        text = "选择${activeLevel.title}",
                        fontSize = 14.sp,
                        color = RokuricsColors.softText
                    )
                }

                // Level buttons
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    levels.forEach { level ->
                        FilingLevelButton(
                            level = level,
                            value = valueFor(level),
                            isActive = activeLevel == level,
                            isEnabled = canActivate(level),
                            onClick = {
                                if (canActivate(level)) {
                                    activeLevel = level
                                    newValueDraft = ""
                                }
                            },
                            modifier = Modifier.weight(1f)
                        )
                    }
                }

                // Candidates
                if (candidates.isEmpty()) {
                    Text(
                        text = "暂无已有${activeLevel.title}，可以新建。",
                        fontSize = 12.sp,
                        color = RokuricsColors.softText
                    )
                } else {
                    FlowRow(
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        candidates.forEach { candidate ->
                            val isSelected = valueFor(activeLevel) == candidate
                            Surface(
                                modifier = Modifier.clickable {
                                    val update: (String) -> Unit = when (activeLevel) {
                                        StudyFolderLevel.TYPE -> onTypeChange
                                        StudyFolderLevel.SUBJECT -> onSubjectChange
                                        StudyFolderLevel.CHAPTER -> onChapterChange
                                        StudyFolderLevel.TOPIC -> onTopicChange
                                        StudyFolderLevel.CUSTOM -> {{}}
                                    }
                                    update(candidate)
                                    // Auto-advance to next level
                                    when (activeLevel) {
                                        StudyFolderLevel.TYPE -> activeLevel = StudyFolderLevel.SUBJECT
                                        StudyFolderLevel.SUBJECT -> activeLevel = StudyFolderLevel.CHAPTER
                                        StudyFolderLevel.CHAPTER -> activeLevel = StudyFolderLevel.TOPIC
                                        else -> {}
                                    }
                                    newValueDraft = ""
                                },
                                shape = RoundedCornerShape(12.dp),
                                color = if (isSelected) RokuricsColors.aqua
                                else RokuricsColors.aqua.copy(alpha = 0.12f)
                            ) {
                                Text(
                                    text = candidate,
                                    fontSize = 12.sp,
                                    fontWeight = FontWeight.SemiBold,
                                    color = if (isSelected) Color.White else RokuricsColors.deepText,
                                    modifier = Modifier.padding(horizontal = 11.dp, vertical = 8.dp)
                                )
                            }
                        }
                    }
                }

                // New value input
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(48.dp)
                        .clip(RoundedCornerShape(16.dp))
                        .background(RokuricsColors.aqua.copy(alpha = 0.08f))
                        .border(1.dp, RokuricsColors.aqua.copy(alpha = 0.2f), RoundedCornerShape(16.dp))
                        .padding(horizontal = 12.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    OutlinedTextField(
                        value = newValueDraft,
                        onValueChange = { newValueDraft = it },
                        modifier = Modifier.weight(1f),
                        placeholder = { Text("新建${activeLevel.title}", fontSize = 13.sp) },
                        textStyle = LocalTextStyle.current.copy(fontSize = 13.sp),
                        singleLine = true
                    )
                    IconButton(
                        onClick = {
                            val trimmed = newValueDraft.trim()
                            if (trimmed.isNotEmpty()) {
                                val update: (String) -> Unit = when (activeLevel) {
                                    StudyFolderLevel.TYPE -> onTypeChange
                                    StudyFolderLevel.SUBJECT -> onSubjectChange
                                    StudyFolderLevel.CHAPTER -> onChapterChange
                                    StudyFolderLevel.TOPIC -> onTopicChange
                                    StudyFolderLevel.CUSTOM -> {{}}
                                }
                                update(trimmed)
                                newValueDraft = ""
                                // Auto-advance
                                when (activeLevel) {
                                    StudyFolderLevel.TYPE -> activeLevel = StudyFolderLevel.SUBJECT
                                    StudyFolderLevel.SUBJECT -> activeLevel = StudyFolderLevel.CHAPTER
                                    StudyFolderLevel.CHAPTER -> activeLevel = StudyFolderLevel.TOPIC
                                    else -> {}
                                }
                            }
                        },
                        enabled = newValueDraft.trim().isNotEmpty()
                    ) {
                        Icon(
                            Icons.Default.Add,
                            contentDescription = "新建${activeLevel.title}",
                            tint = if (newValueDraft.trim().isNotEmpty()) RokuricsColors.aqua
                            else RokuricsColors.softText
                        )
                    }
                }

                // Action buttons
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    OutlinedButton(
                        onClick = onDirectSave,
                        modifier = Modifier.weight(1f).height(48.dp),
                        shape = RoundedCornerShape(24.dp)
                    ) {
                        Text("直接保存", fontSize = 14.sp, color = RokuricsColors.softText)
                    }
                    Button(
                        onClick = onSave,
                        modifier = Modifier.weight(1f).height(48.dp),
                        enabled = hasAnyFiling(),
                        shape = RoundedCornerShape(24.dp),
                        colors = ButtonDefaults.buttonColors(
                            containerColor = RokuricsColors.aqua,
                            disabledContainerColor = RokuricsColors.softText.copy(alpha = 0.3f)
                        )
                    ) {
                        Text("保存", fontSize = 14.sp)
                    }
                }
            }
        }
    }
}

@Composable
fun FilingLevelButton(
    level: StudyFolderLevel,
    value: String?,
    isActive: Boolean,
    isEnabled: Boolean,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Surface(
        modifier = modifier.clickable(enabled = isEnabled, onClick = onClick),
        shape = RoundedCornerShape(13.dp),
        color = if (isActive) RokuricsColors.aqua.copy(alpha = 0.15f)
        else Color.White.copy(alpha = 0.2f),
        border = if (isActive) androidx.compose.foundation.BorderStroke(1.dp, RokuricsColors.aqua.copy(alpha = 0.46f))
        else androidx.compose.foundation.BorderStroke(1.dp, Color.White.copy(alpha = 0.12f))
    ) {
        Column(
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 9.dp)
        ) {
            Text(
                text = level.title,
                fontSize = 10.sp,
                fontWeight = FontWeight.Bold,
                color = RokuricsColors.softText
            )
            Text(
                text = value ?: "未选择",
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                color = if (isEnabled) RokuricsColors.deepText else RokuricsColors.softText,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
}

@Composable
fun RecordButton(
    modifier: Modifier = Modifier,
    text: String,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    color: Color,
    enabled: Boolean = true,
    onClick: () -> Unit
) {
    Surface(
        modifier = modifier
            .height(76.dp)
            .rokuricsScaleClickable(onClick = onClick, enabled = enabled),
        shape = RoundedCornerShape(24.dp),
        color = if (enabled) Color.White.copy(alpha = 0.34f) else Color.White.copy(alpha = 0.20f),
        tonalElevation = if (enabled) 2.dp else 0.dp,
        shadowElevation = if (enabled) 6.dp else 1.dp,
        border = if (enabled) androidx.compose.foundation.BorderStroke(
            0.5.dp, color.copy(alpha = 0.34f)
        ) else null
    ) {
        Column(
            modifier = Modifier.fillMaxSize(),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Icon(
                imageVector = icon,
                contentDescription = text,
                tint = if (enabled) color else RokuricsColors.softText,
                modifier = Modifier.size(22.dp)
            )
            Spacer(modifier = Modifier.height(4.dp))
            Text(
                text = text,
                fontSize = 13.sp,
                fontWeight = FontWeight.SemiBold,
                color = if (enabled) color else RokuricsColors.softText
            )
        }
    }
}

@Composable
private fun WaveformBars(
    amplitudeLevel: Float,
    barCount: Int = 20,
    modifier: Modifier = Modifier
) {
    val animatedLevels = remember { mutableStateListOf<Float>() }
    // Initialize bar levels
    LaunchedEffect(Unit) {
        if (animatedLevels.isEmpty()) {
            repeat(barCount) { animatedLevels.add(0f) }
        }
    }
    // Animate bars toward the target amplitude with staggered decay
    LaunchedEffect(amplitudeLevel) {
        if (animatedLevels.size != barCount) return@LaunchedEffect
        // Push new amplitude into a random bar, decay others
        val targetIdx = (Math.random() * barCount).toInt()
        for (i in 0 until barCount) {
            val target = if (i == targetIdx) (amplitudeLevel * 0.9f + 0.1f).coerceIn(0.05f, 1f)
            else (animatedLevels[i] * 0.85f).coerceAtLeast(0.04f)
            animatedLevels[i] = target
        }
        // Push random mid-level bars for visual richness when amplitude is low
        repeat(barCount / 4) {
            val idx = (Math.random() * barCount).toInt()
            animatedLevels[idx] = (animatedLevels[idx] + (Math.random().toFloat() * 0.3f)).coerceAtMost(1f)
        }
    }

    Row(
        modifier = modifier.padding(horizontal = 8.dp),
        horizontalArrangement = Arrangement.spacedBy(3.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        if (animatedLevels.size == barCount) {
            for (i in 0 until barCount) {
                val heightFraction = animatedLevels[i]
                Box(
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxHeight(fraction = heightFraction.coerceIn(0.08f, 1f))
                        .clip(RoundedCornerShape(4.dp))
                        .background(
                            Brush.linearGradient(
                                colors = listOf(RokuricsColors.aqua, RokuricsColors.mint),
                                start = Offset(0f, Float.POSITIVE_INFINITY),
                                end = Offset(0f, 0f)
                            )
                        )
                )
            }
        }
    }
}
