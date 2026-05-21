package com.vita0818.kikaria.ui.home

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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.clickable
import androidx.compose.foundation.background
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
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
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaProfileAvatar
import com.vita0818.kikaria.ui.components.kikariaGlassStroke
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
    onOpenMastered: () -> Unit
) {
    val dateTitle = rememberDateTitle()
    val daysLeftText = "-- Days Left"
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

                KikariaProfileAvatar(size = 44.dp)
            }

            Spacer(modifier = Modifier.height(32.dp))

            // ── Central start bubble ──
            KikariaStartBubble(onClick = onStartReview)

            Spacer(modifier = Modifier.height(30.dp))

            // ── Progress card ──
            TodayProgressCard(
                dateText = dateTitle,
                daysLeftText = daysLeftText,
                progressText = progressText,
                onClick = { /* today overview */ }
            )

            Spacer(modifier = Modifier.height(12.dp))

            // ── Dashboard card ──
            DashboardCard(
                scopeCountText = scopeCountText,
                reinforcedCount = reinforcedCount,
                masteredCount = masteredCount,
                presetName = presetName,
                onOpenScope = onOpenScope,
                onOpenReinforcement = onOpenReinforcement,
                onOpenMastered = onOpenMastered
            )

            Spacer(modifier = Modifier.height(12.dp))
        }
    }
}

// ─── Start Bubble (matches iOS StartReviewButton / orbiter) ───

@Composable
private fun KikariaStartBubble(onClick: () -> Unit) {
    val isDark = isSystemInDarkTheme()
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.28f) else KikariaColors.Sky.copy(alpha = 0.28f)

    Box(
        modifier = Modifier
            .size(190.dp)
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
        // Play arrow icon (matches iOS "→" in start button)
        Text(
            "\u25B6", // ▶
            color = Color.White.copy(alpha = 0.96f),
            fontSize = 64.sp,
            fontWeight = FontWeight.Normal,
            textAlign = TextAlign.Center
        )
    }
}

// ─── Today Progress Card ───

@Composable
private fun TodayProgressCard(
    dateText: String,
    daysLeftText: String,
    progressText: String,
    onClick: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val masteredGreen = if (isDark) KikariaColors.MasteredDeepGreenDark else KikariaColors.MasteredDeepGreen
    val blueGray = if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.12f) else KikariaColors.Sky.copy(alpha = 0.12f)
    val cornerRadius = 28.dp
    val shape = RoundedCornerShape(cornerRadius)

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(18.dp, shape, ambientColor = shadowC, spotColor = shadowC)
            .clip(shape)
            .background(glassSurface.copy(alpha = 0.40f))
            .kikariaGlassStroke(shape, isDark)
            .clickable { onClick() }
            .padding(horizontal = 20.dp, vertical = 20.dp)
    ) {
        Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = KikariaTypography.mixedText(dateText, size = 23, weight = FontWeight.SemiBold),
                    color = deepText,
                    maxLines = 1
                )
                Text(
                    text = daysLeftText,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = softText,
                    maxLines = 1
                )
            }
            Text(
                text = KikariaTypography.mixedText(progressText, size = 25, weight = FontWeight.Bold),
                color = masteredGreen,
                maxLines = 1
            )
            Spacer(modifier = Modifier.width(8.dp))
            Text("\u203A", fontSize = 20.sp, fontWeight = FontWeight.SemiBold, color = blueGray.copy(alpha = 0.52f))
        }
    }
}

// ─── Dashboard Card ───

@Composable
private fun DashboardCard(
    scopeCountText: String, reinforcedCount: Int, masteredCount: Int,
    presetName: String,
    onOpenScope: () -> Unit, onOpenReinforcement: () -> Unit, onOpenMastered: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val blueGray = if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val cyan = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan
    val masteredGreen = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen
    val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.12f) else KikariaColors.Sky.copy(alpha = 0.12f)
    val cornerRadius = 28.dp
    val shape = RoundedCornerShape(cornerRadius)

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(18.dp, shape, ambientColor = shadowC, spotColor = shadowC)
            .clip(shape)
            .background(glassSurface.copy(alpha = 0.40f))
            .kikariaGlassStroke(shape, isDark)
    ) {
        Column {
            Row(modifier = Modifier.fillMaxWidth()) {
                DashboardMetricColumn("\u8303\u56F4", scopeCountText, sky, softText, Modifier.weight(1f), onOpenScope)
                Box(Modifier.width(1.dp).height(42.dp).align(Alignment.CenterVertically).background(blueGray.copy(alpha = 0.16f)))
                DashboardMetricColumn("\u91CD\u70B9\u96C6\u9526", "$reinforcedCount", cyan, softText, Modifier.weight(1f), onOpenReinforcement)
                Box(Modifier.width(1.dp).height(42.dp).align(Alignment.CenterVertically).background(blueGray.copy(alpha = 0.16f)))
                DashboardMetricColumn("\u5DF2\u638C\u63E1", "$masteredCount", masteredGreen, softText, Modifier.weight(1f), onOpenMastered)
            }
            Box(Modifier.fillMaxWidth().padding(horizontal = 18.dp).height(1.dp).background(blueGray.copy(alpha = 0.12f)))
            Row(
                Modifier
                    .fillMaxWidth()
                    .clickable { /* preset selection */ }
                    .padding(horizontal = 20.dp, vertical = 16.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    KikariaTypography.mixedText(presetName, size = 16, weight = FontWeight.SemiBold),
                    color = deepText,
                    maxLines = 1,
                    modifier = Modifier.weight(1f, fill = false)
                )
                Text(
                    " \u5F53\u524D\u9884\u8BBE",
                    fontSize = 12.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = softText,
                    maxLines = 1
                )
                Spacer(Modifier.weight(1f))
                Text("\u203A", fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = blueGray.copy(alpha = 0.58f))
            }
        }
    }
}

@Composable
private fun DashboardMetricColumn(
    title: String, value: String, tint: Color, labelColor: Color,
    modifier: Modifier = Modifier, onClick: () -> Unit
) {
    Box(
        modifier
            .clickable { onClick() }
            .padding(horizontal = 12.dp, vertical = 18.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Text(title, fontSize = 13.sp, fontWeight = FontWeight.SemiBold, color = labelColor, maxLines = 1)
            Spacer(Modifier.height(8.dp))
            Text(
                KikariaTypography.mixedText(value, size = 24, weight = FontWeight.Bold),
                color = tint,
                maxLines = 1,
                textAlign = TextAlign.Center
            )
        }
    }
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
