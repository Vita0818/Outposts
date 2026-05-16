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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import com.vita0818.kikaria.viewmodel.ReviewMode

// ─── Glass stroke helpers ───

private fun glassCardStrokeColors(isDark: Boolean): List<Color> {
    val accent = if (isDark) KikariaColors.GlassStrokeAccentDark else KikariaColors.GlassStrokeAccent
    return listOf(
        Color.White.copy(alpha = if (isDark) 0.36f else 0.44f),
        Color.White.copy(alpha = if (isDark) 0.08f else 0.10f),
        accent.copy(alpha = if (isDark) 0.22f else 0.14f)
    )
}

private fun Modifier.glassStroke(
    shape: RoundedCornerShape,
    isDark: Boolean,
    lineWidth: Float = 1f
): Modifier = this.drawBehind {
    val strokeWidth = lineWidth * density
    drawRoundRect(
        brush = Brush.linearGradient(
            colors = glassCardStrokeColors(isDark),
            start = Offset.Zero,
            end = Offset(size.width, size.height)
        ),
        cornerRadius = CornerRadius(
            shape.topStart.toPx(size, this),
            shape.topEnd.toPx(size, this)
        ),
        style = Stroke(width = strokeWidth)
    )
}

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
    val glassFill = Brush.linearGradient(listOf(glass.copy(alpha = 0f), glass.copy(alpha = 0f)))
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
                KikariaColors.NextAmber.copy(alpha = if (isDark) 0.72f else 0.68f),
                Color(0xFF9487CC).copy(alpha = if (isDark) 0.68f else 0.56f)
            )),
            foreground = Color.White.copy(alpha = 0.94f),
            shadowColor = if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber,
            shadowOpacity = if (isPrimary) 0.12f else 0.055f,
            strokeAccent = if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber,
            strokeAccentOpacity = 0.16f
        )
        ActionTone.Red -> {
            val removeLight = Brush.linearGradient(listOf(Color(0xFFE66059), Color(0xFFFA9480)))
            val removeDark = Brush.linearGradient(listOf(Color(0xFF942420), Color(0xFFDB4747)))
            ToneColors(
                primaryFill = if (isDark) removeDark else removeLight,
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
            .shadow(16.dp, shape, ambientColor = colors.shadowColor.copy(alpha = colors.shadowOpacity),
                spotColor = colors.shadowColor.copy(alpha = colors.shadowOpacity))
            .clip(shape)
            .background(if (isPrimary) colors.primaryFill else colors.secondaryFill)
            .glassStroke(shape, isDark)
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
    val pageGradient = if (isDark) KikariaColors.PageGradientDark else KikariaColors.PageGradientLight
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(pageGradient)
    ) {
        if (point == null) {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                Text(
                    text = "没有可复习的知识点",
                    color = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText,
                    fontSize = 18.sp
                )
            }
            return@Box
        }

        // Back button overlay (glass circle, matches iOS KikariaAdaptiveBackButton)
        Box(
            modifier = Modifier
                .padding(start = 24.dp, top = 12.dp)
                .size(42.dp)
                .shadow(10.dp, CircleShape,
                    ambientColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.08f),
                    spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.08f))
                .clip(CircleShape)
                .background(glassSurface.copy(alpha = 0.40f))
                .clickable { onBack() },
            contentAlignment = Alignment.Center
        ) {
            Text("‹", fontSize = 22.sp, fontWeight = FontWeight.SemiBold,
                color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText)
        }

        Column(modifier = Modifier.fillMaxSize()) {
            Spacer(Modifier.height(12.dp))

            // Progress bar
            LinearProgressIndicator(
                progress = viewModel.reviewProgress,
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
                // Title card (glass-styled, matches iOS titleGroup)
                val titleCardShape = RoundedCornerShape(24.dp)
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .shadow(18.dp, titleCardShape,
                            ambientColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.12f),
                            spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.12f))
                        .clip(titleCardShape)
                        .background(glassSurface.copy(alpha = 0.40f))
                        .glassStroke(titleCardShape, isDark)
                        .padding(24.dp)
                ) {
                    Column {
                        Text(
                            text = point.title,
                            fontFamily = FontFamily.Serif,
                            fontSize = 24.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText,
                            lineHeight = 32.sp
                        )

                        if (point.tags.isNotEmpty()) {
                            Spacer(Modifier.height(12.dp))
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                point.tags.forEach { tag -> ReviewTagChip(tag, isDark) }
                            }
                        }

                        Spacer(Modifier.height(8.dp))
                        Text(
                            text = "该知识点今日复习 ${viewModel.todayReviewCount} 次",
                            fontFamily = FontFamily.Serif,
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = (if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText).copy(alpha = 0.78f)
                        )
                    }
                }

                Spacer(Modifier.height(14.dp))

                // Hint card (glass-styled, matches iOS FloatingInfoCard)
                AnimatedVisibility(
                    visible = viewModel.isHintShown,
                    enter = fadeIn() + slideInVertically { it / 2 }
                ) {
                    ReviewInfoCard(label = "提示", text = point.hint, isDark = isDark)
                }

                if (viewModel.isHintShown) {
                    Spacer(Modifier.height(12.dp))
                }

                // Content card (glass-styled)
                AnimatedVisibility(
                    visible = viewModel.isContentShown,
                    enter = fadeIn() + slideInVertically { it / 2 }
                ) {
                    ReviewInfoCard(label = "答案", text = point.content, isDark = isDark)
                }

                // Reveal buttons (glass-styled, matches iOS revealButtons)
                if (!viewModel.isContentShown) {
                    Spacer(Modifier.height(16.dp))

                    if (!viewModel.isHintShown) {
                        ReviewActionButton(
                            text = "查看提示",
                            tone = ActionTone.Blue,
                            isPrimary = false,
                            onClick = { viewModel.showHint() }
                        )
                    } else {
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

            // Bottom action bar (matches iOS answeredActionGrid)
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
                            text = if (point?.isReinforced == true) "再次加入 ×${point.reinforcementCount}" else "加入重点集锦",
                            tone = ActionTone.Amber,
                            isPrimary = true,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ReviewActionButton(
                            text = if (point?.isMastered == true) "已设定为掌握" else "加入已掌握",
                            tone = ActionTone.Green,
                            isPrimary = point?.isMastered != true,
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
                            text = if (point?.isMastered == true) "已设定为掌握" else "加入已掌握",
                            tone = ActionTone.Green,
                            isPrimary = point?.isMastered != true,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.MASTERED -> {
                        ReviewActionButton(
                            text = if (point?.isReinforced == true) "再次加入 ×${point?.reinforcementCount}" else "加入重点集锦",
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

            ReviewActionButton(
                text = "下一个",
                tone = ActionTone.Amber,
                isPrimary = false,
                verticalContent = true,
                icon = "⇄",
                onClick = { viewModel.nextPoint() },
                modifier = Modifier.weight(0.8f).height(110.dp)
            )
        }
    }
}

// ─── ReviewInfoCard ───

@Composable
private fun ReviewInfoCard(label: String, text: String, isDark: Boolean) {
    val shape = RoundedCornerShape(26.dp)
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(18.dp, shape,
                ambientColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.14f),
                spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.14f))
            .clip(shape)
            .background(glassSurface.copy(alpha = 0.56f))
            .glassStroke(shape, isDark)
            .padding(18.dp)
    ) {
        Column {
            Text(
                text = label,
                fontSize = 14.sp,
                fontWeight = FontWeight.Bold,
                color = sky
            )
            Spacer(Modifier.height(10.dp))
            Text(
                text = text,
                fontSize = 17.sp,
                fontWeight = FontWeight.Normal,
                color = deepText,
                lineHeight = 26.sp
            )
        }
    }
}

// ─── ReviewTagChip ───

@Composable
private fun ReviewTagChip(tag: String, isDark: Boolean) {
    val capsuleShape = RoundedCornerShape(16.dp)
    Box(
        modifier = Modifier
            .clip(capsuleShape)
            .shadow(6.dp, capsuleShape,
                ambientColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.04f),
                spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.04f))
            .background((if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface).copy(alpha = 0.38f))
            .padding(horizontal = 11.dp, vertical = 6.dp)
    ) {
        Text(
            text = tag,
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
        )
    }
}
