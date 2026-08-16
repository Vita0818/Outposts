package com.vita0818.kikaria.ui.preset

import android.content.Context
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.data.KnowledgePreset
import com.vita0818.kikaria.ui.components.KikariaEditorTextField
import com.vita0818.kikaria.ui.components.KikariaFormPageShell
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaTagChip
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics
import com.vita0818.kikaria.util.MarkdownParser
import java.util.Date
import java.util.UUID

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun EditPresetScreen(
    preset: KnowledgePreset,
    knowledgePoints: List<KnowledgePoint>,
    onBack: () -> Unit,
    onSavePreset: (name: String, category: String, markdownText: String) -> Unit,
    onAddPoint: () -> Unit,
    onEditPoint: (KnowledgePoint) -> Unit,
    onDeletePoint: (KnowledgePoint) -> Unit,
    onDeletePreset: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val context = LocalContext.current

    var name by remember(preset.id) { mutableStateOf(preset.name) }
    var category by remember(preset.id) { mutableStateOf(preset.category) }
    var searchText by remember { mutableStateOf("") }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var pendingDeletePoint by remember { mutableStateOf<KnowledgePoint?>(null) }
    var isConfirmingPresetDelete by remember { mutableStateOf(false) }

    val markdownText = remember(knowledgePoints) {
        MarkdownParser.markdownFromPoints(knowledgePoints)
    }
    val filteredPoints = remember(knowledgePoints, searchText) {
        val query = searchText.trim()
        if (query.isEmpty()) {
            knowledgePoints
        } else {
            knowledgePoints.filter { point ->
                point.title.contains(query, ignoreCase = true) ||
                    point.tags.any { it.contains(query, ignoreCase = true) } ||
                    point.hint.contains(query, ignoreCase = true) ||
                    point.content.contains(query, ignoreCase = true)
            }
        }
    }

    val exportLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.CreateDocument("text/markdown")
    ) { uri: Uri? ->
        if (uri != null && !writeMarkdownExport(context, uri, markdownText)) {
            errorMessage = "导出失败。"
        }
    }

    val doSave: () -> Unit = {
        val trimmedName = name.trim()
        if (trimmedName.isEmpty()) {
            errorMessage = "请填写预设名称。"
        } else {
            onSavePreset(trimmedName, category.trim(), markdownText)
        }
    }

    val metrics = rememberKikariaPhoneMetrics()

    KikariaFormPageShell(
        title = "编辑预设",
        onBack = onBack,
        metrics = metrics,
        closeIcon = KikariaIcons.back,
        actionLabel = "保存",
        onAction = doSave
    ) {
        Spacer(modifier = Modifier.height(8.dp))
        KikariaEditorTextField("预设名称", name, { name = it }, isDark, true)
        Spacer(modifier = Modifier.height(10.dp))
        KikariaEditorTextField("分类", category, { category = it }, isDark, true)

        Spacer(modifier = Modifier.height(16.dp))
        PresetEditorActionButton(
            text = "导出 Markdown",
            icon = KikariaIcons.share,
            textColor = deepText,
            background = if (isDark) KikariaColors.GlassSurfaceDark.copy(alpha = 0.44f)
            else KikariaColors.GlassSurface.copy(alpha = 0.44f),
            shadowColor = sky.copy(alpha = 0.10f),
            onClick = {
                exportLauncher.launch("Kikaria-${sanitizedFilename(name.ifBlank { preset.name })}.md")
            }
        )

        Spacer(modifier = Modifier.height(10.dp))
        PresetEditorActionButton(
            text = "添加知识点",
            icon = KikariaIcons.add,
            textColor = Color.White,
            backgroundBrush = actionGrad,
            shadowColor = sky.copy(alpha = 0.20f),
            onClick = onAddPoint
        )

        Spacer(modifier = Modifier.height(16.dp))
        PresetSearchBar(
            searchText = searchText,
            onSearchTextChange = { searchText = it },
            placeholder = "搜索知识点",
            isDark = isDark
        )

        Spacer(modifier = Modifier.height(16.dp))
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                KikariaTypography.mixedText("知识点", size = 14, weight = FontWeight.SemiBold),
                color = softText,
                modifier = Modifier.padding(start = 4.dp)
            )
            Text(
                "${filteredPoints.size} / ${knowledgePoints.size}",
                fontSize = 12.sp,
                fontWeight = FontWeight.SemiBold,
                color = sky
            )
        }

        Spacer(modifier = Modifier.height(8.dp))
        if (filteredPoints.isEmpty()) {
            KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 24.dp, fillOpacity = 0.42f) {
                Column(
                    modifier = Modifier.padding(22.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text("没有找到相关知识点", fontSize = 18.sp, fontWeight = FontWeight.Bold, color = deepText)
                    Spacer(modifier = Modifier.height(6.dp))
                    Text("换个关键词试试看。", fontSize = 14.sp, color = softText)
                }
            }
        } else {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                filteredPoints.forEach { point ->
                    EditableKnowledgePointCard(
                        point = point,
                        isDark = isDark,
                        onEdit = { onEditPoint(point) },
                        onDelete = { pendingDeletePoint = point }
                    )
                }
            }
        }

        pendingDeletePoint?.let { point ->
            Spacer(modifier = Modifier.height(14.dp))
            ConfirmCard(
                title = "删除知识点？",
                subtitle = "删除后，该知识点的重点集锦、已掌握和今日复习次数也会一并移除。",
                confirmText = "删除",
                isDark = isDark,
                onCancel = { pendingDeletePoint = null },
                onConfirm = {
                    onDeletePoint(point)
                    pendingDeletePoint = null
                }
            )
        }

        if (!preset.isBuiltIn) {
            Spacer(modifier = Modifier.height(16.dp))
            PresetEditorActionButton(
                text = "删除此预设",
                icon = KikariaIcons.delete,
                textColor = removeCoral,
                background = if (isDark) KikariaColors.GlassSurfaceDark.copy(alpha = 0.40f)
                else KikariaColors.GlassSurface.copy(alpha = 0.40f),
                shadowColor = removeCoral.copy(alpha = 0.10f),
                onClick = { isConfirmingPresetDelete = true }
            )
        }

        if (isConfirmingPresetDelete) {
            Spacer(modifier = Modifier.height(14.dp))
            ConfirmCard(
                title = "删除预设？",
                subtitle = "此操作会删除该自定义预设和它的学习状态。",
                confirmText = "删除",
                isDark = isDark,
                onCancel = { isConfirmingPresetDelete = false },
                onConfirm = {
                    onDeletePreset()
                    isConfirmingPresetDelete = false
                }
            )
        }

        errorMessage?.let { message ->
            Spacer(modifier = Modifier.height(12.dp))
            KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 18.dp, fillOpacity = 0.50f) {
                Text(message, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = removeCoral, modifier = Modifier.padding(14.dp))
            }
        }

        Spacer(modifier = Modifier.height(32.dp))
    }
}

