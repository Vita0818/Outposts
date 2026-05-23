package com.vita0818.kikaria.viewmodel

import android.app.Application
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.AndroidViewModel
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.data.KnowledgePreset
import com.vita0818.kikaria.data.SamplePresets
import com.vita0818.kikaria.data.StudyActivityRecord
import com.vita0818.kikaria.data.StudyActivityType
import com.vita0818.kikaria.util.KikariaPersistence
import com.vita0818.kikaria.util.MarkdownParser
import java.util.Calendar
import java.util.Date
import java.util.UUID

/**
 * Central ViewModel for the Kikaria Android app.
 *
 * Manages:
 * - active preset and its knowledge points
 * - selected tags for scoped review
 * - review queue and state
 * - reinforcement and mastered collections
 * - study activity records
 * - persistence via KikariaPersistence
 *
 * Translated from the PresetStudyState + AppState logic in ContentView.swift.
 */
class KikariaViewModel(application: Application) : AndroidViewModel(application) {

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
    var dailyGoal by mutableIntStateOf(20)

    // --- Activity records ---
    var activityRecords = mutableStateListOf<StudyActivityRecord>()

    // --- UI state ---
    var toastMessage by mutableStateOf<String?>(null)
    var userDisplayName by mutableStateOf("")
    var userHandle by mutableStateOf("user")
    var countdownStartDate by mutableStateOf<Date?>(null)
    var countdownEndDate by mutableStateOf<Date?>(null)
    var dangerPercent by mutableIntStateOf(80)
    var notificationsEnabled by mutableStateOf(false)
    var notificationTimeText by mutableStateOf("21:00")

    // --- Computed countdown ---
    val countdownDays: Int
        get() {
            val end = countdownEndDate ?: return 0
            val calendar = Calendar.getInstance()
            val today = calendar.apply {
                set(Calendar.HOUR_OF_DAY, 0)
                set(Calendar.MINUTE, 0)
                set(Calendar.SECOND, 0)
                set(Calendar.MILLISECOND, 0)
            }.time
            val target = Calendar.getInstance().apply {
                time = end
                set(Calendar.HOUR_OF_DAY, 0)
                set(Calendar.MINUTE, 0)
                set(Calendar.SECOND, 0)
                set(Calendar.MILLISECOND, 0)
            }.time
            val diffMs = target.time - today.time
            val days = (diffMs / (1000 * 60 * 60 * 24)).toInt()
            return maxOf(0, days)
        }

    // --- Derived ---
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

    init {
        loadState()
    }

    // --- Persistence ---

    private fun loadState() {
        val state = KikariaPersistence.load(getApplication())
        if (state != null && state.presets.isNotEmpty()) {
            presets.clear()
            presets.addAll(state.presets)
            activePresetId = state.currentPresetId.ifEmpty { presets.firstOrNull()?.id ?: KnowledgePreset.DEFAULT_PRESET_ID }
            userDisplayName = state.userDisplayName
            userHandle = state.userHandle.ifEmpty { "user" }
            dailyGoal = state.dailyGoal.coerceIn(1, 100)
            countdownEndDate = state.countdownEndDate
            dangerPercent = state.dangerPercent.coerceIn(1, 100)
            notificationsEnabled = state.notificationsEnabled
            notificationTimeText = state.notificationTimeText.ifEmpty { "21:00" }
            loadPresetKnowledgePoints()
        } else {
            loadInitialPresets()
        }
    }

    fun saveState() {
        KikariaPersistence.save(
            getApplication(),
            KikariaPersistence.AppState(
                presets = presets.toList(),
                currentPresetId = activePresetId,
                userDisplayName = userDisplayName,
                userHandle = userHandle,
                dailyGoal = dailyGoal,
                countdownEndDate = countdownEndDate,
                dangerPercent = dangerPercent,
                notificationsEnabled = notificationsEnabled,
                notificationTimeText = notificationTimeText
            )
        )
    }

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
        saveState()
    }

    // --- Toast ---

    fun showToast(message: String) {
        toastMessage = message
    }

    fun clearToast() {
        toastMessage = null
    }

    // --- Settings ---

    fun setDailyGoal(goal: Int) {
        dailyGoal = goal.coerceIn(1, 100)
        saveState()
    }

    fun setCountdownRange(startDate: Date?, endDate: Date?) {
        countdownStartDate = startDate
        countdownEndDate = endDate
        saveState()
    }

    fun setDangerPercent(percent: Int) {
        dangerPercent = percent.coerceIn(1, 100)
        saveState()
    }

    fun setNotificationsEnabled(enabled: Boolean) {
        notificationsEnabled = enabled
        saveState()
    }

    fun setNotificationTime(timeText: String) {
        notificationTimeText = timeText
        saveState()
    }

    fun updateProfile(displayName: String, handle: String) {
        userDisplayName = displayName.trim()
        userHandle = handle.trim().trimStart('@')
        saveState()
    }

    // --- Preset Management ---

    fun createPreset(name: String, category: String, markdownText: String): KnowledgePreset {
        val trimmedName = name.trim()
        val trimmedCategory = category.trim().ifEmpty { "自定义" }
        val preset = KnowledgePreset(
            id = "user-${UUID.randomUUID()}",
            name = trimmedName.ifEmpty { "新预设" },
            subtitle = "自定义知识点",
            category = trimmedCategory,
            markdownText = markdownText,
            isBuiltIn = false
        )
        presets.add(preset)
        switchPreset(preset.id)
        showToast("已创建「${preset.name}」")
        return preset
    }

    fun deletePreset(presetId: String) {
        val preset = presets.find { it.id == presetId } ?: return
        if (preset.isBuiltIn) return
        if (presets.size <= 1) {
            showToast("无法删除最后一个预设")
            return
        }
        presets.remove(preset)
        if (activePresetId == presetId) {
            activePresetId = presets.first().id
            loadPresetKnowledgePoints()
            selectedTags.clear()
            resetTodayCounts()
        }
        saveState()
        showToast("已删除「${preset.name}」")
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
}

enum class ReviewMode {
    NORMAL,
    REINFORCEMENT,
    MASTERED
}
