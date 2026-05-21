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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaTagChip
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import com.vita0818.kikaria.viewmodel.ReviewMode

// ─── Tone system (matches iOS ReviewActionTone) ───

private enum class ActionTone { Blue, Green, Amber, Red }

private data class ToneColors(
    val primaryFill: Brush,
    val secondaryFill: Brush,
    val foreground: Color,
    val shadowColor: Color,
    val shadowOpacity: Float
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
            shadowOpacity = if (isPrimary) 0.22f else 0.10f
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
            shadowOpacity = if (isPrimary) 0.22f else 0.10f
        )
        ActionTone.Amber -> ToneColors(
            primaryFill = if (isDark) KikariaColors.NextGradientDark else KikariaColors.NextGradientLight,
            secondaryFill = Brush.linearGradient(listOf(
                (if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber).copy(alpha = if (isDark) 0.72f else 0.68f),
                Color(0xFF9487CC).copy(alpha = if (isDark) 0.68f else 0.56f)
            )),
            foreground = Color.White.copy(alpha = 0.94f),
            shadowColor = if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber,
            shadowOpacity = if (isPrimary) 0.12f else 0.055f
        )
        ActionTone.Red -> ToneColors(
            primaryFill = if (isDark) KikariaColors.RemoveGradientDark else KikariaColors.RemoveGradientLight,
            secondaryFill = Brush.linearGradient(listOf(
                KikariaColors.RemoveCoral.copy(alpha = if (isDark) 0.70f else 0.58f),
                Color(0xFFFA9480).copy(alpha = if (isDark) 0.56f else 0.46f)
            )),
            foreground = Color.White,
            shadowColor = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral,
            shadowOpacity = if (isPrimary) 0.22f else 0.10f
        )
    }
}

// ─── ReviewActionButton (matches iOS ReviewActionButton) ───

@Composable
private fun ReviewActionButton(
    text: String,
    tone: ActionTone = ActionTone.Blue,
    isPrimary: Boolean = true,
    verticalContent: Boolean = false,
    icon: String? = null,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val isDark = isSystemInDarkTheme()
    val colors = toneColors(tone, isDark, isPrimary)
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
            .padding(vertical = 19.dp),
        contentAlignment = Alignment.Center
    ) {
        if (verticalContent && icon != null) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(icon, fontSize = 20.sp, fontWeight = FontWeight.SemiBold, color = colors.foreground)
                Spacer(Modifier.height(8.dp))
                Text(text, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, color = colors.foreground)
            }
        } else {
            Text(text, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, color = colors.foreground)
        }
    }
}

// ─── Main ReviewScreen ───

