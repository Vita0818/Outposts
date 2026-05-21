package com.vita0818.kikaria.ui.scope

import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaPageTitle
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.viewmodel.KikariaViewModel

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun ScopeSelectionScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit
) {
    val allTags = viewModel.allTags
    val selected = viewModel.selectedTags.toSet()
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val tertiaryText = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val mist = if (isDark) KikariaColors.MistDark else KikariaColors.Mist
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val masteredGreen = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen

    KikariaPageShell {
        Box(modifier = Modifier.fillMaxSize()) {
            KikariaBackButton(onClick = onBack)

            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 24.dp)
                    .padding(top = 70.dp)
            ) {
                KikariaPageTitle(title = "选择范围")

                Spacer(modifier = Modifier.height(8.dp))

                Text(
                    text = if (selected.isEmpty())
                        "未选择标签时，会默认使用全部知识点。"
                    else "已选择 ${selected.size} 个标签。",
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Medium,
                    color = softText
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Tag chips matching iOS ScopeTagChip with action gradient fill for selected
                FlowRow(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    allTags.forEach { tag ->
                        val isTagSelected = tag in selected
                        Box(
                            modifier = Modifier
                                .clip(RoundedCornerShape(20.dp))
                                .shadow(
                                    7.dp, RoundedCornerShape(20.dp),
                                    ambientColor = sky.copy(alpha = if (isTagSelected) 0.18f else 0.06f),
                                    spotColor = sky.copy(alpha = if (isTagSelected) 0.18f else 0.06f)
                                )
                                .background(
                                    if (isTagSelected) actionGrad
                                    else mist.copy(alpha = 0.45f)
                                )
                                .clickable {
                                    if (isTagSelected) viewModel.selectedTags.remove(tag)
                                    else viewModel.selectedTags.add(tag)
                                }
                                .padding(horizontal = 14.dp, vertical = 9.dp)
                        ) {
                            Text(
                                text = tag,
                                fontSize = 13.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = if (isTagSelected) Color.White else softText
                            )
                        }
                    }
                }

                if (allTags.isEmpty()) {
                    Spacer(modifier = Modifier.height(32.dp))
                    Text(
                        text = "当前预设没有标签。知识点将以标题形式展示。",
                        color = tertiaryText,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Medium
                    )
                }

                Spacer(modifier = Modifier.height(24.dp))

                // Preview: selected points count
                val previewCount = viewModel.selectedKnowledgePoints.size
                Text(
                    text = "将复习 $previewCount 个知识点",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Medium,
                    color = masteredGreen
                )

                Spacer(modifier = Modifier.height(32.dp))
            }
        }
    }
}
