package com.vita0818.kikaria.data

import java.util.Date

/**
 * Per-preset study state, translated from PresetStudyState in ContentView.swift.
 *
 * Each preset maintains independent:
 * - knowledge points (parsed from markdown)
 * - tag selection for scoped review
 * - daily review records and activity records
 * - daily goal (1–100, clamped)
 * - countdown start/end dates
 * - notification settings
 * - danger percent (1–100, clamped)
 */
data class PresetStudyState(
    val presetId: String,
    val knowledgePoints: List<KnowledgePoint> = emptyList(),
    val markdownText: String = "",
    val selectedTags: Set<String> = emptySet(),
    val dailyReviewRecords: Map<String, DailyReviewRecord> = emptyMap(),
    val activityRecords: List<StudyActivityRecord> = emptyList(),
    val dailyGoal: Int = 20,
    val countdownStartDate: Date? = null,
    val countdownEndDate: Date? = null,
    val notificationsEnabled: Boolean = false,
    val notificationTime: Date = defaultNotificationTime(),
    val dangerPercent: Int = 80
) {
    companion object {
        fun defaultNotificationTime(): Date {
            val cal = java.util.Calendar.getInstance()
            cal.set(java.util.Calendar.HOUR_OF_DAY, 21)
            cal.set(java.util.Calendar.MINUTE, 0)
            cal.set(java.util.Calendar.SECOND, 0)
            cal.set(java.util.Calendar.MILLISECOND, 0)
            return cal.time
        }

        fun clampedDailyGoal(value: Int): Int = value.coerceIn(1, 100)

        fun clampedDangerPercent(value: Int): Int = value.coerceIn(1, 100)
    }
}

/**
 * Daily review record tracking per-knowledge-point review counts per day,
 * translated from DailyReviewRecord in ContentView.swift.
 */
data class DailyReviewRecord(
    val date: Date,
    val count: Int
)
