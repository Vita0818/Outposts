package com.vita0818.kikaria.ui.mastered

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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaEmptyState
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaPageTitle
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
                KikariaPageTitle(title = "\u5DF2\u638C\u63E1")

                Spacer(modifier = Modifier.height(18.dp))

                if (points.isEmpty()) {
                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 30.dp,
                        fillOpacity = 0.54f
                    ) {
                        KikariaEmptyState(
                            title = "\u8FD8\u6CA1\u6709\u5DF2\u638C\u63E1",
                            subtitle = "\u5728\u80CC\u8BF5\u65F6\u67E5\u770B\u7B54\u6848\u540E\uFF0C\u53EF\u4EE5\u628A\u771F\u6B63\u719F\u6089\u7684\u77E5\u8BC6\u70B9\u6807\u8BB0\u5230\u8FD9\u91CC\u3002"
                        )
                    }
                } else {
                    // Start review button
                    MasteredStartButton(
                        count = points.size,
                        onClick = onStartMasteredReview
                    )

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
private fun MasteredStartButton(
    count: Int,
    onClick: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val masteredGrad = if (isDark) KikariaColors.MasteredGradientDark else KikariaColors.MasteredGradientLight
    val shape = RoundedCornerShape(28.dp)

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp)
            .shadow(20.dp, shape,
                ambientColor = (if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen).copy(alpha = 0.16f),
                spotColor = (if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen).copy(alpha = 0.16f))
            .clip(shape)
            .background(masteredGrad)
            .clickable { onClick() }
            .padding(horizontal = 22.dp, vertical = 22.dp),
        contentAlignment = Alignment.Center
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = "\u5F00\u59CB\u590D\u4E60",
                fontSize = 20.sp,
                fontWeight = FontWeight.SemiBold,
                color = Color.White
            )
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    text = KikariaTypography.mixedText("$count", size = 20, weight = FontWeight.Bold),
                    color = Color.White
                )
                Spacer(Modifier.padding(start = 12.dp))
                Text("\u203A", fontSize = 18.sp, fontWeight = FontWeight.SemiBold,
                    color = Color.White.copy(alpha = 0.72f))
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
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val masteredDeepGreen = if (isDark) KikariaColors.MasteredDeepGreenDark else KikariaColors.MasteredDeepGreen

    KikariaGlassCard(
        modifier = Modifier.fillMaxWidth().padding(vertical = 6.dp).clickable { expanded = !expanded },
        cornerRadius = 30.dp,
        fillOpacity = 0.42f,
        shadowElevation = 20.dp,
        shadowOpacity = 0.12f
    ) {
        Column(modifier = Modifier.padding(18.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = KikariaTypography.mixedText(point.title, size = 22, weight = FontWeight.SemiBold),
                    color = masteredDeepGreen,
                    modifier = Modifier.weight(1f)
                )
            }

            if (expanded) {
                Spacer(Modifier.height(10.dp))
                Text(
                    text = "\u63D0\u793A",
                    fontSize = 13.sp,
                    fontWeight = FontWeight.Bold,
                    color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                )
                Text(
                    text = KikariaTypography.mixedText(point.hint, size = 15, weight = FontWeight.Medium),
                    color = softText,
                    lineHeight = 22.sp
                )
                Spacer(Modifier.height(8.dp))
                Text(
                    text = "\u7B54\u6848",
                    fontSize = 13.sp,
                    fontWeight = FontWeight.Bold,
                    color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                )
                Text(
                    text = KikariaTypography.mixedText(point.content, size = 15, weight = FontWeight.Medium),
                    color = deepText,
                    lineHeight = 22.sp
                )
            }

            Spacer(Modifier.height(14.dp))

            // Remove button
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(16.dp))
                    .shadow(14.dp, RoundedCornerShape(16.dp),
                        ambientColor = (if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral).copy(alpha = 0.18f),
                        spotColor = (if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral).copy(alpha = 0.18f))
                    .background(
                        if (isDark) KikariaColors.RemoveGradientDark else KikariaColors.RemoveGradientLight
                    )
                    .clickable { onRemove() }
                    .padding(vertical = 14.dp),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = "\u79FB\u51FA\u5DF2\u638C\u63E1",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = Color.White
                )
            }
        }
    }
}
