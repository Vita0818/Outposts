package com.vita0818.kikaria.data

import kotlin.math.ceil

/**
 * 学习进度判定,对齐 Kikaria-Apple 的 evaluateStudyProgressWarning 与今日计数逻辑。
 */
object StudyLogic {

    fun isSameDay(a: Long, b: Long): Boolean = AppStore.startOfDay(a) == AppStore.startOfDay(b)

    fun todayRecords(records: List<StudyActivityRecord>, today: Long = System.currentTimeMillis()): List<StudyActivityRecord> =
        records.filter { isSameDay(it.date, today) }

    fun todayCountOfType(state: PresetStudyState, type: StudyActivityType): Int =
        todayRecords(state.activityRecords).count { it.type == type }

    /** 今日新增掌握 = 今日 markedMastered 记录去重知识点数。 */
    fun todayMarkedMasteredCount(state: PresetStudyState): Int =
        todayRecords(state.activityRecords).filter { it.type == StudyActivityType.MARKED_MASTERED }.map { it.pointId }.distinct().size

    /** 倒数天数 = startOfDay(end) - startOfDay(today),下限 0。 */
    fun countdownDays(endDate: Long?, today: Long = System.currentTimeMillis()): Int? {
        endDate ?: return null
        return AppStore.daysBetween(today, endDate).coerceAtLeast(0).toInt()
    }

    /**
     * 进度预警:知识点总数>0、起止都设置、start<=end、今天>=start 时,
     * 期望掌握 = ceil(total * elapsedDays/totalDays)(两端含当天,过终点则 1),
     * 实际/期望 < dangerPercent/100 则需要提醒。
     */
    fun evaluateStudyProgressWarning(
        totalCount: Int,
        masteredCount: Int,
        startDate: Long?,
        endDate: Long?,
        dangerPercent: Int,
        today: Long = System.currentTimeMillis(),
    ): Boolean {
        if (totalCount <= 0) return false
        val start = startDate ?: return false
        val end = endDate ?: return false
        if (start > end) return false
        if (today < start) return false

        val totalDays = (AppStore.daysBetween(start, end) + 1).coerceAtLeast(1)
        val elapsed = if (AppStore.startOfDay(today) >= AppStore.startOfDay(end)) {
            totalDays
        } else {
            (AppStore.daysBetween(start, today) + 1).coerceAtLeast(1)
        }
        val progress = elapsed.toDouble() / totalDays.toDouble()
        val expected = ceil(totalCount * progress).toInt().coerceAtLeast(1)
        return masteredCount.toDouble() / expected.toDouble() < dangerPercent.toDouble() / 100.0
    }
}
