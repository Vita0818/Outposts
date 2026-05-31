package com.vita0818.kikaria.ui.scope

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
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
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaCircularIconButton
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics
import com.vita0818.kikaria.viewmodel.KikariaViewModel

/**
 * Scope selection screen matching iOS ScopeSelectionView (ContentView.swift lines 7153-7282).
 *
 * Apple layout:
 * - Page title "选择范围" with subtitle
 * - Search bar for filtering tags
 * - Adaptive LazyVGrid of ScopeTagChip items
 * - "完成" action button at bottom
 * - Back button overlay
 */
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
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface

    val metrics = rememberKikariaPhoneMetrics()
    val scale = maxOf(metrics.scopeScale, 1f)
    val pagePadding = metrics.innerHorizontalPadding

    var searchText by remember { mutableStateOf("") }
    val filteredTags = if (searchText.isBlank()) allTags
        else allTags.filter { searchText.trim().lowercase() in it.lowercase() }

    KikariaPageShell {
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = if (metrics.isTablet) Alignment.TopCenter else Alignment.TopStart
        ) {
            Column(
                modifier = Modifier
                    .then(
                        if (metrics.isTablet) Modifier.widthIn(max = metrics.mainMaxWidth)
                        else Modifier
                    )
                    .fillMaxSize()
            ) {
                // Scrollable content
                Column(
                    modifier = Modifier
                        .weight(1f)
                        .verticalScroll(rememberScrollState())
                        .padding(horizontal = pagePadding)
                        .padding(top = 24.dp)
                ) {
                    // Title + subtitle
                    Spacer(modifier = Modifier.height(metrics.ipadPortraitListPageTopInset))
                    Text(
                        text = KikariaTypography.mixedText(
                            "选择范围", size = 32, weight = FontWeight.Bold
                        ),
                        color = deepText
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = if (selected.isEmpty())
                            "未选择标签时，会默认使用全部知识点。"
                        else "已选择 ${selected.size} 个标签。",
                        fontSize = (15 * scale).sp,
                        fontWeight = FontWeight.Medium,
                        color = softText
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    // Search bar — matches Apple KikariaSearchBar
                    val searchShape = RoundedCornerShape(22.dp)
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .shadow(12.dp, searchShape,
                                ambientColor = sky.copy(alpha = 0.08f),
                                spotColor = sky.copy(alpha = 0.08f))
                            .clip(searchShape)
                            .background(glassSurface.copy(alpha = 0.44f))
                            .padding(horizontal = 16.dp, vertical = 14.dp)
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Icon(
                                imageVector = KikariaIcons.search,
                                contentDescription = "搜索",
                                modifier = Modifier.size((20 * scale).dp),
                                tint = softText
                            )
                            Spacer(modifier = Modifier.padding(start = 10.dp))
                            androidx.compose.foundation.text.BasicTextField(
                                value = searchText,
                                onValueChange = { searchText = it },
                                textStyle = androidx.compose.ui.text.TextStyle(
                                    fontSize = (15 * scale).sp,
                                    fontWeight = FontWeight.Medium,
                                    color = deepText
                                ),
                                modifier = Modifier.weight(1f),
                                decorationBox = { innerTextField ->
                                    Box {
                                        if (searchText.isEmpty()) {
                                            Text(
                                                "搜索标签或知识点",
                                                fontSize = (15 * scale).sp,
                                                fontWeight = FontWeight.Medium,
                                                color = softText.copy(alpha = 0.6f)
                                            )
                                        }
                                        innerTextField()
                                    }
                                }
                            )
                            if (searchText.isNotEmpty()) {
                                Box(
                                    modifier = Modifier
                                        .clip(RoundedCornerShape(12.dp))
                                        .clickable { searchText = "" }
                                        .padding(4.dp)
                                ) {
                                    Text(
                                        "✕", fontSize = (15 * scale).sp,
                                        color = if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray
                                    )
                                }
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(16.dp))

                    if (filteredTags.isEmpty()) {
                        Spacer(modifier = Modifier.height(32.dp))
                        Text(
                            text = "没有找到相关标签",
                            fontSize = 20.sp,
                            fontWeight = FontWeight.Bold,
                            color = deepText,
                            modifier = Modifier.fillMaxWidth(),
                            textAlign = TextAlign.Center
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "换个关键词试试看。",
                            fontSize = 15.sp,
                            color = softText,
                            modifier = Modifier.fillMaxWidth(),
                            textAlign = TextAlign.Center
                        )
                    } else {
                        // Tag grid — matches Apple LazyVGrid
                        val gridMinWidth = metrics.scopeGridMinimumWidth
                        LazyVerticalGrid(
                            columns = GridCells.Adaptive(minSize = gridMinWidth),
                            horizontalArrangement = Arrangement.spacedBy(metrics.scopeGridSpacing),
                            verticalArrangement = Arrangement.spacedBy(metrics.scopeGridSpacing),
                            contentPadding = PaddingValues(bottom = 96.dp)
                        ) {
                            items(filteredTags) { tag ->
                                val isTagSelected = tag in selected
                                ScopeTagChip(
                                    title = tag,
                                    isSelected = isTagSelected,
                                    scale = scale,
                                    isDark = isDark,
                                    onClick = {
                                        if (isTagSelected) viewModel.selectedTags.remove(tag)
                                        else viewModel.selectedTags.add(tag)
                                    }
                                )
                            }
                        }
                    }
                }

                // Bottom "完成" button — matches Apple
                Box(
                    modifier = Modifier
                        .padding(horizontal = pagePadding)
                        .padding(bottom = 16.dp)
                        .fillMaxWidth()
                        .shadow(18.dp, RoundedCornerShape(28.dp),
                            spotColor = sky.copy(alpha = 0.22f))
                        .clip(RoundedCornerShape(28.dp))
                        .background(actionGrad)
                        .clickable { onBack() }
                        .padding(vertical = (18 * scale).dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "完成",
                        fontSize = (17 * scale).sp,
                        fontWeight = FontWeight.SemiBold,
                        color = Color.White
                    )
                }
            }

            // Back button overlay
            Box(
                modifier = Modifier
                    .align(Alignment.TopStart)
                    .padding(start = metrics.horizontalPadding, top = 12.dp)
            ) {
                KikariaCircularIconButton(
                    onClick = onBack,
                    icon = KikariaIcons.back,
                    contentDescription = "返回",
                    size = metrics.backButtonSize
                )
            }
        }
    }
}

// ─── Scope Tag Chip — matches Apple ScopeTagChip ───

@Composable
private fun ScopeTagChip(
    title: String,
    isSelected: Boolean,
    scale: Float = 1f,
    isDark: Boolean,
    onClick: () -> Unit
) {
    val resolvedScale = maxOf(scale, 1f)
    val shape = RoundedCornerShape((20 * resolvedScale).dp)
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(
                (12 * resolvedScale).dp, shape,
                ambientColor = sky.copy(alpha = if (isSelected) 0.18f else 0.06f),
                spotColor = sky.copy(alpha = if (isSelected) 0.18f else 0.06f)
            )
            .clip(shape)
            .then(
                if (isSelected) Modifier.background(actionGrad)
                else Modifier.background(glassSurface.copy(alpha = 0.34f))
            )
            .clickable { onClick() }
            .padding(horizontal = (14 * resolvedScale).dp, vertical = (16 * resolvedScale).dp),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = KikariaTypography.mixedText(
                title,
                size = (13 * resolvedScale).toInt(),
                weight = FontWeight.SemiBold
            ),
            color = if (isSelected) Color.White else deepText,
            maxLines = 2,
            textAlign = TextAlign.Center
        )
    }
}