@Composable
fun EditKnowledgePointScreen(
    presetName: String,
    point: KnowledgePoint?,
    onBack: () -> Unit,
    onSave: (KnowledgePoint) -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral

    var title by remember(point?.id) { mutableStateOf(point?.title.orEmpty()) }
    var tagsText by remember(point?.id) { mutableStateOf(point?.tags?.joinToString(", ").orEmpty()) }
    var hint by remember(point?.id) { mutableStateOf(point?.hint.orEmpty()) }
    var content by remember(point?.id) { mutableStateOf(point?.content.orEmpty()) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    val metrics = rememberKikariaPhoneMetrics()

    val doSave: () -> Unit = {
        val trimmedTitle = title.trim()
        val trimmedHint = hint.trim()
        val trimmedContent = content.trim()
        if (trimmedTitle.isEmpty() || trimmedHint.isEmpty() || trimmedContent.isEmpty()) {
            errorMessage = "标题、提示和答案都不能为空。"
        } else {
            val now = Date()
            val savedPoint = KnowledgePoint(
                id = point?.id ?: UUID.randomUUID().toString(),
                title = trimmedTitle,
                tags = tagsText.split(",", "，")
                    .map { it.trim() }
                    .filter { it.isNotEmpty() },
                hint = trimmedHint,
                content = trimmedContent,
                reinforcementCount = point?.reinforcementCount ?: 0,
                lastReinforcedAt = point?.lastReinforcedAt,
                isMastered = point?.isMastered ?: false,
                createdAt = point?.createdAt ?: now,
                updatedAt = now
            )
            onSave(savedPoint)
        }
    }

    KikariaFormPageShell(
        title = if (point == null) "添加知识点" else "编辑知识点",
        onBack = onBack,
        metrics = metrics,
        closeIcon = KikariaIcons.back,
        actionLabel = "保存",
        onAction = doSave
    ) {
        Spacer(modifier = Modifier.height(8.dp))
        Text(
            KikariaTypography.mixedText(presetName, size = 24, weight = FontWeight.SemiBold),
            color = deepText,
            modifier = Modifier.padding(start = 4.dp, bottom = 14.dp)
        )
        KikariaEditorTextField("标题", title, { title = it }, isDark, true)
        Spacer(modifier = Modifier.height(12.dp))
        KikariaEditorTextField("标签，用逗号分隔", tagsText, { tagsText = it }, isDark, true)
        Spacer(modifier = Modifier.height(12.dp))
        KnowledgeLongTextField("提示", hint, { hint = it }, 150.dp, isDark)
        Spacer(modifier = Modifier.height(12.dp))
        KnowledgeLongTextField("答案", content, { content = it }, 220.dp, isDark)
        errorMessage?.let { message ->
            Spacer(modifier = Modifier.height(12.dp))
            KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 18.dp, fillOpacity = 0.50f) {
                Text(message, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = removeCoral, modifier = Modifier.padding(14.dp))
            }
        }
        Spacer(modifier = Modifier.height(32.dp))
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun EditableKnowledgePointCard(
    point: KnowledgePoint,
    isDark: Boolean,
    onEdit: () -> Unit,
    onDelete: () -> Unit
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral

    KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 22.dp, fillOpacity = 0.42f, shadowElevation = 12.dp) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Column(modifier = Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(7.dp)) {
                Text(
                    KikariaTypography.mixedText(point.title, size = 16, weight = FontWeight.SemiBold),
                    color = deepText,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
                if (point.tags.isNotEmpty()) {
                    FlowRow(horizontalArrangement = Arrangement.spacedBy(5.dp), verticalArrangement = Arrangement.spacedBy(5.dp)) {
                        point.tags.forEach { tag ->
                            KikariaTagChip(tag = tag, fontSize = 10)
                        }
                    }
                } else {
                    Text("无标签", fontSize = 12.sp, fontWeight = FontWeight.Medium, color = softText)
                }
            }
            CircleIconButton(KikariaIcons.edit, "编辑", sky, isDark, onEdit)
            CircleIconButton(KikariaIcons.delete, "删除", removeCoral, isDark, onDelete)
        }
    }
}

