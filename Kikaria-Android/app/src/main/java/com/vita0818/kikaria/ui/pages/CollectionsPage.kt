package com.vita0818.kikaria.ui.pages

import androidx.compose.foundation.background
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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.RemoveCircle
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Shuffle
import androidx.compose.material.icons.filled.AutoAwesome
import androidx.compose.material3.Icon
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
import com.vita0818.kikaria.ui.KikariaSearchBar
import com.vita0818.kikaria.ui.LightTagRow
import com.vita0818.kikaria.ui.PageHeader
import com.vita0818.kikaria.ui.SoftEmptyState
import com.vita0818.kikaria.ui.theme.kikariaColors

/** 重点集锦 / 已掌握列表,结构相同。 */
@Composable
fun CollectionsPage(navController: NavController, mastered: Boolean) {
    val colors = kikariaColors()
    var query by remember { mutableStateOf("") }

    val source = if (mastered) AppModel.masteredPoints else AppModel.reinforcedPoints
    val filtered = if (query.isBlank()) source else source.filter {
        it.title.lowercase().contains(query.trim().lowercase()) ||
            it.tags.any { tag -> tag.lowercase().contains(query.trim().lowercase()) }
    }

    Column(Modifier.fillMaxSize().padding(horizontal = 24.dp)) {
        PageHeader(
            title = if (mastered) "已掌握" else "重点集锦",
            onBack = { navController.popBackStack() },
        )
        KikariaSearchBar(
            value = query,
            onValueChange = { query = it },
            placeholder = "搜索知识点",
            modifier = Modifier.fillMaxWidth(),
        )
        Spacer(Modifier.height(16.dp))

        if (source.isEmpty()) {
            SoftEmptyState(
                icon = if (mastered) Icons.Filled.CheckCircle else Icons.Filled.AutoAwesome,
                title = if (mastered) "还没有已掌握" else "还没有重点",
                subtitle = if (mastered) {
                    "在背诵时查看答案后，可以把真正熟悉的知识点标记到这里。"
                } else {
                    "在背诵时查看答案后，可以把知识点加入这里。"
                },
            )
        } else if (filtered.isEmpty()) {
            SoftEmptyState(
                icon = Icons.Filled.Search,
                title = "没有找到相关知识点",
                subtitle = "换个关键词试试看。",
            )
        } else {
            LazyColumn(
                verticalArrangement = Arrangement.spacedBy(12.dp),
                modifier = Modifier.weight(1f),
            ) {
                items(filtered, key = { it.id }) { point ->
                    CollectionCard(point, mastered)
                }
            }
        }

        if (source.isNotEmpty()) {
            Spacer(Modifier.height(14.dp))
            GlassCard(cornerRadius = 26, fillAlpha = 0.42f, strokeAlpha = 0.4f, modifier = Modifier.fillMaxWidth()) {
                Row(
                    Modifier
                        .fillMaxWidth()
                        .clickable {
                            navController.navigate(
                                Routes.review(if (mastered) "mastered" else "reinforcement"),
                            )
                        }
                        .padding(horizontal = 20.dp, vertical = 16.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Icon(
                        Icons.Filled.Shuffle,
                        contentDescription = null,
                        tint = if (mastered) colors.masteredGreen else colors.sky,
                        modifier = Modifier.size(20.dp),
                    )
                    Spacer(Modifier.width(10.dp))
                    Column(Modifier.weight(1f)) {
                        Text(
                            if (mastered) "开始复习" else "开始重点背诵",
                            color = colors.deepText,
                            fontSize = 20.sp,
                            fontWeight = FontWeight.SemiBold,
                        )
                    }
                    Text(
                        "${filtered.size}",
                        color = if (mastered) colors.masteredGreen else colors.sky,
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Bold,
                        fontFamily = FontFamily.Serif,
                    )
                }
            }
            Spacer(Modifier.height(20.dp))
        }
    }
}

@Composable
private fun CollectionCard(point: KnowledgePoint, mastered: Boolean) {
    val colors = kikariaColors()
    GlassCard(cornerRadius = 30, fillAlpha = 0.42f, modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.fillMaxWidth().padding(20.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    point.title,
                    color = colors.deepText,
                    fontSize = 22.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.weight(1f),
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
                if (!mastered && point.reinforcementCount > 0) {
                    Spacer(Modifier.width(8.dp))
                    Text(
                        "×${point.reinforcementCount}",
                        color = colors.sky,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Bold,
                    )
                }
            }
            if (point.tags.isNotEmpty()) {
                Spacer(Modifier.height(10.dp))
                LightTagRow(tags = point.tags)
            }
            Spacer(Modifier.height(12.dp))
            InfoPreview(title = "提示", text = point.hint)
            Spacer(Modifier.height(8.dp))
            InfoPreview(title = "答案", text = point.content)
            Spacer(Modifier.height(14.dp))
            Box(
                Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(50))
                    .background(Brush.linearGradient(colors.removeGradient))
                    .clickable {
                        if (mastered) AppModel.removeMastered(point.id) else AppModel.removeReinforcement(point.id)
                    }
                    .padding(horizontal = 16.dp, vertical = 10.dp),
                contentAlignment = Alignment.Center,
            ) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(
                        Icons.Filled.RemoveCircle,
                        contentDescription = null,
                        tint = Color.White,
                        modifier = Modifier.size(16.dp),
                    )
                    Spacer(Modifier.width(6.dp))
                    Text(
                        if (mastered) "移出已掌握" else "移出重点集锦",
                        color = Color.White,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.SemiBold,
                    )
                }
            }
        }
    }
}

@Composable
private fun InfoPreview(title: String, text: String) {
    val colors = kikariaColors()
    Column(Modifier.fillMaxWidth()) {
        Text(title, color = colors.sky, fontSize = 13.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(4.dp))
        Text(
            text,
            color = colors.deepText.copy(alpha = 0.82f),
            fontSize = 15.sp,
            maxLines = 2,
            overflow = TextOverflow.Ellipsis,
            lineHeight = 21.sp,
        )
    }
}
