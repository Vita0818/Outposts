package com.rokurics.app.ui.library

import android.media.MediaPlayer
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
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
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.rokurics.app.data.AudioFileStore
import com.rokurics.app.data.StudyLibraryStore
import com.rokurics.app.domain.model.*
import com.rokurics.app.domain.model.StudyFolderColorToken
import com.rokurics.app.domain.sync.StudyLibraryBrowser
import com.rokurics.app.service.RecordingManager
import com.rokurics.app.ui.theme.RokuricsAdaptiveMetrics
import com.rokurics.app.ui.theme.RokuricsColors
import com.rokurics.app.ui.theme.rokuricsScaleClickable
import kotlinx.coroutines.delay
import java.text.SimpleDateFormat
import java.util.*

@OptIn(ExperimentalMaterial3Api::class, ExperimentalFoundationApi::class)
@Composable
fun RecordingLibraryScreen(
    recordingManager: RecordingManager,
    onBack: () -> Unit
) {
    val studyLibraryStore = remember { StudyLibraryStore() }
    val audioFileStore = remember { AudioFileStore() }
    val recordings by recordingManager.recordings.collectAsState()
    val trashedRecordings by recordingManager.trashedRecordings.collectAsState()

    var browsePath by remember { mutableStateOf(StudyBrowsePath()) }
    var selectedTab by remember { mutableIntStateOf(0) }
    var editingRecordingId by remember { mutableStateOf<String?>(null) }
    var editTitle by remember { mutableStateOf("") }
    var detailRecordingId by remember { mutableStateOf<String?>(null) }
    var readingRecordingId by remember { mutableStateOf<String?>(null) }
    var readingKind by remember { mutableStateOf(StudyReadingKind.TRANSCRIPT) }
    var uploadStatusMessage by remember { mutableStateOf<String?>(null) }
    var folderColorTargetId by remember { mutableStateOf<String?>(null) }

    // Playback state
    var playingRecordingId by remember { mutableStateOf<String?>(null) }
    var isPlaying by remember { mutableStateOf(false) }
    var playbackPositionMs by remember { mutableIntStateOf(0) }
    var playbackDurationMs by remember { mutableIntStateOf(0) }
    var isSeeking by remember { mutableStateOf(false) }
    var seekFraction by remember { mutableFloatStateOf(0f) }
    val mediaPlayer = remember { mutableStateOf<MediaPlayer?>(null) }

    DisposableEffect(Unit) {
        onDispose {
            mediaPlayer.value?.apply { if (isPlaying) stop(); release() }
        }
    }

    // Position tracker during playback
    LaunchedEffect(isPlaying) {
        if (isPlaying) {
            while (isPlaying) {
                kotlinx.coroutines.delay(250)
                if (!isSeeking) {
                    val mp = mediaPlayer.value ?: continue
                    try {
                        playbackPositionMs = mp.currentPosition
                        if (playbackDurationMs > 0 && playbackPositionMs >= playbackDurationMs) {
                            playbackPositionMs = playbackDurationMs
                            isPlaying = false
                        }
                    } catch (_: Exception) {}
                }
            }
        }
    }

    // Position updater while seeking
    LaunchedEffect(isSeeking) {
        if (isSeeking) {
            while (isSeeking) {
                kotlinx.coroutines.delay(100)
                val mp = mediaPlayer.value ?: continue
                try {
                    playbackPositionMs = mp.currentPosition
                    if (playbackDurationMs > 0) {
                        seekFraction = playbackPositionMs.toFloat() / playbackDurationMs
                    }
                } catch (_: Exception) {}
            }
        }
    }

    LaunchedEffect(Unit) {
        recordingManager.reloadRecordings()
        studyLibraryStore.refresh()
    }

    // Push recording metadata into study library on load
    LaunchedEffect(recordings) {
        for (rec in recordings) {
            studyLibraryStore.upsertRecordingMetadata(rec)
        }
    }

    fun stopPlayback() {
        mediaPlayer.value?.apply { if (isPlaying) stop(); release() }
        mediaPlayer.value = null
        playingRecordingId = null
        isPlaying = false
        playbackPositionMs = 0
        playbackDurationMs = 0
        isSeeking = false
    }

    // Compute browser content — refresh study library store from recordings
    val allItems = remember(recordings) { studyLibraryStore.allStudyItems() }
    val allFolders = remember { studyLibraryStore.allStudyFolders() }
    val content = remember(browsePath, recordings) {
        studyLibraryStore.refresh()
        studyLibraryStore.syncFromRecordings(recordings)
        StudyLibraryBrowser.content(
            studyLibraryStore.allStudyItems(),
            studyLibraryStore.allStudyFolders(),
            browsePath
        )
    }

    // Reading page overlay
    if (readingRecordingId != null) {
        StudyReadingPage(
            recordingID = readingRecordingId!!,
            kind = readingKind,
            studyLibraryStore = studyLibraryStore,
            audioFileStore = audioFileStore,
            onBack = { readingRecordingId = null }
        )
        return
    }

    // Detail page overlay
    if (detailRecordingId != null) {
        RecordingStudyDetailPage(
            recordingID = detailRecordingId!!,
            recordingManager = recordingManager,
            studyLibraryStore = studyLibraryStore,
            onBack = { detailRecordingId = null },
            onOpenTranscript = { id ->
                readingRecordingId = id
                readingKind = StudyReadingKind.TRANSCRIPT
            },
            onOpenNote = { id ->
                readingRecordingId = id
                readingKind = StudyReadingKind.NOTE
            },
            onUpload = { id ->
                recordingManager.enqueueUpload(id)
                uploadStatusMessage = "已加入传输队列"
            },
            onRemoteTranscribe = { id ->
                recordingManager.startRemoteTranscription(id)
            },
            onGenerateNote = { id ->
                recordingManager.startNoteGeneration(id)
            }
        )
        return
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("学习库", fontSize = 18.sp, fontWeight = FontWeight.SemiBold) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "返回")
                    }
                },
                actions = {
                    // Trash tab toggle
                    FilterChip(
                        selected = selectedTab == 1,
                        onClick = {
                            selectedTab = if (selectedTab == 1) 0 else 1
                            browsePath = StudyBrowsePath()
                        },
                        label = { Text("已删除", fontSize = 12.sp) }
                    )
                }
            )
        }
    ) { padding ->
        if (selectedTab == 1) {
            // Trash view
            LazyColumn(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .background(Color(0xFFF0FAF8)),
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                if (trashedRecordings.isEmpty()) {
                    item {
                        Box(Modifier.fillMaxWidth().padding(48.dp), contentAlignment = Alignment.Center) {
                            Text("已删除列表为空", color = RokuricsColors.softText)
                        }
                    }
                }
                items(trashedRecordings, key = { it.id }) { rec ->
                    RecordingRow(
                        recording = rec, isTrashed = true,
                        onRename = { recordingManager.renameRecording(rec.id, it); editingRecordingId = null },
                        onDelete = { recordingManager.deleteRecording(rec.id) },
                        onRestore = { recordingManager.restoreRecording(rec.id) },
                        onPermanentDelete = { recordingManager.permanentlyDeleteRecording(rec.id) },
                        onEditClick = { editingRecordingId = rec.id; editTitle = rec.title }
                    )
                }
            }
        } else {
            // Tree browser
            BoxWithConstraints(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
            ) {
                val metrics = RokuricsAdaptiveMetrics(maxWidth.value, maxHeight.value)
                val horzPadding = metrics.horizontalPadding
                val contentMaxWidth = metrics.contentMaxWidth

            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(horizontal = horzPadding, vertical = 8.dp)
                    .then(if (metrics.isWide) Modifier.widthIn(max = contentMaxWidth) else Modifier),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                // Breadcrumb bar
                BreadcrumbBar(
                    path = browsePath,
                    onNavigate = { browsePath = it }
                )

                Spacer(modifier = Modifier.height(12.dp))

                // Content area
                LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    // Empty state
                    if (content.isEmpty) {
                        item {
                            EmptyLibraryState()
                        }
                    }

                    // Folder grid
                    if (content.folders.isNotEmpty()) {
                        item {
                            Text(
                                text = browsePath.components.lastOrNull() ?: "门类",
                                fontSize = 15.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = RokuricsColors.deepText,
                                modifier = Modifier.padding(vertical = 4.dp)
                            )
                        }
                        // Responsive folder grid: 3 cols on wide, 2 on narrow
                        item {
                            val columns = if (metrics.isWide) 3 else 2
                            val chunked = content.folders.chunked(columns)
                            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                                chunked.forEach { row ->
                                    Row(
                                        modifier = Modifier.fillMaxWidth(),
                                        horizontalArrangement = Arrangement.spacedBy(10.dp)
                                    ) {
                                        row.forEach { folder ->
                                            StudyFolderTile(
                                                folder = folder,
                                                onClick = { browsePath = folder.path },
                                                onLongClick = { folderColorTargetId = folder.folderID },
                                                modifier = Modifier.weight(1f)
                                            )
                                        }
                                        // Fill empty slots
                                        repeat(columns - row.size) {
                                            Spacer(modifier = Modifier.weight(1f))
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Recording cards
                    if (content.items.isNotEmpty()) {
                        item {
                            Text(
                                text = "录音 · ${content.items.size} 项",
                                fontSize = 15.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = RokuricsColors.deepText,
                                modifier = Modifier.padding(vertical = 4.dp)
                            )
                        }
                        items(content.items, key = { it.itemID }) { item ->
                            val rec = recordings.find { r -> r.id == item.recordingID }
                            if (rec != null) {
                                RecordingRow(
                                    recording = rec,
                                    isTrashed = false,
                                    isPlaying = playingRecordingId == rec.id && isPlaying,
                                    onClick = { detailRecordingId = rec.id },
                                    onPlay = {
                                        if (playingRecordingId == rec.id && isPlaying) {
                                            mediaPlayer.value?.pause(); isPlaying = false
                                        } else if (playingRecordingId == rec.id) {
                                            mediaPlayer.value?.start(); isPlaying = true
                                        } else {
                                            stopPlayback()
                                            val file = audioFileStore.audioFileFor(rec)
                                            if (file.exists()) {
                                                try {
                                                    val mp = MediaPlayer().apply {
                                                        setDataSource(file.absolutePath)
                                                        prepare(); start()
                                                        setOnCompletionListener { stopPlayback() }
                                                    }
                                                    mediaPlayer.value = mp
                                                    playingRecordingId = rec.id; isPlaying = true
                                                    playbackDurationMs = mp.duration
                                                    playbackPositionMs = 0
                                                } catch (_: Exception) {}
                                            }
                                        }
                                    },
                                    onRename = { newTitle ->
                                        recordingManager.renameRecording(rec.id, newTitle)
                                        editingRecordingId = null
                                    },
                                    onDelete = { recordingManager.deleteRecording(rec.id) },
                                    onRestore = {},
                                    onPermanentDelete = {},
                                    onEditClick = { editingRecordingId = rec.id; editTitle = rec.title },
                                    onLocalTranscribe = { recordingManager.startLocalTranscription(rec.id) }
                                )
                            }
                        }
                    }
                }
            }
            }
        }

        // Playback bar with seek and time
        if (playingRecordingId != null) {
            Surface(
                modifier = Modifier.fillMaxWidth(),
                color = Color.White.copy(alpha = 0.92f),
                shadowElevation = 8.dp
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 6.dp)
                        .navigationBarsPadding()
                ) {
                    // Title + Time + Controls row
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        IconButton(
                            onClick = {
                                if (isPlaying) {
                                    mediaPlayer.value?.pause(); isPlaying = false
                                } else {
                                    mediaPlayer.value?.start(); isPlaying = true
                                }
                            },
                            modifier = Modifier.size(36.dp)
                        ) {
                            Icon(
                                imageVector = if (isPlaying) Icons.Default.Pause else Icons.Default.PlayArrow,
                                contentDescription = if (isPlaying) "暂停" else "播放",
                                tint = RokuricsColors.aqua,
                                modifier = Modifier.size(22.dp)
                            )
                        }
                        Column(
                            modifier = Modifier
                                .weight(1f)
                                .padding(horizontal = 8.dp)
                        ) {
                            Text(
                                text = recordings.find { it.id == playingRecordingId }?.title ?: "播放中",
                                fontSize = 13.sp, fontWeight = FontWeight.SemiBold,
                                color = RokuricsColors.deepText, maxLines = 1,
                                overflow = TextOverflow.Ellipsis
                            )
                            Spacer(Modifier.height(2.dp))
                            // Compact seek bar
                            Slider(
                                value = if (isSeeking) seekFraction
                                else if (playbackDurationMs > 0) playbackPositionMs.toFloat() / playbackDurationMs
                                else 0f,
                                onValueChange = { fraction ->
                                    seekFraction = fraction
                                    isSeeking = true
                                },
                                onValueChangeFinished = {
                                    val targetMs = (seekFraction * playbackDurationMs).toInt()
                                    mediaPlayer.value?.seekTo(targetMs)
                                    playbackPositionMs = targetMs
                                    isSeeking = false
                                },
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .height(18.dp),
                                colors = SliderDefaults.colors(
                                    thumbColor = RokuricsColors.aqua,
                                    activeTrackColor = RokuricsColors.aqua,
                                    inactiveTrackColor = RokuricsColors.aqua.copy(alpha = 0.14f)
                                ),
                                enabled = playbackDurationMs > 0
                            )
                        }
                        // Time display
                        Text(
                            text = "${formatPosition(playbackPositionMs)} / ${formatPosition(playbackDurationMs)}",
                            fontSize = 11.sp,
                            color = RokuricsColors.softText,
                            modifier = Modifier.padding(end = 4.dp)
                        )
                        IconButton(
                            onClick = { stopPlayback() },
                            modifier = Modifier.size(32.dp)
                        ) {
                            Icon(
                                Icons.Default.Close,
                                contentDescription = "关闭",
                                tint = RokuricsColors.softText,
                                modifier = Modifier.size(18.dp)
                            )
                        }
                    }
                }
            }
        }
    }

    // Folder color picker dialog
    if (folderColorTargetId != null) {
        val allFolders = remember { studyLibraryStore.allStudyFolders() }
        val targetFolder = allFolders.find { it.folderID == folderColorTargetId }
        val currentColor = targetFolder?.colorToken ?: StudyFolderColorToken.DEFAULT

        AlertDialog(
            onDismissRequest = { folderColorTargetId = null },
            title = { Text("文件夹颜色", fontWeight = FontWeight.Bold) },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    Text(
                        text = targetFolder?.name ?: "",
                        fontSize = 15.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = RokuricsColors.deepText
                    )
                    // 2-row color grid (6 per row)
                    val colorTokens = StudyFolderColorToken.entries
                    val rows = colorTokens.chunked(6)
                    rows.forEach { row ->
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            row.forEach { token ->
                                val isSelected = token == currentColor
                                Surface(
                                    modifier = Modifier
                                        .size(40.dp)
                                        .combinedClickable(
                                            onClick = {
                                                studyLibraryStore.updateFolderColor(folderColorTargetId!!, token)
                                                studyLibraryStore.refresh()
                                                folderColorTargetId = null
                                            }
                                        ),
                                    shape = CircleShape,
                                    color = Color(token.hexColor).copy(alpha = 0.2f),
                                    border = if (isSelected) BorderStroke(2.dp, Color(token.hexColor))
                                    else BorderStroke(1.dp, Color.Transparent)
                                ) {
                                    Box(
                                        modifier = Modifier
                                            .fillMaxSize()
                                            .padding(6.dp),
                                        contentAlignment = Alignment.Center
                                    ) {
                                        Surface(
                                            modifier = Modifier.size(24.dp),
                                            shape = CircleShape,
                                            color = Color(token.hexColor)
                                        ) {}
                                        if (isSelected) {
                                            Icon(
                                                Icons.Default.Check,
                                                contentDescription = null,
                                                tint = Color.White,
                                                modifier = Modifier.size(16.dp)
                                            )
                                        }
                                    }
                                }
                            }
                            // Fill remaining slots
                            repeat(6 - row.size) {
                                Spacer(Modifier.size(40.dp))
                            }
                        }
                    }
                }
            },
            confirmButton = {
                TextButton(onClick = { folderColorTargetId = null }) {
                    Text("关闭")
                }
            }
        )
    }

    // Edit dialog
    if (editingRecordingId != null) {
        AlertDialog(
            onDismissRequest = { editingRecordingId = null },
            title = { Text("重命名") },
            text = {
                OutlinedTextField(value = editTitle, onValueChange = { editTitle = it }, label = { Text("标题") })
            },
            confirmButton = {
                TextButton(onClick = {
                    recordingManager.renameRecording(editingRecordingId!!, editTitle)
                    editingRecordingId = null
                }) { Text("确定") }
            },
            dismissButton = {
                TextButton(onClick = { editingRecordingId = null }) { Text("取消") }
            }
        )
    }
}

