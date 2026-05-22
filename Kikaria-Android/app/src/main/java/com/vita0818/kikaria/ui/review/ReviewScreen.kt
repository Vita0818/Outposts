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
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
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
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val tertiaryText = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText

    KikariaPageShell {
        if (point == null) {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                Text(
                    text = "\u6CA1\u6709\u53EF\u590D\u4E60\u7684\u77E5\u8BC6\u70B9",
                    color = tertiaryText,
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Medium
                )
            }
            return@KikariaPageShell
        }

        // Back button overlay
        KikariaBackButton(onClick = onBack)

        Column(modifier = Modifier.fillMaxSize()) {
            Spacer(Modifier.height(12.dp))

            // Progress bar
            LinearProgressIndicator(
                progress = { viewModel.reviewProgress },
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 24.dp)
                    .height(4.dp)
                    .clip(RoundedCornerShape(2.dp)),
                color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                trackColor = if (isDark) KikariaColors.MistDark else KikariaColors.Mist,
            )

            Spacer(Modifier.height(16.dp))

            // Scrollable content
            Column(
                modifier = Modifier
                    .weight(1f)
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 24.dp)
            ) {
                // Title card
                KikariaGlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 24.dp,
                    fillOpacity = 0.40f
                ) {
                    Column(modifier = Modifier.padding(24.dp)) {
                        Text(
                            text = KikariaTypography.mixedText(point.title, size = 24, weight = FontWeight.SemiBold),
                            color = deepText,
                            lineHeight = 32.sp
                        )

                        if (point.tags.isNotEmpty()) {
                            Spacer(Modifier.height(12.dp))
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                point.tags.forEach { tag ->
                                    KikariaTagChip(tag = tag)
                                }
                            }
                        }

                        Spacer(Modifier.height(8.dp))
                        Text(
                            text = KikariaTypography.mixedText(
                                "\u8BE5\u77E5\u8BC6\u70B9\u4ECA\u65E5\u590D\u4E60 ${viewModel.todayReviewCount} \u6B21",
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
                                text = "\u63D0\u793A",
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
                }

                if (viewModel.isHintShown) {
                    Spacer(Modifier.height(12.dp))
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
                                text = "\u7B54\u6848",
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

                // Reveal buttons
                if (!viewModel.isContentShown) {
                    Spacer(Modifier.height(16.dp))

                    if (!viewModel.isHintShown) {
                        ReviewActionButton(
                            text = "\u67E5\u770B\u63D0\u793A",
                            tone = ActionTone.Blue,
                            isPrimary = false,
                            onClick = { viewModel.showHint() }
                        )
                    } else {
                        ReviewActionButton(
                            text = "\u67E5\u770B\u7B54\u6848",
                            tone = ActionTone.Blue,
                            isPrimary = true,
                            onClick = { viewModel.showContent() }
                        )
                    }
                }

                Spacer(Modifier.height(16.dp))
            }

            // Bottom action bar
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

// ─── ReviewBottomActionBar ───

@Composable
private fun ReviewBottomActionBar(
    viewModel: KikariaViewModel,
    isDark: Boolean,
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
                            text = if (point?.isReinforced == true) "\u518D\u6B21\u52A0\u5165 \u00D7${point.reinforcementCount}" else "\u52A0\u5165\u91CD\u70B9\u96C6\u9526",
                            tone = ActionTone.Amber,
                            isPrimary = true,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = if (point?.isMastered == true) "\u5DF2\u8BBE\u5B9A\u4E3A\u638C\u63E1" else "\u52A0\u5165\u5DF2\u638C\u63E1",
                            tone = ActionTone.Green,
                            isPrimary = point?.isMastered != true,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.REINFORCEMENT -> {
                        ReviewActionButton(
                            text = "\u79FB\u51FA\u91CD\u70B9\u96C6\u9526",
                            tone = ActionTone.Red,
                            isPrimary = true,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = if (point?.isMastered == true) "\u5DF2\u8BBE\u5B9A\u4E3A\u638C\u63E1" else "\u52A0\u5165\u5DF2\u638C\u63E1",
                            tone = ActionTone.Green,
                            isPrimary = point?.isMastered != true,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.MASTERED -> {
                        ReviewActionButton(
                            text = if (point?.isReinforced == true) "\u518D\u6B21\u52A0\u5165 \u00D7${point.reinforcementCount}" else "\u52A0\u5165\u91CD\u70B9\u96C6\u9526",
                            tone = ActionTone.Amber,
                            isPrimary = true,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = "\u79FB\u51FA\u5DF2\u638C\u63E1",
                            tone = ActionTone.Red,
                            isPrimary = true,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                }
            }

            ReviewActionButton(
                text = "\u4E0B\u4E00\u4E2A",
                tone = ActionTone.Amber,
                isPrimary = false,
                verticalContent = true,
                icon = "\u21C4",
                onClick = { viewModel.nextPoint() },
                modifier = Modifier.weight(0.8f).height(110.dp)
            )
        }
    }
}
