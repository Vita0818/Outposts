package com.vita0818.kikaria.ui.review

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.slideInVertically
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.widthIn
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.animation.core.snap
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import kotlinx.coroutines.delay
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlin.math.abs
import kotlin.math.roundToInt
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaMathText
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaTagChip
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaDesign
import com.vita0818.kikaria.ui.theme.KikariaPhoneMetrics
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import com.vita0818.kikaria.viewmodel.ReviewMode

/**
 * ReviewScreen — iOS compact branch layout.
 *
 * iOS reference: ContentView.swift lines 8081–8153.
 *
 * Layout:
 * - contentRegion: scrollable VStack with minHeight = available, centered
 * - actionRegion: fixed at bottom with reviewActionBottomPadding
 * - back button: overlay at topLeading (not in content flow)
 * - horizontalPadding from metrics
 */

// ─── Tone system (matches iOS ReviewActionTone) ───

private enum class ActionTone { Blue, Green, Amber, Red }

private data class ToneColors(
    val primaryFill: Brush,
    val secondaryFill: Brush,
    val foreground: Color,
    val shadowColor: Color,
    val shadowOpacity: Float,
    val strokeAccent: Color,
    val strokeAccentOpacity: Float
)

private fun toneColors(tone: ActionTone, isDark: Boolean, isPrimary: Boolean): ToneColors {
    val glass = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    return when (tone) {
        ActionTone.Blue -> ToneColors(
            primaryFill = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight,
            secondaryFill = Brush.linearGradient(listOf(
                glass.copy(alpha = if (isDark) 0.36f else 0.46f),
                glass.copy(alpha = if (isDark) 0.36f else 0.46f)
            )),
            foreground = if (isPrimary) Color.White
                else if (isDark) KikariaColors.DeepTextDark.copy(alpha = 0.92f) else KikariaColors.DeepText,
            shadowColor = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
            shadowOpacity = if (isPrimary) 0.22f else 0.10f,
            strokeAccent = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
            strokeAccentOpacity = 0.18f
        )
        ActionTone.Green -> ToneColors(
            primaryFill = if (isDark) KikariaColors.MasteredGradientDark else KikariaColors.MasteredGradientLight,
            secondaryFill = Brush.linearGradient(listOf(
                glass.copy(alpha = if (isDark) 0.36f else 0.46f),
                glass.copy(alpha = if (isDark) 0.36f else 0.46f)
            )),
            foreground = if (isPrimary) Color.White
                else if (isDark) KikariaColors.DeepTextDark.copy(alpha = 0.92f) else KikariaColors.DeepText,
            shadowColor = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
            shadowOpacity = if (isPrimary) 0.22f else 0.10f,
            strokeAccent = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
            strokeAccentOpacity = 0.18f
        )
        ActionTone.Amber -> ToneColors(
            primaryFill = if (isDark) KikariaColors.NextGradientDark else KikariaColors.NextGradientLight,
            secondaryFill = Brush.linearGradient(listOf(
                (if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber).copy(
                    alpha = if (isDark) 0.72f else 0.68f),
                Color(0xFF9487CC).copy(alpha = if (isDark) 0.68f else 0.56f)
            )),
            foreground = Color.White.copy(alpha = 0.94f),
            shadowColor = if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber,
            shadowOpacity = if (isPrimary) 0.12f else 0.055f,
            strokeAccent = if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber,
            strokeAccentOpacity = 0.16f
        )
        ActionTone.Red -> ToneColors(
            primaryFill = if (isDark) KikariaColors.RemoveGradientDark else KikariaColors.RemoveGradientLight,
            secondaryFill = Brush.linearGradient(listOf(
                KikariaColors.RemoveCoral.copy(alpha = if (isDark) 0.70f else 0.58f),
                Color(0xFFFA9480).copy(alpha = if (isDark) 0.56f else 0.46f)
            )),
            foreground = Color.White,
            shadowColor = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral,
            shadowOpacity = if (isPrimary) 0.22f else 0.10f,
            strokeAccent = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral,
            strokeAccentOpacity = 0.18f
        )
    }
}