@Composable
fun ReviewScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit
) {
    val point = viewModel.currentPoint
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    KikariaPageShell {
        if (point == null) {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                Text(
                    text = "没有可复习的知识点",
                    color = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText,
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Medium
                )
            }
            return@KikariaPageShell
        }

        // Back button overlay
        KikariaBackButton(onClick = onBack)

        Column(modifier = Modifier.fillMaxSize()) {
            // Scrollable content
            Column(
                modifier = Modifier
                    .weight(1f)
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 24.dp)
            ) {
                Spacer(Modifier.height(60.dp))

                // ── Knowledge title card (matching iOS titleGroup) ──
                KikariaGlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 26.dp,
                    fillOpacity = 0.56f,
                    shadowElevation = 18.dp,
                    shadowOpacity = 0.14f
                ) {
                    Column(
                        modifier = Modifier.padding(18.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(
                            text = KikariaTypography.mixedText(point.title, size = 24, weight = FontWeight.SemiBold),
                            color = deepText,
                            textAlign = TextAlign.Center,
                            lineHeight = 32.sp,
                            modifier = Modifier.fillMaxWidth()
                        )

                        if (point.tags.isNotEmpty()) {
                            Spacer(Modifier.height(12.dp))
                            Row(
                                horizontalArrangement = Arrangement.spacedBy(8.dp),
                                modifier = Modifier.fillMaxWidth(),
                            ) {
                                point.tags.forEach { tag ->
                                    KikariaTagChip(tag = tag)
                                }
                            }
                        }

                        Spacer(Modifier.height(8.dp))
                        // Today review count pill
                        Box(
                            modifier = Modifier
                                .clip(RoundedCornerShape(16.dp))
                                .background(
                                    if (isDark) KikariaColors.GlassSurfaceDark.copy(alpha = 0.42f)
                                    else KikariaColors.GlassSurface.copy(alpha = 0.42f)
                                )
                                .padding(horizontal = 18.dp, vertical = 8.dp)
                        ) {
                            Text(
                                text = "该知识点今日复习 ${viewModel.todayReviewCount} 次",
                                fontSize = 12.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = deepText.copy(alpha = 0.78f)
                            )
                        }
                    }
                }

                Spacer(Modifier.height(14.dp))

                // ── Hint card (animated) ──
                AnimatedVisibility(
                    visible = viewModel.isHintShown,
                    enter = fadeIn() + slideInVertically { it / 2 }
                ) {
                    Column {
                        KikariaGlassCard(
                            modifier = Modifier.fillMaxWidth(),
                            cornerRadius = 26.dp,
                            fillOpacity = 0.56f,
                            shadowElevation = 18.dp,
                            shadowOpacity = 0.14f
                        ) {
                            Column(modifier = Modifier.padding(18.dp)) {
                                Text(
                                    text = "提示",
                                    fontSize = 14.sp,
                                    fontWeight = FontWeight.Bold,
                                    color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                                )
                                Spacer(Modifier.height(10.dp))
                                Text(
                                    text = KikariaTypography.mixedText(point.hint, size = 17, weight = FontWeight.Normal),
                                    color = deepText,
                                    lineHeight = 26.sp
                                )
                            }
                        }
                        Spacer(Modifier.height(12.dp))
                    }
                }

                // ── Answer card (animated) ──
                AnimatedVisibility(
                    visible = viewModel.isContentShown,
                    enter = fadeIn() + slideInVertically { it / 2 }
                ) {
                    KikariaGlassCard(
                        modifier = Modifier.fillMaxWidth(),
                        cornerRadius = 26.dp,
                        fillOpacity = 0.56f,
                        shadowElevation = 18.dp,
                        shadowOpacity = 0.14f
                    ) {
                        Column(modifier = Modifier.padding(18.dp)) {
                            Text(
                                text = "答案",
                                fontSize = 14.sp,
                                fontWeight = FontWeight.Bold,
                                color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                            )
                            Spacer(Modifier.height(10.dp))
                            Text(
                                text = KikariaTypography.mixedText(point.content, size = 17, weight = FontWeight.Normal),
                                color = deepText,
                                lineHeight = 26.sp
                            )
                        }
                    }
                }

                // ── Reveal buttons (when answer not shown) ──
                if (!viewModel.isContentShown) {
                    Spacer(Modifier.height(16.dp))

                    if (!viewModel.isHintShown) {
                        ReviewActionButton(
                            text = "查看提示",
                            tone = ActionTone.Blue,
                            isPrimary = false,
                            onClick = { viewModel.showHint() }
                        )
                    }

                    if (viewModel.isHintShown) {
                        Spacer(Modifier.height(12.dp))
                        ReviewActionButton(
                            text = "查看答案",
                            tone = ActionTone.Blue,
                            isPrimary = true,
                            onClick = { viewModel.showContent() }
                        )
                    }
                }

                Spacer(Modifier.height(16.dp))
            }

            // ── Bottom action bar (when answer is revealed) ──
            if (viewModel.isContentShown) {
                ReviewBottomActionBar(
                    viewModel = viewModel,
                    isDark = isDark,
                    modifier = Modifier.padding(horizontal = 24.dp, vertical = 12.dp)
                )
            }
        }
    }
}

// ─── ReviewBottomActionBar (matches iOS answeredActionGrid with tone system) ───

@Composable
private fun ReviewBottomActionBar(
    viewModel: KikariaViewModel,
    isDark: Boolean,
    modifier: Modifier = Modifier
) {
    val point = viewModel.currentPoint ?: return

    Column(modifier = modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            verticalAlignment = Alignment.Top
        ) {
            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                when (viewModel.reviewMode) {
                    ReviewMode.NORMAL -> {
                        ReviewActionButton(
                            text = if (point.isReinforced) "再次加入 ×${point.reinforcementCount}" else "加入重点集锦",
                            tone = ActionTone.Amber,
                            isPrimary = true,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = if (point.isMastered) "已设定为掌握" else "加入已掌握",
                            tone = ActionTone.Green,
                            isPrimary = !point.isMastered,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.REINFORCEMENT -> {
                        ReviewActionButton(
                            text = "移出重点集锦",
                            tone = ActionTone.Red,
                            isPrimary = true,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = if (point.isMastered) "已设定为掌握" else "加入已掌握",
                            tone = ActionTone.Green,
                            isPrimary = !point.isMastered,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.MASTERED -> {
                        ReviewActionButton(
                            text = if (point.isReinforced) "再次加入 ×${point.reinforcementCount}" else "加入重点集锦",
                            tone = ActionTone.Amber,
                            isPrimary = true,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = "移出已掌握",
                            tone = ActionTone.Red,
                            isPrimary = true,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                }
            }

            // "Next" button (vertical, amber tone)
            ReviewActionButton(
                text = "下一个",
                tone = ActionTone.Amber,
                isPrimary = false,
                verticalContent = true,
                icon = KikariaIcons_TEXT_NEXT,
                onClick = { viewModel.nextPoint() },
                modifier = Modifier.weight(0.8f).height(110.dp)
            )
        }
    }
}

private const val KikariaIcons_TEXT_NEXT = "\u21C4"
