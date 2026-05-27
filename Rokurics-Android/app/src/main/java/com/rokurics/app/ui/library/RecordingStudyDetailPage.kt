package com.rokurics.app.ui.library

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
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
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.rokurics.app.data.StudyLibraryStore
import com.rokurics.app.domain.model.*
import com.rokurics.app.service.RecordingManager
import com.rokurics.app.ui.theme.RokuricsColors
import java.text.SimpleDateFormat
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RecordingStudyDetailPage(
    recordingID: String,
    recordingManager: RecordingManager,
    studyLibraryStore: StudyLibraryStore,
    onBack: () -> Unit,
    onOpenTranscript: (String) -> Unit = {},
    onOpenNote: (String) -> Unit = {},
    onUpload: (String) -> Unit = {},
    onRemoteTranscribe: (String) -> Unit = {},
    onGenerateNote: (String) -> Unit = {}
) {
    val recordings by recordingManager.recordings.collectAsState()
    val recording = recordings.find { it.id == recordingID }
    val studyItem = studyLibraryStore.allStudyItems().find { it.recordingID == recordingID }

    if (recording == null) {
        Box(Modifier.fillMaxSize().background(Color(0xFFF0FAF8)), contentAlignment = Alignment.Center) {
            Text("未找到录音", color = RokuricsColors.softText)
        }
        return
    }

    var typeDraft by remember { mutableStateOf(recording.studyFiling?.type ?: "") }
    var subjectDraft by remember { mutableStateOf(recording.studyFiling?.subject ?: "") }
    var chapterDraft by remember { mutableStateOf(recording.studyFiling?.chapter ?: "") }
    var topicDraft by remember { mutableStateOf(recording.studyFiling?.topic ?: "") }
    var statusMessage by remember { mutableStateOf<String?>(null) }
    var isRenaming by remember { mutableStateOf(false) }
    var renameDraft by remember { mutableStateOf(recording.title) }
    var showDeleteConfirm by remember { mutableStateOf(false) }
    var activeFilingLevel by remember { mutableStateOf<StudyFolderLevel?>(null) }
    var showPlayer by remember { mutableStateOf(false) }
    val audioFilePath = remember(recordingID) { recordingManager.getAudioFilePath(recordingID) }

    val dateFormat = remember { SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.CHINA) }
    val allItems = studyLibraryStore.allStudyItems()
    val allFolders = studyLibraryStore.allStudyFolders()

    fun filingCandidates(level: StudyFolderLevel): List<String> {
        val values = mutableSetOf<String>()
        for (item in allItems) {
            val v = when (level) {
                StudyFolderLevel.TYPE -> item.filingPath.type
                StudyFolderLevel.SUBJECT -> item.filingPath.subject
                StudyFolderLevel.CHAPTER -> item.filingPath.chapter
                StudyFolderLevel.TOPIC -> item.filingPath.topic
                else -> null
            }
            if (v != null && v.isNotEmpty()) values.add(v)
        }
        for (folder in allFolders) {
            val v = when (level) {
                StudyFolderLevel.TYPE -> folder.path.type
                StudyFolderLevel.SUBJECT -> folder.path.subject
                StudyFolderLevel.CHAPTER -> folder.path.chapter
                StudyFolderLevel.TOPIC -> folder.path.topic
                else -> null
            }
            if (v != null && v.isNotEmpty()) values.add(v)
        }
        return values.toList().sorted()
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("录音详情", fontSize = 18.sp, fontWeight = FontWeight.SemiBold) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "返回")
                    }
                },
                actions = {
                    IconButton(onClick = { isRenaming = true; renameDraft = recording.title }) {
                        Icon(Icons.Default.Edit, contentDescription = "重命名", tint = RokuricsColors.aqua)
                    }
                    IconButton(onClick = { showDeleteConfirm = true }) {
                        Icon(Icons.Default.Delete, contentDescription = "删除", tint = RokuricsColors.coral)
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
                .padding(20.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Title card
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(22.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.52f)),
                border = BorderStroke(0.5.dp, Color.White.copy(alpha = 0.22f))
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            Icons.Default.Mic,
                            contentDescription = null,
                            tint = RokuricsColors.aqua,
                            modifier = Modifier.size(28.dp)
                        )
                        Spacer(Modifier.width(10.dp))
                        Text(
                            text = recording.title,
                            fontSize = 20.sp,
                            fontWeight = FontWeight.Bold,
                            color = RokuricsColors.deepText,
                            maxLines = 2,
                            overflow = TextOverflow.Ellipsis,
                            modifier = Modifier.weight(1f)
                        )
                    }

                    HorizontalDivider(color = RokuricsColors.softText.copy(alpha = 0.12f))

                    // Metadata rows
                    MetadataRow("日期", dateFormat.format(recording.createdAt))
                    MetadataRow("时长", formatDuration(recording.duration))
                    MetadataRow("文件大小", formatFileSize(recording.fileSize))
                    MetadataRow("格式", "${recording.format} / ${recording.codec}")
                    MetadataRow("采样率", "${recording.sampleRate.toInt()} Hz · ${if (recording.channels == 1) "单声道" else "立体声"}")
                    MetadataRow("比特率", "${recording.bitrate / 1000} kbps")
                }
            }

            // Status chips
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                UploadStatusChip(recording.uploadStatus)
                val ts = recording.transcriptionStatus
                if (ts != "notStarted" && ts != "not_started") {
                    StatusChip("转录", ts, RokuricsColors.mint)
                }
                val ns = recording.noteStatus
                if (ns != "notStarted" && ns != "not_started") {
                    StatusChip("笔记", ns, RokuricsColors.softTeal)
                }
            }

            // Filing card
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(22.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.52f)),
                border = BorderStroke(0.5.dp, Color.White.copy(alpha = 0.22f))
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    Text("学习归档", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = RokuricsColors.deepText)

                    val levels = listOf(StudyFolderLevel.TYPE, StudyFolderLevel.SUBJECT, StudyFolderLevel.CHAPTER, StudyFolderLevel.TOPIC)
                    levels.forEach { level ->
                        val currentValue = when (level) {
                            StudyFolderLevel.TYPE -> typeDraft
                            StudyFolderLevel.SUBJECT -> subjectDraft
                            StudyFolderLevel.CHAPTER -> chapterDraft
                            StudyFolderLevel.TOPIC -> topicDraft
                            else -> ""
                        }
                        val candidates = filingCandidates(level)

                        FilingEditRow(
                            level = level,
                            value = currentValue,
                            candidates = candidates,
                            isExpanded = activeFilingLevel == level,
                            onToggle = { activeFilingLevel = if (activeFilingLevel == level) null else level },
                            onSelect = { v ->
                                when (level) {
                                    StudyFolderLevel.TYPE -> typeDraft = v
                                    StudyFolderLevel.SUBJECT -> subjectDraft = v
                                    StudyFolderLevel.CHAPTER -> chapterDraft = v
                                    StudyFolderLevel.TOPIC -> topicDraft = v
                                    else -> {}
                                }
                                activeFilingLevel = null
                            },
                            onValueChange = { v ->
                                when (level) {
                                    StudyFolderLevel.TYPE -> typeDraft = v
                                    StudyFolderLevel.SUBJECT -> subjectDraft = v
                                    StudyFolderLevel.CHAPTER -> chapterDraft = v
                                    StudyFolderLevel.TOPIC -> topicDraft = v
                                    else -> {}
                                }
                            }
                        )
                    }

                    Button(
                        onClick = {
                            val filing = StudyFilingPath(
                                type = typeDraft.ifEmpty { null },
                                subject = subjectDraft.ifEmpty { null },
                                chapter = chapterDraft.ifEmpty { null },
                                topic = topicDraft.ifEmpty { null }
                            )
                            recordingManager.updateStudyFiling(recordingID, filing)
                            studyLibraryStore.updateFiling(recordingID, filing)
                            statusMessage = "归档已保存"
                        },
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(24.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = RokuricsColors.aqua)
                    ) {
                        Text("保存归档", fontSize = 14.sp)
                    }

                    if (statusMessage != null) {
                        Text(statusMessage!!, fontSize = 13.sp, color = RokuricsColors.mint, modifier = Modifier.fillMaxWidth(), textAlign = TextAlign.Center)
                    }
                }
            }

            // Actions card
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(22.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.52f)),
                border = BorderStroke(0.5.dp, Color.White.copy(alpha = 0.22f))
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Text("操作", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = RokuricsColors.deepText)

                    val uploadStatus = recording.uploadStatus
                    val canUpload = uploadStatus != "uploaded" && uploadStatus != "uploading"

                    DetailActionButton(
                        icon = Icons.Default.CloudUpload,
                        label = when (uploadStatus) {
                            "uploaded" -> "已上传"
                            "uploading" -> "上传中..."
                            "pending" -> "等待传输"
                            "failed" -> "重新上传"
                            else -> "上传到 Mac"
                        },
                        tint = if (canUpload) RokuricsColors.aqua else RokuricsColors.softText,
                        enabled = canUpload,
                        onClick = { onUpload(recordingID) }
                    )

                    val hasTranscript = studyItem?.hasTranscript == true
                    val ts = recording.transcriptionStatus
                    val isTranscribing = ts == "queued" || ts == "running"

                    if (hasTranscript) {
                        DetailActionButton(
                            icon = Icons.Default.TextSnippet,
                            label = "查看转写文本",
                            tint = RokuricsColors.mint,
                            enabled = true,
                            onClick = { onOpenTranscript(recordingID) }
                        )
                    } else if (isTranscribing) {
                        DetailActionButton(
                            icon = Icons.Default.TextSnippet,
                            label = "转写中...",
                            tint = RokuricsColors.softText,
                            enabled = false,
                            onClick = {}
                        )
                    } else {
                        DetailActionButton(
                            icon = Icons.Default.TextSnippet,
                            label = "开始转写",
                            tint = RokuricsColors.mint,
                            enabled = true,
                            onClick = { onRemoteTranscribe(recordingID) }
                        )
                    }

                    val hasNote = studyItem?.hasNote == true
                    val ns = recording.noteStatus
                    val isGeneratingNote = ns == "queued" || ns == "running"

                    if (hasNote) {
                        DetailActionButton(
                            icon = Icons.Default.AutoAwesome,
                            label = "查看 AI 笔记",
                            tint = RokuricsColors.softTeal,
                            enabled = true,
                            onClick = { onOpenNote(recordingID) }
                        )
                    } else if (isGeneratingNote) {
                        DetailActionButton(
                            icon = Icons.Default.AutoAwesome,
                            label = "生成笔记中...",
                            tint = RokuricsColors.softText,
                            enabled = false,
                            onClick = {}
                        )
                    } else {
                        DetailActionButton(
                            icon = Icons.Default.AutoAwesome,
                            label = "生成 AI 笔记",
                            tint = RokuricsColors.softTeal,
                            enabled = hasTranscript,
                            onClick = { onGenerateNote(recordingID) }
                        )
                    }

                    DetailActionButton(
                        icon = Icons.Default.PlayArrow,
                        label = if (showPlayer) "收起播放器" else "播放录音",
                        tint = RokuricsColors.coral,
                        enabled = audioFilePath != null,
                        onClick = { showPlayer = !showPlayer }
                    )

                    if (showPlayer && audioFilePath != null) {
                        AudioPlayerBar(
                            audioFilePath = audioFilePath,
                            modifier = Modifier.padding(top = 4.dp)
                        )
                    }
                }
            }

            Spacer(Modifier.navigationBarsPadding())
            Spacer(Modifier.height(16.dp))
        }
    }

    // Rename dialog
    if (isRenaming) {
        AlertDialog(
            onDismissRequest = { isRenaming = false },
            title = { Text("重命名录音") },
            text = {
                OutlinedTextField(
                    value = renameDraft,
                    onValueChange = { renameDraft = it },
                    label = { Text("标题") },
                    singleLine = true
                )
            },
            confirmButton = {
                TextButton(onClick = {
                    recordingManager.renameRecording(recordingID, renameDraft)
                    isRenaming = false
                }) { Text("保存") }
            },
            dismissButton = {
                TextButton(onClick = { isRenaming = false }) { Text("取消") }
            }
        )
    }

    // Delete confirmation
    if (showDeleteConfirm) {
        AlertDialog(
            onDismissRequest = { showDeleteConfirm = false },
            title = { Text("删除录音") },
            text = { Text("确定要删除「${recording.title}」吗？录音将被移入废纸篓。") },
            confirmButton = {
                TextButton(onClick = {
                    recordingManager.deleteRecording(recordingID)
                    showDeleteConfirm = false
                    onBack()
                }) { Text("删除", color = RokuricsColors.coral) }
            },
            dismissButton = {
                TextButton(onClick = { showDeleteConfirm = false }) { Text("取消") }
            }
        )
    }
}

