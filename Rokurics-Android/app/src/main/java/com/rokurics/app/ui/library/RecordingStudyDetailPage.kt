package com.rokurics.app.ui.library

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Article
import androidx.compose.material.icons.automirrored.filled.Chat
import androidx.compose.material.icons.automirrored.filled.TextSnippet
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
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
import com.rokurics.app.ui.theme.adaptivePageGradientBrush
import com.rokurics.app.ui.theme.rokuricsGlassCard
import com.rokurics.app.ui.theme.rokuricsScaleClickable
import java.text.SimpleDateFormat
import java.util.Locale

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
    val studyItem = studyLibraryStore.allStudyItems.find { it.recordingID == recordingID }

    if (recording == null) {
        Box(Modifier.fillMaxSize().background(adaptivePageGradientBrush()), contentAlignment = Alignment.Center) {
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
    var showFileInfo by remember { mutableStateOf(false) }
    val audioFilePath = remember(recordingID) { recordingManager.getAudioFilePath(recordingID) }
    val dateFormat = remember { SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.CHINA) }
    val allItems = studyLibraryStore.allStudyItems
    val allFolders = studyLibraryStore.allStudyFolders

    val saveFiling: () -> Unit = {
        val filing = StudyFilingPath(
            type = typeDraft.ifEmpty { null },
            subject = subjectDraft.ifEmpty { null },
            chapter = chapterDraft.ifEmpty { null },
            topic = topicDraft.ifEmpty { null }
        )
        recordingManager.updateStudyFiling(recordingID, filing)
        studyLibraryStore.updateFiling(recordingID, filing)
        statusMessage = "归档已保存"
    }

    val isTranscribing = recording.transcriptionStatus == "queued" || recording.transcriptionStatus == "running"
    val isGeneratingNote = recording.noteStatus == "queued" || recording.noteStatus == "running"
    val hasTranscript = studyItem?.hasTranscript == true
    val hasNote = studyItem?.hasNote == true

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
            v?.let { if (it.isNotEmpty()) values.add(it) }
        }
        for (folder in allFolders) {
            val v: String? = when (level) {
                StudyFolderLevel.TYPE -> folder.path.type
                StudyFolderLevel.SUBJECT -> folder.path.subject
                StudyFolderLevel.CHAPTER -> folder.path.chapter
                StudyFolderLevel.TOPIC -> folder.path.topic
                else -> null
            }
            v?.let { if (it.isNotEmpty()) values.add(it) }
        }
        return values.toList().sorted()
    }

    // Apple parity: page gradient + scrollable content (no Scaffold)
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(adaptivePageGradientBrush())
            .statusBarsPadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 20.dp, vertical = 16.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            // ── Header: back button + title + actions (Apple parity) ──
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.Top
            ) {
                // Glass circle back button
                Box(
                    modifier = Modifier
                        .size(42.dp)
                        .clip(CircleShape)
                        .background(if (isSystemInDarkTheme()) Color(0xFF0D2424).copy(alpha = 0.55f) else Color.White.copy(alpha = 0.46f))
                        .rokuricsScaleClickable(onClick = onBack),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        Icons.AutoMirrored.Filled.ArrowBack,
                        contentDescription = "返回",
                        tint = RokuricsColors.deepText,
                        modifier = Modifier.size(22.dp)
                    )
                }

                Spacer(modifier = Modifier.width(14.dp))

                // Title + subtitle
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = recording.title,
                        fontSize = 24.sp,
                        fontWeight = FontWeight.Bold,
                        color = RokuricsColors.deepText,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = "${dateFormat.format(recording.createdAt)} · ${formatDuration(recording.duration)}",
                        fontSize = 12.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = RokuricsColors.tertiaryText
                    )
                }

                Spacer(modifier = Modifier.width(8.dp))

                // Right action capsule (Apple parity: glass capsule with import + delete)
                Surface(
                    shape = RoundedCornerShape(22.dp),
                    color = Color.White.copy(alpha = 0.36f),
                    border = androidx.compose.foundation.BorderStroke(0.5.dp, Color.White.copy(alpha = 0.36f))
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        // Import to chat button
                        Box(
                            modifier = Modifier
                                .size(40.dp)
                                .rokuricsScaleClickable(onClick = { /* import to chat */ }),
                            contentAlignment = Alignment.Center
                        ) {
                            Icon(
                                Icons.AutoMirrored.Filled.Chat,
                                contentDescription = "导入 AI 对话",
                                tint = RokuricsColors.aqua,
                                modifier = Modifier.size(18.dp)
                            )
                        }
                        VerticalDivider(
                            modifier = Modifier.height(22.dp),
                            thickness = 0.5.dp,
                            color = Color.White.copy(alpha = 0.18f)
                        )
                        // Delete button
                        Box(
                            modifier = Modifier
                                .size(40.dp)
                                .rokuricsScaleClickable(onClick = { showDeleteConfirm = true }),
                            contentAlignment = Alignment.Center
                        ) {
                            Icon(
                                Icons.Default.Delete,
                                contentDescription = "删除",
                                tint = RokuricsColors.coral,
                                modifier = Modifier.size(18.dp)
                            )
                        }
                    }
                }
            }

            // ── Action grid: 2x2 (Apple parity: iPhone recording detail actions) ──
            // iOS labels: 上传, 转写/未转写, 总结/无总结, 重命名
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                // Upload button (Apple parity: first action)
                DetailGridButton(
                    modifier = Modifier.weight(1f),
                    title = "上传",
                    icon = Icons.Default.CloudUpload,
                    enabled = recording.uploadStatus != "uploaded" && recording.relativeAudioPath.isNotEmpty(),
                    onClick = { onUpload(recordingID) }
                )
                // Transcribe button (Apple parity: second action, shows status)
                DetailGridButton(
                    modifier = Modifier.weight(1f),
                    title = when {
                        isTranscribing -> "转写中"
                        hasTranscript -> "已转写"
                        else -> "未转写"
                    },
                    icon = if (hasTranscript) Icons.Default.CheckCircle else Icons.AutoMirrored.Filled.TextSnippet,
                    enabled = !isTranscribing && recording.relativeAudioPath.isNotEmpty(),
                    onClick = { onRemoteTranscribe(recordingID) }
                )
            }
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                // Summarize button (Apple parity: third action, shows status)
                DetailGridButton(
                    modifier = Modifier.weight(1f),
                    title = when {
                        isGeneratingNote -> "总结中"
                        hasNote -> "已总结"
                        else -> "无总结"
                    },
                    icon = if (hasNote) Icons.Default.CheckCircle else Icons.Default.AutoAwesome,
                    enabled = hasTranscript && !isGeneratingNote,
                    onClick = { onGenerateNote(recordingID) }
                )
                // Rename button (Apple parity: fourth action)
                DetailGridButton(
                    modifier = Modifier.weight(1f),
                    title = "重命名",
                    icon = Icons.Default.Edit,
                    enabled = true,
                    onClick = { isRenaming = true }
                )
            }

            // ── Filing card (Apple parity: glass card + level buttons) ──
            val isDark = isSystemInDarkTheme()
            val filingCardBg = if (isDark) Color(0xFF0D2424).copy(alpha = 0.55f) else Color.White.copy(alpha = 0.30f)
            val filingCardBorder = Color.White.copy(alpha = if (isDark) 0.08f else 0.24f)
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(18.dp),
                colors = CardDefaults.cardColors(containerColor = filingCardBg),
                border = BorderStroke(0.5.dp, filingCardBorder)
            ) {
                Column(
                    modifier = Modifier.padding(14.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    // Level buttons row (Apple parity: MacStudyFilingLevelButton)
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(6.dp)
                    ) {
                        val levels = listOf(StudyFolderLevel.TYPE, StudyFolderLevel.SUBJECT, StudyFolderLevel.CHAPTER, StudyFolderLevel.TOPIC)
                        levels.forEach { level ->
                            val currentValue = when (level) {
                                StudyFolderLevel.TYPE -> typeDraft
                                StudyFolderLevel.SUBJECT -> subjectDraft
                                StudyFolderLevel.CHAPTER -> chapterDraft
                                StudyFolderLevel.TOPIC -> topicDraft
                                else -> ""
                            }
                            val canActivate = when (level) {
                                StudyFolderLevel.TYPE -> true
                                StudyFolderLevel.SUBJECT -> typeDraft.isNotEmpty()
                                StudyFolderLevel.CHAPTER -> typeDraft.isNotEmpty() && subjectDraft.isNotEmpty()
                                StudyFolderLevel.TOPIC -> typeDraft.isNotEmpty() && subjectDraft.isNotEmpty() && chapterDraft.isNotEmpty()
                                else -> false
                            }
                            FilingLevelChip(
                                level = level,
                                value = currentValue,
                                isActive = activeFilingLevel == level,
                                isEnabled = canActivate,
                                modifier = Modifier.weight(1f),
                                onClick = {
                                    if (canActivate) {
                                        activeFilingLevel = if (activeFilingLevel == level) null else level
                                    }
                                }
                            )
                        }
                    }

                    // Expanded level: candidate chips + new value input
                    if (activeFilingLevel != null) {
                        val level = activeFilingLevel!!
                        val candidates = filingCandidates(level)
                        var newValueDraft by remember { mutableStateOf("") }

                        if (candidates.isNotEmpty()) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(6.dp)
                            ) {
                                candidates.take(8).forEach { candidate ->
                                    val isSelected = when (level) {
                                        StudyFolderLevel.TYPE -> typeDraft == candidate
                                        StudyFolderLevel.SUBJECT -> subjectDraft == candidate
                                        StudyFolderLevel.CHAPTER -> chapterDraft == candidate
                                        StudyFolderLevel.TOPIC -> topicDraft == candidate
                                        else -> false
                                    }
                                    Surface(
                                        modifier = Modifier.clickable {
                                            when (level) {
                                                StudyFolderLevel.TYPE -> typeDraft = candidate
                                                StudyFolderLevel.SUBJECT -> subjectDraft = candidate
                                                StudyFolderLevel.CHAPTER -> chapterDraft = candidate
                                                StudyFolderLevel.TOPIC -> topicDraft = candidate
                                                else -> {}
                                            }
                                            saveFiling()
                                            activeFilingLevel = null
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
                                            modifier = Modifier.padding(horizontal = 11.dp, vertical = 7.dp)
                                        )
                                    }
                                }
                            }
                        }

                        // New value input row
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clip(RoundedCornerShape(12.dp))
                                .background(RokuricsColors.aqua.copy(alpha = 0.06f))
                                .padding(horizontal = 10.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            OutlinedTextField(
                                value = newValueDraft,
                                onValueChange = { newValueDraft = it },
                                modifier = Modifier.weight(1f),
                                placeholder = { Text("新建${level.title}", fontSize = 13.sp) },
                                textStyle = LocalTextStyle.current.copy(fontSize = 13.sp),
                                singleLine = true,
                                colors = OutlinedTextFieldDefaults.colors(
                                    focusedBorderColor = Color.Transparent,
                                    unfocusedBorderColor = Color.Transparent
                                )
                            )
                            IconButton(
                                onClick = {
                                    val trimmed = newValueDraft.trim()
                                    if (trimmed.isNotEmpty()) {
                                        when (level) {
                                            StudyFolderLevel.TYPE -> typeDraft = trimmed
                                            StudyFolderLevel.SUBJECT -> subjectDraft = trimmed
                                            StudyFolderLevel.CHAPTER -> chapterDraft = trimmed
                                            StudyFolderLevel.TOPIC -> topicDraft = trimmed
                                            else -> {}
                                        }
                                        newValueDraft = ""
                                        saveFiling()
                                        activeFilingLevel = null
                                    }
                                },
                                enabled = newValueDraft.trim().isNotEmpty()
                            ) {
                                Icon(Icons.Default.Add, contentDescription = "新建", tint = if (newValueDraft.trim().isNotEmpty()) RokuricsColors.aqua else RokuricsColors.tertiaryText)
                            }
                        }
                    }

                    if (statusMessage != null) {
                        Text(statusMessage!!, fontSize = 12.sp, fontWeight = FontWeight.SemiBold, color = RokuricsColors.mint)
                    }
                }
            }

            // ── File info card (collapsible, Apple parity) ──
            val fileInfoBg = if (isDark) Color(0xFF0D2424).copy(alpha = 0.45f) else Color.White.copy(alpha = 0.24f)
            val fileInfoBorder = Color.White.copy(alpha = if (isDark) 0.06f else 0.20f)
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(18.dp),
                colors = CardDefaults.cardColors(containerColor = fileInfoBg),
                border = BorderStroke(0.5.dp, fileInfoBorder)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable { showFileInfo = !showFileInfo },
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text("文件信息", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = RokuricsColors.deepText)
                        Spacer(Modifier.weight(1f))
                        Icon(
                            if (showFileInfo) Icons.Default.ExpandLess else Icons.Default.ExpandMore,
                            contentDescription = null,
                            tint = RokuricsColors.softText
                        )
                    }

                    if (showFileInfo) {
                        Spacer(modifier = Modifier.height(12.dp))
                        DetailMetadataRow("日期", dateFormat.format(recording.createdAt))
                        DetailMetadataRow("时长", formatDuration(recording.duration))
                        DetailMetadataRow("大小", formatFileSize(recording.fileSize))
                        DetailMetadataRow("格式", "${recording.format} / ${recording.codec}")
                        DetailMetadataRow("采样率", "${recording.sampleRate.toInt()} Hz · ${if (recording.channels == 1) "单声道" else "立体声"}")
                        DetailMetadataRow("比特率", "${recording.bitrate / 1000} kbps")
                    }
                }
            }

            // ── Play button (Apple parity) ──
            if (audioFilePath != null) {
                Button(
                    onClick = { showPlayer = !showPlayer },
                    modifier = Modifier.fillMaxWidth().height(48.dp),
                    shape = RoundedCornerShape(24.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = RokuricsColors.coral)
                ) {
                    Icon(
                        if (showPlayer) Icons.Default.Pause else Icons.Default.PlayArrow,
                        contentDescription = null,
                        modifier = Modifier.size(18.dp)
                    )
                    Spacer(Modifier.width(8.dp))
                    Text(if (showPlayer) "收起播放器" else "播放录音", fontSize = 14.sp)
                }

                if (showPlayer) {
                    AudioPlayerBar(
                        audioFilePath = audioFilePath,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.navigationBarsPadding())
            Spacer(modifier = Modifier.height(16.dp))
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

// ── Detail Grid Button (Apple parity: MacStudyDetailActionButton) ──

@Composable
private fun DetailGridButton(
    modifier: Modifier = Modifier,
    title: String,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    enabled: Boolean,
    onClick: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    Box(
        modifier = modifier
            .clickable(enabled = enabled, onClick = onClick)
            .rokuricsGlassCard(
                cornerRadius = 14.dp,
                fillOpacity = if (enabled) 0.28f else 0.16f,
                strokeOpacity = if (enabled) 0.24f else 0.12f,
                shadowOpacity = if (enabled) 0.04f else 0.02f,
                shadowRadius = 6.dp,
                fillColor = if (isDark) RokuricsColors.glassSurfaceDark else RokuricsColors.glassSurface
            ),
        contentAlignment = Alignment.Center
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                icon,
                contentDescription = null,
                tint = if (enabled) RokuricsColors.deepText else RokuricsColors.tertiaryText,
                modifier = Modifier.size(18.dp)
            )
            Spacer(Modifier.width(8.dp))
            Text(
                text = title,
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                color = if (enabled) RokuricsColors.deepText else RokuricsColors.tertiaryText,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
}

// ── Filing Level Chip (Apple parity: MacStudyFilingLevelButton) ──

@Composable
private fun FilingLevelChip(
    level: StudyFolderLevel,
    value: String,
    isActive: Boolean,
    isEnabled: Boolean,
    modifier: Modifier = Modifier,
    onClick: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    Surface(
        modifier = modifier.clickable(enabled = isEnabled, onClick = onClick),
        shape = RoundedCornerShape(13.dp),
        color = if (isActive) RokuricsColors.aqua.copy(alpha = 0.24f) else RokuricsColors.aqua.copy(alpha = 0.10f),
        border = BorderStroke(
            0.5.dp,
            if (isActive) RokuricsColors.aqua.copy(alpha = 0.42f)
            else Color.White.copy(alpha = if (isDark) 0.06f else 0.14f)
        )
    ) {
        Column(modifier = Modifier.padding(horizontal = 10.dp, vertical = 8.dp)) {
            Text(
                text = level.title,
                fontSize = 10.sp,
                fontWeight = FontWeight.Bold,
                color = RokuricsColors.tertiaryText
            )
            Text(
                text = value.ifEmpty { "未选择" },
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                color = if (isEnabled) RokuricsColors.deepText else RokuricsColors.tertiaryText,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
}

// ── Detail Metadata Row ──

@Composable
private fun DetailMetadataRow(label: String, value: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(label, fontSize = 14.sp, color = RokuricsColors.softText)
        Text(value, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = RokuricsColors.deepText)
    }
}

// ── Format Helpers ──

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
