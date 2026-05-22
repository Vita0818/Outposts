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
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
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
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePreset
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography

/**
 * Preset Selection screen translated from the iOS PresetSelectionView in ContentView.swift.
 *
 * Displays the list of available presets, allows switching, and provides
 * entry points for creating new presets and editing existing ones.
 */
@Composable
fun PresetSelectionScreen(
    presets: List<KnowledgePreset>,
    currentPresetId: String,
    onBack: () -> Unit,
    onSwitchPreset: (KnowledgePreset) -> Unit,
    onNewPreset: () -> Unit = {},
    onEditPreset: (KnowledgePreset) -> Unit = {},
    onDeletePreset: (KnowledgePreset) -> Unit = {}
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    var pendingDelete by remember { mutableStateOf<KnowledgePreset?>(null) }

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
                // Title row
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = KikariaTypography.mixedText(
                            "切换预设",
                            size = 30,
                            weight = FontWeight.Bold
                        ),
                        color = deepText,
                        modifier = Modifier.weight(1f)
                    )
                }

                Spacer(modifier = Modifier.height(6.dp))

                Text(
                    text = KikariaTypography.mixedText(
                        "选择一个预设开始学习，或创建新的预设。",
                        size = 15,
                        weight = FontWeight.Medium
                    ),
                    color = softText
                )

                Spacer(modifier = Modifier.height(14.dp))

                // New preset button
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(22.dp))
                        .shadow(
                            16.dp, RoundedCornerShape(22.dp),
                            ambientColor = sky.copy(alpha = 0.18f),
                            spotColor = sky.copy(alpha = 0.18f)
                        )
                        .background(
                            if (isDark) KikariaColors.ActionGradientDark
                            else KikariaColors.ActionGradientLight
                        )
                        .clickable { onNewPreset() }
                        .padding(horizontal = 20.dp, vertical = 18.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.Center
                    ) {
                        Text(
                            text = "+",
                            fontSize = 20.sp,
                            fontWeight = FontWeight.Bold,
                            color = Color.White
                        )
                        Spacer(modifier = Modifier.padding(start = 10.dp))
                        Text(
                            text = "上传新预设",
                            fontSize = 17.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = Color.White
                        )
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                // Preset cards
                presets.forEach { preset ->
                    val isCurrent = preset.id == currentPresetId
                    PresetCard(
                        preset = preset,
                        isCurrent = isCurrent,
                        onSelect = { onSwitchPreset(preset) },
                        onEdit = { onEditPreset(preset) },
                        onDelete = {
                            if (preset.isBuiltIn) {
                                // Built-in presets cannot be deleted
                            } else {
                                pendingDelete = preset
                            }
                        }
                    )
                }

                if (presets.isEmpty()) {
                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 28.dp,
                        fillOpacity = 0.44f
                    ) {
                        Column(
                            modifier = Modifier.padding(30.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                text = "暂无预设",
                                fontSize = 20.sp,
                                fontWeight = FontWeight.Bold,
                                color = deepText
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = "点击上方按钮上传你的第一个预设。",
                                fontSize = 15.sp,
                                fontWeight = FontWeight.Normal,
                                color = softText
                            )
                        }
                    }
                }

                // Delete confirmation
                if (pendingDelete != null) {
                    val preset = pendingDelete!!
                    Spacer(modifier = Modifier.height(16.dp))
                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 20.dp,
                        fillOpacity = 0.50f
                    ) {
                        Column(
                            modifier = Modifier.padding(20.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                text = "确认删除「${preset.name}」？",
                                fontSize = 17.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = deepText
                            )
                            Text(
                                text = "此操作不可撤销。",
                                fontSize = 14.sp,
                                fontWeight = FontWeight.Medium,
                                color = softText
                            )
                            Spacer(modifier = Modifier.height(14.dp))
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(12.dp)
                            ) {
                                Box(
                                    modifier = Modifier
                                        .weight(1f)
                                        .clip(RoundedCornerShape(14.dp))
                                        .background(
                                            (if (isDark) KikariaColors.MistDark else KikariaColors.Mist)
                                                .copy(alpha = 0.6f)
                                        )
                                        .clickable { pendingDelete = null }
                                        .padding(vertical = 14.dp),
                                    contentAlignment = Alignment.Center
                                ) {
                                    Text(
                                        "取消",
                                        fontSize = 15.sp,
                                        fontWeight = FontWeight.SemiBold,
                                        color = deepText
                                    )
                                }
                                Box(
                                    modifier = Modifier
                                        .weight(1f)
                                        .clip(RoundedCornerShape(14.dp))
                                        .background(
                                            if (isDark) KikariaColors.RemoveGradientDark
                                            else KikariaColors.RemoveGradientLight
                                        )
                                        .clickable {
                                            onDeletePreset(preset)
                                            pendingDelete = null
                                        }
                                        .padding(vertical = 14.dp),
                                    contentAlignment = Alignment.Center
                                ) {
                                    Text(
                                        "删除",
                                        fontSize = 15.sp,
                                        fontWeight = FontWeight.SemiBold,
                                        color = Color.White
                                    )
                                }
                            }
                        }
                    }
                }

                Spacer(modifier = Modifier.height(32.dp))
            }
        }
    }
}

