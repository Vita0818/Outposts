package com.vita0818.kikaria.ui.review

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.slideInVertically
import androidx.compose.foundation.background
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.GlassCard
import com.vita0818.kikaria.ui.components.InfoCard
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import com.vita0818.kikaria.viewmodel.ReviewMode

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReviewScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit
) {
    val point = viewModel.currentPoint

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = when (viewModel.reviewMode) {
                            ReviewMode.NORMAL -> "复习"
                            ReviewMode.REINFORCEMENT -> "重点复习"
                            ReviewMode.MASTERED -> "已掌握复习"
                        },
                        fontWeight = FontWeight.SemiBold,
                        color = KikariaColors.DeepText
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Text("←", fontSize = 22.sp, color = KikariaColors.DeepText)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = KikariaColors.GlassSurface.copy(alpha = 0f)
                )
            )
        },
        containerColor = KikariaColors.GlassSurface
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
                    color = KikariaColors.TertiaryText,
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
                progress = { viewModel.reviewProgress },
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 24.dp)
                    .height(4.dp)
                    .clip(RoundedCornerShape(2.dp)),
                color = KikariaColors.Sky,
                trackColor = KikariaColors.Mist,
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
                            color = KikariaColors.DeepText,
                            lineHeight = 32.sp
                        )

                        if (point.tags.isNotEmpty()) {
                            Spacer(modifier = Modifier.height(12.dp))
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                point.tags.forEach { tag ->
                                    TagChip(tag = tag)
                                }
                            }
                        }

                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "今日已复习 ${viewModel.todayReviewCount} 次",
                            fontSize = 13.sp,
                            color = KikariaColors.TertiaryText
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
                                containerColor = KikariaColors.Sky
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
                                containerColor = KikariaColors.Cyan
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
                    modifier = Modifier.padding(horizontal = 24.dp, vertical = 12.dp)
                )
            }
        }
    }
}

@Composable
private fun BottomActionBar(
    viewModel: KikariaViewModel,
    modifier: Modifier = Modifier
) {
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
                        ActionButton(
                            text = if (viewModel.currentPoint?.isReinforced == true) "再次重点" else "加入重点",
                            color = KikariaColors.NextAmber,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ActionButton(
                            text = "标记掌握",
                            color = KikariaColors.MasteredGreen,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.REINFORCEMENT -> {
                        ActionButton(
                            text = "移出重点",
                            color = KikariaColors.NextAmber,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ActionButton(
                            text = "标记掌握",
                            color = KikariaColors.MasteredGreen,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                    ReviewMode.MASTERED -> {
                        ActionButton(
                            text = "加入重点",
                            color = KikariaColors.NextAmber,
                            onClick = { viewModel.toggleReinforcement() }
                        )
                        ActionButton(
                            text = "移出已掌握",
                            color = KikariaColors.MasteredGreen,
                            onClick = { viewModel.toggleMastered() }
                        )
                    }
                }
            }

            // Right: Next button (larger)
            Button(
                onClick = { viewModel.nextPoint() },
                modifier = Modifier
                    .weight(0.8f)
                    .height(110.dp),
                shape = RoundedCornerShape(20.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = KikariaColors.Sky
                ),
                enabled = viewModel.hasNextPoint
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text("→", fontSize = 28.sp, fontWeight = FontWeight.Bold)
                    Text("下一个", fontSize = 14.sp)
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
private fun TagChip(tag: String) {
    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(8.dp))
            .background(KikariaColors.Mist.copy(alpha = 0.8f))
            .padding(horizontal = 10.dp, vertical = 4.dp)
    ) {
        Text(
            text = tag,
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            color = KikariaColors.BlueGray
        )
    }
}
