package com.vita0818.kikaria.ui.mastered

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
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.ui.components.KikariaCircularIconButton
import com.vita0818.kikaria.ui.components.KikariaEmptyState
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.kikariaGlassStroke
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.viewmodel.KikariaViewModel

@Composable
fun MasteredScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit,
    onStartMasteredReview: () -> Unit
) {
    val points = viewModel.masteredPoints
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val masteredDeepGreen = if (isDark) KikariaColors.MasteredDeepGreenDark else KikariaColors.MasteredDeepGreen
    val masteredGreen = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen
    val masteredCompletedGreen = if (isDark) KikariaColors.MasteredCompletedGreenDark else KikariaColors.MasteredCompletedGreen
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface

    KikariaPageShell {
        Box(modifier = Modifier.fillMaxSize()) {
            // Back button
            KikariaCircularIconButton(
                onClick = onBack,
                icon = KikariaIcons.back,
                modifier = Modifier.padding(start = 24.dp, top = 12.dp),
                size = 42.dp
            )

            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 24.dp)
                    .padding(top = 70.dp)
            ) {
                // Page title
                Text(
                    text = KikariaTypography.mixedText(
                        "\u5DF2\u638C\u63E1",
                        size = 32,
                        weight = FontWeight.Bold
                    ),
                    color = deepText
                )

                Spacer(modifier = Modifier.height(18.dp))

                if (points.isEmpty()) {
                    KikariaEmptyState(
                        title = "\u5DF2\u638C\u63E1\u5217\u8868\u4E3A\u7A7A",
                        subtitle = "\u5728\u590D\u4E60\u4E2D\u6807\u8BB0\u638C\u63E1\u540E\uFF0C\u77E5\u8BC6\u70B9\u4F1A\u51FA\u73B0\u5728\u8FD9\u91CC\u3002"
                    )
                } else {
                    // Start review button
                    val shape = RoundedCornerShape(16.dp)
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 8.dp)
                            .height(50.dp)
                            .shadow(12.dp, shape,
                                ambientColor = masteredGreen.copy(alpha = 0.22f),
                                spotColor = masteredGreen.copy(alpha = 0.22f))
                            .clip(shape)
                            .background(
                                if (isDark) KikariaColors.MasteredGradientDark
                                else KikariaColors.MasteredGradientLight
                            )
                            .clickable { onStartMasteredReview() },
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = "\u590D\u4E60\u5DF2\u638C\u63E1 (${points.size})",
                            fontSize = 17.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = Color.White
                        )
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Mastered items
                    points.forEach { point ->
                        MasteredItem(
                            point = point,
                            onRemove = { viewModel.toggleMastered(point) }
                        )
                    }

                    Spacer(modifier = Modifier.height(32.dp))
                }
            }
        }
    }
}

@Composable
private fun MasteredItem(
    point: KnowledgePoint,
    onRemove: () -> Unit
) {
    var expanded by remember { mutableStateOf(false) }
    val isDark = isSystemInDarkTheme()
    val masteredCompletedGreen = if (isDark) KikariaColors.MasteredCompletedGreenDark else KikariaColors.MasteredCompletedGreen
    val masteredDeepGreen = if (isDark) KikariaColors.MasteredDeepGreenDark else KikariaColors.MasteredDeepGreen
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral
    val cardShape = RoundedCornerShape(16.dp)
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp)
            .shadow(10.dp, cardShape,
                ambientColor = (if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen).copy(alpha = 0.08f),
                spotColor = (if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen).copy(alpha = 0.08f))
            .clip(cardShape)
            .background(masteredCompletedGreen.copy(alpha = 0.40f))
            .kikariaGlassStroke(cardShape, isDark)
            .clickable { expanded = !expanded }
            .padding(16.dp)
    ) {
        Column {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = KikariaTypography.mixedText(point.title, size = 16, weight = FontWeight.SemiBold),
                    color = masteredDeepGreen,
                    modifier = Modifier.weight(1f)
                )
                Box(
                    modifier = Modifier
                        .clip(RoundedCornerShape(10.dp))
                        .background(removeCoral.copy(alpha = 0.12f))
                        .clickable { onRemove() }
                        .padding(horizontal = 14.dp, vertical = 7.dp)
                ) {
                    Text(
                        text = "\u79FB\u51FA",
                        fontSize = 13.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = removeCoral
                    )
                }
            }

            if (expanded) {
                Spacer(modifier = Modifier.height(10.dp))
                Text(
                    text = KikariaTypography.mixedText(point.hint, size = 14, weight = FontWeight.Normal),
                    color = softText
                )
                Spacer(modifier = Modifier.height(6.dp))
                Text(
                    text = KikariaTypography.mixedText(point.content, size = 14, weight = FontWeight.Normal),
                    color = deepText
                )
            }
        }
    }
}
