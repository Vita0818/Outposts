package com.vita0818.kikaria.viewmodel

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.data.KnowledgePreset
import com.vita0818.kikaria.data.SamplePresets
import com.vita0818.kikaria.data.StudyActivityRecord
import com.vita0818.kikaria.data.StudyActivityType
import com.vita0818.kikaria.util.MarkdownParser
import java.util.Calendar
import java.util.Date

/**
 * Central ViewModel for the Kikaria Android app.
 *
 * Manages:
 * - active preset and its knowledge points
 * - selected tags for scoped review
 * - review queue and state
 * - reinforcement and mastered collections
 * - study activity records
 * - countdown date for study deadline tracking
 * - study progress warning evaluation
 *
 * Translated from the PresetStudyState + KikariaAppState logic in ContentView.swift.
 */
class KikariaViewModel : ViewModel() {

    // --- Presets ---
    var presets = mutableStateListOf<KnowledgePreset>()
    var activePresetId by mutableStateOf(KnowledgePreset.DEFAULT_PRESET_ID)

    val activePreset: KnowledgePreset?
        get() = presets.find { it.id == activePresetId }

    // --- Knowledge Points (parsed from active preset markdown) ---
    var knowledgePoints = mutableStateListOf<KnowledgePoint>()

    // --- Tag Selection ---
    var selectedTags = mutableStateListOf<String>()
    val allTags: List<String>
        get() = knowledgePoints.flatMap { it.tags }.distinct().sorted()

    // --- Review State ---
    var reviewQueue = mutableStateListOf<KnowledgePoint>()
    var currentReviewIndex by mutableIntStateOf(-1)
    var isHintShown by mutableStateOf(false)
    var isContentShown by mutableStateOf(false)
    var reviewMode by mutableStateOf(ReviewMode.NORMAL)

    // --- Today Progress ---
    var todayReviewCount by mutableIntStateOf(0)
    var todayHintCount by mutableIntStateOf(0)
    var todayMasteredCount by mutableIntStateOf(0)

    /** Daily study goal: number of points to review per day. Default 20, matching iOS. */
    var dailyGoal by mutableIntStateOf(20)

    // --- Countdown (study deadline) ---
    /**
     * Optional countdown end date for study deadline tracking.
     * When set, the home screen shows "X Days Left".
     * Translated from iOS `PresetStudyState.countdownEndDate`.
     */
    var countdownEndDate: Date? by mutableStateOf(null)

    // --- Danger threshold ---
    /**
     * Percentage threshold for study progress warnings.
     * When masteredCount / expectedMasteredCount * 100 < dangerPercent,
     * a notification warning is triggered (when notifications are enabled).
     * Range: 1-100, default 80. Translated from iOS `PresetStudyState.dangerPercent`.
     */
    var dangerPercent by mutableIntStateOf(80)

    // --- Notifications (placeholder for future implementation) ---
    /** Whether study progress notifications are enabled. Placeholder for future notification support. */
    var notificationsEnabled by mutableStateOf(false)

    // --- Activity records ---
    var activityRecords = mutableStateListOf<StudyActivityRecord>()

    // --- UI state ---
    var toastMessage by mutableStateOf<String?>(null)

    // --- Derived: Countdown ---

    /**
     * Number of days remaining until countdownEndDate.
     * Returns null if no countdown is set.
     * Translated from iOS `countdownDays(until:)`.
     */
    val countdownDays: Int?
        get() {
            val targetDate = countdownEndDate ?: return null
            val calendar = Calendar.getInstance()
            val today = calendar.apply {
                set(Calendar.HOUR_OF_DAY, 0)
                set(Calendar.MINUTE, 0)
                set(Calendar.SECOND, 0)
                set(Calendar.MILLISECOND, 0)
            }.time
            val target = Calendar.getInstance().apply {
                time = targetDate
                set(Calendar.HOUR_OF_DAY, 0)
                set(Calendar.MINUTE, 0)
                set(Calendar.SECOND, 0)
                set(Calendar.MILLISECOND, 0)
            }.time
            val diffMillis = target.time - today.time
            val days = (diffMillis / (1000 * 60 * 60 * 24)).toInt()
            return maxOf(0, days)
        }