// ─── ReviewActionButton ───

@Composable
private fun ReviewActionButton(
    text: String,
    tone: ActionTone = ActionTone.Blue,
    isPrimary: Boolean = true,
    verticalContent: Boolean = false,
    icon: String? = null,
    buttonScale: Float = 1f,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val colors = toneColors(tone, isDark, isPrimary)
    val scale = maxOf(buttonScale, 1f)
    val cornerRadius = 26.dp
    val shape = RoundedCornerShape(cornerRadius)

    Box(
        modifier = modifier
            .fillMaxWidth()
            .shadow(16.dp, shape,
                ambientColor = colors.shadowColor.copy(alpha = colors.shadowOpacity),
                spotColor = colors.shadowColor.copy(alpha = colors.shadowOpacity))
            .clip(shape)
            .background(if (isPrimary) colors.primaryFill else colors.secondaryFill)
            .clickable { onClick() }
            .padding(vertical = ((if (verticalContent && icon != null) 16f else 19f) * scale).dp),
        contentAlignment = Alignment.Center
    ) {
        if (verticalContent && icon != null) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(icon, fontSize = (22 * scale).sp, fontWeight = FontWeight.Normal, color = colors.foreground)
                Spacer(Modifier.height((6 * scale).dp))
                Text(text, fontSize = (15 * scale).sp, fontWeight = FontWeight.SemiBold, color = colors.foreground)
            }
        } else if (icon != null) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.Center
            ) {
                Text(icon, fontSize = (18 * scale).sp, color = colors.foreground)
                Spacer(Modifier.width((8 * scale).dp))
                Text(text, fontSize = (17 * scale).sp, fontWeight = FontWeight.SemiBold, color = colors.foreground)
            }
        } else {
            Text(text, fontSize = (17 * scale).sp, fontWeight = FontWeight.SemiBold, color = colors.foreground)
        }
    }
}

// ─── Main ReviewScreen ───