// ── Breadcrumb Bar ───────────────────────────────────────────────

@Composable
fun BreadcrumbBar(
    path: StudyBrowsePath,
    onNavigate: (StudyBrowsePath) -> Unit
) {
    val crumbs = StudyLibraryBrowser.breadcrumbs(path)
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .horizontalScroll(rememberScrollState()),
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Back button
        if (!path.isRoot) {
            IconButton(
                onClick = { onNavigate(path.parent) },
                modifier = Modifier.size(32.dp)
            ) {
                Icon(
                    Icons.Default.ChevronLeft,
                    contentDescription = "返回上一级",
                    tint = RokuricsColors.aqua,
                    modifier = Modifier.size(20.dp)
                )
            }
        }

        crumbs.forEachIndexed { index, (label, crumbPath) ->
            val isLast = index == crumbs.lastIndex
            Surface(
                modifier = Modifier.clickable(enabled = !isLast) { onNavigate(crumbPath) },
                shape = RoundedCornerShape(12.dp),
                color = if (isLast) RokuricsColors.aqua.copy(alpha = 0.1f) else Color.Transparent
            ) {
                Text(
                    text = label,
                    fontSize = 13.sp,
                    fontWeight = if (isLast) FontWeight.Bold else FontWeight.Medium,
                    color = if (isLast) RokuricsColors.deepText else RokuricsColors.softText,
                    modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
            if (!isLast) {
                Text(" / ", fontSize = 12.sp, color = RokuricsColors.softText)
            }
        }
    }
}

// ── Folder Tile ──────────────────────────────────────────────────

@OptIn(ExperimentalFoundationApi::class)
@Composable
fun StudyFolderTile(
    folder: StudyBrowseFolder,
    onClick: () -> Unit,
    onLongClick: () -> Unit = {},
    modifier: Modifier = Modifier
) {
    val colorHex = folder.colorToken?.hexColor ?: StudyFolderColorToken.DEFAULT.hexColor
    val tileColor = Color(colorHex)
    val tileAlpha = if (folder.isFallback) 0.4f else 0.18f

    Card(
        modifier = modifier
            .height(130.dp)
            .combinedClickable(
                onClick = onClick,
                onLongClick = onLongClick
            ),
        shape = RoundedCornerShape(18.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.55f))
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(14.dp),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Folder icon with color badge
            Box(
                modifier = Modifier.size(48.dp),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Default.Folder,
                    contentDescription = null,
                    tint = tileColor.copy(alpha = if (folder.isFallback) 0.35f else 1f),
                    modifier = Modifier.size(44.dp)
                )
                // Color dot badge
                Surface(
                    modifier = Modifier
                        .size(14.dp)
                        .align(Alignment.BottomEnd)
                        .offset(x = 2.dp, y = 2.dp),
                    shape = CircleShape,
                    color = tileColor.copy(alpha = if (folder.isFallback) 0.4f else 0.9f)
                ) {}
            }

            Spacer(modifier = Modifier.height(10.dp))

            // Title
            Text(
                text = folder.title,
                fontSize = 14.sp,
                fontWeight = FontWeight.Bold,
                color = if (folder.isFallback) RokuricsColors.softText else RokuricsColors.deepText,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
                textAlign = TextAlign.Center
            )

            Spacer(modifier = Modifier.height(4.dp))

            // Item count
            if (folder.itemCount > 0) {
                Text(
                    text = "${folder.itemCount} 项",
                    fontSize = 11.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = RokuricsColors.softText
                )
            }
        }
    }
}

