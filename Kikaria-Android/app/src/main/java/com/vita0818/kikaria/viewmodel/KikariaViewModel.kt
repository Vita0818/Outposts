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
    var isReviewCompleted by mutableStateOf(false)

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
    var hasCompletedOnboarding by mutableStateOf(false)
    var hasCompletedProfileSetup by mutableStateOf(false)
    var avatarUri by mutableStateOf<String?>(null)

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
        com.vita0818.kikaria.util.KikariaNotificationManager.createChannel(getApplication())
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
            countdownStartDate = state.countdownStartDate
            countdownEndDate = state.countdownEndDate
            dangerPercent = state.dangerPercent.coerceIn(1, 100)
            notificationsEnabled = state.notificationsEnabled
            notificationTimeText = state.notificationTimeText.ifEmpty { "21:00" }
            hasCompletedOnboarding = state.hasCompletedOnboarding
            hasCompletedProfileSetup = state.hasCompletedProfileSetup
            avatarUri = state.avatarUri
            selectedTags.clear()
            state.selectedTags.forEach { selectedTags.add(it) }
            activityRecords.clear()
            state.activityRecords.forEach { activityRecords.add(it) }

            val lastDate = state.lastActiveDate
            if (lastDate != null && isSameDay(lastDate, Date())) {
                todayReviewCount = state.todayReviewCount
                todayHintCount = state.todayHintCount
                todayMasteredCount = state.todayMasteredCount
            }

            loadPresetKnowledgePoints(state.knowledgePoints)
        } else {
            loadInitialPresets()
        }
    }

    fun saveState() {
        val lastActive = Date()
        KikariaPersistence.save(
            getApplication(),
            KikariaPersistence.AppState(
                schemaVersion = 2,
                presets = presets.toList(),
                currentPresetId = activePresetId,
                userDisplayName = userDisplayName,
                userHandle = userHandle,
                dailyGoal = dailyGoal,
                countdownStartDate = countdownStartDate,
                countdownEndDate = countdownEndDate,
                dangerPercent = dangerPercent,
                notificationsEnabled = notificationsEnabled,
                notificationTimeText = notificationTimeText,
                hasCompletedOnboarding = hasCompletedOnboarding,
                hasCompletedProfileSetup = hasCompletedProfileSetup,
                avatarUri = avatarUri,
                knowledgePoints = knowledgePoints.toList(),
                activityRecords = activityRecords.toList(),
                selectedTags = selectedTags.toList(),
                todayReviewCount = todayReviewCount,
                todayHintCount = todayHintCount,
                todayMasteredCount = todayMasteredCount,
                lastActiveDate = lastActive
            )
        )
    }

    private fun loadInitialPresets() {
        presets.clear()
        presets.addAll(SamplePresets.all)
        val assetPresets = com.vita0818.kikaria.util.PresetLoader.loadPresets(getApplication())
        val existingIds = presets.map { it.id }.toSet()
        assetPresets.filter { it.id !in existingIds }.forEach { presets.add(it) }
        loadPresetKnowledgePoints(emptyList())
    }

    fun loadPresetKnowledgePoints(savedPoints: List<KnowledgePoint> = emptyList()) {
        val preset = activePreset ?: return
        knowledgePoints.clear()
        val parsed = MarkdownParser.parseKnowledgePoints(preset.markdownText)
        val savedById = savedPoints.associateBy { it.id }
        for (point in parsed) {
            val saved = savedById[point.id]
            if (saved != null) {
                knowledgePoints.add(point.copy(
                    reinforcementCount = saved.reinforcementCount,
                    lastReinforcedAt = saved.lastReinforcedAt,
                    isMastered = saved.isMastered,
                    createdAt = saved.createdAt,
                    updatedAt = saved.updatedAt
                ))
            } else {
                knowledgePoints.add(point)
            }
        }
    }

    private fun isSameDay(d1: Date, d2: Date): Boolean {
        val c1 = Calendar.getInstance().apply { time = d1 }
        val c2 = Calendar.getInstance().apply { time = d2 }
        return c1.get(Calendar.YEAR) == c2.get(Calendar.YEAR) &&
            c1.get(Calendar.DAY_OF_YEAR) == c2.get(Calendar.DAY_OF_YEAR)
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

    fun updateDailyGoal(goal: Int) {
        dailyGoal = goal.coerceIn(1, 100)
        saveState()
    }

    fun setCountdownRange(startDate: Date?, endDate: Date?) {
        countdownStartDate = startDate
        countdownEndDate = endDate
        saveState()
    }

    fun updateDangerPercent(percent: Int) {
        dangerPercent = percent.coerceIn(1, 100)
        saveState()
    }

    fun updateNotificationsEnabled(enabled: Boolean) {
        notificationsEnabled = enabled
        saveState()
        if (enabled) {
            scheduleReminder()
        } else {
            cancelReminder()
        }
    }

    fun setNotificationTime(timeText: String) {
        notificationTimeText = timeText
        saveState()
        if (notificationsEnabled) {
            scheduleReminder()
        }
    }

    private fun scheduleReminder() {
        val parts = notificationTimeText.split(":")
        val hour = parts.firstOrNull()?.toIntOrNull() ?: 21
        val minute = parts.getOrNull(1)?.toIntOrNull() ?: 0
        com.vita0818.kikaria.util.KikariaNotificationManager.scheduleReminder(
            getApplication(), hour, minute
        )
    }

    private fun cancelReminder() {
        com.vita0818.kikaria.util.KikariaNotificationManager.cancelReminder(getApplication())
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

    fun updatePreset(presetId: String, name: String, category: String, markdownText: String) {
        val index = presets.indexOfFirst { it.id == presetId }
        if (index == -1) return
        val preset = presets[index]
        val updated = preset.copy(
            name = name.trim().ifEmpty { preset.name },
            category = category.trim().ifEmpty { preset.category },
            markdownText = markdownText.trim().ifEmpty { preset.markdownText }
        )
        presets[index] = updated
        if (activePresetId == presetId) {
            loadPresetKnowledgePoints()
        }
        saveState()
        showToast("已更新「${updated.name}」")
    }

    fun importPreset(name: String, markdownText: String): KnowledgePreset {
        val category = "导入"
        return createPreset(name, category, markdownText)
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
            activePresetId = presets.firstOrNull()?.id ?: KnowledgePreset.DEFAULT_PRESET_ID
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
        isReviewCompleted = false
    }

    fun showHint() {
        val point = currentPoint ?: return
        isHintShown = true
        todayHintCount++
        recordActivity(StudyActivityType.VIEWED_HINT, point)
        saveState()
    }

    fun showContent() {
        val point = currentPoint ?: return
        isContentShown = true
        todayReviewCount++
        recordActivity(StudyActivityType.REVIEWED_ANSWER, point)
        saveState()
    }

    fun nextPoint() {
        if (hasNextPoint) {
            currentReviewIndex++
            isHintShown = false
            isContentShown = false
        } else if (reviewMode != ReviewMode.NORMAL) {
            isReviewCompleted = true
            currentReviewIndex = -1
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

        syncReviewQueue(knowledgePoints[index])
        saveState()
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

        syncReviewQueue(knowledgePoints[index])
        saveState()
    }

    private fun syncReviewQueue(updated: KnowledgePoint) {
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

    fun completeOnboarding() {
        hasCompletedOnboarding = true
        saveState()
    }

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
