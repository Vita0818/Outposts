package com.vita0818.kikaria.ui.preset

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
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
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography

/**
 * Markdown editor / New Preset screen translated from NewPresetView in ContentView.swift.
 *
 * Allows users to create a new preset by entering a name, category, and
 * pasting/typing structured Markdown text.
 */
@Composable
fun NewPresetScreen(
    onBack: () -> Unit,
    onCreatePreset: (name: String, category: String, markdownText: String) -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight

    var name by remember { mutableStateOf("") }
    var category by remember { mutableStateOf("") }
    var markdownText by remember { mutableStateOf("") }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    KikariaPageShell {
        Box(modifier = Modifier.fillMaxSize()) {
            KikariaBackButton(onClick = onBack)

            Column(modifier = Modifier.fillMaxSize()) {
                // Top bar
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(start = 70.dp, end = 24.dp, top = 18.dp, bottom = 16.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "上传新预设",
                        fontSize = 17.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = deepText
                    )
                    Box(
                        modifier = Modifier
                            .clip(RoundedCornerShape(14.dp))
                            .clickable {
                                val trimmedName = name.trim()
                                val trimmedMarkdown = markdownText.trim()
                                if (trimmedName.isEmpty()) {
                                    errorMessage = "请填写预设名称。"
                                } else if (trimmedMarkdown.isEmpty()) {
                                    errorMessage = "请填写 Markdown 文本。"
                                } else {
                                    onCreatePreset(trimmedName, category.trim(), trimmedMarkdown)
                                }
                            }
                            .background(sky.copy(alpha = 0.18f))
                            .padding(horizontal = 16.dp, vertical = 10.dp)
                    ) {
                        Text("保存", fontSize = 15.sp, fontWeight = FontWeight.SemiBold, color = sky)
                    }
                }

                Column(
                    modifier = Modifier
                        .weight(1f)
                        .verticalScroll(rememberScrollState())
                        .padding(horizontal = 24.dp)
                ) {
                    Spacer(modifier = Modifier.height(8.dp))

                    // Name field
                    EditorTextField(
                        title = "预设名称",
                        value = name,
                        onValueChange = { name = it },
                        isDark = isDark,
                        singleLine = true
                    )

                    Spacer(modifier = Modifier.height(14.dp))

                    // Category field
                    EditorTextField(
                        title = "分类",
                        value = category,
                        onValueChange = { category = it },
                        isDark = isDark,
                        singleLine = true
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    // Markdown text section
                    Text(
                        text = KikariaTypography.mixedText(
                            "Markdown 文本",
                            size = 14,
                            weight = FontWeight.SemiBold
                        ),
                        color = softText,
                        modifier = Modifier.padding(start = 4.dp, bottom = 8.dp)
                    )

                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 24.dp,
                        fillOpacity = 0.56f
                    ) {
                        TextField(
                            value = markdownText,
                            onValueChange = { markdownText = it },
                            textStyle = TextStyle(
                                fontSize = 15.sp,
                                fontWeight = FontWeight.Normal,
                                fontFamily = FontFamily.Monospace,
                                color = deepText,
                                lineHeight = 22.sp
                            ),
                            colors = TextFieldDefaults.colors(
                                focusedContainerColor = Color.Transparent,
                                unfocusedContainerColor = Color.Transparent,
                                focusedIndicatorColor = Color.Transparent,
                                unfocusedIndicatorColor = Color.Transparent,
                                cursorColor = sky
                            ),
                            keyboardOptions = KeyboardOptions(
                                capitalization = KeyboardCapitalization.None
                            ),
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(260.dp)
                                .padding(14.dp),
                            placeholder = {
                                Text(
                                    text = "# 知识点名称\n\ntags: 标签1, 标签2\n\nhint:\n这里是提示...\n\ncontent:\n这里是完整答案...\n\n---",
                                    fontSize = 14.sp,
                                    fontWeight = FontWeight.Normal,
                                    fontFamily = FontFamily.Monospace,
                                    color = softText.copy(alpha = 0.5f),
                                    lineHeight = 20.sp
                                )
                            }
                        )
                    }

                    // Error message
                    if (errorMessage != null) {
                        Spacer(modifier = Modifier.height(12.dp))
                        KikariaGlassCard(
                            modifier = Modifier.fillMaxWidth(),
                            cornerRadius = 18.dp,
                            fillOpacity = 0.50f
                        ) {
                            Text(
                                text = errorMessage!!,
                                fontSize = 14.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral,
                                modifier = Modifier.padding(14.dp)
                            )
                        }
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Create button
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clip(RoundedCornerShape(22.dp))
                            .background(actionGrad)
                            .clickable {
                                val trimmedName = name.trim()
                                val trimmedMarkdown = markdownText.trim()
                                if (trimmedName.isEmpty()) {
                                    errorMessage = "请填写预设名称。"
                                } else if (trimmedMarkdown.isEmpty()) {
                                    errorMessage = "请填写 Markdown 文本。"
                                } else {
                                    onCreatePreset(trimmedName, category.trim(), trimmedMarkdown)
                                }
                            }
                            .padding(vertical = 17.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            "创建预设",
                            fontSize = 17.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = Color.White
                        )
                    }

                    Spacer(modifier = Modifier.height(32.dp))
                }
            }
        }
    }
}

@Composable
private fun EditorTextField(
    title: String,
    value: String,
    onValueChange: (String) -> Unit,
    isDark: Boolean,
    singleLine: Boolean = false
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    Column {
        Text(
            text = KikariaTypography.mixedText(
                title,
                size = 14,
                weight = FontWeight.SemiBold
            ),
            color = softText,
            modifier = Modifier.padding(start = 4.dp, bottom = 8.dp)
        )

        KikariaGlassCard(
            modifier = Modifier.fillMaxWidth(),
            cornerRadius = 20.dp,
            fillOpacity = 0.50f,
            shadowElevation = 12.dp,
            shadowOpacity = 0.08f
        ) {
            TextField(
                value = value,
                onValueChange = onValueChange,
                textStyle = TextStyle(
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Normal,
                    color = deepText
                ),
                colors = TextFieldDefaults.colors(
                    focusedContainerColor = Color.Transparent,
                    unfocusedContainerColor = Color.Transparent,
                    focusedIndicatorColor = Color.Transparent,
                    unfocusedIndicatorColor = Color.Transparent,
                    cursorColor = sky
                ),
                keyboardOptions = KeyboardOptions(
                    capitalization = KeyboardCapitalization.None
                ),
                modifier = Modifier.fillMaxWidth().padding(16.dp),
                singleLine = singleLine,
                placeholder = {
                    Text(
                        text = title,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Normal,
                        color = softText.copy(alpha = 0.5f)
                    )
                }
            )
        }
    }
}