// ── Empty State ──────────────────────────────────────────────────

@Composable
fun EmptyLibraryState() {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(64.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Icon(
                imageVector = Icons.Default.MenuBook,
                contentDescription = null,
                tint = RokuricsColors.aqua.copy(alpha = 0.5f),
                modifier = Modifier.size(56.dp)
            )
            Spacer(modifier = Modifier.height(16.dp))
            Text(
                text = "暂无学习内容",
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                color = RokuricsColors.deepText
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = "录音归档后，按门类→课程→章节→主题自动整理",
                fontSize = 14.sp,
                color = RokuricsColors.softText,
                textAlign = TextAlign.Center
            )
        }
    }
}

// ── Recording Row ────────────────────────────────────────────────

@Composable
fun RecordingRow(
    recording: com.rokurics.app.domain.model.RecordingMetadata,
    isTrashed: Boolean = false,
    isPlaying: Boolean = false,
    onClick: () -> Unit = {},
    onPlay: () -> Unit = {},
    onRename: (String) -> Unit,
    onDelete: () -> Unit,
    onRestore: () -> Unit,
    onPermanentDelete: () -> Unit,
    onEditClick: () -> Unit,
    onLocalTranscribe: () -> Unit = {}
) {
    val dateFormat = remember { SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.CHINA) }
    var showMenu by remember { mutableStateOf(false) }

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .then(
                if (!isTrashed) Modifier.rokuricsScaleClickable(onClick = onClick)
                else Modifier
            ),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.52f)),
        border = androidx.compose.foundation.BorderStroke(0.5.dp, Color.White.copy(alpha = 0.22f))
    ) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            if (!isTrashed) {
                IconButton(onClick = onPlay, modifier = Modifier.size(40.dp)) {
                    Icon(
                        imageVector = if (isPlaying) Icons.Default.PauseCircle else Icons.Default.PlayCircle,
                        contentDescription = if (isPlaying) "暂停" else "播放",
                        tint = if (isPlaying) RokuricsColors.coral else RokuricsColors.aqua,
                        modifier = Modifier.size(36.dp)
                    )
                }
            } else {
                Icon(Icons.Default.Mic, contentDescription = null, tint = RokuricsColors.aqua, modifier = Modifier.size(32.dp))
            }
            Spacer(modifier = Modifier.width(10.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = recording.title,
                    fontSize = 15.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = RokuricsColors.deepText,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
                Spacer(modifier = Modifier.height(2.dp))
                Text(
                    text = "${dateFormat.format(recording.createdAt)} · ${formatDuration(recording.duration)}",
                    fontSize = 12.sp,
                    color = RokuricsColors.softText
                )
                if (!isTrashed) {
                    Spacer(modifier = Modifier.height(4.dp))
                    Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                        UploadStatusChip(recording.uploadStatus)
                        val ts = recording.transcriptionStatus
                        if (ts != null && ts != "notStarted" && ts != "not_started") {
                            StatusChip("转录", ts, RokuricsColors.mint)
                        }
                        val ns = recording.noteStatus
                        if (ns != null && ns != "notStarted" && ns != "not_started") {
                            StatusChip("笔记", ns, RokuricsColors.softTeal)
                        }
                    }
                    // Upload progress bar
                    if (recording.uploadStatus == "uploading") {
                        val fraction = recording.uploadProgressFraction ?: 0.0
                        val description = recording.uploadProgressDescription ?: ""
                        Spacer(modifier = Modifier.height(4.dp))
                        LinearProgressIndicator(
                            progress = { fraction.toFloat().coerceIn(0f, 1f) },
                            modifier = Modifier.fillMaxWidth().height(3.dp).clip(RoundedCornerShape(2.dp)),
                            color = RokuricsColors.aqua,
                            trackColor = RokuricsColors.aqua.copy(alpha = 0.12f)
                        )
                        if (description.isNotEmpty()) {
                            Spacer(modifier = Modifier.height(2.dp))
                            Text(
                                text = description,
                                fontSize = 10.sp,
                                color = RokuricsColors.aqua
                            )
                        }
                    }
                }
            }
            Box {
                IconButton(onClick = { showMenu = true }) {
                    Icon(Icons.Default.MoreVert, contentDescription = "更多", tint = RokuricsColors.softText)
                }
                DropdownMenu(expanded = showMenu, onDismissRequest = { showMenu = false }) {
                    if (isTrashed) {
                        DropdownMenuItem(text = { Text("恢复") }, onClick = { showMenu = false; onRestore() })
                        DropdownMenuItem(text = { Text("永久删除") }, onClick = { showMenu = false; onPermanentDelete() })
                    } else {
                        DropdownMenuItem(text = { Text("本地转写") }, onClick = { showMenu = false; onLocalTranscribe() })
                        DropdownMenuItem(text = { Text("重命名") }, onClick = { showMenu = false; onEditClick() })
                        DropdownMenuItem(text = { Text("删除") }, onClick = { showMenu = false; onDelete() })
                    }
                }
            }
        }
    }
}

@Composable
fun UploadStatusChip(status: String?) {
    val (label, color) = when (status) {
        "uploaded" -> "已上传" to RokuricsColors.mint
        "uploading" -> "传输中" to RokuricsColors.aqua
        "failed" -> "传输失败" to RokuricsColors.coral
        "pending" -> "待传输" to RokuricsColors.softTeal
        else -> return
    }
    StatusChip("传输", label, color)
}

@Composable
fun StatusChip(label: String, value: String, color: Color) {
    Surface(shape = RoundedCornerShape(6.dp), color = color.copy(alpha = 0.12f)) {
        Text(
            text = value,
            fontSize = 11.sp,
            fontWeight = FontWeight.SemiBold,
            color = color,
            modifier = Modifier.padding(horizontal = 6.dp, vertical = 2.dp)
        )
    }
}

private fun formatDuration(seconds: Double): String {
    if (seconds < 60) return "${seconds.toInt()} sec"
    return String.format("%.1f min", seconds / 60)
}

private fun formatPosition(ms: Int): String {
    val totalSecs = (ms / 1000).coerceAtLeast(0)
    val minutes = totalSecs / 60
    val seconds = totalSecs % 60
    return "%02d:%02d".format(minutes, seconds)
}