// ─── Preset Card ───

@Composable
private fun PresetCard(
    preset: KnowledgePreset,
    isCurrent: Boolean,
    onSelect: () -> Unit,
    onEdit: () -> Unit,
    onDelete: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val cyan = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral

    KikariaGlassCard(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp),
        cornerRadius = 26.dp,
        fillOpacity = if (isCurrent) 0.48f else 0.38f,
        shadowElevation = if (isCurrent) 20.dp else 16.dp,
        shadowOpacity = if (isCurrent) 0.14f else 0.10f
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .clickable { onSelect() }
                .padding(18.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = KikariaTypography.mixedText(
                            preset.name,
                            size = 20,
                            weight = FontWeight.SemiBold
                        ),
                        color = if (isCurrent) sky else deepText,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis
                    )
                    if (preset.subtitle.isNotEmpty()) {
                        Spacer(modifier = Modifier.height(4.dp))
                        Text(
                            text = KikariaTypography.mixedText(
                                preset.subtitle,
                                size = 14,
                                weight = FontWeight.Medium
                            ),
                            color = softText,
                            maxLines = 1,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                }

                if (isCurrent) {
                    Box(
                        modifier = Modifier
                            .clip(RoundedCornerShape(10.dp))
                            .background(sky.copy(alpha = 0.18f))
                            .padding(horizontal = 10.dp, vertical = 5.dp)
                    ) {
                        Text(
                            text = "当前",
                            fontSize = 11.sp,
                            fontWeight = FontWeight.Bold,
                            color = sky
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(8.dp))

            // Description
            if (preset.description.isNotEmpty()) {
                Text(
                    text = preset.description,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.Normal,
                    color = softText.copy(alpha = 0.78f),
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            // Action buttons
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                // Edit button
                Box(
                    modifier = Modifier
                        .weight(1f)
                        .clip(RoundedCornerShape(14.dp))
                        .shadow(
                            8.dp, RoundedCornerShape(14.dp),
                            spotColor = cyan.copy(alpha = 0.10f)
                        )
                        .background(
                            (if (isDark) KikariaColors.GlassSurfaceDark
                            else KikariaColors.GlassSurface).copy(alpha = 0.38f)
                        )
                        .clickable { onEdit() }
                        .padding(vertical = 12.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "编辑",
                        fontSize = 14.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = deepText
                    )
                }

                // Delete button (only for non-built-in presets)
                if (!preset.isBuiltIn) {
                    Box(
                        modifier = Modifier
                            .weight(1f)
                            .clip(RoundedCornerShape(14.dp))
                            .shadow(
                                8.dp, RoundedCornerShape(14.dp),
                                spotColor = removeCoral.copy(alpha = 0.12f)
                            )
                            .background(
                                removeCoral.copy(alpha = 0.15f)
                            )
                            .clickable { onDelete() }
                            .padding(vertical = 12.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = "删除",
                            fontSize = 14.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = removeCoral
                        )
                    }
                }
            }
        }
    }
}
