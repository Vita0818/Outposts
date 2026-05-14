package com.vita0818.kikaria.ui.scope

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.viewmodel.KikariaViewModel

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun ScopeSelectionScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit
) {
    val allTags = viewModel.allTags
    val selected = viewModel.selectedTags.toSet()

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "范围选择",
                        fontWeight = FontWeight.SemiBold,
                        color = KikariaColors.DeepText
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Text("←", fontSize = 22.sp, color = KikariaColors.DeepText)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = KikariaColors.GlassSurface.copy(alpha = 0f)
                )
            )
        },
        containerColor = KikariaColors.GlassSurface
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 24.dp)
        ) {
            Text(
                text = "选择标签以限定复习范围。未选择任何标签时将使用全部知识点。",
                fontSize = 14.sp,
                color = KikariaColors.SoftText
            )

            Spacer(modifier = Modifier.height(16.dp))

            // Clear all
            if (selected.isNotEmpty()) {
                Button(
                    onClick = { viewModel.selectedTags.clear() },
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = KikariaColors.Mist.copy(alpha = 0.8f)
                    )
                ) {
                    Text(
                        text = "清除全部 (当前选中 ${selected.size})",
                        color = KikariaColors.DeepText,
                        fontSize = 14.sp
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
                    FilterChip(
                        selected = tag in selected,
                        onClick = {
                            if (tag in selected) {
                                viewModel.selectedTags.remove(tag)
                            } else {
                                viewModel.selectedTags.add(tag)
                            }
                        },
                        label = {
                            Text(
                                text = tag,
                                fontSize = 13.sp
                            )
                        },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = KikariaColors.Sky.copy(alpha = 0.25f),
                            selectedLabelColor = KikariaColors.DeepText
                        )
                    )
                }
            }

            if (allTags.isEmpty()) {
                Spacer(modifier = Modifier.height(32.dp))
                Text(
                    text = "当前预设没有标签。知识点将以标题形式展示。",
                    color = KikariaColors.TertiaryText,
                    fontSize = 14.sp
                )
            }

            Spacer(modifier = Modifier.height(24.dp))

            // Preview: selected points count
            val previewCount = viewModel.selectedKnowledgePoints.size
            Text(
                text = "将复习 $previewCount 个知识点",
                fontSize = 14.sp,
                color = KikariaColors.MasteredGreen,
                fontWeight = FontWeight.Medium
            )
        }
    }
}
