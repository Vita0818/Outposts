package com.vita0818.kikaria.ui.home

import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
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
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.GlassCard
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    viewModel: KikariaViewModel,
    onStartReview: () -> Unit,
    onOpenScope: () -> Unit,
    onOpenReinforcement: () -> Unit,
    onOpenMastered: () -> Unit
) {
    val todayDate = rememberFormattedDate()

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "Kikaria",
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 28.sp,
                        color = KikariaColors.DeepText
                    )
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = KikariaColors.GlassSurface.copy(alpha = 0f)
                )
            )
        },
        containerColor = KikariaColors.GlassSurface
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(20.dp))

            // --- Central Bubble Start Button ---
            StartReviewBubble(
                onClick = onStartReview,
                modifier = Modifier.size(180.dp)
            )

            Spacer(modifier = Modifier.height(32.dp))

            // --- Date & Today Progress Card ---
            GlassCard(
                modifier = Modifier.fillMaxWidth(),
                cornerRadius = 24.dp
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(
                        text = todayDate,
                        style = MaterialTheme.typography.titleMedium,
                        color = KikariaColors.SoftText,
                        fontWeight = FontWeight.Medium
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceEvenly
                    ) {
                        StatItem(
                            label = "今日复习",
                            value = "${viewModel.todayReviewCount}",
                            color = KikariaColors.Sky
                        )
                        StatItem(
                            label = "今日掌握",
                            value = "${viewModel.todayMasteredCount}",
                            color = KikariaColors.MasteredGreen
                        )
                        StatItem(
                            label = "每日目标",
                            value = "${viewModel.dailyGoal}",
                            color = KikariaColors.NextAmber
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            // --- Quick Action Cards ---
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                QuickActionCard(
                    title = "范围选择",
                    subtitle = if (viewModel.selectedTags.isEmpty()) "全部" else "${viewModel.selectedTags.size} 个标签",
                    modifier = Modifier.weight(1f),
                    onClick = onOpenScope
                )
                QuickActionCard(
                    title = "重点集锦",
                    subtitle = "${viewModel.reinforcedPoints.size} 个",
                    modifier = Modifier.weight(1f),
                    onClick = onOpenReinforcement
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                QuickActionCard(
                    title = "已掌握",
                    subtitle = "${viewModel.masteredPoints.size} 个",
                    modifier = Modifier.weight(1f),
                    onClick = onOpenMastered
                )
                QuickActionCard(
                    title = "当前预设",
                    subtitle = viewModel.activePreset?.name ?: "无",
                    modifier = Modifier.weight(1f),
                    onClick = { /* TODO: preset selection dialog */ }
                )
            }

            Spacer(modifier = Modifier.height(32.dp))
        }
    }
}

@Composable
private fun StartReviewBubble(
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val infiniteTransition = rememberInfiniteTransition(label = "bubble_breath")
    val scale by infiniteTransition.animateFloat(
        initialValue = 0.96f,
        targetValue = 1.04f,
        animationSpec = infiniteRepeatable(
            animation = tween(2800),
            repeatMode = androidx.compose.animation.core.RepeatMode.Reverse
        ),
        label = "bubble_scale"
    )

    Box(
        modifier = modifier,
        contentAlignment = Alignment.Center
    ) {
        // Decorative smaller bubbles
        Box(
            modifier = Modifier
                .size(60.dp)
                .scale(scale)
                .align(Alignment.TopEnd)
                .offset(x = 10.dp, y = (-10).dp)
                .clip(CircleShape)
                .background(
                    Brush.radialGradient(
                        colors = listOf(
                            KikariaColors.BubbleLavender.copy(alpha = 0.7f),
                            KikariaColors.BubbleLavender.copy(alpha = 0.2f)
                        )
                    )
                )
        )
        Box(
            modifier = Modifier
                .size(45.dp)
                .scale(1f / scale)
                .align(Alignment.BottomStart)
                .offset(x = (-5).dp, y = 5.dp)
                .clip(CircleShape)
                .background(
                    Brush.radialGradient(
                        colors = listOf(
                            KikariaColors.BubbleMint.copy(alpha = 0.7f),
                            KikariaColors.BubbleMint.copy(alpha = 0.2f)
                        )
                    )
                )
        )

        // Main central circle
        Box(
            modifier = Modifier
                .size(100.dp)
                .scale(scale)
                .clip(CircleShape)
                .background(KikariaColors.ActionGradientLight)
                .clickable { onClick() },
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = "开始",
                color = androidx.compose.ui.graphics.Color.White,
                fontSize = 22.sp,
                fontWeight = FontWeight.Bold
            )
        }
    }
}

@Composable
private fun StatItem(
    label: String,
    value: String,
    color: androidx.compose.ui.graphics.Color
) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
            text = value,
            fontSize = 28.sp,
            fontWeight = FontWeight.Bold,
            color = color
        )
        Text(
            text = label,
            fontSize = 12.sp,
            color = KikariaColors.TertiaryText,
            fontWeight = FontWeight.Medium
        )
    }
}

@Composable
private fun QuickActionCard(
    title: String,
    subtitle: String,
    modifier: Modifier = Modifier,
    onClick: () -> Unit
) {
    Card(
        modifier = modifier.clickable { onClick() },
        shape = RoundedCornerShape(20.dp),
        colors = CardDefaults.cardColors(
            containerColor = KikariaColors.Mist.copy(alpha = 0.6f)
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Text(
                text = title,
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.SemiBold,
                color = KikariaColors.DeepText
            )
            Spacer(modifier = Modifier.height(4.dp))
            Text(
                text = subtitle,
                style = MaterialTheme.typography.bodySmall,
                color = KikariaColors.TertiaryText
            )
        }
    }
}

@Composable
private fun rememberFormattedDate(): String {
    val formatter = SimpleDateFormat("EEEE, MMMM d", Locale.ENGLISH)
    return formatter.format(Date())
}
