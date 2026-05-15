package com.vita0818.kikaria.ui.presets

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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.IconButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePreset
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.viewmodel.KikariaViewModel

/**
 * Preset selection screen.
 * Displays all available presets with name, category, subtitle, and knowledge point count.
 * Tapping a preset switches to it and navigates back.
 *
 * Translated from the preset selection UI in ContentView.swift.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PresetSelectionScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val activeId = viewModel.activePresetId

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "预设选择",
                        fontWeight = FontWeight.SemiBold,
                        color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Text(
                            "←",
                            fontSize = 22.sp,
                            color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = (if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface)
                        .copy(alpha = 0f)
                )
            )
        },
        containerColor = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 24.dp)
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Spacer(modifier = Modifier.height(8.dp))

            Text(
                text = "选择一个预设以切换知识点库。每个预设包含独立的知识点集合。",
                fontSize = 14.sp,
                color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
            )

            Spacer(modifier = Modifier.height(8.dp))

            viewModel.presets.forEach { preset ->
                PresetCard(
                    preset = preset,
                    isActive = preset.id == activeId,
                    isDark = isDark,
                    onClick = {
                        if (preset.id != activeId) {
                            viewModel.switchPreset(preset.id)
                            onBack()
                        }
                    }
                )
            }

            Spacer(modifier = Modifier.height(32.dp))
        }
    }
}

@Composable
private fun PresetCard(
    preset: KnowledgePreset,
    isActive: Boolean,
    isDark: Boolean,
    onClick: () -> Unit
) {
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val tertiaryText = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.10f) else KikariaColors.Sky.copy(alpha = 0.10f)
    val activeBorderColor = if (isDark) KikariaColors.GlassStrokeAccentDark else KikariaColors.GlassStrokeAccent

    val shape = RoundedCornerShape(20.dp)

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(
                elevation = if (isActive) 12.dp else 6.dp,
                shape = shape,
                ambientColor = shadowC,
                spotColor = shadowC
            )
            .clip(shape)
            .background(
                if (isActive)
                    sky.copy(alpha = if (isDark) 0.18f else 0.12f)
                else
                    glassSurface.copy(alpha = if (isDark) 0.55f else 0.70f)
            )
            .clickable { onClick() }
            .padding(horizontal = 20.dp, vertical = 18.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = preset.name,
                        fontSize = 17.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = deepText,
                        maxLines = 1
                    )
                    if (preset.isBuiltIn) {
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = "内置",
                            fontSize = 11.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = sky,
                            modifier = Modifier
                                .clip(RoundedCornerShape(6.dp))
                                .background(sky.copy(alpha = 0.15f))
                                .padding(horizontal = 8.dp, vertical = 2.dp)
                        )
                    }
                }
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = preset.subtitle,
                    fontSize = 13.sp,
                    color = softText,
                    maxLines = 1
                )
                Spacer(modifier = Modifier.height(2.dp))
                Text(
                    text = "${preset.knowledgePointCount} 个知识点 · ${preset.category}",
                    fontSize = 12.sp,
                    color = tertiaryText,
                    maxLines = 1
                )
            }

            if (isActive) {
                Spacer(modifier = Modifier.width(12.dp))
                Text(
                    text = "✓",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = sky
                )
            }
        }
    }
}
