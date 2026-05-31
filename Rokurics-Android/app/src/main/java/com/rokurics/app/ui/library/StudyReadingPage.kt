package com.rokurics.app.ui.library

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
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.rokurics.app.data.AudioFileStore
import com.rokurics.app.data.StudyLibraryStore
import com.rokurics.app.domain.model.StudyItemMetadata
import com.rokurics.app.ui.theme.adaptivePageGradientBrush
import com.rokurics.app.ui.theme.RokuricsColors
import java.io.File

enum class StudyReadingKind { TRANSCRIPT, NOTE }

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StudyReadingPage(
    recordingID: String,
    kind: StudyReadingKind,
    studyLibraryStore: StudyLibraryStore,
    audioFileStore: AudioFileStore = remember { AudioFileStore() },
    onBack: () -> Unit
) {
    val item = studyLibraryStore.allStudyItems.find { it.recordingID == recordingID }

    var loadState by remember { mutableStateOf<ReadingLoadState>(ReadingLoadState.Loading) }
    val title = item?.title ?: "录音"
    val subtitle = when (kind) {
        StudyReadingKind.TRANSCRIPT -> "转写文本"
        StudyReadingKind.NOTE -> "AI 总结"
    }

    LaunchedEffect(recordingID, kind) {
        loadState = loadContent(item, kind, audioFileStore)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(title, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, maxLines = 1)
                        Text(subtitle, fontSize = 11.sp, color = RokuricsColors.softText)
                    }
                },
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
                .background(adaptivePageGradientBrush())
                .verticalScroll(rememberScrollState())
                .padding(20.dp)
        ) {
            when (loadState) {
                is ReadingLoadState.Loading -> {
                    Box(Modifier.fillMaxWidth().padding(48.dp), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            CircularProgressIndicator(color = RokuricsColors.aqua)
                            Spacer(Modifier.height(16.dp))
                            Text("正在读取...", color = RokuricsColors.softText, fontSize = 14.sp)
                        }
                    }
                }
                is ReadingLoadState.Error -> {
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(20.dp),
                        colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.7f))
                    ) {
                        Column(
                            modifier = Modifier.padding(24.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Icon(
                                Icons.Default.ErrorOutline,
                                contentDescription = null,
                                tint = RokuricsColors.coral,
                                modifier = Modifier.size(48.dp)
                            )
                            Spacer(Modifier.height(12.dp))
                            Text(
                                text = (loadState as ReadingLoadState.Error).message,
                                color = RokuricsColors.softText,
                                fontSize = 14.sp,
                                textAlign = TextAlign.Center
                            )
                        }
                    }
                }
                is ReadingLoadState.Loaded -> {
                    val content = (loadState as ReadingLoadState.Loaded).markdown

                    // Markdown rendering
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(22.dp),
                        colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.7f))
                    ) {
                        Column(modifier = Modifier.padding(20.dp)) {
                            Text(
                                text = when (kind) {
                                    StudyReadingKind.TRANSCRIPT -> "转写正文"
                                    StudyReadingKind.NOTE -> "总结正文"
                                },
                                fontSize = 17.sp,
                                fontWeight = FontWeight.Bold,
                                color = RokuricsColors.deepText
                            )
                            Spacer(Modifier.height(14.dp))

                            val blocks = parseMarkdownBlocks(content)
                            if (blocks.isEmpty()) {
                                Text("暂无内容", color = RokuricsColors.softText, fontSize = 14.sp)
                            } else {
                                blocks.forEach { block ->
                                    when (block) {
                                        is MarkdownBlock.Heading -> {
                                            val topPadding = if (block.level == 1) 8 else 12
                                            Spacer(Modifier.height(topPadding.dp))
                                            Text(
                                                text = block.text,
                                                fontSize = if (block.level == 1) 20.sp else 17.sp,
                                                fontWeight = FontWeight.SemiBold,
                                                color = RokuricsColors.deepText
                                            )
                                            Spacer(Modifier.height(6.dp))
                                        }
                                        is MarkdownBlock.Bullet -> {
                                            Row(modifier = Modifier.padding(start = 4.dp, top = 2.dp, bottom = 2.dp)) {
                                                Text("•", color = RokuricsColors.aqua, fontSize = 15.sp, fontWeight = FontWeight.Bold)
                                                Spacer(Modifier.width(9.dp))
                                                Text(
                                                    text = block.text,
                                                    fontSize = 15.sp,
                                                    color = RokuricsColors.deepText,
                                                    lineHeight = 22.sp
                                                )
                                            }
                                        }
                                        is MarkdownBlock.Paragraph -> {
                                            Text(
                                                text = block.text,
                                                fontSize = 15.sp,
                                                color = RokuricsColors.deepText,
                                                lineHeight = 24.sp,
                                                modifier = Modifier.padding(vertical = 3.dp)
                                            )
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Spacer(Modifier.navigationBarsPadding())
                    Spacer(Modifier.height(24.dp))
                }
            }
        }
    }
}

private sealed class ReadingLoadState {
    data object Loading : ReadingLoadState()
    data class Loaded(val markdown: String) : ReadingLoadState()
    data class Error(val message: String) : ReadingLoadState()
}

private sealed class MarkdownBlock {
    data class Heading(val level: Int, val text: String) : MarkdownBlock()
    data class Bullet(val text: String) : MarkdownBlock()
    data class Paragraph(val text: String) : MarkdownBlock()
}

private fun parseMarkdownBlocks(markdown: String): List<MarkdownBlock> {
    return markdown
        .replace("\r\n", "\n")
        .replace("\r", "\n")
        .lines()
        .mapNotNull { line ->
            val trimmed = line.trim()
            if (trimmed.isEmpty()) return@mapNotNull null

            if (trimmed.startsWith("### ")) MarkdownBlock.Heading(3, cleanText(trimmed.removePrefix("### ")))
            else if (trimmed.startsWith("## ")) MarkdownBlock.Heading(2, cleanText(trimmed.removePrefix("## ")))
            else if (trimmed.startsWith("# ")) MarkdownBlock.Heading(1, cleanText(trimmed.removePrefix("# ")))
            else if (trimmed.startsWith("- ") || trimmed.startsWith("* "))
                MarkdownBlock.Bullet(cleanText(trimmed.removePrefix("- ").removePrefix("* ")))
            else MarkdownBlock.Paragraph(cleanText(trimmed))
        }
        .filter { block ->
            when (block) {
                is MarkdownBlock.Heading -> block.text.isNotEmpty()
                is MarkdownBlock.Bullet -> block.text.isNotEmpty()
                is MarkdownBlock.Paragraph -> block.text.isNotEmpty()
            }
        }
}

private fun cleanText(text: String): String {
    return text.replace("**", "").replace("`", "").trim()
}

private fun loadContent(
    item: StudyItemMetadata?,
    kind: StudyReadingKind,
    audioFileStore: AudioFileStore
): ReadingLoadState {
    if (item == null) return ReadingLoadState.Error("未找到学习内容")

    return try {
        val baseDir = File(com.rokurics.app.RokuricsApp.instance.filesDir, "Rokurics")

        if (kind == StudyReadingKind.TRANSCRIPT) {
            val recordingID = item.recordingID ?: return ReadingLoadState.Error("未关联录音")
            // Transcript is stored as Metadata/<id>_transcript.json by RecordingManager
            val transcriptFile = audioFileStore.makeMetadataFile("${recordingID}_transcript")
            if (transcriptFile.exists()) ReadingLoadState.Loaded(transcriptFile.readText())
            else ReadingLoadState.Error("未找到转写文档")
        } else {
            // Note is stored relative to the study library root
            val notePath = item.noteRelativePath
                ?: return ReadingLoadState.Error("未找到笔记文档")
            val noteFile = File(baseDir, notePath)
            if (noteFile.exists()) ReadingLoadState.Loaded(noteFile.readText())
            else ReadingLoadState.Error("笔记文件不存在")
        }
    } catch (e: Exception) {
        ReadingLoadState.Error("无法读取: ${e.message}")
    }
}
