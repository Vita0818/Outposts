package com.vita0818.kikaria.ui.pages

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.IosShare
import androidx.compose.material.icons.filled.RemoveCircle
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import com.vita0818.kikaria.AppModel
import com.vita0818.kikaria.Routes
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.ui.GlassCard
import com.vita0818.kikaria.ui.GradientCapsuleButton
import com.vita0818.kikaria.ui.GlassIconButton
import com.vita0818.kikaria.ui.KikariaSearchBar
import com.vita0818.kikaria.ui.PageHeader
import com.vita0818.kikaria.ui.theme.kikariaColors
import java.io.File
import java.util.UUID

/** 预设列表:切换/编辑/删除。 */
@Composable
fun PresetSelectionPage(navController: NavController) {
    val colors = kikariaColors()
    var switchTarget by remember { mutableStateOf<String?>(null) }
    var deleteTarget by remember { mutableStateOf<String?>(null) }

    Column(Modifier.fillMaxSize().padding(horizontal = 24.dp)) {
        PageHeader(title = "切换预设", onBack = { navController.popBackStack() })
        GradientCapsuleButton(
            text = "上传新预设",
            icon = Icons.Filled.Add,
            gradient = colors.actionGradient,
        ) { navController.navigate(Routes.NEW_PRESET) }
        Spacer(Modifier.height(16.dp))

        LazyColumn(verticalArrangement = Arrangement.spacedBy(12.dp), modifier = Modifier.weight(1f)) {
            items(AppModel.presets, key = { it.id }) { preset ->
                val isCurrent = preset.id == AppModel.currentPresetID
                GlassCard(cornerRadius = 26, fillAlpha = 0.42f, modifier = Modifier.fillMaxWidth()) {
                    Column(
                        Modifier
                            .fillMaxWidth()
                            .clickable { switchTarget = preset.id }
                            .padding(18.dp),
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Text(
                                preset.name,
                                color = colors.deepText,
                                fontSize = 20.sp,
                                fontWeight = FontWeight.SemiBold,
                                modifier = Modifier.weight(1f),
                                maxLines = 1,
                                overflow = TextOverflow.Ellipsis,
                            )
                            if (isCurrent) {
                                GlassCard(cornerRadius = 50, fillAlpha = 0.4f, strokeAlpha = 0.3f) {
                                    Text(
                                        "当前",
                                        color = colors.sky,
                                        fontSize = 11.sp,
                                        fontWeight = FontWeight.Bold,
                                        modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
                                    )
                                }
                            }
                            Spacer(Modifier.width(8.dp))
                            GlassIconButton(icon = Icons.Filled.Edit, size = 34) {
                                navController.navigate(Routes.editPreset(preset.id))
                            }
                            GlassIconButton(icon = Icons.Filled.Delete, size = 34) { deleteTarget = preset.id }
                        }
                        Spacer(Modifier.height(6.dp))
                        Text("${preset.knowledgePointCount} 个知识点", color = colors.softText, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                    }
                }
            }
        }
        Spacer(Modifier.height(20.dp))
    }

    switchTarget?.let { targetId ->
        AlertDialog(
            onDismissRequest = { switchTarget = null },
            title = { Text("切换预设？") },
            text = { Text("将切换到另一套知识点。当前预设的学习进度会被保留。") },
            confirmButton = {
                TextButton(onClick = {
                    AppModel.switchToPreset(targetId)
                    switchTarget = null
                }) { Text("确认切换") }
            },
            dismissButton = {
                TextButton(onClick = { switchTarget = null }) { Text("取消") }
            },
        )
    }

    deleteTarget?.let { targetId ->
        val preset = AppModel.presets.firstOrNull { it.id == targetId }
        AlertDialog(
            onDismissRequest = { deleteTarget = null },
            title = { Text("删除预设？") },
            text = { Text("删除后将移除该预设的所有知识点、重点集锦、已掌握状态和学习记录。") },
            confirmButton = {
                TextButton(onClick = {
                    AppModel.deletePreset(targetId)
                    deleteTarget = null
                }) { Text("删除", color = colors.removeCoral) }
            },
            dismissButton = {
                TextButton(onClick = { deleteTarget = null }) { Text("取消") }
            },
        )
        if (preset == null) deleteTarget = null
    }
}

