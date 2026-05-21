package com.vita0818.kikaria.ui.home

import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
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
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaProfileAvatar
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale

@Composable
fun HomeScreen(
    viewModel: KikariaViewModel,
    onStartReview: () -> Unit,
    onOpenScope: () -> Unit,
    onOpenReinforcement: () -> Unit,
    onOpenMastered: () -> Unit,
    onOpenPresetSelection: () -> Unit = {},
    onOpenSettings: () -> Unit = {}
) {
    val dateTitle = rememberDateTitle()
    val countdownDays = viewModel.countdownDays
    val daysLeftText = if (countdownDays != null) "$countdownDays Days Left" else "-- Days Left"
    val progressText = "${viewModel.todayMasteredCount}/${viewModel.dailyGoal}"
    val scopeCountText = if (viewModel.selectedTags.isEmpty())
        "${viewModel.allTags.size}" else "${viewModel.selectedTags.size}"
    val reinforcedCount = viewModel.reinforcedPoints.size
    val masteredCount = viewModel.masteredPoints.size
    val presetName = viewModel.activePreset?.name ?: "无"

    val isDark = isSystemInDarkTheme()

    KikariaPageShell {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp, vertical = 14.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // ── Header: Kikaria title + profile avatar ──
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Kikaria",
                    fontSize = 39.sp,
                    fontWeight = FontWeight.SemiBold,
                    fontFamily = FontFamily.Serif,
                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                )

                KikariaProfileAvatar(
                    size = 44.dp,
                    displayName = viewModel.userDisplayName,
                    modifier = Modifier.clickable { onOpenSettings() }
                )
            }

            Spacer(modifier = Modifier.height(32.dp))

            // ── Central start bubble (matching iOS StartReviewButton) ──
            KikariaStartBubble(
                onClick = onStartReview,
                masteredCount = masteredCount
            )

            Spacer(modifier = Modifier.height(30.dp))

            // ── Progress card (today overview link) ──
            KikariaGlassCard(
                modifier = Modifier.fillMaxWidth(),
                cornerRadius = 28.dp,
                fillOpacity = 0.42f,
                shadowElevation = 17.dp,
                shadowOpacity = 0.11f
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 20.dp, vertical = 20.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Column(modifier = Modifier.weight(1f)) {
                        Text(
                            text = KikariaTypography.mixedText(dateTitle, size = 23, weight = FontWeight.SemiBold),
                            color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                            maxLines = 1
                        )
                        Text(
                            text = KikariaTypography.mixedText(daysLeftText, size = 13, weight = FontWeight.SemiBold),
                            color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                            maxLines = 1
                        )
                    }
                    Text(
                        text = KikariaTypography.mixedText(progressText, size = 25, weight = FontWeight.Bold),
                        color = if (isDark) KikariaColors.MasteredDeepGreenDark else KikariaColors.MasteredDeepGreen,
                        maxLines = 1
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Text(
                        KikariaIcons.TEXT_FORWARD,
                        fontSize = 20.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray).copy(alpha = 0.52f)
                    )
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            // ── Dashboard card ──
            KikariaGlassCard(
                modifier = Modifier.fillMaxWidth(),
                cornerRadius = 28.dp,
                fillOpacity = 0.40f,
                shadowElevation = 18.dp,
                shadowOpacity = 0.12f
            ) {
                Column {
                    Row(modifier = Modifier.fillMaxWidth()) {
                        DashboardMetricColumn(
                            title = "范围",
                            value = scopeCountText,
                            tint = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                            modifier = Modifier.weight(1f),
                            onClick = onOpenScope
                        )
                        DashboardDivider()
                        DashboardMetricColumn(
                            title = "重点集锦",
                            value = "$reinforcedCount",
                            tint = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan,
                            modifier = Modifier.weight(1f),
                            onClick = onOpenReinforcement
                        )
                        DashboardDivider()
                        DashboardMetricColumn(
                            title = "已掌握",
                            value = "$masteredCount",
                            tint = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
                            modifier = Modifier.weight(1f),
                            onClick = onOpenMastered
                        )
                    }
                    Box(
                        Modifier
                            .fillMaxWidth()
                            .padding(horizontal = 18.dp)
                            .height(1.dp)
                            .background(
                                (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                                    .copy(alpha = 0.12f)
                            )
                    )
                    Row(
                        Modifier
                            .fillMaxWidth()
                            .clickable { onOpenPresetSelection() }
                            .padding(horizontal = 20.dp, vertical = 16.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            KikariaTypography.mixedText(presetName, size = 16, weight = FontWeight.SemiBold),
                            color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                            maxLines = 1,
                            modifier = Modifier.weight(1f, fill = false)
                        )
                        Text(
                            " 当前预设",
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                            maxLines = 1
                        )
                        Spacer(Modifier.weight(1f))
                        Text(
                            KikariaIcons.TEXT_FORWARD,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray).copy(alpha = 0.58f)
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(12.dp))
        }
    }
}

// ─── Start Bubble (matches iOS StartReviewButton with decorative bubbles and breathing animation) ───

@Composable
private fun KikariaStartBubble(
    onClick: () -> Unit,
    masteredCount: Int = 0
) {
    val isDark = isSystemInDarkTheme()
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.28f) else KikariaColors.Sky.copy(alpha = 0.28f)

    // Breathing animation (matches iOS withAnimation(.easeInOut(duration: 5.4).repeatForever))
    val infiniteTransition = rememberInfiniteTransition(label = "breathe")
    val breatheScale by infiniteTransition.animateFloat(
        initialValue = 0.992f,
        targetValue = 1.018f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400),
            repeatMode = RepeatMode.Reverse
        ),
        label = "breatheScale"
    )

    Box(
        modifier = Modifier.size(272.dp),
        contentAlignment = Alignment.Center
    ) {
        // Decorative orbiting bubbles (simplified from iOS TimelineView orbit)
        // We draw the smaller decorative bubbles at static positions
        val bubbleMint = if (isDark) KikariaColors.BubbleMintDark else KikariaColors.BubbleMint
        val bubbleLavender = if (isDark) KikariaColors.BubbleLavenderDark else KikariaColors.BubbleLavender
        val bubbleGreen = if (isDark) KikariaColors.BubbleGreenDark else KikariaColors.BubbleGreen

        DecorativeBubble(
            size = 92.dp,
            colors = listOf(if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan, bubbleMint),
            modifier = Modifier.offset(x = (-96).dp, y = (-68).dp)
        )
        DecorativeBubble(
            size = 80.dp,
            colors = listOf(bubbleLavender, if (isDark) KikariaColors.MistDark else KikariaColors.Mist),
            modifier = Modifier.offset(x = 102.dp, y = (-56).dp)
        )
        DecorativeBubble(
            size = 78.dp,
            colors = listOf(bubbleGreen, if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan),
            modifier = Modifier.offset(x = 92.dp, y = 80.dp)
        )
        DecorativeBubble(
            size = 74.dp,
            colors = listOf(if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                if (isDark) KikariaColors.BubbleWhiteDark else KikariaColors.BubbleWhite),
            modifier = Modifier.offset(x = (-106).dp, y = 78.dp)
        )

        // Main center circle with gradient + radial highlight + arrow
        Box(
            modifier = Modifier
                .size(190.dp)
                .scale(breatheScale)
                .shadow(28.dp, CircleShape, ambientColor = shadowC, spotColor = shadowC)
                .clip(CircleShape)
                .background(actionGrad)
                .clickable { onClick() },
            contentAlignment = Alignment.Center
        ) {
            // Radial glass highlight
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .clip(CircleShape)
                    .background(
                        Brush.radialGradient(
                            listOf(
                                Color.White.copy(alpha = 0.30f),
                                Color.White.copy(alpha = 0.10f),
                                Color.White.copy(alpha = 0.02f)
                            ),
                            center = androidx.compose.ui.geometry.Offset(57f, 57f),
                            radius = 150f
                        )
                    )
            )
            // Arrow icon (matches iOS "arrow.right")
            Text(
                KikariaIcons.TEXT_ARROW_RIGHT,
                color = Color.White.copy(alpha = 0.96f),
                fontSize = 70.sp,
                fontWeight = FontWeight.Normal
            )
        }
    }
}

