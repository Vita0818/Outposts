package com.vita0818.kikaria.ui.scope

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
                KikariaPageTitle(title = "\u8303\u56F4\u9009\u62E9")

                Spacer(modifier = Modifier.height(10.dp))

                Text(
                    text = if (selected.isEmpty())
                        "\u672A\u9009\u62E9\u6807\u7B7E\u65F6\uFF0C\u4F1A\u9ED8\u8BA4\u4F7F\u7528\u5168\u90E8\u77E5\u8BC6\u70B9\u3002"
                    else "\u5DF2\u9009\u62E9 ${selected.size} \u4E2A\u6807\u7B7E\u3002",
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Medium,
                    color = softText
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Clear all button
                if (selected.isNotEmpty()) {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clip(RoundedCornerShape(12.dp))
                            .shadow(6.dp, RoundedCornerShape(12.dp),
                                ambientColor = sky.copy(alpha = 0.06f),
                                spotColor = sky.copy(alpha = 0.06f))
                            .background(mist.copy(alpha = 0.6f))
                            .clickable { viewModel.selectedTags.clear() }
                            .padding(14.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = "\u6E05\u9664\u5168\u90E8 (\u5F53\u524D\u9009\u4E2D ${selected.size})",
                            color = deepText,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.SemiBold
                        )
                    }

                    Spacer(modifier = Modifier.height(12.dp))
                }

                // Tag chips
                FlowRow(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    allTags.forEach { tag ->
                        val isTagSelected = tag in selected
                        val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
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
                        text = "\u5F53\u524D\u9884\u8BBE\u6CA1\u6709\u6807\u7B7E\u3002\u77E5\u8BC6\u70B9\u5C06\u4EE5\u6807\u9898\u5F62\u5F0F\u5C55\u793A\u3002",
                        color = tertiaryText,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Medium
                    )
                }

                Spacer(modifier = Modifier.height(24.dp))

                // Preview: selected points count
                val previewCount = viewModel.selectedKnowledgePoints.size
                Text(
                    text = "\u5C06\u590D\u4E60 $previewCount \u4E2A\u77E5\u8BC6\u70B9",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Medium,
                    color = masteredGreen
                )

                Spacer(modifier = Modifier.height(32.dp))
            }
        }
    }
}
