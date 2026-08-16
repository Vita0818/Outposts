package com.vita0818.kikaria.ui.mastered

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.BasicTextField
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
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaEmptyState
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaPageTitle
import com.vita0818.kikaria.ui.components.KikariaTagChip
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaPhoneMetrics
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics
import com.vita0818.kikaria.viewmodel.KikariaViewModel

@Composable
fun MasteredScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit,
    onStartMasteredReview: () -> Unit
) {
    val points = viewModel.masteredPoints
    val metrics = rememberKikariaPhoneMetrics()

    if (points.isEmpty()) {
        MasteredEmptyScreen(metrics = metrics, onBack = onBack)
        return
    }

    var searchText by remember { mutableStateOf("") }
    val filteredPoints = remember(points, searchText) {
        points.filterByKnowledgeSearch(searchText)
    }

    KikariaPageShell {
        Box(modifier = Modifier.fillMaxSize()) {
            if (metrics.isTwoColumnCapable) {
                MasteredLandscapeContent(
                    metrics = metrics,
                    points = points,
                    filteredPoints = filteredPoints,
                    searchText = searchText,
                    onSearchTextChange = { searchText = it },
                    onStartMasteredReview = onStartMasteredReview,
                    onRemove = { point -> viewModel.toggleMastered(point) }
                )
            } else {
                MasteredPhoneContent(
                    metrics = metrics,
                    points = points,
                    filteredPoints = filteredPoints,
                    searchText = searchText,
                    onSearchTextChange = { searchText = it },
                    onStartMasteredReview = onStartMasteredReview,
                    onRemove = { point -> viewModel.toggleMastered(point) }
                )
            }

            KikariaBackButton(
                onClick = onBack,
                metrics = metrics,
                modifier = Modifier.align(Alignment.TopStart)
            )
        }
    }
}

@Composable
private fun MasteredEmptyScreen(metrics: KikariaPhoneMetrics, onBack: () -> Unit) {
    KikariaPageShell {
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.Center
        ) {
            KikariaGlassCard(
                Modifier
                    .padding(metrics.horizontalPadding)
                    .then(
                        if (metrics.isTablet) Modifier.widthIn(max = metrics.mainMaxWidth)
                        else Modifier
                    )
                    .fillMaxWidth(),
                cornerRadius = 30.dp,
                fillOpacity = 0.54f
            ) {
                KikariaEmptyState(
                    title = "还没有已掌握",
                    subtitle = "在背诵时查看答案后，可以把真正熟悉的知识点标记到这里。"
                )
            }

            KikariaBackButton(
                onClick = onBack,
                metrics = metrics,
                modifier = Modifier.align(Alignment.TopStart)
            )
        }
    }
}

@Composable
private fun MasteredPhoneContent(
    metrics: KikariaPhoneMetrics,
    points: List<KnowledgePoint>,
    filteredPoints: List<KnowledgePoint>,
    searchText: String,
    onSearchTextChange: (String) -> Unit,
    onStartMasteredReview: () -> Unit,
    onRemove: (KnowledgePoint) -> Unit
) {
    Box(
        modifier = Modifier.fillMaxSize(),
        contentAlignment = if (metrics.isTablet) Alignment.TopCenter else Alignment.TopStart
    ) {
        Column(
            modifier = Modifier
                .then(if (metrics.isTablet) Modifier.widthIn(max = metrics.mainMaxWidth) else Modifier)
                .fillMaxSize()
        ) {
            LazyColumn(
                modifier = Modifier
                    .weight(1f)
                    .fillMaxWidth(),
                contentPadding = PaddingValues(
                    start = metrics.horizontalPadding,
                    end = metrics.horizontalPadding,
                    bottom = 150.dp
                ),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                item {
                    Spacer(modifier = Modifier.height(metrics.pageTopPadding))
                    KikariaPageTitle(title = "已掌握")
                    Spacer(modifier = Modifier.height(18.dp))
                    KnowledgeCollectionSearchBar(
                        text = searchText,
                        onTextChange = onSearchTextChange
                    )
                }

                if (filteredPoints.isEmpty()) {
                    item {
                        KikariaGlassCard(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(top = 12.dp),
                            cornerRadius = 28.dp,
                            fillOpacity = 0.44f
                        ) {
                            KikariaEmptyState(
                                title = "没有找到相关知识点",
                                subtitle = "换个关键词试试看。"
                            )
                        }
                    }
                } else {
                    items(filteredPoints, key = { it.id }) { point ->
                        MasteredItem(point = point, onRemove = { onRemove(point) })
                    }
                }
            }

            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(
                        if (isSystemInDarkTheme()) Color.Black.copy(alpha = 0.18f)
                        else Color.White.copy(alpha = 0.34f)
                    )
                    .padding(horizontal = metrics.horizontalPadding)
                    .padding(top = 18.dp, bottom = 20.dp)
            ) {
                MasteredStartButton(
                    count = points.size,
                    onClick = onStartMasteredReview
                )
            }
        }
    }
}

