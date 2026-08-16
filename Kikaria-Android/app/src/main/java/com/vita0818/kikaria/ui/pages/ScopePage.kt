package com.vita0818.kikaria.ui.pages

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import com.vita0818.kikaria.AppModel
import com.vita0818.kikaria.ui.GlassCard
import com.vita0818.kikaria.ui.GradientCapsuleButton
import com.vita0818.kikaria.ui.KikariaSearchBar
import com.vita0818.kikaria.ui.PageHeader
import com.vita0818.kikaria.ui.SoftEmptyState
import com.vita0818.kikaria.ui.theme.kikariaColors
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Tag

/** 范围选择:标签多选 + 搜索(匹配标签名或知识点内容)。 */
@Composable
fun ScopePage(navController: NavController) {
    val colors = kikariaColors()
    var query by remember { mutableStateOf("") }

    val allTags = AppModel.allTags
    val matchedTags: Set<String> = if (query.isBlank()) {
        allTags.toSet()
    } else {
        val q = query.trim().lowercase()
        val direct = allTags.filter { it.lowercase().contains(q) }
        val viaPoints = AppModel.knowledgePoints
            .filter {
                it.title.lowercase().contains(q) || it.hint.lowercase().contains(q) || it.content.lowercase().contains(q)
            }
            .flatMap { it.tags }
        (direct + viaPoints).toSet()
    }
    val visibleTags = allTags.filter { matchedTags.contains(it) }

    Column(Modifier.fillMaxSize().padding(horizontal = 24.dp)) {
        PageHeader(title = "选择范围", onBack = { navController.popBackStack() })
        Text(
            if (AppModel.selectedTags.isEmpty()) "未选择标签时，会默认使用全部知识点。"
            else "已选择 ${AppModel.selectedTags.size} 个标签。",
            color = colors.softText,
            fontSize = 15.sp,
        )
        Spacer(Modifier.height(14.dp))
        KikariaSearchBar(
            value = query,
            onValueChange = { query = it },
            placeholder = "搜索标签或知识点",
            modifier = Modifier.fillMaxWidth(),
        )
        Spacer(Modifier.height(16.dp))

        if (visibleTags.isEmpty()) {
            SoftEmptyState(icon = Icons.Filled.Tag, title = "没有找到相关标签", subtitle = "换个关键词试试看。")
        } else {
            LazyVerticalGrid(
                columns = GridCells.Adaptive(minSize = 140.dp),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
                modifier = Modifier.weight(1f),
            ) {
                items(visibleTags) { tag ->
                    val selected = AppModel.selectedTags.contains(tag)
                    Box(
                        Modifier
                            .height(54.dp)
                            .clip(RoundedCornerShape(20.dp))
                            .background(
                                if (selected) Brush.linearGradient(colors.actionGradient)
                                else Brush.linearGradient(listOf(colors.glassSurface.copy(alpha = 0.36f), colors.glassSurface.copy(alpha = 0.36f))),
                            )
                            .border(
                                1.dp,
                                if (selected) colors.cyan.copy(alpha = 0.7f) else Color.White.copy(alpha = 0.28f),
                                RoundedCornerShape(20.dp),
                            )
                            .clickable {
                                AppModel.selectedTags = if (selected) {
                                    AppModel.selectedTags - tag
                                } else {
                                    AppModel.selectedTags + tag
                                }
                                AppModel.scheduleStudyStatePersistence()
                            },
                        contentAlignment = Alignment.Center,
                    ) {
                        Text(
                            tag,
                            color = if (selected) Color.White else colors.deepText,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.SemiBold,
                            modifier = Modifier.padding(horizontal = 10.dp),
                        )
                    }
                }
            }
        }

        Spacer(Modifier.height(16.dp))
        GradientCapsuleButton(text = "完成", gradient = colors.actionGradient) {
            navController.popBackStack()
        }
        Spacer(Modifier.height(20.dp))
    }
}