@Composable
private fun DecorativeBubble(
    size: androidx.compose.ui.unit.Dp,
    colors: List<Color>,
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .size(size)
            .shadow(14.dp, CircleShape,
                ambientColor = colors.first().copy(alpha = 0.10f),
                spotColor = colors.first().copy(alpha = 0.10f))
            .clip(CircleShape)
            .background(Brush.linearGradient(colors))
    )
}

// ─── Dashboard Helpers ───

@Composable
private fun DashboardMetricColumn(
    title: String,
    value: String,
    tint: Color,
    modifier: Modifier = Modifier,
    onClick: () -> Unit
) {
    Box(
        modifier
            .clickable { onClick() }
            .padding(horizontal = 12.dp, vertical = 18.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Text(
                text = title,
                fontSize = 13.sp,
                fontWeight = FontWeight.SemiBold,
                color = if (isSystemInDarkTheme()) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                maxLines = 1
            )
            Spacer(Modifier.height(8.dp))
            Text(
                text = KikariaTypography.mixedText(value, size = 24, weight = FontWeight.Bold),
                color = tint,
                maxLines = 1
            )
        }
    }
}

@Composable
private fun DashboardDivider() {
    val isDark = isSystemInDarkTheme()
    Box(
        Modifier
            .width(1.dp)
            .height(42.dp)
            .background((if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray).copy(alpha = 0.16f))
    )
}

// ─── Date helpers ───

private fun rememberDateTitle(): String {
    val calendar = Calendar.getInstance()
    val day = calendar.get(Calendar.DAY_OF_MONTH)
    val monthFormat = SimpleDateFormat("MMM", Locale.ENGLISH)
    val month = monthFormat.format(calendar.time)
    return "$month $day${ordinalSuffix(day)}"
}

private fun ordinalSuffix(day: Int): String {
    val lastTwo = day % 100
    if (lastTwo == 11 || lastTwo == 12 || lastTwo == 13) return "th"
    return when (day % 10) { 1 -> "st"; 2 -> "nd"; 3 -> "rd"; else -> "th" }
}