@Composable
private fun MasteredLandscapeContent(
    metrics: KikariaPhoneMetrics,
    points: List<KnowledgePoint>,
    filteredPoints: List<KnowledgePoint>,
    searchText: String,
    onSearchTextChange: (String) -> Unit,
    onStartMasteredReview: () -> Unit,
    onRemove: (KnowledgePoint) -> Unit
) {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.TopCenter) {
        Column(
            modifier = Modifier
                .widthIn(max = 980.dp)
                .fillMaxSize()
                .padding(horizontal = metrics.horizontalPadding)
                .padding(top = metrics.pageTopPadding, bottom = 34.dp)
        ) {
            KikariaPageTitle(title = "已掌握")
            Spacer(modifier = Modifier.height(18.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(18.dp)
            ) {
                KnowledgeCollectionSearchBar(
                    text = searchText,
                    onTextChange = onSearchTextChange,
                    modifier = Modifier.weight(1f)
                )

                Box(modifier = Modifier.widthIn(min = 240.dp, max = 260.dp)) {
                    MasteredStartButton(
                        count = points.size,
                        onClick = onStartMasteredReview
                    )
                }
            }

            Spacer(modifier = Modifier.height(18.dp))

            if (filteredPoints.isEmpty()) {
                KikariaGlassCard(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 12.dp),
                    cornerRadius = 28.dp,
                    fillOpacity = 0.44f
                ) {
                    KikariaEmptyState(
                        title = "没有找到相关知识点",
                        subtitle = "换个关键词试试看。"
                    )
                }
            } else {
                LazyVerticalGrid(
                    columns = GridCells.Fixed(2),
                    modifier = Modifier.fillMaxSize(),
                    horizontalArrangement = Arrangement.spacedBy(24.dp),
                    verticalArrangement = Arrangement.spacedBy(20.dp),
                    contentPadding = PaddingValues(top = 4.dp, bottom = 24.dp)
                ) {
                    items(filteredPoints, key = { it.id }) { point ->
                        MasteredItem(point = point, onRemove = { onRemove(point) })
                    }
                }
            }
        }
    }
}

@Composable
private fun MasteredStartButton(count: Int, onClick: () -> Unit) {
    val isDark = isSystemInDarkTheme()
    val masteredGrad = if (isDark) KikariaColors.MasteredGradientDark else KikariaColors.MasteredGradientLight
    val shape = RoundedCornerShape(28.dp)
    Box(
        Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp)
            .shadow(
                20.dp,
                shape,
                ambientColor = (if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen).copy(alpha = 0.16f),
                spotColor = (if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen).copy(alpha = 0.16f)
            )
            .clip(shape)
            .background(masteredGrad)
            .clickable { onClick() }
            .padding(horizontal = 22.dp, vertical = 22.dp),
        contentAlignment = Alignment.Center
    ) {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text("开始复习", fontSize = 20.sp, fontWeight = FontWeight.SemiBold, color = Color.White)
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(KikariaTypography.mixedText("$count", size = 20, weight = FontWeight.Bold), color = Color.White)
                Spacer(Modifier.width(12.dp))
                Icon(
                    imageVector = KikariaIcons.forward,
                    contentDescription = "开始复习",
                    modifier = Modifier.size(18.dp),
                    tint = Color.White.copy(alpha = 0.72f)
                )
            }
        }
    }
}