@Composable
fun ReviewScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit
) {
    val metrics = rememberKikariaPhoneMetrics()
    val point = viewModel.currentPoint
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val tertiaryText = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText

    if (point == null) {
        KikariaPageShell {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                if (viewModel.isReviewCompleted) {
                    ReinforcementCompletionView(
                        metrics = metrics,
                        onReturnHome = onBack
                    )
                } else {
                    Text(
                        text = "没有可复习的知识点",
                        color = tertiaryText,
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Medium
                    )
                }
                KikariaBackButton(
                    onClick = onBack,
                    metrics = metrics,
                    modifier = Modifier.align(Alignment.TopStart)
                )
            }
        }
        return
    }

    // ── iOS-aligned bi-directional swipe gesture system ──
    // iOS reference: ContentView.swift reviewDragGesture() + handleFullScreenDragGesture()
    // Thresholds match iOS: horizontal 80px, vertical 90px (reveal) / 160px (next-after-answer)
    // Dominance factor 1.4× matches iOS dominantDragAxis

    // ── State variables (must be declared before local functions that reference them) ──
    var swipeTargetX by remember { mutableFloatStateOf(0f) }
    var swipeTargetY by remember { mutableFloatStateOf(0f) }
    var isSwipeSettling by remember { mutableStateOf(false) }
    val swipeDisplayX by animateFloatAsState(
        targetValue = swipeTargetX,
        animationSpec = if (isSwipeSettling) spring(dampingRatio = 0.55f, stiffness = 400f) else snap(),
        label = "swipeX"
    )
    val swipeDisplayY by animateFloatAsState(
        targetValue = swipeTargetY,
        animationSpec = if (isSwipeSettling) spring(dampingRatio = 0.55f, stiffness = 400f) else snap(),
        label = "swipeY"
    )
    var gestureFlash by remember { mutableFloatStateOf(0f) }
    val flashAlpha by animateFloatAsState(
        targetValue = gestureFlash,
        animationSpec = tween(durationMillis = 140),
        label = "flash"
    )
    var showScopePanel by remember { mutableStateOf(false) }
    val scopePanelAlpha by animateFloatAsState(
        targetValue = if (showScopePanel) 1f else 0f,
        animationSpec = tween(300),
        label = "scopePanel"
    )

    // ── Local gesture handler functions (must be defined before pointerInput block) ──

    fun triggerGestureFlash() {
        gestureFlash = 1f
    }

    fun handleSwipeRight(vm: KikariaViewModel) {
        // iOS reference: right swipe from left edge in normal mode opens scope panel;
        // otherwise progressive reveal: hint → content → reinforcement.
        if (vm.reviewMode == ReviewMode.NORMAL && !vm.isHintShown && !vm.isContentShown) {
            showScopePanel = true
        } else {
            when {
                vm.isContentShown -> vm.toggleReinforcement()
                vm.isHintShown -> vm.showContent()
                vm.currentPoint != null -> vm.showHint()
            }
        }
    }

    fun handleSwipeLeft(vm: KikariaViewModel) {
        // iOS reference: handleNormalSwipeLeft / handleReinforcementSwipeLeft / handleMasteredSwipeLeft
        when (vm.reviewMode) {
            ReviewMode.NORMAL -> {
                // Reveal content + add to reinforcement (matches iOS)
                vm.showContent()
                vm.toggleReinforcement()
            }
            ReviewMode.REINFORCEMENT -> {
                // Remove from reinforcement + next (matches iOS)
                vm.toggleReinforcement()
                vm.nextPoint()
            }
            ReviewMode.MASTERED -> {
                // Remove from mastered + next (matches iOS)
                vm.toggleMastered()
                vm.nextPoint()
            }
        }
    }

    fun handleSwipeUp(vm: KikariaViewModel) {
        // iOS reference: up = reveal content (if hidden) OR next point (if content shown)
        if (vm.isContentShown) {
            if (vm.hasNextPoint) vm.nextPoint()
        } else {
            vm.showContent()
        }
    }

    fun handleSwipeDown(vm: KikariaViewModel) {
        // iOS reference: down = previous point (unless in card area with visible content)
        if (vm.isContentShown) {
            vm.previousPoint()
        } else if (vm.hasPreviousPoint) {
            vm.previousPoint()
        }
    }

    // ── Gesture feedback flash & settle reset ──
    LaunchedEffect(gestureFlash) {
        if (gestureFlash > 0f) {
            delay(140)
            gestureFlash = 0f
        }
    }

    // Reset isSwipeSettling after spring animation completes (~500ms)
    LaunchedEffect(isSwipeSettling) {
        if (isSwipeSettling) {
            delay(500)
            isSwipeSettling = false
        }
    }

    // ── Swipe gesture for the action bar area ──
    val swipeModifier = Modifier.pointerInput(viewModel.isContentShown, viewModel.isHintShown, viewModel.reviewMode) {
        val horizontalThreshold = 80f
        val verticalThreshold = if (viewModel.isContentShown) 160f else 90f

        detectDragGestures(
            onDrag = { _, dragAmount ->
                isSwipeSettling = false
                swipeTargetX += dragAmount.x
                swipeTargetY += dragAmount.y
            },
            onDragEnd = {
                val dx = swipeTargetX
                val dy = swipeTargetY
                val horizontal = abs(dx)
                val vertical = abs(dy)

                if (horizontal > horizontalThreshold && horizontal > vertical) {
                    triggerGestureFlash()
                    if (dx > 0) handleSwipeRight(viewModel)
                    else handleSwipeLeft(viewModel)
                } else if (vertical > verticalThreshold && vertical > horizontal) {
                    triggerGestureFlash()
                    if (dy < 0) handleSwipeUp(viewModel)
                    else handleSwipeDown(viewModel)
                }
                swipeTargetX = 0f
                swipeTargetY = 0f
                isSwipeSettling = true
            },
            onDragCancel = {
                swipeTargetX = 0f
                swipeTargetY = 0f
                isSwipeSettling = true
            }
        )
    }

    KikariaPageShell {
        Box(modifier = Modifier.fillMaxSize()) {
            // ── Gesture feedback flash overlay ──
            if (flashAlpha > 0.01f) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(
                            (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
                                .copy(alpha = flashAlpha * 0.18f)
                        )
                        .then(
                            Modifier.pointerInput(Unit) {} // consume touches during flash
                        )
                )
            }

            Column(modifier = Modifier.fillMaxSize().offset { IntOffset(swipeDisplayX.roundToInt(), swipeDisplayY.roundToInt()) }) {
                // Top spacer for back button clearance (matches metrics.backButtonTopPadding)
                Spacer(Modifier.height(metrics.backButtonTopPadding + 16.dp))

                if (metrics.reviewUsesTwoColumnLayout) {
                    // ── Tablet two-column layout (iOS reviewLandscapeContent lines 8056-8078) ──
                    Row(
                        modifier = Modifier
                            .weight(1f)
                            .padding(horizontal = metrics.horizontalPadding)
                            .padding(vertical = 28.dp),
                        horizontalArrangement = Arrangement.spacedBy(48.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        // Left column: scrollable reading content (iOS: reviewLandscapeReadingColumn)
                        Column(
                            modifier = Modifier
                                .weight(1f)
                                .fillMaxHeight()
                                .verticalScroll(rememberScrollState())
                        ) {
                            ReviewContentCards(
                                point = point, viewModel = viewModel,
                                deepText = deepText
                            )
                            Spacer(Modifier.height(24.dp))
                        }

                        // Right column: action panel (iOS: reviewLandscapeActionPanel)
                        // Fixed min width for action column matching iOS 340-380dp range
                        Column(
                            modifier = Modifier
                                .widthIn(min = 340.dp)
                                .weight(0.56f, fill = false)
                                .padding(bottom = metrics.reviewActionBottomPadding),
                            verticalArrangement = Arrangement.spacedBy(12.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            if (!viewModel.isContentShown) {
                                if (!viewModel.isHintShown) {
                                    // Apple: both hint + answer buttons visible simultaneously
                                    ReviewActionButton(
                                        text = "查看提示",
                                        icon = "✦",
                                        tone = ActionTone.Blue,
                                        isPrimary = false,
                                        buttonScale = metrics.reviewButtonScale,
                                        onClick = { viewModel.showHint() }
                                    )
                                    ReviewActionButton(
                                        text = "查看答案",
                                        icon = "▣",
                                        tone = ActionTone.Blue,
                                        isPrimary = true,
                                        buttonScale = metrics.reviewButtonScale,
                                        onClick = { viewModel.showContent() }
                                    )
                                } else {
                                    ReviewActionButton(
                                        text = "查看答案",
                                        icon = "▣",
                                        tone = ActionTone.Blue,
                                        isPrimary = true,
                                        buttonScale = metrics.reviewButtonScale,
                                        onClick = { viewModel.showContent() }
                                    )
                                }
                            }

                            if (viewModel.isContentShown) {
                                TabletReviewActions(
                                    viewModel = viewModel,
                                    buttonScale = metrics.reviewButtonScale
                                )
                            }
                        }
                    }
                } else {
                    // ── Phone single-column layout ──
                    Column(
                        modifier = Modifier
                            .weight(1f)
                            .verticalScroll(rememberScrollState())
                            .padding(horizontal = metrics.horizontalPadding)
                    ) {
                        ReviewContentCards(
                            point = point, viewModel = viewModel,
                            deepText = deepText
                        )
                        Spacer(Modifier.height(16.dp))
                    }

                    // Fixed-height bottom action region with swipe gestures
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(horizontal = metrics.horizontalPadding)
                            .padding(bottom = metrics.reviewActionBottomPadding)
                            .heightIn(min = 138.dp, max = 220.dp)
                            .then(swipeModifier)
                    ) {
                        if (!viewModel.isContentShown) {
                            if (!viewModel.isHintShown) {
                                // Apple: both hint + answer buttons visible simultaneously
                                Column(
                                    modifier = Modifier.fillMaxWidth(),
                                    verticalArrangement = Arrangement.spacedBy(10.dp)
                                ) {
                                    ReviewActionButton(
                                        text = "查看提示",
                                        icon = "✦",
                                        tone = ActionTone.Blue,
                                        isPrimary = false,
                                        buttonScale = metrics.reviewButtonScale,
                                        onClick = { viewModel.showHint() }
                                    )
                                    ReviewActionButton(
                                        text = "查看答案",
                                        icon = "▣",
                                        tone = ActionTone.Blue,
                                        isPrimary = true,
                                        buttonScale = metrics.reviewButtonScale,
                                        onClick = { viewModel.showContent() }
                                    )
                                }
                            } else {
                                ReviewActionButton(
                                    text = "查看答案",
                                    icon = "▣",
                                    tone = ActionTone.Blue,
                                    isPrimary = true,
                                    buttonScale = metrics.reviewButtonScale,
                                    onClick = { viewModel.showContent() },
                                    modifier = Modifier.padding(vertical = 8.dp)
                                )
                            }
                        }

                        if (viewModel.isContentShown) {
                            ReviewBottomActionBar(
                                viewModel = viewModel,
                                buttonScale = metrics.reviewButtonScale,
                                modifier = Modifier.padding(vertical = 12.dp)
                            )
                        }
                    }
                }
            }

            // Overlay back button — iOS pattern: above content, topLeading
            KikariaBackButton(
                onClick = onBack,
                metrics = metrics,
                modifier = Modifier.align(Alignment.TopStart)
            )

            // ── Scope panel overlay (iOS: in-review scope selection, slides from left) ──
            if (showScopePanel || scopePanelAlpha > 0.01f) {
                // Semi-transparent backdrop
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(Color.Black.copy(alpha = scopePanelAlpha * 0.35f))
                        .clickable(
                            enabled = scopePanelAlpha > 0.5f,
                            indication = null,
                            interactionSource = remember { androidx.compose.foundation.interaction.MutableInteractionSource() }
                        ) { showScopePanel = false }
                )

                // Scope panel card — slides in from left
                Box(
                    modifier = Modifier
                        .align(Alignment.CenterStart)
                        .fillMaxHeight()
                        .widthIn(max = 340.dp)
                        .fillMaxWidth(0.72f)
                        .background(
                            if (isDark) KikariaColors.PageGradientDark
                            else KikariaColors.PageGradientLight
                        )
                        .clickable(enabled = false) { /* consume clicks */ }
                ) {
                    Column(
                        modifier = Modifier
                            .padding(start = metrics.horizontalPadding, top = metrics.backButtonTopPadding + 48.dp, end = metrics.horizontalPadding, bottom = metrics.bottomSafePadding + 32.dp)
                    ) {
                        // Close button row
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = KikariaTypography.mixedText("选择范围", size = 20, weight = FontWeight.Bold),
                                color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                            )
                            KikariaBackButton(
                                onClick = { showScopePanel = false },
                                metrics = metrics
                            )
                        }

                        Spacer(Modifier.height(16.dp))

                        // Search bar — filter tags by name
                        var scopeSearchText by remember { mutableStateOf("") }
                        val searchShape = RoundedCornerShape(12.dp)
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .shadow(6.dp, searchShape,
                                    ambientColor = Color.Black.copy(alpha = 0.04f),
                                    spotColor = Color.Black.copy(alpha = 0.04f))
                                .clip(searchShape)
                                .background(glassSurface.copy(alpha = 0.48f))
                                .padding(horizontal = 12.dp, vertical = 10.dp)
                        ) {
                            androidx.compose.foundation.text.BasicTextField(
                                value = scopeSearchText,
                                onValueChange = { scopeSearchText = it },
                                textStyle = androidx.compose.ui.text.TextStyle(
                                    fontSize = 15.sp,
                                    fontWeight = FontWeight.Medium,
                                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                                ),
                                modifier = Modifier.fillMaxWidth(),
                                decorationBox = { innerTextField ->
                                    Box {
                                        if (scopeSearchText.isEmpty()) {
                                            Text(
                                                "搜索标签",
                                                fontSize = 15.sp,
                                                fontWeight = FontWeight.Medium,
                                                color = (if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText).copy(alpha = 0.6f)
                                            )
                                        }
                                        innerTextField()
                                    }
                                }
                            )
                        }

                        Spacer(Modifier.height(12.dp))

                        // Tag chips for scope selection
                        val filteredTags = if (scopeSearchText.isBlank()) viewModel.allTags
                            else viewModel.allTags.filter { scopeSearchText.trim() in it }

                        if (filteredTags.isEmpty()) {
                            Text(
                                if (scopeSearchText.isNotBlank()) "无匹配标签" else "暂无标签",
                                color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText,
                                fontSize = 15.sp
                            )
                        } else {
                            LazyColumn(
                                modifier = Modifier.weight(1f),
                                verticalArrangement = Arrangement.spacedBy(10.dp)
                            ) {
                                items(filteredTags.chunked(2)) { rowTags ->
                                    Row(
                                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                                    ) {
                                        rowTags.forEach { tag ->
                                            val isSelected = tag in viewModel.selectedTags
                                            Box(
                                                modifier = Modifier
                                                    .clip(RoundedCornerShape(KikariaDesign.PillRadius))
                                                    .background(
                                                        if (isSelected) {
                                                            if (isDark) KikariaColors.SkyDark.copy(alpha = 0.32f)
                                                            else KikariaColors.Sky.copy(alpha = 0.28f)
                                                        } else glassSurface.copy(alpha = 0.42f)
                                                    )
                                                    .clickable {
                                                        if (isSelected) {
                                                            viewModel.selectedTags.remove(tag)
                                                        } else {
                                                            viewModel.selectedTags.add(tag)
                                                        }
                                                    }
                                                    .padding(horizontal = 12.dp, vertical = 8.dp)
                                            ) {
                                                Text(
                                                    tag,
                                                    fontSize = 14.sp,
                                                    fontWeight = FontWeight.Medium,
                                                    color = if (isDark) KikariaColors.DeepTextDark
                                                    else KikariaColors.DeepText
                                                )
                                            }
                                        }
                                    }
                                }

                            }

                            Spacer(Modifier.height(8.dp))

                            // Clear all
                            if (viewModel.selectedTags.isNotEmpty()) {
                                Text(
                                    "清除所有",
                                    fontSize = 13.sp,
                                    fontWeight = FontWeight.SemiBold,
                                    color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                                    modifier = Modifier
                                        .clickable { viewModel.selectedTags.clear() }
                                        .padding(vertical = 6.dp)
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

// ─── Shared content cards (used by both phone and tablet layouts) ───

@Composable
private fun ReviewContentCards(
    point: com.vita0818.kikaria.data.KnowledgePoint,
    viewModel: KikariaViewModel,
    deepText: Color
) {
    val isDark = isSystemInDarkTheme()
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val pillShape = RoundedCornerShape(KikariaDesign.PillRadius)

    // Title group — matches Apple titleGroup: title + tags + review count pill
    // NOT wrapped in a card; clean text on page background
    Column(
        modifier = Modifier.fillMaxWidth(),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = KikariaTypography.mixedText(
                point.title, size = 36, weight = FontWeight.SemiBold
            ),
            color = deepText,
            textAlign = TextAlign.Center,
            lineHeight = 44.sp,
            modifier = Modifier.padding(horizontal = 22.dp)
        )

        // Chapter/context chip — Apple reference shows a single semantic chip
        // (e.g. "5.4 Recursive Algorithms"), not multiple category tags.
        val chapterTag = point.tags.firstOrNull()
        if (chapterTag != null) {
            Spacer(Modifier.height(14.dp))
            KikariaTagChip(tag = chapterTag)
        }

        Spacer(Modifier.height(14.dp))

        // Review count pill — matches Apple TodayReviewCountPill
        Box(
            modifier = Modifier
                .clip(pillShape)
                .shadow(12.dp, pillShape,
                    ambientColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.10f),
                    spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.10f))
                .background(glassSurface.copy(alpha = if (isDark) 0.38f else 0.42f))
                .padding(horizontal = 18.dp, vertical = 8.dp)
        ) {
            Text(
                text = KikariaTypography.mixedText(
                    "该知识点今日复习 ${viewModel.todayReviewCount} 次",
                    size = 12,
                    weight = FontWeight.SemiBold
                ),
                color = deepText.copy(alpha = 0.78f)
            )
        }
    }

    Spacer(Modifier.height(14.dp))

    // Hint card
    AnimatedVisibility(
        visible = viewModel.isHintShown,
        enter = fadeIn() + slideInVertically { it / 2 }
    ) {
        KikariaGlassCard(
            modifier = Modifier.fillMaxWidth(),
            cornerRadius = 26.dp,
            fillOpacity = 0.56f
        ) {
            Column(modifier = Modifier.padding(18.dp)) {
                Text(
                    text = "提示",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Bold,
                    color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                )
                Spacer(Modifier.height(10.dp))
                KikariaMathText(
                    text = point.hint,
                    fontSize = 17,
                    fontWeight = FontWeight.Normal
                )
            }
        }
    }

    if (viewModel.isHintShown) {
        Spacer(Modifier.height(10.dp))
    }

    // Content card
    AnimatedVisibility(
        visible = viewModel.isContentShown,
        enter = fadeIn() + slideInVertically { it / 2 }
    ) {
        KikariaGlassCard(
            modifier = Modifier.fillMaxWidth(),
            cornerRadius = 26.dp,
            fillOpacity = 0.56f
        ) {
            Column(modifier = Modifier.padding(18.dp)) {
                Text(
                    text = "答案",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Bold,
                    color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                )
                Spacer(Modifier.height(10.dp))
                KikariaMathText(
                    text = point.content,
                    fontSize = 17,
                    fontWeight = FontWeight.Normal
                )
            }
        }
    }
}

// ─── Tablet review actions (vertical stack without row constraint) ───

@Composable
private fun TabletReviewActions(
    viewModel: KikariaViewModel,
    buttonScale: Float = 1f
) {
    val point = viewModel.currentPoint

    when (viewModel.reviewMode) {
        ReviewMode.NORMAL -> {
            ReviewActionButton(
                text = if (point?.isReinforced == true)
                    "再次加入 ×${point.reinforcementCount}" else "加入重点集锦",
                icon = "+",
                tone = ActionTone.Blue,
                isPrimary = true,
                buttonScale = buttonScale,
                onClick = { viewModel.toggleReinforcement() }
            )
            ReviewActionButton(
                text = if (point?.isMastered == true)
                    "已设定为掌握" else "加入已掌握",
                icon = if (point?.isMastered == true) null else "✓",
                tone = ActionTone.Green,
                isPrimary = point?.isMastered != true,
                buttonScale = buttonScale,
                onClick = { viewModel.toggleMastered() }
            )
        }
        ReviewMode.REINFORCEMENT -> {
            ReviewActionButton(
                text = "移出重点集锦",
                icon = "−",
                tone = ActionTone.Red,
                isPrimary = true,
                buttonScale = buttonScale,
                onClick = { viewModel.toggleReinforcement() }
            )
            ReviewActionButton(
                text = if (point?.isMastered == true)
                    "已设定为掌握" else "加入已掌握",
                icon = if (point?.isMastered == true) null else "✓",
                tone = ActionTone.Green,
                isPrimary = point?.isMastered != true,
                buttonScale = buttonScale,
                onClick = { viewModel.toggleMastered() }
            )
        }
        ReviewMode.MASTERED -> {
            ReviewActionButton(
                text = if (point?.isReinforced == true)
                    "再次加入 ×${point.reinforcementCount}" else "加入重点集锦",
                icon = "+",
                tone = ActionTone.Blue,
                isPrimary = true,
                buttonScale = buttonScale,
                onClick = { viewModel.toggleReinforcement() }
            )
            ReviewActionButton(
                text = "移出已掌握",
                icon = "−",
                tone = ActionTone.Red,
                isPrimary = true,
                buttonScale = buttonScale,
                onClick = { viewModel.toggleMastered() }
            )
        }
    }

    Spacer(Modifier.height(8.dp))

    ReviewActionButton(
        text = "下一个",
        icon = "↬",
        tone = ActionTone.Amber,
        isPrimary = false,
        buttonScale = buttonScale,
        onClick = { viewModel.nextPoint() },
        modifier = Modifier.fillMaxWidth()
    )
}

// ─── ReviewBottomActionBar ───

@Composable
private fun ReviewBottomActionBar(
    viewModel: KikariaViewModel,
    buttonScale: Float = 1f,
    modifier: Modifier = Modifier
) {
    val point = viewModel.currentPoint

    Column(modifier = modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                when (viewModel.reviewMode) {
                    ReviewMode.NORMAL -> {
                        ReviewActionButton(
                            text = if (point?.isReinforced == true)
                                "再次加入 ×${point.reinforcementCount}" else "加入重点集锦",
                            icon = "+",
                            tone = ActionTone.Blue,
                            isPrimary = true,
                            buttonScale = buttonScale,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = if (point?.isMastered == true)
                                "已设定为掌握" else "加入已掌握",
                            icon = if (point?.isMastered == true) null else "✓",
                            tone = ActionTone.Green,
                            isPrimary = point?.isMastered != true,
                            buttonScale = buttonScale,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.REINFORCEMENT -> {
                        ReviewActionButton(
                            text = "移出重点集锦",
                            icon = "−",
                            tone = ActionTone.Red,
                            isPrimary = true,
                            buttonScale = buttonScale,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = if (point?.isMastered == true)
                                "已设定为掌握" else "加入已掌握",
                            icon = if (point?.isMastered == true) null else "✓",
                            tone = ActionTone.Green,
                            isPrimary = point?.isMastered != true,
                            buttonScale = buttonScale,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.MASTERED -> {
                        ReviewActionButton(
                            text = if (point?.isReinforced == true)
                                "再次加入 ×${point.reinforcementCount}" else "加入重点集锦",
                            icon = "+",
                            tone = ActionTone.Blue,
                            isPrimary = true,
                            buttonScale = buttonScale,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = "移出已掌握",
                            icon = "−",
                            tone = ActionTone.Red,
                            isPrimary = true,
                            buttonScale = buttonScale,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                }
            }

            ReviewActionButton(
                text = "下一个",
                icon = "↬",
                tone = ActionTone.Amber,
                isPrimary = false,
                verticalContent = true,
                buttonScale = buttonScale,
                onClick = { viewModel.nextPoint() },
                modifier = Modifier.weight(0.54f)
            )
        }
    }
}

// ─── ReinforcementCompletionView ───

/**
 * Completion view shown when all reinforcement or mastered points have been reviewed.
 * iOS reference: ContentView.swift lines 9567-9590.
 */
@Composable
private fun ReinforcementCompletionView(
    metrics: KikariaPhoneMetrics,
    onReturnHome: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val masteredGreen = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val skyShadow = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.20f) else KikariaColors.Sky.copy(alpha = 0.20f)

    Column(
        modifier = Modifier
            .padding(horizontal = metrics.horizontalPadding)
            .widthIn(max = metrics.reviewMaxWidth),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(28.dp)
    ) {
        // Checkmark — iOS uses Image(systemName: "checkmark.circle.fill") at 86pt
        Text(
            text = "✓",
            fontSize = 86.sp,
            fontWeight = FontWeight.SemiBold,
            color = masteredGreen,
            modifier = Modifier.shadow(16.dp, RoundedCornerShape(50), spotColor = Color.Green.copy(alpha = 0.16f))
        )

        // Return home button — iOS uses Capsule with actionGradient
        Box(
            modifier = Modifier
                .shadow(16.dp, RoundedCornerShape(KikariaDesign.PillRadius),
                    spotColor = skyShadow, ambientColor = skyShadow)
                .clip(RoundedCornerShape(KikariaDesign.PillRadius))
                .background(actionGrad)
                .clickable { onReturnHome() }
                .padding(horizontal = 42.dp, vertical = 16.dp),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = "返回首页",
                fontSize = 17.sp,
                fontWeight = FontWeight.Bold,
                color = Color.White
            )
        }
    }
}
