package com.vita0818.kikaria.ui.pages

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
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
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import com.vita0818.kikaria.AppModel
import com.vita0818.kikaria.Routes
import com.vita0818.kikaria.data.AppStore
import com.vita0818.kikaria.data.StudyLogic
import com.vita0818.kikaria.ui.GlassCard
import com.vita0818.kikaria.ui.ProfileAvatar
import com.vita0818.kikaria.ui.theme.kikariaColors
import java.util.Calendar
import kotlin.math.roundToInt

@Composable
fun HomePage(navController: NavController) {
    val colors = kikariaColors()
    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp),
    ) {
        Spacer(Modifier.height(14.dp))
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(
                "Kikaria",
                color = colors.deepText,
                fontSize = 39.sp,
                fontWeight = FontWeight.SemiBold,
                fontFamily = FontFamily.Serif,
            )
            Spacer(Modifier.weight(1f))
            ProfileAvatar(size = 44) { navController.navigate(Routes.SETTINGS) }
        }
        Spacer(Modifier.height(32.dp))
        Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
            StartReviewButton { navController.navigate(Routes.review("normal")) }
        }
        Spacer(Modifier.height(30.dp))
        TodayProgressCard(navController)
        Spacer(Modifier.height(12.dp))
        DashboardGridCard(navController)
        Spacer(Modifier.height(28.dp))
    }
}

@Composable
private fun TodayProgressCard(navController: NavController) {
    val colors = kikariaColors()
    val cal = Calendar.getInstance()
    val dateTitle = "${cal.get(Calendar.MONTH) + 1}月${cal.get(Calendar.DAY_OF_MONTH)}日"
    val countdown = AppModel.countdownDayCount
    val remaining = countdown?.let { "剩余 $it 天" } ?: "--"

    GlassCard(
        cornerRadius = 25,
        fillAlpha = 0.42f,
        strokeAlpha = 0.46f,
        modifier = Modifier
            .fillMaxWidth()
            .clickable { navController.navigate(Routes.TODAY) },
    ) {
        Row(
            Modifier.padding(horizontal = 18.dp, vertical = 14.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Column(Modifier.weight(1f)) {
                Text(dateTitle, color = colors.deepText, fontSize = 23.sp, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.height(2.dp))
                Text(remaining, color = colors.softText, fontSize = 13.sp)
            }
            Row(verticalAlignment = Alignment.Bottom) {
                Text(
                    "${AppModel.todayMasteredCount}",
                    color = colors.masteredDeepGreen,
                    fontSize = 25.sp,
                    fontWeight = FontWeight.Bold,
                    fontFamily = FontFamily.Serif,
                )
                Text(
                    "/${AppModel.dailyGoal}",
                    color = colors.softText,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    fontFamily = FontFamily.Serif,
                )
            }
            Icon(
                Icons.AutoMirrored.Filled.KeyboardArrowRight,
                contentDescription = null,
                tint = colors.blueGray.copy(alpha = 0.52f),
                modifier = Modifier.padding(start = 4.dp),
            )
        }
    }
}

@Composable
private fun DashboardGridCard(navController: NavController) {
    val colors = kikariaColors()
    GlassCard(cornerRadius = 28, fillAlpha = 0.40f, strokeAlpha = 0.44f, modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly) {
                MetricColumn("范围", "${AppModel.allTags.size}", colors.sky, Modifier.weight(1f)) { navController.navigate(Routes.SCOPE) }
                VerticalDivider()
                MetricColumn("重点集锦", "${AppModel.reinforcedPoints.size}", colors.cyan, Modifier.weight(1f)) { navController.navigate(Routes.REINFORCEMENT) }
                VerticalDivider()
                MetricColumn("已掌握", "${AppModel.masteredCount}", colors.masteredGreen, Modifier.weight(1f)) { navController.navigate(Routes.MASTERED) }
            }
            Spacer(Modifier.height(12.dp))
            Box(
                Modifier
                    .fillMaxWidth()
                    .height(1.dp)
                    .background(colors.blueGray.copy(alpha = 0.12f)),
            )
            Spacer(Modifier.height(12.dp))
            Row(
                Modifier
                    .fillMaxWidth()
                    .clickable { navController.navigate(Routes.PRESET_SELECTION) }
                    .padding(vertical = 6.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(Modifier.weight(1f)) {
                    Text(
                        AppModel.currentPreset?.name ?: "预设不存在",
                        color = colors.deepText,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.SemiBold,
                    )
                    Spacer(Modifier.height(2.dp))
                    Text("当前预设", color = colors.softText, fontSize = 12.sp)
                }
                Icon(
                    Icons.AutoMirrored.Filled.KeyboardArrowRight,
                    contentDescription = null,
                    tint = colors.blueGray.copy(alpha = 0.52f),
                )
            }
        }
    }
}

