package com.vita0818.kikaria.ui.overview

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
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.StudyActivityRecord
import com.vita0818.kikaria.data.StudyActivityType
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale

/**
 * Review History screen with a calendar grid showing study activity per day.
 * Translated from the iOS ReviewHistoryView in ContentView.swift.
 */
@Composable
fun ReviewHistoryScreen(
    activityRecords: List<StudyActivityRecord>,
    onBack: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    var visibleMonth by remember { mutableStateOf(Calendar.getInstance().time) }
    var selectedDate by remember { mutableStateOf(Date()) }

    val calendar = Calendar.getInstance()

    // Month title
    fun monthTitle(): String {
        val cal = Calendar.getInstance().apply { time = visibleMonth }
        val year = cal.get(Calendar.YEAR)
        val month = cal.get(Calendar.MONTH) + 1
        return "${year}年 ${month}月"
    }

    // Month cells
    fun monthCells(): List<Date?> {
        val cal = Calendar.getInstance().apply {
            time = visibleMonth
            set(Calendar.DAY_OF_MONTH, 1)
        }
        val firstWeekday = cal.get(Calendar.DAY_OF_WEEK) // Sunday=1, Monday=2, ...
        val leadingBlankCount = if (firstWeekday == Calendar.SUNDAY) 6 else firstWeekday - 2
        val daysInMonth = cal.getActualMaximum(Calendar.DAY_OF_MONTH)

        val cells = mutableListOf<Date?>()
        repeat(leadingBlankCount) { cells.add(null) }
        for (day in 1..daysInMonth) {
            cal.set(Calendar.DAY_OF_MONTH, day)
            cells.add(cal.time)
        }
        while (cells.size % 7 != 0) {
            cells.add(null)
        }
        return cells
    }

    fun changeMonth(by: Int) {
        val cal = Calendar.getInstance().apply { time = visibleMonth }
        cal.add(Calendar.MONTH, by)
        visibleMonth = cal.time
    }

    fun recordCount(onDate: Date): Int {
        val cal = Calendar.getInstance()
        return activityRecords.count { record ->
            cal.time = record.date
            val sameDay = cal.get(Calendar.YEAR) == Calendar.getInstance().apply { time = onDate }.get(Calendar.YEAR) &&
                    cal.get(Calendar.DAY_OF_YEAR) == Calendar.getInstance().apply { time = onDate }.get(Calendar.DAY_OF_YEAR)
            sameDay
        }
    }

    fun records(onDate: Date): List<StudyActivityRecord> {
        val cal = Calendar.getInstance()
        return activityRecords.filter { record ->
            cal.time = record.date
            val sameDay = cal.get(Calendar.YEAR) == Calendar.getInstance().apply { time = onDate }.get(Calendar.YEAR) &&
                    cal.get(Calendar.DAY_OF_YEAR) == Calendar.getInstance().apply { time = onDate }.get(Calendar.DAY_OF_YEAR)
            sameDay
        }
    }

    fun summary(onDate: Date): Map<String, Int> {
        val recs = records(onDate)
        return mapOf(
            "查看提示" to recs.count { it.type == StudyActivityType.VIEWED_HINT },
            "查看答案" to recs.count { it.type == StudyActivityType.REVIEWED_ANSWER },
            "新增掌握" to recs.count { it.type == StudyActivityType.MARKED_MASTERED },
            "加入重点" to recs.count { it.type == StudyActivityType.ADDED_REINFORCEMENT }
        )
    }

    val weekdaySymbols = listOf("一", "二", "三", "四", "五", "六", "日")

    KikariaPageShell {
        Box(modifier = Modifier.fillMaxSize()) {
            KikariaBackButton(onClick = onBack)

            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 24.dp)
                    .padding(top = 70.dp)
            ) {
                Text(
                    text = KikariaTypography.mixedText("复习历史", size = 32, weight = FontWeight.Bold),
                    color = deepText
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Calendar card
                KikariaGlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 30.dp,
                    fillOpacity = 0.44f
                ) {
                    Column(modifier = Modifier.padding(18.dp)) {
                        // Month navigation
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Box(
                                modifier = Modifier
                                    .clip(RoundedCornerShape(12.dp))
                                    .background(
                                        (if (isDark) KikariaColors.GlassSurfaceDark
                                        else KikariaColors.GlassSurface).copy(alpha = 0.36f)
                                    )
                                    .clickable { changeMonth(-1) }
                                    .padding(12.dp),
                                contentAlignment = Alignment.Center
                            ) {
                                Text("‹", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = sky)
                            }

                            Text(
                                text = KikariaTypography.mixedText(
                                    monthTitle(),
                                    size = 20,
                                    weight = FontWeight.SemiBold
                                ),
                                color = deepText
                            )

                            Box(
                                modifier = Modifier
                                    .clip(RoundedCornerShape(12.dp))
                                    .background(
                                        (if (isDark) KikariaColors.GlassSurfaceDark
                                        else KikariaColors.GlassSurface).copy(alpha = 0.36f)
                                    )
                                    .clickable { changeMonth(1) }
                                    .padding(12.dp),
                                contentAlignment = Alignment.Center
                            ) {
                                Text("›", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = sky)
                            }
                        }

                        Spacer(modifier = Modifier.height(12.dp))

                        // Weekday headers
                        Row(modifier = Modifier.fillMaxWidth()) {
                            weekdaySymbols.forEach { symbol ->
                                Text(
                                    text = symbol,
                                    fontSize = 12.sp,
                                    fontWeight = FontWeight.SemiBold,
                                    color = softText,
                                    textAlign = TextAlign.Center,
                                    modifier = Modifier.weight(1f)
                                )
                            }
                        }

                        Spacer(modifier = Modifier.height(8.dp))

                        // Calendar grid
                        val cells = monthCells()
                        val today = Calendar.getInstance().time
                        cells.chunked(7).forEach { row ->
                            Row(modifier = Modifier.fillMaxWidth()) {
                                row.forEach { date ->
                                    val count = date?.let { recordCount(it) } ?: 0
                                    val isToday = date != null && Calendar.getInstance().let { cal ->
                                        cal.time = date
                                        val calToday = Calendar.getInstance()
                                        cal.get(Calendar.DAY_OF_YEAR) == calToday.get(Calendar.DAY_OF_YEAR) &&
                                                cal.get(Calendar.YEAR) == calToday.get(Calendar.YEAR)
                                    }
                                    val isSelected = date != null && Calendar.getInstance().let { cal ->
                                        cal.time = date
                                        val calSel = Calendar.getInstance().apply { time = selectedDate }
                                        cal.get(Calendar.DAY_OF_YEAR) == calSel.get(Calendar.DAY_OF_YEAR) &&
                                                cal.get(Calendar.YEAR) == calSel.get(Calendar.YEAR)
                                    }

                                    val fillColor = if (date == null) Color.Transparent
                                    else when {
                                        count == 0 -> Color.White.copy(alpha = if (isDark) 0.08f else 0.42f)
                                        count in 1..2 -> (if (isDark) KikariaColors.CyanDark else KikariaColors.Cyan)
                                            .copy(alpha = 0.42f)
                                        count in 3..5 -> sky.copy(alpha = 0.54f)
                                        else -> (if (isDark) KikariaColors.MasteredGreenDark
                                        else KikariaColors.MasteredGreen).copy(alpha = 0.62f)
                                    }

                                    val borderColor = when {
                                        isSelected -> deepText.copy(alpha = 0.45f)
                                        isToday -> sky.copy(alpha = 0.65f)
                                        else -> Color.Transparent
                                    }

                                    Box(
                                        modifier = Modifier
                                            .weight(1f)
                                            .padding(2.dp)
                                            .height(38.dp)
                                            .clip(RoundedCornerShape(12.dp))
                                            .background(fillColor)
                                            .then(
                                                if (borderColor != Color.Transparent) {
                                                    Modifier.shadow(
                                                        1.dp, RoundedCornerShape(12.dp),
                                                        spotColor = borderColor
                                                    )
                                                } else Modifier
                                            )
                                            .then(
                                                if (date != null) Modifier.clickable { selectedDate = date }
                                                else Modifier
                                            ),
                                        contentAlignment = Alignment.Center
                                    ) {
                                        if (date != null) {
                                            val dayNum = Calendar.getInstance().apply { time = date }
                                                .get(Calendar.DAY_OF_MONTH)
                                            Text(
                                                text = "$dayNum",
                                                fontSize = 12.sp,
                                                fontWeight = FontWeight.SemiBold,
                                                color = deepText.copy(
                                                    alpha = if (count == 0) 0.58f else 0.86f
                                                )
                                            )
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Spacer(modifier = Modifier.height(12.dp))

                // Selected day summary
                val dateSummary = summary(selectedDate)
                val cal = Calendar.getInstance().apply { time = selectedDate }
                val titleText = "${cal.get(Calendar.MONTH) + 1}月${cal.get(Calendar.DAY_OF_MONTH)}日"
                val totalRecords = dateSummary.values.sum()

                KikariaGlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 28.dp,
                    fillOpacity = 0.44f
                ) {
                    Column(modifier = Modifier.padding(20.dp)) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = KikariaTypography.mixedText(
                                    titleText,
                                    size = 19,
                                    weight = FontWeight.SemiBold
                                ),
                                color = deepText
                            )
                            Text(
                                text = "$totalRecords 条记录",
                                fontSize = 12.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = softText,
                                modifier = Modifier
                                    .clip(RoundedCornerShape(12.dp))
                                    .background(
                                        (if (isDark) KikariaColors.GlassSurfaceDark
                                        else KikariaColors.GlassSurface).copy(alpha = 0.36f)
                                    )
                                    .padding(horizontal = 11.dp, vertical = 6.dp)
                            )
                        }

                        Spacer(modifier = Modifier.height(14.dp))

                        if (totalRecords == 0) {
                            Text(
                                text = "这一天还没有学习记录。",
                                fontSize = 15.sp,
                                fontWeight = FontWeight.Medium,
                                color = softText
                            )
                        } else {
                            dateSummary.entries.forEach { (title, count) ->
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(vertical = 4.dp),
                                    horizontalArrangement = Arrangement.SpaceBetween
                                ) {
                                    Text(
                                        text = title,
                                        fontSize = 15.sp,
                                        fontWeight = FontWeight.Medium,
                                        color = deepText
                                    )
                                    Text(
                                        text = KikariaTypography.mixedText(
                                            "$count",
                                            size = 17,
                                            weight = FontWeight.Bold
                                        ),
                                        color = sky
                                    )
                                }
                            }
                        }
                    }
                }

                Spacer(modifier = Modifier.height(32.dp))
            }
        }
    }
}
