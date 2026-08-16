package com.vita0818.kikaria.ui.pages

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Lightbulb
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import com.vita0818.kikaria.AppModel
import com.vita0818.kikaria.Routes
import com.vita0818.kikaria.data.AppStore
import com.vita0818.kikaria.data.StudyActivityType
import com.vita0818.kikaria.data.StudyLogic
import com.vita0818.kikaria.ui.GlassCard
import com.vita0818.kikaria.ui.GlassIconButton
import com.vita0818.kikaria.ui.PageHeader
import com.vita0818.kikaria.ui.theme.kikariaColors
import java.util.Calendar

/** 今日概览:今日新增掌握 Hero 卡 + 2x2 指标 + 复习历史入口。 */
@Composable
fun TodayOverviewPage(navController: NavController) {
    val colors = kikariaColors()
    val todayMastered = AppModel.todayMasteredCount
    val goal = AppModel.dailyGoal

    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp),
    ) {
        PageHeader(title = "今日概览", onBack = { navController.popBackStack() })
        Text(
            AppModel.currentPreset?.name ?: "",
            color = colors.softText,
            fontSize = 15.sp,
            fontWeight = FontWeight.Medium,
        )
        Spacer(Modifier.height(16.dp))

        GlassCard(cornerRadius = 30, fillAlpha = 0.5f, modifier = Modifier.fillMaxWidth()) {
            Column(Modifier.fillMaxWidth().padding(24.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                Text("今日新增已掌握", color = colors.softText, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.height(10.dp))
                Row(verticalAlignment = Alignment.Bottom) {
                    Text(
                        "$todayMastered",
                        color = colors.masteredDeepGreen,
                        fontSize = 58.sp,
                        fontWeight = FontWeight.Bold,
                        fontFamily = FontFamily.Serif,
                    )
                    Text(
                        " / $goal",
                        color = colors.softText,
                        fontSize = 24.sp,
                        fontWeight = FontWeight.SemiBold,
                    )
                }
                Spacer(Modifier.height(10.dp))
                Text(
                    progressMessage(todayMastered, goal, AppModel.todayReviewedAnswerCount),
                    color = colors.softText,
                    fontSize = 15.sp,
                )
            }
        }
        Spacer(Modifier.height(14.dp))

        val countdown = AppModel.countdownDayCount
        val metrics = listOf(
            "查看答案" to "${AppModel.todayReviewedAnswerCount}",
            "总已掌握" to "${AppModel.masteredCount}",
            "查看提示" to "${AppModel.todayViewedHintCount}",
            "倒数" to (countdown?.let { "$it 天" } ?: "--"),
        )
        Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
            metrics.chunked(2).forEach { row ->
                Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                    row.forEach { (title, value) ->
                        GlassCard(cornerRadius = 24, fillAlpha = 0.44f, modifier = Modifier.weight(1f)) {
                            Column(Modifier.fillMaxWidth().padding(18.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                                Text(title, color = colors.softText, fontSize = 13.sp)
                                Spacer(Modifier.height(6.dp))
                                Text(
                                    value,
                                    color = colors.deepText,
                                    fontSize = if (value.length > 4) 30.sp else 46.sp,
                                    fontWeight = FontWeight.Bold,
                                    fontFamily = FontFamily.Serif,
                                )
                            }
                        }
                    }
                    if (row.size == 1) Spacer(Modifier.weight(1f))
                }
            }
        }
        Spacer(Modifier.height(14.dp))

        GlassCard(
            cornerRadius = 24,
            fillAlpha = 0.44f,
            modifier = Modifier
                .fillMaxWidth()
                .clickable { navController.navigate(Routes.HISTORY) },
        ) {
            Row(Modifier.fillMaxWidth().padding(18.dp), verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Filled.CalendarMonth, contentDescription = null, tint = colors.sky, modifier = Modifier.size(20.dp))
                Spacer(Modifier.size(12.dp))
                Text("复习历史", color = colors.deepText, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = null, tint = colors.blueGray)
            }
        }
        Spacer(Modifier.height(30.dp))
    }
}