/** 新建预设:名称/分类 + 文件导入 + Markdown 文本。 */
@Composable
fun NewPresetPage(navController: NavController) {
    val colors = kikariaColors()
    val context = LocalContext.current
    var name by remember { mutableStateOf("") }
    var category by remember { mutableStateOf("") }
    var markdown by remember { mutableStateOf("") }

    val fileLauncher = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        if (uri != null) {
            runCatching {
                requireNotNull(context.contentResolver.openInputStream(uri)).use { it.bufferedReader().readText() }
            }.onSuccess { text ->
                markdown = text
                if (name.isBlank()) {
                    uri.lastPathSegment?.substringAfterLast('/')?.substringBeforeLast('.')?.let { name = it }
                }
            }.onFailure {
                AppModel.showToast("文件读取失败，请确认它是 UTF-8 文本。")
            }
        }
    }

    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp),
    ) {
        PageHeader(
            title = "上传新预设",
            onBack = { navController.popBackStack() },
            trailing = {
                Text(
                    "保存",
                    color = colors.sky,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier
                        .clickable {
                            if (AppModel.createPreset(name, category, markdown)) {
                                navController.popBackStack()
                            }
                        }
                        .padding(8.dp),
                )
            },
        )
        ProfileField("预设名称", name) { name = it }
        Spacer(Modifier.height(12.dp))
        ProfileField("分类", category) { category = it }
        Spacer(Modifier.height(16.dp))
        GradientCapsuleButton(
            text = "选择 .md / .txt 文件",
            icon = Icons.Filled.Description,
            gradient = colors.actionGradient,
            fontSize = 15,
        ) {
            fileLauncher.launch(arrayOf("text/markdown", "text/plain", "text/*", "application/octet-stream"))
        }
        Spacer(Modifier.height(16.dp))
        Text("Markdown 文本", color = colors.softText, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        LongTextField(value = markdown, onValueChange = { markdown = it }, minLines = 10)
        Spacer(Modifier.height(10.dp))
        Text(
            "如何编写 Markdown 预设？",
            color = colors.sky,
            fontSize = 14.sp,
            fontWeight = FontWeight.Medium,
            modifier = Modifier.clickable { navController.navigate(Routes.MARKDOWN_GUIDE) },
        )
        Spacer(Modifier.height(30.dp))
    }
}

@Composable
fun LongTextField(value: String, onValueChange: (String) -> Unit, minLines: Int = 6) {
    val colors = kikariaColors()
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        modifier = Modifier.fillMaxWidth().height((minLines * 26).dp),
        shape = RoundedCornerShape(22.dp),
        textStyle = androidx.compose.ui.text.TextStyle(
            fontSize = 16.sp,
            fontFamily = FontFamily.Serif,
            color = colors.deepText,
        ),
        colors = OutlinedTextFieldDefaults.colors(
            focusedBorderColor = colors.sky,
            unfocusedBorderColor = colors.blueGray.copy(alpha = 0.4f),
            focusedContainerColor = colors.glassSurface.copy(alpha = 0.35f),
            unfocusedContainerColor = colors.glassSurface.copy(alpha = 0.35f),
        ),
    )
}