@Composable
private fun PresetEditorActionButton(
    text: String,
    icon: ImageVector,
    textColor: Color,
    shadowColor: Color,
    onClick: () -> Unit,
    background: Color? = null,
    backgroundBrush: Brush? = null
) {
    val shape = RoundedCornerShape(22.dp)
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(14.dp, shape, ambientColor = shadowColor, spotColor = shadowColor)
            .clip(shape)
            .then(
                when {
                    backgroundBrush != null -> Modifier.background(backgroundBrush)
                    background != null -> Modifier.background(background)
                    else -> Modifier
                }
            )
            .clickable { onClick() }
            .padding(vertical = 15.dp),
        contentAlignment = Alignment.Center
    ) {
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.Center) {
            Icon(icon, contentDescription = null, modifier = Modifier.size(19.dp), tint = textColor)
            Spacer(modifier = Modifier.width(8.dp))
            Text(text, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, color = textColor)
        }
    }
}

@Composable
private fun CircleIconButton(
    icon: ImageVector,
    contentDescription: String,
    tint: Color,
    isDark: Boolean,
    onClick: () -> Unit
) {
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shape = RoundedCornerShape(17.dp)
    Box(
        modifier = Modifier
            .size(34.dp)
            .shadow(7.dp, shape, ambientColor = tint.copy(alpha = 0.08f), spotColor = tint.copy(alpha = 0.08f))
            .clip(shape)
            .background(glassSurface.copy(alpha = 0.36f))
            .clickable { onClick() },
        contentAlignment = Alignment.Center
    ) {
        Icon(icon, contentDescription = contentDescription, modifier = Modifier.size(18.dp), tint = tint)
    }
}