/** 复习历史:月历热力 + 当日统计。 */
@Composable
fun ReviewHistoryPage(navController: NavController) {
    val colors = kikariaColors()
    var monthOffset by remember { mutableStateOf(0) }
    var selectedDay by remember { mutableStateOf<Long?>(AppStore.startOfDay(System.currentTimeMillis())) }

    val calendar = Calendar.getInstance()
    calendar.timeInMillis = System.currentTimeMillis()
    calendar.add(Calendar.MONTH, monthOffset)
    calendar.set(Calendar.DAY_OF_MONTH, 1)
    val year = calendar.get(Calendar.YEAR)
    val month = calendar.get(Calendar.MONTH)

    val firstDay = calendar.clone() as Calendar
    val daysInMonth = calendar.getActualMaximum(Calendar.DAY_OF_MONTH)
    val leadingBlanks = (firstDay.get(Calendar.DAY_OF_WEEK) + 5) % 7 // 周一为首

    // 每日 reviewedAnswer 计数
    val perDayCounts by remember(monthOffset, AppModel.activityRecords) {
        mutableStateOf(
            AppModel.activityRecords
                .filter { it.type == StudyActivityType.REVIEWED_ANSWER }
                .groupBy { AppStore.startOfDay(it.date) }
                .mapValues { it.value.size },
        )
    }

    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp),
    ) {
        PageHeader(title = "复习历史", onBack = { navController.popBackStack() })

        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            GlassIconButton(icon = Icons.AutoMirrored.Filled.KeyboardArrowLeft, size = 40) {
                monthOffset -= 1
                selectedDay = null
            }
            Text(
                "${year}年 ${month + 1}月",
                color = colors.deepText,
                fontSize = 20.sp,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.weight(1f),
                textAlign = androidx.compose.ui.text.style.TextAlign.Center,
            )
            GlassIconButton(icon = Icons.AutoMirrored.Filled.KeyboardArrowRight, size = 40) {
                monthOffset += 1
                selectedDay = null
            }
        }
        Spacer(Modifier.height(14.dp))

        val weekSymbols = listOf("一", "二", "三", "四", "五", "六", "日")
        GlassCard(cornerRadius = 24, fillAlpha = 0.44f, modifier = Modifier.fillMaxWidth()) {
            Column(Modifier.padding(14.dp)) {
                Row(Modifier.fillMaxWidth()) {
                    weekSymbols.forEach { symbol ->
                        Text(
                            symbol,
                            color = colors.softText,
                            fontSize = 12.sp,
                            textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                            modifier = Modifier.weight(1f),
                        )
                    }
                }
                Spacer(Modifier.height(8.dp))
                val cells: List<Int?> = List(leadingBlanks) { null } + (1..daysInMonth).toList()
                cells.chunked(7).forEach { week ->
                    Row(Modifier.fillMaxWidth().padding(vertical = 2.dp)) {
                        (0 until 7).forEach { daySlot ->
                            val day = week.getOrNull(daySlot)
                            Box(Modifier.weight(1f), contentAlignment = Alignment.Center) {
                                if (day != null) {
                                    val cal = calendar.clone() as Calendar
                                    cal.set(Calendar.DAY_OF_MONTH, day)
                                    val dayStart = cal.timeInMillis
                                    val count = perDayCounts[dayStart] ?: 0
                                    val isToday = AppStore.startOfDay(System.currentTimeMillis()) == dayStart
                                    val isSelected = selectedDay == dayStart
                                    HistoryCalendarDayCell(
                                        day = day,
                                        count = count,
                                        isToday = isToday,
                                        isSelected = isSelected,
                                    ) { selectedDay = dayStart }
                                }
                            }
                        }
                    }
                }
            }
        }
        Spacer(Modifier.height(14.dp))

        selectedDay?.let { day ->
            val dayRecords = AppModel.activityRecords.filter { AppStore.startOfDay(it.date) == day }
            val cal = Calendar.getInstance().apply { timeInMillis = day }
            GlassCard(cornerRadius = 24, fillAlpha = 0.44f, modifier = Modifier.fillMaxWidth()) {
                Column(Modifier.padding(18.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text(
                            "${cal.get(Calendar.MONTH) + 1}月${cal.get(Calendar.DAY_OF_MONTH)}日",
                            color = colors.deepText,
                            fontSize = 18.sp,
                            fontWeight = FontWeight.SemiBold,
                        )
                        Spacer(Modifier.weight(1f))
                        Text(
                            "${dayRecords.size} 条记录",
                            color = colors.softText,
                            fontSize = 13.sp,
                            fontWeight = FontWeight.SemiBold,
                        )
                    }
                    Spacer(Modifier.height(12.dp))
                    if (dayRecords.isEmpty()) {
                        Text("这一天还没有学习记录。", color = colors.softText, fontSize = 15.sp)
                    } else {
                        HistorySummaryRow("查看提示", dayRecords.count { it.type == StudyActivityType.VIEWED_HINT })
                        HistorySummaryRow("查看答案", dayRecords.count { it.type == StudyActivityType.REVIEWED_ANSWER })
                        HistorySummaryRow("新增掌握", dayRecords.count { it.type == StudyActivityType.MARKED_MASTERED })
                        HistorySummaryRow("加入重点", dayRecords.count { it.type == StudyActivityType.ADDED_REINFORCEMENT })
                    }
                }
            }
        }
        Spacer(Modifier.height(30.dp))
    }
}

@Composable
private fun HistoryCalendarDayCell(
    day: Int,
    count: Int,
    isToday: Boolean,
    isSelected: Boolean,
    onClick: () -> Unit,
) {
    val colors = kikariaColors()
    val dark = androidx.compose.foundation.isSystemInDarkTheme()
    val fill = when {
        count >= 6 -> colors.masteredGreen.copy(alpha = 0.62f)
        count >= 3 -> colors.sky.copy(alpha = 0.54f)
        count >= 1 -> colors.cyan.copy(alpha = 0.42f)
        else -> if (dark) Color.White.copy(alpha = 0.06f) else Color.White.copy(alpha = 0.42f)
    }
    val shape = RoundedCornerShape(12.dp)
    val border = when {
        isSelected -> androidx.compose.foundation.BorderStroke(2.dp, colors.deepText.copy(alpha = 0.45f))
        isToday -> androidx.compose.foundation.BorderStroke(1.4.dp, colors.sky.copy(alpha = 0.65f))
        else -> null
    }
    Box(
        Modifier
            .height(38.dp)
            .padding(2.dp)
            .clip(shape)
            .background(fill)
            .then(
                if (border != null) Modifier.border(border, shape) else Modifier,
            )
            .clickable { onClick() },
        contentAlignment = Alignment.Center,
    ) {
        Text(
            "$day",
            color = colors.deepText,
            fontSize = 12.sp,
            fontFamily = FontFamily.Serif,
        )
    }
}

@Composable
private fun HistorySummaryRow(title: String, count: Int) {
    val colors = kikariaColors()
    Row(Modifier.fillMaxWidth().padding(vertical = 6.dp)) {
        Text(title, color = colors.softText, fontSize = 15.sp, modifier = Modifier.weight(1f))
        Text("$count", color = colors.sky, fontSize = 17.sp, fontWeight = FontWeight.Bold)
    }
}
