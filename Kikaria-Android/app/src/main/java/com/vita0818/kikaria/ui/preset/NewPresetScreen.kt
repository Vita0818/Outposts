package com.vita0818.kikaria.ui.preset

import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaEditorTextField
import com.vita0818.kikaria.ui.components.KikariaFormPageShell
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics

@Composable
fun NewPresetScreen(
    onBack: () -> Unit,
    onCreatePreset: (name: String, category: String, markdownText: String) -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    var name by remember { mutableStateOf("") }
    var category by remember { mutableStateOf("") }
    var markdownText by remember { mutableStateOf("") }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    val doSave: () -> Unit = {
        val n = name.trim(); val m = markdownText.trim()
        if (n.isEmpty()) errorMessage = "请填写预设名称。"
        else if (m.isEmpty()) errorMessage = "请填写 Markdown 文本。"
        else onCreatePreset(n, category.trim(), m)
    }

    val metrics = rememberKikariaPhoneMetrics()

    KikariaFormPageShell(
        title = "上传新预设",
        onBack = onBack,
        metrics = metrics,
        closeIcon = KikariaIcons.back,
        actionLabel = "保存",
        onAction = doSave
    ) {
        Spacer(modifier = Modifier.height(12.dp))
        KikariaEditorTextField("预设名称", name, { name = it }, isDark, true)
        Spacer(modifier = Modifier.height(10.dp))
        KikariaEditorTextField("分类", category, { category = it }, isDark, true)
        Spacer(modifier = Modifier.height(12.dp))
        Text(
            KikariaTypography.mixedText("Markdown 文本", size = 14, weight = FontWeight.SemiBold),
            color = softText, modifier = Modifier.padding(start = 4.dp, bottom = 8.dp)
        )
        KikariaGlassCard(Modifier.fillMaxWidth().height(metrics.newPresetTextEditorMinHeight), cornerRadius = 24.dp, fillOpacity = 0.56f) {
            TextField(
                value = markdownText, onValueChange = { markdownText = it },
                textStyle = TextStyle(fontSize = 14.sp, fontWeight = FontWeight.Normal, fontFamily = FontFamily.Monospace, color = deepText, lineHeight = 20.sp),
                colors = TextFieldDefaults.colors(focusedContainerColor = Color.Transparent, unfocusedContainerColor = Color.Transparent, focusedIndicatorColor = Color.Transparent, unfocusedIndicatorColor = Color.Transparent, cursorColor = sky),
                keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.None),
                modifier = Modifier.fillMaxWidth().padding(14.dp),
                placeholder = {
                    Text("# 知识点名称\n\ntags: 标签1, 标签2\n\nhint:\n这里是提示...\n\ncontent:\n这里是完整答案...\n\n---",
                        fontSize = 14.sp, fontWeight = FontWeight.Normal, fontFamily = FontFamily.Monospace,
                        color = softText.copy(alpha = 0.5f), lineHeight = 20.sp)
                }
            )
        }
        if (errorMessage != null) {
            Spacer(modifier = Modifier.height(12.dp))
            KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 18.dp, fillOpacity = 0.50f) {
                Text(errorMessage ?: "", fontSize = 14.sp, fontWeight = FontWeight.SemiBold,
                    color = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral,
                    modifier = Modifier.padding(14.dp))
            }
        }
        Spacer(modifier = Modifier.height(32.dp))
    }
}