    /**
     * Human-readable countdown text.
     * Returns "--" if no countdown is set, otherwise "X 天".
     * Translated from iOS `countdownText(for:)`.
     */
    val countdownText: String
        get() {
            val days = countdownDays ?: return "--"
            return "$days 天"
        }

    // --- Derived: Collections ---

    val masteredPoints: List<KnowledgePoint>
        get() = knowledgePoints.filter { it.isMastered }

    val reinforcedPoints: List<KnowledgePoint>
        get() = knowledgePoints
            .filter { it.reinforcementCount > 0 }
            .sortedWith(
                compareByDescending<KnowledgePoint> { it.reinforcementCount }
                    .thenByDescending { it.lastReinforcedAt }
                    .thenBy { it.title }
            )

    val currentPoint: KnowledgePoint?
        get() = if (currentReviewIndex in reviewQueue.indices) reviewQueue[currentReviewIndex] else null

    val reviewProgress: Float
        get() = if (reviewQueue.isEmpty()) 0f
        else (currentReviewIndex + 1).toFloat() / reviewQueue.size.toFloat()

    val hasNextPoint: Boolean
        get() = currentReviewIndex < reviewQueue.size - 1

    val hasPreviousPoint: Boolean
        get() = currentReviewIndex > 0

    val selectedKnowledgePoints: List<KnowledgePoint>
        get() {
            if (selectedTags.isEmpty()) return knowledgePoints.toList()
            return knowledgePoints.filter { point ->
                point.tags.any { it in selectedTags }
            }
        }

    // --- Derived: Study Progress Warning ---

    /**
     * Study progress warning data, translated from iOS `StudyProgressWarning`.
     * Used to determine if the user is falling behind on their study goals.
     */
    data class StudyProgressWarning(
        val masteredCount: Int,
        val expectedMasteredCount: Int,
        val dangerPercent: Int,
        val remainingDays: Int?
    ) {
        /** Whether the warning is currently active. */
        val isActive: Boolean
            get() {
                if (masteredCount >= expectedMasteredCount) return false
                if (expectedMasteredCount <= 0) return false
                val actualPercent = masteredCount * 100 / expectedMasteredCount
                return actualPercent < dangerPercent
            }

        fun body(presetName: String): String {
            return "今天的「$presetName」学习量尚未达标哦，抓紧学习吧！"
        }
    }

    /**
     * Evaluates the current study progress warning.
     * Returns null if no warning is needed.
     * Translated from iOS `evaluateStudyProgressWarning(for:)`.
     */
    val studyProgressWarning: StudyProgressWarning?
        get() {
            val mastered = todayMasteredCount
            val goal = dailyGoal
            if (mastered >= goal) return null
            if (goal <= 0) return null
            return StudyProgressWarning(
                masteredCount = mastered,
                expectedMasteredCount = goal,
                dangerPercent = dangerPercent,
                remainingDays = countdownDays
            )
        }

    init {
        loadInitialPresets()
    }

    // --- Initialization ---

    private fun loadInitialPresets() {
        presets.clear()
        presets.addAll(SamplePresets.all)
        loadPresetKnowledgePoints()
    }

    fun loadPresetKnowledgePoints() {
        val preset = activePreset ?: return
        knowledgePoints.clear()
        val parsed = MarkdownParser.parseKnowledgePoints(preset.markdownText)
        knowledgePoints.addAll(parsed)
    }

    fun switchPreset(presetId: String) {
        activePresetId = presetId
        loadPresetKnowledgePoints()
        selectedTags.clear()
        resetTodayCounts()
    }

    // --- Review ---