/** 编辑预设:元数据 + 导出 + 知识点列表(仅当前预设可改知识点)。 */
@Composable
fun EditPresetPage(navController: NavController, presetId: String) {
    val colors = kikariaColors()
    val context = LocalContext.current
    val preset = AppModel.presets.firstOrNull { it.id == presetId }
    var name by remember(presetId) { mutableStateOf(preset?.name ?: "") }
    var category by remember(presetId) { mutableStateOf(preset?.category ?: "") }
    var query by remember { mutableStateOf("") }
    var deletePointTarget by remember { mutableStateOf<KnowledgePoint?>(null) }

    if (preset == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) {
            PageHeader(title = "编辑预设", onBack = { navController.popBackStack() })
            com.vita0818.kikaria.ui.SoftEmptyState(
                icon = Icons.Filled.Description,
                title = "预设不存在",
                subtitle = "请返回后重新选择预设。",
            )
        }
        return
    }

    val isCurrent = preset.id == AppModel.currentPresetID
    val filteredPoints = if (query.isBlank()) AppModel.knowledgePoints else AppModel.knowledgePoints.filter {
        it.title.lowercase().contains(query.trim().lowercase()) ||
            it.tags.any { tag -> tag.lowercase().contains(query.trim().lowercase()) }
    }

    Column(Modifier.fillMaxSize().padding(horizontal = 24.dp)) {
        PageHeader(
            title = "编辑预设",
            onBack = { navController.popBackStack() },
            trailing = {
                Text(
                    "保存",
                    color = colors.sky,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier
                        .clickable {
                            AppModel.updatePresetMetadata(preset.id, name, category)
                            navController.popBackStack()
                        }
                        .padding(8.dp),
                )
            },
        )
        Column(Modifier.verticalScroll(rememberScrollState())) {
            Text(preset.name, color = colors.deepText, fontSize = 26.sp, fontWeight = FontWeight.SemiBold)
            Spacer(Modifier.height(14.dp))
            ProfileField("预设名称", name) { name = it }
            Spacer(Modifier.height(12.dp))
            ProfileField("分类", category) { category = it }
            Spacer(Modifier.height(16.dp))
            GradientCapsuleButton(
                text = "导出 Markdown",
                icon = Icons.Filled.IosShare,
                gradient = colors.actionGradient,
                fontSize = 15,
            ) {
                runCatching {
                    val dir = File(context.getExternalFilesDir(null), "exports")
                    dir.mkdirs()
                    val safeName = preset.name.replace(Regex("[/\\\\?%*|\"<>:\n]"), "-").ifEmpty { "预设" }
                    val file = File(dir, "Kikaria-$safeName.md")
                    file.writeText(preset.markdownText)
                    AppModel.showToast("导出文件已准备好：${file.absolutePath}")
                }.onFailure {
                    AppModel.showToast("导出失败")
                }
            }
            Spacer(Modifier.height(16.dp))
            GradientCapsuleButton(
                text = "添加知识点",
                icon = Icons.Filled.Add,
                gradient = colors.masteredActionGradient,
                fontSize = 15,
            ) {
                navController.navigate(Routes.editPoint(preset.id, null))
            }
            Spacer(Modifier.height(16.dp))
            KikariaSearchBar(value = query, onValueChange = { query = it }, placeholder = "搜索知识点")
            Spacer(Modifier.height(12.dp))

            if (!isCurrent) {
                Text(
                    "知识点编辑仅对当前预设开放。切换到该预设后即可修改。",
                    color = colors.softText,
                    fontSize = 13.sp,
                )
            } else {
                LazyColumn(Modifier.height(((filteredPoints.size.coerceAtMost(8)) * 84).dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    items(filteredPoints, key = { it.id }) { point ->
                        GlassCard(cornerRadius = 22, fillAlpha = 0.4f, modifier = Modifier.fillMaxWidth()) {
                            Row(
                                Modifier.fillMaxWidth().padding(horizontal = 14.dp, vertical = 10.dp),
                                verticalAlignment = Alignment.CenterVertically,
                            ) {
                                Column(Modifier.weight(1f)) {
                                    Text(point.title, color = colors.deepText, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, maxLines = 1, overflow = TextOverflow.Ellipsis)
                                    if (point.tags.isNotEmpty()) {
                                        Spacer(Modifier.height(2.dp))
                                        Text(point.tags.joinToString(" · "), color = colors.softText, fontSize = 12.sp, maxLines = 2, overflow = TextOverflow.Ellipsis)
                                    }
                                }
                                GlassIconButton(icon = Icons.Filled.Edit, size = 34) {
                                    navController.navigate(Routes.editPoint(preset.id, point.id))
                                }
                                GlassIconButton(icon = Icons.Filled.Delete, size = 34) { deletePointTarget = point }
                            }
                        }
                    }
                }
            }
            Spacer(Modifier.height(16.dp))

            if (!preset.isBuiltIn) {
                GlassCard(cornerRadius = 22, fillAlpha = 0.3f, strokeAlpha = 0.4f, modifier = Modifier.fillMaxWidth()) {
                    Text(
                        "删除此预设",
                        color = colors.removeCoral,
                        fontSize = 15.sp,
                        fontWeight = FontWeight.SemiBold,
                        modifier = Modifier
                            .clickable { AppModel.deletePreset(preset.id); navController.popBackStack() }
                            .padding(18.dp)
                            .fillMaxWidth(),
                        textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                    )
                }
            }
            Spacer(Modifier.height(30.dp))
        }
    }

    deletePointTarget?.let { point ->
        AlertDialog(
            onDismissRequest = { deletePointTarget = null },
            title = { Text("删除知识点？") },
            text = { Text("删除后，该知识点的重点集锦、已掌握和今日复习次数也会一并移除。") },
            confirmButton = {
                TextButton(onClick = {
                    AppModel.deleteKnowledgePoint(preset.id, point.id)
                    deletePointTarget = null
                }) { Text("删除", color = colors.removeCoral) }
            },
            dismissButton = {
                TextButton(onClick = { deletePointTarget = null }) { Text("取消") }
            },
        )
    }
}

