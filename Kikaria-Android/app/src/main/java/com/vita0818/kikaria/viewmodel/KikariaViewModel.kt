package com.vita0818.kikaria.viewmodel

import android.app.Application
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.AndroidViewModel
import com.vita0818.kikaria.data.DailyReviewRecord
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
    private var presetStates = mutableStateMapOf<String, KikariaPersistence.PresetStudyState>()
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
    var dailyReviewRecords = mutableStateMapOf<String, DailyReviewRecord>()

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

    fun matchesKnowledgeSearch(point: KnowledgePoint, query: String): Boolean {
        val normalizedQuery = query.trim()
        if (normalizedQuery.isEmpty()) return true
        return point.title.contains(normalizedQuery, ignoreCase = true) ||
            point.tags.any { it.contains(normalizedQuery, ignoreCase = true) } ||
            point.hint.contains(normalizedQuery, ignoreCase = true) ||
            point.content.contains(normalizedQuery, ignoreCase = true)
    }

    fun scopeTagsMatchingSearch(query: String): List<String> {
        val normalizedQuery = query.trim()
        if (normalizedQuery.isEmpty()) return allTags
        val relevantTags = knowledgePoints
            .filter { matchesKnowledgeSearch(it, normalizedQuery) }
            .flatMap { it.tags }
            .toSet()
        return allTags.filter { tag ->
            tag.contains(normalizedQuery, ignoreCase = true) || tag in relevantTags
        }
    }

    init {
        com.vita0818.kikaria.util.KikariaNotificationManager.createChannel(getApplication())
        loadState()
        rescheduleReminderIfEnabled()
    }

    // --- Persistence ---

    private fun loadState() {
        val state = KikariaPersistence.load(getApplication())
        if (state != null) {
            hasCompletedOnboarding = state.hasCompletedOnboarding
            hasCompletedProfileSetup = state.hasCompletedProfileSetup
            userDisplayName = state.userDisplayName
            userHandle = state.userHandle.ifEmpty { "user" }
            avatarUri = state.avatarUri
        }

        installPresets(state?.presets.orEmpty())
        activePresetId = resolvePresetId(
            state?.currentPresetId.orEmpty().ifEmpty { KnowledgePreset.DEFAULT_PRESET_ID }
        )

        presetStates.clear()
        state?.presetStates.orEmpty().forEach { (presetId, presetState) ->
            presets.find { it.id == presetId }?.let { preset ->
                presetStates[presetId] = sanitizePresetState(presetState, preset)
            }
        }

        if (state != null && activePresetId !in presetStates) {
            legacyStateFromAppState(state, activePresetId)?.let { legacyState ->
                presetStates[activePresetId] = legacyState
            }
        }

        ensurePresetStates()
        restorePresetState(presetStates[activePresetId] ?: initialStudyState(activePreset ?: return))
    }

    fun saveState() {
        val lastActive = Date()
        val activeState = currentPresetStateSnapshot(lastActive)
        presetStates[activePresetId] = activeState
        KikariaPersistence.save(
            getApplication(),
            KikariaPersistence.AppState(
                schemaVersion = 3,
                presets = presets.toList(),
                currentPresetId = activePresetId,
                presetStates = presetStates.toMap(),
                userDisplayName = userDisplayName,
                userHandle = userHandle,
                dailyGoal = activeState.dailyGoal,
                countdownStartDate = activeState.countdownStartDate,
                countdownEndDate = activeState.countdownEndDate,
                dangerPercent = activeState.dangerPercent,
                notificationsEnabled = activeState.notificationsEnabled,
                notificationTimeText = activeState.notificationTimeText,
                hasCompletedOnboarding = hasCompletedOnboarding,
                hasCompletedProfileSetup = hasCompletedProfileSetup,
                avatarUri = avatarUri,
                knowledgePoints = activeState.knowledgePoints,
                activityRecords = activeState.activityRecords,
                selectedTags = activeState.selectedTags,
                todayReviewCount = activeState.todayReviewCount,
                todayHintCount = activeState.todayHintCount,
                todayMasteredCount = activeState.todayMasteredCount,
                lastActiveDate = lastActive
            )
        )
        com.vita0818.kikaria.widget.KikariaWidgetUpdater.updateWidgets(getApplication())
    }

    private fun installPresets(savedPresets: List<KnowledgePreset>) {
        presets.clear()
        presets.addAll(savedPresets.ifEmpty { SamplePresets.all })
        val assetPresets = com.vita0818.kikaria.util.PresetLoader.loadPresets(getApplication())
        val existingIds = presets.map { it.id }.toSet()
        assetPresets.filter { it.id !in existingIds }.forEach { presets.add(it) }
    }

    fun loadPresetKnowledgePoints(savedPoints: List<KnowledgePoint> = emptyList()) {
        val preset = activePreset ?: return
        val state = if (savedPoints.isNotEmpty()) {
            initialStudyState(preset).copy(knowledgePoints = savedPoints)
        } else {
            presetStates[preset.id] ?: initialStudyState(preset)
        }
        presetStates[preset.id] = sanitizePresetState(state, preset)
        restorePresetState(presetStates[preset.id] ?: return)
    }

    private fun resolvePresetId(candidateId: String): String {
        return when {
            presets.any { it.id == candidateId } -> candidateId
            presets.any { it.id == KnowledgePreset.DEFAULT_PRESET_ID } -> KnowledgePreset.DEFAULT_PRESET_ID
            else -> presets.firstOrNull()?.id ?: KnowledgePreset.DEFAULT_PRESET_ID
        }
    }

    private fun ensurePresetStates() {
        presets.forEach { preset ->
            if (preset.id !in presetStates) {
                presetStates[preset.id] = initialStudyState(preset)
            }
        }
    }

    private fun initialStudyState(preset: KnowledgePreset): KikariaPersistence.PresetStudyState {
        val points = MarkdownParser.parseKnowledgePoints(preset.markdownText)
        return KikariaPersistence.PresetStudyState(
            presetId = preset.id,
            knowledgePoints = points,
            markdownText = preset.markdownText,
            selectedTags = emptyList(),
            dailyReviewRecords = emptyMap(),
            activityRecords = emptyList(),
            dailyGoal = 20,
            countdownStartDate = null,
            countdownEndDate = null,
            dangerPercent = 80,
            notificationsEnabled = false,
            notificationTimeText = "21:00",
            todayReviewCount = 0,
            todayHintCount = 0,
            todayMasteredCount = 0,
            lastActiveDate = null
        )
    }

    private fun legacyStateFromAppState(
        appState: KikariaPersistence.AppState,
        presetId: String
    ): KikariaPersistence.PresetStudyState? {
        val preset = presets.find { it.id == presetId } ?: return null
        val points = appState.knowledgePoints.ifEmpty {
            MarkdownParser.parseKnowledgePoints(preset.markdownText)
        }
        val lastDate = appState.lastActiveDate
        val hasTodayCounts = lastDate != null && isSameDay(lastDate, Date())

        return sanitizePresetState(
            KikariaPersistence.PresetStudyState(
                presetId = presetId,
                knowledgePoints = points,
                markdownText = preset.markdownText,
                selectedTags = appState.selectedTags,
                dailyReviewRecords = emptyMap(),
                activityRecords = appState.activityRecords,
                dailyGoal = appState.dailyGoal,
                countdownStartDate = appState.countdownStartDate,
                countdownEndDate = appState.countdownEndDate,
                dangerPercent = appState.dangerPercent,
                notificationsEnabled = appState.notificationsEnabled,
                notificationTimeText = appState.notificationTimeText,
                todayReviewCount = if (hasTodayCounts) appState.todayReviewCount else 0,
                todayHintCount = if (hasTodayCounts) appState.todayHintCount else 0,
                todayMasteredCount = if (hasTodayCounts) appState.todayMasteredCount else 0,
                lastActiveDate = lastDate
            ),
            preset
        )
    }

    private fun sanitizePresetState(
        state: KikariaPersistence.PresetStudyState,
        preset: KnowledgePreset
    ): KikariaPersistence.PresetStudyState {
        val markdown = state.markdownText.ifBlank { preset.markdownText }
        val points = state.knowledgePoints.ifEmpty { MarkdownParser.parseKnowledgePoints(markdown) }
        val pointIds = points.map { it.id }.toSet()
        return state.copy(
            presetId = preset.id,
            knowledgePoints = points,
            markdownText = markdown,
            selectedTags = validSelectedTags(state.selectedTags, points),
            dailyReviewRecords = state.dailyReviewRecords.filterKeys { it in pointIds },
            activityRecords = state.activityRecords.filter { it.pointId in pointIds },
            dailyGoal = state.dailyGoal.coerceIn(1, 100),
            dangerPercent = state.dangerPercent.coerceIn(1, 100),
            notificationTimeText = state.notificationTimeText.ifBlank { "21:00" }
        )
    }

    private fun currentPresetStateSnapshot(lastActive: Date = Date()): KikariaPersistence.PresetStudyState {
        val markdown = MarkdownParser.markdownFromPoints(knowledgePoints.toList())
        return KikariaPersistence.PresetStudyState(
            presetId = activePresetId,
            knowledgePoints = knowledgePoints.toList(),
            markdownText = markdown,
            selectedTags = selectedTags.toList(),
            dailyReviewRecords = dailyReviewRecords.toMap(),
            activityRecords = activityRecords.toList(),
            dailyGoal = dailyGoal.coerceIn(1, 100),
            countdownStartDate = countdownStartDate,
            countdownEndDate = countdownEndDate,
            dangerPercent = dangerPercent.coerceIn(1, 100),
            notificationsEnabled = notificationsEnabled,
            notificationTimeText = notificationTimeText.ifBlank { "21:00" },
            todayReviewCount = todayReviewCount,
            todayHintCount = todayHintCount,
            todayMasteredCount = todayMasteredCount,
            lastActiveDate = lastActive
        )
    }

    private fun restorePresetState(state: KikariaPersistence.PresetStudyState) {
        val preset = presets.find { it.id == state.presetId } ?: return
        val sanitized = sanitizePresetState(state, preset)
        activePresetId = sanitized.presetId

        knowledgePoints.clear()
        knowledgePoints.addAll(sanitized.knowledgePoints)

        selectedTags.clear()
        selectedTags.addAll(validSelectedTags(sanitized.selectedTags, sanitized.knowledgePoints))

        dailyReviewRecords.clear()
        dailyReviewRecords.putAll(sanitized.dailyReviewRecords)

        activityRecords.clear()
        activityRecords.addAll(sanitized.activityRecords)

        dailyGoal = sanitized.dailyGoal.coerceIn(1, 100)
        countdownStartDate = sanitized.countdownStartDate
        countdownEndDate = sanitized.countdownEndDate
        dangerPercent = sanitized.dangerPercent.coerceIn(1, 100)
        notificationsEnabled = sanitized.notificationsEnabled
        notificationTimeText = sanitized.notificationTimeText.ifBlank { "21:00" }

        val lastDate = sanitized.lastActiveDate
        if (lastDate != null && isSameDay(lastDate, Date())) {
            todayHintCount = sanitized.todayHintCount
            todayMasteredCount = sanitized.todayMasteredCount
            todayReviewCount = maxOf(todayReviewRecordsTotal(), sanitized.todayReviewCount)
        } else {
            resetTodayCounts()
        }

        resetReviewSession()
        presetStates[sanitized.presetId] = sanitized
    }

    private fun validSelectedTags(tags: Collection<String>, points: List<KnowledgePoint>): List<String> {
        val availableTags = points.flatMap { it.tags }.toSet()
        return tags.distinct().filter { it in availableTags }
    }

    private fun resetReviewSession() {
        reviewQueue.clear()
        currentReviewIndex = -1
        isHintShown = false
        isContentShown = false
        isReviewCompleted = false
    }

    fun todayReviewCountFor(pointId: String): Int {
        val record = dailyReviewRecords[pointId] ?: return 0
        return if (isSameDay(record.date, Date())) record.count else 0
    }

    private fun incrementTodayReviewCountFor(pointId: String) {
        val now = Date()
        val current = dailyReviewRecords[pointId]
        val nextCount = if (current != null && isSameDay(current.date, now)) {
            current.count + 1
        } else {
            1
        }
        dailyReviewRecords[pointId] = DailyReviewRecord(date = now, count = nextCount)
        todayReviewCount = todayReviewRecordsTotal()
    }

    private fun todayReviewRecordsTotal(): Int {
        val today = Date()
        return dailyReviewRecords.values
            .filter { isSameDay(it.date, today) }
            .sumOf { it.count.coerceAtLeast(0) }
    }

    private fun isSameDay(d1: Date, d2: Date): Boolean {
        val c1 = Calendar.getInstance().apply { time = d1 }
        val c2 = Calendar.getInstance().apply { time = d2 }
        return c1.get(Calendar.YEAR) == c2.get(Calendar.YEAR) &&
            c1.get(Calendar.DAY_OF_YEAR) == c2.get(Calendar.DAY_OF_YEAR)
    }

    fun switchPreset(presetId: String) {
        val targetPreset = presets.find { it.id == presetId } ?: return
        presetStates[activePresetId] = currentPresetStateSnapshot()
        val targetState = presetStates[presetId] ?: initialStudyState(targetPreset)
        presetStates[presetId] = sanitizePresetState(targetState, targetPreset)
        restorePresetState(presetStates[presetId] ?: return)
        saveState()
        rescheduleReminderIfEnabled()
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
        if (enabled && !com.vita0818.kikaria.util.KikariaNotificationManager.canPostNotifications(getApplication())) {
            notificationsEnabled = false
            toastMessage = "请在系统设置中允许通知"
            saveState()
            return
        }

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
        val scheduled = com.vita0818.kikaria.util.KikariaNotificationManager.scheduleReminder(
            getApplication(), hour, minute
        )
        if (!scheduled) {
            notificationsEnabled = false
            toastMessage = "请在系统设置中允许通知"
            saveState()
        }
    }

    private fun cancelReminder() {
        com.vita0818.kikaria.util.KikariaNotificationManager.cancelReminder(getApplication())
    }

    private fun rescheduleReminderIfEnabled() {
        if (notificationsEnabled) {
            scheduleReminder()
        } else {
            cancelReminder()
        }
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
        presetStates[activePresetId] = currentPresetStateSnapshot()
        val preset = KnowledgePreset(
            id = "user-${UUID.randomUUID()}",
            name = trimmedName.ifEmpty { "新预设" },
            subtitle = "自定义知识点",
            category = trimmedCategory,
            markdownText = markdownText.trim(),
            isBuiltIn = false
        )
        presets.add(preset)
        presetStates[preset.id] = initialStudyState(preset)
        switchPreset(preset.id)
        showToast("已创建「${preset.name}」")
        return preset
    }

    fun updatePreset(presetId: String, name: String, category: String, markdownText: String) {
        val index = presets.indexOfFirst { it.id == presetId }
        if (index == -1) return
        val preset = presets[index]
        val previousState = stateForEditing(presetId) ?: initialStudyState(preset)
        val parsedPoints = MarkdownParser.parseKnowledgePoints(markdownText.trim().ifEmpty { preset.markdownText })
        val mergedPoints = mergeParsedPointsWithExisting(parsedPoints, previousState.knowledgePoints)
        val updated = preset.copy(
            name = name.trim().ifEmpty { preset.name },
            category = category.trim().ifEmpty { preset.category },
            markdownText = markdownText.trim().ifEmpty { preset.markdownText }
        )
        presets[index] = updated
        val updatedState = sanitizePresetState(
            previousState.copy(
                presetId = presetId,
                knowledgePoints = mergedPoints,
                markdownText = updated.markdownText,
                selectedTags = validSelectedTags(previousState.selectedTags, mergedPoints)
            ),
            updated
        )
        presetStates[presetId] = updatedState
        if (activePresetId == presetId) {
            restorePresetState(updatedState)
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
        presetStates[activePresetId] = currentPresetStateSnapshot()
        presets.remove(preset)
        presetStates.remove(presetId)
        if (activePresetId == presetId) {
            val nextPreset = presets.firstOrNull() ?: return
            val nextState = presetStates[nextPreset.id] ?: initialStudyState(nextPreset)
            presetStates[nextPreset.id] = nextState
            restorePresetState(nextState)
        }
        saveState()
        showToast("已删除「${preset.name}」")
    }

    fun knowledgePointsForPreset(presetId: String): List<KnowledgePoint> {
        val state = stateForEditing(presetId) ?: return emptyList()
        return state.knowledgePoints
    }

    fun upsertKnowledgePoint(presetId: String, point: KnowledgePoint) {
        val preset = presets.find { it.id == presetId } ?: return
        val state = stateForEditing(presetId) ?: initialStudyState(preset)
        val points = state.knowledgePoints.toMutableList()
        val index = points.indexOfFirst { it.id == point.id }
        if (index == -1) points.add(point) else points[index] = point
        syncEditedPresetState(presetId, points, state)
        showToast(if (index == -1) "已添加知识点" else "已更新知识点")
    }

    fun deleteKnowledgePoint(presetId: String, pointId: String) {
        val state = stateForEditing(presetId) ?: return
        val points = state.knowledgePoints.filterNot { it.id == pointId }
        val updatedRecords = state.dailyReviewRecords.filterKeys { it != pointId }
        val updatedActivities = state.activityRecords.filter { it.pointId != pointId }
        syncEditedPresetState(
            presetId,
            points,
            state.copy(
                dailyReviewRecords = updatedRecords,
                activityRecords = updatedActivities,
                selectedTags = validSelectedTags(state.selectedTags, points)
            )
        )
        showToast("已删除知识点")
    }

    private fun stateForEditing(presetId: String): KikariaPersistence.PresetStudyState? {
        if (presetId == activePresetId) {
            return currentPresetStateSnapshot()
        }
        val preset = presets.find { it.id == presetId } ?: return null
        return presetStates[presetId] ?: initialStudyState(preset)
    }

    private fun syncEditedPresetState(
        presetId: String,
        points: List<KnowledgePoint>,
        baseState: KikariaPersistence.PresetStudyState
    ) {
        val presetIndex = presets.indexOfFirst { it.id == presetId }
        if (presetIndex == -1) return
        val markdown = MarkdownParser.markdownFromPoints(points)
        val updatedPreset = presets[presetIndex].copy(markdownText = markdown)
        presets[presetIndex] = updatedPreset
        val updatedState = sanitizePresetState(
            baseState.copy(
                presetId = presetId,
                knowledgePoints = points,
                markdownText = markdown,
                selectedTags = validSelectedTags(baseState.selectedTags, points)
            ),
            updatedPreset
        )
        presetStates[presetId] = updatedState
        if (presetId == activePresetId) {
            restorePresetState(updatedState)
        }
        saveState()
    }

    private fun mergeParsedPointsWithExisting(
        parsedPoints: List<KnowledgePoint>,
        existingPoints: List<KnowledgePoint>
    ): List<KnowledgePoint> {
        val existingByTitle = existingPoints.associateBy { it.title }
        return parsedPoints.map { parsed ->
            val existing = existingByTitle[parsed.title] ?: return@map parsed
            parsed.copy(
                id = existing.id,
                reinforcementCount = existing.reinforcementCount,
                lastReinforcedAt = existing.lastReinforcedAt,
                isMastered = existing.isMastered,
                createdAt = existing.createdAt
            )
        }
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
        if (isContentShown) return
        isContentShown = true
        incrementTodayReviewCountFor(point.id)
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

    fun addCurrentPointReinforcement(point: KnowledgePoint? = null, shouldShowToast: Boolean = true) {
        val target = point ?: currentPoint ?: return
        val index = knowledgePoints.indexOfFirst { it.id == target.id }
        if (index == -1) return

        val updated = target.addReinforcement()
        knowledgePoints[index] = updated
        recordActivity(StudyActivityType.ADDED_REINFORCEMENT, target)
        if (shouldShowToast) {
            toastMessage = "已加入重点集锦 (第${updated.reinforcementCount}次)"
        }

        syncReviewQueue(updated)
        saveState()
    }

    fun removeCurrentPointReinforcement(point: KnowledgePoint? = null, shouldShowToast: Boolean = true) {
        val target = point ?: currentPoint ?: return
        if (!target.isReinforced) return

        val index = knowledgePoints.indexOfFirst { it.id == target.id }
        if (index == -1) return

        val updated = target.clearReinforcement()
        knowledgePoints[index] = updated
        recordActivity(StudyActivityType.REMOVED_REINFORCEMENT, target)
        if (shouldShowToast) {
            toastMessage = "已移出重点集锦"
        }

        syncReviewQueue(updated)
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