@Composable
private fun PresetSearchBar(
    searchText: String,
    onSearchTextChange: (String) -> Unit,
    placeholder: String,
    isDark: Boolean
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shape = RoundedCornerShape(22.dp)

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(12.dp, shape, ambientColor = sky.copy(alpha = 0.08f), spotColor = sky.copy(alpha = 0.08f))
            .clip(shape)
            .background(glassSurface.copy(alpha = 0.44f))
            .padding(horizontal = 16.dp, vertical = 14.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(KikariaIcons.search, contentDescription = "搜索", modifier = Modifier.size(20.dp), tint = softText)
            Spacer(modifier = Modifier.width(10.dp))
            BasicTextField(
                value = searchText,
                onValueChange = onSearchTextChange,
                textStyle = TextStyle(fontSize = 15.sp, fontWeight = FontWeight.Medium, color = deepText),
                modifier = Modifier.weight(1f),
                decorationBox = { innerTextField ->
                    Box {
                        if (searchText.isEmpty()) {
                            Text(placeholder, fontSize = 15.sp, fontWeight = FontWeight.Medium, color = softText.copy(alpha = 0.6f))
                        }
                        innerTextField()
                    }
                }
            )
            if (searchText.isNotEmpty()) {
                Box(
                    modifier = Modifier
                        .clip(RoundedCornerShape(12.dp))
                        .clickable { onSearchTextChange("") }
                        .padding(4.dp)
                ) {
                    Icon(KikariaIcons.close, contentDescription = "清空搜索", modifier = Modifier.size(15.dp), tint = softText)
                }
            }
        }
    }
}

@Composable
private fun KnowledgeLongTextField(
    title: String,
    value: String,
    onValueChange: (String) -> Unit,
    minHeight: Dp,
    isDark: Boolean
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    Column {
        Text(
            KikariaTypography.mixedText(title, size = 14, weight = FontWeight.SemiBold),
            color = softText,
            modifier = Modifier.padding(start = 4.dp, bottom = 8.dp)
        )
        KikariaGlassCard(Modifier.fillMaxWidth().height(minHeight), cornerRadius = 22.dp, fillOpacity = 0.56f) {
            TextField(
                value = value,
                onValueChange = onValueChange,
                textStyle = TextStyle(
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Normal,
                    fontFamily = FontFamily.Default,
                    color = deepText,
                    lineHeight = 21.sp
                ),
                colors = TextFieldDefaults.colors(
                    focusedContainerColor = Color.Transparent,
                    unfocusedContainerColor = Color.Transparent,
                    focusedIndicatorColor = Color.Transparent,
                    unfocusedIndicatorColor = Color.Transparent,
                    cursorColor = sky
                ),
                keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.None),
                modifier = Modifier.fillMaxWidth().padding(14.dp)
            )
        }
    }
}

@Composable
private fun ConfirmCard(
    title: String,
    subtitle: String,
    confirmText: String,
    isDark: Boolean,
    onCancel: () -> Unit,
    onConfirm: () -> Unit
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val removeGradient = if (isDark) KikariaColors.RemoveGradientDark else KikariaColors.RemoveGradientLight
    val mist = if (isDark) KikariaColors.MistDark else KikariaColors.Mist

    KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 20.dp, fillOpacity = 0.50f) {
        Column(modifier = Modifier.padding(18.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            Text(title, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, color = deepText)
            Spacer(modifier = Modifier.height(5.dp))
            Text(subtitle, fontSize = 13.sp, fontWeight = FontWeight.Medium, color = softText)
            Spacer(modifier = Modifier.height(14.dp))
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                Box(
                    modifier = Modifier
                        .weight(1f)
                        .clip(RoundedCornerShape(14.dp))
                        .background(mist.copy(alpha = 0.60f))
                        .clickable { onCancel() }
                        .padding(vertical = 13.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text("取消", fontSize = 15.sp, fontWeight = FontWeight.SemiBold, color = deepText)
                }
                Box(
                    modifier = Modifier
                        .weight(1f)
                        .clip(RoundedCornerShape(14.dp))
                        .background(removeGradient)
                        .clickable { onConfirm() }
                        .padding(vertical = 13.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(confirmText, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, color = Color.White)
                }
            }
        }
    }
}

private fun writeMarkdownExport(context: Context, uri: Uri, markdownText: String): Boolean {
    return try {
        context.contentResolver.openOutputStream(uri)?.use { stream ->
            stream.write(markdownText.toByteArray(Charsets.UTF_8))
        } != null
    } catch (_: Exception) {
        false
    }
}

private fun sanitizedFilename(name: String): String {
    return name.trim()
        .ifEmpty { "Preset" }
        .replace(Regex("[^A-Za-z0-9_\\-\\u4e00-\\u9fa5]+"), "-")
        .trim('-')
        .ifEmpty { "Preset" }
}