/** 编辑/添加知识点。 */
@Composable
fun EditKnowledgePointPage(navController: NavController, presetId: String, pointId: String) {
    val colors = kikariaColors()
    val isNew = pointId == "new"
    val existing = if (isNew) null else AppModel.knowledgePoints.firstOrNull { it.id == pointId }
    var title by remember(pointId) { mutableStateOf(existing?.title ?: "") }
    var tagsText by remember(pointId) { mutableStateOf(existing?.tags?.joinToString(", ") ?: "") }
    var hint by remember(pointId) { mutableStateOf(existing?.hint ?: "") }
    var content by remember(pointId) { mutableStateOf(existing?.content ?: "") }

    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp),
    ) {
        PageHeader(
            title = if (isNew) "添加知识点" else "编辑知识点",
            onBack = { navController.popBackStack() },
            trailing = {
                Text(
                    "保存",
                    color = colors.sky,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier
                        .clickable {
                            if (title.isBlank() || hint.isBlank() || content.isBlank()) {
                                AppModel.showToast("标题、提示和答案都不能为空。")
                                return@clickable
                            }
                            val tags = tagsText.split(",", "，").map { it.trim() }.filter { it.isNotEmpty() }
                            val now = System.currentTimeMillis()
                            val point = existing?.copy(
                                title = title.trim(),
                                tags = tags,
                                hint = hint.trim(),
                                content = content.trim(),
                                updatedAt = now,
                            ) ?: KnowledgePoint(
                                id = UUID.randomUUID().toString(),
                                title = title.trim(),
                                tags = tags,
                                hint = hint.trim(),
                                content = content.trim(),
                                reinforcementCount = 0,
                                lastReinforcedAt = null,
                                isMastered = false,
                                createdAt = now,
                                updatedAt = now,
                            )
                            AppModel.upsertKnowledgePoint(presetId, point)
                            navController.popBackStack()
                        }
                        .padding(8.dp),
                )
            },
        )
        ProfileField("标题", title) { title = it }
        Spacer(Modifier.height(12.dp))
        ProfileField("标签，用逗号分隔", tagsText) { tagsText = it }
        Spacer(Modifier.height(14.dp))
        Text("提示", color = colors.softText, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        LongTextField(value = hint, onValueChange = { hint = it }, minLines = 6)
        Spacer(Modifier.height(14.dp))
        Text("答案", color = colors.softText, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        LongTextField(value = content, onValueChange = { content = it }, minLines = 8)
        Spacer(Modifier.height(30.dp))
    }
}

private val markdownFormatTemplate = """
    # 知识点名称

    tags: 标签1, 标签2, 标签3

    hint:
    这里写提示，可以是一句话，也可以是几行文字。

    content:
    这里写完整答案或背诵内容，可以是一段或多段文字。

    ---
""".trimIndent()

private val markdownCompleteExample = """
    # 极限的保号性

    tags: 高等数学, 极限, 基础

    hint:
    当函数极限大于 0 时，函数值在充分靠近该点时也大于 0。

    content:
    若 lim f(x) = A，且 A > 0，则存在某个去心邻域，使得在该邻域内 f(x) > 0。

    ---

    # 罗尔定理

    tags: 高等数学, 中值定理

    hint:
    闭区间连续，开区间可导，两端函数值相等。

    content:
    若函数 f(x) 在 [a,b] 上连续，在 (a,b) 内可导，且 f(a)=f(b)，则至少存在一点 ξ∈(a,b)，使得 f'(ξ)=0。
""".trimIndent()

private val markdownLatexExample = """
    Kikaria 使用本地渲染引擎排版公式，不会联网处理。

    推荐：中文说明放在公式外

    函数 ${'$'}f(x)=x^2${'$'} 的导数是 ${'$'}2x${'$'}。

    当 x 接近 0 时，有：

    ${'$'}${'$'}
    \lim_{x\to0}\frac{\sin x}{x}=1
    ${'$'}${'$'}

    不推荐：没有 ${'$'} 包裹的 LaTeX 不会渲染

    \Delta\varphi=0
""".trimIndent()

private val markdownAIPrompt = """
    请你把我提供的学习资料整理成 Kikaria 背诵 App 支持的结构化 Markdown 知识点。

    格式必须严格遵守：

    # 知识点名称

    tags: 标签1, 标签2, 标签3

    hint:
    用简洁语言给出背诵提示，不要直接泄露完整答案。

    content:
    写出完整、准确、适合背诵的知识点内容。

    ---

    要求：
    1. 每个知识点之间必须用单独一行 --- 分隔。
    2. 每个知识点都必须包含标题、tags、hint、content 四部分。
    3. tags 后的标签用逗号分隔。
    4. hint 要简短，适合作为回忆提示。
    5. content 要完整、准确、适合直接背诵。
    6. 不要生成多余解释。
    7. 不要使用表格。
    8. 不要把多个知识点混在一起。
    9. 如果原资料太长，请拆分成多个小知识点。
    10. 输出结果只保留 Markdown 内容，不要添加寒暄或说明。
    11. 数学公式可以使用 LaTeX，Kikaria 会用本地渲染引擎渲染，不会联网处理。
    12. 只有 ${'$'}...${'$'} 和 ${'$'}${'$'}...${'$'}${'$'} 中的内容会渲染为公式；没有包裹的 LaTeX 命令会按普通文本保留。
    13. 行内公式用 ${'$'}...${'$'}，块级公式用 ${'$'}${'$'}...${'$'}${'$'}。
    14. 公式环境中不要混入中文，中文解释要写在公式外；必要时可少量使用 \text{...}。

    下面是需要整理的资料：

    【在这里粘贴课本、讲义、笔记或 OCR 文本】
""".trimIndent()

/** Markdown 格式说明。 */
@Composable
fun MarkdownGuidePage(navController: NavController) {
    val colors = kikariaColors()
    val clipboard = LocalClipboardManager.current

    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp),
    ) {
        PageHeader(title = "Markdown 格式说明", onBack = { navController.popBackStack() })

        GuideCard("简介") {
            GuideBody("Kikaria 使用结构化 Markdown 保存知识点。每个知识点包含标题、标签、提示和答案四部分，用单独一行 --- 分隔。")
        }
        GuideCard("格式规则") { CodeBlock(markdownFormatTemplate) }
        GuideCard("规则说明") {
            BulletText("标题必须以 # 开头。")
            BulletText("tags: 后面写标签，多个标签用英文逗号或中文逗号分隔。")
            BulletText("hint: 后面写提示。")
            BulletText("content: 后面写完整内容。")
            BulletText("每个知识点之间用单独一行 --- 分隔。")
            BulletText("建议每个知识点不要太长，适合一次背诵。")
            BulletText("标签可以用于后续选择背诵范围。")
        }
        GuideCard("LaTeX 公式") {
            GuideBody("只有 \$...\$ 和 \$\$...\$\$ 中的内容会渲染为公式。")
            CodeBlock(markdownLatexExample)
        }
        GuideCard("完整示例") { CodeBlock(markdownCompleteExample) }
        GuideCard("给 AI 助手的 Prompt") {
            GuideBody("把下面这段 Prompt 连同你的学习资料一起发给 AI 助手，即可自动生成符合 Kikaria 格式的知识点。")
            Spacer(Modifier.height(10.dp))
            GradientCapsuleButton(
                text = "复制 Prompt",
                gradient = colors.actionGradient,
                fontSize = 15,
            ) {
                clipboard.setText(AnnotatedString(markdownAIPrompt))
                AppModel.showToast("Prompt 已复制")
            }
        }
        Spacer(Modifier.height(30.dp))
    }
}

