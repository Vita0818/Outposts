package com.vita0818.kikaria.ui.preset

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePreset
import com.vita0818.kikaria.ui.components.KikariaFormPageShell
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaTagChip
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics
import com.vita0818.kikaria.util.MarkdownParser

@Composable
fun EditPresetScreen(
    preset: KnowledgePreset,
    onBack: () -> Unit,
    onSavePreset: (name: String, category: String, markdownText: String) -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral

    var name by remember { mutableStateOf(preset.name) }
    var category by remember { mutableStateOf(preset.category) }
    var markdownText by remember { mutableStateOf(preset.markdownText) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    val parsedPoints = remember(markdownText) {
        if (markdownText.isNotBlank()) MarkdownParser.parseKnowledgePoints(markdownText)
        else emptyList()
    }

    val doSave: () -> Unit = {
        val n = name.trim(); val m = markdownText.trim()
        if (n.isEmpty()) errorMessage = "请填写预设名称。"
        else if (m.isEmpty()) errorMessage = "请填写 Markdown 文本。"
        else onSavePreset(n, category.trim(), m)
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
        EditorTextField("预设名称", name, { name = it }, isDark, true)
        Spacer(modifier = Modifier.height(10.dp))
        EditorTextField("分类", category, { category = it }, isDark, true)
        Spacer(modifier = Modifier.height(12.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
            Text(KikariaTypography.mixedText("Markdown 文本", size = 14, weight = FontWeight.SemiBold), color = softText, modifier = Modifier.padding(start = 4.dp))
            Text("${parsedPoints.size} 个知识点", fontSize = 12.sp, fontWeight = FontWeight.SemiBold, color = sky)
        }
        Spacer(modifier = Modifier.height(8.dp))
        KikariaGlassCard(Modifier.fillMaxWidth().height(180.dp), cornerRadius = 24.dp, fillOpacity = 0.56f) {
            TextField(
                value = markdownText, onValueChange = { markdownText = it },
                textStyle = TextStyle(fontSize = 14.sp, fontWeight = FontWeight.Normal, fontFamily = FontFamily.Monospace, color = deepText, lineHeight = 20.sp),
                colors = TextFieldDefaults.colors(focusedContainerColor = Color.Transparent, unfocusedContainerColor = Color.Transparent, focusedIndicatorColor = Color.Transparent, unfocusedIndicatorColor = Color.Transparent, cursorColor = sky),
                keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.None),
                modifier = Modifier.fillMaxWidth().padding(14.dp)
            )
        }
        if (errorMessage != null) {
            Spacer(modifier = Modifier.height(12.dp))
            KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 18.dp, fillOpacity = 0.50f) {
                Text(errorMessage ?: "", fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = removeCoral, modifier = Modifier.padding(14.dp))
            }
        }
        if (parsedPoints.isNotEmpty()) {
            Spacer(modifier = Modifier.height(16.dp))
            Text(KikariaTypography.mixedText("知识点预览", size = 14, weight = FontWeight.SemiBold), color = softText, modifier = Modifier.padding(start = 4.dp, bottom = 8.dp))
            KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 24.dp, fillOpacity = 0.42f) {
                Column(modifier = Modifier.padding(14.dp)) {
                    parsedPoints.forEach { point ->
                        Row(Modifier.fillMaxWidth().padding(vertical = 6.dp), verticalAlignment = Alignment.CenterVertically) {
                            Text(KikariaTypography.mixedText(point.title, size = 15, weight = FontWeight.SemiBold), color = deepText, modifier = Modifier.weight(1f))
                            if (point.tags.isNotEmpty()) {
                                Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                                    point.tags.take(2).forEach { tag -> KikariaTagChip(tag = tag, fontSize = 10) }
                                    if (point.tags.size > 2) Text("+${point.tags.size - 2}", fontSize = 10.sp, fontWeight = FontWeight.SemiBold, color = softText)
                                }
                            }
                        }
                    }
                }
            }
        }
        Spacer(modifier = Modifier.height(32.dp))
    }
}

@Composable
private fun EditorTextField(title: String, value: String, onValueChange: (String) -> Unit, isDark: Boolean, singleLine: Boolean = false) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    Column {
        Text(KikariaTypography.mixedText(title, size = 14, weight = FontWeight.SemiBold), color = softText, modifier = Modifier.padding(start = 4.dp, bottom = 8.dp))
        KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 20.dp, fillOpacity = 0.50f, shadowElevation = 12.dp, shadowOpacity = 0.08f) {
            TextField(
                value = value, onValueChange = onValueChange,
                textStyle = TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Normal, color = deepText),
                colors = TextFieldDefaults.colors(focusedContainerColor = Color.Transparent, unfocusedContainerColor = Color.Transparent, focusedIndicatorColor = Color.Transparent, unfocusedIndicatorColor = Color.Transparent, cursorColor = sky),
                keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.None),
                modifier = Modifier.fillMaxWidth().padding(16.dp), singleLine = singleLine,
                placeholder = { Text(title, fontSize = 16.sp, fontWeight = FontWeight.Normal, color = softText.copy(alpha = 0.5f)) }
            )
        }
    }
}
