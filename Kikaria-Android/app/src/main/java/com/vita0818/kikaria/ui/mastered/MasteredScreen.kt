package com.vita0818.kikaria.ui.mastered

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.expandVertically
import androidx.compose.animation.shrinkVertically
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
                KikariaPageTitle(title = "已掌握")

                Spacer(modifier = Modifier.height(18.dp))

                if (points.isEmpty()) {
                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 30.dp,
                        fillOpacity = 0.54f
                    ) {
                        KikariaEmptyState(
                            title = "还没有已掌握",
                            subtitle = "在背诵时查看答案后，可以把真正熟悉的知识点标记到这里。"
                        )
                    }
                } else {
                    // Start review button (green gradient, matching iOS MasteredStartButton)
                    MasteredStartButton(
                        count = points.size,
                        onClick = onStartMasteredReview,
                        isDark = isDark
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
    onClick: () -> Unit,
    isDark: Boolean
) {
    KikariaGlassCard(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp)
            .clickable { onClick() },
        cornerRadius = 28.dp,
        fillOpacity = 0.46f,
        shadowElevation = 20.dp,
        shadowOpacity = 0.16f
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 22.dp, vertical = 22.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = "开始复习",
                fontSize = 20.sp,
                fontWeight = FontWeight.SemiBold,
                color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
            )
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    text = KikariaTypography.mixedText("$count", size = 20, weight = FontWeight.Bold),
                    color = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen
                )
                Spacer(Modifier.width(12.dp))
                Text(
                    "›",
                    fontSize = 18.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray
                )
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
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val masteredDeepGreen = if (isDark) KikariaColors.MasteredDeepGreenDark else KikariaColors.MasteredDeepGreen
    val removeGrad = if (isDark) KikariaColors.RemoveGradientDark else KikariaColors.RemoveGradientLight

    KikariaGlassCard(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp)
            .clickable { expanded = !expanded },
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

            // Expanded preview
            AnimatedVisibility(
                visible = expanded,
                enter = expandVertically(),
                exit = shrinkVertically()
            ) {
                Column {
                    Spacer(Modifier.height(10.dp))
                    Text(
                        text = "提示",
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Bold,
                        color = sky
                    )
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = KikariaTypography.mixedText(
                            point.hint.take(120) + if (point.hint.length > 120) "..." else "",
                            size = 15,
                            weight = FontWeight.Medium
                        ),
                        color = softText,
                        lineHeight = 22.sp
                    )
                    Spacer(Modifier.height(8.dp))
                    Text(
                        text = "答案",
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Bold,
                        color = sky
                    )
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = KikariaTypography.mixedText(
                            point.content.take(120) + if (point.content.length > 120) "..." else "",
                            size = 15,
                            weight = FontWeight.Medium
                        ),
                        color = deepText,
                        lineHeight = 22.sp
                    )
                }
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
                    .background(removeGrad)
                    .clickable { onRemove() }
                    .padding(vertical = 14.dp),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = "移出已掌握",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = Color.White
                )
            }
        }
    }
}