@Composable
private fun GuideCard(title: String, content: @Composable () -> Unit) {
    val colors = kikariaColors()
    GlassCard(cornerRadius = 24, fillAlpha = 0.42f, modifier = Modifier.fillMaxWidth().padding(bottom = 14.dp)) {
        Column(Modifier.fillMaxWidth().padding(18.dp)) {
            Text(title, color = colors.deepText, fontSize = 17.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(8.dp))
            content()
        }
    }
}

@Composable
private fun GuideBody(text: String) {
    val colors = kikariaColors()
    Text(text, color = colors.softText, fontSize = 14.sp, lineHeight = 21.sp)
}

@Composable
private fun BulletText(text: String) {
    val colors = kikariaColors()
    Row(Modifier.fillMaxWidth().padding(vertical = 3.dp), verticalAlignment = Alignment.Top) {
        Box(
            Modifier
                .padding(top = 7.dp)
                .size(5.dp)
                .clip(androidx.compose.foundation.shape.CircleShape)
                .background(colors.sky),
        )
        Spacer(Modifier.width(8.dp))
        Text(text, color = colors.softText, fontSize = 14.sp, lineHeight = 21.sp)
    }
}

@Composable
private fun CodeBlock(code: String) {
    val colors = kikariaColors()
    val scroller = rememberScrollState()
    Column(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(18.dp))
            .background(colors.glassSurface.copy(alpha = 0.5f))
            .border(1.dp, colors.blueGray.copy(alpha = 0.25f), RoundedCornerShape(18.dp))
            .padding(14.dp),
    ) {
        Text(
            code,
            color = colors.deepText,
            fontSize = 13.sp,
            fontFamily = FontFamily.Monospace,
            modifier = Modifier.horizontalScroll(scroller),
        )
    }
}
