package com.vita0818.kikaria.ui.home

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
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.draw.scale
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.theme.KikariaColors
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
    val daysLeftText = rememberDaysLeftText()
    val progressText = "${viewModel.todayMasteredCount}/${viewModel.dailyGoal}"
    val scopeCountText = if (viewModel.selectedTags.isEmpty())
        "${viewModel.allTags.size}" else "${viewModel.selectedTags.size}"
    val reinforcedCount = viewModel.reinforcedPoints.size
    val masteredCount = viewModel.masteredPoints.size
    val presetName = viewModel.activePreset?.name ?: "无"

    val isDark = isSystemInDarkTheme()
    val pageGradient = if (isDark) KikariaColors.PageGradientDark else KikariaColors.PageGradientLight

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(pageGradient)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(28.dp))

            // --- Header: Kikaria title + avatar ---
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Kikaria",
                    fontFamily = FontFamily.Serif,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 39.sp,
                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                )

                Box(
                    modifier = Modifier
                        .size(44.dp)
                        .clip(CircleShape)
                        .background(
                            Brush.linearGradient(
                                colors = if (isDark)
                                    listOf(KikariaColors.SkyDark, KikariaColors.CyanDark)
                                else
                                    listOf(KikariaColors.Sky, KikariaColors.Cyan)
                            )
                        ),
                    contentAlignment = Alignment.Center
                ) {
                    Text("V", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 17.sp)
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            KikariaStartButton(onClick = onStartReview, isDark = isDark)

            Spacer(modifier = Modifier.height(24.dp))

            TodayProgressCard(
                dateText = dateTitle,
                daysLeftText = daysLeftText,
                progressText = progressText,
                isDark = isDark,
                onClick = { /* TODO: today overview */ }
            )

            Spacer(modifier = Modifier.height(12.dp))

            DashboardCard(
                scopeCountText = scopeCountText,
                reinforcedCount = reinforcedCount,
                masteredCount = masteredCount,
                presetName = presetName,
                isDark = isDark,
                onOpenScope = onOpenScope,
                onOpenReinforcement = onOpenReinforcement,
                onOpenMastered = onOpenMastered
            )

            Spacer(modifier = Modifier.height(32.dp))
        }
    }
}

// ─── Liquid Glass helpers ───

/**
 * Creates the layered glass card border gradient composable.
 * Matches iOS LiquidGlassCardModifier stroke: white → faint white → accent cyan.
 */
private fun glassCardStrokeColors(isDark: Boolean): List<Color> {
    val accent = if (isDark) KikariaColors.GlassStrokeAccentDark else KikariaColors.GlassStrokeAccent
    return listOf(
        Color.White.copy(alpha = if (isDark) 0.36f else 0.44f),
        Color.White.copy(alpha = if (isDark) 0.08f else 0.10f),
        accent.copy(alpha = if (isDark) 0.22f else 0.14f)
    )
}

private fun glassCircleStrokeColors(isDark: Boolean): List<Color> {
    val accent = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    return listOf(
        Color.White.copy(alpha = if (isDark) 0.38f else 0.42f),
        Color.White.copy(alpha = if (isDark) 0.08f else 0.09f),
        accent.copy(alpha = if (isDark) 0.24f else 0.12f)
    )
}

/**
 * Draws a glass stroke overlay using Compose drawBehind.
 * This approximates the iOS gradient border on glass cards.
 */
private fun Modifier.liquidGlassStroke(
    shape: RoundedCornerShape,
    isDark: Boolean,
    lineWidth: Float = 1f
): Modifier = this.drawBehind {
    val strokeWidth = lineWidth * density
    val colors = glassCardStrokeColors(isDark)
    drawRoundRect(
        brush = Brush.linearGradient(
            colors = colors,
            start = Offset.Zero,
            end = Offset(size.width, size.height)
        ),
        cornerRadius = CornerRadius(
            shape.topStart.toPx(size, density),
            shape.topEnd.toPx(size, density)
        ),
        style = Stroke(width = strokeWidth)
    )
}