@Composable
private fun MetricColumn(title: String, value: String, tint: Color, modifier: Modifier, onClick: () -> Unit) {
    val colors = kikariaColors()
    Column(
        modifier
            .clickable { onClick() }
            .padding(vertical = 4.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text(value, color = tint, fontSize = 24.sp, fontWeight = FontWeight.Bold, fontFamily = FontFamily.Serif)
        Spacer(Modifier.height(4.dp))
        Text(title, color = colors.softText, fontSize = 13.sp)
    }
}

@Composable
private fun VerticalDivider() {
    val colors = kikariaColors()
    Box(
        Modifier
            .width(1.dp)
            .height(42.dp)
            .background(colors.blueGray.copy(alpha = 0.16f), RoundedCornerShape(1.dp)),
    )
}

/** 中央开始背诵按钮:渐变大圆 + 白色箭头 + 四个装饰泡(呼吸/公转动画)。 */
@Composable
fun StartReviewButton(onClick: () -> Unit) {
    val colors = kikariaColors()
    val transition = rememberInfiniteTransition(label = "start")
    val breathe by transition.animateFloat(
        initialValue = 0.97f,
        targetValue = 1.04f,
        animationSpec = infiniteRepeatable(tween(2700, easing = LinearEasing), RepeatMode.Reverse),
        label = "breathe",
    )
    val orbit by transition.animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec = infiniteRepeatable(tween(60_000, easing = LinearEasing), RepeatMode.Restart),
        label = "orbit",
    )

    Box(
        Modifier
            .size(260.dp),
        contentAlignment = Alignment.Center,
    ) {
        DecorativeBubble(92, colors.bubbleMint, 0.48f, orbit + 20f, 100f, 66f)
        DecorativeBubble(80, colors.bubbleLavender, 0.42f, orbit + 140f, 96f, 60f)
        DecorativeBubble(78, colors.bubbleGreen, 0.38f, orbit + 230f, 104f, 74f)
        DecorativeBubble(74, colors.bubbleWhite, 0.36f, orbit + 310f, 96f, 80f)

        Box(
            Modifier
                .size(190.dp * breathe)
                .shadow(18.dp, CircleShape, ambientColor = colors.sky.copy(alpha = 0.28f))
                .clip(CircleShape)
                .background(Brush.linearGradient(colors.actionGradient))
                .clickable { onClick() },
            contentAlignment = Alignment.Center,
        ) {
            Icon(
                Icons.AutoMirrored.Filled.ArrowForward,
                contentDescription = "开始背诵",
                tint = Color.White,
                modifier = Modifier.size(70.dp),
            )
        }
    }
}

@Composable
private fun DecorativeBubble(sizeDp: Int, tint: Color, alpha: Float, angleDeg: Float, radiusXDp: Float, radiusYDp: Float) {
    val angle = Math.toRadians(angleDeg.toDouble())
    val dx = (radiusXDp * kotlin.math.cos(angle)).roundToInt()
    val dy = (radiusYDp * kotlin.math.sin(angle)).roundToInt()
    Box(
        Modifier
            .offset { IntOffset(dx, dy) }
            .size(sizeDp.dp)
            .clip(CircleShape)
            .background(
                Brush.linearGradient(
                    listOf(tint.copy(alpha = alpha), tint.copy(alpha = alpha * 0.55f), Color.White.copy(alpha = 0.35f)),
                ),
            ),
    )
}

/** 首页/概览共用的进度文案。 */
fun progressMessage(todayMastered: Int, goal: Int, todayReviewed: Int): String = when {
    todayMastered >= goal -> "今日目标已经达成，保持这份节奏就很好。"
    todayReviewed > 0 -> "今日已经进入状态，还差 ${goal - todayMastered} 个新增掌握达到目标。"
    else -> "今天还很安静，可以从一个知识点开始。"
}