@Composable
private fun MetadataRow(label: String, value: String) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(label, fontSize = 14.sp, color = RokuricsColors.softText)
        Text(value, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = RokuricsColors.deepText)
    }
}

@Composable
private fun FilingEditRow(
    level: StudyFolderLevel,
    value: String,
    candidates: List<String>,
    isExpanded: Boolean,
    onToggle: () -> Unit,
    onSelect: (String) -> Unit,
    onValueChange: (String) -> Unit
) {
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .height(48.dp)
                .clip(RoundedCornerShape(16.dp))
                .background(RokuricsColors.aqua.copy(alpha = 0.06f))
                .border(1.dp, RokuricsColors.aqua.copy(alpha = 0.18f), RoundedCornerShape(16.dp))
                .clickable(onClick = onToggle)
                .padding(horizontal = 14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = level.title,
                fontSize = 13.sp,
                fontWeight = FontWeight.SemiBold,
                color = RokuricsColors.softText,
                modifier = Modifier.width(56.dp)
            )
            OutlinedTextField(
                value = value,
                onValueChange = onValueChange,
                modifier = Modifier.weight(1f),
                placeholder = { Text("选择或输入${level.title}", fontSize = 13.sp) },
                textStyle = LocalTextStyle.current.copy(fontSize = 13.sp),
                singleLine = true,
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = Color.Transparent,
                    unfocusedBorderColor = Color.Transparent
                )
            )
            Icon(
                if (isExpanded) Icons.Default.ExpandLess else Icons.Default.ExpandMore,
                contentDescription = null,
                tint = RokuricsColors.softText,
                modifier = Modifier.size(20.dp)
            )
        }

        if (isExpanded && candidates.isNotEmpty()) {
            Surface(
                shape = RoundedCornerShape(14.dp),
                color = Color.White.copy(alpha = 0.9f),
                shadowElevation = 4.dp
            ) {
                Column(modifier = Modifier.padding(8.dp)) {
                    candidates.take(8).forEach { candidate ->
                        val isSelected = value == candidate
                        Surface(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable { onSelect(candidate) }
                                .padding(vertical = 2.dp),
                            shape = RoundedCornerShape(10.dp),
                            color = if (isSelected) RokuricsColors.aqua.copy(alpha = 0.12f) else Color.Transparent
                        ) {
                            Text(
                                text = candidate,
                                fontSize = 13.sp,
                                fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Normal,
                                color = if (isSelected) RokuricsColors.aqua else RokuricsColors.deepText,
                                modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp)
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun DetailActionButton(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
    tint: Color,
    enabled: Boolean,
    onClick: () -> Unit
) {
    Surface(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(enabled = enabled, onClick = onClick),
        shape = RoundedCornerShape(16.dp),
        color = if (enabled) tint.copy(alpha = 0.08f) else Color.Transparent,
        border = if (enabled) androidx.compose.foundation.BorderStroke(1.dp, tint.copy(alpha = 0.22f))
        else androidx.compose.foundation.BorderStroke(1.dp, Color.Transparent)
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(icon, contentDescription = null, tint = if (enabled) tint else RokuricsColors.softText, modifier = Modifier.size(22.dp))
            Spacer(Modifier.width(12.dp))
            Text(
                text = label,
                fontSize = 14.sp,
                fontWeight = FontWeight.SemiBold,
                color = if (enabled) RokuricsColors.deepText else RokuricsColors.softText
            )
            Spacer(Modifier.weight(1f))
            Icon(Icons.Default.ChevronRight, contentDescription = null, tint = RokuricsColors.softText, modifier = Modifier.size(20.dp))
        }
    }
}

private fun formatDuration(seconds: Double): String {
    val totalSeconds = seconds.toInt().coerceAtLeast(0)
    val hours = totalSeconds / 3600
    val minutes = (totalSeconds % 3600) / 60
    val secs = totalSeconds % 60
    return if (hours > 0) "%d:%02d:%02d".format(hours, minutes, secs)
    else "%02d:%02d".format(minutes, secs)
}

private fun formatFileSize(bytes: Long): String {
    if (bytes < 1024) return "$bytes B"
    val kb = bytes / 1024.0
    if (kb < 1024) return "%.1f KB".format(kb)
    return "%.1f MB".format(kb / 1024.0)
}