private fun Modifier.liquidGlassCircleStroke(
    isDark: Boolean,
    lineWidth: Float = 1f
): Modifier = this.drawBehind {
    val strokeWidth = lineWidth * density
    val colors = glassCircleStrokeColors(isDark)
    drawCircle(
        brush = Brush.linearGradient(
            colors = colors,
            start = Offset.Zero,
            end = Offset(size.width, size.height)
        ),
        radius = size.minDimension / 2f - strokeWidth / 2f,
        style = Stroke(width = strokeWidth)
    )
}

// ─── Start Button ───

@Composable
private fun KikariaStartButton(onClick: () -> Unit, isDark: Boolean) {
    val breatheTransition = rememberInfiniteTransition(label = "breathe")
    val breatheScale by breatheTransition.animateFloat(
        initialValue = 0.992f,
        targetValue = 1.018f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400),
            repeatMode = androidx.compose.animation.core.RepeatMode.Reverse
        ),
        label = "breathe"
    )

    val orbitTransition = rememberInfiniteTransition(label = "orbit")
    val orbitAngle by orbitTransition.animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec = infiniteRepeatable(
            animation = tween(150000),
            repeatMode = androidx.compose.animation.core.RepeatMode.Restart
        ),
        label = "orbit"
    )

    val scale = 1f

    Box(modifier = Modifier.size(272.dp, 260.dp), contentAlignment = Alignment.Center) {
        // Orbiting bubble ring
        Box(modifier = Modifier.size(272.dp, 260.dp).rotate(orbitAngle)) {
            DecorativeBubble(92f, if (isDark) listOf(KikariaColors.CyanDark, KikariaColors.BubbleMintDark) else listOf(KikariaColors.Cyan, KikariaColors.BubbleMint), 0.48f, breatheScale, isDark, -96f, -68f)
            DecorativeBubble(80f, if (isDark) listOf(KikariaColors.BubbleLavenderDark, KikariaColors.MistDark) else listOf(KikariaColors.BubbleLavender, KikariaColors.Mist), 0.42f, 1f / breatheScale, isDark, 102f, -56f)
            DecorativeBubble(78f, if (isDark) listOf(KikariaColors.BubbleGreenDark, KikariaColors.CyanDark) else listOf(KikariaColors.BubbleGreen, KikariaColors.Cyan), 0.38f, breatheScale, isDark, 92f, 80f)
            DecorativeBubble(74f, if (isDark) listOf(KikariaColors.SkyDark, KikariaColors.BubbleWhiteDark) else listOf(KikariaColors.Sky, KikariaColors.BubbleWhite), 0.36f, 1f / breatheScale, isDark, -106f, 78f)
        }

        val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
        val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.28f) else KikariaColors.Sky.copy(alpha = 0.28f)

        // Center action circle with glass treatment
        Box(
            modifier = Modifier
                .size((190 * scale).dp)
                .scale(breatheScale)
                .shadow(28.dp, CircleShape, ambientColor = shadowC, spotColor = shadowC)
                .clip(CircleShape)
                .background(actionGrad)
                .liquidGlassCircleStroke(isDark, lineWidth = 1.1f)
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
                            listOf(Color.White.copy(alpha = 0.30f), Color.White.copy(alpha = 0.10f), Color.White.copy(alpha = 0.02f)),
                            center = Offset(57f, 57f),
                            radius = 150f
                        )
                    )
            )
            Text("→", color = Color.White.copy(alpha = 0.96f), fontSize = 70.sp, fontWeight = FontWeight.Normal, textAlign = TextAlign.Center)
        }
    }
}

@Composable
private fun DecorativeBubble(
    size: Float, colors: List<Color>, opacity: Float,
    breatheScale: Float, isDark: Boolean, offsetX: Float, offsetY: Float
) {
    val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.10f) else KikariaColors.Sky.copy(alpha = 0.10f)
    Box(
        modifier = Modifier
            .offset(x = offsetX.dp, y = offsetY.dp)
            .size(size.dp)
            .scale(breatheScale)
            .shadow(14.dp, CircleShape, ambientColor = shadowC, spotColor = shadowC)
            .clip(CircleShape)
            .background(Brush.linearGradient(colors.map { it.copy(alpha = opacity) }))
            .liquidGlassCircleStroke(isDark, lineWidth = 1f)
    ) {
        // Radial glass highlight matching iOS SoftBubble
        Box(
            modifier = Modifier
                .fillMaxSize()
                .clip(CircleShape)
                .background(
                    Brush.radialGradient(
                        listOf(Color.White.copy(alpha = 0.24f), Color.White.copy(alpha = 0.05f), Color.Transparent),
                        center = Offset(0.25f * size, 0.25f * size),
                        radius = size * 0.72f
                    )
                )
        )
    }
}

