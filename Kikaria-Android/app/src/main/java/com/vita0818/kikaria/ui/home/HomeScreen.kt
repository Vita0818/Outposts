package com.vita0818.kikaria.ui.home

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
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaMetricLabel
import com.vita0818.kikaria.ui.components.KikariaMetricValue
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
    onOpenSettings: () -> Unit = {},
    onOpenTodayOverview: () -> Unit = {}
) {
    val dateTitle = rememberDateTitle()
    val countdownDays = viewModel.countdownDays
    val daysLeftText = if (countdownDays != null) "$countdownDays Days Left" else "-- Days Left"
    val progressText = "${viewModel.todayMasteredCount}/${viewModel.dailyGoal}"
    val scopeCountText = if (viewModel.selectedTags.isEmpty())
        "${viewModel.allTags.size}" else "${viewModel.selectedTags.size}"
    val reinforcedCount = viewModel.reinforcedPoints.size
    val masteredCount = viewModel.masteredPoints.size
    val presetName = viewModel.activePreset?.name ?: "\u65E0"

    val isDark = isSystemInDarkTheme()

    KikariaPageShell {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp)
                .padding(top = 14.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // ── Header: Kikaria title + profile avatar ──
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = KikariaTypography.mixedText("Kikaria", size = 39, weight = FontWeight.SemiBold),
                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                )

                KikariaProfileAvatar(
                    size = 44.dp,
                    displayName = viewModel.userDisplayName,
                    modifier = Modifier.clickable { onOpenSettings() }
                )
            }

            Spacer(modifier = Modifier.height(32.dp))

            // ── Central start bubble ──
            KikariaStartBubble(
                onClick = onStartReview,
                dailyGoal = viewModel.dailyGoal,
                masteredCount = masteredCount,
                countdownDays = countdownDays
            )

            Spacer(modifier = Modifier.height(30.dp))

            // ── Progress card ──
            KikariaGlassCard(
                modifier = Modifier.fillMaxWidth().clickable { onOpenTodayOverview() },
                cornerRadius = 28.dp,
                fillOpacity = 0.40f
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth().padding(horizontal = 20.dp, vertical = 20.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Column(modifier = Modifier.weight(1f)) {
                        Text(
                            text = KikariaTypography.mixedText(dateTitle, size = 23, weight = FontWeight.SemiBold),
                            color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                            maxLines = 1
                        )
                        Text(
                            text = daysLeftText,
                            fontSize = 13.sp,
                            fontWeight = FontWeight.SemiBold,
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
                        "\u203A",
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
                fillOpacity = 0.40f
            ) {
                Column {
                    Row(modifier = Modifier.fillMaxWidth()) {
                        DashboardMetricColumn(
                            title = "\u8303\u56F4",
                            value = scopeCountText,
                            tint = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                            modifier = Modifier.weight(1f),
                            onClick = onOpenScope
                        )
                        DashboardDivider()
                        DashboardMetricColumn(
                            title = "\u91CD\u70B9\u96C6\u9526",
                            value = "$reinforcedCount",
                            tint = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan,
                            modifier = Modifier.weight(1f),
                            onClick = onOpenReinforcement
                        )
                        DashboardDivider()
                        DashboardMetricColumn(
                            title = "\u5DF2\u638C\u63E1",
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
                            .background((if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray).copy(alpha = 0.12f))
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
                            " \u5F53\u524D\u9884\u8BBE",
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                            maxLines = 1
                        )
                        Spacer(Modifier.weight(1f))
                        Text(
                            "\u203A",
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

// ─── Start Bubble (matches iOS StartReviewButton) ───

@Composable
private fun KikariaStartBubble(
    onClick: () -> Unit,
    dailyGoal: Int = 20,
    masteredCount: Int = 0,
    countdownDays: Int? = null
) {
    val isDark = isSystemInDarkTheme()
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.28f) else KikariaColors.Sky.copy(alpha = 0.28f)

    // Decorative bubble colors matching iOS SoftBubble
    val bubbleColors = listOf(
        if (isDark) KikariaColors.BubbleMintDark else KikariaColors.BubbleMint,
        if (isDark) KikariaColors.BubbleLavenderDark else KikariaColors.BubbleLavender,
        if (isDark) KikariaColors.BubbleGreenDark else KikariaColors.BubbleGreen,
        if (isDark) KikariaColors.BubbleWhiteDark else KikariaColors.BubbleWhite
    )

    Box(
        modifier = Modifier.size(220.dp),
        contentAlignment = Alignment.Center
    ) {
        // ── Decorative orbiting bubbles (matches iOS DecorativeBubble pattern) ──
        // Top-right bubble
        Box(
            modifier = Modifier
                .align(Alignment.TopEnd)
                .padding(end = 14.dp, top = 10.dp)
                .size(36.dp)
                .shadow(12.dp, CircleShape,
                    ambientColor = bubbleColors[0].copy(alpha = 0.18f),
                    spotColor = bubbleColors[0].copy(alpha = 0.18f))
                .clip(CircleShape)
                .background(bubbleColors[0].copy(alpha = 0.55f))
        )
        // Bottom-left bubble
        Box(
            modifier = Modifier
                .align(Alignment.BottomStart)
                .padding(start = 12.dp, bottom = 14.dp)
                .size(44.dp)
                .shadow(14.dp, CircleShape,
                    ambientColor = bubbleColors[1].copy(alpha = 0.20f),
                    spotColor = bubbleColors[1].copy(alpha = 0.20f))
                .clip(CircleShape)
                .background(bubbleColors[1].copy(alpha = 0.50f))
        )
        // Top-left small bubble
        Box(
            modifier = Modifier
                .align(Alignment.TopStart)
                .padding(start = 8.dp, top = 4.dp)
                .size(28.dp)
                .shadow(9.dp, CircleShape,
                    ambientColor = bubbleColors[2].copy(alpha = 0.16f),
                    spotColor = bubbleColors[2].copy(alpha = 0.16f))
                .clip(CircleShape)
                .background(bubbleColors[2].copy(alpha = 0.48f))
        )
        // Bottom-right small bubble
        Box(
            modifier = Modifier
                .align(Alignment.BottomEnd)
                .padding(end = 6.dp, bottom = 6.dp)
                .size(32.dp)
                .shadow(10.dp, CircleShape,
                    ambientColor = bubbleColors[3].copy(alpha = 0.17f),
                    spotColor = bubbleColors[3].copy(alpha = 0.17f))
                .clip(CircleShape)
                .background(bubbleColors[3].copy(alpha = 0.50f))
        )

        // ── Central start circle ──
        Box(
            modifier = Modifier
                .size(190.dp)
                .shadow(28.dp, CircleShape, ambientColor = shadowC, spotColor = shadowC)
                .clip(CircleShape)
                .background(actionGrad)
                .clickable { onClick() },
            contentAlignment = Alignment.Center
        ) {
            // Radial glass highlight — matches iOS radial gradient overlay
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
            // Arrow icon (matches iOS "arrow.right" / "→")
            Text(
                KikariaIcons.TEXT_ARROW_RIGHT,
                color = Color.White.copy(alpha = 0.96f),
                fontSize = 70.sp,
                fontWeight = FontWeight.Normal,
                textAlign = TextAlign.Center
            )
        }
    }
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
    val isDark = isSystemInDarkTheme()
    Box(
        modifier
            .clickable { onClick() }
            .padding(horizontal = 12.dp, vertical = 18.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            KikariaMetricLabel(title)
            Spacer(Modifier.height(8.dp))
            KikariaMetricValue(value = value, tint = tint, fontSize = 24)
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

// Import needed for TEXT_ARROW_RIGHT symbol
private val KikariaIcons = com.vita0818.kikaria.ui.components.KikariaIcons