@Composable
private fun MasteredItem(point: KnowledgePoint, onRemove: () -> Unit) {
    val isDark = isSystemInDarkTheme()
    val masteredDeepGreen = if (isDark) KikariaColors.MasteredDeepGreenDark else KikariaColors.MasteredDeepGreen
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral
    KikariaGlassCard(
        Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp),
        cornerRadius = 30.dp,
        fillOpacity = 0.42f,
        shadowElevation = 20.dp,
        shadowOpacity = 0.12f
    ) {
        Column(modifier = Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(14.dp)) {
            Text(
                KikariaTypography.mixedText(point.title, size = 22, weight = FontWeight.SemiBold),
                color = masteredDeepGreen,
                modifier = Modifier.fillMaxWidth()
            )

            KnowledgeCollectionTagRow(tags = point.tags)
            KnowledgeCollectionInfoPreview(title = "提示", text = point.hint)
            KnowledgeCollectionInfoPreview(title = "答案", text = point.content)

            Box(
                Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(16.dp))
                    .shadow(
                        14.dp,
                        RoundedCornerShape(16.dp),
                        ambientColor = removeCoral.copy(alpha = 0.18f),
                        spotColor = removeCoral.copy(alpha = 0.18f)
                    )
                    .background(if (isDark) KikariaColors.RemoveGradientDark else KikariaColors.RemoveGradientLight)
                    .clickable { onRemove() }
                    .padding(vertical = 14.dp),
                contentAlignment = Alignment.Center
            ) {
                Text("移出已掌握", fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = Color.White)
            }
        }
    }
}

@Composable
private fun KnowledgeCollectionSearchBar(
    text: String,
    onTextChange: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shape = RoundedCornerShape(22.dp)

    Box(
        modifier = modifier
            .fillMaxWidth()
            .shadow(12.dp, shape, ambientColor = sky.copy(alpha = 0.08f), spotColor = sky.copy(alpha = 0.08f))
            .clip(shape)
            .background(glassSurface.copy(alpha = 0.44f))
            .padding(horizontal = 16.dp, vertical = 14.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                imageVector = KikariaIcons.search,
                contentDescription = "搜索",
                modifier = Modifier.size(20.dp),
                tint = softText
            )
            Spacer(modifier = Modifier.width(10.dp))
            BasicTextField(
                value = text,
                onValueChange = onTextChange,
                textStyle = TextStyle(
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Medium,
                    color = deepText
                ),
                modifier = Modifier.weight(1f),
                decorationBox = { innerTextField ->
                    Box {
                        if (text.isEmpty()) {
                            Text(
                                "搜索知识点",
                                fontSize = 15.sp,
                                fontWeight = FontWeight.Medium,
                                color = softText.copy(alpha = 0.6f)
                            )
                        }
                        innerTextField()
                    }
                }
            )
            if (text.isNotEmpty()) {
                Icon(
                    imageVector = KikariaIcons.clearSearch,
                    contentDescription = "清空搜索",
                    modifier = Modifier
                        .clip(RoundedCornerShape(12.dp))
                        .clickable { onTextChange("") }
                        .padding(4.dp)
                        .size(15.dp),
                    tint = if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray
                )
            }
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun KnowledgeCollectionTagRow(tags: List<String>) {
    if (tags.isEmpty()) return

    FlowRow(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp, Alignment.CenterHorizontally),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        tags.forEach { tag ->
            KikariaTagChip(tag = tag, fontSize = 12)
        }
    }
}

@Composable
private fun KnowledgeCollectionInfoPreview(title: String, text: String) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    Column(modifier = Modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Text(title, fontSize = 13.sp, fontWeight = FontWeight.Bold, color = sky)
        Text(
            text.previewText(),
            fontSize = 15.sp,
            fontWeight = FontWeight.Medium,
            color = deepText.copy(alpha = 0.82f),
            lineHeight = 20.sp,
            maxLines = 2,
            overflow = TextOverflow.Ellipsis
        )
    }
}

private fun List<KnowledgePoint>.filterByKnowledgeSearch(query: String): List<KnowledgePoint> {
    val trimmedQuery = query.trim()
    if (trimmedQuery.isEmpty()) return this
    return filter { point -> point.matchesKnowledgeSearch(trimmedQuery) }
}

private fun KnowledgePoint.matchesKnowledgeSearch(query: String): Boolean {
    return listOf(title, tags.joinToString(" "), hint, content).any {
        it.contains(query, ignoreCase = true)
    }
}

private fun String.previewText(maxCharacters: Int = 120): String {
    val collapsed = replace("\n", " ")
        .replace("\t", " ")
        .split(" ")
        .filter { it.isNotBlank() }
        .joinToString(" ")

    if (collapsed.isEmpty()) return "暂无内容"
    if (collapsed.length <= maxCharacters) return collapsed
    return collapsed.take(maxCharacters).trimEnd() + "..."
}
