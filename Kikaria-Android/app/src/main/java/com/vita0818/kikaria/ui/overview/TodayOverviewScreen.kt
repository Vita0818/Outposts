package com.vita0818.kikaria.ui.overview

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
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaScrollPageShell
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics

/**
 * Today Overview screen translated from the iOS TodayOverviewView in ContentView.swift.
 *
 * Shows today's study activity summary including mastered count vs goal,
 */
@Composable
fun TodayOverviewScreen(
    presetName: String,
    todayMasteredCount: Int,
    todayHintCount: Int,
    todayReviewCount: Int,
    totalMasteredCount: Int,
    dailyGoal: Int,
    countdownDays: Int,
    onBack: () -> Unit,
    onOpenHistory: () -> Unit = {}
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val masteredDeepGreen = if (isDark) KikariaColors.MasteredDeepGreenDark
        else KikariaColors.MasteredDeepGreen
    val blueGray = if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray

    val remainingToGoal = maxOf(0, dailyGoal - todayMasteredCount)
    val progressMessage = when {
        todayMasteredCount >= dailyGoal ->
            "今日目标已经达成，保持这份节奏就很好。"
        todayReviewCount > 0 ->
            "今日已经进入状态，还差 $remainingToGoal 个新增掌握达到目标。"
        else ->
            "今天还很安静，可以从一个知识点开始。"
    }

    val metrics = rememberKikariaPhoneMetrics()

    KikariaScrollPageShell(onBack = onBack, metrics = metrics) {
        Spacer(modifier = Modifier.height(metrics.pageTopPadding))
                Column {
                    Text(
                        text = KikariaTypography.mixedText(
                            "今日概览",
                            size = 32,
                            weight = FontWeight.Bold
                        ),
                        color = deepText
                    )
                    Spacer(modifier = Modifier.height(6.dp))
                    Text(
                        text = KikariaTypography.mixedText(
                            presetName,
                            size = 15,
                            weight = FontWeight.Medium
                        ),
                        color = softText
                    )
                }

                Spacer(modifier = Modifier.height(16.dp))

                // ── Hero: Today's mastered progress ──
                KikariaGlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 30.dp,
                    fillOpacity = 0.40f
                ) {
                    Column(
                        modifier = Modifier.padding(22.dp)
                    ) {
                        Text(
                            text = "今日新增已掌握",
                            fontSize = 15.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = softText
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Row(
                            verticalAlignment = Alignment.Bottom
                        ) {
                            Text(
                                text = KikariaTypography.mixedText(
                                    "$todayMasteredCount",
                                    size = 58,
                                    weight = FontWeight.Bold
                                ),
                                color = masteredDeepGreen
                            )
                            Text(
                                text = KikariaTypography.mixedText(
                                    " / $dailyGoal",
                                    size = 24,
                                    weight = FontWeight.SemiBold
                                ),
                                color = softText
                            )
                        }
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = KikariaTypography.mixedText(
                                progressMessage,
                                size = 15,
                                weight = FontWeight.Medium
                            ),
                            color = deepText.copy(alpha = 0.82f),
                            lineHeight = 22.sp
                        )
                    }
                }

                Spacer(modifier = Modifier.height(12.dp))

                // ── Metric grid (2×2) ──
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    OverviewMetricCard(
                        title = "查看答案",
                        value = "$todayReviewCount",
                        modifier = Modifier.weight(1f)
                    )
                    OverviewMetricCard(
                        title = "总已掌握",
                        value = "$totalMasteredCount",
                        modifier = Modifier.weight(1f)
                    )
                }

                Spacer(modifier = Modifier.height(12.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    OverviewMetricCard(
                        title = "查看提示",
                        value = "$todayHintCount",
                        modifier = Modifier.weight(1f)
                    )
                    OverviewMetricCard(
                        title = "倒数",
                        value = if (countdownDays > 0) "${countdownDays}天" else "--",
                        modifier = Modifier.weight(1f)
                    )
                }

                Spacer(modifier = Modifier.height(12.dp))

                // ── Review History link ──
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { onOpenHistory() }
                ) {
                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 26.dp,
                        fillOpacity = 0.38f
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = 20.dp, vertical = 19.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = KikariaTypography.mixedText(
                                    "复习历史",
                                    size = 18,
                                    weight = FontWeight.SemiBold
                                ),
                                color = deepText,
                                modifier = Modifier.weight(1f)
                            )
                            Text(
                                "📅",
                                fontSize = 18.sp
                            )
                            Spacer(modifier = Modifier.padding(start = 12.dp))
                            Text(
                                "›",
                                fontSize = 18.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = blueGray.copy(alpha = 0.52f)
                            )
                        }
                    }
                }

                Spacer(modifier = Modifier.height(32.dp))
    }
}

// ─── Overview Metric Card ───

@Composable
private fun OverviewMetricCard(
    title: String,
    value: String,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    KikariaGlassCard(
        modifier = modifier,
        cornerRadius = 24.dp,
        fillOpacity = 0.34f,
        shadowElevation = 14.dp,
        shadowOpacity = 0.08f
    ) {
        Column(
            modifier = Modifier.padding(18.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Text(
                text = title,
                fontSize = 13.sp,
                fontWeight = FontWeight.SemiBold,
                color = softText
            )
            Text(
                text = KikariaTypography.mixedText(
                    value,
                    size = 46,
                    weight = FontWeight.Bold
                ),
                color = deepText,
                maxLines = 1,
                softWrap = false
            )
                    }
                }
            }
