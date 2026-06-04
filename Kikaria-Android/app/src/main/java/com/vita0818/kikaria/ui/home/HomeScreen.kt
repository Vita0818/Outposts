package com.vita0818.kikaria.ui.home

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.runtime.getValue
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.defaultMinSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaMetricLabel
import com.vita0818.kikaria.ui.components.KikariaMetricValue
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaProfileAvatar
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaPhoneMetrics
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale

// ═══════════════════════════════════════════════════════════════════
//  HomeScreen — three layout modes matching iOS ContentView.swift
//  Compact iPhone, Pad Portrait, Landscape Two-Column.
// ═══════════════════════════════════════════════════════════════════

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
    val metrics = rememberKikariaPhoneMetrics()
    val isDark = isSystemInDarkTheme()
    val dateTitle = rememberDateTitle()
    val countdownDays = viewModel.countdownDays
    val daysLeftText = if (countdownDays > 0) "$countdownDays Days Left" else "-- Days Left"
    val progressText = "${viewModel.todayMasteredCount}/${viewModel.dailyGoal}"
    val scopeCountText = if (viewModel.selectedTags.isEmpty())
        "${viewModel.allTags.size}" else "${viewModel.selectedTags.size}"
    val reinforcedCount = viewModel.reinforcedPoints.size
    val masteredCount = viewModel.masteredPoints.size
    val presetName = viewModel.activePreset?.name ?: "无"

    KikariaPageShell {
        when {
            metrics.homeUsesTwoColumnLayout -> HomeLandscapeLayout(
                metrics = metrics, isDark = isDark,
                dateTitle = dateTitle, daysLeftText = daysLeftText,
                progressText = progressText, scopeCountText = scopeCountText,
                reinforcedCount = reinforcedCount, masteredCount = masteredCount,
                presetName = presetName, viewModel = viewModel,
                onStartReview = onStartReview, onOpenScope = onOpenScope,
                onOpenReinforcement = onOpenReinforcement, onOpenMastered = onOpenMastered,
                onOpenPresetSelection = onOpenPresetSelection,
                onOpenSettings = onOpenSettings, onOpenTodayOverview = onOpenTodayOverview,
            )
            metrics.isPadPortrait -> PadPortraitHomeLayout(
                metrics = metrics, isDark = isDark,
                dateTitle = dateTitle, daysLeftText = daysLeftText,
                progressText = progressText, scopeCountText = scopeCountText,
                reinforcedCount = reinforcedCount, masteredCount = masteredCount,
                presetName = presetName, viewModel = viewModel,
                onStartReview = onStartReview, onOpenScope = onOpenScope,
                onOpenReinforcement = onOpenReinforcement, onOpenMastered = onOpenMastered,
                onOpenPresetSelection = onOpenPresetSelection,
                onOpenSettings = onOpenSettings, onOpenTodayOverview = onOpenTodayOverview,
            )
            else -> CompactHomeLayout(
                metrics = metrics, isDark = isDark,
                dateTitle = dateTitle, daysLeftText = daysLeftText,
                progressText = progressText, scopeCountText = scopeCountText,
                reinforcedCount = reinforcedCount, masteredCount = masteredCount,
                presetName = presetName, viewModel = viewModel,
                onStartReview = onStartReview, onOpenScope = onOpenScope,
                onOpenReinforcement = onOpenReinforcement, onOpenMastered = onOpenMastered,
                onOpenPresetSelection = onOpenPresetSelection,
                onOpenSettings = onOpenSettings, onOpenTodayOverview = onOpenTodayOverview,
            )
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Compact iPhone Home — matches iOS contentNavigationStack compact branch
//  Apple ref: ContentView.swift lines 1586–1648
// ═══════════════════════════════════════════════════════════════════

@Composable
private fun CompactHomeLayout(
    metrics: KikariaPhoneMetrics, isDark: Boolean,
    dateTitle: String, daysLeftText: String, progressText: String,
    scopeCountText: String, reinforcedCount: Int, masteredCount: Int,
    presetName: String, viewModel: KikariaViewModel,
    onStartReview: () -> Unit, onOpenScope: () -> Unit,
    onOpenReinforcement: () -> Unit, onOpenMastered: () -> Unit,
    onOpenPresetSelection: () -> Unit, onOpenSettings: () -> Unit,
    onOpenTodayOverview: () -> Unit,
) {
    val homeScale = metrics.homeScale
    val headerScale = metrics.headerScale

    BoxWithConstraints(
        modifier = Modifier
            .fillMaxSize(),
        contentAlignment = Alignment.Center
    ) {
        Column(
            modifier = Modifier
                .padding(horizontal = metrics.horizontalPadding)
                .widthIn(max = metrics.homeMaxWidth)
                .defaultMinSize(minHeight = maxHeight)
                .verticalScroll(rememberScrollState())
                .fillMaxWidth(),
            verticalArrangement = Arrangement.Top,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Header: title + avatar — Apple: HStack(.top, 14)
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = metrics.titleTopPadding),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = KikariaTypography.mixedText(
                        "Kikaria", size = (39 * headerScale).toInt(), weight = FontWeight.SemiBold
                    ),
                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                )
                Box(modifier = Modifier.clickable { onOpenSettings() }) {
                    KikariaProfileAvatar(
                        size = (44 * headerScale).dp, displayName = viewModel.userDisplayName
                    )
                }
            }

            Spacer(modifier = Modifier.height(32.dp))

            // Start bubble — Apple: NavigationLink(.review) { StartReviewButton }
            KikariaStartBubble(onClick = onStartReview, homeScale = homeScale)

            Spacer(modifier = Modifier.height(30.dp))

            // Info cards — Apple: VStack(spacing: 12)
            HomeInfoCards(
                metrics = metrics, isDark = isDark,
                dateTitle = dateTitle, daysLeftText = daysLeftText,
                progressText = progressText, scopeCountText = scopeCountText,
                reinforcedCount = reinforcedCount, masteredCount = masteredCount,
                presetName = presetName,
                onOpenTodayOverview = onOpenTodayOverview,
                onOpenScope = onOpenScope, onOpenReinforcement = onOpenReinforcement,
                onOpenMastered = onOpenMastered, onOpenPresetSelection = onOpenPresetSelection,
            )

            Spacer(modifier = Modifier.height(12.dp))
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Pad Portrait Home — matches iOS padPortraitHomeContent
//  Apple ref: ContentView.swift lines 1303–1376
// ═══════════════════════════════════════════════════════════════════

@Composable
private fun PadPortraitHomeLayout(
    metrics: KikariaPhoneMetrics, isDark: Boolean,
    dateTitle: String, daysLeftText: String, progressText: String,
    scopeCountText: String, reinforcedCount: Int, masteredCount: Int,
    presetName: String, viewModel: KikariaViewModel,
    onStartReview: () -> Unit, onOpenScope: () -> Unit,
    onOpenReinforcement: () -> Unit, onOpenMastered: () -> Unit,
    onOpenPresetSelection: () -> Unit, onOpenSettings: () -> Unit,
    onOpenTodayOverview: () -> Unit,
) {
    val isLargePortrait = metrics.isLargePadPortrait
    val bubbleScale = minOf(metrics.homeScale, 1.32f)
    val topPadding = if (isLargePortrait) 58.dp else 48.dp
    val bubbleSafeSpacing = if (isLargePortrait) 36.dp else 30.dp
    val cardEdgeInset = metrics.homeCardEdgeInset
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(scrollState)
            .padding(top = topPadding)
            .padding(horizontal = metrics.horizontalPadding)
            .padding(bottom = cardEdgeInset),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Column(
            modifier = Modifier
                .widthIn(max = metrics.homeMaxWidth)
                .fillMaxWidth()
        ) {
            // Header row
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = KikariaTypography.mixedText(
                        "Kikaria",
                        size = (if (isLargePortrait) 58 else 54).toInt(),
                        weight = FontWeight.SemiBold
                    ),
                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                )
                Spacer(modifier = Modifier.width(24.dp))
                Box(modifier = Modifier.clickable { onOpenSettings() }) {
                    KikariaProfileAvatar(
                        size = (if (isLargePortrait) 66 else 62).dp,
                        displayName = viewModel.userDisplayName
                    )
                }
            }
        }

        Spacer(modifier = Modifier.height(bubbleSafeSpacing))

        // Bubble centered between header and Dashboard
        KikariaStartBubble(onClick = onStartReview, homeScale = bubbleScale)

        Spacer(modifier = Modifier.height(bubbleSafeSpacing))

        // Bottom cards
        Column(
            modifier = Modifier
                .widthIn(max = metrics.homeMaxWidth)
                .fillMaxWidth(),
            verticalArrangement = Arrangement.spacedBy(18.dp)
        ) {
            HomeInfoCards(
                metrics = metrics, isDark = isDark,
                dateTitle = dateTitle, daysLeftText = daysLeftText,
                progressText = progressText, scopeCountText = scopeCountText,
                reinforcedCount = reinforcedCount, masteredCount = masteredCount,
                presetName = presetName,
                onOpenTodayOverview = onOpenTodayOverview,
                onOpenScope = onOpenScope, onOpenReinforcement = onOpenReinforcement,
                onOpenMastered = onOpenMastered, onOpenPresetSelection = onOpenPresetSelection,
                padPortrait = true
            )
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Landscape Two-Column Home — matches iOS homeLandscapeContent
//  Apple ref: ContentView.swift lines 1378–1451
// ═══════════════════════════════════════════════════════════════════

@Composable
private fun HomeLandscapeLayout(
    metrics: KikariaPhoneMetrics, isDark: Boolean,
    dateTitle: String, daysLeftText: String, progressText: String,
    scopeCountText: String, reinforcedCount: Int, masteredCount: Int,
    presetName: String, viewModel: KikariaViewModel,
    onStartReview: () -> Unit, onOpenScope: () -> Unit,
    onOpenReinforcement: () -> Unit, onOpenMastered: () -> Unit,
    onOpenPresetSelection: () -> Unit, onOpenSettings: () -> Unit,
    onOpenTodayOverview: () -> Unit,
) {
    val leftWidth = metrics.homeLandscapeLeftWidth
    val rightWidth = metrics.homeLandscapeRightWidth
    val leftColumnHeight = metrics.homeLandscapeColumnHeight
    val cardScale = metrics.homeLandscapeCardScale

    Box(modifier = Modifier.fillMaxSize()) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState()),
            contentAlignment = Alignment.Center
        ) {
            Row(
                modifier = Modifier
                    .padding(horizontal = metrics.horizontalPadding)
                    .padding(vertical = 36.dp)
                    .widthIn(max = 1080.dp),
                horizontalArrangement = Arrangement.spacedBy(metrics.homeLandscapeColumnSpacing),
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Left column: title + bubble
                Column(
                    modifier = Modifier
                        .width(leftWidth)
                        .heightIn(min = leftColumnHeight),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(
                        text = KikariaTypography.mixedText(
                            "Kikaria",
                            size = (39 * metrics.headerScale).toInt(),
                            weight = FontWeight.SemiBold
                        ),
                        color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                        modifier = Modifier.fillMaxWidth()
                    )

                    Spacer(modifier = Modifier.height(34.dp))

                    KikariaStartBubble(
                        onClick = onStartReview,
                        homeScale = metrics.homeLandscapeBubbleScale
                    )

                    Spacer(modifier = Modifier.height(34.dp))
                }

                // Right column: progress card + dashboard card
                Column(
                    modifier = Modifier.width(rightWidth),
                    verticalArrangement = Arrangement.spacedBy((14 * cardScale).dp)
                ) {
                    // Progress card
                    KikariaGlassCard(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable { onOpenTodayOverview() },
                        cornerRadius = 28.dp,
                        fillOpacity = 0.58f
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = (24 * cardScale).dp, vertical = (24 * cardScale).dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = KikariaTypography.mixedText(
                                        dateTitle, size = (27 * cardScale).toInt(), weight = FontWeight.SemiBold
                                    ),
                                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                                    maxLines = 1
                                )
                                Spacer(modifier = Modifier.height((6 * cardScale).dp))
                                Text(
                                    text = daysLeftText,
                                    fontSize = (14 * cardScale).sp, fontWeight = FontWeight.SemiBold,
                                    color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                                    maxLines = 1
                                )
                            }
                            Text(
                                text = KikariaTypography.mixedText(
                                    progressText, size = (30 * cardScale).toInt(), weight = FontWeight.Bold
                                ),
                                color = if (isDark) KikariaColors.MasteredDeepGreenDark
                                    else KikariaColors.MasteredDeepGreen,
                                maxLines = 1
                            )
                            Spacer(modifier = Modifier.width((12 * cardScale).dp))
                            Icon(
                                imageVector = KikariaIcons.forward,
                                contentDescription = "查看详情",
                                modifier = Modifier.size((15 * cardScale).dp),
                                tint = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                                    .copy(alpha = 0.52f)
                            )
                        }
                    }

                    // Dashboard card — three metrics + preset row
                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 28.dp,
                        fillOpacity = 0.56f
                    ) {
                        Column {
                            Row(modifier = Modifier.fillMaxWidth()) {
                                LandscapeMetricColumn(
                                    title = "范围", value = scopeCountText,
                                    tint = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                                    modifier = Modifier.weight(1f),
                                    onClick = onOpenScope, cardScale = cardScale
                                )
                                DashboardDivider(scale = cardScale)
                                LandscapeMetricColumn(
                                    title = "重点集锦", value = "$reinforcedCount",
                                    tint = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan,
                                    modifier = Modifier.weight(1f),
                                    onClick = onOpenReinforcement, cardScale = cardScale
                                )
                                DashboardDivider(scale = cardScale)
                                LandscapeMetricColumn(
                                    title = "已掌握", value = "$masteredCount",
                                    tint = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
                                    modifier = Modifier.weight(1f),
                                    onClick = onOpenMastered, cardScale = cardScale
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
                                    .padding(horizontal = (20 * cardScale).dp, vertical = (16 * cardScale).dp)
                                    .heightIn(min = (56 * cardScale).dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    KikariaTypography.mixedText(
                                        presetName, size = (18 * cardScale).toInt(), weight = FontWeight.SemiBold
                                    ),
                                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                                    maxLines = 1, modifier = Modifier.weight(1f, fill = false)
                                )
                                Text(
                                    " 当前预设",
                                    fontSize = (14 * cardScale).sp, fontWeight = FontWeight.SemiBold,
                                    color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                                    maxLines = 1
                                )
                                Spacer(Modifier.weight(1f))
                                Icon(
                                    imageVector = KikariaIcons.forward,
                                    contentDescription = "切换预设",
                                    modifier = Modifier.size((12 * cardScale).dp),
                                    tint = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                                        .copy(alpha = 0.58f)
                                )
                            }
                        }
                    }
                }
            }
        }

        // Avatar overlay — fixed at top-right outside scroll, matches iOS
        Box(
            modifier = Modifier
                .align(Alignment.TopEnd)
                .padding(top = 26.dp, end = metrics.horizontalPadding)
                .clickable { onOpenSettings() }
        ) {
            KikariaProfileAvatar(
                size = 48.dp,
                displayName = viewModel.userDisplayName
            )
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Shared Start Bubble — orbits + center circle, matches iOS StartReviewButton
// ═══════════════════════════════════════════════════════════════════

/**
 * A decorative bubble with glassy translucent effect matching Apple's SoftBubble.
 *
 * Layers:
 * 1. Gradient fill (base colour)
 * 2. Radial highlight at top-left — white radial gradient simulating light reflection
 * 3. Gradient stroke — white→transparent→cyan, top-left to bottom-right
 * 4. Shadow
 */
@Composable
private fun GlassyBubble(
    sizeDp: androidx.compose.ui.unit.Dp,
    fillColors: List<Color>,
    shadowColor: Color,
    modifier: Modifier = Modifier,
    highlightFractionX: Float = 0.22f,
    highlightFractionY: Float = 0.22f,
) {
    val isDark = isSystemInDarkTheme()
    val strokeCyan = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan

    Box(
        modifier = modifier
            .size(sizeDp)
            .shadow(14.dp, CircleShape,
                ambientColor = shadowColor,
                spotColor = shadowColor)
            .clip(CircleShape)
            .background(Brush.linearGradient(fillColors))
            .drawWithContent {
                drawContent()
                val r = size.minDimension / 2f
                // Radial highlight — subdued to match reference flatter spheres
                drawCircle(
                    brush = Brush.radialGradient(
                        listOf(
                            Color.White.copy(alpha = 0.10f),
                            Color.White.copy(alpha = 0.03f),
                            Color.Transparent
                        ),
                        center = Offset(size.width * highlightFractionX, size.height * highlightFractionY),
                        radius = r
                    )
                )
                // Gradient stroke — subdued to match reference
                drawCircle(
                    brush = Brush.linearGradient(
                        listOf(
                            Color.White.copy(alpha = 0.28f),
                            Color.White.copy(alpha = 0.06f),
                            strokeCyan.copy(alpha = 0.12f)
                        ),
                        start = Offset.Zero,
                        end = Offset(size.width, size.height)
                    ),
                    radius = r - 0.5f * density,
                    style = Stroke(width = 1f * density)
                )
            }
    )
}

@Composable
private fun KikariaStartBubble(
    onClick: () -> Unit,
    homeScale: Float = 1f,
) {
    val isDark = isSystemInDarkTheme()

    val actionGrad = if (isDark) KikariaColors.ActionGradientDark
        else KikariaColors.ActionGradientLight
    val shadowC = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.28f)
        else KikariaColors.Sky.copy(alpha = 0.28f)

    val bubble1Color = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan
    val bubble1Color2 = if (isDark) KikariaColors.BubbleMintDark else KikariaColors.BubbleMint
    val bubble2Color = if (isDark) KikariaColors.BubbleLavenderDark else KikariaColors.BubbleLavender
    val bubble2Color2 = if (isDark) KikariaColors.MistDark else KikariaColors.Mist
    val bubble3Color = if (isDark) KikariaColors.BubbleGreenDark else KikariaColors.BubbleGreen
    val bubble3Color2 = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan
    val bubble4Color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val bubble4Color2 = if (isDark) KikariaColors.BubbleWhiteDark else KikariaColors.BubbleWhite

    val transition = rememberInfiniteTransition(label = "bubble")
    val orbitAngle by transition.animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec = infiniteRepeatable(
            animation = tween(150_000, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "orbitAngle"
    )
    val breathe by transition.animateFloat(
        initialValue = 1.012f, targetValue = 0.996f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ), label = "breathe"
    )
    val breatheB1 by transition.animateFloat(
        initialValue = 1.035f, targetValue = 0.985f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ), label = "breatheB1"
    )
    val breatheB2 by transition.animateFloat(
        initialValue = 0.985f, targetValue = 1.04f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ), label = "breatheB2"
    )
    val breatheB3 by transition.animateFloat(
        initialValue = 1.035f, targetValue = 0.985f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ), label = "breatheB3"
    )
    val breatheB4 by transition.animateFloat(
        initialValue = 0.985f, targetValue = 1.04f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ), label = "breatheB4"
    )
    val breatheY by transition.animateFloat(
        initialValue = 2f, targetValue = -5f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ), label = "breatheY"
    )

    val visualScale = maxOf(homeScale, 0.1f)
    val centerSize = (190 * visualScale).dp
    val orbitScale = visualScale

    Box(
        modifier = Modifier
            .size(width = (272 * visualScale).dp, height = (260 * visualScale).dp)
            .graphicsLayer { translationY = breatheY * orbitScale },
        contentAlignment = Alignment.Center
    ) {
        // Outer rotating ring
        Box(
            modifier = Modifier
                .fillMaxSize()
                .graphicsLayer { rotationZ = orbitAngle }
        ) {
            // Bubble 1: cyan+mint (top-left diagonal) — iOS diagonal layout
            GlassyBubble(
                sizeDp = (92 * orbitScale).dp,
                fillColors = listOf(bubble1Color.copy(alpha = 0.42f), bubble1Color2.copy(alpha = 0.42f)),
                shadowColor = bubble1Color.copy(alpha = 0.12f),
                highlightFractionX = 0.18f,
                highlightFractionY = 0.14f,
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(x = (-96 * orbitScale).dp, y = (-68 * orbitScale).dp)
                    .graphicsLayer { scaleX = breatheB1; scaleY = breatheB1; rotationZ = -orbitAngle }
            )
            // Bubble 2: lavender+mist (top-right diagonal)
            GlassyBubble(
                sizeDp = (80 * orbitScale).dp,
                fillColors = listOf(bubble2Color.copy(alpha = 0.32f), bubble2Color2.copy(alpha = 0.32f)),
                shadowColor = bubble2Color.copy(alpha = 0.10f),
                highlightFractionX = 0.12f,
                highlightFractionY = 0.24f,
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(x = (102 * orbitScale).dp, y = (-56 * orbitScale).dp)
                    .graphicsLayer { scaleX = breatheB2; scaleY = breatheB2; rotationZ = -orbitAngle }
            )
            // Bubble 3: green+cyan (bottom-right diagonal)
            GlassyBubble(
                sizeDp = (78 * orbitScale).dp,
                fillColors = listOf(bubble3Color.copy(alpha = 0.30f), bubble3Color2.copy(alpha = 0.30f)),
                shadowColor = bubble3Color.copy(alpha = 0.09f),
                highlightFractionX = 0.22f,
                highlightFractionY = 0.08f,
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(x = (92 * orbitScale).dp, y = (80 * orbitScale).dp)
                    .graphicsLayer { scaleX = breatheB3; scaleY = breatheB3; rotationZ = -orbitAngle }
            )
            // Bubble 4: sky+white (bottom-left diagonal)
            GlassyBubble(
                sizeDp = (74 * orbitScale).dp,
                fillColors = listOf(bubble4Color.copy(alpha = 0.34f), bubble4Color2.copy(alpha = 0.34f)),
                shadowColor = bubble4Color.copy(alpha = 0.10f),
                highlightFractionX = 0.32f,
                highlightFractionY = 0.18f,
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(x = (-106 * orbitScale).dp, y = (78 * orbitScale).dp)
                    .graphicsLayer { scaleX = breatheB4; scaleY = breatheB4; rotationZ = -orbitAngle }
            )
        }

        // Center circle — 190dp actionGradient + radial overlay + gradient stroke + arrow
        val centerStrokeCyan = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan
        Box(
            modifier = Modifier
                .size(centerSize)
                .graphicsLayer { scaleX = breathe; scaleY = breathe }
                .shadow(28.dp, CircleShape, ambientColor = shadowC, spotColor = shadowC)
                .clip(CircleShape).background(actionGrad)
                .drawWithContent {
                    drawContent()
                    val r = size.minDimension / 2f
                    // Gradient stroke — subdued to match reference blue center
                    drawCircle(
                        brush = Brush.linearGradient(
                            listOf(
                                Color.White.copy(alpha = 0.22f),
                                Color.White.copy(alpha = 0.05f),
                                centerStrokeCyan.copy(alpha = 0.10f)
                            ),
                            start = Offset.Zero,
                            end = Offset(size.width, size.height)
                        ),
                        radius = r - 0.4f * density,
                        style = Stroke(width = 0.8f * density)
                    )
                }
                .clickable { onClick() },
            contentAlignment = Alignment.Center
        ) {
            // Radial gradient overlay — white highlight from top-left
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
                            center = Offset(57f, 57f),
                            radius = 150f
                        )
                    )
            )
            Icon(
                imageVector = Icons.AutoMirrored.Filled.ArrowForward,
                contentDescription = "开始复习",
                modifier = Modifier
                    .size((70 * visualScale).dp)
                    .shadow(
                        elevation = 8.dp,
                        shape = RoundedCornerShape(4.dp),
                        ambientColor = Color.White.copy(alpha = 0.10f),
                        spotColor = Color.White.copy(alpha = 0.10f)
                    ),
                tint = Color.White.copy(alpha = 0.96f)
            )
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Shared Info Cards — Progress + Dashboard
// ═══════════════════════════════════════════════════════════════════

