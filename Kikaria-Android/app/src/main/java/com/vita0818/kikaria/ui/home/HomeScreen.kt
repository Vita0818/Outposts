package com.vita0818.kikaria.ui.home

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.runtime.withFrameMillis
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
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
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

    Box(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState()),
        contentAlignment = Alignment.Center
    ) {
        Column(
            modifier = Modifier
                .padding(horizontal = metrics.horizontalPadding)
                .widthIn(max = metrics.homeMaxWidth),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Header: title + avatar — Apple: HStack(.top, 14)
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 14.dp),
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

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
    ) {
        Column(
            modifier = Modifier
                .padding(top = topPadding)
                .padding(horizontal = metrics.horizontalPadding)
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

            // Expanding bubble section
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f, fill = true),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                KikariaStartBubble(
                    onClick = onStartReview,
                    homeScale = bubbleScale
                )
            }

            // Bottom cards
            Column(verticalArrangement = Arrangement.spacedBy(18.dp)) {
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
    val cardScale = 1.05f // Apple: homeLandscapeCardScale

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
            horizontalArrangement = Arrangement.spacedBy(56.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Left column: title + bubble
            Column(
                modifier = Modifier.width(leftWidth),
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
                    homeScale = 1.04f // Apple: homeLandscapeBubbleScale
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
                    fillOpacity = 0.42f
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
                        Text(
                            "›", fontSize = (15 * cardScale).sp, fontWeight = FontWeight.SemiBold,
                            color = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                                .copy(alpha = 0.52f)
                        )
                    }
                }

                // Dashboard card — three metrics + preset row
                KikariaGlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 28.dp,
                    fillOpacity = 0.40f
                ) {
                    Column {
                        Row(modifier = Modifier.fillMaxWidth()) {
                            LandscapeMetricColumn(
                                title = "范围", value = scopeCountText,
                                tint = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                                modifier = Modifier.weight(1f),
                                onClick = onOpenScope, cardScale = cardScale
                            )
                            DashboardDivider()
                            LandscapeMetricColumn(
                                title = "重点集锦", value = "$reinforcedCount",
                                tint = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan,
                                modifier = Modifier.weight(1f),
                                onClick = onOpenReinforcement, cardScale = cardScale
                            )
                            DashboardDivider()
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
                                .padding(horizontal = (20 * cardScale).dp, vertical = (16 * cardScale).dp),
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
                            Text(
                                "›", fontSize = 14.sp, fontWeight = FontWeight.SemiBold,
                                color = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                                    .copy(alpha = 0.58f)
                            )
                        }
                    }
                }
            }
        }

        // Avatar overlay — top right, matches iOS
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
    var orbitAngle by remember { mutableFloatStateOf(0f) }
    LaunchedEffect(Unit) {
        while (true) {
            withFrameMillis { frameMillis ->
                orbitAngle = ((frameMillis % 150_000L).toFloat() / 150_000f) * 360f
            }
        }
    }
    val breathe by transition.animateFloat(
        initialValue = 0.992f, targetValue = 1.018f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ), label = "breathe"
    )
    val breatheY by transition.animateFloat(
        initialValue = 2f, targetValue = -5f,
        animationSpec = infiniteRepeatable(
            animation = tween(5400, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ), label = "breatheY"
    )

    val visualScale = maxOf(homeScale, 0.1f)
    val containerSize = (272 * visualScale).dp
    val centerSize = (190 * visualScale).dp
    val arrowSize = (70 * visualScale).sp
    val orbitScale = visualScale

    Box(
        modifier = Modifier
            .size(containerSize)
            .graphicsLayer { translationY = breatheY * orbitScale },
        contentAlignment = Alignment.Center
    ) {
        // Outer rotating ring
        Box(
            modifier = Modifier
                .fillMaxSize()
                .graphicsLayer { rotationZ = orbitAngle }
        ) {
            // Bubble 1: cyan+mint, 92dp, top-left (-96,-68)
            Box(
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(x = (-96 * orbitScale).dp, y = (-68 * orbitScale).dp)
                    .size((92 * orbitScale).dp)
                    .graphicsLayer { scaleX = breathe; scaleY = breathe; rotationZ = -orbitAngle }
                    .shadow(14.dp, CircleShape,
                        ambientColor = bubble1Color.copy(alpha = 0.12f),
                        spotColor = bubble1Color.copy(alpha = 0.12f))
                    .clip(CircleShape)
                    .background(
                        Brush.linearGradient(
                            listOf(bubble1Color.copy(alpha = 0.48f), bubble1Color2.copy(alpha = 0.48f))
                        )
                    )
            )
            // Bubble 2: lavender+mist, 80dp, top-right (102,-56)
            Box(
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(x = (102 * orbitScale).dp, y = (-56 * orbitScale).dp)
                    .size((80 * orbitScale).dp)
                    .graphicsLayer { scaleX = 1f / breathe; scaleY = 1f / breathe; rotationZ = -orbitAngle }
                    .shadow(14.dp, CircleShape,
                        ambientColor = bubble2Color.copy(alpha = 0.11f),
                        spotColor = bubble2Color.copy(alpha = 0.11f))
                    .clip(CircleShape)
                    .background(
                        Brush.linearGradient(
                            listOf(bubble2Color.copy(alpha = 0.42f), bubble2Color2.copy(alpha = 0.42f))
                        )
                    )
            )
            // Bubble 3: green+cyan, 78dp, bottom-right (92,80)
            Box(
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(x = (92 * orbitScale).dp, y = (80 * orbitScale).dp)
                    .size((78 * orbitScale).dp)
                    .graphicsLayer { scaleX = breathe; scaleY = breathe; rotationZ = -orbitAngle }
                    .shadow(14.dp, CircleShape,
                        ambientColor = bubble3Color.copy(alpha = 0.10f),
                        spotColor = bubble3Color.copy(alpha = 0.10f))
                    .clip(CircleShape)
                    .background(
                        Brush.linearGradient(
                            listOf(bubble3Color.copy(alpha = 0.38f), bubble3Color2.copy(alpha = 0.38f))
                        )
                    )
            )
            // Bubble 4: sky+white, 74dp, bottom-left (-106,78)
            Box(
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(x = (-106 * orbitScale).dp, y = (78 * orbitScale).dp)
                    .size((74 * orbitScale).dp)
                    .graphicsLayer { scaleX = 1f / breathe; scaleY = 1f / breathe; rotationZ = -orbitAngle }
                    .shadow(14.dp, CircleShape,
                        ambientColor = bubble4Color.copy(alpha = 0.09f),
                        spotColor = bubble4Color.copy(alpha = 0.09f))
                    .clip(CircleShape)
                    .background(
                        Brush.linearGradient(
                            listOf(bubble4Color.copy(alpha = 0.36f), bubble4Color2.copy(alpha = 0.36f))
                        )
                    )
            )
        }

        // Center circle — 190dp actionGradient + radial overlay + arrow
        Box(
            modifier = Modifier
                .size(centerSize)
                .graphicsLayer { scaleX = breathe; scaleY = breathe }
                .shadow(28.dp, CircleShape, ambientColor = shadowC, spotColor = shadowC)
                .clip(CircleShape).background(actionGrad)
                .clickable { onClick() },
            contentAlignment = Alignment.Center
        ) {
            // Radial gradient overlay
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
            Text(
                KikariaIcons.TEXT_ARROW_RIGHT,
                color = Color.White.copy(alpha = 0.96f),
                fontSize = arrowSize, fontWeight = FontWeight.Normal,
                textAlign = TextAlign.Center
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
            fillOpacity = 0.42f
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(
                        horizontal = (if (padPortrait) 24 * scale else 20f).dp,
                        vertical = (if (padPortrait) 24 * scale else 20f).dp
                    ),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = KikariaTypography.mixedText(
                            dateTitle,
                            size = (if (padPortrait) 27 * scale else 23f).toInt(),
                            weight = FontWeight.SemiBold
                        ),
                        color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                        maxLines = 1
                    )
                    Spacer(modifier = Modifier.height((if (padPortrait) 6 * scale else 4f).dp))
                    Text(
                        text = daysLeftText,
                        fontSize = (if (padPortrait) 14 * scale else 13f).sp,
                        fontWeight = FontWeight.SemiBold,
                        color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                        maxLines = 1
                    )
                }
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
                Text(
                    "›",
                    fontSize = (if (padPortrait) 15 * scale else 12f).sp,
                    fontWeight = FontWeight.SemiBold,
                    color = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                        .copy(alpha = 0.52f)
                )
            }
        }

        // Dashboard card — matches HomeDashboardGridCard
        KikariaGlassCard(
            modifier = Modifier.fillMaxWidth(),
            cornerRadius = if (padPortrait) (28 * scale).dp else 28.dp,
            fillOpacity = 0.40f
        ) {
            Column {
                Row(modifier = Modifier.fillMaxWidth()) {
                    DashboardMetricColumn(
                        title = "范围", value = scopeCountText,
                        tint = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                        modifier = Modifier.weight(1f), onClick = onOpenScope,
                        scale = scale
                    )
                    DashboardDivider()
                    DashboardMetricColumn(
                        title = "重点集锦", value = "$reinforcedCount",
                        tint = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan,
                        modifier = Modifier.weight(1f), onClick = onOpenReinforcement,
                        scale = scale
                    )
                    DashboardDivider()
                    DashboardMetricColumn(
                        title = "已掌握", value = "$masteredCount",
                        tint = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
                        modifier = Modifier.weight(1f), onClick = onOpenMastered,
                        scale = scale
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
                        ),
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
                        " 当前预设",
                        fontSize = (if (padPortrait) 14 * scale else 12f).sp,
                        fontWeight = FontWeight.SemiBold,
                        color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                        maxLines = 1
                    )
                    Spacer(Modifier.weight(1f))
                    Text(
                        "›", fontSize = 14.sp, fontWeight = FontWeight.SemiBold,
                        color = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
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
) {
    Box(
        modifier = modifier
            .clickable { onClick() }
            .padding(horizontal = (12 * scale).dp, vertical = (18 * scale).dp),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            KikariaMetricLabel(title, fontSize = (13 * scale).toInt())
            Spacer(Modifier.height(8.dp))
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
            Spacer(Modifier.height(8.dp))
            KikariaMetricValue(
                value = value, tint = tint,
                fontSize = (24 * cardScale).toInt()
            )
        }
    }
}

@Composable
private fun DashboardDivider(modifier: Modifier = Modifier) {
    val isDark = isSystemInDarkTheme()
    Box(
        modifier
            .width(1.dp)
            .height(42.dp)
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