// ─── Today Progress Card ───

@Composable
private fun TodayProgressCard(
    dateText: String, daysLeftText: String, progressText: String,
    isDark: Boolean, onClick: () -> Unit
) {
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
            .liquidGlassStroke(shape, isDark)
            .clickable { onClick() }
            .padding(horizontal = 20.dp, vertical = 20.dp)
    ) {
        Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    dateText,
                    fontFamily = FontFamily.Serif,
                    fontSize = 23.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = deepText,
                    maxLines = 1
                )
                Text(
                    daysLeftText,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = softText,
                    maxLines = 1
                )
            }
            Text(
                progressText,
                fontFamily = FontFamily.Serif,
                fontSize = 25.sp,
                fontWeight = FontWeight.Bold,
                color = masteredGreen,
                maxLines = 1
            )
            Spacer(modifier = Modifier.width(8.dp))
            Text("›", fontSize = 20.sp, fontWeight = FontWeight.SemiBold, color = blueGray.copy(alpha = 0.52f))
        }
    }
}

// ─── Dashboard Card ───

@Composable
private fun DashboardCard(
    scopeCountText: String, reinforcedCount: Int, masteredCount: Int,
    presetName: String, isDark: Boolean,
    onOpenScope: () -> Unit, onOpenReinforcement: () -> Unit, onOpenMastered: () -> Unit
) {
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
            .liquidGlassStroke(shape, isDark)
    ) {
        Column {
            Row(modifier = Modifier.fillMaxWidth()) {
                DashboardMetricColumn("范围", scopeCountText, sky, softText, Modifier.weight(1f), onOpenScope)
                Box(Modifier.width(1.dp).height(42.dp).align(Alignment.CenterVertically).background(blueGray.copy(alpha = 0.16f)))
                DashboardMetricColumn("重点集锦", "$reinforcedCount", cyan, softText, Modifier.weight(1f), onOpenReinforcement)
                Box(Modifier.width(1.dp).height(42.dp).align(Alignment.CenterVertically).background(blueGray.copy(alpha = 0.16f)))
                DashboardMetricColumn("已掌握", "$masteredCount", masteredGreen, softText, Modifier.weight(1f), onOpenMastered)
            }
            Box(Modifier.fillMaxWidth().padding(horizontal = 18.dp).height(1.dp).background(blueGray.copy(alpha = 0.12f)))
            Row(
                Modifier
                    .fillMaxWidth()
                    .clickable { /* TODO: preset selection */ }
                    .padding(horizontal = 20.dp, vertical = 16.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    presetName,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = deepText,
                    maxLines = 1,
                    modifier = Modifier.weight(1f, fill = false)
                )
                Text(" 当前预设", fontSize = 12.sp, fontWeight = FontWeight.SemiBold, color = softText, maxLines = 1)
                Spacer(Modifier.weight(1f))
                Text("›", fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = blueGray.copy(alpha = 0.58f))
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
                value,
                fontFamily = FontFamily.Serif,
                fontSize = 24.sp,
                fontWeight = FontWeight.Bold,
                color = tint,
                maxLines = 1,
                textAlign = TextAlign.Center
            )
        }
    }
}

// ─── Date helpers ───

@Composable
private fun rememberDateTitle(): String {
    val calendar = Calendar.getInstance()
    val day = calendar.get(Calendar.DAY_OF_MONTH)
    val monthFormat = SimpleDateFormat("MMM", Locale.ENGLISH)
    val month = monthFormat.format(calendar.time)
    return "$month $day${ordinalSuffix(day)}"
}

@Composable
private fun rememberDaysLeftText(): String = "-- Days Left"

private fun ordinalSuffix(day: Int): String {
    val lastTwo = day % 100
    if (lastTwo == 11 || lastTwo == 12 || lastTwo == 13) return "th"
    return when (day % 10) { 1 -> "st"; 2 -> "nd"; 3 -> "rd"; else -> "th" }
}
