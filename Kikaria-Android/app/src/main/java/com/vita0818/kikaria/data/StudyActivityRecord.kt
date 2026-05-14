package com.vita0818.kikaria.data

import java.util.Date
import java.util.UUID

/**
 * Study activity record, translated from StudyTracking.swift.
 * Tracks discrete study actions for today overview and review history.
 */
data class StudyActivityRecord(
    val id: String = UUID.randomUUID().toString(),
    val presetId: String,
    val date: Date = Date(),
    val type: StudyActivityType,
    val pointId: String,
    val pointTitle: String
)

enum class StudyActivityType {
    VIEWED_HINT,
    REVIEWED_ANSWER,
    MARKED_MASTERED,
    REMOVED_MASTERED,
    ADDED_REINFORCEMENT,
    REMOVED_REINFORCEMENT
}
