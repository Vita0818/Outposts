package com.vita0818.kikaria.ui.presets

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaPageTitle
import com.vita0818.kikaria.ui.components.KikariaPresetCard
import com.vita0818.kikaria.viewmodel.KikariaViewModel

@Composable
fun PresetSelectionScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit
) {
    val presets = viewModel.presets
    val activeId = viewModel.activePresetId

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
                KikariaPageTitle(title = "切换预设")

                Spacer(modifier = Modifier.height(18.dp))

                presets.forEach { preset ->
                    val pointCount = try {
                        com.vita0818.kikaria.util.MarkdownParser.parseKnowledgePoints(preset.markdownText).size
                    } catch (_: Exception) { 0 }

                    KikariaPresetCard(
                        name = preset.name,
                        isCurrent = preset.id == activeId,
                        pointCount = pointCount,
                        onSelect = {
                            if (preset.id != activeId) {
                                viewModel.switchPreset(preset.id)
                            }
                        },
                        modifier = Modifier.padding(vertical = 6.dp)
                    )
                }

                Spacer(modifier = Modifier.height(34.dp))
            }
        }
    }
}