@Composable
private fun HomeInfoCards(
    metrics: KikariaPhoneMetrics, isDark: Boolean,
    dateTitle: String, daysLeftText: String, progressText: String,
    scopeCountText: String, reinforcedCount: Int, masteredCount: Int,
    presetName: String,
    onOpenTodayOverview: () -> Unit, onOpenScope: () -> Unit,
    onOpenReinforcement: () -> Unit, onOpenMastered: () -> Unit,
    onOpenPresetSelection: () -> Unit,
    padPortrait: Boolean = false,
) {
    val cardScale = if (padPortrait) metrics.cardScale else 1f
    val scale = maxOf(cardScale, 1f)
    val progressSpacing = (if (padPortrait) 18f else 14f) * scale
    val spacerMinLength = 12f * scale

    Column(
        modifier = Modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(if (padPortrait) (18 * scale).dp else 12.dp)
    ) {
            // Progress card — matches TodayOverviewHomeProgressButton
            KikariaGlassCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable { onOpenTodayOverview() },
                cornerRadius = if (padPortrait) (28 * scale).dp else 25.dp,
                fillOpacity = 0.58f
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(
                        horizontal = (if (padPortrait) 24 * scale else 20f).dp,
                        vertical = (if (padPortrait) 24 * scale else 20f).dp
                ),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(progressSpacing.dp)
            ) {
                Column {
                    Text(
                        text = KikariaTypography.mixedText(
                            dateTitle,
                            size = (if (padPortrait) 27 * scale else 23f).toInt(),
                            weight = FontWeight.SemiBold
                        ),
                        color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                        maxLines = 1
                    )
                    Spacer(modifier = Modifier.height((if (padPortrait) 6 * scale else 5 * scale).dp))
                    Text(
                        text = daysLeftText,
                        fontSize = (if (padPortrait) 14 * scale else 13f).sp,
                        fontWeight = FontWeight.SemiBold,
                        color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                        maxLines = 1
                    )
                }

                Spacer(
                    modifier = Modifier
                        .widthIn(min = spacerMinLength.dp)
                        .weight(1f)
                )

                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = KikariaTypography.mixedText(
                            progressText,
                            size = (if (padPortrait) 30 * scale else 25f).toInt(),
                            weight = FontWeight.Bold
                        ),
                        color = if (isDark) KikariaColors.MasteredDeepGreenDark
                            else KikariaColors.MasteredDeepGreen,
                        maxLines = 1
                    )
                    Spacer(modifier = Modifier.width((8 * scale).dp))
                    Icon(
                        imageVector = KikariaIcons.forward,
                        contentDescription = "查看详情",
                        modifier = Modifier.size((if (padPortrait) 15f * scale else 12f).dp),
                        tint = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                            .copy(alpha = 0.52f)
                    )
                }
            }
        }

        // Dashboard card — matches HomeDashboardGridCard
        KikariaGlassCard(
            modifier = Modifier.fillMaxWidth(),
            cornerRadius = if (padPortrait) (28 * scale).dp else 28.dp,
            fillOpacity = 0.56f
        ) {
            Column {
                Row(modifier = Modifier.fillMaxWidth()) {
                    DashboardMetricColumn(
                        title = "范围", value = scopeCountText,
                        tint = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                        modifier = Modifier.weight(1f), onClick = onOpenScope,
                        scale = scale,
                        isExpanded = padPortrait
                    )
                    DashboardDivider(scale = scale, isExpanded = padPortrait)
                    DashboardMetricColumn(
                        title = "重点集锦", value = "$reinforcedCount",
                        tint = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan,
                        modifier = Modifier.weight(1f), onClick = onOpenReinforcement,
                        scale = scale,
                        isExpanded = padPortrait
                    )
                    DashboardDivider(scale = scale, isExpanded = padPortrait)
                    DashboardMetricColumn(
                        title = "已掌握", value = "$masteredCount",
                        tint = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
                        modifier = Modifier.weight(1f), onClick = onOpenMastered,
                        scale = scale,
                        isExpanded = padPortrait
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
                            .padding(
                                horizontal = (if (padPortrait) 22 * scale else 20f).dp,
                                vertical = (if (padPortrait) 18 * scale else 16f).dp
                            )
                            .heightIn(min = if (padPortrait) (64 * scale).dp else (56 * scale).dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        KikariaTypography.mixedText(
                            presetName,
                            size = (if (padPortrait) 18 * scale else 16f).toInt(),
                            weight = FontWeight.SemiBold
                        ),
                        color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                        maxLines = 1,
                        modifier = Modifier.weight(1f, fill = false)
                    )
                    Text(
                        "当前预设",
                        fontSize = (if (padPortrait) 14 * scale else 12f).sp,
                        fontWeight = FontWeight.SemiBold,
                        color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                        maxLines = 1
                    )
                    Spacer(modifier = Modifier.width((8 * scale).dp))
                    Spacer(Modifier.weight(1f))
                    Icon(
                        imageVector = KikariaIcons.forward,
                        contentDescription = "切换预设",
                        modifier = Modifier.size((12 * scale).dp),
                        tint = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                            .copy(alpha = 0.58f)
                    )
                }
            }
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Dashboard helpers
// ═══════════════════════════════════════════════════════════════════

@Composable
private fun DashboardMetricColumn(
    title: String, value: String, tint: Color,
    modifier: Modifier = Modifier, onClick: () -> Unit,
    scale: Float = 1f,
    isExpanded: Boolean = false,
) {
    val minHeight = (if (isExpanded) 98f else 82f) * maxOf(scale, 1f)
    Box(
        modifier = modifier
            .clickable { onClick() }
            .padding(horizontal = (12 * scale).dp, vertical = (18 * scale).dp)
            .heightIn(min = minHeight.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            KikariaMetricLabel(title, fontSize = (13 * scale).toInt())
            Spacer(Modifier.height((8 * scale).dp))
            KikariaMetricValue(
                value = value, tint = tint,
                fontSize = (24 * scale).toInt()
            )
        }
    }
}

@Composable
private fun LandscapeMetricColumn(
    title: String, value: String, tint: Color,
    modifier: Modifier = Modifier, onClick: () -> Unit,
    cardScale: Float = 1f,
) {
    Box(
        modifier = modifier
            .clickable { onClick() }
            .padding(horizontal = (14 * cardScale).dp, vertical = (22 * cardScale).dp),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            KikariaMetricLabel(title, fontSize = (13 * cardScale).toInt())
            Spacer(Modifier.height((8 * cardScale).dp))
            KikariaMetricValue(
                value = value, tint = tint,
                fontSize = (24 * cardScale).toInt()
            )
        }
    }
}

@Composable
private fun DashboardDivider(
    modifier: Modifier = Modifier,
    scale: Float = 1f,
    isExpanded: Boolean = false
) {
    val isDark = isSystemInDarkTheme()
    val dividerHeight = (if (isExpanded) 50f else 42f) * maxOf(scale, 1f)
    Box(
        modifier
            .width(1.dp)
            .height(dividerHeight.dp)
            .background(
                (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                    .copy(alpha = 0.16f)
            )
    )
}

// ═══════════════════════════════════════════════════════════════════
//  Date helpers
// ═══════════════════════════════════════════════════════════════════

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
