package com.vita0818.kikaria.ui.reinforcement

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
import androidx.compose.ui.graphics.Brush
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
import com.vita0818.kikaria.ui.components.KikariaTagChip
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.viewmodel.KikariaViewModel

@Composable
fun ReinforcementScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit,
    onStartReinforcementReview: () -> Unit
) {
    val points = viewModel.reinforcedPoints
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
                KikariaPageTitle(title = "重点集锦")

                Spacer(modifier = Modifier.height(18.dp))

                if (points.isEmpty()) {
                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 30.dp,
                        fillOpacity = 0.54f
                    ) {
                        KikariaEmptyState(
                            title = "还没有重点",
                            subtitle = "在背诵时查看答案后，可以把知识点加入这里。"
                        )
                    }
                } else {
                    // Start review button (amber gradient, matching iOS ReinforcementStartButton)
                    ReinforcementStartButton(
                        count = points.size,
                        onClick = onStartReinforcementReview,
                        isDark = isDark
                    )

                    Spacer(modifier = Modifier.height(12.dp))

                    // Reinforcement items
                    points.forEach { point ->
                        ReinforcementItem(
                            point = point,
                            onRemove = { viewModel.toggleReinforcement(point) }
                        )
                    }

                    Spacer(modifier = Modifier.height(32.dp))
                }
            }
        }
    }
}

@Composable
private fun ReinforcementStartButton(
    count: Int,
    onClick: () -> Unit,
    isDark: Boolean
) {
    val shape = RoundedCornerShape(28.dp)

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
                text = "开始重点背诵",
                fontSize = 20.sp,
                fontWeight = FontWeight.SemiBold,
                color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
            )
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    text = KikariaTypography.mixedText("$count", size = 20, weight = FontWeight.Bold),
                    color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
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
private fun ReinforcementItem(
    point: KnowledgePoint,
    onRemove: () -> Unit
) {
    var expanded by remember { mutableStateOf(false) }
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
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
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = KikariaTypography.mixedText(point.title, size = 22, weight = FontWeight.SemiBold),
                        color = deepText
                    )
                    if (point.reinforcementCount > 0) {
                        Spacer(Modifier.height(4.dp))
                        Text(
                            text = "×${point.reinforcementCount}",
                            fontSize = 14.sp,
                            fontWeight = FontWeight.Bold,
                            color = sky
                        )
                    }
                }

                // Tags
                if (point.tags.isNotEmpty()) {
                    Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                        point.tags.take(3).forEach { tag ->
                            KikariaTagChip(tag = tag, fontSize = 11)
                        }
                    }
                }
            }

            // Expanded content with preview
            AnimatedVisibility(
                visible = expanded,
                enter = expandVertically(),
                exit = shrinkVertically()
            ) {
                Column {
                    Spacer(Modifier.height(10.dp))
                    // Hint preview
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
                    // Answer preview
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

            // Remove button (matching iOS action button with remove gradient)
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
                    text = "移出重点集锦",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = Color.White
                )
            }
        }
    }
}