    fun startReview(mode: ReviewMode = ReviewMode.NORMAL) {
        reviewMode = mode
        val pool = when (mode) {
            ReviewMode.NORMAL -> selectedKnowledgePoints
            ReviewMode.REINFORCEMENT -> reinforcedPoints
            ReviewMode.MASTERED -> masteredPoints
        }

        if (pool.isEmpty()) {
            toastMessage = when (mode) {
                ReviewMode.NORMAL -> "没有可复习的知识点"
                ReviewMode.REINFORCEMENT -> "重点集锦为空"
                ReviewMode.MASTERED -> "已掌握列表为空"
            }
            return
        }

        reviewQueue.clear()
        reviewQueue.addAll(pool.shuffled())
        currentReviewIndex = 0
        isHintShown = false
        isContentShown = false
    }

    fun showHint() {
        val point = currentPoint ?: return
        isHintShown = true
        todayHintCount++
        recordActivity(StudyActivityType.VIEWED_HINT, point)
    }

    fun showContent() {
        val point = currentPoint ?: return
        isContentShown = true
        todayReviewCount++
        recordActivity(StudyActivityType.REVIEWED_ANSWER, point)
    }

    fun nextPoint() {
        if (hasNextPoint) {
            currentReviewIndex++
            isHintShown = false
            isContentShown = false
        }
    }

    fun previousPoint() {
        if (hasPreviousPoint) {
            currentReviewIndex--
            isHintShown = false
            isContentShown = false
        }
    }

    // --- Reinforcement ---

    fun toggleReinforcement(point: KnowledgePoint? = null) {
        val target = point ?: currentPoint ?: return
        val index = knowledgePoints.indexOfFirst { it.id == target.id }
        if (index == -1) return

        if (target.isReinforced) {
            knowledgePoints[index] = target.clearReinforcement()
            recordActivity(StudyActivityType.REMOVED_REINFORCEMENT, target)
            toastMessage = "已移出重点集锦"
        } else {
            val updated = target.addReinforcement()
            knowledgePoints[index] = updated
            recordActivity(StudyActivityType.ADDED_REINFORCEMENT, target)
            toastMessage = "已加入重点集锦 (第${updated.reinforcementCount}次)"
        }

        // Sync review queue
        syncReviewQueue(index, knowledgePoints[index])
    }

    // --- Mastered ---

    fun toggleMastered(point: KnowledgePoint? = null) {
        val target = point ?: currentPoint ?: return
        val index = knowledgePoints.indexOfFirst { it.id == target.id }
        if (index == -1) return

        if (target.isMastered) {
            knowledgePoints[index] = target.unmarkMastered()
            recordActivity(StudyActivityType.REMOVED_MASTERED, target)
            toastMessage = "已移出已掌握"
        } else {
            knowledgePoints[index] = target.markMastered()
            todayMasteredCount++
            recordActivity(StudyActivityType.MARKED_MASTERED, target)
            toastMessage = "已标记为掌握"
        }

        syncReviewQueue(index, knowledgePoints[index])
    }

    private fun syncReviewQueue(index: Int, updated: KnowledgePoint) {
        val queueIndex = reviewQueue.indexOfFirst { it.id == updated.id }
        if (queueIndex != -1) {
            reviewQueue[queueIndex] = updated
        }
    }

    // --- Activity Records ---

    private fun recordActivity(type: StudyActivityType, point: KnowledgePoint) {
        activityRecords.add(
            StudyActivityRecord(
                presetId = activePresetId,
                type = type,
                pointId = point.id,
                pointTitle = point.title
            )
        )
    }

    // --- Reset ---

    private fun resetTodayCounts() {
        todayReviewCount = 0
        todayHintCount = 0
        todayMasteredCount = 0
    }

    fun clearToast() {
        toastMessage = null
    }
}

enum class ReviewMode {
    NORMAL,
    REINFORCEMENT,
    MASTERED
}
