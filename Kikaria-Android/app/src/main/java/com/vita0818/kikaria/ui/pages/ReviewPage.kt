package com.vita0818.kikaria.ui.pages

import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.togetherWith
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.gestures.detectHorizontalDragGestures
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
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AddCircle
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.Lightbulb
import androidx.compose.material.icons.filled.RemoveCircle
import androidx.compose.material.icons.filled.Shuffle
import androidx.compose.material.icons.filled.Tag
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import com.vita0818.kikaria.AppModel
import com.vita0818.kikaria.Routes
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.ui.GlassCard
import com.vita0818.kikaria.ui.GradientCapsuleButton
import com.vita0818.kikaria.ui.LightTagRow
import com.vita0818.kikaria.ui.PageHeader
import com.vita0818.kikaria.ui.ReviewActionButton
import com.vita0818.kikaria.ui.SoftEmptyState
import com.vita0818.kikaria.ui.math.KikariaMathText
import com.vita0818.kikaria.ui.theme.kikariaColors
import kotlin.math.abs

enum class ReviewMode { NORMAL, REINFORCEMENT, MASTERED }

/** 复习页:队列 shuffle、提示/答案、三模式动作网格、方向手势。 */
@Composable
fun ReviewPage(navController: NavController, modeName: String) {
    val mode = when (modeName) {
        "reinforcement" -> ReviewMode.REINFORCEMENT
        "mastered" -> ReviewMode.MASTERED
        else -> ReviewMode.NORMAL
    }
    val colors = kikariaColors()

    val points: List<KnowledgePoint> = when (mode) {
        ReviewMode.NORMAL -> {
            val tags = AppModel.selectedTags
            if (tags.isEmpty()) AppModel.knowledgePoints
            else AppModel.knowledgePoints.filter { it.tags.any(tags::contains) }
        }
        ReviewMode.REINFORCEMENT -> AppModel.reinforcedPoints
        ReviewMode.MASTERED -> AppModel.masteredPoints
    }

    var queue by remember(mode) { mutableStateOf<List<String>>(emptyList()) }
    var index by remember(mode) { mutableStateOf(0) }
    var isShowingHint by remember { mutableStateOf(false) }
    var isShowingContent by remember { mutableStateOf(false) }

    fun rebuildQueue(avoidFirstId: String?) {
        val ids = points.map { it.id }.shuffled().toMutableList()
        if (ids.size > 1 && ids.first() == avoidFirstId) {
            val swapIndex = ids.indexOfFirst { it != avoidFirstId }
            if (swapIndex > 0) {
                ids[0] = ids[swapIndex].also { ids[swapIndex] = ids[0] }
            }
        }
        queue = ids
        index = 0
    }

    fun resetRevealState() {
        isShowingHint = false
        isShowingContent = false
    }

    // 队列初始化与知识点集变化时 reconcile
    LaunchedEffect(mode) { rebuildQueue(null) }
    LaunchedEffect(points.map { it.id }.joinToString(",")) {
        val valid = points.map { it.id }.toSet()
        queue = queue.filter { valid.contains(it) }
        if (queue.isEmpty()) rebuildQueue(null)
        if (index >= queue.size) index = 0
    }

    fun next() {
        if (queue.isEmpty()) return
        if (index >= queue.size - 1) {
            val avoid = queue.getOrNull(index)
            resetRevealState()
            rebuildQueue(avoid)
        } else {
            resetRevealState()
            index += 1
        }
    }

    fun previous() {
        if (queue.isEmpty()) return
        resetRevealState()
        index = if (index == 0) queue.size - 1 else index - 1
    }

    fun revealHint() {
        val point = queue.getOrNull(index)?.let(AppModel::pointById) ?: return
        if (!isShowingHint) {
            isShowingHint = true
            AppModel.recordViewedHint(point)
        }
    }

    fun revealContent() {
        val point = queue.getOrNull(index)?.let(AppModel::pointById) ?: return
        if (!isShowingContent) {
            isShowingContent = true
            AppModel.recordReviewedAnswer(point)
        }
    }

    if (points.isEmpty()) {
        Column(Modifier.fillMaxSize()) {
            when (mode) {
                ReviewMode.REINFORCEMENT, ReviewMode.MASTERED -> {
                    Column(
                        Modifier.fillMaxSize().padding(24.dp),
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.Center,
                    ) {
                        Icon(
                            Icons.Filled.CheckCircle,
                            contentDescription = null,
                            tint = colors.masteredGreen,
                            modifier = Modifier.size(86.dp),
                        )
                        Spacer(Modifier.height(20.dp))
                        GradientCapsuleButton("返回首页", gradient = colors.actionGradient) {
                            navController.popBackStack(Routes.HOME, inclusive = false)
                        }
                    }
                }
                ReviewMode.NORMAL -> {
                    PageHeader(title = "背诵", onBack = { navController.popBackStack() })
                    SoftEmptyState(
                        icon = Icons.Filled.Tag,
                        title = "暂无知识点",
                        subtitle = "请返回后调整选择范围。",
                        modifier = Modifier.padding(24.dp),
                    )
                }
            }
        }
        return
    }

    val currentPoint: KnowledgePoint? = queue.getOrNull(index)?.let(AppModel::pointById)

    // 横向手势(对齐 Apple 左右滑语义,阈值 80px);纵向操作由按钮承担,滚动保持原生
    var dragTotalX by remember { mutableStateOf(0f) }
    val gestureModifier = Modifier.pointerInput(mode, currentPoint?.id, isShowingContent) {
        detectHorizontalDragGestures(
            onDragEnd = {
                val dx = dragTotalX
                dragTotalX = 0f
                if (abs(dx) > 80) {
                    if (dx < 0) {
                        when (mode) {
                            ReviewMode.NORMAL -> {
                                // 左滑:显示答案并加入重点集锦(绝不标记掌握)
                                revealContent()
                                currentPoint?.let { AppModel.addReinforcement(it.id) }
                            }
                            ReviewMode.REINFORCEMENT -> {
                                currentPoint?.let { AppModel.removeReinforcement(it.id) }
                                next()
                            }
                            ReviewMode.MASTERED -> {
                                currentPoint?.let { AppModel.removeMastered(it.id) }
                                next()
                            }
                        }
                    } else if (mode == ReviewMode.NORMAL) {
                        navController.navigate(Routes.SCOPE)
                    }
                }
            },
        ) { change, amount ->
            change.consume()
            dragTotalX += amount
        }
    }

    Column(Modifier.fillMaxSize().padding(bottom = 16.dp)) {
        Column(
            Modifier
                .weight(1f)
                .then(gestureModifier)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
        ) {
            AnimatedContent(
                targetState = currentPoint?.id to (isShowingHint to isShowingContent),
                transitionSpec = { fadeIn(androidx.compose.animation.core.tween(200)) togetherWith fadeOut(androidx.compose.animation.core.tween(180)) },
                label = "reviewPoint",
            ) { target ->
                val point = target.first?.let(AppModel::pointById)
                val showHint = target.second.first
                val showContent = target.second.second
                if (point != null) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text(
                            point.title,
                            color = colors.deepText,
                            fontSize = 40.sp,
                            fontWeight = FontWeight.SemiBold,
                            fontFamily = FontFamily.Serif,
                            textAlign = TextAlign.Center,
                            lineHeight = 50.sp,
                        )
                        if (point.tags.isNotEmpty()) {
                            Spacer(Modifier.height(16.dp))
                            LightTagRow(tags = point.tags)
                        }
                        val reviewCount = AppModel.todayReviewCountFor(point.id)
                        if (reviewCount > 0) {
                            Spacer(Modifier.height(10.dp))
                            Text(
                                "该知识点今日复习 $reviewCount 次",
                                color = colors.deepText.copy(alpha = 0.78f),
                                fontSize = 12.sp,
                            )
                        }
                        if (showHint) {
                            Spacer(Modifier.height(20.dp))
                            FloatingInfoCard(title = "提示", text = point.hint, fontSize = 17)
                        }
                        if (showContent) {
                            Spacer(Modifier.height(14.dp))
                            FloatingInfoCard(title = "答案", text = point.content, fontSize = 18)
                        }
                    }
                }
            }
        }

        // 底部动作区
        Column(Modifier.padding(horizontal = 24.dp), verticalArrangement = Arrangement.spacedBy(14.dp)) {
            if (!isShowingContent) {
                ReviewActionButton(
                    text = "查看提示",
                    icon = Icons.Filled.Lightbulb,
                    gradient = colors.actionGradient,
                    toneColor = colors.sky,
                    primary = false,
                    enabled = !isShowingHint,
                ) { revealHint() }
                ReviewActionButton(
                    text = "查看答案",
                    icon = Icons.Filled.Description,
                    gradient = colors.actionGradient,
                    toneColor = colors.sky,
                    primary = true,
                ) { revealContent() }
            } else {
                currentPoint?.let { point ->
                    val addFocusText = if (point.reinforcementCount > 0) "再次加入 ×${point.reinforcementCount}" else "加入重点集锦"
                    when (mode) {
                        ReviewMode.NORMAL -> {
                            ReviewActionButton(
                                text = addFocusText,
                                icon = Icons.Filled.AddCircle,
                                gradient = colors.actionGradient,
                                toneColor = colors.sky,
                                primary = true,
                            ) { AppModel.addReinforcement(point.id) }
                            MasteredButton(point)
                        }
                        ReviewMode.REINFORCEMENT -> {
                            ReviewActionButton(
                                text = "移出重点集锦",
                                icon = Icons.Filled.RemoveCircle,
                                gradient = colors.removeGradient,
                                toneColor = colors.removeCoral,
                                primary = true,
                            ) {
                                AppModel.removeReinforcement(point.id)
                                next()
                            }
                            MasteredButton(point)
                        }
                        ReviewMode.MASTERED -> {
                            ReviewActionButton(
                                text = addFocusText,
                                icon = Icons.Filled.AddCircle,
                                gradient = colors.actionGradient,
                                toneColor = colors.sky,
                                primary = true,
                            ) { AppModel.addReinforcement(point.id) }
                            ReviewActionButton(
                                text = "移出已掌握",
                                icon = Icons.Filled.RemoveCircle,
                                gradient = colors.removeGradient,
                                toneColor = colors.removeCoral,
                                primary = true,
                            ) {
                                AppModel.removeMastered(point.id)
                                next()
                            }
                        }
                    }
                    ReviewActionButton(
                        text = "下一个",
                        icon = Icons.Filled.Shuffle,
                        gradient = colors.nextGradient,
                        toneColor = colors.nextAmber,
                        primary = true,
                    ) { next() }
                }
            }
        }
    }
}

@Composable
private fun MasteredButton(point: KnowledgePoint) {
    val colors = kikariaColors()
    if (point.isMastered) {
        ReviewActionButton(
            text = "已设定为掌握",
            icon = Icons.Filled.CheckCircle,
            gradient = colors.masteredActionGradient,
            toneColor = colors.softText,
            primary = false,
            enabled = false,
        ) { }
    } else {
        ReviewActionButton(
            text = "加入已掌握",
            icon = Icons.Filled.AddCircle,
            gradient = colors.masteredActionGradient,
            toneColor = colors.masteredGreen,
            primary = true,
        ) { AppModel.markMastered(point.id) }
    }
}

/** 提示/答案卡:标题 + 数学混排正文。 */
@Composable
fun FloatingInfoCard(title: String, text: String, fontSize: Int) {
    val colors = kikariaColors()
    GlassCard(cornerRadius = 26, fillAlpha = 0.56f, modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.fillMaxWidth().padding(20.dp)) {
            Text(title, color = colors.sky, fontSize = 14.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(8.dp))
            KikariaMathText(
                text = text,
                fontSize = fontSize,
                textColor = colors.deepText,
                accentColor = colors.sky,
            )
        }
    }
}
