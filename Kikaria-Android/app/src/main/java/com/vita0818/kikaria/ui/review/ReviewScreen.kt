package com.vita0818.kikaria.ui.review

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.slideInVertically
import androidx.compose.foundation.background
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
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.GlassCard
import com.vita0818.kikaria.ui.components.InfoCard
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import com.vita0818.kikaria.viewmodel.ReviewMode

/**
 * Review screen, translated from the review section of ContentView.swift.
 *
 * Shows knowledge point title, tags, hint/content reveal, and mode-specific action buttons.
 * Supports three review modes: Normal, Reinforcement, Mastered.
 *
 * Key improvements from source:
 * - Point counter (e.g., "3 / 7") in app bar
 * - Previous and Next navigation buttons
 * - Adaptive dark mode colors
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReviewScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit
) {
    val point = viewModel.currentPoint
    val isDark = isSystemInDarkTheme()
    val totalPoints = viewModel.reviewQueue.size
    val currentIndex = viewModel.currentReviewIndex
    val progressCounter = if (totalPoints > 0) "${currentIndex + 1} / $totalPoints" else ""

    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = when (viewModel.reviewMode) {
                                ReviewMode.NORMAL -> "复习"
                                ReviewMode.REINFORCEMENT -> "重点复习"
                                ReviewMode.MASTERED -> "已掌握复习"
                            },
                            fontWeight = FontWeight.SemiBold,
                            color = deepText
                        )
                        if (progressCounter.isNotEmpty()) {
                            Text(
                                text = progressCounter,
                                fontSize = 12.sp,
                                fontWeight = FontWeight.Medium,
                                color = softText
                            )
                        }
                    }
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Text(
                            "←",
                            fontSize = 22.sp,
                            fontFamily = KikariaTypography.serifFamily,
                            color = deepText
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = glassSurface.copy(alpha = 0f)
                )
            )
        },
        containerColor = glassSurface
    ) { padding ->
        if (point == null) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = "没有可复习的知识点",
                    color = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText,
                    fontSize = 18.sp
                )
            }
            return@Scaffold
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            // Progress bar
            LinearProgressIndicator(
                progress = viewModel.reviewProgress,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 24.dp)
                    .height(4.dp)
                    .clip(RoundedCornerShape(2.dp)),
                color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky,
                trackColor = if (isDark) KikariaColors.MistDark else KikariaColors.Mist
            )

            Spacer(modifier = Modifier.height(16.dp))

            // Scrollable content area
            Column(
                modifier = Modifier
                    .weight(1f)
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 24.dp)
            ) {
                // Title card
                GlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 24.dp
                ) {
                    Column(modifier = Modifier.padding(24.dp)) {
                        Text(
                            text = point.title,
                            fontSize = 24.sp,
                            fontWeight = FontWeight.Bold,
                            color = deepText,
                            lineHeight = 32.sp
                        )

                        if (point.tags.isNotEmpty()) {
                            Spacer(modifier = Modifier.height(12.dp))
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                point.tags.forEach { tag ->
                                    TagChip(tag = tag, isDark = isDark)
                                }
                            }
                        }

                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "今日已复习 ${viewModel.todayReviewCount} 次",
                            fontSize = 13.sp,
                            color = if (isDark) KikariaColors.TertiaryTextDark else KikariaColors.TertiaryText
                        )
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                // Hint section
                AnimatedVisibility(
                    visible = viewModel.isHintShown,
                    enter = fadeIn() + slideInVertically { it / 2 }
                ) {
                    InfoCard(
                        modifier = Modifier.fillMaxWidth(),
                        label = "💡 提示",
                        text = point.hint
                    )
                }

                if (viewModel.isHintShown) {
                    Spacer(modifier = Modifier.height(12.dp))
                }

                // Content section
                AnimatedVisibility(
                    visible = viewModel.isContentShown,
                    enter = fadeIn() + slideInVertically { it / 2 }
                ) {
                    InfoCard(
                        modifier = Modifier.fillMaxWidth(),
                        label = "📖 内容",
                        text = point.content
                    )
                }

                // Show Hint / Show Content buttons
                if (!viewModel.isContentShown) {
                    Spacer(modifier = Modifier.height(24.dp))

                    if (!viewModel.isHintShown) {
                        Button(
                            onClick = { viewModel.showHint() },
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(52.dp),
                            shape = RoundedCornerShape(16.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                            )
                        ) {
                            Text("查看提示", fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
                        }
                    } else {
                        Button(
                            onClick = { viewModel.showContent() },
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(52.dp),
                            shape = RoundedCornerShape(16.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan
                            )
                        ) {
                            Text("查看答案", fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
                        }
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))
            }

            // Bottom action bar (visible after content is shown)
            if (viewModel.isContentShown) {
                BottomActionBar(
                    viewModel = viewModel,
                    isDark = isDark,
                    modifier = Modifier.padding(horizontal = 24.dp, vertical = 12.dp)
                )
            }
        }
    }
}

@Composable
private fun BottomActionBar(
    viewModel: KikariaViewModel,
    isDark: Boolean,
    modifier: Modifier = Modifier
) {
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    Column(modifier = modifier.fillMaxWidth()) {
        // Previous/Next navigation row
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(bottom = 12.dp),
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Previous button
            Button(
                onClick = { viewModel.previousPoint() },
                modifier = Modifier
                    .weight(0.5f)
                    .height(44.dp),
                shape = RoundedCornerShape(14.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = sky.copy(alpha = 0.30f),
                    disabledContainerColor = sky.copy(alpha = 0.10f)
                ),
                enabled = viewModel.hasPreviousPoint
            ) {
                Text(
                    "← 上一个",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Medium,
                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                )
            }

            // Next button
            Button(
                onClick = { viewModel.nextPoint() },
                modifier = Modifier
                    .weight(0.5f)
                    .height(44.dp),
                shape = RoundedCornerShape(14.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = sky.copy(alpha = 0.30f),
                    disabledContainerColor = sky.copy(alpha = 0.10f)
                ),
                enabled = viewModel.hasNextPoint
            ) {
                Text(
                    "下一个 →",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Medium,
                    color = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
                )
            }
        }

        // Mode-specific action buttons
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
                        ActionButton(
                            text = if (viewModel.currentPoint?.isReinforced == true) "再次重点" else "加入重点",
                            color = if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ActionButton(
                            text = "标记掌握",
                            color = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.REINFORCEMENT -> {
                        ActionButton(
                            text = "移出重点",
                            color = if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ActionButton(
                            text = "标记掌握",
                            color = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.MASTERED -> {
                        ActionButton(
                            text = "加入重点",
                            color = if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ActionButton(
                            text = "移出已掌握",
                            color = if (isDark) KikariaColors.MasteredGreenDark else KikariaColors.MasteredGreen,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun ActionButton(
    text: String,
    color: Color,
    onClick: () -> Unit
) {
    Button(
        onClick = onClick,
        modifier = Modifier
            .fillMaxWidth()
            .height(50.dp),
        shape = RoundedCornerShape(16.dp),
        colors = ButtonDefaults.buttonColors(containerColor = color)
    ) {
        Text(
            text = text,
            fontSize = 15.sp,
            fontWeight = FontWeight.SemiBold,
            color = Color.White
        )
    }
}

@Composable
private fun TagChip(tag: String, isDark: Boolean) {
    val mist = if (isDark) KikariaColors.MistDark else KikariaColors.Mist
    val blueGray = if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray

    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(8.dp))
            .background(mist.copy(alpha = 0.8f))
            .padding(horizontal = 10.dp, vertical = 4.dp)
    ) {
        Text(
            text = tag,
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            color = blueGray
        )
    }
}
